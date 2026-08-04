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
    internal static class KursaShieldBashAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string ShieldBashSlotName = "Kursa_04_ShieldBash";
        private const string ModelName = "Kursa_Model";
        private const string AnimatedRootName = "Kursa_ShieldBash_AnimatedRoot";
        private const string SourceModelPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Models/Kursa_ShieldBash_Source.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_04_ShieldBash.controller";
        private const string ImportedClipName = "Kursa_04_ShieldBash_Mixamo";
        private const string ValidationFolder =
            "docs/validation/kursa_shield_bash_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_ShieldBash_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_ShieldBash_FinalReview.png";
        private const string ScaleFinalReviewPath =
            ValidationFolder + "/Kursa_ShieldBash_Size_FinalReview.png";
        private const string ScaleDiagnosticPath =
            ValidationFolder + "/Kursa_ShieldBash_Size_Diagnostic_01.png";
        private const float MatrixTolerance = 0.00001f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        // Samples cover the complete clip and its return to the first frame for direct review.
        private static readonly float[] ReviewFractions = Enumerable.Range(0, 19)
            .Select(index => index / 18f)
            .ToArray();

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Shield Bash Animation")]
        public static void ApplyKursaShieldBashAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var bashSlot = RequireChild(placement.transform, ShieldBashSlotName);
            var previous = RequireModel(bashSlot);

            var takeName = ConfigureImporter();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                throw new InvalidOperationException(
                    "Kursa shield-bash source prefab is missing.");
            var sourceRenderer = RequireRenderer(
                sourcePrefab.transform,
                "Kursa shield-bash source FBX");
            var sourceClip = RequireEmbeddedClip(takeName);
            RequireExactRigCompatibility(staticRenderer, sourceRenderer);
            RequireClipBindings(sourcePrefab.transform, sourceClip);
            var controller = CreateController(sourceClip);

            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;

            var wrapper = new GameObject("Kursa_ShieldBash_Wrapper_Pending");
            wrapper.transform.SetParent(bashSlot, false);
            wrapper.transform.SetLocalPositionAndRotation(
                previousPosition,
                previousRotation);
            wrapper.transform.localScale = staticModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Kursa shield-bash source FBX could not be instantiated.");
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
                    ShieldBashSlotName);
                ApplyExactStaticAppearance(
                    wrapper.transform,
                    replacementRenderer,
                    staticRenderer);
                ConfigureAnimator(replacement, controller);
                RequirePlacedContract(
                    wrapper.transform,
                    staticRenderer,
                    sourceClip,
                    controller);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(wrapper);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            wrapper.name = ModelName;
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform),
                "A Kursa slot outside Kursa_04_ShieldBash changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Kursa placement changed.");
            RequireSlotContract(placement.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after replacing Kursa_04_ShieldBash.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log(
                "KursaShieldBashAnimationApplied Result=PASS, " +
                "Slot=Kursa_04_ShieldBash, Source=" + SourceModelPath +
                ", MixamoTake=" + takeName +
                ", ExactStaticMesh=True, ExactStaticUv=True, ExactStaticSkin=True" +
                ", ExactStaticMaterials=True, Loop=True, RootMotion=False" +
                ", OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Shield Bash Scale Match")]
        public static void ApplyKursaShieldBashScaleMatch()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var bashModel = RequireModel(RequireChild(
                placement.transform,
                ShieldBashSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var bashRenderer = RequireRenderer(bashModel, ShieldBashSlotName);
            var clip = RequireEmbeddedClip(ImportedClipName);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Kursa shield-bash controller is missing.");
            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);

            var animator = bashModel.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Placed shield-bash model must contain one Animator.");
            Transform scaleWrapper;
            if (animator.transform == bashModel)
            {
                var slot = bashModel.parent;
                var previousPosition = bashModel.localPosition;
                var previousRotation = bashModel.localRotation;
                bashModel.name = AnimatedRootName;
                var wrapperObject = new GameObject("Kursa_ShieldBash_Wrapper_Pending");
                scaleWrapper = wrapperObject.transform;
                scaleWrapper.SetParent(slot, false);
                scaleWrapper.SetLocalPositionAndRotation(
                    previousPosition,
                    previousRotation);
                scaleWrapper.localScale = staticModel.localScale;
                bashModel.SetParent(scaleWrapper, false);
                bashModel.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                bashModel.localScale = Vector3.one;
                wrapperObject.name = ModelName;
            }
            else
            {
                scaleWrapper = bashModel;
                scaleWrapper.localScale = staticModel.localScale;
                animator.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                animator.transform.localScale = Vector3.one;
                animator.transform.name = AnimatedRootName;
            }
            EditorUtility.SetDirty(scaleWrapper);
            EditorUtility.SetDirty(animator.transform);
            RequirePlacedContract(scaleWrapper, staticRenderer, clip, controller);
            if (scaleWrapper.localScale != staticModel.localScale)
                throw new InvalidOperationException(
                    "Kursa shield-bash model scale does not match the static Kursa.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform),
                "A Kursa slot outside Kursa_04_ShieldBash changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Kursa placement changed.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after matching shield-bash scale.");
            Debug.Log(
                "KursaShieldBashScaleMatched Result=PASS, " +
                "Reference=Kursa_01_Static_Review/Kursa_Model, " +
                "Target=Kursa_04_ShieldBash/Kursa_Model, " +
                "AnimatedRoot=Kursa_ShieldBash_AnimatedRoot, " +
                "ScaleWrapper=True, EmbeddedClipUnchanged=True, " +
                "OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True, " +
                "SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Shield Bash Scale")]
        public static void InspectKursaShieldBashScale()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var clip = RequireEmbeddedClip(ImportedClipName);
            var slotNames = new[]
            {
                StaticSlotName,
                "Kursa_03_Move",
                ShieldBashSlotName
            };
            foreach (var slotName in slotNames)
            {
                var slot = RequireChild(placement.transform, slotName);
                var model = RequireModel(slot);
                var renderer = RequireRenderer(model, slotName);
                var rootBone = renderer.rootBone ?? throw new InvalidOperationException(
                    slotName + " root bone is missing.");
                Debug.Log(
                    "KursaScaleInspection Slot=" + slotName +
                    ", SlotLocalScale=" + Format(slot.localScale) +
                    ", SlotLossyScale=" + Format(slot.lossyScale) +
                    ", ModelLocalScale=" + Format(model.localScale) +
                    ", ModelLossyScale=" + Format(model.lossyScale) +
                    ", RendererLocalScale=" + Format(renderer.transform.localScale) +
                    ", RendererLossyScale=" + Format(renderer.transform.lossyScale) +
                    ", RootBoneLocalScale=" + Format(rootBone.localScale) +
                    ", RootBoneLossyScale=" + Format(rootBone.lossyScale) +
                    ", RendererBoundsSize=" + Format(renderer.bounds.size) +
                    ", MeshBoundsSize=" + Format(renderer.sharedMesh.bounds.size) +
                    ", NonUnitScaleChain=" + DescribeNonUnitScaleChain(model));
            }
            Debug.Log(
                "KursaScaleInspection ClipScaleCurves=" +
                DescribeTransformScaleCurves(clip));
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Bash Diagnostic")]
        public static void CaptureKursaShieldBashDiagnostic()
        {
            var destination = NextDiagnosticPath();
            var cameraYaw = destination.EndsWith(
                "_02.png",
                StringComparison.Ordinal) ? 40f : 0f;
            CaptureShieldBashReview(destination, "Diagnostic", cameraYaw);
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Bash Final Review")]
        public static void CaptureKursaShieldBashFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Kursa shield-bash final review already exists: " +
                    FinalReviewPath);
            }
            CaptureShieldBashReview(destination, "FinalReview", 40f);
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Bash Scale Final Review")]
        public static void CaptureKursaShieldBashScaleFinalReview()
        {
            var destination = Absolute(ScaleFinalReviewPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Kursa shield-bash scale final review already exists: " +
                    ScaleFinalReviewPath);
            }
            CaptureShieldBashReview(
                destination,
                "ScaleFinalReview",
                40f,
                useSharedScaleFraming: true);
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Bash Scale Diagnostic")]
        public static void CaptureKursaShieldBashScaleDiagnostic()
        {
            var destination = Absolute(ScaleDiagnosticPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The approved Kursa shield-bash scale diagnostic already exists: " +
                    ScaleDiagnosticPath);
            var failedFinal = Absolute(ScaleFinalReviewPath);
            if (File.Exists(failedFinal)) File.Delete(failedFinal);
            CaptureShieldBashReview(
                destination,
                "ScaleDiagnostic",
                40f,
                useSharedScaleFraming: true);
        }

        private static string ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(SourceModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Kursa shield-bash FBX importer is unavailable.");
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
            {
                throw new InvalidOperationException(
                    "The Kursa shield-bash FBX must expose exactly one Mixamo take. " +
                    "Matches=" + matches.Length + ", Defaults=" +
                    string.Join("|", defaults.Select(item =>
                        item.name + ":" + item.takeName)) + ".");
            }

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
                    "shield-bash clip. Clips=" +
                    string.Join("|", clips.Select(item => item.name)) + ".");
            }
            return clips[0];
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Kursa shield-bash controller could not be replaced.");
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(
                "KursaShieldBashMixamoLoop");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
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
                {
                    throw new InvalidOperationException(
                        "Kursa shield-bash FBX Animator root is not exact.");
                }
                animator = animators[0];
            }
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
        }

        private static void RequireExactRigCompatibility(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer sourceRenderer)
        {
            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            var sourceMesh = sourceRenderer.sharedMesh ??
                throw new InvalidOperationException("Shield-bash source mesh is missing.");
            if (staticRenderer.rootBone == null || sourceRenderer.rootBone == null)
                throw new InvalidOperationException("A Kursa root bone is missing.");

            var sourceIndices = UniqueBoneIndices(
                sourceRenderer.bones,
                "shield-bash source");
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
                {
                    throw new InvalidOperationException(
                        "Shield-bash source is missing exact static bone: " +
                        item.Key + ".");
                }
                if (!MatrixMatches(
                    staticMesh.bindposes[item.Value],
                    sourceMesh.bindposes[sourceIndex]))
                {
                    throw new InvalidOperationException(
                        "Shield-bash source bind pose differs for exact static bone: " +
                        item.Key + ".");
                }
            }
            if (!string.Equals(
                staticRenderer.rootBone.name,
                sourceRenderer.rootBone.name,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Shield-bash source root bone differs from the static Kursa root bone.");
            }
        }

        private static void ApplyExactStaticAppearance(
            Transform replacement,
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer staticRenderer)
        {
            RequireExactRigCompatibility(staticRenderer, replacementRenderer);
            var replacementBones = replacement.GetComponentsInChildren<Transform>(true);
            var byName = UniqueTransforms(replacementBones, "shield-bash replacement");
            var mappedBones = staticRenderer.bones.Select(staticBone =>
            {
                if (!byName.TryGetValue(staticBone.name, out var mapped))
                {
                    throw new InvalidOperationException(
                        "Shield-bash replacement is missing exact static bone: " +
                        staticBone.name + ".");
                }
                return mapped;
            }).ToArray();
            if (!byName.TryGetValue(staticRenderer.rootBone.name, out var mappedRoot))
            {
                throw new InvalidOperationException(
                    "Shield-bash replacement is missing the exact static root bone.");
            }

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
            Transform replacement,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip sourceClip,
            AnimatorController controller)
        {
            var renderer = RequireRenderer(replacement, ShieldBashSlotName);
            if (renderer.sharedMesh != staticRenderer.sharedMesh ||
                !renderer.sharedMaterials.SequenceEqual(staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Placed shield-bash appearance does not share the static Kursa assets.");
            }
            var expectedBoneNames = staticRenderer.bones.Select(item => item.name);
            if (!renderer.bones.Select(item => item.name).SequenceEqual(expectedBoneNames))
            {
                throw new InvalidOperationException(
                    "Placed shield-bash skin bone order differs from the static Kursa.");
            }
            var animator = replacement.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Placed shield-bash model must contain one Animator.");
            if (animator.transform == replacement ||
                animator.transform.parent != replacement ||
                !string.Equals(
                    animator.transform.name,
                    AnimatedRootName,
                    StringComparison.Ordinal) ||
                animator.transform.localPosition != Vector3.zero ||
                animator.transform.localRotation != Quaternion.identity ||
                animator.transform.localScale != Vector3.one)
            {
                throw new InvalidOperationException(
                    "Placed shield-bash animated root is not isolated below its scale wrapper.");
            }
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Placed shield-bash Animator configuration differs.");
            }
            if (!AnimationUtility.GetAnimationClipSettings(sourceClip).loopTime)
                throw new InvalidOperationException("Shield-bash Mixamo clip is not looping.");
            RequireClipBindings(animator.transform, sourceClip);
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
            {
                throw new InvalidOperationException(
                    "Shield-bash Mixamo clip paths do not exactly match the FBX hierarchy: " +
                    string.Join("|", missing) + ".");
            }
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

        private static void CaptureShieldBashReview(
            string destination,
            string captureKind,
            float cameraYaw,
            bool useSharedScaleFraming = false)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var bashModel = RequireModel(RequireChild(
                placement.transform,
                ShieldBashSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var bashRenderer = RequireRenderer(bashModel, ShieldBashSlotName);
            var clip = RequireEmbeddedClip(ImportedClipName);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Kursa shield-bash controller is missing.");
            RequirePlacedContract(bashModel, staticRenderer, clip, controller);
            CaptureContactSheet(
                scene,
                staticModel,
                staticRenderer,
                bashModel,
                bashRenderer,
                clip,
                cameraYaw,
                destination,
                useSharedScaleFraming);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa shield-bash capture changed the scene dirty state.");
            Debug.Log(
                "KursaShieldBashReviewCaptured Kind=" + captureKind +
                ", FullLoop=True, StaticAppearanceReference=True" +
                ", DirectVisualReviewRequired=True, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            Transform bashModel,
            SkinnedMeshRenderer bashRenderer,
            AnimationClip clip,
            float cameraYaw,
            string destination,
            bool useSharedScaleFraming)
        {
            const int panelWidth = 360;
            const int panelHeight = 360;
            const int columns = 5;
            const int rows = 4;
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererState(item))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject(
                "KursaShieldBashReviewCamera",
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
            var animator = bashModel.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Kursa shield-bash Animator is missing during capture.");
            var animatorEnabled = animator != null && animator.enabled;
            var snapshots = bashModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                if (animator != null) animator.enabled = false;
                var fixedBashBounds = FullLoopBounds(
                    animator.transform,
                    bashRenderer,
                    clip,
                    snapshots);
                var staticFramingBounds = staticRenderer.bounds;
                var bashFramingBounds = fixedBashBounds;
                if (useSharedScaleFraming)
                {
                    var sharedSize = Vector3.Max(
                        staticFramingBounds.size,
                        bashFramingBounds.size);
                    staticFramingBounds = new Bounds(
                        staticFramingBounds.center,
                        sharedSize);
                    bashFramingBounds = new Bounds(
                        bashFramingBounds.center,
                        sharedSize);
                }
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 28f;
                camera.targetTexture = target;

                RenderSubjectPanel(
                    camera,
                    staticModel,
                    staticRenderer,
                    sceneRenderers,
                    target,
                    panel,
                    cameraYaw,
                    staticFramingBounds);
                CopyPanel(panel, grid, 0, rows - 1, panelWidth, panelHeight);

                for (var index = 0; index < ReviewFractions.Length; index++)
                {
                    foreach (var snapshot in snapshots) snapshot.Restore();
                    clip.SampleAnimation(
                        animator.gameObject,
                        clip.length * ReviewFractions[index]);
                    RenderSubjectPanel(
                        camera,
                        bashModel,
                        bashRenderer,
                        sceneRenderers,
                        target,
                        panel,
                        cameraYaw,
                        bashFramingBounds);
                    var panelIndex = index + 1;
                    var column = panelIndex % columns;
                    var row = rows - 1 - panelIndex / columns;
                    CopyPanel(panel, grid, column, row, panelWidth, panelHeight);
                }
                grid.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa shield-bash capture folder."));
                File.WriteAllBytes(destination, grid.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
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

        private static void RenderSubjectPanel(
            Camera camera,
            Transform model,
            Renderer renderer,
            IEnumerable<Renderer> sceneRenderers,
            RenderTexture target,
            Texture2D panel,
            float cameraYaw,
            Bounds fixedBounds)
        {
            foreach (var sceneRenderer in sceneRenderers)
                sceneRenderer.enabled = sceneRenderer.transform.IsChildOf(model);
            FrameCamera(
                camera,
                model,
                fixedBounds,
                target.width / (float)target.height,
                cameraYaw);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            panel.Apply();
        }

        private static Bounds FullLoopBounds(
            Transform animationRoot,
            Renderer renderer,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var fraction in ReviewFractions)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(animationRoot.gameObject, clip.length * fraction);
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
            if (!initialized)
                throw new InvalidOperationException(
                    "Kursa shield-bash full-loop bounds are unavailable.");
            return result;
        }

        private static void FrameCamera(
            Camera camera,
            Transform model,
            Bounds bounds,
            float aspect,
            float cameraYaw)
        {
            var direction = Quaternion.AngleAxis(cameraYaw, model.up) *
                model.forward.normalized;
            var vertical = bounds.extents.y /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position = bounds.center + direction * distance +
                Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void CopyPanel(
            Texture2D panel,
            Texture2D grid,
            int column,
            int row,
            int panelWidth,
            int panelHeight)
        {
            grid.SetPixels(
                column * panelWidth,
                row * panelHeight,
                panelWidth,
                panelHeight,
                panel.GetPixels());
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 3; index++)
            {
                var candidate = Absolute(string.Format(
                    DiagnosticPathFormat,
                    index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The approved Kursa shield-bash diagnostic captures already exist.");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on the Kursa shield bash.");
            }
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item =>
                string.Equals(
                    item.name,
                    PlacementRootName,
                    StringComparison.Ordinal)) ??
            throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (!string.Equals(slot.name, SlotNames[index], StringComparison.Ordinal) ||
                    slot.childCount != 1 ||
                    !string.Equals(
                        slot.GetChild(0).name,
                        ModelName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at " + index + ".");
                }
            }
        }

        private static Transform RequireChild(Transform parent, string childName)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(item => string.Equals(
                    item.name,
                    childName,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Required direct child differs: " + childName + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 ||
                !string.Equals(
                    slot.GetChild(0).name,
                    ModelName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    slot.name + " model contract differs.");
            }
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(
            Transform model,
            string context) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    context + " must contain one skinned renderer.");

        private static string[] OtherSlotSignatures(Transform placement) =>
            SlotNames.Where(item => item != ShieldBashSlotName)
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
                        builder.Append(':')
                            .Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':')
                            .Append(AssetDatabase.GetAssetPath(material));
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

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private static string DescribeNonUnitScaleChain(Transform model) =>
            string.Join(
                ";",
                model.GetComponentsInChildren<Transform>(true)
                    .Where(item => Vector3.SqrMagnitude(item.localScale - Vector3.one) >
                        0.00000001f)
                    .Select(item => AnimationUtility.CalculateTransformPath(item, model) +
                        "=" + Format(item.localScale)));

        private static string DescribeTransformScaleCurves(AnimationClip clip) =>
            string.Join(
                ";",
                AnimationUtility.GetCurveBindings(clip)
                    .Where(item => item.type == typeof(Transform) &&
                        item.propertyName.IndexOf(
                            "m_LocalScale",
                            StringComparison.Ordinal) >= 0)
                    .Select(item =>
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, item);
                        return (string.IsNullOrEmpty(item.path) ? "<model-root>" : item.path) +
                            "/" + item.propertyName + "=" +
                            string.Join(",", curve.keys.Select(key =>
                                key.value.ToString("0.########")));
                    }));

        private static string Format(Vector3 value) =>
            "(" + value.x.ToString("0.########") + "," +
            value.y.ToString("0.########") + "," +
            value.z.ToString("0.########") + ")";

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer rendererValue)
            {
                renderer = rendererValue;
                enabled = rendererValue.enabled;
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

            public TransformSnapshot(Transform transformValue)
            {
                transform = transformValue;
                position = transformValue.localPosition;
                rotation = transformValue.localRotation;
                scale = transformValue.localScale;
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
