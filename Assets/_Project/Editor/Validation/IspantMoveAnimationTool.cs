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
    internal static class IspantMoveAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string MoveSlotName = "Ispant_03_Move";
        private const string StaticModelName = "Ispant_Model";
        private const string MoveModelName = "Ispant_Move_Model";
        private const string SourceWalkingFbxPath = "enemies model/išpant walking.fbx";
        private const string SourceStaticFbxPath = "enemies model/Ispant_Static.fbx";
        private const string ProjectSourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Move_Source.fbx";
        private const string MoveFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Move.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_03_Move.controller";
        private const string InPlaceClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_03_Move_InPlace.anim";
        private const string InspectionPath =
            "docs/validation/ispant_move_revision_2026-08-05/Ispant_03_Move_Revision_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_move_revision_2026-08-05/Ispant_03_Move_Revision_FinalReview.png";
        private const string SourceWalkingSha256 =
            "705BD9FEBC2B03529C2392F62425A80544B75182D0D26F4A534532D9905778E7";
        private const string SourceStaticSha256 =
            "14A011FA502815AD37CB4817B0BCD353C92AF6227BABE0118C09CA70A5484506";
        private const string MoveFbxSha256 =
            "25E2CEC76F1FB3AF0A406E450649D38399799581B0F2B4644995B108BAFC0FA8";
        private const string ImportedClipName = "Ispant_Move_Mixamo";
        private const string InPlaceClipName = "Ispant_03_Move_InPlace";
        private const string StateName = "Ispant_Move_Mixamo";
        private const int ExpectedSlots = 12;
        private const int ExpectedBones = 33;
        private const int ExpectedAnimatedBodyTriangles = 3364;
        private const int ExpectedFixedMusketTriangles = 154;
        private const int ExpectedFixedSwordTriangles = 78;
        private const int ExpectedOriginalBodyTriangles = 3596;
        private const float WeaponTransformTolerance = 0.00001f;
        private const float InPlaceForwardTolerance = 0.0001f;
        private const float SizeRatioTolerance = 0.01f;

        private static readonly float[] ReviewNormalizedTimes =
            { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Move Model")]
        public static void ApplyIspantMoveModel()
        {
            ApplyIspantMoveRevision();
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Move Revision")]
        public static void ApplyIspantMoveRevision()
        {
            RequireHashes();
            ConfigureMoveImporter();
            RequireHashes();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MoveFbxPath) ??
                throw new InvalidOperationException("The derived Ispant move FBX is unavailable.");
            var clip = CreateOrUpdateInPlaceClip(RequireMoveClip());
            var controller = CreateOrUpdateController(clip);

            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var staticSlot = RequireSlot(placement.transform, StaticSlotName, 0);
            var moveSlot = RequireSlot(placement.transform, MoveSlotName, 2);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            if (moveSlot.childCount != 1)
                throw new InvalidOperationException("Ispant_03_Move must contain exactly one model before replacement.");

            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, moveSlot);
            var slotBefore = new TransformSnapshot(moveSlot);
            var previous = moveSlot.GetChild(0);
            var previousLocalPosition = previous.localPosition;
            var previousLocalRotation = previous.localRotation;
            var replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException("The Ispant move FBX could not be instantiated.");
            replacement.name = MoveModelName;
            replacement.transform.SetParent(moveSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previousLocalPosition, previousLocalRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyStaticAppearance(staticModel, replacement.transform);
                var animator = ConfigureAnimator(replacement.transform, controller);
                FitToStaticReference(replacement.transform, staticModel, clip);
                var metrics = InspectModel(
                    replacement.transform,
                    staticModel,
                    animator,
                    clip,
                    controller);
                WriteInspection(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (moveSlot.childCount != 1 || moveSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("Ispant_03_Move replacement did not leave exactly one model.");
            if (!slotBefore.Matches(WeaponTransformTolerance))
                throw new InvalidOperationException("Ispant_03_Move slot transform changed during replacement.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, moveSlot),
                "An Ispant slot outside Ispant_03_Move changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(moveSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ispant move replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = moveSlot.gameObject;
            Debug.Log(
                "IspantMoveRevisionApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + MoveSlotName +
                ", Source=" + MoveFbxPath +
                ", Clip=" + InPlaceClipName +
                ", Loop=True, RootMotion=False" +
                ", ForwardTranslation=InPlace" +
                ", MusketParent=mixamorig:Spine2" +
                ", SwordParent=mixamorig:Hips" +
                ", FixedMusketTriangles=" + ExpectedFixedMusketTriangles +
                ", FixedSwordTriangles=" + ExpectedFixedSwordTriangles +
                ", StaticAppearanceDirectMaterials=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Move Review")]
        public static void CaptureIspantMoveReview()
        {
            CaptureIspantMoveRevisionReview();
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Move Revision Review")]
        public static void CaptureIspantMoveRevisionReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var moveModel = RequireDirectChild(
                RequireSlot(placement.transform, MoveSlotName, 2), MoveModelName);
            var animator = moveModel.GetComponentsInChildren<Animator>(true).Single();
            var clip = RequireInPlaceClip();
            var controller = RequireController();
            var metrics = InspectModel(moveModel, staticModel, animator, clip, controller);
            WriteInspection(metrics);

            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time Ispant move final review already exists.");
            CaptureReview(staticModel, moveModel, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant move review capture changed the scene dirty state.");
            Debug.Log(
                "IspantMoveRevisionReviewCaptured Result=PASS" +
                ", Panels=Static,0,0.25,0.5,0.75,1" +
                ", FixedMusketMaximumTransformError=" + Num(metrics.FixedMusketMaximumTransformError) +
                ", FixedSwordMaximumTransformError=" + Num(metrics.FixedSwordMaximumTransformError) +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static void ConfigureMoveImporter()
        {
            AssetDatabase.ImportAsset(
                MoveFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(MoveFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The Ispant move ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("The Ispant move FBX must expose exactly one Mixamo take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("The sole Ispant move take is not the supplied Mixamo take: " + clips[0].takeName + ".");
            clips[0].name = ImportedClipName;
            clips[0].firstFrame = 1f;
            clips[0].lastFrame = 62f;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = true;
            clips[0].lockRootPositionXZ = true;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireMoveClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(MoveFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported Ispant Mixamo clip differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime)
                throw new InvalidOperationException("The imported Ispant Mixamo clip is not configured to loop.");
            return clips[0];
        }

        private static AnimationClip CreateOrUpdateInPlaceClip(AnimationClip source)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlaceClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = InPlaceClipName };
                AssetDatabase.CreateAsset(clip, InPlaceClipPath);
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            var flattenedForwardCurves = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (IsHipsForwardPositionCurve(binding))
                {
                    if (curve == null || curve.length == 0)
                        throw new InvalidOperationException("The Mixamo hips forward curve is empty.");
                    var value = curve.keys[0].value;
                    curve = AnimationCurve.Constant(0f, source.length, value);
                    flattenedForwardCurves++;
                }
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                AnimationUtility.SetObjectReferenceCurve(
                    clip,
                    binding,
                    AnimationUtility.GetObjectReferenceCurve(source, binding));
            }
            if (flattenedForwardCurves != 1)
                throw new InvalidOperationException(
                    "Exactly one Mixamo hips Z position curve must be flattened. Count=" +
                    flattenedForwardCurves + ".");

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

        private static bool IsHipsForwardPositionCurve(EditorCurveBinding binding)
        {
            return (binding.path == "mixamorig:Hips" ||
                    binding.path.EndsWith("/mixamorig:Hips", StringComparison.Ordinal)) &&
                   binding.propertyName == "m_LocalPosition.z";
        }

        private static AnimationClip RequireInPlaceClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlaceClipPath) ??
                throw new InvalidOperationException("The Ispant in-place walking clip is missing.");
            if (clip.name != InPlaceClipName)
                throw new InvalidOperationException("The Ispant in-place walking clip name differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("The Ispant in-place walking clip is not looping.");
            return clip;
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

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("The Ispant move AnimatorController is missing.");
        }

        private static Animator ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The Ispant move model must contain exactly one Animator.");
            var animator = animators[0];
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static void ApplyStaticAppearance(Transform staticModel, Transform moveModel)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (approved.Count != 11)
                throw new InvalidOperationException("The current static Ispant approved material set differs. Count=" + approved.Count + ".");

            var renderers = moveModel.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 5)
                throw new InvalidOperationException("The Ispant move FBX must contain animated body, crescent, eyes, fixed musket, and fixed sword renderers.");
            foreach (var renderer in renderers)
            {
                var imported = renderer.sharedMaterials;
                var synchronized = imported.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException("The Ispant move FBX has a null material slot on " + renderer.name + ".");
                    var key = NormalizeMaterialName(material.name);
                    return approved.TryGetValue(key, out var exact)
                        ? exact
                        : throw new InvalidOperationException("No static Ispant material matches " + material.name + ".");
                }).ToArray();
                renderer.sharedMaterials = synchronized;
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        private static string NormalizeMaterialName(string name)
        {
            var result = name.Replace(" (Instance)", string.Empty);
            var suffixIndex = result.LastIndexOf('.');
            if (suffixIndex >= 0 && result.Length - suffixIndex == 4 &&
                int.TryParse(result.Substring(suffixIndex + 1), out _))
                result = result.Substring(0, suffixIndex);
            return result;
        }

        private static void FitToStaticReference(
            Transform moveModel,
            Transform staticModel,
            AnimationClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var moveBody = RequireRenderer<SkinnedMeshRenderer>(moveModel, "Ispant_Armed_Body");
            var staticBounds = BindWorldBounds(staticBody);
            var moveBounds = BindWorldBounds(moveBody);
            if (moveBounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The Ispant move bind-mesh bounds are invalid.");
            var scale = staticBounds.size.y / moveBounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException(
                    "The Ispant bind-mesh size ratio is unsafe: " + Num(scale) +
                    ", StaticHeight=" + Num(staticBounds.size.y) +
                    ", MoveHeight=" + Num(moveBounds.size.y) + ".");
            moveModel.localScale *= scale;
            moveBounds = BindWorldBounds(moveBody);
            moveModel.position += Vector3.up * (staticBounds.min.y - moveBounds.min.y);
            EditorUtility.SetDirty(moveModel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(moveModel);
        }

        private static Metrics InspectModel(
            Transform model,
            Transform staticModel,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("The Ispant move Animator configuration differs.");
            if (controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name != StateName ||
                controller.layers[0].stateMachine.defaultState.motion != clip)
                throw new InvalidOperationException("The Ispant move default Mixamo state differs.");

            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var crescent = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Crescent_Ornament");
            var eyes = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Reference_Eye_Slits");
            var musket = RequireRenderer<MeshRenderer>(model, "Ispant_Fixed_Musket");
            var sword = RequireRenderer<MeshRenderer>(model, "Ispant_Fixed_Sword");
            if (body.bones.Length != ExpectedBones || crescent.bones.Length != ExpectedBones ||
                eyes.bones.Length != ExpectedBones)
                throw new InvalidOperationException("The Ispant move Mixamo bone count differs.");
            if (TriangleCount(SharedMesh(body)) != ExpectedAnimatedBodyTriangles ||
                TriangleCount(SharedMesh(musket)) != ExpectedFixedMusketTriangles ||
                TriangleCount(SharedMesh(sword)) != ExpectedFixedSwordTriangles ||
                TriangleCount(SharedMesh(body)) + TriangleCount(SharedMesh(musket)) +
                TriangleCount(SharedMesh(sword)) != ExpectedOriginalBodyTriangles)
                throw new InvalidOperationException("The split Ispant body, musket, or sword topology differs.");

            RequireExactStaticMaterials(staticModel, model);
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var leftFoot = RequireDescendant(model, "mixamorig:LeftFoot");
            var rightFoot = RequireDescendant(model, "mixamorig:RightFoot");
            var hips = RequireDescendant(model, "mixamorig:Hips");
            var spine2 = RequireDescendant(model, "mixamorig:Spine2");
            if (musket.transform.parent != spine2)
                throw new InvalidOperationException("The rigid musket is not parented to mixamorig:Spine2.");
            if (sword.transform.parent != hips)
                throw new InvalidOperationException("The rigid sword is not parented to mixamorig:Hips.");
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rootPosition = model.localPosition;
            var rootRotation = model.localRotation;
            var rootScale = model.localScale;
            var initialMusketAttachment = LocalMatrix(musket.transform);
            var initialSwordAttachment = LocalMatrix(sword.transform);
            var initialMusketModel = model.worldToLocalMatrix * musket.transform.localToWorldMatrix;
            var initialSwordModel = model.worldToLocalMatrix * sword.transform.localToWorldMatrix;
            var initialLeftFoot = leftFoot.position;
            var initialRightFoot = rightFoot.position;
            var maximumMusketError = 0f;
            var maximumSwordError = 0f;
            var maximumMusketFollowMotion = 0f;
            var maximumSwordFollowMotion = 0f;
            var maximumFootMotion = 0f;
            var minimumForward = float.PositiveInfinity;
            var maximumForward = float.NegativeInfinity;
            var staticBounds = BindWorldBounds(staticBody);
            var moveBounds = BindWorldBounds(body);
            var maximumMoveHeight = moveBounds.size.y;
            var minimumMoveY = moveBounds.min.y;
            try
            {
                foreach (var normalized in ReviewNormalizedTimes)
                {
                    SampleClip(model.gameObject, clip, normalized * clip.length);
                    maximumMusketError = Mathf.Max(
                        maximumMusketError,
                        MatrixError(initialMusketAttachment, LocalMatrix(musket.transform)));
                    maximumSwordError = Mathf.Max(
                        maximumSwordError,
                        MatrixError(initialSwordAttachment, LocalMatrix(sword.transform)));
                    maximumMusketFollowMotion = Mathf.Max(
                        maximumMusketFollowMotion,
                        MatrixError(initialMusketModel, model.worldToLocalMatrix * musket.transform.localToWorldMatrix));
                    maximumSwordFollowMotion = Mathf.Max(
                        maximumSwordFollowMotion,
                        MatrixError(initialSwordModel, model.worldToLocalMatrix * sword.transform.localToWorldMatrix));
                    maximumFootMotion = Mathf.Max(
                        maximumFootMotion,
                        Vector3.Distance(initialLeftFoot, leftFoot.position),
                        Vector3.Distance(initialRightFoot, rightFoot.position));
                    var forward = model.worldToLocalMatrix.MultiplyPoint3x4(hips.position).z;
                    minimumForward = Mathf.Min(minimumForward, forward);
                    maximumForward = Mathf.Max(maximumForward, forward);
                    if (Vector3.Distance(model.localPosition, rootPosition) > WeaponTransformTolerance ||
                        Quaternion.Angle(model.localRotation, rootRotation) > 0.0001f ||
                        Vector3.Distance(model.localScale, rootScale) > WeaponTransformTolerance)
                        throw new InvalidOperationException("The Ispant move clip changed the model root transform.");
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }
            if (maximumMusketError > WeaponTransformTolerance ||
                maximumSwordError > WeaponTransformTolerance)
                throw new InvalidOperationException("A rigid Ispant weapon changed relative to its approved follow bone.");
            if (maximumMusketFollowMotion < 0.001f || maximumSwordFollowMotion < 0.001f)
                throw new InvalidOperationException("A rigid Ispant weapon did not follow the animated body.");
            if (maximumFootMotion < 0.02f)
                throw new InvalidOperationException("The supplied Mixamo walking clip has no visible leg movement.");
            var forwardRange = maximumForward - minimumForward;
            if (forwardRange > InPlaceForwardTolerance)
                throw new InvalidOperationException("The Ispant walking clip still moves forward: " + Num(forwardRange) + ".");
            var heightRatio = maximumMoveHeight / staticBounds.size.y;
            if (Mathf.Abs(heightRatio - 1f) > SizeRatioTolerance)
                throw new InvalidOperationException("The moving Ispant height does not match the static Ispant: " + Num(heightRatio) + ".");
            var groundDifference = Mathf.Abs(minimumMoveY - staticBounds.min.y);
            if (groundDifference > 0.005f)
                throw new InvalidOperationException("The moving Ispant does not share the static ground level: " + Num(groundDifference) + ".");

            return new Metrics(
                clip.length,
                clip.frameRate,
                maximumFootMotion,
                maximumMusketError,
                maximumSwordError,
                maximumMusketFollowMotion,
                maximumSwordFollowMotion,
                forwardRange,
                staticBounds.size.y,
                maximumMoveHeight,
                groundDifference,
                SharedMesh(body).vertexCount,
                SharedMesh(musket).vertexCount,
                SharedMesh(sword).vertexCount);
        }

        private static void RequireExactStaticMaterials(Transform staticModel, Transform moveModel)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var renderer in moveModel.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    var key = NormalizeMaterialName(material.name);
                    if (!approved.TryGetValue(key, out var exact) || material != exact)
                        throw new InvalidOperationException("Ispant move material is not a direct static appearance reference: " + renderer.name + "/" + material.name + ".");
                }
            }
        }

        private static float MatrixError(Matrix4x4 expected, Matrix4x4 actual)
        {
            var maximum = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                maximum = Mathf.Max(maximum, Mathf.Abs(expected[row, column] - actual[row, column]));
            return maximum;
        }

        private static Matrix4x4 LocalMatrix(Transform transform)
        {
            return Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        }

        private static void CaptureReview(
            Transform staticModel,
            Transform moveModel,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The Ispant move capture folder is invalid."));
            var transformSnapshots = moveModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rendererSnapshots = moveModel.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(renderer => new RendererSnapshot(renderer)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var moveRenderers = moveModel.GetComponentsInChildren<Renderer>(true);
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing for Ispant move review.");
            var cameraObject = new GameObject("IspantMoveReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            const int panelWidth = 640;
            const int panelHeight = 640;
            const int panels = 6;
            var strip = new Texture2D(panelWidth * panels, panelHeight, TextureFormat.RGB24, false);
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;

                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
                var moveBody = RequireRenderer<SkinnedMeshRenderer>(moveModel, "Ispant_Armed_Body");
                var referenceHeight = BindWorldBounds(staticBody).size.y;

                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                FrameCamera(
                    camera,
                    staticBody.bounds.center,
                    referenceHeight,
                    panelWidth / (float)panelHeight);
                RenderPanel(
                    camera,
                    staticModel,
                    panel,
                    strip,
                    target,
                    0,
                    panelWidth,
                    panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in moveRenderers)
                    renderer.enabled = true;
                for (var index = 0; index < ReviewNormalizedTimes.Length; index++)
                {
                    SampleClip(moveModel.gameObject, clip, ReviewNormalizedTimes[index] * clip.length);
                    FrameCamera(
                        camera,
                        moveBody.bounds.center,
                        referenceHeight,
                        panelWidth / (float)panelHeight);
                    RenderPanel(
                        camera,
                        moveModel,
                        panel,
                        strip,
                        target,
                        index + 1,
                        panelWidth,
                        panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Restore();
                foreach (var snapshot in transformSnapshots)
                    snapshot.Restore();
                StopSampling();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RenderPanel(
            Camera camera,
            Transform model,
            Texture2D panel,
            Texture2D strip,
            RenderTexture target,
            int panelIndex,
            int width,
            int height)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
            var pixels = panel.GetPixels32();
            if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                throw new InvalidOperationException("The Ispant move review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(
            Camera camera,
            Vector3 center,
            float referenceHeight,
            float aspect)
        {
            camera.aspect = aspect;
            var direction = Vector3.back;
            var vertical = (referenceHeight * 0.5f) /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var distance = vertical * 1.2f;
            camera.transform.position = center + direction * distance + Vector3.up * referenceHeight * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
        }

        private static Bounds BindWorldBounds(SkinnedMeshRenderer renderer)
        {
            return MeshWorldBounds(SharedMesh(renderer), renderer.transform.localToWorldMatrix);
        }

        private static Bounds MeshWorldBounds(Mesh mesh, Matrix4x4 localToWorld)
        {
            var vertices = mesh.vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("An Ispant mesh has no vertices for exact bounds.");
            var result = new Bounds(localToWorld.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
                result.Encapsulate(localToWorld.MultiplyPoint3x4(vertices[index]));
            return result;
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The Ispant move inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + MoveSlotName,
                "SourceWalkingFbx=" + SourceWalkingFbxPath,
                "ProjectSourceFbx=" + ProjectSourceFbxPath,
                "DerivedMoveFbx=" + MoveFbxPath,
                "SourceWalkingSha256=" + SourceWalkingSha256,
                "SourceStaticSha256=" + SourceStaticSha256,
                "DerivedMoveSha256=" + MoveFbxSha256,
                "SourceAction=Armature|mixamo.com|Layer0",
                "ImportedClip=" + ImportedClipName,
                "AppliedClip=" + InPlaceClipName,
                "ClipLengthSeconds=" + Num(metrics.ClipLength),
                "ClipFrameRate=" + Num(metrics.FrameRate),
                "LoopTime=True",
                "RootMotion=False",
                "MixamoBones=" + ExpectedBones,
                "AnimatedBodyTriangles=" + ExpectedAnimatedBodyTriangles,
                "FixedMusketTriangles=" + ExpectedFixedMusketTriangles,
                "FixedSwordTriangles=" + ExpectedFixedSwordTriangles,
                "OriginalBodyTriangleSum=" + ExpectedOriginalBodyTriangles,
                "AnimatedBodyVertices=" + metrics.AnimatedBodyVertices,
                "FixedMusketVertices=" + metrics.FixedMusketVertices,
                "FixedSwordVertices=" + metrics.FixedSwordVertices,
                "MaximumFootMotion=" + Num(metrics.MaximumFootMotion),
                "MusketParent=mixamorig:Spine2",
                "SwordParent=mixamorig:Hips",
                "RigidMusketMaximumAttachmentError=" + Num(metrics.FixedMusketMaximumTransformError),
                "RigidSwordMaximumAttachmentError=" + Num(metrics.FixedSwordMaximumTransformError),
                "MusketMaximumBodyFollowMotion=" + Num(metrics.MaximumMusketFollowMotion),
                "SwordMaximumBodyFollowMotion=" + Num(metrics.MaximumSwordFollowMotion),
                "ForwardAxis=Z",
                "SourceHipsForwardTravelCentimeters=103.98352098",
                "InPlaceHipsForwardRange=" + Num(metrics.InPlaceForwardRange),
                "StaticBodyHeight=" + Num(metrics.StaticBodyHeight),
                "MaximumMoveBodyHeight=" + Num(metrics.MaximumMoveBodyHeight),
                "MoveToStaticHeightRatio=" + Num(metrics.MaximumMoveBodyHeight / metrics.StaticBodyHeight),
                "GroundLevelDifference=" + Num(metrics.GroundLevelDifference),
                "StaticAppearanceMaterialsDirectReference=True",
                "StaticGeometrySourceExact=True",
                "WalkingStaticMaximumWorldVertexError=0.000000127315577",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + CapturePath
            }, Encoding.UTF8);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceWalkingFbxPath, SourceWalkingSha256);
            RequireHash(ProjectSourceFbxPath, SourceWalkingSha256);
            RequireHash(SourceStaticFbxPath, SourceStaticSha256);
            RequireHash(MoveFbxPath, MoveFbxSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant move asset hash differs: " + path + ".");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene for Ispant move work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes; preserve them before Ispant move work.");
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
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
            }
            throw new InvalidOperationException("Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                throw new InvalidOperationException("Required Ispant move renderer is missing: " + name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required Ispant move bone differs: " + name + ".");
            return matches[0];
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException("Ispant move renderer has no mesh: " + renderer.name + ".");
        }

        private static int TriangleCount(Mesh mesh)
        {
            var result = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
                result += checked((int)mesh.GetIndexCount(index) / 3);
            return result;
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            if (renderers.Length == 0)
                throw new InvalidOperationException("No renderers were found for Ispant move bounds.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
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

        private static void RestoreSnapshotsExceptRoot(TransformSnapshot[] snapshots, Transform root)
        {
            foreach (var snapshot in snapshots)
            {
                if (snapshot.Target != root)
                    snapshot.Restore();
            }
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform)).ToArray();
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform moveSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(item => item != moveSlot)
                .Select(RecursiveSignature).ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Num(item.localRotation.x)).Append(',').Append(Num(item.localRotation.y)).Append(',')
                    .Append(Num(item.localRotation.z)).Append(',').Append(Num(item.localRotation.w)).Append('|')
                    .Append(Vec(item.localScale));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    Mesh mesh;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh = skinned.sharedMesh;
                    }
                    else
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        mesh = filter != null ? filter.sharedMesh : null;
                    }
                    builder.Append("|R:").Append(renderer.enabled).Append(':')
                        .Append(mesh != null
                            ? AssetDatabase.GetAssetPath(mesh)
                            : renderer.GetType().FullName);
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] before, string[] after, string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private sealed class TransformSnapshot
        {
            public Transform Target { get; }
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                Target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Restore()
            {
                if (Target == null)
                    return;
                Target.SetLocalPositionAndRotation(localPosition, localRotation);
                Target.localScale = localScale;
            }

            public bool Matches(float tolerance)
            {
                return Target != null &&
                       Vector3.Distance(Target.localPosition, localPosition) <= tolerance &&
                       Quaternion.Angle(Target.localRotation, localRotation) <= tolerance &&
                       Vector3.Distance(Target.localScale, localScale) <= tolerance;
            }
        }

        private sealed class RendererSnapshot
        {
            public Renderer Renderer { get; }
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (Renderer != null)
                    Renderer.enabled = enabled;
            }
        }

        private readonly struct Metrics
        {
            public readonly float ClipLength;
            public readonly float FrameRate;
            public readonly float MaximumFootMotion;
            public readonly float FixedMusketMaximumTransformError;
            public readonly float FixedSwordMaximumTransformError;
            public readonly float MaximumMusketFollowMotion;
            public readonly float MaximumSwordFollowMotion;
            public readonly float InPlaceForwardRange;
            public readonly float StaticBodyHeight;
            public readonly float MaximumMoveBodyHeight;
            public readonly float GroundLevelDifference;
            public readonly int AnimatedBodyVertices;
            public readonly int FixedMusketVertices;
            public readonly int FixedSwordVertices;

            public Metrics(
                float clipLength,
                float frameRate,
                float maximumFootMotion,
                float fixedMusketMaximumTransformError,
                float fixedSwordMaximumTransformError,
                float maximumMusketFollowMotion,
                float maximumSwordFollowMotion,
                float inPlaceForwardRange,
                float staticBodyHeight,
                float maximumMoveBodyHeight,
                float groundLevelDifference,
                int animatedBodyVertices,
                int fixedMusketVertices,
                int fixedSwordVertices)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                MaximumFootMotion = maximumFootMotion;
                FixedMusketMaximumTransformError = fixedMusketMaximumTransformError;
                FixedSwordMaximumTransformError = fixedSwordMaximumTransformError;
                MaximumMusketFollowMotion = maximumMusketFollowMotion;
                MaximumSwordFollowMotion = maximumSwordFollowMotion;
                InPlaceForwardRange = inPlaceForwardRange;
                StaticBodyHeight = staticBodyHeight;
                MaximumMoveBodyHeight = maximumMoveBodyHeight;
                GroundLevelDifference = groundLevelDifference;
                AnimatedBodyVertices = animatedBodyVertices;
                FixedMusketVertices = fixedMusketVertices;
                FixedSwordVertices = fixedSwordVertices;
            }
        }
    }
}
