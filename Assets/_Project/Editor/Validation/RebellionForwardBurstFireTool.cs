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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    internal static class RebellionForwardBurstFireTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Rebellion Enemy Placement";
        private const string SlotName = "Rebellion_04_Forward_Burst_Fire";
        private const string ModelName = "Rebellion_Model";
        private const string CylinderPivotName =
            "Rebellion_Gun_Cylinder_Pivot";
        private const string FlashPivotName = "Rebellion_Burst_Flash_Pivot";
        private const string FlashName = "Rebellion_Burst_Flash";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Rebellion/Controllers";
        private const string VfxFolder =
            "Assets/_Project/Art/Enemies/Rebellion/VFX";
        private const string ClipPath =
            AnimationFolder + "/Rebellion_04_Forward_Burst_Fire.anim";
        private const string ControllerPath =
            ControllerFolder + "/Rebellion_04_Forward_Burst_Fire.controller";
        private const string AttackClipPath =
            AnimationFolder + "/Rebellion_02_Attack_Mode_Transition.anim";
        private const string MoveClipPath =
            AnimationFolder + "/Rebellion_01_Move_SpiderCrawl.anim";
        private const string ScanClipPath =
            AnimationFolder + "/Rebellion_03_Forward_Scan.anim";
        private const string MoveControllerPath =
            ControllerFolder + "/Rebellion_01_Move_SpiderCrawl.controller";
        private const string AttackControllerPath =
            ControllerFolder + "/Rebellion_02_Attack_Mode_Transition.controller";
        private const string ScanControllerPath =
            ControllerFolder + "/Rebellion_03_Forward_Scan.controller";
        private const string HitControllerPath =
            ControllerFolder + "/Rebellion_05_Hit_Reaction.controller";
        private const string MeshPath =
            VfxFolder + "/Rebellion_Forward_Burst_Flash.asset";
        private const string MaterialPath =
            VfxFolder + "/Rebellion_Forward_Burst_Flash.mat";
        private const string TexturePath =
            VfxFolder + "/Rebellion_Forward_Burst_Flash_Gradient.png";
        private const string CorrectedModelPath =
            "Assets/_Project/Art/Enemies/Rebellion/ApprovedAppearance/" +
            "Rebellion_ApprovedAppearance.glb";
        private const string CorrectedModelSha256 =
            "C791B028B759A82087C185A98ADD3A5412BCAE8A110DFAFF33F7E3E1694D60F9";
        private const string ApprovalStatusPath =
            "artSample/enemies/rebellion/forward_burst_fire/" +
            "APPROVAL_STATUS.json";
        private const string InspectionPath =
            "docs/validation/rebellion_forward_burst_fire_2026-07-25/" +
            "Rebellion_04_ForwardBurstFire_Inspection.txt";
        private const string ReviewPath =
            "docs/validation/rebellion_forward_burst_fire_2026-07-25/" +
            "Rebellion_04_ForwardBurstFire_VisualReview.png";
        private const string StateName = "ForwardBurstFire";
        private const float AttackStandingPoseTime = 1.2f;
        private const float LoopSecondsValue = 5f;
        private const float ShotInterval = 0.2f;
        private const float FlashSeconds = 0.08f;
        private const float RotationDegreesPerShot = 360f / 7f;
        private const int ShotCount = 25;
        private const float FlashLength = 0.14f;
        private const float FlashWidth = 0.07f;
        private const float FlashMuzzleOffset = 0.002f;
        private const float StepEdgeSeconds = 1f / 240f;

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

        private static readonly string[] LegBoneNames =
        {
            "Bone_013", "Bone_012", "Bone_011", "Bone_010", "Bone_009",
            "Bone_018", "Bone_017", "Bone_016", "Bone_015", "Bone_014",
            "Bone_023", "Bone_022", "Bone_021", "Bone_020", "Bone_019",
            "Bone_028", "Bone_027", "Bone_026", "Bone_025", "Bone_024"
        };

        private static readonly string[] BarrelNames =
        {
            "Rebellion_Gun_Barrel_00",
            "Rebellion_Gun_Barrel_01",
            "Rebellion_Gun_Barrel_02",
            "Rebellion_Gun_Barrel_03",
            "Rebellion_Gun_Barrel_04",
            "Rebellion_Gun_Barrel_05",
            "Rebellion_Gun_Barrel_06"
        };

        private static readonly string[] CylinderDetailNames =
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

        [MenuItem("Bellerophon/Enemies/Rebellion/Apply Forward Burst Fire")]
        public static void ApplyForwardBurstFire()
        {
            RequireApprovedSample();
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(placementRoot, SlotName);
            var model = RequireModel(slot);
            var placementState = TransformState.Capture(placementRoot);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);
            EnsureEditableSlotModel(model);
            PrepareRigForApply(model);
            var protectedHashes = CaptureProtectedAssetHashes();
            var standingPose = CaptureStandingPose(slot, model);

            EnsureFolder(AnimationFolder);
            EnsureFolder(ControllerFolder);
            EnsureFolder(VfxFolder);
            var mesh = CreateFlashMesh();
            var texture = CreateFlashTexture();
            var material = CreateFlashMaterial(texture);
            var cylinder = CreateCylinderPivot(model, standingPose);
            var flash = CreateFlashObjects(
                slot,
                model,
                mesh,
                material,
                standingPose);
            var clip = CreateBurstClip(
                slot,
                model,
                cylinder,
                flash,
                standingPose);
            var controller = CreateController(clip);

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
            RequireSameTransform(slotState, slot, SlotName);
            RequireSameTransform(modelState, model, ModelName);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Rebellion forward " +
                    "burst fire application.");
            }
            AssetDatabase.SaveAssets();
            RequireProtectedAssetHashes(protectedHashes);

            Debug.Log(
                "RebellionForwardBurstFireApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Clip=" + ClipPath +
                ", Controller=" + ControllerPath +
                ", LoopSeconds=5" +
                ", ShotInterval=0.2" +
                ", ShotCount=25" +
                ", FlashSeconds=0.08" +
                ", RotationDegreesPerShot=" +
                RotationDegreesPerShot.ToString("0.######") +
                ", RotationDirectionFrontView=Clockwise" +
                ", FlashAnchor=UpperBarrelMuzzle" +
                ", RotatingObject=" + CylinderPivotName +
                ", Slot04ModelInstanceUnpacked=True" +
                ", Bone007RotationCurves=0" +
                ", GunHubFixed=True" +
                ", RotatingMuzzles=7" +
                ", CylinderCenterFixed=True" +
                ", CircularRing=False" +
                ", StandingPoseTime=1.2" +
                ", RootMotion=False" +
                ", PlacementPreserved=True" +
                ", ExistingAnimationsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Forward Burst Fire")]
        public static void InspectForwardBurstFire()
        {
            RequireApprovedSample();
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before forward burst inspection.");
            }

            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireSlot(placementRoot, SlotName);
            var model = RequireModel(slot);
            var cylinder = RequireAppliedCylinderRig(model);

            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "Forward burst fire clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "Forward burst fire controller is missing.");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) ??
                       throw new InvalidOperationException(
                           "Forward burst flash mesh is missing.");
            var material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) ??
                throw new InvalidOperationException(
                    "Forward burst flash material is missing.");
            var texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) ??
                throw new InvalidOperationException(
                    "Forward burst flash texture is missing.");
            var pivot = RequireDescendant(slot, FlashPivotName);
            var flash = RequireDescendant(pivot, FlashName);
            var filter = flash.GetComponent<MeshFilter>() ??
                         throw new InvalidOperationException(
                             "Forward burst flash MeshFilter is missing.");
            var renderer = flash.GetComponent<MeshRenderer>() ??
                           throw new InvalidOperationException(
                               "Forward burst flash MeshRenderer is missing.");

            if (pivot.parent == null || pivot.parent.name != "Bone_008")
            {
                throw new InvalidOperationException(
                    "Forward burst flash pivot must be fixed to Bone_008.");
            }
            if (IsDescendantOf(pivot, RequireDescendant(model, "Bone_007")))
            {
                throw new InvalidOperationException(
                    "Forward burst flash pivot must not rotate with Bone_007.");
            }
            if (filter.sharedMesh != mesh ||
                renderer.sharedMaterial != material ||
                material.GetTexture("_BaseMap") != texture)
            {
                throw new InvalidOperationException(
                    "Forward burst VFX asset assignments are unexpected.");
            }
            if (mesh.vertexCount != 20 ||
                mesh.triangles.Length != 72 ||
                Mathf.Abs(mesh.bounds.size.z - FlashLength) > 0.0001f ||
                Mathf.Abs(mesh.bounds.size.x - FlashWidth) > 0.0001f ||
                Mathf.Abs(mesh.bounds.size.y - FlashWidth) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Forward burst flash mesh dimensions or topology changed.");
            }
            if (Mathf.Abs(clip.length - LoopSecondsValue) > 0.0001f ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException(
                    "Forward burst fire clip must be a 5-second loop.");
            }

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "Forward burst Animator is missing.");
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Forward burst Animator configuration is unexpected.");
            }

            var bindings = InspectBindings(clip);
            if (bindings.LegRotationBones != LegBoneNames.Length ||
                bindings.BodyPositionBones != 1 ||
                bindings.CylinderRotationBindings != 4 ||
                bindings.Bone007RotationBindings != 0 ||
                bindings.FlashScaleBindings != 3 ||
                bindings.UnexpectedBindings != 0)
            {
                throw new InvalidOperationException(
                    "Forward burst animation bindings are unexpected.");
            }

            var pose = InspectMotion(
                slot,
                model,
                cylinder,
                pivot,
                flash,
                clip);
            if (pose.StandingPositionError > 0.0001f ||
                pose.StandingRotationError > 0.01f ||
                pose.MaximumMuzzleAlignmentError > 0.013f ||
                pose.MaximumCylinderRotationError > 0.05f ||
                pose.MaximumCylinderCenterPositionError > 0.00001f ||
                pose.MaximumRingAxisAlignmentError > 0.05f ||
                pose.MaximumRingRadiusError > 0.00001f ||
                pose.MaximumMuzzleOutOfPlaneError > 0.00001f ||
                pose.MaximumBone007RotationError > 0.01f ||
                pose.MaximumGunHubPositionError > 0.00001f ||
                pose.MaximumGunHubRotationError > 0.01f ||
                pose.MinimumFlashOnScale < 0.95f ||
                pose.MaximumFlashOffScale > 0.05f ||
                pose.ClockwiseDisplacement <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Forward burst pose, muzzle alignment, flash timing, or " +
                    "clockwise rotation inspection failed. " +
                    "StandingPositionError=" +
                    pose.StandingPositionError.ToString("0.######") +
                    ", StandingRotationError=" +
                    pose.StandingRotationError.ToString("0.######") +
                    ", MaximumMuzzleAlignmentError=" +
                    pose.MaximumMuzzleAlignmentError.ToString("0.######") +
                    ", MaximumCylinderRotationError=" +
                    pose.MaximumCylinderRotationError.ToString("0.######") +
                    ", MaximumCylinderCenterPositionError=" +
                    pose.MaximumCylinderCenterPositionError
                        .ToString("0.######") +
                    ", MaximumRingAxisAlignmentError=" +
                    pose.MaximumRingAxisAlignmentError
                        .ToString("0.######") +
                    ", MaximumRingRadiusError=" +
                    pose.MaximumRingRadiusError.ToString("0.######") +
                    ", MaximumMuzzleOutOfPlaneError=" +
                    pose.MaximumMuzzleOutOfPlaneError
                        .ToString("0.######") +
                    ", MaximumBone007RotationError=" +
                    pose.MaximumBone007RotationError.ToString("0.######") +
                    ", MaximumGunHubPositionError=" +
                    pose.MaximumGunHubPositionError.ToString("0.######") +
                    ", MaximumGunHubRotationError=" +
                    pose.MaximumGunHubRotationError.ToString("0.######") +
                    ", MinimumFlashOnScale=" +
                    pose.MinimumFlashOnScale.ToString("0.######") +
                    ", MaximumFlashOffScale=" +
                    pose.MaximumFlashOffScale.ToString("0.######") +
                    ", ClockwiseDisplacement=" +
                    pose.ClockwiseDisplacement.ToString("0.######") + ".");
            }

            RequireAnimatorAssignments(placementRoot);
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + SlotName);
            report.AppendLine("Clip=" + ClipPath);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("State=" + StateName);
            report.AppendLine("LoopSeconds=5");
            report.AppendLine("LoopEnabled=True");
            report.AppendLine("StandingPoseSource=" +
                              AttackClipPath + "@1.2");
            report.AppendLine("ShotIntervalSeconds=0.2");
            report.AppendLine("ShotCount=25");
            report.AppendLine("ShotTimes=0.0..4.8");
            report.AppendLine("FlashDurationSeconds=0.08");
            report.AppendLine("RotationDurationSeconds=0.12");
            report.AppendLine("RotationDegreesPerShot=" +
                              RotationDegreesPerShot.ToString("0.######"));
            report.AppendLine("RotationDirectionFrontView=Clockwise");
            report.AppendLine("RotatingObject=" + CylinderPivotName);
            report.AppendLine("RotatingRigBone=None");
            report.AppendLine("WeaponBranch=Bone_008>Bone_007>Bone_006");
            report.AppendLine("CylinderPivotParent=Bone_007");
            report.AppendLine(
                "CylinderPivotPosition=AverageOfSevenBarrelMeshCenters");
            report.AppendLine(
                "CylinderRotationAxis=NormalOfSevenMuzzleCenterPlane");
            report.AppendLine("MuzzleAlignmentToleranceMeters=0.013");
            report.AppendLine("RotatingMuzzles=GunBarrel00..06");
            report.AppendLine("RotatingMuzzleCount=7");
            report.AppendLine("GunHubParent=Bone_007");
            report.AppendLine("GunHubFixed=True");
            report.AppendLine("Bone007RotationCurves=0");
            report.AppendLine("CylinderCenterFixed=True");
            report.AppendLine("FlashAnchor=UpperBarrelMuzzle");
            report.AppendLine("FlashParent=Bone_008");
            report.AppendLine("FlashLengthMeters=0.14");
            report.AppendLine("FlashMaximumWidthMeters=0.07");
            report.AppendLine("FlashPalette=WhiteYellowCore;YellowMiddle;OrangeEdge");
            report.AppendLine("CircularRing=False");
            report.AppendLine("Smoke=False");
            report.AppendLine("Casing=False");
            report.AppendLine("Projectile=False");
            report.AppendLine("Trail=False");
            report.AppendLine("Recoil=False");
            report.AppendLine("LegRotationBones=" +
                              bindings.LegRotationBones);
            report.AppendLine("CylinderRotationBindings=" +
                              bindings.CylinderRotationBindings);
            report.AppendLine("Bone007RotationBindings=" +
                              bindings.Bone007RotationBindings);
            report.AppendLine("FlashScaleBindings=" +
                              bindings.FlashScaleBindings);
            report.AppendLine("StandingPositionError=" +
                              pose.StandingPositionError.ToString("0.######"));
            report.AppendLine("StandingRotationError=" +
                              pose.StandingRotationError.ToString("0.######"));
            report.AppendLine("MaximumMuzzleAlignmentError=" +
                              pose.MaximumMuzzleAlignmentError
                                  .ToString("0.######"));
            report.AppendLine("MaximumCylinderRotationError=" +
                              pose.MaximumCylinderRotationError
                                  .ToString("0.######"));
            report.AppendLine("MaximumCylinderCenterPositionError=" +
                              pose.MaximumCylinderCenterPositionError
                                  .ToString("0.######"));
            report.AppendLine("MaximumRingAxisAlignmentErrorDegrees=" +
                              pose.MaximumRingAxisAlignmentError
                                  .ToString("0.######"));
            report.AppendLine("MaximumRingRadiusError=" +
                              pose.MaximumRingRadiusError
                                  .ToString("0.######"));
            report.AppendLine("MaximumMuzzleOutOfPlaneError=" +
                              pose.MaximumMuzzleOutOfPlaneError
                                  .ToString("0.######"));
            report.AppendLine("MaximumBone007RotationError=" +
                              pose.MaximumBone007RotationError
                                  .ToString("0.######"));
            report.AppendLine("MaximumGunHubPositionError=" +
                              pose.MaximumGunHubPositionError
                                  .ToString("0.######"));
            report.AppendLine("MaximumGunHubRotationError=" +
                              pose.MaximumGunHubRotationError
                                  .ToString("0.######"));
            report.AppendLine("MinimumFlashOnScale=" +
                              pose.MinimumFlashOnScale.ToString("0.######"));
            report.AppendLine("MaximumFlashOffScale=" +
                              pose.MaximumFlashOffScale.ToString("0.######"));
            report.AppendLine("ClockwiseDisplacement=" +
                              pose.ClockwiseDisplacement.ToString("0.######"));
            report.AppendLine("RootMotion=False");
            report.AppendLine("PlacementFixed=True");
            report.AppendLine("CorrectedModelSha256=" +
                              CorrectedModelSha256);
            WriteText(InspectionPath, report.ToString());

            Debug.Log(
                "RebellionForwardBurstFireInspected Result=PASS" +
                ", LoopSeconds=5" +
                ", ShotInterval=0.2" +
                ", ShotCount=25" +
                ", FlashSeconds=0.08" +
                ", RotationDegreesPerShot=" +
                RotationDegreesPerShot.ToString("0.######") +
                ", RotationDirectionFrontView=Clockwise" +
                ", RotatingObject=" + CylinderPivotName +
                ", Bone007RotationBindings=0" +
                ", GunHubFixed=True" +
                ", RotatingMuzzleCount=7" +
                ", MaximumCylinderCenterPositionError=" +
                pose.MaximumCylinderCenterPositionError
                    .ToString("0.######") +
                ", MaximumMuzzleAlignmentError=" +
                pose.MaximumMuzzleAlignmentError.ToString("0.######") +
                ", CircularRing=False" +
                ", RootMotion=False" +
                ", PlacementFixed=True" +
                ", Report=" + InspectionPath + ".");
        }

        internal static void CaptureRuntimeFrame(string path)
        {
            RebellionMoveAnimationTool.CaptureRuntimeFrameForSlotFramedBy(
                SlotName,
                ModelName,
                FlashName,
                path);
        }

        internal static void ComposeRuntimeReview(
            IReadOnlyList<string> panelPaths,
            string outputPath)
        {
            const int columns = 5;
            if (panelPaths.Count == 0)
            {
                throw new InvalidOperationException(
                    "Forward burst review has no panels.");
            }

            var panels = new List<Texture2D>();
            try
            {
                foreach (var path in panelPaths)
                {
                    var panel = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (!panel.LoadImage(File.ReadAllBytes(path)))
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                        throw new InvalidOperationException(
                            "Could not load forward burst review panel.");
                    }
                    panels.Add(panel);
                }

                var panelWidth = panels[0].width;
                var panelHeight = panels[0].height;
                var rows = Mathf.CeilToInt(panelPaths.Count / (float)columns);
                var review = new Texture2D(
                    panelWidth * columns,
                    panelHeight * rows,
                    TextureFormat.RGB24,
                    false);
                try
                {
                    var background =
                        Enumerable.Repeat(
                            new Color(0.035f, 0.035f, 0.045f),
                            review.width * review.height)
                            .ToArray();
                    review.SetPixels(background);
                    for (var index = 0; index < panels.Count; index++)
                    {
                        var column = index % columns;
                        var row = rows - 1 - (index / columns);
                        review.SetPixels(
                            column * panelWidth,
                            row * panelHeight,
                            panelWidth,
                            panelHeight,
                            panels[index].GetPixels());
                    }
                    review.Apply();
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(outputPath) ??
                        throw new InvalidOperationException(
                            "Forward burst review output path is invalid."));
                    File.WriteAllBytes(outputPath, review.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(review);
                }
            }
            finally
            {
                foreach (var panel in panels)
                {
                    UnityEngine.Object.DestroyImmediate(panel);
                }
            }
        }

        internal static string FinalReviewAbsolutePath =>
            Absolute(ReviewPath);
        internal static string AnimatorStateName => StateName;
        internal static float LoopSeconds => LoopSecondsValue;

        private static StandingPose CaptureStandingPose(
            Transform slot,
            Transform model)
        {
            var attackClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AttackClipPath) ??
                throw new InvalidOperationException(
                    "Attack transition clip is missing.");
            if (attackClip.length < AttackStandingPoseTime)
            {
                throw new InvalidOperationException(
                    "Attack transition clip does not contain its standing peak.");
            }

            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                attackClip.SampleAnimation(
                    slot.gameObject,
                    AttackStandingPoseTime);
                var body = RequireDescendant(model, "Bone_001");
                var weapon = RequireDescendant(model, "Bone_007");
                var weaponTip = RequireDescendant(model, "Bone_006");
                var outward = weaponTip.position - weapon.position;
                if (outward.sqrMagnitude < 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Weapon outward axis is unavailable.");
                }
                outward.Normalize();
                var ringFrame =
                    CalculateBarrelRingFrame(model, outward);
                var upperBarrel = RequireUpperBarrel(
                    model,
                    ringFrame.Center,
                    ringFrame.Up);
                var muzzle = MuzzlePoint(upperBarrel, ringFrame.Normal);

                return new StandingPose(
                    body.localPosition,
                    LegBoneNames.ToDictionary(
                        name => name,
                        name => RequireDescendant(model, name).localRotation,
                        StringComparer.Ordinal),
                    weapon.InverseTransformPoint(ringFrame.Center),
                    Quaternion.Inverse(weapon.rotation) *
                    Quaternion.LookRotation(ringFrame.Normal, ringFrame.Up),
                    RequireDescendant(model, "Bone_008")
                        .InverseTransformPoint(
                            muzzle + (ringFrame.Normal * FlashMuzzleOffset)),
                    Quaternion.Inverse(
                        RequireDescendant(model, "Bone_008").rotation) *
                    Quaternion.LookRotation(ringFrame.Normal, ringFrame.Up),
                    upperBarrel.name);
            }
            finally
            {
                RestoreAll(snapshots);
            }
        }

        private static Transform CreateCylinderPivot(
            Transform model,
            StandingPose standingPose)
        {
            var weapon = RequireDescendant(model, "Bone_007");
            var existing = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == CylinderPivotName);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Cylinder pivot must be removed before recreation.");
            }

            var pivotObject = new GameObject(CylinderPivotName);
            var pivot = pivotObject.transform;
            pivot.SetParent(weapon, false);
            pivot.localPosition = standingPose.CylinderPivotLocalPosition;
            pivot.localRotation = standingPose.CylinderPivotLocalRotation;
            pivot.localScale = Vector3.one;
            foreach (var detailName in BarrelNames)
            {
                var detail = RequireDescendant(model, detailName);
                detail.SetParent(pivot, true);
                EditorUtility.SetDirty(detail);
            }
            EditorUtility.SetDirty(pivotObject);
            return pivot;
        }

        private static Mesh CreateFlashMesh()
        {
            DeleteAssetIfPresent(MeshPath);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (var plane = 0; plane < 4; plane++)
            {
                var radians = plane * Mathf.PI * 0.25f;
                var radial =
                    new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                var start = vertices.Count;
                vertices.Add(-radial * 0.005f);
                vertices.Add((-radial * (FlashWidth * 0.5f)) +
                             Vector3.forward * 0.04f);
                vertices.Add(Vector3.forward * FlashLength);
                vertices.Add((radial * (FlashWidth * 0.5f)) +
                             Vector3.forward * 0.04f);
                vertices.Add(radial * 0.005f);
                uvs.Add(new Vector2(0f, 0.45f));
                uvs.Add(new Vector2(0.285714f, 0f));
                uvs.Add(new Vector2(1f, 0.5f));
                uvs.Add(new Vector2(0.285714f, 1f));
                uvs.Add(new Vector2(0f, 0.55f));
                AddDoubleSidedTriangle(triangles, start, start + 1, start + 2);
                AddDoubleSidedTriangle(triangles, start, start + 2, start + 3);
                AddDoubleSidedTriangle(triangles, start, start + 3, start + 4);
            }

            var mesh = new Mesh
            {
                name = "Rebellion_Forward_Burst_Flash",
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void AddDoubleSidedTriangle(
            ICollection<int> triangles,
            int a,
            int b,
            int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(a);
        }

        private static Texture2D CreateFlashTexture()
        {
            const int width = 128;
            const int height = 64;
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);
            try
            {
                var pixels = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    var v = y / (float)(height - 1);
                    var radial = Mathf.Abs((v * 2f) - 1f);
                    for (var x = 0; x < width; x++)
                    {
                        var u = x / (float)(width - 1);
                        var core = 1f - SmoothStep(0.04f, 0.48f, radial);
                        var edge = SmoothStep(0.3f, 1f, radial);
                        var color = Color.Lerp(
                            new Color(1f, 0.98f, 0.72f),
                            new Color(1f, 0.18f, 0.01f),
                            edge);
                        color = Color.Lerp(
                            color,
                            new Color(1f, 1f, 0.94f),
                            core * (1f - u));
                        var longitudinal =
                            (1f - SmoothStep(0.72f, 1f, u)) *
                            Mathf.Lerp(1f, 0.55f, u);
                        var alpha =
                            (1f - SmoothStep(0.72f, 1f, radial)) *
                            longitudinal;
                        pixels[(y * width) + x] =
                            new Color(color.r, color.g, color.b, alpha);
                    }
                }
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(
                    Absolute(TexturePath),
                    texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(
                TexturePath,
                ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(TexturePath)
                as TextureImporter ??
                throw new InvalidOperationException(
                    "Forward burst texture importer is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) ??
                   throw new InvalidOperationException(
                       "Forward burst texture failed to import.");
        }

        private static Material CreateFlashMaterial(Texture2D texture)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                throw new InvalidOperationException(
                    "URP Unlit shader is missing.");
            var material =
                AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "Rebellion_Forward_Burst_Flash"
                };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateFlashObjects(
            Transform slot,
            Transform model,
            Mesh mesh,
            Material material,
            StandingPose standingPose)
        {
            var existing = slot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == FlashPivotName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var bodyWeapon = RequireDescendant(model, "Bone_008");
            var pivotObject = new GameObject(FlashPivotName);
            var pivot = pivotObject.transform;
            pivot.SetParent(bodyWeapon, false);
            pivot.localPosition = standingPose.FlashPivotLocalPosition;
            pivot.localRotation = standingPose.FlashPivotLocalRotation;
            var parentScale = bodyWeapon.lossyScale;
            if (Mathf.Abs(parentScale.x) < 0.000001f ||
                Mathf.Abs(parentScale.y) < 0.000001f ||
                Mathf.Abs(parentScale.z) < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Rebellion weapon scale cannot support world-space flash.");
            }
            pivot.localScale = new Vector3(
                1f / Mathf.Abs(parentScale.x),
                1f / Mathf.Abs(parentScale.y),
                1f / Mathf.Abs(parentScale.z));

            var flashObject = new GameObject(FlashName);
            var flash = flashObject.transform;
            flash.SetParent(pivot, false);
            flash.localPosition = Vector3.zero;
            flash.localRotation = Quaternion.identity;
            flash.localScale = Vector3.one;
            flashObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = flashObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            EditorUtility.SetDirty(pivotObject);
            EditorUtility.SetDirty(flashObject);
            return flash;
        }

        private static AnimationClip CreateBurstClip(
            Transform slot,
            Transform model,
            Transform cylinder,
            Transform flash,
            StandingPose standingPose)
        {
            DeleteAssetIfPresent(ClipPath);
            var clip = new AnimationClip
            {
                name = "Rebellion_04_Forward_Burst_Fire",
                frameRate = 60f
            };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var body = RequireDescendant(model, "Bone_001");
            SetVectorCurves(
                clip,
                AnimationUtility.CalculateTransformPath(body, slot),
                new[]
                {
                    new VectorKey(0f, standingPose.BodyLocalPosition),
                    new VectorKey(
                        LoopSecondsValue,
                        standingPose.BodyLocalPosition)
                });
            foreach (var boneName in LegBoneNames)
            {
                var bone = RequireDescendant(model, boneName);
                var rotation = standingPose.LegLocalRotations[boneName];
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(bone, slot),
                    new[]
                    {
                        new QuaternionKey(0f, rotation),
                        new QuaternionKey(LoopSecondsValue, rotation)
                    });
            }

            var cylinderKeys = new List<QuaternionKey>();
            for (var shot = 0; shot < ShotCount; shot++)
            {
                var start = shot * ShotInterval;
                var flashEnd = start + FlashSeconds;
                var end = start + ShotInterval;
                cylinderKeys.Add(
                    new QuaternionKey(
                        start,
                        CylinderRotation(standingPose, shot)));
                cylinderKeys.Add(
                    new QuaternionKey(
                        flashEnd,
                        CylinderRotation(standingPose, shot)));
                cylinderKeys.Add(
                    new QuaternionKey(
                        end,
                        CylinderRotation(standingPose, shot + 1)));
            }
            SetQuaternionCurves(
                clip,
                AnimationUtility.CalculateTransformPath(cylinder, slot),
                CoalesceQuaternionKeys(cylinderKeys));

            var flashKeys = new List<Keyframe>();
            for (var shot = 0; shot < ShotCount; shot++)
            {
                var start = shot * ShotInterval;
                var flashEnd = start + FlashSeconds;
                var next = start + ShotInterval;
                flashKeys.Add(new Keyframe(start, 1f));
                flashKeys.Add(
                    new Keyframe(
                        Mathf.Max(start, flashEnd - StepEdgeSeconds),
                        1f));
                flashKeys.Add(new Keyframe(flashEnd, 0f));
                flashKeys.Add(
                    new Keyframe(
                        Mathf.Max(flashEnd, next - StepEdgeSeconds),
                        0f));
            }
            flashKeys.Add(new Keyframe(LoopSecondsValue, 1f));
            var flashPath =
                AnimationUtility.CalculateTransformPath(flash, slot);
            SetLinearCurve(
                clip,
                flashPath,
                "m_LocalScale.x",
                CoalesceKeyframes(flashKeys));
            SetLinearCurve(
                clip,
                flashPath,
                "m_LocalScale.y",
                CoalesceKeyframes(flashKeys));
            SetLinearCurve(
                clip,
                flashPath,
                "m_LocalScale.z",
                CoalesceKeyframes(flashKeys));

            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Quaternion CylinderRotation(
            StandingPose pose,
            int completedSteps)
        {
            return pose.CylinderPivotLocalRotation *
                   Quaternion.AngleAxis(
                       completedSteps * RotationDegreesPerShot,
                       Vector3.forward);
        }

        private static IReadOnlyList<QuaternionKey> CoalesceQuaternionKeys(
            IEnumerable<QuaternionKey> source)
        {
            return source
                .GroupBy(item => Mathf.RoundToInt(item.Time * 100000f))
                .OrderBy(group => group.Key)
                .Select(group => group.Last())
                .ToArray();
        }

        private static Keyframe[] CoalesceKeyframes(
            IEnumerable<Keyframe> source)
        {
            return source
                .GroupBy(item => Mathf.RoundToInt(item.time * 100000f))
                .OrderBy(group => group.Key)
                .Select(group => group.Last())
                .ToArray();
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            var state =
                controller.layers[0].stateMachine.AddState(StateName);
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static BindingMetrics InspectBindings(AnimationClip clip)
        {
            var legs = new HashSet<string>(LegBoneNames, StringComparer.Ordinal);
            var legRotationBones =
                new HashSet<string>(StringComparer.Ordinal);
            var bodyPositionBones =
                new HashSet<string>(StringComparer.Ordinal);
            var cylinderRotationBindings = 0;
            var bone007RotationBindings = 0;
            var flashScaleBindings = 0;
            var unexpected = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var name = binding.path.Split('/').Last();
                if (binding.type == typeof(Transform) &&
                    binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal) &&
                    legs.Contains(name))
                {
                    legRotationBones.Add(name);
                }
                else if (binding.type == typeof(Transform) &&
                         binding.propertyName.StartsWith(
                             "m_LocalPosition.",
                             StringComparison.Ordinal) &&
                         name == "Bone_001")
                {
                    bodyPositionBones.Add(name);
                }
                else if (binding.type == typeof(Transform) &&
                         binding.propertyName.StartsWith(
                             "m_LocalRotation.",
                             StringComparison.Ordinal) &&
                         name == CylinderPivotName)
            {
                    cylinderRotationBindings++;
                }
                else if (binding.type == typeof(Transform) &&
                         binding.propertyName.StartsWith(
                             "m_LocalRotation.",
                             StringComparison.Ordinal) &&
                         name == "Bone_007")
                {
                    bone007RotationBindings++;
                }
                else if (binding.type == typeof(Transform) &&
                         binding.propertyName.StartsWith(
                             "m_LocalScale.",
                             StringComparison.Ordinal) &&
                         name == FlashName)
                {
                    flashScaleBindings++;
                }
                else
                {
                    unexpected++;
                }
            }
            return new BindingMetrics(
                legRotationBones.Count,
                bodyPositionBones.Count,
                cylinderRotationBindings,
                bone007RotationBindings,
                flashScaleBindings,
                unexpected);
        }

        private static MotionMetrics InspectMotion(
            Transform slot,
            Transform model,
            Transform cylinder,
            Transform pivot,
            Transform flash,
            AnimationClip clip)
        {
            var attack =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AttackClipPath) ??
                throw new InvalidOperationException(
                    "Attack transition clip is missing.");
            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var tracked = LegBoneNames
                .Select(name => RequireDescendant(model, name))
                .Concat(new[] { RequireDescendant(model, "Bone_001") })
                .ToArray();
            var expected =
                new Dictionary<string, LocalPose>(StringComparer.Ordinal);
            var standingPositionError = 0f;
            var standingRotationError = 0f;
            var maximumMuzzleAlignmentError = 0f;
            var maximumCylinderRotationError = 0f;
            var maximumCylinderCenterPositionError = 0f;
            var maximumRingAxisAlignmentError = 0f;
            var maximumRingRadiusError = 0f;
            var maximumMuzzleOutOfPlaneError = 0f;
            var maximumBone007RotationError = 0f;
            var maximumGunHubPositionError = 0f;
            var maximumGunHubRotationError = 0f;
            var minimumFlashOnScale = float.PositiveInfinity;
            var maximumFlashOffScale = 0f;
            var clockwiseDisplacement = 0f;
            var weapon = RequireDescendant(model, "Bone_007");
            var weaponTip = RequireDescendant(model, "Bone_006");
            var startingUpperBarrelName = string.Empty;
            var startingUpperBarrelPosition = Vector3.zero;
            var frontRight = Vector3.zero;
            var cylinderCenterPosition = Vector3.zero;
            var cylinderBaseRotation = Quaternion.identity;
            var bone007BaseRotation = Quaternion.identity;
            var gunHub = RequireDescendant(model, "Rebellion_Gun_Hub");
            var gunHubBasePose = default(LocalPose);
            var startingRingRadii =
                new Dictionary<string, float>(StringComparer.Ordinal);
            var startingMuzzlePlaneOffsets =
                new Dictionary<string, float>(StringComparer.Ordinal);

            try
            {
                RestoreAll(snapshots);
                attack.SampleAnimation(slot.gameObject, AttackStandingPoseTime);
                foreach (var item in tracked)
                {
                    expected[item.name] = LocalPose.Capture(item);
                }

                RestoreAll(snapshots);
                clip.SampleAnimation(slot.gameObject, 0f);
                var approximateOutward =
                    (weaponTip.position - weapon.position).normalized;
                var ringFrame =
                    CalculateBarrelRingFrame(model, approximateOutward);
                frontRight = Vector3.Cross(
                    ringFrame.Up,
                    -ringFrame.Normal).normalized;
                var upper = RequireUpperBarrel(
                    model,
                    ringFrame.Center,
                    ringFrame.Up);
                startingUpperBarrelName = upper.name;
                startingUpperBarrelPosition = upper.bounds.center;
                cylinderCenterPosition = cylinder.position;
                cylinderBaseRotation = cylinder.localRotation;
                bone007BaseRotation = weapon.localRotation;
                gunHubBasePose = LocalPose.Capture(gunHub);
                maximumRingAxisAlignmentError = AxisAlignmentAngle(
                    cylinder.forward,
                    ringFrame.Normal);
                foreach (var barrelName in BarrelNames)
                {
                    var center = MeshCenterPoint(
                        RequireRenderer(model, barrelName));
                    var offset = center - cylinder.position;
                    startingRingRadii[barrelName] =
                        Vector3.ProjectOnPlane(offset, cylinder.forward)
                            .magnitude;
                    startingMuzzlePlaneOffsets[barrelName] =
                        Vector3.Dot(offset, cylinder.forward);
                }

                for (var shot = 0; shot < ShotCount; shot++)
                {
                    var shotTime = shot * ShotInterval;
                    RestoreAll(snapshots);
                    clip.SampleAnimation(slot.gameObject, shotTime + 0.02f);
                    approximateOutward =
                        (weaponTip.position - weapon.position).normalized;
                    ringFrame = CalculateBarrelRingFrame(
                        model,
                        approximateOutward);
                    foreach (var item in tracked)
                    {
                        var actual = LocalPose.Capture(item);
                        var target = expected[item.name];
                        standingPositionError = Mathf.Max(
                            standingPositionError,
                            Vector3.Distance(actual.Position, target.Position));
                        standingRotationError = Mathf.Max(
                            standingRotationError,
                            Quaternion.Angle(actual.Rotation, target.Rotation));
                    }

                    var nearestMuzzleError = BarrelNames
                        .Select(name =>
                            MuzzlePoint(
                                RequireRenderer(model, name),
                                ringFrame.Normal))
                        .Min(point =>
                            Vector3.Distance(point, pivot.position));
                    maximumMuzzleAlignmentError = Mathf.Max(
                        maximumMuzzleAlignmentError,
                        nearestMuzzleError);
                    maximumCylinderCenterPositionError = Mathf.Max(
                        maximumCylinderCenterPositionError,
                        Vector3.Distance(
                            cylinder.position,
                            cylinderCenterPosition));
                    maximumRingAxisAlignmentError = Mathf.Max(
                        maximumRingAxisAlignmentError,
                        AxisAlignmentAngle(
                            cylinder.forward,
                            ringFrame.Normal));
                    foreach (var barrelName in BarrelNames)
                    {
                        var center = MeshCenterPoint(
                            RequireRenderer(model, barrelName));
                        var offset = center - cylinder.position;
                        var radius = Vector3.ProjectOnPlane(
                                offset,
                                cylinder.forward)
                            .magnitude;
                        maximumRingRadiusError = Mathf.Max(
                            maximumRingRadiusError,
                            Mathf.Abs(
                                radius - startingRingRadii[barrelName]));
                        var planeOffset =
                            Vector3.Dot(offset, cylinder.forward);
                        maximumMuzzleOutOfPlaneError = Mathf.Max(
                            maximumMuzzleOutOfPlaneError,
                            Mathf.Abs(
                                planeOffset -
                                startingMuzzlePlaneOffsets[barrelName]));
                    }
                    maximumBone007RotationError = Mathf.Max(
                        maximumBone007RotationError,
                        Quaternion.Angle(
                            weapon.localRotation,
                            bone007BaseRotation));
                    maximumGunHubPositionError = Mathf.Max(
                        maximumGunHubPositionError,
                        Vector3.Distance(
                            gunHub.localPosition,
                            gunHubBasePose.Position));
                    maximumGunHubRotationError = Mathf.Max(
                        maximumGunHubRotationError,
                        Quaternion.Angle(
                            gunHub.localRotation,
                            gunHubBasePose.Rotation));
                    minimumFlashOnScale = Mathf.Min(
                        minimumFlashOnScale,
                        MinimumComponent(flash.localScale));

                    var expectedRotation =
                        cylinderBaseRotation *
                        Quaternion.AngleAxis(
                            shot * RotationDegreesPerShot,
                            Vector3.forward);
                    maximumCylinderRotationError = Mathf.Max(
                        maximumCylinderRotationError,
                        Quaternion.Angle(
                            cylinder.localRotation,
                            expectedRotation));

                    RestoreAll(snapshots);
                    clip.SampleAnimation(slot.gameObject, shotTime + 0.1f);
                    maximumFlashOffScale = Mathf.Max(
                        maximumFlashOffScale,
                        MaximumComponent(flash.localScale));
                }

                RestoreAll(snapshots);
                clip.SampleAnimation(slot.gameObject, 0.14f);
                var trackedUpper =
                    RequireRenderer(model, startingUpperBarrelName);
                clockwiseDisplacement = Vector3.Dot(
                    trackedUpper.bounds.center - startingUpperBarrelPosition,
                    frontRight);
            }
            finally
            {
                RestoreAll(snapshots);
            }

            return new MotionMetrics(
                standingPositionError,
                standingRotationError,
                maximumMuzzleAlignmentError,
                maximumCylinderRotationError,
                maximumCylinderCenterPositionError,
                maximumRingAxisAlignmentError,
                maximumRingRadiusError,
                maximumMuzzleOutOfPlaneError,
                maximumBone007RotationError,
                maximumGunHubPositionError,
                maximumGunHubRotationError,
                minimumFlashOnScale,
                maximumFlashOffScale,
                clockwiseDisplacement);
        }

        private static float MinimumComponent(Vector3 value)
        {
            return Mathf.Min(value.x, Mathf.Min(value.y, value.z));
        }

        private static float MaximumComponent(Vector3 value)
        {
            return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
        }

        private static Renderer RequireUpperBarrel(
            Transform model,
            Vector3 ringCenter,
            Vector3 up)
        {
            return BarrelNames
                .Select(name => RequireRenderer(model, name))
                .OrderByDescending(item =>
                    Vector3.Dot(item.bounds.center - ringCenter, up))
                .First();
        }

        private static Vector3 MuzzlePoint(
            Renderer renderer,
            Vector3 outward)
        {
            var filter = renderer.GetComponent<MeshFilter>() ??
                         throw new InvalidOperationException(
                             renderer.name + " MeshFilter is missing.");
            var mesh = filter.sharedMesh ??
                       throw new InvalidOperationException(
                           renderer.name + " mesh is missing.");
            var points = mesh.vertices
                .Select(renderer.transform.TransformPoint)
                .ToArray();
            if (points.Length == 0)
            {
                throw new InvalidOperationException(
                    renderer.name + " has no vertices.");
            }
            var maximum =
                points.Max(point => Vector3.Dot(point, outward));
            var muzzleRing = points
                .Where(point =>
                    maximum - Vector3.Dot(point, outward) < 0.0001f)
                .ToArray();
            return muzzleRing.Aggregate(Vector3.zero, (sum, point) =>
                       sum + point) /
                   muzzleRing.Length;
        }

        private static Vector3 MeshCenterPoint(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>() ??
                         throw new InvalidOperationException(
                             renderer.name + " MeshFilter is missing.");
            var mesh = filter.sharedMesh ??
                       throw new InvalidOperationException(
                           renderer.name + " mesh is missing.");
            return renderer.transform.TransformPoint(mesh.bounds.center);
        }

        private static BarrelRingFrame CalculateBarrelRingFrame(
            Transform model,
            Vector3 approximateOutward)
        {
            if (approximateOutward.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Approximate weapon outward axis is unavailable.");
            }
            approximateOutward.Normalize();

            var centers = BarrelNames
                .Select(name =>
                    MeshCenterPoint(RequireRenderer(model, name)))
                .ToArray();
            var center =
                centers.Aggregate(Vector3.zero, (sum, point) => sum + point) /
                centers.Length;
            var offsets = centers.Select(point => point - center).ToArray();
            var crosses = new List<Vector3>();
            var reference = Vector3.zero;
            for (var first = 0; first < offsets.Length - 1; first++)
            {
                for (var second = first + 1;
                     second < offsets.Length;
                     second++)
                {
                    var cross =
                        Vector3.Cross(offsets[first], offsets[second]);
                    if (cross.sqrMagnitude < 0.0000000001f)
                    {
                        continue;
                    }
                    crosses.Add(cross);
                    if (cross.sqrMagnitude > reference.sqrMagnitude)
                    {
                        reference = cross;
                    }
                }
            }
            if (reference.sqrMagnitude < 0.0000000001f)
            {
                throw new InvalidOperationException(
                    "Seven muzzle centers do not define a rotation plane.");
            }

            if (Vector3.Dot(reference, approximateOutward) < 0f)
            {
                reference = -reference;
            }
            var normal = Vector3.zero;
            foreach (var cross in crosses)
            {
                var aligned =
                    Vector3.Dot(cross, reference) < 0f ? -cross : cross;
                normal += aligned.normalized;
            }
            if (normal.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Seven muzzle center plane normal is unavailable.");
            }
            normal.Normalize();
            if (Vector3.Dot(normal, approximateOutward) < 0f)
            {
                normal = -normal;
            }

            var up = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (up.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Muzzle ring front-view up axis is unavailable.");
            }
            up.Normalize();
            return new BarrelRingFrame(center, normal, up);
        }

        private static float AxisAlignmentAngle(
            Vector3 first,
            Vector3 second)
        {
            return Mathf.Min(
                Vector3.Angle(first, second),
                Vector3.Angle(first, -second));
        }

        private static Renderer RequireRenderer(
            Transform root,
            string name)
        {
            return RequireDescendant(root, name).GetComponent<Renderer>() ??
                   throw new InvalidOperationException(
                       name + " renderer is missing.");
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
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                continuity.Select(item =>
                    new Keyframe(item.Time, item.Rotation.x)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                continuity.Select(item =>
                    new Keyframe(item.Time, item.Rotation.y)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                continuity.Select(item =>
                    new Keyframe(item.Time, item.Rotation.z)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                continuity.Select(item =>
                    new Keyframe(item.Time, item.Rotation.w)).ToArray());
        }

        private static void SetVectorCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<VectorKey> values)
        {
            SetLinearCurve(
                clip,
                path,
                "m_LocalPosition.x",
                values.Select(item =>
                    new Keyframe(item.Time, item.Position.x)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalPosition.y",
                values.Select(item =>
                    new Keyframe(item.Time, item.Position.y)).ToArray());
            SetLinearCurve(
                clip,
                path,
                "m_LocalPosition.z",
                values.Select(item =>
                    new Keyframe(item.Time, item.Position.z)).ToArray());
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
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

        private static void RequireAnimatorAssignments(
            Transform placementRoot)
        {
            foreach (var slotName in SlotNames)
            {
                var slot = RequireSlot(placementRoot, slotName);
                var animator = slot.GetComponent<Animator>();
                var actual =
                    animator == null ||
                    animator.runtimeAnimatorController == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController);
                var expected = slotName == "Rebellion_01_Move"
                    ? MoveControllerPath
                    : slotName == "Rebellion_02_Attack_Mode_Transition"
                        ? AttackControllerPath
                        : slotName == "Rebellion_03_Forward_Scan"
                            ? ScanControllerPath
                            : slotName == SlotName
                                ? ControllerPath
                                : slotName == "Rebellion_05_Hit_Reaction"
                                    ? HitControllerPath
                                    : string.Empty;
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        slotName + " controller assignment is unexpected. " +
                        "Expected " + expected + ", found " + actual + ".");
                }
            }
        }

        private static Dictionary<string, string> CaptureProtectedAssetHashes()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [MoveClipPath] = Sha256IfPresent(MoveClipPath),
                [MoveControllerPath] = Sha256IfPresent(MoveControllerPath),
                [AttackClipPath] = Sha256IfPresent(AttackClipPath),
                [AttackControllerPath] =
                    Sha256IfPresent(AttackControllerPath),
                [ScanClipPath] = Sha256IfPresent(ScanClipPath),
                [ScanControllerPath] = Sha256IfPresent(ScanControllerPath)
            };
        }

        private static void RequireProtectedAssetHashes(
            IReadOnlyDictionary<string, string> expected)
        {
            foreach (var pair in expected)
            {
                if (!string.Equals(
                        Sha256IfPresent(pair.Key),
                        pair.Value,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        pair.Key + " changed unexpectedly.");
                }
            }
        }

        private static void PrepareRigForApply(Transform model)
        {
            var bodyWeapon = RequireDescendant(model, "Bone_008");
            var rotatingWeapon = RequireDescendant(model, "Bone_007");
            var weaponTip = RequireDescendant(model, "Bone_006");
            if (rotatingWeapon.parent != bodyWeapon ||
                weaponTip.parent != rotatingWeapon)
            {
                throw new InvalidOperationException(
                    "Weapon rig must remain Bone_008>Bone_007>Bone_006.");
            }

            var existingPivot = model.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == CylinderPivotName);
            if (existingPivot != null)
            {
                foreach (var detailName in CylinderDetailNames)
                {
                    var detail = RequireDescendant(model, detailName);
                    detail.SetParent(rotatingWeapon, true);
                    EditorUtility.SetDirty(detail);
                }
                UnityEngine.Object.DestroyImmediate(
                    existingPivot.gameObject);
            }

            foreach (var detailName in CylinderDetailNames)
            {
                var detail = RequireDescendant(model, detailName);
                if (detail.parent != rotatingWeapon)
                {
                    throw new InvalidOperationException(
                        detailName +
                        " must be restored as a direct child of Bone_007 " +
                        "before cylinder pivot creation.");
                }
            }
        }

        private static void EnsureEditableSlotModel(Transform model)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(model.gameObject))
            {
                return;
            }

            var instanceRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    model.gameObject);
            if (instanceRoot == null || instanceRoot.transform != model)
            {
                throw new InvalidOperationException(
                    "Slot 04 Rebellion model must be the prefab instance " +
                    "root before scene-only unpacking.");
            }
            PrefabUtility.UnpackPrefabInstance(
                instanceRoot,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            if (PrefabUtility.IsPartOfPrefabInstance(model.gameObject))
            {
                throw new InvalidOperationException(
                    "Slot 04 Rebellion model prefab instance could not be " +
                    "unpacked for the scene-only cylinder pivot.");
            }
        }

        private static Transform RequireAppliedCylinderRig(Transform model)
        {
            var bodyWeapon = RequireDescendant(model, "Bone_008");
            var weapon = RequireDescendant(model, "Bone_007");
            var weaponTip = RequireDescendant(model, "Bone_006");
            if (weapon.parent != bodyWeapon ||
                weaponTip.parent != weapon)
            {
                throw new InvalidOperationException(
                    "Weapon rig must remain Bone_008>Bone_007>Bone_006.");
            }

            var pivot = RequireDescendant(model, CylinderPivotName);
            if (pivot.parent != weapon)
            {
                throw new InvalidOperationException(
                    CylinderPivotName + " must be a direct child of Bone_007.");
            }
            var gunHub = RequireDescendant(model, "Rebellion_Gun_Hub");
            if (gunHub.parent != weapon)
            {
                throw new InvalidOperationException(
                    "Rebellion_Gun_Hub must stay fixed as a direct child of " +
                    "Bone_007.");
            }
            foreach (var detailName in BarrelNames)
            {
                if (RequireDescendant(model, detailName).parent != pivot)
                {
                    throw new InvalidOperationException(
                        detailName + " must be a direct child of " +
                        CylinderPivotName + ".");
                }
            }
            return pivot;
        }

        private static void RequireApprovedSample()
        {
            var path = Absolute(ApprovalStatusPath);
            var text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            if (!text.Contains("\"status\": \"APPROVED\"") ||
                !text.Contains("\"approved_for_unity\": true"))
            {
                throw new InvalidOperationException(
                    "Forward burst fire art sample is not approved.");
            }
        }

        private static void RequireCorrectedModelHash()
        {
            var absolute = Absolute(CorrectedModelPath);
            var actual =
                File.Exists(absolute) ? Sha256(absolute) : string.Empty;
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

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Current active scene must be CargoRunMvp.");
            }
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .FirstOrDefault(item =>
                           item.name == PlacementRootName)?.transform ??
                   throw new InvalidOperationException(
                       PlacementRootName + " is missing.");
        }

        private static Transform RequireSlot(
            Transform placementRoot,
            string slotName)
        {
            return placementRoot.Find(slotName) ??
                   throw new InvalidOperationException(
                       slotName + " is missing.");
        }

        private static Transform RequireModel(Transform slot)
        {
            return slot.Find(ModelName) ??
                   throw new InvalidOperationException(
                       slot.name + "/" + ModelName + " is missing.");
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       name + " is missing under " + root.name + ".");
        }

        private static bool IsDescendantOf(
            Transform candidate,
            Transform parent)
        {
            for (var current = candidate.parent;
                 current != null;
                 current = current.parent)
            {
                if (current == parent)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RestoreAll(
            IEnumerable<TransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
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

        private static float SmoothStep(
            float edge0,
            float edge1,
            float value)
        {
            return Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(edge0, edge1, value));
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

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException(
                        "Project root is unavailable."),
                    relativePath));
        }

        private readonly struct StandingPose
        {
            public StandingPose(
                Vector3 bodyLocalPosition,
                IReadOnlyDictionary<string, Quaternion> legLocalRotations,
                Vector3 cylinderPivotLocalPosition,
                Quaternion cylinderPivotLocalRotation,
                Vector3 flashPivotLocalPosition,
                Quaternion flashPivotLocalRotation,
                string upperBarrelName)
            {
                BodyLocalPosition = bodyLocalPosition;
                LegLocalRotations = legLocalRotations;
                CylinderPivotLocalPosition = cylinderPivotLocalPosition;
                CylinderPivotLocalRotation = cylinderPivotLocalRotation;
                FlashPivotLocalPosition = flashPivotLocalPosition;
                FlashPivotLocalRotation = flashPivotLocalRotation;
                UpperBarrelName = upperBarrelName;
            }

            public Vector3 BodyLocalPosition { get; }
            public IReadOnlyDictionary<string, Quaternion> LegLocalRotations
            {
                get;
            }
            public Vector3 CylinderPivotLocalPosition { get; }
            public Quaternion CylinderPivotLocalRotation { get; }
            public Vector3 FlashPivotLocalPosition { get; }
            public Quaternion FlashPivotLocalRotation { get; }
            public string UpperBarrelName { get; }
        }

        private readonly struct BindingMetrics
        {
            public BindingMetrics(
                int legRotationBones,
                int bodyPositionBones,
                int cylinderRotationBindings,
                int bone007RotationBindings,
                int flashScaleBindings,
                int unexpectedBindings)
            {
                LegRotationBones = legRotationBones;
                BodyPositionBones = bodyPositionBones;
                CylinderRotationBindings = cylinderRotationBindings;
                Bone007RotationBindings = bone007RotationBindings;
                FlashScaleBindings = flashScaleBindings;
                UnexpectedBindings = unexpectedBindings;
            }
            public int LegRotationBones { get; }
            public int BodyPositionBones { get; }
            public int CylinderRotationBindings { get; }
            public int Bone007RotationBindings { get; }
            public int FlashScaleBindings { get; }
            public int UnexpectedBindings { get; }
        }

        private readonly struct MotionMetrics
        {
            public MotionMetrics(
                float standingPositionError,
                float standingRotationError,
                float maximumMuzzleAlignmentError,
                float maximumCylinderRotationError,
                float maximumCylinderCenterPositionError,
                float maximumRingAxisAlignmentError,
                float maximumRingRadiusError,
                float maximumMuzzleOutOfPlaneError,
                float maximumBone007RotationError,
                float maximumGunHubPositionError,
                float maximumGunHubRotationError,
                float minimumFlashOnScale,
                float maximumFlashOffScale,
                float clockwiseDisplacement)
            {
                StandingPositionError = standingPositionError;
                StandingRotationError = standingRotationError;
                MaximumMuzzleAlignmentError = maximumMuzzleAlignmentError;
                MaximumCylinderRotationError = maximumCylinderRotationError;
                MaximumCylinderCenterPositionError =
                    maximumCylinderCenterPositionError;
                MaximumRingAxisAlignmentError =
                    maximumRingAxisAlignmentError;
                MaximumRingRadiusError = maximumRingRadiusError;
                MaximumMuzzleOutOfPlaneError =
                    maximumMuzzleOutOfPlaneError;
                MaximumBone007RotationError = maximumBone007RotationError;
                MaximumGunHubPositionError = maximumGunHubPositionError;
                MaximumGunHubRotationError = maximumGunHubRotationError;
                MinimumFlashOnScale = minimumFlashOnScale;
                MaximumFlashOffScale = maximumFlashOffScale;
                ClockwiseDisplacement = clockwiseDisplacement;
            }
            public float StandingPositionError { get; }
            public float StandingRotationError { get; }
            public float MaximumMuzzleAlignmentError { get; }
            public float MaximumCylinderRotationError { get; }
            public float MaximumCylinderCenterPositionError { get; }
            public float MaximumRingAxisAlignmentError { get; }
            public float MaximumRingRadiusError { get; }
            public float MaximumMuzzleOutOfPlaneError { get; }
            public float MaximumBone007RotationError { get; }
            public float MaximumGunHubPositionError { get; }
            public float MaximumGunHubRotationError { get; }
            public float MinimumFlashOnScale { get; }
            public float MaximumFlashOffScale { get; }
            public float ClockwiseDisplacement { get; }
        }

        private readonly struct BarrelRingFrame
        {
            public BarrelRingFrame(
                Vector3 center,
                Vector3 normal,
                Vector3 up)
            {
                Center = center;
                Normal = normal;
                Up = up;
            }

            public Vector3 Center { get; }
            public Vector3 Normal { get; }
            public Vector3 Up { get; }
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

        private readonly struct LocalPose
        {
            public LocalPose(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public static LocalPose Capture(Transform transform)
            {
                return new LocalPose(
                    transform.localPosition,
                    transform.localRotation);
            }
        }

        private readonly struct TransformState
        {
            private TransformState(
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }
            private Vector3 LocalPosition { get; }
            private Quaternion LocalRotation { get; }
            private Vector3 LocalScale { get; }
            public static TransformState Capture(Transform transform)
            {
                return new TransformState(
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale);
            }
            public bool Matches(Transform transform)
            {
                return Vector3.Distance(
                           LocalPosition,
                           transform.localPosition) < 0.000001f &&
                       Quaternion.Angle(
                           LocalRotation,
                           transform.localRotation) < 0.00001f &&
                       Vector3.Distance(
                           LocalScale,
                           transform.localScale) < 0.000001f;
            }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public void Restore()
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }
    }
}
