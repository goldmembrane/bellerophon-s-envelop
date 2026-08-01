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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static partial class PahurRunningModelAndAnimationTool
    {
        private const string DeathSlotName = "Pahur_11_Death";
        private const string SourceDeathModelPath =
            @"D:\Bellerophon2\Bellerophon\enemies model\pāḫḫur death.fbx";
        private const string SourceDeathSha256 =
            "B1D523DE84C9E104B8C864CB9F531519D92B3BC460C6D92020B1DF6C945F2770";
        private const string DeathModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurDeath.fbx";
        private const string DeathAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurDeathApprovedAppearanceMesh.asset";
        private const string DeathClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_11_Death_InPlace.anim";
        private const string DeathControllerPath =
            "Assets/_Project/Art/Enemies/Pahur/Controllers/Pahur_11_Death.controller";
        private const string DeathStateName = "PahurDeathMixamoLoop";
        private const string DeathReportPath =
            "docs/validation/pahur_death_animation_2026-08-01/Pahur_11_Death_NoVerticalDrop_Validation.txt";
        private const string DeathCapturePath =
            "docs/validation/pahur_death_animation_2026-08-01/Pahur_11_Death_NoVerticalDrop_Review.png";
        private const float DeathHoldSeconds = 1f;
        private const float DeathArmSettleSeconds = 0.25f;
        private const float DeathArmContactTolerance = 0.003f;
        // Preserves the user-approved floor-facing right-arm angle while the
        // erroneous Armature Y translation is removed.
        private static readonly Quaternion DeathRightArmFinalRotation =
            new Quaternion(
                0.592768431f,
                0.187915742f,
                -0.477321982f,
                0.620867968f).normalized;
        private static readonly Quaternion DeathRightForeArmFinalRotation =
            new Quaternion(
                -0.38413617f,
                0.0182604771f,
                0.108573064f,
                0.916688561f).normalized;

        [MenuItem("Bellerophon/Enemies/Pahur/Inspect Death Source")]
        public static void InspectPahurDeathSource()
        {
            RequireDeathSourceHash();
            ImportDeathModel();
            var takeName = ConfigureDeathImporter();
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur death FBX is missing.");
            var renderer = RequireRenderer(prefab.transform, "death FBX");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running Pahur FBX is missing.");
            RequireExactMiniTransferContract(
                renderer,
                RequireRenderer(runningPrefab.transform, "approved running FBX"));
            var clip = RequireDeathSourceClip(takeName);

            var scene = RequireScene(false);
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel = RequireModel(
                RequireChild(placement.transform, StaticSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var deathSlot = RequireChild(placement.transform, DeathSlotName);
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var matchedScale = MatchedRunningScale(
                staticPrefab,
                prefab,
                staticModel);
            var arm = AnalyzeDeathRightArmSurface(
                prefab,
                renderer,
                clip,
                matchedScale,
                deathSlot.position.y + staticModel.localPosition.y,
                staticRenderer.bounds.min.y);
            var diagnostics = DeathArmDiagnostics(
                prefab,
                clip,
                matchedScale,
                deathSlot.position.y + staticModel.localPosition.y);

            Debug.Log(
                "PahurDeathSourceInspection Result=PASS" +
                ", Sha256=" + SourceDeathSha256 +
                ", Clip=" + clip.name +
                ", ClipLength=" + NumDeath(clip.length) +
                ", FrameRate=" + NumDeath(clip.frameRate) +
                ", Vertices=" + renderer.sharedMesh.vertexCount +
                ", Triangles=" + renderer.sharedMesh.triangles.Length / 3 +
                ", Bones=" + renderer.bones.Length +
                ", BoneNames=" +
                string.Join("|", renderer.bones.Select(item => item.name)) +
                ", ExactAppearanceTransferContract=True" +
                ", RightArmVertices25=" + arm.Vertices25 +
                ", RightArmVertices50=" + arm.Vertices50 +
                ", FloorWorldY=" + NumDeath(arm.FloorWorldY) +
                ", FinalArmSurfaceMinWorldY=" +
                NumDeath(arm.MinimumWorldY) +
                ", FinalArmSurfaceGap=" + NumDeath(arm.Gap) +
                ", " + diagnostics + ".");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Death Animation")]
        public static void ApplyPahurDeathAnimation()
        {
            var scene = RequireScene(true);
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel = RequireModel(
                RequireChild(placement.transform, StaticSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var slot = RequireChild(placement.transform, DeathSlotName);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_11_Death must contain exactly one current model.");
            }

            var otherSlots = OtherSlotSignatures(placement.transform, DeathSlotName);
            var protectedRoots = ProtectedRootSignatures(scene, placement.transform);
            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;

            RequireDeathSourceHash();
            ImportDeathModel();
            var takeName = ConfigureDeathImporter();
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur death FBX is missing.");
            var prefabRenderer = RequireRenderer(prefab.transform, "death FBX");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running Pahur FBX is missing.");
            RequireExactMiniTransferContract(
                prefabRenderer,
                RequireRenderer(runningPrefab.transform, "approved running FBX"));
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var sourceClip = RequireDeathSourceClip(takeName);
            var sourceLength = sourceClip.length;
            var appearance = CreateDeathAppearanceMesh(prefabRenderer);
            var matchedScale = MatchedRunningScale(
                staticPrefab,
                prefab,
                staticModel);
            var clip = CreateDeathInPlaceHoldClip(
                sourceClip,
                prefab.transform,
                prefabRenderer);
            var armAngleError = AuthorDeathRightArmFloorAngle(
                clip,
                sourceLength,
                prefab);
            var armatureYChange = RequireDeathArmatureYStable(clip, prefab);
            RequireNoHorizontalRootTranslation(
                prefab.transform,
                prefabRenderer,
                clip);
            var holdChange = RequireDeathHold(clip, sourceLength);
            var controller = CreateDeathController(clip);

            var previous = slot.GetChild(0);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;
            var replacement =
                PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "The Pahur death prefab could not be instantiated.");
            replacement.name = ModelName;
            replacement.transform.SetParent(slot, false);
            replacement.transform.SetLocalPositionAndRotation(
                new Vector3(
                    previousPosition.x,
                    staticModel.localPosition.y,
                    previousPosition.z),
                previousRotation);
            replacement.transform.localScale = Vector3.one * matchedScale;
            try
            {
                var renderer = RequireRenderer(replacement.transform, DeathSlotName);
                renderer.sharedMesh = appearance;
                renderer.sharedMaterials = staticRenderer.sharedMaterials.ToArray();
                renderer.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

                var animator = replacement.GetComponent<Animator>() ??
                               replacement.AddComponent<Animator>();
                var sourceAnimator = prefab.GetComponent<Animator>() ??
                                     throw new InvalidOperationException(
                                         "The Pahur death FBX has no Animator.");
                animator.avatar = sourceAnimator.avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            RequireUnchanged(
                otherSlots,
                OtherSlotSignatures(placement.transform, DeathSlotName),
                "A Pahur slot outside Pahur_11_Death changed.");
            RequireUnchanged(
                protectedRoots,
                ProtectedRootSignatures(scene, placement.transform),
                "A scene root outside the Pahur placement changed.");
            if (slot.localPosition != slotPosition ||
                slot.localRotation != slotRotation ||
                slot.localScale != slotScale)
            {
                throw new InvalidOperationException(
                    "The Pahur death slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Pahur death model.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurDeathAnimationApplied Result=PASS" +
                ", SourceClip=" + sourceClip.name +
                ", SourceLength=" + NumDeath(sourceLength) +
                ", PlaybackClip=" + clip.name +
                ", HoldSeconds=" + NumDeath(DeathHoldSeconds) +
                ", Loop=True, ReturnMotion=False" +
                ", HorizontalRootMotion=False" +
                ", StaticAppearanceTransferredExactly=True" +
                ", SharedStaticMaterials=True" +
                ", ArmatureYCurve=False" +
                ", ArmatureYChange=" + NumDeath(armatureYChange) +
                ", RightArmFloorAnglePreserved=True" +
                ", RightArmFloorAngleError=" + NumDeath(armAngleError) +
                ", HoldCurveChange=" + NumDeath(holdChange) +
                ", OtherSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Validate Death Animation")]
        public static void ValidatePahurDeathAnimation()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel = RequireModel(
                RequireChild(placement.transform, StaticSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var slot = RequireChild(placement.transform, DeathSlotName);
            var model = RequireModel(slot);
            var renderer = RequireRenderer(model, DeathSlotName);
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DeathModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur death FBX is missing.");
            var prefabRenderer = RequireRenderer(prefab.transform, "death FBX");
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(DeathAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The Pahur death appearance mesh is missing.");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var expectedScale = MatchedRunningScale(
                staticPrefab,
                prefab,
                staticModel);
            if (renderer.sharedMesh != appearance ||
                !renderer.sharedMaterials.SequenceEqual(staticRenderer.sharedMaterials) ||
                model.localScale != Vector3.one * expectedScale ||
                model.localPosition.y != staticModel.localPosition.y)
            {
                throw new InvalidOperationException(
                    "The Pahur death appearance, size, or Y position differs from the approved static contract.");
            }

            RequireMiniAppearancePreserved(prefabRenderer.sharedMesh, appearance);
            var animator = model.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "The Pahur death model has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(DeathControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur death controller is missing.");
            var clip = controller.layers[0].stateMachine.defaultState.motion as AnimationClip ??
                       throw new InvalidOperationException(
                           "The Pahur death controller has no clip.");
            if (AssetDatabase.GetAssetPath(clip) != DeathClipPath ||
                !clip.isLooping ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "The Pahur death animation contract differs.");
            }

            RequireDeathSourceHash();
            if (Sha256(Absolute(DeathModelPath)) != SourceDeathSha256)
            {
                throw new InvalidOperationException(
                    "The imported Pahur death FBX differs from the supplied source.");
            }

            var sourceClip = RequireDeathSourceClip("mixamo.com");
            var sourceLength = sourceClip.length;
            if (Mathf.Abs(clip.length - (sourceLength + DeathHoldSeconds)) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "The Pahur death clip does not include the one-second final hold.");
            }

            RequireNoHorizontalRootTranslation(prefab.transform, prefabRenderer, clip);
            var holdChange = RequireDeathHold(clip, sourceLength);
            var armatureYChange = RequireDeathArmatureYStable(clip, prefab);
            var armPose = CaptureDeathArmLocalPose(prefab, clip, sourceLength);
            var armHoldChange = Mathf.Max(
                DeathArmPoseDifference(
                    armPose,
                    CaptureDeathArmLocalPose(
                        prefab,
                        clip,
                        sourceLength + DeathHoldSeconds * 0.5f)),
                DeathArmPoseDifference(
                    armPose,
                    CaptureDeathArmLocalPose(
                        prefab,
                        clip,
                        clip.length - 0.0001f)));
            if (armHoldChange > 0.001f)
            {
                throw new InvalidOperationException(
                    "The floor-facing right-arm angle changes during the hold.");
            }
            WriteDeathVerticalFixReport(
                sourceClip,
                clip,
                prefabRenderer.sharedMesh,
                appearance,
                model,
                staticModel,
                holdChange,
                armatureYChange,
                armHoldChange,
                armPose);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur death validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurDeathAnimationValidated Result=PASS" +
                ", Clip=" + clip.name +
                ", SourceLength=" + NumDeath(sourceLength) +
                ", HoldSeconds=" + NumDeath(DeathHoldSeconds) +
                ", ArmatureYCurve=False" +
                ", ArmatureYChange=" + NumDeath(armatureYChange) +
                ", RightArmFloorAngleHoldChange=" + NumDeath(armHoldChange) +
                ", HoldCurveChange=" + NumDeath(holdChange) +
                ", SceneChanged=False, Report=" + DeathReportPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Inspect Death Vertical Motion")]
        public static void InspectPahurDeathVerticalMotion()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var model = RequireModel(
                RequireChild(placement.transform, DeathSlotName));
            var renderer = RequireRenderer(model, DeathSlotName);
            var animator = model.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "The Pahur death model has no Animator.");
            var controller = animator.runtimeAnimatorController as AnimatorController ??
                             throw new InvalidOperationException(
                                 "The Pahur death controller is missing.");
            var clip = controller.layers[0].stateMachine.defaultState.motion as AnimationClip ??
                       throw new InvalidOperationException(
                           "The Pahur death clip is missing.");
            var armature = renderer.rootBone.parent ??
                           throw new InvalidOperationException(
                               "The Pahur death root bone has no Armature parent.");
            var rightArm = renderer.bones.Single(bone => bone.name == "RightArm");
            var rightForeArm = renderer.bones.Single(
                bone => bone.name == "RightForeArm");
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item))
                .ToArray();
            var samples = new[]
            {
                0f,
                2.5f,
                2.85f,
                2.95f,
                3.05f,
                3.1f,
                3.6f,
                Mathf.Max(0f, clip.length - 0.0001f)
            };
            var diagnostics = new StringBuilder();
            try
            {
                foreach (var time in samples)
                {
                    clip.SampleAnimation(animator.gameObject, time);
                    diagnostics.Append(
                        " Time=" + NumDeath(time) +
                        " ModelLocalY=" + NumDeath(model.localPosition.y) +
                        " ArmatureLocalY=" + NumDeath(armature.localPosition.y) +
                        " ArmatureWorldY=" + NumDeath(armature.position.y) +
                        " HipsInModelY=" +
                        NumDeath(model.InverseTransformPoint(renderer.rootBone.position).y) +
                        " RightArmRotation=" + rightArm.localRotation.ToString("R") +
                        " RightForeArmRotation=" +
                        rightForeArm.localRotation.ToString("R"));
                }
            }
            finally
            {
                foreach (var state in states)
                {
                    state.Restore();
                }
            }

            var yBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    binding.propertyName.EndsWith("Position.y", StringComparison.Ordinal) ||
                    binding.propertyName == "RootT.y" ||
                    binding.propertyName == "MotionT.y")
                .Select(binding => binding.path + ":" + binding.propertyName)
                .ToArray();
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur death vertical inspection changed the scene dirty state.");
            }

            Debug.Log(
                "PahurDeathVerticalMotionInspection Result=PASS" +
                ", YBindings=" + string.Join("|", yBindings) +
                "," + diagnostics +
                " SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture Death Review")]
        public static void CapturePahurDeathReview()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var model = RequireModel(
                RequireChild(placement.transform, DeathSlotName));
            var animator = model.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "The Pahur death model has no Animator.");
            var controller = animator.runtimeAnimatorController as AnimatorController ??
                             throw new InvalidOperationException(
                                 "The Pahur death controller is missing.");
            var clip = controller.layers[0].stateMachine.defaultState.motion as AnimationClip ??
                       throw new InvalidOperationException(
                           "The Pahur death clip is missing.");
            var destination = Absolute(DeathCapturePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Pahur death review already exists: " + DeathCapturePath);
            }

            Capture(model, animator, clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur death capture changed the scene dirty state.");
            }

            Debug.Log(
                "PahurDeathReviewCaptured Result=PASS, Image=" +
                DeathCapturePath + ", SceneChanged=False.");
        }

        private static Mesh CreateDeathAppearanceMesh(
            SkinnedMeshRenderer sourceRenderer)
        {
            var approved =
                AssetDatabase.LoadAssetAtPath<Mesh>(ApprovedRunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The approved Pahur appearance mesh is missing.");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running Pahur FBX is missing.");
            RequireExactMiniTransferContract(
                sourceRenderer,
                RequireRenderer(runningPrefab.transform, "approved running FBX"));
            var source = sourceRenderer.sharedMesh;
            var generated = UnityEngine.Object.Instantiate(source);
            generated.name = "PahurDeathApprovedAppearanceMesh";
            var approvedUv3 = new List<Vector4>();
            approved.GetUVs(3, approvedUv3);
            if (approvedUv3.Count != source.vertexCount)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The approved static-derived Pahur appearance channel differs.");
            }

            generated.SetUVs(3, approvedUv3);
            generated.subMeshCount = approved.subMeshCount;
            for (var index = 0; index < approved.subMeshCount; index++)
            {
                generated.SetTriangles(approved.GetTriangles(index), index, false);
            }

            generated.bounds = source.bounds;
            if (AssetDatabase.LoadAssetAtPath<Mesh>(DeathAppearanceMeshPath) != null &&
                !AssetDatabase.DeleteAsset(DeathAppearanceMeshPath))
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The previous Pahur death appearance mesh could not be replaced.");
            }

            AssetDatabase.CreateAsset(generated, DeathAppearanceMeshPath);
            AssetDatabase.SaveAssets();
            RequireMiniAppearancePreserved(source, generated);
            return generated;
        }

        private static AnimationClip CreateDeathInPlaceHoldClip(
            AnimationClip source,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, DeathClipPath);
            }

            EditorUtility.CopySerialized(source, clip);
            clip.name = "Pahur_11_Death_InPlace";
            clip.wrapMode = WrapMode.Loop;
            var rootPath = AnimationUtility.CalculateTransformPath(
                renderer.rootBone,
                root);
            var horizontalProperties = HorizontalLocalPositionProperties(
                root,
                renderer.rootBone.parent);
            var horizontalBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    (binding.path.Length == 0 &&
                     (binding.propertyName == "RootT.x" ||
                      binding.propertyName == "RootT.z" ||
                      binding.propertyName == "MotionT.x" ||
                      binding.propertyName == "MotionT.z")) ||
                    (binding.path == rootPath &&
                     horizontalProperties.Contains(binding.propertyName)))
                .ToArray();
            if (horizontalBindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "The Pahur death Mixamo clip has no horizontal root curves to lock.");
            }

            foreach (var binding in horizontalBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "A Pahur death horizontal root curve is missing.");
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.Constant(0f, source.length, curve.Evaluate(0f)));
            }

            ExtendDeathCurvesForHold(clip, source.length);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void ExtendDeathCurvesForHold(
            AnimationClip clip,
            float sourceLength)
        {
            var holdEnd = sourceLength + DeathHoldSeconds;
            var finalSampleTime = Mathf.Max(0f, sourceLength - 0.0001f);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "A Pahur death animation curve is missing.");
                var value = curve.Evaluate(finalSampleTime);
                var keys = curve.keys
                    .Where(key => key.time < sourceLength - 0.00001f)
                    .Concat(new[]
                    {
                        new Keyframe(sourceLength, value, 0f, 0f),
                        new Keyframe(holdEnd, value, 0f, 0f)
                    })
                    .OrderBy(key => key.time)
                    .ToArray();
                var extended = new AnimationCurve(keys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(clip, binding, extended);
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys == null || keys.Length == 0)
                {
                    continue;
                }

                var value = keys
                    .Where(key => key.time <= finalSampleTime + 0.00001f)
                    .OrderBy(key => key.time)
                    .Last()
                    .value;
                var extended = keys
                    .Where(key => key.time < sourceLength - 0.00001f)
                    .Concat(new[]
                    {
                        new ObjectReferenceKeyframe
                        {
                            time = sourceLength,
                            value = value
                        },
                        new ObjectReferenceKeyframe
                        {
                            time = holdEnd,
                            value = value
                        }
                    })
                    .OrderBy(key => key.time)
                    .ToArray();
                AnimationUtility.SetObjectReferenceCurve(clip, binding, extended);
            }
        }

        private static float AuthorDeathRightArmFloorAngle(
            AnimationClip clip,
            float sourceLength,
            GameObject prefab)
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var renderer = RequireRenderer(clone.transform, "death arm angle authoring");
                var rightArm = renderer.bones.Single(bone => bone.name == "RightArm");
                var rightForeArm = renderer.bones.Single(
                    bone => bone.name == "RightForeArm");
                clip.SampleAnimation(clone, sourceLength);
                var baseRotation = rightArm.localRotation;
                var baseForeArmRotation = rightForeArm.localRotation;
                var correction =
                    DeathRightArmFinalRotation * Quaternion.Inverse(baseRotation);
                var foreCorrection =
                    DeathRightForeArmFinalRotation *
                    Quaternion.Inverse(baseForeArmRotation);
                var rightArmPath = AnimationUtility.CalculateTransformPath(
                    rightArm,
                    clone.transform);
                var rightForeArmPath = AnimationUtility.CalculateTransformPath(
                    rightForeArm,
                    clone.transform);
                var sampleCount = Mathf.Max(
                    2,
                    Mathf.CeilToInt(sourceLength * Mathf.Max(1f, clip.frameRate)) + 1);
                var xKeys = new List<Keyframe>(sampleCount + 1);
                var yKeys = new List<Keyframe>(sampleCount + 1);
                var zKeys = new List<Keyframe>(sampleCount + 1);
                var wKeys = new List<Keyframe>(sampleCount + 1);
                var foreXKeys = new List<Keyframe>(sampleCount + 1);
                var foreYKeys = new List<Keyframe>(sampleCount + 1);
                var foreZKeys = new List<Keyframe>(sampleCount + 1);
                var foreWKeys = new List<Keyframe>(sampleCount + 1);
                var previous = Quaternion.identity;
                var previousFore = Quaternion.identity;
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = sourceLength * index / (sampleCount - 1f);
                    clip.SampleAnimation(clone, time);
                    var alpha = Mathf.Clamp01(
                        (time - (sourceLength - DeathArmSettleSeconds)) /
                        DeathArmSettleSeconds);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    var adjusted =
                        Quaternion.Slerp(Quaternion.identity, correction, alpha) *
                        rightArm.localRotation;
                    var adjustedFore =
                        Quaternion.Slerp(
                            Quaternion.identity,
                            foreCorrection,
                            alpha) *
                        rightForeArm.localRotation;
                    if (index > 0 && Quaternion.Dot(previous, adjusted) < 0f)
                    {
                        adjusted = new Quaternion(
                            -adjusted.x,
                            -adjusted.y,
                            -adjusted.z,
                            -adjusted.w);
                    }
                    if (index > 0 &&
                        Quaternion.Dot(previousFore, adjustedFore) < 0f)
                    {
                        adjustedFore = new Quaternion(
                            -adjustedFore.x,
                            -adjustedFore.y,
                            -adjustedFore.z,
                            -adjustedFore.w);
                    }

                    previous = adjusted;
                    previousFore = adjustedFore;
                    xKeys.Add(new Keyframe(time, adjusted.x));
                    yKeys.Add(new Keyframe(time, adjusted.y));
                    zKeys.Add(new Keyframe(time, adjusted.z));
                    wKeys.Add(new Keyframe(time, adjusted.w));
                    foreXKeys.Add(new Keyframe(time, adjustedFore.x));
                    foreYKeys.Add(new Keyframe(time, adjustedFore.y));
                    foreZKeys.Add(new Keyframe(time, adjustedFore.z));
                    foreWKeys.Add(new Keyframe(time, adjustedFore.w));
                }

                var holdEnd = sourceLength + DeathHoldSeconds;
                xKeys.Add(new Keyframe(holdEnd, previous.x));
                yKeys.Add(new Keyframe(holdEnd, previous.y));
                zKeys.Add(new Keyframe(holdEnd, previous.z));
                wKeys.Add(new Keyframe(holdEnd, previous.w));
                foreXKeys.Add(new Keyframe(holdEnd, previousFore.x));
                foreYKeys.Add(new Keyframe(holdEnd, previousFore.y));
                foreZKeys.Add(new Keyframe(holdEnd, previousFore.z));
                foreWKeys.Add(new Keyframe(holdEnd, previousFore.w));
                RemoveBreakthroughRotationCurves(clip, rightArmPath);
                RemoveBreakthroughRotationCurves(clip, rightForeArmPath);
                SetQuaternionCurve(clip, rightArmPath, "x", xKeys.ToArray());
                SetQuaternionCurve(clip, rightArmPath, "y", yKeys.ToArray());
                SetQuaternionCurve(clip, rightArmPath, "z", zKeys.ToArray());
                SetQuaternionCurve(clip, rightArmPath, "w", wKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "x",
                    foreXKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "y",
                    foreYKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "z",
                    foreZKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "w",
                    foreWKeys.ToArray());
                clip.EnsureQuaternionContinuity();
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }

            var pose = CaptureDeathArmLocalPose(prefab, clip, sourceLength);
            var error = Mathf.Max(
                Quaternion.Angle(pose.RightArm, DeathRightArmFinalRotation),
                Quaternion.Angle(
                    pose.RightForeArm,
                    DeathRightForeArmFinalRotation));
            if (error > 0.001f)
            {
                throw new InvalidOperationException(
                    "The approved floor-facing right-arm angle was not preserved. Error=" +
                    NumDeath(error) + ".");
            }

            return error;
        }

        private static float RequireDeathArmatureYStable(
            AnimationClip clip,
            GameObject prefab)
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var renderer = RequireRenderer(clone.transform, "death vertical validation");
                var armature = renderer.rootBone.parent ??
                               throw new InvalidOperationException(
                                   "The Pahur death root bone has no Armature parent.");
                var armaturePath = AnimationUtility.CalculateTransformPath(
                    armature,
                    clone.transform);
                if (AnimationUtility.GetCurveBindings(clip).Any(binding =>
                        binding.path == armaturePath &&
                        binding.propertyName == "m_LocalPosition.y"))
                {
                    throw new InvalidOperationException(
                        "The Pahur death clip still animates Armature Y.");
                }

                var initial = armature.localPosition.y;
                var maximum = 0f;
                for (var index = 0; index <= 16; index++)
                {
                    clip.SampleAnimation(clone, clip.length * index / 16f);
                    maximum = Mathf.Max(
                        maximum,
                        Mathf.Abs(armature.localPosition.y - initial));
                }

                if (maximum > 0.00001f)
                {
                    throw new InvalidOperationException(
                        "The Pahur death Armature Y changes. Change=" +
                        NumDeath(maximum) + ".");
                }

                return maximum;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static DeathArmLocalPose CaptureDeathArmLocalPose(
            GameObject prefab,
            AnimationClip clip,
            float time)
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                clip.SampleAnimation(clone, time);
                var renderer = RequireRenderer(clone.transform, "death arm pose validation");
                return new DeathArmLocalPose(
                    renderer.bones.Single(bone => bone.name == "RightArm").localRotation,
                    renderer.bones.Single(
                        bone => bone.name == "RightForeArm").localRotation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static float DeathArmPoseDifference(
            DeathArmLocalPose expected,
            DeathArmLocalPose actual)
        {
            return Mathf.Max(
                Quaternion.Angle(expected.RightArm, actual.RightArm),
                Quaternion.Angle(expected.RightForeArm, actual.RightForeArm));
        }

        private static DeathArmSurfaceMetrics AuthorDeathRightArmGroundContact(
            AnimationClip clip,
            float sourceLength,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            float modelScale,
            float modelWorldY,
            float floorWorldY)
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.position = new Vector3(0f, modelWorldY, 0f);
            clone.transform.localScale = Vector3.one * modelScale;
            var baked = new Mesh();
            try
            {
                var renderer = RequireRenderer(clone.transform, "death arm authoring");
                var indices = RequireDeathArmSurfaceIndices(renderer, 0.5f);
                var rightArm = renderer.bones.Single(bone => bone.name == "RightArm");
                var rightForeArm = renderer.bones.Single(
                    bone => bone.name == "RightForeArm");
                clip.SampleAnimation(clone, sourceLength);
                var baseRotation = rightArm.localRotation;
                var baseForeArmRotation = rightForeArm.localRotation;
                var best = FindDeathArmContactRotation(
                    renderer,
                    rightArm,
                    baseRotation,
                    rightForeArm,
                    baseForeArmRotation,
                    indices,
                    baked,
                    floorWorldY);
                rightArm.localRotation = baseRotation;
                rightForeArm.localRotation = baseForeArmRotation;
                var rightArmPath = AnimationUtility.CalculateTransformPath(
                    rightArm,
                    clone.transform);
                var rightForeArmPath = AnimationUtility.CalculateTransformPath(
                    rightForeArm,
                    clone.transform);
                var sampleCount = Mathf.Max(
                    2,
                    Mathf.CeilToInt(sourceLength * Mathf.Max(1f, clip.frameRate)) + 1);
                var xKeys = new List<Keyframe>(sampleCount + 1);
                var yKeys = new List<Keyframe>(sampleCount + 1);
                var zKeys = new List<Keyframe>(sampleCount + 1);
                var wKeys = new List<Keyframe>(sampleCount + 1);
                var foreXKeys = new List<Keyframe>(sampleCount + 1);
                var foreYKeys = new List<Keyframe>(sampleCount + 1);
                var foreZKeys = new List<Keyframe>(sampleCount + 1);
                var foreWKeys = new List<Keyframe>(sampleCount + 1);
                var correction =
                    best.Arm.Rotation * Quaternion.Inverse(baseRotation);
                var foreCorrection =
                    best.ForeArm.Rotation * Quaternion.Inverse(baseForeArmRotation);
                var previous = Quaternion.identity;
                var previousFore = Quaternion.identity;
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = sourceLength * index / (sampleCount - 1f);
                    clip.SampleAnimation(clone, time);
                    var sampled = rightArm.localRotation;
                    var sampledFore = rightForeArm.localRotation;
                    var alpha = Mathf.Clamp01(
                        (time - (sourceLength - DeathArmSettleSeconds)) /
                        DeathArmSettleSeconds);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    var adjusted =
                        Quaternion.Slerp(Quaternion.identity, correction, alpha) *
                        sampled;
                    var adjustedFore =
                        Quaternion.Slerp(
                            Quaternion.identity,
                            foreCorrection,
                            alpha) *
                        sampledFore;
                    if (index > 0 && Quaternion.Dot(previous, adjusted) < 0f)
                    {
                        adjusted = new Quaternion(
                            -adjusted.x,
                            -adjusted.y,
                            -adjusted.z,
                            -adjusted.w);
                    }

                    if (index > 0 &&
                        Quaternion.Dot(previousFore, adjustedFore) < 0f)
                    {
                        adjustedFore = new Quaternion(
                            -adjustedFore.x,
                            -adjustedFore.y,
                            -adjustedFore.z,
                            -adjustedFore.w);
                    }

                    previous = adjusted;
                    previousFore = adjustedFore;
                    xKeys.Add(new Keyframe(time, adjusted.x));
                    yKeys.Add(new Keyframe(time, adjusted.y));
                    zKeys.Add(new Keyframe(time, adjusted.z));
                    wKeys.Add(new Keyframe(time, adjusted.w));
                    foreXKeys.Add(new Keyframe(time, adjustedFore.x));
                    foreYKeys.Add(new Keyframe(time, adjustedFore.y));
                    foreZKeys.Add(new Keyframe(time, adjustedFore.z));
                    foreWKeys.Add(new Keyframe(time, adjustedFore.w));
                }

                var holdEnd = sourceLength + DeathHoldSeconds;
                xKeys.Add(new Keyframe(holdEnd, previous.x));
                yKeys.Add(new Keyframe(holdEnd, previous.y));
                zKeys.Add(new Keyframe(holdEnd, previous.z));
                wKeys.Add(new Keyframe(holdEnd, previous.w));
                foreXKeys.Add(new Keyframe(holdEnd, previousFore.x));
                foreYKeys.Add(new Keyframe(holdEnd, previousFore.y));
                foreZKeys.Add(new Keyframe(holdEnd, previousFore.z));
                foreWKeys.Add(new Keyframe(holdEnd, previousFore.w));
                RemoveBreakthroughRotationCurves(clip, rightArmPath);
                RemoveBreakthroughRotationCurves(clip, rightForeArmPath);
                SetQuaternionCurve(clip, rightArmPath, "x", xKeys.ToArray());
                SetQuaternionCurve(clip, rightArmPath, "y", yKeys.ToArray());
                SetQuaternionCurve(clip, rightArmPath, "z", zKeys.ToArray());
                SetQuaternionCurve(clip, rightArmPath, "w", wKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "x",
                    foreXKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "y",
                    foreYKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "z",
                    foreZKeys.ToArray());
                SetQuaternionCurve(
                    clip,
                    rightForeArmPath,
                    "w",
                    foreWKeys.ToArray());
                clip.EnsureQuaternionContinuity();
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            var metrics = AnalyzeDeathRightArmSurface(
                prefab,
                prefabRenderer,
                clip,
                modelScale,
                modelWorldY,
                floorWorldY,
                sourceLength);
            RequireDeathArmContact(metrics, "authored final pose");
            return metrics;
        }

        private static float AuthorDeathGroundedFinalPose(
            AnimationClip clip,
            float sourceLength,
            GameObject prefab,
            float modelScale,
            float modelWorldY,
            float floorWorldY)
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.position = new Vector3(0f, modelWorldY, 0f);
            clone.transform.localScale = Vector3.one * modelScale;
            var baked = new Mesh();
            try
            {
                var renderer = RequireRenderer(clone.transform, "death grounding authoring");
                var rootBone = renderer.rootBone ??
                               throw new InvalidOperationException(
                                   "The Pahur death renderer has no root bone.");
                var groundingRoot = rootBone.parent ??
                                    throw new InvalidOperationException(
                                        "The Pahur death root bone has no skeleton parent.");
                if (groundingRoot == clone.transform)
                {
                    throw new InvalidOperationException(
                        "The Pahur death skeleton has no separate grounding root.");
                }
                clip.SampleAnimation(clone, sourceLength);
                renderer.BakeMesh(baked);
                var minimum = baked.vertices.Min(vertex =>
                    renderer.transform.TransformPoint(vertex).y);
                var downward = minimum - floorWorldY;
                if (downward < -DeathArmContactTolerance)
                {
                    throw new InvalidOperationException(
                        "The source death pose already penetrates the floor.");
                }

                var baseFinal = groundingRoot.localPosition;
                var baseWorldY = groundingRoot.position.y;
                var desiredWorld = groundingRoot.position - Vector3.up * downward;
                var desiredLocal = groundingRoot.parent != null
                    ? groundingRoot.parent.InverseTransformPoint(desiredWorld)
                    : desiredWorld;
                var correction = (desiredLocal - baseFinal) / modelScale;
                var path = AnimationUtility.CalculateTransformPath(
                    groundingRoot,
                    clone.transform);
                var sampleCount = Mathf.Max(
                    2,
                    Mathf.CeilToInt(sourceLength * Mathf.Max(1f, clip.frameRate)) + 1);
                var xKeys = new List<Keyframe>(sampleCount + 1);
                var yKeys = new List<Keyframe>(sampleCount + 1);
                var zKeys = new List<Keyframe>(sampleCount + 1);
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = sourceLength * index / (sampleCount - 1f);
                    clip.SampleAnimation(clone, time);
                    var alpha = Mathf.Clamp01(
                        (time - (sourceLength - DeathArmSettleSeconds)) /
                        DeathArmSettleSeconds);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    var adjusted = groundingRoot.localPosition + correction * alpha;
                    xKeys.Add(new Keyframe(time, adjusted.x));
                    yKeys.Add(new Keyframe(time, adjusted.y));
                    zKeys.Add(new Keyframe(time, adjusted.z));
                }

                var holdEnd = sourceLength + DeathHoldSeconds;
                var final = new Vector3(
                    xKeys[xKeys.Count - 1].value,
                    yKeys[yKeys.Count - 1].value,
                    zKeys[zKeys.Count - 1].value);
                xKeys.Add(new Keyframe(holdEnd, final.x));
                yKeys.Add(new Keyframe(holdEnd, final.y));
                zKeys.Add(new Keyframe(holdEnd, final.z));
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                             .Where(binding =>
                                 binding.path == path &&
                                 binding.propertyName.StartsWith(
                                     "m_LocalPosition.",
                                     StringComparison.Ordinal))
                             .ToArray())
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                }

                SetDeathPositionCurve(clip, path, "x", xKeys.ToArray());
                SetDeathPositionCurve(clip, path, "y", yKeys.ToArray());
                SetDeathPositionCurve(clip, path, "z", zKeys.ToArray());
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();

                clip.SampleAnimation(clone, sourceLength);
                renderer.BakeMesh(baked);
                var groundedMinimum = baked.vertices.Min(vertex =>
                    renderer.transform.TransformPoint(vertex).y);
                if (Mathf.Abs(groundedMinimum - floorWorldY) >
                    DeathArmContactTolerance)
                {
                    throw new InvalidOperationException(
                        "The final death pose was not grounded. Gap=" +
                        NumDeath(groundedMinimum - floorWorldY) +
                        ", InitialGap=" + NumDeath(downward) +
                        ", GroundingPath=" + path +
                        ", RootWorldDelta=" +
                        NumDeath(groundingRoot.position.y - baseWorldY) +
                        ", Correction=" + ScaleText(correction) + ".");
                }

                return downward;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void SetDeathPositionCurve(
            AnimationClip clip,
            string path,
            string suffix,
            Keyframe[] keys)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalPosition." + suffix),
                new AnimationCurve(keys));
        }

        private static DeathArmPose FindDeathArmContactRotation(
            SkinnedMeshRenderer renderer,
            Transform rightArm,
            Quaternion baseRotation,
            Transform rightForeArm,
            Quaternion baseForeArmRotation,
            int[] indices,
            Mesh baked,
            float floorWorldY)
        {
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            var best = new DeathArmRotation(
                baseRotation,
                0,
                0f,
                DeathArmMinimumWorldY(renderer, indices, baked),
                float.PositiveInfinity);
            for (var axis = 0; axis < axes.Length; axis++)
            {
                for (var angle = -170f; angle <= 170.001f; angle += 2f)
                {
                    var rotation = Quaternion.AngleAxis(angle, axes[axis]) * baseRotation;
                    rightArm.localRotation = rotation;
                    var minimum = DeathArmMinimumWorldY(renderer, indices, baked);
                    var score = DeathArmContactScore(minimum - floorWorldY, angle);
                    if (score < best.Score)
                    {
                        best = new DeathArmRotation(rotation, axis, angle, minimum, score);
                    }
                }
            }

            for (var angle = best.Angle - 2f;
                 angle <= best.Angle + 2.0001f;
                 angle += 0.02f)
            {
                var rotation =
                    Quaternion.AngleAxis(angle, axes[best.Axis]) * baseRotation;
                rightArm.localRotation = rotation;
                var minimum = DeathArmMinimumWorldY(renderer, indices, baked);
                var score = DeathArmContactScore(minimum - floorWorldY, angle);
                if (score < best.Score)
                {
                    best = new DeathArmRotation(rotation, best.Axis, angle, minimum, score);
                }
            }

            rightArm.localRotation = best.Rotation;
            rightForeArm.localRotation = baseForeArmRotation;
            var bestFore = new DeathArmRotation(
                baseForeArmRotation,
                0,
                0f,
                DeathArmMinimumWorldY(renderer, indices, baked),
                float.PositiveInfinity);
            for (var axis = 0; axis < axes.Length; axis++)
            {
                for (var angle = -170f; angle <= 170.001f; angle += 2f)
                {
                    var rotation =
                        Quaternion.AngleAxis(angle, axes[axis]) *
                        baseForeArmRotation;
                    rightForeArm.localRotation = rotation;
                    var minimum = DeathArmMinimumWorldY(renderer, indices, baked);
                    var score = DeathArmContactScore(minimum - floorWorldY, angle);
                    if (score < bestFore.Score)
                    {
                        bestFore = new DeathArmRotation(
                            rotation,
                            axis,
                            angle,
                            minimum,
                            score);
                    }
                }
            }

            for (var angle = bestFore.Angle - 2f;
                 angle <= bestFore.Angle + 2.0001f;
                 angle += 0.02f)
            {
                var rotation =
                    Quaternion.AngleAxis(angle, axes[bestFore.Axis]) *
                    baseForeArmRotation;
                rightForeArm.localRotation = rotation;
                var minimum = DeathArmMinimumWorldY(renderer, indices, baked);
                var score = DeathArmContactScore(minimum - floorWorldY, angle);
                if (score < bestFore.Score)
                {
                    bestFore = new DeathArmRotation(
                        rotation,
                        bestFore.Axis,
                        angle,
                        minimum,
                        score);
                }
            }

            rightForeArm.localRotation = bestFore.Rotation;
            var armBaseAfterFirstAxis = best.Rotation;
            var combinedBest = new DeathArmRotation(
                best.Rotation,
                0,
                0f,
                DeathArmMinimumWorldY(renderer, indices, baked),
                float.PositiveInfinity);
            for (var axis = 0; axis < axes.Length; axis++)
            {
                for (var angle = -35f; angle <= 35.001f; angle += 0.5f)
                {
                    var rotation =
                        Quaternion.AngleAxis(angle, axes[axis]) *
                        armBaseAfterFirstAxis;
                    rightArm.localRotation = rotation;
                    var minimum = DeathArmMinimumWorldY(renderer, indices, baked);
                    var score = DeathArmContactScore(minimum - floorWorldY, angle);
                    if (score < combinedBest.Score)
                    {
                        combinedBest = new DeathArmRotation(
                            rotation,
                            axis,
                            angle,
                            minimum,
                            score);
                    }
                }
            }

            for (var angle = combinedBest.Angle - 0.5f;
                 angle <= combinedBest.Angle + 0.5001f;
                 angle += 0.01f)
            {
                var rotation =
                    Quaternion.AngleAxis(angle, axes[combinedBest.Axis]) *
                    armBaseAfterFirstAxis;
                rightArm.localRotation = rotation;
                var minimum = DeathArmMinimumWorldY(renderer, indices, baked);
                var score = DeathArmContactScore(minimum - floorWorldY, angle);
                if (score < combinedBest.Score)
                {
                    combinedBest = new DeathArmRotation(
                        rotation,
                        combinedBest.Axis,
                        angle,
                        minimum,
                        score);
                }
            }

            best = combinedBest;
            rightArm.localRotation = best.Rotation;
            var finalMinimum = DeathArmMinimumWorldY(renderer, indices, baked);
            if (Mathf.Abs(finalMinimum - floorWorldY) >
                DeathArmContactTolerance)
            {
                throw new InvalidOperationException(
                    "The right-arm side could not be rotated onto the floor. Gap=" +
                    NumDeath(finalMinimum - floorWorldY) + ".");
            }

            return new DeathArmPose(best, bestFore);
        }

        private static float DeathArmContactScore(float gap, float angle)
        {
            var contact = gap >= -DeathArmContactTolerance
                ? Mathf.Abs(gap)
                : Mathf.Abs(gap) * 8f;
            return contact + Mathf.Abs(angle) * 0.000001f;
        }

        private static float DeathArmMinimumWorldY(
            SkinnedMeshRenderer renderer,
            int[] indices,
            Mesh baked)
        {
            renderer.BakeMesh(baked);
            var vertices = baked.vertices;
            return indices.Min(index =>
                renderer.transform.TransformPoint(vertices[index]).y);
        }

        private static int[] RequireDeathArmSurfaceIndices(
            SkinnedMeshRenderer renderer,
            float threshold)
        {
            var armIndices = renderer.bones
                .Select((bone, index) => new { bone.name, index })
                .Where(item =>
                    item.name == "RightArm" || item.name == "RightForeArm")
                .Select(item => item.index)
                .ToArray();
            if (armIndices.Length != 2)
            {
                throw new InvalidOperationException(
                    "The death FBX must contain RightArm and RightForeArm bones.");
            }

            var armSet = armIndices.ToHashSet();
            var indices = renderer.sharedMesh.boneWeights
                .Select((weight, index) => new
                {
                    Weight = ArmWeight(weight, armSet),
                    Index = index
                })
                .Where(item => item.Weight >= threshold)
                .Select(item => item.Index)
                .ToArray();
            if (indices.Length == 0)
            {
                throw new InvalidOperationException(
                    "The death FBX has no weighted right-arm side vertices.");
            }

            return indices;
        }

        private static void RequireDeathArmContact(
            DeathArmSurfaceMetrics metrics,
            string label)
        {
            if (Mathf.Abs(metrics.Gap) > DeathArmContactTolerance)
            {
                throw new InvalidOperationException(
                    "The right-arm side does not touch the floor at " + label +
                    ". Gap=" + NumDeath(metrics.Gap) + ".");
            }
        }

        private static float RequireDeathHold(
            AnimationClip clip,
            float sourceLength)
        {
            var maximum = 0f;
            var middle = sourceLength + DeathHoldSeconds * 0.5f;
            var end = sourceLength + DeathHoldSeconds;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "A Pahur death hold curve is missing.");
                var startValue = curve.Evaluate(sourceLength);
                maximum = Mathf.Max(
                    maximum,
                    Mathf.Max(
                        Mathf.Abs(startValue - curve.Evaluate(middle)),
                        Mathf.Abs(startValue - curve.Evaluate(end))));
            }

            if (maximum > 0.00001f)
            {
                throw new InvalidOperationException(
                    "The Pahur death final pose is not held for one second. Change=" +
                    NumDeath(maximum) + ".");
            }

            return maximum;
        }

        private static AnimatorController CreateDeathController(AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(DeathControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    DeathControllerPath);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            var state = machine.AddState(DeathStateName);
            state.motion = clip;
            state.speed = 1f;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void WriteDeathVerticalFixReport(
            AnimationClip sourceClip,
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Transform model,
            Transform staticModel,
            float holdChange,
            float armatureYChange,
            float armHoldChange,
            DeathArmLocalPose armPose)
        {
            var destination = Absolute(DeathReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur death report path."));
            var report = new StringBuilder();
            report.AppendLine("Pahur Death No-Vertical-Drop Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceSha256=" + SourceDeathSha256);
            report.AppendLine("ImportedSourceHashMatches=True");
            report.AppendLine("SourceClip=" + sourceClip.name);
            report.AppendLine("SourceLength=" + NumDeath(sourceClip.length));
            report.AppendLine("PlaybackClip=" + clip.name);
            report.AppendLine("PlaybackLength=" + NumDeath(clip.length));
            report.AppendLine("FinalPoseHoldSeconds=" + NumDeath(DeathHoldSeconds));
            report.AppendLine("Loop=True");
            report.AppendLine("ReturnMotion=False");
            report.AppendLine("Vertices=" + source.vertexCount);
            report.AppendLine("Triangles=" + source.triangles.Length / 3);
            report.AppendLine("Bones=" + source.bindposes.Length);
            report.AppendLine("ShapeSkinBindPosesPreserved=True");
            report.AppendLine(
                "StaticApprovedAppearanceTransferredByExactVertexIndex=True");
            report.AppendLine("NewAppearanceDataGenerated=False");
            report.AppendLine("ApprovedMaterialSlots=" + appearance.subMeshCount);
            report.AppendLine("SharedStaticMaterials=True");
            report.AppendLine("ModelScale=" + ScaleText(model.localScale));
            report.AppendLine("ModelY=" + NumDeath(model.localPosition.y));
            report.AppendLine("StaticY=" + NumDeath(staticModel.localPosition.y));
            report.AppendLine("HorizontalRootMotion=False");
            report.AppendLine("ApplyRootMotion=False");
            report.AppendLine("ArmaturePositionYCurve=False");
            report.AppendLine("ArmatureLocalYChange=" + NumDeath(armatureYChange));
            report.AppendLine("VerticalDropRemoved=True");
            report.AppendLine(
                "RightArmFloorAngle=" + armPose.RightArm.ToString("R"));
            report.AppendLine(
                "RightForeArmFloorAngle=" + armPose.RightForeArm.ToString("R"));
            report.AppendLine("RightArmFloorAnglePreserved=True");
            report.AppendLine(
                "RightArmFloorAngleHoldChangeDegrees=" + NumDeath(armHoldChange));
            report.AppendLine("HoldCurveMaximumChange=" + NumDeath(holdChange));
            report.AppendLine("FinalPoseHeldExactlyForOneSecond=True");
            report.AppendLine("OtherSlotsPreservedByApply=True");
            report.AppendLine("OtherSceneRootsPreservedByApply=True");
            report.AppendLine("SceneSaved=True");
            report.AppendLine("SceneChangedByValidation=False");
            File.WriteAllText(destination, report.ToString(), new UTF8Encoding(false));
        }

        private static void WriteDeathReport(
            AnimationClip sourceClip,
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Transform model,
            Transform staticModel,
            float holdChange,
            DeathArmSurfaceMetrics contactStart,
            DeathArmSurfaceMetrics contactMiddle,
            DeathArmSurfaceMetrics contactEnd)
        {
            var destination = Absolute(DeathReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur death report path."));
            var report = new StringBuilder();
            report.AppendLine("Pahur Death Animation Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceSha256=" + SourceDeathSha256);
            report.AppendLine("ImportedSourceHashMatches=True");
            report.AppendLine("SourceClip=" + sourceClip.name);
            report.AppendLine("SourceLength=" + NumDeath(sourceClip.length));
            report.AppendLine("PlaybackClip=" + clip.name);
            report.AppendLine("PlaybackLength=" + NumDeath(clip.length));
            report.AppendLine("FinalPoseHoldSeconds=" + NumDeath(DeathHoldSeconds));
            report.AppendLine("Loop=True");
            report.AppendLine("ReturnMotion=False");
            report.AppendLine("Vertices=" + source.vertexCount);
            report.AppendLine("Triangles=" + source.triangles.Length / 3);
            report.AppendLine("Bones=" + source.bindposes.Length);
            report.AppendLine("ShapeSkinBindPosesPreserved=True");
            report.AppendLine(
                "StaticApprovedAppearanceTransferredByExactVertexIndex=True");
            report.AppendLine("NewAppearanceDataGenerated=False");
            report.AppendLine("ApprovedMaterialSlots=" + appearance.subMeshCount);
            report.AppendLine("SharedStaticMaterials=True");
            report.AppendLine("ModelScale=" + ScaleText(model.localScale));
            report.AppendLine("ModelY=" + NumDeath(model.localPosition.y));
            report.AppendLine("StaticY=" + NumDeath(staticModel.localPosition.y));
            report.AppendLine("HorizontalRootMotion=False");
            report.AppendLine("ApplyRootMotion=False");
            report.AppendLine("RightArmSideDefinition=RightArmAndRightForeArmWeightAtLeast0.5");
            report.AppendLine("RightArmSideVertices=" + contactStart.Vertices50);
            report.AppendLine("FloorWorldY=" + NumDeath(contactStart.FloorWorldY));
            report.AppendLine("RightArmContactStartGap=" + NumDeath(contactStart.Gap));
            report.AppendLine("RightArmContactMiddleGap=" + NumDeath(contactMiddle.Gap));
            report.AppendLine("RightArmContactEndGap=" + NumDeath(contactEnd.Gap));
            report.AppendLine("RightArmSideTouchesFloorDuringHold=True");
            report.AppendLine("HoldCurveMaximumChange=" + NumDeath(holdChange));
            report.AppendLine("FinalPoseHeldExactlyForOneSecond=True");
            report.AppendLine("OtherSlotsPreservedByApply=True");
            report.AppendLine("OtherSceneRootsPreservedByApply=True");
            report.AppendLine("SceneSaved=True");
            report.AppendLine("SceneChangedByValidation=False");
            File.WriteAllText(destination, report.ToString(), new UTF8Encoding(false));
        }

        private static void RequireDeathSourceHash()
        {
            if (!File.Exists(SourceDeathModelPath) ||
                Sha256(SourceDeathModelPath) != SourceDeathSha256)
            {
                throw new InvalidOperationException(
                    "The supplied Pahur death FBX is missing or changed.");
            }
        }

        private static void ImportDeathModel()
        {
            var destination = Absolute(DeathModelPath);
            if (!File.Exists(destination) ||
                Sha256(destination) != SourceDeathSha256)
            {
                File.Copy(SourceDeathModelPath, destination, true);
            }

            AssetDatabase.ImportAsset(
                DeathModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureDeathImporter()
        {
            var importer =
                AssetImporter.GetAtPath(DeathModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "The Pahur death importer is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            var matches = importer.defaultClipAnimations
                .Where(item =>
                    item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.takeName.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Pahur death FBX must contain exactly one Mixamo take. Found=" +
                    matches.Length + ".");
            }

            var selected = matches[0];
            selected.loopTime = true;
            selected.loopPose = false;
            selected.wrapMode = WrapMode.Loop;
            selected.lockRootPositionXZ = true;
            selected.keepOriginalPositionXZ = true;
            importer.animationWrapMode = WrapMode.Loop;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
            return selected.name;
        }

        private static AnimationClip RequireDeathSourceClip(string takeName)
        {
            var matches = AssetDatabase.LoadAllAssetsAtPath(DeathModelPath)
                .OfType<AnimationClip>()
                .Where(item =>
                    !item.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                    (item.name == takeName ||
                     item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The configured Pahur death Mixamo clip is not unique.");
            }

            return matches[0];
        }

        private static DeathArmSurfaceMetrics AnalyzeDeathRightArmSurface(
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            AnimationClip clip,
            float modelScale,
            float modelWorldY,
            float floorWorldY)
        {
            return AnalyzeDeathRightArmSurface(
                prefab,
                prefabRenderer,
                clip,
                modelScale,
                modelWorldY,
                floorWorldY,
                Mathf.Max(0f, clip.length - 0.0001f));
        }

        private static DeathArmSurfaceMetrics AnalyzeDeathRightArmSurface(
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            AnimationClip clip,
            float modelScale,
            float modelWorldY,
            float floorWorldY,
            float sampleTime)
        {
            var armIndices = prefabRenderer.bones
                .Select((bone, index) => new { bone.name, index })
                .Where(item =>
                    item.name == "RightArm" || item.name == "RightForeArm")
                .Select(item => item.index)
                .ToArray();
            if (armIndices.Length != 2)
            {
                throw new InvalidOperationException(
                    "The death FBX must contain RightArm and RightForeArm bones.");
            }

            var armSet = armIndices.ToHashSet();
            var weights = prefabRenderer.sharedMesh.boneWeights;
            var vertexWeights = weights
                .Select(weight =>
                    ArmWeight(weight, armSet))
                .ToArray();
            var indices25 = vertexWeights
                .Select((weight, index) => new { weight, index })
                .Where(item => item.weight >= 0.25f)
                .Select(item => item.index)
                .ToArray();
            var count50 = vertexWeights.Count(weight => weight >= 0.5f);
            var indices50 = vertexWeights
                .Select((weight, index) => new { weight, index })
                .Where(item => item.weight >= 0.5f)
                .Select(item => item.index)
                .ToArray();
            if (indices25.Length == 0 || indices50.Length == 0)
            {
                throw new InvalidOperationException(
                    "The death FBX has no weighted right-arm surface vertices.");
            }

            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.position = new Vector3(0f, modelWorldY, 0f);
            clone.transform.localScale = Vector3.one * modelScale;
            var baked = new Mesh();
            try
            {
                clip.SampleAnimation(clone, sampleTime);
                var renderer = RequireRenderer(clone.transform, "death arm inspection");
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                var minimum = indices50
                    .Min(index =>
                        renderer.transform.TransformPoint(vertices[index]).y);
                return new DeathArmSurfaceMetrics(
                    indices25.Length,
                    count50,
                    floorWorldY,
                    minimum);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static float ArmWeight(BoneWeight weight, System.Collections.Generic.HashSet<int> arm)
        {
            var total = 0f;
            if (arm.Contains(weight.boneIndex0)) total += weight.weight0;
            if (arm.Contains(weight.boneIndex1)) total += weight.weight1;
            if (arm.Contains(weight.boneIndex2)) total += weight.weight2;
            if (arm.Contains(weight.boneIndex3)) total += weight.weight3;
            return total;
        }

        private static string DeathArmDiagnostics(
            GameObject prefab,
            AnimationClip clip,
            float modelScale,
            float modelWorldY)
        {
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.position = new Vector3(0f, modelWorldY, 0f);
            clone.transform.localScale = Vector3.one * modelScale;
            var baked = new Mesh();
            try
            {
                clip.SampleAnimation(clone, Mathf.Max(0f, clip.length - 0.0001f));
                var renderer = RequireRenderer(clone.transform, "death arm diagnostics");
                renderer.BakeMesh(baked);
                var minimum = baked.vertices.Min(vertex =>
                    renderer.transform.TransformPoint(vertex).y);
                string Bone(string name)
                {
                    var bone = renderer.bones.Single(item => item.name == name);
                    return name + "=" + ScaleText(bone.position);
                }

                return
                    "FinalWholeMeshMinWorldY=" + NumDeath(minimum) +
                    ", " + Bone("RightShoulder") +
                    ", " + Bone("RightArm") +
                    ", " + Bone("RightForeArm") +
                    ", " + Bone("RightHand");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static string NumDeath(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private readonly struct DeathArmSurfaceMetrics
        {
            public DeathArmSurfaceMetrics(
                int vertices25,
                int vertices50,
                float floorWorldY,
                float minimumWorldY)
            {
                Vertices25 = vertices25;
                Vertices50 = vertices50;
                FloorWorldY = floorWorldY;
                MinimumWorldY = minimumWorldY;
            }

            public int Vertices25 { get; }
            public int Vertices50 { get; }
            public float FloorWorldY { get; }
            public float MinimumWorldY { get; }
            public float Gap => MinimumWorldY - FloorWorldY;
        }

        private readonly struct DeathArmRotation
        {
            public DeathArmRotation(
                Quaternion rotation,
                int axis,
                float angle,
                float minimumWorldY,
                float score)
            {
                Rotation = rotation;
                Axis = axis;
                Angle = angle;
                MinimumWorldY = minimumWorldY;
                Score = score;
            }

            public Quaternion Rotation { get; }
            public int Axis { get; }
            public float Angle { get; }
            public float MinimumWorldY { get; }
            public float Score { get; }
        }

        private readonly struct DeathArmPose
        {
            public DeathArmPose(
                DeathArmRotation arm,
                DeathArmRotation foreArm)
            {
                Arm = arm;
                ForeArm = foreArm;
            }

            public DeathArmRotation Arm { get; }
            public DeathArmRotation ForeArm { get; }
        }

        private readonly struct DeathArmLocalPose
        {
            public DeathArmLocalPose(
                Quaternion rightArm,
                Quaternion rightForeArm)
            {
                RightArm = rightArm;
                RightForeArm = rightForeArm;
            }

            public Quaternion RightArm { get; }
            public Quaternion RightForeArm { get; }
        }
    }
}
