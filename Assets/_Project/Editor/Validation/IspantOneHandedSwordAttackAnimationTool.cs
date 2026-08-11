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

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantOneHandedSwordAttackAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string MountSourceSlotName = "Ispant_05_RunningOneHandedSwordAttack";
        private const string AttackSlotName = "Ispant_09_OneHandedSwordAttack";
        private const string StaticModelName = "Ispant_Model";
        private const string MountSourceModelName = "Ispant_RunningSwordAttack_Model";
        private const string AttackModelName = "Ispant_OneHandedSwordAttack_Model";
        private const string SwordRootName = "Ispant_ApprovedLongSword";
        private const string SwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string MusketName = "Ispant_RunningAttack_RigidMusket";
        private const string SourceFbxPath = "enemies model/išpant slash.fbx";
        private const string ProjectSourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack_Source.fbx";
        private const string ModelFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_09_OneHandedSwordAttack.controller";
        private const string PlaybackClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_09_OneHandedSwordAttack_Mixamo.anim";
        private const string InspectionPath =
            "docs/validation/ispant_one_handed_sword_attack_2026-08-11/Ispant_09_OneHandedSwordAttack_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_one_handed_sword_attack_2026-08-11/Ispant_09_OneHandedSwordAttack_FinalReview.png";
        private const string VisualDiagnosticCapturePath =
            "docs/validation/ispant_one_handed_sword_attack_visual_revision_2026-08-11/Ispant_09_VisualDiagnostic.png";
        private const string VisualFinalCapturePath =
            "docs/validation/ispant_one_handed_sword_attack_visual_revision_2026-08-11/Ispant_09_VisualFinal.png";
        private const string SourceSha256 =
            "8170211F11E64D5D1BA0D74DA680CB29EEA8068AA28989EE37A28221A8A35467";
        private const string ModelSha256 =
            "71FD6407AEF7B4AACC331C712B676881C74A1A1788A0A28067B685493F04DDB2";
        private const string ClipName = "Ispant_09_OneHandedSwordAttack_Mixamo";
        private const string PlaybackClipName = "Ispant_09_OneHandedSwordAttack_Mixamo";
        private const string StateName = "Ispant_09_OneHandedSwordAttack_Mixamo";
        private const int ExpectedSlots = 12;
        private const int ExpectedBones = 33;
        private const int ExpectedBodyTriangles = 3364;
        private const int ExpectedMusketTriangles = 154;
        private const int ExpectedCrescentTriangles = 1253;
        private const int ExpectedEyeTriangles = 312;
        private const int ExpectedSwordTriangles = 4092;
        private const int FirstFrame = 1;
        private const int LastFrame = 91;
        private const float TransformTolerance = 0.0001f;
        private const float SizeRatioTolerance = 0.01f;
        private const float MotionEpsilon = 0.000001f;
        private const float ExpectedSwordLength = 1.4374533f;
        private const float VisualRootLift = 0.35f;
        private static readonly Vector3 ApprovedGripCenterLocal = new Vector3(0f, 0f, -0.103f);
        private static readonly float[] VisualReviewNormalizedTimes =
            Enumerable.Range(0, 31).Select(index => index / 30f).ToArray();

        [MenuItem("Bellerophon/Enemies/Ispant/Apply One-Handed Sword Attack Animation")]
        public static void ApplyIspantOneHandedSwordAttackAnimation()
        {
            RequireHashes();
            ConfigureSourceImporter();
            RequireHashes();
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbxPath) ??
                throw new InvalidOperationException("The source-derived Ispant slash model is unavailable.");
            var sourceClip = RequireSourceClip();
            var clip = CreateOrUpdatePlaybackClip(sourceClip, RequireModelSchemaClip());
            var controller = CreateOrUpdateController(clip);

            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var mountSourceModel = RequireDirectChild(
                RequireSlot(placement.transform, MountSourceSlotName, 4), MountSourceModelName);
            var attackSlot = RequireSlot(placement.transform, AttackSlotName, 8);
            if (attackSlot.childCount != 1)
                throw new InvalidOperationException(
                    "Ispant_09_OneHandedSwordAttack must contain exactly one model before replacement.");

            var otherSlotsBefore = OtherSlotSignatures(placement.transform, attackSlot);
            var slotBefore = new TransformSnapshot(attackSlot);
            var previous = attackSlot.GetChild(0);
            var replacement = PrefabUtility.InstantiatePrefab(modelPrefab, scene) as GameObject ??
                throw new InvalidOperationException("The source-derived Ispant slash model could not be instantiated.");
            replacement.name = AttackModelName;
            replacement.transform.SetParent(attackSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previous.localPosition, previous.localRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyStaticAppearance(staticModel, replacement.transform);
                FitToStaticReference(replacement.transform, staticModel);
                replacement.transform.position += Vector3.up * VisualRootLift;
                CloneExactStaticSword(staticModel, mountSourceModel, replacement.transform);
                ApplySwordForearmAlignment(replacement.transform, clip);
                ConfigureAnimator(replacement.transform, controller);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (attackSlot.childCount != 1 || attackSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The slot-9 replacement did not leave exactly one model.");
            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The slot-9 transform changed during replacement.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, attackSlot),
                "An Ispant slot outside slot 9 changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(attackSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after the slot-9 replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = attackSlot.gameObject;
            Debug.Log(
                "IspantOneHandedSwordAttackAnimationApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + AttackSlotName +
                ", Source=" + SourceFbxPath +
                ", Clip=" + ClipName + ", Loop=True, Speed=1" +
                ", StaticMaterialsDirect=True, StaticSwordShared=True" +
                ", VisualHeightAdjusted=True, SwordDrivenByRightArm=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsTouched=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect One-Handed Sword Attack Animation")]
        public static void InspectIspantOneHandedSwordAttackAnimation()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var mountSourceModel = RequireDirectChild(
                RequireSlot(placement.transform, MountSourceSlotName, 4), MountSourceModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, AttackSlotName, 8), AttackModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var metrics = InspectModel(model, staticModel, mountSourceModel, animator,
                RequireSourceClip(), RequirePlaybackClip(), RequireController());
            WriteInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Slot-9 inspection changed the scene dirty state.");
            Debug.Log(
                "IspantOneHandedSwordAttackAnimationInspected Result=PASS" +
                ", ClipLength=" + Num(metrics.ClipLength) +
                ", FrameRate=" + Num(metrics.FrameRate) +
                ", RightHandMotion=" + Num(metrics.MaximumRightHandMotion) +
                ", RightHandAngle=" + Num(metrics.MaximumRightHandAngularMotion) +
                ", RightForeArmAngle=" + Num(metrics.MaximumRightForeArmAngularMotion) +
                ", HeightRatio=" + Num(metrics.HeightRatio) +
                ", GroundDifference=" + Num(metrics.GroundDifference) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture One-Handed Sword Attack Review")]
        public static void CaptureIspantOneHandedSwordAttackReview()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var mountSourceModel = RequireDirectChild(
                RequireSlot(placement.transform, MountSourceSlotName, 4), MountSourceModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, AttackSlotName, 8), AttackModelName);
            var sourceClip = RequireSourceClip();
            var clip = RequirePlaybackClip();
            var metrics = InspectModel(model, staticModel, mountSourceModel,
                model.GetComponentsInChildren<Animator>(true).Single(), sourceClip, clip,
                RequireController());
            WriteInspection(metrics);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-9 final review already exists.");
            CaptureReview(staticModel, model, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Slot-9 review capture changed the scene dirty state.");
            Debug.Log(
                "IspantOneHandedSwordAttackReviewCaptured Result=PASS" +
                ", Panels=Static,0,0.25,0.5,0.75,1, Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 9 Visual Diagnostic")]
        public static void CaptureIspant09VisualDiagnostic()
        {
            var destination = Absolute(VisualDiagnosticCapturePath);
            if (File.Exists(destination))
                File.Delete(destination);
            CaptureIspant09VisualReview(destination);
            Debug.Log(
                "Ispant09VisualDiagnosticCaptured" +
                ", Panels=StaticAnd31ContinuousPhases" +
                ", Image=" + VisualDiagnosticCapturePath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 9 Visual Final")]
        public static void CaptureIspant09VisualFinal()
        {
            var destination = Absolute(VisualFinalCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-9 visual final already exists.");
            CaptureIspant09VisualReview(destination);
            Debug.Log(
                "Ispant09VisualFinalCaptured" +
                ", Panels=StaticAnd31ContinuousPhases" +
                ", Image=" + VisualFinalCapturePath + ".");
        }

        private static void CaptureIspant09VisualReview(string destination)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, AttackSlotName, 8), AttackModelName);
            CaptureReview(staticModel, model, RequirePlaybackClip(), destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Slot-9 visual capture changed the scene dirty state.");
        }

        private static void ConfigureSourceImporter()
        {
            AssetDatabase.ImportAsset(ProjectSourceFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ProjectSourceFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The supplied slash ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importBlendShapes = true;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("The supplied slash FBX must expose exactly one animation take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The supplied slash animation take is not Mixamo: " + clips[0].takeName + ".");
            clips[0].name = ClipName;
            clips[0].firstFrame = FirstFrame;
            clips[0].lastFrame = LastFrame;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = false;
            clips[0].lockRootPositionXZ = false;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ProjectSourceFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ClipName)
                throw new InvalidOperationException("The imported slot-9 Mixamo clip differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime || !clips[0].isLooping)
                throw new InvalidOperationException("The slot-9 Mixamo clip is not configured to loop.");
            if (AssetDatabase.GetAssetPath(clips[0]) != ProjectSourceFbxPath)
                throw new InvalidOperationException("The slot-9 clip is not loaded from the supplied slash FBX.");
            return clips[0];
        }

        private static AnimationClip RequireModelSchemaClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException("The source-derived model must expose one binding schema clip.");
            return clips[0];
        }

        private static AnimationClip CreateOrUpdatePlaybackClip(
            AnimationClip source, AnimationClip modelSchema)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlaybackClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = PlaybackClipName };
                AssetDatabase.CreateAsset(clip, PlaybackClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            var schemaBindings = AnimationUtility.GetCurveBindings(modelSchema)
                .GroupBy(BindingKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var copied = 0;
            foreach (var sourceBinding in AnimationUtility.GetCurveBindings(source))
            {
                var targetBinding = ResolveSchemaBinding(sourceBinding, schemaBindings);
                var sourceCurve = AnimationUtility.GetEditorCurve(source, sourceBinding) ??
                    throw new InvalidOperationException("A supplied Mixamo source curve is missing.");
                AnimationUtility.SetEditorCurve(clip, targetBinding, CloneCurve(sourceCurve));
                copied++;
            }
            if (copied == 0)
                throw new InvalidOperationException("The supplied Mixamo source clip has no transform curves.");

            var sourceObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            if (sourceObjectBindings.Length != 0)
                throw new InvalidOperationException(
                    "The supplied Mixamo source unexpectedly contains object-reference curves.");
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.name = PlaybackClipName;
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            RequireExactCurveRemap(source, modelSchema, clip);
            return clip;
        }

        private static EditorCurveBinding ResolveSchemaBinding(
            EditorCurveBinding sourceBinding,
            IReadOnlyDictionary<string, EditorCurveBinding[]> schemaBindings)
        {
            var key = BindingKey(sourceBinding);
            if (!schemaBindings.TryGetValue(key, out var candidates) || candidates.Length != 1)
                throw new InvalidOperationException(
                    "The supplied Mixamo curve cannot be mapped exactly to the model rig: " +
                    sourceBinding.path + " / " + sourceBinding.propertyName + ".");
            return candidates[0];
        }

        private static string BindingKey(EditorCurveBinding binding)
        {
            var separator = binding.path.LastIndexOf('/');
            var leaf = separator >= 0 ? binding.path.Substring(separator + 1) : binding.path;
            return leaf + "|" + binding.propertyName + "|" + binding.type.FullName;
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static AnimationClip RequirePlaybackClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlaybackClipPath) ??
                throw new InvalidOperationException("The slot-9 rebound Mixamo clip is missing.");
            if (clip.name != PlaybackClipName ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime || !clip.isLooping)
                throw new InvalidOperationException("The slot-9 rebound Mixamo loop differs.");
            RequireExactCurveRemap(RequireSourceClip(), RequireModelSchemaClip(), clip);
            return clip;
        }

        private static void RequireExactCurveRemap(
            AnimationClip source, AnimationClip modelSchema, AnimationClip playback)
        {
            var schemaBindings = AnimationUtility.GetCurveBindings(modelSchema)
                .GroupBy(BindingKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var playbackBindings = AnimationUtility.GetCurveBindings(playback)
                .ToDictionary(binding => binding.path + "|" + binding.propertyName + "|" +
                                         binding.type.FullName, binding => binding,
                    StringComparer.Ordinal);
            var sourceBindings = AnimationUtility.GetCurveBindings(source);
            if (playbackBindings.Count < sourceBindings.Length)
                throw new InvalidOperationException("The rebound clip is missing source Mixamo curves.");
            foreach (var sourceBinding in sourceBindings)
            {
                var target = ResolveSchemaBinding(sourceBinding, schemaBindings);
                var targetKey = target.path + "|" + target.propertyName + "|" + target.type.FullName;
                if (!playbackBindings.TryGetValue(targetKey, out var actualBinding))
                    throw new InvalidOperationException("A rebound Mixamo curve binding is missing: " + targetKey + ".");
                var expectedCurve = AnimationUtility.GetEditorCurve(source, sourceBinding);
                var actualCurve = AnimationUtility.GetEditorCurve(playback, actualBinding);
                if (!CurvesMatch(expectedCurve, actualCurve))
                    throw new InvalidOperationException("A rebound Mixamo curve value differs: " + targetKey + ".");
            }
        }

        private static bool CurvesMatch(AnimationCurve expected, AnimationCurve actual)
        {
            if (expected == null || actual == null || expected.length != actual.length ||
                expected.preWrapMode != actual.preWrapMode || expected.postWrapMode != actual.postWrapMode)
                return false;
            for (var index = 0; index < expected.length; index++)
            {
                var left = expected[index];
                var right = actual[index];
                if (Mathf.Abs(left.time - right.time) > MotionEpsilon ||
                    Mathf.Abs(left.value - right.value) > MotionEpsilon ||
                    Mathf.Abs(left.inTangent - right.inTangent) > MotionEpsilon ||
                    Mathf.Abs(left.outTangent - right.outTangent) > MotionEpsilon ||
                    left.weightedMode != right.weightedMode)
                    return false;
            }
            return true;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray())
                stateMachine.RemoveState(child.state);
            foreach (var child in stateMachine.stateMachines.ToArray())
                stateMachine.RemoveStateMachine(child.stateMachine);
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("The slot-9 AnimatorController is missing.");
        }

        private static Animator ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The slot-9 model must contain exactly one Animator.");
            var animator = animators[0];
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void ApplyStaticAppearance(Transform staticModel, Transform model)
        {
            var approved = StaticMaterialMap(staticModel);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 4)
                throw new InvalidOperationException(
                    "The source-derived slash model must contain body, crescent, eyes, and rigid musket renderers.");
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException("A slot-9 material slot is null.");
                    var key = NormalizeMaterialName(material.name);
                    return approved.TryGetValue(key, out var exact)
                        ? exact
                        : throw new InvalidOperationException(
                            "No exact static Ispant material matches " + material.name + ".");
                }).ToArray();
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static Dictionary<string, Material> StaticMaterialMap(Transform staticModel)
        {
            return staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static void CloneExactStaticSword(
            Transform staticModel, Transform mountSourceModel, Transform targetModel)
        {
            var staticRenderer = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var mountRenderer = RequireRenderer<MeshRenderer>(mountSourceModel, SwordRendererName);
            var mountRoot = mountRenderer.transform.parent;
            if (mountRoot == null || mountRoot.name != SwordRootName || mountRoot.parent == null ||
                mountRoot.parent.name != "mixamorig:RightHand")
                throw new InvalidOperationException("The existing exact right-hand sword mount differs.");
            if (SharedMesh(mountRenderer) != SharedMesh(staticRenderer) ||
                !mountRenderer.sharedMaterials.SequenceEqual(staticRenderer.sharedMaterials))
                throw new InvalidOperationException("The existing right-hand sword is not the exact static sword.");

            var rightHand = RequireDescendant(targetModel, "mixamorig:RightHand");
            var root = new GameObject(SwordRootName);
            root.transform.SetParent(rightHand, false);
            CopyLocalTransform(mountRoot, root.transform);
            var rendererObject = new GameObject(SwordRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            CopyLocalTransform(mountRenderer.transform, rendererObject.transform);
            var filter = rendererObject.AddComponent<MeshFilter>();
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = staticRenderer.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterials = staticRenderer.sharedMaterials;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
        }

        private static void CopyLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void ApplySwordForearmAlignment(Transform model, AnimationClip clip)
        {
            var sword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var swordRoot = sword.transform.parent ??
                throw new InvalidOperationException("The slot-9 sword root is missing.");
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            if (swordRoot.parent != rightHand)
                throw new InvalidOperationException("The slot-9 sword is not under the right hand.");

            var path = AnimationUtility.CalculateTransformPath(swordRoot, model);
            var localBladeAxis = CalculateSwordLocalBladeAxis(sword);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rotations = new Quaternion[LastFrame - FirstFrame + 1];
            var positions = new Vector3[LastFrame - FirstFrame + 1];
            var mountedLocalPosition = swordRoot.localPosition;
            var mountedLocalRotation = swordRoot.localRotation;
            Quaternion? previous = null;
            try
            {
                for (var frame = FirstFrame; frame <= LastFrame; frame++)
                {
                    var normalized = (frame - FirstFrame) / (float)(LastFrame - FirstFrame);
                    SampleClip(model.gameObject, clip, normalized * clip.length);
                    swordRoot.localPosition = mountedLocalPosition;
                    swordRoot.localRotation = mountedLocalRotation;
                    var forearmAxis = rightHand.position - rightForeArm.position;
                    if (forearmAxis.sqrMagnitude <= MotionEpsilon)
                        throw new InvalidOperationException("The slot-9 right forearm axis collapsed.");
                    var bladeAxis = sword.transform.TransformVector(localBladeAxis);
                    if (bladeAxis.sqrMagnitude <= MotionEpsilon)
                        throw new InvalidOperationException("The slot-9 sword blade axis collapsed.");

                    var desiredWorldRotation =
                        Quaternion.FromToRotation(bladeAxis.normalized, forearmAxis.normalized) *
                        swordRoot.rotation;
                    var desiredLocalRotation =
                        Quaternion.Inverse(swordRoot.parent.rotation) * desiredWorldRotation;
                    if (previous.HasValue && Quaternion.Dot(previous.Value, desiredLocalRotation) < 0f)
                    {
                        desiredLocalRotation = new Quaternion(
                            -desiredLocalRotation.x,
                            -desiredLocalRotation.y,
                            -desiredLocalRotation.z,
                            -desiredLocalRotation.w);
                    }
                    var gripLocal = ApprovedGripCenterLocal *
                                    (SharedMesh(sword).bounds.size.z / ExpectedSwordLength);
                    var mountedGripWorld = sword.transform.TransformPoint(gripLocal);
                    swordRoot.localRotation = desiredLocalRotation;
                    swordRoot.position += mountedGripWorld - sword.transform.TransformPoint(gripLocal);
                    rotations[frame - FirstFrame] = swordRoot.localRotation;
                    positions[frame - FirstFrame] = swordRoot.localPosition;
                    previous = desiredLocalRotation;
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }

            rotations[rotations.Length - 1] = rotations[0];
            positions[positions.Length - 1] = positions[0];
            var propertyNames = new[]
            {
                "m_LocalRotation.x",
                "m_LocalRotation.y",
                "m_LocalRotation.z",
                "m_LocalRotation.w"
            };
            for (var component = 0; component < propertyNames.Length; component++)
            {
                var keys = new Keyframe[rotations.Length];
                for (var index = 0; index < rotations.Length; index++)
                {
                    var time = index / (float)(rotations.Length - 1) * clip.length;
                    keys[index] = new Keyframe(time, rotations[index][component]);
                }
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < keys.Length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyNames[component]),
                    curve);
            }
            var positionPropertyNames = new[]
            {
                "m_LocalPosition.x",
                "m_LocalPosition.y",
                "m_LocalPosition.z"
            };
            for (var component = 0; component < positionPropertyNames.Length; component++)
            {
                var keys = new Keyframe[positions.Length];
                for (var index = 0; index < positions.Length; index++)
                {
                    var time = index / (float)(positions.Length - 1) * clip.length;
                    keys[index] = new Keyframe(time, positions[index][component]);
                }
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < keys.Length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path, typeof(Transform), positionPropertyNames[component]),
                    curve);
            }
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static Vector3 CalculateSwordLocalBladeAxis(MeshRenderer renderer)
        {
            var mesh = SharedMesh(renderer);
            var vertices = mesh.vertices;
            var grip = ApprovedGripCenterLocal * (mesh.bounds.size.z / ExpectedSwordLength);
            var maximumZ = vertices.Max(vertex => vertex.z);
            var tipVertices = vertices.Where(vertex => maximumZ - vertex.z <= 0.000005f).ToArray();
            if (tipVertices.Length == 0)
                throw new InvalidOperationException("The approved sword tip vertices are missing.");
            var tip = tipVertices.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                      tipVertices.Length;
            return tip - grip;
        }

        private static void FitToStaticReference(Transform model, Transform staticModel)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var staticBounds = BindWorldBounds(staticBody);
            var bounds = BindWorldBounds(body);
            if (bounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The slot-9 bind bounds are invalid.");
            var scale = staticBounds.size.y / bounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException("The slot-9 size ratio is unsafe: " + Num(scale) + ".");
            model.localScale *= scale;
            bounds = BindWorldBounds(body);
            model.position += Vector3.up * (staticBounds.min.y - bounds.min.y);
            EditorUtility.SetDirty(model);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        private static Metrics InspectModel(
            Transform model,
            Transform staticModel,
            Transform mountSourceModel,
            Animator animator,
            AnimationClip sourceClip,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("The slot-9 Animator configuration differs.");
            var defaultState = controller.layers[0].stateMachine.defaultState;
            if (defaultState == null || defaultState.name != StateName || defaultState.motion != clip ||
                Mathf.Abs(defaultState.speed - 1f) > 0.000001f)
                throw new InvalidOperationException("The slot-9 default Mixamo state differs.");
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime || !clip.isLooping)
                throw new InvalidOperationException("The slot-9 source Mixamo clip does not loop.");
            RequireExactCurveRemap(sourceClip, RequireModelSchemaClip(), clip);

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
            if (prefabPath != ModelFbxPath)
                throw new InvalidOperationException("The slot-9 model is not the verified slash-derived model.");
            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var crescent = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Crescent_Ornament");
            var eyes = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Reference_Eye_Slits");
            var musket = RequireRenderer<MeshRenderer>(model, MusketName);
            var sword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            if (model.GetComponentsInChildren<Renderer>(true).Length != 5)
                throw new InvalidOperationException("The slot-9 renderer set differs.");
            if (body.bones.Length != ExpectedBones || crescent.bones.Length != ExpectedBones ||
                eyes.bones.Length != ExpectedBones)
                throw new InvalidOperationException("The slot-9 Mixamo bone count differs.");
            if (TriangleCount(SharedMesh(body)) != ExpectedBodyTriangles ||
                TriangleCount(SharedMesh(musket)) != ExpectedMusketTriangles ||
                TriangleCount(SharedMesh(crescent)) != ExpectedCrescentTriangles ||
                TriangleCount(SharedMesh(eyes)) != ExpectedEyeTriangles ||
                TriangleCount(SharedMesh(sword)) != ExpectedSwordTriangles)
                throw new InvalidOperationException("The slot-9 synchronized mesh topology differs.");
            if (musket.transform.parent != RequireDescendant(model, "mixamorig:Spine2"))
                throw new InvalidOperationException("The source musket is not rigidly attached to the back.");
            if (sword.transform.parent == null || sword.transform.parent.parent !=
                RequireDescendant(model, "mixamorig:RightHand"))
                throw new InvalidOperationException("The exact static sword is not mounted to the right hand.");

            RequireExactStaticMaterials(staticModel, model);
            RequireExactStaticSword(staticModel, mountSourceModel, sword);
            var staticBounds = BindWorldBounds(
                RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body"));
            var modelBounds = BindWorldBounds(body);
            var heightRatio = modelBounds.size.y / staticBounds.size.y;
            var groundDifference = Mathf.Abs(modelBounds.min.y - staticBounds.min.y);
            if (Mathf.Abs(heightRatio - 1f) > SizeRatioTolerance || groundDifference > 0.005f)
                throw new InvalidOperationException(
                    "The slot-9 model does not match the static size and ground level.");

            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            var handPositions = new List<Vector3>();
            var handRotations = new List<Quaternion>();
            var foreArmRotations = new List<Quaternion>();
            try
            {
                var sampleCount = LastFrame - FirstFrame + 1;
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = sampleCount == 1 ? 0f : clip.length * index / (sampleCount - 1f);
                    SampleClip(model.gameObject, clip, time);
                    handPositions.Add(model.InverseTransformPoint(rightHand.position));
                    handRotations.Add(rightHand.localRotation);
                    foreArmRotations.Add(rightForeArm.localRotation);
                }
            }
            finally
            {
                foreach (var snapshot in transformSnapshots)
                    snapshot.Restore();
                StopSampling();
            }
            var maximumRightHandMotion = 0f;
            var maximumRightHandAngularMotion = 0f;
            var maximumRightForeArmAngularMotion = 0f;
            for (var first = 0; first < handPositions.Count; first++)
            for (var second = first + 1; second < handPositions.Count; second++)
            {
                maximumRightHandMotion = Mathf.Max(maximumRightHandMotion,
                    Vector3.Distance(handPositions[first], handPositions[second]));
                maximumRightHandAngularMotion = Mathf.Max(maximumRightHandAngularMotion,
                    Quaternion.Angle(handRotations[first], handRotations[second]));
                maximumRightForeArmAngularMotion = Mathf.Max(maximumRightForeArmAngularMotion,
                    Quaternion.Angle(foreArmRotations[first], foreArmRotations[second]));
            }
            if (maximumRightHandMotion <= MotionEpsilon &&
                maximumRightHandAngularMotion <= MotionEpsilon &&
                maximumRightForeArmAngularMotion <= MotionEpsilon)
                throw new InvalidOperationException(
                    "The supplied Mixamo slash curves produced no right-arm transform change.");

            return new Metrics(clip.length, clip.frameRate, maximumRightHandMotion,
                maximumRightHandAngularMotion, maximumRightForeArmAngularMotion,
                heightRatio, groundDifference);
        }

        private static void RequireExactStaticSword(
            Transform staticModel, Transform mountSourceModel, MeshRenderer target)
        {
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var mountSword = RequireRenderer<MeshRenderer>(mountSourceModel, SwordRendererName);
            if (SharedMesh(target) != SharedMesh(staticSword) ||
                !target.sharedMaterials.SequenceEqual(staticSword.sharedMaterials))
                throw new InvalidOperationException("The slot-9 sword is not the exact static shared sword.");
            RequireLocalTransform(target.transform.parent, mountSword.transform.parent,
                "right-hand sword root mount");
            RequireLocalTransform(target.transform, mountSword.transform,
                "right-hand sword renderer correction");
        }

        private static void RequireExactStaticMaterials(Transform staticModel, Transform model)
        {
            var approved = StaticMaterialMap(staticModel);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null ||
                    !approved.TryGetValue(NormalizeMaterialName(material.name), out var exact) ||
                    material != exact)
                    throw new InvalidOperationException(
                        "A slot-9 material is not a direct static appearance reference.");
            }
        }

        private static void RequireLocalTransform(Transform actual, Transform expected, string label)
        {
            if (Vector3.Distance(actual.localPosition, expected.localPosition) > TransformTolerance ||
                Quaternion.Angle(actual.localRotation, expected.localRotation) > TransformTolerance ||
                Vector3.Distance(actual.localScale, expected.localScale) > TransformTolerance)
                throw new InvalidOperationException("The copied " + label + " differs.");
        }

        private static string NormalizeMaterialName(string name)
        {
            var result = name.Replace(" (Instance)", string.Empty);
            var suffix = result.LastIndexOf('.');
            if (suffix >= 0 && result.Length - suffix == 4 &&
                int.TryParse(result.Substring(suffix + 1), out _))
                result = result.Substring(0, suffix);
            return result;
        }

        private static void CaptureReview(
            Transform staticModel, Transform model, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The slot-9 capture folder is invalid."));
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = staticRenderers.Concat(modelRenderers)
                .Distinct().Select(renderer => new RendererSnapshot(renderer)).ToArray();
            var layerSnapshots = staticRenderers.Concat(modelRenderers)
                .Select(renderer => renderer.gameObject).Distinct()
                .Select(item => new LayerSnapshot(item)).ToArray();
            var cameraObject = new GameObject("Ispant09ReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var keyObject = new GameObject("Ispant09ReviewKey", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            var fillObject = new GameObject("Ispant09ReviewFill", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            const int renderLayer = 30;
            const int panelWidth = 400;
            const int panelHeight = 520;
            var panels = VisualReviewNormalizedTimes.Length + 1;
            var strip = new Texture2D(panelWidth * panels, panelHeight, TextureFormat.RGB24, false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in layerSnapshots)
                    snapshot.GameObject.layer = renderLayer;
                foreach (var renderer in staticRenderers.Concat(modelRenderers))
                    renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << renderLayer;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                var key = keyObject.GetComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.2f;
                key.color = new Color(1f, 0.95f, 0.88f);
                key.cullingMask = 1 << renderLayer;
                keyObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var fill = fillObject.GetComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.7f;
                fill.color = new Color(0.65f, 0.78f, 1f);
                fill.cullingMask = 1 << renderLayer;
                fillObject.transform.rotation = Quaternion.Euler(20f, 145f, 0f);
                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
                var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                var attackReferenceCenter = model.parent.TransformPoint(
                    staticModel.parent.InverseTransformPoint(staticBody.bounds.center));
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                FrameCamera(camera, staticBody.bounds.center, referenceHeight);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                for (var index = 0; index < VisualReviewNormalizedTimes.Length; index++)
                {
                    SampleClip(model.gameObject, clip, VisualReviewNormalizedTimes[index] * clip.length);
                    FrameCamera(camera, attackReferenceCenter, referenceHeight);
                    RenderPanel(camera, panel, strip, target, index + 1, panelWidth, panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Restore();
                foreach (var snapshot in layerSnapshots)
                    snapshot.Restore();
                foreach (var snapshot in transformSnapshots)
                    snapshot.Restore();
                StopSampling();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
        }

        private static void RenderPanel(
            Camera camera, Texture2D panel, Texture2D strip, RenderTexture target,
            int panelIndex, int width, int height)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The slot-9 review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(Camera camera, Vector3 center, float height)
        {
            camera.aspect = 1f;
            var distance = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * distance * 1.25f +
                                        Vector3.up * height * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The slot-9 inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + AttackSlotName,
                "SourceFbx=" + SourceFbxPath,
                "ProjectSourceFbx=" + ProjectSourceFbxPath,
                "ModelFbx=" + ModelFbxPath,
                "SourceSha256=" + SourceSha256,
                "ModelSha256=" + ModelSha256,
                "SourceAction=Armature|mixamo.com|Layer0",
                "SourceFrames=" + FirstFrame + "-" + LastFrame,
                "PlaybackClip=" + ClipName,
                "PlaybackReboundClip=" + PlaybackClipName,
                "PlaybackCurveValuesCopiedExactly=True",
                "PlaybackBindingPathsMappedByExactUniqueBoneAndProperty=True",
                "ClipLengthSeconds=" + Num(metrics.ClipLength),
                "ClipFrameRate=" + Num(metrics.FrameRate),
                "LoopTime=True",
                "AnimatorSpeed=1",
                "AnimatorApplyRootMotion=False",
                "MaximumRightHandMotion=" + Num(metrics.MaximumRightHandMotion),
                "MaximumRightHandAngularMotion=" + Num(metrics.MaximumRightHandAngularMotion),
                "MaximumRightForeArmAngularMotion=" + Num(metrics.MaximumRightForeArmAngularMotion),
                "MixamoBones=" + ExpectedBones,
                "BodyTriangles=" + ExpectedBodyTriangles,
                "CrescentTriangles=" + ExpectedCrescentTriangles,
                "EyeTriangles=" + ExpectedEyeTriangles,
                "RigidMusketTriangles=" + ExpectedMusketTriangles,
                "MusketSource=Supplied slash FBX vertices",
                "MusketParent=mixamorig:Spine2",
                "SwordSource=Ispant_01_Static shared mesh and materials",
                "SwordMountSource=existing synchronized slot-5 right-hand mount",
                "SwordTriangles=" + ExpectedSwordTriangles,
                "StaticAppearanceMaterialsDirectReference=True",
                "HeightRatio=" + Num(metrics.HeightRatio),
                "GroundLevelDifference=" + Num(metrics.GroundDifference),
                "OtherSlotsChanged=False",
                "OtherSceneRootsTouched=False",
                "ReviewImage=" + CapturePath
            }, Encoding.UTF8);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectSourceFbxPath, SourceSha256);
            RequireHash(ModelFbxPath, ModelSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant slot-9 asset hash differs: " + path + ".");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active for slot-9 work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var roots = scene.GetRootGameObjects().Where(item => item.name == PlacementRootName).ToArray();
            if (roots.Length != 1 || roots[0].transform.childCount != ExpectedSlots)
                throw new InvalidOperationException("The approved Ispant placement contract differs.");
            return roots[0];
        }

        private static Transform RequireSlot(Transform placement, string name, int index)
        {
            if (index < 0 || index >= placement.childCount || placement.GetChild(index).name != name)
                throw new InvalidOperationException("The required Ispant slot differs: " + name + ".");
            return placement.GetChild(index);
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return parent.Cast<Transform>().SingleOrDefault(child => child.name == name) ??
                   throw new InvalidOperationException(
                       "Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required slot-9 renderer is missing: " + name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required slot-9 bone differs: " + name + ".");
            return matches[0];
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException("A slot-9 renderer has no mesh: " + renderer.name + ".");
        }

        private static int TriangleCount(Mesh mesh)
        {
            var total = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                total += mesh.GetIndexCount(subMesh) > int.MaxValue
                    ? throw new InvalidOperationException("A slot-9 mesh index count is unsafe.")
                    : (int)mesh.GetIndexCount(subMesh) / 3;
            return total;
        }

        private static Bounds BindWorldBounds(SkinnedMeshRenderer renderer)
        {
            var vertices = SharedMesh(renderer).vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("A slot-9 mesh has no vertices.");
            var bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
                bounds.Encapsulate(renderer.transform.TransformPoint(vertices[index]));
            return bounds;
        }

        private static void SampleClip(GameObject model, AnimationClip clip, float time)
        {
            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, clip, time);
            AnimationMode.EndSampling();
        }

        private static void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount).Select(placement.GetChild)
                .Where(item => item != targetSlot).Select(RecursiveSignature).ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Num(item.localRotation.x)).Append(',').Append(Num(item.localRotation.y)).Append(',')
                    .Append(Num(item.localRotation.z)).Append(',').Append(Num(item.localRotation.w)).Append('|')
                    .Append(Vec(item.localScale));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled).Append(':')
                        .Append(AssetDatabase.GetAssetPath(SharedMesh(renderer)));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private readonly struct Metrics
        {
            public readonly float ClipLength;
            public readonly float FrameRate;
            public readonly float MaximumRightHandMotion;
            public readonly float MaximumRightHandAngularMotion;
            public readonly float MaximumRightForeArmAngularMotion;
            public readonly float HeightRatio;
            public readonly float GroundDifference;

            public Metrics(
                float clipLength,
                float frameRate,
                float maximumRightHandMotion,
                float maximumRightHandAngularMotion,
                float maximumRightForeArmAngularMotion,
                float heightRatio,
                float groundDifference)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                MaximumRightHandMotion = maximumRightHandMotion;
                MaximumRightHandAngularMotion = maximumRightHandAngularMotion;
                MaximumRightForeArmAngularMotion = maximumRightForeArmAngularMotion;
                HeightRatio = heightRatio;
                GroundDifference = groundDifference;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform _transform;
            private readonly Vector3 _localPosition;
            private readonly Quaternion _localRotation;
            private readonly Vector3 _localScale;

            public TransformSnapshot(Transform transform)
            {
                _transform = transform;
                _localPosition = transform.localPosition;
                _localRotation = transform.localRotation;
                _localScale = transform.localScale;
            }

            public bool Matches(float tolerance)
            {
                return Vector3.Distance(_transform.localPosition, _localPosition) <= tolerance &&
                       Quaternion.Angle(_transform.localRotation, _localRotation) <= tolerance &&
                       Vector3.Distance(_transform.localScale, _localScale) <= tolerance;
            }

            public void Restore()
            {
                _transform.localPosition = _localPosition;
                _transform.localRotation = _localRotation;
                _transform.localScale = _localScale;
            }
        }

        private readonly struct RendererSnapshot
        {
            public readonly Renderer Renderer;
            private readonly bool _enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                _enabled = renderer.enabled;
            }

            public void Restore()
            {
                Renderer.enabled = _enabled;
            }
        }

        private readonly struct LayerSnapshot
        {
            public readonly GameObject GameObject;
            private readonly int _layer;

            public LayerSnapshot(GameObject gameObject)
            {
                GameObject = gameObject;
                _layer = gameObject.layer;
            }

            public void Restore()
            {
                GameObject.layer = _layer;
            }
        }
    }
}
