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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static class PahurToGuardianStanceAnimationTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string TargetSlotName =
            "Pahur_08_ToGuardianStance";
        private const string GuardianSlotName =
            "Pahur_06_GuardianFlamethrower";
        private const string ModelName = "Pahur_Model";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx";
        private const string GuardianClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_06_GuardianFlamethrower_InPlace.anim";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Pahur/Animations";
        private const string ControllerFolder =
            "Assets/_Project/Art/Enemies/Pahur/Controllers";
        private const string ClipPath =
            AnimationFolder + "/Pahur_08_ToGuardianStance.anim";
        private const string ControllerPath =
            ControllerFolder + "/Pahur_08_ToGuardianStance.controller";
        private const string StateName = "PahurToGuardianStanceLoop";
        private const string ValidationFolder =
            "docs/validation/pahur_to_guardian_stance_2026-08-01";
        private const string ReportPath =
            ValidationFolder + "/Pahur_08_ToGuardianStance_0_8SecondTransition_Validation.txt";
        private const string CapturePath =
            ValidationFolder + "/Pahur_08_ToGuardianStance_0_8SecondTransition_Review.png";
        private const float TransitionSeconds = 0.8f;
        private const float HoldSeconds = 1f;
        private const float DurationSeconds =
            TransitionSeconds + HoldSeconds;
        private const float FrameRate = 60f;
        private const float Tolerance = 0.001f;
        private const float RotationToleranceDegrees = 0.1f;
        private const int ExpectedTriangles = 4330;
        private const int ExpectedBones = 24;
        private const int ExpectedMaterials = 15;

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

        [MenuItem("Bellerophon/Enemies/Pahur/Apply To Guardian Stance Animation")]
        public static void ApplyPahurToGuardianStanceAnimation()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp contains pre-existing unsaved changes.");
            }

            var placement = RequirePlacementRoot();
            RequireSlotContract(placement.transform);
            var targetSlot = RequireDirectChild(
                placement.transform,
                TargetSlotName);
            var guardianSlot = RequireDirectChild(
                placement.transform,
                GuardianSlotName);
            var targetModel = RequireModel(targetSlot);
            var guardianModel = RequireModel(guardianSlot);
            var targetRenderer = RequireApprovedRenderer(targetModel);
            var guardianClip = RequireGuardianClip();
            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var protectedBefore = ProtectedRootSignatures(scene);
            var targetPlacementBefore = PlacementTransformSignature(
                targetSlot,
                targetModel);
            var appearanceBefore = AppearanceSignature(targetRenderer);

            var animators = targetModel.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    TargetSlotName + " contains multiple Animators.");
            }

            var animator = animators.SingleOrDefault();
            var animatorAdded = animator == null;
            var animatorSnapshot = animator == null
                ? default
                : new AnimatorSnapshot(animator);
            try
            {
                EnsureAssetFolder(AnimationFolder);
                EnsureAssetFolder(ControllerFolder);
                DeleteAssetIfPresent(ClipPath);
                DeleteAssetIfPresent(ControllerPath);

                var clip = CreateTransitionClip(
                    targetModel,
                    guardianModel,
                    guardianClip);
                var controller = CreateController(clip);
                animator ??= targetModel.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                foreach (var legacy in
                         targetModel.GetComponentsInChildren<Animation>(true))
                {
                    legacy.enabled = false;
                    EditorUtility.SetDirty(legacy);
                }

                animator.Rebind();
                animator.Update(0f);
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

                var metrics = InspectState(
                    placement.transform,
                    targetSlot,
                    targetModel,
                    guardianModel,
                    animator,
                    clip,
                    controller,
                    guardianClip);
                RequireSameSignatures(
                    otherSlotsBefore,
                    OtherSlotSignatures(placement.transform),
                    "A Pahur slot outside Pahur_08_ToGuardianStance changed.");
                RequireSameSignatures(
                    protectedBefore,
                    ProtectedRootSignatures(scene),
                    "A scene root outside the Pahur placement changed.");
                if (targetPlacementBefore != PlacementTransformSignature(
                        targetSlot,
                        targetModel))
                {
                    throw new InvalidOperationException(
                        "The Pahur_08 slot or model root transform changed.");
                }

                if (appearanceBefore != AppearanceSignature(targetRenderer))
                {
                    throw new InvalidOperationException(
                        "The Pahur_08 mesh or material assignment changed.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying the Pahur guardian transition.");
                }

                AssetDatabase.SaveAssets();
                Debug.Log(
                    "PahurToGuardianStanceAnimationApplied Result=PASS" +
                    ", Slot=" + TargetSlotName +
                    ", TransitionSeconds=" + Num(TransitionSeconds) +
                    ", HoldSeconds=" + Num(HoldSeconds) +
                    ", DurationSeconds=" + Num(DurationSeconds) +
                    ", TargetGuardianClipTime=0" +
                    ", AnimatedSkeletonPaths=" + metrics.SkeletonPathCount +
                    ", Loop=True" +
                    ", ReturnSegment=False" +
                    ", RootMotion=False" +
                    ", OtherSlotsUnchanged=True" +
                    ", OtherSceneRootsUnchanged=True" +
                    ", SceneSaved=True.");
            }
            catch
            {
                if (animatorAdded && animator != null)
                {
                    UnityEngine.Object.DestroyImmediate(animator);
                }
                else if (animator != null)
                {
                    animatorSnapshot.Restore(animator);
                }

                DeleteAssetIfPresent(ControllerPath);
                DeleteAssetIfPresent(ClipPath);
                AssetDatabase.SaveAssets();
                if (scene.isDirty)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Validate To Guardian Stance Animation")]
        public static void ValidatePahurToGuardianStanceAnimation()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before Pahur guardian transition validation.");
            }

            var placement = RequirePlacementRoot();
            RequireSlotContract(placement.transform);
            var targetSlot = RequireDirectChild(
                placement.transform,
                TargetSlotName);
            var targetModel = RequireModel(targetSlot);
            var guardianModel = RequireModel(
                RequireDirectChild(placement.transform, GuardianSlotName));
            var animator = RequireAnimator(targetModel);
            var clip = RequireTransitionClip();
            var controller = RequireController();
            var guardianClip = RequireGuardianClip();
            var protectedBefore = ProtectedRootSignatures(scene);

            var metrics = InspectState(
                placement.transform,
                targetSlot,
                targetModel,
                guardianModel,
                animator,
                clip,
                controller,
                guardianClip);
            WriteValidationReport(metrics);
            RequireSameSignatures(
                protectedBefore,
                ProtectedRootSignatures(scene),
                "A scene root outside the Pahur placement changed during validation.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pahur guardian transition validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurToGuardianStanceAnimationValidated Result=PASS" +
                ", Slot=" + TargetSlotName +
                ", DurationSeconds=" + Num(DurationSeconds) +
                ", FinalGuardianPositionError=" +
                Num(metrics.FinalPositionError) +
                ", FinalGuardianRotationErrorDegrees=" +
                Num(metrics.FinalRotationErrorDegrees) +
                ", HoldPositionChange=" +
                Num(metrics.HoldPositionChange) +
                ", HoldRotationChangeDegrees=" +
                Num(metrics.HoldRotationChangeDegrees) +
                ", SceneChanged=False" +
                ", Report=" + ReportPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture To Guardian Stance Review")]
        public static void CapturePahurToGuardianStanceReview()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before the final Pahur guardian transition capture.");
            }

            var placement = RequirePlacementRoot();
            RequireSlotContract(placement.transform);
            var targetSlot = RequireDirectChild(
                placement.transform,
                TargetSlotName);
            var targetModel = RequireModel(targetSlot);
            var guardianModel = RequireModel(
                RequireDirectChild(placement.transform, GuardianSlotName));
            InspectState(
                placement.transform,
                targetSlot,
                targetModel,
                guardianModel,
                RequireAnimator(targetModel),
                RequireTransitionClip(),
                RequireController(),
                RequireGuardianClip());

            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Pahur guardian transition review already exists: " +
                    CapturePath);
            }

            CapturePoseStrip(
                targetModel,
                RequireTransitionClip(),
                destination);
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pahur guardian transition capture changed the scene dirty state.");
            }

            Debug.Log(
                "PahurToGuardianStanceReviewCaptured Result=PASS" +
                ", Slot=" + TargetSlotName +
                ", Times=0,0.4,0.8,1.3,1.799" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static AnimationClip CreateTransitionClip(
            Transform targetModel,
            Transform guardianModel,
            AnimationClip guardianClip)
        {
            var startPaths = RequireSkeletonPaths(targetModel);
            GameObject guardianClone = null;
            try
            {
                guardianClone = UnityEngine.Object.Instantiate(
                    guardianModel.gameObject);
                guardianClone.hideFlags = HideFlags.HideAndDontSave;
                DisableAnimationComponents(guardianClone.transform);
                guardianClip.SampleAnimation(guardianClone, 0f);
                var targetPaths = RequireSkeletonPaths(guardianClone.transform);
                RequireSamePathSet(startPaths.Keys, targetPaths.Keys);

                var clip = new AnimationClip
                {
                    name = "Pahur_08_ToGuardianStance",
                    frameRate = FrameRate,
                    wrapMode = WrapMode.Loop
                };
                foreach (var path in startPaths.Keys
                             .OrderBy(item => item, StringComparer.Ordinal))
                {
                    var start = TransformPose.From(startPaths[path]);
                    var target = TransformPose.From(targetPaths[path]);
                    var targetRotation = target.LocalRotation;
                    if (Quaternion.Dot(start.LocalRotation, targetRotation) < 0f)
                    {
                        targetRotation = Negate(targetRotation);
                    }

                    SetPositionCurves(
                        clip,
                        path,
                        start.LocalPosition,
                        target.LocalPosition);
                    SetQuaternionCurves(
                        clip,
                        path,
                        start.LocalRotation,
                        targetRotation);
                }

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
            finally
            {
                if (guardianClone != null)
                {
                    UnityEngine.Object.DestroyImmediate(guardianClone);
                }
            }
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var state = machine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static TransitionMetrics InspectState(
            Transform placement,
            Transform targetSlot,
            Transform targetModel,
            Transform guardianModel,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            AnimationClip guardianClip)
        {
            RequireSlotContract(placement);
            RequireApprovedRenderer(targetModel);
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Pahur_08 Animator differs from the guardian transition contract.");
            }

            if (controller.layers.Length != 1 ||
                controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name != StateName ||
                controller.layers[0].stateMachine.defaultState.motion != clip ||
                Mathf.Abs(clip.length - DurationSeconds) > Tolerance)
            {
                throw new InvalidOperationException(
                    "The Pahur_08 clip or controller contract differs.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || settings.loopBlend)
            {
                throw new InvalidOperationException(
                    "Pahur_08 must loop without a blended return segment.");
            }

            var skeletonPaths = RequireSkeletonPaths(targetModel);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != skeletonPaths.Count * 7 ||
                bindings.Any(binding =>
                    binding.type != typeof(Transform) ||
                    !skeletonPaths.ContainsKey(binding.path) ||
                    !IsAllowedTransformProperty(binding.propertyName)))
            {
                throw new InvalidOperationException(
                    "Pahur_08 contains animation bindings outside the shared skeleton.");
            }

            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "A Pahur_08 transform curve is missing.");
                if (curve.length != 3 ||
                    Mathf.Abs(curve.keys[0].time) > Tolerance ||
                    Mathf.Abs(curve.keys[1].time - TransitionSeconds) > Tolerance ||
                    Mathf.Abs(curve.keys[2].time - DurationSeconds) > Tolerance)
                {
                    throw new InvalidOperationException(
                        "A Pahur_08 curve does not use the 0/0.8/1.8 second timing contract.");
                }
            }

            return CompareSampledPoses(
                targetSlot,
                targetModel,
                guardianModel,
                clip,
                guardianClip,
                skeletonPaths.Count,
                bindings.Length);
        }

        private static TransitionMetrics CompareSampledPoses(
            Transform targetSlot,
            Transform targetModel,
            Transform guardianModel,
            AnimationClip clip,
            AnimationClip guardianClip,
            int skeletonPathCount,
            int bindingCount)
        {
            GameObject transitionClone = null;
            GameObject guardianClone = null;
            GameObject standingClone = null;
            try
            {
                transitionClone = UnityEngine.Object.Instantiate(
                    targetModel.gameObject);
                guardianClone = UnityEngine.Object.Instantiate(
                    guardianModel.gameObject);
                var standingPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                    throw new InvalidOperationException(
                        "The approved standing Pahur FBX is missing.");
                standingClone = UnityEngine.Object.Instantiate(standingPrefab);
                transitionClone.hideFlags = HideFlags.HideAndDontSave;
                guardianClone.hideFlags = HideFlags.HideAndDontSave;
                standingClone.hideFlags = HideFlags.HideAndDontSave;
                DisableAnimationComponents(transitionClone.transform);
                DisableAnimationComponents(guardianClone.transform);
                DisableAnimationComponents(standingClone.transform);

                var transitionRootBefore =
                    TransformPose.From(transitionClone.transform);
                var targetSlotBefore = TransformPose.From(targetSlot);
                var standing = RequireSkeletonPaths(standingClone.transform);
                var transitionPaths =
                    RequireSkeletonPaths(transitionClone.transform);
                var guardianPaths =
                    RequireSkeletonPaths(guardianClone.transform);
                RequireSamePathSet(standing.Keys, transitionPaths.Keys);
                RequireSamePathSet(transitionPaths.Keys, guardianPaths.Keys);

                clip.SampleAnimation(transitionClone, 0f);
                var startPose = CapturePoses(transitionPaths);
                var standingPose = CapturePoses(standing);
                var startError = ComparePoses(startPose, standingPose);

                clip.SampleAnimation(transitionClone, TransitionSeconds);
                var finalPose = CapturePoses(transitionPaths);
                clip.SampleAnimation(
                    transitionClone,
                    DurationSeconds - 1f / FrameRate);
                var holdPose = CapturePoses(transitionPaths);

                guardianClip.SampleAnimation(guardianClone, 0f);
                var guardianPose = CapturePoses(guardianPaths);
                var finalError = ComparePoses(finalPose, guardianPose);
                var holdError = ComparePoses(finalPose, holdPose);
                var transitionDelta = ComparePoses(startPose, finalPose);

                if (startError.Position > Tolerance ||
                    startError.RotationDegrees > RotationToleranceDegrees)
                {
                    throw new InvalidOperationException(
                        "Pahur_08 does not start in the approved standing pose.");
                }

                if (finalError.Position > Tolerance ||
                    finalError.RotationDegrees > RotationToleranceDegrees)
                {
                    throw new InvalidOperationException(
                        "Pahur_08 does not finish in the Pahur_06 guardian pose.");
                }

                if (holdError.Position > Tolerance ||
                    holdError.RotationDegrees > RotationToleranceDegrees)
                {
                    throw new InvalidOperationException(
                        "Pahur_08 does not hold the final guardian pose for one second.");
                }

                if (transitionDelta.Position <= Tolerance &&
                    transitionDelta.RotationDegrees <= RotationToleranceDegrees)
                {
                    throw new InvalidOperationException(
                        "Pahur_08 contains no standing-to-guardian pose change.");
                }

                if (!transitionRootBefore.Approximately(
                        TransformPose.From(transitionClone.transform)) ||
                    !targetSlotBefore.Approximately(TransformPose.From(targetSlot)))
                {
                    throw new InvalidOperationException(
                        "Pahur_08 animation changed the model root or slot transform.");
                }

                return new TransitionMetrics(
                    skeletonPathCount,
                    bindingCount,
                    startError.Position,
                    startError.RotationDegrees,
                    finalError.Position,
                    finalError.RotationDegrees,
                    holdError.Position,
                    holdError.RotationDegrees,
                    transitionDelta.Position,
                    transitionDelta.RotationDegrees);
            }
            finally
            {
                DestroyImmediateIfPresent(transitionClone);
                DestroyImmediateIfPresent(guardianClone);
                DestroyImmediateIfPresent(standingClone);
            }
        }

        private static Dictionary<string, Transform> RequireSkeletonPaths(
            Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    model.name + " must contain exactly one skinned renderer.");
            if (renderer.bones.Length != ExpectedBones)
            {
                throw new InvalidOperationException(
                    model.name + " bone count differs. Count=" +
                    renderer.bones.Length + ".");
            }

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

            var result = new Dictionary<string, Transform>(
                StringComparer.Ordinal);
            foreach (var item in transforms)
            {
                var path = AnimationUtility.CalculateTransformPath(item, model);
                if (string.IsNullOrEmpty(path))
                {
                    throw new InvalidOperationException(
                        "The Pahur skeleton unexpectedly includes the model root.");
                }

                result.Add(path, item);
            }

            return result;
        }

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

        private static void CapturePoseStrip(
            Transform model,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur guardian transition review folder."));
            var poseSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var otherRendererSnapshots = model.gameObject.scene
                .GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .Where(item => !item.transform.IsChildOf(model))
                .Select(item => new RendererSnapshot(item))
                .ToArray();
            var animator = RequireAnimator(model);
            var animatorEnabled = animator.enabled;
            var player = GameObject.Find("Player") ??
                         throw new InvalidOperationException("Player is missing.");
            var sourceCamera = player.GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "The Player camera is missing.");
            var cameraObject = new GameObject(
                "PahurToGuardianStanceReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int panelWidth = 384;
            const int panelHeight = 640;
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
            var strip = new Texture2D(
                panelWidth * 5,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            var times = new[] { 0f, 0.4f, 0.8f, 1.3f, 1.799f };
            try
            {
                foreach (var snapshot in otherRendererSnapshots)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = target;

                var combined = default(Bounds);
                var hasBounds = false;
                foreach (var time in times)
                {
                    clip.SampleAnimation(model.gameObject, time);
                    var frameBounds = BoundsOf(model);
                    if (!hasBounds)
                    {
                        combined = frameBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(frameBounds);
                    }
                }

                FrameCamera(
                    camera,
                    combined,
                    model,
                    sourceCamera,
                    panelWidth / (float)panelHeight);
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(model.gameObject, times[index]);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, panelWidth, panelHeight),
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
                            "The Pahur guardian transition review contains Unity's magenta shader fallback.");
                    }

                    strip.SetPixels32(
                        index * panelWidth,
                        0,
                        panelWidth,
                        panelHeight,
                        pixels);
                }

                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in otherRendererSnapshots)
                {
                    snapshot.Restore();
                }

                foreach (var snapshot in poseSnapshots)
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
            Bounds bounds,
            Transform model,
            Camera sourceCamera,
            float aspect)
        {
            var viewDirection = sourceCamera.transform.position - bounds.center;
            viewDirection.y = 0f;
            if (viewDirection.sqrMagnitude < 0.0001f)
            {
                viewDirection = -model.forward;
            }

            viewDirection.Normalize();
            camera.aspect = aspect;
            var verticalDistance = bounds.extents.y /
                                   Mathf.Tan(camera.fieldOfView *
                                             Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) *
                aspect);
            var horizontalDistance =
                Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(verticalDistance, horizontalDistance) * 1.18f;
            camera.transform.position = bounds.center +
                                        viewDirection * distance +
                                        Vector3.up * (bounds.extents.y * 0.02f);
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Pahur_08 has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void WriteValidationReport(TransitionMetrics metrics)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur guardian transition validation folder."));
            File.WriteAllLines(
                destination,
                new[]
                {
                    "Pahur To Guardian Stance Animation Validation",
                    "Result=PASS",
                    "Scene=" + ScenePath,
                    "Slot=" + TargetSlotName,
                    "Clip=" + ClipPath,
                    "Controller=" + ControllerPath,
                    "State=" + StateName,
                    "TransitionSeconds=" + Num(TransitionSeconds),
                    "HoldSeconds=" + Num(HoldSeconds),
                    "DurationSeconds=" + Num(DurationSeconds),
                    "TargetGuardianClipTime=0",
                    "Loop=True",
                    "ReturnSegment=False",
                    "RootMotion=False",
                    "SkeletonPaths=" + metrics.SkeletonPathCount,
                    "TransformBindings=" + metrics.BindingCount,
                    "StartStandingPositionError=" + Num(metrics.StartPositionError),
                    "StartStandingRotationErrorDegrees=" + Num(metrics.StartRotationErrorDegrees),
                    "FinalGuardianPositionError=" + Num(metrics.FinalPositionError),
                    "FinalGuardianRotationErrorDegrees=" + Num(metrics.FinalRotationErrorDegrees),
                    "HoldPositionChange=" + Num(metrics.HoldPositionChange),
                    "HoldRotationChangeDegrees=" + Num(metrics.HoldRotationChangeDegrees),
                    "StandingToGuardianPositionChange=" + Num(metrics.TransitionPositionChange),
                    "StandingToGuardianRotationChangeDegrees=" + Num(metrics.TransitionRotationChangeDegrees),
                    "OtherSlotsPreservedByApply=True",
                    "OtherSceneRootsPreservedByApply=True",
                    "SceneSaved=True",
                    "ValidationSceneChanged=False"
                },
                new UTF8Encoding(false));
        }

        private static SkinnedMeshRenderer RequireApprovedRenderer(
            Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    model.name + " must contain exactly one skinned renderer.");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(
                model.gameObject);
            var triangleCount = renderer.sharedMesh == null
                ? 0
                : renderer.sharedMesh.triangles.Length / 3;
            if (source == null ||
                AssetDatabase.GetAssetPath(source) != ModelPath ||
                triangleCount != ExpectedTriangles ||
                renderer.bones.Length != ExpectedBones ||
                renderer.sharedMaterials.Length != ExpectedMaterials)
            {
                throw new InvalidOperationException(
                    "Pahur_08 approved model, mesh, rig, or material contract differs.");
            }

            return renderer;
        }

        private static string AppearanceSignature(SkinnedMeshRenderer renderer)
        {
            return AssetDatabase.GetAssetPath(renderer.sharedMesh) + "|" +
                   string.Join(
                       ",",
                       renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath));
        }

        private static Animator RequireAnimator(Transform model)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
            {
                throw new InvalidOperationException(
                    TargetSlotName + " must contain exactly one Animator.");
            }

            return animators[0];
        }

        private static AnimationClip RequireTransitionClip()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException(
                       "The Pahur guardian transition clip is missing.");
        }

        private static AnimationClip RequireGuardianClip()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(GuardianClipPath) ??
                   throw new InvalidOperationException(
                       "The approved Pahur guardian clip is missing.");
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(
                       ControllerPath) ??
                   throw new InvalidOperationException(
                       "The Pahur guardian transition controller is missing.");
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
            {
                throw new InvalidOperationException(
                    slot.name + " must contain exactly one Pahur_Model.");
            }

            return slot.GetChild(0);
        }

        private static Transform RequireDirectChild(
            Transform parent,
            string name)
        {
            return parent.Cast<Transform>()
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Pahur slot is missing: " + name + ".");
        }

        private static GameObject RequirePlacementRoot()
        {
            return GameObject.Find(PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The Pahur placement root is missing.");
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" +
                    scene.path + ".");
            }

            return scene;
        }

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The Pahur placement must contain exactly eleven slots.");
            }

            for (var index = 0; index < SlotNames.Length; index++)
            {
                if (placement.GetChild(index).name != SlotNames[index] ||
                    placement.GetChild(index).childCount != 1)
                {
                    throw new InvalidOperationException(
                        "The Pahur slot contract differs at index " + index + ".");
                }
            }
        }

        private static string[] OtherSlotSignatures(Transform placement)
        {
            return placement.Cast<Transform>()
                .Where(item => item.name != TargetSlotName)
                .Select(HierarchyAndAssetSignature)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .Select(item => HierarchyAndAssetSignature(item.transform))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchyAndAssetSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item => AnimationUtility.CalculateTransformPath(item, root),
                             StringComparer.Ordinal))
            {
                builder.Append(AnimationUtility.CalculateTransformPath(item, root));
                builder.Append('|');
                builder.Append(Vec(item.localPosition));
                builder.Append('|');
                builder.Append(Quat(item.localRotation));
                builder.Append('|');
                builder.Append(Vec(item.localScale));
                builder.Append(';');
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                builder.Append(renderer.enabled);
                builder.Append('|');
                builder.Append(AssetDatabase.GetAssetPath(
                    (renderer as SkinnedMeshRenderer)?.sharedMesh));
                builder.Append('|');
                builder.Append(string.Join(
                    ",",
                    renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
                builder.Append(';');
            }

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                builder.Append(animator.enabled);
                builder.Append('|');
                builder.Append(animator.applyRootMotion);
                builder.Append('|');
                builder.Append(AssetDatabase.GetAssetPath(
                    animator.runtimeAnimatorController));
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static string PlacementTransformSignature(
            Transform slot,
            Transform model)
        {
            return Vec(slot.localPosition) + "|" +
                   Quat(slot.localRotation) + "|" +
                   Vec(slot.localScale) + "|" +
                   Vec(model.localPosition) + "|" +
                   Quat(model.localRotation) + "|" +
                   Vec(model.localScale);
        }

        private static void RequireSameSignatures(
            IReadOnlyList<string> before,
            IReadOnlyList<string> after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static Dictionary<string, TransformPose> CapturePoses(
            IReadOnlyDictionary<string, Transform> paths)
        {
            return paths.ToDictionary(
                item => item.Key,
                item => TransformPose.From(item.Value),
                StringComparer.Ordinal);
        }

        private static PoseError ComparePoses(
            IReadOnlyDictionary<string, TransformPose> first,
            IReadOnlyDictionary<string, TransformPose> second)
        {
            RequireSamePathSet(first.Keys, second.Keys);
            var position = 0f;
            var rotation = 0f;
            foreach (var path in first.Keys)
            {
                position = Mathf.Max(
                    position,
                    Vector3.Distance(
                        first[path].LocalPosition,
                        second[path].LocalPosition));
                rotation = Mathf.Max(
                    rotation,
                    Quaternion.Angle(
                        first[path].LocalRotation,
                        second[path].LocalRotation));
            }

            return new PoseError(position, rotation);
        }

        private static void RequireSamePathSet(
            IEnumerable<string> first,
            IEnumerable<string> second)
        {
            if (!first.OrderBy(item => item, StringComparer.Ordinal)
                    .SequenceEqual(
                        second.OrderBy(item => item, StringComparer.Ordinal),
                        StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The standing and guardian Pahur skeleton paths differ.");
            }
        }

        private static bool IsAllowedTransformProperty(string property)
        {
            return property == "m_LocalPosition.x" ||
                   property == "m_LocalPosition.y" ||
                   property == "m_LocalPosition.z" ||
                   property == "m_LocalRotation.x" ||
                   property == "m_LocalRotation.y" ||
                   property == "m_LocalRotation.z" ||
                   property == "m_LocalRotation.w";
        }

        private static void DisableAnimationComponents(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
            }
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                throw new InvalidOperationException(
                    "Required Pahur animation folder is missing: " + folder + ".");
            }
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(
                    "Could not replace Pahur guardian transition asset: " + path + ".");
            }
        }

        private static void DestroyImmediateIfPresent(GameObject item)
        {
            if (item != null)
            {
                UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static Quaternion Negate(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static string Absolute(string relativePath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException(
                                  "Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w);
        }

        private readonly struct AnimatorSnapshot
        {
            private readonly bool enabled;
            private readonly RuntimeAnimatorController controller;
            private readonly bool applyRootMotion;
            private readonly AnimatorCullingMode cullingMode;
            private readonly AnimatorUpdateMode updateMode;

            public AnimatorSnapshot(Animator animator)
            {
                enabled = animator.enabled;
                controller = animator.runtimeAnimatorController;
                applyRootMotion = animator.applyRootMotion;
                cullingMode = animator.cullingMode;
                updateMode = animator.updateMode;
            }

            public void Restore(Animator animator)
            {
                animator.enabled = enabled;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = applyRootMotion;
                animator.cullingMode = cullingMode;
                animator.updateMode = updateMode;
            }
        }

        private readonly struct TransformPose
        {
            public TransformPose(
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

            public static TransformPose From(Transform transform)
            {
                return new TransformPose(
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale);
            }

            public bool Approximately(TransformPose other)
            {
                return Vector3.Distance(LocalPosition, other.LocalPosition) <= Tolerance &&
                       Quaternion.Angle(LocalRotation, other.LocalRotation) <=
                       RotationToleranceDegrees &&
                       Vector3.Distance(LocalScale, other.LocalScale) <= Tolerance;
            }
        }

        private readonly struct PoseError
        {
            public PoseError(float position, float rotationDegrees)
            {
                Position = position;
                RotationDegrees = rotationDegrees;
            }

            public float Position { get; }
            public float RotationDegrees { get; }
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform transform;
            private readonly TransformPose pose;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                pose = TransformPose.From(transform);
            }

            public void Restore()
            {
                transform.localPosition = pose.LocalPosition;
                transform.localRotation = pose.LocalRotation;
                transform.localScale = pose.LocalScale;
            }
        }

        private sealed class RendererSnapshot
        {
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public Renderer Renderer { get; }

            public void Restore()
            {
                Renderer.enabled = enabled;
            }
        }

        private readonly struct TransitionMetrics
        {
            public TransitionMetrics(
                int skeletonPathCount,
                int bindingCount,
                float startPositionError,
                float startRotationErrorDegrees,
                float finalPositionError,
                float finalRotationErrorDegrees,
                float holdPositionChange,
                float holdRotationChangeDegrees,
                float transitionPositionChange,
                float transitionRotationChangeDegrees)
            {
                SkeletonPathCount = skeletonPathCount;
                BindingCount = bindingCount;
                StartPositionError = startPositionError;
                StartRotationErrorDegrees = startRotationErrorDegrees;
                FinalPositionError = finalPositionError;
                FinalRotationErrorDegrees = finalRotationErrorDegrees;
                HoldPositionChange = holdPositionChange;
                HoldRotationChangeDegrees = holdRotationChangeDegrees;
                TransitionPositionChange = transitionPositionChange;
                TransitionRotationChangeDegrees = transitionRotationChangeDegrees;
            }

            public int SkeletonPathCount { get; }
            public int BindingCount { get; }
            public float StartPositionError { get; }
            public float StartRotationErrorDegrees { get; }
            public float FinalPositionError { get; }
            public float FinalRotationErrorDegrees { get; }
            public float HoldPositionChange { get; }
            public float HoldRotationChangeDegrees { get; }
            public float TransitionPositionChange { get; }
            public float TransitionRotationChangeDegrees { get; }
        }
    }
}
