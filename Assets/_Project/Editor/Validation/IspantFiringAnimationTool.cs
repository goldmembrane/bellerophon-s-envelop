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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantFiringAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string StaticModelName = "Ispant_Model";
        private const string SourceSlotName = "Ispant_06_SheathSwordDrawMusket";
        private const string SourceModelName = "Ispant_SheathSword_Model";
        private const string PreviousTargetSlotName = "Ispant_07_BreakthroughMusketAimFireRecover";
        private const string TargetSlotName = "Ispant_07_BreakthroughMusketAimFire";
        private const string TargetModelName = "Ispant_Firing_Model";
        private const string AppearanceRootName = "Ispant_StaticAppearance";
        private const string HandMusketRootName = "Ispant_Firing_HandMusket";
        private const string HandMusketRendererName = "Ispant_Firing_HandMusket_Renderer";
        private const string SourceHandMusketRendererName = "Ispant_ChangeToRifle_HandMusket_Renderer";
        private const string WaistSwordRootName = "Ispant_ApprovedLongSword_LeftWaist";
        private const string WaistSwordRendererName = "Ispant_ApprovedLongSword_LeftWaist_Renderer";
        private const string BodyRendererName = "Ispant_Armed_Body";
        private const string SourceFbxPath = "enemies model/išpant firing.fbx";
        private const string ProjectFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Firing.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_07_Firing.controller";
        private const string SourceFinalAimClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_ChangeToRifle.anim";
        private const string InspectionPath =
            "docs/validation/ispant_firing_replacement_2026-08-10/Ispant_07_Firing_Inspection.txt";
        private const string GeometryDiagnosticPath =
            "docs/validation/ispant_firing_replacement_2026-08-10/Ispant_07_Firing_GeometryDiagnostic.txt";
        private const string CapturePath =
            "docs/validation/ispant_firing_replacement_2026-08-10/Ispant_07_Firing_FinalReview.png";
        private const string GripDiagnosticCapturePath =
            "docs/validation/ispant_firing_replacement_2026-08-10/Ispant_06_07_Grip_Diagnostic.png";
        private const string GroundAlignmentDiagnosticCapturePath =
            "docs/validation/ispant_firing_replacement_2026-08-10/Ispant_07_StaticBodyBottomY_Diagnostic.png";
        private const string MuzzleFlashDiagnosticCapturePath =
            "docs/validation/ispant_firing_replacement_2026-08-10/Ispant_07_MuzzleFlash_Diagnostic.png";
        private const string ApprovedFlashMeshPath =
            "Assets/_Project/Art/Enemies/Rebellion/VFX/Rebellion_Forward_Burst_Flash.asset";
        private const string ApprovedFlashMaterialPath =
            "Assets/_Project/Art/Enemies/Rebellion/VFX/Rebellion_Forward_Burst_Flash.mat";
        private const string SourceSha256 =
            "1EA7256EEEC66221BEF4A33994801145E76C6271EB705976EFA399DD79C26A6E";
        private const string ImportedClipName = "Ispant_Firing_Mixamo";
        private const string RetargetedClipName = "Ispant_Firing_Mixamo_Retargeted";
        private const string StateName = "Ispant_Firing_Mixamo";
        private const string MuzzleFlashPivotName = "Ispant_Firing_MuzzleFlash_Pivot";
        private const string MuzzleFlashName = "Ispant_Firing_MuzzleFlash";
        private const float BreakthroughAttackIntervalSeconds = 2.5f;
        private const float PlaybackSpeed = 0.2f;
        private const float FlashDurationSeconds = 0.08f;
        private const float FlashMuzzleOffset = 0.004f;
        private const float MatrixTolerance = 0.0001f;
        private const int BodySilhouetteResolution = 2048;
        private const int ExpectedSlots = 12;

        private static readonly string[] StaticAppearanceRendererNames =
        {
            "Ispant_Armed_Body",
            "Ispant_Crescent_Ornament",
            "Ispant_Reference_Eye_Slits"
        };

        private static readonly string[] AimPoseBoneNames =
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

        private static readonly string[] AimTranslationBoneNames =
        {
            "LeftShoulder",
            "RightShoulder"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 7 Firing Replacement")]
        public static void ApplyIspant07FiringReplacement()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            _ = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                throw new InvalidOperationException("The supplied Ispant firing FBX is unavailable.");
            var sourceClip = RequireFiringClip();
            var sourceFinalAimClip = RequireSourceFinalAimClip();

            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var sourceModel = RequireDirectChild(
                RequireSlot(placement.transform, SourceSlotName, 5), SourceModelName);
            var targetSlot = RequireTargetSlot(placement.transform);
            if (targetSlot.childCount != 1)
                throw new InvalidOperationException("Ispant slot 7 must contain exactly one model before replacement.");
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, targetSlot);
            var slotSnapshot = new TransformSnapshot(targetSlot);
            var previous = targetSlot.GetChild(0);
            var replacement = UnityEngine.Object.Instantiate(sourceModel.gameObject);
            SceneManager.MoveGameObjectToScene(replacement, scene);
            replacement.name = TargetModelName;
            replacement.transform.SetParent(targetSlot, false);
            replacement.transform.SetLocalPositionAndRotation(
                staticModel.localPosition, staticModel.localRotation);
            replacement.transform.localScale = sourceModel.localScale;

            try
            {
                PrepareExactStaticAppearanceClone(replacement.transform);
                var controller = CreateOrUpdateController(
                    sourceClip,
                    sourceFinalAimClip,
                    replacement.transform);
                var clip = RequireRetargetedClip(controller);
                var animator = ConfigureAnimator(replacement.transform, controller);
                ConfigureExistingFinalAimMusket(
                    sourceModel,
                    sourceFinalAimClip,
                    replacement.transform,
                    clip);
                ConfigureApprovedMuzzleFlash(replacement.transform, clip);
                AlignTargetModelYToStaticBodyBottom(
                    staticModel,
                    replacement.transform,
                    clip);
                var metrics = InspectModel(
                    staticModel,
                    sourceModel,
                    replacement.transform,
                    animator,
                    clip,
                    controller,
                    sourceFinalAimClip);
                WriteInspection(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            targetSlot.name = TargetSlotName;
            if (targetSlot.childCount != 1 || targetSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The slot-7 replacement did not leave exactly one model.");
            if (!slotSnapshot.Matches(MatrixTolerance))
                throw new InvalidOperationException("The slot-7 placement transform changed during replacement.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, targetSlot),
                "An Ispant slot outside slot 7 changed during replacement.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(targetSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot-7 replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = targetSlot.gameObject;
            Debug.Log(
                "Ispant07FiringReplacementApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + TargetSlotName +
                ", Source=" + ProjectFbxPath +
                ", Clip=" + ImportedClipName +
                ", Loop=True, PlaybackSpeed=" + Num(PlaybackSpeed) +
                ", AttackIntervalSeconds=" + Num(BreakthroughAttackIntervalSeconds) +
                ", MuzzleFlash=ApprovedRebellionForwardBurstFlash" +
                ", StaticAppearanceSharedAssets=True" +
                ", MusketSource=Ispant_06_FinalAim" +
                ", LeftWaistSwordSource=Ispant_06_LeftWaist" +
                ", LeftWaistSwordParent=mixamorig:Hips" +
                ", BodyBottomYAlignedToStatic=True" +
                ", OtherSlotsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 7 Firing Replacement")]
        public static void InspectIspant07FiringReplacement()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var sourceModel = RequireDirectChild(
                RequireSlot(placement.transform, SourceSlotName, 5), SourceModelName);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 6), TargetModelName);
            var animator = targetModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("The slot-7 firing Animator is missing.");
            _ = RequireFiringClip();
            var controller = RequireController();
            var retargetedClip = RequireRetargetedClip(controller);
            WriteGeometryDiagnostic(staticModel, sourceModel, targetModel, retargetedClip);
            var metrics = InspectModel(
                staticModel,
                sourceModel,
                targetModel,
                animator,
                retargetedClip,
                controller,
                RequireSourceFinalAimClip());
            WriteInspection(metrics);
            if (EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Unity reports script compilation errors.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-7 inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant07FiringReplacementInspected Result=PASS" +
                ", ClipLength=" + Num(metrics.ClipLength) +
                ", EffectiveLoopSeconds=" + Num(metrics.EffectiveLoopSeconds) +
                ", PlaybackSpeed=" + Num(metrics.PlaybackSpeed) +
                ", AppearanceRenderers=" + metrics.AppearanceRendererCount +
                ", MusketRendererLocalMatrixError=" +
                Num(metrics.MusketRendererLocalMatrixError) +
                ", MusketFinalAimModelRotationErrorDegrees=" +
                Num(metrics.MusketModelRotationErrorDegrees) +
                ", MusketMaximumRightHandContactErrorMeters=" +
                Num(metrics.MusketMaximumRightHandContactErrorMeters) +
                ", MusketMaximumLeftHandContactErrorMeters=" +
                Num(metrics.MusketMaximumLeftHandContactErrorMeters) +
                ", WaistSwordBodyFollowRotationDegrees=" +
                Num(metrics.WaistSwordBodyFollowRotationChange) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 7 Firing Replacement Review")]
        public static void CaptureIspant07FiringReplacementReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var sourceModel = RequireDirectChild(
                RequireSlot(placement.transform, SourceSlotName, 5), SourceModelName);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 6), TargetModelName);
            _ = RequireFiringClip();
            var controller = RequireController();
            var clip = RequireRetargetedClip(controller);
            var sourceFinalAimClip = RequireSourceFinalAimClip();
            var animator = targetModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException("The slot-7 firing Animator is missing.");
            var metrics = InspectModel(
                staticModel,
                sourceModel,
                targetModel,
                animator,
                clip,
                controller,
                sourceFinalAimClip);
            WriteInspection(metrics);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                File.Delete(destination);
            CaptureMuzzleFlashReview(targetModel, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-7 review capture changed the scene dirty state.");
            Debug.Log(
                "Ispant07FiringReplacementReviewCaptured Result=PASS" +
                ", Columns=BeforeFire,Firing,AfterFire" +
                ", ApprovedMuzzleFlash=True" +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        public static void CaptureIspant06And07GripDiagnostic()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var sourceModel = RequireDirectChild(
                RequireSlot(placement.transform, SourceSlotName, 5), SourceModelName);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 6), TargetModelName);
            var controller = RequireController();
            var targetClip = RequireRetargetedClip(controller);
            var destination = Absolute(GripDiagnosticCapturePath);
            CaptureGripDiagnostic(
                sourceModel,
                targetModel,
                RequireSourceFinalAimClip(),
                targetClip,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6/7 grip diagnostic changed the scene dirty state.");
            Debug.Log(
                "Ispant06And07GripDiagnosticCaptured Result=PASS" +
                ", Columns=Slot6Final,Slot7Start,Slot7Mid,Slot7End" +
                ", Rows=Front,Left,Right" +
                ", Image=" + GripDiagnosticCapturePath + ", SceneChanged=False.");
        }

        public static void CaptureIspant07GroundAlignmentDiagnostic()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 6), TargetModelName);
            var clip = RequireRetargetedClip(RequireController());
            CaptureGroundAlignmentReview(
                staticModel,
                targetModel,
                clip,
                Absolute(GroundAlignmentDiagnosticCapturePath));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-7 ground-alignment diagnostic changed the scene dirty state.");
            Debug.Log(
                "Ispant07GroundAlignmentDiagnosticCaptured Result=PASS" +
                ", Columns=Static,Slot7Start,Slot7Mid,Slot7End" +
                ", SharedCameraAndGroundHeight=True" +
                ", Image=" + GroundAlignmentDiagnosticCapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 7 Muzzle Flash Diagnostic")]
        public static void CaptureIspant07MuzzleFlashDiagnostic()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var targetModel = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 6), TargetModelName);
            var clip = RequireRetargetedClip(RequireController());
            CaptureMuzzleFlashReview(
                targetModel,
                clip,
                Absolute(MuzzleFlashDiagnosticCapturePath));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-7 muzzle-flash diagnostic changed the scene dirty state.");
            var timing = DetermineFiringTiming(targetModel, clip);
            Debug.Log(
                "Ispant07MuzzleFlashDiagnosticCaptured Result=PASS" +
                ", FireFrame=" + timing.FireFrame +
                ", FireClipTime=" + Num(timing.ClipTime) +
                ", Columns=BeforeFire,Firing,AfterFire" +
                ", Image=" + MuzzleFlashDiagnosticCapturePath +
                ", SceneChanged=False.");
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                ProjectFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ProjectFbxPath) as ModelImporter ??
                           throw new InvalidOperationException("The Ispant firing ModelImporter is missing.");
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    "The supplied Ispant firing FBX must expose exactly one embedded animation take.");
            if (clips[0].takeName.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The sole firing take is not identified as Mixamo: " + clips[0].takeName + ".");

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

        private static AnimationClip RequireFiringClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ProjectFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported Ispant firing Mixamo clip differs.");
            if (!AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime)
                throw new InvalidOperationException("The imported Ispant firing clip is not looping.");
            return clips[0];
        }

        private static AnimationClip RequireSourceFinalAimClip()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceFinalAimClipPath) ??
                   throw new InvalidOperationException("The approved slot-6 final aiming clip is missing.");
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip sourceClip,
            AnimationClip sourceFinalAimClip,
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
                         .Where(item => item.name == RetargetedClipName)
                         .ToArray())
                UnityEngine.Object.DestroyImmediate(oldClip, true);
            var clip = RetargetClip(sourceClip, sourceFinalAimClip, targetModel);
            AssetDatabase.AddObjectToAsset(clip, controller);
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = PlaybackSpeed;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip RetargetClip(
            AnimationClip sourceClip,
            AnimationClip sourceFinalAimClip,
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
                        "The supplied firing clip contains an unsupported curve type: " +
                        binding.type.FullName + ".");
                if (binding.propertyName.StartsWith("m_LocalScale.", StringComparison.Ordinal))
                    continue;
                var targetPath = RetargetPath(binding.path, targetModel, targetBones);
                var targetBinding = EditorCurveBinding.FloatCurve(
                    targetPath,
                    typeof(Transform),
                    binding.propertyName);
                AnimationUtility.SetEditorCurve(
                    result,
                    targetBinding,
                    AnimationUtility.GetEditorCurve(sourceClip, binding));
            }
            if (AnimationUtility.GetObjectReferenceCurveBindings(sourceClip).Length != 0)
                throw new InvalidOperationException(
                    "The supplied firing clip contains unsupported object-reference curves.");
            RebaseAimPoseCurves(sourceClip, sourceFinalAimClip, targetModel, result);
            result.EnsureQuaternionContinuity();
            AnimationUtility.SetAnimationEvents(result, AnimationUtility.GetAnimationEvents(sourceClip));
            var settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            return result;
        }

        private static void RebaseAimPoseCurves(
            AnimationClip firingSource,
            AnimationClip sourceFinalAimClip,
            Transform targetModel,
            AnimationClip result)
        {
            var targetBones = BuildUniqueTransformMap(targetModel);
            var sourceBindings = AnimationUtility.GetCurveBindings(firingSource);
            var snapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var finalRotations = new Dictionary<string, Quaternion>(StringComparer.OrdinalIgnoreCase);
            var finalPositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
            try
            {
                SampleClip(targetModel.gameObject, sourceFinalAimClip, sourceFinalAimClip.length);
                foreach (var boneName in AimPoseBoneNames)
                    finalRotations[boneName] = RequireMappedTransform(targetBones, boneName).localRotation;
                foreach (var boneName in AimTranslationBoneNames)
                    finalPositions[boneName] = RequireMappedTransform(targetBones, boneName).localPosition;
            }
            finally
            {
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            var frameCount = Mathf.Max(1, Mathf.RoundToInt(firingSource.length * firingSource.frameRate));
            foreach (var boneName in AimPoseBoneNames)
            {
                var sourceRotationBindings = sourceBindings.Where(binding =>
                        binding.type == typeof(Transform) &&
                        binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) &&
                        string.Equals(
                            NormalizeBoneName(binding.path.Split('/').Last()),
                            boneName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var times = Enumerable.Range(0, frameCount + 1)
                    .Select(frame => Mathf.Min(frame / firingSource.frameRate, firingSource.length))
                    .ToArray();
                var rotations = new Quaternion[times.Length];
                if (sourceRotationBindings.Length == 0)
                {
                    for (var index = 0; index < times.Length; index++)
                        rotations[index] = finalRotations[boneName];
                }
                else
                {
                    if (sourceRotationBindings.Length != 4)
                        throw new InvalidOperationException(
                            "The supplied firing clip has an incomplete rotation for aim bone: " +
                            boneName + ".");
                    var sourceCurves = new[] { "x", "y", "z", "w" }
                        .Select(component => sourceRotationBindings.Single(binding =>
                            binding.propertyName == "m_LocalRotation." + component))
                        .Select(binding => AnimationUtility.GetEditorCurve(firingSource, binding))
                        .ToArray();
                    Quaternion SourceRotation(float time)
                    {
                        return NormalizeQuaternion(new Quaternion(
                            sourceCurves[0].Evaluate(time),
                            sourceCurves[1].Evaluate(time),
                            sourceCurves[2].Evaluate(time),
                            sourceCurves[3].Evaluate(time)));
                    }

                    var sourceStart = SourceRotation(0f);
                    for (var index = 0; index < times.Length; index++)
                    {
                        var sourceRotation = SourceRotation(times[index]);
                        var delta = Quaternion.Inverse(sourceStart) * sourceRotation;
                        var desired = NormalizeQuaternion(finalRotations[boneName] * delta);
                        if (index > 0 && Quaternion.Dot(rotations[index - 1], desired) < 0f)
                            desired = new Quaternion(-desired.x, -desired.y, -desired.z, -desired.w);
                        rotations[index] = desired;
                    }
                }

                var target = RequireMappedTransform(targetBones, boneName);
                var targetPath = AnimationUtility.CalculateTransformPath(target, targetModel);
                SetQuaternionCurve(result, targetPath, "x", times, rotations.Select(item => item.x).ToArray());
                SetQuaternionCurve(result, targetPath, "y", times, rotations.Select(item => item.y).ToArray());
                SetQuaternionCurve(result, targetPath, "z", times, rotations.Select(item => item.z).ToArray());
                SetQuaternionCurve(result, targetPath, "w", times, rotations.Select(item => item.w).ToArray());
            }

            foreach (var boneName in AimTranslationBoneNames)
            {
                var sourcePositionBindings = sourceBindings.Where(binding =>
                        binding.type == typeof(Transform) &&
                        binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal) &&
                        string.Equals(
                            NormalizeBoneName(binding.path.Split('/').Last()),
                            boneName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var times = Enumerable.Range(0, frameCount + 1)
                    .Select(frame => Mathf.Min(frame / firingSource.frameRate, firingSource.length))
                    .ToArray();
                var positions = new Vector3[times.Length];
                if (sourcePositionBindings.Length == 0)
                {
                    for (var index = 0; index < times.Length; index++)
                        positions[index] = finalPositions[boneName];
                }
                else
                {
                    if (sourcePositionBindings.Length != 3)
                        throw new InvalidOperationException(
                            "The supplied firing clip has an incomplete position for aim bone: " +
                            boneName + ".");
                    var sourceCurves = new[] { "x", "y", "z" }
                        .Select(component => sourcePositionBindings.Single(binding =>
                            binding.propertyName == "m_LocalPosition." + component))
                        .Select(binding => AnimationUtility.GetEditorCurve(firingSource, binding))
                        .ToArray();
                    Vector3 SourcePosition(float time)
                    {
                        return new Vector3(
                            sourceCurves[0].Evaluate(time),
                            sourceCurves[1].Evaluate(time),
                            sourceCurves[2].Evaluate(time));
                    }

                    var sourceStart = SourcePosition(0f);
                    for (var index = 0; index < times.Length; index++)
                        positions[index] = finalPositions[boneName] +
                            SourcePosition(times[index]) - sourceStart;
                }

                var target = RequireMappedTransform(targetBones, boneName);
                var targetPath = AnimationUtility.CalculateTransformPath(target, targetModel);
                SetVector3Curve(result, targetPath, "x", times, positions.Select(item => item.x).ToArray());
                SetVector3Curve(result, targetPath, "y", times, positions.Select(item => item.y).ToArray());
                SetVector3Curve(result, targetPath, "z", times, positions.Select(item => item.z).ToArray());
            }
        }

        private static void SetQuaternionCurve(
            AnimationClip clip,
            string path,
            string component,
            IReadOnlyList<float> times,
            IReadOnlyList<float> values)
        {
            var curve = new AnimationCurve(Enumerable.Range(0, times.Count)
                .Select(index => new Keyframe(times[index], values[index])).ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation." + component),
                curve);
        }

        private static void SetVector3Curve(
            AnimationClip clip,
            string path,
            string component,
            IReadOnlyList<float> times,
            IReadOnlyList<float> values)
        {
            var curve = new AnimationCurve(Enumerable.Range(0, times.Count)
                .Select(index => new Keyframe(times[index], values[index])).ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalPosition." + component),
                curve);
        }

        private static Quaternion NormalizeQuaternion(Quaternion value)
        {
            var magnitude = Mathf.Sqrt(
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude <= 0.000001f)
                throw new InvalidOperationException("A firing animation quaternion is invalid.");
            return new Quaternion(
                value.x / magnitude,
                value.y / magnitude,
                value.z / magnitude,
                value.w / magnitude);
        }

        private static string RetargetPath(
            string sourcePath,
            Transform targetModel,
            IReadOnlyDictionary<string, Transform> targetBones)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return string.Empty;
            var leafName = sourcePath.Split('/').Last();
            var key = NormalizeBoneName(leafName);
            if (!targetBones.TryGetValue(key, out var target))
                throw new InvalidOperationException(
                    "The calibrated Ispant rig is missing a firing animation bone: " + key + ".");
            return AnimationUtility.CalculateTransformPath(target, targetModel);
        }

        private static AnimationClip RequireRetargetedClip(AnimatorController controller)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                .OfType<AnimationClip>()
                .Where(item => item.name == RetargetedClipName)
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException("The slot-7 retargeted firing clip differs.");
            if (!AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime)
                throw new InvalidOperationException("The slot-7 retargeted firing clip is not looping.");
            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.motion != clips[0])
                throw new InvalidOperationException("The slot-7 controller does not use the retargeted firing clip.");
            return clips[0];
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                   throw new InvalidOperationException("The slot-7 firing AnimatorController is missing.");
        }

        private static Animator ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The firing FBX must contain exactly one Animator.");
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

        private static void PrepareExactStaticAppearanceClone(Transform model)
        {
            var appearanceRenderers = StaticAppearanceRendererNames.Select(rendererName =>
                    model.GetComponentsInChildren<Renderer>(true)
                        .SingleOrDefault(item => item.name == rendererName) ??
                    throw new InvalidOperationException(
                        "The calibrated static appearance clone is missing: " + rendererName + "."))
                .ToArray();
            var handMusketRenderer = RequireRenderer<MeshRenderer>(
                model, SourceHandMusketRendererName);
            var waistSwordRenderer = RequireRenderer<MeshRenderer>(
                model, WaistSwordRendererName);
            var waistSwordRoot = waistSwordRenderer.transform.parent;
            if (waistSwordRoot == null || waistSwordRoot.name != WaistSwordRootName ||
                waistSwordRoot.parent != RequireDescendant(model, "mixamorig:Hips"))
                throw new InvalidOperationException(
                    "The slot-6 left-waist sword source is not rigidly attached to mixamorig:Hips.");
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)
                         .Where(item => !appearanceRenderers.Contains(item) &&
                                        item != handMusketRenderer &&
                                        item != waistSwordRenderer)
                         .ToArray())
            {
                var filter = renderer.GetComponent<MeshFilter>();
                UnityEngine.Object.DestroyImmediate(renderer);
                if (filter != null)
                    UnityEngine.Object.DestroyImmediate(filter);
            }

            var oldRoot = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == AppearanceRootName);
            if (oldRoot != null)
                UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            var appearanceRoot = new GameObject(AppearanceRootName);
            appearanceRoot.transform.SetParent(model, false);
            appearanceRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            appearanceRoot.transform.localScale = Vector3.one;
            foreach (var renderer in appearanceRenderers)
                renderer.transform.SetParent(appearanceRoot.transform, false);
            waistSwordRenderer.enabled = true;
            EditorUtility.SetDirty(appearanceRoot);
            EditorUtility.SetDirty(waistSwordRoot.gameObject);
            EditorUtility.SetDirty(waistSwordRenderer.gameObject);
        }

        private static void ConfigureExistingFinalAimMusket(
            Transform sourceModel,
            AnimationClip sourceFinalAimClip,
            Transform targetModel,
            AnimationClip targetClip)
        {
            _ = targetClip;
            var sourceRenderer = RequireRenderer<MeshRenderer>(
                sourceModel, SourceHandMusketRendererName);
            var renderer = RequireRenderer<MeshRenderer>(
                targetModel, SourceHandMusketRendererName);
            var root = renderer.transform.parent ??
                       throw new InvalidOperationException("The calibrated hand-musket root is missing.");
            var rightHand = RequireDescendant(targetModel, "mixamorig:RightHand");
            if (root.parent != rightHand)
                throw new InvalidOperationException(
                    "The calibrated hand musket is not an exact right-hand child.");
            var sourceRoot = sourceRenderer.transform.parent ??
                             throw new InvalidOperationException(
                                 "The slot-6 calibrated hand-musket root is missing.");
            if (MatrixError(LocalMatrix(sourceRoot), LocalMatrix(root)) > MatrixTolerance ||
                MatrixError(
                    LocalMatrix(sourceRenderer.transform),
                    LocalMatrix(renderer.transform)) > MatrixTolerance)
                throw new InvalidOperationException(
                    "The cloned slot-7 musket no longer preserves the corrected slot-6 local mount.");
            var sampledSource = CreateSamplingClone(
                sourceModel, "Ispant06CorrectedFinalMusketSample");
            Vector3 sourceFinalRootLocalPosition;
            Quaternion sourceFinalRootLocalRotation;
            Vector3 sourceFinalRootLocalScale;
            try
            {
                SampleClip(
                    sampledSource.gameObject,
                    sourceFinalAimClip,
                    sourceFinalAimClip.length);
                var sampledSourceRoot = RequireRenderer<MeshRenderer>(
                        sampledSource, SourceHandMusketRendererName)
                    .transform.parent ??
                    throw new InvalidOperationException(
                        "The sampled corrected slot-6 hand-musket root is missing.");
                sourceFinalRootLocalPosition = sampledSourceRoot.localPosition;
                sourceFinalRootLocalRotation = sampledSourceRoot.localRotation;
                sourceFinalRootLocalScale = sampledSourceRoot.localScale;
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledSource.gameObject);
            }
            root.SetLocalPositionAndRotation(
                sourceFinalRootLocalPosition,
                NormalizeQuaternion(sourceFinalRootLocalRotation));
            root.localScale = sourceFinalRootLocalScale;
            root.name = HandMusketRootName;
            renderer.name = HandMusketRendererName;
            renderer.enabled = true;
            EditorUtility.SetDirty(root.gameObject);
            EditorUtility.SetDirty(renderer.gameObject);
        }

        private static void ConfigureApprovedMuzzleFlash(
            Transform targetModel,
            AnimationClip targetClip)
        {
            var musketRenderer = RequireRenderer<MeshRenderer>(
                targetModel, HandMusketRendererName);
            foreach (var existing in targetModel.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name == MuzzleFlashPivotName ||
                                        item.name == MuzzleFlashName)
                         .OrderByDescending(item => item.GetComponentsInParent<Transform>(true).Length)
                         .ToArray())
            {
                if (existing != null)
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var flashMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ApprovedFlashMeshPath) ??
                            throw new InvalidOperationException(
                                "The approved Rebellion muzzle-flash mesh is missing.");
            var flashMaterial = AssetDatabase.LoadAssetAtPath<Material>(ApprovedFlashMaterialPath) ??
                                throw new InvalidOperationException(
                                    "The approved Rebellion muzzle-flash material is missing.");
            var geometry = DetermineMusketMuzzleGeometry(SharedMesh(musketRenderer));
            var localUp = Vector3.ProjectOnPlane(Vector3.up, geometry.LocalAxis);
            if (localUp.sqrMagnitude < 0.000001f)
                localUp = Vector3.ProjectOnPlane(Vector3.right, geometry.LocalAxis);
            localUp.Normalize();

            var pivotObject = new GameObject(MuzzleFlashPivotName);
            var pivot = pivotObject.transform;
            pivot.SetParent(musketRenderer.transform, false);
            pivot.localPosition = geometry.LocalTip +
                                  geometry.LocalAxis * FlashMuzzleOffset;
            pivot.localRotation = Quaternion.LookRotation(geometry.LocalAxis, localUp);
            var parentScale = musketRenderer.transform.lossyScale;
            if (Mathf.Abs(parentScale.x) < 0.000001f ||
                Mathf.Abs(parentScale.y) < 0.000001f ||
                Mathf.Abs(parentScale.z) < 0.000001f)
                throw new InvalidOperationException(
                    "The slot-7 musket cannot support the approved world-scale muzzle flash.");
            pivot.localScale = new Vector3(
                1f / Mathf.Abs(parentScale.x),
                1f / Mathf.Abs(parentScale.y),
                1f / Mathf.Abs(parentScale.z));

            var flashObject = new GameObject(
                MuzzleFlashName,
                typeof(MeshFilter),
                typeof(MeshRenderer));
            var flash = flashObject.transform;
            flash.SetParent(pivot, false);
            flash.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            flash.localScale = Vector3.zero;
            flashObject.GetComponent<MeshFilter>().sharedMesh = flashMesh;
            var flashRenderer = flashObject.GetComponent<MeshRenderer>();
            flashRenderer.sharedMaterial = flashMaterial;
            flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
            flashRenderer.receiveShadows = false;
            flashRenderer.lightProbeUsage = LightProbeUsage.Off;
            flashRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var timing = DetermineFiringTiming(targetModel, targetClip);
            SetMuzzleFlashScaleCurves(targetModel, targetClip, flash, timing);
            EditorUtility.SetDirty(pivotObject);
            EditorUtility.SetDirty(flashObject);
            EditorUtility.SetDirty(targetClip);
            AssetDatabase.SaveAssets();
        }

        private static FireTiming DetermineFiringTiming(
            Transform targetModel,
            AnimationClip targetClip)
        {
            var sampled = CreateSamplingClone(targetModel, "Ispant07FiringTimingSample");
            var musket = RequireRenderer<MeshRenderer>(sampled, HandMusketRendererName);
            var geometry = DetermineMusketMuzzleGeometry(SharedMesh(musket));
            var frameCount = Mathf.RoundToInt(targetClip.length * targetClip.frameRate);
            if (frameCount < 2)
                throw new InvalidOperationException(
                    "The slot-7 firing clip has too few frames to locate its recoil.");
            var bestRearwardDelta = float.NegativeInfinity;
            var bestDeltaEndFrame = -1;
            try
            {
                SampleClip(sampled.gameObject, targetClip, 0f);
                var previousPosition = musket.transform.TransformPoint(geometry.LocalTip);
                var previousAxis = musket.transform.TransformDirection(geometry.LocalAxis).normalized;
                for (var frame = 1; frame <= frameCount; frame++)
                {
                    var time = Mathf.Min(frame / targetClip.frameRate, targetClip.length);
                    SampleClip(sampled.gameObject, targetClip, time);
                    var currentPosition = musket.transform.TransformPoint(geometry.LocalTip);
                    var rearwardDelta = Vector3.Dot(
                        previousPosition - currentPosition,
                        previousAxis);
                    if (rearwardDelta > bestRearwardDelta)
                    {
                        bestRearwardDelta = rearwardDelta;
                        bestDeltaEndFrame = frame;
                    }
                    previousPosition = currentPosition;
                    previousAxis = musket.transform.TransformDirection(
                        geometry.LocalAxis).normalized;
                }
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampled.gameObject);
            }
            if (bestDeltaEndFrame <= 0 || bestRearwardDelta <= 0f)
                throw new InvalidOperationException(
                    "The slot-7 source motion has no measurable rearward recoil frame.");
            var fireFrame = bestDeltaEndFrame - 1;
            return new FireTiming(
                fireFrame,
                fireFrame / targetClip.frameRate,
                bestDeltaEndFrame,
                bestRearwardDelta);
        }

        private static void SetMuzzleFlashScaleCurves(
            Transform targetModel,
            AnimationClip targetClip,
            Transform flash,
            FireTiming timing)
        {
            var flashClipDuration = FlashDurationSeconds * PlaybackSpeed;
            var flashEnd = Mathf.Min(
                timing.ClipTime + flashClipDuration,
                targetClip.length);
            if (flashEnd <= timing.ClipTime)
                throw new InvalidOperationException(
                    "The approved muzzle-flash duration does not fit the firing clip.");
            var path = AnimationUtility.CalculateTransformPath(flash, targetModel);
            foreach (var component in new[] { "x", "y", "z" })
            {
                var keys = new List<Keyframe>();
                if (timing.ClipTime > 0f)
                    keys.Add(new Keyframe(0f, 0f));
                keys.Add(new Keyframe(timing.ClipTime, 1f));
                keys.Add(new Keyframe(flashEnd, 0f));
                if (flashEnd < targetClip.length)
                    keys.Add(new Keyframe(targetClip.length, 0f));
                var curve = new AnimationCurve(keys.ToArray());
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(
                    targetClip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        "m_LocalScale." + component),
                    curve);
            }
            targetClip.EnsureQuaternionContinuity();
        }

        private static MuzzleGeometry DetermineMusketMuzzleGeometry(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices.Length < 4)
                throw new InvalidOperationException(
                    "The approved musket mesh has too few vertices to establish its muzzle.");
            var first = 0;
            var second = 1;
            var maximumSquaredDistance = 0f;
            for (var left = 0; left < vertices.Length; left++)
            for (var right = left + 1; right < vertices.Length; right++)
            {
                var squaredDistance = (vertices[right] - vertices[left]).sqrMagnitude;
                if (squaredDistance <= maximumSquaredDistance)
                    continue;
                maximumSquaredDistance = squaredDistance;
                first = left;
                second = right;
            }
            var axis = (vertices[second] - vertices[first]).normalized;
            var projections = vertices.Select(vertex =>
                Vector3.Dot(vertex - vertices[first], axis)).ToArray();
            var length = Mathf.Sqrt(maximumSquaredDistance);
            var endRange = length * 0.2f;
            var firstSpread = vertices.Where((vertex, index) => projections[index] <= endRange)
                .Average(vertex => Vector3.Cross(vertex - vertices[first], axis).magnitude);
            var secondSpread = vertices.Where((vertex, index) => projections[index] >= length - endRange)
                .Average(vertex => Vector3.Cross(vertex - vertices[second], axis).magnitude);
            if (Mathf.Abs(firstSpread - secondSpread) < MatrixTolerance)
                throw new InvalidOperationException(
                    "The approved musket geometry does not distinguish stock from muzzle.");
            var localAxis = secondSpread < firstSpread ? axis : -axis;
            var localProjections = vertices.Select(vertex =>
                Vector3.Dot(vertex, localAxis)).ToArray();
            var muzzleProjection = localProjections.Max();
            var stockProjection = localProjections.Min();
            var muzzleEndRange = (muzzleProjection - stockProjection) * 0.2f;
            var muzzleVertices = vertices.Where((vertex, index) =>
                    localProjections[index] >= muzzleProjection - muzzleEndRange)
                .ToArray();
            if (muzzleVertices.Length < 3)
                throw new InvalidOperationException(
                    "The approved musket thin muzzle end has fewer than three vertices.");
            var muzzleCenter = muzzleVertices.Aggregate(
                Vector3.zero, (sum, vertex) => sum + vertex) / muzzleVertices.Length;
            muzzleCenter += localAxis *
                            (muzzleProjection - Vector3.Dot(muzzleCenter, localAxis));
            return new MuzzleGeometry(
                localAxis,
                muzzleCenter);
        }

        private static void AlignTargetModelYToStaticBodyBottom(
            Transform staticModel,
            Transform targetModel,
            AnimationClip targetClip)
        {
            var renderedBottoms = MeasureRenderedBodyBottoms(
                staticModel, targetModel, targetClip);
            var worldYDelta = renderedBottoms.StaticWorldY -
                              renderedBottoms.TargetWorldY;

            var parent = targetModel.parent ??
                         throw new InvalidOperationException(
                             "The slot-7 model root has no placement parent.");
            var localDelta = parent.InverseTransformVector(Vector3.up * worldYDelta);
            if (Mathf.Abs(localDelta.x) > MatrixTolerance ||
                Mathf.Abs(localDelta.z) > MatrixTolerance)
                throw new InvalidOperationException(
                    "Static body-bottom alignment would change slot-7 X or Z.");
            var localPosition = targetModel.localPosition;
            targetModel.localPosition = new Vector3(
                localPosition.x,
                localPosition.y + localDelta.y,
                localPosition.z);
            EditorUtility.SetDirty(targetModel.gameObject);
        }

        private static RenderedBodyBottoms MeasureRenderedBodyBottoms(
            Transform staticModel,
            Transform targetModel,
            AnimationClip targetClip)
        {
            const int captureLayer = 31;
            var sampledStatic = CreateSamplingClone(
                staticModel, "IspantStaticBodyBottomRenderSample");
            var sampledTarget = CreateSamplingClone(
                targetModel, "Ispant07BodyBottomRenderSample");
            sampledStatic.position = new Vector3(0f, sampledStatic.position.y, 0f);
            sampledTarget.position = new Vector3(0f, sampledTarget.position.y, 0f);
            SetLayerRecursive(sampledStatic, captureLayer);
            SetLayerRecursive(sampledTarget, captureLayer);
            SampleClip(sampledTarget.gameObject, targetClip, 0f);
            var staticRenderers = sampledStatic.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = sampledTarget.GetComponentsInChildren<Renderer>(true);
            SetEnabled(staticRenderers, false);
            SetEnabled(targetRenderers, false);
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(
                sampledStatic, BodyRendererName);
            var targetBody = RequireRenderer<SkinnedMeshRenderer>(
                sampledTarget, BodyRendererName);
            var cameraObject = new GameObject(
                "IspantBodyBottomMeasurementCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(
                BodySilhouetteResolution,
                BodySilhouetteResolution,
                24,
                RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(
                BodySilhouetteResolution,
                BodySilhouetteResolution,
                TextureFormat.RGBA32,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                staticBody.enabled = true;
                targetBody.enabled = true;
                var sharedBounds = staticBody.bounds;
                sharedBounds.Encapsulate(targetBody.bounds);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.cullingMask = 1 << captureLayer;
                camera.orthographic = true;
                camera.aspect = 1f;
                camera.orthographicSize = Mathf.Max(
                    Mathf.Max(sharedBounds.extents.y, sharedBounds.extents.x), 0.1f) * 1.1f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.transform.position = sharedBounds.center +
                                            Vector3.forward * (sharedBounds.extents.z + 5f);
                camera.transform.rotation = Quaternion.LookRotation(
                    sharedBounds.center - camera.transform.position, Vector3.up);
                camera.targetTexture = target;

                targetBody.enabled = false;
                var staticBottomPixel = RenderBodyBottomPixel(camera, pixels, target);
                staticBody.enabled = false;
                targetBody.enabled = true;
                var targetBottomPixel = RenderBodyBottomPixel(camera, pixels, target);
                var worldUnitsPerPixel = camera.orthographicSize * 2f /
                                         BodySilhouetteResolution;
                var cameraBottomWorldY = camera.transform.position.y -
                                         camera.orthographicSize;
                return new RenderedBodyBottoms(
                    cameraBottomWorldY + (staticBottomPixel + 0.5f) * worldUnitsPerPixel,
                    cameraBottomWorldY + (targetBottomPixel + 0.5f) * worldUnitsPerPixel,
                    staticBottomPixel,
                    targetBottomPixel,
                    worldUnitsPerPixel);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledStatic.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
                UnityEngine.Object.DestroyImmediate(pixels);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static int RenderBodyBottomPixel(
            Camera camera,
            Texture2D pixels,
            RenderTexture target)
        {
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(
                new Rect(0f, 0f, BodySilhouetteResolution, BodySilhouetteResolution),
                0,
                0);
            pixels.Apply();
            var colors = pixels.GetPixels32();
            for (var y = 0; y < BodySilhouetteResolution; y++)
            {
                var row = y * BodySilhouetteResolution;
                for (var x = 0; x < BodySilhouetteResolution; x++)
                {
                    if (colors[row + x].a != 0)
                        return y;
                }
            }
            throw new InvalidOperationException(
                "The rendered Ispant body silhouette contains no visible pixels.");
        }

        private static void RemoveImportedVisualRenderers(Transform model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                UnityEngine.Object.DestroyImmediate(renderer);
                if (filter != null)
                    UnityEngine.Object.DestroyImmediate(filter);
            }
        }

        private static void CloneExactStaticAppearance(
            Transform staticModel,
            Transform calibratedModel,
            Transform targetModel)
        {
            var old = targetModel.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == AppearanceRootName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var appearanceRoot = new GameObject(AppearanceRootName);
            appearanceRoot.transform.SetParent(targetModel, false);
            appearanceRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            appearanceRoot.transform.localScale = Vector3.one;

            var targetBones = BuildUniqueTransformMap(targetModel);
            ValidateCalibratedAppearance(staticModel, calibratedModel);
            foreach (var rendererName in StaticAppearanceRendererNames)
            {
                var source = calibratedModel.GetComponentsInChildren<Renderer>(true)
                    .SingleOrDefault(item => item.name == rendererName) ??
                    throw new InvalidOperationException(
                        "The calibrated static Ispant appearance renderer is missing: " + rendererName + ".");
                var rendererObject = new GameObject(source.name);
                rendererObject.transform.SetParent(appearanceRoot.transform, false);
                SetLocalMatrix(
                    rendererObject.transform,
                    calibratedModel.worldToLocalMatrix * source.transform.localToWorldMatrix);

                if (source is SkinnedMeshRenderer sourceSkinned)
                {
                    var targetSkinned = rendererObject.AddComponent<SkinnedMeshRenderer>();
                    targetSkinned.sharedMesh = sourceSkinned.sharedMesh;
                    targetSkinned.sharedMaterials = sourceSkinned.sharedMaterials;
                    targetSkinned.bones = MapStaticBones(sourceSkinned, targetBones);
                    targetSkinned.rootBone = sourceSkinned.rootBone == null
                        ? null
                        : RequireMappedTransform(targetBones, sourceSkinned.rootBone.name);
                    targetSkinned.localBounds = sourceSkinned.localBounds;
                    targetSkinned.quality = sourceSkinned.quality;
                    targetSkinned.updateWhenOffscreen = true;
                    targetSkinned.skinnedMotionVectors = sourceSkinned.skinnedMotionVectors;
                    CopyRendererSettings(sourceSkinned, targetSkinned);
                }
                else if (source is MeshRenderer sourceMeshRenderer)
                {
                    var sourceFilter = sourceMeshRenderer.GetComponent<MeshFilter>() ??
                                       throw new InvalidOperationException(
                                           "The static appearance MeshRenderer has no MeshFilter: " + source.name + ".");
                    rendererObject.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
                    var targetRenderer = rendererObject.AddComponent<MeshRenderer>();
                    targetRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
                    CopyRendererSettings(sourceMeshRenderer, targetRenderer);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unsupported static appearance renderer type: " + source.GetType().Name + ".");
                }
                EditorUtility.SetDirty(rendererObject);
            }
            EditorUtility.SetDirty(appearanceRoot);
        }

        private static void ValidateCalibratedAppearance(
            Transform staticModel,
            Transform calibratedModel)
        {
            foreach (var rendererName in StaticAppearanceRendererNames)
            {
                var staticRenderer = staticModel.GetComponentsInChildren<Renderer>(true)
                    .Single(item => item.name == rendererName);
                var calibratedRenderer = calibratedModel.GetComponentsInChildren<Renderer>(true)
                    .Single(item => item.name == rendererName);
                if (calibratedRenderer is not SkinnedMeshRenderer)
                    throw new InvalidOperationException(
                        "The calibrated firing appearance is not skinned: " + rendererName + ".");

                var expectedMaterials = staticRenderer.sharedMaterials.AsEnumerable();
                if (rendererName == "Ispant_Armed_Body")
                    expectedMaterials = expectedMaterials.Where(material =>
                        material != null &&
                        material.name != "Ispant_Wood_Approved" &&
                        material.name != "Ispant_Steel_Approved");
                if (!expectedMaterials.SequenceEqual(calibratedRenderer.sharedMaterials))
                    throw new InvalidOperationException(
                        "The calibrated firing appearance materials differ from the static reference: " +
                        rendererName + ".");
            }

            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var calibratedBody = RequireRenderer<SkinnedMeshRenderer>(calibratedModel, "Ispant_Armed_Body");
            if (staticBody.sharedMesh.subMeshCount != 9 || calibratedBody.sharedMesh.subMeshCount != 7 ||
                calibratedBody.sharedMaterials.Any(material => material != null &&
                    (material.name == "Ispant_Wood_Approved" || material.name == "Ispant_Steel_Approved")))
                throw new InvalidOperationException(
                    "The calibrated firing body does not exactly exclude the static back-musket submeshes.");
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
                    NormalizeBoneName(item.name),
                    "Hips",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hipsMatches.Length != 1)
                throw new InvalidOperationException(
                    "The firing rig must contain exactly one Hips skeleton root.");
            var armature = hipsMatches[0].parent;
            if (armature == null ||
                !string.Equals(NormalizeBoneName(armature.name), "Armature", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The firing rig Armature parent is missing.");
            result["Armature"] = armature;
            foreach (var item in hipsMatches[0].GetComponentsInChildren<Transform>(true))
            {
                if (item != hipsMatches[0] &&
                    item.name.IndexOf("mixamorig:", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var key = NormalizeBoneName(item.name);
                if (result.TryGetValue(key, out var existing) && existing != item)
                    throw new InvalidOperationException(
                        "The firing rig contains duplicate normalized bone names: " + key + ".");
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
                : throw new InvalidOperationException("The firing rig is missing static bone: " + key + ".");
        }

        private static Transform[] MapStaticBones(
            SkinnedMeshRenderer source,
            IReadOnlyDictionary<string, Transform> targetBones)
        {
            var sourceBones = source.bones;
            var result = new Transform[sourceBones.Length];
            for (var index = 0; index < sourceBones.Length; index++)
            {
                var sourceBone = sourceBones[index] ??
                                 throw new InvalidOperationException(
                                     "The static skin contains a null bone at index " + index + ".");
                var key = NormalizeBoneName(sourceBone.name);
                if (targetBones.TryGetValue(key, out var exact))
                {
                    result[index] = exact;
                    continue;
                }
                if (IsBoneUsed(source.sharedMesh, index))
                    throw new InvalidOperationException(
                        "The firing rig is missing a weighted static bone: " + key + ".");
                var parent = sourceBone.parent ??
                             throw new InvalidOperationException(
                                 "The firing rig is missing an unweighted root bone: " + key + ".");
                result[index] = RequireMappedTransform(targetBones, parent.name);
            }
            return result;
        }

        private static bool IsBoneUsed(Mesh mesh, int boneIndex)
        {
            foreach (var weight in mesh.boneWeights)
            {
                if ((weight.boneIndex0 == boneIndex && weight.weight0 > 0f) ||
                    (weight.boneIndex1 == boneIndex && weight.weight1 > 0f) ||
                    (weight.boneIndex2 == boneIndex && weight.weight2 > 0f) ||
                    (weight.boneIndex3 == boneIndex && weight.weight3 > 0f))
                    return true;
            }
            return false;
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

        private static void CloneFinalAimMusket(
            Transform sourceModel,
            AnimationClip sourceFinalAimClip,
            Transform targetModel,
            AnimationClip targetClip)
        {
            var old = targetModel.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == HandMusketRootName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var sourceRenderer = RequireRenderer<MeshRenderer>(
                sourceModel, SourceHandMusketRendererName);
            var targetRightHand = RequireDescendant(targetModel, "mixamorig:RightHand");
            var sampledSource = CreateSamplingClone(sourceModel, "Ispant06FinalAimSample");
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant07StartSample");
            Matrix4x4 sourceModelRelativeRoot;
            Matrix4x4 sourceRootRelativeRenderer;
            Matrix4x4 targetHandRelativeRoot;
            try
            {
                var sampledSourceRenderer = RequireRenderer<MeshRenderer>(
                    sampledSource, SourceHandMusketRendererName);
                var sampledSourceRoot = sampledSourceRenderer.transform.parent ??
                                        throw new InvalidOperationException("The slot-6 hand-musket root is missing.");
                SampleClip(sampledSource.gameObject, sourceFinalAimClip, sourceFinalAimClip.length);
                sourceModelRelativeRoot = sampledSource.worldToLocalMatrix * sampledSourceRoot.localToWorldMatrix;
                sourceRootRelativeRenderer = sampledSourceRoot.worldToLocalMatrix *
                                             sampledSourceRenderer.transform.localToWorldMatrix;
                SampleClip(sampledTarget.gameObject, targetClip, 0f);
                var sampledTargetRightHand = RequireDescendant(sampledTarget, "mixamorig:RightHand");
                var targetDesiredRootWorld = sampledTarget.localToWorldMatrix * sourceModelRelativeRoot;
                targetHandRelativeRoot = sampledTargetRightHand.worldToLocalMatrix * targetDesiredRootWorld;
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledSource.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }

            var root = new GameObject(HandMusketRootName);
            root.transform.SetParent(targetRightHand, false);
            SetLocalMatrix(root.transform, targetHandRelativeRoot);
            var rendererObject = new GameObject(HandMusketRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            SetLocalMatrix(rendererObject.transform, sourceRootRelativeRenderer);
            rendererObject.AddComponent<MeshFilter>().sharedMesh =
                sourceRenderer.GetComponent<MeshFilter>()?.sharedMesh ??
                throw new InvalidOperationException("The slot-6 final musket MeshFilter is missing.");
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            CopyRendererSettings(sourceRenderer, renderer);
            renderer.enabled = true;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
        }

        private static Metrics InspectModel(
            Transform staticModel,
            Transform sourceModel,
            Transform targetModel,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            AnimationClip sourceFinalAimClip)
        {
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion)
                throw new InvalidOperationException("The slot-7 Animator configuration differs.");
            var stateMachine = controller.layers[0].stateMachine;
            if (stateMachine.states.Length != 1 || stateMachine.defaultState == null)
                throw new InvalidOperationException("The slot-7 controller must contain one default state.");
            var state = stateMachine.defaultState;
            if (state.name != StateName || state.motion != clip ||
                Mathf.Abs(state.speed - PlaybackSpeed) > 0.000001f)
                throw new InvalidOperationException(
                    "The slot-7 firing state or design-source breakthrough attack speed differs.");
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The slot-7 Mixamo firing clip is not looping.");
            if (Mathf.Abs(clip.length / PlaybackSpeed -
                          BreakthroughAttackIntervalSeconds) > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-7 effective loop does not match the 2.5-second breakthrough attack interval.");

            ValidateCalibratedAppearance(staticModel, sourceModel);
            var modelScaleError = Vector3.Distance(targetModel.localScale, sourceModel.localScale);
            if (modelScaleError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-7 model scale differs from the calibrated static appearance scale: " +
                    Num(modelScaleError) + ".");

            var appearanceRoot = RequireDirectChild(targetModel, AppearanceRootName);
            var appearanceRenderers = appearanceRoot.GetComponentsInChildren<Renderer>(true);
            if (appearanceRenderers.Length != StaticAppearanceRendererNames.Length)
                throw new InvalidOperationException("The exact static appearance renderer count differs.");
            foreach (var rendererName in StaticAppearanceRendererNames)
            {
                var source = sourceModel.GetComponentsInChildren<Renderer>(true)
                    .Single(item => item.name == rendererName);
                var target = appearanceRenderers.Single(item => item.name == rendererName);
                if (SharedMesh(source) != SharedMesh(target) ||
                    !source.sharedMaterials.SequenceEqual(target.sharedMaterials))
                    throw new InvalidOperationException(
                        "The slot-7 appearance is not a direct static reference: " + rendererName + ".");
                if (source is not SkinnedMeshRenderer sourceSkinned ||
                    target is not SkinnedMeshRenderer targetSkinned)
                    throw new InvalidOperationException(
                        "The slot-7 calibrated appearance is not fully skinned: " + rendererName + ".");
                var expectedBones = MapStaticBones(
                    sourceSkinned,
                    BuildUniqueTransformMap(targetModel));
                if (expectedBones.Length != targetSkinned.bones.Length ||
                    !expectedBones.SequenceEqual(targetSkinned.bones))
                    throw new InvalidOperationException(
                        "The slot-7 static skin binding differs: " + rendererName + ".");
            }

            var allTargetRenderers = targetModel.GetComponentsInChildren<Renderer>(true);
            var backMusketRenderers = allTargetRenderers.Where(renderer =>
                renderer.name.Contains("RigidMusket", StringComparison.OrdinalIgnoreCase) ||
                renderer.name.Contains("BackMusket", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (backMusketRenderers.Length != 0 || allTargetRenderers.Length != 6)
                throw new InvalidOperationException(
                    "The slot-7 renderer set must contain body, crescent, eyes, the hand musket, " +
                    "the approved muzzle flash, and one left-waist long sword.");
            var targetBody = appearanceRenderers.Single(item => item.name == "Ispant_Armed_Body");
            if (SharedMesh(targetBody).subMeshCount != 7 || targetBody.sharedMaterials.Length != 7)
                throw new InvalidOperationException("The slot-7 body still contains back-musket submeshes.");

            var sourceRenderer = RequireRenderer<MeshRenderer>(
                sourceModel, SourceHandMusketRendererName);
            var targetRenderer = RequireRenderer<MeshRenderer>(
                targetModel, HandMusketRendererName);
            var targetRoot = targetRenderer.transform.parent ??
                             throw new InvalidOperationException("The slot-7 hand-musket root is missing.");
            if (targetRoot.parent != RequireDescendant(targetModel, "mixamorig:RightHand"))
                throw new InvalidOperationException("The slot-7 musket is not rigidly parented to the right hand.");
            if (SharedMesh(sourceRenderer) != SharedMesh(targetRenderer) ||
                !sourceRenderer.sharedMaterials.SequenceEqual(targetRenderer.sharedMaterials))
                throw new InvalidOperationException("The slot-7 musket is not the exact slot-6 final musket.");
            var sourceWaistSword = RequireRenderer<MeshRenderer>(
                sourceModel, WaistSwordRendererName);
            var targetWaistSword = RequireRenderer<MeshRenderer>(
                targetModel, WaistSwordRendererName);
            var sourceWaistSwordRoot = sourceWaistSword.transform.parent;
            var targetWaistSwordRoot = targetWaistSword.transform.parent;
            if (sourceWaistSwordRoot == null || targetWaistSwordRoot == null ||
                sourceWaistSwordRoot.name != WaistSwordRootName ||
                targetWaistSwordRoot.name != WaistSwordRootName ||
                sourceWaistSwordRoot.parent != RequireDescendant(sourceModel, "mixamorig:Hips") ||
                targetWaistSwordRoot.parent != RequireDescendant(targetModel, "mixamorig:Hips") ||
                SharedMesh(sourceWaistSword) != SharedMesh(targetWaistSword) ||
                !sourceWaistSword.sharedMaterials.SequenceEqual(targetWaistSword.sharedMaterials) ||
                MatrixError(LocalMatrix(sourceWaistSwordRoot), LocalMatrix(targetWaistSwordRoot)) >
                MatrixTolerance ||
                MatrixError(LocalMatrix(sourceWaistSword.transform), LocalMatrix(targetWaistSword.transform)) >
                MatrixTolerance ||
                !targetWaistSword.enabled)
                throw new InvalidOperationException(
                    "The slot-7 left-waist sword does not exactly preserve the slot-6 source mount and assets.");
            var flashPivot = RequireDescendant(targetModel, MuzzleFlashPivotName);
            var flash = RequireDescendant(targetModel, MuzzleFlashName);
            if (flash.parent != flashPivot || flashPivot.parent != targetRenderer.transform)
                throw new InvalidOperationException(
                    "The slot-7 muzzle flash is not rigidly attached to the approved musket muzzle.");
            var flashFilter = flash.GetComponent<MeshFilter>() ??
                              throw new InvalidOperationException(
                                  "The slot-7 muzzle flash MeshFilter is missing.");
            var flashRenderer = flash.GetComponent<MeshRenderer>() ??
                                throw new InvalidOperationException(
                                    "The slot-7 muzzle flash MeshRenderer is missing.");
            if (AssetDatabase.GetAssetPath(flashFilter.sharedMesh) != ApprovedFlashMeshPath ||
                AssetDatabase.GetAssetPath(flashRenderer.sharedMaterial) != ApprovedFlashMaterialPath)
                throw new InvalidOperationException(
                    "The slot-7 muzzle flash does not reuse the exact approved Rebellion assets.");
            var muzzleGeometry = DetermineMusketMuzzleGeometry(SharedMesh(targetRenderer));
            var expectedFlashPivotLocalPosition = muzzleGeometry.LocalTip +
                                                  muzzleGeometry.LocalAxis * FlashMuzzleOffset;
            var flashAnchorError = Vector3.Distance(
                flashPivot.localPosition, expectedFlashPivotLocalPosition);
            var flashAxisError = Vector3.Angle(
                flashPivot.localRotation * Vector3.forward,
                muzzleGeometry.LocalAxis);
            var fireTiming = DetermineFiringTiming(targetModel, clip);

            var sampledSource = CreateSamplingClone(sourceModel, "Ispant06InspectSample");
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant07InspectSample");
            var sampledSourceRenderer = RequireRenderer<MeshRenderer>(
                sampledSource, SourceHandMusketRendererName);
            var sampledTargetRenderer = RequireRenderer<MeshRenderer>(
                sampledTarget, HandMusketRendererName);
            var sampledWaistSword = RequireRenderer<MeshRenderer>(
                sampledTarget, WaistSwordRendererName);
            var sampledWaistSwordRoot = sampledWaistSword.transform.parent ??
                                       throw new InvalidOperationException(
                                           "The sampled slot-7 left-waist sword root is missing.");
            var sampledTargetRoot = sampledTargetRenderer.transform.parent ??
                                    throw new InvalidOperationException("The sampled slot-7 hand-musket root is missing.");
            var sampledAppearanceRenderers = RequireDirectChild(sampledTarget, AppearanceRootName)
                .GetComponentsInChildren<Renderer>(true);
            var maximumMusketLocalDrift = 0f;
            var flashBeforeScaleError = float.PositiveInfinity;
            var flashFiringScaleError = float.PositiveInfinity;
            var flashAfterScaleError = float.PositiveInfinity;
            var musketRendererLocalMatrixError = float.PositiveInfinity;
            var musketModelRotationErrorDegrees = float.PositiveInfinity;
            var maximumMusketRightHandContactErrorMeters = 0f;
            var maximumMusketLeftHandContactErrorMeters = 0f;
            var sourceMusketModelRotation = Quaternion.identity;
            var targetMusketModelRotation = Quaternion.identity;
            var sourceRightHandGripInMusket = Vector3.zero;
            var sourceLeftHandGripInMusket = Vector3.zero;
            var sourceMusketRootLocalRotation = Quaternion.identity;
            var targetMusketRootLocalRotation = Quaternion.identity;
            var sourceMusketRendererLocalMatrix = Matrix4x4.identity;
            var aimPoseModelRotationTrace = string.Empty;
            var maximumAimPoseStartRotationError = float.PositiveInfinity;
            var maximumAimPoseStartPositionError = float.PositiveInfinity;
            var leftSupportGripStartError = float.PositiveInfinity;
            var rightGripStartError = float.PositiveInfinity;
            var waistSwordHipLocalDrift = 0f;
            var waistSwordBodyFollowPositionChange = 0f;
            var waistSwordBodyFollowRotationChange = 0f;
            var waistSwordHipLocalReference = Matrix4x4.identity;
            var waistSwordModelRelativeReference = Matrix4x4.identity;
            var waistSwordReferencePosition = Vector3.zero;
            var waistSwordReferenceRotation = Quaternion.identity;
            var renderedBodyBottoms = MeasureRenderedBodyBottoms(
                staticModel, targetModel, clip);
            var staticBodyBottomY = renderedBodyBottoms.StaticWorldY;
            var targetBodyBottomY = renderedBodyBottoms.TargetWorldY;
            var bodyBottomYError = Mathf.Abs(
                staticBodyBottomY - targetBodyBottomY);
            var startLocal = LocalMatrix(sampledTargetRoot);
            try
            {
                SampleClip(sampledSource.gameObject, sourceFinalAimClip, sourceFinalAimClip.length);
                var sourceRoot = sampledSourceRenderer.transform.parent ??
                                  throw new InvalidOperationException("The slot-6 hand-musket root is missing.");
                sourceMusketRootLocalRotation = sourceRoot.localRotation;
                sourceMusketRendererLocalMatrix = LocalMatrix(sampledSourceRenderer.transform);
                sourceMusketModelRotation =
                    Quaternion.Inverse(sampledSource.rotation) * sampledSourceRenderer.transform.rotation;
                var sourceRightHand = RequireDescendant(
                    sampledSource, "mixamorig:RightHand");
                var sourceLeftHand = RequireDescendant(
                    sampledSource, "mixamorig:LeftHand");
                var sourceBody = RequireRenderer<SkinnedMeshRenderer>(
                    sampledSource, BodyRendererName);
                var sourceRightHandAnchorLocal =
                    VisibleHandAnchorLocal(sourceBody, sourceRightHand);
                var sourceLeftHandAnchorLocal =
                    VisibleHandAnchorLocal(sourceBody, sourceLeftHand);
                sourceRightHandGripInMusket = sampledSourceRenderer.transform.InverseTransformPoint(
                    sourceRightHand.TransformPoint(sourceRightHandAnchorLocal));
                sourceLeftHandGripInMusket = sampledSourceRenderer.transform.InverseTransformPoint(
                    sourceLeftHand.TransformPoint(sourceLeftHandAnchorLocal));
                var sourceBoneMap = BuildUniqueTransformMap(sampledSource);
                var targetBoneMap = BuildUniqueTransformMap(sampledTarget);
                var sourceAimPoseRotations = AimPoseBoneNames.ToDictionary(
                    item => item,
                    item => RequireMappedTransform(sourceBoneMap, item).localRotation,
                    StringComparer.OrdinalIgnoreCase);
                var sourceAimPosePositions = AimTranslationBoneNames.ToDictionary(
                    item => item,
                    item => RequireMappedTransform(sourceBoneMap, item).localPosition,
                    StringComparer.OrdinalIgnoreCase);
                var sourceAimPoseModelRotations = AimPoseBoneNames.ToDictionary(
                    item => item,
                    item => Quaternion.Inverse(sampledSource.rotation) *
                            RequireMappedTransform(sourceBoneMap, item).rotation,
                    StringComparer.OrdinalIgnoreCase);
                var sourceSpine2 = RequireDescendant(sampledSource, "mixamorig:Spine2");
                var sourceLeftGrip = sourceSpine2.InverseTransformPoint(
                    sourceLeftHand.TransformPoint(sourceLeftHandAnchorLocal));
                var sourceRightGrip = sourceSpine2.InverseTransformPoint(
                    sourceRightHand.TransformPoint(sourceRightHandAnchorLocal));
                SampleClip(sampledTarget.gameObject, clip, 0f);
                waistSwordHipLocalReference = LocalMatrix(sampledWaistSwordRoot);
                waistSwordModelRelativeReference = ModelRelativeMatrix(
                    sampledTarget, sampledWaistSwordRoot);
                DecomposeMatrix(
                    waistSwordModelRelativeReference,
                    out waistSwordReferencePosition,
                    out waistSwordReferenceRotation,
                    out _);
                var sampledFlash = RequireDescendant(sampledTarget, MuzzleFlashName);
                var flashClipDuration = FlashDurationSeconds * PlaybackSpeed;
                var beforeTime = fireTiming.ClipTime > 0f
                    ? Mathf.Max(0f, fireTiming.ClipTime - 0.5f / clip.frameRate)
                    : Mathf.Max(0f, clip.length - 0.5f / clip.frameRate);
                var firingTime = Mathf.Min(
                    fireTiming.ClipTime + flashClipDuration * 0.5f,
                    clip.length);
                var afterTime = Mathf.Min(
                    fireTiming.ClipTime + flashClipDuration + 0.5f / clip.frameRate,
                    clip.length);
                SampleClip(sampledTarget.gameObject, clip, beforeTime);
                flashBeforeScaleError = sampledFlash.localScale.magnitude;
                SampleClip(sampledTarget.gameObject, clip, firingTime);
                flashFiringScaleError = Vector3.Distance(
                    sampledFlash.localScale, Vector3.one);
                SampleClip(sampledTarget.gameObject, clip, afterTime);
                flashAfterScaleError = sampledFlash.localScale.magnitude;
                SampleClip(sampledTarget.gameObject, clip, 0f);
                var targetLeftHand = RequireDescendant(
                    sampledTarget, "mixamorig:LeftHand");
                var targetRightHand = RequireDescendant(
                    sampledTarget, "mixamorig:RightHand");
                targetMusketRootLocalRotation = sampledTargetRoot.localRotation;
                maximumAimPoseStartRotationError = AimPoseBoneNames.Max(item => Quaternion.Angle(
                    sourceAimPoseRotations[item],
                    RequireMappedTransform(targetBoneMap, item).localRotation));
                maximumAimPoseStartPositionError = AimTranslationBoneNames.Max(item => Vector3.Distance(
                    sourceAimPosePositions[item],
                    RequireMappedTransform(targetBoneMap, item).localPosition));
                aimPoseModelRotationTrace = string.Join(
                    ",",
                    AimPoseBoneNames.Select(item =>
                        item + "=" + Num(Quaternion.Angle(
                            sourceAimPoseModelRotations[item],
                            Quaternion.Inverse(sampledTarget.rotation) *
                            RequireMappedTransform(targetBoneMap, item).rotation))));
                leftSupportGripStartError = Vector3.Distance(
                    sourceLeftGrip,
                    RequireDescendant(sampledTarget, "mixamorig:Spine2")
                        .InverseTransformPoint(targetLeftHand.TransformPoint(
                            sourceLeftHandAnchorLocal)));
                rightGripStartError = Vector3.Distance(
                    sourceRightGrip,
                    RequireDescendant(sampledTarget, "mixamorig:Spine2")
                        .InverseTransformPoint(targetRightHand.TransformPoint(
                            sourceRightHandAnchorLocal)));
                musketRendererLocalMatrixError = MatrixError(
                    sourceMusketRendererLocalMatrix,
                    LocalMatrix(sampledTargetRenderer.transform));
                targetMusketModelRotation =
                    Quaternion.Inverse(sampledTarget.rotation) * sampledTargetRenderer.transform.rotation;
                musketModelRotationErrorDegrees = Quaternion.Angle(
                    sourceMusketModelRotation,
                    targetMusketModelRotation);
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    SampleClip(
                        sampledTarget.gameObject,
                        clip,
                        Mathf.Min(frame / clip.frameRate, clip.length));
                    maximumMusketLocalDrift = Mathf.Max(
                        maximumMusketLocalDrift,
                        MatrixError(startLocal, LocalMatrix(sampledTargetRoot)));
                    waistSwordHipLocalDrift = Mathf.Max(
                        waistSwordHipLocalDrift,
                        MatrixError(
                            waistSwordHipLocalReference,
                            LocalMatrix(sampledWaistSwordRoot)));
                    var waistSwordModelRelative = ModelRelativeMatrix(
                        sampledTarget, sampledWaistSwordRoot);
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
                    maximumMusketRightHandContactErrorMeters = Mathf.Max(
                        maximumMusketRightHandContactErrorMeters,
                        Vector3.Distance(
                            sampledTargetRenderer.transform.TransformPoint(sourceRightHandGripInMusket),
                            targetRightHand.TransformPoint(sourceRightHandAnchorLocal)));
                    maximumMusketLeftHandContactErrorMeters = Mathf.Max(
                        maximumMusketLeftHandContactErrorMeters,
                        Vector3.Distance(
                            sampledTargetRenderer.transform.TransformPoint(sourceLeftHandGripInMusket),
                            targetLeftHand.TransformPoint(sourceLeftHandAnchorLocal)));
                    foreach (var renderer in sampledAppearanceRenderers.Append<Renderer>(sampledTargetRenderer))
                    {
                        var bounds = renderer.bounds;
                        if (!IsFinite(bounds.center) || !IsFinite(bounds.size) || bounds.size.sqrMagnitude <= 0f)
                            throw new InvalidOperationException(
                                "The slot-7 firing animation produced invalid visible bounds: " + renderer.name + ".");
                    }
                }
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledSource.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }
            if (maximumAimPoseStartRotationError > 0.001f ||
                maximumAimPoseStartPositionError > 0.001f ||
                leftSupportGripStartError > 0.001f ||
                rightGripStartError > 0.001f)
                throw new InvalidOperationException(
                    "The slot-7 start pose does not match the slot-6 final two-hand aim: AimPose=" +
                    Num(maximumAimPoseStartRotationError) +
                    ", AimPosition=" + Num(maximumAimPoseStartPositionError) +
                    ", LeftGrip=" + Num(leftSupportGripStartError) +
                    ", RightGrip=" + Num(rightGripStartError) + ".");
            if (musketRendererLocalMatrixError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-7 musket renderer does not preserve the slot-6 local mesh placement: " +
                    Num(musketRendererLocalMatrixError) + ".");
            if (musketModelRotationErrorDegrees > 0.001f)
                throw new InvalidOperationException(
                    "The slot-7 muzzle and trigger orientation does not match the slot-6 final aim: " +
                    Num(musketModelRotationErrorDegrees) + " degrees" +
                    ", SourceModelEuler=" + sourceMusketModelRotation.eulerAngles.ToString("F6") +
                    ", TargetModelEuler=" + targetMusketModelRotation.eulerAngles.ToString("F6") +
                    ", SourceRootLocalEuler=" + sourceMusketRootLocalRotation.eulerAngles.ToString("F6") +
                    ", TargetRootLocalEuler=" + targetMusketRootLocalRotation.eulerAngles.ToString("F6") +
                    ", AimPoseModelRotationErrors=" + aimPoseModelRotationTrace + ".");
            if (maximumMusketRightHandContactErrorMeters > 0.03f ||
                maximumMusketLeftHandContactErrorMeters > 0.03f)
                throw new InvalidOperationException(
                    "The slot-7 hands do not remain on the slot-6 musket grip points: Right=" +
                    Num(maximumMusketRightHandContactErrorMeters) +
                    ", Left=" + Num(maximumMusketLeftHandContactErrorMeters) + " meters.");
            if (maximumMusketLocalDrift > MatrixTolerance)
                throw new InvalidOperationException(
                    "The slot-7 musket local mount drifts during firing: " +
                    Num(maximumMusketLocalDrift) + ".");
            if (waistSwordHipLocalDrift > MatrixTolerance ||
                waistSwordBodyFollowRotationChange <= 0.01f)
                throw new InvalidOperationException(
                    "The slot-7 left-waist sword does not rigidly follow the animated body: LocalDrift=" +
                    Num(waistSwordHipLocalDrift) +
                    ", PositionChange=" + Num(waistSwordBodyFollowPositionChange) +
                    ", RotationChangeDegrees=" + Num(waistSwordBodyFollowRotationChange) + ".");
            if (flashAnchorError > MatrixTolerance || flashAxisError > 0.001f)
                throw new InvalidOperationException(
                    "The approved muzzle flash is not aligned to the exact musket muzzle end: Position=" +
                    Num(flashAnchorError) + ", AxisDegrees=" + Num(flashAxisError) + ".");
            if (flashBeforeScaleError > MatrixTolerance ||
                flashFiringScaleError > MatrixTolerance ||
                flashAfterScaleError > MatrixTolerance)
                throw new InvalidOperationException(
                    "The muzzle flash visibility does not match the detected firing instant: Before=" +
                    Num(flashBeforeScaleError) + ", Firing=" + Num(flashFiringScaleError) +
                    ", After=" + Num(flashAfterScaleError) + ".");
            if (renderedBodyBottoms.PixelError > 1)
                throw new InvalidOperationException(
                    "The rendered slot-7 starting body bottom does not match the static model Y height: Static=" +
                    Num(staticBodyBottomY) + ", Target=" + Num(targetBodyBottomY) +
                    ", Error=" + Num(bodyBottomYError) +
                    ", PixelError=" + renderedBodyBottoms.PixelError + ".");
            return new Metrics(
                clip.length,
                clip.frameRate,
                PlaybackSpeed,
                clip.length / PlaybackSpeed,
                BreakthroughAttackIntervalSeconds,
                fireTiming.FireFrame,
                fireTiming.ClipTime,
                fireTiming.ClipTime / PlaybackSpeed,
                fireTiming.PeakRecoilEndFrame,
                fireTiming.PeakRearwardDelta,
                FlashDurationSeconds,
                flashAnchorError,
                flashAxisError,
                flashBeforeScaleError,
                flashFiringScaleError,
                flashAfterScaleError,
                appearanceRenderers.Length,
                musketRendererLocalMatrixError,
                musketModelRotationErrorDegrees,
                maximumMusketRightHandContactErrorMeters,
                maximumMusketLeftHandContactErrorMeters,
                maximumMusketLocalDrift,
                SharedMesh(targetRenderer).triangles.Length / 3,
                modelScaleError,
                allTargetRenderers.Length,
                backMusketRenderers.Length,
                maximumAimPoseStartRotationError,
                maximumAimPoseStartPositionError,
                targetModel.localPosition.y,
                staticBodyBottomY,
                targetBodyBottomY,
                bodyBottomYError,
                leftSupportGripStartError,
                rightGripStartError,
                waistSwordHipLocalDrift,
                waistSwordBodyFollowPositionChange,
                waistSwordBodyFollowRotationChange);
        }

        private static void CaptureMuzzleFlashReview(
            Transform targetModel,
            AnimationClip targetClip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException(
                                          "The slot-7 muzzle-flash capture folder is invalid."));
            const int captureLayer = 31;
            const int panelWidth = 640;
            const int panelHeight = 720;
            const int columns = 3;
            var sampled = CreateSamplingClone(
                targetModel, "Ispant07MuzzleFlashCaptureSample");
            sampled.position = new Vector3(0f, sampled.position.y, 0f);
            SetLayerRecursive(sampled, captureLayer);
            var renderers = sampled.GetComponentsInChildren<Renderer>(true);
            SetEnabled(renderers, true);
            var musket = RequireRenderer<MeshRenderer>(sampled, HandMusketRendererName);
            _ = RequireDescendant(sampled, MuzzleFlashName);
            var geometry = DetermineMusketMuzzleGeometry(SharedMesh(musket));
            var timing = DetermineFiringTiming(targetModel, targetClip);
            var flashClipDuration = FlashDurationSeconds * PlaybackSpeed;
            var beforeTime = timing.ClipTime > 0f
                ? Mathf.Max(0f, timing.ClipTime - 0.5f / targetClip.frameRate)
                : Mathf.Max(0f, targetClip.length - 0.5f / targetClip.frameRate);
            var firingTime = Mathf.Min(
                timing.ClipTime + flashClipDuration * 0.5f,
                targetClip.length);
            var afterTime = Mathf.Min(
                timing.ClipTime + flashClipDuration + 0.5f / targetClip.frameRate,
                targetClip.length);
            var times = new[] { beforeTime, firingTime, afterTime };
            var cameraObject = new GameObject(
                "Ispant07MuzzleFlashReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth, panelHeight, TextureFormat.RGB24, false);
            var strip = new Texture2D(
                panelWidth * columns, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                SampleClip(sampled.gameObject, targetClip, firingTime);
                var muzzlePosition = musket.transform.TransformPoint(geometry.LocalTip);
                var muzzleAxis = musket.transform.TransformDirection(
                    geometry.LocalAxis).normalized;
                var leftHand = RequireDescendant(sampled, "mixamorig:LeftHand");
                var rightHand = RequireDescendant(sampled, "mixamorig:RightHand");
                var focusBounds = musket.bounds;
                focusBounds.Encapsulate(leftHand.position);
                focusBounds.Encapsulate(rightHand.position);
                focusBounds.Encapsulate(muzzlePosition + muzzleAxis * 0.15f);
                var viewDirection = Vector3.Cross(Vector3.up, muzzleAxis);
                if (viewDirection.sqrMagnitude < 0.000001f)
                    viewDirection = sampled.right;
                viewDirection.Normalize();

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.7f, 0.72f, 0.75f, 1f);
                camera.cullingMask = 1 << captureLayer;
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = target;
                var distance = Mathf.Max(focusBounds.size.magnitude, 0.1f) * 0.5f /
                               Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.2f;
                camera.transform.position = focusBounds.center + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focusBounds.center - camera.transform.position, Vector3.up);

                for (var index = 0; index < times.Length; index++)
                {
                    SampleClip(sampled.gameObject, targetClip, times[index]);
                    RenderFixedCameraPanel(
                        camera,
                        panel,
                        strip,
                        target,
                        index,
                        panelWidth,
                        panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampled.gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureGroundAlignmentReview(
            Transform staticModel,
            Transform targetModel,
            AnimationClip targetClip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException(
                                          "The slot-7 ground-alignment capture folder is invalid."));
            const int captureLayer = 31;
            const int panelWidth = 640;
            const int panelHeight = 720;
            const int columns = 4;
            var sampledStatic = CreateSamplingClone(
                staticModel, "IspantStaticGroundAlignmentSample");
            var sampledTarget = CreateSamplingClone(
                targetModel, "Ispant07GroundAlignmentSample");
            sampledStatic.position = new Vector3(0f, sampledStatic.position.y, 0f);
            sampledTarget.position = new Vector3(0f, sampledTarget.position.y, 0f);
            SetLayerRecursive(sampledStatic, captureLayer);
            SetLayerRecursive(sampledTarget, captureLayer);
            var staticRenderers = sampledStatic.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = sampledTarget.GetComponentsInChildren<Renderer>(true);
            var cameraObject = new GameObject(
                "Ispant07GroundAlignmentReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth, panelHeight, TextureFormat.RGB24, false);
            var strip = new Texture2D(
                panelWidth * columns, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.7f, 0.72f, 0.75f, 1f);
                camera.cullingMask = 1 << captureLayer;
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.targetTexture = target;

                SetEnabled(staticRenderers, true);
                SetEnabled(targetRenderers, false);
                var sharedBounds = CombinedBounds(staticRenderers.Where(item => item.enabled).ToArray());
                var times = new[] { 0f, 0.5f, 1f };
                foreach (var normalizedTime in times)
                {
                    SampleClip(
                        sampledTarget.gameObject,
                        targetClip,
                        normalizedTime * targetClip.length);
                    SetEnabled(targetRenderers, true);
                    sharedBounds.Encapsulate(CombinedBounds(targetRenderers));
                    SetEnabled(targetRenderers, false);
                }
                FrameCamera(camera, sharedBounds, panelWidth / (float)panelHeight);

                SetEnabled(staticRenderers, true);
                RenderFixedCameraPanel(
                    camera, panel, strip, target, 0, panelWidth, panelHeight);
                SetEnabled(staticRenderers, false);
                for (var index = 0; index < times.Length; index++)
                {
                    SampleClip(
                        sampledTarget.gameObject,
                        targetClip,
                        times[index] * targetClip.length);
                    SetEnabled(targetRenderers, true);
                    RenderFixedCameraPanel(
                        camera, panel, strip, target,
                        index + 1, panelWidth, panelHeight);
                    SetEnabled(targetRenderers, false);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledStatic.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RenderFixedCameraPanel(
            Camera camera,
            Texture2D panel,
            Texture2D strip,
            RenderTexture target,
            int panelIndex,
            int width,
            int height)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException(
                    "The slot-7 ground-alignment review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void CaptureReview(
            Transform staticModel,
            Transform sourceModel,
            Transform targetModel,
            AnimationClip sourceFinalAimClip,
            AnimationClip targetClip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("The slot-7 capture folder is invalid."));
            const int captureLayer = 31;
            var sampledStatic = CreateSamplingClone(staticModel, "IspantStaticCaptureSample");
            var sampledSource = CreateSamplingClone(sourceModel, "Ispant06CaptureSample");
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant07CaptureSample");
            SetLayerRecursive(sampledStatic, captureLayer);
            SetLayerRecursive(sampledSource, captureLayer);
            SetLayerRecursive(sampledTarget, captureLayer);
            var staticRenderers = sampledStatic.GetComponentsInChildren<Renderer>(true);
            var sourceRenderers = sampledSource.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = sampledTarget.GetComponentsInChildren<Renderer>(true);
            var cameraObject = new GameObject("Ispant07FiringReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            const int panelWidth = 640;
            const int panelHeight = 720;
            const int panels = 5;
            var strip = new Texture2D(panelWidth * panels, panelHeight, TextureFormat.RGB24, false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                SetEnabled(sourceRenderers, false);
                SetEnabled(targetRenderers, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << captureLayer;
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.targetTexture = target;

                SetEnabled(staticRenderers, true);
                RenderPanel(camera, sampledStatic, panel, strip, target, 0, panelWidth, panelHeight);
                SetEnabled(staticRenderers, false);

                SampleClip(sampledSource.gameObject, sourceFinalAimClip, sourceFinalAimClip.length);
                SetCharacterAppearanceEnabled(sourceRenderers, true);
                RenderPanel(camera, sampledSource, panel, strip, target, 1, panelWidth, panelHeight);
                SetEnabled(sourceRenderers, false);

                var times = new[] { 0f, 0.5f, 1f };
                for (var index = 0; index < times.Length; index++)
                {
                    SampleClip(sampledTarget.gameObject, targetClip, times[index] * targetClip.length);
                    SetEnabled(targetRenderers, true);
                    RenderPanel(
                        camera,
                        sampledTarget,
                        panel,
                        strip,
                        target,
                        index + 2,
                        panelWidth,
                        panelHeight);
                    SetEnabled(targetRenderers, false);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledStatic.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledSource.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureGripDiagnostic(
            Transform sourceModel,
            Transform targetModel,
            AnimationClip sourceFinalAimClip,
            AnimationClip targetClip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException(
                                          "The slot-6/7 grip diagnostic folder is invalid."));
            const int captureLayer = 31;
            const int panelWidth = 640;
            const int panelHeight = 640;
            const int columns = 4;
            const int rows = 3;
            var sampledSource = CreateSamplingClone(sourceModel, "Ispant06GripDiagnosticSample");
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant07GripDiagnosticSample");
            SetLayerRecursive(sampledSource, captureLayer);
            SetLayerRecursive(sampledTarget, captureLayer);
            var sourceRenderers = sampledSource.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = sampledTarget.GetComponentsInChildren<Renderer>(true);
            var cameraObject = new GameObject("Ispant06And07GripDiagnosticCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth, panelHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelWidth * columns, panelHeight * rows, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.7f, 0.72f, 0.75f, 1f);
                camera.cullingMask = 1 << captureLayer;
                camera.fieldOfView = 28f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.targetTexture = target;
                var viewDirections = new[]
                {
                    Vector3.forward,
                    Vector3.left,
                    Vector3.right
                };
                for (var row = 0; row < rows; row++)
                {
                    SetEnabled(targetRenderers, false);
                    SetEnabled(sourceRenderers, false);
                    SampleClip(
                        sampledSource.gameObject,
                        sourceFinalAimClip,
                        sourceFinalAimClip.length);
                    SetGripDiagnosticRenderersEnabled(
                        sourceRenderers, SourceHandMusketRendererName, true);
                    RenderGripDiagnosticPanel(
                        camera, sampledSource,
                        SourceHandMusketRendererName,
                        viewDirections[row], panel, sheet, target,
                        0, row, panelWidth, panelHeight);
                    SetEnabled(sourceRenderers, false);

                    var normalizedTimes = new[] { 0f, 0.5f, 1f };
                    for (var column = 1; column < columns; column++)
                    {
                        SetEnabled(targetRenderers, false);
                        SampleClip(
                            sampledTarget.gameObject,
                            targetClip,
                            normalizedTimes[column - 1] * targetClip.length);
                        SetGripDiagnosticRenderersEnabled(
                            targetRenderers, HandMusketRendererName, true);
                        RenderGripDiagnosticPanel(
                            camera, sampledTarget,
                            HandMusketRendererName,
                            viewDirections[row], panel, sheet, target,
                            column, row, panelWidth, panelHeight);
                        SetEnabled(targetRenderers, false);
                    }
                }
                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledSource.gameObject);
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void SetGripDiagnosticRenderersEnabled(
            IEnumerable<Renderer> renderers,
            string musketRendererName,
            bool enabled)
        {
            foreach (var renderer in renderers)
                renderer.enabled = enabled &&
                    (StaticAppearanceRendererNames.Contains(
                         renderer.name, StringComparer.Ordinal) ||
                     renderer.name == musketRendererName);
        }

        private static void RenderGripDiagnosticPanel(
            Camera camera,
            Transform model,
            string musketRendererName,
            Vector3 viewDirection,
            Texture2D panel,
            Texture2D sheet,
            RenderTexture target,
            int column,
            int row,
            int width,
            int height)
        {
            var leftHand = RequireDescendant(model, "mixamorig:LeftHand");
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightShoulder = RequireDescendant(model, "mixamorig:RightShoulder");
            _ = RequireRenderer<MeshRenderer>(model, musketRendererName);
            var center = (leftHand.position + rightHand.position + rightShoulder.position) / 3f;
            center += Vector3.up * 0.04f;
            camera.aspect = width / (float)height;
            const float framedHeight = 0.9f;
            var distance = (framedHeight * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.08f;
            camera.transform.position = center + viewDirection.normalized * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException(
                    "The slot-6/7 grip diagnostic contains magenta shader fallback.");
            sheet.SetPixels32(column * width, row * height, width, height, pixels);
        }

        private static void SetCharacterAppearanceEnabled(Renderer[] renderers, bool enabled)
        {
            foreach (var renderer in renderers)
            {
                if (StaticAppearanceRendererNames.Contains(renderer.name, StringComparer.Ordinal))
                    renderer.enabled = enabled;
            }
            var handMusket = renderers.SingleOrDefault(
                item => item.name == SourceHandMusketRendererName);
            if (handMusket != null)
                handMusket.enabled = enabled;
        }

        private static void SetEnabled(IEnumerable<Renderer> renderers, bool enabled)
        {
            foreach (var renderer in renderers)
                renderer.enabled = enabled;
        }

        private static void SetLayerRecursive(Transform root, int layer)
        {
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static void RenderPanel(
            Camera camera,
            Transform model,
            Texture2D panel,
            Texture2D strip,
            RenderTexture target,
            int panelIndex,
            int width,
            int height)
        {
            var bounds = CombinedBounds(model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray());
            FrameCamera(camera, bounds, width / (float)height);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The slot-7 review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(Camera camera, Bounds bounds, float aspect)
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
            camera.transform.position = bounds.center + Vector3.forward * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position, Vector3.up);
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            if (renderers.Length == 0)
                throw new InvalidOperationException("No visible renderers were found for slot-7 review.");
            var result = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("The slot-7 inspection folder is invalid."));
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
                "FrameRate=" + Num(metrics.FrameRate),
                "Loop=True",
                "PlaybackSpeed=" + Num(metrics.PlaybackSpeed),
                "EffectiveLoopSeconds=" + Num(metrics.EffectiveLoopSeconds),
                "DesignSourceMode=Ispant_BreakthroughMusket",
                "DesignSourceAttackIntervalSeconds=" +
                Num(metrics.DesignSourceAttackIntervalSeconds),
                "DetectedFireFrame=" + metrics.FireFrame,
                "DetectedFireClipTimeSeconds=" + Num(metrics.FireClipTime),
                "DetectedFireCycleTimeSeconds=" + Num(metrics.FireCycleTime),
                "PeakRecoilEndFrame=" + metrics.PeakRecoilEndFrame,
                "PeakRearwardMuzzleDeltaMeters=" + Num(metrics.PeakRearwardMuzzleDelta),
                "MuzzleFlashDurationSeconds=" + Num(metrics.MuzzleFlashDuration),
                "MuzzleFlashMesh=" + ApprovedFlashMeshPath,
                "MuzzleFlashMaterial=" + ApprovedFlashMaterialPath,
                "MuzzleFlashAnchorErrorMeters=" + Num(metrics.MuzzleFlashAnchorError),
                "MuzzleFlashAxisErrorDegrees=" + Num(metrics.MuzzleFlashAxisError),
                "MuzzleFlashBeforeScaleError=" + Num(metrics.MuzzleFlashBeforeScaleError),
                "MuzzleFlashFiringScaleError=" + Num(metrics.MuzzleFlashFiringScaleError),
                "MuzzleFlashAfterScaleError=" + Num(metrics.MuzzleFlashAfterScaleError),
                "StaticAppearanceRendererCount=" + metrics.AppearanceRendererCount,
                "StaticAppearanceSharedMeshes=True",
                "StaticAppearanceSharedMaterials=True",
                "StaticAppearanceCalibratedFromSlot6=True",
                "ModelScaleReference=Ispant_06_SheathSwordDrawMusket",
                "ModelScaleError=" + Num(metrics.ModelScaleError),
                "VisibleRendererCount=" + metrics.VisibleRendererCount,
                "BackMusketRendererCount=" + metrics.BackMusketRendererCount,
                "BodySubMeshCountWithoutBackMusket=7",
                "CrescentSkinned=True",
                "EyeSlitsSkinned=True",
                "MusketSource=Ispant_06_FinalAim",
                "MusketParent=mixamorig:RightHand",
                "MusketTriangles=" + metrics.MusketTriangles,
                "MusketRendererLocalMatrixError=" + Num(metrics.MusketRendererLocalMatrixError),
                "MusketFinalAimModelRotationErrorDegrees=" +
                Num(metrics.MusketModelRotationErrorDegrees),
                "MusketMaximumRightHandContactErrorMeters=" +
                Num(metrics.MusketMaximumRightHandContactErrorMeters),
                "MusketMaximumLeftHandContactErrorMeters=" +
                Num(metrics.MusketMaximumLeftHandContactErrorMeters),
                "MusketLocalMountMaximumDrift=" + Num(metrics.MusketLocalMountMaximumDrift),
                "LeftWaistSwordSource=Ispant_06_SheathSwordDrawMusket exact shared mesh/material and local mount",
                "LeftWaistSwordParent=mixamorig:Hips",
                "LeftWaistSwordHipLocalMatrixDrift=" + Num(metrics.WaistSwordHipLocalDrift),
                "LeftWaistSwordBodyFollowPositionChange=" +
                Num(metrics.WaistSwordBodyFollowPositionChange),
                "LeftWaistSwordBodyFollowRotationChangeDegrees=" +
                Num(metrics.WaistSwordBodyFollowRotationChange),
                "AimPoseStartMaximumRotationErrorDegrees=" +
                Num(metrics.AimPoseStartMaximumRotationError),
                "AimPoseStartMaximumShoulderPositionErrorMeters=" +
                Num(metrics.AimPoseStartMaximumShoulderPositionError),
                "TargetModelLocalY=" + Num(metrics.TargetModelLocalY),
                "StaticBodyBottomWorldY=" + Num(metrics.StaticBodyBottomWorldY),
                "TargetBodyBottomWorldYAtStart=" +
                Num(metrics.TargetBodyBottomWorldYAtStart),
                "BodyBottomYErrorMeters=" + Num(metrics.BodyBottomYError),
                "LeftSupportGripStartErrorMeters=" + Num(metrics.LeftSupportGripStartError),
                "RightGripStartErrorMeters=" + Num(metrics.RightGripStartError),
                "RootMotion=False",
                "OtherIspantSlotsChanged=False",
                "SceneChanged=False",
                "ReviewImage=" + CapturePath
            });
        }

        private static void WriteGeometryDiagnostic(
            Transform staticModel,
            Transform sourceModel,
            Transform targetModel,
            AnimationClip targetClip)
        {
            var destination = Absolute(GeometryDiagnosticPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("The slot-7 diagnostic folder is invalid."));
            var lines = new List<string>
            {
                "StaticModel=" + TransformLine(staticModel),
                "SourceModel=" + TransformLine(sourceModel),
                "TargetModel=" + TransformLine(targetModel),
                "[StaticRenderers]"
            };
            lines.AddRange(staticModel.GetComponentsInChildren<Renderer>(true)
                .Select(item => RendererLine(staticModel, item)));
            lines.Add("[Slot6Renderers]");
            lines.AddRange(sourceModel.GetComponentsInChildren<Renderer>(true)
                .Select(item => RendererLine(sourceModel, item)));
            lines.Add("[Slot7RenderersAtStart]");
            var sampledTarget = CreateSamplingClone(targetModel, "Ispant07GeometrySample");
            try
            {
                SampleClip(sampledTarget.gameObject, targetClip, 0f);
                lines.AddRange(sampledTarget.GetComponentsInChildren<Renderer>(true)
                    .Select(item => RendererLine(sampledTarget, item)));
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledTarget.gameObject);
            }
            var firingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                               throw new InvalidOperationException("The firing FBX prefab is unavailable for diagnostics.");
            var sampledFiringPrefab = UnityEngine.Object.Instantiate(firingPrefab).transform;
            sampledFiringPrefab.name = "IspantFiringSourceSample";
            sampledFiringPrefab.gameObject.hideFlags = HideFlags.HideAndDontSave;
            lines.Add("[FiringSourceRenderersAtStart]");
            try
            {
                SampleClip(sampledFiringPrefab.gameObject, RequireFiringClip(), 0f);
                lines.AddRange(sampledFiringPrefab.GetComponentsInChildren<Renderer>(true)
                    .Select(item => RendererLine(sampledFiringPrefab, item)));
                lines.Add("[FiringSourceTransformCurves]");
                lines.AddRange(AnimationUtility.GetCurveBindings(RequireFiringClip()).Select(binding =>
                    binding.path + "|" + binding.type.Name + "|" + binding.propertyName));
            }
            finally
            {
                StopSampling();
                UnityEngine.Object.DestroyImmediate(sampledFiringPrefab.gameObject);
            }
            File.WriteAllLines(destination, lines);
        }

        private static string RendererLine(Transform model, Renderer renderer)
        {
            var skinned = renderer as SkinnedMeshRenderer;
            var mesh = SharedMesh(renderer);
            var materials = renderer.sharedMaterials.Select(material =>
                (material == null ? "<null>" : material.name) + "@" +
                (material == null ? "<none>" : AssetDatabase.GetAssetPath(material)));
            return RelativePath(model, renderer.transform) +
                   "|Type=" + renderer.GetType().Name +
                   "|Parent=" + (renderer.transform.parent == null ? "<none>" : renderer.transform.parent.name) +
                   "|Mesh=" + mesh.name +
                   "|MeshAsset=" + AssetDatabase.GetAssetPath(mesh) +
                   "|Vertices=" + mesh.vertexCount +
                   "|SubMeshes=" + mesh.subMeshCount +
                   "|Materials=" + string.Join(",", materials) +
                   "|Enabled=" + renderer.enabled +
                   "|LocalPosition=" + Vec(renderer.transform.localPosition) +
                   "|LocalScale=" + Vec(renderer.transform.localScale) +
                   "|BoundsCenter=" + Vec(renderer.bounds.center) +
                   "|BoundsSize=" + Vec(renderer.bounds.size) +
                   "|RootBone=" + (skinned == null || skinned.rootBone == null ? "<none>" : skinned.rootBone.name) +
                   "|Bones=" + (skinned == null ? 0 : skinned.bones.Length) +
                   "|BoneNames=" + (skinned == null
                       ? "<none>"
                       : string.Join(",", skinned.bones.Select(item =>
                           item == null ? "<null>" : item.name)));
        }

        private static string TransformLine(Transform transform)
        {
            return transform.name +
                   "|Position=" + Vec(transform.position) +
                   "|LocalPosition=" + Vec(transform.localPosition) +
                   "|LocalScale=" + Vec(transform.localScale) +
                   "|LossyScale=" + Vec(transform.lossyScale);
        }

        private static string RelativePath(Transform root, Transform item)
        {
            if (item == root)
                return root.name;
            var names = new Stack<string>();
            var current = item;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            if (current != root)
                throw new InvalidOperationException("A diagnostic renderer is outside its Ispant model root.");
            return root.name + "/" + string.Join("/", names);
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene for slot-7 firing work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved changes; preserve them before slot-7 firing work.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var matches = scene.GetRootGameObjects()
                .Where(item => item.name == PlacementRootName).ToArray();
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

        private static Transform RequireTargetSlot(Transform placement)
        {
            if (placement.childCount <= 6)
                throw new InvalidOperationException("Ispant slot 7 is missing.");
            var target = placement.GetChild(6);
            if (target.name != PreviousTargetSlotName && target.name != TargetSlotName)
                throw new InvalidOperationException("The existing Ispant slot-7 identity differs: " + target.name + ".");
            return target;
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return Enumerable.Range(0, parent.childCount)
                       .Select(parent.GetChild)
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       "Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required Ispant renderer is missing: " + name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required firing rig transform differs: " + name + ".");
            return matches[0];
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

        private static Vector3 VisibleHandAnchorLocal(
            SkinnedMeshRenderer body,
            Transform handBone)
        {
            var mesh = body.sharedMesh ??
                       throw new InvalidOperationException(
                           "The Ispant body mesh is missing while locating the visible hand.");
            var handBoneIndices = new HashSet<int>(Enumerable.Range(0, body.bones.Length)
                .Where(index => body.bones[index] == handBone ||
                                (body.bones[index] != null &&
                                 body.bones[index].IsChildOf(handBone))));
            if (handBoneIndices.Count == 0)
                throw new InvalidOperationException(
                    "The visible Ispant hand bone is not bound to the body mesh: " +
                    handBone.name + ".");
            var weights = mesh.boneWeights;
            if (weights.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "The Ispant body bone weights do not match its vertices.");
            var vertices = mesh.vertices;
            var bindPoses = mesh.bindposes;
            var selected = Enumerable.Range(0, vertices.Length)
                .Where(index => BoneWeightForIndices(
                    weights[index], handBoneIndices) >= 0.25f)
                .Select(index => handBone.InverseTransformPoint(
                    SkinnedVertexWorld(
                        vertices[index], weights[index], body.bones, bindPoses)))
                .ToArray();
            if (selected.Length < 4)
                throw new InvalidOperationException(
                    "The visible Ispant hand region has too few weighted vertices: " +
                    handBone.name + ", Vertices=" + selected.Length + ".");
            var bounds = new Bounds(selected[0], Vector3.zero);
            for (var index = 1; index < selected.Length; index++)
                bounds.Encapsulate(selected[index]);
            return bounds.center;
        }

        private static Vector3 SkinnedVertexWorld(
            Vector3 vertex,
            BoneWeight weight,
            IReadOnlyList<Transform> bones,
            IReadOnlyList<Matrix4x4> bindPoses)
        {
            var result = Vector3.zero;
            result += weight.weight0 * (bones[weight.boneIndex0].localToWorldMatrix *
                                        bindPoses[weight.boneIndex0]).MultiplyPoint3x4(vertex);
            result += weight.weight1 * (bones[weight.boneIndex1].localToWorldMatrix *
                                        bindPoses[weight.boneIndex1]).MultiplyPoint3x4(vertex);
            result += weight.weight2 * (bones[weight.boneIndex2].localToWorldMatrix *
                                        bindPoses[weight.boneIndex2]).MultiplyPoint3x4(vertex);
            result += weight.weight3 * (bones[weight.boneIndex3].localToWorldMatrix *
                                        bindPoses[weight.boneIndex3]).MultiplyPoint3x4(vertex);
            return result;
        }

        private static float BoneWeightForIndices(
            BoneWeight weight,
            ISet<int> boneIndices)
        {
            var result = 0f;
            if (boneIndices.Contains(weight.boneIndex0))
                result += weight.weight0;
            if (boneIndices.Contains(weight.boneIndex1))
                result += weight.weight1;
            if (boneIndices.Contains(weight.boneIndex2))
                result += weight.weight2;
            if (boneIndices.Contains(weight.boneIndex3))
                result += weight.weight3;
            return result;
        }

        private static Transform CreateSamplingClone(Transform original, string name)
        {
            var clone = UnityEngine.Object.Instantiate(original.gameObject);
            clone.name = name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            return clone.transform;
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

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            DecomposeMatrix(matrix, out var position, out var rotation, out var scale);
            target.SetLocalPositionAndRotation(position, rotation);
            target.localScale = scale;
        }

        private static void DecomposeMatrix(
            Matrix4x4 matrix,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale)
        {
            position = matrix.GetColumn(3);
            var x = (Vector3)matrix.GetColumn(0);
            var y = (Vector3)matrix.GetColumn(1);
            var z = (Vector3)matrix.GetColumn(2);
            scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                throw new InvalidOperationException("A copied Ispant transform has invalid scale.");
            rotation = Quaternion.LookRotation(z / scale.z, y / scale.y);
        }

        private static Matrix4x4 LocalMatrix(Transform transform)
        {
            return Matrix4x4.TRS(
                transform.localPosition,
                transform.localRotation,
                transform.localScale);
        }

        private static Matrix4x4 ModelRelativeMatrix(Transform model, Transform item)
        {
            var chain = new Stack<Transform>();
            var current = item;
            while (current != null && current != model)
            {
                chain.Push(current);
                current = current.parent;
            }
            if (current != model)
                throw new InvalidOperationException(
                    "An Ispant transform is outside the expected model root: " + item.name + ".");
            var result = Matrix4x4.identity;
            while (chain.Count > 0)
                result *= LocalMatrix(chain.Pop());
            return result;
        }

        private static float MatrixError(Matrix4x4 expected, Matrix4x4 actual)
        {
            var maximum = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                maximum = Mathf.Max(maximum, Mathf.Abs(expected[row, column] - actual[row, column]));
            return maximum;
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(item => item != targetSlot)
                .Select(RecursiveSignature).ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Num(item.localRotation.x)).Append(',')
                    .Append(Num(item.localRotation.y)).Append(',')
                    .Append(Num(item.localRotation.z)).Append(',')
                    .Append(Num(item.localRotation.w)).Append('|')
                    .Append(Vec(item.localScale));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled).Append(':')
                        .Append(AssetDatabase.GetAssetPath(SharedMesh(renderer)));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (expected.SequenceEqual(actual, StringComparer.Ordinal))
                return;

            var differingIndex = Enumerable.Range(0, Mathf.Min(expected.Length, actual.Length))
                .FirstOrDefault(index => !string.Equals(expected[index], actual[index], StringComparison.Ordinal));
            var expectedValue = differingIndex < expected.Length ? expected[differingIndex] : "<missing>";
            var actualValue = differingIndex < actual.Length ? actual[differingIndex] : "<missing>";
            var differingCharacter = 0;
            while (differingCharacter < expectedValue.Length &&
                   differingCharacter < actualValue.Length &&
                   expectedValue[differingCharacter] == actualValue[differingCharacter])
                differingCharacter++;
            var excerptStart = Mathf.Max(0, differingCharacter - 80);
            var expectedExcerpt = expectedValue.Substring(
                excerptStart, Mathf.Min(200, expectedValue.Length - excerptStart));
            var actualExcerpt = actualValue.Substring(
                excerptStart, Mathf.Min(200, actualValue.Length - excerptStart));
            throw new InvalidOperationException(
                message + " Index=" + differingIndex +
                ", ExpectedCount=" + expected.Length +
                ", ActualCount=" + actual.Length +
                ", Character=" + differingCharacter +
                ", ExpectedExcerpt=" + expectedExcerpt +
                ", ActualExcerpt=" + actualExcerpt + ".");
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectFbxPath, SourceSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            var actual = Sha256(Absolute(path));
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The approved Ispant firing source hash differs: " + path + "=" + actual + ".");
        }

        private static string Sha256(string path)
        {
            using var algorithm = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private sealed class TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Restore()
            {
                if (target == null)
                    return;
                target.SetLocalPositionAndRotation(localPosition, localRotation);
                target.localScale = localScale;
            }

            public bool Matches(float tolerance)
            {
                return target != null &&
                       Vector3.Distance(target.localPosition, localPosition) <= tolerance &&
                       Quaternion.Angle(target.localRotation, localRotation) <= tolerance &&
                       Vector3.Distance(target.localScale, localScale) <= tolerance;
            }
        }

        private sealed class RendererSnapshot
        {
            public Renderer Renderer { get; }
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (Renderer != null)
                    Renderer.enabled = enabled;
            }
        }

        private readonly struct MuzzleGeometry
        {
            public readonly Vector3 LocalAxis;
            public readonly Vector3 LocalTip;

            public MuzzleGeometry(Vector3 localAxis, Vector3 localTip)
            {
                LocalAxis = localAxis;
                LocalTip = localTip;
            }
        }

        private readonly struct FireTiming
        {
            public readonly int FireFrame;
            public readonly float ClipTime;
            public readonly int PeakRecoilEndFrame;
            public readonly float PeakRearwardDelta;

            public FireTiming(
                int fireFrame,
                float clipTime,
                int peakRecoilEndFrame,
                float peakRearwardDelta)
            {
                FireFrame = fireFrame;
                ClipTime = clipTime;
                PeakRecoilEndFrame = peakRecoilEndFrame;
                PeakRearwardDelta = peakRearwardDelta;
            }
        }

        private readonly struct RenderedBodyBottoms
        {
            public readonly float StaticWorldY;
            public readonly float TargetWorldY;
            public readonly int StaticPixel;
            public readonly int TargetPixel;
            public readonly float WorldUnitsPerPixel;

            public int PixelError => Mathf.Abs(StaticPixel - TargetPixel);

            public RenderedBodyBottoms(
                float staticWorldY,
                float targetWorldY,
                int staticPixel,
                int targetPixel,
                float worldUnitsPerPixel)
            {
                StaticWorldY = staticWorldY;
                TargetWorldY = targetWorldY;
                StaticPixel = staticPixel;
                TargetPixel = targetPixel;
                WorldUnitsPerPixel = worldUnitsPerPixel;
            }
        }

        private readonly struct Metrics
        {
            public readonly float ClipLength;
            public readonly float FrameRate;
            public readonly float PlaybackSpeed;
            public readonly float EffectiveLoopSeconds;
            public readonly float DesignSourceAttackIntervalSeconds;
            public readonly int FireFrame;
            public readonly float FireClipTime;
            public readonly float FireCycleTime;
            public readonly int PeakRecoilEndFrame;
            public readonly float PeakRearwardMuzzleDelta;
            public readonly float MuzzleFlashDuration;
            public readonly float MuzzleFlashAnchorError;
            public readonly float MuzzleFlashAxisError;
            public readonly float MuzzleFlashBeforeScaleError;
            public readonly float MuzzleFlashFiringScaleError;
            public readonly float MuzzleFlashAfterScaleError;
            public readonly int AppearanceRendererCount;
            public readonly float MusketRendererLocalMatrixError;
            public readonly float MusketModelRotationErrorDegrees;
            public readonly float MusketMaximumRightHandContactErrorMeters;
            public readonly float MusketMaximumLeftHandContactErrorMeters;
            public readonly float MusketLocalMountMaximumDrift;
            public readonly int MusketTriangles;
            public readonly float ModelScaleError;
            public readonly int VisibleRendererCount;
            public readonly int BackMusketRendererCount;
            public readonly float AimPoseStartMaximumRotationError;
            public readonly float AimPoseStartMaximumShoulderPositionError;
            public readonly float TargetModelLocalY;
            public readonly float StaticBodyBottomWorldY;
            public readonly float TargetBodyBottomWorldYAtStart;
            public readonly float BodyBottomYError;
            public readonly float LeftSupportGripStartError;
            public readonly float RightGripStartError;
            public readonly float WaistSwordHipLocalDrift;
            public readonly float WaistSwordBodyFollowPositionChange;
            public readonly float WaistSwordBodyFollowRotationChange;

            public Metrics(
                float clipLength,
                float frameRate,
                float playbackSpeed,
                float effectiveLoopSeconds,
                float designSourceAttackIntervalSeconds,
                int fireFrame,
                float fireClipTime,
                float fireCycleTime,
                int peakRecoilEndFrame,
                float peakRearwardMuzzleDelta,
                float muzzleFlashDuration,
                float muzzleFlashAnchorError,
                float muzzleFlashAxisError,
                float muzzleFlashBeforeScaleError,
                float muzzleFlashFiringScaleError,
                float muzzleFlashAfterScaleError,
                int appearanceRendererCount,
                float musketRendererLocalMatrixError,
                float musketModelRotationErrorDegrees,
                float musketMaximumRightHandContactErrorMeters,
                float musketMaximumLeftHandContactErrorMeters,
                float musketLocalMountMaximumDrift,
                int musketTriangles,
                float modelScaleError,
                int visibleRendererCount,
                int backMusketRendererCount,
                float aimPoseStartMaximumRotationError,
                float aimPoseStartMaximumShoulderPositionError,
                float targetModelLocalY,
                float staticBodyBottomWorldY,
                float targetBodyBottomWorldYAtStart,
                float bodyBottomYError,
                float leftSupportGripStartError,
                float rightGripStartError,
                float waistSwordHipLocalDrift,
                float waistSwordBodyFollowPositionChange,
                float waistSwordBodyFollowRotationChange)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                PlaybackSpeed = playbackSpeed;
                EffectiveLoopSeconds = effectiveLoopSeconds;
                DesignSourceAttackIntervalSeconds = designSourceAttackIntervalSeconds;
                FireFrame = fireFrame;
                FireClipTime = fireClipTime;
                FireCycleTime = fireCycleTime;
                PeakRecoilEndFrame = peakRecoilEndFrame;
                PeakRearwardMuzzleDelta = peakRearwardMuzzleDelta;
                MuzzleFlashDuration = muzzleFlashDuration;
                MuzzleFlashAnchorError = muzzleFlashAnchorError;
                MuzzleFlashAxisError = muzzleFlashAxisError;
                MuzzleFlashBeforeScaleError = muzzleFlashBeforeScaleError;
                MuzzleFlashFiringScaleError = muzzleFlashFiringScaleError;
                MuzzleFlashAfterScaleError = muzzleFlashAfterScaleError;
                AppearanceRendererCount = appearanceRendererCount;
                MusketRendererLocalMatrixError = musketRendererLocalMatrixError;
                MusketModelRotationErrorDegrees = musketModelRotationErrorDegrees;
                MusketMaximumRightHandContactErrorMeters = musketMaximumRightHandContactErrorMeters;
                MusketMaximumLeftHandContactErrorMeters = musketMaximumLeftHandContactErrorMeters;
                MusketLocalMountMaximumDrift = musketLocalMountMaximumDrift;
                MusketTriangles = musketTriangles;
                ModelScaleError = modelScaleError;
                VisibleRendererCount = visibleRendererCount;
                BackMusketRendererCount = backMusketRendererCount;
                AimPoseStartMaximumRotationError = aimPoseStartMaximumRotationError;
                AimPoseStartMaximumShoulderPositionError =
                    aimPoseStartMaximumShoulderPositionError;
                TargetModelLocalY = targetModelLocalY;
                StaticBodyBottomWorldY = staticBodyBottomWorldY;
                TargetBodyBottomWorldYAtStart = targetBodyBottomWorldYAtStart;
                BodyBottomYError = bodyBottomYError;
                LeftSupportGripStartError = leftSupportGripStartError;
                RightGripStartError = rightGripStartError;
                WaistSwordHipLocalDrift = waistSwordHipLocalDrift;
                WaistSwordBodyFollowPositionChange = waistSwordBodyFollowPositionChange;
                WaistSwordBodyFollowRotationChange = waistSwordBodyFollowRotationChange;
            }
        }
    }
}
