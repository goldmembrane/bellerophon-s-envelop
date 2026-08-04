using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaToShieldStanceAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string TargetSlotName = "Kursa_05_ToShieldStance";
        private const string ModelName = "Kursa_Model";
        private const string EffectName = "Kursa_ShieldStanceIcon";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_05_ToShieldStance_Loop.anim";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_05_ToShieldStance.controller";
        private const string SpritePath =
            "Assets/_Project/Art/Enemies/Kursa/Effects/Kursa_ShieldStanceIcon.png";
        private const string ValidationFolder =
            "docs/validation/kursa_to_shield_stance_right_leg_further_back_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_ToShieldStance_RightLegFurtherBack_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_ToShieldStance_RightLegFurtherBack_FinalReview.png";
        private const float TransitionSeconds = 1f;
        private const float HoldSeconds = 2f;
        private const float DurationSeconds = TransitionSeconds + HoldSeconds;
        private const float FrameRate = 60f;
        private const float EffectWorldSize = 0.42f;
        private const string ShieldMoveTargetSlotName = "Kursa_07_ShieldStanceMove";
        private const string ShieldMoveClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove_InPlace.anim";
        internal const string ShieldMoveControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove.controller";
        private const string ShieldMoveValidationFolder =
            "docs/validation/kursa_shield_stance_move_2026-08-04";
        private const string ShieldMoveDiagnosticPathFormat =
            ShieldMoveValidationFolder + "/Kursa_ShieldStanceMove_Diagnostic_{0:00}.png";
        private const string ShieldMoveFinalReviewPath =
            ShieldMoveValidationFolder + "/Kursa_ShieldStanceMove_FinalReview.png";

        private static readonly HashSet<string> ShieldMoveLegBoneNames =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
                "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"
            };

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        private static readonly float[] ReviewTimes =
        {
            0f, 0.5f, 1f, 2f, DurationSeconds - 1f / FrameRate
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply To Shield Stance Animation")]
        public static void ApplyKursaToShieldStanceAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticSlot = RequireDirectChild(placement.transform, StaticSlotName);
            var targetSlot = RequireDirectChild(placement.transform, TargetSlotName);
            var staticModel = RequireModel(staticSlot);
            var previousModel = RequireModel(targetSlot);
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var targetSlotTransformBefore = LocalTransformSignature(targetSlot);

            ConfigureSpriteImporter();
            DeleteAssetIfPresent(ControllerPath);
            DeleteAssetIfPresent(ClipPath);

            GameObject replacement = null;
            try
            {
                replacement = UnityEngine.Object.Instantiate(staticModel.gameObject);
                replacement.name = ModelName;
                replacement.transform.SetParent(targetSlot, false);
                replacement.transform.SetLocalPositionAndRotation(
                    staticModel.localPosition,
                    staticModel.localRotation);
                replacement.transform.localScale = staticModel.localScale;

                foreach (var animator in replacement.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var legacy in replacement.GetComponentsInChildren<Animation>(true))
                    UnityEngine.Object.DestroyImmediate(legacy);

                var replacementRenderer = RequireRenderer(
                    replacement.transform,
                    TargetSlotName);
                RequireExactStaticAppearance(staticRenderer, replacementRenderer);
                var effectRenderer = CreateEffect(replacement.transform, replacementRenderer);
                var clip = CreateClip(replacement.transform, replacementRenderer, effectRenderer);
                var controller = CreateController(clip);
                var animatorComponent = replacement.AddComponent<Animator>();
                animatorComponent.runtimeAnimatorController = controller;
                animatorComponent.applyRootMotion = false;
                animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorComponent.updateMode = AnimatorUpdateMode.Normal;
                animatorComponent.enabled = true;
                animatorComponent.Rebind();
                animatorComponent.Update(0f);
                EditorUtility.SetDirty(animatorComponent);

                RequirePlacedContract(
                    replacement.transform,
                    staticRenderer,
                    clip,
                    controller);

                UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
                replacement = null;
                RequireEqual(
                    otherSlotsBefore,
                    OtherSlotSignatures(placement.transform),
                    "A Kursa slot outside Kursa_05_ToShieldStance changed.");
                RequireEqual(
                    otherRootsBefore,
                    OtherRootSignatures(scene, placement),
                    "A scene root outside the Kursa placement changed.");
                if (targetSlotTransformBefore != LocalTransformSignature(targetSlot))
                    throw new InvalidOperationException(
                        "The Kursa_05_ToShieldStance slot transform changed.");
                RequireSlotContract(placement.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying Kursa_05_ToShieldStance.");
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "KursaToShieldStanceAnimationApplied Result=PASS, " +
                    "Slot=Kursa_05_ToShieldStance, StaticAppearanceCopied=True, " +
                    "TransitionSeconds=1, HoldSeconds=2, DurationSeconds=3, " +
                    "LeftKneeBent=True, RightFootBack=True, LeftShieldArmForward=True, " +
                    "ApprovedShieldIcon=True, Loop=True, RootMotion=False, " +
                    "Kursa_06_Unchanged=True, OtherSlotsUnchanged=True, " +
                    "OtherSceneRootsUnchanged=True, SceneSaved=True.");
            }
            catch
            {
                if (replacement != null)
                    UnityEngine.Object.DestroyImmediate(replacement);
                DeleteAssetIfPresent(ControllerPath);
                DeleteAssetIfPresent(ClipPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture To Shield Stance Diagnostic")]
        public static void CaptureKursaToShieldStanceDiagnostic()
        {
            CaptureReview(NextDiagnosticPath(), "Diagnostic");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture To Shield Stance Final Review")]
        public static void CaptureKursaToShieldStanceFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa shield-stance final review already exists: " +
                    FinalReviewPath);
            CaptureReview(destination, "FinalReview");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Shield Stance Move Animation")]
        public static void ApplyKursaShieldStanceMoveAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var stanceModel = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            var moveModel = RequireModel(RequireDirectChild(
                placement.transform,
                "Kursa_03_Move"));
            var targetSlot = RequireDirectChild(
                placement.transform,
                ShieldMoveTargetSlotName);
            var previousModel = RequireModel(targetSlot);
            var stanceRenderer = RequireRenderer(stanceModel, TargetSlotName);
            var otherSlotsBefore = SlotSignaturesExcept(
                placement.transform,
                ShieldMoveTargetSlotName);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var targetSlotTransformBefore = LocalTransformSignature(targetSlot);
            var stanceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "Kursa to-shield-stance clip is missing.");
            var moveClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                KursaMoveAnimationTool.ClipPath) ??
                throw new InvalidOperationException("Kursa move clip is missing.");

            DeleteAssetIfPresent(ShieldMoveControllerPath);
            DeleteAssetIfPresent(ShieldMoveClipPath);

            GameObject replacement = null;
            try
            {
                replacement = UnityEngine.Object.Instantiate(stanceModel.gameObject);
                replacement.name = ModelName;
                replacement.transform.SetParent(targetSlot, false);
                replacement.transform.SetLocalPositionAndRotation(
                    stanceModel.localPosition,
                    stanceModel.localRotation);
                replacement.transform.localScale = stanceModel.localScale;

                foreach (var animator in replacement.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var legacy in replacement.GetComponentsInChildren<Animation>(true))
                    UnityEngine.Object.DestroyImmediate(legacy);

                var replacementRenderer = RequireRenderer(
                    replacement.transform,
                    ShieldMoveTargetSlotName);
                RequireMatchingAppearance(
                    stanceRenderer,
                    replacementRenderer,
                    ShieldMoveTargetSlotName);
                var effectRenderer = replacement
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .SingleOrDefault(item => item.name == EffectName) ??
                    throw new InvalidOperationException(
                        "Kursa_07_ShieldStanceMove is missing the approved shield icon.");
                var clip = CreateShieldMoveClip(
                    replacement.transform,
                    replacementRenderer,
                    moveModel,
                    stanceClip,
                    moveClip);
                var controller = CreateShieldMoveController(clip);
                var animatorComponent = replacement.AddComponent<Animator>();
                animatorComponent.runtimeAnimatorController = controller;
                animatorComponent.applyRootMotion = false;
                animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorComponent.updateMode = AnimatorUpdateMode.Normal;
                animatorComponent.enabled = true;
                animatorComponent.Rebind();
                animatorComponent.Update(0f);
                var effectColor = effectRenderer.color;
                effectColor.a = 1f;
                effectRenderer.color = effectColor;
                EditorUtility.SetDirty(effectRenderer);
                EditorUtility.SetDirty(animatorComponent);

                RequireShieldMovePlacedContract(
                    replacement.transform,
                    stanceRenderer,
                    clip,
                    moveClip,
                    controller);

                UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
                replacement = null;
                RequireEqual(
                    otherSlotsBefore,
                    SlotSignaturesExcept(placement.transform, ShieldMoveTargetSlotName),
                    "A Kursa slot outside Kursa_07_ShieldStanceMove changed.");
                RequireEqual(
                    otherRootsBefore,
                    OtherRootSignatures(scene, placement),
                    "A scene root outside the Kursa placement changed.");
                if (targetSlotTransformBefore != LocalTransformSignature(targetSlot))
                    throw new InvalidOperationException(
                        "The Kursa_07_ShieldStanceMove slot transform changed.");
                RequireSlotContract(placement.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying " +
                        "Kursa_07_ShieldStanceMove.");
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "KursaShieldStanceMoveAnimationApplied Result=PASS, " +
                    "Slot=Kursa_07_ShieldStanceMove, MoveLowerBodySource=True, " +
                    "MoveSpeedPercent=100, ShieldStanceUpperBodyLocked=True, " +
                    "ShieldStanceStrideMatched=True, InPlace=True, " +
                    "ApprovedShieldIconVisible=True, Loop=True, RootMotion=False, " +
                    "Kursa_03_Unchanged=True, Kursa_05_Unchanged=True, " +
                    "Kursa_06_Unchanged=True, OtherSlotsUnchanged=True, " +
                    "OtherSceneRootsUnchanged=True, SceneSaved=True.");
            }
            catch
            {
                if (replacement != null)
                    UnityEngine.Object.DestroyImmediate(replacement);
                DeleteAssetIfPresent(ShieldMoveControllerPath);
                DeleteAssetIfPresent(ShieldMoveClipPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Stance Move Diagnostic")]
        public static void CaptureKursaShieldStanceMoveDiagnostic()
        {
            CaptureShieldMoveReview(NextShieldMoveDiagnosticPath(), "Diagnostic");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Stance Move Final Review")]
        public static void CaptureKursaShieldStanceMoveFinalReview()
        {
            var destination = Absolute(ShieldMoveFinalReviewPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa shield-stance-move final review already exists: " +
                    ShieldMoveFinalReviewPath);
            CaptureShieldMoveReview(destination, "FinalReview");
        }

        private static AnimationClip CreateShieldMoveClip(
            Transform targetModel,
            SkinnedMeshRenderer targetRenderer,
            Transform moveModel,
            AnimationClip stanceClip,
            AnimationClip moveClip)
        {
            var targetSkeleton = RequireSkeletonPaths(targetModel, targetRenderer);
            var targetSnapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var moveSampler = UnityEngine.Object.Instantiate(moveModel.gameObject);
            moveSampler.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (var animator in moveSampler.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var legacy in moveSampler.GetComponentsInChildren<Animation>(true))
                    UnityEngine.Object.DestroyImmediate(legacy);
                var moveRenderer = RequireRenderer(
                    moveSampler.transform,
                    "temporary Kursa move sampler");
                var moveSkeleton = RequireSkeletonPaths(
                    moveSampler.transform,
                    moveRenderer);
                var targetByName = targetSkeleton.Values.ToDictionary(
                    item => item.name,
                    item => item,
                    StringComparer.Ordinal);
                var moveByName = moveSkeleton.Values.ToDictionary(
                    item => item.name,
                    item => item,
                    StringComparer.Ordinal);
                foreach (var boneName in ShieldMoveLegBoneNames)
                {
                    if (!targetByName.ContainsKey(boneName) ||
                        !moveByName.ContainsKey(boneName))
                    {
                        throw new InvalidOperationException(
                            "The Kursa shield-stance move leg rig is missing " +
                            boneName + ".");
                    }
                }

                stanceClip.SampleAnimation(targetModel.gameObject, TransitionSeconds);
                var stancePoses = CapturePoses(targetSkeleton);
                var targetForward = Vector3.ProjectOnPlane(
                    targetModel.forward,
                    targetModel.up).normalized;
                var targetSignedGap = Vector3.Dot(
                    targetByName["LeftFoot"].position -
                    targetByName["RightFoot"].position,
                    targetForward);
                var targetGap = Mathf.Abs(targetSignedGap);
                var phase = FindShieldMoveGaitPhase(
                    moveSampler.transform,
                    moveClip,
                    moveByName,
                    Mathf.Sign(targetSignedGap));
                if (targetGap <= 0.0001f || phase.MaximumGap <= 0.0001f)
                    throw new InvalidOperationException(
                        "The Kursa shield-stance move stride reference is degenerate.");
                var strideScale = targetGap / phase.MaximumGap;

                moveClip.SampleAnimation(moveSampler, phase.NeutralTime);
                var neutralRotations = ShieldMoveLegBoneNames.ToDictionary(
                    item => item,
                    item => moveByName[item].localRotation,
                    StringComparer.Ordinal);
                moveClip.SampleAnimation(moveSampler, phase.StartTime);
                var alignmentByBone = new Dictionary<string, Quaternion>(
                    StringComparer.Ordinal);
                foreach (var boneName in ShieldMoveLegBoneNames)
                {
                    var scaledStart = Quaternion.SlerpUnclamped(
                        neutralRotations[boneName],
                        moveByName[boneName].localRotation,
                        strideScale);
                    alignmentByBone[boneName] =
                        targetByName[boneName].localRotation *
                        Quaternion.Inverse(scaledStart);
                }

                var keysByPath = targetSkeleton.Keys.ToDictionary(
                    item => item,
                    _ => new ShieldMoveTransformKeys(),
                    StringComparer.Ordinal);
                var frameRate = moveClip.frameRate > 0f
                    ? moveClip.frameRate
                    : FrameRate;
                var frames = Mathf.RoundToInt(moveClip.length * frameRate);
                for (var frame = 0; frame <= frames; frame++)
                {
                    var time = frame == frames
                        ? moveClip.length
                        : frame / frameRate;
                    var sourceTime = frame == frames
                        ? phase.StartTime
                        : Mathf.Repeat(phase.StartTime + time, moveClip.length);
                    moveClip.SampleAnimation(moveSampler, sourceTime);
                    foreach (var item in targetSkeleton)
                    {
                        var pose = stancePoses[item.Key];
                        var rotation = pose.Rotation;
                        if (ShieldMoveLegBoneNames.Contains(item.Value.name))
                        {
                            var boneName = item.Value.name;
                            var scaledSource = Quaternion.SlerpUnclamped(
                                neutralRotations[boneName],
                                moveByName[boneName].localRotation,
                                strideScale);
                            rotation = alignmentByBone[boneName] * scaledSource;
                        }
                        keysByPath[item.Key].Add(time, pose.Position, rotation);
                    }
                }

                var clip = new AnimationClip
                {
                    name = "Kursa_07_ShieldStanceMove_InPlace",
                    frameRate = frameRate,
                    wrapMode = WrapMode.Loop
                };
                foreach (var path in targetSkeleton.Keys.OrderBy(
                             item => item,
                             StringComparer.Ordinal))
                {
                    SetShieldMoveTransformCurves(
                        clip,
                        path,
                        keysByPath[path]);
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                settings.loopBlend = false;
                settings.keepOriginalOrientation = true;
                settings.keepOriginalPositionY = true;
                settings.keepOriginalPositionXZ = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                clip.EnsureQuaternionContinuity();
                AssetDatabase.CreateAsset(clip, ShieldMoveClipPath);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
                return clip;
            }
            finally
            {
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                UnityEngine.Object.DestroyImmediate(moveSampler);
            }
        }

        private static ShieldMoveGaitPhase FindShieldMoveGaitPhase(
            Transform moveSampler,
            AnimationClip moveClip,
            IReadOnlyDictionary<string, Transform> moveByName,
            float targetSign)
        {
            var frameRate = moveClip.frameRate > 0f
                ? moveClip.frameRate
                : FrameRate;
            var frames = Mathf.Max(1, Mathf.RoundToInt(moveClip.length * frameRate));
            var forward = Vector3.ProjectOnPlane(
                moveSampler.forward,
                moveSampler.up).normalized;
            var selectedSeparation = targetSign >= 0f
                ? float.NegativeInfinity
                : float.PositiveInfinity;
            var startTime = 0f;
            var neutralTime = 0f;
            var smallestAbsoluteSeparation = float.PositiveInfinity;
            var maximumGap = 0f;
            for (var frame = 0; frame < frames; frame++)
            {
                var time = frame / frameRate;
                moveClip.SampleAnimation(moveSampler.gameObject, time);
                var separation = Vector3.Dot(
                    moveByName["LeftFoot"].position -
                    moveByName["RightFoot"].position,
                    forward);
                var absoluteSeparation = Mathf.Abs(separation);
                maximumGap = Mathf.Max(maximumGap, absoluteSeparation);
                if (absoluteSeparation < smallestAbsoluteSeparation)
                {
                    smallestAbsoluteSeparation = absoluteSeparation;
                    neutralTime = time;
                }
                var preferred = targetSign >= 0f
                    ? separation > selectedSeparation
                    : separation < selectedSeparation;
                if (preferred)
                {
                    selectedSeparation = separation;
                    startTime = time;
                }
            }
            return new ShieldMoveGaitPhase(startTime, neutralTime, maximumGap);
        }

        private static void SetShieldMoveTransformCurves(
            AnimationClip clip,
            string path,
            ShieldMoveTransformKeys keys)
        {
            SetShieldMoveCurve(clip, path, "m_LocalPosition.x", keys.PositionX);
            SetShieldMoveCurve(clip, path, "m_LocalPosition.y", keys.PositionY);
            SetShieldMoveCurve(clip, path, "m_LocalPosition.z", keys.PositionZ);
            SetShieldMoveCurve(clip, path, "m_LocalRotation.x", keys.RotationX);
            SetShieldMoveCurve(clip, path, "m_LocalRotation.y", keys.RotationY);
            SetShieldMoveCurve(clip, path, "m_LocalRotation.z", keys.RotationZ);
            SetShieldMoveCurve(clip, path, "m_LocalRotation.w", keys.RotationW);
        }

        private static void SetShieldMoveCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
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

        private static AnimatorController CreateShieldMoveController(
            AnimationClip clip)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ShieldMoveControllerPath);
            var state = controller.layers[0].stateMachine.AddState(
                "KursaShieldStanceMoveInPlace");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RequireShieldMovePlacedContract(
            Transform model,
            SkinnedMeshRenderer stanceRenderer,
            AnimationClip clip,
            AnimationClip moveClip,
            AnimatorController controller)
        {
            var renderer = RequireRenderer(model, ShieldMoveTargetSlotName);
            RequireMatchingAppearance(
                stanceRenderer,
                renderer,
                ShieldMoveTargetSlotName);
            var animator = model.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Kursa_07_ShieldStanceMove must contain one Animator.");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate ||
                animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(
                    "Kursa_07_ShieldStanceMove Animator configuration differs.");
            }
            var effect = model.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(item => item.name == EffectName) ??
                throw new InvalidOperationException(
                    "Kursa_07_ShieldStanceMove is missing the approved shield icon.");
            if (effect.sprite == null ||
                AssetDatabase.GetAssetPath(effect.sprite) != SpritePath ||
                effect.color.a < 0.99f)
            {
                throw new InvalidOperationException(
                    "Kursa_07_ShieldStanceMove shield icon differs.");
            }
            if (Mathf.Abs(clip.length - moveClip.length) > 0.001f ||
                Mathf.Abs(clip.frameRate - moveClip.frameRate) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Kursa_07_ShieldStanceMove speed differs from Kursa_03_Move.");
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || settings.loopBlend)
                throw new InvalidOperationException(
                    "Kursa_07_ShieldStanceMove must loop without return blending.");
        }

        private static void CaptureShieldMoveReview(
            string destination,
            string kind)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var stanceModel = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            var moveModel = RequireModel(RequireDirectChild(
                placement.transform,
                "Kursa_03_Move"));
            var targetModel = RequireModel(RequireDirectChild(
                placement.transform,
                ShieldMoveTargetSlotName));
            var stanceRenderer = RequireRenderer(stanceModel, TargetSlotName);
            var targetRenderer = RequireRenderer(
                targetModel,
                ShieldMoveTargetSlotName);
            var stanceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException(
                    "Kursa to-shield-stance clip is missing.");
            var moveClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                KursaMoveAnimationTool.ClipPath) ??
                throw new InvalidOperationException("Kursa move clip is missing.");
            var targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                ShieldMoveClipPath) ?? throw new InvalidOperationException(
                "Kursa shield-stance move clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ShieldMoveControllerPath) ?? throw new InvalidOperationException(
                "Kursa shield-stance move controller is missing.");
            RequireShieldMovePlacedContract(
                targetModel,
                stanceRenderer,
                targetClip,
                moveClip,
                controller);
            CaptureShieldMoveContactSheet(
                scene,
                stanceModel,
                moveModel,
                targetModel,
                stanceClip,
                moveClip,
                targetClip,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa shield-stance move capture changed the scene dirty state.");
            Debug.Log(
                "KursaShieldStanceMoveReviewCaptured Kind=" + kind +
                ", DirectVisualReviewRequired=True, MoveSpeedComparison=True, " +
                "ShieldStanceUpperAndStrideComparison=True, FullLoop=True, Image=" +
                destination + ", SceneChanged=False.");
        }

        private static void CaptureShieldMoveContactSheet(
            Scene scene,
            Transform stanceModel,
            Transform moveModel,
            Transform targetModel,
            AnimationClip stanceClip,
            AnimationClip moveClip,
            AnimationClip targetClip,
            string destination)
        {
            const int panelWidth = 320;
            const int panelHeight = 320;
            const int columns = 6;
            const int rows = 4;
            var reviewTimes = new[]
            {
                0f,
                targetClip.length * 0.25f,
                targetClip.length * 0.5f,
                targetClip.length * 0.75f,
                targetClip.length - 1f / targetClip.frameRate
            };
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererSnapshot(item))
                .ToArray();
            var stanceSnapshots = stanceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var moveSnapshots = moveModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var targetSnapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var stanceAnimator = stanceModel.GetComponentsInChildren<Animator>(true).Single();
            var moveAnimator = moveModel.GetComponentsInChildren<Animator>(true).Single();
            var targetAnimator = targetModel.GetComponentsInChildren<Animator>(true).Single();
            var stanceAnimatorEnabled = stanceAnimator.enabled;
            var moveAnimatorEnabled = moveAnimator.enabled;
            var targetAnimatorEnabled = targetAnimator.enabled;
            var stanceEffect = stanceModel.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(item => item.name == EffectName);
            var targetEffect = targetModel.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(item => item.name == EffectName);
            var stanceEffectColor = stanceEffect.color;
            var targetEffectColor = targetEffect.color;
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing.");
            var cameraObject = new GameObject(
                "KursaShieldStanceMoveReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var targetTexture = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var sheet = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                stanceAnimator.enabled = false;
                moveAnimator.enabled = false;
                targetAnimator.enabled = false;
                var moveFullBounds = SampledShieldMoveBounds(
                    moveModel,
                    moveClip,
                    reviewTimes,
                    moveSnapshots,
                    BoundsOf);
                var targetFullBounds = SampledShieldMoveBounds(
                    targetModel,
                    targetClip,
                    reviewTimes,
                    targetSnapshots,
                    BoundsOf);
                var sharedFullSize = Vector3.Max(
                    moveFullBounds.size,
                    targetFullBounds.size);
                moveFullBounds.size = sharedFullSize;
                targetFullBounds.size = sharedFullSize;

                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                stanceClip.SampleAnimation(stanceModel.gameObject, TransitionSeconds);
                var stanceUpperBounds = CurrentUpperBodyBounds(stanceModel);
                var stanceLegBounds = CurrentLegBounds(stanceModel);
                var targetUpperBounds = SampledShieldMoveBounds(
                    targetModel,
                    targetClip,
                    reviewTimes,
                    targetSnapshots,
                    CurrentUpperBodyBounds);
                var targetLegBounds = SampledShieldMoveBounds(
                    targetModel,
                    targetClip,
                    reviewTimes,
                    targetSnapshots,
                    CurrentLegBounds);
                var sharedUpperSize = Vector3.Max(
                    stanceUpperBounds.size,
                    targetUpperBounds.size);
                stanceUpperBounds.size = sharedUpperSize;
                targetUpperBounds.size = sharedUpperSize;
                var sharedLegSize = Vector3.Max(
                    stanceLegBounds.size,
                    targetLegBounds.size);
                stanceLegBounds.size = sharedLegSize;
                targetLegBounds.size = sharedLegSize;

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 30f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = targetTexture;

                RenderShieldMoveTimedRow(
                    camera,
                    moveModel,
                    moveClip,
                    reviewTimes,
                    moveSnapshots,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    sheet,
                    moveFullBounds,
                    35f,
                    0,
                    rows,
                    panelWidth,
                    panelHeight);
                RenderShieldMoveTimedRow(
                    camera,
                    targetModel,
                    targetClip,
                    reviewTimes,
                    targetSnapshots,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    sheet,
                    targetFullBounds,
                    35f,
                    1,
                    rows,
                    panelWidth,
                    panelHeight);

                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                stanceClip.SampleAnimation(stanceModel.gameObject, TransitionSeconds);
                RenderPanel(
                    camera,
                    stanceModel,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    stanceUpperBounds,
                    90f);
                CopyPanel(panel, sheet, 0, rows - 1 - 2, panelWidth, panelHeight);
                RenderShieldMoveTimedCells(
                    camera,
                    targetModel,
                    targetClip,
                    reviewTimes,
                    targetSnapshots,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    sheet,
                    targetUpperBounds,
                    90f,
                    2,
                    rows,
                    panelWidth,
                    panelHeight);

                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                stanceClip.SampleAnimation(stanceModel.gameObject, TransitionSeconds);
                RenderPanel(
                    camera,
                    stanceModel,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    stanceLegBounds,
                    90f);
                CopyPanel(panel, sheet, 0, rows - 1 - 3, panelWidth, panelHeight);
                RenderShieldMoveTimedCells(
                    camera,
                    targetModel,
                    targetClip,
                    reviewTimes,
                    targetSnapshots,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    sheet,
                    targetLegBounds,
                    90f,
                    3,
                    rows,
                    panelWidth,
                    panelHeight);

                sheet.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa shield-stance move review folder."));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                foreach (var snapshot in moveSnapshots) snapshot.Restore();
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                stanceEffect.color = stanceEffectColor;
                targetEffect.color = targetEffectColor;
                stanceAnimator.enabled = stanceAnimatorEnabled;
                moveAnimator.enabled = moveAnimatorEnabled;
                targetAnimator.enabled = targetAnimatorEnabled;
                foreach (var snapshot in rendererStates) snapshot.Restore();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds SampledShieldMoveBounds(
            Transform model,
            AnimationClip clip,
            IEnumerable<float> times,
            IReadOnlyList<TransformSnapshot> snapshots,
            Func<Transform, Bounds> selector)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var time in times)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, time);
                var current = selector(model);
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(current);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static void RenderShieldMoveTimedRow(
            Camera camera,
            Transform model,
            AnimationClip clip,
            IReadOnlyList<float> times,
            IReadOnlyList<TransformSnapshot> snapshots,
            Renderer[] sceneRenderers,
            RenderTexture targetTexture,
            Texture2D panel,
            Texture2D sheet,
            Bounds bounds,
            float yaw,
            int row,
            int rows,
            int panelWidth,
            int panelHeight)
        {
            foreach (var snapshot in snapshots) snapshot.Restore();
            clip.SampleAnimation(model.gameObject, 0f);
            RenderPanel(
                camera,
                model,
                sceneRenderers,
                targetTexture,
                panel,
                bounds,
                yaw);
            CopyPanel(panel, sheet, 0, rows - 1 - row, panelWidth, panelHeight);
            RenderShieldMoveTimedCells(
                camera,
                model,
                clip,
                times,
                snapshots,
                sceneRenderers,
                targetTexture,
                panel,
                sheet,
                bounds,
                yaw,
                row,
                rows,
                panelWidth,
                panelHeight);
        }

        private static void RenderShieldMoveTimedCells(
            Camera camera,
            Transform model,
            AnimationClip clip,
            IReadOnlyList<float> times,
            IReadOnlyList<TransformSnapshot> snapshots,
            Renderer[] sceneRenderers,
            RenderTexture targetTexture,
            Texture2D panel,
            Texture2D sheet,
            Bounds bounds,
            float yaw,
            int row,
            int rows,
            int panelWidth,
            int panelHeight)
        {
            for (var index = 0; index < times.Count; index++)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, times[index]);
                RenderPanel(
                    camera,
                    model,
                    sceneRenderers,
                    targetTexture,
                    panel,
                    bounds,
                    yaw);
                CopyPanel(
                    panel,
                    sheet,
                    index + 1,
                    rows - 1 - row,
                    panelWidth,
                    panelHeight);
            }
        }

        private static string NextShieldMoveDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var candidate = Absolute(string.Format(
                    ShieldMoveDiagnosticPathFormat,
                    index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The two approved Kursa shield-stance move diagnostics already exist.");
        }

        private static void RequireMatchingAppearance(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual,
            string context)
        {
            if (expected.sharedMesh != actual.sharedMesh ||
                expected.bones.Length != actual.bones.Length ||
                expected.sharedMaterials.Length != actual.sharedMaterials.Length ||
                !expected.sharedMaterials.SequenceEqual(actual.sharedMaterials))
            {
                throw new InvalidOperationException(
                    context + " does not use the exact shield-stance Kursa appearance.");
            }
        }

        private static string[] SlotSignaturesExcept(
            Transform placement,
            string excludedSlotName) =>
            SlotNames.Where(item => item != excludedSlotName)
                .Select(item => RecursiveSignature(RequireDirectChild(placement, item)))
                .ToArray();

        private static void ConfigureSpriteImporter()
        {
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter ??
                throw new InvalidOperationException(
                    "The approved Kursa shield icon PNG importer is unavailable.");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static SpriteRenderer CreateEffect(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) ??
                throw new InvalidOperationException(
                    "The approved Kursa shield icon sprite is missing.");
            var effectObject = new GameObject(EffectName);
            effectObject.transform.SetParent(model, false);
            var worldPosition = renderer.bounds.center +
                model.up * (renderer.bounds.extents.y + 0.18f);
            effectObject.transform.localPosition = model.InverseTransformPoint(worldPosition);
            effectObject.transform.localRotation = Quaternion.identity;
            var worldScale = model.lossyScale;
            effectObject.transform.localScale = new Vector3(
                EffectWorldSize / Mathf.Abs(worldScale.x),
                EffectWorldSize / Mathf.Abs(worldScale.y),
                EffectWorldSize / Mathf.Abs(worldScale.z));
            var spriteRenderer = effectObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            spriteRenderer.sortingOrder = 100;
            EditorUtility.SetDirty(spriteRenderer);
            return spriteRenderer;
        }

        private static AnimationClip CreateClip(
            Transform model,
            SkinnedMeshRenderer renderer,
            SpriteRenderer effectRenderer)
        {
            var skeleton = RequireSkeletonPaths(model, renderer);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var start = CapturePoses(skeleton);
            Dictionary<string, LocalPose> target;
            try
            {
                AuthorTargetPose(model, skeleton);
                target = CapturePoses(skeleton);
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
            }

            var clip = new AnimationClip
            {
                name = "Kursa_05_ToShieldStance_Loop",
                frameRate = FrameRate,
                wrapMode = WrapMode.Loop
            };
            foreach (var path in skeleton.Keys.OrderBy(item => item, StringComparer.Ordinal))
            {
                var startPose = start[path];
                var targetPose = target[path];
                var targetRotation = targetPose.Rotation;
                if (Quaternion.Dot(startPose.Rotation, targetRotation) < 0f)
                    targetRotation = Negate(targetRotation);
                SetPositionCurves(clip, path, startPose.Position, targetPose.Position);
                SetQuaternionCurves(clip, path, startPose.Rotation, targetRotation);
            }

            var effectPath = AnimationUtility.CalculateTransformPath(
                effectRenderer.transform,
                model);
            var revealTime = TransitionSeconds - 1f / FrameRate;
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    effectPath,
                    typeof(SpriteRenderer),
                    "m_Color.a"),
                LinearCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(revealTime, 0f),
                    new Keyframe(TransitionSeconds, 1f),
                    new Keyframe(DurationSeconds, 1f)));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void AuthorTargetPose(
            Transform model,
            IReadOnlyDictionary<string, Transform> skeleton)
        {
            var forward = Vector3.ProjectOnPlane(model.forward, model.up).normalized;
            if (forward.sqrMagnitude < 0.9f)
                throw new InvalidOperationException("The Kursa model forward axis is unavailable.");
            var right = Vector3.Cross(model.up, forward).normalized;

            var hips = RequireBone(skeleton, "Hips");
            var leftUpLeg = RequireBone(skeleton, "LeftUpLeg");
            var leftLeg = RequireBone(skeleton, "LeftLeg");
            var leftFoot = RequireBone(skeleton, "LeftFoot");
            var rightUpLeg = RequireBone(skeleton, "RightUpLeg");
            var rightLeg = RequireBone(skeleton, "RightLeg");
            var rightFoot = RequireBone(skeleton, "RightFoot");
            var rightToe = RequireBone(skeleton, "RightToeBase");
            var leftShoulder = RequireBone(skeleton, "LeftShoulder");
            var leftArm = RequireBone(skeleton, "LeftArm");
            var leftForeArm = RequireBone(skeleton, "LeftForeArm");
            var leftHand = RequireBone(skeleton, "LeftHand");

            var leftFootWorldRotation = leftFoot.rotation;
            var rightFootWorldRotation = rightFoot.rotation;
            var leftHandWorldRotation = leftHand.rotation;
            var leftFootStart = leftFoot.position;
            var rightFootStart = rightFoot.position;
            var rightToeStart = rightToe.position;
            var leftKneeStart = leftLeg.position;
            var rightKneeStart = rightLeg.position;
            var leftLegLength = Vector3.Distance(leftUpLeg.position, leftLeg.position) +
                Vector3.Distance(leftLeg.position, leftFoot.position);
            var rightLegLength = Vector3.Distance(rightUpLeg.position, rightLeg.position) +
                Vector3.Distance(rightLeg.position, rightFoot.position);

            // The stride and crouch are proportional to this rig's leg length so the
            // authored stance stays modest while both feet remain planted on the floor.
            var leftFootTarget = leftFootStart + forward * (leftLegLength * 0.24f);
            var rightToeTarget = rightToeStart - forward * (rightLegLength * 0.82f);
            var tiptoeRotation = SelectTiptoeRotation(
                rightFootWorldRotation,
                rightToeStart - rightFootStart,
                right,
                model.up,
                38f);
            var tiptoeOffset = tiptoeRotation *
                Quaternion.Inverse(rightFootWorldRotation) *
                (rightToeStart - rightFootStart);
            var rightFootTarget = rightToeTarget - tiptoeOffset;
            hips.position -= model.up * (Mathf.Min(leftLegLength, rightLegLength) * 0.10f);
            var leftKneePole = leftKneeStart +
                forward * (leftLegLength * 0.40f) -
                model.up * (leftLegLength * 0.10f);
            var rightKneePole = rightKneeStart +
                forward * (rightLegLength * 0.02f) -
                model.up * (rightLegLength * 0.36f);
            SolveTwoBoneChain(
                leftUpLeg,
                leftLeg,
                leftFoot,
                leftFootTarget,
                leftKneePole,
                leftFootWorldRotation);
            SolveTwoBoneChain(
                rightUpLeg,
                rightLeg,
                rightFoot,
                rightFootTarget,
                rightKneePole,
                tiptoeRotation);

            RotateForDisplacement(leftShoulder, leftHand, right, forward, 30f);
            var shieldArmLength = Vector3.Distance(leftArm.position, leftForeArm.position) +
                Vector3.Distance(leftForeArm.position, leftHand.position);
            var shieldArmDirection =
                (forward * 0.99f - model.up * 0.12f).normalized;
            var leftHandTarget = leftArm.position +
                shieldArmDirection * (shieldArmLength * 0.98f);
            var leftElbowPole = leftForeArm.position -
                right * (shieldArmLength * 0.42f) +
                forward * (shieldArmLength * 0.18f);
            SolveTwoBoneChain(
                leftArm,
                leftForeArm,
                leftHand,
                leftHandTarget,
                leftElbowPole,
                leftHandWorldRotation);
        }

        private static Quaternion SelectTiptoeRotation(
            Quaternion restRotation,
            Vector3 restToeOffset,
            Vector3 worldAxis,
            Vector3 worldUp,
            float angleDegrees)
        {
            var positive = Quaternion.AngleAxis(angleDegrees, worldAxis) * restRotation;
            var negative = Quaternion.AngleAxis(-angleDegrees, worldAxis) * restRotation;
            var positiveOffset = positive * Quaternion.Inverse(restRotation) * restToeOffset;
            var negativeOffset = negative * Quaternion.Inverse(restRotation) * restToeOffset;
            return Vector3.Dot(positiveOffset - restToeOffset, -worldUp) >=
                   Vector3.Dot(negativeOffset - restToeOffset, -worldUp)
                ? positive
                : negative;
        }

        private static void SolveTwoBoneChain(
            Transform upper,
            Transform lower,
            Transform tip,
            Vector3 tipTarget,
            Vector3 pole,
            Quaternion tipRotation)
        {
            var rootPosition = upper.position;
            var upperLength = Vector3.Distance(upper.position, lower.position);
            var lowerLength = Vector3.Distance(lower.position, tip.position);
            var rootToTarget = tipTarget - rootPosition;
            var targetDistance = Mathf.Clamp(
                rootToTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            var targetDirection = rootToTarget.normalized;
            var poleDirection = Vector3.ProjectOnPlane(
                pole - rootPosition,
                targetDirection).normalized;
            if (poleDirection.sqrMagnitude < 0.5f)
                throw new InvalidOperationException(
                    "The Kursa shield-stance two-bone pole is degenerate.");

            var along =
                (upperLength * upperLength + targetDistance * targetDistance -
                 lowerLength * lowerLength) /
                (2f * targetDistance);
            var away = Mathf.Sqrt(Mathf.Max(
                0f,
                upperLength * upperLength - along * along));
            var desiredJoint = rootPosition +
                targetDirection * along +
                poleDirection * away;

            upper.rotation = Quaternion.FromToRotation(
                lower.position - upper.position,
                desiredJoint - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                tip.position - lower.position,
                tipTarget - lower.position) * lower.rotation;
            tip.rotation = tipRotation;
        }

        private static void RotateForDisplacement(
            Transform joint,
            Transform effector,
            Vector3 worldAxis,
            Vector3 desiredDirection,
            float angleDegrees)
        {
            var originalRotation = joint.rotation;
            var originalPosition = effector.position;
            joint.rotation = Quaternion.AngleAxis(angleDegrees, worldAxis) * originalRotation;
            var positiveScore = Vector3.Dot(
                effector.position - originalPosition,
                desiredDirection);
            joint.rotation = Quaternion.AngleAxis(-angleDegrees, worldAxis) * originalRotation;
            var negativeScore = Vector3.Dot(
                effector.position - originalPosition,
                desiredDirection);
            joint.rotation = Quaternion.AngleAxis(
                positiveScore >= negativeScore ? angleDegrees : -angleDegrees,
                worldAxis) * originalRotation;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(
                "KursaToShieldStanceLoop");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RequirePlacedContract(
            Transform model,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip clip,
            AnimatorController controller)
        {
            var renderer = RequireRenderer(model, TargetSlotName);
            RequireExactStaticAppearance(staticRenderer, renderer);
            var animator = model.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Kursa_05_ToShieldStance must contain one Animator.");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate ||
                animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(
                    "Kursa_05_ToShieldStance Animator configuration differs.");
            }
            var effect = model.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(item => item.name == EffectName) ??
                throw new InvalidOperationException("The approved shield icon is missing.");
            if (effect.sprite == null || AssetDatabase.GetAssetPath(effect.sprite) != SpritePath)
                throw new InvalidOperationException("The approved shield icon sprite differs.");
            if (Mathf.Abs(clip.length - DurationSeconds) > 0.001f)
                throw new InvalidOperationException("The shield-stance loop duration differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || settings.loopBlend)
                throw new InvalidOperationException(
                    "The shield-stance clip must loop without return blending.");
        }

        private static void CaptureReview(string destination, string kind)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireDirectChild(
                placement.transform,
                StaticSlotName));
            var targetModel = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var targetRenderer = RequireRenderer(targetModel, TargetSlotName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Kursa shield-stance clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Kursa shield-stance controller is missing.");
            RequirePlacedContract(targetModel, staticRenderer, clip, controller);
            CaptureContactSheet(
                scene,
                staticModel,
                staticRenderer,
                targetModel,
                targetRenderer,
                clip,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa shield-stance capture changed the scene dirty state.");
            Debug.Log(
                "KursaToShieldStanceReviewCaptured Kind=" + kind +
                ", DirectVisualReviewRequired=True, FrontAndOblique=True, " +
                "FullTransitionAndHold=True, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            Transform targetModel,
            SkinnedMeshRenderer targetRenderer,
            AnimationClip clip,
            string destination)
        {
            const int panelWidth = 320;
            const int panelHeight = 400;
            const int columns = 6;
            const int rows = 3;
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererSnapshot(item))
                .ToArray();
            var targetSnapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animator = targetModel.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing.");
            var cameraObject = new GameObject(
                "KursaToShieldStanceReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var targetTexture = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var sheet = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                animator.enabled = false;
                var targetBodyBounds = FullLoopRendererBounds(
                    targetModel,
                    targetRenderer,
                    clip,
                    targetSnapshots);
                var effectRenderer = targetModel
                    .GetComponentsInChildren<SpriteRenderer>(true)
                    .Single(item => item.name == EffectName);
                var targetFullBounds = targetBodyBounds;
                targetFullBounds.Encapsulate(effectRenderer.bounds);
                var offset = targetFullBounds.center - targetBodyBounds.center;
                var staticBounds = new Bounds(
                    staticRenderer.bounds.center + offset,
                    targetFullBounds.size);
                var targetLegBounds = FullLoopLegBounds(
                    targetModel,
                    clip,
                    targetSnapshots);
                var staticLegBounds = CurrentLegBounds(staticModel);
                var sharedLegSize = Vector3.Max(
                    targetLegBounds.size,
                    staticLegBounds.size);
                targetLegBounds.size = sharedLegSize;
                staticLegBounds.size = sharedLegSize;
                var targetUpperBounds = FullLoopUpperBodyBounds(
                    targetModel,
                    clip,
                    targetSnapshots);
                var staticUpperBounds = CurrentUpperBodyBounds(staticModel);
                var sharedUpperSize = Vector3.Max(
                    targetUpperBounds.size,
                    staticUpperBounds.size);
                targetUpperBounds.size = sharedUpperSize;
                staticUpperBounds.size = sharedUpperSize;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 30f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = targetTexture;

                for (var row = 0; row < rows; row++)
                {
                    var staticReviewBounds = row == 0
                        ? staticBounds
                        : row == 1 ? staticUpperBounds : staticLegBounds;
                    var targetReviewBounds = row == 0
                        ? targetFullBounds
                        : row == 1 ? targetUpperBounds : targetLegBounds;
                    var yaw = row == 0 ? 35f : 90f;
                    RenderPanel(
                        camera,
                        staticModel,
                        sceneRenderers,
                        targetTexture,
                        panel,
                        staticReviewBounds,
                        yaw);
                    CopyPanel(panel, sheet, 0, rows - 1 - row, panelWidth, panelHeight);
                    for (var index = 0; index < ReviewTimes.Length; index++)
                    {
                        foreach (var snapshot in targetSnapshots) snapshot.Restore();
                        clip.SampleAnimation(targetModel.gameObject, ReviewTimes[index]);
                        RenderPanel(
                            camera,
                            targetModel,
                            sceneRenderers,
                            targetTexture,
                            panel,
                            targetReviewBounds,
                            yaw);
                        CopyPanel(
                            panel,
                            sheet,
                            index + 1,
                            rows - 1 - row,
                            panelWidth,
                            panelHeight);
                    }
                }
                sheet.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa shield-stance review folder."));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                animator.enabled = animatorEnabled;
                foreach (var snapshot in rendererStates) snapshot.Restore();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds FullLoopBounds(
            Transform model,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var time in ReviewTimes)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, time);
                var current = BoundsOf(model);
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(current);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds FullLoopRendererBounds(
            Transform model,
            Renderer renderer,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var time in ReviewTimes)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, time);
                if (!initialized)
                {
                    result = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(renderer.bounds);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds FullLoopLegBounds(
            Transform model,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var time in ReviewTimes)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, time);
                var current = CurrentLegBounds(model);
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(current);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds FullLoopUpperBodyBounds(
            Transform model,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var time in ReviewTimes)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, time);
                var current = CurrentUpperBodyBounds(model);
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(current);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds CurrentUpperBodyBounds(Transform model)
        {
            var names = new[]
            {
                "Hips", "Head", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand"
            };
            var points = names.Select(name =>
                model.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == name).position).ToArray();
            var result = new Bounds(points[0], Vector3.zero);
            foreach (var point in points.Skip(1)) result.Encapsulate(point);
            result.Expand(new Vector3(0.68f, 0.26f, 0.68f));
            return result;
        }

        private static Bounds CurrentLegBounds(Transform model)
        {
            var names = new[]
            {
                "Hips", "LeftUpLeg", "LeftLeg", "LeftFoot",
                "LeftToeBase", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"
            };
            var points = names.Select(name =>
                model.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == name).position).ToArray();
            var result = new Bounds(points[0], Vector3.zero);
            foreach (var point in points.Skip(1)) result.Encapsulate(point);
            result.Expand(new Vector3(0.32f, 0.16f, 0.32f));
            return result;
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeSelf)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The Kursa review model has no renderer.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void RenderPanel(
            Camera camera,
            Transform model,
            IEnumerable<Renderer> sceneRenderers,
            RenderTexture target,
            Texture2D panel,
            Bounds bounds,
            float yaw)
        {
            foreach (var renderer in sceneRenderers)
                renderer.enabled = renderer.transform.IsChildOf(model);
            FrameCamera(camera, model, bounds, target.width / (float)target.height, yaw);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            panel.Apply();
        }

        private static void FrameCamera(
            Camera camera,
            Transform model,
            Bounds bounds,
            float aspect,
            float yaw)
        {
            var direction = Quaternion.AngleAxis(yaw, model.up) * model.forward.normalized;
            var vertical = bounds.extents.y /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.16f;
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void CopyPanel(
            Texture2D panel,
            Texture2D sheet,
            int column,
            int row,
            int width,
            int height)
        {
            sheet.SetPixels(
                column * width,
                row * height,
                width,
                height,
                panel.GetPixels());
        }

        private static Dictionary<string, Transform> RequireSkeletonPaths(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var transforms = new HashSet<Transform>(renderer.bones);
            foreach (var bone in renderer.bones)
            {
                var current = bone.parent;
                while (current != null && current != model)
                {
                    transforms.Add(current);
                    current = current.parent;
                }
            }
            var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var item in transforms)
            {
                var path = AnimationUtility.CalculateTransformPath(item, model);
                if (string.IsNullOrEmpty(path))
                    throw new InvalidOperationException(
                        "The Kursa skeleton unexpectedly includes the model root.");
                result.Add(path, item);
            }
            return result;
        }

        private static Transform RequireBone(
            IReadOnlyDictionary<string, Transform> skeleton,
            string name)
        {
            var matches = skeleton.Values.Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "The Kursa rig does not contain exactly one " + name + " bone.");
            return matches[0];
        }

        private static Dictionary<string, LocalPose> CapturePoses(
            IReadOnlyDictionary<string, Transform> skeleton) =>
            skeleton.ToDictionary(
                item => item.Key,
                item => new LocalPose(item.Value),
                StringComparer.Ordinal);

        private static void SetPositionCurves(
            AnimationClip clip,
            string path,
            Vector3 start,
            Vector3 target)
        {
            SetTransformCurve(clip, path, "m_LocalPosition.x", start.x, target.x);
            SetTransformCurve(clip, path, "m_LocalPosition.y", start.y, target.y);
            SetTransformCurve(clip, path, "m_LocalPosition.z", start.z, target.z);
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion start,
            Quaternion target)
        {
            SetTransformCurve(clip, path, "m_LocalRotation.x", start.x, target.x);
            SetTransformCurve(clip, path, "m_LocalRotation.y", start.y, target.y);
            SetTransformCurve(clip, path, "m_LocalRotation.z", start.z, target.z);
            SetTransformCurve(clip, path, "m_LocalRotation.w", start.w, target.w);
        }

        private static void SetTransformCurve(
            AnimationClip clip,
            string path,
            string property,
            float start,
            float target)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                LinearCurve(
                    new Keyframe(0f, start),
                    new Keyframe(TransitionSeconds, target),
                    new Keyframe(DurationSeconds, target)));
        }

        private static AnimationCurve LinearCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
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
            return curve;
        }

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);

        private static void RequireExactStaticAppearance(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual)
        {
            if (expected.sharedMesh != actual.sharedMesh ||
                expected.bones.Length != actual.bones.Length ||
                expected.sharedMaterials.Length != actual.sharedMaterials.Length ||
                !expected.sharedMaterials.SequenceEqual(actual.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Kursa_05_ToShieldStance does not use the exact static Kursa appearance.");
            }
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var candidate = Absolute(string.Format(DiagnosticPathFormat, index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The two approved Kursa shield-stance diagnostics already exist.");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on Kursa_05_ToShieldStance.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item =>
                item.name == PlacementRootName) ??
            throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 ||
                    slot.GetChild(0).name != ModelName)
                {
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at index " + index + ".");
                }
            }
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(slot.name + " model contract differs.");
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(
            Transform model,
            string context) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    context + " must contain one skinned renderer.");

        private static string[] OtherSlotSignatures(Transform placement) =>
            SlotNames.Where(item => item != TargetSlotName)
                .Select(item => RecursiveSignature(RequireDirectChild(placement, item)))
                .ToArray();

        private static string[] OtherRootSignatures(Scene scene, GameObject placement) =>
            scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform))
                .ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(item.localPosition).Append('|')
                    .Append(item.localRotation).Append('|')
                    .Append(item.localScale);
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled);
                    if (renderer is SkinnedMeshRenderer skinned)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
                foreach (var animator in item.GetComponents<Animator>())
                {
                    builder.Append("|A:").Append(animator.enabled)
                        .Append(':').Append(animator.applyRootMotion)
                        .Append(':').Append(AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController));
                }
            }
            return builder.ToString();
        }

        private static string LocalTransformSignature(Transform transform) =>
            transform.localPosition + "|" + transform.localRotation + "|" +
            transform.localScale;

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException("Asset could not be replaced: " + path);
            }
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private readonly struct ShieldMoveGaitPhase
        {
            public readonly float StartTime;
            public readonly float NeutralTime;
            public readonly float MaximumGap;

            public ShieldMoveGaitPhase(
                float startTime,
                float neutralTime,
                float maximumGap)
            {
                StartTime = startTime;
                NeutralTime = neutralTime;
                MaximumGap = maximumGap;
            }
        }

        private sealed class ShieldMoveTransformKeys
        {
            public readonly List<Keyframe> PositionX = new List<Keyframe>();
            public readonly List<Keyframe> PositionY = new List<Keyframe>();
            public readonly List<Keyframe> PositionZ = new List<Keyframe>();
            public readonly List<Keyframe> RotationX = new List<Keyframe>();
            public readonly List<Keyframe> RotationY = new List<Keyframe>();
            public readonly List<Keyframe> RotationZ = new List<Keyframe>();
            public readonly List<Keyframe> RotationW = new List<Keyframe>();
            private Quaternion previousRotation;
            private bool hasPreviousRotation;

            public void Add(float time, Vector3 position, Quaternion rotation)
            {
                if (hasPreviousRotation && Quaternion.Dot(previousRotation, rotation) < 0f)
                    rotation = Negate(rotation);
                previousRotation = rotation;
                hasPreviousRotation = true;
                PositionX.Add(new Keyframe(time, position.x));
                PositionY.Add(new Keyframe(time, position.y));
                PositionZ.Add(new Keyframe(time, position.z));
                RotationX.Add(new Keyframe(time, rotation.x));
                RotationY.Add(new Keyframe(time, rotation.y));
                RotationZ.Add(new Keyframe(time, rotation.z));
                RotationW.Add(new Keyframe(time, rotation.w));
            }
        }

        private readonly struct LocalPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform value)
            {
                transform = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            public void Restore()
            {
                if (transform == null) return;
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }
        }

        private readonly struct RendererSnapshot
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
                if (Renderer != null) Renderer.enabled = enabled;
            }
        }
    }
}
