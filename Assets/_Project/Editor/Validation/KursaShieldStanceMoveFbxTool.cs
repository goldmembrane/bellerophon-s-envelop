using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaShieldStanceMoveFbxTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string StanceSlotName = "Kursa_05_ToShieldStance";
        private const string TargetSlotName = "Kursa_07_ShieldStanceMove";
        private const string ModelName = "Kursa_Model";
        private const string AnimatedRootName = "Kursa_ShieldStanceMove_AnimatedRoot";
        private const string EffectName = "Kursa_ShieldStanceIcon";
        private const string ExternalSourcePath =
            "enemies model/KUŠkursa shield walking.fbx";
        private const string SourceModelPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Models/Kursa_ShieldStanceMove_Source.fbx";
        private const string StanceClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_05_ToShieldStance_Loop.anim";
        private const string ObsoleteProceduralClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove_InPlace.anim";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove.controller";
        private const string PendingControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove_FbxPending.controller";
        private const string UpperPoseClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove_UpperPose.anim";
        private const string UpperBodyMaskPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_07_ShieldStanceMove_UpperBody.mask";
        private const string ImportedClipName = "Kursa_07_ShieldStanceMove_Mixamo";
        private const string BaseStateName = "KursaShieldStanceMoveMixamoLoop";
        private const string UpperStateName = "KursaShieldStanceUpperPose";
        private const string ValidationFolder =
            "docs/validation/kursa_shield_stance_move_fbx_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_ShieldStanceMoveFbx_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_ShieldStanceMoveFbx_FinalReview.png";
        private const float StanceCompletionTime = 1f;
        private const float MatrixTolerance = 0.00001f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        private static readonly float[] ReviewFractions = Enumerable.Range(0, 18)
            .Select(index => index / 17f)
            .ToArray();

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Shield Stance Move FBX")]
        public static void ApplyKursaShieldStanceMoveFbx()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var stanceModel = RequireModel(RequireChild(
                placement.transform,
                StanceSlotName));
            var targetSlot = RequireChild(placement.transform, TargetSlotName);
            var previous = RequireModel(targetSlot);
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var stanceRenderer = RequireRenderer(stanceModel, StanceSlotName);
            var stanceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                StanceClipPath) ?? throw new InvalidOperationException(
                "Kursa to-shield-stance clip is missing.");

            CopySourceIntoUnity();
            var takeName = ConfigureImporter();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                throw new InvalidOperationException(
                    "Kursa shield-stance move source prefab is missing.");
            var sourceRenderer = RequireRenderer(
                sourcePrefab.transform,
                "Kursa shield-stance move source FBX");
            var sourceClip = RequireEmbeddedClip(takeName);
            RequireExactRigCompatibility(staticRenderer, sourceRenderer);
            RequireClipBindings(sourcePrefab.transform, sourceClip);

            DeleteAssetIfPresent(PendingControllerPath);
            DeleteAssetIfPresent(UpperPoseClipPath);
            DeleteAssetIfPresent(UpperBodyMaskPath);

            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;

            var wrapper = new GameObject("Kursa_ShieldStanceMove_Wrapper_Pending");
            wrapper.transform.SetParent(targetSlot, false);
            wrapper.transform.SetLocalPositionAndRotation(
                previousPosition,
                previousRotation);
            wrapper.transform.localScale = staticModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Kursa shield-stance move source FBX could not be instantiated.");
            replacement.name = AnimatedRootName;
            replacement.transform.SetParent(wrapper.transform, false);
            replacement.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            replacement.transform.localScale = Vector3.one;

            try
            {
                var replacementRenderer = RequireRenderer(
                    wrapper.transform,
                    TargetSlotName);
                ApplyExactStaticAppearance(
                    wrapper.transform,
                    replacementRenderer,
                    staticRenderer);
                var effect = CopyApprovedEffect(stanceModel, wrapper.transform);
                var upperPoseClip = CreateUpperPoseClip(
                    replacement.transform,
                    stanceModel,
                    stanceRenderer,
                    stanceClip,
                    sourceClip.length);
                var upperMask = CreateUpperBodyMask(replacement.transform);
                var controller = CreateLayeredController(
                    sourceClip,
                    upperPoseClip,
                    upperMask);
                ConfigureAnimator(replacement, controller);
                RequirePlacedContract(
                    wrapper.transform,
                    staticRenderer,
                    sourceClip,
                    upperPoseClip,
                    upperMask,
                    controller,
                    effect);

                DeleteAssetIfPresent(ControllerPath);
                var moveError = AssetDatabase.MoveAsset(
                    PendingControllerPath,
                    ControllerPath);
                if (!string.IsNullOrEmpty(moveError))
                    throw new InvalidOperationException(
                        "Kursa shield-stance move controller could not be finalized: " +
                        moveError);
                DeleteAssetIfPresent(ObsoleteProceduralClipPath);

                UnityEngine.Object.DestroyImmediate(previous.gameObject);
                wrapper.name = ModelName;
                RequireEqual(
                    otherSlotsBefore,
                    OtherSlotSignatures(placement.transform),
                    "A Kursa slot outside Kursa_07_ShieldStanceMove changed.");
                RequireEqual(
                    otherRootsBefore,
                    OtherRootSignatures(scene, placement),
                    "A scene root outside the Kursa placement changed.");
                RequireSlotContract(placement.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after replacing " +
                        "Kursa_07_ShieldStanceMove.");
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "KursaShieldStanceMoveFbxApplied Result=PASS, Slot=" +
                    TargetSlotName + ", Source=" + SourceModelPath +
                    ", MixamoTake=" + takeName +
                    ", FullBodyMixamoBaseLayer=True, UpperPoseOverrideAfterBase=True" +
                    ", ExactStaticMesh=True, ExactStaticSkin=True" +
                    ", ExactStaticMaterials=True, ApprovedShieldIconVisible=True" +
                    ", Loop=True, RootMotion=False, OtherSlotsUnchanged=True" +
                    ", OtherSceneRootsUnchanged=True, SceneSaved=True.");
            }
            catch
            {
                if (wrapper != null) UnityEngine.Object.DestroyImmediate(wrapper);
                DeleteAssetIfPresent(PendingControllerPath);
                DeleteAssetIfPresent(UpperPoseClipPath);
                DeleteAssetIfPresent(UpperBodyMaskPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Stance Move FBX Diagnostic")]
        public static void CaptureKursaShieldStanceMoveFbxDiagnostic()
        {
            CaptureReview(NextDiagnosticPath(), "Diagnostic");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Stance Move FBX Final Review")]
        public static void CaptureKursaShieldStanceMoveFbxFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa shield-stance move FBX final review already exists: " +
                    FinalReviewPath);
            CaptureReview(destination, "FinalReview");
        }

        private static void CopySourceIntoUnity()
        {
            var source = Absolute(ExternalSourcePath);
            if (!File.Exists(source))
                throw new FileNotFoundException(
                    "The user-supplied Kursa shield walking FBX is missing.",
                    source);
            var destination = Absolute(SourceModelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid FBX destination folder."));
            File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(
                SourceModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(SourceModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Kursa shield-stance move FBX importer is unavailable.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            var defaults = importer.defaultClipAnimations;
            var matches = defaults.Where(item =>
                    item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.takeName.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "The Kursa shield walking FBX must expose exactly one Mixamo take. " +
                    "Matches=" + matches.Length + ", Defaults=" +
                    string.Join("|", defaults.Select(item =>
                        item.name + ":" + item.takeName)) + ".");
            var selected = matches[0];
            selected.name = ImportedClipName;
            selected.loopTime = true;
            selected.loopPose = false;
            selected.wrapMode = WrapMode.Loop;
            importer.animationWrapMode = WrapMode.Loop;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
            return selected.name;
        }

        private static AnimationClip RequireEmbeddedClip(string clipName)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(SourceModelPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(clips[0].name, clipName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The selected Mixamo take is not the sole imported Kursa " +
                    "shield walking clip. Clips=" +
                    string.Join("|", clips.Select(item => item.name)) + ".");
            }
            return clips[0];
        }

        private static AnimationClip CreateUpperPoseClip(
            Transform animatedRoot,
            Transform stanceModel,
            SkinnedMeshRenderer stanceRenderer,
            AnimationClip stanceClip,
            float duration)
        {
            var sourceRenderer = RequireRenderer(
                animatedRoot,
                "Kursa shield-stance move animated root");
            var sourceBones = UniqueTransforms(
                sourceRenderer.rootBone.GetComponentsInChildren<Transform>(true),
                "shield-stance move source skeleton");
            var stanceBones = UniqueTransforms(
                stanceRenderer.rootBone.GetComponentsInChildren<Transform>(true),
                "to-shield-stance skeleton");
            if (!sourceBones.TryGetValue("Spine", out var sourceSpine) ||
                !stanceBones.ContainsKey("Spine"))
            {
                throw new InvalidOperationException(
                    "The Kursa upper-body Spine bone is missing.");
            }
            var stanceSnapshots = stanceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                stanceClip.SampleAnimation(stanceModel.gameObject, StanceCompletionTime);
                var upperBones = sourceSpine.GetComponentsInChildren<Transform>(true);
                var clip = new AnimationClip
                {
                    name = "Kursa_07_ShieldStanceMove_UpperPose",
                    frameRate = 60f,
                    wrapMode = WrapMode.Loop
                };
                foreach (var sourceBone in upperBones)
                {
                    if (!stanceBones.TryGetValue(sourceBone.name, out var stanceBone))
                        throw new InvalidOperationException(
                            "The #5 completed stance is missing upper-body bone: " +
                            sourceBone.name + ".");
                    var path = AnimationUtility.CalculateTransformPath(
                        sourceBone,
                        animatedRoot);
                    SetConstantTransformCurves(
                        clip,
                        path,
                        stanceBone.localPosition,
                        stanceBone.localRotation,
                        duration);
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                clip.EnsureQuaternionContinuity();
                AssetDatabase.CreateAsset(clip, UpperPoseClipPath);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
                return clip;
            }
            finally
            {
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
            }
        }

        private static void SetConstantTransformCurves(
            AnimationClip clip,
            string path,
            Vector3 position,
            Quaternion rotation,
            float duration)
        {
            SetConstantCurve(clip, path, "m_LocalPosition.x", position.x, duration);
            SetConstantCurve(clip, path, "m_LocalPosition.y", position.y, duration);
            SetConstantCurve(clip, path, "m_LocalPosition.z", position.z, duration);
            SetConstantCurve(clip, path, "m_LocalRotation.x", rotation.x, duration);
            SetConstantCurve(clip, path, "m_LocalRotation.y", rotation.y, duration);
            SetConstantCurve(clip, path, "m_LocalRotation.z", rotation.z, duration);
            SetConstantCurve(clip, path, "m_LocalRotation.w", rotation.w, duration);
        }

        private static void SetConstantCurve(
            AnimationClip clip,
            string path,
            string property,
            float value,
            float duration)
        {
            var curve = AnimationCurve.Constant(0f, duration, value);
            curve.preWrapMode = WrapMode.ClampForever;
            curve.postWrapMode = WrapMode.ClampForever;
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static AvatarMask CreateUpperBodyMask(Transform animatedRoot)
        {
            var spine = animatedRoot.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "Spine") ??
                throw new InvalidOperationException(
                    "The Kursa shield walking FBX Spine bone is missing.");
            var upper = new HashSet<Transform>(
                spine.GetComponentsInChildren<Transform>(true));
            var transforms = animatedRoot.GetComponentsInChildren<Transform>(true)
                .Where(item => item != animatedRoot)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(
                    item,
                    animatedRoot), StringComparer.Ordinal)
                .ToArray();
            var mask = new AvatarMask { name = "Kursa_07_ShieldStanceMove_UpperBody" };
            for (var index = 0;
                 index < (int)AvatarMaskBodyPart.LastBodyPart;
                 index++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            }
            mask.transformCount = transforms.Length;
            for (var index = 0; index < transforms.Length; index++)
            {
                mask.SetTransformPath(
                    index,
                    AnimationUtility.CalculateTransformPath(
                        transforms[index],
                        animatedRoot));
                mask.SetTransformActive(index, upper.Contains(transforms[index]));
            }
            AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            EditorUtility.SetDirty(mask);
            AssetDatabase.SaveAssets();
            return mask;
        }

        private static AnimatorController CreateLayeredController(
            AnimationClip sourceClip,
            AnimationClip upperPoseClip,
            AvatarMask upperMask)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                PendingControllerPath);
            var baseState = controller.layers[0].stateMachine.AddState(BaseStateName);
            baseState.motion = sourceClip;
            baseState.speed = 1f;
            baseState.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = baseState;

            controller.AddLayer("ShieldStanceUpperOverride");
            var layers = controller.layers;
            var upperLayer = layers[1];
            upperLayer.defaultWeight = 1f;
            upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            upperLayer.avatarMask = upperMask;
            var upperState = upperLayer.stateMachine.AddState(UpperStateName);
            upperState.motion = upperPoseClip;
            upperState.speed = 1f;
            upperState.writeDefaultValues = false;
            upperLayer.stateMachine.defaultState = upperState;
            layers[1] = upperLayer;
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureAnimator(
            GameObject replacement,
            RuntimeAnimatorController controller)
        {
            var animators = replacement.GetComponentsInChildren<Animator>(true);
            Animator animator;
            if (animators.Length == 0)
            {
                animator = replacement.AddComponent<Animator>();
            }
            else
            {
                if (animators.Length != 1 || animators[0].transform != replacement.transform)
                    throw new InvalidOperationException(
                        "Kursa shield-stance move FBX Animator root is not exact.");
                animator = animators[0];
            }
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.Rebind();
            animator.Update(0f);
            EditorUtility.SetDirty(animator);
        }

        private static SpriteRenderer CopyApprovedEffect(
            Transform stanceModel,
            Transform wrapper)
        {
            var source = stanceModel.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(item => item.name == EffectName) ??
                throw new InvalidOperationException(
                    "The approved Kursa shield icon is missing from #5.");
            var copy = UnityEngine.Object.Instantiate(source.gameObject);
            copy.name = EffectName;
            copy.transform.SetParent(wrapper, false);
            copy.transform.localPosition = source.transform.localPosition;
            copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = source.transform.localScale;
            var renderer = copy.GetComponent<SpriteRenderer>() ??
                throw new InvalidOperationException(
                    "The copied Kursa shield icon renderer is missing.");
            var color = source.color;
            color.a = 1f;
            renderer.color = color;
            EditorUtility.SetDirty(renderer);
            return renderer;
        }

        private static void RequireExactRigCompatibility(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer sourceRenderer)
        {
            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            var sourceMesh = sourceRenderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Shield walking source mesh is missing.");
            if (staticRenderer.rootBone == null || sourceRenderer.rootBone == null)
                throw new InvalidOperationException("A Kursa root bone is missing.");
            var sourceIndices = UniqueBoneIndices(
                sourceRenderer.bones,
                "shield walking source");
            var staticIndices = UniqueBoneIndices(
                staticRenderer.bones,
                "static Kursa");
            if (staticMesh.bindposes.Length != staticRenderer.bones.Length ||
                sourceMesh.bindposes.Length != sourceRenderer.bones.Length)
            {
                throw new InvalidOperationException(
                    "A Kursa mesh bind-pose list does not match its renderer bones.");
            }
            foreach (var item in staticIndices)
            {
                if (!sourceIndices.TryGetValue(item.Key, out var sourceIndex))
                    throw new InvalidOperationException(
                        "Shield walking source is missing exact static bone: " +
                        item.Key + ".");
                if (!MatrixMatches(
                    staticMesh.bindposes[item.Value],
                    sourceMesh.bindposes[sourceIndex]))
                {
                    throw new InvalidOperationException(
                        "Shield walking source bind pose differs for exact static bone: " +
                        item.Key + ".");
                }
            }
            if (!string.Equals(
                staticRenderer.rootBone.name,
                sourceRenderer.rootBone.name,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Shield walking source root bone differs from static Kursa.");
            }
        }

        private static void ApplyExactStaticAppearance(
            Transform replacement,
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer staticRenderer)
        {
            RequireExactRigCompatibility(staticRenderer, replacementRenderer);
            var byName = UniqueTransforms(
                replacement.GetComponentsInChildren<Transform>(true),
                "shield-stance move replacement");
            var mappedBones = staticRenderer.bones.Select(staticBone =>
            {
                if (!byName.TryGetValue(staticBone.name, out var mapped))
                    throw new InvalidOperationException(
                        "Shield-stance move replacement is missing exact static bone: " +
                        staticBone.name + ".");
                return mapped;
            }).ToArray();
            if (!byName.TryGetValue(staticRenderer.rootBone.name, out var mappedRoot))
                throw new InvalidOperationException(
                    "Shield-stance move replacement is missing the exact root bone.");
            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            var staticMaterials = staticRenderer.sharedMaterials;
            if (staticMaterials.Length != staticMesh.subMeshCount ||
                staticMaterials.Any(item => item == null))
            {
                throw new InvalidOperationException(
                    "Static Kursa material slots are incomplete.");
            }
            replacementRenderer.sharedMesh = staticMesh;
            replacementRenderer.bones = mappedBones;
            replacementRenderer.rootBone = mappedRoot;
            replacementRenderer.sharedMaterials = staticMaterials;
            replacementRenderer.localBounds = staticRenderer.localBounds;
            replacementRenderer.quality = staticRenderer.quality;
            replacementRenderer.updateWhenOffscreen = true;
            replacementRenderer.skinnedMotionVectors =
                staticRenderer.skinnedMotionVectors;
            replacementRenderer.shadowCastingMode =
                staticRenderer.shadowCastingMode;
            replacementRenderer.receiveShadows = staticRenderer.receiveShadows;
            replacementRenderer.lightProbeUsage = staticRenderer.lightProbeUsage;
            replacementRenderer.reflectionProbeUsage =
                staticRenderer.reflectionProbeUsage;
            replacementRenderer.renderingLayerMask =
                staticRenderer.renderingLayerMask;
            replacementRenderer.motionVectorGenerationMode =
                staticRenderer.motionVectorGenerationMode;
            var propertyBlock = new MaterialPropertyBlock();
            staticRenderer.GetPropertyBlock(propertyBlock);
            replacementRenderer.SetPropertyBlock(propertyBlock);
            for (var index = 0; index < staticMesh.blendShapeCount; index++)
            {
                replacementRenderer.SetBlendShapeWeight(
                    index,
                    staticRenderer.GetBlendShapeWeight(index));
            }
            EditorUtility.SetDirty(replacementRenderer);
        }

        private static void RequirePlacedContract(
            Transform wrapper,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip sourceClip,
            AnimationClip upperPoseClip,
            AvatarMask upperMask,
            AnimatorController controller,
            SpriteRenderer effect)
        {
            var renderer = RequireRenderer(wrapper, TargetSlotName);
            if (renderer.sharedMesh != staticRenderer.sharedMesh ||
                !renderer.sharedMaterials.SequenceEqual(staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Placed shield-stance move appearance differs from static Kursa.");
            }
            if (!renderer.bones.Select(item => item.name).SequenceEqual(
                staticRenderer.bones.Select(item => item.name)))
            {
                throw new InvalidOperationException(
                    "Placed shield-stance move skin bone order differs.");
            }
            var animator = wrapper.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Placed shield-stance move must contain one Animator.");
            if (animator.transform == wrapper ||
                animator.transform.parent != wrapper ||
                animator.transform.name != AnimatedRootName ||
                animator.transform.localPosition != Vector3.zero ||
                animator.transform.localRotation != Quaternion.identity ||
                animator.transform.localScale != Vector3.one)
            {
                throw new InvalidOperationException(
                    "Placed shield-stance move animated root is not isolated.");
            }
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Placed shield-stance move Animator configuration differs.");
            }
            if (!AnimationUtility.GetAnimationClipSettings(sourceClip).loopTime ||
                !AnimationUtility.GetAnimationClipSettings(upperPoseClip).loopTime)
            {
                throw new InvalidOperationException(
                    "Shield-stance move clips are not looping.");
            }
            var layers = controller.layers;
            if (layers.Length != 2 ||
                layers[0].stateMachine.defaultState?.motion != sourceClip ||
                layers[1].stateMachine.defaultState?.motion != upperPoseClip ||
                layers[1].blendingMode != AnimatorLayerBlendingMode.Override ||
                layers[1].defaultWeight != 1f ||
                layers[1].avatarMask != upperMask)
            {
                throw new InvalidOperationException(
                    "Shield-stance move layered controller differs.");
            }
            if (effect == null || effect.name != EffectName ||
                effect.sprite == null || effect.color.a < 0.99f)
            {
                throw new InvalidOperationException(
                    "Placed shield-stance move approved icon differs.");
            }
            RequireClipBindings(animator.transform, sourceClip);
            RequireClipBindings(animator.transform, upperPoseClip);
        }

        private static void RequireClipBindings(Transform root, AnimationClip clip)
        {
            var missing = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform) &&
                    !string.IsNullOrEmpty(binding.path) &&
                    root.Find(binding.path) == null)
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (missing.Length != 0)
                throw new InvalidOperationException(
                    "Kursa shield-stance move clip paths differ: " +
                    string.Join("|", missing) + ".");
        }

        private static void CaptureReview(string destination, string kind)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var stanceModel = RequireModel(RequireChild(
                placement.transform,
                StanceSlotName));
            var targetModel = RequireModel(RequireChild(
                placement.transform,
                TargetSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var targetRenderer = RequireRenderer(targetModel, TargetSlotName);
            var sourceClip = RequireEmbeddedClip(ImportedClipName);
            var upperClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                UpperPoseClipPath) ?? throw new InvalidOperationException(
                "Kursa shield-stance move upper pose clip is missing.");
            var upperMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                UpperBodyMaskPath) ?? throw new InvalidOperationException(
                "Kursa shield-stance move upper mask is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                "Kursa shield-stance move controller is missing.");
            var effect = targetModel.GetComponentsInChildren<SpriteRenderer>(true)
                .SingleOrDefault(item => item.name == EffectName) ??
                throw new InvalidOperationException(
                    "Kursa shield-stance move effect is missing.");
            RequirePlacedContract(
                targetModel,
                staticRenderer,
                sourceClip,
                upperClip,
                upperMask,
                controller,
                effect);
            CaptureContactSheet(
                scene,
                staticModel,
                stanceModel,
                targetModel,
                targetRenderer,
                sourceClip,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa shield-stance move FBX capture changed scene dirty state.");
            Debug.Log(
                "KursaShieldStanceMoveFbxReviewCaptured Kind=" + kind +
                ", FullMixamoLoop=True, UpperOverride=True" +
                ", StaticAppearanceReference=True, StanceReference=True" +
                ", DirectVisualReviewRequired=True, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            Transform stanceModel,
            Transform targetModel,
            SkinnedMeshRenderer targetRenderer,
            AnimationClip sourceClip,
            string destination)
        {
            const int panelWidth = 320;
            const int panelHeight = 320;
            const int columns = 5;
            const int rows = 5;
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererState(item))
                .ToArray();
            var stanceSnapshots = stanceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var targetSnapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var stanceAnimator = stanceModel.GetComponentsInChildren<Animator>(true).Single();
            var targetAnimator = targetModel.GetComponentsInChildren<Animator>(true).Single();
            var stanceAnimatorEnabled = stanceAnimator.enabled;
            var targetAnimatorEnabled = targetAnimator.enabled;
            var stanceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                StanceClipPath) ?? throw new InvalidOperationException(
                "Kursa stance clip is missing during capture.");
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject(
                "KursaShieldStanceMoveFbxReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
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
            var grid = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                stanceAnimator.enabled = false;
                targetAnimator.enabled = true;
                var targetBounds = FullControllerBounds(
                    targetModel,
                    targetRenderer,
                    targetAnimator,
                    targetSnapshots);
                var staticBounds = BoundsOf(staticModel);
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                stanceClip.SampleAnimation(stanceModel.gameObject, StanceCompletionTime);
                var stanceBounds = BoundsOf(stanceModel);
                var sharedSize = Vector3.Max(
                    Vector3.Max(staticBounds.size, stanceBounds.size),
                    targetBounds.size);
                staticBounds.size = sharedSize;
                stanceBounds.size = sharedSize;
                targetBounds.size = sharedSize;
                var upperBounds = FullControllerUpperBounds(
                    targetModel,
                    targetAnimator,
                    targetSnapshots);

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 28f;
                camera.aspect = 1f;
                camera.targetTexture = target;

                RenderSubject(
                    camera,
                    staticModel,
                    sceneRenderers,
                    target,
                    panel,
                    staticBounds,
                    35f);
                CopyPanel(panel, grid, 0, rows - 1, panelWidth, panelHeight);
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                stanceClip.SampleAnimation(stanceModel.gameObject, StanceCompletionTime);
                RenderSubject(
                    camera,
                    stanceModel,
                    sceneRenderers,
                    target,
                    panel,
                    stanceBounds,
                    35f);
                CopyPanel(panel, grid, 1, rows - 1, panelWidth, panelHeight);

                for (var index = 0; index < ReviewFractions.Length; index++)
                {
                    SampleLayeredAnimator(
                        targetAnimator,
                        targetSnapshots,
                        ReviewFractions[index]);
                    RenderSubject(
                        camera,
                        targetModel,
                        sceneRenderers,
                        target,
                        panel,
                        targetBounds,
                        35f);
                    var panelIndex = index + 2;
                    CopyPanel(
                        panel,
                        grid,
                        panelIndex % columns,
                        rows - 1 - panelIndex / columns,
                        panelWidth,
                        panelHeight);
                }

                var closeFractions = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                for (var index = 0; index < closeFractions.Length; index++)
                {
                    SampleLayeredAnimator(
                        targetAnimator,
                        targetSnapshots,
                        closeFractions[index]);
                    RenderSubject(
                        camera,
                        targetModel,
                        sceneRenderers,
                        target,
                        panel,
                        upperBounds,
                        90f);
                    CopyPanel(
                        panel,
                        grid,
                        index,
                        0,
                        panelWidth,
                        panelHeight);
                }
                grid.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa shield-stance move FBX review folder."));
                File.WriteAllBytes(destination, grid.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in stanceSnapshots) snapshot.Restore();
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                stanceAnimator.enabled = stanceAnimatorEnabled;
                targetAnimator.enabled = targetAnimatorEnabled;
                foreach (var state in rendererStates) state.Restore();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(grid);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void SampleLayeredAnimator(
            Animator animator,
            IReadOnlyList<TransformSnapshot> snapshots,
            float fraction)
        {
            foreach (var snapshot in snapshots) snapshot.Restore();
            animator.Rebind();
            animator.Play(BaseStateName, 0, fraction);
            animator.Play(UpperStateName, 1, fraction);
            animator.Update(0f);
        }

        private static Bounds FullControllerBounds(
            Transform targetModel,
            Renderer targetRenderer,
            Animator animator,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var effect = targetModel.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(item => item.name == EffectName);
            var initialized = false;
            var result = new Bounds();
            foreach (var fraction in ReviewFractions)
            {
                SampleLayeredAnimator(animator, snapshots, fraction);
                var current = targetRenderer.bounds;
                current.Encapsulate(effect.bounds);
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else result.Encapsulate(current);
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds FullControllerUpperBounds(
            Transform targetModel,
            Animator animator,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var fraction in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                SampleLayeredAnimator(animator, snapshots, fraction);
                var current = CurrentUpperBounds(targetModel);
                if (!initialized)
                {
                    result = current;
                    initialized = true;
                }
                else result.Encapsulate(current);
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static Bounds CurrentUpperBounds(Transform model)
        {
            var spine = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "Spine") ??
                throw new InvalidOperationException(
                    "Shield-stance move review is missing the Spine bone.");
            var points = spine.GetComponentsInChildren<Transform>(true)
                .Select(item => item.position)
                .ToArray();
            var result = new Bounds(points[0], Vector3.zero);
            foreach (var point in points.Skip(1)) result.Encapsulate(point);
            result.Expand(new Vector3(0.72f, 0.32f, 0.72f));
            return result;
        }

        private static Bounds BoundsOf(Transform model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeSelf)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Kursa review model has no renderer.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void RenderSubject(
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
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void CopyPanel(
            Texture2D panel,
            Texture2D grid,
            int column,
            int row,
            int width,
            int height)
        {
            grid.SetPixels(
                column * width,
                row * height,
                width,
                height,
                panel.GetPixels());
        }

        private static Dictionary<string, int> UniqueBoneIndices(
            IReadOnlyList<Transform> bones,
            string context)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < bones.Count; index++)
            {
                var bone = bones[index] ?? throw new InvalidOperationException(
                    context + " contains a null bone.");
                if (!result.TryAdd(bone.name, index))
                    throw new InvalidOperationException(
                        context + " contains duplicate bone name: " + bone.name + ".");
            }
            return result;
        }

        private static Dictionary<string, Transform> UniqueTransforms(
            IEnumerable<Transform> transforms,
            string context)
        {
            var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var transform in transforms)
            {
                if (!result.TryAdd(transform.name, transform))
                    throw new InvalidOperationException(
                        context + " contains duplicate transform name: " +
                        transform.name + ".");
            }
            return result;
        }

        private static bool MatrixMatches(Matrix4x4 left, Matrix4x4 right)
        {
            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    if (Mathf.Abs(left[row, column] - right[row, column]) >
                        MatrixTolerance)
                        return false;
                }
            }
            return true;
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var candidate = Absolute(string.Format(DiagnosticPathFormat, index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The two approved Kursa shield-stance move FBX diagnostics exist.");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on Kursa shield-stance move FBX.");
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
                        "Kursa slot contract differs at " + index + ".");
                }
            }
        }

        private static Transform RequireChild(Transform parent, string name)
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
                .Select(item => RecursiveSignature(RequireChild(placement, item)))
                .ToArray();

        private static string[] OtherRootSignatures(
            Scene scene,
            GameObject placement) =>
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
                        builder.Append(':').Append(
                            AssetDatabase.GetAssetPath(skinned.sharedMesh));
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
                throw new InvalidOperationException(
                    "Asset could not be replaced: " + path);
            }
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null) renderer.enabled = enabled;
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
    }
}
