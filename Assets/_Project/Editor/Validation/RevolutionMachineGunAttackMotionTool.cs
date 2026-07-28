using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionMachineGunAttackMotionTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string AttackSlotName = "Revolution_04";
        private const string ArtRoot =
            "Assets/_Project/Art/Enemies/Revolution";
        private const string AnimationFolder =
            ArtRoot + "/Animations";
        private const string ControllerFolder =
            ArtRoot + "/Controllers";
        private const string ClipPath =
            AnimationFolder +
            "/Revolution_04_MachineGun_Attack.anim";
        private const string ControllerPath =
            ControllerFolder +
            "/Revolution_04_MachineGun_Attack.controller";
        private const string ReadableSourceModelPath =
            AnimationFolder +
            "/Revolution_04_MachineGun_ReadableSource.fbx";
        private const string RiggedMeshPath =
            AnimationFolder +
            "/Revolution_04_MachineGun_RiggedMesh.asset";
        private const string LeftBarrelMeshPath =
            AnimationFolder +
            "/Revolution_04_Left_MachineGun_Barrels.asset";
        private const string RightBarrelMeshPath =
            AnimationFolder +
            "/Revolution_04_Right_MachineGun_Barrels.asset";
        private const string RightBarrelGroupNamePrefix =
            "Revolution_Right_MachineGun_BarrelGroup_";
        private const string RightBarrelGroupAssetPrefix =
            AnimationFolder +
            "/Revolution_04_Right_MachineGun_BarrelGroup_";
        private const string ApprovedModelPath =
            ArtRoot +
            "/ApprovedAppearance/Models/Revolution_ApprovedAppearance.fbx";
        private const string ApprovedMaterialFolder =
            ArtRoot +
            "/ApprovedAppearance/Materials/";
        private const string RebellionFlashMeshPath =
            "Assets/_Project/Art/Enemies/Rebellion/VFX/" +
            "Rebellion_Forward_Burst_Flash.asset";
        private const string RebellionFlashMaterialPath =
            "Assets/_Project/Art/Enemies/Rebellion/VFX/" +
            "Rebellion_Forward_Burst_Flash.mat";
        private const string StateName =
            "RevolutionMachineGunAttack";
        private const string LeftSpinBoneName =
            "Revolution_Left_Barrel_Ring_Bone";
        private const string RightSpinBoneName =
            "Revolution_Right_Barrel_Ring_Bone";
        private const string LeftFlashPivotName =
            "Revolution_Left_Muzzle_Flash_Pivot";
        private const string RightFlashPivotName =
            "Revolution_Right_Muzzle_Flash_Pivot";
        private const string LeftFlashName =
            "Revolution_Left_Muzzle_Flash";
        private const string RightFlashName =
            "Revolution_Right_Muzzle_Flash";
        private const string InvalidLeftSpinPivotName =
            "Revolution_Left_Gun_Spin_Pivot";
        private const string InvalidRightSpinPivotName =
            "Revolution_Right_Gun_Spin_Pivot";
        private const string ReportPath =
            "docs/validation/revolution_machinegun_attack_2026-07-28/" +
            "Revolution_04_MachineGunAttack_Inspection.txt";
        private const string CapturePath =
            "docs/validation/revolution_machinegun_attack_2026-07-28/" +
            "Revolution_04_MachineGunAttack_VisualReview.png";
        private const string VisualSequenceFolderName =
            "Bellerophon_RevolutionMachineGunVisualReview";
        private const float LoopSeconds = 5f;
        private const float ShotInterval = 0.2f;
        private const float FlashSeconds = 0.08f;
        private const float RotationDegreesPerShot = 72f;
        private const float StepEdgeSeconds = 1f / 240f;
        private const float FlashMuzzleOffset = 0.004f;
        private const int ShotCount = 25;
        private const int ExpectedTriangles = 3945;
        private const int ExpectedBaseBones = 24;
        private const int ExpectedRiggedBones = 26;
        private const int ExpectedMaterials = 8;
        private const int RightBarrelGroupCount = 5;

        private static readonly string[] SlotNames =
        {
            "Revolution_01",
            "Revolution_02",
            "Revolution_03",
            "Revolution_04",
            "Revolution_05",
            "Revolution_06",
            "Revolution_07",
            "Revolution_08"
        };

        private static readonly string[] FixedAimBoneNames =
        {
            "LeftArm",
            "LeftForeArm",
            "LeftHand",
            "RightArm",
            "RightForeArm",
            "RightHand"
        };

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Machine Gun Attack Motion")]
        public static void ApplyRevolutionMachineGunAttackMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying the corrected Revolution machine-gun attack.");
            }

            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var slot = RequireDirectChild(root.transform, AttackSlotName);
            var model = RequireModel(slot);
            DisablePreviousAnimatorForRebuild(model);
            var renderer = RequireBaseOrPreviousRenderer(model);
            var flashMesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    RebellionFlashMeshPath) ??
                throw new InvalidOperationException(
                    "The approved Rebellion muzzle-flash mesh is missing.");
            var flashMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    RebellionFlashMaterialPath) ??
                throw new InvalidOperationException(
                    "The approved Rebellion muzzle-flash material is missing.");
            var readableSource =
                PrepareReadableSourceModel();
            RequireCompatibleBoneOrder(renderer, readableSource.Renderer);

            var protectedBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherSlotSignatures(root.transform);
            var rootState = TransformState.Capture(root.transform);
            var slotState = TransformState.Capture(slot);
            var modelState = TransformState.Capture(model);

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(
                "Correct Revolution machine-gun attack motion");
            Undo.RegisterFullObjectHierarchyUndo(
                slot.gameObject,
                "Correct Revolution machine-gun attack motion");

            try
            {
                EnsureEditableSlotModel(model);
                RemoveInvalidAndPreviousRig(model, renderer);
                renderer = RequireBaseApprovedAppearance(model);
                RestoreApprovedBaseBonePose(
                    renderer,
                    readableSource.Renderer);

                var bakedBase = BakeWorldVertices(renderer);
                var leftSelection = SelectBarrelComponents(
                    readableSource.Mesh,
                    renderer,
                    bakedBase,
                    "LeftForeArm",
                    "LeftHand");
                var rightSelection =
                    SelectCompleteRightBarrelAssembly(
                    readableSource.Mesh,
                    renderer,
                    bakedBase,
                    "RightForeArm",
                    "RightHand");
                if (leftSelection.VertexIndices.Overlaps(
                        rightSelection.VertexIndices))
                {
                    throw new InvalidOperationException(
                        "Left and right Revolution barrel component selections overlap.");
                }

                var leftRig = CreateBarrelRig(
                    model,
                    "LeftForeArm",
                    "LeftHand",
                    LeftSpinBoneName,
                    LeftFlashPivotName,
                    LeftFlashName,
                    leftSelection,
                    flashMesh,
                    flashMaterial);
                var rightRig = CreateBarrelRig(
                    model,
                    "RightForeArm",
                    "RightHand",
                    RightSpinBoneName,
                    RightFlashPivotName,
                    RightFlashName,
                    rightSelection,
                    flashMesh,
                    flashMaterial);

                var aimPose = CalculateForwardAimPose(
                    slot,
                    model,
                    leftRig,
                    rightRig);
                var derivedMesh = CreateRiggedMesh(
                    model,
                    readableSource.Mesh,
                    renderer,
                    leftRig,
                    rightRig,
                    leftSelection,
                    rightSelection,
                    aimPose);
                var originalBones = renderer.bones.ToArray();
                renderer.sharedMesh = derivedMesh;
                renderer.bones = originalBones
                    .Concat(
                        new[]
                        {
                            leftRig.SpinBone,
                            rightRig.SpinBone
                        })
                    .ToArray();
                EditorUtility.SetDirty(renderer);

                var clip = CreateAttackClip(
                    model,
                    leftRig,
                    rightRig,
                    aimPose);
                var controller = CreateController(clip);
                var animator = GetOrCreateAnimator(model);
                animator.enabled = true;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                foreach (var animation in
                         model.GetComponentsInChildren<Animation>(true))
                {
                    animation.enabled = false;
                    EditorUtility.SetDirty(animation);
                }
                animator.Rebind();
                animator.Update(0f);
                EditorUtility.SetDirty(animator);

                RequireSameTransform(
                    rootState,
                    root.transform,
                    PlacementRootName);
                RequireSameTransform(
                    slotState,
                    slot,
                    AttackSlotName);
                RequireSameTransform(
                    modelState,
                    model,
                    model.name);
                RequireCorrectedAppearance(
                    model,
                    readableSource);

                if (!otherSlotsBefore.SequenceEqual(
                        OtherSlotSignatures(root.transform),
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A Revolution slot outside Revolution_04 changed while correcting the machine-gun attack.");
                }
                if (!protectedBefore.SequenceEqual(
                        ProtectedRootSignatures(scene),
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A scene root outside the Revolution placement changed while correcting the machine-gun attack.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after correcting the Revolution machine-gun attack.");
                }
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log(
                    "RevolutionMachineGunAttackApplied" +
                    ", Correction=RebellionStyleInPlaceBarrelRingRotation" +
                    ", Slot=" + AttackSlotName +
                    ", LoopSeconds=" + Num(LoopSeconds) +
                    ", ShotIntervalSeconds=" + Num(ShotInterval) +
                    ", FlashSeconds=" + Num(FlashSeconds) +
                    ", RotationDegreesPerShot=" +
                    Num(RotationDegreesPerShot) +
                    ", LeftBarrelComponents=" +
                    leftSelection.ComponentCount +
                    ", RightBarrelComponents=" +
                    rightSelection.ComponentCount +
                    ", LeftBarrelVertices=" +
                    leftSelection.VertexIndices.Count +
                    ", RightBarrelVertices=" +
                    rightSelection.VertexIndices.Count +
                    ", LeftRotationAxis=FixedMuzzleRingPlaneNormal" +
                    ", RightRotationAxis=RightBarrelFrontRearRingCenterLine" +
                    ", BarrelGeometry=LeftCombinedRigidRing_RightFiveCompleteCounterRotatingBarrelGroups" +
                    ", RightBarrelOrientationCompensation=EqualAndOppositePerGroup" +
                    ", RightBarrelOrbitRadius=MatchedToNormalLeftBarrelCenters" +
                    ", FlashAnchor=FixedUpperBarrelMuzzle" +
                    ", WholeArmSpin=False" +
                    ", ReusedRebellionFlashAssets=True" +
                    ", RootMotion=False" +
                    ", OtherSlotsUnchanged=True" +
                    ", PlayerCameraAndOtherRootsUnchanged=True" +
                    ", VisualReviewRequired=True" +
                    ", SceneSaved=True.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Inspect Machine Gun Attack Motion")]
        public static void InspectRevolutionMachineGunAttackMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before corrected Revolution machine-gun inspection.");
            }
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var slot = RequireDirectChild(root.transform, AttackSlotName);
            var model = RequireModel(slot);
            var readableSource = PrepareReadableSourceModel();
            RequireCorrectedAppearance(model, readableSource);
            var animator = RequireAnimator(model);
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "The corrected Revolution machine-gun clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The corrected Revolution machine-gun controller is missing.");
            var wasDirty = scene.isDirty;
            var metrics = InspectMotion(
                root.transform,
                slot,
                model,
                animator,
                clip,
                controller,
                readableSource);
            WriteInspectionReport(metrics);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Corrected Revolution machine-gun inspection changed the scene dirty state.");
            }
            Debug.Log(
                "RevolutionMachineGunAttackInspected Result=PASS" +
                ", Correction=ForwardAimAndBarrelOnlyRingRotation" +
                ", Slot=" + AttackSlotName +
                ", LeftForwardAimErrorDegrees=" +
                Num(metrics.LeftForwardAimErrorDegrees) +
                ", RightForwardAimErrorDegrees=" +
                Num(metrics.RightForwardAimErrorDegrees) +
                ", LeftRightwardDisplacement=" +
                Num(metrics.LeftRightwardDisplacement) +
                ", RightRightwardDisplacement=" +
                Num(metrics.RightRightwardDisplacement) +
                ", FixedArmAndHousingDriftDegrees=" +
                Num(metrics.MaximumFixedAimBoneDriftDegrees) +
                ", LoopBoundaryError=" +
                Num(metrics.LoopBoundaryError) +
                ", SceneChanged=False.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Capture Machine Gun Attack Review")]
        public static void CaptureRevolutionMachineGunAttackMotionReview()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before corrected Revolution machine-gun capture.");
            }
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var slot = RequireDirectChild(root.transform, AttackSlotName);
            var model = RequireModel(slot);
            var readableSource = PrepareReadableSourceModel();
            RequireCorrectedAppearance(model, readableSource);
            var animator = RequireAnimator(model);
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "The corrected Revolution machine-gun clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The corrected Revolution machine-gun controller is missing.");
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The corrected one-time Revolution machine-gun review path already exists: " +
                    CapturePath);
            }
            CaptureReviewGrid(
                scene,
                model,
                animator,
                clip,
                destination);
            Debug.Log(
                "RevolutionMachineGunAttackCaptured" +
                ", Correction=RebellionStyleInPlaceBarrelRingRotation" +
                ", Slot=" + AttackSlotName +
                ", ReviewTimes=0.00,0.04,0.08,0.12,0.16,0.20" +
                ", Views=FullBodyFront,GunFront,RightGunFrontCloseup" +
                ", Image=" + CapturePath +
                ", AutomatedMotionJudgement=False" +
                ", SceneChanged=False.");
        }

        private static ReadableSource PrepareReadableSourceModel()
        {
            EnsureAssetFolder(AnimationFolder);
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    ReadableSourceModelPath) == null)
            {
                if (!AssetDatabase.CopyAsset(
                        ApprovedModelPath,
                        ReadableSourceModelPath))
                {
                    throw new InvalidOperationException(
                        "Could not create the exact readable Revolution animation source copy.");
                }
            }
            var importer =
                AssetImporter.GetAtPath(ReadableSourceModelPath)
                    as ModelImporter ??
                throw new InvalidOperationException(
                    "Readable Revolution animation source importer is missing.");
            if (!importer.isReadable || importer.importAnimation)
            {
                importer.isReadable = true;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }
            var asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ReadableSourceModelPath) ??
                throw new InvalidOperationException(
                    "Readable Revolution animation source failed to import.");
            var renderer =
                asset.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Readable Revolution animation source must contain one skinned renderer.");
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Readable Revolution animation source mesh is missing.");
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException(
                    "Readable Revolution animation source mesh remains non-readable.");
            }
            return new ReadableSource(asset, renderer, mesh);
        }

        private static void RequireCompatibleBoneOrder(
            SkinnedMeshRenderer target,
            SkinnedMeshRenderer source)
        {
            var targetNames = target.bones
                .Take(ExpectedBaseBones)
                .Select(item => item.name)
                .ToArray();
            var sourceNames = source.bones
                .Select(item => item.name)
                .ToArray();
            if (targetNames.Length != ExpectedBaseBones ||
                sourceNames.Length != ExpectedBaseBones ||
                !targetNames.SequenceEqual(
                    sourceNames,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Readable source and placed Revolution bone orders differ.");
            }
        }

        private static BarrelSelection SelectBarrelComponents(
            Mesh sourceMesh,
            SkinnedMeshRenderer targetRenderer,
            IReadOnlyList<Vector3> bakedWorldVertices,
            string forearmName,
            string handName,
            Vector3? fixedRotationAxis = null)
        {
            if (sourceMesh.vertexCount != bakedWorldVertices.Count)
            {
                throw new InvalidOperationException(
                    handName +
                    " source and baked vertex counts differ.");
            }
            var forearm = RequireBone(targetRenderer, forearmName);
            var hand = RequireBone(targetRenderer, handName);
            var approximateAxis = hand.position - forearm.position;
            if (approximateAxis.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    handName + " does not define a gun axis.");
            }
            approximateAxis.Normalize();
            var handBoneIndex =
                Array.FindIndex(
                    targetRenderer.bones,
                    item => item == hand);
            if (handBoneIndex < 0)
            {
                throw new InvalidOperationException(
                    handName + " is missing from the renderer bone array.");
            }
            var boneWeights = sourceMesh.boneWeights;
            if (boneWeights.Length != sourceMesh.vertexCount)
            {
                throw new InvalidOperationException(
                    handName +
                    " readable source does not expose legacy four-weight skinning.");
            }

            var componentIndices = ConnectedComponents(sourceMesh);
            var components = componentIndices
                .Select(indices =>
                    DescribeComponent(
                        indices,
                        bakedWorldVertices,
                        boneWeights,
                        handBoneIndex,
                        hand.position,
                        approximateAxis))
                .Where(item =>
                    item.HandWeight >= 0.45f &&
                    item.MaximumProjection > 0.04f)
                .ToArray();
            if (components.Length == 0)
            {
                throw new InvalidOperationException(
                    handName +
                    " has no forward hand-weighted gun components.");
            }
            var selected = components
                .Where(item =>
                    item.MeanRadialDistance >= 0.008f &&
                    (item.ProjectionLength >= 0.025f ||
                     item.MeanProjection >= 0.04f))
                .ToArray();
            if (selected.Length < 3)
            {
                throw new InvalidOperationException(
                    handName +
                    " barrel ring component selection is insufficient. CandidateComponents=" +
                    components.Length + ", SelectedComponents=" +
                    selected.Length + ".");
            }
            var initialVertices =
                new HashSet<int>(
                    selected.SelectMany(item => item.VertexIndices));
            if (initialVertices.Count < 12)
            {
                throw new InvalidOperationException(
                    handName +
                    " barrel ring vertex selection is insufficient. VertexCount=" +
                    initialVertices.Count + ".");
            }
            var axisPoint = initialVertices
                                .Select(index =>
                                    bakedWorldVertices[index])
                                .Aggregate(
                                    Vector3.zero,
                                    (sum, point) => sum + point) /
                            initialVertices.Count;
            var provisionalMuzzleCandidates = SelectMuzzleRingCluster(
                selected
                    .Select(component =>
                        ComponentMuzzle(
                            component,
                            bakedWorldVertices,
                            axisPoint,
                            approximateAxis))
                    .ToArray());
            var detectedAxis = BarrelRingPlaneNormal(
                provisionalMuzzleCandidates
                    .Select(candidate => candidate.Position)
                    .ToArray(),
                approximateAxis);
            var axis =
                fixedRotationAxis?.normalized ??
                detectedAxis;
            if (Vector3.Dot(axis, approximateAxis) < 0f)
            {
                axis = -axis;
            }
            var vertices = initialVertices;
            var muzzleCandidates = SelectMuzzleRingCluster(
                selected
                    .Select(component =>
                        ComponentMuzzle(
                            component,
                            bakedWorldVertices,
                            axisPoint,
                            axis))
                    .ToArray());
            var up = Vector3.ProjectOnPlane(
                Vector3.up,
                axis);
            if (up.sqrMagnitude < 0.000001f)
            {
                up = Vector3.ProjectOnPlane(
                    targetRenderer.transform.right,
                    axis);
            }
            up.Normalize();
            var muzzle = muzzleCandidates
                .OrderByDescending(candidate =>
                    Vector3.Dot(
                        candidate.Position - axisPoint,
                        up))
                .First()
                .Position;
            var meanBarrelOrbitRadius = selected
                .Select(component =>
                    Vector3.ProjectOnPlane(
                        ComponentCenter(
                            component,
                            bakedWorldVertices) -
                        axisPoint,
                        axis).magnitude)
                .Average();
            return new BarrelSelection(
                vertices,
                selected.Length,
                axis,
                axisPoint,
                muzzle,
                meanBarrelOrbitRadius);
        }

        private static BarrelSelection
            SelectCompleteRightBarrelAssembly(
                Mesh sourceMesh,
                SkinnedMeshRenderer targetRenderer,
                IReadOnlyList<Vector3> bakedWorldVertices,
                string forearmName,
                string handName)
        {
            if (sourceMesh.vertexCount !=
                bakedWorldVertices.Count)
            {
                throw new InvalidOperationException(
                    handName +
                    " source and baked vertex counts differ.");
            }
            var forearm =
                RequireBone(targetRenderer, forearmName);
            var hand =
                RequireBone(targetRenderer, handName);
            var approximateAxis =
                hand.position - forearm.position;
            if (approximateAxis.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    handName + " does not define a gun axis.");
            }
            approximateAxis.Normalize();
            var handBoneIndex = Array.FindIndex(
                targetRenderer.bones,
                item => item == hand);
            if (handBoneIndex < 0)
            {
                throw new InvalidOperationException(
                    handName +
                    " is missing from the renderer bone array.");
            }
            var boneWeights = sourceMesh.boneWeights;
            if (boneWeights.Length !=
                sourceMesh.vertexCount)
            {
                throw new InvalidOperationException(
                    handName +
                    " readable source does not expose legacy four-weight skinning.");
            }

            // The right model has different disconnected topology from
            // the left. Select its ten longitudinal components, then
            // pair them into five complete barrels.
            var forwardComponents =
                ConnectedComponents(sourceMesh)
                    .Select(indices =>
                        DescribeComponent(
                            indices,
                            bakedWorldVertices,
                            boneWeights,
                            handBoneIndex,
                            hand.position,
                            approximateAxis))
                    .Where(item =>
                        item.MaximumProjection > 0.04f)
                    .ToArray();
            var barrelCores = forwardComponents
                .Where(item =>
                    item.MeanRadialDistance >= 0.008f &&
                    item.ProjectionLength >= 0.025f)
                .OrderByDescending(item =>
                    item.ProjectionLength)
                .Take(10)
                .ToArray();
            if (barrelCores.Length < 10)
            {
                throw new InvalidOperationException(
                    handName +
                    " complete barrel assembly does not expose all ten longitudinal barrel components.");
            }
            var initialVertices = new HashSet<int>(
                barrelCores.SelectMany(item =>
                    item.VertexIndices));
            var selectedPoints = initialVertices
                .Select(index =>
                    bakedWorldVertices[index])
                .ToArray();
            var center = selectedPoints.Aggregate(
                             Vector3.zero,
                             (sum, point) => sum + point) /
                         selectedPoints.Length;
            var projections = selectedPoints
                .Select(point =>
                    Vector3.Dot(
                        point - center,
                        approximateAxis))
                .ToArray();
            var minimumProjection = projections.Min();
            var maximumProjection = projections.Max();
            var endBand = Mathf.Max(
                0.003f,
                (maximumProjection - minimumProjection) *
                0.12f);
            var rearCenter = selectedPoints
                .Where((point, index) =>
                    projections[index] <=
                    minimumProjection + endBand)
                .Aggregate(
                    Vector3.zero,
                    (sum, point) => sum + point);
            var rearCount = projections.Count(value =>
                value <= minimumProjection + endBand);
            rearCenter /= rearCount;
            var frontCenter = selectedPoints
                .Where((point, index) =>
                    projections[index] >=
                    maximumProjection - endBand)
                .Aggregate(
                    Vector3.zero,
                    (sum, point) => sum + point);
            var frontCount = projections.Count(value =>
                value >= maximumProjection - endBand);
            frontCenter /= frontCount;
            var axis = frontCenter - rearCenter;
            if (axis.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    handName +
                    " complete barrel assembly does not define front and rear ring centers.");
            }
            axis.Normalize();
            if (Vector3.Dot(axis, approximateAxis) < 0f)
            {
                axis = -axis;
            }
            var vertexGroups = PairRightBarrelComponents(
                barrelCores,
                bakedWorldVertices,
                center,
                axis);
            var muzzleRing = SelectMuzzleRingCluster(
                barrelCores
                    .Select(component =>
                        ComponentMuzzle(
                            component,
                            bakedWorldVertices,
                            hand.position,
                            axis))
                    .ToArray());
            var axisPoint = vertexGroups
                .Select(group =>
                    group
                        .Select(index =>
                            bakedWorldVertices[index])
                        .Aggregate(
                            Vector3.zero,
                            (sum, point) => sum + point) /
                    group.Count)
                .Aggregate(
                    Vector3.zero,
                    (sum, point) => sum + point) /
                vertexGroups.Count;
            var vertices = initialVertices;
            if (vertices.Count < 12)
            {
                throw new InvalidOperationException(
                    handName +
                    " complete barrel assembly selection is insufficient.");
            }
            var up = Vector3.ProjectOnPlane(
                Vector3.up,
                axis);
            if (up.sqrMagnitude < 0.000001f)
            {
                up = Vector3.ProjectOnPlane(
                    targetRenderer.transform.right,
                    axis);
            }
            up.Normalize();
            var muzzle = muzzleRing
                .OrderByDescending(candidate =>
                    Vector3.Dot(
                        candidate.Position - axisPoint,
                        up))
                .First()
                .Position;
            var meanBarrelOrbitRadius = barrelCores
                .Select(component =>
                    Vector3.ProjectOnPlane(
                        ComponentCenter(
                            component,
                            bakedWorldVertices) -
                        axisPoint,
                        axis).magnitude)
                .Average();
            return new BarrelSelection(
                vertices,
                barrelCores.Length,
                axis,
                axisPoint,
                muzzle,
                meanBarrelOrbitRadius,
                vertexGroups);
        }

        private static IReadOnlyList<HashSet<int>>
            PairRightBarrelComponents(
                IReadOnlyList<ComponentDescription> components,
                IReadOnlyList<Vector3> worldVertices,
                Vector3 ringCenter,
                Vector3 axis)
        {
            if (components.Count !=
                RightBarrelGroupCount * 2)
            {
                throw new InvalidOperationException(
                    "The right barrel ring must expose two longitudinal mesh components per barrel.");
            }
            var remaining = components.ToList();
            var groups = new List<HashSet<int>>(
                RightBarrelGroupCount);
            while (remaining.Count > 0)
            {
                var seed = remaining[0];
                remaining.RemoveAt(0);
                var seedCenter = ComponentCenter(
                    seed,
                    worldVertices);
                var seedRadial = Vector3.ProjectOnPlane(
                    seedCenter - ringCenter,
                    axis);
                var partner = remaining
                    .OrderBy(candidate =>
                    {
                        var candidateRadial =
                            Vector3.ProjectOnPlane(
                                ComponentCenter(
                                    candidate,
                                    worldVertices) -
                                ringCenter,
                                axis);
                        return (
                            candidateRadial -
                            seedRadial).sqrMagnitude;
                    })
                    .First();
                remaining.Remove(partner);
                var group = new HashSet<int>(
                    seed.VertexIndices);
                group.UnionWith(
                    partner.VertexIndices);
                groups.Add(group);
            }
            if (groups.Count != RightBarrelGroupCount)
            {
                throw new InvalidOperationException(
                    "The right barrel components could not be paired into five complete barrels.");
            }
            return groups;
        }

        private static Vector3 ComponentCenter(
            ComponentDescription component,
            IReadOnlyList<Vector3> worldVertices)
        {
            return component.VertexIndices
                       .Select(index =>
                           worldVertices[index])
                       .Aggregate(
                           Vector3.zero,
                           (sum, point) => sum + point) /
                   component.VertexIndices.Length;
        }

        private static MuzzleCandidate[] SelectMuzzleRingCluster(
            IReadOnlyList<MuzzleCandidate> candidates)
        {
            const float projectionBand = 0.025f;
            var clusters = candidates
                .Select(seed =>
                    candidates
                        .Where(candidate =>
                            Mathf.Abs(
                                candidate.Projection -
                                seed.Projection) <=
                            projectionBand)
                        .ToArray())
                .Where(cluster => cluster.Length >= 3)
                .OrderByDescending(cluster => cluster.Length)
                .ThenByDescending(cluster =>
                    cluster.Average(candidate =>
                        candidate.Projection))
                .ToArray();
            if (clusters.Length == 0)
            {
                throw new InvalidOperationException(
                    "Revolution barrel components do not expose a shared muzzle-ring depth.");
            }
            return clusters[0];
        }

        private static Vector3 AverageBarrelComponentAxis(
            IReadOnlyList<ComponentDescription> components,
            IReadOnlyList<Vector3> worldVertices,
            Vector3 approximateAxis)
        {
            var elongated = components
                .Where(component =>
                    component.ProjectionLength >= 0.025f)
                .ToArray();
            var candidates = elongated.Length >= 3
                ? elongated
                : components.ToArray();
            var axis = Vector3.zero;
            foreach (var component in candidates)
            {
                var componentAxis = DominantComponentAxis(
                    component,
                    worldVertices,
                    approximateAxis);
                if (Vector3.Dot(
                        componentAxis,
                        approximateAxis) < 0f)
                {
                    componentAxis = -componentAxis;
                }
                axis += componentAxis;
            }
            if (axis.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Revolution barrel components do not define a common longitudinal axis.");
            }
            axis.Normalize();
            return axis;
        }

        private static Vector3 DominantComponentAxis(
            ComponentDescription component,
            IReadOnlyList<Vector3> worldVertices,
            Vector3 fallbackAxis)
        {
            var points = component.VertexIndices
                .Select(index => worldVertices[index])
                .ToArray();
            var center = points.Aggregate(
                             Vector3.zero,
                             (sum, point) => sum + point) /
                         points.Length;
            var axis = fallbackAxis.normalized;
            for (var iteration = 0;
                 iteration < 12;
                 iteration++)
            {
                var next = Vector3.zero;
                foreach (var point in points)
                {
                    var offset = point - center;
                    next += offset *
                            Vector3.Dot(offset, axis);
                }
                if (next.sqrMagnitude < 0.000000001f)
                {
                    return fallbackAxis.normalized;
                }
                axis = next.normalized;
            }
            return axis;
        }

        private static MuzzleCandidate ComponentMuzzle(
            ComponentDescription component,
            IReadOnlyList<Vector3> worldVertices,
            Vector3 axisPoint,
            Vector3 axis)
        {
            var points = component.VertexIndices
                .Select(index => worldVertices[index])
                .ToArray();
            var projections = points
                .Select(point =>
                    Vector3.Dot(
                        point - axisPoint,
                        axis))
                .ToArray();
            var maximum = projections.Max();
            var minimum = projections.Min();
            var endDepth = Mathf.Max(
                0.0015f,
                (maximum - minimum) * 0.12f);
            var endPoints = points
                .Where((point, index) =>
                    projections[index] >= maximum - endDepth)
                .ToArray();
            return new MuzzleCandidate(
                endPoints.Aggregate(
                    Vector3.zero,
                    (sum, point) => sum + point) /
                endPoints.Length,
                maximum);
        }

        private static Vector3 BarrelRingPlaneNormal(
            IReadOnlyList<Vector3> muzzleCenters,
            Vector3 approximateAxis)
        {
            var center = muzzleCenters.Aggregate(
                             Vector3.zero,
                             (sum, point) => sum + point) /
                         muzzleCenters.Count;
            var offsets = muzzleCenters
                .Select(point => point - center)
                .ToArray();
            var crosses = new List<Vector3>();
            var reference = Vector3.zero;
            for (var first = 0;
                 first < offsets.Length - 1;
                 first++)
            {
                for (var second = first + 1;
                     second < offsets.Length;
                     second++)
                {
                    var cross =
                        Vector3.Cross(
                            offsets[first],
                            offsets[second]);
                    if (cross.sqrMagnitude <
                        0.0000000001f)
                    {
                        continue;
                    }
                    crosses.Add(cross);
                    if (cross.sqrMagnitude >
                        reference.sqrMagnitude)
                    {
                        reference = cross;
                    }
                }
            }
            if (reference.sqrMagnitude <
                0.0000000001f)
            {
                throw new InvalidOperationException(
                    "Revolution muzzle centers do not define a rotation plane.");
            }
            if (Vector3.Dot(
                    reference,
                    approximateAxis) < 0f)
            {
                reference = -reference;
            }
            var normal = Vector3.zero;
            foreach (var cross in crosses)
            {
                normal +=
                    (Vector3.Dot(
                         cross,
                         reference) < 0f
                        ? -cross
                        : cross).normalized;
            }
            if (normal.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "Revolution muzzle rotation-plane normal is unavailable.");
            }
            normal.Normalize();
            if (Vector3.Dot(
                    normal,
                    approximateAxis) < 0f)
            {
                normal = -normal;
            }
            return normal;
        }

        private static IReadOnlyList<int[]> ConnectedComponents(
            Mesh mesh)
        {
            var parent = Enumerable.Range(0, mesh.vertexCount).ToArray();
            for (var subMesh = 0;
                 subMesh < mesh.subMeshCount;
                 subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                for (var index = 0;
                     index < triangles.Length;
                     index += 3)
                {
                    Union(parent, triangles[index], triangles[index + 1]);
                    Union(parent, triangles[index], triangles[index + 2]);
                }
            }
            return Enumerable.Range(0, mesh.vertexCount)
                .GroupBy(index => Find(parent, index))
                .Select(group => group.ToArray())
                .ToArray();
        }

        private static int Find(int[] parent, int value)
        {
            var root = value;
            while (parent[root] != root)
            {
                root = parent[root];
            }
            while (parent[value] != value)
            {
                var next = parent[value];
                parent[value] = root;
                value = next;
            }
            return root;
        }

        private static void Union(int[] parent, int first, int second)
        {
            var firstRoot = Find(parent, first);
            var secondRoot = Find(parent, second);
            if (firstRoot != secondRoot)
            {
                parent[secondRoot] = firstRoot;
            }
        }

        private static ComponentDescription DescribeComponent(
            int[] indices,
            IReadOnlyList<Vector3> worldVertices,
            IReadOnlyList<BoneWeight> boneWeights,
            int handBoneIndex,
            Vector3 handPosition,
            Vector3 axis)
        {
            var projections = new float[indices.Length];
            var radialTotal = 0f;
            var handWeightTotal = 0f;
            for (var item = 0;
                 item < indices.Length;
                 item++)
            {
                var vertexIndex = indices[item];
                var offset =
                    worldVertices[vertexIndex] - handPosition;
                projections[item] = Vector3.Dot(offset, axis);
                radialTotal +=
                    Vector3.ProjectOnPlane(offset, axis).magnitude;
                handWeightTotal +=
                    WeightForBone(
                        boneWeights[vertexIndex],
                        handBoneIndex);
            }
            return new ComponentDescription(
                indices,
                projections.Min(),
                projections.Max(),
                projections.Average(),
                radialTotal / indices.Length,
                handWeightTotal / indices.Length);
        }

        private static float WeightForBone(
            BoneWeight weight,
            int boneIndex)
        {
            var result = 0f;
            if (weight.boneIndex0 == boneIndex)
            {
                result += weight.weight0;
            }
            if (weight.boneIndex1 == boneIndex)
            {
                result += weight.weight1;
            }
            if (weight.boneIndex2 == boneIndex)
            {
                result += weight.weight2;
            }
            if (weight.boneIndex3 == boneIndex)
            {
                result += weight.weight3;
            }
            return result;
        }

        private static GunRig CreateBarrelRig(
            Transform model,
            string forearmName,
            string handName,
            string spinBoneName,
            string flashPivotName,
            string flashName,
            BarrelSelection selection,
            Mesh flashMesh,
            Material flashMaterial)
        {
            var forearm = RequireDescendant(model, forearmName);
            var hand = RequireDescendant(model, handName);
            if (hand.parent != forearm)
            {
                throw new InvalidOperationException(
                    handName + " must remain a direct child of " +
                    forearmName + ".");
            }
            var up = Vector3.ProjectOnPlane(
                Vector3.up,
                selection.Axis);
            if (up.sqrMagnitude < 0.000001f)
            {
                up = Vector3.ProjectOnPlane(
                    model.right,
                    selection.Axis);
            }
            up.Normalize();

            var spinObject = new GameObject(spinBoneName);
            Undo.RegisterCreatedObjectUndo(
                spinObject,
                "Create Revolution barrel ring bone");
            var spin = spinObject.transform;
            spin.SetParent(hand, true);
            spin.position = selection.AxisPoint;
            spin.rotation =
                Quaternion.LookRotation(selection.Axis, up);
            spin.localScale = Vector3.one;

            var flashPivotObject = new GameObject(flashPivotName);
            Undo.RegisterCreatedObjectUndo(
                flashPivotObject,
                "Create Revolution fixed muzzle flash pivot");
            var flashPivot = flashPivotObject.transform;
            flashPivot.SetParent(hand, true);
            flashPivot.position =
                selection.MuzzlePosition +
                (selection.Axis * FlashMuzzleOffset);
            flashPivot.rotation =
                Quaternion.LookRotation(selection.Axis, up);
            var parentScale = hand.lossyScale;
            if (Mathf.Abs(parentScale.x) < 0.000001f ||
                Mathf.Abs(parentScale.y) < 0.000001f ||
                Mathf.Abs(parentScale.z) < 0.000001f)
            {
                throw new InvalidOperationException(
                    handName +
                    " cannot support a world-scale muzzle flash.");
            }
            flashPivot.localScale = new Vector3(
                1f / Mathf.Abs(parentScale.x),
                1f / Mathf.Abs(parentScale.y),
                1f / Mathf.Abs(parentScale.z));

            var flashObject = new GameObject(
                flashName,
                typeof(MeshFilter),
                typeof(MeshRenderer));
            Undo.RegisterCreatedObjectUndo(
                flashObject,
                "Create Revolution fixed muzzle flash");
            var flash = flashObject.transform;
            flash.SetParent(flashPivot, false);
            flash.localPosition = Vector3.zero;
            flash.localRotation = Quaternion.identity;
            flash.localScale = Vector3.one;
            flashObject.GetComponent<MeshFilter>().sharedMesh = flashMesh;
            var flashRenderer =
                flashObject.GetComponent<MeshRenderer>();
            flashRenderer.sharedMaterial = flashMaterial;
            flashRenderer.shadowCastingMode =
                ShadowCastingMode.Off;
            flashRenderer.receiveShadows = false;
            flashRenderer.lightProbeUsage = LightProbeUsage.Off;
            flashRenderer.reflectionProbeUsage =
                ReflectionProbeUsage.Off;
            EditorUtility.SetDirty(spinObject);
            EditorUtility.SetDirty(flashPivotObject);
            EditorUtility.SetDirty(flashObject);
            return new GunRig(
                forearm,
                hand,
                spin,
                flashPivot,
                flash,
                selection.Axis,
                selection.MuzzlePosition,
                spin.localRotation,
                selection.VertexIndices.Count,
                selection.ComponentCount);
        }

        private static Mesh CreateRiggedMesh(
            Transform model,
            Mesh source,
            SkinnedMeshRenderer renderer,
            GunRig left,
            GunRig right,
            BarrelSelection leftSelection,
            BarrelSelection rightSelection,
            AimPose aimPose)
        {
            DeleteAssetIfPresent(RiggedMeshPath);
            var derived =
                UnityEngine.Object.Instantiate(source);
            derived.name =
                "Revolution_04_MachineGun_RiggedMesh";
            var baseBindPoses = source.bindposes;
            if (baseBindPoses.Length != ExpectedBaseBones)
            {
                UnityEngine.Object.DestroyImmediate(derived);
                throw new InvalidOperationException(
                    "Readable Revolution source bind-pose count differs.");
            }
            derived.bindposes = baseBindPoses
                .Concat(
                    new[]
                    {
                        left.SpinBone.worldToLocalMatrix *
                        renderer.localToWorldMatrix,
                        right.SpinBone.worldToLocalMatrix *
                        renderer.localToWorldMatrix
                    })
                .ToArray();
            var weights = source.boneWeights;
            if (weights.Length != source.vertexCount)
            {
                UnityEngine.Object.DestroyImmediate(derived);
                throw new InvalidOperationException(
                    "Readable Revolution source bone weights are unavailable.");
            }
            var leftIndex = ExpectedBaseBones;
            var rightIndex = ExpectedBaseBones + 1;
            foreach (var vertex in leftSelection.VertexIndices)
            {
                weights[vertex] = FullWeight(leftIndex);
            }
            foreach (var vertex in rightSelection.VertexIndices)
            {
                weights[vertex] = FullWeight(rightIndex);
            }
            SetExplicitBoneWeights(derived, weights);
            var extractedVertices = new HashSet<int>(
                leftSelection.VertexIndices);
            extractedVertices.UnionWith(
                rightSelection.VertexIndices);
            for (var subMesh = 0;
                 subMesh < source.subMeshCount;
                 subMesh++)
            {
                var sourceTriangles =
                    source.GetTriangles(subMesh);
                var remainingTriangles = new List<int>(
                    sourceTriangles.Length);
                for (var index = 0;
                     index < sourceTriangles.Length;
                     index += 3)
                {
                    if (extractedVertices.Contains(
                            sourceTriangles[index]) &&
                        extractedVertices.Contains(
                            sourceTriangles[index + 1]) &&
                        extractedVertices.Contains(
                            sourceTriangles[index + 2]))
                    {
                        continue;
                    }
                    remainingTriangles.Add(
                        sourceTriangles[index]);
                    remainingTriangles.Add(
                        sourceTriangles[index + 1]);
                    remainingTriangles.Add(
                        sourceTriangles[index + 2]);
                }
                derived.SetTriangles(
                    remainingTriangles,
                    subMesh,
                    false);
            }
            derived.RecalculateBounds();
            AssetDatabase.CreateAsset(derived, RiggedMeshPath);
            EditorUtility.SetDirty(derived);
            Mesh leftBarrels;
            var poseSnapshots =
                model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            try
            {
                foreach (var boneName in FixedAimBoneNames)
                {
                    RequireDescendant(model, boneName)
                        .localRotation =
                        aimPose.LocalRotations[boneName];
                }
                leftBarrels = CreateExtractedBarrelMesh(
                    source,
                    renderer,
                    left.SpinBone,
                    leftSelection,
                    LeftBarrelMeshPath,
                    "Revolution_04_Left_MachineGun_Barrels");
                DeletePreviousRightBarrelGroupAssets();
                DeleteAssetIfPresent(RightBarrelMeshPath);
                if (rightSelection.VertexGroups.Count !=
                    RightBarrelGroupCount)
                {
                    throw new InvalidOperationException(
                        "The right barrel selection does not contain five complete counter-rotating groups.");
                }
                for (var groupIndex = 0;
                     groupIndex <
                     rightSelection.VertexGroups.Count;
                     groupIndex++)
                {
                    var groupVertices =
                        rightSelection.VertexGroups[
                            groupIndex];
                    var suffix = (groupIndex + 1)
                        .ToString(
                            "D2",
                            CultureInfo.InvariantCulture);
                    var groupObject = new GameObject(
                        RightBarrelGroupNamePrefix +
                        suffix);
                    Undo.RegisterCreatedObjectUndo(
                        groupObject,
                        "Create Revolution right barrel ring group");
                    var groupTransform =
                        groupObject.transform;
                    groupTransform.SetParent(
                        right.SpinBone,
                        true);
                    var originalGroupCenter =
                        groupVertices
                            .Select(vertex =>
                                SkinWorldPoint(
                                    source.vertices[vertex],
                                    source.boneWeights[
                                        vertex],
                                    source.bindposes,
                                    renderer.bones))
                            .Aggregate(
                                Vector3.zero,
                                (sum, point) =>
                                    sum + point) /
                        groupVertices.Count;
                    groupTransform.position =
                        originalGroupCenter;
                    groupTransform.rotation =
                        right.SpinBone.rotation;
                    groupTransform.localScale =
                        Vector3.one;
                    var groupMesh =
                        CreateExtractedBarrelMesh(
                            source,
                            renderer,
                            groupTransform,
                            groupVertices,
                            RightBarrelGroupAssetPrefix +
                            suffix + ".asset",
                            RightBarrelGroupNamePrefix +
                            suffix);
                    var firingAxis =
                        right.SpinBone.forward;
                    var axialCenter =
                        right.SpinBone.position +
                        Vector3.Project(
                            originalGroupCenter -
                            right.SpinBone.position,
                            firingAxis);
                    var radialDirection =
                        Vector3.ProjectOnPlane(
                            originalGroupCenter -
                            right.SpinBone.position,
                            firingAxis);
                    if (radialDirection.sqrMagnitude <
                        0.000001f)
                    {
                        throw new InvalidOperationException(
                            "A right barrel group cannot be placed on " +
                            "the normal left-arm ring radius.");
                    }
                    groupTransform.position =
                        axialCenter +
                        (radialDirection.normalized *
                         leftSelection.MeanBarrelOrbitRadius);
                    AttachExtractedBarrelRenderer(
                        groupTransform,
                        groupMesh,
                        renderer.sharedMaterials);
                    right.CounterRotatingBarrelGroups.Add(
                        new CounterRotatingBarrelGroup(
                            groupTransform,
                            groupTransform.localRotation));
                }
            }
            finally
            {
                RestoreAll(poseSnapshots);
            }
            AttachExtractedBarrelRenderer(
                left.SpinBone,
                leftBarrels,
                renderer.sharedMaterials);
            return derived;
        }

        private static void
            DeletePreviousRightBarrelGroupAssets()
        {
            for (var group = 1; group <= 32; group++)
            {
                var suffix = group.ToString(
                    "D2",
                    CultureInfo.InvariantCulture);
                DeleteAssetIfPresent(
                    AnimationFolder +
                    "/Revolution_04_Right_MachineGun_BarrelGroup_" +
                    suffix + ".asset");
            }
        }

        private static Mesh CreateExtractedBarrelMesh(
            Mesh source,
            SkinnedMeshRenderer renderer,
            Transform spinBone,
            BarrelSelection selection,
            string assetPath,
            string assetName)
        {
            return CreateExtractedBarrelMesh(
                source,
                renderer,
                spinBone,
                selection.VertexIndices,
                assetPath,
                assetName);
        }

        private static Mesh CreateExtractedBarrelMesh(
            Mesh source,
            SkinnedMeshRenderer renderer,
            Transform localSpace,
            HashSet<int> selectedVertices,
            string assetPath,
            string assetName)
        {
            DeleteAssetIfPresent(assetPath);
            var extracted =
                UnityEngine.Object.Instantiate(source);
            extracted.name = assetName;
            var sourceWeights = source.boneWeights;
            var sourceBindPoses = source.bindposes;
            var sourceBones = renderer.bones;
            var bakedVertices = source.vertices;
            for (var vertex = 0;
                 vertex < bakedVertices.Length;
                 vertex++)
            {
                bakedVertices[vertex] =
                    localSpace.InverseTransformPoint(
                        SkinWorldPoint(
                            bakedVertices[vertex],
                            sourceWeights[vertex],
                            sourceBindPoses,
                            sourceBones));
            }
            extracted.vertices = bakedVertices;
            var bakedNormals = source.normals;
            if (bakedNormals.Length == extracted.vertexCount)
            {
                for (var vertex = 0;
                     vertex < bakedNormals.Length;
                     vertex++)
                {
                    bakedNormals[vertex] =
                        localSpace.InverseTransformDirection(
                                SkinWorldDirection(
                                    bakedNormals[vertex],
                                    sourceWeights[vertex],
                                    sourceBindPoses,
                                    sourceBones))
                            .normalized;
                }
                extracted.normals = bakedNormals;
            }
            var bakedTangents = source.tangents;
            if (bakedTangents.Length == extracted.vertexCount)
            {
                for (var vertex = 0;
                     vertex < bakedTangents.Length;
                     vertex++)
                {
                    var tangent = localSpace
                        .InverseTransformDirection(
                            SkinWorldDirection(
                                new Vector3(
                                    bakedTangents[vertex].x,
                                    bakedTangents[vertex].y,
                                    bakedTangents[vertex].z),
                                sourceWeights[vertex],
                                sourceBindPoses,
                                sourceBones))
                        .normalized;
                    bakedTangents[vertex] = new Vector4(
                        tangent.x,
                        tangent.y,
                        tangent.z,
                        bakedTangents[vertex].w);
                }
                extracted.tangents = bakedTangents;
            }
            for (var subMesh = 0;
                 subMesh < source.subMeshCount;
                 subMesh++)
            {
                var sourceTriangles =
                    source.GetTriangles(subMesh);
                var selectedTriangles = new List<int>();
                for (var index = 0;
                     index < sourceTriangles.Length;
                     index += 3)
                {
                    if (!selectedVertices.Contains(
                            sourceTriangles[index]) ||
                        !selectedVertices.Contains(
                            sourceTriangles[index + 1]) ||
                        !selectedVertices.Contains(
                            sourceTriangles[index + 2]))
                    {
                        continue;
                    }
                    selectedTriangles.Add(
                        sourceTriangles[index]);
                    selectedTriangles.Add(
                        sourceTriangles[index + 1]);
                    selectedTriangles.Add(
                        sourceTriangles[index + 2]);
                }
                extracted.SetTriangles(
                    selectedTriangles,
                    subMesh,
                    false);
            }
            extracted.RecalculateBounds();
            AssetDatabase.CreateAsset(extracted, assetPath);
            EditorUtility.SetDirty(extracted);
            return extracted;
        }

        private static Vector3 SkinWorldPoint(
            Vector3 point,
            BoneWeight weight,
            IReadOnlyList<Matrix4x4> bindPoses,
            IReadOnlyList<Transform> bones)
        {
            var result = Vector3.zero;
            AddSkinnedPoint(
                ref result,
                point,
                weight.boneIndex0,
                weight.weight0,
                bindPoses,
                bones);
            AddSkinnedPoint(
                ref result,
                point,
                weight.boneIndex1,
                weight.weight1,
                bindPoses,
                bones);
            AddSkinnedPoint(
                ref result,
                point,
                weight.boneIndex2,
                weight.weight2,
                bindPoses,
                bones);
            AddSkinnedPoint(
                ref result,
                point,
                weight.boneIndex3,
                weight.weight3,
                bindPoses,
                bones);
            return result;
        }

        private static void AddSkinnedPoint(
            ref Vector3 result,
            Vector3 point,
            int boneIndex,
            float weight,
            IReadOnlyList<Matrix4x4> bindPoses,
            IReadOnlyList<Transform> bones)
        {
            if (weight <= 0f)
            {
                return;
            }
            result += bones[boneIndex]
                      .localToWorldMatrix
                      .MultiplyPoint3x4(
                          bindPoses[boneIndex]
                              .MultiplyPoint3x4(point)) *
                      weight;
        }

        private static Vector3 SkinWorldDirection(
            Vector3 direction,
            BoneWeight weight,
            IReadOnlyList<Matrix4x4> bindPoses,
            IReadOnlyList<Transform> bones)
        {
            var result = Vector3.zero;
            AddSkinnedDirection(
                ref result,
                direction,
                weight.boneIndex0,
                weight.weight0,
                bindPoses,
                bones);
            AddSkinnedDirection(
                ref result,
                direction,
                weight.boneIndex1,
                weight.weight1,
                bindPoses,
                bones);
            AddSkinnedDirection(
                ref result,
                direction,
                weight.boneIndex2,
                weight.weight2,
                bindPoses,
                bones);
            AddSkinnedDirection(
                ref result,
                direction,
                weight.boneIndex3,
                weight.weight3,
                bindPoses,
                bones);
            return result;
        }

        private static void AddSkinnedDirection(
            ref Vector3 result,
            Vector3 direction,
            int boneIndex,
            float weight,
            IReadOnlyList<Matrix4x4> bindPoses,
            IReadOnlyList<Transform> bones)
        {
            if (weight <= 0f)
            {
                return;
            }
            result += bones[boneIndex]
                      .localToWorldMatrix
                      .MultiplyVector(
                          bindPoses[boneIndex]
                              .MultiplyVector(direction)) *
                      weight;
        }

        private static void AttachExtractedBarrelRenderer(
            Transform spinBone,
            Mesh barrelMesh,
            IReadOnlyList<Material> materials)
        {
            var filter =
                spinBone.gameObject.AddComponent<MeshFilter>();
            var renderer =
                spinBone.gameObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = barrelMesh;
            renderer.sharedMaterials = materials.ToArray();
            renderer.shadowCastingMode =
                ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage =
                LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage =
                ReflectionProbeUsage.BlendProbes;
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(renderer);
        }

        private static BoneWeight FullWeight(int boneIndex)
        {
            return new BoneWeight
            {
                boneIndex0 = boneIndex,
                weight0 = 1f,
                boneIndex1 = 0,
                weight1 = 0f,
                boneIndex2 = 0,
                weight2 = 0f,
                boneIndex3 = 0,
                weight3 = 0f
            };
        }

        private static void SetExplicitBoneWeights(
            Mesh mesh,
            IReadOnlyList<BoneWeight> legacyWeights)
        {
            var influenceCounts = new byte[legacyWeights.Count];
            var influences = new List<BoneWeight1>(
                legacyWeights.Count * 4);
            for (var vertex = 0;
                 vertex < legacyWeights.Count;
                 vertex++)
            {
                var weight = legacyWeights[vertex];
                AddExplicitBoneWeight(
                    influences,
                    influenceCounts,
                    vertex,
                    weight.boneIndex0,
                    weight.weight0);
                AddExplicitBoneWeight(
                    influences,
                    influenceCounts,
                    vertex,
                    weight.boneIndex1,
                    weight.weight1);
                AddExplicitBoneWeight(
                    influences,
                    influenceCounts,
                    vertex,
                    weight.boneIndex2,
                    weight.weight2);
                AddExplicitBoneWeight(
                    influences,
                    influenceCounts,
                    vertex,
                    weight.boneIndex3,
                    weight.weight3);
            }
            using var bonesPerVertex = new NativeArray<byte>(
                influenceCounts,
                Allocator.Temp);
            using var allWeights = new NativeArray<BoneWeight1>(
                influences.ToArray(),
                Allocator.Temp);
            mesh.SetBoneWeights(bonesPerVertex, allWeights);
        }

        private static void AddExplicitBoneWeight(
            ICollection<BoneWeight1> influences,
            IList<byte> influenceCounts,
            int vertex,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f)
            {
                return;
            }
            influences.Add(
                new BoneWeight1
                {
                    boneIndex = boneIndex,
                    weight = weight
                });
            influenceCounts[vertex]++;
        }

        private static AimPose CalculateForwardAimPose(
            Transform slot,
            Transform model,
            GunRig left,
            GunRig right)
        {
            var snapshots =
                model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            try
            {
                var forward =
                    Vector3.ProjectOnPlane(
                        slot.forward,
                        Vector3.up);
                if (forward.sqrMagnitude < 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Revolution_04 does not define a horizontal forward direction.");
                }
                forward.Normalize();
                AimArm(model, left, forward);
                MirrorLeftArmPoseToRight(model, left, right);
                var rotations = FixedAimBoneNames.ToDictionary(
                    name => name,
                    name =>
                        RequireDescendant(model, name).localRotation,
                    StringComparer.Ordinal);
                var leftError =
                    Vector3.Angle(left.SpinBone.forward, forward);
                var rightError =
                    Vector3.Angle(right.SpinBone.forward, forward);
                if (leftError > 0.5f || rightError > 0.5f)
                {
                    throw new InvalidOperationException(
                        "Calculated Revolution gun aim does not face forward. LeftError=" +
                        Num(leftError) + ", RightError=" +
                        Num(rightError) + ".");
                }
                return new AimPose(
                    forward,
                    rotations,
                    leftError,
                    rightError);
            }
            finally
            {
                RestoreAll(snapshots);
            }
        }

        private static void AimArm(
            Transform model,
            GunRig rig,
            Vector3 forward)
        {
            var upperArm = rig.Forearm.parent ??
                           throw new InvalidOperationException(
                               rig.Forearm.name +
                               " upper arm is missing.");
            var currentUpperDirection =
                rig.Forearm.position - upperArm.position;
            var outward =
                Vector3.ProjectOnPlane(
                    upperArm.position - model.position,
                    forward);
            outward = Vector3.ProjectOnPlane(
                outward,
                Vector3.up);
            if (outward.sqrMagnitude < 0.000001f)
            {
                outward =
                    upperArm.name.StartsWith(
                        "Left",
                        StringComparison.Ordinal)
                        ? -model.right
                        : model.right;
            }
            outward.Normalize();
            var desiredUpperDirection =
                ((forward * 0.72f) +
                 (outward * 0.28f) +
                 (Vector3.down * 0.08f))
                .normalized;
            upperArm.rotation =
                Quaternion.FromToRotation(
                    currentUpperDirection,
                    desiredUpperDirection) *
                upperArm.rotation;

            var currentGunAxis = rig.SpinBone.forward;
            rig.Forearm.rotation =
                Quaternion.FromToRotation(
                    currentGunAxis,
                    forward) *
                rig.Forearm.rotation;
        }

        private static void MirrorLeftArmPoseToRight(
            Transform model,
            GunRig left,
            GunRig right)
        {
            var leftUpperArm = left.Forearm.parent ??
                               throw new InvalidOperationException(
                                   "LeftForeArm upper arm is missing.");
            var rightUpperArm = right.Forearm.parent ??
                                throw new InvalidOperationException(
                                    "RightForeArm upper arm is missing.");

            AlignBoneDirection(
                rightUpperArm,
                right.Forearm.position - rightUpperArm.position,
                MirrorModelDirection(
                    model,
                    left.Forearm.position - leftUpperArm.position));
            AlignBoneDirection(
                right.Forearm,
                right.Hand.position - right.Forearm.position,
                MirrorModelDirection(
                    model,
                    left.Hand.position - left.Forearm.position));

            var mirroredSpinForward =
                MirrorModelDirection(model, left.SpinBone.forward);
            var mirroredSpinUp =
                MirrorModelDirection(model, left.SpinBone.up);
            var desiredSpinRotation =
                Quaternion.LookRotation(
                    mirroredSpinForward,
                    mirroredSpinUp);
            right.Hand.rotation =
                desiredSpinRotation *
                Quaternion.Inverse(right.SpinBone.localRotation);
        }

        private static void AlignBoneDirection(
            Transform bone,
            Vector3 currentDirection,
            Vector3 desiredDirection)
        {
            if (currentDirection.sqrMagnitude < 0.000001f ||
                desiredDirection.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    bone.name +
                    " cannot be aligned from a zero-length arm direction.");
            }
            bone.rotation =
                Quaternion.FromToRotation(
                    currentDirection,
                    desiredDirection) *
                bone.rotation;
        }

        private static Vector3 MirrorModelDirection(
            Transform model,
            Vector3 worldDirection)
        {
            var localDirection =
                model.InverseTransformDirection(worldDirection);
            return model.TransformDirection(
                    new Vector3(
                        -localDirection.x,
                        localDirection.y,
                        localDirection.z))
                .normalized;
        }

        private static AnimationClip CreateAttackClip(
            Transform model,
            GunRig left,
            GunRig right,
            AimPose aimPose)
        {
            DeleteAssetIfPresent(ClipPath);
            var clip = new AnimationClip
            {
                name = "Revolution_04_MachineGun_Attack",
                frameRate = 60f
            };
            var settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);

            foreach (var boneName in FixedAimBoneNames)
            {
                var bone = RequireDescendant(model, boneName);
                var rotation =
                    aimPose.LocalRotations[boneName];
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        bone,
                        model),
                    new[]
                    {
                        new QuaternionKey(0f, rotation),
                        new QuaternionKey(LoopSeconds, rotation)
                    });
            }

            foreach (var rig in new[] { left, right })
            {
                var rotations = new List<QuaternionKey>();
                for (var shot = 0;
                     shot < ShotCount;
                     shot++)
                {
                    var start = shot * ShotInterval;
                    var flashEnd = start + FlashSeconds;
                    var end = start + ShotInterval;
                    rotations.Add(
                        new QuaternionKey(
                            start,
                            SpinRotation(rig, shot)));
                    rotations.Add(
                        new QuaternionKey(
                            flashEnd,
                            SpinRotation(rig, shot)));
                    rotations.Add(
                        new QuaternionKey(
                            end,
                            SpinRotation(rig, shot + 1)));
                }
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        rig.SpinBone,
                        model),
                    CoalesceQuaternionKeys(rotations));
                foreach (var barrelGroup in
                         rig.CounterRotatingBarrelGroups)
                {
                    var counterRotations =
                        new List<QuaternionKey>();
                    for (var shot = 0;
                         shot < ShotCount;
                         shot++)
                    {
                        var start =
                            shot * ShotInterval;
                        var flashEnd =
                            start + FlashSeconds;
                        var end =
                            start + ShotInterval;
                        counterRotations.Add(
                            new QuaternionKey(
                                start,
                                CounterSpinRotation(
                                    barrelGroup,
                                    shot)));
                        counterRotations.Add(
                            new QuaternionKey(
                                flashEnd,
                                CounterSpinRotation(
                                    barrelGroup,
                                    shot)));
                        counterRotations.Add(
                            new QuaternionKey(
                                end,
                                CounterSpinRotation(
                                    barrelGroup,
                                    shot + 1)));
                    }
                    SetQuaternionCurves(
                        clip,
                        AnimationUtility
                            .CalculateTransformPath(
                                barrelGroup.Transform,
                                model),
                        CoalesceQuaternionKeys(
                            counterRotations));
                }

                var flashKeys = new List<Keyframe>();
                for (var shot = 0;
                     shot < ShotCount;
                     shot++)
                {
                    var start = shot * ShotInterval;
                    var flashEnd = start + FlashSeconds;
                    var next = start + ShotInterval;
                    flashKeys.Add(new Keyframe(start, 1f));
                    flashKeys.Add(
                        new Keyframe(
                            Mathf.Max(
                                start,
                                flashEnd - StepEdgeSeconds),
                            1f));
                    flashKeys.Add(
                        new Keyframe(flashEnd, 0f));
                    flashKeys.Add(
                        new Keyframe(
                            Mathf.Max(
                                flashEnd,
                                next - StepEdgeSeconds),
                            0f));
                }
                flashKeys.Add(
                    new Keyframe(LoopSeconds, 1f));
                var flashPath =
                    AnimationUtility.CalculateTransformPath(
                        rig.Flash,
                        model);
                var coalesced = CoalesceKeyframes(flashKeys);
                SetLinearCurve(
                    clip,
                    flashPath,
                    "m_LocalScale.x",
                    coalesced);
                SetLinearCurve(
                    clip,
                    flashPath,
                    "m_LocalScale.y",
                    coalesced);
                SetLinearCurve(
                    clip,
                    flashPath,
                    "m_LocalScale.z",
                    coalesced);
            }
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static Quaternion SpinRotation(
            GunRig rig,
            int completedShots)
        {
            return rig.BaseLocalRotation *
                   Quaternion.AngleAxis(
                       completedShots * RotationDegreesPerShot,
                       Vector3.forward);
        }

        private static Quaternion CounterSpinRotation(
            CounterRotatingBarrelGroup barrelGroup,
            int completedShots)
        {
            return barrelGroup.BaseLocalRotation *
                   Quaternion.AngleAxis(
                       -completedShots *
                       RotationDegreesPerShot,
                       Vector3.forward);
        }

        private static AnimatorController CreateController(
            AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller =
                AnimatorController
                    .CreateAnimatorControllerAtPath(
                        ControllerPath);
            var state =
                controller.layers[0]
                    .stateMachine.AddState(StateName);
            state.motion = clip;
            controller.layers[0]
                .stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static MotionMetrics InspectMotion(
            Transform placementRoot,
            Transform slot,
            Transform model,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            ReadableSource readableSource)
        {
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                !animator.enabled)
            {
                throw new InvalidOperationException(
                    "Revolution_04 Animator configuration is unexpected.");
            }
            if (Mathf.Abs(clip.length - LoopSeconds) > 0.0001f ||
                !AnimationUtility
                    .GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException(
                    "Corrected Revolution machine-gun attack must be a five-second loop.");
            }
            var renderer =
                RequireCorrectedAppearance(
                    model,
                    readableSource);
            var left = RequireAppliedRig(
                model,
                renderer,
                "LeftForeArm",
                "LeftHand",
                LeftSpinBoneName,
                LeftFlashPivotName,
                LeftFlashName,
                ExpectedBaseBones);
            var right = RequireAppliedRig(
                model,
                renderer,
                "RightForeArm",
                "RightHand",
                RightSpinBoneName,
                RightFlashPivotName,
                RightFlashName,
                ExpectedBaseBones + 1);
            RequireFlashAssets(left);
            RequireFlashAssets(right);
            RequireRiggedMeshGeometry(
                renderer.sharedMesh,
                readableSource.Mesh,
                left,
                right);
            RequireClipBindings(
                clip,
                model,
                left,
                right);
            RequireAnimatorAssignments(placementRoot);

            var snapshots =
                model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            var animatorEnabled = animator.enabled;
            var modelPosition = model.position;
            var modelRotation = model.rotation;
            var targetForward =
                Vector3.ProjectOnPlane(
                    slot.forward,
                    Vector3.up).normalized;
            var minimumFlashOnScale =
                float.PositiveInfinity;
            var maximumFlashOffScale = 0f;
            var maximumRotationError = 0f;
            var maximumRootPositionError = 0f;
            var maximumRootRotationError = 0f;
            var maximumFixedAimBoneDrift = 0f;
            var loopBoundaryError = 0f;
            var leftAimError = 0f;
            var rightAimError = 0f;
            var leftRightward = 0f;
            var rightRightward = 0f;
            var fixedBase =
                new Dictionary<string, Quaternion>(
                    StringComparer.Ordinal);
            try
            {
                animator.enabled = false;
                RestoreAll(snapshots);
                clip.SampleAnimation(
                    model.gameObject,
                    0.02f);
                foreach (var boneName in FixedAimBoneNames)
                {
                    fixedBase[boneName] =
                        RequireDescendant(model, boneName)
                            .localRotation;
                }
                leftAimError =
                    Vector3.Angle(
                        left.SpinBone.forward,
                        targetForward);
                rightAimError =
                    Vector3.Angle(
                        right.SpinBone.forward,
                        targetForward);

                foreach (var shot in
                         Enumerable.Range(0, ShotCount))
                {
                    RestoreAll(snapshots);
                    clip.SampleAnimation(
                        model.gameObject,
                        (shot * ShotInterval) + 0.02f);
                    minimumFlashOnScale = Mathf.Min(
                        minimumFlashOnScale,
                        Mathf.Min(
                            MinimumComponent(
                                left.Flash.localScale),
                            MinimumComponent(
                                right.Flash.localScale)));
                    maximumRotationError = Mathf.Max(
                        maximumRotationError,
                        Quaternion.Angle(
                            left.SpinBone.localRotation,
                            SpinRotation(left, shot)));
                    maximumRotationError = Mathf.Max(
                        maximumRotationError,
                        Quaternion.Angle(
                            right.SpinBone.localRotation,
                            SpinRotation(right, shot)));
                    foreach (var boneName in FixedAimBoneNames)
                    {
                        maximumFixedAimBoneDrift = Mathf.Max(
                            maximumFixedAimBoneDrift,
                            Quaternion.Angle(
                                fixedBase[boneName],
                                RequireDescendant(
                                        model,
                                        boneName)
                                    .localRotation));
                    }
                    maximumRootPositionError = Mathf.Max(
                        maximumRootPositionError,
                        Vector3.Distance(
                            model.position,
                            modelPosition));
                    maximumRootRotationError = Mathf.Max(
                        maximumRootRotationError,
                        Quaternion.Angle(
                            model.rotation,
                            modelRotation));

                    RestoreAll(snapshots);
                    clip.SampleAnimation(
                        model.gameObject,
                        (shot * ShotInterval) + 0.10f);
                    maximumFlashOffScale = Mathf.Max(
                        maximumFlashOffScale,
                        Mathf.Max(
                            MaximumComponent(
                                left.Flash.localScale),
                            MaximumComponent(
                                right.Flash.localScale)));
                }

                leftRightward =
                    MeasureRightwardDisplacement(
                        clip,
                        model,
                        left,
                        snapshots);
                rightRightward =
                    MeasureRightwardDisplacement(
                        clip,
                        model,
                        right,
                        snapshots);

                RestoreAll(snapshots);
                clip.SampleAnimation(model.gameObject, 0f);
                var leftStart =
                    left.SpinBone.localRotation;
                var rightStart =
                    right.SpinBone.localRotation;
                var leftFlashStart =
                    left.Flash.localScale;
                var rightFlashStart =
                    right.Flash.localScale;
                RestoreAll(snapshots);
                clip.SampleAnimation(
                    model.gameObject,
                    LoopSeconds);
                loopBoundaryError = Mathf.Max(
                    Quaternion.Angle(
                        leftStart,
                        left.SpinBone.localRotation),
                    Quaternion.Angle(
                        rightStart,
                        right.SpinBone.localRotation));
                loopBoundaryError = Mathf.Max(
                    loopBoundaryError,
                    Vector3.Distance(
                        leftFlashStart,
                        left.Flash.localScale));
                loopBoundaryError = Mathf.Max(
                    loopBoundaryError,
                    Vector3.Distance(
                        rightFlashStart,
                        right.Flash.localScale));
            }
            finally
            {
                RestoreAll(snapshots);
                animator.enabled = animatorEnabled;
            }

            if (leftAimError > 0.5f ||
                rightAimError > 0.5f ||
                minimumFlashOnScale < 0.95f ||
                maximumFlashOffScale > 0.05f ||
                maximumRotationError > 0.05f ||
                maximumFixedAimBoneDrift > 0.01f ||
                maximumRootPositionError > 0.00001f ||
                maximumRootRotationError > 0.01f ||
                loopBoundaryError > 0.01f ||
                leftRightward <= 0.0001f ||
                rightRightward <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Corrected Revolution machine-gun inspection failed. LeftAimError=" +
                    Num(leftAimError) +
                    ", RightAimError=" +
                    Num(rightAimError) +
                    ", MinimumFlashOnScale=" +
                    Num(minimumFlashOnScale) +
                    ", MaximumFlashOffScale=" +
                    Num(maximumFlashOffScale) +
                    ", MaximumRotationError=" +
                    Num(maximumRotationError) +
                    ", MaximumFixedAimBoneDrift=" +
                    Num(maximumFixedAimBoneDrift) +
                    ", LoopBoundaryError=" +
                    Num(loopBoundaryError) +
                    ", LeftRightwardDisplacement=" +
                    Num(leftRightward) +
                    ", RightRightwardDisplacement=" +
                    Num(rightRightward) + ".");
            }

            return new MotionMetrics
            {
                LeftForwardAimErrorDegrees = leftAimError,
                RightForwardAimErrorDegrees = rightAimError,
                MinimumFlashOnScale = minimumFlashOnScale,
                MaximumFlashOffScale = maximumFlashOffScale,
                MaximumRotationError = maximumRotationError,
                MaximumFixedAimBoneDriftDegrees =
                    maximumFixedAimBoneDrift,
                MaximumRootPositionError =
                    maximumRootPositionError,
                MaximumRootRotationError =
                    maximumRootRotationError,
                LoopBoundaryError = loopBoundaryError,
                LeftRightwardDisplacement = leftRightward,
                RightRightwardDisplacement = rightRightward,
                LeftBarrelVertices = left.BarrelVertexCount,
                RightBarrelVertices = right.BarrelVertexCount,
                LeftBarrelComponents = left.BarrelComponentCount,
                RightBarrelComponents = right.BarrelComponentCount,
                LeftMuzzlePosition = left.FlashPivot.position,
                RightMuzzlePosition = right.FlashPivot.position
            };
        }

        private static float MeasureRightwardDisplacement(
            AnimationClip clip,
            Transform model,
            GunRig rig,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            RestoreAll(snapshots);
            clip.SampleAnimation(model.gameObject, 0f);
            var start =
                rig.SpinBone.TransformPoint(
                    Vector3.up * 0.12f);
            var screenRight = -rig.SpinBone.right;
            RestoreAll(snapshots);
            clip.SampleAnimation(model.gameObject, 0.14f);
            var end =
                rig.SpinBone.TransformPoint(
                    Vector3.up * 0.12f);
            return Vector3.Dot(
                end - start,
                screenRight);
        }

        private static void RequireRiggedMeshGeometry(
            Mesh rigged,
            Mesh source,
            GunRig left,
            GunRig right)
        {
            if (!rigged.isReadable ||
                rigged.vertexCount != source.vertexCount ||
                rigged.subMeshCount != source.subMeshCount ||
                rigged.bindposes.Length != ExpectedRiggedBones ||
                rigged.uv.Length != source.uv.Length ||
                !rigged.vertices.SequenceEqual(source.vertices) ||
                !rigged.uv.SequenceEqual(source.uv))
            {
                throw new InvalidOperationException(
                    "Corrected Revolution rig mesh changed vertices, UVs, submeshes, or bind-pose count.");
            }
            for (var subMesh = 0;
                 subMesh < source.subMeshCount;
                 subMesh++)
            {
                if (!rigged.GetTriangles(subMesh)
                        .SequenceEqual(
                            source.GetTriangles(subMesh)))
                {
                    throw new InvalidOperationException(
                        "Corrected Revolution rig mesh changed triangle topology at submesh " +
                        subMesh + ".");
                }
            }
            var weights = rigged.boneWeights;
            var leftCount = weights.Count(item =>
                item.boneIndex0 == ExpectedBaseBones &&
                Mathf.Abs(item.weight0 - 1f) < 0.00001f);
            var rightCount = weights.Count(item =>
                item.boneIndex0 == ExpectedBaseBones + 1 &&
                Mathf.Abs(item.weight0 - 1f) < 0.00001f);
            if (leftCount != left.BarrelVertexCount ||
                rightCount != right.BarrelVertexCount ||
                leftCount < 12 ||
                rightCount < 12)
            {
                throw new InvalidOperationException(
                    "Corrected Revolution barrel-only bone weights differ. Left=" +
                    leftCount + ", Right=" + rightCount + ".");
            }
        }

        private static void RequireClipBindings(
            AnimationClip clip,
            Transform model,
            GunRig left,
            GunRig right)
        {
            var fixedPaths = new HashSet<string>(
                FixedAimBoneNames.Select(name =>
                    AnimationUtility.CalculateTransformPath(
                        RequireDescendant(model, name),
                        model)),
                StringComparer.Ordinal);
            var leftSpin =
                AnimationUtility.CalculateTransformPath(
                    left.SpinBone,
                    model);
            var rightSpin =
                AnimationUtility.CalculateTransformPath(
                    right.SpinBone,
                    model);
            var leftFlash =
                AnimationUtility.CalculateTransformPath(
                    left.Flash,
                    model);
            var rightFlash =
                AnimationUtility.CalculateTransformPath(
                    right.Flash,
                    model);
            var fixedRotationBindings = 0;
            var spinRotationBindings = 0;
            var flashBindings = 0;
            var unexpected = 0;
            foreach (var binding in
                     AnimationUtility.GetCurveBindings(clip))
            {
                if (fixedPaths.Contains(binding.path) &&
                    binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                {
                    fixedRotationBindings++;
                }
                else if ((binding.path == leftSpin ||
                          binding.path == rightSpin) &&
                         binding.propertyName.StartsWith(
                             "m_LocalRotation.",
                             StringComparison.Ordinal))
                {
                    spinRotationBindings++;
                }
                else if ((binding.path == leftFlash ||
                          binding.path == rightFlash) &&
                         binding.propertyName.StartsWith(
                             "m_LocalScale.",
                             StringComparison.Ordinal))
                {
                    flashBindings++;
                }
                else
                {
                    unexpected++;
                }
            }
            if (fixedRotationBindings !=
                    FixedAimBoneNames.Length * 4 ||
                spinRotationBindings != 8 ||
                flashBindings != 6 ||
                unexpected != 0)
            {
                throw new InvalidOperationException(
                    "Corrected Revolution machine-gun clip bindings are unexpected. FixedAim=" +
                    fixedRotationBindings +
                    ", Spin=" + spinRotationBindings +
                    ", Flash=" + flashBindings +
                    ", Unexpected=" + unexpected + ".");
            }
        }

        private static void RequireFlashAssets(GunRig rig)
        {
            var filter =
                rig.Flash.GetComponent<MeshFilter>() ??
                throw new InvalidOperationException(
                    rig.Flash.name +
                    " MeshFilter is missing.");
            var renderer =
                rig.Flash.GetComponent<MeshRenderer>() ??
                throw new InvalidOperationException(
                    rig.Flash.name +
                    " MeshRenderer is missing.");
            if (AssetDatabase.GetAssetPath(filter.sharedMesh) !=
                    RebellionFlashMeshPath ||
                AssetDatabase.GetAssetPath(
                    renderer.sharedMaterial) !=
                    RebellionFlashMaterialPath)
            {
                throw new InvalidOperationException(
                    rig.Flash.name +
                    " must directly reuse the Rebellion muzzle-flash assets.");
            }
        }

        private static GunRig RequireAppliedRig(
            Transform model,
            SkinnedMeshRenderer renderer,
            string forearmName,
            string handName,
            string spinBoneName,
            string flashPivotName,
            string flashName,
            int spinBoneIndex)
        {
            var forearm = RequireDescendant(model, forearmName);
            var hand = RequireDescendant(model, handName);
            var spin = RequireDescendant(model, spinBoneName);
            var flashPivot =
                RequireDescendant(model, flashPivotName);
            var flash = RequireDescendant(model, flashName);
            if (hand.parent != forearm ||
                spin.parent != hand ||
                flashPivot.parent != hand ||
                flash.parent != flashPivot ||
                renderer.bones.Length != ExpectedRiggedBones ||
                renderer.bones[spinBoneIndex] != spin)
            {
                throw new InvalidOperationException(
                    spinBoneName +
                    " hierarchy or renderer bone assignment differs from the corrected barrel-only rig.");
            }
            var weights = renderer.sharedMesh.boneWeights;
            var vertexCount = weights.Count(item =>
                item.boneIndex0 == spinBoneIndex &&
                Mathf.Abs(item.weight0 - 1f) < 0.00001f);
            return new GunRig(
                forearm,
                hand,
                spin,
                flashPivot,
                flash,
                spin.forward,
                flashPivot.position,
                spin.localRotation,
                vertexCount,
                CountWeightedComponents(
                    renderer.sharedMesh,
                    spinBoneIndex));
        }

        private static int CountWeightedComponents(
            Mesh mesh,
            int boneIndex)
        {
            var weights = mesh.boneWeights;
            var selected = new HashSet<int>(
                Enumerable.Range(0, weights.Length)
                    .Where(index =>
                        weights[index].boneIndex0 ==
                            boneIndex &&
                        Mathf.Abs(
                            weights[index].weight0 - 1f) <
                        0.00001f));
            return ConnectedComponents(mesh)
                .Count(component =>
                    component.Any(selected.Contains));
        }

        private static void RequireAnimatorAssignments(
            Transform root)
        {
            foreach (var slotName in SlotNames)
            {
                var slot =
                    RequireDirectChild(root, slotName);
                var model = RequireModel(slot);
                var animators =
                    model.GetComponentsInChildren<Animator>(true);
                if (animators.Length > 1)
                {
                    throw new InvalidOperationException(
                        slotName +
                        " contains multiple Animators.");
                }
                var controllerPath =
                    animators.Length == 0
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(
                            animators[0]
                                .runtimeAnimatorController);
                if (slotName == "Revolution_02")
                {
                    if (controllerPath !=
                        ArtRoot +
                        "/Controllers/Revolution_02_Idle_Breathing.controller")
                    {
                        throw new InvalidOperationException(
                            "Revolution_02 idle controller changed.");
                    }
                }
                else if (slotName == AttackSlotName)
                {
                    if (controllerPath != ControllerPath)
                    {
                        throw new InvalidOperationException(
                            "Revolution_04 corrected machine-gun controller is not assigned.");
                    }
                }
                else if (!string.IsNullOrEmpty(controllerPath))
                {
                    throw new InvalidOperationException(
                        slotName +
                        " unexpectedly has an Animator Controller: " +
                        controllerPath + ".");
                }
            }
        }

        private static void CaptureReviewGrid(
            Scene scene,
            Transform model,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid corrected Revolution review folder."));
            var snapshots =
                model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            var animatorEnabled = animator.enabled;
            var otherRenderers =
                scene.GetRootGameObjects()
                    .SelectMany(item =>
                        item.GetComponentsInChildren<Renderer>(true))
                    .Where(item =>
                        !item.transform.IsChildOf(model))
                    .Select(item =>
                        new RendererEnabledSnapshot(item))
                    .ToArray();
            var player = GameObject.Find("Player") ??
                         throw new InvalidOperationException(
                             "Player is missing.");
            var sourceCamera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var cameraObject = new GameObject(
                "RevolutionCorrectedMachineGunReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelWidth = 640;
            const int panelHeight = 420;
            var grid = new Texture2D(
                panelWidth * 6,
                panelHeight * 3,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            var times = new[]
            {
                0f, 0.04f, 0.08f, 0.12f, 0.16f, 0.20f
            };
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }
                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags =
                    CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                for (var row = 0; row < 3; row++)
                {
                    for (var column = 0;
                         column < times.Length;
                         column++)
                    {
                        RestoreAll(snapshots);
                        clip.SampleAnimation(
                            model.gameObject,
                            times[column]);
                        if (row == 0)
                        {
                            FrameFullModel(
                                camera,
                                model,
                                sourceCamera,
                                panelWidth /
                                (float)panelHeight);
                        }
                        else if (row == 1)
                        {
                            FrameGunCloseup(
                                camera,
                                model,
                                sourceCamera,
                                panelWidth /
                                (float)panelHeight,
                                0f);
                        }
                        else
                        {
                            FrameRightGunFrontCloseup(
                                camera,
                                model,
                                sourceCamera,
                                panelWidth /
                                (float)panelHeight);
                        }
                        camera.Render();
                        RenderTexture.active = target;
                        panel.ReadPixels(
                            new Rect(
                                0f,
                                0f,
                                panelWidth,
                                panelHeight),
                            0,
                            0);
                        panel.Apply();
                        var pixels = panel.GetPixels32();
                        if (pixels.Any(pixel =>
                                pixel.r >= 240 &&
                                pixel.b >= 240 &&
                                pixel.g <= 24))
                        {
                            throw new InvalidOperationException(
                                "Corrected Revolution machine-gun review contains Unity's magenta shader fallback.");
                        }
                        grid.SetPixels32(
                            column * panelWidth,
                            (2 - row) * panelHeight,
                            panelWidth,
                            panelHeight,
                            pixels);
                    }
                }
                grid.Apply();
                File.WriteAllBytes(
                    destination,
                    grid.EncodeToPNG());
                CaptureVisualSequenceFrames(
                    model,
                    clip,
                    snapshots,
                    camera,
                    sourceCamera,
                    target,
                    panel);
                CaptureSelectedBarrelOverlay(
                    model,
                    clip,
                    snapshots,
                    camera,
                    sourceCamera,
                    target,
                    panel);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>()
                    .targetTexture = null;
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Restore();
                }
                RestoreAll(snapshots);
                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(grid);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureSelectedBarrelOverlay(
            Transform model,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots,
            Camera camera,
            Camera sourceCamera,
            RenderTexture target,
            Texture2D panel)
        {
            RestoreAll(snapshots);
            clip.SampleAnimation(model.gameObject, 0f);
            var skinned =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Single();
            var source = skinned.sharedMesh;
            var overlay = new Mesh
            {
                name = "RevolutionSelectedBarrelOverlay"
            };
            var overlayObject = new GameObject(
                "RevolutionSelectedBarrelOverlay",
                typeof(SkinnedMeshRenderer))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var shader =
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                throw new InvalidOperationException(
                    "A temporary unlit overlay shader is unavailable.");
            var overlayMaterials = new[]
            {
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorRed",
                    new Color(1f, 0.05f, 0.05f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorYellow",
                    new Color(1f, 0.9f, 0.05f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorGreen",
                    new Color(0.05f, 1f, 0.15f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorCyan",
                    new Color(0.05f, 0.9f, 1f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorMagenta",
                    new Color(1f, 0.05f, 0.85f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorOrange",
                    new Color(1f, 0.4f, 0.05f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorBlue",
                    new Color(0.15f, 0.35f, 1f, 1f)),
                CreateDebugOverlayMaterial(
                    shader,
                    "RevolutionBarrelSectorWhite",
                    new Color(1f, 1f, 1f, 1f))
            };
            GameObject leftSpinMarker = null;
            GameObject rightSpinMarker = null;
            var rightGroupRenderers = model
                .GetComponentsInChildren<MeshRenderer>(true)
                .Where(item =>
                    item.name.StartsWith(
                        RightBarrelGroupNamePrefix,
                        StringComparison.Ordinal))
                .OrderBy(item => item.name)
                .ToArray();
            var rightGroupMaterials =
                rightGroupRenderers
                    .Select(item =>
                        item.sharedMaterials)
                    .ToArray();
            try
            {
                for (var groupIndex = 0;
                     groupIndex <
                     rightGroupRenderers.Length;
                     groupIndex++)
                {
                    var groupRenderer =
                        rightGroupRenderers[groupIndex];
                    groupRenderer.sharedMaterials =
                        Enumerable.Repeat(
                                overlayMaterials[
                                    groupIndex %
                                    overlayMaterials.Length],
                                groupRenderer
                                    .sharedMaterials
                                    .Length)
                            .ToArray();
                }
                leftSpinMarker = CreateSpinReviewMarker(
                    RequireDescendant(model, LeftSpinBoneName),
                    overlayMaterials[2],
                    "RevolutionLeftSpinReviewMarker");
                rightSpinMarker = CreateSpinReviewMarker(
                    RequireDescendant(model, RightSpinBoneName),
                    overlayMaterials[6],
                    "RevolutionRightSpinReviewMarker");
                overlay.vertices = source.vertices;
                overlay.normals = source.normals;
                overlay.uv = source.uv;
                overlay.boneWeights = source.boneWeights;
                overlay.bindposes = source.bindposes;
                overlay.subMeshCount = overlayMaterials.Length;
                var weights = source.boneWeights;
                var sectorTriangles = Enumerable
                    .Range(0, overlayMaterials.Length)
                    .Select(_ => new List<int>())
                    .ToArray();
                for (var subMesh = 0;
                     subMesh < source.subMeshCount;
                     subMesh++)
                {
                    var triangles = source.GetTriangles(subMesh);
                    for (var index = 0;
                         index < triangles.Length;
                         index += 3)
                    {
                        AddSelectedTriangleBySector(
                            source,
                            triangles,
                            index,
                            weights,
                            ExpectedBaseBones,
                            0,
                            sectorTriangles);
                        AddSelectedTriangleBySector(
                            source,
                            triangles,
                            index,
                            weights,
                            ExpectedBaseBones + 1,
                            4,
                            sectorTriangles);
                    }
                }
                for (var sector = 0;
                     sector < sectorTriangles.Length;
                     sector++)
                {
                    overlay.SetTriangles(
                        sectorTriangles[sector],
                        sector);
                }
                overlay.bounds = source.bounds;
                var overlayTransform = overlayObject.transform;
                overlayTransform.SetParent(
                    skinned.transform.parent,
                    false);
                overlayTransform.localPosition =
                    skinned.transform.localPosition;
                overlayTransform.localRotation =
                    skinned.transform.localRotation;
                overlayTransform.localScale =
                    skinned.transform.localScale;
                var overlayRenderer =
                    overlayObject.GetComponent<SkinnedMeshRenderer>();
                overlayRenderer.sharedMesh = overlay;
                overlayRenderer.bones = skinned.bones;
                overlayRenderer.rootBone = skinned.rootBone;
                overlayRenderer.localBounds = skinned.localBounds;
                overlayRenderer.updateWhenOffscreen = true;
                overlayRenderer.sharedMaterials = overlayMaterials;
                overlayRenderer.shadowCastingMode =
                    ShadowCastingMode.Off;
                overlayRenderer.receiveShadows = false;

                var debugTimes = new[]
                {
                    0.08f, 0.104f, 0.128f,
                    0.152f, 0.176f, 0.20f
                };
                var debugGrid = new Texture2D(
                    target.width * debugTimes.Length,
                    target.height,
                    TextureFormat.RGB24,
                    false);
                try
                {
                    for (var frame = 0;
                         frame < debugTimes.Length;
                         frame++)
                    {
                        RestoreAll(snapshots);
                        clip.SampleAnimation(
                            model.gameObject,
                            debugTimes[frame]);
                        FrameBounds(
                            camera,
                            BoundsOf(model),
                            sourceCamera,
                            target.width /
                            (float)target.height,
                            1.08f,
                            42f);
                        camera.Render();
                        RenderTexture.active = target;
                        panel.ReadPixels(
                            new Rect(
                                0f,
                                0f,
                                target.width,
                                target.height),
                            0,
                            0);
                        panel.Apply();
                        debugGrid.SetPixels32(
                            frame * target.width,
                            0,
                            target.width,
                            target.height,
                            panel.GetPixels32());
                    }
                    debugGrid.Apply();
                    File.WriteAllBytes(
                        Path.Combine(
                            Path.GetTempPath(),
                            VisualSequenceFolderName,
                            "RevolutionSelectedBarrelOverlay.png"),
                        debugGrid.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(debugGrid);
                }
            }
            finally
            {
                for (var groupIndex = 0;
                     groupIndex <
                     rightGroupRenderers.Length;
                     groupIndex++)
                {
                    rightGroupRenderers[groupIndex]
                        .sharedMaterials =
                        rightGroupMaterials[groupIndex];
                }
                if (leftSpinMarker != null)
                {
                    UnityEngine.Object.DestroyImmediate(leftSpinMarker);
                }
                if (rightSpinMarker != null)
                {
                    UnityEngine.Object.DestroyImmediate(rightSpinMarker);
                }
                UnityEngine.Object.DestroyImmediate(overlayObject);
                UnityEngine.Object.DestroyImmediate(overlay);
                foreach (var material in overlayMaterials)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
            }
        }

        private static GameObject CreateSpinReviewMarker(
            Transform spinBone,
            Material material,
            string markerName)
        {
            var root = new GameObject(markerName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            root.transform.SetParent(spinBone, false);
            var origin = GameObject.CreatePrimitive(
                PrimitiveType.Sphere);
            origin.name = markerName + "_Origin";
            origin.hideFlags = HideFlags.HideAndDontSave;
            var originCollider = origin.GetComponent<Collider>();
            if (originCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    originCollider);
            }
            origin.transform.SetParent(root.transform, false);
            origin.transform.localPosition = Vector3.zero;
            origin.transform.localScale =
                Vector3.one * 0.035f;
            origin.GetComponent<MeshRenderer>().sharedMaterial =
                material;

            var marker = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            marker.name = markerName + "_Spoke";
            marker.hideFlags = HideFlags.HideAndDontSave;
            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition =
                new Vector3(0f, 0.12f, 0.025f);
            marker.transform.localRotation =
                Quaternion.Euler(0f, 0f, 35f);
            marker.transform.localScale =
                new Vector3(0.018f, 0.055f, 0.018f);
            marker.GetComponent<MeshRenderer>().sharedMaterial =
                material;
            return root;
        }

        private static Material CreateDebugOverlayMaterial(
            Shader shader,
            string materialName,
            Color color)
        {
            return new Material(shader)
            {
                name = materialName,
                color = color,
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 5000
            };
        }

        private static void AddSelectedTriangleBySector(
            Mesh source,
            IReadOnlyList<int> triangles,
            int start,
            IReadOnlyList<BoneWeight> weights,
            int boneIndex,
            int sectorOffset,
            IReadOnlyList<List<int>> destinations)
        {
            var first = triangles[start];
            var second = triangles[start + 1];
            var third = triangles[start + 2];
            if (WeightForBone(weights[first], boneIndex) < 0.99f ||
                WeightForBone(weights[second], boneIndex) < 0.99f ||
                WeightForBone(weights[third], boneIndex) < 0.99f)
            {
                return;
            }
            var bindPose = source.bindposes[boneIndex];
            var localCenter =
                (bindPose.MultiplyPoint3x4(source.vertices[first]) +
                 bindPose.MultiplyPoint3x4(source.vertices[second]) +
                 bindPose.MultiplyPoint3x4(source.vertices[third])) /
                3f;
            var angle = Mathf.Atan2(localCenter.y, localCenter.x);
            var sector = Mathf.FloorToInt(
                Mathf.Repeat(
                    (angle + Mathf.PI) / (Mathf.PI * 0.5f),
                    4f));
            var destination = destinations[sectorOffset + sector];
            destination.Add(first);
            destination.Add(second);
            destination.Add(third);
        }

        private static void CaptureVisualSequenceFrames(
            Transform model,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots,
            Camera camera,
            Camera sourceCamera,
            RenderTexture target,
            Texture2D panel)
        {
            var sequenceFolder = Path.Combine(
                Path.GetTempPath(),
                VisualSequenceFolderName);
            Directory.CreateDirectory(sequenceFolder);
            foreach (var oldFrame in
                     Directory.GetFiles(
                         sequenceFolder,
                         "RevolutionMachineGunVisual_*.png"))
            {
                File.Delete(oldFrame);
            }

            const int framesPerSecond = 30;
            const int frameCount = 61;
            camera.targetTexture = target;
            FrameBounds(
                camera,
                PrimaryModelBounds(model),
                sourceCamera,
                target.width / (float)target.height,
                1.12f,
                32f);
            for (var frameIndex = 0;
                 frameIndex < frameCount;
                 frameIndex++)
            {
                RestoreAll(snapshots);
                clip.SampleAnimation(
                    model.gameObject,
                    frameIndex / (float)framesPerSecond);
                camera.Render();
                RenderTexture.active = target;
                panel.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        target.width,
                        target.height),
                    0,
                    0);
                panel.Apply();
                File.WriteAllBytes(
                    Path.Combine(
                        sequenceFolder,
                        "RevolutionMachineGunVisual_" +
                        frameIndex.ToString("D4") +
                        ".png"),
                    panel.EncodeToPNG());
            }
        }

        private static void FrameFullModel(
            Camera camera,
            Transform model,
            Camera sourceCamera,
            float aspect)
        {
            FrameBounds(
                camera,
                PrimaryModelBounds(model),
                sourceCamera,
                aspect,
                1.15f);
        }

        private static Bounds PrimaryModelBounds(
            Transform model)
        {
            return model
                       .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault()
                       ?.bounds ??
                   throw new InvalidOperationException(
                       "Revolution_04 primary skinned renderer is missing.");
        }

        private static void FrameGunCloseup(
            Camera camera,
            Transform model,
            Camera sourceCamera,
            float aspect,
            float yawOffset)
        {
            var left = RequireDescendant(
                model,
                LeftSpinBoneName);
            var right = RequireDescendant(
                model,
                RightSpinBoneName);
            var leftFlash = RequireDescendant(
                model,
                LeftFlashPivotName);
            var rightFlash = RequireDescendant(
                model,
                RightFlashPivotName);
            var bounds = new Bounds(
                (left.position + right.position +
                 leftFlash.position + rightFlash.position) /
                4f,
                Vector3.one * 0.1f);
            bounds.Encapsulate(left.position);
            bounds.Encapsulate(right.position);
            bounds.Encapsulate(leftFlash.position);
            bounds.Encapsulate(rightFlash.position);
            bounds.Expand(0.28f);
            FrameBounds(
                camera,
                bounds,
                sourceCamera,
                aspect,
                1.08f,
                yawOffset);
        }

        private static void FrameRightGunFrontCloseup(
            Camera camera,
            Transform model,
            Camera sourceCamera,
            float aspect)
        {
            var spin = RequireDescendant(
                model,
                RightSpinBoneName);
            var groupRenderers = spin
                .GetComponentsInChildren<MeshRenderer>(true)
                .Where(item =>
                    item.name.StartsWith(
                        RightBarrelGroupNamePrefix,
                        StringComparison.Ordinal))
                .ToArray();
            if (groupRenderers.Length != RightBarrelGroupCount)
            {
                throw new InvalidOperationException(
                    "The right muzzle-on review requires exactly " +
                    RightBarrelGroupCount +
                    " counter-rotating barrel groups, but found " +
                    groupRenderers.Length +
                    ".");
            }
            var groupWorldVertices = new List<Vector3>();
            foreach (var groupRenderer in groupRenderers)
            {
                var filter =
                    groupRenderer.GetComponent<MeshFilter>();
                if (filter == null ||
                    filter.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        "A right counter-rotating barrel group has " +
                        "no readable mesh.");
                }
                var mesh = filter.sharedMesh;
                var renderedVertexIndices = new HashSet<int>();
                for (var subMesh = 0;
                     subMesh < mesh.subMeshCount;
                     subMesh++)
                {
                    foreach (var vertexIndex in
                             mesh.GetTriangles(subMesh))
                    {
                        renderedVertexIndices.Add(vertexIndex);
                    }
                }
                var vertices = mesh.vertices;
                var renderedWorldVertices =
                    renderedVertexIndices.Select(vertexIndex =>
                        filter.transform.TransformPoint(
                            vertices[vertexIndex]))
                        .ToArray();
                groupWorldVertices.AddRange(renderedWorldVertices);
            }
            if (groupWorldVertices.Count == 0)
            {
                throw new InvalidOperationException(
                    "The right counter-rotating barrel groups have " +
                    "no vertices for muzzle-on framing.");
            }
            var groupCenter = Vector3.zero;
            foreach (var vertex in groupWorldVertices)
            {
                groupCenter += vertex;
            }
            groupCenter /= groupWorldVertices.Count;
            camera.aspect = aspect;
            camera.fieldOfView = 24f;
            camera.nearClipPlane = 0.03f;
            camera.transform.rotation =
                sourceCamera.transform.rotation;
            camera.transform.position =
                groupCenter +
                (-camera.transform.forward * 1.2f);
        }

        private static void FrameBounds(
            Camera camera,
            Bounds bounds,
            Camera sourceCamera,
            float aspect,
            float padding,
            float yawOffset = 0f)
        {
            var viewDirection =
                sourceCamera.transform.position - bounds.center;
            viewDirection.y = 0f;
            if (viewDirection.sqrMagnitude < 0.0001f)
            {
                viewDirection = Vector3.back;
            }
            viewDirection.Normalize();
            viewDirection =
                Quaternion.AngleAxis(
                    yawOffset,
                    Vector3.up) *
                viewDirection;
            camera.aspect = aspect;
            var verticalDistance =
                bounds.extents.y /
                Mathf.Tan(
                    camera.fieldOfView *
                    Mathf.Deg2Rad *
                    0.5f);
            var horizontalFov =
                2f * Mathf.Atan(
                    Mathf.Tan(
                        camera.fieldOfView *
                        Mathf.Deg2Rad *
                        0.5f) *
                    aspect);
            var horizontalDistance =
                Mathf.Max(
                    bounds.extents.x,
                    bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance =
                Mathf.Max(
                    verticalDistance,
                    horizontalDistance) *
                padding;
            camera.transform.position =
                bounds.center +
                viewDirection * distance;
            camera.transform.rotation =
                Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers =
                model.GetComponentsInChildren<Renderer>(false)
                    .Where(item =>
                        item.enabled &&
                        item.gameObject.activeInHierarchy)
                    .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Revolution_04 has no visible renderer.");
            }
            var bounds = renderers[0].bounds;
            for (var index = 1;
                 index < renderers.Length;
                 index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static void WriteInspectionReport(
            MotionMetrics metrics)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid corrected Revolution inspection folder."));
            var report = new StringBuilder();
            report.AppendLine(
                "Revolution 04 Corrected Machine Gun Attack Inspection");
            report.AppendLine("Result=PASS");
            report.AppendLine(
                "Correction=ForwardAimAndBarrelOnlyRingRotation");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + AttackSlotName);
            report.AppendLine("Clip=" + ClipPath);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("RiggedMesh=" + RiggedMeshPath);
            report.AppendLine(
                "ReadableExactSource=" +
                ReadableSourceModelPath);
            report.AppendLine("State=" + StateName);
            report.AppendLine(
                "LoopSeconds=" + Num(LoopSeconds));
            report.AppendLine("LoopEnabled=True");
            report.AppendLine("FrameRate=60");
            report.AppendLine(
                "ShotIntervalSeconds=" +
                Num(ShotInterval));
            report.AppendLine("ShotCount=" + ShotCount);
            report.AppendLine(
                "FlashDurationSeconds=" +
                Num(FlashSeconds));
            report.AppendLine(
                "RotationDegreesPerShot=" +
                Num(RotationDegreesPerShot));
            report.AppendLine("RotationTurnsPerLoop=5");
            report.AppendLine(
                "RotationDirectionFrontView=RightClockwise");
            report.AppendLine(
                "LeftForwardAimErrorDegrees=" +
                Num(metrics.LeftForwardAimErrorDegrees));
            report.AppendLine(
                "RightForwardAimErrorDegrees=" +
                Num(metrics.RightForwardAimErrorDegrees));
            report.AppendLine(
                "LeftRightwardDisplacement=" +
                Num(metrics.LeftRightwardDisplacement));
            report.AppendLine(
                "RightRightwardDisplacement=" +
                Num(metrics.RightRightwardDisplacement));
            report.AppendLine(
                "MaximumFixedArmAndHousingDriftDegrees=" +
                Num(metrics.MaximumFixedAimBoneDriftDegrees));
            report.AppendLine(
                "LeftBarrelComponents=" +
                metrics.LeftBarrelComponents);
            report.AppendLine(
                "RightBarrelComponents=" +
                metrics.RightBarrelComponents);
            report.AppendLine(
                "LeftBarrelVertices=" +
                metrics.LeftBarrelVertices);
            report.AppendLine(
                "RightBarrelVertices=" +
                metrics.RightBarrelVertices);
            report.AppendLine(
                "LeftMuzzlePosition=" +
                Vec(metrics.LeftMuzzlePosition));
            report.AppendLine(
                "RightMuzzlePosition=" +
                Vec(metrics.RightMuzzlePosition));
            report.AppendLine(
                "MinimumFlashOnScale=" +
                Num(metrics.MinimumFlashOnScale));
            report.AppendLine(
                "MaximumFlashOffScale=" +
                Num(metrics.MaximumFlashOffScale));
            report.AppendLine(
                "MaximumBarrelRotationError=" +
                Num(metrics.MaximumRotationError));
            report.AppendLine(
                "MaximumRootPositionError=" +
                Num(metrics.MaximumRootPositionError));
            report.AppendLine(
                "MaximumRootRotationError=" +
                Num(metrics.MaximumRootRotationError));
            report.AppendLine(
                "LoopBoundaryError=" +
                Num(metrics.LoopBoundaryError));
            report.AppendLine(
                "AnimatedSpinBones=" +
                LeftSpinBoneName + "," + RightSpinBoneName);
            report.AppendLine(
                "FixedAimBones=" +
                string.Join(",", FixedAimBoneNames));
            report.AppendLine(
                "ReusedFlashMesh=" +
                RebellionFlashMeshPath);
            report.AppendLine(
                "ReusedFlashMaterial=" +
                RebellionFlashMaterialPath);
            report.AppendLine(
                "VerticesTrianglesUvsPreserved=True");
            report.AppendLine(
                "WholeArmUsedAsSpinAxis=False");
            report.AppendLine(
                "GunHousingAndDiscSpin=False");
            report.AppendLine(
                "NewWeaponGeometryCreated=False");
            report.AppendLine(
                "MachineGunConversionCreated=False");
            report.AppendLine("RootMotion=False");
            report.AppendLine(
                "ApprovedAppearancePreserved=True");
            report.AppendLine(
                "OtherRevolutionSlotsChanged=False");
            report.AppendLine(
                "PlayerCameraAndOtherRootsChanged=False");
            report.AppendLine(
                "SceneChangedByInspection=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static SkinnedMeshRenderer
            RequireBaseOrPreviousRenderer(Transform model)
        {
            var renderer = RequireSingleRenderer(model);
            var path =
                AssetDatabase.GetAssetPath(
                    renderer.sharedMesh);
            if (path != ApprovedModelPath &&
                path != RiggedMeshPath)
            {
                throw new InvalidOperationException(
                    "Revolution_04 uses an unexpected mesh before correction: " +
                    path + ".");
            }
            RequireApprovedMaterials(renderer);
            return renderer;
        }

        private static SkinnedMeshRenderer
            RequireBaseApprovedAppearance(Transform model)
        {
            var renderer = RequireSingleRenderer(model);
            if (AssetDatabase.GetAssetPath(
                    renderer.sharedMesh) != ApprovedModelPath ||
                renderer.bones.Length != ExpectedBaseBones)
            {
                throw new InvalidOperationException(
                    "Revolution_04 did not restore the original approved mesh and 24-bone rig before correction.");
            }
            RequireApprovedMaterials(renderer);
            RequireGeometryCounts(
                renderer.sharedMesh,
                ExpectedBaseBones);
            return renderer;
        }

        private static SkinnedMeshRenderer
            RequireCorrectedAppearance(
                Transform model,
                ReadableSource readableSource)
        {
            var renderer = RequireSingleRenderer(model);
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Corrected Revolution mesh is missing.");
            if (AssetDatabase.GetAssetPath(mesh) !=
                    RiggedMeshPath ||
                renderer.bones.Length != ExpectedRiggedBones)
            {
                throw new InvalidOperationException(
                    "Revolution_04 does not use the corrected barrel-only rig mesh.");
            }
            RequireApprovedMaterials(renderer);
            RequireExtractedBarrelGeometry(model, mesh);
            RequireCompatibleBoneOrder(
                renderer,
                readableSource.Renderer);
            return renderer;
        }

        private static void RequireExtractedBarrelGeometry(
            Transform model,
            Mesh baseMesh)
        {
            var leftMesh = RequireExtractedBarrelMesh(
                model,
                LeftSpinBoneName,
                LeftBarrelMeshPath);
            var meshes = new List<Mesh>
            {
                baseMesh,
                leftMesh
            };
            meshes.AddRange(
                RequireExtractedRightBarrelGroupMeshes(
                    model));
            var triangles = meshes.Sum(item =>
                Enumerable.Range(0, item.subMeshCount)
                    .Sum(index =>
                        checked(
                            (int)item.GetIndexCount(index) /
                            3)));
            if (triangles != ExpectedTriangles ||
                meshes.Any(item =>
                    item.subMeshCount !=
                    ExpectedMaterials) ||
                baseMesh.bindposes.Length !=
                ExpectedRiggedBones)
            {
                throw new InvalidOperationException(
                    "Extracted Revolution barrel geometry counts differ. CombinedTriangles=" +
                    triangles + ", BaseBindPoses=" +
                    baseMesh.bindposes.Length + ".");
            }
        }

        private static IReadOnlyList<Mesh>
            RequireExtractedRightBarrelGroupMeshes(
                Transform model)
        {
            var spinBone = RequireDescendant(
                model,
                RightSpinBoneName);
            var meshes = new List<Mesh>(
                RightBarrelGroupCount);
            for (var groupIndex = 0;
                 groupIndex < RightBarrelGroupCount;
                 groupIndex++)
            {
                var suffix = (groupIndex + 1)
                    .ToString(
                        "D2",
                        CultureInfo.InvariantCulture);
                var groupName =
                    RightBarrelGroupNamePrefix + suffix;
                var group = spinBone
                    .GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item =>
                        item.name == groupName) ??
                    throw new InvalidOperationException(
                        "The corrected right barrel ring is missing " +
                        groupName + ".");
                if (group.parent != spinBone)
                {
                    throw new InvalidOperationException(
                        groupName +
                        " must remain a direct child of the right barrel ring.");
                }
                var filter =
                    group.GetComponent<MeshFilter>();
                var renderer =
                    group.GetComponent<MeshRenderer>();
                var expectedPath =
                    RightBarrelGroupAssetPrefix +
                    suffix + ".asset";
                if (filter == null ||
                    renderer == null ||
                    filter.sharedMesh == null ||
                    AssetDatabase.GetAssetPath(
                        filter.sharedMesh) !=
                    expectedPath ||
                    renderer.sharedMaterials.Length !=
                    ExpectedMaterials)
                {
                    throw new InvalidOperationException(
                        groupName +
                        " does not contain its extracted barrel mesh and approved material slots.");
                }
                meshes.Add(filter.sharedMesh);
            }
            return meshes;
        }

        private static Mesh RequireExtractedBarrelMesh(
            Transform model,
            string spinBoneName,
            string expectedMeshPath)
        {
            var spinBone =
                RequireDescendant(model, spinBoneName);
            var filter =
                spinBone.GetComponent<MeshFilter>();
            var renderer =
                spinBone.GetComponent<MeshRenderer>();
            if (filter == null ||
                renderer == null ||
                filter.sharedMesh == null ||
                AssetDatabase.GetAssetPath(
                    filter.sharedMesh) != expectedMeshPath ||
                renderer.sharedMaterials.Length !=
                ExpectedMaterials)
            {
                throw new InvalidOperationException(
                    spinBoneName +
                    " does not contain the extracted barrel-only mesh and approved material slots.");
            }
            return filter.sharedMesh;
        }

        private static void RequireGeometryCounts(
            Mesh mesh,
            int expectedBones)
        {
            var triangles =
                Enumerable.Range(0, mesh.subMeshCount)
                    .Sum(index =>
                        checked(
                            (int)mesh.GetIndexCount(index) /
                            3));
            if (triangles != ExpectedTriangles ||
                mesh.subMeshCount != ExpectedMaterials ||
                mesh.bindposes.Length != expectedBones)
            {
                throw new InvalidOperationException(
                    "Revolution geometry counts differ. Triangles=" +
                    triangles + ", SubMeshes=" +
                    mesh.subMeshCount + ", BindPoses=" +
                    mesh.bindposes.Length + ".");
            }
        }

        private static void RequireApprovedMaterials(
            SkinnedMeshRenderer renderer)
        {
            if (renderer.sharedMaterials.Length !=
                    ExpectedMaterials ||
                renderer.sharedMaterials.Any(material =>
                    material == null ||
                    !AssetDatabase.GetAssetPath(material)
                        .StartsWith(
                            ApprovedMaterialFolder,
                            StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Revolution_04 approved materials changed.");
            }
        }

        private static SkinnedMeshRenderer RequireSingleRenderer(
            Transform model)
        {
            var renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_04 must contain exactly one skinned renderer. Count=" +
                    renderers.Length + ".");
            }
            return renderers[0];
        }

        private static Animator GetOrCreateAnimator(
            Transform model)
        {
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Revolution_04 must not contain multiple Animators. Count=" +
                    animators.Length + ".");
            }
            return animators.Length == 1
                ? animators[0]
                : Undo.AddComponent<Animator>(
                    model.gameObject);
        }

        private static Animator RequireAnimator(
            Transform model)
        {
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_04 must contain exactly one Animator. Count=" +
                    animators.Length + ".");
            }
            return animators[0];
        }

        private static void RestoreApprovedBaseBonePose(
            SkinnedMeshRenderer target,
            SkinnedMeshRenderer approvedSource)
        {
            if (target.bones.Length != ExpectedBaseBones ||
                approvedSource.bones.Length != ExpectedBaseBones)
            {
                throw new InvalidOperationException(
                    "Revolution base bone pose restoration requires matching 24-bone rigs.");
            }
            for (var index = 0;
                 index < ExpectedBaseBones;
                 index++)
            {
                var targetBone = target.bones[index];
                var sourceBone = approvedSource.bones[index];
                if (targetBone.name != sourceBone.name)
                {
                    throw new InvalidOperationException(
                        "Revolution base bone pose restoration found a bone-order mismatch at index " +
                        index + ".");
                }
                targetBone.localPosition =
                    sourceBone.localPosition;
                targetBone.localRotation =
                    sourceBone.localRotation;
                targetBone.localScale =
                    sourceBone.localScale;
                EditorUtility.SetDirty(targetBone);
            }
        }

        private static void DisablePreviousAnimatorForRebuild(
            Transform model)
        {
            foreach (var animator in
                     model.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }
        }

        private static void RemoveInvalidAndPreviousRig(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            RestoreInvalidHandParent(
                model,
                "LeftForeArm",
                "LeftHand",
                InvalidLeftSpinPivotName);
            RestoreInvalidHandParent(
                model,
                "RightForeArm",
                "RightHand",
                InvalidRightSpinPivotName);
            DestroyNamedIfPresent(model, LeftSpinBoneName);
            DestroyNamedIfPresent(model, RightSpinBoneName);
            DestroyNamedIfPresent(model, LeftFlashPivotName);
            DestroyNamedIfPresent(model, RightFlashPivotName);
            if (AssetDatabase.GetAssetPath(renderer.sharedMesh) ==
                RiggedMeshPath)
            {
                var approvedAsset =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        ApprovedModelPath) ??
                    throw new InvalidOperationException(
                        "Approved Revolution model asset is missing.");
                var approvedRenderer =
                    approvedAsset.GetComponentsInChildren<
                            SkinnedMeshRenderer>(true)
                        .SingleOrDefault() ??
                    throw new InvalidOperationException(
                        "Approved Revolution model renderer is missing.");
                renderer.sharedMesh =
                    approvedRenderer.sharedMesh;
                renderer.bones =
                    renderer.bones
                        .Take(ExpectedBaseBones)
                        .ToArray();
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void RestoreInvalidHandParent(
            Transform model,
            string forearmName,
            string handName,
            string invalidPivotName)
        {
            var forearm =
                RequireDescendant(model, forearmName);
            var hand =
                RequireDescendant(model, handName);
            var pivot = model
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item =>
                    item.name == invalidPivotName);
            if (pivot == null)
            {
                return;
            }
            hand.SetParent(forearm, true);
            UnityEngine.Object.DestroyImmediate(
                pivot.gameObject);
        }

        private static void DestroyNamedIfPresent(
            Transform model,
            string name)
        {
            var item = model
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate =>
                    candidate.name == name);
            if (item != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    item.gameObject);
            }
        }

        private static void EnsureEditableSlotModel(
            Transform model)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(
                    model.gameObject))
            {
                return;
            }
            var instanceRoot =
                PrefabUtility
                    .GetOutermostPrefabInstanceRoot(
                        model.gameObject);
            if (instanceRoot == null ||
                instanceRoot.transform != model)
            {
                throw new InvalidOperationException(
                    "Revolution_04 model must be the prefab instance root before scene-only rigging.");
            }
            PrefabUtility.UnpackPrefabInstance(
                instanceRoot,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            if (PrefabUtility.IsPartOfPrefabInstance(
                    model.gameObject))
            {
                throw new InvalidOperationException(
                    "Revolution_04 model could not be unpacked for corrected scene-only rigging.");
            }
        }

        private static IReadOnlyList<Vector3>
            BakeWorldVertices(SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, true);
                return baked.vertices
                    .Select(
                        renderer.transform.TransformPoint)
                    .ToArray();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Transform RequireBone(
            SkinnedMeshRenderer renderer,
            string name)
        {
            return renderer.bones.SingleOrDefault(item =>
                       item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Revolution renderer bone is missing or duplicated: " +
                       name + ".");
        }

        private static Transform RequireDescendant(
            Transform root,
            string name)
        {
            return root
                       .GetComponentsInChildren<Transform>(true)
                       .SingleOrDefault(item =>
                           item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Revolution transform is missing or duplicated: " +
                       name + ".");
        }

        private static Transform RequireModel(
            Transform slot)
        {
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    AttackSlotName +
                    " must contain exactly one model.");
            }
            return slot.GetChild(0);
        }

        private static Transform RequireDirectChild(
            Transform parent,
            string name)
        {
            return parent.Cast<Transform>()
                       .SingleOrDefault(item =>
                           item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Revolution slot is missing: " +
                       name + ".");
        }

        private static GameObject RequirePlacementRoot()
        {
            return GameObject.Find(PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The Revolution placement root is missing.");
        }

        private static void RequireSlotContract(
            Transform root)
        {
            if (root.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The Revolution placement must contain exactly eight slots.");
            }
            for (var index = 0;
                 index < SlotNames.Length;
                 index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "The Revolution slot contract differs at index " +
                        index + ".");
                }
            }
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" +
                    scene.path);
            }
            return scene;
        }

        private static string[] OtherSlotSignatures(
            Transform root)
        {
            return root.Cast<Transform>()
                .Where(item =>
                    item.name != AttackSlotName)
                .Select(HierarchyAndAssetSignature)
                .OrderBy(
                    item => item,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchyAndAssetSignature(
            Transform slot)
        {
            var builder = new StringBuilder();
            foreach (var item in
                     slot.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item =>
                                 AnimationUtility
                                     .CalculateTransformPath(
                                         item,
                                         slot),
                             StringComparer.Ordinal))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        item,
                        slot));
                builder.Append('|');
                builder.Append(Vec(item.localPosition));
                builder.Append('|');
                builder.Append(Quat(item.localRotation));
                builder.Append('|');
                builder.Append(Vec(item.localScale));
                builder.Append(';');
            }
            foreach (var renderer in
                     slot.GetComponentsInChildren<Renderer>(true))
            {
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        (renderer as SkinnedMeshRenderer)
                        ?.sharedMesh));
                builder.Append('|');
                builder.Append(
                    string.Join(
                        ",",
                        renderer.sharedMaterials.Select(
                            AssetDatabase.GetAssetPath)));
                builder.Append(';');
            }
            foreach (var animator in
                     slot.GetComponentsInChildren<Animator>(true))
            {
                builder.Append(animator.enabled);
                builder.Append('|');
                builder.Append(animator.applyRootMotion);
                builder.Append('|');
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        animator
                            .runtimeAnimatorController));
                builder.Append(';');
            }
            return builder.ToString();
        }

        private static string[] ProtectedRootSignatures(
            Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item =>
                    item.name != PlacementRootName)
                .Select(item =>
                    GlobalObjectId
                        .GetGlobalObjectIdSlow(item) +
                    "|" + item.name +
                    "|" + item.activeSelf +
                    "|" + Vec(item.transform.position) +
                    "|" + Quat(item.transform.rotation) +
                    "|" + Vec(item.transform.localScale) +
                    "|" + item.transform.childCount)
                .OrderBy(
                    item => item,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            var parent =
                Path.GetDirectoryName(folder)
                    ?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) ||
                string.IsNullOrEmpty(name) ||
                !AssetDatabase.IsValidFolder(parent))
            {
                throw new InvalidOperationException(
                    "Invalid Revolution animation folder: " +
                    folder);
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void DeleteAssetIfPresent(
            string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(
                    "Could not replace corrected Revolution machine-gun asset: " +
                    path);
            }
        }

        private static IReadOnlyList<QuaternionKey>
            CoalesceQuaternionKeys(
                IEnumerable<QuaternionKey> source)
        {
            return source
                .GroupBy(item =>
                    Mathf.RoundToInt(
                        item.Time * 100000f))
                .OrderBy(group => group.Key)
                .Select(group => group.Last())
                .ToArray();
        }

        private static Keyframe[] CoalesceKeyframes(
            IEnumerable<Keyframe> source)
        {
            return source
                .GroupBy(item =>
                    Mathf.RoundToInt(
                        item.time * 100000f))
                .OrderBy(group => group.Key)
                .Select(group => group.Last())
                .ToArray();
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<QuaternionKey> keys)
        {
            var continuous =
                new List<QuaternionKey>(keys.Count);
            Quaternion? previous = null;
            foreach (var item in keys)
            {
                var rotation = item.Value;
                if (previous.HasValue &&
                    Quaternion.Dot(
                        previous.Value,
                        rotation) < 0f)
                {
                    rotation = new Quaternion(
                        -rotation.x,
                        -rotation.y,
                        -rotation.z,
                        -rotation.w);
                }
                continuous.Add(
                    new QuaternionKey(
                        item.Time,
                        rotation));
                previous = rotation;
            }
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                continuous.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.x)));
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                continuous.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.y)));
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                continuous.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.z)));
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                continuous.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.w)));
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IEnumerable<Keyframe> keys)
        {
            var curve =
                new AnimationCurve(keys.ToArray());
            for (var index = 0;
                 index < curve.length;
                 index++)
            {
                AnimationUtility
                    .SetKeyLeftTangentMode(
                        curve,
                        index,
                        AnimationUtility
                            .TangentMode.Linear);
                AnimationUtility
                    .SetKeyRightTangentMode(
                        curve,
                        index,
                        AnimationUtility
                            .TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static void RequireSameTransform(
            TransformState expected,
            Transform actual,
            string label)
        {
            if (Vector3.Distance(
                    expected.LocalPosition,
                    actual.localPosition) > 0.000001f ||
                Quaternion.Angle(
                    expected.LocalRotation,
                    actual.localRotation) > 0.0001f ||
                Vector3.Distance(
                    expected.LocalScale,
                    actual.localScale) > 0.000001f)
            {
                throw new InvalidOperationException(
                    label +
                    " Transform changed unexpectedly.");
            }
        }

        private static void RestoreAll(
            IEnumerable<TransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }
        }

        private static float MinimumComponent(
            Vector3 value)
        {
            return Mathf.Min(
                value.x,
                Mathf.Min(value.y, value.z));
        }

        private static float MaximumComponent(
            Vector3 value)
        {
            return Mathf.Max(
                value.x,
                Mathf.Max(value.y, value.z));
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    relative));
        }

        private static string Num(float value)
        {
            return value.ToString(
                "0.######",
                CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" +
                   Num(value.x) + "," +
                   Num(value.y) + "," +
                   Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" +
                   Num(value.x) + "," +
                   Num(value.y) + "," +
                   Num(value.z) + "," +
                   Num(value.w) + ")";
        }

        private sealed class ReadableSource
        {
            public ReadableSource(
                GameObject asset,
                SkinnedMeshRenderer renderer,
                Mesh mesh)
            {
                Asset = asset;
                Renderer = renderer;
                Mesh = mesh;
            }

            public GameObject Asset { get; }
            public SkinnedMeshRenderer Renderer { get; }
            public Mesh Mesh { get; }
        }

        private sealed class BarrelSelection
        {
            public BarrelSelection(
                HashSet<int> vertexIndices,
                int componentCount,
                Vector3 axis,
                Vector3 axisPoint,
                Vector3 muzzlePosition,
                float meanBarrelOrbitRadius,
                IReadOnlyList<HashSet<int>> vertexGroups =
                null)
            {
                VertexIndices = vertexIndices;
                ComponentCount = componentCount;
                Axis = axis;
                AxisPoint = axisPoint;
                MuzzlePosition = muzzlePosition;
                MeanBarrelOrbitRadius =
                    meanBarrelOrbitRadius;
                VertexGroups =
                    vertexGroups ??
                    Array.Empty<HashSet<int>>();
            }

            public HashSet<int> VertexIndices { get; }
            public int ComponentCount { get; }
            public Vector3 Axis { get; }
            public Vector3 AxisPoint { get; }
            public Vector3 MuzzlePosition { get; }
            // Read-only geometry measurement used to mirror the
            // accepted left-arm ring radius onto the right arm.
            public float MeanBarrelOrbitRadius { get; }
            public IReadOnlyList<HashSet<int>>
                VertexGroups { get; }
        }

        private readonly struct MuzzleCandidate
        {
            public MuzzleCandidate(
                Vector3 position,
                float projection)
            {
                Position = position;
                Projection = projection;
            }

            public Vector3 Position { get; }
            public float Projection { get; }
        }

        private sealed class ComponentDescription
        {
            public ComponentDescription(
                int[] vertexIndices,
                float minimumProjection,
                float maximumProjection,
                float meanProjection,
                float meanRadialDistance,
                float handWeight)
            {
                VertexIndices = vertexIndices;
                MinimumProjection = minimumProjection;
                MaximumProjection = maximumProjection;
                MeanProjection = meanProjection;
                MeanRadialDistance = meanRadialDistance;
                HandWeight = handWeight;
            }

            public int[] VertexIndices { get; }
            public float MinimumProjection { get; }
            public float MaximumProjection { get; }
            public float MeanProjection { get; }
            public float MeanRadialDistance { get; }
            public float HandWeight { get; }
            public float ProjectionLength =>
                MaximumProjection - MinimumProjection;
        }

        private sealed class GunRig
        {
            public GunRig(
                Transform forearm,
                Transform hand,
                Transform spinBone,
                Transform flashPivot,
                Transform flash,
                Vector3 firingAxis,
                Vector3 muzzlePosition,
                Quaternion baseLocalRotation,
                int barrelVertexCount,
                int barrelComponentCount)
            {
                Forearm = forearm;
                Hand = hand;
                SpinBone = spinBone;
                FlashPivot = flashPivot;
                Flash = flash;
                FiringAxis = firingAxis;
                MuzzlePosition = muzzlePosition;
                BaseLocalRotation = baseLocalRotation;
                BarrelVertexCount = barrelVertexCount;
                BarrelComponentCount =
                    barrelComponentCount;
                CounterRotatingBarrelGroups =
                    new List<CounterRotatingBarrelGroup>();
            }

            public Transform Forearm { get; }
            public Transform Hand { get; }
            public Transform SpinBone { get; }
            public Transform FlashPivot { get; }
            public Transform Flash { get; }
            public Vector3 FiringAxis { get; }
            public Vector3 MuzzlePosition { get; }
            public Quaternion BaseLocalRotation { get; }
            public int BarrelVertexCount { get; }
            public int BarrelComponentCount { get; }
            public List<CounterRotatingBarrelGroup>
                CounterRotatingBarrelGroups { get; }
        }

        private sealed class CounterRotatingBarrelGroup
        {
            public CounterRotatingBarrelGroup(
                Transform transform,
                Quaternion baseLocalRotation)
            {
                Transform = transform;
                BaseLocalRotation = baseLocalRotation;
            }

            public Transform Transform { get; }
            public Quaternion BaseLocalRotation { get; }
        }

        private sealed class AimPose
        {
            public AimPose(
                Vector3 forward,
                IReadOnlyDictionary<string, Quaternion>
                    localRotations,
                float leftError,
                float rightError)
            {
                Forward = forward;
                LocalRotations = localRotations;
                LeftError = leftError;
                RightError = rightError;
            }

            public Vector3 Forward { get; }
            public IReadOnlyDictionary<string, Quaternion>
                LocalRotations { get; }
            public float LeftError { get; }
            public float RightError { get; }
        }

        private readonly struct QuaternionKey
        {
            public QuaternionKey(
                float time,
                Quaternion value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public Quaternion Value { get; }
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

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }

            public static TransformState Capture(
                Transform target)
            {
                return new TransformState(
                    target.localPosition,
                    target.localRotation,
                    target.localScale);
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

        private sealed class RendererEnabledSnapshot
        {
            private readonly bool enabled;

            public RendererEnabledSnapshot(
                Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public Renderer Renderer { get; }

            public void Restore()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = enabled;
                }
            }
        }

        private sealed class MotionMetrics
        {
            public float LeftForwardAimErrorDegrees
            {
                get;
                set;
            }

            public float RightForwardAimErrorDegrees
            {
                get;
                set;
            }

            public float MinimumFlashOnScale { get; set; }
            public float MaximumFlashOffScale { get; set; }
            public float MaximumRotationError { get; set; }

            public float MaximumFixedAimBoneDriftDegrees
            {
                get;
                set;
            }

            public float MaximumRootPositionError
            {
                get;
                set;
            }

            public float MaximumRootRotationError
            {
                get;
                set;
            }

            public float LoopBoundaryError { get; set; }
            public float LeftRightwardDisplacement { get; set; }
            public float RightRightwardDisplacement { get; set; }
            public int LeftBarrelVertices { get; set; }
            public int RightBarrelVertices { get; set; }
            public int LeftBarrelComponents { get; set; }
            public int RightBarrelComponents { get; set; }
            public Vector3 LeftMuzzlePosition { get; set; }
            public Vector3 RightMuzzlePosition { get; set; }
        }
    }
}
