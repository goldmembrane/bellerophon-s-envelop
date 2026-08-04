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
    internal static class KursaShieldBreakFbxTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string ShieldBreakSlotName = "Kursa_12_ShieldBreakReaction";
        private const string ModelName = "Kursa_Model";
        private const string AnimatedRootName = "Kursa_ShieldBreak_AnimatedRoot";
        private const string SourceModelPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Models/Kursa_ShieldBreak_Source.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_12_ShieldBreakReaction.controller";
        private const string PlaybackClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_12_ShieldBreakReaction_Playback.anim";
        private const string ShieldlessMeshPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_12_ShieldBreakReaction_Shieldless.asset";
        private const string FragmentMeshesPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_12_ShieldBreakReaction_Fragments.asset";
        private const string ImportedClipName = "Kursa_12_ShieldBreakReaction_MixamoSource";
        private const string PlaybackClipName = "Kursa_12_ShieldBreakReaction_Playback";
        private const string FragmentRootName = "Kursa_ShieldFragments";
        private const string ScatterTypeName =
            "Bellerophon.Enemies.Kursa.KursaShieldFragmentScatter";
        private const string ValidationFolder =
            "docs/validation/kursa_shield_break_optimized_scatter_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_ShieldBreakFbx_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_ShieldBreakFbx_FinalReview.png";
        private const float MatrixTolerance = 0.00001f;
        // The approved effect spreads the exact shield triangles for 1 second.
        private const float ShatterSeconds = 1f;
        // Each source face is split at its three edge midpoints into four smaller faces.
        private const int FragmentsPerSourceTriangle = 4;
        // A fixed seed bakes one approved random pattern that repeats identically every loop.
        private const int FixedScatterSeed = 120804;
        // Random travel varies across this approved shield-radius range to avoid a spherical shell.
        private const float MinimumScatterMultiplier = 1.8f;
        private const float MaximumScatterMultiplier = 3f;
        // Height varies while independently ranged horizontal axes fill the left-back quadrant.
        private const float ScatterVerticalDeviation = 0.75f;
        private const float MinimumLeftDirectionWeight = 0.15f;
        private const float MaximumLeftDirectionWeight = 1.35f;
        private const float MinimumBackwardDirectionWeight = 0.4f;
        private const float MaximumBackwardDirectionWeight = 1.5f;
        private static readonly int[] ShieldSubmeshes = { 3, 8 };

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Shield Break FBX Replacement")]
        public static void ApplyKursaShieldBreakFbxReplacement()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                StaticSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var shieldBreakSlot = RequireChild(placement.transform, ShieldBreakSlotName);
            var previous = RequireModel(shieldBreakSlot);

            var takeName = ConfigureImporter();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                throw new InvalidOperationException(
                    "Kursa shield-break source prefab is missing.");
            var sourceRenderer = RequireRenderer(
                sourcePrefab.transform,
                "Kursa shield-break source FBX");
            var sourceClip = RequireEmbeddedClip(takeName);
            RequireExactRigCompatibility(staticRenderer, sourceRenderer);
            RequireClipBindings(sourcePrefab.transform, sourceClip);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;

            var wrapper = new GameObject("Kursa_ShieldBreak_Wrapper_Pending");
            wrapper.transform.SetParent(shieldBreakSlot, false);
            wrapper.transform.SetLocalPositionAndRotation(
                previousPosition,
                previousRotation);
            wrapper.transform.localScale = staticModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(sourcePrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Kursa shield-break source FBX could not be instantiated.");
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
                    ShieldBreakSlotName);
                ApplyExactStaticAppearance(
                    wrapper.transform,
                    replacementRenderer,
                    staticRenderer);
                var playbackClip = CreatePlaybackClip(
                    sourceClip);
                var controller = CreateController(playbackClip);
                var animator = ConfigureAnimator(replacement, controller);
                BuildSlotOnlyShieldFragments(
                    replacementRenderer,
                    staticRenderer,
                    replacement.transform,
                    animator,
                    sourceClip);
                RequirePlacedContract(
                    wrapper.transform,
                    staticRenderer,
                    playbackClip,
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
                "A Kursa slot outside Kursa_12_ShieldBreakReaction changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Kursa placement changed.");
            RequireSlotContract(placement.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after replacing Kursa_12_ShieldBreakReaction.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log(
                "KursaShieldBreakFbxReplacementApplied Result=PASS, " +
                "Slot=Kursa_12_ShieldBreakReaction, Source=" + SourceModelPath +
                ", MixamoTake=" + takeName +
                ", CombinedShieldFragmentMesh=True, FragmentRenderers=1" +
                ", FragmentAnimationCurves=0, StaticSourceReadOnly=True" +
                ", ExactStaticUv=True, ExactStaticSkin=True" +
                ", ExactStaticMaterials=True, ShatterSeconds=1" +
                ", FragmentsDetachedFromLeftHand=True" +
                ", FragmentsHiddenUntilLoopEnd=True, Loop=True, RootMotion=False" +
                ", OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Break FBX Diagnostic")]
        public static void CaptureKursaShieldBreakFbxDiagnostic()
        {
            var destination = NextDiagnosticPath();
            var cameraYaw = destination.EndsWith(
                "_02.png",
                StringComparison.Ordinal) ? 40f : 0f;
            CaptureShieldBreakReview(destination, "Diagnostic", cameraYaw);
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Shield Break FBX Final Review")]
        public static void CaptureKursaShieldBreakFbxFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Kursa shield-break final review already exists: " +
                    FinalReviewPath);
            }
            CaptureShieldBreakReview(destination, "FinalReview", 0f);
        }

        private static string ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(SourceModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Kursa shield-break FBX importer is unavailable.");
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
                    "The Kursa shield-break FBX must expose exactly one Mixamo take. " +
                    "Matches=" + matches.Length + ", Defaults=" +
                    string.Join("|", defaults.Select(item =>
                        item.name + ":" + item.takeName)) + ".");
            }

            var selected = matches[0];
            selected.name = ImportedClipName;
            selected.loopTime = false;
            selected.loopPose = false;
            selected.wrapMode = WrapMode.Once;
            importer.animationWrapMode = WrapMode.Once;
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
                    "shield-break clip. Clips=" +
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
                    "Existing Kursa shield-break controller could not be replaced.");
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(
                "KursaShieldBreakPlaybackLoop");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Animator ConfigureAnimator(
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
                        "Kursa shield-break FBX Animator root is not exact.");
                }
                animator = animators[0];
            }
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            return animator;
        }

        private static void RequireExactRigCompatibility(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer sourceRenderer)
        {
            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            var sourceMesh = sourceRenderer.sharedMesh ??
                throw new InvalidOperationException("Shield-break source mesh is missing.");
            if (staticRenderer.rootBone == null || sourceRenderer.rootBone == null)
                throw new InvalidOperationException("A Kursa root bone is missing.");

            var sourceIndices = UniqueBoneIndices(
                sourceRenderer.bones,
                "shield-break source");
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
                        "Shield-break source is missing exact static bone: " +
                        item.Key + ".");
                }
                if (!MatrixMatches(
                    staticMesh.bindposes[item.Value],
                    sourceMesh.bindposes[sourceIndex]))
                {
                    throw new InvalidOperationException(
                        "Shield-break source bind pose differs for exact static bone: " +
                        item.Key + ".");
                }
            }
            if (!string.Equals(
                staticRenderer.rootBone.name,
                sourceRenderer.rootBone.name,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Shield-break source root bone differs from the static Kursa root bone.");
            }
        }

        private static void ApplyExactStaticAppearance(
            Transform replacement,
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer staticRenderer)
        {
            RequireExactRigCompatibility(staticRenderer, replacementRenderer);
            var replacementBones = replacement.GetComponentsInChildren<Transform>(true);
            var byName = UniqueTransforms(replacementBones, "shield-break replacement");
            var mappedBones = staticRenderer.bones.Select(staticBone =>
            {
                if (!byName.TryGetValue(staticBone.name, out var mapped))
                {
                    throw new InvalidOperationException(
                        "Shield-break replacement is missing exact static bone: " +
                        staticBone.name + ".");
                }
                return mapped;
            }).ToArray();
            if (!byName.TryGetValue(staticRenderer.rootBone.name, out var mappedRoot))
            {
                throw new InvalidOperationException(
                    "Shield-break replacement is missing the exact static root bone.");
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

        private static void BuildSlotOnlyShieldFragments(
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer staticRenderer,
            Transform animationRoot,
            Animator animator,
            AnimationClip sourceClip)
        {
            var staticMesh = staticRenderer.sharedMesh ??
                throw new InvalidOperationException("Static Kursa mesh is missing.");
            if (ShieldSubmeshes.Any(index => index < 0 || index >= staticMesh.subMeshCount))
                throw new InvalidOperationException(
                    "The inspected static shield submesh contract no longer matches.");

            var leftHandIndex = Array.FindIndex(
                staticRenderer.bones,
                item => item != null && string.Equals(
                    item.name,
                    "LeftHand",
                    StringComparison.Ordinal));
            if (leftHandIndex < 0)
                throw new InvalidOperationException("Static Kursa LeftHand bone is missing.");
            var mappedLeftHand = replacementRenderer.bones[leftHandIndex];
            if (mappedLeftHand == null)
                throw new InvalidOperationException("Replacement Kursa LeftHand bone is missing.");
            var detachedFrame = CaptureInitialDetachedFragmentFrame(
                animationRoot,
                mappedLeftHand,
                sourceClip);

            var shieldVertexIndices = ShieldSubmeshes
                .SelectMany(staticMesh.GetTriangles)
                .Distinct()
                .OrderBy(index => index)
                .ToArray();
            RequireRigidLeftHandShield(staticMesh, shieldVertexIndices, leftHandIndex);

            DeleteGeneratedAsset(ShieldlessMeshPath);
            DeleteGeneratedAsset(FragmentMeshesPath);
            var shieldlessMesh = UnityEngine.Object.Instantiate(staticMesh);
            shieldlessMesh.name = "Kursa_12_ShieldBreakReaction_Shieldless";
            foreach (var submesh in ShieldSubmeshes)
                shieldlessMesh.SetTriangles(Array.Empty<int>(), submesh, false);
            AssetDatabase.CreateAsset(shieldlessMesh, ShieldlessMeshPath);
            replacementRenderer.sharedMesh = shieldlessMesh;

            var bindpose = staticMesh.bindposes[leftHandIndex];
            var triangles = BuildShieldTriangles(staticMesh, bindpose);
            if (triangles.Count == 0)
                throw new InvalidOperationException("Static Kursa shield has no triangles.");
            var shieldCenter = triangles.Aggregate(
                Vector3.zero,
                (sum, item) => sum + item.Center) / triangles.Count;
            var shieldRadius = triangles
                .SelectMany(item => item.Positions)
                .Max(position => Vector3.Distance(position, shieldCenter));
            if (shieldRadius <= 0.00001f)
                throw new InvalidOperationException("Static Kursa shield radius is invalid.");
            var fragmentRoot = new GameObject(FragmentRootName);
            fragmentRoot.transform.SetParent(animationRoot, false);
            fragmentRoot.transform.SetLocalPositionAndRotation(
                detachedFrame.Position,
                detachedFrame.Rotation);
            fragmentRoot.transform.localScale = detachedFrame.Scale;
            var combinedMesh = CreateCombinedFragmentMesh(staticMesh, triangles);
            AssetDatabase.CreateAsset(combinedMesh, FragmentMeshesPath);
            var filter = fragmentRoot.AddComponent<MeshFilter>();
            filter.sharedMesh = combinedMesh;
            var meshRenderer = fragmentRoot.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = ShieldSubmeshes
                .Select(index => staticRenderer.sharedMaterials[index])
                .ToArray();
            CopyRendererSettings(staticRenderer, meshRenderer);

            var startCenters = new Vector3[triangles.Count];
            var endCenters = new Vector3[triangles.Count];
            var endRotations = new Quaternion[triangles.Count];
            var scatterRandom = new System.Random(FixedScatterSeed);
            for (var index = 0; index < triangles.Count; index++)
            {
                var triangle = triangles[index];
                var randomDirection = NextRandomLeftBackDirection(
                    scatterRandom,
                    detachedFrame.RootToFrameDirection);
                var travelMultiplier = Mathf.Lerp(
                    MinimumScatterMultiplier,
                    MaximumScatterMultiplier,
                    NextRandom01(scatterRandom));
                var endPosition = triangle.Center +
                    randomDirection * shieldRadius * travelMultiplier;
                var rotationAxis = NextRandomDirection(scatterRandom);
                var endRotation = Quaternion.AngleAxis(
                    Mathf.Lerp(120f, 300f, NextRandom01(scatterRandom)),
                    rotationAxis);
                startCenters[index] = triangle.Center;
                endCenters[index] = endPosition;
                endRotations[index] = endRotation;
            }
            var scatter = fragmentRoot.AddComponent(RequireScatterType());
            InvokeScatter(
                scatter,
                "Configure",
                animator,
                filter,
                meshRenderer,
                ShatterSeconds,
                sourceClip.length,
                startCenters,
                endCenters,
                endRotations);
            EvaluateScatter(scatter, 0f);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(replacementRenderer);
            EditorUtility.SetDirty(filter);
            EditorUtility.SetDirty(meshRenderer);
            EditorUtility.SetDirty(scatter);
        }

        private static DetachedFragmentFrame CaptureInitialDetachedFragmentFrame(
            Transform animationRoot,
            Transform mappedLeftHand,
            AnimationClip sourceClip)
        {
            var snapshots = animationRoot.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                sourceClip.SampleAnimation(animationRoot.gameObject, 0f);
                var handInRoot = animationRoot.worldToLocalMatrix *
                    mappedLeftHand.localToWorldMatrix;
                var position = (Vector3)handInRoot.GetColumn(3);
                var rotation = handInRoot.rotation;
                var scale = handInRoot.lossyScale;
                var reconstructed = Matrix4x4.TRS(position, rotation, scale);
                if (!MatrixMatches(handInRoot, reconstructed))
                {
                    throw new InvalidOperationException(
                        "The initial LeftHand frame contains unsupported shear; " +
                        "detached shield fragments were not guessed.");
                }
                return new DetachedFragmentFrame(
                    position,
                    rotation,
                    scale,
                    handInRoot.inverse);
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
            }
        }

        private static Vector3 NextRandomLeftBackDirection(
            System.Random random,
            Matrix4x4 rootToFrameDirection)
        {
            var leftMagnitude = Mathf.Lerp(
                MinimumLeftDirectionWeight,
                MaximumLeftDirectionWeight,
                NextRandom01(random));
            var backwardMagnitude = Mathf.Lerp(
                MinimumBackwardDirectionWeight,
                MaximumBackwardDirectionWeight,
                NextRandom01(random));
            var directionInKursaSpace = new Vector3(
                -leftMagnitude,
                NextRandomSigned(random) * ScatterVerticalDeviation,
                -backwardMagnitude).normalized;
            return rootToFrameDirection.MultiplyVector(directionInKursaSpace).normalized;
        }

        private static Vector3 NextRandomDirection(System.Random random)
        {
            Vector3 direction;
            do
            {
                direction = new Vector3(
                    NextRandomSigned(random),
                    NextRandomSigned(random),
                    NextRandomSigned(random));
            } while (direction.sqrMagnitude <= 0.0001f);
            return direction.normalized;
        }

        private static float NextRandomSigned(System.Random random) =>
            NextRandom01(random) * 2f - 1f;

        private static float NextRandom01(System.Random random) =>
            (float)random.NextDouble();

        private static void RequireRigidLeftHandShield(
            Mesh mesh,
            IEnumerable<int> shieldVertexIndices,
            int leftHandIndex)
        {
            var weights = mesh.boneWeights;
            if (weights.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "Static Kursa shield bone weights are not available for exact extraction.");
            foreach (var vertexIndex in shieldVertexIndices)
            {
                var weight = weights[vertexIndex];
                var leftWeight = 0f;
                var otherWeight = 0f;
                AccumulateWeight(weight.boneIndex0, weight.weight0);
                AccumulateWeight(weight.boneIndex1, weight.weight1);
                AccumulateWeight(weight.boneIndex2, weight.weight2);
                AccumulateWeight(weight.boneIndex3, weight.weight3);
                if (leftWeight < 0.9999f || otherWeight > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Static shield is not rigidly weighted to LeftHand at vertex " +
                        vertexIndex + "; exact non-guess extraction was stopped.");
                }

                void AccumulateWeight(int boneIndex, float value)
                {
                    if (value <= 0f) return;
                    if (boneIndex == leftHandIndex) leftWeight += value;
                    else otherWeight += value;
                }
            }
        }

        private static List<ShieldTriangleData> BuildShieldTriangles(
            Mesh mesh,
            Matrix4x4 bindpose)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            if (normals.Length != vertices.Length)
                throw new InvalidOperationException(
                    "Static shield normals are incomplete; exact appearance cannot be copied.");
            var result = new List<ShieldTriangleData>();
            var subdivisionWeights = FragmentBarycentricSets();
            foreach (var submesh in ShieldSubmeshes)
            {
                var indices = mesh.GetTriangles(submesh);
                if (indices.Length % 3 != 0)
                    throw new InvalidOperationException("Static shield triangles are malformed.");
                for (var offset = 0; offset < indices.Length; offset += 3)
                {
                    var sourceIndices = new[]
                    {
                        indices[offset], indices[offset + 1], indices[offset + 2]
                    };
                    var positions = sourceIndices
                        .Select(index => bindpose.MultiplyPoint3x4(vertices[index]))
                        .ToArray();
                    var transformedNormals = sourceIndices
                        .Select(index => bindpose.MultiplyVector(normals[index]).normalized)
                        .ToArray();
                    var transformedTangents = tangents.Length == vertices.Length
                        ? sourceIndices.Select(index =>
                        {
                            var tangent = tangents[index];
                            var direction = bindpose.MultiplyVector(new Vector3(
                                tangent.x,
                                tangent.y,
                                tangent.z)).normalized;
                            return new Vector4(
                                direction.x,
                                direction.y,
                                direction.z,
                                tangent.w);
                        }).ToArray()
                        : Array.Empty<Vector4>();
                    foreach (var childWeights in subdivisionWeights)
                    {
                        var childPositions = childWeights
                            .Select(weights => Interpolate(positions, weights))
                            .ToArray();
                        var childNormals = childWeights
                            .Select(weights => Interpolate(transformedNormals, weights).normalized)
                            .ToArray();
                        var childTangents = transformedTangents.Length == 3
                            ? childWeights.Select(weights =>
                                InterpolateTangent(transformedTangents, weights)).ToArray()
                            : Array.Empty<Vector4>();
                        var center = (childPositions[0] + childPositions[1] +
                            childPositions[2]) / 3f;
                        var normal = childNormals.Aggregate(
                            Vector3.zero,
                            (sum, item) => sum + item);
                        if (normal.sqrMagnitude <= 0.00000001f)
                        {
                            normal = Vector3.Cross(
                                childPositions[1] - childPositions[0],
                                childPositions[2] - childPositions[0]);
                        }
                        result.Add(new ShieldTriangleData(
                            submesh,
                            sourceIndices,
                            childWeights,
                            childPositions,
                            childNormals,
                            childTangents,
                            center,
                            normal.normalized));
                    }
                }
            }
            return result;
        }

        private static Vector3[][] FragmentBarycentricSets()
        {
            var a = new Vector3(1f, 0f, 0f);
            var b = new Vector3(0f, 1f, 0f);
            var c = new Vector3(0f, 0f, 1f);
            var ab = (a + b) * 0.5f;
            var bc = (b + c) * 0.5f;
            var ca = (c + a) * 0.5f;
            return new[]
            {
                new[] { a, ab, ca },
                new[] { ab, b, bc },
                new[] { ca, bc, c },
                new[] { ab, bc, ca }
            };
        }

        private static Vector3 Interpolate(Vector3[] values, Vector3 weights) =>
            values[0] * weights.x + values[1] * weights.y + values[2] * weights.z;

        private static Vector4 Interpolate(Vector4[] values, Vector3 weights) =>
            values[0] * weights.x + values[1] * weights.y + values[2] * weights.z;

        private static Vector4 InterpolateTangent(Vector4[] values, Vector3 weights)
        {
            var blended = Interpolate(values, weights);
            var direction = new Vector3(blended.x, blended.y, blended.z).normalized;
            return new Vector4(
                direction.x,
                direction.y,
                direction.z,
                blended.w >= 0f ? 1f : -1f);
        }

        private static Mesh CreateCombinedFragmentMesh(
            Mesh source,
            IReadOnlyList<ShieldTriangleData> triangles)
        {
            var result = new Mesh
            {
                name = "Kursa_12_ShieldBreakReaction_Fragments"
            };
            var vertices = new List<Vector3>(triangles.Count * 3);
            var normals = new List<Vector3>(triangles.Count * 3);
            var tangents = new List<Vector4>(triangles.Count * 3);
            var submeshIndices = ShieldSubmeshes.ToDictionary(
                submesh => submesh,
                _ => new List<int>());
            var sourceUvs = new List<Vector4>[8];
            var resultUvs = new List<Vector4>[8];
            for (var channel = 0; channel < 8; channel++)
            {
                sourceUvs[channel] = new List<Vector4>();
                source.GetUVs(channel, sourceUvs[channel]);
                if (sourceUvs[channel].Count == source.vertexCount)
                {
                    resultUvs[channel] = new List<Vector4>(triangles.Count * 3);
                }
            }
            var sourceColors = source.colors32;
            var resultColors = sourceColors.Length == source.vertexCount
                ? new List<Color32>(triangles.Count * 3)
                : null;
            foreach (var triangle in triangles)
            {
                if (triangle.Tangents.Length != 3)
                {
                    throw new InvalidOperationException(
                        "Static shield tangents are incomplete; exact combined fragments stopped.");
                }
                var vertexStart = vertices.Count;
                vertices.AddRange(triangle.Positions);
                normals.AddRange(triangle.Normals);
                tangents.AddRange(triangle.Tangents);
                submeshIndices[triangle.Submesh].Add(vertexStart);
                submeshIndices[triangle.Submesh].Add(vertexStart + 1);
                submeshIndices[triangle.Submesh].Add(vertexStart + 2);
                for (var channel = 0; channel < resultUvs.Length; channel++)
                {
                    if (resultUvs[channel] == null) continue;
                    foreach (var weights in triangle.Barycentrics)
                    {
                        resultUvs[channel].Add(
                            sourceUvs[channel][triangle.SourceIndices[0]] * weights.x +
                            sourceUvs[channel][triangle.SourceIndices[1]] * weights.y +
                            sourceUvs[channel][triangle.SourceIndices[2]] * weights.z);
                    }
                }
                if (resultColors != null)
                {
                    foreach (var weights in triangle.Barycentrics)
                    {
                        var color =
                            (Color)sourceColors[triangle.SourceIndices[0]] * weights.x +
                            (Color)sourceColors[triangle.SourceIndices[1]] * weights.y +
                            (Color)sourceColors[triangle.SourceIndices[2]] * weights.z;
                        resultColors.Add((Color32)color);
                    }
                }
            }
            result.indexFormat = vertices.Count > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            result.SetVertices(vertices);
            result.SetNormals(normals);
            result.SetTangents(tangents);
            for (var channel = 0; channel < resultUvs.Length; channel++)
            {
                if (resultUvs[channel] != null)
                    result.SetUVs(channel, resultUvs[channel]);
            }
            if (resultColors != null)
                result.SetColors(resultColors);
            result.subMeshCount = ShieldSubmeshes.Length;
            for (var index = 0; index < ShieldSubmeshes.Length; index++)
                result.SetTriangles(submeshIndices[ShieldSubmeshes[index]], index, false);
            result.RecalculateBounds();
            return result;
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
            destination.motionVectorGenerationMode = source.motionVectorGenerationMode;
            var propertyBlock = new MaterialPropertyBlock();
            source.GetPropertyBlock(propertyBlock);
            destination.SetPropertyBlock(propertyBlock);
        }

        private static AnimationClip CreatePlaybackClip(AnimationClip source)
        {
            if (source.length <= ShatterSeconds)
                throw new InvalidOperationException(
                    "The Mixamo shield-break clip must be longer than 1 second.");
            DeleteGeneratedAsset(PlaybackClipPath);
            var playback = new AnimationClip
            {
                name = PlaybackClipName,
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop
            };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                var copiedCurve = new AnimationCurve(sourceCurve.keys)
                {
                    preWrapMode = sourceCurve.preWrapMode,
                    postWrapMode = sourceCurve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(playback, binding, copiedCurve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(playback, binding, keys);
            }
            AnimationUtility.SetAnimationEvents(playback, AnimationUtility.GetAnimationEvents(source));

            var settings = AnimationUtility.GetAnimationClipSettings(playback);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(playback, settings);
            AssetDatabase.CreateAsset(playback, PlaybackClipPath);
            AssetDatabase.SaveAssets();
            return playback;
        }

        private static Type RequireScatterType()
        {
            // The approved Scripts path is compiled into the default runtime assembly,
            // so the editor assembly resolves this scene component without a hard reference.
            var matches = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(
                    ScatterTypeName,
                    throwOnError: false,
                    ignoreCase: false))
                .Where(type => type != null)
                .ToArray();
            if (matches.Length != 1 || !typeof(Component).IsAssignableFrom(matches[0]))
            {
                throw new InvalidOperationException(
                    "The compact Kursa shield scatter runtime type is unavailable.");
            }
            return matches[0];
        }

        private static void InvokeScatter(
            Component scatter,
            string methodName,
            params object[] arguments)
        {
            var method = scatter.GetType().GetMethod(methodName) ??
                throw new InvalidOperationException(
                    "Compact Kursa shield scatter method is unavailable: " +
                    methodName + ".");
            try
            {
                method.Invoke(scatter, arguments);
            }
            catch (System.Reflection.TargetInvocationException exception)
            {
                throw new InvalidOperationException(
                    "Compact Kursa shield scatter failed: " + methodName + ".",
                    exception.InnerException ?? exception);
            }
        }

        private static void EvaluateScatter(Component scatter, float elapsedSeconds) =>
            InvokeScatter(scatter, "EvaluateAtSeconds", elapsedSeconds);

        private static T RequireScatterValue<T>(Component scatter, string propertyName)
        {
            var property = scatter.GetType().GetProperty(propertyName) ??
                throw new InvalidOperationException(
                    "Compact Kursa shield scatter property is unavailable: " +
                    propertyName + ".");
            var value = property.GetValue(scatter);
            if (!(value is T typedValue))
            {
                throw new InvalidOperationException(
                    "Compact Kursa shield scatter property type differs: " +
                    propertyName + ".");
            }
            return typedValue;
        }

        private static void DeleteGeneratedAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException("Generated asset could not be replaced: " + path);
            }
        }

        private static void RequirePlacedContract(
            Transform replacement,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip playbackClip,
            AnimatorController controller)
        {
            var renderer = RequireRenderer(replacement, ShieldBreakSlotName);
            var expectedShieldless = AssetDatabase.LoadAssetAtPath<Mesh>(
                ShieldlessMeshPath) ?? throw new InvalidOperationException(
                "Placed shield-break shieldless mesh asset is missing.");
            if (renderer.sharedMesh != expectedShieldless ||
                !renderer.sharedMaterials.SequenceEqual(staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Placed shield-break body does not use its exact static-derived assets.");
            }
            RequireShieldlessMeshContract(staticRenderer.sharedMesh, expectedShieldless);
            var expectedBoneNames = staticRenderer.bones.Select(item => item.name);
            if (!renderer.bones.Select(item => item.name).SequenceEqual(expectedBoneNames))
            {
                throw new InvalidOperationException(
                    "Placed shield-break skin bone order differs from the static Kursa.");
            }
            var animator = replacement.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Placed shield-break model must contain one Animator.");
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
                    "Placed shield-break animated root is not isolated below its scale wrapper.");
            }
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Placed shield-break Animator configuration differs.");
            }
            if (playbackClip != AssetDatabase.LoadAssetAtPath<AnimationClip>(PlaybackClipPath) ||
                !AnimationUtility.GetAnimationClipSettings(playbackClip).loopTime)
                throw new InvalidOperationException("Shield-break playback clip is not looping.");
            RequireClipBindings(animator.transform, playbackClip);

            var fragmentRootMatches = replacement
                .GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(item.name, FragmentRootName, StringComparison.Ordinal))
                .ToArray();
            if (fragmentRootMatches.Length != 1 ||
                fragmentRootMatches[0].parent != animator.transform)
            {
                throw new InvalidOperationException(
                    "Slot 12 shield fragments are not detached directly below its animation root.");
            }
            var fragmentRootPath = AnimationUtility.CalculateTransformPath(
                fragmentRootMatches[0],
                animator.transform);
            if (AnimationUtility.GetCurveBindings(playbackClip).Any(binding =>
                    string.Equals(binding.path, fragmentRootPath, StringComparison.Ordinal) ||
                    binding.path.StartsWith(
                        fragmentRootPath + "/",
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "The compact slot 12 fragment mesh must not contain per-fragment curves.");
            }
            var fragments = fragmentRootMatches[0]
                .GetComponentsInChildren<MeshRenderer>(true)
                .ToArray();
            var expectedFragmentCount = ShieldSubmeshes.Sum(
                submesh => staticRenderer.sharedMesh.GetTriangles(submesh).Length / 3) *
                FragmentsPerSourceTriangle;
            var fragmentMeshes = AssetDatabase.LoadAllAssetsAtPath(FragmentMeshesPath)
                .OfType<Mesh>()
                .ToArray();
            if (fragments.Length != 1 ||
                fragments[0].transform != fragmentRootMatches[0] ||
                fragmentMeshes.Length != 1)
            {
                throw new InvalidOperationException(
                    "Slot 12 compact shield fragments must use one mesh renderer and one mesh.");
            }
            var expectedMaterials = ShieldSubmeshes
                .Select(index => staticRenderer.sharedMaterials[index])
                .ToArray();
            var fragmentRenderer = fragments[0];
            var filter = fragmentRenderer.GetComponent<MeshFilter>();
            var fragmentMesh = fragmentMeshes[0];
            if (filter == null || filter.sharedMesh != fragmentMesh ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(fragmentMesh),
                    FragmentMeshesPath,
                    StringComparison.Ordinal) ||
                !fragmentRenderer.sharedMaterials.SequenceEqual(expectedMaterials) ||
                fragmentMesh.vertexCount != expectedFragmentCount * 3 ||
                fragmentMesh.subMeshCount != ShieldSubmeshes.Length)
            {
                throw new InvalidOperationException(
                    "Slot 12 compact shield mesh does not preserve its geometry/material contract.");
            }
            for (var index = 0; index < ShieldSubmeshes.Length; index++)
            {
                var sourceTriangleCount = staticRenderer.sharedMesh
                    .GetTriangles(ShieldSubmeshes[index]).Length / 3;
                if (fragmentMesh.GetTriangles(index).Length / 3 !=
                    sourceTriangleCount * FragmentsPerSourceTriangle)
                {
                    throw new InvalidOperationException(
                        "A compact shield material submesh has a different triangle count.");
                }
            }
            var scatter = fragmentRootMatches[0]
                .GetComponents(RequireScatterType())
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Slot 12 compact shield scatter component is missing.");
            if (RequireScatterValue<Animator>(scatter, "Animator") != animator ||
                RequireScatterValue<MeshFilter>(scatter, "MeshFilter") != filter ||
                RequireScatterValue<MeshRenderer>(scatter, "MeshRenderer") !=
                    fragmentRenderer ||
                RequireScatterValue<int>(scatter, "FragmentCount") !=
                    expectedFragmentCount ||
                !Mathf.Approximately(
                    RequireScatterValue<float>(scatter, "ScatterSeconds"),
                    ShatterSeconds) ||
                !Mathf.Approximately(
                    RequireScatterValue<float>(scatter, "ClipSeconds"),
                    playbackClip.length))
            {
                throw new InvalidOperationException(
                    "Slot 12 compact shield scatter configuration differs.");
            }
            EvaluateScatter(scatter, 0f);
            if (!fragmentRenderer.enabled)
                throw new InvalidOperationException("Compact shield is hidden at loop start.");
            EvaluateScatter(scatter, ShatterSeconds);
            if (fragmentRenderer.enabled)
                throw new InvalidOperationException("Compact shield remains visible after shatter.");
            EvaluateScatter(scatter, 0f);
        }

        private static void RequireShieldlessMeshContract(Mesh source, Mesh shieldless)
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
                    "Slot 12 shieldless body changed static geometry channels.");
            }
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                var expected = ShieldSubmeshes.Contains(submesh)
                    ? Array.Empty<int>()
                    : source.GetTriangles(submesh);
                if (!expected.SequenceEqual(shieldless.GetTriangles(submesh)))
                {
                    throw new InvalidOperationException(
                        "Slot 12 shieldless body submesh contract differs at " + submesh + ".");
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
                    "Shield-break playback paths do not exactly match the FBX hierarchy: " +
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

        private static void CaptureShieldBreakReview(
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
            var shieldBreakModel = RequireModel(RequireChild(
                placement.transform,
                ShieldBreakSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var shieldBreakRenderer = RequireRenderer(shieldBreakModel, ShieldBreakSlotName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlaybackClipPath) ??
                throw new InvalidOperationException(
                    "Kursa shield-break playback clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Kursa shield-break controller is missing.");
            RequirePlacedContract(shieldBreakModel, staticRenderer, clip, controller);
            CaptureContactSheet(
                scene,
                staticModel,
                staticRenderer,
                shieldBreakModel,
                shieldBreakRenderer,
                clip,
                cameraYaw,
                destination,
                useSharedScaleFraming);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa shield-break capture changed the scene dirty state.");
            Debug.Log(
                "KursaShieldBreakFbxReviewCaptured Kind=" + captureKind +
                ", FullLoop=True, StaticAppearanceReference=True" +
                ", DirectVisualReviewRequired=True, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            Transform shieldBreakModel,
            SkinnedMeshRenderer shieldBreakRenderer,
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
                "KursaShieldBreakReviewCamera",
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
            var animator = shieldBreakModel.GetComponentsInChildren<Animator>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Kursa shield-break Animator is missing during capture.");
            var scatter = shieldBreakModel
                .GetComponentsInChildren(RequireScatterType(), true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Kursa compact shield scatter is missing during capture.");
            var animatorEnabled = animator != null && animator.enabled;
            var snapshots = shieldBreakModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                if (animator != null) animator.enabled = false;
                var reviewTimes = ReviewTimes(clip);
                var fixedShieldBreakBounds = FullLoopBounds(
                    animator.transform,
                    shieldBreakModel,
                    clip,
                    scatter,
                    snapshots,
                    reviewTimes);
                var staticFramingBounds = staticRenderer.bounds;
                var shieldBreakFramingBounds = fixedShieldBreakBounds;
                if (useSharedScaleFraming)
                {
                    var sharedSize = Vector3.Max(
                        staticFramingBounds.size,
                        shieldBreakFramingBounds.size);
                    staticFramingBounds = new Bounds(
                        staticFramingBounds.center,
                        sharedSize);
                    shieldBreakFramingBounds = new Bounds(
                        shieldBreakFramingBounds.center,
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

                for (var index = 0; index < reviewTimes.Length; index++)
                {
                    foreach (var snapshot in snapshots) snapshot.Restore();
                    clip.SampleAnimation(
                        animator.gameObject,
                        reviewTimes[index]);
                    EvaluateScatter(scatter, reviewTimes[index]);
                    RenderSubjectPanel(
                        camera,
                        shieldBreakModel,
                        shieldBreakRenderer,
                        sceneRenderers,
                        target,
                        panel,
                        cameraYaw,
                        shieldBreakFramingBounds);
                    var panelIndex = index + 1;
                    var column = panelIndex % columns;
                    var row = rows - 1 - panelIndex / columns;
                    CopyPanel(panel, grid, column, row, panelWidth, panelHeight);
                }
                grid.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa shield-break capture folder."));
                File.WriteAllBytes(destination, grid.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                EvaluateScatter(scatter, 0f);
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
            {
                if (!sceneRenderer.transform.IsChildOf(model))
                    sceneRenderer.enabled = false;
                else if (sceneRenderer == renderer)
                    sceneRenderer.enabled = true;
            }
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
            Transform model,
            AnimationClip clip,
            Component scatter,
            IReadOnlyList<TransformSnapshot> snapshots,
            IReadOnlyList<float> reviewTimes)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var reviewTime in reviewTimes)
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                clip.SampleAnimation(animationRoot.gameObject, reviewTime);
                EvaluateScatter(scatter, reviewTime);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.enabled))
                {
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
            EvaluateScatter(scatter, 0f);
            if (!initialized)
                throw new InvalidOperationException(
                    "Kursa shield-break full-loop bounds are unavailable.");
            return result;
        }

        private static float[] ReviewTimes(AnimationClip clip)
        {
            if (clip.length <= ShatterSeconds)
                throw new InvalidOperationException(
                    "Kursa shield-break playback is too short for direct shatter review.");
            var result = new List<float>
            {
                0f, 0.15f, 0.3f, 0.5f, 0.75f, 1f, 1.25f, 1.49f,
                ShatterSeconds
            };
            const int laterSampleCount = 10;
            for (var index = 1; index <= laterSampleCount; index++)
            {
                result.Add(Mathf.Lerp(
                    ShatterSeconds,
                    clip.length,
                    index / (float)laterSampleCount));
            }
            return result.ToArray();
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
            for (var index = 1; index <= 2; index++)
            {
                var candidate = Absolute(string.Format(
                    DiagnosticPathFormat,
                    index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The approved Kursa shield-break diagnostic captures already exist.");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on the Kursa shield-break.");
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
            SlotNames.Where(item => item != ShieldBreakSlotName)
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

        private readonly struct DetachedFragmentFrame
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }
            public Matrix4x4 RootToFrameDirection { get; }

            public DetachedFragmentFrame(
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                Matrix4x4 rootToFrameDirection)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
                RootToFrameDirection = rootToFrameDirection;
            }
        }

        private sealed class ShieldTriangleData
        {
            public int Submesh { get; }
            public int[] SourceIndices { get; }
            public Vector3[] Barycentrics { get; }
            public Vector3[] Positions { get; }
            public Vector3[] Normals { get; }
            public Vector4[] Tangents { get; }
            public Vector3 Center { get; }
            public Vector3 Normal { get; }

            public ShieldTriangleData(
                int submesh,
                int[] sourceIndices,
                Vector3[] barycentrics,
                Vector3[] positions,
                Vector3[] normals,
                Vector4[] tangents,
                Vector3 center,
                Vector3 normal)
            {
                Submesh = submesh;
                SourceIndices = sourceIndices;
                Barycentrics = barycentrics;
                Positions = positions;
                Normals = normals;
                Tangents = tangents;
                Center = center;
                Normal = normal;
            }
        }

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
