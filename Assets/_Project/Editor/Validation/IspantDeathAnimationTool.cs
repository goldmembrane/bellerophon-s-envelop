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

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantDeathAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string MoveSlotName = "Ispant_03_Move";
        private const string DeathSlotName = "Ispant_12_Death";
        private const string StaticModelName = "Ispant_Model";
        private const string MoveModelName = "Ispant_Move_Model";
        private const string DeathModelName = "Ispant_Death_Model";
        private const string BodyRendererName = "Ispant_Armed_Body";
        private const string SwordRootName = "Ispant_ApprovedLongSword";
        private const string SwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string SourceFbxPath = "enemies model/išpant death.fbx";
        private const string ProjectFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Death.fbx";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_12_Death_Mixamo.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_12_Death.controller";
        private const string DiagnosticPath =
            "docs/validation/ispant_death_sword_rigid_follow_2026-08-11/Ispant_12_Death_RigidSword_Diagnostic.png";
        private const string DetailDiagnosticPath =
            "docs/validation/ispant_death_sword_rigid_follow_2026-08-11/Ispant_12_Death_RigidSword_DetailDiagnostic.png";
        private const string FinalPath =
            "docs/validation/ispant_death_sword_rigid_follow_2026-08-11/Ispant_12_Death_RigidSword_Final.png";
        private const string DetailFinalPath =
            "docs/validation/ispant_death_sword_rigid_follow_2026-08-11/Ispant_12_Death_RigidSword_DetailFinal.png";
        private const string SourceSha256 =
            "BA9CA3D67FD7843DA5D965C5BC5846CD638DB62BDC2AA7CD25F93C024B6B4224";
        private const string ImportedClipName = "Ispant_12_Death_Mixamo_Source";
        private const string PlaybackClipName = "Ispant_12_Death_Mixamo";
        private const string StateName = "Ispant_12_Death_Mixamo";
        private const int ExpectedSlots = 12;
        private const float MotionEpsilon = 0.000001f;
        private const float TransformTolerance = 0.0001f;

        // These scales keep a visible left/right clearance while narrowing the
        // complete leg silhouette during the fall. Upper-leg rotation carries
        // the thigh armor inward without changing bone lengths.
        private const float FallenThighRootSeparationScale = 0.35f;
        private const float FallenKneeSeparationScale = 0.30f;
        private const float FallenFootSeparationScale = 0.28f;
        private const float FallDetectionDropFraction = 0.08f;
        private const float FallClosureDurationFraction = 0.22f;

        private static readonly float[] ReviewNormalizedTimes =
        {
            0f, 0.08f, 0.14f, 0.18f, 0.22f, 0.26f, 0.30f,
            0.40f, 0.50f, 0.625f, 0.75f, 0.875f, 1f
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 12 Death Replacement")]
        public static void ApplyIspant12DeathReplacement()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            _ = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                throw new InvalidOperationException("The supplied Ispant death FBX is unavailable.");
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                              throw new InvalidOperationException(
                                  "The supplied Ispant death model container is unavailable.");
            var sourceClip = RequireSourceClip();
            var playbackClip = CreateOrUpdatePlaybackClip(sourceClip);

            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var moveModel = RequireDirectChild(
                RequireSlot(placement.transform, MoveSlotName, 2), MoveModelName);
            var deathSlot = RequireSlot(placement.transform, DeathSlotName, 11);
            if (deathSlot.childCount != 1)
                throw new InvalidOperationException(
                    "Ispant slot 12 must contain exactly one model before replacement.");

            var otherSlotsBefore = OtherSlotSignatures(placement.transform, deathSlot);
            var slotBefore = new TransformSnapshot(deathSlot);
            var previous = deathSlot.GetChild(0);
            var replacement = PrefabUtility.InstantiatePrefab(modelPrefab, scene) as GameObject ??
                              throw new InvalidOperationException(
                                  "The static-appearance-compatible death model could not be instantiated.");
            replacement.name = DeathModelName;
            replacement.transform.SetParent(deathSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previous.localPosition, previous.localRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyExactStaticMaterials(staticModel, replacement.transform);
                FitToStaticReference(replacement.transform, staticModel);
                AuthorFallingFullLegClosure(replacement.transform, playbackClip);
                var swordFollow = CreateRigidHipSword(
                    staticModel, moveModel, replacement.transform);
                AuthorBodySwordFollow(replacement.transform, playbackClip, swordFollow);
                ConfigureAnimator(replacement.transform, CreateOrUpdateController(playbackClip));
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (deathSlot.childCount != 1 || deathSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The slot-12 replacement did not leave exactly one model.");
            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The slot-12 container transform changed during replacement.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, deathSlot),
                "An Ispant slot outside slot 12 changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(deathSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-12 replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = deathSlot.gameObject;
            Debug.Log(
                "Ispant12DeathReplacementApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + DeathSlotName +
                ", Source=" + SourceFbxPath +
                ", EmbeddedClip=mixamo.com, Loop=True" +
                ", StaticAppearanceShared=True, VisualGroundAlignedToStaticReference=True" +
                ", FallingThighKneeFootClosure=True" +
                ", SingleRigidSword=True, BodyPositionRotationSwordFollow=True" +
                ", SwordScaleCurves=False, SkinnedSwordRemoved=True" +
                ", OtherSlotsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 12 Sword Structure")]
        public static void InspectIspant12SwordStructure()
        {
            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var moveModel = RequireDirectChild(
                RequireSlot(placement.transform, MoveSlotName, 2), MoveModelName);
            var deathModel = RequireDirectChild(
                RequireSlot(placement.transform, DeathSlotName, 11), DeathModelName);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectFbxPath) ??
                         throw new InvalidOperationException(
                             "The supplied Ispant death model container is unavailable.");
            var builder = new StringBuilder();
            AppendRendererStructure(builder, "SourceFBX", source.transform);
            AppendRendererStructure(builder, "StaticScene", staticModel);
            AppendRendererStructure(builder, "MoveScene", moveModel);
            AppendRendererStructure(builder, "CurrentScene", deathModel);
            Debug.Log(builder.ToString());
        }

        private static void AppendRendererStructure(
            StringBuilder builder,
            string label,
            Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            builder.Append(label).Append(" Renderers=").Append(renderers.Length).AppendLine();
            foreach (var renderer in renderers)
            {
                var mesh = renderer is SkinnedMeshRenderer skinned
                    ? skinned.sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                builder.Append("- Type=").Append(renderer.GetType().Name)
                    .Append(", Path=").Append(AnimationUtility.CalculateTransformPath(renderer.transform, root))
                    .Append(", Mesh=").Append(mesh == null ? "<null>" : mesh.name)
                    .Append(", Vertices=").Append(mesh == null ? 0 : mesh.vertexCount)
                    .Append(", Materials=").Append(renderer.sharedMaterials.Length)
                    .Append(", LossyScale=").Append(renderer.transform.lossyScale.ToString("F6"))
                    .Append(", BoundsCenter=").Append(renderer.bounds.center.ToString("F6"))
                    .Append(", BoundsSize=").Append(renderer.bounds.size.ToString("F6"));
                if (renderer is SkinnedMeshRenderer skin)
                {
                    builder.Append(", Bones=").Append(skin.bones.Length)
                        .Append(", RootBone=")
                        .Append(skin.rootBone == null ? "<null>" : skin.rootBone.name)
                        .Append(", BoneNames=")
                        .Append(string.Join("|", skin.bones.Select(item =>
                            item == null ? "<null>" : item.name)));
                }
                builder.AppendLine();
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 12 Death Diagnostic")]
        public static void CaptureIspant12DeathDiagnostic()
        {
            var destination = Absolute(DiagnosticPath);
            var detailDestination = Absolute(DetailDiagnosticPath);
            if (File.Exists(destination))
                File.Delete(destination);
            if (File.Exists(detailDestination))
                File.Delete(detailDestination);
            CaptureVisualReview(destination);
            CaptureVisualReview(detailDestination, focusOnLegs: true);
            Debug.Log("Ispant12DeathDiagnosticCaptured Image=" + DiagnosticPath +
                      ", DetailImage=" + DetailDiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 12 Death Final")]
        public static void CaptureIspant12DeathFinal()
        {
            var destination = Absolute(FinalPath);
            var detailDestination = Absolute(DetailFinalPath);
            if (File.Exists(destination) || File.Exists(detailDestination))
                throw new InvalidOperationException("The one-time slot-12 death final already exists.");
            CaptureVisualReview(destination);
            CaptureVisualReview(detailDestination, focusOnLegs: true);
            Debug.Log("Ispant12DeathFinalCaptured Image=" + FinalPath +
                      ", DetailImage=" + DetailFinalPath + ".");
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                ProjectFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ProjectFbxPath) as ModelImporter ??
                           throw new InvalidOperationException("The supplied death ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importBlendShapes = true;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    "The supplied death FBX must expose exactly one animation take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The supplied death animation take is not Mixamo: " + clips[0].takeName + ".");
            clips[0].name = ImportedClipName;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = false;
            clips[0].lockRootPositionXZ = false;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ProjectFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported slot-12 Mixamo clip differs.");
            if (AssetDatabase.GetAssetPath(clips[0]) != ProjectFbxPath)
                throw new InvalidOperationException(
                    "The slot-12 animation is not loaded from the supplied death FBX.");
            return clips[0];
        }

        private static AnimationClip CreateOrUpdatePlaybackClip(AnimationClip source)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = PlaybackClipName };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                                  throw new InvalidOperationException(
                                      "A supplied death curve is missing.");
                var clonedCurve = new AnimationCurve(sourceCurve.keys)
                {
                    preWrapMode = sourceCurve.preWrapMode,
                    postWrapMode = sourceCurve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(clip, binding, clonedCurve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(
                    clip,
                    binding,
                    AnimationUtility.GetObjectReferenceCurve(source, binding));

            clip.name = PlaybackClipName;
            clip.frameRate = source.frameRate;
            clip.wrapMode = WrapMode.Loop;
            AnimationUtility.SetAnimationEvents(clip, AnimationUtility.GetAnimationEvents(source));
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void AuthorFallingFullLegClosure(Transform model, AnimationClip clip)
        {
            var bones = BuildUniqueTransformMap(model);
            var head = RequireMappedTransform(bones, "Head");
            var leftUpper = RequireMappedTransform(bones, "LeftUpLeg");
            var leftLower = RequireMappedTransform(bones, "LeftLeg");
            var leftFoot = RequireMappedTransform(bones, "LeftFoot");
            var rightUpper = RequireMappedTransform(bones, "RightUpLeg");
            var rightLower = RequireMappedTransform(bones, "RightLeg");
            var rightFoot = RequireMappedTransform(bones, "RightFoot");
            var controlled = new[] { leftUpper, leftLower, rightUpper, rightLower };
            var rendererBones = new HashSet<Transform>(
                RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName).bones);
            foreach (var bone in controlled)
            {
                if (!rendererBones.Contains(bone))
                    throw new InvalidOperationException(
                        "A full-leg closure bone is not used by the Ispant body renderer: " +
                        bone.name + ".");
            }

            var paths = controlled.Select(item =>
                AnimationUtility.CalculateTransformPath(item, model)).ToArray();
            var sampleCount = Mathf.Max(2, Mathf.RoundToInt(clip.length * clip.frameRate) + 1);
            var headHeights = new float[sampleCount];
            var rotations = controlled.Select(_ => new Quaternion[sampleCount]).ToArray();
            var leftUpperPositions = new Vector3[sampleCount];
            var rightUpperPositions = new Vector3[sampleCount];
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();

            try
            {
                for (var index = 0; index < sampleCount; index++)
                {
                    foreach (var snapshot in snapshots)
                        snapshot.Restore();
                    SampleClip(
                        model.gameObject,
                        clip,
                        clip.length * index / (sampleCount - 1f));
                    headHeights[index] = model.InverseTransformPoint(head.position).y;
                }
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();

                var firstHalfCount = Mathf.Max(2, sampleCount / 2);
                var uprightHeight = headHeights.Take(firstHalfCount).Max();
                var fallenHeight = headHeights.Skip(sampleCount / 2).Min();
                var totalDrop = uprightHeight - fallenHeight;
                if (totalDrop <= MotionEpsilon)
                    throw new InvalidOperationException(
                        "The supplied death animation does not contain a visible fall.");
                var fallThreshold = uprightHeight - totalDrop * FallDetectionDropFraction;
                var searchStart = Mathf.Clamp(
                    Mathf.RoundToInt((sampleCount - 1) * 0.3f),
                    0,
                    sampleCount - 2);
                var fallStartIndex = Enumerable.Range(
                        searchStart,
                        sampleCount - searchStart)
                    .FirstOrDefault(index => headHeights[index] <= fallThreshold);
                if (fallStartIndex <= searchStart)
                    fallStartIndex = searchStart;
                var closureEndIndex = Mathf.Clamp(
                    fallStartIndex + Mathf.RoundToInt(
                        (sampleCount - 1) * FallClosureDurationFraction),
                    fallStartIndex + 1,
                    sampleCount - 1);

                for (var index = 0; index < sampleCount; index++)
                {
                    foreach (var snapshot in snapshots)
                        snapshot.Restore();
                    SampleClip(
                        model.gameObject,
                        clip,
                        clip.length * index / (sampleCount - 1f));
                    var progress = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(fallStartIndex, closureEndIndex, index));
                    var thighRootMidpoint =
                        (leftUpper.localPosition + rightUpper.localPosition) * 0.5f;
                    var thighRootScale = Mathf.Lerp(
                        1f,
                        FallenThighRootSeparationScale,
                        progress);
                    leftUpper.localPosition = thighRootMidpoint +
                                              (leftUpper.localPosition - thighRootMidpoint) *
                                              thighRootScale;
                    rightUpper.localPosition = thighRootMidpoint +
                                               (rightUpper.localPosition - thighRootMidpoint) *
                                               thighRootScale;
                    var lateralAxis = rightUpper.position - leftUpper.position;
                    if (lateralAxis.sqrMagnitude <= MotionEpsilon)
                        throw new InvalidOperationException(
                            "The thigh roots cannot define the leg-closing axis.");
                    lateralAxis.Normalize();

                    var kneeMidpoint = (leftLower.position + rightLower.position) * 0.5f;
                    var footMidpoint = (leftFoot.position + rightFoot.position) * 0.5f;
                    var kneeScale = Mathf.Lerp(1f, FallenKneeSeparationScale, progress);
                    var footScale = Mathf.Lerp(1f, FallenFootSeparationScale, progress);
                    var leftKneeTarget = CloseAcrossAxis(
                        leftLower.position,
                        kneeMidpoint,
                        lateralAxis,
                        kneeScale);
                    var rightKneeTarget = CloseAcrossAxis(
                        rightLower.position,
                        kneeMidpoint,
                        lateralAxis,
                        kneeScale);
                    var leftFootTarget = CloseAcrossAxis(
                        leftFoot.position,
                        footMidpoint,
                        lateralAxis,
                        footScale);
                    var rightFootTarget = CloseAcrossAxis(
                        rightFoot.position,
                        footMidpoint,
                        lateralAxis,
                        footScale);

                    RotateBoneToward(leftUpper, leftLower.position, leftKneeTarget);
                    RotateBoneToward(rightUpper, rightLower.position, rightKneeTarget);
                    RotateBoneToward(leftLower, leftFoot.position, leftFootTarget);
                    RotateBoneToward(rightLower, rightFoot.position, rightFootTarget);

                    leftUpperPositions[index] = leftUpper.localPosition;
                    rightUpperPositions[index] = rightUpper.localPosition;

                    for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                    {
                        var rotation = controlled[boneIndex].localRotation;
                        if (index > 0 &&
                            Quaternion.Dot(rotations[boneIndex][index - 1], rotation) < 0f)
                            rotation = new Quaternion(
                                -rotation.x,
                                -rotation.y,
                                -rotation.z,
                                -rotation.w);
                        rotations[boneIndex][index] = rotation;
                    }
                }
            }
            finally
            {
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                SetRotationCurves(clip, paths[boneIndex], rotations[boneIndex]);
            SetLocalPositionCurves(clip, paths[0], leftUpperPositions);
            SetLocalPositionCurves(clip, paths[2], rightUpperPositions);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static Vector3 CloseAcrossAxis(
            Vector3 point,
            Vector3 midpoint,
            Vector3 lateralAxis,
            float separationScale)
        {
            var lateralOffset = Vector3.Dot(point - midpoint, lateralAxis);
            return point - lateralAxis * lateralOffset * (1f - separationScale);
        }

        private static void RotateBoneToward(
            Transform bone,
            Vector3 childPosition,
            Vector3 targetPosition)
        {
            var sourceDirection = childPosition - bone.position;
            var targetDirection = targetPosition - bone.position;
            if (sourceDirection.sqrMagnitude <= MotionEpsilon ||
                targetDirection.sqrMagnitude <= MotionEpsilon)
                throw new InvalidOperationException("A leg-closing direction collapsed.");
            bone.rotation = Quaternion.FromToRotation(sourceDirection, targetDirection) *
                            bone.rotation;
        }

        private static void SetRotationCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Quaternion> values)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path &&
                                        (item.propertyName.StartsWith(
                                             "m_LocalRotation.", StringComparison.Ordinal) ||
                                         item.propertyName.StartsWith(
                                             "localEulerAngles", StringComparison.Ordinal)))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);

            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y",
                "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                var keys = new Keyframe[values.Count];
                for (var index = 0; index < values.Count; index++)
                {
                    var value = component switch
                    {
                        0 => values[index].x,
                        1 => values[index].y,
                        2 => values[index].z,
                        _ => values[index].w
                    };
                    keys[index] = new Keyframe(
                        clip.length * index / (values.Count - 1f),
                        value);
                }
                var curve = new AnimationCurve(keys)
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
                for (var index = 0; index < keys.Length; index++)
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
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        properties[component]),
                    curve);
            }
        }

        private static void SetLocalPositionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Vector3> values)
        {
            var properties = new[]
            {
                "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                             .Where(item => item.path == path &&
                                            item.propertyName == properties[component])
                             .ToArray())
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                var keys = new Keyframe[values.Count];
                for (var index = 0; index < values.Count; index++)
                {
                    var value = component switch
                    {
                        0 => values[index].x,
                        1 => values[index].y,
                        _ => values[index].z
                    };
                    keys[index] = new Keyframe(
                        clip.length * index / (values.Count - 1f),
                        value);
                }
                var curve = new AnimationCurve(keys)
                {
                    preWrapMode = WrapMode.ClampForever,
                    postWrapMode = WrapMode.ClampForever
                };
                for (var index = 0; index < keys.Length; index++)
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
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        properties[component]),
                    curve);
            }
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
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
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The slot-12 model must contain exactly one Animator.");
            var animator = animators[0];
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        private static void ApplyExactStaticMaterials(Transform staticModel, Transform model)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 4)
                throw new InvalidOperationException(
                    "The Ispant model container must contain body, crescent, eyes, and back musket renderers.");
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException("A death-model material slot is null.");
                    var key = NormalizeMaterialName(material.name);
                    return approved.TryGetValue(key, out var exact)
                        ? exact
                        : throw new InvalidOperationException(
                            "No exact static Ispant material matches " + material.name + ".");
                }).ToArray();
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static string NormalizeMaterialName(string name)
        {
            const string instanceSuffix = " (Instance)";
            const string duplicatedSuffix = "_duplicated";
            var result = name.EndsWith(instanceSuffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - instanceSuffix.Length)
                : name;
            while (result.EndsWith(duplicatedSuffix, StringComparison.OrdinalIgnoreCase))
                result = result.Substring(0, result.Length - duplicatedSuffix.Length);
            return result;
        }

        private static SwordFollowBinding CreateRigidHipSword(
            Transform staticModel,
            Transform moveModel,
            Transform targetModel)
        {
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var source = RequireRenderer<MeshRenderer>(moveModel, SwordRendererName);
            if (SharedMesh(source) != SharedMesh(staticSword) ||
                !source.sharedMaterials.SequenceEqual(staticSword.sharedMaterials))
                throw new InvalidOperationException("The Mixamo hip sword is not the exact static sword.");
            var sourceRoot = source.transform.parent ??
                             throw new InvalidOperationException("The Mixamo hip sword root is missing.");
            var sourceHips = sourceRoot.parent ??
                             throw new InvalidOperationException("The Mixamo hip sword Hips parent is missing.");
            if (!string.Equals(NormalizeBoneName(sourceHips.name), "Hips", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The exact sword is not mounted below Mixamo Hips.");
            var targetHips = RequireMappedTransform(BuildUniqueTransformMap(targetModel), sourceHips.name);
            var deformingSword = RequireRenderer<SkinnedMeshRenderer>(targetModel, SwordRootName);
            UnityEngine.Object.DestroyImmediate(deformingSword.gameObject);
            var root = new GameObject(SwordRootName);
            root.transform.SetParent(targetModel, false);
            SetLocalMatrix(
                root.transform,
                moveModel.worldToLocalMatrix * sourceRoot.localToWorldMatrix);
            CloneMeshRenderer(source, root.transform, SwordRendererName, LocalMatrix(source.transform));
            var bodyPositionOffset = targetHips.InverseTransformPoint(root.transform.position);
            var bodyRotationOffset = Quaternion.Inverse(targetHips.rotation) *
                                     root.transform.rotation;
            EditorUtility.SetDirty(root);
            return new SwordFollowBinding(
                root.transform,
                targetHips,
                bodyPositionOffset,
                bodyRotationOffset);
        }

        private static void AuthorBodySwordFollow(
            Transform model,
            AnimationClip clip,
            SwordFollowBinding binding)
        {
            if (binding.SwordRoot.parent != model)
                throw new InvalidOperationException(
                    "The death sword follow root must be a direct model child.");
            var path = AnimationUtility.CalculateTransformPath(binding.SwordRoot, model);
            var sampleCount = Mathf.Max(2, Mathf.RoundToInt(clip.length * clip.frameRate) + 1);
            var positions = new Vector3[sampleCount];
            var rotations = new Quaternion[sampleCount];
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                for (var index = 0; index < sampleCount; index++)
                {
                    foreach (var snapshot in snapshots)
                        snapshot.Restore();
                    SampleClip(
                        model.gameObject,
                        clip,
                        clip.length * index / (sampleCount - 1f));
                    binding.SwordRoot.SetPositionAndRotation(
                        binding.BodyAnchor.TransformPoint(binding.BodyPositionOffset),
                        binding.BodyAnchor.rotation * binding.BodyRotationOffset);
                    positions[index] = binding.SwordRoot.localPosition;
                    var rotation = binding.SwordRoot.localRotation;
                    if (index > 0 && Quaternion.Dot(rotations[index - 1], rotation) < 0f)
                        rotation = new Quaternion(
                            -rotation.x,
                            -rotation.y,
                            -rotation.z,
                            -rotation.w);
                    rotations[index] = rotation;
                }
            }
            finally
            {
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            SetLocalPositionCurves(clip, path, positions);
            SetRotationCurves(clip, path, rotations);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private readonly struct SwordFollowBinding
        {
            public SwordFollowBinding(
                Transform swordRoot,
                Transform bodyAnchor,
                Vector3 bodyPositionOffset,
                Quaternion bodyRotationOffset)
            {
                SwordRoot = swordRoot;
                BodyAnchor = bodyAnchor;
                BodyPositionOffset = bodyPositionOffset;
                BodyRotationOffset = bodyRotationOffset;
            }

            public Transform SwordRoot { get; }
            public Transform BodyAnchor { get; }
            public Vector3 BodyPositionOffset { get; }
            public Quaternion BodyRotationOffset { get; }
        }

        private static void CloneMeshRenderer(
            MeshRenderer source,
            Transform parent,
            string name,
            Matrix4x4 localMatrix)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            SetLocalMatrix(item.transform, localMatrix);
            item.AddComponent<MeshFilter>().sharedMesh = SharedMesh(source);
            var target = item.AddComponent<MeshRenderer>();
            target.sharedMaterials = source.sharedMaterials;
            CopyRendererSettings(source, target);
            EditorUtility.SetDirty(item);
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
                    NormalizeBoneName(item.name), "Hips", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hipsMatches.Length != 1)
                throw new InvalidOperationException("The death rig must contain exactly one Hips skeleton root.");
            var hips = hipsMatches[0];
            foreach (var item in hips.GetComponentsInChildren<Transform>(true).Prepend(hips.parent))
            {
                if (item == null)
                    continue;
                var key = NormalizeBoneName(item.name);
                if (result.TryGetValue(key, out var existing) && existing != item)
                    throw new InvalidOperationException("The death rig has duplicate bone name: " + key + ".");
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
                : throw new InvalidOperationException("The death rig is missing bone: " + key + ".");
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
            var number = int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
            return withoutNamespace.Substring(0, digitStart) +
                   number.ToString(CultureInfo.InvariantCulture);
        }

#if false // Cancelled custom leg-closing and kneel authoring; the embedded Mixamo clip is used unchanged.
        private static void AuthorFullLegKneelThenFall(Transform model, AnimationClip clip)
        {
            var bones = BuildUniqueTransformMap(model);
            var hips = RequireMappedTransform(bones, "Hips");
            var head = RequireMappedTransform(bones, "Head");
            var body = RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName);
            var leftUpper = RequireMappedTransform(bones, "LeftUpLeg");
            var leftLower = RequireMappedTransform(bones, "LeftLeg");
            var leftFoot = RequireMappedTransform(bones, "LeftFoot");
            var rightUpper = RequireMappedTransform(bones, "RightUpLeg");
            var rightLower = RequireMappedTransform(bones, "RightLeg");
            var rightFoot = RequireMappedTransform(bones, "RightFoot");
            var controlled = new[]
            {
                leftUpper, leftLower, leftFoot,
                rightUpper, rightLower, rightFoot
            };
            var rendererBones = new HashSet<Transform>(body.bones);
            foreach (var controlledBone in controlled)
            {
                if (!rendererBones.Contains(controlledBone))
                    throw new InvalidOperationException(
                        "A controlled full-leg bone is not referenced by the Ispant body renderer: " +
                        controlledBone.name + ".");
            }
            var paths = controlled.Select(item =>
                AnimationUtility.CalculateTransformPath(item, model)).ToArray();
            var hipsPath = AnimationUtility.CalculateTransformPath(hips, model);
            var sampleCount = Mathf.Max(2, Mathf.RoundToInt(clip.length * clip.frameRate) + 1);
            var headHeights = new float[sampleCount];
            var fallProgress = new float[sampleCount];
            var values = controlled.Select(_ => new Quaternion[sampleCount]).ToArray();
            var sourceRotations = controlled.Select(_ => new Quaternion[sampleCount]).ToArray();
            var kneeFlexion = new float[sampleCount];
            var hipsPositions = new Vector3[sampleCount];
            var leftUpperPositions = new Vector3[sampleCount];
            var leftLowerPositions = new Vector3[sampleCount];
            var leftFootPositions = new Vector3[sampleCount];
            var rightUpperPositions = new Vector3[sampleCount];
            var rightLowerPositions = new Vector3[sampleCount];
            var rightFootPositions = new Vector3[sampleCount];
            var targetGroundY = model.parent.TransformPoint(Vector3.zero).y;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();

            try
            {
                for (var index = 0; index < sampleCount; index++)
                {
                    foreach (var snapshot in snapshots)
                        snapshot.Restore();
                    SampleClip(model.gameObject, clip, clip.length * index / (sampleCount - 1f));
                    headHeights[index] = model.InverseTransformPoint(head.position).y;
                    for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                        sourceRotations[boneIndex][index] = controlled[boneIndex].localRotation;
                    var leftFlexion = 180f - Vector3.Angle(
                        leftUpper.position - leftLower.position,
                        leftFoot.position - leftLower.position);
                    var rightFlexion = 180f - Vector3.Angle(
                        rightUpper.position - rightLower.position,
                        rightFoot.position - rightLower.position);
                    kneeFlexion[index] = (leftFlexion + rightFlexion) * 0.5f;
                }
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();

                var leadEndIndex = Mathf.Clamp(
                    Mathf.RoundToInt(KneelLeadSeconds * clip.frameRate),
                    1,
                    sampleCount - 2);
                var startHeight = headHeights[leadEndIndex];
                var fallenHeight = headHeights.Skip(leadEndIndex).Min();
                if (startHeight - fallenHeight <= MotionEpsilon)
                    throw new InvalidOperationException(
                        "The supplied death motion does not contain a visible head-height fall.");
                var accumulated = 0f;
                for (var index = 0; index < sampleCount; index++)
                {
                    if (index < leadEndIndex)
                    {
                        fallProgress[index] = 0f;
                        continue;
                    }
                    var raw = Mathf.InverseLerp(startHeight, fallenHeight, headHeights[index]);
                    accumulated = Mathf.Max(accumulated, Mathf.SmoothStep(0f, 1f, raw));
                    fallProgress[index] = accumulated;
                }

                var fallStartIndex = Enumerable.Range(
                        leadEndIndex + 1,
                        sampleCount - leadEndIndex - 1)
                    .OrderByDescending(index => headHeights[index - 1] - headHeights[index])
                    .First();
                var kneelEndIndex = Mathf.Clamp(
                    Mathf.RoundToInt(leadEndIndex * KneelSettleFraction),
                    1,
                    leadEndIndex - 1);
                var kneelPoseIndex = Enumerable.Range(0, sampleCount)
                    .OrderByDescending(index => kneeFlexion[index])
                    .First();
                var kneelReleaseEndIndex = Mathf.Min(
                    sampleCount - 1,
                    fallStartIndex + Mathf.Max(
                        2,
                        Mathf.RoundToInt(KneelReleaseSeconds * clip.frameRate)));

                for (var index = 0; index < sampleCount; index++)
                {
                    foreach (var snapshot in snapshots)
                        snapshot.Restore();
                    SampleClip(model.gameObject, clip, clip.length * index / (sampleCount - 1f));
                    var kneelProgress = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0f, kneelEndIndex, index));
                    var kneelRelease = index < fallStartIndex
                        ? 0f
                        : Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(fallStartIndex, kneelReleaseEndIndex, index));
                    var kneelHold = kneelProgress * (1f - kneelRelease);
                    var closeProgress = Mathf.Max(kneelProgress, fallProgress[index]);
                    var thighRootScale = Mathf.Lerp(
                        1f,
                        ThighRootSeparationScale,
                        closeProgress);
                    var thighRootMidpoint =
                        (leftUpper.localPosition + rightUpper.localPosition) * 0.5f;
                    leftUpper.localPosition = thighRootMidpoint +
                                              (leftUpper.localPosition - thighRootMidpoint) *
                                              thighRootScale;
                    rightUpper.localPosition = thighRootMidpoint +
                                               (rightUpper.localPosition - thighRootMidpoint) *
                                               thighRootScale;
                    var leftKneePosition = leftLower.position;
                    var rightKneePosition = rightLower.position;
                    var leftFootPosition = leftFoot.position;
                    var rightFootPosition = rightFoot.position;
                    var kneelGroundY = Mathf.Min(leftFootPosition.y, rightFootPosition.y);
                    var upperLegLength = ((leftKneePosition - leftUpper.position).magnitude +
                                          (rightKneePosition - rightUpper.position).magnitude) *
                                         0.5f;
                    var currentHipJointY =
                        (leftUpper.position.y + rightUpper.position.y) * 0.5f;
                    var virtualHipY = kneelGroundY +
                                      upperLegLength * KneelVirtualHipHeightScale;
                    hips.position += Vector3.down *
                                     Mathf.Max(0f, currentHipJointY - virtualHipY) *
                                     kneelHold;
                    var lateralAxis = (rightUpper.position - leftUpper.position).normalized;
                    if (lateralAxis.sqrMagnitude <= MotionEpsilon)
                        throw new InvalidOperationException(
                            "The thigh roots cannot define the character lateral axis.");
                    var kneelForward = Vector3.Cross(lateralAxis, Vector3.up).normalized;
                    if (Vector3.Dot(kneelForward, model.forward) < 0f)
                        kneelForward = -kneelForward;
                    for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                        controlled[boneIndex].localRotation = Quaternion.Slerp(
                            controlled[boneIndex].localRotation,
                            sourceRotations[boneIndex][kneelPoseIndex],
                            kneelHold);
                    CloseFullLegPair(
                        kneelForward,
                        leftUpper,
                        leftLower,
                        leftFoot,
                        rightUpper,
                        rightLower,
                        rightFoot,
                        leftKneePosition,
                        rightKneePosition,
                        leftFootPosition,
                        rightFootPosition,
                        closeProgress,
                        kneelHold);
                    PoseLegInKneelDirections(
                        leftUpper,
                        leftLower,
                        leftFoot,
                        kneelForward,
                        kneelHold);
                    PoseLegInKneelDirections(
                        rightUpper,
                        rightLower,
                        rightFoot,
                        kneelForward,
                        kneelHold);
                    PlaceLegInKneelDirections(
                        leftUpper,
                        leftLower,
                        leftFoot,
                        kneelForward,
                        kneelHold);
                    PlaceLegInKneelDirections(
                        rightUpper,
                        rightLower,
                        rightFoot,
                        kneelForward,
                        kneelHold);
                    var currentKneeY = (leftLower.position.y + rightLower.position.y) * 0.5f;
                    hips.position += Vector3.up * (kneelGroundY - currentKneeY) * kneelHold;
                    var fallenGroundProgress = index <= fallStartIndex
                        ? 0f
                        : Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(fallStartIndex, sampleCount - 1, index));
                    if (fallenGroundProgress > MotionEpsilon)
                        hips.position += Vector3.up *
                                         (targetGroundY - body.bounds.min.y) *
                                         fallenGroundProgress;
                    hipsPositions[index] = hips.localPosition;
                    leftUpperPositions[index] = leftUpper.localPosition;
                    leftLowerPositions[index] = leftLower.localPosition;
                    leftFootPositions[index] = leftFoot.localPosition;
                    rightUpperPositions[index] = rightUpper.localPosition;
                    rightLowerPositions[index] = rightLower.localPosition;
                    rightFootPositions[index] = rightFoot.localPosition;
                    for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                    {
                        var rotation = controlled[boneIndex].localRotation;
                        if (index > 0 && Quaternion.Dot(values[boneIndex][index - 1], rotation) < 0f)
                            rotation = Negate(rotation);
                        values[boneIndex][index] = rotation;
                    }
                }
            }
            finally
            {
                StopSampling();
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            for (var boneIndex = 0; boneIndex < controlled.Length; boneIndex++)
                SetQuaternionCurves(clip, paths[boneIndex], values[boneIndex]);
            SetPositionCurves(clip, paths[0], leftUpperPositions);
            SetPositionCurves(clip, paths[1], leftLowerPositions);
            SetPositionCurves(clip, paths[2], leftFootPositions);
            SetPositionCurves(clip, paths[3], rightUpperPositions);
            SetPositionCurves(clip, paths[4], rightLowerPositions);
            SetPositionCurves(clip, paths[5], rightFootPositions);
            SetPositionCurves(clip, hipsPath, hipsPositions);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static void CloseFullLegPair(
            Vector3 modelForward,
            Transform leftUpper,
            Transform leftLower,
            Transform leftFoot,
            Transform rightUpper,
            Transform rightLower,
            Transform rightFoot,
            Vector3 leftKneePosition,
            Vector3 rightKneePosition,
            Vector3 leftFootPosition,
            Vector3 rightFootPosition,
            float closeProgress,
            float kneelProgress)
        {
            var separationScale = Mathf.Lerp(1f, FallenLegSeparationScale, closeProgress);
            var originalFootMidpoint = (leftFootPosition + rightFootPosition) * 0.5f;
            var originalKneeMidpoint = (leftKneePosition + rightKneePosition) * 0.5f;
            var leftFootOffset = leftFootPosition - originalFootMidpoint;
            var rightFootOffset = rightFootPosition - originalFootMidpoint;
            var leftKneeOffset = leftKneePosition - originalKneeMidpoint;
            var rightKneeOffset = rightKneePosition - originalKneeMidpoint;
            var groundForward = Vector3.ProjectOnPlane(modelForward, Vector3.up).normalized;
            if (groundForward.sqrMagnitude <= MotionEpsilon)
                throw new InvalidOperationException("The model forward direction cannot define a kneel.");
            var lowerLegLength = ((leftFootPosition - leftKneePosition).magnitude +
                                  (rightFootPosition - rightKneePosition).magnitude) * 0.5f;
            var upperLegLength = ((leftKneePosition - leftUpper.position).magnitude +
                                  (rightKneePosition - rightUpper.position).magnitude) * 0.5f;
            var kneelGroundY = Mathf.Min(leftFootPosition.y, rightFootPosition.y);
            var kneelKneeMidpoint = originalKneeMidpoint +
                                    groundForward * upperLegLength * KneelKneeForwardScale;
            kneelKneeMidpoint.y = kneelGroundY;
            var kneeMidpoint = Vector3.Lerp(
                originalKneeMidpoint,
                kneelKneeMidpoint,
                kneelProgress);
            var kneelFootMidpoint = kneelKneeMidpoint -
                                    groundForward * lowerLegLength * KneelFootBackScale;
            kneelFootMidpoint.y = kneelGroundY;
            var footMidpoint = Vector3.Lerp(
                originalFootMidpoint,
                kneelFootMidpoint,
                kneelProgress);
            var leftFootTarget = footMidpoint +
                                 leftFootOffset * separationScale;
            var rightFootTarget = footMidpoint +
                                  rightFootOffset * separationScale;
            var leftKneeTarget = kneeMidpoint +
                                 leftKneeOffset * separationScale;
            var rightKneeTarget = kneeMidpoint +
                                  rightKneeOffset * separationScale;
            leftKneeTarget.y = Mathf.Lerp(leftKneeTarget.y, kneelGroundY, kneelProgress);
            rightKneeTarget.y = Mathf.Lerp(rightKneeTarget.y, kneelGroundY, kneelProgress);
            PoseLegTowardTargets(
                leftUpper,
                leftLower,
                leftFoot,
                leftKneeTarget,
                leftFootTarget,
                leftFoot.rotation);
            PoseLegTowardTargets(
                rightUpper,
                rightLower,
                rightFoot,
                rightKneeTarget,
                rightFootTarget,
                rightFoot.rotation);
        }

        private static void PoseLegTowardTargets(
            Transform upper,
            Transform lower,
            Transform tip,
            Vector3 kneeTarget,
            Vector3 footTarget,
            Quaternion tipRotation)
        {
            if ((kneeTarget - upper.position).sqrMagnitude <= MotionEpsilon ||
                (footTarget - lower.position).sqrMagnitude <= MotionEpsilon)
                throw new InvalidOperationException("A full-leg kneel target collapsed.");
            upper.rotation = Quaternion.FromToRotation(
                lower.position - upper.position,
                kneeTarget - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                tip.position - lower.position,
                footTarget - lower.position) * lower.rotation;
            tip.rotation = tipRotation;
        }

        private static void PoseLegInKneelDirections(
            Transform upper,
            Transform lower,
            Transform tip,
            Vector3 forward,
            float progress)
        {
            var sourceThighDirection = (lower.position - upper.position).normalized;
            var kneelingThighDirection =
                (Vector3.down + forward * KneelThighForwardScale).normalized;
            var thighDirection = Vector3.Slerp(
                sourceThighDirection,
                kneelingThighDirection,
                progress);
            upper.rotation = Quaternion.FromToRotation(
                                 lower.position - upper.position,
                                 thighDirection) *
                             upper.rotation;

            var sourceShinDirection = (tip.position - lower.position).normalized;
            var kneelingShinDirection = -forward;
            var shinDirection = Vector3.Slerp(
                sourceShinDirection,
                kneelingShinDirection,
                progress);
            lower.rotation = Quaternion.FromToRotation(
                                 tip.position - lower.position,
                                 shinDirection) *
                             lower.rotation;
        }

        private static void PlaceLegInKneelDirections(
            Transform upper,
            Transform lower,
            Transform tip,
            Vector3 forward,
            float progress)
        {
            var sourceThighVector = lower.position - upper.position;
            var sourceShinVector = tip.position - lower.position;
            var kneelingThighDirection =
                (Vector3.down + forward * KneelThighForwardScale).normalized;
            var kneelingShinDirection = -forward;
            var thighDirection = Vector3.Slerp(
                sourceThighVector.normalized,
                kneelingThighDirection,
                progress);
            var shinDirection = Vector3.Slerp(
                sourceShinVector.normalized,
                kneelingShinDirection,
                progress);
            lower.position = upper.position + thighDirection * sourceThighVector.magnitude;
            tip.position = lower.position + shinDirection * sourceShinVector.magnitude;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Quaternion> values)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path &&
                                        (item.propertyName.StartsWith(
                                             "m_LocalRotation.", StringComparison.Ordinal) ||
                                         item.propertyName.StartsWith(
                                             "localEulerAngles", StringComparison.Ordinal)))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y",
                "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                var keys = new Keyframe[values.Count];
                for (var index = 0; index < values.Count; index++)
                    keys[index] = new Keyframe(
                        clip.length * index / (values.Count - 1f), values[index][component]);
                var curve = new AnimationCurve(keys)
                {
                    preWrapMode = WrapMode.Loop,
                    postWrapMode = WrapMode.Loop
                };
                for (var index = 0; index < keys.Length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]),
                    curve);
            }
        }

        private static void SetPositionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Vector3> values)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path &&
                                        item.propertyName.StartsWith(
                                            "m_LocalPosition.", StringComparison.Ordinal))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
            var properties = new[]
            {
                "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                var keys = new Keyframe[values.Count];
                for (var index = 0; index < values.Count; index++)
                    keys[index] = new Keyframe(
                        clip.length * index / (values.Count - 1f), values[index][component]);
                var curve = new AnimationCurve(keys)
                {
                    preWrapMode = WrapMode.Loop,
                    postWrapMode = WrapMode.Loop
                };
                for (var index = 0; index < keys.Length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]),
                    curve);
            }
        }

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);
#endif

        private static void FitToStaticReference(Transform model, Transform staticModel)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BodyRendererName);
            var body = RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName);
            var staticBounds = BindWorldBounds(staticBody);
            var bounds = BindWorldBounds(body);
            if (bounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The slot-12 bind bounds are invalid.");
            var scale = staticBounds.size.y / bounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException("The slot-12 appearance scale is unsafe.");
            model.localScale *= scale;
            bounds = BindWorldBounds(body);
            model.position += Vector3.up * (staticBounds.min.y - bounds.min.y);
            EditorUtility.SetDirty(model);
        }

        private static void CaptureVisualReview(
            string destination,
            bool focusOnLegs = false,
            bool useSourceClip = false)
        {
            StopSampling();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, DeathSlotName, 11), DeathModelName);
            var clip = useSourceClip
                ? RequireSourceClip()
                : AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                  throw new InvalidOperationException(
                      "The full-leg-closure death playback clip is missing.");
            var targetLegBones = focusOnLegs
                ? new[]
                {
                    RequireMappedTransform(BuildUniqueTransformMap(model), "LeftUpLeg"),
                    RequireMappedTransform(BuildUniqueTransformMap(model), "LeftLeg"),
                    RequireMappedTransform(BuildUniqueTransformMap(model), "LeftFoot"),
                    RequireMappedTransform(BuildUniqueTransformMap(model), "RightUpLeg"),
                    RequireMappedTransform(BuildUniqueTransformMap(model), "RightLeg"),
                    RequireMappedTransform(BuildUniqueTransformMap(model), "RightFoot")
                }
                : Array.Empty<Transform>();
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid slot-12 capture folder."));

            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var targetRenderers = model.GetComponentsInChildren<Renderer>(true);
            var visibleStaticRenderers = staticRenderers.Where(item => item.enabled).ToArray();
            var visibleTargetRenderers = targetRenderers.Where(item => item.enabled).ToArray();
            var rendererSnapshots = staticRenderers.Concat(targetRenderers)
                .Distinct().Select(item => new RendererSnapshot(item)).ToArray();
            var layerSnapshots = staticRenderers.Concat(targetRenderers)
                .Select(item => item.gameObject).Distinct()
                .Select(item => new LayerSnapshot(item)).ToArray();
            var cameraObject = new GameObject("Ispant12DeathReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var keyObject = new GameObject("Ispant12DeathReviewKey", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            var fillObject = new GameObject("Ispant12DeathReviewFill", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            const int renderLayer = 30;
            const int panelWidth = 420;
            const int panelHeight = 560;
            const int panelColumns = 7;
            var viewYaw = new[] { 0f, -90f, 0f };
            var viewPitch = new[] { 0f, 0f, 65f };
            var panelsPerView = ReviewNormalizedTimes.Length + 1;
            var panelCount = panelsPerView * viewYaw.Length;
            var panelRows = Mathf.CeilToInt(panelCount / (float)panelColumns);
            var strip = new Texture2D(
                panelWidth * panelColumns,
                panelHeight * panelRows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var layer in layerSnapshots)
                    layer.GameObject.layer = renderLayer;
                foreach (var renderer in staticRenderers.Concat(targetRenderers))
                    renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << renderLayer;
                camera.fieldOfView = 34f;
                var key = keyObject.GetComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.2f;
                key.color = new Color(1f, 0.95f, 0.88f);
                key.cullingMask = 1 << renderLayer;
                keyObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var fill = fillObject.GetComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.7f;
                fill.color = new Color(0.65f, 0.78f, 1f);
                fill.cullingMask = 1 << renderLayer;
                fillObject.transform.rotation = Quaternion.Euler(20f, 145f, 0f);

                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BodyRendererName);
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                var staticCenter = staticBody.bounds.center;
                var targetCenter = model.parent.TransformPoint(
                    staticModel.parent.InverseTransformPoint(staticCenter));
                var panelReferenceHeight = focusOnLegs ? referenceHeight * 0.7f : referenceHeight;
                var staticPanelCenter = focusOnLegs
                    ? staticCenter - Vector3.up * referenceHeight * 0.22f
                    : staticCenter;
                for (var view = 0; view < viewYaw.Length; view++)
                {
                    foreach (var renderer in visibleStaticRenderers)
                        renderer.enabled = true;
                    foreach (var renderer in targetRenderers)
                        renderer.enabled = false;
                    RenderPanel(
                        camera,
                        target,
                        panel,
                        strip,
                        view * panelsPerView,
                        staticPanelCenter,
                        panelReferenceHeight,
                        viewYaw[view],
                        viewPitch[view],
                        panelWidth,
                        panelHeight,
                        panelColumns,
                        panelRows);
                    foreach (var renderer in staticRenderers)
                        renderer.enabled = false;
                    foreach (var renderer in visibleTargetRenderers)
                        renderer.enabled = true;
                    for (var index = 0; index < ReviewNormalizedTimes.Length; index++)
                    {
                        SampleClip(
                            model.gameObject,
                            clip,
                            ReviewNormalizedTimes[index] * clip.length);
                        foreach (var renderer in visibleTargetRenderers)
                            renderer.enabled = true;
                        foreach (var layer in layerSnapshots)
                            layer.GameObject.layer = renderLayer;
                        var panelCenter = focusOnLegs
                            ? targetLegBones.Aggregate(
                                  Vector3.zero,
                                  (sum, bone) => sum + bone.position) / targetLegBones.Length
                            : targetCenter;
                        RenderPanel(
                            camera,
                            target,
                            panel,
                            strip,
                            view * panelsPerView + index + 1,
                            panelCenter,
                            panelReferenceHeight,
                            viewYaw[view],
                            viewPitch[view],
                            panelWidth,
                            panelHeight,
                            panelColumns,
                            panelRows);
                    }
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                foreach (var snapshot in rendererSnapshots) snapshot.Restore();
                foreach (var snapshot in layerSnapshots) snapshot.Restore();
                foreach (var snapshot in transformSnapshots) snapshot.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Slot-12 visual capture changed the scene dirty state.");
        }

        private static void CaptureLateFallComparison(string destination)
        {
            StopSampling();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, DeathSlotName, 11), DeathModelName);
            var sourceClip = RequireSourceClip();
            var modifiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                               throw new InvalidOperationException(
                                   "The full-leg-closure death playback clip is missing.");
            var map = BuildUniqueTransformMap(model);
            var targetLegBones = new[]
            {
                RequireMappedTransform(map, "LeftUpLeg"),
                RequireMappedTransform(map, "LeftLeg"),
                RequireMappedTransform(map, "LeftFoot"),
                RequireMappedTransform(map, "RightUpLeg"),
                RequireMappedTransform(map, "RightLeg"),
                RequireMappedTransform(map, "RightFoot")
            };
            var comparisonTimes = new[] { 0.50f, 0.625f, 0.75f, 0.875f, 1f };
            var clips = new[] { sourceClip, modifiedClip };

            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid slot-12 comparison folder."));
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = renderers.Select(item => new RendererSnapshot(item)).ToArray();
            var layerSnapshots = renderers.Select(item => item.gameObject).Distinct()
                .Select(item => new LayerSnapshot(item)).ToArray();
            var cameraObject = new GameObject("Ispant12DeathComparisonCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var keyObject = new GameObject("Ispant12DeathComparisonKey", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            var fillObject = new GameObject("Ispant12DeathComparisonFill", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            const int renderLayer = 30;
            const int panelWidth = 600;
            const int panelHeight = 600;
            const int columns = 5;
            const int rows = 2;
            var strip = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var layer in layerSnapshots)
                    layer.GameObject.layer = renderLayer;
                foreach (var renderer in renderers)
                    renderer.enabled = true;
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << renderLayer;
                camera.fieldOfView = 30f;
                var key = keyObject.GetComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.2f;
                key.color = new Color(1f, 0.95f, 0.88f);
                key.cullingMask = 1 << renderLayer;
                keyObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var fill = fillObject.GetComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.7f;
                fill.color = new Color(0.65f, 0.78f, 1f);
                fill.cullingMask = 1 << renderLayer;
                fillObject.transform.rotation = Quaternion.Euler(20f, 145f, 0f);
                var referenceHeight = BindWorldBounds(
                    RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName)).size.y * 0.52f;

                for (var row = 0; row < clips.Length; row++)
                {
                    for (var column = 0; column < comparisonTimes.Length; column++)
                    {
                        SampleClip(
                            model.gameObject,
                            clips[row],
                            comparisonTimes[column] * clips[row].length);
                        var center = targetLegBones.Aggregate(
                            Vector3.zero,
                            (sum, bone) => sum + bone.position) / targetLegBones.Length;
                        RenderPanel(
                            camera,
                            target,
                            panel,
                            strip,
                            row * columns + column,
                            center,
                            referenceHeight,
                            0f,
                            72f,
                            panelWidth,
                            panelHeight,
                            columns,
                            rows);
                    }
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                StopSampling();
                foreach (var snapshot in rendererSnapshots) snapshot.Restore();
                foreach (var snapshot in layerSnapshots) snapshot.Restore();
                foreach (var snapshot in transformSnapshots) snapshot.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Slot-12 comparison capture changed the scene dirty state.");
        }

        private static void RenderPanel(
            Camera camera,
            RenderTexture target,
            Texture2D panel,
            Texture2D strip,
            int index,
            Vector3 center,
            float referenceHeight,
            float yaw,
            float pitch,
            int width,
            int panelHeight,
            int columns,
            int rows)
        {
            camera.aspect = width / (float)panelHeight;
            var framingHeight = referenceHeight * 1.5f;
            var distance = (framingHeight * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var viewDirection = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
            camera.transform.position = center + viewDirection * distance * 1.28f;
            camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, panelHeight), 0, 0);
            panel.Apply();
            var column = index % columns;
            var rowFromTop = index / columns;
            strip.SetPixels32(
                column * width,
                (rows - 1 - rowFromTop) * panelHeight,
                width,
                panelHeight,
                panel.GetPixels32());
        }

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            var position = (Vector3)matrix.GetColumn(3);
            var x = (Vector3)matrix.GetColumn(0);
            var y = (Vector3)matrix.GetColumn(1);
            var z = (Vector3)matrix.GetColumn(2);
            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                throw new InvalidOperationException("A copied death transform has invalid scale.");
            target.SetLocalPositionAndRotation(
                position,
                Quaternion.LookRotation(z / scale.z, y / scale.y));
            target.localScale = scale;
        }

        private static Matrix4x4 LocalMatrix(Transform transform) =>
            Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);

        private static Bounds BindWorldBounds(SkinnedMeshRenderer renderer)
        {
            var vertices = SharedMesh(renderer).vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("An Ispant body mesh has no vertices.");
            var bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
                bounds.Encapsulate(renderer.transform.TransformPoint(vertices[index]));
            return bounds;
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException(
                    "An Ispant renderer has no mesh: " + renderer.name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true)
                       .SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException(
                       "Required Ispant renderer is missing: " + name + ".");
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

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active for slot-12 work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var roots = scene.GetRootGameObjects()
                .Where(item => item.name == PlacementRootName).ToArray();
            if (roots.Length != 1 || roots[0].transform.childCount != ExpectedSlots)
                throw new InvalidOperationException("The approved Ispant placement contract differs.");
            return roots[0];
        }

        private static Transform RequireSlot(Transform placement, string name, int index)
        {
            if (index < 0 || index >= placement.childCount || placement.GetChild(index).name != name)
                throw new InvalidOperationException("The required Ispant slot differs: " + name + ".");
            return placement.GetChild(index);
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return parent.Cast<Transform>().SingleOrDefault(child => child.name == name) ??
                   throw new InvalidOperationException(
                       "Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount).Select(placement.GetChild)
                .Where(item => item != targetSlot)
                .Select(item => item.name + "|" + item.childCount + "|" +
                                Vec(item.localPosition) + "|" + Vec(item.localScale))
                .ToArray();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectFbxPath, SourceSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant slot-12 asset hash differs: " + path + ".");
        }

        private static string Absolute(string path) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        private static string Num(float value) =>
            value.ToString("0.#########", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);

        private readonly struct TransformSnapshot
        {
            private readonly Transform _transform;
            private readonly Vector3 _localPosition;
            private readonly Quaternion _localRotation;
            private readonly Vector3 _localScale;

            public TransformSnapshot(Transform transform)
            {
                _transform = transform;
                _localPosition = transform.localPosition;
                _localRotation = transform.localRotation;
                _localScale = transform.localScale;
            }

            public bool Matches(float tolerance)
            {
                return Vector3.Distance(_transform.localPosition, _localPosition) <= tolerance &&
                       Quaternion.Angle(_transform.localRotation, _localRotation) <= tolerance &&
                       Vector3.Distance(_transform.localScale, _localScale) <= tolerance;
            }

            public void Restore()
            {
                _transform.localPosition = _localPosition;
                _transform.localRotation = _localRotation;
                _transform.localScale = _localScale;
            }
        }

        private readonly struct RendererSnapshot
        {
            private readonly Renderer _renderer;
            private readonly bool _enabled;

            public RendererSnapshot(Renderer renderer)
            {
                _renderer = renderer;
                _enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (_renderer != null)
                    _renderer.enabled = _enabled;
            }
        }

        private readonly struct LayerSnapshot
        {
            public readonly GameObject GameObject;
            private readonly int _layer;

            public LayerSnapshot(GameObject gameObject)
            {
                GameObject = gameObject;
                _layer = gameObject.layer;
            }

            public void Restore()
            {
                if (GameObject != null)
                    GameObject.layer = _layer;
            }
        }
    }
}
