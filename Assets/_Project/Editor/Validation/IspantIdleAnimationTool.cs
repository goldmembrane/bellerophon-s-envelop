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
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantIdleAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string ModelName = "Ispant_Model";
        private const string AnimationFbxPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Idle.fbx";
        private const string ClipPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_02_Idle.anim";
        internal const string ControllerPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_02_Idle.controller";
        private const string ReportPath = "docs/validation/ispant_idle_2026-08-05/Ispant_02_Idle_15mm_Inspection.txt";
        private const string DiagnosticPath = "docs/validation/ispant_idle_2026-08-05/Ispant_02_Idle_15mm_Diagnostic.png";
        private const string FinalReviewPath = "docs/validation/ispant_idle_2026-08-05/Ispant_02_Idle_15mm_FinalReview.png";
        private const string ExpectedAnimationSha256 = "B28ABB66FEF2095C3DCD56DEEC56B0FE79F9D3846A712EB969378B2E5B73AC8E";
        private const float Duration = 2f;
        private const float FrameRate = 60f;
        private const float ExpectedVerticalTravel = 0.015f;
        private const float TravelTolerance = 0.0002f;
        private const float FootTolerance = 0.001f;
        private const float LoopTolerance = 0.00002f;
        private const int SampleFrames = 120;
        private const string IdleRigName = "Ispant_Idle_FootLock_Rig";

        private static readonly string[] SlotNames =
        {
            "Ispant_01_Static",
            "Ispant_02_Idle",
            "Ispant_03_Move",
            "Ispant_04_DrawSword",
            "Ispant_05_RunningOneHandedSwordAttack",
            "Ispant_06_SheathSwordDrawMusket",
            "Ispant_07_BreakthroughMusketAimFireRecover",
            "Ispant_08_StowMusketDrawSword",
            "Ispant_09_OneHandedSwordAttack",
            "Ispant_10_Stop",
            "Ispant_11_HitReaction",
            "Ispant_12_Death"
        };

        private static readonly float[] ReviewTimes = { 0f, 0.5f, 1f, 1.5f, 2f };

        private static readonly HashSet<string> AnimatedBoneNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Hips", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
            "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase",
            "Spine02", "Spine01", "Spine", "LeftShoulder", "LeftArm",
            "LeftForeArm", "LeftHand", "RightShoulder", "RightArm",
            "RightForeArm", "RightHand", "neck", "Head", "head_end", "headfront"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Idle Animation")]
        public static void ApplyIspantIdleAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequirePreApplySlotContract(placement.transform);
            RequireHash(AnimationFbxPath, ExpectedAnimationSha256);

            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var slotTransformsBefore = Enumerable.Range(0, placement.transform.childCount)
                .Select(index => new TransformSnapshot(placement.transform.GetChild(index)))
                .ToArray();
            var modelTransformsBefore = Enumerable.Range(0, placement.transform.childCount)
                .Select(index => new TransformSnapshot(RequireDirectChild(placement.transform.GetChild(index), ModelName)))
                .ToArray();

            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.transform.GetChild(index);
                if (slot.name != SlotNames[index])
                {
                    slot.name = SlotNames[index];
                    EditorUtility.SetDirty(slot.gameObject);
                }
            }

            ConfigureAnimationImporter();
            RequireSlotContract(placement.transform);
            var idleModel = RequireDirectChild(RequireDirectChild(placement.transform, SlotNames[1]), ModelName);
            var clip = CreateSanitizedClip(RequireSourceClip(), idleModel);
            var controller = CreateController(clip);
            ConfigureIdleAnimator(idleModel, controller);
            ConfigureIdleRig(idleModel);
            NormalizeRiggedTorsoTravel(idleModel, clip);
            DisableUnapprovedSlotAnimators(placement.transform);

            if (slotTransformsBefore.Any(snapshot => !snapshot.Matches(0.000001f)) ||
                modelTransformsBefore.Any(snapshot => !snapshot.Matches(0.000001f)))
                throw new InvalidOperationException("An Ispant slot or model transform changed while applying names and idle animation.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement), "A scene root outside the Ispant placement changed.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after the Ispant idle apply.");
            AssetDatabase.SaveAssets();
            Debug.Log("IspantIdleAnimationApplied Result=PASS, Slot=Ispant_02_Idle, Duration=2, TargetWorldVerticalTravel=0.015, RootMotion=False, SlotNames=12, OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Idle Animation")]
        public static void InspectIspantIdleAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            RequireHash(AnimationFbxPath, ExpectedAnimationSha256);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var model = RequireDirectChild(RequireDirectChild(placement.transform, SlotNames[1]), ModelName);
            var metrics = Inspect(model, placement.transform, RequireClip(), RequireController());
            WriteReport(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant idle inspection changed the scene dirty state.");
            Debug.Log("IspantIdleAnimationInspected Result=PASS, VerticalTravel=" + Num(metrics.VerticalTravel) +
                ", MaximumFootError=" + Num(metrics.MaximumFootError) +
                ", MaximumLoopError=" + Num(metrics.MaximumLoopError) +
                ", HeadTravel=" + Num(metrics.HeadTravel) +
                ", LeftHandTravel=" + Num(metrics.LeftHandTravel) +
                ", RightHandTravel=" + Num(metrics.RightHandTravel) +
                ", BackMusketTravel=" + Num(metrics.BackMusketTravel) +
                ", MinimumKneeRotation=" + Num(metrics.MinimumKneeRotation) +
                ", RootTranslation=0, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Idle Diagnostic")]
        public static void CaptureIspantIdleAnimationDiagnostic()
        {
            CaptureReview(DiagnosticPath, "IspantIdleAnimationDiagnosticCaptured", requireInspectionPass: false);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Idle Final Review")]
        public static void CaptureIspantIdleAnimationFinalReview()
        {
            CaptureReview(FinalReviewPath, "IspantIdleAnimationFinalReviewCaptured", requireInspectionPass: true);
        }

        private static void CaptureReview(string relativePath, string logPrefix, bool requireInspectionPass)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var model = RequireDirectChild(RequireDirectChild(placement.transform, SlotNames[1]), ModelName);
            var clip = RequireClip();
            if (requireInspectionPass)
                Inspect(model, placement.transform, clip, RequireController());
            var destination = Absolute(relativePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time Ispant idle capture already exists: " + relativePath);
            CaptureStrip(model, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant idle capture changed the scene dirty state.");
            Debug.Log(logPrefix + " Result=PASS, Times=0,0.5,1,1.5,2, Image=" + relativePath + ", SceneChanged=False.");
        }

        private static void ConfigureAnimationImporter()
        {
            var importer = AssetImporter.GetAtPath(AnimationFbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ispant idle FBX importer is unavailable.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("Ispant idle FBX must expose exactly one take.");
            clips[0].name = "Ispant_Idle";
            clips[0].firstFrame = 0f;
            clips[0].lastFrame = SampleFrames;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootHeightY = false;
            clips[0].lockRootPositionXZ = true;
            clips[0].lockRootRotation = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(AnimationFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException("Ispant idle FBX must import exactly one animation clip.");
            return clips[0];
        }

        private static AnimationClip CreateSanitizedClip(AnimationClip source, Transform model)
        {
            DeleteAssetIfPresent(ClipPath, "Existing Ispant idle clip could not be replaced.");
            var clip = new AnimationClip { name = "Ispant_02_Idle", frameRate = FrameRate };
            var copied = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                var boneName = binding.path.Split('/').Last();
                if (boneName != "Hips" || !binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
                    continue;
                var target = FindUniqueDescendant(model, boneName);
                if (target == null)
                    continue;
                var targetPath = AnimationUtility.CalculateTransformPath(target, model);
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
                    curve = RebasePositionCurve(curve, target, binding.propertyName);
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), binding.propertyName),
                    curve);
                copied++;
            }
            if (copied == 0)
                throw new InvalidOperationException("No Ispant idle transform curves could be mapped to the scene rig.");

            SetLoopSettings(clip);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            var initialTravel = MeasureVisibleTorsoTravel(model, clip);
            if (initialTravel <= 0.000001f)
                throw new InvalidOperationException("Imported Ispant hip cycle has no measurable torso travel.");
            ScalePositionCurveDeltas(clip, ExpectedVerticalTravel / initialTravel);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            var finalTravel = MeasureVisibleTorsoTravel(model, clip);
            if (Mathf.Abs(finalTravel - ExpectedVerticalTravel) > TravelTolerance)
                throw new InvalidOperationException("Ispant torso travel normalization failed. Actual=" + Num(finalTravel) + ".");
            return clip;
        }

        private static void ScalePositionCurveDeltas(AnimationClip clip, float ratio)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
                    continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var baseline = curve.keys[0].value;
                var keys = curve.keys;
                for (var index = 0; index < keys.Length; index++)
                {
                    keys[index].value = baseline + (keys[index].value - baseline) * ratio;
                    keys[index].inTangent *= ratio;
                    keys[index].outTangent *= ratio;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static AnimationCurve RebasePositionCurve(AnimationCurve source, Transform target, string propertyName)
        {
            if (source == null || source.length == 0)
                throw new InvalidOperationException("Ispant position curve is empty: " + propertyName + ".");
            var targetBaseline = propertyName.EndsWith(".x", StringComparison.Ordinal)
                ? target.localPosition.x
                : propertyName.EndsWith(".y", StringComparison.Ordinal)
                    ? target.localPosition.y
                    : target.localPosition.z;
            var sourceBaseline = source.keys[0].value;
            var keys = source.keys;
            for (var index = 0; index < keys.Length; index++)
                keys[index].value = targetBaseline + (keys[index].value - sourceBaseline);
            var rebased = new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return rebased;
        }

        private static void SetLoopSettings(AnimationClip clip)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static float MeasureVisibleTorsoTravel(Transform model, AnimationClip clip)
        {
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.name == "Ispant_Armed_Body") ??
                throw new InvalidOperationException("Ispant body renderer is missing.");
            var torso = MeshRegions.WeightedRegion(body, 0.45f, "Hips", "Spine02", "Spine01", "Spine");
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault();
            var animatorEnabled = animator != null && animator.enabled;
            var baked = new Mesh();
            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            try
            {
                if (animator != null)
                    animator.enabled = false;
                for (var frame = 0; frame <= SampleFrames; frame++)
                {
                    clip.SampleAnimation(model.gameObject, frame / FrameRate);
                    body.BakeMesh(baked);
                    var y = RegionCentroidY(WorldVertices(body, baked), torso);
                    minimum = Mathf.Min(minimum, y);
                    maximum = Mathf.Max(maximum, y);
                }
                return maximum - minimum;
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                if (animator != null)
                    animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void NormalizeRiggedTorsoTravel(Transform model, AnimationClip clip)
        {
            var initialTravel = MeasureRiggedTorsoTravel(model, clip);
            if (initialTravel <= 0.000001f)
                throw new InvalidOperationException("Rigged Ispant idle has no measurable torso travel.");
            ScalePositionCurveDeltas(clip, ExpectedVerticalTravel / initialTravel);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            var finalTravel = MeasureRiggedTorsoTravel(model, clip);
            if (Mathf.Abs(finalTravel - ExpectedVerticalTravel) > TravelTolerance)
                throw new InvalidOperationException("Rigged Ispant torso travel normalization failed. Actual=" + Num(finalTravel) + ".");
        }

        private static float MeasureRiggedTorsoTravel(Transform model, AnimationClip clip)
        {
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.name == "Ispant_Armed_Body") ??
                throw new InvalidOperationException("Ispant body renderer is missing.");
            var torso = MeshRegions.WeightedRegion(body, 0.45f, "Hips", "Spine02", "Spine01", "Spine");
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var builder = RequireIdleRig(model);
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            var baked = new Mesh();
            RigEvaluationSession evaluation = null;
            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            try
            {
                evaluation = new RigEvaluationSession(animator, builder, clip);
                for (var frame = 0; frame <= SampleFrames; frame++)
                {
                    evaluation.Sample(frame / FrameRate);
                    body.BakeMesh(baked);
                    var y = RegionCentroidY(WorldVertices(body, baked), torso);
                    minimum = Mathf.Min(minimum, y);
                    maximum = Mathf.Max(maximum, y);
                }
                return maximum - minimum;
            }
            finally
            {
                evaluation?.Dispose();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath, "Existing Ispant idle controller could not be replaced.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("IspantIdle");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureIdleAnimator(Transform model, AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
                throw new InvalidOperationException("Ispant_02_Idle contains multiple Animators.");
            var animator = animators.Length == 0 ? model.gameObject.AddComponent<Animator>() : animators[0];
            if (animator.transform != model)
                throw new InvalidOperationException("Ispant_02_Idle Animator must be on Ispant_Model.");
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
        }

        private static void ConfigureIdleRig(Transform model)
        {
            var existing = Enumerable.Range(0, model.childCount)
                .Select(model.GetChild)
                .Where(item => item.name == IdleRigName)
                .ToArray();
            foreach (var item in existing)
                UnityEngine.Object.DestroyImmediate(item.gameObject);

            var rigObject = new GameObject(IdleRigName);
            rigObject.transform.SetParent(model, false);
            var rig = rigObject.AddComponent(RequireRiggingType("UnityEngine.Animations.Rigging.Rig"));
            SetFloat(rig, "m_Weight", 1f);
            CreateFootConstraint(rigObject.transform, model, "Left");
            CreateFootConstraint(rigObject.transform, model, "Right");

            var builderType = RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            var builders = model.GetComponents(builderType);
            if (builders.Length > 1)
                throw new InvalidOperationException("Ispant_02_Idle contains multiple RigBuilders.");
            var builder = builders.Length == 0 ? model.gameObject.AddComponent(builderType) : builders[0];
            ((Behaviour)builder).enabled = true;
            var builderObject = new SerializedObject(builder);
            var layers = builderObject.FindProperty("m_RigLayers") ??
                throw new InvalidOperationException("Animation Rigging layer property is unavailable.");
            layers.arraySize = 1;
            var layer = layers.GetArrayElementAtIndex(0);
            layer.FindPropertyRelative("m_Rig").objectReferenceValue = rig;
            layer.FindPropertyRelative("m_Active").boolValue = true;
            builderObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rigObject);
            EditorUtility.SetDirty(rig);
            EditorUtility.SetDirty(builder);
        }

        private static void CreateFootConstraint(Transform rigRoot, Transform model, string side)
        {
            var upper = RequireDescendant(model, side + "UpLeg");
            var lower = RequireDescendant(model, side + "Leg");
            var foot = RequireDescendant(model, side + "Foot");
            var target = new GameObject(side + "FootTarget").transform;
            target.SetParent(rigRoot, false);
            target.position = foot.position;
            target.rotation = foot.rotation;
            var hint = new GameObject(side + "KneeHint").transform;
            hint.SetParent(rigRoot, false);
            var line = foot.position - upper.position;
            var projection = line.sqrMagnitude > 0.000001f
                ? upper.position + Vector3.Project(lower.position - upper.position, line)
                : upper.position;
            var bendDirection = lower.position - projection;
            if (bendDirection.sqrMagnitude < 0.000001f)
                bendDirection = model.forward;
            hint.position = lower.position + bendDirection.normalized * Mathf.Max(line.magnitude * 0.35f, 0.1f);
            hint.rotation = lower.rotation;

            var constraintObject = new GameObject(side + "FootTwoBoneIK");
            constraintObject.transform.SetParent(rigRoot, false);
            var constraint = constraintObject.AddComponent(RequireRiggingType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint"));
            var serialized = new SerializedObject(constraint);
            SetObject(serialized, "m_Data.m_Root", upper);
            SetObject(serialized, "m_Data.m_Mid", lower);
            SetObject(serialized, "m_Data.m_Tip", foot);
            SetObject(serialized, "m_Data.m_Target", target);
            SetObject(serialized, "m_Data.m_Hint", hint);
            SetFloat(serialized, "m_Data.m_TargetPositionWeight", 1f);
            SetFloat(serialized, "m_Data.m_TargetRotationWeight", 1f);
            SetFloat(serialized, "m_Data.m_HintWeight", 1f);
            SetBool(serialized, "m_Data.m_MaintainTargetPositionOffset", false);
            SetBool(serialized, "m_Data.m_MaintainTargetRotationOffset", false);
            SetFloat(serialized, "m_Weight", 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(constraint);
        }

        private static void DisableUnapprovedSlotAnimators(Transform placement)
        {
            for (var index = 0; index < SlotNames.Length; index++)
            {
                if (index == 1)
                    continue;
                var slot = RequireDirectChild(placement, SlotNames[index]);
                foreach (var animator in slot.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                    animator.runtimeAnimatorController = null;
                    animator.applyRootMotion = false;
                    EditorUtility.SetDirty(animator);
                }
                foreach (var animation in slot.GetComponentsInChildren<Animation>(true))
                {
                    animation.enabled = false;
                    EditorUtility.SetDirty(animation);
                }
            }
        }

        private static Component RequireIdleRig(Transform model)
        {
            var builderType = RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            var constraintType = RequireRiggingType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
            var builder = model.GetComponents(builderType).SingleOrDefault() ??
                throw new InvalidOperationException("Ispant_02_Idle RigBuilder is missing.");
            var builderObject = new SerializedObject(builder);
            var layers = builderObject.FindProperty("m_RigLayers");
            if (!((Behaviour)builder).enabled || layers == null || layers.arraySize != 1)
                throw new InvalidOperationException("Ispant_02_Idle RigBuilder configuration differs.");
            var layer = layers.GetArrayElementAtIndex(0);
            var rig = layer.FindPropertyRelative("m_Rig").objectReferenceValue as Component;
            if (!layer.FindPropertyRelative("m_Active").boolValue || rig == null || rig.name != IdleRigName ||
                Mathf.Abs(GetFloat(rig, "m_Weight") - 1f) > 0.0001f)
                throw new InvalidOperationException("Ispant_02_Idle Rig layer configuration differs.");
            var constraints = rig.GetComponentsInChildren(constraintType, true);
            if (constraints.Length != 2)
                throw new InvalidOperationException("Ispant_02_Idle must contain exactly two foot IK constraints.");
            foreach (var constraint in constraints)
            {
                var serialized = new SerializedObject(constraint);
                var isValid = (bool)(constraint.GetType().GetMethod("IsValid")?.Invoke(constraint, null) ?? false);
                if (GetObject(serialized, "m_Data.m_Root") == null || GetObject(serialized, "m_Data.m_Mid") == null ||
                    GetObject(serialized, "m_Data.m_Tip") == null || GetObject(serialized, "m_Data.m_Target") == null ||
                    GetObject(serialized, "m_Data.m_Hint") == null ||
                    GetFloat(serialized, "m_Data.m_TargetPositionWeight") < 0.9999f ||
                    GetFloat(serialized, "m_Data.m_TargetRotationWeight") < 0.9999f ||
                    GetFloat(serialized, "m_Data.m_HintWeight") < 0.9999f || GetFloat(serialized, "m_Weight") < 0.9999f || !isValid)
                    throw new InvalidOperationException("Ispant foot IK constraint differs.");
            }
            return builder;
        }

        private static Metrics Inspect(Transform model, Transform placement, AnimationClip clip, AnimatorController controller)
        {
            if (Mathf.Abs(clip.length - Duration) > 0.001f || Mathf.Abs(clip.frameRate - FrameRate) > 0.001f)
                throw new InvalidOperationException("Ispant idle duration or frame rate differs.");
            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
                throw new InvalidOperationException("Ispant idle clip is not looping.");
            if (AnimationUtility.GetCurveBindings(clip).Any(binding => binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Ispant idle must not contain bone scale curves.");

            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Ispant_02_Idle must contain exactly one Animator.");
            if (animator.transform != model || !animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("Ispant_02_Idle Animator configuration differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = RequireDirectChild(placement, SlotNames[index]);
                var enabledAnimators = slot.GetComponentsInChildren<Animator>(true).Where(item => item.enabled).ToArray();
                var enabledLegacy = slot.GetComponentsInChildren<Animation>(true).Any(item => item.enabled);
                if (enabledLegacy || (index == 1 ? enabledAnimators.Length != 1 : enabledAnimators.Length != 0))
                    throw new InvalidOperationException(SlotNames[index] + " animation state differs.");
            }

            var leftLeg = RequireDescendant(model, "LeftLeg");
            var rightLeg = RequireDescendant(model, "RightLeg");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.name == "Ispant_Armed_Body") ??
                throw new InvalidOperationException("Ispant body renderer is missing.");
            var rigBuilder = RequireIdleRig(model);
            var regions = MeshRegions.Create(body);
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            var animatorEnabled = animator.enabled;
            var modelLocalPosition = model.localPosition;
            var modelLocalRotation = model.localRotation;
            var modelLocalScale = model.localScale;
            var baked = new Mesh();
            RigEvaluationSession evaluation = null;
            try
            {
                evaluation = new RigEvaluationSession(animator, rigBuilder, clip);
                evaluation.Sample(0f);
                body.BakeMesh(baked);
                var initialVertices = WorldVertices(body, baked);
                var initialLeftLeg = leftLeg.rotation;
                var initialRightLeg = rightLeg.rotation;
                var minTorso = RegionCentroidY(initialVertices, regions.Torso);
                var maxTorso = minTorso;
                var minHead = RegionCentroidY(initialVertices, regions.Head);
                var maxHead = minHead;
                var minLeftHand = RegionCentroidY(initialVertices, regions.LeftHand);
                var maxLeftHand = minLeftHand;
                var minRightHand = RegionCentroidY(initialVertices, regions.RightHand);
                var maxRightHand = minRightHand;
                var minMusket = RegionCentroidY(initialVertices, regions.Musket);
                var maxMusket = minMusket;
                var maxFootError = 0f;
                var maxFootErrorTime = 0f;
                var maxFootName = string.Empty;
                var maxLeftKnee = 0f;
                var maxRightKnee = 0f;

                for (var frame = 0; frame <= SampleFrames; frame++)
                {
                    evaluation.Sample(frame / FrameRate);
                    body.BakeMesh(baked);
                    var vertices = WorldVertices(body, baked);
                    var torsoY = RegionCentroidY(vertices, regions.Torso);
                    var headY = RegionCentroidY(vertices, regions.Head);
                    var leftHandY = RegionCentroidY(vertices, regions.LeftHand);
                    var rightHandY = RegionCentroidY(vertices, regions.RightHand);
                    var musketY = RegionCentroidY(vertices, regions.Musket);
                    minTorso = Mathf.Min(minTorso, torsoY);
                    maxTorso = Mathf.Max(maxTorso, torsoY);
                    minHead = Mathf.Min(minHead, headY);
                    maxHead = Mathf.Max(maxHead, headY);
                    minLeftHand = Mathf.Min(minLeftHand, leftHandY);
                    maxLeftHand = Mathf.Max(maxLeftHand, leftHandY);
                    minRightHand = Mathf.Min(minRightHand, rightHandY);
                    maxRightHand = Mathf.Max(maxRightHand, rightHandY);
                    minMusket = Mathf.Min(minMusket, musketY);
                    maxMusket = Mathf.Max(maxMusket, musketY);
                    var leftFootError = RegionCentroidDistance(vertices, initialVertices, regions.LeftFoot);
                    var rightFootError = RegionCentroidDistance(vertices, initialVertices, regions.RightFoot);
                    if (leftFootError > maxFootError)
                    {
                        maxFootError = leftFootError;
                        maxFootErrorTime = frame / FrameRate;
                        maxFootName = "LeftFoot";
                    }
                    if (rightFootError > maxFootError)
                    {
                        maxFootError = rightFootError;
                        maxFootErrorTime = frame / FrameRate;
                        maxFootName = "RightFoot";
                    }
                    maxLeftKnee = Mathf.Max(maxLeftKnee, Quaternion.Angle(initialLeftLeg, leftLeg.rotation));
                    maxRightKnee = Mathf.Max(maxRightKnee, Quaternion.Angle(initialRightLeg, rightLeg.rotation));
                    if (Vector3.Distance(model.localPosition, modelLocalPosition) > 0.000001f ||
                        Quaternion.Angle(model.localRotation, modelLocalRotation) > 0.000001f ||
                        Vector3.Distance(model.localScale, modelLocalScale) > 0.000001f)
                        throw new InvalidOperationException("Ispant idle changed the model root transform.");
                }

                body.BakeMesh(baked);
                var endVertices = WorldVertices(body, baked);
                var maxLoopError = regions.AllTracked.Max(region => MaximumRegionDistance(endVertices, initialVertices, region));
                var verticalTravel = maxTorso - minTorso;
                var headTravel = maxHead - minHead;
                var leftHandTravel = maxLeftHand - minLeftHand;
                var rightHandTravel = maxRightHand - minRightHand;
                var musketTravel = maxMusket - minMusket;
                var minimumKneeRotation = Mathf.Min(maxLeftKnee, maxRightKnee);
                if (Mathf.Abs(verticalTravel - ExpectedVerticalTravel) > TravelTolerance)
                    throw new InvalidOperationException("Ispant idle vertical travel differs. Actual=" + Num(verticalTravel) + ".");
                if (maxFootError > FootTolerance)
                    throw new InvalidOperationException("Ispant idle feet do not remain planted. Error=" + Num(maxFootError) +
                        ", Time=" + Num(maxFootErrorTime) + ", Foot=" + maxFootName + ".");
                if (maxLoopError > LoopTolerance)
                    throw new InvalidOperationException("Ispant idle loop boundary differs. Error=" + Num(maxLoopError) + ".");
                var minimumFollowTravel = ExpectedVerticalTravel * 0.8f;
                if (headTravel < minimumFollowTravel || leftHandTravel < minimumFollowTravel ||
                    rightHandTravel < minimumFollowTravel || musketTravel < minimumFollowTravel)
                    throw new InvalidOperationException("Ispant torso, arms, or back-carried equipment do not follow the body travel.");
                if (minimumKneeRotation < 0.5f)
                    throw new InvalidOperationException("Ispant idle does not show bilateral knee flexion.");
                return new Metrics(verticalTravel, maxFootError, maxLoopError, headTravel, leftHandTravel, rightHandTravel, musketTravel, minimumKneeRotation);
            }
            finally
            {
                evaluation?.Dispose();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3[] WorldVertices(SkinnedMeshRenderer renderer, Mesh baked)
        {
            var matrix = renderer.localToWorldMatrix;
            var vertices = baked.vertices;
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] = matrix.MultiplyPoint3x4(vertices[index]);
            return vertices;
        }

        private static float RegionCentroidY(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> indices)
        {
            var sum = 0f;
            for (var index = 0; index < indices.Count; index++)
                sum += vertices[indices[index]].y;
            return sum / indices.Count;
        }

        private static Vector3 RegionCentroid(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> indices)
        {
            var sum = Vector3.zero;
            for (var index = 0; index < indices.Count; index++)
                sum += vertices[indices[index]];
            return sum / indices.Count;
        }

        private static float RegionCentroidDistance(IReadOnlyList<Vector3> current, IReadOnlyList<Vector3> initial, IReadOnlyList<int> indices)
        {
            return Vector3.Distance(RegionCentroid(current, indices), RegionCentroid(initial, indices));
        }

        private static float MaximumRegionDistance(IReadOnlyList<Vector3> current, IReadOnlyList<Vector3> initial, IReadOnlyList<int> indices)
        {
            var maximum = 0f;
            for (var index = 0; index < indices.Count; index++)
            {
                var vertexIndex = indices[index];
                maximum = Mathf.Max(maximum, Vector3.Distance(current[vertexIndex], initial[vertexIndex]));
            }
            return maximum;
        }

        private static void CaptureStrip(Transform model, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid capture folder."));
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var rigBuilder = RequireIdleRig(model);
            RigEvaluationSession evaluation = null;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .Where(item => !item.transform.IsChildOf(model))
                .Select(item => new RendererSnapshot(item))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject("IspantIdleReviewCamera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            const int width = 384;
            const int height = 640;
            var strip = new Texture2D(width * ReviewTimes.Length, height, TextureFormat.RGB24, false);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in otherRenderers)
                    snapshot.Renderer.enabled = false;
                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                evaluation = new RigEvaluationSession(animator, rigBuilder, clip);
                evaluation.Sample(0f);
                FrameCamera(camera, model, sourceCamera, width / (float)height);
                for (var index = 0; index < ReviewTimes.Length; index++)
                {
                    evaluation.Sample(ReviewTimes[index]);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                        throw new InvalidOperationException("Ispant idle review contains Unity magenta shader fallback.");
                    strip.SetPixels32(index * width, 0, width, height, pixels);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                evaluation?.Dispose();
                foreach (var renderer in otherRenderers)
                    renderer.Restore();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, Camera source, float aspect)
        {
            var body = model.GetComponentsInChildren<Renderer>(false)
                .SingleOrDefault(item => item.name == "Ispant_Armed_Body") ??
                throw new InvalidOperationException("Ispant body renderer is missing.");
            var bounds = body.bounds;
            var direction = source.transform.position - bounds.center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.back;
            direction.Normalize();
            camera.aspect = aspect;
            var vertical = bounds.extents.y / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) / Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position = bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position, Vector3.up);
        }

        private static void WriteReport(Metrics metrics)
        {
            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? throw new InvalidOperationException("Invalid report folder."));
            File.WriteAllLines(absolute, new[]
            {
                "Result=PASS",
                "Target=Approved Ispant Enemy Placement/Ispant_02_Idle",
                "SlotNames=" + string.Join(",", SlotNames),
                "DurationSeconds=" + Num(Duration),
                "FrameRate=" + Num(FrameRate),
                "VerticalTravel=" + Num(metrics.VerticalTravel),
                "MaximumFootError=" + Num(metrics.MaximumFootError),
                "MaximumLoopError=" + Num(metrics.MaximumLoopError),
                "HeadTravel=" + Num(metrics.HeadTravel),
                "LeftHandTravel=" + Num(metrics.LeftHandTravel),
                "RightHandTravel=" + Num(metrics.RightHandTravel),
                "BackMusketTravel=" + Num(metrics.BackMusketTravel),
                "MinimumKneeRotationDegrees=" + Num(metrics.MinimumKneeRotation),
                "FeetPlanted=True",
                "TorsoAndBackMusketFollow=True",
                "RootMotion=False",
                "BoneScaling=False",
                "OtherSlotsAnimated=False",
                "OtherSceneRootsUnchanged=True",
                "AnimationFbxSha256=" + ExpectedAnimationSha256
            }, Encoding.UTF8);
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("Open CargoRunMvp before working on Ispant idle.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            return scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                throw new InvalidOperationException("Approved Ispant placement is missing.");
        }

        private static void RequirePreApplySlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Ispant slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                var oldName = "Ispant_" + (index + 1).ToString("00", CultureInfo.InvariantCulture);
                if ((slot.name != oldName && slot.name != SlotNames[index]) || slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                    throw new InvalidOperationException("Ispant pre-apply slot contract differs at index " + index + ".");
            }
        }

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Ispant slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                    throw new InvalidOperationException("Ispant slot contract differs at index " + index + ".");
            }
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount).Select(parent.GetChild).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            return FindUniqueDescendant(root, name) ??
                throw new InvalidOperationException("Required Ispant bone differs: " + name + ".");
        }

        private static Transform FindUniqueDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static AnimationClip RequireClip()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Sanitized Ispant idle clip is missing.");
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Ispant idle controller is missing.");
        }

        private static void DeleteAssetIfPresent(string path, string message)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null && !AssetDatabase.DeleteAsset(path))
                throw new InvalidOperationException(message);
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform))
                .ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(Num(item.localPosition.x)).Append(',').Append(Num(item.localPosition.y)).Append(',').Append(Num(item.localPosition.z)).Append('|')
                    .Append(Num(item.localRotation.x)).Append(',').Append(Num(item.localRotation.y)).Append(',').Append(Num(item.localRotation.z)).Append(',').Append(Num(item.localRotation.w)).Append('|')
                    .Append(Num(item.localScale.x)).Append(',').Append(Num(item.localScale.y)).Append(',').Append(Num(item.localScale.z));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled);
                    if (renderer is SkinnedMeshRenderer skinned)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] before, string[] after, string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant animation asset hash differs: " + path + ".");
        }

        private static Type RequireRiggingType(string fullName)
        {
            return Type.GetType(fullName + ", Unity.Animation.Rigging", throwOnError: false) ??
                throw new InvalidOperationException("Animation Rigging type is unavailable: " + fullName + ".");
        }

        private static object InvokeRigBuilder(Component builder, string methodName, params object[] arguments)
        {
            var method = builder.GetType().GetMethods()
                .SingleOrDefault(item => item.Name == methodName && item.GetParameters().Length == arguments.Length) ??
                throw new InvalidOperationException("Animation Rigging method is unavailable: " + methodName + ".");
            return method.Invoke(builder, arguments);
        }

        private static void SetRigBuilderManualUpdate(Component builder)
        {
            var property = builder.GetType().GetProperty("graph") ??
                throw new InvalidOperationException("Animation Rigging graph property is unavailable.");
            var graph = (PlayableGraph)property.GetValue(builder);
            if (!graph.IsValid())
                throw new InvalidOperationException("Animation Rigging graph is invalid after build.");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        }

        private static void SetObject(SerializedObject serialized, string path, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(path) ??
                throw new InvalidOperationException("Animation Rigging object property is unavailable: " + path + ".");
            property.objectReferenceValue = value;
        }

        private static UnityEngine.Object GetObject(SerializedObject serialized, string path)
        {
            return serialized.FindProperty(path)?.objectReferenceValue;
        }

        private static void SetFloat(Component component, string path, float value)
        {
            var serialized = new SerializedObject(component);
            SetFloat(serialized, path, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(SerializedObject serialized, string path, float value)
        {
            var property = serialized.FindProperty(path) ??
                throw new InvalidOperationException("Animation Rigging float property is unavailable: " + path + ".");
            property.floatValue = value;
        }

        private static float GetFloat(Component component, string path)
        {
            return GetFloat(new SerializedObject(component), path);
        }

        private static float GetFloat(SerializedObject serialized, string path)
        {
            var property = serialized.FindProperty(path) ??
                throw new InvalidOperationException("Animation Rigging float property is unavailable: " + path + ".");
            return property.floatValue;
        }

        private static void SetBool(SerializedObject serialized, string path, bool value)
        {
            var property = serialized.FindProperty(path) ??
                throw new InvalidOperationException("Animation Rigging bool property is unavailable: " + path + ".");
            property.boolValue = value;
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct Metrics
        {
            public readonly float VerticalTravel;
            public readonly float MaximumFootError;
            public readonly float MaximumLoopError;
            public readonly float HeadTravel;
            public readonly float LeftHandTravel;
            public readonly float RightHandTravel;
            public readonly float BackMusketTravel;
            public readonly float MinimumKneeRotation;

            public Metrics(float verticalTravel, float maximumFootError, float maximumLoopError, float headTravel, float leftHandTravel, float rightHandTravel, float backMusketTravel, float minimumKneeRotation)
            {
                VerticalTravel = verticalTravel;
                MaximumFootError = maximumFootError;
                MaximumLoopError = maximumLoopError;
                HeadTravel = headTravel;
                LeftHandTravel = leftHandTravel;
                RightHandTravel = rightHandTravel;
                BackMusketTravel = backMusketTravel;
                MinimumKneeRotation = minimumKneeRotation;
            }
        }

        private sealed class RigEvaluationSession : IDisposable
        {
            private readonly Animator animator;
            private readonly Component builder;
            private readonly bool animatorEnabled;
            private readonly bool builderEnabled;
            private PlayableGraph graph;
            private AnimationClipPlayable clipPlayable;

            public RigEvaluationSession(Animator animator, Component builder, AnimationClip clip)
            {
                this.animator = animator;
                this.builder = builder;
                animatorEnabled = animator.enabled;
                builderEnabled = ((Behaviour)builder).enabled;
                animator.enabled = true;
                ((Behaviour)builder).enabled = false;
                animator.Rebind();

                graph = PlayableGraph.Create("IspantIdleRigEvaluation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                clipPlayable = AnimationClipPlayable.Create(graph, clip);
                clipPlayable.SetApplyFootIK(false);
                var output = AnimationPlayableOutput.Create(graph, "IspantIdleClip", animator);
                output.SetSourcePlayable(clipPlayable);
                if (!(bool)InvokeRigBuilder(builder, "Build", graph))
                    throw new InvalidOperationException("Ispant idle external RigBuilder graph could not be built.");
                graph.Play();
            }

            public void Sample(float time)
            {
                clipPlayable.SetTime(time);
                InvokeRigBuilder(builder, "SyncLayers");
                graph.Evaluate(0f);
            }

            public void Dispose()
            {
                if (graph.IsValid())
                    graph.Destroy();
                InvokeRigBuilder(builder, "Clear");
                ((Behaviour)builder).enabled = builderEnabled;
                animator.enabled = animatorEnabled;
            }
        }

        private sealed class MeshRegions
        {
            public int[] Torso { get; private set; }
            public int[] LeftFoot { get; private set; }
            public int[] RightFoot { get; private set; }
            public int[] Head { get; private set; }
            public int[] LeftHand { get; private set; }
            public int[] RightHand { get; private set; }
            public int[] Musket { get; private set; }
            public int[][] AllTracked => new[] { Torso, LeftFoot, RightFoot, Head, LeftHand, RightHand, Musket };

            public static MeshRegions Create(SkinnedMeshRenderer renderer)
            {
                return new MeshRegions
                {
                    Torso = WeightedVertices(renderer, 0.45f, "Hips", "Spine02", "Spine01", "Spine"),
                    LeftFoot = WeightedVertices(renderer, 0.65f, "LeftFoot", "LeftToeBase"),
                    RightFoot = WeightedVertices(renderer, 0.65f, "RightFoot", "RightToeBase"),
                    Head = WeightedVertices(renderer, 0.45f, "Head", "head_end", "headfront"),
                    LeftHand = WeightedVertices(renderer, 0.55f, "LeftHand"),
                    RightHand = WeightedVertices(renderer, 0.55f, "RightHand"),
                    Musket = MaterialVertices(renderer, "Wood")
                };
            }

            public static int[] WeightedRegion(SkinnedMeshRenderer renderer, float minimumWeight, params string[] boneNames)
            {
                return WeightedVertices(renderer, minimumWeight, boneNames);
            }

            private static int[] WeightedVertices(SkinnedMeshRenderer renderer, float minimumWeight, params string[] boneNames)
            {
                var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Ispant body mesh is missing.");
                var boneIndices = renderer.bones
                    .Select((bone, index) => new { bone, index })
                    .Where(item => item.bone != null && boneNames.Contains(item.bone.name, StringComparer.Ordinal))
                    .Select(item => item.index)
                    .ToHashSet();
                if (boneIndices.Count == 0)
                    throw new InvalidOperationException("Ispant weighted region bones are missing: " + string.Join(",", boneNames) + ".");
                var weights = mesh.boneWeights;
                var selected = new List<int>();
                for (var index = 0; index < weights.Length; index++)
                {
                    var weight = WeightFor(weights[index], boneIndices);
                    if (weight >= minimumWeight)
                        selected.Add(index);
                }
                if (selected.Count == 0)
                    throw new InvalidOperationException("Ispant weighted mesh region is empty: " + string.Join(",", boneNames) + ".");
                return selected.ToArray();
            }

            private static float WeightFor(BoneWeight weight, HashSet<int> boneIndices)
            {
                var total = 0f;
                if (boneIndices.Contains(weight.boneIndex0)) total += weight.weight0;
                if (boneIndices.Contains(weight.boneIndex1)) total += weight.weight1;
                if (boneIndices.Contains(weight.boneIndex2)) total += weight.weight2;
                if (boneIndices.Contains(weight.boneIndex3)) total += weight.weight3;
                return total;
            }

            private static int[] MaterialVertices(SkinnedMeshRenderer renderer, string materialToken)
            {
                var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Ispant body mesh is missing.");
                var materialIndex = Array.FindIndex(renderer.sharedMaterials, material =>
                    material != null && material.name.IndexOf(materialToken, StringComparison.OrdinalIgnoreCase) >= 0);
                if (materialIndex < 0 || materialIndex >= mesh.subMeshCount)
                    throw new InvalidOperationException("Ispant back-musket material region is missing: " + materialToken + ".");
                var vertices = mesh.GetTriangles(materialIndex).Distinct().ToArray();
                if (vertices.Length == 0)
                    throw new InvalidOperationException("Ispant back-musket material region is empty.");
                return vertices;
            }
        }

        private readonly struct Pose
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;

            public Pose(Transform item)
            {
                position = item.position;
                rotation = item.rotation;
            }

            public float Error(Transform item)
            {
                return Mathf.Max(Vector3.Distance(position, item.position), Quaternion.Angle(rotation, item.rotation) * Mathf.Deg2Rad);
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform item;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform value)
            {
                item = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            public void Restore()
            {
                if (item == null)
                    return;
                item.localPosition = position;
                item.localRotation = rotation;
                item.localScale = scale;
            }

            public bool Matches(float tolerance)
            {
                return item != null && Vector3.Distance(position, item.localPosition) <= tolerance &&
                    Quaternion.Angle(rotation, item.localRotation) <= tolerance &&
                    Vector3.Distance(scale, item.localScale) <= tolerance;
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
                if (Renderer != null)
                    Renderer.enabled = enabled;
            }
        }
    }
}
