using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerWalkForwardAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LayoutRootName = "PlayerAnimationLayout";
        private const string WalkKey = "Player_Walk_Forward";
        private const string EmbeddedSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward_Embedded.fbx";
        private const string SourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward_Standard.fbx";
        private const string ClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward.controller";
        private const string SourceClipName = "Player_Walk_Forward_Mixamo_Source";
        private const float FacingYaw = 180f;
        private const float PositionTolerance = 0.001f;
        private const float MinimumFootTravel = 0.08f;
        private const float PelvisLateralRetention = 0.05f;
        private const float PelvisRollRetention = 0.08f;
        private const float TorsoLeanRetention = 0.1f;
        private const int UprightCorrectionPasses = 2;
        private const float MaximumPelvisLateralTravel = 0.004f;
        private const float MaximumPelvisRollRangeDegrees = 2f;
        private const float MaximumPelvisRollDegrees = 1.25f;
        private const float MaximumTorsoLeanRangeDegrees = 2f;
        private const float MaximumTorsoLeanDegrees = 1.5f;
        private const float MaximumMeanTorsoLeanDegrees = 0.08f;
        private const int TorsoCenteringPasses = 2;
        private const float MinimumLegHalfSpacing = 0.07f;
        private const float MaximumFootLateralRange = 0.012f;
        private const float MaximumKneeLateralRange = 0.012f;
        private const float MinimumFootCenterClearance = 0.055f;
        private const float MinimumKneeCenterClearance = 0.035f;
        private const int CaptureWidth = 3840;
        private const int CaptureHeight = 2160;
        private static readonly float[] ReviewPhases = { 0f, 0.25f, 0.5f, 0.75f };

        public static void InspectEmbeddedSourceClips()
        {
            var importer = AssetImporter.GetAtPath(EmbeddedSourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Player embedded animation source is not imported.");
            var takes = importer.defaultClipAnimations ??
                        Array.Empty<ModelImporterClipAnimation>();
            var clips = AssetDatabase.LoadAllAssetsAtPath(EmbeddedSourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();
            var nonMixamoClips = clips
                .Where(clip => clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) < 0)
                .ToArray();

            Debug.Log(
                "Player embedded source clips inspected." +
                " Source=" + EmbeddedSourcePath +
                ", ImportAnimation=" + importer.importAnimation +
                ", DefaultTakeCount=" + takes.Length.ToString(
                    CultureInfo.InvariantCulture) +
                ", DefaultTakes=" + string.Join(
                    " | ",
                    takes.Select(take =>
                        take.name +
                        "[Take=" + take.takeName +
                        ", First=" + Num(take.firstFrame) +
                        ", Last=" + Num(take.lastFrame) + "]")) +
                ", ImportedClipCount=" + clips.Length.ToString(
                    CultureInfo.InvariantCulture) +
                ", ImportedClips=" + string.Join(
                    " | ",
                    clips.Select(clip =>
                        clip.name +
                        "[Duration=" + Num(clip.length) +
                        ", FrameRate=" + Num(clip.frameRate) + "]")) +
                ", NonMixamoClipCount=" + nonMixamoClips.Length.ToString(
                    CultureInfo.InvariantCulture) + ".");
        }

        [MenuItem("Bellerophon/Player/Apply Walk Forward Animation")]
        public static void Apply()
        {
            var scene = RequireScene();
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var walkInstance = RequireDirectChild(layoutRoot, WalkKey);
            var rootBefore = new TransformState(walkInstance);
            var otherAnimationStates = OtherAnimationStates(layoutRoot, walkInstance);

            var sourceClip = ConfigureAndLoadSourceClip();
            var clip = CreateInPlaceClip(
                sourceClip,
                walkInstance,
                out var removedPlanarTravel,
                out var uprightCorrection);
            var controller = CreateController(clip);
            ConfigureAnimator(walkInstance, controller);

            if (!rootBefore.Matches(walkInstance))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward root transform changed while applying animation.");
            }

            RequireEqual(
                otherAnimationStates,
                OtherAnimationStates(layoutRoot, walkInstance),
                "A player instance outside Player_Walk_Forward changed animation state.");
            var metrics = Inspect(clip, controller);

            EditorUtility.SetDirty(walkInstance.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            }

            Debug.Log(
                "PlayerWalkForwardAnimation applied." +
                " SourceClip=" + sourceClip.name +
                ", Duration=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", RemovedPlanarTravel=" + Num(removedPlanarTravel) +
                ", PelvisLateralBefore=" +
                Num(uprightCorrection.Before.PelvisLateralTravel) +
                ", PelvisLateralAfter=" +
                Num(uprightCorrection.After.PelvisLateralTravel) +
                ", PelvisRollRangeBefore=" +
                Num(uprightCorrection.Before.PelvisRollRangeDegrees) +
                ", PelvisRollRangeAfter=" +
                Num(uprightCorrection.After.PelvisRollRangeDegrees) +
                ", MaximumPelvisRollAfter=" +
                Num(uprightCorrection.After.MaximumPelvisRollDegrees) +
                ", TorsoLeanRangeBefore=" +
                Num(uprightCorrection.Before.TorsoLeanRangeDegrees) +
                ", TorsoLeanRangeAfter=" +
                Num(uprightCorrection.After.TorsoLeanRangeDegrees) +
                ", MaximumTorsoLeanAfter=" +
                Num(uprightCorrection.After.MaximumTorsoLeanDegrees) +
                ", MeanTorsoLeanBefore=" +
                Num(uprightCorrection.Before.MeanTorsoLeanDegrees) +
                ", MeanTorsoLeanAfter=" +
                Num(uprightCorrection.After.MeanTorsoLeanDegrees) +
                ", LeftFootLateralBefore=" +
                Num(uprightCorrection.LegsBefore.LeftFootLateralRange) +
                ", LeftFootLateralAfter=" +
                Num(uprightCorrection.LegsAfter.LeftFootLateralRange) +
                ", RightFootLateralBefore=" +
                Num(uprightCorrection.LegsBefore.RightFootLateralRange) +
                ", RightFootLateralAfter=" +
                Num(uprightCorrection.LegsAfter.RightFootLateralRange) +
                ", MinimumFootClearanceBefore=" +
                Num(uprightCorrection.LegsBefore.MinimumFootCenterClearance) +
                ", MinimumFootClearanceAfter=" +
                Num(uprightCorrection.LegsAfter.MinimumFootCenterClearance) +
                ", MinimumKneeClearanceBefore=" +
                Num(uprightCorrection.LegsBefore.MinimumKneeCenterClearance) +
                ", MinimumKneeClearanceAfter=" +
                Num(uprightCorrection.LegsAfter.MinimumKneeCenterClearance) +
                ", RemainingPlanarDrift=" + Num(metrics.RemainingPlanarDrift) +
                ", RootPositionError=" + Num(metrics.RootPositionError) +
                ", LeftFootTravel=" + Num(metrics.LeftFootTravel) +
                ", RightFootTravel=" + Num(metrics.RightFootTravel) +
                ", CurveBindings=" + metrics.CurveBindingCount.ToString(
                    CultureInfo.InvariantCulture) +
                ", AnimatedInstance=" + WalkKey +
                ", Loop=True" +
                ", ApplyRootMotion=False" +
                ", OtherInstancesUnchanged=True" +
                ", SceneSaved=True.");
        }

        public static void Capture(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(
                    "CapturePlayerWalkForwardAnimation requires an output path.",
                    nameof(outputPath));
            }

            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException(
                           "Player_Walk_Forward clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                                 ControllerPath) ??
                             throw new InvalidOperationException(
                                 "Player_Walk_Forward controller is missing.");
            var metrics = Inspect(clip, controller);
            var destination = Absolute(outputPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "The walk capture path has no directory."));

            CapturePhaseStrip(clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward capture changed the scene dirty state.");
            }

            Debug.Log(
                "PlayerWalkForwardAnimation captured." +
                " Output=" + destination +
                ", ReviewPhases=0,0.25,0.5,0.75" +
                ", Duration=" + Num(clip.length) +
                ", PelvisLateralTravel=" + Num(metrics.PelvisLateralTravel) +
                ", PelvisRollRange=" + Num(metrics.PelvisRollRangeDegrees) +
                ", MaximumPelvisRoll=" + Num(metrics.MaximumPelvisRollDegrees) +
                ", TorsoLeanRange=" + Num(metrics.TorsoLeanRangeDegrees) +
                ", MaximumTorsoLean=" + Num(metrics.MaximumTorsoLeanDegrees) +
                ", MeanTorsoLean=" + Num(metrics.MeanTorsoLeanDegrees) +
                ", RemainingPlanarDrift=" + Num(metrics.RemainingPlanarDrift) +
                ", RootPositionError=" + Num(metrics.RootPositionError) +
                ", LeftFootTravel=" + Num(metrics.LeftFootTravel) +
                ", RightFootTravel=" + Num(metrics.RightFootTravel) +
                ", Loop=True" +
                ", InPlace=True" +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        private static AnimationClip ConfigureAndLoadSourceClip()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Player_Walk_Forward source FBX is not imported.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward source must contain exactly one take. Count=" +
                    (clips?.Length ?? 0).ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (clips[0].takeName.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward source take is not Mixamo. Take=" +
                    clips[0].takeName + ".");
            }

            clips[0].name = SourceClipName;
            clips[0].loopTime = true;
            clips[0].loopPose = true;
            clips[0].lockRootRotation = true;
            clips[0].lockRootHeightY = true;
            clips[0].lockRootPositionXZ = true;
            clips[0].keepOriginalOrientation = true;
            clips[0].keepOriginalPositionY = true;
            clips[0].keepOriginalPositionXZ = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var importedClips = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (importedClips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward imported clip count differs. Count=" +
                    importedClips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return importedClips[0];
        }

        private static AnimationClip CreateInPlaceClip(
            AnimationClip sourceClip,
            Transform animationRoot,
            out float removedPlanarTravel,
            out UprightCorrectionMetrics uprightCorrection)
        {
            DeleteAssetIfPresent(
                ClipPath,
                "Existing Player_Walk_Forward clip could not be replaced.");
            var clip = UnityEngine.Object.Instantiate(sourceClip);
            clip.name = WalkKey;
            clip.legacy = false;
            clip.wrapMode = WrapMode.Loop;

            removedPlanarTravel = RemoveAccumulatedPlanarRootTravel(
                clip,
                animationRoot);
            var swayBefore = MeasureSway(clip, animationRoot);
            var legsBefore = MeasureLegAlignment(clip, animationRoot);
            StabilizeUprightMotion(clip, animationRoot);
            for (var pass = 0; pass < TorsoCenteringPasses; pass++)
            {
                CenterUpperBodyMean(clip, animationRoot);
            }

            StabilizeLegTracking(clip, animationRoot);
            var swayAfter = MeasureSway(clip, animationRoot);
            var legsAfter = MeasureLegAlignment(clip, animationRoot);
            uprightCorrection = new UprightCorrectionMetrics(
                swayBefore,
                swayAfter,
                legsBefore,
                legsAfter);
            if (swayAfter.PelvisLateralTravel > MaximumPelvisLateralTravel ||
                swayAfter.PelvisRollRangeDegrees > MaximumPelvisRollRangeDegrees ||
                swayAfter.MaximumPelvisRollDegrees > MaximumPelvisRollDegrees ||
                swayAfter.TorsoLeanRangeDegrees > MaximumTorsoLeanRangeDegrees ||
                swayAfter.MaximumTorsoLeanDegrees > MaximumTorsoLeanDegrees ||
                Mathf.Abs(swayAfter.MeanTorsoLeanDegrees) >
                    MaximumMeanTorsoLeanDegrees)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward upright correction remains off center." +
                    " PelvisBefore=" + Num(swayBefore.PelvisLateralTravel) +
                    ", PelvisAfter=" + Num(swayAfter.PelvisLateralTravel) +
                    ", PelvisRollBefore=" + Num(swayBefore.PelvisRollRangeDegrees) +
                    ", PelvisRollAfter=" + Num(swayAfter.PelvisRollRangeDegrees) +
                    ", TorsoBefore=" + Num(swayBefore.TorsoLeanRangeDegrees) +
                    ", TorsoAfter=" + Num(swayAfter.TorsoLeanRangeDegrees) +
                    ", MeanTorsoBefore=" +
                    Num(swayBefore.MeanTorsoLeanDegrees) +
                    ", MeanTorsoAfter=" +
                    Num(swayAfter.MeanTorsoLeanDegrees) + ".");
            }

            ValidateLegAlignment(legsAfter);

            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                ClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException(
                       "Player_Walk_Forward clip was not reloaded after saving.");
        }

        private static float RemoveAccumulatedPlanarRootTravel(
            AnimationClip clip,
            Transform animationRoot)
        {
            var hips = RequireNamedBone(animationRoot, "Hips");
            if (hips.parent == null)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward Hips has no parent transform.");
            }

            var hipsPath = AnimationUtility.CalculateTransformPath(
                hips,
                animationRoot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var hipsPositionBindings = bindings
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    binding.path.Equals(hipsPath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith(
                        "m_LocalPosition.",
                        StringComparison.Ordinal))
                .ToDictionary(
                    binding => binding.propertyName,
                    binding => binding,
                    StringComparer.Ordinal);
            var localDelta = new Vector3(
                CurveDelta(clip, hipsPositionBindings, "m_LocalPosition.x"),
                CurveDelta(clip, hipsPositionBindings, "m_LocalPosition.y"),
                CurveDelta(clip, hipsPositionBindings, "m_LocalPosition.z"));
            var worldDelta = hips.parent.TransformVector(localDelta);
            var planarWorldDelta = Vector3.ProjectOnPlane(worldDelta, Vector3.up);
            var planarLocalDelta = hips.parent.InverseTransformVector(planarWorldDelta);
            DetrendCurve(
                clip,
                hipsPositionBindings,
                "m_LocalPosition.x",
                planarLocalDelta.x);
            DetrendCurve(
                clip,
                hipsPositionBindings,
                "m_LocalPosition.y",
                planarLocalDelta.y);
            DetrendCurve(
                clip,
                hipsPositionBindings,
                "m_LocalPosition.z",
                planarLocalDelta.z);

            foreach (var binding in bindings.Where(IsAnimatorPlanarRootBinding))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length < 2)
                {
                    continue;
                }

                var delta = curve.keys[curve.length - 1].value -
                            curve.keys[0].value;
                DetrendCurve(clip, binding, delta);
            }

            return planarWorldDelta.magnitude;
        }

        private static float CurveDelta(
            AnimationClip clip,
            IReadOnlyDictionary<string, EditorCurveBinding> bindings,
            string propertyName)
        {
            if (!bindings.TryGetValue(propertyName, out var binding))
            {
                return 0f;
            }

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve == null || curve.length < 2
                ? 0f
                : curve.keys[curve.length - 1].value - curve.keys[0].value;
        }

        private static void DetrendCurve(
            AnimationClip clip,
            IReadOnlyDictionary<string, EditorCurveBinding> bindings,
            string propertyName,
            float delta)
        {
            if (!bindings.TryGetValue(propertyName, out var binding))
            {
                if (Mathf.Abs(delta) > 0.000001f)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Forward is missing " + propertyName + ".");
                }

                return;
            }

            DetrendCurve(clip, binding, delta);
        }

        private static void DetrendCurve(
            AnimationClip clip,
            EditorCurveBinding binding,
            float delta)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.length < 2)
            {
                return;
            }

            var startTime = curve.keys[0].time;
            var endTime = curve.keys[curve.length - 1].time;
            var duration = endTime - startTime;
            if (duration <= 0.000001f || Mathf.Abs(delta) <= 0.000001f)
            {
                return;
            }

            var slope = delta / duration;
            var keys = curve.keys;
            for (var index = 0; index < keys.Length; index++)
            {
                var key = keys[index];
                key.value -= delta * ((key.time - startTime) / duration);
                if (!float.IsInfinity(key.inTangent) && !float.IsNaN(key.inTangent))
                {
                    key.inTangent -= slope;
                }

                if (!float.IsInfinity(key.outTangent) && !float.IsNaN(key.outTangent))
                {
                    key.outTangent -= slope;
                }

                keys[index] = key;
            }

            curve.keys = keys;
            curve.preWrapMode = WrapMode.Loop;
            curve.postWrapMode = WrapMode.Loop;
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static bool IsAnimatorPlanarRootBinding(EditorCurveBinding binding)
        {
            if (!binding.propertyName.EndsWith(".x", StringComparison.Ordinal) &&
                !binding.propertyName.EndsWith(".z", StringComparison.Ordinal))
            {
                return false;
            }

            return binding.type == typeof(Animator) &&
                   (binding.propertyName.StartsWith("RootT.", StringComparison.Ordinal) ||
                    binding.propertyName.StartsWith("MotionT.", StringComparison.Ordinal));
        }

        private static void StabilizeUprightMotion(
            AnimationClip clip,
            Transform animationRoot)
        {
            var sample = UnityEngine.Object.Instantiate(animationRoot.gameObject);
            sample.name = "PlayerWalkForward_UprightCorrection";
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var hips = RequireNamedBone(sample.transform, "Hips");
                var leftUpLeg = RequireNamedBone(sample.transform, "LeftUpLeg");
                var rightUpLeg = RequireNamedBone(sample.transform, "RightUpLeg");
                var spine = RequireNamedBone(sample.transform, "Spine02");
                var head = RequireNamedBone(sample.transform, "Head");
                if (hips.parent == null || spine.parent == null)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Forward upright correction needs parented Hips and Spine02.");
                }

                var frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
                var times = Enumerable.Range(0, frameCount + 1)
                    .Select(index => Mathf.Min(index / clip.frameRate, clip.length))
                    .ToArray();
                var lateralPositions = new float[times.Length];
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(sample, times[index]);
                    lateralPositions[index] = Vector3.Dot(
                        hips.position - sample.transform.position,
                        sample.transform.right);
                }

                var meanLateral = lateralPositions.Average();
                var hipsPositions = new Vector3[times.Length];
                var hipsRotations = new Quaternion[times.Length];
                var spineRotations = new Quaternion[times.Length];
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(sample, times[index]);
                    var lateralOffset = lateralPositions[index] - meanLateral;
                    var desiredHipsWorldPosition = hips.position -
                                                   sample.transform.right *
                                                   lateralOffset *
                                                   (1f - PelvisLateralRetention);
                    hips.position = desiredHipsWorldPosition;

                    for (var pass = 0; pass < UprightCorrectionPasses; pass++)
                    {
                        var pelvisRoll = PelvisRollDegrees(
                            leftUpLeg,
                            rightUpLeg,
                            sample.transform);
                        hips.rotation = Quaternion.AngleAxis(
                                            -pelvisRoll * (1f - PelvisRollRetention),
                                            sample.transform.forward) *
                                        hips.rotation;
                    }

                    for (var pass = 0; pass < UprightCorrectionPasses; pass++)
                    {
                        var centerLine = head.position - hips.position;
                        var vertical = Vector3.Dot(centerLine, sample.transform.up);
                        if (vertical <= 0.0001f)
                        {
                            throw new InvalidOperationException(
                                "Player_Walk_Forward torso center line is invalid.");
                        }

                        var lateral = Vector3.Dot(centerLine, sample.transform.right);
                        var leanDegrees = Mathf.Atan2(lateral, vertical) * Mathf.Rad2Deg;
                        spine.rotation = Quaternion.AngleAxis(
                                             leanDegrees * (1f - TorsoLeanRetention),
                                             sample.transform.forward) *
                                         spine.rotation;
                    }

                    hipsPositions[index] = hips.localPosition;
                    var hipsRotation = hips.localRotation;
                    if (index > 0 &&
                        Quaternion.Dot(hipsRotations[index - 1], hipsRotation) < 0f)
                    {
                        hipsRotation = Negate(hipsRotation);
                    }

                    hipsRotations[index] = hipsRotation;
                    var spineRotation = spine.localRotation;
                    if (index > 0 &&
                        Quaternion.Dot(spineRotations[index - 1], spineRotation) < 0f)
                    {
                        spineRotation = Negate(spineRotation);
                    }

                    spineRotations[index] = spineRotation;
                }

                hipsPositions[hipsPositions.Length - 1] = hipsPositions[0];
                hipsRotations[hipsRotations.Length - 1] = hipsRotations[0];
                spineRotations[spineRotations.Length - 1] = spineRotations[0];
                var hipsPath = AnimationUtility.CalculateTransformPath(
                    RequireNamedBone(animationRoot, "Hips"),
                    animationRoot);
                var spinePath = AnimationUtility.CalculateTransformPath(
                    RequireNamedBone(animationRoot, "Spine02"),
                    animationRoot);
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalPosition.x",
                    times,
                    hipsPositions.Select(value => value.x).ToArray());
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalPosition.y",
                    times,
                    hipsPositions.Select(value => value.y).ToArray());
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalPosition.z",
                    times,
                    hipsPositions.Select(value => value.z).ToArray());
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalRotation.x",
                    times,
                    hipsRotations.Select(value => value.x).ToArray());
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalRotation.y",
                    times,
                    hipsRotations.Select(value => value.y).ToArray());
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalRotation.z",
                    times,
                    hipsRotations.Select(value => value.z).ToArray());
                SetSampledCurve(
                    clip,
                    hipsPath,
                    "m_LocalRotation.w",
                    times,
                    hipsRotations.Select(value => value.w).ToArray());
                SetSampledCurve(
                    clip,
                    spinePath,
                    "m_LocalRotation.x",
                    times,
                    spineRotations.Select(value => value.x).ToArray());
                SetSampledCurve(
                    clip,
                    spinePath,
                    "m_LocalRotation.y",
                    times,
                    spineRotations.Select(value => value.y).ToArray());
                SetSampledCurve(
                    clip,
                    spinePath,
                    "m_LocalRotation.z",
                    times,
                    spineRotations.Select(value => value.z).ToArray());
                SetSampledCurve(
                    clip,
                    spinePath,
                    "m_LocalRotation.w",
                    times,
                    spineRotations.Select(value => value.w).ToArray());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static void CenterUpperBodyMean(
            AnimationClip clip,
            Transform animationRoot)
        {
            var sample = UnityEngine.Object.Instantiate(animationRoot.gameObject);
            sample.name = "PlayerWalkForward_TorsoCentering";
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var hips = RequireNamedBone(sample.transform, "Hips");
                var head = RequireNamedBone(sample.transform, "Head");
                var spine = RequireNamedBone(sample.transform, "Spine02");
                var frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
                var times = Enumerable.Range(0, frameCount + 1)
                    .Select(index => Mathf.Min(index / clip.frameRate, clip.length))
                    .ToArray();
                var meanLean = 0f;
                foreach (var time in times)
                {
                    clip.SampleAnimation(sample, time);
                    var centerLine = head.position - hips.position;
                    meanLean += Mathf.Atan2(
                                    Vector3.Dot(centerLine, sample.transform.right),
                                    Vector3.Dot(centerLine, sample.transform.up)) *
                                Mathf.Rad2Deg;
                }

                meanLean /= times.Length;
                if (Mathf.Abs(meanLean) <= 0.002f)
                {
                    return;
                }

                var rotations = new Quaternion[times.Length];
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(sample, times[index]);
                    spine.rotation = Quaternion.AngleAxis(
                                         meanLean,
                                         sample.transform.forward) *
                                     spine.rotation;
                    rotations[index] = Continuous(
                        index > 0 ? rotations[index - 1] : spine.localRotation,
                        spine.localRotation,
                        index > 0);
                }

                rotations[rotations.Length - 1] = rotations[0];
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "Spine02"),
                        animationRoot),
                    times,
                    rotations);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static void StabilizeLegTracking(
            AnimationClip clip,
            Transform animationRoot)
        {
            var sample = UnityEngine.Object.Instantiate(animationRoot.gameObject);
            sample.name = "PlayerWalkForward_LegTracking";
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var hips = RequireNamedBone(sample.transform, "Hips");
                var leftUpLeg = RequireNamedBone(sample.transform, "LeftUpLeg");
                var leftLeg = RequireNamedBone(sample.transform, "LeftLeg");
                var leftFoot = RequireNamedBone(sample.transform, "LeftFoot");
                var rightUpLeg = RequireNamedBone(sample.transform, "RightUpLeg");
                var rightLeg = RequireNamedBone(sample.transform, "RightLeg");
                var rightFoot = RequireNamedBone(sample.transform, "RightFoot");
                var frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
                var times = Enumerable.Range(0, frameCount + 1)
                    .Select(index => Mathf.Min(index / clip.frameRate, clip.length))
                    .ToArray();
                var hipLaterals = new float[times.Length];
                var leftKneeLaterals = new float[times.Length];
                var rightKneeLaterals = new float[times.Length];
                var leftFootLaterals = new float[times.Length];
                var rightFootLaterals = new float[times.Length];
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(sample, times[index]);
                    hipLaterals[index] = Lateral(sample.transform, hips.position);
                    leftKneeLaterals[index] = Lateral(sample.transform, leftLeg.position);
                    rightKneeLaterals[index] = Lateral(sample.transform, rightLeg.position);
                    leftFootLaterals[index] = Lateral(sample.transform, leftFoot.position);
                    rightFootLaterals[index] = Lateral(sample.transform, rightFoot.position);
                }

                var center = hipLaterals.Average();
                var sideSign = Mathf.Sign(
                    leftFootLaterals.Average() - rightFootLaterals.Average());
                if (Mathf.Approximately(sideSign, 0f))
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Forward leg sides cannot be determined.");
                }

                var footHalfSpacing = Mathf.Max(
                    MinimumLegHalfSpacing,
                    Mathf.Abs(
                        leftFootLaterals.Average() -
                        rightFootLaterals.Average()) * 0.5f);
                var kneeHalfSpacing = Mathf.Max(
                    MinimumKneeCenterClearance,
                    Mathf.Abs(
                        leftKneeLaterals.Average() -
                        rightKneeLaterals.Average()) * 0.5f);
                var leftFootTarget = center + sideSign * footHalfSpacing;
                var rightFootTarget = center - sideSign * footHalfSpacing;
                var leftKneeTarget = center + sideSign * kneeHalfSpacing;
                var rightKneeTarget = center - sideSign * kneeHalfSpacing;

                var leftUpRotations = new Quaternion[times.Length];
                var leftLegRotations = new Quaternion[times.Length];
                var leftFootRotations = new Quaternion[times.Length];
                var rightUpRotations = new Quaternion[times.Length];
                var rightLegRotations = new Quaternion[times.Length];
                var rightFootRotations = new Quaternion[times.Length];
                for (var index = 0; index < times.Length; index++)
                {
                    clip.SampleAnimation(sample, times[index]);
                    SolveLegTracking(
                        sample.transform,
                        leftUpLeg,
                        leftLeg,
                        leftFoot,
                        leftKneeTarget,
                        leftFootTarget);
                    SolveLegTracking(
                        sample.transform,
                        rightUpLeg,
                        rightLeg,
                        rightFoot,
                        rightKneeTarget,
                        rightFootTarget);
                    leftUpRotations[index] = Continuous(
                        index > 0 ? leftUpRotations[index - 1] : leftUpLeg.localRotation,
                        leftUpLeg.localRotation,
                        index > 0);
                    leftLegRotations[index] = Continuous(
                        index > 0 ? leftLegRotations[index - 1] : leftLeg.localRotation,
                        leftLeg.localRotation,
                        index > 0);
                    leftFootRotations[index] = Continuous(
                        index > 0 ? leftFootRotations[index - 1] : leftFoot.localRotation,
                        leftFoot.localRotation,
                        index > 0);
                    rightUpRotations[index] = Continuous(
                        index > 0 ? rightUpRotations[index - 1] : rightUpLeg.localRotation,
                        rightUpLeg.localRotation,
                        index > 0);
                    rightLegRotations[index] = Continuous(
                        index > 0 ? rightLegRotations[index - 1] : rightLeg.localRotation,
                        rightLeg.localRotation,
                        index > 0);
                    rightFootRotations[index] = Continuous(
                        index > 0 ? rightFootRotations[index - 1] : rightFoot.localRotation,
                        rightFoot.localRotation,
                        index > 0);
                }

                CloseLoop(leftUpRotations);
                CloseLoop(leftLegRotations);
                CloseLoop(leftFootRotations);
                CloseLoop(rightUpRotations);
                CloseLoop(rightLegRotations);
                CloseLoop(rightFootRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "LeftUpLeg"),
                        animationRoot),
                    times,
                    leftUpRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "LeftLeg"),
                        animationRoot),
                    times,
                    leftLegRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "LeftFoot"),
                        animationRoot),
                    times,
                    leftFootRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "RightUpLeg"),
                        animationRoot),
                    times,
                    rightUpRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "RightLeg"),
                        animationRoot),
                    times,
                    rightLegRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(
                        RequireNamedBone(animationRoot, "RightFoot"),
                        animationRoot),
                    times,
                    rightFootRotations);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static void SolveLegTracking(
            Transform animationRoot,
            Transform upperLeg,
            Transform lowerLeg,
            Transform foot,
            float kneeLateralTarget,
            float footLateralTarget)
        {
            var footWorldRotation = foot.rotation;
            var target = foot.position + animationRoot.right *
                         (footLateralTarget - Lateral(animationRoot, foot.position));
            var hint = lowerLeg.position + animationRoot.right *
                       (kneeLateralTarget - Lateral(animationRoot, lowerLeg.position));
            var upperPosition = upperLeg.position;
            var upperLength = Vector3.Distance(upperPosition, lowerLeg.position);
            var lowerLength = Vector3.Distance(lowerLeg.position, foot.position);
            var targetVector = target - upperPosition;
            var targetDistance = Mathf.Clamp(
                targetVector.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            var targetDirection = targetVector.normalized;
            target = upperPosition + targetDirection * targetDistance;
            var poleDirection = Vector3.ProjectOnPlane(
                hint - upperPosition,
                targetDirection);
            if (poleDirection.sqrMagnitude <= 0.000001f)
            {
                poleDirection = Vector3.ProjectOnPlane(
                    lowerLeg.position - upperPosition,
                    targetDirection);
            }

            if (poleDirection.sqrMagnitude <= 0.000001f)
            {
                poleDirection = Vector3.ProjectOnPlane(
                    animationRoot.forward,
                    targetDirection);
            }

            poleDirection.Normalize();
            var cosine = Mathf.Clamp(
                (upperLength * upperLength + targetDistance * targetDistance -
                 lowerLength * lowerLength) /
                (2f * upperLength * targetDistance),
                -1f,
                1f);
            var sine = Mathf.Sqrt(Mathf.Max(0f, 1f - cosine * cosine));
            var desiredKnee = upperPosition +
                              (targetDirection * cosine + poleDirection * sine) *
                              upperLength;
            upperLeg.rotation = Quaternion.FromToRotation(
                                    lowerLeg.position - upperPosition,
                                    desiredKnee - upperPosition) *
                                upperLeg.rotation;
            lowerLeg.rotation = Quaternion.FromToRotation(
                                    foot.position - lowerLeg.position,
                                    target - lowerLeg.position) *
                                lowerLeg.rotation;
            foot.rotation = footWorldRotation;
        }

        private static LegAlignmentMetrics MeasureLegAlignment(
            AnimationClip clip,
            Transform animationRoot)
        {
            var sample = UnityEngine.Object.Instantiate(animationRoot.gameObject);
            sample.name = "PlayerWalkForward_LegAlignmentMeasurement";
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var hips = RequireNamedBone(sample.transform, "Hips");
                var leftKnee = RequireNamedBone(sample.transform, "LeftLeg");
                var rightKnee = RequireNamedBone(sample.transform, "RightLeg");
                var leftFoot = RequireNamedBone(sample.transform, "LeftFoot");
                var rightFoot = RequireNamedBone(sample.transform, "RightFoot");
                var frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
                var values = new List<LegAlignmentSample>(frameCount + 1);
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    clip.SampleAnimation(
                        sample,
                        Mathf.Min(frame / clip.frameRate, clip.length));
                    values.Add(new LegAlignmentSample(
                        Lateral(sample.transform, hips.position),
                        Lateral(sample.transform, leftKnee.position),
                        Lateral(sample.transform, rightKnee.position),
                        Lateral(sample.transform, leftFoot.position),
                        Lateral(sample.transform, rightFoot.position)));
                }

                var sideSign = Mathf.Sign(values.Average(value =>
                    value.LeftFoot - value.RightFoot));
                if (Mathf.Approximately(sideSign, 0f))
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Forward measured leg sides cannot be determined.");
                }

                return new LegAlignmentMetrics(
                    Range(values.Select(value => value.LeftFoot)),
                    Range(values.Select(value => value.RightFoot)),
                    Range(values.Select(value => value.LeftKnee)),
                    Range(values.Select(value => value.RightKnee)),
                    values.Min(value =>
                        sideSign * (value.LeftFoot - value.Hips)),
                    values.Min(value =>
                        -sideSign * (value.RightFoot - value.Hips)),
                    values.Min(value =>
                        sideSign * (value.LeftKnee - value.Hips)),
                    values.Min(value =>
                        -sideSign * (value.RightKnee - value.Hips)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static void ValidateLegAlignment(LegAlignmentMetrics metrics)
        {
            if (metrics.LeftFootLateralRange > MaximumFootLateralRange ||
                metrics.RightFootLateralRange > MaximumFootLateralRange ||
                metrics.LeftKneeLateralRange > MaximumKneeLateralRange ||
                metrics.RightKneeLateralRange > MaximumKneeLateralRange ||
                metrics.MinimumFootCenterClearance < MinimumFootCenterClearance ||
                metrics.MinimumKneeCenterClearance < MinimumKneeCenterClearance)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward legs do not keep parallel tracking." +
                    " LeftFootRange=" + Num(metrics.LeftFootLateralRange) +
                    ", RightFootRange=" + Num(metrics.RightFootLateralRange) +
                    ", LeftKneeRange=" + Num(metrics.LeftKneeLateralRange) +
                    ", RightKneeRange=" + Num(metrics.RightKneeLateralRange) +
                    ", MinimumFootClearance=" +
                    Num(metrics.MinimumFootCenterClearance) +
                    ", MinimumKneeClearance=" +
                    Num(metrics.MinimumKneeCenterClearance) + ".");
            }
        }

        private static float Lateral(Transform root, Vector3 worldPosition)
        {
            return Vector3.Dot(worldPosition - root.position, root.right);
        }

        private static float Range(IEnumerable<float> values)
        {
            var array = values.ToArray();
            return array.Max() - array.Min();
        }

        private static Quaternion Continuous(
            Quaternion previous,
            Quaternion current,
            bool hasPrevious)
        {
            return hasPrevious && Quaternion.Dot(previous, current) < 0f
                ? Negate(current)
                : current;
        }

        private static void CloseLoop(Quaternion[] rotations)
        {
            rotations[rotations.Length - 1] = rotations[0];
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            float[] times,
            Quaternion[] rotations)
        {
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.x",
                times,
                rotations.Select(value => value.x).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.y",
                times,
                rotations.Select(value => value.y).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.z",
                times,
                rotations.Select(value => value.z).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.w",
                times,
                rotations.Select(value => value.w).ToArray());
        }

        private static SwayMetrics MeasureSway(
            AnimationClip clip,
            Transform animationRoot)
        {
            var sample = UnityEngine.Object.Instantiate(animationRoot.gameObject);
            sample.name = "PlayerWalkForward_SwayMeasurement";
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var hips = RequireNamedBone(sample.transform, "Hips");
                var leftUpLeg = RequireNamedBone(sample.transform, "LeftUpLeg");
                var rightUpLeg = RequireNamedBone(sample.transform, "RightUpLeg");
                var head = RequireNamedBone(sample.transform, "Head");
                var frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
                var minimumPelvisLateral = float.PositiveInfinity;
                var maximumPelvisLateral = float.NegativeInfinity;
                var minimumTorsoLean = float.PositiveInfinity;
                var maximumTorsoLean = float.NegativeInfinity;
                var maximumAbsoluteTorsoLean = 0f;
                var minimumPelvisRoll = float.PositiveInfinity;
                var maximumPelvisRoll = float.NegativeInfinity;
                var maximumAbsolutePelvisRoll = 0f;
                var torsoLeanSum = 0f;
                var sampleCount = 0;
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var time = Mathf.Min(frame / clip.frameRate, clip.length);
                    clip.SampleAnimation(sample, time);
                    var pelvisLateral = Vector3.Dot(
                        hips.position - sample.transform.position,
                        sample.transform.right);
                    var centerLine = head.position - hips.position;
                    var pelvisRoll = PelvisRollDegrees(
                        leftUpLeg,
                        rightUpLeg,
                        sample.transform);
                    var torsoLean = Mathf.Atan2(
                                        Vector3.Dot(centerLine, sample.transform.right),
                                        Vector3.Dot(centerLine, sample.transform.up)) *
                                    Mathf.Rad2Deg;
                    minimumPelvisLateral = Mathf.Min(
                        minimumPelvisLateral,
                        pelvisLateral);
                    maximumPelvisLateral = Mathf.Max(
                        maximumPelvisLateral,
                        pelvisLateral);
                    minimumPelvisRoll = Mathf.Min(minimumPelvisRoll, pelvisRoll);
                    maximumPelvisRoll = Mathf.Max(maximumPelvisRoll, pelvisRoll);
                    maximumAbsolutePelvisRoll = Mathf.Max(
                        maximumAbsolutePelvisRoll,
                        Mathf.Abs(pelvisRoll));
                    minimumTorsoLean = Mathf.Min(minimumTorsoLean, torsoLean);
                    maximumTorsoLean = Mathf.Max(maximumTorsoLean, torsoLean);
                    maximumAbsoluteTorsoLean = Mathf.Max(
                        maximumAbsoluteTorsoLean,
                        Mathf.Abs(torsoLean));
                    torsoLeanSum += torsoLean;
                    sampleCount++;
                }

                return new SwayMetrics(
                    maximumPelvisLateral - minimumPelvisLateral,
                    maximumPelvisRoll - minimumPelvisRoll,
                    maximumAbsolutePelvisRoll,
                    maximumTorsoLean - minimumTorsoLean,
                    maximumAbsoluteTorsoLean,
                    sampleCount > 0 ? torsoLeanSum / sampleCount : 0f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static float PelvisRollDegrees(
            Transform leftUpLeg,
            Transform rightUpLeg,
            Transform animationRoot)
        {
            var hipLine = rightUpLeg.position - leftUpLeg.position;
            if (Vector3.Dot(hipLine, animationRoot.right) < 0f)
            {
                hipLine = -hipLine;
            }

            var horizontal = Vector3.Dot(hipLine, animationRoot.right);
            if (Mathf.Abs(horizontal) <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward pelvis line is invalid.");
            }

            return Mathf.Atan2(
                       Vector3.Dot(hipLine, animationRoot.up),
                       horizontal) *
                   Mathf.Rad2Deg;
        }

        private static void SetSampledCurve(
            AnimationClip clip,
            string path,
            string property,
            float[] times,
            float[] values)
        {
            if (times.Length != values.Length || times.Length < 2)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward sampled curve data differs.");
            }

            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                float tangent;
                if (index == 0)
                {
                    tangent = (values[1] - values[0]) / (times[1] - times[0]);
                }
                else if (index == times.Length - 1)
                {
                    tangent = (values[index] - values[index - 1]) /
                              (times[index] - times[index - 1]);
                }
                else
                {
                    tangent = (values[index + 1] - values[index - 1]) /
                              (times[index + 1] - times[index - 1]);
                }

                keys[index] = new Keyframe(
                    times[index],
                    values[index],
                    tangent,
                    tangent);
            }

            var curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static Quaternion Negate(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            DeleteAssetIfPresent(
                ControllerPath,
                "Existing Player_Walk_Forward controller could not be replaced.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("PlayerWalkForward");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureAnimator(
            Transform walkInstance,
            AnimatorController controller)
        {
            var animator = RequireAnimator(walkInstance);
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        private static WalkMetrics Inspect(
            AnimationClip clip,
            AnimatorController controller)
        {
            RequireScene();
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var walkInstance = RequireDirectChild(layoutRoot, WalkKey);
            var animator = RequireAnimator(walkInstance);
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward Animator configuration differs.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward is not the controller default motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward clip is not looping.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward contains no animation curves.");
            }

            var missingPaths = bindings
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => !string.IsNullOrEmpty(path) && walkInstance.Find(path) == null)
                .ToArray();
            if (missingPaths.Length != 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward has incompatible transform paths: " +
                    string.Join(", ", missingPaths) + ".");
            }

            var remainingPlanarDrift = RemainingPlanarDrift(clip, walkInstance);
            if (remainingPlanarDrift > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward retains accumulated planar drift. Drift=" +
                    Num(remainingPlanarDrift) + ".");
            }

            var evaluation = EvaluateMotion(clip, walkInstance);
            if (evaluation.RootPositionError > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward root moved during sampled playback. Error=" +
                    Num(evaluation.RootPositionError) + ".");
            }

            if (evaluation.LeftFootTravel < MinimumFootTravel ||
                evaluation.RightFootTravel < MinimumFootTravel)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward does not contain a full alternating step. Left=" +
                    Num(evaluation.LeftFootTravel) + ", Right=" +
                    Num(evaluation.RightFootTravel) + ".");
            }

            var sway = MeasureSway(clip, walkInstance);
            if (sway.PelvisLateralTravel > MaximumPelvisLateralTravel ||
                sway.PelvisRollRangeDegrees > MaximumPelvisRollRangeDegrees ||
                sway.MaximumPelvisRollDegrees > MaximumPelvisRollDegrees ||
                sway.TorsoLeanRangeDegrees > MaximumTorsoLeanRangeDegrees ||
                sway.MaximumTorsoLeanDegrees > MaximumTorsoLeanDegrees)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward remains too laterally unstable." +
                    " PelvisLateral=" + Num(sway.PelvisLateralTravel) +
                    ", PelvisRollRange=" + Num(sway.PelvisRollRangeDegrees) +
                    ", MaximumPelvisRoll=" + Num(sway.MaximumPelvisRollDegrees) +
                    ", TorsoLeanRange=" + Num(sway.TorsoLeanRangeDegrees) +
                    ", MaximumTorsoLean=" + Num(sway.MaximumTorsoLeanDegrees) + ".");
            }

            if (Mathf.Abs(sway.MeanTorsoLeanDegrees) >
                MaximumMeanTorsoLeanDegrees)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward upper body is laterally off center." +
                    " MeanTorsoLean=" + Num(sway.MeanTorsoLeanDegrees) + ".");
            }

            ValidateLegAlignment(MeasureLegAlignment(clip, walkInstance));

            return new WalkMetrics(
                clip.length,
                clip.frameRate,
                bindings.Length,
                sway.PelvisLateralTravel,
                sway.PelvisRollRangeDegrees,
                sway.MaximumPelvisRollDegrees,
                sway.TorsoLeanRangeDegrees,
                sway.MaximumTorsoLeanDegrees,
                sway.MeanTorsoLeanDegrees,
                remainingPlanarDrift,
                evaluation.RootPositionError,
                evaluation.LeftFootTravel,
                evaluation.RightFootTravel);
        }

        private static float RemainingPlanarDrift(
            AnimationClip clip,
            Transform animationRoot)
        {
            var hips = RequireNamedBone(animationRoot, "Hips");
            if (hips.parent == null)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward Hips has no parent transform.");
            }

            var hipsPath = AnimationUtility.CalculateTransformPath(
                hips,
                animationRoot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var hipsPositionBindings = bindings
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    binding.path.Equals(hipsPath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith(
                        "m_LocalPosition.",
                        StringComparison.Ordinal))
                .ToDictionary(
                    binding => binding.propertyName,
                    binding => binding,
                    StringComparer.Ordinal);
            var localDelta = new Vector3(
                CurveDelta(clip, hipsPositionBindings, "m_LocalPosition.x"),
                CurveDelta(clip, hipsPositionBindings, "m_LocalPosition.y"),
                CurveDelta(clip, hipsPositionBindings, "m_LocalPosition.z"));
            var planarWorldDelta = Vector3.ProjectOnPlane(
                hips.parent.TransformVector(localDelta),
                Vector3.up);

            var animatorRootDrift = 0f;
            foreach (var binding in bindings.Where(IsAnimatorPlanarRootBinding))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length < 2)
                {
                    continue;
                }

                animatorRootDrift = Mathf.Max(
                    animatorRootDrift,
                    Mathf.Abs(
                        curve.keys[curve.length - 1].value -
                        curve.keys[0].value));
            }

            return Mathf.Max(planarWorldDelta.magnitude, animatorRootDrift);
        }

        private static MotionEvaluation EvaluateMotion(
            AnimationClip clip,
            Transform walkInstance)
        {
            var sample = UnityEngine.Object.Instantiate(walkInstance.gameObject);
            sample.name = "PlayerWalkForward_Evaluation";
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var leftFoot = RequireNamedBone(sample.transform, "LeftFoot");
                var rightFoot = RequireNamedBone(sample.transform, "RightFoot");
                var rootPositionError = 0f;
                var leftPositions = new List<Vector3>();
                var rightPositions = new List<Vector3>();
                foreach (var phase in ReviewPhases.Append(1f))
                {
                    clip.SampleAnimation(sample, clip.length * phase);
                    rootPositionError = Mathf.Max(
                        rootPositionError,
                        sample.transform.position.magnitude);
                    leftPositions.Add(sample.transform.InverseTransformPoint(leftFoot.position));
                    rightPositions.Add(sample.transform.InverseTransformPoint(rightFoot.position));
                }

                return new MotionEvaluation(
                    rootPositionError,
                    MaximumTravel(leftPositions),
                    MaximumTravel(rightPositions));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static float MaximumTravel(IReadOnlyList<Vector3> positions)
        {
            var maximum = 0f;
            for (var left = 0; left < positions.Count; left++)
            {
                for (var right = left + 1; right < positions.Count; right++)
                {
                    maximum = Mathf.Max(
                        maximum,
                        Vector3.Distance(positions[left], positions[right]));
                }
            }

            return maximum;
        }

        private static void CapturePhaseStrip(AnimationClip clip, string destination)
        {
            var scene = RequireScene();
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var walkInstance = RequireDirectChild(layoutRoot, WalkKey);
            const float spacing = 2.2f;
            var samples = new List<GameObject>();
            var guides = new List<GameObject>();
            Material guideMaterial = null;
            GameObject cameraObject = null;
            GameObject keyLightObject = null;
            GameObject fillLightObject = null;
            RendererState[] rendererStates = null;
            try
            {
                for (var index = 0; index < ReviewPhases.Length; index++)
                {
                    var sample = UnityEngine.Object.Instantiate(walkInstance.gameObject);
                    sample.name = "PlayerWalkForward_" +
                                  ReviewPhases[index].ToString(
                                      "0.00",
                                      CultureInfo.InvariantCulture);
                    sample.hideFlags = HideFlags.HideAndDontSave;
                    sample.transform.SetPositionAndRotation(
                        new Vector3(
                            (index - (ReviewPhases.Length - 1) * 0.5f) * spacing,
                            0f,
                            0f),
                        Quaternion.Euler(0f, FacingYaw, 0f));
                    sample.transform.localScale = Vector3.one;
                    var animator = RequireAnimator(sample.transform);
                    animator.enabled = false;
                    animator.runtimeAnimatorController = null;
                    clip.SampleAnimation(sample, clip.length * ReviewPhases[index]);
                    samples.Add(sample);
                }

                var sampleRenderers = samples
                    .SelectMany(sample => sample.GetComponentsInChildren<Renderer>(true))
                    .Where(renderer => renderer.enabled)
                    .ToArray();
                var sampleBounds = BoundsOf(sampleRenderers);
                var guideShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                  Shader.Find("Unlit/Color") ??
                                  throw new InvalidOperationException(
                                      "No unlit shader is available for walk review guides.");
                guideMaterial = new Material(guideShader)
                {
                    name = "PlayerWalkForwardReviewGuide",
                    color = new Color(0.15f, 0.85f, 1f, 1f),
                    hideFlags = HideFlags.HideAndDontSave
                };

                guides.Add(CreateGuide(
                    "PlayerWalkForward_GroundGuide",
                    new Vector3(0f, sampleBounds.min.y - 0.012f, 0.22f),
                    new Vector3(
                        spacing * ReviewPhases.Length + 0.8f,
                        0.008f,
                        0.04f),
                    guideMaterial));
                foreach (var sample in samples)
                {
                    guides.Add(CreateGuide(
                        sample.name + "_RootGuide",
                        new Vector3(
                            sample.transform.position.x,
                            sampleBounds.center.y,
                            0.22f),
                        new Vector3(0.008f, sampleBounds.size.y, 0.04f),
                        guideMaterial));
                }

                var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                rendererStates = allRenderers
                    .Select(renderer => new RendererState(renderer))
                    .ToArray();
                var visibleRenderers = sampleRenderers
                    .Concat(guides.Select(item => item.GetComponent<Renderer>()))
                    .Where(renderer => renderer != null)
                    .ToHashSet();
                foreach (var renderer in allRenderers)
                {
                    renderer.enabled = visibleRenderers.Contains(renderer);
                }

                cameraObject = new GameObject(
                    "PlayerWalkForwardReviewCamera",
                    typeof(Camera));
                keyLightObject = new GameObject(
                    "PlayerWalkForwardReviewKeyLight",
                    typeof(Light));
                fillLightObject = new GameObject(
                    "PlayerWalkForwardReviewFillLight",
                    typeof(Light));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                keyLightObject.hideFlags = HideFlags.HideAndDontSave;
                fillLightObject.hideFlags = HideFlags.HideAndDontSave;
                ConfigureLight(
                    keyLightObject.GetComponent<Light>(),
                    Quaternion.Euler(35f, -25f, 0f),
                    1.7f);
                ConfigureLight(
                    fillLightObject.GetComponent<Light>(),
                    Quaternion.Euler(20f, 150f, 0f),
                    0.85f);

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
                camera.orthographic = true;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                var review = RenderPanel(
                    camera,
                    CaptureWidth,
                    CaptureHeight,
                    BoundsOf(visibleRenderers),
                    new Vector3(0.08f, 0.06f, -1f));
                try
                {
                    RequireVisibleCapture(review, camera.backgroundColor);
                    File.WriteAllBytes(destination, review.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(review);
                }
            }
            finally
            {
                if (rendererStates != null)
                {
                    foreach (var state in rendererStates)
                    {
                        state.Restore();
                    }
                }

                foreach (var guide in guides.Where(item => item != null))
                {
                    UnityEngine.Object.DestroyImmediate(guide);
                }

                foreach (var sample in samples.Where(item => item != null))
                {
                    UnityEngine.Object.DestroyImmediate(sample);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (keyLightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(keyLightObject);
                }

                if (fillLightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(fillLightObject);
                }

                if (guideMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(guideMaterial);
                }
            }
        }

        private static Texture2D RenderPanel(
            Camera camera,
            int width,
            int height,
            Bounds bounds,
            Vector3 viewDirection)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                var direction = viewDirection.normalized;
                var distance = Mathf.Max(10f, bounds.extents.magnitude * 3f);
                camera.aspect = width / (float)height;
                camera.transform.position = bounds.center + direction * distance;
                camera.transform.LookAt(bounds.center, Vector3.up);
                var horizontalExtent = ProjectedHalfExtent(
                    bounds.extents,
                    camera.transform.right);
                var verticalExtent = ProjectedHalfExtent(
                    bounds.extents,
                    camera.transform.up);
                camera.orthographicSize = Mathf.Max(
                    verticalExtent * 1.08f,
                    horizontalExtent / camera.aspect * 1.08f,
                    1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f + 10f;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                panel.Apply();
                return panel;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreateGuide(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guide.name = name;
            guide.hideFlags = HideFlags.HideAndDontSave;
            guide.transform.SetPositionAndRotation(position, Quaternion.identity);
            guide.transform.localScale = scale;
            var collider = guide.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            guide.GetComponent<Renderer>().sharedMaterial = material;
            return guide;
        }

        private static void RequireVisibleCapture(Texture2D image, Color background)
        {
            var pixels = image.GetPixels32();
            var background32 = (Color32)background;
            var visible = 0;
            var sampled = 0;
            const int stride = 16;
            for (var index = 0; index < pixels.Length; index += stride)
            {
                sampled++;
                var pixel = pixels[index];
                var difference = Mathf.Abs(pixel.r - background32.r) +
                                 Mathf.Abs(pixel.g - background32.g) +
                                 Mathf.Abs(pixel.b - background32.b);
                if (difference >= 18)
                {
                    visible++;
                }
            }

            var visibleRatio = visible / (float)sampled;
            if (visibleRatio < 0.005f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward capture contains no visible model. Ratio=" +
                    Num(visibleRatio) + ".");
            }
        }

        private static Bounds BoundsOf(IEnumerable<Renderer> renderers)
        {
            var array = renderers.Where(renderer => renderer != null).ToArray();
            if (array.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Forward review has no visible renderer.");
            }

            var bounds = array[0].bounds;
            foreach (var renderer in array.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static float ProjectedHalfExtent(Vector3 extents, Vector3 axis)
        {
            return Mathf.Abs(axis.x) * extents.x +
                   Mathf.Abs(axis.y) * extents.y +
                   Mathf.Abs(axis.z) * extents.z;
        }

        private static void ConfigureLight(
            Light light,
            Quaternion rotation,
            float intensity)
        {
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            light.transform.rotation = rotation;
        }

        private static Animator RequireAnimator(Transform root)
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    root.name + " contains multiple Animators.");
            }

            return animators.Length == 1
                ? animators[0]
                : root.gameObject.AddComponent<Animator>();
        }

        private static Transform RequireNamedBone(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name.Equals(name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player rig bone differs: " + name + ". Count=" +
                    matches.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return matches[0];
        }

        private static string[] OtherAnimationStates(
            Transform layoutRoot,
            Transform excluded)
        {
            return Enumerable.Range(0, layoutRoot.childCount)
                .Select(layoutRoot.GetChild)
                .Where(child => child != excluded)
                .Select(child =>
                    child.name + "|" + string.Join(
                        ";",
                        child.GetComponentsInChildren<Animator>(true)
                            .Select(animator =>
                                animator.enabled + "," +
                                AssetDatabase.GetAssetPath(
                                    animator.runtimeAnimatorController) + "," +
                                animator.applyRootMotion)))
                .ToArray();
        }

        private static void RequireEqual(
            string[] expected,
            string[] actual,
            string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void DeleteAssetIfPresent(string path, string message)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return root;
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(child => child.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            }

            return matches[0];
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene. ActiveScene=" +
                    scene.path + ".");
            }

            return scene;
        }

        private static string Absolute(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException(
                                  "Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private readonly struct WalkMetrics
        {
            public WalkMetrics(
                float duration,
                float frameRate,
                int curveBindingCount,
                float pelvisLateralTravel,
                float pelvisRollRangeDegrees,
                float maximumPelvisRollDegrees,
                float torsoLeanRangeDegrees,
                float maximumTorsoLeanDegrees,
                float meanTorsoLeanDegrees,
                float remainingPlanarDrift,
                float rootPositionError,
                float leftFootTravel,
                float rightFootTravel)
            {
                Duration = duration;
                FrameRate = frameRate;
                CurveBindingCount = curveBindingCount;
                PelvisLateralTravel = pelvisLateralTravel;
                PelvisRollRangeDegrees = pelvisRollRangeDegrees;
                MaximumPelvisRollDegrees = maximumPelvisRollDegrees;
                TorsoLeanRangeDegrees = torsoLeanRangeDegrees;
                MaximumTorsoLeanDegrees = maximumTorsoLeanDegrees;
                MeanTorsoLeanDegrees = meanTorsoLeanDegrees;
                RemainingPlanarDrift = remainingPlanarDrift;
                RootPositionError = rootPositionError;
                LeftFootTravel = leftFootTravel;
                RightFootTravel = rightFootTravel;
            }

            public float Duration { get; }
            public float FrameRate { get; }
            public int CurveBindingCount { get; }
            public float PelvisLateralTravel { get; }
            public float PelvisRollRangeDegrees { get; }
            public float MaximumPelvisRollDegrees { get; }
            public float TorsoLeanRangeDegrees { get; }
            public float MaximumTorsoLeanDegrees { get; }
            public float MeanTorsoLeanDegrees { get; }
            public float RemainingPlanarDrift { get; }
            public float RootPositionError { get; }
            public float LeftFootTravel { get; }
            public float RightFootTravel { get; }
        }

        private readonly struct SwayMetrics
        {
            public SwayMetrics(
                float pelvisLateralTravel,
                float pelvisRollRangeDegrees,
                float maximumPelvisRollDegrees,
                float torsoLeanRangeDegrees,
                float maximumTorsoLeanDegrees,
                float meanTorsoLeanDegrees)
            {
                PelvisLateralTravel = pelvisLateralTravel;
                PelvisRollRangeDegrees = pelvisRollRangeDegrees;
                MaximumPelvisRollDegrees = maximumPelvisRollDegrees;
                TorsoLeanRangeDegrees = torsoLeanRangeDegrees;
                MaximumTorsoLeanDegrees = maximumTorsoLeanDegrees;
                MeanTorsoLeanDegrees = meanTorsoLeanDegrees;
            }

            public float PelvisLateralTravel { get; }
            public float PelvisRollRangeDegrees { get; }
            public float MaximumPelvisRollDegrees { get; }
            public float TorsoLeanRangeDegrees { get; }
            public float MaximumTorsoLeanDegrees { get; }
            public float MeanTorsoLeanDegrees { get; }
        }

        private readonly struct UprightCorrectionMetrics
        {
            public UprightCorrectionMetrics(
                SwayMetrics before,
                SwayMetrics after,
                LegAlignmentMetrics legsBefore,
                LegAlignmentMetrics legsAfter)
            {
                Before = before;
                After = after;
                LegsBefore = legsBefore;
                LegsAfter = legsAfter;
            }

            public SwayMetrics Before { get; }
            public SwayMetrics After { get; }
            public LegAlignmentMetrics LegsBefore { get; }
            public LegAlignmentMetrics LegsAfter { get; }
        }

        private readonly struct LegAlignmentSample
        {
            public LegAlignmentSample(
                float hips,
                float leftKnee,
                float rightKnee,
                float leftFoot,
                float rightFoot)
            {
                Hips = hips;
                LeftKnee = leftKnee;
                RightKnee = rightKnee;
                LeftFoot = leftFoot;
                RightFoot = rightFoot;
            }

            public float Hips { get; }
            public float LeftKnee { get; }
            public float RightKnee { get; }
            public float LeftFoot { get; }
            public float RightFoot { get; }
        }

        private readonly struct LegAlignmentMetrics
        {
            public LegAlignmentMetrics(
                float leftFootLateralRange,
                float rightFootLateralRange,
                float leftKneeLateralRange,
                float rightKneeLateralRange,
                float minimumLeftFootCenterClearance,
                float minimumRightFootCenterClearance,
                float minimumLeftKneeCenterClearance,
                float minimumRightKneeCenterClearance)
            {
                LeftFootLateralRange = leftFootLateralRange;
                RightFootLateralRange = rightFootLateralRange;
                LeftKneeLateralRange = leftKneeLateralRange;
                RightKneeLateralRange = rightKneeLateralRange;
                MinimumLeftFootCenterClearance = minimumLeftFootCenterClearance;
                MinimumRightFootCenterClearance = minimumRightFootCenterClearance;
                MinimumLeftKneeCenterClearance = minimumLeftKneeCenterClearance;
                MinimumRightKneeCenterClearance = minimumRightKneeCenterClearance;
            }

            public float LeftFootLateralRange { get; }
            public float RightFootLateralRange { get; }
            public float LeftKneeLateralRange { get; }
            public float RightKneeLateralRange { get; }
            public float MinimumLeftFootCenterClearance { get; }
            public float MinimumRightFootCenterClearance { get; }
            public float MinimumLeftKneeCenterClearance { get; }
            public float MinimumRightKneeCenterClearance { get; }
            public float MinimumFootCenterClearance => Mathf.Min(
                MinimumLeftFootCenterClearance,
                MinimumRightFootCenterClearance);
            public float MinimumKneeCenterClearance => Mathf.Min(
                MinimumLeftKneeCenterClearance,
                MinimumRightKneeCenterClearance);
        }

        private readonly struct MotionEvaluation
        {
            public MotionEvaluation(
                float rootPositionError,
                float leftFootTravel,
                float rightFootTravel)
            {
                RootPositionError = rootPositionError;
                LeftFootTravel = leftFootTravel;
                RightFootTravel = rightFootTravel;
            }

            public float RootPositionError { get; }
            public float LeftFootTravel { get; }
            public float RightFootTravel { get; }
        }

        private readonly struct TransformState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformState(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(position, transform.position) <= PositionTolerance &&
                       Quaternion.Angle(rotation, transform.rotation) <= 0.01f &&
                       Vector3.Distance(scale, transform.localScale) <= PositionTolerance;
            }
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }
    }
}
