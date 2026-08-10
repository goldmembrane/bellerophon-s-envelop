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
    internal static class IspantChangingToSwordAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string StaticModelName = "Ispant_Model";
        private const string ReferenceSlotName = "Ispant_07_BreakthroughMusketAimFire";
        private const string ReferenceModelName = "Ispant_Firing_Model";
        private const string StaticPoseSlotName = "Ispant_06_SheathSwordDrawMusket";
        private const string StaticPoseModelName = "Ispant_SheathSword_Model";
        private const string DrawSwordSlotName = "Ispant_04_DrawSword";
        private const string DrawSwordModelName = "Ispant_DrawSword_Model";
        private const string TargetSlotName = "Ispant_08_StowMusketDrawSword";
        private const string TargetModelName = "Ispant_ChangingToSword_Model";
        private const string OriginalTargetModelName = "Ispant_Model";
        private const string AppearanceRootName = "Ispant_StaticAppearance";
        private const string ExactStaticHoldRootName = "Ispant_08_ExactStaticHold";
        private const string IntermediateBackMusketName = "Ispant_08_IntermediateBackMusket";
        private const string SourceBackMusketName = "Ispant_Sheath_RigidMusket";
        private const string HandMusketRootName = "Ispant_Firing_HandMusket";
        private const string HandMusketRendererName = "Ispant_Firing_HandMusket_Renderer";
        private const string WaistSwordRootName = "Ispant_ApprovedLongSword_LeftWaist";
        private const string WaistSwordRendererName = "Ispant_ApprovedLongSword_LeftWaist_Renderer";
        private const string HandSwordRootName = "Ispant_ApprovedLongSword";
        private const string HandSwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string MuzzleFlashPivotName = "Ispant_Firing_MuzzleFlash_Pivot";
        private const string MuzzleFlashName = "Ispant_Firing_MuzzleFlash";
        private const string SourceFbxPath = "enemies model/išpant changing to sword.fbx";
        private const string ProjectFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_ChangingToSword.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_08_ChangingToSword.controller";
        private const string DrawSwordClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_04_DrawSword_0_9m_Upward.anim";
        private const string ReferenceControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_07_Firing.controller";
        private const string InspectionPath =
            "docs/validation/ispant_changing_to_sword_2026-08-10/Ispant_08_ChangingToSword_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_changing_to_sword_2026-08-10/Ispant_08_ChangingToSword_FinalReview.png";
        private const string DiagnosticCapturePath =
            "docs/validation/ispant_changing_to_sword_2026-08-10/Ispant_08_RearArm_Diagnostic.png";
        private const string SourceSha256 =
            "90027F34321AF766487DE422B9342ED8239CBCBD9B6B524CB60B3344A1CE0D7F";
        private const string ImportedClipName = "Ispant_ChangingToSword_Mixamo";
        private const string RetargetedClipName = "Ispant_ChangingToSword_Mixamo_Retargeted";
        private const string TransitionBridgeClipName = "Ispant_08_To_04_DrawSword_Bridge";
        private const string DrawContinuationClipName = "Ispant_04_DrawSword_Continuation";
        private const string ContinuousSequenceClipName =
            "Ispant_08_StowMusketDrawSword_ContinuousSequence";
        private const string StateName = "Ispant_ChangingToSword_Mixamo";
        private const string TransitionBridgeStateName = "Ispant_08_To_04_DrawSword_Bridge";
        private const string DrawContinuationStateName = "Ispant_04_DrawSword";
        // User removed the separate hand-lowered hold; the bridge begins at hand-lowering end.
        private const float LoweredPoseHoldDuration = 0f;
        // User-specified duration for the actual-pose bridge between slot 8 and slot 4.
        private const float TransitionBridgeDuration = 0.3f;
        // User requires the slot-4 draw-sword motion to continue from its original first frame.
        private const int DrawContinuationSourceStartFrame = 0;
        // User-specified visibility timing: frame 139 is the final right-hand-musket frame,
        // and frame 140 switches to the exact back-musket renderer.
        private const int DirectlyObservedRearArmEndFrame = 139;
        // Directly observed intermediate sample where the empty right arm is returning forward.
        private const int DirectlyObservedForwardArmReturnMidFrame = 170;
        // Directly observed in the same contact sheet: the original right arm has returned by frame 180.
        private const int DirectlyObservedForwardArmEndFrame = 180;
        // Directly observed final standing-return sample in the approved 0..230 contact sheet.
        private const int DirectlyObservedStaticReturnSourceEndFrame = 230;
        // Previously approved duration for the source animation's hand-lowering range.
        private const float HandLoweringDuration = 0.4f;
        private const float MatrixTolerance = 0.0001f;
        private const int ExpectedSlots = 12;

        private static readonly string[] StaticAppearanceRendererNames =
        {
            "Ispant_Armed_Body",
            "Ispant_Crescent_Ornament",
            "Ispant_Reference_Eye_Slits"
        };

        private static readonly string[] ReferencePoseBoneNames =
        {
            "Armature",
            "Hips",
            "Spine",
            "Spine1",
            "Spine2",
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm",
            "LeftHand",
            "RightShoulder",
            "RightArm",
            "RightForeArm",
            "RightHand"
        };

        private static readonly string[] ReferenceTranslationBoneNames =
        {
            "Armature",
            "Hips",
            "Spine",
            "Spine1",
            "Spine2",
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm",
            "LeftHand",
            "RightShoulder",
            "RightArm",
            "RightForeArm",
            "RightHand"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 8 Changing To Sword Replacement")]
        public static void ApplyIspant08ChangingToSwordReplacement()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            _ = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                throw new InvalidOperationException("The supplied Ispant changing-to-sword FBX is unavailable.");
            var sourceClip = RequireImportedClip();
            var referenceClip = RequireReferenceClip();
            var drawSwordClip = RequireDrawSwordClip();

            var scene = RequireScene(requireClean: false);
            var resumedKnownFailedApply = ClearKnownFailedApplyDirtyState(scene);
            if (!resumedKnownFailedApply)
                scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var referenceModel = RequireDirectChild(
                RequireSlot(placement.transform, ReferenceSlotName, 6), ReferenceModelName);
            var staticPoseModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticPoseSlotName, 5), StaticPoseModelName);
            var drawSwordModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var targetSlot = RequireSlot(placement.transform, TargetSlotName, 7);
            if (targetSlot.childCount != 1)
                throw new InvalidOperationException("Ispant slot 8 must contain exactly one model before replacement.");

            var otherSlotsBefore = OtherSlotSignatures(placement.transform, targetSlot);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var slotSnapshot = new TransformSnapshot(targetSlot);
            var previous = targetSlot.GetChild(0);
            var replacement = UnityEngine.Object.Instantiate(referenceModel.gameObject);
            SceneManager.MoveGameObjectToScene(replacement, scene);
            replacement.name = TargetModelName;
            replacement.transform.SetParent(targetSlot, false);
            CopyLocalTransform(referenceModel, replacement.transform);
            Metrics metrics;

            try
            {
                RemoveFiringEffect(replacement.transform);
                ConfigureAnimatedHoldAppearance(
                    staticPoseModel, drawSwordModel, replacement.transform);
                var controller = CreateOrUpdateController(
                    sourceClip,
                    referenceClip,
                    drawSwordClip,
                    drawSwordModel,
                    replacement.transform);
                var retargetedClip = RequireRetargetedClip(controller);
                var transitionBridgeClip = RequireTransitionBridgeClip(controller);
                var drawContinuationClip = RequireDrawContinuationClip(controller);
                var animator = ConfigureAnimator(replacement.transform, controller);
                metrics = InspectModel(
                    staticModel,
                    referenceModel,
                    staticPoseModel,
                    drawSwordModel,
                    replacement.transform,
                    animator,
                    sourceClip,
                    referenceClip,
                    retargetedClip,
                    transitionBridgeClip,
                    drawSwordClip,
                    drawContinuationClip,
                    controller);
                WriteInspection(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                if (targetSlot.childCount == 1 && targetSlot.GetChild(0) == previous &&
                    slotSnapshot.Matches(MatrixTolerance) &&
                    otherSlotsBefore.SequenceEqual(OtherSlotSignatures(placement.transform, targetSlot)) &&
                    otherRootsBefore.SequenceEqual(OtherRootSignatures(scene, placement)) &&
                    !EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not clear the verified failed slot-8 temporary replacement state.");
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (targetSlot.childCount != 1 || targetSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The slot-8 replacement did not leave exactly one model.");
            if (!slotSnapshot.Matches(MatrixTolerance))
                throw new InvalidOperationException("The slot-8 placement transform changed during replacement.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, targetSlot),
                "An Ispant slot outside slot 8 changed during replacement.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed during replacement.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(targetSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot-8 replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = targetSlot.gameObject;
            Debug.Log(
                "Ispant08ChangingToSwordReplacementApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + TargetSlotName +
                ", Source=" + ProjectFbxPath +
                ", Clip=" + ImportedClipName +
                ", Loop=True, PlaybackSpeed=1, LoweredPoseHoldSeconds=" + Num(LoweredPoseHoldDuration) +
                ", DrawContinuationSource=Ispant_04_DrawSword" +
                ", DrawContinuationSeconds=" + Num(metrics.DrawContinuationLength) +
                ", TransitionBridgeSeconds=" + Num(metrics.TransitionBridgeLength) +
                ", SequenceCycleSeconds=" + Num(metrics.SequenceCycleLength) +
                ", StaticAppearanceSource=Ispant_01_Static" +
                ", MusketAndArmPoseSource=Ispant_07" +
                ", RearArmMotionEndFrame=" + metrics.RearArmMotionEndFrame +
                ", BackMusketAttachFrame=" + metrics.BackMusketAttachFrame +
                ", ForwardArmMotionEndFrame=" + metrics.ForwardArmMotionEndFrame +
                ", HandLoweringSeconds=" + Num(metrics.HandLoweringSeconds) +
                ", HandMusketVisibleAfterAttach=False" +
                ", LeftWaistSwordSource=Ispant_07" +
                ", LeftWaistSwordParent=mixamorig:Hips" +
                ", StaticModelRendererTransition=False" +
                ", FinalBackMusketSource=Ispant_06_SheathSwordDrawMusket" +
                ", OtherSlotsChanged=False, OtherRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 8 Changing To Sword Replacement")]
        public static void InspectIspant08ChangingToSwordReplacement()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var referenceModel = RequireDirectChild(
                RequireSlot(placement.transform, ReferenceSlotName, 6), ReferenceModelName);
            var staticPoseModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticPoseSlotName, 5), StaticPoseModelName);
            var drawSwordModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 7), TargetModelName);
            var animator = targetModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("The slot-8 Animator is missing.");
            var sourceClip = RequireImportedClip();
            var referenceClip = RequireReferenceClip();
            var drawSwordClip = RequireDrawSwordClip();
            var controller = RequireController();
            var retargetedClip = RequireRetargetedClip(controller);
            var transitionBridgeClip = RequireTransitionBridgeClip(controller);
            var drawContinuationClip = RequireDrawContinuationClip(controller);
            var metrics = InspectModel(
                staticModel,
                referenceModel,
                staticPoseModel,
                drawSwordModel,
                targetModel,
                animator,
                sourceClip,
                referenceClip,
                retargetedClip,
                transitionBridgeClip,
                drawSwordClip,
                drawContinuationClip,
                controller);
            WriteInspection(metrics);
            if (EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Unity reports script compilation errors.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-8 inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant08ChangingToSwordReplacementInspected Result=PASS" +
                ", ClipLength=" + Num(metrics.ClipLength) +
                ", Loop=True, PlaybackSpeed=1" +
                ", DrawContinuationSeconds=" + Num(metrics.DrawContinuationLength) +
                ", TransitionBridgeSeconds=" + Num(metrics.TransitionBridgeLength) +
                ", SequenceCycleSeconds=" + Num(metrics.SequenceCycleLength) +
                ", AppearanceAssetMismatch=" + Num(metrics.AppearanceAssetMismatch) +
                ", MusketMountError=" + Num(metrics.MusketMountError) +
                ", RearArmMotionEndFrame=" + metrics.RearArmMotionEndFrame +
                ", BackMusketAttachFrame=" + metrics.BackMusketAttachFrame +
                ", ForwardArmMotionEndFrame=" + metrics.ForwardArmMotionEndFrame +
                ", HandLoweringSeconds=" + Num(metrics.HandLoweringSeconds) +
                ", HandMusketVisibleAfterAttach=False" +
                ", FinalBackMusketMountError=" + Num(metrics.FinalBackMusketMountError) +
                ", LoweredPoseHoldSeconds=" + Num(metrics.LoweredPoseHoldSeconds) +
                ", WaistSwordBodyFollowRotationDegrees=" +
                Num(metrics.WaistSwordBodyFollowRotationChange) +
                ", LeftShoulderStartHeightError=" + Num(metrics.LeftShoulderStartHeightError) +
                ", RightShoulderStartHeightError=" + Num(metrics.RightShoulderStartHeightError) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 8 Changing To Sword Review")]
        public static void CaptureIspant08ChangingToSwordReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var referenceModel = RequireDirectChild(
                RequireSlot(placement.transform, ReferenceSlotName, 6), ReferenceModelName);
            var staticPoseModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticPoseSlotName, 5), StaticPoseModelName);
            var drawSwordModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 7), TargetModelName);
            var sourceClip = RequireImportedClip();
            var referenceClip = RequireReferenceClip();
            var drawSwordClip = RequireDrawSwordClip();
            var controller = RequireController();
            var retargetedClip = RequireRetargetedClip(controller);
            var transitionBridgeClip = RequireTransitionBridgeClip(controller);
            var drawContinuationClip = RequireDrawContinuationClip(controller);
            var animator = targetModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("The slot-8 Animator is missing.");
            var metrics = InspectModel(
                staticModel,
                referenceModel,
                staticPoseModel,
                drawSwordModel,
                targetModel,
                animator,
                sourceClip,
                referenceClip,
                retargetedClip,
                transitionBridgeClip,
                drawSwordClip,
                drawContinuationClip,
                controller);
            WriteInspection(metrics);
            CaptureRearArmDiagnostic(
                targetModel,
                retargetedClip,
                sourceClip.length,
                Absolute(DiagnosticCapturePath));
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                File.Delete(destination);
            CaptureReview(
                targetModel,
                retargetedClip,
                transitionBridgeClip,
                drawContinuationClip,
                metrics.BackMusketAttachTime,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-8 review capture changed the scene dirty state.");
            Debug.Log(
                "Ispant08ChangingToSwordReviewCaptured Result=PASS" +
                ", Columns=RearArmEnd,BackMusketAttached,ForwardArmEnd,HandLoweringEnd,BridgeStart,Bridge16,Bridge26,Bridge36,Bridge46,Bridge56,BridgeEnd,DrawStart,DrawFirstMotionFrame,DrawSecondMotionFrame,Draw50,DrawEnd,LoopStart" +
                ", Rows=Front,Back" +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                ProjectFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ProjectFbxPath) as ModelImporter ??
                           throw new InvalidOperationException("The changing-to-sword ModelImporter is missing.");
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    "The supplied changing-to-sword FBX must expose exactly one embedded animation take.");
            if (clips[0].takeName.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The sole changing-to-sword take is not identified as Mixamo: " + clips[0].takeName + ".");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            clips[0].name = ImportedClipName;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireImportedClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ProjectFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported changing-to-sword Mixamo clip differs.");
            if (!AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime)
                throw new InvalidOperationException("The imported changing-to-sword clip is not looping.");
            return clips[0];
        }

        private static AnimationClip RequireReferenceClip()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ReferenceControllerPath) ??
                             throw new InvalidOperationException("The slot-7 reference controller is missing.");
            var state = controller.layers[0].stateMachine.defaultState;
            return state?.motion as AnimationClip ??
                   throw new InvalidOperationException("The slot-7 reference clip is missing.");
        }

        private static AnimationClip RequireDrawSwordClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DrawSwordClipPath) ??
                       throw new InvalidOperationException("The current slot-4 draw-sword clip is missing.");
            if (clip.name != "Ispant_04_DrawSword_0_9m_Upward" ||
                Mathf.Abs(clip.frameRate - 25f) > 0.000001f ||
                Mathf.Abs(clip.length - 1.76f) > 0.0001f ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException(
                    "The current slot-4 draw-sword clip name, speed, duration, or loop setting differs.");
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip sourceClip,
            AnimationClip referenceClip,
            AnimationClip drawSwordClip,
            Transform drawSwordModel,
            Transform targetModel)
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
            foreach (var oldClip in AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                         .OfType<AnimationClip>()
                         .Where(item => item.name == RetargetedClipName ||
                                        item.name == TransitionBridgeClipName ||
                                        item.name == DrawContinuationClipName ||
                                        item.name == ContinuousSequenceClipName)
                         .ToArray())
                UnityEngine.Object.DestroyImmediate(oldClip, true);

            var clip = RetargetClip(
                sourceClip,
                referenceClip,
                targetModel);
            var drawContinuationClip = CreateDrawContinuationClip(
                drawSwordClip,
                drawSwordModel,
                targetModel);
            RebaseDrawContinuationVerticalPosition(
                clip,
                drawContinuationClip,
                targetModel);
            var transitionBridgeClip = CreateTransitionBridgeClip(
                clip,
                drawContinuationClip,
                targetModel);
            var continuousSequenceClip = CreateContinuousSequenceClip(
                clip,
                transitionBridgeClip,
                drawContinuationClip,
                targetModel);
            AssetDatabase.AddObjectToAsset(clip, controller);
            AssetDatabase.AddObjectToAsset(transitionBridgeClip, controller);
            AssetDatabase.AddObjectToAsset(drawContinuationClip, controller);
            AssetDatabase.AddObjectToAsset(continuousSequenceClip, controller);
            var state = stateMachine.AddState(StateName);
            state.motion = continuousSequenceClip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RebaseDrawContinuationVerticalPosition(
            AnimationClip changingClip,
            AnimationClip drawClip,
            Transform targetModel)
        {
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant08DrawVerticalRebaseSource");
            float verticalOffset;
            try
            {
                SampleClip(sampledTarget.gameObject, changingClip, changingClip.length);
                var changingHips = RequireMappedTransform(
                    BuildUniqueTransformMap(sampledTarget), "Hips");
                var changingHeight = ModelLocalPosition(sampledTarget, changingHips).y;
                SampleClip(sampledTarget.gameObject, drawClip, 0f);
                var drawHips = RequireMappedTransform(
                    BuildUniqueTransformMap(sampledTarget), "Hips");
                verticalOffset = changingHeight - ModelLocalPosition(sampledTarget, drawHips).y;
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }

            var armature = RequireMappedTransform(BuildUniqueTransformMap(targetModel), "Armature");
            var path = AnimationUtility.CalculateTransformPath(armature, targetModel);
            var binding = EditorCurveBinding.FloatCurve(
                path, typeof(Transform), "m_LocalPosition.y");
            var curve = AnimationUtility.GetEditorCurve(drawClip, binding) ??
                        throw new InvalidOperationException(
                            "The current slot-4 continuation has no Armature vertical-position curve.");
            var keys = curve.keys;
            for (var index = 0; index < keys.Length; index++)
                keys[index].value += verticalOffset;
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(drawClip, binding, curve);
        }

        private static AnimationClip CreateTransitionBridgeClip(
            AnimationClip changingClip,
            AnimationClip drawClip,
            Transform targetModel)
        {
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant08TransitionBridgeSource");
            var targetBones = BuildUniqueTransformMap(sampledTarget);
            var handSwordRoot = sampledTarget.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == HandSwordRootName);
            var transforms = targetBones.Values.Append(handSwordRoot)
                .Distinct()
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, sampledTarget),
                    StringComparer.Ordinal)
                .ToArray();
            var paths = transforms.ToDictionary(
                item => item,
                item => AnimationUtility.CalculateTransformPath(item, sampledTarget));
            Dictionary<string, LocalPose> startPoses;
            Dictionary<string, LocalPose> endPoses;
            try
            {
                SampleClip(sampledTarget.gameObject, changingClip, changingClip.length);
                startPoses = transforms.ToDictionary(
                    item => paths[item], item => new LocalPose(item), StringComparer.Ordinal);
                SampleClip(sampledTarget.gameObject, drawClip, 0f);
                endPoses = transforms.ToDictionary(
                    item => paths[item], item => new LocalPose(item), StringComparer.Ordinal);
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }

            const float frameRate = 60f;
            var frameCount = Mathf.RoundToInt(TransitionBridgeDuration * frameRate);
            var times = Enumerable.Range(0, frameCount + 1)
                .Select(frame => frame / frameRate).ToArray();
            var result = new AnimationClip
            {
                name = TransitionBridgeClipName,
                frameRate = frameRate,
                wrapMode = WrapMode.ClampForever,
                legacy = false
            };
            foreach (var path in startPoses.Keys)
            {
                var start = startPoses[path];
                var end = endPoses[path];
                var positions = new Vector3[times.Length];
                var rotations = new Quaternion[times.Length];
                var scales = new Vector3[times.Length];
                for (var index = 0; index < times.Length; index++)
                {
                    // Keep motion advancing through both state boundaries. Smoothstep forces its
                    // velocity to zero at 0 and 1, which made the lowered arm visibly stop before
                    // moving to the waist and stop again immediately before the draw clip.
                    var continuous = index / (float)frameCount;
                    positions[index] = Vector3.LerpUnclamped(start.Position, end.Position, continuous);
                    rotations[index] = Quaternion.SlerpUnclamped(start.Rotation, end.Rotation, continuous);
                    if (index > 0 && Quaternion.Dot(rotations[index - 1], rotations[index]) < 0f)
                        rotations[index] = new Quaternion(
                            -rotations[index].x, -rotations[index].y,
                            -rotations[index].z, -rotations[index].w);
                    scales[index] = Vector3.LerpUnclamped(start.Scale, end.Scale, continuous);
                }
                SetCurve(result, path, "m_LocalPosition.x", times, positions.Select(item => item.x));
                SetCurve(result, path, "m_LocalPosition.y", times, positions.Select(item => item.y));
                SetCurve(result, path, "m_LocalPosition.z", times, positions.Select(item => item.z));
                SetCurve(result, path, "m_LocalRotation.x", times, rotations.Select(item => item.x));
                SetCurve(result, path, "m_LocalRotation.y", times, rotations.Select(item => item.y));
                SetCurve(result, path, "m_LocalRotation.z", times, rotations.Select(item => item.z));
                SetCurve(result, path, "m_LocalRotation.w", times, rotations.Select(item => item.w));
                SetCurve(result, path, "m_LocalScale.x", times, scales.Select(item => item.x));
                SetCurve(result, path, "m_LocalScale.y", times, scales.Select(item => item.y));
                SetCurve(result, path, "m_LocalScale.z", times, scales.Select(item => item.z));
            }

            var handMusket = RequireRenderer<MeshRenderer>(targetModel, HandMusketRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(targetModel, IntermediateBackMusketName);
            var waistSword = RequireRenderer<MeshRenderer>(targetModel, WaistSwordRendererName);
            var handSword = RequireRenderer<MeshRenderer>(targetModel, HandSwordRendererName);
            foreach (var renderer in StaticAppearanceRendererNames
                         .Select(name => RequireRenderer<Renderer>(targetModel, name)))
                SetRendererVisibilityCurve(
                    result, targetModel, renderer,
                    new[] { 0f, TransitionBridgeDuration }, new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, handMusket,
                new[] { 0f, TransitionBridgeDuration }, new[] { 0f, 0f });
            SetRendererVisibilityCurve(
                result, targetModel, backMusket,
                new[] { 0f, TransitionBridgeDuration }, new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, waistSword,
                new[] { 0f, TransitionBridgeDuration }, new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, handSword,
                new[] { 0f, TransitionBridgeDuration }, new[] { 0f, 0f });
            result.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(changingClip);
            settings.startTime = 0f;
            settings.stopTime = TransitionBridgeDuration;
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            return result;
        }

        private static AnimationClip CreateDrawContinuationClip(
            AnimationClip source,
            Transform sourceModel,
            Transform targetModel)
        {
            var sourceStartTime = DrawContinuationSourceStartFrame / source.frameRate;
            var continuationLength = source.length - sourceStartTime;
            if (continuationLength <= 0f)
                throw new InvalidOperationException(
                    "The observed slot-4 draw continuation start is outside the source clip.");
            var targetTransforms = BuildUniqueTransformMap(targetModel);
            var result = new AnimationClip
            {
                name = DrawContinuationClipName,
                frameRate = source.frameRate,
                wrapMode = WrapMode.ClampForever,
                legacy = false
            };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.type != typeof(Transform))
                    throw new InvalidOperationException(
                        "The current slot-4 draw-sword clip contains an unsupported curve type: " +
                        binding.type.FullName + ".");
                var sourceTransform = RequireDescendantByPath(sourceModel, binding.path);
                var key = NormalizeBoneName(sourceTransform.name);
                Transform targetTransform;
                if (sourceTransform.name == HandSwordRootName)
                    targetTransform = targetModel.GetComponentsInChildren<Transform>(true)
                        .SingleOrDefault(item => item.name == HandSwordRootName) ??
                                      throw new InvalidOperationException(
                                          "The slot-8 exact slot-4 right-hand sword root is missing.");
                else if (!targetTransforms.TryGetValue(key, out targetTransform))
                    throw new InvalidOperationException(
                        "The slot-8 rig is missing a slot-4 draw-sword transform: " + key + ".");
                var targetPath = AnimationUtility.CalculateTransformPath(
                    targetTransform, targetModel);
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                                  throw new InvalidOperationException(
                                      "A current slot-4 draw-sword curve is missing.");
                AnimationUtility.SetEditorCurve(
                    result,
                    EditorCurveBinding.FloatCurve(
                        targetPath, typeof(Transform), binding.propertyName),
                    TrimCurve(sourceCurve, sourceStartTime, continuationLength));
            }
            if (AnimationUtility.GetObjectReferenceCurveBindings(source).Length != 0)
                throw new InvalidOperationException(
                    "The current slot-4 draw-sword clip contains unsupported object-reference curves.");

            var handMusket = RequireRenderer<MeshRenderer>(targetModel, HandMusketRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(targetModel, IntermediateBackMusketName);
            var waistSword = RequireRenderer<MeshRenderer>(targetModel, WaistSwordRendererName);
            var handSword = RequireRenderer<MeshRenderer>(targetModel, HandSwordRendererName);
            var appearance = StaticAppearanceRendererNames
                .Select(name => RequireRenderer<Renderer>(targetModel, name)).ToArray();
            foreach (var renderer in appearance)
                SetRendererVisibilityCurve(
                    result, targetModel, renderer,
                    new[] { 0f, continuationLength }, new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, handMusket,
                new[] { 0f, continuationLength }, new[] { 0f, 0f });
            SetRendererVisibilityCurve(
                result, targetModel, backMusket,
                new[] { 0f, continuationLength }, new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, waistSword,
                new[] { 0f, continuationLength }, new[] { 0f, 0f });
            SetRendererVisibilityCurve(
                result, targetModel, handSword,
                new[] { 0f, continuationLength }, new[] { 1f, 1f });

            result.EnsureQuaternionContinuity();
            var events = AnimationUtility.GetAnimationEvents(source)
                .Where(item => item.time >= sourceStartTime - 0.0001f)
                .Select(item =>
                {
                    item.time = Mathf.Max(0f, item.time - sourceStartTime);
                    return item;
                })
                .ToArray();
            AnimationUtility.SetAnimationEvents(result, events);
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = 0f;
            settings.stopTime = continuationLength;
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            return result;
        }

        private static AnimationClip CreateContinuousSequenceClip(
            AnimationClip changingClip,
            AnimationClip bridgeClip,
            AnimationClip drawClip,
            Transform targetModel)
        {
            const float frameRate = 60f;
            var totalLength = changingClip.length + bridgeClip.length + drawClip.length;
            var frameCount = Mathf.RoundToInt(totalLength * frameRate);
            var sampledTarget = CreateSamplingClone(
                targetModel,
                "Ispant08ContinuousSequenceSource");
            var transforms = sampledTarget.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, sampledTarget),
                    StringComparer.Ordinal)
                .ToArray();
            var renderers = sampledTarget.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item.transform, sampledTarget),
                    StringComparer.Ordinal)
                .ToArray();
            var paths = transforms.ToDictionary(
                item => item,
                item => AnimationUtility.CalculateTransformPath(item, sampledTarget));
            var rendererPaths = renderers.ToDictionary(
                item => item,
                item => AnimationUtility.CalculateTransformPath(item.transform, sampledTarget));
            var times = new float[frameCount + 1];
            var positions = transforms.ToDictionary(item => item, _ => new Vector3[frameCount + 1]);
            var rotations = transforms.ToDictionary(item => item, _ => new Quaternion[frameCount + 1]);
            var scales = transforms.ToDictionary(item => item, _ => new Vector3[frameCount + 1]);
            var visibility = renderers.ToDictionary(item => item, _ => new float[frameCount + 1]);
            try
            {
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var sequenceTime = Mathf.Min(frame / frameRate, totalLength);
                    times[frame] = sequenceTime;
                    if (sequenceTime <= changingClip.length + 0.000001f)
                    {
                        SampleClip(sampledTarget.gameObject, changingClip, sequenceTime);
                    }
                    else if (sequenceTime <= changingClip.length + bridgeClip.length + 0.000001f)
                    {
                        SampleClip(
                            sampledTarget.gameObject,
                            bridgeClip,
                            sequenceTime - changingClip.length);
                    }
                    else
                    {
                        SampleClip(
                            sampledTarget.gameObject,
                            drawClip,
                            sequenceTime - changingClip.length - bridgeClip.length);
                    }

                    foreach (var transform in transforms)
                    {
                        positions[transform][frame] = transform.localPosition;
                        var rotation = transform.localRotation;
                        if (frame > 0 && Quaternion.Dot(rotations[transform][frame - 1], rotation) < 0f)
                            rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                        rotations[transform][frame] = rotation;
                        scales[transform][frame] = transform.localScale;
                    }
                    foreach (var renderer in renderers)
                        visibility[renderer][frame] = renderer.enabled ? 1f : 0f;
                }
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }

            var result = new AnimationClip
            {
                name = ContinuousSequenceClipName,
                frameRate = frameRate,
                wrapMode = WrapMode.Loop,
                legacy = false
            };
            foreach (var transform in transforms)
            {
                var path = paths[transform];
                SetCurve(result, path, "m_LocalPosition.x", times, positions[transform].Select(item => item.x));
                SetCurve(result, path, "m_LocalPosition.y", times, positions[transform].Select(item => item.y));
                SetCurve(result, path, "m_LocalPosition.z", times, positions[transform].Select(item => item.z));
                SetCurve(result, path, "m_LocalRotation.x", times, rotations[transform].Select(item => item.x));
                SetCurve(result, path, "m_LocalRotation.y", times, rotations[transform].Select(item => item.y));
                SetCurve(result, path, "m_LocalRotation.z", times, rotations[transform].Select(item => item.z));
                SetCurve(result, path, "m_LocalRotation.w", times, rotations[transform].Select(item => item.w));
                SetCurve(result, path, "m_LocalScale.x", times, scales[transform].Select(item => item.x));
                SetCurve(result, path, "m_LocalScale.y", times, scales[transform].Select(item => item.y));
                SetCurve(result, path, "m_LocalScale.z", times, scales[transform].Select(item => item.z));
            }
            foreach (var renderer in renderers)
            {
                var binding = EditorCurveBinding.FloatCurve(
                    rendererPaths[renderer], renderer.GetType(), "m_Enabled");
                var keys = times.Select((time, index) =>
                    new Keyframe(time, visibility[renderer][index])).ToArray();
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(result, binding, curve);
            }
            result.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(changingClip);
            settings.startTime = 0f;
            settings.stopTime = totalLength;
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            return result;
        }

        private static AnimationCurve TrimCurve(
            AnimationCurve source,
            float sourceStartTime,
            float continuationLength)
        {
            var keys = source.keys
                .Where(item => item.time >= sourceStartTime - 0.0001f)
                .Select(item =>
                {
                    item.time = Mathf.Clamp(item.time - sourceStartTime, 0f, continuationLength);
                    return item;
                })
                .ToList();
            if (keys.Count == 0 || keys[0].time > 0.0001f)
                keys.Insert(0, new Keyframe(0f, source.Evaluate(sourceStartTime)));
            else
            {
                var first = keys[0];
                first.time = 0f;
                first.value = source.Evaluate(sourceStartTime);
                keys[0] = first;
            }
            if (keys[^1].time < continuationLength - 0.0001f)
                keys.Add(new Keyframe(
                    continuationLength,
                    source.Evaluate(sourceStartTime + continuationLength)));
            var result = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return result;
        }

        private static void ConfigureExactExitTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
            transition.canTransitionToSelf = false;
        }

        private static AnimationClip RetargetClip(
            AnimationClip sourceClip,
            AnimationClip referenceClip,
            Transform targetModel)
        {
            var targetBones = BuildUniqueTransformMap(targetModel);
            var result = new AnimationClip
            {
                name = RetargetedClipName,
                frameRate = sourceClip.frameRate,
                wrapMode = WrapMode.Loop,
                legacy = false
            };
            foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip))
            {
                if (binding.type != typeof(Transform))
                    throw new InvalidOperationException(
                        "The supplied changing-to-sword clip contains an unsupported curve type: " +
                        binding.type.FullName + ".");
                if (binding.propertyName.StartsWith("m_LocalScale.", StringComparison.Ordinal))
                    continue;
                var targetPath = RetargetPath(binding.path, targetModel, targetBones);
                AnimationUtility.SetEditorCurve(
                    result,
                    EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), binding.propertyName),
                    AnimationUtility.GetEditorCurve(sourceClip, binding));
            }
            if (AnimationUtility.GetObjectReferenceCurveBindings(sourceClip).Length != 0)
                throw new InvalidOperationException(
                    "The supplied changing-to-sword clip contains unsupported object-reference curves.");
            RebaseReferencePoseCurves(sourceClip, referenceClip, targetModel, result);
            AppendAnimatedHandLoweringAndBackMusketHold(result, sourceClip, targetModel);
            result.EnsureQuaternionContinuity();
            AnimationUtility.SetAnimationEvents(result, AnimationUtility.GetAnimationEvents(sourceClip));
            var authoredLength = result.length;
            var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            settings.startTime = 0f;
            settings.stopTime = authoredLength;
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            return result;
        }

        private static void RebaseReferencePoseCurves(
            AnimationClip source,
            AnimationClip referenceClip,
            Transform targetModel,
            AnimationClip result)
        {
            var targetBones = BuildUniqueTransformMap(targetModel);
            var sourceBindings = AnimationUtility.GetCurveBindings(source);
            var snapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var referenceRotations = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
            var referencePositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
            try
            {
                SampleClip(targetModel.gameObject, referenceClip, 0f);
                foreach (var boneName in ReferencePoseBoneNames)
                    referenceRotations[boneName] = RequireMappedTransform(targetBones, boneName).localRotation;
                foreach (var boneName in ReferenceTranslationBoneNames)
                    referencePositions[boneName] = RequireMappedTransform(targetBones, boneName).localPosition;
            }
            finally
            {
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            var frameCount = Mathf.Max(1, Mathf.RoundToInt(source.length * source.frameRate));
            var times = Enumerable.Range(0, frameCount + 1)
                .Select(frame => Mathf.Min(frame / source.frameRate, source.length)).ToArray();
            foreach (var boneName in ReferencePoseBoneNames)
            {
                var bindings = sourceBindings.Where(binding =>
                        binding.type == typeof(Transform) &&
                        binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) &&
                        string.Equals(
                            NormalizeBoneName(binding.path.Split('/').Last()),
                            boneName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var rotations = new Quaternion[times.Length];
                if (bindings.Length == 0)
                {
                    for (var index = 0; index < times.Length; index++)
                        rotations[index] = referenceRotations[boneName];
                }
                else
                {
                    if (bindings.Length != 4)
                        throw new InvalidOperationException(
                            "The source clip has an incomplete rotation for reference bone: " + boneName + ".");
                    var curves = new[] { "x", "y", "z", "w" }
                        .Select(component => bindings.Single(binding =>
                            binding.propertyName == "m_LocalRotation." + component))
                        .Select(binding => AnimationUtility.GetEditorCurve(source, binding)).ToArray();
                    Quaternion SourceRotation(float time) => NormalizeQuaternion(new Quaternion(
                        curves[0].Evaluate(time), curves[1].Evaluate(time),
                        curves[2].Evaluate(time), curves[3].Evaluate(time)));
                    var sourceStart = SourceRotation(0f);
                    for (var index = 0; index < times.Length; index++)
                    {
                        var desired = NormalizeQuaternion(
                            referenceRotations[boneName] *
                            (Quaternion.Inverse(sourceStart) * SourceRotation(times[index])));
                        if (index > 0 && Quaternion.Dot(rotations[index - 1], desired) < 0f)
                            desired = new Quaternion(-desired.x, -desired.y, -desired.z, -desired.w);
                        rotations[index] = desired;
                    }
                }
                var target = RequireMappedTransform(targetBones, boneName);
                var path = AnimationUtility.CalculateTransformPath(target, targetModel);
                SetCurve(result, path, "m_LocalRotation.x", times, rotations.Select(item => item.x));
                SetCurve(result, path, "m_LocalRotation.y", times, rotations.Select(item => item.y));
                SetCurve(result, path, "m_LocalRotation.z", times, rotations.Select(item => item.z));
                SetCurve(result, path, "m_LocalRotation.w", times, rotations.Select(item => item.w));
            }

            foreach (var boneName in ReferenceTranslationBoneNames)
            {
                var bindings = sourceBindings.Where(binding =>
                        binding.type == typeof(Transform) &&
                        binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal) &&
                        string.Equals(
                            NormalizeBoneName(binding.path.Split('/').Last()),
                            boneName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var positions = new Vector3[times.Length];
                if (bindings.Length == 0)
                {
                    for (var index = 0; index < times.Length; index++)
                        positions[index] = referencePositions[boneName];
                }
                else
                {
                    if (bindings.Length != 3)
                        throw new InvalidOperationException(
                            "The source clip has an incomplete position for reference bone: " + boneName + ".");
                    var curves = new[] { "x", "y", "z" }
                        .Select(component => bindings.Single(binding =>
                            binding.propertyName == "m_LocalPosition." + component))
                        .Select(binding => AnimationUtility.GetEditorCurve(source, binding)).ToArray();
                    Vector3 SourcePosition(float time) => new Vector3(
                        curves[0].Evaluate(time), curves[1].Evaluate(time), curves[2].Evaluate(time));
                    var sourceStart = SourcePosition(0f);
                    for (var index = 0; index < times.Length; index++)
                        positions[index] = referencePositions[boneName] +
                                           SourcePosition(times[index]) - sourceStart;
                }
                var target = RequireMappedTransform(targetBones, boneName);
                var path = AnimationUtility.CalculateTransformPath(target, targetModel);
                SetCurve(result, path, "m_LocalPosition.x", times, positions.Select(item => item.x));
                SetCurve(result, path, "m_LocalPosition.y", times, positions.Select(item => item.y));
                SetCurve(result, path, "m_LocalPosition.z", times, positions.Select(item => item.z));
            }
        }

        private static void AppendAnimatedHandLoweringAndBackMusketHold(
            AnimationClip result,
            AnimationClip sourceClip,
            Transform targetModel)
        {
            var timing = FindRearArmAttachTiming(
                result,
                sourceClip,
                targetModel);
            var attachPreviousFrame = Mathf.Max(0f, timing.AttachTime - 1f / sourceClip.frameRate);
            var forwardArmEndTime = DirectlyObservedForwardArmEndFrame / sourceClip.frameRate;
            if (forwardArmEndTime <= timing.AttachTime || forwardArmEndTime >= sourceClip.length)
                throw new InvalidOperationException(
                    "The directly observed forward-arm endpoint is outside the retained source motion.");
            var handLoweringEnd = forwardArmEndTime + HandLoweringDuration;
            var holdEnd = handLoweringEnd + LoweredPoseHoldDuration;
            CompressOriginalStaticReturnMotion(
                result,
                sourceClip,
                DirectlyObservedForwardArmEndFrame,
                DirectlyObservedStaticReturnSourceEndFrame,
                handLoweringEnd);
            var handMusket = RequireRenderer<MeshRenderer>(targetModel, HandMusketRendererName);
            var intermediateBackMusket = RequireRenderer<MeshRenderer>(
                targetModel, IntermediateBackMusketName);
            var waistSword = RequireRenderer<MeshRenderer>(
                targetModel, WaistSwordRendererName);
            var handSword = RequireRenderer<MeshRenderer>(
                targetModel, HandSwordRendererName);
            var animatedAppearanceRenderers = targetModel.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != handMusket &&
                                   renderer != intermediateBackMusket &&
                                   renderer != waistSword &&
                                   renderer != handSword)
                .ToArray();
            if (animatedAppearanceRenderers.Length != 3)
                throw new InvalidOperationException(
                    "The slot-8 animated visibility group differs.");
            foreach (var renderer in animatedAppearanceRenderers)
                SetRendererVisibilityCurve(
                    result, targetModel, renderer,
                    new[] { 0f, holdEnd },
                    new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, waistSword,
                new[] { 0f, holdEnd },
                new[] { 1f, 1f });
            SetRendererVisibilityCurve(
                result, targetModel, handSword,
                new[] { 0f, holdEnd },
                new[] { 0f, 0f });
            SetRendererVisibilityCurve(
                result, targetModel, handMusket,
                new[] { 0f, attachPreviousFrame, timing.AttachTime, holdEnd },
                new[] { 1f, 1f, 0f, 0f });
            SetRendererVisibilityCurve(
                result, targetModel, intermediateBackMusket,
                new[] { 0f, attachPreviousFrame, timing.AttachTime, holdEnd },
                new[] { 0f, 0f, 1f, 1f });
        }

        private static void CompressOriginalStaticReturnMotion(
            AnimationClip clip,
            AnimationClip sourceClip,
            int sourceStartFrame,
            int sourceEndFrame,
            float compressedEndTime)
        {
            var sourceStartTime = sourceStartFrame / sourceClip.frameRate;
            var sourceEndTime = sourceEndFrame / sourceClip.frameRate;
            if (sourceStartFrame <= 0 || sourceEndFrame <= sourceStartFrame ||
                sourceEndTime >= sourceClip.length || compressedEndTime <= sourceStartTime)
                throw new InvalidOperationException(
                    "The directly observed static-return source range is invalid.");
            var sampleFrames = Enumerable.Range(
                sourceStartFrame,
                sourceEndFrame - sourceStartFrame + 1).ToArray();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => binding.type == typeof(Transform)).ToArray())
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(clip, binding);
                var keys = sourceCurve.keys
                    .Where(key => key.time < sourceStartTime - 0.000001f)
                    .ToList();
                foreach (var frame in sampleFrames)
                {
                    var sourceTime = frame / sourceClip.frameRate;
                    var normalized = (sourceTime - sourceStartTime) /
                                     (sourceEndTime - sourceStartTime);
                    var compressedTime = Mathf.Lerp(
                        sourceStartTime, compressedEndTime, normalized);
                    keys.Add(new Keyframe(compressedTime, sourceCurve.Evaluate(sourceTime)));
                }
                var compressedCurve = new AnimationCurve(keys.ToArray());
                for (var index = 0; index < compressedCurve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        compressedCurve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        compressedCurve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(clip, binding, compressedCurve);
            }
        }

        private static void AppendExactStaticCloneMotionAndReturnCurves(
            AnimationClip clip,
            AnimationClip sourceClip,
            Transform targetModel,
            Transform staticHoldRoot,
            int attachFrame,
            int forwardArmEndFrame,
            float staticReturnEnd,
            float holdEnd)
        {
            var targetBones = BuildUniqueTransformMap(targetModel);
            var staticBones = BuildStaticCloneTransformMap(staticHoldRoot, targetBones.Keys);
            var missingPoseBones = ReferencePoseBoneNames
                .Where(key => !staticBones.ContainsKey(key)).ToArray();
            if (missingPoseBones.Length != 0)
                throw new InvalidOperationException(
                    "The exact static clone is missing required body-pose bones: " +
                    string.Join(",", missingPoseBones) + ".");

            var sampleTimes = Enumerable.Range(
                    attachFrame,
                    forwardArmEndFrame - attachFrame + 1)
                .Select(frame => frame / sourceClip.frameRate)
                .ToArray();
            var sampledPoses = targetBones.Keys.ToDictionary(
                key => key,
                _ => new List<LocalPose>(),
                StringComparer.OrdinalIgnoreCase);
            var targetDefaults = targetBones.ToDictionary(
                pair => pair.Key,
                pair => new LocalPose(pair.Value),
                StringComparer.OrdinalIgnoreCase);
            var snapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                foreach (var time in sampleTimes)
                {
                    SampleClip(targetModel.gameObject, clip, time);
                    foreach (var pair in targetBones)
                        sampledPoses[pair.Key].Add(new LocalPose(pair.Value));
                }
            }
            finally
            {
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            foreach (var pair in staticBones)
            {
                var sourcePoses = sampledPoses[pair.Key];
                var staticPose = new LocalPose(pair.Value);
                var times = new List<float> { 0f };
                times.AddRange(sampleTimes);
                times.Add(staticReturnEnd);
                times.Add(holdEnd);
                var rotations = new List<Quaternion> { Quaternion.identity };
                rotations.AddRange(sourcePoses.Select(sourcePose =>
                    NormalizeQuaternion(
                        staticPose.Rotation *
                        (Quaternion.Inverse(targetDefaults[pair.Key].Rotation) *
                         sourcePose.Rotation))));
                rotations[0] = rotations[1];
                rotations.Add(staticPose.Rotation);
                rotations.Add(staticPose.Rotation);
                for (var index = 1; index < rotations.Count; index++)
                {
                    var rotation = rotations[index];
                    if (Quaternion.Dot(rotations[index - 1], rotation) < 0f)
                        rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                    rotations[index] = rotation;
                }
                var path = AnimationUtility.CalculateTransformPath(pair.Value, targetModel);
                SetCurve(clip, path, "m_LocalRotation.x", times, rotations.Select(item => item.x));
                SetCurve(clip, path, "m_LocalRotation.y", times, rotations.Select(item => item.y));
                SetCurve(clip, path, "m_LocalRotation.z", times, rotations.Select(item => item.z));
                SetCurve(clip, path, "m_LocalRotation.w", times, rotations.Select(item => item.w));
            }
        }

        private static void TrimTransformCurvesAfter(AnimationClip clip, float endTime)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => binding.type == typeof(Transform)).ToArray())
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var endValue = curve.Evaluate(endTime);
                var keys = curve.keys.Where(key => key.time < endTime).ToList();
                keys.Add(new Keyframe(endTime, endValue));
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys.ToArray()));
            }
        }

        private static void SetRendererVisibilityCurve(
            AnimationClip clip,
            Transform model,
            Renderer renderer,
            IReadOnlyList<float> times,
            IReadOnlyList<float> values)
        {
            if (times.Count != values.Count || times.Count < 2)
                throw new InvalidOperationException("A slot-8 visibility curve is incomplete.");
            var curve = new AnimationCurve(times.Select(
                (time, index) => new Keyframe(time, values[index])).ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    AnimationUtility.CalculateTransformPath(renderer.transform, model),
                    renderer.GetType(),
                    "m_Enabled"),
                curve);
        }

        private static RearArmAttachTiming FindRearArmAttachTiming(
            AnimationClip result,
            AnimationClip sourceClip,
            Transform targetModel)
        {
            _ = result;
            _ = targetModel;
            var frameCount = Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate);
            if (DirectlyObservedRearArmEndFrame <= 0 ||
                DirectlyObservedRearArmEndFrame >= frameCount)
                throw new InvalidOperationException(
                    "The directly observed rear-arm endpoint is outside the source motion.");
            var attachFrame = DirectlyObservedRearArmEndFrame + 1;
            return new RearArmAttachTiming(
                DirectlyObservedRearArmEndFrame,
                DirectlyObservedRearArmEndFrame / sourceClip.frameRate,
                attachFrame,
                attachFrame / sourceClip.frameRate,
                0f);
        }

        private static void AppendTransformHoldCurves(
            AnimationClip clip,
            string path,
            LocalPose defaultPose,
            LocalPose holdPose,
            float transitionStart,
            float transitionEnd,
            float holdEnd)
        {
            var desiredRotation = holdPose.Rotation;
            var previousRotation = EvaluateQuaternion(
                clip,
                path,
                defaultPose.Rotation,
                transitionStart);
            if (Quaternion.Dot(previousRotation, desiredRotation) < 0f)
                desiredRotation = new Quaternion(
                    -desiredRotation.x,
                    -desiredRotation.y,
                    -desiredRotation.z,
                    -desiredRotation.w);

            AppendHoldCurve(
                clip, path, "m_LocalPosition.x", defaultPose.Position.x,
                holdPose.Position.x, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalPosition.y", defaultPose.Position.y,
                holdPose.Position.y, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalPosition.z", defaultPose.Position.z,
                holdPose.Position.z, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalRotation.x", defaultPose.Rotation.x,
                desiredRotation.x, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalRotation.y", defaultPose.Rotation.y,
                desiredRotation.y, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalRotation.z", defaultPose.Rotation.z,
                desiredRotation.z, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalRotation.w", defaultPose.Rotation.w,
                desiredRotation.w, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalScale.x", defaultPose.Scale.x,
                holdPose.Scale.x, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalScale.y", defaultPose.Scale.y,
                holdPose.Scale.y, transitionStart, transitionEnd, holdEnd);
            AppendHoldCurve(
                clip, path, "m_LocalScale.z", defaultPose.Scale.z,
                holdPose.Scale.z, transitionStart, transitionEnd, holdEnd);
        }

        private static void AppendHoldCurve(
            AnimationClip clip,
            string path,
            string property,
            float defaultValue,
            float holdValue,
            float transitionStart,
            float transitionEnd,
            float holdEnd)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            var source = AnimationUtility.GetEditorCurve(clip, binding);
            var keys = source == null
                ? new List<Keyframe>()
                : source.keys.Where(key => key.time < transitionStart - 0.000001f).ToList();
            var transitionValue = source == null ? defaultValue : source.Evaluate(transitionStart);
            if (source == null && transitionStart > 0.000001f)
                keys.Add(new Keyframe(0f, defaultValue));
            keys.Add(new Keyframe(transitionStart, transitionValue));
            keys.Add(new Keyframe(transitionEnd, holdValue));
            keys.Add(new Keyframe(holdEnd, holdValue));
            var result = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < result.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    result, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    result, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, binding, result);
        }

        private static Quaternion EvaluateQuaternion(
            AnimationClip clip,
            string path,
            Quaternion defaultValue,
            float time)
        {
            float Value(string component, float fallback)
            {
                var binding = EditorCurveBinding.FloatCurve(
                    path, typeof(Transform), "m_LocalRotation." + component);
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                return curve == null ? fallback : curve.Evaluate(time);
            }
            return NormalizeQuaternion(new Quaternion(
                Value("x", defaultValue.x),
                Value("y", defaultValue.y),
                Value("z", defaultValue.z),
                Value("w", defaultValue.w)));
        }

        private static void SetCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<float> times,
            IEnumerable<float> values)
        {
            var valueArray = values.ToArray();
            var curve = new AnimationCurve(Enumerable.Range(0, times.Count)
                .Select(index => new Keyframe(times[index], valueArray[index])).ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static AnimationClip RequireRetargetedClip(AnimatorController controller)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                .OfType<AnimationClip>()
                .Where(item => item.name == RetargetedClipName).ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException("The slot-8 retargeted clip differs.");
            if (AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime)
                throw new InvalidOperationException("The slot-8 changing clip must be non-looping inside the sequence.");
            return clips[0];
        }

        private static AnimationClip RequireDrawContinuationClip(AnimatorController controller)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                .OfType<AnimationClip>()
                .Where(item => item.name == DrawContinuationClipName).ToArray();
            var expectedLength = 1.76f - DrawContinuationSourceStartFrame / 25f;
            if (clips.Length != 1 || AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime ||
                Mathf.Abs(clips[0].length - expectedLength) > 0.0001f)
                throw new InvalidOperationException(
                    "The slot-8 exact slot-4 draw-sword continuation clip differs.");
            return clips[0];
        }

        private static AnimationClip RequireTransitionBridgeClip(AnimatorController controller)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                .OfType<AnimationClip>()
                .Where(item => item.name == TransitionBridgeClipName).ToArray();
            if (clips.Length != 1 || AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime ||
                Mathf.Abs(clips[0].length - TransitionBridgeDuration) > 0.0001f)
                throw new InvalidOperationException(
                    "The slot-8 to slot-4 transition bridge clip differs.");
            return clips[0];
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                   throw new InvalidOperationException("The slot-8 AnimatorController is missing.");
        }

        private static Animator ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The slot-8 model must contain exactly one Animator.");
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

        private static void RemoveFiringEffect(Transform model)
        {
            foreach (var item in model.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name == MuzzleFlashPivotName || item.name == MuzzleFlashName)
                         .OrderByDescending(item => item.GetComponentsInParent<Transform>(true).Length)
                         .ToArray())
            {
                if (item != null)
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }

        private static void ConfigureAnimatedHoldAppearance(
            Transform staticPoseModel,
            Transform drawSwordModel,
            Transform targetModel)
        {
            foreach (var existing in targetModel.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name == ExactStaticHoldRootName ||
                                        item.name == IntermediateBackMusketName ||
                                        item.name == HandSwordRootName)
                         .OrderByDescending(item => item.GetComponentsInParent<Transform>(true).Length)
                         .ToArray())
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var waistSword = RequireRenderer<MeshRenderer>(
                targetModel, WaistSwordRendererName);
            var waistSwordRoot = waistSword.transform.parent;
            if (waistSwordRoot == null || waistSwordRoot.name != WaistSwordRootName ||
                waistSwordRoot.parent != RequireMappedTransform(
                    BuildUniqueTransformMap(targetModel), "Hips"))
                throw new InvalidOperationException(
                    "The slot-7 left-waist sword source is not rigidly attached to mixamorig:Hips.");
            waistSword.enabled = true;

            var sourceBackMusket = RequireRenderer<MeshRenderer>(
                staticPoseModel, SourceBackMusketName);
            var targetSpine2 = RequireMappedTransform(
                BuildUniqueTransformMap(targetModel), "Spine2");
            var intermediateBackMusket = UnityEngine.Object.Instantiate(
                sourceBackMusket.gameObject);
            intermediateBackMusket.name = IntermediateBackMusketName;
            intermediateBackMusket.transform.SetParent(targetSpine2, false);
            CopyLocalTransform(sourceBackMusket.transform, intermediateBackMusket.transform);
            var intermediateRenderer = intermediateBackMusket
                .GetComponentsInChildren<MeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException(
                    "The exact slot-6 back-musket clone has no single MeshRenderer.");
            intermediateRenderer.enabled = false;

            var sourceHandSword = RequireRenderer<MeshRenderer>(
                drawSwordModel, HandSwordRendererName);
            var sourceHandSwordRoot = sourceHandSword.transform.parent;
            var sourceRightHand = RequireMappedTransform(
                BuildUniqueTransformMap(drawSwordModel), "RightHand");
            if (sourceHandSwordRoot == null ||
                sourceHandSwordRoot.name != HandSwordRootName ||
                sourceHandSwordRoot.parent != sourceRightHand)
                throw new InvalidOperationException(
                    "The current slot-4 long sword is not a direct RightHand child.");
            var targetRightHand = RequireMappedTransform(
                BuildUniqueTransformMap(targetModel), "RightHand");
            var targetHandSwordRoot = UnityEngine.Object.Instantiate(
                sourceHandSwordRoot.gameObject);
            targetHandSwordRoot.name = HandSwordRootName;
            targetHandSwordRoot.transform.SetParent(targetRightHand, false);
            CopyLocalTransform(sourceHandSwordRoot, targetHandSwordRoot.transform);
            var targetHandSword = RequireRenderer<MeshRenderer>(
                targetHandSwordRoot.transform, HandSwordRendererName);
            targetHandSword.enabled = false;

            EditorUtility.SetDirty(intermediateBackMusket);
            EditorUtility.SetDirty(targetHandSwordRoot);
            EditorUtility.SetDirty(waistSwordRoot.gameObject);
            EditorUtility.SetDirty(waistSword.gameObject);
        }

        private static Metrics InspectModel(
            Transform staticModel,
            Transform referenceModel,
            Transform staticPoseModel,
            Transform drawSwordModel,
            Transform targetModel,
            Animator animator,
            AnimationClip sourceClip,
            AnimationClip referenceClip,
            AnimationClip targetClip,
            AnimationClip transitionBridgeClip,
            AnimationClip drawSwordClip,
            AnimationClip drawContinuationClip,
            AnimatorController controller)
        {
            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion)
                throw new InvalidOperationException("The slot-8 Animator configuration differs.");
            if (targetModel.localPosition != referenceModel.localPosition ||
                targetModel.localRotation != referenceModel.localRotation ||
                targetModel.localScale != referenceModel.localScale)
                throw new InvalidOperationException("The slot-8 model scale or placement no longer matches slot 7.");
            if (Mathf.Abs(sourceClip.frameRate - targetClip.frameRate) > 0.000001f)
                throw new InvalidOperationException(
                    "The retargeted slot-8 frame rate differs from the source motion.");
            var expectedDrawContinuationLength = drawSwordClip.length -
                                                 DrawContinuationSourceStartFrame /
                                                 drawSwordClip.frameRate;
            if (Mathf.Abs(drawSwordClip.frameRate - drawContinuationClip.frameRate) > 0.000001f ||
                Mathf.Abs(expectedDrawContinuationLength - drawContinuationClip.length) > 0.0001f)
                throw new InvalidOperationException(
                    "The slot-8 draw-sword continuation changed the current slot-4 speed or original start frame.");
            RequireExactSequenceController(
                controller, targetClip, transitionBridgeClip, drawContinuationClip);

            var referenceAppearance = RequireDirectChild(referenceModel, AppearanceRootName);
            var targetAppearance = RequireDirectChild(targetModel, AppearanceRootName);
            var appearanceMismatch = 0f;
            foreach (var rendererName in StaticAppearanceRendererNames)
            {
                var reference = RequireRenderer<Renderer>(referenceAppearance, rendererName);
                var target = RequireRenderer<Renderer>(targetAppearance, rendererName);
                appearanceMismatch = Mathf.Max(appearanceMismatch, RendererAssetMismatch(reference, target));
            }
            if (appearanceMismatch > 0f)
                throw new InvalidOperationException("The slot-8 appearance assets differ from the static-synchronized slot 7.");
            _ = StaticAppearanceRendererNames.Select(name => RequireRenderer<Renderer>(staticModel, name)).ToArray();

            if (targetModel.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == ExactStaticHoldRootName))
                throw new InvalidOperationException(
                    "Slot 8 must not contain the legacy exact-static hold clone.");
            var allRenderers = targetModel.GetComponentsInChildren<Renderer>(true);
            var targetMusket = RequireRenderer<MeshRenderer>(targetModel, HandMusketRendererName);
            var intermediateBackMusket = RequireRenderer<MeshRenderer>(
                targetModel, IntermediateBackMusketName);
            var referenceWaistSword = RequireRenderer<MeshRenderer>(
                referenceModel, WaistSwordRendererName);
            var targetWaistSword = RequireRenderer<MeshRenderer>(
                targetModel, WaistSwordRendererName);
            var sourceHandSword = RequireRenderer<MeshRenderer>(
                drawSwordModel, HandSwordRendererName);
            var targetHandSword = RequireRenderer<MeshRenderer>(
                targetModel, HandSwordRendererName);
            var referenceWaistSwordRoot = referenceWaistSword.transform.parent;
            var targetWaistSwordRoot = targetWaistSword.transform.parent;
            if (referenceWaistSwordRoot == null || targetWaistSwordRoot == null ||
                referenceWaistSwordRoot.name != WaistSwordRootName ||
                targetWaistSwordRoot.name != WaistSwordRootName ||
                referenceWaistSwordRoot.parent != RequireMappedTransform(
                    BuildUniqueTransformMap(referenceModel), "Hips") ||
                targetWaistSwordRoot.parent != RequireMappedTransform(
                    BuildUniqueTransformMap(targetModel), "Hips") ||
                SharedMesh(referenceWaistSword) != SharedMesh(targetWaistSword) ||
                !referenceWaistSword.sharedMaterials.SequenceEqual(
                    targetWaistSword.sharedMaterials) ||
                MatrixError(
                    LocalMatrix(referenceWaistSwordRoot),
                    LocalMatrix(targetWaistSwordRoot)) > MatrixTolerance ||
                MatrixError(
                    LocalMatrix(referenceWaistSword.transform),
                    LocalMatrix(targetWaistSword.transform)) > MatrixTolerance ||
                !targetWaistSword.enabled)
                throw new InvalidOperationException(
                    "The slot-8 left-waist sword does not exactly preserve the slot-7 source mount and assets.");
            var sourceHandSwordRoot = sourceHandSword.transform.parent;
            var targetHandSwordRoot = targetHandSword.transform.parent;
            if (sourceHandSwordRoot == null || targetHandSwordRoot == null ||
                sourceHandSwordRoot.name != HandSwordRootName ||
                targetHandSwordRoot.name != HandSwordRootName ||
                sourceHandSwordRoot.parent != RequireMappedTransform(
                    BuildUniqueTransformMap(drawSwordModel), "RightHand") ||
                targetHandSwordRoot.parent != RequireMappedTransform(
                    BuildUniqueTransformMap(targetModel), "RightHand") ||
                SharedMesh(sourceHandSword) != SharedMesh(targetHandSword) ||
                !sourceHandSword.sharedMaterials.SequenceEqual(
                    targetHandSword.sharedMaterials) ||
                MatrixError(
                    LocalMatrix(sourceHandSwordRoot),
                    LocalMatrix(targetHandSwordRoot)) > MatrixTolerance ||
                MatrixError(
                    LocalMatrix(sourceHandSword.transform),
                    LocalMatrix(targetHandSword.transform)) > MatrixTolerance ||
                targetHandSword.enabled)
                throw new InvalidOperationException(
                    "The slot-8 right-hand sword does not exactly preserve the current slot-4 mount and assets.");
            var backMusketRenderers = allRenderers.Where(renderer =>
                renderer == intermediateBackMusket).ToArray();
            var animatedAppearanceRenderers = allRenderers.Where(renderer =>
                renderer != targetMusket &&
                renderer != intermediateBackMusket &&
                renderer != targetWaistSword &&
                renderer != targetHandSword).ToArray();
            if (backMusketRenderers.Length != 1)
                throw new InvalidOperationException(
                    "Slot 8 must contain the exact slot-6 intermediate back musket.");
            if (allRenderers.Any(renderer =>
                    renderer.name == MuzzleFlashName || renderer.name == MuzzleFlashPivotName))
                throw new InvalidOperationException("The slot-7 muzzle flash was copied into slot 8.");
            if (allRenderers.Length != 7 ||
                animatedAppearanceRenderers.Length != 3 ||
                targetModel.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == ExactStaticHoldRootName))
                throw new InvalidOperationException(
                    "Slot 8 must contain three animated appearance renderers, two musket renderers, " +
                    "one left-waist sword, and one exact slot-4 right-hand sword renderer.");
            if (animatedAppearanceRenderers.Any(renderer => !renderer.enabled) ||
                !targetMusket.enabled || intermediateBackMusket.enabled ||
                !targetWaistSword.enabled || targetHandSword.enabled)
                throw new InvalidOperationException(
                    "The saved slot-8 renderer visibility does not start in the animated state.");

            var referenceMusket = RequireRenderer<MeshRenderer>(referenceModel, HandMusketRendererName);
            var handMusketHideTime = RendererVisibilitySwitchTime(
                targetClip, targetModel, targetMusket, visibleAfterSwitch: false);
            var backMusketShowTime = RendererVisibilitySwitchTime(
                targetClip, targetModel, intermediateBackMusket, visibleAfterSwitch: true);
            var forwardArmEndFrame = DirectlyObservedForwardArmEndFrame;
            var forwardArmEndTime = forwardArmEndFrame / targetClip.frameRate;
            var handLoweringEndTime = forwardArmEndTime + HandLoweringDuration;
            var loweredPoseHoldSeconds = targetClip.length - handLoweringEndTime;
            if (Mathf.Abs(handMusketHideTime - backMusketShowTime) > 0.000001f ||
                handMusketHideTime <= 0f || handMusketHideTime >= sourceClip.length ||
                forwardArmEndTime <= handMusketHideTime ||
                Mathf.Abs(targetClip.length -
                           (forwardArmEndTime + HandLoweringDuration + LoweredPoseHoldDuration)) > 0.0001f ||
                Mathf.Abs(loweredPoseHoldSeconds - LoweredPoseHoldDuration) > 0.0001f)
                throw new InvalidOperationException(
                    "The hand-musket hide, forward-arm motion, 0.4-second hand lowering, or removed hold timing differs.");
            var backMusketAttachFrame = Mathf.RoundToInt(handMusketHideTime * targetClip.frameRate);
            var rearArmEndFrame = backMusketAttachFrame - 1;
            var rearArmEndTime = rearArmEndFrame / targetClip.frameRate;
            var referenceMusketRoot = referenceMusket.transform.parent ??
                                      throw new InvalidOperationException("The slot-7 hand-musket root is missing.");
            var targetMusketRoot = targetMusket.transform.parent ??
                                   throw new InvalidOperationException("The slot-8 hand-musket root is missing.");
            if (referenceMusketRoot.name != HandMusketRootName || targetMusketRoot.name != HandMusketRootName ||
                targetMusketRoot.parent?.name != "mixamorig:RightHand")
                throw new InvalidOperationException("The slot-8 musket is not attached exactly like slot 7.");
            var musketMountError = Mathf.Max(
                MatrixError(LocalMatrix(referenceMusketRoot), LocalMatrix(targetMusketRoot)),
                MatrixError(LocalMatrix(referenceMusket.transform), LocalMatrix(targetMusket.transform)));
            musketMountError = Mathf.Max(musketMountError, RendererAssetMismatch(referenceMusket, targetMusket));
            if (musketMountError > MatrixTolerance)
                throw new InvalidOperationException("The slot-8 hand-musket placement differs from slot 7.");
            var sourceBackMusket = RequireRenderer<MeshRenderer>(
                staticPoseModel, SourceBackMusketName);
            var intermediateBackMusketMountError = Mathf.Max(
                MatrixError(LocalMatrix(sourceBackMusket.transform),
                            LocalMatrix(intermediateBackMusket.transform)),
                RendererAssetMismatch(sourceBackMusket, intermediateBackMusket));
            if (sourceBackMusket.transform.parent == null ||
                intermediateBackMusket.transform.parent == null ||
                NormalizeBoneName(sourceBackMusket.transform.parent.name) != "Spine2" ||
                NormalizeBoneName(intermediateBackMusket.transform.parent.name) != "Spine2" ||
                intermediateBackMusketMountError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The intermediate back musket does not exactly reuse the slot-6 Spine2 mount.");

            var sampledReference = CreateSamplingClone(referenceModel, "Ispant07Slot8InspectReference");
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant08InspectSample");
            float leftShoulderHeightError;
            float rightShoulderHeightError;
            float referencePoseRotationError = 0f;
            var referenceMusketRootLocal = Matrix4x4.identity;
            try
            {
                SampleClip(sampledReference.gameObject, referenceClip, 0f);
                var referenceMap = BuildUniqueTransformMap(sampledReference);
                var referenceLeftShoulderHeight = ModelLocalPosition(
                    sampledReference,
                    RequireMappedTransform(referenceMap, "LeftShoulder")).y;
                var referenceRightShoulderHeight = ModelLocalPosition(
                    sampledReference,
                    RequireMappedTransform(referenceMap, "RightShoulder")).y;
                var referenceRotations = ReferencePoseBoneNames.ToDictionary(
                    boneName => boneName,
                    boneName => RequireMappedTransform(referenceMap, boneName).localRotation,
                    StringComparer.OrdinalIgnoreCase);
                var sampledReferenceMusket = RequireRenderer<MeshRenderer>(
                    sampledReference, HandMusketRendererName);
                referenceMusketRootLocal = LocalMatrix(sampledReferenceMusket.transform.parent);

                SampleClip(sampledTarget.gameObject, targetClip, 0f);
                var targetMap = BuildUniqueTransformMap(sampledTarget);
                leftShoulderHeightError = Mathf.Abs(
                    referenceLeftShoulderHeight -
                    ModelLocalPosition(sampledTarget, RequireMappedTransform(targetMap, "LeftShoulder")).y);
                rightShoulderHeightError = Mathf.Abs(
                    referenceRightShoulderHeight -
                    ModelLocalPosition(sampledTarget, RequireMappedTransform(targetMap, "RightShoulder")).y);
                foreach (var boneName in ReferencePoseBoneNames)
                    referencePoseRotationError = Mathf.Max(
                        referencePoseRotationError,
                        Quaternion.Angle(
                            referenceRotations[boneName],
                            RequireMappedTransform(targetMap, boneName).localRotation));
                var sampledTargetMusket = RequireRenderer<MeshRenderer>(sampledTarget, HandMusketRendererName);
                musketMountError = Mathf.Max(
                    musketMountError,
                    MatrixError(
                        referenceMusketRootLocal,
                        LocalMatrix(sampledTargetMusket.transform.parent)));
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledReference.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }
            if (leftShoulderHeightError > MatrixTolerance ||
                rightShoulderHeightError > MatrixTolerance ||
                referencePoseRotationError > 0.01f ||
                musketMountError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-8 initial arm height or musket pose differs from slot 7. " +
                    "LeftShoulderHeightError=" + Num(leftShoulderHeightError) +
                    ", RightShoulderHeightError=" + Num(rightShoulderHeightError) +
                    ", ReferencePoseRotationErrorDegrees=" + Num(referencePoseRotationError) +
                    ", MusketMountError=" + Num(musketMountError) + ".");

            var sampledFinalTarget = CreateSamplingClone(targetModel, "Ispant08FinalPoseSample");
            var finalBackMusketMountError = intermediateBackMusketMountError;
            var animatedHoldPoseDrift = 0f;
            var animatedHoldMusketDrift = 0f;
            var preStowMusketMountError = 0f;
            var attachRigPoseError = intermediateBackMusketMountError;
            var forwardArmEndRigPoseError = 0f;
            var waistSwordHipLocalDrift = 0f;
            var waistSwordBodyFollowPositionChange = 0f;
            var waistSwordBodyFollowRotationChange = 0f;
            try
            {
                var sampledWaistSword = RequireRenderer<MeshRenderer>(
                    sampledFinalTarget, WaistSwordRendererName);
                var sampledWaistSwordRoot = sampledWaistSword.transform.parent ??
                                           throw new InvalidOperationException(
                                               "The sampled slot-8 left-waist sword root is missing.");
                SampleClip(sampledFinalTarget.gameObject, targetClip, 0f);
                var waistSwordHipLocalReference = LocalMatrix(sampledWaistSwordRoot);
                var waistSwordModelRelativeReference =
                    sampledFinalTarget.worldToLocalMatrix * sampledWaistSwordRoot.localToWorldMatrix;
                DecomposeMatrix(
                    waistSwordModelRelativeReference,
                    out var waistSwordReferencePosition,
                    out var waistSwordReferenceRotation,
                    out _);
                var frameCount = Mathf.Max(
                    1, Mathf.RoundToInt(targetClip.length * targetClip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    SampleClip(
                        sampledFinalTarget.gameObject,
                        targetClip,
                        Mathf.Min(frame / targetClip.frameRate, targetClip.length));
                    waistSwordHipLocalDrift = Mathf.Max(
                        waistSwordHipLocalDrift,
                        MatrixError(
                            waistSwordHipLocalReference,
                            LocalMatrix(sampledWaistSwordRoot)));
                    var waistSwordModelRelative =
                        sampledFinalTarget.worldToLocalMatrix * sampledWaistSwordRoot.localToWorldMatrix;
                    DecomposeMatrix(
                        waistSwordModelRelative,
                        out var waistSwordPosition,
                        out var waistSwordRotation,
                        out _);
                    waistSwordBodyFollowPositionChange = Mathf.Max(
                        waistSwordBodyFollowPositionChange,
                        Vector3.Distance(
                            waistSwordReferencePosition,
                            waistSwordPosition));
                    waistSwordBodyFollowRotationChange = Mathf.Max(
                        waistSwordBodyFollowRotationChange,
                        Quaternion.Angle(
                            waistSwordReferenceRotation,
                            waistSwordRotation));
                }

                SampleClip(sampledFinalTarget.gameObject, targetClip, rearArmEndTime);
                RequireSlot8VisibilityPhase(
                    sampledFinalTarget, handVisible: true, backVisible: false,
                    "rear-motion end");
                var preStowMusket = RequireRenderer<MeshRenderer>(
                    sampledFinalTarget, HandMusketRendererName);
                preStowMusketMountError = Mathf.Max(
                    MatrixError(
                        referenceMusketRootLocal,
                        LocalMatrix(preStowMusket.transform.parent)),
                    MatrixError(
                        LocalMatrix(referenceMusket.transform),
                        LocalMatrix(preStowMusket.transform)));

                SampleClip(sampledFinalTarget.gameObject, targetClip, handMusketHideTime);
                RequireSlot8VisibilityPhase(
                    sampledFinalTarget, handVisible: false, backVisible: true,
                    "back-musket attach");
                var attachedHandMusket = RequireRenderer<MeshRenderer>(
                    sampledFinalTarget, HandMusketRendererName);
                if (attachedHandMusket.enabled)
                    throw new InvalidOperationException(
                        "The right-hand musket is visible after the back musket is attached.");

                SampleClip(sampledFinalTarget.gameObject, targetClip, forwardArmEndTime);
                RequireSlot8VisibilityPhase(
                    sampledFinalTarget, handVisible: false, backVisible: true,
                    "forward-arm motion end");

                SampleClip(sampledFinalTarget.gameObject, targetClip, handLoweringEndTime);
                RequireSlot8VisibilityPhase(
                    sampledFinalTarget, handVisible: false, backVisible: true,
                    "hand-lowering end");
                var loweredPoseMatrices = RelativeTransformMatrices(sampledFinalTarget);
                var loweredBackMusket = RequireRenderer<MeshRenderer>(
                    sampledFinalTarget, IntermediateBackMusketName);
                finalBackMusketMountError = Mathf.Max(
                    finalBackMusketMountError,
                    Mathf.Max(
                        MatrixError(LocalMatrix(sourceBackMusket.transform),
                                    LocalMatrix(loweredBackMusket.transform)),
                        RendererAssetMismatch(sourceBackMusket, loweredBackMusket)));
                var loweredBackMusketRelative =
                    sampledFinalTarget.worldToLocalMatrix * loweredBackMusket.transform.localToWorldMatrix;

                SampleClip(sampledFinalTarget.gameObject, targetClip, targetClip.length);
                RequireSlot8VisibilityPhase(
                    sampledFinalTarget, handVisible: false, backVisible: true,
                    "lowered-pose hold end");
                var heldPoseMatrices = RelativeTransformMatrices(sampledFinalTarget);
                foreach (var pair in loweredPoseMatrices)
                    animatedHoldPoseDrift = Mathf.Max(
                        animatedHoldPoseDrift,
                        MatrixError(pair.Value, heldPoseMatrices[pair.Key]));
                var heldBackMusket = RequireRenderer<MeshRenderer>(
                    sampledFinalTarget, IntermediateBackMusketName);
                var heldBackMusketRelative =
                    sampledFinalTarget.worldToLocalMatrix * heldBackMusket.transform.localToWorldMatrix;
                animatedHoldMusketDrift = MatrixError(
                    loweredBackMusketRelative, heldBackMusketRelative);
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledFinalTarget.gameObject);
            }
            if (preStowMusketMountError > MatrixTolerance ||
                attachRigPoseError > MatrixTolerance ||
                forwardArmEndRigPoseError > MatrixTolerance ||
                finalBackMusketMountError > MatrixTolerance ||
                animatedHoldPoseDrift > MatrixTolerance ||
                animatedHoldMusketDrift > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-8 back-musket stow or animated lowered-pose hold differs. " +
                    "PreStowMountError=" + Num(preStowMusketMountError) +
                    ", AttachRigPoseError=" + Num(attachRigPoseError) +
                    ", ForwardArmEndRigPoseError=" + Num(forwardArmEndRigPoseError) +
                    ", FinalBackMusketMountError=" + Num(finalBackMusketMountError) +
                    ", HoldPoseDrift=" + Num(animatedHoldPoseDrift) +
                    ", HoldMusketDrift=" + Num(animatedHoldMusketDrift) + ".");
            if (waistSwordHipLocalDrift > MatrixTolerance ||
                waistSwordBodyFollowRotationChange <= 0.01f)
                throw new InvalidOperationException(
                    "The slot-8 left-waist sword does not rigidly follow the animated body: LocalDrift=" +
                    Num(waistSwordHipLocalDrift) +
                    ", PositionChange=" + Num(waistSwordBodyFollowPositionChange) +
                    ", RotationChangeDegrees=" + Num(waistSwordBodyFollowRotationChange) + ".");

            var transitionBridgeEndpointPoseError = InspectTransitionBridge(
                targetModel,
                targetClip,
                transitionBridgeClip,
                drawContinuationClip,
                out var transitionBridgeMaxFrameYStep,
                out var transitionBridgeYTravel);
            if (Mathf.Abs(transitionBridgeClip.length - TransitionBridgeDuration) > 0.0001f ||
                transitionBridgeEndpointPoseError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-8 to slot-4 transition bridge differs: " +
                    "Length=" + Num(transitionBridgeClip.length) +
                    ", EndpointPoseError=" + Num(transitionBridgeEndpointPoseError) + ".");

            var drawContinuationPoseError = InspectDrawContinuation(
                drawSwordModel,
                drawSwordClip,
                targetModel,
                drawContinuationClip,
                sourceBackMusket,
                out var drawContinuationVerticalRebase,
                out var drawContinuationBackMusketMountError);
            if (drawContinuationPoseError > MatrixTolerance ||
                drawContinuationBackMusketMountError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-8 continuation differs from the current slot-4 animation: " +
                    "PoseError=" + Num(drawContinuationPoseError) +
                    ", BackMusketMountError=" +
                    Num(drawContinuationBackMusketMountError) + ".");

            return new Metrics(
                targetClip.length,
                targetClip.frameRate,
                appearanceMismatch,
                musketMountError,
                preStowMusketMountError,
                finalBackMusketMountError,
                animatedHoldPoseDrift,
                animatedHoldMusketDrift,
                loweredPoseHoldSeconds,
                rearArmEndFrame,
                rearArmEndTime,
                backMusketAttachFrame,
                handMusketHideTime,
                forwardArmEndFrame,
                forwardArmEndTime,
                HandLoweringDuration,
                attachRigPoseError,
                forwardArmEndRigPoseError,
                leftShoulderHeightError,
                rightShoulderHeightError,
                referencePoseRotationError,
                animatedAppearanceRenderers.Length + 2,
                backMusketRenderers.Length,
                AnimationUtility.GetCurveBindings(targetClip).Length,
                waistSwordHipLocalDrift,
                waistSwordBodyFollowPositionChange,
                waistSwordBodyFollowRotationChange,
                drawContinuationClip.length,
                transitionBridgeClip.length,
                targetClip.length + transitionBridgeClip.length + drawContinuationClip.length,
                transitionBridgeEndpointPoseError,
                transitionBridgeMaxFrameYStep,
                transitionBridgeYTravel,
                drawContinuationVerticalRebase,
                drawContinuationPoseError,
                drawContinuationBackMusketMountError,
                AnimationUtility.GetCurveBindings(drawContinuationClip).Length);
        }

        private static float InspectTransitionBridge(
            Transform targetModel,
            AnimationClip changingClip,
            AnimationClip bridgeClip,
            AnimationClip drawClip,
            out float maximumFrameYStep,
            out float yTravel)
        {
            var sampledTarget = CreateSamplingClone(
                targetModel, "Ispant08TransitionBridgeInspectSample");
            maximumFrameYStep = 0f;
            yTravel = 0f;
            var endpointError = 0f;
            try
            {
                SampleClip(sampledTarget.gameObject, changingClip, changingClip.length);
                RequireSlot8VisibilityPhase(
                    sampledTarget, handVisible: false, backVisible: true,
                    "changing-to-sword hold end");
                var changingEnd = RelativeTransformMatrices(sampledTarget);

                SampleClip(sampledTarget.gameObject, bridgeClip, 0f);
                RequireSlot8VisibilityPhase(
                    sampledTarget, handVisible: false, backVisible: true,
                    "transition bridge start");
                var bridgeStart = RelativeTransformMatrices(sampledTarget);
                foreach (var pair in changingEnd)
                    endpointError = Mathf.Max(
                        endpointError, MatrixError(pair.Value, bridgeStart[pair.Key]));

                SampleClip(sampledTarget.gameObject, bridgeClip, bridgeClip.length);
                RequireSlot8VisibilityPhase(
                    sampledTarget, handVisible: false, backVisible: true,
                    "transition bridge end");
                var bridgeEnd = RelativeTransformMatrices(sampledTarget);

                SampleClip(sampledTarget.gameObject, drawClip, 0f);
                RequireSlot8DrawVisibilityPhase(sampledTarget, "draw-sword start");
                var drawStart = RelativeTransformMatrices(sampledTarget);
                foreach (var pair in bridgeEnd)
                    endpointError = Mathf.Max(
                        endpointError, MatrixError(pair.Value, drawStart[pair.Key]));

                var frameCount = Mathf.RoundToInt(bridgeClip.length * bridgeClip.frameRate);
                float? previousY = null;
                var firstY = 0f;
                var finalY = 0f;
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    SampleClip(
                        sampledTarget.gameObject,
                        bridgeClip,
                        Mathf.Min(frame / bridgeClip.frameRate, bridgeClip.length));
                    RequireSlot8VisibilityPhase(
                        sampledTarget, handVisible: false, backVisible: true,
                        "transition bridge frame " + frame);
                    var hips = RequireMappedTransform(
                        BuildUniqueTransformMap(sampledTarget), "Hips");
                    var y = ModelLocalPosition(sampledTarget, hips).y;
                    if (frame == 0)
                        firstY = y;
                    if (previousY.HasValue)
                        maximumFrameYStep = Mathf.Max(
                            maximumFrameYStep, Mathf.Abs(y - previousY.Value));
                    previousY = y;
                    finalY = y;
                }
                yTravel = Mathf.Abs(finalY - firstY);
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }
            return endpointError;
        }

        private static float InspectDrawContinuation(
            Transform sourceModel,
            AnimationClip sourceClip,
            Transform targetModel,
            AnimationClip targetClip,
            MeshRenderer sourceBackMusket,
            out float verticalRebase,
            out float backMusketMountError)
        {
            var sourceStartTime = DrawContinuationSourceStartFrame / sourceClip.frameRate;
            var expectedLength = sourceClip.length - sourceStartTime;
            if (Mathf.Abs(targetClip.length - expectedLength) > 0.0001f)
                throw new InvalidOperationException(
                    "The slot-8 draw continuation does not start at the original slot-4 first frame.");
            var sampledSource = CreateSamplingClone(
                sourceModel, "Ispant04DrawContinuationSourceSample");
            var sampledTarget = CreateSamplingClone(
                targetModel, "Ispant08DrawContinuationTargetSample");
            var sourcePaths = AnimationUtility.GetCurveBindings(sourceClip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var maximumPoseError = 0f;
            verticalRebase = 0f;
            backMusketMountError = 0f;
            try
            {
                foreach (var normalizedTime in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
                {
                    var sourceTime = sourceStartTime + normalizedTime * targetClip.length;
                    SampleClip(sampledSource.gameObject, sourceClip, sourceTime);
                    var sourceMatrices = sourcePaths.ToDictionary(
                        path => path,
                        path => LocalMatrix(
                            RequireDescendantByPath(sampledSource, path)),
                        StringComparer.Ordinal);
                    var sourcePoses = sourcePaths.ToDictionary(
                        path => path,
                        path => new LocalPose(
                            RequireDescendantByPath(sampledSource, path)),
                        StringComparer.Ordinal);

                    SampleClip(
                        sampledTarget.gameObject,
                        targetClip,
                        normalizedTime * targetClip.length);
                    RequireSlot8DrawVisibilityPhase(
                        sampledTarget,
                        "slot-4 continuation " + Num(normalizedTime));
                    var targetBones = BuildUniqueTransformMap(sampledTarget);
                    foreach (var path in sourcePaths)
                    {
                        var sourceTransform = RequireDescendantByPath(
                            sampledSource, path);
                        Transform targetTransform;
                        if (sourceTransform.name == HandSwordRootName)
                            targetTransform = sampledTarget
                                .GetComponentsInChildren<Transform>(true)
                                .Single(item => item.name == HandSwordRootName);
                        else
                            targetTransform = RequireMappedTransform(
                                targetBones, sourceTransform.name);
                        if (NormalizeBoneName(sourceTransform.name) == "Armature")
                        {
                            var currentVerticalRebase =
                                targetTransform.localPosition.y - sourceTransform.localPosition.y;
                            if (normalizedTime == 0f)
                                verticalRebase = currentVerticalRebase;
                            else
                                maximumPoseError = Mathf.Max(
                                    maximumPoseError,
                                    Mathf.Abs(currentVerticalRebase - verticalRebase));
                            var sourcePose = sourcePoses[path];
                            var expectedPosition = sourcePose.Position;
                            expectedPosition.y += verticalRebase;
                            maximumPoseError = Mathf.Max(
                                maximumPoseError,
                                MatrixError(
                                    Matrix4x4.TRS(
                                        expectedPosition,
                                        sourcePose.Rotation,
                                        sourcePose.Scale),
                                    LocalMatrix(targetTransform)));
                            continue;
                        }
                        maximumPoseError = Mathf.Max(
                            maximumPoseError,
                            MatrixError(sourceMatrices[path], LocalMatrix(targetTransform)));
                    }
                    var sampledBackMusket = RequireRenderer<MeshRenderer>(
                        sampledTarget, IntermediateBackMusketName);
                    backMusketMountError = Mathf.Max(
                        backMusketMountError,
                        Mathf.Max(
                            MatrixError(
                                LocalMatrix(sourceBackMusket.transform),
                                LocalMatrix(sampledBackMusket.transform)),
                            RendererAssetMismatch(
                                sourceBackMusket, sampledBackMusket)));
                }
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledSource.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }
            return maximumPoseError;
        }

        private static float MatchedRigPoseError(
            Transform model,
            Transform exactStaticRoot,
            IReadOnlyDictionary<string, LocalPose> animatedDefaults,
            IReadOnlyDictionary<string, LocalPose> staticDefaults)
        {
            var animatedBones = BuildUniqueTransformMap(model);
            var staticBones = BuildStaticCloneTransformMap(exactStaticRoot, animatedBones.Keys);
            if (ReferencePoseBoneNames.Any(key => !staticBones.ContainsKey(key)))
                return 1f;
            var error = 0f;
            foreach (var pair in staticBones)
            {
                var expectedRotation = NormalizeQuaternion(
                    staticDefaults[pair.Key].Rotation *
                    (Quaternion.Inverse(animatedDefaults[pair.Key].Rotation) *
                     animatedBones[pair.Key].localRotation));
                error = Mathf.Max(error, Quaternion.Angle(expectedRotation, pair.Value.localRotation));
                error = Mathf.Max(
                    error,
                    Vector3.Distance(staticDefaults[pair.Key].Position, pair.Value.localPosition));
                error = Mathf.Max(
                    error,
                    Vector3.Distance(staticDefaults[pair.Key].Scale, pair.Value.localScale));
            }
            return error;
        }

        private static float RendererAssetMismatch(Renderer expected, Renderer actual)
        {
            if (expected.GetType() != actual.GetType() || SharedMesh(expected) != SharedMesh(actual) ||
                expected.sharedMaterials.Length != actual.sharedMaterials.Length)
                return 1f;
            for (var index = 0; index < expected.sharedMaterials.Length; index++)
                if (expected.sharedMaterials[index] != actual.sharedMaterials[index])
                    return 1f;
            return MatrixError(LocalMatrix(expected.transform), LocalMatrix(actual.transform));
        }

        private static float ExactStaticCloneError(
            Transform staticModel,
            Transform targetModel,
            Transform exactStaticRoot)
        {
            var sourceTransforms = RelativeTransformMatrices(staticModel);
            var cloneTransforms = RelativeTransformMatrices(exactStaticRoot);
            if (sourceTransforms.Count != cloneTransforms.Count ||
                sourceTransforms.Keys.Any(path => !cloneTransforms.ContainsKey(path)))
                return 1f;

            var error = 0f;
            foreach (var pair in sourceTransforms)
                error = Mathf.Max(error, MatrixError(pair.Value, cloneTransforms[pair.Key]));

            var sourceRenderers = staticModel.GetComponentsInChildren<Renderer>(true).ToDictionary(
                renderer => AnimationUtility.CalculateTransformPath(renderer.transform, staticModel),
                StringComparer.Ordinal);
            var cloneRenderers = exactStaticRoot.GetComponentsInChildren<Renderer>(true).ToDictionary(
                renderer => AnimationUtility.CalculateTransformPath(renderer.transform, exactStaticRoot),
                StringComparer.Ordinal);
            if (sourceRenderers.Count != cloneRenderers.Count ||
                sourceRenderers.Keys.Any(path => !cloneRenderers.ContainsKey(path)))
                return 1f;
            foreach (var pair in sourceRenderers)
                error = Mathf.Max(
                    error,
                    RendererAssetMismatch(pair.Value, cloneRenderers[pair.Key]));

            if (staticModel.parent != null && targetModel.parent != null)
            {
                var sourceInSlot = staticModel.parent.worldToLocalMatrix * staticModel.localToWorldMatrix;
                var cloneInSlot = targetModel.parent.worldToLocalMatrix * exactStaticRoot.localToWorldMatrix;
                error = Mathf.Max(error, MatrixError(sourceInSlot, cloneInSlot));
            }
            return error;
        }

        private static Dictionary<string, Matrix4x4> RelativeTransformMatrices(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true).ToDictionary(
                item => AnimationUtility.CalculateTransformPath(item, root),
                item => root.worldToLocalMatrix * item.localToWorldMatrix,
                StringComparer.Ordinal);
        }

        private static void RequireSlot8VisibilityPhase(
            Transform model,
            bool handVisible,
            bool backVisible,
            string sampleLabel)
        {
            if (model.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == ExactStaticHoldRootName))
                throw new InvalidOperationException(
                    "The legacy exact-static hold clone remains at " + sampleLabel + ".");
            var handMusket = RequireRenderer<MeshRenderer>(model, HandMusketRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(model, IntermediateBackMusketName);
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var handSword = RequireRenderer<MeshRenderer>(model, HandSwordRendererName);
            var appearanceRenderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != handMusket &&
                                   renderer != backMusket &&
                                   renderer != waistSword &&
                                   renderer != handSword)
                .ToArray();
            if (appearanceRenderers.Length != 3 ||
                appearanceRenderers.Any(renderer => !renderer.enabled) ||
                handMusket.enabled != handVisible ||
                backMusket.enabled != backVisible ||
                !waistSword.enabled ||
                handSword.enabled)
                throw new InvalidOperationException(
                    "The slot-8 renderer visibility differs at " + sampleLabel + ".");
        }

        private static void RequireSlot8DrawVisibilityPhase(
            Transform model,
            string sampleLabel)
        {
            var handMusket = RequireRenderer<MeshRenderer>(model, HandMusketRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(model, IntermediateBackMusketName);
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var handSword = RequireRenderer<MeshRenderer>(model, HandSwordRendererName);
            var appearanceRenderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != handMusket &&
                                   renderer != backMusket &&
                                   renderer != waistSword &&
                                   renderer != handSword)
                .ToArray();
            if (appearanceRenderers.Length != 3 ||
                appearanceRenderers.Any(renderer => !renderer.enabled) ||
                handMusket.enabled || !backMusket.enabled ||
                waistSword.enabled || !handSword.enabled)
                throw new InvalidOperationException(
                    "The slot-8 slot-4 continuation visibility differs at " + sampleLabel + ".");
        }

        private static void RequireExactSequenceController(
            AnimatorController controller,
            AnimationClip changingClip,
            AnimationClip bridgeClip,
            AnimationClip drawClip)
        {
            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states.Select(item => item.state).ToArray();
            if (states.Length != 1)
                throw new InvalidOperationException(
                    "The slot-8 sequence controller must contain exactly one continuous state.");
            var changingState = states.SingleOrDefault(item => item.name == StateName) ??
                                throw new InvalidOperationException(
                                    "The slot-8 changing state is missing.");
            var continuousClip = RequireContinuousSequenceClip(controller);
            var expectedLength = changingClip.length + bridgeClip.length + drawClip.length;
            if (changingState.motion != continuousClip ||
                stateMachine.defaultState != changingState ||
                Mathf.Abs(changingState.speed - 1f) > 0.000001f ||
                changingState.transitions.Length != 0 ||
                Mathf.Abs(continuousClip.length - expectedLength) > 0.0001f ||
                !AnimationUtility.GetAnimationClipSettings(continuousClip).loopTime)
                throw new InvalidOperationException(
                    "The slot-8 continuous sequence state, speed, length, loop, or transition count differs.");
        }

        private static AnimationClip RequireContinuousSequenceClip(AnimatorController controller)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                .OfType<AnimationClip>()
                .Where(item => item.name == ContinuousSequenceClipName)
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    "The slot-8 controller must contain exactly one continuous sequence clip.");
            return clips[0];
        }

        private static void RequireExactExitTransition(
            AnimatorState source,
            AnimatorState destination)
        {
            if (source.transitions.Length != 1)
                throw new InvalidOperationException(
                    "A slot-8 sequence state must contain exactly one transition.");
            var transition = source.transitions[0];
            if (transition.destinationState != destination || !transition.hasExitTime ||
                Mathf.Abs(transition.exitTime - 1f) > 0.000001f ||
                !transition.hasFixedDuration ||
                Mathf.Abs(transition.duration) > 0.000001f ||
                transition.conditions.Length != 0)
                throw new InvalidOperationException(
                    "The slot-8 sequence transition is not an exact unblended exit transition.");
        }

        private static float RendererVisibilitySwitchTime(
            AnimationClip clip,
            Transform model,
            Renderer renderer,
            bool visibleAfterSwitch)
        {
            var binding = EditorCurveBinding.FloatCurve(
                AnimationUtility.CalculateTransformPath(renderer.transform, model),
                renderer.GetType(),
                "m_Enabled");
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                        throw new InvalidOperationException(
                            "A slot-8 renderer visibility curve is missing: " + renderer.name + ".");
            var expected = visibleAfterSwitch ? 1f : 0f;
            var keys = curve.keys;
            for (var index = 1; index < keys.Length; index++)
                if (Mathf.Abs(keys[index].value - expected) <= 0.000001f &&
                    Mathf.Abs(keys[index - 1].value - expected) > 0.000001f)
                    return keys[index].time;
            throw new InvalidOperationException(
                "A slot-8 renderer visibility switch is missing: " + renderer.name + ".");
        }

        private static void CaptureRearArmDiagnostic(
            Transform targetModel,
            AnimationClip targetClip,
            float sourceMotionLength,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException(
                                          "The slot-8 diagnostic folder is invalid."));
            const int captureLayer = 31;
            const int panelWidth = 480;
            const int panelHeight = 540;
            const int columns = 11;
            const int firstFrame = 135;
            const int frameStep = 1;
            const int sampleCount = 11;
            const int rows = 2;
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant08RearArmDiagnostic");
            SetLayerRecursive(sampledTarget, captureLayer);
            if (sampledTarget.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name == ExactStaticHoldRootName))
                throw new InvalidOperationException(
                    "The diagnostic target contains the legacy exact-static hold clone.");
            var intermediateBackMusket = RequireRenderer<MeshRenderer>(
                sampledTarget, IntermediateBackMusketName);
            var handSword = RequireRenderer<MeshRenderer>(
                sampledTarget, HandSwordRendererName);
            var originalMotionRenderers = sampledTarget.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer != intermediateBackMusket &&
                                   renderer != handSword).ToArray();
            var hiddenRenderers = new Renderer[] { intermediateBackMusket, handSword };
            if (originalMotionRenderers.Length != 5)
                throw new InvalidOperationException(
                    "The diagnostic must show the three animated appearance renderers, hand musket, " +
                    "and left-waist long sword.");

            var cameraObject = new GameObject("Ispant08RearArmDiagnosticCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var strip = new Texture2D(
                panelWidth * columns, panelHeight * rows, TextureFormat.RGB24, false);
            var renderTarget = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            var timingLines = new List<string>
            {
                "Frame,Time,RightHandModelLocalX,RightHandModelLocalY,RightHandModelLocalZ"
            };
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << captureLayer;
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.targetTexture = renderTarget;

                for (var index = 0; index < sampleCount; index++)
                {
                    var frame = firstFrame + index * frameStep;
                    var time = Mathf.Min(sourceMotionLength, frame / targetClip.frameRate);
                    SampleClip(sampledTarget.gameObject, targetClip, time);
                    var sampledBones = BuildUniqueTransformMap(sampledTarget);
                    var rightHandPosition = ModelLocalPosition(
                        sampledTarget, RequireMappedTransform(sampledBones, "RightHand"));
                    timingLines.Add(
                        frame + "," + Num(time) + "," +
                        Num(rightHandPosition.x) + "," +
                        Num(rightHandPosition.y) + "," +
                        Num(rightHandPosition.z));
                    SetEnabled(originalMotionRenderers, true);
                    SetEnabled(hiddenRenderers, false);
                    var batch = index / columns;
                    var column = index % columns;
                    var frontRow = rows - 1 - batch * 2;
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        column, frontRow, Vector3.right, panelWidth, panelHeight);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        column, frontRow - 1, Vector3.left, panelWidth, panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
                File.WriteAllLines(
                    Path.ChangeExtension(destination, ".timing.txt"),
                    timingLines);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                renderTarget.Release();
                UnityEngine.Object.DestroyImmediate(renderTarget);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureReview(
            Transform targetModel,
            AnimationClip targetClip,
            AnimationClip transitionBridgeClip,
            AnimationClip drawContinuationClip,
            float backMusketAttachTime,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("The slot-8 capture folder is invalid."));
            const int captureLayer = 31;
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant08Capture");
            SetLayerRecursive(sampledTarget, captureLayer);
            var targetRenderers = sampledTarget.GetComponentsInChildren<Renderer>(true);
            var cameraObject = new GameObject("Ispant08ChangingToSwordReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            const int panelWidth = 320;
            const int panelHeight = 540;
            // Four changing samples, seven dense bridge samples, five draw samples, and loop start.
            const int columns = 17;
            const int rows = 2;
            var strip = new Texture2D(
                panelWidth * columns, panelHeight * rows, TextureFormat.RGB24, false);
            var renderTarget = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                SetEnabled(targetRenderers, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << captureLayer;
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.targetTexture = renderTarget;

                var changingTimes = new[]
                {
                    Mathf.Max(0f, backMusketAttachTime - 1f / targetClip.frameRate),
                    backMusketAttachTime,
                    DirectlyObservedForwardArmEndFrame / targetClip.frameRate,
                    DirectlyObservedForwardArmEndFrame / targetClip.frameRate +
                    HandLoweringDuration
                };
                for (var index = 0; index < changingTimes.Length; index++)
                {
                    SampleClip(
                        sampledTarget.gameObject, targetClip, changingTimes[index]);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        index, 1, Vector3.forward, panelWidth, panelHeight);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        index, 0, Vector3.back, panelWidth, panelHeight);
                    SetEnabled(targetRenderers, false);
                }

                var bridgeNormalizedTimes = new[]
                {
                    0f, 1f / 6f, 2f / 6f, 3f / 6f, 4f / 6f, 5f / 6f, 1f
                };
                for (var index = 0; index < bridgeNormalizedTimes.Length; index++)
                {
                    SampleClip(
                        sampledTarget.gameObject,
                        transitionBridgeClip,
                        bridgeNormalizedTimes[index] * transitionBridgeClip.length);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        changingTimes.Length + index, 1,
                        Vector3.forward, panelWidth, panelHeight);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        changingTimes.Length + index, 0,
                        Vector3.back, panelWidth, panelHeight);
                    SetEnabled(targetRenderers, false);
                }

                var drawTimes = new[]
                {
                    0f,
                    1f / drawContinuationClip.frameRate,
                    2f / drawContinuationClip.frameRate,
                    drawContinuationClip.length * 0.5f,
                    drawContinuationClip.length
                };
                for (var index = 0; index < drawTimes.Length; index++)
                {
                    SampleClip(
                        sampledTarget.gameObject,
                        drawContinuationClip,
                        drawTimes[index]);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        changingTimes.Length + bridgeNormalizedTimes.Length + index, 1,
                        Vector3.forward, panelWidth, panelHeight);
                    RenderPanel(
                        camera, sampledTarget, panel, strip, renderTarget,
                        changingTimes.Length + bridgeNormalizedTimes.Length + index, 0,
                        Vector3.back, panelWidth, panelHeight);
                    SetEnabled(targetRenderers, false);
                }

                SampleClip(sampledTarget.gameObject, targetClip, 0f);
                RenderPanel(
                    camera, sampledTarget, panel, strip, renderTarget,
                    columns - 1, 1, Vector3.forward, panelWidth, panelHeight);
                RenderPanel(
                    camera, sampledTarget, panel, strip, renderTarget,
                    columns - 1, 0, Vector3.back, panelWidth, panelHeight);
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                renderTarget.Release();
                UnityEngine.Object.DestroyImmediate(renderTarget);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RenderPanel(
            Camera camera,
            Transform model,
            Texture2D panel,
            Texture2D strip,
            RenderTexture target,
            int column,
            int row,
            Vector3 viewDirection,
            int width,
            int height)
        {
            var bounds = CombinedBounds(model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray());
            FrameCamera(camera, bounds, width / (float)height, viewDirection);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The slot-8 review contains magenta shader fallback.");
            strip.SetPixels32(column * width, row * height, width, height, pixels);
        }

        private static void FrameCamera(
            Camera camera,
            Bounds bounds,
            float aspect,
            Vector3 viewDirection)
        {
            camera.aspect = aspect;
            var height = Mathf.Max(bounds.size.y, 0.1f);
            var verticalDistance = (height * 0.5f) /
                                   Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontalDistance = (Mathf.Max(bounds.size.x, 0.1f) * 0.5f) /
                                     Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(verticalDistance, horizontalDistance) * 1.25f;
            camera.transform.position = bounds.center + viewDirection.normalized * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position, Vector3.up);
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            if (renderers.Length == 0)
                throw new InvalidOperationException("No visible renderers were found for slot-8 review.");
            var result = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("The slot-8 inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + TargetSlotName,
                "SourceFbx=" + SourceFbxPath,
                "ProjectFbx=" + ProjectFbxPath,
                "SourceSha256=" + SourceSha256,
                "EmbeddedClip=" + ImportedClipName,
                "RetargetedClip=" + RetargetedClipName,
                "ClipLengthSeconds=" + Num(metrics.ClipLength),
                "DrawContinuationSource=Ispant_04_DrawSword current animation",
                "DrawContinuationSourceClip=" + DrawSwordClipPath,
                "DrawContinuationClip=" + DrawContinuationClipName,
                "DrawContinuationSourceStartFrame=" + DrawContinuationSourceStartFrame,
                "DrawContinuationSourceStartTime=" +
                Num(DrawContinuationSourceStartFrame / 25f),
                "DrawContinuationRemovedDuplicatePreDrawSeconds=" +
                Num(DrawContinuationSourceStartFrame / 25f),
                "DrawContinuationLengthSeconds=" + Num(metrics.DrawContinuationLength),
                "TransitionBridgeClip=" + TransitionBridgeClipName,
                "TransitionBridgeLengthSeconds=" + Num(metrics.TransitionBridgeLength),
                "TransitionBridgeInterpolation=LinearContinuousWithoutEndpointEase",
                "SequenceCycleLengthSeconds=" + Num(metrics.SequenceCycleLength),
                "RetainedSourceMotionLengthSeconds=" +
                Num(DirectlyObservedStaticReturnSourceEndFrame / metrics.FrameRate),
                "HandLoweringSourceStartFrame=" + DirectlyObservedForwardArmEndFrame,
                "HandLoweringSourceEndFrame=" + DirectlyObservedStaticReturnSourceEndFrame,
                "HandLoweringSourceEndTime=" +
                Num(DirectlyObservedStaticReturnSourceEndFrame / metrics.FrameRate),
                "HandLoweringSeconds=" + Num(metrics.HandLoweringSeconds),
                "LoweredPoseHoldSeconds=" + Num(metrics.LoweredPoseHoldSeconds),
                "LoweredPoseHoldRemoved=True",
                "FrameRate=" + Num(metrics.FrameRate),
                "Loop=True",
                "PlaybackSpeed=1",
                "StaticAppearanceSource=Ispant_01_Static via exact slot-7 synchronized shared assets",
                "AppearanceAssetMismatch=" + Num(metrics.AppearanceAssetMismatch),
                "VisibleRendererCount=" + metrics.VisibleRendererCount,
                "SeparateBackMusketRendererCount=" + metrics.BackMusketRendererCount,
                "IntermediateBackMusketSource=Ispant_06_SheathSwordDrawMusket exact shared mesh/material and Spine2 local mount",
                "HandMusketSource=Ispant_07_BreakthroughMusketAimFire",
                "HandMusketParent=mixamorig:RightHand",
                "MusketMountError=" + Num(metrics.MusketMountError),
                "PreStowMusketMountError=" + Num(metrics.PreStowMusketMountError),
                "RearArmMotionEndFrame=" + metrics.RearArmMotionEndFrame,
                "RearArmMotionEndTime=" + Num(metrics.RearArmMotionEndTime),
                "BackMusketAttachFrame=" + metrics.BackMusketAttachFrame,
                "BackMusketAttachTime=" + Num(metrics.BackMusketAttachTime),
                "ForwardArmMotionEndFrame=" + metrics.ForwardArmMotionEndFrame,
                "ForwardArmMotionEndTime=" + Num(metrics.ForwardArmMotionEndTime),
                "AttachRigPoseError=" + Num(metrics.AttachRigPoseError),
                "ForwardArmEndRigPoseError=" + Num(metrics.ForwardArmEndRigPoseError),
                "HandMusketVisibleAfterAttach=False",
                "BackAndHandMusketSimultaneouslyVisible=False",
                "LeftWaistSwordSource=Ispant_07_BreakthroughMusketAimFire exact shared mesh/material and local mount",
                "LeftWaistSwordParent=mixamorig:Hips",
                "LeftWaistSwordHipLocalMatrixDrift=" + Num(metrics.WaistSwordHipLocalDrift),
                "LeftWaistSwordBodyFollowPositionChange=" +
                Num(metrics.WaistSwordBodyFollowPositionChange),
                "LeftWaistSwordBodyFollowRotationChangeDegrees=" +
                Num(metrics.WaistSwordBodyFollowRotationChange),
                "RightHandSwordSource=Ispant_04_DrawSword exact shared mesh/material and RightHand local mount",
                "DrawContinuationVerticalRebase=" +
                Num(metrics.DrawContinuationVerticalRebase),
                "DrawContinuationPoseError=" + Num(metrics.DrawContinuationPoseError),
                "DrawContinuationBackMusketMountError=" +
                Num(metrics.DrawContinuationBackMusketMountError),
                "DrawContinuationCurveCount=" + metrics.DrawContinuationCurveCount,
                "TransitionBridgePoseSource=exact slot-8 hand-lowering end and exact slot-4 frame-14 final-waist pose only",
                "TransitionBridgeEndpointPoseError=" +
                Num(metrics.TransitionBridgeEndpointPoseError),
                "TransitionBridgeMaxFrameYStep=" +
                Num(metrics.TransitionBridgeMaxFrameYStep),
                "TransitionBridgeYTravel=" + Num(metrics.TransitionBridgeYTravel),
                "SequenceTransitions=ChangingToSword->0.3sTransitionBridge->DrawSwordFromSourceFrame14->ChangingToSword exact endpoint transitions",
                "StaticModelRendererTransition=False",
                "TransitionStartPoseSource=Ispant_ChangingToSword_Mixamo_Retargeted frame 230 compressed endpoint",
                "FinalBackMusketSource=Ispant_06_SheathSwordDrawMusket exact shared mesh/material and Spine2 local mount",
                "FinalBackMusketMountError=" + Num(metrics.FinalBackMusketMountError),
                "AnimatedHoldPoseDrift=" + Num(metrics.AnimatedHoldPoseDrift),
                "AnimatedHoldMusketDrift=" + Num(metrics.AnimatedHoldMusketDrift),
                "ArmHeightSource=Ispant_07_BreakthroughMusketAimFire start pose",
                "LeftShoulderStartHeightError=" + Num(metrics.LeftShoulderStartHeightError),
                "RightShoulderStartHeightError=" + Num(metrics.RightShoulderStartHeightError),
                "ReferencePoseRotationErrorDegrees=" + Num(metrics.ReferencePoseRotationErrorDegrees),
                "RetargetedCurveCount=" + metrics.RetargetedCurveCount,
                "AnimatorApplyRootMotion=False",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + CapturePath
            });
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectFbxPath, SourceSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            var absolute = Absolute(path);
            if (!File.Exists(absolute))
                throw new FileNotFoundException("Required Ispant FBX is missing.", absolute);
            using var stream = File.OpenRead(absolute);
            using var sha = SHA256.Create();
            var actual = string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("X2")));
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("The Ispant FBX hash differs: " + path + ".");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene for slot-8 work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes; preserve them before slot-8 work.");
            return scene;
        }

        private static bool ClearKnownFailedApplyDirtyState(Scene scene)
        {
            if (!scene.isDirty)
                return false;
            var placement = RequirePlacement(scene);
            var targetSlot = RequireSlot(placement.transform, TargetSlotName, 7);
            if (targetSlot.childCount != 1)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes outside the known failed slot-8 temporary replacement.");
            if (targetSlot.GetChild(0).name == TargetModelName)
            {
                RestoreOtherSlotsFromSavedScene(scene, placement, targetSlot);
                return true;
            }
            if (targetSlot.GetChild(0).name != OriginalTargetModelName ||
                targetSlot.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name == TargetModelName))
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes outside the known failed slot-8 temporary replacement.");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not clear the known failed slot-8 temporary replacement state.");
            return false;
        }

        private static void RestoreOtherSlotsFromSavedScene(
            Scene liveScene,
            GameObject livePlacement,
            Transform liveTargetSlot)
        {
            var previewScene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var savedPlacement = RequirePlacement(previewScene);
                if (livePlacement.transform.childCount != savedPlacement.transform.childCount)
                    throw new InvalidOperationException(
                        "The live Ispant placement cannot be compared with the saved scene.");
                for (var index = 0; index < livePlacement.transform.childCount; index++)
                {
                    var liveSlot = livePlacement.transform.GetChild(index);
                    if (liveSlot == liveTargetSlot)
                        continue;
                    RestoreHierarchyState(
                        liveSlot,
                        savedPlacement.transform.GetChild(index));
                }
                RequireEqual(
                    OtherRootSignatures(previewScene, savedPlacement),
                    OtherRootSignatures(liveScene, livePlacement),
                    "A scene root outside the Ispant placement differs from the saved scene.");
                for (var index = 0; index < livePlacement.transform.childCount; index++)
                {
                    var liveSlot = livePlacement.transform.GetChild(index);
                    if (liveSlot == liveTargetSlot)
                        continue;
                    var savedSlot = savedPlacement.transform.GetChild(index);
                    var expected = RecursiveSignature(savedSlot);
                    var actual = RecursiveSignature(liveSlot);
                    if (expected == actual)
                        continue;
                    var expectedLines = expected.Split('\n');
                    var actualLines = actual.Split('\n');
                    var differenceIndex = Enumerable.Range(
                            0, Mathf.Min(expectedLines.Length, actualLines.Length))
                        .FirstOrDefault(line => expectedLines[line] != actualLines[line]);
                    throw new InvalidOperationException(
                        "An Ispant slot outside slot 8 could not be restored to the saved scene. " +
                        "SlotIndex=" + index +
                        ", Slot=" + liveSlot.name +
                        ", DifferenceLine=" + differenceIndex +
                        ", Saved=" + expectedLines[Mathf.Min(differenceIndex, expectedLines.Length - 1)] +
                        ", Live=" + actualLines[Mathf.Min(differenceIndex, actualLines.Length - 1)] + ".");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static void RestoreHierarchyState(Transform live, Transform saved)
        {
            if (live.name != saved.name ||
                live.childCount != saved.childCount ||
                live.GetComponents<Component>().Length != saved.GetComponents<Component>().Length)
                throw new InvalidOperationException(
                    "A non-slot-8 hierarchy differs structurally from the saved scene: " + live.name + ".");
            live.SetLocalPositionAndRotation(saved.localPosition, saved.localRotation);
            live.localScale = saved.localScale;
            live.gameObject.SetActive(saved.gameObject.activeSelf);
            var liveRenderers = live.GetComponents<Renderer>();
            var savedRenderers = saved.GetComponents<Renderer>();
            if (liveRenderers.Length != savedRenderers.Length)
                throw new InvalidOperationException(
                    "A non-slot-8 renderer structure differs from the saved scene: " + live.name + ".");
            for (var index = 0; index < liveRenderers.Length; index++)
                liveRenderers[index].enabled = savedRenderers[index].enabled;
            for (var index = 0; index < live.childCount; index++)
                RestoreHierarchyState(live.GetChild(index), saved.GetChild(index));
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var matches = scene.GetRootGameObjects().Where(item => item.name == PlacementRootName).ToArray();
            if (matches.Length != 1 || matches[0].transform.childCount != ExpectedSlots)
                throw new InvalidOperationException("The approved Ispant placement contract differs.");
            return matches[0];
        }

        private static Transform RequireSlot(Transform placement, string name, int index)
        {
            if (index < 0 || index >= placement.childCount || placement.GetChild(index).name != name)
                throw new InvalidOperationException("The required Ispant slot differs: " + name + ".");
            return placement.GetChild(index);
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return Enumerable.Range(0, parent.childCount).Select(parent.GetChild)
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       "Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static Transform RequireDescendantByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;
            return root.Find(path) ??
                   throw new InvalidOperationException(
                       "Required slot-4 draw-sword transform path is missing: " + path + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required Ispant renderer is missing: " + name + ".");
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

        private static Dictionary<string, Transform> BuildUniqueTransformMap(Transform root)
        {
            var result = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            var hipsMatches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => !IsInsideExactStaticHold(item, root))
                .Where(item => string.Equals(
                    NormalizeBoneName(item.name), "Hips", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (hipsMatches.Length != 1)
                throw new InvalidOperationException("The slot-8 rig must contain exactly one Hips skeleton root.");
            var armature = hipsMatches[0].parent;
            if (armature == null ||
                !string.Equals(NormalizeBoneName(armature.name), "Armature", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The slot-8 rig Armature parent is missing.");
            result["Armature"] = armature;
            foreach (var item in hipsMatches[0].GetComponentsInChildren<Transform>(true))
            {
                if (item != hipsMatches[0] &&
                    item.name.IndexOf("mixamorig:", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var key = NormalizeBoneName(item.name);
                if (result.TryGetValue(key, out var existing) && existing != item)
                    throw new InvalidOperationException(
                        "The slot-8 rig contains duplicate normalized bone names: " + key + ".");
                result[key] = item;
            }
            return result;
        }

        private static Dictionary<string, Transform> BuildStaticCloneTransformMap(
            Transform root,
            IEnumerable<string> requiredBoneNames)
        {
            var result = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
            var required = new HashSet<string>(
                requiredBoneNames.Select(NormalizeBoneName),
                StringComparer.OrdinalIgnoreCase);
            var hipsMatches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    NormalizeBoneName(item.name), "Hips", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hipsMatches.Length != 1 || hipsMatches[0].parent == null)
                throw new InvalidOperationException(
                    "The exact static clone must contain one Hips skeleton root and its actual parent.");
            result["Armature"] = hipsMatches[0].parent;
            foreach (var item in hipsMatches[0].GetComponentsInChildren<Transform>(true))
            {
                var key = NormalizeBoneName(item.name);
                if (!required.Contains(key))
                    continue;
                if (result.TryGetValue(key, out var existing) && existing != item)
                    throw new InvalidOperationException(
                        "The exact static clone contains duplicate normalized bone names: " + key + ".");
                result[key] = item;
            }
            return result;
        }

        private static bool IsInsideExactStaticHold(Transform item, Transform modelRoot)
        {
            var current = item;
            while (current != null && current != modelRoot)
            {
                if (current.name == ExactStaticHoldRootName)
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static Transform RequireMappedTransform(
            IReadOnlyDictionary<string, Transform> map,
            string name)
        {
            var key = NormalizeBoneName(name);
            return map.TryGetValue(key, out var result)
                ? result
                : throw new InvalidOperationException("The slot-8 rig is missing bone: " + key + ".");
        }

        private static string RetargetPath(
            string sourcePath,
            Transform targetModel,
            IReadOnlyDictionary<string, Transform> targetBones)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return string.Empty;
            var key = NormalizeBoneName(sourcePath.Split('/').Last());
            if (!targetBones.TryGetValue(key, out var target))
                throw new InvalidOperationException(
                    "The calibrated Ispant rig is missing a changing-to-sword animation bone: " + key + ".");
            return AnimationUtility.CalculateTransformPath(target, targetModel);
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
            var normalizedNumber = int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
            return withoutNamespace.Substring(0, digitStart) +
                   normalizedNumber.ToString(CultureInfo.InvariantCulture);
        }

        private static Quaternion NormalizeQuaternion(Quaternion value)
        {
            var magnitude = Mathf.Sqrt(
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude <= 0.000001f)
                throw new InvalidOperationException("A changing-to-sword animation quaternion is invalid.");
            return new Quaternion(
                value.x / magnitude, value.y / magnitude,
                value.z / magnitude, value.w / magnitude);
        }

        private static Transform CreateSamplingClone(Transform source, string name)
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject);
            clone.name = name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.SetActive(true);
            return clone.transform;
        }

        private static void SampleClip(GameObject model, AnimationClip clip, float time)
        {
            StopSampling();
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, clip, Mathf.Clamp(time, 0f, clip.length));
            AnimationMode.EndSampling();
        }

        private static void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private static void SetLayerRecursive(Transform root, int layer)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void SetEnabled(IEnumerable<Renderer> renderers, bool enabled)
        {
            foreach (var renderer in renderers)
                renderer.enabled = enabled;
        }

        private static Vector3 ModelLocalPosition(Transform model, Transform item)
        {
            return model.InverseTransformPoint(item.position);
        }

        private static void CopyLocalTransform(Transform source, Transform target)
        {
            target.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
            target.localScale = source.localScale;
        }

        private static void DecomposeMatrix(
            Matrix4x4 matrix,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale)
        {
            position = matrix.GetColumn(3);
            var right = (Vector3)matrix.GetColumn(0);
            var up = (Vector3)matrix.GetColumn(1);
            var forward = (Vector3)matrix.GetColumn(2);
            scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
            if (scale.x <= 0.000001f || scale.y <= 0.000001f || scale.z <= 0.000001f)
                throw new InvalidOperationException("The static back-musket target has a zero scale axis.");
            if (Vector3.Dot(Vector3.Cross(right, up), forward) < 0f)
                scale.x = -scale.x;
            rotation = Quaternion.LookRotation(forward / scale.z, up / scale.y);
        }

        private static Matrix4x4 LocalMatrix(Transform transform)
        {
            return Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        }

        private static float MatrixError(Matrix4x4 expected, Matrix4x4 actual)
        {
            var error = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                error = Mathf.Max(error, Mathf.Abs(expected[row, column] - actual[row, column]));
            return error;
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects().Where(item => item != placement)
                .Select(item => RecursiveSignature(item.transform)).ToArray();
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount).Select(placement.GetChild)
                .Where(item => item != targetSlot).Select(RecursiveSignature).ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var parts = new List<string>();
            void Visit(Transform item)
            {
                parts.Add(item.name + "|" + item.localPosition.ToString("F6") + "|" +
                          item.localRotation.ToString("F6") + "|" +
                          item.localScale.ToString("F6") + "|" +
                          item.gameObject.activeSelf + "|" + item.GetComponents<Component>().Length);
                foreach (Transform child in item)
                    Visit(child);
            }
            Visit(root);
            return string.Join("\n", parts);
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (expected.Length != actual.Length || !expected.SequenceEqual(actual))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
            }

            public void Restore()
            {
                if (target == null)
                    return;
                target.SetLocalPositionAndRotation(position, rotation);
                target.localScale = scale;
            }

            public bool Matches(float tolerance)
            {
                return target != null &&
                       Vector3.Distance(position, target.localPosition) <= tolerance &&
                       Quaternion.Angle(rotation, target.localRotation) <= tolerance &&
                       Vector3.Distance(scale, target.localScale) <= tolerance;
            }
        }

        private readonly struct LocalPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public LocalPose(Transform transform)
                : this(transform.localPosition, transform.localRotation, transform.localScale)
            {
            }

            public LocalPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public void Apply(Transform transform)
            {
                transform.SetLocalPositionAndRotation(Position, Rotation);
                transform.localScale = Scale;
            }
        }

        private readonly struct Metrics
        {
            public readonly float ClipLength;
            public readonly float FrameRate;
            public readonly float AppearanceAssetMismatch;
            public readonly float MusketMountError;
            public readonly float PreStowMusketMountError;
            public readonly float FinalBackMusketMountError;
            public readonly float AnimatedHoldPoseDrift;
            public readonly float AnimatedHoldMusketDrift;
            public readonly float LoweredPoseHoldSeconds;
            public readonly int RearArmMotionEndFrame;
            public readonly float RearArmMotionEndTime;
            public readonly int BackMusketAttachFrame;
            public readonly float BackMusketAttachTime;
            public readonly int ForwardArmMotionEndFrame;
            public readonly float ForwardArmMotionEndTime;
            public readonly float HandLoweringSeconds;
            public readonly float AttachRigPoseError;
            public readonly float ForwardArmEndRigPoseError;
            public readonly float LeftShoulderStartHeightError;
            public readonly float RightShoulderStartHeightError;
            public readonly float ReferencePoseRotationErrorDegrees;
            public readonly int VisibleRendererCount;
            public readonly int BackMusketRendererCount;
            public readonly int RetargetedCurveCount;
            public readonly float WaistSwordHipLocalDrift;
            public readonly float WaistSwordBodyFollowPositionChange;
            public readonly float WaistSwordBodyFollowRotationChange;
            public readonly float DrawContinuationLength;
            public readonly float TransitionBridgeLength;
            public readonly float SequenceCycleLength;
            public readonly float TransitionBridgeEndpointPoseError;
            public readonly float TransitionBridgeMaxFrameYStep;
            public readonly float TransitionBridgeYTravel;
            public readonly float DrawContinuationVerticalRebase;
            public readonly float DrawContinuationPoseError;
            public readonly float DrawContinuationBackMusketMountError;
            public readonly int DrawContinuationCurveCount;

            public Metrics(
                float clipLength,
                float frameRate,
                float appearanceAssetMismatch,
                float musketMountError,
                float preStowMusketMountError,
                float finalBackMusketMountError,
                float animatedHoldPoseDrift,
                float animatedHoldMusketDrift,
                float loweredPoseHoldSeconds,
                int rearArmMotionEndFrame,
                float rearArmMotionEndTime,
                int backMusketAttachFrame,
                float backMusketAttachTime,
                int forwardArmMotionEndFrame,
                float forwardArmMotionEndTime,
                float handLoweringSeconds,
                float attachRigPoseError,
                float forwardArmEndRigPoseError,
                float leftShoulderStartHeightError,
                float rightShoulderStartHeightError,
                float referencePoseRotationErrorDegrees,
                int visibleRendererCount,
                int backMusketRendererCount,
                int retargetedCurveCount,
                float waistSwordHipLocalDrift,
                float waistSwordBodyFollowPositionChange,
                float waistSwordBodyFollowRotationChange,
                float drawContinuationLength,
                float transitionBridgeLength,
                float sequenceCycleLength,
                float transitionBridgeEndpointPoseError,
                float transitionBridgeMaxFrameYStep,
                float transitionBridgeYTravel,
                float drawContinuationVerticalRebase,
                float drawContinuationPoseError,
                float drawContinuationBackMusketMountError,
                int drawContinuationCurveCount)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                AppearanceAssetMismatch = appearanceAssetMismatch;
                MusketMountError = musketMountError;
                PreStowMusketMountError = preStowMusketMountError;
                FinalBackMusketMountError = finalBackMusketMountError;
                AnimatedHoldPoseDrift = animatedHoldPoseDrift;
                AnimatedHoldMusketDrift = animatedHoldMusketDrift;
                LoweredPoseHoldSeconds = loweredPoseHoldSeconds;
                RearArmMotionEndFrame = rearArmMotionEndFrame;
                RearArmMotionEndTime = rearArmMotionEndTime;
                BackMusketAttachFrame = backMusketAttachFrame;
                BackMusketAttachTime = backMusketAttachTime;
                ForwardArmMotionEndFrame = forwardArmMotionEndFrame;
                ForwardArmMotionEndTime = forwardArmMotionEndTime;
                HandLoweringSeconds = handLoweringSeconds;
                AttachRigPoseError = attachRigPoseError;
                ForwardArmEndRigPoseError = forwardArmEndRigPoseError;
                LeftShoulderStartHeightError = leftShoulderStartHeightError;
                RightShoulderStartHeightError = rightShoulderStartHeightError;
                ReferencePoseRotationErrorDegrees = referencePoseRotationErrorDegrees;
                VisibleRendererCount = visibleRendererCount;
                BackMusketRendererCount = backMusketRendererCount;
                RetargetedCurveCount = retargetedCurveCount;
                WaistSwordHipLocalDrift = waistSwordHipLocalDrift;
                WaistSwordBodyFollowPositionChange = waistSwordBodyFollowPositionChange;
                WaistSwordBodyFollowRotationChange = waistSwordBodyFollowRotationChange;
                DrawContinuationLength = drawContinuationLength;
                TransitionBridgeLength = transitionBridgeLength;
                SequenceCycleLength = sequenceCycleLength;
                TransitionBridgeEndpointPoseError = transitionBridgeEndpointPoseError;
                TransitionBridgeMaxFrameYStep = transitionBridgeMaxFrameYStep;
                TransitionBridgeYTravel = transitionBridgeYTravel;
                DrawContinuationVerticalRebase = drawContinuationVerticalRebase;
                DrawContinuationPoseError = drawContinuationPoseError;
                DrawContinuationBackMusketMountError =
                    drawContinuationBackMusketMountError;
                DrawContinuationCurveCount = drawContinuationCurveCount;
            }
        }

        private readonly struct RearArmAttachTiming
        {
            public readonly int RearArmEndFrame;
            public readonly float RearArmEndTime;
            public readonly int AttachFrame;
            public readonly float AttachTime;
            public readonly float HandToBackTargetDistance;

            public RearArmAttachTiming(
                int rearArmEndFrame,
                float rearArmEndTime,
                int attachFrame,
                float attachTime,
                float handToBackTargetDistance)
            {
                RearArmEndFrame = rearArmEndFrame;
                RearArmEndTime = rearArmEndTime;
                AttachFrame = attachFrame;
                AttachTime = attachTime;
                HandToBackTargetDistance = handToBackTargetDistance;
            }
        }
    }
}
