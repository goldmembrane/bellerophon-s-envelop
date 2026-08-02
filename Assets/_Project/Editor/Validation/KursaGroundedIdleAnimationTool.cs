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

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaGroundedIdleAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string IdleSlotName = "Kursa_02_Idle";
        private const string ModelName = "Kursa_Model";
        private const string RuntimeModelPath = "Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance/Models/Kursa_Appearance_RuntimeProjection.fbx";
        private const string AnimationFbxPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_02_GroundedIdle.fbx";
        private const string ClipPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_02_GroundedIdle.anim";
        internal const string ControllerPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_02_GroundedIdle.controller";
        private const string ReportPath = "docs/validation/kursa_idle_animation_2026-08-02/Kursa_02_GroundedIdle_Inspection.txt";
        private const string CapturePath = "docs/validation/kursa_idle_animation_2026-08-02/Kursa_02_GroundedIdle_Review.png";
        private const string ExpectedRuntimeSha256 = "D9F30E87DE6C8D2438D8A8C56D7CD1E394E8F3E6CD15248A1F69CFB8F62472E9";
        private const string ExpectedAnimationSha256 = "F87EB8AB62687571DC686350DD2BB7CF9734DE6EA54E5DB9BA86A233DD469AAE";
        private const float Duration = 2f;
        private const float FrameRate = 60f;
        private const float HeadCurveSampleRate = 120f;
        private const float Travel = 0.03f;
        private const float PositionTolerance = 0.001f;
        private const float CurveTolerance = 0.0002f;
        private const int ExpectedVertices = 3377;
        private const int ExpectedTriangles = 3913;
        private const int ExpectedBones = 24;
        private const int ExpectedMaterials = 9;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_ShieldStance",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        private static readonly HashSet<string> SourceAnimatedBones = new HashSet<string>(StringComparer.Ordinal)
        {
            "Hips", "LeftUpLeg", "LeftLeg", "LeftFoot",
            "RightUpLeg", "RightLeg", "RightFoot"
        };

        private static readonly HashSet<string> AnimatedBones =
            new HashSet<string>(SourceAnimatedBones, StringComparer.Ordinal)
            {
                "Head"
            };

        private static readonly float[] SampleTimes = { 0f, 0.5f, 1f, 1.5f, 2f };
        private static readonly float[] ExpectedDown = { 0f, 0.015f, 0.03f, 0.015f, 0f };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Grounded Idle Animation")]
        public static void ApplyKursaIdleAnimation()
        {
            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var idleSlot = RequireDirectChild(placement.transform, IdleSlotName);
            var model = RequireDirectChild(idleSlot, ModelName);
            if (scene.isDirty && !IsKnownIncompleteApply(placement.transform, model))
                throw new InvalidOperationException("CargoRunMvp has unsaved changes outside the known incomplete Kursa idle apply.");
            RequireHash(RuntimeModelPath, ExpectedRuntimeSha256);
            RequireHash(AnimationFbxPath, ExpectedAnimationSha256);
            ConfigureAnimationImporter();
            RequireApprovedModel(model);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var placementTransformsBefore = placement.transform.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var pose = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();

            var clip = CreateSanitizedClip(RequireSourceClip(), model);
            var controller = CreateController(clip);
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
                throw new InvalidOperationException("Kursa_02_Idle contains multiple Animators: " + animators.Length + ".");
            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);

            foreach (var snapshot in pose) snapshot.Restore();
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform), "A Kursa slot outside Kursa_02_Idle changed.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement), "A scene root outside the Kursa placement changed.");
            if (placementTransformsBefore.Any(item => !item.Matches(0.000001f)))
                throw new InvalidOperationException("A Kursa placement transform changed beyond tolerance while applying the idle controller.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after the Kursa idle apply.");
            AssetDatabase.SaveAssets();
            Debug.Log("KursaIdleAnimationApplied Result=PASS, Slot=Kursa_02_Idle, Controller=" + ControllerPath + ", HeadCurveSampleRate=" + Num(HeadCurveSampleRate) + ", ModelLocalForwardHeadAlignment=True, OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Grounded Idle Animation")]
        public static void InspectKursaIdleAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            RequireHash(RuntimeModelPath, ExpectedRuntimeSha256);
            RequireHash(AnimationFbxPath, ExpectedAnimationSha256);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var model = RequireDirectChild(RequireDirectChild(placement.transform, IdleSlotName), ModelName);
            var metrics = Inspect(model, placement.transform, RequireClip(), RequireController());
            WriteReport(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Kursa idle inspection changed the scene dirty state.");
            Debug.Log("KursaIdleAnimationInspected Result=PASS, VerticalTravel=" + Num(metrics.VerticalTravel) + ", MaximumCurveError=" + Num(metrics.MaximumCurveError) + ", MaximumFootError=" + Num(metrics.MaximumFootError) + ", GroundVariation=" + Num(metrics.GroundVariation) + ", MinimumRightArmThighClearance=" + Num(metrics.MinimumArmThighClearance) + ", RightArmThighOverlapPairs=0, LoopError=" + Num(metrics.LoopError) + ", MaximumHeadLocalFrameError=" + Num(metrics.MaximumHeadLocalFrameError) + ", HeadSampleRate=" + Num(HeadCurveSampleRate) + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Grounded Idle Review")]
        public static void CaptureKursaIdleAnimationReview()
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var model = RequireDirectChild(RequireDirectChild(placement.transform, IdleSlotName), ModelName);
            var clip = RequireClip();
            Inspect(model, placement.transform, clip, RequireController());
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time Kursa idle review already exists: " + CapturePath);
            CaptureStrip(model, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Kursa idle capture changed the scene dirty state.");
            Debug.Log("KursaIdleAnimationReviewCaptured Result=PASS, Times=0,0.5,1,1.5,2, Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static void ConfigureAnimationImporter()
        {
            var importer = AssetImporter.GetAtPath(AnimationFbxPath) as ModelImporter ??
                throw new InvalidOperationException("Kursa idle FBX importer is unavailable.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("Kursa idle FBX must expose exactly one take.");
            clips[0].name = "Kursa_02_GroundedIdle";
            clips[0].takeName = clips[0].takeName;
            clips[0].firstFrame = 0f;
            clips[0].lastFrame = 120f;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootHeightY = false;
            clips[0].lockRootPositionXZ = true;
            clips[0].lockRootRotation = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(AnimationFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException("Kursa idle FBX must import exactly one animation clip.");
            return clips[0];
        }

        private static AnimationClip CreateSanitizedClip(AnimationClip source, Transform model)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ClipPath) != null && !AssetDatabase.DeleteAsset(ClipPath))
                throw new InvalidOperationException("Existing Kursa idle clip could not be replaced.");
            var clip = new AnimationClip { name = "Kursa_02_GroundedIdle", frameRate = FrameRate };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var bone = binding.path.Split('/').Last();
                if (!SourceAnimatedBones.Contains(bone) || binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                var target = RequireDescendant(model, bone);
                var targetPath = AnimationUtility.CalculateTransformPath(target, model);
                var targetBinding = EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), binding.propertyName);
                AnimationUtility.SetEditorCurve(clip, targetBinding, AnimationUtility.GetEditorCurve(source, binding));
            }
            KursaForwardHeadAlignmentTool.AddModelLocalForwardHeadCurves(
                clip,
                model,
                HeadCurveSampleRate);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimationClip RequireClip()
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Sanitized Kursa idle clip is missing.");
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null)
            {
                if (!AssetDatabase.DeleteAsset(ControllerPath))
                    throw new InvalidOperationException("Existing Kursa idle controller could not be replaced.");
            }
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("KursaGroundedIdle");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Kursa idle controller is missing.");
        }

        private static Metrics Inspect(Transform model, Transform placement, AnimationClip clip, AnimatorController controller)
        {
            RequireApprovedModel(model);
            if (Mathf.Abs(clip.length - Duration) > 0.001f || Mathf.Abs(clip.frameRate - FrameRate) > 0.001f)
                throw new InvalidOperationException("Kursa idle duration or frame rate differs.");
            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
                throw new InvalidOperationException("Kursa idle clip is not looping.");
            RequireCurveContract(clip);

            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Kursa_02_Idle must contain exactly one Animator.");
            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("Kursa_02_Idle Animator configuration differs.");
            foreach (var slotName in SlotNames.Where(item => item != IdleSlotName))
            {
                var other = RequireDirectChild(placement, slotName);
                var enabledAnimators = other.GetComponentsInChildren<Animator>(true).Where(item => item.enabled).ToArray();
                var enabledLegacyAnimations = other.GetComponentsInChildren<Animation>(true).Any(item => item.enabled);
                var hasApprovedMoveAnimator = slotName == "Kursa_03_Move" &&
                    enabledAnimators.Length == 1 &&
                    !enabledAnimators[0].applyRootMotion &&
                    AssetDatabase.GetAssetPath(enabledAnimators[0].runtimeAnimatorController) == KursaMoveAnimationTool.ControllerPath;
                if (enabledLegacyAnimations || (slotName == "Kursa_03_Move" ? !hasApprovedMoveAnimator : enabledAnimators.Length != 0))
                    throw new InvalidOperationException(slotName + " animation state changed.");
            }

            var hips = RequireDescendant(model, "Hips");
            var leftFoot = RequireDescendant(model, "LeftFoot");
            var rightFoot = RequireDescendant(model, "RightFoot");
            var leftArm = RequireDescendant(model, "LeftArm");
            var leftForeArm = RequireDescendant(model, "LeftForeArm");
            var leftHand = RequireDescendant(model, "LeftHand");
            var rightArm = RequireDescendant(model, "RightArm");
            var rightForeArm = RequireDescendant(model, "RightForeArm");
            var rightHand = RequireDescendant(model, "RightHand");
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            var protectedLocal = model.GetComponentsInChildren<Transform>(true)
                .Where(item => !AnimatedBones.Contains(item.name))
                .ToDictionary(item => item, item => new LocalPose(item));
            var animatorEnabled = animator.enabled;
            var baked = new Mesh();
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(model.gameObject, 0f);
                var restHips = hips.position;
                var restLeftFoot = leftFoot.position;
                var restRightFoot = rightFoot.position;
                renderer.BakeMesh(baked);
                var restGround = MinimumWorldY(renderer, baked);
                var maxCurveError = 0f;
                var maxFootError = 0f;
                var maxGroundVariation = 0f;
                var minimumClearance = float.PositiveInfinity;
                var totalOverlaps = 0;
                var endHips = restHips;
                for (var index = 0; index < SampleTimes.Length; index++)
                {
                    clip.SampleAnimation(model.gameObject, SampleTimes[index]);
                    var actualDown = restHips.y - hips.position.y;
                    maxCurveError = Mathf.Max(maxCurveError, Mathf.Abs(actualDown - ExpectedDown[index]));
                    maxFootError = Mathf.Max(maxFootError, Vector3.Distance(leftFoot.position, restLeftFoot), Vector3.Distance(rightFoot.position, restRightFoot));
                    foreach (var pair in protectedLocal)
                        if (!pair.Value.Matches(pair.Key, 0.00001f))
                            throw new InvalidOperationException("A non-idle Kursa transform changed at t=" + Num(SampleTimes[index]) + ": " + pair.Key.name + ".");
                    renderer.BakeMesh(baked);
                    maxGroundVariation = Mathf.Max(maxGroundVariation, Mathf.Abs(MinimumWorldY(renderer, baked) - restGround));
                    var separation = ArmThighSeparation(renderer, baked);
                    minimumClearance = Mathf.Min(minimumClearance, separation.Clearance);
                    totalOverlaps += separation.Overlaps;
                    if (index == SampleTimes.Length - 1) endHips = hips.position;
                }
                var verticalTravel = SampleTimes.Select(time => { clip.SampleAnimation(model.gameObject, time); return hips.position.y; }).Max() -
                                     SampleTimes.Select(time => { clip.SampleAnimation(model.gameObject, time); return hips.position.y; }).Min();
                var loopError = Vector3.Distance(restHips, endHips);
                if (Mathf.Abs(verticalTravel - Travel) > CurveTolerance || maxCurveError > CurveTolerance)
                {
                    var actualDown = SampleTimes.Select(time =>
                    {
                        clip.SampleAnimation(model.gameObject, time);
                        return restHips.y - hips.position.y;
                    }).ToArray();
                    throw new InvalidOperationException(
                        "Kursa idle vertical curve differs from the approved down-first 3 cm curve. VerticalTravel=" +
                        Num(verticalTravel) + ", MaximumCurveError=" + Num(maxCurveError) +
                        ", ActualDown=" + string.Join(",", actualDown.Select(Num)) + ".");
                }
                if (maxFootError > PositionTolerance)
                    throw new InvalidOperationException("Kursa idle foot anchoring exceeds 0.001 Unity unit.");
                if (maxGroundVariation > PositionTolerance)
                    throw new InvalidOperationException("Kursa idle ground-contact variation exceeds 0.001 Unity unit.");
                if (totalOverlaps != 0 || minimumClearance <= 0.00001f)
                    throw new InvalidOperationException("Kursa right arm intersects the right thigh during idle.");
                if (loopError > CurveTolerance)
                    throw new InvalidOperationException("Kursa idle loop boundary differs.");
                var maximumHeadLocalFrameError = 0f;
                var headSamples = Mathf.CeilToInt(
                    clip.length * HeadCurveSampleRate);
                for (var sample = 0; sample <= headSamples; sample++)
                {
                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * sample / headSamples);
                    maximumHeadLocalFrameError = Mathf.Max(
                        maximumHeadLocalFrameError,
                        KursaForwardHeadAlignmentTool.MeasureHeadLocalFrameError(
                            model,
                            renderer));
                }
                if (maximumHeadLocalFrameError > 0.05f)
                    throw new InvalidOperationException(
                        "Kursa idle face does not remain on model-local +Z/+Y. MaximumError=" +
                        Num(maximumHeadLocalFrameError) + ".");
                return new Metrics(
                    verticalTravel,
                    maxCurveError,
                    maxFootError,
                    maxGroundVariation,
                    minimumClearance,
                    loopError,
                    maximumHeadLocalFrameError);
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void RequireCurveContract(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0) throw new InvalidOperationException("Kursa idle clip has no transform curves.");
            foreach (var binding in bindings)
            {
                if (string.IsNullOrEmpty(binding.path))
                {
                    if (binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new InvalidOperationException("Kursa idle root contains a scale curve: " + binding.propertyName);
                    var rootCurve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (rootCurve == null || rootCurve.keys.Any(key => Mathf.Abs(key.value - rootCurve.keys[0].value) > 0.000001f))
                        throw new InvalidOperationException("Kursa idle contains non-constant root motion: " + binding.propertyName);
                    continue;
                }
                var bone = binding.path.Split('/').Last();
                if (binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Kursa idle contains a scale curve: " + binding.path + "/" + binding.propertyName);
                if (bone == "Head" &&
                    !binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Kursa idle Head may contain rotation curves only: " +
                        binding.propertyName + ".");
                if (!AnimatedBones.Contains(bone))
                {
                    var constantCurve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (constantCurve == null || constantCurve.keys.Any(key => Mathf.Abs(key.value - constantCurve.keys[0].value) > 0.000001f))
                        throw new InvalidOperationException("Unauthorized moving Kursa idle curve: " + binding.path + "/" + binding.propertyName);
                }
            }
            if (AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0)
                throw new InvalidOperationException("Kursa idle contains object-reference curves.");
            foreach (var bone in AnimatedBones)
                if (!bindings.Any(item => item.path.Split('/').Last() == bone))
                    throw new InvalidOperationException("Kursa idle is missing curves for " + bone + ".");
        }

        private static void RequireApprovedModel(Transform model)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Kursa_02_Idle must contain one skinned renderer.");
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Kursa mesh is missing.");
            var triangles = Enumerable.Range(0, mesh.subMeshCount).Sum(index => (int)mesh.GetIndexCount(index) / 3);
            if (AssetDatabase.GetAssetPath(mesh) != RuntimeModelPath || mesh.vertexCount != ExpectedVertices || triangles != ExpectedTriangles || renderer.bones.Length != ExpectedBones || renderer.sharedMaterials.Length != ExpectedMaterials)
                throw new InvalidOperationException("Kursa_02_Idle approved appearance contract differs.");
        }

        private static Separation ArmThighSeparation(SkinnedMeshRenderer renderer, Mesh baked)
        {
            var arm = InfluenceTriangles(renderer, baked, new[] { "RightArm", "RightForeArm", "RightHand" });
            var thigh = InfluenceTriangles(renderer, baked, new[] { "RightUpLeg" });
            var minimum = float.PositiveInfinity;
            var overlaps = 0;
            foreach (var first in arm)
            foreach (var second in thigh)
            {
                var distance = TriangleDistanceSquared(first, second);
                minimum = Mathf.Min(minimum, distance);
                if (distance <= 0.0000000001f) overlaps++;
            }
            return new Separation(overlaps, Mathf.Sqrt(Mathf.Max(0f, minimum)));
        }

        private static List<Triangle> InfluenceTriangles(SkinnedMeshRenderer renderer, Mesh baked, IEnumerable<string> names)
        {
            var wanted = new HashSet<string>(names, StringComparer.Ordinal);
            var bones = new HashSet<int>(renderer.bones.Select((bone, index) => new { bone, index })
                .Where(item => item.bone != null && wanted.Contains(item.bone.name)).Select(item => item.index));
            if (bones.Count != wanted.Count) throw new InvalidOperationException("Kursa influence bones are incomplete.");
            var source = renderer.sharedMesh;
            var weights = source.boneWeights;
            var vertices = baked.vertices.Select(item => renderer.localToWorldMatrix.MultiplyPoint3x4(item)).ToArray();
            var result = new List<Triangle>();
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                var indices = source.GetIndices(submesh);
                for (var index = 0; index < indices.Length; index += 3)
                {
                    var a = indices[index]; var b = indices[index + 1]; var c = indices[index + 2];
                    if (Influence(weights[a], bones) + Influence(weights[b], bones) + Influence(weights[c], bones) > 1.5f)
                        result.Add(new Triangle(vertices[a], vertices[b], vertices[c]));
                }
            }
            if (result.Count == 0) throw new InvalidOperationException("Kursa influence surface contains no triangles.");
            return result;
        }

        private static float Influence(BoneWeight weight, HashSet<int> bones)
        {
            var value = 0f;
            if (bones.Contains(weight.boneIndex0)) value += weight.weight0;
            if (bones.Contains(weight.boneIndex1)) value += weight.weight1;
            if (bones.Contains(weight.boneIndex2)) value += weight.weight2;
            if (bones.Contains(weight.boneIndex3)) value += weight.weight3;
            return value;
        }

        private static float TriangleDistanceSquared(Triangle first, Triangle second)
        {
            var firstEdges = new[] { (first.A, first.B), (first.B, first.C), (first.C, first.A) };
            var secondEdges = new[] { (second.A, second.B), (second.B, second.C), (second.C, second.A) };
            if (firstEdges.Any(edge => SegmentIntersectsTriangle(edge.Item1, edge.Item2, second)) || secondEdges.Any(edge => SegmentIntersectsTriangle(edge.Item1, edge.Item2, first))) return 0f;
            var minimum = Mathf.Min(PointTriangleDistanceSquared(first.A, second), PointTriangleDistanceSquared(first.B, second), PointTriangleDistanceSquared(first.C, second), PointTriangleDistanceSquared(second.A, first), PointTriangleDistanceSquared(second.B, first), PointTriangleDistanceSquared(second.C, first));
            foreach (var firstEdge in firstEdges)
            foreach (var secondEdge in secondEdges)
                minimum = Mathf.Min(minimum, SegmentDistanceSquared(firstEdge.Item1, firstEdge.Item2, secondEdge.Item1, secondEdge.Item2));
            return minimum;
        }

        private static bool SegmentIntersectsTriangle(Vector3 start, Vector3 end, Triangle triangle)
        {
            var direction = end - start; var edge1 = triangle.B - triangle.A; var edge2 = triangle.C - triangle.A;
            var cross = Vector3.Cross(direction, edge2); var determinant = Vector3.Dot(edge1, cross);
            if (Mathf.Abs(determinant) <= 0.0000001f) return false;
            var inverse = 1f / determinant; var delta = start - triangle.A; var u = Vector3.Dot(delta, cross) * inverse;
            if (u < 0f || u > 1f) return false;
            var q = Vector3.Cross(delta, edge1); var v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f) return false;
            var distance = Vector3.Dot(edge2, q) * inverse;
            return distance >= 0f && distance <= 1f;
        }

        private static float PointTriangleDistanceSquared(Vector3 point, Triangle triangle) => (point - ClosestPoint(point, triangle)).sqrMagnitude;

        private static Vector3 ClosestPoint(Vector3 point, Triangle t)
        {
            var ab = t.B - t.A; var ac = t.C - t.A; var ap = point - t.A;
            var d1 = Vector3.Dot(ab, ap); var d2 = Vector3.Dot(ac, ap); if (d1 <= 0f && d2 <= 0f) return t.A;
            var bp = point - t.B; var d3 = Vector3.Dot(ab, bp); var d4 = Vector3.Dot(ac, bp); if (d3 >= 0f && d4 <= d3) return t.B;
            var vc = d1 * d4 - d3 * d2; if (vc <= 0f && d1 >= 0f && d3 <= 0f) return t.A + ab * (d1 / (d1 - d3));
            var cp = point - t.C; var d5 = Vector3.Dot(ab, cp); var d6 = Vector3.Dot(ac, cp); if (d6 >= 0f && d5 <= d6) return t.C;
            var vb = d5 * d2 - d1 * d6; if (vb <= 0f && d2 >= 0f && d6 <= 0f) return t.A + ac * (d2 / (d2 - d6));
            var va = d3 * d6 - d5 * d4; if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f) return t.B + (t.C - t.B) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            var denominator = 1f / (va + vb + vc); return t.A + ab * (vb * denominator) + ac * (vc * denominator);
        }

        private static float SegmentDistanceSquared(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            var d1 = q1 - p1; var d2 = q2 - p2; var r = p1 - p2; var a = Vector3.Dot(d1, d1); var e = Vector3.Dot(d2, d2); var f = Vector3.Dot(d2, r);
            float s; float t;
            if (a <= 0.0000001f && e <= 0.0000001f) return r.sqrMagnitude;
            if (a <= 0.0000001f) { s = 0f; t = Mathf.Clamp01(f / e); }
            else
            {
                var c = Vector3.Dot(d1, r);
                if (e <= 0.0000001f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                else
                {
                    var b = Vector3.Dot(d1, d2); var denominator = a * e - b * b;
                    s = denominator == 0f ? 0f : Mathf.Clamp01((b * f - c * e) / denominator);
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                    else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
                }
            }
            return ((p1 + d1 * s) - (p2 + d2 * t)).sqrMagnitude;
        }

        private static float MinimumWorldY(SkinnedMeshRenderer renderer, Mesh baked)
        {
            var matrix = renderer.localToWorldMatrix;
            return baked.vertices.Min(item => matrix.MultiplyPoint3x4(item).y);
        }

        private static void CaptureStrip(Transform model, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid capture folder."));
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .Where(item => !item.transform.IsChildOf(model)).Select(item => new RendererSnapshot(item)).ToArray();
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ?? throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject("KursaIdleReviewCamera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            const int width = 384; const int height = 640;
            var strip = new Texture2D(width * 5, height, TextureFormat.RGB24, false);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var item in otherRenderers) item.Renderer.enabled = false;
                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f); camera.cullingMask = ~0; camera.fieldOfView = 34f; camera.targetTexture = target;
                clip.SampleAnimation(model.gameObject, 0f); FrameCamera(camera, model, sourceCamera, width / (float)height);
                for (var index = 0; index < SampleTimes.Length; index++)
                {
                    clip.SampleAnimation(model.gameObject, SampleTimes[index]); camera.Render(); RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0); panel.Apply(); var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24)) throw new InvalidOperationException("Kursa idle review contains Unity magenta shader fallback.");
                    strip.SetPixels32(index * width, 0, width, height, pixels);
                }
                strip.Apply(); File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive; cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var item in otherRenderers) item.Restore(); foreach (var snapshot in snapshots) snapshot.Restore(); animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel); UnityEngine.Object.DestroyImmediate(strip); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, Camera source, float aspect)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false).Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Kursa_02_Idle has no visible renderer.");
            var bounds = renderers[0].bounds; for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var direction = source.transform.position - bounds.center; direction.y = 0f; if (direction.sqrMagnitude < 0.0001f) direction = Vector3.back; direction.Normalize();
            camera.aspect = aspect; var vertical = bounds.extents.y / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) / Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position = bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position, Vector3.up);
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) throw new InvalidOperationException("Open CargoRunMvp before working on Kursa idle.");
            if (requireClean && scene.isDirty) throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static bool IsKnownIncompleteApply(Transform placement, Transform model)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1 || animators[0].transform != model ||
                !animators[0].enabled || animators[0].applyRootMotion ||
                AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController) != ControllerPath)
                return false;
            foreach (var slotName in SlotNames.Where(item => item != IdleSlotName))
            {
                var slot = RequireDirectChild(placement, slotName);
                var enabledAnimators = slot.GetComponentsInChildren<Animator>(true).Where(item => item.enabled).ToArray();
                var enabledLegacyAnimations = slot.GetComponentsInChildren<Animation>(true).Any(item => item.enabled);
                var hasApprovedMoveAnimator = slotName == "Kursa_03_Move" &&
                    enabledAnimators.Length == 1 &&
                    !enabledAnimators[0].applyRootMotion &&
                    AssetDatabase.GetAssetPath(enabledAnimators[0].runtimeAnimatorController) == KursaMoveAnimationTool.ControllerPath;
                if (enabledLegacyAnimations || (slotName == "Kursa_03_Move" ? !hasApprovedMoveAnimator : enabledAnimators.Length != 0))
                    return false;
            }
            return true;
        }

        private static GameObject RequirePlacement(Scene scene) => scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ?? throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length) throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 || slot.GetChild(0).name != ModelName) throw new InvalidOperationException("Kursa slot contract differs at index " + index + ".");
            }
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount).Select(parent.GetChild).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Required Kursa bone differs: " + name + ".");
            return matches[0];
        }

        private static string[] OtherSlotSignatures(Transform placement) => SlotNames.Where(item => item != IdleSlotName).Select(item => RecursiveSignature(RequireDirectChild(placement, item))).ToArray();
        private static string[] OtherRootSignatures(Scene scene, GameObject placement) => scene.GetRootGameObjects().Where(item => item != placement).OrderBy(item => item.name, StringComparer.Ordinal).Select(item => RecursiveSignature(item.transform)).ToArray();
        private static string[] TransformSignatures(Transform root) => root.GetComponentsInChildren<Transform>(true).Select(item => TransformSignature(item)).ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(TransformSignature(item));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled).Append(':');
                    if (renderer is SkinnedMeshRenderer skinned) builder.Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials) builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
                foreach (var animator in item.GetComponents<Animator>()) builder.Append("|A:").Append(animator.enabled).Append(':').Append(animator.applyRootMotion).Append(':').Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            }
            return builder.ToString();
        }

        private static string TransformSignature(Transform item) => item.name + '|' + item.gameObject.activeSelf + '|' + Num(item.localPosition.x) + ',' + Num(item.localPosition.y) + ',' + Num(item.localPosition.z) + '|' + Num(item.localRotation.x) + ',' + Num(item.localRotation.y) + ',' + Num(item.localRotation.z) + ',' + Num(item.localRotation.w) + '|' + Num(item.localScale.x) + ',' + Num(item.localScale.y) + ',' + Num(item.localScale.z);
        private static void RequireEqual(string[] before, string[] after, string message) { if (!before.SequenceEqual(after, StringComparer.Ordinal)) throw new InvalidOperationException(message); }

        private static void WriteReport(Metrics metrics)
        {
            var absolute = Absolute(ReportPath); Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? throw new InvalidOperationException("Invalid report folder."));
            File.WriteAllLines(absolute, new[]
            {
                "Result=PASS", "Target=Approved Kursa Enemy Placement/Kursa_02_Idle", "DurationSeconds=" + Num(Duration), "FrameRate=" + Num(FrameRate),
                "Timeline=0:0,0.5:-0.015,1:-0.03,1.5:-0.015,2:0", "VerticalTravel=" + Num(metrics.VerticalTravel), "MaximumCurveError=" + Num(metrics.MaximumCurveError),
                "MaximumFootError=" + Num(metrics.MaximumFootError), "GroundVariation=" + Num(metrics.GroundVariation), "RightArmThighOverlapPairs=0",
                "MinimumRightArmThighClearance=" + Num(metrics.MinimumArmThighClearance), "LoopError=" + Num(metrics.LoopError), "RootMotion=False", "BoneScaling=False",
                "MaximumHeadLocalFrameError=" + Num(metrics.MaximumHeadLocalFrameError), "HeadCurveSampleRate=" + Num(HeadCurveSampleRate),
                "HeadDirectionBasis=HeadToHeadFrontAlignedToModelLocalPositiveZ", "HeadUpBasis=HeadToHeadEndAlignedToModelLocalPositiveY",
                "AnimatedBones=Hips,LeftUpLeg,LeftLeg,LeftFoot,RightUpLeg,RightLeg,RightFoot,Head", "OtherSlotsUnchanged=True", "OtherSceneRootsUnchanged=True",
                "RuntimeFbxSha256=" + ExpectedRuntimeSha256, "AnimationFbxSha256=" + ExpectedAnimationSha256
            }, Encoding.UTF8);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path)); using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Kursa asset hash differs: " + path + ".");
        }

        private static string Absolute(string relative) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        private static string Num(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private readonly struct Metrics
        {
            public readonly float VerticalTravel, MaximumCurveError, MaximumFootError, GroundVariation, MinimumArmThighClearance, LoopError, MaximumHeadLocalFrameError;
            public Metrics(float travel, float curve, float foot, float ground, float clearance, float loop, float head) { VerticalTravel = travel; MaximumCurveError = curve; MaximumFootError = foot; GroundVariation = ground; MinimumArmThighClearance = clearance; LoopError = loop; MaximumHeadLocalFrameError = head; }
        }

        private readonly struct Separation { public readonly int Overlaps; public readonly float Clearance; public Separation(int overlaps, float clearance) { Overlaps = overlaps; Clearance = clearance; } }
        private readonly struct Triangle { public readonly Vector3 A, B, C; public Triangle(Vector3 a, Vector3 b, Vector3 c) { A = a; B = b; C = c; } }

        private readonly struct LocalPose
        {
            private readonly Vector3 position, scale; private readonly Quaternion rotation;
            public LocalPose(Transform item) { position = item.localPosition; rotation = item.localRotation; scale = item.localScale; }
            public bool Matches(Transform item, float tolerance) => Vector3.Distance(position, item.localPosition) <= tolerance && Quaternion.Angle(rotation, item.localRotation) <= tolerance && Vector3.Distance(scale, item.localScale) <= tolerance;
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform item; private readonly Vector3 position, scale; private readonly Quaternion rotation;
            public TransformSnapshot(Transform value) { item = value; position = value.localPosition; rotation = value.localRotation; scale = value.localScale; }
            public void Restore() { if (item == null) return; item.localPosition = position; item.localRotation = rotation; item.localScale = scale; }
            public bool Matches(float tolerance) => item != null && Vector3.Distance(position, item.localPosition) <= tolerance && Quaternion.Angle(rotation, item.localRotation) <= tolerance && Vector3.Distance(scale, item.localScale) <= tolerance;
        }

        private readonly struct RendererSnapshot
        {
            public readonly Renderer Renderer; private readonly bool enabled;
            public RendererSnapshot(Renderer renderer) { Renderer = renderer; enabled = renderer.enabled; }
            public void Restore() { if (Renderer != null) Renderer.enabled = enabled; }
        }
    }
}
