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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionMeleeAttackModelReplacementTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceFbxPath =
            "D:/Bellerophon2/Bellerophon/enemies model/révolution slash.fbx";
        private const string ImportedFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/Models/RevolutionSlash.fbx";
        private const string ApprovedAppearanceFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/ApprovedAppearance/Models/Revolution_ApprovedAppearance.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Revolution/Controllers/Revolution_05_MeleeAttack.controller";
        private const string ValidationFolder =
            "docs/validation/revolution_melee_attack_2026-07-29";
        private const string InspectionPath =
            ValidationFolder + "/Revolution_05_MeleeAttack_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Revolution_05_MeleeAttack_VisualReview.png";
        private const string DeathSourceFbxPath =
            "D:/Bellerophon2/Bellerophon/enemies model/révolution death.fbx";
        private const string DeathImportedFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/Models/RevolutionDeath.fbx";
        private const string DeathControllerPath =
            "Assets/_Project/Art/Enemies/Revolution/Controllers/Revolution_08_Death.controller";
        private const string DeathValidationFolder =
            "docs/validation/revolution_death_2026-07-29";
        private const string DeathInspectionPath =
            DeathValidationFolder + "/Revolution_08_Death_Inspection.txt";
        private const string DeathCapturePath =
            DeathValidationFolder + "/Revolution_08_Death_VisualReview.png";
        private const string DeathEndInspectionPath =
            DeathValidationFolder + "/Revolution_08_Death_ToEnd_Inspection.txt";
        private const string DeathEndCapturePath =
            DeathValidationFolder + "/Revolution_08_Death_ToEnd_VisualReview.png";
        private const string SourceSha256 =
            "599A1203FCD360D6401F6AD168823FF1702DBFAAF050C26D01FF71BCA991A52B";
        private const string DeathSourceSha256 =
            "978D21F60FCEC9E07DF284A6A4349064BA3D7962687CDB8402E3B4F615CBADEA";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string StaticSlotName = "Revolution_01";
        private const string MeleeSlotName = "Revolution_05";
        private const string DeathSlotName = "Revolution_08";
        private const string ReplacementModelName =
            "Revolution_Slash_Model";
        private const string DeathReplacementModelName =
            "Revolution_Death_Model";
        private const string MixamoTakeMarker = "mixamo.com";
        private const string ImportedClipName =
            "Revolution_05_MeleeAttack_Mixamo";
        private const string StateName =
            "Revolution_05_MeleeAttack_Mixamo";
        private const string DeathImportedClipName =
            "Revolution_08_Death_Mixamo";
        private const string DeathStateName =
            "Revolution_08_Death_Mixamo";
        private const int ExpectedTriangleCount = 3945;
        private const int ExpectedBoneCount = 24;
        private const int ExpectedApprovedMaterialCount = 8;
        private const int ReviewLayer = 30;
        private const int PanelSize = 320;

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Melee Attack Model Replacement")]
        public static void ApplyRevolutionMeleeAttack()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution melee replacement requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            RequireHash(SourceFbxPath, SourceSha256);
            CopySourceFbx();
            ConfigureImporter();
            RequireHash(ImportedFbxPath, SourceSha256);

            var importedPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ImportedFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Revolution slash FBX is missing.",
                    ImportedFbxPath);
            var importedClip = RequireImportedMixamoClip();
            var importedAvatar =
                AssetDatabase.LoadAllAssetsAtPath(ImportedFbxPath)
                    .OfType<Avatar>()
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Revolution slash FBX did not produce exactly one Generic Avatar.");
            var importedRenderer =
                RequireMainRenderer(
                    importedPrefab.transform,
                    "imported Revolution slash FBX");
            RequireAuthoredGeometry(importedRenderer);
            var controller = CreateOrUpdateController(importedClip);

            var scene = RequireCurrentScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(placementRoot, StaticSlotName);
            var meleeSlot =
                RequireDirectChild(placementRoot, MeleeSlotName);
            if (meleeSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_05 must contain exactly one model before replacement.");
            }

            var staticModel = staticSlot.GetChild(0);
            var previousModel = meleeSlot.GetChild(0);
            var staticMainRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01 static approved model");
            RequireApprovedStaticAppearance(staticMainRenderer);
            RequireMatchingBoneNames(
                staticMainRenderer,
                importedRenderer,
                "Revolution slash rig");

            var slotPositionBefore = meleeSlot.localPosition;
            var slotRotationBefore = meleeSlot.localRotation;
            var slotScaleBefore = meleeSlot.localScale;
            var otherSlotsBefore =
                CaptureOtherSlotSignatures(placementRoot, meleeSlot);
            var previousLocalPosition = previousModel.localPosition;
            var previousLocalRotation = previousModel.localRotation;
            var previousLocalScale = previousModel.localScale;

            var replacement =
                PrefabUtility.InstantiatePrefab(importedPrefab, scene) as
                    GameObject ??
                throw new InvalidOperationException(
                    "Revolution slash FBX could not be instantiated.");
            replacement.name = ReplacementModelName;
            replacement.transform.SetParent(meleeSlot, false);
            replacement.transform.SetLocalPositionAndRotation(
                previousLocalPosition,
                previousLocalRotation);
            replacement.transform.localScale = previousLocalScale;

            try
            {
                SynchronizeAppearance(
                    staticModel,
                    replacement.transform);
                var replacementMainRenderer =
                    RequireMainRenderer(
                        replacement.transform,
                        "Revolution_05 slash model");
                RequireMatchingBoneNames(
                    importedRenderer,
                    replacementMainRenderer,
                    "instantiated Revolution slash rig");
                RequireAppearanceSynchronized(
                    staticModel,
                    replacement.transform);

                var animator = replacement.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = replacement.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.avatar = importedAvatar;
                animator.applyRootMotion = false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    animator);
                RequireAnimator(
                    animator,
                    controller,
                    importedClip);

                if (replacement.transform.localPosition !=
                        previousLocalPosition ||
                    replacement.transform.localRotation !=
                        previousLocalRotation ||
                    replacement.transform.localScale !=
                        previousLocalScale)
                {
                    throw new InvalidOperationException(
                        "The replacement model did not preserve the previous Revolution_05 local transform.");
                }
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(
                previousModel.gameObject);
            if (meleeSlot.childCount != 1 ||
                meleeSlot.GetChild(0) != replacement.transform)
            {
                throw new InvalidOperationException(
                    "Revolution_05 replacement did not leave exactly one slash model.");
            }

            RequireSlotTransformUnchanged(
                meleeSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                meleeSlot,
                otherSlotsBefore);
            RequirePrefabSource(replacement.transform);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            CaptureVisualReview(
                staticSlot,
                meleeSlot,
                replacement,
                importedClip);
            RevertSampledEulerHintOverrides(
                replacement.transform);
            WriteInspection(
                staticModel,
                replacement.transform,
                importedClip,
                controller);

            RequireSlotTransformUnchanged(
                meleeSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                meleeSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(meleeSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Revolution_05 melee replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            Selection.activeGameObject = meleeSlot.gameObject;
            Debug.Log(
                "RevolutionMeleeAttackApplied" +
                ", Slot=" + MeleeSlotName +
                ", Source=" + ImportedFbxPath +
                ", Clip=" + importedClip.name +
                ", ClipLength=" +
                importedClip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", Loop=True" +
                ", RootMotion=False" +
                ", StaticAppearanceDirectReference=True" +
                ", NewAppearanceAssets=False" +
                ", OtherSlotsUnchanged=True" +
                ", Capture=" + CapturePath + ".");

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Death Model Replacement")]
        public static void ApplyRevolutionDeath()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution death replacement requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            RequireHash(
                DeathSourceFbxPath,
                DeathSourceSha256);
            CopyDeathSourceFbx();
            ConfigureDeathImporter();
            RequireHash(
                DeathImportedFbxPath,
                DeathSourceSha256);

            var importedPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    DeathImportedFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Revolution death FBX is missing.",
                    DeathImportedFbxPath);
            var importedClip =
                RequireImportedDeathMixamoClip();
            var importedAvatar =
                AssetDatabase.LoadAllAssetsAtPath(
                        DeathImportedFbxPath)
                    .OfType<Avatar>()
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Revolution death FBX did not produce exactly one Generic Avatar.");
            var importedRenderer =
                RequireMainRenderer(
                    importedPrefab.transform,
                    "imported Revolution death FBX");
            RequireAuthoredGeometry(importedRenderer);
            var controller =
                CreateOrUpdateDeathController(
                    importedClip);

            var scene = RequireCurrentScene();
            var placementRoot =
                RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(
                    placementRoot,
                    StaticSlotName);
            var deathSlot =
                RequireDirectChild(
                    placementRoot,
                    DeathSlotName);
            if (deathSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_08 must contain exactly one model before replacement.");
            }

            var staticModel = staticSlot.GetChild(0);
            var previousModel = deathSlot.GetChild(0);
            var staticMainRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01 static approved model");
            RequireApprovedStaticAppearance(
                staticMainRenderer);
            RequireMatchingBoneNames(
                staticMainRenderer,
                importedRenderer,
                "Revolution death rig");

            var slotPositionBefore =
                deathSlot.localPosition;
            var slotRotationBefore =
                deathSlot.localRotation;
            var slotScaleBefore =
                deathSlot.localScale;
            var otherSlotsBefore =
                CaptureOtherSlotSignatures(
                    placementRoot,
                    deathSlot);
            var previousLocalPosition =
                previousModel.localPosition;
            var previousLocalRotation =
                previousModel.localRotation;
            var previousLocalScale =
                previousModel.localScale;

            var replacement =
                PrefabUtility.InstantiatePrefab(
                    importedPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "Revolution death FBX could not be instantiated.");
            replacement.name =
                DeathReplacementModelName;
            replacement.transform.SetParent(
                deathSlot,
                false);
            replacement.transform
                .SetLocalPositionAndRotation(
                    previousLocalPosition,
                    previousLocalRotation);
            replacement.transform.localScale =
                previousLocalScale;

            try
            {
                SynchronizeAppearance(
                    staticModel,
                    replacement.transform);
                var replacementMainRenderer =
                    RequireMainRenderer(
                        replacement.transform,
                        "Revolution_08 death model");
                RequireMatchingBoneNames(
                    importedRenderer,
                    replacementMainRenderer,
                    "instantiated Revolution death rig");
                RequireAppearanceSynchronized(
                    staticModel,
                    replacement.transform);

                var animator =
                    replacement.GetComponent<Animator>();
                if (animator == null)
                {
                    animator =
                        replacement.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController =
                    controller;
                animator.avatar = importedAvatar;
                animator.applyRootMotion = false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode =
                    AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        animator);
                RequireAnimator(
                    animator,
                    controller,
                    importedClip);

                if (replacement.transform.localPosition !=
                        previousLocalPosition ||
                    replacement.transform.localRotation !=
                        previousLocalRotation ||
                    replacement.transform.localScale !=
                        previousLocalScale)
                {
                    throw new InvalidOperationException(
                        "The replacement model did not preserve the previous Revolution_08 local transform.");
                }
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(
                    replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(
                previousModel.gameObject);
            if (deathSlot.childCount != 1 ||
                deathSlot.GetChild(0) !=
                replacement.transform)
            {
                throw new InvalidOperationException(
                    "Revolution_08 replacement did not leave exactly one death model.");
            }

            RequireSlotTransformUnchanged(
                deathSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                deathSlot,
                otherSlotsBefore);
            RequireDeathPrefabSource(
                replacement.transform);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            Directory.CreateDirectory(
                Absolute(DeathValidationFolder));
            CaptureVisualReview(
                staticSlot,
                deathSlot,
                replacement,
                importedClip,
                DeathCapturePath);
            RevertSampledEulerHintOverrides(
                replacement.transform);
            WriteDeathInspection(
                staticModel,
                replacement.transform,
                importedClip,
                controller);

            RequireSlotTransformUnchanged(
                deathSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                deathSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(
                deathSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Revolution_08 death replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(
                DeathSourceFbxPath,
                DeathSourceSha256);
            RequireHash(
                DeathImportedFbxPath,
                DeathSourceSha256);
            Selection.activeGameObject =
                deathSlot.gameObject;
            Debug.Log(
                "RevolutionDeathApplied" +
                ", Slot=" + DeathSlotName +
                ", Source=" + DeathImportedFbxPath +
                ", Clip=" + importedClip.name +
                ", ClipLength=" +
                importedClip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", Loop=True" +
                ", LoopPose=False" +
                ", RootMotion=False" +
                ", StaticAppearanceDirectReference=True" +
                ", NewAppearanceAssets=False" +
                ", OtherSlotsUnchanged=True" +
                ", Capture=" +
                DeathCapturePath + ".");

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication
                        .isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Review Death To Exact End")]
        public static void ReviewRevolutionDeathToEnd()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution death end review requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            var scene = RequireCurrentScene();
            var placementRoot =
                RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(
                    placementRoot,
                    StaticSlotName);
            var deathSlot =
                RequireDirectChild(
                    placementRoot,
                    DeathSlotName);
            if (staticSlot.childCount != 1 ||
                deathSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_01 and Revolution_08 must each contain exactly one model for the death end review.");
            }

            var staticModel = staticSlot.GetChild(0);
            var deathModel = deathSlot.GetChild(0);
            var clip = RequireImportedDeathMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    DeathControllerPath) ??
                throw new FileNotFoundException(
                    "Revolution death controller is missing.",
                    DeathControllerPath);
            var animator =
                deathModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Revolution_08 death model is missing its Animator.");

            RequireDeathPrefabSource(deathModel);
            RequireAppearanceSynchronized(
                staticModel,
                deathModel);
            RequireAnimator(
                animator,
                controller,
                clip);

            Directory.CreateDirectory(
                Absolute(DeathValidationFolder));
            CaptureVisualReview(
                staticSlot,
                deathSlot,
                deathModel.gameObject,
                clip,
                DeathEndCapturePath);
            WriteDeathEndInspection(clip);

            RequireDeathPrefabSource(deathModel);
            RequireAppearanceSynchronized(
                staticModel,
                deathModel);
            RequireAnimator(
                animator,
                controller,
                clip);

            Selection.activeGameObject =
                deathSlot.gameObject;
            Debug.Log(
                "RevolutionDeathReviewedToExactEnd" +
                ", Slot=" + DeathSlotName +
                ", Clip=" + clip.name +
                ", ClipLength=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", FinalNormalizedTime=1" +
                ", RuntimeClipModified=False" +
                ", RuntimeLoop=True" +
                ", RuntimeLoopPose=False" +
                ", Capture=" +
                DeathEndCapturePath + ".");

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication
                        .isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Play Current Death Loop")]
        public static void PlayCurrentRevolutionDeathLoop()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode &&
                !EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Unity is already changing Play Mode state.");
            }

            var scene = RequireCurrentScene();
            var placementRoot =
                RequirePlacementRoot(scene);
            var deathSlot =
                RequireDirectChild(
                    placementRoot,
                    DeathSlotName);
            if (deathSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_08 must contain exactly one death model.");
            }

            var deathModel = deathSlot.GetChild(0);
            var clip = RequireImportedDeathMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    DeathControllerPath) ??
                throw new FileNotFoundException(
                    "Revolution death controller is missing.",
                    DeathControllerPath);
            var animator =
                deathModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Revolution_08 death model is missing its Animator.");
            RequireDeathPrefabSource(deathModel);
            RequireAnimator(
                animator,
                controller,
                clip);

            Selection.activeGameObject =
                deathSlot.gameObject;
            Debug.Log(
                "RevolutionDeathLoopReady" +
                ", Slot=" + DeathSlotName +
                ", Clip=" + clip.name +
                ", FullCycleLength=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", Loop=True" +
                ", LoopPose=False" +
                ", RootMotion=False.");

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (!EditorApplication
                            .isPlayingOrWillChangePlaymode)
                    {
                        EditorApplication.EnterPlaymode();
                    }
                };
            }
        }

        private static void CopySourceFbx()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Absolute(ImportedFbxPath)) ??
                throw new InvalidOperationException(
                    "Revolution model directory is invalid."));
            if (!File.Exists(Absolute(ImportedFbxPath)) ||
                !string.Equals(
                    Sha256(Absolute(ImportedFbxPath)),
                    SourceSha256,
                    StringComparison.Ordinal))
            {
                File.Copy(
                    SourceFbxPath,
                    Absolute(ImportedFbxPath),
                    true);
            }

            AssetDatabase.ImportAsset(
                ImportedFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImporter()
        {
            var importer =
                AssetImporter.GetAtPath(ImportedFbxPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution slash ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType =
                ModelImporterAnimationType.Generic;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.None;
            importer.animationCompression =
                ModelImporterAnimationCompression.Off;
            importer.animationWrapMode = WrapMode.Loop;
            importer.SaveAndReimport();

            importer =
                AssetImporter.GetAtPath(ImportedFbxPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution slash ModelImporter was lost after its initial import.");
            var sourceClips = importer.defaultClipAnimations;
            var mixamoCandidates =
                sourceClips.Where(candidate =>
                        string.Equals(
                            candidate.name,
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            candidate.takeName,
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) ||
                        candidate.name.IndexOf(
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        candidate.takeName.IndexOf(
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            if (mixamoCandidates.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Revolution slash FBX must expose exactly one Mixamo take. Candidates=" +
                    string.Join(
                        "|",
                        mixamoCandidates.Select(candidate =>
                            candidate.name + "[" +
                            candidate.takeName + "]")) +
                    ", Available=" +
                    string.Join(
                        "|",
                        sourceClips.Select(candidate =>
                            candidate.name + "[" +
                            candidate.takeName + "]")));
            }

            var mixamoClip = mixamoCandidates[0];
            mixamoClip.name = ImportedClipName;
            mixamoClip.wrapMode = WrapMode.Loop;
            mixamoClip.loopTime = true;
            mixamoClip.loopPose = true;
            importer.clipAnimations = new[] { mixamoClip };
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                ImportedFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static AnimationClip RequireImportedMixamoClip()
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(ImportedFbxPath)
                    .OfType<AnimationClip>()
                    .Where(candidate =>
                        !candidate.name.StartsWith(
                            "__preview__",
                            StringComparison.Ordinal))
                    .ToArray();
            if (clips.Length != 1 ||
                clips[0].name != ImportedClipName)
            {
                throw new InvalidOperationException(
                    "Revolution slash FBX did not import exactly one selected Mixamo clip. Imported=" +
                    string.Join(
                        "|",
                        clips.Select(candidate => candidate.name)));
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime ||
                !clips[0].isLooping ||
                clips[0].empty)
            {
                throw new InvalidOperationException(
                    "The selected Revolution Mixamo clip is empty or is not configured to loop.");
            }

            return clips[0];
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController
                        .CreateAnimatorControllerAtPath(
                            ControllerPath);
            }

            controller.parameters =
                Array.Empty<AnimatorControllerParameter>();
            var stateMachine =
                controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (var child in
                     stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(
                    child.stateMachine);
            }

            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void CopyDeathSourceFbx()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    Absolute(DeathImportedFbxPath)) ??
                throw new InvalidOperationException(
                    "Revolution death model directory is invalid."));
            if (!File.Exists(
                    Absolute(DeathImportedFbxPath)) ||
                !string.Equals(
                    Sha256(
                        Absolute(DeathImportedFbxPath)),
                    DeathSourceSha256,
                    StringComparison.Ordinal))
            {
                File.Copy(
                    DeathSourceFbxPath,
                    Absolute(DeathImportedFbxPath),
                    true);
            }

            AssetDatabase.ImportAsset(
                DeathImportedFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureDeathImporter()
        {
            var importer =
                AssetImporter.GetAtPath(
                    DeathImportedFbxPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution death ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType =
                ModelImporterAnimationType.Generic;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.None;
            importer.animationCompression =
                ModelImporterAnimationCompression.Off;
            importer.animationWrapMode = WrapMode.Loop;
            importer.SaveAndReimport();

            importer =
                AssetImporter.GetAtPath(
                    DeathImportedFbxPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution death ModelImporter was lost after its initial import.");
            var sourceClips =
                importer.defaultClipAnimations;
            var mixamoCandidates =
                sourceClips.Where(candidate =>
                        string.Equals(
                            candidate.name,
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            candidate.takeName,
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) ||
                        candidate.name.IndexOf(
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) >=
                        0 ||
                        candidate.takeName.IndexOf(
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) >=
                        0)
                    .ToArray();
            if (mixamoCandidates.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Revolution death FBX must expose exactly one Mixamo take. Candidates=" +
                    string.Join(
                        "|",
                        mixamoCandidates.Select(candidate =>
                            candidate.name + "[" +
                            candidate.takeName + "]")) +
                    ", Available=" +
                    string.Join(
                        "|",
                        sourceClips.Select(candidate =>
                            candidate.name + "[" +
                            candidate.takeName + "]")));
            }

            var mixamoClip = mixamoCandidates[0];
            mixamoClip.name =
                DeathImportedClipName;
            mixamoClip.wrapMode = WrapMode.Loop;
            mixamoClip.loopTime = true;
            mixamoClip.loopPose = false;
            importer.clipAnimations =
                new[] { mixamoClip };
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                DeathImportedFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static AnimationClip
            RequireImportedDeathMixamoClip()
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(
                        DeathImportedFbxPath)
                    .OfType<AnimationClip>()
                    .Where(candidate =>
                        !candidate.name.StartsWith(
                            "__preview__",
                            StringComparison.Ordinal))
                    .ToArray();
            if (clips.Length != 1 ||
                clips[0].name !=
                DeathImportedClipName)
            {
                throw new InvalidOperationException(
                    "Revolution death FBX did not import exactly one selected Mixamo clip. Imported=" +
                    string.Join(
                        "|",
                        clips.Select(candidate =>
                            candidate.name)));
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(
                    clips[0]);
            if (!settings.loopTime ||
                settings.loopBlend ||
                !clips[0].isLooping ||
                clips[0].empty)
            {
                throw new InvalidOperationException(
                    "The selected Revolution death Mixamo clip is empty, does not loop, or still applies Loop Pose blending.");
            }

            return clips[0];
        }

        private static AnimatorController
            CreateOrUpdateDeathController(
                AnimationClip clip)
        {
            var controller =
                AssetDatabase
                    .LoadAssetAtPath<AnimatorController>(
                        DeathControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController
                        .CreateAnimatorControllerAtPath(
                            DeathControllerPath);
            }

            controller.parameters =
                Array.Empty<AnimatorControllerParameter>();
            var stateMachine =
                controller.layers[0].stateMachine;
            foreach (var child in
                     stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (var child in
                     stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(
                    child.stateMachine);
            }

            var state =
                stateMachine.AddState(DeathStateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            }

            return scene;
        }

        private static Transform RequirePlacementRoot(
            Scene scene)
        {
            var matches =
                scene.GetRootGameObjects()
                    .Where(root =>
                        root.name == PlacementRootName)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Revolution placement root is missing or ambiguous.");
            }

            return matches[0].transform;
        }

        private static Transform RequireDirectChild(
            Transform parent,
            string name)
        {
            var matches =
                parent.Cast<Transform>()
                    .Where(child => child.name == name)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    name +
                    " is missing or ambiguous below " +
                    parent.name + ".");
            }

            return matches[0];
        }

        private static SkinnedMeshRenderer RequireMainRenderer(
            Transform root,
            string context)
        {
            var renderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    context +
                    " must contain exactly one SkinnedMeshRenderer. Found=" +
                    renderers.Length + ".");
            }

            return renderers[0];
        }

        private static void RequireAuthoredGeometry(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "The imported Revolution slash renderer has no mesh.");
            if (mesh.vertexCount <= 0 ||
                TriangleCount(mesh) != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "The supplied Revolution slash geometry or rig differs from the current Revolution model. Vertices=" +
                    mesh.vertexCount +
                    ", Triangles=" + TriangleCount(mesh) +
                    ", Bones=" + renderer.bones.Length + ".");
            }
        }

        private static void RequireApprovedStaticAppearance(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Revolution_01 approved renderer has no mesh.");
            if (AssetDatabase.GetAssetPath(mesh) !=
                    ApprovedAppearanceFbxPath ||
                TriangleCount(mesh) != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount ||
                mesh.subMeshCount !=
                    ExpectedApprovedMaterialCount ||
                renderer.sharedMaterials.Length !=
                    ExpectedApprovedMaterialCount)
            {
                throw new InvalidOperationException(
                    "Revolution_01 does not expose the expected approved appearance contract. Vertices=" +
                    mesh.vertexCount +
                    ", Triangles=" + TriangleCount(mesh) +
                    ", Bones=" + renderer.bones.Length +
                    ", SubMeshes=" + mesh.subMeshCount +
                    ", Materials=" +
                    renderer.sharedMaterials.Length + ".");
            }
        }

        private static void SynchronizeAppearance(
            Transform staticModel,
            Transform targetModel)
        {
            var staticRenderers =
                RendererMap(staticModel);
            var targetRenderers =
                RendererMap(targetModel);
            if (!staticRenderers.Keys.SequenceEqual(
                    targetRenderers.Keys,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Revolution slash renderer hierarchy does not match Revolution_01. Static=" +
                    string.Join("|", staticRenderers.Keys) +
                    ", Slash=" +
                    string.Join("|", targetRenderers.Keys));
            }

            foreach (var pair in staticRenderers)
            {
                var source = pair.Value;
                var target = targetRenderers[pair.Key];
                if (source.GetType() != target.GetType())
                {
                    throw new InvalidOperationException(
                        "Renderer type differs at " + pair.Key + ".");
                }

                target.sharedMaterials =
                    source.sharedMaterials.ToArray();
                target.enabled = source.enabled;
                target.shadowCastingMode =
                    source.shadowCastingMode;
                target.receiveShadows =
                    source.receiveShadows;

                if (source is SkinnedMeshRenderer sourceSkinned &&
                    target is SkinnedMeshRenderer targetSkinned)
                {
                    RequireMatchingBoneNames(
                        sourceSkinned,
                        targetSkinned,
                        pair.Key);
                    targetSkinned.sharedMesh =
                        sourceSkinned.sharedMesh;
                    targetSkinned.updateWhenOffscreen =
                        sourceSkinned.updateWhenOffscreen;
                }
                else
                {
                    var sourceFilter =
                        source.GetComponent<MeshFilter>() ??
                        throw new InvalidOperationException(
                            "Static renderer has no MeshFilter at " +
                            pair.Key + ".");
                    var targetFilter =
                        target.GetComponent<MeshFilter>() ??
                        throw new InvalidOperationException(
                            "Slash renderer has no MeshFilter at " +
                            pair.Key + ".");
                    targetFilter.sharedMesh =
                        sourceFilter.sharedMesh;
                    EditorUtility.SetDirty(targetFilter);
                    PrefabUtility
                        .RecordPrefabInstancePropertyModifications(
                            targetFilter);
                }

                EditorUtility.SetDirty(target);
                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        target);
            }
        }

        private static SortedDictionary<string, Renderer>
            RendererMap(Transform root)
        {
            var result =
                new SortedDictionary<string, Renderer>(
                    StringComparer.Ordinal);
            foreach (var renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                var path =
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        root);
                var key =
                    path + "|" + renderer.GetType().Name;
                if (result.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        "Duplicate renderer path: " + key);
                }

                result.Add(key, renderer);
            }

            return result;
        }

        private static void RequireAppearanceSynchronized(
            Transform staticModel,
            Transform targetModel)
        {
            var staticRenderers =
                RendererMap(staticModel);
            var targetRenderers =
                RendererMap(targetModel);
            if (!staticRenderers.Keys.SequenceEqual(
                    targetRenderers.Keys,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Static and melee renderer paths differ after appearance synchronization.");
            }

            foreach (var pair in staticRenderers)
            {
                var source = pair.Value;
                var target = targetRenderers[pair.Key];
                if (!source.sharedMaterials.SequenceEqual(
                        target.sharedMaterials))
                {
                    throw new InvalidOperationException(
                        "Revolution_05 materials differ from Revolution_01 at " +
                        pair.Key + ".");
                }

                if (source is SkinnedMeshRenderer sourceSkinned &&
                    target is SkinnedMeshRenderer targetSkinned)
                {
                    if (sourceSkinned.sharedMesh !=
                        targetSkinned.sharedMesh)
                    {
                        throw new InvalidOperationException(
                            "Revolution_05 skinned mesh differs from Revolution_01 at " +
                            pair.Key + ".");
                    }
                }
                else
                {
                    var sourceMesh =
                        source.GetComponent<MeshFilter>()
                            ?.sharedMesh;
                    var targetMesh =
                        target.GetComponent<MeshFilter>()
                            ?.sharedMesh;
                    if (sourceMesh != targetMesh)
                    {
                        throw new InvalidOperationException(
                            "Revolution_05 static mesh differs from Revolution_01 at " +
                            pair.Key + ".");
                    }
                }
            }
        }

        private static void RequireMatchingBoneNames(
            SkinnedMeshRenderer reference,
            SkinnedMeshRenderer target,
            string context)
        {
            var referenceNames =
                reference.bones
                    .Select(bone =>
                        bone != null ? bone.name : "<null>")
                    .ToArray();
            var targetNames =
                target.bones
                    .Select(bone =>
                        bone != null ? bone.name : "<null>")
                    .ToArray();
            if (!referenceNames.SequenceEqual(
                    targetNames,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    context +
                    " bone names or order differ from Revolution_01.");
            }
        }

        private static void RequireAnimator(
            Animator animator,
            RuntimeAnimatorController controller,
            AnimationClip clip)
        {
            if (animator.runtimeAnimatorController != controller ||
                animator.avatar == null ||
                animator.applyRootMotion ||
                !animator.enabled)
            {
                throw new InvalidOperationException(
                    "Revolution_05 Animator contract differs.");
            }

            var clips =
                animator.runtimeAnimatorController.animationClips;
            if (clips.Length != 1 ||
                clips[0] != clip)
            {
                throw new InvalidOperationException(
                    "Revolution_05 controller must reference only the selected Mixamo slash clip.");
            }
        }

        private static void RequirePrefabSource(
            Transform replacement)
        {
            var source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    replacement.gameObject);
            var path =
                source != null
                    ? AssetDatabase.GetAssetPath(source)
                    : string.Empty;
            if (path != ImportedFbxPath)
            {
                throw new InvalidOperationException(
                    "Revolution_05 is not a direct instance of the supplied slash FBX. Source=" +
                    path);
            }
        }

        private static void RequireDeathPrefabSource(
            Transform replacement)
        {
            var source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    replacement.gameObject);
            var path =
                source != null
                    ? AssetDatabase.GetAssetPath(source)
                    : string.Empty;
            if (path != DeathImportedFbxPath)
            {
                throw new InvalidOperationException(
                    "Revolution_08 is not a direct instance of the supplied death FBX. Source=" +
                    path);
            }
        }

        private static SortedDictionary<string, string>
            CaptureOtherSlotSignatures(
                Transform placementRoot,
                Transform excludedSlot)
        {
            return new SortedDictionary<string, string>(
                placementRoot.Cast<Transform>()
                    .Where(slot => slot != excludedSlot)
                    .ToDictionary(
                        slot => slot.name,
                        SlotSignature,
                        StringComparer.Ordinal),
                StringComparer.Ordinal);
        }

        private static void RequireOtherSlotsUnchanged(
            Transform placementRoot,
            Transform excludedSlot,
            IReadOnlyDictionary<string, string> before)
        {
            var after =
                CaptureOtherSlotSignatures(
                    placementRoot,
                    excludedSlot);
            if (before.Count != after.Count ||
                before.Any(pair =>
                    !after.TryGetValue(
                        pair.Key,
                        out var value) ||
                    value != pair.Value))
            {
                throw new InvalidOperationException(
                    "A Revolution slot outside Revolution_05 changed.");
            }
        }

        private static string SlotSignature(Transform slot)
        {
            var builder = new StringBuilder();
            foreach (var item in
                     slot.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        item,
                        slot));
                builder.Append('|');
                builder.Append(item.localPosition.ToString("R"));
                builder.Append('|');
                builder.Append(item.localRotation.ToString("R"));
                builder.Append('|');
                builder.Append(item.localScale.ToString("R"));
                builder.Append('|');
                builder.Append(item.gameObject.activeSelf);
                builder.AppendLine();
            }

            foreach (var renderer in
                     slot.GetComponentsInChildren<Renderer>(true))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        slot));
                builder.Append('|');
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        renderer is SkinnedMeshRenderer skinned
                            ? skinned.sharedMesh
                            : renderer.GetComponent<MeshFilter>()
                                ?.sharedMesh));
                builder.Append('|');
                builder.Append(
                    string.Join(
                        ",",
                        renderer.sharedMaterials.Select(
                            AssetDatabase.GetAssetPath)));
                builder.AppendLine();
            }

            foreach (var animator in
                     slot.GetComponentsInChildren<Animator>(true))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        animator.transform,
                        slot));
                builder.Append('|');
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController));
                builder.Append('|');
                builder.Append(animator.applyRootMotion);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void RequireSlotTransformUnchanged(
            Transform slot,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (slot.localPosition != position ||
                slot.localRotation != rotation ||
                slot.localScale != scale)
            {
                throw new InvalidOperationException(
                    "Revolution_05 slot transform changed.");
            }
        }

        private static void CaptureVisualReview(
            Transform staticSlot,
            Transform meleeSlot,
            GameObject meleeModel,
            AnimationClip clip,
            string capturePath = CapturePath)
        {
            var staticStates =
                CaptureLayerStates(staticSlot);
            var meleeStates =
                CaptureLayerStates(meleeSlot);
            var cameraObject =
                new GameObject(
                    "Revolution_Melee_ReviewCamera",
                    typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var keyLightObject =
                new GameObject(
                    "Revolution_Melee_ReviewKey",
                    typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var fillLightObject =
                new GameObject(
                    "Revolution_Melee_ReviewFill",
                    typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var panels = new List<Texture2D>();
            var animationModeStarted = false;
            AnimationClip reviewClip = null;

            try
            {
                SetLayerRecursively(staticSlot, ReviewLayer);
                SetLayerRecursively(meleeSlot, ReviewLayer);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.09f, 0.105f, 0.13f, 1f);
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.allowHDR = true;
                camera.allowMSAA = true;
                camera.cullingMask = 1 << ReviewLayer;

                ConfigureReviewLight(
                    keyLightObject.GetComponent<Light>(),
                    Quaternion.Euler(35f, -35f, 0f),
                    3.5f);
                ConfigureReviewLight(
                    fillLightObject.GetComponent<Light>(),
                    Quaternion.Euler(20f, 145f, 0f),
                    1.8f);

                var normalizedTimes =
                    new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                reviewClip =
                    UnityEngine.Object.Instantiate(clip);
                reviewClip.name =
                    clip.name + "_ExactEndReview";
                reviewClip.hideFlags =
                    HideFlags.HideAndDontSave;
                reviewClip.wrapMode =
                    WrapMode.ClampForever;
                var reviewClipSettings =
                    AnimationUtility.GetAnimationClipSettings(
                        reviewClip);
                reviewClipSettings.loopTime = false;
                reviewClipSettings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(
                    reviewClip,
                    reviewClipSettings);
                var staticReviewBounds =
                    BakedWorldBounds(
                        RequireMainRenderer(
                            staticSlot,
                            "Revolution_01 fixed review framing"));
                var staticReviewLocalCenter =
                    staticSlot.InverseTransformPoint(
                        staticReviewBounds.center);
                var staticReviewRadius =
                    Mathf.Max(
                        staticReviewBounds.extents.magnitude,
                        staticReviewBounds.extents.y) *
                    1.8f;
                var staticScaleMagnitude =
                    Mathf.Max(
                        Mathf.Abs(staticSlot.lossyScale.x),
                        Mathf.Abs(staticSlot.lossyScale.y),
                        Mathf.Abs(staticSlot.lossyScale.z));
                var animatedScaleMagnitude =
                    Mathf.Max(
                        Mathf.Abs(meleeSlot.lossyScale.x),
                        Mathf.Abs(meleeSlot.lossyScale.y),
                        Mathf.Abs(meleeSlot.lossyScale.z));
                var animatedReviewRadius =
                    staticReviewRadius *
                    animatedScaleMagnitude /
                    staticScaleMagnitude;
                var animatedReviewCenter =
                    meleeSlot.TransformPoint(
                        staticReviewLocalCenter);
                var staticFront =
                    RenderPanel(
                        camera,
                        staticSlot,
                        0f,
                        staticReviewBounds.center,
                        staticReviewRadius);
                var staticOblique =
                    RenderPanel(
                        camera,
                        staticSlot,
                        0.58f,
                        staticReviewBounds.center,
                        staticReviewRadius);
                for (var index = 0;
                     index < normalizedTimes.Length;
                     index++)
                {
                    panels.Add(CloneTexture(staticFront));
                }

                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                foreach (var normalizedTime in normalizedTimes)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        meleeModel,
                        reviewClip,
                        clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    panels.Add(
                        RenderPanel(
                            camera,
                            meleeSlot,
                            0f,
                            animatedReviewCenter,
                            animatedReviewRadius));
                }

                for (var index = 0;
                     index < normalizedTimes.Length;
                     index++)
                {
                    panels.Add(CloneTexture(staticOblique));
                }

                foreach (var normalizedTime in normalizedTimes)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        meleeModel,
                        reviewClip,
                        clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    panels.Add(
                        RenderPanel(
                            camera,
                            meleeSlot,
                            0.58f,
                            animatedReviewCenter,
                            animatedReviewRadius));
                }

                var sheet =
                    ComposeSheet(
                        panels,
                        normalizedTimes.Length,
                        4);
                try
                {
                    File.WriteAllBytes(
                        Absolute(capturePath),
                        sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                }

                UnityEngine.Object.DestroyImmediate(staticFront);
                UnityEngine.Object.DestroyImmediate(staticOblique);
            }
            finally
            {
                if (animationModeStarted)
                {
                    AnimationMode.StopAnimationMode();
                }

                RestoreLayerStates(staticStates);
                RestoreLayerStates(meleeStates);
                if (reviewClip != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        reviewClip);
                }

                foreach (var panel in panels)
                {
                    if (panel != null)
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static void ConfigureReviewLight(
            Light light,
            Quaternion rotation,
            float intensity)
        {
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            light.transform.rotation = rotation;
            light.cullingMask = 1 << ReviewLayer;
        }

        private static Texture2D RenderPanel(
            Camera camera,
            Transform slot,
            float oblique,
            Vector3? fixedCenter = null,
            float fixedRadius = 0f)
        {
            var mainRenderer =
                RequireMainRenderer(
                    slot,
                    slot.name + " direct visual review");
            if (!mainRenderer.enabled)
            {
                throw new InvalidOperationException(
                    slot.name +
                    " main renderer is disabled for direct review.");
            }

            var bounds =
                fixedCenter.HasValue
                    ? default
                    : BakedWorldBounds(mainRenderer);

            var viewDirection =
                (slot.forward +
                 slot.right * oblique).normalized;
            var radius =
                fixedCenter.HasValue
                    ? fixedRadius
                    : Mathf.Max(
                        bounds.extents.magnitude,
                        bounds.extents.y);
            var center =
                fixedCenter ?? bounds.center;
            if (radius <= 0f)
            {
                throw new InvalidOperationException(
                    "Revolution review framing radius must be positive.");
            }

            var distance =
                radius /
                Mathf.Tan(
                    camera.fieldOfView *
                    0.5f *
                    Mathf.Deg2Rad) *
                1.2f;
            camera.transform.position =
                center + viewDirection * distance;
            camera.transform.rotation =
                Quaternion.LookRotation(
                    center -
                    camera.transform.position,
                    Vector3.up);

            var renderTexture =
                RenderTexture.GetTemporary(
                    PanelSize,
                    PanelSize,
                    24,
                    RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture =
                    new Texture2D(
                        PanelSize,
                        PanelSize,
                        TextureFormat.RGBA32,
                        false);
                texture.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        PanelSize,
                        PanelSize),
                    0,
                    0);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Bounds BakedWorldBounds(
            SkinnedMeshRenderer renderer)
        {
            var bakedMesh = new Mesh
            {
                name = "Revolution_Melee_Review_BakedMesh"
            };
            try
            {
                renderer.BakeMesh(bakedMesh);
                var local = bakedMesh.bounds;
                var transform = renderer.transform;
                var worldCenter =
                    transform.TransformPoint(local.center);
                var worldExtentsX =
                    transform.TransformVector(
                        new Vector3(local.extents.x, 0f, 0f));
                var worldExtentsY =
                    transform.TransformVector(
                        new Vector3(0f, local.extents.y, 0f));
                var worldExtentsZ =
                    transform.TransformVector(
                        new Vector3(0f, 0f, local.extents.z));
                var worldExtents =
                    new Vector3(
                        Mathf.Abs(worldExtentsX.x) +
                        Mathf.Abs(worldExtentsY.x) +
                        Mathf.Abs(worldExtentsZ.x),
                        Mathf.Abs(worldExtentsX.y) +
                        Mathf.Abs(worldExtentsY.y) +
                        Mathf.Abs(worldExtentsZ.y),
                        Mathf.Abs(worldExtentsX.z) +
                        Mathf.Abs(worldExtentsY.z) +
                        Mathf.Abs(worldExtentsZ.z));
                return new Bounds(
                    worldCenter,
                    worldExtents * 2f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        private static Texture2D CloneTexture(
            Texture2D source)
        {
            var clone =
                new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false);
            clone.SetPixels32(source.GetPixels32());
            clone.Apply(false, false);
            return clone;
        }

        private static Texture2D ComposeSheet(
            IReadOnlyList<Texture2D> panels,
            int columns,
            int rows)
        {
            if (panels.Count != columns * rows)
            {
                throw new InvalidOperationException(
                    "Unexpected Revolution melee review panel count.");
            }

            var sheet =
                new Texture2D(
                    columns * PanelSize,
                    rows * PanelSize,
                    TextureFormat.RGBA32,
                    false);
            var background =
                Enumerable.Repeat(
                        new Color32(7, 9, 12, 255),
                        sheet.width * sheet.height)
                    .ToArray();
            sheet.SetPixels32(background);
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0;
                     column < columns;
                     column++)
                {
                    var panel =
                        panels[row * columns + column];
                    sheet.SetPixels32(
                        column * PanelSize,
                        (rows - 1 - row) * PanelSize,
                        PanelSize,
                        PanelSize,
                        panel.GetPixels32());
                }
            }

            sheet.Apply(false, false);
            return sheet;
        }

        private static LayerState[] CaptureLayerStates(
            Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(item =>
                    new LayerState(
                        item.gameObject,
                        item.gameObject.layer))
                .ToArray();
        }

        private static void SetLayerRecursively(
            Transform root,
            int layer)
        {
            foreach (var item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                item.gameObject.layer = layer;
            }
        }

        private static void RestoreLayerStates(
            IEnumerable<LayerState> states)
        {
            foreach (var state in states)
            {
                state.GameObject.layer = state.Layer;
            }
        }

        private static void RevertSampledEulerHintOverrides(
            Transform root)
        {
            foreach (var item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                var serialized = new SerializedObject(item);
                var eulerHint =
                    serialized.FindProperty(
                        "m_LocalEulerAnglesHint");
                if (eulerHint != null &&
                    eulerHint.prefabOverride)
                {
                    PrefabUtility.RevertPropertyOverride(
                        eulerHint,
                        InteractionMode.AutomatedAction);
                }
            }
        }

        private static void WriteInspection(
            Transform staticModel,
            Transform replacement,
            AnimationClip clip,
            AnimatorController controller)
        {
            var staticRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01");
            var replacementRenderer =
                RequireMainRenderer(
                    replacement,
                    "Revolution_05");
            var builder = new StringBuilder();
            builder.AppendLine(
                "Revolution 05 Melee Attack Inspection");
            builder.AppendLine(
                "Source=" + SourceFbxPath);
            builder.AppendLine(
                "ImportedSource=" + ImportedFbxPath);
            builder.AppendLine(
                "SourceSha256=" + SourceSha256);
            builder.AppendLine(
                "Slot=" + MeleeSlotName);
            builder.AppendLine(
                "PrefabSource=" + ImportedFbxPath);
            builder.AppendLine(
                "Clip=" + clip.name);
            builder.AppendLine(
                "ClipLength=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ClipFrameRate=" +
                clip.frameRate.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ClipCurveBindings=" +
                AnimationUtility.GetCurveBindings(clip).Length);
            builder.AppendLine("Loop=True");
            builder.AppendLine("RootMotion=False");
            builder.AppendLine(
                "Controller=" + ControllerPath);
            builder.AppendLine(
                "State=" + StateName);
            builder.AppendLine(
                "StaticMesh=" +
                AssetDatabase.GetAssetPath(
                    staticRenderer.sharedMesh));
            builder.AppendLine(
                "MeleeMesh=" +
                AssetDatabase.GetAssetPath(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "StaticAndMeleeMeshSame=" +
                (staticRenderer.sharedMesh ==
                 replacementRenderer.sharedMesh));
            builder.AppendLine(
                "ApprovedMaterials=" +
                string.Join(
                    "|",
                    staticRenderer.sharedMaterials.Select(
                        AssetDatabase.GetAssetPath)));
            builder.AppendLine(
                "StaticAndMeleeMaterialsSame=" +
                staticRenderer.sharedMaterials.SequenceEqual(
                    replacementRenderer.sharedMaterials));
            builder.AppendLine(
                "UnityMeshVertices=" +
                replacementRenderer.sharedMesh.vertexCount);
            builder.AppendLine(
                "Triangles=" +
                TriangleCount(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "Bones=" +
                replacementRenderer.bones.Length);
            builder.AppendLine(
                "VisualReview=" + CapturePath);
            builder.AppendLine(
                "VisualRows=StaticFront|AnimatedFront|StaticOblique|AnimatedOblique");
            builder.AppendLine(
                "VisualColumns=0|0.25|0.5|0.75|1 normalized time");
            builder.AppendLine(
                "NewMeshCreated=False");
            builder.AppendLine(
                "NewMaterialCreated=False");
            builder.AppendLine(
                "NewTextureCreated=False");
            builder.AppendLine(
                "OtherRevolutionSlotsChanged=False");
            File.WriteAllText(
                Absolute(InspectionPath),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteDeathInspection(
            Transform staticModel,
            Transform replacement,
            AnimationClip clip,
            AnimatorController controller)
        {
            var staticRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01");
            var replacementRenderer =
                RequireMainRenderer(
                    replacement,
                    "Revolution_08");
            var builder = new StringBuilder();
            builder.AppendLine(
                "Revolution 08 Death Inspection");
            builder.AppendLine(
                "Source=" + DeathSourceFbxPath);
            builder.AppendLine(
                "ImportedSource=" +
                DeathImportedFbxPath);
            builder.AppendLine(
                "SourceSha256=" +
                DeathSourceSha256);
            builder.AppendLine(
                "Slot=" + DeathSlotName);
            builder.AppendLine(
                "PrefabSource=" +
                DeathImportedFbxPath);
            builder.AppendLine(
                "SelectedTake=Armature|mixamo.com|Layer0");
            builder.AppendLine(
                "Clip=" + clip.name);
            builder.AppendLine(
                "ClipLength=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ClipFrameRate=" +
                clip.frameRate.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ClipCurveBindings=" +
                AnimationUtility
                    .GetCurveBindings(clip).Length);
            builder.AppendLine("Loop=True");
            builder.AppendLine("LoopPose=False");
            builder.AppendLine("RootMotion=False");
            builder.AppendLine(
                "Controller=" +
                DeathControllerPath);
            builder.AppendLine(
                "State=" + DeathStateName);
            builder.AppendLine(
                "StaticMesh=" +
                AssetDatabase.GetAssetPath(
                    staticRenderer.sharedMesh));
            builder.AppendLine(
                "DeathMesh=" +
                AssetDatabase.GetAssetPath(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "StaticAndDeathMeshSame=" +
                (staticRenderer.sharedMesh ==
                 replacementRenderer.sharedMesh));
            builder.AppendLine(
                "ApprovedMaterials=" +
                string.Join(
                    "|",
                    staticRenderer.sharedMaterials.Select(
                        AssetDatabase.GetAssetPath)));
            builder.AppendLine(
                "StaticAndDeathMaterialsSame=" +
                staticRenderer.sharedMaterials
                    .SequenceEqual(
                        replacementRenderer
                            .sharedMaterials));
            builder.AppendLine(
                "UnityMeshVertices=" +
                replacementRenderer.sharedMesh
                    .vertexCount);
            builder.AppendLine(
                "Triangles=" +
                TriangleCount(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "Bones=" +
                replacementRenderer.bones.Length);
            builder.AppendLine(
                "VisualReview=" +
                DeathCapturePath);
            builder.AppendLine(
                "VisualRows=StaticFront|AnimatedFront|StaticOblique|AnimatedOblique");
            builder.AppendLine(
                "VisualColumns=0|0.25|0.5|0.75|1 normalized time");
            builder.AppendLine(
                "NewMeshCreated=False");
            builder.AppendLine(
                "NewMaterialCreated=False");
            builder.AppendLine(
                "NewTextureCreated=False");
            builder.AppendLine(
                "OtherRevolutionSlotsChanged=False");
            File.WriteAllText(
                Absolute(DeathInspectionPath),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static void WriteDeathEndInspection(
            AnimationClip clip)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                "Revolution 08 Death Exact End Review");
            builder.AppendLine(
                "Slot=" + DeathSlotName);
            builder.AppendLine(
                "Clip=" + clip.name);
            builder.AppendLine(
                "ClipLength=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "FinalNormalizedTime=1");
            builder.AppendLine(
                "FinalSampleTime=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ReviewSampling=NonLoopingInMemoryClone");
            builder.AppendLine(
                "RuntimeClipModified=False");
            builder.AppendLine(
                "RuntimeLoop=True");
            builder.AppendLine(
                "RuntimeLoopPose=False");
            builder.AppendLine(
                "RuntimeRootMotion=False");
            builder.AppendLine(
                "VisualReview=" +
                DeathEndCapturePath);
            builder.AppendLine(
                "VisualRows=StaticFront|AnimatedFront|StaticOblique|AnimatedOblique");
            builder.AppendLine(
                "VisualColumns=0|0.25|0.5|0.75|1 normalized time");
            File.WriteAllText(
                Absolute(DeathEndInspectionPath),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var index = 0;
                 index < mesh.subMeshCount;
                 index++)
            {
                count +=
                    (int)mesh.GetIndexCount(index) / 3;
            }

            return count;
        }

        private static void RequireHash(
            string path,
            string expected)
        {
            var absolute =
                path.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal)
                    ? Absolute(path)
                    : path;
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Required Revolution file is missing.",
                    absolute);
            }

            var actual = Sha256(absolute);
            if (!string.Equals(
                    actual,
                    expected,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Revolution file hash differs. Path=" +
                    path + ", Actual=" + actual +
                    ", Expected=" + expected + ".");
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(
                    hash.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)
                        ?.FullName ??
                    throw new InvalidOperationException(
                        "Project root is unavailable."),
                    path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private readonly struct LayerState
        {
            public LayerState(
                GameObject gameObject,
                int layer)
            {
                GameObject = gameObject;
                Layer = layer;
            }

            public GameObject GameObject { get; }
            public int Layer { get; }
        }
    }
}
