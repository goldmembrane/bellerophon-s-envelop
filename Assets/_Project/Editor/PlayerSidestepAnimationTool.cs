using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PlayerSidestepAnimationTool
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "Player_Sidestep";
        internal const string SourceFbxPath = "Assets/_Project/Art/Player/Animations/Player_Sidestep_Mixamo.fbx";
        internal const string InPlaceClipPath = "Assets/_Project/Art/Player/Animations/Player_Sidestep_Mixamo_InPlace.anim";
        internal const string ControllerPath = "Assets/_Project/Art/Player/Animations/Player_Sidestep.controller";
        internal const string StateName = "PlayerSidestep";
        internal const string FinalCapturePath = "docs/validation/player_sidestep_mixamo_in_place_final.png";

        private const string ExpectedSourceHash = "189353927B503D31BCDBDD71DA1BF828D0090E4400345C5730F283DEDA18D528";
        private const string ExpectedTakeName = "mixamo.com";
        private const float CurveTolerance = 0.000001f;
        private const float LeftArmClearanceDegrees = 18f;
        private const float RightArmClearanceDegrees = 18f;
        private const float LeftShoulderClearanceDegrees = 6f;
        private const float RightShoulderClearanceDegrees = 6f;
        private const float ArmAxisProbeDegrees = 1f;

        private sealed class CarrierSelection
        {
            internal EditorCurveBinding[] Bindings;
            internal string[] HorizontalProperties;
            internal string VerticalProperty;
        }

        private sealed class ArmClearanceAdjustment
        {
            internal string Side;
            internal string Joint;
            internal EditorCurveBinding Binding;
            internal float OffsetDegrees;
            internal float MeanLateralGainPerDegree;
            internal float MinimumLateralGainPerDegree;
        }

        [MenuItem("Bellerophon/Player/Apply Sidestep Mixamo In Place")]
        internal static void Apply()
        {
            RequireCleanScene();
            EnsureSourceHash();
            ConfigureSourceImporter();

            AnimationClip sourceClip = LoadSingleSourceClip();
            Scene scene = OpenCargoRunScene();
            GameObject target = FindUniqueTarget(scene);
            string otherObjectsBefore = CaptureOtherObjects(scene, target);
            string rendererAssetsBefore = CaptureRendererAssets(target);
            string prefabPathBefore = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            Vector3 targetPositionBefore = target.transform.position;
            Quaternion targetRotationBefore = target.transform.rotation;
            Vector3 targetScaleBefore = target.transform.localScale;

            VerifyAllTransformBindingsExist(sourceClip, target);
            AnimationClip inPlaceClip = CreateOrUpdateInPlaceClip(
                sourceClip,
                target,
                out CarrierSelection carrier,
                out ArmClearanceAdjustment[] armAdjustments);
            AnimatorController controller = CreateOrUpdateController(inPlaceClip);
            Animator animator = ConfigureAnimator(target, controller);

            AssertAnimatorConfiguration(animator, controller, inPlaceClip);
            AssertUnchanged(target.transform.position, targetPositionBefore, "Player_Sidestep world position");
            AssertUnchanged(target.transform.rotation, targetRotationBefore, "Player_Sidestep world rotation");
            AssertUnchanged(target.transform.localScale, targetScaleBefore, "Player_Sidestep local scale");
            AssertEqual(rendererAssetsBefore, CaptureRendererAssets(target), "model/skin/material asset connection");
            AssertEqual(prefabPathBefore, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target), "prefab asset path");
            AssertEqual(otherObjectsBefore, CaptureOtherObjects(scene, target), "objects outside Player_Sidestep");

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[PlayerSidestep] Applied exact Take '{ExpectedTakeName}' through state '{StateName}'. " +
                $"Loop=True, ApplyRootMotion=False, Mirror=False, Speed=1. " +
                $"In-place carrier={carrier.Bindings[0].path}, horizontal axes=" +
                $"{string.Join(",", carrier.HorizontalProperties)}, vertical axis={carrier.VerticalProperty}. " +
                $"Arm clearance={string.Join("; ", armAdjustments.Select(item => $"{item.Joint}:{item.Binding.propertyName}:{item.OffsetDegrees:R}deg"))}. " +
                "Only the directly selected bilateral shoulder/upper-arm Euler curves were offset; all other rotation, joint, limb, " +
                "speed, timing, retarget, and direction curves were preserved.");
        }

        [MenuItem("Bellerophon/Player/Capture Sidestep Mixamo In Place Final")]
        internal static void CaptureFinal()
        {
            RequireCleanScene();
            const string metricsPath = "docs/validation/player_sidestep_mixamo_in_place_review_metrics.json";
            string absoluteMetricsPath = Path.GetFullPath(metricsPath);
            if (!File.Exists(absoluteMetricsPath) ||
                !File.ReadAllText(absoluteMetricsPath).Contains("\"passedNumericChecks\": true"))
            {
                throw new InvalidOperationException("The two-loop Play Mode review must pass before final capture composition.");
            }

            int[] phaseFrames = { 0, 8, 15, 23 };
            Texture2D[] phaseTextures = new Texture2D[phaseFrames.Length];
            Texture2D strip = null;
            try
            {
                int sourceWidth = 0;
                int sourceHeight = 0;
                for (int i = 0; i < phaseFrames.Length; i++)
                {
                    string framePath = Path.GetFullPath(
                        $"Logs/PlayerSidestepPlayModeReviewFrames/frame_{phaseFrames[i]:000}.png");
                    if (!File.Exists(framePath))
                    {
                        throw new FileNotFoundException("A validated Play Mode review phase frame is missing.", framePath);
                    }

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(framePath), false))
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                        throw new InvalidOperationException($"Could not load validated phase frame: {framePath}");
                    }

                    phaseTextures[i] = texture;
                    sourceWidth = sourceWidth == 0 ? texture.width : sourceWidth;
                    sourceHeight = sourceHeight == 0 ? texture.height : sourceHeight;
                    if (texture.width != sourceWidth || texture.height != sourceHeight || texture.width % 2 != 0)
                    {
                        throw new InvalidOperationException("Validated review phase frames do not share the expected composite dimensions.");
                    }
                }

                int panelWidth = sourceWidth / 2;
                strip = new Texture2D(panelWidth * phaseFrames.Length, sourceHeight, TextureFormat.RGB24, false);
                for (int i = 0; i < phaseTextures.Length; i++)
                {
                    Color[] pixels = phaseTextures[i].GetPixels(panelWidth, 0, panelWidth, sourceHeight);
                    strip.SetPixels(panelWidth * i, 0, panelWidth, sourceHeight, pixels);
                }

                strip.Apply(false, false);
                string absolutePath = Path.GetFullPath(FinalCapturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? throw new InvalidOperationException());
                File.WriteAllBytes(absolutePath, strip.EncodeToPNG());
                Debug.Log(
                    $"[PlayerSidestep] Final four-phase strip composed from validated Play Mode frames " +
                    $"{string.Join(",", phaseFrames)}: {absolutePath}");
            }
            finally
            {
                foreach (Texture2D phaseTexture in phaseTextures)
                {
                    if (phaseTexture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(phaseTexture);
                    }
                }

                if (strip != null)
                {
                    UnityEngine.Object.DestroyImmediate(strip);
                }
            }
        }

        internal static Scene OpenCargoRunScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !string.Equals(active.path, ScenePath, StringComparison.Ordinal))
            {
                active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            return active;
        }

        internal static GameObject FindUniqueTarget(Scene scene)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => string.Equals(item.name, TargetName, StringComparison.Ordinal))
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one {TargetName}; found {matches.Length}.");
            }

            return matches[0];
        }

        internal static AnimationClip LoadInPlaceClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlaceClipPath);
            if (clip == null)
            {
                throw new InvalidOperationException($"In-place clip is missing: {InPlaceClipPath}");
            }

            return clip;
        }

        internal static Transform FindHips(GameObject root)
        {
            Transform[] hips = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(StripNamespace(item.name), "Hips", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hips.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one Hips under {root.name}; found {hips.Length}.");
            }

            return hips[0];
        }

        internal static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && !item.forceRenderingOff)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"No enabled renderer found under {root.name}.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        internal static void FrameCamera(Camera camera, Transform target, Bounds bounds, float padding)
        {
            Vector3 up = target.up.normalized;
            Vector3 forward = target.forward.normalized;
            camera.transform.rotation = Quaternion.LookRotation(-forward, up);
            camera.transform.position = bounds.center + forward * Math.Max(5f, bounds.size.magnitude * 2f);
            camera.orthographic = true;
            camera.orthographicSize = Math.Max(0.1f, bounds.extents.y * padding);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Math.Max(20f, bounds.size.magnitude * 5f);
        }

        internal static Camera CreatePreviewCamera(Scene scene)
        {
            GameObject cameraObject = new GameObject("PlayerSidestepPreviewCamera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = true;
            return camera;
        }

        internal static Light CreatePreviewLight(Scene scene, Transform target)
        {
            GameObject lightObject = new GameObject("PlayerSidestepPreviewLight", typeof(Light));
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.transform.rotation = Quaternion.LookRotation(-target.forward - target.up * 0.65f, target.up);
            return light;
        }

        private static void RequireCleanScene()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                throw new InvalidOperationException("The active scene has unsaved changes. Save or discard them before applying Player_Sidestep.");
            }
        }

        private static void EnsureSourceHash()
        {
            string absolutePath = Path.GetFullPath(SourceFbxPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Player sidestep source FBX is missing.", absolutePath);
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolutePath))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                if (!string.Equals(actual, ExpectedSourceHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Source FBX hash changed. Expected {ExpectedSourceHash}, actual {actual}.");
                }
            }
        }

        private static void ConfigureSourceImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(SourceFbxPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"ModelImporter was not found for {SourceFbxPath}.");
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException($"Expected one embedded default Take; found {clips?.Length ?? 0}.");
            }

            if (!string.Equals(clips[0].takeName, ExpectedTakeName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Expected Take '{ExpectedTakeName}', found '{clips[0].takeName}'.");
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadSingleSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourceFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one imported animation clip; found {clips.Length}.");
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime || settings.loopBlend)
            {
                throw new InvalidOperationException("Source clip must use Loop Time without Loop Pose blending.");
            }

            return clips[0];
        }

        private static void VerifyAllTransformBindingsExist(AnimationClip sourceClip, GameObject target)
        {
            string[] missing = AnimationUtility.GetCurveBindings(sourceClip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct()
                .Where(path => !string.IsNullOrEmpty(path) && target.transform.Find(path) == null)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "Direct source paths do not match Player_Sidestep. Retargeting is prohibited. Missing: " +
                    string.Join(", ", missing));
            }
        }

        private static AnimationClip CreateOrUpdateInPlaceClip(
            AnimationClip source,
            GameObject target,
            out CarrierSelection carrier,
            out ArmClearanceAdjustment[] armAdjustments)
        {
            AnimationClip clone = UnityEngine.Object.Instantiate(source);
            clone.name = "Player_Sidestep_Mixamo_InPlace";
            clone.hideFlags = HideFlags.None;

            Dictionary<EditorCurveBinding, AnimationCurve> sourceCurves = AnimationUtility.GetCurveBindings(source)
                .ToDictionary(binding => binding, binding => AnimationUtility.GetEditorCurve(source, binding));
            EditorCurveBinding[] sourceObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);

            carrier = SelectCarrier(source, target);
            HashSet<string> horizontalProperties = new HashSet<string>(carrier.HorizontalProperties, StringComparer.Ordinal);
            HashSet<EditorCurveBinding> changedBindings = new HashSet<EditorCurveBinding>();

            foreach (EditorCurveBinding binding in carrier.Bindings)
            {
                if (!horizontalProperties.Contains(binding.propertyName))
                {
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clone, binding);
                if (curve == null || curve.length == 0 || CurveRange(curve) <= CurveTolerance)
                {
                    continue;
                }

                Keyframe[] keys = curve.keys;
                float lockedValue = keys[0].value;
                for (int i = 0; i < keys.Length; i++)
                {
                    Keyframe key = keys[i];
                    key.value = lockedValue;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    keys[i] = key;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clone, binding, curve);
                changedBindings.Add(binding);
            }

            if (changedBindings.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(clone);
                throw new InvalidOperationException("The selected world-horizontal carrier axes contained no motion to remove.");
            }

            armAdjustments = ApplyArmClearance(clone, target);

            AnimationClipSettings cloneSettings = AnimationUtility.GetAnimationClipSettings(clone);
            cloneSettings.loopTime = true;
            cloneSettings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clone, cloneSettings);

            VerifyDerivedClip(
                source,
                clone,
                sourceCurves,
                sourceObjectBindings,
                changedBindings,
                carrier,
                armAdjustments);

            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlaceClipPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clone, InPlaceClipPath);
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

        private static ArmClearanceAdjustment[] ApplyArmClearance(AnimationClip clip, GameObject target)
        {
            string[] sides = { "Left", "Right" };
            List<ArmClearanceAdjustment> adjustments = new List<ArmClearanceAdjustment>();

            // Preserve the established upper-arm correction first, then add only the small
            // shoulder clearance required to keep the bent forearm outside the torso silhouette.
            foreach (string side in sides)
            {
                adjustments.Add(ApplyJointClearance(
                    clip,
                    target,
                    side,
                    side + "Arm",
                    ArmClearanceDegreesForSide(side)));
            }

            foreach (string side in sides)
            {
                adjustments.Add(ApplyJointClearance(
                    clip,
                    target,
                    side,
                    side + "Shoulder",
                    ShoulderClearanceDegreesForSide(side)));
            }

            if (adjustments.Count != 4 || adjustments.Select(item => item.Binding.path).Distinct().Count() != 4)
            {
                throw new InvalidOperationException("Exactly one shoulder and one upper-arm Euler curve per side must be adjusted.");
            }

            return adjustments.ToArray();
        }

        private static ArmClearanceAdjustment ApplyJointClearance(
            AnimationClip clip,
            GameObject target,
            string side,
            string jointName,
            float clearanceDegrees)
        {
            Transform joint = FindUniqueBone(target, jointName);
            string path = AnimationUtility.CalculateTransformPath(joint, target.transform);
            EditorCurveBinding[] rotationBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform) &&
                    string.Equals(binding.path, path, StringComparison.Ordinal) &&
                    IsRawEulerProperty(binding.propertyName))
                .OrderBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
            if (rotationBindings.Length != 3)
            {
                throw new InvalidOperationException(
                    $"Expected three raw Euler curves for {path}; found {rotationBindings.Length}.");
            }

            ArmClearanceAdjustment adjustment = SelectArmClearanceAxis(
                clip,
                target,
                side,
                jointName,
                clearanceDegrees,
                rotationBindings);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, adjustment.Binding);
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.value += adjustment.OffsetDegrees;
                keys[i] = key;
            }

            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, adjustment.Binding, curve);
            return adjustment;
        }

        private static ArmClearanceAdjustment SelectArmClearanceAxis(
            AnimationClip clip,
            GameObject target,
            string side,
            string jointName,
            float clearanceDegrees,
            IReadOnlyCollection<EditorCurveBinding> bindings)
        {
            Transform arm = FindUniqueBone(target, side + "Arm");
            Transform foreArm = FindUniqueBone(target, side + "ForeArm");
            Transform hand = FindUniqueBone(target, side + "Hand");
            Transform spine = FindUniqueBone(target, "Spine");
            Vector3 lateral = target.transform.right.normalized;
            float[] phases = { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f };
            float[] sideSigns = new float[phases.Length];
            float[] baseForeArmLaterals = new float[phases.Length];
            float[] baseHandLaterals = new float[phases.Length];
            List<ArmClearanceAdjustment> candidates = new List<ArmClearanceAdjustment>();

            if (AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException("Cannot inspect arm-clearance axes while another Animation Mode session is active.");
            }

            AnimationMode.StartAnimationMode();
            try
            {
                for (int phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(target, clip, clip.length * phases[phaseIndex]);
                    AnimationMode.EndSampling();

                    float sideSign = Mathf.Sign(Vector3.Dot(arm.position - spine.position, lateral));
                    if (Mathf.Approximately(sideSign, 0f))
                    {
                        throw new InvalidOperationException($"Could not determine the direct lateral side of {side}Arm.");
                    }

                    sideSigns[phaseIndex] = sideSign;
                    baseForeArmLaterals[phaseIndex] =
                        sideSign * Vector3.Dot(foreArm.position - spine.position, lateral);
                    baseHandLaterals[phaseIndex] =
                        sideSign * Vector3.Dot(hand.position - spine.position, lateral);
                }

                foreach (EditorCurveBinding binding in bindings)
                {
                    AnimationCurve originalCurve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (originalCurve == null || originalCurve.length == 0)
                    {
                        throw new InvalidOperationException(
                            $"Could not probe the actual animation curve for {binding.path}/{binding.propertyName}.");
                    }

                    foreach (float direction in new[] { -1f, 1f })
                    {
                        List<float> lateralGains = new List<float>();
                        AnimationCurve probeCurve = CloneCurveWithConstantOffset(
                            originalCurve,
                            direction * ArmAxisProbeDegrees);
                        try
                        {
                            AnimationUtility.SetEditorCurve(clip, binding, probeCurve);
                            for (int phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
                            {
                                AnimationMode.BeginSampling();
                                AnimationMode.SampleAnimationClip(target, clip, clip.length * phases[phaseIndex]);
                                AnimationMode.EndSampling();

                                float adjustedForeArm = sideSigns[phaseIndex] *
                                    Vector3.Dot(foreArm.position - spine.position, lateral);
                                float adjustedHand = sideSigns[phaseIndex] *
                                    Vector3.Dot(hand.position - spine.position, lateral);
                                lateralGains.Add(
                                    ((adjustedForeArm - baseForeArmLaterals[phaseIndex]) +
                                        (adjustedHand - baseHandLaterals[phaseIndex])) * 0.5f);
                            }
                        }
                        finally
                        {
                            AnimationUtility.SetEditorCurve(clip, binding, originalCurve);
                        }

                        candidates.Add(new ArmClearanceAdjustment
                        {
                            Side = side,
                            Joint = jointName,
                            Binding = binding,
                            OffsetDegrees = direction * clearanceDegrees,
                            MeanLateralGainPerDegree = lateralGains.Average() / ArmAxisProbeDegrees,
                            MinimumLateralGainPerDegree = lateralGains.Min() / ArmAxisProbeDegrees
                        });
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            ArmClearanceAdjustment selected = candidates
                .OrderByDescending(item => item.MinimumLateralGainPerDegree)
                .ThenByDescending(item => item.MeanLateralGainPerDegree)
                .First();
            if (selected.MinimumLateralGainPerDegree <= 0f || selected.MeanLateralGainPerDegree <= 0f)
            {
                throw new InvalidOperationException(
                    $"No {jointName} Euler axis moved the forearm and hand outward consistently across all sampled phases.");
            }

            ArmClearanceAdjustment runnerUp = candidates
                .OrderByDescending(item => item.MinimumLateralGainPerDegree)
                .ThenByDescending(item => item.MeanLateralGainPerDegree)
                .Skip(1)
                .First();
            if (selected.MinimumLateralGainPerDegree - runnerUp.MinimumLateralGainPerDegree < CurveTolerance)
            {
                throw new InvalidOperationException($"The direct outward Euler axis for {jointName} is ambiguous.");
            }

            Debug.Log(
                $"[PlayerSidestep] {jointName} outward axis selected directly: {selected.Binding.propertyName}, " +
                $"offset={selected.OffsetDegrees:R}deg, mean lateral gain/deg={selected.MeanLateralGainPerDegree:R}, " +
                $"minimum lateral gain/deg={selected.MinimumLateralGainPerDegree:R}.");
            return selected;
        }

        private static AnimationCurve CloneCurveWithConstantOffset(AnimationCurve source, float offset)
        {
            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.value += offset;
                keys[i] = key;
            }

            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static CarrierSelection SelectCarrier(AnimationClip source, GameObject target)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            var groups = bindings
                .Where(binding => binding.type == typeof(Transform) && IsPositionProperty(binding.propertyName))
                .GroupBy(binding => binding.path)
                .Select(group => new
                {
                    Path = group.Key,
                    Bindings = group.ToArray(),
                    Transform = string.IsNullOrEmpty(group.Key) ? target.transform : target.transform.Find(group.Key)
                })
                .Where(item => item.Transform != null &&
                    string.Equals(StripNamespace(item.Transform.name), "Hips", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var moving = groups.Where(item => item.Bindings.Any(binding =>
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                return curve != null && CurveRange(curve) > CurveTolerance;
            })).ToArray();

            if (moving.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one animated Hips position carrier for direct in-place conversion; found {moving.Length}.");
            }

            Transform parent = moving[0].Transform.parent;
            if (parent == null)
            {
                throw new InvalidOperationException("Hips has no parent, so its world-axis mapping cannot be determined directly.");
            }

            var axes = new[]
            {
                new { Property = "m_LocalPosition.x", Direction = parent.TransformDirection(Vector3.right).normalized },
                new { Property = "m_LocalPosition.y", Direction = parent.TransformDirection(Vector3.up).normalized },
                new { Property = "m_LocalPosition.z", Direction = parent.TransformDirection(Vector3.forward).normalized }
            };
            var vertical = axes.OrderByDescending(axis => Mathf.Abs(Vector3.Dot(axis.Direction, Vector3.up))).First();
            float verticalDot = Mathf.Abs(Vector3.Dot(vertical.Direction, Vector3.up));
            string[] horizontal = axes.Where(axis => axis.Property != vertical.Property)
                .Where(axis => Mathf.Abs(Vector3.Dot(axis.Direction, Vector3.up)) < 0.1f)
                .Select(axis => axis.Property)
                .ToArray();
            if (verticalDot < 0.9f || horizontal.Length != 2)
            {
                throw new InvalidOperationException(
                    "Hips parent axes are not aligned clearly enough to distinguish world-horizontal from world-vertical without inference.");
            }

            string[] available = moving[0].Bindings.Select(binding => binding.propertyName).ToArray();
            if (horizontal.Any(property => !available.Contains(property)))
            {
                throw new InvalidOperationException("One or more directly identified Hips horizontal position curves are missing.");
            }

            return new CarrierSelection
            {
                Bindings = moving[0].Bindings,
                HorizontalProperties = horizontal,
                VerticalProperty = vertical.Property
            };
        }

        private static void VerifyDerivedClip(
            AnimationClip source,
            AnimationClip derived,
            IReadOnlyDictionary<EditorCurveBinding, AnimationCurve> sourceCurves,
            IReadOnlyCollection<EditorCurveBinding> sourceObjectBindings,
            IReadOnlyCollection<EditorCurveBinding> changedBindings,
            CarrierSelection carrier,
            IReadOnlyCollection<ArmClearanceAdjustment> armAdjustments)
        {
            EditorCurveBinding[] derivedBindings = AnimationUtility.GetCurveBindings(derived);
            if (!new HashSet<EditorCurveBinding>(sourceCurves.Keys).SetEquals(derivedBindings))
            {
                throw new InvalidOperationException("Derived clip binding set differs from the exact source clip.");
            }

            foreach (KeyValuePair<EditorCurveBinding, AnimationCurve> pair in sourceCurves)
            {
                AnimationCurve derivedCurve = AnimationUtility.GetEditorCurve(derived, pair.Key);
                ArmClearanceAdjustment armAdjustment = armAdjustments
                    .FirstOrDefault(item => item.Binding.Equals(pair.Key));
                if (changedBindings.Contains(pair.Key))
                {
                    if (CurveRange(derivedCurve) > CurveTolerance)
                    {
                        throw new InvalidOperationException($"In-place axis is not constant: {pair.Key.path}/{pair.Key.propertyName}");
                    }

                    AssertKeyTimesAndWeightsEqual(pair.Value, derivedCurve, pair.Key);
                }
                else if (armAdjustment != null)
                {
                    AssertCurveConstantOffset(
                        pair.Value,
                        derivedCurve,
                        pair.Key,
                        armAdjustment.OffsetDegrees);
                }
                else if (!CurvesEqual(pair.Value, derivedCurve))
                {
                    throw new InvalidOperationException(
                        $"A source curve outside the approved Hips/upper-arm scope changed: " +
                        $"{pair.Key.path}/{pair.Key.propertyName}");
                }
            }

            EditorCurveBinding[] derivedObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(derived);
            if (!new HashSet<EditorCurveBinding>(sourceObjectBindings).SetEquals(derivedObjectBindings))
            {
                throw new InvalidOperationException("Object-reference animation bindings changed.");
            }

            if (changedBindings.Any(binding => binding.path != carrier.Bindings[0].path ||
                !carrier.HorizontalProperties.Contains(binding.propertyName)))
            {
                throw new InvalidOperationException("A curve outside the directly identified world-horizontal carrier axes changed.");
            }

            string[] expectedArmJoints = { "LeftArm", "RightArm", "LeftShoulder", "RightShoulder" };
            if (armAdjustments.Count != expectedArmJoints.Length ||
                !new HashSet<string>(armAdjustments.Select(item => item.Joint), StringComparer.Ordinal)
                    .SetEquals(expectedArmJoints) ||
                armAdjustments.Any(item => !item.Binding.path.EndsWith(item.Joint, StringComparison.Ordinal) ||
                    !IsRawEulerProperty(item.Binding.propertyName) ||
                    !Mathf.Approximately(
                        Mathf.Abs(item.OffsetDegrees),
                        item.Joint.EndsWith("Shoulder", StringComparison.Ordinal)
                            ? ShoulderClearanceDegreesForSide(item.Side)
                            : ArmClearanceDegreesForSide(item.Side))))
            {
                throw new InvalidOperationException("The approved bilateral shoulder/upper-arm clearance scope was not preserved.");
            }
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
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
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Animator ConfigureAnimator(GameObject target, RuntimeAnimatorController controller)
        {
            Animator[] animators = target.GetComponentsInChildren<Animator>(true);
            Animator animator;
            if (animators.Length == 0)
            {
                animator = target.AddComponent<Animator>();
            }
            else if (animators.Length == 1 && animators[0].gameObject == target)
            {
                animator = animators[0];
            }
            else
            {
                throw new InvalidOperationException(
                    $"Player_Sidestep Animator placement is ambiguous; expected zero or one root Animator, found {animators.Length}.");
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void AssertAnimatorConfiguration(
            Animator animator,
            AnimatorController controller,
            AnimationClip expectedClip)
        {
            if (!animator.enabled || animator.applyRootMotion || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("Player_Sidestep Animator connection is not exact.");
            }

            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            if (states.Length != 1 || states[0].state.motion != expectedClip ||
                !Mathf.Approximately(states[0].state.speed, 1f) || states[0].state.mirror)
            {
                throw new InvalidOperationException("Sidestep controller must contain exactly one unmirrored speed-1 source-derived state.");
            }
        }

        private static string CaptureRendererAssets(GameObject target)
        {
            return string.Join("\n", target.GetComponentsInChildren<Renderer>(true)
                .OrderBy(renderer => AnimationUtility.CalculateTransformPath(renderer.transform, target.transform), StringComparer.Ordinal)
                .Select(renderer =>
                {
                    string path = AnimationUtility.CalculateTransformPath(renderer.transform, target.transform);
                    string mesh = renderer is SkinnedMeshRenderer skinned
                        ? AssetDatabase.GetAssetPath(skinned.sharedMesh)
                        : renderer is MeshRenderer && renderer.GetComponent<MeshFilter>() != null
                            ? AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh)
                            : string.Empty;
                    string materials = string.Join("|", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath));
                    return $"{path}:{renderer.GetType().FullName}:{mesh}:{materials}";
                }));
        }

        private static string CaptureOtherObjects(Scene scene, GameObject excludedRoot)
        {
            return string.Join("\n", scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item != excludedRoot.transform && !item.IsChildOf(excludedRoot.transform))
                .Select(item =>
                {
                    string path = FullScenePath(item);
                    string components = string.Join(",", item.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName));
                    return $"{path}|{item.gameObject.activeSelf}|{item.localPosition:R}|{item.localRotation:R}|{item.localScale:R}|{components}";
                })
                .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static string FullScenePath(Transform transform)
        {
            Stack<string> parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
            {
                parts.Push(current.name);
            }

            return string.Join("/", parts);
        }

        private static bool IsPositionProperty(string property)
        {
            return property == "m_LocalPosition.x" || property == "m_LocalPosition.y" || property == "m_LocalPosition.z";
        }

        private static bool IsRawEulerProperty(string property)
        {
            return property == "localEulerAnglesRaw.x" || property == "localEulerAnglesRaw.y" ||
                property == "localEulerAnglesRaw.z";
        }

        private static float ArmClearanceDegreesForSide(string side)
        {
            if (string.Equals(side, "Left", StringComparison.Ordinal))
            {
                return LeftArmClearanceDegrees;
            }

            if (string.Equals(side, "Right", StringComparison.Ordinal))
            {
                return RightArmClearanceDegrees;
            }

            throw new InvalidOperationException($"Unsupported arm side: {side}");
        }

        private static float ShoulderClearanceDegreesForSide(string side)
        {
            if (string.Equals(side, "Left", StringComparison.Ordinal))
            {
                return LeftShoulderClearanceDegrees;
            }

            if (string.Equals(side, "Right", StringComparison.Ordinal))
            {
                return RightShoulderClearanceDegrees;
            }

            throw new InvalidOperationException($"Unsupported shoulder side: {side}");
        }

        private static Vector3 EulerAxis(string property)
        {
            if (property.EndsWith(".x", StringComparison.Ordinal))
            {
                return Vector3.right;
            }

            if (property.EndsWith(".y", StringComparison.Ordinal))
            {
                return Vector3.up;
            }

            if (property.EndsWith(".z", StringComparison.Ordinal))
            {
                return Vector3.forward;
            }

            throw new InvalidOperationException($"Unsupported Euler property: {property}");
        }

        private static Transform FindUniqueBone(GameObject root, string exactNameWithoutNamespace)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    StripNamespace(item.name),
                    exactNameWithoutNamespace,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one {exactNameWithoutNamespace} under {root.name}; found {matches.Length}.");
            }

            return matches[0];
        }

        private static string StripNamespace(string name)
        {
            int colon = name.LastIndexOf(':');
            return colon >= 0 ? name.Substring(colon + 1) : name;
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

        private static bool CurvesEqual(AnimationCurve left, AnimationCurve right)
        {
            if (left == null || right == null || left.length != right.length || left.preWrapMode != right.preWrapMode ||
                left.postWrapMode != right.postWrapMode)
            {
                return false;
            }

            for (int i = 0; i < left.length; i++)
            {
                Keyframe a = left.keys[i];
                Keyframe b = right.keys[i];
                if (a.time != b.time || a.value != b.value || a.inTangent != b.inTangent ||
                    a.outTangent != b.outTangent || a.inWeight != b.inWeight ||
                    a.outWeight != b.outWeight || a.weightedMode != b.weightedMode)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertKeyTimesAndWeightsEqual(
            AnimationCurve source,
            AnimationCurve derived,
            EditorCurveBinding binding)
        {
            if (source.length != derived.length)
            {
                throw new InvalidOperationException($"Key count changed: {binding.path}/{binding.propertyName}");
            }

            for (int i = 0; i < source.length; i++)
            {
                Keyframe a = source.keys[i];
                Keyframe b = derived.keys[i];
                if (a.time != b.time || a.inWeight != b.inWeight || a.outWeight != b.outWeight ||
                    a.weightedMode != b.weightedMode)
                {
                    throw new InvalidOperationException($"Key timing/weight changed: {binding.path}/{binding.propertyName}");
                }
            }
        }

        private static void AssertCurveConstantOffset(
            AnimationCurve source,
            AnimationCurve derived,
            EditorCurveBinding binding,
            float expectedOffset)
        {
            if (source == null || derived == null || source.length != derived.length ||
                source.preWrapMode != derived.preWrapMode || source.postWrapMode != derived.postWrapMode)
            {
                throw new InvalidOperationException(
                    $"Upper-arm curve structure changed: {binding.path}/{binding.propertyName}");
            }

            for (int i = 0; i < source.length; i++)
            {
                Keyframe a = source.keys[i];
                Keyframe b = derived.keys[i];
                if (!Mathf.Approximately(b.value - a.value, expectedOffset) ||
                    a.time != b.time || a.inTangent != b.inTangent || a.outTangent != b.outTangent ||
                    a.inWeight != b.inWeight || a.outWeight != b.outWeight || a.weightedMode != b.weightedMode)
                {
                    throw new InvalidOperationException(
                        $"Upper-arm offset changed source timing or swing shape: " +
                        $"{binding.path}/{binding.propertyName}, key={i}.");
                }
            }
        }

        private static void AssertEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unexpected change to {label}.");
            }
        }

        private static void AssertUnchanged(Vector3 actual, Vector3 expected, string label)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException($"Unexpected change to {label}: {expected:R} -> {actual:R}");
            }
        }

        private static void AssertUnchanged(Quaternion actual, Quaternion expected, string label)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException($"Unexpected change to {label}: {expected:R} -> {actual:R}");
            }
        }
    }
}
