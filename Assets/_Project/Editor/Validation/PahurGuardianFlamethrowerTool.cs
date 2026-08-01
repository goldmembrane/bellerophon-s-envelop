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
        private const string GuardianSlotName =
            "Pahur_06_GuardianFlamethrower";
        private const string SourceGuardianModelPath =
            @"D:\Bellerophon2\Bellerophon\enemies model\pāḫḫur guardian pose.fbx";
        private const string SourceGuardianSha256 =
            "143C3FB64A7A42CC64A9EA18AA43B4282A21FD96DA859E7E439AA007C7E8B49D";
        private const string GuardianModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurGuardianPose.fbx";
        private const string GuardianAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurGuardianApprovedAppearanceMesh.asset";
        private const string GuardianClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_06_GuardianFlamethrower_InPlace.anim";
        private const string GuardianControllerPath =
            "Assets/_Project/Art/Enemies/Pahur/Controllers/Pahur_06_GuardianFlamethrower.controller";
        private const string GuardianStateName =
            "PahurGuardianFlamethrower";
        private const string GuardianMuzzleName =
            "Pahur_GuardianFlamethrower_Muzzle";
        private const string GuardianReportPath =
            "docs/validation/pahur_guardian_flamethrower_2026-07-31/Pahur_06_GuardianFlamethrower_Validation.txt";
        private const float GuardianFlameLengthMultiplier = 2f;
        private const float GuardianTailRiseStartLengthRatio = 0.75f;
        private const float GuardianTailRiseHeightRatio = 1.5f;
        private const float GuardianTailRiseAngleDegrees = 90f;
        private const float GuardianTailRiseToleranceDegrees = 2f;
        private const float GuardianTailDensityMultiplier = 5f;
        private const float GuardianTailDensityMaskTransition = 0.01f;
        private const string GuardianTailDensitySuffix =
            "_GuardianTailDensity";

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Inspect Guardian Source")]
        public static void InspectPahurGuardianSource()
        {
            RequireGuardianSourceHash();
            ImportGuardianModel();
            var takeName = ConfigureGuardianImporter();
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    GuardianModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur guardian FBX is missing.");
            var renderer =
                RequireRenderer(
                    prefab.transform,
                    "guardian FBX");
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RunningModelPath) ??
                throw new InvalidOperationException(
                    "The approved running FBX is missing.");
            RequireExactMiniTransferContract(
                renderer,
                RequireRenderer(
                    runningPrefab.transform,
                    "approved running FBX"));
            var clip =
                RequireGuardianSourceClip(takeName);
            var scene = RequireScene(false);
            var placement = RequirePlacement(scene);
            var staticRenderer =
                RequireRenderer(
                    RequireModel(
                        RequireChild(
                            placement.transform,
                            StaticSlotName)),
                    StaticSlotName);
            var approved =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    ApprovedRunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The approved running appearance mesh is missing.");
            var weaponVertices =
                RequireWeaponBarrelIndices(
                    renderer,
                    approved,
                    RequireWeaponMaterialIndex(
                        staticRenderer.sharedMaterials));
            var weaponBoneIndex =
                RequireRightWeaponBoneIndex(
                    renderer,
                    approved,
                    weaponVertices);
            Debug.Log(
                "PahurGuardianSourceInspection Result=PASS" +
                ", Sha256=" + SourceGuardianSha256 +
                ", Clip=" + clip.name +
                ", Vertices=" + renderer.sharedMesh.vertexCount +
                ", Bones=" + renderer.bones.Length +
                ", ExactAppearanceTransferContract=True" +
                ", WeaponBone=" +
                renderer.bones[weaponBoneIndex].name +
                ", WeaponVertices=" +
                weaponVertices.Length +
                ".");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Apply Guardian Flamethrower")]
        public static void ApplyPahurGuardianFlamethrower()
        {
            var scene = RequireScene(false);
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        StaticSlotName));
            var staticRenderer =
                RequireRenderer(
                    staticModel,
                    StaticSlotName);
            RequireApprovedMaterials(staticRenderer);
            var slot =
                RequireChild(
                    placement.transform,
                    GuardianSlotName);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_06_GuardianFlamethrower must contain exactly one current model.");
            }

            var otherSlots =
                OtherSlotSignatures(
                    placement.transform,
                    GuardianSlotName);
            var protectedRoots =
                ProtectedRootSignatures(
                    scene,
                    placement.transform);
            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;

            RequireGuardianSourceHash();
            ImportGuardianModel();
            var takeName = ConfigureGuardianImporter();
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    GuardianModelPath) ??
                throw new InvalidOperationException(
                    "The guardian FBX is missing.");
            var prefabRenderer =
                RequireRenderer(
                    prefab.transform,
                    "guardian FBX");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var sourceClip =
                RequireGuardianSourceClip(takeName);
            var appearance =
                CreateGuardianAppearanceMesh(prefabRenderer);
            var clip =
                CreateGuardianInPlaceClip(
                    sourceClip,
                    prefab.transform,
                    prefabRenderer);
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var weapon =
                AuthorGuardianFrontFacingWeapon(
                    clip,
                    prefab,
                    prefabRenderer,
                    appearance,
                    weaponMaterialIndex);
            RequireNoHorizontalRootTranslation(
                prefab.transform,
                prefabRenderer,
                clip);
            var controller =
                CreateGuardianController(clip);
            var matchedScale =
                MatchedRunningScale(
                    staticPrefab,
                    prefab,
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
                    prefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "The guardian prefab could not be instantiated.");
            replacement.name = ModelName;
            replacement.transform.SetParent(slot, false);
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
                        GuardianSlotName);
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
                    prefab.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "The guardian FBX has no Animator.");
                animator.avatar = sourceAnimator.avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);

                var targetBone =
                    renderer.bones[weapon.Aim.BoneIndex];
                var muzzle =
                    UnityEngine.Object.Instantiate(
                        miniMuzzle.gameObject,
                        targetBone,
                        false);
                muzzle.name = GuardianMuzzleName;
                muzzle.transform.localPosition =
                    weapon.Aim.MuzzleLocalPosition;
                muzzle.transform.localRotation =
                    weapon.Aim.MuzzleLocalRotation;
                muzzle.transform.localScale = Vector3.one;
                RequireExactFlameEffectCopy(
                    miniMuzzle,
                    muzzle.transform);
                ApplyGuardianTailRise(muzzle.transform);
                RequireGuardianTailRise(
                    miniMuzzle,
                    muzzle.transform);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            RequireUnchanged(
                otherSlots,
                OtherSlotSignatures(
                    placement.transform,
                    GuardianSlotName),
                "A Pahur slot outside Pahur_06_GuardianFlamethrower changed.");
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
                    "The guardian slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurGuardianFlamethrowerApplied Result=PASS" +
                ", SourceClip=" + sourceClip.name +
                ", Loop=True" +
                ", HorizontalRootMotion=False" +
                ", ExactApprovedAppearanceChannels=True" +
                ", HorizontalWeapon=True" +
                ", ForwardWeapon=True" +
                ", ExistingMiniFlameEffectCopied=True" +
                ", FlameTailRiseDegrees=" +
                GuardianTailRiseAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", OtherSlotsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Validate Guardian Flamethrower")]
        public static void ValidatePahurGuardianFlamethrower()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
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
                        GuardianSlotName));
            var renderer =
                RequireRenderer(
                    model,
                    GuardianSlotName);
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    GuardianModelPath) ??
                throw new InvalidOperationException(
                    "The guardian FBX is missing.");
            var prefabRenderer =
                RequireRenderer(
                    prefab.transform,
                    "guardian FBX");
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    GuardianAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The guardian appearance mesh is missing.");
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
                    "The guardian Pahur appearance, size, or Y position differs.");
            }

            RequireMiniAppearancePreserved(
                prefabRenderer.sharedMesh,
                appearance);
            var animator =
                model.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "The guardian Pahur has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    GuardianControllerPath) ??
                throw new InvalidOperationException(
                    "The guardian controller is missing.");
            var clip =
                controller.layers[0]
                    .stateMachine
                    .defaultState
                    .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The guardian controller has no clip.");
            if (AssetDatabase.GetAssetPath(clip) != GuardianClipPath ||
                !clip.isLooping ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "The guardian animation contract differs.");
            }

            RequireGuardianSourceHash();
            if (Sha256(Absolute(GuardianModelPath)) != SourceGuardianSha256)
            {
                throw new InvalidOperationException(
                    "The imported guardian FBX differs from the supplied source.");
            }

            RequireNoHorizontalRootTranslation(
                prefab.transform,
                prefabRenderer,
                clip);
            var weapon =
                RequireGuardianFrontFacingWeapon(
                    clip,
                    prefab,
                    prefabRenderer,
                    appearance,
                    RequireWeaponMaterialIndex(
                        staticRenderer.sharedMaterials));
            var muzzle =
                model.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(
                        item => item.name == GuardianMuzzleName) ??
                throw new InvalidOperationException(
                    "The guardian muzzle is missing.");
            if (muzzle.parent.name !=
                    prefabRenderer.bones[weapon.Aim.BoneIndex].name ||
                muzzle.localPosition != weapon.Aim.MuzzleLocalPosition ||
                Quaternion.Angle(
                    muzzle.localRotation,
                    weapon.Aim.MuzzleLocalRotation) > 0.01f)
            {
                throw new InvalidOperationException(
                    "The guardian muzzle anchor differs.");
            }

            var miniMuzzle =
                RequireValidatedMiniFlameMuzzle(
                    placement.transform,
                    staticRenderer);
            var tail =
                RequireGuardianTailRise(
                    miniMuzzle,
                    muzzle);
            WriteGuardianReport(
                clip,
                prefabRenderer.sharedMesh,
                appearance,
                model,
                staticModel,
                weapon,
                tail);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Guardian validation changed the scene.");
            }

            Debug.Log(
                "PahurGuardianFlamethrowerValidated Result=PASS" +
                ", Clip=" + clip.name +
                ", ModelScale=" + ScaleText(model.localScale) +
                ", ModelY=" +
                model.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaxWeaponElevationDegrees=" +
                weapon.Aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaxWeaponForwardAngleDegrees=" +
                weapon.MaximumForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", FlameTailRiseMinimumDegrees=" +
                tail.MinimumTailRiseDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", FlameTailRiseMaximumDegrees=" +
                tail.MaximumTailRiseDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", SceneChanged=False.");
        }

        private static void RequireGuardianSourceHash()
        {
            if (!File.Exists(SourceGuardianModelPath) ||
                Sha256(SourceGuardianModelPath) != SourceGuardianSha256)
            {
                throw new InvalidOperationException(
                    "The supplied guardian FBX is missing or changed.");
            }
        }

        private static void ImportGuardianModel()
        {
            var destination = Absolute(GuardianModelPath);
            if (!File.Exists(destination) ||
                Sha256(destination) != SourceGuardianSha256)
            {
                File.Copy(
                    SourceGuardianModelPath,
                    destination,
                    true);
            }

            AssetDatabase.ImportAsset(
                GuardianModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureGuardianImporter()
        {
            var importer =
                AssetImporter.GetAtPath(
                    GuardianModelPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "The guardian importer is missing.");
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
                    "The guardian FBX must contain exactly one Mixamo take. Found=" +
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

        private static AnimationClip RequireGuardianSourceClip(
            string takeName)
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(GuardianModelPath)
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
                    "The configured guardian Mixamo clip is not unique.");
            }

            return matches[0];
        }

        private static Mesh CreateGuardianAppearanceMesh(
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
            var generated = UnityEngine.Object.Instantiate(source);
            generated.name = "PahurGuardianApprovedAppearanceMesh";
            var uv3 = new List<Vector4>();
            approved.GetUVs(3, uv3);
            if (uv3.Count != source.vertexCount)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The approved Pahur UV3 channel differs.");
            }

            generated.SetUVs(3, uv3);
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
                    GuardianAppearanceMeshPath) != null &&
                !AssetDatabase.DeleteAsset(GuardianAppearanceMeshPath))
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The previous guardian appearance mesh could not be removed.");
            }

            AssetDatabase.CreateAsset(
                generated,
                GuardianAppearanceMeshPath);
            AssetDatabase.SaveAssets();
            RequireMiniAppearancePreserved(source, generated);
            return generated;
        }

        private static AnimationClip CreateGuardianInPlaceClip(
            AnimationClip source,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    GuardianClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, GuardianClipPath);
            }

            EditorUtility.CopySerialized(source, clip);
            clip.name = "Pahur_06_GuardianFlamethrower_InPlace";
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
                    "The guardian Mixamo clip has no horizontal root curves to lock.");
            }

            foreach (var binding in bindings)
            {
                var curve =
                    AnimationUtility.GetEditorCurve(clip, binding) ??
                    throw new InvalidOperationException(
                        "A guardian horizontal root curve is missing.");
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
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static GuardianWeaponMetrics
            AuthorGuardianFrontFacingWeapon(
            AnimationClip clip,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            Mesh appearance,
            int weaponSubMesh)
        {
            AuthorHorizontalMiniFlameAim(
                clip,
                prefab,
                prefabRenderer,
                appearance,
                weaponSubMesh);
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
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            var baked = new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "guardian front-facing authoring");
                renderer.sharedMesh = appearance;
                var weaponBone = renderer.bones[weaponBoneIndex];
                var path =
                    AnimationUtility.CalculateTransformPath(
                        weaponBone,
                        clone.transform);
                RemoveBreakthroughRotationCurves(clip, path);
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
                        clip.length * index /
                        (frameCount - 1f);
                    clip.SampleAnimation(clone, time);
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

                SetQuaternionCurve(clip, path, "x", xKeys);
                SetQuaternionCurve(clip, path, "y", yKeys);
                SetQuaternionCurve(clip, path, "z", zKeys);
                SetQuaternionCurve(clip, path, "w", wKeys);
                clip.EnsureQuaternionContinuity();
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            return RequireGuardianFrontFacingWeapon(
                clip,
                prefab,
                prefabRenderer,
                appearance,
                weaponSubMesh);
        }

        private static GuardianWeaponMetrics
            RequireGuardianFrontFacingWeapon(
            AnimationClip clip,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            Mesh appearance,
            int weaponSubMesh)
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
            var clone = UnityEngine.Object.Instantiate(prefab);
            clone.hideFlags = HideFlags.HideAndDontSave;
            var baked = new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "guardian front-facing validation");
                renderer.sharedMesh = appearance;
                var weaponBone = renderer.bones[weaponBoneIndex];
                var maximumAngle = 0f;
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
                    maximumAngle =
                        Mathf.Max(
                            maximumAngle,
                            Vector3.Angle(
                                frame.Direction,
                                Vector3.forward));
                }

                if (maximumAngle > 0.35f)
                {
                    throw new InvalidOperationException(
                        "The guardian gun does not face forward. Angle=" +
                        maximumAngle.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        ".");
                }

                return new GuardianWeaponMetrics(
                    RequireHorizontalMiniFlameAim(
                        clip,
                        prefab,
                        prefabRenderer,
                        appearance,
                        weaponSubMesh),
                    maximumAngle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static AnimatorController CreateGuardianController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    GuardianControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(
                        GuardianControllerPath);
            }

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            var state = machine.AddState(GuardianStateName);
            state.motion = clip;
            state.speed = 1f;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ApplyGuardianTailRise(
            Transform muzzle)
        {
            var originalParticles =
                muzzle.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particle in originalParticles)
            {
                var velocity = particle.velocityOverLifetime;
                var original = velocity.y;
                if (original.mode !=
                        ParticleSystemCurveMode.TwoConstants ||
                    velocity.x.mode !=
                        ParticleSystemCurveMode.TwoConstants ||
                    velocity.z.mode !=
                        ParticleSystemCurveMode.TwoConstants ||
                    Mathf.Abs(velocity.x.constantMin) > 0.000001f ||
                    Mathf.Abs(velocity.x.constantMax) > 0.000001f ||
                    Mathf.Abs(velocity.z.constantMin) > 0.000001f ||
                    Mathf.Abs(velocity.z.constantMax) > 0.000001f)
                {
                    throw new InvalidOperationException(
                        "The approved mini flame velocity mode or X/Z zero contract differs.");
                }

                var main = particle.main;
                var originalStartSpeed = main.startSpeed;
                if (originalStartSpeed.mode !=
                    ParticleSystemCurveMode.TwoConstants)
                {
                    throw new InvalidOperationException(
                        "The approved mini flame start speed mode differs.");
                }

                main.startSpeed =
                    new ParticleSystem.MinMaxCurve(
                        originalStartSpeed.constantMin *
                        GuardianFlameLengthMultiplier,
                        originalStartSpeed.constantMax *
                        GuardianFlameLengthMultiplier);
                var forwardSpeed =
                    (main.startSpeed.constantMin +
                     main.startSpeed.constantMax) *
                    0.5f;
                var riseSpeed =
                    forwardSpeed *
                    GuardianTailRiseHeightRatio *
                    2f /
                    (1f - GuardianTailRiseStartLengthRatio);
                var minimum = original.constantMin;
                var maximum = original.constantMax;
                var zeroMinimum =
                    AnimationCurve.Constant(0f, 1f, 0f);
                var zeroMaximum =
                    AnimationCurve.Constant(0f, 1f, 0f);
                velocity.x =
                    new ParticleSystem.MinMaxCurve(
                        1f,
                        zeroMinimum,
                        zeroMaximum);
                velocity.y =
                    new ParticleSystem.MinMaxCurve(
                        1f,
                        new AnimationCurve(
                            new Keyframe(0f, minimum),
                            new Keyframe(
                                GuardianTailRiseStartLengthRatio,
                                minimum),
                            new Keyframe(
                                1f,
                                riseSpeed + minimum)),
                        new AnimationCurve(
                            new Keyframe(0f, maximum),
                            new Keyframe(
                                GuardianTailRiseStartLengthRatio,
                                maximum),
                            new Keyframe(
                                1f,
                                riseSpeed + maximum)));
                velocity.z =
                    new ParticleSystem.MinMaxCurve(
                        1f,
                        new AnimationCurve(
                            new Keyframe(0f, 0f),
                            new Keyframe(
                                GuardianTailRiseStartLengthRatio,
                                0f),
                            new Keyframe(1f, -forwardSpeed)),
                        new AnimationCurve(
                            new Keyframe(0f, 0f),
                            new Keyframe(
                                GuardianTailRiseStartLengthRatio,
                                0f),
                            new Keyframe(1f, -forwardSpeed)));
                EditorUtility.SetDirty(particle);
            }

            ApplyGuardianTailDensity(originalParticles);
        }

        private static void ApplyGuardianTailDensity(
            IEnumerable<ParticleSystem> originalParticles)
        {
            foreach (var source in originalParticles)
            {
                var sourceEmission = source.emission;
                var sourceRate = sourceEmission.rateOverTime;
                var sourceColor = source.colorOverLifetime;
                var sourceGradient = sourceColor.color;
                if (sourceRate.mode != ParticleSystemCurveMode.Constant ||
                    !sourceColor.enabled ||
                    sourceGradient.mode !=
                    ParticleSystemGradientMode.Gradient)
                {
                    throw new InvalidOperationException(
                        "The approved flame emission or color mode differs.");
                }

                var duplicate =
                    UnityEngine.Object.Instantiate(
                        source.gameObject,
                        source.transform.parent,
                        false);
                duplicate.name = source.name + GuardianTailDensitySuffix;
                duplicate.transform.localPosition =
                    source.transform.localPosition;
                duplicate.transform.localRotation =
                    source.transform.localRotation;
                duplicate.transform.localScale =
                    source.transform.localScale;
                var particles = duplicate.GetComponent<ParticleSystem>() ??
                    throw new InvalidOperationException(
                        "The guardian tail density particle copy is missing.");
                var main = particles.main;
                main.maxParticles =
                    Mathf.CeilToInt(
                        source.main.maxParticles *
                        (GuardianTailDensityMultiplier - 1f));
                var emission = particles.emission;
                emission.rateOverTime =
                    new ParticleSystem.MinMaxCurve(
                        sourceRate.constant *
                        (GuardianTailDensityMultiplier - 1f));

                var baseline = sourceGradient.gradient;
                var maskEnd =
                    GuardianTailRiseStartLengthRatio +
                    GuardianTailDensityMaskTransition;
                var masked = new Gradient
                {
                    mode = baseline.mode
                };
                var alphaKeys =
                    new List<GradientAlphaKey>
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(
                            0f,
                            GuardianTailRiseStartLengthRatio),
                        new GradientAlphaKey(
                            baseline.Evaluate(maskEnd).a,
                            maskEnd)
                    };
                alphaKeys.AddRange(
                    baseline.alphaKeys.Where(item => item.time > maskEnd));
                masked.SetKeys(
                    baseline.colorKeys,
                    alphaKeys.ToArray());
                var color = particles.colorOverLifetime;
                color.color =
                    new ParticleSystem.MinMaxGradient(masked);
                particles.Play(true);
                EditorUtility.SetDirty(particles);
            }
        }

        private static GuardianTailMetrics RequireGuardianTailRise(
            Transform source,
            Transform guardian)
        {
            var expected =
                UnityEngine.Object.Instantiate(source.gameObject);
            expected.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                ApplyGuardianTailRise(expected.transform);
                var sourceParticles =
                    source.GetComponentsInChildren<ParticleSystem>(true)
                        .OrderBy(item => item.name, StringComparer.Ordinal)
                        .ToArray();
                var guardianParticles =
                    guardian.GetComponentsInChildren<ParticleSystem>(true)
                        .Where(
                            item =>
                                !item.name.EndsWith(
                                    GuardianTailDensitySuffix,
                                    StringComparison.Ordinal))
                        .OrderBy(item => item.name, StringComparer.Ordinal)
                        .ToArray();
                var densityParticles =
                    guardian.GetComponentsInChildren<ParticleSystem>(true)
                        .Where(
                            item =>
                                item.name.EndsWith(
                                    GuardianTailDensitySuffix,
                                    StringComparison.Ordinal))
                        .ToDictionary(
                            item => item.name,
                            StringComparer.Ordinal);
                if (sourceParticles.Length != 2 ||
                    guardianParticles.Length != sourceParticles.Length ||
                    densityParticles.Count != sourceParticles.Length)
                {
                    throw new InvalidOperationException(
                        "The guardian flame particle count differs.");
                }

                var minimumAngle = float.PositiveInfinity;
                var maximumAngle = float.NegativeInfinity;
                var minimumRiseHeightRatio = float.PositiveInfinity;
                var maximumRiseHeightRatio = float.NegativeInfinity;
                var minimumDensityMultiplier = float.PositiveInfinity;
                var maximumDensityMultiplier = float.NegativeInfinity;
                for (var index = 0;
                     index < sourceParticles.Length;
                     index++)
                {
                    var sourceParticle = sourceParticles[index];
                    var actual = guardianParticles[index];
                    if (!densityParticles.TryGetValue(
                            actual.name + GuardianTailDensitySuffix,
                            out var densityParticle))
                    {
                        throw new InvalidOperationException(
                            "The guardian tail density layer is missing for " +
                            actual.name +
                            ".");
                    }

                    var densityMultiplier =
                        RequireGuardianTailDensity(
                            actual,
                            densityParticle);
                    var sourceVelocity =
                        sourceParticle.velocityOverLifetime;
                    var actualVelocity = actual.velocityOverLifetime;
                    if (actualVelocity.x.mode !=
                            ParticleSystemCurveMode.TwoCurves ||
                        actualVelocity.y.mode !=
                            ParticleSystemCurveMode.TwoCurves ||
                        actualVelocity.z.mode !=
                            ParticleSystemCurveMode.TwoCurves)
                    {
                        throw new InvalidOperationException(
                            "The guardian flame X/Y/Z velocity curves do not share TwoCurves mode.");
                    }

                    var minimumCurve = actualVelocity.y.curveMin;
                    var maximumCurve = actualVelocity.y.curveMax;
                    var multiplier = actualVelocity.y.curveMultiplier;
                    var centerAtTailStart =
                        (minimumCurve.Evaluate(
                             GuardianTailRiseStartLengthRatio) +
                         maximumCurve.Evaluate(
                             GuardianTailRiseStartLengthRatio)) *
                        0.5f * multiplier;
                    var centerAtEnd =
                        (minimumCurve.Evaluate(1f) +
                         maximumCurve.Evaluate(1f)) *
                        0.5f * multiplier;
                    var sourceHalfSpread =
                        (sourceVelocity.y.constantMax -
                         sourceVelocity.y.constantMin) *
                        0.5f;
                    var endHalfSpread =
                        (maximumCurve.Evaluate(1f) -
                         minimumCurve.Evaluate(1f)) *
                        0.5f * multiplier;
                    var main = actual.main;
                    var sourceMain = sourceParticle.main;
                    var minimumLengthMultiplier =
                        main.startSpeed.constantMin /
                        sourceMain.startSpeed.constantMin;
                    var maximumLengthMultiplier =
                        main.startSpeed.constantMax /
                        sourceMain.startSpeed.constantMax;
                    var forwardSpeed =
                        (main.startSpeed.constantMin +
                         main.startSpeed.constantMax) *
                        0.5f;
                    var zMinimumCurve = actualVelocity.z.curveMin;
                    var zMaximumCurve = actualVelocity.z.curveMax;
                    var zMultiplier = actualVelocity.z.curveMultiplier;
                    var zCenterAtTailStart =
                        (zMinimumCurve.Evaluate(
                             GuardianTailRiseStartLengthRatio) +
                         zMaximumCurve.Evaluate(
                             GuardianTailRiseStartLengthRatio)) *
                        0.5f * zMultiplier;
                    var zCenterAtEnd =
                        (zMinimumCurve.Evaluate(1f) +
                         zMaximumCurve.Evaluate(1f)) *
                        0.5f * zMultiplier;
                    var terminalForwardSpeed =
                        forwardSpeed + zCenterAtEnd;
                    var angle =
                        Mathf.Atan2(
                            centerAtEnd,
                            terminalForwardSpeed) *
                        Mathf.Rad2Deg;
                    var riseHeightRatio =
                        IntegrateCenterVelocity(
                            minimumCurve,
                            maximumCurve,
                            multiplier,
                            GuardianTailRiseStartLengthRatio,
                            1f) /
                        forwardSpeed;
                    var xVelocityMaximum =
                        new[]
                        {
                            0f,
                            GuardianTailRiseStartLengthRatio,
                            1f
                        }
                            .Max(
                                age =>
                                    Mathf.Max(
                                        Mathf.Abs(
                                            actualVelocity.x.curveMin
                                                .Evaluate(age) *
                                            actualVelocity.x.curveMultiplier),
                                        Mathf.Abs(
                                            actualVelocity.x.curveMax
                                                .Evaluate(age) *
                                            actualVelocity.x.curveMultiplier)));
                    var zSpreadMaximum =
                        new[]
                        {
                            0f,
                            GuardianTailRiseStartLengthRatio,
                            1f
                        }
                            .Max(
                                age =>
                                    Mathf.Abs(
                                        (zMaximumCurve.Evaluate(age) -
                                         zMinimumCurve.Evaluate(age)) *
                                        zMultiplier));
                    if (Mathf.Abs(centerAtTailStart) > 0.001f ||
                        Mathf.Abs(endHalfSpread - sourceHalfSpread) >
                            0.001f ||
                        Mathf.Abs(
                            minimumLengthMultiplier -
                            GuardianFlameLengthMultiplier) > 0.000001f ||
                        Mathf.Abs(
                            maximumLengthMultiplier -
                            GuardianFlameLengthMultiplier) > 0.000001f ||
                        xVelocityMaximum > 0.000001f ||
                        Mathf.Abs(zCenterAtTailStart) > 0.001f ||
                        zSpreadMaximum > 0.000001f ||
                        Mathf.Abs(terminalForwardSpeed) > 0.001f ||
                        Mathf.Abs(
                            riseHeightRatio -
                            GuardianTailRiseHeightRatio) > 0.001f ||
                        Mathf.Abs(
                            angle -
                            GuardianTailRiseAngleDegrees) >
                        GuardianTailRiseToleranceDegrees)
                    {
                        throw new InvalidOperationException(
                            "The guardian flame tail rise contract differs. Layer=" +
                            actual.name +
                            ", TailStartCenter=" +
                            centerAtTailStart.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            ", LengthMultiplier=" +
                            minimumLengthMultiplier.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            "/" +
                            maximumLengthMultiplier.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            ", XVelocityMaximum=" +
                            xVelocityMaximum.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            ", TerminalForwardSpeed=" +
                            terminalForwardSpeed.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            ", RiseHeightRatio=" +
                            riseHeightRatio.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            ", EndAngle=" +
                            angle.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            ".");
                    }

                    minimumAngle = Mathf.Min(minimumAngle, angle);
                    maximumAngle = Mathf.Max(maximumAngle, angle);
                    minimumRiseHeightRatio =
                        Mathf.Min(
                            minimumRiseHeightRatio,
                            riseHeightRatio);
                    maximumRiseHeightRatio =
                        Mathf.Max(
                            maximumRiseHeightRatio,
                            riseHeightRatio);
                    minimumDensityMultiplier =
                        Mathf.Min(
                            minimumDensityMultiplier,
                            densityMultiplier);
                    maximumDensityMultiplier =
                        Mathf.Max(
                            maximumDensityMultiplier,
                            densityMultiplier);
                }

                RequireExactGuardianFlameEffectCopy(
                    expected.transform,
                    guardian);
                return new GuardianTailMetrics(
                    minimumAngle,
                    maximumAngle,
                    minimumRiseHeightRatio,
                    maximumRiseHeightRatio,
                    minimumDensityMultiplier,
                    maximumDensityMultiplier);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expected);
            }
        }

        private static float IntegrateCenterVelocity(
            AnimationCurve minimum,
            AnimationCurve maximum,
            float multiplier,
            float start,
            float end)
        {
            const int stepCount = 256;
            var step = (end - start) / stepCount;
            var sum =
                (minimum.Evaluate(start) + maximum.Evaluate(start)) *
                0.5f * multiplier +
                (minimum.Evaluate(end) + maximum.Evaluate(end)) *
                0.5f * multiplier;
            for (var index = 1; index < stepCount; index++)
            {
                var age = start + step * index;
                var center =
                    (minimum.Evaluate(age) + maximum.Evaluate(age)) *
                    0.5f * multiplier;
                sum += center * (index % 2 == 0 ? 2f : 4f);
            }

            return sum * step / 3f;
        }

        private static float RequireGuardianTailDensity(
            ParticleSystem baseline,
            ParticleSystem density)
        {
            var baselineEmission = baseline.emission.rateOverTime;
            var densityEmission = density.emission.rateOverTime;
            var baselineColor = baseline.colorOverLifetime;
            var densityColor = density.colorOverLifetime;
            var baselineRenderer =
                baseline.GetComponent<ParticleSystemRenderer>();
            var densityRenderer =
                density.GetComponent<ParticleSystemRenderer>();
            if (baselineEmission.mode != ParticleSystemCurveMode.Constant ||
                densityEmission.mode != ParticleSystemCurveMode.Constant ||
                baselineColor.color.mode !=
                    ParticleSystemGradientMode.Gradient ||
                densityColor.color.mode !=
                    ParticleSystemGradientMode.Gradient ||
                baselineRenderer == null ||
                densityRenderer == null ||
                baseline.transform.localPosition !=
                    density.transform.localPosition ||
                baseline.transform.localRotation !=
                    density.transform.localRotation ||
                baseline.transform.localScale !=
                    density.transform.localScale ||
                baselineRenderer.sharedMaterial !=
                    densityRenderer.sharedMaterial ||
                baselineRenderer.renderMode != densityRenderer.renderMode ||
                baselineRenderer.alignment != densityRenderer.alignment ||
                baselineRenderer.sortMode != densityRenderer.sortMode)
            {
                throw new InvalidOperationException(
                    "The guardian tail density layer copy contract differs.");
            }

            var baselineRate = baselineEmission.constant;
            var combinedDensityMultiplier =
                (baselineRate + densityEmission.constant) /
                baselineRate;
            var requiredMaximumParticles =
                Mathf.CeilToInt(
                    baseline.main.maxParticles *
                    (GuardianTailDensityMultiplier - 1f));
            var baselineGradient = baselineColor.color.gradient;
            var densityGradient = densityColor.color.gradient;
            var maskEnd =
                GuardianTailRiseStartLengthRatio +
                GuardianTailDensityMaskTransition;
            var hiddenAlphaMaximum =
                new[]
                {
                    0f,
                    GuardianTailRiseStartLengthRatio * 0.5f,
                    GuardianTailRiseStartLengthRatio
                }.Max(age => densityGradient.Evaluate(age).a);
            var visibleColorDifference =
                new[] { maskEnd, 0.85f, 0.95f, 1f }
                    .Max(
                        age =>
                            MaximumColorDifference(
                                baselineGradient.Evaluate(age),
                                densityGradient.Evaluate(age)));
            if (Mathf.Abs(
                    combinedDensityMultiplier -
                    GuardianTailDensityMultiplier) > 0.000001f ||
                density.main.maxParticles < requiredMaximumParticles ||
                hiddenAlphaMaximum > 0.001f ||
                visibleColorDifference > 0.001f)
            {
                throw new InvalidOperationException(
                    "The guardian tail density contract differs. Layer=" +
                    baseline.name +
                    ", CombinedDensityMultiplier=" +
                    combinedDensityMultiplier.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", HiddenAlphaMaximum=" +
                    hiddenAlphaMaximum.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", VisibleColorDifference=" +
                    visibleColorDifference.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ".");
            }

            return combinedDensityMultiplier;
        }

        private static float MaximumColorDifference(
            Color expected,
            Color actual)
        {
            return Mathf.Max(
                Mathf.Abs(expected.r - actual.r),
                Mathf.Max(
                    Mathf.Abs(expected.g - actual.g),
                    Mathf.Max(
                        Mathf.Abs(expected.b - actual.b),
                        Mathf.Abs(expected.a - actual.a))));
        }

        private static void RequireExactGuardianFlameEffectCopy(
            Transform expected,
            Transform actual)
        {
            var expectedChildren =
                expected.Cast<Transform>()
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray();
            var actualChildren =
                actual.Cast<Transform>()
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray();
            if (expectedChildren.Length != 4 ||
                actualChildren.Length != expectedChildren.Length)
            {
                throw new InvalidOperationException(
                    "The guardian flame hierarchy differs from the exact configured effect.");
            }

            for (var index = 0; index < expectedChildren.Length; index++)
            {
                var expectedChild = expectedChildren[index];
                var actualChild = actualChildren[index];
                var expectedParticle =
                    expectedChild.GetComponent<ParticleSystem>();
                var actualParticle =
                    actualChild.GetComponent<ParticleSystem>();
                var expectedRenderer =
                    expectedChild.GetComponent<ParticleSystemRenderer>();
                var actualRenderer =
                    actualChild.GetComponent<ParticleSystemRenderer>();
                if (expectedChild.name != actualChild.name ||
                    expectedChild.localPosition != actualChild.localPosition ||
                    expectedChild.localRotation != actualChild.localRotation ||
                    expectedChild.localScale != actualChild.localScale ||
                    expectedParticle == null ||
                    actualParticle == null ||
                    expectedRenderer == null ||
                    actualRenderer == null ||
                    EditorJsonUtility.ToJson(expectedParticle) !=
                        EditorJsonUtility.ToJson(actualParticle) ||
                    expectedRenderer.sharedMaterial !=
                        actualRenderer.sharedMaterial ||
                    expectedRenderer.renderMode != actualRenderer.renderMode ||
                    expectedRenderer.alignment != actualRenderer.alignment ||
                    expectedRenderer.sortMode != actualRenderer.sortMode)
                {
                    throw new InvalidOperationException(
                        "The guardian flame effect differs from the exact configured effect.");
                }
            }
        }

        private static void WriteGuardianReport(
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Transform model,
            Transform staticModel,
            GuardianWeaponMetrics weapon,
            GuardianTailMetrics tail)
        {
            var destination = Absolute(GuardianReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid guardian report path."));
            var report = new StringBuilder();
            report.AppendLine("Pahur Guardian Flamethrower Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceSha256=" + SourceGuardianSha256);
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
            report.AppendLine("WeaponBoneIndex=" + weapon.Aim.BoneIndex);
            report.AppendLine(
                "MaximumWeaponElevationDegrees=" +
                weapon.Aim.MaximumElevationDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumWeaponForwardAngleDegrees=" +
                weapon.MaximumForwardAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MuzzleLocalPosition=" +
                ScaleText(weapon.Aim.MuzzleLocalPosition));
            report.AppendLine("ExistingMiniFlameEffectCopiedExactly=True");
            report.AppendLine("ParticleSystems=4");
            report.AppendLine("BaseParticleSystems=2");
            report.AppendLine("TailDensityParticleSystems=2");
            report.AppendLine("VelocityCurveModeXYZ=TwoCurves");
            report.AppendLine("GuardianFlameLengthMultiplier=2");
            report.AppendLine("GuardianTailRiseStartLengthRatio=0.75");
            report.AppendLine("GuardianTailRiseHeightTargetRatio=1.5");
            report.AppendLine("GuardianTailDensityTargetMultiplier=5");
            report.AppendLine("GuardianTailDensityHiddenBeforeRatio=0.75");
            report.AppendLine("GuardianTailDensityFullVisibilityRatio=0.76");
            report.AppendLine("AdditionalVelocityX=0");
            report.AppendLine("TailStartAdditionalVelocityZ=0");
            report.AppendLine("TailEndForwardCenterVelocity=0");
            report.AppendLine("MiniFlameOverallHorizontalWidthMultiplier=2");
            report.AppendLine("MiniFlameTailHorizontalWidthMultiplier=5");
            report.AppendLine(
                "GuardianTailRiseTargetDegrees=" +
                GuardianTailRiseAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "GuardianTailRiseMinimumDegrees=" +
                tail.MinimumTailRiseDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "GuardianTailRiseMaximumDegrees=" +
                tail.MaximumTailRiseDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "GuardianTailRiseMinimumHeightRatio=" +
                tail.MinimumTailRiseHeightRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "GuardianTailRiseMaximumHeightRatio=" +
                tail.MaximumTailRiseHeightRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "GuardianTailDensityMinimumMultiplier=" +
                tail.MinimumTailDensityMultiplier.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "GuardianTailDensityMaximumMultiplier=" +
                tail.MaximumTailDensityMultiplier.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine("SceneChangedByValidation=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private readonly struct GuardianWeaponMetrics
        {
            public GuardianWeaponMetrics(
                MiniFlameAim aim,
                float maximumForwardAngleDegrees)
            {
                Aim = aim;
                MaximumForwardAngleDegrees =
                    maximumForwardAngleDegrees;
            }

            public MiniFlameAim Aim { get; }
            public float MaximumForwardAngleDegrees { get; }
        }

        private readonly struct GuardianTailMetrics
        {
            public GuardianTailMetrics(
                float minimumTailRiseDegrees,
                float maximumTailRiseDegrees,
                float minimumTailRiseHeightRatio,
                float maximumTailRiseHeightRatio,
                float minimumTailDensityMultiplier,
                float maximumTailDensityMultiplier)
            {
                MinimumTailRiseDegrees = minimumTailRiseDegrees;
                MaximumTailRiseDegrees = maximumTailRiseDegrees;
                MinimumTailRiseHeightRatio =
                    minimumTailRiseHeightRatio;
                MaximumTailRiseHeightRatio =
                    maximumTailRiseHeightRatio;
                MinimumTailDensityMultiplier =
                    minimumTailDensityMultiplier;
                MaximumTailDensityMultiplier =
                    maximumTailDensityMultiplier;
            }

            public float MinimumTailRiseDegrees { get; }
            public float MaximumTailRiseDegrees { get; }
            public float MinimumTailRiseHeightRatio { get; }
            public float MaximumTailRiseHeightRatio { get; }
            public float MinimumTailDensityMultiplier { get; }
            public float MaximumTailDensityMultiplier { get; }
        }
    }
}
