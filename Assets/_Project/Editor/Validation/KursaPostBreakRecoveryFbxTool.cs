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
    internal static class KursaPostBreakRecoveryFbxTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string PreviousRecoverySlotName = "Kursa_06_ShieldStance";
        private const string RecoverySlotName = "Kursa_06_PostBreakRecovery";
        private const string ModelName = "Kursa_Model";
        private const string AnimatedRootName = "Kursa_PostBreakRecovery_AnimatedRoot";
        private const string SourceModelPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Models/Kursa_PostBreakRecovery_Source.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_06_PostBreakRecovery.controller";
        private const string PlaybackClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_06_PostBreakRecovery_Playback.anim";
        private const string ShieldlessMeshPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_06_PostBreakRecovery_Shieldless.asset";
        private const string RegeneratedShieldMeshPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_06_PostBreakRecovery_Shield.asset";
        private const string ImportedClipName = "Kursa_06_PostBreakRecovery_Mixamo";
        private const string PlaybackClipName = "Kursa_06_PostBreakRecovery_Playback";
        private const string RegeneratedShieldRootName = "Kursa_RegeneratedShield";
        private const string ValidationFolder =
            "docs/validation/kursa_post_break_recovery_regeneration_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_PostBreakRecoveryFbx_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_PostBreakRecoveryFbx_FinalReview.png";
        private const float MatrixTolerance = 0.00001f;
        // User-approved sequence timing after the unmodified Mixamo motion.
        private const float StaticRecoverySeconds = 0.3f;
        private const float ShieldRevealSeconds = 0.5f;
        private const float CompletedHoldSeconds = 1f;
        private static readonly int[] ShieldSubmeshes = { 3, 8 };

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

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Post-Break Recovery FBX Replacement")]
        public static void ApplyKursaPostBreakRecoveryFbxReplacement()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var recoverySlot = RequireSlotContractBeforeRename(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var previous = RequireModel(recoverySlot);

            var takeName = ConfigureImporter();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                throw new InvalidOperationException(
                    "Kursa post-break recovery source prefab is missing.");
            var sourceRenderer = RequireRenderer(
                sourcePrefab.transform,
                "Kursa post-break recovery source FBX");
            var sourceClip = RequireEmbeddedClip(takeName);
            RequireExactRigCompatibility(staticRenderer, sourceRenderer);
            RequireClipBindings(sourcePrefab.transform, sourceClip);

            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;

            var wrapper = new GameObject("Kursa_PostBreakRecovery_Wrapper_Pending");
            wrapper.transform.SetParent(recoverySlot, false);
            wrapper.transform.SetLocalPositionAndRotation(
                previousPosition,
                previousRotation);
            wrapper.transform.localScale = staticModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Kursa post-break recovery source FBX could not be instantiated.");
            replacement.name = AnimatedRootName;
            replacement.transform.SetParent(wrapper.transform, false);
            replacement.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            replacement.transform.localScale = Vector3.one;

            AnimationClip playbackClip;
            AnimatorController controller;
            try
            {
                var replacementRenderer = RequireRenderer(
                    wrapper.transform,
                    RecoverySlotName);
                ApplyExactStaticAppearance(
                    wrapper.transform,
                    replacementRenderer,
                    staticRenderer);
                var shieldRoot = CreateRegeneratedShield(
                    replacement.transform,
                    replacementRenderer,
                    staticRenderer);
                playbackClip = CreatePlaybackClip(
                    sourceClip,
                    replacement.transform,
                    staticRenderer,
                    shieldRoot);
                controller = CreateController(playbackClip);
                ConfigureAnimator(replacement, controller);
                RequirePlacedContract(
                    wrapper.transform,
                    staticRenderer,
                    sourceClip,
                    playbackClip,
                    controller);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(wrapper);
                throw;
            }

            recoverySlot.name = RecoverySlotName;
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            wrapper.name = ModelName;
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform),
                "A Kursa slot outside Kursa_06_PostBreakRecovery changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Kursa placement changed.");
            RequireSlotContract(placement.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after replacing Kursa_06_PostBreakRecovery.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log(
                "KursaPostBreakRecoveryFbxReplacementApplied Result=PASS, " +
                "Slot=Kursa_06_PostBreakRecovery, Source=" + SourceModelPath +
                ", MixamoTake=" + takeName +
                ", ExactStaticDerivedMesh=True, ExactStaticUv=True, ExactStaticSkin=True" +
                ", ExactStaticMaterials=True, ShieldRemoved=True, ShieldSubmeshes=3|8" +
                ", StaticRecoverySeconds=" + StaticRecoverySeconds +
                ", ShieldRevealSeconds=" + ShieldRevealSeconds +
                ", CompletedHoldSeconds=" + CompletedHoldSeconds +
                ", CenterOutShieldRegeneration=True, Loop=True, RootMotion=False" +
                ", OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Post-Break Recovery FBX Diagnostic")]
        public static void CaptureKursaPostBreakRecoveryFbxDiagnostic()
        {
            var destination = NextDiagnosticPath();
            var cameraYaw = destination.EndsWith(
                "_02.png",
                StringComparison.Ordinal) ? 40f : 0f;
            CapturePostBreakRecoveryReview(destination, "Diagnostic", cameraYaw);
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Post-Break Recovery FBX Final Review")]
        public static void CaptureKursaPostBreakRecoveryFbxFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Kursa post-break recovery final review already exists: " +
                    FinalReviewPath);
            }
            CapturePostBreakRecoveryReview(destination, "FinalReview", 40f);
        }

        private static string ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(SourceModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Kursa post-break recovery FBX importer is unavailable.");
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
                    "The Kursa post-break recovery FBX must expose exactly one Mixamo take. " +
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
                    "post-break recovery clip. Clips=" +
                    string.Join("|", clips.Select(item => item.name)) + ".");
            }
            return clips[0];
        }

        private static AnimationClip CreatePlaybackClip(
            AnimationClip source,
            Transform animationRoot,
            SkinnedMeshRenderer staticRenderer,
            Transform shieldRoot)
        {
            if (source.length <= 0f)
                throw new InvalidOperationException(
                    "The Mixamo post-break recovery clip has no duration.");
            DeleteGeneratedAsset(PlaybackClipPath);
            var sourceEnd = source.length;
            var recoveryEnd = sourceEnd + StaticRecoverySeconds;
            var revealEnd = recoveryEnd + ShieldRevealSeconds;
            var totalEnd = revealEnd + CompletedHoldSeconds;
            var playback = new AnimationClip
            {
                name = PlaybackClipName,
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop
            };
            var sourceBindings = AnimationUtility.GetCurveBindings(source);
            var sourceCurves = sourceBindings
                .Where(binding => binding.type == typeof(Transform))
                .ToDictionary(
                binding => CurveKey(binding.path, binding.propertyName),
                binding => AnimationUtility.GetEditorCurve(source, binding),
                StringComparer.Ordinal);
            var staticByName = UniqueTransforms(
                staticRenderer.bones,
                "static Kursa recovery pose");

            foreach (var binding in sourceBindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                var curve = new AnimationCurve(sourceCurve.keys)
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
                var endValue = sourceCurve.Evaluate(sourceEnd);
                var targetValue = endValue;
                if (binding.type == typeof(Transform) &&
                    !string.IsNullOrEmpty(binding.path))
                {
                    var animatedTransform = animationRoot.Find(binding.path);
                    if (animatedTransform != null &&
                        staticByName.TryGetValue(
                            animatedTransform.name,
                            out var staticTransform))
                    {
                        targetValue = StaticPoseCurveValue(
                            binding,
                            staticTransform,
                            sourceCurves,
                            sourceEnd,
                            endValue);
                    }
                }
                var sourceIndex = SetCurveKey(curve, sourceEnd, endValue);
                var recoveryIndex = SetCurveKey(
                    curve,
                    recoveryEnd,
                    targetValue);
                var totalIndex = SetCurveKey(curve, totalEnd, targetValue);
                SetLinearTangent(curve, sourceIndex);
                SetLinearTangent(curve, recoveryIndex);
                SetConstantTangent(curve, totalIndex);
                AnimationUtility.SetEditorCurve(playback, binding, curve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                AnimationUtility.SetObjectReferenceCurve(
                    playback,
                    binding,
                    AnimationUtility.GetObjectReferenceCurve(source, binding));
            }
            AnimationUtility.SetAnimationEvents(
                playback,
                AnimationUtility.GetAnimationEvents(source));

            var shieldPath = AnimationUtility.CalculateTransformPath(
                shieldRoot,
                animationRoot);
            foreach (var axis in new[] { "x", "y", "z" })
            {
                var binding = EditorCurveBinding.FloatCurve(
                    shieldPath,
                    typeof(Transform),
                    "m_LocalScale." + axis);
                var curve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(sourceEnd, 0f),
                    new Keyframe(recoveryEnd, 0f),
                    new Keyframe(revealEnd, 1f),
                    new Keyframe(totalEnd, 1f));
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve,
                        index,
                        AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve,
                        index,
                        AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(playback, binding, curve);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(playback);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(playback, settings);
            AssetDatabase.CreateAsset(playback, PlaybackClipPath);
            AssetDatabase.SaveAssets();
            return playback;
        }

        private static float StaticPoseCurveValue(
            EditorCurveBinding binding,
            Transform staticTransform,
            IReadOnlyDictionary<string, AnimationCurve> sourceCurves,
            float sourceEnd,
            float endValue)
        {
            var property = binding.propertyName;
            if (property.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
                return VectorComponent(staticTransform.localPosition, property);
            if (property.StartsWith("m_LocalScale.", StringComparison.Ordinal))
                return VectorComponent(staticTransform.localScale, property);
            if (property.StartsWith("m_LocalRotation.", StringComparison.Ordinal))
            {
                var target = staticTransform.localRotation;
                var end = new Quaternion(
                    CurveValue(sourceCurves, binding.path, "m_LocalRotation.x", sourceEnd),
                    CurveValue(sourceCurves, binding.path, "m_LocalRotation.y", sourceEnd),
                    CurveValue(sourceCurves, binding.path, "m_LocalRotation.z", sourceEnd),
                    CurveValue(sourceCurves, binding.path, "m_LocalRotation.w", sourceEnd));
                if (Quaternion.Dot(end, target) < 0f)
                {
                    target.x = -target.x;
                    target.y = -target.y;
                    target.z = -target.z;
                    target.w = -target.w;
                }
                return QuaternionComponent(target, property);
            }
            if (property.IndexOf("localEulerAngles", StringComparison.Ordinal) >= 0)
            {
                var target = VectorComponent(staticTransform.localEulerAngles, property);
                return endValue + Mathf.DeltaAngle(endValue, target);
            }
            return endValue;
        }

        private static float CurveValue(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            string path,
            string property,
            float time)
        {
            return curves.TryGetValue(CurveKey(path, property), out var curve)
                ? curve.Evaluate(time)
                : property.EndsWith(".w", StringComparison.Ordinal) ? 1f : 0f;
        }

        private static string CurveKey(string path, string property) =>
            path + "\u001f" + property;

        private static float VectorComponent(Vector3 value, string property)
        {
            if (property.EndsWith(".x", StringComparison.Ordinal)) return value.x;
            if (property.EndsWith(".y", StringComparison.Ordinal)) return value.y;
            if (property.EndsWith(".z", StringComparison.Ordinal)) return value.z;
            throw new InvalidOperationException(
                "Unsupported vector animation property: " + property + ".");
        }

        private static float QuaternionComponent(Quaternion value, string property)
        {
            if (property.EndsWith(".x", StringComparison.Ordinal)) return value.x;
            if (property.EndsWith(".y", StringComparison.Ordinal)) return value.y;
            if (property.EndsWith(".z", StringComparison.Ordinal)) return value.z;
            if (property.EndsWith(".w", StringComparison.Ordinal)) return value.w;
            throw new InvalidOperationException(
                "Unsupported quaternion animation property: " + property + ".");
        }

        private static int SetCurveKey(AnimationCurve curve, float time, float value)
        {
            for (var index = 0; index < curve.length; index++)
            {
                if (Mathf.Abs(curve[index].time - time) > 0.00001f) continue;
                return curve.MoveKey(index, new Keyframe(time, value));
            }
            return curve.AddKey(new Keyframe(time, value));
        }

        private static void SetLinearTangent(AnimationCurve curve, int index)
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

        private static void SetConstantTangent(AnimationCurve curve, int index)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                index,
                AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(
                curve,
                index,
                AnimationUtility.TangentMode.Constant);
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Kursa post-break recovery controller could not be replaced.");
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(
                "KursaPostBreakRecoveryPlaybackLoop");
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
                        "Kursa post-break recovery FBX Animator root is not exact.");
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
                throw new InvalidOperationException(
                    "Post-break recovery source mesh is missing.");
            if (staticRenderer.rootBone == null || sourceRenderer.rootBone == null)
                throw new InvalidOperationException("A Kursa root bone is missing.");

            var sourceIndices = UniqueBoneIndices(
                sourceRenderer.bones,
                "post-break recovery source");
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
                        "Post-break recovery source is missing exact static bone: " +
                        item.Key + ".");
                }
                if (!MatrixMatches(
                    staticMesh.bindposes[item.Value],
                    sourceMesh.bindposes[sourceIndex]))
                {
                    throw new InvalidOperationException(
                        "Post-break recovery source bind pose differs for exact static bone: " +
                        item.Key + ".");
                }
            }
            if (!string.Equals(
                staticRenderer.rootBone.name,
                sourceRenderer.rootBone.name,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Post-break recovery source root bone differs from the static Kursa root bone.");
            }
        }

        private static void ApplyExactStaticAppearance(
            Transform replacement,
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer staticRenderer)
        {
            RequireExactRigCompatibility(staticRenderer, replacementRenderer);
            var replacementBones = replacement.GetComponentsInChildren<Transform>(true);
            var byName = UniqueTransforms(
                replacementBones,
                "post-break recovery replacement");
            var mappedBones = staticRenderer.bones.Select(staticBone =>
            {
                if (!byName.TryGetValue(staticBone.name, out var mapped))
                {
                    throw new InvalidOperationException(
                        "Post-break recovery replacement is missing exact static bone: " +
                        staticBone.name + ".");
                }
                return mapped;
            }).ToArray();
            if (!byName.TryGetValue(staticRenderer.rootBone.name, out var mappedRoot))
            {
                throw new InvalidOperationException(
                    "Post-break recovery replacement is missing the exact static root bone.");
            }

            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            if (ShieldSubmeshes.Any(index => index < 0 || index >= staticMesh.subMeshCount))
            {
                throw new InvalidOperationException(
                    "The inspected static shield submesh contract no longer matches.");
            }
            var staticMaterials = staticRenderer.sharedMaterials;
            if (staticMaterials.Length != staticMesh.subMeshCount ||
                staticMaterials.Any(item => item == null))
            {
                throw new InvalidOperationException(
                    "Static Kursa material slots are incomplete.");
            }

            DeleteGeneratedAsset(ShieldlessMeshPath);
            var shieldlessMesh = UnityEngine.Object.Instantiate(staticMesh);
            shieldlessMesh.name = "Kursa_06_PostBreakRecovery_Shieldless";
            foreach (var submesh in ShieldSubmeshes)
                shieldlessMesh.SetTriangles(Array.Empty<int>(), submesh, false);
            AssetDatabase.CreateAsset(shieldlessMesh, ShieldlessMeshPath);

            replacementRenderer.sharedMesh = shieldlessMesh;
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

        private static Transform CreateRegeneratedShield(
            Transform animationRoot,
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer staticRenderer)
        {
            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            var leftHandIndex = Array.FindIndex(
                staticRenderer.bones,
                item => item != null && string.Equals(
                    item.name,
                    "LeftHand",
                    StringComparison.Ordinal));
            if (leftHandIndex < 0)
                throw new InvalidOperationException("Static Kursa LeftHand bone is missing.");
            var shieldVertexIndices = ShieldSubmeshes
                .SelectMany(staticMesh.GetTriangles)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();
            RequireRigidLeftHandShield(staticMesh, shieldVertexIndices, leftHandIndex);
            if (leftHandIndex >= replacementRenderer.bones.Length ||
                replacementRenderer.bones[leftHandIndex] == null)
            {
                throw new InvalidOperationException(
                    "Replacement Kursa LeftHand bone is missing.");
            }

            DeleteGeneratedAsset(RegeneratedShieldMeshPath);
            var shieldMesh = CreateRigidShieldMesh(
                staticMesh,
                staticMesh.bindposes[leftHandIndex],
                shieldVertexIndices,
                out var shieldCenter);
            AssetDatabase.CreateAsset(shieldMesh, RegeneratedShieldMeshPath);

            var shieldRoot = new GameObject(RegeneratedShieldRootName).transform;
            shieldRoot.SetParent(replacementRenderer.bones[leftHandIndex], false);
            shieldRoot.localPosition = shieldCenter;
            shieldRoot.localRotation = Quaternion.identity;
            shieldRoot.localScale = Vector3.zero;
            var filter = shieldRoot.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = shieldMesh;
            var renderer = shieldRoot.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = ShieldSubmeshes
                .Select(index => staticRenderer.sharedMaterials[index])
                .ToArray();
            CopyRendererSettings(staticRenderer, renderer);

            var path = AnimationUtility.CalculateTransformPath(
                shieldRoot,
                animationRoot);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException(
                    "Regenerated shield animation path is empty.");
            return shieldRoot;
        }

        private static Mesh CreateRigidShieldMesh(
            Mesh source,
            Matrix4x4 bindpose,
            IReadOnlyList<int> shieldVertexIndices,
            out Vector3 center)
        {
            var sourceVertices = source.vertices;
            var sourceNormals = source.normals;
            var sourceTangents = source.tangents;
            if (sourceNormals.Length != source.vertexCount ||
                sourceTangents.Length != source.vertexCount)
            {
                throw new InvalidOperationException(
                    "Static shield normal or tangent channels are incomplete.");
            }

            var bounds = new Bounds(
                bindpose.MultiplyPoint3x4(sourceVertices[shieldVertexIndices[0]]),
                Vector3.zero);
            foreach (var index in shieldVertexIndices.Skip(1))
                bounds.Encapsulate(bindpose.MultiplyPoint3x4(sourceVertices[index]));
            center = bounds.center;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var submeshIndices = ShieldSubmeshes
                .Select((_, index) => new List<int>())
                .ToArray();
            var sourceUvs = new List<Vector4>[8];
            var resultUvs = new List<Vector4>[8];
            for (var channel = 0; channel < sourceUvs.Length; channel++)
            {
                sourceUvs[channel] = new List<Vector4>();
                source.GetUVs(channel, sourceUvs[channel]);
                if (sourceUvs[channel].Count == source.vertexCount)
                    resultUvs[channel] = new List<Vector4>();
            }
            var sourceColors = source.colors32;
            var resultColors = sourceColors.Length == source.vertexCount
                ? new List<Color32>()
                : null;

            for (var materialIndex = 0; materialIndex < ShieldSubmeshes.Length;
                materialIndex++)
            {
                var triangles = source.GetTriangles(ShieldSubmeshes[materialIndex]);
                if (triangles.Length % 3 != 0)
                    throw new InvalidOperationException(
                        "Static shield triangles are malformed.");
                foreach (var sourceIndex in triangles)
                {
                    var vertexIndex = vertices.Count;
                    vertices.Add(
                        bindpose.MultiplyPoint3x4(sourceVertices[sourceIndex]) - center);
                    normals.Add(
                        bindpose.MultiplyVector(sourceNormals[sourceIndex]).normalized);
                    var sourceTangent = sourceTangents[sourceIndex];
                    var tangentDirection = bindpose.MultiplyVector(new Vector3(
                        sourceTangent.x,
                        sourceTangent.y,
                        sourceTangent.z)).normalized;
                    tangents.Add(new Vector4(
                        tangentDirection.x,
                        tangentDirection.y,
                        tangentDirection.z,
                        sourceTangent.w));
                    for (var channel = 0; channel < resultUvs.Length; channel++)
                    {
                        if (resultUvs[channel] != null)
                            resultUvs[channel].Add(sourceUvs[channel][sourceIndex]);
                    }
                    if (resultColors != null)
                        resultColors.Add(sourceColors[sourceIndex]);
                    submeshIndices[materialIndex].Add(vertexIndex);
                }
            }

            var result = new Mesh
            {
                name = "Kursa_06_PostBreakRecovery_Shield",
                indexFormat = vertices.Count > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            result.SetVertices(vertices);
            result.SetNormals(normals);
            result.SetTangents(tangents);
            for (var channel = 0; channel < resultUvs.Length; channel++)
            {
                if (resultUvs[channel] != null)
                    result.SetUVs(channel, resultUvs[channel]);
            }
            if (resultColors != null) result.SetColors(resultColors);
            result.subMeshCount = ShieldSubmeshes.Length;
            for (var index = 0; index < submeshIndices.Length; index++)
                result.SetTriangles(submeshIndices[index], index, false);
            result.RecalculateBounds();
            return result;
        }

        private static void RequireRigidLeftHandShield(
            Mesh mesh,
            IEnumerable<int> shieldVertexIndices,
            int leftHandIndex)
        {
            var weights = mesh.boneWeights;
            if (weights.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "Static Kursa shield bone weights are unavailable.");
            foreach (var vertexIndex in shieldVertexIndices)
            {
                var weight = weights[vertexIndex];
                var leftWeight = 0f;
                var otherWeight = 0f;
                Accumulate(weight.boneIndex0, weight.weight0);
                Accumulate(weight.boneIndex1, weight.weight1);
                Accumulate(weight.boneIndex2, weight.weight2);
                Accumulate(weight.boneIndex3, weight.weight3);
                if (leftWeight < 0.9999f || otherWeight > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Static shield is not rigidly weighted to LeftHand at vertex " +
                        vertexIndex + ".");
                }

                void Accumulate(int boneIndex, float value)
                {
                    if (value <= 0f) return;
                    if (boneIndex == leftHandIndex) leftWeight += value;
                    else otherWeight += value;
                }
            }
        }

        private static void CopyRendererSettings(
            SkinnedMeshRenderer source,
            MeshRenderer destination)
        {
            destination.shadowCastingMode = source.shadowCastingMode;
            destination.receiveShadows = source.receiveShadows;
            destination.lightProbeUsage = source.lightProbeUsage;
            destination.reflectionProbeUsage = source.reflectionProbeUsage;
            destination.renderingLayerMask = source.renderingLayerMask;
            destination.motionVectorGenerationMode =
                source.motionVectorGenerationMode;
            var propertyBlock = new MaterialPropertyBlock();
            source.GetPropertyBlock(propertyBlock);
            destination.SetPropertyBlock(propertyBlock);
        }

        private static void DeleteGeneratedAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(
                    "Generated asset could not be replaced: " + path);
            }
        }

        private static void RequireShieldlessMeshContract(
            Mesh source,
            Mesh shieldless)
        {
            if (source == null ||
                source.vertexCount != shieldless.vertexCount ||
                source.subMeshCount != shieldless.subMeshCount ||
                !source.vertices.SequenceEqual(shieldless.vertices) ||
                !source.normals.SequenceEqual(shieldless.normals) ||
                !source.tangents.SequenceEqual(shieldless.tangents) ||
                !source.uv.SequenceEqual(shieldless.uv) ||
                !source.bindposes.SequenceEqual(shieldless.bindposes) ||
                !source.boneWeights.SequenceEqual(shieldless.boneWeights))
            {
                throw new InvalidOperationException(
                    "Slot 6 shieldless body changed static geometry channels.");
            }
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                var expected = ShieldSubmeshes.Contains(submesh)
                    ? Array.Empty<int>()
                    : source.GetTriangles(submesh);
                if (!expected.SequenceEqual(shieldless.GetTriangles(submesh)))
                {
                    throw new InvalidOperationException(
                        "Slot 6 shieldless body submesh contract differs at " +
                        submesh + ".");
                }
            }
        }

        private static void RequirePlacedContract(
            Transform replacement,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip sourceClip,
            AnimationClip playbackClip,
            AnimatorController controller)
        {
            var renderer = RequireRenderer(replacement, RecoverySlotName);
            var expectedShieldless = AssetDatabase.LoadAssetAtPath<Mesh>(
                ShieldlessMeshPath) ?? throw new InvalidOperationException(
                "Placed post-break recovery shieldless mesh asset is missing.");
            if (renderer.sharedMesh != expectedShieldless ||
                !renderer.sharedMaterials.SequenceEqual(staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Placed post-break recovery appearance does not use its exact static-derived assets.");
            }
            RequireShieldlessMeshContract(staticRenderer.sharedMesh, expectedShieldless);
            var expectedBoneNames = staticRenderer.bones.Select(item => item.name);
            if (!renderer.bones.Select(item => item.name).SequenceEqual(expectedBoneNames))
            {
                throw new InvalidOperationException(
                    "Placed post-break recovery skin bone order differs from the static Kursa.");
            }
            var animator = replacement.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Placed post-break recovery model must contain one Animator.");
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
                    "Placed post-break recovery animated root is not isolated below its scale wrapper.");
            }
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Placed post-break recovery Animator configuration differs.");
            }
            if (!AnimationUtility.GetAnimationClipSettings(playbackClip).loopTime)
                throw new InvalidOperationException(
                    "Post-break recovery playback clip is not looping.");
            if (!controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Any(state => state.state.motion == playbackClip))
            {
                throw new InvalidOperationException(
                    "Post-break recovery controller does not use the playback clip.");
            }
            RequireClipBindings(animator.transform, sourceClip);
            RequireClipBindings(animator.transform, playbackClip);
            RequireRecoveryTimingContract(
                animator.transform,
                staticRenderer,
                sourceClip,
                playbackClip);
        }

        private static void RequireRecoveryTimingContract(
            Transform animationRoot,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip sourceClip,
            AnimationClip playbackClip)
        {
            var recoveryEnd = sourceClip.length + StaticRecoverySeconds;
            var revealEnd = recoveryEnd + ShieldRevealSeconds;
            var expectedEnd = revealEnd + CompletedHoldSeconds;
            if (Mathf.Abs(playbackClip.length - expectedEnd) > 0.001f)
                throw new InvalidOperationException(
                    "Post-break recovery playback duration differs.");

            var shieldRoots = animationRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    item.name,
                    RegeneratedShieldRootName,
                    StringComparison.Ordinal))
                .ToArray();
            if (shieldRoots.Length != 1 ||
                shieldRoots[0].parent == null ||
                !string.Equals(
                    shieldRoots[0].parent.name,
                    "LeftHand",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Regenerated shield must be attached once to LeftHand.");
            }
            var shieldRoot = shieldRoots[0];
            var shieldMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                RegeneratedShieldMeshPath) ?? throw new InvalidOperationException(
                "Regenerated shield mesh asset is missing.");
            var filter = shieldRoot.GetComponent<MeshFilter>();
            var shieldRenderer = shieldRoot.GetComponent<MeshRenderer>();
            var expectedMaterials = ShieldSubmeshes
                .Select(index => staticRenderer.sharedMaterials[index])
                .ToArray();
            if (filter == null || filter.sharedMesh != shieldMesh ||
                shieldRenderer == null ||
                !shieldRenderer.sharedMaterials.SequenceEqual(expectedMaterials) ||
                shieldMesh.subMeshCount != ShieldSubmeshes.Length)
            {
                throw new InvalidOperationException(
                    "Regenerated shield appearance contract differs.");
            }
            for (var index = 0; index < ShieldSubmeshes.Length; index++)
            {
                if (shieldMesh.GetTriangles(index).Length !=
                    staticRenderer.sharedMesh.GetTriangles(ShieldSubmeshes[index]).Length)
                {
                    throw new InvalidOperationException(
                        "Regenerated shield triangle contract differs at " + index + ".");
                }
            }

            var shieldPath = AnimationUtility.CalculateTransformPath(
                shieldRoot,
                animationRoot);
            foreach (var axis in new[] { "x", "y", "z" })
            {
                var binding = EditorCurveBinding.FloatCurve(
                    shieldPath,
                    typeof(Transform),
                    "m_LocalScale." + axis);
                var curve = AnimationUtility.GetEditorCurve(playbackClip, binding) ??
                    throw new InvalidOperationException(
                        "Regenerated shield scale curve is missing: " + axis + ".");
                if (Mathf.Abs(curve.Evaluate(recoveryEnd)) > 0.0001f ||
                    Mathf.Abs(curve.Evaluate(revealEnd) - 1f) > 0.0001f ||
                    Mathf.Abs(curve.Evaluate(expectedEnd) - 1f) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Regenerated shield timing curve differs: " + axis + ".");
                }
            }
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
                    "Post-break recovery Mixamo clip paths do not exactly match the FBX hierarchy: " +
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

        private static void CapturePostBreakRecoveryReview(
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
            var recoveryModel = RequireModel(RequireChild(
                placement.transform,
                RecoverySlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var recoveryRenderer = RequireRenderer(
                recoveryModel,
                RecoverySlotName);
            var sourceClip = RequireEmbeddedClip(ImportedClipName);
            var playbackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                PlaybackClipPath) ?? throw new InvalidOperationException(
                "Kursa post-break recovery playback clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Kursa post-break recovery controller is missing.");
            RequirePlacedContract(
                recoveryModel,
                staticRenderer,
                sourceClip,
                playbackClip,
                controller);
            CaptureContactSheet(
                scene,
                staticModel,
                staticRenderer,
                recoveryModel,
                recoveryRenderer,
                playbackClip,
                cameraYaw,
                destination,
                useSharedScaleFraming);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa post-break recovery capture changed the scene dirty state.");
            Debug.Log(
                "KursaPostBreakRecoveryFbxReviewCaptured Kind=" + captureKind +
                ", FullLoop=True, StaticRecoverySeconds=" + StaticRecoverySeconds +
                ", ShieldRevealSeconds=" + ShieldRevealSeconds +
                ", CompletedHoldSeconds=" + CompletedHoldSeconds +
                ", StaticAppearanceReference=True" +
                ", DirectVisualReviewRequired=True, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            Transform recoveryModel,
            SkinnedMeshRenderer recoveryRenderer,
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
                "KursaPostBreakRecoveryReviewCamera",
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
            var animator = recoveryModel.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Kursa post-break recovery Animator is missing during capture.");
            var animatorEnabled = animator != null && animator.enabled;
            var snapshots = recoveryModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                if (animator != null) animator.enabled = false;
                var fixedBashBounds = FullLoopBounds(
                    animator.transform,
                    recoveryModel.GetComponentsInChildren<Renderer>(true),
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
                        recoveryModel,
                        recoveryRenderer,
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
                        "Invalid Kursa post-break recovery capture folder."));
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
            IReadOnlyList<Renderer> renderers,
            AnimationClip clip,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var fraction in ReviewFractions)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(animationRoot.gameObject, clip.length * fraction);
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled) continue;
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
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            if (!initialized)
                throw new InvalidOperationException(
                    "Kursa post-break recovery full-loop bounds are unavailable.");
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
                "The approved Kursa post-break recovery diagnostic captures already exist.");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on the Kursa post-break recovery.");
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

        private static Transform RequireSlotContractBeforeRename(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");

            Transform recoverySlot = null;
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                var expectedName = SlotNames[index];
                var nameMatches = index == 5
                    ? string.Equals(
                        slot.name,
                        PreviousRecoverySlotName,
                        StringComparison.Ordinal) || string.Equals(
                        slot.name,
                        RecoverySlotName,
                        StringComparison.Ordinal)
                    : string.Equals(
                        slot.name,
                        expectedName,
                        StringComparison.Ordinal);
                if (!nameMatches ||
                    slot.childCount != 1 ||
                    !string.Equals(
                        slot.GetChild(0).name,
                        ModelName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Kursa pre-replacement slot contract differs at " + index + ".");
                }
                if (index == 5) recoverySlot = slot;
            }
            return recoverySlot ?? throw new InvalidOperationException(
                "Kursa post-break recovery slot is missing.");
        }

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
            SlotNames.Where(item => item != RecoverySlotName)
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
