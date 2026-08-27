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
    internal static class PlayerCrouchEnterAnimationTool
    {
        internal const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string LayoutRootName = "PlayerAnimationLayout";
        internal const string TargetName = "Player_Crouch_Enter";
        internal const string StateName = "PlayerCrouchEnter";
        internal const string SourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Enter_Mixamo.fbx";
        internal const string CorrectedClipPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Enter_Mixamo_Corrected.anim";
        internal const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Crouch_Enter.controller";
        internal const string ExpectedTakeName = "mixamo.com";
        internal const string ExpectedSourceHash =
            "79D193061AAB7F47169EBA40D97F8BC3C1385850E5EF05DF254435F07988390F";
        internal const float HoldDurationSeconds = 0.5f;
        private const float CorrectionDegrees = 20f;
        private const float CorrectionStartNormalizedTime = 0.55f;
        private const float AxisProbeDegrees = 1f;
        private const float CurveTolerance = 0.000001f;

        private sealed class LegAdjustment
        {
            internal EditorCurveBinding Binding;
            internal float OffsetDegrees;
            internal float SourceKneeOutward;
            internal float CorrectedKneeOutward;
            internal float KneeInwardGainPerDegree;
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Enter Source Animation")]
        internal static void ApplySource()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Player_Crouch_Enter source apply.");
            }

            EnsureSourceHash();
            Transform target = RequireTarget(scene);
            Vector3 positionBefore = target.position;
            Quaternion rotationBefore = target.rotation;
            Vector3 scaleBefore = target.localScale;
            Dictionary<string, string> otherAnimatorStates =
                CaptureOtherAnimatorStates(target);

            ConfigureDirectSourceImporter();
            AnimationClip sourceClip = LoadSingleSourceClip();
            VerifyAllTransformBindingsExist(sourceClip, target);
            int leftRotationBindingCount = CountLeftLegRotationBindings(sourceClip, target);
            AnimatorController controller = ConfigureController(sourceClip);
            Animator animator = ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);

            AssertRootUnchanged(
                target,
                positionBefore,
                rotationBefore,
                scaleBefore);
            RequireEqual(
                otherAnimatorStates,
                CaptureOtherAnimatorStates(target),
                "Another Player animation instance changed.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp remained dirty after Player_Crouch_Enter source save.");
            }

            Debug.Log(
                "[PlayerCrouchEnter] Embedded source animation connected." +
                " Take=" + sourceClip.name +
                ", Duration=" + Num(sourceClip.length) +
                ", FrameRate=" + Num(sourceClip.frameRate) +
                ", Loop=True" +
                ", ApplyRootMotion=False" +
                ", SourceClipDirect=True" +
                ", Retargeting=False" +
                ", DerivedClip=False" +
                ", LeftLegRotationBindings=" +
                leftRotationBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", OtherPlayersUnchanged=True.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Enter Left Leg Correction")]
        internal static void ApplyCorrection()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Player_Crouch_Enter correction.");
            }

            EnsureSourceHash();
            Transform target = RequireTarget(scene);
            Vector3 positionBefore = target.position;
            Quaternion rotationBefore = target.rotation;
            Vector3 scaleBefore = target.localScale;
            Dictionary<string, string> otherAnimatorStates =
                CaptureOtherAnimatorStates(target);
            AnimationClip sourceClip = LoadSingleSourceClip();
            VerifyAllTransformBindingsExist(sourceClip, target);

            AnimationClip correctedClip = CreateOrUpdateCorrectedClip(
                sourceClip,
                target,
                out LegAdjustment adjustment);
            AnimatorController controller = ConfigureController(correctedClip);
            ConfigureAnimator(target, controller);
            EditorSceneManager.SaveScene(scene);

            AssertRootUnchanged(
                target,
                positionBefore,
                rotationBefore,
                scaleBefore);
            RequireEqual(
                otherAnimatorStates,
                CaptureOtherAnimatorStates(target),
                "Another Player animation instance changed.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp remained dirty after Player_Crouch_Enter correction save.");
            }

            Debug.Log(
                "[PlayerCrouchEnter] Left-leg correction applied." +
                " SourceTake=" + sourceClip.name +
                ", CorrectedClip=" + correctedClip.name +
                ", Binding=" + adjustment.Binding.path + "/" +
                adjustment.Binding.propertyName +
                ", OffsetDegrees=" + Num(adjustment.OffsetDegrees) +
                ", CorrectionStartNormalized=" +
                Num(CorrectionStartNormalizedTime) +
                ", SourceEndKneeOutward=" +
                Num(adjustment.SourceKneeOutward) +
                ", CorrectedEndKneeOutward=" +
                Num(adjustment.CorrectedKneeOutward) +
                ", KneeInwardGainPerDegree=" +
                Num(adjustment.KneeInwardGainPerDegree) +
                ", ChangedBindings=1" +
                ", LeftLegPositionCurvesChanged=False" +
                ", RightLegChanged=False" +
                ", SourceMotionTimingChanged=False" +
                ", FinalPoseHoldSeconds=" +
                Num(HoldDurationSeconds) +
                ", TotalDuration=" + Num(correctedClip.length) +
                ", ApplyRootMotion=False.");
        }

        [MenuItem("Bellerophon/Player/Apply Crouch Enter Half-Second Hold")]
        internal static void ApplyHalfSecondHold()
        {
            ApplyCorrection();
        }

        internal static Transform RequireTarget(Scene scene)
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
                .Where(child => child.name == TargetName)
                .ToArray();
            if (targets.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter instance count differs.");
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
                    "Player_Crouch_Enter bone count differs: " + name + ".");
            }

            return matches[0];
        }

        internal static AnimationClip LoadSingleSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The imported FBX does not expose exactly the investigated crouch Take.");
            }

            if (!string.Equals(
                    AssetDatabase.GetAssetPath(clips[0]),
                    SourcePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter source is not the direct FBX clip.");
            }

            return clips[0];
        }

        internal static AnimatorController ConfigureController(AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter controller layer count differs.");
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            AnimatorState state = stateMachine.AddState(StateName);
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

        internal static Animator ConfigureAnimator(
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
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter Animator configuration differs.");
            }

            return animator;
        }

        internal static void VerifyAllTransformBindingsExist(
            AnimationClip clip,
            Transform target)
        {
            string[] missing = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct()
                .Where(path => !string.IsNullOrEmpty(path) && target.Find(path) == null)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "The embedded crouch clip paths do not match Player_Crouch_Enter. " +
                    "Retargeting is prohibited. Missing=" +
                    string.Join(", ", missing) + ".");
            }
        }

        private static void EnsureSourceHash()
        {
            string absolutePath = Path.GetFullPath(SourcePath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Player_Crouch_Enter source FBX is missing.",
                    absolutePath);
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolutePath))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty);
                if (!string.Equals(
                        actual,
                        ExpectedSourceHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter source FBX hash differs. Actual=" +
                        actual + ".");
                }
            }
        }

        private static void ConfigureDirectSourceImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                                     throw new InvalidOperationException(
                                         "Player_Crouch_Enter Mixamo FBX is not imported.");
            if (!importer.importAnimation)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter Mixamo FBX has animation import disabled.");
            }

            ModelImporterClipAnimation[] defaultClips =
                importer.defaultClipAnimations ??
                Array.Empty<ModelImporterClipAnimation>();
            if (defaultClips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one embedded crouch Take; found " +
                    defaultClips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            ModelImporterClipAnimation sourceTake = defaultClips[0];
            if (!string.Equals(
                    sourceTake.takeName,
                    ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The embedded Take is not the investigated Mixamo Take. Actual=" +
                    sourceTake.takeName + ".");
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            sourceTake.name = sourceTake.takeName;
            sourceTake.loopTime = true;
            sourceTake.loopPose = false;
            importer.clipAnimations = new[] { sourceTake };
            importer.SaveAndReimport();
        }

        private static int CountLeftLegRotationBindings(
            AnimationClip clip,
            Transform target)
        {
            string[] jointNames = { "LeftUpLeg", "LeftLeg", "LeftFoot" };
            HashSet<string> paths = jointNames
                .Select(name => AnimationUtility.CalculateTransformPath(
                    FindUniqueBone(target, name),
                    target))
                .ToHashSet(StringComparer.Ordinal);
            int count = AnimationUtility.GetCurveBindings(clip)
                .Count(binding =>
                    binding.type == typeof(Transform) &&
                    paths.Contains(binding.path) &&
                    (binding.propertyName.StartsWith(
                         "localEulerAnglesRaw.",
                         StringComparison.Ordinal) ||
                     binding.propertyName.StartsWith(
                         "m_LocalRotation.",
                         StringComparison.Ordinal)));
            if (count == 0)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter has no left-leg rotation bindings.");
            }

            return count;
        }

        private static AnimationClip CreateOrUpdateCorrectedClip(
            AnimationClip source,
            Transform target,
            out LegAdjustment adjustment)
        {
            AnimationClip clone = UnityEngine.Object.Instantiate(source);
            clone.name = "Player_Crouch_Enter_Mixamo_Corrected";
            clone.hideFlags = HideFlags.None;
            float sourceDuration = source.length;

            Dictionary<EditorCurveBinding, AnimationCurve> sourceCurves =
                AnimationUtility.GetCurveBindings(source)
                    .ToDictionary(
                        binding => binding,
                        binding => AnimationUtility.GetEditorCurve(source, binding));
            EditorCurveBinding[] sourceObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(source);
            adjustment = SelectLeftUpperLegCorrectionAxis(clone, target);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clone,
                adjustment.Binding) ??
                throw new InvalidOperationException(
                    "Selected Player_Crouch_Enter correction curve is missing.");
            ApplyEndWeightedOffset(
                curve,
                sourceDuration,
                adjustment.OffsetDegrees);
            AnimationUtility.SetEditorCurve(clone, adjustment.Binding, curve);
            AppendFinalPoseHold(clone, sourceDuration);

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clone);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.stopTime =
                settings.startTime + sourceDuration + HoldDurationSeconds;
            AnimationUtility.SetAnimationClipSettings(clone, settings);
            adjustment.CorrectedKneeOutward = SampleEndKneeOutward(
                clone,
                target);

            VerifyCorrectedClip(
                source,
                clone,
                sourceCurves,
                sourceObjectBindings,
                adjustment);
            AnimationClip existing =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(CorrectedClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clone, CorrectedClipPath);
                existing = clone;
            }
            else
            {
                EditorUtility.CopySerialized(clone, existing);
                UnityEngine.Object.DestroyImmediate(clone);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static LegAdjustment SelectLeftUpperLegCorrectionAxis(
            AnimationClip clip,
            Transform target)
        {
            Transform leftUpLeg = FindUniqueBone(target, "LeftUpLeg");
            string path = AnimationUtility.CalculateTransformPath(
                leftUpLeg,
                target);
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, path, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith(
                        "localEulerAnglesRaw.",
                        StringComparison.Ordinal))
                .OrderBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
            if (bindings.Length != 3)
            {
                throw new InvalidOperationException(
                    "Expected three raw Euler curves for Player_Crouch_Enter LeftUpLeg; found " +
                    bindings.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            float sourceKneeOutward = SampleEndKneeOutward(clip, target);
            List<LegAdjustment> candidates = new List<LegAdjustment>();
            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve original = AnimationUtility.GetEditorCurve(
                    clip,
                    binding) ??
                    throw new InvalidOperationException(
                        "LeftUpLeg Euler curve is missing: " +
                        binding.propertyName + ".");
                foreach (float direction in new[] { -1f, 1f })
                {
                    AnimationCurve probe = CloneCurve(original);
                    ApplyEndWeightedOffset(
                        probe,
                        clip.length,
                        direction * AxisProbeDegrees);
                    try
                    {
                        AnimationUtility.SetEditorCurve(clip, binding, probe);
                        float adjustedKneeOutward = SampleEndKneeOutward(
                            clip,
                            target);
                        candidates.Add(new LegAdjustment
                        {
                            Binding = binding,
                            OffsetDegrees = direction * CorrectionDegrees,
                            SourceKneeOutward = sourceKneeOutward,
                            CorrectedKneeOutward = adjustedKneeOutward,
                            KneeInwardGainPerDegree =
                                sourceKneeOutward - adjustedKneeOutward
                        });
                    }
                    finally
                    {
                        AnimationUtility.SetEditorCurve(clip, binding, original);
                    }
                }
            }

            LegAdjustment selected = candidates
                .OrderByDescending(candidate => candidate.KneeInwardGainPerDegree)
                .First();
            if (selected.KneeInwardGainPerDegree <= CurveTolerance)
            {
                throw new InvalidOperationException(
                    "No directly probed LeftUpLeg Euler axis reduces the final crouch knee outward excess.");
            }

            LegAdjustment runnerUp = candidates
                .OrderByDescending(candidate => candidate.KneeInwardGainPerDegree)
                .Skip(1)
                .First();
            if (selected.KneeInwardGainPerDegree -
                runnerUp.KneeInwardGainPerDegree <= CurveTolerance)
            {
                throw new InvalidOperationException(
                    "The directly probed LeftUpLeg correction axis is ambiguous.");
            }

            Debug.Log(
                "[PlayerCrouchEnter] Direct LeftUpLeg axis probe selected " +
                selected.Binding.propertyName +
                " with offset " + Num(selected.OffsetDegrees) +
                " degrees and inward gain/degree " +
                Num(selected.KneeInwardGainPerDegree) + ".");
            return selected;
        }

        private static float SampleEndKneeOutward(
            AnimationClip clip,
            Transform target)
        {
            Transform hips = FindUniqueBone(target, "Hips");
            Transform leftUpLeg = FindUniqueBone(target, "LeftUpLeg");
            Transform leftLeg = FindUniqueBone(target, "LeftLeg");
            Transform leftFoot = FindUniqueBone(target, "LeftFoot");
            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException(
                    "Cannot sample Player_Crouch_Enter while another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(
                    target.gameObject,
                    clip,
                    clip.length * 0.999f);
                AnimationMode.EndSampling();
                return KneeOutwardExcess(
                    target,
                    hips,
                    leftUpLeg,
                    leftLeg,
                    leftFoot);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static float KneeOutwardExcess(
            Transform target,
            Transform hips,
            Transform upperLeg,
            Transform lowerLeg,
            Transform foot)
        {
            Vector3 lateral = target.right.normalized;
            float sideSign = Mathf.Sign(Vector3.Dot(
                upperLeg.position - hips.position,
                lateral));
            if (Mathf.Approximately(sideSign, 0f))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter left-leg side could not be determined directly.");
            }

            float thighLength = Vector3.Distance(
                upperLeg.position,
                lowerLeg.position);
            float shinLength = Vector3.Distance(
                lowerLeg.position,
                foot.position);
            float ratio = thighLength / Mathf.Max(
                thighLength + shinLength,
                0.000001f);
            float hipLateral = sideSign * Vector3.Dot(
                upperLeg.position - hips.position,
                lateral);
            float kneeLateral = sideSign * Vector3.Dot(
                lowerLeg.position - hips.position,
                lateral);
            float ankleLateral = sideSign * Vector3.Dot(
                foot.position - hips.position,
                lateral);
            return kneeLateral - Mathf.Lerp(
                hipLateral,
                ankleLateral,
                ratio);
        }

        private static void ApplyEndWeightedOffset(
            AnimationCurve curve,
            float clipLength,
            float offsetDegrees)
        {
            Keyframe[] keys = curve.keys;
            float startTime = clipLength * CorrectionStartNormalizedTime;
            for (int index = 0; index < keys.Length; index++)
            {
                Keyframe key = keys[index];
                float weight = Mathf.InverseLerp(
                    startTime,
                    clipLength,
                    key.time);
                weight = weight * weight * (3f - 2f * weight);
                key.value += offsetDegrees * weight;
                keys[index] = key;
            }

            curve.keys = keys;
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static void AppendFinalPoseHold(
            AnimationClip clip,
            float sourceDuration)
        {
            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter corrected clip has no curves to hold.");
            }

            foreach (EditorCurveBinding binding in bindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    clip,
                    binding) ??
                    throw new InvalidOperationException(
                        "Player_Crouch_Enter hold curve is missing: " +
                        binding.path + "/" + binding.propertyName + ".");
                AppendFinalPoseHold(curve, sourceDuration);
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static void VerifyCorrectedClip(
            AnimationClip source,
            AnimationClip corrected,
            IReadOnlyDictionary<EditorCurveBinding, AnimationCurve> sourceCurves,
            IReadOnlyCollection<EditorCurveBinding> sourceObjectBindings,
            LegAdjustment adjustment)
        {
            EditorCurveBinding[] correctedBindings =
                AnimationUtility.GetCurveBindings(corrected);
            if (!new HashSet<EditorCurveBinding>(sourceCurves.Keys)
                    .SetEquals(correctedBindings))
            {
                throw new InvalidOperationException(
                    "Corrected crouch clip binding set differs from the source.");
            }

            foreach (KeyValuePair<EditorCurveBinding, AnimationCurve> pair in sourceCurves)
            {
                AnimationCurve actual = AnimationUtility.GetEditorCurve(
                    corrected,
                    pair.Key);
                AnimationCurve expected = CloneCurve(pair.Value);
                if (pair.Key.Equals(adjustment.Binding))
                {
                    ApplyEndWeightedOffset(
                        expected,
                        source.length,
                        adjustment.OffsetDegrees);
                }

                AppendFinalPoseHold(expected, source.length);
                if (!CurvesEqual(expected, actual))
                {
                    throw new InvalidOperationException(
                        "A corrected crouch curve differs from the approved correction plus final hold: " +
                        pair.Key.path + "/" + pair.Key.propertyName + ".");
                }
            }

            float expectedDuration = source.length + HoldDurationSeconds;
            if (Mathf.Abs(corrected.length - expectedDuration) > CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter corrected clip does not contain the exact half-second hold." +
                    " Expected=" + Num(expectedDuration) +
                    ", Actual=" + Num(corrected.length) + ".");
            }

            EditorCurveBinding[] correctedObjectBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(corrected);
            if (!new HashSet<EditorCurveBinding>(sourceObjectBindings)
                    .SetEquals(correctedObjectBindings))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter object-reference bindings changed.");
            }

            if (!adjustment.Binding.path.EndsWith(
                    "LeftUpLeg",
                    StringComparison.Ordinal) ||
                !adjustment.Binding.propertyName.StartsWith(
                    "localEulerAnglesRaw.",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter correction left the approved LeftUpLeg Euler scope.");
            }
        }

        private static void AppendFinalPoseHold(
            AnimationCurve curve,
            float sourceDuration)
        {
            if (curve == null || curve.length == 0)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter expected hold curve is empty.");
            }

            Keyframe[] keys = curve.keys;
            int lastIndex = keys.Length - 1;
            if (keys[lastIndex].time > sourceDuration + CurveTolerance)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter source curve extends beyond the investigated Take duration.");
            }

            float finalValue = curve.Evaluate(sourceDuration);
            Keyframe lastSourceKey = keys[lastIndex];
            lastSourceKey.outTangent = 0f;
            keys[lastIndex] = lastSourceKey;

            Keyframe finalPose;
            if (Mathf.Abs(lastSourceKey.time - sourceDuration) <= CurveTolerance)
            {
                finalPose = lastSourceKey;
            }
            else
            {
                finalPose = lastSourceKey;
                finalPose.time = sourceDuration;
                finalPose.value = finalValue;
                finalPose.inTangent = 0f;
                finalPose.outTangent = 0f;
                Array.Resize(ref keys, keys.Length + 1);
                keys[keys.Length - 1] = finalPose;
            }

            Keyframe holdEnd = finalPose;
            holdEnd.time = sourceDuration + HoldDurationSeconds;
            holdEnd.inTangent = 0f;
            holdEnd.outTangent = 0f;
            Array.Resize(ref keys, keys.Length + 1);
            keys[keys.Length - 1] = holdEnd;
            curve.keys = keys;
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
                    Mathf.Abs(left.inTangent - right.inTangent) > CurveTolerance ||
                    Mathf.Abs(left.outTangent - right.outTangent) > CurveTolerance ||
                    Mathf.Abs(left.inWeight - right.inWeight) > CurveTolerance ||
                    Mathf.Abs(left.outWeight - right.outWeight) > CurveTolerance ||
                    left.weightedMode != right.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertRootUnchanged(
            Transform target,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (Vector3.Distance(target.position, position) > 0.000001f ||
                Quaternion.Angle(target.rotation, rotation) > 0.000001f ||
                Vector3.Distance(target.localScale, scale) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Player_Crouch_Enter root Transform changed during apply.");
            }
        }

        private static Dictionary<string, string> CaptureOtherAnimatorStates(
            Transform target)
        {
            Transform layoutRoot = target.parent ??
                                   throw new InvalidOperationException(
                                       "Player_Crouch_Enter has no layout parent.");
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
                    !string.Equals(pair.Value, value, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string StripNamespace(string value)
        {
            int separator = value.LastIndexOf(':');
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for Player_Crouch_Enter apply.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
