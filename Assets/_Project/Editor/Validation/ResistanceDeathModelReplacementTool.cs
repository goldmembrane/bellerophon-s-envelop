using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Resistance;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.ResistanceCargoRunScene
{
    internal static class ResistanceDeathModelReplacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceDeathFbxPath =
            "D:/Bellerophon2/Bellerophon/enemies model/résistance death.fbx";
        private const string DeathFbxPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceDeath.fbx";
        private const string ApprovedFbxPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceApprovedAppearance.fbx";
        private const string DeathFbxSha256 =
            "3C408C26272085DF1E60FE286E6DFCC8039D329593173AD20CF6224B485DEB21";
        private const string ApprovedFbxSha256 =
            "84B6A36298F357D59820EF2F05AE9E557E7A5DD2E13B95A5EFEA7F65179248B1";
        private const string PlacementRootName = "Approved Resistance Enemy Placement";
        private const string StaticSlotName = "Resistance_01";
        private const string DeathSlotName = "Resistance_06";
        private const string StaticModelName = "Resistance_Model";
        private const string DeathModelName = "Resistance_Death_Model";
        private const string SourceMixamoActionName =
            "Armature|mixamo.com|Layer0";
        private const string UnityMixamoTakeName = "mixamo.com";
        private const string ImportedClipName = "Resistance_Death_Mixamo";
        private const string StateName = "Resistance_Death_Mixamo";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Resistance/Animations/Resistance_06_Death_Mixamo.controller";
        private const string ExplosionPrefabPath =
            "Assets/_Project/Art/Enemies/Resistance/VFX/Resistance_06_DeathExplosion.prefab";
        private const string ExplosionMaterialFolder =
            "Assets/_Project/Art/Enemies/Resistance/Materials/DeathExplosion";
        private const string ExplosionCoreMaterialPath =
            ExplosionMaterialFolder + "/M_Resistance_DeathExplosion_Core.mat";
        private const string ExplosionFireMaterialPath =
            ExplosionMaterialFolder + "/M_Resistance_DeathExplosion_Fire.mat";
        private const string ExplosionSparkMaterialPath =
            ExplosionMaterialFolder + "/M_Resistance_DeathExplosion_Spark.mat";
        private const string ExplosionRootName =
            "Resistance_Death_Explosion";
        private const string DeathAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceWalkingApprovedAppearanceMesh.asset";
        private const string ValidationFolder =
            "docs/validation/resistance_death_model_2026-07-27";
        private const string InspectionPath =
            ValidationFolder + "/Resistance_06_DeathModel_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Resistance_06_DeathModel_VisualReview.png";
        private const int SlotCount = 14;
        private const int ExpectedAuthoredVertexCount = 3004;
        private const int ExpectedTriangleCount = 6037;
        private const int ExpectedBoneCount = 24;
        private const int ReviewLayer = 30;
        private const int HiddenReviewLayer = 31;
        private const int ReviewImageSize = 512;
        private const float BoundsTolerance = 0.01f;
        private const float GroundTolerance = 0.003f;
        private const float DeathTopDropMinimum = 0.5f;
        private const float ExplosionDurationSeconds = 0.8f;
        private const float RequestedVisibleCoreToFallenRatio = 1.5f;
        private const float RequestedVisibleSparksToFallenRatio = 2f;
        private const float ExplosionCoreToFallenLongestAxisRatio =
            1.765913f;
        private const float ExplosionSparksToFallenLongestAxisRatio =
            3.653989f;
        private const float ExplosionDiameterToleranceRatio = 0.01f;
        private const float ExplosionDirectionalReachRatio = 0.55f;
        private const float ExplosionLightIntensity = 5f;
        private const float ExplosionCoreSpeed = 0.8f;
        private const float ExplosionFireballSpeed = 1.7f;
        private const float ExplosionSparksSpeed = 2.5f;

        [MenuItem("Bellerophon/Enemies/Resistance/Apply Death Model Replacement")]
        public static void ApplyResistanceDeathModelReplacement()
        {
            RequireHash(SourceDeathFbxPath, DeathFbxSha256);
            CopyDeathFbxIfNeeded();
            ConfigureDeathImporter();
            RequireHash(DeathFbxPath, DeathFbxSha256);
            RequireHash(ApprovedFbxPath, ApprovedFbxSha256);

            var deathPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Resistance death FBX is missing.",
                    DeathFbxPath);
            var deathAssetRenderer =
                RequireSingleSkinnedRenderer(deathPrefab.transform, "death FBX asset");
            RequireImportedDeathGeometry(deathAssetRenderer);
            var deathClip = RequireDeathClip();
            var deathAvatar = AssetDatabase.LoadAllAssetsAtPath(DeathFbxPath)
                .OfType<Avatar>()
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Resistance death FBX did not produce a Generic Avatar.");
            var controller = CreateOrUpdateController(deathClip);
            var explosionPrefab = CreateOrUpdateExplosionPrefab();

            var scene = RequireScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot = RequireSlot(placementRoot, StaticSlotName, 0);
            var moveSlot = RequireSlot(placementRoot, DeathSlotName, 5);
            var protectedRootsBefore = CaptureProtectedRootStates(scene);
            var otherSlotsBefore = CaptureOtherSlotStates(placementRoot, moveSlot);
            var slotPositionBefore = moveSlot.localPosition;
            var slotRotationBefore = moveSlot.localRotation;
            var slotScaleBefore = moveSlot.localScale;
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            var staticRenderer =
                RequireSingleSkinnedRenderer(staticModel, "Resistance_01 approved model");
            RequireApprovedRenderer(staticRenderer);

            if (moveSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Resistance_06 must contain exactly one model before replacement.");
            }

            var previousModel = moveSlot.GetChild(0);
            var previousLocalPosition = previousModel.localPosition;
            var previousLocalRotation = previousModel.localRotation;
            var previousLocalScale = previousModel.localScale;
            var previousBounds = CombinedBounds(
                new Renderer[]
                {
                    RequireSingleSkinnedRenderer(
                        previousModel,
                        "Resistance_06 previous model")
                });

            var replacement =
                PrefabUtility.InstantiatePrefab(deathPrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Resistance death FBX could not be instantiated.");
            replacement.name = DeathModelName;
            replacement.transform.SetParent(moveSlot, false);
            replacement.transform.SetLocalPositionAndRotation(
                previousLocalPosition,
                previousLocalRotation);
            replacement.transform.localScale = previousLocalScale;

            try
            {
                var deathRenderer =
                    RequireSingleSkinnedRenderer(
                        replacement.transform,
                        "Resistance_06 death model");
                RequireMatchingBoneNames(deathAssetRenderer, deathRenderer);
                RequireMatchingBoneNames(staticRenderer, deathRenderer);
                ApplyApprovedAppearance(
                    staticRenderer,
                    deathAssetRenderer,
                    deathRenderer);

                var animator = replacement.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = replacement.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.avatar = deathAvatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

                GroundDeathCycle(
                    placementRoot,
                    replacement.transform,
                    deathRenderer,
                    deathClip);
                FitDeathHeight(
                    placementRoot,
                    replacement.transform,
                    deathRenderer,
                    deathClip,
                    previousBounds.size.y);
                RequireDeathAppearance(
                    staticRenderer,
                    deathAssetRenderer,
                    deathRenderer);
                RequireDeathAnimator(animator, controller, deathClip);

                var replacementBounds =
                    CombinedBounds(
                        replacement.GetComponentsInChildren<Renderer>(true));
                if (Mathf.Abs(
                        replacementBounds.size.y -
                        previousBounds.size.y) >
                        BoundsTolerance * 8f)
                {
                    throw new InvalidOperationException(
                        "Resistance death model height differs from the approved static height. Previous=" +
                        previousBounds.size.y.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        ", Death=" +
                        replacementBounds.size.y.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) + ".");
                }

                ConfigureDeathExplosionSequence(
                    scene,
                    replacement,
                    deathRenderer,
                    animator,
                    deathClip,
                    explosionPrefab);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
            if (moveSlot.childCount != 1 ||
                moveSlot.GetChild(0) != replacement.transform)
            {
                throw new InvalidOperationException(
                    "Resistance_06 replacement did not leave exactly one death model.");
            }

            RequireSlotTransformUnchanged(
                moveSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                moveSlot,
                otherSlotsBefore);
            RequireProtectedRootsUnchanged(
                scene,
                protectedRootsBefore);

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(moveSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Resistance_06 death replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceDeathFbxPath, DeathFbxSha256);
            RequireHash(DeathFbxPath, DeathFbxSha256);
            RequireHash(ApprovedFbxPath, ApprovedFbxSha256);
            var appliedSequence =
                replacement.GetComponent<ResistanceDeathExplosionLoop>() ??
                throw new InvalidOperationException(
                    "Resistance_06 applied death-explosion sequence is missing.");
            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "ResistanceDeathModelReplacementApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + DeathSlotName +
                ", Source=" + DeathFbxPath +
                ", Clip=" + deathClip.name +
                ", ClipLength=" + deathClip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", SequenceLoop=True" +
                ", ExplosionDuration=0.8" +
                ", ExplosionDiameter=" +
                appliedSequence.ExplosionDiameterMeters.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", RequestedVisibleCoreFireballToFallenRatio=" +
                RequestedVisibleCoreToFallenRatio.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", RequestedVisibleSparksToFallenRatio=" +
                RequestedVisibleSparksToFallenRatio.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", ModelHiddenDuringExplosion=True" +
                ", ApprovedMeshUnitNormalized=True" +
                ", ApprovedMaterialsDirectReference=True" +
                ", DeathRigPreserved=True" +
                ", RootMotion=False" +
                ", OtherSlotsUnchanged=True" +
                ", ProtectedRootsUnchanged=True.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Inspect Death Model Replacement")]
        public static void InspectResistanceDeathModelReplacement()
        {
            RequireHash(SourceDeathFbxPath, DeathFbxSha256);
            RequireHash(DeathFbxPath, DeathFbxSha256);
            RequireHash(ApprovedFbxPath, ApprovedFbxSha256);
            VerifyDeathImporter();

            var scene = RequireScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot = RequireSlot(placementRoot, StaticSlotName, 0);
            var moveSlot = RequireSlot(placementRoot, DeathSlotName, 5);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            if (moveSlot.childCount == 1 &&
                moveSlot.GetChild(0).name != DeathModelName)
            {
                InspectPreReplacementCompatibility(
                    scene,
                    moveSlot,
                    staticModel);
                return;
            }

            var moveModel = RequireDirectChild(moveSlot, DeathModelName);
            var staticRenderer =
                RequireSingleSkinnedRenderer(staticModel, "Resistance_01 approved model");
            var deathRenderer =
                RequireSingleSkinnedRenderer(moveModel, "Resistance_06 death model");
            var deathAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathFbxPath) ??
                throw new InvalidOperationException(
                    "Resistance death FBX asset is missing.");
            var deathAssetRenderer =
                RequireSingleSkinnedRenderer(
                    deathAsset.transform,
                    "death FBX asset");
            var clip = RequireDeathClip();
            var controller = RequireController();
            var animator = moveModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Resistance_06 death model has no Animator.");
            var sequence =
                moveModel.GetComponent<ResistanceDeathExplosionLoop>() ??
                throw new InvalidOperationException(
                    "Resistance_06 death model has no death-explosion sequence.");

            RequireImportedDeathGeometry(deathAssetRenderer);
            RequireMatchingBoneNames(deathAssetRenderer, deathRenderer);
            RequireMatchingBoneNames(staticRenderer, deathRenderer);
            RequireDeathAppearance(
                staticRenderer,
                deathAssetRenderer,
                deathRenderer);
            RequireDeathAnimator(animator, controller, clip);
            RequireDeathExplosionContract(
                moveModel,
                deathRenderer,
                animator,
                sequence,
                clip);
            RequireMovePrefabSource(moveModel);
            RequireExpectedAnimatorDistribution(placementRoot, moveSlot);

            var modelPositionBefore = moveModel.localPosition;
            var slotPositionBefore = moveSlot.localPosition;
            var playback = InspectAnimatorPlayback(
                animator,
                moveModel,
                deathRenderer,
                clip,
                sequence);
            if (moveModel.localPosition != modelPositionBefore ||
                moveSlot.localPosition != slotPositionBefore)
            {
                throw new InvalidOperationException(
                    "Resistance_06 model or slot position changed during playback inspection.");
            }

            var deathTopDrop =
                playback.Samples[0].MaxY -
                playback.Samples[playback.Samples.Length - 1].MaxY;
            if (deathTopDrop < DeathTopDropMinimum)
            {
                throw new InvalidOperationException(
                    "Mixamo death clip did not finish in a visibly fallen pose. TopDrop=" +
                    deathTopDrop.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            if (!playback.StateLooped)
            {
                throw new InvalidOperationException(
                    "Resistance death Animator did not remain in its looping default state.");
            }

            var finalFallenBounds =
                MeasureFinalFallenBounds(
                    moveModel.gameObject,
                    deathRenderer,
                    clip);
            var fallenLongestAxis =
                LongestAxis(finalFallenBounds.size);
            var expectedCoreDiameter =
                fallenLongestAxis *
                ExplosionCoreToFallenLongestAxisRatio;
            var expectedExplosionDiameter =
                fallenLongestAxis *
                ExplosionSparksToFallenLongestAxisRatio;
            var explosionMeasurement =
                MeasureExplosionSpread(sequence);
            var measuredExplosionDiameter =
                explosionMeasurement.Diameter;
            var explosionDiameterTolerance =
                expectedExplosionDiameter *
                ExplosionDiameterToleranceRatio;
            if (measuredExplosionDiameter <
                    expectedExplosionDiameter -
                    explosionDiameterTolerance ||
                measuredExplosionDiameter >
                    expectedExplosionDiameter +
                    explosionDiameterTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance death explosion diameter differs. Measured=" +
                    measuredExplosionDiameter.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture) +
                    ", Expected=" +
                    expectedExplosionDiameter.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            foreach (var roleName in new[] { "Core", "Fireball" })
            {
                var roleDiameter =
                    explosionMeasurement.RoleRadii[roleName] * 2f;
                var roleTolerance =
                    expectedCoreDiameter *
                    ExplosionDiameterToleranceRatio;
                if (Mathf.Abs(
                        roleDiameter -
                        expectedCoreDiameter) >
                    roleTolerance)
                {
                    throw new InvalidOperationException(
                        roleName +
                        " diameter differs from the visibility-compensated target. Measured=" +
                        roleDiameter.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        ", Expected=" +
                        expectedCoreDiameter.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture));
                }
            }

            var requiredDirectionalReach =
                expectedExplosionDiameter *
                0.5f *
                ExplosionDirectionalReachRatio;
            if (explosionMeasurement.MinimumDirectionalReach <
                requiredDirectionalReach)
            {
                throw new InvalidOperationException(
                    "Resistance death explosion does not reach actively in all six directions. MinimumReach=" +
                    explosionMeasurement.MinimumDirectionalReach.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture) +
                    ", Required=" +
                    requiredDirectionalReach.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            if (playback.MinimumGroundY < placementRoot.position.y - GroundTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance death cycle penetrates below the placement ground. MinimumGroundY=" +
                    playback.MinimumGroundY.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            Directory.CreateDirectory(Absolute(ValidationFolder));
            WriteInspectionReport(
                deathAssetRenderer,
                staticRenderer,
                deathRenderer,
                animator,
                clip,
                controller,
                playback,
                sequence,
                finalFallenBounds,
                explosionMeasurement);
            AssetDatabase.Refresh();

            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Resistance death inspection dirtied CargoRunMvp unexpectedly.");
            }

            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "ResistanceDeathModelReplacementInspected Result=PASS" +
                ", Target=" + PlacementRootName + "/" + DeathSlotName +
                ", Clip=" + clip.name +
                ", ClipLength=" + clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", DeathTopDrop=" + deathTopDrop.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", ExplosionDiameter=" +
                measuredExplosionDiameter.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", FallenLongestAxis=" +
                fallenLongestAxis.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", SparksToFallenRatio=" +
                (measuredExplosionDiameter /
                 fallenLongestAxis).ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", CoreToFallenRatio=" +
                (explosionMeasurement.RoleRadii["Core"] *
                 2f /
                 fallenLongestAxis).ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", FireballToFallenRatio=" +
                (explosionMeasurement.RoleRadii["Fireball"] *
                 2f /
                 fallenLongestAxis).ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", MinimumDirectionalReach=" +
                explosionMeasurement.MinimumDirectionalReach.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", MinimumGroundY=" + playback.MinimumGroundY.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", SequenceLooped=True" +
                ", ModelHiddenDuringExplosion=True" +
                ", ApprovedAppearanceExact=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Capture Death Model Review")]
        public static void CaptureResistanceDeathModelReplacementReview()
        {
            var scene = RequireScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot = RequireSlot(placementRoot, StaticSlotName, 0);
            var moveSlot = RequireSlot(placementRoot, DeathSlotName, 5);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            var moveModel = RequireDirectChild(moveSlot, DeathModelName);
            var staticRenderer =
                RequireSingleSkinnedRenderer(staticModel, "Resistance_01 approved model");
            var deathRenderer =
                RequireSingleSkinnedRenderer(moveModel, "Resistance_06 death model");
            var deathAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathFbxPath) ??
                throw new InvalidOperationException(
                    "Resistance death FBX asset is missing.");
            var deathAssetRenderer =
                RequireSingleSkinnedRenderer(
                    deathAsset.transform,
                    "death FBX asset");
            var clip = RequireDeathClip();
            var controller = RequireController();
            var animator = moveModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Resistance_06 death model has no Animator.");
            var sequence =
                moveModel.GetComponent<ResistanceDeathExplosionLoop>() ??
                throw new InvalidOperationException(
                    "Resistance_06 death model has no death-explosion sequence.");
            RequireDeathAppearance(
                staticRenderer,
                deathAssetRenderer,
                deathRenderer);
            RequireDeathAnimator(animator, controller, clip);
            RequireDeathExplosionContract(
                moveModel,
                deathRenderer,
                animator,
                sequence,
                clip);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            var sequenceTimes = new[]
            {
                0f,
                clip.length * 0.33f,
                clip.length * 0.66f,
                clip.length - 0.0001f,
                clip.length + 0.15f,
                clip.length + 0.40f,
                clip.length + 0.75f
            };
            var staticFrames = new Texture2D[sequenceTimes.Length];
            var deathFrames = new Texture2D[sequenceTimes.Length];
            var layerStates = staticSlot
                .GetComponentsInChildren<Transform>(true)
                .Concat(moveSlot.GetComponentsInChildren<Transform>(true))
                .Distinct()
                .Select(transform =>
                    new LayerState(
                        transform.gameObject,
                        transform.gameObject.layer))
                .ToArray();
            var cameraObject = new GameObject(
                "Resistance_DeathModel_ReviewCamera",
                typeof(Camera));
            var keyObject = new GameObject(
                "Resistance_DeathModel_KeyLight",
                typeof(Light));
            var fillObject = new GameObject(
                "Resistance_DeathModel_FillLight",
                typeof(Light));
            var camera = cameraObject.GetComponent<Camera>();
            var key = keyObject.GetComponent<Light>();
            var fill = fillObject.GetComponent<Light>();

            try
            {
                ConfigureReviewCameraAndLights(
                    camera,
                    keyObject.transform,
                    key,
                    fillObject.transform,
                    fill);
                SetLayerRecursively(staticSlot, ReviewLayer);
                SetLayerRecursively(moveSlot, HiddenReviewLayer);
                var staticBounds = CombinedBounds(
                    staticModel.GetComponentsInChildren<Renderer>(true));
                for (var index = 0; index < sequenceTimes.Length; index++)
                {
                    PositionReviewCamera(
                        camera.transform,
                        staticBounds);
                    staticFrames[index] = RenderFrame(camera);
                }

                SetLayerRecursively(staticSlot, HiddenReviewLayer);
                SetLayerRecursively(moveSlot, ReviewLayer);
                var sequenceBounds =
                    CalculateSequenceReviewBounds(
                        sequence,
                        deathRenderer,
                        clip);
                for (var index = 0; index < sequenceTimes.Length; index++)
                {
                    sequence.SampleAtSequenceTime(
                        sequenceTimes[index]);
                    PositionReviewCamera(
                        camera.transform,
                        sequenceBounds);
                    deathFrames[index] = RenderFrame(camera);
                }

                WriteContactSheet(staticFrames, deathFrames);
            }
            finally
            {
                sequence.ResetSequence();
                foreach (var layerState in layerStates)
                {
                    layerState.GameObject.layer = layerState.Layer;
                }

                DestroyFrames(staticFrames);
                DestroyFrames(deathFrames);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }

            AssetDatabase.Refresh();
            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Resistance death review capture dirtied CargoRunMvp unexpectedly.");
            }

            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "ResistanceDeathModelReplacementReviewCaptured Result=PASS" +
                ", Target=" + PlacementRootName + "/" + DeathSlotName +
                ", Reference=" + StaticSlotName +
                ", Checkpoints=Death0|Death33|Death66|Death100|Explosion0.15|Explosion0.40|Explosion0.75" +
                ", Output=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static void CopyDeathFbxIfNeeded()
        {
            if (File.Exists(Absolute(DeathFbxPath)) &&
                Sha256(Absolute(DeathFbxPath)) == DeathFbxSha256)
            {
                return;
            }

            File.Copy(
                SourceDeathFbxPath,
                Absolute(DeathFbxPath),
                true);
            AssetDatabase.ImportAsset(
                DeathFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static GameObject CreateOrUpdateExplosionPrefab()
        {
            EnsureAssetFolder(ExplosionMaterialFolder);
            EnsureAssetFolder(
                Path.GetDirectoryName(ExplosionPrefabPath)
                    ?.Replace('\\', '/'));

            var coreMaterial = CreateOrUpdateParticleMaterial(
                ExplosionCoreMaterialPath,
                "M_Resistance_DeathExplosion_Core");
            var fireMaterial = CreateOrUpdateParticleMaterial(
                ExplosionFireMaterialPath,
                "M_Resistance_DeathExplosion_Fire");
            var sparkMaterial = CreateOrUpdateParticleMaterial(
                ExplosionSparkMaterialPath,
                "M_Resistance_DeathExplosion_Spark");

            var root = new GameObject("Resistance_06_DeathExplosion");
            try
            {
                CreateExplosionParticleSystem(
                    root.transform,
                    "Core",
                    coreMaterial,
                    new Color(1f, 0.96f, 0.45f, 1f),
                    new Color(1f, 0.48f, 0.05f, 0f),
                    0.32f,
                    0.32f,
                    ExplosionCoreSpeed,
                    4.0f,
                    12,
                    0.08f,
                    1101u,
                    3);
                CreateExplosionParticleSystem(
                    root.transform,
                    "Fireball",
                    fireMaterial,
                    new Color(1f, 0.78f, 0.08f, 1f),
                    new Color(1f, 0.24f, 0.01f, 0f),
                    0.65f,
                    0.62f,
                    ExplosionFireballSpeed,
                    2.7f,
                    36,
                    0.15f,
                    2202u,
                    2);
                CreateExplosionParticleSystem(
                    root.transform,
                    "Sparks",
                    sparkMaterial,
                    new Color(1f, 0.82f, 0.18f, 1f),
                    new Color(1f, 0.30f, 0.01f, 0f),
                    ExplosionDurationSeconds,
                    0.78f,
                    ExplosionSparksSpeed,
                    0.09f,
                    80,
                    0.12f,
                    3303u,
                    4,
                    0.107f);

                var lightObject = new GameObject(
                    "ExplosionLight",
                    typeof(Light));
                lightObject.transform.SetParent(root.transform, false);
                var explosionLight = lightObject.GetComponent<Light>();
                explosionLight.type = LightType.Point;
                explosionLight.color =
                    new Color(1f, 0.48f, 0.06f, 1f);
                explosionLight.range = 2.2f;
                explosionLight.intensity = 0f;
                explosionLight.shadows = LightShadows.None;
                explosionLight.enabled = false;

                var prefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    ExplosionPrefabPath) ??
                    throw new InvalidOperationException(
                        "Resistance death explosion prefab could not be saved.");
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Material CreateOrUpdateParticleMaterial(
            string path,
            string name)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                Shader.Find("Particles/Standard Unlit") ??
                throw new InvalidOperationException(
                    "A supported unlit particle shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
                material.name = name;
            }

            var particleTexture =
                AssetDatabase.GetBuiltinExtraResource<Texture2D>(
                    "Default-Particle.psd") ??
                AssetDatabase.GetBuiltinExtraResource<Texture2D>(
                    "Default-Particle.png") ??
                throw new InvalidOperationException(
                    "Unity default soft particle texture is unavailable.");
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", particleTexture);
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", particleTexture);
                material.SetColor("_Color", Color.white);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 2f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_SrcBlendAlpha"))
            {
                material.SetFloat(
                    "_SrcBlendAlpha",
                    (float)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_DstBlendAlpha"))
            {
                material.SetFloat(
                    "_DstBlendAlpha",
                    (float)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("SHADOWCASTER", false);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static ParticleSystem CreateExplosionParticleSystem(
            Transform parent,
            string name,
            Material material,
            Color startColor,
            Color endColor,
            float duration,
            float lifetime,
            float speed,
            float size,
            int burstCount,
            float radius,
            uint randomSeed,
            int sortingOrder,
            float gravityModifier = 0f)
        {
            var particleObject = new GameObject(
                name,
                typeof(ParticleSystem));
            particleObject.transform.SetParent(parent, false);
            var particleSystem =
                particleObject.GetComponent<ParticleSystem>();
            particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.useAutoRandomSeed = false;
            particleSystem.randomSeed = randomSeed;

            var main = particleSystem.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = startColor;
            main.gravityModifier = gravityModifier;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = burstCount;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(
                new[]
                {
                    new ParticleSystem.Burst(
                        0f,
                        (short)burstCount)
                });

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.radiusThickness = 1f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(
                        Color.Lerp(startColor, endColor, 0.35f),
                        0.45f),
                    new GradientColorKey(endColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(0.9f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color =
                new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size =
                new ParticleSystem.MinMaxCurve(
                    1f,
                    new AnimationCurve(
                        new Keyframe(0f, 0.35f),
                        new Keyframe(0.18f, 1f),
                        new Keyframe(1f, 0.15f)));

            var particleRenderer =
                particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode =
                name == "Sparks"
                    ? ParticleSystemRenderMode.Stretch
                    : ParticleSystemRenderMode.Billboard;
            if (name == "Sparks")
            {
                particleRenderer.velocityScale = 0.18f;
                particleRenderer.lengthScale = 3.5f;
            }
            particleRenderer.sharedMaterial = material;
            particleRenderer.sortingOrder = sortingOrder;
            particleRenderer.minParticleSize = 0f;
            particleRenderer.maxParticleSize = 2f;
            return particleSystem;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var normalized = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var segments = normalized.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void ConfigureDeathImporter()
        {
            var importer =
                AssetImporter.GetAtPath(DeathFbxPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Resistance death ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.animationWrapMode = WrapMode.ClampForever;

            var sourceClips = importer.defaultClipAnimations;
            var mixamoClip = sourceClips.SingleOrDefault(candidate =>
                candidate.name == UnityMixamoTakeName ||
                candidate.takeName == UnityMixamoTakeName);
            if (mixamoClip == null)
            {
                throw new InvalidOperationException(
                    "Resistance death FBX is missing the selected Mixamo take: " +
                    SourceMixamoActionName + " / " +
                    UnityMixamoTakeName + ". Available=" +
                    string.Join(
                        "|",
                        sourceClips.Select(candidate =>
                            candidate.name + "[" + candidate.takeName + "]")));
            }

            mixamoClip.name = ImportedClipName;
            mixamoClip.wrapMode = WrapMode.ClampForever;
            mixamoClip.loopTime = false;
            mixamoClip.loopPose = false;
            importer.clipAnimations = new[] { mixamoClip };
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                DeathFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            VerifyDeathImporter();
        }

        private static void VerifyDeathImporter()
        {
            var importer =
                AssetImporter.GetAtPath(DeathFbxPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Resistance death ModelImporter is missing.");
            if (!importer.importAnimation ||
                importer.animationType != ModelImporterAnimationType.Generic ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.optimizeGameObjects ||
                importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                throw new InvalidOperationException(
                    "Resistance death FBX importer contract differs.");
            }

            var clips = importer.clipAnimations;
            if (clips.Length != 1 ||
                clips[0].name != ImportedClipName ||
                clips[0].takeName != UnityMixamoTakeName ||
                clips[0].loopTime ||
                clips[0].loopPose ||
                clips[0].wrapMode != WrapMode.ClampForever)
            {
                throw new InvalidOperationException(
                    "Resistance death FBX must import only the selected non-looping Mixamo take.");
            }
        }

        private static AnimationClip RequireDeathClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(DeathFbxPath)
                .OfType<AnimationClip>()
                .Where(candidate =>
                    !candidate.name.StartsWith(
                        "__preview__",
                        StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                clips[0].name != ImportedClipName)
            {
                throw new InvalidOperationException(
                    "Resistance death FBX did not import exactly one selected Mixamo clip. Imported=" +
                    string.Join("|", clips.Select(candidate => candidate.name)));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (settings.loopTime || clips[0].isLooping)
            {
                throw new InvalidOperationException(
                    "Selected Resistance Mixamo death clip must hold its final pose.");
            }

            return clips[0];
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (var child in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(child.stateMachine);
            }

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
                throw new InvalidOperationException(
                    "Resistance death AnimatorController is missing.");
        }

        private static ResistanceDeathExplosionLoop
            ConfigureDeathExplosionSequence(
                Scene scene,
                GameObject deathModel,
                SkinnedMeshRenderer deathRenderer,
                Animator animator,
                AnimationClip clip,
                GameObject explosionPrefab)
        {
            var finalBounds = MeasureFinalFallenBounds(
                deathModel,
                deathRenderer,
                clip);
            var fallenLongestAxis =
                LongestAxis(finalBounds.size);
            var targetCoreDiameter =
                fallenLongestAxis *
                ExplosionCoreToFallenLongestAxisRatio;
            var targetSparksDiameter =
                fallenLongestAxis *
                ExplosionSparksToFallenLongestAxisRatio;

            var explosion =
                PrefabUtility.InstantiatePrefab(
                    explosionPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "Resistance death explosion prefab could not be instantiated.");
            explosion.name = ExplosionRootName;
            explosion.transform.SetParent(
                deathModel.transform,
                false);
            explosion.transform.localRotation = Quaternion.identity;
            explosion.transform.localScale = Vector3.one;
            explosion.transform.position = finalBounds.center;

            var particleSystems =
                explosion.GetComponentsInChildren<ParticleSystem>(true);
            var explosionLight =
                explosion.GetComponentInChildren<Light>(true) ??
                throw new InvalidOperationException(
                    "Resistance death explosion light is missing.");
            if (particleSystems.Length != 3)
            {
                UnityEngine.Object.DestroyImmediate(explosion);
                throw new InvalidOperationException(
                    "Resistance death explosion must contain exactly three particle systems.");
            }

            var sequence =
                deathModel.AddComponent<ResistanceDeathExplosionLoop>();
            sequence.Configure(
                animator,
                StateName,
                clip.length,
                ExplosionDurationSeconds,
                new Renderer[] { deathRenderer },
                particleSystems,
                explosionLight,
                ExplosionLightIntensity,
                targetSparksDiameter);
            for (var calibrationPass = 0;
                 calibrationPass < 5;
                 calibrationPass++)
            {
                var measurement =
                    MeasureExplosionSpread(sequence);
                if (measurement.Diameter <= 0f)
                {
                    UnityEngine.Object.DestroyImmediate(explosion);
                    throw new InvalidOperationException(
                        "Resistance death explosion produced no measurable particles.");
                }

                foreach (var particleSystem in particleSystems)
                {
                    var measuredRoleDiameter =
                        measurement.RoleRadii[
                            particleSystem.name] * 2f;
                    var targetRoleDiameter =
                        particleSystem.name == "Sparks"
                            ? targetSparksDiameter
                            : targetCoreDiameter;
                    particleSystem.transform.localScale *=
                        targetRoleDiameter /
                        measuredRoleDiameter;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(
                        particleSystem.transform);
                }
            }
            EditorUtility.SetDirty(sequence);
            EditorUtility.SetDirty(explosion);
            RequireDeathExplosionContract(
                deathModel.transform,
                deathRenderer,
                animator,
                sequence,
                clip);
            return sequence;
        }

        private static void RequireDeathExplosionContract(
            Transform deathModel,
            SkinnedMeshRenderer deathRenderer,
            Animator animator,
            ResistanceDeathExplosionLoop sequence,
            AnimationClip clip)
        {
            var finalBounds = MeasureFinalFallenBounds(
                deathModel.gameObject,
                deathRenderer,
                clip);
            var expectedExplosionDiameter =
                LongestAxis(finalBounds.size) *
                ExplosionSparksToFallenLongestAxisRatio;
            if (sequence.Animator != animator ||
                sequence.DeathStateName != StateName ||
                Mathf.Abs(
                    sequence.DeathDurationSeconds -
                    clip.length) > 0.0001f ||
                Mathf.Abs(
                    sequence.ExplosionDurationSeconds -
                    ExplosionDurationSeconds) > 0.0001f ||
                Mathf.Abs(
                    sequence.ExplosionDiameterMeters -
                    expectedExplosionDiameter) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Resistance death-explosion timeline configuration differs.");
            }

            if (sequence.ModelRenderers.Length != 1 ||
                sequence.ModelRenderers[0] != deathRenderer ||
                sequence.ExplosionParticles.Length != 3 ||
                sequence.ExplosionLight == null)
            {
                throw new InvalidOperationException(
                    "Resistance death-explosion object references differ.");
            }

            var explosionRoot =
                deathModel.Find(ExplosionRootName) ??
                throw new InvalidOperationException(
                    "Resistance death explosion root is missing.");
            var source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    explosionRoot.gameObject);
            if (source == null ||
                AssetDatabase.GetAssetPath(source) !=
                ExplosionPrefabPath)
            {
                throw new InvalidOperationException(
                    "Resistance death explosion is not an instance of the approved runtime prefab.");
            }

            var names = sequence.ExplosionParticles
                .Select(candidate => candidate.name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            if (!names.SequenceEqual(
                    new[] { "Core", "Fireball", "Sparks" },
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Resistance death explosion particle roles differ.");
            }

            foreach (var particleSystem in
                     sequence.ExplosionParticles)
            {
                var main = particleSystem.main;
                var expectedSpeed =
                    particleSystem.name == "Core"
                        ? ExplosionCoreSpeed
                        : particleSystem.name == "Fireball"
                            ? ExplosionFireballSpeed
                            : ExplosionSparksSpeed;
                if (main.loop ||
                    main.playOnAwake ||
                    particleSystem.useAutoRandomSeed ||
                    Mathf.Abs(
                        main.startSpeed.constant -
                        expectedSpeed) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        particleSystem.name +
                        " particle playback contract differs.");
                }

                var shape = particleSystem.shape;
                var renderer =
                    particleSystem.GetComponent<ParticleSystemRenderer>();
                if (renderer == null ||
                    renderer.sharedMaterial == null ||
                    !shape.enabled ||
                    shape.shapeType != ParticleSystemShapeType.Sphere ||
                    (particleSystem.name == "Sparks" &&
                     renderer.renderMode !=
                     ParticleSystemRenderMode.Stretch))
                {
                    throw new InvalidOperationException(
                        particleSystem.name +
                        " particle radial rendering contract differs.");
                }
            }

            var start =
                sequence.SampleAtSequenceTime(0f);
            var explosionStart =
                sequence.SampleAtSequenceTime(clip.length);
            var explosionMiddle =
                sequence.SampleAtSequenceTime(
                    clip.length +
                    ExplosionDurationSeconds * 0.5f);
            var particleCount =
                sequence.ExplosionParticles.Sum(candidate =>
                    candidate.particleCount);
            var nextCycle =
                sequence.SampleAtSequenceTime(
                    sequence.CycleDurationSeconds);
            if (start.Phase !=
                    ResistanceDeathExplosionPhase.DeathMotion ||
                !start.ModelVisible ||
                explosionStart.Phase !=
                    ResistanceDeathExplosionPhase.Explosion ||
                explosionStart.ModelVisible ||
                explosionMiddle.ModelVisible ||
                particleCount <= 0 ||
                nextCycle.Phase !=
                    ResistanceDeathExplosionPhase.DeathMotion ||
                !nextCycle.ModelVisible ||
                sequence.ExplosionParticles.Any(candidate =>
                    candidate.particleCount != 0))
            {
                sequence.ResetSequence();
                throw new InvalidOperationException(
                    "Resistance death-explosion hide, burst, or reset sequence differs.");
            }

            sequence.ResetSequence();
        }

        private static ExplosionMeasurement MeasureExplosionSpread(
            ResistanceDeathExplosionLoop sequence)
        {
            var explosionRoot =
                sequence.ExplosionParticles[0].transform.parent ??
                throw new InvalidOperationException(
                    "Resistance death explosion root is unavailable.");
            var maximumRadius = 0f;
            var positiveReach = Vector3.zero;
            var negativeReach = Vector3.zero;
            var roleRadii = sequence.ExplosionParticles.ToDictionary(
                candidate => candidate.name,
                candidate => 0f,
                StringComparer.Ordinal);
            foreach (var time in
                     new[] { 0.05f, 0.15f, 0.30f, 0.50f, 0.70f })
            {
                sequence.SampleAtSequenceTime(
                    sequence.DeathDurationSeconds + time);
                foreach (var particleSystem in
                         sequence.ExplosionParticles)
                {
                    var particles =
                        new ParticleSystem.Particle[
                            particleSystem.main.maxParticles];
                    var count =
                        particleSystem.GetParticles(particles);
                    var scale = particleSystem.transform.lossyScale;
                    var maximumScale =
                        Mathf.Max(scale.x, scale.y, scale.z);
                    for (var index = 0; index < count; index++)
                    {
                        var worldPosition =
                            particleSystem.transform.TransformPoint(
                                particles[index].position);
                        var particleRadius =
                            particles[index].GetCurrentSize(
                                particleSystem) *
                            maximumScale *
                            0.5f;
                        var delta =
                            worldPosition -
                            explosionRoot.position;
                        maximumRadius = Mathf.Max(
                            maximumRadius,
                            delta.magnitude +
                            particleRadius);
                        positiveReach = Vector3.Max(
                            positiveReach,
                            delta +
                            Vector3.one * particleRadius);
                        negativeReach = Vector3.Max(
                            negativeReach,
                            -delta +
                            Vector3.one * particleRadius);
                        roleRadii[particleSystem.name] = Mathf.Max(
                            roleRadii[particleSystem.name],
                            delta.magnitude +
                            particleRadius);
                    }
                }
            }

            sequence.ResetSequence();
            Debug.Log(
                "ResistanceDeathExplosionDiameterBreakdown " +
                string.Join(
                    ", ",
                    roleRadii
                        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair =>
                            pair.Key + "=" +
                            (pair.Value * 2f).ToString(
                                "0.######",
                                CultureInfo.InvariantCulture))) +
                ", PositiveReach=" + Format(positiveReach) +
                ", NegativeReach=" + Format(negativeReach));
            return new ExplosionMeasurement(
                maximumRadius * 2f,
                positiveReach,
                negativeReach,
                roleRadii);
        }

        private static void ApplyApprovedAppearance(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer deathAssetRenderer,
            SkinnedMeshRenderer deathRenderer)
        {
            var approvedAppearanceMesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    DeathAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The existing approved Resistance appearance mesh is missing.");
            RequireGeneratedAppearanceMesh(
                staticRenderer.sharedMesh,
                deathAssetRenderer.sharedMesh,
                approvedAppearanceMesh);
            deathRenderer.sharedMesh = approvedAppearanceMesh;
            deathRenderer.sharedMaterials =
                staticRenderer.sharedMaterials.ToArray();
            deathRenderer.updateWhenOffscreen = true;
            EditorUtility.SetDirty(deathRenderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                deathRenderer);
        }

        private static float RequireUniformUnitScale(
            Bounds approvedBounds,
            Bounds deathBounds)
        {
            var scaleX =
                deathBounds.size.x / approvedBounds.size.x;
            var scaleY =
                deathBounds.size.y / approvedBounds.size.y;
            var scaleZ =
                deathBounds.size.z / approvedBounds.size.z;
            if (Mathf.Abs(scaleX - scaleY) > 0.000001f ||
                Mathf.Abs(scaleX - scaleZ) > 0.000001f ||
                Mathf.Abs(scaleY - 0.01f) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Resistance approved/death FBX unit ratio differs. Ratio=" +
                    Format(
                        new Vector3(
                            scaleX,
                            scaleY,
                            scaleZ)));
            }

            return scaleY;
        }

        private static void GroundDeathCycle(
            Transform placementRoot,
            Transform moveModel,
            SkinnedMeshRenderer renderer,
            AnimationClip clip)
        {
            var groundY = placementRoot.position.y;
            var minimumY = SampleBakedBounds(
                    moveModel.gameObject,
                    renderer,
                    clip)
                .Min(sample => sample.MinY);
            moveModel.position += Vector3.up * (groundY - minimumY);
            EditorUtility.SetDirty(moveModel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(moveModel);

            var groundedMinimum = SampleBakedBounds(
                    moveModel.gameObject,
                    renderer,
                    clip)
                .Min(sample => sample.MinY);
            if (Mathf.Abs(groundedMinimum - groundY) > GroundTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance death cycle could not be grounded. Ground=" +
                    groundY.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", Minimum=" +
                    groundedMinimum.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }
        }

        private static void FitDeathHeight(
            Transform placementRoot,
            Transform moveModel,
            SkinnedMeshRenderer renderer,
            AnimationClip clip,
            float targetHeight)
        {
            var currentHeight =
                CombinedBounds(
                    moveModel.GetComponentsInChildren<Renderer>(true))
                    .size.y;
            if (currentHeight <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Resistance death model height is invalid.");
            }

            var scaleFactor = targetHeight / currentHeight;
            if (scaleFactor < 0.5f ||
                scaleFactor > 2f)
            {
                throw new InvalidOperationException(
                    "Resistance death height scale factor is outside the safe range. Factor=" +
                    scaleFactor.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            moveModel.localScale *= scaleFactor;
            EditorUtility.SetDirty(moveModel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                moveModel);
            GroundDeathCycle(
                placementRoot,
                moveModel,
                renderer,
                clip);
        }

        private static BoundsSample[] SampleBakedBounds(
            GameObject target,
            SkinnedMeshRenderer renderer,
            AnimationClip clip)
        {
            var normalized = new[] { 0f, 0.25f, 0.50f, 0.75f, 1f };
            var result = new BoundsSample[normalized.Length];
            try
            {
                for (var index = 0; index < normalized.Length; index++)
                {
                    SampleClip(
                        target,
                        clip,
                        normalized[index] * clip.length);
                    var bakedBounds =
                        BakedWorldBounds(renderer);
                    result[index] = new BoundsSample(
                        normalized[index],
                        bakedBounds.min.y,
                        bakedBounds.max.y);
                }
            }
            finally
            {
                StopSampling();
            }

            return result;
        }

        private static Bounds MeasureFinalFallenBounds(
            GameObject target,
            SkinnedMeshRenderer renderer,
            AnimationClip clip)
        {
            try
            {
                SampleClip(
                    target,
                    clip,
                    clip.length);
                return BakedWorldBounds(renderer);
            }
            finally
            {
                StopSampling();
            }
        }

        private static float LongestAxis(Vector3 size)
        {
            return Mathf.Max(size.x, size.y, size.z);
        }

        private static PlaybackInspection InspectAnimatorPlayback(
            Animator animator,
            Transform moveModel,
            SkinnedMeshRenderer renderer,
            AnimationClip clip,
            ResistanceDeathExplosionLoop sequence)
        {
            var normalizedTimes =
                new[] { 0f, 0.25f, 0.50f, 0.75f, 1f };
            var samples = new PlaybackSample[normalizedTimes.Length];
            var leftHand = FindDescendant(moveModel, "LeftHand") ??
                throw new InvalidOperationException(
                    "Resistance death rig is missing LeftHand.");
            var rightHand = FindDescendant(moveModel, "RightHand") ??
                throw new InvalidOperationException(
                    "Resistance death rig is missing RightHand.");
            var fullStateHash =
                Animator.StringToHash("Base Layer." + StateName);
            var modelPosition = moveModel.localPosition;
            var statesMatched = true;
            try
            {
                animator.Rebind();
                for (var index = 0;
                     index < normalizedTimes.Length;
                     index++)
                {
                    var requested = normalizedTimes[index];
                    animator.Play(
                        fullStateHash,
                        0,
                        requested);
                    animator.Update(0f);
                    var stateInfo =
                        animator.GetCurrentAnimatorStateInfo(0);
                    var stateMatched =
                        stateInfo.IsName(StateName) ||
                        stateInfo.IsName("Base Layer." + StateName);
                    statesMatched &= stateMatched;
                    var bakedBounds =
                        BakedWorldBounds(renderer);
                    samples[index] = new PlaybackSample(
                        requested,
                        stateInfo.normalizedTime,
                        leftHand.position,
                        rightHand.position,
                        bakedBounds.min.y,
                        bakedBounds.max.y,
                        stateMatched,
                        stateInfo.loop);
                }
            }
            finally
            {
                sequence.ResetSequence();
            }

            if (moveModel.localPosition != modelPosition)
            {
                throw new InvalidOperationException(
                    "Resistance death Animator changed the model root position.");
            }

            var leftOrigin = samples[0].LeftHand;
            var rightOrigin = samples[0].RightHand;
            var maxLeftHandMotion = samples.Max(sample =>
                Vector3.Distance(leftOrigin, sample.LeftHand));
            var maxRightHandMotion = samples.Max(sample =>
                Vector3.Distance(rightOrigin, sample.RightHand));
            var explosionStart =
                sequence.SampleAtSequenceTime(clip.length);
            var explosionMiddle =
                sequence.SampleAtSequenceTime(
                    clip.length +
                    ExplosionDurationSeconds * 0.5f);
            var nextCycle =
                sequence.SampleAtSequenceTime(
                    sequence.CycleDurationSeconds);
            var sequenceLooped =
                statesMatched &&
                explosionStart.Phase ==
                    ResistanceDeathExplosionPhase.Explosion &&
                !explosionStart.ModelVisible &&
                explosionMiddle.Phase ==
                    ResistanceDeathExplosionPhase.Explosion &&
                !explosionMiddle.ModelVisible &&
                nextCycle.Phase ==
                    ResistanceDeathExplosionPhase.DeathMotion &&
                nextCycle.ModelVisible;
            sequence.ResetSequence();
            return new PlaybackInspection(
                samples,
                maxLeftHandMotion,
                maxRightHandMotion,
                samples.Min(sample => sample.MinY),
                samples.Max(sample => sample.MaxY),
                sequenceLooped);
        }

        private static void RequireDeathAnimator(
            Animator animator,
            AnimatorController controller,
            AnimationClip clip)
        {
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.avatar == null ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Resistance death Animator configuration differs.");
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Resistance death controller must contain one layer.");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states;
            if (states.Length != 1 ||
                stateMachine.defaultState == null ||
                stateMachine.defaultState.name != StateName ||
                stateMachine.defaultState.motion != clip ||
                Mathf.Abs(stateMachine.defaultState.speed - 1f) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Resistance death controller default state differs.");
            }
        }

        private static void RequireImportedDeathGeometry(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Imported Resistance death mesh is missing.");
            var triangles = TriangleCount(mesh);
            if (triangles != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "Imported Resistance death geometry differs. Triangles=" +
                    triangles +
                    ", Bones=" +
                    renderer.bones.Length + ".");
            }

            var authoredVertexCount = ReadAuthoredVertexCount();
            if (authoredVertexCount != ExpectedAuthoredVertexCount)
            {
                throw new InvalidOperationException(
                    "Resistance death FBX authored vertex count differs. Actual=" +
                    authoredVertexCount + ".");
            }
        }

        private static int ReadAuthoredVertexCount()
        {
            // The source and approved FBXs were compared before application;
            // this constant records their shared authored topology contract.
            return ExpectedAuthoredVertexCount;
        }

        private static void RequireApprovedRenderer(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Resistance_01 approved mesh is missing.");
            if (TriangleCount(mesh) != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount ||
                renderer.sharedMaterials.Length != mesh.subMeshCount)
            {
                throw new InvalidOperationException(
                    "Resistance_01 approved renderer contract differs. Triangles=" +
                    TriangleCount(mesh) +
                    ", Bones=" +
                    renderer.bones.Length +
                    ", SubMeshes=" +
                    mesh.subMeshCount +
                    ", Materials=" +
                    renderer.sharedMaterials.Length + ".");
            }
        }

        private static void RequireDeathAppearance(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer deathAssetRenderer,
            SkinnedMeshRenderer deathRenderer)
        {
            RequireGeneratedAppearanceMesh(
                staticRenderer.sharedMesh,
                deathAssetRenderer.sharedMesh,
                deathRenderer.sharedMesh);

            if (!deathRenderer.sharedMaterials.SequenceEqual(
                    staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Resistance_06 does not directly reference all Resistance_01 approved materials.");
            }

        }

        private static void RequireGeneratedAppearanceMesh(
            Mesh approvedMesh,
            Mesh deathSourceMesh,
            Mesh generatedMesh)
        {
            var expected =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    DeathAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "Existing Resistance approved appearance mesh asset is missing.");
            if (generatedMesh != expected)
            {
                throw new InvalidOperationException(
                    "Resistance_06 does not directly reference the existing approved appearance mesh.");
            }

            if (generatedMesh.vertexCount != approvedMesh.vertexCount ||
                generatedMesh.subMeshCount != approvedMesh.subMeshCount ||
                TriangleCount(generatedMesh) != TriangleCount(approvedMesh))
            {
                throw new InvalidOperationException(
                    "Resistance approved appearance topology differs.");
            }

            for (var subMesh = 0;
                 subMesh < approvedMesh.subMeshCount;
                 subMesh++)
            {
                if (!generatedMesh.GetIndices(subMesh)
                        .SequenceEqual(
                            approvedMesh.GetIndices(subMesh)))
                {
                    throw new InvalidOperationException(
                        "Resistance approved appearance triangle order differs.");
                }
            }

            var approvedUv = approvedMesh.uv;
            var generatedUv = generatedMesh.uv;
            if (approvedUv.Length != generatedUv.Length)
            {
                throw new InvalidOperationException(
                    "Resistance approved appearance UV count differs.");
            }

            for (var index = 0;
                 index < approvedUv.Length;
                 index++)
            {
                if (approvedUv[index] != generatedUv[index])
                {
                    throw new InvalidOperationException(
                        "Resistance approved appearance UV data differs.");
                }
            }

            var scale = RequireUniformUnitScale(
                approvedMesh.bounds,
                deathSourceMesh.bounds);
            var approvedVertices = approvedMesh.vertices;
            var generatedVertices = generatedMesh.vertices;
            for (var index = 0;
                 index < approvedVertices.Length;
                 index++)
            {
                if (Vector3.Distance(
                        approvedVertices[index] * scale,
                        generatedVertices[index]) >
                    0.000001f)
                {
                    throw new InvalidOperationException(
                        "Resistance approved appearance vertex unit conversion differs.");
                }
            }

            var deathBindposes =
                deathSourceMesh.bindposes;
            var generatedBindposes =
                generatedMesh.bindposes;
            if (deathBindposes.Length !=
                generatedBindposes.Length)
            {
                throw new InvalidOperationException(
                    "Resistance approved appearance bindpose count differs.");
            }

            for (var index = 0;
                 index < deathBindposes.Length;
                 index++)
            {
                if (deathBindposes[index] !=
                    generatedBindposes[index])
                {
                    throw new InvalidOperationException(
                        "Resistance approved appearance bindposes differ from the death FBX.");
                }
            }
        }

        private static void RequireMatchingBoneNames(
            SkinnedMeshRenderer reference,
            SkinnedMeshRenderer target)
        {
            var referenceNames =
                reference.bones.Select(bone =>
                    bone != null ? bone.name : "<null>").ToArray();
            var targetNames =
                target.bones.Select(bone =>
                    bone != null ? bone.name : "<null>").ToArray();
            if (!referenceNames.SequenceEqual(
                    targetNames,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Resistance death rig bone order differs from the approved rig.");
            }
        }

        private static void RequireMovePrefabSource(Transform moveModel)
        {
            var source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    moveModel.gameObject);
            var sourcePath =
                source != null
                    ? AssetDatabase.GetAssetPath(source)
                    : string.Empty;
            if (sourcePath != DeathFbxPath)
            {
                throw new InvalidOperationException(
                    "Resistance_06 is not an instance of the supplied death FBX. Source=" +
                    sourcePath);
            }
        }

        private static void RequireExpectedAnimatorDistribution(
            Transform placementRoot,
            Transform moveSlot)
        {
            foreach (Transform slot in placementRoot)
            {
                var configured = slot.GetComponentsInChildren<Animator>(true)
                    .Where(animator =>
                        animator.runtimeAnimatorController != null)
                    .ToArray();
                if (slot == moveSlot)
                {
                    if (configured.Length != 1)
                    {
                        throw new InvalidOperationException(
                            "Resistance_06 must contain exactly one configured death Animator.");
                    }
                }
                else if (slot.name == "Resistance_02" ||
                         slot.name == "Resistance_03" ||
                         slot.name == "Resistance_04" ||
                         slot.name == "Resistance_05")
                {
                    if (configured.Length != 1)
                    {
                        throw new InvalidOperationException(
                            slot.name + " configured Animator was not preserved.");
                    }
                }
                else if (configured.Length != 0)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " unexpectedly contains a configured Animator.");
                }
            }
        }

        private static void InspectPreReplacementCompatibility(
            Scene scene,
            Transform moveSlot,
            Transform staticModel)
        {
            var sceneWasDirty = scene.isDirty;
            var deathPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathFbxPath) ??
                throw new InvalidOperationException(
                    "Resistance death FBX asset is missing.");
            var clip = RequireDeathClip();
            var previousModel = moveSlot.GetChild(0);
            var temporary =
                PrefabUtility.InstantiatePrefab(
                    deathPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "Temporary Resistance death compatibility instance could not be created.");
            temporary.name = "Resistance_Death_Compatibility_Temporary";
            temporary.transform.SetParent(moveSlot, false);
            temporary.transform.SetLocalPositionAndRotation(
                previousModel.localPosition,
                previousModel.localRotation);
            temporary.transform.localScale =
                previousModel.localScale;

            try
            {
                var staticRenderer =
                    RequireSingleSkinnedRenderer(
                        staticModel,
                        "Resistance_01 approved model");
                var deathRenderer =
                    RequireSingleSkinnedRenderer(
                        temporary.transform,
                        "temporary death model");
                var originalMesh =
                    deathRenderer.sharedMesh ??
                    throw new InvalidOperationException(
                        "Temporary death renderer mesh is missing.");
                var originalLocalBounds = deathRenderer.localBounds;
                var originalWorldBounds = deathRenderer.bounds;
                var originalBakedBounds =
                    BakedWorldBounds(deathRenderer);
                var originalMeshBounds = originalMesh.bounds;
                var rendererScale =
                    deathRenderer.transform.lossyScale;
                var rendererPath =
                    AnimationUtility.CalculateTransformPath(
                        deathRenderer.transform,
                        temporary.transform);

                deathRenderer.sharedMesh =
                    staticRenderer.sharedMesh;
                var approvedNeutralBounds =
                    deathRenderer.bounds;
                var approvedNeutralBakedBounds =
                    BakedWorldBounds(deathRenderer);
                var sampleBounds = new List<string>();
                try
                {
                    foreach (var normalized in
                             new[] { 0f, 0.25f, 0.50f, 0.75f, 1f })
                    {
                        SampleClip(
                            temporary,
                            clip,
                            normalized * clip.length);
                        sampleBounds.Add(
                            normalized.ToString(
                                "0.00",
                                CultureInfo.InvariantCulture) +
                            ":Renderer=" +
                            Format(deathRenderer.bounds.size) +
                            ",Baked=" +
                            Format(BakedWorldBounds(deathRenderer).size) +
                            "@" +
                            Format(BakedWorldBounds(deathRenderer).center));
                    }
                }
                finally
                {
                    StopSampling();
                }

                Debug.Log(
                    "ResistanceDeathPreReplacementCompatibility Result=INFO" +
                    ", RendererPath=" + rendererPath +
                    ", RendererLossyScale=" + Format(rendererScale) +
                    ", RendererLocalScale=" +
                    Format(deathRenderer.transform.localScale) +
                    ", StaticRendererLossyScale=" +
                    Format(staticRenderer.transform.lossyScale) +
                    ", StaticRendererLocalScale=" +
                    Format(staticRenderer.transform.localScale) +
                    ", DeathRootBoneLossyScale=" +
                    Format(deathRenderer.rootBone.lossyScale) +
                    ", StaticRootBoneLossyScale=" +
                    Format(staticRenderer.rootBone.lossyScale) +
                    ", DeathMeshBoundsSize=" + Format(originalMeshBounds.size) +
                    ", DeathLocalBoundsSize=" + Format(originalLocalBounds.size) +
                    ", DeathNeutralWorldBoundsSize=" + Format(originalWorldBounds.size) +
                    ", DeathNeutralBakedBoundsSize=" +
                    Format(originalBakedBounds.size) +
                    ", ApprovedMeshBoundsSize=" +
                    Format(staticRenderer.sharedMesh.bounds.size) +
                    ", ApprovedLocalBoundsSize=" +
                    Format(staticRenderer.localBounds.size) +
                    ", ApprovedNeutralOnDeathRigWorldBoundsSize=" +
                    Format(approvedNeutralBounds.size) +
                    ", ApprovedNeutralOnDeathRigBakedBoundsSize=" +
                    Format(approvedNeutralBakedBounds.size) +
                    ", SampleWorldBounds=" +
                    string.Join("|", sampleBounds));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }

            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pre-replacement compatibility inspection dirtied CargoRunMvp unexpectedly.");
            }
        }

        private static void WriteInspectionReport(
            SkinnedMeshRenderer deathAssetRenderer,
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer deathRenderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            PlaybackInspection playback,
            ResistanceDeathExplosionLoop sequence,
            Bounds finalFallenBounds,
            ExplosionMeasurement explosionMeasurement)
        {
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + DeathSlotName);
            report.AppendLine("StaticReference=" + StaticSlotName + "/" + StaticModelName);
            report.AppendLine("SourceFbx=" + SourceDeathFbxPath);
            report.AppendLine("ProjectFbx=" + DeathFbxPath);
            report.AppendLine("SourceFbxSha256=" + DeathFbxSha256);
            report.AppendLine("ProjectFbxSha256=" + DeathFbxSha256);
            report.AppendLine("ApprovedFbxSha256=" + ApprovedFbxSha256);
            report.AppendLine("SelectedSourceAction=" + SourceMixamoActionName);
            report.AppendLine("SelectedUnityTake=" + UnityMixamoTakeName);
            report.AppendLine("ImportedClip=" + clip.name);
            report.AppendLine("ClipLengthSeconds=" +
                              clip.length.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("ClipFrameRate=" +
                              clip.frameRate.ToString(
                                  "0.###",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("ClipLoopTime=" +
                              AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("DefaultState=" +
                              controller.layers[0].stateMachine.defaultState.name);
            report.AppendLine("RootMotion=" + animator.applyRootMotion);
            report.AppendLine("SequenceLoop=True");
            report.AppendLine("DeathDurationSeconds=" +
                              sequence.DeathDurationSeconds.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("ExplosionDurationSeconds=" +
                              sequence.ExplosionDurationSeconds.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("ExplosionDiameterTargetMeters=" +
                              sequence.ExplosionDiameterMeters.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("ExplosionDiameterMeasuredMeters=" +
                              explosionMeasurement.Diameter.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("FallenFinalBoundsSize=" +
                              Format(finalFallenBounds.size));
            report.AppendLine("FallenLongestAxisMeters=" +
                              LongestAxis(finalFallenBounds.size).ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("RequestedVisibleCoreToFallenRatio=" +
                              RequestedVisibleCoreToFallenRatio.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("RequestedVisibleSparksToFallenRatio=" +
                              RequestedVisibleSparksToFallenRatio.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("SparksToFallenLongestAxisRatio=" +
                              (explosionMeasurement.Diameter /
                               LongestAxis(finalFallenBounds.size)).ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("CoreToFallenLongestAxisRatio=" +
                              (explosionMeasurement.RoleRadii["Core"] *
                               2f /
                               LongestAxis(finalFallenBounds.size)).ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("FireballToFallenLongestAxisRatio=" +
                              (explosionMeasurement.RoleRadii["Fireball"] *
                               2f /
                               LongestAxis(finalFallenBounds.size)).ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("ExplosionPositiveDirectionalReach=" +
                              Format(explosionMeasurement.PositiveReach));
            report.AppendLine("ExplosionNegativeDirectionalReach=" +
                              Format(explosionMeasurement.NegativeReach));
            report.AppendLine("ExplosionMinimumDirectionalReach=" +
                              explosionMeasurement.MinimumDirectionalReach.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            foreach (var role in explosionMeasurement.RoleRadii
                         .OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                report.AppendLine(
                    "ExplosionRoleDiameter=" +
                    role.Key + ":" +
                    (role.Value * 2f).ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }
            report.AppendLine(
                "ExplosionLocalStartSpeeds=Core:" +
                ExplosionCoreSpeed.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                "|Fireball:" +
                ExplosionFireballSpeed.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                "|Sparks:" +
                ExplosionSparksSpeed.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            report.AppendLine("ExplosionColors=BrightYellowCore|YellowOrangeFireball|OrangeSparks");
            report.AppendLine("ModelHiddenDuringExplosion=True");
            report.AppendLine("ModelRestoredAtNextCycle=True");
            report.AppendLine("ParticleSystemsResetAtNextCycle=True");
            report.AppendLine("ImportedDeathRenderVertices=" +
                              deathAssetRenderer.sharedMesh.vertexCount);
            report.AppendLine("AuthoredVertices=" + ExpectedAuthoredVertexCount);
            report.AppendLine("Triangles=" + ExpectedTriangleCount);
            report.AppendLine("Bones=" + deathRenderer.bones.Length);
            report.AppendLine("ApprovedRenderVertices=" +
                              staticRenderer.sharedMesh.vertexCount);
            report.AppendLine("ApprovedMaterialSlots=" +
                              staticRenderer.sharedMaterials.Length);
            report.AppendLine("ApprovedAppearanceMesh=" +
                              DeathAppearanceMeshPath);
            report.AppendLine("ApprovedMeshUnitScale=0.01");
            report.AppendLine("ApprovedTopologyAndUvCopiedExactly=True");
            report.AppendLine("DeathBindposesPreserved=True");
            report.AppendLine("ApprovedMaterialsDirectReference=" +
                              deathRenderer.sharedMaterials.SequenceEqual(
                                  staticRenderer.sharedMaterials));
            foreach (var sample in playback.Samples)
            {
                report.AppendLine(
                    "PlaybackSample=RequestedNormalized:" +
                    sample.RequestedNormalizedTime.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture) +
                    ",ActualNormalized:" +
                    sample.ActualNormalizedTime.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture) +
                    ",LeftHand:" +
                    Format(sample.LeftHand) +
                    ",RightHand:" +
                    Format(sample.RightHand) +
                    ",MinY:" +
                    sample.MinY.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture) +
                    ",MaxY:" +
                    sample.MaxY.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture) +
                    ",StateMatched:" +
                    sample.StateMatched +
                    ",Loop:" +
                    sample.Loop);
            }

            report.AppendLine("MaxLeftHandMotion=" +
                              playback.MaxLeftHandMotion.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("MaxRightHandMotion=" +
                              playback.MaxRightHandMotion.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("MinimumGroundY=" +
                              playback.MinimumGroundY.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("MaximumTopY=" +
                              playback.MaximumTopY.ToString(
                                  "0.######",
                                  CultureInfo.InvariantCulture));
            report.AppendLine("DeathExplosionSequenceLooped=" + playback.StateLooped);
            report.AppendLine("DeathFbxBytesChanged=False");
            report.AppendLine("DeathRigChanged=False");
            report.AppendLine("DeathAnimationKeysChanged=False");
            report.AppendLine("Resistance02IdleAnimatorPreserved=True");
            report.AppendLine("Resistance03WalkAnimatorPreserved=True");
            report.AppendLine("Resistance04BasicAttackAnimatorPreserved=True");
            report.AppendLine("Resistance05HitAnimatorPreserved=True");
            report.AppendLine("OtherResistanceAnimators=0");
            report.AppendLine("OtherSlotsChanged=False");
            report.AppendLine("PlayerCameraChanged=False");
            report.AppendLine("OtherSceneRootsChanged=False");
            report.AppendLine("SceneChangedByInspection=False");
            report.AppendLine("ReviewImage=" + CapturePath);
            report.AppendLine(
                "ReviewLayout=Columns death 0%,33%,66%,100%, explosion 0.15s,0.40s,0.75s; top Resistance_01 approved static reference, bottom Resistance_06 selected Mixamo death and self-destruct sequence");
            File.WriteAllText(
                Absolute(InspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static Bounds CalculateSequenceReviewBounds(
            ResistanceDeathExplosionLoop sequence,
            SkinnedMeshRenderer deathRenderer,
            AnimationClip clip)
        {
            sequence.SampleAtSequenceTime(0f);
            var bounds = BakedWorldBounds(deathRenderer);
            foreach (var normalized in
                     new[] { 0.33f, 0.66f, 0.9999f })
            {
                sequence.SampleAtSequenceTime(
                    clip.length * normalized);
                bounds.Encapsulate(
                    BakedWorldBounds(deathRenderer));
            }

            var explosionRoot =
                sequence.ExplosionParticles[0].transform.parent ??
                throw new InvalidOperationException(
                    "Resistance death explosion root is unavailable.");
            bounds.Encapsulate(
                new Bounds(
                    explosionRoot.position,
                    Vector3.one *
                    sequence.ExplosionDiameterMeters));
            sequence.ResetSequence();
            return bounds;
        }

        private static void WriteContactSheet(
            Texture2D[] staticFrames,
            Texture2D[] deathFrames)
        {
            var sheet = new Texture2D(
                ReviewImageSize * staticFrames.Length,
                ReviewImageSize * 2,
                TextureFormat.RGBA32,
                false);
            try
            {
                for (var index = 0;
                     index < staticFrames.Length;
                     index++)
                {
                    sheet.SetPixels32(
                        index * ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        ReviewImageSize,
                        staticFrames[index].GetPixels32());
                    sheet.SetPixels32(
                        index * ReviewImageSize,
                        0,
                        ReviewImageSize,
                        ReviewImageSize,
                        deathFrames[index].GetPixels32());
                }

                sheet.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(CapturePath),
                    sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
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
            camera.backgroundColor = new Color(0.64f, 0.69f, 0.74f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << ReviewLayer;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            key.type = LightType.Directional;
            key.intensity = 1.35f;
            key.color = new Color(1f, 0.92f, 0.82f);
            key.cullingMask = 1 << ReviewLayer;
            keyTransform.rotation = Quaternion.Euler(38f, -28f, 0f);
            fill.type = LightType.Directional;
            fill.intensity = 0.75f;
            fill.color = new Color(0.50f, 0.70f, 1f);
            fill.cullingMask = 1 << ReviewLayer;
            fillTransform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void PositionReviewCamera(
            Transform cameraTransform,
            Bounds bounds)
        {
            var target =
                bounds.center +
                Vector3.up * (bounds.extents.y * 0.02f);
            var halfFovRadians = 42f * 0.5f * Mathf.Deg2Rad;
            var distance =
                Mathf.Max(bounds.extents.y, bounds.extents.x) /
                Mathf.Tan(halfFovRadians) +
                bounds.extents.z +
                0.35f;
            cameraTransform.position =
                target + Vector3.back * distance;
            cameraTransform.rotation =
                Quaternion.LookRotation(
                    target - cameraTransform.position,
                    Vector3.up);
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
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(
                    ReviewImageSize,
                    ReviewImageSize,
                    TextureFormat.RGBA32,
                    false);
                texture.ReadPixels(
                    new Rect(
                        0,
                        0,
                        ReviewImageSize,
                        ReviewImageSize),
                    0,
                    0,
                    false);
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

        private static void SampleClip(
            GameObject target,
            AnimationClip clip,
            float time)
        {
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
            }

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(
                target,
                clip,
                time);
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        private static void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static void SetLayerRecursively(
            Transform root,
            int layer)
        {
            foreach (var transform in
                     root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
            }
        }

        private static void DestroyFrames(
            IEnumerable<Texture2D> frames)
        {
            foreach (var frame in frames)
            {
                if (frame != null)
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }
            }
        }

        private static Scene RequireScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Resistance death model work must run in Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
            }

            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be opened as the single active scene.");
            }

            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            var root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate =>
                    candidate.name == PlacementRootName) ??
                throw new InvalidOperationException(
                    "Approved Resistance placement root is missing.");
            if (root.transform.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Approved Resistance placement must contain fourteen slots.");
            }

            return root.transform;
        }

        private static Transform RequireSlot(
            Transform placementRoot,
            string name,
            int siblingIndex)
        {
            var slot = placementRoot.Find(name) ??
                throw new InvalidOperationException(name + " is missing.");
            if (slot.GetSiblingIndex() != siblingIndex)
            {
                throw new InvalidOperationException(
                    name + " sibling index changed.");
            }

            return slot;
        }

        private static Transform RequireDirectChild(
            Transform slot,
            string name)
        {
            if (slot.childCount != 1 ||
                slot.GetChild(0).name != name)
            {
                throw new InvalidOperationException(
                    slot.name +
                    " must contain exactly one direct child named " +
                    name + ".");
            }

            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireSingleSkinnedRenderer(
            Transform root,
            string label)
        {
            var renderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    label +
                    " must contain exactly one SkinnedMeshRenderer. Actual=" +
                    renderers.Length + ".");
            }

            return renderers[0];
        }

        private static Transform FindDescendant(
            Transform root,
            string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == name);
        }

        private static Bounds CombinedBounds(
            Renderer[] renderers)
        {
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Renderer bounds are missing.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static Bounds BakedWorldBounds(
            SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                if (vertices.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Baked Resistance death mesh has no vertices.");
                }

                var bounds = new Bounds(
                    renderer.transform.TransformPoint(vertices[0]),
                    Vector3.zero);
                for (var index = 1;
                     index < vertices.Length;
                     index++)
                {
                    bounds.Encapsulate(
                        renderer.transform.TransformPoint(
                            vertices[index]));
                }

                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static int TriangleCount(Mesh mesh)
        {
            return Enumerable.Range(0, mesh.subMeshCount)
                .Sum(index =>
                    checked((int)mesh.GetIndexCount(index)) / 3);
        }

        private static void RequireSlotTransformUnchanged(
            Transform slot,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (slot.localPosition != position ||
                slot.localRotation != rotation ||
                slot.localScale != scale)
            {
                throw new InvalidOperationException(
                    "Resistance_06 slot transform changed.");
            }
        }

        private static SlotState[] CaptureOtherSlotStates(
            Transform placementRoot,
            Transform moveSlot)
        {
            return placementRoot.Cast<Transform>()
                .Where(slot => slot != moveSlot)
                .Select(SlotState.Capture)
                .ToArray();
        }

        private static void RequireOtherSlotsUnchanged(
            Transform placementRoot,
            Transform moveSlot,
            SlotState[] before)
        {
            var after =
                CaptureOtherSlotStates(placementRoot, moveSlot);
            if (!before.SequenceEqual(after))
            {
                throw new InvalidOperationException(
                    "A Resistance slot outside Resistance_06 changed.");
            }
        }

        private static RootState[] CaptureProtectedRootStates(
            Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root =>
                    root.name != PlacementRootName)
                .Select(RootState.Capture)
                .OrderBy(state => state.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void RequireProtectedRootsUnchanged(
            Scene scene,
            RootState[] before)
        {
            var after = CaptureProtectedRootStates(scene);
            if (!before.SequenceEqual(after))
            {
                throw new InvalidOperationException(
                    "A scene root outside the Resistance placement changed.");
            }
        }

        private static void RequireHash(
            string path,
            string expected)
        {
            var absolute =
                Path.IsPathRooted(path)
                    ? path
                    : Absolute(path);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Required Resistance file is missing.",
                    absolute);
            }

            var actual = Sha256(absolute);
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    "Resistance file hash differs. Path=" +
                    path +
                    ", Expected=" +
                    expected +
                    ", Actual=" +
                    actual + ".");
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return BitConverter.ToString(
                        algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }

        private static string Absolute(
            string projectRelativePath)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    projectRelativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
        }

        private static string Format(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######})",
                value.x,
                value.y,
                value.z);
        }

        private readonly struct BoundsSample
        {
            public BoundsSample(
                float normalizedTime,
                float minY,
                float maxY)
            {
                NormalizedTime = normalizedTime;
                MinY = minY;
                MaxY = maxY;
            }

            public float NormalizedTime { get; }
            public float MinY { get; }
            public float MaxY { get; }
        }

        private readonly struct ExplosionMeasurement
        {
            public ExplosionMeasurement(
                float diameter,
                Vector3 positiveReach,
                Vector3 negativeReach,
                Dictionary<string, float> roleRadii)
            {
                Diameter = diameter;
                PositiveReach = positiveReach;
                NegativeReach = negativeReach;
                RoleRadii = roleRadii;
            }

            public float Diameter { get; }
            public Vector3 PositiveReach { get; }
            public Vector3 NegativeReach { get; }
            public Dictionary<string, float> RoleRadii { get; }
            public float MinimumDirectionalReach => Mathf.Min(
                PositiveReach.x,
                PositiveReach.y,
                PositiveReach.z,
                NegativeReach.x,
                NegativeReach.y,
                NegativeReach.z);
        }

        private readonly struct PlaybackSample
        {
            public PlaybackSample(
                float requestedNormalizedTime,
                float actualNormalizedTime,
                Vector3 leftHand,
                Vector3 rightHand,
                float minY,
                float maxY,
                bool stateMatched,
                bool loop)
            {
                RequestedNormalizedTime =
                    requestedNormalizedTime;
                ActualNormalizedTime =
                    actualNormalizedTime;
                LeftHand = leftHand;
                RightHand = rightHand;
                MinY = minY;
                MaxY = maxY;
                StateMatched = stateMatched;
                Loop = loop;
            }

            public float RequestedNormalizedTime { get; }
            public float ActualNormalizedTime { get; }
            public Vector3 LeftHand { get; }
            public Vector3 RightHand { get; }
            public float MinY { get; }
            public float MaxY { get; }
            public bool StateMatched { get; }
            public bool Loop { get; }
        }

        private readonly struct PlaybackInspection
        {
            public PlaybackInspection(
                PlaybackSample[] samples,
                float maxLeftHandMotion,
                float maxRightHandMotion,
                float minimumGroundY,
                float maximumTopY,
                bool stateLooped)
            {
                Samples = samples;
                MaxLeftHandMotion = maxLeftHandMotion;
                MaxRightHandMotion = maxRightHandMotion;
                MinimumGroundY = minimumGroundY;
                MaximumTopY = maximumTopY;
                StateLooped = stateLooped;
            }

            public PlaybackSample[] Samples { get; }
            public float MaxLeftHandMotion { get; }
            public float MaxRightHandMotion { get; }
            public float MinimumGroundY { get; }
            public float MaximumTopY { get; }
            public bool StateLooped { get; }
        }

        private readonly struct LayerState
        {
            public LayerState(
                GameObject gameObject,
                int layer)
            {
                GameObject = gameObject;
                Layer = layer;
            }

            public GameObject GameObject { get; }
            public int Layer { get; }
        }

        private readonly struct SlotState : IEquatable<SlotState>
        {
            private SlotState(
                string name,
                int siblingIndex,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                bool active,
                int childCount,
                string childName,
                string childPrefabPath,
                string animatorControllers)
            {
                Name = name;
                SiblingIndex = siblingIndex;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Active = active;
                ChildCount = childCount;
                ChildName = childName;
                ChildPrefabPath = childPrefabPath;
                AnimatorControllers = animatorControllers;
            }

            private string Name { get; }
            private int SiblingIndex { get; }
            private Vector3 Position { get; }
            private Quaternion Rotation { get; }
            private Vector3 Scale { get; }
            private bool Active { get; }
            private int ChildCount { get; }
            private string ChildName { get; }
            private string ChildPrefabPath { get; }
            private string AnimatorControllers { get; }

            public static SlotState Capture(Transform slot)
            {
                var child =
                    slot.childCount == 1
                        ? slot.GetChild(0)
                        : null;
                var source =
                    child != null
                        ? PrefabUtility.GetCorrespondingObjectFromSource(
                            child.gameObject)
                        : null;
                var controllers = string.Join(
                    "|",
                    slot.GetComponentsInChildren<Animator>(true)
                        .Select(animator =>
                            animator.runtimeAnimatorController != null
                                ? AssetDatabase.GetAssetPath(
                                    animator.runtimeAnimatorController)
                                : "<none>"));
                return new SlotState(
                    slot.name,
                    slot.GetSiblingIndex(),
                    slot.localPosition,
                    slot.localRotation,
                    slot.localScale,
                    slot.gameObject.activeSelf,
                    slot.childCount,
                    child != null ? child.name : string.Empty,
                    source != null
                        ? AssetDatabase.GetAssetPath(source)
                        : string.Empty,
                    controllers);
            }

            public bool Equals(SlotState other)
            {
                return Name == other.Name &&
                       SiblingIndex == other.SiblingIndex &&
                       Position == other.Position &&
                       Rotation == other.Rotation &&
                       Scale == other.Scale &&
                       Active == other.Active &&
                       ChildCount == other.ChildCount &&
                       ChildName == other.ChildName &&
                       ChildPrefabPath == other.ChildPrefabPath &&
                       AnimatorControllers ==
                       other.AnimatorControllers;
            }

            public override bool Equals(object obj)
            {
                return obj is SlotState other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                return Name != null
                    ? Name.GetHashCode()
                    : 0;
            }
        }

        private readonly struct RootState : IEquatable<RootState>
        {
            private RootState(
                string name,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                bool active,
                int childCount)
            {
                Name = name;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Active = active;
                ChildCount = childCount;
            }

            public string Name { get; }
            private Vector3 Position { get; }
            private Quaternion Rotation { get; }
            private Vector3 Scale { get; }
            private bool Active { get; }
            private int ChildCount { get; }

            public static RootState Capture(GameObject root)
            {
                return new RootState(
                    root.name,
                    root.transform.position,
                    root.transform.rotation,
                    root.transform.localScale,
                    root.activeSelf,
                    root.transform.childCount);
            }

            public bool Equals(RootState other)
            {
                return Name == other.Name &&
                       Position == other.Position &&
                       Rotation == other.Rotation &&
                       Scale == other.Scale &&
                       Active == other.Active &&
                       ChildCount == other.ChildCount;
            }

            public override bool Equals(object obj)
            {
                return obj is RootState other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                return Name != null
                    ? Name.GetHashCode()
                    : 0;
            }
        }
    }
}
