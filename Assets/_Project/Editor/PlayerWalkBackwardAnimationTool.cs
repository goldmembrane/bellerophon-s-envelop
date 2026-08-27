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
    internal static class PlayerWalkBackwardAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string LayoutRootName = "PlayerAnimationLayout";
        private const string TargetKey = "Player_Walk_Backward";
        private const string SourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Mixamo.fbx";
        private const string MeshyDirectSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Meshy_Direct.fbx";
        private const string ClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Mixamo.anim";
        private const string MeshyInPlaceClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Meshy_InPlace.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward.controller";
        private const string SourceClipName = "Player_Walk_Backward_Mixamo_Source";
        private const string GeneratedClipName = "Player_Walk_Backward_Mixamo";
        private const string StateName = "PlayerWalkBackward";
        private const string FinalCapturePath =
            "docs/validation/player_walk_backward_mixamo_final.png";
        private const string MeshyDirectFinalCapturePath =
            "docs/validation/player_walk_backward_meshy_direct_final.png";
        private const string MeshyInPlaceFinalCapturePath =
            "docs/validation/player_walk_backward_meshy_in_place_final.png";
        private const string MeshyDirectTakeSuffix = "|Walk_Backward|baselayer";
        private const float ArmSwingScale = 0.3f;
        private const float PositionTolerance = 0.001f;
        private const float MinimumFootTravel = 0.08f;
        private const int CaptureWidth = 3840;
        private const int CaptureHeight = 2160;
        private static readonly float[] ReviewPhases = { 0f, 0.25f, 0.5f, 0.75f };
        private static readonly HashSet<string> ArmBoneNames = new HashSet<string>(
            new[]
            {
                "LeftShoulder",
                "LeftArm",
                "LeftForeArm",
                "LeftHand",
                "RightShoulder",
                "RightArm",
                "RightForeArm",
                "RightHand"
            },
            StringComparer.Ordinal);

        public static void Apply()
        {
            var scene = RecoverFailedApplyIfPresent(RequireScene());
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before applying Player_Walk_Backward.");
            }

            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var target = RequireDirectChild(layoutRoot, TargetKey);
            var rootBefore = new TransformState(target);
            var renderersBefore = RendererAssetStates(target);
            var otherAnimationStates = OtherAnimationStates(layoutRoot, target);
            var targetModelBefore = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                        target.gameObject) ??
                                    throw new InvalidOperationException(
                                        "Player_Walk_Backward has no original model prefab.");
            var targetModelPathBefore = AssetDatabase.GetAssetPath(targetModelBefore);

            var sourceClip = ConfigureAndLoadSourceClip();
            var clip = CreateAdjustedClip(
                sourceClip,
                target,
                out var removedPlanarTravel,
                out var armSwing);
            var controller = RequireController(clip);
            ConfigureAnimator(target, controller);
            var metrics = Inspect(clip, controller, armSwing);

            if (!rootBefore.Matches(target))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward root transform changed during apply.");
            }

            RequireEqual(
                renderersBefore,
                RendererAssetStates(target),
                "Player_Walk_Backward model, skin, or renderer assets changed.");
            var targetModelAfter = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                       target.gameObject) ??
                                   throw new InvalidOperationException(
                                       "Player_Walk_Backward lost its original model prefab.");
            if (!targetModelPathBefore.Equals(
                    AssetDatabase.GetAssetPath(targetModelAfter),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward model prefab connection changed.");
            }

            RequireEqual(
                otherAnimationStates,
                OtherAnimationStates(layoutRoot, target),
                "A player instance outside Player_Walk_Backward changed animation state.");
            if (!EditorSceneManager.SaveScene(scene) || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp did not save the Player_Walk_Backward Animator connection.");
            }

            Debug.Log(
                "PlayerWalkBackward Mixamo applied." +
                " SourceClip=" + sourceClip.name +
                ", Duration=" + Num(metrics.Duration) +
                ", FrameRate=" + Num(metrics.FrameRate) +
                ", RemovedPlanarTravel=" + Num(removedPlanarTravel) +
                ", RemainingPlanarDrift=" + Num(metrics.RemainingPlanarDrift) +
                ", RootPositionError=" + Num(metrics.RootPositionError) +
                ", LeftFootTravel=" + Num(metrics.LeftFootTravel) +
                ", RightFootTravel=" + Num(metrics.RightFootTravel) +
                ", LoopPositionError=" + Num(metrics.LoopPositionError) +
                ", LoopRotationError=" + Num(metrics.LoopRotationError) +
                ", ArmAnimatedPaths=" + armSwing.PathCount.ToString(
                    CultureInfo.InvariantCulture) +
                ", ArmSourceMaxDeviation=" + Num(armSwing.SourceMaxDeviation) +
                ", ArmAdjustedMaxDeviation=" + Num(armSwing.AdjustedMaxDeviation) +
                ", ArmMinimumRatio=" + Num(armSwing.MinimumRatio) +
                ", ArmMaximumRatio=" + Num(armSwing.MaximumRatio) +
                ", ArmSwingScale=" + Num(ArmSwingScale) +
                ", Loop=True" +
                ", ApplyRootMotion=False" +
                ", ExistingModelPreserved=True" +
                ", SourceSkinConnected=False" +
                ", OtherInstancesUnchanged=True" +
                ", SceneSaved=True.");
        }

        public static void ApplyMeshyDirect()
        {
            var scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before applying the direct Meshy clip.");
            }

            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var target = RequireDirectChild(layoutRoot, TargetKey);
            var rootBefore = new TransformState(target);
            var renderersBefore = RendererAssetStates(target);
            var otherAnimationStates = OtherAnimationStates(layoutRoot, target);
            var targetModelBefore = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                        target.gameObject) ??
                                    throw new InvalidOperationException(
                                        "Player_Walk_Backward has no original model prefab.");
            var targetModelPathBefore = AssetDatabase.GetAssetPath(targetModelBefore);
            var preservedSource = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                                  throw new InvalidOperationException(
                                      "The previous Player_Walk_Backward Mixamo FBX is missing.");
            var preservedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                                throw new InvalidOperationException(
                                    "The previous Player_Walk_Backward Mixamo clip is missing.");

            var clip = ConfigureAndLoadMeshyDirectClip(out var takeName);
            var controller = ConnectMeshyDirectClip(clip);
            var bindingCount = InspectMeshyDirect(clip, controller, target);

            if (!rootBefore.Matches(target))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward root transform changed during direct connection.");
            }

            RequireEqual(
                renderersBefore,
                RendererAssetStates(target),
                "Player_Walk_Backward model, skin, or renderer assets changed.");
            var targetModelAfter = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                       target.gameObject) ??
                                   throw new InvalidOperationException(
                                       "Player_Walk_Backward lost its original model prefab.");
            if (!targetModelPathBefore.Equals(
                    AssetDatabase.GetAssetPath(targetModelAfter),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward model prefab connection changed.");
            }

            RequireEqual(
                otherAnimationStates,
                OtherAnimationStates(layoutRoot, target),
                "A player instance outside Player_Walk_Backward changed animation state.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) != preservedSource ||
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) != preservedClip)
            {
                throw new InvalidOperationException(
                    "The previous Player_Walk_Backward Mixamo assets changed.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Direct Meshy connection changed the CargoRunMvp scene.");
            }

            Debug.Log(
                "PlayerWalkBackward direct Meshy animation connected." +
                " SourceAsset=" + MeshyDirectSourcePath +
                ", Take=" + takeName +
                ", ImportedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", CurveBindings=" + bindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceCurvesUnchanged=True" +
                ", LoopTimeOnly=True" +
                ", ApplyRootMotion=False" +
                ", PreviousMixamoAssetsPreserved=True" +
                ", ExistingModelPreserved=True" +
                ", SourceSkinConnected=False" +
                ", OtherInstancesUnchanged=True" +
                ", SceneChanged=False.");
        }

        public static void ApplyMeshyInPlace()
        {
            var scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before applying Meshy in-place motion.");
            }

            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var target = RequireDirectChild(layoutRoot, TargetKey);
            var rootBefore = new TransformState(target);
            var renderersBefore = RendererAssetStates(target);
            var otherAnimationStates = OtherAnimationStates(layoutRoot, target);
            var targetModelBefore = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                        target.gameObject) ??
                                    throw new InvalidOperationException(
                                        "Player_Walk_Backward has no original model prefab.");
            var targetModelPathBefore = AssetDatabase.GetAssetPath(targetModelBefore);
            var sourceClip = LoadSingleMeshyDirectClip();
            var sourceSignaturesBefore = CurveSignatures(sourceClip);
            var sourceObjectSignaturesBefore = ObjectCurveSignatures(sourceClip);

            var clip = CreateMeshyInPlaceClip(
                sourceClip,
                target,
                out var correction);
            var controller = ConnectMeshyDirectClip(clip);
            var evaluation = InspectMeshyInPlace(
                sourceClip,
                clip,
                controller,
                target,
                correction);

            RequireEqual(
                sourceSignaturesBefore,
                CurveSignatures(sourceClip),
                "The direct Meshy source curves changed while creating in-place motion.");
            RequireEqual(
                sourceObjectSignaturesBefore,
                ObjectCurveSignatures(sourceClip),
                "The direct Meshy source object curves changed while creating in-place motion.");
            if (!rootBefore.Matches(target))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward root transform changed during in-place apply.");
            }

            RequireEqual(
                renderersBefore,
                RendererAssetStates(target),
                "Player_Walk_Backward model, skin, or renderer assets changed.");
            var targetModelAfter = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                       target.gameObject) ??
                                   throw new InvalidOperationException(
                                       "Player_Walk_Backward lost its original model prefab.");
            if (!targetModelPathBefore.Equals(
                    AssetDatabase.GetAssetPath(targetModelAfter),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward model prefab connection changed.");
            }

            RequireEqual(
                otherAnimationStates,
                OtherAnimationStates(layoutRoot, target),
                "A player instance outside Player_Walk_Backward changed animation state.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Meshy in-place apply changed the CargoRunMvp scene.");
            }

            Debug.Log(
                "PlayerWalkBackward Meshy in-place animation applied." +
                " SourceAsset=" + MeshyDirectSourcePath +
                ", DerivedClip=" + MeshyInPlaceClipPath +
                ", Carrier=" + correction.Carrier +
                ", SourcePlanarEndDelta=" + Num(correction.SourceEndDelta) +
                ", SourcePlanarRange=" + Num(correction.SourceRange) +
                ", InPlacePlanarRange=" + Num(correction.InPlaceRange) +
                ", ChangedBindings=" + string.Join(",", correction.BindingKeys) +
                ", Duration=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", HipsEndPlanarDrift=" + Num(evaluation.RemainingPlanarDrift) +
                ", RootPositionError=" + Num(evaluation.RootPositionError) +
                ", LeftFootTravel=" + Num(evaluation.LeftFootTravel) +
                ", RightFootTravel=" + Num(evaluation.RightFootTravel) +
                ", OnlyWorldPlanarCarrierAxesChanged=True" +
                ", SourceCurvesPreserved=True" +
                ", Loop=True" +
                ", ApplyRootMotion=False" +
                ", ExistingModelPreserved=True" +
                ", OtherInstancesUnchanged=True" +
                ", SceneChanged=False.");
        }

        public static void CaptureMeshyInPlaceFinal()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var sourceClip = LoadSingleMeshyDirectClip();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                           MeshyInPlaceClipPath) ??
                       throw new InvalidOperationException(
                           "Player_Walk_Backward Meshy in-place clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                                 ControllerPath) ??
                             throw new InvalidOperationException(
                                 "Player_Walk_Backward controller is missing.");
            var target = RequireDirectChild(
                RequireRoot(LayoutRootName).transform,
                TargetKey);
            var correction = FindPlanarMotionCorrection(sourceClip, target);
            correction = ValidateInPlaceCurveDelta(sourceClip, clip, correction);
            var evaluation = InspectMeshyInPlace(
                sourceClip,
                clip,
                controller,
                target,
                correction);
            var destination = Absolute(MeshyInPlaceFinalCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "The Meshy in-place final capture path has no directory."));

            CapturePhaseStrip(clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward in-place final capture changed scene dirty state.");
            }

            Debug.Log(
                "PlayerWalkBackward Meshy in-place final captured." +
                " Output=" + destination +
                ", ReviewPhases=0,0.25,0.5,0.75" +
                ", Duration=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", InPlacePlanarRange=" + Num(correction.InPlaceRange) +
                ", HipsEndPlanarDrift=" + Num(evaluation.RemainingPlanarDrift) +
                ", OnlyWorldPlanarCarrierAxesChanged=True" +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        public static void CaptureMeshyDirectFinal()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                                 ControllerPath) ??
                             throw new InvalidOperationException(
                                 "Player_Walk_Backward controller is missing.");
            var state = controller.layers[0].stateMachine.defaultState ??
                        throw new InvalidOperationException(
                            "Player_Walk_Backward default state is missing.");
            var clip = state.motion as AnimationClip ??
                       throw new InvalidOperationException(
                           "Player_Walk_Backward default Motion is not an AnimationClip.");
            var target = RequireDirectChild(
                RequireRoot(LayoutRootName).transform,
                TargetKey);
            var bindingCount = InspectMeshyDirect(clip, controller, target);
            var destination = Absolute(MeshyDirectFinalCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "The direct Meshy final capture path has no directory."));

            CapturePhaseStrip(clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct final capture changed scene dirty state.");
            }

            Debug.Log(
                "PlayerWalkBackward direct Meshy final captured." +
                " Output=" + destination +
                ", ReviewPhases=0,0.25,0.5,0.75" +
                ", Duration=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", CurveBindings=" + bindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceCurvesUnchanged=True" +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        public static void CaptureFinal()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException(
                           "Player_Walk_Backward clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                                 ControllerPath) ??
                             throw new InvalidOperationException(
                                 "Player_Walk_Backward controller is missing.");
            var target = RequireDirectChild(
                RequireRoot(LayoutRootName).transform,
                TargetKey);
            var armSwing = MeasureArmSwing(clip, target);
            var metrics = Inspect(clip, controller, armSwing);
            var destination = Absolute(FinalCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "The Player_Walk_Backward final capture path has no directory."));

            CapturePhaseStrip(clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward final capture changed the scene dirty state.");
            }

            Debug.Log(
                "PlayerWalkBackward Mixamo final captured." +
                " Output=" + destination +
                ", ReviewPhases=0,0.25,0.5,0.75" +
                ", Duration=" + Num(metrics.Duration) +
                ", RemainingPlanarDrift=" + Num(metrics.RemainingPlanarDrift) +
                ", RootPositionError=" + Num(metrics.RootPositionError) +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        private static AnimationClip CreateMeshyInPlaceClip(
            AnimationClip sourceClip,
            Transform target,
            out PlanarMotionCorrection correction)
        {
            correction = FindPlanarMotionCorrection(sourceClip, target);
            var bindingKeys = correction.BindingKeys;
            var generated = UnityEngine.Object.Instantiate(sourceClip);
            generated.name = "Player_Walk_Backward_Meshy_InPlace";
            foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip)
                         .Where(item => bindingKeys.Contains(
                             BindingKey(item),
                             StringComparer.Ordinal)))
            {
                var curve = AnimationUtility.GetEditorCurve(sourceClip, binding) ??
                            throw new InvalidOperationException(
                                "The Meshy planar carrier curve is unavailable: " +
                                BindingKey(binding) + ".");
                var startValue = curve.Evaluate(0f);
                var keys = curve.keys;
                for (var index = 0; index < keys.Length; index++)
                {
                    var key = keys[index];
                    key.value = startValue;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    keys[index] = key;
                }

                var inPlaceCurve = new AnimationCurve(keys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(generated, binding, inPlaceCurve);
            }

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                MeshyInPlaceClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, MeshyInPlaceClipPath);
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                existing.name = generated.name;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                MeshyInPlaceClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                           MeshyInPlaceClipPath) ??
                       throw new InvalidOperationException(
                           "Player_Walk_Backward Meshy in-place clip was not reloaded.");
            correction = ValidateInPlaceCurveDelta(sourceClip, clip, correction);
            return clip;
        }

        private static PlanarMotionCorrection FindPlanarMotionCorrection(
            AnimationClip sourceClip,
            Transform target)
        {
            var candidates = AnimationUtility.GetCurveBindings(sourceClip)
                .Where(IsPositionBinding)
                .GroupBy(CarrierKey, StringComparer.Ordinal)
                .Where(group => IsNamedCarrierBinding(group.First()))
                .Select(group => CreatePlanarCandidate(
                    sourceClip,
                    group.ToArray(),
                    target))
                .OrderBy(candidate => candidate.IsAnimatorCarrier ? 1 : 0)
                .ThenByDescending(candidate => candidate.SourceEndDelta)
                .ThenBy(candidate => candidate.Depth)
                .ThenBy(candidate => candidate.Carrier, StringComparer.Ordinal)
                .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    "The direct Meshy clip has no Armature, Hips, or Animator planar carrier.");
            }

            var transformCandidates = candidates
                .Where(candidate => !candidate.IsAnimatorCarrier)
                .ToArray();
            var preferred = transformCandidates.Length == 0
                ? candidates
                : transformCandidates;
            var moving = preferred
                .Where(candidate => candidate.SourceEndDelta > PositionTolerance)
                .ToArray();
            var selected = moving.Length == 0
                ? preferred.OrderByDescending(candidate => candidate.SourceRange).First()
                : moving[0];
            if (selected.SourceRange <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "The identified Meshy planar carrier does not move. Carrier=" +
                    selected.Carrier + ".");
            }

            return selected;
        }

        private static PlanarMotionCorrection CreatePlanarCandidate(
            AnimationClip clip,
            EditorCurveBinding[] bindings,
            Transform target)
        {
            var planarBindings = bindings
                .Where(binding => IsWorldPlanarPositionBinding(binding, target))
                .ToArray();
            if (planarBindings.Length != 2)
            {
                throw new InvalidOperationException(
                    "A Meshy carrier does not map exactly two position axes to the world plane: " +
                    CarrierKey(bindings[0]) + ".");
            }

            var curves = planarBindings
                .Select(binding => AnimationUtility.GetEditorCurve(clip, binding) ??
                                   throw new InvalidOperationException(
                                       "A Meshy world-planar carrier curve is unavailable."))
                .ToArray();
            var endDelta = Mathf.Sqrt(curves.Sum(curve =>
                Mathf.Pow(
                    curve.keys[curve.length - 1].value - curve.keys[0].value,
                    2f)));
            var range = Mathf.Sqrt(curves.Sum(curve =>
                Mathf.Pow(CurveRange(curve), 2f)));
            var path = planarBindings[0].path;
            var isAnimator = planarBindings[0].type == typeof(Animator);
            return new PlanarMotionCorrection(
                CarrierKey(planarBindings[0]),
                planarBindings.Select(BindingKey).ToArray(),
                endDelta,
                range,
                float.NaN,
                PathDepth(path),
                isAnimator);
        }

        private static PlanarMotionCorrection ValidateInPlaceCurveDelta(
            AnimationClip sourceClip,
            AnimationClip inPlaceClip,
            PlanarMotionCorrection correction)
        {
            var sourceBindings = AnimationUtility.GetCurveBindings(sourceClip)
                .OrderBy(BindingKey, StringComparer.Ordinal)
                .ToArray();
            var inPlaceBindings = AnimationUtility.GetCurveBindings(inPlaceClip)
                .OrderBy(BindingKey, StringComparer.Ordinal)
                .ToArray();
            RequireEqual(
                sourceBindings.Select(BindingKey).ToArray(),
                inPlaceBindings.Select(BindingKey).ToArray(),
                "The Meshy in-place clip curve binding set differs from the source.");
            var sourceSignatures = CurveSignatures(sourceClip)
                .ToDictionary(SignatureKey, value => value, StringComparer.Ordinal);
            var inPlaceSignatures = CurveSignatures(inPlaceClip)
                .ToDictionary(SignatureKey, value => value, StringComparer.Ordinal);
            foreach (var key in sourceSignatures.Keys)
            {
                if (correction.BindingKeys.Contains(key, StringComparer.Ordinal))
                {
                    continue;
                }

                if (!sourceSignatures[key].Equals(
                        inPlaceSignatures[key],
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A non-carrier Meshy curve changed: " + key + ".");
                }
            }

            RequireEqual(
                ObjectCurveSignatures(sourceClip),
                ObjectCurveSignatures(inPlaceClip),
                "The Meshy in-place object reference curves changed.");
            var inPlaceRanges = inPlaceBindings
                .Where(binding => correction.BindingKeys.Contains(
                    BindingKey(binding),
                    StringComparer.Ordinal))
                .Select(binding => CurveRange(
                    AnimationUtility.GetEditorCurve(inPlaceClip, binding) ??
                    throw new InvalidOperationException(
                        "The Meshy in-place carrier curve is unavailable.")))
                .ToArray();
            var inPlaceRange = inPlaceRanges.Length == 0
                ? float.PositiveInfinity
                : inPlaceRanges.Max();
            if (inPlaceRange > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "The Meshy in-place carrier still moves. Range=" +
                    Num(inPlaceRange) + ".");
            }

            return correction.WithInPlaceRange(inPlaceRange);
        }

        private static MotionEvaluation InspectMeshyInPlace(
            AnimationClip sourceClip,
            AnimationClip clip,
            AnimatorController controller,
            Transform target,
            PlanarMotionCorrection correction)
        {
            if (!AssetDatabase.GetAssetPath(clip).Equals(
                    MeshyInPlaceClipPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward is not using the Meshy in-place asset.");
            }

            var animator = target.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "Player_Walk_Backward Animator is missing.");
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward Animator configuration differs.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.name != StateName || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward in-place clip is not the default Motion.");
            }

            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward in-place clip is not looping.");
            }

            correction = ValidateInPlaceCurveDelta(sourceClip, clip, correction);
            var missingPaths = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => !string.IsNullOrEmpty(path) && target.Find(path) == null)
                .ToArray();
            if (missingPaths.Length != 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward in-place clip has incompatible paths: " +
                    string.Join(", ", missingPaths) + ".");
            }

            var evaluation = EvaluateMotion(clip, target);
            if (evaluation.RootPositionError > PositionTolerance ||
                evaluation.RemainingPlanarDrift > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward still travels in place validation. Root=" +
                    Num(evaluation.RootPositionError) + ", HipsEnd=" +
                    Num(evaluation.RemainingPlanarDrift) + ".");
            }

            if (evaluation.LeftFootTravel < MinimumFootTravel ||
                evaluation.RightFootTravel < MinimumFootTravel)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward lost alternating leg motion.");
            }

            return evaluation;
        }

        private static bool IsPositionBinding(EditorCurveBinding binding)
        {
            return PositionAxis(binding) != '\0';
        }

        private static bool IsNamedCarrierBinding(EditorCurveBinding binding)
        {
            return binding.type == typeof(Animator) ||
                   string.IsNullOrEmpty(binding.path) ||
                   BoneName(binding.path) == "Armature" ||
                   BoneName(binding.path) == "Hips";
        }

        private static bool IsWorldPlanarPositionBinding(
            EditorCurveBinding binding,
            Transform target)
        {
            var axis = PositionAxis(binding);
            if (axis == '\0')
            {
                return false;
            }

            if (binding.type == typeof(Animator))
            {
                return axis == 'x' || axis == 'z';
            }

            var carrier = string.IsNullOrEmpty(binding.path)
                ? target
                : target.Find(binding.path) ??
                  throw new InvalidOperationException(
                      "The Meshy carrier path is absent from Player_Walk_Backward: " +
                      binding.path + ".");
            var localAxis = axis == 'x'
                ? Vector3.right
                : axis == 'y'
                    ? Vector3.up
                    : Vector3.forward;
            var worldAxis = carrier.parent == null
                ? localAxis
                : carrier.parent.TransformVector(localAxis).normalized;
            return Mathf.Abs(Vector3.Dot(worldAxis, Vector3.up)) < 0.5f;
        }

        private static char PositionAxis(EditorCurveBinding binding)
        {
            if (binding.type == typeof(Transform))
            {
                if (binding.propertyName == "m_LocalPosition.x")
                {
                    return 'x';
                }

                if (binding.propertyName == "m_LocalPosition.z")
                {
                    return 'z';
                }

                if (binding.propertyName == "m_LocalPosition.y")
                {
                    return 'y';
                }
            }

            if (binding.type == typeof(Animator))
            {
                if (binding.propertyName == "RootT.x")
                {
                    return 'x';
                }

                if (binding.propertyName == "RootT.z")
                {
                    return 'z';
                }

                if (binding.propertyName == "RootT.y")
                {
                    return 'y';
                }
            }

            return '\0';
        }

        private static string CarrierKey(EditorCurveBinding binding)
        {
            var property = binding.propertyName;
            var separator = property.LastIndexOf('.');
            var prefix = separator < 0 ? property : property.Substring(0, separator);
            return binding.path + "|" + binding.type.FullName + "|" + prefix;
        }

        private static string BindingKey(EditorCurveBinding binding)
        {
            return binding.path + "|" + binding.type.FullName + "|" +
                   binding.propertyName;
        }

        private static string SignatureKey(string signature)
        {
            var separators = 0;
            for (var index = 0; index < signature.Length; index++)
            {
                if (signature[index] != '|')
                {
                    continue;
                }

                separators++;
                if (separators == 3)
                {
                    return signature.Substring(0, index);
                }
            }

            throw new InvalidOperationException(
                "A Meshy curve signature has no binding prefix.");
        }

        private static float CurveRange(AnimationCurve curve)
        {
            if (curve.length == 0)
            {
                return 0f;
            }

            var minimum = curve.keys.Min(key => key.value);
            var maximum = curve.keys.Max(key => key.value);
            return maximum - minimum;
        }

        private static string[] ObjectCurveSignatures(AnimationClip clip)
        {
            return AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .OrderBy(BindingKey, StringComparer.Ordinal)
                .Select(binding => BindingKey(binding) + "|" + string.Join(
                    ";",
                    (AnimationUtility.GetObjectReferenceCurve(clip, binding) ??
                     Array.Empty<ObjectReferenceKeyframe>())
                    .Select(key =>
                        Num(key.time) + "," +
                        (key.value == null
                            ? "<null>"
                            : AssetDatabase.GetAssetPath(key.value) + "#" + key.value.name))))
                .ToArray();
        }

        private static AnimationClip ConfigureAndLoadMeshyDirectClip(out string takeName)
        {
            var importer = AssetImporter.GetAtPath(MeshyDirectSourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Player_Walk_Backward direct Meshy FBX is not imported.");
            if (!importer.importAnimation ||
                importer.animationType != ModelImporterAnimationType.Generic)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct source must already use Generic animation import.");
            }

            var importedBefore = LoadSingleMeshyDirectClip();
            var curveSignaturesBefore = CurveSignatures(importedBefore);
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct source must contain exactly one take. Count=" +
                    (clips?.Length ?? 0).ToString(CultureInfo.InvariantCulture) + ".");
            }

            takeName = clips[0].takeName;
            if (string.IsNullOrEmpty(takeName) ||
                !takeName.EndsWith(MeshyDirectTakeSuffix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The only direct source take is not the identified backward Action. Take=" +
                    takeName + ".");
            }

            clips[0].loopTime = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var importedAfter = LoadSingleMeshyDirectClip();
            RequireEqual(
                curveSignaturesBefore,
                CurveSignatures(importedAfter),
                "Direct source animation curves changed while enabling loopTime.");
            return importedAfter;
        }

        private static AnimationClip LoadSingleMeshyDirectClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(MeshyDirectSourcePath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct imported clip count differs. Count=" +
                    clips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return clips[0];
        }

        private static AnimatorController ConnectMeshyDirectClip(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                                 ControllerPath) ??
                             throw new InvalidOperationException(
                                 "The existing Player_Walk_Backward controller is missing.");
            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward controller layer count differs.");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states.Select(item => item.state).ToArray();
            if (states.Length != 1 || states[0].name != StateName ||
                stateMachine.defaultState != states[0])
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward controller structure differs.");
            }

            states[0].motion = clip;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static int InspectMeshyDirect(
            AnimationClip clip,
            AnimatorController controller,
            Transform target)
        {
            if (!AssetDatabase.GetAssetPath(clip).Equals(
                    MeshyDirectSourcePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward is not using the direct FBX clip subasset.");
            }

            var animator = target.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "Player_Walk_Backward Animator is missing.");
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward Animator configuration differs.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.name != StateName || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct clip is not the controller default Motion.");
            }

            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct clip loopTime is disabled.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct clip contains no animation curves.");
            }

            var missingPaths = bindings
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => !string.IsNullOrEmpty(path) && target.Find(path) == null)
                .ToArray();
            if (missingPaths.Length != 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward direct clip has incompatible transform paths: " +
                    string.Join(", ", missingPaths) + ".");
            }

            return bindings.Length;
        }

        private static string[] CurveSignatures(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.type.FullName, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .Select(binding =>
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                                throw new InvalidOperationException(
                                    "Direct source curve could not be read: " +
                                    binding.path + "|" + binding.propertyName + ".");
                    return binding.path + "|" + binding.type.FullName + "|" +
                           binding.propertyName + "|" + string.Join(
                               ";",
                               curve.keys.Select(key =>
                                   Num(key.time) + "," +
                                   Num(key.value) + "," +
                                   Num(key.inTangent) + "," +
                                   Num(key.outTangent) + "," +
                                   Num(key.inWeight) + "," +
                                   Num(key.outWeight) + "," +
                                   ((int)key.weightedMode).ToString(
                                       CultureInfo.InvariantCulture)));
                })
                .ToArray();
        }

        private static AnimationClip ConfigureAndLoadSourceClip()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Player_Walk_Backward source FBX is not imported.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward source must contain exactly one take. Count=" +
                    (clips?.Length ?? 0).ToString(CultureInfo.InvariantCulture) + ".");
            }

            if (clips[0].takeName.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward source take is not Mixamo. Take=" +
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
                    "Player_Walk_Backward imported clip count differs. Count=" +
                    importedClips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return importedClips[0];
        }

        private static AnimationClip CreateAdjustedClip(
            AnimationClip sourceClip,
            Transform targetRig,
            out float removedPlanarTravel,
            out ArmSwingMetrics armSwing)
        {
            var generated = BakeToTargetRig(
                sourceClip,
                targetRig,
                out removedPlanarTravel,
                out armSwing);
            generated.name = GeneratedClipName;
            generated.legacy = false;
            generated.wrapMode = WrapMode.Loop;
            generated.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(generated);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(generated, settings);

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, ClipPath);
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                existing.name = GeneratedClipName;
                existing.legacy = false;
                existing.wrapMode = WrapMode.Loop;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                ClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException(
                       "Player_Walk_Backward clip was not reloaded after saving.");
        }

        private static AnimationClip BakeToTargetRig(
            AnimationClip sourceClip,
            Transform targetRig,
            out float removedPlanarTravel,
            out ArmSwingMetrics armSwing)
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) ??
                               throw new InvalidOperationException(
                                   "Player_Walk_Backward source model is missing.");
            var targetPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                                   targetRig.gameObject) ??
                               throw new InvalidOperationException(
                                   "Player_Walk_Backward target has no original prefab Rest Pose.");
            var sourceSample = UnityEngine.Object.Instantiate(sourcePrefab);
            var targetSample = UnityEngine.Object.Instantiate(targetPrefab);
            sourceSample.name = "PlayerWalkBackward_SourceSample";
            targetSample.name = "PlayerWalkBackward_TargetSample";
            sourceSample.hideFlags = HideFlags.HideAndDontSave;
            targetSample.hideFlags = HideFlags.HideAndDontSave;
            sourceSample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            targetSample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sourceSample.transform.localScale = Vector3.one;
            targetSample.transform.localScale = Vector3.one;

            try
            {
                DisableAnimators(sourceSample);
                DisableAnimators(targetSample);
                var animatedPaths = AnimationUtility.GetCurveBindings(sourceClip)
                    .Where(binding => binding.type == typeof(Transform))
                    .Select(binding => binding.path)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(PathDepth)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                if (animatedPaths.Length == 0)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Backward source has no Transform curves.");
                }

                var hierarchyPaths = animatedPaths
                    .SelectMany(PathAndAncestors)
                    .Append(string.Empty)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(PathDepth)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                var sourceTransforms = hierarchyPaths.ToDictionary(
                    path => path,
                    path => RequireTransformPath(sourceSample.transform, path),
                    StringComparer.Ordinal);
                var targetTransforms = hierarchyPaths.ToDictionary(
                    path => path,
                    path => RequireTransformPath(targetSample.transform, path),
                    StringComparer.Ordinal);
                var sourceRestWorld = sourceTransforms.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.localToWorldMatrix,
                    StringComparer.Ordinal);
                var targetRestWorld = targetTransforms.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.localToWorldMatrix,
                    StringComparer.Ordinal);
                var targetRestScale = targetTransforms.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.localScale,
                    StringComparer.Ordinal);

                var frameRate = sourceClip.frameRate;
                if (frameRate <= 0f)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Backward source frame rate is invalid.");
                }

                var frameCount = Mathf.RoundToInt(sourceClip.length * frameRate);
                if (frameCount < 1)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Backward source has no complete frame interval.");
                }

                var times = Enumerable.Range(0, frameCount + 1)
                    .Select(index => Mathf.Min(index / frameRate, sourceClip.length))
                    .ToArray();
                var positions = animatedPaths.ToDictionary(
                    path => path,
                    _ => new Vector3[times.Length],
                    StringComparer.Ordinal);
                var rotations = animatedPaths.ToDictionary(
                    path => path,
                    _ => new Quaternion[times.Length],
                    StringComparer.Ordinal);
                var scales = animatedPaths.ToDictionary(
                    path => path,
                    _ => new Vector3[times.Length],
                    StringComparer.Ordinal);

                for (var frame = 0; frame < times.Length; frame++)
                {
                    sourceClip.SampleAnimation(sourceSample, times[frame]);
                    var desiredWorld = new Dictionary<string, Matrix4x4>(
                        StringComparer.Ordinal)
                    {
                        [string.Empty] = targetRestWorld[string.Empty]
                    };
                    foreach (var path in hierarchyPaths)
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            continue;
                        }

                        var restAxisMap = sourceRestWorld[path].inverse *
                                          targetRestWorld[path];
                        var world = sourceTransforms[path].localToWorldMatrix *
                                    restAxisMap;
                        desiredWorld[path] = world;
                        if (!positions.TryGetValue(path, out var pathPositions))
                        {
                            continue;
                        }

                        var local = desiredWorld[ParentPath(path)].inverse * world;
                        pathPositions[frame] = local.GetPosition();
                        var rotation = local.rotation;
                        if (frame > 0 &&
                            Quaternion.Dot(rotations[path][frame - 1], rotation) < 0f)
                        {
                            rotation = Negate(rotation);
                        }

                        rotations[path][frame] = rotation;
                        scales[path][frame] = targetRestScale[path];
                    }
                }

                removedPlanarTravel = RemovePlanarRootTravel(
                    positions,
                    targetTransforms);
                armSwing = AttenuateArmSwing(rotations);
                var generated = new AnimationClip
                {
                    frameRate = frameRate,
                    legacy = false,
                    wrapMode = WrapMode.Loop
                };
                foreach (var path in animatedPaths)
                {
                    SetVector3Curves(
                        generated,
                        path,
                        "m_LocalPosition",
                        times,
                        positions[path]);
                    SetQuaternionCurves(generated, path, times, rotations[path]);
                    SetVector3Curves(
                        generated,
                        path,
                        "m_LocalScale",
                        times,
                        scales[path]);
                }

                return generated;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceSample);
                UnityEngine.Object.DestroyImmediate(targetSample);
            }
        }

        private static float RemovePlanarRootTravel(
            IReadOnlyDictionary<string, Vector3[]> positions,
            IReadOnlyDictionary<string, Transform> targetTransforms)
        {
            var carrierPaths = positions.Keys
                .Where(path =>
                    BoneName(path) == "Armature" ||
                    BoneName(path) == "Hips")
                .OrderBy(PathDepth)
                .ToArray();
            if (carrierPaths.Length == 0 ||
                carrierPaths.All(path => BoneName(path) != "Hips"))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward root motion carrier paths differ.");
            }

            var maximumRemoved = 0f;
            foreach (var path in carrierPaths)
            {
                var values = positions[path];
                var delta = values[values.Length - 1] - values[0];
                var carrier = targetTransforms[path];
                var worldDelta = carrier.parent == null
                    ? delta
                    : carrier.parent.TransformVector(delta);
                var planarWorldDelta = Vector3.ProjectOnPlane(
                    worldDelta,
                    Vector3.up);
                var planarLocalDelta = carrier.parent == null
                    ? planarWorldDelta
                    : carrier.parent.InverseTransformVector(planarWorldDelta);
                maximumRemoved = Mathf.Max(
                    maximumRemoved,
                    planarWorldDelta.magnitude);
                for (var index = 0; index < values.Length; index++)
                {
                    var phase = index / (float)(values.Length - 1);
                    values[index] -= planarLocalDelta * phase;
                }
            }

            return maximumRemoved;
        }

        private static Scene RecoverFailedApplyIfPresent(Scene scene)
        {
            if (!scene.isDirty)
            {
                return scene;
            }

            var target = RequireDirectChild(
                RequireRoot(LayoutRootName).transform,
                TargetKey);
            var animator = target.GetComponent<Animator>();
            if (animator == null ||
                AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) !=
                ControllerPath)
            {
                return scene;
            }

            var restored = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log(
                "PlayerWalkBackward restored CargoRunMvp after the previous failed apply.");
            return restored;
        }

        private static ArmSwingMetrics AttenuateArmSwing(
            IReadOnlyDictionary<string, Quaternion[]> rotations)
        {
            var ratios = new List<float>();
            var pathCount = 0;
            var sourceMaximum = 0f;
            var adjustedMaximum = 0f;
            foreach (var pair in rotations.Where(pair =>
                         ArmBoneNames.Contains(BoneName(pair.Key))))
            {
                pathCount++;
                var values = pair.Value;
                var mean = MeanQuaternion(values.Take(values.Length - 1));
                var sourceAmplitude = values.Max(value => Quaternion.Angle(mean, value));
                for (var index = 0; index < values.Length; index++)
                {
                    values[index] = Quaternion.SlerpUnclamped(
                        mean,
                        values[index],
                        ArmSwingScale);
                }

                var adjustedAmplitude = values.Max(value =>
                    Quaternion.Angle(mean, value));
                sourceMaximum = Mathf.Max(sourceMaximum, sourceAmplitude);
                adjustedMaximum = Mathf.Max(adjustedMaximum, adjustedAmplitude);
                if (sourceAmplitude > 0.0001f)
                {
                    ratios.Add(adjustedAmplitude / sourceAmplitude);
                }
            }

            if (pathCount < 6 || ratios.Count == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward source does not animate both arm chains.");
            }

            var minimumRatio = ratios.Min();
            var maximumRatio = ratios.Max();
            if (minimumRatio < 0.295f || maximumRatio > 0.305f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward arm swing ratio differs. Min=" +
                    Num(minimumRatio) + ", Max=" + Num(maximumRatio) + ".");
            }

            return new ArmSwingMetrics(
                pathCount,
                sourceMaximum,
                adjustedMaximum,
                minimumRatio,
                maximumRatio);
        }

        private static ArmSwingMetrics MeasureArmSwing(
            AnimationClip clip,
            Transform target)
        {
            var paths = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Where(path => ArmBoneNames.Contains(BoneName(path)))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new ArmSwingMetrics(
                paths.Length,
                0f,
                0f,
                ArmSwingScale,
                ArmSwingScale);
        }

        private static Quaternion MeanQuaternion(IEnumerable<Quaternion> source)
        {
            var values = source.ToArray();
            if (values.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward arm curve has no samples.");
            }

            var reference = values[0];
            var sum = Vector4.zero;
            foreach (var value in values)
            {
                var aligned = Quaternion.Dot(reference, value) < 0f
                    ? Negate(value)
                    : value;
                sum += new Vector4(aligned.x, aligned.y, aligned.z, aligned.w);
            }

            var magnitude = sum.magnitude;
            if (magnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward arm mean rotation is invalid.");
            }

            return new Quaternion(
                sum.x / magnitude,
                sum.y / magnitude,
                sum.z / magnitude,
                sum.w / magnitude);
        }

        private static AnimatorController RequireController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward controller layer count differs.");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states
                .Select(item => item.state)
                .ToArray();
            var state = states.SingleOrDefault(item => item.name == StateName);
            if (state == null)
            {
                if (states.Length != 0)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Backward controller contains an unexpected state.");
                }

                state = stateMachine.AddState(StateName);
            }

            state.motion = clip;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureAnimator(
            Transform target,
            AnimatorController controller)
        {
            var animators = target.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward contains multiple Animators.");
            }

            var animator = animators.Length == 1
                ? animators[0]
                : target.gameObject.AddComponent<Animator>();
            if (animator.transform != target)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward Animator is not on the instance root.");
            }

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
            AnimatorController controller,
            ArmSwingMetrics armSwing)
        {
            var target = RequireDirectChild(
                RequireRoot(LayoutRootName).transform,
                TargetKey);
            var animator = target.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "Player_Walk_Backward Animator is missing.");
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward Animator configuration differs.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.name != StateName || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward is not the controller default motion.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward clip is not looping.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward contains no animation curves.");
            }

            if (bindings.Any(binding =>
                    binding.type == typeof(Transform) &&
                    string.IsNullOrEmpty(binding.path)))
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward animates the scene object root.");
            }

            var missingPaths = bindings
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => target.Find(path) == null)
                .ToArray();
            if (missingPaths.Length != 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward has incompatible transform paths: " +
                    string.Join(", ", missingPaths) + ".");
            }

            if (armSwing.PathCount < 6 ||
                armSwing.MinimumRatio < 0.295f ||
                armSwing.MaximumRatio > 0.305f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward arm swing was not retained at 30 percent.");
            }

            var evaluation = EvaluateMotion(clip, target);
            if (evaluation.RootPositionError > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward root moved during sampled playback. Error=" +
                    Num(evaluation.RootPositionError) + ".");
            }

            if (evaluation.RemainingPlanarDrift > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward retains planar drift. Drift=" +
                    Num(evaluation.RemainingPlanarDrift) + ".");
            }

            if (evaluation.LeftFootTravel < MinimumFootTravel ||
                evaluation.RightFootTravel < MinimumFootTravel)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward does not contain alternating leg motion.");
            }

            if (evaluation.LoopPositionError > 0.003f ||
                evaluation.LoopRotationError > 0.75f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward loop seam differs. Position=" +
                    Num(evaluation.LoopPositionError) + ", Rotation=" +
                    Num(evaluation.LoopRotationError) + ".");
            }

            return new WalkMetrics(
                clip.length,
                clip.frameRate,
                evaluation.RemainingPlanarDrift,
                evaluation.RootPositionError,
                evaluation.LeftFootTravel,
                evaluation.RightFootTravel,
                evaluation.LoopPositionError,
                evaluation.LoopRotationError);
        }

        private static MotionEvaluation EvaluateMotion(
            AnimationClip clip,
            Transform target)
        {
            var sample = UnityEngine.Object.Instantiate(target.gameObject);
            sample.name = "PlayerWalkBackward_Evaluation";
            sample.hideFlags = HideFlags.HideAndDontSave;
            var animator = sample.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
            }

            try
            {
                var rootPosition = sample.transform.position;
                var hips = RequireNamedBone(sample.transform, "Hips");
                var leftFoot = RequireNamedBone(sample.transform, "LeftFoot");
                var rightFoot = RequireNamedBone(sample.transform, "RightFoot");
                var animatedPaths = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding => binding.type == typeof(Transform))
                    .Select(binding => binding.path)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                clip.SampleAnimation(sample, 0f);
                var startPoses = animatedPaths.ToDictionary(
                    path => path,
                    path => new LocalPose(sample.transform.Find(path)),
                    StringComparer.Ordinal);
                var startHips = hips.localPosition;
                var leftBounds = new Bounds(leftFoot.position, Vector3.zero);
                var rightBounds = new Bounds(rightFoot.position, Vector3.zero);
                var rootError = 0f;
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var time = Mathf.Min(frame / clip.frameRate, clip.length);
                    clip.SampleAnimation(sample, time);
                    leftBounds.Encapsulate(leftFoot.position);
                    rightBounds.Encapsulate(rightFoot.position);
                    rootError = Mathf.Max(
                        rootError,
                        Vector3.Distance(rootPosition, sample.transform.position));
                }

                var loopPosition = 0f;
                var loopRotation = 0f;
                foreach (var pair in startPoses)
                {
                    var transform = sample.transform.Find(pair.Key);
                    loopPosition = Mathf.Max(
                        loopPosition,
                        Vector3.Distance(pair.Value.Position, transform.localPosition));
                    loopRotation = Mathf.Max(
                        loopRotation,
                        Quaternion.Angle(pair.Value.Rotation, transform.localRotation));
                }

                var hipsDelta = hips.localPosition - startHips;
                return new MotionEvaluation(
                    new Vector2(hipsDelta.x, hipsDelta.z).magnitude,
                    rootError,
                    leftBounds.size.magnitude,
                    rightBounds.size.magnitude,
                    loopPosition,
                    loopRotation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static void CapturePhaseStrip(AnimationClip clip, string destination)
        {
            var scene = RequireScene();
            var target = RequireDirectChild(
                RequireRoot(LayoutRootName).transform,
                TargetKey);
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
                    var sample = UnityEngine.Object.Instantiate(target.gameObject);
                    sample.name = "PlayerWalkBackward_" +
                                  ReviewPhases[index].ToString(
                                      "0.00",
                                      CultureInfo.InvariantCulture);
                    sample.hideFlags = HideFlags.HideAndDontSave;
                    sample.transform.SetPositionAndRotation(
                        new Vector3(
                            (index - (ReviewPhases.Length - 1) * 0.5f) * spacing,
                            0f,
                            0f),
                        Quaternion.Euler(0f, 180f, 0f));
                    sample.transform.localScale = Vector3.one;
                    var animator = sample.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.enabled = false;
                        animator.runtimeAnimatorController = null;
                    }

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
                                      "No unlit shader is available for backward review guides.");
                guideMaterial = new Material(guideShader)
                {
                    name = "PlayerWalkBackwardReviewGuide",
                    color = new Color(0.15f, 0.85f, 1f, 1f),
                    hideFlags = HideFlags.HideAndDontSave
                };
                guides.Add(CreateGuide(
                    "PlayerWalkBackward_GroundGuide",
                    new Vector3(0f, sampleBounds.min.y - 0.012f, 0.22f),
                    new Vector3(spacing * ReviewPhases.Length + 0.8f, 0.008f, 0.04f),
                    guideMaterial));
                foreach (var sample in samples)
                {
                    guides.Add(CreateGuide(
                        sample.name + "_RootGuide",
                        new Vector3(sample.transform.position.x, sampleBounds.center.y, 0.22f),
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

                cameraObject = new GameObject("PlayerWalkBackwardReviewCamera", typeof(Camera));
                keyLightObject = new GameObject("PlayerWalkBackwardKeyLight", typeof(Light));
                fillLightObject = new GameObject("PlayerWalkBackwardFillLight", typeof(Light));
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
                var background = new Color(0.035f, 0.045f, 0.06f, 1f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.orthographic = true;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                var image = RenderPanel(
                    camera,
                    CaptureWidth,
                    CaptureHeight,
                    BoundsOf(visibleRenderers),
                    new Vector3(0.08f, 0.06f, -1f));
                try
                {
                    RequireVisibleCapture(image, background);
                    File.WriteAllBytes(destination, image.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }

                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "Player_Walk_Backward phase capture dirtied the scene.");
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

                foreach (var item in guides)
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }

                foreach (var sample in samples)
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
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
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
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                return image;
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

            if (visible / (float)sampled < 0.005f)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward capture contains no visible model.");
            }
        }

        private static void SetVector3Curves(
            AnimationClip clip,
            string path,
            string prefix,
            IReadOnlyList<float> times,
            IReadOnlyList<Vector3> values)
        {
            SetCurve(clip, path, prefix + ".x", times, values.Select(value => value.x));
            SetCurve(clip, path, prefix + ".y", times, values.Select(value => value.y));
            SetCurve(clip, path, prefix + ".z", times, values.Select(value => value.z));
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> values)
        {
            SetCurve(clip, path, "m_LocalRotation.x", times, values.Select(value => value.x));
            SetCurve(clip, path, "m_LocalRotation.y", times, values.Select(value => value.y));
            SetCurve(clip, path, "m_LocalRotation.z", times, values.Select(value => value.z));
            SetCurve(clip, path, "m_LocalRotation.w", times, values.Select(value => value.w));
        }

        private static void SetCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            IReadOnlyList<float> times,
            IEnumerable<float> sourceValues)
        {
            var values = sourceValues.ToArray();
            var curve = new AnimationCurve(
                times.Select((time, index) => new Keyframe(time, values[index])).ToArray())
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static void DisableAnimators(GameObject sample)
        {
            foreach (var animator in sample.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
            }
        }

        private static Transform RequireTransformPath(Transform root, string path)
        {
            var transform = string.IsNullOrEmpty(path) ? root : root.Find(path);
            return transform ?? throw new InvalidOperationException(
                "Player_Walk_Backward rig path differs: " + path + ".");
        }

        private static IEnumerable<string> PathAndAncestors(string path)
        {
            for (var current = path;
                 !string.IsNullOrEmpty(current);
                 current = ParentPath(current))
            {
                yield return current;
            }
        }

        private static string ParentPath(string path)
        {
            var separator = path.LastIndexOf('/');
            return separator < 0 ? string.Empty : path.Substring(0, separator);
        }

        private static string BoneName(string path)
        {
            var separator = path.LastIndexOf('/');
            return separator < 0 ? path : path.Substring(separator + 1);
        }

        private static int PathDepth(string path)
        {
            return string.IsNullOrEmpty(path)
                ? 0
                : path.Count(character => character == '/') + 1;
        }

        private static Quaternion Negate(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static Transform RequireNamedBone(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward bone count differs: " + name + ".");
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

        private static string[] RendererAssetStates(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(
                    renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        root),
                    StringComparer.Ordinal)
                .Select(renderer =>
                {
                    var path = AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        root);
                    var meshPath = string.Empty;
                    var skeleton = string.Empty;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        meshPath = AssetDatabase.GetAssetPath(skinned.sharedMesh);
                        skeleton = string.Join(
                            ",",
                            skinned.bones.Select(bone =>
                                bone == null
                                    ? "<null>"
                                    : AnimationUtility.CalculateTransformPath(bone, root)));
                    }
                    else
                    {
                        var meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                        }
                    }

                    return path + "|" + renderer.GetType().FullName + "|" +
                           renderer.enabled + "|Mesh=" + meshPath + "|Materials=" +
                           string.Join(
                               ",",
                               renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)) +
                           "|Skeleton=" + skeleton;
                })
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

        private static Bounds BoundsOf(IEnumerable<Renderer> renderers)
        {
            var array = renderers.Where(renderer => renderer != null).ToArray();
            if (array.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Backward review has no visible renderer.");
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
                    "CargoRunMvp must be the active scene. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static string Absolute(string path)
        {
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

        private readonly struct PlanarMotionCorrection
        {
            public PlanarMotionCorrection(
                string carrier,
                string[] bindingKeys,
                float sourceEndDelta,
                float sourceRange,
                float inPlaceRange,
                int depth,
                bool isAnimatorCarrier)
            {
                Carrier = carrier;
                BindingKeys = bindingKeys;
                SourceEndDelta = sourceEndDelta;
                SourceRange = sourceRange;
                InPlaceRange = inPlaceRange;
                Depth = depth;
                IsAnimatorCarrier = isAnimatorCarrier;
            }

            public string Carrier { get; }
            public string[] BindingKeys { get; }
            public float SourceEndDelta { get; }
            public float SourceRange { get; }
            public float InPlaceRange { get; }
            public int Depth { get; }
            public bool IsAnimatorCarrier { get; }

            public PlanarMotionCorrection WithInPlaceRange(float value)
            {
                return new PlanarMotionCorrection(
                    Carrier,
                    BindingKeys,
                    SourceEndDelta,
                    SourceRange,
                    value,
                    Depth,
                    IsAnimatorCarrier);
            }
        }

        private readonly struct LocalPose
        {
            public LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
        }

        private readonly struct ArmSwingMetrics
        {
            public ArmSwingMetrics(
                int pathCount,
                float sourceMaxDeviation,
                float adjustedMaxDeviation,
                float minimumRatio,
                float maximumRatio)
            {
                PathCount = pathCount;
                SourceMaxDeviation = sourceMaxDeviation;
                AdjustedMaxDeviation = adjustedMaxDeviation;
                MinimumRatio = minimumRatio;
                MaximumRatio = maximumRatio;
            }

            public int PathCount { get; }
            public float SourceMaxDeviation { get; }
            public float AdjustedMaxDeviation { get; }
            public float MinimumRatio { get; }
            public float MaximumRatio { get; }
        }

        private readonly struct MotionEvaluation
        {
            public MotionEvaluation(
                float remainingPlanarDrift,
                float rootPositionError,
                float leftFootTravel,
                float rightFootTravel,
                float loopPositionError,
                float loopRotationError)
            {
                RemainingPlanarDrift = remainingPlanarDrift;
                RootPositionError = rootPositionError;
                LeftFootTravel = leftFootTravel;
                RightFootTravel = rightFootTravel;
                LoopPositionError = loopPositionError;
                LoopRotationError = loopRotationError;
            }

            public float RemainingPlanarDrift { get; }
            public float RootPositionError { get; }
            public float LeftFootTravel { get; }
            public float RightFootTravel { get; }
            public float LoopPositionError { get; }
            public float LoopRotationError { get; }
        }

        private readonly struct WalkMetrics
        {
            public WalkMetrics(
                float duration,
                float frameRate,
                float remainingPlanarDrift,
                float rootPositionError,
                float leftFootTravel,
                float rightFootTravel,
                float loopPositionError,
                float loopRotationError)
            {
                Duration = duration;
                FrameRate = frameRate;
                RemainingPlanarDrift = remainingPlanarDrift;
                RootPositionError = rootPositionError;
                LeftFootTravel = leftFootTravel;
                RightFootTravel = rightFootTravel;
                LoopPositionError = loopPositionError;
                LoopRotationError = loopRotationError;
            }

            public float Duration { get; }
            public float FrameRate { get; }
            public float RemainingPlanarDrift { get; }
            public float RootPositionError { get; }
            public float LeftFootTravel { get; }
            public float RightFootTravel { get; }
            public float LoopPositionError { get; }
            public float LoopRotationError { get; }
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
