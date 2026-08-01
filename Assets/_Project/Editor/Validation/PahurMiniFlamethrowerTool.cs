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
        private const string SourceMiniFlameModelPath =
            @"D:\Bellerophon2\Bellerophon\enemies model\pāḫḫur flame attack.fbx";
        private const string MiniFlameAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurMiniFlameApprovedAppearanceMesh.asset";
        private const string ApprovedRunningAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurRunningApprovedAppearanceMesh.asset";
        private const string MiniFlameClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_04_MiniFlamethrower_Horizontal.anim";
        private const string MiniFlameControllerPath =
            "Assets/_Project/Art/Enemies/Pahur/Controllers/Pahur_04_MiniFlamethrower.controller";
        private const string MiniFlameStateName =
            "PahurMiniFlamethrower";
        private const string MiniFlameMuzzleName =
            "Pahur_MiniFlamethrower_Muzzle";
        private const string MiniFlameOuterMaterialPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_Flame_Outer.mat";
        private const string MiniFlameCoreMaterialPath =
            "Assets/_Project/Art/Enemies/Smorzando/VFX/Materials/Smorzando_Flame_Core.mat";
        private const string MiniFlameReportPath =
            "docs/validation/pahur_mini_flamethrower_2026-07-31/Pahur_04_MiniFlamethrower_Validation.txt";
        private const string MiniFlameCapturePath =
            "docs/validation/pahur_mini_flamethrower_2026-07-31/Pahur_04_MiniFlamethrower_Review.png";
        private const float MiniFlameInitialSizeFactor =
            0.42f;
        private const float MiniFlameMaximumStartSizeFactor =
            5.2f;
        private const float MiniFlameOuterTailVerticalSpeedFactor =
            0.75f;
        private const float MiniFlameCoreTailVerticalSpeedFactor =
            0.5f;
        private const float MiniFlameOverallHorizontalWidthMultiplier =
            2f;
        private const float MiniFlameTailHorizontalWidthMultiplier =
            5f;
        private const float MiniFlameTailWidthAge =
            0.72f;
        private const float MiniFlameFootSoleSelectionRadius =
            0.32f;
        private const float MiniFlameFootSoleSelectionHeight =
            0.008f;
        private const float MiniFlameMaximumFootGroundError =
            0.012f;
        private const float MiniFlameMaximumSoleHeightRange =
            0.01f;
        private const float MiniFlameMaximumSoleAngleDegrees =
            2f;

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Apply Mini Flamethrower")]
        public static void ApplyPahurMiniFlameAttack()
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
                    MiniFlameSlotName);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_04_MiniFlamethrower must contain exactly one current model.");
            }

            var otherSlots =
                OtherSlotSignatures(
                    placement.transform,
                    MiniFlameSlotName);
            var protectedRoots =
                ProtectedRootSignatures(
                    scene,
                    placement.transform);
            var slotPosition =
                slot.localPosition;
            var slotRotation =
                slot.localRotation;
            var slotScale =
                slot.localScale;

            RequireMiniFlameSourceHash();
            ImportMiniFlameModel();
            var takeName =
                ConfigureMiniFlameImporter();
            var miniPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    MiniFlameModelPath) ??
                throw new InvalidOperationException(
                    "The mini flame attack FBX is missing.");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var miniRenderer =
                RequireRenderer(
                    miniPrefab.transform,
                    "mini flame attack FBX");
            var sourceClip =
                RequireMiniFlameSourceClip(
                    takeName);
            var appearance =
                CreateMiniFlameAppearanceMesh(
                    miniPrefab,
                    miniRenderer);
            var inPlaceClip =
                CreateMiniFlameInPlaceClip(
                    sourceClip,
                    miniPrefab.transform,
                    miniRenderer);
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var aim =
                AuthorHorizontalMiniFlameAim(
                    inPlaceClip,
                    miniPrefab,
                    miniRenderer,
                    appearance,
                    weaponMaterialIndex);
            var leftFootGrounding =
                AuthorGroundedMiniFlameLeftFoot(
                    inPlaceClip,
                    miniPrefab,
                    appearance);
            RequireNoHorizontalRootTranslation(
                miniPrefab.transform,
                miniRenderer,
                inPlaceClip);
            var controller =
                CreateMiniFlameController(
                    inPlaceClip);
            var matchedScale =
                MatchedRunningScale(
                    staticPrefab,
                    miniPrefab,
                    staticModel);

            var previous =
                slot.GetChild(0);
            var previousPosition =
                previous.localPosition;
            var previousRotation =
                previous.localRotation;
            var replacement =
                PrefabUtility.InstantiatePrefab(
                    miniPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "The mini flame attack prefab could not be instantiated.");
            replacement.name =
                ModelName;
            replacement.transform.SetParent(
                slot,
                false);
            replacement.transform
                .SetLocalPositionAndRotation(
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
                        MiniFlameSlotName);
                renderer.sharedMesh =
                    appearance;
                renderer.sharedMaterials =
                    staticRenderer.sharedMaterials
                        .ToArray();
                renderer.updateWhenOffscreen =
                    true;
                EditorUtility.SetDirty(
                    renderer);
                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        renderer);

                var animator =
                    replacement.GetComponent<Animator>() ??
                    replacement.AddComponent<Animator>();
                var sourceAnimator =
                    miniPrefab.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "The mini flame attack FBX has no Animator.");
                animator.avatar =
                    sourceAnimator.avatar;
                animator.runtimeAnimatorController =
                    controller;
                animator.applyRootMotion =
                    false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode =
                    AnimatorUpdateMode.Normal;
                animator.enabled =
                    true;
                EditorUtility.SetDirty(
                    animator);

                var targetBone =
                    renderer.bones[aim.BoneIndex];
                var muzzle =
                    new GameObject(
                        MiniFlameMuzzleName);
                muzzle.transform.SetParent(
                    targetBone,
                    false);
                muzzle.transform.localPosition =
                    aim.MuzzleLocalPosition;
                muzzle.transform.localRotation =
                    aim.MuzzleLocalRotation;
                muzzle.transform.localScale =
                    Vector3.one;
                CreateMiniFlameParticles(
                    muzzle.transform,
                    aim.WeaponLength,
                    aim.WeaponRadius);
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
                    MiniFlameSlotName),
                "A Pahur slot outside Pahur_04_MiniFlamethrower changed.");
            RequireUnchanged(
                protectedRoots,
                ProtectedRootSignatures(
                    scene,
                    placement.transform),
                "A scene root outside the Pahur placement changed.");
            if (slot.localPosition !=
                    slotPosition ||
                slot.localRotation !=
                    slotRotation ||
                slot.localScale !=
                    slotScale)
            {
                throw new InvalidOperationException(
                    "The Pahur mini flamethrower slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(
                scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurMiniFlameAttackApplied Result=PASS" +
                ", SourceClip=" +
                sourceClip.name +
                ", PlaybackClip=" +
                inPlaceClip.name +
                ", Loop=True" +
                ", ExactApprovedAppearanceChannels=True" +
                ", HorizontalWeapon=True" +
                ", MuzzleFlame=True" +
                ", FlameTailVerticalSpread=True" +
                ", LeftFootGrounded=True" +
                ", MaximumLeftFootGroundError=" +
                leftFootGrounding.MaximumGroundError.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumLeftSoleHeightRange=" +
                leftFootGrounding.MaximumSoleHeightRange.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumLeftSoleAngleDegrees=" +
                leftFootGrounding.MaximumSoleAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", OtherSlotsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Validate Mini Flamethrower")]
        public static void ValidatePahurMiniFlameAttack()
        {
            var scene =
                RequireScene(false);
            var wasDirty =
                scene.isDirty;
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
            var slot =
                RequireChild(
                    placement.transform,
                    MiniFlameSlotName);
            var model =
                RequireModel(slot);
            var renderer =
                RequireRenderer(
                    model,
                    MiniFlameSlotName);
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    MiniFlameAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The mini flame appearance mesh is missing.");
            var miniPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    MiniFlameModelPath) ??
                throw new InvalidOperationException(
                    "The mini flame FBX is missing.");
            var miniRenderer =
                RequireRenderer(
                    miniPrefab.transform,
                    "mini flame FBX");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The static Pahur FBX is missing.");
            var expectedScale =
                MatchedRunningScale(
                    staticPrefab,
                    miniPrefab,
                    staticModel);
            if (renderer.sharedMesh !=
                    appearance ||
                !renderer.sharedMaterials
                    .SequenceEqual(
                        staticRenderer.sharedMaterials) ||
                model.localScale !=
                    Vector3.one *
                    expectedScale ||
                model.localPosition.y !=
                    staticModel.localPosition.y)
            {
                throw new InvalidOperationException(
                    "The mini flame Pahur appearance, size, or Y position differs.");
            }

            RequireMiniAppearancePreserved(
                miniRenderer.sharedMesh,
                appearance);
            var animator =
                model.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "The mini flame Pahur has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    MiniFlameControllerPath) ??
                throw new InvalidOperationException(
                    "The mini flame controller is missing.");
            var clip =
                controller.layers[0]
                    .stateMachine
                    .defaultState
                    .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The mini flame controller has no clip.");
            if (AssetDatabase.GetAssetPath(clip) !=
                    MiniFlameClipPath ||
                !clip.isLooping ||
                animator.runtimeAnimatorController !=
                    controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "The mini flame animation contract differs.");
            }

            RequireNoHorizontalRootTranslation(
                miniPrefab.transform,
                miniRenderer,
                clip);
            var weaponMaterialIndex =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var aim =
                RequireHorizontalMiniFlameAim(
                    clip,
                    miniPrefab,
                    miniRenderer,
                    appearance,
                    weaponMaterialIndex);
            var leftFootGrounding =
                RequireMiniFlameLeftFootGrounded(
                    clip,
                    miniPrefab,
                    appearance);
            var muzzle =
                model.GetComponentsInChildren<Transform>(
                        true)
                    .SingleOrDefault(
                        item =>
                            item.name ==
                            MiniFlameMuzzleName) ??
                throw new InvalidOperationException(
                    "The mini flamethrower muzzle is missing.");
            if (muzzle.parent.name !=
                    miniRenderer.bones[aim.BoneIndex]
                        .name ||
                muzzle.localPosition !=
                    aim.MuzzleLocalPosition ||
                Quaternion.Angle(
                    muzzle.localRotation,
                    aim.MuzzleLocalRotation) >
                    0.01f)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower muzzle anchor differs.");
            }

            var particleMetrics =
                RequireMiniFlameParticles(
                    muzzle,
                    aim.WeaponLength,
                    aim.WeaponRadius);
            WriteMiniFlameReport(
                clip,
                miniRenderer.sharedMesh,
                appearance,
                model.localScale,
                model.localPosition.y,
                staticModel.localPosition.y,
                aim,
                particleMetrics,
                leftFootGrounding);
            if (scene.isDirty !=
                wasDirty)
            {
                throw new InvalidOperationException(
                    "Mini flamethrower validation changed the scene.");
            }

            Debug.Log(
                "PahurMiniFlameAttackValidated Result=PASS" +
                ", Clip=" +
                clip.name +
                ", RunningVertices=" +
                miniRenderer.sharedMesh.vertexCount +
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
                ", FlameTailVerticalSpreadRatio=" +
                particleMetrics.TailVerticalSpreadRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", FlameMinimumHorizontalWidthMultiplier=" +
                particleMetrics.MinimumHorizontalWidthMultiplier.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", FlameTailHorizontalWidthMultiplier=" +
                particleMetrics.TailHorizontalWidthMultiplier.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", FlameTailToNearHorizontalWidthRatio=" +
                particleMetrics.TailToNearHorizontalWidthRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumLeftFootGroundError=" +
                leftFootGrounding.MaximumGroundError.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumLeftSoleHeightRange=" +
                leftFootGrounding.MaximumSoleHeightRange.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MaximumLeftSoleAngleDegrees=" +
                leftFootGrounding.MaximumSoleAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", ParticleSystems=2" +
                ", SceneChanged=False.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Capture Mini Flamethrower")]
        public static void CapturePahurMiniFlameAttackReview()
        {
            var scene =
                RequireScene(false);
            var wasDirty =
                scene.isDirty;
            var placement =
                RequirePlacement(scene);
            var model =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        MiniFlameSlotName));
            var animator =
                model.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "The mini flame Pahur has no Animator.");
            var controller =
                animator.runtimeAnimatorController as
                    AnimatorController ??
                throw new InvalidOperationException(
                    "The mini flame controller is missing.");
            var clip =
                controller.layers[0]
                    .stateMachine
                    .defaultState
                    .motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The mini flame clip is missing.");
            Capture(
                model,
                animator,
                clip,
                MiniFlameCapturePath);
            if (scene.isDirty !=
                wasDirty)
            {
                throw new InvalidOperationException(
                    "Mini flamethrower capture changed the scene.");
            }

            Debug.Log(
                "PahurMiniFlameAttackReviewCaptured Result=PASS" +
                ", Image=" +
                MiniFlameCapturePath +
                ", SceneChanged=False.");
        }

        private static void RequireMiniFlameSourceHash()
        {
            if (!File.Exists(
                    SourceMiniFlameModelPath) ||
                Sha256(
                    SourceMiniFlameModelPath) !=
                    SourceMiniFlameSha256)
            {
                throw new InvalidOperationException(
                    "The supplied mini flame attack FBX is missing or changed.");
            }
        }

        private static void ImportMiniFlameModel()
        {
            var destination =
                Absolute(
                    MiniFlameModelPath);
            if (!File.Exists(destination) ||
                Sha256(destination) !=
                    SourceMiniFlameSha256)
            {
                File.Copy(
                    SourceMiniFlameModelPath,
                    destination,
                    true);
            }

            AssetDatabase.ImportAsset(
                MiniFlameModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureMiniFlameImporter()
        {
            var importer =
                AssetImporter.GetAtPath(
                    MiniFlameModelPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "The mini flame attack importer is missing.");
            importer.importAnimation =
                true;
            importer.animationType =
                ModelImporterAnimationType.Generic;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects =
                false;
            importer.isReadable =
                true;
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
                                StringComparison.OrdinalIgnoreCase) >=
                            0 ||
                            item.takeName.IndexOf(
                                "mixamo",
                                StringComparison.OrdinalIgnoreCase) >=
                            0)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The mini flame FBX must contain exactly one Mixamo take. Found=" +
                    matches.Length +
                    ".");
            }

            var selected =
                matches[0];
            selected.loopTime =
                true;
            selected.loopPose =
                true;
            selected.wrapMode =
                WrapMode.Loop;
            selected.lockRootPositionXZ =
                true;
            selected.keepOriginalPositionXZ =
                true;
            importer.animationWrapMode =
                WrapMode.Loop;
            importer.clipAnimations =
                new[] { selected };
            importer.SaveAndReimport();
            return selected.name;
        }

        private static AnimationClip RequireMiniFlameSourceClip(
            string takeName)
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(
                        MiniFlameModelPath)
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
                            item.name ==
                            takeName ||
                            item.name.IndexOf(
                                "mixamo",
                                StringComparison.OrdinalIgnoreCase) >=
                            0)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The configured mini flame Mixamo clip is not unique.");
            }

            return matches[0];
        }

        private static Mesh CreateMiniFlameAppearanceMesh(
            GameObject miniPrefab,
            SkinnedMeshRenderer miniRenderer)
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
            var runningRenderer =
                RequireRenderer(
                    runningPrefab.transform,
                    "approved running FBX");
            RequireExactMiniTransferContract(
                miniRenderer,
                runningRenderer);
            var source =
                miniRenderer.sharedMesh;
            var generated =
                UnityEngine.Object.Instantiate(
                    source);
            generated.name =
                "PahurMiniFlameApprovedAppearanceMesh";
            var uv3 =
                new List<Vector4>();
            approved.GetUVs(
                3,
                uv3);
            if (uv3.Count !=
                source.vertexCount)
            {
                UnityEngine.Object.DestroyImmediate(
                    generated);
                throw new InvalidOperationException(
                    "The approved Pahur UV3 channel differs.");
            }

            generated.SetUVs(
                3,
                uv3);
            generated.subMeshCount =
                approved.subMeshCount;
            for (var subMesh = 0;
                 subMesh < approved.subMeshCount;
                 subMesh++)
            {
                generated.SetTriangles(
                    approved.GetTriangles(
                        subMesh),
                    subMesh,
                    false);
            }

            generated.bounds =
                source.bounds;
            if (AssetDatabase.LoadAssetAtPath<Mesh>(
                    MiniFlameAppearanceMeshPath) !=
                null &&
                !AssetDatabase.DeleteAsset(
                    MiniFlameAppearanceMeshPath))
            {
                UnityEngine.Object.DestroyImmediate(
                    generated);
                throw new InvalidOperationException(
                    "The previous mini flame appearance mesh could not be removed.");
            }

            AssetDatabase.CreateAsset(
                generated,
                MiniFlameAppearanceMeshPath);
            AssetDatabase.SaveAssets();
            RequireMiniAppearancePreserved(
                source,
                generated);
            return generated;
        }

        private static void RequireExactMiniTransferContract(
            SkinnedMeshRenderer mini,
            SkinnedMeshRenderer running)
        {
            var source =
                mini.sharedMesh;
            var approvedSource =
                running.sharedMesh;
            var matches =
                source.vertexCount ==
                    approvedSource.vertexCount &&
                source.subMeshCount ==
                    approvedSource.subMeshCount &&
                source.vertices.SequenceEqual(
                    approvedSource.vertices) &&
                source.normals.SequenceEqual(
                    approvedSource.normals) &&
                source.tangents.SequenceEqual(
                    approvedSource.tangents) &&
                source.uv.SequenceEqual(
                    approvedSource.uv) &&
                source.boneWeights.SequenceEqual(
                    approvedSource.boneWeights) &&
                Enumerable.Range(
                        0,
                        source.subMeshCount)
                    .All(
                        index =>
                            source.GetTriangles(index)
                                .SequenceEqual(
                                    approvedSource
                                        .GetTriangles(index))) &&
                mini.bones.Select(item =>
                        item.name)
                    .SequenceEqual(
                        running.bones.Select(item =>
                            item.name));
            if (!matches)
            {
                throw new InvalidOperationException(
                    "The mini flame model cannot receive the approved appearance by exact index.");
            }
        }

        private static void RequireMiniAppearancePreserved(
            Mesh source,
            Mesh appearance)
        {
            if (source.vertexCount !=
                    appearance.vertexCount ||
                source.bounds !=
                    appearance.bounds ||
                !source.vertices.SequenceEqual(
                    appearance.vertices) ||
                !source.normals.SequenceEqual(
                    appearance.normals) ||
                !source.tangents.SequenceEqual(
                    appearance.tangents) ||
                !source.uv.SequenceEqual(
                    appearance.uv) ||
                !source.boneWeights.SequenceEqual(
                    appearance.boneWeights) ||
                !source.bindposes.SequenceEqual(
                    appearance.bindposes))
            {
                throw new InvalidOperationException(
                    "The mini flame model shape or skin changed.");
            }

            var sourceIndices =
                Enumerable.Range(
                        0,
                        source.subMeshCount)
                    .SelectMany(
                        index =>
                            source.GetTriangles(index))
                    .OrderBy(item =>
                        item)
                    .ToArray();
            var appearanceIndices =
                Enumerable.Range(
                        0,
                        appearance.subMeshCount)
                    .SelectMany(
                        index =>
                            appearance.GetTriangles(index))
                    .OrderBy(item =>
                        item)
                    .ToArray();
            if (!sourceIndices.SequenceEqual(
                    appearanceIndices))
            {
                throw new InvalidOperationException(
                    "The mini flame appearance triangle indices changed.");
            }
        }

        private static AnimationClip CreateMiniFlameInPlaceClip(
            AnimationClip source,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    MiniFlameClipPath);
            if (clip == null)
            {
                clip =
                    new AnimationClip();
                AssetDatabase.CreateAsset(
                    clip,
                    MiniFlameClipPath);
            }

            EditorUtility.CopySerialized(
                source,
                clip);
            clip.name =
                "Pahur_04_MiniFlamethrower_Horizontal";
            clip.wrapMode =
                WrapMode.Loop;
            var rootPath =
                AnimationUtility.CalculateTransformPath(
                    renderer.rootBone,
                    root);
            var horizontalProperties =
                HorizontalLocalPositionProperties(
                    root,
                    renderer.rootBone.parent);
            foreach (var binding in
                     AnimationUtility.GetCurveBindings(
                             clip)
                         .Where(
                             binding =>
                                 (binding.path.Length ==
                                      0 &&
                                  (binding.propertyName ==
                                       "RootT.x" ||
                                   binding.propertyName ==
                                       "RootT.z" ||
                                   binding.propertyName ==
                                       "MotionT.x" ||
                                   binding.propertyName ==
                                       "MotionT.z")) ||
                                 (binding.path ==
                                      rootPath &&
                                  horizontalProperties
                                      .Contains(
                                          binding
                                              .propertyName)))
                         .ToArray())
            {
                var curve =
                    AnimationUtility.GetEditorCurve(
                        clip,
                        binding) ??
                    throw new InvalidOperationException(
                        "A mini flame root curve is missing.");
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.Constant(
                        0f,
                        clip.length,
                        curve.Evaluate(0f)));
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(
                    clip);
            settings.loopTime =
                true;
            settings.loopBlend =
                true;
            settings.keepOriginalPositionXZ =
                true;
            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);
            EditorUtility.SetDirty(
                clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static HashSet<string>
            HorizontalLocalPositionProperties(
                Transform model,
                Transform boneParent)
        {
            var result =
                new HashSet<string>(
                    StringComparer.Ordinal);
            var axes =
                new[]
                {
                    Vector3.right,
                    Vector3.up,
                    Vector3.forward
                };
            var suffixes =
                new[] { "x", "y", "z" };
            for (var index = 0;
                 index < axes.Length;
                 index++)
            {
                var direction =
                    model.InverseTransformDirection(
                            boneParent.TransformDirection(
                                axes[index]))
                        .normalized;
                if (Mathf.Abs(direction.x) >
                        0.5f ||
                    Mathf.Abs(direction.z) >
                        0.5f)
                {
                    result.Add(
                        "m_LocalPosition." +
                        suffixes[index]);
                }
            }

            return result;
        }

        private static int RequireWeaponMaterialIndex(
            IReadOnlyList<Material> materials)
        {
            var matches =
                Enumerable.Range(
                        0,
                        materials.Count)
                    .Where(
                        index =>
                            materials[index] !=
                                null &&
                            AssetDatabase.GetAssetPath(
                                    materials[index])
                                .EndsWith(
                                    "Pahur_weapon_gunmetal_Approved.mat",
                                    StringComparison.Ordinal))
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The approved Pahur weapon material slot is not unique.");
            }

            return matches[0];
        }

        private static string DescribeMiniWeaponComponents(
            GameObject prefab)
        {
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
            var weaponSubMesh =
                RequireWeaponMaterialIndex(
                    staticRenderer.sharedMaterials);
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    MiniFlameAppearanceMeshPath) ??
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    ApprovedRunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "No approved Pahur appearance mesh exists.");
            var renderer =
                RequireRenderer(
                    prefab.transform,
                    "mini weapon component inspection");
            var components =
                WeaponComponents(
                    appearance,
                    weaponSubMesh);
            var output =
                new StringBuilder();
            output.Append(
                Environment.NewLine +
                "WeaponConnectedComponents=" +
                components.Count);
            for (var index = 0;
                 index < components.Count;
                 index++)
            {
                var vertices =
                    components[index]
                        .Distinct()
                        .ToArray();
                var points =
                    vertices.Select(
                            vertex =>
                                renderer.transform
                                    .TransformPoint(
                                        appearance.vertices[
                                            vertex]))
                        .Select(
                            point =>
                                prefab.transform
                                    .InverseTransformPoint(
                                        point))
                        .ToArray();
                var center =
                    Average(points);
                var axis =
                    PrincipalAxis(
                        points,
                        center);
                var projections =
                    points.Select(
                            point =>
                                Vector3.Dot(
                                    point - center,
                                    axis))
                        .ToArray();
                var length =
                    projections.Max() -
                    projections.Min();
                var radius =
                    Mathf.Sqrt(
                        points.Average(
                            point =>
                            {
                                var offset =
                                    point - center;
                                var perpendicular =
                                    offset -
                                    axis *
                                    Vector3.Dot(
                                        offset,
                                        axis);
                                return perpendicular
                                    .sqrMagnitude;
                            }));
                var weights =
                    RightArmWeights(
                        renderer,
                        appearance,
                        vertices);
                output.Append(
                    Environment.NewLine +
                    "WeaponComponent=" +
                    index +
                    ", Vertices=" +
                    vertices.Length +
                    ", Length=" +
                    length.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", Radius=" +
                    radius.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", Elongation=" +
                    (length /
                     Mathf.Max(
                         radius,
                         0.000001f))
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", RightArm=" +
                    weights.RightArm.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", RightForeArm=" +
                    weights.RightForeArm.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", RightHand=" +
                    weights.RightHand.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            }

            return output.ToString();
        }

        private static List<int[]> WeaponComponents(
            Mesh mesh,
            int subMesh)
        {
            var triangles =
                mesh.GetTriangles(
                    subMesh);
            var parents =
                Enumerable.Range(
                        0,
                        mesh.vertexCount)
                    .ToArray();
            var ranks =
                new byte[
                    mesh.vertexCount];
            var samePositions =
                new Dictionary<Vector3, int>();
            foreach (var vertex in
                     triangles.Distinct())
            {
                var position =
                    mesh.vertices[vertex];
                if (samePositions.TryGetValue(
                        position,
                        out var existing))
                {
                    Union(
                        parents,
                        ranks,
                        vertex,
                        existing);
                }
                else
                {
                    samePositions.Add(
                        position,
                        vertex);
                }
            }

            for (var index = 0;
                 index < triangles.Length;
                 index += 3)
            {
                Union(
                    parents,
                    ranks,
                    triangles[index],
                    triangles[index + 1]);
                Union(
                    parents,
                    ranks,
                    triangles[index],
                    triangles[index + 2]);
            }

            return Enumerable.Range(
                    0,
                    triangles.Length /
                    3)
                .GroupBy(
                    triangle =>
                        Find(
                            parents,
                            triangles[
                                triangle * 3]))
                .Select(
                    group =>
                        group.SelectMany(
                                triangle =>
                                    new[]
                                    {
                                        triangles[
                                            triangle *
                                            3],
                                        triangles[
                                            triangle *
                                            3 +
                                            1],
                                        triangles[
                                            triangle *
                                            3 +
                                            2]
                                    })
                            .ToArray())
                .OrderByDescending(
                    component =>
                        component.Length)
                .ToList();
        }

        private static int Find(
            int[] parents,
            int value)
        {
            while (parents[value] !=
                   value)
            {
                parents[value] =
                    parents[
                        parents[value]];
                value =
                    parents[value];
            }

            return value;
        }

        private static void Union(
            int[] parents,
            byte[] ranks,
            int a,
            int b)
        {
            var rootA =
                Find(
                    parents,
                    a);
            var rootB =
                Find(
                    parents,
                    b);
            if (rootA == rootB)
            {
                return;
            }

            if (ranks[rootA] <
                ranks[rootB])
            {
                parents[rootA] =
                    rootB;
            }
            else
            {
                parents[rootB] =
                    rootA;
                if (ranks[rootA] ==
                    ranks[rootB])
                {
                    ranks[rootA]++;
                }
            }
        }

        private static RightArmWeightTotals RightArmWeights(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            IReadOnlyList<int> vertices)
        {
            var totals =
                new float[
                    renderer.bones.Length];
            var weights =
                mesh.boneWeights;
            foreach (var vertex in vertices)
            {
                AddBoneWeight(
                    totals,
                    weights[vertex].boneIndex0,
                    weights[vertex].weight0);
                AddBoneWeight(
                    totals,
                    weights[vertex].boneIndex1,
                    weights[vertex].weight1);
                AddBoneWeight(
                    totals,
                    weights[vertex].boneIndex2,
                    weights[vertex].weight2);
                AddBoneWeight(
                    totals,
                    weights[vertex].boneIndex3,
                    weights[vertex].weight3);
            }

            float Named(
                string name)
            {
                var index =
                    Array.FindIndex(
                        renderer.bones,
                        item =>
                            item.name ==
                            name);
                return index >= 0
                    ? totals[index]
                    : 0f;
            }

            return new RightArmWeightTotals(
                Named("RightArm"),
                Named("RightForeArm"),
                Named("RightHand"));
        }

        private static MiniFlameAim AuthorHorizontalMiniFlameAim(
            AnimationClip clip,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            Mesh appearance,
            int weaponSubMesh)
        {
            var boneIndex =
                RequireRightWeaponBoneIndex(
                    prefabRenderer,
                    appearance,
                    RequireWeaponBarrelIndices(
                        prefabRenderer,
                        appearance,
                        weaponSubMesh));
            var clone =
                UnityEngine.Object.Instantiate(
                    prefab);
            clone.hideFlags =
                HideFlags.HideAndDontSave;
            var baked =
                new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "mini flame aim authoring");
                renderer.sharedMesh =
                    appearance;
                var bone =
                    renderer.bones[boneIndex];
                var bonePath =
                    AnimationUtility.CalculateTransformPath(
                        bone,
                        clone.transform);
                foreach (var binding in
                         AnimationUtility.GetCurveBindings(
                                 clip)
                             .Where(
                                 item =>
                                     item.path ==
                                         bonePath &&
                                     item.propertyName
                                         .IndexOf(
                                             "localEuler",
                                             StringComparison
                                                 .OrdinalIgnoreCase) >=
                                     0)
                             .ToArray())
                {
                    AnimationUtility.SetEditorCurve(
                        clip,
                        binding,
                        null);
                }

                var frameCount =
                    Mathf.Clamp(
                        Mathf.CeilToInt(
                            clip.length *
                            Mathf.Max(
                                1f,
                                clip.frameRate)) +
                        1,
                        2,
                        241);
                var xKeys =
                    new Keyframe[frameCount];
                var yKeys =
                    new Keyframe[frameCount];
                var zKeys =
                    new Keyframe[frameCount];
                var wKeys =
                    new Keyframe[frameCount];
                var previous =
                    Quaternion.identity;
                var weaponIndices =
                    RequireWeaponBarrelIndices(
                        prefabRenderer,
                        appearance,
                        weaponSubMesh);
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
                         iteration < 3;
                         iteration++)
                    {
                        renderer.BakeMesh(
                            baked);
                        var frame =
                            AnalyzeWeapon(
                                clone.transform,
                                renderer,
                                baked,
                                weaponIndices,
                                bone.position);
                        var horizontal =
                            new Vector3(
                                frame.Direction.x,
                                0f,
                                frame.Direction.z);
                        if (horizontal.sqrMagnitude <
                            0.000001f)
                        {
                            throw new InvalidOperationException(
                                "The mini flamethrower has no horizontal direction.");
                        }

                        var correction =
                            Quaternion.FromToRotation(
                                clone.transform
                                    .TransformDirection(
                                        frame.Direction),
                                clone.transform
                                    .TransformDirection(
                                        horizontal
                                            .normalized));
                        bone.rotation =
                            correction *
                            bone.rotation;
                    }

                    var rotation =
                        bone.localRotation;
                    if (index > 0 &&
                        Quaternion.Dot(
                            previous,
                            rotation) <
                        0f)
                    {
                        rotation =
                            new Quaternion(
                                -rotation.x,
                                -rotation.y,
                                -rotation.z,
                                -rotation.w);
                    }

                    previous =
                        rotation;
                    xKeys[index] =
                        new Keyframe(
                            time,
                            rotation.x);
                    yKeys[index] =
                        new Keyframe(
                            time,
                            rotation.y);
                    zKeys[index] =
                        new Keyframe(
                            time,
                            rotation.z);
                    wKeys[index] =
                        new Keyframe(
                            time,
                            rotation.w);
                }

                SetQuaternionCurve(
                    clip,
                    bonePath,
                    "x",
                    xKeys);
                SetQuaternionCurve(
                    clip,
                    bonePath,
                    "y",
                    yKeys);
                SetQuaternionCurve(
                    clip,
                    bonePath,
                    "z",
                    zKeys);
                SetQuaternionCurve(
                    clip,
                    bonePath,
                    "w",
                    wKeys);
                clip.EnsureQuaternionContinuity();
                EditorUtility.SetDirty(
                    clip);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    baked);
                UnityEngine.Object.DestroyImmediate(
                    clone);
            }

            return RequireHorizontalMiniFlameAim(
                clip,
                prefab,
                prefabRenderer,
                appearance,
                weaponSubMesh);
        }

        private static MiniFlameLeftFootGrounding
            AuthorGroundedMiniFlameLeftFoot(
            AnimationClip clip,
            GameObject prefab,
            Mesh appearance)
        {
            var clone =
                UnityEngine.Object.Instantiate(
                    prefab);
            clone.hideFlags =
                HideFlags.HideAndDontSave;
            var baked =
                new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "mini flame left foot authoring");
                renderer.sharedMesh =
                    appearance;
                var leftUpLeg =
                    RequireMiniFlameBone(
                        renderer,
                        "LeftUpLeg");
                var leftLeg =
                    RequireMiniFlameBone(
                        renderer,
                        "LeftLeg");
                var leftFoot =
                    RequireMiniFlameBone(
                        renderer,
                        "LeftFoot");
                var leftToe =
                    RequireMiniFlameBone(
                        renderer,
                        "LeftToeBase");
                var neutralToeRotation =
                    leftToe.localRotation;
                var soleSelection =
                    MiniFlameLeftSoleIndices(
                        appearance,
                        renderer,
                        leftLeg,
                        leftFoot,
                        leftToe);
                var groundY =
                    appearance.bounds.min.y;
                var frameCount =
                    Mathf.Clamp(
                        Mathf.CeilToInt(
                            clip.length *
                            Mathf.Max(
                                1f,
                                clip.frameRate)) +
                        1,
                        2,
                        241);
                var times =
                    new float[frameCount];
                var upLegRotations =
                    new Quaternion[frameCount];
                var legRotations =
                    new Quaternion[frameCount];
                var footRotations =
                    new Quaternion[frameCount];
                var toeRotations =
                    new Quaternion[frameCount];
                var authoredMaximumGroundError =
                    0f;
                var authoredMaximumHeightRange =
                    0f;
                var authoredMaximumAngle =
                    0f;
                for (var index = 0;
                     index < frameCount;
                     index++)
                {
                    var time =
                        clip.length *
                        index /
                        (frameCount - 1f);
                    times[index] =
                        time;
                    clip.SampleAnimation(
                        clone,
                        time);
                    leftToe.localRotation =
                        neutralToeRotation;
                    for (var coupling = 0;
                         coupling < 32;
                         coupling++)
                    {
                        for (var flatten = 0;
                             flatten < 3;
                             flatten++)
                        {
                            renderer.BakeMesh(
                                baked);
                            var solePlane =
                                MeasureMiniFlameLeftSole(
                                    clone.transform,
                                    renderer,
                                    baked,
                                    soleSelection.AllIndices);
                            leftFoot.rotation =
                                Quaternion.FromToRotation(
                                    clone.transform
                                        .TransformDirection(
                                            solePlane.Normal),
                                    clone.transform.up) *
                                leftFoot.rotation;
                        }

                        renderer.BakeMesh(
                            baked);
                        var sole =
                            MeasureMiniFlameLeftSole(
                                clone.transform,
                                renderer,
                                baked,
                                soleSelection.AllIndices);
                        var groundError =
                            sole.MinimumY -
                            groundY;
                        if (Mathf.Abs(
                                groundError) <=
                                0.00005f &&
                            sole.MaximumY -
                                sole.MinimumY <=
                                MiniFlameMaximumSoleHeightRange &&
                            sole.AngleDegrees <=
                                MiniFlameMaximumSoleAngleDegrees)
                        {
                            break;
                        }

                        if (Mathf.Abs(
                                groundError) >
                            0.00005f)
                        {
                            var flattenedFootRotation =
                                leftFoot.rotation;
                            var target =
                                leftFoot.position -
                                clone.transform.TransformVector(
                                    Vector3.up *
                                    groundError);
                            SolveMiniFlameTwoBoneCcd(
                                leftUpLeg,
                                leftLeg,
                                leftFoot,
                                target);
                            leftFoot.rotation =
                                flattenedFootRotation;
                        }
                    }

                    renderer.BakeMesh(
                        baked);
                    var authoredSole =
                        MeasureMiniFlameLeftSole(
                            clone.transform,
                            renderer,
                            baked,
                            soleSelection.AllIndices);
                    authoredMaximumGroundError =
                        Mathf.Max(
                            authoredMaximumGroundError,
                            Mathf.Max(
                                Mathf.Abs(
                                    authoredSole.MinimumY -
                                    groundY),
                                Mathf.Abs(
                                    authoredSole.MaximumY -
                                    groundY)));
                    authoredMaximumHeightRange =
                        Mathf.Max(
                            authoredMaximumHeightRange,
                            authoredSole.MaximumY -
                            authoredSole.MinimumY);
                    authoredMaximumAngle =
                        Mathf.Max(
                            authoredMaximumAngle,
                            authoredSole.AngleDegrees);

                    upLegRotations[index] =
                        leftUpLeg.localRotation;
                    legRotations[index] =
                        leftLeg.localRotation;
                    footRotations[index] =
                        leftFoot.localRotation;
                    toeRotations[index] =
                        leftToe.localRotation;
                }

                SetQuaternionCurves(
                    clip,
                    AnimationUtility
                        .CalculateTransformPath(
                            leftUpLeg,
                            clone.transform),
                    times,
                    upLegRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility
                        .CalculateTransformPath(
                            leftLeg,
                            clone.transform),
                    times,
                    legRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility
                        .CalculateTransformPath(
                            leftFoot,
                            clone.transform),
                    times,
                    footRotations);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility
                        .CalculateTransformPath(
                            leftToe,
                            clone.transform),
                    times,
                    toeRotations);
                clip.EnsureQuaternionContinuity();
                Debug.Log(
                    "Pahur mini flame authored left sole: " +
                    "MaximumError=" +
                    authoredMaximumGroundError.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", MaximumHeightRange=" +
                    authoredMaximumHeightRange.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", MaximumAngleDegrees=" +
                    authoredMaximumAngle.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                EditorUtility.SetDirty(
                    clip);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    baked);
                UnityEngine.Object.DestroyImmediate(
                    clone);
            }

            return RequireMiniFlameLeftFootGrounded(
                clip,
                prefab,
                appearance);
        }

        private static MiniFlameLeftFootGrounding
            RequireMiniFlameLeftFootGrounded(
            AnimationClip clip,
            GameObject prefab,
            Mesh appearance)
        {
            var clone =
                UnityEngine.Object.Instantiate(
                    prefab);
            clone.hideFlags =
                HideFlags.HideAndDontSave;
            var baked =
                new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "mini flame left foot validation");
                renderer.sharedMesh =
                    appearance;
                var leftFoot =
                    RequireMiniFlameBone(
                        renderer,
                        "LeftFoot");
                var leftToe =
                    RequireMiniFlameBone(
                        renderer,
                        "LeftToeBase");
                var soleSelection =
                    MiniFlameLeftSoleIndices(
                        appearance,
                        renderer,
                        RequireMiniFlameBone(
                            renderer,
                            "LeftLeg"),
                        leftFoot,
                        leftToe);
                var groundY =
                    appearance.bounds.min.y;
                var maximumError =
                    0f;
                var maximumHeightRange =
                    0f;
                var maximumAngle =
                    0f;
                for (var index = 0;
                     index <= 32;
                     index++)
                {
                    clip.SampleAnimation(
                        clone,
                        clip.length *
                        index /
                        32f);
                    renderer.BakeMesh(
                        baked);
                    var sole =
                        MeasureMiniFlameLeftSole(
                            clone.transform,
                            renderer,
                            baked,
                            soleSelection.AllIndices);
                    maximumError =
                        Mathf.Max(
                            maximumError,
                            Mathf.Max(
                                Mathf.Abs(
                                    sole.MinimumY -
                                    groundY),
                                Mathf.Abs(
                                    sole.MaximumY -
                                    groundY)));
                    maximumHeightRange =
                        Mathf.Max(
                            maximumHeightRange,
                            sole.MaximumY -
                            sole.MinimumY);
                    maximumAngle =
                        Mathf.Max(
                            maximumAngle,
                            sole.AngleDegrees);
                }

                if (maximumError >
                        MiniFlameMaximumFootGroundError ||
                    maximumHeightRange >
                        MiniFlameMaximumSoleHeightRange ||
                    maximumAngle >
                        MiniFlameMaximumSoleAngleDegrees)
                {
                    throw new InvalidOperationException(
                        "The mini flamethrower full left sole is not grounded. MaximumError=" +
                        maximumError.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        ", MaximumHeightRange=" +
                        maximumHeightRange.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        ", MaximumAngleDegrees=" +
                        maximumAngle.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                }

                return new MiniFlameLeftFootGrounding(
                    maximumError,
                    maximumHeightRange,
                    maximumAngle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    baked);
                UnityEngine.Object.DestroyImmediate(
                    clone);
            }
        }

        private static Transform RequireMiniFlameBone(
            SkinnedMeshRenderer renderer,
            string name)
        {
            return renderer.bones
                       .SingleOrDefault(
                           item =>
                               item.name ==
                               name) ??
                   throw new InvalidOperationException(
                       "The mini flamethrower rig is missing " +
                       name +
                       ".");
        }

        private static MiniFlameSoleSelection
            MiniFlameLeftSoleIndices(
            Mesh appearance,
            SkinnedMeshRenderer renderer,
            Transform leftLeg,
            Transform leftFoot,
            Transform leftToe)
        {
            var vertices =
                appearance.vertices;
            var boneIndex =
                Array.IndexOf(
                    renderer.bones,
                    leftFoot);
            var legBoneIndex =
                Array.IndexOf(
                    renderer.bones,
                    leftLeg);
            var toeBoneIndex =
                Array.IndexOf(
                    renderer.bones,
                    leftToe);
            if (legBoneIndex < 0 ||
                boneIndex < 0 ||
                toeBoneIndex < 0 ||
                legBoneIndex >=
                    appearance.bindposes.Length ||
                boneIndex >=
                    appearance.bindposes.Length ||
                toeBoneIndex >=
                    appearance.bindposes.Length)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower left foot bind pose is missing.");
            }

            var footPosition =
                appearance.bindposes[boneIndex]
                    .inverse
                    .MultiplyPoint3x4(
                        Vector3.zero);
            var legPosition =
                appearance.bindposes[legBoneIndex]
                    .inverse
                    .MultiplyPoint3x4(
                        Vector3.zero);
            var toePosition =
                appearance.bindposes[toeBoneIndex]
                    .inverse
                    .MultiplyPoint3x4(
                        Vector3.zero);
            var footToToe =
                (toePosition - footPosition)
                .normalized;
            var legToFoot =
                footPosition - legPosition;
            var soleDown =
                (legToFoot -
                 footToToe *
                 Vector3.Dot(
                     legToFoot,
                     footToToe))
                .normalized;
            if (footToToe.sqrMagnitude <
                    Mathf.Epsilon ||
                soleDown.sqrMagnitude <
                    Mathf.Epsilon)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower left sole bind axes are unavailable.");
            }

            var candidates =
                Enumerable.Range(
                        0,
                        vertices.Length)
                    .Where(
                        index =>
                            Vector3.Distance(
                                vertices[index],
                                footPosition) <=
                            MiniFlameFootSoleSelectionRadius &&
                            MiniFlameBoneWeight(
                                appearance.boneWeights[
                                    index],
                                boneIndex,
                                toeBoneIndex) >=
                            0.5f)
                    .ToArray();
            if (candidates.Length == 0)
            {
                throw new InvalidOperationException(
                    "No mini flamethrower left foot vertices were found.");
            }

            var longitudinalMinimum =
                candidates.Min(
                    index =>
                        Vector3.Dot(
                            vertices[index] -
                            footPosition,
                            footToToe));
            var longitudinalMaximum =
                candidates.Max(
                    index =>
                        Vector3.Dot(
                            vertices[index] -
                            footPosition,
                            footToToe));
            var longitudinalRange =
                longitudinalMaximum -
                longitudinalMinimum;
            var heelLimit =
                longitudinalMinimum +
                longitudinalRange *
                0.35f;
            var toeLimit =
                longitudinalMaximum -
                longitudinalRange *
                0.35f;
            var heelCandidates =
                candidates.Where(
                        index =>
                            Vector3.Dot(
                                vertices[index] -
                                footPosition,
                                footToToe) <=
                            heelLimit)
                    .ToArray();
            var toeCandidates =
                candidates.Where(
                        index =>
                            Vector3.Dot(
                                vertices[index] -
                                footPosition,
                                footToToe) >=
                            toeLimit)
                    .ToArray();
            if (heelCandidates.Length == 0 ||
                toeCandidates.Length == 0)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower left sole contact regions are missing.");
            }

            var heelMaximumDown =
                heelCandidates.Max(
                    index =>
                        Vector3.Dot(
                            vertices[index] -
                            footPosition,
                            soleDown));
            var toeMaximumDown =
                toeCandidates.Max(
                    index =>
                        Vector3.Dot(
                            vertices[index] -
                            footPosition,
                            soleDown));
            var heel =
                heelCandidates.Where(
                        index =>
                            Vector3.Dot(
                                vertices[index] -
                                footPosition,
                                soleDown) >=
                            heelMaximumDown -
                            MiniFlameFootSoleSelectionHeight)
                    .ToArray();
            var toe =
                toeCandidates.Where(
                        index =>
                            Vector3.Dot(
                                vertices[index] -
                                footPosition,
                                soleDown) >=
                            toeMaximumDown -
                            MiniFlameFootSoleSelectionHeight)
                    .ToArray();
            if (heel.Length < 3 ||
                toe.Length < 3)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower left heel or toe sole selection is incomplete.");
            }

            var sole =
                heel.Concat(
                        toe)
                    .Distinct()
                    .ToArray();
            Debug.Log(
                "Pahur mini flame left sole weights: " +
                "Vertices=" +
                sole.Length +
                ", LeftLeg=" +
                sole.Average(
                        index =>
                            MiniFlameBoneWeight(
                                appearance.boneWeights[index],
                                legBoneIndex))
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                ", LeftFoot=" +
                sole.Average(
                        index =>
                            MiniFlameBoneWeight(
                                appearance.boneWeights[index],
                                boneIndex))
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                ", LeftToeBase=" +
                sole.Average(
                        index =>
                            MiniFlameBoneWeight(
                                appearance.boneWeights[index],
                                toeBoneIndex))
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            return new MiniFlameSoleSelection(
                sole,
                heel,
                toe);
        }

        private static float MiniFlameBoneWeight(
            BoneWeight weight,
            int boneIndex)
        {
            var total =
                0f;
            if (weight.boneIndex0 == boneIndex)
            {
                total +=
                    weight.weight0;
            }

            if (weight.boneIndex1 == boneIndex)
            {
                total +=
                    weight.weight1;
            }

            if (weight.boneIndex2 == boneIndex)
            {
                total +=
                    weight.weight2;
            }

            if (weight.boneIndex3 == boneIndex)
            {
                total +=
                    weight.weight3;
            }

            return total;
        }

        private static float MiniFlameBoneWeight(
            BoneWeight weight,
            int first,
            int second)
        {
            var total =
                0f;
            if (weight.boneIndex0 == first ||
                weight.boneIndex0 == second)
            {
                total +=
                    weight.weight0;
            }

            if (weight.boneIndex1 == first ||
                weight.boneIndex1 == second)
            {
                total +=
                    weight.weight1;
            }

            if (weight.boneIndex2 == first ||
                weight.boneIndex2 == second)
            {
                total +=
                    weight.weight2;
            }

            if (weight.boneIndex3 == first ||
                weight.boneIndex3 == second)
            {
                total +=
                    weight.weight3;
            }

            return total;
        }

        private static MiniFlameLeftSoleMetrics
            MeasureMiniFlameLeftSole(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh baked,
            IReadOnlyList<int> soleIndices)
        {
            var vertices =
                baked.vertices;
            var points =
                soleIndices.Select(
                        index =>
                            model.InverseTransformPoint(
                                renderer.transform
                                    .TransformPoint(
                                        vertices[index])))
                    .ToArray();
            var center =
                Average(
                    points);
            var forward =
                PrincipalAxis(
                    points,
                    center);
            var lateralPoints =
                points.Select(
                        point =>
                        {
                            var offset =
                                point - center;
                            return center +
                                   offset -
                                   forward *
                                   Vector3.Dot(
                                       offset,
                                       forward);
                        })
                    .ToArray();
            var lateral =
                PrincipalAxis(
                    lateralPoints,
                    center);
            var normal =
                Vector3.Cross(
                        forward,
                        lateral)
                    .normalized;
            if (Vector3.Dot(
                    normal,
                    Vector3.up) <
                0f)
            {
                normal =
                    -normal;
            }

            return new MiniFlameLeftSoleMetrics(
                normal,
                points.Min(
                    point =>
                        point.y),
                points.Max(
                    point =>
                        point.y),
                Vector3.Angle(
                    normal,
                    Vector3.up));
        }

        private static void OptimizeMiniFlameLeftSole(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh baked,
            Transform leftFoot,
            Transform leftToe,
            IReadOnlyList<int> soleIndices)
        {
            var steps =
                new[]
                {
                    30f,
                    15f,
                    7.5f,
                    3f,
                    1f,
                    0.3f
                };
            var bones =
                new[]
                {
                    leftFoot,
                    leftToe
                };
            var axes =
                new[]
                {
                    model.right,
                    model.forward
                };
            renderer.BakeMesh(
                baked);
            var bestScore =
                MiniFlameSoleScore(
                    model,
                    renderer,
                    baked,
                    soleIndices);
            foreach (var step in steps)
            {
                for (var sweep = 0;
                     sweep < 2;
                     sweep++)
                {
                    foreach (var bone in bones)
                    {
                        foreach (var axis in axes)
                        {
                            var originalRotation =
                                bone.rotation;
                            var bestRotation =
                                originalRotation;
                            foreach (var sign in new[]
                                     {
                                         -1f,
                                         1f
                                     })
                            {
                                bone.rotation =
                                    Quaternion.AngleAxis(
                                        sign * step,
                                        axis) *
                                    originalRotation;
                                renderer.BakeMesh(
                                    baked);
                                var score =
                                    MiniFlameSoleScore(
                                        model,
                                        renderer,
                                        baked,
                                        soleIndices);
                                if (score < bestScore)
                                {
                                    bestScore =
                                        score;
                                    bestRotation =
                                        bone.rotation;
                                }
                            }

                            bone.rotation =
                                bestRotation;
                        }
                    }
                }
            }
        }

        private static float MiniFlameSoleScore(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh baked,
            IReadOnlyList<int> soleIndices)
        {
            var sole =
                MeasureMiniFlameLeftSole(
                    model,
                    renderer,
                    baked,
                    soleIndices);
            return sole.MaximumY -
                   sole.MinimumY +
                   sole.AngleDegrees *
                   0.001f;
        }

        private static void SolveMiniFlameTwoBoneCcd(
            Transform upper,
            Transform lower,
            Transform foot,
            Vector3 target)
        {
            for (var iteration = 0;
                 iteration < 8;
                 iteration++)
            {
                RotateMiniFlameBoneToTarget(
                    lower,
                    foot,
                    target);
                RotateMiniFlameBoneToTarget(
                    upper,
                    foot,
                    target);
                if (Vector3.Distance(
                        foot.position,
                        target) <=
                    0.00005f)
                {
                    break;
                }
            }
        }

        private static void RotateMiniFlameBoneToTarget(
            Transform bone,
            Transform foot,
            Vector3 target)
        {
            var currentDirection =
                foot.position -
                bone.position;
            var targetDirection =
                target -
                bone.position;
            if (currentDirection.sqrMagnitude <
                    0.00000001f ||
                targetDirection.sqrMagnitude <
                    0.00000001f)
            {
                return;
            }

            bone.rotation =
                Quaternion.FromToRotation(
                    currentDirection,
                    targetDirection) *
                bone.rotation;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations)
        {
            var xKeys =
                new Keyframe[times.Count];
            var yKeys =
                new Keyframe[times.Count];
            var zKeys =
                new Keyframe[times.Count];
            var wKeys =
                new Keyframe[times.Count];
            var previous =
                Quaternion.identity;
            for (var index = 0;
                 index < times.Count;
                 index++)
            {
                var rotation =
                    rotations[index];
                if (index > 0 &&
                    Quaternion.Dot(
                        previous,
                        rotation) <
                    0f)
                {
                    rotation =
                        new Quaternion(
                            -rotation.x,
                            -rotation.y,
                            -rotation.z,
                            -rotation.w);
                }

                previous =
                    rotation;
                xKeys[index] =
                    new Keyframe(
                        times[index],
                        rotation.x);
                yKeys[index] =
                    new Keyframe(
                        times[index],
                        rotation.y);
                zKeys[index] =
                    new Keyframe(
                        times[index],
                        rotation.z);
                wKeys[index] =
                    new Keyframe(
                        times[index],
                        rotation.w);
            }

            SetQuaternionCurve(
                clip,
                path,
                "x",
                xKeys);
            SetQuaternionCurve(
                clip,
                path,
                "y",
                yKeys);
            SetQuaternionCurve(
                clip,
                path,
                "z",
                zKeys);
            SetQuaternionCurve(
                clip,
                path,
                "w",
                wKeys);
        }

        private static void SetQuaternionCurve(
            AnimationClip clip,
            string path,
            string component,
            Keyframe[] keys)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation." +
                    component),
                new AnimationCurve(keys));
        }

        private static MiniFlameAim RequireHorizontalMiniFlameAim(
            AnimationClip clip,
            GameObject prefab,
            SkinnedMeshRenderer prefabRenderer,
            Mesh appearance,
            int weaponSubMesh)
        {
            var boneIndex =
                RequireRightWeaponBoneIndex(
                    prefabRenderer,
                    appearance,
                    RequireWeaponBarrelIndices(
                        prefabRenderer,
                        appearance,
                        weaponSubMesh));
            var clone =
                UnityEngine.Object.Instantiate(
                    prefab);
            clone.hideFlags =
                HideFlags.HideAndDontSave;
            var baked =
                new Mesh();
            try
            {
                var renderer =
                    RequireRenderer(
                        clone.transform,
                        "mini flame aim validation");
                renderer.sharedMesh =
                    appearance;
                var bone =
                    renderer.bones[boneIndex];
                var weaponIndices =
                    RequireWeaponBarrelIndices(
                        prefabRenderer,
                        appearance,
                        weaponSubMesh);
                var maximumElevation =
                    0f;
                WeaponFrame first =
                    default;
                for (var index = 0;
                     index <= 16;
                     index++)
                {
                    clip.SampleAnimation(
                        clone,
                        clip.length *
                        index /
                        16f);
                    renderer.BakeMesh(
                        baked);
                    var frame =
                        AnalyzeWeapon(
                            clone.transform,
                            renderer,
                            baked,
                            weaponIndices,
                            bone.position);
                    if (index == 0)
                    {
                        first =
                            frame;
                    }

                    maximumElevation =
                        Mathf.Max(
                            maximumElevation,
                            Mathf.Abs(
                                Mathf.Asin(
                                    Mathf.Clamp(
                                        frame.Direction.y,
                                        -1f,
                                        1f)) *
                                Mathf.Rad2Deg));
                }

                if (maximumElevation >
                    0.35f)
                {
                    throw new InvalidOperationException(
                        "The mini flamethrower is not horizontal. MaximumElevation=" +
                        maximumElevation.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                }

                clip.SampleAnimation(
                    clone,
                    0f);
                renderer.BakeMesh(
                    baked);
                first =
                    AnalyzeWeapon(
                        clone.transform,
                        renderer,
                        baked,
                        weaponIndices,
                        bone.position);
                var muzzleWorld =
                    clone.transform.TransformPoint(
                        first.Muzzle);
                var directionWorld =
                    clone.transform.TransformDirection(
                        first.Direction);
                var rotationWorld =
                    Quaternion.LookRotation(
                        directionWorld,
                        clone.transform.up);
                return new MiniFlameAim(
                    boneIndex,
                    bone.InverseTransformPoint(
                        muzzleWorld),
                    Quaternion.Inverse(
                        bone.rotation) *
                    rotationWorld,
                    first.Length,
                    first.Radius,
                    maximumElevation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    baked);
                UnityEngine.Object.DestroyImmediate(
                    clone);
            }
        }

        private static int RequireRightWeaponBoneIndex(
            SkinnedMeshRenderer renderer,
            Mesh appearance,
            IReadOnlyList<int> weaponVertices)
        {
            var allowed =
                new HashSet<string>(
                    new[]
                    {
                        "RightArm",
                        "RightForeArm",
                        "RightHand"
                    },
                    StringComparer.Ordinal);
            var weights =
                new float[
                    renderer.bones.Length];
            var boneWeights =
                appearance.boneWeights;
            foreach (var vertex in
                     weaponVertices)
            {
                AddBoneWeight(
                    weights,
                    boneWeights[vertex].boneIndex0,
                    boneWeights[vertex].weight0);
                AddBoneWeight(
                    weights,
                    boneWeights[vertex].boneIndex1,
                    boneWeights[vertex].weight1);
                AddBoneWeight(
                    weights,
                    boneWeights[vertex].boneIndex2,
                    boneWeights[vertex].weight2);
                AddBoneWeight(
                    weights,
                    boneWeights[vertex].boneIndex3,
                    boneWeights[vertex].weight3);
            }

            var candidates =
                Enumerable.Range(
                        0,
                        renderer.bones.Length)
                    .Where(
                        index =>
                            allowed.Contains(
                                renderer.bones[index]
                                    .name))
                    .OrderByDescending(
                        index =>
                            weights[index])
                    .ToArray();
            if (candidates.Length == 0 ||
                weights[candidates[0]] <=
                0f)
            {
                throw new InvalidOperationException(
                    "The weapon is not weighted to the right arm chain.");
            }

            return candidates[0];
        }

        private static int[] RequireWeaponBarrelIndices(
            SkinnedMeshRenderer renderer,
            Mesh appearance,
            int weaponSubMesh)
        {
            var candidates =
                new List<
                    (int[] Vertices,
                     float Length,
                     float Elongation,
                     float RightWeight)>();
            foreach (var component in
                     WeaponComponents(
                         appearance,
                         weaponSubMesh))
            {
                var vertices =
                    component.Distinct()
                        .ToArray();
                var points =
                    vertices.Select(
                            index =>
                                renderer.transform
                                    .TransformPoint(
                                        appearance.vertices[
                                            index]))
                        .ToArray();
                var center =
                    Average(points);
                var axis =
                    PrincipalAxis(
                        points,
                        center);
                var projections =
                    points.Select(
                            point =>
                                Vector3.Dot(
                                    point - center,
                                    axis))
                        .ToArray();
                var length =
                    projections.Max() -
                    projections.Min();
                var radius =
                    Mathf.Sqrt(
                        points.Average(
                            point =>
                            {
                                var offset =
                                    point - center;
                                var perpendicular =
                                    offset -
                                    axis *
                                    Vector3.Dot(
                                        offset,
                                        axis);
                                return perpendicular
                                    .sqrMagnitude;
                            }));
                var weights =
                    RightArmWeights(
                        renderer,
                        appearance,
                        vertices);
                var rightWeight =
                    weights.RightArm +
                    weights.RightForeArm +
                    weights.RightHand;
                if (rightWeight >=
                        vertices.Length *
                        0.8f &&
                    length /
                    Mathf.Max(
                        radius,
                        0.000001f) >=
                        5f)
                {
                    candidates.Add(
                        (vertices,
                         length,
                         length /
                         Mathf.Max(
                             radius,
                             0.000001f),
                         rightWeight));
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "No right-arm elongated flamethrower barrel component was found.");
            }

            return candidates
                .OrderByDescending(
                    item =>
                        item.Length)
                .First()
                .Vertices;
        }

        private static void AddBoneWeight(
            float[] totals,
            int index,
            float weight)
        {
            if (index >= 0 &&
                index < totals.Length)
            {
                totals[index] +=
                    weight;
            }
        }

        private static WeaponFrame AnalyzeWeapon(
            Transform model,
            SkinnedMeshRenderer renderer,
            Mesh baked,
            IReadOnlyList<int> indices,
            Vector3 handWorld)
        {
            var points =
                indices.Select(
                        index =>
                            model.InverseTransformPoint(
                                renderer.transform
                                    .TransformPoint(
                                        baked.vertices[
                                            index])))
                    .ToArray();
            var center =
                points.Aggregate(
                    Vector3.zero,
                    (sum, point) =>
                        sum + point) /
                points.Length;
            var axis =
                PrincipalAxis(
                    points,
                    center);
            var minimum =
                float.PositiveInfinity;
            var maximum =
                float.NegativeInfinity;
            foreach (var point in points)
            {
                var projection =
                    Vector3.Dot(
                        point - center,
                        axis);
                minimum =
                    Mathf.Min(
                        minimum,
                        projection);
                maximum =
                    Mathf.Max(
                        maximum,
                        projection);
            }

            var length =
                maximum - minimum;
            var threshold =
                Mathf.Max(
                    length * 0.075f,
                    0.0001f);
            var minimumPoints =
                points.Where(
                        point =>
                            Vector3.Dot(
                                point - center,
                                axis) <=
                            minimum +
                            threshold)
                    .ToArray();
            var maximumPoints =
                points.Where(
                        point =>
                            Vector3.Dot(
                                point - center,
                                axis) >=
                            maximum -
                            threshold)
                    .ToArray();
            var minimumCenter =
                Average(
                    minimumPoints);
            var maximumCenter =
                Average(
                    maximumPoints);
            var hand =
                model.InverseTransformPoint(
                    handWorld);
            var muzzleAtMaximum =
                Vector3.Distance(
                    maximumCenter,
                    hand) >=
                Vector3.Distance(
                    minimumCenter,
                    hand);
            var muzzleBandCenter =
                muzzleAtMaximum
                    ? maximumCenter
                    : minimumCenter;
            var muzzlePlane =
                muzzleAtMaximum
                    ? maximum
                    : minimum;
            var muzzle =
                muzzleBandCenter +
                axis *
                (muzzlePlane -
                 Vector3.Dot(
                     muzzleBandCenter - center,
                     axis));
            var direction =
                muzzleAtMaximum
                    ? axis
                    : -axis;
            var radiusSquared =
                points.Average(
                    point =>
                    {
                        var offset =
                            point - center;
                        var perpendicular =
                            offset -
                            direction *
                            Vector3.Dot(
                                offset,
                                direction);
                        return perpendicular
                            .sqrMagnitude;
                    });
            return new WeaponFrame(
                direction,
                muzzle,
                length,
                Mathf.Sqrt(
                    radiusSquared));
        }

        private static Vector3 PrincipalAxis(
            IReadOnlyList<Vector3> points,
            Vector3 center)
        {
            var bounds =
                new Bounds(
                    points[0],
                    Vector3.zero);
            foreach (var point in points)
            {
                bounds.Encapsulate(
                    point);
            }

            var axis =
                bounds.size.x >=
                    bounds.size.y &&
                bounds.size.x >=
                    bounds.size.z
                    ? Vector3.right
                    : bounds.size.y >=
                      bounds.size.z
                        ? Vector3.up
                        : Vector3.forward;
            var xx = 0f;
            var xy = 0f;
            var xz = 0f;
            var yy = 0f;
            var yz = 0f;
            var zz = 0f;
            foreach (var point in points)
            {
                var value =
                    point - center;
                xx += value.x * value.x;
                xy += value.x * value.y;
                xz += value.x * value.z;
                yy += value.y * value.y;
                yz += value.y * value.z;
                zz += value.z * value.z;
            }

            for (var iteration = 0;
                 iteration < 16;
                 iteration++)
            {
                axis =
                    new Vector3(
                        xx * axis.x +
                        xy * axis.y +
                        xz * axis.z,
                        xy * axis.x +
                        yy * axis.y +
                        yz * axis.z,
                        xz * axis.x +
                        yz * axis.y +
                        zz * axis.z)
                    .normalized;
            }

            return axis;
        }

        private static Vector3 Average(
            IReadOnlyList<Vector3> points)
        {
            if (points.Count == 0)
            {
                throw new InvalidOperationException(
                    "A weapon endpoint has no vertices.");
            }

            var result =
                Vector3.zero;
            foreach (var point in points)
            {
                result +=
                    point;
            }

            return result /
                   points.Count;
        }

        private static AnimatorController CreateMiniFlameController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    MiniFlameControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController
                        .CreateAnimatorControllerAtPath(
                            MiniFlameControllerPath);
            }

            var machine =
                controller.layers[0]
                    .stateMachine;
            foreach (var child in
                     machine.states.ToArray())
            {
                machine.RemoveState(
                    child.state);
            }

            var state =
                machine.AddState(
                    MiniFlameStateName);
            state.motion =
                clip;
            state.speed =
                1f;
            machine.defaultState =
                state;
            EditorUtility.SetDirty(
                controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void CreateMiniFlameParticles(
            Transform muzzle,
            float weaponLength,
            float weaponRadius)
        {
            var outer =
                AssetDatabase.LoadAssetAtPath<Material>(
                    MiniFlameOuterMaterialPath) ??
                throw new InvalidOperationException(
                    "The existing outer flame material is missing.");
            var core =
                AssetDatabase.LoadAssetAtPath<Material>(
                    MiniFlameCoreMaterialPath) ??
                throw new InvalidOperationException(
                    "The existing core flame material is missing.");
            var radius =
                Mathf.Max(
                    weaponRadius * 0.52f,
                    0.032f);
            var length =
                Mathf.Max(
                    weaponLength * 1.05f,
                    0.62f);
            CreateMiniFlameParticleLayer(
                muzzle,
                "FlameOuter",
                outer,
                radius,
                length,
                500f,
                6f,
                MiniFlameOuterTailVerticalSpeedFactor);
            CreateMiniFlameParticleLayer(
                muzzle,
                "FlameCore",
                core,
                radius * 0.62f,
                length * 0.86f,
                360f,
                3f,
                MiniFlameCoreTailVerticalSpeedFactor);
        }

        private static void CreateMiniFlameParticleLayer(
            Transform parent,
            string name,
            Material material,
            float radius,
            float length,
            float rate,
            float angle,
            float tailVerticalSpeedFactor)
        {
            var gameObject =
                new GameObject(
                    name,
                    typeof(ParticleSystem));
            gameObject.transform.SetParent(
                parent,
                false);
            var maximumStartSize =
                radius *
                MiniFlameMaximumStartSizeFactor;
            gameObject.transform.localPosition =
                Vector3.forward *
                (maximumStartSize *
                 MiniFlameInitialSizeFactor *
                 0.5f);
            gameObject.transform.localRotation =
                Quaternion.identity;
            gameObject.transform.localScale =
                Vector3.one;
            var particles =
                gameObject.GetComponent<ParticleSystem>();
            var main =
                particles.main;
            main.loop =
                true;
            main.playOnAwake =
                true;
            main.duration =
                1f;
            main.simulationSpace =
                ParticleSystemSimulationSpace.Local;
            main.scalingMode =
                ParticleSystemScalingMode.Hierarchy;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.28f,
                    0.42f);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    length / 0.34f *
                    0.9f,
                    length / 0.34f *
                    1.1f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    radius * 3.8f,
                    maximumStartSize);
            main.startColor =
                Color.white;
            main.maxParticles =
                512;

            var emission =
                particles.emission;
            emission.enabled =
                true;
            emission.rateOverTime =
                rate;
            var shape =
                particles.shape;
            shape.enabled =
                true;
            shape.shapeType =
                ParticleSystemShapeType.Cone;
            shape.angle =
                angle;
            shape.radius =
                radius * 0.22f;
            shape.radiusThickness =
                1f;

            var velocity =
                particles.velocityOverLifetime;
            velocity.enabled =
                true;
            velocity.space =
                ParticleSystemSimulationSpace.Local;
            velocity.x =
                new ParticleSystem.MinMaxCurve(
                    0f,
                    0f);
            velocity.y =
                new ParticleSystem.MinMaxCurve(
                    -length *
                    tailVerticalSpeedFactor,
                    length *
                    tailVerticalSpeedFactor);
            velocity.z =
                new ParticleSystem.MinMaxCurve(
                    0f,
                    0f);

            var size =
                particles.sizeOverLifetime;
            size.enabled =
                true;
            size.separateAxes =
                true;
            var horizontalSize =
                new ParticleSystem.MinMaxCurve(
                    1f,
                    CreateMiniFlameHorizontalSizeCurve());
            size.x =
                horizontalSize;
            size.y =
                new ParticleSystem.MinMaxCurve(
                    1f,
                    new AnimationCurve(
                        new Keyframe(
                            0f,
                            MiniFlameInitialSizeFactor),
                        new Keyframe(
                            0.18f,
                            0.85f),
                        new Keyframe(
                            0.72f,
                            1.75f),
                        new Keyframe(
                            1f,
                            0.35f)));
            size.z =
                horizontalSize;
            var color =
                particles.colorOverLifetime;
            color.enabled =
                true;
            var gradient =
                new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        Color.white,
                        0f),
                    new GradientColorKey(
                        Color.white,
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(
                        1f,
                        0f),
                    new GradientAlphaKey(
                        1f,
                        0.08f),
                    new GradientAlphaKey(
                        0.82f,
                        0.68f),
                    new GradientAlphaKey(
                        0f,
                        1f)
                });
            color.color =
                gradient;
            var particleRenderer =
                gameObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment =
                ParticleSystemRenderSpace.View;
            particleRenderer.velocityScale =
                0f;
            particleRenderer.lengthScale =
                1f;
            particleRenderer.sharedMaterial =
                material;
            particleRenderer.sortMode =
                ParticleSystemSortMode.Distance;
            particles.Play(
                true);
        }

        private static AnimationCurve
            CreateMiniFlameBaseHorizontalSizeCurve()
        {
            return new AnimationCurve(
                new Keyframe(
                    0f,
                    MiniFlameInitialSizeFactor),
                new Keyframe(
                    0.18f,
                    1f),
                new Keyframe(
                    1f,
                    0.18f));
        }

        private static AnimationCurve
            CreateMiniFlameHorizontalSizeCurve()
        {
            var baseline =
                CreateMiniFlameBaseHorizontalSizeCurve();
            const float transitionAge =
                0.45f;
            var transitionMultiplier =
                Mathf.Lerp(
                    MiniFlameOverallHorizontalWidthMultiplier,
                    MiniFlameTailHorizontalWidthMultiplier,
                    (transitionAge - 0.18f) /
                    (MiniFlameTailWidthAge - 0.18f));
            return new AnimationCurve(
                new Keyframe(
                    0f,
                    baseline.Evaluate(0f) *
                    MiniFlameOverallHorizontalWidthMultiplier),
                new Keyframe(
                    0.18f,
                    baseline.Evaluate(0.18f) *
                    MiniFlameOverallHorizontalWidthMultiplier),
                new Keyframe(
                    transitionAge,
                    baseline.Evaluate(transitionAge) *
                    transitionMultiplier),
                new Keyframe(
                    MiniFlameTailWidthAge,
                    baseline.Evaluate(
                        MiniFlameTailWidthAge) *
                    MiniFlameTailHorizontalWidthMultiplier),
                new Keyframe(
                    1f,
                    baseline.Evaluate(1f) *
                    MiniFlameTailHorizontalWidthMultiplier));
        }

        private static MiniFlameParticleMetrics
            RequireMiniFlameParticles(
            Transform muzzle,
            float weaponLength,
            float weaponRadius)
        {
            var particles =
                muzzle.GetComponentsInChildren<ParticleSystem>(
                    true);
            if (particles.Length != 2)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower must have two flame particle layers.");
            }

            var materialPaths =
                particles.Select(
                        item =>
                            AssetDatabase.GetAssetPath(
                                item.GetComponent<
                                    ParticleSystemRenderer>()
                                    .sharedMaterial))
                    .OrderBy(item =>
                        item,
                        StringComparer.Ordinal)
                    .ToArray();
            var expected =
                new[]
                    {
                        MiniFlameCoreMaterialPath,
                        MiniFlameOuterMaterialPath
                    }
                    .OrderBy(item =>
                        item,
                        StringComparer.Ordinal)
                    .ToArray();
            var outerRadius =
                Mathf.Max(
                    weaponRadius * 0.52f,
                    0.032f);
            var outerLength =
                Mathf.Max(
                    weaponLength * 1.05f,
                    0.62f);
            var expectedOffsets =
                new Dictionary<string, float>(
                    StringComparer.Ordinal)
                {
                    {
                        "FlameOuter",
                        outerRadius *
                        MiniFlameMaximumStartSizeFactor *
                        MiniFlameInitialSizeFactor *
                        0.5f
                    },
                    {
                        "FlameCore",
                        outerRadius *
                        0.62f *
                        MiniFlameMaximumStartSizeFactor *
                        MiniFlameInitialSizeFactor *
                        0.5f
                    }
                };
            var expectedVerticalSpeeds =
                new Dictionary<string, float>(
                    StringComparer.Ordinal)
                {
                    {
                        "FlameOuter",
                        outerLength *
                        MiniFlameOuterTailVerticalSpeedFactor
                    },
                    {
                        "FlameCore",
                        outerLength *
                        0.86f *
                        MiniFlameCoreTailVerticalSpeedFactor
                    }
                };
            var baselineHorizontalSize =
                CreateMiniFlameBaseHorizontalSizeCurve();
            if (!materialPaths.SequenceEqual(
                    expected) ||
                particles.Any(
                    item =>
                    {
                        var velocity =
                            item.velocityOverLifetime;
                        if (!expectedOffsets.TryGetValue(
                                item.name,
                                out var expectedOffset) ||
                            !expectedVerticalSpeeds.TryGetValue(
                                item.name,
                                out var expectedVerticalSpeed))
                        {
                            return true;
                        }

                        return
                            !item.main.loop ||
                            !item.main.playOnAwake ||
                            item.main.simulationSpace !=
                            ParticleSystemSimulationSpace
                                .Local ||
                            item.GetComponent<
                                    ParticleSystemRenderer>()
                                .renderMode !=
                            ParticleSystemRenderMode.Billboard ||
                            Vector3.Distance(
                                item.transform.localPosition,
                                Vector3.forward *
                                expectedOffset) >
                            0.00001f ||
                            !velocity.enabled ||
                            velocity.space !=
                            ParticleSystemSimulationSpace
                                .Local ||
                            Mathf.Abs(
                                velocity.x.constantMax) >
                            0.00001f ||
                            Mathf.Abs(
                                velocity.z.constantMax) >
                            0.00001f ||
                            Mathf.Abs(
                                velocity.y.constantMin +
                                expectedVerticalSpeed) >
                            0.00001f ||
                            Mathf.Abs(
                                velocity.y.constantMax -
                                expectedVerticalSpeed) >
                            0.00001f ||
                            !item.sizeOverLifetime.enabled ||
                            !item.sizeOverLifetime
                                .separateAxes ||
                            item.sizeOverLifetime
                                .y.curve.Evaluate(
                                    0.72f) <
                            1.7f;
                    }))
            {
                throw new InvalidOperationException(
                    "The mini flamethrower particle contract differs.");
            }

            var minimumSpreadRatio =
                particles.Min(
                    item =>
                    {
                        var expectedVerticalSpeed =
                            expectedVerticalSpeeds[
                                item.name];
                        var lifetime =
                            item.main.startLifetime
                                .constantMax;
                        var startSize =
                            item.main.startSize
                                .constantMax;
                        var size =
                            item.sizeOverLifetime;
                        const float nearAge =
                            0.12f;
                        const float tailAge =
                            0.72f;
                        var nearHalfHeight =
                            expectedVerticalSpeed *
                            lifetime *
                            nearAge +
                            startSize *
                            size.y.curve.Evaluate(
                                nearAge) *
                            0.5f;
                        var tailHalfHeight =
                            expectedVerticalSpeed *
                            lifetime *
                            tailAge +
                            startSize *
                            size.y.curve.Evaluate(
                                tailAge) *
                            0.5f;
                        return tailHalfHeight /
                               Mathf.Max(
                                   nearHalfHeight,
                                   0.00001f);
                    });
            if (minimumSpreadRatio <
                2.5f)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower tail is not vertically wider than its near field. Ratio=" +
                    minimumSpreadRatio.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            }

            var minimumHorizontalWidthMultiplier =
                particles.Min(
                    item =>
                        Enumerable.Range(
                                0,
                                101)
                            .Min(
                                sample =>
                                {
                                    var age =
                                        sample /
                                        100f;
                                    return item.sizeOverLifetime
                                               .x.curve.Evaluate(
                                                   age) /
                                           Mathf.Max(
                                               baselineHorizontalSize
                                                   .Evaluate(
                                                       age),
                                               0.00001f);
                                }));
            var tailHorizontalWidthMultiplier =
                particles.Min(
                    item =>
                        item.sizeOverLifetime
                            .x.curve.Evaluate(
                                MiniFlameTailWidthAge) /
                        Mathf.Max(
                            baselineHorizontalSize.Evaluate(
                                MiniFlameTailWidthAge),
                            0.00001f));
            const float nearAge =
                0.12f;
            var tailToNearHorizontalWidthRatio =
                particles.Min(
                    item =>
                        item.sizeOverLifetime
                            .x.curve.Evaluate(
                                MiniFlameTailWidthAge) /
                        Mathf.Max(
                            item.sizeOverLifetime
                                .x.curve.Evaluate(
                                    nearAge),
                            0.00001f));
            if (minimumHorizontalWidthMultiplier <
                    MiniFlameOverallHorizontalWidthMultiplier -
                    0.01f ||
                Mathf.Abs(
                    tailHorizontalWidthMultiplier -
                    MiniFlameTailHorizontalWidthMultiplier) >
                0.01f ||
                tailToNearHorizontalWidthRatio <=
                1f)
            {
                throw new InvalidOperationException(
                    "The mini flamethrower horizontal width contract differs. MinimumMultiplier=" +
                    minimumHorizontalWidthMultiplier.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", TailMultiplier=" +
                    tailHorizontalWidthMultiplier.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", TailToNearRatio=" +
                    tailToNearHorizontalWidthRatio.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            }

            return new MiniFlameParticleMetrics(
                minimumSpreadRatio,
                minimumHorizontalWidthMultiplier,
                tailHorizontalWidthMultiplier,
                tailToNearHorizontalWidthRatio);
        }

        private static void WriteMiniFlameReport(
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Vector3 scale,
            float modelY,
            float staticY,
            MiniFlameAim aim,
            MiniFlameParticleMetrics particleMetrics,
            MiniFlameLeftFootGrounding leftFootGrounding)
        {
            var destination =
                Absolute(
                    MiniFlameReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    destination) ??
                throw new InvalidOperationException(
                    "Invalid mini flame report path."));
            var report =
                new StringBuilder();
            report.AppendLine(
                "Pahur Mini Flamethrower Validation");
            report.AppendLine(
                "Result=PASS");
            report.AppendLine(
                "SourceSha256=" +
                SourceMiniFlameSha256);
            report.AppendLine(
                "SourceClip=mixamo.com");
            report.AppendLine(
                "PlaybackClip=" +
                clip.name);
            report.AppendLine(
                "Loop=True");
            report.AppendLine(
                "Vertices=" +
                source.vertexCount);
            report.AppendLine(
                "ShapeSkinBindPosesPreserved=True");
            report.AppendLine(
                "ApprovedAppearanceTransferredByExactVertexIndex=True");
            report.AppendLine(
                "ApprovedMaterialSlots=" +
                appearance.subMeshCount);
            report.AppendLine(
                "ModelScale=" +
                ScaleText(scale));
            report.AppendLine(
                "ModelY=" +
                modelY.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "StaticY=" +
                staticY.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "HorizontalRootMotion=False");
            report.AppendLine(
                "WeaponBoneIndex=" +
                aim.BoneIndex);
            report.AppendLine(
                "MaximumWeaponElevationDegrees=" +
                aim.MaximumElevationDegrees
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            report.AppendLine(
                "MuzzleLocalPosition=" +
                ScaleText(
                    aim.MuzzleLocalPosition));
            report.AppendLine(
                "ParticleSystems=2");
            report.AppendLine(
                "FlameVisibleStartAlignedToMuzzle=True");
            report.AppendLine(
                "FlameTailVerticalSpread=True");
            report.AppendLine(
                "FlameTailVerticalSpreadRatio=" +
                particleMetrics.TailVerticalSpreadRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "FlameMinimumHorizontalWidthMultiplier=" +
                particleMetrics.MinimumHorizontalWidthMultiplier.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "FlameTailHorizontalWidthMultiplier=" +
                particleMetrics.TailHorizontalWidthMultiplier.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "FlameTailToNearHorizontalWidthRatio=" +
                particleMetrics.TailToNearHorizontalWidthRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "LeftFootGrounded=True");
            report.AppendLine(
                "MaximumLeftFootGroundError=" +
                leftFootGrounding.MaximumGroundError.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumLeftSoleHeightRange=" +
                leftFootGrounding.MaximumSoleHeightRange.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumLeftSoleAngleDegrees=" +
                leftFootGrounding.MaximumSoleAngleDegrees.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "ExistingFlameMaterialsReused=True");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private readonly struct WeaponFrame
        {
            public WeaponFrame(
                Vector3 direction,
                Vector3 muzzle,
                float length,
                float radius)
            {
                Direction =
                    direction;
                Muzzle =
                    muzzle;
                Length =
                    length;
                Radius =
                    radius;
            }

            public Vector3 Direction
            {
                get;
            }

            public Vector3 Muzzle
            {
                get;
            }

            public float Length
            {
                get;
            }

            public float Radius
            {
                get;
            }
        }

        private readonly struct RightArmWeightTotals
        {
            public RightArmWeightTotals(
                float rightArm,
                float rightForeArm,
                float rightHand)
            {
                RightArm =
                    rightArm;
                RightForeArm =
                    rightForeArm;
                RightHand =
                    rightHand;
            }

            public float RightArm
            {
                get;
            }

            public float RightForeArm
            {
                get;
            }

            public float RightHand
            {
                get;
            }
        }

        private readonly struct MiniFlameLeftSoleMetrics
        {
            public MiniFlameLeftSoleMetrics(
                Vector3 normal,
                float minimumY,
                float maximumY,
                float angleDegrees)
            {
                Normal =
                    normal;
                MinimumY =
                    minimumY;
                MaximumY =
                    maximumY;
                AngleDegrees =
                    angleDegrees;
            }

            public Vector3 Normal
            {
                get;
            }

            public float MinimumY
            {
                get;
            }

            public float MaximumY
            {
                get;
            }

            public float AngleDegrees
            {
                get;
            }
        }

        private readonly struct MiniFlameSoleSelection
        {
            public MiniFlameSoleSelection(
                int[] allIndices,
                int[] heelIndices,
                int[] toeIndices)
            {
                AllIndices =
                    allIndices;
                HeelIndices =
                    heelIndices;
                ToeIndices =
                    toeIndices;
            }

            public int[] AllIndices
            {
                get;
            }

            public int[] HeelIndices
            {
                get;
            }

            public int[] ToeIndices
            {
                get;
            }

        }

        private readonly struct MiniFlameLeftFootGrounding
        {
            public MiniFlameLeftFootGrounding(
                float maximumGroundError,
                float maximumSoleHeightRange,
                float maximumSoleAngleDegrees)
            {
                MaximumGroundError =
                    maximumGroundError;
                MaximumSoleHeightRange =
                    maximumSoleHeightRange;
                MaximumSoleAngleDegrees =
                    maximumSoleAngleDegrees;
            }

            public float MaximumGroundError
            {
                get;
            }

            public float MaximumSoleHeightRange
            {
                get;
            }

            public float MaximumSoleAngleDegrees
            {
                get;
            }
        }

        private readonly struct MiniFlameParticleMetrics
        {
            public MiniFlameParticleMetrics(
                float tailVerticalSpreadRatio,
                float minimumHorizontalWidthMultiplier,
                float tailHorizontalWidthMultiplier,
                float tailToNearHorizontalWidthRatio)
            {
                TailVerticalSpreadRatio =
                    tailVerticalSpreadRatio;
                MinimumHorizontalWidthMultiplier =
                    minimumHorizontalWidthMultiplier;
                TailHorizontalWidthMultiplier =
                    tailHorizontalWidthMultiplier;
                TailToNearHorizontalWidthRatio =
                    tailToNearHorizontalWidthRatio;
            }

            public float TailVerticalSpreadRatio
            {
                get;
            }

            public float MinimumHorizontalWidthMultiplier
            {
                get;
            }

            public float TailHorizontalWidthMultiplier
            {
                get;
            }

            public float TailToNearHorizontalWidthRatio
            {
                get;
            }
        }

        private readonly struct MiniFlameAim
        {
            public MiniFlameAim(
                int boneIndex,
                Vector3 muzzleLocalPosition,
                Quaternion muzzleLocalRotation,
                float weaponLength,
                float weaponRadius,
                float maximumElevationDegrees)
            {
                BoneIndex =
                    boneIndex;
                MuzzleLocalPosition =
                    muzzleLocalPosition;
                MuzzleLocalRotation =
                    muzzleLocalRotation;
                WeaponLength =
                    weaponLength;
                WeaponRadius =
                    weaponRadius;
                MaximumElevationDegrees =
                    maximumElevationDegrees;
            }

            public int BoneIndex
            {
                get;
            }

            public Vector3 MuzzleLocalPosition
            {
                get;
            }

            public Quaternion MuzzleLocalRotation
            {
                get;
            }

            public float WeaponLength
            {
                get;
            }

            public float WeaponRadius
            {
                get;
            }

            public float MaximumElevationDegrees
            {
                get;
            }
        }
    }
}
