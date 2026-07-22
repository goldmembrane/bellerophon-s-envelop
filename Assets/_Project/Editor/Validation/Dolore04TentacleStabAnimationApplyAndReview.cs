using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore04TentacleStabAnimation
{
    internal static class Dolore04TentacleStabAnimationApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string SlotName = "Dolore_04_Tentacle_Stab_Attack";
        private const string ExecutionSlotName = "Dolore_05_Execution_Pull_In";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string RootBoneName = "Bone_000";
        private const string TipBoneName = "Bone_001";
        private const int ExpectedBoneCount = 13;
        private const int RingBoneIndex = 1;
        private const int FirstAnimatedBoneIndex = 4;
        private const int CaptureLayer = 31;
        private const float SourceGenerationDuration = 0.5f;
        private const float SourceGenerationMidTime = SourceGenerationDuration * 0.5f;
        private const float SourceHiddenScaleFactor = 0.001f;
        private const float TipPassSampleTime = 0.8f;
        private const float HiddenBehindSurfaceDistance = 0.03f;
        // After the tip threads through the ring, it clears the frame in depth before any meaningful rise.
        private const float FrontClearTime = 1.05f;
        private const float DiagonalExitTime = 1.18f;
        private const float EarlyRiseTime = 1.30f;
        private const float MidRiseTime = 1.55f;
        private const float FullChainClearTime = 1.78f;
        private const float EmergenceFrameClearance = 0.48f;
        private const float MinimumPreRiseFrameClearance = 0.40f;
        private const float MaximumPreClearRise = 0.12f;
        private const float MaximumRingConnectionHeight = 0.10f;
        private const float MaximumDiagonalExitBaseDistance = 0.20f;
        private const float ActiveFrameClearance = 0.30f;
        private const float MinimumActiveFrameClearance = 0.20f;
        private const float MinimumMovingVertexWeight = 0.75f;
        private const float FrontSurfaceThreshold = 0.01f;
        private const float MinimumEmergenceVertexHeight = -0.08f;
        private const float BodyHideEndTime = 2.35f;
        private const float RingHideStartTime = 2.35f;
        private const float RiseDuration = 1.5f;
        private const float IntroDuration = SourceGenerationDuration + RiseDuration;
        private const float StrikeDuration = 0.5f;
        private const float RecoverDuration = 0.8f;
        private const float SingleAttackDuration = StrikeDuration + RecoverDuration;
        private const float SecondStrikeTime = SingleAttackDuration + StrikeDuration;
        private const float WindupTime = 0.16f;
        private const float WindupHoldTime = 0.28f;
        private const float AccelerationTime = 0.38f;
        private const float NearImpactTime = 0.44f;
        private const float ImpactTime = 0.46f;
        private const float ImpactHoldEndTime = 0.58f;
        private const float ReboundTime = 0.72f;
        private const float RisingRecoveryTime = 1.02f;
        private const float LoopDuration = SingleAttackDuration * 2f;
        private const float RevealTime = 1f / 60f;
        private const float RiseStartTime = SourceGenerationDuration;
        private const float TransformTolerance = 0.00001f;
        private const float MinimumIntroRise = 0.25f;
        private const float MinimumStrikeDrop = 0.20f;
        private const float MinimumStrikeForward = 0.12f;
        private const float MaximumSurfaceAnchorDrift = 0.002f;
        private const float MaximumStrikeLateral = 0.20f;
        private const float MinimumPreparedHeight = 0.45f;
        private const float MinimumWindupLift = 0.10f;
        private const float MinimumWindupRetreat = 0.05f;
        private const float MinimumLateStrikeOutward = 0.35f;
        private const float MaximumImpactHoldError = 0.01f;

        private const string AssetRoot =
            "Assets/_Project/Art/Generated/Enemies/Dolore/AttackAttachment";
        private const string AnimationFolder = AssetRoot + "/Animations";
        private const string ReviewFolder = AssetRoot + "/Review";
        private const string IntroClipPath = AnimationFolder + "/Dolore_04_TentacleStab_Intro.anim";
        private const string LoopClipPath = AnimationFolder + "/Dolore_04_TentacleStab_AttackLoop.anim";
        private const string ControllerPath = AnimationFolder + "/Dolore_04_TentacleStab.controller";
        private const string InspectionPath = ReviewFolder + "/Dolore_04_TentacleStab_Inspection.txt";
        private const string CapturePath = ReviewFolder + "/Dolore_04_TentacleStab_Animation.png";

        private static readonly string[] ExpectedSlotNames =
        {
            "Dolore_01_Static_Review",
            "Dolore_02_Idle",
            "Dolore_03_Move_Quadruped",
            SlotName,
            ExecutionSlotName,
            "Dolore_06_Hit_Reaction",
            "Dolore_07_Death"
        };

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 3 Tentacle Stab Target")]
        public static void InspectTarget()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var slots = RequireSlots(scene);
            var target = RequireTarget(slots[3]);
            var chain = RequireBoneChain(target.Renderer);
            var report = new StringBuilder()
                .Append("Dolore04TentacleStabTargetInspected Result=PASS")
                .Append(" Scene=").Append(scene.path)
                .Append(" SceneDirty=").Append(wasDirty)
                .Append(" SlotLocalPosition=").Append(Vec(slots[3].localPosition))
                .Append(" SlotLocalRotation=").Append(Quat(slots[3].localRotation))
                .Append(" SlotLocalScale=").Append(Vec(slots[3].localScale))
                .Append(" AttachmentLocalPosition=").Append(Vec(target.Attachment.localPosition))
                .Append(" AttachmentLocalRotation=").Append(Quat(target.Attachment.localRotation))
                .Append(" AttachmentLocalScale=").Append(Vec(target.Attachment.localScale))
                .Append(" Renderer=").Append(PathFrom(target.Renderer.transform, target.Attachment))
                .Append(" Mesh=").Append(AssetDatabase.GetAssetPath(target.Renderer.sharedMesh))
                .Append(" Bones=").Append(chain.Count);
            foreach (var bone in chain)
            {
                report.Append(" [")
                    .Append(bone.name)
                    .Append(" Path=").Append(PathFrom(bone, target.Attachment))
                    .Append(" Parent=").Append(bone.parent != null ? bone.parent.name : "<null>")
                    .Append(" P=").Append(Vec(bone.localPosition))
                    .Append(" R=").Append(Quat(bone.localRotation))
                    .Append(" S=").Append(Vec(bone.localScale))
                    .Append(']');
            }
            var boundaryIndices = AttackRootSurfaceVertexIndices(target.Renderer);
            var legacyWeights = target.Renderer.sharedMesh.boneWeights;
            foreach (var vertexIndex in boundaryIndices)
            {
                var weight = legacyWeights[vertexIndex];
                report.Append(" [BoundaryVertex=").Append(vertexIndex)
                    .Append(" Weights=")
                    .Append(BoneWeightText(weight.boneIndex0, weight.weight0, target.Renderer)).Append(',')
                    .Append(BoneWeightText(weight.boneIndex1, weight.weight1, target.Renderer)).Append(',')
                    .Append(BoneWeightText(weight.boneIndex2, weight.weight2, target.Renderer)).Append(',')
                    .Append(BoneWeightText(weight.boneIndex3, weight.weight3, target.Renderer)).Append(']');
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Target inspection changed CargoRunMvp.");
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Motion 3 Tentacle Stab Animation")]
        public static void ApplyAnimation()
        {
            var scene = RequireActiveScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp contains unsaved changes. The animation tool will not overwrite them.");
            var slots = RequireSlots(scene);
            var target = RequireTarget(slots[3]);
            var execution = RequireTarget(slots[4]);
            RequireMatchingAttachmentAnchor(target, execution);
            var protectedRootsBefore = ProtectedRootSignatures(scene);
            var protectedSlotsBefore = ProtectedSlotSignatures(slots);
            var targetBaseBefore = HierarchySignature(RequireModel(slots[3]), AttachmentName);
            var attachmentTransformBefore = TransformSignature(target.Attachment);
            var sourceTransformBefore = TransformSignature(target.Source);
            var rootBoneBefore = TransformSignature(target.RootBone);

            EnsureFolder(AnimationFolder);
            var assets = CreateOrUpdateAnimationAssets(target, ResolveOutwardDirection(scene, target));
            var animator = target.Attachment.GetComponent<Animator>();
            if (animator == null) animator = target.Attachment.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = assets.Controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            target.Renderer.updateWhenOffscreen = false;
            EditorUtility.SetDirty(target.Renderer);

            if (TransformSignature(target.Attachment) != attachmentTransformBefore ||
                TransformSignature(target.Source) != sourceTransformBefore ||
                TransformSignature(target.RootBone) != rootBoneBefore)
                throw new InvalidOperationException("The approved tentacle start transform changed during animation setup.");
            if (HierarchySignature(RequireModel(slots[3]), AttachmentName) != targetBaseBefore)
                throw new InvalidOperationException("The Dolore motion 3 base model changed.");
            if (!protectedRootsBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
                throw new InvalidOperationException("A scene root outside Approved Dolore Enemy Placement changed.");
            if (!protectedSlotsBefore.SequenceEqual(ProtectedSlotSignatures(slots), StringComparer.Ordinal))
                throw new InvalidOperationException("A Dolore slot outside motion object 3 changed.");
            RequireMatchingAttachmentAnchor(RequireTarget(slots[3]), execution);

            InspectState(scene, false);
            var animatedBounds = CalculateAnimatedLocalBounds(target, assets.Intro, assets.Loop, false);
            animatedBounds.Expand(Vector3.Max(animatedBounds.size * 0.18f, Vector3.one * 0.5f));
            target.Renderer.localBounds = animatedBounds;
            target.Renderer.updateWhenOffscreen = false;
            EditorUtility.SetDirty(target.Renderer);
            var metrics = InspectState(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            WriteInspection(metrics, "Apply", true);
            Debug.Log("Dolore04TentacleStabAnimationApplied Result=PASS IntroSeconds=2 " +
                      "SourceGenerationSeconds=0.5 RiseSeconds=1.5 StrikeSeconds=0.5 " +
                      "RecoverSeconds=0.8 BuiltInRigBones=13 RootBoneAnimated=False " +
                      "OtherDoloreSlotsChanged=False OtherSceneRootsChanged=False SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 3 Tentacle Stab Animation")]
        public static void InspectAnimation()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var metrics = InspectState(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Animation inspection changed CargoRunMvp.");
            Debug.Log("Dolore04TentacleStabAnimationInspected Result=PASS IntroSeconds=" +
                      Num(metrics.IntroLength) + " SourceGenerationSeconds=0.5 RiseSeconds=1.5 " +
                      "StrikeSeconds=0.5 RecoverSeconds=0.8 IntroRise=" +
                      Num(metrics.IntroRise) + " StrikeForward=" + Num(metrics.StrikeForward) +
                      " StrikeDrop=" + Num(metrics.StrikeDrop) + " SecondStrikeForward=" +
                      Num(metrics.SecondStrikeForward) + " SecondStrikeDrop=" +
                      Num(metrics.SecondStrikeDrop) + " FirstReturnError=" +
                      Num(metrics.ReturnError) + " RootDrift=" + Num(metrics.RootDrift) +
                      " SurfaceAnchorDrift=" + Num(metrics.SurfaceAnchorDrift) +
                      " StrikeLateral=" + Num(metrics.StrikeLateral) +
                      " SecondStrikeLateral=" + Num(metrics.SecondStrikeLateral) +
                      " FirstWindupLift=" + Num(metrics.FirstWindupLift) +
                      " FirstWindupRetreat=" + Num(metrics.FirstWindupRetreat) +
                      " FirstLateStrikeOutward=" + Num(metrics.FirstLateStrikeOutward) +
                      " FirstImpactHoldError=" + Num(metrics.FirstImpactHoldError) +
                      " SecondWindupLift=" + Num(metrics.SecondWindupLift) +
                      " SecondWindupRetreat=" + Num(metrics.SecondWindupRetreat) +
                      " SecondLateStrikeOutward=" + Num(metrics.SecondLateStrikeOutward) +
                      " SecondImpactHoldError=" + Num(metrics.SecondImpactHoldError) +
                      " HiddenReturnError=" + Num(metrics.HiddenReturnError) +
                      " HiddenPoseError=" + Num(metrics.HiddenPoseError) +
                      " IntroHiddenChainOffset=" + Num(metrics.IntroHiddenChainOffset) +
                      " SourceReadyChainOffset=" + Num(metrics.SourceReadyChainOffset) +
                      " LoopHiddenChainOffset=" + Num(metrics.LoopHiddenChainOffset) +
                      " SourceHiddenScaleRatio=" + Num(metrics.SourceHiddenScaleRatio) +
                      " SourceMidScaleRatio=" + Num(metrics.SourceMidScaleRatio) +
                      " SourceReadyScaleError=" + Num(metrics.SourceReadyScaleError) +
                      " InitialRiseUp=" + Num(metrics.InitialRiseUp) +
                      " InitialRiseLateral=" + Num(metrics.InitialRiseLateral) +
                      " TipFrontClearanceBeforeRise=" + Num(metrics.TipFrontClearanceBeforeRise) +
                      " TipRiseBeforeFrontClear=" + Num(metrics.TipRiseBeforeFrontClear) +
                      " DiagonalExitBaseDistance=" + Num(metrics.DiagonalExitBaseDistance) +
                      " EarlyRiseConnectionHeight=" + Num(metrics.EarlyRiseConnectionHeight) +
                      " MidRiseConnectionHeight=" + Num(metrics.MidRiseConnectionHeight) +
                      " MinimumActiveFrameClearance=" + Num(metrics.MinimumActiveFrameClearance) +
                      " MinimumEmergenceVisibleVertexHeight=" + Num(metrics.MinimumEmergenceVisibleVertexHeight) +
                      " FirstStrikeTipAim=" + Num(metrics.FirstStrikeTipAim) +
                      " FirstStrikeTipLead=" + Num(metrics.FirstStrikeTipLead) +
                      " SecondStrikeTipAim=" + Num(metrics.SecondStrikeTipAim) +
                      " SecondStrikeTipLead=" + Num(metrics.SecondStrikeTipLead) +
                      " PreparedHeight=" + Num(metrics.PreparedHeight) +
                      " AnimatedRigBones=" + metrics.AnimatedBoneCount + " SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Motion 3 Tentacle Stab Animation")]
        public static void CaptureAnimation()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var metrics = InspectState(scene);
            var slots = RequireSlots(scene);
            var target = RequireTarget(slots[3]);
            var intro = RequireAsset<AnimationClip>(IntroClipPath);
            var loop = RequireAsset<AnimationClip>(LoopClipPath);
            var poses = new[]
            {
                new PoseSample(intro, 0f),
                new PoseSample(intro, 0.25f),
                new PoseSample(intro, SourceGenerationDuration),
                new PoseSample(intro, SourceGenerationDuration + 0.75f),
                new PoseSample(intro, IntroDuration),
                new PoseSample(loop, 0f),
                new PoseSample(loop, StrikeDuration),
                new PoseSample(loop, SingleAttackDuration),
                new PoseSample(loop, SecondStrikeTime),
                new PoseSample(loop, LoopDuration)
            };
            var images = CapturePoses(RequireModel(slots[3]), poses);
            try
            {
                SaveSheet(images, ProjectAbsolutePath(CapturePath));
            }
            finally
            {
                foreach (var image in images)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
            }
            AssetDatabase.ImportAsset(
                CapturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Animation capture changed CargoRunMvp.");
            Debug.Log("Dolore04TentacleStabAnimationCaptured Result=PASS Image=" + CapturePath +
                      " Layout=Intro[0|0.25|0.5|1.25|2];AttackLoop[0|0.5|1.3|1.8|2.6] " +
                      "RootDrift=" + Num(metrics.RootDrift) + " SceneChanged=False.");
        }

        private static AnimationAssets CreateOrUpdateAnimationAssets(Target target, Vector3 outward)
        {
            var chain = RequireBoneChain(target.Renderer);
            var saved = chain.Select(BoneState.Capture).ToArray();
            Quaternion[] hidden;
            Quaternion[] entering;
            Quaternion[] passing;
            Quaternion[] frontCleared;
            Quaternion[] diagonalExit;
            Quaternion[] risingEarly;
            Quaternion[] risingMid;
            Quaternion[] fullyEmerged;
            Quaternion[] prepared;
            Quaternion[] windup;
            Quaternion[] strike;
            Vector3 enteringBasePosition;
            Vector3 passingBasePosition;
            Vector3 frontClearedBasePosition;
            Vector3 diagonalExitBasePosition;
            Vector3 earlyRiseBasePosition;
            Vector3 midRiseBasePosition;
            Vector3 fullyEmergedBasePosition;
            Vector3 preparedBasePosition;
            Vector3 windupBasePosition;
            Vector3 strikeBasePosition;
            Vector3 hiddenBasePosition;
            try
            {
                var surfaceCenter = AttackRootSurfaceCenter(target.Renderer);
                hidden = BuildDirectionPose(chain, saved, outward, _ => 0f, 0f);
                hiddenBasePosition = BasePositionForWorldTip(
                    chain[FirstAnimatedBoneIndex], chain[chain.Count - 1],
                    surfaceCenter - outward * HiddenBehindSurfaceDistance);
                entering = BuildDirectionPose(chain, saved, outward, index => index < 7 ? 0f : 55f, 2f);
                enteringBasePosition = BasePositionForWorldTip(
                    chain[FirstAnimatedBoneIndex], chain[chain.Count - 1],
                    surfaceCenter + outward * 0.02f + Vector3.up * 0.015f);
                passing = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    _ => 0f,
                    2f);
                passingBasePosition = BasePositionForWorldTip(
                    chain[FirstAnimatedBoneIndex], chain[chain.Count - 1],
                    surfaceCenter + outward * 0.18f + Vector3.up * 0.04f);
                frontCleared = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    _ => 0f,
                    2f);
                frontClearedBasePosition = BasePositionForWorldTip(
                    chain[FirstAnimatedBoneIndex], chain[chain.Count - 1],
                    surfaceCenter + outward * EmergenceFrameClearance + Vector3.up * 0.08f);
                diagonalExit = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    _ => 20f,
                    3f);
                diagonalExitBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex], surfaceCenter, outward, 0f);
                risingEarly = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    index => new[] { 12f, 18f, 26f, 36f, 48f, 60f, 74f, 86f }[index],
                    4f);
                earlyRiseBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex], surfaceCenter, outward, 0f);
                risingMid = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    index => new[] { 20f, 30f, 42f, 54f, 66f, 76f, 84f, 89f }[index],
                    4f);
                midRiseBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex], surfaceCenter, outward, 0f);
                fullyEmerged = BuildDirectionPose(chain, saved, outward, index => 70f + index * 0.45f, 4f);
                fullyEmergedBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex], surfaceCenter + outward * EmergenceFrameClearance, outward, 70f);
                prepared = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    index =>
                    {
                        var t = index / 7f;
                        return 70f + 20f * Mathf.Sin(Mathf.PI * t) + 3f * t;
                    },
                    10f);
                windup = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    index =>
                    {
                        var t = index / 7f;
                        return 102f + 22f * Mathf.Sin(Mathf.PI * t) + 4f * t;
                    },
                    22f);
                strike = BuildDirectionPose(
                    chain,
                    saved,
                    outward,
                    index =>
                    {
                        var t = index / 7f;
                        return -2f - 48f * Mathf.Pow(t, 1.05f);
                    },
                    -46f);
                preparedBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex], surfaceCenter + outward * ActiveFrameClearance, outward, 70f);
                windupBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex],
                    surfaceCenter + outward * ActiveFrameClearance + Vector3.up * 0.32f,
                    outward,
                    102f);
                strikeBasePosition = DirectionalChildPositionFromWorldStart(
                    chain[FirstAnimatedBoneIndex],
                    surfaceCenter + outward * (ActiveFrameClearance + 0.18f),
                    outward,
                    -4f,
                    2.05f);
            }
            finally
            {
                Restore(chain, saved);
            }

            var intro = CreateOrResetClip(IntroClipPath, false);
            var loop = CreateOrResetClip(LoopClipPath, false);
            for (var boneIndex = FirstAnimatedBoneIndex; boneIndex < chain.Count; boneIndex++)
            {
                var path = AnimationUtility.CalculateTransformPath(chain[boneIndex], target.Attachment);
                var defaultPosition = saved[boneIndex].Position;
                var finalIntroPosition = boneIndex == FirstAnimatedBoneIndex
                    ? preparedBasePosition
                    : defaultPosition;
                var hiddenPosition = boneIndex == FirstAnimatedBoneIndex ? hiddenBasePosition : defaultPosition;
                WriteVectorCurves(
                    intro,
                    path,
                    "m_LocalPosition",
                    new[]
                    {
                        new TimedVector(0f, hiddenPosition),
                        new TimedVector(RiseStartTime, hiddenPosition),
                        new TimedVector(0.65f, boneIndex == FirstAnimatedBoneIndex ? enteringBasePosition : defaultPosition),
                        new TimedVector(TipPassSampleTime, boneIndex == FirstAnimatedBoneIndex ? passingBasePosition : defaultPosition),
                        new TimedVector(FrontClearTime, boneIndex == FirstAnimatedBoneIndex ? frontClearedBasePosition : defaultPosition),
                        new TimedVector(DiagonalExitTime, boneIndex == FirstAnimatedBoneIndex ? diagonalExitBasePosition : defaultPosition),
                        new TimedVector(EarlyRiseTime, boneIndex == FirstAnimatedBoneIndex ? earlyRiseBasePosition : defaultPosition),
                        new TimedVector(MidRiseTime, boneIndex == FirstAnimatedBoneIndex ? midRiseBasePosition : defaultPosition),
                        new TimedVector(FullChainClearTime, boneIndex == FirstAnimatedBoneIndex ? fullyEmergedBasePosition : defaultPosition),
                        new TimedVector(IntroDuration, finalIntroPosition)
                    });
                WriteQuaternionCurves(
                    intro,
                    path,
                    new[]
                    {
                        new TimedQuaternion(0f, hidden[boneIndex]),
                        new TimedQuaternion(RiseStartTime, hidden[boneIndex]),
                        new TimedQuaternion(0.65f, entering[boneIndex]),
                        new TimedQuaternion(TipPassSampleTime, passing[boneIndex]),
                        new TimedQuaternion(FrontClearTime, frontCleared[boneIndex]),
                        new TimedQuaternion(DiagonalExitTime, diagonalExit[boneIndex]),
                        new TimedQuaternion(EarlyRiseTime, risingEarly[boneIndex]),
                        new TimedQuaternion(MidRiseTime, risingMid[boneIndex]),
                        new TimedQuaternion(FullChainClearTime, fullyEmerged[boneIndex]),
                        new TimedQuaternion(IntroDuration, prepared[boneIndex])
                    });

                var secondWindupTime = SingleAttackDuration + WindupTime;
                var secondWindupHoldTime = SingleAttackDuration + WindupHoldTime;
                var secondAccelerationTime = SingleAttackDuration + AccelerationTime;
                var secondNearImpactTime = SingleAttackDuration + NearImpactTime;
                var secondImpactTime = SingleAttackDuration + ImpactTime;
                var secondImpactHoldEndTime = SingleAttackDuration + ImpactHoldEndTime;
                var secondReboundTime = SingleAttackDuration + ReboundTime;
                var secondRisingRecoveryTime = SingleAttackDuration + RisingRecoveryTime;
                var loopPositions = boneIndex == FirstAnimatedBoneIndex
                    ? new[]
                    {
                        new TimedVector(0f, preparedBasePosition),
                        new TimedVector(WindupTime, windupBasePosition),
                        new TimedVector(WindupHoldTime, windupBasePosition),
                        new TimedVector(AccelerationTime, Vector3.Lerp(windupBasePosition, strikeBasePosition, 0.48f)),
                        new TimedVector(NearImpactTime, Vector3.Lerp(windupBasePosition, strikeBasePosition, 0.88f)),
                        new TimedVector(ImpactTime, strikeBasePosition),
                        new TimedVector(StrikeDuration, strikeBasePosition),
                        new TimedVector(ImpactHoldEndTime, strikeBasePosition),
                        new TimedVector(ReboundTime, Vector3.Lerp(strikeBasePosition, preparedBasePosition, 0.22f)),
                        new TimedVector(RisingRecoveryTime, Vector3.Lerp(strikeBasePosition, preparedBasePosition, 0.78f)),
                        new TimedVector(SingleAttackDuration, preparedBasePosition),
                        new TimedVector(secondWindupTime, windupBasePosition),
                        new TimedVector(secondWindupHoldTime, windupBasePosition),
                        new TimedVector(secondAccelerationTime, Vector3.Lerp(windupBasePosition, strikeBasePosition, 0.48f)),
                        new TimedVector(secondNearImpactTime, Vector3.Lerp(windupBasePosition, strikeBasePosition, 0.88f)),
                        new TimedVector(secondImpactTime, strikeBasePosition),
                        new TimedVector(SecondStrikeTime, strikeBasePosition),
                        new TimedVector(secondImpactHoldEndTime, strikeBasePosition),
                        new TimedVector(secondReboundTime, Vector3.Lerp(strikeBasePosition, preparedBasePosition, 0.22f)),
                        new TimedVector(secondRisingRecoveryTime, Vector3.Lerp(strikeBasePosition, preparedBasePosition, 0.78f)),
                        new TimedVector(BodyHideEndTime, preparedBasePosition),
                        new TimedVector(LoopDuration, preparedBasePosition)
                    }
                    : new[]
                    {
                        new TimedVector(0f, defaultPosition),
                        new TimedVector(SingleAttackDuration, defaultPosition),
                        new TimedVector(SecondStrikeTime, defaultPosition),
                        new TimedVector(BodyHideEndTime, defaultPosition),
                        new TimedVector(LoopDuration, defaultPosition)
                    };
                WriteVectorCurves(loop, path, "m_LocalPosition", loopPositions);
                var chainT = (boneIndex - FirstAnimatedBoneIndex) / 8f;
                var strikeLaunch = Quaternion.Slerp(
                    windup[boneIndex],
                    strike[boneIndex],
                    Mathf.Lerp(0.42f, 0.82f, chainT));
                var strikeNearImpact = Quaternion.Slerp(
                    windup[boneIndex],
                    strike[boneIndex],
                    Mathf.Lerp(0.84f, 0.98f, chainT));
                var rebound = Quaternion.Slerp(
                    strike[boneIndex],
                    prepared[boneIndex],
                    Mathf.Lerp(0.18f, 0.58f, chainT));
                var rising = Quaternion.Slerp(
                    strike[boneIndex],
                    prepared[boneIndex],
                    Mathf.Lerp(0.68f, 0.84f, chainT));
                WriteQuaternionCurves(
                    loop,
                    path,
                    new[]
                    {
                        new TimedQuaternion(0f, prepared[boneIndex]),
                        new TimedQuaternion(WindupTime, windup[boneIndex]),
                        new TimedQuaternion(WindupHoldTime, windup[boneIndex]),
                        new TimedQuaternion(AccelerationTime, strikeLaunch),
                        new TimedQuaternion(NearImpactTime, strikeNearImpact),
                        new TimedQuaternion(ImpactTime, strike[boneIndex]),
                        new TimedQuaternion(StrikeDuration, strike[boneIndex]),
                        new TimedQuaternion(ImpactHoldEndTime, strike[boneIndex]),
                        new TimedQuaternion(ReboundTime, rebound),
                        new TimedQuaternion(RisingRecoveryTime, rising),
                        new TimedQuaternion(SingleAttackDuration, prepared[boneIndex]),
                        new TimedQuaternion(secondWindupTime, windup[boneIndex]),
                        new TimedQuaternion(secondWindupHoldTime, windup[boneIndex]),
                        new TimedQuaternion(secondAccelerationTime, strikeLaunch),
                        new TimedQuaternion(secondNearImpactTime, strikeNearImpact),
                        new TimedQuaternion(secondImpactTime, strike[boneIndex]),
                        new TimedQuaternion(SecondStrikeTime, strike[boneIndex]),
                        new TimedQuaternion(secondImpactHoldEndTime, strike[boneIndex]),
                        new TimedQuaternion(secondReboundTime, Quaternion.Slerp(strike[boneIndex], prepared[boneIndex], 0.22f)),
                        new TimedQuaternion(secondRisingRecoveryTime, rising),
                        new TimedQuaternion(BodyHideEndTime, prepared[boneIndex]),
                        new TimedQuaternion(LoopDuration, prepared[boneIndex])
                    });
                if (boneIndex == FirstAnimatedBoneIndex)
                {
                    var hiddenBodyScale = saved[boneIndex].Scale * SourceHiddenScaleFactor;
                    WriteVectorCurves(
                        intro,
                        path,
                        "m_LocalScale",
                        new[]
                        {
                            new TimedVector(0f, saved[boneIndex].Scale),
                            new TimedVector(IntroDuration, saved[boneIndex].Scale)
                        });
                    WriteVectorCurves(
                        loop,
                        path,
                        "m_LocalScale",
                        new[]
                        {
                        new TimedVector(0f, saved[boneIndex].Scale),
                            new TimedVector(secondImpactHoldEndTime, saved[boneIndex].Scale),
                            new TimedVector(BodyHideEndTime, hiddenBodyScale),
                            new TimedVector(LoopDuration, hiddenBodyScale)
                        });
                }
            }
            var ringBone = chain[RingBoneIndex];
            var ringPath = AnimationUtility.CalculateTransformPath(ringBone, target.Attachment);
            var ringSurfacePivot = AttackRootSurfaceCenter(target.Renderer);
            var ringDefaultPosition = saved[RingBoneIndex].Position;
            var ringDefaultScale = saved[RingBoneIndex].Scale;
            var ringHiddenScale = ringDefaultScale * SourceHiddenScaleFactor;
            var ringMidScale = ringDefaultScale * 0.5f;
            var ringHiddenPosition = ScaleAroundWorldPivotLocalPosition(
                ringBone, target.Renderer, ringSurfacePivot, SourceHiddenScaleFactor);
            var ringMidPosition = ScaleAroundWorldPivotLocalPosition(
                ringBone, target.Renderer, ringSurfacePivot, 0.5f);
            WriteVectorCurves(
                intro,
                ringPath,
                "m_LocalPosition",
                new[]
                {
                    new TimedVector(0f, ringHiddenPosition),
                    new TimedVector(RevealTime, ringHiddenPosition),
                    new TimedVector(SourceGenerationMidTime, ringMidPosition),
                    new TimedVector(SourceGenerationDuration, ringDefaultPosition),
                    new TimedVector(IntroDuration, ringDefaultPosition)
                });
            WriteVectorCurves(
                intro,
                ringPath,
                "m_LocalScale",
                new[]
                {
                    new TimedVector(0f, ringHiddenScale),
                    new TimedVector(RevealTime, ringHiddenScale),
                    new TimedVector(SourceGenerationMidTime, ringMidScale),
                    new TimedVector(SourceGenerationDuration, ringDefaultScale),
                    new TimedVector(IntroDuration, ringDefaultScale)
                });
            var ringHideMidTime = RingHideStartTime + (LoopDuration - RingHideStartTime) * 0.5f;
            WriteVectorCurves(
                loop,
                ringPath,
                "m_LocalPosition",
                new[]
                {
                    new TimedVector(0f, ringDefaultPosition),
                    new TimedVector(RingHideStartTime, ringDefaultPosition),
                    new TimedVector(ringHideMidTime, ringMidPosition),
                    new TimedVector(LoopDuration, ringHiddenPosition)
                });
            WriteVectorCurves(
                loop,
                ringPath,
                "m_LocalScale",
                new[]
                {
                    new TimedVector(0f, ringDefaultScale),
                    new TimedVector(RingHideStartTime, ringDefaultScale),
                    new TimedVector(ringHideMidTime, ringMidScale),
                    new TimedVector(LoopDuration, ringHiddenScale)
                });
            var rendererPath = AnimationUtility.CalculateTransformPath(target.Renderer.transform, target.Attachment);
            WriteVisibilityCurve(
                intro,
                rendererPath,
                new[]
                {
                    new Keyframe(0f, 0f),
                    new Keyframe(RevealTime, 1f),
                    new Keyframe(IntroDuration, 1f)
                });
            WriteVisibilityCurve(
                loop,
                rendererPath,
                new[]
                {
                    new Keyframe(0f, 1f),
                    new Keyframe(LoopDuration - RevealTime, 1f),
                    new Keyframe(LoopDuration, 0f)
                });
            intro.EnsureQuaternionContinuity();
            loop.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(intro);
            EditorUtility.SetDirty(loop);
            var controller = CreateOrUpdateController(intro, loop);
            AssetDatabase.SaveAssets();
            return new AnimationAssets(intro, loop, controller);
        }

        private static Vector3 DirectionalChildPositionFromWorldStart(
            Transform child,
            Vector3 worldStart,
            Vector3 outward,
            float angleDegrees,
            float lengthScale = 1f)
        {
            var parent = child.parent ?? throw new InvalidOperationException(child.name + " has no parent bone.");
            var worldLength = Vector3.Distance(child.position, parent.position);
            var angle = angleDegrees * Mathf.Deg2Rad;
            var desiredWorld = (outward * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)).normalized *
                               worldLength * lengthScale;
            return parent.InverseTransformPoint(worldStart + desiredWorld);
        }

        private static Vector3 BasePositionForWorldTip(
            Transform movableBase,
            Transform tip,
            Vector3 desiredWorldTip)
        {
            var parent = movableBase.parent ??
                         throw new InvalidOperationException(movableBase.name + " has no parent bone.");
            return parent.InverseTransformPoint(movableBase.position + desiredWorldTip - tip.position);
        }

        private static float MinimumMovingBoneOutwardClearance(
            IReadOnlyList<Transform> chain,
            Vector3 surfaceCenter,
            Vector3 outward)
        {
            return chain.Skip(FirstAnimatedBoneIndex)
                .Min(item => Vector3.Dot(item.position - surfaceCenter, outward));
        }

        private static float MinimumFrontMovingVertexHeight(
            SkinnedMeshRenderer renderer,
            Vector3 surfaceCenter,
            Vector3 outward)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException("The tentacle renderer has no mesh.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindPoses = mesh.bindposes;
            var bones = renderer.bones;
            var movingBoneIndices = new HashSet<int>(
                Enumerable.Range(0, bones.Length)
                    .Where(index => IsMovingTentacleBoneName(bones[index].name)));
            var minimumHeight = float.PositiveInfinity;
            for (var index = 0; index < vertices.Length; index++)
            {
                var weight = weights[index];
                var movingWeight =
                    (movingBoneIndices.Contains(weight.boneIndex0) ? weight.weight0 : 0f) +
                    (movingBoneIndices.Contains(weight.boneIndex1) ? weight.weight1 : 0f) +
                    (movingBoneIndices.Contains(weight.boneIndex2) ? weight.weight2 : 0f) +
                    (movingBoneIndices.Contains(weight.boneIndex3) ? weight.weight3 : 0f);
                if (movingWeight < MinimumMovingVertexWeight) continue;
                var worldVertex = WeightedWorldVertex(
                                      vertices[index], weight.boneIndex0, weight.weight0, bones, bindPoses) +
                                  WeightedWorldVertex(
                                      vertices[index], weight.boneIndex1, weight.weight1, bones, bindPoses) +
                                  WeightedWorldVertex(
                                      vertices[index], weight.boneIndex2, weight.weight2, bones, bindPoses) +
                                  WeightedWorldVertex(
                                      vertices[index], weight.boneIndex3, weight.weight3, bones, bindPoses);
                if (Vector3.Dot(worldVertex - surfaceCenter, outward) < FrontSurfaceThreshold) continue;
                minimumHeight = Mathf.Min(minimumHeight, worldVertex.y - surfaceCenter.y);
            }
            return float.IsPositiveInfinity(minimumHeight) ? 0f : minimumHeight;
        }

        private static bool IsMovingTentacleBoneName(string boneName)
        {
            if (!boneName.StartsWith("Bone_", StringComparison.Ordinal) ||
                !int.TryParse(boneName.Substring(5), out var boneNumber))
                return false;
            return boneNumber >= 1 && boneNumber <= 9;
        }

        private static Vector3 ScaleAroundWorldPivotLocalPosition(
            Transform transform,
            SkinnedMeshRenderer renderer,
            Vector3 worldPivot,
            float scaleFactor)
        {
            var saved = BoneState.Capture(transform);
            var forceMatrixBefore = renderer.forceMatrixRecalculationPerRender;
            try
            {
                renderer.forceMatrixRecalculationPerRender = true;
                transform.localScale = saved.Scale * scaleFactor;
                for (var iteration = 0; iteration < 6; iteration++)
                {
                    var scaledSurface = AttackRootSurfaceCenter(renderer);
                    var correction = worldPivot - scaledSurface;
                    transform.position += correction;
                    if (correction.sqrMagnitude <= TransformTolerance * TransformTolerance) break;
                }
                return transform.localPosition;
            }
            finally
            {
                saved.Apply(transform);
                renderer.forceMatrixRecalculationPerRender = forceMatrixBefore;
            }
        }

        private static float ScaleRatio(Vector3 value, Vector3 reference)
        {
            if (reference.sqrMagnitude <= Mathf.Epsilon)
                throw new InvalidOperationException("The approved source scale is zero.");
            return value.magnitude / reference.magnitude;
        }

        private static Quaternion[] BuildDirectionPose(
            IReadOnlyList<Transform> chain,
            IReadOnlyList<BoneState> defaults,
            Vector3 forward,
            Func<int, float> angleForSegment,
            float tipLocalBend)
        {
            Restore(chain, defaults);
            for (var index = FirstAnimatedBoneIndex; index < chain.Count - 1; index++)
            {
                var currentDirection = chain[index + 1].position - chain[index].position;
                var angle = angleForSegment(index - FirstAnimatedBoneIndex) * Mathf.Deg2Rad;
                var desiredDirection = (forward * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)).normalized;
                chain[index].rotation =
                    Quaternion.FromToRotation(currentDirection.normalized, desiredDirection) * chain[index].rotation;
            }
            chain[chain.Count - 1].localRotation =
                chain[chain.Count - 1].localRotation * Quaternion.Euler(0f, 0f, tipLocalBend);
            return chain.Select(item => item.localRotation).ToArray();
        }

        private static AnimationClip CreateOrResetClip(string path, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.ClearCurves();
            clip.frameRate = 60f;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip intro, AnimationClip loop)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray()) stateMachine.RemoveState(child.state);
            foreach (var child in stateMachine.stateMachines.ToArray())
                stateMachine.RemoveStateMachine(child.stateMachine);
            var introState = stateMachine.AddState("Intro");
            introState.motion = intro;
            introState.speed = 1f;
            var loopState = stateMachine.AddState("AttackLoop");
            loopState.motion = loop;
            loopState.speed = 1f;
            stateMachine.defaultState = introState;
            var transition = introState.AddTransition(loopState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            var restartTransition = loopState.AddTransition(introState);
            restartTransition.hasExitTime = true;
            restartTransition.exitTime = 1f;
            restartTransition.hasFixedDuration = true;
            restartTransition.duration = 0f;
            restartTransition.offset = 0f;
            restartTransition.canTransitionToSelf = false;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Metrics InspectState(Scene scene, bool requireRendererBounds = true)
        {
            var slots = RequireSlots(scene);
            var target = RequireTarget(slots[3]);
            var execution = RequireTarget(slots[4]);
            RequireMatchingAttachmentAnchor(target, execution);
            var chain = RequireBoneChain(target.Renderer);
            var intro = RequireAsset<AnimationClip>(IntroClipPath);
            var loop = RequireAsset<AnimationClip>(LoopClipPath);
            var controller = RequireAsset<AnimatorController>(ControllerPath);
            var animator = target.Attachment.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The motion 3 attachment Animator is missing.");
            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion)
                throw new InvalidOperationException("The motion 3 attachment Animator settings changed.");
            if (Mathf.Abs(intro.length - IntroDuration) > 0.0001f ||
                Mathf.Abs(loop.length - LoopDuration) > 0.0001f)
                throw new InvalidOperationException("The approved animation timing changed.");
            var introSettings = AnimationUtility.GetAnimationClipSettings(intro);
            var loopSettings = AnimationUtility.GetAnimationClipSettings(loop);
            if (introSettings.loopTime || loopSettings.loopTime)
                throw new InvalidOperationException("Intro and AttackLoop must both be non-looping controller states.");
            var introBindings = AnimationUtility.GetCurveBindings(intro);
            var loopBindings = AnimationUtility.GetCurveBindings(loop);
            RequireRigBindings(introBindings, chain, target.Attachment, "Intro");
            RequireRigBindings(loopBindings, chain, target.Attachment, "AttackLoop");
            RequireRingGenerationBindings(introBindings, chain, target.Attachment, "Intro");
            RequireRingGenerationBindings(loopBindings, chain, target.Attachment, "AttackLoop");
            RequireVisibilityBinding(introBindings, target, "Intro");
            RequireVisibilityBinding(loopBindings, target, "AttackLoop");
            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states.Select(item => item.state).ToArray();
            var introState = states.SingleOrDefault(item => item.name == "Intro") ??
                             throw new InvalidOperationException("Intro state is missing.");
            var loopState = states.SingleOrDefault(item => item.name == "AttackLoop") ??
                            throw new InvalidOperationException("AttackLoop state is missing.");
            if (stateMachine.defaultState != introState || introState.motion != intro || loopState.motion != loop)
                throw new InvalidOperationException("Animator state motions changed.");
            var transition = introState.transitions.SingleOrDefault(item => item.destinationState == loopState) ??
                             throw new InvalidOperationException("Intro to AttackLoop transition is missing.");
            if (!transition.hasExitTime || Mathf.Abs(transition.exitTime - 1f) > 0.0001f ||
                transition.duration > 0.0001f)
                throw new InvalidOperationException("Intro to AttackLoop transition timing changed.");
            var restartTransition = loopState.transitions.SingleOrDefault(item => item.destinationState == introState) ??
                                    throw new InvalidOperationException("AttackLoop to Intro restart transition is missing.");
            if (!restartTransition.hasExitTime || Mathf.Abs(restartTransition.exitTime - 1f) > 0.0001f ||
                restartTransition.duration > 0.0001f)
                throw new InvalidOperationException("AttackLoop to Intro restart timing changed.");

            var requiredAnimatedBounds = CalculateAnimatedLocalBounds(target, intro, loop, false);
            if (requireRendererBounds &&
                !ContainsBounds(target.Renderer.localBounds, requiredAnimatedBounds, 0.001f))
                throw new InvalidOperationException(
                    "The tentacle renderer bounds do not contain the full authored motion. Stored=" +
                    target.Renderer.localBounds + " Required=" + requiredAnimatedBounds);

            var saved = chain.Select(BoneState.Capture).ToArray();
            var rootPositions = new List<Vector3>();
            var surfacePositions = new List<Vector3>();
            var activeFrameClearances = new List<float>();
            var emergenceVisibleVertexHeights = new List<float>();
            Vector3 introStartTip;
            Vector3 sourceReadySurface;
            Vector3 sourceReadyTip;
            Vector3 initialRiseTip;
            Vector3 frontClearTip;
            Vector3 frontClearSurface;
            float enteringVisibleVertexHeight;
            float tipPassVisibleVertexHeight;
            float frontClearVisibleVertexHeight;
            float diagonalExitVisibleVertexHeight;
            float diagonalExitBaseDistance;
            float earlyRiseVisibleVertexHeight;
            float midRiseVisibleVertexHeight;
            float fullClearVisibleVertexHeight;
            Vector3 introEndTip;
            Vector3 prepTip;
            Vector3 firstWindupTip;
            Vector3 firstAccelerationTip;
            Vector3 firstStrikeTip;
            Vector3 firstImpactHoldTip;
            Vector3 firstReturnTip;
            Vector3 secondWindupTip;
            Vector3 secondAccelerationTip;
            Vector3 secondStrikeTip;
            Vector3 secondImpactHoldTip;
            Vector3 hiddenReturnTip;
            BoneState[] introHiddenPose;
            BoneState[] loopHiddenPose;
            bool introHiddenVisible;
            bool introRevealVisible;
            bool introRiseVisible;
            bool loopStartVisible;
            bool loopHiddenVisible;
            float introHiddenChainOffset;
            float sourceReadyChainOffset;
            float loopHiddenChainOffset;
            float introHiddenMaximumOutward;
            float sourceReadyMaximumOutward;
            float sourceHiddenScaleRatio;
            float sourceMidScaleRatio;
            float sourceReadyScaleError;
            float firstStrikeTipAim;
            float firstStrikeTipLead;
            float secondStrikeTipAim;
            float secondStrikeTipLead;
            var rendererEnabledBefore = target.Renderer.enabled;
            var ringSaved = BoneState.Capture(chain[RingBoneIndex]);
            var outward = ResolveOutwardDirection(scene, target);
            var lateral = Vector3.Cross(Vector3.up, outward).normalized;
            var strikeAimDirection = (outward + Vector3.down * 1.15f).normalized;
            try
            {
                intro.SampleAnimation(target.Attachment.gameObject, 0f);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                introStartTip = chain[chain.Count - 1].position;
                introHiddenPose = chain.Skip(FirstAnimatedBoneIndex).Select(BoneState.Capture).ToArray();
                introHiddenVisible = target.Renderer.enabled;
                sourceHiddenScaleRatio = ScaleRatio(chain[RingBoneIndex].localScale, ringSaved.Scale);
                introHiddenChainOffset = Vector3.Distance(
                    chain[chain.Count - 1].position, surfacePositions[surfacePositions.Count - 1]);
                introHiddenMaximumOutward = chain.Skip(FirstAnimatedBoneIndex)
                    .Max(item => Vector3.Dot(
                        item.position - surfacePositions[surfacePositions.Count - 1], outward));
                intro.SampleAnimation(target.Attachment.gameObject, RevealTime);
                introRevealVisible = target.Renderer.enabled;
                intro.SampleAnimation(target.Attachment.gameObject, SourceGenerationMidTime);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                sourceMidScaleRatio = ScaleRatio(chain[RingBoneIndex].localScale, ringSaved.Scale);
                intro.SampleAnimation(target.Attachment.gameObject, RiseStartTime);
                rootPositions.Add(chain[0].position);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                introRiseVisible = target.Renderer.enabled;
                sourceReadySurface = surfacePositions[surfacePositions.Count - 1];
                sourceReadyTip = chain[chain.Count - 1].position;
                sourceReadyScaleError = Vector3.Distance(chain[RingBoneIndex].localScale, ringSaved.Scale);
                sourceReadyChainOffset = Vector3.Distance(sourceReadyTip, sourceReadySurface);
                sourceReadyMaximumOutward = chain.Skip(FirstAnimatedBoneIndex)
                    .Max(item => Vector3.Dot(item.position - sourceReadySurface, outward));
                intro.SampleAnimation(target.Attachment.gameObject, 0.65f);
                var enteringSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(enteringSurface);
                enteringVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, enteringSurface, outward);
                emergenceVisibleVertexHeights.Add(enteringVisibleVertexHeight);
                intro.SampleAnimation(target.Attachment.gameObject, TipPassSampleTime);
                initialRiseTip = chain[chain.Count - 1].position;
                var tipPassSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(tipPassSurface);
                tipPassVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, tipPassSurface, outward);
                emergenceVisibleVertexHeights.Add(tipPassVisibleVertexHeight);
                intro.SampleAnimation(target.Attachment.gameObject, FrontClearTime);
                frontClearTip = chain[chain.Count - 1].position;
                frontClearSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(frontClearSurface);
                frontClearVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, frontClearSurface, outward);
                emergenceVisibleVertexHeights.Add(frontClearVisibleVertexHeight);
                intro.SampleAnimation(target.Attachment.gameObject, DiagonalExitTime);
                var diagonalExitSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(diagonalExitSurface);
                diagonalExitVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, diagonalExitSurface, outward);
                emergenceVisibleVertexHeights.Add(diagonalExitVisibleVertexHeight);
                diagonalExitBaseDistance = Vector3.Distance(
                    chain[FirstAnimatedBoneIndex].position, diagonalExitSurface);
                intro.SampleAnimation(target.Attachment.gameObject, EarlyRiseTime);
                var earlyRiseSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(earlyRiseSurface);
                earlyRiseVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, earlyRiseSurface, outward);
                emergenceVisibleVertexHeights.Add(earlyRiseVisibleVertexHeight);
                intro.SampleAnimation(target.Attachment.gameObject, MidRiseTime);
                var midRiseSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(midRiseSurface);
                midRiseVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, midRiseSurface, outward);
                emergenceVisibleVertexHeights.Add(midRiseVisibleVertexHeight);
                intro.SampleAnimation(target.Attachment.gameObject, FullChainClearTime);
                var fullClearSurface = AttackRootSurfaceCenter(target.Renderer);
                surfacePositions.Add(fullClearSurface);
                fullClearVisibleVertexHeight = MinimumFrontMovingVertexHeight(
                    target.Renderer, fullClearSurface, outward);
                emergenceVisibleVertexHeights.Add(fullClearVisibleVertexHeight);
                activeFrameClearances.Add(MinimumMovingBoneOutwardClearance(chain, fullClearSurface, outward));
                intro.SampleAnimation(target.Attachment.gameObject, IntroDuration);
                rootPositions.Add(chain[0].position);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                activeFrameClearances.Add(MinimumMovingBoneOutwardClearance(
                    chain, surfacePositions[surfacePositions.Count - 1], outward));
                introEndTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, 0f);
                rootPositions.Add(chain[0].position);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                activeFrameClearances.Add(MinimumMovingBoneOutwardClearance(
                    chain, surfacePositions[surfacePositions.Count - 1], outward));
                prepTip = chain[chain.Count - 1].position;
                loopStartVisible = target.Renderer.enabled;
                loop.SampleAnimation(target.Attachment.gameObject, WindupHoldTime);
                firstWindupTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, AccelerationTime);
                firstAccelerationTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, StrikeDuration);
                rootPositions.Add(chain[0].position);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                activeFrameClearances.Add(MinimumMovingBoneOutwardClearance(
                    chain, surfacePositions[surfacePositions.Count - 1], outward));
                firstStrikeTip = chain[chain.Count - 1].position;
                firstStrikeTipAim = Vector3.Dot(
                    (chain[chain.Count - 1].position - chain[chain.Count - 2].position).normalized,
                    strikeAimDirection);
                firstStrikeTipLead = Vector3.Dot(firstStrikeTip, outward) -
                                     chain.Skip(FirstAnimatedBoneIndex).Take(chain.Count - FirstAnimatedBoneIndex - 1)
                                         .Max(item => Vector3.Dot(item.position, outward));
                loop.SampleAnimation(target.Attachment.gameObject, ImpactHoldEndTime);
                firstImpactHoldTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, SingleAttackDuration);
                rootPositions.Add(chain[0].position);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                activeFrameClearances.Add(MinimumMovingBoneOutwardClearance(
                    chain, surfacePositions[surfacePositions.Count - 1], outward));
                firstReturnTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, SingleAttackDuration + WindupHoldTime);
                secondWindupTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, SingleAttackDuration + AccelerationTime);
                secondAccelerationTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, SecondStrikeTime);
                rootPositions.Add(chain[0].position);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                activeFrameClearances.Add(MinimumMovingBoneOutwardClearance(
                    chain, surfacePositions[surfacePositions.Count - 1], outward));
                secondStrikeTip = chain[chain.Count - 1].position;
                secondStrikeTipAim = Vector3.Dot(
                    (chain[chain.Count - 1].position - chain[chain.Count - 2].position).normalized,
                    strikeAimDirection);
                secondStrikeTipLead = Vector3.Dot(secondStrikeTip, outward) -
                                      chain.Skip(FirstAnimatedBoneIndex).Take(chain.Count - FirstAnimatedBoneIndex - 1)
                                          .Max(item => Vector3.Dot(item.position, outward));
                loop.SampleAnimation(target.Attachment.gameObject, SingleAttackDuration + ImpactHoldEndTime);
                secondImpactHoldTip = chain[chain.Count - 1].position;
                loop.SampleAnimation(target.Attachment.gameObject, LoopDuration);
                surfacePositions.Add(AttackRootSurfaceCenter(target.Renderer));
                hiddenReturnTip = chain[chain.Count - 1].position;
                loopHiddenPose = chain.Skip(FirstAnimatedBoneIndex).Select(BoneState.Capture).ToArray();
                loopHiddenVisible = target.Renderer.enabled;
                loopHiddenChainOffset = chain.Skip(FirstAnimatedBoneIndex)
                    .Max(item => Vector3.Distance(item.position, surfacePositions[surfacePositions.Count - 1]));
            }
            finally
            {
                Restore(chain, saved);
                target.Renderer.enabled = rendererEnabledBefore;
            }
            var rootDrift = rootPositions.Max(item => Vector3.Distance(rootPositions[0], item));
            var surfaceAnchorDrift = surfacePositions.Max(item => Vector3.Distance(surfacePositions[0], item));
            var introRise = introEndTip.y - introStartTip.y;
            var strikeDrop = prepTip.y - firstStrikeTip.y;
            var returnError = Vector3.Distance(firstReturnTip, prepTip);
            var hiddenReturnError = Vector3.Distance(hiddenReturnTip, introStartTip);
            var hiddenPoseError = introHiddenPose.Zip(loopHiddenPose, BoneStateDifference).Max();
            var strikeDelta = firstStrikeTip - prepTip;
            var strikeOutward = Vector3.Dot(strikeDelta, outward);
            var strikeLateral = Mathf.Abs(Vector3.Dot(strikeDelta, lateral));
            var firstWindupRetreat = Vector3.Dot(prepTip - firstWindupTip, outward);
            var firstWindupLift = firstWindupTip.y - prepTip.y;
            var firstLateStrikeOutward = Vector3.Dot(firstStrikeTip - firstAccelerationTip, outward);
            var firstImpactHoldError = Vector3.Distance(firstImpactHoldTip, firstStrikeTip);
            var secondStrikeDelta = secondStrikeTip - firstReturnTip;
            var secondStrikeOutward = Vector3.Dot(secondStrikeDelta, outward);
            var secondStrikeDrop = firstReturnTip.y - secondStrikeTip.y;
            var secondStrikeLateral = Mathf.Abs(Vector3.Dot(secondStrikeDelta, lateral));
            var secondWindupRetreat = Vector3.Dot(firstReturnTip - secondWindupTip, outward);
            var secondWindupLift = secondWindupTip.y - firstReturnTip.y;
            var secondLateStrikeOutward = Vector3.Dot(secondStrikeTip - secondAccelerationTip, outward);
            var secondImpactHoldError = Vector3.Distance(secondImpactHoldTip, secondStrikeTip);
            var initialRiseDelta = initialRiseTip - sourceReadyTip;
            var initialRiseUp = initialRiseDelta.y;
            var initialRiseLateral = Mathf.Abs(Vector3.Dot(initialRiseDelta, lateral));
            var initialTipSurfaceOutward = Vector3.Dot(initialRiseTip - sourceReadySurface, outward);
            var tipFrontClearanceBeforeRise = Vector3.Dot(frontClearTip - frontClearSurface, outward);
            var tipRiseBeforeFrontClear = frontClearTip.y - sourceReadySurface.y;
            var minimumActiveFrameClearance = activeFrameClearances.Min();
            var minimumEmergenceVisibleVertexHeight = emergenceVisibleVertexHeights.Min();
            var preparedHeight = prepTip.y - sourceReadySurface.y;
            if (rootDrift > TransformTolerance)
                throw new InvalidOperationException("The fixed root bone moved: " + Num(rootDrift));
            if (surfaceAnchorDrift > MaximumSurfaceAnchorDrift)
                throw new InvalidOperationException(
                    "The modeled attachment surface moved: " + Num(surfaceAnchorDrift) +
                    " Samples=" + string.Join("|", surfacePositions.Select(Vec)));
            if (sourceHiddenScaleRatio > 0.005f || sourceMidScaleRatio < 0.2f ||
                sourceMidScaleRatio > 0.8f || sourceReadyScaleError > TransformTolerance)
                throw new InvalidOperationException(
                    "The source does not gradually generate from hidden to full size. HiddenRatio=" +
                    Num(sourceHiddenScaleRatio) + " MidRatio=" + Num(sourceMidScaleRatio) +
                    " ReadyError=" + Num(sourceReadyScaleError));
            if (initialRiseUp < 0.03f || initialRiseLateral > 0.03f || initialTipSurfaceOutward < 0.005f)
                throw new InvalidOperationException(
                    "The pointed tip does not pass outward through the source before rising. Up=" +
                    Num(initialRiseUp) + " Lateral=" + Num(initialRiseLateral) +
                    " SurfaceOutward=" + Num(initialTipSurfaceOutward));
            if (tipFrontClearanceBeforeRise < MinimumPreRiseFrameClearance ||
                tipRiseBeforeFrontClear > MaximumPreClearRise)
                throw new InvalidOperationException(
                    "The pointed tip rises before it clears the frame in depth. FrontClearance=" +
                    Num(tipFrontClearanceBeforeRise) + " RiseBeforeClear=" + Num(tipRiseBeforeFrontClear));
            if (diagonalExitBaseDistance > MaximumDiagonalExitBaseDistance)
                throw new InvalidOperationException(
                    "The diagonal exit is detached from the ring. BaseDistance=" +
                    Num(diagonalExitBaseDistance));
            if (earlyRiseVisibleVertexHeight > MaximumRingConnectionHeight ||
                midRiseVisibleVertexHeight > MaximumRingConnectionHeight)
                throw new InvalidOperationException(
                    "The rising tentacle is no longer visibly connected through the ring. Early=" +
                    Num(earlyRiseVisibleVertexHeight) + " Mid=" + Num(midRiseVisibleVertexHeight));
            if (minimumActiveFrameClearance < MinimumActiveFrameClearance)
                throw new InvalidOperationException(
                    "The active tentacle overlaps the Dolore frame after passing through the ring. MinimumClearance=" +
                    Num(minimumActiveFrameClearance));
            if (minimumEmergenceVisibleVertexHeight < MinimumEmergenceVertexHeight)
                throw new InvalidOperationException(
                    "Moving tentacle mesh appears below the ring before threading through it. MinimumVisibleHeight=" +
                    Num(minimumEmergenceVisibleVertexHeight) +
                    " Entering=" + Num(enteringVisibleVertexHeight) +
                    " TipPass=" + Num(tipPassVisibleVertexHeight) +
                    " FrontClear=" + Num(frontClearVisibleVertexHeight) +
                    " DiagonalExit=" + Num(diagonalExitVisibleVertexHeight) +
                    " EarlyRise=" + Num(earlyRiseVisibleVertexHeight) +
                    " MidRise=" + Num(midRiseVisibleVertexHeight) +
                    " FullClear=" + Num(fullClearVisibleVertexHeight));
            if (introRise < MinimumIntroRise)
                throw new InvalidOperationException("The tip does not rise far enough during Intro: " + Num(introRise));
            if (strikeOutward < MinimumStrikeForward)
                throw new InvalidOperationException("The strike does not travel out of the frame far enough: " + Num(strikeOutward));
            if (strikeDrop < MinimumStrikeDrop)
                throw new InvalidOperationException("The strike does not descend far enough: " + Num(strikeDrop));
            if (strikeLateral > MaximumStrikeLateral)
                throw new InvalidOperationException("The strike still travels sideways: " + Num(strikeLateral));
            if (firstWindupLift < MinimumWindupLift || firstWindupRetreat < MinimumWindupRetreat ||
                firstLateStrikeOutward < MinimumLateStrikeOutward ||
                firstImpactHoldError > MaximumImpactHoldError)
                throw new InvalidOperationException(
                    "The first target-directed strike does not build and release enough force. Lift=" +
                    Num(firstWindupLift) + " Retreat=" + Num(firstWindupRetreat) +
                    " LateOutward=" + Num(firstLateStrikeOutward) +
                    " HoldError=" + Num(firstImpactHoldError));
            if (firstStrikeTipAim < 0.75f || firstStrikeTipLead < 0.05f)
                throw new InvalidOperationException(
                    "The pointed tip does not lead the first target-directed downstrike. Aim=" +
                    Num(firstStrikeTipAim) + " Lead=" + Num(firstStrikeTipLead));
            if (secondStrikeOutward < MinimumStrikeForward || secondStrikeDrop < MinimumStrikeDrop ||
                secondStrikeLateral > MaximumStrikeLateral)
                throw new InvalidOperationException(
                    "The second strike changed direction. Outward=" + Num(secondStrikeOutward) +
                    " Drop=" + Num(secondStrikeDrop) + " Lateral=" + Num(secondStrikeLateral));
            if (secondStrikeTipAim < 0.75f || secondStrikeTipLead < 0.05f)
                throw new InvalidOperationException(
                    "The pointed tip does not lead the second target-directed downstrike. Aim=" +
                    Num(secondStrikeTipAim) + " Lead=" + Num(secondStrikeTipLead));
            if (secondWindupLift < MinimumWindupLift || secondWindupRetreat < MinimumWindupRetreat ||
                secondLateStrikeOutward < MinimumLateStrikeOutward ||
                secondImpactHoldError > MaximumImpactHoldError)
                throw new InvalidOperationException(
                    "The second target-directed strike does not match the first force profile. Lift=" +
                    Num(secondWindupLift) + " Retreat=" + Num(secondWindupRetreat) +
                    " LateOutward=" + Num(secondLateStrikeOutward) +
                    " HoldError=" + Num(secondImpactHoldError));
            if (preparedHeight < MinimumPreparedHeight)
                throw new InvalidOperationException("The preparation pose is not high enough: " + Num(preparedHeight));
            if (returnError > TransformTolerance)
                throw new InvalidOperationException("The first attack does not return to its preparation pose: " + Num(returnError));
            if (introHiddenVisible || !introRevealVisible || !introRiseVisible ||
                !loopStartVisible || loopHiddenVisible)
                throw new InvalidOperationException(
                    "Tentacle renderer visibility timing changed. Intro0=" + introHiddenVisible +
                    " Reveal=" + introRevealVisible + " Rise=" + introRiseVisible +
                    " Loop0=" + loopStartVisible + " LoopEnd=" + loopHiddenVisible);
            if (sourceReadyChainOffset > HiddenBehindSurfaceDistance + 0.005f ||
                sourceReadyMaximumOutward > -0.01f ||
                loopHiddenChainOffset > MaximumSurfaceAnchorDrift)
                throw new InvalidOperationException(
                    "The intact hidden tentacle is not fully behind the modeled source surface. IntroTipOffset=" +
                    Num(introHiddenChainOffset) + " SourceReady=" + Num(sourceReadyChainOffset) +
                    " IntroMaximumOutward=" + Num(introHiddenMaximumOutward) +
                    " SourceReadyMaximumOutward=" + Num(sourceReadyMaximumOutward) +
                    " LoopHidden=" + Num(loopHiddenChainOffset));
            return new Metrics(
                intro.length,
                loop.length,
                introRise,
                strikeOutward,
                strikeDrop,
                strikeLateral,
                secondStrikeOutward,
                secondStrikeDrop,
                secondStrikeLateral,
                firstWindupLift,
                firstWindupRetreat,
                firstLateStrikeOutward,
                firstImpactHoldError,
                secondWindupLift,
                secondWindupRetreat,
                secondLateStrikeOutward,
                secondImpactHoldError,
                preparedHeight,
                returnError,
                hiddenReturnError,
                hiddenPoseError,
                introHiddenChainOffset,
                sourceReadyChainOffset,
                loopHiddenChainOffset,
                sourceHiddenScaleRatio,
                sourceMidScaleRatio,
                sourceReadyScaleError,
                initialRiseUp,
                initialRiseLateral,
                tipFrontClearanceBeforeRise,
                tipRiseBeforeFrontClear,
                diagonalExitBaseDistance,
                earlyRiseVisibleVertexHeight,
                midRiseVisibleVertexHeight,
                minimumActiveFrameClearance,
                minimumEmergenceVisibleVertexHeight,
                firstStrikeTipAim,
                firstStrikeTipLead,
                secondStrikeTipAim,
                secondStrikeTipLead,
                rootDrift,
                surfaceAnchorDrift,
                AnimatedBoneNames(introBindings, chain, target.Attachment).Count);
        }

        private static Bounds CalculateAnimatedLocalBounds(
            Target target,
            AnimationClip intro,
            AnimationClip loop,
            bool addMargin)
        {
            var chain = RequireBoneChain(target.Renderer);
            var saved = chain.Select(BoneState.Capture).ToArray();
            var rendererEnabledBefore = target.Renderer.enabled;
            var samples = new[]
            {
                new PoseSample(intro, 0f),
                new PoseSample(intro, 0.25f),
                new PoseSample(intro, SourceGenerationDuration),
                new PoseSample(intro, SourceGenerationDuration + 0.75f),
                new PoseSample(intro, IntroDuration),
                new PoseSample(loop, 0f),
                new PoseSample(loop, 0.25f),
                new PoseSample(loop, StrikeDuration),
                new PoseSample(loop, 0.9f),
                new PoseSample(loop, SingleAttackDuration),
                new PoseSample(loop, SecondStrikeTime),
                new PoseSample(loop, SecondStrikeTime + 0.4f),
                new PoseSample(loop, LoopDuration)
            };
            var mesh = target.Renderer.sharedMesh ??
                       throw new InvalidOperationException("The tentacle renderer mesh is missing.");
            var combined = mesh.bounds;
            try
            {
                foreach (var sample in samples)
                {
                    sample.Clip.SampleAnimation(target.Attachment.gameObject, sample.Time);
                    foreach (var bone in chain)
                        combined.Encapsulate(target.Renderer.transform.InverseTransformPoint(bone.position));
                }
            }
            finally
            {
                Restore(chain, saved);
                target.Renderer.enabled = rendererEnabledBefore;
            }
            if (addMargin) combined.Expand(Vector3.Max(combined.size * 0.18f, Vector3.one * 0.5f));
            return combined;
        }

        private static bool ContainsBounds(Bounds outer, Bounds inner, float tolerance)
        {
            return outer.min.x <= inner.min.x + tolerance && outer.min.y <= inner.min.y + tolerance &&
                   outer.min.z <= inner.min.z + tolerance && outer.max.x >= inner.max.x - tolerance &&
                   outer.max.y >= inner.max.y - tolerance && outer.max.z >= inner.max.z - tolerance;
        }

        private static void RequireRigBindings(
            IReadOnlyCollection<EditorCurveBinding> bindings,
            IReadOnlyList<Transform> chain,
            Transform animationRoot,
            string label)
        {
            var fixedPaths = new HashSet<string>(
                chain.Take(FirstAnimatedBoneIndex)
                    .Where((_, index) => index != RingBoneIndex)
                    .Select(item => AnimationUtility.CalculateTransformPath(item, animationRoot)),
                StringComparer.Ordinal);
            if (bindings.Any(item => fixedPaths.Contains(item.path) || string.IsNullOrEmpty(item.path)))
                throw new InvalidOperationException(label + " moves the fixed attachment base rig.");
            var animated = AnimatedBoneNames(bindings, chain, animationRoot);
            var expected = chain.Skip(FirstAnimatedBoneIndex).Select(item => item.name)
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
            if (!animated.OrderBy(item => item, StringComparer.Ordinal).SequenceEqual(expected, StringComparer.Ordinal))
                throw new InvalidOperationException(label + " does not actively animate all 9 deforming rig bones.");
        }

        private static void RequireVisibilityBinding(
            IEnumerable<EditorCurveBinding> bindings,
            Target target,
            string label)
        {
            var rendererPath = AnimationUtility.CalculateTransformPath(
                target.Renderer.transform,
                target.Attachment);
            if (!bindings.Any(item => item.path == rendererPath &&
                                      item.type == typeof(SkinnedMeshRenderer) &&
                                      item.propertyName == "m_Enabled"))
                throw new InvalidOperationException(label + " is missing the tentacle renderer visibility curve.");
        }

        private static void RequireRingGenerationBindings(
            IEnumerable<EditorCurveBinding> bindings,
            IReadOnlyList<Transform> chain,
            Transform animationRoot,
            string label)
        {
            var ringPath = AnimationUtility.CalculateTransformPath(chain[RingBoneIndex], animationRoot);
            var sourceBindings = bindings.Where(item => item.path == ringPath && item.type == typeof(Transform))
                .Select(item => item.propertyName).ToArray();
            if (!new[] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                        "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" }
                    .All(sourceBindings.Contains))
                throw new InvalidOperationException(label + " is missing fixed-ring generation curves.");
        }

        private static HashSet<string> AnimatedBoneNames(
            IEnumerable<EditorCurveBinding> bindings,
            IReadOnlyList<Transform> chain,
            Transform animationRoot)
        {
            var pathToName = chain.Skip(FirstAnimatedBoneIndex).ToDictionary(
                item => AnimationUtility.CalculateTransformPath(item, animationRoot),
                item => item.name,
                StringComparer.Ordinal);
            return new HashSet<string>(
                bindings.Where(item => pathToName.ContainsKey(item.path))
                    .Select(item => pathToName[item.path]),
                StringComparer.Ordinal);
        }

        private static void WriteInspection(Metrics metrics, string phase, bool sceneSaved)
        {
            EnsureFolder(ReviewFolder);
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + SlotName)
                .AppendLine("IntroClip=" + IntroClipPath)
                .AppendLine("AttackLoopClip=" + LoopClipPath)
                .AppendLine("Controller=" + ControllerPath)
                .AppendLine("IntroSeconds=" + Num(metrics.IntroLength))
                .AppendLine("SourceGenerationSeconds=" + Num(SourceGenerationDuration))
                .AppendLine("RiseSeconds=" + Num(RiseDuration))
                .AppendLine("StrikeSeconds=" + Num(StrikeDuration))
                .AppendLine("RecoverSeconds=" + Num(RecoverDuration))
                .AppendLine("AttackLoopSeconds=" + Num(metrics.LoopLength))
                .AppendLine("BuiltInRigBones=13")
                .AppendLine("AnimatedMovableBones=" + metrics.AnimatedBoneCount)
                .AppendLine("FixedAttachmentBones=Bone_000,Bone_012,Bone_011,Bone_010")
                .AppendLine("RootBoneAnimated=False")
                .AppendLine("RootDrift=" + Num(metrics.RootDrift))
                .AppendLine("ModeledSurfaceAnchorDrift=" + Num(metrics.SurfaceAnchorDrift))
                .AppendLine("IntroTipRise=" + Num(metrics.IntroRise))
                .AppendLine("StrikeForwardTravel=" + Num(metrics.StrikeForward))
                .AppendLine("StrikeVerticalDrop=" + Num(metrics.StrikeDrop))
                .AppendLine("StrikeLateralTravel=" + Num(metrics.StrikeLateral))
                .AppendLine("SecondStrikeForwardTravel=" + Num(metrics.SecondStrikeForward))
                .AppendLine("SecondStrikeVerticalDrop=" + Num(metrics.SecondStrikeDrop))
                .AppendLine("SecondStrikeLateralTravel=" + Num(metrics.SecondStrikeLateral))
                .AppendLine("FirstWindupLift=" + Num(metrics.FirstWindupLift))
                .AppendLine("FirstWindupRetreat=" + Num(metrics.FirstWindupRetreat))
                .AppendLine("FirstLateStrikeOutward=" + Num(metrics.FirstLateStrikeOutward))
                .AppendLine("FirstImpactHoldError=" + Num(metrics.FirstImpactHoldError))
                .AppendLine("SecondWindupLift=" + Num(metrics.SecondWindupLift))
                .AppendLine("SecondWindupRetreat=" + Num(metrics.SecondWindupRetreat))
                .AppendLine("SecondLateStrikeOutward=" + Num(metrics.SecondLateStrikeOutward))
                .AppendLine("SecondImpactHoldError=" + Num(metrics.SecondImpactHoldError))
                .AppendLine("PreparedHeight=" + Num(metrics.PreparedHeight))
                .AppendLine("FirstReturnError=" + Num(metrics.ReturnError))
                .AppendLine("HiddenReturnError=" + Num(metrics.HiddenReturnError))
                .AppendLine("HiddenPoseError=" + Num(metrics.HiddenPoseError))
                .AppendLine("IntroHiddenChainOffset=" + Num(metrics.IntroHiddenChainOffset))
                .AppendLine("SourceReadyChainOffset=" + Num(metrics.SourceReadyChainOffset))
                .AppendLine("LoopHiddenChainOffset=" + Num(metrics.LoopHiddenChainOffset))
                .AppendLine("SourceHiddenScaleRatio=" + Num(metrics.SourceHiddenScaleRatio))
                .AppendLine("SourceMidScaleRatio=" + Num(metrics.SourceMidScaleRatio))
                .AppendLine("SourceReadyScaleError=" + Num(metrics.SourceReadyScaleError))
                .AppendLine("InitialRiseUp=" + Num(metrics.InitialRiseUp))
                .AppendLine("InitialRiseLateral=" + Num(metrics.InitialRiseLateral))
                .AppendLine("TipFrontClearanceBeforeRise=" + Num(metrics.TipFrontClearanceBeforeRise))
                .AppendLine("TipRiseBeforeFrontClear=" + Num(metrics.TipRiseBeforeFrontClear))
                .AppendLine("DiagonalExitBaseDistance=" + Num(metrics.DiagonalExitBaseDistance))
                .AppendLine("EarlyRiseConnectionHeight=" + Num(metrics.EarlyRiseConnectionHeight))
                .AppendLine("MidRiseConnectionHeight=" + Num(metrics.MidRiseConnectionHeight))
                .AppendLine("MinimumActiveFrameClearance=" + Num(metrics.MinimumActiveFrameClearance))
                .AppendLine("MinimumEmergenceVisibleVertexHeight=" + Num(metrics.MinimumEmergenceVisibleVertexHeight))
                .AppendLine("FirstStrikeTipAim=" + Num(metrics.FirstStrikeTipAim))
                .AppendLine("FirstStrikeTipLead=" + Num(metrics.FirstStrikeTipLead))
                .AppendLine("SecondStrikeTipAim=" + Num(metrics.SecondStrikeTipAim))
                .AppendLine("SecondStrikeTipLead=" + Num(metrics.SecondStrikeTipLead))
                .AppendLine("ControllerCycle=Intro->AttackLoop->Intro")
                .AppendLine("VisibilityCycle=Hidden->SourceReveal->Rise->TwoStrikes->Hidden")
                .AppendLine("IntroHiddenRendererEnabled=False")
                .AppendLine("SourceRevealRendererEnabled=True")
                .AppendLine("LoopEndRendererEnabled=False")
                .AppendLine("AttachmentAnchorMatchesApprovedStaticSlot=True")
                .AppendLine("OtherDoloreSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("SceneSaved=" + sceneSaved);
            File.WriteAllText(ProjectAbsolutePath(InspectionPath), report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                InspectionPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static List<Texture2D> CapturePoses(Transform sourceModel, IReadOnlyList<PoseSample> poses)
        {
            GameObject clone = null;
            GameObject cameraObject = null;
            var lights = new List<GameObject>();
            var images = new List<Texture2D>();
            try
            {
                clone = UnityEngine.Object.Instantiate(sourceModel.gameObject);
                clone.name = "Dolore_04_TentacleStab_Capture";
                clone.transform.SetParent(null);
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                clone.transform.localScale = sourceModel.lossyScale;
                SetLayerRecursively(clone.transform, CaptureLayer);
                foreach (var animator in clone.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    renderer.updateWhenOffscreen = true;
                var attachment = clone.transform.Find(AttachmentName) ??
                                 throw new InvalidOperationException("Capture clone is missing the attachment root.");
                var combined = default(Bounds);
                var hasBounds = false;
                foreach (var pose in poses)
                {
                    pose.Clip.SampleAnimation(attachment.gameObject, pose.Time);
                    var bounds = BoundsOfVisible(clone.transform);
                    if (!hasBounds)
                    {
                        combined = bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(bounds);
                    }
                }
                combined.Expand(combined.size * 0.12f);
                cameraObject = new GameObject("Dolore_04_TentacleStab_Camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.024f, 0.023f, 1f);
                camera.fieldOfView = 28f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                lights.Add(CreateLight("Dolore_04_Key", new Color(0.78f, 0.90f, 0.87f), 1.25f,
                    new Vector3(-0.45f, -0.65f, -0.62f)));
                lights.Add(CreateLight("Dolore_04_Fill", new Color(0.95f, 0.66f, 0.38f), 0.48f,
                    new Vector3(0.68f, -0.32f, -0.45f)));
                lights.Add(CreateLight("Dolore_04_Rim", new Color(0.20f, 0.70f, 0.62f), 0.62f,
                    new Vector3(-0.15f, -0.25f, 0.82f)));
                foreach (var pose in poses)
                {
                    pose.Clip.SampleAnimation(attachment.gameObject, pose.Time);
                    images.Add(CaptureView(camera, combined, Vector3.forward, 640, 480));
                }
                return images;
            }
            catch
            {
                foreach (var image in images)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
                throw;
            }
            finally
            {
                foreach (var light in lights)
                    if (light != null) UnityEngine.Object.DestroyImmediate(light);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static Texture2D CaptureView(
            Camera camera,
            Bounds bounds,
            Vector3 viewDirection,
            int width,
            int height)
        {
            var verticalFov = camera.fieldOfView * Mathf.Deg2Rad;
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * width / height);
            var verticalDistance = bounds.size.y * 0.5f / Mathf.Tan(verticalFov * 0.5f);
            var horizontalDistance = bounds.size.x * 0.5f / Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(verticalDistance, horizontalDistance) + bounds.extents.z + 0.4f;
            camera.transform.position = bounds.center + viewDirection.normalized * distance;
            camera.transform.rotation =
                Quaternion.LookRotation((bounds.center - camera.transform.position).normalized, Vector3.up);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void SaveSheet(IReadOnlyList<Texture2D> images, string outputPath)
        {
            const int columns = 5;
            const int rows = 2;
            const int width = 640;
            const int height = 480;
            if (images.Count != columns * rows)
                throw new InvalidOperationException("Tentacle animation capture requires exactly ten poses.");
            var sheet = new Texture2D(width * columns, height * rows, TextureFormat.RGBA32, false, false);
            try
            {
                sheet.SetPixels32(Enumerable.Repeat(
                    new Color32(4, 6, 6, 255),
                    sheet.width * sheet.height).ToArray());
                for (var index = 0; index < images.Count; index++)
                {
                    var x = index % columns * width;
                    var y = (rows - 1 - index / columns) * height;
                    sheet.SetPixels32(x, y, width, height, images[index].GetPixels32());
                }
                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ??
                                          throw new InvalidOperationException("Capture output path is invalid."));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static GameObject CreateLight(string name, Color color, float intensity, Vector3 direction)
        {
            var lightObject = new GameObject(name);
            lightObject.layer = CaptureLayer;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << CaptureLayer;
            lightObject.transform.rotation = Quaternion.LookRotation(direction.normalized);
            return lightObject;
        }

        private static Bounds BoundsOfVisible(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("No visible renderers are available for capture.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var index = 0; index < root.childCount; index++)
                SetLayerRecursively(root.GetChild(index), layer);
        }

        private static Vector3 AttackRootSurfaceCenter(SkinnedMeshRenderer renderer)
        {
            var originalIndices = AttackRootSurfaceVertexIndices(renderer);
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(renderer.name + " mesh is missing.");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindPoses = mesh.bindposes;
            var bones = renderer.bones;
            var sum = Vector3.zero;
            foreach (var index in originalIndices)
            {
                var weight = weights[index];
                var worldVertex = WeightedWorldVertex(
                    vertices[index], weight.boneIndex0, weight.weight0, bones, bindPoses) +
                    WeightedWorldVertex(vertices[index], weight.boneIndex1, weight.weight1, bones, bindPoses) +
                    WeightedWorldVertex(vertices[index], weight.boneIndex2, weight.weight2, bones, bindPoses) +
                    WeightedWorldVertex(vertices[index], weight.boneIndex3, weight.weight3, bones, bindPoses);
                sum += worldVertex;
            }
            return sum / originalIndices.Length;
        }

        private static Vector3 WeightedWorldVertex(
            Vector3 vertex,
            int boneIndex,
            float weight,
            IReadOnlyList<Transform> bones,
            IReadOnlyList<Matrix4x4> bindPoses)
        {
            if (weight <= 0f) return Vector3.zero;
            return (bones[boneIndex].localToWorldMatrix * bindPoses[boneIndex])
                   .MultiplyPoint3x4(vertex) * weight;
        }

        private static int[] AttackRootSurfaceVertexIndices(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(renderer.name + " mesh is missing.");
            var sourceVertices = mesh.vertices;
            var weldedByPosition = new Dictionary<Vector3Int, int>();
            var weldedRepresentatives = new List<int>();
            var vertexToWelded = new int[sourceVertices.Length];
            for (var vertexIndex = 0; vertexIndex < sourceVertices.Length; vertexIndex++)
            {
                var vertex = sourceVertices[vertexIndex];
                var key = new Vector3Int(
                    Mathf.RoundToInt(vertex.x * 100000f),
                    Mathf.RoundToInt(vertex.y * 100000f),
                    Mathf.RoundToInt(vertex.z * 100000f));
                if (!weldedByPosition.TryGetValue(key, out var weldedIndex))
                {
                    weldedIndex = weldedRepresentatives.Count;
                    weldedByPosition.Add(key, weldedIndex);
                    weldedRepresentatives.Add(vertexIndex);
                }
                vertexToWelded[vertexIndex] = weldedIndex;
            }
            var edgeCounts = new Dictionary<ulong, int>();
            var triangles = mesh.triangles;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                CountEdge(edgeCounts, vertexToWelded[triangles[index]], vertexToWelded[triangles[index + 1]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 1]], vertexToWelded[triangles[index + 2]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 2]], vertexToWelded[triangles[index]]);
            }
            var boundaryEdges = edgeCounts.Where(item => item.Value == 1).Select(item => item.Key).ToArray();
            var boundaryVertices = new HashSet<int>();
            foreach (var edge in boundaryEdges)
            {
                boundaryVertices.Add((int)(edge >> 32));
                boundaryVertices.Add((int)(edge & uint.MaxValue));
            }
            if (boundaryEdges.Length != 5 || boundaryVertices.Count != 5)
                throw new InvalidOperationException(
                    renderer.name + " must retain the approved five-vertex attachment boundary.");
            return boundaryVertices.Select(index => weldedRepresentatives[index]).ToArray();
        }

        private static string BoneWeightText(int boneIndex, float weight, SkinnedMeshRenderer renderer)
        {
            if (weight <= 0f) return "<none>:0";
            return renderer.bones[boneIndex].name + ":" + Num(weight);
        }

        private static void CountEdge(IDictionary<ulong, int> edgeCounts, int first, int second)
        {
            var minimum = (uint)Math.Min(first, second);
            var maximum = (uint)Math.Max(first, second);
            var key = ((ulong)minimum << 32) | maximum;
            edgeCounts[key] = edgeCounts.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        private static void RequireMatchingAttachmentAnchor(Target target, Target reference)
        {
            if (!TransformApproximately(target.Attachment, reference.Attachment) ||
                !TransformApproximately(target.Source, reference.Source) ||
                !TransformApproximately(target.RootBone, reference.RootBone))
                throw new InvalidOperationException(
                    "Motion 3 no longer matches the approved static attachment start transform.");
            if (AssetDatabase.GetAssetPath(target.Renderer.sharedMesh) !=
                AssetDatabase.GetAssetPath(reference.Renderer.sharedMesh))
                throw new InvalidOperationException("Motion 3 attack mesh changed.");
            if (!target.Renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)
                    .SequenceEqual(reference.Renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)))
                throw new InvalidOperationException("Motion 3 attack materials changed.");
        }

        private static bool TransformApproximately(Transform left, Transform right)
        {
            return Vector3.Distance(left.localPosition, right.localPosition) <= TransformTolerance &&
                   Quaternion.Angle(left.localRotation, right.localRotation) <= 0.001f &&
                   Vector3.Distance(left.localScale, right.localScale) <= TransformTolerance;
        }

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            return scene;
        }

        private static Vector3 ResolveOutwardDirection(Scene scene, Target target)
        {
            var player = scene.GetRootGameObjects().SingleOrDefault(item => item.name == "Player") ??
                         throw new InvalidOperationException("The CargoRunMvp Player root is missing.");
            var anchor = AttackRootSurfaceCenter(target.Renderer);
            var outward = Vector3.ProjectOnPlane(player.transform.position - anchor, Vector3.up).normalized;
            if (outward.sqrMagnitude < 0.9f)
                throw new InvalidOperationException("The frame outward direction toward Player is unavailable.");
            return outward;
        }

        private static Transform[] RequireSlots(Scene scene)
        {
            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(item => item.name == PlacementRootName) ??
                                throw new InvalidOperationException("Approved Dolore placement root is missing.");
            if (placementRoot.transform.childCount != ExpectedSlotNames.Length)
                throw new InvalidOperationException("Approved Dolore placement must contain exactly seven slots.");
            var slots = new Transform[ExpectedSlotNames.Length];
            for (var index = 0; index < slots.Length; index++)
            {
                slots[index] = placementRoot.transform.GetChild(index);
                if (slots[index].name != ExpectedSlotNames[index])
                    throw new InvalidOperationException("Dolore slot order or name changed at index " + index + ".");
            }
            return slots;
        }

        private static Transform RequireModel(Transform slot)
        {
            return Enumerable.Range(0, slot.childCount).Select(slot.GetChild)
                       .SingleOrDefault(item => item.name == ModelName) ??
                   throw new InvalidOperationException(slot.name + " is missing " + ModelName + ".");
        }

        private static Target RequireTarget(Transform slot)
        {
            var model = RequireModel(slot);
            var attachment = model.Find(AttachmentName) ??
                             throw new InvalidOperationException(slot.name + " is missing " + AttachmentName + ".");
            var renderer = attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.sharedMesh != null && item.bones.Length == ExpectedBoneCount) ??
                           throw new InvalidOperationException("The approved 13-bone attack renderer is missing.");
            var source = attachment.Find("Dolore_Attack_Source") ??
                         throw new InvalidOperationException("The approved attack source instance is missing.");
            var rootBone = renderer.bones.SingleOrDefault(item => item.name == RootBoneName) ??
                           throw new InvalidOperationException(RootBoneName + " is missing.");
            return new Target(model, attachment, source, renderer, rootBone);
        }

        private static List<Transform> RequireBoneChain(SkinnedMeshRenderer renderer)
        {
            if (renderer.bones.Length != ExpectedBoneCount)
                throw new InvalidOperationException("The approved attack bone count changed.");
            var available = new HashSet<Transform>(renderer.bones);
            var root = renderer.bones.SingleOrDefault(item => item.name == RootBoneName) ??
                       throw new InvalidOperationException(RootBoneName + " is missing.");
            var chain = new List<Transform> { root };
            while (chain.Count < ExpectedBoneCount)
            {
                var next = available.SingleOrDefault(item => item.parent == chain[chain.Count - 1]);
                if (next == null)
                    throw new InvalidOperationException("The built-in attack rig is not a single 13-bone chain.");
                chain.Add(next);
            }
            if (chain[chain.Count - 1].name != TipBoneName)
                throw new InvalidOperationException("The built-in attack rig tip changed.");
            return chain;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => HierarchySignature(item.transform, null)).ToArray();
        }

        private static string[] ProtectedSlotSignatures(IReadOnlyList<Transform> slots)
        {
            return slots.Where((_, index) => index != 3)
                .Select(item => HierarchySignature(item, null)).ToArray();
        }

        private static string HierarchySignature(Transform root, string excludedChildName)
        {
            var builder = new StringBuilder();
            AppendHierarchySignature(builder, root, root, excludedChildName);
            return builder.ToString();
        }

        private static void AppendHierarchySignature(
            StringBuilder builder,
            Transform current,
            Transform root,
            string excludedChildName)
        {
            if (current != root && current.name == excludedChildName) return;
            builder.Append('|').Append(PathFrom(current, root))
                .Append(" T=").Append(TransformSignature(current))
                .Append(" A=").Append(current.gameObject.activeSelf);
            foreach (var renderer in current.GetComponents<Renderer>())
            {
                builder.Append(" Mesh=")
                    .Append(AssetDatabase.GetAssetPath(
                        renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : null))
                    .Append(" Mats=")
                    .Append(string.Join(",", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            }
            for (var index = 0; index < current.childCount; index++)
                AppendHierarchySignature(builder, current.GetChild(index), root, excludedChildName);
        }

        private static string TransformSignature(Transform value)
        {
            return Vec(value.localPosition) + "|" + Quat(value.localRotation) + "|" + Vec(value.localScale);
        }

        private static string PathFrom(Transform current, Transform root)
        {
            if (current == root) return string.Empty;
            var names = new List<string>();
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }
            if (current != root)
                throw new InvalidOperationException("Transform is not below the requested root.");
            names.Reverse();
            return string.Join("/", names);
        }

        private static void WriteVectorCurves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            IReadOnlyList<TimedVector> values)
        {
            SetCurve(clip, path, propertyPrefix + ".x", values.Select(item => new Keyframe(item.Time, item.Value.x)));
            SetCurve(clip, path, propertyPrefix + ".y", values.Select(item => new Keyframe(item.Time, item.Value.y)));
            SetCurve(clip, path, propertyPrefix + ".z", values.Select(item => new Keyframe(item.Time, item.Value.z)));
        }

        private static void WriteQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<TimedQuaternion> values)
        {
            var aligned = new List<TimedQuaternion>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                if (index > 0 && Quaternion.Dot(aligned[index - 1].Value, value.Value) < 0f)
                    value = new TimedQuaternion(
                        value.Time,
                        new Quaternion(-value.Value.x, -value.Value.y, -value.Value.z, -value.Value.w));
                aligned.Add(value);
            }
            SetCurve(clip, path, "m_LocalRotation.x",
                aligned.Select(item => new Keyframe(item.Time, item.Value.x)));
            SetCurve(clip, path, "m_LocalRotation.y",
                aligned.Select(item => new Keyframe(item.Time, item.Value.y)));
            SetCurve(clip, path, "m_LocalRotation.z",
                aligned.Select(item => new Keyframe(item.Time, item.Value.z)));
            SetCurve(clip, path, "m_LocalRotation.w",
                aligned.Select(item => new Keyframe(item.Time, item.Value.w)));
        }

        private static void SetCurve(
            AnimationClip clip,
            string path,
            string property,
            IEnumerable<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static void WriteVisibilityCurve(
            AnimationClip clip,
            string path,
            IReadOnlyList<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "m_Enabled"),
                curve);
        }

        private static void Restore(IReadOnlyList<Transform> chain, IReadOnlyList<BoneState> values)
        {
            for (var index = 0; index < chain.Count; index++) values[index].Apply(chain[index]);
        }

        private static float BoneStateDifference(BoneState first, BoneState second)
        {
            return Mathf.Max(
                Vector3.Distance(first.Position, second.Position),
                Quaternion.Angle(first.Rotation, second.Rotation) / 180f,
                Vector3.Distance(first.Scale, second.Scale));
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException(typeof(T).Name + " asset is missing: " + path);
        }

        private static void EnsureFolder(string path)
        {
            var normalized = path.Replace('\\', '/');
            var parts = normalized.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static string ProjectAbsolutePath(string assetPath)
        {
            var root = Directory.GetParent(Application.dataPath)?.FullName ??
                       throw new InvalidOperationException("Unity project root is unavailable.");
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";
        }

        private readonly struct Target
        {
            public Target(
                Transform model,
                Transform attachment,
                Transform source,
                SkinnedMeshRenderer renderer,
                Transform rootBone)
            {
                Model = model;
                Attachment = attachment;
                Source = source;
                Renderer = renderer;
                RootBone = rootBone;
            }

            public Transform Model { get; }
            public Transform Attachment { get; }
            public Transform Source { get; }
            public SkinnedMeshRenderer Renderer { get; }
            public Transform RootBone { get; }
        }

        private readonly struct BoneState
        {
            private BoneState(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }

            public static BoneState Capture(Transform transform)
            {
                return new BoneState(transform.localPosition, transform.localRotation, transform.localScale);
            }

            public void Apply(Transform transform)
            {
                transform.localPosition = Position;
                transform.localRotation = Rotation;
                transform.localScale = Scale;
            }
        }

        private readonly struct AnimationAssets
        {
            public AnimationAssets(AnimationClip intro, AnimationClip loop, AnimatorController controller)
            {
                Intro = intro;
                Loop = loop;
                Controller = controller;
            }

            public AnimationClip Intro { get; }
            public AnimationClip Loop { get; }
            public AnimatorController Controller { get; }
        }

        private readonly struct TimedVector
        {
            public TimedVector(float time, Vector3 value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public Vector3 Value { get; }
        }

        private readonly struct TimedQuaternion
        {
            public TimedQuaternion(float time, Quaternion value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public Quaternion Value { get; }
        }

        private readonly struct PoseSample
        {
            public PoseSample(AnimationClip clip, float time)
            {
                Clip = clip;
                Time = time;
            }

            public AnimationClip Clip { get; }
            public float Time { get; }
        }

        private readonly struct Metrics
        {
            public Metrics(
                float introLength,
                float loopLength,
                float introRise,
                float strikeForward,
                float strikeDrop,
                float strikeLateral,
                float secondStrikeForward,
                float secondStrikeDrop,
                float secondStrikeLateral,
                float firstWindupLift,
                float firstWindupRetreat,
                float firstLateStrikeOutward,
                float firstImpactHoldError,
                float secondWindupLift,
                float secondWindupRetreat,
                float secondLateStrikeOutward,
                float secondImpactHoldError,
                float preparedHeight,
                float returnError,
                float hiddenReturnError,
                float hiddenPoseError,
                float introHiddenChainOffset,
                float sourceReadyChainOffset,
                float loopHiddenChainOffset,
                float sourceHiddenScaleRatio,
                float sourceMidScaleRatio,
                float sourceReadyScaleError,
                float initialRiseUp,
                float initialRiseLateral,
                float tipFrontClearanceBeforeRise,
                float tipRiseBeforeFrontClear,
                float diagonalExitBaseDistance,
                float earlyRiseConnectionHeight,
                float midRiseConnectionHeight,
                float minimumActiveFrameClearance,
                float minimumEmergenceVisibleVertexHeight,
                float firstStrikeTipAim,
                float firstStrikeTipLead,
                float secondStrikeTipAim,
                float secondStrikeTipLead,
                float rootDrift,
                float surfaceAnchorDrift,
                int animatedBoneCount)
            {
                IntroLength = introLength;
                LoopLength = loopLength;
                IntroRise = introRise;
                StrikeForward = strikeForward;
                StrikeDrop = strikeDrop;
                StrikeLateral = strikeLateral;
                SecondStrikeForward = secondStrikeForward;
                SecondStrikeDrop = secondStrikeDrop;
                SecondStrikeLateral = secondStrikeLateral;
                FirstWindupLift = firstWindupLift;
                FirstWindupRetreat = firstWindupRetreat;
                FirstLateStrikeOutward = firstLateStrikeOutward;
                FirstImpactHoldError = firstImpactHoldError;
                SecondWindupLift = secondWindupLift;
                SecondWindupRetreat = secondWindupRetreat;
                SecondLateStrikeOutward = secondLateStrikeOutward;
                SecondImpactHoldError = secondImpactHoldError;
                PreparedHeight = preparedHeight;
                ReturnError = returnError;
                HiddenReturnError = hiddenReturnError;
                HiddenPoseError = hiddenPoseError;
                IntroHiddenChainOffset = introHiddenChainOffset;
                SourceReadyChainOffset = sourceReadyChainOffset;
                LoopHiddenChainOffset = loopHiddenChainOffset;
                SourceHiddenScaleRatio = sourceHiddenScaleRatio;
                SourceMidScaleRatio = sourceMidScaleRatio;
                SourceReadyScaleError = sourceReadyScaleError;
                InitialRiseUp = initialRiseUp;
                InitialRiseLateral = initialRiseLateral;
                TipFrontClearanceBeforeRise = tipFrontClearanceBeforeRise;
                TipRiseBeforeFrontClear = tipRiseBeforeFrontClear;
                DiagonalExitBaseDistance = diagonalExitBaseDistance;
                EarlyRiseConnectionHeight = earlyRiseConnectionHeight;
                MidRiseConnectionHeight = midRiseConnectionHeight;
                MinimumActiveFrameClearance = minimumActiveFrameClearance;
                MinimumEmergenceVisibleVertexHeight = minimumEmergenceVisibleVertexHeight;
                FirstStrikeTipAim = firstStrikeTipAim;
                FirstStrikeTipLead = firstStrikeTipLead;
                SecondStrikeTipAim = secondStrikeTipAim;
                SecondStrikeTipLead = secondStrikeTipLead;
                RootDrift = rootDrift;
                SurfaceAnchorDrift = surfaceAnchorDrift;
                AnimatedBoneCount = animatedBoneCount;
            }

            public float IntroLength { get; }
            public float LoopLength { get; }
            public float IntroRise { get; }
            public float StrikeForward { get; }
            public float StrikeDrop { get; }
            public float StrikeLateral { get; }
            public float SecondStrikeForward { get; }
            public float SecondStrikeDrop { get; }
            public float SecondStrikeLateral { get; }
            public float FirstWindupLift { get; }
            public float FirstWindupRetreat { get; }
            public float FirstLateStrikeOutward { get; }
            public float FirstImpactHoldError { get; }
            public float SecondWindupLift { get; }
            public float SecondWindupRetreat { get; }
            public float SecondLateStrikeOutward { get; }
            public float SecondImpactHoldError { get; }
            public float PreparedHeight { get; }
            public float ReturnError { get; }
            public float HiddenReturnError { get; }
            public float HiddenPoseError { get; }
            public float IntroHiddenChainOffset { get; }
            public float SourceReadyChainOffset { get; }
            public float LoopHiddenChainOffset { get; }
            public float SourceHiddenScaleRatio { get; }
            public float SourceMidScaleRatio { get; }
            public float SourceReadyScaleError { get; }
            public float InitialRiseUp { get; }
            public float InitialRiseLateral { get; }
            public float TipFrontClearanceBeforeRise { get; }
            public float TipRiseBeforeFrontClear { get; }
            public float DiagonalExitBaseDistance { get; }
            public float EarlyRiseConnectionHeight { get; }
            public float MidRiseConnectionHeight { get; }
            public float MinimumActiveFrameClearance { get; }
            public float MinimumEmergenceVisibleVertexHeight { get; }
            public float FirstStrikeTipAim { get; }
            public float FirstStrikeTipLead { get; }
            public float SecondStrikeTipAim { get; }
            public float SecondStrikeTipLead { get; }
            public float RootDrift { get; }
            public float SurfaceAnchorDrift { get; }
            public int AnimatedBoneCount { get; }
        }
    }
}
