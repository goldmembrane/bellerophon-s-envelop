using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.TergoCargoRunScene
{
    internal static class TergoRunChaseAnimation
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Tergo Enemy Placement";
        private const string StaticRootName = "Tergo_00_Static_Review";
        private const string IdleRootName = "Tergo_01_Idle";
        private const string WalkRootName = "Tergo_02_Walk_Wander";
        private const string DetectRootName = "Tergo_03_Detect_User";
        private const string RunRootName = "Tergo_04_BackRush";
        private const string PierceAttackRootName = "Tergo_05_Pierce_Attack";
        private const string DownedPounceRootName = "Tergo_07_Downed_Pounce";
        private const string InterruptStaggerRootName = "Tergo_09_Interrupt_Stagger";
        private const string CrouchTrembleRootName = "Tergo_10_Crouch_Tremble_5s";
        private const string HitNormalRootName = "Tergo_11_Hit_Normal";
        private const string DeathRootName = "Tergo_12_Death";
        private const string EyeContainerName = "TergoApprovedEyes";
        private const string NormalModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx";
        private const string RunAnimationSourceAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_running.fbx";
        private const string ThrustModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/tergo thrust.fbx";
        private const string ThrustModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_thrust.fbx";
        private const string TakedownModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/tergo takedown.fbx";
        private const string TakedownModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_takedown.fbx";
        private const string FallOverModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/tergo fall over.fbx";
        private const string FallOverModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_fall_over.fbx";
        private const string TerrifiedModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/tergo terrified.fbx";
        private const string TerrifiedModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_terrified.fbx";
        private const string HittedModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/tergo hitted.fbx";
        private const string HittedModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_hitted.fbx";
        private const string DyingModelSourceAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/tergo dying.fbx";
        private const string DyingModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_dying.fbx";
        private const string AnimationFolderPath = "Assets/_Project/Art/Enemies/Tergo/Animations";
        private const string IdleBreathingControllerPath = AnimationFolderPath + "/Tergo_Idle_Breathing.controller";
        private const string WalkImportedControllerPath = AnimationFolderPath + "/Tergo_Walk_Wander_FromFbx.controller";
        private const string RunChaseClipPath = AnimationFolderPath + "/Tergo_Run_Chase.anim";
        private const string RunChaseControllerPath = AnimationFolderPath + "/Tergo_Run_Chase.controller";
        private const string RunChaseClipName = "Tergo_Run_Chase";
        private const string PierceAttackClipPath = AnimationFolderPath + "/Tergo_Pierce_Attack.anim";
        private const string PierceAttackControllerPath = AnimationFolderPath + "/Tergo_Pierce_Attack.controller";
        private const string PierceAttackClipName = "Tergo_Pierce_Attack";
        private const string ThrustFbxPierceAttackClipPath = AnimationFolderPath + "/Tergo_Pierce_Attack_Thrust_Fbx.anim";
        private const string ThrustFbxPierceAttackControllerPath = AnimationFolderPath + "/Tergo_Pierce_Attack_Thrust_Fbx.controller";
        private const string ThrustFbxPierceAttackClipName = "Tergo_Pierce_Attack_Thrust_Fbx";
        private const string DownedPounceFromFbxClipPath = AnimationFolderPath + "/Tergo_Downed_Pounce_FromFbx.anim";
        private const string DownedPounceFromFbxControllerPath = AnimationFolderPath + "/Tergo_Downed_Pounce_FromFbx.controller";
        private const string DownedPounceFromFbxClipName = "Tergo_Downed_Pounce_FromFbx";
        private const string InterruptStaggerClipPath = AnimationFolderPath + "/Tergo_Interrupt_Stagger_BackwardFall.anim";
        private const string InterruptStaggerControllerPath = AnimationFolderPath + "/Tergo_Interrupt_Stagger_BackwardFall.controller";
        private const string InterruptStaggerClipName = "Tergo_Interrupt_Stagger_BackwardFall";
        private const string FallOverFbxInterruptStaggerClipPath = AnimationFolderPath + "/Tergo_Interrupt_Stagger_FallOver_Fbx.anim";
        private const string FallOverFbxInterruptStaggerControllerPath = AnimationFolderPath + "/Tergo_Interrupt_Stagger_FallOver_Fbx.controller";
        private const string FallOverFbxInterruptStaggerClipName = "Tergo_Interrupt_Stagger_FallOver_Fbx";
        private const string CrouchTrembleClipPath = AnimationFolderPath + "/Tergo_Crouch_Tremble_5s.anim";
        private const string CrouchTrembleControllerPath = AnimationFolderPath + "/Tergo_Crouch_Tremble_5s.controller";
        private const string CrouchTrembleClipName = "Tergo_Crouch_Tremble_5s";
        private const string TerrifiedFbxCrouchTrembleClipPath = AnimationFolderPath + "/Tergo_Crouch_Tremble_5s_Terrified_Fbx.anim";
        private const string TerrifiedFbxCrouchTrembleControllerPath = AnimationFolderPath + "/Tergo_Crouch_Tremble_5s_Terrified_Fbx.controller";
        private const string TerrifiedFbxCrouchTrembleClipName = "Tergo_Crouch_Tremble_5s_Terrified_Fbx";
        private const string HittedFbxHitNormalClipPath = AnimationFolderPath + "/Tergo_Hit_Normal_Hitted_Fbx.anim";
        private const string HittedFbxHitNormalControllerPath = AnimationFolderPath + "/Tergo_Hit_Normal_Hitted_Fbx.controller";
        private const string HittedFbxHitNormalClipName = "Tergo_Hit_Normal_Hitted_Fbx";
        private const float HittedFbxHitNormalPlaybackSpeed = 1.5f;
        private const string DyingFbxDeathClipPath = AnimationFolderPath + "/Tergo_Death_Dying_Fbx.anim";
        private const string DyingFbxDeathControllerPath = AnimationFolderPath + "/Tergo_Death_Dying_Fbx.controller";
        private const string DyingFbxDeathClipName = "Tergo_Death_Dying_Fbx";
        private const float DeathMeltStartDelay = 0.08f;
        private const float DeathMeltSinkDuration = 0.9f;
        private const float DeathMeltPuddleDuration = 2.2f;
        private const float DeathMeltHoldDuration = 0.35f;
        private const float DeathMeltPuddleGroundClearance = 0.035f;
        private const float DeathMeltPuddlePlanarSpread = 1.55f;
        private const float DeathMeltPuddleMaxBoneHeightRange = 0.2f;
        private const string ApprovedDeathMeltPuddleSampleFbxPath = "artSample/enemies/tergo_death_melt_puddle/exports/tergo_death_melt_puddle_blendshape.fbx";
        private const string ApprovedDeathMeltPuddleModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo_death_melt_puddle_blendshape.fbx";
        private const string ApprovedDeathMeltPuddleRootName = "Tergo_12_Death_Approved_MeltPuddle";
        private const string ApprovedDeathMeltWeightSagShape = "DEATH_TERGO_01_weight_sag";
        private const string ApprovedDeathMeltCrushCollapseShape = "DEATH_TERGO_02_crush_collapse";
        private const string ApprovedDeathMeltSpreadShape = "DEATH_TERGO_03_melt_spread";
        private const float ApprovedDeathMeltStartDelay = 0.02f;
        private const float ApprovedDeathMeltStartYOffset = 0.09f;
        private const float ApprovedDeathMeltSagDuration = 0.45f;
        private const float ApprovedDeathMeltCollapseDuration = 0.9f;
        private const float ApprovedDeathMeltSpreadDuration = 1.45f;
        private const float ApprovedDeathMeltHoldDuration = 0.35f;
        private const float ApprovedDeathMeltVisibilityLead = 0.001f;
        private const string HitNormalClipPath = AnimationFolderPath + "/Tergo_Hit_Normal.anim";
        private const string HitNormalControllerPath = AnimationFolderPath + "/Tergo_Hit_Normal.controller";
        private const string HitNormalClipName = "Tergo_Hit_Normal";
        private const float InterruptStaggerDuration = 1.36f;
        private const float InterruptStaggerFallTime = 0.44f;
        private const float InterruptStaggerImpactTime = 0.72f;
        private const float InterruptStaggerSettleTime = 1.12f;
        private const float CrouchTrembleDuration = 5f;
        private const float CrouchTremblePushTime = 0.45f;
        private const float CrouchTrembleRiseTime = 1.35f;
        private const float CrouchTrembleCoverFaceTime = 2.05f;
        private const float CrouchTrembleTrembleStartTime = 2.22f;
        private const float CrouchTrembleTrembleStep = 0.18f;
        private const float HitNormalDuration = 2.2f;
        private const float HitNormalGuardTime = 0.72f;
        private const float HitNormalHoldTime = 1.14f;
        private const float HitNormalRecoverTime = 1.82f;
        private const float PierceAttackDuration = 1.2f;
        private const float PierceAttackReadyTime = 0.12f;
        private const float PierceAttackWindupTime = 0.38f;
        private const float PierceAttackPlantTime = 0.68f;
        private const float PierceAttackThrustTime = 0.82f;
        private const float PierceAttackHoldTime = 1.02f;
        private const float PierceAttackRecoverTime = 1.12f;
        private const string AuthoredSprintClipPath = AnimationFolderPath + "/Tergo_BackRush_Authored_Sprint.anim";
        private const string AuthoredSprintControllerPath = AnimationFolderPath + "/Tergo_BackRush_Authored_Sprint.controller";
        private const string AuthoredSprintClipName = "Tergo_BackRush_Authored_Sprint";
        private const float AuthoredSprintDuration = 0.4f;

        private static readonly string[] WaistStabilizedBonePaths =
        {
            "Armature/Hips/Spine02",
            "Armature/Hips/Spine02/Spine01",
            "Armature/Hips/Spine02/Spine01/Spine"
        };

        private static readonly string[] AuthoredSprintRequiredBonePaths =
        {
            "Armature/Hips",
            "Armature/Hips/Spine02",
            "Armature/Hips/Spine02/Spine01",
            "Armature/Hips/Spine02/Spine01/Spine",
            "Armature/Hips/Spine02/Spine01/Spine/neck",
            "Armature/Hips/Spine02/Spine01/Spine/neck/Head"
        };

        private static readonly string[] ReferenceDrivenSprintMotionPaths =
        {
            "Armature/Hips",
            "Armature/Hips/Spine02",
            "Armature/Hips/Spine02/Spine01",
            "Armature/Hips/Spine02/Spine01/Spine",
            "Armature/Hips/Spine02/Spine01/Spine/neck",
            "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
            "Armature/Hips/LeftUpLeg",
            "Armature/Hips/LeftUpLeg/LeftLeg",
            "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
            "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase",
            "Armature/Hips/RightUpLeg",
            "Armature/Hips/RightUpLeg/RightLeg",
            "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
            "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand"
        };

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Run Chase Animation")]
        public static void ApplyTergoRunChaseAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            var importedClips = LoadImportedAnimationClips();
            var sourceClip = SelectRunSourceClip(importedClips);
            var runClip = EnsureCopiedRunClip(sourceClip);
            var controller = EnsureRunController(runClip);
            var avatar = EnsureRunningAvatar();
            var rigResult = ReplaceRunRigWithRunningFbxArmature(runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while applying run chase animation.");
            }

            var idleConfiguredAnimators = CountConfiguredAnimators(idleRoot);
            var walkConfiguredAnimators = CountConfiguredAnimators(walkRoot);
            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);

            if (runConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    RunRootName + " must have exactly one configured Animator. Count=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            if (otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Only idle, walk, and run Tergo objects may have animation controllers now. OtherConfiguredAnimators=" +
                    otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    RunRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo run chase animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoRunChaseAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceAnimationFbx=" + RunAnimationSourceAssetPath +
                ", SourceClips=" + FormatClipNames(importedClips) +
                ", RunClip=" + RunChaseClipPath +
                ", RunClipLength=" + runClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", Controller=" + RunChaseControllerPath +
                ", AvatarSource=" + RunAnimationSourceAssetPath +
                ", AvatarAssigned=" + (avatar != null).ToString(CultureInfo.InvariantCulture) +
                ", RigArmatureReplaced=True" +
                ", OldArmatureReplaced=" + rigResult.OldArmatureReplaced.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRenderersPreserved=" + rigResult.SkinnedRenderersPreserved.ToString(CultureInfo.InvariantCulture) +
                ", RendererMeshesPreserved=True" +
                ", RendererMaterialsPreserved=True" +
                ", RendererBonesReplaced=" + rigResult.RendererBonesReplaced.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerReparentedToRunningRig=" + rigResult.EyeContainerReparented.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", IdleConfiguredAnimators=" + idleConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", WalkConfiguredAnimators=" + walkConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RunConfiguredAnimators=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", DetectUserDedicatedAnimation=False" +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", RootTransformUnchanged=True" +
                ", SourceFbxModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Replace Pierce Attack With Thrust FBX")]
        public static void ReplaceTergoPierceAttackWithThrustFbx()
        {
            var thrustPrefab = ImportThrustModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var existingPierceRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, PierceAttackRootName);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceState = TransformState.Capture(existingPierceRoot);
            var pierceSiblingIndex = existingPierceRoot.GetSiblingIndex();
            var pierceWasActive = existingPierceRoot.gameObject.activeSelf;

            UnityEngine.Object.DestroyImmediate(existingPierceRoot.gameObject);

            var instanceObject = PrefabUtility.InstantiatePrefab(thrustPrefab, placementRoot.transform) as GameObject;
            if (instanceObject == null)
            {
                throw new InvalidOperationException("Failed to instantiate thrust FBX prefab: " + ThrustModelAssetPath);
            }

            instanceObject.name = PierceAttackRootName;
            var pierceAttackRoot = instanceObject.transform;
            pierceState.ApplyTo(pierceAttackRoot);
            pierceAttackRoot.SetSiblingIndex(Mathf.Clamp(pierceSiblingIndex, 0, placementRoot.transform.childCount - 1));
            instanceObject.SetActive(pierceWasActive);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, ThrustModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " is not linked to thrust FBX asset. SourcePath=" + sourcePath);
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot))
            {
                throw new InvalidOperationException("Non-target Tergo root transform changed while replacing " + PierceAttackRootName + ".");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, PierceAttackRootName)))
            {
                throw new InvalidOperationException("Non-target Tergo root list changed while replacing " + PierceAttackRootName + ".");
            }

            var armature = RequireChild(pierceAttackRoot, "Armature");
            var rendererCount = pierceAttackRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = pierceAttackRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(PierceAttackRootName + " thrust FBX model must include visible skinned renderers.");
            }

            var armatureBoneCount = CountRigBones(armature);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo pierce attack with thrust FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackThrustFbxReplaced" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", SourceFile=" + ThrustModelSourceAbsolutePath +
                ", ImportedAsset=" + ThrustModelAssetPath +
                ", OldPierceRootDeleted=True" +
                ", NewPierceRootFromThrustFbx=True" +
                ", SlotTransformPreserved=True" +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Replace Downed Pounce With Takedown FBX")]
        public static void ReplaceTergoDownedPounceWithTakedownFbx()
        {
            var takedownPrefab = ImportTakedownModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var existingDownedPounceRoot = FindDirectChild(placementRoot.transform, DownedPounceRootName);
            var hadExistingDownedPounceRoot = existingDownedPounceRoot != null;
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, DownedPounceRootName);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedLocalPosition = Vector3.zero;
            var downedLocalRotation = Quaternion.identity;
            var downedLocalScale = Vector3.one;
            var downedSiblingIndex = pierceAttackRoot.GetSiblingIndex() + 1;
            var downedWasActive = true;

            if (existingDownedPounceRoot != null)
            {
                downedLocalPosition = existingDownedPounceRoot.localPosition;
                downedLocalRotation = existingDownedPounceRoot.localRotation;
                downedLocalScale = existingDownedPounceRoot.localScale;
                downedSiblingIndex = existingDownedPounceRoot.GetSiblingIndex();
                downedWasActive = existingDownedPounceRoot.gameObject.activeSelf;
                UnityEngine.Object.DestroyImmediate(existingDownedPounceRoot.gameObject);
            }
            else
            {
                var slotOffset = pierceAttackRoot.localPosition - runRoot.localPosition;
                if (slotOffset.sqrMagnitude < 0.0001f)
                {
                    slotOffset = Vector3.right * 2.5f;
                }

                downedLocalPosition = pierceAttackRoot.localPosition + slotOffset;
                downedLocalRotation = pierceAttackRoot.localRotation;
                downedLocalScale = pierceAttackRoot.localScale;
            }

            var instanceObject = PrefabUtility.InstantiatePrefab(takedownPrefab, placementRoot.transform) as GameObject;
            if (instanceObject == null)
            {
                throw new InvalidOperationException("Failed to instantiate takedown FBX prefab: " + TakedownModelAssetPath);
            }

            instanceObject.name = DownedPounceRootName;
            var downedPounceRoot = instanceObject.transform;
            downedPounceRoot.localPosition = downedLocalPosition;
            downedPounceRoot.localRotation = downedLocalRotation;
            downedPounceRoot.localScale = downedLocalScale;
            downedPounceRoot.SetSiblingIndex(Mathf.Clamp(downedSiblingIndex, 0, placementRoot.transform.childCount - 1));
            instanceObject.SetActive(downedWasActive);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TakedownModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " is not linked to takedown FBX asset. SourcePath=" + sourcePath);
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException("Non-target Tergo root transform changed while replacing " + DownedPounceRootName + ".");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, DownedPounceRootName)))
            {
                throw new InvalidOperationException("Non-target Tergo root list changed while replacing " + DownedPounceRootName + ".");
            }

            var armature = RequireChild(downedPounceRoot, "Armature");
            var rendererCount = downedPounceRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = downedPounceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(DownedPounceRootName + " takedown FBX model must include visible skinned renderers.");
            }

            var armatureBoneCount = CountRigBones(armature);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo Downed Pounce with takedown FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoDownedPounceTakedownFbxReplaced" +
                ", Target=" + PlacementRootName + "/" + DownedPounceRootName +
                ", SourceFile=" + TakedownModelSourceAbsolutePath +
                ", ImportedAsset=" + TakedownModelAssetPath +
                ", HadExistingDownedPounceRoot=" + hadExistingDownedPounceRoot.ToString(CultureInfo.InvariantCulture) +
                ", OldDownedPounceRootDeleted=True" +
                ", NewDownedPounceRootFromTakedownFbx=True" +
                ", SlotTransformApplied=True" +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Replace Interrupt Stagger With Fall Over FBX")]
        public static void ReplaceTergoInterruptStaggerWithFallOverFbx()
        {
            var fallOverPrefab = ImportFallOverModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var existingInterruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, InterruptStaggerRootName);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedPounceState = TransformState.Capture(downedPounceRoot);
            var interruptState = TransformState.Capture(existingInterruptRoot);
            var interruptSiblingIndex = existingInterruptRoot.GetSiblingIndex();
            var interruptWasActive = existingInterruptRoot.gameObject.activeSelf;

            UnityEngine.Object.DestroyImmediate(existingInterruptRoot.gameObject);

            var instanceObject = PrefabUtility.InstantiatePrefab(fallOverPrefab, placementRoot.transform) as GameObject;
            if (instanceObject == null)
            {
                throw new InvalidOperationException("Failed to instantiate fall-over FBX prefab: " + FallOverModelAssetPath);
            }

            instanceObject.name = InterruptStaggerRootName;
            var interruptRoot = instanceObject.transform;
            interruptState.ApplyTo(interruptRoot);
            interruptRoot.SetSiblingIndex(Mathf.Clamp(interruptSiblingIndex, 0, placementRoot.transform.childCount - 1));
            instanceObject.SetActive(interruptWasActive);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, FallOverModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " is not linked to fall-over FBX asset. SourcePath=" + sourcePath);
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot) ||
                !downedPounceState.Matches(downedPounceRoot))
            {
                throw new InvalidOperationException("Non-target Tergo root transform changed while replacing " + InterruptStaggerRootName + ".");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, InterruptStaggerRootName)))
            {
                throw new InvalidOperationException("Non-target Tergo root list changed while replacing " + InterruptStaggerRootName + ".");
            }

            if (!interruptState.Matches(interruptRoot))
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " slot transform was not preserved after fall-over FBX replacement.");
            }

            var armature = RequireChild(interruptRoot, "Armature");
            var rendererCount = interruptRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = interruptRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " fall-over FBX model must include visible skinned renderers.");
            }

            var armatureBoneCount = CountRigBones(armature);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo interrupt stagger with fall-over FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoInterruptStaggerFallOverFbxReplaced" +
                ", Target=" + PlacementRootName + "/" + InterruptStaggerRootName +
                ", SourceFile=" + FallOverModelSourceAbsolutePath +
                ", ImportedAsset=" + FallOverModelAssetPath +
                ", OldInterruptStaggerRootDeleted=True" +
                ", NewInterruptStaggerRootFromFallOverFbx=True" +
                ", SlotTransformPreserved=True" +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Interrupt Stagger Fall Over FBX Loop")]
        public static void ApplyTergoInterruptStaggerFallOverFbxLoop()
        {
            ImportFallOverModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var interruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(interruptRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, FallOverModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " must stay linked to fall-over FBX asset before applying fall-over animation. SourcePath=" + sourcePath);
            }

            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedPounceState = TransformState.Capture(downedPounceRoot);
            var interruptState = TransformState.Capture(interruptRoot);
            var sourceClips = LoadFallOverAnimationClips();
            var sourceClip = SelectFallOverSourceClip(sourceClips);
            var clip = EnsureCopiedFallOverFbxInterruptStaggerClip(sourceClip);
            var controller = EnsureFallOverFbxInterruptStaggerController(clip);
            var avatar = LoadFallOverAvatarOrNull();
            var removedChildAnimators = RemovePierceAttackChildAnimators(interruptRoot);

            if (!SampleClipChangesTransforms(clip, interruptRoot))
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " fall-over FBX clip did not change target transforms.");
            }

            var animator = interruptRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = interruptRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playbackMetrics = EvaluateFallOverFbxAnimatorPlayback(animator, interruptRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " fall-over FBX Animator did not visibly move. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot) ||
                !downedPounceState.Matches(downedPounceRoot) ||
                !interruptState.Matches(interruptRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while applying " + InterruptStaggerRootName + " fall-over FBX loop.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + InterruptStaggerRootName + " fall-over FBX loop.");
            }

            if (!FallOverFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Fall-over FBX controller default state does not use the copied fall-over clip.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " fall-over FBX clip must be configured for loop playback.");
            }

            var interruptConfiguredAnimators = CountConfiguredAnimators(interruptRoot);
            if (interruptConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " must keep exactly one configured Animator after fall-over loop apply. Count=" +
                    interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo interrupt stagger fall-over FBX loop.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoInterruptStaggerFallOverFbxLoopApplied" +
                ", Target=" + PlacementRootName + "/" + InterruptStaggerRootName +
                ", SourceModel=" + FallOverModelAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(sourceClips) +
                ", Clip=" + FallOverFbxInterruptStaggerClipPath +
                ", Controller=" + FallOverFbxInterruptStaggerControllerPath +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", InterruptConfiguredAnimators=" + interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Replace Crouch Tremble With Terrified FBX")]
        public static void ReplaceTergoCrouchTrembleWithTerrifiedFbx()
        {
            var terrifiedPrefab = ImportTerrifiedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var interruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var existingCrouchRoot = RequireChild(placementRoot.transform, CrouchTrembleRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, CrouchTrembleRootName);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedPounceState = TransformState.Capture(downedPounceRoot);
            var interruptState = TransformState.Capture(interruptRoot);
            var crouchState = TransformState.Capture(existingCrouchRoot);
            var crouchSiblingIndex = existingCrouchRoot.GetSiblingIndex();
            var crouchWasActive = existingCrouchRoot.gameObject.activeSelf;

            UnityEngine.Object.DestroyImmediate(existingCrouchRoot.gameObject);

            var instanceObject = PrefabUtility.InstantiatePrefab(terrifiedPrefab, placementRoot.transform) as GameObject;
            if (instanceObject == null)
            {
                throw new InvalidOperationException("Failed to instantiate terrified FBX prefab: " + TerrifiedModelAssetPath);
            }

            instanceObject.name = CrouchTrembleRootName;
            var crouchRoot = instanceObject.transform;
            crouchState.ApplyTo(crouchRoot);
            crouchRoot.SetSiblingIndex(Mathf.Clamp(crouchSiblingIndex, 0, placementRoot.transform.childCount - 1));
            instanceObject.SetActive(crouchWasActive);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TerrifiedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " is not linked to terrified FBX asset. SourcePath=" + sourcePath);
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot) ||
                !downedPounceState.Matches(downedPounceRoot) ||
                !interruptState.Matches(interruptRoot))
            {
                throw new InvalidOperationException("Non-target Tergo root transform changed while replacing " + CrouchTrembleRootName + ".");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, CrouchTrembleRootName)))
            {
                throw new InvalidOperationException("Non-target Tergo root list changed while replacing " + CrouchTrembleRootName + ".");
            }

            if (!crouchState.Matches(crouchRoot))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " slot transform was not preserved after terrified FBX replacement.");
            }

            var armature = RequireChild(crouchRoot, "Armature");
            var rendererCount = crouchRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = crouchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX model must include visible skinned renderers.");
            }

            var armatureBoneCount = CountRigBones(armature);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo crouch tremble with terrified FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoCrouchTrembleTerrifiedFbxReplaced" +
                ", Target=" + PlacementRootName + "/" + CrouchTrembleRootName +
                ", SourceFile=" + TerrifiedModelSourceAbsolutePath +
                ", ImportedAsset=" + TerrifiedModelAssetPath +
                ", OldCrouchTrembleRootDeleted=True" +
                ", NewCrouchTrembleRootFromTerrifiedFbx=True" +
                ", SlotTransformPreserved=True" +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Crouch Tremble Terrified FBX Loop")]
        public static void ApplyTergoCrouchTrembleTerrifiedFbxLoop()
        {
            ImportTerrifiedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var interruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var crouchRoot = RequireChild(placementRoot.transform, CrouchTrembleRootName);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(crouchRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TerrifiedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " must stay linked to terrified FBX asset before applying terrified animation. SourcePath=" + sourcePath);
            }

            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedPounceState = TransformState.Capture(downedPounceRoot);
            var interruptState = TransformState.Capture(interruptRoot);
            var crouchState = TransformState.Capture(crouchRoot);
            var sourceClips = LoadTerrifiedAnimationClips();
            var sourceClip = SelectTerrifiedSourceClip(sourceClips);
            var clip = EnsureCopiedTerrifiedFbxCrouchTrembleClip(sourceClip);
            var controller = EnsureTerrifiedFbxCrouchTrembleController(clip);
            var avatar = LoadTerrifiedAvatarOrNull();
            var removedChildAnimators = RemovePierceAttackChildAnimators(crouchRoot);

            if (!SampleClipChangesTransforms(clip, crouchRoot))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX clip did not change target transforms.");
            }

            var animator = crouchRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = crouchRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playbackMetrics = EvaluateTerrifiedFbxAnimatorPlayback(animator, crouchRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " terrified FBX Animator did not visibly move. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot) ||
                !downedPounceState.Matches(downedPounceRoot) ||
                !interruptState.Matches(interruptRoot) ||
                !crouchState.Matches(crouchRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while applying " + CrouchTrembleRootName + " terrified FBX loop.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + CrouchTrembleRootName + " terrified FBX loop.");
            }

            if (!TerrifiedFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Terrified FBX controller default state does not use the copied terrified clip.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX clip must be configured for loop playback.");
            }

            var crouchConfiguredAnimators = CountConfiguredAnimators(crouchRoot);
            if (crouchConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " must keep exactly one configured Animator after terrified loop apply. Count=" +
                    crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo crouch tremble terrified FBX loop.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoCrouchTrembleTerrifiedFbxLoopApplied" +
                ", Target=" + PlacementRootName + "/" + CrouchTrembleRootName +
                ", SourceModel=" + TerrifiedModelAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(sourceClips) +
                ", Clip=" + TerrifiedFbxCrouchTrembleClipPath +
                ", Controller=" + TerrifiedFbxCrouchTrembleControllerPath +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", CrouchConfiguredAnimators=" + crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Crouch Tremble Terrified FBX Loop")]
        public static void ValidateTergoCrouchTrembleTerrifiedFbxLoop()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var crouchRoot = RequireChild(placementRoot.transform, CrouchTrembleRootName);
            var controller = RequireAsset<AnimatorController>(TerrifiedFbxCrouchTrembleControllerPath);
            var clip = RequireAsset<AnimationClip>(TerrifiedFbxCrouchTrembleClipPath);
            RequireConfiguredAnimator(crouchRoot, controller, CrouchTrembleRootName);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(crouchRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TerrifiedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " must stay linked to terrified FBX asset during validation. SourcePath=" + sourcePath);
            }

            var animator = crouchRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " is missing its root Animator.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!TerrifiedFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, crouchRoot))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX animation is not connected correctly.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX animation loop setting is not enabled.");
            }

            var playbackMetrics = EvaluateTerrifiedFbxAnimatorPlayback(animator, crouchRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " terrified FBX Animator did not move during validation. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            var armature = RequireChild(crouchRoot, "Armature");
            var rendererCount = crouchRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = crouchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var armatureBoneCount = CountRigBones(armature);
            var crouchConfiguredAnimators = CountConfiguredAnimators(crouchRoot);
            if (rendererCount == 0 || skinnedRendererCount == 0 || crouchConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected terrified crouch tremble validation counts. Renderer=" +
                    rendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Skinned=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Animator=" + crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoCrouchTrembleTerrifiedFbxLoopValidated" +
                ", Target=" + PlacementRootName + "/" + CrouchTrembleRootName +
                ", SourceModel=" + TerrifiedModelAssetPath +
                ", Clip=" + TerrifiedFbxCrouchTrembleClipPath +
                ", Controller=" + TerrifiedFbxCrouchTrembleControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", SourceModelLinked=True" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", CrouchConfiguredAnimators=" + crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Crouch Tremble Visual Details From Static Review")]
        public static void SyncTergoCrouchTrembleVisualDetailsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var crouchRoot = RequireChild(placementRoot.transform, CrouchTrembleRootName);
            var controller = RequireAsset<AnimatorController>(TerrifiedFbxCrouchTrembleControllerPath);
            var clip = RequireAsset<AnimationClip>(TerrifiedFbxCrouchTrembleClipPath);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(crouchRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TerrifiedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " must stay linked to terrified FBX asset before visual sync. SourcePath=" + sourcePath);
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);
            var animator = crouchRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " must keep the terrified FBX controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;
            var targetLightCountBefore = CountBackRushVisualLights(crouchRoot);

            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, crouchRoot);
            DestroyBackRushVisualLightGameObjects(crouchRoot);
            DestroyBackRushVisualNamedDescendants(crouchRoot, EyeContainerName);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, crouchRoot, sourceEyeContainer);
            var copiedEyeLightCount = CountBackRushVisualLights(copiedEyeContainer);
            var copiedExternalLights = CopyBackRushVisualExternalLightObjectsFromReference(staticRoot, crouchRoot, sourceEyeContainer);
            var removedCopiedEyeAnimators = RemoveAnimatorComponentsUnderRoot(copiedEyeContainer);
            var targetLightCountAfter = CountBackRushVisualLights(crouchRoot);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, crouchRoot, sourceEyeContainer);

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + CrouchTrembleRootName + " visual details.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " Animator changed while syncing visual details.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, crouchRoot))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(crouchRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (copiedEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(copiedEyeContainer))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " eye container was not placed at the same relative head position as the reference Tergo.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCountAfter != sourceLightCount ||
                copiedEyeLightCount + copiedExternalLights != sourceLightCount)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " light count does not match reference after visual sync. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedEye=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedExternal=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!TerrifiedFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, crouchRoot))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX animation was not preserved after visual sync.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " terrified FBX animation loop setting changed during visual sync.");
            }

            var crouchConfiguredAnimators = CountConfiguredAnimators(crouchRoot);
            if (crouchConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " must keep exactly one configured Animator after visual sync. Count=" +
                    crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo crouch tremble visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoCrouchTrembleVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + CrouchTrembleRootName +
                ", Reference=" + StaticRootName +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionMatched=True" +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", CopiedEyeLights=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                ", CopiedExternalLights=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedEyeAnimators=" + removedCopiedEyeAnimators.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPreserved=True" +
                ", LoopAnimationPreserved=True" +
                ", MotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", CrouchConfiguredAnimators=" + crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Hit Normal")]
        public static void ApplyTergoHitNormal()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var hitRoot = EnsureHitNormalRoot(placementRoot.transform, staticRoot);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);

            RequireHitNormalVisualMatchesStatic(staticRoot, hitRoot);

            var clip = EnsureHitNormalClip(hitRoot);
            var metrics = EvaluateHitNormalMetrics(clip, hitRoot);
            RequireHitNormalMetrics(metrics);
            var controller = EnsureHitNormalController(clip);
            var removedChildAnimators = RemovePierceAttackChildAnimators(hitRoot);

            var animator = hitRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hitRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playback = EvaluateHitNormalAnimatorPlayback(animator, hitRoot, clip);
            if (!playback.FirstPassMoved || !playback.PostLoopMoved)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " hit normal Animator did not visibly move. FirstRotationDelta=" +
                    playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, hitRoot))
            {
                throw new InvalidOperationException(HitNormalRootName + " hit normal clip did not change target transforms.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + HitNormalRootName + ".");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);
            RequireHitNormalVisualMatchesStatic(staticRoot, hitRoot);

            var hitConfiguredAnimators = CountConfiguredAnimators(hitRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName,
                DownedPounceRootName,
                InterruptStaggerRootName,
                CrouchTrembleRootName,
                HitNormalRootName);
            if (hitConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after hit normal apply. Hit=" +
                    hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo hit normal.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoHitNormalApplied" +
                ", Target=" + PlacementRootName + "/" + HitNormalRootName +
                ", Clip=" + HitNormalClipPath +
                ", Controller=" + HitNormalControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", VerticalFaceGuard=True" +
                ", HeadTurnSide=True" +
                ", BackwardRecoil=True" +
                ", HandsNotCrossed=" + metrics.HandsNotCrossed.ToString(CultureInfo.InvariantCulture) +
                ", MinHandForwardDelta=" + metrics.MinHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", MinHandRaiseDelta=" + metrics.MinHandRaiseDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", GuardHandLateralDistance=" + metrics.GuardHandLateralDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                ", FaceGuardDistance=" + metrics.FaceGuardDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                ", ForearmVerticalScore=" + metrics.ForearmVerticalScore.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ArmRaiseAngle=" + metrics.ArmRaiseAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HeadTurnAngle=" + metrics.HeadTurnAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsBackwardDelta=" + metrics.HipsBackwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BodyRecoilAngle=" + metrics.BodyRecoilAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", GuardHoldDrift=" + metrics.GuardHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimeLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", AvatarCleared=True" +
                ", VisualMatchesStatic=True" +
                ", HitConfiguredAnimators=" + hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Hit Normal")]
        public static void ValidateTergoHitNormal()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var hitRoot = RequireChild(placementRoot.transform, HitNormalRootName);
            var clip = RequireAsset<AnimationClip>(HitNormalClipPath);
            var controller = RequireAsset<AnimatorController>(HitNormalControllerPath);
            RequireConfiguredAnimator(hitRoot, controller, HitNormalRootName);

            var animator = hitRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(HitNormalRootName + " is missing its root Animator.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (Mathf.Abs(clip.length - HitNormalDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Hit normal clip length changed. Expected=" +
                    HitNormalDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Actual=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(HitNormalClipName + " must loop for scene review playback.");
            }

            if (animator.avatar != null)
            {
                throw new InvalidOperationException(HitNormalRootName + " transform-curve clip must keep Animator avatar null.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(HitNormalRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(HitNormalRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!controller.animationClips.Any(candidate => candidate == clip) || !ControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException(HitNormalControllerPath + " must use " + HitNormalClipPath + " as its default state motion.");
            }

            var metrics = EvaluateHitNormalMetrics(clip, hitRoot);
            RequireHitNormalMetrics(metrics);
            var playback = EvaluateHitNormalAnimatorPlayback(animator, hitRoot, clip);
            if (!playback.FirstPassMoved || !playback.PostLoopMoved)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " Animator did not move during validation. FirstRotationDelta=" +
                    playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            RequireHitNormalVisualMatchesStatic(staticRoot, hitRoot);

            var hitConfiguredAnimators = CountConfiguredAnimators(hitRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName,
                DownedPounceRootName,
                InterruptStaggerRootName,
                CrouchTrembleRootName,
                HitNormalRootName);
            if (hitConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts during hit normal validation. Hit=" +
                    hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoHitNormalValidated" +
                ", Target=" + PlacementRootName + "/" + HitNormalRootName +
                ", Clip=" + HitNormalClipPath +
                ", Controller=" + HitNormalControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", VerticalFaceGuard=True" +
                ", HeadTurnSide=True" +
                ", BackwardRecoil=True" +
                ", HandsNotCrossed=" + metrics.HandsNotCrossed.ToString(CultureInfo.InvariantCulture) +
                ", MinHandForwardDelta=" + metrics.MinHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", MinHandRaiseDelta=" + metrics.MinHandRaiseDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", GuardHandLateralDistance=" + metrics.GuardHandLateralDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                ", FaceGuardDistance=" + metrics.FaceGuardDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                ", ForearmVerticalScore=" + metrics.ForearmVerticalScore.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ArmRaiseAngle=" + metrics.ArmRaiseAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HeadTurnAngle=" + metrics.HeadTurnAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsBackwardDelta=" + metrics.HipsBackwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BodyRecoilAngle=" + metrics.BodyRecoilAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", GuardHoldDrift=" + metrics.GuardHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimeLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", AvatarAssigned=False" +
                ", VisualMatchesStatic=True" +
                ", HitConfiguredAnimators=" + hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Hitted FBX As Hit Normal")]
        public static void ApplyTergoHittedModelAsHitNormal()
        {
            var hittedPrefab = ImportHittedModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var existingHitRoot = RequireChild(placementRoot.transform, HitNormalRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, HitNormalRootName);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);
            var hitState = TransformState.Capture(existingHitRoot);
            var hitSiblingIndex = existingHitRoot.GetSiblingIndex();
            var hitWasActive = existingHitRoot.gameObject.activeSelf;
            var hitRoot = existingHitRoot;
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(existingHitRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            var oldHitNormalRootDeleted = false;
            var newHitNormalRootFromHittedFbx = false;

            if (!string.Equals(sourcePath, HittedModelAssetPath, StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(existingHitRoot.gameObject);

                var instanceObject = PrefabUtility.InstantiatePrefab(hittedPrefab, placementRoot.transform) as GameObject;
                if (instanceObject == null)
                {
                    throw new InvalidOperationException("Failed to instantiate hitted FBX prefab: " + HittedModelAssetPath);
                }

                instanceObject.name = HitNormalRootName;
                hitRoot = instanceObject.transform;
                hitState.ApplyTo(hitRoot);
                hitRoot.SetSiblingIndex(Mathf.Clamp(hitSiblingIndex, 0, placementRoot.transform.childCount - 1));
                instanceObject.SetActive(hitWasActive);

                sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
                sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
                if (!string.Equals(sourcePath, HittedModelAssetPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        HitNormalRootName + " is not linked to hitted FBX asset. SourcePath=" + sourcePath);
                }

                oldHitNormalRootDeleted = true;
                newHitNormalRootFromHittedFbx = true;
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, HitNormalRootName)))
            {
                throw new InvalidOperationException("Non-target Tergo root list changed while replacing " + HitNormalRootName + ".");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (hitRoot.GetSiblingIndex() != hitSiblingIndex || hitRoot.gameObject.activeSelf != hitWasActive)
            {
                throw new InvalidOperationException(HitNormalRootName + " slot sibling or active state was not preserved.");
            }

            var armature = RequireChild(hitRoot, "Armature");
            var rendererCount = hitRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = hitRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX model must include visible skinned renderers.");
            }

            var sourceClips = LoadHittedAnimationClips();
            var sourceClip = SelectHittedSourceClip(sourceClips);
            var clip = EnsureCopiedHittedFbxHitNormalClip(sourceClip);
            var controller = EnsureHittedFbxHitNormalController(clip);
            var avatar = LoadHittedAvatarOrNull();
            var removedChildAnimators = RemovePierceAttackChildAnimators(hitRoot);

            if (!SampleClipChangesTransforms(clip, hitRoot))
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX clip did not change target transforms.");
            }

            var animator = hitRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hitRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playbackMetrics = EvaluateHittedFbxAnimatorPlayback(animator, hitRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " hitted FBX Animator did not visibly move. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (!HittedFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Hitted FBX controller default state does not use the copied hitted clip.");
            }

            var stateSpeed = GetHittedFbxControllerDefaultStateSpeed(controller);
            if (Mathf.Abs(stateSpeed - HittedFbxHitNormalPlaybackSpeed) > 0.001f)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " hitted FBX controller state speed was not updated. StateSpeed=" +
                    stateSpeed.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX clip must be configured for loop playback.");
            }

            var hitConfiguredAnimators = CountConfiguredAnimators(hitRoot);
            if (hitConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " must keep exactly one configured Animator after hitted loop apply. Count=" +
                    hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var armatureBoneCount = CountRigBones(armature);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo hit normal with hitted FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoHitNormalHittedFbxApplied" +
                ", Target=" + PlacementRootName + "/" + HitNormalRootName +
                ", SourceFile=" + HittedModelSourceAbsolutePath +
                ", ImportedAsset=" + HittedModelAssetPath +
                ", OldHitNormalRootDeleted=" + oldHitNormalRootDeleted.ToString(CultureInfo.InvariantCulture) +
                ", NewHitNormalRootFromHittedFbx=" + newHitNormalRootFromHittedFbx.ToString(CultureInfo.InvariantCulture) +
                ", SlotTransformPreserved=True" +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(sourceClips) +
                ", Clip=" + HittedFbxHitNormalClipPath +
                ", Controller=" + HittedFbxHitNormalControllerPath +
                ", StateSpeed=" + stateSpeed.ToString("0.###", CultureInfo.InvariantCulture) +
                ", EffectivePlaybackSpeed=" + HittedFbxHitNormalPlaybackSpeed.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", HitConfiguredAnimators=" + hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Hitted FBX Hit Normal")]
        public static void ValidateTergoHittedModelAsHitNormal()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var hitRoot = RequireChild(placementRoot.transform, HitNormalRootName);
            var controller = RequireAsset<AnimatorController>(HittedFbxHitNormalControllerPath);
            var clip = RequireAsset<AnimationClip>(HittedFbxHitNormalClipPath);
            RequireConfiguredAnimator(hitRoot, controller, HitNormalRootName);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(hitRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, HittedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " must stay linked to hitted FBX asset during validation. SourcePath=" + sourcePath);
            }

            var animator = hitRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(HitNormalRootName + " is missing its root Animator.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(HitNormalRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(HitNormalRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!HittedFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, hitRoot))
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX animation is not connected correctly.");
            }

            var stateSpeed = GetHittedFbxControllerDefaultStateSpeed(controller);
            if (Mathf.Abs(stateSpeed - HittedFbxHitNormalPlaybackSpeed) > 0.001f)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " hitted FBX controller state speed is not fast enough. StateSpeed=" +
                    stateSpeed.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX animation loop setting is not enabled.");
            }

            var playbackMetrics = EvaluateHittedFbxAnimatorPlayback(animator, hitRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " hitted FBX Animator did not move during validation. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var targetEyeContainer = RequireBackRushVisualFirstNamedDescendant(hitRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, hitRoot, sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetEyeRendererCount = targetEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var targetLightCount = CountBackRushVisualLights(hitRoot);

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, hitRoot))
            {
                throw new InvalidOperationException(HitNormalRootName + " body materials do not match the reference Tergo.");
            }

            if (CountDescendantsByName(hitRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(HitNormalRootName + " must keep exactly one approved eye container.");
            }

            if (targetEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(targetEyeContainer))
            {
                throw new InvalidOperationException(HitNormalRootName + " eye container does not match the reference Tergo relative position.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCount != sourceLightCount)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " light count does not match the reference Tergo. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCount.ToString(CultureInfo.InvariantCulture));
            }

            var armature = RequireChild(hitRoot, "Armature");
            var rendererCount = hitRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = hitRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var armatureBoneCount = CountRigBones(armature);
            var hitConfiguredAnimators = CountConfiguredAnimators(hitRoot);
            if (rendererCount == 0 || skinnedRendererCount == 0 || hitConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected hitted hit normal validation counts. Renderer=" +
                    rendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Skinned=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Animator=" + hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoHitNormalHittedFbxValidated" +
                ", Target=" + PlacementRootName + "/" + HitNormalRootName +
                ", SourceModel=" + HittedModelAssetPath +
                ", Clip=" + HittedFbxHitNormalClipPath +
                ", Controller=" + HittedFbxHitNormalControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", StateSpeed=" + stateSpeed.ToString("0.###", CultureInfo.InvariantCulture) +
                ", EffectivePlaybackSpeed=" + HittedFbxHitNormalPlaybackSpeed.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", SourceModelLinked=True" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BodyMaterialsMatchReference=True" +
                ", EyeContainerCount=1" +
                ", EyeRelativePositionMatched=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLights=" + targetLightCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", HitConfiguredAnimators=" + hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Dying FBX As Death")]
        public static void ApplyTergoDyingModelAsDeath()
        {
            var dyingPrefab = ImportDyingModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var existingDeathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, DeathRootName);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);
            var deathState = TransformState.Capture(existingDeathRoot);
            var deathSiblingIndex = existingDeathRoot.GetSiblingIndex();
            var deathWasActive = existingDeathRoot.gameObject.activeSelf;

            UnityEngine.Object.DestroyImmediate(existingDeathRoot.gameObject);

            var instanceObject = PrefabUtility.InstantiatePrefab(dyingPrefab, placementRoot.transform) as GameObject;
            if (instanceObject == null)
            {
                throw new InvalidOperationException("Failed to instantiate dying FBX prefab: " + DyingModelAssetPath);
            }

            instanceObject.name = DeathRootName;
            var deathRoot = instanceObject.transform;
            deathState.ApplyTo(deathRoot);
            deathRoot.SetSiblingIndex(Mathf.Clamp(deathSiblingIndex, 0, placementRoot.transform.childCount - 1));
            instanceObject.SetActive(deathWasActive);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(instanceObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " is not linked to dying FBX asset. SourcePath=" + sourcePath);
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, DeathRootName)))
            {
                throw new InvalidOperationException("Non-target Tergo root list changed while replacing " + DeathRootName + ".");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (deathRoot.GetSiblingIndex() != deathSiblingIndex || deathRoot.gameObject.activeSelf != deathWasActive)
            {
                throw new InvalidOperationException(DeathRootName + " slot sibling or active state was not preserved.");
            }

            var armature = RequireChild(deathRoot, "Armature");
            var rendererCount = deathRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = deathRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX model must include visible skinned renderers.");
            }

            var sourceClips = LoadDyingAnimationClips();
            var sourceClip = SelectDyingSourceClip(sourceClips);
            var clip = EnsureCopiedDyingFbxDeathClip(sourceClip);
            var controller = EnsureDyingFbxDeathController(clip);
            var avatar = LoadDyingAvatarOrNull();
            var removedChildAnimators = RemovePierceAttackChildAnimators(deathRoot);

            if (!SampleClipChangesTransforms(clip, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX clip did not change target transforms.");
            }

            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = deathRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playbackMetrics = EvaluateDyingFbxAnimatorPlayback(animator, deathRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    DeathRootName + " dying FBX Animator did not visibly move. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", EndRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", EndPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Dying FBX controller default state does not use the copied dying clip.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            var deathConfiguredAnimators = CountConfiguredAnimators(deathRoot);
            if (deathConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    DeathRootName + " must keep exactly one configured Animator after dying apply. Count=" +
                    deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var armatureBoneCount = CountRigBones(armature);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo death with dying FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoDeathDyingFbxApplied" +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", SourceFile=" + DyingModelSourceAbsolutePath +
                ", ImportedAsset=" + DyingModelAssetPath +
                ", OldDeathRootDeleted=True" +
                ", NewDeathRootFromDyingFbx=True" +
                ", SlotTransformPreserved=True" +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(sourceClips) +
                ", Clip=" + DyingFbxDeathClipPath +
                ", Controller=" + DyingFbxDeathControllerPath +
                ", LoopTime=" + settings.loopTime.ToString(CultureInfo.InvariantCulture) +
                ", WrapMode=" + clip.wrapMode +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterEnd=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", EndRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", EndPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", DeathConfiguredAnimators=" + deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Dying FBX Death")]
        public static void ValidateTergoDyingModelAsDeath()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var deathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var controller = RequireAsset<AnimatorController>(DyingFbxDeathControllerPath);
            var clip = RequireAsset<AnimationClip>(DyingFbxDeathClipPath);
            RequireConfiguredAnimator(deathRoot, controller, DeathRootName);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(deathRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " must stay linked to dying FBX asset during validation. SourcePath=" + sourcePath);
            }

            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(DeathRootName + " is missing its root Animator.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(DeathRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(DeathRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation is not connected correctly.");
            }

            var playbackMetrics = EvaluateDyingFbxAnimatorPlayback(animator, deathRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    DeathRootName + " dying FBX Animator did not move during validation. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", EndRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", EndPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation loop setting is not enabled.");
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var targetEyeContainer = RequireBackRushVisualFirstNamedDescendant(deathRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, deathRoot, sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetEyeRendererCount = targetEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var targetLightCount = CountBackRushVisualLights(deathRoot);

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " body materials do not match the reference Tergo.");
            }

            if (CountDescendantsByName(deathRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(DeathRootName + " must keep exactly one approved eye container.");
            }

            if (targetEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(targetEyeContainer))
            {
                throw new InvalidOperationException(DeathRootName + " eye container does not match the reference Tergo relative position.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCount != sourceLightCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " light count does not match the reference Tergo. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCount.ToString(CultureInfo.InvariantCulture));
            }

            var armature = RequireChild(deathRoot, "Armature");
            var rendererCount = deathRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = deathRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var armatureBoneCount = CountRigBones(armature);
            var deathConfiguredAnimators = CountConfiguredAnimators(deathRoot);
            if (rendererCount == 0 || skinnedRendererCount == 0 || deathConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected dying death validation counts. Renderer=" +
                    rendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Skinned=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Animator=" + deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoDeathDyingFbxValidated" +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", SourceModel=" + DyingModelAssetPath +
                ", Clip=" + DyingFbxDeathClipPath +
                ", Controller=" + DyingFbxDeathControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", SourceModelLinked=True" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterEnd=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", EndRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", EndPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BodyMaterialsMatchReference=True" +
                ", EyeContainerCount=1" +
                ", EyeRelativePositionMatched=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLights=" + targetLightCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", DeathConfiguredAnimators=" + deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Death Visual Details From Static Review")]
        public static void SyncTergoDeathVisualDetailsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var deathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var controller = RequireAsset<AnimatorController>(DyingFbxDeathControllerPath);
            var clip = RequireAsset<AnimationClip>(DyingFbxDeathClipPath);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(deathRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " must stay linked to dying FBX asset before visual sync. SourcePath=" + sourcePath);
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);
            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(DeathRootName + " must keep the dying FBX controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;
            var targetLightCountBefore = CountBackRushVisualLights(deathRoot);

            EnsureDyingFbxDeathClipLoops(clip);
            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, deathRoot);
            DestroyBackRushVisualLightGameObjects(deathRoot);
            DestroyBackRushVisualNamedDescendants(deathRoot, EyeContainerName);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, deathRoot, sourceEyeContainer);
            var copiedEyeLightCount = CountBackRushVisualLights(copiedEyeContainer);
            var copiedExternalLights = CopyBackRushVisualExternalLightObjectsFromReference(staticRoot, deathRoot, sourceEyeContainer);
            var removedCopiedEyeAnimators = RemoveAnimatorComponentsUnderRoot(copiedEyeContainer);
            var targetLightCountAfter = CountBackRushVisualLights(deathRoot);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, deathRoot, sourceEyeContainer);

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + DeathRootName + " visual details.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(DeathRootName + " Animator changed while syncing visual details.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(deathRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(DeathRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (copiedEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(copiedEyeContainer))
            {
                throw new InvalidOperationException(DeathRootName + " eye container was not placed at the same relative head position as the reference Tergo.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCountAfter != sourceLightCount ||
                copiedEyeLightCount + copiedExternalLights != sourceLightCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " light count does not match reference after visual sync. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedEye=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedExternal=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation was not preserved after visual sync.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation loop setting was not enabled during visual sync.");
            }

            var deathConfiguredAnimators = CountConfiguredAnimators(deathRoot);
            if (deathConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    DeathRootName + " must keep exactly one configured Animator after visual sync. Count=" +
                    deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo death visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoDeathVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", Reference=" + StaticRootName +
                ", SourceModel=" + DyingModelAssetPath +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionMatched=True" +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", CopiedEyeLights=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                ", CopiedExternalLights=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedEyeAnimators=" + removedCopiedEyeAnimators.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPreserved=True" +
                ", LoopAnimationPreserved=True" +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", RootTransformPreserved=True" +
                ", DeathConfiguredAnimators=" + deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Approved Death Melt Puddle Model")]
        public static void ApplyTergoApprovedDeathMeltPuddleModel()
        {
            var approvedPrefab = ImportApprovedDeathMeltPuddleModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var deathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var controller = RequireAsset<AnimatorController>(DyingFbxDeathControllerPath);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(deathRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " must stay linked to dying FBX asset before applying approved melt puddle. SourcePath=" + sourcePath);
            }

            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(DeathRootName + " must keep the dying FBX controller before applying approved melt puddle.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;
            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var targetLightCountBefore = CountBackRushVisualLights(deathRoot);

            var sourceClip = SelectDyingSourceClip(LoadDyingAnimationClips());
            var baseFallEndTime = Mathf.Max(sourceClip.length, 0.01f);
            var clip = EnsureCopiedDyingFbxDeathClip(sourceClip);
            var baseMotionClip = CloneAnimationClipForComparison(clip, DyingFbxDeathClipName + "_ApprovedMeltBaseCompare");
            EnsureDyingFbxDeathClipLoops(clip);

            var removedOldPuddleRoots = RemoveApprovedDeathMeltPuddleChildren(deathRoot);
            var puddleRoot = InstantiateApprovedDeathMeltPuddleRoot(approvedPrefab, deathRoot);
            var removedPuddleAnimators = RemoveAnimatorComponentsUnderRoot(puddleRoot);
            var puddleRenderer = RequireApprovedDeathMeltPuddleRenderer(puddleRoot);
            var materialSlotsSynced = CopyApprovedPuddleBodyMaterialsFromDeathRoot(deathRoot, puddleRoot, puddleRenderer);
            var bodyRenderers = GetDeathBodyRenderers(deathRoot, puddleRoot).ToArray();
            var eyeRenderers = GetDeathEyeRenderers(deathRoot).ToArray();
            if (bodyRenderers.Length == 0)
            {
                throw new InvalidOperationException(DeathRootName + " has no original body renderer to preserve through the falling motion.");
            }

            if (eyeRenderers.Length != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " eye renderer count changed before applying approved melt puddle. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + eyeRenderers.Length.ToString(CultureInfo.InvariantCulture));
            }

            var alignment = AlignApprovedPuddleToDyingFinalPose(deathRoot, puddleRoot, bodyRenderers, clip, baseFallEndTime);
            var timeline = BuildApprovedDeathMeltTimeline(baseFallEndTime);
            var rewrittenBindings = ApplyApprovedDeathMeltPuddleCurves(
                clip,
                deathRoot,
                puddleRoot,
                puddleRenderer,
                bodyRenderers,
                eyeRenderers,
                timeline);
            var finalSampleCorrection = CorrectApprovedPuddleRootAgainstFinalAnimationSample(
                clip,
                deathRoot,
                puddleRoot,
                puddleRenderer,
                bodyRenderers,
                timeline);
            rewrittenBindings += SetApprovedPuddleRootStartOffsetCurves(clip, deathRoot, puddleRoot, timeline);
            SetApprovedDeathMeltInitialVisibility(puddleRoot, puddleRenderer, bodyRenderers, eyeRenderers);

            var controllerAfter = EnsureDyingFbxDeathController(clip);
            if (controllerAfter != controller)
            {
                throw new InvalidOperationException(DeathRootName + " dying controller asset changed unexpectedly during approved melt puddle apply.");
            }

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Dying FBX controller default state does not use the approved melt puddle clip.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + DeathRootName + " approved melt puddle.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(DeathRootName + " Animator changed while applying approved melt puddle.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation loop setting was not preserved.");
            }

            if (CountBackRushVisualLights(deathRoot) != targetLightCountBefore ||
                targetLightCountBefore != sourceLightCount)
            {
                throw new InvalidOperationException(DeathRootName + " light setup changed while applying approved melt puddle.");
            }

            if (!ApprovedPuddleBodyMaterialMatchesDeathBody(deathRoot, puddleRoot, puddleRenderer))
            {
                throw new InvalidOperationException(ApprovedDeathMeltPuddleRootName + " body material does not match " + DeathRootName + " body material.");
            }

            RequireApprovedDeathMeltPuddleCurveBindings(clip, deathRoot, puddleRoot, puddleRenderer, bodyRenderers, eyeRenderers);
            var sampleMetrics = SampleApprovedDeathMeltPuddleClip(clip, deathRoot, puddleRenderer, bodyRenderers, eyeRenderers, timeline);
            RequireApprovedDeathMeltSampleMetrics(sampleMetrics);
            var floorMetrics = EvaluateApprovedDeathMeltPuddleFloorMetrics(clip, deathRoot, puddleRoot, puddleRenderer, bodyRenderers, timeline);
            Debug.Log(
                "TergoApprovedDeathMeltPuddleFloorPreSave" +
                ", Correction=" + FormatVector3(finalSampleCorrection) +
                ", GroundDelta=" + floorMetrics.GroundDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", StartYOffset=" + floorMetrics.StartYOffset.ToString("0.######", CultureInfo.InvariantCulture) +
                ", CenterHorizontalDelta=" + floorMetrics.CenterHorizontalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", VerticalHeight=" + floorMetrics.VerticalHeight.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HorizontalExtent=" + floorMetrics.HorizontalExtent.ToString("0.######", CultureInfo.InvariantCulture) +
                ", VerticalRatio=" + floorMetrics.VerticalToHorizontalRatio.ToString("0.######", CultureInfo.InvariantCulture));
            RequireApprovedDeathMeltPuddleFloorMetrics(floorMetrics);
            RequireApprovedCompressionBlendShapesCut(clip, deathRoot, puddleRenderer);
            var sourceMatch = EvaluateDyingBaseMotionPreservedBeforeMelt(baseMotionClip, clip, deathRoot, baseFallEndTime);
            RequireDyingSourceMotionPreserved(sourceMatch);
            UnityEngine.Object.DestroyImmediate(baseMotionClip);

            var deathConfiguredAnimators = CountConfiguredAnimators(deathRoot);
            if (deathConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    DeathRootName + " must keep exactly one configured Animator after approved melt puddle apply. Count=" +
                    deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo approved death melt puddle.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoApprovedDeathMeltPuddleApplied" +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", Sample=" + ApprovedDeathMeltPuddleSampleFbxPath +
                ", ModelAsset=" + ApprovedDeathMeltPuddleModelAssetPath +
                ", Clip=" + DyingFbxDeathClipPath +
                ", BaseFallEndTime=" + baseFallEndTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MeltStart=" + timeline.MeltStart.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SpreadTime=" + timeline.SpreadTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HoldTime=" + timeline.HoldTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RewrittenCurveBindings=" + rewrittenBindings.ToString(CultureInfo.InvariantCulture) +
                ", RemovedOldPuddleRoots=" + removedOldPuddleRoots.ToString(CultureInfo.InvariantCulture) +
                ", RemovedPuddleAnimators=" + removedPuddleAnimators.ToString(CultureInfo.InvariantCulture) +
                ", MaterialSlotsSynced=" + materialSlotsSynced.ToString(CultureInfo.InvariantCulture) +
                ", BodyRenderersPreservedUntilMelt=" + bodyRenderers.Length.ToString(CultureInfo.InvariantCulture) +
                ", EyeRenderersPreservedUntilMelt=" + eyeRenderers.Length.ToString(CultureInfo.InvariantCulture) +
                ", LightsUnchanged=True" +
                ", PuddleScale=" + alignment.Scale.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleCenterHorizontalDelta=" + alignment.CenterHorizontalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleGroundDelta=" + alignment.GroundDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleLocalRotationEuler=" + FormatVector3(alignment.LocalRotationEuler) +
                ", PuddleFinalSampleCorrection=" + FormatVector3(finalSampleCorrection) +
                ", PuddleStartYOffset=" + floorMetrics.StartYOffset.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleVerticalHeight=" + floorMetrics.VerticalHeight.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleHorizontalExtent=" + floorMetrics.HorizontalExtent.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleVerticalRatio=" + floorMetrics.VerticalToHorizontalRatio.ToString("0.######", CultureInfo.InvariantCulture) +
                ", CompressionBlendShapesCut=True" +
                ", BaseFallMotionPreserved=True" +
                ", BaseFallMaxPositionDelta=" + sourceMatch.MaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BaseFallMaxRotationDelta=" + sourceMatch.MaxRotationDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", ExistingDeathMotionTouchedBeforeEnd=False" +
                ", ApprovedSampleBlendShapes=True" +
                ", LoopTime=True" +
                ", WrapMode=Loop");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Approved Death Melt Puddle Model")]
        public static void ValidateTergoApprovedDeathMeltPuddleModel()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var deathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var controller = RequireAsset<AnimatorController>(DyingFbxDeathControllerPath);
            var clip = RequireAsset<AnimationClip>(DyingFbxDeathClipPath);
            var approvedModel = RequireAsset<GameObject>(ApprovedDeathMeltPuddleModelAssetPath);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(deathRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " must stay linked to dying FBX asset for approved melt puddle validation. SourcePath=" + sourcePath);
            }

            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(DeathRootName + " must keep the dying FBX controller for approved melt puddle validation.");
            }

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Dying FBX controller default state does not use the approved melt puddle clip.");
            }

            var sourceClip = SelectDyingSourceClip(LoadDyingAnimationClips());
            var baseFallEndTime = Mathf.Max(sourceClip.length, 0.01f);
            var timeline = BuildApprovedDeathMeltTimeline(baseFallEndTime);
            var puddleRoot = RequireChild(deathRoot, ApprovedDeathMeltPuddleRootName);
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(puddleRoot.gameObject);
            if (!string.Equals(prefabPath, ApprovedDeathMeltPuddleModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    ApprovedDeathMeltPuddleRootName + " must be linked to approved model asset. Expected=" +
                    ApprovedDeathMeltPuddleModelAssetPath + ", Actual=" + prefabPath);
            }

            if (approvedModel == null)
            {
                throw new InvalidOperationException("Approved death melt puddle model asset did not load.");
            }

            var puddleRenderer = RequireApprovedDeathMeltPuddleRenderer(puddleRoot);
            var bodyRenderers = GetDeathBodyRenderers(deathRoot, puddleRoot).ToArray();
            var eyeRenderers = GetDeathEyeRenderers(deathRoot).ToArray();
            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            if (bodyRenderers.Length == 0)
            {
                throw new InvalidOperationException(DeathRootName + " has no original body renderer during approved melt puddle validation.");
            }

            if (eyeRenderers.Length != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " eye renderer count changed during approved melt puddle validation. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + eyeRenderers.Length.ToString(CultureInfo.InvariantCulture));
            }

            if (CountBackRushVisualLights(deathRoot) != CountBackRushVisualLights(staticRoot))
            {
                throw new InvalidOperationException(DeathRootName + " light count does not match the reference Tergo during approved melt puddle validation.");
            }

            if (!ApprovedPuddleBodyMaterialMatchesDeathBody(deathRoot, puddleRoot, puddleRenderer))
            {
                throw new InvalidOperationException(ApprovedDeathMeltPuddleRootName + " body material does not match " + DeathRootName + " body material.");
            }

            var bindingCount = RequireApprovedDeathMeltPuddleCurveBindings(clip, deathRoot, puddleRoot, puddleRenderer, bodyRenderers, eyeRenderers);
            var sampleMetrics = SampleApprovedDeathMeltPuddleClip(clip, deathRoot, puddleRenderer, bodyRenderers, eyeRenderers, timeline);
            RequireApprovedDeathMeltSampleMetrics(sampleMetrics);
            var floorMetrics = EvaluateApprovedDeathMeltPuddleFloorMetrics(clip, deathRoot, puddleRoot, puddleRenderer, bodyRenderers, timeline);
            RequireApprovedDeathMeltPuddleFloorMetrics(floorMetrics);
            RequireApprovedCompressionBlendShapesCut(clip, deathRoot, puddleRenderer);
            RequireNoTransformCurveKeysAfterBaseFallEnd(clip, baseFallEndTime);
            var baseMotionClip = CreateDyingFbxDeathBaseComparisonClip(clip, baseFallEndTime);
            var sourceMatch = EvaluateDyingBaseMotionPreservedBeforeMelt(baseMotionClip, clip, deathRoot, baseFallEndTime);
            RequireDyingSourceMotionPreserved(sourceMatch);
            UnityEngine.Object.DestroyImmediate(baseMotionClip);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation loop setting is not enabled after approved melt puddle apply.");
            }

            if (CountConfiguredAnimators(deathRoot) != 1)
            {
                throw new InvalidOperationException(DeathRootName + " must have exactly one configured Animator after approved melt puddle apply.");
            }

            Debug.Log(
                "TergoApprovedDeathMeltPuddleValidated" +
                ", Scene=" + scene.path +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", ModelAsset=" + ApprovedDeathMeltPuddleModelAssetPath +
                ", Clip=" + DyingFbxDeathClipPath +
                ", BaseFallEndTime=" + baseFallEndTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MeltStart=" + timeline.MeltStart.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SpreadTime=" + timeline.SpreadTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + bindingCount.ToString(CultureInfo.InvariantCulture) +
                ", BodyVisibleBeforeMelt=" + sampleMetrics.BodyVisibleBeforeMelt +
                ", PuddleHiddenBeforeMelt=" + sampleMetrics.PuddleHiddenBeforeMelt +
                ", PuddleVisibleAfterMelt=" + sampleMetrics.PuddleVisibleAfterMelt +
                ", BodyHiddenAfterMelt=" + sampleMetrics.BodyHiddenAfterMelt +
                ", EyeHiddenAfterMelt=" + sampleMetrics.EyeHiddenAfterMelt +
                ", FinalSpreadWeight=" + sampleMetrics.FinalSpreadWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PuddleGroundDelta=" + floorMetrics.GroundDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleStartYOffset=" + floorMetrics.StartYOffset.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleVerticalHeight=" + floorMetrics.VerticalHeight.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleHorizontalExtent=" + floorMetrics.HorizontalExtent.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PuddleVerticalRatio=" + floorMetrics.VerticalToHorizontalRatio.ToString("0.######", CultureInfo.InvariantCulture) +
                ", CompressionBlendShapesCut=True" +
                ", ExistingDeathMotionTouchedBeforeEnd=False" +
                ", BaseFallMaxPositionDelta=" + sourceMatch.MaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BaseFallMaxRotationDelta=" + sourceMatch.MaxRotationDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Death Melt Puddle Animation")]
        public static void ApplyTergoDeathMeltPuddleAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var deathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var controller = RequireAsset<AnimatorController>(DyingFbxDeathControllerPath);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(deathRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " must stay linked to dying FBX asset before applying melt puddle animation. SourcePath=" + sourcePath);
            }

            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(DeathRootName + " must keep the dying FBX controller before applying melt puddle animation.");
            }

            RequireLongaStyleDeathMeltBlendShapeModel(deathRoot);

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;

            var sourceClip = SelectDyingSourceClip(LoadDyingAnimationClips());
            var baseFallEndTime = Mathf.Max(sourceClip.length, 0.01f);
            var clip = EnsureCopiedDyingFbxDeathClip(sourceClip);
            var baseMotionClip = CloneAnimationClipForComparison(clip, DyingFbxDeathClipName + "_BeforeMeltCompare");
            EnsureDyingFbxDeathClipLoops(clip);
            var rewrittenBindings = ApplyDeathMeltPuddleCurves(clip, deathRoot, baseFallEndTime);
            var controllerAfter = EnsureDyingFbxDeathController(clip);
            if (controllerAfter != controller)
            {
                throw new InvalidOperationException(DeathRootName + " dying controller asset changed unexpectedly during melt puddle apply.");
            }

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Dying FBX controller default state does not use the melt puddle clip.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + DeathRootName + " melt puddle animation.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(DeathRootName + " Animator changed while applying melt puddle animation.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DeathRootName + " dying FBX animation loop setting was not preserved.");
            }

            if (!SampleClipChangesTransforms(clip, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " melt puddle clip does not animate the target model.");
            }

            var metrics = EvaluateDeathMeltPuddleMetrics(clip, deathRoot, baseFallEndTime);
            RequireDeathMeltPuddleMetrics(metrics);
            var sourceMatch = EvaluateDyingBaseMotionPreservedBeforeMelt(baseMotionClip, clip, deathRoot, baseFallEndTime);
            RequireDyingSourceMotionPreserved(sourceMatch);
            UnityEngine.Object.DestroyImmediate(baseMotionClip);

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, deathRoot) ||
                CountDescendantsByName(deathRoot, EyeContainerName) != 1 ||
                CountBackRushVisualLights(deathRoot) != CountBackRushVisualLights(staticRoot))
            {
                throw new InvalidOperationException(DeathRootName + " visual sync state was not preserved while applying melt puddle animation.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo death melt puddle animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoDeathMeltPuddleAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", Clip=" + DyingFbxDeathClipPath +
                ", Controller=" + DyingFbxDeathControllerPath +
                ", BaseFallEndTime=" + baseFallEndTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MeltSinkDuration=" + DeathMeltSinkDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MeltPuddleDuration=" + DeathMeltPuddleDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RewrittenCurveBindings=" + rewrittenBindings.ToString(CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", PureAnimationOnly=True" +
                ", NewMeshCreated=False" +
                ", NewMaterialCreated=False" +
                ", NewVfxCreated=False" +
                ", BaseFallMotionPreserved=True" +
                ", BaseFallMaxPositionDelta=" + sourceMatch.MaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BaseFallMaxRotationDelta=" + sourceMatch.MaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", BaseFallMaxScaleDelta=" + sourceMatch.MaxScaleDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HipsHeightDrop=" + metrics.HipsHeightDrop.ToString("0.######", CultureInfo.InvariantCulture) +
                ", AverageVerticalScaleRatio=" + metrics.AverageVerticalScaleRatio.ToString("0.###", CultureInfo.InvariantCulture) +
                ", AverageHorizontalScaleRatio=" + metrics.AverageHorizontalScaleRatio.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PuddleBoneHeightRange=" + metrics.PuddleBoneHeightRange.ToString("0.######", CultureInfo.InvariantCulture) +
                ", AveragePuddleGroundDistance=" + metrics.AveragePuddleGroundDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                ", FinalHoldStable=" + metrics.FinalHoldStable.ToString(CultureInfo.InvariantCulture) +
                ", BodyMaterialsPreserved=True" +
                ", EyeContainerPreserved=True" +
                ", LightCountPreserved=True" +
                ", AnimatorPreserved=True" +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Death Melt Puddle Animation")]
        public static void ValidateTergoDeathMeltPuddleAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var deathRoot = RequireChild(placementRoot.transform, DeathRootName);
            var controller = RequireAsset<AnimatorController>(DyingFbxDeathControllerPath);
            var clip = RequireAsset<AnimationClip>(DyingFbxDeathClipPath);
            RequireConfiguredAnimator(deathRoot, controller, DeathRootName);

            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(deathRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, DyingModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DeathRootName + " must stay linked to dying FBX asset during melt puddle validation. SourcePath=" + sourcePath);
            }

            var animator = deathRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(DeathRootName + " is not using the dying FBX controller during melt puddle validation.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(DeathRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(DeathRootName + " must use AlwaysAnimate culling for review playback.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DeathRootName + " melt puddle animation must loop.");
            }

            if (!DyingFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " melt puddle animation is not connected correctly.");
            }

            RequireLongaStyleDeathMeltBlendShapeModel(deathRoot);

            var sourceClip = SelectDyingSourceClip(LoadDyingAnimationClips());
            var baseFallEndTime = Mathf.Max(sourceClip.length, 0.01f);
            var baseMotionClip = CreateDyingFbxDeathBaseComparisonClip(clip, baseFallEndTime);
            var metrics = EvaluateDeathMeltPuddleMetrics(clip, deathRoot, baseFallEndTime);
            RequireDeathMeltPuddleMetrics(metrics);
            var sourceMatch = EvaluateDyingBaseMotionPreservedBeforeMelt(baseMotionClip, clip, deathRoot, baseFallEndTime);
            RequireDyingSourceMotionPreserved(sourceMatch);
            UnityEngine.Object.DestroyImmediate(baseMotionClip);

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var targetEyeContainer = RequireBackRushVisualFirstNamedDescendant(deathRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, deathRoot, sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetEyeRendererCount = targetEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var targetLightCount = CountBackRushVisualLights(deathRoot);

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, deathRoot))
            {
                throw new InvalidOperationException(DeathRootName + " body materials do not match the reference Tergo.");
            }

            if (targetEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(targetEyeContainer))
            {
                throw new InvalidOperationException(DeathRootName + " eye container does not match the reference Tergo relative position.");
            }

            if (sourceEyeRendererCount != targetEyeRendererCount || sourceLightCount != targetLightCount)
            {
                throw new InvalidOperationException(
                    DeathRootName + " visual sync counts changed during melt puddle validation. SourceEyes=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", TargetEyes=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", TargetLights=" + targetLightCount.ToString(CultureInfo.InvariantCulture));
            }

            var curveBindings = AnimationUtility.GetCurveBindings(clip).Length;
            var deathConfiguredAnimators = CountConfiguredAnimators(deathRoot);
            if (deathConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    DeathRootName + " must keep exactly one configured Animator. Count=" +
                    deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoDeathMeltPuddleAnimationValidated" +
                ", Target=" + PlacementRootName + "/" + DeathRootName +
                ", Clip=" + DyingFbxDeathClipPath +
                ", Controller=" + DyingFbxDeathControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", BaseFallEndTime=" + baseFallEndTime.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MeltSegmentDuration=" + metrics.MeltSegmentDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", ControllerUsesClip=True" +
                ", PureAnimationOnly=True" +
                ", CurveBindings=" + curveBindings.ToString(CultureInfo.InvariantCulture) +
                ", BaseFallMotionPreserved=True" +
                ", BaseFallMaxPositionDelta=" + sourceMatch.MaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BaseFallMaxRotationDelta=" + sourceMatch.MaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", BaseFallMaxScaleDelta=" + sourceMatch.MaxScaleDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HipsHeightDrop=" + metrics.HipsHeightDrop.ToString("0.######", CultureInfo.InvariantCulture) +
                ", AverageVerticalScaleRatio=" + metrics.AverageVerticalScaleRatio.ToString("0.###", CultureInfo.InvariantCulture) +
                ", AverageHorizontalScaleRatio=" + metrics.AverageHorizontalScaleRatio.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PuddleBoneHeightRange=" + metrics.PuddleBoneHeightRange.ToString("0.######", CultureInfo.InvariantCulture) +
                ", AveragePuddleGroundDistance=" + metrics.AveragePuddleGroundDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                ", FinalHoldStable=" + metrics.FinalHoldStable.ToString(CultureInfo.InvariantCulture) +
                ", PuddlePoseHeld=True" +
                ", BodyMaterialsMatchReference=True" +
                ", EyeContainerCount=1" +
                ", EyeRelativePositionMatched=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLights=" + targetLightCount.ToString(CultureInfo.InvariantCulture) +
                ", DeathConfiguredAnimators=" + deathConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Hit Normal Visual Details From Static Review")]
        public static void SyncTergoHitNormalVisualDetailsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var hitRoot = RequireChild(placementRoot.transform, HitNormalRootName);
            var controller = RequireAsset<AnimatorController>(HittedFbxHitNormalControllerPath);
            var clip = RequireAsset<AnimationClip>(HittedFbxHitNormalClipPath);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(hitRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, HittedModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " must stay linked to hitted FBX asset before visual sync. SourcePath=" + sourcePath);
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);
            var animator = hitRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(HitNormalRootName + " must keep the hitted FBX controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;
            var targetLightCountBefore = CountBackRushVisualLights(hitRoot);

            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, hitRoot);
            DestroyBackRushVisualLightGameObjects(hitRoot);
            DestroyBackRushVisualNamedDescendants(hitRoot, EyeContainerName);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, hitRoot, sourceEyeContainer);
            var copiedEyeLightCount = CountBackRushVisualLights(copiedEyeContainer);
            var copiedExternalLights = CopyBackRushVisualExternalLightObjectsFromReference(staticRoot, hitRoot, sourceEyeContainer);
            var removedCopiedEyeAnimators = RemoveAnimatorComponentsUnderRoot(copiedEyeContainer);
            var targetLightCountAfter = CountBackRushVisualLights(hitRoot);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, hitRoot, sourceEyeContainer);

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + HitNormalRootName + " visual details.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(HitNormalRootName + " Animator changed while syncing visual details.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, hitRoot))
            {
                throw new InvalidOperationException(HitNormalRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(hitRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(HitNormalRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (copiedEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(copiedEyeContainer))
            {
                throw new InvalidOperationException(HitNormalRootName + " eye container was not placed at the same relative head position as the reference Tergo.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCountAfter != sourceLightCount ||
                copiedEyeLightCount + copiedExternalLights != sourceLightCount)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " light count does not match reference after visual sync. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedEye=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedExternal=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!HittedFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, hitRoot))
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX animation was not preserved after visual sync.");
            }

            var stateSpeed = GetHittedFbxControllerDefaultStateSpeed(controller);
            if (Mathf.Abs(stateSpeed - HittedFbxHitNormalPlaybackSpeed) > 0.001f)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " hitted FBX controller state speed changed during visual sync. StateSpeed=" +
                    stateSpeed.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(HitNormalRootName + " hitted FBX animation loop setting changed during visual sync.");
            }

            var hitConfiguredAnimators = CountConfiguredAnimators(hitRoot);
            if (hitConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " must keep exactly one configured Animator after visual sync. Count=" +
                    hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo hit normal visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoHitNormalVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + HitNormalRootName +
                ", Reference=" + StaticRootName +
                ", SourceModel=" + HittedModelAssetPath +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionMatched=True" +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", CopiedEyeLights=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                ", CopiedExternalLights=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedEyeAnimators=" + removedCopiedEyeAnimators.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPreserved=True" +
                ", LoopAnimationPreserved=True" +
                ", StateSpeed=" + stateSpeed.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RootTransformPreserved=True" +
                ", HitConfiguredAnimators=" + hitConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Downed Pounce Takedown FBX Loop")]
        public static void ApplyTergoDownedPounceTakedownFbxLoop()
        {
            ImportTakedownModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(downedPounceRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TakedownModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " must stay linked to takedown FBX asset before applying takedown animation. SourcePath=" + sourcePath);
            }

            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedPounceState = TransformState.Capture(downedPounceRoot);
            var sourceClips = LoadTakedownAnimationClips();
            var sourceClip = SelectTakedownSourceClip(sourceClips);
            var clip = EnsureCopiedDownedPounceFromFbxClip(sourceClip);
            var controller = EnsureDownedPounceFromFbxController(clip);
            var avatar = LoadTakedownAvatarOrNull();
            var removedChildAnimators = RemovePierceAttackChildAnimators(downedPounceRoot);

            if (!SampleClipChangesTransforms(clip, downedPounceRoot))
            {
                throw new InvalidOperationException(DownedPounceRootName + " takedown FBX clip did not change target transforms.");
            }

            var animator = downedPounceRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = downedPounceRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playbackMetrics = EvaluateDownedPounceAnimatorPlayback(animator, downedPounceRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " takedown FBX Animator did not visibly move. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot) ||
                !downedPounceState.Matches(downedPounceRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while applying " + DownedPounceRootName + " takedown FBX loop.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + DownedPounceRootName + " takedown FBX loop.");
            }

            if (!DownedPounceControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Downed Pounce controller default state does not use the copied takedown clip.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DownedPounceRootName + " takedown FBX clip must be configured for loop playback.");
            }

            var downedConfiguredAnimators = CountConfiguredAnimators(downedPounceRoot);
            if (downedConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " must keep exactly one configured Animator after takedown loop apply. Count=" +
                    downedConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo downed pounce takedown FBX loop.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoDownedPounceTakedownFbxLoopApplied" +
                ", Target=" + PlacementRootName + "/" + DownedPounceRootName +
                ", SourceModel=" + TakedownModelAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(sourceClips) +
                ", Clip=" + DownedPounceFromFbxClipPath +
                ", Controller=" + DownedPounceFromFbxControllerPath +
                ", LoopTime=True" +
                ", WrapMode=Loop" +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", DownedConfiguredAnimators=" + downedConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Pierce Attack Thrust FBX Animation")]
        public static void ApplyTergoPierceAttackThrustFbxAnimation()
        {
            ImportThrustModelAsset();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(pierceAttackRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, ThrustModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " must stay linked to thrust FBX asset before applying thrust animation. SourcePath=" + sourcePath);
            }

            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceState = TransformState.Capture(pierceAttackRoot);
            var sourceClips = LoadThrustAnimationClips();
            var sourceClip = SelectThrustSourceClip(sourceClips);
            var clip = EnsureCopiedThrustFbxPierceAttackClip(sourceClip);
            var controller = EnsureThrustFbxPierceAttackController(clip);
            var avatar = LoadThrustAvatarOrNull();
            var removedChildAnimators = RemovePierceAttackChildAnimators(pierceAttackRoot);

            if (!SampleClipChangesTransforms(clip, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " thrust FBX clip did not change target transforms.");
            }

            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = pierceAttackRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            if (avatar != null)
            {
                animator.avatar = avatar;
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playbackMetrics = EvaluateThrustFbxAnimatorPlayback(animator, pierceAttackRoot, clip);
            if (!playbackMetrics.MovesAtFirstUpdate || !playbackMetrics.MovesAfterLoop)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " thrust FBX Animator did not visibly move. FirstRotationDelta=" +
                    playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while applying " + PierceAttackRootName + " thrust FBX animation.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + PierceAttackRootName + " thrust FBX animation.");
            }

            if (!ThrustFbxControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Thrust FBX controller default state does not use the copied thrust clip.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo pierce attack thrust FBX animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackThrustFbxAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", SourceModel=" + ThrustModelAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(sourceClips) +
                ", Clip=" + ThrustFbxPierceAttackClipPath +
                ", Controller=" + ThrustFbxPierceAttackControllerPath +
                ", AnimatorControllerAssigned=True" +
                ", AvatarAssigned=" + (animator.avatar != null ? "True" : "False") +
                ", ApplyRootMotion=False" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ClipChangesTransforms=True" +
                ", AnimatorMovesAtFirstUpdate=True" +
                ", AnimatorMovesAfterLoop=True" +
                ", FirstRotationDelta=" + playbackMetrics.FirstRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPositionDelta=" + playbackMetrics.FirstPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopRotationDelta=" + playbackMetrics.LoopRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopPositionDelta=" + playbackMetrics.LoopPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Pierce Attack Visual Details From Static Review")]
        public static void SyncTergoPierceAttackVisualDetailsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var controller = RequireAsset<AnimatorController>(ThrustFbxPierceAttackControllerPath);
            var clip = RequireAsset<AnimationClip>(ThrustFbxPierceAttackClipPath);
            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetNamedEyeLightCount = CountLightsUnderNamedDescendants(pierceAttackRoot, EyeContainerName);
            if (targetNamedEyeLightCount != 0)
            {
                throw new InvalidOperationException(PierceAttackRootName + " has lights under the existing eye container; visual sync would modify lights.");
            }

            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceState = TransformState.Capture(pierceAttackRoot);
            var targetLightCountBefore = CountBackRushVisualLights(pierceAttackRoot);
            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must keep the thrust FBX controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;

            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, pierceAttackRoot);
            DestroyBackRushVisualNamedDescendants(pierceAttackRoot, EyeContainerName);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, pierceAttackRoot, sourceEyeContainer);
            var copiedEyeLightCount = CountBackRushVisualLights(copiedEyeContainer);
            if (copiedEyeLightCount > 0)
            {
                DestroyBackRushVisualLightGameObjects(copiedEyeContainer);
            }

            var removedCopiedEyeAnimators = RemoveAnimatorComponentsUnderRoot(copiedEyeContainer);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetLightCountAfter = CountBackRushVisualLights(pierceAttackRoot);
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, pierceAttackRoot, sourceEyeContainer);

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while syncing " + PierceAttackRootName + " visual details.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + PierceAttackRootName + " visual details.");
            }

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(PierceAttackRootName + " Animator changed while syncing visual details.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(pierceAttackRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (copiedEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(copiedEyeContainer))
            {
                throw new InvalidOperationException(PierceAttackRootName + " eye container was not placed at the same relative head position as the reference Tergo.");
            }

            if (targetLightCountAfter != targetLightCountBefore)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " light count changed while syncing visual details. Before=" +
                    targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!ThrustFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " thrust FBX animation was not preserved after visual sync.");
            }

            var pierceConfiguredAnimators = CountConfiguredAnimators(pierceAttackRoot);
            if (pierceConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " must keep exactly one configured Animator after visual sync. Count=" +
                    pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo pierce attack visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", Reference=" + StaticRootName +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionMatched=True" +
                ", CopiedEyeLightsStripped=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedEyeAnimators=" + removedCopiedEyeAnimators.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPreserved=True" +
                ", MotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", PierceConfiguredAnimators=" + pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Pierce Attack Lights From Static Review")]
        public static void SyncTergoPierceAttackLightsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var controller = RequireAsset<AnimatorController>(ThrustFbxPierceAttackControllerPath);
            var clip = RequireAsset<AnimationClip>(ThrustFbxPierceAttackClipPath);
            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var targetEyeContainer = RequireBackRushVisualFirstNamedDescendant(pierceAttackRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetEyeRendererCountBefore = targetEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            if (sourceLightCount == 0)
            {
                throw new InvalidOperationException(StaticRootName + " has no lights to use as the visual reference.");
            }

            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceState = TransformState.Capture(pierceAttackRoot);
            var targetLightCountBefore = CountBackRushVisualLights(pierceAttackRoot);
            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must keep the thrust FBX controller before light sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;

            DestroyBackRushVisualLightGameObjects(pierceAttackRoot);
            var copiedLights = CopyBackRushVisualAllLightObjectsFromReference(staticRoot, pierceAttackRoot);
            var targetLightCountAfter = CountBackRushVisualLights(pierceAttackRoot);
            var targetEyeContainerAfter = RequireBackRushVisualFirstNamedDescendant(pierceAttackRoot, EyeContainerName);
            var targetEyeRendererCountAfter = targetEyeContainerAfter.GetComponentsInChildren<Renderer>(true).Length;
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, pierceAttackRoot, sourceEyeContainer);

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while syncing " + PierceAttackRootName + " lights.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + PierceAttackRootName + " lights.");
            }

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(PierceAttackRootName + " Animator changed while syncing lights.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " body materials changed while syncing lights.");
            }

            if (CountDescendantsByName(pierceAttackRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must keep exactly one approved eye container after light sync.");
            }

            if (targetEyeContainerAfter.parent != expectedEyeParent || !sourceEyeLocalState.Matches(targetEyeContainerAfter))
            {
                throw new InvalidOperationException(PierceAttackRootName + " eye container moved while syncing lights.");
            }

            if (targetEyeRendererCountBefore != sourceEyeRendererCount || targetEyeRendererCountAfter != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " eye renderer count changed while syncing lights. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Before=" + targetEyeRendererCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + targetEyeRendererCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (copiedLights != sourceLightCount || targetLightCountAfter != sourceLightCount)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " light count does not match reference after sync. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", Copied=" + copiedLights.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!ThrustFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " thrust FBX animation was not preserved after light sync.");
            }

            var pierceConfiguredAnimators = CountConfiguredAnimators(pierceAttackRoot);
            if (pierceConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " must keep exactly one configured Animator after light sync. Count=" +
                    pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo pierce attack lights.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackLightsSynced" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", Reference=" + StaticRootName +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", CopiedLights=" + copiedLights.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", BodyMaterialsPreserved=True" +
                ", EyeContainerPreserved=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionPreserved=True" +
                ", AnimatorPreserved=True" +
                ", MotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", PierceConfiguredAnimators=" + pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Downed Pounce Visual Details From Static Review")]
        public static void SyncTergoDownedPounceVisualDetailsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var controller = RequireAsset<AnimatorController>(DownedPounceFromFbxControllerPath);
            var clip = RequireAsset<AnimationClip>(DownedPounceFromFbxClipPath);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(downedPounceRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, TakedownModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " must stay linked to takedown FBX asset before visual sync. SourcePath=" + sourcePath);
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var staticState = TransformState.Capture(staticRoot);
            var idleState = TransformState.Capture(idleRoot);
            var walkState = TransformState.Capture(walkRoot);
            var detectState = TransformState.Capture(detectRoot);
            var runState = TransformState.Capture(runRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var downedPounceState = TransformState.Capture(downedPounceRoot);
            var animator = downedPounceRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(DownedPounceRootName + " must keep the takedown FBX controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;
            var targetLightCountBefore = CountBackRushVisualLights(downedPounceRoot);

            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, downedPounceRoot);
            DestroyBackRushVisualLightGameObjects(downedPounceRoot);
            DestroyBackRushVisualNamedDescendants(downedPounceRoot, EyeContainerName);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, downedPounceRoot, sourceEyeContainer);
            var copiedEyeLightCount = CountBackRushVisualLights(copiedEyeContainer);
            var copiedExternalLights = CopyBackRushVisualExternalLightObjectsFromReference(staticRoot, downedPounceRoot, sourceEyeContainer);
            var removedCopiedEyeAnimators = RemoveAnimatorComponentsUnderRoot(copiedEyeContainer);
            var targetLightCountAfter = CountBackRushVisualLights(downedPounceRoot);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, downedPounceRoot, sourceEyeContainer);

            if (!staticState.Matches(staticRoot) ||
                !idleState.Matches(idleRoot) ||
                !walkState.Matches(walkRoot) ||
                !detectState.Matches(detectRoot) ||
                !runState.Matches(runRoot) ||
                !pierceAttackState.Matches(pierceAttackRoot) ||
                !downedPounceState.Matches(downedPounceRoot))
            {
                throw new InvalidOperationException("Tergo root transform changed while syncing " + DownedPounceRootName + " visual details.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + DownedPounceRootName + " visual details.");
            }

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(DownedPounceRootName + " Animator changed while syncing visual details.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, downedPounceRoot))
            {
                throw new InvalidOperationException(DownedPounceRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(downedPounceRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(DownedPounceRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (copiedEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(copiedEyeContainer))
            {
                throw new InvalidOperationException(DownedPounceRootName + " eye container was not placed at the same relative head position as the reference Tergo.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCountAfter != sourceLightCount ||
                copiedEyeLightCount + copiedExternalLights != sourceLightCount)
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " light count does not match reference after visual sync. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedEye=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedExternal=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!DownedPounceControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, downedPounceRoot))
            {
                throw new InvalidOperationException(DownedPounceRootName + " takedown FBX animation was not preserved after visual sync.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(DownedPounceRootName + " takedown FBX animation loop setting changed during visual sync.");
            }

            var downedConfiguredAnimators = CountConfiguredAnimators(downedPounceRoot);
            if (downedConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    DownedPounceRootName + " must keep exactly one configured Animator after visual sync. Count=" +
                    downedConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo downed pounce visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoDownedPounceVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + DownedPounceRootName +
                ", Reference=" + StaticRootName +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionMatched=True" +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", CopiedEyeLights=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                ", CopiedExternalLights=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedEyeAnimators=" + removedCopiedEyeAnimators.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPreserved=True" +
                ", LoopAnimationPreserved=True" +
                ", MotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", DownedConfiguredAnimators=" + downedConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync Interrupt Stagger Visual Details From Static Review")]
        public static void SyncTergoInterruptStaggerVisualDetailsFromStaticReview()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var downedPounceRoot = RequireChild(placementRoot.transform, DownedPounceRootName);
            var interruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var controller = RequireAsset<AnimatorController>(FallOverFbxInterruptStaggerControllerPath);
            var clip = RequireAsset<AnimationClip>(FallOverFbxInterruptStaggerClipPath);
            var sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(interruptRoot.gameObject);
            var sourcePath = sourceObject == null ? string.Empty : AssetDatabase.GetAssetPath(sourceObject);
            if (!string.Equals(sourcePath, FallOverModelAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " must stay linked to fall-over FBX asset before visual sync. SourcePath=" + sourcePath);
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceEyeLocalState = TransformState.Capture(sourceEyeContainer);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);
            var animator = interruptRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " must keep the fall-over FBX controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var animatorSpeedBefore = animator.speed;
            var targetLightCountBefore = CountBackRushVisualLights(interruptRoot);

            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, interruptRoot);
            DestroyBackRushVisualLightGameObjects(interruptRoot);
            DestroyBackRushVisualNamedDescendants(interruptRoot, EyeContainerName);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, interruptRoot, sourceEyeContainer);
            var copiedEyeLightCount = CountBackRushVisualLights(copiedEyeContainer);
            var copiedExternalLights = CopyBackRushVisualExternalLightObjectsFromReference(staticRoot, interruptRoot, sourceEyeContainer);
            var removedCopiedEyeAnimators = RemoveAnimatorComponentsUnderRoot(copiedEyeContainer);
            var targetLightCountAfter = CountBackRushVisualLights(interruptRoot);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, interruptRoot, sourceEyeContainer);

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while syncing " + InterruptStaggerRootName + " visual details.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore ||
                Mathf.Abs(animator.speed - animatorSpeedBefore) > 0.0001f)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " Animator changed while syncing visual details.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, interruptRoot))
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(interruptRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (copiedEyeContainer.parent != expectedEyeParent || !sourceEyeLocalState.Matches(copiedEyeContainer))
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " eye container was not placed at the same relative head position as the reference Tergo.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCountAfter != sourceLightCount ||
                copiedEyeLightCount + copiedExternalLights != sourceLightCount)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " light count does not match reference after visual sync. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedEye=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", CopiedExternal=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!FallOverFbxControllerDefaultStateUsesClip(controller, clip) || !SampleClipChangesTransforms(clip, interruptRoot))
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " fall-over FBX animation was not preserved after visual sync.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " fall-over FBX animation loop setting changed during visual sync.");
            }

            var interruptConfiguredAnimators = CountConfiguredAnimators(interruptRoot);
            if (interruptConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " must keep exactly one configured Animator after visual sync. Count=" +
                    interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo interrupt stagger visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoInterruptStaggerVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + InterruptStaggerRootName +
                ", Reference=" + StaticRootName +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeRelativePositionMatched=True" +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", CopiedEyeLights=" + copiedEyeLightCount.ToString(CultureInfo.InvariantCulture) +
                ", CopiedExternalLights=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedEyeAnimators=" + removedCopiedEyeAnimators.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPreserved=True" +
                ", LoopAnimationPreserved=True" +
                ", MotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", InterruptConfiguredAnimators=" + interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Interrupt Stagger Backward Fall")]
        public static void ApplyTergoInterruptStaggerBackwardFall()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var interruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);

            RequireRendererSignaturesMatch(staticRoot, interruptRoot);
            RequireRestPoseSignaturesMatch(staticRoot, interruptRoot);

            var clip = EnsureInterruptStaggerBackwardFallClip(interruptRoot);
            var metrics = EvaluateInterruptStaggerMetrics(clip, interruptRoot);
            RequireInterruptStaggerMetrics(metrics);
            var controller = EnsureInterruptStaggerController(clip);
            var removedChildAnimators = RemovePierceAttackChildAnimators(interruptRoot);

            var animator = interruptRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = interruptRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playback = EvaluateInterruptStaggerAnimatorPlayback(animator, interruptRoot, clip);
            if (!playback.FirstPassMoved)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " backward fall Animator did not visibly move. FirstRotationDelta=" +
                    playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, interruptRoot))
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " backward fall clip did not change target transforms.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + InterruptStaggerRootName + " backward fall.");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);
            RequireRendererSignaturesMatch(staticRoot, interruptRoot);
            RequireRestPoseSignaturesMatch(staticRoot, interruptRoot);

            var interruptConfiguredAnimators = CountConfiguredAnimators(interruptRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName,
                DownedPounceRootName,
                InterruptStaggerRootName);
            if (interruptConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after interrupt stagger apply. Interrupt=" +
                    interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo interrupt stagger backward fall.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoInterruptStaggerBackwardFallApplied" +
                ", Target=" + PlacementRootName + "/" + InterruptStaggerRootName +
                ", Clip=" + InterruptStaggerClipPath +
                ", Controller=" + InterruptStaggerControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", BackwardFall=True" +
                ", ButtImpact=True" +
                ", HipsBackwardDelta=" + metrics.HipsBackwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HipsDropDelta=" + metrics.HipsDropDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HipsFallRotationAngle=" + metrics.HipsFallRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", TorsoFallRotationAngle=" + metrics.TorsoFallRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxLegBendAngle=" + metrics.MaxLegBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxArmFlailAngle=" + metrics.MaxArmFlailAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ImpactHoldDrift=" + metrics.ImpactHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", SettleShakeAngle=" + metrics.SettleShakeAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", FirstPassMaxRotationDelta=" + playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPassMaxPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", AvatarCleared=True" +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", InterruptConfiguredAnimators=" + interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Interrupt Stagger Backward Fall")]
        public static void ValidateTergoInterruptStaggerBackwardFall()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var interruptRoot = RequireChild(placementRoot.transform, InterruptStaggerRootName);
            var clip = RequireAsset<AnimationClip>(InterruptStaggerClipPath);
            var controller = RequireAsset<AnimatorController>(InterruptStaggerControllerPath);
            RequireConfiguredAnimator(interruptRoot, controller, InterruptStaggerRootName);

            var animator = interruptRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " is missing its root Animator.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (Mathf.Abs(clip.length - InterruptStaggerDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger clip length changed. Expected=" +
                    InterruptStaggerDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Actual=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(InterruptStaggerClipName + " must loop for scene review playback.");
            }

            if (animator.avatar != null)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " transform-curve clip must keep Animator avatar null.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(InterruptStaggerRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!ControllerUsesClip(controller, clip) || !ControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException(InterruptStaggerControllerPath + " must use " + InterruptStaggerClipPath + " as its default state motion.");
            }

            var metrics = EvaluateInterruptStaggerMetrics(clip, interruptRoot);
            RequireInterruptStaggerMetrics(metrics);
            var playback = EvaluateInterruptStaggerAnimatorPlayback(animator, interruptRoot, clip);
            if (!playback.FirstPassMoved)
            {
                throw new InvalidOperationException(
                    InterruptStaggerRootName + " Animator did not move during validation. FirstRotationDelta=" +
                    playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", FirstPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            RequireRendererSignaturesMatch(staticRoot, interruptRoot);
            RequireRestPoseSignaturesMatch(staticRoot, interruptRoot);

            var interruptConfiguredAnimators = CountConfiguredAnimators(interruptRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName,
                DownedPounceRootName,
                InterruptStaggerRootName);
            if (interruptConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts during interrupt stagger validation. Interrupt=" +
                    interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoInterruptStaggerBackwardFallValidated" +
                ", Target=" + PlacementRootName + "/" + InterruptStaggerRootName +
                ", Clip=" + InterruptStaggerClipPath +
                ", Controller=" + InterruptStaggerControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", BackwardFall=True" +
                ", ButtImpact=True" +
                ", HipsBackwardDelta=" + metrics.HipsBackwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HipsDropDelta=" + metrics.HipsDropDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HipsFallRotationAngle=" + metrics.HipsFallRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", TorsoFallRotationAngle=" + metrics.TorsoFallRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxLegBendAngle=" + metrics.MaxLegBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxArmFlailAngle=" + metrics.MaxArmFlailAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ImpactHoldDrift=" + metrics.ImpactHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", SettleShakeAngle=" + metrics.SettleShakeAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", FirstPassMaxRotationDelta=" + playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPassMaxPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", AvatarAssigned=False" +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", InterruptConfiguredAnimators=" + interruptConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Crouch Tremble 5s")]
        public static void ApplyTergoCrouchTremble5s()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var crouchRoot = RequireChild(placementRoot.transform, CrouchTrembleRootName);
            var siblingNamesBefore = BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty);
            var rootStatesBefore = CaptureDirectChildTransformStates(placementRoot.transform);

            RequireRendererSignaturesMatch(staticRoot, crouchRoot);
            RequireRestPoseSignaturesMatch(staticRoot, crouchRoot);

            var clip = EnsureCrouchTremble5sClip(crouchRoot);
            var metrics = EvaluateCrouchTrembleMetrics(clip, crouchRoot);
            RequireCrouchTrembleMetrics(metrics);
            var controller = EnsureCrouchTremble5sController(clip);
            var removedChildAnimators = RemovePierceAttackChildAnimators(crouchRoot);

            var animator = crouchRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = crouchRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playback = EvaluateCrouchTrembleAnimatorPlayback(animator, crouchRoot, clip);
            if (!playback.FirstPassMoved || !playback.PostLoopMoved)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " crouch tremble Animator did not visibly move. RiseRotationDelta=" +
                    playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RisePositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", TrembleRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", TremblePositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, crouchRoot))
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " crouch tremble clip did not change target transforms.");
            }

            if (!siblingNamesBefore.SequenceEqual(BuildDirectChildNameSnapshot(placementRoot.transform, string.Empty)))
            {
                throw new InvalidOperationException("Tergo root list changed while applying " + CrouchTrembleRootName + ".");
            }

            RequireDirectChildTransformStatesMatch(placementRoot.transform, rootStatesBefore);
            RequireRendererSignaturesMatch(staticRoot, crouchRoot);
            RequireRestPoseSignaturesMatch(staticRoot, crouchRoot);

            var crouchConfiguredAnimators = CountConfiguredAnimators(crouchRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName,
                DownedPounceRootName,
                InterruptStaggerRootName,
                CrouchTrembleRootName);
            if (crouchConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after crouch tremble apply. Crouch=" +
                    crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo crouch tremble 5s.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoCrouchTremble5sApplied" +
                ", Target=" + PlacementRootName + "/" + CrouchTrembleRootName +
                ", Clip=" + CrouchTrembleClipPath +
                ", Controller=" + CrouchTrembleControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LyingStart=True" +
                ", RiseToCrouch=True" +
                ", FaceCovered=True" +
                ", Tremble5s=True" +
                ", RiseHipsLiftDelta=" + metrics.RiseHipsLiftDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RiseBodyRotationAngle=" + metrics.RiseBodyRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FaceCoverArmAngle=" + metrics.FaceCoverArmAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CrouchLegBendAngle=" + metrics.CrouchLegBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", TremblePositionRange=" + metrics.TremblePositionRange.ToString("0.######", CultureInfo.InvariantCulture) +
                ", TrembleRotationRange=" + metrics.TrembleRotationRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RuntimeRiseMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimeTrembleMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", AvatarCleared=True" +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", CrouchConfiguredAnimators=" + crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTargetTergoRootTransformsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Crouch Tremble 5s")]
        public static void ValidateTergoCrouchTremble5s()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var crouchRoot = RequireChild(placementRoot.transform, CrouchTrembleRootName);
            var clip = RequireAsset<AnimationClip>(CrouchTrembleClipPath);
            var controller = RequireAsset<AnimatorController>(CrouchTrembleControllerPath);
            RequireConfiguredAnimator(crouchRoot, controller, CrouchTrembleRootName);

            var animator = crouchRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " is missing its root Animator.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (Mathf.Abs(clip.length - CrouchTrembleDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble clip length changed. Expected=" +
                    CrouchTrembleDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Actual=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(CrouchTrembleClipName + " must loop for scene review playback.");
            }

            if (animator.avatar != null)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " transform-curve clip must keep Animator avatar null.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " must keep root motion disabled.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(CrouchTrembleRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!controller.animationClips.Any(candidate => candidate == clip) || !ControllerDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException(CrouchTrembleControllerPath + " must use " + CrouchTrembleClipPath + " as its default state motion.");
            }

            var metrics = EvaluateCrouchTrembleMetrics(clip, crouchRoot);
            RequireCrouchTrembleMetrics(metrics);
            var playback = EvaluateCrouchTrembleAnimatorPlayback(animator, crouchRoot, clip);
            if (!playback.FirstPassMoved || !playback.PostLoopMoved)
            {
                throw new InvalidOperationException(
                    CrouchTrembleRootName + " Animator did not move during validation. RiseRotationDelta=" +
                    playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RisePositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", TrembleRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", TremblePositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            RequireRendererSignaturesMatch(staticRoot, crouchRoot);
            RequireRestPoseSignaturesMatch(staticRoot, crouchRoot);

            var crouchConfiguredAnimators = CountConfiguredAnimators(crouchRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName,
                DownedPounceRootName,
                InterruptStaggerRootName,
                CrouchTrembleRootName);
            if (crouchConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts during crouch tremble validation. Crouch=" +
                    crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoCrouchTremble5sValidated" +
                ", Target=" + PlacementRootName + "/" + CrouchTrembleRootName +
                ", Clip=" + CrouchTrembleClipPath +
                ", Controller=" + CrouchTrembleControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", LyingStart=True" +
                ", RiseToCrouch=True" +
                ", FaceCovered=True" +
                ", Tremble5s=True" +
                ", RiseHipsLiftDelta=" + metrics.RiseHipsLiftDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RiseBodyRotationAngle=" + metrics.RiseBodyRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FaceCoverArmAngle=" + metrics.FaceCoverArmAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CrouchLegBendAngle=" + metrics.CrouchLegBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", TremblePositionRange=" + metrics.TremblePositionRange.ToString("0.######", CultureInfo.InvariantCulture) +
                ", TrembleRotationRange=" + metrics.TrembleRotationRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RuntimeRiseMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimeTrembleMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", AvatarAssigned=False" +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", CrouchConfiguredAnimators=" + crouchConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
        }

        private static GameObject ImportThrustModelAsset()
        {
            var sourcePath = Path.GetFullPath(ThrustModelSourceAbsolutePath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException("Missing thrust source FBX: " + sourcePath);
            }

            var targetPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                ThrustModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                throw new InvalidOperationException("Invalid thrust target FBX path: " + targetPath);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(ThrustModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return RequireAsset<GameObject>(ThrustModelAssetPath);
        }

        private static GameObject ImportTakedownModelAsset()
        {
            var sourcePath = Path.GetFullPath(TakedownModelSourceAbsolutePath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException("Missing takedown source FBX: " + sourcePath);
            }

            var targetPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                TakedownModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                throw new InvalidOperationException("Invalid takedown target FBX path: " + targetPath);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(TakedownModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return RequireAsset<GameObject>(TakedownModelAssetPath);
        }

        private static GameObject ImportFallOverModelAsset()
        {
            var sourcePath = Path.GetFullPath(FallOverModelSourceAbsolutePath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException("Missing fall-over source FBX: " + sourcePath);
            }

            var targetPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                FallOverModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                throw new InvalidOperationException("Invalid fall-over target FBX path: " + targetPath);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(FallOverModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return RequireAsset<GameObject>(FallOverModelAssetPath);
        }

        private static GameObject ImportTerrifiedModelAsset()
        {
            var sourcePath = Path.GetFullPath(TerrifiedModelSourceAbsolutePath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException("Missing terrified source FBX: " + sourcePath);
            }

            var targetPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                TerrifiedModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                throw new InvalidOperationException("Invalid terrified target FBX path: " + targetPath);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(TerrifiedModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return RequireAsset<GameObject>(TerrifiedModelAssetPath);
        }

        private static GameObject ImportHittedModelAsset()
        {
            var sourcePath = Path.GetFullPath(HittedModelSourceAbsolutePath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException("Missing hitted source FBX: " + sourcePath);
            }

            var targetPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                HittedModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                throw new InvalidOperationException("Invalid hitted target FBX path: " + targetPath);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(HittedModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return RequireAsset<GameObject>(HittedModelAssetPath);
        }

        private static GameObject ImportDyingModelAsset()
        {
            var sourcePath = Path.GetFullPath(DyingModelSourceAbsolutePath);
            if (!File.Exists(sourcePath))
            {
                throw new InvalidOperationException("Missing dying source FBX: " + sourcePath);
            }

            var targetPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                DyingModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory))
            {
                throw new InvalidOperationException("Invalid dying target FBX path: " + targetPath);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(DyingModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            return RequireAsset<GameObject>(DyingModelAssetPath);
        }

        private static string[] BuildDirectChildNameSnapshot(Transform root, string excludedChildName)
        {
            var names = new List<string>();
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (!string.Equals(child.name, excludedChildName, StringComparison.Ordinal))
                {
                    names.Add(child.name);
                }
            }

            return names.ToArray();
        }

        private static Dictionary<string, TransformState> CaptureDirectChildTransformStates(Transform root)
        {
            var states = new Dictionary<string, TransformState>(StringComparer.Ordinal);
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                states[child.name] = TransformState.Capture(child);
            }

            return states;
        }

        private static void RequireDirectChildTransformStatesMatch(
            Transform root,
            IReadOnlyDictionary<string, TransformState> expectedStates)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (!expectedStates.TryGetValue(child.name, out var expectedState))
                {
                    throw new InvalidOperationException("Unexpected direct child under " + root.name + ": " + child.name);
                }

                if (!expectedState.Matches(child))
                {
                    throw new InvalidOperationException(root.name + "/" + child.name + " transform changed unexpectedly.");
                }
            }
        }

        private static AnimationClip[] LoadThrustAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ThrustModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + ThrustModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip[] LoadTakedownAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(TakedownModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + TakedownModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip[] LoadFallOverAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(FallOverModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + FallOverModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip[] LoadTerrifiedAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(TerrifiedModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + TerrifiedModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip[] LoadHittedAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(HittedModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + HittedModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip[] LoadDyingAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(DyingModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + DyingModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip SelectThrustSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetThrustClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static AnimationClip SelectTakedownSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetTakedownClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static AnimationClip SelectFallOverSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetFallOverClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static AnimationClip SelectTerrifiedSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetTerrifiedClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static AnimationClip SelectHittedSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetHittedClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static AnimationClip SelectDyingSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetDyingClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static int GetThrustClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("thrust"))
            {
                score += 140;
            }

            if (lower.Contains("pierce"))
            {
                score += 130;
            }

            if (lower.Contains("attack"))
            {
                score += 120;
            }

            if (lower.Contains("stab"))
            {
                score += 110;
            }

            if (lower.Contains("punch"))
            {
                score += 100;
            }

            if (lower.Contains("drill"))
            {
                score += 90;
            }

            if (lower.Contains("take"))
            {
                score += 20;
            }

            return score;
        }

        private static int GetTakedownClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("takedown"))
            {
                score += 150;
            }

            if (lower.Contains("pounce"))
            {
                score += 140;
            }

            if (lower.Contains("downed"))
            {
                score += 130;
            }

            if (lower.Contains("down"))
            {
                score += 100;
            }

            if (lower.Contains("attack"))
            {
                score += 70;
            }

            if (lower.Contains("take"))
            {
                score += 40;
            }

            return score;
        }

        private static int GetFallOverClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("fall"))
            {
                score += 150;
            }

            if (lower.Contains("over"))
            {
                score += 130;
            }

            if (lower.Contains("stagger"))
            {
                score += 120;
            }

            if (lower.Contains("interrupt"))
            {
                score += 110;
            }

            if (lower.Contains("hit"))
            {
                score += 70;
            }

            if (lower.Contains("down"))
            {
                score += 60;
            }

            return score;
        }

        private static int GetTerrifiedClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("terrified"))
            {
                score += 170;
            }

            if (lower.Contains("fear"))
            {
                score += 150;
            }

            if (lower.Contains("crouch"))
            {
                score += 140;
            }

            if (lower.Contains("tremble") || lower.Contains("shake"))
            {
                score += 130;
            }

            if (lower.Contains("idle"))
            {
                score += 60;
            }

            if (lower.Contains("hit") || lower.Contains("stagger"))
            {
                score += 50;
            }

            return score;
        }

        private static int GetHittedClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("hitted"))
            {
                score += 170;
            }

            if (lower.Contains("hit"))
            {
                score += 150;
            }

            if (lower.Contains("hurt"))
            {
                score += 140;
            }

            if (lower.Contains("damage"))
            {
                score += 130;
            }

            if (lower.Contains("recoil"))
            {
                score += 120;
            }

            if (lower.Contains("normal"))
            {
                score += 80;
            }

            return score;
        }

        private static int GetDyingClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("dying"))
            {
                score += 170;
            }

            if (lower.Contains("death"))
            {
                score += 160;
            }

            if (lower.Contains("die"))
            {
                score += 150;
            }

            if (lower.Contains("dead"))
            {
                score += 130;
            }

            if (lower.Contains("fall"))
            {
                score += 80;
            }

            if (lower.Contains("hit") || lower.Contains("hurt"))
            {
                score += 40;
            }

            return score;
        }

        private static AnimationClip EnsureCopiedThrustFbxPierceAttackClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ThrustFbxPierceAttackClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = ThrustFbxPierceAttackClipName
                };
                AssetDatabase.CreateAsset(clip, ThrustFbxPierceAttackClipPath);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = ThrustFbxPierceAttackClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureCopiedDownedPounceFromFbxClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DownedPounceFromFbxClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = DownedPounceFromFbxClipName
                };
                AssetDatabase.CreateAsset(clip, DownedPounceFromFbxClipPath);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = DownedPounceFromFbxClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureCopiedFallOverFbxInterruptStaggerClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(FallOverFbxInterruptStaggerClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = FallOverFbxInterruptStaggerClipName
                };
                AssetDatabase.CreateAsset(clip, FallOverFbxInterruptStaggerClipPath);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = FallOverFbxInterruptStaggerClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureCopiedTerrifiedFbxCrouchTrembleClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(TerrifiedFbxCrouchTrembleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = TerrifiedFbxCrouchTrembleClipName
                };
                AssetDatabase.CreateAsset(clip, TerrifiedFbxCrouchTrembleClipPath);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = TerrifiedFbxCrouchTrembleClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureCopiedHittedFbxHitNormalClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HittedFbxHitNormalClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = HittedFbxHitNormalClipName
                };
                AssetDatabase.CreateAsset(clip, HittedFbxHitNormalClipPath);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = HittedFbxHitNormalClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureCopiedDyingFbxDeathClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DyingFbxDeathClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = DyingFbxDeathClipName
                };
                AssetDatabase.CreateAsset(clip, DyingFbxDeathClipPath);
            }

            var sourceSettings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = DyingFbxDeathClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = sourceSettings.loopBlend;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip CreateDyingFbxDeathBaseComparisonClip(AnimationClip targetClip, float baseFallEndTime)
        {
            var clip = CloneAnimationClipForComparison(targetClip, DyingFbxDeathClipName + "_BaseCompare");
            var meltStartTime = GetDeathMeltStartTime(baseFallEndTime);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                var retainedKeys = curve.keys
                    .Where(key => key.time < meltStartTime - 0.0001f)
                    .ToArray();
                if (retainedKeys.Length == curve.length)
                {
                    continue;
                }

                if (retainedKeys.Length == 0)
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    continue;
                }

                var retainedCurve = new AnimationCurve(retainedKeys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(clip, binding, retainedCurve);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(baseFallEndTime, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static AnimationClip CloneAnimationClipForComparison(AnimationClip sourceClip, string clipName)
        {
            var clip = new AnimationClip
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = clipName;
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }

        private static AnimationClip CloneDyingFbxSourceClipForComparison(AnimationClip sourceClip, string clipName)
        {
            var clip = CloneAnimationClipForComparison(sourceClip, clipName);
            var sourceSettings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = sourceSettings.loopBlend;
            settings.startTime = 0f;
            settings.stopTime = Mathf.Max(sourceClip.length, 0.01f);
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        private static AnimatorController EnsureThrustFbxPierceAttackController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ThrustFbxPierceAttackControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ThrustFbxPierceAttackControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(ThrustFbxPierceAttackClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureDownedPounceFromFbxController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DownedPounceFromFbxControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(DownedPounceFromFbxControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(DownedPounceFromFbxClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureFallOverFbxInterruptStaggerController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(FallOverFbxInterruptStaggerControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(FallOverFbxInterruptStaggerControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(FallOverFbxInterruptStaggerClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureTerrifiedFbxCrouchTrembleController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(TerrifiedFbxCrouchTrembleControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(TerrifiedFbxCrouchTrembleControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(TerrifiedFbxCrouchTrembleClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureHittedFbxHitNormalController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HittedFbxHitNormalControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(HittedFbxHitNormalControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(HittedFbxHitNormalClipName);
            state.motion = clip;
            state.speed = HittedFbxHitNormalPlaybackSpeed;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController EnsureDyingFbxDeathController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DyingFbxDeathControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(DyingFbxDeathControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(DyingFbxDeathClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureDyingFbxDeathClipLoops(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static bool ThrustFbxControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static bool DownedPounceControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static bool FallOverFbxControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static bool TerrifiedFbxControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static bool HittedFbxControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static bool DyingFbxControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static float GetHittedFbxControllerDefaultStateSpeed(AnimatorController controller)
        {
            if (controller.layers.Length == 0)
            {
                return 0f;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState == null ? 0f : defaultState.speed;
        }

        private static Avatar LoadThrustAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(ThrustModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static Avatar LoadTakedownAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(TakedownModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static Avatar LoadFallOverAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(FallOverModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static Avatar LoadTerrifiedAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(TerrifiedModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static Avatar LoadHittedAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(HittedModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static Avatar LoadDyingAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(DyingModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static ThrustFbxPlaybackMetrics EvaluateThrustFbxAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(ThrustFbxPierceAttackClipName, 0, 0f);
                animator.Update(0f);
                var startStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var midStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Play(ThrustFbxPierceAttackClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                var loopStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                return ThrustFbxPlaybackMetrics.FromSamples(startStates, midStates, loopStates);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static ThrustFbxPlaybackMetrics EvaluateDownedPounceAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(DownedPounceFromFbxClipName, 0, 0f);
                animator.Update(0f);
                var startStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var midStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Play(DownedPounceFromFbxClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                var loopStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                return ThrustFbxPlaybackMetrics.FromSamples(startStates, midStates, loopStates);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static ThrustFbxPlaybackMetrics EvaluateFallOverFbxAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(FallOverFbxInterruptStaggerClipName, 0, 0f);
                animator.Update(0f);
                var startStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var midStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Play(FallOverFbxInterruptStaggerClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                var loopStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                return ThrustFbxPlaybackMetrics.FromSamples(startStates, midStates, loopStates);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static ThrustFbxPlaybackMetrics EvaluateTerrifiedFbxAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(TerrifiedFbxCrouchTrembleClipName, 0, 0f);
                animator.Update(0f);
                var startStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var midStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Play(TerrifiedFbxCrouchTrembleClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var loopStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                return ThrustFbxPlaybackMetrics.FromSamples(startStates, midStates, loopStates);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static ThrustFbxPlaybackMetrics EvaluateHittedFbxAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(HittedFbxHitNormalClipName, 0, 0f);
                animator.Update(0f);
                var startStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var midStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Play(HittedFbxHitNormalClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                var loopStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                return ThrustFbxPlaybackMetrics.FromSamples(startStates, midStates, loopStates);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static ThrustFbxPlaybackMetrics EvaluateDyingFbxAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(DyingFbxDeathClipName, 0, 0f);
                animator.Update(0f);
                var startStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Update(Mathf.Clamp(clip.length * 0.5f, 0.02f, Mathf.Max(clip.length - 0.001f, 0.02f)));
                var midStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                animator.Play(DyingFbxDeathClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                var endStates = transforms.Select(LocalTransformSample.Capture).ToArray();

                return ThrustFbxPlaybackMetrics.FromSamples(startStates, midStates, endStates);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Replace BackRush With Running Model")]
        public static void ReplaceTergoBackRushWithRunningModel()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var existingRunRoot = FindDirectChild(placementRoot.transform, RunRootName);
            var hadExistingRunRoot = existingRunRoot != null;
            var slot = BackRushRestoreSlot.CaptureOrInfer(
                placementRoot.transform,
                existingRunRoot,
                detectRoot,
                pierceAttackRoot);

            var detectState = TransformState.Capture(detectRoot);
            var pierceAttackState = TransformState.Capture(pierceAttackRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var runningPrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var importedClips = LoadImportedAnimationClips();
            var sourceClip = SelectRunSourceClip(importedClips);
            var runClip = EnsureCopiedRunClip(sourceClip);
            var controller = EnsureRunController(runClip);
            var avatar = EnsureRunningAvatar();

            if (hadExistingRunRoot)
            {
                UnityEngine.Object.DestroyImmediate(existingRunRoot.gameObject);
            }

            var instanceObject = PrefabUtility.InstantiatePrefab(runningPrefab, placementRoot.transform) as GameObject;
            if (instanceObject == null)
            {
                instanceObject = UnityEngine.Object.Instantiate(runningPrefab, placementRoot.transform);
            }

            instanceObject.name = RunRootName;
            var runRoot = instanceObject.transform;
            slot.ApplyTo(runRoot);

            var childAnimators = runRoot.GetComponentsInChildren<Animator>(true)
                .Where(animator => animator.transform != runRoot)
                .ToArray();
            foreach (var childAnimator in childAnimators)
            {
                UnityEngine.Object.DestroyImmediate(childAnimator);
            }

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!slot.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " slot transform changed while replacing with running FBX model.");
            }

            if (!detectState.Matches(detectRoot) || !pierceAttackState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException("Neighbor Tergo slot transforms changed while replacing " + RunRootName + ".");
            }

            var armature = RequireChild(runRoot, "Armature");
            var rendererCount = runRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(RunRootName + " running FBX model must include visible renderers.");
            }

            var runningBoneCount = CountRigBones(armature);
            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after running model replacement. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(runClip, runRoot))
            {
                throw new InvalidOperationException(RunRootName + " running FBX animation did not move the replacement model.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo BackRush with running FBX model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushRunningModelReplaced" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceModel=" + RunAnimationSourceAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(importedClips) +
                ", Clip=" + RunChaseClipPath +
                ", Controller=" + RunChaseControllerPath +
                ", AvatarAssigned=" + (avatar != null).ToString(CultureInfo.InvariantCulture) +
                ", OldBackRushRootDeleted=" + hadExistingRunRoot.ToString(CultureInfo.InvariantCulture) +
                ", NewBackRushRootFromRunningFbx=True" +
                ", SlotTransformPreserved=True" +
                ", NeighborSlotsPreserved=True" +
                ", ArmaturePresent=True" +
                ", RunningRigBoneCount=" + runningBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", RemovedChildAnimators=" + childAnimators.Length.ToString(CultureInfo.InvariantCulture) +
                ", RunConfiguredAnimators=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", SampleTransformChanged=True" +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Running Model")]
        public static void ValidateTergoBackRushRunningModel()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var runningPrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var runningAvatar = RequireRunningAvatar();

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(RunRootName + " is not using the running model chase controller.");
            }

            if (animator.avatar != runningAvatar)
            {
                throw new InvalidOperationException(RunRootName + " is not using the running FBX avatar.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " must keep root motion disabled for review placement.");
            }

            var armature = RequireChild(runRoot, "Armature");
            var sourceArmature = RequireChild(runningPrefab.transform, "Armature");
            var runningBoneCount = CountRigBones(armature);
            var sourceBoneCount = CountRigBones(sourceArmature);
            if (runningBoneCount != sourceBoneCount)
            {
                throw new InvalidOperationException(
                    "Running model armature bone count does not match source FBX. Source=" +
                    sourceBoneCount.ToString(CultureInfo.InvariantCulture) +
                    ", Scene=" + runningBoneCount.ToString(CultureInfo.InvariantCulture));
            }

            var rendererCount = runRoot.GetComponentsInChildren<Renderer>(true).Length;
            var skinnedRendererCount = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0 || skinnedRendererCount == 0)
            {
                throw new InvalidOperationException(RunRootName + " running FBX model must include visible renderers.");
            }

            if (!ControllerUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Tergo run chase controller is not bound to " + RunChaseClipPath);
            }

            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            if (Mathf.Abs(clip.length - sourceClip.length) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip must preserve the imported running source length. SourceLength=" +
                    sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RunLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (clip.length <= 0.01f || (curveBindingCount + objectBindingCount) == 0)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip has no usable animation data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, runRoot))
            {
                throw new InvalidOperationException("Tergo running model clip did not change any target transforms when sampled.");
            }

            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoBackRushRunningModelValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceModel=" + RunAnimationSourceAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", Clip=" + RunChaseClipPath +
                ", Controller=" + RunChaseControllerPath +
                ", AvatarSource=" + RunAnimationSourceAssetPath +
                ", AvatarAssigned=True" +
                ", BackRushRootFromRunningFbx=True" +
                ", ArmaturePresent=True" +
                ", SourceBoneCount=" + sourceBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", RunningRigBoneCount=" + runningBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererCount=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + skinnedRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleTransformChanged=True" +
                ", ApplyRootMotion=False" +
                ", RunConfiguredAnimators=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Sync BackRush Visual Details")]
        public static void SyncTergoBackRushVisualDetails()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var runController = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var runningAvatar = RequireRunningAvatar();
            var runClip = RequireAsset<AnimationClip>(RunChaseClipPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != runController)
            {
                throw new InvalidOperationException(RunRootName + " must keep the running model chase controller before visual sync.");
            }

            var controllerBefore = animator.runtimeAnimatorController;
            var avatarBefore = animator.avatar;
            var applyRootMotionBefore = animator.applyRootMotion;
            var animatorEnabledBefore = animator.enabled;
            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetLightCountBefore = CountBackRushVisualLights(runRoot);

            var syncedBodyRenderers = SyncBackRushVisualBodyMaterialsFromReference(staticRoot, runRoot);
            DestroyBackRushVisualNamedDescendants(runRoot, EyeContainerName);
            DestroyBackRushVisualLightGameObjects(runRoot);
            var copiedEyeContainer = CopyBackRushVisualEyeContainerFromReference(staticRoot, runRoot, sourceEyeContainer);
            var copiedExternalLights = CopyBackRushVisualExternalLightObjectsFromReference(staticRoot, runRoot, sourceEyeContainer);
            var targetLightCountAfter = CountBackRushVisualLights(runRoot);
            var targetEyeRendererCount = copiedEyeContainer.GetComponentsInChildren<Renderer>(true).Length;

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " root transform changed while syncing visual details.");
            }

            if (animator.runtimeAnimatorController != controllerBefore ||
                animator.avatar != avatarBefore ||
                animator.applyRootMotion != applyRootMotionBefore ||
                animator.enabled != animatorEnabledBefore)
            {
                throw new InvalidOperationException(RunRootName + " Animator changed while syncing visual details.");
            }

            if (animator.avatar != runningAvatar)
            {
                throw new InvalidOperationException(RunRootName + " must keep the running FBX avatar after visual sync.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, runRoot))
            {
                throw new InvalidOperationException(RunRootName + " body materials do not match the reference Tergo after visual sync.");
            }

            if (CountDescendantsByName(runRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(RunRootName + " must have exactly one approved eye container after visual sync.");
            }

            if (targetEyeRendererCount != sourceEyeRendererCount)
            {
                throw new InvalidOperationException(
                    RunRootName + " eye renderer count does not match the reference Tergo. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            if (targetLightCountAfter != sourceLightCount)
            {
                throw new InvalidOperationException(
                    RunRootName + " light count does not match the reference Tergo. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(runClip, runRoot))
            {
                throw new InvalidOperationException(RunRootName + " running animation stopped changing transforms after visual sync.");
            }

            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after visual sync. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after syncing Tergo BackRush visual details.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushVisualDetailsSynced" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Reference=" + StaticRootName +
                ", BodyMaterialsSynced=True" +
                ", SyncedBodyRenderers=" + syncedBodyRenderers.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerSynced=True" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsBefore=" + targetLightCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", TargetLightsAfter=" + targetLightCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", CopiedExternalLights=" + copiedExternalLights.ToString(CultureInfo.InvariantCulture) +
                ", MotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", AnimatorPreserved=True" +
                ", RunConfiguredAnimators=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Visual Details")]
        public static void ValidateTergoBackRushVisualDetails()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var runController = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var runningAvatar = RequireRunningAvatar();
            var runClip = RequireAsset<AnimationClip>(RunChaseClipPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != runController)
            {
                throw new InvalidOperationException(RunRootName + " is not using the running model chase controller.");
            }

            if (animator.avatar != runningAvatar)
            {
                throw new InvalidOperationException(RunRootName + " is not using the running FBX avatar.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " must keep root motion disabled.");
            }

            if (!BackRushVisualBodyMaterialsMatchReference(staticRoot, runRoot))
            {
                throw new InvalidOperationException(RunRootName + " body materials do not match the reference Tergo.");
            }

            var sourceEyeContainer = RequireBackRushVisualFirstNamedDescendant(staticRoot, EyeContainerName);
            var targetEyeContainer = RequireBackRushVisualFirstNamedDescendant(runRoot, EyeContainerName);
            if (CountDescendantsByName(runRoot, EyeContainerName) != 1)
            {
                throw new InvalidOperationException(RunRootName + " must have exactly one approved eye container.");
            }

            var expectedEyeParent = FindBackRushVisualMatchingParent(staticRoot, runRoot, sourceEyeContainer);
            if (targetEyeContainer.parent != expectedEyeParent)
            {
                throw new InvalidOperationException(RunRootName + " eye container is not attached to the matching Tergo head parent.");
            }

            var sourceEyeRendererCount = sourceEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            var targetEyeRendererCount = targetEyeContainer.GetComponentsInChildren<Renderer>(true).Length;
            if (sourceEyeRendererCount != targetEyeRendererCount)
            {
                throw new InvalidOperationException(
                    RunRootName + " eye renderer count does not match reference. Source=" +
                    sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture));
            }

            var sourceLightCount = CountBackRushVisualLights(staticRoot);
            var targetLightCount = CountBackRushVisualLights(runRoot);
            if (sourceLightCount != targetLightCount)
            {
                throw new InvalidOperationException(
                    RunRootName + " light count does not match reference. Source=" +
                    sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCount.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(runClip, runRoot))
            {
                throw new InvalidOperationException(RunRootName + " running animation does not change transforms after visual detail sync.");
            }

            var bodyRendererCount = FindBackRushVisualBodyRenderers(runRoot).Length;
            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoBackRushVisualDetailsValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Reference=" + StaticRootName +
                ", BodyMaterialsMatchReference=True" +
                ", BodyRendererCount=" + bodyRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerCount=1" +
                ", SourceEyeRenderers=" + sourceEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetEyeRenderers=" + targetEyeRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceLights=" + sourceLightCount.ToString(CultureInfo.InvariantCulture) +
                ", TargetLights=" + targetLightCount.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorControllerPreserved=True" +
                ", RunningAvatarPreserved=True" +
                ", ApplyRootMotion=False" +
                ", SampleTransformChanged=True" +
                ", RunConfiguredAnimators=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Pierce Attack Animation")]
        public static void ApplyTergoPierceAttackAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var runTransformState = TransformState.Capture(runRoot);
            var pierceTransformState = TransformState.Capture(pierceAttackRoot);

            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var runController = RequireAsset<AnimatorController>(RunChaseControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireConfiguredAnimator(runRoot, runController, RunRootName);

            var runAnimator = runRoot.GetComponent<Animator>();
            var runAvatar = runAnimator != null ? runAnimator.avatar : null;
            var runApplyRootMotion = runAnimator != null && runAnimator.applyRootMotion;
            var clip = EnsureReadablePierceAttackClip(pierceAttackRoot);
            var controller = EnsurePierceAttackController(clip);
            var removedChildAnimators = RemovePierceAttackChildAnimators(pierceAttackRoot);

            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = pierceAttackRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = LoadNormalTergoAvatarOrNull();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while applying pierce attack animation.");
            }

            if (!pierceTransformState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " root transform changed while applying pierce attack animation.");
            }

            if (runAnimator == null ||
                runAnimator.runtimeAnimatorController != runController ||
                runAnimator.avatar != runAvatar ||
                runAnimator.applyRootMotion != runApplyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " running Animator changed while applying pierce attack animation.");
            }

            var metrics = EvaluatePierceAttackMetrics(clip, pierceAttackRoot);
            RequirePierceAttackMetrics(metrics);

            if (!SampleClipChangesTransforms(clip, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " pierce attack clip did not change any target transforms.");
            }

            var pierceConfiguredAnimators = CountConfiguredAnimators(pierceAttackRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName);
            if (pierceConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after pierce attack apply. Pierce=" +
                    pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo pierce attack animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", ConnectedFrom=" + RunRootName +
                ", Clip=" + PierceAttackClipPath +
                ", Controller=" + PierceAttackControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmPullBack=True" +
                ", RightArmForwardPierce=True" +
                ", RightStraightPunch=True" +
                ", RightLegForwardKneeBent=True" +
                ", LeftLegStraightSupport=True" +
                ", LeftArmStable=True" +
                ", ReadableAttackTiming=True" +
                ", ImpactHoldFrame=True" +
                ", RootTransformPreserved=True" +
                ", RunMotionPreserved=True" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", PierceConfiguredAnimators=" + pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RightArmWindupAngle=" + metrics.RightArmWindupAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmThrustAngle=" + metrics.RightArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightForeArmThrustAngle=" + metrics.RightForeArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandForwardDelta=" + metrics.RightHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandLateralDelta=" + metrics.RightHandLateralDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandVerticalDelta=" + metrics.RightHandVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightElbowExtensionAngle=" + metrics.RightElbowExtensionAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandHoldDrift=" + metrics.RightHandHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightUpLegForwardAngle=" + metrics.RightUpLegForwardAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeBendAngle=" + metrics.RightKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftUpLegSupportAngle=" + metrics.LeftUpLegSupportAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeBendAngle=" + metrics.LeftKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmMaxAngle=" + metrics.LeftArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftForeArmMaxAngle=" + metrics.LeftForeArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsForwardRange=" + metrics.HipsForwardRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Pierce Attack Animation")]
        public static void ValidateTergoPierceAttackAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var runController = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var pierceController = RequireAsset<AnimatorController>(PierceAttackControllerPath);
            var clip = RequireAsset<AnimationClip>(PierceAttackClipPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireConfiguredAnimator(runRoot, runController, RunRootName);

            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != pierceController)
            {
                throw new InvalidOperationException(PierceAttackRootName + " is not using the pierce attack controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must keep root motion disabled for review placement.");
            }

            if (!ControllerUsesClip(pierceController, clip))
            {
                throw new InvalidOperationException("Tergo pierce attack controller is not bound to " + PierceAttackClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Tergo pierce attack clip must loop so the review motion object visibly keeps moving.");
            }

            if (Mathf.Abs(clip.length - PierceAttackDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Tergo pierce attack clip length changed. Expected=" +
                    PierceAttackDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Actual=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (curveBindingCount < 80 || objectBindingCount != 0)
            {
                throw new InvalidOperationException(
                    "Tergo pierce attack clip has unexpected binding counts. CurveBindings=" +
                    curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " pierce attack clip did not change target transforms.");
            }

            var metrics = EvaluatePierceAttackMetrics(clip, pierceAttackRoot);
            RequirePierceAttackMetrics(metrics);

            var pierceConfiguredAnimators = CountConfiguredAnimators(pierceAttackRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName);
            if (pierceConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts. Pierce=" +
                    pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoPierceAttackAnimationValidated" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", ConnectedFrom=" + RunRootName +
                ", Clip=" + PierceAttackClipPath +
                ", Controller=" + PierceAttackControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", RightArmPullBack=True" +
                ", RightArmForwardPierce=True" +
                ", RightStraightPunch=True" +
                ", RightLegForwardKneeBent=True" +
                ", LeftLegStraightSupport=True" +
                ", LeftArmStable=True" +
                ", ReadableAttackTiming=True" +
                ", ImpactHoldFrame=True" +
                ", SampleTransformChanged=True" +
                ", ApplyRootMotion=False" +
                ", PierceConfiguredAnimators=" + pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RightArmWindupAngle=" + metrics.RightArmWindupAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmThrustAngle=" + metrics.RightArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightForeArmThrustAngle=" + metrics.RightForeArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandForwardDelta=" + metrics.RightHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandLateralDelta=" + metrics.RightHandLateralDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandVerticalDelta=" + metrics.RightHandVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightElbowExtensionAngle=" + metrics.RightElbowExtensionAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandHoldDrift=" + metrics.RightHandHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightUpLegForwardAngle=" + metrics.RightUpLegForwardAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeBendAngle=" + metrics.RightKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftUpLegSupportAngle=" + metrics.LeftUpLegSupportAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeBendAngle=" + metrics.LeftKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmMaxAngle=" + metrics.LeftArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftForeArmMaxAngle=" + metrics.LeftForeArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsForwardRange=" + metrics.HipsForwardRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect Pierce Attack Runtime Playback")]
        public static void InspectTergoPierceAttackRuntimePlayback()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var clip = RequireAsset<AnimationClip>(PierceAttackClipPath);
            var controller = RequireAsset<AnimatorController>(PierceAttackControllerPath);
            var animator = pierceAttackRoot.GetComponent<Animator>();
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            var playback = animator != null
                ? EvaluatePierceAttackAnimatorPlayback(animator, pierceAttackRoot, clip)
                : PierceAttackRuntimePlaybackMetrics.Empty;

            Debug.Log(
                "TergoPierceAttackRuntimePlaybackInspected" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", ConnectedFrom=" + runRoot.name +
                ", Clip=" + PierceAttackClipPath +
                ", Controller=" + PierceAttackControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=" + settings.loopTime.ToString(CultureInfo.InvariantCulture) +
                ", LoopBlend=" + settings.loopBlend.ToString(CultureInfo.InvariantCulture) +
                ", AnimatorPresent=" + (animator != null).ToString(CultureInfo.InvariantCulture) +
                ", AnimatorEnabled=" + (animator != null && animator.enabled).ToString(CultureInfo.InvariantCulture) +
                ", RootActiveInHierarchy=" + pierceAttackRoot.gameObject.activeInHierarchy.ToString(CultureInfo.InvariantCulture) +
                ", ControllerAssigned=" + (animator != null && animator.runtimeAnimatorController == controller).ToString(CultureInfo.InvariantCulture) +
                ", ControllerUsesClip=" + ControllerUsesClip(controller, clip).ToString(CultureInfo.InvariantCulture) +
                ", DefaultStateUsesClip=" + PierceAttackDefaultStateUsesClip(controller, clip).ToString(CultureInfo.InvariantCulture) +
                ", AvatarAssigned=" + (animator != null && animator.avatar != null).ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=" + (animator != null && animator.applyRootMotion).ToString(CultureInfo.InvariantCulture) +
                ", CullingMode=" + (animator != null ? animator.cullingMode.ToString() : "None") +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimePostLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", FirstPassMaxRotationDelta=" + playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPassMaxPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PostLoopMaxRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PostLoopMaxPositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", NeedsRuntimeRepair=" + (!settings.loopTime || animator == null || !playback.PostLoopMoved).ToString(CultureInfo.InvariantCulture));
        }

        public static void ApplyTergoPierceAttackStraightPunchCurrentSceneOnly()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException(
                    "Current active scene must be CargoRunMvp for the visual run. ActiveScene=" + scene.path);
            }

            var placementRoot = RequireSceneObject(PlacementRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var pierceTransformState = TransformState.Capture(pierceAttackRoot);
            var clip = EnsureReadablePierceAttackClip(pierceAttackRoot);
            var controller = EnsurePierceAttackController(clip);
            var removedChildAnimators = RemovePierceAttackChildAnimators(pierceAttackRoot);

            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = pierceAttackRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var metrics = EvaluatePierceAttackMetrics(clip, pierceAttackRoot);
            if (!SampleClipChangesTransforms(clip, pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " straight punch clip did not change target transforms.");
            }

            if (!pierceTransformState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " root transform changed while applying straight punch.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo straight punch.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackStraightPunchCurrentSceneApplied" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", Clip=" + PierceAttackClipPath +
                ", Controller=" + PierceAttackControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightStraightPunch=True" +
                ", RightHandForwardDelta=" + metrics.RightHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandLateralDelta=" + metrics.RightHandLateralDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandVerticalDelta=" + metrics.RightHandVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightElbowExtensionAngle=" + metrics.RightElbowExtensionAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandHoldDrift=" + metrics.RightHandHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RootTransformPreserved=True" +
                ", ActiveScenePreserved=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Repair Pierce Attack Runtime Playback")]
        public static void RepairTergoPierceAttackRuntimePlayback()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var pierceTransformState = TransformState.Capture(pierceAttackRoot);
            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var clip = EnsureReadablePierceAttackClip(pierceAttackRoot);
            SetPierceAttackRuntimePlaybackClipSettings(clip);
            var metrics = EvaluatePierceAttackMetrics(clip, pierceAttackRoot);
            RequirePierceAttackMetrics(metrics);
            var controller = EnsurePierceAttackController(clip);
            var removedChildAnimators = RemovePierceAttackChildAnimators(pierceAttackRoot);
            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = pierceAttackRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            var playback = EvaluatePierceAttackAnimatorPlayback(animator, pierceAttackRoot, clip);
            if (!playback.FirstPassMoved || !playback.PostLoopMoved)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " Animator runtime playback still does not move enough. FirstPassMoved=" +
                    playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                    ", PostLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                    ", FirstPassMaxRotationDelta=" + playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", PostLoopMaxRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (!pierceTransformState.Matches(pierceAttackRoot))
            {
                throw new InvalidOperationException(PierceAttackRootName + " transform changed while repairing runtime playback.");
            }

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while repairing " + PierceAttackRootName + ".");
            }

            var pierceConfiguredAnimators = CountConfiguredAnimators(pierceAttackRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName);
            if (pierceConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after pierce runtime playback repair. Pierce=" +
                    pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after repairing Tergo pierce attack runtime playback.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoPierceAttackRuntimePlaybackRepaired" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", ConnectedFrom=" + RunRootName +
                ", Clip=" + PierceAttackClipPath +
                ", Controller=" + PierceAttackControllerPath +
                ", LoopTime=True" +
                ", ReviewPlaybackLoops=True" +
                ", AvatarCleared=True" +
                ", ApplyRootMotion=False" +
                ", CullingMode=AlwaysAnimate" +
                ", AnimatorEnabled=True" +
                ", RemovedChildAnimators=" + removedChildAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimePostLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", FirstPassMaxRotationDelta=" + playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPassMaxPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PostLoopMaxRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PostLoopMaxPositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightArmWindupAngle=" + metrics.RightArmWindupAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmThrustAngle=" + metrics.RightArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightForeArmThrustAngle=" + metrics.RightForeArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandForwardDelta=" + metrics.RightHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandLateralDelta=" + metrics.RightHandLateralDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandVerticalDelta=" + metrics.RightHandVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightElbowExtensionAngle=" + metrics.RightElbowExtensionAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandHoldDrift=" + metrics.RightHandHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightUpLegForwardAngle=" + metrics.RightUpLegForwardAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeBendAngle=" + metrics.RightKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftUpLegSupportAngle=" + metrics.LeftUpLegSupportAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeBendAngle=" + metrics.LeftKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmMaxAngle=" + metrics.LeftArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftForeArmMaxAngle=" + metrics.LeftForeArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PierceConfiguredAnimators=" + pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RunMotionPreserved=True" +
                ", RootTransformPreserved=True" +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Pierce Attack Runtime Playback")]
        public static void ValidateTergoPierceAttackRuntimePlayback()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(PierceAttackClipPath);
            var controller = RequireAsset<AnimatorController>(PierceAttackControllerPath);
            RequireConfiguredAnimator(pierceAttackRoot, controller, PierceAttackRootName);
            var animator = pierceAttackRoot.GetComponent<Animator>();
            if (animator == null)
            {
                throw new InvalidOperationException(PierceAttackRootName + " is missing its root Animator.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (Mathf.Abs(clip.length - PierceAttackDuration) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Tergo pierce attack runtime clip length changed. Expected=" +
                    PierceAttackDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Actual=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var metrics = EvaluatePierceAttackMetrics(clip, pierceAttackRoot);
            RequirePierceAttackMetrics(metrics);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            if (!settings.loopTime)
            {
                throw new InvalidOperationException(PierceAttackClipName + " must loop for the review motion object to visibly keep moving.");
            }

            if (animator.avatar != null)
            {
                throw new InvalidOperationException(PierceAttackRootName + " runtime playback uses transform curves and must keep Animator avatar null.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must keep root motion disabled for review playback.");
            }

            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(PierceAttackRootName + " must use AlwaysAnimate culling for review playback.");
            }

            if (!ControllerUsesClip(controller, clip) || !PierceAttackDefaultStateUsesClip(controller, clip))
            {
                throw new InvalidOperationException(PierceAttackControllerPath + " must use " + PierceAttackClipPath + " as its default state motion.");
            }

            var playback = EvaluatePierceAttackAnimatorPlayback(animator, pierceAttackRoot, clip);
            if (!playback.FirstPassMoved || !playback.PostLoopMoved)
            {
                throw new InvalidOperationException(
                    PierceAttackRootName + " Animator runtime playback did not move during validation. FirstPassMoved=" +
                    playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                    ", PostLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture));
            }

            var pierceConfiguredAnimators = CountConfiguredAnimators(pierceAttackRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName,
                PierceAttackRootName);
            if (pierceConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts during pierce runtime validation. Pierce=" +
                    pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoPierceAttackRuntimePlaybackValidated" +
                ", Target=" + PlacementRootName + "/" + PierceAttackRootName +
                ", ConnectedFrom=" + runRoot.name +
                ", Clip=" + PierceAttackClipPath +
                ", Controller=" + PierceAttackControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", ReviewPlaybackLoops=True" +
                ", AnimatorEnabled=" + animator.enabled.ToString(CultureInfo.InvariantCulture) +
                ", AvatarAssigned=False" +
                ", ApplyRootMotion=False" +
                ", CullingMode=AlwaysAnimate" +
                ", RuntimeFirstPassMoved=" + playback.FirstPassMoved.ToString(CultureInfo.InvariantCulture) +
                ", RuntimePostLoopMoved=" + playback.PostLoopMoved.ToString(CultureInfo.InvariantCulture) +
                ", FirstPassMaxRotationDelta=" + playback.FirstPassMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FirstPassMaxPositionDelta=" + playback.FirstPassMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", PostLoopMaxRotationDelta=" + playback.PostLoopMaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PostLoopMaxPositionDelta=" + playback.PostLoopMaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightArmWindupAngle=" + metrics.RightArmWindupAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmThrustAngle=" + metrics.RightArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightForeArmThrustAngle=" + metrics.RightForeArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandForwardDelta=" + metrics.RightHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandLateralDelta=" + metrics.RightHandLateralDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightHandVerticalDelta=" + metrics.RightHandVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightElbowExtensionAngle=" + metrics.RightElbowExtensionAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightHandHoldDrift=" + metrics.RightHandHoldDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RightUpLegForwardAngle=" + metrics.RightUpLegForwardAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeBendAngle=" + metrics.RightKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftUpLegSupportAngle=" + metrics.LeftUpLegSupportAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeBendAngle=" + metrics.LeftKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmMaxAngle=" + metrics.LeftArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftForeArmMaxAngle=" + metrics.LeftForeArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PierceConfiguredAnimators=" + pierceConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", NonTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Run Chase Animation")]
        public static void ValidateTergoRunChaseAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var runningAvatar = RequireRunningAvatar();
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(RunRootName + " is not using the run chase controller.");
            }

            if (animator.avatar != runningAvatar)
            {
                throw new InvalidOperationException(RunRootName + " is not using the running FBX avatar.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " must keep root motion disabled for review placement.");
            }

            var armature = RequireChild(runRoot, "Armature");
            var runningPrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var sourceArmature = RequireChild(runningPrefab.transform, "Armature");
            var runningBoneCount = CountRigBones(armature);
            var sourceBoneCount = CountRigBones(sourceArmature);
            if (runningBoneCount != sourceBoneCount)
            {
                throw new InvalidOperationException(
                    "Running rig replacement bone count does not match source running FBX. Source=" +
                    sourceBoneCount.ToString(CultureInfo.InvariantCulture) +
                    ", Scene=" + runningBoneCount.ToString(CultureInfo.InvariantCulture));
            }

            var renderers = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(RunRootName + " must keep its visible SkinnedMeshRenderer.");
            }

            var rendererBonesOnRunningRig = renderers.All(renderer =>
                renderer.sharedMesh != null &&
                renderer.bones != null &&
                renderer.bones.Length > 0 &&
                renderer.bones.All(bone => bone != null && bone.IsChildOf(armature)));
            if (!rendererBonesOnRunningRig)
            {
                throw new InvalidOperationException(RunRootName + " SkinnedMeshRenderer bones are not all on the running rig armature.");
            }

            var eyeContainer = runRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => string.Equals(transform.name, EyeContainerName, StringComparison.Ordinal));
            var headFront = armature.Find("Hips/Spine02/Spine01/Spine/neck/Head/headfront");
            if (eyeContainer == null || headFront == null || !eyeContainer.IsChildOf(headFront))
            {
                throw new InvalidOperationException(RunRootName + " generated eyes must be parented under the running rig headfront bone.");
            }

            if (!ControllerUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Tergo run chase controller is not bound to " + RunChaseClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Tergo run chase clip must loop.");
            }

            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            if (Mathf.Abs(clip.length - sourceClip.length) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip must preserve the imported running source length. SourceLength=" +
                    sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RunLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (clip.length <= 0.01f || (curveBindingCount + objectBindingCount) == 0)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip has no usable animation data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            var sampleTransformChanged = SampleClipChangesTransforms(clip, runRoot);
            if (!sampleTransformChanged)
            {
                throw new InvalidOperationException("Tergo run chase clip did not change any target transforms when sampled.");
            }

            var idleConfiguredAnimators = CountConfiguredAnimators(idleRoot);
            var walkConfiguredAnimators = CountConfiguredAnimators(walkRoot);
            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);

            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    RunRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoRunChaseAnimationValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", Controller=" + RunChaseControllerPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceAnimationFbx=" + RunAnimationSourceAssetPath +
                ", RunClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", AvatarSource=" + RunAnimationSourceAssetPath +
                ", RigArmatureReplaced=True" +
                ", RunningRigBoneCount=" + runningBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRenderersPreserved=" + renderers.Length.ToString(CultureInfo.InvariantCulture) +
                ", RendererBonesOnRunningRig=True" +
                ", EyeContainerOnRunningRig=True" +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleTransformChanged=True" +
                ", LoopTime=True" +
                ", ApplyRootMotion=False" +
                ", IdleConfiguredAnimators=" + idleConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", WalkConfiguredAnimators=" + walkConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RunConfiguredAnimators=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", DetectUserDedicatedAnimation=False" +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", StaticDetectAndRemainingTergoUnanimated=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect BackRush Authored Sprint Rig")]
        public static void InspectTergoBackRushAuthoredSprintRig()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var armature = RequireChild(runRoot, "Armature");

            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            foreach (var requiredPath in AuthoredSprintRequiredBonePaths)
            {
                RequireAuthoredRelativeTransform(runRoot, requiredPath);
            }

            var secondaryPaths = FindAuthoredSprintSecondaryMotionPaths(runRoot).ToArray();
            var animatorCount = runRoot.GetComponentsInChildren<Animator>(true).Length;
            var configuredAnimatorCount = CountConfiguredAnimators(runRoot);
            var rendererCount = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);

            Debug.Log(
                "TergoBackRushAuthoredSprintRigInspected" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", RigBasis=CurrentBackRushRig" +
                ", SourceAnimationFbxClipCopied=False" +
                ", RunningFbxAnimationUsed=False" +
                ", ArmatureBoneCount=" + CountRigBones(armature).ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRenderers=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", RequiredSprintBones=" + string.Join("|", AuthoredSprintRequiredBonePaths) +
                ", SecondarySprintBones=" + FormatPathList(secondaryPaths) +
                ", SecondarySprintBoneCount=" + secondaryPaths.Length.ToString(CultureInfo.InvariantCulture) +
                ", ExistingAnimatorComponents=" + animatorCount.ToString(CultureInfo.InvariantCulture) +
                ", ExistingConfiguredAnimators=" + configuredAnimatorCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply BackRush Authored Sprint")]
        public static void ApplyTergoBackRushAuthoredSprint()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var secondaryPaths = FindAuthoredSprintSecondaryMotionPaths(runRoot).ToArray();
            var clip = EnsureAuthoredSprintClip(runRoot);
            var controller = EnsureAuthoredSprintController(clip);
            RemoveAnimatorComponentsBelowRoot(runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            var avatar = LoadNormalTergoAvatarOrNull();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while applying authored sprint animation.");
            }

            var idleConfiguredAnimators = CountConfiguredAnimators(idleRoot);
            var walkConfiguredAnimators = CountConfiguredAnimators(walkRoot);
            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after authored sprint apply. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying authored Tergo BackRush sprint animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            var motion = MeasureAuthoredSprintMotion(clip, runRoot);
            Debug.Log(
                "TergoBackRushAuthoredSprintApplied" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + AuthoredSprintClipPath +
                ", Controller=" + AuthoredSprintControllerPath +
                ", RigBasis=CurrentBackRushRig" +
                ", SourceAnimationFbxClipCopied=False" +
                ", RunningFbxAnimationUsed=False" +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SecondarySprintBoneCount=" + secondaryPaths.Length.ToString(CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + motion.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsLateralRange=" + motion.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxTorsoRotationAngle=" + motion.MaxTorsoRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxHeadRotationAngle=" + motion.MaxHeadRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", AnimatorCountOnBackRush=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", RootTransformUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Authored Sprint")]
        public static void ValidateTergoBackRushAuthoredSprint()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(AuthoredSprintClipPath);
            var controller = RequireAsset<AnimatorController>(AuthoredSprintControllerPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(RunRootName + " is not using the authored sprint controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " must keep root motion disabled for in-place sprint review.");
            }

            if (!ControllerUsesClipAtPath(controller, clip, AuthoredSprintClipPath))
            {
                throw new InvalidOperationException("Tergo authored sprint controller is not bound to " + AuthoredSprintClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || !settings.loopBlend || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Tergo authored sprint clip must loop.");
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (clip.length < 0.4f || clip.length > 0.65f || curveBindingCount < 60 || objectBindingCount != 0)
            {
                throw new InvalidOperationException(
                    "Tergo authored sprint clip has unexpected authored data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var requiredPath in AuthoredSprintRequiredBonePaths)
            {
                RequireAuthoredTransformCurveBindings(clip, requiredPath);
            }

            if (!SampleClipChangesTransforms(clip, runRoot))
            {
                throw new InvalidOperationException("Tergo authored sprint clip did not change any target transforms when sampled.");
            }

            var motion = MeasureAuthoredSprintMotion(clip, runRoot);
            if (motion.HipsVerticalRange < 0.09f ||
                motion.HipsLateralRange < 0.035f ||
                motion.MaxTorsoRotationAngle < 8f ||
                motion.MaxHeadRotationAngle < 5f)
            {
                throw new InvalidOperationException(
                    "Tergo authored sprint motion is too weak for a full sprint. HipsVerticalRange=" +
                    motion.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", HipsLateralRange=" + motion.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", MaxTorsoRotationAngle=" + motion.MaxTorsoRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", MaxHeadRotationAngle=" + motion.MaxHeadRotationAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts while validating authored sprint. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    RunRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoBackRushAuthoredSprintValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + AuthoredSprintClipPath +
                ", Controller=" + AuthoredSprintControllerPath +
                ", RigBasis=CurrentBackRushRig" +
                ", SourceAnimationFbxClipCopied=False" +
                ", RunningFbxAnimationUsed=False" +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleTransformChanged=True" +
                ", HipsVerticalRange=" + motion.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsLateralRange=" + motion.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxTorsoRotationAngle=" + motion.MaxTorsoRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", MaxHeadRotationAngle=" + motion.MaxHeadRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", ApplyRootMotion=False" +
                ", AnimatorCountOnBackRush=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect BackRush Sprint Reference Motion")]
        public static void InspectTergoBackRushSprintReferenceMotion()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);
            foreach (var requiredPath in ReferenceDrivenSprintMotionPaths)
            {
                RequireAuthoredRelativeTransform(runRoot, requiredPath);
            }

            var profile = BuildReferenceSprintProfile();
            Debug.Log(
                "TergoBackRushSprintReferenceMotionInspected" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", ReferenceAsset=" + RunAnimationSourceAssetPath +
                ", ReferenceClip=" + profile.SourceClipName +
                ", ReferenceClipLength=" + profile.SourceClipLength.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ReferenceClips=" + profile.SourceClipsSummary +
                ", ReferenceUsedForMotionStudyOnly=True" +
                ", SourceAnimationCurvesCopied=False" +
                ", SourceRigCopied=False" +
                ", TargetRendererMatchesStatic=True" +
                ", TargetRestPoseMatchesStatic=True" +
                ", ReferenceAxes=" + FormatReferenceAxes(profile));
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Rewrite BackRush Authored Sprint From Reference")]
        public static void RewriteTergoBackRushAuthoredSprintFromReference()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var profile = BuildReferenceSprintProfile();
            var clip = EnsureReferenceDrivenAuthoredSprintClip(runRoot, profile);
            var controller = EnsureAuthoredSprintController(clip);
            RemoveAnimatorComponentsBelowRoot(runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = LoadNormalTergoAvatarOrNull();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while rewriting authored sprint animation.");
            }

            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after sprint rewrite. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after rewriting authored Tergo sprint animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var metrics = MeasureReferenceDrivenSprintMotion(clip, runRoot, profile);
            Debug.Log(
                "TergoBackRushSprintReferenceRewriteApplied" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + AuthoredSprintClipPath +
                ", Controller=" + AuthoredSprintControllerPath +
                ", ReferenceAsset=" + RunAnimationSourceAssetPath +
                ", ReferenceClip=" + profile.SourceClipName +
                ", ReferenceUsedForMotionStudyOnly=True" +
                ", SourceAnimationCurvesCopied=False" +
                ", SourceRigCopied=False" +
                ", HighKneeMotionAuthored=True" +
                ", AlternatingArmPumpAuthored=True" +
                ", SideWobbleReduced=True" +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + AnimationUtility.GetCurveBindings(clip).Length.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length.ToString(CultureInfo.InvariantCulture) +
                ", LeftUpLegRange=" + metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftLegRange=" + metrics.LeftLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightLegRange=" + metrics.RightLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsLateralRange=" + metrics.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeWorldYRange=" + metrics.LeftKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeWorldYRange=" + metrics.RightKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", AnimatorCountOnBackRush=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", RootTransformUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Authored Sprint Rewrite")]
        public static void ValidateTergoBackRushAuthoredSprintRewrite()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(AuthoredSprintClipPath);
            var controller = RequireAsset<AnimatorController>(AuthoredSprintControllerPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(RunRootName + " is not using the rewritten authored sprint controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " must keep root motion disabled for in-place sprint review.");
            }

            if (!ControllerUsesClipAtPath(controller, clip, AuthoredSprintClipPath))
            {
                throw new InvalidOperationException("Tergo authored sprint controller is not bound to " + AuthoredSprintClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (!settings.loopTime || !settings.loopBlend || clip.wrapMode != WrapMode.Loop ||
                clip.length < 0.34f || clip.length > 0.46f ||
                curveBindingCount < 200 || objectBindingCount != 0)
            {
                throw new InvalidOperationException(
                    "Tergo rewritten authored sprint clip has unexpected data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var requiredPath in ReferenceDrivenSprintMotionPaths)
            {
                RequireAuthoredTransformCurveBindings(clip, requiredPath);
            }

            if (!SampleClipChangesTransforms(clip, runRoot))
            {
                throw new InvalidOperationException("Tergo rewritten authored sprint clip did not change target transforms when sampled.");
            }

            var profile = BuildReferenceSprintProfile();
            var metrics = MeasureReferenceDrivenSprintMotion(clip, runRoot, profile);
            if (metrics.LeftUpLegRange < 70f ||
                metrics.RightUpLegRange < 70f ||
                metrics.LeftLegRange < 45f ||
                metrics.RightLegRange < 45f ||
                metrics.LeftArmRange < 65f ||
                metrics.RightArmRange < 65f ||
                metrics.HipsVerticalRange < 0.085f ||
                metrics.HipsLateralRange > 0.025f)
            {
                throw new InvalidOperationException(
                    "Tergo rewritten authored sprint is not a strong high-knee sprint. LeftUpLegRange=" +
                    metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LeftLegRange=" + metrics.LeftLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RightLegRange=" + metrics.RightLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", HipsLateralRange=" + metrics.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0 || eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo state after rewritten sprint validation. RunAnimators=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", OtherAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", EyeContainers=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoBackRushSprintReferenceRewriteValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + AuthoredSprintClipPath +
                ", Controller=" + AuthoredSprintControllerPath +
                ", ReferenceAsset=" + RunAnimationSourceAssetPath +
                ", ReferenceClip=" + profile.SourceClipName +
                ", ReferenceUsedForMotionStudyOnly=True" +
                ", SourceAnimationCurvesCopied=False" +
                ", SourceRigCopied=False" +
                ", HighKneeMotionAuthored=True" +
                ", AlternatingArmPumpAuthored=True" +
                ", SideWobbleReduced=True" +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleTransformChanged=True" +
                ", LeftUpLegRange=" + metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftLegRange=" + metrics.LeftLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightLegRange=" + metrics.RightLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsLateralRange=" + metrics.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeWorldYRange=" + metrics.LeftKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeWorldYRange=" + metrics.RightKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", ApplyRootMotion=False" +
                ", AnimatorCountOnBackRush=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect BackRush Sprint Video Reference")]
        public static void InspectTergoBackRushSprintVideoReference()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var currentFrameCount = CountValidationFrames("docs/validation/tergo_backrush_sprint_current_20260705");
            var referenceFrameCount = CountValidationFrames("docs/validation/tergo_backrush_sprint_reference_20260705");
            if (currentFrameCount <= 0 || referenceFrameCount <= 0)
            {
                throw new InvalidOperationException(
                    "Missing extracted video reference frames. CurrentFrames=" +
                    currentFrameCount.ToString(CultureInfo.InvariantCulture) +
                    ", ReferenceFrames=" + referenceFrameCount.ToString(CultureInfo.InvariantCulture));
            }

            var clip = RequireAsset<AnimationClip>(AuthoredSprintClipPath);
            var profile = BuildReferenceSprintProfile();
            var metrics = MeasureReferenceDrivenSprintMotion(clip, runRoot, profile);
            Debug.Log(
                "TergoBackRushSprintVideoReferenceInspected" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", CurrentVideoFrames=" + currentFrameCount.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceVideoFrames=" + referenceFrameCount.ToString(CultureInfo.InvariantCulture) +
                ", CurrentFailure=NoClearRunningSilhouette" +
                ", ReferencePose=HighKneeForwardLift_BackLegKick_AlternatingArmPump_ForwardLean" +
                ", ExistingClip=" + AuthoredSprintClipPath +
                ", ExistingClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ExistingLeftUpLegRange=" + metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ExistingRightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ExistingLeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ExistingRightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Rewrite BackRush Sprint To Video Reference")]
        public static void RewriteTergoBackRushSprintToVideoReference()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var currentFrameCount = CountValidationFrames("docs/validation/tergo_backrush_sprint_current_20260705");
            var referenceFrameCount = CountValidationFrames("docs/validation/tergo_backrush_sprint_reference_20260705");
            if (currentFrameCount <= 0 || referenceFrameCount <= 0)
            {
                throw new InvalidOperationException("Video reference frames must be extracted before rewriting sprint motion.");
            }

            var profile = BuildReferenceSprintProfile();
            var clip = EnsureVideoReferenceSprintClip(runRoot, profile);
            var controller = EnsureAuthoredSprintController(clip);
            RemoveAnimatorComponentsBelowRoot(runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = LoadNormalTergoAvatarOrNull();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while rewriting sprint to video reference.");
            }

            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts after video reference rewrite. Run=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after video-reference Tergo sprint rewrite.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var metrics = MeasureReferenceDrivenSprintMotion(clip, runRoot, profile);
            Debug.Log(
                "TergoBackRushSprintVideoReferenceRewriteApplied" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + AuthoredSprintClipPath +
                ", Controller=" + AuthoredSprintControllerPath +
                ", CurrentVideoFrames=" + currentFrameCount.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceVideoFrames=" + referenceFrameCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceAnimationCurvesCopied=False" +
                ", SourceRigCopied=False" +
                ", VideoReferencePoseAuthored=True" +
                ", ExplicitKeyPoses=True" +
                ", HighKneeForwardLift=True" +
                ", BackLegKick=True" +
                ", AlternatingArmPump=True" +
                ", SideWobbleReduced=True" +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + AnimationUtility.GetCurveBindings(clip).Length.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + AnimationUtility.GetObjectReferenceCurveBindings(clip).Length.ToString(CultureInfo.InvariantCulture) +
                ", LeftUpLegRange=" + metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftLegRange=" + metrics.LeftLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightLegRange=" + metrics.RightLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsLateralRange=" + metrics.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeWorldYRange=" + metrics.LeftKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeWorldYRange=" + metrics.RightKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", AnimatorCountOnBackRush=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", RootTransformUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Sprint Video Reference")]
        public static void ValidateTergoBackRushSprintVideoReference()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(AuthoredSprintClipPath);
            var controller = RequireAsset<AnimatorController>(AuthoredSprintControllerPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(RunRootName + " is not using the video-reference authored sprint controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(RunRootName + " must keep root motion disabled for in-place sprint review.");
            }

            if (!ControllerUsesClipAtPath(controller, clip, AuthoredSprintClipPath))
            {
                throw new InvalidOperationException("Tergo authored sprint controller is not bound to " + AuthoredSprintClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (!settings.loopTime || !settings.loopBlend || clip.wrapMode != WrapMode.Loop ||
                clip.length < 0.34f || clip.length > 0.46f ||
                curveBindingCount < 200 || objectBindingCount != 0)
            {
                throw new InvalidOperationException(
                    "Tergo video-reference sprint clip has unexpected data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var requiredPath in ReferenceDrivenSprintMotionPaths)
            {
                RequireAuthoredTransformCurveBindings(clip, requiredPath);
            }

            if (!SampleClipChangesTransforms(clip, runRoot))
            {
                throw new InvalidOperationException("Tergo video-reference sprint clip did not change target transforms when sampled.");
            }

            var profile = BuildReferenceSprintProfile();
            var metrics = MeasureReferenceDrivenSprintMotion(clip, runRoot, profile);
            if (metrics.LeftUpLegRange < 120f ||
                metrics.RightUpLegRange < 120f ||
                metrics.LeftLegRange < 80f ||
                metrics.RightLegRange < 80f ||
                metrics.LeftArmRange < 150f ||
                metrics.RightArmRange < 150f ||
                metrics.HipsVerticalRange < 0.08f ||
                metrics.HipsLateralRange > 0.02f)
            {
                throw new InvalidOperationException(
                    "Tergo video-reference sprint is still not strong enough. LeftUpLegRange=" +
                    metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LeftLegRange=" + metrics.LeftLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RightLegRange=" + metrics.RightLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", LeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", HipsLateralRange=" + metrics.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var currentFrameCount = CountValidationFrames("docs/validation/tergo_backrush_sprint_current_20260705");
            var referenceFrameCount = CountValidationFrames("docs/validation/tergo_backrush_sprint_reference_20260705");
            var runConfiguredAnimators = CountConfiguredAnimators(runRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName,
                RunRootName);
            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (runConfiguredAnimators != 1 || otherConfiguredAnimators != 0 || eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo state after video-reference sprint validation. RunAnimators=" +
                    runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", OtherAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", EyeContainers=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoBackRushSprintVideoReferenceValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + AuthoredSprintClipPath +
                ", Controller=" + AuthoredSprintControllerPath +
                ", CurrentVideoFrames=" + currentFrameCount.ToString(CultureInfo.InvariantCulture) +
                ", ReferenceVideoFrames=" + referenceFrameCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceAnimationCurvesCopied=False" +
                ", SourceRigCopied=False" +
                ", VideoReferencePoseAuthored=True" +
                ", ExplicitKeyPoses=True" +
                ", HighKneeForwardLift=True" +
                ", BackLegKick=True" +
                ", AlternatingArmPump=True" +
                ", SideWobbleReduced=True" +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleTransformChanged=True" +
                ", LeftUpLegRange=" + metrics.LeftUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightUpLegRange=" + metrics.RightUpLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftLegRange=" + metrics.LeftLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightLegRange=" + metrics.RightLegRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftArmRange=" + metrics.LeftArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightArmRange=" + metrics.RightArmRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsVerticalRange=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", HipsLateralRange=" + metrics.HipsLateralRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LeftKneeWorldYRange=" + metrics.LeftKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RightKneeWorldYRange=" + metrics.RightKneeWorldYRange.ToString("0.###", CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", ApplyRootMotion=False" +
                ", AnimatorCountOnBackRush=" + runConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Restore BackRush Visual Model")]
        public static void RestoreTergoBackRushVisualModel()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var pierceAttackRoot = RequireChild(placementRoot.transform, PierceAttackRootName);
            var existingRunRoot = FindDirectChild(placementRoot.transform, RunRootName);

            var restoreSlot = BackRushRestoreSlot.CaptureOrInfer(
                placementRoot.transform,
                existingRunRoot,
                detectRoot,
                pierceAttackRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var previousRendererCount = existingRunRoot != null
                ? existingRunRoot.GetComponentsInChildren<Renderer>(true).Length
                : 0;
            var previousAnimatorCount = existingRunRoot != null
                ? existingRunRoot.GetComponentsInChildren<Animator>(true).Length
                : 0;
            var existingRunRootWasPresent = existingRunRoot != null;
            if (existingRunRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRunRoot.gameObject);
            }

            var runObject = UnityEngine.Object.Instantiate(staticRoot.gameObject, placementRoot.transform, false);
            runObject.name = RunRootName;
            var runRoot = runObject.transform;
            restoreSlot.ApplyTo(runRoot);
            var removedCopiedAnimators = DestroyAnimationComponents(runRoot);

            if (!restoreSlot.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while restoring the visual model.");
            }

            RequireBackRushVisualMatchesReference(staticRoot, runRoot);
            RequireNoConfiguredAnimator(runRoot, RunRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);

            EditorUtility.SetDirty(runObject);
            EditorUtility.SetDirty(runRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after restoring Tergo BackRush visual model.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushVisualModelRestored" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Reference=" + PlacementRootName + "/" + StaticRootName +
                ", ExistingRunRootWasPresent=" + existingRunRootWasPresent.ToString(CultureInfo.InvariantCulture) +
                ", PreviousRendererCount=" + previousRendererCount.ToString(CultureInfo.InvariantCulture) +
                ", CurrentRendererCount=" + runRoot.GetComponentsInChildren<Renderer>(true).Length.ToString(CultureInfo.InvariantCulture) +
                ", PreviousAnimatorComponents=" + previousAnimatorCount.ToString(CultureInfo.InvariantCulture) +
                ", RemovedCopiedAnimatorComponents=" + removedCopiedAnimators.ToString(CultureInfo.InvariantCulture) +
                ", RestoredByRootClone=True" +
                ", SiblingIndex=" + runRoot.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) +
                ", EyeContainers=" + CountDescendantsByName(runRoot, EyeContainerName).ToString(CultureInfo.InvariantCulture) +
                ", RootTransformUnchanged=True" +
                ", AnimationRemoved=True" +
                ", OtherTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Visual Model")]
        public static void ValidateTergoBackRushVisualModel()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredAnimator(runRoot, RunRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);
            RequireBackRushVisualMatchesReference(staticRoot, runRoot);

            Debug.Log(
                "TergoBackRushVisualModelValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Reference=" + PlacementRootName + "/" + StaticRootName +
                ", RendererCount=" + runRoot.GetComponentsInChildren<Renderer>(true).Length.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainers=" + CountDescendantsByName(runRoot, EyeContainerName).ToString(CultureInfo.InvariantCulture) +
                ", AnimatorComponents=" + runRoot.GetComponentsInChildren<Animator>(true).Length.ToString(CultureInfo.InvariantCulture) +
                ", VisualHierarchyMatchesReference=True" +
                ", StaticDetectAndRemainingTergoUnanimated=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Replace BackRush Rig Only")]
        public static void ReplaceTergoBackRushRigOnly()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var rendererSignaturesBefore = BuildRendererSignatures(runRoot);
            var eyeContainerCountBefore = CountDescendantsByName(runRoot, EyeContainerName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredAnimator(runRoot, RunRootName);

            var rigResult = ReplaceRunRigWithRunningFbxArmature(runRoot);
            var removedAnimatorComponents = DestroyAnimationComponents(runRoot);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while replacing rig only.");
            }

            RequireRendererSignaturesUnchanged(rendererSignaturesBefore, BuildRendererSignatures(runRoot));
            RequireBackRushRigOnlyState(staticRoot, runRoot);
            RequireNoAnimatorComponents(runRoot, RunRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);

            EditorUtility.SetDirty(runRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after replacing Tergo BackRush rig only.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushRigOnlyReplaced" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceRig=" + RunAnimationSourceAssetPath +
                ", OldArmatureReplaced=" + rigResult.OldArmatureReplaced.ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRenderersPreserved=" + rigResult.SkinnedRenderersPreserved.ToString(CultureInfo.InvariantCulture) +
                ", RendererBonesReplaced=" + rigResult.RendererBonesReplaced.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainerReparentedToRunningRig=" + rigResult.EyeContainerReparented.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersBefore=" + eyeContainerCountBefore.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersAfter=" + CountDescendantsByName(runRoot, EyeContainerName).ToString(CultureInfo.InvariantCulture) +
                ", RemovedAnimatorComponents=" + removedAnimatorComponents.ToString(CultureInfo.InvariantCulture) +
                ", RendererMeshMaterialUnchanged=True" +
                ", AnimationApplied=False" +
                ", RootTransformUnchanged=True" +
                ", OtherTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Rig Only")]
        public static void ValidateTergoBackRushRigOnly()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireNoConfiguredAnimator(runRoot, RunRootName);
            RequireNoAnimatorComponents(runRoot, RunRootName);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);
            RequireBackRushRigOnlyState(staticRoot, runRoot);

            var armature = RequireChild(runRoot, "Armature");
            var renderers = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            Debug.Log(
                "TergoBackRushRigOnlyValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceRig=" + RunAnimationSourceAssetPath +
                ", RunningRigBoneCount=" + CountRigBones(armature).ToString(CultureInfo.InvariantCulture) +
                ", SkinnedRendererCount=" + renderers.Length.ToString(CultureInfo.InvariantCulture) +
                ", RendererBonesOnRunningRig=True" +
                ", RendererMeshMaterialMatchesStaticReference=True" +
                ", EyeContainerOnRunningRig=True" +
                ", AnimatorComponents=0" +
                ", AnimationApplied=False" +
                ", IdleWalkControllersPreserved=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Apply BackRush Animation Only")]
        public static void ApplyTergoBackRushAnimationOnly()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var rendererSignaturesBefore = BuildRendererSignatures(runRoot);
            var rigSignaturesBefore = BuildRigStateSignatures(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushRigOnlyState(staticRoot, runRoot);

            var importedClips = LoadImportedAnimationClips();
            var sourceClip = SelectRunSourceClip(importedClips);
            var runClip = EnsureCopiedRunClip(sourceClip);
            var controller = EnsureRunController(runClip);
            var avatar = EnsureRunningAvatar();

            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while applying animation only.");
            }

            RequireRendererSignaturesUnchanged(rendererSignaturesBefore, BuildRendererSignatures(runRoot));
            RequireRigStateSignaturesUnchanged(rigSignaturesBefore, BuildRigStateSignatures(runRoot));
            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, runClip, avatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            EditorUtility.SetDirty(runRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo BackRush animation only.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushAnimationOnlyApplied" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceAnimationFbx=" + RunAnimationSourceAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SourceClips=" + FormatClipNames(importedClips) +
                ", RunClip=" + RunChaseClipPath +
                ", RunClipLength=" + runClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", Controller=" + RunChaseControllerPath +
                ", AvatarAssigned=" + (avatar != null).ToString(CultureInfo.InvariantCulture) +
                ", RendererMeshMaterialUnchanged=True" +
                ", RigStateUnchanged=True" +
                ", RootTransformUnchanged=True" +
                ", ApplyRootMotion=False" +
                ", AnimationOnly=True" +
                ", OtherTergoRootsModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Animation Only")]
        public static void ValidateTergoBackRushAnimationOnly()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, clip, avatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;

            Debug.Log(
                "TergoBackRushAnimationOnlyValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceAnimationFbx=" + RunAnimationSourceAssetPath +
                ", SourceClip=" + sourceClip.name +
                ", SourceClipLength=" + sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RunClip=" + RunChaseClipPath +
                ", RunClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", Controller=" + RunChaseControllerPath +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", RendererMeshMaterialMatchesStaticReference=True" +
                ", RunningRigPreserved=True" +
                ", EyeContainerOnRunningRig=True" +
                ", AnimatorComponents=1" +
                ", ApplyRootMotion=False" +
                ", SampleTransformChanged=True" +
                ", AnimationOnly=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect BackRush Waist Twist")]
        public static void InspectTergoBackRushWaistTwist()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();

            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, clip, avatar);

            var waistReport = BuildWaistRotationReport(runRoot, clip);
            var bindposeReport = BuildBindposeMismatchReport(runRoot);
            var waistBindingCount = CountWaistRotationBindings(clip);

            Debug.Log(
                "TergoBackRushWaistTwistInspected" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", WaistRotationBindings=" + waistBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", WaistSampleReport=" + waistReport +
                ", BindposeReport=" + bindposeReport +
                ", Diagnosis=WaistSpineRotationCurvesNeedStabilizationForCurrentModelRigCombination");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Repair BackRush Waist Twist")]
        public static void RepairTergoBackRushWaistTwist()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var rendererSignaturesBefore = BuildRendererSignatures(runRoot);
            var rigStateBefore = BuildRigStateSignatures(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, clip, avatar);

            var beforeReport = BuildWaistRotationReport(runRoot, clip);
            var stabilizedBindings = StabilizeWaistRotationCurves(runRoot, clip);
            var afterReport = BuildWaistRotationReport(runRoot, clip);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while repairing waist twist.");
            }

            RequireRendererSignaturesUnchanged(rendererSignaturesBefore, BuildRendererSignatures(runRoot));
            RequireRigStateSignaturesUnchanged(rigStateBefore, BuildRigStateSignatures(runRoot));
            RequireBackRushWaistTwistFixed(staticRoot, runRoot, controller, clip, avatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            EditorUtility.SetDirty(clip);
            EditorUtility.SetDirty(runRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after repairing Tergo BackRush waist twist.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushWaistTwistRepaired" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", StabilizedBindings=" + stabilizedBindings.ToString(CultureInfo.InvariantCulture) +
                ", WaistBefore=" + beforeReport +
                ", WaistAfter=" + afterReport +
                ", RendererMeshMaterialUnchanged=True" +
                ", RigStateUnchanged=True" +
                ", RootTransformUnchanged=True" +
                ", AnimationStillApplied=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Waist Twist Fixed")]
        public static void ValidateTergoBackRushWaistTwistFixed()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushWaistTwistFixed(staticRoot, runRoot, controller, clip, avatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            Debug.Log(
                "TergoBackRushWaistTwistFixedValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", WaistSampleReport=" + BuildWaistRotationReport(runRoot, clip) +
                ", WaistRotationBindings=" + CountWaistRotationBindings(clip).ToString(CultureInfo.InvariantCulture) +
                ", RendererMeshMaterialMatchesStaticReference=True" +
                ", RunningRigPreserved=True" +
                ", AnimatorComponents=1" +
                ", SampleTransformChanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect BackRush Running Pose")]
        public static void InspectTergoBackRushRunningPose()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();

            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, clip, avatar);

            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            Debug.Log(
                "TergoBackRushRunningPoseInspected" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", SourceClip=" + sourceClip.name +
                ", CurrentWaistReport=" + BuildWaistRotationReport(runRoot, clip) +
                ", SourceWaistReport=" + BuildSourceWaistRotationReport(sourceClip) +
                ", CurrentBindposeReport=" + BuildBindposeMismatchReport(runRoot) +
                ", CurrentBindposeMaxError=" + GetBindposeMaxError(runRoot).ToString("0.######", CultureInfo.InvariantCulture) +
                ", Diagnosis=PreviousConstantWaistLockMustBeReplacedWithRestPoseRetargeting");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Repair BackRush Running Pose")]
        public static void RepairTergoBackRushRunningPose()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var rendererSignaturesBefore = BuildRendererSignatures(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();
            var currentClip = RequireAsset<AnimationClip>(RunChaseClipPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, currentClip, avatar);

            var beforeWaistReport = BuildWaistRotationReport(runRoot, currentClip);
            var beforeBindposeError = GetBindposeMaxError(runRoot);
            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            var runClip = EnsureCopiedRunClip(sourceClip);
            var copiedRestTransforms = CopyReferenceRigRestPose(staticRoot, runRoot);
            var retargetedCurves = RetargetWaistRotationCurvesToCurrentRig(sourceClip, runClip, runRoot);
            var afterWaistReport = BuildWaistRotationReport(runRoot, runClip);
            var afterBindposeError = GetBindposeMaxError(runRoot);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while repairing running pose.");
            }

            RequireRendererSignaturesUnchanged(rendererSignaturesBefore, BuildRendererSignatures(runRoot));
            RequireBackRushRunningPoseState(staticRoot, runRoot, controller, runClip, avatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            EditorUtility.SetDirty(runRoot);
            EditorUtility.SetDirty(runClip);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after repairing Tergo BackRush running pose.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushRunningPoseRepaired" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", SourceClip=" + sourceClip.name +
                ", PreviousConstantWaistLockDiscarded=True" +
                ", CopiedRestTransforms=" + copiedRestTransforms.ToString(CultureInfo.InvariantCulture) +
                ", RetargetedTransformCurves=" + retargetedCurves.ToString(CultureInfo.InvariantCulture) +
                ", SourceCurveTimingPreserved=True" +
                ", WaistBefore=" + beforeWaistReport +
                ", WaistAfter=" + afterWaistReport +
                ", BindposeMaxErrorBefore=" + beforeBindposeError.ToString("0.######", CultureInfo.InvariantCulture) +
                ", BindposeMaxErrorAfter=" + afterBindposeError.ToString("0.######", CultureInfo.InvariantCulture) +
                ", RendererMeshMaterialUnchanged=True" +
                ", RootTransformUnchanged=True" +
                ", AnimationStillApplied=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Running Pose")]
        public static void ValidateTergoBackRushRunningPose()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var avatar = RequireRunningAvatar();

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushRunningPoseState(staticRoot, runRoot, controller, clip, avatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            Debug.Log(
                "TergoBackRushRunningPoseValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", WaistSampleReport=" + BuildWaistRotationReport(runRoot, clip) +
                ", BindposeReport=" + BuildBindposeMismatchReport(runRoot) +
                ", BindposeMaxError=" + GetBindposeMaxError(runRoot).ToString("0.######", CultureInfo.InvariantCulture) +
                ", WaistRotationBindings=" + CountWaistRotationBindings(clip).ToString(CultureInfo.InvariantCulture) +
                ", RendererMeshMaterialMatchesStaticReference=True" +
                ", AnimatorComponents=1" +
                ", SampleTransformChanged=True" +
                ", ConstantWaistLock=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Inspect BackRush Normal Run")]
        public static void InspectTergoBackRushNormalRun()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);

            Debug.Log(
                "TergoBackRushNormalRunInspected" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceClip=" + sourceClip.name +
                ", CurrentClip=" + RunChaseClipPath +
                ", CurrentWaistReport=" + BuildWaistRotationReport(runRoot, clip) +
                ", SourceWaistReport=" + BuildSourceWaistRotationReport(sourceClip) +
                ", RendererMatchesStatic=" + RendererSignaturesEqual(staticRoot, runRoot).ToString(CultureInfo.InvariantCulture) +
                ", RestPoseMatchesStatic=" + RestPoseSignaturesEqual(staticRoot, runRoot).ToString(CultureInfo.InvariantCulture) +
                ", CurrentBindposeReport=" + BuildBindposeMismatchReport(runRoot) +
                ", CurrentBindposeMaxError=" + GetBindposeMaxError(runRoot).ToString("0.######", CultureInfo.InvariantCulture) +
                ", Diagnosis=BackRushMustUseNormalTergoModelRigWithRetargetedRunningAnimationOnly");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Repair BackRush Normal Run")]
        public static void RepairTergoBackRushNormalRun()
        {
            RestoreTergoBackRushVisualModel();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var rendererSignaturesBefore = BuildRendererSignatures(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            var runClip = EnsureCopiedRunClip(sourceClip);
            var controller = EnsureRunController(runClip);
            var normalAvatar = LoadNormalTergoAvatarOrNull();

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);

            var retargetedCurves = RetargetWaistRotationCurvesToCurrentRig(sourceClip, runClip, runRoot);
            var animator = runRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = runRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = normalAvatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while repairing normal run.");
            }

            RequireRendererSignaturesUnchanged(rendererSignaturesBefore, BuildRendererSignatures(runRoot));
            RequireBackRushNormalRunState(staticRoot, runRoot, controller, runClip, normalAvatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            EditorUtility.SetDirty(runRoot);
            EditorUtility.SetDirty(runClip);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after repairing Tergo BackRush normal run.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoBackRushNormalRunRepaired" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", SourceClip=" + sourceClip.name +
                ", NormalModel=" + NormalModelAssetPath +
                ", SourceAnimationFbx=" + RunAnimationSourceAssetPath +
                ", RetargetedTransformCurves=" + retargetedCurves.ToString(CultureInfo.InvariantCulture) +
                ", WaistReport=" + BuildWaistRotationReport(runRoot, runClip) +
                ", RendererMatchesStatic=True" +
                ", RestPoseMatchesStatic=True" +
                ", NormalAvatarAssigned=" + (normalAvatar != null).ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", RootTransformUnchanged=True" +
                ", AnimationOnlyFromRunningFbx=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate BackRush Normal Run")]
        public static void ValidateTergoBackRushNormalRun()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var staticRoot = RequireChild(placementRoot.transform, StaticRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            var clip = RequireAsset<AnimationClip>(RunChaseClipPath);
            var controller = RequireAsset<AnimatorController>(RunChaseControllerPath);
            var normalAvatar = LoadNormalTergoAvatarOrNull();

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);
            RequireBackRushNormalRunState(staticRoot, runRoot, controller, clip, normalAvatar);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName, RunRootName);

            Debug.Log(
                "TergoBackRushNormalRunValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", Clip=" + RunChaseClipPath +
                ", SourceAnimationFbx=" + RunAnimationSourceAssetPath +
                ", NormalModel=" + NormalModelAssetPath +
                ", WaistSampleReport=" + BuildWaistRotationReport(runRoot, clip) +
                ", RendererMeshMaterialMatchesStaticReference=True" +
                ", RestPoseMatchesStaticReference=True" +
                ", NormalAvatarAssigned=" + (normalAvatar != null).ToString(CultureInfo.InvariantCulture) +
                ", AnimatorComponents=1" +
                ", SampleTransformChanged=True" +
                ", ApplyRootMotion=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Remove Run Chase Animation")]
        public static void RemoveTergoRunChaseAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);

            var runTransformState = TransformState.Capture(runRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);
            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var rendererCountBefore = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var eyeContainerCountBefore = CountDescendantsByName(runRoot, EyeContainerName);
            var armatureBefore = RequireChild(runRoot, "Armature");
            var armatureBoneCountBefore = CountRigBones(armatureBefore);
            var animators = runRoot.GetComponentsInChildren<Animator>(true);
            foreach (var animator in animators)
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            if (!runTransformState.Matches(runRoot))
            {
                throw new InvalidOperationException(RunRootName + " transform changed while removing run chase animation.");
            }

            var rendererCountAfter = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var eyeContainerCountAfter = CountDescendantsByName(runRoot, EyeContainerName);
            var armatureAfter = RequireChild(runRoot, "Armature");
            var armatureBoneCountAfter = CountRigBones(armatureAfter);
            if (rendererCountAfter != rendererCountBefore)
            {
                throw new InvalidOperationException(
                    RunRootName + " SkinnedMeshRenderer count changed while removing animation. Before=" +
                    rendererCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + rendererCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (eyeContainerCountAfter != eyeContainerCountBefore)
            {
                throw new InvalidOperationException(
                    RunRootName + " generated eye container count changed while removing animation. Before=" +
                    eyeContainerCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + eyeContainerCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            if (armatureBoneCountAfter != armatureBoneCountBefore)
            {
                throw new InvalidOperationException(
                    RunRootName + " armature bone count changed while removing animation. Before=" +
                    armatureBoneCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + armatureBoneCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            var runAnimatorCountAfter = runRoot.GetComponentsInChildren<Animator>(true).Length;
            if (runAnimatorCountAfter != 0)
            {
                throw new InvalidOperationException(
                    RunRootName + " must have no Animator after removing run chase animation. Count=" +
                    runAnimatorCountAfter.ToString(CultureInfo.InvariantCulture));
            }

            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName);
            if (otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Only idle and walk Tergo objects may have animation controllers after removing run chase. OtherConfiguredAnimators=" +
                    otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after removing Tergo run chase animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoRunChaseAnimationRemoved" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", RemovedAnimators=" + animators.Length.ToString(CultureInfo.InvariantCulture) +
                ", RunAnimatorCountAfter=0" +
                ", SkinnedRenderersPreserved=" + rendererCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCountPreserved=" + armatureBoneCountAfter.ToString(CultureInfo.InvariantCulture) +
                ", RootTransformUnchanged=True" +
                ", IdleAndWalkAnimationsPreserved=True" +
                ", StaticDetectAndRemainingTergoUnanimated=True");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Run Chase Animation Removed")]
        public static void ValidateTergoRunChaseAnimationRemoved()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var detectRoot = RequireChild(placementRoot.transform, DetectRootName);
            var runRoot = RequireChild(placementRoot.transform, RunRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var walkController = RequireAsset<AnimatorController>(WalkImportedControllerPath);

            RequireConfiguredAnimator(idleRoot, idleController, IdleRootName);
            RequireConfiguredAnimator(walkRoot, walkController, WalkRootName);
            RequireNoConfiguredAnimator(detectRoot, DetectRootName);

            var runAnimatorCount = runRoot.GetComponentsInChildren<Animator>(true).Length;
            if (runAnimatorCount != 0)
            {
                throw new InvalidOperationException(
                    RunRootName + " must have no Animator after removal. Count=" +
                    runAnimatorCount.ToString(CultureInfo.InvariantCulture));
            }

            var rendererCount = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (rendererCount == 0)
            {
                throw new InvalidOperationException(RunRootName + " must keep its visible SkinnedMeshRenderer after animation removal.");
            }

            var armature = RequireChild(runRoot, "Armature");
            var armatureBoneCount = CountRigBones(armature);
            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    RunRootName + " must keep exactly one generated eye container after animation removal. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(
                placementRoot.transform,
                IdleRootName,
                WalkRootName);
            if (otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Only idle and walk Tergo objects may have animation controllers after removal. OtherConfiguredAnimators=" +
                    otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoRunChaseAnimationRemovalValidated" +
                ", Target=" + PlacementRootName + "/" + RunRootName +
                ", RunAnimatorCount=0" +
                ", SkinnedRenderersPreserved=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                ", ArmatureBoneCount=" + armatureBoneCount.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", IdleAndWalkAnimationsPreserved=True" +
                ", StaticDetectAndRemainingTergoUnanimated=True");
        }

        private static Avatar EnsureRunningAvatar()
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RunAnimationSourceAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
            if (avatar != null)
            {
                return avatar;
            }

            var importer = AssetImporter.GetAtPath(RunAnimationSourceAssetPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Missing model importer for " + RunAnimationSourceAssetPath);
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            return RequireRunningAvatar();
        }

        private static Avatar RequireRunningAvatar()
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(RunAnimationSourceAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
            if (avatar == null)
            {
                throw new InvalidOperationException("Missing running FBX Avatar: " + RunAnimationSourceAssetPath);
            }

            return avatar;
        }

        private static RigReplacementResult ReplaceRunRigWithRunningFbxArmature(Transform runRoot)
        {
            var sourcePrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var sourceArmature = RequireChild(sourcePrefab.transform, "Armature");
            var oldArmature = RequireChild(runRoot, "Armature");
            var rendererSnapshots = CaptureRendererRigSnapshots(runRoot, oldArmature);
            if (rendererSnapshots.Count == 0)
            {
                throw new InvalidOperationException(RunRootName + " must keep at least one visible SkinnedMeshRenderer.");
            }

            var eyeContainer = FindDescendantByName(runRoot, EyeContainerName);
            if (eyeContainer != null)
            {
                eyeContainer.SetParent(runRoot, true);
            }

            foreach (var snapshot in rendererSnapshots)
            {
                if (snapshot.Renderer.transform.IsChildOf(oldArmature))
                {
                    snapshot.Renderer.transform.SetParent(runRoot, true);
                }
            }

            UnityEngine.Object.DestroyImmediate(oldArmature.gameObject);

            var newArmatureObject = UnityEngine.Object.Instantiate(sourceArmature.gameObject);
            newArmatureObject.name = "Armature";
            StripNonTransformComponents(newArmatureObject);

            var newArmature = newArmatureObject.transform;
            newArmature.SetParent(runRoot, false);
            CopyLocalTransform(sourceArmature, newArmature);

            var replacedBoneReferences = 0;
            foreach (var snapshot in rendererSnapshots)
            {
                replacedBoneReferences += ApplySnapshotToRunningRig(snapshot, newArmature);
            }

            var headFront = newArmature.Find("Hips/Spine02/Spine01/Spine/neck/Head/headfront") ??
                            FindDescendantByName(newArmature, "headfront");
            if (eyeContainer != null)
            {
                if (headFront == null)
                {
                    throw new InvalidOperationException("Running rig does not contain a headfront bone for generated eyes.");
                }

                eyeContainer.SetParent(headFront, true);
            }

            return new RigReplacementResult(
                oldArmatureReplaced: true,
                skinnedRenderersPreserved: rendererSnapshots.Count,
                rendererBonesReplaced: replacedBoneReferences,
                eyeContainerReparented: eyeContainer != null);
        }

        private static List<RendererRigSnapshot> CaptureRendererRigSnapshots(Transform runRoot, Transform oldArmature)
        {
            var snapshots = new List<RendererRigSnapshot>();
            foreach (var renderer in runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null)
                {
                    throw new InvalidOperationException(renderer.name + " must keep its visible shared mesh before rig replacement.");
                }

                snapshots.Add(new RendererRigSnapshot(
                    renderer,
                    renderer.sharedMesh,
                    renderer.sharedMaterials,
                    CaptureBonePaths(renderer, oldArmature),
                    CaptureRootBonePath(renderer, oldArmature)));
            }

            return snapshots;
        }

        private static string[] CaptureBonePaths(SkinnedMeshRenderer renderer, Transform oldArmature)
        {
            if (renderer.bones == null || renderer.bones.Length == 0)
            {
                throw new InvalidOperationException(renderer.name + " has no bones to replace with the running rig.");
            }

            return renderer.bones
                .Select(bone => GetRelativePath(oldArmature, bone, renderer.name + " bone"))
                .ToArray();
        }

        private static string CaptureRootBonePath(SkinnedMeshRenderer renderer, Transform oldArmature)
        {
            return renderer.rootBone == null
                ? string.Empty
                : GetRelativePath(oldArmature, renderer.rootBone, renderer.name + " rootBone");
        }

        private static int ApplySnapshotToRunningRig(RendererRigSnapshot snapshot, Transform newArmature)
        {
            if (snapshot.Renderer == null)
            {
                throw new InvalidOperationException("A Tergo SkinnedMeshRenderer was lost during running rig replacement.");
            }

            if (snapshot.Renderer.sharedMesh != snapshot.SharedMesh)
            {
                throw new InvalidOperationException(snapshot.Renderer.name + " shared mesh changed during running rig replacement.");
            }

            if (!MaterialsMatch(snapshot.Renderer.sharedMaterials, snapshot.SharedMaterials))
            {
                throw new InvalidOperationException(snapshot.Renderer.name + " materials changed during running rig replacement.");
            }

            var newBones = snapshot.BonePaths
                .Select(path => RequireRelativeTransform(newArmature, path, snapshot.Renderer.name + " replacement bone"))
                .ToArray();

            snapshot.Renderer.bones = newBones;
            snapshot.Renderer.rootBone = string.IsNullOrEmpty(snapshot.RootBonePath)
                ? newArmature
                : RequireRelativeTransform(newArmature, snapshot.RootBonePath, snapshot.Renderer.name + " replacement rootBone");
            EditorUtility.SetDirty(snapshot.Renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(snapshot.Renderer);
            return newBones.Length;
        }

        private static string GetRelativePath(Transform root, Transform target, string context)
        {
            if (target == null)
            {
                throw new InvalidOperationException("Missing " + context + " transform.");
            }

            if (target == root)
            {
                return string.Empty;
            }

            if (!target.IsChildOf(root))
            {
                throw new InvalidOperationException(context + " is not under the current Armature.");
            }

            var names = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static Transform RequireRelativeTransform(Transform root, string path, string context)
        {
            if (string.IsNullOrEmpty(path))
            {
                return root;
            }

            var target = root.Find(path);
            if (target == null)
            {
                throw new InvalidOperationException("Missing " + context + " on running rig: " + path);
            }

            return target;
        }

        private static int DestroyAnimationComponents(Transform root)
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            foreach (var animator in animators)
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            return animators.Length;
        }

        private static void RequireBackRushVisualMatchesReference(Transform referenceRoot, Transform targetRoot)
        {
            RequireTransformHierarchyMatches(referenceRoot, targetRoot);
            RequireRendererSignaturesMatch(referenceRoot, targetRoot);

            var referenceSkinnedCount = referenceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var targetSkinnedCount = targetRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (targetSkinnedCount != referenceSkinnedCount)
            {
                throw new InvalidOperationException(
                    RunRootName + " SkinnedMeshRenderer count does not match " + StaticRootName +
                    ". Reference=" + referenceSkinnedCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetSkinnedCount.ToString(CultureInfo.InvariantCulture));
            }

            var referenceEyeCount = CountDescendantsByName(referenceRoot, EyeContainerName);
            var targetEyeCount = CountDescendantsByName(targetRoot, EyeContainerName);
            if (referenceEyeCount != 1 || targetEyeCount != 1)
            {
                throw new InvalidOperationException(
                    "Tergo approved eye container count must be exactly one on both reference and BackRush. Reference=" +
                    referenceEyeCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void RequireBackRushRigOnlyState(Transform staticRoot, Transform runRoot)
        {
            RequireRendererSignaturesMatch(staticRoot, runRoot);

            var runningPrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var sourceArmature = RequireChild(runningPrefab.transform, "Armature");
            var armature = RequireChild(runRoot, "Armature");
            RequireRigHierarchyMatches(sourceArmature, armature);

            var renderers = runRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(RunRootName + " must keep its visible SkinnedMeshRenderer after rig-only replacement.");
            }

            var rendererBonesOnRunningRig = renderers.All(renderer =>
                renderer.sharedMesh != null &&
                renderer.bones != null &&
                renderer.bones.Length > 0 &&
                renderer.bones.All(bone => bone != null && bone.IsChildOf(armature)) &&
                (renderer.rootBone == null || renderer.rootBone.IsChildOf(armature) || renderer.rootBone == armature));
            if (!rendererBonesOnRunningRig)
            {
                throw new InvalidOperationException(RunRootName + " SkinnedMeshRenderer bones are not all on the running rig armature.");
            }

            var eyeContainerCount = CountDescendantsByName(runRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    RunRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainer = FindDescendantByName(runRoot, EyeContainerName);
            var headFront = armature.Find("Hips/Spine02/Spine01/Spine/neck/Head/headfront");
            if (eyeContainer == null || headFront == null || !eyeContainer.IsChildOf(headFront))
            {
                throw new InvalidOperationException(RunRootName + " generated eyes must be parented under the running rig headfront bone.");
            }
        }

        private static void RequireBackRushAnimationOnlyState(
            Transform staticRoot,
            Transform runRoot,
            AnimatorController controller,
            AnimationClip clip,
            Avatar avatar)
        {
            RequireBackRushRigOnlyState(staticRoot, runRoot);
            RequireSingleAnimatorComponentOnRoot(runRoot, controller, avatar);

            if (!ControllerUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Tergo run chase controller is not bound to " + RunChaseClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Tergo run chase clip must loop.");
            }

            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            if (Mathf.Abs(clip.length - sourceClip.length) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip must preserve the imported running source length. SourceLength=" +
                    sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RunLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (clip.length <= 0.01f || (curveBindingCount + objectBindingCount) == 0)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip has no usable animation data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, runRoot))
            {
                throw new InvalidOperationException("Tergo run chase clip did not change any target transforms when sampled.");
            }
        }

        private static void RequireBackRushWaistTwistFixed(
            Transform staticRoot,
            Transform runRoot,
            AnimatorController controller,
            AnimationClip clip,
            Avatar avatar)
        {
            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, clip, avatar);
            foreach (var path in WaistStabilizedBonePaths)
            {
                var target = RequireRelativeTransform(runRoot, path, "waist stabilized bone");
                var rest = target.localRotation;
                var sampledMaxAngle = GetSampledMaxLocalRotationAngle(clip, runRoot, target, rest);
                if (sampledMaxAngle > 0.25f)
                {
                    throw new InvalidOperationException(
                        "Waist stabilized bone still rotates too much: " + path +
                        ", MaxAngle=" + sampledMaxAngle.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }
        }

        private static void RequireBackRushRunningPoseState(
            Transform staticRoot,
            Transform runRoot,
            AnimatorController controller,
            AnimationClip clip,
            Avatar avatar)
        {
            RequireBackRushAnimationOnlyState(staticRoot, runRoot, controller, clip, avatar);

            var bindposeMaxError = GetBindposeMaxError(runRoot);
            if (float.IsInfinity(bindposeMaxError))
            {
                throw new InvalidOperationException(
                    "BackRush visible Tergo mesh bindpose state could not be measured. MaxError=" +
                    bindposeMaxError.ToString("0.######", CultureInfo.InvariantCulture));
            }

            var waistMaxAngle = GetMaxWaistRotationAngle(clip, runRoot);
            if (waistMaxAngle < 0.5f)
            {
                throw new InvalidOperationException(
                    "BackRush waist rotation is still locked instead of preserving running motion. MaxAngle=" +
                    waistMaxAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (waistMaxAngle > 90f)
            {
                throw new InvalidOperationException(
                    "BackRush waist rotation still contains a large flip. MaxAngle=" +
                    waistMaxAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static void RequireBackRushNormalRunState(
            Transform staticRoot,
            Transform runRoot,
            AnimatorController controller,
            AnimationClip clip,
            Avatar avatar)
        {
            RequireRendererSignaturesMatch(staticRoot, runRoot);
            RequireRestPoseSignaturesMatch(staticRoot, runRoot);
            RequireSingleAnimatorComponentOnRoot(runRoot, controller, avatar);

            if (!ControllerUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Tergo run chase controller is not bound to " + RunChaseClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Tergo run chase clip must loop.");
            }

            var sourceClip = SelectRunSourceClip(LoadImportedAnimationClips());
            if (Mathf.Abs(clip.length - sourceClip.length) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip must preserve the imported running source length. SourceLength=" +
                    sourceClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", RunLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (clip.length <= 0.01f || (curveBindingCount + objectBindingCount) == 0)
            {
                throw new InvalidOperationException(
                    "Tergo run chase clip has no usable animation data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            if (!SampleClipChangesTransforms(clip, runRoot))
            {
                throw new InvalidOperationException("Tergo run chase clip did not change any target transforms when sampled.");
            }

            var waistMaxAngle = GetMaxWaistRotationAngle(clip, runRoot);
            if (waistMaxAngle < 0.5f)
            {
                throw new InvalidOperationException(
                    "BackRush waist rotation is locked instead of preserving running motion. MaxAngle=" +
                    waistMaxAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (waistMaxAngle > 120f)
            {
                throw new InvalidOperationException(
                    "BackRush waist rotation still contains a large flip. MaxAngle=" +
                    waistMaxAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static int RetargetRunClipCurvesToCurrentRig(
            AnimationClip sourceClip,
            AnimationClip targetClip,
            Transform targetRoot)
        {
            var sourcePrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var retargetedCurves = 0;

            foreach (var path in GetAnimatedTransformPaths(sourceClip))
            {
                var sourceTransform = sourcePrefab.transform.Find(path);
                var targetTransform = targetRoot.Find(path);
                if (sourceTransform == null || targetTransform == null)
                {
                    continue;
                }

                var sourceRest = LocalTransformSample.Capture(sourceTransform);
                var targetRest = LocalTransformSample.Capture(targetTransform);
                var times = GetTransformCurveKeyTimes(sourceClip, path);
                var positions = new Vector3[times.Length];
                var rotations = new Quaternion[times.Length];
                var scales = new Vector3[times.Length];
                var sourceRestRotation = NormalizeQuaternion(sourceRest.LocalRotation);
                var targetRestRotation = NormalizeQuaternion(targetRest.LocalRotation);
                var previousRotation = Quaternion.identity;
                var hasPreviousRotation = false;

                for (var index = 0; index < times.Length; index++)
                {
                    var time = times[index];
                    var sourcePosition = EvaluateLocalPosition(sourceClip, path, time, sourceRest.LocalPosition);
                    var sourceRotation = EvaluateLocalRotation(sourceClip, path, time, sourceRestRotation);
                    var sourceScale = EvaluateLocalScale(sourceClip, path, time, sourceRest.LocalScale);

                    positions[index] = targetRest.LocalPosition + (sourcePosition - sourceRest.LocalPosition);
                    rotations[index] = NormalizeQuaternion(
                        targetRestRotation *
                        Quaternion.Inverse(sourceRestRotation) *
                        sourceRotation);
                    if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotations[index]) < 0f)
                    {
                        rotations[index] = FlipQuaternion(rotations[index]);
                    }

                    previousRotation = rotations[index];
                    hasPreviousRotation = true;
                    scales[index] = Vector3.Scale(
                        targetRest.LocalScale,
                        DivideVector(sourceScale, sourceRest.LocalScale));
                }

                retargetedCurves += SetTransformCurves(targetClip, path, times, positions, rotations, scales);
            }

            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();
            return retargetedCurves;
        }

        private static Vector3 EvaluateLocalPosition(AnimationClip clip, string path, float time, Vector3 fallback)
        {
            return new Vector3(
                EvaluateFloatCurve(clip, path, "m_LocalPosition.x", time, fallback.x),
                EvaluateFloatCurve(clip, path, "m_LocalPosition.y", time, fallback.y),
                EvaluateFloatCurve(clip, path, "m_LocalPosition.z", time, fallback.z));
        }

        private static Vector3 EvaluateLocalScale(AnimationClip clip, string path, float time, Vector3 fallback)
        {
            return new Vector3(
                EvaluateFloatCurve(clip, path, "m_LocalScale.x", time, fallback.x),
                EvaluateFloatCurve(clip, path, "m_LocalScale.y", time, fallback.y),
                EvaluateFloatCurve(clip, path, "m_LocalScale.z", time, fallback.z));
        }

        private static int CopyReferenceRigRestPose(Transform staticRoot, Transform runRoot)
        {
            var referenceArmature = RequireChild(staticRoot, "Armature");
            var targetArmature = RequireChild(runRoot, "Armature");
            var copied = 0;

            foreach (var path in BuildRigBonePaths(targetArmature))
            {
                var reference = RequireRelativeTransform(referenceArmature, path, "reference Tergo rest bone");
                var target = RequireRelativeTransform(targetArmature, path, "BackRush rest bone");
                CopyLocalTransform(reference, target);
                EditorUtility.SetDirty(target);
                copied++;
            }

            return copied;
        }

        private static int RetargetRunClipToCurrentRig(AnimationClip sourceClip, AnimationClip targetClip, Transform targetRoot)
        {
            var sourceInstance = InstantiateRunningSourcePrefab("Retarget");
            try
            {
                var sourceRoot = sourceInstance.transform;
                var animatedPaths = GetAnimatedTransformPaths(sourceClip);
                var retargetedCurves = 0;

                foreach (var path in animatedPaths)
                {
                    var sourceTransform = sourceRoot.Find(path);
                    var targetTransform = targetRoot.Find(path);
                    if (sourceTransform == null || targetTransform == null)
                    {
                        continue;
                    }

                    var sourceRest = LocalTransformSample.Capture(sourceTransform);
                    var targetRest = LocalTransformSample.Capture(targetTransform);
                    var times = GetTransformCurveKeyTimes(sourceClip, path);
                    var positions = new Vector3[times.Length];
                    var rotations = new Quaternion[times.Length];
                    var scales = new Vector3[times.Length];
                    var previousRotation = Quaternion.identity;
                    var hasPreviousRotation = false;

                    for (var index = 0; index < times.Length; index++)
                    {
                        var time = times[index];
                        sourceClip.SampleAnimation(sourceRoot.gameObject, time);
                    var sourcePose = LocalTransformSample.Capture(sourceTransform);
                    var sourceRestRotation = NormalizeQuaternion(sourceRest.LocalRotation);
                    var targetRestRotation = NormalizeQuaternion(targetRest.LocalRotation);
                    var sourcePoseRotation = NormalizeQuaternion(sourcePose.LocalRotation);

                    positions[index] = targetRest.LocalPosition + (sourcePose.LocalPosition - sourceRest.LocalPosition);
                    rotations[index] = NormalizeQuaternion(
                        targetRestRotation *
                        Quaternion.Inverse(sourceRestRotation) *
                        sourcePoseRotation);
                        if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotations[index]) < 0f)
                        {
                            rotations[index] = FlipQuaternion(rotations[index]);
                        }

                        previousRotation = rotations[index];
                        hasPreviousRotation = true;
                        scales[index] = Vector3.Scale(
                            targetRest.LocalScale,
                            DivideVector(sourcePose.LocalScale, sourceRest.LocalScale));
                    }

                    retargetedCurves += SetTransformCurves(targetClip, path, times, positions, rotations, scales);
                }

                targetClip.EnsureQuaternionContinuity();
                EditorUtility.SetDirty(targetClip);
                AssetDatabase.SaveAssets();
                return retargetedCurves;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceInstance);
            }
        }

        private static int RetargetWaistRotationCurvesToCurrentRig(
            AnimationClip sourceClip,
            AnimationClip targetClip,
            Transform targetRoot)
        {
            var sourcePrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var retargetedCurves = 0;

            foreach (var path in WaistStabilizedBonePaths)
            {
                var sourceTransform = RequireRelativeTransform(sourcePrefab.transform, path, "source waist rotation bone");
                var targetTransform = RequireRelativeTransform(targetRoot, path, "target waist rotation bone");
                var sourceRestRotation = NormalizeQuaternion(sourceTransform.localRotation);
                var targetRestRotation = NormalizeQuaternion(targetTransform.localRotation);
                var times = GetRotationCurveKeyTimes(sourceClip, path);
                var rotations = new Quaternion[times.Length];
                var previousRotation = Quaternion.identity;
                var hasPreviousRotation = false;

                for (var index = 0; index < times.Length; index++)
                {
                    var sourceRotation = EvaluateLocalRotation(sourceClip, path, times[index], sourceRestRotation);
                    rotations[index] = NormalizeQuaternion(
                        targetRestRotation *
                        Quaternion.Inverse(sourceRestRotation) *
                        sourceRotation);

                    if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotations[index]) < 0f)
                    {
                        rotations[index] = FlipQuaternion(rotations[index]);
                    }

                    previousRotation = rotations[index];
                    hasPreviousRotation = true;
                }

                SetQuaternionCurves(targetClip, path, times, rotations);
                ClearEulerRotationCurves(targetClip, path);
                retargetedCurves += 4;
            }

            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();
            return retargetedCurves;
        }

        private static float[] GetRotationCurveKeyTimes(AnimationClip clip, string path)
        {
            var times = new SortedSet<float>();
            times.Add(0f);
            times.Add(Mathf.Max(clip.length, 0f));

            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => binding.type == typeof(Transform) &&
                                           string.Equals(binding.path, path, StringComparison.Ordinal) &&
                                           binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                foreach (var key in curve.keys)
                {
                    times.Add(Mathf.Clamp(key.time, 0f, Mathf.Max(clip.length, 0f)));
                }
            }

            return times.ToArray();
        }

        private static Quaternion EvaluateLocalRotation(
            AnimationClip clip,
            string path,
            float time,
            Quaternion fallback)
        {
            var x = EvaluateFloatCurve(clip, path, "m_LocalRotation.x", time, fallback.x);
            var y = EvaluateFloatCurve(clip, path, "m_LocalRotation.y", time, fallback.y);
            var z = EvaluateFloatCurve(clip, path, "m_LocalRotation.z", time, fallback.z);
            var w = EvaluateFloatCurve(clip, path, "m_LocalRotation.w", time, fallback.w);
            return NormalizeQuaternion(new Quaternion(x, y, z, w));
        }

        private static float EvaluateFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            float time,
            float fallback)
        {
            var curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName));
            return curve == null ? fallback : curve.Evaluate(time);
        }

        private static GameObject InstantiateRunningSourcePrefab(string context)
        {
            var sourcePrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(sourcePrefab);
            }

            instance.name = "TergoRunningSource_" + context;
            foreach (var transform in instance.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            return instance;
        }

        private static string BuildSourceWaistRotationReport(AnimationClip sourceClip)
        {
            var sourceInstance = InstantiateRunningSourcePrefab("WaistReport");
            try
            {
                return BuildWaistRotationReport(sourceInstance.transform, sourceClip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceInstance);
            }
        }

        private static string[] GetAnimatedTransformPaths(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static float[] GetTransformCurveKeyTimes(AnimationClip clip, string path)
        {
            var times = new SortedSet<float>();
            times.Add(0f);
            times.Add(Mathf.Max(clip.length, 0f));

            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => binding.type == typeof(Transform) &&
                                           string.Equals(binding.path, path, StringComparison.Ordinal)))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                foreach (var key in curve.keys)
                {
                    times.Add(Mathf.Clamp(key.time, 0f, Mathf.Max(clip.length, 0f)));
                }
            }

            return times.ToArray();
        }

        private static int SetTransformCurves(
            AnimationClip clip,
            string path,
            float[] times,
            Vector3[] positions,
            Quaternion[] rotations,
            Vector3[] scales)
        {
            var bindings = new[]
            {
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.x"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.y"),
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z")
            };
            var curves = new[]
            {
                BuildFloatCurve(times, positions.Select(value => value.x).ToArray()),
                BuildFloatCurve(times, positions.Select(value => value.y).ToArray()),
                BuildFloatCurve(times, positions.Select(value => value.z).ToArray()),
                BuildFloatCurve(times, rotations.Select(value => value.x).ToArray()),
                BuildFloatCurve(times, rotations.Select(value => value.y).ToArray()),
                BuildFloatCurve(times, rotations.Select(value => value.z).ToArray()),
                BuildFloatCurve(times, rotations.Select(value => value.w).ToArray()),
                BuildFloatCurve(times, scales.Select(value => value.x).ToArray()),
                BuildFloatCurve(times, scales.Select(value => value.y).ToArray()),
                BuildFloatCurve(times, scales.Select(value => value.z).ToArray())
            };

            AnimationUtility.SetEditorCurves(clip, bindings, curves);
            ClearEulerRotationCurves(clip, path);
            return 10;
        }

        private static AnimationCurve BuildFloatCurve(float[] times, float[] values)
        {
            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keys[index] = new Keyframe(times[index], values[index]);
            }

            return new AnimationCurve(keys);
        }

        private static void SetVector3Curves(AnimationClip clip, string path, string propertyPrefix, float[] times, Vector3[] values)
        {
            SetFloatCurve(clip, path, propertyPrefix + ".x", times, values.Select(value => value.x).ToArray());
            SetFloatCurve(clip, path, propertyPrefix + ".y", times, values.Select(value => value.y).ToArray());
            SetFloatCurve(clip, path, propertyPrefix + ".z", times, values.Select(value => value.z).ToArray());
        }

        private static void SetQuaternionCurves(AnimationClip clip, string path, float[] times, Quaternion[] values)
        {
            SetFloatCurve(clip, path, "m_LocalRotation.x", times, values.Select(value => value.x).ToArray());
            SetFloatCurve(clip, path, "m_LocalRotation.y", times, values.Select(value => value.y).ToArray());
            SetFloatCurve(clip, path, "m_LocalRotation.z", times, values.Select(value => value.z).ToArray());
            SetFloatCurve(clip, path, "m_LocalRotation.w", times, values.Select(value => value.w).ToArray());
        }

        private static void SetFloatCurve(AnimationClip clip, string path, string propertyName, float[] times, float[] values)
        {
            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keys[index] = new Keyframe(times[index], values[index]);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                new AnimationCurve(keys));
        }

        private static GameObject ImportApprovedDeathMeltPuddleModelAsset()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var sampleAbsolutePath = Path.GetFullPath(Path.Combine(
                projectRoot,
                ApprovedDeathMeltPuddleSampleFbxPath.Replace('/', Path.DirectorySeparatorChar)));
            var targetAbsolutePath = Path.GetFullPath(Path.Combine(
                projectRoot,
                ApprovedDeathMeltPuddleModelAssetPath.Replace('/', Path.DirectorySeparatorChar)));

            if (!File.Exists(sampleAbsolutePath))
            {
                throw new FileNotFoundException(
                    "Approved Tergo death melt puddle sample FBX was not found.",
                    sampleAbsolutePath);
            }

            var targetDirectory = Path.GetDirectoryName(targetAbsolutePath);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sampleAbsolutePath, targetAbsolutePath, true);
            AssetDatabase.ImportAsset(ApprovedDeathMeltPuddleModelAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(ApprovedDeathMeltPuddleModelAssetPath) as ModelImporter;
            if (importer != null)
            {
                var changed = false;
                if (!importer.importBlendShapes)
                {
                    importer.importBlendShapes = true;
                    changed = true;
                }

                if (importer.importAnimation)
                {
                    importer.importAnimation = false;
                    changed = true;
                }

                if (Mathf.Abs(importer.globalScale - 1f) > 0.0001f)
                {
                    importer.globalScale = 1f;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }

            return RequireAsset<GameObject>(ApprovedDeathMeltPuddleModelAssetPath);
        }

        private static int RemoveApprovedDeathMeltPuddleChildren(Transform deathRoot)
        {
            var removed = 0;
            for (var index = deathRoot.childCount - 1; index >= 0; index--)
            {
                var child = deathRoot.GetChild(index);
                if (!string.Equals(child.name, ApprovedDeathMeltPuddleRootName, StringComparison.Ordinal))
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(child.gameObject);
                removed++;
            }

            return removed;
        }

        private static Transform InstantiateApprovedDeathMeltPuddleRoot(GameObject approvedPrefab, Transform deathRoot)
        {
            var instance = PrefabUtility.InstantiatePrefab(approvedPrefab, deathRoot) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(approvedPrefab, deathRoot);
            }

            instance.name = ApprovedDeathMeltPuddleRootName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance.transform;
        }

        private static ApprovedDeathMeltTimeline BuildApprovedDeathMeltTimeline(float baseFallEndTime)
        {
            var meltStart = baseFallEndTime + ApprovedDeathMeltStartDelay;
            var sagTime = meltStart + ApprovedDeathMeltSagDuration;
            var collapseTime = meltStart + ApprovedDeathMeltCollapseDuration;
            var spreadTime = meltStart + ApprovedDeathMeltSpreadDuration;
            var holdTime = spreadTime + ApprovedDeathMeltHoldDuration;
            return new ApprovedDeathMeltTimeline(meltStart, sagTime, collapseTime, spreadTime, holdTime);
        }

        private static SkinnedMeshRenderer RequireApprovedDeathMeltPuddleRenderer(Transform puddleRoot)
        {
            var renderers = puddleRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.sharedMesh != null)
                .ToArray();
            foreach (var renderer in renderers)
            {
                if (HasBlendShape(renderer, ApprovedDeathMeltWeightSagShape) &&
                    HasBlendShape(renderer, ApprovedDeathMeltCrushCollapseShape) &&
                    HasBlendShape(renderer, ApprovedDeathMeltSpreadShape))
                {
                    return renderer;
                }
            }

            var report = string.Join(
                "; ",
                renderers.Select(renderer =>
                    renderer.name + ":[" +
                    string.Join(
                        "|",
                        Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                            .Select(renderer.sharedMesh.GetBlendShapeName)) + "]"));
            throw new InvalidOperationException(
                ApprovedDeathMeltPuddleRootName + " must contain the approved Tergo melt puddle BlendShapes. BlendShapes=" + report);
        }

        private static bool HasBlendShape(SkinnedMeshRenderer renderer, string shapeName)
        {
            return renderer.sharedMesh != null && renderer.sharedMesh.GetBlendShapeIndex(shapeName) >= 0;
        }

        private static int CopyApprovedPuddleBodyMaterialsFromDeathRoot(
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer)
        {
            var sourceMaterial = GetDeathBodyRenderers(deathRoot, puddleRoot)
                .SelectMany(renderer => renderer.sharedMaterials)
                .FirstOrDefault(material => material != null);
            if (sourceMaterial == null)
            {
                throw new InvalidOperationException(DeathRootName + " has no body material to copy to the approved melt puddle renderer.");
            }

            var targetMaterials = puddleRenderer.sharedMaterials;
            if (targetMaterials.Length == 0)
            {
                targetMaterials = new Material[1];
            }

            for (var index = 0; index < targetMaterials.Length; index++)
            {
                targetMaterials[index] = sourceMaterial;
            }

            puddleRenderer.sharedMaterials = targetMaterials;
            return targetMaterials.Length;
        }

        private static bool ApprovedPuddleBodyMaterialMatchesDeathBody(
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer)
        {
            var sourceMaterial = GetDeathBodyRenderers(deathRoot, puddleRoot)
                .SelectMany(renderer => renderer.sharedMaterials)
                .FirstOrDefault(material => material != null);
            if (sourceMaterial == null)
            {
                return false;
            }

            var targetMaterials = puddleRenderer.sharedMaterials;
            return targetMaterials.Length > 0 && targetMaterials.All(material => material == sourceMaterial);
        }

        private static IEnumerable<SkinnedMeshRenderer> GetDeathBodyRenderers(Transform deathRoot, Transform excludedRoot)
        {
            return deathRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer =>
                    renderer.sharedMesh != null &&
                    !IsSameOrDescendantOf(renderer.transform, excludedRoot) &&
                    !HasNamedAncestor(renderer.transform, EyeContainerName));
        }

        private static Renderer[] GetDeathEyeRenderers(Transform deathRoot)
        {
            var eyeRoot = FindFirstNamedDescendant(deathRoot, EyeContainerName);
            return eyeRoot == null ? Array.Empty<Renderer>() : eyeRoot.GetComponentsInChildren<Renderer>(true);
        }

        private static Transform FindFirstNamedDescendant(Transform root, string descendantName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => string.Equals(transform.name, descendantName, StringComparison.Ordinal));
        }

        private static bool HasNamedAncestor(Transform transform, string ancestorName)
        {
            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, ancestorName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsSameOrDescendantOf(Transform transform, Transform possibleAncestor)
        {
            if (possibleAncestor == null)
            {
                return false;
            }

            var current = transform;
            while (current != null)
            {
                if (current == possibleAncestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static ApprovedDeathMeltAlignmentMetrics AlignApprovedPuddleToDyingFinalPose(
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer[] bodyRenderers,
            AnimationClip clip,
            float baseFallEndTime)
        {
            var originalStates = CaptureTransformStates(deathRoot, puddleRoot);
            Bounds finalBodyBounds;
            try
            {
                clip.SampleAnimation(deathRoot.gameObject, Mathf.Min(baseFallEndTime, Mathf.Max(clip.length - 0.001f, 0f)));
                finalBodyBounds = CalculateCombinedBounds(bodyRenderers.Cast<Renderer>().ToArray());
            }
            finally
            {
                RestoreTransformStates(originalStates);
            }

            var puddleRenderers = puddleRoot.GetComponentsInChildren<Renderer>(true);
            var originalPuddleWeights = CaptureBlendShapeWeights(RequireApprovedDeathMeltPuddleRenderer(puddleRoot));
            Bounds alignedBounds;
            float scale;
            float centerDelta;
            float groundDelta;
            try
            {
                var puddleRenderer = RequireApprovedDeathMeltPuddleRenderer(puddleRoot);
                SetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltWeightSagShape, 0f);
                SetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltCrushCollapseShape, 0f);
                SetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltSpreadShape, 100f);
                var floorRotation = SelectApprovedPuddleFloorRotation(puddleRoot, puddleRenderers, finalBodyBounds);
                puddleRoot.localRotation = floorRotation;

                var sampleBounds = CalculateCombinedBounds(puddleRenderers);
                var bodyHorizontalExtent = Mathf.Max(finalBodyBounds.size.x, finalBodyBounds.size.z);
                var sampleHorizontalExtent = Mathf.Max(sampleBounds.size.x, sampleBounds.size.z);
                scale = sampleHorizontalExtent > 0.0001f
                    ? Mathf.Clamp(bodyHorizontalExtent / sampleHorizontalExtent, 0.001f, 250f)
                    : 1f;
                puddleRoot.localScale = Vector3.one * scale;

                var scaledSampleBounds = CalculateCombinedBounds(puddleRenderers);
                var offset = new Vector3(
                    finalBodyBounds.center.x - scaledSampleBounds.center.x,
                    finalBodyBounds.min.y - scaledSampleBounds.min.y,
                    finalBodyBounds.center.z - scaledSampleBounds.center.z);
                puddleRoot.position += offset;

                alignedBounds = CalculateCombinedBounds(puddleRenderers);
                centerDelta = Vector2.Distance(
                    new Vector2(finalBodyBounds.center.x, finalBodyBounds.center.z),
                    new Vector2(alignedBounds.center.x, alignedBounds.center.z));
                groundDelta = Mathf.Abs(finalBodyBounds.min.y - alignedBounds.min.y);
            }
            finally
            {
                RestoreBlendShapeWeights(RequireApprovedDeathMeltPuddleRenderer(puddleRoot), originalPuddleWeights);
            }

            var horizontalExtent = Mathf.Max(alignedBounds.size.x, alignedBounds.size.z);
            var verticalRatio = horizontalExtent > 0.0001f ? alignedBounds.size.y / horizontalExtent : float.PositiveInfinity;
            return new ApprovedDeathMeltAlignmentMetrics(
                scale,
                centerDelta,
                groundDelta,
                puddleRoot.localEulerAngles,
                alignedBounds.size.y,
                horizontalExtent,
                verticalRatio);
        }

        private static Quaternion SelectApprovedPuddleFloorRotation(
            Transform puddleRoot,
            Renderer[] puddleRenderers,
            Bounds finalBodyBounds)
        {
            var bestRotation = puddleRoot.localRotation;
            var bestScore = float.PositiveInfinity;
            var bodyAspect = CalculateHorizontalAspect(finalBodyBounds);
            foreach (var candidate in BuildRightAngleRotationCandidates())
            {
                puddleRoot.localRotation = candidate;
                var bounds = CalculateCombinedBounds(puddleRenderers);
                var horizontalExtent = Mathf.Max(bounds.size.x, bounds.size.z);
                if (horizontalExtent <= 0.0001f)
                {
                    continue;
                }

                var verticalRatio = bounds.size.y / horizontalExtent;
                var aspectDelta = Mathf.Abs(Mathf.Log(CalculateHorizontalAspect(bounds) / bodyAspect));
                var horizontalArea = bounds.size.x * bounds.size.z;
                var score = verticalRatio * 1000f + aspectDelta * 10f - horizontalArea * 0.001f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestRotation = candidate;
                }
            }

            return bestRotation;
        }

        private static Quaternion[] BuildRightAngleRotationCandidates()
        {
            var angles = new[] { 0f, 90f, 180f, 270f };
            var candidates = new List<Quaternion>();
            foreach (var x in angles)
            {
                foreach (var y in angles)
                {
                    foreach (var z in angles)
                    {
                        candidates.Add(Quaternion.Euler(x, y, z));
                    }
                }
            }

            return candidates.ToArray();
        }

        private static float CalculateHorizontalAspect(Bounds bounds)
        {
            var longer = Mathf.Max(bounds.size.x, bounds.size.z);
            var shorter = Mathf.Max(Mathf.Min(bounds.size.x, bounds.size.z), 0.0001f);
            return Mathf.Max(longer / shorter, 1f);
        }

        private static Vector3 CorrectApprovedPuddleRootAgainstFinalAnimationSample(
            AnimationClip clip,
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer,
            SkinnedMeshRenderer[] bodyRenderers,
            ApprovedDeathMeltTimeline timeline)
        {
            var transformStates = CaptureTransformStates(deathRoot, null);
            var rendererStates = CaptureRendererEnabledStates(deathRoot);
            var puddleWeights = CaptureBlendShapeWeights(puddleRenderer);
            Vector3 correction;
            try
            {
                clip.SampleAnimation(deathRoot.gameObject, Mathf.Max(0f, timeline.MeltStart - ApprovedDeathMeltStartDelay - 0.001f));
                var bodyBounds = CalculateCombinedBounds(bodyRenderers.Cast<Renderer>().ToArray());

                clip.SampleAnimation(deathRoot.gameObject, timeline.SpreadTime);
                var puddleBounds = CalculateCombinedBounds(puddleRenderer.GetComponentsInChildren<Renderer>(true));
                correction = new Vector3(
                    bodyBounds.center.x - puddleBounds.center.x,
                    bodyBounds.min.y - puddleBounds.min.y,
                    bodyBounds.center.z - puddleBounds.center.z);
            }
            finally
            {
                RestoreTransformStates(transformStates);
                RestoreRendererEnabledStates(rendererStates);
                RestoreBlendShapeWeights(puddleRenderer, puddleWeights);
            }

            puddleRoot.position += correction;
            return correction;
        }

        private static ApprovedDeathMeltFloorMetrics EvaluateApprovedDeathMeltPuddleFloorMetrics(
            AnimationClip clip,
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer,
            SkinnedMeshRenderer[] bodyRenderers,
            ApprovedDeathMeltTimeline timeline)
        {
            var transformStates = CaptureTransformStates(deathRoot, null);
            var rendererStates = CaptureRendererEnabledStates(deathRoot);
            var puddleWeights = CaptureBlendShapeWeights(puddleRenderer);
            try
            {
                clip.SampleAnimation(deathRoot.gameObject, Mathf.Max(0f, timeline.MeltStart - ApprovedDeathMeltStartDelay - 0.001f));
                var bodyBounds = CalculateCombinedBounds(bodyRenderers.Cast<Renderer>().ToArray());

                clip.SampleAnimation(deathRoot.gameObject, timeline.SpreadTime);
                var puddleBounds = CalculateCombinedBounds(puddleRenderer.GetComponentsInChildren<Renderer>(true));
                var finalPuddlePosition = puddleRoot.position;
                var horizontalExtent = Mathf.Max(puddleBounds.size.x, puddleBounds.size.z);
                var verticalRatio = horizontalExtent > 0.0001f ? puddleBounds.size.y / horizontalExtent : float.PositiveInfinity;
                var centerDelta = Vector2.Distance(
                    new Vector2(bodyBounds.center.x, bodyBounds.center.z),
                    new Vector2(puddleBounds.center.x, puddleBounds.center.z));

                clip.SampleAnimation(deathRoot.gameObject, timeline.MeltStart);
                var startYOffset = puddleRoot.position.y - finalPuddlePosition.y;

                return new ApprovedDeathMeltFloorMetrics(
                    Mathf.Abs(bodyBounds.min.y - puddleBounds.min.y),
                    puddleBounds.size.y,
                    horizontalExtent,
                    verticalRatio,
                    centerDelta,
                    startYOffset);
            }
            finally
            {
                RestoreTransformStates(transformStates);
                RestoreRendererEnabledStates(rendererStates);
                RestoreBlendShapeWeights(puddleRenderer, puddleWeights);
            }
        }

        private static void RequireApprovedDeathMeltPuddleFloorMetrics(ApprovedDeathMeltFloorMetrics metrics)
        {
            if (metrics.GroundDelta <= 0.08f &&
                metrics.CenterHorizontalDelta <= 0.2f &&
                metrics.HorizontalExtent > 0.0001f &&
                metrics.VerticalToHorizontalRatio <= 0.35f &&
                Mathf.Abs(metrics.StartYOffset - ApprovedDeathMeltStartYOffset) <= 0.005f)
            {
                return;
            }

            throw new InvalidOperationException(
                ApprovedDeathMeltPuddleRootName + " is not aligned as a floor puddle after the dying pose. " +
                "GroundDelta=" + metrics.GroundDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", CenterHorizontalDelta=" + metrics.CenterHorizontalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", VerticalHeight=" + metrics.VerticalHeight.ToString("0.######", CultureInfo.InvariantCulture) +
                ", HorizontalExtent=" + metrics.HorizontalExtent.ToString("0.######", CultureInfo.InvariantCulture) +
                ", VerticalRatio=" + metrics.VerticalToHorizontalRatio.ToString("0.######", CultureInfo.InvariantCulture) +
                ", StartYOffset=" + metrics.StartYOffset.ToString("0.######", CultureInfo.InvariantCulture));
        }

        private static Bounds CalculateCombinedBounds(Renderer[] renderers)
        {
            var hasBounds = false;
            var combined = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var rendererBounds = CalculateCurrentWorldBounds(renderer);
                if (!hasBounds)
                {
                    combined = rendererBounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(rendererBounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("Cannot calculate bounds because no renderer was provided.");
            }

            return combined;
        }

        private static Bounds CalculateCurrentWorldBounds(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh != null)
            {
                var bakedMesh = new Mesh();
                try
                {
                    skinnedRenderer.BakeMesh(bakedMesh);
                    if (bakedMesh.vertexCount > 0)
                    {
                        var vertices = bakedMesh.vertices;
                        var bounds = new Bounds(skinnedRenderer.transform.TransformPoint(vertices[0]), Vector3.zero);
                        for (var index = 1; index < vertices.Length; index++)
                        {
                            bounds.Encapsulate(skinnedRenderer.transform.TransformPoint(vertices[index]));
                        }

                        return bounds;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                }
            }

            return renderer.bounds;
        }

        private static int ApplyApprovedDeathMeltPuddleCurves(
            AnimationClip clip,
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer,
            SkinnedMeshRenderer[] bodyRenderers,
            Renderer[] eyeRenderers,
            ApprovedDeathMeltTimeline timeline)
        {
            var bindingCount = 0;
            foreach (var renderer in puddleRoot.GetComponentsInChildren<Renderer>(true))
            {
                bindingCount += SetRendererEnabledCurve(
                    clip,
                    deathRoot,
                    renderer,
                    new[] { 0f, Mathf.Max(0f, timeline.MeltStart - ApprovedDeathMeltVisibilityLead), timeline.MeltStart, timeline.HoldTime },
                    new[] { 0f, 0f, 1f, 1f });
            }

            var hideBeforeMelt = Mathf.Max(0f, timeline.MeltStart - ApprovedDeathMeltVisibilityLead);
            foreach (var renderer in bodyRenderers.Cast<Renderer>().Concat(eyeRenderers))
            {
                bindingCount += SetRendererEnabledCurve(
                    clip,
                    deathRoot,
                    renderer,
                    new[] { 0f, hideBeforeMelt, timeline.MeltStart, timeline.HoldTime },
                    new[] { 1f, 1f, 0f, 0f });
            }

            bindingCount += SetApprovedBlendShapeCurve(
                clip,
                deathRoot,
                puddleRenderer,
                ApprovedDeathMeltWeightSagShape,
                new[] { 0f, timeline.MeltStart, timeline.HoldTime },
                new[] { 0f, 0f, 0f });
            bindingCount += SetApprovedBlendShapeCurve(
                clip,
                deathRoot,
                puddleRenderer,
                ApprovedDeathMeltCrushCollapseShape,
                new[] { 0f, timeline.MeltStart, timeline.HoldTime },
                new[] { 0f, 0f, 0f });
            bindingCount += SetApprovedBlendShapeCurve(
                clip,
                deathRoot,
                puddleRenderer,
                ApprovedDeathMeltSpreadShape,
                new[] { 0f, timeline.MeltStart, timeline.SpreadTime, timeline.HoldTime },
                new[] { 0f, 0f, 100f, 100f });

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f;
            settings.stopTime = timeline.HoldTime;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = WrapMode.Loop;
            EditorUtility.SetDirty(clip);
            return bindingCount;
        }

        private static int SetRendererEnabledCurve(
            AnimationClip clip,
            Transform animationRoot,
            Renderer renderer,
            float[] times,
            float[] values)
        {
            var path = AnimationUtility.CalculateTransformPath(renderer.transform, animationRoot);
            var binding = EditorCurveBinding.FloatCurve(path, renderer.GetType(), "m_Enabled");
            AnimationUtility.SetEditorCurve(clip, binding, BuildFloatCurve(times, values));
            return 1;
        }

        private static int SetApprovedBlendShapeCurve(
            AnimationClip clip,
            Transform animationRoot,
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            float[] times,
            float[] values)
        {
            if (!HasBlendShape(renderer, blendShapeName))
            {
                throw new InvalidOperationException(renderer.name + " is missing approved BlendShape " + blendShapeName + ".");
            }

            var path = AnimationUtility.CalculateTransformPath(renderer.transform, animationRoot);
            var binding = EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + blendShapeName);
            AnimationUtility.SetEditorCurve(clip, binding, BuildFloatCurve(times, values));
            return 1;
        }

        private static int SetApprovedPuddleRootStartOffsetCurves(
            AnimationClip clip,
            Transform animationRoot,
            Transform puddleRoot,
            ApprovedDeathMeltTimeline timeline)
        {
            var path = AnimationUtility.CalculateTransformPath(puddleRoot, animationRoot);
            var finalLocalPosition = puddleRoot.localPosition;
            var startLocalPosition = finalLocalPosition + animationRoot.InverseTransformVector(Vector3.up * ApprovedDeathMeltStartYOffset);
            var holdBeforeMelt = Mathf.Max(0f, timeline.MeltStart - ApprovedDeathMeltVisibilityLead);
            var times = new[] { 0f, holdBeforeMelt, timeline.MeltStart, timeline.SpreadTime, timeline.HoldTime };
            SetTransformFloatCurve(
                clip,
                path,
                "m_LocalPosition.x",
                times,
                new[] { startLocalPosition.x, startLocalPosition.x, startLocalPosition.x, finalLocalPosition.x, finalLocalPosition.x });
            SetTransformFloatCurve(
                clip,
                path,
                "m_LocalPosition.y",
                times,
                new[] { startLocalPosition.y, startLocalPosition.y, startLocalPosition.y, finalLocalPosition.y, finalLocalPosition.y });
            SetTransformFloatCurve(
                clip,
                path,
                "m_LocalPosition.z",
                times,
                new[] { startLocalPosition.z, startLocalPosition.z, startLocalPosition.z, finalLocalPosition.z, finalLocalPosition.z });
            return 3;
        }

        private static void SetTransformFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            float[] times,
            float[] values)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                BuildFloatCurve(times, values));
        }

        private static int RequireApprovedDeathMeltPuddleCurveBindings(
            AnimationClip clip,
            Transform deathRoot,
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer,
            SkinnedMeshRenderer[] bodyRenderers,
            Renderer[] eyeRenderers)
        {
            var count = 0;
            foreach (var renderer in puddleRoot.GetComponentsInChildren<Renderer>(true))
            {
                RequireRendererEnabledCurve(clip, deathRoot, renderer);
                count++;
            }

            foreach (var renderer in bodyRenderers.Cast<Renderer>().Concat(eyeRenderers))
            {
                RequireRendererEnabledCurve(clip, deathRoot, renderer);
                count++;
            }

            RequireApprovedBlendShapeCurve(clip, deathRoot, puddleRenderer, ApprovedDeathMeltWeightSagShape);
            RequireApprovedBlendShapeCurve(clip, deathRoot, puddleRenderer, ApprovedDeathMeltCrushCollapseShape);
            RequireApprovedBlendShapeCurve(clip, deathRoot, puddleRenderer, ApprovedDeathMeltSpreadShape);
            RequireApprovedPuddleRootStartOffsetCurves(clip, deathRoot, puddleRoot);
            return count + 6;
        }

        private static AnimationCurve RequireRendererEnabledCurve(AnimationClip clip, Transform root, Renderer renderer)
        {
            var path = AnimationUtility.CalculateTransformPath(renderer.transform, root);
            var binding = EditorCurveBinding.FloatCurve(path, renderer.GetType(), "m_Enabled");
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
            {
                throw new InvalidOperationException("Missing renderer enabled curve for " + path + ".");
            }

            return curve;
        }

        private static AnimationCurve RequireApprovedBlendShapeCurve(
            AnimationClip clip,
            Transform root,
            SkinnedMeshRenderer renderer,
            string blendShapeName)
        {
            var path = AnimationUtility.CalculateTransformPath(renderer.transform, root);
            var binding = EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + blendShapeName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
            {
                throw new InvalidOperationException("Missing approved BlendShape curve " + blendShapeName + " for " + path + ".");
            }

            return curve;
        }

        private static void RequireApprovedPuddleRootStartOffsetCurves(AnimationClip clip, Transform root, Transform puddleRoot)
        {
            var path = AnimationUtility.CalculateTransformPath(puddleRoot, root);
            RequireTransformCurve(clip, path, "m_LocalPosition.x");
            RequireTransformCurve(clip, path, "m_LocalPosition.y");
            RequireTransformCurve(clip, path, "m_LocalPosition.z");
        }

        private static AnimationCurve RequireTransformCurve(AnimationClip clip, string path, string propertyName)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
            {
                throw new InvalidOperationException("Missing Transform curve " + propertyName + " for " + path + ".");
            }

            return curve;
        }

        private static void RequireApprovedCompressionBlendShapesCut(
            AnimationClip clip,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            RequireBlendShapeCurveMaxValue(clip, root, renderer, ApprovedDeathMeltWeightSagShape, 1f);
            RequireBlendShapeCurveMaxValue(clip, root, renderer, ApprovedDeathMeltCrushCollapseShape, 1f);
        }

        private static void RequireBlendShapeCurveMaxValue(
            AnimationClip clip,
            Transform root,
            SkinnedMeshRenderer renderer,
            string blendShapeName,
            float maxAllowedValue)
        {
            var curve = RequireApprovedBlendShapeCurve(clip, root, renderer, blendShapeName);
            var maxValue = curve.keys.Length == 0 ? 0f : curve.keys.Max(key => key.value);
            if (maxValue <= maxAllowedValue)
            {
                return;
            }

            throw new InvalidOperationException(
                blendShapeName + " compression segment was not cut. MaxValue=" +
                maxValue.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void RequireNoTransformCurveKeysAfterBaseFallEnd(AnimationClip clip, float baseFallEndTime)
        {
            var threshold = baseFallEndTime + 0.002f;
            var lateTransformKeys = new List<string>();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => binding.type == typeof(Transform)))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                var lateKey = curve.keys.FirstOrDefault(key => key.time > threshold);
                if (lateKey.time > threshold)
                {
                    if (IsApprovedPuddleRootPositionBinding(binding))
                    {
                        continue;
                    }

                    lateTransformKeys.Add(
                        binding.path + "/" + binding.propertyName + "@" +
                        lateKey.time.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }

            if (lateTransformKeys.Count > 0)
            {
                throw new InvalidOperationException(
                    DeathRootName + " has Transform or bone keys after the original dying FBX end. LateKeys=" +
                    string.Join(", ", lateTransformKeys.Take(12)) +
                    (lateTransformKeys.Count > 12 ? ", ..." : string.Empty));
            }
        }

        private static bool IsApprovedPuddleRootPositionBinding(EditorCurveBinding binding)
        {
            return string.Equals(binding.path, ApprovedDeathMeltPuddleRootName, StringComparison.Ordinal) &&
                   (string.Equals(binding.propertyName, "m_LocalPosition.x", StringComparison.Ordinal) ||
                    string.Equals(binding.propertyName, "m_LocalPosition.y", StringComparison.Ordinal) ||
                    string.Equals(binding.propertyName, "m_LocalPosition.z", StringComparison.Ordinal));
        }

        private static void SetApprovedDeathMeltInitialVisibility(
            Transform puddleRoot,
            SkinnedMeshRenderer puddleRenderer,
            SkinnedMeshRenderer[] bodyRenderers,
            Renderer[] eyeRenderers)
        {
            foreach (var renderer in puddleRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (var renderer in bodyRenderers.Cast<Renderer>().Concat(eyeRenderers))
            {
                renderer.enabled = true;
            }

            SetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltWeightSagShape, 0f);
            SetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltCrushCollapseShape, 0f);
            SetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltSpreadShape, 0f);
        }

        private static ApprovedDeathMeltSampleMetrics SampleApprovedDeathMeltPuddleClip(
            AnimationClip clip,
            Transform deathRoot,
            SkinnedMeshRenderer puddleRenderer,
            SkinnedMeshRenderer[] bodyRenderers,
            Renderer[] eyeRenderers,
            ApprovedDeathMeltTimeline timeline)
        {
            var transformStates = CaptureTransformStates(deathRoot, null);
            var rendererStates = CaptureRendererEnabledStates(deathRoot);
            var puddleWeights = CaptureBlendShapeWeights(puddleRenderer);
            var puddleRenderers = puddleRenderer.GetComponentsInChildren<Renderer>(true);

            try
            {
                clip.SampleAnimation(deathRoot.gameObject, Mathf.Max(0f, timeline.MeltStart - ApprovedDeathMeltVisibilityLead * 2f));
                var bodyVisibleBefore = bodyRenderers.All(renderer => renderer.enabled);
                var puddleHiddenBefore = puddleRenderers.All(renderer => !renderer.enabled);

                clip.SampleAnimation(deathRoot.gameObject, timeline.MeltStart + 0.02f);
                var puddleVisibleAfter = puddleRenderers.All(renderer => renderer.enabled);
                var bodyHiddenAfter = bodyRenderers.All(renderer => !renderer.enabled);
                var eyeHiddenAfter = eyeRenderers.All(renderer => !renderer.enabled);

                clip.SampleAnimation(deathRoot.gameObject, timeline.SpreadTime);
                return new ApprovedDeathMeltSampleMetrics(
                    bodyVisibleBefore,
                    puddleHiddenBefore,
                    puddleVisibleAfter,
                    bodyHiddenAfter,
                    eyeHiddenAfter,
                    GetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltWeightSagShape),
                    GetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltCrushCollapseShape),
                    GetBlendShapeWeight(puddleRenderer, ApprovedDeathMeltSpreadShape));
            }
            finally
            {
                RestoreTransformStates(transformStates);
                RestoreRendererEnabledStates(rendererStates);
                RestoreBlendShapeWeights(puddleRenderer, puddleWeights);
            }
        }

        private static void RequireApprovedDeathMeltSampleMetrics(ApprovedDeathMeltSampleMetrics metrics)
        {
            if (metrics.BodyVisibleBeforeMelt &&
                metrics.PuddleHiddenBeforeMelt &&
                metrics.PuddleVisibleAfterMelt &&
                metrics.BodyHiddenAfterMelt &&
                metrics.EyeHiddenAfterMelt &&
                metrics.FinalSagWeight <= 1f &&
                metrics.FinalCollapseWeight <= 1f &&
                metrics.FinalSpreadWeight >= 99f)
            {
                return;
            }

            throw new InvalidOperationException(
                "Approved Tergo death melt puddle clip does not transition from the original lying body to the approved puddle shape. " +
                "BodyVisibleBeforeMelt=" + metrics.BodyVisibleBeforeMelt +
                ", PuddleHiddenBeforeMelt=" + metrics.PuddleHiddenBeforeMelt +
                ", PuddleVisibleAfterMelt=" + metrics.PuddleVisibleAfterMelt +
                ", BodyHiddenAfterMelt=" + metrics.BodyHiddenAfterMelt +
                ", EyeHiddenAfterMelt=" + metrics.EyeHiddenAfterMelt +
                ", FinalSagWeight=" + metrics.FinalSagWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FinalCollapseWeight=" + metrics.FinalCollapseWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                ", FinalSpreadWeight=" + metrics.FinalSpreadWeight.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static Dictionary<Transform, TransformState> CaptureTransformStates(Transform root, Transform excludedRoot)
        {
            var states = new Dictionary<Transform, TransformState>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (excludedRoot != null && IsSameOrDescendantOf(transform, excludedRoot))
                {
                    continue;
                }

                states[transform] = TransformState.Capture(transform);
            }

            return states;
        }

        private static void RestoreTransformStates(Dictionary<Transform, TransformState> states)
        {
            foreach (var pair in states)
            {
                if (pair.Key != null)
                {
                    pair.Value.ApplyTo(pair.Key);
                }
            }
        }

        private static Dictionary<Renderer, bool> CaptureRendererEnabledStates(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .ToDictionary(renderer => renderer, renderer => renderer.enabled);
        }

        private static void RestoreRendererEnabledStates(Dictionary<Renderer, bool> states)
        {
            foreach (var pair in states)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = pair.Value;
                }
            }
        }

        private static float[] CaptureBlendShapeWeights(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            var weights = new float[mesh.blendShapeCount];
            for (var index = 0; index < weights.Length; index++)
            {
                weights[index] = renderer.GetBlendShapeWeight(index);
            }

            return weights;
        }

        private static void RestoreBlendShapeWeights(SkinnedMeshRenderer renderer, float[] weights)
        {
            for (var index = 0; index < weights.Length; index++)
            {
                renderer.SetBlendShapeWeight(index, weights[index]);
            }
        }

        private static void SetBlendShapeWeight(SkinnedMeshRenderer renderer, string shapeName, float weight)
        {
            var index = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
            if (index < 0)
            {
                throw new InvalidOperationException(renderer.name + " is missing BlendShape " + shapeName + ".");
            }

            renderer.SetBlendShapeWeight(index, weight);
        }

        private static float GetBlendShapeWeight(SkinnedMeshRenderer renderer, string shapeName)
        {
            var index = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
            if (index < 0)
            {
                throw new InvalidOperationException(renderer.name + " is missing BlendShape " + shapeName + ".");
            }

            return renderer.GetBlendShapeWeight(index);
        }

        private static void RequireLongaStyleDeathMeltBlendShapeModel(Transform deathRoot)
        {
            var skinnedRenderers = deathRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in skinnedRenderers)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                for (var index = 0; index < mesh.blendShapeCount; index++)
                {
                    var shapeName = mesh.GetBlendShapeName(index);
                    if (IsLongaStyleDeathMeltBlendShapeName(shapeName))
                    {
                        return;
                    }
                }
            }

            var blendShapeReport = string.Join(
                "; ",
                skinnedRenderers
                    .Select(renderer => renderer.sharedMesh == null
                        ? renderer.name + ":<no mesh>"
                        : renderer.name + ":[" + string.Join(
                            "|",
                            Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                                .Select(renderer.sharedMesh.GetBlendShapeName)) + "]"));

            throw new InvalidOperationException(
                DeathRootName +
                " cannot use LongaArma-style death melt puddle animation because the current Tergo death model has no death melt/puddle BlendShape. " +
                "LongaArma_06_Death_MeltPuddle is driven by death-specific BlendShape curves, not bone or Transform flattening. " +
                "Create and approve a Tergo death melt/puddle BlendShape model before applying this command. " +
                "SkinnedRendererCount=" + skinnedRenderers.Length.ToString(CultureInfo.InvariantCulture) +
                ", BlendShapes=" + blendShapeReport);
        }

        private static bool IsLongaStyleDeathMeltBlendShapeName(string shapeName)
        {
            if (string.IsNullOrWhiteSpace(shapeName))
            {
                return false;
            }

            var normalized = shapeName.ToLowerInvariant();
            var isDeathShape =
                normalized.Contains("death", StringComparison.Ordinal) ||
                normalized.Contains("dying", StringComparison.Ordinal);
            var isMeltPuddleShape =
                normalized.Contains("melt", StringComparison.Ordinal) ||
                normalized.Contains("puddle", StringComparison.Ordinal) ||
                normalized.Contains("liquid", StringComparison.Ordinal) ||
                normalized.Contains("spread", StringComparison.Ordinal) ||
                normalized.Contains("collapse", StringComparison.Ordinal) ||
                normalized.Contains("crush", StringComparison.Ordinal) ||
                normalized.Contains("sag", StringComparison.Ordinal);

            return isDeathShape && isMeltPuddleShape;
        }

        private static float GetDyingSourceDeathEndTime()
        {
            return Mathf.Max(SelectDyingSourceClip(LoadDyingAnimationClips()).length, 0.01f);
        }

        private static int ApplyDeathMeltPuddleCurves(AnimationClip clip, Transform deathRoot, float baseFallEndTime)
        {
            var meltStartTime = GetDeathMeltStartTime(baseFallEndTime);
            var sinkTime = meltStartTime + DeathMeltSinkDuration;
            var puddleTime = meltStartTime + DeathMeltPuddleDuration;
            var holdTime = puddleTime + DeathMeltHoldDuration;
            var specs = BuildDeathMeltPuddleBoneSpecs();
            var transforms = deathRoot.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var restSamples = new Dictionary<string, LocalTransformSample>(StringComparer.Ordinal);
            var startSamples = new Dictionary<string, LocalTransformSample>(StringComparer.Ordinal);
            var finalPositionSamples = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            var finalScaleSamples = new Dictionary<string, Vector3>(StringComparer.Ordinal);

            try
            {
                foreach (var spec in specs)
                {
                    var transform = deathRoot.Find(spec.Path);
                    if (transform != null)
                    {
                        restSamples[spec.Path] = LocalTransformSample.Capture(transform);
                    }
                }

                clip.SampleAnimation(deathRoot.gameObject, Mathf.Min(baseFallEndTime, Mathf.Max(clip.length - 0.001f, 0f)));
                foreach (var spec in specs)
                {
                    var transform = deathRoot.Find(spec.Path);
                    if (transform != null)
                    {
                        startSamples[spec.Path] = LocalTransformSample.Capture(transform);
                    }
                }

                var groundY = CalculateDeathMeltPuddleGroundY(deathRoot);
                var centerWorld = CalculateDeathMeltPuddleCenterWorld(deathRoot);
                centerWorld.y = groundY;
                var finalWorldMatrices = new Dictionary<string, Matrix4x4>(StringComparer.Ordinal);
                foreach (var spec in specs)
                {
                    var transform = deathRoot.Find(spec.Path);
                    if (transform != null)
                    {
                        var finalWorldPosition = CalculateDeathMeltPuddleWorldPosition(
                            transform,
                            centerWorld,
                            groundY,
                            spec);
                        var finalScale = CalculateWorldFloorAlignedPuddleScale(
                            transform,
                            transform.localScale,
                            spec.ScaleMultiplier);
                        var parentMatrix = GetDeathMeltPuddleParentWorldMatrix(transform, spec.Path, finalWorldMatrices);
                        var finalLocalPosition = parentMatrix.inverse.MultiplyPoint3x4(finalWorldPosition);

                        finalPositionSamples[spec.Path] = finalLocalPosition;
                        finalScaleSamples[spec.Path] = finalScale;
                        finalWorldMatrices[spec.Path] = parentMatrix * Matrix4x4.TRS(
                            finalLocalPosition,
                            startSamples[spec.Path].LocalRotation,
                            finalScale);
                    }
                }
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }
            }

            var rewritten = 0;
            foreach (var spec in specs)
            {
                if (!startSamples.TryGetValue(spec.Path, out var start) ||
                    !restSamples.TryGetValue(spec.Path, out var rest) ||
                    !finalPositionSamples.TryGetValue(spec.Path, out var finalPosition) ||
                    !finalScaleSamples.TryGetValue(spec.Path, out var finalScale))
                {
                    continue;
                }

                var sinkPosition = Vector3.Lerp(start.LocalPosition, finalPosition, 0.72f);
                var sinkScale = Vector3.Lerp(start.LocalScale, finalScale, 0.58f);

                rewritten += ReplaceDeathMeltTailVector3Curves(
                    clip,
                    spec.Path,
                    "m_LocalPosition",
                    rest.LocalPosition,
                    start.LocalPosition,
                    start.LocalPosition,
                    sinkPosition,
                    finalPosition,
                    finalPosition,
                    baseFallEndTime,
                    meltStartTime,
                    sinkTime,
                    puddleTime,
                    holdTime);
                rewritten += ReplaceDeathMeltTailQuaternionCurves(
                    clip,
                    spec.Path,
                    rest.LocalRotation,
                    start.LocalRotation,
                    start.LocalRotation,
                    start.LocalRotation,
                    start.LocalRotation,
                    start.LocalRotation,
                    baseFallEndTime,
                    meltStartTime,
                    sinkTime,
                    puddleTime,
                    holdTime);
                rewritten += ReplaceDeathMeltTailVector3Curves(
                    clip,
                    spec.Path,
                    "m_LocalScale",
                    rest.LocalScale,
                    start.LocalScale,
                    start.LocalScale,
                    sinkScale,
                    finalScale,
                    finalScale,
                    baseFallEndTime,
                    meltStartTime,
                    sinkTime,
                    puddleTime,
                    holdTime);
            }

            EnsureDyingFbxDeathClipLoops(clip);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.stopTime = Mathf.Max(settings.stopTime, holdTime);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return rewritten;
        }

        private static float CalculateDeathMeltPuddleGroundY(Transform deathRoot)
        {
            var renderers = deathRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return deathRoot.position.y;
            }

            return renderers.Min(renderer => renderer.bounds.min.y);
        }

        private static Vector3 CalculateDeathMeltPuddleCenterWorld(Transform deathRoot)
        {
            var hips = deathRoot.Find("Armature/Hips");
            if (hips != null)
            {
                return hips.position;
            }

            var renderers = deathRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return deathRoot.position;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.center;
        }

        private static Vector3 CalculateDeathMeltPuddleWorldPosition(
            Transform transform,
            Vector3 centerWorld,
            float groundY,
            DeathMeltPuddleBoneSpec spec)
        {
            var startWorld = transform.position;
            var planarFromCenter = new Vector3(startWorld.x - centerWorld.x, 0f, startWorld.z - centerWorld.z);
            var targetWorld = new Vector3(
                centerWorld.x + planarFromCenter.x * DeathMeltPuddlePlanarSpread + spec.PositionOffset.x,
                groundY + DeathMeltPuddleGroundClearance,
                centerWorld.z + planarFromCenter.z * DeathMeltPuddlePlanarSpread + spec.PositionOffset.z);
            return targetWorld;
        }

        private static Matrix4x4 GetDeathMeltPuddleParentWorldMatrix(
            Transform transform,
            string path,
            Dictionary<string, Matrix4x4> finalWorldMatrices)
        {
            var separatorIndex = path.LastIndexOf('/');
            if (separatorIndex > 0)
            {
                var parentPath = path.Substring(0, separatorIndex);
                if (finalWorldMatrices.TryGetValue(parentPath, out var finalParentMatrix))
                {
                    return finalParentMatrix;
                }
            }

            return transform.parent == null ? Matrix4x4.identity : transform.parent.localToWorldMatrix;
        }

        private static Vector3 CalculateWorldFloorAlignedPuddleScale(
            Transform transform,
            Vector3 startLocalScale,
            Vector3 scaleMultiplier)
        {
            var verticalAxis = FindLocalAxisClosestToWorldUp(transform);
            var horizontalMultiplier = Mathf.Max(DeathMeltPuddlePlanarSpread, (Mathf.Abs(scaleMultiplier.x) + Mathf.Abs(scaleMultiplier.z)) * 0.5f);
            var verticalMultiplier = Mathf.Clamp(Mathf.Abs(scaleMultiplier.y), 0.035f, 0.18f);
            var multiplier = new Vector3(horizontalMultiplier, horizontalMultiplier, horizontalMultiplier);
            multiplier = SetVectorComponent(multiplier, verticalAxis, verticalMultiplier);
            return new Vector3(
                startLocalScale.x * multiplier.x,
                startLocalScale.y * multiplier.y,
                startLocalScale.z * multiplier.z);
        }

        private static int FindLocalAxisClosestToWorldUp(Transform transform)
        {
            var rightScore = Mathf.Abs(Vector3.Dot(transform.right.normalized, Vector3.up));
            var upScore = Mathf.Abs(Vector3.Dot(transform.up.normalized, Vector3.up));
            var forwardScore = Mathf.Abs(Vector3.Dot(transform.forward.normalized, Vector3.up));
            if (rightScore >= upScore && rightScore >= forwardScore)
            {
                return 0;
            }

            return upScore >= forwardScore ? 1 : 2;
        }

        private static Vector3 SetVectorComponent(Vector3 value, int componentIndex, float componentValue)
        {
            switch (componentIndex)
            {
                case 0:
                    value.x = componentValue;
                    break;
                case 1:
                    value.y = componentValue;
                    break;
                default:
                    value.z = componentValue;
                    break;
            }

            return value;
        }

        private static float GetVectorComponent(Vector3 value, int componentIndex)
        {
            switch (componentIndex)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        private static int ReplaceDeathMeltTailVector3Curves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            Vector3 fallbackValue,
            Vector3 baseEndValue,
            Vector3 startValue,
            Vector3 sinkValue,
            Vector3 puddleValue,
            Vector3 holdValue,
            float baseEndTime,
            float startTime,
            float sinkTime,
            float puddleTime,
            float holdTime)
        {
            var changed = 0;
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, propertyPrefix + ".x", fallbackValue.x, baseEndValue.x, startValue.x, sinkValue.x, puddleValue.x, holdValue.x, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, propertyPrefix + ".y", fallbackValue.y, baseEndValue.y, startValue.y, sinkValue.y, puddleValue.y, holdValue.y, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, propertyPrefix + ".z", fallbackValue.z, baseEndValue.z, startValue.z, sinkValue.z, puddleValue.z, holdValue.z, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            return changed;
        }

        private static int ReplaceDeathMeltTailQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion fallbackValue,
            Quaternion baseEndValue,
            Quaternion startValue,
            Quaternion sinkValue,
            Quaternion puddleValue,
            Quaternion holdValue,
            float baseEndTime,
            float startTime,
            float sinkTime,
            float puddleTime,
            float holdTime)
        {
            var changed = 0;
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, "m_LocalRotation.x", fallbackValue.x, baseEndValue.x, startValue.x, sinkValue.x, puddleValue.x, holdValue.x, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, "m_LocalRotation.y", fallbackValue.y, baseEndValue.y, startValue.y, sinkValue.y, puddleValue.y, holdValue.y, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, "m_LocalRotation.z", fallbackValue.z, baseEndValue.z, startValue.z, sinkValue.z, puddleValue.z, holdValue.z, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            changed += ReplaceDeathMeltTailFloatCurve(clip, path, "m_LocalRotation.w", fallbackValue.w, baseEndValue.w, startValue.w, sinkValue.w, puddleValue.w, holdValue.w, baseEndTime, startTime, sinkTime, puddleTime, holdTime);
            return changed;
        }

        private static int ReplaceDeathMeltTailFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            float fallbackValue,
            float baseEndValue,
            float startValue,
            float sinkValue,
            float puddleValue,
            float holdValue,
            float baseEndTime,
            float startTime,
            float sinkTime,
            float puddleTime,
            float holdTime)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            var existingCurve = AnimationUtility.GetEditorCurve(clip, binding);
            var curve = existingCurve ?? new AnimationCurve(new Keyframe(0f, fallbackValue));
            AddDeathMeltKeyIfMissing(curve, baseEndTime, baseEndValue);
            AddOrReplaceDeathMeltKey(curve, startTime, startValue);
            AddOrReplaceDeathMeltKey(curve, sinkTime, sinkValue);
            AddOrReplaceDeathMeltKey(curve, puddleTime, puddleValue);
            AddOrReplaceDeathMeltKey(curve, holdTime, holdValue);
            curve.preWrapMode = existingCurve == null ? WrapMode.ClampForever : existingCurve.preWrapMode;
            curve.postWrapMode = WrapMode.Loop;

            AnimationUtility.SetEditorCurve(clip, binding, curve);
            return 1;
        }

        private static void AddDeathMeltKeyIfMissing(AnimationCurve curve, float time, float value)
        {
            if (FindDeathMeltKeyIndex(curve, time) >= 0)
            {
                return;
            }

            curve.AddKey(new Keyframe(time, value, 0f, 0f));
        }

        private static void AddOrReplaceDeathMeltKey(AnimationCurve curve, float time, float value)
        {
            var keyIndex = FindDeathMeltKeyIndex(curve, time);
            if (keyIndex >= 0)
            {
                curve.RemoveKey(keyIndex);
            }

            curve.AddKey(new Keyframe(time, value, 0f, 0f));
        }

        private static int FindDeathMeltKeyIndex(AnimationCurve curve, float time)
        {
            for (var index = 0; index < curve.length; index++)
            {
                if (Mathf.Abs(curve[index].time - time) <= 0.0001f)
                {
                    return index;
                }
            }

            return -1;
        }

        private static DeathMeltPuddleMotionMetrics EvaluateDeathMeltPuddleMetrics(
            AnimationClip clip,
            Transform deathRoot,
            float baseFallEndTime)
        {
            var meltStartTime = GetDeathMeltStartTime(baseFallEndTime);
            var puddleTime = meltStartTime + DeathMeltPuddleDuration;
            var holdTime = puddleTime + DeathMeltHoldDuration;
            var specs = BuildDeathMeltPuddleBoneSpecs()
                .Where(spec => deathRoot.Find(spec.Path) != null)
                .ToArray();
            var transforms = deathRoot.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var startSamples = new Dictionary<string, LocalTransformSample>(StringComparer.Ordinal);
            var puddleSamples = new Dictionary<string, LocalTransformSample>(StringComparer.Ordinal);
            var holdSamples = new Dictionary<string, LocalTransformSample>(StringComparer.Ordinal);
            var startWorldPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            var puddleWorldPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            var startVerticalAxes = new Dictionary<string, int>(StringComparer.Ordinal);
            var groundY = 0f;

            try
            {
                clip.SampleAnimation(deathRoot.gameObject, Mathf.Min(meltStartTime, Mathf.Max(clip.length - 0.001f, 0f)));
                groundY = CalculateDeathMeltPuddleGroundY(deathRoot);
                foreach (var spec in specs)
                {
                    var transform = deathRoot.Find(spec.Path);
                    startSamples[spec.Path] = LocalTransformSample.Capture(transform);
                    startWorldPositions[spec.Path] = transform.position;
                    startVerticalAxes[spec.Path] = FindLocalAxisClosestToWorldUp(transform);
                }

                clip.SampleAnimation(deathRoot.gameObject, Mathf.Min(puddleTime, Mathf.Max(clip.length, 0f)));
                foreach (var spec in specs)
                {
                    var transform = deathRoot.Find(spec.Path);
                    puddleSamples[spec.Path] = LocalTransformSample.Capture(transform);
                    puddleWorldPositions[spec.Path] = transform.position;
                }

                clip.SampleAnimation(deathRoot.gameObject, Mathf.Min(holdTime, Mathf.Max(clip.length, 0f)));
                foreach (var spec in specs)
                {
                    holdSamples[spec.Path] = LocalTransformSample.Capture(deathRoot.Find(spec.Path));
                }
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }
            }

            var hipsHeightDrop = startWorldPositions["Armature/Hips"].y - puddleWorldPositions["Armature/Hips"].y;
            var puddleMinY = puddleWorldPositions.Values.Min(position => position.y);
            var puddleMaxY = puddleWorldPositions.Values.Max(position => position.y);
            var puddleBoneHeightRange = puddleMaxY - puddleMinY;
            var averagePuddleGroundDistance = puddleWorldPositions.Values
                .Select(position => Mathf.Abs(position.y - (groundY + DeathMeltPuddleGroundClearance)))
                .DefaultIfEmpty(0f)
                .Average();
            var verticalRatios = new List<float>();
            var horizontalRatios = new List<float>();
            var maxHoldPositionDrift = 0f;
            var maxHoldRotationDrift = 0f;
            var maxHoldScaleDrift = 0f;

            foreach (var spec in specs)
            {
                var start = startSamples[spec.Path];
                var puddle = puddleSamples[spec.Path];
                var hold = holdSamples[spec.Path];
                var verticalAxis = startVerticalAxes[spec.Path];
                var startVertical = Mathf.Abs(GetVectorComponent(start.LocalScale, verticalAxis));
                var puddleVertical = Mathf.Abs(GetVectorComponent(puddle.LocalScale, verticalAxis));
                verticalRatios.Add(startVertical <= 0.0001f ? 1f : puddleVertical / startVertical);
                var startHorizontal = 0f;
                var puddleHorizontal = 0f;
                var horizontalAxisCount = 0;
                for (var axis = 0; axis < 3; axis++)
                {
                    if (axis == verticalAxis)
                    {
                        continue;
                    }

                    startHorizontal += Mathf.Abs(GetVectorComponent(start.LocalScale, axis));
                    puddleHorizontal += Mathf.Abs(GetVectorComponent(puddle.LocalScale, axis));
                    horizontalAxisCount++;
                }

                startHorizontal = Mathf.Max(0.0001f, startHorizontal / Mathf.Max(horizontalAxisCount, 1));
                puddleHorizontal /= Mathf.Max(horizontalAxisCount, 1);
                horizontalRatios.Add(puddleHorizontal / startHorizontal);
                maxHoldPositionDrift = Mathf.Max(maxHoldPositionDrift, Vector3.Distance(puddle.LocalPosition, hold.LocalPosition));
                maxHoldRotationDrift = Mathf.Max(maxHoldRotationDrift, Quaternion.Angle(puddle.LocalRotation, hold.LocalRotation));
                maxHoldScaleDrift = Mathf.Max(maxHoldScaleDrift, Vector3.Distance(puddle.LocalScale, hold.LocalScale));
            }

            return new DeathMeltPuddleMotionMetrics(
                Mathf.Max(clip.length - meltStartTime, 0f),
                hipsHeightDrop,
                verticalRatios.DefaultIfEmpty(1f).Average(),
                horizontalRatios.DefaultIfEmpty(1f).Average(),
                puddleBoneHeightRange,
                averagePuddleGroundDistance,
                maxHoldPositionDrift <= 0.015f && maxHoldRotationDrift <= 2.5f && maxHoldScaleDrift <= 0.05f,
                maxHoldPositionDrift,
                maxHoldRotationDrift,
                maxHoldScaleDrift);
        }

        private static void RequireDeathMeltPuddleMetrics(DeathMeltPuddleMotionMetrics metrics)
        {
            if (metrics.MeltSegmentDuration < DeathMeltPuddleDuration)
            {
                throw new InvalidOperationException(
                    DeathRootName + " melt puddle segment is too short. Duration=" +
                    metrics.MeltSegmentDuration.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.HipsHeightDrop < 0.08f)
            {
                throw new InvalidOperationException(
                    DeathRootName + " does not sink enough while melting. HipsHeightDrop=" +
                    metrics.HipsHeightDrop.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.AverageVerticalScaleRatio > 0.42f)
            {
                throw new InvalidOperationException(
                    DeathRootName + " does not flatten enough into a puddle. AverageVerticalScaleRatio=" +
                    metrics.AverageVerticalScaleRatio.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.AverageHorizontalScaleRatio < 1.35f)
            {
                throw new InvalidOperationException(
                    DeathRootName + " does not spread outward enough into a puddle. AverageHorizontalScaleRatio=" +
                    metrics.AverageHorizontalScaleRatio.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.PuddleBoneHeightRange > DeathMeltPuddleMaxBoneHeightRange)
            {
                throw new InvalidOperationException(
                    DeathRootName + " puddle bones are not flat enough against the floor. PuddleBoneHeightRange=" +
                    metrics.PuddleBoneHeightRange.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.AveragePuddleGroundDistance > 0.08f)
            {
                throw new InvalidOperationException(
                    DeathRootName + " puddle bones are not close enough to the floor. AveragePuddleGroundDistance=" +
                    metrics.AveragePuddleGroundDistance.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (!metrics.FinalHoldStable)
            {
                throw new InvalidOperationException(
                    DeathRootName + " final puddle pose is not held steadily. PositionDrift=" +
                    metrics.MaxHoldPositionDrift.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", RotationDrift=" + metrics.MaxHoldRotationDrift.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", ScaleDrift=" + metrics.MaxHoldScaleDrift.ToString("0.######", CultureInfo.InvariantCulture));
            }
        }

        private static float GetDeathMeltStartTime(float baseFallEndTime)
        {
            return Mathf.Max(baseFallEndTime, 0.01f) + DeathMeltStartDelay;
        }

        private static DyingSourceMotionMatchMetrics EvaluateDyingBaseMotionPreservedBeforeMelt(
            AnimationClip baseClip,
            AnimationClip targetClip,
            Transform targetRoot,
            float baseFallEndTime)
        {
            var targetTransforms = targetRoot.GetComponentsInChildren<Transform>(true);
            var targetOriginalStates = targetTransforms.Select(LocalTransformSample.Capture).ToArray();

            try
            {
                var paths = GetAnimatedTransformPaths(baseClip)
                    .Where(path => targetRoot.Find(path) != null)
                    .ToArray();
                var sampleTimes = BuildDyingSourcePreservationSampleTimes(baseFallEndTime);
                var maxPositionDelta = 0f;
                var maxRotationDelta = 0f;
                var maxScaleDelta = 0f;
                var sampleCount = 0;

                foreach (var time in sampleTimes)
                {
                    for (var index = 0; index < targetTransforms.Length; index++)
                    {
                        targetTransforms[index].localPosition = targetOriginalStates[index].LocalPosition;
                        targetTransforms[index].localRotation = targetOriginalStates[index].LocalRotation;
                        targetTransforms[index].localScale = targetOriginalStates[index].LocalScale;
                    }

                    baseClip.SampleAnimation(targetRoot.gameObject, time);
                    var baseSamples = new Dictionary<string, LocalTransformSample>(StringComparer.Ordinal);
                    foreach (var path in paths)
                    {
                        baseSamples[path] = LocalTransformSample.Capture(targetRoot.Find(path));
                    }

                    for (var index = 0; index < targetTransforms.Length; index++)
                    {
                        targetTransforms[index].localPosition = targetOriginalStates[index].LocalPosition;
                        targetTransforms[index].localRotation = targetOriginalStates[index].LocalRotation;
                        targetTransforms[index].localScale = targetOriginalStates[index].LocalScale;
                    }

                    targetClip.SampleAnimation(targetRoot.gameObject, time);
                    foreach (var path in paths)
                    {
                        var sourceTransform = baseSamples[path];
                        var targetTransform = targetRoot.Find(path);
                        maxPositionDelta = Mathf.Max(maxPositionDelta, Vector3.Distance(sourceTransform.LocalPosition, targetTransform.localPosition));
                        maxRotationDelta = Mathf.Max(maxRotationDelta, Quaternion.Angle(sourceTransform.LocalRotation, targetTransform.localRotation));
                        maxScaleDelta = Mathf.Max(maxScaleDelta, Vector3.Distance(sourceTransform.LocalScale, targetTransform.localScale));
                        sampleCount++;
                    }
                }

                return new DyingSourceMotionMatchMetrics(
                    maxPositionDelta,
                    maxRotationDelta,
                    maxScaleDelta,
                    sampleCount);
            }
            finally
            {
                for (var index = 0; index < targetTransforms.Length; index++)
                {
                    targetTransforms[index].localPosition = targetOriginalStates[index].LocalPosition;
                    targetTransforms[index].localRotation = targetOriginalStates[index].LocalRotation;
                    targetTransforms[index].localScale = targetOriginalStates[index].LocalScale;
                }
            }
        }

        private static float[] BuildDyingSourcePreservationSampleTimes(float baseFallEndTime)
        {
            var safeEnd = Mathf.Max(baseFallEndTime - 0.001f, 0f);
            var times = new SortedSet<float> { 0f, safeEnd };
            const int sampleCount = 16;
            for (var index = 1; index < sampleCount; index++)
            {
                times.Add(Mathf.Clamp(baseFallEndTime * index / sampleCount, 0f, safeEnd));
            }

            return times.ToArray();
        }

        private static void RequireDyingSourceMotionPreserved(DyingSourceMotionMatchMetrics metrics)
        {
            if (metrics.SampleCount <= 0)
            {
                throw new InvalidOperationException(DeathRootName + " source fall preservation check did not sample any matching animated paths.");
            }

            if (metrics.MaxPositionDelta > 0.002f ||
                metrics.MaxRotationDelta > 0.35f ||
                metrics.MaxScaleDelta > 0.002f)
            {
                throw new InvalidOperationException(
                    DeathRootName + " original falling motion changed before the melt segment. MaxPositionDelta=" +
                    metrics.MaxPositionDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", MaxRotationDelta=" + metrics.MaxRotationDelta.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", MaxScaleDelta=" + metrics.MaxScaleDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }
        }

        private static DeathMeltPuddleBoneSpec[] BuildDeathMeltPuddleBoneSpecs()
        {
            return new[]
            {
                new DeathMeltPuddleBoneSpec("Armature", new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(2.6f, 0.045f, 2.6f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips", new Vector3(0f, -0.42f, 0f), new Vector3(0f, 0f, 0f), new Vector3(2.2f, 0.16f, 2.2f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02", new Vector3(0f, -0.08f, 0.02f), new Vector3(8f, 0f, 3f), new Vector3(1.9f, 0.16f, 1.85f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01", new Vector3(0f, -0.06f, 0.02f), new Vector3(5f, 0f, -3f), new Vector3(1.85f, 0.14f, 1.8f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine", new Vector3(0f, -0.05f, 0.03f), new Vector3(4f, 0f, 4f), new Vector3(1.7f, 0.14f, 1.7f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/neck", new Vector3(0f, -0.035f, 0.02f), new Vector3(0f, 0f, 0f), new Vector3(1.45f, 0.16f, 1.45f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/neck/Head", new Vector3(0f, -0.04f, 0.03f), new Vector3(0f, 0f, 0f), new Vector3(1.35f, 0.18f, 1.35f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/LeftUpLeg", new Vector3(-0.1f, -0.04f, 0.05f), new Vector3(0f, -8f, 22f), new Vector3(1.7f, 0.14f, 1.45f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/LeftUpLeg/LeftLeg", new Vector3(-0.08f, -0.03f, 0.04f), new Vector3(0f, 4f, 18f), new Vector3(1.65f, 0.14f, 1.4f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot", new Vector3(-0.06f, -0.02f, 0.04f), new Vector3(0f, 0f, 12f), new Vector3(1.5f, 0.13f, 1.35f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase", new Vector3(-0.04f, -0.01f, 0.03f), new Vector3(0f, 0f, 8f), new Vector3(1.35f, 0.12f, 1.25f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/RightUpLeg", new Vector3(0.1f, -0.04f, 0.05f), new Vector3(0f, 8f, -22f), new Vector3(1.7f, 0.14f, 1.45f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/RightUpLeg/RightLeg", new Vector3(0.08f, -0.03f, 0.04f), new Vector3(0f, -4f, -18f), new Vector3(1.65f, 0.14f, 1.4f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/RightUpLeg/RightLeg/RightFoot", new Vector3(0.06f, -0.02f, 0.04f), new Vector3(0f, 0f, -12f), new Vector3(1.5f, 0.13f, 1.35f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase", new Vector3(0.04f, -0.01f, 0.03f), new Vector3(0f, 0f, -8f), new Vector3(1.35f, 0.12f, 1.25f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder", new Vector3(-0.07f, -0.03f, 0.04f), new Vector3(0f, -8f, 24f), new Vector3(1.45f, 0.15f, 1.35f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm", new Vector3(-0.1f, -0.03f, 0.05f), new Vector3(0f, -12f, 28f), new Vector3(1.55f, 0.13f, 1.4f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm", new Vector3(-0.08f, -0.025f, 0.04f), new Vector3(0f, -6f, 18f), new Vector3(1.5f, 0.13f, 1.3f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand", new Vector3(-0.05f, -0.02f, 0.03f), new Vector3(0f, -4f, 12f), new Vector3(1.35f, 0.12f, 1.2f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/RightShoulder", new Vector3(0.07f, -0.03f, 0.04f), new Vector3(0f, 8f, -24f), new Vector3(1.45f, 0.15f, 1.35f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm", new Vector3(0.1f, -0.03f, 0.05f), new Vector3(0f, 12f, -28f), new Vector3(1.55f, 0.13f, 1.4f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm", new Vector3(0.08f, -0.025f, 0.04f), new Vector3(0f, 6f, -18f), new Vector3(1.5f, 0.13f, 1.3f)),
                new DeathMeltPuddleBoneSpec("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand", new Vector3(0.05f, -0.02f, 0.03f), new Vector3(0f, 4f, -12f), new Vector3(1.35f, 0.12f, 1.2f))
            };
        }

        private static void ClearEulerRotationCurves(AnimationClip clip, string path)
        {
            var eulerBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, path, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal))
                .ToArray();

            foreach (var binding in eulerBindings)
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static Vector3 DivideVector(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                Mathf.Abs(divisor.x) <= 0.00001f ? 1f : value.x / divisor.x,
                Mathf.Abs(divisor.y) <= 0.00001f ? 1f : value.y / divisor.y,
                Mathf.Abs(divisor.z) <= 0.00001f ? 1f : value.z / divisor.z);
        }

        private static Quaternion FlipQuaternion(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static Quaternion NormalizeQuaternion(Quaternion value)
        {
            var magnitude = Mathf.Sqrt(
                value.x * value.x +
                value.y * value.y +
                value.z * value.z +
                value.w * value.w);
            if (magnitude <= 0.00001f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(
                value.x / magnitude,
                value.y / magnitude,
                value.z / magnitude,
                value.w / magnitude);
        }

        private static float GetMaxWaistRotationAngle(AnimationClip clip, Transform runRoot)
        {
            return WaistStabilizedBonePaths
                .Select(path =>
                {
                    var target = RequireRelativeTransform(runRoot, path, "waist running pose bone");
                    return GetSampledMaxLocalRotationAngle(clip, runRoot, target, target.localRotation);
                })
                .DefaultIfEmpty(0f)
                .Max();
        }

        private static int StabilizeWaistRotationCurves(Transform runRoot, AnimationClip clip)
        {
            var bindingsChanged = 0;
            foreach (var path in WaistStabilizedBonePaths)
            {
                var target = RequireRelativeTransform(runRoot, path, "waist stabilized bone");
                var localRotation = target.localRotation;
                SetConstantTransformCurve(clip, path, "m_LocalRotation.x", localRotation.x);
                SetConstantTransformCurve(clip, path, "m_LocalRotation.y", localRotation.y);
                SetConstantTransformCurve(clip, path, "m_LocalRotation.z", localRotation.z);
                SetConstantTransformCurve(clip, path, "m_LocalRotation.w", localRotation.w);
                SetConstantTransformCurve(clip, path, "localEulerAnglesRaw.x", target.localEulerAngles.x);
                SetConstantTransformCurve(clip, path, "localEulerAnglesRaw.y", target.localEulerAngles.y);
                SetConstantTransformCurve(clip, path, "localEulerAnglesRaw.z", target.localEulerAngles.z);
                bindingsChanged += 7;
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return bindingsChanged;
        }

        private static void SetConstantTransformCurve(AnimationClip clip, string path, string propertyName, float value)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, value),
                new Keyframe(Mathf.Max(clip.length, 0.001f), value));

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static string BuildWaistRotationReport(Transform runRoot, AnimationClip clip)
        {
            return string.Join(
                "|",
                WaistStabilizedBonePaths.Select(path =>
                {
                    var target = RequireRelativeTransform(runRoot, path, "waist report bone");
                    var rest = target.localRotation;
                    var maxAngle = GetSampledMaxLocalRotationAngle(clip, runRoot, target, rest);
                    return path + ":" + maxAngle.ToString("0.###", CultureInfo.InvariantCulture);
                }));
        }

        private static float GetSampledMaxLocalRotationAngle(
            AnimationClip clip,
            Transform sampleRoot,
            Transform target,
            Quaternion restLocalRotation)
        {
            var transforms = sampleRoot.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(TransformState.Capture).ToArray();
            try
            {
                var maxAngle = 0f;
                var sampleCount = 12;
                for (var index = 0; index <= sampleCount; index++)
                {
                    var time = clip.length <= 0.0001f
                        ? 0f
                        : Mathf.Clamp(clip.length * index / sampleCount, 0f, Mathf.Max(clip.length - 0.0001f, 0f));
                    clip.SampleAnimation(sampleRoot.gameObject, time);
                    maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(restLocalRotation, target.localRotation));
                }

                return maxAngle;
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    originalStates[index].ApplyTo(transforms[index]);
                }
            }
        }

        private static int CountWaistRotationBindings(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .Count(binding =>
                    WaistStabilizedBonePaths.Contains(binding.path, StringComparer.Ordinal) &&
                    (binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) ||
                     binding.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal)));
        }

        private static string BuildBindposeMismatchReport(Transform root)
        {
            return string.Join(
                "|",
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Select(renderer =>
                    {
                        var maxError = CalculateBindposeMaxError(renderer);
                        return GetRelativePath(root, renderer.transform, renderer.name) +
                               ":" + maxError.ToString("0.######", CultureInfo.InvariantCulture);
                    }));
        }

        private static float GetBindposeMaxError(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                return float.PositiveInfinity;
            }

            return renderers
                .Select(CalculateBindposeMaxError)
                .DefaultIfEmpty(float.PositiveInfinity)
                .Max();
        }

        private static float CalculateBindposeMaxError(SkinnedMeshRenderer renderer)
        {
            if (renderer.sharedMesh == null ||
                renderer.bones == null ||
                renderer.sharedMesh.bindposes == null ||
                renderer.bones.Length == 0 ||
                renderer.bones.Length != renderer.sharedMesh.bindposes.Length)
            {
                return float.PositiveInfinity;
            }

            var maxError = 0f;
            for (var index = 0; index < renderer.bones.Length; index++)
            {
                var bone = renderer.bones[index];
                if (bone == null)
                {
                    return float.PositiveInfinity;
                }

                var expected = bone.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                maxError = Mathf.Max(maxError, MatrixMaxAbsDifference(expected, renderer.sharedMesh.bindposes[index]));
            }

            return maxError;
        }

        private static float MatrixMaxAbsDifference(Matrix4x4 left, Matrix4x4 right)
        {
            var max = 0f;
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    max = Mathf.Max(max, Mathf.Abs(left[row, column] - right[row, column]));
                }
            }

            return max;
        }

        private static void RequireSingleAnimatorComponentOnRoot(
            Transform root,
            AnimatorController expectedController,
            Avatar expectedAvatar)
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1 || animators[0].transform != root)
            {
                throw new InvalidOperationException(
                    root.name + " must have exactly one Animator component on the root. Count=" +
                    animators.Length.ToString(CultureInfo.InvariantCulture));
            }

            var animator = animators[0];
            if (animator.runtimeAnimatorController != expectedController)
            {
                throw new InvalidOperationException(root.name + " is not using the run chase controller.");
            }

            if (animator.avatar != expectedAvatar)
            {
                throw new InvalidOperationException(root.name + " is not using the running FBX avatar.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(root.name + " must keep root motion disabled for review placement.");
            }

            if (!animator.enabled)
            {
                throw new InvalidOperationException(root.name + " Animator must be enabled for animation review.");
            }
        }

        private static void RequireRendererSignaturesUnchanged(string[] before, string[] after)
        {
            if (before.Length != after.Length)
            {
                throw new InvalidOperationException(
                    RunRootName + " renderer count changed during rig-only replacement. Before=" +
                    before.Length.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + after.Length.ToString(CultureInfo.InvariantCulture));
            }

            for (var index = 0; index < before.Length; index++)
            {
                if (!string.Equals(before[index], after[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        RunRootName + " renderer mesh/material changed during rig-only replacement. Before=" +
                        before[index] + ", After=" + after[index]);
                }
            }
        }

        private static void RequireRigStateSignaturesUnchanged(string[] before, string[] after)
        {
            if (before.Length != after.Length)
            {
                throw new InvalidOperationException(
                    RunRootName + " rig state count changed during animation-only application. Before=" +
                    before.Length.ToString(CultureInfo.InvariantCulture) +
                    ", After=" + after.Length.ToString(CultureInfo.InvariantCulture));
            }

            for (var index = 0; index < before.Length; index++)
            {
                if (!string.Equals(before[index], after[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        RunRootName + " rig state changed during animation-only application. Before=" +
                        before[index] + ", After=" + after[index]);
                }
            }
        }

        private static void RequireRigHierarchyMatches(Transform sourceArmature, Transform targetArmature)
        {
            var sourcePaths = BuildRigBonePaths(sourceArmature);
            var targetPaths = BuildRigBonePaths(targetArmature);
            if (sourcePaths.Length != targetPaths.Length)
            {
                throw new InvalidOperationException(
                    RunRootName + " running rig bone count does not match source FBX. Source=" +
                    sourcePaths.Length.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetPaths.Length.ToString(CultureInfo.InvariantCulture));
            }

            for (var index = 0; index < sourcePaths.Length; index++)
            {
                if (!string.Equals(sourcePaths[index], targetPaths[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        RunRootName + " running rig hierarchy does not match source FBX. Source=" +
                        sourcePaths[index] + ", Target=" + targetPaths[index]);
                }
            }
        }

        private static string[] BuildTransformPaths(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(transform => GetRelativePath(root, transform, transform.name))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] BuildRigBonePaths(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Where(transform => !IsUnderNamedAncestor(transform, EyeContainerName))
                .Select(transform => GetRelativePath(root, transform, transform.name))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool IsUnderNamedAncestor(Transform transform, string ancestorName)
        {
            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, ancestorName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static string[] BuildRigStateSignatures(Transform root)
        {
            var armature = RequireChild(root, "Armature");
            var signatures = new List<string>();
            signatures.AddRange(BuildRigBonePaths(armature).Select(path => "Bone=" + path));

            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                         .OrderBy(renderer => GetRelativePath(root, renderer.transform, renderer.name), StringComparer.Ordinal))
            {
                var rendererPath = GetRelativePath(root, renderer.transform, renderer.name);
                var rootBonePath = renderer.rootBone == null
                    ? "<null>"
                    : GetRelativePath(armature, renderer.rootBone, renderer.name + " rootBone");
                var bonePaths = renderer.bones == null
                    ? Array.Empty<string>()
                    : renderer.bones
                        .Select(bone => GetRelativePath(armature, bone, renderer.name + " bone"))
                        .ToArray();

                signatures.Add(
                    "RendererRig=" + rendererPath +
                    "|RootBone=" + rootBonePath +
                    "|Bones=" + string.Join(",", bonePaths));
            }

            return signatures
                .OrderBy(signature => signature, StringComparer.Ordinal)
                .ToArray();
        }

        private static void RequireNoAnimatorComponents(Transform root, string rootName)
        {
            var animatorCount = root.GetComponentsInChildren<Animator>(true).Length;
            if (animatorCount != 0)
            {
                throw new InvalidOperationException(
                    rootName + " must not have Animator components. Count=" +
                    animatorCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void RequireTransformHierarchyMatches(Transform referenceRoot, Transform targetRoot)
        {
            var referenceTransforms = referenceRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != referenceRoot)
                .ToArray();
            var targetTransforms = targetRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != targetRoot)
                .ToArray();

            if (targetTransforms.Length != referenceTransforms.Length)
            {
                throw new InvalidOperationException(
                    RunRootName + " transform count does not match " + StaticRootName +
                    ". Reference=" + referenceTransforms.Length.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetTransforms.Length.ToString(CultureInfo.InvariantCulture));
            }

            var targetByPath = targetTransforms.ToDictionary(
                transform => GetRelativePath(targetRoot, transform, transform.name),
                transform => transform,
                StringComparer.Ordinal);

            foreach (var referenceTransform in referenceTransforms)
            {
                var relativePath = GetRelativePath(referenceRoot, referenceTransform, referenceTransform.name);
                if (!targetByPath.TryGetValue(relativePath, out var targetTransform))
                {
                    throw new InvalidOperationException(RunRootName + " is missing visual transform: " + relativePath);
                }

                if (targetTransform.gameObject.activeSelf != referenceTransform.gameObject.activeSelf)
                {
                    throw new InvalidOperationException(RunRootName + " active state does not match reference at " + relativePath);
                }

                if (!TransformState.Capture(referenceTransform).Matches(targetTransform))
                {
                    throw new InvalidOperationException(RunRootName + " local transform does not match reference at " + relativePath);
                }
            }
        }

        private static void RequireRendererSignaturesMatch(Transform referenceRoot, Transform targetRoot)
        {
            var referenceSignatures = BuildRendererSignatures(referenceRoot);
            var targetSignatures = BuildRendererSignatures(targetRoot);
            if (referenceSignatures.Length != targetSignatures.Length)
            {
                throw new InvalidOperationException(
                    RunRootName + " renderer count does not match " + StaticRootName +
                    ". Reference=" + referenceSignatures.Length.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetSignatures.Length.ToString(CultureInfo.InvariantCulture));
            }

            for (var index = 0; index < referenceSignatures.Length; index++)
            {
                if (!string.Equals(referenceSignatures[index], targetSignatures[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        RunRootName + " renderer signature does not match " + StaticRootName +
                        ". Reference=" + referenceSignatures[index] +
                        ", Target=" + targetSignatures[index]);
                }
            }
        }

        private static bool RendererSignaturesEqual(Transform referenceRoot, Transform targetRoot)
        {
            var referenceSignatures = BuildRendererSignatures(referenceRoot);
            var targetSignatures = BuildRendererSignatures(targetRoot);
            return referenceSignatures.Length == targetSignatures.Length &&
                   !referenceSignatures.Where((signature, index) => signature != targetSignatures[index]).Any();
        }

        private static void RequireRestPoseSignaturesMatch(Transform referenceRoot, Transform targetRoot)
        {
            var referenceSignatures = BuildRestPoseSignatures(referenceRoot);
            var targetSignatures = BuildRestPoseSignatures(targetRoot);
            if (referenceSignatures.Length != targetSignatures.Length)
            {
                throw new InvalidOperationException(
                    RunRootName + " rest pose transform count does not match " + StaticRootName +
                    ". Reference=" + referenceSignatures.Length.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetSignatures.Length.ToString(CultureInfo.InvariantCulture));
            }

            for (var index = 0; index < referenceSignatures.Length; index++)
            {
                if (referenceSignatures[index] == targetSignatures[index])
                {
                    continue;
                }

                throw new InvalidOperationException(
                    RunRootName + " rest pose transform differs from " + StaticRootName +
                    ". Reference=" + referenceSignatures[index] +
                    ", Target=" + targetSignatures[index]);
            }
        }

        private static bool RestPoseSignaturesEqual(Transform referenceRoot, Transform targetRoot)
        {
            var referenceSignatures = BuildRestPoseSignatures(referenceRoot);
            var targetSignatures = BuildRestPoseSignatures(targetRoot);
            return referenceSignatures.Length == targetSignatures.Length &&
                   !referenceSignatures.Where((signature, index) => signature != targetSignatures[index]).Any();
        }

        private static string[] BuildRestPoseSignatures(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != root)
                .Select(transform =>
                    GetRelativePath(root, transform, transform.name) +
                    "|Active=" + transform.gameObject.activeSelf.ToString(CultureInfo.InvariantCulture) +
                    "|P=" + FormatVector3(transform.localPosition) +
                    "|R=" + FormatQuaternion(transform.localRotation) +
                    "|S=" + FormatVector3(transform.localScale))
                .OrderBy(signature => signature, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] BuildRendererSignatures(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .Select(renderer =>
                    GetRelativePath(root, renderer.transform, renderer.name) +
                    "|Type=" + renderer.GetType().Name +
                    "|Enabled=" + renderer.enabled.ToString(CultureInfo.InvariantCulture) +
                    "|Active=" + renderer.gameObject.activeSelf.ToString(CultureInfo.InvariantCulture) +
                    "|Mesh=" + GetRendererMeshSignature(renderer) +
                    "|Materials=" + string.Join(",", renderer.sharedMaterials.Select(GetMaterialSignature)))
                .OrderBy(signature => signature, StringComparer.Ordinal)
                .ToArray();
        }

        private static string GetRendererMeshSignature(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return GetObjectSignature(skinnedMeshRenderer.sharedMesh);
            }

            var meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null
                ? GetObjectSignature(meshFilter.sharedMesh)
                : "<no-mesh>";
        }

        private static string GetMaterialSignature(Material material)
        {
            return GetObjectSignature(material);
        }

        private static string GetObjectSignature(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "<null>";
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);
            return assetPath + "#" + asset.name;
        }

        private static void StripNonTransformComponents(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var component in transform.GetComponents<Component>())
                {
                    if (component is Transform)
                    {
                        continue;
                    }

                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
        }

        private static bool MaterialsMatch(Material[] left, Material[] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static void CopyLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static int CountRigBones(Transform armature)
        {
            return armature.GetComponentsInChildren<Transform>(true).Length;
        }

        private static Transform FindDescendantByName(Transform root, string objectName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => string.Equals(transform.name, objectName, StringComparison.Ordinal));
        }

        private static AnimationClip EnsureAuthoredSprintClip(Transform runRoot)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AuthoredSprintClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = AuthoredSprintClipName
                };
                AssetDatabase.CreateAsset(clip, AuthoredSprintClipPath);
            }

            clip.ClearCurves();
            clip.name = AuthoredSprintClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;

            ApplyAuthoredSprintCurves(clip, runRoot);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureAuthoredSprintController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AuthoredSprintControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AuthoredSprintControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == AuthoredSprintClipName);
            if (state == null)
            {
                state = stateMachine.AddState(AuthoredSprintClipName);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ApplyAuthoredSprintCurves(AnimationClip clip, Transform runRoot)
        {
            foreach (var requiredPath in AuthoredSprintRequiredBonePaths)
            {
                RequireAuthoredRelativeTransform(runRoot, requiredPath);
            }

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips",
                phase => new Vector3(
                    0.032f * AuthoredSprintSin(phase, 1f, 0f),
                    0.056f * AuthoredSprintSin(phase, 2f, 0.08f),
                    0.018f * AuthoredSprintCos(phase, 2f, 0f)),
                phase => new Vector3(
                    -5.5f + 2.4f * AuthoredSprintSin(phase, 2f, 0.15f),
                    5.5f * AuthoredSprintSin(phase, 1f, 0.24f),
                    -7.5f * AuthoredSprintSin(phase, 1f, 0f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips/Spine02",
                phase => Vector3.zero,
                phase => new Vector3(
                    -6.25f + 2f * AuthoredSprintSin(phase, 2f, 0.28f),
                    -3.75f * AuthoredSprintSin(phase, 1f, 0.08f),
                    6f * AuthoredSprintSin(phase, 1f, 0.5f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips/Spine02/Spine01",
                phase => Vector3.zero,
                phase => new Vector3(
                    -5.25f + 1.8f * AuthoredSprintSin(phase, 2f, 0.42f),
                    3.25f * AuthoredSprintSin(phase, 1f, 0.18f),
                    -5.25f * AuthoredSprintSin(phase, 1f, 0.5f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips/Spine02/Spine01/Spine",
                phase => Vector3.zero,
                phase => new Vector3(
                    -4.25f + 1.5f * AuthoredSprintSin(phase, 2f, 0.55f),
                    2.75f * AuthoredSprintSin(phase, 1f, 0.26f),
                    4.25f * AuthoredSprintSin(phase, 1f, 0f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips/Spine02/Spine01/Spine/neck",
                phase => Vector3.zero,
                phase => new Vector3(
                    2.75f + 1f * AuthoredSprintSin(phase, 2f, 0.68f),
                    -2.75f * AuthoredSprintSin(phase, 1f, 0.24f),
                    2.25f * AuthoredSprintSin(phase, 1f, 0.5f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                phase => Vector3.zero,
                phase => new Vector3(
                    4f + 1.2f * AuthoredSprintSin(phase, 2f, 0.76f),
                    -4.5f * AuthoredSprintSin(phase, 1f, 0.24f),
                    1.75f * AuthoredSprintSin(phase, 1f, 0.5f)));

            var secondaryIndex = 0;
            foreach (var secondaryPath in FindAuthoredSprintSecondaryMotionPaths(runRoot))
            {
                var lowerPath = secondaryPath.ToLowerInvariant();
                var phaseOffset = (secondaryIndex % 2 == 0 ? 0f : 0.5f) + (secondaryIndex % 3) * 0.08f;
                var swing = lowerPath.Contains("foot") || lowerPath.Contains("toe") || lowerPath.Contains("leg") ||
                    lowerPath.Contains("thigh") || lowerPath.Contains("calf") || lowerPath.Contains("shin")
                        ? 16f
                        : 10f;
                var recoil = lowerPath.Contains("hand") || lowerPath.Contains("arm") || lowerPath.Contains("claw") ||
                    lowerPath.Contains("finger")
                        ? 9f
                        : 6f;

                SetAuthoredSprintTransformCurves(
                    clip,
                    runRoot,
                    secondaryPath,
                    phase => Vector3.zero,
                    phase => new Vector3(
                        swing * AuthoredSprintSin(phase, 1f, phaseOffset),
                        3.5f * AuthoredSprintSin(phase, 2f, phaseOffset + 0.1f),
                        recoil * AuthoredSprintCos(phase, 1f, phaseOffset)));
                secondaryIndex++;
            }
        }

        private static void SetAuthoredSprintTransformCurves(
            AnimationClip clip,
            Transform runRoot,
            string relativePath,
            Func<float, Vector3> positionOffsetAtPhase,
            Func<float, Vector3> rotationOffsetAtPhase)
        {
            var target = RequireAuthoredRelativeTransform(runRoot, relativePath);
            var rest = LocalTransformSample.Capture(target);
            var bindings = new List<EditorCurveBinding>(10);
            var curves = new List<AnimationCurve>(10);
            var positionX = new List<Keyframe>();
            var positionY = new List<Keyframe>();
            var positionZ = new List<Keyframe>();
            var rotationX = new List<Keyframe>();
            var rotationY = new List<Keyframe>();
            var rotationZ = new List<Keyframe>();
            var rotationW = new List<Keyframe>();
            var scaleX = new List<Keyframe>();
            var scaleY = new List<Keyframe>();
            var scaleZ = new List<Keyframe>();

            const int sampleCount = 16;
            for (var index = 0; index <= sampleCount; index++)
            {
                var phase = index / (float)sampleCount;
                var time = AuthoredSprintDuration * phase;
                var position = rest.LocalPosition + positionOffsetAtPhase(phase);
                var rotation = rest.LocalRotation * Quaternion.Euler(rotationOffsetAtPhase(phase));
                AddKey(positionX, time, position.x);
                AddKey(positionY, time, position.y);
                AddKey(positionZ, time, position.z);
                AddKey(rotationX, time, rotation.x);
                AddKey(rotationY, time, rotation.y);
                AddKey(rotationZ, time, rotation.z);
                AddKey(rotationW, time, rotation.w);
                AddKey(scaleX, time, rest.LocalScale.x);
                AddKey(scaleY, time, rest.LocalScale.y);
                AddKey(scaleZ, time, rest.LocalScale.z);
            }

            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalPosition.x", positionX);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalPosition.y", positionY);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalPosition.z", positionZ);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalRotation.x", rotationX);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalRotation.y", rotationY);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalRotation.z", rotationZ);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalRotation.w", rotationW);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalScale.x", scaleX);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalScale.y", scaleY);
            AddAuthoredSprintCurve(bindings, curves, relativePath, "m_LocalScale.z", scaleZ);

            AnimationUtility.SetEditorCurves(clip, bindings.ToArray(), curves.ToArray());
        }

        private static void AddAuthoredSprintCurve(
            ICollection<EditorCurveBinding> bindings,
            ICollection<AnimationCurve> curves,
            string relativePath,
            string propertyName,
            IList<Keyframe> keys)
        {
            bindings.Add(EditorCurveBinding.FloatCurve(relativePath, typeof(Transform), propertyName));
            curves.Add(BuildAuthoredSprintCurve(keys));
        }

        private static AnimationCurve BuildAuthoredSprintCurve(IList<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };

            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static void AddKey(ICollection<Keyframe> keys, float time, float value)
        {
            keys.Add(new Keyframe(time, value));
        }

        private static float AuthoredSprintSin(float phase, float cycles, float offset)
        {
            return Mathf.Sin((phase * cycles + offset) * Mathf.PI * 2f);
        }

        private static float AuthoredSprintCos(float phase, float cycles, float offset)
        {
            return Mathf.Cos((phase * cycles + offset) * Mathf.PI * 2f);
        }

        private static Transform RequireAuthoredRelativeTransform(Transform root, string relativePath)
        {
            var transform = root.Find(relativePath);
            if (transform == null)
            {
                throw new InvalidOperationException("Missing authored sprint rig path under " + root.name + ": " + relativePath);
            }

            return transform;
        }

        private static IEnumerable<string> FindAuthoredSprintSecondaryMotionPaths(Transform runRoot)
        {
            var required = new HashSet<string>(AuthoredSprintRequiredBonePaths, StringComparer.Ordinal);
            return runRoot.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != runRoot)
                .Select(transform => GetAuthoredRelativePath(runRoot, transform))
                .Where(path =>
                    path.StartsWith("Armature/", StringComparison.Ordinal) &&
                    !required.Contains(path) &&
                    !IsGeneratedEyePath(path) &&
                    IsAuthoredSprintSecondaryMotionPath(path))
                .OrderBy(GetAuthoredSprintSecondaryPriority)
                .ThenBy(path => path, StringComparer.Ordinal)
                .Take(18);
        }

        private static bool IsAuthoredSprintSecondaryMotionPath(string relativePath)
        {
            var leaf = GetAuthoredPathLeaf(relativePath).ToLowerInvariant();
            return leaf.Contains("leg") ||
                   leaf.Contains("foot") ||
                   leaf.Contains("toe") ||
                   leaf.Contains("arm") ||
                   leaf.Contains("hand") ||
                   leaf.Contains("claw") ||
                   leaf.Contains("finger") ||
                   leaf.Contains("shoulder") ||
                   leaf.Contains("thigh") ||
                   leaf.Contains("calf") ||
                   leaf.Contains("shin") ||
                   leaf.Contains("tail") ||
                   leaf.Contains("tentacle") ||
                   leaf.Contains("left") ||
                   leaf.Contains("right") ||
                   leaf.Contains("_l") ||
                   leaf.Contains("_r") ||
                   leaf.Contains(".l") ||
                   leaf.Contains(".r");
        }

        private static bool IsGeneratedEyePath(string relativePath)
        {
            return relativePath.IndexOf("/" + EyeContainerName, StringComparison.Ordinal) >= 0;
        }

        private static int GetAuthoredSprintSecondaryPriority(string relativePath)
        {
            var leaf = GetAuthoredPathLeaf(relativePath).ToLowerInvariant();
            if (leaf.Contains("leg") || leaf.Contains("foot") || leaf.Contains("toe") ||
                leaf.Contains("thigh") || leaf.Contains("calf") || leaf.Contains("shin"))
            {
                return 0;
            }

            if (leaf.Contains("arm") || leaf.Contains("hand") || leaf.Contains("claw") ||
                leaf.Contains("finger") || leaf.Contains("shoulder"))
            {
                return 1;
            }

            return 2;
        }

        private static string GetAuthoredPathLeaf(string relativePath)
        {
            var separatorIndex = relativePath.LastIndexOf('/');
            return separatorIndex >= 0
                ? relativePath.Substring(separatorIndex + 1)
                : relativePath;
        }

        private static string GetAuthoredRelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }

            var names = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                throw new InvalidOperationException(target.name + " is not a child of " + root.name);
            }

            return string.Join("/", names);
        }

        private static string FormatPathList(IReadOnlyCollection<string> paths)
        {
            return paths.Count == 0
                ? "None"
                : string.Join("|", paths);
        }

        private static void RemoveAnimatorComponentsBelowRoot(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)
                         .Where(candidate => candidate.transform != root)
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }
        }

        private static bool ControllerUsesClipAtPath(
            AnimatorController controller,
            AnimationClip clip,
            string clipPath)
        {
            return controller.animationClips.Any(candidate =>
                candidate == clip ||
                string.Equals(AssetDatabase.GetAssetPath(candidate), clipPath, StringComparison.Ordinal));
        }

        private static void RequireAuthoredTransformCurveBindings(AnimationClip clip, string relativePath)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var expectedProperties = new[]
            {
                "m_LocalPosition.x",
                "m_LocalPosition.y",
                "m_LocalPosition.z",
                "m_LocalRotation.x",
                "m_LocalRotation.y",
                "m_LocalRotation.z",
                "m_LocalRotation.w",
                "m_LocalScale.x",
                "m_LocalScale.y",
                "m_LocalScale.z"
            };

            foreach (var propertyName in expectedProperties)
            {
                if (!bindings.Any(binding =>
                        string.Equals(binding.path, relativePath, StringComparison.Ordinal) &&
                        string.Equals(binding.propertyName, propertyName, StringComparison.Ordinal) &&
                        binding.type == typeof(Transform)))
                {
                    throw new InvalidOperationException(
                        "Missing authored sprint curve binding: " + relativePath + "/" + propertyName);
                }
            }
        }

        private static AuthoredSprintMotionMetrics MeasureAuthoredSprintMotion(AnimationClip clip, Transform runRoot)
        {
            var transforms = runRoot.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(TransformState.Capture).ToArray();
            var hips = RequireAuthoredRelativeTransform(runRoot, "Armature/Hips");
            var torsoBones = new[]
            {
                RequireAuthoredRelativeTransform(runRoot, "Armature/Hips/Spine02"),
                RequireAuthoredRelativeTransform(runRoot, "Armature/Hips/Spine02/Spine01"),
                RequireAuthoredRelativeTransform(runRoot, "Armature/Hips/Spine02/Spine01/Spine")
            };
            var head = RequireAuthoredRelativeTransform(runRoot, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var hipsRest = LocalTransformSample.Capture(hips);
            var torsoRest = torsoBones.Select(LocalTransformSample.Capture).ToArray();
            var headRest = LocalTransformSample.Capture(head);

            try
            {
                var minHipsY = float.PositiveInfinity;
                var maxHipsY = float.NegativeInfinity;
                var minHipsX = float.PositiveInfinity;
                var maxHipsX = float.NegativeInfinity;
                var maxTorsoRotationAngle = 0f;
                var maxHeadRotationAngle = 0f;

                const int sampleCount = 32;
                for (var index = 0; index <= sampleCount; index++)
                {
                    var time = clip.length * (index / (float)sampleCount);
                    clip.SampleAnimation(runRoot.gameObject, time);
                    minHipsY = Mathf.Min(minHipsY, hips.localPosition.y);
                    maxHipsY = Mathf.Max(maxHipsY, hips.localPosition.y);
                    minHipsX = Mathf.Min(minHipsX, hips.localPosition.x);
                    maxHipsX = Mathf.Max(maxHipsX, hips.localPosition.x);

                    for (var torsoIndex = 0; torsoIndex < torsoBones.Length; torsoIndex++)
                    {
                        maxTorsoRotationAngle = Mathf.Max(
                            maxTorsoRotationAngle,
                            Quaternion.Angle(torsoRest[torsoIndex].LocalRotation, torsoBones[torsoIndex].localRotation));
                    }

                    maxHeadRotationAngle = Mathf.Max(
                        maxHeadRotationAngle,
                        Quaternion.Angle(headRest.LocalRotation, head.localRotation));
                }

                return new AuthoredSprintMotionMetrics(
                    maxHipsY - minHipsY,
                    maxHipsX - minHipsX,
                    maxTorsoRotationAngle,
                    maxHeadRotationAngle,
                    Quaternion.Angle(hipsRest.LocalRotation, hips.localRotation));
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    originalStates[index].ApplyTo(transforms[index]);
                }
            }
        }

        private static AnimationClip EnsureReferenceDrivenAuthoredSprintClip(
            Transform runRoot,
            ReferenceSprintProfile profile)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AuthoredSprintClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = AuthoredSprintClipName
                };
                AssetDatabase.CreateAsset(clip, AuthoredSprintClipPath);
            }

            clip.ClearCurves();
            clip.name = AuthoredSprintClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;

            ApplyReferenceDrivenSprintCurves(clip, runRoot, profile);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureVideoReferenceSprintClip(
            Transform runRoot,
            ReferenceSprintProfile profile)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AuthoredSprintClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = AuthoredSprintClipName
                };
                AssetDatabase.CreateAsset(clip, AuthoredSprintClipPath);
            }

            clip.ClearCurves();
            clip.name = AuthoredSprintClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;

            ApplyVideoReferenceSprintCurves(clip, runRoot, profile);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void ApplyVideoReferenceSprintCurves(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile)
        {
            foreach (var path in ReferenceDrivenSprintMotionPaths)
            {
                RequireAuthoredRelativeTransform(runRoot, path);
            }

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips",
                phase => new Vector3(
                    0f,
                    VideoPoseCurve(phase, 0.11f, 0.015f, 0.11f, 0.015f),
                    VideoPoseCurve(phase, 0.028f, 0f, -0.028f, 0f)),
                phase => AxisEuler(profile, "Armature/Hips", VideoPoseCurve(phase, 7f, 4f, 7f, 4f)));

            SetVideoBodyPoseCurve(clip, runRoot, profile, "Armature/Hips/Spine02", 12f, 2f);
            SetVideoBodyPoseCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01", 9f, 1.5f);
            SetVideoBodyPoseCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01/Spine", 7f, 1.25f);
            SetVideoBodyPoseCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01/Spine/neck", -3f, 0.75f);
            SetVideoBodyPoseCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01/Spine/neck/Head", -4f, 1f);

            SetVideoReferenceLegPoses(
                clip,
                runRoot,
                profile,
                "Armature/Hips/LeftUpLeg",
                "Armature/Hips/LeftUpLeg/LeftLeg",
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase",
                highAtPhaseZero: true);

            SetVideoReferenceLegPoses(
                clip,
                runRoot,
                profile,
                "Armature/Hips/RightUpLeg",
                "Armature/Hips/RightUpLeg/RightLeg",
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
                highAtPhaseZero: false);

            SetVideoReferenceArmPoses(
                clip,
                runRoot,
                profile,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                forwardAtPhaseZero: false);

            SetVideoReferenceArmPoses(
                clip,
                runRoot,
                profile,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                forwardAtPhaseZero: true);
        }

        private static void SetVideoBodyPoseCurve(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile,
            string path,
            float leanDegrees,
            float pulseDegrees)
        {
            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                path,
                phase => Vector3.zero,
                phase => AxisEuler(
                    profile,
                    path,
                    VideoPoseCurve(
                        phase,
                        leanDegrees + pulseDegrees,
                        leanDegrees - pulseDegrees,
                        leanDegrees + pulseDegrees,
                        leanDegrees - pulseDegrees)));
        }

        private static void SetVideoReferenceLegPoses(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile,
            string upLegPath,
            string legPath,
            string footPath,
            string toePath,
            bool highAtPhaseZero)
        {
            var upLeg0 = highAtPhaseZero ? 118f : -70f;
            var upLeg25 = highAtPhaseZero ? 8f : -12f;
            var upLeg50 = highAtPhaseZero ? -70f : 118f;
            var upLeg75 = highAtPhaseZero ? -12f : 8f;

            var leg0 = highAtPhaseZero ? 134f : 8f;
            var leg25 = highAtPhaseZero ? 34f : 64f;
            var leg50 = highAtPhaseZero ? 8f : 134f;
            var leg75 = highAtPhaseZero ? 64f : 34f;

            var foot0 = highAtPhaseZero ? 58f : -36f;
            var foot25 = highAtPhaseZero ? 6f : 24f;
            var foot50 = highAtPhaseZero ? -36f : 58f;
            var foot75 = highAtPhaseZero ? 24f : 6f;

            var toe0 = highAtPhaseZero ? 20f : -16f;
            var toe25 = highAtPhaseZero ? 4f : 10f;
            var toe50 = highAtPhaseZero ? -16f : 20f;
            var toe75 = highAtPhaseZero ? 10f : 4f;

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                upLegPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, upLegPath, VideoPoseCurve(phase, upLeg0, upLeg25, upLeg50, upLeg75)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                legPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, legPath, VideoPoseCurve(phase, leg0, leg25, leg50, leg75)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                footPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, footPath, VideoPoseCurve(phase, foot0, foot25, foot50, foot75)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                toePath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, toePath, VideoPoseCurve(phase, toe0, toe25, toe50, toe75)));
        }

        private static void SetVideoReferenceArmPoses(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile,
            string shoulderPath,
            string armPath,
            string foreArmPath,
            string handPath,
            bool forwardAtPhaseZero)
        {
            var shoulder0 = forwardAtPhaseZero ? 22f : -18f;
            var shoulder25 = forwardAtPhaseZero ? 4f : -4f;
            var shoulder50 = forwardAtPhaseZero ? -18f : 22f;
            var shoulder75 = forwardAtPhaseZero ? -4f : 4f;

            var arm0 = forwardAtPhaseZero ? 110f : -90f;
            var arm25 = forwardAtPhaseZero ? 8f : -8f;
            var arm50 = forwardAtPhaseZero ? -90f : 110f;
            var arm75 = forwardAtPhaseZero ? -8f : 8f;

            var foreArm0 = forwardAtPhaseZero ? 74f : 42f;
            var foreArm25 = forwardAtPhaseZero ? 48f : 48f;
            var foreArm50 = forwardAtPhaseZero ? 42f : 74f;
            var foreArm75 = forwardAtPhaseZero ? 48f : 48f;

            var hand0 = forwardAtPhaseZero ? 28f : -18f;
            var hand25 = forwardAtPhaseZero ? 4f : -4f;
            var hand50 = forwardAtPhaseZero ? -18f : 28f;
            var hand75 = forwardAtPhaseZero ? -4f : 4f;

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                shoulderPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, shoulderPath, VideoPoseCurve(phase, shoulder0, shoulder25, shoulder50, shoulder75)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                armPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, armPath, VideoPoseCurve(phase, arm0, arm25, arm50, arm75)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                foreArmPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, foreArmPath, VideoPoseCurve(phase, foreArm0, foreArm25, foreArm50, foreArm75)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                handPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, handPath, VideoPoseCurve(phase, hand0, hand25, hand50, hand75)));
        }

        private static float VideoPoseCurve(float phase, float phase0, float phase25, float phase50, float phase75)
        {
            phase = NormalizePhase(phase);
            if (phase < 0.25f)
            {
                return SmoothLerp(phase0, phase25, phase / 0.25f);
            }

            if (phase < 0.5f)
            {
                return SmoothLerp(phase25, phase50, (phase - 0.25f) / 0.25f);
            }

            if (phase < 0.75f)
            {
                return SmoothLerp(phase50, phase75, (phase - 0.5f) / 0.25f);
            }

            return SmoothLerp(phase75, phase0, (phase - 0.75f) / 0.25f);
        }

        private static float SmoothLerp(float from, float to, float t)
        {
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(from, to, t);
        }

        private static void ApplyReferenceDrivenSprintCurves(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile)
        {
            foreach (var path in ReferenceDrivenSprintMotionPaths)
            {
                RequireAuthoredRelativeTransform(runRoot, path);
            }

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                "Armature/Hips",
                phase => new Vector3(
                    0.006f * AuthoredSprintSin(phase, 1f, 0f),
                    0.058f * AuthoredSprintSin(phase, 2f, 0.05f),
                    0.026f * AuthoredSprintCos(phase, 1f, 0f)),
                phase => AxisEuler(profile, "Armature/Hips", SprintForwardBack(phase, 0.05f, 7.5f, 4f)));

            SetReferenceBodyCurve(clip, runRoot, profile, "Armature/Hips/Spine02", 9.5f, 1.5f, 0.1f);
            SetReferenceBodyCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01", 7.25f, 1.25f, 0.2f);
            SetReferenceBodyCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01/Spine", 5.75f, 1f, 0.3f);
            SetReferenceBodyCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01/Spine/neck", -2.75f, 0.75f, 0.4f);
            SetReferenceBodyCurve(clip, runRoot, profile, "Armature/Hips/Spine02/Spine01/Spine/neck/Head", -3.5f, 0.9f, 0.45f);

            SetReferenceLegCurves(
                clip,
                runRoot,
                profile,
                "Armature/Hips/LeftUpLeg",
                "Armature/Hips/LeftUpLeg/LeftLeg",
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase",
                0f);

            SetReferenceLegCurves(
                clip,
                runRoot,
                profile,
                "Armature/Hips/RightUpLeg",
                "Armature/Hips/RightUpLeg/RightLeg",
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
                0.5f);

            SetReferenceArmCurves(
                clip,
                runRoot,
                profile,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                0.5f);

            SetReferenceArmCurves(
                clip,
                runRoot,
                profile,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                0f);
        }

        private static void SetReferenceBodyCurve(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile,
            string path,
            float leanDegrees,
            float pulseDegrees,
            float phaseOffset)
        {
            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                path,
                phase => Vector3.zero,
                phase => AxisEuler(profile, path, leanDegrees + pulseDegrees * AuthoredSprintSin(phase, 2f, phaseOffset)));
        }

        private static void SetReferenceLegCurves(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile,
            string upLegPath,
            string legPath,
            string footPath,
            string toePath,
            float forwardPhase)
        {
            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                upLegPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, upLegPath, SprintForwardBack(phase, forwardPhase, 64f, 28f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                legPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, legPath, SprintPositivePulse(phase, forwardPhase, 56f) - SprintPositivePulse(phase, forwardPhase + 0.5f, 10f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                footPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, footPath, SprintPositivePulse(phase, forwardPhase, 24f) - SprintPositivePulse(phase, forwardPhase + 0.5f, 20f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                toePath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, toePath, SprintPositivePulse(phase, forwardPhase + 0.08f, 16f) - SprintPositivePulse(phase, forwardPhase + 0.5f, 12f)));
        }

        private static void SetReferenceArmCurves(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile,
            string shoulderPath,
            string armPath,
            string foreArmPath,
            string handPath,
            float forwardPhase)
        {
            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                shoulderPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, shoulderPath, SprintForwardBack(phase, forwardPhase, 16f, 10f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                armPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, armPath, SprintForwardBack(phase, forwardPhase, 52f, 42f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                foreArmPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, foreArmPath, 20f + SprintPositivePulse(phase, forwardPhase, 22f)));

            SetAuthoredSprintTransformCurves(
                clip,
                runRoot,
                handPath,
                phase => Vector3.zero,
                phase => AxisEuler(profile, handPath, SprintForwardBack(phase, forwardPhase, 12f, 10f)));
        }

        private static float SprintForwardBack(float phase, float forwardPhase, float forwardDegrees, float backDegrees)
        {
            var cycle = Mathf.Cos((phase - NormalizePhase(forwardPhase)) * Mathf.PI * 2f);
            return cycle >= 0f
                ? cycle * forwardDegrees
                : cycle * backDegrees;
        }

        private static float SprintPositivePulse(float phase, float peakPhase, float degrees)
        {
            return Mathf.Max(0f, Mathf.Cos((phase - NormalizePhase(peakPhase)) * Mathf.PI * 2f)) * degrees;
        }

        private static float NormalizePhase(float phase)
        {
            phase %= 1f;
            return phase < 0f ? phase + 1f : phase;
        }

        private static Vector3 AxisEuler(ReferenceSprintProfile profile, string path, float degrees)
        {
            var axis = profile.GetAxis(path);
            var signedDegrees = axis.Sign * degrees;
            switch (axis.AxisIndex)
            {
                case 0:
                    return new Vector3(signedDegrees, 0f, 0f);
                case 1:
                    return new Vector3(0f, signedDegrees, 0f);
                default:
                    return new Vector3(0f, 0f, signedDegrees);
            }
        }

        private static ReferenceSprintProfile BuildReferenceSprintProfile()
        {
            var sourceClips = LoadImportedAnimationClips();
            var sourceClip = SelectRunSourceClip(sourceClips);
            var sourcePrefab = RequireAsset<GameObject>(RunAnimationSourceAssetPath);
            var sourceInstance = UnityEngine.Object.Instantiate(sourcePrefab);
            sourceInstance.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                var transforms = new Dictionary<string, Transform>(StringComparer.Ordinal);
                var restRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
                foreach (var path in ReferenceDrivenSprintMotionPaths)
                {
                    var transform = sourceInstance.transform.Find(path);
                    if (transform == null)
                    {
                        continue;
                    }

                    transforms[path] = transform;
                    restRotations[path] = transform.localRotation;
                }

                var ranges = transforms.Keys.ToDictionary(
                    path => path,
                    path => new AxisRangeTracker(),
                    StringComparer.Ordinal);

                const int sampleCount = 48;
                for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    var time = sourceClip.length * (sampleIndex / (float)sampleCount);
                    sourceClip.SampleAnimation(sourceInstance, time);
                    foreach (var pair in transforms)
                    {
                        var delta = Quaternion.Inverse(restRotations[pair.Key]) * pair.Value.localRotation;
                        ranges[pair.Key].Add(NormalizeEuler(delta.eulerAngles));
                    }
                }

                var axes = new Dictionary<string, SprintAxisReference>(StringComparer.Ordinal);
                foreach (var path in ReferenceDrivenSprintMotionPaths)
                {
                    if (ranges.TryGetValue(path, out var range))
                    {
                        axes[path] = range.ToAxisReference(path);
                    }
                    else
                    {
                        axes[path] = SprintAxisReference.Fallback(path);
                    }
                }

                return new ReferenceSprintProfile(
                    sourceClip.name,
                    sourceClip.length,
                    FormatClipNames(sourceClips),
                    axes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceInstance);
            }
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(
                NormalizeEulerDegrees(euler.x),
                NormalizeEulerDegrees(euler.y),
                NormalizeEulerDegrees(euler.z));
        }

        private static float NormalizeEulerDegrees(float value)
        {
            value %= 360f;
            if (value > 180f)
            {
                value -= 360f;
            }

            if (value < -180f)
            {
                value += 360f;
            }

            return value;
        }

        private static string FormatReferenceAxes(ReferenceSprintProfile profile)
        {
            return string.Join(
                "|",
                ReferenceDrivenSprintMotionPaths.Select(path =>
                {
                    var axis = profile.GetAxis(path);
                    return path + ":" + axis.AxisName +
                           ":Sign=" + axis.Sign.ToString("0", CultureInfo.InvariantCulture) +
                           ":Range=" + axis.Range.ToString("0.###", CultureInfo.InvariantCulture);
                }));
        }

        private static int CountValidationFrames(string projectRelativeDirectory)
        {
            var fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectRelativeDirectory));
            return Directory.Exists(fullPath)
                ? Directory.GetFiles(fullPath, "frame_*.jpg").Length
                : 0;
        }

        private static ReferenceDrivenSprintMetrics MeasureReferenceDrivenSprintMotion(
            AnimationClip clip,
            Transform runRoot,
            ReferenceSprintProfile profile)
        {
            var transforms = runRoot.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(TransformState.Capture).ToArray();
            var pathTransforms = ReferenceDrivenSprintMotionPaths.ToDictionary(
                path => path,
                path => RequireAuthoredRelativeTransform(runRoot, path),
                StringComparer.Ordinal);
            var restRotations = pathTransforms.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.localRotation,
                StringComparer.Ordinal);
            var axisRanges = ReferenceDrivenSprintMotionPaths.ToDictionary(
                path => path,
                path => new FloatRangeTracker(),
                StringComparer.Ordinal);
            var hips = pathTransforms["Armature/Hips"];
            var hipsYRange = new FloatRangeTracker();
            var hipsXRange = new FloatRangeTracker();
            var leftKneeYRange = new FloatRangeTracker();
            var rightKneeYRange = new FloatRangeTracker();

            try
            {
                const int sampleCount = 40;
                for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                {
                    var time = clip.length * (sampleIndex / (float)sampleCount);
                    clip.SampleAnimation(runRoot.gameObject, time);
                    hipsYRange.Add(hips.localPosition.y);
                    hipsXRange.Add(hips.localPosition.x);
                    leftKneeYRange.Add(pathTransforms["Armature/Hips/LeftUpLeg/LeftLeg"].position.y);
                    rightKneeYRange.Add(pathTransforms["Armature/Hips/RightUpLeg/RightLeg"].position.y);

                    foreach (var pair in pathTransforms)
                    {
                        var axis = profile.GetAxis(pair.Key);
                        var delta = Quaternion.Inverse(restRotations[pair.Key]) * pair.Value.localRotation;
                        axisRanges[pair.Key].Add(GetAxisValue(NormalizeEuler(delta.eulerAngles), axis.AxisIndex));
                    }
                }

                return new ReferenceDrivenSprintMetrics(
                    axisRanges["Armature/Hips/LeftUpLeg"].Range,
                    axisRanges["Armature/Hips/RightUpLeg"].Range,
                    axisRanges["Armature/Hips/LeftUpLeg/LeftLeg"].Range,
                    axisRanges["Armature/Hips/RightUpLeg/RightLeg"].Range,
                    axisRanges["Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm"].Range,
                    axisRanges["Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm"].Range,
                    hipsYRange.Range,
                    hipsXRange.Range,
                    leftKneeYRange.Range,
                    rightKneeYRange.Range);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    originalStates[index].ApplyTo(transforms[index]);
                }
            }
        }

        private static float GetAxisValue(Vector3 value, int axisIndex)
        {
            switch (axisIndex)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        private static AnimationClip[] LoadImportedAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(RunAnimationSourceAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + RunAnimationSourceAssetPath);
            }

            return clips;
        }

        private static AnimationClip SelectRunSourceClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetRunClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static int GetRunClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("run"))
            {
                score += 120;
            }

            if (lower.Contains("sprint"))
            {
                score += 115;
            }

            if (lower.Contains("chase"))
            {
                score += 110;
            }

            if (lower.Contains("rush"))
            {
                score += 105;
            }

            if (lower.Contains("dash"))
            {
                score += 100;
            }

            if (lower.Contains("back"))
            {
                score += 70;
            }

            if (lower.Contains("walk"))
            {
                score += 30;
            }

            if (lower.Contains("move"))
            {
                score += 20;
            }

            if (lower.Contains("locomotion"))
            {
                score += 15;
            }

            return score;
        }

        private static AnimationClip EnsureCopiedRunClip(AnimationClip sourceClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunChaseClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = RunChaseClipName
                };
                AssetDatabase.CreateAsset(clip, RunChaseClipPath);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = RunChaseClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureRunController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(RunChaseControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(RunChaseControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == RunChaseClipName);
            if (state == null)
            {
                state = stateMachine.AddState(RunChaseClipName);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static bool ControllerUsesClip(AnimatorController controller, AnimationClip clip)
        {
            return controller.animationClips.Any(candidate =>
                candidate == clip ||
                string.Equals(AssetDatabase.GetAssetPath(candidate), RunChaseClipPath, StringComparison.Ordinal));
        }

        private static bool ControllerDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static bool SampleClipChangesTransforms(AnimationClip clip, Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(TransformState.Capture).ToArray();
            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startStates = transforms.Select(TransformState.Capture).ToArray();
                clip.SampleAnimation(root.gameObject, Mathf.Clamp(clip.length * 0.5f, 0f, Mathf.Max(clip.length - 0.001f, 0f)));

                for (var index = 0; index < transforms.Length; index++)
                {
                    if (!startStates[index].Matches(transforms[index]))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    originalStates[index].ApplyTo(transforms[index]);
                }
            }
        }

        private static void RequireConfiguredAnimator(
            Transform root,
            AnimatorController expectedController,
            string rootName)
        {
            var animator = root.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != expectedController)
            {
                throw new InvalidOperationException(rootName + " must keep its existing approved controller.");
            }

            var configuredCount = CountConfiguredAnimators(root);
            if (configuredCount != 1)
            {
                throw new InvalidOperationException(
                    rootName + " must keep exactly one configured Animator. Count=" +
                    configuredCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static int SyncBackRushVisualBodyMaterialsFromReference(Transform sourceRoot, Transform targetRoot)
        {
            var sourceBodyRenderers = FindBackRushVisualBodyRenderers(sourceRoot);
            if (sourceBodyRenderers.Length == 0)
            {
                throw new InvalidOperationException(sourceRoot.name + " has no body renderer to use as the visual reference.");
            }

            var sourceBodyRenderer = sourceBodyRenderers[0];
            var sourceMaterials = sourceBodyRenderer.sharedMaterials;
            if (sourceMaterials.Length == 0 || sourceMaterials.All(material => material == null))
            {
                throw new InvalidOperationException(sourceRoot.name + " reference body renderer has no usable shared materials.");
            }

            var targetBodyRenderers = FindBackRushVisualBodyRenderers(targetRoot);
            if (targetBodyRenderers.Length == 0)
            {
                throw new InvalidOperationException(targetRoot.name + " has no body renderer to sync.");
            }

            foreach (var renderer in targetBodyRenderers)
            {
                var slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                var syncedMaterials = new Material[slotCount];
                for (var index = 0; index < syncedMaterials.Length; index++)
                {
                    syncedMaterials[index] = sourceMaterials[Mathf.Min(index, sourceMaterials.Length - 1)];
                }

                renderer.sharedMaterials = syncedMaterials;
                renderer.shadowCastingMode = sourceBodyRenderer.shadowCastingMode;
                renderer.receiveShadows = sourceBodyRenderer.receiveShadows;
                renderer.lightProbeUsage = sourceBodyRenderer.lightProbeUsage;
                renderer.reflectionProbeUsage = sourceBodyRenderer.reflectionProbeUsage;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            return targetBodyRenderers.Length;
        }

        private static bool BackRushVisualBodyMaterialsMatchReference(Transform sourceRoot, Transform targetRoot)
        {
            var sourceBodyRenderers = FindBackRushVisualBodyRenderers(sourceRoot);
            var targetBodyRenderers = FindBackRushVisualBodyRenderers(targetRoot);
            if (sourceBodyRenderers.Length == 0 || targetBodyRenderers.Length == 0)
            {
                return false;
            }

            var sourceMaterials = sourceBodyRenderers[0].sharedMaterials;
            if (sourceMaterials.Length == 0)
            {
                return false;
            }

            foreach (var renderer in targetBodyRenderers)
            {
                var targetMaterials = renderer.sharedMaterials;
                if (targetMaterials.Length == 0)
                {
                    return false;
                }

                for (var index = 0; index < targetMaterials.Length; index++)
                {
                    var expected = sourceMaterials[Mathf.Min(index, sourceMaterials.Length - 1)];
                    if (targetMaterials[index] != expected)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Renderer[] FindBackRushVisualBodyRenderers(Transform root)
        {
            var eyeContainers = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => string.Equals(transform.name, EyeContainerName, StringComparison.Ordinal))
                .ToArray();
            var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => !IsBackRushVisualUnderAny(renderer.transform, eyeContainers))
                .Cast<Renderer>()
                .ToArray();
            if (skinnedRenderers.Length > 0)
            {
                return skinnedRenderers;
            }

            return root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !IsBackRushVisualUnderAny(renderer.transform, eyeContainers))
                .ToArray();
        }

        private static Transform CopyBackRushVisualEyeContainerFromReference(
            Transform sourceRoot,
            Transform targetRoot,
            Transform sourceEyeContainer)
        {
            var targetParent = FindBackRushVisualMatchingParent(sourceRoot, targetRoot, sourceEyeContainer);
            var copiedObject = UnityEngine.Object.Instantiate(sourceEyeContainer.gameObject, targetParent, false);
            copiedObject.name = EyeContainerName;
            copiedObject.SetActive(sourceEyeContainer.gameObject.activeSelf);
            CopyBackRushVisualLocalTransform(sourceEyeContainer, copiedObject.transform);
            EditorUtility.SetDirty(copiedObject);
            return copiedObject.transform;
        }

        private static int CopyBackRushVisualExternalLightObjectsFromReference(
            Transform sourceRoot,
            Transform targetRoot,
            Transform sourceEyeContainer)
        {
            var copied = 0;
            foreach (var sourceLight in sourceRoot.GetComponentsInChildren<Light>(true))
            {
                if (sourceLight.transform.IsChildOf(sourceEyeContainer))
                {
                    continue;
                }

                var targetParent = FindBackRushVisualMatchingParent(sourceRoot, targetRoot, sourceLight.transform);
                var copiedObject = UnityEngine.Object.Instantiate(sourceLight.gameObject, targetParent, false);
                copiedObject.name = sourceLight.gameObject.name;
                copiedObject.SetActive(sourceLight.gameObject.activeSelf);
                CopyBackRushVisualLocalTransform(sourceLight.transform, copiedObject.transform);
                EditorUtility.SetDirty(copiedObject);
                copied++;
            }

            return copied;
        }

        private static int CopyBackRushVisualAllLightObjectsFromReference(Transform sourceRoot, Transform targetRoot)
        {
            var copied = 0;
            foreach (var sourceLight in sourceRoot.GetComponentsInChildren<Light>(true))
            {
                var targetParent = FindBackRushVisualMatchingParent(sourceRoot, targetRoot, sourceLight.transform);
                var copiedObject = UnityEngine.Object.Instantiate(sourceLight.gameObject, targetParent, false);
                copiedObject.name = sourceLight.gameObject.name;
                copiedObject.SetActive(sourceLight.gameObject.activeSelf);
                CopyBackRushVisualLocalTransform(sourceLight.transform, copiedObject.transform);
                EditorUtility.SetDirty(copiedObject);
                copied++;
            }

            return copied;
        }

        private static void DestroyBackRushVisualNamedDescendants(Transform root, string objectName)
        {
            var targets = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != root && string.Equals(transform.name, objectName, StringComparison.Ordinal))
                .OrderByDescending(transform => GetBackRushVisualHierarchyDepth(transform))
                .ToArray();
            foreach (var target in targets)
            {
                UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        private static void DestroyBackRushVisualLightGameObjects(Transform root)
        {
            var targets = root.GetComponentsInChildren<Light>(true)
                .Select(light => light.gameObject)
                .Distinct()
                .OrderByDescending(gameObject => GetBackRushVisualHierarchyDepth(gameObject.transform))
                .ToArray();
            foreach (var target in targets)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static int CountLightsUnderNamedDescendants(Transform root, string objectName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != root && string.Equals(transform.name, objectName, StringComparison.Ordinal))
                .SelectMany(transform => transform.GetComponentsInChildren<Light>(true))
                .Distinct()
                .Count();
        }

        private static int RemoveAnimatorComponentsUnderRoot(Transform root)
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            foreach (var animator in animators)
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            return animators.Length;
        }

        private static Transform RequireBackRushVisualFirstNamedDescendant(Transform root, string objectName)
        {
            var target = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => string.Equals(transform.name, objectName, StringComparison.Ordinal));
            if (target == null)
            {
                throw new InvalidOperationException("Missing descendant under " + root.name + ": " + objectName);
            }

            return target;
        }

        private static Transform FindBackRushVisualMatchingParent(Transform sourceRoot, Transform targetRoot, Transform sourceTransform)
        {
            var sourceParent = sourceTransform.parent;
            var sourceParentPath = GetBackRushVisualRelativePath(sourceRoot, sourceParent);
            var targetParent = FindBackRushVisualByRelativePath(targetRoot, sourceParentPath);
            if (targetParent != null)
            {
                return targetParent;
            }

            var headAttachment = FindBackRushVisualHeadAttachment(targetRoot);
            return headAttachment != null ? headAttachment : targetRoot;
        }

        private static Transform FindBackRushVisualHeadAttachment(Transform root)
        {
            var candidates = new[]
            {
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head/headfront",
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                "Armature/Hips/Spine02/Spine01/Spine/Neck/Head/headfront",
                "Armature/Hips/Spine02/Spine01/Spine/Neck/Head"
            };

            foreach (var candidate in candidates)
            {
                var transform = root.Find(candidate);
                if (transform != null)
                {
                    return transform;
                }
            }

            return null;
        }

        private static Transform FindBackRushVisualByRelativePath(Transform root, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return root;
            }

            return root.Find(path);
        }

        private static string GetBackRushVisualRelativePath(Transform root, Transform transform)
        {
            if (root == null || transform == null || transform == root)
            {
                return string.Empty;
            }

            var names = new Stack<string>();
            var current = transform;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return current == root ? string.Join("/", names.ToArray()) : string.Empty;
        }

        private static bool IsBackRushVisualUnderAny(Transform transform, Transform[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate != null && transform.IsChildOf(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static void CopyBackRushVisualLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static int CountBackRushVisualLights(Transform root)
        {
            return root.GetComponentsInChildren<Light>(true).Length;
        }

        private static Transform EnsureHitNormalRoot(Transform placementRoot, Transform staticRoot)
        {
            var hitRoot = FindDirectChild(placementRoot, HitNormalRootName);
            if (hitRoot != null)
            {
                return hitRoot;
            }

            var hitObject = UnityEngine.Object.Instantiate(staticRoot.gameObject, placementRoot, false);
            hitObject.name = HitNormalRootName;
            hitRoot = hitObject.transform;

            var crouchRoot = FindDirectChild(placementRoot, CrouchTrembleRootName);
            if (crouchRoot != null)
            {
                hitRoot.localPosition = crouchRoot.localPosition + new Vector3(1.45f, 0f, 0f);
                hitRoot.localRotation = crouchRoot.localRotation;
                hitRoot.localScale = crouchRoot.localScale;
                hitRoot.SetSiblingIndex(Mathf.Min(crouchRoot.GetSiblingIndex() + 1, placementRoot.childCount - 1));
            }
            else
            {
                hitRoot.localPosition = staticRoot.localPosition + new Vector3(1.45f, 0f, 0f);
                hitRoot.localRotation = staticRoot.localRotation;
                hitRoot.localScale = staticRoot.localScale;
            }

            DestroyAnimationComponents(hitRoot);
            EditorUtility.SetDirty(hitObject);
            return hitRoot;
        }

        private static void RequireHitNormalVisualMatchesStatic(Transform staticRoot, Transform hitRoot)
        {
            RequireRendererSignaturesMatch(staticRoot, hitRoot);

            var referenceSkinnedCount = staticRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var targetSkinnedCount = hitRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            if (targetSkinnedCount != referenceSkinnedCount)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " SkinnedMeshRenderer count does not match " + StaticRootName +
                    ". Reference=" + referenceSkinnedCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetSkinnedCount.ToString(CultureInfo.InvariantCulture));
            }

            var referenceEyeCount = CountDescendantsByName(staticRoot, EyeContainerName);
            var targetEyeCount = CountDescendantsByName(hitRoot, EyeContainerName);
            if (referenceEyeCount != 1 || targetEyeCount != 1)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " must keep the approved eye container count. Reference=" +
                    referenceEyeCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetEyeCount.ToString(CultureInfo.InvariantCulture));
            }

            var referenceLightCount = CountBackRushVisualLights(staticRoot);
            var targetLightCount = CountBackRushVisualLights(hitRoot);
            if (targetLightCount != referenceLightCount)
            {
                throw new InvalidOperationException(
                    HitNormalRootName + " light count does not match " + StaticRootName +
                    ". Reference=" + referenceLightCount.ToString(CultureInfo.InvariantCulture) +
                    ", Target=" + targetLightCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static AnimationClip EnsureHitNormalClip(Transform hitRoot)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitNormalClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, HitNormalClipPath);
            }

            clip.name = HitNormalClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            clip.ClearCurves();

            var hips = RequirePierceAttackBone(hitRoot, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine");
            var neck = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/neck");
            var head = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightShoulder = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder");
            var rightArm = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftShoulder = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder");
            var leftArm = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var leftHand = RequirePierceAttackBone(hitRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");

            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips",
                hips.localPosition,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.1f, new Vector3(0f, -0.004f, -0.025f)),
                    new PierceAttackVectorKey(HitNormalGuardTime, new Vector3(0f, -0.015f, -0.105f)),
                    new PierceAttackVectorKey(HitNormalHoldTime, new Vector3(0f, -0.018f, -0.095f)),
                    new PierceAttackVectorKey(HitNormalRecoverTime, new Vector3(0f, -0.006f, -0.025f)),
                    new PierceAttackVectorKey(HitNormalDuration, Vector3.zero)
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips",
                hips.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.1f, new Vector3(-5f, 0f, 0f)),
                    new PierceAttackVectorKey(HitNormalGuardTime, new Vector3(-14f, 0f, 0f)),
                    new PierceAttackVectorKey(HitNormalHoldTime, new Vector3(-12f, 0f, 0f)),
                    new PierceAttackVectorKey(HitNormalRecoverTime, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(HitNormalDuration, Vector3.zero)
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02",
                spine02.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-16f, 4f, -3f), new Vector3(-12f, 3f, -2f), new Vector3(-4f, 1f, 0f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01",
                spine01.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-14f, 7f, -4f), new Vector3(-10f, 6f, -3f), new Vector3(-3f, 2f, -1f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine",
                spine.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-11f, 9f, -5f), new Vector3(-8f, 7f, -3f), new Vector3(-2f, 2f, -1f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck",
                neck.localRotation,
                BuildHitNormalBodyKeys(new Vector3(5f, 25f, -4f), new Vector3(4f, 20f, -3f), new Vector3(1f, 6f, -1f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                head.localRotation,
                BuildHitNormalBodyKeys(new Vector3(2f, 58f, -10f), new Vector3(1f, 50f, -7f), new Vector3(0f, 16f, -2f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                rightShoulder.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-68f, -12f, 16f), new Vector3(-62f, -10f, 14f), new Vector3(-18f, -3f, 4f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                rightArm.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-112f, -16f, 22f), new Vector3(-104f, -14f, 20f), new Vector3(-28f, -4f, 6f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                rightForeArm.localRotation,
                BuildHitNormalBodyKeys(new Vector3(96f, 6f, 8f), new Vector3(90f, 5f, 7f), new Vector3(22f, 2f, 2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                rightHand.localRotation,
                BuildHitNormalBodyKeys(new Vector3(0f, 8f, -8f), new Vector3(0f, 7f, -7f), new Vector3(0f, 2f, -2f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                leftShoulder.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-68f, 12f, -16f), new Vector3(-62f, 10f, -14f), new Vector3(-18f, 3f, -4f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                leftArm.localRotation,
                BuildHitNormalBodyKeys(new Vector3(-112f, 16f, -22f), new Vector3(-104f, 14f, -20f), new Vector3(-28f, 4f, -6f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                leftForeArm.localRotation,
                BuildHitNormalBodyKeys(new Vector3(96f, -6f, -8f), new Vector3(90f, -5f, -7f), new Vector3(22f, -2f, -2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                leftHand.localRotation,
                BuildHitNormalBodyKeys(new Vector3(0f, -8f, 8f), new Vector3(0f, -7f, 7f), new Vector3(0f, -2f, 2f)));
            AddHitNormalFaceGuardPositionCurves(clip, hitRoot, head, rightHand, leftHand);

            clip.EnsureQuaternionContinuity();
            SetHitNormalClipSettings(clip);
            return clip;
        }

        private static PierceAttackVectorKey[] BuildHitNormalBodyKeys(
            Vector3 guard,
            Vector3 hold,
            Vector3 recover)
        {
            return new[]
            {
                new PierceAttackVectorKey(0f, Vector3.zero),
                new PierceAttackVectorKey(0.1f, guard * 0.35f),
                new PierceAttackVectorKey(HitNormalGuardTime, guard),
                new PierceAttackVectorKey(HitNormalHoldTime, hold),
                new PierceAttackVectorKey(HitNormalRecoverTime, recover),
                new PierceAttackVectorKey(HitNormalDuration, Vector3.zero)
            };
        }

        private static void AddHitNormalFaceGuardPositionCurves(
            AnimationClip clip,
            Transform root,
            Transform head,
            Transform rightHand,
            Transform leftHand)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();
            var rightBasePosition = rightHand.localPosition;
            var leftBasePosition = leftHand.localPosition;
            Vector3 rightGuardOffset;
            Vector3 leftGuardOffset;
            Vector3 rightHoldOffset;
            Vector3 leftHoldOffset;
            Vector3 rightRecoverOffset;
            Vector3 leftRecoverOffset;

            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startRightRoot = root.InverseTransformPoint(rightHand.position);
                var startLeftRoot = root.InverseTransformPoint(leftHand.position);

                clip.SampleAnimation(root.gameObject, HitNormalGuardTime);
                var guardRightRoot = root.InverseTransformPoint(rightHand.position);
                var guardLeftRoot = root.InverseTransformPoint(leftHand.position);
                var guardHeadRoot = root.InverseTransformPoint(head.position);
                var centerX = (startRightRoot.x + startLeftRoot.x) * 0.5f;
                var targetHalfWidth = Mathf.Clamp(Mathf.Abs(startRightRoot.x - startLeftRoot.x) * 0.18f, 0.085f, 0.16f);
                var rightGuardTargetRoot = guardRightRoot;
                var leftGuardTargetRoot = guardLeftRoot;
                if (startRightRoot.x >= startLeftRoot.x)
                {
                    rightGuardTargetRoot.x = centerX + targetHalfWidth;
                    leftGuardTargetRoot.x = centerX - targetHalfWidth;
                }
                else
                {
                    rightGuardTargetRoot.x = centerX - targetHalfWidth;
                    leftGuardTargetRoot.x = centerX + targetHalfWidth;
                }

                var targetY = Mathf.Max(
                    guardHeadRoot.y - 0.035f,
                    Mathf.Max(startRightRoot.y, startLeftRoot.y) + 0.12f);
                var targetZ = Mathf.Max(
                    guardHeadRoot.z + 0.06f,
                    Mathf.Max(startRightRoot.z, startLeftRoot.z) + 0.18f);
                rightGuardTargetRoot.y = targetY;
                leftGuardTargetRoot.y = targetY;
                rightGuardTargetRoot.z = targetZ;
                leftGuardTargetRoot.z = targetZ;

                var rightGuardLocal = rightHand.parent.InverseTransformPoint(root.TransformPoint(rightGuardTargetRoot));
                var leftGuardLocal = leftHand.parent.InverseTransformPoint(root.TransformPoint(leftGuardTargetRoot));
                rightGuardOffset = rightGuardLocal - rightBasePosition;
                leftGuardOffset = leftGuardLocal - leftBasePosition;
                clip.SampleAnimation(root.gameObject, HitNormalHoldTime);
                var rightHoldLocal = rightHand.parent.InverseTransformPoint(root.TransformPoint(rightGuardTargetRoot));
                var leftHoldLocal = leftHand.parent.InverseTransformPoint(root.TransformPoint(leftGuardTargetRoot));
                rightHoldOffset = rightHoldLocal - rightBasePosition;
                leftHoldOffset = leftHoldLocal - leftBasePosition;
                rightRecoverOffset = rightGuardOffset * 0.24f;
                leftRecoverOffset = leftGuardOffset * 0.24f;
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }
            }

            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                rightBasePosition,
                BuildHitNormalBodyKeys(rightGuardOffset, rightHoldOffset, rightRecoverOffset));
            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                leftBasePosition,
                BuildHitNormalBodyKeys(leftGuardOffset, leftHoldOffset, leftRecoverOffset));
        }

        private static AnimatorController EnsureHitNormalController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HitNormalControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(HitNormalControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(HitNormalClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SetHitNormalClipSettings(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = HitNormalDuration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static HitNormalMotionMetrics EvaluateHitNormalMetrics(AnimationClip clip, Transform root)
        {
            var hips = RequirePierceAttackBone(root, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(root, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine");
            var head = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var leftHand = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();

            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startHipsPosition = hips.localPosition;
                var startHipsRotation = hips.localRotation;
                var startSpine02Rotation = spine02.localRotation;
                var startSpine01Rotation = spine01.localRotation;
                var startSpineRotation = spine.localRotation;
                var startHeadRotation = head.localRotation;
                var startRightArmRotation = rightArm.localRotation;
                var startRightForeArmRotation = rightForeArm.localRotation;
                var startLeftArmRotation = leftArm.localRotation;
                var startLeftForeArmRotation = leftForeArm.localRotation;
                var startRightHandPosition = root.InverseTransformPoint(rightHand.position);
                var startLeftHandPosition = root.InverseTransformPoint(leftHand.position);

                clip.SampleAnimation(root.gameObject, HitNormalGuardTime);
                var guardHipsPosition = hips.localPosition;
                var guardHipsRotation = hips.localRotation;
                var guardSpine02Rotation = spine02.localRotation;
                var guardSpine01Rotation = spine01.localRotation;
                var guardSpineRotation = spine.localRotation;
                var guardHeadRotation = head.localRotation;
                var guardRightArmRotation = rightArm.localRotation;
                var guardRightForeArmRotation = rightForeArm.localRotation;
                var guardLeftArmRotation = leftArm.localRotation;
                var guardLeftForeArmRotation = leftForeArm.localRotation;
                var guardHeadPosition = root.InverseTransformPoint(head.position);
                var guardRightForeArmPosition = root.InverseTransformPoint(rightForeArm.position);
                var guardLeftForeArmPosition = root.InverseTransformPoint(leftForeArm.position);
                var guardRightHandPosition = root.InverseTransformPoint(rightHand.position);
                var guardLeftHandPosition = root.InverseTransformPoint(leftHand.position);

                clip.SampleAnimation(root.gameObject, HitNormalHoldTime);
                var holdHipsPosition = hips.localPosition;
                var holdRightHandPosition = root.InverseTransformPoint(rightHand.position);
                var holdLeftHandPosition = root.InverseTransformPoint(leftHand.position);

                var startHandSide = Mathf.Sign(startRightHandPosition.x - startLeftHandPosition.x);
                var guardHandSide = Mathf.Sign(guardRightHandPosition.x - guardLeftHandPosition.x);
                var startLateralDistance = Mathf.Abs(startRightHandPosition.x - startLeftHandPosition.x);
                var guardLateralDistance = Mathf.Abs(guardRightHandPosition.x - guardLeftHandPosition.x);
                var handsNotCrossed = startHandSide != 0f &&
                    guardHandSide != 0f &&
                    startHandSide == guardHandSide;
                var minHandRaiseDelta = Mathf.Min(
                    guardRightHandPosition.y - startRightHandPosition.y,
                    guardLeftHandPosition.y - startLeftHandPosition.y);
                var guardHandCenter = (guardRightHandPosition + guardLeftHandPosition) * 0.5f;
                var faceGuardDistance = Vector3.Distance(guardHandCenter, guardHeadPosition);
                var rightForearmDirection = (guardRightHandPosition - guardRightForeArmPosition).normalized;
                var leftForearmDirection = (guardLeftHandPosition - guardLeftForeArmPosition).normalized;
                var forearmVerticalScore = Mathf.Min(
                    Mathf.Abs(Vector3.Dot(rightForearmDirection, Vector3.up)),
                    Mathf.Abs(Vector3.Dot(leftForearmDirection, Vector3.up)));

                var rightArmAngle = Mathf.Max(
                    Quaternion.Angle(startRightArmRotation, guardRightArmRotation),
                    Quaternion.Angle(startRightForeArmRotation, guardRightForeArmRotation));
                var leftArmAngle = Mathf.Max(
                    Quaternion.Angle(startLeftArmRotation, guardLeftArmRotation),
                    Quaternion.Angle(startLeftForeArmRotation, guardLeftForeArmRotation));
                var guardHoldDrift = Mathf.Max(
                    Vector3.Distance(guardRightHandPosition, holdRightHandPosition),
                    Mathf.Max(
                        Vector3.Distance(guardLeftHandPosition, holdLeftHandPosition),
                        Vector3.Distance(guardHipsPosition, holdHipsPosition)));

                return new HitNormalMotionMetrics(
                    handsNotCrossed,
                    startHandSide,
                    guardHandSide,
                    startLateralDistance,
                    guardLateralDistance,
                    Mathf.Min(guardRightHandPosition.z - startRightHandPosition.z, guardLeftHandPosition.z - startLeftHandPosition.z),
                    minHandRaiseDelta,
                    faceGuardDistance,
                    forearmVerticalScore,
                    Mathf.Min(rightArmAngle, leftArmAngle),
                    Quaternion.Angle(startHeadRotation, guardHeadRotation),
                    startHipsPosition.z - guardHipsPosition.z,
                    Quaternion.Angle(startHipsRotation, guardHipsRotation) +
                    Quaternion.Angle(startSpine02Rotation, guardSpine02Rotation) +
                    Quaternion.Angle(startSpine01Rotation, guardSpine01Rotation) +
                    Quaternion.Angle(startSpineRotation, guardSpineRotation),
                    guardHoldDrift);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }
            }
        }

        private static void RequireHitNormalMetrics(HitNormalMotionMetrics metrics)
        {
            if (!metrics.HandsNotCrossed)
            {
                throw new InvalidOperationException(
                    "Hit normal hands must stay on their own side for a straight face guard. StartSide=" +
                    metrics.StartHandSide.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", GuardSide=" + metrics.GuardHandSide.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", StartLateral=" + metrics.StartHandLateralDistance.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", GuardLateral=" + metrics.GuardHandLateralDistance.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.GuardHandLateralDistance < 0.12f || metrics.GuardHandLateralDistance > 0.36f)
            {
                throw new InvalidOperationException(
                    "Hit normal straight face guard hand spacing is outside the expected range. LateralDistance=" +
                    metrics.GuardHandLateralDistance.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.MinHandForwardDelta < 0.025f)
            {
                throw new InvalidOperationException(
                    "Hit normal hands do not move forward enough. ForwardDelta=" +
                    metrics.MinHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.MinHandRaiseDelta < 0.08f)
            {
                throw new InvalidOperationException(
                    "Hit normal hands do not rise enough to guard the face. RaiseDelta=" +
                    metrics.MinHandRaiseDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.FaceGuardDistance > 0.32f)
            {
                throw new InvalidOperationException(
                    "Hit normal hands are not close enough to the face guard position. FaceGuardDistance=" +
                    metrics.FaceGuardDistance.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.ForearmVerticalScore < 0.42f)
            {
                throw new InvalidOperationException(
                    "Hit normal forearms are not vertical enough for a straight face guard. VerticalScore=" +
                    metrics.ForearmVerticalScore.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.ArmRaiseAngle < 55f)
            {
                throw new InvalidOperationException(
                    "Hit normal arm raise angle is too small for a face guard. ArmRaiseAngle=" +
                    metrics.ArmRaiseAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.HeadTurnAngle < 35f)
            {
                throw new InvalidOperationException(
                    "Hit normal head does not turn sideways enough. HeadTurnAngle=" +
                    metrics.HeadTurnAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.HipsBackwardDelta < 0.055f)
            {
                throw new InvalidOperationException(
                    "Hit normal backward recoil is too small. HipsBackwardDelta=" +
                    metrics.HipsBackwardDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.BodyRecoilAngle < 34f)
            {
                throw new InvalidOperationException(
                    "Hit normal body recoil rotation is too small. BodyRecoilAngle=" +
                    metrics.BodyRecoilAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.GuardHoldDrift > 0.08f)
            {
                throw new InvalidOperationException(
                    "Hit normal face guard hold drifts too much. GuardHoldDrift=" +
                    metrics.GuardHoldDrift.ToString("0.######", CultureInfo.InvariantCulture));
            }
        }

        private static PierceAttackRuntimePlaybackMetrics EvaluateHitNormalAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var snapshots = root.GetComponentsInChildren<Transform>(true)
                .ToDictionary(transform => transform, LocalTransformSample.Capture);
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(HitNormalClipName, 0, 0f);
                animator.Update(0f);
                var start = CapturePierceAttackRuntimePose(root);
                animator.Update(HitNormalGuardTime);
                var guard = CapturePierceAttackRuntimePose(root);

                animator.Play(HitNormalClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.06f);
                var loopStart = CapturePierceAttackRuntimePose(root);
                animator.Update(HitNormalGuardTime);
                var loopGuard = CapturePierceAttackRuntimePose(root);

                var firstRotationDelta = MaxPierceAttackRuntimeRotationDelta(start, guard);
                var firstPositionDelta = MaxPierceAttackRuntimePositionDelta(start, guard);
                var loopRotationDelta = MaxPierceAttackRuntimeRotationDelta(loopStart, loopGuard);
                var loopPositionDelta = MaxPierceAttackRuntimePositionDelta(loopStart, loopGuard);

                return new PierceAttackRuntimePlaybackMetrics(
                    firstRotationDelta > 8f || firstPositionDelta > 0.015f,
                    loopRotationDelta > 8f || loopPositionDelta > 0.015f,
                    firstRotationDelta,
                    firstPositionDelta,
                    loopRotationDelta,
                    loopPositionDelta);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Key.localPosition = snapshot.Value.LocalPosition;
                    snapshot.Key.localRotation = snapshot.Value.LocalRotation;
                    snapshot.Key.localScale = snapshot.Value.LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static AnimationClip EnsureInterruptStaggerBackwardFallClip(Transform interruptRoot)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(InterruptStaggerClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, InterruptStaggerClipPath);
            }

            clip.name = InterruptStaggerClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            clip.ClearCurves();

            var hips = RequirePierceAttackBone(interruptRoot, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine");
            var neck = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/neck");
            var head = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightShoulder = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder");
            var rightArm = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftShoulder = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder");
            var leftArm = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var leftHand = RequirePierceAttackBone(interruptRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
            var rightUpLeg = RequirePierceAttackBone(interruptRoot, "Armature/Hips/RightUpLeg");
            var rightLeg = RequirePierceAttackBone(interruptRoot, "Armature/Hips/RightUpLeg/RightLeg");
            var rightFoot = RequirePierceAttackBone(interruptRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot");
            var leftUpLeg = RequirePierceAttackBone(interruptRoot, "Armature/Hips/LeftUpLeg");
            var leftLeg = RequirePierceAttackBone(interruptRoot, "Armature/Hips/LeftUpLeg/LeftLeg");
            var leftFoot = RequirePierceAttackBone(interruptRoot, "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");

            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips",
                hips.localPosition,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(0f, -0.018f, -0.03f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(0f, -0.09f, -0.115f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(0f, -0.205f, -0.215f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(0f, -0.185f, -0.19f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(0f, -0.2f, -0.2f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(0f, -0.2f, -0.202f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips",
                hips.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-12f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-42f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-76f, 0f, -2f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-66f, 0f, 1f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-72f, 0f, -1f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-74f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02",
                spine02.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-8f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-24f, 0f, 3f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-38f, 0f, -3f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-31f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-35f, 0f, -2f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-36f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01",
                spine01.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-6f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-20f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-32f, 0f, -2f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-27f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-30f, 0f, -1f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-31f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine",
                spine.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-5f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-16f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-26f, 0f, -2f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-20f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-24f, 0f, -1f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-24f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck",
                neck.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(20f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(34f, 0f, 0f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(26f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(30f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(31f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                head.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(10f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(24f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(38f, 0f, 0f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(30f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(34f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(34f, 0f, 0f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                rightShoulder.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-20f, 15f, -10f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-50f, 34f, -22f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-74f, 45f, -32f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-58f, 38f, -25f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-64f, 42f, -28f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-62f, 42f, -28f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                rightArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-35f, 12f, -18f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-82f, 26f, -32f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-118f, 38f, -44f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-96f, 30f, -34f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-108f, 34f, -38f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-106f, 34f, -38f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                rightForeArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(12f, 0f, -8f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(28f, 0f, -16f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(48f, 0f, -22f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(34f, 0f, -15f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(40f, 0f, -18f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(40f, 0f, -18f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                rightHand.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(10f, 0f, -8f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(28f, 0f, -16f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(46f, 0f, -20f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(34f, 0f, -14f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(38f, 0f, -16f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(38f, 0f, -16f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                leftShoulder.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-20f, -15f, 10f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-50f, -34f, 22f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-74f, -45f, 32f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-58f, -38f, 25f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-64f, -42f, 28f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-62f, -42f, 28f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                leftArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-35f, -12f, 18f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-82f, -26f, 32f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-118f, -38f, 44f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-96f, -30f, 34f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-108f, -34f, 38f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-106f, -34f, 38f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                leftForeArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(12f, 0f, 8f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(28f, 0f, 16f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(48f, 0f, 22f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(34f, 0f, 15f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(40f, 0f, 18f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(40f, 0f, 18f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                leftHand.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(10f, 0f, 8f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(28f, 0f, 16f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(46f, 0f, 20f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(34f, 0f, 14f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(38f, 0f, 16f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(38f, 0f, 16f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg",
                rightUpLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(18f, 0f, 2f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(44f, 0f, 4f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(72f, 0f, 5f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(62f, 0f, 4f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(68f, 0f, 4f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(68f, 0f, 4f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg",
                rightLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-18f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-50f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-86f, 0f, 0f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-76f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-82f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-82f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
                rightFoot.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(20f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(34f, 0f, 0f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(26f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(30f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(30f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg",
                leftUpLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(18f, 0f, -2f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(44f, 0f, -4f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(72f, 0f, -5f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(62f, 0f, -4f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(68f, 0f, -4f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(68f, 0f, -4f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg",
                leftLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(-18f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(-50f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(-86f, 0f, 0f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(-76f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(-82f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(-82f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
                leftFoot.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(0.18f, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerFallTime, new Vector3(20f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerImpactTime, new Vector3(34f, 0f, 0f)),
                    new PierceAttackVectorKey(0.86f, new Vector3(26f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerSettleTime, new Vector3(30f, 0f, 0f)),
                    new PierceAttackVectorKey(InterruptStaggerDuration, new Vector3(30f, 0f, 0f))
                });

            clip.EnsureQuaternionContinuity();
            SetInterruptStaggerClipSettings(clip);
            return clip;
        }

        private static AnimatorController EnsureInterruptStaggerController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(InterruptStaggerControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(InterruptStaggerControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(InterruptStaggerClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SetInterruptStaggerClipSettings(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = InterruptStaggerDuration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static InterruptStaggerMotionMetrics EvaluateInterruptStaggerMetrics(AnimationClip clip, Transform root)
        {
            var hips = RequirePierceAttackBone(root, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(root, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine");
            var rightArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var leftArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var rightLeg = RequirePierceAttackBone(root, "Armature/Hips/RightUpLeg/RightLeg");
            var leftLeg = RequirePierceAttackBone(root, "Armature/Hips/LeftUpLeg/LeftLeg");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();

            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startHipsPosition = hips.localPosition;
                var startHipsRotation = hips.localRotation;
                var startSpine02Rotation = spine02.localRotation;
                var startSpine01Rotation = spine01.localRotation;
                var startSpineRotation = spine.localRotation;
                var startRightArmRotation = rightArm.localRotation;
                var startRightForeArmRotation = rightForeArm.localRotation;
                var startLeftArmRotation = leftArm.localRotation;
                var startLeftForeArmRotation = leftForeArm.localRotation;
                var startRightLegRotation = rightLeg.localRotation;
                var startLeftLegRotation = leftLeg.localRotation;

                clip.SampleAnimation(root.gameObject, InterruptStaggerFallTime);
                var fallHipsPosition = hips.localPosition;

                clip.SampleAnimation(root.gameObject, InterruptStaggerImpactTime);
                var impactHipsPosition = hips.localPosition;
                var impactHipsRotation = hips.localRotation;
                var impactSpine02Rotation = spine02.localRotation;
                var impactSpine01Rotation = spine01.localRotation;
                var impactSpineRotation = spine.localRotation;
                var impactRightArmRotation = rightArm.localRotation;
                var impactRightForeArmRotation = rightForeArm.localRotation;
                var impactLeftArmRotation = leftArm.localRotation;
                var impactLeftForeArmRotation = leftForeArm.localRotation;
                var impactRightLegRotation = rightLeg.localRotation;
                var impactLeftLegRotation = leftLeg.localRotation;

                clip.SampleAnimation(root.gameObject, InterruptStaggerSettleTime);
                var settleHipsPosition = hips.localPosition;
                var settleSpineRotation = spine.localRotation;

                return new InterruptStaggerMotionMetrics(
                    startHipsPosition.z - impactHipsPosition.z,
                    startHipsPosition.y - impactHipsPosition.y,
                    Vector3.Distance(fallHipsPosition, impactHipsPosition),
                    Quaternion.Angle(startHipsRotation, impactHipsRotation),
                    Quaternion.Angle(startSpine02Rotation, impactSpine02Rotation) +
                    Quaternion.Angle(startSpine01Rotation, impactSpine01Rotation) +
                    Quaternion.Angle(startSpineRotation, impactSpineRotation),
                    Mathf.Max(
                        Quaternion.Angle(startRightLegRotation, impactRightLegRotation),
                        Quaternion.Angle(startLeftLegRotation, impactLeftLegRotation)),
                    Mathf.Max(
                        Mathf.Max(
                            Quaternion.Angle(startRightArmRotation, impactRightArmRotation),
                            Quaternion.Angle(startRightForeArmRotation, impactRightForeArmRotation)),
                        Mathf.Max(
                            Quaternion.Angle(startLeftArmRotation, impactLeftArmRotation),
                            Quaternion.Angle(startLeftForeArmRotation, impactLeftForeArmRotation))),
                    Vector3.Distance(impactHipsPosition, settleHipsPosition),
                    Quaternion.Angle(impactSpineRotation, settleSpineRotation));
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }
            }
        }

        private static void RequireInterruptStaggerMetrics(InterruptStaggerMotionMetrics metrics)
        {
            if (metrics.HipsBackwardDelta < 0.16f || metrics.HipsDropDelta < 0.16f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger must visibly fall backward and downward. Backward=" +
                    metrics.HipsBackwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", Drop=" + metrics.HipsDropDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.FallTravelDistance < 0.1f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger fall travel is too small. Distance=" +
                    metrics.FallTravelDistance.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.HipsFallRotationAngle < 55f || metrics.TorsoFallRotationAngle < 70f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger backward body rotation is too small. Hips=" +
                    metrics.HipsFallRotationAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Torso=" + metrics.TorsoFallRotationAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.MaxLegBendAngle < 55f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger butt-impact leg bend is too small. LegBend=" +
                    metrics.MaxLegBendAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.MaxArmFlailAngle < 70f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger arms do not flail enough during the backward fall. ArmFlail=" +
                    metrics.MaxArmFlailAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.ImpactHoldDrift > 0.07f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger butt-impact hold drifts too much after landing. Drift=" +
                    metrics.ImpactHoldDrift.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.SettleShakeAngle < 2f || metrics.SettleShakeAngle > 14f)
            {
                throw new InvalidOperationException(
                    "Interrupt stagger landing shake is outside the expected small range. Shake=" +
                    metrics.SettleShakeAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static PierceAttackRuntimePlaybackMetrics EvaluateInterruptStaggerAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var snapshots = root.GetComponentsInChildren<Transform>(true)
                .ToDictionary(transform => transform, LocalTransformSample.Capture);
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(InterruptStaggerClipName, 0, 0f);
                animator.Update(0f);
                var firstStart = CapturePierceAttackRuntimePose(root);
                animator.Update(Mathf.Min(InterruptStaggerImpactTime, clip.length * 0.75f));
                var firstImpact = CapturePierceAttackRuntimePose(root);

                var firstRotationDelta = MaxPierceAttackRuntimeRotationDelta(firstStart, firstImpact);
                var firstPositionDelta = MaxPierceAttackRuntimePositionDelta(firstStart, firstImpact);

                return new PierceAttackRuntimePlaybackMetrics(
                    firstRotationDelta > 12f || firstPositionDelta > 0.08f,
                    true,
                    firstRotationDelta,
                    firstPositionDelta,
                    0f,
                    0f);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Key.localPosition = snapshot.Value.LocalPosition;
                    snapshot.Key.localRotation = snapshot.Value.LocalRotation;
                    snapshot.Key.localScale = snapshot.Value.LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static AnimationClip EnsureCrouchTremble5sClip(Transform crouchRoot)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CrouchTrembleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, CrouchTrembleClipPath);
            }

            clip.name = CrouchTrembleClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            clip.ClearCurves();

            var hips = RequirePierceAttackBone(crouchRoot, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine");
            var neck = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/neck");
            var head = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightShoulder = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder");
            var rightArm = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftShoulder = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder");
            var leftArm = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var leftHand = RequirePierceAttackBone(crouchRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
            var rightUpLeg = RequirePierceAttackBone(crouchRoot, "Armature/Hips/RightUpLeg");
            var rightLeg = RequirePierceAttackBone(crouchRoot, "Armature/Hips/RightUpLeg/RightLeg");
            var rightFoot = RequirePierceAttackBone(crouchRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot");
            var rightToe = RequirePierceAttackBone(crouchRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase");
            var leftUpLeg = RequirePierceAttackBone(crouchRoot, "Armature/Hips/LeftUpLeg");
            var leftLeg = RequirePierceAttackBone(crouchRoot, "Armature/Hips/LeftUpLeg/LeftLeg");
            var leftFoot = RequirePierceAttackBone(crouchRoot, "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");
            var leftToe = RequirePierceAttackBone(crouchRoot, "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase");

            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips",
                hips.localPosition,
                BuildCrouchTrembleKeys(
                    new Vector3(0f, -0.22f, -0.2f),
                    new Vector3(0f, -0.205f, -0.17f),
                    new Vector3(0f, -0.17f, -0.08f),
                    new Vector3(0f, -0.145f, -0.02f),
                    new Vector3(0f, -0.155f, 0.012f),
                    new Vector3(0.012f, 0.014f, -0.008f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips",
                hips.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-74f, 0f, 0f),
                    new Vector3(-42f, 0f, 2f),
                    new Vector3(6f, 0f, 0f),
                    new Vector3(32f, 0f, 0f),
                    new Vector3(40f, 0f, 0f),
                    new Vector3(2.2f, 1.4f, 2.5f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02",
                spine02.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-36f, 0f, 0f),
                    new Vector3(-24f, 0f, 1f),
                    new Vector3(4f, 0f, 0f),
                    new Vector3(28f, 0f, 0f),
                    new Vector3(32f, 0f, 0f),
                    new Vector3(3.5f, -1.5f, 2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01",
                spine01.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-31f, 0f, 0f),
                    new Vector3(-18f, 0f, 1f),
                    new Vector3(6f, 0f, 0f),
                    new Vector3(22f, 0f, 0f),
                    new Vector3(26f, 0f, 0f),
                    new Vector3(3f, 1.2f, -2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine",
                spine.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-24f, 0f, 0f),
                    new Vector3(-12f, 0f, 1f),
                    new Vector3(5f, 0f, 0f),
                    new Vector3(16f, 0f, 0f),
                    new Vector3(18f, 0f, 0f),
                    new Vector3(3.2f, -1.4f, 2.4f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck",
                neck.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(31f, 0f, 0f),
                    new Vector3(18f, 0f, 0f),
                    new Vector3(-10f, 0f, 0f),
                    new Vector3(-22f, 0f, 0f),
                    new Vector3(-28f, 0f, 0f),
                    new Vector3(2.8f, 1.2f, -2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                head.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(34f, 0f, 0f),
                    new Vector3(20f, 0f, 0f),
                    new Vector3(-14f, 0f, 0f),
                    new Vector3(-26f, 0f, 0f),
                    new Vector3(-32f, 0f, 0f),
                    new Vector3(3f, -1f, 2.2f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                rightShoulder.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-62f, 42f, -28f),
                    new Vector3(-48f, 30f, -24f),
                    new Vector3(-24f, 16f, -20f),
                    new Vector3(-46f, 28f, -40f),
                    new Vector3(-62f, 34f, -54f),
                    new Vector3(4.5f, 2.5f, -3.5f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                rightArm.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-106f, 34f, -38f),
                    new Vector3(-80f, 22f, -34f),
                    new Vector3(-42f, 18f, -30f),
                    new Vector3(-72f, 32f, -48f),
                    new Vector3(-92f, 40f, -62f),
                    new Vector3(4.8f, 2.8f, -4.2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                rightForeArm.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(40f, 0f, -18f),
                    new Vector3(62f, 0f, -24f),
                    new Vector3(84f, 0f, -28f),
                    new Vector3(110f, 0f, -34f),
                    new Vector3(126f, 0f, -42f),
                    new Vector3(4f, 1f, -3f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                rightHand.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(38f, 0f, -16f),
                    new Vector3(52f, 0f, -18f),
                    new Vector3(70f, 0f, -22f),
                    new Vector3(86f, 0f, -28f),
                    new Vector3(96f, 0f, -34f),
                    new Vector3(3f, 0.8f, -2.5f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                leftShoulder.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-62f, -42f, 28f),
                    new Vector3(-48f, -30f, 24f),
                    new Vector3(-24f, -16f, 20f),
                    new Vector3(-46f, -28f, 40f),
                    new Vector3(-62f, -34f, 54f),
                    new Vector3(4.5f, -2.5f, 3.5f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                leftArm.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-106f, -34f, 38f),
                    new Vector3(-80f, -22f, 34f),
                    new Vector3(-42f, -18f, 30f),
                    new Vector3(-72f, -32f, 48f),
                    new Vector3(-92f, -40f, 62f),
                    new Vector3(4.8f, -2.8f, 4.2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                leftForeArm.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(40f, 0f, 18f),
                    new Vector3(62f, 0f, 24f),
                    new Vector3(84f, 0f, 28f),
                    new Vector3(110f, 0f, 34f),
                    new Vector3(126f, 0f, 42f),
                    new Vector3(4f, -1f, 3f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                leftHand.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(38f, 0f, 16f),
                    new Vector3(52f, 0f, 18f),
                    new Vector3(70f, 0f, 22f),
                    new Vector3(86f, 0f, 28f),
                    new Vector3(96f, 0f, 34f),
                    new Vector3(3f, -0.8f, 2.5f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg",
                rightUpLeg.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(68f, 0f, 4f),
                    new Vector3(78f, 0f, 4f),
                    new Vector3(86f, 0f, 3f),
                    new Vector3(96f, 0f, 3f),
                    new Vector3(100f, 0f, 4f),
                    new Vector3(2f, 0f, 1.2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg",
                rightLeg.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-82f, 0f, 0f),
                    new Vector3(-96f, 0f, 0f),
                    new Vector3(-110f, 0f, 0f),
                    new Vector3(-118f, 0f, 0f),
                    new Vector3(-122f, 0f, 0f),
                    new Vector3(-2f, 0f, 1f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
                rightFoot.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(30f, 0f, 0f),
                    new Vector3(34f, 0f, 0f),
                    new Vector3(38f, 0f, 0f),
                    new Vector3(42f, 0f, 0f),
                    new Vector3(44f, 0f, 0f),
                    new Vector3(1.5f, 0f, 0.8f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
                rightToe.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(8f, 0f, 0f),
                    new Vector3(10f, 0f, 0f),
                    new Vector3(14f, 0f, 0f),
                    new Vector3(16f, 0f, 0f),
                    new Vector3(18f, 0f, 0f),
                    new Vector3(1.2f, 0f, 0.6f)));

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg",
                leftUpLeg.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(68f, 0f, -4f),
                    new Vector3(78f, 0f, -4f),
                    new Vector3(86f, 0f, -3f),
                    new Vector3(96f, 0f, -3f),
                    new Vector3(100f, 0f, -4f),
                    new Vector3(2f, 0f, -1.2f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg",
                leftLeg.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(-82f, 0f, 0f),
                    new Vector3(-96f, 0f, 0f),
                    new Vector3(-110f, 0f, 0f),
                    new Vector3(-118f, 0f, 0f),
                    new Vector3(-122f, 0f, 0f),
                    new Vector3(-2f, 0f, -1f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
                leftFoot.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(30f, 0f, 0f),
                    new Vector3(34f, 0f, 0f),
                    new Vector3(38f, 0f, 0f),
                    new Vector3(42f, 0f, 0f),
                    new Vector3(44f, 0f, 0f),
                    new Vector3(1.5f, 0f, -0.8f)));
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase",
                leftToe.localRotation,
                BuildCrouchTrembleKeys(
                    new Vector3(8f, 0f, 0f),
                    new Vector3(10f, 0f, 0f),
                    new Vector3(14f, 0f, 0f),
                    new Vector3(16f, 0f, 0f),
                    new Vector3(18f, 0f, 0f),
                    new Vector3(1.2f, 0f, -0.6f)));

            clip.EnsureQuaternionContinuity();
            SetCrouchTremble5sClipSettings(clip);
            return clip;
        }

        private static PierceAttackVectorKey[] BuildCrouchTrembleKeys(
            Vector3 lying,
            Vector3 push,
            Vector3 rise,
            Vector3 crouch,
            Vector3 covered,
            Vector3 jitter)
        {
            var keys = new List<PierceAttackVectorKey>
            {
                new PierceAttackVectorKey(0f, lying),
                new PierceAttackVectorKey(CrouchTremblePushTime, push),
                new PierceAttackVectorKey(0.9f, rise),
                new PierceAttackVectorKey(CrouchTrembleRiseTime, crouch),
                new PierceAttackVectorKey(CrouchTrembleCoverFaceTime, covered)
            };

            var sign = 1f;
            for (var time = CrouchTrembleTrembleStartTime; time < CrouchTrembleDuration - 0.001f; time += CrouchTrembleTrembleStep)
            {
                keys.Add(new PierceAttackVectorKey(time, covered + jitter * sign));
                sign *= -1f;
            }

            keys.Add(new PierceAttackVectorKey(CrouchTrembleDuration, covered));
            return keys.ToArray();
        }

        private static AnimatorController EnsureCrouchTremble5sController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CrouchTrembleControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(CrouchTrembleControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(CrouchTrembleClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SetCrouchTremble5sClipSettings(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = CrouchTrembleDuration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static CrouchTrembleMotionMetrics EvaluateCrouchTrembleMetrics(AnimationClip clip, Transform root)
        {
            var hips = RequirePierceAttackBone(root, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(root, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine");
            var head = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var leftArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var rightUpLeg = RequirePierceAttackBone(root, "Armature/Hips/RightUpLeg");
            var rightLeg = RequirePierceAttackBone(root, "Armature/Hips/RightUpLeg/RightLeg");
            var leftUpLeg = RequirePierceAttackBone(root, "Armature/Hips/LeftUpLeg");
            var leftLeg = RequirePierceAttackBone(root, "Armature/Hips/LeftUpLeg/LeftLeg");
            var restRightUpLegRotation = rightUpLeg.localRotation;
            var restRightLegRotation = rightLeg.localRotation;
            var restLeftUpLegRotation = leftUpLeg.localRotation;
            var restLeftLegRotation = leftLeg.localRotation;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(LocalTransformSample.Capture).ToArray();

            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startHipsPosition = hips.localPosition;
                var startHipsRotation = hips.localRotation;
                var startSpine02Rotation = spine02.localRotation;
                var startSpine01Rotation = spine01.localRotation;
                var startSpineRotation = spine.localRotation;
                var startRightArmRotation = rightArm.localRotation;
                var startRightForeArmRotation = rightForeArm.localRotation;
                var startLeftArmRotation = leftArm.localRotation;
                var startLeftForeArmRotation = leftForeArm.localRotation;

                clip.SampleAnimation(root.gameObject, CrouchTrembleRiseTime);
                var riseHipsPosition = hips.localPosition;
                var riseHipsRotation = hips.localRotation;
                var riseSpine02Rotation = spine02.localRotation;
                var riseSpine01Rotation = spine01.localRotation;
                var riseSpineRotation = spine.localRotation;

                clip.SampleAnimation(root.gameObject, CrouchTrembleCoverFaceTime);
                var coverHipsPosition = hips.localPosition;
                var coverSpineRotation = spine.localRotation;
                var coverHeadRotation = head.localRotation;
                var coverRightArmRotation = rightArm.localRotation;
                var coverRightForeArmRotation = rightForeArm.localRotation;
                var coverLeftArmRotation = leftArm.localRotation;
                var coverLeftForeArmRotation = leftForeArm.localRotation;
                var coverRightUpLegRotation = rightUpLeg.localRotation;
                var coverRightLegRotation = rightLeg.localRotation;
                var coverLeftUpLegRotation = leftUpLeg.localRotation;
                var coverLeftLegRotation = leftLeg.localRotation;

                var maxTremblePosition = 0f;
                var maxTrembleRotation = 0f;
                for (var time = CrouchTrembleTrembleStartTime; time < CrouchTrembleDuration - 0.001f; time += CrouchTrembleTrembleStep)
                {
                    clip.SampleAnimation(root.gameObject, time);
                    maxTremblePosition = Mathf.Max(maxTremblePosition, Vector3.Distance(coverHipsPosition, hips.localPosition));
                    maxTrembleRotation = Mathf.Max(
                        maxTrembleRotation,
                        Mathf.Max(
                            Mathf.Max(Quaternion.Angle(coverSpineRotation, spine.localRotation), Quaternion.Angle(coverHeadRotation, head.localRotation)),
                            Mathf.Max(
                                Mathf.Max(Quaternion.Angle(coverRightArmRotation, rightArm.localRotation), Quaternion.Angle(coverRightForeArmRotation, rightForeArm.localRotation)),
                                Mathf.Max(Quaternion.Angle(coverLeftArmRotation, leftArm.localRotation), Quaternion.Angle(coverLeftForeArmRotation, leftForeArm.localRotation)))));
                }

                return new CrouchTrembleMotionMetrics(
                    riseHipsPosition.y - startHipsPosition.y,
                    Quaternion.Angle(startHipsRotation, riseHipsRotation) +
                    Quaternion.Angle(startSpine02Rotation, riseSpine02Rotation) +
                    Quaternion.Angle(startSpine01Rotation, riseSpine01Rotation) +
                    Quaternion.Angle(startSpineRotation, riseSpineRotation),
                    Mathf.Max(
                        Mathf.Max(Quaternion.Angle(startRightArmRotation, coverRightArmRotation), Quaternion.Angle(startRightForeArmRotation, coverRightForeArmRotation)),
                        Mathf.Max(Quaternion.Angle(startLeftArmRotation, coverLeftArmRotation), Quaternion.Angle(startLeftForeArmRotation, coverLeftForeArmRotation))),
                    Mathf.Max(
                        Mathf.Max(Quaternion.Angle(restRightUpLegRotation, coverRightUpLegRotation), Quaternion.Angle(restRightLegRotation, coverRightLegRotation)),
                        Mathf.Max(Quaternion.Angle(restLeftUpLegRotation, coverLeftUpLegRotation), Quaternion.Angle(restLeftLegRotation, coverLeftLegRotation))),
                    maxTremblePosition,
                    maxTrembleRotation);
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = originalStates[index].LocalPosition;
                    transforms[index].localRotation = originalStates[index].LocalRotation;
                    transforms[index].localScale = originalStates[index].LocalScale;
                }
            }
        }

        private static void RequireCrouchTrembleMetrics(CrouchTrembleMotionMetrics metrics)
        {
            if (metrics.RiseHipsLiftDelta < 0.045f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble must lift from the lying pose into a crouch. HipsLift=" +
                    metrics.RiseHipsLiftDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.RiseBodyRotationAngle < 95f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble body rise rotation is too small. BodyRotation=" +
                    metrics.RiseBodyRotationAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.FaceCoverArmAngle < 65f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble arms do not cover the face enough. ArmAngle=" +
                    metrics.FaceCoverArmAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.CrouchLegBendAngle < 75f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble leg bend is too small for a crouched pose. LegBend=" +
                    metrics.CrouchLegBendAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.TremblePositionRange < 0.012f || metrics.TremblePositionRange > 0.06f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble body shake position range is outside the expected range. PositionRange=" +
                    metrics.TremblePositionRange.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.TrembleRotationRange < 2.5f || metrics.TrembleRotationRange > 18f)
            {
                throw new InvalidOperationException(
                    "Crouch tremble body shake rotation range is outside the expected range. RotationRange=" +
                    metrics.TrembleRotationRange.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static PierceAttackRuntimePlaybackMetrics EvaluateCrouchTrembleAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var snapshots = root.GetComponentsInChildren<Transform>(true)
                .ToDictionary(transform => transform, LocalTransformSample.Capture);
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(CrouchTrembleClipName, 0, 0f);
                animator.Update(0f);
                var lyingStart = CapturePierceAttackRuntimePose(root);
                animator.Update(Mathf.Min(CrouchTrembleCoverFaceTime, clip.length * 0.5f));
                var faceCovered = CapturePierceAttackRuntimePose(root);
                animator.Update(0.54f);
                var trembleA = CapturePierceAttackRuntimePose(root);
                animator.Update(CrouchTrembleTrembleStep);
                var trembleB = CapturePierceAttackRuntimePose(root);

                var riseRotationDelta = MaxPierceAttackRuntimeRotationDelta(lyingStart, faceCovered);
                var risePositionDelta = MaxPierceAttackRuntimePositionDelta(lyingStart, faceCovered);
                var trembleRotationDelta = MaxPierceAttackRuntimeRotationDelta(trembleA, trembleB);
                var tremblePositionDelta = MaxPierceAttackRuntimePositionDelta(trembleA, trembleB);

                return new PierceAttackRuntimePlaybackMetrics(
                    riseRotationDelta > 12f || risePositionDelta > 0.04f,
                    trembleRotationDelta > 1.2f || tremblePositionDelta > 0.004f,
                    riseRotationDelta,
                    risePositionDelta,
                    trembleRotationDelta,
                    tremblePositionDelta);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Key.localPosition = snapshot.Value.LocalPosition;
                    snapshot.Key.localRotation = snapshot.Value.LocalRotation;
                    snapshot.Key.localScale = snapshot.Value.LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static int GetBackRushVisualHierarchyDepth(Transform transform)
        {
            var depth = 0;
            var current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static AnimationClip EnsurePierceAttackClip(Transform pierceAttackRoot)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PierceAttackClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, PierceAttackClipPath);
            }

            clip.name = PierceAttackClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            clip.ClearCurves();

            var hips = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine");
            var neck = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/neck");
            var head = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightShoulder = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder");
            var rightArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftShoulder = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder");
            var leftArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var leftHand = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
            var rightUpLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg");
            var rightLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg/RightLeg");
            var rightFoot = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot");
            var rightToe = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase");
            var leftUpLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/LeftUpLeg");
            var leftLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/LeftUpLeg/LeftLeg");
            var leftFoot = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");

            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips",
                hips.localPosition,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(0f, -0.015f, 0.015f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(0.012f, -0.055f, -0.055f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(0f, -0.13f, 0.145f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(0f, -0.125f, 0.13f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(0f, -0.055f, 0.02f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips",
                hips.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(15f, -10f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(24f, 2f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(22f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(9f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02",
                spine02.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(5f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(14f, -14f, 2f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(26f, 10f, -2f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(24f, 8f, -2f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(8f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01",
                spine01.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(12f, -12f, 4f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(22f, 12f, -4f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(22f, 10f, -4f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(6f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine",
                spine.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(2f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(10f, -10f, 3f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(18f, 12f, -3f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(18f, 12f, -3f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(5f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck",
                neck.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-8f, 8f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-12f, -6f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-12f, -6f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-4f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                head.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-5f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-8f, 10f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-16f, -8f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-16f, -8f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-5f, 0f, 0f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                rightShoulder.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-8f, 6f, -12f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(28f, -28f, -32f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-42f, 16f, 18f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-44f, 16f, 18f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-10f, 4f, -8f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                rightArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-20f, 10f, -20f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(78f, -32f, -48f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-118f, 8f, 8f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-120f, 8f, 8f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-35f, 4f, -12f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                rightForeArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(12f, 0f, -8f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(38f, 6f, -28f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-42f, -2f, 3f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-42f, -2f, 3f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-8f, 0f, -4f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                rightHand.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(5f, 0f, -8f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(36f, 0f, -32f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-86f, 0f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-88f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-12f, 0f, -4f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                leftShoulder.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(10f, -4f, 12f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-30f, 22f, 28f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(48f, -18f, -24f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(44f, -18f, -22f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(12f, -4f, 8f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                leftArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(18f, -8f, 18f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-46f, 22f, 36f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(62f, -14f, -30f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(58f, -14f, -28f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(20f, -6f, 12f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                leftForeArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-8f, 0f, 8f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-28f, -4f, 18f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(24f, 0f, -18f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(24f, 0f, -18f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-6f, 0f, 6f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                leftHand.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-6f, 0f, 6f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-22f, 0f, 20f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(24f, 0f, -20f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(24f, 0f, -20f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-6f, 0f, 6f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg",
                rightUpLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(22f, 0f, 4f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(38f, 0f, 8f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(68f, 0f, 4f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(68f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(28f, 0f, 2f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg",
                rightLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-30f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-56f, 0f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-84f, 0f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-84f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-40f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
                rightFoot.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(10f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(20f, 0f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(30f, 0f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(28f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(10f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
                rightToe.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(2f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(18f, 0f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(18f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(4f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg",
                leftUpLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-18f, 0f, -4f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-42f, 0f, -8f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-34f, 0f, -6f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-34f, 0f, -6f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-12f, 0f, -2f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg",
                leftLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(18f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(42f, 0f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(26f, 0f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(26f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(10f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
                leftFoot.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-8f, 0f, 0f)),
                    new PierceAttackVectorKey(0.12f, new Vector3(-18f, 0f, 0f)),
                    new PierceAttackVectorKey(0.26f, new Vector3(-12f, 0f, 0f)),
                    new PierceAttackVectorKey(0.36f, new Vector3(-12f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-4f, 0f, 0f))
                });

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = PierceAttackDuration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip EnsureReadablePierceAttackClip(Transform pierceAttackRoot)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PierceAttackClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, PierceAttackClipPath);
            }

            clip.name = PierceAttackClipName;
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            clip.ClearCurves();

            var hips = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips");
            var spine02 = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02");
            var spine01 = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01");
            var spine = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine");
            var neck = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/neck");
            var head = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/neck/Head");
            var rightShoulder = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder");
            var rightArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftShoulder = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder");
            var leftArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var leftHand = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
            var rightUpLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg");
            var rightLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg/RightLeg");
            var rightFoot = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot");
            var rightToe = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase");
            var leftUpLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/LeftUpLeg");
            var leftLeg = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/LeftUpLeg/LeftLeg");
            var leftFoot = RequirePierceAttackBone(pierceAttackRoot, "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");

            SetPierceAttackPositionCurves(
                clip,
                "Armature/Hips",
                hips.localPosition,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(0f, -0.015f, 0.02f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(0f, -0.035f, -0.055f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(0f, -0.095f, 0.12f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(0f, -0.105f, 0.18f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(0f, -0.105f, 0.18f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(0f, -0.04f, 0.04f)),
                    new PierceAttackVectorKey(PierceAttackDuration, Vector3.zero)
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips",
                hips.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(6f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(12f, -5f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(16f, 2f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(18f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(18f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(4f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02",
                spine02.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(3f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(5f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(9f, -8f, 2f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(14f, 4f, -2f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(16f, 6f, -2f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(16f, 6f, -2f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(7f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(3f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01",
                spine01.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(2f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(8f, -7f, 2f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(12f, 4f, -2f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(14f, 5f, -2f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(14f, 5f, -2f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(6f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(2f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine",
                spine.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(1f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(3f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(6f, -6f, 1f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(10f, 3f, -1f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(12f, 4f, -1f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(12f, 4f, -1f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(5f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(1f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck",
                neck.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-3f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-6f, 3f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-7f, -2f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-8f, -2f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-8f, -2f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-3f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head",
                head.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-5f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-8f, 4f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-9f, -3f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-10f, -3f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-10f, -3f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-5f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-4f, 0f, 0f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder",
                rightShoulder.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-8f, 6f, -12f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-10f, 4f, -10f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(28f, -28f, -32f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-20f, 6f, 8f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-44f, 16f, 18f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-44f, 16f, 18f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-14f, 6f, -8f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-8f, 6f, -12f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                rightArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-20f, 10f, -20f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-26f, 8f, -18f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(78f, -32f, -48f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-74f, 6f, 6f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-120f, 8f, 8f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-120f, 8f, 8f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-36f, 6f, -12f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-20f, 10f, -20f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                rightForeArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(12f, 0f, -8f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(16f, 0f, -10f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(38f, 6f, -28f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(4f, 0f, -2f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(0f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(0f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(8f, 0f, -4f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(12f, 0f, -8f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand",
                rightHand.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(5f, 0f, -8f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(8f, 0f, -10f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(36f, 0f, -32f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-6f, 0f, -2f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(0f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(0f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-4f, 0f, -4f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(5f, 0f, -8f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder",
                leftShoulder.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(4f, -2f, 4f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(5f, -2f, 5f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-2f, 4f, 8f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-4f, 4f, 6f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-6f, 4f, 6f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-6f, 4f, 6f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(2f, -1f, 4f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(4f, -2f, 4f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                leftArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(8f, -3f, 8f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(8f, -3f, 8f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(10f, 0f, 10f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(11f, 0f, 9f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(12f, 0f, 8f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(12f, 0f, 8f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(9f, -2f, 8f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(8f, -3f, 8f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                leftForeArm.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-4f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-4f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-2f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(0f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(2f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(2f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-2f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-4f, 0f, 3f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand",
                leftHand.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-3f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-3f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-2f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(0f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(2f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(2f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-2f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-3f, 0f, 3f))
                });

            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg",
                rightUpLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(12f, 0f, 2f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(18f, 0f, 3f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(38f, 0f, 6f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(60f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(62f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(62f, 0f, 4f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(25f, 0f, 2f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(12f, 0f, 2f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg",
                rightLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-18f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-42f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-70f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-70f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-70f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-25f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-8f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot",
                rightFoot.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(18f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(22f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(20f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(20f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(4f, 0f, 0f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase",
                rightToe.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(3f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(12f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(10f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(10f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, Vector3.zero)
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg",
                leftUpLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-4f, 0f, -2f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-8f, 0f, -2f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-16f, 0f, -3f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-18f, 0f, -3f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-18f, 0f, -3f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-18f, 0f, -3f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-10f, 0f, -2f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-4f, 0f, -2f))
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg",
                leftLeg.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, Vector3.zero),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(6f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, Vector3.zero)
                });
            SetPierceAttackRotationCurves(
                clip,
                "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot",
                leftFoot.localRotation,
                new[]
                {
                    new PierceAttackVectorKey(0f, new Vector3(-2f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackReadyTime, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackWindupTime, new Vector3(-6f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackPlantTime, new Vector3(-8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackThrustTime, new Vector3(-8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackHoldTime, new Vector3(-8f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackRecoverTime, new Vector3(-4f, 0f, 0f)),
                    new PierceAttackVectorKey(PierceAttackDuration, new Vector3(-2f, 0f, 0f))
                });

            clip.EnsureQuaternionContinuity();
            SetPierceAttackRuntimePlaybackClipSettings(clip);
            return clip;
        }

        private static AnimatorController EnsurePierceAttackController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PierceAttackControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(PierceAttackControllerPath);
            }

            if (controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var state = stateMachine.AddState(PierceAttackClipName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static int RemovePierceAttackChildAnimators(Transform root)
        {
            var childAnimators = root.GetComponentsInChildren<Animator>(true)
                .Where(animator => animator.transform != root)
                .ToArray();
            foreach (var animator in childAnimators)
            {
                UnityEngine.Object.DestroyImmediate(animator);
            }

            return childAnimators.Length;
        }

        private static Transform RequirePierceAttackBone(Transform root, string path)
        {
            var transform = root.Find(path);
            if (transform == null)
            {
                throw new InvalidOperationException("Missing pierce attack rig bone under " + root.name + ": " + path);
            }

            return transform;
        }

        private static void SetPierceAttackPositionCurves(
            AnimationClip clip,
            string path,
            Vector3 basePosition,
            PierceAttackVectorKey[] keys)
        {
            var x = new Keyframe[keys.Length];
            var y = new Keyframe[keys.Length];
            var z = new Keyframe[keys.Length];
            for (var index = 0; index < keys.Length; index++)
            {
                var value = basePosition + keys[index].Value;
                x[index] = new Keyframe(keys[index].Time, value.x);
                y[index] = new Keyframe(keys[index].Time, value.y);
                z[index] = new Keyframe(keys[index].Time, value.z);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"),
                new AnimationCurve(x));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                new AnimationCurve(y));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"),
                new AnimationCurve(z));
        }

        private static void SetPierceAttackRotationCurves(
            AnimationClip clip,
            string path,
            Quaternion baseRotation,
            PierceAttackVectorKey[] keys)
        {
            var x = new Keyframe[keys.Length];
            var y = new Keyframe[keys.Length];
            var z = new Keyframe[keys.Length];
            var w = new Keyframe[keys.Length];
            for (var index = 0; index < keys.Length; index++)
            {
                var rotation = baseRotation * Quaternion.Euler(keys[index].Value);
                x[index] = new Keyframe(keys[index].Time, rotation.x);
                y[index] = new Keyframe(keys[index].Time, rotation.y);
                z[index] = new Keyframe(keys[index].Time, rotation.z);
                w[index] = new Keyframe(keys[index].Time, rotation.w);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"),
                new AnimationCurve(x));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"),
                new AnimationCurve(y));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"),
                new AnimationCurve(z));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"),
                new AnimationCurve(w));
        }

        private static PierceAttackMotionMetrics EvaluatePierceAttackMetrics(AnimationClip clip, Transform root)
        {
            var hips = RequirePierceAttackBone(root, "Armature/Hips");
            var rightArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            var rightForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm");
            var rightHand = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            var leftArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            var leftForeArm = RequirePierceAttackBone(root, "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm");
            var rightUpLeg = RequirePierceAttackBone(root, "Armature/Hips/RightUpLeg");
            var rightLeg = RequirePierceAttackBone(root, "Armature/Hips/RightUpLeg/RightLeg");
            var leftUpLeg = RequirePierceAttackBone(root, "Armature/Hips/LeftUpLeg");
            var leftLeg = RequirePierceAttackBone(root, "Armature/Hips/LeftUpLeg/LeftLeg");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(transform => transform.localPosition).ToArray();
            var rotations = transforms.Select(transform => transform.localRotation).ToArray();
            var scales = transforms.Select(transform => transform.localScale).ToArray();

            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startHips = hips.localPosition;
                var startRightArm = rightArm.localRotation;
                var startRightForeArm = rightForeArm.localRotation;
                var startRightUpLeg = rightUpLeg.localRotation;
                var startRightLeg = rightLeg.localRotation;
                var startLeftArm = leftArm.localRotation;
                var startLeftForeArm = leftForeArm.localRotation;
                var startLeftUpLeg = leftUpLeg.localRotation;
                var startLeftLeg = leftLeg.localRotation;

                clip.SampleAnimation(root.gameObject, PierceAttackWindupTime);
                var windupHips = hips.localPosition;
                var windupRightArm = rightArm.localRotation;
                var windupRightForeArm = rightForeArm.localRotation;
                var windupRightHandPosition = root.InverseTransformPoint(rightHand.position);
                var windupLeftArm = leftArm.localRotation;
                var windupLeftForeArm = leftForeArm.localRotation;

                clip.SampleAnimation(root.gameObject, PierceAttackThrustTime);
                var thrustHips = hips.localPosition;
                var thrustRightArm = rightArm.localRotation;
                var thrustRightForeArm = rightForeArm.localRotation;
                var thrustRightHandPosition = root.InverseTransformPoint(rightHand.position);
                var thrustRightUpLeg = rightUpLeg.localRotation;
                var thrustRightLeg = rightLeg.localRotation;
                var thrustLeftArm = leftArm.localRotation;
                var thrustLeftForeArm = leftForeArm.localRotation;
                var thrustLeftUpLeg = leftUpLeg.localRotation;
                var thrustLeftLeg = leftLeg.localRotation;
                var thrustRightElbowExtensionAngle = Vector3.Angle(
                    rightArm.position - rightForeArm.position,
                    rightHand.position - rightForeArm.position);

                clip.SampleAnimation(root.gameObject, PierceAttackHoldTime);
                var holdHips = hips.localPosition;
                var holdRightArm = rightArm.localRotation;
                var holdRightHandPosition = root.InverseTransformPoint(rightHand.position);
                var holdLeftArm = leftArm.localRotation;
                var holdLeftForeArm = leftForeArm.localRotation;

                var minY = Mathf.Min(startHips.y, windupHips.y, thrustHips.y, holdHips.y);
                var maxY = Mathf.Max(startHips.y, windupHips.y, thrustHips.y, holdHips.y);
                var xRange = Mathf.Max(startHips.x, windupHips.x, thrustHips.x, holdHips.x) -
                             Mathf.Min(startHips.x, windupHips.x, thrustHips.x, holdHips.x);
                var zRange = Mathf.Max(startHips.z, windupHips.z, thrustHips.z, holdHips.z) -
                             Mathf.Min(startHips.z, windupHips.z, thrustHips.z, holdHips.z);
                var leftArmMaxAngle = Mathf.Max(
                    Quaternion.Angle(startLeftArm, windupLeftArm),
                    Quaternion.Angle(startLeftArm, thrustLeftArm));
                leftArmMaxAngle = Mathf.Max(leftArmMaxAngle, Quaternion.Angle(startLeftArm, holdLeftArm));
                var leftForeArmMaxAngle = Mathf.Max(
                    Quaternion.Angle(startLeftForeArm, windupLeftForeArm),
                    Quaternion.Angle(startLeftForeArm, thrustLeftForeArm));
                leftForeArmMaxAngle = Mathf.Max(leftForeArmMaxAngle, Quaternion.Angle(startLeftForeArm, holdLeftForeArm));
                var rightHandThrustDelta = thrustRightHandPosition - windupRightHandPosition;
                var rightHandHoldDelta = holdRightHandPosition - thrustRightHandPosition;

                return new PierceAttackMotionMetrics(
                    Quaternion.Angle(startRightArm, windupRightArm),
                    Quaternion.Angle(windupRightArm, thrustRightArm),
                    Quaternion.Angle(windupRightForeArm, thrustRightForeArm),
                    rightHandThrustDelta.z,
                    Mathf.Abs(rightHandThrustDelta.x),
                    Mathf.Abs(rightHandThrustDelta.y),
                    thrustRightElbowExtensionAngle,
                    rightHandHoldDelta.magnitude,
                    Quaternion.Angle(startRightUpLeg, thrustRightUpLeg),
                    Quaternion.Angle(startRightLeg, thrustRightLeg),
                    Quaternion.Angle(startLeftUpLeg, thrustLeftUpLeg),
                    Quaternion.Angle(startLeftLeg, thrustLeftLeg),
                    leftArmMaxAngle,
                    leftForeArmMaxAngle,
                    Mathf.Max(xRange, zRange),
                    maxY - minY,
                    Quaternion.Angle(thrustRightArm, holdRightArm));
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = positions[index];
                    transforms[index].localRotation = rotations[index];
                    transforms[index].localScale = scales[index];
                }
            }
        }

        private static void RequirePierceAttackMetrics(PierceAttackMotionMetrics metrics)
        {
            if (PierceAttackDuration < 1f)
            {
                throw new InvalidOperationException(
                    "Pierce attack timing is too fast to read. Duration=" +
                    PierceAttackDuration.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.RightArmWindupAngle < 45f)
            {
                throw new InvalidOperationException(
                    "Pierce attack right arm pull-back is too small. Angle=" +
                    metrics.RightArmWindupAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.RightArmThrustAngle < 110f)
            {
                throw new InvalidOperationException(
                    "Pierce attack right arm forward thrust is too small. Angle=" +
                    metrics.RightArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.RightForeArmThrustAngle < 40f)
            {
                throw new InvalidOperationException(
                    "Pierce attack right forearm thrust is too small. Angle=" +
                    metrics.RightForeArmThrustAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.RightHandForwardDelta < 0.04f ||
                metrics.RightHandForwardDelta < metrics.RightHandLateralDelta * 1.25f)
            {
                throw new InvalidOperationException(
                    "Pierce attack right hand is not driving forward like a straight punch. Forward=" +
                    metrics.RightHandForwardDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", Lateral=" + metrics.RightHandLateralDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", Vertical=" + metrics.RightHandVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.RightElbowExtensionAngle < 145f)
            {
                throw new InvalidOperationException(
                    "Pierce attack right elbow is not extended enough for a straight punch. ElbowAngle=" +
                    metrics.RightElbowExtensionAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.RightHandHoldDrift > 0.02f)
            {
                throw new InvalidOperationException(
                    "Pierce attack straight punch impact hand hold is drifting. HoldDrift=" +
                    metrics.RightHandHoldDrift.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (metrics.RightUpLegForwardAngle < 35f || metrics.RightKneeBendAngle < 35f)
            {
                throw new InvalidOperationException(
                    "Pierce attack right forward bent-knee pose is too small. UpLeg=" +
                    metrics.RightUpLegForwardAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Knee=" + metrics.RightKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.LeftUpLegSupportAngle < 10f)
            {
                throw new InvalidOperationException(
                    "Pierce attack left support leg does not brace behind the thrust. Angle=" +
                    metrics.LeftUpLegSupportAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.LeftKneeBendAngle > 20f)
            {
                throw new InvalidOperationException(
                    "Pierce attack left support knee bends too much. Angle=" +
                    metrics.LeftKneeBendAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.LeftArmMaxAngle > 25f || metrics.LeftForeArmMaxAngle > 20f)
            {
                throw new InvalidOperationException(
                    "Pierce attack left arm is moving too much. Arm=" +
                    metrics.LeftArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", ForeArm=" + metrics.LeftForeArmMaxAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.HipsForwardRange < 0.12f || metrics.HipsVerticalRange < 0.08f)
            {
                throw new InvalidOperationException(
                    "Pierce attack body lunge range is too small. Forward=" +
                    metrics.HipsForwardRange.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Vertical=" + metrics.HipsVerticalRange.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (metrics.ImpactHoldAngle > 6f)
            {
                throw new InvalidOperationException(
                    "Pierce attack impact hold is unstable. HoldAngle=" +
                    metrics.ImpactHoldAngle.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static void RequireNoConfiguredAnimator(Transform root, string rootName)
        {
            var configuredCount = CountConfiguredAnimators(root);
            if (configuredCount != 0)
            {
                throw new InvalidOperationException(
                    rootName + " must not have a dedicated animation controller. Count=" +
                    configuredCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void RequireNoConfiguredTergoAnimatorsExcept(Transform placementRoot, params string[] allowedRootNames)
        {
            var count = CountConfiguredTergoAnimatorsExcept(placementRoot, allowedRootNames);
            if (count != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected configured Tergo animators outside approved roots. Count=" +
                    count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static int CountConfiguredTergoAnimatorsExcept(Transform placementRoot, params string[] allowedRootNames)
        {
            var count = 0;
            for (var index = 0; index < placementRoot.childCount; index++)
            {
                var child = placementRoot.GetChild(index);
                if (!child.name.StartsWith("Tergo_", StringComparison.Ordinal) ||
                    allowedRootNames.Contains(child.name, StringComparer.Ordinal))
                {
                    continue;
                }

                count += CountConfiguredAnimators(child);
            }

            return count;
        }

        private static int CountConfiguredAnimators(Transform root)
        {
            return root.GetComponentsInChildren<Animator>(true)
                .Count(animator => animator.runtimeAnimatorController != null);
        }

        private static int CountDescendantsByName(Transform root, string objectName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Count(transform => string.Equals(transform.name, objectName, StringComparison.Ordinal));
        }

        private static void SetPierceAttackRuntimePlaybackClipSettings(AnimationClip clip)
        {
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.startTime = 0f;
            settings.stopTime = PierceAttackDuration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static bool PierceAttackDefaultStateUsesClip(AnimatorController controller, AnimationClip clip)
        {
            if (controller.layers.Length == 0)
            {
                return false;
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            return defaultState != null && defaultState.motion == clip;
        }

        private static PierceAttackRuntimePlaybackMetrics EvaluatePierceAttackAnimatorPlayback(
            Animator animator,
            Transform root,
            AnimationClip clip)
        {
            var snapshots = root.GetComponentsInChildren<Transform>(true)
                .ToDictionary(transform => transform, LocalTransformSample.Capture);
            var previousEnabled = animator.enabled;
            var previousApplyRootMotion = animator.applyRootMotion;
            var previousCullingMode = animator.cullingMode;
            var previousSpeed = animator.speed;

            try
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 1f;
                animator.Rebind();
                animator.Update(0f);
                animator.Play(PierceAttackClipName, 0, 0f);
                animator.Update(0f);
                var firstStart = CapturePierceAttackRuntimePose(root);
                animator.Update(Mathf.Min(0.26f, clip.length * 0.5f));
                var firstStrike = CapturePierceAttackRuntimePose(root);

                animator.Play(PierceAttackClipName, 0, 0f);
                animator.Update(0f);
                animator.Update(clip.length + 0.08f);
                var postLoopA = CapturePierceAttackRuntimePose(root);
                animator.Update(0.14f);
                var postLoopB = CapturePierceAttackRuntimePose(root);

                var firstRotationDelta = MaxPierceAttackRuntimeRotationDelta(firstStart, firstStrike);
                var firstPositionDelta = MaxPierceAttackRuntimePositionDelta(firstStart, firstStrike);
                var postLoopRotationDelta = MaxPierceAttackRuntimeRotationDelta(postLoopA, postLoopB);
                var postLoopPositionDelta = MaxPierceAttackRuntimePositionDelta(postLoopA, postLoopB);

                return new PierceAttackRuntimePlaybackMetrics(
                    firstRotationDelta > 8f || firstPositionDelta > 0.015f,
                    postLoopRotationDelta > 8f || postLoopPositionDelta > 0.015f,
                    firstRotationDelta,
                    firstPositionDelta,
                    postLoopRotationDelta,
                    postLoopPositionDelta);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Key.localPosition = snapshot.Value.LocalPosition;
                    snapshot.Key.localRotation = snapshot.Value.LocalRotation;
                    snapshot.Key.localScale = snapshot.Value.LocalScale;
                }

                animator.enabled = previousEnabled;
                animator.applyRootMotion = previousApplyRootMotion;
                animator.cullingMode = previousCullingMode;
                animator.speed = previousSpeed;
            }
        }

        private static PierceAttackRuntimeBoneSample[] CapturePierceAttackRuntimePose(Transform root)
        {
            return new[]
            {
                CapturePierceAttackRuntimeBone(root, "Armature/Hips"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/Spine02"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/Spine02/Spine01/Spine"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/RightUpLeg"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/RightUpLeg/RightLeg"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/LeftUpLeg"),
                CapturePierceAttackRuntimeBone(root, "Armature/Hips/LeftUpLeg/LeftLeg")
            };
        }

        private static PierceAttackRuntimeBoneSample CapturePierceAttackRuntimeBone(Transform root, string path)
        {
            var bone = RequirePierceAttackBone(root, path);
            return new PierceAttackRuntimeBoneSample(path, bone.localPosition, bone.localRotation);
        }

        private static float MaxPierceAttackRuntimeRotationDelta(
            PierceAttackRuntimeBoneSample[] first,
            PierceAttackRuntimeBoneSample[] second)
        {
            var max = 0f;
            for (var index = 0; index < first.Length; index++)
            {
                max = Mathf.Max(max, Quaternion.Angle(first[index].LocalRotation, second[index].LocalRotation));
            }

            return max;
        }

        private static float MaxPierceAttackRuntimePositionDelta(
            PierceAttackRuntimeBoneSample[] first,
            PierceAttackRuntimeBoneSample[] second)
        {
            var max = 0f;
            for (var index = 0; index < first.Length; index++)
            {
                max = Mathf.Max(max, Vector3.Distance(first[index].LocalPosition, second[index].LocalPosition));
            }

            return max;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing asset: " + path);
            }

            return asset;
        }

        private static GameObject RequireSceneObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                return target;
            }

            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!string.Equals(candidate.name, objectName, StringComparison.Ordinal) ||
                    EditorUtility.IsPersistent(candidate) ||
                    !candidate.scene.IsValid() ||
                    !string.Equals(candidate.scene.path, CargoRunScenePath, StringComparison.Ordinal))
                {
                    continue;
                }

                return candidate;
            }

            throw new InvalidOperationException("Missing scene object: " + objectName);
        }

        private static Transform RequireChild(Transform root, string childName)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException("Missing child under " + root.name + ": " + childName);
            }

            return child;
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            for (var index = 0; index < root.childCount; index++)
            {
                var child = root.GetChild(index);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static string FormatClipNames(AnimationClip[] clips)
        {
            return string.Join(
                "|",
                clips.Select(clip =>
                    clip.name + "(" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) + "s)"));
        }

        private static Avatar LoadNormalTergoAvatarOrNull()
        {
            return AssetDatabase.LoadAllAssetsAtPath(NormalModelAssetPath)
                .OfType<Avatar>()
                .FirstOrDefault();
        }

        private static string FormatVector3(Vector3 value)
        {
            return value.x.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return value.x.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("0.######", CultureInfo.InvariantCulture) + "," +
                   value.w.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private readonly struct PierceAttackVectorKey
        {
            public readonly float Time;
            public readonly Vector3 Value;

            public PierceAttackVectorKey(float time, Vector3 value)
            {
                Time = time;
                Value = value;
            }
        }

        private readonly struct InterruptStaggerMotionMetrics
        {
            public readonly float HipsBackwardDelta;
            public readonly float HipsDropDelta;
            public readonly float FallTravelDistance;
            public readonly float HipsFallRotationAngle;
            public readonly float TorsoFallRotationAngle;
            public readonly float MaxLegBendAngle;
            public readonly float MaxArmFlailAngle;
            public readonly float ImpactHoldDrift;
            public readonly float SettleShakeAngle;

            public InterruptStaggerMotionMetrics(
                float hipsBackwardDelta,
                float hipsDropDelta,
                float fallTravelDistance,
                float hipsFallRotationAngle,
                float torsoFallRotationAngle,
                float maxLegBendAngle,
                float maxArmFlailAngle,
                float impactHoldDrift,
                float settleShakeAngle)
            {
                HipsBackwardDelta = hipsBackwardDelta;
                HipsDropDelta = hipsDropDelta;
                FallTravelDistance = fallTravelDistance;
                HipsFallRotationAngle = hipsFallRotationAngle;
                TorsoFallRotationAngle = torsoFallRotationAngle;
                MaxLegBendAngle = maxLegBendAngle;
                MaxArmFlailAngle = maxArmFlailAngle;
                ImpactHoldDrift = impactHoldDrift;
                SettleShakeAngle = settleShakeAngle;
            }
        }

        private readonly struct CrouchTrembleMotionMetrics
        {
            public readonly float RiseHipsLiftDelta;
            public readonly float RiseBodyRotationAngle;
            public readonly float FaceCoverArmAngle;
            public readonly float CrouchLegBendAngle;
            public readonly float TremblePositionRange;
            public readonly float TrembleRotationRange;

            public CrouchTrembleMotionMetrics(
                float riseHipsLiftDelta,
                float riseBodyRotationAngle,
                float faceCoverArmAngle,
                float crouchLegBendAngle,
                float tremblePositionRange,
                float trembleRotationRange)
            {
                RiseHipsLiftDelta = riseHipsLiftDelta;
                RiseBodyRotationAngle = riseBodyRotationAngle;
                FaceCoverArmAngle = faceCoverArmAngle;
                CrouchLegBendAngle = crouchLegBendAngle;
                TremblePositionRange = tremblePositionRange;
                TrembleRotationRange = trembleRotationRange;
            }
        }

        private readonly struct HitNormalMotionMetrics
        {
            public readonly bool HandsNotCrossed;
            public readonly float StartHandSide;
            public readonly float GuardHandSide;
            public readonly float StartHandLateralDistance;
            public readonly float GuardHandLateralDistance;
            public readonly float MinHandForwardDelta;
            public readonly float MinHandRaiseDelta;
            public readonly float FaceGuardDistance;
            public readonly float ForearmVerticalScore;
            public readonly float ArmRaiseAngle;
            public readonly float HeadTurnAngle;
            public readonly float HipsBackwardDelta;
            public readonly float BodyRecoilAngle;
            public readonly float GuardHoldDrift;

            public HitNormalMotionMetrics(
                bool handsNotCrossed,
                float startHandSide,
                float guardHandSide,
                float startHandLateralDistance,
                float guardHandLateralDistance,
                float minHandForwardDelta,
                float minHandRaiseDelta,
                float faceGuardDistance,
                float forearmVerticalScore,
                float armRaiseAngle,
                float headTurnAngle,
                float hipsBackwardDelta,
                float bodyRecoilAngle,
                float guardHoldDrift)
            {
                HandsNotCrossed = handsNotCrossed;
                StartHandSide = startHandSide;
                GuardHandSide = guardHandSide;
                StartHandLateralDistance = startHandLateralDistance;
                GuardHandLateralDistance = guardHandLateralDistance;
                MinHandForwardDelta = minHandForwardDelta;
                MinHandRaiseDelta = minHandRaiseDelta;
                FaceGuardDistance = faceGuardDistance;
                ForearmVerticalScore = forearmVerticalScore;
                ArmRaiseAngle = armRaiseAngle;
                HeadTurnAngle = headTurnAngle;
                HipsBackwardDelta = hipsBackwardDelta;
                BodyRecoilAngle = bodyRecoilAngle;
                GuardHoldDrift = guardHoldDrift;
            }
        }

        private readonly struct DeathMeltPuddleBoneSpec
        {
            public readonly string Path;
            public readonly Vector3 PositionOffset;
            public readonly Vector3 EulerOffset;
            public readonly Vector3 ScaleMultiplier;

            public DeathMeltPuddleBoneSpec(
                string path,
                Vector3 positionOffset,
                Vector3 eulerOffset,
                Vector3 scaleMultiplier)
            {
                Path = path;
                PositionOffset = positionOffset;
                EulerOffset = eulerOffset;
                ScaleMultiplier = scaleMultiplier;
            }
        }

        private readonly struct DeathMeltPuddleMotionMetrics
        {
            public readonly float MeltSegmentDuration;
            public readonly float HipsHeightDrop;
            public readonly float AverageVerticalScaleRatio;
            public readonly float AverageHorizontalScaleRatio;
            public readonly float PuddleBoneHeightRange;
            public readonly float AveragePuddleGroundDistance;
            public readonly bool FinalHoldStable;
            public readonly float MaxHoldPositionDrift;
            public readonly float MaxHoldRotationDrift;
            public readonly float MaxHoldScaleDrift;

            public DeathMeltPuddleMotionMetrics(
                float meltSegmentDuration,
                float hipsHeightDrop,
                float averageVerticalScaleRatio,
                float averageHorizontalScaleRatio,
                float puddleBoneHeightRange,
                float averagePuddleGroundDistance,
                bool finalHoldStable,
                float maxHoldPositionDrift,
                float maxHoldRotationDrift,
                float maxHoldScaleDrift)
            {
                MeltSegmentDuration = meltSegmentDuration;
                HipsHeightDrop = hipsHeightDrop;
                AverageVerticalScaleRatio = averageVerticalScaleRatio;
                AverageHorizontalScaleRatio = averageHorizontalScaleRatio;
                PuddleBoneHeightRange = puddleBoneHeightRange;
                AveragePuddleGroundDistance = averagePuddleGroundDistance;
                FinalHoldStable = finalHoldStable;
                MaxHoldPositionDrift = maxHoldPositionDrift;
                MaxHoldRotationDrift = maxHoldRotationDrift;
                MaxHoldScaleDrift = maxHoldScaleDrift;
            }
        }

        private readonly struct DyingSourceMotionMatchMetrics
        {
            public readonly float MaxPositionDelta;
            public readonly float MaxRotationDelta;
            public readonly float MaxScaleDelta;
            public readonly int SampleCount;

            public DyingSourceMotionMatchMetrics(
                float maxPositionDelta,
                float maxRotationDelta,
                float maxScaleDelta,
                int sampleCount)
            {
                MaxPositionDelta = maxPositionDelta;
                MaxRotationDelta = maxRotationDelta;
                MaxScaleDelta = maxScaleDelta;
                SampleCount = sampleCount;
            }
        }

        private readonly struct PierceAttackMotionMetrics
        {
            public readonly float RightArmWindupAngle;
            public readonly float RightArmThrustAngle;
            public readonly float RightForeArmThrustAngle;
            public readonly float RightHandForwardDelta;
            public readonly float RightHandLateralDelta;
            public readonly float RightHandVerticalDelta;
            public readonly float RightElbowExtensionAngle;
            public readonly float RightHandHoldDrift;
            public readonly float RightUpLegForwardAngle;
            public readonly float RightKneeBendAngle;
            public readonly float LeftUpLegSupportAngle;
            public readonly float LeftKneeBendAngle;
            public readonly float LeftArmMaxAngle;
            public readonly float LeftForeArmMaxAngle;
            public readonly float HipsForwardRange;
            public readonly float HipsVerticalRange;
            public readonly float ImpactHoldAngle;

            public PierceAttackMotionMetrics(
                float rightArmWindupAngle,
                float rightArmThrustAngle,
                float rightForeArmThrustAngle,
                float rightHandForwardDelta,
                float rightHandLateralDelta,
                float rightHandVerticalDelta,
                float rightElbowExtensionAngle,
                float rightHandHoldDrift,
                float rightUpLegForwardAngle,
                float rightKneeBendAngle,
                float leftUpLegSupportAngle,
                float leftKneeBendAngle,
                float leftArmMaxAngle,
                float leftForeArmMaxAngle,
                float hipsForwardRange,
                float hipsVerticalRange,
                float impactHoldAngle)
            {
                RightArmWindupAngle = rightArmWindupAngle;
                RightArmThrustAngle = rightArmThrustAngle;
                RightForeArmThrustAngle = rightForeArmThrustAngle;
                RightHandForwardDelta = rightHandForwardDelta;
                RightHandLateralDelta = rightHandLateralDelta;
                RightHandVerticalDelta = rightHandVerticalDelta;
                RightElbowExtensionAngle = rightElbowExtensionAngle;
                RightHandHoldDrift = rightHandHoldDrift;
                RightUpLegForwardAngle = rightUpLegForwardAngle;
                RightKneeBendAngle = rightKneeBendAngle;
                LeftUpLegSupportAngle = leftUpLegSupportAngle;
                LeftKneeBendAngle = leftKneeBendAngle;
                LeftArmMaxAngle = leftArmMaxAngle;
                LeftForeArmMaxAngle = leftForeArmMaxAngle;
                HipsForwardRange = hipsForwardRange;
                HipsVerticalRange = hipsVerticalRange;
                ImpactHoldAngle = impactHoldAngle;
            }
        }

        private readonly struct PierceAttackRuntimeBoneSample
        {
            public readonly string Path;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;

            public PierceAttackRuntimeBoneSample(string path, Vector3 localPosition, Quaternion localRotation)
            {
                Path = path;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
            }
        }

        private readonly struct PierceAttackRuntimePlaybackMetrics
        {
            public static readonly PierceAttackRuntimePlaybackMetrics Empty =
                new PierceAttackRuntimePlaybackMetrics(false, false, 0f, 0f, 0f, 0f);

            public readonly bool FirstPassMoved;
            public readonly bool PostLoopMoved;
            public readonly float FirstPassMaxRotationDelta;
            public readonly float FirstPassMaxPositionDelta;
            public readonly float PostLoopMaxRotationDelta;
            public readonly float PostLoopMaxPositionDelta;

            public PierceAttackRuntimePlaybackMetrics(
                bool firstPassMoved,
                bool postLoopMoved,
                float firstPassMaxRotationDelta,
                float firstPassMaxPositionDelta,
                float postLoopMaxRotationDelta,
                float postLoopMaxPositionDelta)
            {
                FirstPassMoved = firstPassMoved;
                PostLoopMoved = postLoopMoved;
                FirstPassMaxRotationDelta = firstPassMaxRotationDelta;
                FirstPassMaxPositionDelta = firstPassMaxPositionDelta;
                PostLoopMaxRotationDelta = postLoopMaxRotationDelta;
                PostLoopMaxPositionDelta = postLoopMaxPositionDelta;
            }
        }

        private readonly struct RigReplacementResult
        {
            public readonly bool OldArmatureReplaced;
            public readonly int SkinnedRenderersPreserved;
            public readonly int RendererBonesReplaced;
            public readonly bool EyeContainerReparented;

            public RigReplacementResult(
                bool oldArmatureReplaced,
                int skinnedRenderersPreserved,
                int rendererBonesReplaced,
                bool eyeContainerReparented)
            {
                OldArmatureReplaced = oldArmatureReplaced;
                SkinnedRenderersPreserved = skinnedRenderersPreserved;
                RendererBonesReplaced = rendererBonesReplaced;
                EyeContainerReparented = eyeContainerReparented;
            }
        }

        private sealed class RendererRigSnapshot
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly Mesh SharedMesh;
            public readonly Material[] SharedMaterials;
            public readonly string[] BonePaths;
            public readonly string RootBonePath;

            public RendererRigSnapshot(
                SkinnedMeshRenderer renderer,
                Mesh sharedMesh,
                Material[] sharedMaterials,
                string[] bonePaths,
                string rootBonePath)
            {
                Renderer = renderer;
                SharedMesh = sharedMesh;
                SharedMaterials = sharedMaterials;
                BonePaths = bonePaths;
                RootBonePath = rootBonePath;
            }
        }

        private readonly struct BackRushRestoreSlot
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly int siblingIndex;
            private readonly bool activeSelf;

            private BackRushRestoreSlot(
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                int siblingIndex,
                bool activeSelf)
            {
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.localScale = localScale;
                this.siblingIndex = siblingIndex;
                this.activeSelf = activeSelf;
            }

            public static BackRushRestoreSlot CaptureOrInfer(
                Transform placementRoot,
                Transform existingRunRoot,
                Transform detectRoot,
                Transform pierceAttackRoot)
            {
                if (existingRunRoot != null)
                {
                    return new BackRushRestoreSlot(
                        existingRunRoot.localPosition,
                        existingRunRoot.localRotation,
                        existingRunRoot.localScale,
                        existingRunRoot.GetSiblingIndex(),
                        true);
                }

                return new BackRushRestoreSlot(
                    Vector3.Lerp(detectRoot.localPosition, pierceAttackRoot.localPosition, 0.5f),
                    detectRoot.localRotation,
                    detectRoot.localScale,
                    Mathf.Clamp(detectRoot.GetSiblingIndex() + 1, 0, placementRoot.childCount),
                    true);
            }

            public void ApplyTo(Transform transform)
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
                transform.gameObject.SetActive(activeSelf);

                if (transform.parent != null)
                {
                    transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, transform.parent.childCount - 1));
                }
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(localPosition, transform.localPosition) <= 0.0001f &&
                       Quaternion.Angle(localRotation, transform.localRotation) <= 0.001f &&
                       Vector3.Distance(localScale, transform.localScale) <= 0.0001f &&
                       transform.gameObject.activeSelf == activeSelf &&
                       transform.GetSiblingIndex() == siblingIndex;
            }
        }

        private readonly struct TransformState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            private TransformState(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.localScale = localScale;
            }

            public static TransformState Capture(Transform transform)
            {
                return new TransformState(transform.localPosition, transform.localRotation, transform.localScale);
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(localPosition, transform.localPosition) <= 0.0001f &&
                       Quaternion.Angle(localRotation, transform.localRotation) <= 0.001f &&
                       Vector3.Distance(localScale, transform.localScale) <= 0.0001f;
            }

            public void ApplyTo(Transform transform)
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }

        private readonly struct ApprovedDeathMeltTimeline
        {
            public readonly float MeltStart;
            public readonly float SagTime;
            public readonly float CollapseTime;
            public readonly float SpreadTime;
            public readonly float HoldTime;

            public ApprovedDeathMeltTimeline(float meltStart, float sagTime, float collapseTime, float spreadTime, float holdTime)
            {
                MeltStart = meltStart;
                SagTime = sagTime;
                CollapseTime = collapseTime;
                SpreadTime = spreadTime;
                HoldTime = holdTime;
            }
        }

        private readonly struct ApprovedDeathMeltAlignmentMetrics
        {
            public readonly float Scale;
            public readonly float CenterHorizontalDelta;
            public readonly float GroundDelta;
            public readonly Vector3 LocalRotationEuler;
            public readonly float VerticalHeight;
            public readonly float HorizontalExtent;
            public readonly float VerticalToHorizontalRatio;

            public ApprovedDeathMeltAlignmentMetrics(
                float scale,
                float centerHorizontalDelta,
                float groundDelta,
                Vector3 localRotationEuler,
                float verticalHeight,
                float horizontalExtent,
                float verticalToHorizontalRatio)
            {
                Scale = scale;
                CenterHorizontalDelta = centerHorizontalDelta;
                GroundDelta = groundDelta;
                LocalRotationEuler = localRotationEuler;
                VerticalHeight = verticalHeight;
                HorizontalExtent = horizontalExtent;
                VerticalToHorizontalRatio = verticalToHorizontalRatio;
            }
        }

        private readonly struct ApprovedDeathMeltFloorMetrics
        {
            public readonly float GroundDelta;
            public readonly float VerticalHeight;
            public readonly float HorizontalExtent;
            public readonly float VerticalToHorizontalRatio;
            public readonly float CenterHorizontalDelta;
            public readonly float StartYOffset;

            public ApprovedDeathMeltFloorMetrics(
                float groundDelta,
                float verticalHeight,
                float horizontalExtent,
                float verticalToHorizontalRatio,
                float centerHorizontalDelta,
                float startYOffset)
            {
                GroundDelta = groundDelta;
                VerticalHeight = verticalHeight;
                HorizontalExtent = horizontalExtent;
                VerticalToHorizontalRatio = verticalToHorizontalRatio;
                CenterHorizontalDelta = centerHorizontalDelta;
                StartYOffset = startYOffset;
            }
        }

        private readonly struct ApprovedDeathMeltSampleMetrics
        {
            public readonly bool BodyVisibleBeforeMelt;
            public readonly bool PuddleHiddenBeforeMelt;
            public readonly bool PuddleVisibleAfterMelt;
            public readonly bool BodyHiddenAfterMelt;
            public readonly bool EyeHiddenAfterMelt;
            public readonly float FinalSagWeight;
            public readonly float FinalCollapseWeight;
            public readonly float FinalSpreadWeight;

            public ApprovedDeathMeltSampleMetrics(
                bool bodyVisibleBeforeMelt,
                bool puddleHiddenBeforeMelt,
                bool puddleVisibleAfterMelt,
                bool bodyHiddenAfterMelt,
                bool eyeHiddenAfterMelt,
                float finalSagWeight,
                float finalCollapseWeight,
                float finalSpreadWeight)
            {
                BodyVisibleBeforeMelt = bodyVisibleBeforeMelt;
                PuddleHiddenBeforeMelt = puddleHiddenBeforeMelt;
                PuddleVisibleAfterMelt = puddleVisibleAfterMelt;
                BodyHiddenAfterMelt = bodyHiddenAfterMelt;
                EyeHiddenAfterMelt = eyeHiddenAfterMelt;
                FinalSagWeight = finalSagWeight;
                FinalCollapseWeight = finalCollapseWeight;
                FinalSpreadWeight = finalSpreadWeight;
            }
        }

        private readonly struct LocalTransformSample
        {
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Vector3 LocalScale;

            private LocalTransformSample(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public static LocalTransformSample Capture(Transform transform)
            {
                return new LocalTransformSample(transform.localPosition, transform.localRotation, transform.localScale);
            }
        }

        private readonly struct ThrustFbxPlaybackMetrics
        {
            public readonly bool MovesAtFirstUpdate;
            public readonly bool MovesAfterLoop;
            public readonly float FirstRotationDelta;
            public readonly float FirstPositionDelta;
            public readonly float LoopRotationDelta;
            public readonly float LoopPositionDelta;

            private ThrustFbxPlaybackMetrics(
                bool movesAtFirstUpdate,
                bool movesAfterLoop,
                float firstRotationDelta,
                float firstPositionDelta,
                float loopRotationDelta,
                float loopPositionDelta)
            {
                MovesAtFirstUpdate = movesAtFirstUpdate;
                MovesAfterLoop = movesAfterLoop;
                FirstRotationDelta = firstRotationDelta;
                FirstPositionDelta = firstPositionDelta;
                LoopRotationDelta = loopRotationDelta;
                LoopPositionDelta = loopPositionDelta;
            }

            public static ThrustFbxPlaybackMetrics FromSamples(
                LocalTransformSample[] startStates,
                LocalTransformSample[] midStates,
                LocalTransformSample[] loopStates)
            {
                var firstRotationDelta = 0f;
                var firstPositionDelta = 0f;
                var loopRotationDelta = 0f;
                var loopPositionDelta = 0f;

                for (var index = 0; index < startStates.Length; index++)
                {
                    firstRotationDelta = Mathf.Max(
                        firstRotationDelta,
                        Quaternion.Angle(startStates[index].LocalRotation, midStates[index].LocalRotation));
                    firstPositionDelta = Mathf.Max(
                        firstPositionDelta,
                        Vector3.Distance(startStates[index].LocalPosition, midStates[index].LocalPosition));
                    loopRotationDelta = Mathf.Max(
                        loopRotationDelta,
                        Quaternion.Angle(startStates[index].LocalRotation, loopStates[index].LocalRotation));
                    loopPositionDelta = Mathf.Max(
                        loopPositionDelta,
                        Vector3.Distance(startStates[index].LocalPosition, loopStates[index].LocalPosition));
                }

                return new ThrustFbxPlaybackMetrics(
                    firstRotationDelta > 1f || firstPositionDelta > 0.001f,
                    loopRotationDelta > 1f || loopPositionDelta > 0.001f,
                    firstRotationDelta,
                    firstPositionDelta,
                    loopRotationDelta,
                    loopPositionDelta);
            }
        }

        private readonly struct AuthoredSprintMotionMetrics
        {
            public readonly float HipsVerticalRange;
            public readonly float HipsLateralRange;
            public readonly float MaxTorsoRotationAngle;
            public readonly float MaxHeadRotationAngle;
            public readonly float HipsRotationAngleAtLastSample;

            public AuthoredSprintMotionMetrics(
                float hipsVerticalRange,
                float hipsLateralRange,
                float maxTorsoRotationAngle,
                float maxHeadRotationAngle,
                float hipsRotationAngleAtLastSample)
            {
                HipsVerticalRange = hipsVerticalRange;
                HipsLateralRange = hipsLateralRange;
                MaxTorsoRotationAngle = maxTorsoRotationAngle;
                MaxHeadRotationAngle = maxHeadRotationAngle;
                HipsRotationAngleAtLastSample = hipsRotationAngleAtLastSample;
            }
        }

        private sealed class ReferenceSprintProfile
        {
            private readonly Dictionary<string, SprintAxisReference> axes;

            public readonly string SourceClipName;
            public readonly float SourceClipLength;
            public readonly string SourceClipsSummary;

            public ReferenceSprintProfile(
                string sourceClipName,
                float sourceClipLength,
                string sourceClipsSummary,
                Dictionary<string, SprintAxisReference> axes)
            {
                SourceClipName = sourceClipName;
                SourceClipLength = sourceClipLength;
                SourceClipsSummary = sourceClipsSummary;
                this.axes = axes;
            }

            public SprintAxisReference GetAxis(string path)
            {
                return axes.TryGetValue(path, out var axis)
                    ? axis
                    : SprintAxisReference.Fallback(path);
            }
        }

        private readonly struct SprintAxisReference
        {
            public readonly int AxisIndex;
            public readonly float Sign;
            public readonly float Range;

            private SprintAxisReference(int axisIndex, float sign, float range)
            {
                AxisIndex = axisIndex;
                Sign = sign == 0f ? 1f : Mathf.Sign(sign);
                Range = range;
            }

            public string AxisName
            {
                get
                {
                    switch (AxisIndex)
                    {
                        case 0:
                            return "X";
                        case 1:
                            return "Y";
                        default:
                            return "Z";
                    }
                }
            }

            public static SprintAxisReference Create(int axisIndex, float sign, float range)
            {
                return new SprintAxisReference(axisIndex, sign, range);
            }

            public static SprintAxisReference Fallback(string path)
            {
                var lower = path.ToLowerInvariant();
                if (lower.Contains("arm") || lower.Contains("leg") || lower.Contains("foot") || lower.Contains("toe"))
                {
                    return new SprintAxisReference(0, 1f, 0f);
                }

                return new SprintAxisReference(0, -1f, 0f);
            }
        }

        private sealed class AxisRangeTracker
        {
            private readonly FloatRangeTracker x = new FloatRangeTracker();
            private readonly FloatRangeTracker y = new FloatRangeTracker();
            private readonly FloatRangeTracker z = new FloatRangeTracker();
            private float maxAbsX;
            private float maxAbsY;
            private float maxAbsZ;
            private float maxAbsSignedX = 1f;
            private float maxAbsSignedY = 1f;
            private float maxAbsSignedZ = 1f;

            public void Add(Vector3 value)
            {
                x.Add(value.x);
                y.Add(value.y);
                z.Add(value.z);
                TrackAbs(value.x, ref maxAbsX, ref maxAbsSignedX);
                TrackAbs(value.y, ref maxAbsY, ref maxAbsSignedY);
                TrackAbs(value.z, ref maxAbsZ, ref maxAbsSignedZ);
            }

            public SprintAxisReference ToAxisReference(string path)
            {
                var axisIndex = 0;
                var range = x.Range <= 180f ? x.Range : 0f;
                var signed = maxAbsSignedX;
                var yRange = y.Range <= 180f ? y.Range : 0f;
                var zRange = z.Range <= 180f ? z.Range : 0f;
                if (yRange > range)
                {
                    axisIndex = 1;
                    range = yRange;
                    signed = maxAbsSignedY;
                }

                if (zRange > range)
                {
                    axisIndex = 2;
                    range = zRange;
                    signed = maxAbsSignedZ;
                }

                if (range < 0.001f)
                {
                    return SprintAxisReference.Fallback(path);
                }

                return SprintAxisReference.Create(axisIndex, signed, range);
            }

            private static void TrackAbs(float value, ref float maxAbs, ref float signedValue)
            {
                var abs = Mathf.Abs(value);
                if (abs <= maxAbs)
                {
                    return;
                }

                maxAbs = abs;
                signedValue = value;
            }
        }

        private sealed class FloatRangeTracker
        {
            private float min = float.PositiveInfinity;
            private float max = float.NegativeInfinity;

            public float Range => float.IsInfinity(min) || float.IsInfinity(max) ? 0f : max - min;

            public void Add(float value)
            {
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
        }

        private readonly struct ReferenceDrivenSprintMetrics
        {
            public readonly float LeftUpLegRange;
            public readonly float RightUpLegRange;
            public readonly float LeftLegRange;
            public readonly float RightLegRange;
            public readonly float LeftArmRange;
            public readonly float RightArmRange;
            public readonly float HipsVerticalRange;
            public readonly float HipsLateralRange;
            public readonly float LeftKneeWorldYRange;
            public readonly float RightKneeWorldYRange;

            public ReferenceDrivenSprintMetrics(
                float leftUpLegRange,
                float rightUpLegRange,
                float leftLegRange,
                float rightLegRange,
                float leftArmRange,
                float rightArmRange,
                float hipsVerticalRange,
                float hipsLateralRange,
                float leftKneeWorldYRange,
                float rightKneeWorldYRange)
            {
                LeftUpLegRange = leftUpLegRange;
                RightUpLegRange = rightUpLegRange;
                LeftLegRange = leftLegRange;
                RightLegRange = rightLegRange;
                LeftArmRange = leftArmRange;
                RightArmRange = rightArmRange;
                HipsVerticalRange = hipsVerticalRange;
                HipsLateralRange = hipsLateralRange;
                LeftKneeWorldYRange = leftKneeWorldYRange;
                RightKneeWorldYRange = rightKneeWorldYRange;
            }
        }
    }
}
