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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Bellerophon.Runtime.Animation;

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
        private const string DeathTergoClipPath = AssetFolder + "/Death_Tergo_Retargeted.anim";
        private const string StunTwistAssetFolder =
            "Assets/_Project/Animations/PlayerStatusEffects/StunTwist";
        private const string StunTwistSourceAssetPath =
            StunTwistAssetFolder + "/Sources/Stun_Twist_Source.fbx";
        private const string StunTwistClipPath =
            StunTwistAssetFolder + "/Stun_Twist_Retargeted.anim";
        private const string StunTwistControllerPath =
            StunTwistAssetFolder + "/Stun_Twist.controller";
        private const string StunTwistExternalPath =
            "player model/transfer stunned.fbx";
        private const string StunTwistTarget = "Stun_Twist";
        private const string StunTwistState = "StunTwist_Source";
        private const string StunTwistValidationFolder =
            "docs/validation/StunTwistAnimationAndElectricArc";
        private const string StunTwistApplicationReportPath =
            StunTwistValidationFolder + "/AnimationApplication.txt";
        private const string StunTwistInspectionReportPath =
            StunTwistValidationFolder + "/AnimationInspection.txt";
        private const string StunTwistAnimationReviewImagePath =
            StunTwistValidationFolder + "/AnimationReview.png";
        private const string StunTwistAnimationReviewReportPath =
            StunTwistValidationFolder + "/AnimationReview.txt";
        private const string StunTwistFinalImagePath =
            StunTwistValidationFolder + "/Final.png";
        private const string StunTwistFinalReportPath =
            StunTwistValidationFolder + "/Final.txt";
        private const string StunTwistSampleRoot =
            "Assets/_Project/ArtSamples/StunTwistElectricArc";
        private const string StunTwistSamplePrefabPath =
            StunTwistSampleRoot + "/Prefabs/StunTwistElectricArcVfx.prefab";
        private const string StunTwistSampleScenePath =
            StunTwistSampleRoot + "/StunTwistElectricArcSample.unity";
        private const string StunTwistSampleMaterialFolder =
            StunTwistSampleRoot + "/Materials";
        private const string StunTwistSampleTypeName =
            "Bellerophon.ArtSamples.StunTwistElectricArcUnitySample";
        private const string StunTwistSampleFrontPath =
            "artSample/StunTwistElectricArc/unity_front.png";
        private const string StunTwistSampleRearPath =
            "artSample/StunTwistElectricArc/unity_rear.png";
        private const string StunTwistSampleComparisonPath =
            "artSample/StunTwistElectricArc/unity_comparison.png";
        private const string StunTwistAppliedEffectName =
            "StunTwistElectricArcVfx";
        private const string StunTwistAppliedValidationFolder =
            StunTwistValidationFolder + "/Applied";
        private const string StunTwistAppliedApplicationReportPath =
            StunTwistAppliedValidationFolder + "/Application.txt";
        private const string StunTwistAppliedInspectionReportPath =
            StunTwistAppliedValidationFolder + "/Inspection.txt";
        private const string StunTwistAppliedReviewImagePath =
            StunTwistAppliedValidationFolder + "/Review.png";
        private const string StunTwistAppliedReviewReportPath =
            StunTwistAppliedValidationFolder + "/Review.txt";
        private const string StunTwistAppliedFinalImagePath =
            StunTwistAppliedValidationFolder + "/Final.png";
        private const string StunTwistAppliedFinalReportPath =
            StunTwistAppliedValidationFolder + "/Final.txt";
        private const int StunTwistSampleLayer = 30;
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
        private const string DeathRagdollFinalImagePath =
            ValidationFolder + "/DeathRagdollFinal.png";
        private const string DeathRagdollFinalReportPath =
            ValidationFolder + "/DeathRagdollFinal.txt";
        private const string DeathGroundAndLaunchFinalImagePath =
            ValidationFolder + "/DeathFullBodyLaunchTumbleBalance/Final.png";
        private const string DeathGroundAndLaunchFinalReportPath =
            ValidationFolder + "/DeathFullBodyLaunchTumbleBalance/Final.txt";
        private const string DeathTergoValidationFolder = ValidationFolder + "/DeathTergo";
        private const string DeathTergoApplicationReportPath =
            DeathTergoValidationFolder + "/Application.txt";
        private const string DeathTergoInspectionReportPath =
            DeathTergoValidationFolder + "/Inspection.txt";
        private const string DeathTergoReviewImagePath =
            DeathTergoValidationFolder + "/Review.png";
        private const string DeathTergoReviewReportPath =
            DeathTergoValidationFolder + "/Review.txt";
        private const string DeathTergoFinalImagePath =
            DeathTergoValidationFolder + "/Final.png";
        private const string DeathTergoFinalReportPath =
            DeathTergoValidationFolder + "/Final.txt";
        private const float DeathRagdollDurationSeconds = 0.5f;
        private const float FullBodyLaunchDelaySeconds = 0.3f;
        private const float FullBodyLaunchLandingHoldSeconds = 0.5f;
        private const float FullBodyLaunchMaximumRangeMeters = 3f;

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
        private const string DeathFullBodyLaunchTarget = "Death_FullBodyLaunch";
        private const string DeathTergoTarget = "Death_Tergo";
        private const string HitState = "HitReaction_Source";
        private const string FrontFallState = "FrontFall_Source";
        private const string BackFallState = "BackFall_Source";
        private const string GetUpState = "KnockdownGetUp_Source";
        private const string DeathState = "Death_Source";
        private const string DeathTergoState = "DeathTergo_Laying_Source";

        private const string HitControllerPath = AssetFolder + "/Hit_Reaction.controller";
        private const string KnockdownControllerPath = AssetFolder + "/Knockdown.controller";
        private const string GetUpControllerPath = AssetFolder + "/Knockdown_GetUp.controller";
        private const string DeathControllerPath = AssetFolder + "/Death.controller";
        private const string DeathTergoControllerPath = AssetFolder + "/Death_Tergo.controller";

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

        private static readonly SourceSpec DeathTergoSource = new SourceSpec(
            "player model/transfer laying.fbx",
            SourceFolder + "/Death_Tergo_Source.fbx",
            "5970203E9DEF816824178E235C3D52C6A102F53CC9A8B582AA8E5A8C3CC6FF7B",
            false);

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
            AnimationClip deathRetargeted = CreateRetargetedGenericClip(deathClip, DeathClipPath, false);
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
            ConfigureDeathRagdollLoop(death, animators[3]);
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
                .AppendLine("Death=" + DescribeClip(deathClip) + ",sourceLoop=True")
                .AppendLine("DeathRetargetedLoop=False")
                .AppendLine("DeathRagdollDurationSeconds=0.5")
                .AppendLine("DeathSequence=OriginalClip->Ragdoll->ImmediateFirstPoseReset");
            WriteText(ApplyReportPath, report.ToString());
            Debug.Log("[PlayerDamageReaction] Original Mixamo motion connected through Generic compatibility clips.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Apply Death Ragdoll Loop")]
        internal static void ApplyDeathRagdollLoop()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject death = FindUnique(scene, DeathTarget);
            Animator animator = RequireAnimator(death);
            AnimationClip deathClip = RequireAsset<AnimationClip>(DeathClipPath);
            SetClipLoop(deathClip, false);
            RequireSingleController(death, DeathControllerPath, deathClip);
            ConfigureDeathRagdollLoop(death, animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed while applying Death ragdoll loop.");
            AssetDatabase.SaveAssets();
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[PlayerDamageReaction] Death original clip now hands off to a 0.5-second full-body ragdoll before immediate first-pose reset.");
        }

        internal static void InspectDeathRagdollLoop()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject death = FindUnique(scene, DeathTarget);
            AnimationClip source = RequireSingleClip(DeathSource.AssetPath);
            AnimationClip retargeted = RequireAsset<AnimationClip>(DeathClipPath);
            RequireClipLoop(source, true, DeathSource.AssetPath);
            RequireClipLoop(retargeted, false, DeathClipPath);
            RequireSingleController(death, DeathControllerPath, retargeted);
            DeathRagdollLoop loop = death.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException("Death is missing DeathRagdollLoop.");
            if (loop.ConfiguredAnimator != RequireAnimator(death))
                throw new InvalidOperationException("DeathRagdollLoop Animator reference differs.");
            if (loop.Mode != DeathRagdollLoop.PlaybackMode.AnimationEnd)
                throw new InvalidOperationException("DeathRagdollLoop mode differs.");
            if (!string.Equals(loop.StateName, DeathState, StringComparison.Ordinal))
                throw new InvalidOperationException("DeathRagdollLoop state name differs.");
            if (Mathf.Abs(loop.RagdollDuration - DeathRagdollDurationSeconds) > 0.0001f)
                throw new InvalidOperationException("DeathRagdollLoop duration differs from 0.5 seconds.");
            if (Mathf.Abs(loop.SharedGroundHeight - death.transform.position.y) > 0.0001f)
                throw new InvalidOperationException("DeathRagdollLoop ground height differs from the Death placement.");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
        }

        [MenuItem("Bellerophon/Player/Apply Death Tergo Laying Ragdoll Loop")]
        internal static void ApplyDeathTergoLayingRagdollLoop()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, DeathTergoTarget);
            RootSnapshot root = new RootSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar avatar = animator.avatar ??
                throw new InvalidOperationException("Death_Tergo Animator Avatar is missing.");

            EnsureFolder(AssetFolder);
            EnsureFolder(SourceFolder);
            CopyAndConfigureSource(DeathTergoSource);
            AnimationClip source = RequireSingleClip(DeathTergoSource.AssetPath);
            AnimationClip retargeted = CreateRetargetedGenericClip(
                source, DeathTergoClipPath, false);
            AnimatorController controller = CreateSingleController(
                DeathTergoControllerPath, DeathTergoState, retargeted);
            Connect(animator, controller);

            DeathRagdollLoop loop = target.GetComponent<DeathRagdollLoop>();
            if (loop == null)
                loop = Undo.AddComponent<DeathRagdollLoop>(target);
            loop.ConfigureAnimationDeath(
                animator,
                DeathTergoState,
                DeathRagdollDurationSeconds,
                target.transform.position.y);
            EditorUtility.SetDirty(loop);
            AssetDatabase.SaveAssets();

            root.RequireUnchanged(target.transform, DeathTergoTarget);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                DeathTergoTarget + " renderers");
            if (animator.avatar != avatar)
                throw new InvalidOperationException("Death_Tergo Avatar changed unexpectedly.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying Death_Tergo.");
            AssetDatabase.SaveAssets();
            InspectDeathTergoLayingRagdollLoop();

            var report = new StringBuilder()
                .AppendLine("Death_Tergo laying animation and ragdoll loop application")
                .AppendLine("sourceExternalPath=" + DeathTergoSource.ExternalPath)
                .AppendLine("sourceAssetPath=" + DeathTergoSource.AssetPath)
                .AppendLine("sourceSha256=" + DeathTergoSource.Sha256)
                .AppendLine("sourceBinaryCopyExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("retargetedCompatibilityClipGenerated=True")
                .AppendLine("retargetSamplingRate=OriginalSourceFrameRate")
                .AppendLine("retargetPoseInference=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("retargeted=" + DescribeClip(retargeted))
                .AppendLine("retargetedClipLoop=False")
                .AppendLine("sequence=OriginalLayingClip->0.5SecondFullBodyRagdoll->ImmediateFirstPoseReset")
                .AppendLine("ragdollGround=Death_TergoCurrentPlacementY")
                .AppendLine("animatorAndRagdollSimultaneousDrive=False")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False");
            WriteText(DeathTergoApplicationReportPath, report.ToString());
            Debug.Log(
                "[PlayerDamageReaction] Death_Tergo now plays the exact transfer laying source, " +
                "hands off to a 0.5-second full-body ragdoll on its current ground, and resets.");
        }

        internal static void InspectDeathTergoLayingRagdollLoop()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, DeathTergoTarget);
            string external = Absolute(DeathTergoSource.ExternalPath);
            string copied = Absolute(DeathTergoSource.AssetPath);
            RequireEqual(DeathTergoSource.Sha256, Sha256(external), "Death_Tergo external source");
            RequireEqual(DeathTergoSource.Sha256, Sha256(copied), "Death_Tergo copied source");

            AnimationClip source = RequireSingleClip(DeathTergoSource.AssetPath);
            AnimationClip retargeted = RequireAsset<AnimationClip>(DeathTergoClipPath);
            RequireClipLoop(source, false, DeathTergoSource.AssetPath);
            RequireClipLoop(retargeted, false, DeathTergoClipPath);
            if (retargeted.humanMotion)
                throw new InvalidOperationException(
                    "Death_Tergo compatibility clip must contain Generic transform curves.");
            if (Mathf.Abs(source.length - retargeted.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - retargeted.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Death_Tergo retargeted timing differs from the original embedded clip.");
            if (AnimationUtility.GetCurveBindings(retargeted).Length == 0)
                throw new InvalidOperationException("Death_Tergo compatibility clip has no transform curves.");

            RequireSingleController(target, DeathTergoControllerPath, retargeted);
            Animator animator = RequireAnimator(target);
            DeathRagdollLoop loop = target.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException("Death_Tergo is missing DeathRagdollLoop.");
            if (loop.ConfiguredAnimator != animator)
                throw new InvalidOperationException("Death_Tergo ragdoll Animator reference differs.");
            if (loop.Mode != DeathRagdollLoop.PlaybackMode.AnimationEnd)
                throw new InvalidOperationException("Death_Tergo ragdoll mode differs.");
            if (!string.Equals(loop.StateName, DeathTergoState, StringComparison.Ordinal))
                throw new InvalidOperationException("Death_Tergo ragdoll state name differs.");
            if (Mathf.Abs(loop.RagdollDuration - DeathRagdollDurationSeconds) > 0.0001f)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll duration differs from 0.5 seconds.");
            if (Mathf.Abs(loop.SharedGroundHeight - target.transform.position.y) > 0.0001f)
                throw new InvalidOperationException(
                    "Death_Tergo physical ground differs from its current placement.");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Death_Tergo laying animation and ragdoll loop inspection")
                .AppendLine("sourceSha256=" + Sha256(copied))
                .AppendLine("sourceBinaryCopyExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("retargeted=" + DescribeClip(retargeted))
                .AppendLine("timingMatchesSource=True")
                .AppendLine("retargetedClipLoop=False")
                .AppendLine("controllerState=" + DeathTergoState)
                .AppendLine("ragdollDurationSeconds=0.5")
                .AppendLine("ragdollGroundHeight=" +
                    loop.SharedGroundHeight.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("animatorAndRagdollSimultaneousDrive=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(DeathTergoInspectionReportPath, report.ToString());
        }

        [MenuItem("Bellerophon/Player/Inspect Stun Twist Sources")]
        internal static void InspectStunTwistAnimationAndElectricArcSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, StunTwistTarget);
            Animator animator = RequireAnimator(target);
            if (animator.avatar == null)
                throw new InvalidOperationException("Stun_Twist Animator Avatar is missing.");
            string external = Absolute(StunTwistExternalPath);
            if (!File.Exists(external))
                throw new FileNotFoundException(
                    "Stun_Twist Mixamo source is missing.", external);
            if (new FileInfo(external).Length <= 0)
                throw new InvalidOperationException(
                    "Stun_Twist Mixamo source is empty.");
            if (target.GetComponentsInChildren<Component>(true).Any(
                component => component != null &&
                    component.GetType().FullName == StunTwistSampleTypeName))
                throw new InvalidOperationException(
                    "The unapproved electric-arc sample is already linked to Stun_Twist.");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[StunTwist] Sources inspected read-only. source=" +
                StunTwistExternalPath + "|bytes=" +
                new FileInfo(external).Length +
                "|targetAvatar=" + animator.avatar.name +
                "|sampleLinkedToGameplay=False");
        }

        [MenuItem("Bellerophon/Player/Apply Stun Twist Animation")]
        internal static void ApplyStunTwistAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, StunTwistTarget);
            RootSnapshot root = new RootSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Animator animator = RequireAnimator(target);
            Avatar avatar = animator.avatar ??
                throw new InvalidOperationException("Stun_Twist Animator Avatar is missing.");

            EnsureFolder(StunTwistAssetFolder);
            EnsureFolder(StunTwistAssetFolder + "/Sources");
            string sourceHash = CopyAndConfigureExactLoopingSource(
                StunTwistExternalPath,
                StunTwistSourceAssetPath);
            AnimationClip source = RequireSingleClip(StunTwistSourceAssetPath);
            AnimationClip retargeted = CreateRetargetedGenericClip(
                source,
                StunTwistClipPath,
                true);
            AnimatorController controller = CreateSingleController(
                StunTwistControllerPath,
                StunTwistState,
                retargeted);
            Connect(animator, controller);

            root.RequireUnchanged(target.transform, StunTwistTarget);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                StunTwistTarget + " renderers");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(
                    "Stun_Twist Avatar changed unexpectedly.");
            if (target.GetComponentsInChildren<Component>(true).Any(
                component => component != null &&
                    component.GetType().FullName == StunTwistSampleTypeName))
                throw new InvalidOperationException(
                    "The electric-arc sample must not be linked before approval.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying Stun_Twist.");
            AssetDatabase.SaveAssets();
            InspectStunTwistAnimation();

            var report = new StringBuilder()
                .AppendLine("Stun_Twist exact Mixamo animation application")
                .AppendLine("sourceExternalPath=" + StunTwistExternalPath)
                .AppendLine("sourceAssetPath=" + StunTwistSourceAssetPath)
                .AppendLine("sourceSha256=" + sourceHash)
                .AppendLine("sourceBinaryCopyExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("retargetedCompatibilityClipGenerated=True")
                .AppendLine("retargetSamplingRate=OriginalSourceFrameRate")
                .AppendLine("retargetPoseInference=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("retargeted=" + DescribeClip(retargeted))
                .AppendLine("sourceLoop=True")
                .AppendLine("retargetedLoop=True")
                .AppendLine("targetTransformChanged=False")
                .AppendLine("targetRenderersChanged=False")
                .AppendLine("targetAvatarChanged=False")
                .AppendLine("electricArcSampleLinkedToGameplay=False");
            WriteText(StunTwistApplicationReportPath, report.ToString());
            Debug.Log(
                "[StunTwist] Exact transfer stunned Mixamo animation connected and looped; " +
                "electric-arc art sample remains unlinked.");
        }

        internal static void InspectStunTwistAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, StunTwistTarget);
            string external = Absolute(StunTwistExternalPath);
            string copied = Absolute(StunTwistSourceAssetPath);
            if (!File.Exists(external) || !File.Exists(copied))
                throw new FileNotFoundException(
                    "Stun_Twist source or copied FBX is missing.");
            string externalHash = Sha256(external);
            RequireEqual(
                externalHash,
                Sha256(copied),
                "Stun_Twist exact source copy");

            AnimationClip source = RequireSingleClip(StunTwistSourceAssetPath);
            AnimationClip retargeted = RequireAsset<AnimationClip>(StunTwistClipPath);
            RequireClipLoop(source, true, StunTwistSourceAssetPath);
            RequireClipLoop(retargeted, true, StunTwistClipPath);
            if (retargeted.humanMotion)
                throw new InvalidOperationException(
                    "Stun_Twist compatibility clip must contain Generic transform curves.");
            if (Mathf.Abs(source.length - retargeted.length) > 0.0001f ||
                Mathf.Abs(source.frameRate - retargeted.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "Stun_Twist retargeted timing differs from the original embedded clip.");
            if (AnimationUtility.GetCurveBindings(retargeted).Length == 0)
                throw new InvalidOperationException(
                    "Stun_Twist compatibility clip has no transform curves.");
            RequireSingleController(target, StunTwistControllerPath, retargeted);
            bool electricArcLinked = target.GetComponentsInChildren<Component>(true).Any(
                component => component != null &&
                    component.GetType().FullName == StunTwistSampleTypeName);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();

            var report = new StringBuilder()
                .AppendLine("Stun_Twist animation structural inspection")
                .AppendLine("sourceSha256=" + externalHash)
                .AppendLine("sourceBinaryCopyExact=True")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("retargeted=" + DescribeClip(retargeted))
                .AppendLine("timingMatchesSource=True")
                .AppendLine("sourceLoop=True")
                .AppendLine("retargetedLoop=True")
                .AppendLine("controllerState=" + StunTwistState)
                .AppendLine("electricArcSampleLinkedToGameplay=" + electricArcLinked)
                .AppendLine("unityConsoleErrors=0");
            WriteText(StunTwistInspectionReportPath, report.ToString());
        }

        [MenuItem("Bellerophon/Art Samples/Build Stun Twist Electric Arc Sample")]
        internal static void BuildStunTwistElectricArcArtSample()
        {
            RequireEditMode();
            Type effectType = RequireRuntimeType(StunTwistSampleTypeName);
            GameObject playerSource = RequireAsset<GameObject>(PlayerGenericAssetPath);
            AnimationClip stunClip = RequireAsset<AnimationClip>(StunTwistClipPath);
            RuntimeAnimatorController stunController =
                RequireAsset<RuntimeAnimatorController>(StunTwistControllerPath);

            EnsureFolder("Assets/_Project/ArtSamples");
            EnsureFolder(StunTwistSampleRoot);
            EnsureFolder(StunTwistSampleRoot + "/Prefabs");
            EnsureFolder(StunTwistSampleMaterialFolder);

            Material halo = CreateStunArcMaterial(
                StunTwistSampleMaterialFolder + "/StunElectricCyanHalo.mat",
                "StunElectricCyanHalo",
                new Color(0.12f, 0.88f, 1f, 0.34f));
            Material glow = CreateStunArcMaterial(
                StunTwistSampleMaterialFolder + "/StunElectricCyanGlow.mat",
                "StunElectricCyanGlow",
                new Color(0.22f, 0.92f, 1f, 0.88f));
            Material core = CreateStunArcMaterial(
                StunTwistSampleMaterialFolder + "/StunElectricWhiteCore.mat",
                "StunElectricWhiteCore",
                new Color(0.88f, 0.99f, 1f, 1f));

            BodySampleDimensions dimensions = MeasurePlayerBody(playerSource);
            BuildStunElectricArcPrefab(
                effectType,
                dimensions,
                halo,
                glow,
                core);
            CreateStunElectricArcSampleScene(
                playerSource,
                stunClip,
                stunController,
                dimensions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            InspectStunTwistElectricArcArtSampleAssets();
            Debug.Log(
                "[StunTwistSample] Unity-replayable cyan-white torso arc prefab and " +
                "standalone sample scene built without CargoRunMvp linkage.");
        }

        internal static void InspectStunTwistElectricArcArtSample()
        {
            RequireEditMode();
            InspectStunTwistElectricArcArtSampleAssets();
            CaptureStunTwistElectricArcArtSampleReview();
        }

        private static void InspectStunTwistElectricArcArtSampleAssets()
        {
            Type effectType = RequireRuntimeType(StunTwistSampleTypeName);
            GameObject prefab = RequireAsset<GameObject>(StunTwistSamplePrefabPath);
            if (prefab.GetComponent(effectType) == null)
                throw new InvalidOperationException(
                    "Stun electric-arc prefab controller is missing.");
            LineRenderer[] lines = prefab.GetComponentsInChildren<LineRenderer>(true);
            if (lines.Length != 24)
                throw new InvalidOperationException(
                    "Stun electric-arc prefab must contain 8 cyan-white three-layer arcs; actual=" +
                    lines.Length);
            if (lines.Any(line => line.sharedMaterial == null || line.positionCount != 9))
                throw new InvalidOperationException(
                    "Stun electric-arc line layer configuration is incomplete.");
            RequireAsset<SceneAsset>(StunTwistSampleScenePath);

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
        }

        [MenuItem("Bellerophon/Player/Apply Approved Stun Twist Electric Arc Sample")]
        internal static void ApplyStunTwistApprovedElectricArcSample()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, StunTwistTarget);
            Animator animator = RequireAnimator(target);
            RootSnapshot targetRoot = new RootSnapshot(target.transform);
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            Avatar avatar = animator.avatar;
            Component[] existingControllers = target.GetComponentsInChildren<Component>(true)
                .Where(component => component != null &&
                    component.GetType().FullName == StunTwistSampleTypeName)
                .ToArray();
            if (existingControllers.Length > 1)
                throw new InvalidOperationException(
                    "Stun_Twist has duplicate approved electric-arc controllers.");
            Transform existingEffect = existingControllers.Length == 1
                ? existingControllers[0].transform
                : null;
            string bodyRendererSignature = existingEffect == null
                ? RendererSignature(target.transform)
                : RendererSignatureExcluding(target.transform, existingEffect);

            InspectStunTwistAnimation();
            InspectStunTwistElectricArcArtSampleAssets();
            Hash128 sampleDependencyHashBefore =
                AssetDatabase.GetAssetDependencyHash(StunTwistSamplePrefabPath);
            GameObject prefab = RequireAsset<GameObject>(StunTwistSamplePrefabPath);
            GameObject effect;
            if (existingEffect == null)
            {
                effect = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                    throw new InvalidOperationException(
                        "Approved Stun_Twist electric-arc prefab could not be instantiated.");
            }
            else
            {
                effect = existingEffect.gameObject;
                string existingPrefabPath =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(effect);
                if (!string.Equals(
                    existingPrefabPath,
                    StunTwistSamplePrefabPath,
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Existing Stun_Twist electric-arc object is not the approved prefab.");
            }

            effect.name = StunTwistAppliedEffectName;
            effect.transform.SetParent(animator.transform, false);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one;
            effect.SetActive(true);
            Component effectController = effect.GetComponent(
                RequireRuntimeType(StunTwistSampleTypeName)) ??
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric-arc controller is missing.");
            if (effectController is Behaviour behaviour)
                behaviour.enabled = true;

            targetRoot.RequireUnchanged(target.transform, StunTwistTarget);
            RequireEqual(
                bodyRendererSignature,
                RendererSignatureExcluding(target.transform, effect.transform),
                "Stun_Twist body renderers");
            if (animator.runtimeAnimatorController != controller || animator.avatar != avatar)
                throw new InvalidOperationException(
                    "Stun_Twist Animator configuration changed while applying the approved sample.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying the approved Stun_Twist electric arc.");
            AssetDatabase.SaveAssets();
            Hash128 sampleDependencyHashAfter =
                AssetDatabase.GetAssetDependencyHash(StunTwistSamplePrefabPath);
            if (sampleDependencyHashBefore != sampleDependencyHashAfter)
                throw new InvalidOperationException(
                    "Approved Stun_Twist sample assets changed during scene application.");

            InspectStunTwistApprovedElectricArcSample();
            var report = new StringBuilder()
                .AppendLine("Approved Stun_Twist electric-arc sample application")
                .AppendLine("target=Stun_Twist")
                .AppendLine("approvedPrefab=" + StunTwistSamplePrefabPath)
                .AppendLine("prefabDependencyHash=" + sampleDependencyHashAfter)
                .AppendLine("prefabSourceLinked=True")
                .AppendLine("attachmentParent=Stun_Twist Animator")
                .AppendLine("attachmentLocalPosition=(0,0,0)")
                .AppendLine("attachmentLocalRotation=Identity")
                .AppendLine("attachmentLocalScale=(1,1,1)")
                .AppendLine("animationControllerChanged=False")
                .AppendLine("animationAvatarChanged=False")
                .AppendLine("targetRootTransformChanged=False")
                .AppendLine("targetBodyRenderersChanged=False")
                .AppendLine("sampleVisualAssetsChanged=False");
            WriteText(StunTwistAppliedApplicationReportPath, report.ToString());
            Debug.Log(
                "[StunTwistApplied] Approved electric-arc prefab connected unchanged to Stun_Twist Animator.");
        }

        internal static void InspectStunTwistApprovedElectricArcSample()
        {
            RequireEditMode();
            InspectStunTwistAnimation();
            InspectStunTwistElectricArcArtSampleAssets();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, StunTwistTarget);
            Animator animator = RequireAnimator(target);
            Type effectType = RequireRuntimeType(StunTwistSampleTypeName);
            Component[] controllers = target.GetComponentsInChildren<Component>(true)
                .Where(component => component != null && component.GetType() == effectType)
                .ToArray();
            if (controllers.Length != 1)
                throw new InvalidOperationException(
                    "Stun_Twist must contain exactly one approved electric-arc controller; actual=" +
                    controllers.Length);
            Component effectController = controllers[0];
            GameObject effect = effectController.gameObject;
            if (effect.transform.parent != animator.transform)
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric arc is not parented to the target Animator.");
            if (effect.transform.localPosition.sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(effect.transform.localRotation, Quaternion.identity) > 0.0001f ||
                Vector3.Distance(effect.transform.localScale, Vector3.one) > 0.0001f)
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric-arc root Transform differs from the sample.");
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(effect);
            if (!string.Equals(
                prefabPath,
                StunTwistSamplePrefabPath,
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Stun_Twist electric arc is not linked to the approved prefab.");
            if (!effect.activeInHierarchy ||
                (effectController is Behaviour behaviour && !behaviour.enabled))
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric arc is inactive.");

            LineRenderer[] lines = effect.GetComponentsInChildren<LineRenderer>(true);
            if (lines.Length != 24 || lines.Any(line => line.positionCount != 9))
                throw new InvalidOperationException(
                    "Applied Stun_Twist electric arc differs from the approved 8-group, three-layer sample.");
            string haloPath = StunTwistSampleMaterialFolder + "/StunElectricCyanHalo.mat";
            string glowPath = StunTwistSampleMaterialFolder + "/StunElectricCyanGlow.mat";
            string corePath = StunTwistSampleMaterialFolder + "/StunElectricWhiteCore.mat";
            Dictionary<string, int> materialCounts = lines
                .GroupBy(line => AssetDatabase.GetAssetPath(line.sharedMaterial))
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            if (!materialCounts.TryGetValue(haloPath, out int haloCount) || haloCount != 8 ||
                !materialCounts.TryGetValue(glowPath, out int glowCount) || glowCount != 8 ||
                !materialCounts.TryGetValue(corePath, out int coreCount) || coreCount != 8 ||
                materialCounts.Count != 3)
                throw new InvalidOperationException(
                    "Applied Stun_Twist electric-arc material layers differ from the approved sample.");

            Hash128 dependencyHash =
                AssetDatabase.GetAssetDependencyHash(StunTwistSamplePrefabPath);
            var report = new StringBuilder()
                .AppendLine("Approved Stun_Twist electric-arc structural inspection")
                .AppendLine("approvedPrefab=" + prefabPath)
                .AppendLine("prefabDependencyHash=" + dependencyHash)
                .AppendLine("controllerCount=1")
                .AppendLine("arcGroups=8")
                .AppendLine("layersPerGroup=3")
                .AppendLine("lineRenderers=24")
                .AppendLine("cyanHaloMaterials=8")
                .AppendLine("cyanGlowMaterials=8")
                .AppendLine("whiteCoreMaterials=8")
                .AppendLine("animatorController=" +
                    AssetDatabase.GetAssetPath(animator.runtimeAnimatorController))
                .AppendLine("sampleVisualAssetsChanged=False")
                .AppendLine("unityConsoleErrors=0");
            WriteText(StunTwistAppliedInspectionReportPath, report.ToString());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
        }

        internal static void WriteStunTwistApprovedElectricArcReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float observedNormalizedTime,
            int distinctVisiblePatterns,
            int maximumVisibleGroups,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (observedNormalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Applied Stun_Twist review did not complete one natural loop.");
            if (distinctVisiblePatterns < 2 || maximumVisibleGroups < 1)
                throw new InvalidOperationException(
                    "Applied Stun_Twist electric arcs did not show changing blink patterns.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Stun_Twist root moved during approved VFX review.");
            string image = Absolute(StunTwistAppliedReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(image) ??
                throw new InvalidOperationException(
                    "Applied Stun_Twist review folder is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Approved Stun_Twist electric-arc natural Play Mode review")
                .AppendLine("captureMethod=Natural Animator and VFX playback")
                .AppendLine("panels=Front3,Rear3")
                .AppendLine("approvedSampleVisualsModified=False")
                .AppendLine("animationSourceModified=False")
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    observedNormalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("distinctVisiblePatterns=" + distinctVisiblePatterns)
                .AppendLine("maximumVisibleGroups=" + maximumVisibleGroups)
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(StunTwistAppliedReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void CaptureStunTwistApprovedElectricArcSampleFinal()
        {
            RequireEditMode();
            InspectStunTwistApprovedElectricArcSample();
            string approvedPath = Absolute(StunTwistSampleComparisonPath);
            string appliedPath = Absolute(StunTwistAppliedReviewImagePath);
            if (!File.Exists(approvedPath) || !File.Exists(appliedPath))
                throw new InvalidOperationException(
                    "Approved sample and applied Play Mode review images must exist before final capture.");
            string finalPath = Absolute(StunTwistAppliedFinalImagePath);
            string finalReport = Absolute(StunTwistAppliedFinalReportPath);
            if (File.Exists(finalPath) || File.Exists(finalReport))
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric-arc final evidence already exists.");

            Texture2D approved = LoadPng(approvedPath);
            Texture2D applied = LoadPng(appliedPath);
            try
            {
                Texture2D combined = CombineVertical(approved, applied);
                try
                {
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(finalPath) ??
                        throw new InvalidOperationException(
                            "Applied Stun_Twist final folder is unavailable."));
                    File.WriteAllBytes(finalPath, combined.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(approved);
                UnityEngine.Object.DestroyImmediate(applied);
            }

            WriteText(
                StunTwistAppliedFinalReportPath,
                "Approved Stun_Twist electric-arc application final evidence\n" +
                "approvedSampleAppliedUnchanged=True\n" +
                "animationNaturalPlayModeLoopReviewed=True\n" +
                "electricArcCoverage=Chest,Abdomen,Back\n" +
                "electricArcColor=CyanWhite\n" +
                "electricArcMotion=IrregularOrbitAndBlink\n" +
                "electricArcLinkedToGameplay=True\n" +
                "stunTwistPassStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[StunTwistApplied] Approved sample and applied natural Play Mode review combined once as final evidence.");
        }

        private static void CaptureStunTwistElectricArcArtSampleReview()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene sampleScene = EditorSceneManager.OpenScene(
                StunTwistSampleScenePath,
                OpenSceneMode.Additive);
            Texture2D front = null;
            Texture2D rear = null;
            bool startedAnimationMode = false;
            try
            {
                GameObject player = FindUnique(sampleScene, "StunTwist_Player_Model");
                GameObject effect = FindUnique(sampleScene, "StunTwistElectricArcVfx");
                Camera camera = player.transform.parent
                    .GetComponentsInChildren<Camera>(true)
                    .Single(item => item.name == "StunTwistElectricArcSampleCamera");
                Component controller = effect.GetComponent(
                    RequireRuntimeType(StunTwistSampleTypeName)) ??
                    throw new InvalidOperationException(
                        "Stun electric-arc sample controller is missing.");
                AnimationClip clip = RequireAsset<AnimationClip>(StunTwistClipPath);

                if (AnimationMode.InAnimationMode())
                    throw new InvalidOperationException(
                        "Close the active Animation preview before capturing the Stun sample.");
                AnimationMode.StartAnimationMode();
                startedAnimationMode = true;
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(player, clip, clip.length * 0.42f);
                AnimationMode.EndSampling();
                controller.GetType().GetMethod("SetPreviewTime")?.Invoke(
                    controller,
                    new object[] { 0.83f });

                LogStunSampleSpatialDiagnostics(player, effect, controller);
                ConfigureStunSampleCamera(camera, player, effect, true);
                front = RenderTextureFromCamera(camera, 900, 900);
                WritePng(front, StunTwistSampleFrontPath);
                ConfigureStunSampleCamera(camera, player, effect, false);
                rear = RenderTextureFromCamera(camera, 900, 900);
                WritePng(rear, StunTwistSampleRearPath);
                Texture2D comparison = CombineHorizontal(front, rear);
                try
                {
                    WritePng(comparison, StunTwistSampleComparisonPath);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(comparison);
                }
            }
            finally
            {
                if (startedAnimationMode)
                    AnimationMode.StopAnimationMode();
                if (front != null)
                    UnityEngine.Object.DestroyImmediate(front);
                if (rear != null)
                    UnityEngine.Object.DestroyImmediate(rear);
                EditorSceneManager.CloseScene(sampleScene, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[StunTwistSample] Direct Unity front/rear art-sample review images captured.");
        }

        internal static void WriteStunTwistAnimationReviewEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float observedNormalizedTime,
            Vector3 rootPositionError,
            float rootRotationError)
        {
            if (observedNormalizedTime < 1.05f)
                throw new InvalidOperationException(
                    "Stun_Twist natural playback did not complete one full loop.");
            if (rootPositionError.magnitude > 0.0001f || rootRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Stun_Twist root moved during natural playback.");
            string image = Absolute(StunTwistAnimationReviewImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(image) ??
                throw new InvalidOperationException(
                    "Stun_Twist review folder is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());
            AnimationClip source = RequireSingleClip(StunTwistSourceAssetPath);
            var report = new StringBuilder()
                .AppendLine("Stun_Twist natural Play Mode animation review")
                .AppendLine("captureMethod=Natural Animator playback from connected controller")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("source=" + DescribeClip(source))
                .AppendLine("loopObserved=True")
                .AppendLine("observedNormalizedTime=" +
                    observedNormalizedTime.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("targetRootPositionChanged=False")
                .AppendLine("targetRootRotationChanged=False")
                .AppendLine("electricArcSampleVisible=False")
                .AppendLine("directVisualReviewPending=True");
            WriteText(StunTwistAnimationReviewReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void CaptureStunTwistAnimationAndElectricArcFinal()
        {
            RequireEditMode();
            InspectStunTwistAnimation();
            InspectStunTwistElectricArcArtSampleAssets();
            string animationPath = Absolute(StunTwistAnimationReviewImagePath);
            string samplePath = Absolute(StunTwistSampleComparisonPath);
            if (!File.Exists(animationPath) || !File.Exists(samplePath))
                throw new InvalidOperationException(
                    "Direct animation and art-sample review images must exist before finalization.");
            string finalPath = Absolute(StunTwistFinalImagePath);
            string finalReport = Absolute(StunTwistFinalReportPath);
            if (File.Exists(finalPath) || File.Exists(finalReport))
                throw new InvalidOperationException(
                    "Stun_Twist one-time final evidence already exists.");

            Texture2D animation = LoadPng(animationPath);
            Texture2D sample = LoadPng(samplePath);
            try
            {
                Texture2D combined = CombineVertical(animation, sample);
                try
                {
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(finalPath) ??
                        throw new InvalidOperationException(
                            "Stun_Twist final folder is unavailable."));
                    File.WriteAllBytes(finalPath, combined.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(combined);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(animation);
                UnityEngine.Object.DestroyImmediate(sample);
            }

            WriteText(
                StunTwistFinalReportPath,
                "Stun_Twist animation and electric-arc art sample final evidence\n" +
                "animationSourceExact=True\n" +
                "animationNaturalPlayModeLoopReviewed=True\n" +
                "electricArcUnityApplicable=True\n" +
                "electricArcCoverage=Chest,Abdomen,Back\n" +
                "electricArcColor=CyanWhite\n" +
                "electricArcMotion=IrregularOrbitAndBlink\n" +
                "electricArcLinkedToGameplay=False\n" +
                "stunTwistPassStatusChanged=False\n" +
                "directVisualReviewPassed=True\n");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[StunTwist] Directly reviewed animation and standalone electric-arc " +
                "sample combined once as final evidence.");
        }

        [MenuItem("Bellerophon/Player/Apply Death Ground And Full Body Launch")]
        internal static void ApplyDeathRagdollGroundAndFullBodyLaunch()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject death = FindUnique(scene, DeathTarget);
            GameObject launch = FindUnique(scene, DeathFullBodyLaunchTarget);
            float sharedGroundHeight = death.transform.position.y;
            if (Mathf.Abs(launch.transform.position.y - sharedGroundHeight) > 0.0001f)
                throw new InvalidOperationException(
                    "Death and Death_FullBodyLaunch must share the same physical ground height.");

            AnimationClip deathClip = RequireAsset<AnimationClip>(DeathClipPath);
            SetClipLoop(deathClip, false);
            Animator deathAnimator = RequireAnimator(death);
            RequireSingleController(death, DeathControllerPath, deathClip);
            ConfigureDeathRagdollLoop(death, deathAnimator, sharedGroundHeight);

            Animator launchAnimator = RequireAnimator(launch);
            DeathRagdollLoop launchLoop = launch.GetComponent<DeathRagdollLoop>();
            if (launchLoop == null)
                launchLoop = Undo.AddComponent<DeathRagdollLoop>(launch);
            launchLoop.ConfigureFullBodyLaunch(
                launchAnimator,
                FullBodyLaunchDelaySeconds,
                FullBodyLaunchLandingHoldSeconds,
                FullBodyLaunchMaximumRangeMeters,
                sharedGroundHeight);
            EditorUtility.SetDirty(launchLoop);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying the shared Death ground and full-body launch.");
            AssetDatabase.SaveAssets();
            InspectDeathRagdollGroundAndFullBodyLaunch();
            Debug.Log(
                "[PlayerDamageReaction] Death now rests on the shared physical ground; " +
                "Death_FullBodyLaunch launches after 0.3 seconds, lands within 3 meters, " +
                "holds for 0.5 seconds, and resets.");
        }

        internal static void InspectDeathRagdollGroundAndFullBodyLaunch()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject death = FindUnique(scene, DeathTarget);
            GameObject launch = FindUnique(scene, DeathFullBodyLaunchTarget);
            DeathRagdollLoop deathLoop = death.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException("Death is missing DeathRagdollLoop.");
            DeathRagdollLoop launchLoop = launch.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException("Death_FullBodyLaunch is missing DeathRagdollLoop.");

            if (deathLoop.Mode != DeathRagdollLoop.PlaybackMode.AnimationEnd)
                throw new InvalidOperationException("Death ragdoll mode differs.");
            if (launchLoop.Mode != DeathRagdollLoop.PlaybackMode.FullBodyLaunch)
                throw new InvalidOperationException("Death_FullBodyLaunch ragdoll mode differs.");
            if (deathLoop.ConfiguredAnimator != RequireAnimator(death) ||
                launchLoop.ConfiguredAnimator != RequireAnimator(launch))
                throw new InvalidOperationException("A death ragdoll Animator reference differs.");
            if (Mathf.Abs(deathLoop.SharedGroundHeight - launchLoop.SharedGroundHeight) > 0.0001f)
                throw new InvalidOperationException("Death objects do not share one physical ground height.");
            if (Mathf.Abs(deathLoop.SharedGroundHeight - death.transform.position.y) > 0.0001f)
                throw new InvalidOperationException("Shared death ground differs from the Death placement height.");
            if (Mathf.Abs(deathLoop.RagdollDuration - DeathRagdollDurationSeconds) > 0.0001f ||
                Mathf.Abs(launchLoop.LaunchDelay - FullBodyLaunchDelaySeconds) > 0.0001f ||
                Mathf.Abs(launchLoop.LandingHoldDuration - FullBodyLaunchLandingHoldSeconds) > 0.0001f ||
                Mathf.Abs(launchLoop.MaximumHorizontalRange - FullBodyLaunchMaximumRangeMeters) > 0.0001f)
                throw new InvalidOperationException("Death ground or full-body launch timing/range differs.");
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
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
            RequireClipLoop(death, false, DeathClipPath);
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
            RequireClipLoop(death, false, DeathClipPath);
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
            RequireClipLoop(deathRetargeted, false, DeathClipPath);

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
                .AppendLine("Death=" + DescribeClip(deathClip) + ",sourceLoop=True")
                .AppendLine("HitRetargeted=" + DescribeClip(hitRetargeted))
                .AppendLine("FrontFallRetargeted=" + DescribeClip(frontRetargeted))
                .AppendLine("BackFallRetargeted=" + DescribeClip(backRetargeted))
                .AppendLine("GetUpRetargeted=" + DescribeClip(getUpRetargeted))
                .AppendLine("DeathRetargeted=" + DescribeClip(deathRetargeted) + ",loop=False")
                .AppendLine("DeathRuntimeSequence=OriginalClip->0.5SecondRagdoll->ImmediateFirstPoseReset")
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

        private static Type RequireRuntimeType(string fullName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
            return type ?? throw new InvalidOperationException(
                "Runtime type is unavailable after compilation: " + fullName);
        }

        private static Material CreateStunArcMaterial(
            string path,
            string materialName,
            Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                throw new InvalidOperationException(
                    "No supported unlit line shader was found.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            material.name = materialName;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static BodySampleDimensions MeasurePlayerBody(GameObject source)
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject probe = null;
            try
            {
                probe = PrefabUtility.InstantiatePrefab(source, preview) as GameObject ??
                    throw new InvalidOperationException(
                        "Player model could not be instantiated for body measurement.");
                SetAllRenderersVisible(probe);
                Bounds bounds = CalculateBounds(probe);
                float height = bounds.size.y;
                if (height <= 0.5f)
                    throw new InvalidOperationException(
                        "Player model bounds are invalid for the Stun VFX sample.");
                return new BodySampleDimensions(
                    new Vector3(0f, -height * 0.09f, 0f),
                    height * 0.24f,
                    height * 0.13f,
                    height * 0.10f);
            }
            finally
            {
                if (probe != null)
                    UnityEngine.Object.DestroyImmediate(probe);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void BuildStunElectricArcPrefab(
            Type effectType,
            BodySampleDimensions dimensions,
            Material halo,
            Material glow,
            Material core)
        {
            var root = new GameObject("StunTwistElectricArcVfx");
            try
            {
                Component controller = root.AddComponent(effectType);
                var halos = new List<LineRenderer>();
                var glows = new List<LineRenderer>();
                var cores = new List<LineRenderer>();
                for (int group = 0; group < 8; group++)
                {
                    var groupObject = new GameObject($"TorsoArc_{group:00}");
                    groupObject.transform.SetParent(root.transform, false);
                    halos.Add(CreateStunArcLine(
                        groupObject.transform,
                        $"TorsoArc_{group:00}_Halo",
                        halo,
                        0.048f,
                        0));
                    glows.Add(CreateStunArcLine(
                        groupObject.transform,
                        $"TorsoArc_{group:00}_Glow",
                        glow,
                        0.024f,
                        1));
                    cores.Add(CreateStunArcLine(
                        groupObject.transform,
                        $"TorsoArc_{group:00}_Core",
                        core,
                        0.007f,
                        2));
                }
                var serialized = new SerializedObject(controller);
                SetObjectArray(serialized.FindProperty("cyanHalos"), halos);
                SetObjectArray(serialized.FindProperty("cyanGlows"), glows);
                SetObjectArray(serialized.FindProperty("whiteCores"), cores);
                serialized.FindProperty("torsoCenter").vector3Value = dimensions.Center;
                serialized.FindProperty("torsoHeight").floatValue = dimensions.Height;
                serialized.FindProperty("torsoRadiusX").floatValue = dimensions.RadiusX;
                serialized.FindProperty("torsoRadiusZ").floatValue = dimensions.RadiusZ;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, StunTwistSamplePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static LineRenderer CreateStunArcLine(
            Transform parent,
            string name,
            Material material,
            float width,
            int sortingOrder)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            LineRenderer line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = 9;
            for (int point = 0; point < line.positionCount; point++)
                line.SetPosition(point, Vector3.zero);
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.startWidth = width;
            line.endWidth = width * 0.62f;
            line.startColor = Color.white;
            line.endColor = new Color(1f, 1f, 1f, 0.74f);
            line.sharedMaterial = material;
            line.sortingOrder = sortingOrder;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return line;
        }

        private static void CreateStunElectricArcSampleScene(
            GameObject playerSource,
            AnimationClip stunClip,
            RuntimeAnimatorController stunController,
            BodySampleDimensions dimensions)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene sample = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(sample);
            try
            {
                var root = new GameObject("StunTwistElectricArcSampleRoot");
                SceneManager.MoveGameObjectToScene(root, sample);
                GameObject player = PrefabUtility.InstantiatePrefab(
                    playerSource,
                    sample) as GameObject ??
                    throw new InvalidOperationException(
                        "Player model could not be instantiated in the Stun sample scene.");
                player.name = "StunTwist_Player_Model";
                player.transform.SetParent(root.transform, true);
                SetAllRenderersVisible(player);
                Bounds initialBounds = CalculateBounds(player);
                player.transform.position += Vector3.up * -initialBounds.min.y;
                Animator animator = RequireAnimator(player);
                animator.runtimeAnimatorController = stunController;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                EditorUtility.SetDirty(animator);

                GameObject effectPrefab = RequireAsset<GameObject>(
                    StunTwistSamplePrefabPath);
                GameObject effect = PrefabUtility.InstantiatePrefab(
                    effectPrefab,
                    sample) as GameObject ??
                    throw new InvalidOperationException(
                        "Stun electric-arc prefab could not be instantiated.");
                effect.name = "StunTwistElectricArcVfx";
                effect.transform.SetParent(animator.transform, false);
                effect.transform.localPosition = Vector3.zero;
                effect.transform.localRotation = Quaternion.identity;
                effect.transform.localScale = Vector3.one;

                Camera camera = CreateStunSampleCamera(root.transform);
                CreateStunSampleLighting(root.transform);
                SetLayerRecursively(root, StunTwistSampleLayer);
                camera.cullingMask = 1 << StunTwistSampleLayer;
                ConfigureStunSampleCamera(camera, player, effect, true);

                if (!EditorSceneManager.SaveScene(sample, StunTwistSampleScenePath))
                    throw new InvalidOperationException(
                        "Stun electric-arc sample scene save failed.");
            }
            finally
            {
                EditorSceneManager.CloseScene(sample, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        private static Camera CreateStunSampleCamera(Transform parent)
        {
            var cameraObject = new GameObject(
                "StunTwistElectricArcSampleCamera",
                typeof(Camera));
            cameraObject.transform.SetParent(parent, false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.075f, 1f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            return camera;
        }

        private static void CreateStunSampleLighting(Transform parent)
        {
            var keyObject = new GameObject("StunTwistElectricArcKeyLight", typeof(Light));
            keyObject.transform.SetParent(parent, false);
            Light key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.82f, 0.92f, 1f, 1f);
            key.intensity = 1.45f;
            key.shadows = LightShadows.Soft;
            key.cullingMask = 1 << StunTwistSampleLayer;
            key.transform.rotation = Quaternion.Euler(34f, 30f, 0f);

            var fillObject = new GameObject("StunTwistElectricArcFillLight", typeof(Light));
            fillObject.transform.SetParent(parent, false);
            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.28f, 0.72f, 1f, 1f);
            fill.intensity = 0.85f;
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << StunTwistSampleLayer;
            fill.transform.rotation = Quaternion.Euler(330f, 210f, 0f);
        }

        private static void ConfigureStunSampleCamera(
            Camera camera,
            GameObject player,
            GameObject effect,
            bool front)
        {
            Renderer[] bodyRenderers = player.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.enabled &&
                    !renderer.transform.IsChildOf(effect.transform))
                .ToArray();
            if (bodyRenderers.Length == 0)
                throw new InvalidOperationException(
                    "Stun sample player has no enabled body renderer.");
            Bounds bounds = bodyRenderers[0].bounds;
            foreach (Renderer renderer in bodyRenderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            float height = bounds.size.y;
            Vector3 target = new Vector3(
                bounds.center.x,
                bounds.min.y + height * 0.59f,
                bounds.center.z);
            Vector3 view = front ? player.transform.forward : -player.transform.forward;
            camera.transform.position = target + view.normalized * Mathf.Max(4f, height * 2.2f);
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
            camera.orthographicSize = Mathf.Max(0.65f, height * 0.36f);
        }

        private static void LogStunSampleSpatialDiagnostics(
            GameObject player,
            GameObject effect,
            Component controller)
        {
            Renderer[] bodyRenderers = player.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.enabled &&
                    !renderer.transform.IsChildOf(effect.transform))
                .ToArray();
            Renderer[] effectRenderers = effect.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            Bounds bodyBounds = bodyRenderers[0].bounds;
            foreach (Renderer renderer in bodyRenderers.Skip(1))
                bodyBounds.Encapsulate(renderer.bounds);
            Bounds effectBounds = effectRenderers[0].bounds;
            foreach (Renderer renderer in effectRenderers.Skip(1))
                effectBounds.Encapsulate(renderer.bounds);
            var serialized = new SerializedObject(controller);
            Transform anchor = serialized.FindProperty("torsoAnchor").objectReferenceValue
                as Transform;
            Debug.Log(
                "[StunTwistSample] Spatial diagnostics: player=" +
                player.transform.position.ToString("F4") +
                "|anchor=" + (anchor == null ? "null" : anchor.position.ToString("F4")) +
                "|bodyCenter=" + bodyBounds.center.ToString("F4") +
                "|bodySize=" + bodyBounds.size.ToString("F4") +
                "|effectCenter=" + effectBounds.center.ToString("F4") +
                "|effectSize=" + effectBounds.size.ToString("F4"));
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void SetAllRenderersVisible(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    skinnedMeshRenderer.updateWhenOffscreen = true;
                    skinnedMeshRenderer.forceMatrixRecalculationPerRender = true;
                }
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void SetObjectArray<T>(
            SerializedProperty property,
            IReadOnlyList<T> values) where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static Texture2D RenderTextureFromCamera(
            Camera camera,
            int width,
            int height)
        {
            var renderTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            float previousAspect = camera.aspect;
            try
            {
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                camera.aspect = width / (float)height;
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, camera.backgroundColor);
                // Warm the sampled skinned mesh once before reading the review frame.
                camera.Render();
                camera.Render();
                RenderTexture.active = renderTexture;
                result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                result.Apply(false, false);
                return result;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void WritePng(Texture2D texture, string relativePath)
        {
            string path = Absolute(relativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "PNG destination folder is unavailable."));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static Texture2D CombineHorizontal(Texture2D left, Texture2D right)
        {
            var combined = new Texture2D(
                left.width + right.width,
                Mathf.Max(left.height, right.height),
                TextureFormat.RGBA32,
                false);
            FillTexture(combined, new Color32(4, 9, 15, 255));
            combined.SetPixels32(0, 0, left.width, left.height, left.GetPixels32());
            combined.SetPixels32(left.width, 0, right.width, right.height, right.GetPixels32());
            combined.Apply(false, false);
            return combined;
        }

        private static Texture2D CombineVertical(Texture2D top, Texture2D bottom)
        {
            int width = Mathf.Max(top.width, bottom.width);
            var combined = new Texture2D(
                width,
                top.height + bottom.height,
                TextureFormat.RGBA32,
                false);
            FillTexture(combined, new Color32(4, 9, 15, 255));
            combined.SetPixels32(
                (width - bottom.width) / 2,
                0,
                bottom.width,
                bottom.height,
                bottom.GetPixels32());
            combined.SetPixels32(
                (width - top.width) / 2,
                bottom.height,
                top.width,
                top.height,
                top.GetPixels32());
            combined.Apply(false, false);
            return combined;
        }

        private static void FillTexture(Texture2D texture, Color32 color)
        {
            texture.SetPixels32(
                Enumerable.Repeat(color, texture.width * texture.height).ToArray());
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("PNG could not be decoded: " + path);
            }
            return texture;
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

        private static string CopyAndConfigureExactLoopingSource(
            string externalRelativePath,
            string assetPath)
        {
            string external = Absolute(externalRelativePath);
            string destination = Absolute(assetPath);
            if (!File.Exists(external))
                throw new FileNotFoundException(
                    "Animation source FBX is missing.", external);
            string sourceHash = Sha256(external);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Source destination is unavailable."));
            if (!File.Exists(destination) ||
                !string.Equals(
                    Sha256(destination),
                    sourceHash,
                    StringComparison.OrdinalIgnoreCase))
                File.Copy(external, destination, true);
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter is unavailable: " + assetPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = true;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(assetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter disappeared: " + assetPath);
            ModelImporterClipAnimation[] settings = importer.clipAnimations;
            if (settings == null || settings.Length == 0)
                settings = importer.defaultClipAnimations;
            if (settings == null || settings.Length != 1)
                throw new InvalidOperationException(
                    assetPath + " must expose exactly one embedded animation clip.");
            settings[0].loopTime = true;
            settings[0].loopPose = false;
            importer.clipAnimations = settings;
            importer.SaveAndReimport();

            RequireEqual(sourceHash, Sha256(destination), assetPath);
            RequireClipLoop(RequireSingleClip(assetPath), true, assetPath);
            return sourceHash;
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

        private static void ConfigureDeathRagdollLoop(GameObject death, Animator animator)
        {
            ConfigureDeathRagdollLoop(death, animator, death.transform.position.y);
        }

        private static void ConfigureDeathRagdollLoop(
            GameObject death, Animator animator, float sharedGroundHeight)
        {
            DeathRagdollLoop loop = death.GetComponent<DeathRagdollLoop>();
            if (loop == null)
                loop = Undo.AddComponent<DeathRagdollLoop>(death);
            loop.ConfigureAnimationDeath(
                animator, DeathState, DeathRagdollDurationSeconds, sharedGroundHeight);
            EditorUtility.SetDirty(loop);
        }

        private static void SetClipLoop(AnimationClip clip, bool loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            EditorUtility.SetDirty(clip);
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

        private static string RendererSignatureExcluding(
            Transform root,
            Transform excludedRoot)
        {
            return string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !renderer.transform.IsChildOf(excludedRoot))
                .OrderBy(
                    renderer => AnimationUtility.CalculateTransformPath(renderer.transform, root),
                    StringComparer.Ordinal)
                .Select(renderer =>
                    AnimationUtility.CalculateTransformPath(renderer.transform, root) + "|" +
                    renderer.GetType().FullName + "|" + renderer.enabled + "|" +
                    string.Join(",", renderer.sharedMaterials.Select(
                        material => material == null
                            ? "null"
                            : AssetDatabase.GetAssetPath(material)))));
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

        internal static GameObject RequireRuntimeTarget(string targetName)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Damage reaction runtime review requires Play Mode.");
            GameObject target = FindUnique(RequireScene(), targetName);
            RequireAnimator(target);
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

        internal static void WriteDeathRagdollFinalEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            int configuredBodyCount,
            int maximumDynamicBodyCount,
            float observedRagdollDuration,
            float resetPositionError,
            float resetRotationError)
        {
            if (configuredBodyCount < 15)
                throw new InvalidOperationException("Death ragdoll must configure the full body; actual bodies=" + configuredBodyCount);
            if (maximumDynamicBodyCount != configuredBodyCount)
                throw new InvalidOperationException(
                    "Death ragdoll did not activate every configured body. Configured=" + configuredBodyCount +
                    ", dynamic=" + maximumDynamicBodyCount);
            if (observedRagdollDuration < 0.48f || observedRagdollDuration > 0.65f)
                throw new InvalidOperationException(
                    "Death ragdoll duration differs from the approved 0.5-second interval: " +
                    observedRagdollDuration.ToString("0.######", CultureInfo.InvariantCulture));
            if (resetPositionError > 0.0001f || resetRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Death did not reset to its original root transform. PositionError=" +
                    resetPositionError.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", RotationError=" + resetRotationError.ToString("0.######", CultureInfo.InvariantCulture));

            string image = Absolute(DeathRagdollFinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(image) ??
                throw new InvalidOperationException("Death ragdoll final image directory is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Death natural Play Mode animation-to-ragdoll loop final contact sheet")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("captureMethod=Natural Play Mode playback and physics")
                .AppendLine("frame1=Original Death clip near completion")
                .AppendLine("frame2=Ragdoll start")
                .AppendLine("frame3=Ragdoll approximately 0.25 seconds")
                .AppendLine("frame4=Ragdoll approximately 0.45 seconds")
                .AppendLine("frame5=Immediate first-pose reset after ragdoll")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("retargetedClipLoop=False")
                .AppendLine("animatorAndRagdollSimultaneousDrive=False")
                .AppendLine("configuredBodyCount=" + configuredBodyCount)
                .AppendLine("maximumDynamicBodyCount=" + maximumDynamicBodyCount)
                .AppendLine("approvedRagdollDurationSeconds=0.5")
                .AppendLine("observedRagdollDurationSeconds=" +
                    observedRagdollDuration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("resetPositionError=" +
                    resetPositionError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("resetRotationErrorDegrees=" +
                    resetRotationError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("directVisualReviewPending=True");
            WriteText(DeathRagdollFinalReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void WriteDeathTergoRagdollEvidence(
            Texture2D sheet,
            bool final,
            int consoleErrorsBefore,
            int configuredBodyCount,
            int maximumDynamicBodyCount,
            bool usesDirectBonePhysics,
            float observedRagdollDuration,
            float maximumHipsDrop,
            float maximumHeadRise,
            float maximumRootDisplacement,
            float maximumGroundPenetration,
            float resetPositionError,
            float resetRotationError)
        {
            if (configuredBodyCount < 15)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll must configure the full body; actual bodies=" +
                    configuredBodyCount);
            if (maximumDynamicBodyCount != configuredBodyCount)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll did not activate every configured body. Configured=" +
                    configuredBodyCount + ", dynamic=" + maximumDynamicBodyCount);
            if (!usesDirectBonePhysics)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll must use direct bone rigidbodies.");
            if (observedRagdollDuration < 0.48f || observedRagdollDuration > 0.65f)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll duration differs from the approved 0.5 seconds: " +
                    observedRagdollDuration.ToString("0.######", CultureInfo.InvariantCulture));
            if (maximumHipsDrop > 0.12f)
                throw new InvalidOperationException(
                    "Death_Tergo fell below its lying-ground position. HipsDrop=" +
                    maximumHipsDrop.ToString("0.######", CultureInfo.InvariantCulture));
            if (maximumHeadRise > 0.08f)
                throw new InvalidOperationException(
                    "Death_Tergo head rose unnaturally during the grounded ragdoll. HeadRise=" +
                    maximumHeadRise.ToString("0.######", CultureInfo.InvariantCulture));
            if (maximumRootDisplacement > 0.0001f)
                throw new InvalidOperationException(
                    "Death_Tergo root moved during grounded ragdoll. RootDisplacement=" +
                    maximumRootDisplacement.ToString("0.######", CultureInfo.InvariantCulture));
            if (maximumGroundPenetration > 0.04f)
                throw new InvalidOperationException(
                    "Death_Tergo penetrated its physical ground. Penetration=" +
                    maximumGroundPenetration.ToString("0.######", CultureInfo.InvariantCulture));
            if (resetPositionError > 0.0001f || resetRotationError > 0.01f)
                throw new InvalidOperationException(
                    "Death_Tergo did not reset to its initial root transform. PositionError=" +
                    resetPositionError.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", RotationError=" +
                    resetRotationError.ToString("0.######", CultureInfo.InvariantCulture));

            string imagePath = final ? DeathTergoFinalImagePath : DeathTergoReviewImagePath;
            string reportPath = final ? DeathTergoFinalReportPath : DeathTergoReviewReportPath;
            string image = Absolute(imagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(image) ??
                throw new InvalidOperationException(
                    "Death_Tergo validation image directory is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());
            var report = new StringBuilder()
                .AppendLine("Death_Tergo natural Play Mode laying-to-ragdoll loop " +
                    (final ? "final" : "review") + " contact sheet")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("captureMethod=Natural Play Mode playback and physics")
                .AppendLine("row1=Original transfer laying clip at approximately 0.05, 0.33, 0.66, and 0.93 normalized time")
                .AppendLine("row2=Ragdoll start, ragdoll 0.25 seconds, ragdoll 0.45 seconds, and immediate first-pose reset")
                .AppendLine("sourceSha256=" + DeathTergoSource.Sha256)
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("retargetedClipLoop=False")
                .AppendLine("animatorAndRagdollSimultaneousDrive=False")
                .AppendLine("configuredBodyCount=" + configuredBodyCount)
                .AppendLine("maximumDynamicBodyCount=" + maximumDynamicBodyCount)
                .AppendLine("usesDirectBonePhysics=" + usesDirectBonePhysics)
                .AppendLine("approvedRagdollDurationSeconds=0.5")
                .AppendLine("observedRagdollDurationSeconds=" +
                    observedRagdollDuration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumHipsDropMeters=" +
                    maximumHipsDrop.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumHeadRiseMeters=" +
                    maximumHeadRise.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumRootDisplacementMeters=" +
                    maximumRootDisplacement.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("maximumGroundPenetrationMeters=" +
                    maximumGroundPenetration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("resetPositionError=" +
                    resetPositionError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("resetRotationErrorDegrees=" +
                    resetRotationError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("directVisualReviewPending=True");
            WriteText(reportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        internal static void WriteDeathGroundAndLaunchFinalEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            int deathBodyCount,
            int deathMaximumDynamicBodyCount,
            bool deathUsesDirectBonePhysics,
            float deathRagdollDuration,
            float deathMaximumHipsDrop,
            float deathMaximumHeadRise,
            float deathMaximumRootTransformDisplacement,
            float deathMaximumGroundPenetration,
            float deathResetPositionError,
            float deathResetRotationError,
            int launchBodyCount,
            int launchMaximumDynamicBodyCount,
            bool launchUsesDirectBonePhysics,
            float sharedGroundHeight,
            float launchStartedAtSeconds,
            float flightDuration,
            float landingHoldDuration,
            float maximumHorizontalDisplacement,
            float maximumHipsHeightGain,
            float maximumHipsRotationDegrees,
            float maximumAirborneHipsRotationDegrees,
            float maximumAirborneTorsoTiltDegrees,
            float launchMaximumAverageAngularSpeed,
            float launchMaximumJointAnchorSeparation,
            float launchMaximumRootTransformDisplacement,
            float launchMaximumGroundPenetration,
            float launchLandingHeadHeightAboveGround,
            Vector3 launchDirection,
            float launchResetPositionError,
            float launchResetRotationError)
        {
            string image = Absolute(DeathGroundAndLaunchFinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(image) ??
                throw new InvalidOperationException("Death ground/launch final image directory is unavailable."));
            File.WriteAllBytes(image, sheet.EncodeToPNG());

            if (deathBodyCount < 15 || launchBodyCount < 15)
                throw new InvalidOperationException("Both death ragdolls must configure the full body.");
            if (deathMaximumDynamicBodyCount != deathBodyCount ||
                launchMaximumDynamicBodyCount != launchBodyCount)
                throw new InvalidOperationException("A death ragdoll did not activate every configured body.");
            if (!deathUsesDirectBonePhysics || !launchUsesDirectBonePhysics)
                throw new InvalidOperationException(
                    "A death ragdoll is not driven directly by Rigidbody components on its bones.");
            if (deathRagdollDuration < 0.48f || deathRagdollDuration > 0.65f)
                throw new InvalidOperationException("Death ragdoll duration differs from 0.5 seconds.");
            if (deathMaximumHipsDrop > 0.15f)
                throw new InvalidOperationException(
                    "Death fell below its approved lying ground position. Drop=" +
                    deathMaximumHipsDrop.ToString("0.######", CultureInfo.InvariantCulture));
            if (deathMaximumHeadRise > 0.06f)
                throw new InvalidOperationException(
                    "Death head rose from floor collision during ragdoll activation. Rise=" +
                    deathMaximumHeadRise.ToString("0.######", CultureInfo.InvariantCulture));
            if (launchStartedAtSeconds < 0.28f || launchStartedAtSeconds > 0.38f)
                throw new InvalidOperationException("Death_FullBodyLaunch did not launch at 0.3 seconds.");
            if (maximumHorizontalDisplacement <= 0.25f ||
                maximumHorizontalDisplacement > FullBodyLaunchMaximumRangeMeters)
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch horizontal displacement is outside the approved 3-meter range: " +
                    maximumHorizontalDisplacement.ToString("0.######", CultureInfo.InvariantCulture));
            if (maximumHipsHeightGain <= 0.2f)
                throw new InvalidOperationException("Death_FullBodyLaunch did not receive an upward physical impulse.");
            if (maximumAirborneTorsoTiltDegrees < 45f)
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch remained too upright during flight. TorsoTilt=" +
                    maximumAirborneTorsoTiltDegrees.ToString("0.######", CultureInfo.InvariantCulture));
            if (launchMaximumAverageAngularSpeed <= 0.8f)
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch did not show physical rotation during flight. AngularSpeed=" +
                    launchMaximumAverageAngularSpeed.ToString("0.######", CultureInfo.InvariantCulture));
            if (launchMaximumJointAnchorSeparation > 0.06f)
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch skeleton joints separated excessively. Separation=" +
                    launchMaximumJointAnchorSeparation.ToString("0.######", CultureInfo.InvariantCulture));
            if (flightDuration <= 0.3f || flightDuration > 6f)
                throw new InvalidOperationException("Death_FullBodyLaunch did not reach a stable ground landing.");
            if (launchLandingHeadHeightAboveGround > 0.55f)
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch froze before sprawling onto the ground. HeadHeight=" +
                    launchLandingHeadHeightAboveGround.ToString("0.######", CultureInfo.InvariantCulture));
            if (landingHoldDuration < 0.48f || landingHoldDuration > 0.65f)
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch landing hold differs from 0.5 seconds.");
            if (launchDirection.sqrMagnitude < 0.9f)
                throw new InvalidOperationException("Death_FullBodyLaunch random direction was not recorded.");
            if (deathMaximumRootTransformDisplacement > 0.0001f ||
                launchMaximumRootTransformDisplacement > 0.0001f)
                throw new InvalidOperationException(
                    "A death root Transform moved while its bones were under Rigidbody control.");
            if (deathMaximumGroundPenetration > 0.04f || launchMaximumGroundPenetration > 0.04f)
                throw new InvalidOperationException("A death ragdoll penetrated the shared physical ground.");
            if (deathResetPositionError > 0.0001f || deathResetRotationError > 0.01f ||
                launchResetPositionError > 0.0001f || launchResetRotationError > 0.01f)
                throw new InvalidOperationException("A death ragdoll did not reset to its original root Transform.");

            var report = new StringBuilder()
                .AppendLine("Death shared-ground and Death_FullBodyLaunch natural Play Mode final contact sheet")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("captureMethod=Natural Play Mode playback and Rigidbody physics")
                .AppendLine("row1=Death animation end, grounded ragdoll, and first-pose reset")
                .AppendLine("row2=Death_FullBodyLaunch initial pose, 0.3-second delay, flight, landing hold, and reset")
                .AppendLine("sharedPhysicalGround=True")
                .AppendLine("sharedGroundHeight=" + sharedGroundHeight.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("animatorAndRagdollSimultaneousDrive=False")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("deathConfiguredBodyCount=" + deathBodyCount)
                .AppendLine("deathMaximumDynamicBodyCount=" + deathMaximumDynamicBodyCount)
                .AppendLine("deathUsesDirectBoneRigidbodies=" + deathUsesDirectBonePhysics)
                .AppendLine("deathObservedRagdollDurationSeconds=" +
                    deathRagdollDuration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("deathMaximumHipsDropMeters=" +
                    deathMaximumHipsDrop.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("deathMaximumHeadRiseMeters=" +
                    deathMaximumHeadRise.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("deathMaximumRootTransformDisplacementMeters=" +
                    deathMaximumRootTransformDisplacement.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("deathMaximumGroundPenetrationMeters=" +
                    deathMaximumGroundPenetration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("deathResetPositionError=" +
                    deathResetPositionError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("deathResetRotationErrorDegrees=" +
                    deathResetRotationError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchConfiguredBodyCount=" + launchBodyCount)
                .AppendLine("launchMaximumDynamicBodyCount=" + launchMaximumDynamicBodyCount)
                .AppendLine("launchUsesDirectBoneRigidbodies=" + launchUsesDirectBonePhysics)
                .AppendLine("launchStartedAtSeconds=" +
                    launchStartedAtSeconds.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchFlightDurationSeconds=" +
                    flightDuration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchLandingHoldDurationSeconds=" +
                    landingHoldDuration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumHorizontalDisplacementMeters=" +
                    maximumHorizontalDisplacement.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumHipsHeightGainMeters=" +
                    maximumHipsHeightGain.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumHipsRotationDegrees=" +
                    maximumHipsRotationDegrees.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumAirborneHipsRotationDegrees=" +
                    maximumAirborneHipsRotationDegrees.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumAirborneTorsoTiltDegrees=" +
                    maximumAirborneTorsoTiltDegrees.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumAverageAngularSpeed=" +
                    launchMaximumAverageAngularSpeed.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumJointAnchorSeparationMeters=" +
                    launchMaximumJointAnchorSeparation.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumRootTransformDisplacementMeters=" +
                    launchMaximumRootTransformDisplacement.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchMaximumGroundPenetrationMeters=" +
                    launchMaximumGroundPenetration.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchLandingHeadHeightAboveGroundMeters=" +
                    launchLandingHeadHeightAboveGround.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchDirection=" + launchDirection.ToString("F6"))
                .AppendLine("launchResetPositionError=" +
                    launchResetPositionError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("launchResetRotationErrorDegrees=" +
                    launchResetRotationError.ToString("0.######", CultureInfo.InvariantCulture))
                .AppendLine("directVisualReviewPending=True");
            WriteText(DeathGroundAndLaunchFinalReportPath, report.ToString());
            LightsaberSetupTools.RequireNoNewUnityConsoleErrors(consoleErrorsBefore);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(label + " differs. Expected=" + expected + ", Actual=" + actual);
        }

        private readonly struct BodySampleDimensions
        {
            internal BodySampleDimensions(
                Vector3 center,
                float height,
                float radiusX,
                float radiusZ)
            {
                Center = center;
                Height = height;
                RadiusX = radiusX;
                RadiusZ = radiusZ;
            }

            internal Vector3 Center { get; }
            internal float Height { get; }
            internal float RadiusX { get; }
            internal float RadiusZ { get; }
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
    internal static class StunTwistAnimationPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.StunTwistAnimation.Pending";
        private const string StateKey = "Bellerophon.StunTwistAnimation.State";
        private const string FailureKey = "Bellerophon.StunTwistAnimation.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.StunTwistAnimation.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string ControllerPath =
            "Assets/_Project/Animations/PlayerStatusEffects/StunTwist/Stun_Twist.controller";
        private const string StateName = "StunTwist_Source";
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static float maximumNormalizedTime;

        static StunTwistAnimationPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Stun_Twist animation review must start in Edit Mode.");
            PlayerDamageReactionAnimationSetupTools.InspectStunTwistAnimation();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Stun_Twist animation Play Mode capture has no pending state.");
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
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before Stun_Twist review completed.");
                    CaptureNaturalLoop();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                PlayerDamageReactionAnimationSetupTools.InspectStunTwistAnimation();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Stun_Twist exact Mixamo animation completed a natural Play Mode loop and returned to Edit Mode.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            target = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget(
                "Stun_Twist",
                ControllerPath);
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Stun_Twist Animator is missing in Play Mode.");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            startedAt = EditorApplication.timeSinceStartup;
            maximumNormalizedTime = 0f;
        }

        private static void CaptureNaturalLoop()
        {
            if (EditorApplication.timeSinceStartup - startedAt > 30d)
                throw new TimeoutException(
                    "Stun_Twist natural playback capture exceeded 30 seconds.");
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(StateName))
                return;
            maximumNormalizedTime = Mathf.Max(
                maximumNormalizedTime,
                state.normalizedTime);
            float threshold = Panels.Count == 0 ? 0.05f :
                Panels.Count == 1 ? 0.30f :
                Panels.Count == 2 ? 0.55f :
                Panels.Count == 3 ? 0.80f : 1.05f;
            if (state.normalizedTime < threshold)
                return;
            Panels.Add(RenderTarget());
            RequireRootUnchanged();
            if (Panels.Count == 5)
                FinishCapture();
        }

        private static Texture2D RenderTarget()
        {
            const int width = 520;
            const int height = 620;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Stun_Twist has no enabled renderer in Play Mode.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject cameraObject = new GameObject(
                "StunTwistAnimation_ReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "StunTwistAnimation_ReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherRendererStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer otherRenderer in otherRenderers)
                    otherRenderer.forceRenderingOff = true;

                Vector3 viewDirection = (
                    target.transform.forward * 0.96f +
                    target.transform.right * 0.12f +
                    Vector3.up * 0.03f).normalized;
                float distance = Mathf.Max(4f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.16f,
                    bounds.extents.x / aspect * 1.16f,
                    0.8f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff =
                            otherRendererStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FinishCapture()
        {
            RequireRootUnchanged();
            Texture2D sheet = CombinePanels();
            try
            {
                PlayerDamageReactionAnimationSetupTools
                    .WriteStunTwistAnimationReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != 5)
                throw new InvalidOperationException(
                    "Stun_Twist review requires exactly five natural frames.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * Panels.Count,
                height,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(
                    index * width,
                    0,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootUnchanged()
        {
            if (Vector3.Distance(
                    initialLocalPosition,
                    target.transform.localPosition) > 0.0001f ||
                Quaternion.Angle(
                    initialLocalRotation,
                    target.transform.localRotation) > 0.01f ||
                Vector3.Distance(
                    initialLocalScale,
                    target.transform.localScale) > 0.0001f)
                throw new InvalidOperationException(
                    "Stun_Twist root Transform changed during natural playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Stun_Twist natural Play Mode animation review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class StunTwistApprovedElectricArcPlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.StunTwistApprovedElectricArc.Pending";
        private const string StateKey =
            "Bellerophon.StunTwistApprovedElectricArc.State";
        private const string FailureKey =
            "Bellerophon.StunTwistApprovedElectricArc.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.StunTwistApprovedElectricArc.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string ControllerPath =
            "Assets/_Project/Animations/PlayerStatusEffects/StunTwist/Stun_Twist.controller";
        private const string StateName = "StunTwist_Source";
        private const string EffectTypeName =
            "Bellerophon.ArtSamples.StunTwistElectricArcUnitySample";
        private static readonly float[] CaptureThresholds =
        {
            0.08f, 0.28f, 0.48f, 0.68f, 0.88f, 1.05f
        };
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static readonly HashSet<int> VisiblePatterns = new HashSet<int>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static Component effectController;
        private static Transform effectRoot;
        private static LineRenderer[] coreLines;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static float maximumNormalizedTime;
        private static int maximumVisibleGroups;

        static StunTwistApprovedElectricArcPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric-arc review must start in Edit Mode.");
            PlayerDamageReactionAnimationSetupTools
                .InspectStunTwistApprovedElectricArcSample();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            VisiblePatterns.Clear();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric-arc review has no pending state.");
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
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before the approved Stun_Twist electric-arc review completed.");
                    CaptureNaturalLoop();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                PlayerDamageReactionAnimationSetupTools
                    .InspectStunTwistApprovedElectricArcSample();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Approved cyan-white Stun_Twist electric arcs completed a natural Play Mode loop with front/rear evidence.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            target = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget(
                "Stun_Twist",
                ControllerPath);
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Stun_Twist Animator is missing in Play Mode.");
            Component[] controllers = target.GetComponentsInChildren<Component>(true)
                .Where(component => component != null &&
                    component.GetType().FullName == EffectTypeName)
                .ToArray();
            if (controllers.Length != 1)
                throw new InvalidOperationException(
                    "Stun_Twist must have exactly one approved electric-arc controller in Play Mode; actual=" +
                    controllers.Length);
            effectController = controllers[0];
            effectRoot = effectController.transform;
            effectController.GetType()
                .GetMethod("ResumeAnimatedPreview")?
                .Invoke(effectController, null);
            coreLines = effectRoot.GetComponentsInChildren<LineRenderer>(true)
                .Where(line => line.sharedMaterial != null &&
                    line.sharedMaterial.name.IndexOf(
                        "WhiteCore",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(line => line.name, StringComparer.Ordinal)
                .ToArray();
            if (coreLines.Length != 8)
                throw new InvalidOperationException(
                    "Approved Stun_Twist sample must expose eight white-core arc groups in Play Mode; actual=" +
                    coreLines.Length);

            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            startedAt = EditorApplication.timeSinceStartup;
            maximumNormalizedTime = 0f;
            maximumVisibleGroups = 0;
            VisiblePatterns.Clear();
        }

        private static void CaptureNaturalLoop()
        {
            if (EditorApplication.timeSinceStartup - startedAt > 30d)
                throw new TimeoutException(
                    "Approved Stun_Twist electric-arc playback review exceeded 30 seconds.");
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(StateName))
                return;
            maximumNormalizedTime = Mathf.Max(
                maximumNormalizedTime,
                state.normalizedTime);
            if (Panels.Count >= CaptureThresholds.Length ||
                state.normalizedTime < CaptureThresholds[Panels.Count])
                return;

            RecordVisiblePattern();
            Panels.Add(RenderTarget(Panels.Count >= 3));
            RequireRootUnchanged();
            if (Panels.Count == CaptureThresholds.Length)
                FinishCapture();
        }

        private static void RecordVisiblePattern()
        {
            int pattern = 0;
            int visible = 0;
            for (int index = 0; index < coreLines.Length; index++)
            {
                if (!coreLines[index].enabled || coreLines[index].startColor.a <= 0.05f)
                    continue;
                pattern |= 1 << index;
                visible++;
            }
            VisiblePatterns.Add(pattern);
            maximumVisibleGroups = Mathf.Max(maximumVisibleGroups, visible);
        }

        private static Texture2D RenderTarget(bool rear)
        {
            const int width = 500;
            const int height = 620;
            Renderer[] bodyRenderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled &&
                    !renderer.transform.IsChildOf(effectRoot))
                .ToArray();
            if (bodyRenderers.Length == 0)
                throw new InvalidOperationException(
                    "Stun_Twist has no enabled body renderer in Play Mode.");
            Bounds bodyBounds = bodyRenderers[0].bounds;
            foreach (Renderer renderer in bodyRenderers.Skip(1))
                bodyBounds.Encapsulate(renderer.bounds);
            Transform head = RequireNamedBone("Head");
            Transform hips = RequireNamedBone("Hips");
            Transform leftUpperArm = RequireNamedBone("LeftArm");
            Transform rightUpperArm = RequireNamedBone("RightArm");
            Vector3 torsoCenter = (head.position + hips.position) * 0.5f +
                Vector3.up * 0.05f;
            float torsoHalfHeight = Mathf.Max(
                Vector3.Distance(head.position, hips.position) * 0.68f,
                0.58f);
            float torsoHalfWidth = Mathf.Max(
                Vector3.Distance(leftUpperArm.position, rightUpperArm.position) * 0.82f,
                0.46f);

            GameObject cameraObject = new GameObject(
                "StunTwistApprovedArc_ReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "StunTwistApprovedArc_ReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherRendererStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer otherRenderer in otherRenderers)
                    otherRenderer.forceRenderingOff = true;

                float side = rear ? -1f : 1f;
                Vector3 viewDirection = (
                    target.transform.forward * 0.98f * side +
                    target.transform.right * 0.10f +
                    Vector3.up * 0.02f).normalized;
                float distance = Mathf.Max(3.4f, bodyBounds.extents.magnitude * 2.6f);
                camera.transform.position = torsoCenter + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    torsoCenter - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    torsoHalfHeight * 1.16f,
                    torsoHalfWidth / aspect * 1.16f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bodyBounds.extents.magnitude * 4f;
                camera.allowHDR = true;
                camera.allowMSAA = true;

                light.type = LightType.Directional;
                light.intensity = 1.05f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff =
                            otherRendererStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Transform RequireNamedBone(string boneName)
        {
            Transform[] matches = target.GetComponentsInChildren<Transform>(true)
                .Where(candidate => candidate.name == boneName &&
                    !candidate.IsChildOf(effectRoot))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Stun_Twist requires exactly one " + boneName +
                    " bone for direct visual review; actual=" + matches.Length);
            return matches[0];
        }

        private static void FinishCapture()
        {
            RequireRootUnchanged();
            Texture2D sheet = CombinePanels();
            try
            {
                PlayerDamageReactionAnimationSetupTools
                    .WriteStunTwistApprovedElectricArcReviewEvidence(
                        sheet,
                        SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                        maximumNormalizedTime,
                        VisiblePatterns.Count,
                        maximumVisibleGroups,
                        target.transform.localPosition - initialLocalPosition,
                        Quaternion.Angle(
                            initialLocalRotation,
                            target.transform.localRotation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != CaptureThresholds.Length)
                throw new InvalidOperationException(
                    "Approved Stun_Twist electric-arc review requires six natural frames.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * 3,
                height * 2,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(
                    index % 3 * width,
                    index < 3 ? height : 0,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootUnchanged()
        {
            if (Vector3.Distance(
                    initialLocalPosition,
                    target.transform.localPosition) > 0.0001f ||
                Quaternion.Angle(
                    initialLocalRotation,
                    target.transform.localRotation) > 0.01f ||
                Vector3.Distance(
                    initialLocalScale,
                    target.transform.localScale) > 0.0001f)
                throw new InvalidOperationException(
                    "Stun_Twist root Transform changed during approved VFX playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Approved Stun_Twist electric-arc Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            VisiblePatterns.Clear();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            effectController = null;
            effectRoot = null;
            coreLines = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
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

    [InitializeOnLoad]
    internal static class DeathRagdollLoopPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.DeathRagdollLoop.Pending";
        private const string StateKey = "Bellerophon.DeathRagdollLoop.State";
        private const string FailureKey = "Bellerophon.DeathRagdollLoop.Failure";
        private const string ConsoleErrorsBeforeKey = "Bellerophon.DeathRagdollLoop.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string ControllerPath =
            "Assets/_Project/Animations/PlayerDamageReactions/Death.controller";
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static DeathRagdollLoop loop;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static int maximumDynamicBodyCount;
        private static double captureStarted;

        static DeathRagdollLoopPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Death ragdoll final review must start in Edit Mode.");
            PlayerDamageReactionAnimationSetupTools.InspectDeathRagdollLoop();
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
                throw new InvalidOperationException("Death ragdoll Play Mode capture has no pending state.");
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
                        throw new InvalidOperationException("Play Mode ended before Death ragdoll review completed.");
                    CaptureNaturalPhases();
                    return;
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                PlayerDamageReactionAnimationSetupTools.InspectDeathRagdollLoop();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Death original animation, 0.5-second full-body ragdoll, and immediate first-pose reset were captured in natural Play Mode.");
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
            target = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget("Death", ControllerPath);
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException("Death Animator is missing in Play Mode.");
            loop = target.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException("DeathRagdollLoop is missing in Play Mode.");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            maximumDynamicBodyCount = 0;
            captureStarted = EditorApplication.timeSinceStartup;
        }

        private static void CaptureNaturalPhases()
        {
            if (EditorApplication.timeSinceStartup - captureStarted > 20d)
                throw new TimeoutException("Death ragdoll natural playback capture exceeded 20 seconds.");

            maximumDynamicBodyCount = Mathf.Max(
                maximumDynamicBodyCount,
                loop.ActiveDynamicBodyCount);

            if (Panels.Count == 0 && !loop.IsRagdollActive)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(loop.StateName) && state.normalizedTime >= 0.90f)
                    CapturePanel();
            }
            else if (Panels.Count == 1 && loop.IsRagdollActive && loop.RagdollElapsed >= 0.02f)
            {
                CapturePanel();
            }
            else if (Panels.Count == 2 && loop.IsRagdollActive && loop.RagdollElapsed >= 0.25f)
            {
                CapturePanel();
            }
            else if (Panels.Count == 3 && loop.IsRagdollActive && loop.RagdollElapsed >= 0.45f)
            {
                CapturePanel();
            }
            else if (Panels.Count == 4 && loop.CompletedCycleCount >= 1 && !loop.IsRagdollActive)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(loop.StateName) && state.normalizedTime <= 0.20f)
                {
                    CapturePanel();
                    FinishCapture();
                }
            }
        }

        private static void CapturePanel()
        {
            Panels.Add(ShipRepairSharedAnimationTools.RenderNaturalTarget(target));
        }

        private static void FinishCapture()
        {
            RequireRootReset();
            Texture2D sheet = CombinePanels();
            try
            {
                PlayerDamageReactionAnimationSetupTools.WriteDeathRagdollFinalEvidence(
                    sheet,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    loop.ConfiguredBodyCount,
                    maximumDynamicBodyCount,
                    loop.LastRagdollDuration,
                    loop.LastResetPositionError,
                    loop.LastResetRotationError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != 5)
                throw new InvalidOperationException("Death ragdoll final capture requires exactly five natural frames.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(width * Panels.Count, height, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255), sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(index * width, 0, width, height, Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootReset()
        {
            Transform root = target.transform;
            if (Vector3.Distance(initialLocalPosition, root.localPosition) > 0.0001f ||
                Quaternion.Angle(initialLocalRotation, root.localRotation) > 0.01f ||
                Vector3.Distance(initialLocalScale, root.localScale) > 0.0001f)
                throw new InvalidOperationException("Death root Transform did not reset after ragdoll.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey, "Death ragdoll natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            loop = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class DeathTergoLayingRagdollPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.DeathTergoRagdoll.Pending";
        private const string StateKey = "Bellerophon.DeathTergoRagdoll.State";
        private const string FailureKey = "Bellerophon.DeathTergoRagdoll.Failure";
        private const string FinalKey = "Bellerophon.DeathTergoRagdoll.Final";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.DeathTergoRagdoll.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string ControllerPath =
            "Assets/_Project/Animations/PlayerDamageReactions/Death_Tergo.controller";
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static DeathRagdollLoop loop;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static Bounds stableReviewBounds;
        private static int maximumDynamicBodyCount;
        private static double captureStarted;

        static DeathTergoLayingRagdollPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

        internal static void Start(bool final, Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll review must start in Edit Mode.");
            PlayerDamageReactionAnimationSetupTools.InspectDeathTergoLayingRagdollLoop();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(FinalKey, final);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Death_Tergo ragdoll Play Mode capture has no pending state.");
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
                        throw new InvalidOperationException(
                            "Play Mode ended before Death_Tergo ragdoll review completed.");
                    CaptureNaturalPhases();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                PlayerDamageReactionAnimationSetupTools.InspectDeathTergoLayingRagdollLoop();
                bool final = SessionState.GetBool(FinalKey, false);
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Death_Tergo exact laying animation, 0.5-second grounded full-body ragdoll, " +
                    "and immediate first-pose reset were captured in natural Play Mode (" +
                    (final ? "final" : "review") + ").");
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
            target = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget(
                "Death_Tergo", ControllerPath);
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Death_Tergo Animator is missing in Play Mode.");
            loop = target.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException(
                    "Death_Tergo DeathRagdollLoop is missing in Play Mode.");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            stableReviewBounds = loop.DirectBonePhysicsBounds;
            maximumDynamicBodyCount = 0;
            captureStarted = EditorApplication.timeSinceStartup;
        }

        private static void CaptureNaturalPhases()
        {
            if (EditorApplication.timeSinceStartup - captureStarted > 30d)
                throw new TimeoutException(
                    "Death_Tergo natural playback capture exceeded 30 seconds.");

            maximumDynamicBodyCount = Mathf.Max(
                maximumDynamicBodyCount,
                loop.ActiveDynamicBodyCount);

            if (Panels.Count <= 3 && !loop.IsRagdollActive)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(loop.StateName)) return;
                float threshold = Panels.Count == 0 ? 0.03f :
                    Panels.Count == 1 ? 0.33f :
                    Panels.Count == 2 ? 0.66f : 0.93f;
                if (state.normalizedTime >= threshold)
                    CapturePanel();
            }
            else if (Panels.Count == 4 && loop.IsRagdollActive && loop.RagdollElapsed >= 0.02f)
                CapturePanel();
            else if (Panels.Count == 5 && loop.IsRagdollActive && loop.RagdollElapsed >= 0.25f)
                CapturePanel();
            else if (Panels.Count == 6 && loop.IsRagdollActive && loop.RagdollElapsed >= 0.45f)
                CapturePanel();
            else if (Panels.Count == 7 && loop.CompletedCycleCount >= 1 && !loop.IsRagdollActive)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(loop.StateName) && state.normalizedTime <= 0.20f)
                {
                    CapturePanel();
                    FinishCapture();
                }
            }
        }

        private static void CapturePanel() => Panels.Add(RenderReviewTarget());

        private static Texture2D RenderReviewTarget()
        {
            const int width = 620;
            const int height = 500;
            const int reviewLayer = 31;
            Bounds bounds = stableReviewBounds;
            GameObject cameraObject = new GameObject(
                "DeathTergo_ReviewCamera", typeof(Camera));
            GameObject lightObject = new GameObject(
                "DeathTergo_ReviewLight", typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Transform[] targetTransforms = target.GetComponentsInChildren<Transform>(true);
            int[] originalLayers = targetTransforms.Select(item => item.gameObject.layer).ToArray();
            try
            {
                foreach (Transform item in targetTransforms)
                    item.gameObject.layer = reviewLayer;
                Vector3 viewDirection = (
                    target.transform.forward * 0.85f +
                    Vector3.up * 1.15f +
                    target.transform.right * 0.35f).normalized;
                float distance = Mathf.Max(4f, bounds.extents.magnitude * 3f);
                camera.transform.position = bounds.center + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                float maximumVertical = 0f;
                float maximumHorizontal = 0f;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(
                        bounds.extents,
                        new Vector3(x, y, z));
                    Vector3 offset = corner - bounds.center;
                    maximumVertical = Mathf.Max(
                        maximumVertical,
                        Mathf.Abs(Vector3.Dot(offset, camera.transform.up)));
                    maximumHorizontal = Mathf.Max(
                        maximumHorizontal,
                        Mathf.Abs(Vector3.Dot(offset, camera.transform.right)));
                }
                float aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    0.8f,
                    maximumVertical * 1.20f,
                    maximumHorizontal / aspect * 1.20f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.cullingMask = 1 << reviewLayer;

                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.cullingMask = 1 << reviewLayer;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < targetTransforms.Length; index++)
                    if (targetTransforms[index] != null)
                        targetTransforms[index].gameObject.layer = originalLayers[index];
                if (renderTexture != null) RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FinishCapture()
        {
            RequireRootReset();
            Texture2D sheet = CombinePanels();
            try
            {
                PlayerDamageReactionAnimationSetupTools.WriteDeathTergoRagdollEvidence(
                    sheet,
                    SessionState.GetBool(FinalKey, false),
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    loop.ConfiguredBodyCount,
                    maximumDynamicBodyCount,
                    loop.UsesDirectBoneRigidbodies,
                    loop.LastRagdollDuration,
                    loop.LastMaximumHipsDrop,
                    loop.LastMaximumHeadRise,
                    loop.LastMaximumRootTransformDisplacement,
                    loop.LastMaximumGroundPenetration,
                    loop.LastResetPositionError,
                    loop.LastResetRotationError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != 8)
                throw new InvalidOperationException(
                    "Death_Tergo review requires exactly eight natural frames.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(width * 4, height * 2, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255), sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
            {
                int column = index % 4;
                int row = 1 - index / 4;
                sheet.SetPixels32(
                    column * width,
                    row * height,
                    width,
                    height,
                    Panels[index].GetPixels32());
            }
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootReset()
        {
            Transform root = target.transform;
            if (Vector3.Distance(initialLocalPosition, root.localPosition) > 0.0001f ||
                Quaternion.Angle(initialLocalRotation, root.localRotation) > 0.01f ||
                Vector3.Distance(initialLocalScale, root.localScale) > 0.0001f)
                throw new InvalidOperationException(
                    "Death_Tergo root Transform did not reset after ragdoll.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Death_Tergo ragdoll natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            loop = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseBool(FinalKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class DeathRagdollGroundAndFullBodyLaunchPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.DeathGroundAndLaunch.Pending";
        private const string StateKey = "Bellerophon.DeathGroundAndLaunch.State";
        private const string FailureKey = "Bellerophon.DeathGroundAndLaunch.Failure";
        private const string ConsoleErrorsBeforeKey = "Bellerophon.DeathGroundAndLaunch.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string ControllerPath =
            "Assets/_Project/Animations/PlayerDamageReactions/Death.controller";
        private static readonly List<Texture2D> DeathPanels = new List<Texture2D>();
        private static readonly List<Texture2D> LaunchPanels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject death;
        private static GameObject launch;
        private static Animator deathAnimator;
        private static DeathRagdollLoop deathLoop;
        private static DeathRagdollLoop launchLoop;
        private static Vector3 deathInitialLocalPosition;
        private static Quaternion deathInitialLocalRotation;
        private static Vector3 launchInitialLocalPosition;
        private static Quaternion launchInitialLocalRotation;
        private static Vector3 deathOrigin;
        private static Vector3 launchOrigin;
        private static int deathMaximumDynamicBodyCount;
        private static int launchMaximumDynamicBodyCount;
        private static float launchStartedAtSeconds;
        private static float launchFlightDuration;
        private static float launchLandingHoldDuration;
        private static float launchMaximumHorizontalDisplacement;
        private static float launchMaximumHipsHeightGain;
        private static float launchMaximumHipsRotationDegrees;
        private static float launchMaximumAirborneHipsRotationDegrees;
        private static float launchMaximumAirborneTorsoTiltDegrees;
        private static float launchMaximumAverageAngularSpeed;
        private static float launchMaximumJointAnchorSeparation;
        private static float launchMaximumRootTransformDisplacement;
        private static float launchMaximumGroundPenetration;
        private static float launchLandingHeadHeightAboveGround;
        private static Vector3 launchDirection;
        private static float launchResetPositionError;
        private static float launchResetRotationError;
        private static double captureStarted;

        static DeathRagdollGroundAndFullBodyLaunchPlayModeCapture()
        {
        }

        internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Death ground/full-body launch review must start in Edit Mode.");
            PlayerDamageReactionAnimationSetupTools.InspectDeathRagdollGroundAndFullBodyLaunch();
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
                throw new InvalidOperationException(
                    "Death ground/full-body launch review has no pending state.");
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
                        throw new InvalidOperationException(
                            "Play Mode ended before Death ground/full-body launch review completed.");
                    CaptureNaturalPhases();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }

                PlayerDamageReactionAnimationSetupTools.InspectDeathRagdollGroundAndFullBodyLaunch();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Death shared-ground behavior and Death_FullBodyLaunch physical loop were captured in natural Play Mode.");
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
            death = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget("Death", ControllerPath);
            launch = PlayerDamageReactionAnimationSetupTools.RequireRuntimeTarget("Death_FullBodyLaunch");
            deathAnimator = death.GetComponent<Animator>() ??
                throw new InvalidOperationException("Death Animator is missing in Play Mode.");
            deathLoop = death.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException("Death DeathRagdollLoop is missing in Play Mode.");
            launchLoop = launch.GetComponent<DeathRagdollLoop>() ??
                throw new InvalidOperationException(
                    "Death_FullBodyLaunch DeathRagdollLoop is missing in Play Mode.");
            deathInitialLocalPosition = death.transform.localPosition;
            deathInitialLocalRotation = death.transform.localRotation;
            launchInitialLocalPosition = launch.transform.localPosition;
            launchInitialLocalRotation = launch.transform.localRotation;
            deathOrigin = death.transform.position;
            launchOrigin = launch.transform.position;
            deathMaximumDynamicBodyCount = 0;
            launchMaximumDynamicBodyCount = 0;
            launchStartedAtSeconds = 0f;
            launchFlightDuration = 0f;
            launchLandingHoldDuration = 0f;
            launchMaximumHorizontalDisplacement = 0f;
            launchMaximumHipsHeightGain = 0f;
            launchMaximumHipsRotationDegrees = 0f;
            launchMaximumAirborneHipsRotationDegrees = 0f;
            launchMaximumAirborneTorsoTiltDegrees = 0f;
            launchMaximumAverageAngularSpeed = 0f;
            launchMaximumJointAnchorSeparation = 0f;
            launchMaximumRootTransformDisplacement = 0f;
            launchMaximumGroundPenetration = 0f;
            launchLandingHeadHeightAboveGround = 0f;
            launchDirection = Vector3.zero;
            launchResetPositionError = 0f;
            launchResetRotationError = 0f;
            captureStarted = EditorApplication.timeSinceStartup;
        }

        private static void CaptureNaturalPhases()
        {
            if (EditorApplication.timeSinceStartup - captureStarted > 30d)
                throw new TimeoutException(
                    "Death ground/full-body launch natural playback capture exceeded 30 seconds.");

            deathMaximumDynamicBodyCount = Mathf.Max(
                deathMaximumDynamicBodyCount, deathLoop.ActiveDynamicBodyCount);
            launchMaximumDynamicBodyCount = Mathf.Max(
                launchMaximumDynamicBodyCount, launchLoop.ActiveDynamicBodyCount);
            CaptureDeathPhases();
            CaptureLaunchPhases();
            if (DeathPanels.Count == 5 && LaunchPanels.Count == 6)
                FinishCapture();
        }

        private static void CaptureDeathPhases()
        {
            if (DeathPanels.Count == 0 && !deathLoop.IsRagdollActive)
            {
                AnimatorStateInfo state = deathAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(deathLoop.StateName) && state.normalizedTime >= 0.90f)
                    CaptureDeathPanel();
            }
            else if (DeathPanels.Count == 1 && deathLoop.IsRagdollActive &&
                     deathLoop.RagdollElapsed >= 0.02f)
                CaptureDeathPanel();
            else if (DeathPanels.Count == 2 && deathLoop.IsRagdollActive &&
                     deathLoop.RagdollElapsed >= 0.25f)
                CaptureDeathPanel();
            else if (DeathPanels.Count == 3 && deathLoop.IsRagdollActive &&
                     deathLoop.RagdollElapsed >= 0.45f)
                CaptureDeathPanel();
            else if (DeathPanels.Count == 4 && deathLoop.CompletedCycleCount >= 1 &&
                     !deathLoop.IsRagdollActive)
            {
                AnimatorStateInfo state = deathAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(deathLoop.StateName) && state.normalizedTime <= 0.20f)
                    CaptureDeathPanel();
            }
        }

        private static void CaptureLaunchPhases()
        {
            if (LaunchPanels.Count == 0 && launchLoop.CompletedCycleCount == 0 &&
                !launchLoop.IsRagdollActive && launchLoop.CycleElapsed >= 0.03f)
                CaptureLaunchPanel();
            else if (LaunchPanels.Count == 1 && !launchLoop.IsRagdollActive &&
                     launchLoop.CycleElapsed >= 0.25f)
                CaptureLaunchPanel();
            else if (LaunchPanels.Count == 2 && launchLoop.IsRagdollActive &&
                     !launchLoop.IsLandingHold && launchLoop.RagdollElapsed >= 0.08f)
                CaptureLaunchPanel();
            else if (LaunchPanels.Count == 3 && launchLoop.IsRagdollActive &&
                     !launchLoop.IsLandingHold &&
                     launchLoop.LastMaximumAirborneTorsoTiltDegrees >= 45f)
                CaptureLaunchPanel();
            else if (LaunchPanels.Count == 4 && launchLoop.IsLandingHold &&
                     launchLoop.LandingHoldElapsed >= 0.05f)
                CaptureLaunchPanel();
            else if (LaunchPanels.Count == 5 && launchLoop.CompletedCycleCount >= 1 &&
                     !launchLoop.IsRagdollActive && launchLoop.CycleElapsed <= 0.18f)
            {
                CaptureLaunchPanel();
                SnapshotLaunchMetrics();
            }
        }

        private static void CaptureDeathPanel() =>
            DeathPanels.Add(RenderFixedTarget(death, deathOrigin, deathLoop.SharedGroundHeight));

        private static void CaptureLaunchPanel() =>
            LaunchPanels.Add(RenderFixedTarget(launch, launchOrigin, launchLoop.SharedGroundHeight));

        private static void SnapshotLaunchMetrics()
        {
            launchStartedAtSeconds = launchLoop.LastLaunchStartedAtSeconds;
            launchFlightDuration = launchLoop.LastFlightDuration;
            launchLandingHoldDuration = launchLoop.LastLandingHoldDuration;
            launchMaximumHorizontalDisplacement = launchLoop.LastMaximumHorizontalDisplacement;
            launchMaximumHipsHeightGain = launchLoop.LastMaximumHipsHeightGain;
            launchMaximumHipsRotationDegrees = launchLoop.LastMaximumHipsRotationDegrees;
            launchMaximumAirborneHipsRotationDegrees = launchLoop.LastMaximumAirborneHipsRotationDegrees;
            launchMaximumAirborneTorsoTiltDegrees = launchLoop.LastMaximumAirborneTorsoTiltDegrees;
            launchMaximumAverageAngularSpeed = launchLoop.LastMaximumAverageAngularSpeed;
            launchMaximumJointAnchorSeparation = launchLoop.LastMaximumJointAnchorSeparation;
            launchMaximumRootTransformDisplacement = launchLoop.LastMaximumRootTransformDisplacement;
            launchMaximumGroundPenetration = launchLoop.LastMaximumGroundPenetration;
            launchLandingHeadHeightAboveGround = launchLoop.LastLandingHeadHeightAboveGround;
            launchDirection = launchLoop.LastLaunchDirection;
            launchResetPositionError = launchLoop.LastResetPositionError;
            launchResetRotationError = launchLoop.LastResetRotationError;
        }

        private static void FinishCapture()
        {
            RequireRootReset();
            Texture2D sheet = CombinePanels();
            try
            {
                PlayerDamageReactionAnimationSetupTools.WriteDeathGroundAndLaunchFinalEvidence(
                    sheet,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    deathLoop.ConfiguredBodyCount,
                    deathMaximumDynamicBodyCount,
                    deathLoop.UsesDirectBoneRigidbodies,
                    deathLoop.LastRagdollDuration,
                    deathLoop.LastMaximumHipsDrop,
                    deathLoop.LastMaximumHeadRise,
                    deathLoop.LastMaximumRootTransformDisplacement,
                    deathLoop.LastMaximumGroundPenetration,
                    deathLoop.LastResetPositionError,
                    deathLoop.LastResetRotationError,
                    launchLoop.ConfiguredBodyCount,
                    launchMaximumDynamicBodyCount,
                    launchLoop.UsesDirectBoneRigidbodies,
                    deathLoop.SharedGroundHeight,
                    launchStartedAtSeconds,
                    launchFlightDuration,
                    launchLandingHoldDuration,
                    launchMaximumHorizontalDisplacement,
                    launchMaximumHipsHeightGain,
                    launchMaximumHipsRotationDegrees,
                    launchMaximumAirborneHipsRotationDegrees,
                    launchMaximumAirborneTorsoTiltDegrees,
                    launchMaximumAverageAngularSpeed,
                    launchMaximumJointAnchorSeparation,
                    launchMaximumRootTransformDisplacement,
                    launchMaximumGroundPenetration,
                    launchLandingHeadHeightAboveGround,
                    launchDirection,
                    launchResetPositionError,
                    launchResetRotationError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D RenderFixedTarget(
            GameObject target, Vector3 origin, float groundHeight)
        {
            const int width = 520;
            const int height = 360;
            const int reviewLayer = 31;
            GameObject cameraObject = new GameObject("DeathPhysics_ReviewCamera", typeof(Camera));
            GameObject lightObject = new GameObject("DeathPhysics_ReviewLight", typeof(Light));
            GameObject lineObject = new GameObject("DeathPhysics_GroundMarker", typeof(LineRenderer));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            lineObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            LineRenderer line = lineObject.GetComponent<LineRenderer>();
            Material lineMaterial = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var skinnedRenderers = new List<SkinnedMeshRenderer>();
            var updateWhenOffscreenStates = new List<bool>();
            var skinnedLocalBounds = new List<Bounds>();
            var targetRenderers = new List<Renderer>();
            var forceRenderingOffStates = new List<bool>();
            var layerObjects = new List<GameObject>();
            var originalLayers = new List<int>();
            try
            {
                var seenLayerObjects = new HashSet<GameObject>();
                foreach (Renderer targetRenderer in target.GetComponentsInChildren<Renderer>(true))
                {
                    targetRenderers.Add(targetRenderer);
                    forceRenderingOffStates.Add(targetRenderer.forceRenderingOff);
                    targetRenderer.forceRenderingOff = false;
                    if (targetRenderer is SkinnedMeshRenderer skinnedRenderer)
                    {
                        skinnedRenderers.Add(skinnedRenderer);
                        updateWhenOffscreenStates.Add(skinnedRenderer.updateWhenOffscreen);
                        skinnedLocalBounds.Add(skinnedRenderer.localBounds);
                        skinnedRenderer.updateWhenOffscreen = true;
                        skinnedRenderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 20f);
                    }
                    if (!seenLayerObjects.Add(targetRenderer.gameObject)) continue;
                    layerObjects.Add(targetRenderer.gameObject);
                    originalLayers.Add(targetRenderer.gameObject.layer);
                    targetRenderer.gameObject.layer = reviewLayer;
                }
                lineObject.layer = reviewLayer;

                Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                    Shader.Find("Sprites/Default") ??
                                    Shader.Find("Unlit/Color");
                if (lineShader == null)
                    throw new InvalidOperationException("A validation ground-line shader is unavailable.");
                lineMaterial = new Material(lineShader) { hideFlags = HideFlags.HideAndDontSave };
                if (lineMaterial.HasProperty("_BaseColor"))
                    lineMaterial.SetColor("_BaseColor", new Color(0.55f, 0.65f, 0.72f, 1f));
                if (lineMaterial.HasProperty("_Color"))
                    lineMaterial.SetColor("_Color", new Color(0.55f, 0.65f, 0.72f, 1f));
                line.material = lineMaterial;
                line.positionCount = 2;
                line.startWidth = 0.025f;
                line.endWidth = 0.025f;
                line.useWorldSpace = true;
                line.SetPosition(0, origin - target.transform.right * 3.4f + Vector3.up * (groundHeight - origin.y));
                line.SetPosition(1, origin + target.transform.right * 3.4f + Vector3.up * (groundHeight - origin.y));

                DeathRagdollLoop physicsLoop = target.GetComponent<DeathRagdollLoop>() ??
                    throw new InvalidOperationException("Death physics target has no DeathRagdollLoop.");
                Bounds targetBounds = physicsLoop.DirectBonePhysicsBounds;

                Vector3 front = target.transform.forward.normalized;
                float visibleBottom = Mathf.Min(groundHeight, targetBounds.min.y);
                float visibleTop = Mathf.Max(groundHeight + 0.1f, targetBounds.max.y);
                Vector3 focus = new Vector3(
                    targetBounds.center.x,
                    (visibleBottom + visibleTop) * 0.5f,
                    targetBounds.center.z);
                camera.transform.position = focus + front * 8f;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position, Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    0.85f,
                    (visibleTop - visibleBottom) * 0.62f + 0.18f,
                    targetBounds.extents.x / (width / (float)height) * 1.2f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.cullingMask = 1 << reviewLayer;

                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.cullingMask = 1 << reviewLayer;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < skinnedRenderers.Count; index++)
                    if (skinnedRenderers[index] != null)
                    {
                        skinnedRenderers[index].updateWhenOffscreen = updateWhenOffscreenStates[index];
                        skinnedRenderers[index].localBounds = skinnedLocalBounds[index];
                    }
                for (int index = 0; index < targetRenderers.Count; index++)
                    if (targetRenderers[index] != null)
                        targetRenderers[index].forceRenderingOff = forceRenderingOffStates[index];
                for (int index = 0; index < layerObjects.Count; index++)
                    if (layerObjects[index] != null)
                        layerObjects[index].layer = originalLayers[index];
                if (renderTexture != null) RenderTexture.ReleaseTemporary(renderTexture);
                if (lineMaterial != null) UnityEngine.Object.DestroyImmediate(lineMaterial);
                UnityEngine.Object.DestroyImmediate(lineObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D CombinePanels()
        {
            if (DeathPanels.Count != 5 || LaunchPanels.Count != 6)
                throw new InvalidOperationException(
                    "Death ground/launch final capture requires five Death and six launch frames.");
            const int columns = 6;
            const int gap = 6;
            int panelWidth = LaunchPanels[0].width;
            int panelHeight = LaunchPanels[0].height;
            var sheet = new Texture2D(
                panelWidth * columns + gap * (columns - 1),
                panelHeight * 2 + gap,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255), sheet.width * sheet.height).ToArray());
            for (int index = 0; index < DeathPanels.Count; index++)
                sheet.SetPixels32(index * (panelWidth + gap), panelHeight + gap,
                    panelWidth, panelHeight, DeathPanels[index].GetPixels32());
            for (int index = 0; index < LaunchPanels.Count; index++)
                sheet.SetPixels32(index * (panelWidth + gap), 0,
                    panelWidth, panelHeight, LaunchPanels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootReset()
        {
            if (Vector3.Distance(deathInitialLocalPosition, death.transform.localPosition) > 0.0001f ||
                Quaternion.Angle(deathInitialLocalRotation, death.transform.localRotation) > 0.01f ||
                Vector3.Distance(launchInitialLocalPosition, launch.transform.localPosition) > 0.0001f ||
                Quaternion.Angle(launchInitialLocalRotation, launch.transform.localRotation) > 0.01f)
                throw new InvalidOperationException(
                    "A death root Transform did not reset after physical playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey, "Death ground/full-body launch natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in DeathPanels)
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            foreach (Texture2D panel in LaunchPanels)
                if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            DeathPanels.Clear();
            LaunchPanels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            death = null;
            launch = null;
            deathAnimator = null;
            deathLoop = null;
            launchLoop = null;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
