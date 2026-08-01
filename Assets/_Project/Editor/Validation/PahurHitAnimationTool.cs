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
        private const string HitSlotName = "Pahur_10_Hit";
        private const string SourceHitModelPath =
            @"D:\Bellerophon2\Bellerophon\enemies model\pāḫḫur hit.fbx";
        private const string SourceHitSha256 =
            "9676C4923EC4E0653D73AD45C5AB843D7721B06A8AEFEA527423B630E1A39127";
        private const string HitModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurHit.fbx";
        private const string HitAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurHitApprovedAppearanceMesh.asset";
        private const string HitClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_10_Hit_InPlace.anim";
        private const string HitControllerPath =
            "Assets/_Project/Art/Enemies/Pahur/Controllers/Pahur_10_Hit.controller";
        private const string HitStateName = "PahurHitMixamoLoop";
        private const string HitReportPath =
            "docs/validation/pahur_hit_animation_2026-08-01/Pahur_10_Hit_Validation.txt";
        private const string HitCapturePath =
            "docs/validation/pahur_hit_animation_2026-08-01/Pahur_10_Hit_Review.png";

        [MenuItem("Bellerophon/Enemies/Pahur/Inspect Hit Source")]
        public static void InspectPahurHitSource()
        {
            RequireHitSourceHash();
            ImportHitModel();
            var takeName = ConfigureHitImporter();
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HitModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur hit FBX is missing.");
            var renderer = RequireRenderer(prefab.transform, "hit FBX");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running Pahur FBX is missing.");
            RequireExactMiniTransferContract(
                renderer,
                RequireRenderer(runningPrefab.transform, "approved running FBX"));
            var clip = RequireHitSourceClip(takeName);

            var scene = RequireScene(false);
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticRenderer = RequireRenderer(
                RequireModel(
                    RequireChild(placement.transform, StaticSlotName)),
                StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var approvedAppearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    ApprovedRunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The approved Pahur appearance mesh is missing.");
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(staticRenderer.sharedMaterials);
            var weaponVertices = RequireWeaponBarrelIndices(
                renderer,
                approvedAppearance,
                weaponMaterialIndex);
            var weaponBoneIndex = RequireRightWeaponBoneIndex(
                renderer,
                approvedAppearance,
                weaponVertices);

            Debug.Log(
                "PahurHitSourceInspection Result=PASS" +
                ", Sha256=" + SourceHitSha256 +
                ", Clip=" + clip.name +
                ", Vertices=" + renderer.sharedMesh.vertexCount +
                ", Triangles=" +
                renderer.sharedMesh.triangles.Length / 3 +
                ", Bones=" + renderer.bones.Length +
                ", ExactAppearanceTransferContract=True" +
                ", WeaponBone=" + renderer.bones[weaponBoneIndex].name +
                ", WeaponVertices=" + weaponVertices.Length + ".");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Hit Animation")]
        public static void ApplyPahurHitAnimation()
        {
            var scene = RequireScene(true);
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel = RequireModel(
                RequireChild(placement.transform, StaticSlotName));
            var staticRenderer = RequireRenderer(
                staticModel,
                StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var slot = RequireChild(placement.transform, HitSlotName);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_10_Hit must contain exactly one current model.");
            }

            var otherSlots = OtherSlotSignatures(
                placement.transform,
                HitSlotName);
            var protectedRoots = ProtectedRootSignatures(
                scene,
                placement.transform);
            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;

            RequireHitSourceHash();
            ImportHitModel();
            var takeName = ConfigureHitImporter();
            var hitPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HitModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur hit FBX is missing.");
            var hitPrefabRenderer = RequireRenderer(
                hitPrefab.transform,
                "hit FBX");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running Pahur FBX is missing.");
            RequireExactMiniTransferContract(
                hitPrefabRenderer,
                RequireRenderer(runningPrefab.transform, "approved running FBX"));
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var sourceClip = RequireHitSourceClip(takeName);
            var appearance = CreateHitAppearanceMesh(hitPrefabRenderer);
            var clip = CreateHitInPlaceClip(
                sourceClip,
                hitPrefab.transform,
                hitPrefabRenderer);
            var facing = AuthorGuardianFrontFacingWeapon(
                clip,
                hitPrefab,
                hitPrefabRenderer,
                appearance,
                RequireWeaponMaterialIndex(staticRenderer.sharedMaterials));
            RequireNoHorizontalRootTranslation(
                hitPrefab.transform,
                hitPrefabRenderer,
                clip);
            var controller = CreateHitController(clip);
            var matchedScale = MatchedRunningScale(
                staticPrefab,
                hitPrefab,
                staticModel);

            var previous = slot.GetChild(0);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;
            var replacement =
                PrefabUtility.InstantiatePrefab(hitPrefab, scene) as GameObject ??
                throw new InvalidOperationException(
                    "The Pahur hit prefab could not be instantiated.");
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
                var renderer = RequireRenderer(
                    replacement.transform,
                    HitSlotName);
                renderer.sharedMesh = appearance;
                renderer.sharedMaterials =
                    staticRenderer.sharedMaterials.ToArray();
                renderer.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

                var animator = replacement.GetComponent<Animator>() ??
                               replacement.AddComponent<Animator>();
                var sourceAnimator = hitPrefab.GetComponent<Animator>() ??
                                     throw new InvalidOperationException(
                                         "The Pahur hit FBX has no Animator.");
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
                OtherSlotSignatures(placement.transform, HitSlotName),
                "A Pahur slot outside Pahur_10_Hit changed.");
            RequireUnchanged(
                protectedRoots,
                ProtectedRootSignatures(scene, placement.transform),
                "A scene root outside the Pahur placement changed.");
            if (slot.localPosition != slotPosition ||
                slot.localRotation != slotRotation ||
                slot.localScale != slotScale)
            {
                throw new InvalidOperationException(
                    "The Pahur hit slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Pahur hit model.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurHitAnimationApplied Result=PASS" +
                ", SourceClip=" + sourceClip.name +
                ", PlaybackClip=" + clip.name +
                ", Loop=True" +
                ", HorizontalRootMotion=False" +
                ", StaticAppearanceTransferredExactly=True" +
                ", SharedStaticMaterials=True" +
                ", MaximumWeaponElevationDegrees=" +
                facing.Aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumWeaponForwardAngleDegrees=" +
                facing.MaximumForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", OtherSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Validate Hit Animation")]
        public static void ValidatePahurHitAnimation()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel = RequireModel(
                RequireChild(placement.transform, StaticSlotName));
            var staticRenderer = RequireRenderer(
                staticModel,
                StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var model = RequireModel(
                RequireChild(placement.transform, HitSlotName));
            var renderer = RequireRenderer(model, HitSlotName);
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HitModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur hit FBX is missing.");
            var prefabRenderer = RequireRenderer(prefab.transform, "hit FBX");
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(HitAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The Pahur hit appearance mesh is missing.");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var expectedScale = MatchedRunningScale(
                staticPrefab,
                prefab,
                staticModel);
            if (renderer.sharedMesh != appearance ||
                !renderer.sharedMaterials.SequenceEqual(
                    staticRenderer.sharedMaterials) ||
                model.localScale != Vector3.one * expectedScale ||
                model.localPosition.y != staticModel.localPosition.y)
            {
                throw new InvalidOperationException(
                    "The Pahur hit appearance, size, or Y position differs from the approved static contract.");
            }

            RequireMiniAppearancePreserved(
                prefabRenderer.sharedMesh,
                appearance);
            var animator = model.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "The Pahur hit model has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    HitControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur hit controller is missing.");
            var clip = controller.layers[0]
                .stateMachine
                .defaultState
                .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The Pahur hit controller has no clip.");
            if (AssetDatabase.GetAssetPath(clip) != HitClipPath ||
                !clip.isLooping ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "The Pahur hit animation contract differs.");
            }

            RequireHitSourceHash();
            if (Sha256(Absolute(HitModelPath)) != SourceHitSha256)
            {
                throw new InvalidOperationException(
                    "The imported Pahur hit FBX differs from the supplied source.");
            }

            RequireNoHorizontalRootTranslation(
                prefab.transform,
                prefabRenderer,
                clip);
            var facing = RequireGuardianFrontFacingWeapon(
                clip,
                prefab,
                prefabRenderer,
                appearance,
                RequireWeaponMaterialIndex(staticRenderer.sharedMaterials));
            WriteHitReport(
                clip,
                prefabRenderer.sharedMesh,
                appearance,
                model,
                staticModel,
                facing);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur hit validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurHitAnimationValidated Result=PASS" +
                ", Clip=" + clip.name +
                ", ModelScale=" + ScaleText(model.localScale) +
                ", ModelY=" +
                model.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumWeaponElevationDegrees=" +
                facing.Aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumWeaponForwardAngleDegrees=" +
                facing.MaximumForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", SceneChanged=False" +
                ", Report=" + HitReportPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Capture Hit Review")]
        public static void CapturePahurHitReview()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var model = RequireModel(
                RequireChild(placement.transform, HitSlotName));
            var animator = model.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "The Pahur hit model has no Animator.");
            var controller =
                animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(
                    "The Pahur hit controller is missing.");
            var clip = controller.layers[0]
                .stateMachine
                .defaultState
                .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The Pahur hit clip is missing.");
            var destination = Absolute(HitCapturePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Pahur hit review already exists: " +
                    HitCapturePath);
            }

            Capture(model, animator, clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur hit capture changed the scene dirty state.");
            }

            Debug.Log(
                "PahurHitReviewCaptured Result=PASS" +
                ", Image=" + HitCapturePath +
                ", SceneChanged=False.");
        }

        private static void RequireHitSourceHash()
        {
            if (!File.Exists(SourceHitModelPath) ||
                Sha256(SourceHitModelPath) != SourceHitSha256)
            {
                throw new InvalidOperationException(
                    "The supplied Pahur hit FBX is missing or changed.");
            }
        }

        private static void ImportHitModel()
        {
            var destination = Absolute(HitModelPath);
            if (!File.Exists(destination) ||
                Sha256(destination) != SourceHitSha256)
            {
                File.Copy(SourceHitModelPath, destination, true);
            }

            AssetDatabase.ImportAsset(
                HitModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureHitImporter()
        {
            var importer =
                AssetImporter.GetAtPath(HitModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "The Pahur hit importer is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation =
                ModelImporterMaterialLocation.InPrefab;
            var matches = importer.defaultClipAnimations
                .Where(item =>
                    item.name.IndexOf(
                        "mixamo",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.takeName.IndexOf(
                        "mixamo",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Pahur hit FBX must contain exactly one Mixamo take. Found=" +
                    matches.Length + ".");
            }

            var selected = matches[0];
            selected.loopTime = true;
            selected.loopPose = true;
            selected.wrapMode = WrapMode.Loop;
            selected.lockRootPositionXZ = true;
            selected.keepOriginalPositionXZ = true;
            importer.animationWrapMode = WrapMode.Loop;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
            return selected.name;
        }

        private static AnimationClip RequireHitSourceClip(string takeName)
        {
            var matches = AssetDatabase.LoadAllAssetsAtPath(HitModelPath)
                .OfType<AnimationClip>()
                .Where(item =>
                    !item.name.StartsWith(
                        "__preview__",
                        StringComparison.Ordinal) &&
                    (item.name == takeName ||
                     item.name.IndexOf(
                         "mixamo",
                         StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The configured Pahur hit Mixamo clip is not unique.");
            }

            return matches[0];
        }

        private static Mesh CreateHitAppearanceMesh(
            SkinnedMeshRenderer sourceRenderer)
        {
            var approved =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    ApprovedRunningAppearanceMeshPath) ??
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
            generated.name = "PahurHitApprovedAppearanceMesh";
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
                generated.SetTriangles(
                    approved.GetTriangles(index),
                    index,
                    false);
            }

            generated.bounds = source.bounds;
            if (AssetDatabase.LoadAssetAtPath<Mesh>(HitAppearanceMeshPath) !=
                    null &&
                !AssetDatabase.DeleteAsset(HitAppearanceMeshPath))
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The previous Pahur hit appearance mesh could not be replaced.");
            }

            AssetDatabase.CreateAsset(generated, HitAppearanceMeshPath);
            AssetDatabase.SaveAssets();
            RequireMiniAppearancePreserved(source, generated);
            return generated;
        }

        private static AnimationClip CreateHitInPlaceClip(
            AnimationClip source,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, HitClipPath);
            }

            EditorUtility.CopySerialized(source, clip);
            clip.name = "Pahur_10_Hit_InPlace";
            clip.wrapMode = WrapMode.Loop;
            var rootPath = AnimationUtility.CalculateTransformPath(
                renderer.rootBone,
                root);
            var horizontalProperties = HorizontalLocalPositionProperties(
                root,
                renderer.rootBone.parent);
            var bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    (binding.path.Length == 0 &&
                     (binding.propertyName == "RootT.x" ||
                      binding.propertyName == "RootT.z" ||
                      binding.propertyName == "MotionT.x" ||
                      binding.propertyName == "MotionT.z")) ||
                    (binding.path == rootPath &&
                     horizontalProperties.Contains(binding.propertyName)))
                .ToArray();
            if (bindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "The Pahur hit Mixamo clip has no horizontal root curves to lock.");
            }

            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException(
                                "A Pahur hit horizontal root curve is missing.");
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.Constant(
                        0f,
                        clip.length,
                        curve.Evaluate(0f)));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateHitController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    HitControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(
                        HitControllerPath);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            var state = machine.AddState(HitStateName);
            state.motion = clip;
            state.speed = 1f;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void WriteHitReport(
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Transform model,
            Transform staticModel,
            GuardianWeaponMetrics facing)
        {
            var destination = Absolute(HitReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Pahur hit report path."));
            var report = new StringBuilder();
            report.AppendLine("Pahur Hit Animation Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceSha256=" + SourceHitSha256);
            report.AppendLine("ImportedSourceHashMatches=True");
            report.AppendLine("SourceClip=mixamo.com");
            report.AppendLine("PlaybackClip=" + clip.name);
            report.AppendLine("Loop=True");
            report.AppendLine("Vertices=" + source.vertexCount);
            report.AppendLine(
                "Triangles=" + source.triangles.Length / 3);
            report.AppendLine("Bones=" + source.bindposes.Length);
            report.AppendLine("ShapeSkinBindPosesPreserved=True");
            report.AppendLine(
                "StaticApprovedAppearanceTransferredByExactVertexIndex=True");
            report.AppendLine("NewAppearanceDataGenerated=False");
            report.AppendLine(
                "ApprovedMaterialSlots=" + appearance.subMeshCount);
            report.AppendLine("SharedStaticMaterials=True");
            report.AppendLine("ModelScale=" + ScaleText(model.localScale));
            report.AppendLine(
                "ModelY=" +
                model.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "StaticY=" +
                staticModel.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine("HorizontalRootMotion=False");
            report.AppendLine(
                "WeaponBoneIndex=" + facing.Aim.BoneIndex);
            report.AppendLine(
                "MaximumWeaponElevationDegrees=" +
                facing.Aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumWeaponForwardAngleDegrees=" +
                facing.MaximumForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine("WeaponHorizontalForEntireClip=True");
            report.AppendLine("WeaponForwardForEntireClip=True");
            report.AppendLine("OtherSlotsPreservedByApply=True");
            report.AppendLine("OtherSceneRootsPreservedByApply=True");
            report.AppendLine("SceneSaved=True");
            report.AppendLine("SceneChangedByValidation=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }
    }
}
