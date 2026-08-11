using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantHitReactionAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string MoveSlotName = "Ispant_03_Move";
        private const string HeightReferenceSlotName = "Ispant_09_OneHandedSwordAttack";
        private const string HitSlotName = "Ispant_11_HitReaction";
        private const string StaticModelName = "Ispant_Model";
        private const string MoveModelName = "Ispant_Move_Model";
        private const string HeightReferenceModelName = "Ispant_OneHandedSwordAttack_Model";
        private const string HitModelName = "Ispant_HitReaction_Model";
        private const string BodyRendererName = "Ispant_Armed_Body";
        private const string SwordRootName = "Ispant_ApprovedLongSword";
        private const string SwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string SourceFbxPath = "enemies model/išpant hit.fbx";
        private const string ProjectFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Hit.fbx";
        private const string ModelFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_11_HitReaction_Mixamo.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_11_HitReaction.controller";
        private const string DiagnosticPath =
            "docs/validation/ispant_hit_reaction_2026-08-11/Ispant_11_HitReaction_Diagnostic.png";
        private const string FinalPath =
            "docs/validation/ispant_hit_reaction_2026-08-11/Ispant_11_HitReaction_Final.png";
        private const string HeightDiagnosticPath =
            "docs/validation/ispant_hit_reaction_height_2026-08-11/Ispant_11_Height_Diagnostic.png";
        private const string HeightFinalPath =
            "docs/validation/ispant_hit_reaction_height_2026-08-11/Ispant_11_Height_Final.png";
        private const string SourceSha256 =
            "0002E5C7D8B986C5EADA6EDB0EAEB93F51F7EEA6BF78B5B50ADA384C508C10E4";
        private const string ImportedClipName = "Ispant_11_HitReaction_Mixamo_Source";
        private const string PlaybackClipName = "Ispant_11_HitReaction_Mixamo";
        private const string StateName = "Ispant_11_HitReaction_Mixamo";
        private const int ExpectedSlots = 12;
        private const float MotionEpsilon = 0.000001f;
        private const float TransformTolerance = 0.0001f;
        private static readonly float[] ReviewNormalizedTimes =
            Enumerable.Range(0, 13).Select(index => index / 12f).ToArray();
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 11 Hit Replacement")]
        public static void ApplyIspant11HitReplacement()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            _ = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                throw new InvalidOperationException("The supplied Ispant hit FBX is unavailable.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbxPath) ??
                         throw new InvalidOperationException(
                             "The static-appearance-compatible Ispant model container is unavailable.");
            var sourceClip = RequireSourceClip();
            var clip = CreateOrUpdatePlaybackClip(sourceClip, RequireModelSchemaClip());

            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var moveModel = RequireDirectChild(
                RequireSlot(placement.transform, MoveSlotName, 2), MoveModelName);
            var hitSlot = RequireSlot(placement.transform, HitSlotName, 10);
            if (hitSlot.childCount != 1)
                throw new InvalidOperationException("Ispant slot 11 must contain exactly one model before replacement.");

            var otherSlotsBefore = OtherSlotSignatures(placement.transform, hitSlot);
            var slotBefore = new TransformSnapshot(hitSlot);
            var previous = hitSlot.GetChild(0);
            var replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                              throw new InvalidOperationException("The supplied Ispant hit model could not be instantiated.");
            replacement.name = HitModelName;
            replacement.transform.SetParent(hitSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previous.localPosition, previous.localRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyExactStaticMaterials(staticModel, replacement.transform);
                CloneExactHipSword(staticModel, moveModel, replacement.transform);
                FitToStaticReference(replacement.transform, staticModel);
                AuthorLoweredArmsForEntireClip(replacement.transform, clip);
                ConfigureAnimator(replacement.transform, CreateOrUpdateController(clip));
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (hitSlot.childCount != 1 || hitSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The slot-11 replacement did not leave exactly one model.");
            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The slot-11 container transform changed during replacement.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, hitSlot),
                "An Ispant slot outside slot 11 changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(hitSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after the slot-11 replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = hitSlot.gameObject;
            Debug.Log(
                "Ispant11HitReplacementApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + HitSlotName +
                ", Source=" + SourceFbxPath +
                ", Clip=mixamo.com, Loop=True" +
                ", StaticAppearanceShared=True, ArmsLoweredForEntireClip=True" +
                ", OtherSlotsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 11 Hit Diagnostic")]
        public static void CaptureIspant11HitDiagnostic()
        {
            var destination = Absolute(DiagnosticPath);
            if (File.Exists(destination))
                File.Delete(destination);
            CaptureVisualReview(destination);
            Debug.Log("Ispant11HitDiagnosticCaptured Image=" + DiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 11 Hit Final")]
        public static void CaptureIspant11HitFinal()
        {
            var destination = Absolute(FinalPath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-11 final visual already exists.");
            CaptureVisualReview(destination);
            Debug.Log("Ispant11HitFinalCaptured Image=" + FinalPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 11 Height Alignment")]
        public static void ApplyIspant11HeightAlignment()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var referenceModel = RequireDirectChild(
                RequireSlot(placement.transform, HeightReferenceSlotName, 8),
                HeightReferenceModelName);
            var hitSlot = RequireSlot(placement.transform, HitSlotName, 10);
            var model = RequireDirectChild(hitSlot, HitModelName);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, hitSlot);
            var positionBefore = model.localPosition;
            var rotationBefore = model.localRotation;
            var scaleBefore = model.localScale;

            model.localPosition = new Vector3(
                positionBefore.x,
                referenceModel.localPosition.y,
                positionBefore.z);

            if (Mathf.Abs(model.localPosition.x - positionBefore.x) > TransformTolerance ||
                Mathf.Abs(model.localPosition.z - positionBefore.z) > TransformTolerance ||
                Quaternion.Angle(model.localRotation, rotationBefore) > TransformTolerance ||
                Vector3.Distance(model.localScale, scaleBefore) > TransformTolerance)
                throw new InvalidOperationException("A slot-11 transform component outside Y changed.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, hitSlot),
                "An Ispant slot outside slot 11 changed during height alignment.");

            EditorUtility.SetDirty(model);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot-11 height alignment.");
            Selection.activeGameObject = model.gameObject;
            Debug.Log(
                "Ispant11HeightAlignmentApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + HitSlotName + "/" + HitModelName +
                ", Reference=" + HeightReferenceSlotName + "/" + HeightReferenceModelName +
                ", Changed=ModelLocalPositionYOnly, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 11 Height Diagnostic")]
        public static void CaptureIspant11HeightDiagnostic()
        {
            var destination = Absolute(HeightDiagnosticPath);
            if (File.Exists(destination))
                File.Delete(destination);
            CaptureVisualReview(destination, fixedVerticalReference: true);
            Debug.Log("Ispant11HeightDiagnosticCaptured Image=" + HeightDiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 11 Height Final")]
        public static void CaptureIspant11HeightFinal()
        {
            var destination = Absolute(HeightFinalPath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-11 height final already exists.");
            CaptureVisualReview(destination, fixedVerticalReference: true);
            Debug.Log("Ispant11HeightFinalCaptured Image=" + HeightFinalPath + ".");
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                ProjectFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ProjectFbxPath) as ModelImporter ??
                           throw new InvalidOperationException("The supplied hit ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importBlendShapes = true;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("The supplied hit FBX must expose exactly one animation take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The supplied hit animation take is not Mixamo: " + clips[0].takeName + ".");
            clips[0].name = ImportedClipName;
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
            var clips = AssetDatabase.LoadAllAssetsAtPath(ProjectFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported slot-11 Mixamo clip differs.");
            if (AssetDatabase.GetAssetPath(clips[0]) != ProjectFbxPath)
                throw new InvalidOperationException("The slot-11 animation is not loaded from the supplied FBX.");
            return clips[0];
        }

        private static AnimationClip RequireModelSchemaClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ModelFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    "The static-appearance-compatible model must expose one binding schema clip.");
            return clips[0];
        }

        private static AnimationClip CreateOrUpdatePlaybackClip(
            AnimationClip source,
            AnimationClip modelSchema)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = PlaybackClipName };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            var schemaBindings = AnimationUtility.GetCurveBindings(modelSchema)
                .GroupBy(BindingKey, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                                  throw new InvalidOperationException("A supplied hit curve is missing.");
                AnimationUtility.SetEditorCurve(
                    clip,
                    ResolveSchemaBinding(binding, schemaBindings),
                    CloneCurve(sourceCurve));
            }
            if (AnimationUtility.GetObjectReferenceCurveBindings(source).Length != 0)
                throw new InvalidOperationException("The supplied hit clip contains unexpected object curves.");
            clip.name = PlaybackClipName;
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static EditorCurveBinding ResolveSchemaBinding(
            EditorCurveBinding sourceBinding,
            IReadOnlyDictionary<string, EditorCurveBinding[]> schemaBindings)
        {
            var key = BindingKey(sourceBinding);
            if (!schemaBindings.TryGetValue(key, out var candidates) || candidates.Length != 1)
                throw new InvalidOperationException(
                    "A supplied hit curve cannot be mapped exactly to the Ispant model rig: " +
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

        private static void ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The slot-11 model must contain exactly one Animator.");
            var animator = animators[0];
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        private static void ApplyExactStaticMaterials(Transform staticModel, Transform model)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 4)
                throw new InvalidOperationException(
                    "The Ispant model container must contain body, crescent, eyes, and back musket renderers.");
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException("A model-container material slot is null.");
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

        private static string NormalizeMaterialName(string name)
        {
            const string instanceSuffix = " (Instance)";
            const string duplicatedSuffix = "_duplicated";
            var result = name.EndsWith(instanceSuffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - instanceSuffix.Length)
                : name;
            while (result.EndsWith(duplicatedSuffix, StringComparison.OrdinalIgnoreCase))
                result = result.Substring(0, result.Length - duplicatedSuffix.Length);
            return result;
        }

        private static void CloneExactHipSword(
            Transform staticModel,
            Transform moveModel,
            Transform targetModel)
        {
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var source = RequireRenderer<MeshRenderer>(moveModel, SwordRendererName);
            if (SharedMesh(source) != SharedMesh(staticSword) ||
                !source.sharedMaterials.SequenceEqual(staticSword.sharedMaterials))
                throw new InvalidOperationException("The Mixamo hip sword is not the exact static sword.");
            var sourceRoot = source.transform.parent ??
                             throw new InvalidOperationException("The Mixamo hip sword root is missing.");
            var sourceHips = sourceRoot.parent ??
                             throw new InvalidOperationException("The Mixamo hip sword Hips parent is missing.");
            if (!string.Equals(NormalizeBoneName(sourceHips.name), "Hips", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The exact sword is not mounted below Mixamo Hips.");
            var targetHips = RequireMappedTransform(BuildUniqueTransformMap(targetModel), sourceHips.name);
            var root = new GameObject(SwordRootName);
            root.transform.SetParent(targetHips, false);
            SetLocalMatrix(root.transform, LocalMatrix(sourceRoot));
            CloneMeshRenderer(source, root.transform, SwordRendererName, LocalMatrix(source.transform));
            EditorUtility.SetDirty(root);
        }

        private static MeshRenderer CloneMeshRenderer(
            MeshRenderer source,
            Transform parent,
            string name,
            Matrix4x4 localMatrix)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            SetLocalMatrix(item.transform, localMatrix);
            item.AddComponent<MeshFilter>().sharedMesh = SharedMesh(source);
            var target = item.AddComponent<MeshRenderer>();
            target.sharedMaterials = source.sharedMaterials;
            CopyRendererSettings(source, target);
            EditorUtility.SetDirty(item);
            return target;
        }

        private static void CopyRendererSettings(Renderer source, Renderer target)
        {
            target.enabled = source.enabled;
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = source.lightProbeUsage;
            target.reflectionProbeUsage = source.reflectionProbeUsage;
            target.renderingLayerMask = source.renderingLayerMask;
            target.motionVectorGenerationMode = source.motionVectorGenerationMode;
            target.allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
        }

        private static Dictionary<string, Transform> BuildUniqueTransformMap(Transform root)
        {
            var result = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            var hipsMatches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    NormalizeBoneName(item.name), "Hips", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hipsMatches.Length != 1)
                throw new InvalidOperationException("The hit rig must contain exactly one Hips skeleton root.");
            var hips = hipsMatches[0];
            foreach (var item in hips.GetComponentsInChildren<Transform>(true).Prepend(hips.parent))
            {
                if (item == null)
                    continue;
                var key = NormalizeBoneName(item.name);
                if (result.TryGetValue(key, out var existing) && existing != item)
                    throw new InvalidOperationException("The hit rig has duplicate bone name: " + key + ".");
                result[key] = item;
            }
            return result;
        }

        private static Transform RequireMappedTransform(
            IReadOnlyDictionary<string, Transform> map,
            string name)
        {
            var key = NormalizeBoneName(name);
            return map.TryGetValue(key, out var result)
                ? result
                : throw new InvalidOperationException("The hit rig is missing bone: " + key + ".");
        }

        private static string NormalizeBoneName(string name)
        {
            var separator = name.LastIndexOf(':');
            var withoutNamespace = separator >= 0 ? name.Substring(separator + 1) : name;
            var digitStart = withoutNamespace.Length;
            while (digitStart > 0 && char.IsDigit(withoutNamespace[digitStart - 1]))
                digitStart--;
            if (digitStart == withoutNamespace.Length)
                return withoutNamespace;
            var digits = withoutNamespace.Substring(digitStart);
            var number = int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
            return withoutNamespace.Substring(0, digitStart) +
                   number.ToString(CultureInfo.InvariantCulture);
        }

        private static void AuthorLoweredArmsForEntireClip(Transform model, AnimationClip clip)
        {
            var bones = BuildUniqueTransformMap(model);
            var leftUpper = RequireMappedTransform(bones, "LeftArm");
            var leftLower = RequireMappedTransform(bones, "LeftForeArm");
            var leftHand = RequireMappedTransform(bones, "LeftHand");
            var rightUpper = RequireMappedTransform(bones, "RightArm");
            var rightLower = RequireMappedTransform(bones, "RightForeArm");
            var rightHand = RequireMappedTransform(bones, "RightHand");
            var controlled = new[]
            {
                leftUpper, leftLower, leftHand,
                rightUpper, rightLower, rightHand
            };
            var paths = controlled.Select(item =>
                AnimationUtility.CalculateTransformPath(item, model)).ToArray();
            var sampleCount = Mathf.Max(2, Mathf.RoundToInt(clip.length * clip.frameRate) + 1);
            var values = controlled.Select(_ => new Quaternion[sampleCount]).ToArray();
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = clip.length * index / (sampleCount - 1f);
                    SampleClip(model.gameObject, clip, time);
                    AuthorHangingArm(model, leftUpper, leftLower, leftHand, true);
                    AuthorHangingArm(model, rightUpper, rightLower, rightHand, false);
                    for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                    {
                        var rotation = controlled[boneIndex].localRotation;
                        if (index > 0 && Quaternion.Dot(values[boneIndex][index - 1], rotation) < 0f)
                            rotation = Negate(rotation);
                        values[boneIndex][index] = rotation;
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }
            for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                values[boneIndex][sampleCount - 1] = values[boneIndex][0];
            for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                SetQuaternionCurves(clip, paths[boneIndex], values[boneIndex]);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static void AuthorHangingArm(
            Transform model,
            Transform upper,
            Transform lower,
            Transform hand,
            bool left)
        {
            var handRotation = hand.rotation;
            var armLength = Vector3.Distance(upper.position, lower.position) +
                            Vector3.Distance(lower.position, hand.position);
            var side = left ? -model.right : model.right;
            var handTarget = upper.position - model.up * (armLength * 0.96f) +
                             side * (armLength * 0.10f);
            var elbowPole = upper.position - model.up * (armLength * 0.52f) +
                            side * (armLength * 0.36f) +
                            model.forward * (armLength * 0.08f);
            SolveTwoBoneChain(upper, lower, hand, handTarget, elbowPole, handRotation);
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
            if (rootToTarget.sqrMagnitude <= MotionEpsilon)
                throw new InvalidOperationException("A hanging-arm target collapsed.");
            var targetDistance = Mathf.Clamp(
                rootToTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            var targetDirection = rootToTarget.normalized;
            var poleDirection = Vector3.ProjectOnPlane(pole - rootPosition, targetDirection).normalized;
            if (poleDirection.sqrMagnitude < 0.5f)
                throw new InvalidOperationException("A hanging-arm elbow pole is degenerate.");
            var along = (upperLength * upperLength + targetDistance * targetDistance -
                         lowerLength * lowerLength) / (2f * targetDistance);
            var away = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            var desiredJoint = rootPosition + targetDirection * along + poleDirection * away;
            upper.rotation = Quaternion.FromToRotation(
                lower.position - upper.position,
                desiredJoint - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                tip.position - lower.position,
                tipTarget - lower.position) * lower.rotation;
            tip.rotation = tipRotation;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Quaternion> values)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path &&
                                        (item.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) ||
                                         item.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal)))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y",
                "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                var keys = new Keyframe[values.Count];
                for (var index = 0; index < values.Count; index++)
                    keys[index] = new Keyframe(
                        clip.length * index / (values.Count - 1f), values[index][component]);
                var curve = new AnimationCurve(keys)
                {
                    preWrapMode = WrapMode.Loop,
                    postWrapMode = WrapMode.Loop
                };
                for (var index = 0; index < keys.Length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]),
                    curve);
            }
        }

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);

        private static void FitToStaticReference(Transform model, Transform staticModel)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BodyRendererName);
            var body = RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName);
            var staticBounds = BindWorldBounds(staticBody);
            var bounds = BindWorldBounds(body);
            if (bounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The slot-11 bind bounds are invalid.");
            var scale = staticBounds.size.y / bounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException("The slot-11 appearance scale is unsafe.");
            model.localScale *= scale;
            bounds = BindWorldBounds(body);
            model.position += Vector3.up * (staticBounds.min.y - bounds.min.y);
            EditorUtility.SetDirty(model);
        }

        private static void CaptureVisualReview(
            string destination,
            bool fixedVerticalReference = false)
        {
            StopSampling();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, HitSlotName, 10), HitModelName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The slot-11 playback clip is missing.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid slot-11 capture folder."));

            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = staticRenderers.Concat(targetRenderers)
                .Distinct().Select(item => new RendererSnapshot(item)).ToArray();
            var layerSnapshots = staticRenderers.Concat(targetRenderers)
                .Select(item => item.gameObject).Distinct()
                .Select(item => new LayerSnapshot(item)).ToArray();
            var cameraObject = new GameObject("Ispant11HitReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var keyObject = new GameObject("Ispant11HitReviewKey", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            var fillObject = new GameObject("Ispant11HitReviewFill", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            const int renderLayer = 30;
            const int panelWidth = 380;
            const int panelHeight = 620;
            const int panelColumns = 7;
            var panelCount = ReviewNormalizedTimes.Length + 1;
            var panelRows = Mathf.CeilToInt(panelCount / (float)panelColumns);
            var strip = new Texture2D(
                panelWidth * panelColumns,
                panelHeight * panelRows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var layer in layerSnapshots)
                    layer.GameObject.layer = renderLayer;
                foreach (var renderer in staticRenderers.Concat(targetRenderers))
                    renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << renderLayer;
                camera.fieldOfView = 34f;
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

                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BodyRendererName);
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                var referenceCenter = staticBody.bounds.center;
                var targetReferenceCenter = model.parent.TransformPoint(
                    staticModel.parent.InverseTransformPoint(referenceCenter));
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                foreach (var layer in layerSnapshots)
                    layer.GameObject.layer = renderLayer;
                RenderPanel(camera, target, panel, strip, 0, referenceCenter, referenceHeight,
                    panelWidth, panelHeight, panelColumns, panelRows);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in targetRenderers)
                    renderer.enabled = true;
                for (var index = 0; index < ReviewNormalizedTimes.Length; index++)
                {
                    SampleClip(model.gameObject, clip, ReviewNormalizedTimes[index] * clip.length);
                    foreach (var renderer in targetRenderers)
                        renderer.enabled = true;
                    foreach (var layer in layerSnapshots)
                        layer.GameObject.layer = renderLayer;
                    var body = RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName);
                    RenderPanel(
                        camera,
                        target,
                        panel,
                        strip,
                        index + 1,
                        fixedVerticalReference ? targetReferenceCenter : body.bounds.center,
                        referenceHeight, panelWidth, panelHeight, panelColumns, panelRows);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                foreach (var snapshot in rendererSnapshots) snapshot.Restore();
                foreach (var snapshot in layerSnapshots) snapshot.Restore();
                foreach (var snapshot in transformSnapshots) snapshot.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Slot-11 visual capture changed the scene dirty state.");
        }

        private static void RenderPanel(
            Camera camera,
            RenderTexture target,
            Texture2D panel,
            Texture2D strip,
            int index,
            Vector3 center,
            float height,
            int width,
            int panelHeight,
            int columns,
            int rows)
        {
            camera.aspect = width / (float)panelHeight;
            var distance = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * distance * 1.28f;
            camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, panelHeight), 0, 0);
            panel.Apply();
            var column = index % columns;
            var rowFromTop = index / columns;
            strip.SetPixels32(
                column * width,
                (rows - 1 - rowFromTop) * panelHeight,
                width,
                panelHeight,
                panel.GetPixels32());
        }

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            var position = (Vector3)matrix.GetColumn(3);
            var x = (Vector3)matrix.GetColumn(0);
            var y = (Vector3)matrix.GetColumn(1);
            var z = (Vector3)matrix.GetColumn(2);
            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                throw new InvalidOperationException("A copied hit transform has invalid scale.");
            target.SetLocalPositionAndRotation(position, Quaternion.LookRotation(z / scale.z, y / scale.y));
            target.localScale = scale;
        }

        private static Matrix4x4 LocalMatrix(Transform transform) =>
            Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);

        private static Bounds BindWorldBounds(SkinnedMeshRenderer renderer)
        {
            var vertices = SharedMesh(renderer).vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("An Ispant body mesh has no vertices.");
            var bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
                bounds.Encapsulate(renderer.transform.TransformPoint(vertices[index]));
            return bounds;
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException("An Ispant renderer has no mesh: " + renderer.name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required Ispant renderer is missing: " + name + ".");
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

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active for slot-11 work.");
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

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount).Select(placement.GetChild)
                .Where(item => item != targetSlot)
                .Select(item => item.name + "|" + item.childCount + "|" +
                                Vec(item.localPosition) + "|" + Vec(item.localScale))
                .ToArray();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectFbxPath, SourceSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant slot-11 asset hash differs: " + path + ".");
        }

        private static string Absolute(string path) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        private static string Num(float value) =>
            value.ToString("0.#########", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);

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
            private readonly Renderer _renderer;
            private readonly bool _enabled;

            public RendererSnapshot(Renderer renderer)
            {
                _renderer = renderer;
                _enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (_renderer != null)
                    _renderer.enabled = _enabled;
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
                if (GameObject != null)
                    GameObject.layer = _layer;
            }
        }
    }
}
