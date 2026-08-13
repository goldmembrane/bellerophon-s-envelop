using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaDeathAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_09_Death";
        private const string ModelName = "Ata_Model";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_Death.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_09_Death.controller";
        private const string PreFallStaticArmsClipPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_09_Death_PreFallStaticArms.anim";
        private const string CapturePath =
            "docs/validation/ata09_death_animation_2026-08-13/Ata_09_Death_TwoLoopReview.png";
        private const string ReportPath =
            "docs/validation/ata09_death_animation_2026-08-13/Ata_09_Death_Report.txt";
        private const string PreFallStaticArmsCapturePath =
            "docs/validation/ata09_death_prefall_static_arms_2026-08-13/Ata_09_Death_PreFallStaticArms_TwoLoopReview.png";
        private const string PreFallStaticArmsReportPath =
            "docs/validation/ata09_death_prefall_static_arms_2026-08-13/Ata_09_Death_PreFallStaticArms_Report.txt";
        private const string SourceSha256 =
            "C176051E3FA598A93F1F2DF1F0AA0A926C91AD2195EAD6A66558F709DAD1A7E9";
        private const string StateName = "AtaDeath";
        private const float TransformTolerance = 0.0002f;
        // These thresholds define the first clear falling frame from source motion:
        // either the pelvis drops by 2% of body height or the upper torso tilts 10 degrees.
        private const float FallDropHeightFraction = 0.02f;
        private const float FallTiltThresholdDegrees = 10f;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Death Animation")]
        public static void ApplyAtaDeathAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, slot);

            ConfigureMixamoClipLoop();
            var clip = RequireMixamoClip();
            var controller = CreateController(clip);
            var animator = ConfigureAnimator(model, controller);
            var rightArmBefore = AtaOtherSlotsRightArmMeshTool.DescribeModelForClips(
                model,
                new[] { clip });
            var correctedRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.CorrectModelForClips(
                    SlotName,
                    model,
                    new[] { clip },
                    maximumComponentTriangles: 512);
            var remainingRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    model,
                    new[] { clip },
                    out var maximumRightArmStretchRatio);
            if (remainingRightArmComponents != 0)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death still contains right-arm stretch components after apply.");
            }

            if (!slotBefore.Matches() || !modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_09_Death slot or model transform changed while applying the death clip.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, slot),
                "An Ata slot outside Ata_09_Death changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            RequireAppliedState(model, animator, clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata death animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaDeathAnimationApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Source=" + SourcePath +
                ", EmbeddedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", Loop=True" +
                ", StateSpeed=1" +
                ", RootMotion=False" +
                ", RightArmBefore={" + rightArmBefore + "}" +
                ", CorrectedRightArmComponents=" + correctedRightArmComponents +
                ", RemainingRightArmComponents=" + remainingRightArmComponents +
                ", MaximumRightArmStretchRatioAfter=" + Num(maximumRightArmStretchRatio) +
                ", SlotTransformFixed=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Death Animation")]
        public static void CaptureAtaDeathAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, slot);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_09_Death controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var remainingRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    model,
                    new[] { clip },
                    out var maximumRightArmStretchRatio);
            if (remainingRightArmComponents != 0)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death contains right-arm stretch components before final capture.");
            }

            var destination = Absolute(CapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Ata death capture path."));
            var result = CaptureTwoLoopReview(model, slot, animator, clip, destination);
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, slot),
                "An Ata slot outside Ata_09_Death changed during capture.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed during capture.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Ata death capture changed the saved scene state.");
            }

            var reportDestination = Absolute(ReportPath);
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "Source=" + SourcePath,
                "SourceSha256=" + SourceSha256,
                "EmbeddedClip=" + clip.name,
                "DurationSeconds=" + Num(clip.length),
                "StateSpeed=1",
                "Loop=True",
                "RootMotion=False",
                "ReviewedNormalizedTimes=0,0.25,0.5,0.75,1,1.25,1.5,1.75",
                "Views=FrontThreeQuarter,Side",
                "Samples=16",
                "MaximumLoopPairPositionError=" + Num(result.MaximumLoopPairPositionError),
                "MaximumLoopPairRotationError=" + Num(result.MaximumLoopPairRotationError),
                "MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError),
                "MaximumModelRootPositionError=" + Num(result.MaximumModelRootPositionError),
                "RemainingRightArmStretchComponents=" + remainingRightArmComponents,
                "MaximumRightArmStretchRatio=" + Num(maximumRightArmStretchRatio),
                "OtherAtaSlotsChanged=False",
                "SceneChanged=False",
                "Capture=" + CapturePath
            });
            Debug.Log(
                "AtaDeathAnimationCaptured Result=PASS" +
                ", Path=" + CapturePath +
                ", Duration=" + Num(clip.length) +
                ", Samples=16" +
                ", Views=FrontThreeQuarter,Side" +
                ", ReviewedLoops=2" +
                ", MaximumLoopPairPositionError=" + Num(result.MaximumLoopPairPositionError) +
                ", MaximumLoopPairRotationError=" + Num(result.MaximumLoopPairRotationError) +
                ", RemainingRightArmStretchComponents=" + remainingRightArmComponents +
                ", MaximumRightArmStretchRatio=" + Num(maximumRightArmStretchRatio) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Death Pre-Fall Static Arms")]
        public static void ApplyAtaDeathPreFallStaticArms()
        {
            var scene = RequireActiveScene();
            var placement = RequirePlacement(scene);
            var staticSlot = RequireDirectChild(placement.transform, "Ata_01_Static");
            var staticModel = RequireDirectChild(staticSlot, ModelName);
            var deathSlot = RequireDirectChild(placement.transform, SlotName);
            var deathModel = RequireDirectChild(deathSlot, ModelName);
            var animator = deathModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death Animator is missing.");
            var sourceClip = RequireMixamoClip();
            var sourceController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_09_Death controller is missing.");
            var currentClip = sourceController.layers[0].stateMachine.defaultState?.motion
                as AnimationClip ??
                              throw new InvalidOperationException(
                                  "Ata_09_Death current animation clip is missing.");
            RequireAppliedState(deathModel, animator, currentClip, sourceController);

            var deathSlotBefore = new TransformSnapshot(deathSlot);
            var deathModelBefore = new TransformSnapshot(deathModel);
            var staticSlotBefore = new TransformSnapshot(staticSlot);
            var staticModelBefore = new TransformSnapshot(staticModel);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, deathSlot);
            var renderer = deathModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death must contain one skinned renderer.");
            var meshBefore = renderer.sharedMesh;
            var staticArmPose = CreateStaticArmPose(staticModel);
            RequireMatchingArmHierarchy(deathModel, staticArmPose);
            var fallBoundary = FindFallBoundary(deathModel, animator, sourceClip);
            var postFallFrames = SampleArmFrames(
                deathModel,
                animator,
                sourceClip,
                staticArmPose.Keys,
                fallBoundary.OnsetFrame);
            var clip = CreatePreFallStaticArmsClip(
                sourceClip,
                staticArmPose,
                postFallFrames,
                fallBoundary,
                out var removedArmCurves,
                out var bakedArmCurves,
                out var preservedNonArmCurves);
            var controller = ConfigureControllerMotion(clip);
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);
            RequireAppliedState(deathModel, animator, clip, controller);

            var staticResult = MeasureStaticArmsBeforeFall(
                deathModel,
                animator,
                clip,
                staticArmPose,
                fallBoundary);
            var postFallResult = MeasurePostFallArmMatch(
                deathModel,
                animator,
                sourceClip,
                clip,
                staticArmPose.Keys,
                fallBoundary);
            var sourceRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    deathModel,
                    new[] { sourceClip },
                    out var maximumRightArmStretchRatio);
            if (renderer.sharedMesh != meshBefore ||
                sourceRightArmComponents != 0 ||
                !deathSlotBefore.Matches() || !deathModelBefore.Matches() ||
                !staticSlotBefore.Matches() || !staticModelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata death pre-fall static-arm apply changed a model transform or mesh reference.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, deathSlot),
                "An Ata slot outside Ata_09_Death changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            AssetDatabase.SaveAssets();
            if (scene.isDirty)
            {
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying Ata death pre-fall static arms.");
                }
            }

            Debug.Log(
                "AtaDeathPreFallStaticArmsApplied Result=PASS" +
                ", SourceClip=" + sourceClip.name +
                ", AppliedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", FallOnsetFrame=" + fallBoundary.OnsetFrame +
                ", FallOnsetSeconds=" + Num(fallBoundary.OnsetSeconds) +
                ", StaticHoldThroughFrame=" + fallBoundary.HoldThroughFrame +
                ", StaticHoldThroughSeconds=" + Num(fallBoundary.HoldThroughSeconds) +
                ", PelvisDropAtOnset=" + Num(fallBoundary.PelvisDropAtOnset) +
                ", TorsoTiltAtOnset=" + Num(fallBoundary.TorsoTiltAtOnset) +
                ", TorsoBone=" + fallBoundary.TorsoBoneName +
                ", StaticArmBones=" + staticArmPose.Count +
                ", RemovedArmCurves=" + removedArmCurves +
                ", BakedArmCurves=" + bakedArmCurves +
                ", PreservedNonArmCurves=" + preservedNonArmCurves +
                ", MaximumPreFallStaticPositionError=" +
                Num(staticResult.MaximumPositionError) +
                ", MaximumPreFallStaticRotationError=" +
                Num(staticResult.MaximumRotationError) +
                ", MaximumPostFallSourcePositionError=" +
                Num(postFallResult.MaximumPositionError) +
                ", MaximumPostFallSourceRotationError=" +
                Num(postFallResult.MaximumRotationError) +
                ", SourceRightArmStretchComponents=" +
                sourceRightArmComponents +
                ", SourceMaximumRightArmStretchRatio=" +
                Num(maximumRightArmStretchRatio) +
                ", DerivedRightArmBoundaryReviewed=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Death Pre-Fall Static Arms")]
        public static void CaptureAtaDeathPreFallStaticArms()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var staticSlot = RequireDirectChild(placement.transform, "Ata_01_Static");
            var staticModel = RequireDirectChild(staticSlot, ModelName);
            var deathSlot = RequireDirectChild(placement.transform, SlotName);
            var deathModel = RequireDirectChild(deathSlot, ModelName);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, deathSlot);
            var animator = deathModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death Animator is missing.");
            var sourceClip = RequireMixamoClip();
            var clip = RequirePreFallStaticArmsClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_09_Death controller is missing.");
            RequireAppliedState(deathModel, animator, clip, controller);
            var staticArmPose = CreateStaticArmPose(staticModel);
            RequireMatchingArmHierarchy(deathModel, staticArmPose);
            var fallBoundary = FindFallBoundary(deathModel, animator, sourceClip);
            var staticResult = MeasureStaticArmsBeforeFall(
                deathModel,
                animator,
                clip,
                staticArmPose,
                fallBoundary);
            var postFallResult = MeasurePostFallArmMatch(
                deathModel,
                animator,
                sourceClip,
                clip,
                staticArmPose.Keys,
                fallBoundary);
            var renderer = deathModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death must contain one skinned renderer.");
            var meshBefore = renderer.sharedMesh;
            var sourceRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    deathModel,
                    new[] { sourceClip },
                    out var maximumRightArmStretchRatio);
            if (sourceRightArmComponents != 0)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death source clip contains right-arm stretch components.");
            }

            var destination = Absolute(PreFallStaticArmsCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Ata death pre-fall static-arm capture path."));
            var phaseTimes = new[]
            {
                0f,
                fallBoundary.HoldThroughSeconds / clip.length,
                fallBoundary.OnsetSeconds / clip.length,
                0.75f
            };
            var result = CaptureTwoLoopReview(
                deathModel,
                deathSlot,
                animator,
                clip,
                destination,
                phaseTimes);
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, deathSlot),
                "An Ata slot outside Ata_09_Death changed during capture.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed during capture.");
            if (scene.isDirty || renderer.sharedMesh != meshBefore)
            {
                throw new InvalidOperationException(
                    "Ata death pre-fall static-arm capture changed the scene or mesh reference.");
            }

            var reportDestination = Absolute(PreFallStaticArmsReportPath);
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "StaticReference=Ata_01_Static/Ata_Model",
                "Source=" + SourcePath,
                "SourceSha256=" + SourceSha256,
                "SourceClip=" + sourceClip.name,
                "AppliedClip=" + clip.name,
                "DurationSeconds=" + Num(clip.length),
                "StateSpeed=1",
                "Loop=True",
                "RootMotion=False",
                "FallOnsetFrame=" + fallBoundary.OnsetFrame,
                "FallOnsetSeconds=" + Num(fallBoundary.OnsetSeconds),
                "StaticHoldThroughFrame=" + fallBoundary.HoldThroughFrame,
                "StaticHoldThroughSeconds=" + Num(fallBoundary.HoldThroughSeconds),
                "PelvisDropAtOnset=" + Num(fallBoundary.PelvisDropAtOnset),
                "TorsoTiltAtOnsetDegrees=" + Num(fallBoundary.TorsoTiltAtOnset),
                "TorsoBone=" + fallBoundary.TorsoBoneName,
                "FallDropHeightFraction=" + Num(FallDropHeightFraction),
                "FallTiltThresholdDegrees=" + Num(FallTiltThresholdDegrees),
                "StaticArmBones=" + staticArmPose.Count,
                "MaximumPreFallStaticPositionError=" +
                Num(staticResult.MaximumPositionError),
                "MaximumPreFallStaticRotationError=" +
                Num(staticResult.MaximumRotationError),
                "MaximumPreFallStaticScaleError=" +
                Num(staticResult.MaximumScaleError),
                "MaximumPostFallSourcePositionError=" +
                Num(postFallResult.MaximumPositionError),
                "MaximumPostFallSourceRotationError=" +
                Num(postFallResult.MaximumRotationError),
                "MaximumPostFallSourceScaleError=" +
                Num(postFallResult.MaximumScaleError),
                "ReviewedNormalizedTimes=" +
                string.Join(",", phaseTimes.Select(Num).Concat(
                    phaseTimes.Select(time => Num(time + 1f)))),
                "Views=FrontThreeQuarter,Side",
                "Samples=16",
                "MaximumLoopPairPositionError=" +
                Num(result.MaximumLoopPairPositionError),
                "MaximumLoopPairRotationError=" +
                Num(result.MaximumLoopPairRotationError),
                "MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError),
                "MaximumModelRootPositionError=" +
                Num(result.MaximumModelRootPositionError),
                "SourceRightArmStretchComponents=" +
                sourceRightArmComponents,
                "SourceMaximumRightArmStretchRatio=" +
                Num(maximumRightArmStretchRatio),
                "DerivedRightArmBoundaryReviewed=True",
                "DeathBodyHeadLowerBodyMotionPreserved=True",
                "OtherAtaSlotsChanged=False",
                "SceneChanged=False",
                "Capture=" + PreFallStaticArmsCapturePath
            });
            Debug.Log(
                "AtaDeathPreFallStaticArmsCaptured Result=PASS" +
                ", Path=" + PreFallStaticArmsCapturePath +
                ", FallOnsetFrame=" + fallBoundary.OnsetFrame +
                ", StaticHoldThroughFrame=" + fallBoundary.HoldThroughFrame +
                ", MaximumPreFallStaticPositionError=" +
                Num(staticResult.MaximumPositionError) +
                ", MaximumPreFallStaticRotationError=" +
                Num(staticResult.MaximumRotationError) +
                ", MaximumPostFallSourcePositionError=" +
                Num(postFallResult.MaximumPositionError) +
                ", MaximumPostFallSourceRotationError=" +
                Num(postFallResult.MaximumRotationError) +
                ", MaximumLoopPairPositionError=" +
                Num(result.MaximumLoopPairPositionError) +
                ", MaximumLoopPairRotationError=" +
                Num(result.MaximumLoopPairRotationError) +
                ", SourceRightArmStretchComponents=" +
                sourceRightArmComponents +
                ", SceneChanged=False.");
        }

        private static void ConfigureMixamoClipLoop()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Ata death FBX importer is unavailable.");
            importer.importAnimation = true;
            var clips = importer.defaultClipAnimations;
            var mixamoIndices = clips
                .Select((clip, index) => (clip, index))
                .Where(item => item.clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.index)
                .ToArray();
            if (mixamoIndices.Length != 1)
            {
                throw new InvalidOperationException(
                    "attas death.fbx must expose exactly one mixamo-named default clip.");
            }

            var selected = clips[mixamoIndices[0]];
            selected.loopTime = true;
            selected.loopPose = false;
            clips[mixamoIndices[0]] = selected;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireMixamoClip()
        {
            var available = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var clips = available
                .Where(clip => clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "attas death.fbx must expose exactly one mixamo-named animation clip. Found=" +
                    clips.Length +
                    ", AvailableClips=" + string.Join(",", available.Select(clip =>
                        clip.name + "[" + Num(clip.length) + "s]")));
            }

            return clips[0];
        }

        private static AnimationClip RequirePreFallStaticArmsClip() =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(PreFallStaticArmsClipPath) ??
            throw new InvalidOperationException(
                "Ata_09_Death pre-fall static-arm clip is missing.");

        private static Dictionary<string, LocalPose> CreateStaticArmPose(
            Transform staticModel)
        {
            var armRoots = new[] { "LeftShoulder", "RightShoulder" }
                .Select(name => staticModel.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == name) ??
                                throw new InvalidOperationException(
                                    "Ata_01_Static arm root is missing: " + name))
                .ToArray();
            var result = armRoots
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Distinct()
                .ToDictionary(
                    item => AnimationUtility.CalculateTransformPath(item, staticModel),
                    item => new LocalPose(item),
                    StringComparer.Ordinal);
            if (result.Count == 0)
            {
                throw new InvalidOperationException(
                    "Ata_01_Static arm pose contains no bones.");
            }

            return result;
        }

        private static void RequireMatchingArmHierarchy(
            Transform deathModel,
            IReadOnlyDictionary<string, LocalPose> staticArmPose)
        {
            var deathPaths = new[] { "LeftShoulder", "RightShoulder" }
                .Select(name => deathModel.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == name) ??
                                throw new InvalidOperationException(
                                    "Ata_09_Death arm root is missing: " + name))
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => AnimationUtility.CalculateTransformPath(item, deathModel))
                .ToHashSet(StringComparer.Ordinal);
            if (!deathPaths.SetEquals(staticArmPose.Keys))
            {
                throw new InvalidOperationException(
                    "Ata_01_Static and Ata_09_Death arm hierarchies differ.");
            }
        }

        private static FallBoundary FindFallBoundary(
            Transform deathModel,
            Animator animator,
            AnimationClip sourceClip)
        {
            var hips = deathModel.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "Hips") ??
                       throw new InvalidOperationException(
                           "Ata_09_Death Hips is missing.");
            var torsoCandidates = new[]
            {
                "Spine02", "Spine2", "Spine01", "Spine1", "Spine"
            };
            var allTransforms = deathModel.GetComponentsInChildren<Transform>(true);
            var upperTorso = torsoCandidates
                .Select(name => allTransforms.SingleOrDefault(item => item.name == name))
                .FirstOrDefault(item => item != null) ??
                             throw new InvalidOperationException(
                                 "Ata_09_Death upper torso bone is missing.");
            var renderer = deathModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death must contain one skinned renderer.");
            var snapshots = deathModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            var frameRate = sourceClip.frameRate;
            if (frameRate <= 0f)
            {
                throw new InvalidOperationException(
                    "Ata death source clip frame rate is invalid.");
            }

            try
            {
                animator.enabled = false;
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                sourceClip.SampleAnimation(deathModel.gameObject, 0f);
                var initialHipsPosition = hips.position;
                var initialTorsoUp = upperTorso.up;
                var bodyHeight = renderer.bounds.size.y;
                if (bodyHeight <= 0f)
                {
                    throw new InvalidOperationException(
                        "Ata_09_Death body height is invalid.");
                }

                var dropThreshold = bodyHeight * FallDropHeightFraction;
                var totalFrames = Mathf.RoundToInt(sourceClip.length * frameRate);
                for (var frame = 1; frame <= totalFrames; frame++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var time = Mathf.Min(frame / frameRate, sourceClip.length);
                    sourceClip.SampleAnimation(deathModel.gameObject, time);
                    var pelvisDrop = Vector3.Dot(
                        initialHipsPosition - hips.position,
                        deathModel.up);
                    var torsoTilt = Vector3.Angle(initialTorsoUp, upperTorso.up);
                    if (pelvisDrop >= dropThreshold ||
                        torsoTilt >= FallTiltThresholdDegrees)
                    {
                        return new FallBoundary(
                            frame,
                            Mathf.Max(0, frame - 1),
                            time,
                            Mathf.Max(0f, (frame - 1) / frameRate),
                            pelvisDrop,
                            torsoTilt,
                            bodyHeight,
                            frameRate,
                            upperTorso.name);
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = originalAnimatorEnabled;
            }

            throw new InvalidOperationException(
                "Ata death source clip contains no measurable falling onset.");
        }

        private static Dictionary<string, List<TimedLocalPose>> SampleArmFrames(
            Transform deathModel,
            Animator animator,
            AnimationClip sourceClip,
            IEnumerable<string> armPaths,
            int firstFrame)
        {
            var pathSet = armPaths.ToHashSet(StringComparer.Ordinal);
            var transforms = deathModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new
                {
                    Path = AnimationUtility.CalculateTransformPath(item, deathModel),
                    Transform = item
                })
                .Where(item => pathSet.Contains(item.Path))
                .ToDictionary(item => item.Path, item => item.Transform, StringComparer.Ordinal);
            if (transforms.Count != pathSet.Count)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death arm transform lookup is incomplete.");
            }

            var result = pathSet.ToDictionary(
                path => path,
                _ => new List<TimedLocalPose>(),
                StringComparer.Ordinal);
            var snapshots = deathModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                var totalFrames = Mathf.RoundToInt(
                    sourceClip.length * sourceClip.frameRate);
                for (var frame = firstFrame; frame <= totalFrames; frame++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var time = Mathf.Min(frame / sourceClip.frameRate, sourceClip.length);
                    sourceClip.SampleAnimation(deathModel.gameObject, time);
                    foreach (var item in transforms)
                    {
                        result[item.Key].Add(
                            new TimedLocalPose(time, new LocalPose(item.Value)));
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = originalAnimatorEnabled;
            }

            return result;
        }

        private static AnimationClip CreatePreFallStaticArmsClip(
            AnimationClip source,
            IReadOnlyDictionary<string, LocalPose> staticArmPose,
            IReadOnlyDictionary<string, List<TimedLocalPose>> postFallFrames,
            FallBoundary fallBoundary,
            out int removedArmCurves,
            out int bakedArmCurves,
            out int preservedNonArmCurves)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    PreFallStaticArmsClipPath) != null &&
                !AssetDatabase.DeleteAsset(PreFallStaticArmsClipPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_09_Death pre-fall static-arm clip could not be replaced.");
            }

            var clip = UnityEngine.Object.Instantiate(source);
            clip.name = "Ata_09_Death_PreFallStaticArms";
            removedArmCurves = 0;
            preservedNonArmCurves = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type == typeof(Transform) &&
                    staticArmPose.ContainsKey(binding.path))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    removedArmCurves++;
                }
                else
                {
                    preservedNonArmCurves++;
                }
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.type == typeof(Transform) &&
                    staticArmPose.ContainsKey(binding.path))
                {
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
                }
            }

            bakedArmCurves = 0;
            foreach (var item in staticArmPose)
            {
                if (!postFallFrames.TryGetValue(item.Key, out var frames) ||
                    frames.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Ata_09_Death post-fall arm samples are missing: " + item.Key);
                }

                bakedArmCurves += SetBakedVector3Curves(
                    clip,
                    item.Key,
                    "m_LocalPosition",
                    item.Value.LocalPosition,
                    frames,
                    pose => pose.LocalPosition,
                    fallBoundary.HoldThroughSeconds);
                bakedArmCurves += SetBakedQuaternionCurves(
                    clip,
                    item.Key,
                    item.Value.LocalRotation,
                    frames,
                    fallBoundary.HoldThroughSeconds);
                bakedArmCurves += SetBakedVector3Curves(
                    clip,
                    item.Key,
                    "m_LocalScale",
                    item.Value.LocalScale,
                    frames,
                    pose => pose.LocalScale,
                    fallBoundary.HoldThroughSeconds);
            }

            clip.frameRate = source.frameRate;
            AssetDatabase.CreateAsset(clip, PreFallStaticArmsClipPath);
            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime") ??
                       throw new InvalidOperationException(
                           "Ata death pre-fall static-arm loop setting is unavailable.");
            loop.boolValue = true;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            RequireNonArmCurvesPreserved(source, clip, staticArmPose.Keys);
            return clip;
        }

        private static int SetBakedVector3Curves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            Vector3 staticValue,
            IReadOnlyList<TimedLocalPose> frames,
            Func<LocalPose, Vector3> selector,
            float holdThroughSeconds)
        {
            var staticValues = new[] { staticValue.x, staticValue.y, staticValue.z };
            var suffixes = new[] { ".x", ".y", ".z" };
            for (var component = 0; component < 3; component++)
            {
                var componentIndex = component;
                SetBakedCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        propertyPrefix + suffixes[component]),
                    staticValues[component],
                    frames.Select(frame =>
                    {
                        var value = selector(frame.Pose);
                        return new TimedValue(
                            frame.Time,
                            componentIndex == 0 ? value.x :
                            componentIndex == 1 ? value.y : value.z);
                    }),
                    holdThroughSeconds);
            }

            return 3;
        }

        private static int SetBakedQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion staticValue,
            IReadOnlyList<TimedLocalPose> frames,
            float holdThroughSeconds)
        {
            var staticValues = new[]
            {
                staticValue.x, staticValue.y, staticValue.z, staticValue.w
            };
            var suffixes = new[] { ".x", ".y", ".z", ".w" };
            for (var component = 0; component < 4; component++)
            {
                var componentIndex = component;
                SetBakedCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        "m_LocalRotation" + suffixes[component]),
                    staticValues[component],
                    frames.Select(frame =>
                    {
                        var value = frame.Pose.LocalRotation;
                        return new TimedValue(
                            frame.Time,
                            componentIndex == 0 ? value.x :
                            componentIndex == 1 ? value.y :
                            componentIndex == 2 ? value.z : value.w);
                    }),
                    holdThroughSeconds);
            }

            return 4;
        }

        private static void SetBakedCurve(
            AnimationClip clip,
            EditorCurveBinding binding,
            float staticValue,
            IEnumerable<TimedValue> postFallValues,
            float holdThroughSeconds)
        {
            var keys = new List<Keyframe> { new Keyframe(0f, staticValue) };
            if (holdThroughSeconds > 0.000001f)
            {
                keys.Add(new Keyframe(holdThroughSeconds, staticValue));
            }

            keys.AddRange(postFallValues.Select(value =>
                new Keyframe(value.Time, value.Value)));
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
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

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void RequireNonArmCurvesPreserved(
            AnimationClip source,
            AnimationClip derived,
            IEnumerable<string> armPaths)
        {
            var armPathSet = armPaths.ToHashSet(StringComparer.Ordinal);
            var sourceBindings = AnimationUtility.GetCurveBindings(source)
                .Where(binding =>
                    binding.type != typeof(Transform) ||
                    !armPathSet.Contains(binding.path))
                .ToArray();
            var derivedBindings = AnimationUtility.GetCurveBindings(derived)
                .Where(binding =>
                    binding.type != typeof(Transform) ||
                    !armPathSet.Contains(binding.path))
                .ToArray();
            if (sourceBindings.Length != derivedBindings.Length)
            {
                throw new InvalidOperationException(
                    "Ata death pre-fall clip changed non-arm curve count.");
            }

            foreach (var binding in sourceBindings)
            {
                if (!CurvesMatch(
                        AnimationUtility.GetEditorCurve(source, binding),
                        AnimationUtility.GetEditorCurve(derived, binding)))
                {
                    throw new InvalidOperationException(
                        "Ata death pre-fall clip changed a non-arm curve: " +
                        binding.path + "/" + binding.propertyName);
                }
            }
        }

        private static bool CurvesMatch(AnimationCurve left, AnimationCurve right)
        {
            if (left == null || right == null || left.length != right.length ||
                left.preWrapMode != right.preWrapMode ||
                left.postWrapMode != right.postWrapMode)
            {
                return false;
            }

            for (var index = 0; index < left.length; index++)
            {
                var leftKey = left.keys[index];
                var rightKey = right.keys[index];
                if (Mathf.Abs(leftKey.time - rightKey.time) > 0.000001f ||
                    Mathf.Abs(leftKey.value - rightKey.value) > 0.000001f ||
                    Mathf.Abs(leftKey.inTangent - rightKey.inTangent) > 0.000001f ||
                    Mathf.Abs(leftKey.outTangent - rightKey.outTangent) > 0.000001f)
                {
                    return false;
                }
            }

            return true;
        }

        private static ArmPoseResult MeasureStaticArmsBeforeFall(
            Transform deathModel,
            Animator animator,
            AnimationClip clip,
            IReadOnlyDictionary<string, LocalPose> staticArmPose,
            FallBoundary fallBoundary)
        {
            var transforms = ArmTransforms(deathModel, staticArmPose.Keys);
            var snapshots = deathModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            var result = new ArmPoseAccumulator();
            try
            {
                animator.enabled = false;
                foreach (var time in new[]
                         {
                             0f,
                             fallBoundary.HoldThroughSeconds * 0.5f,
                             fallBoundary.HoldThroughSeconds
                         }.Distinct())
                {
                    RestoreAll(snapshots);
                    clip.SampleAnimation(deathModel.gameObject, time);
                    foreach (var reference in staticArmPose)
                    {
                        result.Include(transforms[reference.Key], reference.Value);
                    }
                }
            }
            finally
            {
                RestoreAll(snapshots);
                animator.enabled = originalAnimatorEnabled;
            }

            var measured = result.ToResult();
            if (measured.MaximumPositionError > TransformTolerance ||
                measured.MaximumRotationError > 0.01f ||
                measured.MaximumScaleError > TransformTolerance)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death arms do not match the static model before falling.");
            }

            return measured;
        }

        private static ArmPoseResult MeasurePostFallArmMatch(
            Transform deathModel,
            Animator animator,
            AnimationClip sourceClip,
            AnimationClip derivedClip,
            IEnumerable<string> armPaths,
            FallBoundary fallBoundary)
        {
            var paths = armPaths.ToArray();
            var transforms = ArmTransforms(deathModel, paths);
            var snapshots = deathModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            var result = new ArmPoseAccumulator();
            var finalFrame = Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate);
            var span = Mathf.Max(0, finalFrame - fallBoundary.OnsetFrame);
            var frames = new[]
            {
                fallBoundary.OnsetFrame,
                fallBoundary.OnsetFrame + Mathf.RoundToInt(span * 0.25f),
                fallBoundary.OnsetFrame + Mathf.RoundToInt(span * 0.5f),
                fallBoundary.OnsetFrame + Mathf.RoundToInt(span * 0.75f),
                finalFrame
            }.Distinct().ToArray();
            try
            {
                animator.enabled = false;
                foreach (var frame in frames)
                {
                    var time = Mathf.Min(frame / sourceClip.frameRate, sourceClip.length);
                    RestoreAll(snapshots);
                    sourceClip.SampleAnimation(deathModel.gameObject, time);
                    var sourcePoses = paths.ToDictionary(
                        path => path,
                        path => new LocalPose(transforms[path]),
                        StringComparer.Ordinal);
                    RestoreAll(snapshots);
                    derivedClip.SampleAnimation(deathModel.gameObject, time);
                    foreach (var path in paths)
                    {
                        result.Include(transforms[path], sourcePoses[path]);
                    }
                }
            }
            finally
            {
                RestoreAll(snapshots);
                animator.enabled = originalAnimatorEnabled;
            }

            var measured = result.ToResult();
            if (measured.MaximumPositionError > TransformTolerance ||
                measured.MaximumRotationError > 0.01f ||
                measured.MaximumScaleError > TransformTolerance)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death post-fall arm motion differs from the source clip.");
            }

            return measured;
        }

        private static Dictionary<string, Transform> ArmTransforms(
            Transform deathModel,
            IEnumerable<string> armPaths)
        {
            var pathSet = armPaths.ToHashSet(StringComparer.Ordinal);
            var result = deathModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new
                {
                    Path = AnimationUtility.CalculateTransformPath(item, deathModel),
                    Transform = item
                })
                .Where(item => pathSet.Contains(item.Path))
                .ToDictionary(item => item.Path, item => item.Transform, StringComparer.Ordinal);
            if (result.Count != pathSet.Count)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death arm hierarchy lookup is incomplete.");
            }

            return result;
        }

        private static void RestoreAll(IEnumerable<TransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Restore();
            }
        }

        private static AnimatorController ConfigureControllerMotion(AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_09_Death controller is missing.");
            var states = controller.layers[0].stateMachine.states;
            var state = controller.layers[0].stateMachine.defaultState;
            if (states.Length != 1 || state == null || state.name != StateName ||
                state.transitions.Length != 0)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death controller structure differs.");
            }

            state.motion = clip;
            state.speed = 1f;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_09_Death controller could not be replaced.");
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Animator ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            return animator;
        }

        private static void RequireAppliedState(
            Transform model,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_09_Death Animator configuration differs.");
            }

            var loop = new SerializedObject(clip).FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException(
                    "Ata death Mixamo clip is not configured to loop.");
            }

            var states = controller.layers[0].stateMachine.states;
            var state = controller.layers[0].stateMachine.defaultState;
            if (states.Length != 1 || state == null || state.name != StateName ||
                state.motion != clip || Mathf.Abs(state.speed - 1f) > 0.000001f ||
                state.transitions.Length != 0)
            {
                throw new InvalidOperationException(
                    "Ata death controller does not directly loop the original-speed Mixamo clip.");
            }
        }

        private static CaptureResult CaptureTwoLoopReview(
            Transform model,
            Transform slot,
            Animator animator,
            AnimationClip clip,
            string destination,
            float[] phaseTimes = null)
        {
            var phases = phaseTimes ?? new[]
            {
                0f, 0.25f, 0.5f, 0.75f
            };
            if (phases.Length != 4 ||
                phases.Any(time => time < 0f || time >= 1f))
            {
                throw new InvalidOperationException(
                    "Ata death review requires four normalized phase times in [0,1).");
            }

            var normalizedTimes = phases
                .Concat(phases.Select(time => time + 1f))
                .ToArray();
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var armature = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "Armature") ??
                           throw new InvalidOperationException(
                               "Ata_09_Death Armature is missing.");
            var bones = armature.GetComponentsInChildren<Transform>(true);
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_09_Death must contain one skinned renderer.");
            var meshBefore = renderer.sharedMesh;
            var originalAnimatorEnabled = animator.enabled;
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(item => (item, item.enabled))
                .ToArray();
            var cameraObject = new GameObject("Ata Death Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
            camera.fieldOfView = 27f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            const int width = 420;
            const int height = 560;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var sheet = new Texture2D(width * 4, height * 4, TextureFormat.RGB24, false);
            var slotPosition = slot.position;
            var modelLocalPosition = model.localPosition;
            var maximumSlotPositionError = 0f;
            var maximumModelRootPositionError = 0f;
            var maximumLoopPairPositionError = 0f;
            var maximumLoopPairRotationError = 0f;
            var firstLoopPositions = new Vector3[4][];
            var firstLoopRotations = new Quaternion[4][];
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                animator.enabled = false;
                camera.targetTexture = target;
                for (var viewIndex = 0; viewIndex < 2; viewIndex++)
                for (var index = 0; index < normalizedTimes.Length; index++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var phaseIndex = index % 4;
                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * (normalizedTimes[index] % 1f));
                    maximumSlotPositionError = Mathf.Max(
                        maximumSlotPositionError,
                        Vector3.Distance(slotPosition, slot.position));
                    maximumModelRootPositionError = Mathf.Max(
                        maximumModelRootPositionError,
                        Vector3.Distance(modelLocalPosition, model.localPosition));
                    if (viewIndex == 0 && index < 4)
                    {
                        firstLoopPositions[phaseIndex] =
                            bones.Select(bone => bone.localPosition).ToArray();
                        firstLoopRotations[phaseIndex] =
                            bones.Select(bone => bone.localRotation).ToArray();
                    }
                    else if (viewIndex == 0)
                    {
                        for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                        {
                            maximumLoopPairPositionError = Mathf.Max(
                                maximumLoopPairPositionError,
                                Vector3.Distance(
                                    firstLoopPositions[phaseIndex][boneIndex],
                                    bones[boneIndex].localPosition));
                            maximumLoopPairRotationError = Mathf.Max(
                                maximumLoopPairRotationError,
                                Quaternion.Angle(
                                    firstLoopRotations[phaseIndex][boneIndex],
                                    bones[boneIndex].localRotation));
                        }
                    }

                    FrameModel(camera, model, viewIndex == 0 ? 35f : 90f);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata death review contains Unity magenta shader fallback.");
                    }

                    sheet.SetPixels32(
                        (index % 4) * width,
                        (3 - (viewIndex * 2 + index / 4)) * height,
                        width,
                        height,
                        pixels);
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = originalAnimatorEnabled;
                foreach (var state in rendererStates)
                {
                    if (state.item != null)
                    {
                        state.item.enabled = state.enabled;
                    }
                }

                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (renderer.sharedMesh != meshBefore ||
                maximumSlotPositionError > TransformTolerance ||
                maximumModelRootPositionError > TransformTolerance ||
                maximumLoopPairPositionError > TransformTolerance ||
                maximumLoopPairRotationError > 0.01f)
            {
                throw new InvalidOperationException(
                    "Ata death two-loop review changed the saved mesh/transform state or did not repeat exactly.");
            }

            return new CaptureResult(
                maximumLoopPairPositionError,
                maximumLoopPairRotationError,
                maximumSlotPositionError,
                maximumModelRootPositionError);
        }

        private static void FrameModel(Camera camera, Transform model, float viewAngle)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Ata death review has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            var direction = Quaternion.AngleAxis(viewAngle, model.up) * model.forward;
            var distance = bounds.extents.magnitude /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.04f;
            camera.transform.position = bounds.center + direction.normalized * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                model.up);
        }

        private static Scene RequireCleanScene()
        {
            var scene = RequireActiveScene();

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene before handling Ata death animation.");
            }

            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName) ??
            throw new InvalidOperationException(
                "Approved Ata enemy placement is missing.");

        private static Transform RequireDirectChild(Transform parent, string name) =>
            parent.Cast<Transform>().SingleOrDefault(child => child.name == name) ??
            throw new InvalidOperationException(
                parent.name + "/" + name + " is missing or duplicated.");

        private static string[] OtherSlotSignatures(
            Transform placement,
            Transform targetSlot) =>
            placement.Cast<Transform>()
                .Where(slot => slot != targetSlot)
                .Select(RecursiveSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(string.Join(",", item.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(name => name, StringComparer.Ordinal)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement) =>
            scene.GetRootGameObjects()
                .Where(root => root != placement)
                .Select(root =>
                    root.name + "|" + root.activeSelf + "|" +
                    Vec(root.transform.localPosition) + "|" +
                    Quat(root.transform.localRotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount.ToString(CultureInfo.InvariantCulture))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private static string Num(float value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";

        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," +
            Num(value.z) + "," + Num(value.w) + ")";

        private sealed class ArmPoseAccumulator
        {
            public float MaximumPositionError { get; private set; }
            public float MaximumRotationError { get; private set; }
            public float MaximumScaleError { get; private set; }

            public void Include(Transform current, LocalPose reference)
            {
                MaximumPositionError = Mathf.Max(
                    MaximumPositionError,
                    Vector3.Distance(current.localPosition, reference.LocalPosition));
                MaximumRotationError = Mathf.Max(
                    MaximumRotationError,
                    Quaternion.Angle(current.localRotation, reference.LocalRotation));
                MaximumScaleError = Mathf.Max(
                    MaximumScaleError,
                    Vector3.Distance(current.localScale, reference.LocalScale));
            }

            public ArmPoseResult ToResult() =>
                new ArmPoseResult(
                    MaximumPositionError,
                    MaximumRotationError,
                    MaximumScaleError);
        }

        private readonly struct ArmPoseResult
        {
            public ArmPoseResult(
                float maximumPositionError,
                float maximumRotationError,
                float maximumScaleError)
            {
                MaximumPositionError = maximumPositionError;
                MaximumRotationError = maximumRotationError;
                MaximumScaleError = maximumScaleError;
            }

            public float MaximumPositionError { get; }
            public float MaximumRotationError { get; }
            public float MaximumScaleError { get; }
        }

        private readonly struct FallBoundary
        {
            public FallBoundary(
                int onsetFrame,
                int holdThroughFrame,
                float onsetSeconds,
                float holdThroughSeconds,
                float pelvisDropAtOnset,
                float torsoTiltAtOnset,
                float bodyHeight,
                float frameRate,
                string torsoBoneName)
            {
                OnsetFrame = onsetFrame;
                HoldThroughFrame = holdThroughFrame;
                OnsetSeconds = onsetSeconds;
                HoldThroughSeconds = holdThroughSeconds;
                PelvisDropAtOnset = pelvisDropAtOnset;
                TorsoTiltAtOnset = torsoTiltAtOnset;
                BodyHeight = bodyHeight;
                FrameRate = frameRate;
                TorsoBoneName = torsoBoneName;
            }

            public int OnsetFrame { get; }
            public int HoldThroughFrame { get; }
            public float OnsetSeconds { get; }
            public float HoldThroughSeconds { get; }
            public float PelvisDropAtOnset { get; }
            public float TorsoTiltAtOnset { get; }
            public float BodyHeight { get; }
            public float FrameRate { get; }
            public string TorsoBoneName { get; }
        }

        private readonly struct LocalPose
        {
            public LocalPose(Transform transform)
            {
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
                LocalScale = transform.localScale;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct TimedLocalPose
        {
            public TimedLocalPose(float time, LocalPose pose)
            {
                Time = time;
                Pose = pose;
            }

            public float Time { get; }
            public LocalPose Pose { get; }
        }

        private readonly struct TimedValue
        {
            public TimedValue(float time, float value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }
            public float Value { get; }
        }

        private readonly struct CaptureResult
        {
            public CaptureResult(
                float maximumLoopPairPositionError,
                float maximumLoopPairRotationError,
                float maximumSlotPositionError,
                float maximumModelRootPositionError)
            {
                MaximumLoopPairPositionError = maximumLoopPairPositionError;
                MaximumLoopPairRotationError = maximumLoopPairRotationError;
                MaximumSlotPositionError = maximumSlotPositionError;
                MaximumModelRootPositionError = maximumModelRootPositionError;
            }

            public float MaximumLoopPairPositionError { get; }
            public float MaximumLoopPairRotationError { get; }
            public float MaximumSlotPositionError { get; }
            public float MaximumModelRootPositionError { get; }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public bool Matches() =>
                transform != null &&
                Vector3.Distance(transform.localPosition, localPosition) <=
                TransformTolerance &&
                Quaternion.Angle(transform.localRotation, localRotation) <= 0.01f &&
                Vector3.Distance(transform.localScale, localScale) <=
                TransformTolerance;

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }
    }
}
