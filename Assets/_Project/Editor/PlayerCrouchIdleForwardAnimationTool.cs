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

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerCrouchIdleForwardAnimationTool
    {
        internal const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string LayoutRootName = "PlayerAnimationLayout";
        internal const string IdleTargetName = "Player_Crouch_Idle";
        internal const string ForwardTargetName = "Player_Crouch_Forward";
        internal const string IdleStateName = "PlayerCrouchIdle";
        internal const string ForwardStateName = "PlayerCrouchForward";
        internal const string EnterClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Enter_Mixamo_Corrected.anim";
        internal const string IdleClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Idle.anim";
        internal const string IdleControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Idle.controller";
        internal const string ForwardSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Forward_Mixamo.fbx";
        internal const string ForwardClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Forward_Mixamo_InPlace.anim";
        internal const string ForwardControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Forward.controller";
        internal const float IdleDurationSeconds = 0.5f;
        internal const string ExpectedForwardTakeName = "mixamo.com";

        private const string ExpectedForwardSourceHash =
            "2C525685A49FCD32D41693A7A2250B64F2674257C1C3CD6B751CCABCB85A3277";
        private const float CurveTolerance = 0.000001f;

        private sealed class CarrierSelection
        {
            internal EditorCurveBinding[] Bindings;
            internal string[] HorizontalProperties;
            internal string VerticalProperty;
        }

        private sealed class PoseSnapshot
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Idle From Enter")]
        internal static void ApplyIdleFromEnter()
        {
            Scene scene = RequireScene();
            RequireCleanScene(scene);
            Transform target = RequireTarget(scene, IdleTargetName);
            Vector3 positionBefore = target.position;
            Quaternion rotationBefore = target.rotation;
            Vector3 scaleBefore = target.localScale;
            Dictionary<string, string> otherAnimatorStates =
                CaptureOtherAnimatorStates(target);
            string enterHashBefore = HashFile(EnterClipPath);

            AnimationClip enterClip = LoadClip(EnterClipPath);
            VerifyAllTransformBindingsExist(enterClip, target);
            AnimationClip idleClip = CreateOrUpdateIdleClip(enterClip);
            VerifyIdlePoseMatchesEnter(enterClip, idleClip, target);
            AnimatorController controller = CreateOrUpdateController(
                IdleControllerPath,
                IdleStateName,
                idleClip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            AssertAnimator(animator, controller, idleClip);
            AssertRootUnchanged(target, positionBefore, rotationBefore, scaleBefore);
            RequireEqual(
                otherAnimatorStates,
                CaptureOtherAnimatorStates(target),
                "Another Player animation instance changed during crouch idle apply.");
            if (!string.Equals(
                    enterHashBefore,
                    HashFile(EnterClipPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter animation changed while deriving crouch idle.");
            }

            Debug.Log(
                "[PlayerCrouchIdle] Applied exact final held pose copied from Player_Crouch_Enter." +
                " SourceHoldSeconds=" + Num(IdleDurationSeconds) +
                ", IdleDuration=" + Num(idleClip.length) +
                ", Bindings=" + AnimationUtility.GetCurveBindings(idleClip).Length.ToString(
                    CultureInfo.InvariantCulture) +
                ", Loop=True, ApplyRootMotion=False, Speed=1, Mirror=False" +
                ", EnterClipUnchanged=True, OtherPlayersUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Forward Mixamo In Place")]
        internal static void ApplyForwardMixamoInPlace()
        {
            Scene scene = RequireScene();
            RequireCleanScene(scene);
            EnsureForwardSourceHash();
            ConfigureForwardImporter();

            Transform target = RequireTarget(scene, ForwardTargetName);
            Vector3 positionBefore = target.position;
            Quaternion rotationBefore = target.rotation;
            Vector3 scaleBefore = target.localScale;
            Dictionary<string, string> otherAnimatorStates =
                CaptureOtherAnimatorStates(target);
            AnimationClip sourceClip = LoadSingleForwardSourceClip();
            VerifyAllTransformBindingsExist(sourceClip, target);
            AnimationClip inPlaceClip = CreateOrUpdateForwardClip(
                sourceClip,
                target,
                out CarrierSelection carrier,
                out EditorCurveBinding[] changedBindings);
            AnimatorController controller = CreateOrUpdateController(
                ForwardControllerPath,
                ForwardStateName,
                inPlaceClip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            AssertAnimator(animator, controller, inPlaceClip);
            AssertRootUnchanged(target, positionBefore, rotationBefore, scaleBefore);
            RequireEqual(
                otherAnimatorStates,
                CaptureOtherAnimatorStates(target),
                "Another Player animation instance changed during crouch forward apply.");
            EnsureForwardSourceHash();

            Debug.Log(
                "[PlayerCrouchForward] Applied exact embedded Take '" +
                ExpectedForwardTakeName + "' as an in-place loop." +
                " Duration=" + Num(inPlaceClip.length) +
                ", FrameRate=" + Num(inPlaceClip.frameRate) +
                ", Carrier=" + carrier.Bindings[0].path +
                ", HorizontalAxes=" + string.Join(",", carrier.HorizontalProperties) +
                ", VerticalAxis=" + carrier.VerticalProperty +
                ", ChangedBindings=" + changedBindings.Length.ToString(
                    CultureInfo.InvariantCulture) +
                ", Loop=True, ApplyRootMotion=False, Speed=1, Mirror=False" +
                ", LimbPoseSpeedTimingChanged=False, OtherPlayersUnchanged=True.");
        }

        internal static Transform RequireTarget(Scene scene, string targetName)
        {
            Transform[] layoutRoots = scene.GetRootGameObjects()
                .Where(root => root.name == LayoutRootName)
                .Select(root => root.transform)
                .ToArray();
            if (layoutRoots.Length != 1)
            {
                throw new InvalidOperationException(
                    "PlayerAnimationLayout root count differs.");
            }

            Transform[] targets = Enumerable.Range(0, layoutRoots[0].childCount)
                .Select(layoutRoots[0].GetChild)
                .Where(child => child.name == targetName)
                .ToArray();
            if (targets.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + targetName + "; found " +
                    targets.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return targets[0];
        }

        internal static Transform FindUniqueBone(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    StripNamespace(item.name),
                    name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + name + " under " + root.name +
                    "; found " + matches.Length.ToString(
                        CultureInfo.InvariantCulture) + ".");
            }

            return matches[0];
        }

        internal static AnimationClip LoadIdleClip()
        {
            return LoadClip(IdleClipPath);
        }

        internal static AnimationClip LoadForwardClip()
        {
            return LoadClip(ForwardClipPath);
        }

        internal static Bounds CalculateRendererBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && !renderer.forceRenderingOff)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    target.name + " has no enabled renderer.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static AnimationClip CreateOrUpdateIdleClip(AnimationClip source)
        {
            if (source.length < IdleDurationSeconds - CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter does not contain the approved half-second final hold.");
            }

            float holdStart = source.length - IdleDurationSeconds;
            EditorCurveBinding[] sourceBindings =
                AnimationUtility.GetCurveBindings(source);
            EditorCurveBinding[] objectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            if (sourceBindings.Length == 0 || objectBindings.Length != 0)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter hold contains an unsupported curve structure.");
            }

            AnimationClip idle = UnityEngine.Object.Instantiate(source);
            idle.name = "Player_Crouch_Idle";
            idle.hideFlags = HideFlags.None;
            foreach (EditorCurveBinding binding in sourceBindings)
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(
                    source,
                    binding) ??
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter curve is missing: " +
                        binding.path + "/" + binding.propertyName + ".");
                float holdStartValue = sourceCurve.Evaluate(holdStart);
                float holdEndValue = sourceCurve.Evaluate(source.length);
                if (Mathf.Abs(holdStartValue - holdEndValue) > CurveTolerance)
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter final half-second is not a held pose: " +
                        binding.path + "/" + binding.propertyName + ".");
                }

                for (int sample = 1; sample < 15; sample++)
                {
                    float time = Mathf.Lerp(
                        holdStart,
                        source.length,
                        sample / 15f);
                    if (Mathf.Abs(
                            sourceCurve.Evaluate(time) - holdStartValue) >
                        CurveTolerance)
                    {
                        throw new InvalidOperationException(
                            "Player_Crouch_Enter final half-second moves between endpoints: " +
                        binding.path + "/" + binding.propertyName + ".");
                    }
                }

                AnimationCurve idleCurve = new AnimationCurve(
                    new Keyframe(0f, holdStartValue, 0f, 0f),
                    new Keyframe(
                        IdleDurationSeconds,
                        holdStartValue,
                        0f,
                        0f))
                {
                    preWrapMode = sourceCurve.preWrapMode,
                    postWrapMode = sourceCurve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(idle, binding, idleCurve);
            }

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(idle);
            settings.startTime = 0f;
            settings.stopTime = IdleDurationSeconds;
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(idle, settings);
            VerifyIdleClip(
                source,
                idle,
                holdStart,
                sourceBindings,
                objectBindings);
            return SaveClip(idle, IdleClipPath);
        }

        private static void VerifyIdleClip(
            AnimationClip source,
            AnimationClip idle,
            float holdStart,
            IReadOnlyCollection<EditorCurveBinding> sourceBindings,
            IReadOnlyCollection<EditorCurveBinding> sourceObjectBindings)
        {
            EditorCurveBinding[] idleBindings =
                AnimationUtility.GetCurveBindings(idle);
            if (!new HashSet<EditorCurveBinding>(sourceBindings)
                    .SetEquals(idleBindings) ||
                Mathf.Abs(idle.length - IdleDurationSeconds) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Idle binding set or duration differs from the copied hold.");
            }

            foreach (EditorCurveBinding binding in sourceBindings)
            {
                AnimationCurve sourceCurve =
                    AnimationUtility.GetEditorCurve(source, binding);
                AnimationCurve idleCurve =
                    AnimationUtility.GetEditorCurve(idle, binding);
                float expected = sourceCurve.Evaluate(holdStart);
                if (idleCurve == null || idleCurve.length != 2 ||
                    Mathf.Abs(idleCurve.keys[0].time) > CurveTolerance ||
                    Mathf.Abs(
                        idleCurve.keys[1].time - IdleDurationSeconds) >
                        CurveTolerance ||
                    Mathf.Abs(idleCurve.keys[0].value - expected) >
                        CurveTolerance ||
                    Mathf.Abs(idleCurve.keys[1].value - expected) >
                        CurveTolerance ||
                    CurveRange(idleCurve) > CurveTolerance)
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Idle does not hold the copied Enter value: " +
                        binding.path + "/" + binding.propertyName + ".");
                }
            }

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(idle);
            if (!settings.loopTime || settings.loopBlend ||
                Mathf.Abs(settings.startTime) > CurveTolerance ||
                Mathf.Abs(
                    settings.stopTime - IdleDurationSeconds) >
                    CurveTolerance ||
                !new HashSet<EditorCurveBinding>(sourceObjectBindings)
                    .SetEquals(
                        AnimationUtility.GetObjectReferenceCurveBindings(idle)))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Idle loop settings differ.");
            }
        }

        private static void VerifyIdlePoseMatchesEnter(
            AnimationClip enter,
            AnimationClip idle,
            Transform target)
        {
            PoseSnapshot enterPose = SamplePose(
                enter,
                target,
                enter.length - IdleDurationSeconds * 0.5f);
            PoseSnapshot idlePose = SamplePose(
                idle,
                target,
                idle.length * 0.5f);
            if (!enterPose.Positions.Keys.ToHashSet().SetEquals(
                    idlePose.Positions.Keys) ||
                !enterPose.Rotations.Keys.ToHashSet().SetEquals(
                    idlePose.Rotations.Keys))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Idle sampled bone set differs from Player_Crouch_Enter.");
            }

            float positionDifference = enterPose.Positions.Keys.Max(path =>
                Vector3.Distance(
                    enterPose.Positions[path],
                    idlePose.Positions[path]));
            float rotationDifference = enterPose.Rotations.Keys.Max(path =>
                Quaternion.Angle(
                    enterPose.Rotations[path],
                    idlePose.Rotations[path]));
            if (positionDifference > CurveTolerance ||
                rotationDifference > 0.001f)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Idle sampled pose differs from the Enter final hold." +
                    " Position=" + Num(positionDifference) +
                    ", Rotation=" + Num(rotationDifference) + ".");
            }

            Debug.Log(
                "[PlayerCrouchIdle] Direct rig sample matches Enter final hold." +
                " PositionDifference=" + Num(positionDifference) +
                ", RotationDifference=" + Num(rotationDifference) + ".");
        }

        private static PoseSnapshot SamplePose(
            AnimationClip clip,
            Transform target,
            float time)
        {
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Cannot sample crouch pose while another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(
                    target.gameObject,
                    clip,
                    time);
                AnimationMode.EndSampling();
                PoseSnapshot pose = new PoseSnapshot();
                foreach (Transform item in target.GetComponentsInChildren<Transform>(
                             true))
                {
                    string path = AnimationUtility.CalculateTransformPath(
                        item,
                        target);
                    pose.Positions[path] = item.localPosition;
                    pose.Rotations[path] = item.localRotation;
                }

                return pose;
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static void ConfigureForwardImporter()
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(ForwardSourcePath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward ModelImporter is missing.");
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1 ||
                !string.Equals(
                    clips[0].takeName,
                    ExpectedForwardTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The crouch walking FBX must expose exactly one Take named '" +
                    ExpectedForwardTakeName + "'.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            clips[0].name = clips[0].takeName;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadSingleForwardSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(
                    ForwardSourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    ExpectedForwardTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Unity does not expose exactly one crouch walking clip named '" +
                    ExpectedForwardTakeName + "'.");
            }

            return clips[0];
        }

        private static AnimationClip CreateOrUpdateForwardClip(
            AnimationClip source,
            Transform target,
            out CarrierSelection carrier,
            out EditorCurveBinding[] changedBindings)
        {
            AnimationClip clone = UnityEngine.Object.Instantiate(source);
            clone.name = "Player_Crouch_Forward_Mixamo_InPlace";
            clone.hideFlags = HideFlags.None;
            Dictionary<EditorCurveBinding, AnimationCurve> sourceCurves =
                AnimationUtility.GetCurveBindings(source)
                    .ToDictionary(
                        binding => binding,
                        binding => AnimationUtility.GetEditorCurve(
                            source,
                            binding));
            EditorCurveBinding[] sourceObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            carrier = SelectCarrier(source, target);
            HashSet<string> horizontalProperties = new HashSet<string>(
                carrier.HorizontalProperties,
                StringComparer.Ordinal);
            List<EditorCurveBinding> changed = new List<EditorCurveBinding>();

            foreach (EditorCurveBinding binding in carrier.Bindings)
            {
                if (!horizontalProperties.Contains(binding.propertyName))
                {
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    clone,
                    binding);
                if (curve == null || curve.length == 0 ||
                    CurveRange(curve) <= CurveTolerance)
                {
                    continue;
                }

                Keyframe[] keys = curve.keys;
                float lockedValue = keys[0].value;
                for (int index = 0; index < keys.Length; index++)
                {
                    Keyframe key = keys[index];
                    key.value = lockedValue;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    keys[index] = key;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clone, binding, curve);
                changed.Add(binding);
            }

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clone);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clone, settings);
            changedBindings = changed.ToArray();
            VerifyForwardClip(
                source,
                clone,
                sourceCurves,
                sourceObjectBindings,
                changedBindings,
                carrier);
            return SaveClip(clone, ForwardClipPath);
        }

        private static CarrierSelection SelectCarrier(
            AnimationClip source,
            Transform target)
        {
            var groups = AnimationUtility.GetCurveBindings(source)
                .Where(binding => binding.type == typeof(Transform) &&
                    IsPositionProperty(binding.propertyName))
                .GroupBy(binding => binding.path)
                .Select(group => new
                {
                    Bindings = group.ToArray(),
                    Transform = string.IsNullOrEmpty(group.Key)
                        ? target
                        : target.Find(group.Key)
                })
                .Where(item => item.Transform != null &&
                    string.Equals(
                        StripNamespace(item.Transform.name),
                        "Hips",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var moving = groups.Where(item => item.Bindings.Any(binding =>
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    source,
                    binding);
                return curve != null && CurveRange(curve) > CurveTolerance;
            })).ToArray();
            if (moving.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one animated Hips position carrier; found " +
                    moving.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            Transform parent = moving[0].Transform.parent;
            if (parent == null)
            {
                throw new InvalidOperationException(
                    "Hips has no parent for direct world-axis mapping.");
            }

            var axes = new[]
            {
                new
                {
                    Property = "m_LocalPosition.x",
                    Direction = parent.TransformDirection(Vector3.right).normalized
                },
                new
                {
                    Property = "m_LocalPosition.y",
                    Direction = parent.TransformDirection(Vector3.up).normalized
                },
                new
                {
                    Property = "m_LocalPosition.z",
                    Direction = parent.TransformDirection(Vector3.forward).normalized
                }
            };
            var vertical = axes
                .OrderByDescending(axis => Mathf.Abs(
                    Vector3.Dot(axis.Direction, Vector3.up)))
                .First();
            float verticalDot = Mathf.Abs(
                Vector3.Dot(vertical.Direction, Vector3.up));
            string[] horizontal = axes
                .Where(axis => axis.Property != vertical.Property)
                .Where(axis => Mathf.Abs(
                    Vector3.Dot(axis.Direction, Vector3.up)) < 0.1f)
                .Select(axis => axis.Property)
                .ToArray();
            string[] available = moving[0].Bindings
                .Select(binding => binding.propertyName)
                .ToArray();
            if (verticalDot < 0.9f || horizontal.Length != 2 ||
                horizontal.Any(property => !available.Contains(property)))
            {
                throw new InvalidOperationException(
                    "Hips axes are not clear enough for direct horizontal lock without inference.");
            }

            return new CarrierSelection
            {
                Bindings = moving[0].Bindings,
                HorizontalProperties = horizontal,
                VerticalProperty = vertical.Property
            };
        }

        private static void VerifyForwardClip(
            AnimationClip source,
            AnimationClip derived,
            IReadOnlyDictionary<EditorCurveBinding, AnimationCurve> sourceCurves,
            IReadOnlyCollection<EditorCurveBinding> sourceObjectBindings,
            IReadOnlyCollection<EditorCurveBinding> changedBindings,
            CarrierSelection carrier)
        {
            EditorCurveBinding[] derivedBindings =
                AnimationUtility.GetCurveBindings(derived);
            if (!new HashSet<EditorCurveBinding>(sourceCurves.Keys)
                    .SetEquals(derivedBindings) ||
                Mathf.Abs(source.length - derived.length) > CurveTolerance ||
                Mathf.Abs(source.frameRate - derived.frameRate) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Crouch forward derived clip structure or timing differs from source.");
            }

            foreach (KeyValuePair<EditorCurveBinding, AnimationCurve> pair in
                     sourceCurves)
            {
                AnimationCurve actual = AnimationUtility.GetEditorCurve(
                    derived,
                    pair.Key);
                if (changedBindings.Contains(pair.Key))
                {
                    AssertLockedCurve(pair.Value, actual, pair.Key);
                }
                else if (!CurvesEqual(pair.Value, actual))
                {
                    throw new InvalidOperationException(
                        "A crouch forward curve outside the approved Hips horizontal scope changed: " +
                        pair.Key.path + "/" + pair.Key.propertyName + ".");
                }
            }

            EditorCurveBinding[] derivedObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(derived);
            if (!new HashSet<EditorCurveBinding>(sourceObjectBindings)
                    .SetEquals(derivedObjectBindings) ||
                changedBindings.Any(binding =>
                    binding.path != carrier.Bindings[0].path ||
                    !carrier.HorizontalProperties.Contains(
                        binding.propertyName)))
            {
                throw new InvalidOperationException(
                    "Crouch forward derived clip changed outside the approved carrier scope.");
            }

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(derived);
            if (!settings.loopTime || settings.loopBlend)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward loop settings differ.");
            }
        }

        private static void AssertLockedCurve(
            AnimationCurve source,
            AnimationCurve actual,
            EditorCurveBinding binding)
        {
            if (source == null || actual == null ||
                source.length != actual.length ||
                CurveRange(actual) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Crouch forward horizontal carrier was not locked: " +
                    binding.path + "/" + binding.propertyName + ".");
            }

            float expected = source.keys[0].value;
            for (int index = 0; index < source.length; index++)
            {
                Keyframe before = source.keys[index];
                Keyframe after = actual.keys[index];
                if (Mathf.Abs(before.time - after.time) > CurveTolerance ||
                    Mathf.Abs(after.value - expected) > CurveTolerance ||
                    Mathf.Abs(after.inTangent) > CurveTolerance ||
                    Mathf.Abs(after.outTangent) > CurveTolerance ||
                    Mathf.Abs(before.inWeight - after.inWeight) >
                        CurveTolerance ||
                    Mathf.Abs(before.outWeight - after.outWeight) >
                        CurveTolerance ||
                    before.weightedMode != after.weightedMode)
                {
                    throw new InvalidOperationException(
                        "Crouch forward locked carrier key structure differs: " +
                        binding.path + "/" + binding.propertyName + ".");
                }
            }
        }

        private static AnimationClip SaveClip(
            AnimationClip generated,
            string path)
        {
            AnimationClip existing =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static AnimatorController CreateOrUpdateController(
            string path,
            string stateName,
            AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    stateName + " controller layer count differs.");
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = 1f;
            state.mirror = false;
            state.cycleOffset = 0f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Animator ConfigureAnimator(
            Transform target,
            RuntimeAnimatorController controller)
        {
            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = target.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void AssertAnimator(
            Animator animator,
            RuntimeAnimatorController controller,
            AnimationClip clip)
        {
            AnimationClip[] clips = controller.animationClips
                .Where(item => item != null)
                .Distinct()
                .ToArray();
            if (animator == null ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate ||
                clips.Length != 1 || clips[0] != clip)
            {
                throw new InvalidOperationException(
                    "Crouch Animator configuration differs.");
            }
        }

        private static void VerifyAllTransformBindingsExist(
            AnimationClip clip,
            Transform target)
        {
            string[] missing = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct()
                .Where(path => !string.IsNullOrEmpty(path) &&
                    target.Find(path) == null)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Direct crouch animation paths do not match " + target.name +
                    ". Retargeting is prohibited. Missing=" +
                    string.Join(",", missing) + ".");
            }
        }

        private static void EnsureForwardSourceHash()
        {
            string actual = HashFile(ForwardSourcePath);
            if (!string.Equals(
                    actual,
                    ExpectedForwardSourceHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Forward source FBX hash changed. Expected=" +
                    ExpectedForwardSourceHash + ", Actual=" + actual + ".");
            }
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            return clip ?? throw new FileNotFoundException(
                "Animation clip is missing.",
                Path.GetFullPath(path));
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for crouch animation apply.");
            }

            return scene;
        }

        private static void RequireCleanScene(Scene scene)
        {
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before crouch animation apply.");
            }
        }

        private static void AssertRootUnchanged(
            Transform target,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (Vector3.Distance(target.position, position) > CurveTolerance ||
                Quaternion.Angle(target.rotation, rotation) > CurveTolerance ||
                Vector3.Distance(target.localScale, scale) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    target.name + " root Transform changed during apply.");
            }
        }

        private static Dictionary<string, string> CaptureOtherAnimatorStates(
            Transform target)
        {
            Transform layoutRoot = target.parent ??
                                   throw new InvalidOperationException(
                                       target.name + " has no layout parent.");
            return Enumerable.Range(0, layoutRoot.childCount)
                .Select(layoutRoot.GetChild)
                .Where(child => child != target)
                .ToDictionary(
                    child => child.name,
                    child =>
                    {
                        Animator animator = child.GetComponent<Animator>();
                        return animator == null
                            ? "none"
                            : string.Join(
                                "|",
                                animator.enabled,
                                animator.applyRootMotion,
                                AssetDatabase.GetAssetPath(
                                    animator.runtimeAnimatorController));
                    },
                    StringComparer.Ordinal);
        }

        private static void RequireEqual(
            IReadOnlyDictionary<string, string> expected,
            IReadOnlyDictionary<string, string> actual,
            string message)
        {
            if (expected.Count != actual.Count ||
                expected.Any(pair =>
                    !actual.TryGetValue(pair.Key, out string value) ||
                    !string.Equals(
                        pair.Value,
                        value,
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static float CurveRange(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return 0f;
            }

            float min = curve.keys.Min(key => key.value);
            float max = curve.keys.Max(key => key.value);
            return max - min;
        }

        private static bool CurvesEqual(
            AnimationCurve first,
            AnimationCurve second)
        {
            if (first == null || second == null ||
                first.length != second.length ||
                first.preWrapMode != second.preWrapMode ||
                first.postWrapMode != second.postWrapMode)
            {
                return false;
            }

            for (int index = 0; index < first.length; index++)
            {
                Keyframe left = first.keys[index];
                Keyframe right = second.keys[index];
                if (Mathf.Abs(left.time - right.time) > CurveTolerance ||
                    Mathf.Abs(left.value - right.value) > CurveTolerance ||
                    Mathf.Abs(left.inTangent - right.inTangent) >
                        CurveTolerance ||
                    Mathf.Abs(left.outTangent - right.outTangent) >
                        CurveTolerance ||
                    Mathf.Abs(left.inWeight - right.inWeight) >
                        CurveTolerance ||
                    Mathf.Abs(left.outWeight - right.outWeight) >
                        CurveTolerance ||
                    left.weightedMode != right.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPositionProperty(string property)
        {
            return property == "m_LocalPosition.x" ||
                   property == "m_LocalPosition.y" ||
                   property == "m_LocalPosition.z";
        }

        private static string HashFile(string assetPath)
        {
            string absolutePath = Path.GetFullPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Required file is missing.",
                    absolutePath);
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolutePath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
            }
        }

        private static string StripNamespace(string value)
        {
            int separator = value.LastIndexOf(':');
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
