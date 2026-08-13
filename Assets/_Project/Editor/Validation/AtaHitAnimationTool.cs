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
    internal static class AtaHitAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_08_Hit";
        private const string ModelName = "Ata_Model";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_Hit.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_08_Hit.controller";
        private const string StaticArmsClipPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_08_Hit_StaticArms.anim";
        private const string CapturePath =
            "docs/validation/ata08_hit_animation_2026-08-13/Ata_08_Hit_TwoLoopReview.png";
        private const string ReportPath =
            "docs/validation/ata08_hit_animation_2026-08-13/Ata_08_Hit_Report.txt";
        private const string StaticArmsCapturePath =
            "docs/validation/ata08_hit_static_arms_2026-08-13/Ata_08_Hit_StaticArms_TwoLoopReview.png";
        private const string StaticArmsReportPath =
            "docs/validation/ata08_hit_static_arms_2026-08-13/Ata_08_Hit_StaticArms_Report.txt";
        private const string StateName = "AtaHit";
        private const float TransformTolerance = 0.0002f;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Hit Animation")]
        public static void ApplyAtaHitAnimation()
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
                    "Ata_08_Hit still contains right-arm stretch components after apply.");
            }

            if (!slotBefore.Matches() || !modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_08_Hit slot or model transform changed while applying the hit clip.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, slot),
                "An Ata slot outside Ata_08_Hit changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            RequireAppliedState(model, animator, clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata hit animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaHitAnimationApplied Result=PASS" +
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
                ", MaximumRightArmStretchRatioAfter=" +
                Num(maximumRightArmStretchRatio) +
                ", SlotTransformFixed=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Hit Animation")]
        public static void CaptureAtaHitAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_08_Hit Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_08_Hit controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var remainingRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    model,
                    new[] { clip },
                    out var maximumRightArmStretchRatio);
            if (remainingRightArmComponents != 0)
            {
                throw new InvalidOperationException(
                    "Ata_08_Hit contains right-arm stretch components before final capture.");
            }

            var destination = Absolute(CapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Ata hit capture path."));
            var result = CaptureTwoLoopReview(model, slot, animator, clip, destination);
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Ata hit capture changed the saved scene state.");
            }

            var reportDestination = Absolute(ReportPath);
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "Source=" + SourcePath,
                "SourceSha256=FF53D3D61B06687034B29F7DDD25706B062D88FAEE99267240572D97671BD4AA",
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
                "AtaHitAnimationCaptured Result=PASS" +
                ", Path=" + CapturePath +
                ", Duration=" + Num(clip.length) +
                ", Samples=16" +
                ", Views=FrontThreeQuarter,Side" +
                ", ReviewedLoops=2" +
                ", MaximumLoopPairPositionError=" +
                Num(result.MaximumLoopPairPositionError) +
                ", MaximumLoopPairRotationError=" +
                Num(result.MaximumLoopPairRotationError) +
                ", RemainingRightArmStretchComponents=" +
                remainingRightArmComponents +
                ", MaximumRightArmStretchRatio=" +
                Num(maximumRightArmStretchRatio) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Hit Static Arms")]
        public static void ApplyAtaHitStaticArms()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var staticSlot = RequireDirectChild(placement.transform, "Ata_01_Static");
            var staticModel = RequireDirectChild(staticSlot, ModelName);
            var hitSlot = RequireDirectChild(placement.transform, SlotName);
            var hitModel = RequireDirectChild(hitSlot, ModelName);
            var animator = hitModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_08_Hit Animator is missing.");
            var sourceClip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_08_Hit controller is missing.");
            RequireAppliedState(hitModel, animator, sourceClip, controller);

            var hitSlotBefore = new TransformSnapshot(hitSlot);
            var hitModelBefore = new TransformSnapshot(hitModel);
            var staticSlotBefore = new TransformSnapshot(staticSlot);
            var staticModelBefore = new TransformSnapshot(staticModel);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, hitSlot);
            var renderer = hitModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_08_Hit must contain one skinned renderer.");
            var correctedMeshBefore = renderer.sharedMesh;
            var staticArmPose = CreateStaticArmPose(staticModel);
            RequireMatchingArmHierarchy(hitModel, staticArmPose);
            var clip = CreateStaticArmsClip(
                sourceClip,
                staticArmPose,
                out var removedArmCurves,
                out var bakedArmCurves,
                out var preservedBodyCurves);
            var state = controller.layers[0].stateMachine.defaultState ??
                        throw new InvalidOperationException(
                            "Ata_08_Hit default state is missing.");
            state.motion = clip;
            state.speed = 1f;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            RequireAppliedState(hitModel, animator, clip, controller);

            var armPoseResult = MeasureStaticArmPose(hitModel, animator, clip, staticArmPose);
            var remainingRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    hitModel,
                    new[] { clip },
                    out var maximumRightArmStretchRatio);
            if (renderer.sharedMesh != correctedMeshBefore ||
                remainingRightArmComponents != 0 ||
                !hitSlotBefore.Matches() || !hitModelBefore.Matches() ||
                !staticSlotBefore.Matches() || !staticModelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata hit static-arm apply changed a model transform or the existing corrected mesh.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, hitSlot),
                "An Ata slot outside Ata_08_Hit changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            AssetDatabase.SaveAssets();

            Debug.Log(
                "AtaHitStaticArmsApplied Result=PASS" +
                ", SourceClip=" + sourceClip.name +
                ", AppliedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", StateSpeed=1" +
                ", Loop=True" +
                ", RootMotion=False" +
                ", StaticArmBones=" + staticArmPose.Count +
                ", RemovedArmCurves=" + removedArmCurves +
                ", BakedArmCurves=" + bakedArmCurves +
                ", PreservedBodyCurves=" + preservedBodyCurves +
                ", MaximumStaticArmPositionError=" +
                Num(armPoseResult.MaximumPositionError) +
                ", MaximumStaticArmRotationError=" +
                Num(armPoseResult.MaximumRotationError) +
                ", ExistingCorrectedMeshPreserved=True" +
                ", RemainingRightArmStretchComponents=" +
                remainingRightArmComponents +
                ", MaximumRightArmStretchRatio=" +
                Num(maximumRightArmStretchRatio) +
                ", OtherAtaSlotsUnchanged=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Hit Static Arms")]
        public static void CaptureAtaHitStaticArms()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var staticSlot = RequireDirectChild(placement.transform, "Ata_01_Static");
            var staticModel = RequireDirectChild(staticSlot, ModelName);
            var hitSlot = RequireDirectChild(placement.transform, SlotName);
            var hitModel = RequireDirectChild(hitSlot, ModelName);
            var animator = hitModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_08_Hit Animator is missing.");
            var clip = RequireStaticArmsClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_08_Hit controller is missing.");
            RequireAppliedState(hitModel, animator, clip, controller);
            var staticArmPose = CreateStaticArmPose(staticModel);
            RequireMatchingArmHierarchy(hitModel, staticArmPose);
            var armPoseResult = MeasureStaticArmPose(
                hitModel,
                animator,
                clip,
                staticArmPose);
            var renderer = hitModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_08_Hit must contain one skinned renderer.");
            var correctedMeshBefore = renderer.sharedMesh;
            var remainingRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    hitModel,
                    new[] { clip },
                    out var maximumRightArmStretchRatio);
            if (remainingRightArmComponents != 0)
            {
                throw new InvalidOperationException(
                    "Ata_08_Hit static-arm clip contains right-arm stretch components.");
            }

            var destination = Absolute(StaticArmsCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Ata hit static-arm capture path."));
            var result = CaptureTwoLoopReview(
                hitModel,
                hitSlot,
                animator,
                clip,
                destination);
            if (scene.isDirty || renderer.sharedMesh != correctedMeshBefore)
            {
                throw new InvalidOperationException(
                    "Ata hit static-arm capture changed the scene or corrected mesh reference.");
            }

            var reportDestination = Absolute(StaticArmsReportPath);
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "StaticReference=Ata_01_Static/Ata_Model",
                "Source=" + SourcePath,
                "AppliedClip=" + clip.name,
                "DurationSeconds=" + Num(clip.length),
                "StateSpeed=1",
                "Loop=True",
                "RootMotion=False",
                "StaticArmBones=" + staticArmPose.Count,
                "StaticArmScope=LeftShoulderAndRightShoulderDescendants",
                "MaximumStaticArmPositionError=" +
                Num(armPoseResult.MaximumPositionError),
                "MaximumStaticArmRotationError=" +
                Num(armPoseResult.MaximumRotationError),
                "MaximumStaticArmScaleError=" +
                Num(armPoseResult.MaximumScaleError),
                "MaximumLoopPairPositionError=" +
                Num(result.MaximumLoopPairPositionError),
                "MaximumLoopPairRotationError=" +
                Num(result.MaximumLoopPairRotationError),
                "MaximumSlotPositionError=" + Num(result.MaximumSlotPositionError),
                "MaximumModelRootPositionError=" +
                Num(result.MaximumModelRootPositionError),
                "RemainingRightArmStretchComponents=" +
                remainingRightArmComponents,
                "MaximumRightArmStretchRatio=" +
                Num(maximumRightArmStretchRatio),
                "ExistingCorrectedMeshPreserved=True",
                "HitBodyHeadLowerBodyMotionPreserved=True",
                "OtherAtaSlotsChanged=False",
                "SceneChanged=False",
                "Capture=" + StaticArmsCapturePath
            });
            Debug.Log(
                "AtaHitStaticArmsCaptured Result=PASS" +
                ", Path=" + StaticArmsCapturePath +
                ", StaticArmBones=" + staticArmPose.Count +
                ", MaximumStaticArmPositionError=" +
                Num(armPoseResult.MaximumPositionError) +
                ", MaximumStaticArmRotationError=" +
                Num(armPoseResult.MaximumRotationError) +
                ", MaximumLoopPairPositionError=" +
                Num(result.MaximumLoopPairPositionError) +
                ", MaximumLoopPairRotationError=" +
                Num(result.MaximumLoopPairRotationError) +
                ", RemainingRightArmStretchComponents=0" +
                ", ExistingCorrectedMeshPreserved=True" +
                ", SceneChanged=False.");
        }

        private static void ConfigureMixamoClipLoop()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Ata hit FBX importer is unavailable.");
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
                    "attas hit.fbx must expose exactly one mixamo-named default clip.");
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
                    "attas hit.fbx must expose exactly one mixamo-named animation clip. Found=" +
                    clips.Length +
                    ", AvailableClips=" + string.Join(",", available.Select(clip =>
                        clip.name + "[" + Num(clip.length) + "s]")));
            }

            return clips[0];
        }

        private static AnimationClip RequireStaticArmsClip() =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(StaticArmsClipPath) ??
            throw new InvalidOperationException(
                "Ata_08_Hit static-arm clip is missing.");

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
            Transform hitModel,
            IReadOnlyDictionary<string, LocalPose> staticArmPose)
        {
            var hitPaths = new[] { "LeftShoulder", "RightShoulder" }
                .Select(name => hitModel.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == name) ??
                                throw new InvalidOperationException(
                                    "Ata_08_Hit arm root is missing: " + name))
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => AnimationUtility.CalculateTransformPath(item, hitModel))
                .ToHashSet(StringComparer.Ordinal);
            if (!hitPaths.SetEquals(staticArmPose.Keys))
            {
                throw new InvalidOperationException(
                    "Ata_01_Static and Ata_08_Hit arm hierarchies differ.");
            }
        }

        private static AnimationClip CreateStaticArmsClip(
            AnimationClip source,
            IReadOnlyDictionary<string, LocalPose> staticArmPose,
            out int removedArmCurves,
            out int bakedArmCurves,
            out int preservedBodyCurves)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(StaticArmsClipPath) != null &&
                !AssetDatabase.DeleteAsset(StaticArmsClipPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_08_Hit static-arm clip could not be replaced.");
            }

            var clip = UnityEngine.Object.Instantiate(source);
            clip.name = "Ata_08_Hit_StaticArms";
            removedArmCurves = 0;
            preservedBodyCurves = 0;
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
                    preservedBodyCurves++;
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
                bakedArmCurves += SetConstantVector3Curves(
                    clip,
                    item.Key,
                    "m_LocalPosition",
                    item.Value.LocalPosition,
                    source.length);
                bakedArmCurves += SetConstantQuaternionCurves(
                    clip,
                    item.Key,
                    item.Value.LocalRotation,
                    source.length);
                bakedArmCurves += SetConstantVector3Curves(
                    clip,
                    item.Key,
                    "m_LocalScale",
                    item.Value.LocalScale,
                    source.length);
            }

            AssetDatabase.CreateAsset(clip, StaticArmsClipPath);
            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime") ??
                       throw new InvalidOperationException(
                           "Ata hit static-arm loop setting is unavailable.");
            loop.boolValue = true;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            RequireBodyCurvesPreserved(source, clip, staticArmPose.Keys);
            return clip;
        }

        private static int SetConstantVector3Curves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            Vector3 value,
            float duration)
        {
            var values = new[] { value.x, value.y, value.z };
            var suffixes = new[] { ".x", ".y", ".z" };
            for (var index = 0; index < values.Length; index++)
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        propertyPrefix + suffixes[index]),
                    AnimationCurve.Constant(0f, duration, values[index]));
            }

            return 3;
        }

        private static int SetConstantQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion value,
            float duration)
        {
            var values = new[] { value.x, value.y, value.z, value.w };
            var suffixes = new[] { ".x", ".y", ".z", ".w" };
            for (var index = 0; index < values.Length; index++)
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        "m_LocalRotation" + suffixes[index]),
                    AnimationCurve.Constant(0f, duration, values[index]));
            }

            return 4;
        }

        private static void RequireBodyCurvesPreserved(
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
                    "Ata hit static-arm clip changed non-arm curve count.");
            }

            foreach (var binding in sourceBindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                var derivedCurve = AnimationUtility.GetEditorCurve(derived, binding);
                if (!CurvesMatch(sourceCurve, derivedCurve))
                {
                    throw new InvalidOperationException(
                        "Ata hit static-arm clip changed a non-arm curve: " +
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

        private static ArmPoseResult MeasureStaticArmPose(
            Transform hitModel,
            Animator animator,
            AnimationClip clip,
            IReadOnlyDictionary<string, LocalPose> staticArmPose)
        {
            var snapshots = hitModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var hitTransforms = hitModel.GetComponentsInChildren<Transform>(true)
                .Where(item => staticArmPose.ContainsKey(
                    AnimationUtility.CalculateTransformPath(item, hitModel)))
                .ToDictionary(
                    item => AnimationUtility.CalculateTransformPath(item, hitModel),
                    item => item,
                    StringComparer.Ordinal);
            var originalAnimatorEnabled = animator.enabled;
            var maximumPositionError = 0f;
            var maximumRotationError = 0f;
            var maximumScaleError = 0f;
            try
            {
                animator.enabled = false;
                foreach (var normalizedTime in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    clip.SampleAnimation(hitModel.gameObject, clip.length * normalizedTime);
                    foreach (var reference in staticArmPose)
                    {
                        var current = hitTransforms[reference.Key];
                        maximumPositionError = Mathf.Max(
                            maximumPositionError,
                            Vector3.Distance(
                                current.localPosition,
                                reference.Value.LocalPosition));
                        maximumRotationError = Mathf.Max(
                            maximumRotationError,
                            Quaternion.Angle(
                                current.localRotation,
                                reference.Value.LocalRotation));
                        maximumScaleError = Mathf.Max(
                            maximumScaleError,
                            Vector3.Distance(
                                current.localScale,
                                reference.Value.LocalScale));
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

            if (maximumPositionError > TransformTolerance ||
                maximumRotationError > 0.01f ||
                maximumScaleError > TransformTolerance)
            {
                throw new InvalidOperationException(
                    "Ata_08_Hit arms do not match the static model pose.");
            }

            return new ArmPoseResult(
                maximumPositionError,
                maximumRotationError,
                maximumScaleError);
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_08_Hit controller could not be replaced.");
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
                    "Ata_08_Hit contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_08_Hit Animator must be on Ata_Model.");
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
                    "Ata_08_Hit Animator configuration differs.");
            }

            var loop = new SerializedObject(clip).FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException(
                    "Ata hit Mixamo clip is not configured to loop.");
            }

            var states = controller.layers[0].stateMachine.states;
            var state = controller.layers[0].stateMachine.defaultState;
            if (states.Length != 1 || state == null || state.name != StateName ||
                state.motion != clip || Mathf.Abs(state.speed - 1f) > 0.000001f ||
                state.transitions.Length != 0)
            {
                throw new InvalidOperationException(
                    "Ata hit controller does not directly loop the original-speed Mixamo clip.");
            }
        }

        private static CaptureResult CaptureTwoLoopReview(
            Transform model,
            Transform slot,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            var normalizedTimes = new[]
            {
                0f, 0.25f, 0.5f, 0.75f,
                1f, 1.25f, 1.5f, 1.75f
            };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var bones = model.Find("Armature")?.GetComponentsInChildren<Transform>(true) ??
                        throw new InvalidOperationException(
                            "Ata_08_Hit Armature is missing.");
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_08_Hit must contain one skinned renderer.");
            var meshBefore = renderer.sharedMesh;
            var originalAnimatorEnabled = animator.enabled;
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(item => (item, item.enabled))
                .ToArray();
            var cameraObject = new GameObject("Ata Hit Review Camera");
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
                            "Ata hit review contains Unity magenta shader fallback.");
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
                    "Ata hit two-loop review changed the saved mesh/transform state or did not repeat exactly.");
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
                    "Ata hit review has no visible renderer.");
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
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene before handling Ata hit animation.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
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
