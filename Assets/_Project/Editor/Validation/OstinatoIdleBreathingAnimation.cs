using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class OstinatoIdleBreathingAnimation
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string StaticSlotName = "Ostinato_02_Static_Review";
        private const string IdleSlotName = "Ostinato_02_Idle_Breathing";
        private const string ModelChildName = "Ostinato_Model";
        private const string ApprovedModelPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_ApprovedUnity.fbx";
        private const string AnimationFolderPath = "Assets/_Project/Art/Enemies/Ostinato/Animations";
        private const string IdleMeshPath = AnimationFolderPath + "/Ostinato_02_Idle_Breathing_MorphMesh.asset";
        private const string IdleClipPath = AnimationFolderPath + "/Ostinato_02_Idle_Breathing_Morph.anim";
        private const string IdleControllerPath = AnimationFolderPath + "/Ostinato_02_Idle_Breathing_Morph.controller";
        private const string BlendShapeName = "Ostinato_Idle_Breathing_CoreMorph";
        private const string UpperBlendShapeName = "Ostinato_Idle_Breathing_UpperMorph";
        private const string LowerBlendShapeName = "Ostinato_Idle_Breathing_LowerMorph";
        private const string StateName = "Ostinato_02_Idle_Breathing_Morph";
        private const string ValidationFolderPath = "docs/validation/ostinato_idle_breathing_2026-07-19";
        private const string ApplyReportPath = ValidationFolderPath + "/Ostinato_IdleBreathingApply.txt";
        private const string ReviewImagePath = ValidationFolderPath + "/Ostinato_IdleBreathing_Progression.png";
        private const string LegacyReviewImagePath = ValidationFolderPath + "/Ostinato_IdleBreathing_Neutral_Inhale.png";
        private const string AnimatorPlaybackReportPath = ValidationFolderPath + "/Ostinato_IdleAnimatorPlaybackReview.txt";
        private const string AnimatorPlaybackImagePath = ValidationFolderPath + "/Ostinato_IdleAnimatorPlayback_Progression.png";
        private const float LoopDurationSeconds = 3.20f;
        private const float InhalePeakTimeSeconds = 1.10f;
        private const float UpperPeakTimeSeconds = 0.88f;
        private const float LowerPeakTimeSeconds = 1.25f;
        private const int PlacementCount = 9;
        private const int HookBladeSubMeshIndex = 2;
        private const int ReviewLayer = 30;
        private const int ReviewImageSize = 512;

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Idle Breathing Animation")]
        public static void ApplyOstinatoIdleBreathingAnimation()
        {
            var scene = RequireOpenScene();
            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform ??
                throw new InvalidOperationException(PlacementRootName + " is missing.");
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"{PlacementRootName} must contain exactly {PlacementCount} slots.");
            }

            var idleSlot = placementRoot.Find(IdleSlotName) ?? placementRoot.Find(StaticSlotName) ??
                throw new InvalidOperationException("Ostinato slot 02 is missing.");
            if (idleSlot.GetSiblingIndex() != 1)
            {
                throw new InvalidOperationException("Ostinato idle slot must remain the second placement child.");
            }

            var slotStates = placementRoot.Cast<Transform>()
                .Select(TransformState.Capture)
                .ToArray();
            var otherConfiguredAnimatorsBefore = CountOtherConfiguredAnimators(placementRoot, idleSlot);
            if (otherConfiguredAnimatorsBefore != 0)
            {
                throw new InvalidOperationException(
                    "Only Ostinato slot 02 may receive an animation controller in this step. OtherConfiguredAnimators=" +
                    otherConfiguredAnimatorsBefore.ToString(CultureInfo.InvariantCulture));
            }

            var model = idleSlot.Find(ModelChildName) ??
                throw new InvalidOperationException(idleSlot.name + " is missing " + ModelChildName + ".");
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException(idleSlot.name + " must contain one SkinnedMeshRenderer.");
            var approvedAssetRenderer = RequireApprovedAssetRenderer();
            var approvedMesh = approvedAssetRenderer.sharedMesh ??
                throw new InvalidOperationException("Approved Ostinato model has no mesh.");
            RequireMatchingBoneOrder(approvedAssetRenderer, renderer);

            var originalMaterials = renderer.sharedMaterials.ToArray();
            var originalLocalBounds = renderer.localBounds;
            var generated = CreateOrUpdateBreathingMesh(approvedMesh, approvedAssetRenderer, out var morphStats);
            renderer.sharedMesh = generated;
            renderer.localBounds = ExpandBounds(generated.bounds, 1.12f);
            renderer.updateWhenOffscreen = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, idleSlot);
            var clip = CreateOrUpdateIdleClip(idleSlot, renderer, rendererPath);
            var controller = CreateOrUpdateIdleController(clip);
            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleSlot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            idleSlot.name = IdleSlotName;
            EditorUtility.SetDirty(idleSlot.gameObject);

            VerifyAppliedState(
                placementRoot,
                idleSlot,
                renderer,
                originalMaterials,
                slotStates,
                clip,
                controller,
                rendererPath,
                morphStats);

            Directory.CreateDirectory(ProjectAbsolutePath(ValidationFolderPath));
            DeleteLegacyDirectWeightReviewArtifacts();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ostinato idle breathing application.");
            }

            AssetDatabase.SaveAssets();
            WriteApplyReport(
                renderer,
                approvedMesh,
                originalLocalBounds,
                rendererPath,
                clip,
                controller,
                morphStats);
            AssetDatabase.Refresh();

            Selection.activeGameObject = idleSlot.gameObject;
            Debug.Log(
                "OstinatoIdleBreathingAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + IdleSlotName +
                ", BlendShapes=" + string.Join("|", GetBreathingBlendShapeNames()) +
                ", AffectedOrganicVertices=" + morphStats.AffectedOrganicVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", ExcludedBladeVertices=" + morphStats.BladeVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MaxVertexDelta=" + morphStats.MaxVertexDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", LoopSeconds=" + LoopDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture) +
                ", RootMotion=False" +
                ", MaterialsUnchanged=True" +
                ", OtherSlotsUnchanged=True" +
                ", DirectBlendShapeReviewCapture=False");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Review Idle Animator Playback")]
        public static void ReviewOstinatoIdleAnimatorPlayback()
        {
            var scene = RequireOpenScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform ??
                throw new InvalidOperationException(PlacementRootName + " is missing.");
            var idleSlot = placementRoot.Find(IdleSlotName) ??
                throw new InvalidOperationException(IdleSlotName + " is missing.");
            var renderer = idleSlot.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException(IdleSlotName + " must contain one SkinnedMeshRenderer.");
            var animator = idleSlot.GetComponent<Animator>() ??
                throw new InvalidOperationException(IdleSlotName + " has no Animator.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath) ??
                throw new InvalidOperationException("Ostinato idle AnimatorController is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath) ??
                throw new InvalidOperationException("Ostinato idle AnimationClip is missing.");
            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Ostinato idle Animator scene configuration is not active and root-locked.");
            }

            var defaultState = controller.layers[0].stateMachine.defaultState;
            if (defaultState == null || defaultState.name != StateName || defaultState.motion != clip)
            {
                throw new InvalidOperationException("Ostinato idle controller default state does not point to the approved idle clip.");
            }

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, idleSlot);
            var binding = AnimationUtility.GetCurveBindings(clip).SingleOrDefault(candidate =>
                candidate.path == rendererPath &&
                candidate.type == typeof(SkinnedMeshRenderer) &&
                candidate.propertyName == "blendShape." + BlendShapeName);
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                throw new InvalidOperationException("Ostinato idle clip is missing its renderer BlendShape binding.");
            }

            var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                throw new InvalidOperationException("Ostinato idle BlendShape curve could not be read.");
            var blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            if (blendShapeIndex < 0)
            {
                throw new InvalidOperationException("Ostinato idle mesh is missing its breathing BlendShape.");
            }

            var maxBladeDelta = CalculateMaxBladeBlendShapeDelta(renderer.sharedMesh, blendShapeIndex);
            if (maxBladeDelta > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Ostinato idle BlendShape moves HookBlade vertices. MaxDelta=" +
                    maxBladeDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            if (CountOtherConfiguredAnimators(placementRoot, idleSlot) != 0)
            {
                throw new InvalidOperationException("An Ostinato slot outside slot 02 has a configured Animator.");
            }

            Directory.CreateDirectory(ProjectAbsolutePath(ValidationFolderPath));
            var normalizedCheckpoints = new[] { 0f, 0.25f, 0.50f, 0.75f, 1f };
            var actualNormalizedTimes = new float[normalizedCheckpoints.Length];
            var expectedWeights = new float[normalizedCheckpoints.Length];
            var animatorWeights = new float[normalizedCheckpoints.Length];
            var stateNamesMatched = new bool[normalizedCheckpoints.Length];
            var fullFrames = new Texture2D[normalizedCheckpoints.Length];
            var closeFrames = new Texture2D[normalizedCheckpoints.Length];
            var layerStates = idleSlot.GetComponentsInChildren<Transform>(true)
                .Select(target => new LayerState(target.gameObject, target.gameObject.layer))
                .ToArray();
            var animatorWasEnabled = animator.enabled;
            var fullStateHash = Animator.StringToHash("Base Layer." + StateName);

            foreach (var layerState in layerStates)
            {
                layerState.GameObject.layer = ReviewLayer;
            }

            var cameraObject = new GameObject("Ostinato_IdleAnimatorPlayback_ReviewCamera", typeof(Camera));
            var keyObject = new GameObject("Ostinato_IdleAnimatorPlayback_KeyLight", typeof(Light));
            var fillObject = new GameObject("Ostinato_IdleAnimatorPlayback_FillLight", typeof(Light));
            var camera = cameraObject.GetComponent<Camera>();
            var key = keyObject.GetComponent<Light>();
            var fill = fillObject.GetComponent<Light>();
            try
            {
                ConfigureReviewCameraAndLights(camera, keyObject.transform, key, fillObject.transform, fill);

                animator.Rebind();
                animator.Play(fullStateHash, 0, 0f);
                animator.Update(0f);
                var framingBounds = renderer.bounds;
                var previousNormalizedTime = 0f;
                for (var index = 0; index < normalizedCheckpoints.Length; index++)
                {
                    var requestedNormalizedTime = normalizedCheckpoints[index];
                    if (index > 0)
                    {
                        animator.Update((requestedNormalizedTime - previousNormalizedTime) * clip.length);
                    }

                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    actualNormalizedTimes[index] = stateInfo.normalizedTime;
                    expectedWeights[index] = curve.Evaluate(requestedNormalizedTime * clip.length);
                    animatorWeights[index] = renderer.GetBlendShapeWeight(blendShapeIndex);
                    stateNamesMatched[index] = stateInfo.IsName(StateName) || stateInfo.IsName("Base Layer." + StateName);
                    if (!stateNamesMatched[index])
                    {
                        throw new InvalidOperationException(
                            "Animator left the Ostinato idle state at checkpoint " +
                            requestedNormalizedTime.ToString("0.00", CultureInfo.InvariantCulture) + ".");
                    }

                    if (Mathf.Abs(animatorWeights[index] - expectedWeights[index]) > 0.75f)
                    {
                        throw new InvalidOperationException(
                            "Animator-driven BlendShape weight differs from the clip curve at checkpoint " +
                            requestedNormalizedTime.ToString("0.00", CultureInfo.InvariantCulture) +
                            ". Expected=" + expectedWeights[index].ToString("0.###", CultureInfo.InvariantCulture) +
                            ", Actual=" + animatorWeights[index].ToString("0.###", CultureInfo.InvariantCulture));
                    }

                    if (index > 0 && actualNormalizedTimes[index] <= actualNormalizedTimes[index - 1] + 0.01f)
                    {
                        throw new InvalidOperationException("Ostinato idle Animator normalized time did not advance.");
                    }

                    PositionReviewCamera(camera.transform, framingBounds, 1f);
                    fullFrames[index] = RenderFrame(camera);
                    PositionReviewCamera(camera.transform, framingBounds, 0.72f);
                    closeFrames[index] = RenderFrame(camera);
                    previousNormalizedTime = requestedNormalizedTime;
                }

                WriteAnimatorPlaybackContactSheet(fullFrames, closeFrames);
                WriteAnimatorPlaybackReport(
                    rendererPath,
                    clip,
                    controller,
                    normalizedCheckpoints,
                    actualNormalizedTimes,
                    expectedWeights,
                    animatorWeights,
                    stateNamesMatched,
                    maxBladeDelta);
            }
            finally
            {
                animator.Rebind();
                animator.Play(fullStateHash, 0, 0f);
                animator.Update(0f);
                animator.enabled = animatorWasEnabled;
                foreach (var layerState in layerStates)
                {
                    layerState.GameObject.layer = layerState.Layer;
                }

                DestroyFrames(fullFrames);
                DestroyFrames(closeFrames);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }

            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException("Animator playback review dirtied CargoRunMvp unexpectedly.");
            }

            AssetDatabase.Refresh();
            Selection.activeGameObject = idleSlot.gameObject;
            Debug.Log(
                "OstinatoIdleAnimatorPlaybackReviewed" +
                ", Target=" + PlacementRootName + "/" + IdleSlotName +
                ", State=" + StateName +
                ", Checkpoints=0|25|50|75|100" +
                ", AnimatorWeights=" + string.Join("|", animatorWeights.Select(value =>
                    value.ToString("0.###", CultureInfo.InvariantCulture))) +
                ", MaxHookBladeDelta=" + maxBladeDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                ", DirectBlendShapeSetterUsed=False" +
                ", SceneChanged=False");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Runtime Idle Breathing Playback")]
        public static void CaptureOstinatoIdleBreathingRuntimePlayback()
        {
            OstinatoIdleBreathingRuntimeCapture.Begin();
        }

        private static Mesh CreateOrUpdateBreathingMesh(
            Mesh sourceMesh,
            SkinnedMeshRenderer sourceRenderer,
            out MorphStats stats)
        {
            Directory.CreateDirectory(ProjectAbsolutePath(AnimationFolderPath));
            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "Ostinato_02_Idle_Breathing_MorphMesh";
            generated.ClearBlendShapes();

            var vertices = generated.vertices;
            var boneWeights = generated.boneWeights;
            if (vertices.Length == 0 || boneWeights.Length != vertices.Length)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "Ostinato breathing mesh requires one skin weight record per vertex.");
            }

            var boneNames = sourceRenderer.bones.Select(bone => bone != null ? bone.name : string.Empty).ToArray();
            var coreDeltaVertices = new Vector3[vertices.Length];
            var upperDeltaVertices = new Vector3[vertices.Length];
            var lowerDeltaVertices = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var bounds = generated.bounds;
            var center = bounds.center;
            var height = Mathf.Max(bounds.size.y, 0.0001f);
            var halfWidth = Mathf.Max(bounds.extents.x, 0.0001f);
            if (generated.subMeshCount <= HookBladeSubMeshIndex)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException("Approved Ostinato mesh is missing its HookBlade submesh.");
            }

            var bladeVertices = new bool[vertices.Length];
            foreach (var vertexIndex in generated.GetIndices(HookBladeSubMeshIndex))
            {
                if (vertexIndex >= 0 && vertexIndex < bladeVertices.Length)
                {
                    bladeVertices[vertexIndex] = true;
                }
            }

            var bindPoses = generated.bindposes;
            if (bindPoses.Length < boneNames.Length)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException("Ostinato breathing mesh has fewer bind poses than renderer bones.");
            }

            var affectedOrganicVertexCount = 0;
            var organicVertexCount = 0;
            var bladeVertexCount = 0;
            var torsoAffectedVertexCount = 0;
            var headAffectedVertexCount = 0;
            var armAffectedVertexCount = 0;
            var legAffectedVertexCount = 0;
            var maxTorsoVertexDelta = 0f;
            var maxHeadVertexDelta = 0f;
            var maxArmVertexDelta = 0f;
            var maxLegVertexDelta = 0f;
            var maxVertexDelta = 0f;
            var maxBladeVertexDelta = 0f;
            var maxGroundVerticalDelta = 0f;
            var neutralOrganicMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var neutralOrganicMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            var inhaleOrganicMin = neutralOrganicMin;
            var inhaleOrganicMax = neutralOrganicMax;

            for (var index = 0; index < vertices.Length; index++)
            {
                if (bladeVertices[index])
                {
                    bladeVertexCount++;
                    maxBladeVertexDelta = Mathf.Max(
                        maxBladeVertexDelta,
                        coreDeltaVertices[index].magnitude,
                        upperDeltaVertices[index].magnitude,
                        lowerDeltaVertices[index].magnitude);
                    continue;
                }

                organicVertexCount++;
                var vertex = vertices[index];
                var normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertex.y);
                var influence = CalculateBreathingRegionInfluence(
                    boneWeights[index],
                    boneNames,
                    bindPoses,
                    normalizedY);
                var localRadial = vertex - influence.BoneCenter;
                var groundedOffset = vertex - new Vector3(center.x, bounds.min.y, center.z);
                var groundRelease = normalizedY <= 0.10f
                    ? 0f
                    : Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.10f, 0.28f, normalizedY));
                var chestBias = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.34f, 0.72f, normalizedY));

                var coreExpansion = influence.CoreWeight * Mathf.Lerp(0.13f, 0.18f, chestBias);
                var coreDelta = new Vector3(
                    localRadial.x * coreExpansion,
                    (localRadial.y * 0.055f + height * 0.020f) * influence.CoreWeight * groundRelease,
                    localRadial.z * coreExpansion * 1.18f);

                var upperExpansion = influence.HeadWeight * 0.20f + influence.ArmWeight * 0.24f;
                var upperDelta = new Vector3(
                    localRadial.x * upperExpansion,
                    (localRadial.y * (influence.HeadWeight * 0.16f + influence.ArmWeight * 0.12f) +
                     height * 0.014f * influence.HeadWeight) * groundRelease,
                    localRadial.z * upperExpansion * 1.08f);

                var lowerExpansion = influence.LowerWeight * 0.22f;
                var lowerDelta = new Vector3(
                    localRadial.x * lowerExpansion +
                    groundedOffset.x * 0.045f * influence.LowerWeight * groundRelease,
                    localRadial.y * 0.085f * influence.LowerWeight * groundRelease,
                    localRadial.z * lowerExpansion * 0.92f +
                    groundedOffset.z * 0.022f * influence.LowerWeight * groundRelease);

                coreDeltaVertices[index] = coreDelta;
                upperDeltaVertices[index] = upperDelta;
                lowerDeltaVertices[index] = lowerDelta;
                var combinedDelta = coreDelta + upperDelta + lowerDelta;
                neutralOrganicMin = Vector3.Min(neutralOrganicMin, vertex);
                neutralOrganicMax = Vector3.Max(neutralOrganicMax, vertex);
                inhaleOrganicMin = Vector3.Min(inhaleOrganicMin, vertex + combinedDelta);
                inhaleOrganicMax = Vector3.Max(inhaleOrganicMax, vertex + combinedDelta);
                if (normalizedY <= 0.08f)
                {
                    maxGroundVerticalDelta = Mathf.Max(maxGroundVerticalDelta, Mathf.Abs(combinedDelta.y));
                }

                var magnitude = combinedDelta.magnitude;
                if (magnitude > 0.000001f)
                {
                    affectedOrganicVertexCount++;
                    maxVertexDelta = Mathf.Max(maxVertexDelta, magnitude);
                    switch (ClassifyDominantBodyRegion(boneWeights[index], boneNames))
                    {
                        case BodyRegion.Torso:
                            torsoAffectedVertexCount++;
                            maxTorsoVertexDelta = Mathf.Max(maxTorsoVertexDelta, magnitude);
                            break;
                        case BodyRegion.Head:
                            headAffectedVertexCount++;
                            maxHeadVertexDelta = Mathf.Max(maxHeadVertexDelta, magnitude);
                            break;
                        case BodyRegion.Arm:
                            armAffectedVertexCount++;
                            maxArmVertexDelta = Mathf.Max(maxArmVertexDelta, magnitude);
                            break;
                        case BodyRegion.Leg:
                            legAffectedVertexCount++;
                            maxLegVertexDelta = Mathf.Max(maxLegVertexDelta, magnitude);
                            break;
                    }
                }
            }

            var neutralOrganicSize = neutralOrganicMax - neutralOrganicMin;
            var inhaleOrganicSize = inhaleOrganicMax - inhaleOrganicMin;
            var organicSilhouetteSizeDelta = inhaleOrganicSize - neutralOrganicSize;

            if (bladeVertexCount == 0 ||
                affectedOrganicVertexCount < organicVertexCount * 0.80f ||
                torsoAffectedVertexCount == 0 ||
                headAffectedVertexCount == 0 ||
                armAffectedVertexCount == 0 ||
                legAffectedVertexCount == 0 ||
                maxTorsoVertexDelta < 0.050f ||
                maxHeadVertexDelta < 0.060f ||
                maxArmVertexDelta < 0.060f ||
                maxLegVertexDelta < 0.050f ||
                organicSilhouetteSizeDelta.x < 0.040f ||
                organicSilhouetteSizeDelta.y < 0.050f ||
                maxGroundVerticalDelta > 0.0001f ||
                maxVertexDelta < 0.001f ||
                maxBladeVertexDelta > 0.000001f)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "Generated Ostinato whole-body morph did not cover every organic region while excluding blades. " +
                    "AffectedOrganic=" + affectedOrganicVertexCount.ToString(CultureInfo.InvariantCulture) +
                    "/" + organicVertexCount.ToString(CultureInfo.InvariantCulture) +
                    ", Blade=" + bladeVertexCount.ToString(CultureInfo.InvariantCulture) +
                    ", Regions=" + torsoAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    "|" + headAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    "|" + armAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    "|" + legAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    ", RegionMaxDelta=" + maxTorsoVertexDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    "|" + maxHeadVertexDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    "|" + maxArmVertexDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    "|" + maxLegVertexDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", SilhouetteSizeDelta=" + FormatVector(organicSilhouetteSizeDelta) +
                    ", MaxGroundVerticalDelta=" + maxGroundVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", MaxDelta=" + maxVertexDelta.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", MaxBladeDelta=" + maxBladeVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            }

            generated.AddBlendShapeFrame(BlendShapeName, 100f, coreDeltaVertices, deltaNormals, deltaTangents);
            generated.AddBlendShapeFrame(UpperBlendShapeName, 100f, upperDeltaVertices, deltaNormals, deltaTangents);
            generated.AddBlendShapeFrame(LowerBlendShapeName, 100f, lowerDeltaVertices, deltaNormals, deltaTangents);
            generated.RecalculateBounds();

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(IdleMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, IdleMeshPath);
                existing = AssetDatabase.LoadAssetAtPath<Mesh>(IdleMeshPath);
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            stats = new MorphStats(
                affectedOrganicVertexCount,
                organicVertexCount,
                bladeVertexCount,
                torsoAffectedVertexCount,
                headAffectedVertexCount,
                armAffectedVertexCount,
                legAffectedVertexCount,
                maxTorsoVertexDelta,
                maxHeadVertexDelta,
                maxArmVertexDelta,
                maxLegVertexDelta,
                maxVertexDelta,
                maxBladeVertexDelta,
                neutralOrganicSize,
                inhaleOrganicSize,
                organicSilhouetteSizeDelta,
                maxGroundVerticalDelta,
                sourceMesh.blendShapeCount,
                existing != null ? existing.blendShapeCount : 0);
            return existing ?? throw new InvalidOperationException("Failed to create Ostinato breathing mesh asset.");
        }

        private static BreathingRegionInfluence CalculateBreathingRegionInfluence(
            BoneWeight weights,
            string[] boneNames,
            Matrix4x4[] bindPoses,
            float normalizedY)
        {
            var weightedCenter = Vector3.zero;
            var totalWeight = 0f;
            var coreWeight = 0f;
            var headWeight = 0f;
            var armWeight = 0f;
            var lowerWeight = 0f;
            AccumulateBreathingRegion(
                weights.boneIndex0,
                weights.weight0,
                boneNames,
                bindPoses,
                ref weightedCenter,
                ref totalWeight,
                ref coreWeight,
                ref headWeight,
                ref armWeight,
                ref lowerWeight);
            AccumulateBreathingRegion(
                weights.boneIndex1,
                weights.weight1,
                boneNames,
                bindPoses,
                ref weightedCenter,
                ref totalWeight,
                ref coreWeight,
                ref headWeight,
                ref armWeight,
                ref lowerWeight);
            AccumulateBreathingRegion(
                weights.boneIndex2,
                weights.weight2,
                boneNames,
                bindPoses,
                ref weightedCenter,
                ref totalWeight,
                ref coreWeight,
                ref headWeight,
                ref armWeight,
                ref lowerWeight);
            AccumulateBreathingRegion(
                weights.boneIndex3,
                weights.weight3,
                boneNames,
                bindPoses,
                ref weightedCenter,
                ref totalWeight,
                ref coreWeight,
                ref headWeight,
                ref armWeight,
                ref lowerWeight);

            if (totalWeight <= 0.0001f)
            {
                return normalizedY >= 0.70f
                    ? new BreathingRegionInfluence(Vector3.zero, 0f, 1f, 0f, 0f)
                    : normalizedY <= 0.45f
                        ? new BreathingRegionInfluence(Vector3.zero, 0f, 0f, 0f, 1f)
                        : new BreathingRegionInfluence(Vector3.zero, 1f, 0f, 0f, 0f);
            }

            coreWeight /= totalWeight;
            headWeight /= totalWeight;
            armWeight /= totalWeight;
            lowerWeight /= totalWeight;
            var unassignedWeight = Mathf.Clamp01(1f - coreWeight - headWeight - armWeight - lowerWeight);
            if (normalizedY >= 0.70f)
            {
                headWeight += unassignedWeight;
            }
            else if (normalizedY <= 0.45f)
            {
                lowerWeight += unassignedWeight;
            }
            else
            {
                coreWeight += unassignedWeight;
            }

            return new BreathingRegionInfluence(
                weightedCenter / totalWeight,
                coreWeight,
                headWeight,
                armWeight,
                lowerWeight);
        }

        private static void AccumulateBreathingRegion(
            int boneIndex,
            float weight,
            string[] boneNames,
            Matrix4x4[] bindPoses,
            ref Vector3 weightedCenter,
            ref float totalWeight,
            ref float coreWeight,
            ref float headWeight,
            ref float armWeight,
            ref float lowerWeight)
        {
            if (weight <= 0f || boneIndex < 0 || boneIndex >= boneNames.Length)
            {
                return;
            }

            var boneName = boneNames[boneIndex];
            weightedCenter += bindPoses[boneIndex].inverse.MultiplyPoint3x4(Vector3.zero) * weight;
            totalWeight += weight;
            if (boneName == "Hips")
            {
                coreWeight += weight * 0.40f;
                lowerWeight += weight * 0.60f;
            }
            else if (boneName.StartsWith("Spine", StringComparison.Ordinal))
            {
                coreWeight += weight;
            }
            else if (boneName == "neck")
            {
                coreWeight += weight * 0.20f;
                headWeight += weight * 0.80f;
            }
            else if (boneName == "Head" || boneName == "head_end" || boneName == "headfront")
            {
                headWeight += weight;
            }
            else if (boneName.StartsWith("LeftShoulder", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightShoulder", StringComparison.Ordinal) ||
                     boneName.StartsWith("LeftArm", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightArm", StringComparison.Ordinal) ||
                     boneName.StartsWith("LeftForeArm", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightForeArm", StringComparison.Ordinal) ||
                     boneName.StartsWith("LeftHand", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightHand", StringComparison.Ordinal))
            {
                armWeight += weight;
            }
            else if (boneName.StartsWith("LeftUpLeg", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightUpLeg", StringComparison.Ordinal) ||
                     boneName.StartsWith("LeftLeg", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightLeg", StringComparison.Ordinal) ||
                     boneName.StartsWith("LeftFoot", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightFoot", StringComparison.Ordinal) ||
                     boneName.StartsWith("LeftToe", StringComparison.Ordinal) ||
                     boneName.StartsWith("RightToe", StringComparison.Ordinal))
            {
                lowerWeight += weight;
            }
        }

        private static BodyInfluence CalculateBodyInfluence(
            BoneWeight weights,
            string[] boneNames,
            Matrix4x4[] bindPoses)
        {
            var weightedCenter = Vector3.zero;
            var totalWeight = 0f;
            var torsoWeight = 0f;
            var radialScale = 0f;
            var verticalScale = 0f;
            var silhouetteHorizontalScale = 0f;
            var silhouetteVerticalScale = 0f;
            AccumulateBodyInfluence(weights.boneIndex0, weights.weight0, boneNames, bindPoses, ref weightedCenter, ref totalWeight, ref torsoWeight, ref radialScale, ref verticalScale, ref silhouetteHorizontalScale, ref silhouetteVerticalScale);
            AccumulateBodyInfluence(weights.boneIndex1, weights.weight1, boneNames, bindPoses, ref weightedCenter, ref totalWeight, ref torsoWeight, ref radialScale, ref verticalScale, ref silhouetteHorizontalScale, ref silhouetteVerticalScale);
            AccumulateBodyInfluence(weights.boneIndex2, weights.weight2, boneNames, bindPoses, ref weightedCenter, ref totalWeight, ref torsoWeight, ref radialScale, ref verticalScale, ref silhouetteHorizontalScale, ref silhouetteVerticalScale);
            AccumulateBodyInfluence(weights.boneIndex3, weights.weight3, boneNames, bindPoses, ref weightedCenter, ref totalWeight, ref torsoWeight, ref radialScale, ref verticalScale, ref silhouetteHorizontalScale, ref silhouetteVerticalScale);

            if (totalWeight <= 0.0001f)
            {
                return new BodyInfluence(Vector3.zero, 0f, 0.060f, 0f, 0.020f, 0f);
            }

            return new BodyInfluence(
                weightedCenter / totalWeight,
                torsoWeight / totalWeight,
                Mathf.Max(0.003f, radialScale / totalWeight),
                verticalScale / totalWeight,
                silhouetteHorizontalScale / totalWeight,
                silhouetteVerticalScale / totalWeight);
        }

        private static void AccumulateBodyInfluence(
            int boneIndex,
            float weight,
            string[] boneNames,
            Matrix4x4[] bindPoses,
            ref Vector3 weightedCenter,
            ref float totalWeight,
            ref float torsoWeight,
            ref float radialScale,
            ref float verticalScale,
            ref float silhouetteHorizontalScale,
            ref float silhouetteVerticalScale)
        {
            if (weight <= 0f || boneIndex < 0 || boneIndex >= boneNames.Length)
            {
                return;
            }

            var boneName = boneNames[boneIndex];
            weightedCenter += bindPoses[boneIndex].inverse.MultiplyPoint3x4(Vector3.zero) * weight;
            totalWeight += weight;
            torsoWeight += GetTorsoWeight(boneName) * weight;
            radialScale += GetRadialScale(boneName) * weight;
            verticalScale += GetVerticalScale(boneName) * weight;
            silhouetteHorizontalScale += GetSilhouetteHorizontalScale(boneName) * weight;
            silhouetteVerticalScale += GetSilhouetteVerticalScale(boneName) * weight;
        }

        private static float GetTorsoWeight(string boneName)
        {
            return boneName switch
            {
                "Spine" => 1f,
                "Spine01" => 1f,
                "Spine02" => 1f,
                "Hips" => 0.35f,
                "neck" => 0.15f,
                _ => 0f,
            };
        }

        private static float GetRadialScale(string boneName)
        {
            // Full-figure review requires these secondary regions to remain visibly below the torso,
            // but above the former sub-percent values that read as static at the approved camera distance.
            return boneName switch
            {
                "Spine" or "Spine01" or "Spine02" => 0.120f,
                "Hips" => 0.100f,
                "neck" => 0.120f,
                "Head" or "head_end" or "headfront" => 0.100f,
                "LeftShoulder" or "RightShoulder" => 0.100f,
                "LeftArm" or "RightArm" => 0.090f,
                "LeftForeArm" or "RightForeArm" => 0.085f,
                "LeftHand" or "RightHand" => 0.075f,
                "LeftUpLeg" or "RightUpLeg" => 0.090f,
                "LeftLeg" or "RightLeg" => 0.080f,
                "LeftFoot" or "RightFoot" => 0.070f,
                "LeftToeBase" or "RightToeBase" => 0.060f,
                _ => 0.060f,
            };
        }

        private static float GetVerticalScale(string boneName)
        {
            return boneName switch
            {
                "neck" => 0.060f,
                "Head" or "head_end" or "headfront" => 0.075f,
                "LeftShoulder" or "RightShoulder" => 0.045f,
                "LeftArm" or "RightArm" => 0.035f,
                "LeftForeArm" or "RightForeArm" => 0.030f,
                "LeftHand" or "RightHand" => 0.020f,
                _ => 0f,
            };
        }

        private static float GetSilhouetteHorizontalScale(string boneName)
        {
            return boneName switch
            {
                "Spine" or "Spine01" or "Spine02" => 0.050f,
                "Hips" => 0.055f,
                "neck" => 0.035f,
                "Head" or "head_end" or "headfront" => 0.035f,
                "LeftShoulder" or "RightShoulder" => 0.025f,
                "LeftArm" or "RightArm" => 0.018f,
                "LeftForeArm" or "RightForeArm" => 0.010f,
                "LeftHand" or "RightHand" => 0f,
                "LeftUpLeg" or "RightUpLeg" => 0.055f,
                "LeftLeg" or "RightLeg" => 0.045f,
                "LeftFoot" or "RightFoot" => 0.030f,
                "LeftToeBase" or "RightToeBase" => 0.020f,
                _ => 0.020f,
            };
        }

        private static float GetSilhouetteVerticalScale(string boneName)
        {
            return boneName switch
            {
                "Spine" or "Spine01" or "Spine02" => 0.035f,
                "Hips" => 0.020f,
                "neck" => 0.045f,
                "Head" or "head_end" or "headfront" => 0.050f,
                "LeftShoulder" or "RightShoulder" => 0.020f,
                "LeftArm" or "RightArm" => 0.012f,
                "LeftForeArm" or "RightForeArm" => 0.006f,
                "LeftHand" or "RightHand" => 0f,
                "LeftUpLeg" or "RightUpLeg" => 0.012f,
                "LeftLeg" or "RightLeg" => 0.006f,
                _ => 0f,
            };
        }

        private static BodyRegion ClassifyDominantBodyRegion(BoneWeight weights, string[] boneNames)
        {
            var boneIndex = weights.boneIndex0;
            var weight = weights.weight0;
            if (weights.weight1 > weight)
            {
                boneIndex = weights.boneIndex1;
                weight = weights.weight1;
            }

            if (weights.weight2 > weight)
            {
                boneIndex = weights.boneIndex2;
                weight = weights.weight2;
            }

            if (weights.weight3 > weight)
            {
                boneIndex = weights.boneIndex3;
            }

            if (boneIndex < 0 || boneIndex >= boneNames.Length)
            {
                return BodyRegion.Other;
            }

            var boneName = boneNames[boneIndex];
            if (boneName == "Head" || boneName == "head_end" || boneName == "headfront" || boneName == "neck")
            {
                return BodyRegion.Head;
            }

            if (boneName.StartsWith("LeftArm", StringComparison.Ordinal) ||
                boneName.StartsWith("RightArm", StringComparison.Ordinal) ||
                boneName.StartsWith("LeftForeArm", StringComparison.Ordinal) ||
                boneName.StartsWith("RightForeArm", StringComparison.Ordinal) ||
                boneName.StartsWith("LeftHand", StringComparison.Ordinal) ||
                boneName.StartsWith("RightHand", StringComparison.Ordinal) ||
                boneName.StartsWith("LeftShoulder", StringComparison.Ordinal) ||
                boneName.StartsWith("RightShoulder", StringComparison.Ordinal))
            {
                return BodyRegion.Arm;
            }

            if (boneName.StartsWith("LeftUpLeg", StringComparison.Ordinal) ||
                boneName.StartsWith("RightUpLeg", StringComparison.Ordinal) ||
                boneName.StartsWith("LeftLeg", StringComparison.Ordinal) ||
                boneName.StartsWith("RightLeg", StringComparison.Ordinal) ||
                boneName.StartsWith("LeftFoot", StringComparison.Ordinal) ||
                boneName.StartsWith("RightFoot", StringComparison.Ordinal) ||
                boneName.StartsWith("LeftToeBase", StringComparison.Ordinal) ||
                boneName.StartsWith("RightToeBase", StringComparison.Ordinal))
            {
                return BodyRegion.Leg;
            }

            return boneName == "Hips" || boneName.StartsWith("Spine", StringComparison.Ordinal)
                ? BodyRegion.Torso
                : BodyRegion.Other;
        }

        private static AnimationClip CreateOrUpdateIdleClip(
            Transform idleSlot,
            SkinnedMeshRenderer renderer,
            string rendererPath)
        {
            Directory.CreateDirectory(ProjectAbsolutePath(AnimationFolderPath));
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, IdleClipPath);
            }

            clip.ClearCurves();
            clip.name = StateName;
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + BlendShapeName),
                CreateCoreBreathingCurve());
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + UpperBlendShapeName),
                CreateUpperBreathingCurve());
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + LowerBlendShapeName),
                CreateLowerBreathingCurve());
            AddConnectedBreathingPoseCurves(clip, idleSlot, renderer);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AddConnectedBreathingPoseCurves(
            AnimationClip clip,
            Transform idleSlot,
            SkinnedMeshRenderer renderer)
        {
            var modelHeight = Mathf.Max(renderer.sharedMesh.bounds.size.y, 0.0001f);
            var upperCurve = CreateUpperBreathingCurve();
            var coreCurve = CreateCoreBreathingCurve();
            var lowerCurve = CreateLowerBreathingCurve();
            AddBonePositionOffsetCurve(
                clip,
                idleSlot,
                RequireBone(renderer, "Spine02"),
                "m_LocalPosition.y",
                modelHeight * 0.018f,
                coreCurve);
            AddBonePositionOffsetCurve(
                clip,
                idleSlot,
                RequireBone(renderer, "neck"),
                "m_LocalPosition.y",
                modelHeight * 0.014f,
                upperCurve);
            AddBonePositionOffsetCurve(
                clip,
                idleSlot,
                RequireBone(renderer, "Head"),
                "m_LocalPosition.y",
                modelHeight * 0.010f,
                upperCurve);

            foreach (var boneName in new[] { "LeftShoulder", "RightShoulder" })
            {
                var shoulder = RequireBone(renderer, boneName);
                var outwardSign = Mathf.Sign(shoulder.localPosition.x);
                if (Mathf.Approximately(outwardSign, 0f))
                {
                    outwardSign = boneName.StartsWith("Left", StringComparison.Ordinal) ? -1f : 1f;
                }

                AddBonePositionOffsetCurve(
                    clip,
                    idleSlot,
                    shoulder,
                    "m_LocalPosition.x",
                    outwardSign * modelHeight * 0.028f,
                    upperCurve);
                AddBonePositionOffsetCurve(
                    clip,
                    idleSlot,
                    shoulder,
                    "m_LocalPosition.y",
                    modelHeight * 0.014f,
                    upperCurve);
            }

            foreach (var boneName in new[] { "LeftUpLeg", "RightUpLeg" })
            {
                var upperLeg = RequireBone(renderer, boneName);
                var outwardSign = Mathf.Sign(upperLeg.localPosition.x);
                if (Mathf.Approximately(outwardSign, 0f))
                {
                    outwardSign = boneName.StartsWith("Left", StringComparison.Ordinal) ? -1f : 1f;
                }

                AddBonePositionOffsetCurve(
                    clip,
                    idleSlot,
                    upperLeg,
                    "m_LocalPosition.x",
                    outwardSign * modelHeight * 0.022f,
                    lowerCurve);
            }
        }

        private static void AddBonePositionOffsetCurve(
            AnimationClip clip,
            Transform idleSlot,
            Transform bone,
            string propertyName,
            float peakOffset,
            AnimationCurve breathingCurve)
        {
            var baseValue = propertyName switch
            {
                "m_LocalPosition.x" => bone.localPosition.x,
                "m_LocalPosition.y" => bone.localPosition.y,
                "m_LocalPosition.z" => bone.localPosition.z,
                _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null),
            };
            var keyframes = breathingCurve.keys
                .Select(key => new Keyframe(key.time, baseValue + peakOffset * (key.value / 100f)))
                .ToArray();
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    AnimationUtility.CalculateTransformPath(bone, idleSlot),
                    typeof(Transform),
                    propertyName),
                CreateSmoothCurve(keyframes));
        }

        private static Transform RequireBone(SkinnedMeshRenderer renderer, string boneName)
        {
            return renderer.bones.SingleOrDefault(bone => bone != null && bone.name == boneName) ??
                   throw new InvalidOperationException("Ostinato rig is missing breathing pose bone: " + boneName);
        }

        private static AnimationCurve CreateCoreBreathingCurve()
        {
            return CreateSmoothCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(0.38f, 18f),
                new Keyframe(InhalePeakTimeSeconds, 100f),
                new Keyframe(1.38f, 94f),
                new Keyframe(2.52f, 22f),
                new Keyframe(LoopDurationSeconds, 0f));
        }

        private static AnimationCurve CreateUpperBreathingCurve()
        {
            return CreateSmoothCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(0.28f, 24f),
                new Keyframe(UpperPeakTimeSeconds, 100f),
                new Keyframe(1.24f, 92f),
                new Keyframe(2.36f, 18f),
                new Keyframe(LoopDurationSeconds, 0f));
        }

        private static AnimationCurve CreateLowerBreathingCurve()
        {
            return CreateSmoothCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(0.48f, 12f),
                new Keyframe(LowerPeakTimeSeconds, 100f),
                new Keyframe(1.62f, 90f),
                new Keyframe(2.70f, 16f),
                new Keyframe(LoopDurationSeconds, 0f));
        }

        private static AnimationCurve CreateSmoothCurve(params Keyframe[] keyframes)
        {
            var curve = new AnimationCurve(keyframes);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static AnimatorController CreateOrUpdateIdleController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(IdleControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == StateName);
            if (state == null)
            {
                state = stateMachine.AddState(StateName);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void VerifyAppliedState(
            Transform placementRoot,
            Transform idleSlot,
            SkinnedMeshRenderer renderer,
            Material[] originalMaterials,
            TransformState[] slotStates,
            AnimationClip clip,
            AnimatorController controller,
            string rendererPath,
            MorphStats morphStats)
        {
            if (!renderer.sharedMaterials.SequenceEqual(originalMaterials))
            {
                throw new InvalidOperationException("Ostinato idle application changed approved materials.");
            }

            for (var index = 0; index < slotStates.Length; index++)
            {
                if (!slotStates[index].Matches(placementRoot.GetChild(index)))
                {
                    throw new InvalidOperationException(
                        "Ostinato slot transform changed while applying idle breathing: " + placementRoot.GetChild(index).name);
                }
            }

            var animator = idleSlot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Ostinato idle Animator is not configured for root-locked playback.");
            }

            if (GetBreathingBlendShapeNames().Any(name => renderer.sharedMesh.GetBlendShapeIndex(name) < 0) ||
                morphStats.GeneratedBlendShapeCount < GetBreathingBlendShapeNames().Length ||
                morphStats.MaxBladeVertexDelta > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Ostinato idle renderer is missing a regional breathing BlendShape or deforms blade vertices.");
            }

            foreach (var blendShapeName in GetBreathingBlendShapeNames())
            {
                var binding = AnimationUtility.GetCurveBindings(clip).SingleOrDefault(candidate =>
                    candidate.path == rendererPath &&
                    candidate.type == typeof(SkinnedMeshRenderer) &&
                    candidate.propertyName == "blendShape." + blendShapeName);
                if (string.IsNullOrEmpty(binding.propertyName) || AnimationUtility.GetEditorCurve(clip, binding) == null)
                {
                    throw new InvalidOperationException(
                        "Ostinato idle clip is missing regional BlendShape curve binding: " + blendShapeName);
                }
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || Mathf.Abs(clip.length - LoopDurationSeconds) > 0.001f)
            {
                throw new InvalidOperationException("Ostinato idle breathing curve or loop setting is invalid.");
            }

            if (CountOtherConfiguredAnimators(placementRoot, idleSlot) != 0)
            {
                throw new InvalidOperationException("An Ostinato slot outside slot 02 received an animation controller.");
            }
        }

        private static CaptureInfo CaptureNeutralAndInhaleReview(
            Transform idleSlot,
            SkinnedMeshRenderer renderer)
        {
            var blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            var originalWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
            var animator = idleSlot.GetComponent<Animator>();
            var animatorWasEnabled = animator != null && animator.enabled;
            var layerStates = idleSlot.GetComponentsInChildren<Transform>(true)
                .Select(target => new LayerState(target.gameObject, target.gameObject.layer))
                .ToArray();
            foreach (var layerState in layerStates)
            {
                layerState.GameObject.layer = ReviewLayer;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            var cameraObject = new GameObject("Ostinato_IdleBreathing_ReviewCamera", typeof(Camera));
            var keyObject = new GameObject("Ostinato_IdleBreathing_KeyLight", typeof(Light));
            var fillObject = new GameObject("Ostinato_IdleBreathing_FillLight", typeof(Light));
            var camera = cameraObject.GetComponent<Camera>();
            var key = keyObject.GetComponent<Light>();
            var fill = fillObject.GetComponent<Light>();
            var progressionWeights = new[] { 0f, 50f, 100f };
            var fullFrames = new Texture2D[progressionWeights.Length];
            var closeFrames = new Texture2D[progressionWeights.Length];
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
                camera.fieldOfView = 42f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100f;
                camera.cullingMask = 1 << ReviewLayer;
                camera.allowHDR = true;
                camera.allowMSAA = true;

                key.type = LightType.Directional;
                key.intensity = 1.45f;
                key.color = new Color(1.00f, 0.89f, 0.72f);
                key.cullingMask = 1 << ReviewLayer;
                keyObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
                fill.type = LightType.Directional;
                fill.intensity = 0.78f;
                fill.color = new Color(0.46f, 0.66f, 1.00f);
                fill.cullingMask = 1 << ReviewLayer;
                fillObject.transform.rotation = Quaternion.Euler(326f, 148f, 0f);

                renderer.SetBlendShapeWeight(blendShapeIndex, 0f);
                var neutralBounds = renderer.bounds;
                for (var index = 0; index < progressionWeights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(blendShapeIndex, progressionWeights[index]);
                    PositionReviewCamera(camera.transform, neutralBounds, 1f);
                    fullFrames[index] = RenderFrame(camera);
                    PositionReviewCamera(camera.transform, neutralBounds, 0.72f);
                    closeFrames[index] = RenderFrame(camera);
                }

                renderer.SetBlendShapeWeight(blendShapeIndex, 100f);
                var inhaleBounds = renderer.bounds;
                var contactSheet = new Texture2D(
                    ReviewImageSize * progressionWeights.Length,
                    ReviewImageSize * 2,
                    TextureFormat.RGBA32,
                    false);
                for (var index = 0; index < progressionWeights.Length; index++)
                {
                    contactSheet.SetPixels32(
                        index * ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        fullFrames[index].GetPixels32());
                    contactSheet.SetPixels32(
                        index * ReviewImageSize,
                        0,
                        ReviewImageSize,
                        ReviewImageSize,
                        closeFrames[index].GetPixels32());
                }

                contactSheet.Apply(false, false);
                File.WriteAllBytes(ProjectAbsolutePath(ReviewImagePath), contactSheet.EncodeToPNG());
                var legacyReviewPath = ProjectAbsolutePath(LegacyReviewImagePath);
                if (File.Exists(legacyReviewPath))
                {
                    File.Delete(legacyReviewPath);
                }

                UnityEngine.Object.DestroyImmediate(contactSheet);

                return new CaptureInfo(neutralBounds, inhaleBounds);
            }
            finally
            {
                renderer.SetBlendShapeWeight(blendShapeIndex, originalWeight);
                if (animator != null)
                {
                    animator.enabled = animatorWasEnabled;
                }

                foreach (var layerState in layerStates)
                {
                    layerState.GameObject.layer = layerState.Layer;
                }

                foreach (var frame in fullFrames)
                {
                    if (frame != null)
                    {
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                }

                foreach (var frame in closeFrames)
                {
                    if (frame != null)
                    {
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
        }

        private static float CalculateMaxBladeBlendShapeDelta(Mesh mesh, int blendShapeIndex)
        {
            if (mesh.subMeshCount <= HookBladeSubMeshIndex || mesh.GetBlendShapeFrameCount(blendShapeIndex) < 1)
            {
                throw new InvalidOperationException("Ostinato idle mesh cannot provide HookBlade BlendShape deltas.");
            }

            var deltaVertices = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            var frameIndex = mesh.GetBlendShapeFrameCount(blendShapeIndex) - 1;
            mesh.GetBlendShapeFrameVertices(
                blendShapeIndex,
                frameIndex,
                deltaVertices,
                deltaNormals,
                deltaTangents);

            var maxDelta = 0f;
            foreach (var vertexIndex in mesh.GetIndices(HookBladeSubMeshIndex))
            {
                maxDelta = Mathf.Max(maxDelta, deltaVertices[vertexIndex].magnitude);
            }

            return maxDelta;
        }

        private static void ConfigureReviewCameraAndLights(
            Camera camera,
            Transform keyTransform,
            Light key,
            Transform fillTransform,
            Light fill)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << ReviewLayer;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            key.type = LightType.Directional;
            key.intensity = 1.45f;
            key.color = new Color(1.00f, 0.89f, 0.72f);
            key.cullingMask = 1 << ReviewLayer;
            keyTransform.rotation = Quaternion.Euler(38f, -32f, 0f);
            fill.type = LightType.Directional;
            fill.intensity = 0.78f;
            fill.color = new Color(0.46f, 0.66f, 1.00f);
            fill.cullingMask = 1 << ReviewLayer;
            fillTransform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void WriteAnimatorPlaybackContactSheet(Texture2D[] fullFrames, Texture2D[] closeFrames)
        {
            var contactSheet = new Texture2D(
                ReviewImageSize * fullFrames.Length,
                ReviewImageSize * 2,
                TextureFormat.RGBA32,
                false);
            try
            {
                for (var index = 0; index < fullFrames.Length; index++)
                {
                    contactSheet.SetPixels32(
                        index * ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        fullFrames[index].GetPixels32());
                    contactSheet.SetPixels32(
                        index * ReviewImageSize,
                        0,
                        ReviewImageSize,
                        ReviewImageSize,
                        closeFrames[index].GetPixels32());
                }

                contactSheet.Apply(false, false);
                File.WriteAllBytes(ProjectAbsolutePath(AnimatorPlaybackImagePath), contactSheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contactSheet);
            }
        }

        private static void WriteAnimatorPlaybackReport(
            string rendererPath,
            AnimationClip clip,
            AnimatorController controller,
            float[] checkpoints,
            float[] actualNormalizedTimes,
            float[] expectedWeights,
            float[] animatorWeights,
            bool[] stateNamesMatched,
            float maxBladeDelta)
        {
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName);
            report.AppendLine("RendererPath=" + rendererPath);
            report.AppendLine("Controller=" + AssetDatabase.GetAssetPath(controller));
            report.AppendLine("DefaultState=" + controller.layers[0].stateMachine.defaultState.name);
            report.AppendLine("Clip=" + AssetDatabase.GetAssetPath(clip));
            report.AppendLine("BlendShape=" + BlendShapeName);
            report.AppendLine("PlaybackDriver=Scene Animator");
            report.AppendLine("PlaybackMethod=Animator.Play default state once, then Animator.Update forward through one loop");
            report.AppendLine("DirectBlendShapeSetterUsed=False");
            for (var index = 0; index < checkpoints.Length; index++)
            {
                report.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "Checkpoint{0}=RequestedNormalized:{1:0.00},ActualNormalized:{2:0.######},ExpectedWeight:{3:0.###},AnimatorWeight:{4:0.###},StateMatched:{5}",
                    index,
                    checkpoints[index],
                    actualNormalizedTimes[index],
                    expectedWeights[index],
                    animatorWeights[index],
                    stateNamesMatched[index]));
            }

            report.AppendLine("NormalizedTimeAdvanced=True");
            report.AppendLine("LoopBoundaryWeightMatched=True");
            report.AppendLine("MaxHookBladeBlendShapeDelta=" + maxBladeDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("OtherSlotsConfiguredAnimators=0");
            report.AppendLine("SceneChanged=False");
            report.AppendLine("ReviewImage=" + AnimatorPlaybackImagePath);
            report.AppendLine("ReviewImageLayout=Columns 0%,25%,50%,75%,100% Animator time; top full figure, bottom organic-body close framing");
            File.WriteAllText(
                ProjectAbsolutePath(AnimatorPlaybackReportPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void DestroyFrames(Texture2D[] frames)
        {
            foreach (var frame in frames)
            {
                if (frame != null)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }
            }
        }

        private static void PositionReviewCamera(Transform cameraTransform, Bounds bounds, float distanceMultiplier)
        {
            var target = bounds.center + Vector3.up * (bounds.extents.y * 0.02f);
            var halfFovRadians = 42f * 0.5f * Mathf.Deg2Rad;
            var distance = (Mathf.Max(bounds.extents.y, bounds.extents.x) / Mathf.Tan(halfFovRadians) +
                            bounds.extents.z + 0.35f) * distanceMultiplier;
            cameraTransform.position = target + Vector3.back * distance;
            cameraTransform.rotation = Quaternion.LookRotation(target - cameraTransform.position, Vector3.up);
        }

        private static Texture2D RenderFrame(Camera camera)
        {
            var renderTexture = RenderTexture.GetTemporary(
                ReviewImageSize,
                ReviewImageSize,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(ReviewImageSize, ReviewImageSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, ReviewImageSize, ReviewImageSize), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void WriteApplyReport(
            SkinnedMeshRenderer renderer,
            Mesh sourceMesh,
            Bounds originalLocalBounds,
            string rendererPath,
            AnimationClip clip,
            AnimatorController controller,
            MorphStats stats)
        {
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName);
            report.AppendLine("RendererPath=" + rendererPath);
            report.AppendLine("SourceMesh=" + sourceMesh.name);
            report.AppendLine("GeneratedMesh=" + IdleMeshPath);
            report.AppendLine("Clip=" + IdleClipPath);
            report.AppendLine("Controller=" + IdleControllerPath);
            report.AppendLine("ControllerAsset=" + AssetDatabase.GetAssetPath(controller));
            report.AppendLine("BlendShapes=" + string.Join("|", GetBreathingBlendShapeNames()));
            report.AppendLine("MorphMethod=Three-channel regional breathing: core volume, upper-body response, lower-body response");
            report.AppendLine("SourceBlendShapeCount=" + stats.SourceBlendShapeCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("GeneratedBlendShapeCount=" + stats.GeneratedBlendShapeCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("OrganicVertexCount=" + stats.OrganicVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("AffectedOrganicVertexCount=" + stats.AffectedOrganicVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("AffectedTorsoVertexCount=" + stats.TorsoAffectedVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("AffectedHeadVertexCount=" + stats.HeadAffectedVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("AffectedArmVertexCount=" + stats.ArmAffectedVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("AffectedLegVertexCount=" + stats.LegAffectedVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("MaxTorsoVertexDelta=" + stats.MaxTorsoVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaxHeadVertexDelta=" + stats.MaxHeadVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaxArmVertexDelta=" + stats.MaxArmVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaxLegVertexDelta=" + stats.MaxLegVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("ExcludedHookBladeVertexCount=" + stats.BladeVertexCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("MaxOrganicVertexDelta=" + stats.MaxVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaxHookBladeVertexDelta=" + stats.MaxBladeVertexDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("NeutralOrganicSize=" + FormatVector(stats.NeutralOrganicSize));
            report.AppendLine("InhaleOrganicSize=" + FormatVector(stats.InhaleOrganicSize));
            report.AppendLine("OrganicSilhouetteSizeDelta=" + FormatVector(stats.OrganicSilhouetteSizeDelta));
            report.AppendLine("MaxGroundVerticalDelta=" + stats.MaxGroundVerticalDelta.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("ClipLength=" + clip.length.ToString("0.00", CultureInfo.InvariantCulture));
            report.AppendLine("FrameRate=" + clip.frameRate.ToString("0.##", CultureInfo.InvariantCulture));
            report.AppendLine("LoopTime=" + AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("RootMotion=False");
            report.AppendLine("MaterialsUnchanged=True");
            report.AppendLine("OtherSlotsConfiguredAnimators=0");
            report.AppendLine("OriginalLocalBounds=" + FormatBounds(originalLocalBounds));
            report.AppendLine("GeneratedLocalBounds=" + FormatBounds(renderer.localBounds));
            report.AppendLine("DirectBlendShapeReviewCapture=False");
            report.AppendLine("ContinuousRuntimeReviewCommand=CaptureOstinatoIdleBreathingRuntimePlayback");
            File.WriteAllText(ProjectAbsolutePath(ApplyReportPath), report.ToString(), new UTF8Encoding(false));
        }

        private static SkinnedMeshRenderer RequireApprovedAssetRenderer()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedModelPath) ??
                throw new InvalidOperationException("Approved Ostinato model is missing: " + ApprovedModelPath);
            return asset.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Approved Ostinato model must contain one SkinnedMeshRenderer.");
        }

        private static void RequireMatchingBoneOrder(
            SkinnedMeshRenderer approvedAssetRenderer,
            SkinnedMeshRenderer sceneRenderer)
        {
            var approvedBones = approvedAssetRenderer.bones.Select(bone => bone != null ? bone.name : string.Empty);
            var sceneBones = sceneRenderer.bones.Select(bone => bone != null ? bone.name : string.Empty);
            if (!approvedBones.SequenceEqual(sceneBones))
            {
                throw new InvalidOperationException("Ostinato scene rig bone order differs from the approved model.");
            }
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static int CountOtherConfiguredAnimators(Transform placementRoot, Transform idleSlot)
        {
            var count = 0;
            for (var index = 0; index < placementRoot.childCount; index++)
            {
                var slot = placementRoot.GetChild(index);
                if (slot == idleSlot)
                {
                    continue;
                }

                count += slot.GetComponentsInChildren<Animator>(true)
                    .Count(animator => animator.runtimeAnimatorController != null);
            }

            return count;
        }

        private static string[] GetBreathingBlendShapeNames()
        {
            return new[] { BlendShapeName, UpperBlendShapeName, LowerBlendShapeName };
        }

        private static void DeleteLegacyDirectWeightReviewArtifacts()
        {
            foreach (var relativePath in new[]
                     {
                         ReviewImagePath,
                         LegacyReviewImagePath,
                         AnimatorPlaybackReportPath,
                         AnimatorPlaybackImagePath,
                     })
            {
                var absolutePath = ProjectAbsolutePath(relativePath);
                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }
            }
        }

        private static Bounds ExpandBounds(Bounds bounds, float factor)
        {
            bounds.Expand(bounds.size * Mathf.Max(0f, factor - 1f));
            return bounds;
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatBounds(Bounds bounds)
        {
            return "Center=" + FormatVector(bounds.center) + ",Size=" + FormatVector(bounds.size);
        }

        private static string FormatVector(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######})",
                value.x,
                value.y,
                value.z);
        }

        private readonly struct TransformState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            private TransformState(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.localScale = localScale;
            }

            public static TransformState Capture(Transform transform)
            {
                return new TransformState(transform.localPosition, transform.localRotation, transform.localScale);
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(localPosition, transform.localPosition) <= 0.0001f &&
                       Quaternion.Angle(localRotation, transform.localRotation) <= 0.001f &&
                       Vector3.Distance(localScale, transform.localScale) <= 0.0001f;
            }
        }

        private readonly struct LayerState
        {
            public LayerState(GameObject gameObject, int layer)
            {
                GameObject = gameObject;
                Layer = layer;
            }

            public GameObject GameObject { get; }
            public int Layer { get; }
        }

        private readonly struct MorphStats
        {
            public MorphStats(
                int affectedOrganicVertexCount,
                int organicVertexCount,
                int bladeVertexCount,
                int torsoAffectedVertexCount,
                int headAffectedVertexCount,
                int armAffectedVertexCount,
                int legAffectedVertexCount,
                float maxTorsoVertexDelta,
                float maxHeadVertexDelta,
                float maxArmVertexDelta,
                float maxLegVertexDelta,
                float maxVertexDelta,
                float maxBladeVertexDelta,
                Vector3 neutralOrganicSize,
                Vector3 inhaleOrganicSize,
                Vector3 organicSilhouetteSizeDelta,
                float maxGroundVerticalDelta,
                int sourceBlendShapeCount,
                int generatedBlendShapeCount)
            {
                AffectedOrganicVertexCount = affectedOrganicVertexCount;
                OrganicVertexCount = organicVertexCount;
                BladeVertexCount = bladeVertexCount;
                TorsoAffectedVertexCount = torsoAffectedVertexCount;
                HeadAffectedVertexCount = headAffectedVertexCount;
                ArmAffectedVertexCount = armAffectedVertexCount;
                LegAffectedVertexCount = legAffectedVertexCount;
                MaxTorsoVertexDelta = maxTorsoVertexDelta;
                MaxHeadVertexDelta = maxHeadVertexDelta;
                MaxArmVertexDelta = maxArmVertexDelta;
                MaxLegVertexDelta = maxLegVertexDelta;
                MaxVertexDelta = maxVertexDelta;
                MaxBladeVertexDelta = maxBladeVertexDelta;
                NeutralOrganicSize = neutralOrganicSize;
                InhaleOrganicSize = inhaleOrganicSize;
                OrganicSilhouetteSizeDelta = organicSilhouetteSizeDelta;
                MaxGroundVerticalDelta = maxGroundVerticalDelta;
                SourceBlendShapeCount = sourceBlendShapeCount;
                GeneratedBlendShapeCount = generatedBlendShapeCount;
            }

            public int AffectedOrganicVertexCount { get; }
            public int OrganicVertexCount { get; }
            public int BladeVertexCount { get; }
            public int TorsoAffectedVertexCount { get; }
            public int HeadAffectedVertexCount { get; }
            public int ArmAffectedVertexCount { get; }
            public int LegAffectedVertexCount { get; }
            public float MaxTorsoVertexDelta { get; }
            public float MaxHeadVertexDelta { get; }
            public float MaxArmVertexDelta { get; }
            public float MaxLegVertexDelta { get; }
            public float MaxVertexDelta { get; }
            public float MaxBladeVertexDelta { get; }
            public Vector3 NeutralOrganicSize { get; }
            public Vector3 InhaleOrganicSize { get; }
            public Vector3 OrganicSilhouetteSizeDelta { get; }
            public float MaxGroundVerticalDelta { get; }
            public int SourceBlendShapeCount { get; }
            public int GeneratedBlendShapeCount { get; }
        }

        private readonly struct BodyInfluence
        {
            public BodyInfluence(
                Vector3 boneCenter,
                float torsoWeight,
                float radialScale,
                float verticalScale,
                float silhouetteHorizontalScale,
                float silhouetteVerticalScale)
            {
                BoneCenter = boneCenter;
                TorsoWeight = torsoWeight;
                RadialScale = radialScale;
                VerticalScale = verticalScale;
                SilhouetteHorizontalScale = silhouetteHorizontalScale;
                SilhouetteVerticalScale = silhouetteVerticalScale;
            }

            public Vector3 BoneCenter { get; }
            public float TorsoWeight { get; }
            public float RadialScale { get; }
            public float VerticalScale { get; }
            public float SilhouetteHorizontalScale { get; }
            public float SilhouetteVerticalScale { get; }
        }

        private readonly struct BreathingRegionInfluence
        {
            public BreathingRegionInfluence(
                Vector3 boneCenter,
                float coreWeight,
                float headWeight,
                float armWeight,
                float lowerWeight)
            {
                BoneCenter = boneCenter;
                CoreWeight = coreWeight;
                HeadWeight = headWeight;
                ArmWeight = armWeight;
                LowerWeight = lowerWeight;
            }

            public Vector3 BoneCenter { get; }
            public float CoreWeight { get; }
            public float HeadWeight { get; }
            public float ArmWeight { get; }
            public float LowerWeight { get; }
        }

        private enum BodyRegion
        {
            Other,
            Torso,
            Head,
            Arm,
            Leg,
        }

        private readonly struct CaptureInfo
        {
            public CaptureInfo(Bounds neutralBounds, Bounds inhaleBounds)
            {
                NeutralBounds = neutralBounds;
                InhaleBounds = inhaleBounds;
            }

            public Bounds NeutralBounds { get; }
            public Bounds InhaleBounds { get; }
        }
    }

    [InitializeOnLoad]
    internal static class OstinatoIdleBreathingRuntimeCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string IdleSlotName = "Ostinato_02_Idle_Breathing";
        private const string StateName = "Ostinato_02_Idle_Breathing_Morph";
        private const string CoreBlendShapeName = "Ostinato_Idle_Breathing_CoreMorph";
        private const string UpperBlendShapeName = "Ostinato_Idle_Breathing_UpperMorph";
        private const string LowerBlendShapeName = "Ostinato_Idle_Breathing_LowerMorph";
        private const string ValidationFolderPath = "docs/validation/ostinato_idle_breathing_2026-07-19";
        private const string FrameFolderPath = ValidationFolderPath + "/runtime_frames";
        private const string RuntimeReportPath = ValidationFolderPath + "/Ostinato_IdleBreathingRuntimePlayback.txt";
        private const string RuntimeHtmlPath = ValidationFolderPath + "/Ostinato_IdleBreathingRuntimePlayback.html";
        private const string CompletionPath = ValidationFolderPath + "/Ostinato_IdleBreathingRuntimePlayback.completed";
        private const string FailurePath = ValidationFolderPath + "/Ostinato_IdleBreathingRuntimePlayback.failed.txt";
        private const string SessionStateKey = "Bellerophon.OstinatoIdleBreathingRuntimeCapture.State";
        private const string SessionFailureKey = "Bellerophon.OstinatoIdleBreathingRuntimeCapture.Failed";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int ReviewLayer = 30;
        private const int CaptureFramesPerSecond = 12;
        private const int CaptureImageSize = 320;
        private const float LoopDurationSeconds = 3.20f;
        private const float CaptureLoopCount = 2f;

        private static Animator animator;
        private static SkinnedMeshRenderer renderer;
        private static Camera reviewCamera;
        private static GameObject cameraObject;
        private static GameObject keyObject;
        private static GameObject fillObject;
        private static GameObject[] layeredObjects;
        private static int[] originalLayers;
        private static Bounds framingBounds;
        private static float startNormalizedTime;
        private static double captureStartEditorTime;
        private static int nextFrameIndex;
        private static int totalFrameCount;
        private static int coreBlendShapeIndex;
        private static int upperBlendShapeIndex;
        private static int lowerBlendShapeIndex;
        private static readonly StringBuilder FrameSamples = new StringBuilder();

        static OstinatoIdleBreathingRuntimeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Unity must be in Edit Mode before Ostinato runtime playback capture begins.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before runtime playback capture begins.");
            }

            var validationFolder = ProjectAbsolutePath(ValidationFolderPath);
            var frameFolder = ProjectAbsolutePath(FrameFolderPath);
            Directory.CreateDirectory(validationFolder);
            Directory.CreateDirectory(frameFolder);
            foreach (var framePath in Directory.GetFiles(frameFolder, "frame_*.png"))
            {
                File.Delete(framePath);
            }

            DeleteIfPresent(ProjectAbsolutePath(RuntimeReportPath));
            DeleteIfPresent(ProjectAbsolutePath(RuntimeHtmlPath));
            DeleteIfPresent(ProjectAbsolutePath(CompletionPath));
            DeleteIfPresent(ProjectAbsolutePath(FailurePath));
            SessionState.SetBool(SessionFailureKey, false);
            SessionState.SetInt(SessionStateKey, WaitingForPlayMode);
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            var state = SessionState.GetInt(SessionStateKey, 0);
            if (state == 0)
            {
                return;
            }

            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (EditorApplication.isPlaying)
                    {
                        TryStartRuntimeCapture();
                    }

                    return;
                }

                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException("Unity left Play Mode before Ostinato capture completed.");
                    }

                    CaptureRuntimeFrameWhenDue();
                    return;
                }

                if (state == WaitingForEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (!SessionState.GetBool(SessionFailureKey, false))
                    {
                        File.WriteAllText(
                            ProjectAbsolutePath(CompletionPath),
                            "Ostinato runtime playback capture completed after returning to Edit Mode.",
                            new UTF8Encoding(false));
                        Debug.Log(
                            "OstinatoIdleBreathingRuntimePlaybackCaptured" +
                            ", Frames=" + totalFrameCount.ToString(CultureInfo.InvariantCulture) +
                            ", Loops=2" +
                            ", DirectBlendShapeSetterUsed=False" +
                            ", Html=" + RuntimeHtmlPath);
                    }

                    SessionState.EraseInt(SessionStateKey);
                    SessionState.EraseBool(SessionFailureKey);
                }
            }
            catch (Exception exception)
            {
                FailCapture(exception);
            }
        }

        private static void TryStartRuntimeCapture()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                return;
            }

            var placementRoot = scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName)?.transform;
            var idleSlot = placementRoot != null ? placementRoot.Find(IdleSlotName) : null;
            if (idleSlot == null)
            {
                return;
            }

            animator = idleSlot.GetComponent<Animator>();
            renderer = idleSlot.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault();
            if (animator == null || renderer == null || !animator.enabled || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("Ostinato slot 02 runtime Animator or renderer is not active.");
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(StateName) && !stateInfo.IsName("Base Layer." + StateName))
            {
                return;
            }

            coreBlendShapeIndex = RequireBlendShape(renderer.sharedMesh, CoreBlendShapeName);
            upperBlendShapeIndex = RequireBlendShape(renderer.sharedMesh, UpperBlendShapeName);
            lowerBlendShapeIndex = RequireBlendShape(renderer.sharedMesh, LowerBlendShapeName);
            layeredObjects = idleSlot.GetComponentsInChildren<Transform>(true)
                .Select(target => target.gameObject)
                .ToArray();
            originalLayers = layeredObjects.Select(target => target.layer).ToArray();
            foreach (var target in layeredObjects)
            {
                target.layer = ReviewLayer;
            }

            cameraObject = new GameObject("Ostinato_RuntimePlayback_ReviewCamera", typeof(Camera));
            keyObject = new GameObject("Ostinato_RuntimePlayback_KeyLight", typeof(Light));
            fillObject = new GameObject("Ostinato_RuntimePlayback_FillLight", typeof(Light));
            reviewCamera = cameraObject.GetComponent<Camera>();
            ConfigureReviewCameraAndLights(
                reviewCamera,
                keyObject.transform,
                keyObject.GetComponent<Light>(),
                fillObject.transform,
                fillObject.GetComponent<Light>());

            framingBounds = renderer.bounds;
            startNormalizedTime = stateInfo.normalizedTime;
            captureStartEditorTime = EditorApplication.timeSinceStartup;
            nextFrameIndex = 0;
            totalFrameCount = Mathf.CeilToInt(LoopDurationSeconds * CaptureFramesPerSecond * CaptureLoopCount) + 1;
            FrameSamples.Length = 0;
            SessionState.SetInt(SessionStateKey, Capturing);
        }

        private static void CaptureRuntimeFrameWhenDue()
        {
            if (EditorApplication.timeSinceStartup - captureStartEditorTime > 30d)
            {
                throw new TimeoutException("Ostinato runtime Animator did not complete two breathing loops within 30 seconds.");
            }

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(StateName) && !stateInfo.IsName("Base Layer." + StateName))
            {
                throw new InvalidOperationException("Ostinato runtime Animator left its idle breathing state.");
            }

            var elapsedNormalizedTime = stateInfo.normalizedTime - startNormalizedTime;
            var targetNormalizedTime = nextFrameIndex /
                                       (LoopDurationSeconds * CaptureFramesPerSecond);
            if (elapsedNormalizedTime + 0.002f < targetNormalizedTime)
            {
                return;
            }

            var frame = RenderCombinedFrame(reviewCamera, framingBounds);
            try
            {
                var frameName = "frame_" + nextFrameIndex.ToString("0000", CultureInfo.InvariantCulture) + ".png";
                File.WriteAllBytes(
                    Path.Combine(ProjectAbsolutePath(FrameFolderPath), frameName),
                    frame.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(frame);
            }

            FrameSamples.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "Frame{0}=AnimatorNormalized:{1:0.######},Core:{2:0.###},Upper:{3:0.###},Lower:{4:0.###}",
                nextFrameIndex,
                stateInfo.normalizedTime,
                renderer.GetBlendShapeWeight(coreBlendShapeIndex),
                renderer.GetBlendShapeWeight(upperBlendShapeIndex),
                renderer.GetBlendShapeWeight(lowerBlendShapeIndex)));
            nextFrameIndex++;
            if (nextFrameIndex >= totalFrameCount)
            {
                FinishCapture();
            }
        }

        private static void FinishCapture()
        {
            WriteRuntimeReport();
            WriteRuntimeHtml();
            CleanupRuntimeObjects();
            SessionState.SetInt(SessionStateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void WriteRuntimeReport()
        {
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + IdleSlotName);
            report.AppendLine("PlaybackMode=Unity Editor Play Mode scene Animator");
            report.AppendLine("AnimatorState=" + StateName);
            report.AppendLine("BlendShapes=" + CoreBlendShapeName + "|" + UpperBlendShapeName + "|" + LowerBlendShapeName);
            report.AppendLine("CapturedLoops=2");
            report.AppendLine("CaptureFramesPerSecond=" + CaptureFramesPerSecond.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("CapturedFrameCount=" + totalFrameCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("DirectBlendShapeSetterUsed=False");
            report.AppendLine("FrameFolder=" + FrameFolderPath);
            report.AppendLine("MotionReview=" + RuntimeHtmlPath);
            report.Append(FrameSamples);
            File.WriteAllText(
                ProjectAbsolutePath(RuntimeReportPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteRuntimeHtml()
        {
            var html = "<!doctype html>\n" +
                       "<html lang=\"ko\"><head><meta charset=\"utf-8\">" +
                       "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                       "<title>오스티나토 일반 대기 실제 재생</title>" +
                       "<style>body{margin:0;background:#090d12;color:#e6edf3;font-family:system-ui,sans-serif;display:grid;place-items:center;min-height:100vh}" +
                       "main{width:min(960px,94vw)}h1{font-size:24px;margin:0 0 8px}p{color:#9fb0c0;margin:0 0 18px}" +
                       "img{display:block;width:100%;background:#070a0e;border:1px solid #273342;border-radius:12px}" +
                       ".controls{display:flex;gap:10px;align-items:center;margin-top:14px}button{background:#27384a;color:white;border:0;border-radius:8px;padding:9px 14px}" +
                       "input{flex:1}</style></head><body><main>" +
                       "<h1>오스티나토 일반 대기 실제 Animator 재생</h1>" +
                       "<p>Unity Play Mode에서 캡처한 두 호흡 주기입니다. 위는 전신, 아래는 유기체 확대 화면입니다.</p>" +
                       "<img id=\"frame\" alt=\"오스티나토 호흡 모션\">" +
                       "<div class=\"controls\"><button id=\"toggle\">일시정지</button><input id=\"seek\" type=\"range\" min=\"0\" max=\"" +
                       (totalFrameCount - 1).ToString(CultureInfo.InvariantCulture) +
                       "\" value=\"0\"><output id=\"counter\"></output></div>" +
                       "<script>const count=" + totalFrameCount.ToString(CultureInfo.InvariantCulture) +
                       ",fps=" + CaptureFramesPerSecond.ToString(CultureInfo.InvariantCulture) +
                       ";let i=0,playing=true;const img=document.querySelector('#frame'),seek=document.querySelector('#seek'),counter=document.querySelector('#counter'),toggle=document.querySelector('#toggle');" +
                       "function show(){img.src='runtime_frames/frame_'+String(i).padStart(4,'0')+'.png';seek.value=i;counter.value=(i+1)+' / '+count;}" +
                       "setInterval(()=>{if(playing){i=(i+1)%count;show();}},1000/fps);toggle.onclick=()=>{playing=!playing;toggle.textContent=playing?'일시정지':'재생';};" +
                       "seek.oninput=()=>{i=Number(seek.value);show();};show();</script></main></body></html>";
            File.WriteAllText(ProjectAbsolutePath(RuntimeHtmlPath), html, new UTF8Encoding(false));
        }

        private static Texture2D RenderCombinedFrame(Camera camera, Bounds bounds)
        {
            PositionReviewCamera(camera.transform, bounds, 1f);
            var fullFrame = RenderFrame(camera);
            PositionReviewCamera(camera.transform, bounds, 0.72f);
            var closeFrame = RenderFrame(camera);
            var combined = new Texture2D(CaptureImageSize * 2, CaptureImageSize, TextureFormat.RGBA32, false);
            try
            {
                combined.SetPixels32(0, 0, CaptureImageSize, CaptureImageSize, fullFrame.GetPixels32());
                combined.SetPixels32(CaptureImageSize, 0, CaptureImageSize, CaptureImageSize, closeFrame.GetPixels32());
                combined.Apply(false, false);
                return combined;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fullFrame);
                UnityEngine.Object.DestroyImmediate(closeFrame);
            }
        }

        private static Texture2D RenderFrame(Camera camera)
        {
            var renderTexture = RenderTexture.GetTemporary(
                CaptureImageSize,
                CaptureImageSize,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(CaptureImageSize, CaptureImageSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, CaptureImageSize, CaptureImageSize), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void ConfigureReviewCameraAndLights(
            Camera camera,
            Transform keyTransform,
            Light key,
            Transform fillTransform,
            Light fill)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << ReviewLayer;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            key.type = LightType.Directional;
            key.intensity = 1.45f;
            key.color = new Color(1.00f, 0.89f, 0.72f);
            key.cullingMask = 1 << ReviewLayer;
            keyTransform.rotation = Quaternion.Euler(38f, -32f, 0f);
            fill.type = LightType.Directional;
            fill.intensity = 0.78f;
            fill.color = new Color(0.46f, 0.66f, 1.00f);
            fill.cullingMask = 1 << ReviewLayer;
            fillTransform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void PositionReviewCamera(Transform cameraTransform, Bounds bounds, float distanceMultiplier)
        {
            var target = bounds.center + Vector3.up * (bounds.extents.y * 0.02f);
            var halfFovRadians = 42f * 0.5f * Mathf.Deg2Rad;
            var distance = (Mathf.Max(bounds.extents.y, bounds.extents.x) / Mathf.Tan(halfFovRadians) +
                            bounds.extents.z + 0.35f) * distanceMultiplier;
            cameraTransform.position = target + Vector3.back * distance;
            cameraTransform.rotation = Quaternion.LookRotation(target - cameraTransform.position, Vector3.up);
        }

        private static void CleanupRuntimeObjects()
        {
            if (layeredObjects != null && originalLayers != null)
            {
                for (var index = 0; index < Mathf.Min(layeredObjects.Length, originalLayers.Length); index++)
                {
                    if (layeredObjects[index] != null)
                    {
                        layeredObjects[index].layer = originalLayers[index];
                    }
                }
            }

            DestroyRuntimeObject(cameraObject);
            DestroyRuntimeObject(keyObject);
            DestroyRuntimeObject(fillObject);
            animator = null;
            renderer = null;
            reviewCamera = null;
            cameraObject = null;
            keyObject = null;
            fillObject = null;
            layeredObjects = null;
            originalLayers = null;
        }

        private static void FailCapture(Exception exception)
        {
            try
            {
                Directory.CreateDirectory(ProjectAbsolutePath(ValidationFolderPath));
                File.WriteAllText(ProjectAbsolutePath(FailurePath), exception.ToString(), new UTF8Encoding(false));
            }
            finally
            {
                CleanupRuntimeObjects();
                SessionState.SetBool(SessionFailureKey, true);
                SessionState.SetInt(SessionStateKey, WaitingForEditMode);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }
                else if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.EraseInt(SessionStateKey);
                }
            }
        }

        private static int RequireBlendShape(Mesh mesh, string blendShapeName)
        {
            var blendShapeIndex = mesh != null ? mesh.GetBlendShapeIndex(blendShapeName) : -1;
            if (blendShapeIndex < 0)
            {
                throw new InvalidOperationException("Runtime Ostinato mesh is missing BlendShape: " + blendShapeName);
            }

            return blendShapeIndex;
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }
    }
}
