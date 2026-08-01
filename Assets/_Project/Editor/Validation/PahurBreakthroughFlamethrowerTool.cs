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
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static partial class PahurRunningModelAndAnimationTool
    {
        private const string BreakthroughSlotName =
            "Pahur_05_BreakthroughFlamethrower";
        private const string SourceBreakthroughModelPath =
            @"D:\Bellerophon2\Bellerophon\enemies model\pāḫḫur breakthrough.fbx";
        private const string SourceBreakthroughSha256 =
            "B203DCC2134B2FFD54EDBC4D5E5A193675DC4B49C6170B95A7D89CF5A5C59F4A";
        private const string BreakthroughModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurBreakthrough.fbx";
        private const string BreakthroughAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurBreakthroughApprovedAppearanceMesh.asset";
        private const string BreakthroughClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_05_BreakthroughFlamethrower_InPlace.anim";
        private const string BreakthroughControllerPath =
            "Assets/_Project/Art/Enemies/Pahur/Controllers/Pahur_05_BreakthroughFlamethrower.controller";
        private const string BreakthroughStateName =
            "PahurBreakthroughFlamethrower";
        private const string BreakthroughMuzzleName =
            "Pahur_BreakthroughFlamethrower_Muzzle";
        private const string BreakthroughReportPath =
            "docs/validation/pahur_breakthrough_flamethrower_2026-07-31/Pahur_05_BreakthroughFlamethrower_Validation.txt";
        private const string BreakthroughCapturePath =
            "docs/validation/pahur_breakthrough_flamethrower_2026-07-31/Pahur_05_BreakthroughFlamethrower_Review.png";

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Inspect Breakthrough Source")]
        public static void InspectPahurBreakthroughSource()
        {
            RequireBreakthroughSourceHash();
            ImportBreakthroughModel();
            var takeName =
                ConfigureBreakthroughImporter();
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BreakthroughModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur breakthrough FBX is missing.");
            var renderer =
                RequireRenderer(
                    prefab.transform,
                    "breakthrough FBX");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running FBX is missing.");
            var runningRenderer =
                RequireRenderer(
                    runningPrefab.transform,
                    "approved running FBX");
            RequireExactMiniTransferContract(
                renderer,
                runningRenderer);
            var clip =
                RequireBreakthroughSourceClip(
                    takeName);
            var scene =
                RequireScene(false);
            var placement =
                RequirePlacement(scene);
            var staticRenderer =
                RequireRenderer(
                    RequireModel(
                        RequireChild(
                            placement.transform,
                            StaticSlotName)),
                    StaticSlotName);
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var approved =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    ApprovedRunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The approved running appearance mesh is missing.");
            var barrelIndices =
                RequireWeaponBarrelIndices(
                    renderer,
                    approved,
                    weaponMaterialIndex);
            var weaponBoneIndex =
                RequireRightWeaponBoneIndex(
                    renderer,
                    approved,
                    barrelIndices);
            Debug.Log(
                "PahurBreakthroughSourceInspection Result=PASS" +
                ", Sha256=" +
                SourceBreakthroughSha256 +
                ", Clip=" +
                clip.name +
                ", Vertices=" +
                renderer.sharedMesh.vertexCount +
                ", Bones=" +
                renderer.bones.Length +
                ", ExactAppearanceTransferContract=True" +
                ", WeaponBone=" +
                renderer.bones[weaponBoneIndex].name +
                ", WeaponVertices=" +
                barrelIndices.Length +
                ".");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Apply Breakthrough Flamethrower")]
        public static void ApplyPahurBreakthroughFlamethrower()
        {
            var activeScene =
                SceneManager.GetActiveScene();
            if (activeScene.IsValid() &&
                activeScene.isLoaded &&
                activeScene.isDirty &&
                string.Equals(
                    activeScene.path,
                    ScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
            }

            var scene =
                RequireScene(true);
            var placement =
                RequirePlacement(scene);
            RequireSlots(
                placement.transform);
            var staticModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        StaticSlotName));
            var staticRenderer =
                RequireRenderer(
                    staticModel,
                    StaticSlotName);
            RequireApprovedMaterials(
                staticRenderer);
            var slot =
                RequireChild(
                    placement.transform,
                    BreakthroughSlotName);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_05_BreakthroughFlamethrower must contain exactly one current model.");
            }

            var otherSlots =
                OtherSlotSignatures(
                    placement.transform,
                    BreakthroughSlotName);
            var protectedRoots =
                ProtectedRootSignatures(
                    scene,
                    placement.transform);
            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;

            RequireBreakthroughSourceHash();
            ImportBreakthroughModel();
            var takeName =
                ConfigureBreakthroughImporter();
            var breakthroughPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BreakthroughModelPath) ??
                throw new InvalidOperationException(
                    "The breakthrough FBX is missing.");
            var breakthroughRenderer =
                RequireRenderer(
                    breakthroughPrefab.transform,
                    "breakthrough FBX");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var sourceClip =
                RequireBreakthroughSourceClip(
                    takeName);
            var appearance =
                CreateBreakthroughAppearanceMesh(
                    breakthroughRenderer);
            var clip =
                CreateBreakthroughInPlaceClip(
                    sourceClip,
                    breakthroughPrefab.transform,
                    breakthroughRenderer);
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var facing =
                AuthorBreakthroughFrontFacingPose(
                    clip,
                    breakthroughPrefab,
                    breakthroughRenderer,
                    appearance,
                    weaponMaterialIndex,
                    staticPrefab);
            var aim = facing.Aim;
            RequireNoHorizontalRootTranslation(
                breakthroughPrefab.transform,
                breakthroughRenderer,
                clip);
            var controller =
                CreateBreakthroughController(
                    clip);
            var matchedScale =
                MatchedRunningScale(
                    staticPrefab,
                    breakthroughPrefab,
                    staticModel);
            var miniMuzzle =
                RequireValidatedMiniFlameMuzzle(
                    placement.transform,
                    staticRenderer);

            var previous = slot.GetChild(0);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;
            var replacement =
                PrefabUtility.InstantiatePrefab(
                    breakthroughPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "The breakthrough prefab could not be instantiated.");
            replacement.name = ModelName;
            replacement.transform.SetParent(
                slot,
                false);
            replacement.transform.SetLocalPositionAndRotation(
                new Vector3(
                    previousPosition.x,
                    staticModel.localPosition.y,
                    previousPosition.z),
                previousRotation);
            replacement.transform.localScale =
                Vector3.one * matchedScale;
            try
            {
                var renderer =
                    RequireRenderer(
                        replacement.transform,
                        BreakthroughSlotName);
                renderer.sharedMesh = appearance;
                renderer.sharedMaterials =
                    staticRenderer.sharedMaterials.ToArray();
                renderer.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    renderer);

                var animator =
                    replacement.GetComponent<Animator>() ??
                    replacement.AddComponent<Animator>();
                var sourceAnimator =
                    breakthroughPrefab.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "The breakthrough FBX has no Animator.");
                animator.avatar = sourceAnimator.avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);

                var targetBone =
                    renderer.bones[aim.BoneIndex];
                var muzzle =
                    UnityEngine.Object.Instantiate(
                        miniMuzzle.gameObject,
                        targetBone,
                        false);
                muzzle.name = BreakthroughMuzzleName;
                muzzle.transform.localPosition =
                    aim.MuzzleLocalPosition;
                muzzle.transform.localRotation =
                    aim.MuzzleLocalRotation;
                muzzle.transform.localScale = Vector3.one;
                RequireExactFlameEffectCopy(
                    miniMuzzle,
                    muzzle.transform);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(
                    replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(
                previous.gameObject);
            RequireUnchanged(
                otherSlots,
                OtherSlotSignatures(
                    placement.transform,
                    BreakthroughSlotName),
                "A Pahur slot outside Pahur_05_BreakthroughFlamethrower changed.");
            RequireUnchanged(
                protectedRoots,
                ProtectedRootSignatures(
                    scene,
                    placement.transform),
                "A scene root outside the Pahur placement changed.");
            if (slot.localPosition != slotPosition ||
                slot.localRotation != slotRotation ||
                slot.localScale != slotScale)
            {
                throw new InvalidOperationException(
                    "The breakthrough slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurBreakthroughFlamethrowerApplied Result=PASS" +
                ", SourceClip=" +
                sourceClip.name +
                ", Loop=True" +
                ", HorizontalRootMotion=False" +
                ", ExactApprovedAppearanceChannels=True" +
                ", HorizontalWeapon=True" +
                ", ForwardWeapon=True" +
                ", StaticFrontFacingHead=True" +
                ", ExistingMiniFlameEffectCopied=True" +
                ", OtherSlotsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Validate Breakthrough Flamethrower")]
        public static void ValidatePahurBreakthroughFlamethrower()
        {
            var scene =
                RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement =
                RequirePlacement(scene);
            RequireSlots(
                placement.transform);
            var staticModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        StaticSlotName));
            var staticRenderer =
                RequireRenderer(
                    staticModel,
                    StaticSlotName);
            var model =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        BreakthroughSlotName));
            var renderer =
                RequireRenderer(
                    model,
                    BreakthroughSlotName);
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BreakthroughModelPath) ??
                throw new InvalidOperationException(
                    "The breakthrough FBX is missing.");
            var prefabRenderer =
                RequireRenderer(
                    prefab.transform,
                    "breakthrough FBX");
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    BreakthroughAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The breakthrough appearance mesh is missing.");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The static Pahur FBX is missing.");
            var expectedScale =
                MatchedRunningScale(
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
                    "The breakthrough Pahur appearance, size, or Y position differs.");
            }

            RequireMiniAppearancePreserved(
                prefabRenderer.sharedMesh,
                appearance);
            var animator =
                model.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "The breakthrough Pahur has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    BreakthroughControllerPath) ??
                throw new InvalidOperationException(
                    "The breakthrough controller is missing.");
            var clip =
                controller.layers[0]
                    .stateMachine
                    .defaultState
                    .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The breakthrough controller has no clip.");
            if (AssetDatabase.GetAssetPath(clip) != BreakthroughClipPath ||
                !clip.isLooping ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "The breakthrough animation contract differs.");
            }

            RequireBreakthroughSourceHash();
            if (Sha256(Absolute(BreakthroughModelPath)) !=
                SourceBreakthroughSha256)
            {
                throw new InvalidOperationException(
                    "The imported breakthrough FBX differs from the supplied source.");
            }

            RequireNoHorizontalRootTranslation(
                prefab.transform,
                prefabRenderer,
                clip);
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var facing =
                RequireBreakthroughFrontFacingPose(
                    clip,
                    prefab,
                    prefabRenderer,
                    appearance,
                    weaponMaterialIndex,
                    staticPrefab);
            var aim = facing.Aim;
            var muzzle =
                model.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(
                        item => item.name == BreakthroughMuzzleName) ??
                throw new InvalidOperationException(
                    "The breakthrough muzzle is missing.");
            if (muzzle.parent.name !=
                    prefabRenderer.bones[aim.BoneIndex].name ||
                muzzle.localPosition != aim.MuzzleLocalPosition ||
                Quaternion.Angle(
                    muzzle.localRotation,
                    aim.MuzzleLocalRotation) > 0.01f)
            {
                throw new InvalidOperationException(
                    "The breakthrough muzzle anchor differs.");
            }

            var miniMuzzle =
                RequireValidatedMiniFlameMuzzle(
                    placement.transform,
                    staticRenderer);
            RequireExactFlameEffectCopy(
                miniMuzzle,
                muzzle);
            WriteBreakthroughReport(
                clip,
                prefabRenderer.sharedMesh,
                appearance,
                model,
                staticModel,
                facing);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Breakthrough validation changed the scene.");
            }

            Debug.Log(
                "PahurBreakthroughFlamethrowerValidated Result=PASS" +
                ", Clip=" +
                clip.name +
                ", ModelScale=" +
                ScaleText(model.localScale) +
                ", ModelY=" +
                model.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaxWeaponElevationDegrees=" +
                aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaxWeaponForwardAngleDegrees=" +
                facing.MaximumWeaponForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaxHeadStaticFrontDeviationDegrees=" +
                facing.MaximumHeadDeviationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", ExistingMiniFlameEffectCopied=True" +
                ", SceneChanged=False.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Capture Breakthrough Flamethrower")]
        public static void CapturePahurBreakthroughFlamethrowerReview()
        {
            var scene =
                RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement =
                RequirePlacement(scene);
            var model =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        BreakthroughSlotName));
            var animator =
                model.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "The breakthrough Pahur has no Animator.");
            var controller =
                animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(
                    "The breakthrough controller is missing.");
            var clip =
                controller.layers[0]
                    .stateMachine
                    .defaultState
                    .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The breakthrough clip is missing.");
            Capture(
                model,
                animator,
                clip,
                BreakthroughCapturePath);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Breakthrough capture changed the scene.");
            }

            Debug.Log(
                "PahurBreakthroughFlamethrowerReviewCaptured Result=PASS" +
                ", Image=" +
                BreakthroughCapturePath +
                ", SceneChanged=False.");
        }

        private static void RequireBreakthroughSourceHash()
        {
            if (!File.Exists(SourceBreakthroughModelPath) ||
                Sha256(SourceBreakthroughModelPath) !=
                    SourceBreakthroughSha256)
            {
                throw new InvalidOperationException(
                    "The supplied breakthrough FBX is missing or changed.");
            }
        }

        private static void ImportBreakthroughModel()
        {
            var destination =
                Absolute(BreakthroughModelPath);
            if (!File.Exists(destination) ||
                Sha256(destination) != SourceBreakthroughSha256)
            {
                File.Copy(
                    SourceBreakthroughModelPath,
                    destination,
                    true);
            }

            AssetDatabase.ImportAsset(
                BreakthroughModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureBreakthroughImporter()
        {
            var importer =
                AssetImporter.GetAtPath(
                    BreakthroughModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "The breakthrough importer is missing.");
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
            var matches =
                importer.defaultClipAnimations
                    .Where(
                        item =>
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
                    "The breakthrough FBX must contain exactly one Mixamo take. Found=" +
                    matches.Length +
                    ".");
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

        private static AnimationClip RequireBreakthroughSourceClip(
            string takeName)
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(
                        BreakthroughModelPath)
                    .OfType<AnimationClip>()
                    .Where(
                        item =>
                            !item.name.StartsWith(
                                "__preview__",
                                StringComparison.Ordinal))
                    .ToArray();
            var matches =
                clips.Where(
                        item =>
                            item.name == takeName ||
                            item.name.IndexOf(
                                "mixamo",
                                StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The configured breakthrough Mixamo clip is not unique.");
            }

            return matches[0];
        }

        private static Mesh CreateBreakthroughAppearanceMesh(
            SkinnedMeshRenderer sourceRenderer)
        {
            var approved =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    ApprovedRunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The approved running appearance mesh is missing.");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running FBX is missing.");
            RequireExactMiniTransferContract(
                sourceRenderer,
                RequireRenderer(
                    runningPrefab.transform,
                    "approved running FBX"));
            var source = sourceRenderer.sharedMesh;
            var generated =
                UnityEngine.Object.Instantiate(source);
            generated.name =
                "PahurBreakthroughApprovedAppearanceMesh";
            var uv3 = new List<Vector4>();
            approved.GetUVs(
                3,
                uv3);
            if (uv3.Count != source.vertexCount)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The approved Pahur UV3 channel differs.");
            }

            generated.SetUVs(
                3,
                uv3);
            generated.subMeshCount = approved.subMeshCount;
            for (var index = 0;
                 index < approved.subMeshCount;
                 index++)
            {
                generated.SetTriangles(
                    approved.GetTriangles(index),
                    index,
                    false);
            }

            generated.bounds = source.bounds;
            if (AssetDatabase.LoadAssetAtPath<Mesh>(
                    BreakthroughAppearanceMeshPath) != null &&
                !AssetDatabase.DeleteAsset(
                    BreakthroughAppearanceMeshPath))
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The previous breakthrough appearance mesh could not be removed.");
            }

            AssetDatabase.CreateAsset(
                generated,
                BreakthroughAppearanceMeshPath);
            AssetDatabase.SaveAssets();
            RequireMiniAppearancePreserved(
                source,
                generated);
            return generated;
        }

        private static AnimationClip CreateBreakthroughInPlaceClip(
            AnimationClip source,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    BreakthroughClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(
                    clip,
                    BreakthroughClipPath);
            }

            EditorUtility.CopySerialized(
                source,
                clip);
            clip.name =
                "Pahur_05_BreakthroughFlamethrower_InPlace";
            clip.wrapMode = WrapMode.Loop;
            var rootPath =
                AnimationUtility.CalculateTransformPath(
                    renderer.rootBone,
                    root);
            var horizontalProperties =
                HorizontalLocalPositionProperties(
                    root,
                    renderer.rootBone.parent);
            var bindings =
                AnimationUtility.GetCurveBindings(clip)
                    .Where(
                        binding =>
                            (binding.path.Length == 0 &&
                             (binding.propertyName == "RootT.x" ||
                              binding.propertyName == "RootT.z" ||
                              binding.propertyName == "MotionT.x" ||
                              binding.propertyName == "MotionT.z")) ||
                            (binding.path == rootPath &&
                             horizontalProperties.Contains(
                                 binding.propertyName)))
                    .ToArray();
            if (bindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "The breakthrough Mixamo clip has no horizontal root curves to lock.");
            }

            foreach (var binding in bindings)
            {
                var curve =
                    AnimationUtility.GetEditorCurve(
                        clip,
                        binding) ??
                    throw new InvalidOperationException(
                        "A breakthrough horizontal root curve is missing.");
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.Constant(
                        0f,
                        clip.length,
                        curve.Evaluate(0f)));
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController CreateBreakthroughController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    BreakthroughControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(
                        BreakthroughControllerPath);
            }

            var machine =
                controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            var state = machine.AddState(BreakthroughStateName);
            state.motion = clip;
            state.speed = 1f;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static BreakthroughFacingMetrics
            AuthorBreakthroughFrontFacingPose(
            AnimationClip clip,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            Mesh appearance,
            int weaponSubMesh,
            GameObject staticPrefab)
        {
            var weaponIndices =
                RequireWeaponBarrelIndices(
                    prefabRenderer,
                    appearance,
                    weaponSubMesh);
            var weaponBoneIndex =
                RequireRightWeaponBoneIndex(
                    prefabRenderer,
                    appearance,
                    weaponIndices);
            var clone =
                UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            var baked = new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "breakthrough front-facing authoring");
                renderer.sharedMesh = appearance;
                var weaponBone = renderer.bones[weaponBoneIndex];
                var staticRenderer =
                    RequireRenderer(
                        staticPrefab.transform,
                        "approved static Pahur FBX");
                var head =
                    RequireMiniFlameBone(renderer, "Head");
                var staticHead =
                    RequireMiniFlameBone(staticRenderer, "Head");
                RequireMatchingBreakthroughBoneParents(
                    head,
                    staticHead);

                var weaponPath =
                    AnimationUtility.CalculateTransformPath(
                        weaponBone,
                        clone.transform);
                RemoveBreakthroughRotationCurves(
                    clip,
                    weaponPath);
                var frameCount =
                    Mathf.Clamp(
                        Mathf.CeilToInt(
                            clip.length *
                            Mathf.Max(1f, clip.frameRate) *
                            2f) +
                        1,
                        2,
                        481);
                var xKeys = new Keyframe[frameCount];
                var yKeys = new Keyframe[frameCount];
                var zKeys = new Keyframe[frameCount];
                var wKeys = new Keyframe[frameCount];
                var previous = Quaternion.identity;
                for (var index = 0;
                     index < frameCount;
                     index++)
                {
                    var time =
                        clip.length *
                        index /
                        (frameCount - 1f);
                    clip.SampleAnimation(
                        clone,
                        time);
                    for (var iteration = 0;
                         iteration < 6;
                         iteration++)
                    {
                        renderer.BakeMesh(baked);
                        var frame =
                            AnalyzeWeapon(
                                clone.transform,
                                renderer,
                                baked,
                                weaponIndices,
                                weaponBone.position);
                        var correction =
                            Quaternion.FromToRotation(
                                clone.transform.TransformDirection(
                                    frame.Direction),
                                clone.transform.forward);
                        weaponBone.rotation =
                            correction * weaponBone.rotation;
                    }

                    var rotation = weaponBone.localRotation;
                    if (index > 0 &&
                        Quaternion.Dot(previous, rotation) < 0f)
                    {
                        rotation =
                            new Quaternion(
                                -rotation.x,
                                -rotation.y,
                                -rotation.z,
                                -rotation.w);
                    }

                    previous = rotation;
                    xKeys[index] = new Keyframe(time, rotation.x);
                    yKeys[index] = new Keyframe(time, rotation.y);
                    zKeys[index] = new Keyframe(time, rotation.z);
                    wKeys[index] = new Keyframe(time, rotation.w);
                }

                SetQuaternionCurve(
                    clip,
                    weaponPath,
                    "x",
                    xKeys);
                SetQuaternionCurve(
                    clip,
                    weaponPath,
                    "y",
                    yKeys);
                SetQuaternionCurve(
                    clip,
                    weaponPath,
                    "z",
                    zKeys);
                SetQuaternionCurve(
                    clip,
                    weaponPath,
                    "w",
                    wKeys);
                SetBreakthroughStaticBoneRotation(
                    clip,
                    clone.transform,
                    head,
                    staticHead.localRotation);
                clip.EnsureQuaternionContinuity();
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            return RequireBreakthroughFrontFacingPose(
                clip,
                prefab,
                prefabRenderer,
                appearance,
                weaponSubMesh,
                staticPrefab);
        }

        private static BreakthroughFacingMetrics
            RequireBreakthroughFrontFacingPose(
            AnimationClip clip,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            Mesh appearance,
            int weaponSubMesh,
            GameObject staticPrefab)
        {
            var weaponIndices =
                RequireWeaponBarrelIndices(
                    prefabRenderer,
                    appearance,
                    weaponSubMesh);
            var weaponBoneIndex =
                RequireRightWeaponBoneIndex(
                    prefabRenderer,
                    appearance,
                    weaponIndices);
            var clone =
                UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            var baked = new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "breakthrough front-facing validation");
                renderer.sharedMesh = appearance;
                var weaponBone = renderer.bones[weaponBoneIndex];
                var staticRenderer =
                    RequireRenderer(
                        staticPrefab.transform,
                        "approved static Pahur FBX");
                var head =
                    RequireMiniFlameBone(renderer, "Head");
                var staticHead =
                    RequireMiniFlameBone(staticRenderer, "Head");
                RequireMatchingBreakthroughBoneParents(
                    head,
                    staticHead);

                var maximumWeaponForwardAngle = 0f;
                var maximumHeadDeviation = 0f;
                for (var index = 0;
                     index <= 16;
                     index++)
                {
                    clip.SampleAnimation(
                        clone,
                        clip.length * index / 16f);
                    renderer.BakeMesh(baked);
                    var frame =
                        AnalyzeWeapon(
                            clone.transform,
                            renderer,
                            baked,
                            weaponIndices,
                            weaponBone.position);
                    maximumWeaponForwardAngle =
                        Mathf.Max(
                            maximumWeaponForwardAngle,
                            Vector3.Angle(
                                frame.Direction,
                                Vector3.forward));
                    maximumHeadDeviation =
                        Mathf.Max(
                            maximumHeadDeviation,
                            Quaternion.Angle(
                                head.localRotation,
                                staticHead.localRotation));
                }

                if (maximumWeaponForwardAngle > 0.35f ||
                    maximumHeadDeviation > 0.01f)
                {
                    throw new InvalidOperationException(
                        "The breakthrough gun or head does not face the approved front. WeaponAngle=" +
                        maximumWeaponForwardAngle.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        ", HeadDeviation=" +
                        maximumHeadDeviation.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        ".");
                }

                var aim =
                    RequireHorizontalMiniFlameAim(
                        clip,
                        prefab,
                        prefabRenderer,
                        appearance,
                        weaponSubMesh);
                return new BreakthroughFacingMetrics(
                    aim,
                    maximumWeaponForwardAngle,
                    maximumHeadDeviation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static void SetBreakthroughStaticBoneRotation(
            AnimationClip clip,
            Transform root,
            Transform bone,
            Quaternion rotation)
        {
            var path =
                AnimationUtility.CalculateTransformPath(
                    bone,
                    root);
            RemoveBreakthroughRotationCurves(
                clip,
                path);
            var xKeys =
                new[]
                {
                    new Keyframe(0f, rotation.x),
                    new Keyframe(clip.length, rotation.x)
                };
            var yKeys =
                new[]
                {
                    new Keyframe(0f, rotation.y),
                    new Keyframe(clip.length, rotation.y)
                };
            var zKeys =
                new[]
                {
                    new Keyframe(0f, rotation.z),
                    new Keyframe(clip.length, rotation.z)
                };
            var wKeys =
                new[]
                {
                    new Keyframe(0f, rotation.w),
                    new Keyframe(clip.length, rotation.w)
                };
            SetQuaternionCurve(clip, path, "x", xKeys);
            SetQuaternionCurve(clip, path, "y", yKeys);
            SetQuaternionCurve(clip, path, "z", zKeys);
            SetQuaternionCurve(clip, path, "w", wKeys);
        }

        private static void RemoveBreakthroughRotationCurves(
            AnimationClip clip,
            string path)
        {
            foreach (var binding in
                     AnimationUtility.GetCurveBindings(clip)
                         .Where(
                             item =>
                                 item.path == path &&
                                 (item.propertyName.StartsWith(
                                      "m_LocalRotation.",
                                      StringComparison.Ordinal) ||
                                  item.propertyName.IndexOf(
                                      "localEuler",
                                      StringComparison.OrdinalIgnoreCase) >=
                                  0))
                         .ToArray())
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    null);
            }
        }

        private static void RequireMatchingBreakthroughBoneParents(
            Transform actual,
            Transform approved)
        {
            if (actual.parent == null ||
                approved.parent == null ||
                actual.parent.name != approved.parent.name)
            {
                throw new InvalidOperationException(
                    actual.name +
                    " does not share the approved static Pahur bone parent.");
            }
        }

        private static Transform RequireValidatedMiniFlameMuzzle(
            Transform placement,
            SkinnedMeshRenderer staticRenderer)
        {
            var miniModel =
                RequireModel(
                    RequireChild(
                        placement,
                        MiniFlameSlotName));
            var miniMuzzle =
                miniModel.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(
                        item => item.name == MiniFlameMuzzleName) ??
                throw new InvalidOperationException(
                    "The approved mini flamethrower muzzle is missing.");
            var miniPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    MiniFlameModelPath) ??
                throw new InvalidOperationException(
                    "The approved mini flame FBX is missing.");
            var miniRenderer =
                RequireRenderer(
                    miniPrefab.transform,
                    "mini flame FBX");
            var miniAppearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    MiniFlameAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The approved mini flame appearance mesh is missing.");
            var miniController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    MiniFlameControllerPath) ??
                throw new InvalidOperationException(
                    "The approved mini flame controller is missing.");
            var miniClip =
                miniController.layers[0]
                    .stateMachine
                    .defaultState
                    .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The approved mini flame clip is missing.");
            var miniAim =
                RequireHorizontalMiniFlameAim(
                    miniClip,
                    miniPrefab,
                    miniRenderer,
                    miniAppearance,
                    RequireWeaponMaterialIndex(
                        staticRenderer.sharedMaterials));
            RequireMiniFlameParticles(
                miniMuzzle,
                miniAim.WeaponLength,
                miniAim.WeaponRadius);
            return miniMuzzle;
        }

        private static void RequireExactFlameEffectCopy(
            Transform source,
            Transform copy)
        {
            var sourceChildren =
                source.Cast<Transform>()
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray();
            var copyChildren =
                copy.Cast<Transform>()
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray();
            if (sourceChildren.Length != copyChildren.Length ||
                sourceChildren.Length != 2)
            {
                throw new InvalidOperationException(
                    "The copied breakthrough flame hierarchy differs from the approved mini flame effect.");
            }

            for (var index = 0;
                 index < sourceChildren.Length;
                 index++)
            {
                var expected = sourceChildren[index];
                var actual = copyChildren[index];
                var expectedParticle =
                    expected.GetComponent<ParticleSystem>();
                var actualParticle =
                    actual.GetComponent<ParticleSystem>();
                var expectedRenderer =
                    expected.GetComponent<ParticleSystemRenderer>();
                var actualRenderer =
                    actual.GetComponent<ParticleSystemRenderer>();
                if (expected.name != actual.name ||
                    expected.localPosition != actual.localPosition ||
                    expected.localRotation != actual.localRotation ||
                    expected.localScale != actual.localScale ||
                    expectedParticle == null ||
                    actualParticle == null ||
                    expectedRenderer == null ||
                    actualRenderer == null ||
                    EditorJsonUtility.ToJson(expectedParticle) !=
                    EditorJsonUtility.ToJson(actualParticle) ||
                    expectedRenderer.sharedMaterial !=
                    actualRenderer.sharedMaterial ||
                    expectedRenderer.renderMode !=
                    actualRenderer.renderMode ||
                    expectedRenderer.alignment !=
                    actualRenderer.alignment ||
                    expectedRenderer.sortMode !=
                    actualRenderer.sortMode)
                {
                    throw new InvalidOperationException(
                        "The copied breakthrough flame effect differs from the approved mini flame effect.");
                }
            }
        }

        private static void WriteBreakthroughReport(
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Transform model,
            Transform staticModel,
            BreakthroughFacingMetrics facing)
        {
            var aim = facing.Aim;
            var destination =
                Absolute(BreakthroughReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid breakthrough report path."));
            var report = new StringBuilder();
            report.AppendLine("Pahur Breakthrough Flamethrower Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceSha256=" + SourceBreakthroughSha256);
            report.AppendLine("ImportedSourceHashMatches=True");
            report.AppendLine("SourceClip=mixamo.com");
            report.AppendLine("PlaybackClip=" + clip.name);
            report.AppendLine("Loop=True");
            report.AppendLine("Vertices=" + source.vertexCount);
            report.AppendLine("ShapeSkinBindPosesPreserved=True");
            report.AppendLine("ApprovedAppearanceTransferredByExactVertexIndex=True");
            report.AppendLine("ApprovedMaterialSlots=" + appearance.subMeshCount);
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
            report.AppendLine("WeaponBoneIndex=" + aim.BoneIndex);
            report.AppendLine(
                "MaximumWeaponElevationDegrees=" +
                aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumWeaponForwardAngleDegrees=" +
                facing.MaximumWeaponForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumHeadStaticFrontDeviationDegrees=" +
                facing.MaximumHeadDeviationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MuzzleLocalPosition=" +
                ScaleText(aim.MuzzleLocalPosition));
            report.AppendLine("ExistingMiniFlameEffectCopiedExactly=True");
            report.AppendLine("ParticleSystems=2");
            report.AppendLine("MiniFlameOverallHorizontalWidthMultiplier=2");
            report.AppendLine("MiniFlameTailHorizontalWidthMultiplier=5");
            report.AppendLine("SceneChangedByValidation=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private readonly struct BreakthroughFacingMetrics
        {
            public BreakthroughFacingMetrics(
                MiniFlameAim aim,
                float maximumWeaponForwardAngleDegrees,
                float maximumHeadDeviationDegrees)
            {
                Aim = aim;
                MaximumWeaponForwardAngleDegrees =
                    maximumWeaponForwardAngleDegrees;
                MaximumHeadDeviationDegrees =
                    maximumHeadDeviationDegrees;
            }

            public MiniFlameAim Aim { get; }
            public float MaximumWeaponForwardAngleDegrees { get; }
            public float MaximumHeadDeviationDegrees { get; }
        }
    }
}
