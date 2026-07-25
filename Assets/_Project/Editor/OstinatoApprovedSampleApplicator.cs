using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.OstinatoApprovedSample
{
    internal static class OstinatoApprovedSampleApplicator
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string ModelChildName = "Ostinato_Model";
        private const string WalkingSlotName = "Ostinato_03_Walking";
        private const string WalkingModelName = "Ostinato_Walking_Model";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx";
        private const string ApprovedModelPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_ApprovedUnity.fbx";
        private const string WalkingSynchronizedModelPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_WalkingAppearanceSynced.fbx";
        private const string WalkingSourceRelativePath = "enemies model/ostinato walking.fbx";
        private const string WalkingModelPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_Walking.fbx";
        private const string TextureRoot = "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Baked";
        private const string MaterialRoot = "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials";
        private const string ValidationRoot = "docs/validation/ostinato_approved_material_2026-07-18";
        private const string WalkingAppearanceValidationRoot = "docs/validation/ostinato_walking_appearance_2026-07-19";
        private const string ApprovedFrontRender = "artSample/enemies/ostinato/renders/01_front_blender_reference_material.png";
        private const string ApprovedSideRender = "artSample/enemies/ostinato/renders/02_side_blender_reference_material.png";
        private const string ApprovedBackRender = "artSample/enemies/ostinato/renders/03_back_blender_reference_material.png";
        private const int WalkingCaptureLayer = 31;
        private const int PlacementCount = 9;

        private static readonly MaterialDefinition[] MaterialDefinitions =
        {
            new MaterialDefinition("Chitin", "Ostinato_Approved_Chitin", 1.35f),
            new MaterialDefinition("SoftTissue", "Ostinato_Approved_SoftTissue", 1.45f),
            new MaterialDefinition("HookBlade", "Ostinato_Approved_HookBlade", 1.00f),
            new MaterialDefinition("CompoundEye", "Ostinato_Approved_CompoundEye", 1.50f),
        };

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Approved Blender Sample To CargoRunMvp")]
        public static void ApplyApprovedOstinatoSampleToCargoRunMvp()
        {
            var currentScene = RequireOpenScene();
            var currentPlacementRoot = currentScene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform ??
                throw new InvalidOperationException(PlacementRootName + " is missing.");
            if (currentPlacementRoot.Find(WalkingSlotName) != null)
            {
                ApplyWalkingAppearanceSync(currentScene, currentPlacementRoot);
                return;
            }

            ConfigureApprovedModelImporter();
            ConfigureApprovedTextureImporters();
            var approvedMaterials = MaterialDefinitions.Select(CreateOrUpdateMaterial).ToArray();
            AssetDatabase.SaveAssets();

            var sourceRenderer = RequireAssetRenderer(SourceModelPath);
            var approvedRenderer = RequireAssetRenderer(ApprovedModelPath);
            var sourceMesh = sourceRenderer.sharedMesh ??
                throw new InvalidOperationException("Source Ostinato mesh is missing.");
            var approvedMesh = approvedRenderer.sharedMesh ??
                throw new InvalidOperationException("Approved Ostinato mesh is missing.");
            var sourceBoneNames = RequireBoneNames(sourceRenderer, "source");
            var approvedBoneNames = RequireBoneNames(approvedRenderer, "approved");
            if (!sourceBoneNames.SequenceEqual(approvedBoneNames))
            {
                throw new InvalidOperationException("Approved Ostinato bone order differs from the source model.");
            }
            if (approvedMesh.subMeshCount != MaterialDefinitions.Length)
            {
                throw new InvalidOperationException(
                    $"Approved Ostinato requires {MaterialDefinitions.Length} submeshes, found {approvedMesh.subMeshCount}.");
            }
            RequireMatchingBounds(sourceMesh.bounds, approvedMesh.bounds);

            var scene = RequireOpenScene();
            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform ??
                throw new InvalidOperationException(PlacementRootName + " is missing.");
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"{PlacementRootName} must contain exactly {PlacementCount} slots.");
            }

            var transformSnapshots = placementRoot.GetComponentsInChildren<Transform>(true)
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var sourceHashBefore = ComputeSha256(ProjectAbsolutePath(SourceModelPath));
            var rendererPaths = new List<string>();
            for (var index = 0; index < PlacementCount; index++)
            {
                var expectedSlotName = $"Ostinato_{index + 1:00}_Static_Review";
                var slot = placementRoot.GetChild(index);
                if (slot.name != expectedSlotName)
                {
                    throw new InvalidOperationException($"Expected {expectedSlotName}, found {slot.name}.");
                }
                var model = slot.Find(ModelChildName) ??
                    throw new InvalidOperationException(expectedSlotName + " is missing " + ModelChildName + ".");
                var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                    throw new InvalidOperationException(expectedSlotName + " must contain one skinned renderer.");
                var sceneBoneNames = RequireBoneNames(renderer, expectedSlotName);
                if (!sceneBoneNames.SequenceEqual(sourceBoneNames))
                {
                    throw new InvalidOperationException(expectedSlotName + " bone order differs from the source model.");
                }

                renderer.sharedMesh = approvedMesh;
                renderer.sharedMaterials = approvedMaterials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
                rendererPaths.Add(expectedSlotName + "/" + ModelChildName + "/" + renderer.name);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after approved Ostinato application.");
            }

            foreach (var snapshot in transformSnapshots)
            {
                snapshot.AssertUnchanged();
            }
            foreach (var renderer in placementRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh != approvedMesh ||
                    !renderer.sharedMaterials.SequenceEqual(approvedMaterials))
                {
                    throw new InvalidOperationException(renderer.name + " does not use the approved model and materials.");
                }
                if (!RequireBoneNames(renderer, renderer.name).SequenceEqual(sourceBoneNames))
                {
                    throw new InvalidOperationException(renderer.name + " bone order changed after application.");
                }
            }

            var sourceHashAfter = ComputeSha256(ProjectAbsolutePath(SourceModelPath));
            if (sourceHashBefore != sourceHashAfter)
            {
                throw new InvalidOperationException("The original Ostinato FBX bytes changed during approved sample application.");
            }

            Directory.CreateDirectory(ProjectAbsolutePath(ValidationRoot));
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("PlacementCount=" + PlacementCount);
            report.AppendLine("SourceModel=" + SourceModelPath);
            report.AppendLine("ApprovedModel=" + ApprovedModelPath);
            report.AppendLine("SourceMesh=" + sourceMesh.name);
            report.AppendLine("ApprovedMesh=" + approvedMesh.name);
            report.AppendLine("SourceVertexCount=" + sourceMesh.vertexCount);
            report.AppendLine("ApprovedVertexCount=" + approvedMesh.vertexCount);
            report.AppendLine("ApprovedSubMeshCount=" + approvedMesh.subMeshCount);
            report.AppendLine("BoneCount=" + sourceBoneNames.Length);
            report.AppendLine("Bones=" + string.Join("|", sourceBoneNames));
            report.AppendLine("Materials=" + string.Join("|", approvedMaterials.Select(material => AssetDatabase.GetAssetPath(material))));
            report.AppendLine(
                "BaseColorMultipliers=" + string.Join(
                    "|",
                    MaterialDefinitions.Select(definition => definition.Label + ":" + definition.BaseColorMultiplier)));
            report.AppendLine("Renderers=" + string.Join("|", rendererPaths));
            report.AppendLine("SourceSha256Before=" + sourceHashBefore);
            report.AppendLine("SourceSha256After=" + sourceHashAfter);
            report.AppendLine("TransformsChanged=False");
            report.AppendLine("RigHierarchyChanged=False");
            report.AppendLine("AnimationChanged=False");
            report.AppendLine("PhysicsChanged=False");
            report.AppendLine("AiChanged=False");
            report.AppendLine("OtherSceneRootsChanged=False");
            report.AppendLine("SceneSaved=True");
            File.WriteAllText(
                ProjectAbsolutePath(ValidationRoot + "/Ostinato_ApprovedSampleApplication.txt"),
                report.ToString(),
                Encoding.UTF8);

            Selection.activeObject = null;
            Debug.Log(
                $"OstinatoApprovedSampleApplied Count={PlacementCount}, SubMeshes={approvedMesh.subMeshCount}, " +
                $"Materials={approvedMaterials.Length}, Bones={sourceBoneNames.Length}, TransformsChanged=False, " +
                "RigHierarchyChanged=False, PhysicsChanged=False, AiChanged=False, OtherSceneRootsChanged=False");
        }

        private static void ApplyWalkingAppearanceSync(Scene scene, Transform placementRoot)
        {
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"{PlacementRootName} must contain exactly {PlacementCount} slots.");
            }

            var walkingSlot = placementRoot.Find(WalkingSlotName) ??
                throw new InvalidOperationException(WalkingSlotName + " is missing.");
            if (walkingSlot.GetSiblingIndex() != 2)
            {
                throw new InvalidOperationException("The Ostinato walking object must remain in slot 03.");
            }
            if (walkingSlot.childCount != 1)
            {
                throw new InvalidOperationException("The Ostinato walking slot must contain exactly one model child.");
            }

            var walkingModel = walkingSlot.GetChild(0);
            if (walkingModel.name != WalkingModelName)
            {
                throw new InvalidOperationException(
                    $"Expected walking model {WalkingModelName}, found {walkingModel.name}.");
            }

            var walkingRenderer = walkingModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("The Ostinato walking model must contain one skinned renderer.");
            var walkingAnimator = walkingModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("The Ostinato walking model is missing its Animator.");
            if (walkingAnimator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("The Ostinato walking Animator has no controller.");
            }

            ConfigureWalkingSynchronizedModelImporter();
            var approvedRenderer = RequireAssetRenderer(ApprovedModelPath);
            var approvedMesh = approvedRenderer.sharedMesh ??
                throw new InvalidOperationException("The approved Ostinato mesh is missing.");
            var synchronizedRenderer = RequireAssetRenderer(WalkingSynchronizedModelPath);
            var synchronizedMesh = synchronizedRenderer.sharedMesh ??
                throw new InvalidOperationException("The walking-compatible approved Ostinato mesh is missing.");
            var approvedMaterials = MaterialDefinitions
                .Select(definition => AssetDatabase.LoadAssetAtPath<Material>(definition.MaterialPath) ??
                    throw new InvalidOperationException("Approved material is missing: " + definition.MaterialPath))
                .ToArray();
            if (approvedMesh.subMeshCount != approvedMaterials.Length ||
                synchronizedMesh.subMeshCount != approvedMaterials.Length)
            {
                throw new InvalidOperationException(
                    $"Approved Ostinato requires {approvedMaterials.Length} submeshes. " +
                    $"Approved={approvedMesh.subMeshCount}, Synchronized={synchronizedMesh.subMeshCount}.");
            }
            var approvedAppearanceFingerprint = BuildMeshAppearanceFingerprint(approvedMesh);
            var synchronizedAppearanceFingerprint = BuildMeshAppearanceFingerprint(synchronizedMesh);
            if (synchronizedAppearanceFingerprint != approvedAppearanceFingerprint)
            {
                throw new InvalidOperationException("The walking-compatible mesh appearance data differs from the approved static mesh.");
            }
            if (walkingRenderer.sharedMesh == synchronizedMesh &&
                walkingRenderer.sharedMaterials.SequenceEqual(approvedMaterials))
            {
                ReviewSynchronizedWalkingPlayback(scene, walkingModel, walkingRenderer, walkingAnimator);
                return;
            }

            var walkingBoneNames = RequireBoneNames(walkingRenderer, "walking scene model");
            var synchronizedBoneNames = RequireBoneNames(synchronizedRenderer, "walking-compatible approved model");
            if (!walkingBoneNames.SequenceEqual(synchronizedBoneNames))
            {
                throw new InvalidOperationException("Walking and synchronized Ostinato bone order differs.");
            }

            var walkingBonePaths = RequireBonePaths(walkingRenderer, walkingModel, "walking scene model");
            var synchronizedBonePaths = RequireBonePaths(
                synchronizedRenderer,
                synchronizedRenderer.transform.root,
                "walking-compatible approved model");
            var boneHierarchyPathsMatch = walkingBonePaths.SequenceEqual(synchronizedBonePaths);
            if (!boneHierarchyPathsMatch)
            {
                throw new InvalidOperationException("Walking and synchronized Ostinato bone hierarchy paths differ.");
            }
            var maximumBoneRestMatrixDifference = CalculateMaximumBoneRestMatrixDifference(
                walkingRenderer,
                walkingModel,
                synchronizedRenderer,
                synchronizedRenderer.transform.root);

            var sourceHashBefore = ComputeSha256(ProjectAbsolutePath(WalkingSourceRelativePath));
            var importedWalkingHashBefore = ComputeSha256(ProjectAbsolutePath(WalkingModelPath));
            if (sourceHashBefore != importedWalkingHashBefore)
            {
                throw new InvalidOperationException("The imported walking FBX differs from the supplied source.");
            }

            var approvedHashBefore = ComputeSha256(ProjectAbsolutePath(ApprovedModelPath));
            var synchronizedHashBefore = ComputeSha256(ProjectAbsolutePath(WalkingSynchronizedModelPath));
            var animationFingerprintBefore = BuildAnimationFingerprint(walkingAnimator);
            var animatorSnapshot = new AnimatorSnapshot(walkingAnimator);
            var otherSlotFingerprints = placementRoot.Cast<Transform>()
                .Where(slot => slot != walkingSlot)
                .ToDictionary(slot => slot, BuildSlotFingerprint);
            var walkingSlotPosition = walkingSlot.localPosition;
            var walkingSlotRotation = walkingSlot.localRotation;
            var walkingSlotScale = walkingSlot.localScale;
            var walkingSlotSiblingIndex = walkingSlot.GetSiblingIndex();
            var previousMeshPath = AssetDatabase.GetAssetPath(walkingRenderer.sharedMesh);
            var previousMaterials = walkingRenderer.sharedMaterials
                .Select(material => material != null ? AssetDatabase.GetAssetPath(material) : "None")
                .ToArray();
            var previousModelPosition = walkingModel.localPosition;
            var previousModelRotation = walkingModel.localRotation;
            var previousModelScale = walkingModel.localScale;
            var synchronizedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WalkingSynchronizedModelPath) ??
                throw new InvalidOperationException("The walking-compatible Ostinato model asset is missing.");
            GameObject synchronizedInstance = null;
            try
            {
                synchronizedInstance = PrefabUtility.InstantiatePrefab(synchronizedAsset, scene) as GameObject ??
                    throw new InvalidOperationException("The walking-compatible Ostinato model could not be instantiated.");
                synchronizedInstance.name = WalkingModelName;
                synchronizedInstance.transform.SetParent(walkingSlot, false);
                synchronizedInstance.transform.localPosition = previousModelPosition;
                synchronizedInstance.transform.localRotation = previousModelRotation;
                synchronizedInstance.transform.localScale = previousModelScale;

                var instanceRenderer = synchronizedInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                    throw new InvalidOperationException("The synchronized walking instance must contain one skinned renderer.");
                instanceRenderer.sharedMaterials = approvedMaterials;
                instanceRenderer.updateWhenOffscreen = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instanceRenderer);
                EditorUtility.SetDirty(instanceRenderer);

                var instanceAnimator = synchronizedInstance.GetComponent<Animator>();
                if (instanceAnimator == null)
                {
                    instanceAnimator = synchronizedInstance.AddComponent<Animator>();
                }
                animatorSnapshot.ApplyPlaybackSettingsTo(instanceAnimator);
                EditorUtility.SetDirty(instanceAnimator);

                if (instanceRenderer.sharedMesh != synchronizedMesh ||
                    !instanceRenderer.sharedMaterials.SequenceEqual(approvedMaterials))
                {
                    throw new InvalidOperationException("The synchronized walking instance does not use the approved appearance assets.");
                }
                if (!RequireBoneNames(instanceRenderer, "synchronized walking instance").SequenceEqual(synchronizedBoneNames))
                {
                    throw new InvalidOperationException("The synchronized walking instance bone order differs from its source asset.");
                }
                var instanceBonePaths = RequireBonePaths(
                    instanceRenderer,
                    synchronizedInstance.transform,
                    "synchronized walking instance");
                if (!instanceBonePaths.SequenceEqual(walkingBonePaths))
                {
                    throw new InvalidOperationException("The synchronized walking instance does not match the animation hierarchy paths.");
                }
                animatorSnapshot.AssertEquivalentPlaybackSettings(instanceAnimator);
                if (BuildAnimationFingerprint(instanceAnimator) != animationFingerprintBefore)
                {
                    throw new InvalidOperationException("Walking animation data changed on the synchronized model instance.");
                }

                UnityEngine.Object.DestroyImmediate(walkingModel.gameObject);
                walkingModel = synchronizedInstance.transform;
                walkingRenderer = instanceRenderer;
                walkingAnimator = instanceAnimator;
                synchronizedInstance = null;
            }
            finally
            {
                if (synchronizedInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(synchronizedInstance);
                }
            }

            if (walkingSlot.childCount != 1 || walkingSlot.GetChild(0) != walkingModel)
            {
                throw new InvalidOperationException("The synchronized Ostinato walking slot does not contain exactly one model.");
            }

            foreach (var entry in otherSlotFingerprints)
            {
                if (BuildSlotFingerprint(entry.Key) != entry.Value)
                {
                    throw new InvalidOperationException("Another Ostinato slot changed: " + entry.Key.name);
                }
            }
            if (walkingSlot.localPosition != walkingSlotPosition ||
                walkingSlot.localRotation != walkingSlotRotation ||
                walkingSlot.localScale != walkingSlotScale ||
                walkingSlot.GetSiblingIndex() != walkingSlotSiblingIndex)
            {
                throw new InvalidOperationException("The Ostinato walking slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after walking appearance synchronization.");
            }

            var sourceHashAfter = ComputeSha256(ProjectAbsolutePath(WalkingSourceRelativePath));
            var importedWalkingHashAfter = ComputeSha256(ProjectAbsolutePath(WalkingModelPath));
            var approvedHashAfter = ComputeSha256(ProjectAbsolutePath(ApprovedModelPath));
            var synchronizedHashAfter = ComputeSha256(ProjectAbsolutePath(WalkingSynchronizedModelPath));
            if (sourceHashAfter != sourceHashBefore ||
                importedWalkingHashAfter != importedWalkingHashBefore ||
                approvedHashAfter != approvedHashBefore ||
                synchronizedHashAfter != synchronizedHashBefore)
            {
                throw new InvalidOperationException("An Ostinato source FBX changed during appearance synchronization.");
            }
            animatorSnapshot.AssertEquivalentPlaybackSettings(walkingAnimator);
            if (BuildAnimationFingerprint(walkingAnimator) != animationFingerprintBefore)
            {
                throw new InvalidOperationException("Walking animation data changed after scene save.");
            }

            Directory.CreateDirectory(ProjectAbsolutePath(WalkingAppearanceValidationRoot));
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + WalkingSlotName + "/" + WalkingModelName);
            report.AppendLine("PreviousMesh=" + previousMeshPath);
            report.AppendLine("PreviousMaterials=" + string.Join("|", previousMaterials));
            report.AppendLine("ApprovedAppearanceMesh=" + AssetDatabase.GetAssetPath(approvedMesh));
            report.AppendLine("SynchronizedMesh=" + AssetDatabase.GetAssetPath(synchronizedMesh));
            report.AppendLine("SynchronizedMaterials=" + string.Join("|", approvedMaterials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("ApprovedAppearanceFingerprint=" + approvedAppearanceFingerprint);
            report.AppendLine("SynchronizedAppearanceFingerprint=" + synchronizedAppearanceFingerprint);
            report.AppendLine("AppearanceDataMatchesApproved=True");
            report.AppendLine("VertexCount=" + synchronizedMesh.vertexCount);
            report.AppendLine("SubMeshCount=" + synchronizedMesh.subMeshCount);
            report.AppendLine("UvCount=" + synchronizedMesh.uv.Length);
            report.AppendLine("BoneCount=" + walkingRenderer.bones.Length);
            report.AppendLine("BoneNames=" + string.Join("|", synchronizedBoneNames));
            report.AppendLine("WalkingBonePaths=" + string.Join("|", walkingBonePaths));
            report.AppendLine("SynchronizedBonePaths=" + string.Join("|", synchronizedBonePaths));
            report.AppendLine("BoneHierarchyPathsMatch=" + boneHierarchyPathsMatch);
            report.AppendLine("PreviousVsSynchronizedRestMatrixDifference=" + maximumBoneRestMatrixDifference.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            report.AppendLine("DirectMeshBindingCompatible=False");
            report.AppendLine("SynchronizedRigUsed=True");
            report.AppendLine("AnimationHierarchyPathsMatch=True");
            report.AppendLine("WalkingSourceSha256Before=" + sourceHashBefore);
            report.AppendLine("WalkingSourceSha256After=" + sourceHashAfter);
            report.AppendLine("WalkingUnityCopySha256Before=" + importedWalkingHashBefore);
            report.AppendLine("WalkingUnityCopySha256After=" + importedWalkingHashAfter);
            report.AppendLine("ApprovedModelSha256Before=" + approvedHashBefore);
            report.AppendLine("ApprovedModelSha256After=" + approvedHashAfter);
            report.AppendLine("SynchronizedModelSha256Before=" + synchronizedHashBefore);
            report.AppendLine("SynchronizedModelSha256After=" + synchronizedHashAfter);
            report.AppendLine("AnimationFingerprintBefore=" + animationFingerprintBefore);
            report.AppendLine("AnimationFingerprintAfter=" + BuildAnimationFingerprint(walkingAnimator));
            report.AppendLine("AnimationChanged=False");
            report.AppendLine("AnimatorPlaybackSettingsChanged=False");
            report.AppendLine("AnimatorAvatarReboundToSynchronizedRig=True");
            report.AppendLine("WalkingTransformChanged=False");
            report.AppendLine("OtherOstinatoSlotsChanged=False");
            report.AppendLine("SceneSaved=True");
            File.WriteAllText(
                ProjectAbsolutePath(WalkingAppearanceValidationRoot + "/Ostinato_WalkingAppearanceSync.txt"),
                report.ToString(),
                new UTF8Encoding(false));

            CaptureWalkingAppearanceComparison(placementRoot, walkingSlot);
            Selection.activeObject = null;
            Debug.Log(
                "OstinatoWalkingAppearanceSynchronized" +
                ", Target=" + PlacementRootName + "/" + WalkingSlotName +
                ", Mesh=" + synchronizedMesh.name +
                ", SubMeshes=" + synchronizedMesh.subMeshCount +
                ", Materials=" + approvedMaterials.Length +
                ", Bones=" + walkingRenderer.bones.Length +
                ", AnimationChanged=False" +
                ", OtherOstinatoSlotsChanged=False" +
                ", FinalCaptureSaved=True");
        }

        private static void ReviewSynchronizedWalkingPlayback(
            Scene scene,
            Transform walkingModel,
            SkinnedMeshRenderer walkingRenderer,
            Animator walkingAnimator)
        {
            var sceneWasDirty = scene.isDirty;
            var animationFingerprintBefore = BuildAnimationFingerprint(walkingAnimator);
            var clips = walkingAnimator.runtimeAnimatorController.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "The synchronized Ostinato walking controller must reference exactly one clip. Count=" + clips.Length);
            }

            var clip = clips[0];
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var unresolvedBindings = bindings
                .Where(binding => !string.IsNullOrEmpty(binding.path) && walkingModel.Find(binding.path) == null)
                .Select(binding => binding.path + "|" + binding.propertyName)
                .Distinct()
                .ToArray();
            if (unresolvedBindings.Length > 0)
            {
                throw new InvalidOperationException(
                    "The synchronized walking rig has unresolved animation bindings: " + string.Join("|", unresolvedBindings));
            }

            var clone = UnityEngine.Object.Instantiate(walkingModel.gameObject);
            clone.name = WalkingModelName + "_PlaybackReviewClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;
            var cloneAnimator = clone.GetComponent<Animator>();
            if (cloneAnimator != null)
            {
                cloneAnimator.enabled = false;
            }
            var cloneRenderer = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("The synchronized walking review clone has no renderer.");
            var bakedMesh = new Mesh { name = "Ostinato_WalkingPlaybackReview_BakedMesh" };
            var sampleFractions = new[] { 0f, 0.25f, 0.5f, 0.75f, 0.99f };
            Vector3[] initialVertices = null;
            var maximumVertexMotion = 0f;
            var sampleReport = new StringBuilder();
            try
            {
                foreach (var fraction in sampleFractions)
                {
                    clip.SampleAnimation(clone, clip.length * fraction);
                    cloneRenderer.BakeMesh(bakedMesh);
                    var vertices = bakedMesh.vertices;
                    if (vertices.Length != walkingRenderer.sharedMesh.vertexCount ||
                        vertices.Any(vertex =>
                            float.IsNaN(vertex.x) || float.IsInfinity(vertex.x) ||
                            float.IsNaN(vertex.y) || float.IsInfinity(vertex.y) ||
                            float.IsNaN(vertex.z) || float.IsInfinity(vertex.z)))
                    {
                        throw new InvalidOperationException("Walking playback produced invalid mesh vertices at fraction " + fraction);
                    }
                    if (bakedMesh.bounds.size.sqrMagnitude <= 0.000001f)
                    {
                        throw new InvalidOperationException("Walking playback produced empty bounds at fraction " + fraction);
                    }

                    if (initialVertices == null)
                    {
                        initialVertices = vertices;
                    }
                    else
                    {
                        for (var index = 0; index < vertices.Length; index++)
                        {
                            maximumVertexMotion = Mathf.Max(
                                maximumVertexMotion,
                                Vector3.Distance(initialVertices[index], vertices[index]));
                        }
                    }
                    sampleReport.AppendLine(
                        "Sample=" + fraction.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) +
                        ",Time=" + (clip.length * fraction).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) +
                        ",Vertices=" + vertices.Length +
                        ",BoundsCenter=" + FormatVector3(bakedMesh.bounds.center) +
                        ",BoundsSize=" + FormatVector3(bakedMesh.bounds.size));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
                UnityEngine.Object.DestroyImmediate(clone);
                Selection.activeObject = null;
            }

            if (maximumVertexMotion <= 0.001f)
            {
                throw new InvalidOperationException("The synchronized walking clip did not deform the visible mesh.");
            }
            var animationFingerprintAfter = BuildAnimationFingerprint(walkingAnimator);
            if (animationFingerprintAfter != animationFingerprintBefore)
            {
                throw new InvalidOperationException("Walking animation data changed during playback review.");
            }
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Walking playback review changed the scene dirty state.");
            }

            Directory.CreateDirectory(ProjectAbsolutePath(WalkingAppearanceValidationRoot));
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + WalkingSlotName + "/" + WalkingModelName);
            report.AppendLine("Clip=" + AssetDatabase.GetAssetPath(clip) + ":" + clip.name);
            report.AppendLine("ClipLength=" + clip.length.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
            report.AppendLine("ClipFrameRate=" + clip.frameRate.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            report.AppendLine("CurveBindings=" + bindings.Length);
            report.AppendLine("ResolvedBindings=" + bindings.Length);
            report.AppendLine("UnresolvedBindings=0");
            report.AppendLine("MaximumVertexMotion=" + maximumVertexMotion.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture));
            report.Append(sampleReport);
            report.AppendLine("AnimationFingerprintBefore=" + animationFingerprintBefore);
            report.AppendLine("AnimationFingerprintAfter=" + animationFingerprintAfter);
            report.AppendLine("AnimationChanged=False");
            report.AppendLine("VisibleMeshDeformed=True");
            report.AppendLine("InvalidVertices=False");
            report.AppendLine("SceneChanged=False");
            report.AppendLine("CaptureSaved=False");
            File.WriteAllText(
                ProjectAbsolutePath(WalkingAppearanceValidationRoot + "/Ostinato_WalkingPlaybackAppearanceReview.txt"),
                report.ToString(),
                new UTF8Encoding(false));
            Debug.Log(
                "OstinatoWalkingAppearancePlaybackReviewed" +
                ", Clip=" + clip.name +
                ", Bindings=" + bindings.Length +
                ", MaximumVertexMotion=" + maximumVertexMotion.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) +
                ", AnimationChanged=False" +
                ", CaptureSaved=False");
        }

        private static string[] RequireBonePaths(
            SkinnedMeshRenderer renderer,
            Transform modelRoot,
            string label)
        {
            if (renderer.bones == null || renderer.bones.Any(bone => bone == null))
            {
                throw new InvalidOperationException(label + " contains a missing skinned-mesh bone.");
            }

            return renderer.bones.Select(bone => RequireRelativePath(modelRoot, bone, label)).ToArray();
        }

        private static string RequireRelativePath(Transform root, Transform target, string label)
        {
            var names = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            if (current != root)
            {
                throw new InvalidOperationException(label + " contains a bone outside its model root: " + target.name);
            }

            return string.Join("/", names);
        }

        private static float CalculateMaximumBoneRestMatrixDifference(
            SkinnedMeshRenderer walkingRenderer,
            Transform walkingRoot,
            SkinnedMeshRenderer synchronizedRenderer,
            Transform synchronizedRoot)
        {
            if (walkingRenderer.bones.Length != synchronizedRenderer.bones.Length)
            {
                throw new InvalidOperationException(
                    $"Synchronized bone count {synchronizedRenderer.bones.Length} differs from walking bone count {walkingRenderer.bones.Length}.");
            }

            var maximumDifference = 0f;
            for (var boneIndex = 0; boneIndex < walkingRenderer.bones.Length; boneIndex++)
            {
                var walkingMatrix = walkingRoot.worldToLocalMatrix *
                                    walkingRenderer.bones[boneIndex].localToWorldMatrix;
                var synchronizedMatrix = synchronizedRoot.worldToLocalMatrix *
                                         synchronizedRenderer.bones[boneIndex].localToWorldMatrix;
                for (var element = 0; element < 16; element++)
                {
                    maximumDifference = Mathf.Max(
                        maximumDifference,
                        Mathf.Abs(walkingMatrix[element] - synchronizedMatrix[element]));
                }
            }

            return maximumDifference;
        }

        private static string BuildMeshAppearanceFingerprint(Mesh mesh)
        {
            var report = new StringBuilder();
            report.AppendLine("Vertices=" + mesh.vertexCount);
            report.AppendLine("SubMeshes=" + mesh.subMeshCount);
            AppendVector3Array(report, "Vertex", mesh.vertices);
            AppendVector3Array(report, "Normal", mesh.normals);
            AppendVector4Array(report, "Tangent", mesh.tangents);
            AppendVector2Array(report, "Uv", mesh.uv);
            foreach (var weight in mesh.boneWeights)
            {
                report.AppendLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "BoneWeight={0},{1},{2},{3}:{4:R},{5:R},{6:R},{7:R}",
                    weight.boneIndex0,
                    weight.boneIndex1,
                    weight.boneIndex2,
                    weight.boneIndex3,
                    weight.weight0,
                    weight.weight1,
                    weight.weight2,
                    weight.weight3));
            }
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                report.AppendLine("SubMesh=" + subMesh + ":" + string.Join(",", mesh.GetIndices(subMesh)));
            }
            return ComputeSha256(Encoding.UTF8.GetBytes(report.ToString()));
        }

        private static void AppendVector2Array(StringBuilder report, string label, Vector2[] values)
        {
            foreach (var value in values)
            {
                report.AppendLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}={1:R},{2:R}",
                    label,
                    value.x,
                    value.y));
            }
        }

        private static void AppendVector3Array(StringBuilder report, string label, Vector3[] values)
        {
            foreach (var value in values)
            {
                report.AppendLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}={1:R},{2:R},{3:R}",
                    label,
                    value.x,
                    value.y,
                    value.z));
            }
        }

        private static void AppendVector4Array(StringBuilder report, string label, Vector4[] values)
        {
            foreach (var value in values)
            {
                report.AppendLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}={1:R},{2:R},{3:R},{4:R}",
                    label,
                    value.x,
                    value.y,
                    value.z,
                    value.w));
            }
        }

        private static string BuildAnimationFingerprint(Animator animator)
        {
            var report = new StringBuilder();
            var controller = animator.runtimeAnimatorController;
            var controllerPath = AssetDatabase.GetAssetPath(controller);
            report.AppendLine("Controller=" + controllerPath);
            report.AppendLine("ControllerSha256=" + ComputeAssetHashOrNone(controllerPath));
            report.AppendLine("ApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("CullingMode=" + animator.cullingMode);
            report.AppendLine("UpdateMode=" + animator.updateMode);
            report.AppendLine("Enabled=" + animator.enabled);
            report.AppendLine("Speed=" + animator.speed);
            foreach (var clip in controller.animationClips
                         .Where(value => value != null)
                         .Distinct()
                         .OrderBy(value => AssetDatabase.GetAssetPath(value), StringComparer.Ordinal)
                         .ThenBy(value => value.name, StringComparer.Ordinal))
            {
                var clipPath = AssetDatabase.GetAssetPath(clip);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                report.AppendLine(
                    "Clip=" + clipPath + ":" + clip.name +
                    ",Sha256=" + ComputeAssetHashOrNone(clipPath) +
                    ",Length=" + clip.length.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    ",FrameRate=" + clip.frameRate.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                    ",LoopTime=" + settings.loopTime +
                    ",LoopBlend=" + settings.loopBlend);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                             .OrderBy(value => value.path, StringComparer.Ordinal)
                             .ThenBy(value => value.propertyName, StringComparer.Ordinal))
                {
                    report.AppendLine(
                        "Binding=" + binding.path + "|" + binding.type.FullName + "|" + binding.propertyName);
                }
            }

            return ComputeSha256(Encoding.UTF8.GetBytes(report.ToString()));
        }

        private static string ComputeAssetHashOrNone(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return "None";
            }

            var absolutePath = ProjectAbsolutePath(assetPath);
            return File.Exists(absolutePath) ? ComputeSha256(absolutePath) : "None";
        }

        private static string BuildSlotFingerprint(Transform slot)
        {
            var report = new StringBuilder();
            foreach (var target in slot.GetComponentsInChildren<Transform>(true))
            {
                var path = RequireRelativePath(slot, target, "Ostinato slot");
                report.AppendLine(
                    "Transform=" + path +
                    ",Active=" + target.gameObject.activeSelf +
                    ",Position=" + FormatVector3(target.localPosition) +
                    ",Rotation=" + FormatQuaternion(target.localRotation) +
                    ",Scale=" + FormatVector3(target.localScale) +
                    ",Sibling=" + target.GetSiblingIndex());
                foreach (var renderer in target.GetComponents<SkinnedMeshRenderer>())
                {
                    report.AppendLine(
                        "Renderer=" + path +
                        ",Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh) +
                        ",Materials=" + string.Join("|", renderer.sharedMaterials.Select(
                            material => material != null ? AssetDatabase.GetAssetPath(material) : "None")));
                }
                foreach (var animator in target.GetComponents<Animator>())
                {
                    report.AppendLine("Animator=" + path + ",Fingerprint=" + BuildAnimationFingerprint(animator));
                }
            }

            return ComputeSha256(Encoding.UTF8.GetBytes(report.ToString()));
        }

        private static string FormatVector3(Vector3 value)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "({0:R},{1:R},{2:R})",
                value.x,
                value.y,
                value.z);
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "({0:R},{1:R},{2:R},{3:R})",
                value.x,
                value.y,
                value.z,
                value.w);
        }

        private static void CaptureWalkingAppearanceComparison(Transform placementRoot, Transform walkingSlot)
        {
            var scene = placementRoot.gameObject.scene;
            var sceneWasDirty = scene.isDirty;
            var staticSlot = placementRoot.GetChild(0);
            var staticModel = staticSlot.Find(ModelChildName) ??
                throw new InvalidOperationException("The static Ostinato comparison model is missing.");
            var walkingModel = walkingSlot.Find(WalkingModelName) ??
                throw new InvalidOperationException("The walking Ostinato comparison model is missing.");
            var staticRenderer = staticModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("The static Ostinato comparison renderer is missing.");
            var walkingRenderer = walkingModel.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("The walking Ostinato comparison renderer is missing.");
            if (BuildMeshAppearanceFingerprint(staticRenderer.sharedMesh) !=
                    BuildMeshAppearanceFingerprint(walkingRenderer.sharedMesh) ||
                !staticRenderer.sharedMaterials.SequenceEqual(walkingRenderer.sharedMaterials))
            {
                throw new InvalidOperationException("Static and walking Ostinato appearance assets differ before capture.");
            }

            var cameraObject = new GameObject("Ostinato_WalkingAppearance_Camera")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = WalkingCaptureLayer,
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.cullingMask = 1 << WalkingCaptureLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.105f, 0.118f, 0.094f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            var lights = new[]
            {
                CreateWalkingCaptureLight("Ostinato_WalkingAppearance_Key", new Vector3(28f, 180f, 0f), new Color(1f, 0.88f, 0.74f), 2.15f),
                CreateWalkingCaptureLight("Ostinato_WalkingAppearance_Fill", new Vector3(34f, 270f, 0f), new Color(0.72f, 0.82f, 1f), 1.80f),
                CreateWalkingCaptureLight("Ostinato_WalkingAppearance_Back", new Vector3(24f, 0f, 0f), new Color(0.86f, 0.92f, 1f), 1.80f),
            };
            Texture2D[] staticViews = null;
            Texture2D[] walkingViews = null;
            Texture2D[] approvedViews = null;
            Texture2D composite = null;
            try
            {
                staticViews = RenderWalkingAppearanceViews(staticModel.gameObject, camera);
                walkingViews = RenderWalkingAppearanceViews(walkingModel.gameObject, camera);
                approvedViews = new[]
                {
                    LoadPng(ApprovedFrontRender),
                    LoadPng(ApprovedSideRender),
                    LoadPng(ApprovedBackRender),
                };
                composite = new Texture2D(2400, 1500, TextureFormat.RGBA32, false, false);
                FillTexture(composite, new Color32(27, 30, 24, 255));
                for (var row = 0; row < 3; row++)
                {
                    var y = (2 - row) * 500;
                    PasteFit(composite, approvedViews[row], new RectInt(0, y, 800, 500));
                    PasteFit(composite, staticViews[row], new RectInt(800, y, 800, 500));
                    PasteFit(composite, walkingViews[row], new RectInt(1600, y, 800, 500));
                }
                composite.Apply(false, false);

                var folder = ProjectAbsolutePath(WalkingAppearanceValidationRoot);
                Directory.CreateDirectory(folder);
                const string fileName = "Ostinato_WalkingAppearanceSync_Comparison.png";
                File.WriteAllBytes(Path.Combine(folder, fileName), composite.EncodeToPNG());
                File.WriteAllLines(
                    Path.Combine(folder, "Ostinato_WalkingAppearanceCaptureManifest.txt"),
                    new[]
                    {
                        "Capture=" + fileName,
                        "Rows=Front;Side;Back",
                        "Columns=ApprovedBlenderSample;UnityStaticReference;UnityWalkingAppearanceSynced",
                        "SharedMesh=" + AssetDatabase.GetAssetPath(walkingRenderer.sharedMesh),
                        "SharedMaterials=" + string.Join("|", walkingRenderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)),
                        "AnimationChanged=False",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True",
                    });
            }
            finally
            {
                DestroyTextures(staticViews);
                DestroyTextures(walkingViews);
                DestroyTextures(approvedViews);
                UnityEngine.Object.DestroyImmediate(composite);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                foreach (var light in lights)
                {
                    UnityEngine.Object.DestroyImmediate(light);
                }
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Walking appearance capture changed scene dirty state.");
            }
        }

        private static Texture2D[] RenderWalkingAppearanceViews(GameObject source, Camera camera)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + "_WalkingAppearanceCaptureClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
            SetLayerRecursively(clone.transform, WalkingCaptureLayer);
            try
            {
                var bounds = CalculateRendererBounds(clone.transform);
                var target = bounds.center;
                var distance = CalculateCaptureDistance(bounds, camera.fieldOfView, 800f / 500f) * 0.90f;
                return new[]
                {
                    RenderPreview(camera, target + Vector3.forward * distance, target, 800, 500),
                    RenderPreview(camera, target + Vector3.right * distance, target, 800, 500),
                    RenderPreview(camera, target + Vector3.back * distance, target, 800, 500),
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static GameObject CreateWalkingCaptureLight(
            string name,
            Vector3 eulerAngles,
            Color color,
            float intensity)
        {
            var lightObject = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = WalkingCaptureLayer,
            };
            lightObject.transform.rotation = Quaternion.Euler(eulerAngles);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << WalkingCaptureLayer;
            return lightObject;
        }

        private static Texture2D RenderPreview(
            Camera camera,
            Vector3 cameraPosition,
            Vector3 target,
            int width,
            int height)
        {
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Ostinato capture model has no renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static float CalculateCaptureDistance(Bounds bounds, float fieldOfView, float aspect)
        {
            var vertical = Mathf.Max(bounds.extents.y, 0.01f);
            var horizontal = Mathf.Max(bounds.extents.x / Mathf.Max(aspect, 0.01f), 0.01f);
            var halfAngle = fieldOfView * 0.5f * Mathf.Deg2Rad;
            return Mathf.Max(vertical, horizontal) / Mathf.Tan(halfAngle) + Mathf.Max(bounds.extents.z, 0.01f);
        }

        private static Texture2D LoadPng(string relativePath)
        {
            var absolutePath = ProjectAbsolutePath(relativePath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Approved Ostinato render is missing.", absolutePath);
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!texture.LoadImage(File.ReadAllBytes(absolutePath), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Approved Ostinato render could not be loaded: " + relativePath);
            }
            return texture;
        }

        private static void FillTexture(Texture2D texture, Color32 color)
        {
            var pixels = Enumerable.Repeat(color, texture.width * texture.height).ToArray();
            texture.SetPixels32(pixels);
        }

        private static void PasteFit(Texture2D destination, Texture2D source, RectInt area)
        {
            var scale = Mathf.Min((float)area.width / source.width, (float)area.height / source.height);
            var width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var xOffset = area.x + (area.width - width) / 2;
            var yOffset = area.y + (area.height - height) / 2;
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                var v = height > 1 ? (float)y / (height - 1) : 0f;
                for (var x = 0; x < width; x++)
                {
                    var u = width > 1 ? (float)x / (width - 1) : 0f;
                    pixels[y * width + x] = source.GetPixelBilinear(u, v);
                }
            }
            destination.SetPixels(xOffset, yOffset, width, height, pixels);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = layer;
            }
        }

        private static void DestroyTextures(Texture2D[] textures)
        {
            if (textures == null)
            {
                return;
            }
            foreach (var texture in textures)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        private readonly struct AnimatorSnapshot
        {
            private readonly RuntimeAnimatorController controller;
            private readonly bool applyRootMotion;
            private readonly AnimatorCullingMode cullingMode;
            private readonly AnimatorUpdateMode updateMode;
            private readonly bool enabled;
            private readonly float speed;

            public AnimatorSnapshot(Animator target)
            {
                controller = target.runtimeAnimatorController;
                applyRootMotion = target.applyRootMotion;
                cullingMode = target.cullingMode;
                updateMode = target.updateMode;
                enabled = target.enabled;
                speed = target.speed;
            }

            public void ApplyPlaybackSettingsTo(Animator target)
            {
                target.runtimeAnimatorController = controller;
                target.applyRootMotion = applyRootMotion;
                target.cullingMode = cullingMode;
                target.updateMode = updateMode;
                target.enabled = enabled;
                target.speed = speed;
            }

            public void AssertEquivalentPlaybackSettings(Animator target)
            {
                if (target == null ||
                    target.runtimeAnimatorController != controller ||
                    target.applyRootMotion != applyRootMotion ||
                    target.cullingMode != cullingMode ||
                    target.updateMode != updateMode ||
                    target.enabled != enabled ||
                    !Mathf.Approximately(target.speed, speed))
                {
                    throw new InvalidOperationException("The Ostinato walking Animator settings changed.");
                }
            }
        }

        private static void ConfigureApprovedModelImporter()
        {
            var importer = AssetImporter.GetAtPath(ApprovedModelPath) as ModelImporter ??
                throw new InvalidOperationException("Approved Ostinato model importer is missing.");
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }

        private static void ConfigureWalkingSynchronizedModelImporter()
        {
            var importer = AssetImporter.GetAtPath(WalkingSynchronizedModelPath) as ModelImporter ??
                throw new InvalidOperationException("Walking-compatible Ostinato model importer is missing.");
            importer.isReadable = true;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }

        private static void ConfigureApprovedTextureImporters()
        {
            foreach (var definition in MaterialDefinitions)
            {
                ConfigureTextureImporter(definition.TexturePath("BaseColor"), false, true, false);
                ConfigureTextureImporter(definition.TexturePath("Normal"), true, false, false);
                ConfigureTextureImporter(definition.TexturePath("MetallicSmoothness"), false, false, true);
            }
        }

        private static void ConfigureTextureImporter(string assetPath, bool normalMap, bool sRgb, bool alphaFromInput)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter ??
                throw new InvalidOperationException("Approved Ostinato texture importer is missing: " + assetPath);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = sRgb;
            importer.alphaSource = alphaFromInput ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateMaterial(MaterialDefinition definition)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("Universal Render Pipeline/Lit shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(definition.MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = definition.MaterialName };
                AssetDatabase.CreateAsset(material, definition.MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.TexturePath("BaseColor")) ??
                throw new InvalidOperationException("Approved base color is missing for " + definition.Label + ".");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.TexturePath("Normal")) ??
                throw new InvalidOperationException("Approved normal is missing for " + definition.Label + ".");
            var metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.TexturePath("MetallicSmoothness")) ??
                throw new InvalidOperationException("Approved metallic-smoothness is missing for " + definition.Label + ".");

            if (definition.Label == "HookBlade")
            {
                // Unity-only override: remove baked directional stripes while keeping dark steel and edge reflections.
                var bladeColor = new Color(0.11f, 0.15f, 0.16f, 1f);
                material.SetColor("_BaseColor", bladeColor);
                material.SetColor("_Color", bladeColor);
                material.SetTexture("_BaseMap", null);
                material.SetTexture("_MainTex", null);
                material.SetTexture("_BumpMap", null);
                material.SetFloat("_BumpScale", 0f);
                material.SetTexture("_MetallicGlossMap", null);
                material.SetFloat("_Metallic", 0.86f);
                material.SetFloat("_Smoothness", 0.60f);
                material.DisableKeyword("_NORMALMAP");
                material.DisableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(
                        definition.BaseColorMultiplier,
                        definition.BaseColorMultiplier,
                        definition.BaseColorMultiplier,
                        1f));
                material.SetTexture("_BaseMap", baseColor);
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1f);
                material.SetTexture("_MetallicGlossMap", metallicSmoothness);
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", 2f);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static SkinnedMeshRenderer RequireAssetRenderer(string assetPath)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) ??
                throw new InvalidOperationException("Ostinato model asset is missing: " + assetPath);
            return model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Ostinato model must contain one skinned renderer: " + assetPath);
        }

        private static string[] RequireBoneNames(SkinnedMeshRenderer renderer, string label)
        {
            if (renderer.bones == null || renderer.bones.Any(bone => bone == null))
            {
                throw new InvalidOperationException(label + " contains a missing skinned-mesh bone.");
            }
            return renderer.bones.Select(bone => bone.name).ToArray();
        }

        private static void RequireMatchingBounds(Bounds source, Bounds approved)
        {
            if ((source.center - approved.center).sqrMagnitude > 0.000004f ||
                (source.size - approved.size).sqrMagnitude > 0.000004f)
            {
                throw new InvalidOperationException(
                    $"Approved Ostinato bounds differ from source. Source={source}, Approved={approved}");
            }
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(ScenePath + " must be the active open scene.");
            }
            return scene;
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private readonly struct MaterialDefinition
        {
            public readonly string Label;
            public readonly string MaterialName;
            // Converts the approved Blender linear/studio-light response to the Unity URP scene-light range.
            public readonly float BaseColorMultiplier;

            public MaterialDefinition(string label, string materialName, float baseColorMultiplier)
            {
                Label = label;
                MaterialName = materialName;
                BaseColorMultiplier = baseColorMultiplier;
            }

            public string MaterialPath => MaterialRoot + "/" + MaterialName + ".mat";

            public string TexturePath(string channel)
            {
                return TextureRoot + "/Ostinato_" + Label + "_" + channel + ".png";
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly int siblingIndex;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
                siblingIndex = target.GetSiblingIndex();
            }

            public void AssertUnchanged()
            {
                if (target == null || target.localPosition != localPosition || target.localRotation != localRotation ||
                    target.localScale != localScale || target.GetSiblingIndex() != siblingIndex)
                {
                    throw new InvalidOperationException("Ostinato transform changed during appearance application.");
                }
            }
        }
    }
}
