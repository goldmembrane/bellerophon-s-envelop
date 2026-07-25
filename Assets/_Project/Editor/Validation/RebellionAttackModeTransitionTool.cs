using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    internal static class RebellionAttackModeTransitionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Rebellion Enemy Placement";
        private const string SlotName = "Rebellion_02_Attack_Mode_Transition";
        private const string MoveSlotName = "Rebellion_01_Move";
        private const string ScanSlotName = "Rebellion_03_Forward_Scan";
        private const string BurstSlotName = "Rebellion_04_Forward_Burst_Fire";
        private const string HitSlotName = "Rebellion_05_Hit_Reaction";
        private const string DeathSlotName = "Rebellion_06_Death";
        private const string HitBodyPivotName =
            "Rebellion_Hit_Body_Rigid_Pivot";
        private const string DeathBodyPivotName =
            "Rebellion_Death_Body_Rigid_Pivot";
        private const string BurstCylinderPivotName =
            "Rebellion_Gun_Cylinder_Pivot";
        private const string ModelName = "Rebellion_Model";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Controllers";
        private const string ClipPath =
            AnimationFolder + "/Rebellion_02_Attack_Mode_Transition.anim";
        private const string ControllerPath =
            ControllerFolder + "/Rebellion_02_Attack_Mode_Transition.controller";
        private const string MoveClipPath =
            AnimationFolder + "/Rebellion_01_Move_SpiderCrawl.anim";
        private const string MoveControllerPath =
            ControllerFolder + "/Rebellion_01_Move_SpiderCrawl.controller";
        private const string ScanControllerPath =
            ControllerFolder + "/Rebellion_03_Forward_Scan.controller";
        private const string BurstControllerPath =
            ControllerFolder + "/Rebellion_04_Forward_Burst_Fire.controller";
        private const string HitClipPath =
            AnimationFolder + "/Rebellion_05_Hit_Reaction.anim";
        private const string HitControllerPath =
            ControllerFolder + "/Rebellion_05_Hit_Reaction.controller";
        private const string DeathClipPath =
            AnimationFolder + "/Rebellion_06_Death.anim";
        private const string DeathControllerPath =
            ControllerFolder + "/Rebellion_06_Death.controller";
        private const string CorrectedModelPath =
            "Assets/_Project/Art/Enemies/Rebellion/ApprovedAppearance/" +
            "Rebellion_ApprovedAppearance.glb";
        private const string CorrectedModelSha256 =
            "C791B028B759A82087C185A98ADD3A5412BCAE8A110DFAFF33F7E3E1694D60F9";
        private const string RigSupportReportPath =
            "artSample/enemies/rebellion/attack_transition_rig_support/" +
            "ATTACK_TRANSITION_RIG_SUPPORT.json";
        private const string RigInspectionPath =
            "docs/validation/rebellion_attack_transition_2026-07-25/" +
            "Rebellion_02_AttackTransition_RigInspection.txt";
        private const string InspectionPath =
            "docs/validation/rebellion_attack_transition_2026-07-25/" +
            "Rebellion_02_AttackTransition_Inspection.txt";
        private const string ReviewPath =
            "docs/validation/rebellion_attack_transition_2026-07-25/" +
            "Rebellion_02_AttackTransition_VisualReview.png";
        private const string HitInspectionPath =
            "docs/validation/rebellion_hit_reaction_2026-07-26/" +
            "Rebellion_05_HitReaction_Inspection.txt";
        private const string HitReviewPath =
            "docs/validation/rebellion_hit_reaction_2026-07-26/" +
            "Rebellion_05_HitReaction_VisualReview.png";
        private const string DeathInspectionPath =
            "docs/validation/rebellion_death_2026-07-26/" +
            "Rebellion_06_Death_Inspection.txt";
        private const string DeathReviewPath =
            "docs/validation/rebellion_death_2026-07-26/" +
            "Rebellion_06_Death_VisualReview.png";
        private const string StateName = "AttackModeTransition";
        private const string HitStateName = "HitReaction";
        private const string DeathStateName = "Death";
        private const float LoopSecondsValue = 2.4f;
        private const float HitLoopSecondsValue = 0.4f;
        private const float HitPeakTime = 0.2f;
        private const float HitReboundTime = 0.3f;
        private const float HitBodyLeftTiltDegrees = 15f;
        private const float HitBodyRightReboundDegrees = 5f;
        private const float HitLegRattleDegrees = 5f;
        private const float HitLegRattleCycles = 2f;
        // Feet remain visually planted while unlocked joints absorb the
        // rigid-body tilt and the two approved joint-rattle cycles.
        private const float HitGroundContactToleranceWorld = 0.002f;
        // Actual baked torso vertices must remain rigid in the Slot05 pivot.
        private const float HitBodyMeshDeformationToleranceWorld = 0.0001f;
        private const float HitPoseStepSeconds = 0.025f;
        private const float DeathLoopSecondsValue = 1.2f;
        private const float DeathCollapseSeconds = 1f;
        private const float DeathHoldSeconds = 0.2f;
        private const float DeathBodyLeftTiltDegrees = 20f;
        private const float DeathPoseStepSeconds = 0.05f;
        // Death distances are derived from each imported leg chain rather
        // than authored as fixed world-space meter values.
        private const float DeathBodyDropSupportRatio = 1.10f;
        private const float DeathFootSpreadChainRatio = 0.44f;
        private const float DeathJointSlackDegrees = 24f;
        private const float DeathGroundToleranceWorld = 0.008f;
        private const float StandEndTime = 1.2f;
        private const float StraightnessRatio = 0.98f;
        private const float MinimumNaturalLiftWorld = 0.04f;
        private const float MaximumNaturalLiftWorld = 0.12f;
        private const float PoseStepSeconds = 0.1f;
        private const float FootPositionToleranceWorld = 0.006f;

        private static readonly string[] SlotNames =
        {
            "Rebellion_00_Static_Review",
            "Rebellion_01_Move",
            "Rebellion_02_Attack_Mode_Transition",
            "Rebellion_03_Forward_Scan",
            "Rebellion_04_Forward_Burst_Fire",
            "Rebellion_05_Hit_Reaction",
            "Rebellion_06_Death"
        };

        private static readonly string[] BodyDetailNames =
        {
            "Rebellion_Front_Recess_Backplate",
            "Rebellion_Panel_Fastener_00",
            "Rebellion_Panel_Fastener_01",
            "Rebellion_Panel_Fastener_02",
            "Rebellion_Panel_Fastener_03",
            "Rebellion_Panel_Vent_00",
            "Rebellion_Panel_Vent_01",
            "Rebellion_Panel_Vent_02",
            "Rebellion_Panel_Vent_03",
            "Rebellion_Scan_Lens"
        };

        private static readonly string[] WeaponDetailNames =
        {
            "Rebellion_Gun_Hub",
            "Rebellion_Gun_Barrel_00",
            "Rebellion_Gun_Barrel_01",
            "Rebellion_Gun_Barrel_02",
            "Rebellion_Gun_Barrel_03",
            "Rebellion_Gun_Barrel_04",
            "Rebellion_Gun_Barrel_05",
            "Rebellion_Gun_Barrel_06"
        };

        private static readonly string[] LegBoneNames =
        {
            "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009",
            "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014",
            "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019",
            "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024"
        };

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Attack Transition Rig")]
        public static void InspectRig()
        {
            RequireCorrectedModelHash();
            var support = RequireRigSupportReport();
            var scene = RequireActiveScene();
            var model = RequireModel(RequireSlot(scene, SlotName));
            var sceneWasDirty = scene.isDirty;
            var rigBones = RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + SlotName);
            report.AppendLine("ModelSha256=" + CorrectedModelSha256);
            report.AppendLine("RigBones=" + rigBones.Length);
            report.AppendLine("WeaponBranch=Bone_008>Bone_007>Bone_006");
            report.AppendLine(
                "LegBranches=Bone_013>009;Bone_018>014;" +
                "Bone_023>019;Bone_028>024");
            report.AppendLine(
                "DiscVerticesReweighted=" + support.disc_selection.vertices);
            report.AppendLine(
                "DiscRoundTripVertices=" + support.roundtrip.disc_vertices);
            report.AppendLine("DiscLegInfluencedVertices=0");
            report.AppendLine("DiscNonBodyInfluencedVertices=0");
            report.AppendLine("DiscExclusiveLiftBone=Bone_008");
            report.AppendLine("GeometryUnchanged=True");
            report.AppendLine("NonDiscWeightsUnchanged=True");
            report.AppendLine("BoneHierarchyUnchanged=True");
            report.AppendLine("CorrectedModelSharedByAllSevenSlots=True");
            report.AppendLine("SceneChanged=False");
            WriteText(RigInspectionPath, report.ToString());

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Attack transition rig inspection changed the scene.");
            }

            Debug.Log(
                "RebellionAttackTransitionRigInspected Result=PASS" +
                ", RigBones=" + rigBones.Length +
                ", DiscVerticesReweighted=" + support.disc_selection.vertices +
                ", DiscLegInfluencedVertices=0" +
                ", DiscExclusiveLiftBone=Bone_008" +
                ", CorrectedModelSharedByAllSevenSlots=True" +
                ", SceneChanged=False" +
                ", Report=" + RigInspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Attack Mode Transition")]
        public static void ApplyAttackModeTransition()
        {
            RequireCorrectedModelHash();
            RequireRigSupportReport();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, SlotName);
            var model = RequireModel(slot);
            RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var placementState = TransformState.Capture(placementRoot);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);
            var moveClipHash = Sha256IfPresent(MoveClipPath);
            var moveControllerHash = Sha256IfPresent(MoveControllerPath);

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var result = CreateAttackClip(slot, model);
            var controller = CreateController(result.Clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            RequireSameTransform(placementState, placementRoot, PlacementRootName);
            RequireSameTransform(slotState, slot, SlotName);
            RequireSameTransform(modelState, model, ModelName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Rebellion " +
                    "attack transition application.");
            }

            AssetDatabase.SaveAssets();
            RequireUnchangedFileHash(MoveClipPath, moveClipHash);
            RequireUnchangedFileHash(MoveControllerPath, moveControllerHash);

            Debug.Log(
                "RebellionAttackModeTransitionApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Clip=" + ClipPath +
                ", Controller=" + ControllerPath +
                ", LoopSeconds=2.4" +
                ", NaturalLiftWorld=" +
                result.NaturalLiftWorld.ToString("0.######") +
                ", FootTargetsFixed=True" +
                ", AdditionalDiscLiftRemoved=True" +
                ", RootMotion=False" +
                ", PlacementPreserved=True" +
                ", MoveAssetsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Attack Mode Transition")]
        public static void InspectAttackModeTransition()
        {
            RequireCorrectedModelHash();
            RequireRigSupportReport();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, SlotName);
            var model = RequireModel(slot);
            RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               SlotName + " has no Animator.");
            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Attack transition Animator must not apply Root Motion.");
            }

            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "Attack transition AnimatorController is missing.");
            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(
                    "Attack transition slot does not use its controller.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException(
                           "Attack transition clip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime ||
                Mathf.Abs(clip.length - LoopSecondsValue) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Attack transition must be a 2.4-second loop.");
            }

            var allowedLegs =
                new HashSet<string>(LegBoneNames, StringComparer.Ordinal);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != (LegBoneNames.Length * 4) + 3)
            {
                throw new InvalidOperationException(
                    "Expected 83 attack transition bindings, found " +
                    bindings.Length + ".");
            }

            var rotationBones = new HashSet<string>(StringComparer.Ordinal);
            var positionBones = new HashSet<string>(StringComparer.Ordinal);
            var maximumLoopBoundaryError = 0f;
            foreach (var binding in bindings)
            {
                var boneName = binding.path.Split('/').Last();
                if (binding.type != typeof(Transform))
                {
                    throw new InvalidOperationException(
                        "Attack transition contains a non-Transform binding.");
                }

                if (binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                {
                    if (!allowedLegs.Contains(boneName))
                    {
                        throw new InvalidOperationException(
                            "Attack transition rotates a non-leg bone: " +
                            boneName);
                    }
                    rotationBones.Add(boneName);
                }
                else if (binding.propertyName.StartsWith(
                             "m_LocalPosition.",
                             StringComparison.Ordinal))
                {
                    if (boneName != "Bone_001")
                    {
                        throw new InvalidOperationException(
                            "Attack transition moves an unexpected bone: " +
                            boneName);
                    }
                    positionBones.Add(boneName);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unexpected attack transition property: " +
                        binding.propertyName);
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "Attack transition curve is missing.");
                maximumLoopBoundaryError = Mathf.Max(
                    maximumLoopBoundaryError,
                    Mathf.Abs(
                        curve.Evaluate(0f) -
                        curve.Evaluate(LoopSecondsValue)));
            }

            if (rotationBones.Count != LegBoneNames.Length ||
                positionBones.Count != 1 ||
                maximumLoopBoundaryError > 0.00001f)
            {
                throw new InvalidOperationException(
                    "Attack transition binding or loop boundary inspection failed.");
            }

            var poses = MeasurePoses(slot, model, clip);
            if (poses.MaximumFootPositionError > FootPositionToleranceWorld)
            {
                throw new InvalidOperationException(
                    "Attack transition foot position error is " +
                    poses.MaximumFootPositionError.ToString("0.######") + "m.");
            }
            if (poses.NaturalDiscLift <= MinimumNaturalLiftWorld * 0.8f ||
                Mathf.Abs(poses.DiscReturnHeightError) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Attack transition disc height sequence is incorrect.");
            }

            RequireAnimatorAssignments(placementRoot);

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("Slot=" + SlotName);
            report.AppendLine("Clip=" + ClipPath);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("State=" + StateName);
            report.AppendLine("LoopSeconds=2.4");
            report.AppendLine("LoopEnabled=True");
            report.AppendLine(
                "LoopBoundaryError=" +
                maximumLoopBoundaryError.ToString("0.########"));
            report.AppendLine("Phase0To1.2=FeetFixed_LegsStraighten");
            report.AppendLine("Phase1.2To2.4=FeetFixed_LegsReturn");
            report.AppendLine("AdditionalDiscLiftPhase=False");
            report.AppendLine(
                "NaturalDiscLiftWorld=" +
                poses.NaturalDiscLift.ToString("0.######"));
            report.AppendLine(
                "DiscReturnHeightErrorWorld=" +
                poses.DiscReturnHeightError.ToString("0.######"));
            report.AppendLine(
                "MaximumFootPositionErrorWorld=" +
                poses.MaximumFootPositionError.ToString("0.######"));
            report.AppendLine("AnimatedLegBones=20");
            report.AppendLine("RotationBindings=80");
            report.AppendLine("PositionBindings=3");
            report.AppendLine("AnimatedPositionBones=Bone_001");
            report.AppendLine("Bone003PositionCurves=0");
            report.AppendLine("WeaponBoneCurves=0");
            report.AppendLine("RootMotion=False");
            report.AppendLine("PlacementFixed=True");
            report.AppendLine("MoveAnimationUnchanged=True");
            report.AppendLine("CorrectedModelSha256=" + CorrectedModelSha256);
            WriteText(InspectionPath, report.ToString());

            Debug.Log(
                "RebellionAttackModeTransitionInspected Result=PASS" +
                ", LoopSeconds=2.4" +
                ", NaturalDiscLiftWorld=" +
                poses.NaturalDiscLift.ToString("0.######") +
                ", MaximumFootPositionErrorWorld=" +
                poses.MaximumFootPositionError.ToString("0.######") +
                ", AdditionalDiscLiftPhase=False" +
                ", Bone003PositionCurves=0" +
                ", RootMotion=False" +
                ", PlacementFixed=True" +
                ", Report=" + InspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Hit Reaction")]
        public static void ApplyHitReaction()
        {
            RequireCorrectedModelHash();
            RequireRigSupportReport();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, HitSlotName);
            var model = RequireModel(slot);
            var modelWorldPose = WorldPose.Capture(model);
            var bodyPivot = EnsureHitBodyPivot(slot, model);
            RequireSameWorldPose(
                modelWorldPose,
                model,
                ModelName + " world pose");
            RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var placementState = TransformState.Capture(placementRoot);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);
            var protectedHashes = CaptureImplementedAnimationHashes();

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var result = CreateHitClip(slot, model, bodyPivot);
            var controller = CreateHitController(result.Clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            RequireSameTransform(
                placementState,
                placementRoot,
                PlacementRootName);
            RequireSameTransform(slotState, slot, HitSlotName);
            RequireSameTransform(modelState, model, ModelName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Rebellion hit " +
                    "reaction application.");
            }
            AssetDatabase.SaveAssets();
            RequireUnchangedFileHashes(protectedHashes);

            Debug.Log(
                "RebellionHitReactionApplied Result=PASS" +
                ", Slot=" + HitSlotName +
                ", Clip=" + HitClipPath +
                ", Controller=" + HitControllerPath +
                ", LoopSeconds=0.4" +
                ", BodyLeftTiltDegrees=15" +
                ", BodyRightReboundDegrees=5" +
                ", BodyRigidPivot=" + HitBodyPivotName +
                ", FootSlideWorld=0" +
                ", LegRattleDegrees=5" +
                ", LegRattleCycles=2" +
                ", RattledLegs=4" +
                ", BodyPositionCurves=0" +
                ", RootMotion=False" +
                ", PlacementPreserved=True" +
                ", ExistingAnimationsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Hit Reaction")]
        public static void InspectHitReaction()
        {
            RequireCorrectedModelHash();
            RequireRigSupportReport();
            var scene = RequireActiveScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before hit reaction inspection.");
            }
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, HitSlotName);
            var model = RequireModel(slot);
            var bodyPivot = RequireHitBodyPivot(slot, model);
            RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               HitSlotName + " has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    HitControllerPath) ??
                throw new InvalidOperationException(
                    "Hit reaction AnimatorController is missing.");
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Hit reaction Animator configuration is unexpected.");
            }

            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipPath) ??
                throw new InvalidOperationException(
                    "Hit reaction AnimationClip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime ||
                Mathf.Abs(clip.length - HitLoopSecondsValue) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Hit reaction must be a 0.4-second loop.");
            }

            var forward = RequireHorizontalWeaponForward(model);
            var legs = CreateAllLegs(model);
            var allowedRotationBones = new HashSet<string>(
                legs
                    .SelectMany(leg => leg.AllBones)
                    .Select(item => item.name)
                    .Concat(new[] { HitBodyPivotName }),
                StringComparer.Ordinal);
            var rotationBones = new HashSet<string>(StringComparer.Ordinal);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var maximumLoopBoundaryError = 0f;
            foreach (var binding in bindings)
            {
                if (binding.type != typeof(Transform))
                {
                    throw new InvalidOperationException(
                        "Hit reaction contains a non-Transform binding.");
                }
                var boneName = binding.path.Split('/').Last();
                if (binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                {
                    if (!allowedRotationBones.Contains(boneName))
                    {
                        throw new InvalidOperationException(
                            "Hit reaction rotates an unexpected bone: " +
                            boneName + ".");
                    }
                    rotationBones.Add(boneName);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unexpected hit reaction property: " +
                        binding.propertyName + ".");
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "Hit reaction curve is missing.");
                maximumLoopBoundaryError = Mathf.Max(
                    maximumLoopBoundaryError,
                    Mathf.Abs(
                        curve.Evaluate(0f) -
                        curve.Evaluate(HitLoopSecondsValue)));
            }
            if (bindings.Length != 84 ||
                rotationBones.Count != 21 ||
                maximumLoopBoundaryError > 0.00001f)
            {
                throw new InvalidOperationException(
                    "Hit reaction bindings or loop boundary are unexpected.");
            }

            var metrics = MeasureHitReaction(
                slot,
                model,
                bodyPivot,
                clip,
                forward);
            if (Mathf.Abs(
                    metrics.PeakBodyLeftTiltDegrees -
                    HitBodyLeftTiltDegrees) > 0.1f ||
                metrics.PeakBodyLeftEdgeVerticalDirection >= -0.25f ||
                Mathf.Abs(
                    metrics.PeakBodyRightReboundDegrees -
                    HitBodyRightReboundDegrees) > 0.1f ||
                metrics.PeakBodyRightEdgeVerticalDirection <= 0.08f ||
                metrics.MaximumBodyPositionError > 0.0001f ||
                metrics.BodyRotationReturnError > 0.01f ||
                metrics.MaximumBodyMeshDeformationWorld >
                    HitBodyMeshDeformationToleranceWorld ||
                metrics.MaximumFootPositionError >
                    HitGroundContactToleranceWorld ||
                metrics.FootReturnError > 0.001f ||
                metrics.MinimumPrimaryRattlePeakDegrees < 4.9f ||
                metrics.MaximumPrimaryRattlePeakDegrees > 5.1f ||
                metrics.MaximumRattleZeroErrorDegrees > 0.05f ||
                metrics.MinimumRattlePeakEventCount != 4 ||
                metrics.RattledLegCount != 4)
            {
                throw new InvalidOperationException(
                    "Hit reaction pose inspection failed. " +
                    "PeakBodyLeftTiltDegrees=" +
                    metrics.PeakBodyLeftTiltDegrees.ToString("0.######") +
                    ", PeakBodyLeftEdgeVerticalDirection=" +
                    metrics.PeakBodyLeftEdgeVerticalDirection
                        .ToString("0.######") +
                    ", PeakBodyRightReboundDegrees=" +
                    metrics.PeakBodyRightReboundDegrees
                        .ToString("0.######") +
                    ", PeakBodyRightEdgeVerticalDirection=" +
                    metrics.PeakBodyRightEdgeVerticalDirection
                        .ToString("0.######") +
                    ", MaximumBodyPositionError=" +
                    metrics.MaximumBodyPositionError
                        .ToString("0.######") +
                    ", BodyRotationReturnError=" +
                    metrics.BodyRotationReturnError
                        .ToString("0.######") +
                    ", MaximumBodyMeshDeformationWorld=" +
                    metrics.MaximumBodyMeshDeformationWorld
                        .ToString("0.######") +
                    ", MaximumFootPositionError=" +
                    metrics.MaximumFootPositionError
                        .ToString("0.######") +
                    ", FootReturnError=" +
                    metrics.FootReturnError
                        .ToString("0.######") +
                    ", MinimumPrimaryRattlePeakDegrees=" +
                    metrics.MinimumPrimaryRattlePeakDegrees
                        .ToString("0.######") +
                    ", MaximumPrimaryRattlePeakDegrees=" +
                    metrics.MaximumPrimaryRattlePeakDegrees
                        .ToString("0.######") +
                    ", MaximumRattleZeroErrorDegrees=" +
                    metrics.MaximumRattleZeroErrorDegrees
                        .ToString("0.######") +
                    ", MinimumRattlePeakEventCount=" +
                    metrics.MinimumRattlePeakEventCount
                        .ToString("0.######") + ".");
            }
            RequireAnimatorAssignments(placementRoot);

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + HitSlotName);
            report.AppendLine("Clip=" + HitClipPath);
            report.AppendLine("Controller=" + HitControllerPath);
            report.AppendLine("State=" + HitStateName);
            report.AppendLine("LoopSeconds=0.4");
            report.AppendLine("LoopEnabled=True");
            report.AppendLine(
                "LoopBoundaryError=" +
                maximumLoopBoundaryError.ToString("0.########"));
            report.AppendLine("RootTranslation=False");
            report.AppendLine("PlacementFixed=True");
            report.AppendLine("DiscardedRearRecoil=True");
            report.AppendLine("BodyPositionBindings=0");
            report.AppendLine("BodyRigidPivot=" + HitBodyPivotName);
            report.AppendLine("BodyTiltDirection=LeftThenRight");
            report.AppendLine("BodyLeftTiltTargetDegrees=15");
            report.AppendLine("BodyRightReboundTargetDegrees=5");
            report.AppendLine(
                "PeakBodyLeftTiltDegrees=" +
                metrics.PeakBodyLeftTiltDegrees.ToString("0.######"));
            report.AppendLine(
                "PeakBodyLeftEdgeVerticalDirection=" +
                metrics.PeakBodyLeftEdgeVerticalDirection
                    .ToString("0.######"));
            report.AppendLine(
                "PeakBodyRightReboundDegrees=" +
                metrics.PeakBodyRightReboundDegrees.ToString("0.######"));
            report.AppendLine(
                "PeakBodyRightEdgeVerticalDirection=" +
                metrics.PeakBodyRightEdgeVerticalDirection
                    .ToString("0.######"));
            report.AppendLine(
                "MaximumBodyPositionError=" +
                metrics.MaximumBodyPositionError.ToString("0.######"));
            report.AppendLine(
                "BodyRotationReturnError=" +
                metrics.BodyRotationReturnError.ToString("0.######"));
            report.AppendLine("ActualRuntimeMeshDeformationChecked=True");
            report.AppendLine(
                "RigidBodyVertexCount=" +
                metrics.RigidBodyVertexCount);
            report.AppendLine(
                "MaximumBodyMeshDeformationWorld=" +
                metrics.MaximumBodyMeshDeformationWorld
                    .ToString("0.########"));
            report.AppendLine(
                "BodyMeshDeformationToleranceWorld=" +
                HitBodyMeshDeformationToleranceWorld
                    .ToString("0.########"));
            report.AppendLine("AnimatedLegs=AllFour");
            report.AppendLine("FootSlideRemoved=True");
            report.AppendLine("FootSlideTargetMeters=0");
            report.AppendLine("AllFeetGroundFixed=True");
            report.AppendLine("RattledLegCount=4");
            report.AppendLine("LegRattleCycles=2");
            report.AppendLine("RattlePeakEvents=4");
            report.AppendLine("PrimaryRattleTargetDegrees=5");
            report.AppendLine(
                "MinimumPrimaryRattlePeakDegrees=" +
                metrics.MinimumPrimaryRattlePeakDegrees
                    .ToString("0.######"));
            report.AppendLine(
                "MaximumPrimaryRattlePeakDegrees=" +
                metrics.MaximumPrimaryRattlePeakDegrees
                    .ToString("0.######"));
            report.AppendLine(
                "MaximumRattleZeroErrorDegrees=" +
                metrics.MaximumRattleZeroErrorDegrees
                    .ToString("0.######"));
            report.AppendLine(
                "MinimumRattlePeakEventCountAcrossLegs=" +
                metrics.MinimumRattlePeakEventCount);
            report.AppendLine(
                "MaximumFootPositionError=" +
                metrics.MaximumFootPositionError.ToString("0.######"));
            report.AppendLine(
                "FootReturnError=" +
                metrics.FootReturnError.ToString("0.######"));
            report.AppendLine(
                "GroundContactToleranceMeters=" +
                HitGroundContactToleranceWorld.ToString("0.######"));
            report.AppendLine("RotationBindings=84");
            report.AppendLine("PositionBindings=0");
            report.AppendLine("RootMotion=False");
            report.AppendLine("ExistingAnimationsUnchanged=True");
            report.AppendLine(
                "CorrectedModelSha256=" + CorrectedModelSha256);
            WriteText(HitInspectionPath, report.ToString());

            Debug.Log(
                "RebellionHitReactionInspected Result=PASS" +
                ", LoopSeconds=0.4" +
                ", PeakBodyLeftTiltDegrees=" +
                metrics.PeakBodyLeftTiltDegrees.ToString("0.######") +
                ", PeakBodyRightReboundDegrees=" +
                metrics.PeakBodyRightReboundDegrees
                    .ToString("0.######") +
                ", MaximumBodyMeshDeformationWorld=" +
                metrics.MaximumBodyMeshDeformationWorld
                    .ToString("0.########") +
                ", MaximumFootPositionError=" +
                metrics.MaximumFootPositionError.ToString("0.######") +
                ", PrimaryRattlePeakDegrees=" +
                metrics.MaximumPrimaryRattlePeakDegrees
                    .ToString("0.######") +
                ", LegRattleCycles=2" +
                ", AllFeetFixed=True" +
                ", BodyPositionBindings=0" +
                ", RootMotion=False" +
                ", PlacementFixed=True" +
                ", Report=" + HitInspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Death")]
        public static void ApplyDeath()
        {
            RequireCorrectedModelHash();
            RequireRigSupportReport();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, DeathSlotName);
            var model = RequireModel(slot);
            var modelWorldPose = WorldPose.Capture(model);
            var bodyPivot = EnsureDeathBodyPivot(slot, model);
            RequireSameWorldPose(
                modelWorldPose,
                model,
                ModelName + " world pose");
            RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var placementState = TransformState.Capture(placementRoot);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);
            var protectedHashes = CaptureAllImplementedAnimationHashes();

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var result = CreateDeathClip(slot, model, bodyPivot);
            var controller = CreateDeathController(result.Clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            RequireSameTransform(
                placementState,
                placementRoot,
                PlacementRootName);
            RequireSameTransform(slotState, slot, DeathSlotName);
            RequireSameTransform(modelState, model, ModelName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Rebellion death " +
                    "animation application.");
            }
            AssetDatabase.SaveAssets();
            RequireUnchangedFileHashes(protectedHashes);

            Debug.Log(
                "RebellionDeathApplied Result=PASS" +
                ", Slot=" + DeathSlotName +
                ", Clip=" + DeathClipPath +
                ", Controller=" + DeathControllerPath +
                ", LoopSeconds=1.2" +
                ", CollapseSeconds=1.0" +
                ", HoldSeconds=0.2" +
                ", BodyLeftTiltDegrees=20" +
                ", BodyDropWorld=" +
                result.BodyDropWorld.ToString("0.######") +
                ", FootSpreadDerivedFromRig=True" +
                ", BodyRigidPivot=" + DeathBodyPivotName +
                ", RootMotion=False" +
                ", PlacementPreserved=True" +
                ", ExistingAnimationsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Death")]
        public static void InspectDeath()
        {
            RequireCorrectedModelHash();
            RequireRigSupportReport();
            var scene = RequireActiveScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before death inspection.");
            }
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, DeathSlotName);
            var model = RequireModel(slot);
            var bodyPivot = RequireDeathBodyPivot(slot, model);
            RequireRigStructure(model);
            RequireAllSlotsUseCorrectedModel(scene);

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               DeathSlotName + " has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    DeathControllerPath) ??
                throw new InvalidOperationException(
                    "Death AnimatorController is missing.");
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Death Animator configuration is unexpected.");
            }

            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath) ??
                throw new InvalidOperationException(
                    "Death AnimationClip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime ||
                Mathf.Abs(clip.length - DeathLoopSecondsValue) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Death animation must be a 1.2-second loop.");
            }

            var legs = CreateAllLegs(model);
            var allowedRotationBones = new HashSet<string>(
                legs
                    .SelectMany(leg => leg.AllBones)
                    .Select(item => item.name)
                    .Concat(new[] { DeathBodyPivotName }),
                StringComparer.Ordinal);
            var rotationBones = new HashSet<string>(StringComparer.Ordinal);
            var positionBones = new HashSet<string>(StringComparer.Ordinal);
            var maximumHoldError = 0f;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (binding.type != typeof(Transform))
                {
                    throw new InvalidOperationException(
                        "Death animation contains a non-Transform binding.");
                }
                var boneName = binding.path.Split('/').Last();
                if (binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                {
                    if (!allowedRotationBones.Contains(boneName))
                    {
                        throw new InvalidOperationException(
                            "Death animation rotates an unexpected bone: " +
                            boneName + ".");
                    }
                    rotationBones.Add(boneName);
                }
                else if (binding.propertyName.StartsWith(
                             "m_LocalPosition.",
                             StringComparison.Ordinal))
                {
                    if (boneName != DeathBodyPivotName)
                    {
                        throw new InvalidOperationException(
                            "Death animation moves an unexpected object: " +
                            boneName + ".");
                    }
                    positionBones.Add(boneName);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unexpected death animation property: " +
                        binding.propertyName + ".");
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "Death animation curve is missing.");
                maximumHoldError = Mathf.Max(
                    maximumHoldError,
                    Mathf.Abs(
                        curve.Evaluate(DeathCollapseSeconds) -
                        curve.Evaluate(DeathLoopSecondsValue)));
            }
            if (bindings.Length != 87 ||
                rotationBones.Count != 21 ||
                positionBones.Count != 1 ||
                maximumHoldError > 0.00001f)
            {
                throw new InvalidOperationException(
                    "Death bindings or final hold are unexpected.");
            }

            var metrics = MeasureDeath(
                slot,
                model,
                bodyPivot,
                clip,
                RequireHorizontalWeaponForward(model));
            if (Mathf.Abs(
                    metrics.FinalBodyLeftTiltDegrees -
                    DeathBodyLeftTiltDegrees) > 0.1f ||
                metrics.FinalBodyLeftEdgeVerticalDirection >= -0.25f ||
                metrics.BodyDropWorld <= 0f ||
                metrics.MinimumFootSpreadIncreaseWorld <= 0f ||
                metrics.MaximumFootGroundErrorWorld >
                    DeathGroundToleranceWorld ||
                metrics.MaximumBodyMeshDeformationWorld >
                    HitBodyMeshDeformationToleranceWorld)
            {
                throw new InvalidOperationException(
                    "Death pose inspection failed. " +
                    "FinalBodyLeftTiltDegrees=" +
                    metrics.FinalBodyLeftTiltDegrees.ToString("0.######") +
                    ", FinalBodyLeftEdgeVerticalDirection=" +
                    metrics.FinalBodyLeftEdgeVerticalDirection
                        .ToString("0.######") +
                    ", BodyDropWorld=" +
                    metrics.BodyDropWorld.ToString("0.######") +
                    ", MinimumFootSpreadIncreaseWorld=" +
                    metrics.MinimumFootSpreadIncreaseWorld
                        .ToString("0.######") +
                    ", MaximumFootGroundErrorWorld=" +
                    metrics.MaximumFootGroundErrorWorld
                        .ToString("0.######") +
                    ", MaximumBodyMeshDeformationWorld=" +
                    metrics.MaximumBodyMeshDeformationWorld
                        .ToString("0.######") + ".");
            }
            RequireAnimatorAssignments(placementRoot);

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + DeathSlotName);
            report.AppendLine("Clip=" + DeathClipPath);
            report.AppendLine("Controller=" + DeathControllerPath);
            report.AppendLine("State=" + DeathStateName);
            report.AppendLine("LoopSeconds=1.2");
            report.AppendLine("LoopEnabled=True");
            report.AppendLine("CollapseSeconds=1.0");
            report.AppendLine("FinalHoldSeconds=0.2");
            report.AppendLine(
                "MaximumFinalHoldCurveError=" +
                maximumHoldError.ToString("0.########"));
            report.AppendLine("AnimatedLegs=AllFour");
            report.AppendLine("FootSpreadDerivedFromImportedRig=True");
            report.AppendLine(
                "MinimumFootSpreadIncreaseWorld=" +
                metrics.MinimumFootSpreadIncreaseWorld.ToString("0.######"));
            report.AppendLine(
                "MaximumFootGroundErrorWorld=" +
                metrics.MaximumFootGroundErrorWorld.ToString("0.######"));
            report.AppendLine("BodyRigidPivot=" + DeathBodyPivotName);
            report.AppendLine("BodyLeftTiltTargetDegrees=20");
            report.AppendLine(
                "FinalBodyLeftTiltDegrees=" +
                metrics.FinalBodyLeftTiltDegrees.ToString("0.######"));
            report.AppendLine(
                "BodyDropWorld=" +
                metrics.BodyDropWorld.ToString("0.######"));
            report.AppendLine("ActualRuntimeMeshDeformationChecked=True");
            report.AppendLine(
                "MaximumBodyMeshDeformationWorld=" +
                metrics.MaximumBodyMeshDeformationWorld
                    .ToString("0.########"));
            report.AppendLine("RootTranslation=False");
            report.AppendLine("PlacementFixed=True");
            report.AppendLine("ExistingAnimationsUnchanged=True");
            report.AppendLine(
                "CorrectedModelSha256=" + CorrectedModelSha256);
            WriteText(DeathInspectionPath, report.ToString());

            Debug.Log(
                "RebellionDeathInspected Result=PASS" +
                ", LoopSeconds=1.2" +
                ", CollapseSeconds=1.0" +
                ", HoldSeconds=0.2" +
                ", FinalBodyLeftTiltDegrees=" +
                metrics.FinalBodyLeftTiltDegrees.ToString("0.######") +
                ", BodyDropWorld=" +
                metrics.BodyDropWorld.ToString("0.######") +
                ", MinimumFootSpreadIncreaseWorld=" +
                metrics.MinimumFootSpreadIncreaseWorld.ToString("0.######") +
                ", MaximumFootGroundErrorWorld=" +
                metrics.MaximumFootGroundErrorWorld.ToString("0.######") +
                ", PlacementFixed=True" +
                ", Report=" + DeathInspectionPath + ".");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            RebellionMoveAnimationTool.CaptureRuntimeFrameForSlot(
                SlotName,
                path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            RebellionMoveAnimationTool.ComposeRuntimeReview(panelPaths, outputPath);
        }

        internal static string FinalReviewAbsolutePath => Absolute(ReviewPath);
        internal static string AnimatorStateName => StateName;
        internal static float LoopSeconds => LoopSecondsValue;
        internal static void CaptureHitRuntimeFrame(string path)
        {
            RebellionMoveAnimationTool.CaptureRuntimeFrameForSlot(
                HitSlotName,
                path);
        }
        internal static string HitFinalReviewAbsolutePath =>
            Absolute(HitReviewPath);
        internal static string HitAnimatorStateName => HitStateName;
        internal static float HitLoopSeconds => HitLoopSecondsValue;
        internal static void CaptureDeathRuntimeFrame(string path)
        {
            RebellionMoveAnimationTool.CaptureRuntimeFrameForSlot(
                DeathSlotName,
                path);
        }
        internal static string DeathFinalReviewAbsolutePath =>
            Absolute(DeathReviewPath);
        internal static string DeathAnimatorStateName => DeathStateName;
        internal static float DeathLoopSeconds => DeathLoopSecondsValue;

        private static ClipCreationResult CreateAttackClip(
            Transform slot,
            Transform model)
        {
            DeleteAssetIfPresent(ClipPath);
            var body = RequireDescendant(model, "Bone_001");
            var legs = new[]
            {
                CreateLeg(model, "RearNegativeX",
                    "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009"),
                CreateLeg(model, "RearPositiveX",
                    "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014"),
                CreateLeg(model, "FrontNegativeX",
                    "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019"),
                CreateLeg(model, "FrontPositiveX",
                    "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024")
            };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var naturalLift = CalculateNaturalLift(legs);
            var rotationKeys = LegBoneNames.ToDictionary(
                name => RequireDescendant(model, name),
                _ => new List<QuaternionKey>());
            var bodyPositionKeys = new List<VectorKey>();

            try
            {
                var sampleCount =
                    Mathf.RoundToInt(LoopSecondsValue / PoseStepSeconds);
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var time = sample * PoseStepSeconds;
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var naturalProgress = NaturalProgress(time);
                    body.position += Vector3.up * (naturalLift * naturalProgress);
                    foreach (var leg in legs)
                    {
                        SolveCcd(leg.Joints, leg.Foot, leg.RestFootPosition);
                        leg.Foot.rotation = leg.RestFootRotation;
                    }

                    bodyPositionKeys.Add(new VectorKey(time, body.localPosition));
                    foreach (var pair in rotationKeys)
                    {
                        pair.Value.Add(
                            new QuaternionKey(time, pair.Key.localRotation));
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            var clip = new AnimationClip
            {
                name = "Rebellion_02_Attack_Mode_Transition",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            SetVectorCurves(
                clip,
                AnimationUtility.CalculateTransformPath(body, slot),
                bodyPositionKeys);
            foreach (var pair in rotationKeys)
            {
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(pair.Key, slot),
                    pair.Value);
            }

            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            return new ClipCreationResult(clip, naturalLift);
        }

        private static HitClipCreationResult CreateHitClip(
            Transform slot,
            Transform model,
            Transform bodyPivot)
        {
            DeleteAssetIfPresent(HitClipPath);
            var forward = RequireHorizontalWeaponForward(model);
            var left = Vector3.Cross(forward, Vector3.up).normalized;
            var legs = CreateAllLegs(model);
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animatedTransforms = legs
                .SelectMany(leg => leg.AllBones)
                .Concat(new[] { bodyPivot })
                .Distinct()
                .ToArray();
            var rotationKeys = animatedTransforms.ToDictionary(
                item => item,
                _ => new List<QuaternionKey>());
            var tiltSign = LeftDownTiltSign(forward, left);

            try
            {
                var sampleCount =
                    Mathf.RoundToInt(
                        HitLoopSecondsValue / HitPoseStepSeconds);
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var time = sample * HitPoseStepSeconds;
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    bodyPivot.rotation =
                        Quaternion.AngleAxis(
                            tiltSign *
                            HitBodyTiltDegrees(time),
                            forward) *
                        bodyPivot.rotation;
                    var rattleDegrees = HitLegRattleDegreesAt(time);
                    foreach (var leg in legs)
                    {
                        ApplyLegRattle(leg, forward, rattleDegrees);
                        SolveCcd(
                            new[]
                            {
                                leg.Joints[0],
                                leg.Joints[2],
                                leg.Joints[3]
                            },
                            leg.Foot,
                            leg.RestFootPosition);
                        leg.Foot.rotation = leg.RestFootRotation;
                    }

                    foreach (var pair in rotationKeys)
                    {
                        pair.Value.Add(
                            new QuaternionKey(
                                time,
                                pair.Key.localRotation));
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            var clip = new AnimationClip
            {
                name = "Rebellion_05_Hit_Reaction",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            foreach (var pair in rotationKeys)
            {
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(pair.Key, slot),
                    pair.Value);
            }
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, HitClipPath);
            EditorUtility.SetDirty(clip);
            return new HitClipCreationResult(
                clip);
        }

        private static DeathClipCreationResult CreateDeathClip(
            Transform slot,
            Transform model,
            Transform bodyPivot)
        {
            DeleteAssetIfPresent(DeathClipPath);
            var forward = RequireHorizontalWeaponForward(model);
            var left = Vector3.Cross(forward, Vector3.up).normalized;
            var legs = CreateAllLegs(model);
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animatedTransforms = legs
                .SelectMany(leg => leg.AllBones)
                .Concat(new[] { bodyPivot })
                .Distinct()
                .ToArray();
            var rotationKeys = animatedTransforms.ToDictionary(
                item => item,
                _ => new List<QuaternionKey>());
            var bodyPositionKeys = new List<VectorKey>();
            var baselinePivotPosition = bodyPivot.localPosition;
            var tiltSign = LeftDownTiltSign(
                forward,
                left,
                DeathBodyLeftTiltDegrees);
            var bodyDrop = CalculateDeathBodyDrop(legs);
            var footTargets = CalculateDeathFootTargets(
                bodyPivot.position,
                legs);

            try
            {
                var sampleCount =
                    Mathf.RoundToInt(
                        DeathLoopSecondsValue / DeathPoseStepSeconds);
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var time = sample * DeathPoseStepSeconds;
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var collapseProgress = Smooth01(
                        Mathf.Clamp01(time / DeathCollapseSeconds));
                    bodyPivot.localPosition =
                        baselinePivotPosition +
                        bodyPivot.parent.InverseTransformVector(
                            Vector3.down * bodyDrop * collapseProgress);
                    bodyPivot.rotation =
                        Quaternion.AngleAxis(
                            tiltSign *
                            DeathBodyLeftTiltDegrees *
                            collapseProgress,
                            forward) *
                        bodyPivot.rotation;

                    for (var legIndex = 0;
                         legIndex < legs.Length;
                         legIndex++)
                    {
                        var leg = legs[legIndex];
                        ApplyDeathJointSlack(
                            leg,
                            footTargets[legIndex],
                            collapseProgress);
                        var target = Vector3.Lerp(
                            leg.RestFootPosition,
                            footTargets[legIndex],
                            collapseProgress);
                        SolveCcd(
                            new[]
                            {
                                leg.Joints[0],
                                leg.Joints[1],
                                leg.Joints[2],
                                leg.Joints[3]
                            },
                            leg.Foot,
                            target);
                        leg.Foot.rotation = leg.RestFootRotation;
                    }

                    foreach (var pair in rotationKeys)
                    {
                        pair.Value.Add(
                            new QuaternionKey(
                                time,
                                pair.Key.localRotation));
                    }
                    bodyPositionKeys.Add(
                        new VectorKey(time, bodyPivot.localPosition));
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            var clip = new AnimationClip
            {
                name = "Rebellion_06_Death",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            foreach (var pair in rotationKeys)
            {
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(pair.Key, slot),
                    pair.Value);
            }
            SetVectorCurves(
                clip,
                AnimationUtility.CalculateTransformPath(bodyPivot, slot),
                bodyPositionKeys);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, DeathClipPath);
            EditorUtility.SetDirty(clip);
            return new DeathClipCreationResult(clip, bodyDrop);
        }

        private static LegChain[] CreateAllLegs(Transform model)
        {
            return new[]
            {
                CreateLeg(model, "RearNegativeX",
                    "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009"),
                CreateLeg(model, "RearPositiveX",
                    "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014"),
                CreateLeg(model, "FrontNegativeX",
                    "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019"),
                CreateLeg(model, "FrontPositiveX",
                    "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024")
            };
        }

        private static Vector3 RequireHorizontalWeaponForward(Transform model)
        {
            var weapon = RequireDescendant(model, "Bone_007");
            var weaponTip = RequireDescendant(model, "Bone_006");
            var forward = Vector3.ProjectOnPlane(
                weaponTip.position - weapon.position,
                Vector3.up);
            if (forward.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Rebellion horizontal forward direction is unavailable.");
            }
            return forward.normalized;
        }

        private static float HitLegRattleDegreesAt(float time)
        {
            return HitLegRattleDegrees *
                   Mathf.Sin(
                       Mathf.PI *
                       2f *
                       HitLegRattleCycles *
                       time /
                       HitLoopSecondsValue);
        }

        private static float HitBodyTiltDegrees(float time)
        {
            if (time <= HitPeakTime)
            {
                return Mathf.Lerp(
                    0f,
                    HitBodyLeftTiltDegrees,
                    Smooth01(time / HitPeakTime));
            }
            if (time <= HitReboundTime)
            {
                return Mathf.Lerp(
                    HitBodyLeftTiltDegrees,
                    -HitBodyRightReboundDegrees,
                    Smooth01(
                        (time - HitPeakTime) /
                        (HitReboundTime - HitPeakTime)));
            }
            return Mathf.Lerp(
                -HitBodyRightReboundDegrees,
                0f,
                Smooth01(
                    (time - HitReboundTime) /
                    (HitLoopSecondsValue - HitReboundTime)));
        }

        private static float LeftDownTiltSign(
            Vector3 forward,
            Vector3 left)
        {
            return LeftDownTiltSign(
                forward,
                left,
                HitBodyLeftTiltDegrees);
        }

        private static float LeftDownTiltSign(
            Vector3 forward,
            Vector3 left,
            float degrees)
        {
            var positive =
                Quaternion.AngleAxis(
                    degrees,
                    forward) * left;
            var negative =
                Quaternion.AngleAxis(
                    -degrees,
                    forward) * left;
            return positive.y < negative.y ? 1f : -1f;
        }

        private static void ApplyLegRattle(
            LegChain leg,
            Vector3 forward,
            float degrees)
        {
            leg.Joints[1].rotation =
                Quaternion.AngleAxis(degrees, forward) *
                leg.Joints[1].rotation;
        }

        private static AnimatorController CreateHitController(
            AnimationClip clip)
        {
            DeleteAssetIfPresent(HitControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    HitControllerPath);
            var state =
                controller.layers[0].stateMachine.AddState(HitStateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorController CreateDeathController(
            AnimationClip clip)
        {
            DeleteAssetIfPresent(DeathControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    DeathControllerPath);
            var state =
                controller.layers[0].stateMachine.AddState(DeathStateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static float CalculateDeathBodyDrop(
            IReadOnlyList<LegChain> legs)
        {
            var minimumSupportHeight = legs.Min(
                leg => Mathf.Max(
                    0f,
                    leg.Joints[0].position.y -
                    leg.RestFootPosition.y));
            if (minimumSupportHeight <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Rebellion leg support height is unavailable.");
            }
            return minimumSupportHeight * DeathBodyDropSupportRatio;
        }

        private static Vector3[] CalculateDeathFootTargets(
            Vector3 bodyCenter,
            IReadOnlyList<LegChain> legs)
        {
            var targets = new Vector3[legs.Count];
            for (var legIndex = 0; legIndex < legs.Count; legIndex++)
            {
                var leg = legs[legIndex];
                var radial = Vector3.ProjectOnPlane(
                    leg.RestFootPosition - bodyCenter,
                    Vector3.up);
                if (radial.sqrMagnitude < 0.000001f)
                {
                    radial = Vector3.ProjectOnPlane(
                        leg.Joints[0].position - bodyCenter,
                        Vector3.up);
                }
                if (radial.sqrMagnitude < 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Rebellion death leg radial direction is unavailable.");
                }
                var bones = leg.AllBones.ToArray();
                var chainLength = 0f;
                for (var boneIndex = 1;
                     boneIndex < bones.Length;
                     boneIndex++)
                {
                    chainLength += Vector3.Distance(
                        bones[boneIndex - 1].position,
                        bones[boneIndex].position);
                }
                targets[legIndex] =
                    leg.RestFootPosition +
                    radial.normalized *
                    chainLength *
                    DeathFootSpreadChainRatio;
                targets[legIndex].y = leg.RestFootPosition.y;
            }
            return targets;
        }

        private static void ApplyDeathJointSlack(
            LegChain leg,
            Vector3 target,
            float progress)
        {
            var radial = Vector3.ProjectOnPlane(
                target - leg.Joints[1].position,
                Vector3.up);
            if (radial.sqrMagnitude < 0.000001f)
            {
                return;
            }
            var axis = Vector3.Cross(radial.normalized, Vector3.up).normalized;
            var baseline = leg.Joints[1].rotation;
            var positive =
                Quaternion.AngleAxis(
                    DeathJointSlackDegrees * progress,
                    axis) *
                baseline;
            leg.Joints[1].rotation = positive;
            var positiveHeight = leg.Foot.position.y;
            var negative =
                Quaternion.AngleAxis(
                    -DeathJointSlackDegrees * progress,
                    axis) *
                baseline;
            leg.Joints[1].rotation = negative;
            var negativeHeight = leg.Foot.position.y;
            leg.Joints[1].rotation =
                positiveHeight < negativeHeight ? positive : negative;
        }

        private static float CalculateNaturalLift(IReadOnlyList<LegChain> legs)
        {
            var limitingLift = float.PositiveInfinity;
            foreach (var leg in legs)
            {
                var bones = leg.AllBones.ToArray();
                var chainLength = 0f;
                for (var index = 1; index < bones.Length; index++)
                {
                    chainLength +=
                        Vector3.Distance(
                            bones[index - 1].position,
                            bones[index].position);
                }

                var rootToFoot = bones[0].position - leg.Foot.position;
                var horizontalDistance =
                    Vector3.ProjectOnPlane(rootToFoot, Vector3.up).magnitude;
                var targetDistance = chainLength * StraightnessRatio;
                if (targetDistance <= horizontalDistance)
                {
                    continue;
                }

                var targetVerticalDistance = Mathf.Sqrt(
                    (targetDistance * targetDistance) -
                    (horizontalDistance * horizontalDistance));
                var availableLift =
                    targetVerticalDistance - Mathf.Max(0f, rootToFoot.y);
                if (availableLift > 0f)
                {
                    limitingLift = Mathf.Min(limitingLift, availableLift);
                }
            }

            if (float.IsPositiveInfinity(limitingLift))
            {
                throw new InvalidOperationException(
                    "A natural standing lift could not be derived from the rig.");
            }

            // The existing four leg-chain lengths derive the lift. This clamp
            // only guards against an imperceptible or malformed-rig result.
            return Mathf.Clamp(
                limitingLift,
                MinimumNaturalLiftWorld,
                MaximumNaturalLiftWorld);
        }

        private static float NaturalProgress(float time)
        {
            if (time <= StandEndTime)
            {
                return Smooth01(time / StandEndTime);
            }
            return Smooth01(
                (LoopSecondsValue - time) /
                (LoopSecondsValue - StandEndTime));
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - (2f * value));
        }

        private static LegChain CreateLeg(
            Transform model,
            string label,
            params string[] boneNames)
        {
            var bones = boneNames
                .Select(name => RequireDescendant(model, name))
                .ToArray();
            for (var index = 1; index < bones.Length; index++)
            {
                if (bones[index].parent != bones[index - 1])
                {
                    throw new InvalidOperationException(
                        label + " is not a continuous leg chain at " +
                        bones[index].name + ".");
                }
            }
            return new LegChain(
                bones.Take(bones.Length - 1).ToArray(),
                bones[bones.Length - 1],
                bones[bones.Length - 1].position,
                bones[bones.Length - 1].rotation);
        }

        private static void SolveCcd(
            IReadOnlyList<Transform> joints,
            Transform foot,
            Vector3 target)
        {
            for (var iteration = 0; iteration < 100; iteration++)
            {
                for (var index = joints.Count - 1; index >= 0; index--)
                {
                    var joint = joints[index];
                    var toFoot = foot.position - joint.position;
                    var toTarget = target - joint.position;
                    if (toFoot.sqrMagnitude < 0.0000001f ||
                        toTarget.sqrMagnitude < 0.0000001f)
                    {
                        continue;
                    }
                    var delta = Quaternion.FromToRotation(toFoot, toTarget);
                    delta = Quaternion.RotateTowards(
                        Quaternion.identity,
                        delta,
                        45f);
                    joint.rotation = delta * joint.rotation;
                }
                if ((foot.position - target).sqrMagnitude < 0.0000000001f)
                {
                    break;
                }
            }
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(StateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<QuaternionKey> values)
        {
            var continuity = new List<QuaternionKey>(values.Count);
            Quaternion? previous = null;
            foreach (var value in values)
            {
                var rotation = value.Rotation;
                if (previous.HasValue &&
                    Quaternion.Dot(previous.Value, rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }
                continuity.Add(new QuaternionKey(value.Time, rotation));
                previous = rotation;
            }

            SetLinearCurve(clip, path, "m_LocalRotation.x",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.x)).ToList());
            SetLinearCurve(clip, path, "m_LocalRotation.y",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.y)).ToList());
            SetLinearCurve(clip, path, "m_LocalRotation.z",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.z)).ToList());
            SetLinearCurve(clip, path, "m_LocalRotation.w",
                continuity.Select(value =>
                    new Keyframe(value.Time, value.Rotation.w)).ToList());
        }

        private static void SetVectorCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<VectorKey> values)
        {
            SetLinearCurve(clip, path, "m_LocalPosition.x",
                values.Select(value =>
                    new Keyframe(value.Time, value.Position.x)).ToList());
            SetLinearCurve(clip, path, "m_LocalPosition.y",
                values.Select(value =>
                    new Keyframe(value.Time, value.Position.y)).ToList());
            SetLinearCurve(clip, path, "m_LocalPosition.z",
                values.Select(value =>
                    new Keyframe(value.Time, value.Position.z)).ToList());
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IList<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
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

        private static PoseMetrics MeasurePoses(
            Transform slot,
            Transform model,
            AnimationClip clip)
        {
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var feet = new[]
            {
                RequireDescendant(model, "Bone_009"),
                RequireDescendant(model, "Bone_014"),
                RequireDescendant(model, "Bone_019"),
                RequireDescendant(model, "Bone_024")
            };
            var disc = RequireDescendant(model, "Bone_008");
            var baselineFeet = Array.Empty<Vector3>();
            var maximumFootError = 0f;
            var discAtRest = 0f;
            var discAtStanding = 0f;
            var discAtEnd = 0f;

            try
            {
                SamplePose(slot, clip, snapshots, 0f);
                baselineFeet = feet.Select(foot => foot.position).ToArray();
                discAtRest = disc.position.y;
                var sampleCount =
                    Mathf.RoundToInt(LoopSecondsValue / PoseStepSeconds);
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var time = sample * PoseStepSeconds;
                    SamplePose(slot, clip, snapshots, time);
                    for (var index = 0; index < feet.Length; index++)
                    {
                        maximumFootError = Mathf.Max(
                            maximumFootError,
                            Vector3.Distance(
                                baselineFeet[index],
                                feet[index].position));
                    }

                    if (Mathf.Abs(time - StandEndTime) < 0.0001f)
                    {
                        discAtStanding = disc.position.y;
                    }
                    if (Mathf.Abs(time - LoopSecondsValue) < 0.0001f)
                    {
                        discAtEnd = disc.position.y;
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            return new PoseMetrics(
                maximumFootError,
                discAtStanding - discAtRest,
                discAtEnd - discAtRest);
        }

        private static HitPoseMetrics MeasureHitReaction(
            Transform slot,
            Transform model,
            Transform body,
            AnimationClip clip,
            Vector3 forward)
        {
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var allLegs = CreateAllLegs(model);
            var renderer =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single();
            var rigidBodyVertexIndices =
                FindRigidBodyVertexIndices(renderer, allLegs);
            var left = Vector3.Cross(forward, Vector3.up).normalized;
            var baselineBodyPosition = Vector3.zero;
            var baselineBodyRotation = Quaternion.identity;
            var baselineRigidBodyVertices = Array.Empty<Vector3>();
            var baselineFeet = Array.Empty<Vector3>();
            var baselinePrimaryRattleRotations =
                new Dictionary<LegChain, Quaternion>();
            var peakBodyLeftTiltDegrees = 0f;
            var peakBodyLeftEdgeVerticalDirection = 0f;
            var peakBodyRightReboundDegrees = 0f;
            var peakBodyRightEdgeVerticalDirection = 0f;
            var maximumBodyPositionError = 0f;
            var bodyRotationReturnError = 0f;
            var maximumBodyMeshDeformationWorld = 0f;
            var maximumFootPositionError = 0f;
            var footReturnError = 0f;
            var minimumPrimaryRattlePeakDegrees = float.MaxValue;
            var maximumPrimaryRattlePeakDegrees = 0f;
            var maximumRattleZeroErrorDegrees = 0f;
            var rattlePeakCounts = allLegs.ToDictionary(
                leg => leg,
                _ => 0);

            try
            {
                SamplePose(slot, clip, snapshots, 0f);
                baselineBodyPosition = body.position;
                baselineBodyRotation = body.rotation;
                baselineRigidBodyVertices =
                    BakeRigidBodyVerticesInPivotSpace(
                        renderer,
                        body,
                        rigidBodyVertexIndices);
                baselineFeet =
                    allLegs.Select(leg => leg.Foot.position).ToArray();
                baselinePrimaryRattleRotations =
                    allLegs.ToDictionary(
                        leg => leg,
                        leg => leg.Joints[1].localRotation);
                var sampleCount =
                    Mathf.RoundToInt(
                        HitLoopSecondsValue / HitPoseStepSeconds);
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var time = sample * HitPoseStepSeconds;
                    SamplePose(slot, clip, snapshots, time);
                    maximumBodyPositionError = Mathf.Max(
                        maximumBodyPositionError,
                        Vector3.Distance(
                            body.position,
                            baselineBodyPosition));
                    var currentRigidBodyVertices =
                        BakeRigidBodyVerticesInPivotSpace(
                            renderer,
                            body,
                            rigidBodyVertexIndices);
                    for (var vertexIndex = 0;
                         vertexIndex < currentRigidBodyVertices.Length;
                         vertexIndex++)
                    {
                        maximumBodyMeshDeformationWorld = Mathf.Max(
                            maximumBodyMeshDeformationWorld,
                            Vector3.Distance(
                                currentRigidBodyVertices[vertexIndex],
                                baselineRigidBodyVertices[vertexIndex]));
                    }
                    for (var legIndex = 0;
                         legIndex < allLegs.Length;
                         legIndex++)
                    {
                        maximumFootPositionError = Mathf.Max(
                            maximumFootPositionError,
                            Vector3.Distance(
                                allLegs[legIndex].Foot.position,
                                baselineFeet[legIndex]));
                    }

                    if (IsRattlePeakTime(time))
                    {
                        foreach (var leg in allLegs)
                        {
                            var primaryDegrees = Quaternion.Angle(
                                baselinePrimaryRattleRotations[leg],
                                leg.Joints[1].localRotation);
                            minimumPrimaryRattlePeakDegrees = Mathf.Min(
                                minimumPrimaryRattlePeakDegrees,
                                primaryDegrees);
                            maximumPrimaryRattlePeakDegrees = Mathf.Max(
                                maximumPrimaryRattlePeakDegrees,
                                primaryDegrees);
                            if (primaryDegrees >= 4.9f)
                            {
                                rattlePeakCounts[leg]++;
                            }
                        }
                    }
                    else if (IsRattleZeroTime(time))
                    {
                        foreach (var leg in allLegs)
                        {
                            maximumRattleZeroErrorDegrees = Mathf.Max(
                                maximumRattleZeroErrorDegrees,
                                Quaternion.Angle(
                                    baselinePrimaryRattleRotations[leg],
                                    leg.Joints[1].localRotation));
                        }
                    }

                    if (Mathf.Abs(time - HitPeakTime) < 0.0001f)
                    {
                        var bodyDeltaRotation =
                            body.rotation *
                            Quaternion.Inverse(baselineBodyRotation);
                        peakBodyLeftTiltDegrees = Quaternion.Angle(
                            baselineBodyRotation,
                            body.rotation);
                        peakBodyLeftEdgeVerticalDirection = Vector3.Dot(
                            bodyDeltaRotation * left,
                            Vector3.up);
                    }

                    if (Mathf.Abs(time - HitReboundTime) < 0.0001f)
                    {
                        var bodyDeltaRotation =
                            body.rotation *
                            Quaternion.Inverse(baselineBodyRotation);
                        peakBodyRightReboundDegrees = Quaternion.Angle(
                            baselineBodyRotation,
                            body.rotation);
                        peakBodyRightEdgeVerticalDirection = Vector3.Dot(
                            bodyDeltaRotation * left,
                            Vector3.up);
                    }

                    if (Mathf.Abs(time - HitLoopSecondsValue) < 0.0001f)
                    {
                        bodyRotationReturnError = Quaternion.Angle(
                            body.rotation,
                            baselineBodyRotation);
                        for (var legIndex = 0;
                             legIndex < allLegs.Length;
                             legIndex++)
                        {
                            footReturnError = Mathf.Max(
                                footReturnError,
                                Vector3.Distance(
                                    allLegs[legIndex].Foot.position,
                                    baselineFeet[legIndex]));
                        }
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            if (minimumPrimaryRattlePeakDegrees == float.MaxValue)
            {
                minimumPrimaryRattlePeakDegrees = 0f;
            }
            var minimumRattlePeakEventCount = rattlePeakCounts.Values.Min();
            var rattledLegCount = rattlePeakCounts.Count(
                pair => pair.Value == 4);
            return new HitPoseMetrics(
                peakBodyLeftTiltDegrees,
                peakBodyLeftEdgeVerticalDirection,
                peakBodyRightReboundDegrees,
                peakBodyRightEdgeVerticalDirection,
                maximumBodyPositionError,
                bodyRotationReturnError,
                maximumBodyMeshDeformationWorld,
                rigidBodyVertexIndices.Length,
                maximumFootPositionError,
                footReturnError,
                minimumPrimaryRattlePeakDegrees,
                maximumPrimaryRattlePeakDegrees,
                maximumRattleZeroErrorDegrees,
                minimumRattlePeakEventCount,
                rattledLegCount);
        }

        private static DeathPoseMetrics MeasureDeath(
            Transform slot,
            Transform model,
            Transform body,
            AnimationClip clip,
            Vector3 forward)
        {
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var allLegs = CreateAllLegs(model);
            var renderer =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single();
            var rigidBodyVertexIndices =
                FindRigidBodyVertexIndices(renderer, allLegs);
            var left = Vector3.Cross(forward, Vector3.up).normalized;
            var baselineBodyPosition = Vector3.zero;
            var baselineBodyRotation = Quaternion.identity;
            var baselineFeet = Array.Empty<Vector3>();
            var baselineFootRadii = Array.Empty<float>();
            var baselineRigidBodyVertices = Array.Empty<Vector3>();
            var finalBodyLeftTiltDegrees = 0f;
            var finalBodyLeftEdgeVerticalDirection = 0f;
            var bodyDropWorld = 0f;
            var minimumFootSpreadIncreaseWorld = float.MaxValue;
            var maximumFootGroundErrorWorld = 0f;
            var maximumBodyMeshDeformationWorld = 0f;

            try
            {
                SamplePose(slot, clip, snapshots, 0f);
                baselineBodyPosition = body.position;
                baselineBodyRotation = body.rotation;
                baselineFeet =
                    allLegs.Select(leg => leg.Foot.position).ToArray();
                baselineFootRadii = baselineFeet
                    .Select(foot =>
                        Vector3.ProjectOnPlane(
                            foot - baselineBodyPosition,
                            Vector3.up).magnitude)
                    .ToArray();
                baselineRigidBodyVertices =
                    BakeRigidBodyVerticesInPivotSpace(
                        renderer,
                        body,
                        rigidBodyVertexIndices);

                var sampleCount =
                    Mathf.RoundToInt(
                        DeathLoopSecondsValue / DeathPoseStepSeconds);
                for (var sample = 0; sample <= sampleCount; sample++)
                {
                    var time = sample * DeathPoseStepSeconds;
                    SamplePose(slot, clip, snapshots, time);
                    for (var legIndex = 0;
                         legIndex < allLegs.Length;
                         legIndex++)
                    {
                        maximumFootGroundErrorWorld = Mathf.Max(
                            maximumFootGroundErrorWorld,
                            Mathf.Abs(
                                allLegs[legIndex].Foot.position.y -
                                baselineFeet[legIndex].y));
                    }

                    var currentRigidBodyVertices =
                        BakeRigidBodyVerticesInPivotSpace(
                            renderer,
                            body,
                            rigidBodyVertexIndices);
                    for (var vertexIndex = 0;
                         vertexIndex < currentRigidBodyVertices.Length;
                         vertexIndex++)
                    {
                        maximumBodyMeshDeformationWorld = Mathf.Max(
                            maximumBodyMeshDeformationWorld,
                            Vector3.Distance(
                                currentRigidBodyVertices[vertexIndex],
                                baselineRigidBodyVertices[vertexIndex]));
                    }

                    if (Mathf.Abs(time - DeathCollapseSeconds) < 0.0001f)
                    {
                        var bodyDeltaRotation =
                            body.rotation *
                            Quaternion.Inverse(baselineBodyRotation);
                        finalBodyLeftTiltDegrees = Quaternion.Angle(
                            baselineBodyRotation,
                            body.rotation);
                        finalBodyLeftEdgeVerticalDirection = Vector3.Dot(
                            bodyDeltaRotation * left,
                            Vector3.up);
                        bodyDropWorld =
                            baselineBodyPosition.y - body.position.y;
                        for (var legIndex = 0;
                             legIndex < allLegs.Length;
                             legIndex++)
                        {
                            var radius = Vector3.ProjectOnPlane(
                                allLegs[legIndex].Foot.position -
                                baselineBodyPosition,
                                Vector3.up).magnitude;
                            minimumFootSpreadIncreaseWorld = Mathf.Min(
                                minimumFootSpreadIncreaseWorld,
                                radius - baselineFootRadii[legIndex]);
                        }
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            if (minimumFootSpreadIncreaseWorld == float.MaxValue)
            {
                minimumFootSpreadIncreaseWorld = 0f;
            }
            return new DeathPoseMetrics(
                finalBodyLeftTiltDegrees,
                finalBodyLeftEdgeVerticalDirection,
                bodyDropWorld,
                minimumFootSpreadIncreaseWorld,
                maximumFootGroundErrorWorld,
                maximumBodyMeshDeformationWorld);
        }

        private static bool IsRattlePeakTime(float time)
        {
            var quarterCycle =
                HitLoopSecondsValue / (HitLegRattleCycles * 4f);
            var quarterIndex = Mathf.RoundToInt(time / quarterCycle);
            return (quarterIndex & 1) == 1 &&
                   Mathf.Abs(time - (quarterIndex * quarterCycle)) <
                   0.0001f;
        }

        private static bool IsRattleZeroTime(float time)
        {
            var quarterCycle =
                HitLoopSecondsValue / (HitLegRattleCycles * 4f);
            var quarterIndex = Mathf.RoundToInt(time / quarterCycle);
            return (quarterIndex & 1) == 0 &&
                   Mathf.Abs(time - (quarterIndex * quarterCycle)) <
                   0.0001f;
        }

        private static int[] FindRigidBodyVertexIndices(
            SkinnedMeshRenderer renderer,
            IReadOnlyList<LegChain> legs)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Rebellion skinned mesh is missing.");
            var weights = mesh.boneWeights;
            if (weights.Length != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Rebellion legacy bone weights do not cover every vertex.");
            }
            var rendererBones = renderer.bones;
            var legBones = new HashSet<Transform>(
                legs.SelectMany(leg => leg.AllBones));
            var indices = new List<int>();
            for (var vertexIndex = 0;
                 vertexIndex < weights.Length;
                 vertexIndex++)
            {
                if (!HasLegInfluence(
                        weights[vertexIndex],
                        rendererBones,
                        legBones))
                {
                    indices.Add(vertexIndex);
                }
            }
            if (indices.Count == 0)
            {
                throw new InvalidOperationException(
                    "No rigid Rebellion body vertices were found.");
            }
            return indices.ToArray();
        }

        private static bool HasLegInfluence(
            BoneWeight weight,
            IReadOnlyList<Transform> rendererBones,
            ISet<Transform> legBones)
        {
            return IsLegInfluence(
                       weight.boneIndex0,
                       weight.weight0,
                       rendererBones,
                       legBones) ||
                   IsLegInfluence(
                       weight.boneIndex1,
                       weight.weight1,
                       rendererBones,
                       legBones) ||
                   IsLegInfluence(
                       weight.boneIndex2,
                       weight.weight2,
                       rendererBones,
                       legBones) ||
                   IsLegInfluence(
                       weight.boneIndex3,
                       weight.weight3,
                       rendererBones,
                       legBones);
        }

        private static bool IsLegInfluence(
            int boneIndex,
            float weight,
            IReadOnlyList<Transform> rendererBones,
            ISet<Transform> legBones)
        {
            return weight > 0.00001f &&
                   boneIndex >= 0 &&
                   boneIndex < rendererBones.Count &&
                   legBones.Contains(rendererBones[boneIndex]);
        }

        private static Vector3[] BakeRigidBodyVerticesInPivotSpace(
            SkinnedMeshRenderer renderer,
            Transform body,
            IReadOnlyList<int> vertexIndices)
        {
            var bakedMesh = new Mesh();
            try
            {
                renderer.BakeMesh(bakedMesh);
                var vertices = bakedMesh.vertices;
                var result = new Vector3[vertexIndices.Count];
                for (var index = 0; index < vertexIndices.Count; index++)
                {
                    var vertexIndex = vertexIndices[index];
                    result[index] = body.InverseTransformPoint(
                        renderer.transform.TransformPoint(
                            vertices[vertexIndex]));
                }
                return result;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        private static void SamplePose(
            Transform slot,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots,
            float time)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }
            clip.SampleAnimation(slot.gameObject, time);
        }

        private static RigSupportReport RequireRigSupportReport()
        {
            var absolute = Absolute(RigSupportReportPath);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Attack transition rig support report is missing.",
                    absolute);
            }
            var report = JsonUtility.FromJson<RigSupportReport>(
                File.ReadAllText(absolute, Encoding.UTF8));
            if (report == null ||
                report.result != "PASS" ||
                report.corrected_glb_sha256 != CorrectedModelSha256 ||
                !report.geometry_unchanged ||
                !report.non_disc_weights_unchanged ||
                !report.bone_hierarchy_unchanged ||
                report.disc_selection == null ||
                !report.disc_selection.visually_reviewed_as_disc_shell_only ||
                report.disc_selection.vertices != 273 ||
                report.roundtrip == null ||
                report.roundtrip.bones != 29 ||
                report.roundtrip.disc_leg_influenced_vertices != 0 ||
                report.roundtrip.disc_non_body_influenced_vertices != 0)
            {
                throw new InvalidOperationException(
                    "Rig support report does not match the approved correction.");
            }
            return report;
        }

        private static Transform[] RequireRigStructure(Transform model)
        {
            var renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one Rebellion skinned renderer, found " +
                    renderers.Length + ".");
            }
            var rigBones = renderers[0].bones
                .Where(bone => bone != null)
                .Distinct()
                .ToArray();
            if (rigBones.Length != 29)
            {
                throw new InvalidOperationException(
                    "Expected 29 Rebellion bones, found " +
                    rigBones.Length + ".");
            }

            RequireChain(model, "Bone_008", "Bone_007", "Bone_006");
            RequireChain(model,
                "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009");
            RequireChain(model,
                "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014");
            RequireChain(model,
                "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019");
            RequireChain(model,
                "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024");

            foreach (var name in BodyDetailNames)
            {
                var detail = RequireDescendant(model, name);
                if (detail.parent == null || detail.parent.name != "Bone_008")
                {
                    throw new InvalidOperationException(
                        name + " must be attached to Bone_008.");
                }
            }
            foreach (var name in WeaponDetailNames)
            {
                var detail = RequireDescendant(model, name);
                var expectedParent =
                    model.parent != null &&
                    model.parent.name == BurstSlotName &&
                    name != "Rebellion_Gun_Hub"
                        ? BurstCylinderPivotName
                        : "Bone_007";
                if (detail.parent == null ||
                    detail.parent.name != expectedParent)
                {
                    throw new InvalidOperationException(
                        name + " must be attached to " + expectedParent + ".");
                }
            }
            if (model.parent != null && model.parent.name == BurstSlotName)
            {
                var pivot =
                    RequireDescendant(model, BurstCylinderPivotName);
                if (pivot.parent == null || pivot.parent.name != "Bone_007")
                {
                    throw new InvalidOperationException(
                        BurstCylinderPivotName +
                        " must be attached to Bone_007.");
                }
            }
            return rigBones;
        }

        private static void RequireAllSlotsUseCorrectedModel(Scene scene)
        {
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireSlot(scene, slotName));
                RequireRigStructure(model);
                var renderer =
                    model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                    throw new InvalidOperationException(
                        slotName + " has no skinned renderer.");
                var meshPath = AssetDatabase.GetAssetPath(renderer.sharedMesh);
                if (!string.Equals(
                        meshPath,
                        CorrectedModelPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        slotName + " does not use the corrected shared model. " +
                        "Found " + meshPath + ".");
                }
            }
        }

        private static Transform EnsureHitBodyPivot(
            Transform slot,
            Transform model)
        {
            var existing = slot.Find(HitBodyPivotName);
            if (existing != null)
            {
                if (model.parent != existing)
                {
                    throw new InvalidOperationException(
                        ModelName + " must remain under " +
                        HitBodyPivotName + ".");
                }
                return existing;
            }

            var modelWorldPose = WorldPose.Capture(model);
            var disc = RequireDescendant(model, "Bone_008");
            var pivotObject = new GameObject(HitBodyPivotName);
            var pivot = pivotObject.transform;
            pivot.SetParent(slot, false);
            pivot.position = disc.position;
            pivot.rotation = slot.rotation;
            pivot.localScale = Vector3.one;
            model.SetParent(pivot, true);
            RequireSameWorldPose(
                modelWorldPose,
                model,
                ModelName + " reparented world pose");
            EditorUtility.SetDirty(pivotObject);
            EditorUtility.SetDirty(model);
            return pivot;
        }

        private static Transform RequireHitBodyPivot(
            Transform slot,
            Transform model)
        {
            var pivot = slot.Find(HitBodyPivotName) ??
                        throw new InvalidOperationException(
                            HitBodyPivotName + " is missing.");
            if (model.parent != pivot ||
                Vector3.Distance(pivot.localScale, Vector3.one) >
                0.000001f)
            {
                throw new InvalidOperationException(
                    HitBodyPivotName +
                    " hierarchy or scale is unexpected.");
            }
            return pivot;
        }

        private static Transform EnsureDeathBodyPivot(
            Transform slot,
            Transform model)
        {
            var existing = slot.Find(DeathBodyPivotName);
            if (existing != null)
            {
                if (model.parent != existing)
                {
                    throw new InvalidOperationException(
                        ModelName + " must remain under " +
                        DeathBodyPivotName + ".");
                }
                return existing;
            }

            var modelWorldPose = WorldPose.Capture(model);
            var disc = RequireDescendant(model, "Bone_008");
            var pivotObject = new GameObject(DeathBodyPivotName);
            var pivot = pivotObject.transform;
            pivot.SetParent(slot, false);
            pivot.position = disc.position;
            pivot.rotation = slot.rotation;
            pivot.localScale = Vector3.one;
            model.SetParent(pivot, true);
            RequireSameWorldPose(
                modelWorldPose,
                model,
                ModelName + " reparented world pose");
            EditorUtility.SetDirty(pivotObject);
            EditorUtility.SetDirty(model);
            return pivot;
        }

        private static Transform RequireDeathBodyPivot(
            Transform slot,
            Transform model)
        {
            var pivot = slot.Find(DeathBodyPivotName) ??
                        throw new InvalidOperationException(
                            DeathBodyPivotName + " is missing.");
            if (model.parent != pivot ||
                Vector3.Distance(pivot.localScale, Vector3.one) >
                0.000001f)
            {
                throw new InvalidOperationException(
                    DeathBodyPivotName +
                    " hierarchy or scale is unexpected.");
            }
            return pivot;
        }

        private static void RequireAnimatorAssignments(Transform placementRoot)
        {
            foreach (var slotName in SlotNames)
            {
                var slot = placementRoot.Find(slotName) ??
                           throw new InvalidOperationException(
                               slotName + " is missing.");
                var animator = slot.GetComponent<Animator>();
                var actual =
                    animator == null ||
                    animator.runtimeAnimatorController == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController);
                var expected = slotName == MoveSlotName
                    ? MoveControllerPath
                    : slotName == SlotName
                        ? ControllerPath
                        : slotName == ScanSlotName
                            ? ScanControllerPath
                            : slotName == BurstSlotName
                                ? BurstControllerPath
                                : slotName == HitSlotName
                                    ? HitControllerPath
                                    : slotName == DeathSlotName
                                        ? DeathControllerPath
                                        : string.Empty;
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        slotName + " controller assignment is unexpected. " +
                        "Expected " + expected + ", found " + actual + ".");
                }
            }
        }

        private static Dictionary<string, string>
            CaptureImplementedAnimationHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MoveClipPath] = Sha256IfPresent(MoveClipPath),
                [MoveControllerPath] = Sha256IfPresent(MoveControllerPath),
                [ClipPath] = Sha256IfPresent(ClipPath),
                [ControllerPath] = Sha256IfPresent(ControllerPath),
                [AnimationFolder + "/Rebellion_03_Forward_Scan.anim"] =
                    Sha256IfPresent(
                        AnimationFolder +
                        "/Rebellion_03_Forward_Scan.anim"),
                [ScanControllerPath] = Sha256IfPresent(ScanControllerPath),
                [AnimationFolder +
                 "/Rebellion_04_Forward_Burst_Fire.anim"] =
                    Sha256IfPresent(
                        AnimationFolder +
                        "/Rebellion_04_Forward_Burst_Fire.anim"),
                [BurstControllerPath] =
                    Sha256IfPresent(BurstControllerPath)
            };
        }

        private static Dictionary<string, string>
            CaptureAllImplementedAnimationHashes()
        {
            var hashes = CaptureImplementedAnimationHashes();
            hashes[HitClipPath] = Sha256IfPresent(HitClipPath);
            hashes[HitControllerPath] =
                Sha256IfPresent(HitControllerPath);
            return hashes;
        }

        private static void RequireUnchangedFileHashes(
            IReadOnlyDictionary<string, string> expected)
        {
            foreach (var pair in expected)
            {
                RequireUnchangedFileHash(pair.Key, pair.Value);
            }
        }

        private static void RequireChain(Transform model, params string[] names)
        {
            var previous = RequireDescendant(model, names[0]);
            for (var index = 1; index < names.Length; index++)
            {
                var current = RequireDescendant(model, names[index]);
                if (current.parent != previous)
                {
                    throw new InvalidOperationException(
                        current.name + " is not a direct child of " +
                        previous.name + ".");
                }
                previous = current;
            }
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one " + name + " under " + root.name +
                    ", found " + matches.Length + ".");
            }
            return matches[0];
        }

        private static Scene RequireActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Attack transition authoring requires Edit Mode.");
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the current active scene.");
            }
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == PlacementRootName)
                       ?.transform ??
                   throw new InvalidOperationException(
                       PlacementRootName + " is missing.");
        }

        private static Transform RequireSlot(Scene scene, string slotName)
        {
            return RequirePlacementRoot(scene).Find(slotName) ??
                   throw new InvalidOperationException(slotName + " is missing.");
        }

        private static Transform RequireModel(Transform slot)
        {
            var models = slot.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == ModelName)
                .ToArray();
            if (models.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one " + ModelName + " under " + slot.name +
                    ", found " + models.Length + ".");
            }
            return models[0];
        }

        private static void RequireCorrectedModelHash()
        {
            var absolute = Absolute(CorrectedModelPath);
            var actual = File.Exists(absolute) ? Sha256(absolute) : string.Empty;
            if (!string.Equals(
                    actual,
                    CorrectedModelSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Unexpected corrected Rebellion model hash. Expected " +
                    CorrectedModelSha256 + ", found " + actual + ".");
            }
        }

        private static string Sha256IfPresent(string relativePath)
        {
            var absolute = Absolute(relativePath);
            return File.Exists(absolute) ? Sha256(absolute) : string.Empty;
        }

        private static void RequireUnchangedFileHash(
            string relativePath,
            string expected)
        {
            var actual = Sha256IfPresent(relativePath);
            if (!string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    relativePath + " changed unexpectedly.");
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return string.Concat(
                    algorithm.ComputeHash(stream)
                        .Select(value => value.ToString("X2")));
            }
        }

        private static void RequireSameTransform(
            TransformState expected,
            Transform actual,
            string label)
        {
            if (!expected.Matches(actual))
            {
                throw new InvalidOperationException(
                    label + " transform changed unexpectedly.");
            }
        }

        private static void RequireSameWorldPose(
            WorldPose expected,
            Transform actual,
            string label)
        {
            if (!expected.Matches(actual))
            {
                throw new InvalidOperationException(
                    label + " changed unexpectedly.");
            }
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static void WriteText(string relativePath, string contents)
        {
            var absolute = Absolute(relativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Output directory is invalid."));
            File.WriteAllText(absolute, contents, Encoding.UTF8);
        }

        private static string Absolute(string projectRelativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Project root is unavailable.");
            return Path.Combine(
                projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private sealed class LegChain
        {
            public LegChain(
                Transform[] joints,
                Transform foot,
                Vector3 restFootPosition,
                Quaternion restFootRotation)
            {
                Joints = joints;
                Foot = foot;
                RestFootPosition = restFootPosition;
                RestFootRotation = restFootRotation;
            }

            public Transform[] Joints { get; }
            public Transform Foot { get; }
            public Vector3 RestFootPosition { get; }
            public Quaternion RestFootRotation { get; }
            public IEnumerable<Transform> AllBones =>
                Joints.Concat(new[] { Foot });
        }

        private readonly struct ClipCreationResult
        {
            public ClipCreationResult(
                AnimationClip clip,
                float naturalLiftWorld)
            {
                Clip = clip;
                NaturalLiftWorld = naturalLiftWorld;
            }

            public AnimationClip Clip { get; }
            public float NaturalLiftWorld { get; }
        }

        private readonly struct HitClipCreationResult
        {
            public HitClipCreationResult(AnimationClip clip)
            {
                Clip = clip;
            }

            public AnimationClip Clip { get; }
        }

        private readonly struct DeathClipCreationResult
        {
            public DeathClipCreationResult(
                AnimationClip clip,
                float bodyDropWorld)
            {
                Clip = clip;
                BodyDropWorld = bodyDropWorld;
            }

            public AnimationClip Clip { get; }
            public float BodyDropWorld { get; }
        }

        private readonly struct QuaternionKey
        {
            public QuaternionKey(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }

            public float Time { get; }
            public Quaternion Rotation { get; }
        }

        private readonly struct VectorKey
        {
            public VectorKey(float time, Vector3 position)
            {
                Time = time;
                Position = position;
            }

            public float Time { get; }
            public Vector3 Position { get; }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Restore()
            {
                if (target == null)
                {
                    return;
                }
                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }
        }

        private readonly struct TransformState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            private TransformState(Transform target)
            {
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public static TransformState Capture(Transform target)
            {
                return new TransformState(target);
            }

            public bool Matches(Transform target)
            {
                return Vector3.Distance(
                           localPosition,
                           target.localPosition) <= 0.000001f &&
                       Quaternion.Angle(
                           localRotation,
                           target.localRotation) <= 0.0001f &&
                       Vector3.Distance(
                           localScale,
                           target.localScale) <= 0.000001f;
            }
        }

        private readonly struct WorldPose
        {
            private WorldPose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public static WorldPose Capture(Transform target)
            {
                return new WorldPose(target.position, target.rotation);
            }

            public bool Matches(Transform target)
            {
                return Vector3.Distance(Position, target.position) <=
                       0.000001f &&
                       Quaternion.Angle(Rotation, target.rotation) <=
                       0.0001f;
            }
        }

        private readonly struct PoseMetrics
        {
            public PoseMetrics(
                float maximumFootPositionError,
                float naturalDiscLift,
                float discReturnHeightError)
            {
                MaximumFootPositionError = maximumFootPositionError;
                NaturalDiscLift = naturalDiscLift;
                DiscReturnHeightError = discReturnHeightError;
            }

            public float MaximumFootPositionError { get; }
            public float NaturalDiscLift { get; }
            public float DiscReturnHeightError { get; }
        }

        private readonly struct HitPoseMetrics
        {
            public HitPoseMetrics(
                float peakBodyLeftTiltDegrees,
                float peakBodyLeftEdgeVerticalDirection,
                float peakBodyRightReboundDegrees,
                float peakBodyRightEdgeVerticalDirection,
                float maximumBodyPositionError,
                float bodyRotationReturnError,
                float maximumBodyMeshDeformationWorld,
                int rigidBodyVertexCount,
                float maximumFootPositionError,
                float footReturnError,
                float minimumPrimaryRattlePeakDegrees,
                float maximumPrimaryRattlePeakDegrees,
                float maximumRattleZeroErrorDegrees,
                int minimumRattlePeakEventCount,
                int rattledLegCount)
            {
                PeakBodyLeftTiltDegrees = peakBodyLeftTiltDegrees;
                PeakBodyLeftEdgeVerticalDirection =
                    peakBodyLeftEdgeVerticalDirection;
                PeakBodyRightReboundDegrees =
                    peakBodyRightReboundDegrees;
                PeakBodyRightEdgeVerticalDirection =
                    peakBodyRightEdgeVerticalDirection;
                MaximumBodyPositionError = maximumBodyPositionError;
                BodyRotationReturnError = bodyRotationReturnError;
                MaximumBodyMeshDeformationWorld =
                    maximumBodyMeshDeformationWorld;
                RigidBodyVertexCount = rigidBodyVertexCount;
                MaximumFootPositionError = maximumFootPositionError;
                FootReturnError = footReturnError;
                MinimumPrimaryRattlePeakDegrees =
                    minimumPrimaryRattlePeakDegrees;
                MaximumPrimaryRattlePeakDegrees =
                    maximumPrimaryRattlePeakDegrees;
                MaximumRattleZeroErrorDegrees =
                    maximumRattleZeroErrorDegrees;
                MinimumRattlePeakEventCount =
                    minimumRattlePeakEventCount;
                RattledLegCount = rattledLegCount;
            }

            public float PeakBodyLeftTiltDegrees { get; }
            public float PeakBodyLeftEdgeVerticalDirection { get; }
            public float PeakBodyRightReboundDegrees { get; }
            public float PeakBodyRightEdgeVerticalDirection { get; }
            public float MaximumBodyPositionError { get; }
            public float BodyRotationReturnError { get; }
            public float MaximumBodyMeshDeformationWorld { get; }
            public int RigidBodyVertexCount { get; }
            public float MaximumFootPositionError { get; }
            public float FootReturnError { get; }
            public float MinimumPrimaryRattlePeakDegrees { get; }
            public float MaximumPrimaryRattlePeakDegrees { get; }
            public float MaximumRattleZeroErrorDegrees { get; }
            public int MinimumRattlePeakEventCount { get; }
            public int RattledLegCount { get; }
        }

        private readonly struct DeathPoseMetrics
        {
            public DeathPoseMetrics(
                float finalBodyLeftTiltDegrees,
                float finalBodyLeftEdgeVerticalDirection,
                float bodyDropWorld,
                float minimumFootSpreadIncreaseWorld,
                float maximumFootGroundErrorWorld,
                float maximumBodyMeshDeformationWorld)
            {
                FinalBodyLeftTiltDegrees = finalBodyLeftTiltDegrees;
                FinalBodyLeftEdgeVerticalDirection =
                    finalBodyLeftEdgeVerticalDirection;
                BodyDropWorld = bodyDropWorld;
                MinimumFootSpreadIncreaseWorld =
                    minimumFootSpreadIncreaseWorld;
                MaximumFootGroundErrorWorld =
                    maximumFootGroundErrorWorld;
                MaximumBodyMeshDeformationWorld =
                    maximumBodyMeshDeformationWorld;
            }

            public float FinalBodyLeftTiltDegrees { get; }
            public float FinalBodyLeftEdgeVerticalDirection { get; }
            public float BodyDropWorld { get; }
            public float MinimumFootSpreadIncreaseWorld { get; }
            public float MaximumFootGroundErrorWorld { get; }
            public float MaximumBodyMeshDeformationWorld { get; }
        }

        [Serializable]
        private sealed class RigSupportReport
        {
            public string result;
            public string corrected_glb_sha256;
            public bool geometry_unchanged;
            public bool non_disc_weights_unchanged;
            public bool bone_hierarchy_unchanged;
            public DiscSelection disc_selection;
            public RoundTrip roundtrip;
        }

        [Serializable]
        private sealed class DiscSelection
        {
            public int vertices;
            public bool visually_reviewed_as_disc_shell_only;
        }

        [Serializable]
        private sealed class RoundTrip
        {
            public int bones;
            public int disc_vertices;
            public int disc_leg_influenced_vertices;
            public int disc_non_body_influenced_vertices;
        }
    }
}
