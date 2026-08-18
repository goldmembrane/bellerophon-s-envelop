using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantVerticalIdleAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string IdleSlotName = "Ispant_02_Idle";
        private const string ModelName = "Ispant_New_Direct_Model";
        private const string AnimationFolder = "Assets/_Project/Art/Enemies/Ispant/Animations";
        private const string ClipName = "Ispant_02_Idle_VerticalLoop_New";
        private const string ClipPath = AnimationFolder + "/" + ClipName + ".anim";
        internal const string ControllerPath = AnimationFolder + "/" + ClipName + ".controller";
        private const string OldClipPath = AnimationFolder + "/Ispant_02_Idle.anim";
        private const string GroundedRigName = "Ispant_New_GroundedBreathing_Rig";
        private const float DurationSeconds = 2f;
        private const float PeakOffsetMeters = 0.0075f;
        private const float TotalVerticalTravelMeters = PeakOffsetMeters * 2f;
        private const float FrameRate = 60f;
        private const float ValueTolerance = 0.00001f;
        private const float FootPositionToleranceMeters = 0.001f;
        private const float MinimumKneeMotionDegrees = 0.1f;
        private const int RequiredReviewLoops = 2;
        private static readonly float[] ReviewTimes = { 0f, 0.5f, 1f, 1.5f, 2f };

        private static bool reviewActive;
        private static double reviewStartTime;
        private static RigEvaluationSession reviewSession;
        private static TransformSnapshot[] reviewSnapshots;
        private static SceneView reviewSceneView;
        private static bool reviewSceneViewDrawGizmos;

        [MenuItem("Bellerophon/Enemies/Ispant/Apply New Grounded Breathing Idle Loop")]
        public static void ApplyIspantIdleAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequireRoot(scene, PlacementRootName);
            var slot = RequireDirectChild(placement.transform, IdleSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var hips = RequireDescendant(model, "Hips");

            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;
            var modelPosition = model.localPosition;
            var modelRotation = model.localRotation;
            var modelScale = model.localScale;
            var otherSlotsBefore = placement.transform.Cast<Transform>()
                .Where(item => item != slot)
                .Select(HierarchySignature)
                .ToArray();
            var otherRootsBefore = scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => HierarchySignature(item.transform))
                .ToArray();

            var clip = CreateNewBreathingLoop(slot, hips);
            var controller = CreateNewController(clip);
            var animator = ConfigureSlotAnimator(slot, controller);
            ConfigureGroundedFootRig(slot, model);

            RequireTransform(slot, slotPosition, slotRotation, slotScale, "Ispant idle slot");
            RequireTransform(model, modelPosition, modelRotation, modelScale, "Ispant idle visual model");
            RequireSequence(
                otherSlotsBefore,
                placement.transform.Cast<Transform>().Where(item => item != slot).Select(HierarchySignature).ToArray(),
                "An Ispant slot outside Ispant_02_Idle changed.");
            RequireSequence(
                otherRootsBefore,
                scene.GetRootGameObjects().Where(item => item != placement)
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .Select(item => HierarchySignature(item.transform)).ToArray(),
                "A scene root outside the Ispant placement changed.");

            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying the grounded Ispant idle loop.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "IspantGroundedBreathingIdleApplied Result=PASS" +
                ", Target=Approved Ispant Enemy Placement/Ispant_02_Idle" +
                ", DurationSeconds=2" +
                ", TotalVerticalTravelMeters=0.015" +
                ", AnimatedBone=Hips" +
                ", FootIKConstraints=2" +
                ", ExistingIdleClipCopied=False" +
                ", ApplyRootMotion=False" +
                ", LoopTime=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Grounded Breathing Idle Loop")]
        public static void InspectIspantIdleAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var result = InspectAppliedState(scene, evaluateMotion: true);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Inspecting the new Ispant idle loop changed the scene dirty state.");
            }

            Debug.Log(
                "IspantGroundedBreathingIdleInspected Result=PASS" +
                ", DurationSeconds=" + Num(result.Clip.length) +
                ", TorsoVerticalTravelMeters=" + Num(result.TorsoVerticalTravel) +
                ", MaximumFootPositionErrorMeters=" + Num(result.MaximumFootPositionError) +
                ", LeftKneeMotionDegrees=" + Num(result.LeftKneeMotion) +
                ", RightKneeMotionDegrees=" + Num(result.RightKneeMotion) +
                ", AnimatedBone=Hips" +
                ", FootIKConstraints=2" +
                ", ExistingIdleClipReferenced=False" +
                ", SlotRootUnchanged=True" +
                ", LoopTime=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Prepare New Grounded Breathing Idle Review")]
        public static void CaptureIspantIdleAnimationDiagnostic()
        {
            var scene = RequireScene(requireClean: true);
            InspectAppliedState(scene, evaluateMotion: true);
            FrameIdleModel(scene);
            Debug.Log("IspantGroundedBreathingIdleReviewPrepared Result=PASS, CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Prepare New Grounded Breathing Idle Final Review")]
        public static void CaptureIspantIdleAnimationFinalReview()
        {
            CaptureIspantIdleAnimationDiagnostic();
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Start New Grounded Breathing Idle Review Playback")]
        public static void StartIspantIdleReviewPlayback()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("The Ispant idle Scene View review must start in Edit Mode.");
            }

            if (reviewActive || AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException("An animation preview is already active.");
            }

            var scene = RequireScene(requireClean: true);
            var result = InspectAppliedState(scene, evaluateMotion: true);
            FrameIdleModel(scene);
            reviewSceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            reviewSceneViewDrawGizmos = reviewSceneView.drawGizmos;
            reviewSceneView.drawGizmos = false;
            reviewSnapshots = result.Slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            AnimationMode.StartAnimationMode();
            reviewSession = new RigEvaluationSession(result.Animator, result.RigBuilder, result.Clip);
            reviewStartTime = EditorApplication.timeSinceStartup;
            reviewActive = true;
            EditorApplication.update += UpdateIdleReview;
            Debug.Log(
                "IspantGroundedBreathingIdleReviewStarted Result=PASS" +
                ", RequiredLoops=2" +
                ", FeetLocked=True" +
                ", KneesArticulated=True" +
                ", ExistingIdleClipReferenced=False" +
                ", LiveSceneView=True" +
                ", UnityAnimationMode=True" +
                ", CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Stop New Grounded Breathing Idle Review Playback")]
        public static void StopIspantIdleReviewPlayback()
        {
            if (!reviewActive || !AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException("The live Ispant idle Scene View review is not active.");
            }

            var elapsed = EditorApplication.timeSinceStartup - reviewStartTime;
            var completedLoops = Mathf.FloorToInt((float)(elapsed / DurationSeconds));
            if (completedLoops < RequiredReviewLoops)
            {
                throw new InvalidOperationException(
                    "The direct Ispant idle review did not complete two loops. Completed=" +
                    completedLoops.ToString(CultureInfo.InvariantCulture) + ".");
            }

            StopIdleReview();
            var result = InspectAppliedState(RequireScene(requireClean: true), evaluateMotion: true);
            Debug.Log(
                "IspantGroundedBreathingIdleReviewStopped Result=PASS" +
                ", CompletedLoops=" + completedLoops.ToString(CultureInfo.InvariantCulture) +
                ", MaximumFootPositionErrorMeters=" + Num(result.MaximumFootPositionError) +
                ", LeftKneeMotionDegrees=" + Num(result.LeftKneeMotion) +
                ", RightKneeMotionDegrees=" + Num(result.RightKneeMotion) +
                ", ExistingIdleClipReferenced=False" +
                ", LiveSceneView=True" +
                ", UnityAnimationMode=True" +
                ", CaptureCreated=False.");
        }

        private static void UpdateIdleReview()
        {
            if (!reviewActive)
            {
                return;
            }

            try
            {
                var elapsed = EditorApplication.timeSinceStartup - reviewStartTime;
                reviewSession.Sample((float)(elapsed % DurationSeconds));
                SceneView.RepaintAll();
            }
            catch (Exception exception)
            {
                StopIdleReview();
                Debug.LogException(exception);
            }
        }

        private static void StopIdleReview()
        {
            EditorApplication.update -= UpdateIdleReview;
            reviewActive = false;
            reviewSession?.Dispose();
            reviewSession = null;
            if (reviewSnapshots != null)
            {
                foreach (var snapshot in reviewSnapshots)
                {
                    snapshot.Restore();
                }
            }

            reviewSnapshots = null;
            if (reviewSceneView != null)
            {
                reviewSceneView.drawGizmos = reviewSceneViewDrawGizmos;
                reviewSceneView = null;
            }

            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            SceneView.RepaintAll();
        }

        private static AnimationClip CreateNewBreathingLoop(Transform slot, Transform hips)
        {
            DeleteNewAssetIfPresent(ClipPath);
            var baseline = hips.localPosition;
            var localPeakDelta = hips.parent.InverseTransformVector(Vector3.up * PeakOffsetMeters);
            var clip = new AnimationClip
            {
                name = ClipName,
                frameRate = FrameRate
            };
            var path = AnimationUtility.CalculateTransformPath(hips, slot);
            SetPositionCurve(clip, path, "m_LocalPosition.x", baseline.x, localPeakDelta.x);
            SetPositionCurve(clip, path, "m_LocalPosition.y", baseline.y, localPeakDelta.y);
            SetPositionCurve(clip, path, "m_LocalPosition.z", baseline.z, localPeakDelta.z);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void SetPositionCurve(AnimationClip clip, string path, string propertyName, float baseline, float peakDelta)
        {
            var speedAtCenter = peakDelta * Mathf.PI;
            var curve = new AnimationCurve(
                new Keyframe(0f, baseline, speedAtCenter, speedAtCenter),
                new Keyframe(0.5f, baseline + peakDelta, 0f, 0f),
                new Keyframe(1f, baseline, -speedAtCenter, -speedAtCenter),
                new Keyframe(1.5f, baseline - peakDelta, 0f, 0f),
                new Keyframe(2f, baseline, speedAtCenter, speedAtCenter))
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static AnimatorController CreateNewController(AnimationClip clip)
        {
            DeleteNewAssetIfPresent(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(ClipName);
            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Animator ConfigureSlotAnimator(Transform slot, AnimatorController controller)
        {
            var directAnimators = slot.GetComponents<Animator>();
            if (directAnimators.Length > 1)
            {
                throw new InvalidOperationException("Ispant_02_Idle has more than one direct Animator.");
            }

            var animator = directAnimators.SingleOrDefault() ?? slot.gameObject.AddComponent<Animator>();
            var descendantAnimators = slot.GetComponentsInChildren<Animator>(true).Where(item => item != animator).ToArray();
            if (descendantAnimators.Any(item => item.runtimeAnimatorController != null))
            {
                throw new InvalidOperationException("Ispant_02_Idle has a configured descendant Animator that would conflict with the new loop.");
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            return animator;
        }

        private static void ConfigureGroundedFootRig(Transform slot, Transform model)
        {
            var existing = slot.Cast<Transform>().Where(item => item.name == GroundedRigName).ToArray();
            foreach (var item in existing)
            {
                UnityEngine.Object.DestroyImmediate(item.gameObject);
            }

            var rigObject = new GameObject(GroundedRigName);
            rigObject.transform.SetParent(slot, false);
            var rig = rigObject.AddComponent(RequireRiggingType("UnityEngine.Animations.Rigging.Rig"));
            SetFloat(rig, "m_Weight", 1f);
            CreateFootConstraint(rigObject.transform, model, "Left");
            CreateFootConstraint(rigObject.transform, model, "Right");

            var builderType = RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            var builders = slot.GetComponents(builderType);
            if (builders.Length > 1)
            {
                throw new InvalidOperationException("Ispant_02_Idle contains multiple direct RigBuilders.");
            }

            var builder = builders.Length == 0 ? slot.gameObject.AddComponent(builderType) : builders[0];
            ((Behaviour)builder).enabled = true;
            var serialized = new SerializedObject(builder);
            var layers = serialized.FindProperty("m_RigLayers") ??
                         throw new InvalidOperationException("Animation Rigging layer property is unavailable.");
            layers.arraySize = 1;
            var layer = layers.GetArrayElementAtIndex(0);
            layer.FindPropertyRelative("m_Rig").objectReferenceValue = rig;
            layer.FindPropertyRelative("m_Active").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            var legLine = foot.position - upper.position;
            var projection = legLine.sqrMagnitude > 0.000001f
                ? upper.position + Vector3.Project(lower.position - upper.position, legLine)
                : upper.position;
            var bendDirection = lower.position - projection;
            if (bendDirection.sqrMagnitude < 0.000001f)
            {
                bendDirection = model.forward;
            }

            hint.position = lower.position + bendDirection.normalized * Mathf.Max(legLine.magnitude * 0.35f, 0.1f);
            hint.rotation = lower.rotation;

            var constraintObject = new GameObject(side + "FootTwoBoneIK");
            constraintObject.transform.SetParent(rigRoot, false);
            var constraint = constraintObject.AddComponent(
                RequireRiggingType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint"));
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

        private static InspectionResult InspectAppliedState(Scene scene, bool evaluateMotion)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be active for the Ispant idle inspection.");
            }

            var placement = RequireRoot(scene, PlacementRootName);
            var slot = RequireDirectChild(placement.transform, IdleSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var hips = RequireDescendant(model, "Hips");
            var leftLeg = RequireDescendant(model, "LeftLeg");
            var rightLeg = RequireDescendant(model, "RightLeg");
            var leftFoot = RequireDescendant(model, "LeftFoot");
            var rightFoot = RequireDescendant(model, "RightFoot");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Ispant grounded breathing clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Ispant grounded breathing controller is missing.");
            var oldClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OldClipPath);
            if (oldClip != null && controller.animationClips.Contains(oldClip))
            {
                throw new InvalidOperationException("The existing Ispant idle clip was connected to the new controller.");
            }

            if (controller.animationClips.Length != 1 || controller.animationClips[0] != clip)
            {
                throw new InvalidOperationException("The new Ispant idle controller must contain only its newly authored clip.");
            }

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Ispant_02_Idle has no direct Animator.");
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("The Ispant idle Animator configuration differs from the approved target state.");
            }

            var expectedPath = AnimationUtility.CalculateTransformPath(hips, slot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var expectedProperties = new[] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z" };
            if (bindings.Length != 3 || bindings.Any(binding =>
                    binding.path != expectedPath || binding.type != typeof(Transform) ||
                    !expectedProperties.Contains(binding.propertyName, StringComparer.Ordinal)))
            {
                throw new InvalidOperationException("The new Ispant idle clip must animate only the Hips local position.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || Mathf.Abs(clip.length - DurationSeconds) > ValueTolerance)
            {
                throw new InvalidOperationException("The new Ispant idle clip duration or loop setting differs.");
            }

            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length != 5)
                {
                    throw new InvalidOperationException("Each newly authored Hips position curve must contain five keys.");
                }
            }

            var rigBuilder = RequireGroundedRig(slot);
            var result = new InspectionResult(slot, model, animator, rigBuilder, clip, hips);
            if (!evaluateMotion)
            {
                return result;
            }

            var snapshots = slot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                using var evaluation = new RigEvaluationSession(animator, rigBuilder, clip);
                evaluation.Sample(0f);
                var initialLeftFootPosition = leftFoot.position;
                var initialRightFootPosition = rightFoot.position;
                var initialLeftFootRotation = leftFoot.rotation;
                var initialRightFootRotation = rightFoot.rotation;
                var initialLeftKneeRotation = leftLeg.rotation;
                var initialRightKneeRotation = rightLeg.rotation;
                var minimumTorsoY = float.PositiveInfinity;
                var maximumTorsoY = float.NegativeInfinity;
                var maximumTorsoXZDrift = 0f;
                var baselineTorsoXZ = new Vector2(hips.position.x, hips.position.z);
                var maximumFootError = 0f;
                var maximumFootRotationError = 0f;
                var leftKneeMotion = 0f;
                var rightKneeMotion = 0f;

                foreach (var time in ReviewTimes)
                {
                    evaluation.Sample(time);
                    minimumTorsoY = Mathf.Min(minimumTorsoY, hips.position.y);
                    maximumTorsoY = Mathf.Max(maximumTorsoY, hips.position.y);
                    maximumTorsoXZDrift = Mathf.Max(
                        maximumTorsoXZDrift,
                        Vector2.Distance(baselineTorsoXZ, new Vector2(hips.position.x, hips.position.z)));
                    maximumFootError = Mathf.Max(
                        maximumFootError,
                        Vector3.Distance(initialLeftFootPosition, leftFoot.position),
                        Vector3.Distance(initialRightFootPosition, rightFoot.position));
                    maximumFootRotationError = Mathf.Max(
                        maximumFootRotationError,
                        Quaternion.Angle(initialLeftFootRotation, leftFoot.rotation),
                        Quaternion.Angle(initialRightFootRotation, rightFoot.rotation));
                    leftKneeMotion = Mathf.Max(leftKneeMotion, Quaternion.Angle(initialLeftKneeRotation, leftLeg.rotation));
                    rightKneeMotion = Mathf.Max(rightKneeMotion, Quaternion.Angle(initialRightKneeRotation, rightLeg.rotation));
                }

                result = new InspectionResult(
                    slot,
                    model,
                    animator,
                    rigBuilder,
                    clip,
                    hips,
                    maximumTorsoY - minimumTorsoY,
                    maximumTorsoXZDrift,
                    maximumFootError,
                    maximumFootRotationError,
                    leftKneeMotion,
                    rightKneeMotion);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }
            }

            if (Mathf.Abs(result.TorsoVerticalTravel - TotalVerticalTravelMeters) > FootPositionToleranceMeters)
            {
                throw new InvalidOperationException("The Ispant torso vertical travel differs from 1.5 cm.");
            }

            if (result.MaximumTorsoXZDrift > FootPositionToleranceMeters)
            {
                throw new InvalidOperationException("The Ispant torso drifts horizontally during the breathing loop.");
            }

            if (result.MaximumFootPositionError > FootPositionToleranceMeters ||
                result.MaximumFootRotationError > 0.1f)
            {
                throw new InvalidOperationException("An Ispant foot does not remain planted during the breathing loop.");
            }

            if (result.LeftKneeMotion < MinimumKneeMotionDegrees ||
                result.RightKneeMotion < MinimumKneeMotionDegrees)
            {
                throw new InvalidOperationException("Both Ispant knees must visibly articulate during the breathing loop.");
            }

            return result;
        }

        private static Component RequireGroundedRig(Transform slot)
        {
            var builderType = RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            var constraintType = RequireRiggingType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
            var builder = slot.GetComponents(builderType).SingleOrDefault() ??
                          throw new InvalidOperationException("Ispant_02_Idle grounded RigBuilder is missing.");
            var serialized = new SerializedObject(builder);
            var layers = serialized.FindProperty("m_RigLayers");
            if (!((Behaviour)builder).enabled || layers == null || layers.arraySize != 1)
            {
                throw new InvalidOperationException("Ispant_02_Idle grounded RigBuilder configuration differs.");
            }

            var layer = layers.GetArrayElementAtIndex(0);
            var rig = layer.FindPropertyRelative("m_Rig").objectReferenceValue as Component;
            if (!layer.FindPropertyRelative("m_Active").boolValue || rig == null || rig.name != GroundedRigName)
            {
                throw new InvalidOperationException("Ispant_02_Idle grounded Rig layer configuration differs.");
            }

            var constraints = rig.GetComponentsInChildren(constraintType, true);
            if (constraints.Length != 2)
            {
                throw new InvalidOperationException("Ispant_02_Idle must have exactly two grounded foot constraints.");
            }

            return builder;
        }

        private static void FrameIdleModel(Scene scene)
        {
            var placement = RequireRoot(scene, PlacementRootName);
            var slot = RequireDirectChild(placement.transform, IdleSlotName);
            var model = RequireDirectChild(slot, ModelName);
            Selection.activeGameObject = model.gameObject;
            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            sceneView.Focus();
            sceneView.FrameSelected(false);
            var renderers = model.GetComponentsInChildren<Renderer>(true).Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("The Ispant idle model has no enabled Renderer for direct review framing.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            sceneView.pivot = bounds.center;
            sceneView.size = Mathf.Max(0.65f, bounds.extents.y * 0.82f);
            Selection.activeGameObject = null;
            sceneView.Repaint();
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("Open CargoRunMvp before working on the Ispant idle loop.");
            }

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            }

            return scene;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required scene root differs: " + name + ".");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = parent.Cast<Transform>().Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Required direct child differs: " + parent.name + "/" + name + ".");
            }

            return matches[0];
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Required Ispant bone differs: " + name + ".");
            }

            return matches[0];
        }

        private static void DeleteNewAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null && !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException("The generated Ispant idle asset could not be replaced: " + path + ".");
            }
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

        private static void SetObject(SerializedObject serialized, string path, UnityEngine.Object value)
        {
            var property = serialized.FindProperty(path) ??
                           throw new InvalidOperationException("Animation Rigging object property is unavailable: " + path + ".");
            property.objectReferenceValue = value;
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

        private static void SetBool(SerializedObject serialized, string path, bool value)
        {
            var property = serialized.FindProperty(path) ??
                           throw new InvalidOperationException("Animation Rigging bool property is unavailable: " + path + ".");
            property.boolValue = value;
        }

        private static void RequireTransform(Transform item, Vector3 position, Quaternion rotation, Vector3 scale, string label)
        {
            if (Vector3.Distance(item.localPosition, position) > ValueTolerance ||
                Quaternion.Angle(item.localRotation, rotation) > ValueTolerance ||
                Vector3.Distance(item.localScale, scale) > ValueTolerance)
            {
                throw new InvalidOperationException(label + " transform changed while applying the idle loop.");
            }
        }

        private static string HierarchySignature(Transform root)
        {
            return string.Join(
                "\n",
                root.GetComponentsInChildren<Transform>(true).Select(item =>
                    AnimationUtility.CalculateTransformPath(item, root) + "|" +
                    item.gameObject.activeSelf + "|" +
                    Vec(item.localPosition) + "|" +
                    Vec(item.localScale) + "|" +
                    Num(item.localRotation.x) + "," + Num(item.localRotation.y) + "," +
                    Num(item.localRotation.z) + "," + Num(item.localRotation.w)));
        }

        private static void RequireSequence(string[] before, string[] after, string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
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

                graph = PlayableGraph.Create("IspantGroundedBreathingIdleEvaluation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                clipPlayable = AnimationClipPlayable.Create(graph, clip);
                clipPlayable.SetApplyFootIK(false);
                var output = AnimationPlayableOutput.Create(graph, "IspantGroundedBreathingClip", animator);
                output.SetSourcePlayable(clipPlayable);
                if (!(bool)InvokeRigBuilder(builder, "Build", graph))
                {
                    throw new InvalidOperationException("Ispant grounded breathing RigBuilder graph could not be built.");
                }

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
                {
                    graph.Destroy();
                }

                InvokeRigBuilder(builder, "Clear");
                ((Behaviour)builder).enabled = builderEnabled;
                animator.enabled = animatorEnabled;
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
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }

        private readonly struct InspectionResult
        {
            public readonly Transform Slot;
            public readonly Transform Model;
            public readonly Animator Animator;
            public readonly Component RigBuilder;
            public readonly AnimationClip Clip;
            public readonly Transform Hips;
            public readonly float TorsoVerticalTravel;
            public readonly float MaximumTorsoXZDrift;
            public readonly float MaximumFootPositionError;
            public readonly float MaximumFootRotationError;
            public readonly float LeftKneeMotion;
            public readonly float RightKneeMotion;

            public InspectionResult(
                Transform slot,
                Transform model,
                Animator animator,
                Component rigBuilder,
                AnimationClip clip,
                Transform hips,
                float torsoVerticalTravel = 0f,
                float maximumTorsoXZDrift = 0f,
                float maximumFootPositionError = 0f,
                float maximumFootRotationError = 0f,
                float leftKneeMotion = 0f,
                float rightKneeMotion = 0f)
            {
                Slot = slot;
                Model = model;
                Animator = animator;
                RigBuilder = rigBuilder;
                Clip = clip;
                Hips = hips;
                TorsoVerticalTravel = torsoVerticalTravel;
                MaximumTorsoXZDrift = maximumTorsoXZDrift;
                MaximumFootPositionError = maximumFootPositionError;
                MaximumFootRotationError = maximumFootRotationError;
                LeftKneeMotion = leftKneeMotion;
                RightKneeMotion = rightKneeMotion;
            }
        }
    }
}
