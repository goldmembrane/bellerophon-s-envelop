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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static class PahurGroundedIdleAnimationTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string IdleSlotName = "Pahur_02_Idle";
        private const string ModelName = "Pahur_Model";
        private const string ArtRoot =
            "Assets/_Project/Art/Enemies/Pahur";
        private const string ModelPath =
            ArtRoot + "/Models/Pahur.fbx";
        private const string ApprovedMaterialFolder =
            ArtRoot + "/ApprovedAppearance/Materials/";
        private const string AnimationFolder =
            ArtRoot + "/Animations";
        private const string ControllerFolder =
            ArtRoot + "/Controllers";
        private const string ClipPath =
            AnimationFolder + "/Pahur_02_GroundedIdle.anim";
        private const string ControllerPath =
            ControllerFolder + "/Pahur_02_GroundedIdle.controller";
        private const string StateName = "PahurGroundedIdle";
        private const string ValidationFolder =
            "docs/validation/pahur_idle_animation_2026-07-31";
        private const string ReportPath =
            ValidationFolder + "/Pahur_02_GroundedIdle_Validation.txt";
        private const string CapturePath =
            ValidationFolder + "/Pahur_02_GroundedIdle_Review.png";
        private static readonly long[]
            KnownSupersededCaptureBytes =
            {
                137432L,
                113207L,
                112288L
            };
        private const string ExpectedFbxSha256 =
            "5A2354A0B89A451DB98EF5AA5409C61EE12CF5638FA6EC2C88110B4C146B537C";
        private const float LoopSeconds = 2f;
        private const float FrameRate = 60f;
        private const float VerticalTravelWorld = 0.03f;
        private const float FootPositionToleranceWorld = 0.002f;
        private const float FootBoneCompensationToleranceWorld =
            0.002f;
        private const float FootSoleSelectionHeightWorld =
            0.005f;
        private const float GroundContactToleranceWorld = 0.003f;
        private const float LoopTolerance = 0.00002f;
        private const int ExpectedTriangles = 4330;
        private const int ExpectedBones = 24;

        private static readonly string[] SlotNames =
        {
            "Pahur_01_Static_Review",
            "Pahur_02_Idle",
            "Pahur_03_Move",
            "Pahur_04_MiniFlamethrower",
            "Pahur_05_BreakthroughFlamethrower",
            "Pahur_06_GuardianFlamethrower",
            "Pahur_07_Stop",
            "Pahur_08_ToGuardianStance",
            "Pahur_09_FromGuardianStance",
            "Pahur_10_Hit",
            "Pahur_11_Death"
        };

        private static readonly string[] AnimatedLegBoneNames =
        {
            "LeftUpLeg",
            "LeftLeg",
            "LeftFoot",
            "RightUpLeg",
            "RightLeg",
            "RightFoot"
        };

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Apply Grounded Idle Animation")]
        public static void ApplyPahurGroundedIdleAnimation()
        {
            var scene = RequireCurrentScene();
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var idleSlot = RequireDirectChild(
                root.transform,
                IdleSlotName);
            var model = RequireModel(idleSlot);
            RequireApprovedAppearance(model, IdleSlotName);
            if (scene.isDirty &&
                !IsKnownIncompleteApply(
                    root.transform,
                    idleSlot,
                    model))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes outside the known incomplete Pahur idle apply. Save or discard them before applying the Pahur idle animation.");
            }

            if (scene.isDirty)
            {
                Debug.Log(
                    "PahurGroundedIdleAnimation: continuing the known incomplete previous apply without reopening the scene.");
            }

            var protectedBefore =
                ProtectedRootSignatures(scene);
            var otherSlotsBefore =
                OtherSlotSignatures(root.transform);
            var placementBefore =
                PlacementTransformSignatures(root.transform);
            var poseBefore =
                root.transform
                    .GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();

            EnsureAssetFolder(AnimationFolder);
            EnsureAssetFolder(ControllerFolder);
            var clip = CreateIdleClip(model);
            var controller = CreateIdleController(clip);
            var animator = GetOrCreateAnimator(model);

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode =
                AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode =
                AnimatorUpdateMode.Normal;
            foreach (var animation in
                     model.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }

            animator.Rebind();
            animator.Update(0f);
            EditorUtility.SetDirty(animator);

            var metrics = InspectMotion(
                root.transform,
                idleSlot,
                model,
                animator,
                clip,
                controller);
            foreach (var snapshot in poseBefore)
            {
                snapshot.Restore();
            }

            RequireSameSignatures(
                otherSlotsBefore,
                OtherSlotSignatures(root.transform),
                "A Pahur slot outside Pahur_02_Idle changed while applying the idle animation.");
            RequireSameSignatures(
                placementBefore,
                PlacementTransformSignatures(root.transform),
                "A Pahur placement, model, or bone transform changed outside animation evaluation.");
            RequireSameSignatures(
                protectedBefore,
                ProtectedRootSignatures(scene),
                "A scene root outside the Pahur placement changed while applying the idle animation.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Pahur idle animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurGroundedIdleAnimationApplied Result=PASS" +
                ", Slot=" + IdleSlotName +
                ", LoopSeconds=" + Num(LoopSeconds) +
                ", VerticalTravelWorld=" +
                Num(metrics.HipsVerticalTravel) +
                ", MaximumFootPositionErrorWorld=" +
                Num(metrics.MaximumFootPositionError) +
                ", MaximumFootSolePositionErrorWorld=" +
                Num(metrics.MaximumFootSolePositionError) +
                ", MinimumMeshYVariationWorld=" +
                Num(metrics.MinimumMeshYVariation) +
                ", RootMotion=False" +
                ", BlendShapeBindings=0" +
                ", OtherSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Validate Grounded Idle Animation")]
        public static void ValidatePahurGroundedIdleAnimation()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var idleSlot = RequireDirectChild(
                root.transform,
                IdleSlotName);
            var model = RequireModel(idleSlot);
            var animator = RequireAnimator(model);
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    ClipPath) ??
                throw new InvalidOperationException(
                    "The Pahur grounded idle clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur grounded idle controller is missing.");

            var metrics = InspectMotion(
                root.transform,
                idleSlot,
                model,
                animator,
                clip,
                controller);
            WriteValidationReport(metrics);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur idle validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurGroundedIdleAnimationValidated Result=PASS" +
                ", Slot=" + IdleSlotName +
                ", LoopSeconds=" + Num(LoopSeconds) +
                ", VerticalTravelWorld=" +
                Num(metrics.HipsVerticalTravel) +
                ", UpperBodyVerticalTravelWorld=" +
                Num(metrics.UpperBodyVerticalTravel) +
                ", MaximumFootPositionErrorWorld=" +
                Num(metrics.MaximumFootPositionError) +
                ", MaximumFootSolePositionErrorWorld=" +
                Num(metrics.MaximumFootSolePositionError) +
                ", MinimumMeshYVariationWorld=" +
                Num(metrics.MinimumMeshYVariation) +
                ", LoopBoundaryError=" +
                Num(metrics.LoopBoundaryError) +
                ", RootPositionError=" +
                Num(metrics.RootPositionError) +
                ", SceneChanged=False.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Capture Grounded Idle Review")]
        public static void CapturePahurGroundedIdleAnimationReview()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot();
            RequireSlotContract(root.transform);
            var idleSlot = RequireDirectChild(
                root.transform,
                IdleSlotName);
            var model = RequireModel(idleSlot);
            var animator = RequireAnimator(model);
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    ClipPath) ??
                throw new InvalidOperationException(
                    "The Pahur grounded idle clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur grounded idle controller is missing.");

            InspectMotion(
                root.transform,
                idleSlot,
                model,
                animator,
                clip,
                controller);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
            {
                var existing = new FileInfo(destination);
                if (!KnownSupersededCaptureBytes.Contains(
                        existing.Length))
                {
                    throw new InvalidOperationException(
                        "The one-time Pahur idle review already exists: " +
                        CapturePath);
                }

                File.Delete(destination);
                Debug.Log(
                    "PahurGroundedIdleAnimation: replacing a known superseded review image at the same path.");
            }

            CapturePoseStrip(
                model,
                animator,
                clip,
                destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur idle review capture changed the scene dirty state.");
            }

            Debug.Log(
                "PahurGroundedIdleAnimationReviewCaptured Result=PASS" +
                ", Slot=" + IdleSlotName +
                ", Times=0,0.5,1,1.5,2" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static AnimationClip CreateIdleClip(
            Transform animatorRoot)
        {
            DeleteAssetIfPresent(ClipPath);
            var hips =
                RequireDescendant(animatorRoot, "Hips");
            var left = CreateLeg(
                animatorRoot,
                "LeftUpLeg",
                "LeftLeg",
                "LeftFoot");
            var right = CreateLeg(
                animatorRoot,
                "RightUpLeg",
                "RightLeg",
                "RightFoot");
            var animatedBones =
                left.AllBones.Concat(right.AllBones)
                    .Distinct()
                    .ToArray();
            var snapshots =
                animatorRoot
                    .GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            var hipsPositionKeys =
                new List<Vector3Key>();
            var footPositionKeys =
                new Dictionary<Transform, List<Vector3Key>>
                {
                    [left.Foot] =
                        new List<Vector3Key>(),
                    [right.Foot] =
                        new List<Vector3Key>()
                };
            var rotationKeys =
                animatedBones.ToDictionary(
                    item => item,
                    _ => new List<QuaternionKey>());
            var restHipsWorld = hips.position;
            var maximumSolveError = 0f;
            var renderer =
                RequireSingleRenderer(
                    animatorRoot,
                    IdleSlotName);
            var baked = new Mesh();

            try
            {
                var leftFootTarget = left.Foot.position;
                var leftFootRotation = left.Foot.rotation;
                var rightFootTarget = right.Foot.position;
                var rightFootRotation = right.Foot.rotation;
                var leftSoleIndices =
                    SpatialFootSoleVertexIndices(
                        renderer,
                        baked,
                        left.Foot);
                var rightSoleIndices =
                    SpatialFootSoleVertexIndices(
                        renderer,
                        baked,
                        right.Foot);
                var leftSoleTarget =
                    Average(
                        BakedWorldVertices(
                            renderer,
                            baked,
                            leftSoleIndices));
                var rightSoleTarget =
                    Average(
                        BakedWorldVertices(
                            renderer,
                            baked,
                            rightSoleIndices));
                var frameCount = Mathf.RoundToInt(
                    LoopSeconds * FrameRate);
                for (var frame = 0;
                     frame <= frameCount;
                     frame++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var time = frame / FrameRate;
                    var normalized = time / LoopSeconds;
                    var downwardOffset =
                        0.5f *
                        (1f - Mathf.Cos(
                            normalized * Mathf.PI * 2f)) *
                        VerticalTravelWorld;
                    hips.position =
                        restHipsWorld -
                        Vector3.up * downwardOffset;

                    SolveTwoBoneLeg(
                        left.Joints,
                        left.Foot,
                        leftFootTarget);
                    left.Foot.rotation = leftFootRotation;
                    SolveTwoBoneLeg(
                        right.Joints,
                        right.Foot,
                        rightFootTarget);
                    right.Foot.rotation = rightFootRotation;

                    maximumSolveError = Mathf.Max(
                        maximumSolveError,
                        Vector3.Distance(
                            left.Foot.position,
                            leftFootTarget),
                        Vector3.Distance(
                            right.Foot.position,
                            rightFootTarget));
                    CompensateFootSole(
                        renderer,
                        baked,
                        left.Foot,
                        leftSoleIndices,
                        leftSoleTarget,
                        leftFootRotation);
                    CompensateFootSole(
                        renderer,
                        baked,
                        right.Foot,
                        rightSoleIndices,
                        rightSoleTarget,
                        rightFootRotation);
                    hipsPositionKeys.Add(
                        new Vector3Key(
                            time,
                            hips.localPosition));
                    footPositionKeys[left.Foot].Add(
                        new Vector3Key(
                            time,
                            left.Foot.localPosition));
                    footPositionKeys[right.Foot].Add(
                        new Vector3Key(
                            time,
                            right.Foot.localPosition));
                    foreach (var bone in animatedBones)
                    {
                        rotationKeys[bone].Add(
                            new QuaternionKey(
                                time,
                                bone.localRotation));
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            if (maximumSolveError >
                FootPositionToleranceWorld * 0.25f)
            {
                throw new InvalidOperationException(
                    "The generated Pahur idle leg solve could not keep both feet fixed. MaximumError=" +
                    Num(maximumSolveError) + ".");
            }

            var clip = new AnimationClip
            {
                name = "Pahur_02_GroundedIdle",
                frameRate = FrameRate
            };
            var settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);

            var hipsPath =
                AnimationUtility.CalculateTransformPath(
                    hips,
                    animatorRoot);
            SetVector3PositionCurves(
                clip,
                hipsPath,
                hipsPositionKeys);
            foreach (var pair in footPositionKeys)
            {
                SetVector3PositionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        pair.Key,
                        animatorRoot),
                    pair.Value);
            }
            foreach (var pair in rotationKeys)
            {
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        pair.Key,
                        animatorRoot),
                    pair.Value);
            }

            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static LegChain CreateLeg(
            Transform root,
            string upperName,
            string lowerName,
            string footName)
        {
            var upper = RequireDescendant(root, upperName);
            var lower = RequireDescendant(root, lowerName);
            var foot = RequireDescendant(root, footName);
            if (lower.parent != upper ||
                foot.parent != lower)
            {
                throw new InvalidOperationException(
                    upperName +
                    " is not a continuous Pahur leg chain.");
            }

            return new LegChain(
                new[] { upper, lower },
                foot);
        }

        private static void SolveTwoBoneLeg(
            IReadOnlyList<Transform> joints,
            Transform foot,
            Vector3 target)
        {
            if (joints.Count != 2)
            {
                throw new InvalidOperationException(
                    "The Pahur idle solver requires a two-joint leg chain.");
            }

            var upper = joints[0];
            var lower = joints[1];
            var upperLength =
                Vector3.Distance(
                    upper.position,
                    lower.position);
            var lowerLength =
                Vector3.Distance(
                    lower.position,
                    foot.position);
            var toTarget = target - upper.position;
            var targetDistance = toTarget.magnitude;
            if (targetDistance <=
                    Mathf.Abs(upperLength - lowerLength) ||
                targetDistance >=
                    upperLength + lowerLength)
            {
                throw new InvalidOperationException(
                    "The Pahur idle foot target is outside the authored leg reach.");
            }

            var targetDirection =
                toTarget / targetDistance;
            var authoredBendDirection =
                Vector3.ProjectOnPlane(
                    lower.position - upper.position,
                    targetDirection);
            if (authoredBendDirection.sqrMagnitude <
                0.0000000001f)
            {
                throw new InvalidOperationException(
                    "The authored Pahur leg is perfectly straight, so its knee direction cannot be preserved without guessing.");
            }

            authoredBendDirection.Normalize();
            var distanceAlongTarget =
                (upperLength * upperLength +
                 targetDistance * targetDistance -
                 lowerLength * lowerLength) /
                (2f * targetDistance);
            var bendDistance = Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    upperLength * upperLength -
                    distanceAlongTarget *
                    distanceAlongTarget));
            var desiredLowerPosition =
                upper.position +
                targetDirection * distanceAlongTarget +
                authoredBendDirection * bendDistance;

            upper.rotation =
                Quaternion.FromToRotation(
                    lower.position - upper.position,
                    desiredLowerPosition - upper.position) *
                upper.rotation;
            lower.rotation =
                Quaternion.FromToRotation(
                    foot.position - lower.position,
                    target - lower.position) *
                lower.rotation;
        }

        private static AnimatorController CreateIdleController(
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
            state.speed = 1f;
            controller.layers[0]
                .stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static MotionMetrics InspectMotion(
            Transform placementRoot,
            Transform idleSlot,
            Transform model,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            RequireApprovedAppearance(model, IdleSlotName);
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode !=
                    AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Pahur_02_Idle Animator does not match the grounded idle contract.");
            }

            if (controller.layers.Length != 1 ||
                controller.layers[0]
                    .stateMachine.defaultState == null ||
                controller.layers[0]
                    .stateMachine.defaultState.name !=
                    StateName ||
                controller.layers[0]
                    .stateMachine.defaultState.motion != clip ||
                Mathf.Abs(clip.length - LoopSeconds) >
                    0.0001f ||
                !AnimationUtility
                    .GetAnimationClipSettings(clip)
                    .loopTime)
            {
                throw new InvalidOperationException(
                    "The Pahur grounded idle clip or controller contract differs.");
            }

            RequireClipBindings(
                clip,
                animator.transform);
            var metrics = MeasureMotion(
                model,
                animator,
                clip);
            if (Mathf.Abs(
                metrics.HipsVerticalTravel -
                    VerticalTravelWorld) > 0.001f ||
                Mathf.Abs(
                    metrics.UpperBodyVerticalTravel -
                    VerticalTravelWorld) > 0.001f ||
                metrics.MaximumFootPositionError >
                    FootBoneCompensationToleranceWorld ||
                metrics.MaximumFootSolePositionError >
                    GroundContactToleranceWorld ||
                metrics.MinimumMeshYVariation >
                    GroundContactToleranceWorld ||
                metrics.LeftLegRotationDelta < 0.25f ||
                metrics.RightLegRotationDelta < 0.25f ||
                metrics.LeftLegRotationDelta > 120f ||
                metrics.RightLegRotationDelta > 120f ||
                metrics.LoopBoundaryError > LoopTolerance ||
                metrics.RootPositionError > 0.00001f)
            {
                throw new InvalidOperationException(
                    "Pahur grounded idle metrics differ. " +
                    "HipsVerticalTravel=" +
                    Num(metrics.HipsVerticalTravel) +
                    ", UpperBodyVerticalTravel=" +
                    Num(metrics.UpperBodyVerticalTravel) +
                    ", MaximumFootPositionError=" +
                    Num(metrics.MaximumFootPositionError) +
                    ", MaximumFootSolePositionError=" +
                    Num(metrics.MaximumFootSolePositionError) +
                    ", MinimumMeshYVariation=" +
                    Num(metrics.MinimumMeshYVariation) +
                    ", LeftLegRotationDelta=" +
                    Num(metrics.LeftLegRotationDelta) +
                    ", RightLegRotationDelta=" +
                    Num(metrics.RightLegRotationDelta) +
                    ", LoopBoundaryError=" +
                    Num(metrics.LoopBoundaryError) +
                    ", RootPositionError=" +
                    Num(metrics.RootPositionError) + ".");
            }

            foreach (var slot in
                     placementRoot.Cast<Transform>())
            {
                if (slot == idleSlot)
                {
                    continue;
                }

                var otherAnimator =
                    slot.GetChild(0)
                        .GetComponentInChildren<Animator>(true);
                if (otherAnimator != null &&
                    otherAnimator.runtimeAnimatorController ==
                    controller)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " incorrectly uses the Pahur idle controller.");
                }
            }

            return metrics;
        }

        private static bool IsKnownIncompleteApply(
            Transform placementRoot,
            Transform idleSlot,
            Transform model)
        {
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    ClipPath);
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (clip == null ||
                controller == null ||
                animators.Length != 1 ||
                animators[0].runtimeAnimatorController != controller ||
                AssetDatabase.GetAssetPath(controller) !=
                    ControllerPath)
            {
                return false;
            }

            foreach (var slot in
                     placementRoot.Cast<Transform>())
            {
                if (slot == idleSlot)
                {
                    continue;
                }

                var otherAnimators =
                    slot.GetComponentsInChildren<Animator>(true);
                if (otherAnimators.Any(item =>
                        item.runtimeAnimatorController ==
                        controller))
                {
                    return false;
                }
            }

            return true;
        }

        private static void RequireClipBindings(
            AnimationClip clip,
            Transform animatorRoot)
        {
            var positionPaths =
                new[]
                {
                    "Hips",
                    "LeftFoot",
                    "RightFoot"
                }
                .Select(name =>
                    AnimationUtility.CalculateTransformPath(
                        RequireDescendant(
                            animatorRoot,
                            name),
                        animatorRoot))
                .ToHashSet(StringComparer.Ordinal);
            var rotationPaths =
                AnimatedLegBoneNames
                    .Select(name =>
                        AnimationUtility.CalculateTransformPath(
                            RequireDescendant(
                                animatorRoot,
                                name),
                            animatorRoot))
                    .ToHashSet(StringComparer.Ordinal);
            var bindings =
                AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 33)
            {
                throw new InvalidOperationException(
                    "Pahur grounded idle must contain exactly 33 Transform curves. Count=" +
                    bindings.Length + ".");
            }

            foreach (var binding in bindings)
            {
                if (binding.type != typeof(Transform) ||
                    binding.propertyName.StartsWith(
                        "blendShape.",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pahur grounded idle contains a non-Transform or morph binding.");
                }

                if (positionPaths.Contains(binding.path) &&
                    (binding.propertyName ==
                         "m_LocalPosition.x" ||
                     binding.propertyName ==
                         "m_LocalPosition.y" ||
                     binding.propertyName ==
                         "m_LocalPosition.z"))
                {
                    continue;
                }

                if (rotationPaths.Contains(binding.path) &&
                    binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                throw new InvalidOperationException(
                    "Unexpected Pahur grounded idle binding: " +
                    binding.path + "/" +
                    binding.propertyName + ".");
            }
        }

        private static MotionMetrics MeasureMotion(
            Transform model,
            Animator animator,
            AnimationClip clip)
        {
            var hips =
                RequireDescendant(animator.transform, "Hips");
            var spine =
                RequireDescendant(animator.transform, "Spine");
            var leftUpper =
                RequireDescendant(animator.transform, "LeftUpLeg");
            var leftLower =
                RequireDescendant(animator.transform, "LeftLeg");
            var leftFoot =
                RequireDescendant(animator.transform, "LeftFoot");
            var rightUpper =
                RequireDescendant(animator.transform, "RightUpLeg");
            var rightLower =
                RequireDescendant(animator.transform, "RightLeg");
            var rightFoot =
                RequireDescendant(animator.transform, "RightFoot");
            var renderer =
                RequireSingleRenderer(model, IdleSlotName);
            var snapshots =
                animator.transform
                    .GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            var animatorEnabled = animator.enabled;
            var rootPosition = model.position;
            var baked = new Mesh();
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(
                    animator.gameObject,
                    0f);
                var leftTarget = leftFoot.position;
                var rightTarget = rightFoot.position;
                var startHipsY = hips.position.y;
                var startSpineY = spine.position.y;
                var startLeftUpper = leftUpper.localRotation;
                var startLeftLower = leftLower.localRotation;
                var startRightUpper = rightUpper.localRotation;
                var startRightLower = rightLower.localRotation;
                var startMinimumY =
                    BakedMinimumWorldY(renderer, baked);
                var footVertexIndices =
                    SpatialFootSoleVertexIndices(
                        renderer,
                        baked,
                        leftFoot)
                    .Concat(
                        SpatialFootSoleVertexIndices(
                            renderer,
                            baked,
                            rightFoot))
                    .ToArray();
                var baselineFootVertices =
                    BakedWorldVertices(
                        renderer,
                        baked,
                        footVertexIndices);
                var minimumHipsY = startHipsY;
                var maximumHipsY = startHipsY;
                var minimumSpineY = startSpineY;
                var maximumSpineY = startSpineY;
                var minimumMeshY = startMinimumY;
                var maximumMeshY = startMinimumY;
                var maximumFootError = 0f;
                var maximumFootSoleError = 0f;
                var leftLegDelta = 0f;
                var rightLegDelta = 0f;
                var rootError = 0f;

                var sampleCount =
                    Mathf.RoundToInt(
                        LoopSeconds * FrameRate * 2f);
                for (var index = 0;
                     index <= sampleCount;
                     index++)
                {
                    var time =
                        index / (FrameRate * 2f);
                    clip.SampleAnimation(
                        animator.gameObject,
                        Mathf.Min(time, LoopSeconds));
                    minimumHipsY =
                        Mathf.Min(minimumHipsY, hips.position.y);
                    maximumHipsY =
                        Mathf.Max(maximumHipsY, hips.position.y);
                    minimumSpineY =
                        Mathf.Min(minimumSpineY, spine.position.y);
                    maximumSpineY =
                        Mathf.Max(maximumSpineY, spine.position.y);
                    maximumFootError = Mathf.Max(
                        maximumFootError,
                        Vector3.Distance(
                            leftFoot.position,
                            leftTarget),
                        Vector3.Distance(
                            rightFoot.position,
                            rightTarget));
                    var currentFootVertices =
                        BakedWorldVertices(
                            renderer,
                            baked,
                            footVertexIndices);
                    for (var vertexIndex = 0;
                         vertexIndex <
                         currentFootVertices.Length;
                         vertexIndex++)
                    {
                        maximumFootSoleError = Mathf.Max(
                            maximumFootSoleError,
                            Vector3.Distance(
                                currentFootVertices[vertexIndex],
                                baselineFootVertices[vertexIndex]));
                    }
                    leftLegDelta = Mathf.Max(
                        leftLegDelta,
                        Quaternion.Angle(
                            startLeftUpper,
                            leftUpper.localRotation) +
                        Quaternion.Angle(
                            startLeftLower,
                            leftLower.localRotation));
                    rightLegDelta = Mathf.Max(
                        rightLegDelta,
                        Quaternion.Angle(
                            startRightUpper,
                            rightUpper.localRotation) +
                        Quaternion.Angle(
                            startRightLower,
                            rightLower.localRotation));
                    var currentMinimumY =
                        BakedMinimumWorldY(renderer, baked);
                    minimumMeshY =
                        Mathf.Min(minimumMeshY, currentMinimumY);
                    maximumMeshY =
                        Mathf.Max(maximumMeshY, currentMinimumY);
                    rootError = Mathf.Max(
                        rootError,
                        Vector3.Distance(
                            model.position,
                            rootPosition));
                }

                clip.SampleAnimation(
                    animator.gameObject,
                    LoopSeconds);
                var loopBoundaryError = Mathf.Max(
                    Mathf.Abs(
                        hips.position.y - startHipsY),
                    Quaternion.Angle(
                        startLeftUpper,
                        leftUpper.localRotation),
                    Quaternion.Angle(
                        startLeftLower,
                        leftLower.localRotation),
                    Quaternion.Angle(
                        startRightUpper,
                        rightUpper.localRotation),
                    Quaternion.Angle(
                        startRightLower,
                        rightLower.localRotation),
                    Vector3.Distance(
                        leftFoot.position,
                        leftTarget),
                    Vector3.Distance(
                        rightFoot.position,
                        rightTarget));

                return new MotionMetrics
                {
                    HipsVerticalTravel =
                        maximumHipsY - minimumHipsY,
                    UpperBodyVerticalTravel =
                        maximumSpineY - minimumSpineY,
                    MaximumFootPositionError =
                        maximumFootError,
                    MaximumFootSolePositionError =
                        maximumFootSoleError,
                    MinimumMeshYVariation =
                        maximumMeshY - minimumMeshY,
                    LeftLegRotationDelta =
                        leftLegDelta,
                    RightLegRotationDelta =
                        rightLegDelta,
                    LoopBoundaryError =
                        loopBoundaryError,
                    RootPositionError =
                        rootError
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }
        }

        private static int[] SpatialFootSoleVertexIndices(
            SkinnedMeshRenderer renderer,
            Mesh baked,
            Transform foot)
        {
            renderer.BakeMesh(baked);
            var vertices = baked.vertices;
            const float footRadiusWorld = 0.3f;
            var footIndices =
                Enumerable.Range(0, vertices.Length)
                    .Where(index =>
                        Vector3.Distance(
                            renderer.transform.TransformPoint(
                                vertices[index]),
                            foot.position) <=
                        footRadiusWorld)
                    .ToArray();
            if (footIndices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Pahur has no baked mesh vertices near foot bone " +
                    foot.name + ".");
            }

            var minimum = footIndices.Min(index =>
                renderer.transform.TransformPoint(
                    vertices[index]).y);
            var soleIndices =
                footIndices.Where(index =>
                    renderer.transform.TransformPoint(
                        vertices[index]).y <=
                    minimum +
                    FootSoleSelectionHeightWorld)
                    .ToArray();
            if (soleIndices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Pahur foot sole vertex selection is empty.");
            }

            return soleIndices;
        }

        private static Vector3[] BakedWorldVertices(
            SkinnedMeshRenderer renderer,
            Mesh baked,
            IReadOnlyList<int> indices)
        {
            renderer.BakeMesh(baked);
            var vertices = baked.vertices;
            var result = new Vector3[indices.Count];
            for (var index = 0;
                 index < indices.Count;
                 index++)
            {
                result[index] =
                    renderer.transform.TransformPoint(
                        vertices[indices[index]]);
            }

            return result;
        }

        private static void CompensateFootSole(
            SkinnedMeshRenderer renderer,
            Mesh baked,
            Transform foot,
            IReadOnlyList<int> soleIndices,
            Vector3 targetCenter,
            Quaternion targetRotation)
        {
            const int iterationCount = 4;
            for (var iteration = 0;
                 iteration < iterationCount;
                 iteration++)
            {
                var currentCenter =
                    Average(
                        BakedWorldVertices(
                            renderer,
                            baked,
                            soleIndices));
                foot.position +=
                    targetCenter - currentCenter;
                foot.rotation = targetRotation;
            }
        }

        private static Vector3 Average(
            IReadOnlyList<Vector3> values)
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot average an empty Pahur foot vertex set.");
            }

            var sum = Vector3.zero;
            foreach (var value in values)
            {
                sum += value;
            }

            return sum / values.Count;
        }

        private static float BakedMinimumWorldY(
            SkinnedMeshRenderer renderer,
            Mesh baked)
        {
            renderer.BakeMesh(baked);
            var vertices = baked.vertices;
            if (vertices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Pahur_02_Idle BakeMesh produced no vertices.");
            }

            var minimum = float.PositiveInfinity;
            foreach (var vertex in vertices)
            {
                minimum = Mathf.Min(
                    minimum,
                    renderer.transform
                        .TransformPoint(vertex).y);
            }

            return minimum;
        }

        private static void CapturePoseStrip(
            Transform model,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur idle review folder."));
            var snapshots =
                animator.transform
                    .GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformSnapshot(item))
                    .ToArray();
            var animatorEnabled = animator.enabled;
            var otherRenderers =
                model.gameObject.scene
                    .GetRootGameObjects()
                    .SelectMany(item =>
                        item.GetComponentsInChildren<Renderer>(
                            true))
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
                "PahurGroundedIdleReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelWidth = 384;
            const int panelHeight = 640;
            var strip = new Texture2D(
                panelWidth * 5,
                panelHeight,
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
            var times =
                new[] { 0f, 0.5f, 1f, 1.5f, 2f };
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = false;
                var camera =
                    cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags =
                    CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                clip.SampleAnimation(
                    animator.gameObject,
                    times[0]);
                FrameCamera(
                    camera,
                    model,
                    sourceCamera,
                    panelWidth /
                    (float)panelHeight);
                for (var index = 0;
                     index < times.Length;
                     index++)
                {
                    clip.SampleAnimation(
                        animator.gameObject,
                        times[index]);
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
                            "Pahur idle review contains Unity's magenta shader fallback.");
                    }

                    strip.SetPixels32(
                        index * panelWidth,
                        0,
                        panelWidth,
                        panelHeight,
                        pixels);
                }

                strip.Apply();
                File.WriteAllBytes(
                    destination,
                    strip.EncodeToPNG());
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

                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(
            Camera camera,
            Transform model,
            Camera sourceCamera,
            float aspect)
        {
            var bounds = BoundsOf(model);
            var viewDirection =
                sourceCamera.transform.position -
                bounds.center;
            viewDirection.y = 0f;
            if (viewDirection.sqrMagnitude < 0.0001f)
            {
                viewDirection = Vector3.back;
            }

            viewDirection.Normalize();
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
                1.18f;
            camera.transform.position =
                bounds.center +
                viewDirection * distance +
                Vector3.up *
                (bounds.extents.y * 0.02f);
            camera.transform.rotation =
                Quaternion.LookRotation(
                    bounds.center -
                    camera.transform.position,
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
                    "Pahur_02_Idle has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1;
                 index < renderers.Length;
                 index++)
            {
                bounds.Encapsulate(
                    renderers[index].bounds);
            }

            return bounds;
        }

        private static void SetVector3PositionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Vector3Key> keys)
        {
            SetLinearCurve(
                clip,
                path,
                "m_LocalPosition.x",
                keys.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.x)));
            SetLinearCurve(
                clip,
                path,
                "m_LocalPosition.y",
                keys.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.y)));
            SetLinearCurve(
                clip,
                path,
                "m_LocalPosition.z",
                keys.Select(item =>
                    new Keyframe(
                        item.Time,
                        item.Value.z)));
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
                new AnimationCurve(keys.ToArray())
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
            for (var index = 0;
                 index < curve.length;
                 index++)
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

        private static void RequireApprovedAppearance(
            Transform model,
            string label)
        {
            if (!string.Equals(
                    Sha256(Absolute(ModelPath)),
                    ExpectedFbxSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The approved Pahur FBX hash changed.");
            }

            var renderer =
                RequireSingleRenderer(model, label);
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           label +
                           " approved mesh is missing.");
            var triangles =
                Enumerable.Range(0, mesh.subMeshCount)
                    .Sum(index =>
                        checked(
                            (int)mesh.GetIndexCount(index) /
                            3));
            if (AssetDatabase.GetAssetPath(mesh) !=
                    ModelPath ||
                triangles != ExpectedTriangles ||
                renderer.bones.Length != ExpectedBones ||
                renderer.sharedMaterials.Length == 0 ||
                renderer.sharedMaterials.Any(material =>
                    material == null ||
                    !AssetDatabase
                        .GetAssetPath(material)
                        .StartsWith(
                            ApprovedMaterialFolder,
                            StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    label +
                    " no longer uses the approved Pahur appearance.");
            }
        }

        private static SkinnedMeshRenderer
            RequireSingleRenderer(
                Transform model,
                string label)
        {
            var renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    label +
                    " must contain exactly one skinned renderer. Count=" +
                    renderers.Length + ".");
            }

            return renderers[0];
        }

        private static Animator RequireAnimator(
            Transform model)
        {
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_02_Idle must contain exactly one Animator. Count=" +
                    animators.Length + ".");
            }

            return animators[0];
        }

        private static Animator GetOrCreateAnimator(
            Transform model)
        {
            var animators =
                model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Pahur_02_Idle must not contain multiple Animators. Count=" +
                    animators.Length + ".");
            }

            return animators.Length == 1
                ? animators[0]
                : model.gameObject.AddComponent<Animator>();
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
                       "Required Pahur bone is missing or duplicated: " +
                       name + ".");
        }

        private static Transform RequireModel(
            Transform slot)
        {
            if (slot.childCount != 1 ||
                slot.GetChild(0).name != ModelName)
            {
                throw new InvalidOperationException(
                    IdleSlotName +
                    " must contain exactly one Pahur_Model.");
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
                       "Required Pahur slot is missing: " +
                       name + ".");
        }

        private static GameObject RequirePlacementRoot()
        {
            return GameObject.Find(PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The Pahur placement root is missing.");
        }

        private static void RequireSlotContract(
            Transform root)
        {
            if (root.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The Pahur placement must contain exactly eleven slots.");
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
                        "The Pahur slot contract differs at index " +
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

        private static string[]
            OtherSlotSignatures(Transform root)
        {
            return root.Cast<Transform>()
                .Where(item =>
                    item.name != IdleSlotName)
                .Select(HierarchyAndAssetSignature)
                .OrderBy(
                    item => item,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string
            HierarchyAndAssetSignature(Transform slot)
        {
            var builder = new StringBuilder();
            foreach (var item in
                     slot.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item =>
                                 RelativePath(slot, item),
                             StringComparer.Ordinal))
            {
                builder.Append(
                    RelativePath(slot, item));
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
                        animator.runtimeAnimatorController));
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static string[]
            PlacementTransformSignatures(Transform root)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .Select(item =>
                    RelativePath(root, item) + "|" +
                    Vec(item.localPosition) + "|" +
                    Quat(item.localRotation) + "|" +
                    Vec(item.localScale))
                .OrderBy(
                    item => item,
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string[]
            ProtectedRootSignatures(Scene scene)
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

        private static string RelativePath(
            Transform root,
            Transform item)
        {
            return item == root
                ? root.name
                : root.name + "/" +
                  AnimationUtility
                      .CalculateTransformPath(
                          item,
                          root);
        }

        private static void WriteValidationReport(
            MotionMetrics metrics)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur idle validation folder."));
            var report = new StringBuilder();
            report.AppendLine(
                "Pahur 02 Grounded Idle Animation Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Slot=" + IdleSlotName);
            report.AppendLine("Model=" + ModelName);
            report.AppendLine("Clip=" + ClipPath);
            report.AppendLine(
                "Controller=" + ControllerPath);
            report.AppendLine("State=" + StateName);
            report.AppendLine("LoopSeconds=2");
            report.AppendLine("LoopEnabled=True");
            report.AppendLine("FrameRate=60");
            report.AppendLine(
                "VerticalTravelTargetWorld=" +
                Num(VerticalTravelWorld));
            report.AppendLine(
                "AnimationCenterBelowAuthoredRestWorld=" +
                Num(VerticalTravelWorld * 0.5f));
            report.AppendLine(
                "HipsVerticalTravelWorld=" +
                Num(metrics.HipsVerticalTravel));
            report.AppendLine(
                "UpperBodyVerticalTravelWorld=" +
                Num(metrics.UpperBodyVerticalTravel));
            report.AppendLine(
                "MaximumFootPositionErrorWorld=" +
                Num(metrics.MaximumFootPositionError));
            report.AppendLine(
                "MaximumFootSolePositionErrorWorld=" +
                Num(metrics.MaximumFootSolePositionError));
            report.AppendLine(
                "FootSoleSelectionHeightWorld=" +
                Num(FootSoleSelectionHeightWorld));
            report.AppendLine(
                "FootSoleSelectionRadiusWorld=0.3");
            report.AppendLine(
                "FootBoneCompensationToleranceWorld=" +
                Num(FootBoneCompensationToleranceWorld));
            report.AppendLine(
                "MinimumMeshYVariationWorld=" +
                Num(metrics.MinimumMeshYVariation));
            report.AppendLine(
                "GroundContactToleranceWorld=" +
                Num(GroundContactToleranceWorld));
            report.AppendLine(
                "LeftLegRotationDeltaDegrees=" +
                Num(metrics.LeftLegRotationDelta));
            report.AppendLine(
                "RightLegRotationDeltaDegrees=" +
                Num(metrics.RightLegRotationDelta));
            report.AppendLine(
                "LoopBoundaryError=" +
                Num(metrics.LoopBoundaryError));
            report.AppendLine(
                "RootPositionError=" +
                Num(metrics.RootPositionError));
            report.AppendLine(
                "AnimatedPositionBones=Hips,LeftFoot,RightFoot");
            report.AppendLine(
                "AnimatedRotationBones=" +
                string.Join(
                    ",",
                    AnimatedLegBoneNames));
            report.AppendLine("CurveBindings=33");
            report.AppendLine("BlendShapeBindings=0");
            report.AppendLine("FootSoleGrounded=True");
            report.AppendLine(
                "FootBoneCompensationApplied=True");
            report.AppendLine("KneesAndHipsFlex=True");
            report.AppendLine(
                "UpperBodyFollowsHips=True");
            report.AppendLine("RootMotion=False");
            report.AppendLine(
                "ApprovedAppearancePreserved=True");
            report.AppendLine(
                "OtherPahurSlotsChanged=False");
            report.AppendLine(
                "OtherSceneRootsChanged=False");
            report.AppendLine(
                "SceneChangedByValidation=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void EnsureAssetFolder(
            string folder)
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
                    "Invalid Pahur animation asset folder: " +
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
                    "Could not replace Pahur animation asset: " +
                    path);
            }
        }

        private static void RequireSameSignatures(
            IReadOnlyList<string> before,
            IReadOnlyList<string> after,
            string message)
        {
            if (!before.SequenceEqual(
                    after,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Absolute(
            string relative)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    relative));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter
                .ToString(hash.ComputeHash(stream))
                .Replace("-", string.Empty);
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

        private sealed class LegChain
        {
            public LegChain(
                IReadOnlyList<Transform> joints,
                Transform foot)
            {
                Joints = joints;
                Foot = foot;
            }

            public IReadOnlyList<Transform> Joints
            {
                get;
            }

            public Transform Foot
            {
                get;
            }

            public IEnumerable<Transform> AllBones =>
                Joints.Concat(new[] { Foot });
        }

        private readonly struct Vector3Key
        {
            public Vector3Key(
                float time,
                Vector3 value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public Vector3 Value { get; }
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

            public RendererEnabledSnapshot(Renderer renderer)
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
            public float HipsVerticalTravel { get; set; }
            public float UpperBodyVerticalTravel { get; set; }
            public float MaximumFootPositionError { get; set; }
            public float MaximumFootSolePositionError { get; set; }
            public float MinimumMeshYVariation { get; set; }
            public float LeftLegRotationDelta { get; set; }
            public float RightLegRotationDelta { get; set; }
            public float LoopBoundaryError { get; set; }
            public float RootPositionError { get; set; }
        }
    }
}
