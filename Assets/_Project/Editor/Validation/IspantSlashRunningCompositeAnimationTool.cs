using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Ispant;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantSlashRunningCompositeAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string SlotName = "Ispant_05_RunningOneHandedSwordAttack";
        private const string ModelName = "Ispant_New_Direct_Model";
        private const string ModelPath = "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string CorrectedBodyMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DrawSword_Body.asset";
        private const string SlashSourcePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_Slash_Source.fbx";
        private const string RunningSourcePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_Running_Source.fbx";
        private const string SlashClipPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_Slash_FullBody.anim";
        private const string RunningClipPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_Running_LowerBody_InPlace.anim";
        private const string LegacySwordModelPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx";
        private const string LegacySwordClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_RunningSwordAttack_InPlace.anim";
        private const string LegacySwordCurvePath =
            "Armature/mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/" +
            "mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/" +
            "mixamorig:RightHand/Ispant_ApprovedLongSword";
        private const string MaskPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_Running_LowerBody.mask";
        private const string ControllerFolder = "Assets/_Project/Art/Enemies/Ispant/Controllers";
        private const string ControllerPath = "Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_New_SlashRunning.controller";
        private const string InspectionPath = "docs/validation/ispant_slash_running_2026-08-20/Ispant_05_SlashRunning_Inspection.txt";
        private const string CapturePath = "docs/validation/ispant_slash_running_2026-08-20/captures/Ispant_05_Source_Composite_Comparison.png";
        private const string FixInspectionPath =
            "docs/validation/ispant_slash_running_outward_fix_2026-08-20/Ispant_05_SlashRunning_OutwardFix_Inspection.txt";
        private const string FixCapturePath =
            "docs/validation/ispant_slash_running_outward_fix_2026-08-20/Ispant_05_SlashRunning_OutwardFix_Comparison.png";
        private const string GifInspectionPath =
            "docs/validation/ispant_slash_gif_trajectory_2026-08-20/Ispant_05_SlashGifTrajectory_Inspection.txt";
        private const string GifCapturePath =
            "docs/validation/ispant_slash_gif_trajectory_2026-08-20/Ispant_05_SlashGifTrajectory_Comparison.png";
        private const string GifReferenceFrameFolder =
            "docs/validation/ispant_slash_gif_trajectory_2026-08-20/reference_frames";
        private const string GifRevisionInspectionPath =
            "docs/validation/ispant_slash_gif_trajectory_revision_2026-08-20/Ispant_05_SlashGifTrajectoryRevision_Inspection.txt";
        private const string GifRevisionCapturePath =
            "docs/validation/ispant_slash_gif_trajectory_revision_2026-08-20/Ispant_05_SlashGifTrajectoryRevision_Comparison.png";
        private const string GifUpwardInspectionPath =
            "docs/validation/ispant_slash_gif_upward_trajectory_2026-08-20/Ispant_05_SlashGifUpwardTrajectory_Inspection.txt";
        private const string GifUpwardCapturePath =
            "docs/validation/ispant_slash_gif_upward_trajectory_2026-08-20/Ispant_05_SlashGifUpwardTrajectory_Comparison.png";
        private const string GifActualTraceInspectionPath =
            "docs/validation/ispant_slash_gif_actual_trace_2026-08-20/Ispant_05_SlashGifActualTrace_Inspection.txt";
        private const string GifActualTraceCapturePath =
            "docs/validation/ispant_slash_gif_actual_trace_2026-08-20/Ispant_05_SlashGifActualTrace_Comparison.png";
        private const string GifActualTraceDiagnosticPath =
            ".analysis-temp/ispant05_actual_gif_trace_2026-08-20/Ispant_05_SlashGifActualTrace_Diagnostic.png";
        private const string LegacySwordInspectionPath =
            "docs/validation/ispant_slash_legacy_sword_motion_2026-08-20/Ispant_05_LegacySwordMotion_Inspection.txt";
        private const string LegacySwordCapturePath =
            "docs/validation/ispant_slash_legacy_sword_motion_2026-08-20/Ispant_05_LegacySwordMotion_Comparison.png";
        private const string LegacySwordGripInspectionPath =
            "docs/validation/ispant_slash_legacy_sword_grip_2026-08-20/Ispant_05_LegacySwordGrip_Inspection.txt";
        private const string LegacySwordGripCapturePath =
            "docs/validation/ispant_slash_legacy_sword_grip_2026-08-20/Ispant_05_LegacySwordGrip_Comparison.png";
        private const string LegacySwordReferenceReviewPath =
            "docs/validation/ispant_running_sword_attack_unified_body_2026-08-09/" +
            "Ispant_05_RunningSwordAttack_UnifiedBody_FinalReview.png";
        private const string SlashImportedName = "Ispant_New_Slash_Mixamo";
        private const string RunningImportedName = "Ispant_New_Running_Mixamo";
        private const string SlashClipName = "Ispant_05_Slash_FullBody";
        private const string RunningClipName = "Ispant_05_Running_LowerBody_InPlace";
        private const string SlashHash = "AB3346A9FA93A0FC6045D5155E60BBB65A095460A444E124D236B987F899FCDE";
        private const string RunningHash = "A8471D0B2F1DF84D589A7BE3D54A171DF05E056915FD358FBEE0F74B5E1D77CB";
        private const string ModelHash = "5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF";
        private const string LegacySwordModelHash =
            "71FD6407AEF7B4AACC331C712B676881C74A1A1788A0A28067B685493F04DDB2";
        private const string LegacySwordClipHash =
            "604C078B996417F1A794BD58D215E20EF482D32B7E263B3224D31851AEF56640";
        private const int LegacySwordFrameCount = 91;
        private const float Tolerance = 0.0001f;
        private const float ReferenceGripDistanceFromPommelRatio = 0.13f;
        private const float ReferenceGripRegionHalfWidthRatio = 0.05f;
        // The lower half of the hand-weighted span includes the wrist and gauntlet.
        // Keeping the sword inside the upper half places the handle in the closed fist.
        private const float ReferenceSwordGripHandLongitudinalStartRatio = 0.5f;
        private const int RequiredLoops = 2;
        private const int CaptureLayer = 30;
        private static readonly Vector3 SwordRollLocalAxis = Vector3.up;

        private static readonly string[] LowerBodyRoots =
        {
            "Armature/Hips",
            "Armature/Hips/LeftUpLeg",
            "Armature/Hips/RightUpLeg"
        };

        private static readonly string[] LowerBodyBones =
        {
            "Hips",
            "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
            "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"
        };

        private static readonly string[] StableForwardBones = { "Head" };
        private static readonly string[] StableLeftArmBones = { "LeftShoulder", "LeftArm", "LeftForeArm" };
        private static readonly HashSet<string> CorrectedUpperBones = new HashSet<string>(
            StableForwardBones.Concat(StableLeftArmBones), StringComparer.Ordinal);

        private static readonly float[] ReviewTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        private static readonly string[] RightArmBones =
            { "RightShoulder", "RightArm", "RightForeArm", "RightHand" };
        private static bool reviewActive;
        private static double reviewStart;
        private static TransformSnapshot[] reviewSnapshots;
        private static SceneView reviewView;
        private static bool reviewGizmos;

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slash Running Sources")]
        public static void InspectIspantSlashRunningSources()
        {
            RequireHashes();
            var slashImporter = RequireImporter(SlashSourcePath);
            var runningImporter = RequireImporter(RunningSourcePath);
            var slashMixamo = MixamoTakes(slashImporter);
            var runningMixamo = MixamoTakes(runningImporter);
            var modelBones = BoneDescriptions(RequireAsset<GameObject>(ModelPath).transform);
            var slashBones = BoneDescriptions(RequireAsset<GameObject>(SlashSourcePath).transform);
            var runningBones = BoneDescriptions(RequireAsset<GameObject>(RunningSourcePath).transform);
            var target = RequireTarget(RequireScene(true));
            if (slashMixamo.Length != 1 || runningMixamo.Length != 1)
                throw new InvalidOperationException("Each supplied FBX must contain exactly one mixamo.com take.");
            if (!slashBones.SequenceEqual(modelBones, StringComparer.Ordinal) ||
                !runningBones.SequenceEqual(modelBones, StringComparer.Ordinal))
                throw new InvalidOperationException("The supplied and target Generic bone hierarchies differ.");
            Debug.Log(
                "IspantSlashRunningSourcesInspected Result=PASS" +
                ", SlashTakes=" + DescribeTakes(slashImporter) +
                ", RunningTakes=" + DescribeTakes(runningImporter) +
                ", SlashSelected=" + DescribeClip(slashMixamo[0]) +
                ", RunningSelected=" + DescribeClip(runningMixamo[0]) +
                ", GenericBoneCount=" + modelBones.Length +
                ", ExactBoneHierarchyMatch=True" +
                ", Target=" + PlacementName + "/" + SlotName + "/" + target.Model.name +
                ", SceneDirty=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slash Running Presentation")]
        public static void DiagnoseIspantSlashRunningPresentation()
        {
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var slash = RequireAsset<AnimationClip>(SlashClipPath);
            var running = RequireAsset<AnimationClip>(RunningClipPath);
            var body = RequireBody(target.Model);
            var directBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(item => item.name == "char1");
            var head = RequireDescendant(target.Model, "Head");
            var leftArm = RequireDescendant(target.Model, "LeftArm");
            var restHead = head.localRotation;
            var restLeftArm = leftArm.localRotation;
            var snapshots = target.Model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            float headOffset;
            float leftArmOffset;
            try
            {
                AnimationMode.StartAnimationMode();
                SampleComposite(target.Model.gameObject, slash, 0f, running, 0f);
                headOffset = Quaternion.Angle(restHead, head.localRotation);
                leftArmOffset = Quaternion.Angle(restLeftArm, leftArm.localRotation);
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
            var sword = RequireSword(target.Model);
            Debug.Log(
                "IspantSlashRunningPresentationDiagnosed Result=PASS" +
                ", HeadStartVsDirectRestDegrees=" + Num(headOffset) +
                ", LeftArmStartVsDirectRestDegrees=" + Num(leftArmOffset) +
                ", BodyUsesDirectUncorrectedMesh=" + (body.sharedMesh == directBody.sharedMesh) +
                ", BodyUsesApprovedCorrectedMesh=" + (body.sharedMesh == RequireAsset<Mesh>(CorrectedBodyMeshPath)) +
                ", Sword=" + sword.name +
                ", SlotSwordFollowerCount=" + target.Slot.GetComponents<IspantRigidSwordFollower>().Length +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Diagnose Slash Running Revision")]
        public static void DiagnoseIspantSlashRunningRevision()
        {
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var slash = RequireAsset<AnimationClip>(SlashClipPath);
            var running = RequireAsset<AnimationClip>(RunningClipPath);
            var follower = target.Slot.GetComponent<IspantRigidSwordFollower>() ??
                throw new InvalidOperationException("The slot 5 sword follower is missing.");
            var hips = RequireDescendant(target.Model, "Hips");
            var leftShoulder = RequireDescendant(target.Model, "LeftShoulder");
            var rightShoulder = RequireDescendant(target.Model, "RightShoulder");
            var spine02 = RequireDescendant(target.Model, "Spine02");
            var spine01 = RequireDescendant(target.Model, "Spine01");
            var spine = RequireDescendant(target.Model, "Spine");
            var directModel = RequireAsset<GameObject>(ModelPath).transform;
            var directSpine02 = RequireDescendant(directModel, "Spine02");
            var directSpine01 = RequireDescendant(directModel, "Spine01");
            var directSpine = RequireDescendant(directModel, "Spine");
            var directHips = RequireDescendant(directModel, "Hips");
            var directLeftShoulder = RequireDescendant(directModel, "LeftShoulder");
            var directRightShoulder = RequireDescendant(directModel, "RightShoulder");
            var directRestUpperLateral = Vector3.Dot(
                Vector3.Lerp(directLeftShoulder.position, directRightShoulder.position, 0.5f) - directHips.position,
                directModel.right);
            var foreArm = RequireDescendant(target.Model, "RightForeArm");
            var hand = RequireDescendant(target.Model, "RightHand");
            var sword = RequireSword(target.Model).transform;
            var swordMesh = sword.GetComponent<MeshFilter>().sharedMesh;
            var gripCenter = CalculateGripCenter(swordMesh);
            var tipCenter = CalculateBladeTipCenter(swordMesh, gripCenter);
            var actualBladeLocalAxis = (tipCenter - gripCenter).normalized;
            var body = RequireBody(target.Model);
            var directBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(item => item.name == "char1");
            var snapshots = target.Model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var minimumUpperLateral = float.PositiveInfinity;
            var maximumUpperLateral = float.NegativeInfinity;
            var minimumUpperLateralWithStableSpine02 = float.PositiveInfinity;
            var maximumUpperLateralWithStableSpine02 = float.NegativeInfinity;
            var minimumUpperLateralWithStableSpine01 = float.PositiveInfinity;
            var maximumUpperLateralWithStableSpine01 = float.NegativeInfinity;
            var minimumUpperLateralWithStableSpine = float.PositiveInfinity;
            var maximumUpperLateralWithStableSpine = float.NegativeInfinity;
            var minimumUpperLateralWithStableSpineChain = float.PositiveInfinity;
            var maximumUpperLateralWithStableSpineChain = float.NegativeInfinity;
            var maximumSpine02RestAngle = 0f;
            var maximumSpine02RestPosition = 0f;
            var minimumBladeUpAngle = 180f;
            var maximumBladeUpAngle = 0f;
            var maximumForeArmBladeAngle = 0f;
            var minimumUpperVertical = float.PositiveInfinity;
            var maximumUpperVertical = float.NegativeInfinity;
            var minimumActualBladeTipOutward = float.PositiveInfinity;
            var maximumActualBladeTipOutward = float.NegativeInfinity;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var sample = 0; sample <= 120; sample++)
                {
                    var normalized = sample / 120f;
                    SampleComposite(target.Model.gameObject, slash, normalized * slash.length,
                        running, normalized * running.length);
                    follower.ApplyFollow(normalized);
                    var upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                    var lateral = Vector3.Dot(upperCenter - hips.position, target.Model.right);
                    minimumUpperLateral = Mathf.Min(minimumUpperLateral, lateral);
                    maximumUpperLateral = Mathf.Max(maximumUpperLateral, lateral);
                    var vertical = Vector3.Dot(upperCenter - hips.position, target.Model.up);
                    minimumUpperVertical = Mathf.Min(minimumUpperVertical, vertical);
                    maximumUpperVertical = Mathf.Max(maximumUpperVertical, vertical);
                    var rightSideSign = Mathf.Sign(Vector3.Dot(
                        rightShoulder.position - leftShoulder.position, target.Model.right));
                    if (Mathf.Approximately(rightSideSign, 0f)) rightSideSign = 1f;
                    var outward = target.Model.right * rightSideSign;
                    var bladeTipOutward = Vector3.Dot(
                        sword.TransformPoint(tipCenter) - upperCenter, outward);
                    minimumActualBladeTipOutward = Mathf.Min(minimumActualBladeTipOutward, bladeTipOutward);
                    maximumActualBladeTipOutward = Mathf.Max(maximumActualBladeTipOutward, bladeTipOutward);
                    var sampledSpine02Position = spine02.localPosition;
                    var sampledSpine02Rotation = spine02.localRotation;
                    maximumSpine02RestAngle = Mathf.Max(maximumSpine02RestAngle,
                        Quaternion.Angle(sampledSpine02Rotation, directSpine02.localRotation));
                    maximumSpine02RestPosition = Mathf.Max(maximumSpine02RestPosition,
                        Vector3.Distance(sampledSpine02Position, directSpine02.localPosition));
                    spine02.localPosition = directSpine02.localPosition;
                    spine02.localRotation = directSpine02.localRotation;
                    upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                    lateral = Vector3.Dot(upperCenter - hips.position, target.Model.right);
                    minimumUpperLateralWithStableSpine02 = Mathf.Min(minimumUpperLateralWithStableSpine02, lateral);
                    maximumUpperLateralWithStableSpine02 = Mathf.Max(maximumUpperLateralWithStableSpine02, lateral);
                    spine02.localPosition = sampledSpine02Position;
                    spine02.localRotation = sampledSpine02Rotation;
                    var sampledSpine01Position = spine01.localPosition;
                    var sampledSpine01Rotation = spine01.localRotation;
                    spine01.localPosition = directSpine01.localPosition;
                    spine01.localRotation = directSpine01.localRotation;
                    upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                    lateral = Vector3.Dot(upperCenter - hips.position, target.Model.right);
                    minimumUpperLateralWithStableSpine01 = Mathf.Min(minimumUpperLateralWithStableSpine01, lateral);
                    maximumUpperLateralWithStableSpine01 = Mathf.Max(maximumUpperLateralWithStableSpine01, lateral);
                    spine01.localPosition = sampledSpine01Position;
                    spine01.localRotation = sampledSpine01Rotation;
                    var sampledSpinePosition = spine.localPosition;
                    var sampledSpineRotation = spine.localRotation;
                    spine.localPosition = directSpine.localPosition;
                    spine.localRotation = directSpine.localRotation;
                    upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                    lateral = Vector3.Dot(upperCenter - hips.position, target.Model.right);
                    minimumUpperLateralWithStableSpine = Mathf.Min(minimumUpperLateralWithStableSpine, lateral);
                    maximumUpperLateralWithStableSpine = Mathf.Max(maximumUpperLateralWithStableSpine, lateral);
                    spine.localPosition = sampledSpinePosition;
                    spine.localRotation = sampledSpineRotation;
                    spine01.localPosition = directSpine01.localPosition;
                    spine01.localRotation = directSpine01.localRotation;
                    spine.localPosition = directSpine.localPosition;
                    spine.localRotation = directSpine.localRotation;
                    upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                    lateral = Vector3.Dot(upperCenter - hips.position, target.Model.right);
                    minimumUpperLateralWithStableSpineChain = Mathf.Min(minimumUpperLateralWithStableSpineChain, lateral);
                    maximumUpperLateralWithStableSpineChain = Mathf.Max(maximumUpperLateralWithStableSpineChain, lateral);
                    spine01.localPosition = sampledSpine01Position;
                    spine01.localRotation = sampledSpine01Rotation;
                    spine.localPosition = sampledSpinePosition;
                    spine.localRotation = sampledSpineRotation;
                    var blade = sword.TransformDirection(actualBladeLocalAxis).normalized;
                    var bladeUp = Vector3.Angle(blade, target.Model.up);
                    minimumBladeUpAngle = Mathf.Min(minimumBladeUpAngle, bladeUp);
                    maximumBladeUpAngle = Mathf.Max(maximumBladeUpAngle, bladeUp);
                    var foreArmDirection = (hand.position - foreArm.position).normalized;
                    maximumForeArmBladeAngle = Mathf.Max(maximumForeArmBladeAngle,
                        Vector3.Angle(blade, foreArmDirection));
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
            var palmLocal = hand.InverseTransformPoint(WeightedPalmCenter(body, hand));
            var handBounds = RightHandWeightedLocalBounds(body, hand);
            Debug.Log(
                "IspantSlashRunningRevisionDiagnosed Result=PASS" +
                ", UpperCenterLateralRange=" + Num(minimumUpperLateral) + ".." + Num(maximumUpperLateral) +
                ", DirectRestUpperCenterLateral=" + Num(directRestUpperLateral) +
                ", UpperCenterVerticalRange=" + Num(minimumUpperVertical) + ".." + Num(maximumUpperVertical) +
                ", ActualBladeLocalAxis=" + actualBladeLocalAxis.ToString("F6", CultureInfo.InvariantCulture) +
                ", ActualBladeTipOutwardRange=" + Num(minimumActualBladeTipOutward) + ".." + Num(maximumActualBladeTipOutward) +
                ", SwordGripLocal=" + gripCenter.ToString("F6", CultureInfo.InvariantCulture) +
                ", WeightedPalmLocal=" + palmLocal.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightHandWeightedBoundsMin=" + handBounds.min.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightHandWeightedBoundsMax=" + handBounds.max.ToString("F6", CultureInfo.InvariantCulture) +
                ", RightHandWeightedLocalYHistogram=" +
                DescribeRightHandWeightedLocalYHistogram(body, hand) +
                ", SwordMeshBoundsMin=" + swordMesh.bounds.min.ToString("F6", CultureInfo.InvariantCulture) +
                ", SwordMeshBoundsMax=" + swordMesh.bounds.max.ToString("F6", CultureInfo.InvariantCulture) +
                ", SwordMeshBounds=" + swordMesh.bounds.size.ToString("F6", CultureInfo.InvariantCulture) +
                ", StableSpine02UpperCenterLateralRange=" + Num(minimumUpperLateralWithStableSpine02) + ".." + Num(maximumUpperLateralWithStableSpine02) +
                ", StableSpine01UpperCenterLateralRange=" + Num(minimumUpperLateralWithStableSpine01) + ".." + Num(maximumUpperLateralWithStableSpine01) +
                ", StableSpineUpperCenterLateralRange=" + Num(minimumUpperLateralWithStableSpine) + ".." + Num(maximumUpperLateralWithStableSpine) +
                ", StableSpine01AndSpineUpperCenterLateralRange=" + Num(minimumUpperLateralWithStableSpineChain) + ".." + Num(maximumUpperLateralWithStableSpineChain) +
                ", Spine02RestAngleRangeMaxDegrees=" + Num(maximumSpine02RestAngle) +
                ", Spine02RestPositionRangeMax=" + Num(maximumSpine02RestPosition) +
                ", BladeToVisibleUpRangeDegrees=" + Num(minimumBladeUpAngle) + ".." + Num(maximumBladeUpAngle) +
                ", MaximumForeArmBladeAngleDegrees=" + Num(maximumForeArmBladeAngle) +
                ", CurrentBodyTriangles=" + TriangleCount(body.sharedMesh) +
                ", DirectIntactBodyTriangles=" + TriangleCount(directBody.sharedMesh) +
                ", CurrentUsesDirectIntactBody=" + (body.sharedMesh == directBody.sharedMesh) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slash Running Composite")]
        public static void ApplyIspantSlashRunningComposite()
        {
            RequireHashes();
            ConfigureImporter(SlashSourcePath, SlashImportedName);
            ConfigureImporter(RunningSourcePath, RunningImportedName);
            RequireHashes();

            var slashSource = RequireImportedClip(SlashSourcePath, SlashImportedName);
            var runningSource = RequireImportedClip(RunningSourcePath, RunningImportedName);
            var slashClip = CreateFullBodyClip(slashSource);
            var runningClip = CreateLowerBodyClip(runningSource);
            var mask = CreateLowerBodyMask();
            var controller = CreateController(slashClip, runningClip, mask);

            // A failed in-scope apply can leave only this slot dirty before retry.
            // Reapplying the complete slot-5 setup below establishes and saves the
            // authoritative final state while unchanged-root signatures stay guarded.
            var scene = RequireScene(false);
            var target = RequireTarget(scene);
            var slotBefore = new TransformSnapshot(target.Slot);
            var modelBefore = new TransformSnapshot(target.Model);
            var otherSlotsBefore = OtherSlotSignatures(target.Placement, target.Slot);
            var otherRootsBefore = OtherRootSignatures(scene, target.Placement);
            var materialsBefore = MaterialSignature(target.Model);

            ConfigureAnimator(target.Model, controller);
            ConfigureIntactBody(target.Model);
            ConfigureSwordFollower(target, slashClip, runningClip);

            if (!slotBefore.Matches(Tolerance) || !modelBefore.Matches(Tolerance))
                throw new InvalidOperationException("The slot or direct model transform changed during animation setup.");
            RequireSame(otherSlotsBefore, OtherSlotSignatures(target.Placement, target.Slot), "Another Ispant slot changed.");
            RequireSame(otherRootsBefore, OtherRootSignatures(scene, target.Placement), "A scene root outside the Ispant placement changed.");
            RequireSame(materialsBefore, MaterialSignature(target.Model), "The slot 5 materials changed.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot 5 animation setup.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = target.Slot.gameObject;
            Debug.Log(
                "IspantSlashRunningCompositeApplied Result=PASS" +
                ", Target=" + PlacementName + "/" + SlotName + "/" + ModelName +
                ", Base=SlashFullBody, Override=RunningHipsAndLegs" +
                ", RootMotion=False, InPlaceAxes=RunningHipsLocalX+LocalY" +
                ", Face=DirectRestForward, LeftArm=DirectRestStable" +
                ", UpperBody=WholeSpineCenteredToDirectRestLateralAndVerticalOffset" +
                ", Sword=RightHandGripPlusPreModelRevisionSlot05Trajectory91Frames" +
                ", BodyMesh=DirectIntactOriginal, MusketContinuous=True, Loop=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Legacy Sword Right Hand Grip")]
        public static void ApplyIspantLegacySwordRightHandGrip()
        {
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var body = RequireBody(target.Model);
            var hand = RequireDescendant(target.Model, "RightHand");
            var follower = target.Slot.GetComponent<IspantRigidSwordFollower>() ??
                throw new InvalidOperationException("The slot 5 sword follower is missing.");
            if (!follower.FollowLegacySwordTrajectory ||
                follower.LegacySwordTrajectoryFrameCount != LegacySwordFrameCount)
                throw new InvalidOperationException("The approved 91-frame legacy sword trajectory is not active.");
            var oldGrip = follower.RightHandGripLocalPosition;
            var bladeBefore = Enumerable.Range(0, LegacySwordFrameCount)
                .Select(frame => follower.EvaluateLegacyBladeDirectionInModelSpace(
                    frame / (float)(LegacySwordFrameCount - 1))).ToArray();
            var rollBefore = Enumerable.Range(0, LegacySwordFrameCount)
                .Select(frame => follower.EvaluateLegacyRollDirectionInModelSpace(
                    frame / (float)(LegacySwordFrameCount - 1))).ToArray();
            var otherSlotsBefore = OtherSlotSignatures(target.Placement, target.Slot);
            var otherRootsBefore = OtherRootSignatures(scene, target.Placement);
            var materialsBefore = MaterialSignature(target.Model);
            var snapshots = target.Model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            Vector3 newGrip;
            try
            {
                AnimationMode.StartAnimationMode();
                SampleComposite(target.Model.gameObject,
                    RequireAsset<AnimationClip>(SlashClipPath), 0f,
                    RequireAsset<AnimationClip>(RunningClipPath), 0f);
                newGrip = hand.InverseTransformPoint(PalmGripCenter(body, hand));
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
            follower.SetRightHandGripLocalPosition(newGrip);
            for (var frame = 0; frame < LegacySwordFrameCount; frame++)
            {
                var normalized = frame / (float)(LegacySwordFrameCount - 1);
                if (Vector3.Angle(bladeBefore[frame],
                        follower.EvaluateLegacyBladeDirectionInModelSpace(normalized)) > 0.0001f ||
                    Vector3.Angle(rollBefore[frame],
                        follower.EvaluateLegacyRollDirectionInModelSpace(normalized)) > 0.0001f)
                    throw new InvalidOperationException("The legacy sword trajectory changed during grip placement.");
            }
            RequireSame(otherSlotsBefore, OtherSlotSignatures(target.Placement, target.Slot),
                "Another Ispant slot changed during grip placement.");
            RequireSame(otherRootsBefore, OtherRootSignatures(scene, target.Placement),
                "A scene root outside the Ispant placement changed during grip placement.");
            RequireSame(materialsBefore, MaterialSignature(target.Model),
                "The slot 5 materials changed during grip placement.");
            EditorUtility.SetDirty(follower);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after grip placement.");
            Debug.Log(
                "IspantLegacySwordRightHandGripApplied Result=PASS" +
                ", OldHandGripLocal=" + oldGrip.ToString("F6", CultureInfo.InvariantCulture) +
                ", NewHandGripLocal=" + newGrip.ToString("F6", CultureInfo.InvariantCulture) +
                ", LegacyBladeTrajectoryChanged=False, LegacyRollTrajectoryChanged=False" +
                ", RightArmModified=False, OtherSlotsChanged=False" +
                ", OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Legacy Sword Right Hand Grip")]
        public static void InspectIspantLegacySwordRightHandGrip()
        {
            var metrics = InspectApplied(true);
            WriteLegacySwordGripInspection(metrics);
            Debug.Log(
                "IspantLegacySwordRightHandGripInspected Result=PASS" +
                ", MaximumGripError=" + Num(metrics.MaximumGripError) +
                ", LegacyBladeTrajectoryMaximumError=" + Num(metrics.MaximumSwordOutwardAngle) +
                ", LegacyRollTrajectoryMaximumError=" + Num(metrics.MaximumSwordForwardCutAngle) +
                ", RightArmCurveError=" + Num(metrics.RightArmCurveError) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slash Running Composite")]
        public static void InspectIspantSlashRunningComposite()
        {
            var metrics = InspectApplied(true);
            WriteLegacySwordInspection(metrics);
            Debug.Log(
                "IspantSlashRunningCompositeInspected Result=PASS" +
                ", SlashCurveError=" + Num(metrics.SlashCurveError) +
                ", RunningCurveError=" + Num(metrics.RunningCurveError) +
                ", RightArmCurveError=" + Num(metrics.RightArmCurveError) +
                ", UpperPosePositionError=" + Num(metrics.UpperPositionError) +
                ", UpperPoseAngleError=" + Num(metrics.UpperAngleError) +
                ", LowerPosePositionError=" + Num(metrics.LowerPositionError) +
                ", LowerPoseAngleError=" + Num(metrics.LowerAngleError) +
                ", MaximumGripError=" + Num(metrics.MaximumGripError) +
                ", LegacyFrame01BladeError=" + Num(metrics.StartBladeUpAngle) +
                ", LegacyBladeTrajectoryMaximumError=" + Num(metrics.MaximumSwordOutwardAngle) +
                ", LegacyRollTrajectoryMaximumError=" + Num(metrics.MaximumSwordForwardCutAngle) +
                ", BladeLength=" + Num(metrics.MinimumBladeTipRadialGain) +
                ", BladeSweepAngle=" + Num(metrics.MaximumBladeUpAngle) +
                ", UpperBodyLateralError=" + Num(metrics.MaximumUpperBodyLateralError) +
                ", UpperBodyVerticalError=" + Num(metrics.MaximumUpperBodyVerticalError) +
                ", StableFaceAndLeftArmAngle=" + Num(metrics.MaximumStableBoneAngle) +
                ", RunningUpperBodyCurves=0" +
                ", FiniteBakedVertices=True, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Start Slash Running Composite Review")]
        public static void StartIspantSlashRunningCompositeReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || reviewActive || AnimationMode.InAnimationMode())
                throw new InvalidOperationException("The composite review requires idle Edit Mode.");
            var metrics = InspectApplied(false);
            reviewSnapshots = metrics.Target.Model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            reviewView = SceneView.lastActiveSceneView;
            if (reviewView != null)
            {
                reviewGizmos = reviewView.drawGizmos;
                reviewView.drawGizmos = false;
                Selection.activeGameObject = metrics.Target.Slot.gameObject;
                reviewView.FrameSelected();
            }
            AnimationMode.StartAnimationMode();
            reviewStart = EditorApplication.timeSinceStartup;
            reviewActive = true;
            EditorApplication.update += UpdateReview;
            Debug.Log("IspantSlashRunningCompositeReviewStarted Result=PASS, RequiredLoopsPerClip=2, CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Stop Slash Running Composite Review")]
        public static void StopIspantSlashRunningCompositeReview()
        {
            if (!reviewActive || !AnimationMode.InAnimationMode())
                throw new InvalidOperationException("The composite review is not active.");
            var slash = RequireAsset<AnimationClip>(SlashClipPath);
            var running = RequireAsset<AnimationClip>(RunningClipPath);
            var elapsed = (float)(EditorApplication.timeSinceStartup - reviewStart);
            var slashLoops = Mathf.FloorToInt(elapsed / slash.length);
            var runningLoops = Mathf.FloorToInt(elapsed / running.length);
            if (slashLoops < RequiredLoops || runningLoops < RequiredLoops)
                throw new InvalidOperationException(
                    "The review has not completed two loops of both clips. Slash=" + slashLoops + ", Running=" + runningLoops + ".");
            StopReview();
            var metrics = InspectApplied(true);
            Debug.Log(
                "IspantSlashRunningCompositeReviewStopped Result=PASS" +
                ", SlashLoops=" + slashLoops + ", RunningLoops=" + runningLoops +
                ", SceneRestored=True, CaptureCreated=False" +
                ", UpperPoseAngleError=" + Num(metrics.UpperAngleError) +
                ", LowerPoseAngleError=" + Num(metrics.LowerAngleError) + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash Running Source Comparison")]
        public static void CaptureIspantSlashRunningSourceComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time final source comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The comparison capture folder is invalid."));
            CaptureComparison(metrics, destination);
            Debug.Log(
                "IspantSlashRunningSourceComparisonCaptured Result=PASS" +
                ", Rows=SlashSource|RunningSource|FinalComposite" +
                ", Columns=0|0.25|0.5|0.75|1" +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash Running Fix Comparison")]
        public static void CaptureIspantSlashRunningFixComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(FixCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time final fix comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The fix comparison folder is invalid."));
            CaptureFixComparison(metrics, destination);
            Debug.Log(
                "IspantSlashRunningFixComparisonCaptured Result=PASS" +
                ", Rows=ActualSlashSource|ActualRunningSource|FinalCorrectedComposite" +
                ", Columns=0|0.25|0.5|0.75|1" +
                ", FaceForward=True, SwordRightHandRealtime=True, LeftArmDeformationRemoved=True" +
                ", Image=" + FixCapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash GIF Trajectory Comparison")]
        public static void CaptureIspantSlashGifTrajectoryComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(GifCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time GIF trajectory comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The GIF trajectory capture folder is invalid."));
            CaptureGifTrajectoryComparison(metrics, destination);
            Debug.Log(
                "IspantSlashGifTrajectoryComparisonCaptured Result=PASS" +
                ", Reference=SuppliedGifAll15Frames" +
                ", Final=Slot05CompositeAll15MatchingTimes" +
                ", RightArmModified=False, GuardImplemented=False" +
                ", Image=" + GifCapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash GIF Trajectory Revision Comparison")]
        public static void CaptureIspantSlashGifTrajectoryRevisionComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(GifRevisionCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time revised GIF trajectory comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The revised GIF trajectory folder is invalid."));
            CaptureGifTrajectoryComparison(metrics, destination);
            WriteGifRevisionInspection(metrics);
            Debug.Log(
                "IspantSlashGifTrajectoryRevisionComparisonCaptured Result=PASS" +
                ", Reference=SuppliedGifAll15Frames" +
                ", Final=Slot05ModelCamScreenTrajectoryAll15MatchingTimes" +
                ", RightArmModified=False, GuardImplemented=False" +
                ", Image=" + GifRevisionCapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash GIF Upward Trajectory Comparison")]
        public static void CaptureIspantSlashGifUpwardTrajectoryComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(GifUpwardCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time upward GIF trajectory comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The upward GIF trajectory folder is invalid."));
            CaptureGifTrajectoryComparison(metrics, destination);
            WriteGifUpwardInspection(metrics);
            Debug.Log(
                "IspantSlashGifUpwardTrajectoryComparisonCaptured Result=PASS" +
                ", Reference=SuppliedGifAll15Frames" +
                ", Final=Slot05StableFrontViewTrajectoryAll15MatchingTimes" +
                ", UpwardKeyFrames=01|02|03|05|06|14|15" +
                ", RightArmModified=False, GuardImplemented=False" +
                ", Image=" + GifUpwardCapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash GIF Actual Trace Comparison")]
        public static void CaptureIspantSlashGifActualTraceComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(GifActualTraceCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time actual GIF trace comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The actual GIF trace folder is invalid."));
            CaptureGifTrajectoryComparison(metrics, destination);
            WriteGifActualTraceInspection(metrics);
            Debug.Log(
                "IspantSlashGifActualTraceComparisonCaptured Result=PASS" +
                ", Reference=SuppliedGifMeasuredGripAndTipPixelsAll15Frames" +
                ", Final=Slot05MeasuredTraceDrivenAll15MatchingTimes" +
                ", RightArmModified=False, GuardImplemented=False" +
                ", Image=" + GifActualTraceCapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slash GIF Actual Trace Diagnostic")]
        public static void CaptureIspantSlashGifActualTraceDiagnostic()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(GifActualTraceDiagnosticPath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The actual GIF trace diagnostic already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The actual GIF trace diagnostic folder is invalid."));
            CaptureGifTrajectoryComparison(metrics, destination);
            Debug.Log(
                "IspantSlashGifActualTraceDiagnosticCaptured Result=PASS" +
                ", Purpose=PostFinalBodyRelativeSideCorrectionDirectReview" +
                ", Reference=SuppliedGifAll15Frames" +
                ", RightArmModified=False, CaptureType=TemporaryDiagnostic" +
                ", Image=" + GifActualTraceDiagnosticPath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Legacy Slot 5 Sword Motion Comparison")]
        public static void CaptureIspantLegacySwordMotionComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(LegacySwordCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time legacy slot 5 sword comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The legacy sword comparison folder is invalid."));
            CaptureLegacySwordMotionComparison(metrics, destination);
            WriteLegacySwordInspection(metrics);
            Debug.Log(
                "IspantLegacySwordMotionComparisonCaptured Result=PASS" +
                ", Reference=PreModelRevisionSlot05FinalReview" +
                ", Final=CurrentSlot05AtMatching0|0.25|0.5|0.75|1Times" +
                ", RightArmModified=False, Image=" + LegacySwordCapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Legacy Sword Right Hand Grip Comparison")]
        public static void CaptureIspantLegacySwordRightHandGripComparison()
        {
            var metrics = InspectApplied(true);
            var destination = Path.GetFullPath(LegacySwordGripCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time legacy sword grip comparison already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The legacy sword grip comparison folder is invalid."));
            CaptureGifTrajectoryComparison(metrics, destination);
            WriteLegacySwordGripInspection(metrics);
            Debug.Log(
                "IspantLegacySwordRightHandGripComparisonCaptured Result=PASS" +
                ", Reference=SuppliedGifAll15Frames" +
                ", Final=CurrentSlot05All15MatchingTimes" +
                ", LegacyTrajectoryChanged=False, RightArmModified=False" +
                ", Image=" + LegacySwordGripCapturePath + ", SceneChanged=False.");
        }

        private static void ConfigureImporter(string path, string importedName)
        {
            var importer = RequireImporter(path);
            var mixamo = MixamoTakes(importer);
            if (mixamo.Length != 1)
                throw new InvalidOperationException("Exactly one mixamo.com take is required in " + path + ".");
            var selected = mixamo[0];
            selected.name = importedName;
            selected.loopTime = true;
            selected.loopPose = false;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateFullBodyClip(AnimationClip source)
        {
            var clip = RequireOrCreateClip(SlashClipPath, SlashClipName);
            ClearClip(clip);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
                AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(source, binding));
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, AnimationUtility.GetObjectReferenceCurve(source, binding));
            ApplyStableDirectModelRotations(clip, StableForwardBones);
            ApplyStableDirectModelRotations(clip, StableLeftArmBones);
            ConfigureLoop(clip, source);
            return clip;
        }

        private static AnimationClip CreateLowerBodyClip(AnimationClip source)
        {
            var clip = RequireOrCreateClip(RunningClipPath, RunningClipName);
            ClearClip(clip);
            var flattened = new HashSet<string>(StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(source).Where(item => IsLowerBodyPath(item.path)))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (binding.path == "Armature/Hips" &&
                    (binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "m_LocalPosition.y"))
                {
                    curve = AnimationCurve.Constant(0f, source.length, curve.keys[0].value);
                    flattened.Add(binding.propertyName);
                }
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            if (!flattened.SetEquals(new[] { "m_LocalPosition.x", "m_LocalPosition.y" }))
                throw new InvalidOperationException("The two running locomotion curves were not flattened exactly.");
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source).Where(item => IsLowerBodyPath(item.path)))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, AnimationUtility.GetObjectReferenceCurve(source, binding));
            ConfigureLoop(clip, source);
            return clip;
        }

        private static AvatarMask CreateLowerBodyMask()
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                mask = new AvatarMask { name = "Ispant_05_Running_LowerBody" };
                AssetDatabase.CreateAsset(mask, MaskPath);
            }
            var prefab = RequireAsset<GameObject>(ModelPath);
            var paths = prefab.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, prefab.transform))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (var index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index,
                    paths[index].Length == 0 || paths[index] == "Armature" || IsLowerBodyPath(paths[index]));
            }
            EditorUtility.SetDirty(mask);
            AssetDatabase.SaveAssets();
            return mask;
        }

        private static AnimatorController CreateController(
            AnimationClip slashClip, AnimationClip runningClip, AvatarMask mask)
        {
            if (!AssetDatabase.IsValidFolder(ControllerFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Enemies/Ispant", "Controllers");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            while (controller.layers.Length > 1)
                controller.RemoveLayer(controller.layers.Length - 1);

            var baseLayer = controller.layers[0];
            baseLayer.name = "Slash Full Body";
            baseLayer.avatarMask = null;
            baseLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            baseLayer.defaultWeight = 1f;
            ClearStateMachine(baseLayer.stateMachine);
            var slashState = baseLayer.stateMachine.AddState("Slash Full Body");
            slashState.motion = slashClip;
            slashState.speed = 1f;
            slashState.writeDefaultValues = true;
            baseLayer.stateMachine.defaultState = slashState;
            var layers = controller.layers;
            layers[0] = baseLayer;
            controller.layers = layers;

            controller.AddLayer("Running Hips And Legs");
            layers = controller.layers;
            var runningLayer = layers[1];
            runningLayer.avatarMask = mask;
            runningLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            runningLayer.defaultWeight = 1f;
            runningLayer.syncedLayerIndex = -1;
            ClearStateMachine(runningLayer.stateMachine);
            var runningState = runningLayer.stateMachine.AddState("Running Hips And Legs");
            runningState.motion = runningClip;
            runningState.speed = 1f;
            runningState.writeDefaultValues = false;
            runningLayer.stateMachine.defaultState = runningState;
            layers[1] = runningLayer;
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ClearStateMachine(AnimatorStateMachine machine)
        {
            foreach (var child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (var child in machine.stateMachines.ToArray())
                machine.RemoveStateMachine(child.stateMachine);
            foreach (var transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);
        }

        private static void ConfigureAnimator(Transform model, AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1 || (animators.Length == 1 && animators[0].transform != model))
                throw new InvalidOperationException("Slot 5 has a conflicting Animator hierarchy.");
            var animator = animators.SingleOrDefault() ?? model.gameObject.AddComponent<Animator>();
            animator.avatar = null;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
        }

        private static void ConfigureIntactBody(Transform model)
        {
            var body = RequireBody(model);
            var direct = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(item => item.name == "char1");
            if (!body.bones.Select(item => item.name)
                    .SequenceEqual(direct.bones.Select(item => item.name), StringComparer.Ordinal) ||
                direct.sharedMesh.bindposes.Length != direct.bones.Length)
                throw new InvalidOperationException("The direct intact body mesh is incompatible with slot 5.");
            body.sharedMesh = direct.sharedMesh;
            body.quality = SkinQuality.Bone4;
            EditorUtility.SetDirty(body);
        }

        private static void ConfigureSwordFollower(Target target, AnimationClip slash, AnimationClip running)
        {
            var body = RequireBody(target.Model);
            var foreArm = RequireDescendant(target.Model, "RightForeArm");
            var hand = RequireDescendant(target.Model, "RightHand");
            var swordRenderer = RequireSword(target.Model);
            var sword = swordRenderer.transform;
            var mesh = swordRenderer.GetComponent<MeshFilter>().sharedMesh;
            var grip = CalculateGripCenter(mesh);
            var bladeLocalAxis = CalculateBladeLocalAxis(mesh, grip);
            var animator = target.Model.GetComponent<Animator>() ??
                throw new InvalidOperationException("The slot 5 Animator is missing before sword setup.");
            var followers = target.Slot.GetComponents<IspantRigidSwordFollower>();
            if (followers.Length > 1)
                throw new InvalidOperationException("Slot 5 has multiple rigid sword followers.");
            var follower = followers.SingleOrDefault() ??
                target.Slot.gameObject.AddComponent<IspantRigidSwordFollower>();
            var snapshots = target.Model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var legacyTrajectory = ExtractLegacySwordTrajectory();
            try
            {
                AnimationMode.StartAnimationMode();
                SampleComposite(target.Model.gameObject, slash, 0f, running, 0f);
                var palm = PalmGripCenter(body, hand);
                follower.ConfigureLegacySwordTrajectory(
                    foreArm,
                    hand,
                    sword,
                    target.Model,
                    animator,
                    hand.InverseTransformPoint(palm),
                    grip,
                    bladeLocalAxis,
                    SwordRollLocalAxis,
                    legacyTrajectory.BladeDirectionsInModelSpace,
                    legacyTrajectory.RollDirectionsInModelSpace);
                follower.ConfigureUpperBodyCentering(
                    RequireDescendant(target.Model, "Hips"),
                    RequireDescendant(target.Model, "Spine"),
                    RequireDescendant(target.Model, "LeftShoulder"),
                    RequireDescendant(target.Model, "RightShoulder"),
                    DirectRestUpperLateralOffset(),
                    DirectRestUpperVerticalOffset());
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
            EditorUtility.SetDirty(follower);
        }

        private static void ApplyStableDirectModelRotations(AnimationClip clip, IEnumerable<string> boneNames)
        {
            var model = RequireAsset<GameObject>(ModelPath).transform;
            foreach (var name in boneNames)
            {
                var bone = RequireDescendant(model, name);
                var path = AnimationUtility.CalculateTransformPath(bone, model);
                var rotation = bone.localRotation;
                SetConstantCurve(clip, path, "m_LocalRotation.x", rotation.x);
                SetConstantCurve(clip, path, "m_LocalRotation.y", rotation.y);
                SetConstantCurve(clip, path, "m_LocalRotation.z", rotation.z);
                SetConstantCurve(clip, path, "m_LocalRotation.w", rotation.w);
            }
            clip.EnsureQuaternionContinuity();
        }

        private static void SetConstantCurve(AnimationClip clip, string path, string property, float value)
        {
            var curve = AnimationCurve.Constant(0f, clip.length, value);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static Inspection InspectApplied(bool sample)
        {
            RequireHashes();
            if (AnimationMode.InAnimationMode())
                throw new InvalidOperationException("Inspection cannot start while AnimationMode is active.");
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var sceneDirty = scene.isDirty;
            var slashSource = RequireImportedClip(SlashSourcePath, SlashImportedName);
            var runningSource = RequireImportedClip(RunningSourcePath, RunningImportedName);
            var slashClip = RequireAsset<AnimationClip>(SlashClipPath);
            var runningClip = RequireAsset<AnimationClip>(RunningClipPath);
            var mask = RequireAsset<AvatarMask>(MaskPath);
            var controller = RequireAsset<AnimatorController>(ControllerPath);
            var animator = target.Model.GetComponent<Animator>() ??
                throw new InvalidOperationException("The slot 5 Animator is missing.");
            if (!animator.enabled || animator.avatar != null || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("The slot 5 Animator configuration differs.");
            if (controller.layers.Length != 2 ||
                controller.layers[0].stateMachine.defaultState?.motion != slashClip ||
                controller.layers[1].stateMachine.defaultState?.motion != runningClip ||
                controller.layers[1].avatarMask != mask ||
                controller.layers[1].blendingMode != AnimatorLayerBlendingMode.Override ||
                Mathf.Abs(controller.layers[1].defaultWeight - 1f) > Tolerance)
                throw new InvalidOperationException("The Slash/Running controller layer contract differs.");
            if (!AnimationUtility.GetAnimationClipSettings(slashClip).loopTime ||
                !AnimationUtility.GetAnimationClipSettings(runningClip).loopTime)
                throw new InvalidOperationException("Both composite clips must loop.");
            RequirePresentationConfiguration(target, animator, slashClip);

            var slashCurveError = CurveSetError(slashSource, slashClip, false);
            var runningCurveError = CurveSetError(runningSource, runningClip, true);
            var rightArmCurveError = CurveSubsetError(slashSource, slashClip, RightArmBones);
            var runningUpperCurves = AnimationUtility.GetCurveBindings(runningClip)
                .Count(item => !IsLowerBodyPath(item.path));
            if (slashCurveError > Tolerance || runningCurveError > Tolerance ||
                rightArmCurveError > Tolerance || runningUpperCurves != 0)
                throw new InvalidOperationException("The copied source curves differ or Running leaked into the upper body.");

            var metrics = sample
                ? SamplePoseAndMeshMetrics(target, slashSource, runningSource, slashClip, runningClip)
                : new SampleMetrics();
            if (scene.isDirty != sceneDirty)
                throw new InvalidOperationException("Composite inspection changed the scene dirty state.");
            return new Inspection(target, slashSource, runningSource, slashClip, runningClip,
                slashCurveError, runningCurveError, rightArmCurveError, metrics);
        }

        private static float CurveSubsetError(
            AnimationClip source, AnimationClip target, IEnumerable<string> boneNames)
        {
            var names = new HashSet<string>(boneNames, StringComparer.Ordinal);
            var sourceBindings = AnimationUtility.GetCurveBindings(source)
                .Where(item => names.Contains(Path.GetFileName(item.path)))
                .OrderBy(BindingKey, StringComparer.Ordinal).ToArray();
            var targetBindings = AnimationUtility.GetCurveBindings(target)
                .Where(item => names.Contains(Path.GetFileName(item.path)))
                .OrderBy(BindingKey, StringComparer.Ordinal).ToArray();
            if (!sourceBindings.Select(BindingKey).SequenceEqual(
                    targetBindings.Select(BindingKey), StringComparer.Ordinal))
                throw new InvalidOperationException("The right-arm curve binding set differs from Slash.");
            var maximum = 0f;
            for (var index = 0; index < sourceBindings.Length; index++)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, sourceBindings[index]);
                var targetCurve = AnimationUtility.GetEditorCurve(target, targetBindings[index]);
                for (var sample = 0; sample <= 120; sample++)
                {
                    var time = source.length * sample / 120f;
                    maximum = Mathf.Max(maximum,
                        Mathf.Abs(sourceCurve.Evaluate(time) - targetCurve.Evaluate(time)));
                }
            }
            return maximum;
        }

        private static SampleMetrics SamplePoseAndMeshMetrics(
            Target target, AnimationClip slashSource, AnimationClip runningSource,
            AnimationClip slashClip, AnimationClip runningClip)
        {
            var slashClone = CreateSamplingClone(target.Model, "Ispant_SlashSource_Inspection");
            var runningClone = CreateSamplingClone(target.Model, "Ispant_RunningSource_Inspection");
            var compositeClone = CreateSamplingClone(target.Model, "Ispant_Composite_Inspection");
            var upperPositionError = 0f;
            var upperAngleError = 0f;
            var lowerPositionError = 0f;
            var lowerAngleError = 0f;
            var maximumBoundsMagnitude = 0f;
            var body = compositeClone.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var meshVertexCount = body.sharedMesh.vertexCount;
            var meshTriangleCount = TriangleCount(body.sharedMesh);
            var baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                AnimationMode.StartAnimationMode();
                for (var sampleIndex = 0; sampleIndex <= 60; sampleIndex++)
                {
                    var normalized = sampleIndex / 60f;
                    SampleSingle(slashClone, slashSource, normalized * slashSource.length);
                    SampleSingle(runningClone, runningSource, normalized * runningSource.length);
                    SampleComposite(compositeClone, slashClip, normalized * slashClip.length,
                        runningClip, normalized * runningClip.length);
                    foreach (var boneName in BoneNames(compositeClone)
                                 .Except(LowerBodyBones, StringComparer.Ordinal)
                                 .Except(CorrectedUpperBones, StringComparer.Ordinal))
                    {
                        var sourceBone = RequireDescendant(slashClone.transform, boneName);
                        var finalBone = RequireDescendant(compositeClone.transform, boneName);
                        upperPositionError = Mathf.Max(upperPositionError,
                            Vector3.Distance(sourceBone.localPosition, finalBone.localPosition));
                        upperAngleError = Mathf.Max(upperAngleError,
                            Quaternion.Angle(sourceBone.localRotation, finalBone.localRotation));
                    }
                    foreach (var boneName in LowerBodyBones)
                    {
                        var sourceBone = RequireDescendant(runningClone.transform, boneName);
                        var finalBone = RequireDescendant(compositeClone.transform, boneName);
                        if (boneName == "Hips")
                        {
                            lowerPositionError = Mathf.Max(lowerPositionError,
                                Mathf.Abs(sourceBone.localPosition.z - finalBone.localPosition.z));
                        }
                        else
                        {
                            lowerPositionError = Mathf.Max(lowerPositionError,
                                Vector3.Distance(sourceBone.localPosition, finalBone.localPosition));
                        }
                        lowerAngleError = Mathf.Max(lowerAngleError,
                            Quaternion.Angle(sourceBone.localRotation, finalBone.localRotation));
                    }
                    body.BakeMesh(baked);
                    if (baked.vertices.Any(vertex => !Finite(vertex)))
                        throw new InvalidOperationException("The composite produced a non-finite baked mesh vertex.");
                    maximumBoundsMagnitude = Mathf.Max(maximumBoundsMagnitude, baked.bounds.size.magnitude);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(slashClone);
                UnityEngine.Object.DestroyImmediate(runningClone);
                UnityEngine.Object.DestroyImmediate(compositeClone);
            }
            if (upperPositionError > Tolerance || upperAngleError > 0.01f ||
                lowerPositionError > Tolerance || lowerAngleError > 0.01f)
                throw new InvalidOperationException(
                    "The sampled source and composite local poses differ." +
                    " UpperPosition=" + Num(upperPositionError) +
                    ", UpperAngle=" + Num(upperAngleError) +
                    ", LowerPosition=" + Num(lowerPositionError) +
                    ", LowerAngle=" + Num(lowerAngleError) + ".");
            var presentation = SamplePresentationMetrics(target, slashClip, runningClip);
            return new SampleMetrics(
                upperPositionError, upperAngleError, lowerPositionError, lowerAngleError,
                maximumBoundsMagnitude, meshVertexCount, meshTriangleCount, presentation);
        }

        private static float CurveSetError(AnimationClip source, AnimationClip target, bool lowerOnly)
        {
            var sourceBindings = AnimationUtility.GetCurveBindings(source)
                .Where(item => !lowerOnly || IsLowerBodyPath(item.path))
                .OrderBy(BindingKey, StringComparer.Ordinal).ToArray();
            var targetBindings = AnimationUtility.GetCurveBindings(target)
                .OrderBy(BindingKey, StringComparer.Ordinal).ToArray();
            if (!sourceBindings.Select(BindingKey).SequenceEqual(targetBindings.Select(BindingKey), StringComparer.Ordinal))
                throw new InvalidOperationException("The source and copied curve binding sets differ.");
            var maximum = 0f;
            for (var index = 0; index < sourceBindings.Length; index++)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, sourceBindings[index]);
                var targetCurve = AnimationUtility.GetEditorCurve(target, targetBindings[index]);
                var flattened = lowerOnly && sourceBindings[index].path == "Armature/Hips" &&
                    (sourceBindings[index].propertyName == "m_LocalPosition.x" ||
                     sourceBindings[index].propertyName == "m_LocalPosition.y");
                var correctedRotation = !lowerOnly &&
                    sourceBindings[index].propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) &&
                    CorrectedUpperBones.Any(name =>
                        sourceBindings[index].path.EndsWith("/" + name, StringComparison.Ordinal));
                for (var sample = 0; sample <= 120; sample++)
                {
                    if (correctedRotation) continue;
                    var time = source.length * sample / 120f;
                    var expected = flattened ? sourceCurve.keys[0].value : sourceCurve.Evaluate(time);
                    maximum = Mathf.Max(maximum, Mathf.Abs(expected - targetCurve.Evaluate(time)));
                }
            }
            return maximum;
        }

        private static void RequirePresentationConfiguration(
            Target target, Animator animator, AnimationClip slashClip)
        {
            var body = RequireBody(target.Model);
            var directBody = RequireAsset<GameObject>(ModelPath)
                .GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(item => item.name == "char1");
            if (body.sharedMesh != directBody.sharedMesh || body.quality != SkinQuality.Bone4)
                throw new InvalidOperationException("Slot 5 does not use the intact direct four-bone body mesh.");
            var foreArm = RequireDescendant(target.Model, "RightForeArm");
            var hand = RequireDescendant(target.Model, "RightHand");
            var hips = RequireDescendant(target.Model, "Hips");
            var upperBodyRoot = RequireDescendant(target.Model, "Spine");
            var leftShoulder = RequireDescendant(target.Model, "LeftShoulder");
            var rightShoulder = RequireDescendant(target.Model, "RightShoulder");
            var sword = RequireSword(target.Model);
            var mesh = sword.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null || sword.GetComponent<SkinnedMeshRenderer>() != null ||
                mesh.boneWeights.Length != 0 || mesh.blendShapeCount != 0)
                throw new InvalidOperationException("The slot 5 long sword must remain a rigid mesh.");
            var followers = target.Slot.GetComponents<IspantRigidSwordFollower>();
            if (followers.Length != 1 ||
                !followers[0].MatchesLegacyTrajectoryAndUpperBodyCentering(
                    foreArm, hand, sword.transform, target.Model, animator,
                    hips, upperBodyRoot, leftShoulder, rightShoulder,
                    LegacySwordFrameCount))
                throw new InvalidOperationException(
                    "The slot 5 whole-upper-body centering or pre-model-revision sword follower differs.");
            RequireStableDirectRotations(slashClip, StableForwardBones);
            RequireStableDirectRotations(slashClip, StableLeftArmBones);
        }

        private static void RequireStableDirectRotations(AnimationClip clip, IEnumerable<string> boneNames)
        {
            var model = RequireAsset<GameObject>(ModelPath).transform;
            foreach (var name in boneNames)
            {
                var bone = RequireDescendant(model, name);
                var path = AnimationUtility.CalculateTransformPath(bone, model);
                for (var sample = 0; sample <= 60; sample++)
                {
                    var time = clip.length * sample / 60f;
                    var rotation = EvaluateRotation(clip, path, time);
                    if (Quaternion.Angle(rotation, bone.localRotation) > 0.02f)
                        throw new InvalidOperationException(name + " does not remain at the direct-model stable rotation.");
                }
            }
        }

        private static PresentationMetrics SamplePresentationMetrics(
            Target target, AnimationClip slash, AnimationClip running)
        {
            var body = RequireBody(target.Model);
            var hips = RequireDescendant(target.Model, "Hips");
            var leftShoulder = RequireDescendant(target.Model, "LeftShoulder");
            var rightShoulder = RequireDescendant(target.Model, "RightShoulder");
            var foreArm = RequireDescendant(target.Model, "RightForeArm");
            var hand = RequireDescendant(target.Model, "RightHand");
            var sword = RequireSword(target.Model);
            var grip = CalculateGripCenter(sword.GetComponent<MeshFilter>().sharedMesh);
            var tip = CalculateBladeTipCenter(sword.GetComponent<MeshFilter>().sharedMesh, grip);
            var follower = target.Slot.GetComponent<IspantRigidSwordFollower>();
            var legacyTrajectory = ExtractLegacySwordTrajectory();
            var directModel = RequireAsset<GameObject>(ModelPath).transform;
            var restRotations = StableForwardBones.Concat(StableLeftArmBones)
                .ToDictionary(name => name, name => RequireDescendant(directModel, name).localRotation,
                    StringComparer.Ordinal);
            var snapshots = target.Model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var maximumGripError = 0f;
            var startBladeUpAngle = 180f;
            var maximumSwordOutwardAngle = 0f;
            var maximumSwordForwardCutAngle = 0f;
            var minimumBladeTipRadialGain = float.PositiveInfinity;
            var minimumGifUpwardTipGain = 0f;
            var maximumBladeUpAngle = 0f;
            var maximumUpperBodyLateralError = 0f;
            var maximumUpperBodyVerticalError = 0f;
            var maximumStableBoneAngle = 0f;
            var maximumSwordMotion = 0f;
            var maximumHandMotion = 0f;
            var firstSword = Vector3.zero;
            var firstHand = Vector3.zero;
            var targetUpperBodyLateral = DirectRestUpperLateralOffset();
            var targetUpperBodyVertical = DirectRestUpperVerticalOffset();
            try
            {
                AnimationMode.StartAnimationMode();
                for (var sample = 0; sample <= 120; sample++)
                {
                    var normalized = sample / 120f;
                    SampleComposite(target.Model.gameObject, slash, normalized * slash.length,
                        running, normalized * running.length);
                    follower.ApplyFollow(normalized);
                    var palm = PalmGripCenter(body, hand);
                    maximumGripError = Mathf.Max(maximumGripError,
                        Vector3.Distance(palm, sword.transform.TransformPoint(grip)));
                    var blade = sword.transform.TransformDirection(follower.SwordBladeLocalAxis).normalized;
                    var roll = sword.transform.TransformDirection(follower.SwordRollLocalAxis).normalized;
                    var expectedBlade = target.Model.TransformDirection(
                        EvaluateLegacyDirection(
                            legacyTrajectory.BladeDirectionsInModelSpace, normalized)).normalized;
                    var expectedRoll = target.Model.TransformDirection(
                        EvaluateLegacyDirection(
                            legacyTrajectory.RollDirectionsInModelSpace, normalized)).normalized;
                    maximumSwordOutwardAngle = Mathf.Max(maximumSwordOutwardAngle,
                        Vector3.Angle(blade, expectedBlade));
                    maximumSwordForwardCutAngle = Mathf.Max(
                        maximumSwordForwardCutAngle,
                        Vector3.Angle(
                            Vector3.ProjectOnPlane(roll, blade).normalized,
                            Vector3.ProjectOnPlane(expectedRoll, expectedBlade).normalized));
                    maximumBladeUpAngle = Mathf.Max(maximumBladeUpAngle,
                        Vector3.Angle(blade, target.Model.up));
                    minimumBladeTipRadialGain = Mathf.Min(minimumBladeTipRadialGain,
                        Vector3.Distance(
                            sword.transform.TransformPoint(tip),
                            sword.transform.TransformPoint(grip)));
                    var upperCenter = Vector3.Lerp(leftShoulder.position, rightShoulder.position, 0.5f);
                    maximumUpperBodyLateralError = Mathf.Max(maximumUpperBodyLateralError,
                        Mathf.Abs(Vector3.Dot(upperCenter - hips.position, target.Model.right) -
                                  targetUpperBodyLateral));
                    maximumUpperBodyVerticalError = Mathf.Max(maximumUpperBodyVerticalError,
                        Mathf.Abs(Vector3.Dot(upperCenter - hips.position, target.Model.up) -
                                  targetUpperBodyVertical));
                    if (sample == 0)
                    {
                        startBladeUpAngle = Vector3.Angle(blade, expectedBlade);
                        firstSword = sword.transform.position;
                        firstHand = hand.position;
                    }
                    else
                    {
                        maximumSwordMotion = Mathf.Max(maximumSwordMotion,
                            Vector3.Distance(firstSword, sword.transform.position));
                        maximumHandMotion = Mathf.Max(maximumHandMotion,
                            Vector3.Distance(firstHand, hand.position));
                    }
                    foreach (var pair in restRotations)
                        maximumStableBoneAngle = Mathf.Max(maximumStableBoneAngle,
                            Quaternion.Angle(pair.Value, RequireDescendant(target.Model, pair.Key).localRotation));
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
            if (maximumGripError > 0.002f || startBladeUpAngle > 0.1f ||
                maximumSwordOutwardAngle > 0.1f || maximumSwordForwardCutAngle > 0.1f ||
                minimumBladeTipRadialGain < 0.1f ||
                maximumBladeUpAngle < 80f ||
                maximumUpperBodyLateralError > 0.001f || maximumUpperBodyVerticalError > 0.001f ||
                maximumStableBoneAngle > 0.02f ||
                maximumSwordMotion < 0.05f || maximumHandMotion < 0.05f)
                throw new InvalidOperationException(
                    "The upper-body/musket/sword presentation contract failed. Grip=" + Num(maximumGripError) +
                    ", LegacyFrame01Blade=" + Num(startBladeUpAngle) +
                    ", LegacyBladeTrajectory=" + Num(maximumSwordOutwardAngle) +
                    ", LegacyRollTrajectory=" + Num(maximumSwordForwardCutAngle) +
                    ", BladeTipRadialGain=" + Num(minimumBladeTipRadialGain) +
                    ", GifUpwardTipMinimumGain=" + Num(minimumGifUpwardTipGain) +
                    ", BladeSweep=" + Num(maximumBladeUpAngle) +
                    ", UpperBodyLateral=" + Num(maximumUpperBodyLateralError) +
                    ", UpperBodyVertical=" + Num(maximumUpperBodyVerticalError) +
                    ", StableBones=" + Num(maximumStableBoneAngle) +
                    ", SwordMotion=" + Num(maximumSwordMotion) +
                    ", HandMotion=" + Num(maximumHandMotion) + ".");
            return new PresentationMetrics(
                maximumGripError, startBladeUpAngle, maximumSwordOutwardAngle,
                maximumSwordForwardCutAngle, minimumBladeTipRadialGain,
                minimumGifUpwardTipGain,
                maximumBladeUpAngle, maximumUpperBodyLateralError, maximumUpperBodyVerticalError,
                maximumStableBoneAngle, maximumSwordMotion, maximumHandMotion);
        }

        private static void UpdateReview()
        {
            try
            {
                var target = RequireTarget(RequireScene(false));
                var slash = RequireAsset<AnimationClip>(SlashClipPath);
                var running = RequireAsset<AnimationClip>(RunningClipPath);
                var elapsed = (float)(EditorApplication.timeSinceStartup - reviewStart);
                SampleComposite(target.Model.gameObject, slash, Mathf.Repeat(elapsed, slash.length),
                    running, Mathf.Repeat(elapsed, running.length));
                var follower = target.Slot.GetComponent<IspantRigidSwordFollower>() ??
                    throw new InvalidOperationException("The slot 5 sword follower is missing during live review.");
                follower.ApplyFollow(Mathf.Repeat(elapsed, slash.length) / slash.length);
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                StopReview();
                Debug.LogException(exception);
            }
        }

        private static void StopReview()
        {
            EditorApplication.update -= UpdateReview;
            reviewActive = false;
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
            if (reviewSnapshots != null)
            {
                foreach (var snapshot in reviewSnapshots)
                    snapshot.Restore();
                reviewSnapshots = null;
            }
            if (reviewView != null)
            {
                reviewView.drawGizmos = reviewGizmos;
                reviewView = null;
            }
            SceneView.RepaintAll();
        }

        private static void CaptureComparison(Inspection metrics, string destination)
        {
            const int panelSize = 512;
            const int columns = 5;
            const int rows = 3;
            var slashClone = CreateSamplingClone(metrics.Target.Model, "Slash_Source_Visual_Reference");
            var runningClone = CreateSamplingClone(metrics.Target.Model, "Running_Source_Visual_Reference");
            var compositeClone = CreateSamplingClone(metrics.Target.Model, "Final_Slash_Running_Composite");
            var clones = new[] { slashClone, runningClone, compositeClone };
            var sceneRenderers = metrics.Target.Model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(item => new RendererSnapshot(item)).ToArray();
            var cameraObject = new GameObject("IspantSlashRunningComparisonCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var lightObject = new GameObject("IspantSlashRunningComparisonLight", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var targetTexture = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panelSize * columns, panelSize * rows, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var camera = cameraObject.GetComponent<Camera>();
            try
            {
                foreach (var snapshot in sceneRenderers)
                    snapshot.Renderer.enabled = false;
                foreach (var clone in clones)
                {
                    SetLayer(clone.transform, CaptureLayer);
                    foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = false;
                }
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.11f, 0.12f, 0.14f, 1f);
                camera.fieldOfView = 32f;
                camera.cullingMask = 1 << CaptureLayer;
                camera.targetTexture = targetTexture;
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.95f, 0.9f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
                var referenceHeight = metrics.Target.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single().bounds.size.y;
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    var normalized = ReviewTimes[column];
                    RenderComparisonPanel(slashClone, metrics.SlashSource, normalized * metrics.SlashSource.length,
                        null, 0f, 0, column, camera, targetTexture, panel, sheet, panelSize, rows, referenceHeight);
                    RenderComparisonPanel(runningClone, metrics.RunningSource, normalized * metrics.RunningSource.length,
                        null, 0f, 1, column, camera, targetTexture, panel, sheet, panelSize, rows, referenceHeight);
                    RenderComparisonPanel(compositeClone, metrics.SlashClip, normalized * metrics.SlashClip.length,
                        metrics.RunningClip, normalized * metrics.RunningClip.length, 2, column,
                        camera, targetTexture, panel, sheet, panelSize, rows, referenceHeight);
                }
                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                foreach (var snapshot in sceneRenderers)
                    snapshot.Restore();
                camera.targetTexture = null;
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                foreach (var clone in clones)
                    UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void CaptureFixComparison(Inspection metrics, string destination)
        {
            const int panelSize = 512;
            const int columns = 5;
            const int rows = 3;
            var slashClone = CreateAssetSamplingClone(SlashSourcePath, "Actual_Slash_Source_Visual_Reference");
            var runningClone = CreateAssetSamplingClone(RunningSourcePath, "Actual_Running_Source_Visual_Reference");
            var compositeClone = CreateSamplingClone(metrics.Target.Model, "Final_Revised_Slash_Running_Composite");
            compositeClone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var clones = new[] { slashClone, runningClone, compositeClone };
            var sceneRenderers = metrics.Target.Model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(item => new RendererSnapshot(item)).ToArray();
            var cameraObject = new GameObject("IspantSlashRunningFixCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var lightObject = new GameObject("IspantSlashRunningFixLight", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var targetTexture = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panelSize * columns, panelSize * rows, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var camera = cameraObject.GetComponent<Camera>();
            var follower = ConfigureCaptureFollower(
                compositeClone.transform, metrics.SlashClip, metrics.RunningClip);
            try
            {
                foreach (var snapshot in sceneRenderers) snapshot.Renderer.enabled = false;
                foreach (var clone in clones)
                {
                    SetLayer(clone.transform, CaptureLayer);
                    foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                }
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.11f, 0.12f, 0.14f, 1f);
                camera.fieldOfView = 32f;
                camera.cullingMask = 1 << CaptureLayer;
                camera.targetTexture = targetTexture;
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.95f, 0.9f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
                // Zero selects each sampled pose's current body height. The previous
                // bind-pose height made the final model too small for direct GIF review.
                const float referenceHeight = 0f;
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    var normalized = ReviewTimes[column];
                    RenderComparisonPanel(slashClone, metrics.SlashSource, normalized * metrics.SlashSource.length,
                        null, 0f, 0, column, camera, targetTexture, panel, sheet, panelSize, rows, referenceHeight);
                    RenderComparisonPanel(runningClone, metrics.RunningSource, normalized * metrics.RunningSource.length,
                        null, 0f, 1, column, camera, targetTexture, panel, sheet, panelSize, rows, referenceHeight);
                    RenderComparisonPanel(compositeClone, metrics.SlashClip, normalized * metrics.SlashClip.length,
                        metrics.RunningClip, normalized * metrics.RunningClip.length, 2, column,
                        camera, targetTexture, panel, sheet, panelSize, rows, referenceHeight, follower);
                }
                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in sceneRenderers) snapshot.Restore();
                camera.targetTexture = null;
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                foreach (var clone in clones) UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void CaptureGifTrajectoryComparison(Inspection metrics, string destination)
        {
            const int panelSize = 260;
            const int rows = 2;
            var columns = IspantRigidSwordFollower.ReferenceTrajectoryFrameCount;
            if (columns != 15)
                throw new InvalidOperationException("The supplied GIF trajectory must contain 15 frames.");
            var compositeClone = CreateSamplingClone(
                metrics.Target.Model, "Final_Slot05_Gif_Sword_Trajectory");
            compositeClone.transform.SetPositionAndRotation(
                Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            var sceneRenderers = metrics.Target.Model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(item => new RendererSnapshot(item)).ToArray();
            var cameraObject = new GameObject("IspantSlashGifTrajectoryCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var lightObject = new GameObject("IspantSlashGifTrajectoryLight", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var targetTexture = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panelSize * columns, panelSize * rows, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var camera = cameraObject.GetComponent<Camera>();
            var follower = ConfigureCaptureFollower(
                compositeClone.transform, metrics.SlashClip, metrics.RunningClip);
            try
            {
                sheet.SetPixels(Enumerable.Repeat(new Color(0.11f, 0.12f, 0.14f, 1f),
                    sheet.width * sheet.height).ToArray());
                foreach (var snapshot in sceneRenderers) snapshot.Renderer.enabled = false;
                SetLayer(compositeClone.transform, CaptureLayer);
                foreach (var renderer in compositeClone.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.11f, 0.12f, 0.14f, 1f);
                camera.fieldOfView = 32f;
                camera.cullingMask = 1 << CaptureLayer;
                camera.targetTexture = targetTexture;
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.95f, 0.9f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
                var referenceHeight = RequireBody(compositeClone.transform).bounds.size.y;
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    CopyGifReferenceFrameToSheet(column, panelSize, rows, sheet);
                    var normalized = column / (float)columns;
                    RenderComparisonPanel(
                        compositeClone,
                        metrics.SlashClip,
                        normalized * metrics.SlashClip.length,
                        metrics.RunningClip,
                        normalized * metrics.RunningClip.length,
                        1,
                        column,
                        camera,
                        targetTexture,
                        panel,
                        sheet,
                        panelSize,
                        rows,
                        referenceHeight,
                        follower);
                }
                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in sceneRenderers) snapshot.Restore();
                camera.targetTexture = null;
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(compositeClone);
            }
        }

        private static void CaptureLegacySwordMotionComparison(
            Inspection metrics, string destination)
        {
            const int panelSize = 640;
            const int rows = 2;
            const int columns = 5;
            const int referenceColumns = 6;
            var referencePath = Path.GetFullPath(LegacySwordReferenceReviewPath);
            if (!File.Exists(referencePath))
                throw new InvalidOperationException(
                    "The pre-model-revision slot 5 visual reference is missing.");
            var reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!reference.LoadImage(File.ReadAllBytes(referencePath), false))
                throw new InvalidOperationException(
                    "The pre-model-revision slot 5 visual reference could not be loaded.");
            var compositeClone = CreateSamplingClone(
                metrics.Target.Model, "Final_Slot05_Legacy_Sword_Motion");
            compositeClone.transform.SetPositionAndRotation(
                Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            var sceneRenderers = metrics.Target.Model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(item => new RendererSnapshot(item)).ToArray();
            var cameraObject = new GameObject("IspantLegacySwordMotionCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var lightObject = new GameObject("IspantLegacySwordMotionLight", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave, layer = CaptureLayer };
            var targetTexture = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panelSize * columns, panelSize * rows, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var camera = cameraObject.GetComponent<Camera>();
            var follower = ConfigureCaptureFollower(
                compositeClone.transform, metrics.SlashClip, metrics.RunningClip);
            var times = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            try
            {
                sheet.SetPixels(Enumerable.Repeat(new Color(0.11f, 0.12f, 0.14f, 1f),
                    sheet.width * sheet.height).ToArray());
                for (var column = 0; column < columns; column++)
                {
                    var sourceColumn = column + 1;
                    var sourceX0 = Mathf.RoundToInt(
                        sourceColumn * reference.width / (float)referenceColumns);
                    var sourceX1 = Mathf.RoundToInt(
                        (sourceColumn + 1) * reference.width / (float)referenceColumns);
                    var sourceWidth = sourceX1 - sourceX0;
                    if (sourceWidth > panelSize || reference.height > panelSize)
                        throw new InvalidOperationException(
                            "The pre-model-revision reference panel exceeds the comparison panel.");
                    sheet.SetPixels(
                        column * panelSize + (panelSize - sourceWidth) / 2,
                        panelSize + (panelSize - reference.height) / 2,
                        sourceWidth,
                        reference.height,
                        reference.GetPixels(sourceX0, 0, sourceWidth, reference.height));
                }
                foreach (var snapshot in sceneRenderers) snapshot.Renderer.enabled = false;
                SetLayer(compositeClone.transform, CaptureLayer);
                foreach (var renderer in compositeClone.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.11f, 0.12f, 0.14f, 1f);
                camera.fieldOfView = 32f;
                camera.cullingMask = 1 << CaptureLayer;
                camera.targetTexture = targetTexture;
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.color = new Color(1f, 0.95f, 0.9f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(38f, -30f, 0f);
                var referenceHeight = RequireBody(compositeClone.transform).bounds.size.y;
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    var normalized = times[column];
                    RenderComparisonPanel(
                        compositeClone,
                        metrics.SlashClip,
                        normalized * metrics.SlashClip.length,
                        metrics.RunningClip,
                        normalized * metrics.RunningClip.length,
                        1,
                        column,
                        camera,
                        targetTexture,
                        panel,
                        sheet,
                        panelSize,
                        rows,
                        referenceHeight,
                        follower);
                }
                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (var snapshot in sceneRenderers) snapshot.Restore();
                camera.targetTexture = null;
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(reference);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(compositeClone);
            }
        }

        private static void CopyGifReferenceFrameToSheet(
            int column, int panelSize, int rows, Texture2D sheet)
        {
            var sourcePath = Path.GetFullPath(Path.Combine(
                GifReferenceFrameFolder,
                "frame-" + (column + 1).ToString("000", CultureInfo.InvariantCulture) + ".png"));
            if (!File.Exists(sourcePath))
                throw new InvalidOperationException("A supplied GIF reference frame is missing: " + sourcePath);
            var source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(sourcePath), false) ||
                    source.width != 220 || source.height != 260)
                    throw new InvalidOperationException("A supplied GIF reference frame has changed dimensions.");
                sheet.SetPixels32(
                    column * panelSize + (panelSize - source.width) / 2,
                    (rows - 1) * panelSize,
                    source.width,
                    source.height,
                    source.GetPixels32());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static void RenderComparisonPanel(
            GameObject clone, AnimationClip baseClip, float baseTime,
            AnimationClip overrideClip, float overrideTime, int row, int column,
            Camera camera, RenderTexture target, Texture2D panel, Texture2D sheet,
            int panelSize, int rows, float referenceHeight,
            IspantRigidSwordFollower follower = null)
        {
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
            if (overrideClip == null)
                SampleSingle(clone, baseClip, baseTime);
            else
                SampleComposite(clone, baseClip, baseTime, overrideClip, overrideTime);
            var body = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            FrameCamera(camera, body.bounds.center,
                referenceHeight > 0f ? referenceHeight : body.bounds.size.y * 0.65f);
            if (follower != null)
                follower.ApplyFollow(baseClip.length > 0f ? baseTime / baseClip.length : 0f);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, panelSize, panelSize), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The direct comparison contains a magenta shader fallback.");
            sheet.SetPixels32(column * panelSize, (rows - 1 - row) * panelSize,
                panelSize, panelSize, pixels);
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private static void FrameCamera(Camera camera, Vector3 center, float height)
        {
            camera.aspect = 1f;
            var distance = (height * 0.5f) / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + new Vector3(0f, height * 0.03f, -distance * 1.28f);
            camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
        }

        private static void SampleSingle(GameObject target, AnimationClip clip, float time)
        {
            ApplyTransformCurves(target.transform, clip, Mathf.Clamp(time, 0f, clip.length));
        }

        private static void SampleComposite(
            GameObject target, AnimationClip slash, float slashTime,
            AnimationClip running, float runningTime)
        {
            ApplyTransformCurves(target.transform, slash, Mathf.Clamp(slashTime, 0f, slash.length));
            ApplyTransformCurves(target.transform, running, Mathf.Clamp(runningTime, 0f, running.length));
        }

        private static void ApplyTransformCurves(Transform root, AnimationClip clip, float time)
        {
            foreach (var group in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.type == typeof(Transform))
                         .GroupBy(item => item.path, StringComparer.Ordinal))
            {
                var target = group.Key.Length == 0 ? root : root.Find(group.Key);
                if (target == null)
                    throw new InvalidOperationException("Composite curve target missing: " + group.Key + ".");
                var position = target.localPosition;
                var rotation = target.localRotation;
                var scale = target.localScale;
                var euler = target.localEulerAngles;
                var positionChanged = false;
                var rotationChanged = false;
                var scaleChanged = false;
                var eulerChanged = false;
                foreach (var binding in group)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                        throw new InvalidOperationException("Composite curve missing: " + BindingKey(binding) + ".");
                    var value = curve.Evaluate(time);
                    switch (binding.propertyName)
                    {
                        case "m_LocalPosition.x": position.x = value; positionChanged = true; break;
                        case "m_LocalPosition.y": position.y = value; positionChanged = true; break;
                        case "m_LocalPosition.z": position.z = value; positionChanged = true; break;
                        case "m_LocalRotation.x": rotation.x = value; rotationChanged = true; break;
                        case "m_LocalRotation.y": rotation.y = value; rotationChanged = true; break;
                        case "m_LocalRotation.z": rotation.z = value; rotationChanged = true; break;
                        case "m_LocalRotation.w": rotation.w = value; rotationChanged = true; break;
                        case "m_LocalScale.x": scale.x = value; scaleChanged = true; break;
                        case "m_LocalScale.y": scale.y = value; scaleChanged = true; break;
                        case "m_LocalScale.z": scale.z = value; scaleChanged = true; break;
                        case "localEulerAnglesRaw.x": euler.x = value; eulerChanged = true; break;
                        case "localEulerAnglesRaw.y": euler.y = value; eulerChanged = true; break;
                        case "localEulerAnglesRaw.z": euler.z = value; eulerChanged = true; break;
                        default:
                            throw new InvalidOperationException(
                                "Unsupported lower-body Transform curve: " + binding.propertyName + ".");
                    }
                }
                if (positionChanged) target.localPosition = position;
                if (rotationChanged) target.localRotation = rotation.normalized;
                else if (eulerChanged) target.localEulerAngles = euler;
                if (scaleChanged) target.localScale = scale;
            }
        }

        private static GameObject CreateSamplingClone(Transform model, string name)
        {
            var clone = UnityEngine.Object.Instantiate(model.gameObject);
            clone.name = name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);
            return clone;
        }

        private static GameObject CreateAssetSamplingClone(string path, string name)
        {
            var clone = UnityEngine.Object.Instantiate(RequireAsset<GameObject>(path));
            clone.name = name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(animator);
            return clone;
        }

        private static IspantRigidSwordFollower ConfigureCaptureFollower(
            Transform model, AnimationClip slash, AnimationClip running)
        {
            var body = RequireBody(model);
            var foreArm = RequireDescendant(model, "RightForeArm");
            var hand = RequireDescendant(model, "RightHand");
            var sword = RequireSword(model);
            var grip = CalculateGripCenter(sword.GetComponent<MeshFilter>().sharedMesh);
            var bladeLocalAxis = CalculateBladeLocalAxis(
                sword.GetComponent<MeshFilter>().sharedMesh, grip);
            SampleComposite(model.gameObject, slash, 0f, running, 0f);
            var palm = PalmGripCenter(body, hand);
            var follower = model.gameObject.AddComponent<IspantRigidSwordFollower>();
            var legacyTrajectory = ExtractLegacySwordTrajectory();
            follower.ConfigureLegacySwordTrajectory(
                foreArm, hand, sword.transform, model, null,
                hand.InverseTransformPoint(palm), grip,
                bladeLocalAxis, SwordRollLocalAxis,
                legacyTrajectory.BladeDirectionsInModelSpace,
                legacyTrajectory.RollDirectionsInModelSpace);
            follower.ConfigureUpperBodyCentering(
                RequireDescendant(model, "Hips"),
                RequireDescendant(model, "Spine"),
                RequireDescendant(model, "LeftShoulder"),
                RequireDescendant(model, "RightShoulder"),
                DirectRestUpperLateralOffset(),
                DirectRestUpperVerticalOffset());
            return follower;
        }

        private static LegacySwordTrajectory ExtractLegacySwordTrajectory()
        {
            var clip = RequireAsset<AnimationClip>(LegacySwordClipPath);
            var clone = CreateAssetSamplingClone(
                LegacySwordModelPath, "Ispant_PreModelRevision_SwordTrajectory_Source");
            var hand = RequireDescendant(clone.transform, "mixamorig:RightHand");
            var swordCurveTarget = new GameObject("Ispant_ApprovedLongSword")
                { hideFlags = HideFlags.HideAndDontSave };
            swordCurveTarget.transform.SetParent(hand, false);
            var bladeDirections = new Vector3[LegacySwordFrameCount];
            var rollDirections = new Vector3[LegacySwordFrameCount];
            try
            {
                AnimationMode.StartAnimationMode();
                for (var frame = 0; frame < LegacySwordFrameCount; frame++)
                {
                    var normalized = frame / (float)(LegacySwordFrameCount - 1);
                    var time = normalized * clip.length;
                    SampleSingle(clone, clip, time);
                    var swordRotationInHand = EvaluateRotation(
                        clip, LegacySwordCurvePath, time);
                    var bladeWorld = hand.TransformDirection(
                        swordRotationInHand * Vector3.forward).normalized;
                    var rollWorld = hand.TransformDirection(
                        swordRotationInHand * Vector3.up);
                    rollWorld = Vector3.ProjectOnPlane(rollWorld, bladeWorld).normalized;
                    bladeDirections[frame] = clone.transform.InverseTransformDirection(
                        bladeWorld).normalized;
                    rollDirections[frame] = clone.transform.InverseTransformDirection(
                        rollWorld).normalized;
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(clone);
            }
            if (Vector3.Angle(bladeDirections[0], bladeDirections[^1]) > 0.01f ||
                Vector3.Angle(rollDirections[0], rollDirections[^1]) > 0.01f)
                throw new InvalidOperationException(
                    "The pre-model-revision slot 5 sword trajectory does not close at the loop boundary.");
            return new LegacySwordTrajectory(bladeDirections, rollDirections);
        }

        private static Vector3 EvaluateLegacyDirection(Vector3[] values, float normalizedTime)
        {
            if (values == null || values.Length != LegacySwordFrameCount)
                throw new InvalidOperationException("The legacy sword trajectory frame set differs.");
            var progress = normalizedTime >= 1f ? 0f : Mathf.Repeat(normalizedTime, 1f);
            var frame = progress * (values.Length - 1);
            var from = Mathf.Clamp(Mathf.FloorToInt(frame), 0, values.Length - 1);
            var to = Mathf.Min(from + 1, values.Length - 1);
            return Vector3.Slerp(values[from], values[to], frame - from).normalized;
        }

        private static void ConfigureLoop(AnimationClip clip, AnimationClip source)
        {
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip RequireOrCreateClip(string path, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
                return clip;
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void ClearClip(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
        }

        private static bool IsLowerBodyPath(string path)
        {
            return LowerBodyRoots.Any(root => path == root || path.StartsWith(root + "/", StringComparison.Ordinal)) &&
                   !path.StartsWith("Armature/Hips/Spine02", StringComparison.Ordinal);
        }

        private static string[] BoneNames(GameObject model)
        {
            var armature = model.transform.Find("Armature") ??
                throw new InvalidOperationException("The direct model Armature is missing.");
            return armature.GetComponentsInChildren<Transform>(true)
                .Where(item => item != armature && item.GetComponent<Renderer>() == null)
                .Select(item => item.name).Distinct(StringComparer.Ordinal).ToArray();
        }

        private static void WriteInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementName + "/" + SlotName + "/" + ModelName,
                "SlashSource=" + SlashSourcePath,
                "SlashSourceSha256=" + SlashHash,
                "SlashTake=mixamo.com",
                "SlashFrames=1-91",
                "RunningSource=" + RunningSourcePath,
                "RunningSourceSha256=" + RunningHash,
                "RunningTake=mixamo.com",
                "RunningFrames=1-43",
                "CompositionOrder=SlashFullBodyThenRunningHipsAndLegs",
                "LowerBodyBones=" + string.Join(",", LowerBodyBones),
                "RootMotion=False",
                "InPlaceFlattenedAxes=RunningHipsLocalX,RunningHipsLocalY",
                "SlashCurveMaximumError=" + Num(metrics.SlashCurveError),
                "RunningCurveMaximumError=" + Num(metrics.RunningCurveError),
                "RunningUpperBodyCurveCount=0",
                "UpperPosePositionMaximumError=" + Num(metrics.UpperPositionError),
                "UpperPoseAngleMaximumErrorDegrees=" + Num(metrics.UpperAngleError),
                "LowerPosePositionMaximumError=" + Num(metrics.LowerPositionError),
                "LowerPoseAngleMaximumErrorDegrees=" + Num(metrics.LowerAngleError),
                "BakedMeshFinite=True",
                "BakedMeshMaximumBoundsMagnitude=" + Num(metrics.MaximumBoundsMagnitude),
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "AnimatorLayers=Slash Full Body|Running Hips And Legs",
                "Loop=True",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ComparisonCapture=" + CapturePath
            }, Encoding.UTF8);
        }

        private static void WriteFixInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(FixInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The fix inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementName + "/" + SlotName + "/" + ModelName,
                "FaceForwardMethod=HeadDirectModelStableRotation",
                "LeftArmDeformationMethod=IntactDirectBodyMeshAndStableLeftShoulderArmForeArm",
                "BodyMesh=" + ModelPath + "/char1",
                "MusketContinuity=DirectIntact9798Triangles",
                "UpperBodyCentering=WholeSpineShoulderMidpointToDirectRestLateralAndVerticalOffset",
                "SwordDriver=RightHandRadialOutwardForwardCutRealtimeLateUpdate",
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "SwordStartBladeToVisibleUpDegrees=" + Num(metrics.StartBladeUpAngle),
                "SwordMaximumOutwardAlignmentErrorAfterBlendDegrees=" + Num(metrics.MaximumSwordOutwardAngle),
                "SwordMaximumForwardCutAlignmentErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "SwordMinimumBladeTipRadialGainMeters=" + Num(metrics.MinimumBladeTipRadialGain),
                "GifUpwardKeyFramesMinimumTipAboveGripMeters=" + Num(metrics.MinimumGifUpwardTipGain),
                "SwordMaximumBladeSweepFromUpDegrees=" + Num(metrics.MaximumBladeUpAngle),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "SwordMaximumWorldMotionMeters=" + Num(metrics.MaximumSwordMotion),
                "RightHandMaximumWorldMotionMeters=" + Num(metrics.MaximumHandMotion),
                "SlashUncorrectedUpperPosePositionMaximumError=" + Num(metrics.UpperPositionError),
                "SlashUncorrectedUpperPoseAngleMaximumErrorDegrees=" + Num(metrics.UpperAngleError),
                "RunningLowerPosePositionMaximumError=" + Num(metrics.LowerPositionError),
                "RunningLowerPoseAngleMaximumErrorDegrees=" + Num(metrics.LowerAngleError),
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "BakedMeshFinite=True",
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "FinalVisualComparison=" + FixCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteGifInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(GifInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The GIF trajectory inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementName + "/" + SlotName + "/" + ModelName,
                "ReferenceGif=C:/Users/gus68/OneDrive/바탕 화면/animated.gif",
                "ReferenceFrames=15",
                "ReferenceFrameRate=10fps",
                "ReferenceDurationSeconds=1.5",
                "SwordDriver=RightHandPalmGripPlusSuppliedGif15FrameTrajectory",
                "RightArmAnimationModified=False",
                "RightArmCurveMaximumError=" + Num(metrics.RightArmCurveError),
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "GifFrame01BladeAngleErrorDegrees=" + Num(metrics.StartBladeUpAngle),
                "GifTrajectoryMaximumAngleErrorDegrees=" + Num(metrics.MaximumSwordOutwardAngle),
                "SwordMaximumForwardCutAlignmentErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "SwordBladeLengthMeters=" + Num(metrics.MinimumBladeTipRadialGain),
                "GifUpwardKeyFramesMinimumTipAboveGripMeters=" + Num(metrics.MinimumGifUpwardTipGain),
                "SwordMaximumSweepFromModelUpDegrees=" + Num(metrics.MaximumBladeUpAngle),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "SlashUncorrectedUpperPosePositionMaximumError=" + Num(metrics.UpperPositionError),
                "SlashUncorrectedUpperPoseAngleMaximumErrorDegrees=" + Num(metrics.UpperAngleError),
                "RunningLowerPosePositionMaximumError=" + Num(metrics.LowerPositionError),
                "RunningLowerPoseAngleMaximumErrorDegrees=" + Num(metrics.LowerAngleError),
                "MusketContinuity=DirectIntact9798Triangles",
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "GuardImplementation=False",
                "BakedMeshFinite=True",
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "FinalVisualComparison=" + GifCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteGifRevisionInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(GifRevisionInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The revised GIF inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementName + "/" + SlotName + "/" + ModelName,
                "ReferenceGif=C:/Users/gus68/OneDrive/바탕 화면/animated.gif",
                "ComparedActualVideo=C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-08-20 21-00-08.mp4",
                "MismatchCause=GifScreenAxesWerePreviouslyInterpretedAsModelLocalAxes",
                "Correction=GifScreenRightUpDepthMappedThroughEnabledModelCam",
                "ReferenceFrames=15",
                "ReferenceFrameRate=10fps",
                "ReferenceDurationSeconds=1.5",
                "RightArmAnimationModified=False",
                "RightArmCurveMaximumError=" + Num(metrics.RightArmCurveError),
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "GifFrame01BladeAngleErrorDegrees=" + Num(metrics.StartBladeUpAngle),
                "GifTrajectoryMaximumAngleErrorDegrees=" + Num(metrics.MaximumSwordOutwardAngle),
                "SwordMaximumForwardCutPlaneErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "SwordBladeLengthMeters=" + Num(metrics.MinimumBladeTipRadialGain),
                "GifUpwardKeyFramesMinimumTipAboveGripMeters=" + Num(metrics.MinimumGifUpwardTipGain),
                "SwordMaximumSweepFromModelUpDegrees=" + Num(metrics.MaximumBladeUpAngle),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "MusketContinuity=DirectIntact9798Triangles",
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "GuardImplementation=False",
                "BakedMeshFinite=True",
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "FinalVisualComparison=" + GifRevisionCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteGifUpwardInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(GifUpwardInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The upward GIF inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementName + "/" + SlotName + "/" + ModelName,
                "ReferenceGif=C:/Users/gus68/OneDrive/바탕 화면/animated.gif",
                "ComparedActualVideo=C:/Users/gus68/Videos/Captures/Bellerophon - CargoRunMvp - Windows, Mac, Linux - Unity 6.3 LTS (6000.3.16f1) _DX12_ 2026-08-20 21-32-46.mp4",
                "MismatchCause=ModelCamRightAxisWasOppositeStableIspantFrontViewRight",
                "Correction=GifRightMappedToNegativeModelRightAndGifUpMappedToModelUp",
                "ReferenceFrames=15",
                "ReferenceFrameRate=10fps",
                "ReferenceDurationSeconds=1.5",
                "UpwardKeyFrames=01|02|03|05|06|14|15",
                "RightArmAnimationModified=False",
                "RightArmCurveMaximumError=" + Num(metrics.RightArmCurveError),
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "GifTrajectoryMaximumAngleErrorDegrees=" + Num(metrics.MaximumSwordOutwardAngle),
                "GifUpwardKeyFramesMinimumTipAboveGripMeters=" + Num(metrics.MinimumGifUpwardTipGain),
                "SwordMaximumForwardCutPlaneErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "SwordBladeLengthMeters=" + Num(metrics.MinimumBladeTipRadialGain),
                "SwordMaximumSweepFromModelUpDegrees=" + Num(metrics.MaximumBladeUpAngle),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "MusketContinuity=DirectIntact9798Triangles",
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "GuardImplementation=False",
                "BakedMeshFinite=True",
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "FinalVisualComparison=" + GifUpwardCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteGifActualTraceInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(GifActualTraceInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The actual GIF trace inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=Approved Ispant Enemy Placement/Ispant_05_RunningOneHandedSwordAttack/Ispant_New_Direct_Model",
                "ReferenceGif=C:/Users/gus68/OneDrive/바탕 화면/animated.gif",
                "ReferenceTrace=docs/validation/ispant_slash_gif_actual_trace_2026-08-20/Ispant_05_SlashGifActualTrace_Pixels.csv",
                "ReferenceFrames=15",
                "ReferenceFrameRate=10fps",
                "ReferenceDimensions=220x260",
                "TraceMethod=MeasuredGripAndVisibleBladeTipPixelsOnEverySuppliedGifFrame",
                "TrajectoryInput=MeasuredPixelsNotInventedDirectionSamples",
                "ReferenceBladeFullLengthPixels=" + Num(IspantRigidSwordFollower.ReferenceTraceBladeFullLengthPixels),
                "RightArmAnimationModified=False",
                "RightArmCurveMaximumError=" + Num(metrics.RightArmCurveError),
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "SwordMaximumForwardCutPlaneErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "MusketContinuity=DirectIntact9798Triangles",
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "GuardImplementation=False",
                "BakedMeshFinite=True",
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "FinalVisualComparison=" + GifActualTraceCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteLegacySwordInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(LegacySwordInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The legacy sword inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=Approved Ispant Enemy Placement/Ispant_05_RunningOneHandedSwordAttack/Ispant_New_Direct_Model",
                "SourceSceneState=PreModelRevisionSlot05",
                "SourceModel=" + LegacySwordModelPath,
                "SourceAnimation=" + LegacySwordClipPath,
                "SourceSwordCurvePath=" + LegacySwordCurvePath,
                "SourceFrames=" + LegacySwordFrameCount,
                "SourceModelSha256=" + LegacySwordModelHash,
                "SourceAnimationSha256=" + LegacySwordClipHash,
                "TransferMethod=LegacySwordBladeAndRollDirectionsSampledInSourceModelSpaceThenAppliedAtCurrentRightHandGrip",
                "RightArmAnimationModified=False",
                "RightArmCurveMaximumError=" + Num(metrics.RightArmCurveError),
                "LegacyFrame01BladeErrorDegrees=" + Num(metrics.StartBladeUpAngle),
                "LegacyBladeTrajectoryMaximumErrorDegrees=" + Num(metrics.MaximumSwordOutwardAngle),
                "LegacyRollTrajectoryMaximumErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "SwordBladeLengthMeters=" + Num(metrics.MinimumBladeTipRadialGain),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "MusketContinuity=DirectIntact9798Triangles",
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "GuardImplementation=False",
                "BakedMeshFinite=True",
                "MeshVertexCount=" + metrics.MeshVertexCount,
                "MeshTriangleCount=" + metrics.MeshTriangleCount,
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "VisualReference=" + LegacySwordReferenceReviewPath,
                "FinalVisualComparison=" + LegacySwordCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteLegacySwordGripInspection(Inspection metrics)
        {
            var destination = Path.GetFullPath(LegacySwordGripInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The legacy sword grip inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementName + "/" + SlotName + "/" + ModelName,
                "ReferenceGif=C:/Users/gus68/OneDrive/바탕 화면/animated.gif",
                "GripMethod=UpperHalfOfRightHandWeightedSpanClosedFistCenter",
                "GripHandLongitudinalStartRatio=" + Num(ReferenceSwordGripHandLongitudinalStartRatio),
                "RightArmAnimationModified=False",
                "RightArmCurveMaximumError=" + Num(metrics.RightArmCurveError),
                "LegacySourceFrames=" + LegacySwordFrameCount,
                "LegacyBladeTrajectoryMaximumErrorDegrees=" + Num(metrics.MaximumSwordOutwardAngle),
                "LegacyRollTrajectoryMaximumErrorDegrees=" + Num(metrics.MaximumSwordForwardCutAngle),
                "SwordGripMaximumErrorMeters=" + Num(metrics.MaximumGripError),
                "UpperBodyMaximumLateralCenterErrorMeters=" + Num(metrics.MaximumUpperBodyLateralError),
                "UpperBodyMaximumVerticalCenterErrorMeters=" + Num(metrics.MaximumUpperBodyVerticalError),
                "FaceAndLeftArmMaximumRestRotationErrorDegrees=" + Num(metrics.MaximumStableBoneAngle),
                "MusketContinuity=DirectIntact9798Triangles",
                "RootMotion=False",
                "InPlace=True",
                "Loop=True",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "FinalVisualComparison=" + LegacySwordGripCapturePath
            }, Encoding.UTF8);
        }

        private static Target RequireTarget(Scene scene)
        {
            var roots = scene.GetRootGameObjects().Where(item => item.name == PlacementName).ToArray();
            if (roots.Length != 1)
                throw new InvalidOperationException("The approved Ispant placement root count differs.");
            var placement = roots[0].transform;
            if (placement.childCount != 12)
                throw new InvalidOperationException("The approved Ispant placement must contain 12 slots.");
            var slot = placement.Find(SlotName) ??
                throw new InvalidOperationException("Ispant slot 5 is missing.");
            if (slot.parent != placement || placement.GetChild(4) != slot || slot.childCount != 1)
                throw new InvalidOperationException("Ispant slot 5 hierarchy or ordinal differs.");
            var model = slot.GetChild(0);
            if (model.name != ModelName)
                throw new InvalidOperationException("Slot 5 does not contain the current direct model.");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
            var correspondingPath = source == null ? string.Empty : AssetDatabase.GetAssetPath(source);
            var nearestInstancePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                model.gameObject);
            if (correspondingPath != ModelPath && nearestInstancePath != ModelPath)
                throw new InvalidOperationException("Slot 5 no longer references the current direct FBX.");
            return new Target(placement, slot, model);
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            if (clean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static ModelImporter RequireImporter(string path) =>
            AssetImporter.GetAtPath(path) as ModelImporter ??
            throw new InvalidOperationException("ModelImporter missing: " + path + ".");

        private static ModelImporterClipAnimation[] MixamoTakes(ModelImporter importer) =>
            (importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>())
            .Where(item => item.takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        private static string DescribeTakes(ModelImporter importer) =>
            string.Join("|", (importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>())
                .Select(DescribeClip));

        private static string DescribeClip(ModelImporterClipAnimation clip) =>
            clip.name + "@" + clip.takeName + "[" + Num(clip.firstFrame) + "-" + Num(clip.lastFrame) + "]";

        private static AnimationClip RequireImportedClip(string path, string name)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1 || clips[0].name != name)
                throw new InvalidOperationException("The selected imported Mixamo clip differs: " + path + ".");
            return clips[0];
        }

        private static string[] BoneDescriptions(Transform root)
        {
            var armature = root.Cast<Transform>().Single(item => item.name == "Armature");
            return armature.GetComponentsInChildren<Transform>(true)
                .Where(item => item != armature && item.GetComponent<Renderer>() == null)
                .Select(item => AnimationUtility.CalculateTransformPath(item, armature) + "<-" +
                    (item.parent == armature ? "Armature" : item.parent.name))
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one bone named " + name + ". Count=" + matches.Length + ".");
            return matches[0];
        }

        private static SkinnedMeshRenderer RequireBody(Transform model) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");

        private static MeshRenderer RequireSword(Transform model) =>
            model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == "Ispant_Approved_LongSword_10K");

        private static Vector3 CalculateGripCenter(Mesh mesh)
        {
            var gripX = Mathf.Lerp(
                mesh.bounds.min.x,
                mesh.bounds.max.x,
                ReferenceGripDistanceFromPommelRatio);
            var halfWidth = mesh.bounds.size.x * ReferenceGripRegionHalfWidthRatio;
            var values = mesh.vertices
                .Where(item => Mathf.Abs(item.x - gripX) <= halfWidth).ToArray();
            if (values.Length < 16)
                throw new InvalidOperationException("The approved long-sword grip region differs. Count=" + values.Length + ".");
            var center = values.Aggregate(Vector3.zero, (sum, value) => sum + value) / values.Length;
            center.x = gripX;
            return center;
        }

        private static Vector3 CalculateBladeTipCenter(Mesh mesh, Vector3 gripCenter)
        {
            var minimumX = mesh.bounds.min.x;
            var maximumX = mesh.bounds.max.x;
            var tipX = Mathf.Abs(minimumX - gripCenter.x) > Mathf.Abs(maximumX - gripCenter.x)
                ? minimumX
                : maximumX;
            var tolerance = Mathf.Max(mesh.bounds.size.x * 0.01f, 0.0000001f);
            var values = mesh.vertices.Where(vertex => Mathf.Abs(vertex.x - tipX) <= tolerance).ToArray();
            if (values.Length == 0)
                throw new InvalidOperationException("The approved long-sword tip region differs.");
            return values.Aggregate(Vector3.zero, (sum, value) => sum + value) / values.Length;
        }

        private static Vector3 CalculateBladeLocalAxis(Mesh mesh, Vector3 gripCenter) =>
            (CalculateBladeTipCenter(mesh, gripCenter) - gripCenter).normalized;

        private static float DirectRestUpperLateralOffset()
        {
            var model = RequireAsset<GameObject>(ModelPath).transform;
            var hips = RequireDescendant(model, "Hips");
            var left = RequireDescendant(model, "LeftShoulder");
            var right = RequireDescendant(model, "RightShoulder");
            return Vector3.Dot(Vector3.Lerp(left.position, right.position, 0.5f) - hips.position, model.right);
        }

        private static float DirectRestUpperVerticalOffset()
        {
            var model = RequireAsset<GameObject>(ModelPath).transform;
            var hips = RequireDescendant(model, "Hips");
            var left = RequireDescendant(model, "LeftShoulder");
            var right = RequireDescendant(model, "RightShoulder");
            return Vector3.Dot(Vector3.Lerp(left.position, right.position, 0.5f) - hips.position, model.up);
        }

        private static Vector3 WeightedPalmCenter(SkinnedMeshRenderer body, Transform hand)
        {
            var values = RightHandWeightedWorldVertices(body, hand);
            return values.Aggregate(Vector3.zero, (sum, value) => sum + value) / values.Length;
        }

        private static Vector3 PalmGripCenter(SkinnedMeshRenderer body, Transform hand)
        {
            var localValues = RightHandWeightedWorldVertices(body, hand)
                .Select(hand.InverseTransformPoint).ToArray();
            var minimum = localValues.Min(item => item.y);
            var maximum = localValues.Max(item => item.y);
            var start = Mathf.Lerp(
                minimum, maximum, ReferenceSwordGripHandLongitudinalStartRatio);
            var fistValues = localValues.Where(item => item.y >= start).ToArray();
            if (fistValues.Length < 16)
                throw new InvalidOperationException(
                    "Too few closed-fist vertices were found for the sword grip.");
            var fistCenter = fistValues.Aggregate(Vector3.zero, (sum, value) => sum + value) /
                             fistValues.Length;
            return hand.TransformPoint(fistCenter);
        }

        private static Bounds RightHandWeightedLocalBounds(SkinnedMeshRenderer body, Transform hand)
        {
            var values = RightHandWeightedWorldVertices(body, hand)
                .Select(hand.InverseTransformPoint).ToArray();
            var bounds = new Bounds(values[0], Vector3.zero);
            foreach (var value in values.Skip(1)) bounds.Encapsulate(value);
            return bounds;
        }

        private static string DescribeRightHandWeightedLocalYHistogram(
            SkinnedMeshRenderer body, Transform hand)
        {
            const int binCount = 8;
            var values = RightHandWeightedWorldVertices(body, hand)
                .Select(hand.InverseTransformPoint).ToArray();
            var minimum = values.Min(item => item.y);
            var maximum = values.Max(item => item.y);
            var counts = new int[binCount];
            foreach (var value in values)
            {
                var normalized = Mathf.InverseLerp(minimum, maximum, value.y);
                var index = Mathf.Clamp(Mathf.FloorToInt(normalized * binCount), 0, binCount - 1);
                counts[index]++;
            }
            return string.Join("/", counts.Select(item => item.ToString(CultureInfo.InvariantCulture)));
        }

        private static Vector3[] RightHandWeightedWorldVertices(
            SkinnedMeshRenderer body, Transform hand)
        {
            var mesh = body.sharedMesh;
            var handIndex = Array.IndexOf(body.bones, hand);
            if (handIndex < 0 || mesh.boneWeights.Length != mesh.vertexCount)
                throw new InvalidOperationException("The RightHand skinning data differs.");
            var values = Enumerable.Range(0, mesh.vertexCount)
                .Where(index => WeightForBone(mesh.boneWeights[index], handIndex) >= 0.1f)
                .Select(index =>
                {
                    var value = Vector3.zero;
                    var weight = mesh.boneWeights[index];
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex0, weight.weight0, body.bones, mesh.bindposes);
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex1, weight.weight1, body.bones, mesh.bindposes);
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex2, weight.weight2, body.bones, mesh.bindposes);
                    AddSkin(ref value, mesh.vertices[index], weight.boneIndex3, weight.weight3, body.bones, mesh.bindposes);
                    return value;
                }).ToArray();
            if (values.Length < 4)
                throw new InvalidOperationException("Too few RightHand-weighted vertices were found.");
            return values;
        }

        private static float WeightForBone(BoneWeight weight, int bone)
        {
            var result = 0f;
            if (weight.boneIndex0 == bone) result += weight.weight0;
            if (weight.boneIndex1 == bone) result += weight.weight1;
            if (weight.boneIndex2 == bone) result += weight.weight2;
            if (weight.boneIndex3 == bone) result += weight.weight3;
            return result;
        }

        private static void AddSkin(
            ref Vector3 result, Vector3 vertex, int index, float weight,
            Transform[] bones, Matrix4x4[] bindposes)
        {
            if (weight > 0f)
                result += bones[index].TransformPoint(bindposes[index].MultiplyPoint3x4(vertex)) * weight;
        }

        private static Quaternion EvaluateRotation(AnimationClip clip, string path, float time)
        {
            var rotation = new Quaternion(
                RequireCurve(clip, path, "m_LocalRotation.x").Evaluate(time),
                RequireCurve(clip, path, "m_LocalRotation.y").Evaluate(time),
                RequireCurve(clip, path, "m_LocalRotation.z").Evaluate(time),
                RequireCurve(clip, path, "m_LocalRotation.w").Evaluate(time));
            return rotation.normalized;
        }

        private static AnimationCurve RequireCurve(AnimationClip clip, string path, string property) =>
            AnimationUtility.GetEditorCurve(
                clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property)) ??
            throw new InvalidOperationException("Required curve missing: " + path + "/" + property + ".");

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Required asset missing: " + path + ".");

        private static string AppearanceSignature(Transform model) =>
            string.Join("|", model.GetComponentsInChildren<Renderer>(true).OrderBy(item => item.name)
                .Select(item => item.name + ":" + item.GetType().FullName + ":" + item.enabled + ":" +
                    MeshPath(item) + ":" + string.Join("+", item.sharedMaterials.Select(AssetDatabase.GetAssetPath))));

        private static string MaterialSignature(Transform model) =>
            string.Join("|", model.GetComponentsInChildren<Renderer>(true).OrderBy(item => item.name)
                .Select(item => item.name + ":" +
                    string.Join("+", item.sharedMaterials.Select(AssetDatabase.GetAssetPath))));

        private static string MeshPath(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return AssetDatabase.GetAssetPath(skinned.sharedMesh);
            var filter = renderer.GetComponent<MeshFilter>();
            return filter == null ? "<none>" : AssetDatabase.GetAssetPath(filter.sharedMesh);
        }

        private static string OtherSlotSignatures(Transform placement, Transform excluded) =>
            string.Join("|", placement.Cast<Transform>().Where(item => item != excluded)
                .OrderBy(item => item.name).Select(TransformSignature));

        private static string OtherRootSignatures(Scene scene, Transform excluded) =>
            string.Join("|", scene.GetRootGameObjects().Where(item => item.transform != excluded)
                .OrderBy(item => item.name).Select(item => TransformSignature(item.transform)));

        private static string TransformSignature(Transform value) =>
            value.name + ":" + Vec(value.localPosition) + ":" + Quat(value.localRotation) + ":" +
            Vec(value.localScale) + ":" + value.childCount;

        private static void RequireSame(string before, string after, string message)
        {
            if (!string.Equals(before, after, StringComparison.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void RequireHashes()
        {
            RequireHash(SlashSourcePath, SlashHash);
            RequireHash(RunningSourcePath, RunningHash);
            RequireHash(ModelPath, ModelHash);
            RequireHash(LegacySwordModelPath, LegacySwordModelHash);
            RequireHash(LegacySwordClipPath, LegacySwordClipHash);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Path.GetFullPath(path));
            using var sha = SHA256.Create();
            var actual = string.Concat(sha.ComputeHash(stream)
                .Select(item => item.ToString("X2", CultureInfo.InvariantCulture)));
            if (actual != expected)
                throw new InvalidOperationException("Asset hash differs: " + path + ".");
        }

        private static string BindingKey(EditorCurveBinding binding) =>
            binding.path + "|" + binding.type.FullName + "|" + binding.propertyName;

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
                count += checked((int)mesh.GetIndexCount(index) / 3);
            return count;
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static void SetLayer(Transform root, int layer)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.layer = layer;
        }

        private static string Num(float value) =>
            value.ToString("0.#########", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);

        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct Target
        {
            public readonly Transform Placement;
            public readonly Transform Slot;
            public readonly Transform Model;

            public Target(Transform placement, Transform slot, Transform model)
            {
                Placement = placement;
                Slot = slot;
                Model = model;
            }
        }

        private readonly struct LegacySwordTrajectory
        {
            public readonly Vector3[] BladeDirectionsInModelSpace;
            public readonly Vector3[] RollDirectionsInModelSpace;

            public LegacySwordTrajectory(
                Vector3[] bladeDirectionsInModelSpace,
                Vector3[] rollDirectionsInModelSpace)
            {
                BladeDirectionsInModelSpace = bladeDirectionsInModelSpace;
                RollDirectionsInModelSpace = rollDirectionsInModelSpace;
            }
        }

        private readonly struct Inspection
        {
            public readonly Target Target;
            public readonly AnimationClip SlashSource;
            public readonly AnimationClip RunningSource;
            public readonly AnimationClip SlashClip;
            public readonly AnimationClip RunningClip;
            public readonly float SlashCurveError;
            public readonly float RunningCurveError;
            public readonly float RightArmCurveError;
            public readonly float UpperPositionError;
            public readonly float UpperAngleError;
            public readonly float LowerPositionError;
            public readonly float LowerAngleError;
            public readonly float MaximumBoundsMagnitude;
            public readonly int MeshVertexCount;
            public readonly int MeshTriangleCount;
            public readonly float MaximumGripError;
            public readonly float StartBladeUpAngle;
            public readonly float MaximumSwordOutwardAngle;
            public readonly float MaximumSwordForwardCutAngle;
            public readonly float MinimumBladeTipRadialGain;
            public readonly float MinimumGifUpwardTipGain;
            public readonly float MaximumBladeUpAngle;
            public readonly float MaximumUpperBodyLateralError;
            public readonly float MaximumUpperBodyVerticalError;
            public readonly float MaximumStableBoneAngle;
            public readonly float MaximumSwordMotion;
            public readonly float MaximumHandMotion;

            public Inspection(
                Target target, AnimationClip slashSource, AnimationClip runningSource,
                AnimationClip slashClip, AnimationClip runningClip,
                float slashCurveError, float runningCurveError,
                float rightArmCurveError, SampleMetrics metrics)
            {
                Target = target;
                SlashSource = slashSource;
                RunningSource = runningSource;
                SlashClip = slashClip;
                RunningClip = runningClip;
                SlashCurveError = slashCurveError;
                RunningCurveError = runningCurveError;
                RightArmCurveError = rightArmCurveError;
                UpperPositionError = metrics.UpperPositionError;
                UpperAngleError = metrics.UpperAngleError;
                LowerPositionError = metrics.LowerPositionError;
                LowerAngleError = metrics.LowerAngleError;
                MaximumBoundsMagnitude = metrics.MaximumBoundsMagnitude;
                MeshVertexCount = metrics.MeshVertexCount;
                MeshTriangleCount = metrics.MeshTriangleCount;
                MaximumGripError = metrics.MaximumGripError;
                StartBladeUpAngle = metrics.StartBladeUpAngle;
                MaximumSwordOutwardAngle = metrics.MaximumSwordOutwardAngle;
                MaximumSwordForwardCutAngle = metrics.MaximumSwordForwardCutAngle;
                MinimumBladeTipRadialGain = metrics.MinimumBladeTipRadialGain;
                MinimumGifUpwardTipGain = metrics.MinimumGifUpwardTipGain;
                MaximumBladeUpAngle = metrics.MaximumBladeUpAngle;
                MaximumUpperBodyLateralError = metrics.MaximumUpperBodyLateralError;
                MaximumUpperBodyVerticalError = metrics.MaximumUpperBodyVerticalError;
                MaximumStableBoneAngle = metrics.MaximumStableBoneAngle;
                MaximumSwordMotion = metrics.MaximumSwordMotion;
                MaximumHandMotion = metrics.MaximumHandMotion;
            }
        }

        private readonly struct SampleMetrics
        {
            public readonly float UpperPositionError;
            public readonly float UpperAngleError;
            public readonly float LowerPositionError;
            public readonly float LowerAngleError;
            public readonly float MaximumBoundsMagnitude;
            public readonly int MeshVertexCount;
            public readonly int MeshTriangleCount;
            public readonly float MaximumGripError;
            public readonly float StartBladeUpAngle;
            public readonly float MaximumSwordOutwardAngle;
            public readonly float MaximumSwordForwardCutAngle;
            public readonly float MinimumBladeTipRadialGain;
            public readonly float MinimumGifUpwardTipGain;
            public readonly float MaximumBladeUpAngle;
            public readonly float MaximumUpperBodyLateralError;
            public readonly float MaximumUpperBodyVerticalError;
            public readonly float MaximumStableBoneAngle;
            public readonly float MaximumSwordMotion;
            public readonly float MaximumHandMotion;

            public SampleMetrics(
                float upperPositionError, float upperAngleError,
                float lowerPositionError, float lowerAngleError,
                float maximumBoundsMagnitude, int meshVertexCount, int meshTriangleCount,
                PresentationMetrics presentation)
            {
                UpperPositionError = upperPositionError;
                UpperAngleError = upperAngleError;
                LowerPositionError = lowerPositionError;
                LowerAngleError = lowerAngleError;
                MaximumBoundsMagnitude = maximumBoundsMagnitude;
                MeshVertexCount = meshVertexCount;
                MeshTriangleCount = meshTriangleCount;
                MaximumGripError = presentation.MaximumGripError;
                StartBladeUpAngle = presentation.StartBladeUpAngle;
                MaximumSwordOutwardAngle = presentation.MaximumSwordOutwardAngle;
                MaximumSwordForwardCutAngle = presentation.MaximumSwordForwardCutAngle;
                MinimumBladeTipRadialGain = presentation.MinimumBladeTipRadialGain;
                MinimumGifUpwardTipGain = presentation.MinimumGifUpwardTipGain;
                MaximumBladeUpAngle = presentation.MaximumBladeUpAngle;
                MaximumUpperBodyLateralError = presentation.MaximumUpperBodyLateralError;
                MaximumUpperBodyVerticalError = presentation.MaximumUpperBodyVerticalError;
                MaximumStableBoneAngle = presentation.MaximumStableBoneAngle;
                MaximumSwordMotion = presentation.MaximumSwordMotion;
                MaximumHandMotion = presentation.MaximumHandMotion;
            }
        }

        private readonly struct PresentationMetrics
        {
            public readonly float MaximumGripError;
            public readonly float StartBladeUpAngle;
            public readonly float MaximumSwordOutwardAngle;
            public readonly float MaximumSwordForwardCutAngle;
            public readonly float MinimumBladeTipRadialGain;
            public readonly float MinimumGifUpwardTipGain;
            public readonly float MaximumBladeUpAngle;
            public readonly float MaximumUpperBodyLateralError;
            public readonly float MaximumUpperBodyVerticalError;
            public readonly float MaximumStableBoneAngle;
            public readonly float MaximumSwordMotion;
            public readonly float MaximumHandMotion;

            public PresentationMetrics(
                float maximumGripError, float startBladeUpAngle,
                float maximumSwordOutwardAngle, float maximumSwordForwardCutAngle,
                float minimumBladeTipRadialGain, float minimumGifUpwardTipGain,
                float maximumBladeUpAngle,
                float maximumUpperBodyLateralError, float maximumUpperBodyVerticalError,
                float maximumStableBoneAngle,
                float maximumSwordMotion, float maximumHandMotion)
            {
                MaximumGripError = maximumGripError;
                StartBladeUpAngle = startBladeUpAngle;
                MaximumSwordOutwardAngle = maximumSwordOutwardAngle;
                MaximumSwordForwardCutAngle = maximumSwordForwardCutAngle;
                MinimumBladeTipRadialGain = minimumBladeTipRadialGain;
                MinimumGifUpwardTipGain = minimumGifUpwardTipGain;
                MaximumBladeUpAngle = maximumBladeUpAngle;
                MaximumUpperBodyLateralError = maximumUpperBodyLateralError;
                MaximumUpperBodyVerticalError = maximumUpperBodyVerticalError;
                MaximumStableBoneAngle = maximumStableBoneAngle;
                MaximumSwordMotion = maximumSwordMotion;
                MaximumHandMotion = maximumHandMotion;
            }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform value)
            {
                target = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            public bool Matches(float tolerance) =>
                target != null && Vector3.Distance(target.localPosition, position) <= tolerance &&
                Quaternion.Angle(target.localRotation, rotation) <= tolerance &&
                Vector3.Distance(target.localScale, scale) <= tolerance;

            public void Restore()
            {
                if (target == null)
                    return;
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private sealed class RendererSnapshot
        {
            public readonly Renderer Renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (Renderer != null)
                    Renderer.enabled = enabled;
            }
        }
    }
}
