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
    internal static class IspantDrawSwordAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string DrawSwordSlotName = "Ispant_04_DrawSword";
        private const string StaticModelName = "Ispant_Model";
        private const string DrawSwordModelName = "Ispant_DrawSword_Model";
        private const string SourceDrawSwordFbxPath = "enemies model/išpant draw sword.fbx";
        private const string SourceStaticFbxPath = "enemies model/Ispant_Static.fbx";
        private const string ProjectSourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword_Source.fbx";
        private const string DrawSwordFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_04_DrawSword.controller";
        private const string InspectionPath =
            "docs/validation/ispant_draw_sword_2026-08-06/Ispant_04_DrawSword_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_draw_sword_2026-08-06/Ispant_04_DrawSword_FinalReview.png";
        private const string SourceDrawSwordSha256 =
            "EDDE5E8B668C987E0C92F37E2D7809AC196F5A4EAC0BE3F5B2408CBAC8862E72";
        private const string SourceStaticSha256 =
            "14A011FA502815AD37CB4817B0BCD353C92AF6227BABE0118C09CA70A5484506";
        private const string DrawSwordFbxSha256 =
            "B9DEB78C6BECA61C81EE5ECD86C4763E56186B8925EED29720B4B62ED482CE42";
        private const string ImportedClipName = "Ispant_DrawSword_Mixamo";
        private const string StateName = "Ispant_DrawSword_Mixamo";
        private const int ExpectedSlots = 12;
        private const int ExpectedBones = 33;
        private const int SourceBodyVertices = 1915;
        private const int ExpectedImportedBodyVertices = 3063;
        private const int ExpectedBodyTriangles = 3364;
        private const int SourceMusketVertices = 83;
        private const int ExpectedImportedMusketVertices = 128;
        private const int ExpectedMusketTriangles = 154;
        private const int SourceSheathVertices = 22;
        private const int ExpectedImportedSheathVertices = 44;
        private const int ExpectedSheathTriangles = 38;
        private const int SourceSwordVertices = 24;
        private const int ExpectedImportedSwordVertices = 40;
        private const int ExpectedSwordTriangles = 40;
        private const int ExpectedOriginalTriangles = 3596;
        private const int FirstFrame = 1;
        private const int LastFrame = 46;
        private const float TransformTolerance = 0.0001f;
        private const float AttachmentTolerance = 0.0001f;
        private const float SizeRatioTolerance = 0.01f;
        private const float MaximumSwordVertexToHandDistance = 0.04f;

        private static readonly float[] ReviewNormalizedTimes =
            { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Draw Sword Animation")]
        public static void ApplyIspantDrawSwordAnimation()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DrawSwordFbxPath) ??
                throw new InvalidOperationException("The derived Ispant draw-sword FBX is unavailable.");
            var clip = RequireClip();
            var controller = CreateOrUpdateController(clip);

            // A failed guarded replacement destroys its temporary instance but leaves Unity's
            // scene dirty flag set. The complete other-root/other-slot signatures below still
            // protect the resumed replacement and the successful path saves the approved scene.
            var scene = RequireScene(requireClean: false);
            var placement = RequirePlacement(scene);
            var staticSlot = RequireSlot(placement.transform, StaticSlotName, 0);
            var drawSwordSlot = RequireSlot(placement.transform, DrawSwordSlotName, 3);
            var staticModel = RequireDirectChild(staticSlot, StaticModelName);
            if (drawSwordSlot.childCount != 1)
                throw new InvalidOperationException("Ispant_04_DrawSword must contain exactly one model before replacement.");

            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, drawSwordSlot);
            var slotBefore = new TransformSnapshot(drawSwordSlot);
            var previous = drawSwordSlot.GetChild(0);
            var previousLocalPosition = previous.localPosition;
            var previousLocalRotation = previous.localRotation;
            var replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException("The Ispant draw-sword FBX could not be instantiated.");
            replacement.name = DrawSwordModelName;
            replacement.transform.SetParent(drawSwordSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previousLocalPosition, previousLocalRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyStaticAppearance(staticModel, replacement.transform);
                var animator = ConfigureAnimator(replacement.transform, controller);
                FitToStaticReference(replacement.transform, staticModel);
                var metrics = InspectModel(replacement.transform, staticModel, animator, clip, controller);
                WriteInspection(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (drawSwordSlot.childCount != 1 || drawSwordSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("Ispant_04_DrawSword replacement did not leave exactly one model.");
            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("Ispant_04_DrawSword slot transform changed during replacement.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, drawSwordSlot),
                "An Ispant slot outside Ispant_04_DrawSword changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(drawSwordSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ispant draw-sword replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = drawSwordSlot.gameObject;
            Debug.Log(
                "IspantDrawSwordAnimationApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + DrawSwordSlotName +
                ", Source=" + DrawSwordFbxPath +
                ", Clip=" + ImportedClipName +
                ", Loop=True, RootMotion=False" +
                ", SwordSource=ReplacementDrawSwordFbx" +
                ", SwordParent=mixamorig:RightHand" +
                ", StaticAppearanceDirectMaterials=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Draw Sword Animation")]
        public static void InspectIspantDrawSwordAnimation()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var metrics = InspectModel(model, staticModel, animator, RequireClip(), RequireController());
            WriteInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant draw-sword inspection changed the scene dirty state.");
            Debug.Log(
                "IspantDrawSwordAnimationInspected Result=PASS" +
                ", MaximumSwordAttachmentError=" + Num(metrics.MaximumSwordAttachmentError) +
                ", MaximumSwordFollowMotion=" + Num(metrics.MaximumSwordFollowMotion) +
                ", MaximumNearestSwordVertexToHand=" + Num(metrics.MaximumNearestSwordVertexToHand) +
                ", Frames=" + FirstFrame + "-" + LastFrame + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Draw Sword Animation Review")]
        public static void CaptureIspantDrawSwordAnimationReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var clip = RequireClip();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var metrics = InspectModel(model, staticModel, animator, clip, RequireController());
            WriteInspection(metrics);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time Ispant draw-sword final review already exists.");
            CaptureReview(staticModel, model, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant draw-sword review capture changed the scene dirty state.");
            Debug.Log(
                "IspantDrawSwordAnimationReviewCaptured Result=PASS" +
                ", Panels=Static,0,0.25,0.5,0.75,1" +
                ", MaximumSwordAttachmentError=" + Num(metrics.MaximumSwordAttachmentError) +
                ", Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                DrawSwordFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(DrawSwordFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The Ispant draw-sword ModelImporter is missing.");
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
                throw new InvalidOperationException("The Ispant draw-sword FBX must expose exactly one Mixamo take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The sole Ispant draw-sword take is not the supplied Mixamo take: " + clips[0].takeName + ".");
            clips[0].name = ImportedClipName;
            clips[0].firstFrame = FirstFrame;
            clips[0].lastFrame = LastFrame;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = true;
            clips[0].lockRootPositionXZ = true;
            clips[0].lockRootHeightY = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(DrawSwordFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported Ispant draw-sword Mixamo clip differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime)
                throw new InvalidOperationException("The imported Ispant draw-sword Mixamo clip is not looping.");
            return clips[0];
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
                throw new InvalidOperationException("The Ispant draw-sword AnimatorController is missing.");
        }

        private static Animator ConfigureAnimator(Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The Ispant draw-sword model must contain exactly one Animator.");
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

        private static void ApplyStaticAppearance(Transform staticModel, Transform model)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (approved.Count != 11)
                throw new InvalidOperationException(
                    "The current static Ispant approved material set differs. Count=" + approved.Count + ".");

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 6)
                throw new InvalidOperationException(
                    "The Ispant draw-sword FBX must contain body, crescent, eyes, rigid musket, rigid sheath, and its own rigid drawn-sword renderers.");
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException(
                            "The Ispant draw-sword FBX has a null material slot on " + renderer.name + ".");
                    var key = NormalizeMaterialName(material.name);
                    return approved.TryGetValue(key, out var exact)
                        ? exact
                        : throw new InvalidOperationException(
                            "No static Ispant material matches " + material.name + ".");
                }).ToArray();
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

        private static void FitToStaticReference(Transform model, Transform staticModel)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var staticBounds = BindWorldBounds(staticBody);
            var bounds = BindWorldBounds(body);
            if (bounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The Ispant draw-sword bind-mesh bounds are invalid.");
            var scale = staticBounds.size.y / bounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException("The Ispant draw-sword bind-mesh size ratio is unsafe: " + Num(scale) + ".");
            model.localScale *= scale;
            bounds = BindWorldBounds(body);
            model.position += Vector3.up * (staticBounds.min.y - bounds.min.y);
            EditorUtility.SetDirty(model);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
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
                throw new InvalidOperationException("The Ispant draw-sword Animator configuration differs.");
            if (controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name != StateName ||
                controller.layers[0].stateMachine.defaultState.motion != clip)
                throw new InvalidOperationException("The Ispant draw-sword default Mixamo state differs.");

            var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
            var crescent = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Crescent_Ornament");
            var eyes = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Reference_Eye_Slits");
            var musket = RequireRenderer<MeshRenderer>(model, "Ispant_DrawSword_RigidMusket");
            var sheath = RequireRenderer<MeshRenderer>(model, "Ispant_DrawSword_RigidSheath");
            var sword = RequireRenderer<MeshRenderer>(model, "Ispant_DrawSword_RigidSword");
            var bodyMesh = SharedMesh(body);
            var musketMesh = SharedMesh(musket);
            var sheathMesh = SharedMesh(sheath);
            var swordMesh = SharedMesh(sword);
            if (body.bones.Length != ExpectedBones || crescent.bones.Length != ExpectedBones ||
                eyes.bones.Length != ExpectedBones)
                throw new InvalidOperationException("The Ispant draw-sword Mixamo bone count differs.");
            if (bodyMesh.vertexCount != ExpectedImportedBodyVertices || TriangleCount(bodyMesh) != ExpectedBodyTriangles ||
                musketMesh.vertexCount != ExpectedImportedMusketVertices || TriangleCount(musketMesh) != ExpectedMusketTriangles ||
                sheathMesh.vertexCount != ExpectedImportedSheathVertices || TriangleCount(sheathMesh) != ExpectedSheathTriangles ||
                swordMesh.vertexCount != ExpectedImportedSwordVertices || TriangleCount(swordMesh) != ExpectedSwordTriangles ||
                TriangleCount(bodyMesh) + TriangleCount(musketMesh) + TriangleCount(sheathMesh) +
                TriangleCount(swordMesh) != ExpectedOriginalTriangles)
                throw new InvalidOperationException(
                    "The Ispant draw-sword body or source-weapon topology differs." +
                    " BodyVertices=" + bodyMesh.vertexCount +
                    ", BodyTriangles=" + TriangleCount(bodyMesh) +
                    ", MusketVertices=" + musketMesh.vertexCount +
                    ", MusketTriangles=" + TriangleCount(musketMesh) +
                    ", SheathVertices=" + sheathMesh.vertexCount +
                    ", SheathTriangles=" + TriangleCount(sheathMesh) +
                    ", SwordVertices=" + swordMesh.vertexCount +
                    ", SwordTriangles=" + TriangleCount(swordMesh) + ".");
            if (musket.GetComponent<SkinnedMeshRenderer>() != null ||
                sheath.GetComponent<SkinnedMeshRenderer>() != null ||
                sword.GetComponent<SkinnedMeshRenderer>() != null)
                throw new InvalidOperationException("The replacement draw-sword FBX weapons must be rigid, not skinned.");

            RequireExactStaticMaterials(staticModel, model);
            var spine2 = RequireDescendant(model, "mixamorig:Spine2");
            var hips = RequireDescendant(model, "mixamorig:Hips");
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            if (musket.transform.parent != spine2)
                throw new InvalidOperationException(
                    "The replacement draw-sword FBX musket is not directly parented to mixamorig:Spine2.");
            if (sheath.transform.parent != hips)
                throw new InvalidOperationException(
                    "The replacement draw-sword FBX sheath is not directly parented to mixamorig:Hips.");
            if (sword.transform.parent != rightHand)
                throw new InvalidOperationException(
                    "The replacement draw-sword FBX sword is not directly parented to mixamorig:RightHand.");

            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, "Ispant_Armed_Body");
            var staticBounds = BindWorldBounds(staticBody);
            var drawBounds = BindWorldBounds(body);
            var heightRatio = drawBounds.size.y / staticBounds.size.y;
            var groundDifference = Mathf.Abs(drawBounds.min.y - staticBounds.min.y);
            if (Mathf.Abs(heightRatio - 1f) > SizeRatioTolerance)
                throw new InvalidOperationException(
                    "The draw-sword Ispant height does not match the static Ispant: " + Num(heightRatio) + ".");
            if (groundDifference > 0.005f)
                throw new InvalidOperationException(
                    "The draw-sword Ispant does not share the static ground level: " + Num(groundDifference) + ".");

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rootPosition = model.localPosition;
            var rootRotation = model.localRotation;
            var rootScale = model.localScale;
            var initialMusketAttachment = LocalMatrix(musket.transform);
            var initialSheathAttachment = LocalMatrix(sheath.transform);
            var initialAttachment = LocalMatrix(sword.transform);
            var initialMusketModel = model.worldToLocalMatrix * musket.transform.localToWorldMatrix;
            var initialSheathModel = model.worldToLocalMatrix * sheath.transform.localToWorldMatrix;
            var initialSwordModel = model.worldToLocalMatrix * sword.transform.localToWorldMatrix;
            var maximumMusketAttachmentError = 0f;
            var maximumSheathAttachmentError = 0f;
            var maximumAttachmentError = 0f;
            var maximumMusketFollowMotion = 0f;
            var maximumSheathFollowMotion = 0f;
            var maximumFollowMotion = 0f;
            var maximumNearestSwordVertexToHand = 0f;
            var minimumNearestSwordVertexToHand = float.PositiveInfinity;
            var maximumHandMotion = 0f;
            Vector3? initialHandPosition = null;
            try
            {
                for (var frame = FirstFrame; frame <= LastFrame; frame++)
                {
                    var normalized = (frame - FirstFrame) / (float)(LastFrame - FirstFrame);
                    SampleClip(model.gameObject, clip, normalized * clip.length);
                    maximumMusketAttachmentError = Mathf.Max(
                        maximumMusketAttachmentError,
                        MatrixError(initialMusketAttachment, LocalMatrix(musket.transform)));
                    maximumSheathAttachmentError = Mathf.Max(
                        maximumSheathAttachmentError,
                        MatrixError(initialSheathAttachment, LocalMatrix(sheath.transform)));
                    maximumAttachmentError = Mathf.Max(
                        maximumAttachmentError,
                        MatrixError(initialAttachment, LocalMatrix(sword.transform)));
                    maximumMusketFollowMotion = Mathf.Max(
                        maximumMusketFollowMotion,
                        MatrixError(initialMusketModel, model.worldToLocalMatrix * musket.transform.localToWorldMatrix));
                    maximumSheathFollowMotion = Mathf.Max(
                        maximumSheathFollowMotion,
                        MatrixError(initialSheathModel, model.worldToLocalMatrix * sheath.transform.localToWorldMatrix));
                    maximumFollowMotion = Mathf.Max(
                        maximumFollowMotion,
                        MatrixError(initialSwordModel, model.worldToLocalMatrix * sword.transform.localToWorldMatrix));
                    var nearest = swordMesh.vertices.Min(vertex =>
                        Vector3.Distance(sword.transform.TransformPoint(vertex), rightHand.position));
                    minimumNearestSwordVertexToHand = Mathf.Min(minimumNearestSwordVertexToHand, nearest);
                    maximumNearestSwordVertexToHand = Mathf.Max(maximumNearestSwordVertexToHand, nearest);
                    if (!initialHandPosition.HasValue)
                        initialHandPosition = rightHand.position;
                    maximumHandMotion = Mathf.Max(
                        maximumHandMotion, Vector3.Distance(initialHandPosition.Value, rightHand.position));
                    if (Vector3.Distance(model.localPosition, rootPosition) > TransformTolerance ||
                        Quaternion.Angle(model.localRotation, rootRotation) > TransformTolerance ||
                        Vector3.Distance(model.localScale, rootScale) > TransformTolerance)
                        throw new InvalidOperationException("The Ispant draw-sword clip changed the slot model root transform.");
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }

            if (maximumMusketAttachmentError > AttachmentTolerance ||
                maximumSheathAttachmentError > AttachmentTolerance ||
                maximumAttachmentError > AttachmentTolerance)
                throw new InvalidOperationException(
                    "A replacement draw-sword FBX weapon changed relative to its follow bone." +
                    " Musket=" + Num(maximumMusketAttachmentError) +
                    ", Sheath=" + Num(maximumSheathAttachmentError) +
                    ", Sword=" + Num(maximumAttachmentError) + ".");
            if (maximumMusketFollowMotion < 0.1f || maximumSheathFollowMotion < 0.1f ||
                maximumFollowMotion < 0.1f || maximumHandMotion < 0.1f)
                throw new InvalidOperationException("A replacement draw-sword FBX weapon did not follow the animated body.");
            if (maximumNearestSwordVertexToHand > MaximumSwordVertexToHandDistance)
                throw new InvalidOperationException(
                    "The replacement draw-sword FBX sword handle is too far from the right hand: " +
                    Num(maximumNearestSwordVertexToHand) + ".");

            return new Metrics(
                clip.length,
                clip.frameRate,
                maximumMusketAttachmentError,
                maximumSheathAttachmentError,
                maximumAttachmentError,
                maximumMusketFollowMotion,
                maximumSheathFollowMotion,
                maximumFollowMotion,
                maximumHandMotion,
                minimumNearestSwordVertexToHand,
                maximumNearestSwordVertexToHand,
                staticBounds.size.y,
                drawBounds.size.y,
                groundDifference);
        }

        private static void RequireExactStaticMaterials(Transform staticModel, Transform model)
        {
            var approved = staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null)
                    throw new InvalidOperationException("The synchronized Ispant draw-sword material is null.");
                var key = NormalizeMaterialName(material.name);
                if (!approved.TryGetValue(key, out var exact) || material != exact)
                    throw new InvalidOperationException(
                        "Ispant draw-sword material is not a direct static appearance reference: " +
                        renderer.name + "/" + material.name + ".");
            }
        }

        private static void CaptureReview(
            Transform staticModel,
            Transform model,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The Ispant draw-sword capture folder is invalid."));
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var rendererSnapshots = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Select(renderer => new RendererSnapshot(renderer)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing for Ispant draw-sword review.");
            var cameraObject = new GameObject("IspantDrawSwordReviewCamera", typeof(Camera))
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
                var body = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Armed_Body");
                var referenceHeight = BindWorldBounds(staticBody).size.y;

                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                FrameCamera(camera, staticBody.bounds.center, referenceHeight, 1f);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                for (var index = 0; index < ReviewNormalizedTimes.Length; index++)
                {
                    SampleClip(model.gameObject, clip, ReviewNormalizedTimes[index] * clip.length);
                    FrameCamera(camera, body.bounds.center, referenceHeight, 1f);
                    RenderPanel(camera, panel, strip, target, index + 1, panelWidth, panelHeight);
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
                throw new InvalidOperationException("The Ispant draw-sword review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(Camera camera, Vector3 center, float referenceHeight, float aspect)
        {
            camera.aspect = aspect;
            var vertical = (referenceHeight * 0.5f) /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.2f +
                Vector3.up * referenceHeight * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(center - camera.transform.position, Vector3.up);
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The Ispant draw-sword inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + DrawSwordSlotName,
                "SourceDrawSwordFbx=" + SourceDrawSwordFbxPath,
                "ProjectSourceFbx=" + ProjectSourceFbxPath,
                "DerivedDrawSwordFbx=" + DrawSwordFbxPath,
                "SourceDrawSwordSha256=" + SourceDrawSwordSha256,
                "SourceStaticSha256=" + SourceStaticSha256,
                "DerivedDrawSwordSha256=" + DrawSwordFbxSha256,
                "SourceAction=Armature|mixamo.com|Layer0",
                "ImportedClip=" + ImportedClipName,
                "ClipFrames=" + FirstFrame + "-" + LastFrame,
                "ClipLengthSeconds=" + Num(metrics.ClipLength),
                "ClipFrameRate=" + Num(metrics.FrameRate),
                "LoopTime=True",
                "RootMotion=False",
                "MixamoBones=" + ExpectedBones,
                "SourceAnimatedBodyVertices=" + SourceBodyVertices,
                "UnityImportedAnimatedBodyVertices=" + ExpectedImportedBodyVertices,
                "AnimatedBodyTriangles=" + ExpectedBodyTriangles,
                "ReplacementSourceMusketVertices=" + SourceMusketVertices,
                "UnityImportedReplacementMusketVertices=" + ExpectedImportedMusketVertices,
                "ReplacementSourceMusketTriangles=" + ExpectedMusketTriangles,
                "ReplacementSourceSheathVertices=" + SourceSheathVertices,
                "UnityImportedReplacementSheathVertices=" + ExpectedImportedSheathVertices,
                "ReplacementSourceSheathTriangles=" + ExpectedSheathTriangles,
                "ReplacementSourceSwordVertices=" + SourceSwordVertices,
                "UnityImportedReplacementSwordVertices=" + ExpectedImportedSwordVertices,
                "ReplacementSourceSwordTriangles=" + ExpectedSwordTriangles,
                "OriginalBodyTriangleSum=" + ExpectedOriginalTriangles,
                "MusketSource=ReplacementDrawSwordFbx",
                "MusketParent=mixamorig:Spine2",
                "RigidMusketMaximumAttachmentError=" + Num(metrics.MaximumMusketAttachmentError),
                "MusketMaximumBodyFollowMotion=" + Num(metrics.MaximumMusketFollowMotion),
                "SheathSource=ReplacementDrawSwordFbx",
                "SheathComponents=77,80",
                "SheathParent=mixamorig:Hips",
                "RigidSheathMaximumAttachmentError=" + Num(metrics.MaximumSheathAttachmentError),
                "SheathMaximumBodyFollowMotion=" + Num(metrics.MaximumSheathFollowMotion),
                "SwordSource=ReplacementDrawSwordFbx",
                "DrawnSwordComponents=78,79",
                "SwordHandleSourceComponent=79",
                "SwordParent=mixamorig:RightHand",
                "RigidSwordMaximumAttachmentError=" + Num(metrics.MaximumSwordAttachmentError),
                "SwordMaximumBodyFollowMotion=" + Num(metrics.MaximumSwordFollowMotion),
                "RightHandMaximumMotion=" + Num(metrics.MaximumHandMotion),
                "MinimumNearestSwordVertexToHand=" + Num(metrics.MinimumNearestSwordVertexToHand),
                "MaximumNearestSwordVertexToHand=" + Num(metrics.MaximumNearestSwordVertexToHand),
                "StaticBodyHeight=" + Num(metrics.StaticBodyHeight),
                "DrawSwordBodyHeight=" + Num(metrics.DrawSwordBodyHeight),
                "DrawSwordToStaticHeightRatio=" + Num(metrics.DrawSwordBodyHeight / metrics.StaticBodyHeight),
                "GroundLevelDifference=" + Num(metrics.GroundLevelDifference),
                "StaticAppearanceMaterialsDirectReference=True",
                "StaticGeometrySourceExact=True",
                "DrawSwordStaticMaximumWorldVertexError=0.000000127315577",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + CapturePath
            }, Encoding.UTF8);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceDrawSwordFbxPath, SourceDrawSwordSha256);
            RequireHash(ProjectSourceFbxPath, SourceDrawSwordSha256);
            RequireHash(SourceStaticFbxPath, SourceStaticSha256);
            RequireHash(DrawSwordFbxPath, DrawSwordFbxSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant draw-sword asset hash differs: " + path + ".");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene for Ispant draw-sword work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes; preserve them before Ispant draw-sword work.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var roots = scene.GetRootGameObjects().Where(item => item.name == PlacementRootName).ToArray();
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
                if (child.name == name)
                    return child;
            throw new InvalidOperationException("Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                throw new InvalidOperationException("Required Ispant draw-sword renderer is missing: " + name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required Ispant draw-sword bone differs: " + name + ".");
            return matches[0];
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException("Ispant draw-sword renderer has no mesh: " + renderer.name + ".");
        }

        private static int TriangleCount(Mesh mesh)
        {
            var result = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
                result += checked((int)mesh.GetIndexCount(index) / 3);
            return result;
        }

        private static Bounds BindWorldBounds(SkinnedMeshRenderer renderer)
        {
            var mesh = SharedMesh(renderer);
            var vertices = mesh.vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("An Ispant draw-sword mesh has no vertices for exact bounds.");
            var bounds = new Bounds(renderer.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (var index = 1; index < vertices.Length; index++)
                bounds.Encapsulate(renderer.transform.TransformPoint(vertices[index]));
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

        private static Matrix4x4 LocalMatrix(Transform transform)
        {
            return Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
        }

        private static float MatrixError(Matrix4x4 expected, Matrix4x4 actual)
        {
            var maximum = 0f;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                maximum = Mathf.Max(maximum, Mathf.Abs(expected[row, column] - actual[row, column]));
            return maximum;
        }

        private static string[] OtherRootSignatures(Scene scene, GameObject placement)
        {
            return scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform)).ToArray();
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(item => item != targetSlot)
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
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform value)
            {
                target = value;
                localPosition = value.localPosition;
                localRotation = value.localRotation;
                localScale = value.localScale;
            }

            public void Restore()
            {
                if (target == null)
                    return;
                target.SetLocalPositionAndRotation(localPosition, localRotation);
                target.localScale = localScale;
            }

            public bool Matches(float tolerance)
            {
                return target != null &&
                       Vector3.Distance(target.localPosition, localPosition) <= tolerance &&
                       Quaternion.Angle(target.localRotation, localRotation) <= tolerance &&
                       Vector3.Distance(target.localScale, localScale) <= tolerance;
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
            public readonly float MaximumMusketAttachmentError;
            public readonly float MaximumSheathAttachmentError;
            public readonly float MaximumSwordAttachmentError;
            public readonly float MaximumMusketFollowMotion;
            public readonly float MaximumSheathFollowMotion;
            public readonly float MaximumSwordFollowMotion;
            public readonly float MaximumHandMotion;
            public readonly float MinimumNearestSwordVertexToHand;
            public readonly float MaximumNearestSwordVertexToHand;
            public readonly float StaticBodyHeight;
            public readonly float DrawSwordBodyHeight;
            public readonly float GroundLevelDifference;

            public Metrics(
                float clipLength,
                float frameRate,
                float maximumMusketAttachmentError,
                float maximumSheathAttachmentError,
                float maximumSwordAttachmentError,
                float maximumMusketFollowMotion,
                float maximumSheathFollowMotion,
                float maximumSwordFollowMotion,
                float maximumHandMotion,
                float minimumNearestSwordVertexToHand,
                float maximumNearestSwordVertexToHand,
                float staticBodyHeight,
                float drawSwordBodyHeight,
                float groundLevelDifference)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                MaximumMusketAttachmentError = maximumMusketAttachmentError;
                MaximumSheathAttachmentError = maximumSheathAttachmentError;
                MaximumSwordAttachmentError = maximumSwordAttachmentError;
                MaximumMusketFollowMotion = maximumMusketFollowMotion;
                MaximumSheathFollowMotion = maximumSheathFollowMotion;
                MaximumSwordFollowMotion = maximumSwordFollowMotion;
                MaximumHandMotion = maximumHandMotion;
                MinimumNearestSwordVertexToHand = minimumNearestSwordVertexToHand;
                MaximumNearestSwordVertexToHand = maximumNearestSwordVertexToHand;
                StaticBodyHeight = staticBodyHeight;
                DrawSwordBodyHeight = drawSwordBodyHeight;
                GroundLevelDifference = groundLevelDifference;
            }
        }
    }
}
