using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantNewWalkingAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string SlotName = "Ispant_03_Move";
        private const string ModelName = "Ispant_New_Direct_Model";
        private const string SourcePath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_Walking_Source.fbx";
        private const string ModelPath = "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string ClipPath = "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_New_Walking_InPlace.anim";
        private const string ControllerFolder = "Assets/_Project/Art/Enemies/Ispant/Controllers";
        private const string ControllerPath = "Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_New_Walking.controller";
        private const string ImportedClipName = "Ispant_New_Walking_Mixamo";
        private const string ClipName = "Ispant_New_Walking_InPlace";
        private const string SourceHash = "7132D83B27CD5C0C11D6D7F014F3138473312D5BC7645623C2EB86A6788B1C5A";
        private const string ModelHash = "5CE54F6117AF08F141BC18A0E46C823AD07877D815DA2906D59CA2967A4974FF";
        private const float Tolerance = 0.0001f;
        private const float SwordFollowTolerance = 0.001f;
        private const int RequiredLoops = 2;

        // Source-local X/Y are locomotion axes; Z is the walking cycle's vertical bounce.
        private static readonly string[] FlattenedProperties = { "m_LocalPosition.x", "m_LocalPosition.y" };
        private static bool reviewActive;
        private static double reviewStart;
        private static TransformSnapshot[] reviewSnapshots;
        private static SceneView reviewView;
        private static bool reviewGizmos;

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Mixamo Walking Source")]
        public static void InspectIspantNewWalkingSource()
        {
            RequireHashes();
            var importer = RequireImporter();
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var imported = ImportedClips();
            var mixamoTakes = defaults.Where(IsMixamo).ToArray();
            var mixamoClips = imported.Where(item => item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            var sourceBones = BoneDescriptions(RequireAsset<GameObject>(SourcePath).transform);
            var targetBones = BoneDescriptions(RequireAsset<GameObject>(ModelPath).transform);
            var target = RequireTarget(RequireScene(true));
            Debug.Log(
                "IspantNewWalkingSourceInspected Result=PASS" +
                ", AnimationType=" + importer.animationType +
                ", ImportAnimation=" + importer.importAnimation +
                ", DefaultClipCount=" + defaults.Length +
                ", DefaultClips=" + string.Join("|", defaults.Select(DescribeClip)) +
                ", ImportedClipCount=" + imported.Length +
                ", ImportedClips=" + string.Join("|", imported.Select(item => item.name + "[Length=" + Num(item.length) + ",Fps=" + Num(item.frameRate) + "]")) +
                ", MixamoTakeCount=" + mixamoTakes.Length +
                ", MixamoTakes=" + string.Join("|", mixamoTakes.Select(DescribeClip)) +
                ", MixamoPositionCurves=" + (mixamoClips.Length == 1 ? PositionCurves(mixamoClips[0]) : "<ambiguous>") +
                ", ExactBoneHierarchyMatch=" + sourceBones.SequenceEqual(targetBones, StringComparer.Ordinal) +
                ", TargetAnimatorCount=" + target.Model.GetComponentsInChildren<Animator>(true).Length +
                ", TargetRenderers=" + RendererDescription(target.Model) +
                ", SceneDirty=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply New Mixamo Walking Animation")]
        public static void ApplyIspantNewWalkingAnimation()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            var source = RequireImportedClip();
            var clip = CreateInPlaceClip(source);
            var controller = CreateController(clip);
            var scene = RequireScene(true);
            var target = RequireTarget(scene);
            var slotBefore = new TransformSnapshot(target.Slot);
            var modelBefore = new TransformSnapshot(target.Model);
            var othersBefore = OtherSlotSignatures(target.Placement, target.Slot);
            var rootsBefore = OtherRootSignatures(scene, target.Placement);
            var appearanceBefore = AppearanceSignature(target.Model);

            ConfigureAnimator(target.Model, controller);

            if (!slotBefore.Matches(Tolerance) || !modelBefore.Matches(Tolerance))
                throw new InvalidOperationException("The move slot or direct model transform changed.");
            RequireSame(othersBefore, OtherSlotSignatures(target.Placement, target.Slot), "Another Ispant slot changed.");
            RequireSame(rootsBefore, OtherRootSignatures(scene, target.Placement), "A scene root outside the Ispant placement changed.");
            RequireSame(appearanceBefore, AppearanceSignature(target.Model), "The move model appearance changed.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = target.Slot.gameObject;
            Debug.Log(
                "IspantNewWalkingAnimationApplied Result=PASS" +
                ", Target=" + PlacementName + "/" + SlotName + "/" + ModelName +
                ", SourceClip=" + SourcePath + "/" + ImportedClipName +
                ", AppliedClip=" + ClipPath +
                ", Controller=" + ControllerPath +
                ", Length=" + Num(clip.length) + ", FrameRate=" + Num(clip.frameRate) +
                ", Loop=True, RootMotion=False, InPlaceAxes=LocalX+LocalY" +
                ", SwordFollow=BakedHipsTransformCurves" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, AppearanceChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect New Mixamo Walking Animation")]
        public static void InspectIspantNewWalkingAnimation()
        {
            var result = InspectApplied(RequireScene(true), true);
            Debug.Log(
                "IspantNewWalkingAnimationInspected Result=PASS" +
                ", Length=" + Num(result.Clip.length) + ", FrameRate=" + Num(result.Clip.frameRate) +
                ", Loop=True, RootMotion=False" +
                ", HipHorizontalRange=" + Num(result.HipHorizontalRange) +
                ", HipVerticalRange=" + Num(result.HipVerticalRange) +
                ", MaximumFootTravel=" + Num(result.MaximumFootTravel) +
                ", SwordPositionError=" + Num(result.SwordPositionError) +
                ", SwordAngleError=" + Num(result.SwordAngleError) +
                ", RendererCount=" + result.RendererCount + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Start New Mixamo Walking Review")]
        public static void StartIspantNewWalkingReviewPlayback()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || reviewActive || AnimationMode.InAnimationMode())
                throw new InvalidOperationException("The walking review requires an idle Edit Mode AnimationMode state.");
            var result = InspectApplied(RequireScene(true), false);
            reviewSnapshots = result.Target.Model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            reviewView = SceneView.lastActiveSceneView;
            if (reviewView != null)
            {
                reviewGizmos = reviewView.drawGizmos;
                reviewView.drawGizmos = false;
                Selection.activeGameObject = result.Target.Slot.gameObject;
                reviewView.FrameSelected();
            }
            AnimationMode.StartAnimationMode();
            reviewStart = EditorApplication.timeSinceStartup;
            reviewActive = true;
            EditorApplication.update += UpdateReview;
            Debug.Log("IspantNewWalkingReviewStarted Result=PASS, RequiredLoops=2, LiveSceneView=True, CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Stop New Mixamo Walking Review")]
        public static void StopIspantNewWalkingReviewPlayback()
        {
            if (!reviewActive || !AnimationMode.InAnimationMode())
                throw new InvalidOperationException("The walking review is not active.");
            var clip = RequireAsset<AnimationClip>(ClipPath);
            var loops = Mathf.FloorToInt((float)((EditorApplication.timeSinceStartup - reviewStart) / clip.length));
            if (loops < RequiredLoops)
                throw new InvalidOperationException("The walking review has not completed two loops. Completed=" + loops + ".");
            StopReview();
            var result = InspectApplied(RequireScene(true), true);
            Debug.Log(
                "IspantNewWalkingReviewStopped Result=PASS, CompletedLoops=" + loops +
                ", SwordPositionError=" + Num(result.SwordPositionError) +
                ", SwordAngleError=" + Num(result.SwordAngleError) +
                ", SceneRestored=True, CaptureCreated=False.");
        }

        private static void ConfigureImporter()
        {
            var importer = RequireImporter();
            var mixamo = (importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>()).Where(IsMixamo).ToArray();
            if (mixamo.Length != 1)
                throw new InvalidOperationException("Exactly one Mixamo take is required. Count=" + mixamo.Length + ".");
            if (!BoneDescriptions(RequireAsset<GameObject>(SourcePath).transform)
                .SequenceEqual(BoneDescriptions(RequireAsset<GameObject>(ModelPath).transform), StringComparer.Ordinal))
                throw new InvalidOperationException("The source and target bone hierarchies differ.");
            var selected = mixamo[0];
            selected.name = ImportedClipName;
            selected.loopTime = true;
            selected.loopPose = true;
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

        private static AnimationClip CreateInPlaceClip(AnimationClip source)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = ClipName };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            var flattened = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (binding.path == "Armature/Hips" && FlattenedProperties.Contains(binding.propertyName, StringComparer.Ordinal))
                {
                    curve = AnimationCurve.Constant(0f, source.length, curve.keys[0].value);
                    flattened++;
                }
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            if (flattened != 2)
                throw new InvalidOperationException("The two expected Mixamo locomotion curves were not found. Count=" + flattened + ".");
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, AnimationUtility.GetObjectReferenceCurve(source, binding));
            BakeSwordFollowCurves(clip);
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (!AssetDatabase.IsValidFolder(ControllerFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/Enemies/Ispant", "Controllers");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (var transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);
            var state = machine.AddState("Ispant_New_Walking");
            state.motion = clip;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureAnimator(Transform model, AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1 || (animators.Length == 1 && animators[0].transform != model))
                throw new InvalidOperationException("The move model has a conflicting Animator hierarchy.");
            var animator = animators.SingleOrDefault() ?? model.gameObject.AddComponent<Animator>();
            animator.avatar = null;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
        }

        private static void BakeSwordFollowCurves(AnimationClip clip)
        {
            var prefab = RequireAsset<GameObject>(ModelPath);
            var hips = RequireDescendant(prefab.transform, "Hips");
            var sword = RequireDescendant(prefab.transform, "Ispant_Approved_LongSword_10K");
            var offsetPosition = Quaternion.Inverse(hips.localRotation) * (sword.localPosition - hips.localPosition);
            var offsetRotation = Quaternion.Inverse(hips.localRotation) * sword.localRotation;
            var px = RequireCurve(clip, "Armature/Hips", "m_LocalPosition.x");
            var py = RequireCurve(clip, "Armature/Hips", "m_LocalPosition.y");
            var pz = RequireCurve(clip, "Armature/Hips", "m_LocalPosition.z");
            var rx = RequireCurve(clip, "Armature/Hips", "m_LocalRotation.x");
            var ry = RequireCurve(clip, "Armature/Hips", "m_LocalRotation.y");
            var rz = RequireCurve(clip, "Armature/Hips", "m_LocalRotation.z");
            var rw = RequireCurve(clip, "Armature/Hips", "m_LocalRotation.w");
            var frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
            var positions = new Keyframe[frameCount + 1][];
            var rotations = new Keyframe[frameCount + 1][];
            for (var frame = 0; frame <= frameCount; frame++)
            {
                var time = Mathf.Min(clip.length, frame / clip.frameRate);
                var hipsPosition = new Vector3(px.Evaluate(time), py.Evaluate(time), pz.Evaluate(time));
                var hipsRotation = new Quaternion(rx.Evaluate(time), ry.Evaluate(time), rz.Evaluate(time), rw.Evaluate(time)).normalized;
                var position = hipsPosition + hipsRotation * offsetPosition;
                var rotation = hipsRotation * offsetRotation;
                positions[frame] = new[] { new Keyframe(time, position.x), new Keyframe(time, position.y), new Keyframe(time, position.z) };
                rotations[frame] = new[] { new Keyframe(time, rotation.x), new Keyframe(time, rotation.y), new Keyframe(time, rotation.z), new Keyframe(time, rotation.w) };
            }
            var swordPath = "Armature/Ispant_Approved_LongSword_10K";
            for (var axis = 0; axis < 3; axis++)
                SetLinearCurve(clip, swordPath, "m_LocalPosition." + "xyz"[axis], positions.Select(item => item[axis]).ToArray());
            for (var axis = 0; axis < 4; axis++)
                SetLinearCurve(clip, swordPath, "m_LocalRotation." + "xyzw"[axis], rotations.Select(item => item[axis]).ToArray());
        }

        private static Inspection InspectApplied(Scene scene, bool sample)
        {
            RequireHashes();
            var target = RequireTarget(scene);
            var clip = RequireAsset<AnimationClip>(ClipPath);
            var controller = RequireAsset<AnimatorController>(ControllerPath);
            var animator = target.Model.GetComponent<Animator>() ?? throw new InvalidOperationException("The move model Animator is missing.");
            if (animator.runtimeAnimatorController != controller || animator.avatar != null || animator.applyRootMotion || !animator.enabled)
                throw new InvalidOperationException("The move Animator configuration differs.");
            if (controller.animationClips.Length != 1 || controller.animationClips[0] != clip ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The walking controller or loop clip differs.");
            var hips = RequireDescendant(target.Model, "Hips");
            var leftFoot = RequireDescendant(target.Model, "LeftFoot");
            var rightFoot = RequireDescendant(target.Model, "RightFoot");
            var sword = RequireDescendant(target.Model, "Ispant_Approved_LongSword_10K");
            if (sword.GetComponent<ParentConstraint>() != null)
                throw new InvalidOperationException("The walking target must not use a scene-only sword constraint.");
            if (target.Placement.childCount != 12 || target.Model.GetComponentsInChildren<Renderer>(true).Length != 2)
                throw new InvalidOperationException("The target slot or renderer count differs.");

            var horizontal = 0f;
            var vertical = 0f;
            var feet = 0f;
            var swordPosition = 0f;
            var swordAngle = 0f;
            if (sample)
            {
                var snapshots = target.Model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
                var hipsValues = new Vector3[9];
                var leftValues = new Vector3[9];
                var rightValues = new Vector3[9];
                var swordOffset = Quaternion.Inverse(hips.rotation) * (sword.position - hips.position);
                var swordRotation = Quaternion.Inverse(hips.rotation) * sword.rotation;
                var alreadySampling = AnimationMode.InAnimationMode();
                if (!alreadySampling)
                    AnimationMode.StartAnimationMode();
                try
                {
                    for (var i = 0; i < 9; i++)
                    {
                        AnimationMode.SampleAnimationClip(target.Model.gameObject, clip, clip.length * i / 8f);
                        hipsValues[i] = hips.localPosition;
                        leftValues[i] = leftFoot.position;
                        rightValues[i] = rightFoot.position;
                        swordPosition = Mathf.Max(swordPosition, Vector3.Distance(Quaternion.Inverse(hips.rotation) * (sword.position - hips.position), swordOffset));
                        swordAngle = Mathf.Max(swordAngle, Quaternion.Angle(Quaternion.Inverse(hips.rotation) * sword.rotation, swordRotation));
                    }
                }
                finally
                {
                    if (!alreadySampling && AnimationMode.InAnimationMode())
                        AnimationMode.StopAnimationMode();
                    foreach (var snapshot in snapshots)
                        snapshot.Restore();
                }
                horizontal = Range(hipsValues, 0) + Range(hipsValues, 1);
                vertical = Range(hipsValues, 2);
                feet = Mathf.Max(MaxTravel(leftValues), MaxTravel(rightValues));
                if (horizontal > Tolerance)
                    throw new InvalidOperationException("The in-place hip range is too large: " + horizontal + ".");
                if (vertical <= Tolerance || feet <= Tolerance)
                    throw new InvalidOperationException("The Mixamo hips and feet do not animate.");
                if (swordPosition > SwordFollowTolerance || swordAngle > 0.01f)
                    throw new InvalidOperationException(
                        "The approved sword separates from the Hips. PositionError=" + Num(swordPosition) +
                        ", AngleError=" + Num(swordAngle) + ".");
            }
            if (scene.isDirty)
                throw new InvalidOperationException("Inspection changed the scene dirty state.");
            return new Inspection(target, clip, horizontal, vertical, feet, swordPosition, swordAngle,
                target.Model.GetComponentsInChildren<Renderer>(true).Length);
        }

        private static void UpdateReview()
        {
            if (!reviewActive) return;
            try
            {
                var target = RequireTarget(RequireScene(true));
                var clip = RequireAsset<AnimationClip>(ClipPath);
                AnimationMode.SampleAnimationClip(target.Model.gameObject, clip,
                    (float)((EditorApplication.timeSinceStartup - reviewStart) % clip.length));
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
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            if (reviewSnapshots != null)
            {
                foreach (var snapshot in reviewSnapshots) snapshot.Restore();
                reviewSnapshots = null;
            }
            if (reviewView != null)
            {
                reviewView.drawGizmos = reviewGizmos;
                reviewView = null;
            }
            SceneView.RepaintAll();
        }

        private static Target RequireTarget(Scene scene)
        {
            var roots = scene.GetRootGameObjects().Where(item => item.name == PlacementName).ToArray();
            if (roots.Length != 1) throw new InvalidOperationException("The Ispant placement root count differs.");
            var placement = roots[0].transform;
            if (placement.childCount != 12) throw new InvalidOperationException("The Ispant placement slot count differs.");
            var slot = placement.Find(SlotName) ?? throw new InvalidOperationException("Ispant_03_Move is missing.");
            if (slot.parent != placement || slot.childCount != 1) throw new InvalidOperationException("Ispant_03_Move hierarchy differs.");
            var model = slot.GetChild(0);
            if (model.name != ModelName) throw new InvalidOperationException("The direct move model is missing.");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
            if (source == null || AssetDatabase.GetAssetPath(source) != ModelPath)
                throw new InvalidOperationException("The move model no longer references the direct FBX.");
            return new Target(placement, slot, model);
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) throw new InvalidOperationException("CargoRunMvp must be active.");
            if (clean && scene.isDirty) throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static ModelImporter RequireImporter() => AssetImporter.GetAtPath(SourcePath) as ModelImporter ?? throw new InvalidOperationException("The walking importer is missing.");
        private static AnimationClip[] ImportedClips() => AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal)).OrderBy(item => item.name, StringComparer.Ordinal).ToArray();
        private static AnimationClip RequireImportedClip()
        {
            var clips = ImportedClips();
            if (clips.Length != 1 || clips[0].name != ImportedClipName) throw new InvalidOperationException("The selected Mixamo clip differs.");
            return clips[0];
        }
        private static T RequireAsset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Required asset missing: " + path + ".");
        private static bool IsMixamo(ModelImporterClipAnimation clip) => clip.takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) >= 0;
        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one " + name + ". Count=" + matches.Length + ".");
            return matches[0];
        }
        private static string[] BoneDescriptions(Transform root)
        {
            var armature = root.Cast<Transform>().Single(item => item.name == "Armature");
            return armature.GetComponentsInChildren<Transform>(true).Where(item => item != armature && item.GetComponent<Renderer>() == null)
                .Select(item => AnimationUtility.CalculateTransformPath(item, armature) + "<-" + (item.parent == armature ? "Armature" : item.parent.name))
                .OrderBy(item => item, StringComparer.Ordinal).ToArray();
        }
        private static string DescribeClip(ModelImporterClipAnimation clip) => clip.name + "@" + clip.takeName + "[" + Num(clip.firstFrame) + "-" + Num(clip.lastFrame) + "]";
        private static string PositionCurves(AnimationClip clip) => string.Join("|", AnimationUtility.GetCurveBindings(clip)
            .Where(item => item.propertyName.Contains("m_LocalPosition")).OrderBy(item => item.path).ThenBy(item => item.propertyName)
            .Select(item => { var values = AnimationUtility.GetEditorCurve(clip, item).keys.Select(key => key.value).ToArray(); return item.path + ":" + item.propertyName + "[" + Num(values.Min()) + ".." + Num(values.Max()) + "]"; }));
        private static string RendererDescription(Transform model) => string.Join("|", model.GetComponentsInChildren<Renderer>(true).OrderBy(item => item.name).Select(item => item.name + ":" + item.GetType().Name));
        private static AnimationCurve RequireCurve(AnimationClip clip, string path, string property)
        {
            var binding = AnimationUtility.GetCurveBindings(clip).SingleOrDefault(item => item.path == path && item.propertyName == property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve ?? throw new InvalidOperationException("Required Mixamo curve missing: " + path + "/" + property + ".");
        }
        private static void SetLinearCurve(AnimationClip clip, string path, string property, Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }
        private static string AppearanceSignature(Transform model) => string.Join("|", model.GetComponentsInChildren<Renderer>(true).OrderBy(item => item.name).Select(item => item.name + ":" + item.GetType().FullName + ":" + item.enabled + ":" + string.Join("+", item.sharedMaterials.Select(AssetDatabase.GetAssetPath))));
        private static string OtherSlotSignatures(Transform placement, Transform excluded) => string.Join("|", placement.Cast<Transform>().Where(item => item != excluded).OrderBy(item => item.name).Select(TransformSignature));
        private static string OtherRootSignatures(Scene scene, Transform excluded) => string.Join("|", scene.GetRootGameObjects().Where(item => item.transform != excluded).OrderBy(item => item.name).Select(item => TransformSignature(item.transform)));
        private static string TransformSignature(Transform value) => value.name + ":" + Vec(value.localPosition) + ":" + Quat(value.localRotation) + ":" + Vec(value.localScale) + ":" + value.childCount;
        private static void RequireSame(string before, string after, string message) { if (!string.Equals(before, after, StringComparison.Ordinal)) throw new InvalidOperationException(message); }
        private static void RequireHashes() { RequireHash(SourcePath, SourceHash); RequireHash(ModelPath, ModelHash); }
        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Path.GetFullPath(path));
            using var sha = SHA256.Create();
            var actual = string.Concat(sha.ComputeHash(stream).Select(item => item.ToString("X2", CultureInfo.InvariantCulture)));
            if (actual != expected) throw new InvalidOperationException("Asset hash differs: " + path + ".");
        }
        private static float Range(Vector3[] values, int axis) => values.Max(value => value[axis]) - values.Min(value => value[axis]);
        private static float MaxTravel(Vector3[] values)
        {
            var max = 0f;
            for (var i = 0; i < values.Length; i++) for (var j = i + 1; j < values.Length; j++) max = Mathf.Max(max, Vector3.Distance(values[i], values[j]));
            return max;
        }
        private static string Num(float value) => value.ToString("0.#########", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) => Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) => Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct Target
        {
            public readonly Transform Placement, Slot, Model;
            public Target(Transform placement, Transform slot, Transform model) { Placement = placement; Slot = slot; Model = model; }
        }
        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position, scale;
            private readonly Quaternion rotation;
            public TransformSnapshot(Transform value) { target = value; position = value.localPosition; rotation = value.localRotation; scale = value.localScale; }
            public bool Matches(float tolerance) => target != null && Vector3.Distance(target.localPosition, position) <= tolerance && Quaternion.Angle(target.localRotation, rotation) <= tolerance && Vector3.Distance(target.localScale, scale) <= tolerance;
            public void Restore() { if (target != null) { target.localPosition = position; target.localRotation = rotation; target.localScale = scale; } }
        }
        private readonly struct Inspection
        {
            public readonly Target Target;
            public readonly AnimationClip Clip;
            public readonly float HipHorizontalRange, HipVerticalRange, MaximumFootTravel, SwordPositionError, SwordAngleError;
            public readonly int RendererCount;
            public Inspection(Target target, AnimationClip clip, float horizontal, float vertical, float feet, float swordPosition, float swordAngle, int rendererCount)
            { Target = target; Clip = clip; HipHorizontalRange = horizontal; HipVerticalRange = vertical; MaximumFootTravel = feet; SwordPositionError = swordPosition; SwordAngleError = swordAngle; RendererCount = rendererCount; }
        }
    }
}
