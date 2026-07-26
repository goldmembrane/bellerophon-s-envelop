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

namespace Bellerophon.Editor.ResistanceCargoRunScene
{
    internal static class ResistanceMoveModelReplacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceWalkingFbxPath =
            "D:/Bellerophon2/Bellerophon/enemies model/résistance walking.fbx";
        private const string WalkingFbxPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceWalking.fbx";
        private const string ApprovedFbxPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceApprovedAppearance.fbx";
        private const string WalkingFbxSha256 =
            "E1E8F5925F781ADDFAA29E9B186AF4CD956A29F04D21B5E60801028612871989";
        private const string ApprovedFbxSha256 =
            "84B6A36298F357D59820EF2F05AE9E557E7A5DD2E13B95A5EFEA7F65179248B1";
        private const string PlacementRootName = "Approved Resistance Enemy Placement";
        private const string StaticSlotName = "Resistance_01";
        private const string MoveSlotName = "Resistance_03";
        private const string StaticModelName = "Resistance_Model";
        private const string MoveModelName = "Resistance_Walking_Model";
        private const string SourceMixamoActionName =
            "Armature|mixamo.com|Layer0";
        private const string UnityMixamoTakeName = "mixamo.com";
        private const string ImportedClipName = "Resistance_Walk_Mixamo";
        private const string StateName = "Resistance_Walk_Mixamo";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Resistance/Animations/Resistance_03_Walk_Mixamo.controller";
        private const string WalkingAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Resistance/Models/ResistanceWalkingApprovedAppearanceMesh.asset";
        private const string ValidationFolder =
            "docs/validation/resistance_move_model_2026-07-26";
        private const string InspectionPath =
            ValidationFolder + "/Resistance_03_MoveModel_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Resistance_03_MoveModel_VisualReview.png";
        private const int SlotCount = 14;
        private const int ExpectedAuthoredVertexCount = 3004;
        private const int ExpectedTriangleCount = 6037;
        private const int ExpectedBoneCount = 24;
        private const int ReviewLayer = 30;
        private const int HiddenReviewLayer = 31;
        private const int ReviewImageSize = 512;
        private const float BoundsTolerance = 0.01f;
        private const float GroundTolerance = 0.003f;
        private const float WalkMotionMinimum = 0.02f;

        [MenuItem("Bellerophon/Enemies/Resistance/Apply Move Model Replacement")]
        public static void ApplyResistanceMoveModelReplacement()
        {
            RequireHash(SourceWalkingFbxPath, WalkingFbxSha256);
            CopyWalkingFbxIfNeeded();
            ConfigureWalkingImporter();
            RequireHash(WalkingFbxPath, WalkingFbxSha256);
            RequireHash(ApprovedFbxPath, ApprovedFbxSha256);

            var walkingPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(WalkingFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Resistance walking FBX is missing.",
                    WalkingFbxPath);
            var walkingAssetRenderer =
                RequireSingleSkinnedRenderer(walkingPrefab.transform, "walking FBX asset");
            RequireImportedWalkingGeometry(walkingAssetRenderer);
            var walkingClip = RequireWalkingClip();
            var walkingAvatar = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath)
                .OfType<Avatar>()
                .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Resistance walking FBX did not produce a Generic Avatar.");
            var controller = CreateOrUpdateController(walkingClip);

            var scene = RequireScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot = RequireSlot(placementRoot, StaticSlotName, 0);
            var moveSlot = RequireSlot(placementRoot, MoveSlotName, 2);
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
                    "Resistance_03 must contain exactly one model before replacement.");
            }

            var previousModel = moveSlot.GetChild(0);
            var previousLocalPosition = previousModel.localPosition;
            var previousLocalRotation = previousModel.localRotation;
            var previousLocalScale = previousModel.localScale;
            var previousBounds = CombinedBounds(
                previousModel.GetComponentsInChildren<Renderer>(true));

            var replacement =
                PrefabUtility.InstantiatePrefab(walkingPrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "Resistance walking FBX could not be instantiated.");
            replacement.name = MoveModelName;
            replacement.transform.SetParent(moveSlot, false);
            replacement.transform.SetLocalPositionAndRotation(
                previousLocalPosition,
                previousLocalRotation);
            replacement.transform.localScale = previousLocalScale;

            try
            {
                var walkingRenderer =
                    RequireSingleSkinnedRenderer(
                        replacement.transform,
                        "Resistance_03 walking model");
                RequireMatchingBoneNames(walkingAssetRenderer, walkingRenderer);
                RequireMatchingBoneNames(staticRenderer, walkingRenderer);
                ApplyApprovedAppearance(
                    staticRenderer,
                    walkingAssetRenderer,
                    walkingRenderer);

                var animator = replacement.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = replacement.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.avatar = walkingAvatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

                GroundWalkingCycle(
                    placementRoot,
                    replacement.transform,
                    walkingRenderer,
                    walkingClip);
                FitWalkingHeight(
                    placementRoot,
                    replacement.transform,
                    walkingRenderer,
                    walkingClip,
                    previousBounds.size.y);
                RequireWalkingAppearance(
                    staticRenderer,
                    walkingAssetRenderer,
                    walkingRenderer);
                RequireWalkingAnimator(animator, controller, walkingClip);

                var replacementBounds =
                    CombinedBounds(
                        replacement.GetComponentsInChildren<Renderer>(true));
                if (Mathf.Abs(
                        replacementBounds.size.y -
                        previousBounds.size.y) >
                        BoundsTolerance * 8f)
                {
                    throw new InvalidOperationException(
                        "Resistance walking model height differs from the approved static height. Previous=" +
                        previousBounds.size.y.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        ", Walking=" +
                        replacementBounds.size.y.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) + ".");
                }
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
                    "Resistance_03 replacement did not leave exactly one walking model.");
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
                    "CargoRunMvp could not be saved after Resistance_03 walking replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceWalkingFbxPath, WalkingFbxSha256);
            RequireHash(WalkingFbxPath, WalkingFbxSha256);
            RequireHash(ApprovedFbxPath, ApprovedFbxSha256);
            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "ResistanceMoveModelReplacementApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + MoveSlotName +
                ", Source=" + WalkingFbxPath +
                ", Clip=" + walkingClip.name +
                ", ClipLength=" + walkingClip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", Loop=True" +
                ", ApprovedMeshUnitNormalized=True" +
                ", ApprovedMaterialsDirectReference=True" +
                ", WalkingRigPreserved=True" +
                ", RootMotion=False" +
                ", OtherSlotsUnchanged=True" +
                ", ProtectedRootsUnchanged=True.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Inspect Move Model Replacement")]
        public static void InspectResistanceMoveModelReplacement()
        {
            RequireHash(SourceWalkingFbxPath, WalkingFbxSha256);
            RequireHash(WalkingFbxPath, WalkingFbxSha256);
            RequireHash(ApprovedFbxPath, ApprovedFbxSha256);
            VerifyWalkingImporter();

            var scene = RequireScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot = RequireSlot(placementRoot, StaticSlotName, 0);
            var moveSlot = RequireSlot(placementRoot, MoveSlotName, 2);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            if (moveSlot.childCount == 1 &&
                moveSlot.GetChild(0).name != MoveModelName)
            {
                InspectPreReplacementCompatibility(
                    scene,
                    moveSlot,
                    staticModel);
                return;
            }

            var moveModel = RequireDirectChild(moveSlot, MoveModelName);
            var staticRenderer =
                RequireSingleSkinnedRenderer(staticModel, "Resistance_01 approved model");
            var walkingRenderer =
                RequireSingleSkinnedRenderer(moveModel, "Resistance_03 walking model");
            var walkingAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(WalkingFbxPath) ??
                throw new InvalidOperationException(
                    "Resistance walking FBX asset is missing.");
            var walkingAssetRenderer =
                RequireSingleSkinnedRenderer(
                    walkingAsset.transform,
                    "walking FBX asset");
            var clip = RequireWalkingClip();
            var controller = RequireController();
            var animator = moveModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Resistance_03 walking model has no Animator.");

            RequireImportedWalkingGeometry(walkingAssetRenderer);
            RequireMatchingBoneNames(walkingAssetRenderer, walkingRenderer);
            RequireMatchingBoneNames(staticRenderer, walkingRenderer);
            RequireWalkingAppearance(
                staticRenderer,
                walkingAssetRenderer,
                walkingRenderer);
            RequireWalkingAnimator(animator, controller, clip);
            RequireMovePrefabSource(moveModel);
            RequireExpectedAnimatorDistribution(placementRoot, moveSlot);

            var modelPositionBefore = moveModel.localPosition;
            var slotPositionBefore = moveSlot.localPosition;
            var playback = InspectAnimatorPlayback(
                animator,
                moveModel,
                walkingRenderer,
                clip);
            if (moveModel.localPosition != modelPositionBefore ||
                moveSlot.localPosition != slotPositionBefore)
            {
                throw new InvalidOperationException(
                    "Resistance_03 model or slot position changed during playback inspection.");
            }

            if (playback.MaxFootMotion < WalkMotionMinimum)
            {
                throw new InvalidOperationException(
                    "Mixamo walking clip did not produce visible foot motion. MaxFootMotion=" +
                    playback.MaxFootMotion.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            if (!playback.StateLooped)
            {
                throw new InvalidOperationException(
                    "Resistance walking Animator did not remain in its looping default state.");
            }

            if (playback.MinimumGroundY < placementRoot.position.y - GroundTolerance)
            {
                throw new InvalidOperationException(
                    "Resistance walking cycle penetrates below the placement ground. MinimumGroundY=" +
                    playback.MinimumGroundY.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            Directory.CreateDirectory(Absolute(ValidationFolder));
            WriteInspectionReport(
                walkingAssetRenderer,
                staticRenderer,
                walkingRenderer,
                animator,
                clip,
                controller,
                playback);
            AssetDatabase.Refresh();

            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Resistance move-model inspection dirtied CargoRunMvp unexpectedly.");
            }

            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "ResistanceMoveModelReplacementInspected Result=PASS" +
                ", Target=" + PlacementRootName + "/" + MoveSlotName +
                ", Clip=" + clip.name +
                ", ClipLength=" + clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", MaxFootMotion=" + playback.MaxFootMotion.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", MinimumGroundY=" + playback.MinimumGroundY.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", StateLooped=True" +
                ", ApprovedAppearanceExact=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Resistance/Capture Move Model Review")]
        public static void CaptureResistanceMoveModelReplacementReview()
        {
            var scene = RequireScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot = RequireSlot(placementRoot, StaticSlotName, 0);
            var moveSlot = RequireSlot(placementRoot, MoveSlotName, 2);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            var moveModel = RequireDirectChild(moveSlot, MoveModelName);
            var staticRenderer =
                RequireSingleSkinnedRenderer(staticModel, "Resistance_01 approved model");
            var walkingRenderer =
                RequireSingleSkinnedRenderer(moveModel, "Resistance_03 walking model");
            var walkingAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(WalkingFbxPath) ??
                throw new InvalidOperationException(
                    "Resistance walking FBX asset is missing.");
            var walkingAssetRenderer =
                RequireSingleSkinnedRenderer(
                    walkingAsset.transform,
                    "walking FBX asset");
            var clip = RequireWalkingClip();
            var controller = RequireController();
            var animator = moveModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Resistance_03 walking model has no Animator.");
            RequireWalkingAppearance(
                staticRenderer,
                walkingAssetRenderer,
                walkingRenderer);
            RequireWalkingAnimator(animator, controller, clip);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            var normalizedTimes = new[] { 0f, 0.25f, 0.50f, 0.75f, 1f };
            var staticFrames = new Texture2D[normalizedTimes.Length];
            var walkingFrames = new Texture2D[normalizedTimes.Length];
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
                "Resistance_MoveModel_ReviewCamera",
                typeof(Camera));
            var keyObject = new GameObject(
                "Resistance_MoveModel_KeyLight",
                typeof(Light));
            var fillObject = new GameObject(
                "Resistance_MoveModel_FillLight",
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
                for (var index = 0; index < normalizedTimes.Length; index++)
                {
                    PositionReviewCamera(
                        camera.transform,
                        staticBounds);
                    staticFrames[index] = RenderFrame(camera);
                }

                SetLayerRecursively(staticSlot, HiddenReviewLayer);
                SetLayerRecursively(moveSlot, ReviewLayer);
                for (var index = 0; index < normalizedTimes.Length; index++)
                {
                    SampleClip(
                        moveModel.gameObject,
                        clip,
                        normalizedTimes[index] * clip.length);
                    var walkingBounds = CombinedBounds(
                        moveModel.GetComponentsInChildren<Renderer>(true));
                    PositionReviewCamera(
                        camera.transform,
                        walkingBounds);
                    walkingFrames[index] = RenderFrame(camera);
                }

                WriteContactSheet(staticFrames, walkingFrames);
            }
            finally
            {
                StopSampling();
                foreach (var layerState in layerStates)
                {
                    layerState.GameObject.layer = layerState.Layer;
                }

                DestroyFrames(staticFrames);
                DestroyFrames(walkingFrames);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }

            AssetDatabase.Refresh();
            if (!sceneWasDirty && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Resistance move-model review capture dirtied CargoRunMvp unexpectedly.");
            }

            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "ResistanceMoveModelReplacementReviewCaptured Result=PASS" +
                ", Target=" + PlacementRootName + "/" + MoveSlotName +
                ", Reference=" + StaticSlotName +
                ", Checkpoints=0|25|50|75|100" +
                ", Output=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static void CopyWalkingFbxIfNeeded()
        {
            if (File.Exists(Absolute(WalkingFbxPath)) &&
                Sha256(Absolute(WalkingFbxPath)) == WalkingFbxSha256)
            {
                return;
            }

            File.Copy(
                SourceWalkingFbxPath,
                Absolute(WalkingFbxPath),
                true);
            AssetDatabase.ImportAsset(
                WalkingFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureWalkingImporter()
        {
            var importer =
                AssetImporter.GetAtPath(WalkingFbxPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Resistance walking ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.animationWrapMode = WrapMode.Loop;

            var sourceClips = importer.defaultClipAnimations;
            var mixamoClip = sourceClips.SingleOrDefault(candidate =>
                candidate.name == UnityMixamoTakeName ||
                candidate.takeName == UnityMixamoTakeName);
            if (mixamoClip == null)
            {
                throw new InvalidOperationException(
                    "Resistance walking FBX is missing the selected Mixamo take: " +
                    SourceMixamoActionName + " / " +
                    UnityMixamoTakeName + ". Available=" +
                    string.Join(
                        "|",
                        sourceClips.Select(candidate =>
                            candidate.name + "[" + candidate.takeName + "]")));
            }

            mixamoClip.name = ImportedClipName;
            mixamoClip.wrapMode = WrapMode.Loop;
            mixamoClip.loopTime = true;
            mixamoClip.loopPose = true;
            importer.clipAnimations = new[] { mixamoClip };
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                WalkingFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            VerifyWalkingImporter();
        }

        private static void VerifyWalkingImporter()
        {
            var importer =
                AssetImporter.GetAtPath(WalkingFbxPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Resistance walking ModelImporter is missing.");
            if (!importer.importAnimation ||
                importer.animationType != ModelImporterAnimationType.Generic ||
                importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                importer.optimizeGameObjects ||
                importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                throw new InvalidOperationException(
                    "Resistance walking FBX importer contract differs.");
            }

            var clips = importer.clipAnimations;
            if (clips.Length != 1 ||
                clips[0].name != ImportedClipName ||
                clips[0].takeName != UnityMixamoTakeName ||
                !clips[0].loopTime ||
                !clips[0].loopPose ||
                clips[0].wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(
                    "Resistance walking FBX must import only the selected looping Mixamo take.");
            }
        }

        private static AnimationClip RequireWalkingClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(WalkingFbxPath)
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
                    "Resistance walking FBX did not import exactly one selected Mixamo clip. Imported=" +
                    string.Join("|", clips.Select(candidate => candidate.name)));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime || !clips[0].isLooping)
            {
                throw new InvalidOperationException(
                    "Selected Resistance Mixamo clip is not looping.");
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
                    "Resistance walking AnimatorController is missing.");
        }

        private static void ApplyApprovedAppearance(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer walkingAssetRenderer,
            SkinnedMeshRenderer walkingRenderer)
        {
            walkingRenderer.sharedMesh =
                CreateOrUpdateWalkingAppearanceMesh(
                    staticRenderer.sharedMesh,
                    walkingAssetRenderer.sharedMesh);
            walkingRenderer.sharedMaterials =
                staticRenderer.sharedMaterials.ToArray();
            walkingRenderer.updateWhenOffscreen = true;
            EditorUtility.SetDirty(walkingRenderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                walkingRenderer);
        }

        private static Mesh CreateOrUpdateWalkingAppearanceMesh(
            Mesh approvedMesh,
            Mesh walkingMesh)
        {
            if (approvedMesh == null || walkingMesh == null)
            {
                throw new InvalidOperationException(
                    "Resistance appearance mesh sources are missing.");
            }

            var scale = RequireUniformUnitScale(
                approvedMesh.bounds,
                walkingMesh.bounds);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(
                    WalkingAppearanceMeshPath) != null)
            {
                AssetDatabase.DeleteAsset(
                    WalkingAppearanceMeshPath);
            }

            var generated =
                UnityEngine.Object.Instantiate(approvedMesh);
            generated.name =
                "ResistanceWalkingApprovedAppearanceMesh";
            var vertices = generated.vertices;
            for (var index = 0;
                 index < vertices.Length;
                 index++)
            {
                vertices[index] *= scale;
            }

            generated.vertices = vertices;
            generated.bindposes = walkingMesh.bindposes;
            generated.RecalculateBounds();
            AssetDatabase.CreateAsset(
                generated,
                WalkingAppearanceMeshPath);
            EditorUtility.SetDirty(generated);
            AssetDatabase.SaveAssets();
            return generated;
        }

        private static float RequireUniformUnitScale(
            Bounds approvedBounds,
            Bounds walkingBounds)
        {
            var scaleX =
                walkingBounds.size.x / approvedBounds.size.x;
            var scaleY =
                walkingBounds.size.y / approvedBounds.size.y;
            var scaleZ =
                walkingBounds.size.z / approvedBounds.size.z;
            if (Mathf.Abs(scaleX - scaleY) > 0.000001f ||
                Mathf.Abs(scaleX - scaleZ) > 0.000001f ||
                Mathf.Abs(scaleY - 0.01f) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Resistance approved/walking FBX unit ratio differs. Ratio=" +
                    Format(
                        new Vector3(
                            scaleX,
                            scaleY,
                            scaleZ)));
            }

            return scaleY;
        }

        private static void GroundWalkingCycle(
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
                    "Resistance walking cycle could not be grounded. Ground=" +
                    groundY.ToString("0.######", CultureInfo.InvariantCulture) +
                    ", Minimum=" +
                    groundedMinimum.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }
        }

        private static void FitWalkingHeight(
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
                    "Resistance walking model height is invalid.");
            }

            var scaleFactor = targetHeight / currentHeight;
            if (scaleFactor < 0.5f ||
                scaleFactor > 2f)
            {
                throw new InvalidOperationException(
                    "Resistance walking height scale factor is outside the safe range. Factor=" +
                    scaleFactor.ToString(
                        "0.######",
                        CultureInfo.InvariantCulture));
            }

            moveModel.localScale *= scaleFactor;
            EditorUtility.SetDirty(moveModel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                moveModel);
            GroundWalkingCycle(
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

        private static PlaybackInspection InspectAnimatorPlayback(
            Animator animator,
            Transform moveModel,
            SkinnedMeshRenderer renderer,
            AnimationClip clip)
        {
            var normalizedTimes =
                new[] { 0f, 0.25f, 0.50f, 0.75f, 1f, 1.25f };
            var samples = new PlaybackSample[normalizedTimes.Length];
            var leftFoot = FindDescendant(moveModel, "LeftFoot") ??
                throw new InvalidOperationException(
                    "Resistance walking rig is missing LeftFoot.");
            var rightFoot = FindDescendant(moveModel, "RightFoot") ??
                throw new InvalidOperationException(
                    "Resistance walking rig is missing RightFoot.");
            var fullStateHash =
                Animator.StringToHash("Base Layer." + StateName);
            var modelPosition = moveModel.localPosition;
            var previousNormalizedTime = 0f;
            var stateLooped = true;
            try
            {
                animator.Rebind();
                animator.Play(fullStateHash, 0, 0f);
                animator.Update(0f);
                for (var index = 0;
                     index < normalizedTimes.Length;
                     index++)
                {
                    var requested = normalizedTimes[index];
                    if (index > 0)
                    {
                        animator.Update(
                            (requested - previousNormalizedTime) *
                            clip.length);
                    }

                    var stateInfo =
                        animator.GetCurrentAnimatorStateInfo(0);
                    var stateMatched =
                        stateInfo.IsName(StateName) ||
                        stateInfo.IsName("Base Layer." + StateName);
                    stateLooped &=
                        stateMatched &&
                        stateInfo.loop &&
                        (index == 0 ||
                         stateInfo.normalizedTime >
                         samples[index - 1].ActualNormalizedTime);
                    var bakedBounds =
                        BakedWorldBounds(renderer);
                    samples[index] = new PlaybackSample(
                        requested,
                        stateInfo.normalizedTime,
                        leftFoot.position,
                        rightFoot.position,
                        bakedBounds.min.y,
                        bakedBounds.max.y,
                        stateMatched,
                        stateInfo.loop);
                    previousNormalizedTime = requested;
                }
            }
            finally
            {
                animator.Rebind();
                animator.Play(fullStateHash, 0, 0f);
                animator.Update(0f);
            }

            if (moveModel.localPosition != modelPosition)
            {
                throw new InvalidOperationException(
                    "Resistance walking Animator changed the model root position.");
            }

            var leftOrigin = samples[0].LeftFoot;
            var rightOrigin = samples[0].RightFoot;
            var maxFootMotion = samples.Max(sample =>
                Mathf.Max(
                    Vector3.Distance(leftOrigin, sample.LeftFoot),
                    Vector3.Distance(rightOrigin, sample.RightFoot)));
            return new PlaybackInspection(
                samples,
                maxFootMotion,
                samples.Min(sample => sample.MinY),
                samples.Max(sample => sample.MaxY),
                stateLooped);
        }

        private static void RequireWalkingAnimator(
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
                    "Resistance walking Animator configuration differs.");
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Resistance walking controller must contain one layer.");
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
                    "Resistance walking controller default state differs.");
            }
        }

        private static void RequireImportedWalkingGeometry(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Imported Resistance walking mesh is missing.");
            var triangles = TriangleCount(mesh);
            if (triangles != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "Imported Resistance walking geometry differs. Triangles=" +
                    triangles +
                    ", Bones=" +
                    renderer.bones.Length + ".");
            }

            var authoredVertexCount = ReadAuthoredVertexCount();
            if (authoredVertexCount != ExpectedAuthoredVertexCount)
            {
                throw new InvalidOperationException(
                    "Resistance walking FBX authored vertex count differs. Actual=" +
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

        private static void RequireWalkingAppearance(
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer walkingAssetRenderer,
            SkinnedMeshRenderer walkingRenderer)
        {
            RequireGeneratedAppearanceMesh(
                staticRenderer.sharedMesh,
                walkingAssetRenderer.sharedMesh,
                walkingRenderer.sharedMesh);

            if (!walkingRenderer.sharedMaterials.SequenceEqual(
                    staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "Resistance_03 does not directly reference all Resistance_01 approved materials.");
            }

        }

        private static void RequireGeneratedAppearanceMesh(
            Mesh approvedMesh,
            Mesh walkingSourceMesh,
            Mesh generatedMesh)
        {
            var expected =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    WalkingAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "Resistance walking approved appearance mesh asset is missing.");
            if (generatedMesh != expected)
            {
                throw new InvalidOperationException(
                    "Resistance_03 does not reference the generated approved walking appearance mesh.");
            }

            if (generatedMesh.vertexCount != approvedMesh.vertexCount ||
                generatedMesh.subMeshCount != approvedMesh.subMeshCount ||
                TriangleCount(generatedMesh) != TriangleCount(approvedMesh))
            {
                throw new InvalidOperationException(
                    "Resistance walking approved appearance topology differs.");
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
                        "Resistance walking approved appearance triangle order differs.");
                }
            }

            var approvedUv = approvedMesh.uv;
            var generatedUv = generatedMesh.uv;
            if (approvedUv.Length != generatedUv.Length)
            {
                throw new InvalidOperationException(
                    "Resistance walking approved appearance UV count differs.");
            }

            for (var index = 0;
                 index < approvedUv.Length;
                 index++)
            {
                if (approvedUv[index] != generatedUv[index])
                {
                    throw new InvalidOperationException(
                        "Resistance walking approved appearance UV data differs.");
                }
            }

            var scale = RequireUniformUnitScale(
                approvedMesh.bounds,
                walkingSourceMesh.bounds);
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
                        "Resistance walking approved appearance vertex unit conversion differs.");
                }
            }

            var walkingBindposes =
                walkingSourceMesh.bindposes;
            var generatedBindposes =
                generatedMesh.bindposes;
            if (walkingBindposes.Length !=
                generatedBindposes.Length)
            {
                throw new InvalidOperationException(
                    "Resistance walking approved appearance bindpose count differs.");
            }

            for (var index = 0;
                 index < walkingBindposes.Length;
                 index++)
            {
                if (walkingBindposes[index] !=
                    generatedBindposes[index])
                {
                    throw new InvalidOperationException(
                        "Resistance walking approved appearance bindposes differ from the walking FBX.");
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
                    "Resistance walking rig bone order differs from the approved rig.");
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
            if (sourcePath != WalkingFbxPath)
            {
                throw new InvalidOperationException(
                    "Resistance_03 is not an instance of the supplied walking FBX. Source=" +
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
                            "Resistance_03 must contain exactly one configured walking Animator.");
                    }
                }
                else if (slot.name == "Resistance_02")
                {
                    if (configured.Length != 1)
                    {
                        throw new InvalidOperationException(
                            "Resistance_02 idle Animator was not preserved.");
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
            var walkingPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(WalkingFbxPath) ??
                throw new InvalidOperationException(
                    "Resistance walking FBX asset is missing.");
            var clip = RequireWalkingClip();
            var previousModel = moveSlot.GetChild(0);
            var temporary =
                PrefabUtility.InstantiatePrefab(
                    walkingPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "Temporary Resistance walking compatibility instance could not be created.");
            temporary.name = "Resistance_Walking_Compatibility_Temporary";
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
                var walkingRenderer =
                    RequireSingleSkinnedRenderer(
                        temporary.transform,
                        "temporary walking model");
                var originalMesh =
                    walkingRenderer.sharedMesh ??
                    throw new InvalidOperationException(
                        "Temporary walking renderer mesh is missing.");
                var originalLocalBounds = walkingRenderer.localBounds;
                var originalWorldBounds = walkingRenderer.bounds;
                var originalBakedBounds =
                    BakedWorldBounds(walkingRenderer);
                var originalMeshBounds = originalMesh.bounds;
                var rendererScale =
                    walkingRenderer.transform.lossyScale;
                var rendererPath =
                    AnimationUtility.CalculateTransformPath(
                        walkingRenderer.transform,
                        temporary.transform);

                walkingRenderer.sharedMesh =
                    staticRenderer.sharedMesh;
                var approvedNeutralBounds =
                    walkingRenderer.bounds;
                var approvedNeutralBakedBounds =
                    BakedWorldBounds(walkingRenderer);
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
                            Format(walkingRenderer.bounds.size) +
                            ",Baked=" +
                            Format(BakedWorldBounds(walkingRenderer).size) +
                            "@" +
                            Format(BakedWorldBounds(walkingRenderer).center));
                    }
                }
                finally
                {
                    StopSampling();
                }

                Debug.Log(
                    "ResistanceMovePreReplacementCompatibility Result=INFO" +
                    ", RendererPath=" + rendererPath +
                    ", RendererLossyScale=" + Format(rendererScale) +
                    ", RendererLocalScale=" +
                    Format(walkingRenderer.transform.localScale) +
                    ", StaticRendererLossyScale=" +
                    Format(staticRenderer.transform.lossyScale) +
                    ", StaticRendererLocalScale=" +
                    Format(staticRenderer.transform.localScale) +
                    ", WalkingRootBoneLossyScale=" +
                    Format(walkingRenderer.rootBone.lossyScale) +
                    ", StaticRootBoneLossyScale=" +
                    Format(staticRenderer.rootBone.lossyScale) +
                    ", WalkingMeshBoundsSize=" + Format(originalMeshBounds.size) +
                    ", WalkingLocalBoundsSize=" + Format(originalLocalBounds.size) +
                    ", WalkingNeutralWorldBoundsSize=" + Format(originalWorldBounds.size) +
                    ", WalkingNeutralBakedBoundsSize=" +
                    Format(originalBakedBounds.size) +
                    ", ApprovedMeshBoundsSize=" +
                    Format(staticRenderer.sharedMesh.bounds.size) +
                    ", ApprovedLocalBoundsSize=" +
                    Format(staticRenderer.localBounds.size) +
                    ", ApprovedNeutralOnWalkingRigWorldBoundsSize=" +
                    Format(approvedNeutralBounds.size) +
                    ", ApprovedNeutralOnWalkingRigBakedBoundsSize=" +
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
            SkinnedMeshRenderer walkingAssetRenderer,
            SkinnedMeshRenderer staticRenderer,
            SkinnedMeshRenderer walkingRenderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            PlaybackInspection playback)
        {
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + MoveSlotName);
            report.AppendLine("StaticReference=" + StaticSlotName + "/" + StaticModelName);
            report.AppendLine("SourceFbx=" + SourceWalkingFbxPath);
            report.AppendLine("ProjectFbx=" + WalkingFbxPath);
            report.AppendLine("SourceFbxSha256=" + WalkingFbxSha256);
            report.AppendLine("ProjectFbxSha256=" + WalkingFbxSha256);
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
            report.AppendLine("ImportedWalkingRenderVertices=" +
                              walkingAssetRenderer.sharedMesh.vertexCount);
            report.AppendLine("AuthoredVertices=" + ExpectedAuthoredVertexCount);
            report.AppendLine("Triangles=" + ExpectedTriangleCount);
            report.AppendLine("Bones=" + walkingRenderer.bones.Length);
            report.AppendLine("ApprovedRenderVertices=" +
                              staticRenderer.sharedMesh.vertexCount);
            report.AppendLine("ApprovedMaterialSlots=" +
                              staticRenderer.sharedMaterials.Length);
            report.AppendLine("ApprovedAppearanceMesh=" +
                              WalkingAppearanceMeshPath);
            report.AppendLine("ApprovedMeshUnitScale=0.01");
            report.AppendLine("ApprovedTopologyAndUvCopiedExactly=True");
            report.AppendLine("WalkingBindposesPreserved=True");
            report.AppendLine("ApprovedMaterialsDirectReference=" +
                              walkingRenderer.sharedMaterials.SequenceEqual(
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
                    ",LeftFoot:" +
                    Format(sample.LeftFoot) +
                    ",RightFoot:" +
                    Format(sample.RightFoot) +
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

            report.AppendLine("MaxFootMotion=" +
                              playback.MaxFootMotion.ToString(
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
            report.AppendLine("AnimatorStateLooped=" + playback.StateLooped);
            report.AppendLine("WalkingFbxBytesChanged=False");
            report.AppendLine("WalkingRigChanged=False");
            report.AppendLine("WalkingAnimationKeysChanged=False");
            report.AppendLine("Resistance02IdleAnimatorPreserved=True");
            report.AppendLine("OtherResistanceAnimators=0");
            report.AppendLine("OtherSlotsChanged=False");
            report.AppendLine("PlayerCameraChanged=False");
            report.AppendLine("OtherSceneRootsChanged=False");
            report.AppendLine("SceneChangedByInspection=False");
            report.AppendLine("ReviewImage=" + CapturePath);
            report.AppendLine(
                "ReviewLayout=Columns 0%,25%,50%,75%,100%; top Resistance_01 approved static reference, bottom Resistance_03 selected Mixamo walking playback");
            File.WriteAllText(
                Absolute(InspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteContactSheet(
            Texture2D[] staticFrames,
            Texture2D[] walkingFrames)
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
                        walkingFrames[index].GetPixels32());
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
                    "Resistance move-model work must run in Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
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
                        "Baked Resistance walking mesh has no vertices.");
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
                    "Resistance_03 slot transform changed.");
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
                    "A Resistance slot outside Resistance_03 changed.");
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

        private readonly struct PlaybackSample
        {
            public PlaybackSample(
                float requestedNormalizedTime,
                float actualNormalizedTime,
                Vector3 leftFoot,
                Vector3 rightFoot,
                float minY,
                float maxY,
                bool stateMatched,
                bool loop)
            {
                RequestedNormalizedTime =
                    requestedNormalizedTime;
                ActualNormalizedTime =
                    actualNormalizedTime;
                LeftFoot = leftFoot;
                RightFoot = rightFoot;
                MinY = minY;
                MaxY = maxY;
                StateMatched = stateMatched;
                Loop = loop;
            }

            public float RequestedNormalizedTime { get; }
            public float ActualNormalizedTime { get; }
            public Vector3 LeftFoot { get; }
            public Vector3 RightFoot { get; }
            public float MinY { get; }
            public float MaxY { get; }
            public bool StateMatched { get; }
            public bool Loop { get; }
        }

        private readonly struct PlaybackInspection
        {
            public PlaybackInspection(
                PlaybackSample[] samples,
                float maxFootMotion,
                float minimumGroundY,
                float maximumTopY,
                bool stateLooped)
            {
                Samples = samples;
                MaxFootMotion = maxFootMotion;
                MinimumGroundY = minimumGroundY;
                MaximumTopY = maximumTopY;
                StateLooped = stateLooped;
            }

            public PlaybackSample[] Samples { get; }
            public float MaxFootMotion { get; }
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
