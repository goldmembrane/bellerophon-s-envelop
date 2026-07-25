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

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    internal static class RebellionMoveAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Rebellion Enemy Placement";
        private const string MoveSlotName = "Rebellion_01_Move";
        private const string ModelName = "Rebellion_Model";
        private const string BurstSlotName = "Rebellion_04_Forward_Burst_Fire";
        private const string BurstCylinderPivotName =
            "Rebellion_Gun_Cylinder_Pivot";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Controllers";
        private const string MoveClipPath =
            AnimationFolder + "/Rebellion_01_Move_SpiderCrawl.anim";
        private const string MoveControllerPath =
            ControllerFolder + "/Rebellion_01_Move_SpiderCrawl.controller";
        private const string AttackControllerPath =
            ControllerFolder + "/Rebellion_02_Attack_Mode_Transition.controller";
        private const string ScanControllerPath =
            ControllerFolder + "/Rebellion_03_Forward_Scan.controller";
        private const string BurstControllerPath =
            ControllerFolder + "/Rebellion_04_Forward_Burst_Fire.controller";
        private const string HitControllerPath =
            ControllerFolder + "/Rebellion_05_Hit_Reaction.controller";
        private const string CorrectedModelPath =
            "Assets/_Project/Art/Enemies/Rebellion/ApprovedAppearance/" +
            "Rebellion_ApprovedAppearance.glb";
        private const string CorrectedModelSha256 =
            "C791B028B759A82087C185A98ADD3A5412BCAE8A110DFAFF33F7E3E1694D60F9";
        private const string MoveStateName = "MoveSpiderCrawl";
        private const string RigInspectionPath =
            "docs/validation/rebellion_move_2026-07-25/" +
            "Rebellion_01_Move_Rig_Inspection.txt";
        private const string MoveInspectionPath =
            "docs/validation/rebellion_move_2026-07-25/" +
            "Rebellion_01_Move_Inspection.txt";
        private const string ReviewPath =
            "docs/validation/rebellion_move_2026-07-25/" +
            "Rebellion_01_Move_VisualReview.png";
        private const float MoveLoopSeconds = 1f;
        private const float StrideLength = 0.9f;
        private const float FootLift = 0.42f;
        private const float StanceFraction = 0.62f;
        private const int PanelWidth = 420;
        private const int PanelHeight = 420;

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

        private static readonly string[] WeaponBoneNames =
        {
            "Bone_008",
            "Bone_007",
            "Bone_006"
        };

        private static readonly float[] PosePhases =
        {
            0f,
            0.125f,
            0.25f,
            0.375f,
            0.5f,
            0.625f,
            0.75f,
            0.875f,
            1f
        };

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Move Rig")]
        public static void InspectMoveRig()
        {
            var scene = RequireActiveScene();
            var slot = RequireSlot(scene, MoveSlotName);
            var model = RequireModel(slot);
            var skinnedRenderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var rigBones = skinnedRenderers
                .SelectMany(renderer => renderer.bones)
                .Where(bone => bone != null)
                .Distinct()
                .OrderBy(bone => bone.name, StringComparer.Ordinal)
                .ToArray();
            if (rigBones.Length != 29)
            {
                throw new InvalidOperationException(
                    "Expected 29 Rebellion rig bones, found " + rigBones.Length + ".");
            }

            RequireCorrectedAttachmentStructure(model);
            var sceneWasDirty = scene.isDirty;
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("Slot=" + MoveSlotName);
            report.AppendLine("Model=" + ModelName);
            report.AppendLine("SceneChanged=False");
            report.AppendLine("SlotLocalPosition=" + Vec(slot.localPosition));
            report.AppendLine("SlotLocalRotation=" + Quat(slot.localRotation));
            report.AppendLine("SlotLocalScale=" + Vec(slot.localScale));
            report.AppendLine("ModelLocalPosition=" + Vec(model.localPosition));
            report.AppendLine("ModelLocalRotation=" + Quat(model.localRotation));
            report.AppendLine("ModelLocalScale=" + Vec(model.localScale));
            report.AppendLine("SkinnedRendererCount=" + skinnedRenderers.Length);
            report.AppendLine("DistinctRigBoneCount=" + rigBones.Length);
            report.AppendLine("WeaponBranch=Bone_008>Bone_007>Bone_006");
            report.AppendLine(
                "LegBranches=Bone_013>009;Bone_018>014;Bone_023>019;Bone_028>024");
            report.AppendLine();
            report.AppendLine("RendererName|RendererType|ParentName|HierarchyPath");
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)
                         .OrderBy(
                             item => AnimationUtility.CalculateTransformPath(
                                 item.transform,
                                 model),
                             StringComparer.Ordinal))
            {
                report.AppendLine(
                    renderer.name + "|" +
                    renderer.GetType().Name + "|" +
                    (renderer.transform.parent == null
                        ? "<none>"
                        : renderer.transform.parent.name) + "|" +
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        model));
            }

            report.AppendLine();
            report.AppendLine(
                "BoneName|ParentName|HierarchyPath|LocalPosition|" +
                "ModelPosition|LocalRotation");
            foreach (var bone in rigBones)
            {
                report.AppendLine(
                    bone.name + "|" +
                    (bone.parent == null ? "<none>" : bone.parent.name) + "|" +
                    AnimationUtility.CalculateTransformPath(bone, model) + "|" +
                    Vec(bone.localPosition) + "|" +
                    Vec(model.InverseTransformPoint(bone.position)) + "|" +
                    Quat(bone.localRotation));
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Rig inspection unexpectedly changed the scene dirty state.");
            }

            WriteText(RigInspectionPath, report.ToString());
            Debug.Log(
                "RebellionMoveRigInspected Result=PASS" +
                ", Slot=" + MoveSlotName +
                ", RigBones=" + rigBones.Length +
                ", SkinnedRenderers=" + skinnedRenderers.Length +
                ", WeaponBranchSeparated=True" +
                ", DetailAttachmentsCorrected=True" +
                ", SceneChanged=False" +
                ", Report=" + RigInspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Move Animation")]
        public static void ApplyMoveAnimation()
        {
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var slot = RequireSlot(scene, MoveSlotName);
            var model = RequireModel(slot);
            RequireCorrectedAttachmentStructure(model);
            var placementRoot = RequirePlacementRoot(scene);
            var rootState = TransformState.Capture(placementRoot);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            var clip = CreateMoveClip(slot, model);
            var controller = CreateMoveController(clip);
            var animator = slot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = slot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            RequireSameTransform(rootState, placementRoot, PlacementRootName);
            RequireSameTransform(slotState, slot, MoveSlotName);
            RequireSameTransform(modelState, model, ModelName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Rebellion move application.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "RebellionMoveAnimationApplied Result=PASS" +
                ", Slot=" + MoveSlotName +
                ", Clip=" + MoveClipPath +
                ", Controller=" + MoveControllerPath +
                ", LoopSeconds=" + MoveLoopSeconds.ToString("0.###") +
                ", Gait=DiagonalSpiderCrawl" +
                ", LegBones=20" +
                ", WeaponCurves=0" +
                ", RootMotion=False" +
                ", SlotTransformPreserved=True" +
                ", ModelTransformPreserved=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Move Animation")]
        public static void InspectMoveAnimation()
        {
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(scene, MoveSlotName);
            var model = RequireModel(slot);
            RequireCorrectedAttachmentStructure(model);
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               MoveSlotName + " has no Animator.");
            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Rebellion move Animator must not apply Root Motion.");
            }

            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(MoveControllerPath) ??
                throw new InvalidOperationException(
                    "Rebellion move AnimatorController is missing.");
            if (animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(
                    "Rebellion move slot does not use the expected controller.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MoveClipPath) ??
                       throw new InvalidOperationException(
                           "Rebellion move clip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
            {
                throw new InvalidOperationException(
                    "Rebellion move clip is not configured as a loop.");
            }
            if (Mathf.Abs(clip.length - MoveLoopSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Unexpected Rebellion move clip length: " + clip.length);
            }

            var allowedBones = CreateLegBoneNameSet();
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != allowedBones.Count * 4)
            {
                throw new InvalidOperationException(
                    "Expected " + (allowedBones.Count * 4) +
                    " Rebellion rotation bindings, found " + bindings.Length + ".");
            }

            var animatedBoneNames = new HashSet<string>(StringComparer.Ordinal);
            var maximumLoopBoundaryError = 0f;
            foreach (var binding in bindings)
            {
                if (binding.type != typeof(Transform) ||
                    !binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Rebellion move clip contains a non-rotation binding: " +
                        binding.path + " " + binding.propertyName);
                }

                var boneName = binding.path.Split('/').Last();
                if (!allowedBones.Contains(boneName))
                {
                    throw new InvalidOperationException(
                        "Rebellion move clip animates a non-leg bone: " + boneName);
                }

                animatedBoneNames.Add(boneName);
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "Rebellion move curve is missing: " + binding.path);
                maximumLoopBoundaryError = Mathf.Max(
                    maximumLoopBoundaryError,
                    Mathf.Abs(curve.Evaluate(0f) - curve.Evaluate(MoveLoopSeconds)));
            }

            if (animatedBoneNames.Count != allowedBones.Count)
            {
                throw new InvalidOperationException(
                    "Not all Rebellion leg bones are animated.");
            }
            if (WeaponBoneNames.Any(animatedBoneNames.Contains))
            {
                throw new InvalidOperationException(
                    "Rebellion move clip unexpectedly animates the weapon branch.");
            }
            if (maximumLoopBoundaryError > 0.00001f)
            {
                throw new InvalidOperationException(
                    "Rebellion move loop boundary error is " +
                    maximumLoopBoundaryError.ToString("0.########") + ".");
            }

            var unexpectedAnimatorControllers = new List<string>();
            foreach (var slotName in SlotNames)
            {
                var candidate = placementRoot.Find(slotName) ??
                                throw new InvalidOperationException(
                                    slotName + " is missing.");
                var candidateAnimator = candidate.GetComponent<Animator>();
                var actualControllerPath =
                    candidateAnimator == null ||
                    candidateAnimator.runtimeAnimatorController == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(
                            candidateAnimator.runtimeAnimatorController);
                var expectedControllerPath = slotName == MoveSlotName
                    ? MoveControllerPath
                    : slotName == "Rebellion_02_Attack_Mode_Transition"
                        ? AttackControllerPath
                        : slotName == "Rebellion_03_Forward_Scan"
                            ? ScanControllerPath
                            : slotName == "Rebellion_04_Forward_Burst_Fire"
                                ? BurstControllerPath
                                : slotName == "Rebellion_05_Hit_Reaction"
                                    ? HitControllerPath
                                    : string.Empty;
                if (!string.Equals(
                        actualControllerPath,
                        expectedControllerPath,
                        StringComparison.Ordinal))
                {
                    unexpectedAnimatorControllers.Add(slotName);
                }

                RequireCorrectedAttachmentStructure(RequireModel(candidate));
            }

            if (unexpectedAnimatorControllers.Count != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Rebellion Animator controller assignments: " +
                    string.Join(", ", unexpectedAnimatorControllers));
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("Slot=" + MoveSlotName);
            report.AppendLine("Clip=" + MoveClipPath);
            report.AppendLine("Controller=" + MoveControllerPath);
            report.AppendLine("State=" + MoveStateName);
            report.AppendLine("LoopSeconds=" + MoveLoopSeconds.ToString("0.###"));
            report.AppendLine("LoopEnabled=True");
            report.AppendLine("LoopBoundaryError=" +
                              maximumLoopBoundaryError.ToString("0.########"));
            report.AppendLine("Gait=DiagonalSpiderCrawl");
            report.AppendLine("AnimatedLegBones=" + animatedBoneNames.Count);
            report.AppendLine("RotationBindings=" + bindings.Length);
            report.AppendLine("WeaponCurves=0");
            report.AppendLine("RootMotion=False");
            report.AppendLine("SlotPositionFixed=True");
            report.AppendLine(
                "ExpectedOtherControllers=Rebellion_02;Rebellion_03;Rebellion_04");
            report.AppendLine("CorrectedRigAttachmentsOnAllSlots=True");
            report.AppendLine("CorrectedModelSha256=" + CorrectedModelSha256);
            WriteText(MoveInspectionPath, report.ToString());

            Debug.Log(
                "RebellionMoveAnimationInspected Result=PASS" +
                ", LoopSeconds=" + MoveLoopSeconds.ToString("0.###") +
                ", LoopBoundaryError=" +
                maximumLoopBoundaryError.ToString("0.########") +
                ", AnimatedLegBones=" + animatedBoneNames.Count +
                ", RotationBindings=" + bindings.Length +
                ", WeaponCurves=0" +
                ", RootMotion=False" +
                ", ExpectedOtherControllers=Rebellion_02;Rebellion_03" +
                ", CorrectedRigAttachmentsOnAllSlots=True" +
                ", Report=" + MoveInspectionPath + ".");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            CaptureRuntimeFrameForSlot(MoveSlotName, path);
        }

        internal static void CaptureRuntimeFrameForSlot(
            string slotName,
            string path)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Play Mode active scene must stay CargoRunMvp.");
            }

            var slot = RequireRuntimeSlot(scene, slotName);
            CapturePanel(slot, path);
        }

        internal static void CaptureRuntimeFrameForSlotFramedBy(
            string slotName,
            string framingChildName,
            string path)
        {
            CaptureRuntimeFrameForSlotFramedBy(
                slotName,
                framingChildName,
                null,
                path);
        }

        internal static void CaptureRuntimeFrameForSlotFramedBy(
            string slotName,
            string framingChildName,
            string excludedRendererName,
            string path)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Play Mode active scene must stay CargoRunMvp.");
            }

            var slot = RequireRuntimeSlot(scene, slotName);
            var framingRoot = slot.Find(framingChildName) ??
                              throw new InvalidOperationException(
                                  framingChildName + " is missing under " +
                                  slotName + ".");
            CapturePanel(
                slot,
                BoundsOf(framingRoot, excludedRendererName),
                path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            ComposePanels(panelPaths, outputPath, panelPaths.Count);
        }

        internal static string FinalReviewAbsolutePath => Absolute(ReviewPath);
        internal static string AnimatorStateName => MoveStateName;
        internal static float LoopSeconds => MoveLoopSeconds;

        private static AnimationClip CreateMoveClip(Transform slot, Transform model)
        {
            DeleteAssetIfPresent(MoveClipPath);
            var legs = new[]
            {
                CreateLeg(model, "FrontNegativeX", 0f,
                    "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019"),
                CreateLeg(model, "RearPositiveX", 0f,
                    "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014"),
                CreateLeg(model, "FrontPositiveX", 0.5f,
                    "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024"),
                CreateLeg(model, "RearNegativeX", 0.5f,
                    "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009")
            };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var rotationKeys = new Dictionary<Transform, List<QuaternionKey>>();
            foreach (var leg in legs)
            {
                foreach (var bone in leg.AllBones)
                {
                    if (!rotationKeys.ContainsKey(bone))
                    {
                        rotationKeys.Add(bone, new List<QuaternionKey>());
                    }
                }
            }

            try
            {
                foreach (var cycle in PosePhases)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var time = cycle * MoveLoopSeconds;
                    foreach (var leg in legs)
                    {
                        var phase = Mathf.Repeat(cycle + leg.PhaseOffset, 1f);
                        var targetOffset = FootTrajectory(phase);
                        var target =
                            model.TransformPoint(leg.RestFootModelPosition + targetOffset);
                        SolveCcd(leg.Joints, leg.Foot, target);
                        leg.Foot.rotation =
                            model.rotation * leg.RestFootModelRotation;
                    }

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
                name = "Rebellion_01_Move_SpiderCrawl",
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
            AssetDatabase.CreateAsset(clip, MoveClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static LegChain CreateLeg(
            Transform model,
            string label,
            float phaseOffset,
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
                        label + " is not a continuous Rebellion leg chain at " +
                        bones[index].name + ".");
                }
            }

            return new LegChain(
                label,
                phaseOffset,
                bones.Take(bones.Length - 1).ToArray(),
                bones[bones.Length - 1],
                model.InverseTransformPoint(bones[bones.Length - 1].position),
                Quaternion.Inverse(model.rotation) *
                bones[bones.Length - 1].rotation);
        }

        private static Vector3 FootTrajectory(float phase)
        {
            if (phase < StanceFraction)
            {
                var stance = phase / StanceFraction;
                return new Vector3(
                    0f,
                    0f,
                    Mathf.Lerp(
                        StrideLength * 0.5f,
                        -StrideLength * 0.5f,
                        stance));
            }

            var swing = (phase - StanceFraction) / (1f - StanceFraction);
            var eased = swing * swing * (3f - 2f * swing);
            return new Vector3(
                0f,
                Mathf.Sin(swing * Mathf.PI) * FootLift,
                Mathf.Lerp(
                    -StrideLength * 0.5f,
                    StrideLength * 0.5f,
                    eased));
        }

        private static void SolveCcd(
            IReadOnlyList<Transform> joints,
            Transform foot,
            Vector3 target)
        {
            for (var iteration = 0; iteration < 20; iteration++)
            {
                for (var index = joints.Count - 1; index >= 0; index--)
                {
                    var joint = joints[index];
                    var toFoot = foot.position - joint.position;
                    var toTarget = target - joint.position;
                    if (toFoot.sqrMagnitude < 0.000001f ||
                        toTarget.sqrMagnitude < 0.000001f)
                    {
                        continue;
                    }

                    var delta = Quaternion.FromToRotation(toFoot, toTarget);
                    delta = Quaternion.RotateTowards(
                        Quaternion.identity,
                        delta,
                        10f);
                    joint.rotation = delta * joint.rotation;
                }

                if ((foot.position - target).sqrMagnitude < 0.000004f)
                {
                    break;
                }
            }
        }

        private static AnimatorController CreateMoveController(AnimationClip clip)
        {
            DeleteAssetIfPresent(MoveControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(MoveControllerPath);
            var state = controller.layers[0].stateMachine.AddState(MoveStateName);
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
            var continuityValues = new List<QuaternionKey>(values.Count);
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

                continuityValues.Add(new QuaternionKey(value.Time, rotation));
                previous = rotation;
            }

            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.x)).ToList());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.y)).ToList());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.z)).ToList());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                continuityValues.Select(
                    value => new Keyframe(value.Time, value.Rotation.w)).ToList());
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
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static HashSet<string> CreateLegBoneNameSet()
        {
            return new HashSet<string>(
                new[]
                {
                    "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009",
                    "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014",
                    "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019",
                    "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024"
                },
                StringComparer.Ordinal);
        }

        private static void RequireCorrectedAttachmentStructure(Transform model)
        {
            RequireBoneChain(model, "Bone_008", "Bone_007", "Bone_006");
            RequireBoneChain(
                model,
                "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009");
            RequireBoneChain(
                model,
                "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014");
            RequireBoneChain(
                model,
                "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019");
            RequireBoneChain(
                model,
                "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024");
            foreach (var name in BodyDetailNames)
            {
                var detail = RequireDescendant(model, name);
                if (detail.parent == null || detail.parent.name != "Bone_008")
                {
                    throw new InvalidOperationException(
                        name + " must be attached to Bone_008, found " +
                        (detail.parent == null ? "<none>" : detail.parent.name) + ".");
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
                        name + " must be attached to " + expectedParent +
                        ", found " +
                        (detail.parent == null ? "<none>" : detail.parent.name) + ".");
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
        }

        private static void RequireBoneChain(
            Transform model,
            params string[] names)
        {
            var previous = RequireDescendant(model, names[0]);
            for (var index = 1; index < names.Length; index++)
            {
                var current = RequireDescendant(model, names[index]);
                if (current.parent != previous)
                {
                    throw new InvalidOperationException(
                        "Expected " + current.name + " under " + previous.name + ".");
                }

                previous = current;
            }
        }

        private static void RequireCorrectedModelHash()
        {
            var absolute = Absolute(CorrectedModelPath);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Corrected Rebellion model is missing.",
                    absolute);
            }

            using var stream = File.OpenRead(absolute);
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty);
            if (!string.Equals(
                    actual,
                    CorrectedModelSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Corrected Rebellion model hash mismatch. Expected " +
                    CorrectedModelSha256 + ", found " + actual + ".");
            }
        }

        private static Scene RequireActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Rebellion move authoring requires Edit Mode.");
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

        private static Transform RequireRuntimeSlot(Scene scene, string slotName)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == PlacementRootName)
                       ?.transform.Find(slotName) ??
                   throw new InvalidOperationException(
                       slotName + " is missing in Play Mode.");
        }

        private static Transform RequireModel(Transform slot)
        {
            return slot.Find(ModelName) ??
                   throw new InvalidOperationException(
                       ModelName + " is missing under " + slot.name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + name + " under " + root.name +
                    ", found " + matches.Length + ".");
            }

            return matches[0];
        }

        private static void CapturePanel(Transform slot, string path)
        {
            CapturePanel(slot, BoundsOf(slot), path);
        }

        private static void CapturePanel(
            Transform slot,
            Bounds bounds,
            string path)
        {
            var hiddenRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(item =>
                    item.enabled &&
                    !item.transform.IsChildOf(slot))
                .ToArray();
            foreach (var renderer in hiddenRenderers)
            {
                renderer.enabled = false;
            }

            var cameraObject = new GameObject(
                "RebellionMoveCaptureCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "RebellionMoveCaptureKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "RebellionMoveCaptureFillLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.012f, 0.017f, 0.021f, 1f);
                camera.aspect = 1f;
                camera.orthographic = true;
                camera.orthographicSize =
                    Mathf.Max(0.25f, bounds.extents.magnitude * 1.08f);
                camera.nearClipPlane = 0.005f;
                camera.farClipPlane = 100f;

                var front = slot.forward.normalized;
                var right = slot.right.normalized;
                var distance = Mathf.Max(1f, bounds.extents.magnitude * 4f);
                var focus = bounds.center + Vector3.up * bounds.extents.y * 0.02f;
                camera.transform.position =
                    focus + front * distance * 0.78f +
                    right * distance * 0.62f +
                    Vector3.up * bounds.extents.y * 0.32f;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);

                var keyLight = keyLightObject.GetComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(0.82f, 0.91f, 1f);
                keyLight.intensity = 2.35f;
                keyLight.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

                var fillLight = fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.35f, 0.68f, 1f);
                fillLight.intensity = 8f;
                fillLight.range = distance * 2.5f;
                fillLight.transform.position =
                    focus - front * distance * 0.25f -
                    right * distance * 0.2f +
                    Vector3.up * bounds.extents.y * 0.5f;

                Capture(camera, path, PanelWidth, PanelHeight);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fillLightObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (var renderer in hiddenRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            return BoundsOf(root, null);
        }

        private static Bounds BoundsOf(
            Transform root,
            string excludedRendererName)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item =>
                    item.enabled &&
                    item.gameObject.activeInHierarchy &&
                    (string.IsNullOrEmpty(excludedRendererName) ||
                     item.name != excludedRendererName))
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    root.name + " has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void Capture(
            Camera camera,
            string path,
            int width,
            int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(path) ??
                throw new InvalidOperationException(
                    "Invalid Rebellion move capture folder."));
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target =
                new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image =
                new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ComposePanels(
            IReadOnlyList<string> panelPaths,
            string outputPath,
            int columns)
        {
            var panels = new List<Texture2D>();
            var rows = Mathf.CeilToInt(panelPaths.Count / (float)columns);
            var review = new Texture2D(
                PanelWidth * columns,
                PanelHeight * rows,
                TextureFormat.RGB24,
                false);
            try
            {
                for (var index = 0; index < panelPaths.Count; index++)
                {
                    var panel =
                        new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (!panel.LoadImage(File.ReadAllBytes(panelPaths[index])))
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                        throw new InvalidOperationException(
                            "Could not load Rebellion move review panel.");
                    }

                    panels.Add(panel);
                    var column = index % columns;
                    var rowFromTop = index / columns;
                    var row = rows - 1 - rowFromTop;
                    review.SetPixels32(
                        column * PanelWidth,
                        row * PanelHeight,
                        PanelWidth,
                        PanelHeight,
                        panel.GetPixels32());
                }

                review.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(outputPath) ??
                    throw new InvalidOperationException(
                        "Invalid Rebellion move review output folder."));
                File.WriteAllBytes(outputPath, review.EncodeToPNG());
            }
            finally
            {
                foreach (var panel in panels)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                UnityEngine.Object.DestroyImmediate(review);
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
                    "Unity project root could not be resolved.");
            return Path.Combine(
                projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Vec(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######})",
                value.x,
                value.y,
                value.z);
        }

        private static string Quat(Quaternion value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######},{3:0.######})",
                value.x,
                value.y,
                value.z,
                value.w);
        }

        private sealed class LegChain
        {
            public readonly string Label;
            public readonly float PhaseOffset;
            public readonly Transform[] Joints;
            public readonly Transform Foot;
            public readonly Vector3 RestFootModelPosition;
            public readonly Quaternion RestFootModelRotation;

            public LegChain(
                string label,
                float phaseOffset,
                Transform[] joints,
                Transform foot,
                Vector3 restFootModelPosition,
                Quaternion restFootModelRotation)
            {
                Label = label;
                PhaseOffset = phaseOffset;
                Joints = joints;
                Foot = foot;
                RestFootModelPosition = restFootModelPosition;
                RestFootModelRotation = restFootModelRotation;
            }

            public IEnumerable<Transform> AllBones
            {
                get
                {
                    foreach (var joint in Joints)
                    {
                        yield return joint;
                    }

                    yield return Foot;
                }
            }
        }

        private readonly struct QuaternionKey
        {
            public readonly float Time;
            public readonly Quaternion Rotation;

            public QuaternionKey(float time, Quaternion rotation)
            {
                Time = time;
                Rotation = rotation;
            }
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
                return Vector3.Distance(localPosition, target.localPosition) <= 0.000001f &&
                       Quaternion.Angle(localRotation, target.localRotation) <= 0.0001f &&
                       Vector3.Distance(localScale, target.localScale) <= 0.000001f;
            }
        }
    }
}
