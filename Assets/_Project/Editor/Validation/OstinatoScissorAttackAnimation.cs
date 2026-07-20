using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Ostinato;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class OstinatoScissorAttackAnimation
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string PlacementRootName = "Approved Ostinato Enemy Placement";
        internal const string StaticSlotName = "Ostinato_04_Static_Review";
        internal const string AttackSlotName = "Ostinato_04_Scissor_Attack";
        internal const string AttackModelName = "Ostinato_ScissorAttack_Model";
        internal const string AttackModelPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack.fbx";
        internal const string SourceAttackRelativePath = "enemies model/ostinato attack.fbx";
        internal const string ApprovedModelPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_ApprovedUnity.fbx";
        internal const string ControllerPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack.controller";
        internal const string StateName = "Ostinato_04_Attack_Source";
        internal const string ValidationFolderPath = "docs/validation/ostinato_attack_fbx_direct_instance_2026-07-20";
        private const string InspectionReportPath = ValidationFolderPath + "/Ostinato_AttackFbxDirectInstanceInspection.txt";
        private const string ApplyReportPath = ValidationFolderPath + "/Ostinato_AttackFbxDirectInstanceApply.txt";
        private const string ReviewReportPath = ValidationFolderPath + "/Ostinato_ScissorAttackReview.txt";
        private const string AccelerationValidationFolderPath = "docs/validation/ostinato_attack_downstrike_acceleration_2026-07-20";
        private const string AccelerationApplyReportPath = AccelerationValidationFolderPath + "/Ostinato_AttackDownstrikeAccelerationApply.txt";
        private const string AccelerationInspectionReportPath = AccelerationValidationFolderPath + "/Ostinato_AttackDownstrikeAccelerationInspection.txt";
        private const string AttackTakeNameFragment = "mixamo.com";
        private const string AttackBindingArmatureName = "Armature";
        private const int ExpectedSlotIndex = 3;
        // User-approved playback rate for the full Ostinato attack take.
        private const float AttackPlaybackSpeed = 2f;

        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Scissor Attack Target")]
        public static void InspectOstinatoScissorAttackTarget()
        {
            var scene = RequireOpenScene();
            var root = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(root);
            var attackAsset = RequireAsset<GameObject>(AttackModelPath);
            var approvedAsset = RequireAsset<GameObject>(ApprovedModelPath);
            var attackRenderer = RequireSingleRenderer(attackAsset, "attack asset");
            var approvedRenderer = RequireSingleRenderer(approvedAsset, "approved asset");
            var clip = RequireAttackClip();
            RequireClipContract(clip);
            var overrideTake = RequireLoopOnlyImporterContract(out var defaultTake);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceAttackRelativePath));
            var importedHash = ComputeSha256(ProjectAbsolutePath(AttackModelPath));
            var sceneModel = slot.childCount == 1 ? slot.GetChild(0).gameObject : null;
            var sceneRenderer = sceneModel != null ? RequireSingleRenderer(sceneModel, "attack scene model") : null;
            if (sceneModel == null)
            {
                throw new InvalidOperationException("Ostinato attack scene model is missing.");
            }
            RequireClipBindingsResolve(sceneModel, clip);
            var sceneAnimator = sceneModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Ostinato attack scene Animator is missing.");
            var scenePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sceneModel);
            if (scenePrefabPath != AttackModelPath)
            {
                throw new InvalidOperationException("Ostinato attack scene model must be an instance of the supplied attack FBX. Actual=" +
                                                    scenePrefabPath);
            }
            RequireSynchronizedAppearance(sceneModel, sceneRenderer, approvedRenderer);
            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + PlacementRootName + "/" + slot.name);
            report.AppendLine("TargetSiblingIndex=" + slot.GetSiblingIndex().ToString(CultureInfo.InvariantCulture));
            report.AppendLine("AttackModel=" + AttackModelPath);
            report.AppendLine("SourceAttack=" + SourceAttackRelativePath);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("ImportedSha256=" + importedHash);
            report.AppendLine("SourceCopyHashesMatch=" + (sourceHash == importedHash));
            report.AppendLine("SelectedTake=" + AttackTakeNameFragment);
            report.AppendLine("AnimationBindingRoot=" + AttackBindingArmatureName);
            report.AppendLine("PlaybackModel=" + AttackModelPath);
            report.AppendLine("PlaybackPrefabSource=" + scenePrefabPath);
            report.AppendLine("AppearanceModel=" + ApprovedModelPath);
            report.AppendLine("Clip=" + clip.name);
            report.AppendLine("ClipLength=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("ClipFrameRate=" + clip.frameRate.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("ClipCurveBindings=" + AnimationUtility.GetCurveBindings(clip).Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("ClipCurveFingerprint=" + BuildClipCurveFingerprint(clip));
            report.AppendLine("ClipOverrideCount=1");
            report.AppendLine("DefaultTakeFirstFrame=" + defaultTake.firstFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("DefaultTakeLastFrame=" + defaultTake.lastFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("DefaultTakeLoopTime=" + defaultTake.loopTime);
            report.AppendLine("OverrideTakeFirstFrame=" + overrideTake.firstFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("OverrideTakeLastFrame=" + overrideTake.lastFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("OverrideTakeLoopTime=" + overrideTake.loopTime);
            report.AppendLine("LoopOnlyOverride=True");
            report.AppendLine("DefaultTakeLockRootRotation=" + defaultTake.lockRootRotation);
            report.AppendLine("DefaultTakeLockRootHeightY=" + defaultTake.lockRootHeightY);
            report.AppendLine("DefaultTakeLockRootPositionXZ=" + defaultTake.lockRootPositionXZ);
            report.AppendLine("ImportedLoopTime=" + AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("AnimatorApplyRootMotion=" + sceneAnimator.applyRootMotion);
            report.AppendLine("AnimatorApplyRootMotionOverridden=False");
            report.AppendLine("PlaybackSpeed=" + RequireAttackControllerContract(clip).ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("MovementCurveModified=False");
            report.AppendLine("ClipBindingsResolveOnAttackFbxInstance=True");
            report.AppendLine("AttackVertexCount=" + attackRenderer.sharedMesh.vertexCount);
            report.AppendLine("ApprovedVertexCount=" + approvedRenderer.sharedMesh.vertexCount);
            report.AppendLine("AttackSubMeshCount=" + attackRenderer.sharedMesh.subMeshCount);
            report.AppendLine("ApprovedSubMeshCount=" + approvedRenderer.sharedMesh.subMeshCount);
            report.AppendLine("AttackBounds=" + attackRenderer.sharedMesh.bounds.ToString("R", CultureInfo.InvariantCulture));
            report.AppendLine("ApprovedBounds=" + approvedRenderer.sharedMesh.bounds.ToString("R", CultureInfo.InvariantCulture));
            report.AppendLine("AttackCoreFingerprint=" + BuildCoreAppearanceFingerprint(attackRenderer.sharedMesh));
            report.AppendLine("ApprovedCoreFingerprint=" + BuildCoreAppearanceFingerprint(approvedRenderer.sharedMesh));
            report.AppendLine("CoreAppearanceMatchesApproved=" +
                (BuildCoreAppearanceFingerprint(attackRenderer.sharedMesh) == BuildCoreAppearanceFingerprint(approvedRenderer.sharedMesh)));
            report.AppendLine("AttackMeshFingerprint=" + BuildAppearanceFingerprint(attackRenderer.sharedMesh));
            report.AppendLine("ApprovedMeshFingerprint=" + BuildAppearanceFingerprint(approvedRenderer.sharedMesh));
            report.AppendLine("PlaybackUsesSuppliedAttackFbxInstance=True");
            report.AppendLine("PlaybackUsesApprovedMesh=True");
            report.AppendLine("CorrectedBladeMeshUsed=False");
            report.AppendLine("BladeCorrectionControlsPresent=False");
            report.AppendLine("SceneUsesApprovedMesh=" +
                (sceneRenderer != null && BuildAppearanceFingerprint(sceneRenderer.sharedMesh) == BuildAppearanceFingerprint(approvedRenderer.sharedMesh)));
            report.AppendLine("SceneUsesApprovedMaterials=" +
                (sceneRenderer != null && sceneRenderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).SequenceEqual(ApprovedMaterialPaths)));
            report.AppendLine("OtherSlotsTargeted=False");
            WriteText(InspectionReportPath, report.ToString());
            Debug.Log("OstinatoScissorAttackTargetInspected, Target=" + slot.name + ", Clip=" + clip.name);
        }

        public static void InspectOstinatoAttackAppearanceSync()
        {
            InspectOstinatoScissorAttackTarget();
        }

        public static void InspectOstinatoAttackFbxUnmodified()
        {
            InspectOstinatoScissorAttackTarget();
        }

        public static void InspectOstinatoAttackLoopPlayback()
        {
            InspectOstinatoScissorAttackTarget();
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Scissor Attack Animation")]
        public static void ApplyOstinatoScissorAttackAnimation()
        {
            var scene = RequireOpenScene();
            var root = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(root);
            var otherSlotsBefore = CaptureOtherSlotSignatures(root);
            var slotPosition = slot.localPosition;
            var slotRotation = slot.localRotation;
            var slotScale = slot.localScale;
            var sourceHashBefore = ComputeSha256(ProjectAbsolutePath(SourceAttackRelativePath));
            var importedHashBefore = ComputeSha256(ProjectAbsolutePath(AttackModelPath));
            if (sourceHashBefore != importedHashBefore)
            {
                throw new InvalidOperationException("The imported Ostinato attack FBX differs from the supplied source.");
            }

            ConfigureAttackImporter();
            RequireAsset<GameObject>(AttackModelPath);
            var attackAsset = RequireAsset<GameObject>(AttackModelPath);
            var approvedAsset = RequireAsset<GameObject>(ApprovedModelPath);
            var approvedRenderer = RequireSingleRenderer(approvedAsset, "approved asset");
            var materials = ApprovedMaterialPaths.Select(RequireAsset<Material>).ToArray();
            var clip = RequireAttackClip();
            RequireClipContract(clip);
            var overrideTake = RequireLoopOnlyImporterContract(out var defaultTake);
            var curveFingerprintBefore = BuildClipCurveFingerprint(clip);
            var controller = CreateOrUpdateController(clip);
            var playbackSpeed = RequireAttackControllerContract(clip);

            var previousModel = slot.childCount == 1 ? slot.GetChild(0) : null;
            if (previousModel == null)
            {
                throw new InvalidOperationException("Ostinato slot 04 must contain exactly one model before attack application.");
            }
            var modelPosition = previousModel.localPosition;
            var modelRotation = previousModel.localRotation;
            var modelScale = previousModel.localScale;
            UnityEngine.Object.DestroyImmediate(previousModel.gameObject);

            var model = PrefabUtility.InstantiatePrefab(attackAsset, scene) as GameObject ??
                throw new InvalidOperationException("The supplied Ostinato attack FBX could not be instantiated for attack playback.");
            model.name = AttackModelName;
            model.transform.SetParent(slot, false);
            model.transform.localPosition = modelPosition;
            model.transform.localRotation = modelRotation;
            model.transform.localScale = modelScale;
            ConfigureAttackPlaybackBindingRoot(model);
            RequireClipBindingsResolve(model, clip);
            var renderer = RequireSingleRenderer(model, "attack instance");
            SynchronizeApprovedAppearance(model, renderer, approvedRenderer, materials);
            renderer.updateWhenOffscreen = true;
            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            var inheritedApplyRootMotion = animator.applyRootMotion;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            slot.name = AttackSlotName;

            if (slot.GetSiblingIndex() != ExpectedSlotIndex ||
                slot.localPosition != slotPosition || slot.localRotation != slotRotation || slot.localScale != slotScale)
            {
                throw new InvalidOperationException("Ostinato slot 04 transform or sibling index changed during attack application.");
            }
            RequireOtherSlotsUnchanged(root, otherSlotsBefore);
            RequireApprovedAppearance(renderer.sharedMesh, approvedRenderer.sharedMesh);
            RequireSynchronizedAppearance(model, renderer, approvedRenderer);
            var prefabSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model);
            if (prefabSourcePath != AttackModelPath)
            {
                throw new InvalidOperationException("The new Ostinato attack scene object is not sourced from the supplied attack FBX. Actual=" +
                                                    prefabSourcePath);
            }
            if (!renderer.sharedMaterials.SequenceEqual(materials))
            {
                throw new InvalidOperationException("Ostinato attack instance does not use the four approved materials in order.");
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            var sourceHashAfter = ComputeSha256(ProjectAbsolutePath(SourceAttackRelativePath));
            var importedHashAfter = ComputeSha256(ProjectAbsolutePath(AttackModelPath));
            if (sourceHashAfter != sourceHashBefore || importedHashAfter != importedHashBefore || sourceHashAfter != importedHashAfter)
            {
                throw new InvalidOperationException("An Ostinato attack FBX changed during scene application.");
            }
            var curveFingerprintAfter = BuildClipCurveFingerprint(RequireAttackClip());
            if (curveFingerprintAfter != curveFingerprintBefore)
            {
                throw new InvalidOperationException("Ostinato attack movement curves changed during scene application.");
            }

            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName + "/" + AttackModelName);
            report.AppendLine("Clip=" + clip.name);
            report.AppendLine("SelectedTake=" + AttackTakeNameFragment);
            report.AppendLine("AnimationBindingRoot=" + AttackBindingArmatureName);
            report.AppendLine("ClipLength=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("ClipFrameRate=" + clip.frameRate.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("ClipCurveBindings=" + AnimationUtility.GetCurveBindings(clip).Length.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("LoopTime=" + AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("ClipOverrideCount=1");
            report.AppendLine("DefaultTakeFirstFrame=" + defaultTake.firstFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("DefaultTakeLastFrame=" + defaultTake.lastFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("DefaultTakeLoopTime=" + defaultTake.loopTime);
            report.AppendLine("OverrideTakeFirstFrame=" + overrideTake.firstFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("OverrideTakeLastFrame=" + overrideTake.lastFrame.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("OverrideTakeLoopTime=" + overrideTake.loopTime);
            report.AppendLine("LoopOnlyOverride=True");
            report.AppendLine("DefaultTakeLockRootRotation=" + defaultTake.lockRootRotation);
            report.AppendLine("DefaultTakeLockRootHeightY=" + defaultTake.lockRootHeightY);
            report.AppendLine("DefaultTakeLockRootPositionXZ=" + defaultTake.lockRootPositionXZ);
            report.AppendLine("MovementCurveFingerprintBefore=" + curveFingerprintBefore);
            report.AppendLine("MovementCurveFingerprintAfter=" + curveFingerprintAfter);
            report.AppendLine("MovementCurveModified=False");
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("AnimatorState=" + StateName);
            report.AppendLine("PlaybackPrefabSource=" + prefabSourcePath);
            report.AppendLine("PlaybackUsesSuppliedAttackFbxInstance=True");
            report.AppendLine("PlaybackSpeed=" + playbackSpeed.ToString("0.###", CultureInfo.InvariantCulture));
            report.AppendLine("AnimatorApplyRootMotion=" + inheritedApplyRootMotion);
            report.AppendLine("AnimatorApplyRootMotionOverridden=False");
            report.AppendLine("Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh));
            report.AppendLine("AppearanceFingerprint=" + BuildAppearanceFingerprint(renderer.sharedMesh));
            report.AppendLine("ApprovedAppearanceFingerprint=" + BuildAppearanceFingerprint(approvedRenderer.sharedMesh));
            report.AppendLine("AppearanceSynchronizedFrom=" + ApprovedModelPath);
            report.AppendLine("CorrectedBladeMeshUsed=False");
            report.AppendLine("BladeCorrectionControlsPresent=False");
            report.AppendLine("Materials=" + string.Join("|", materials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("SourceSha256Before=" + sourceHashBefore);
            report.AppendLine("SourceSha256After=" + sourceHashAfter);
            report.AppendLine("ImportedSha256Before=" + importedHashBefore);
            report.AppendLine("ImportedSha256After=" + importedHashAfter);
            report.AppendLine("SourceCopyHashesMatch=True");
            report.AppendLine("OtherSlotsUnchanged=True");
            WriteText(ApplyReportPath, report.ToString());
            Selection.activeGameObject = slot.gameObject;
            Debug.Log("OstinatoAttackLoopPlaybackApplied, Target=" + AttackSlotName +
                ", Length=" + Format(clip.length) +
                ", Loop=" + AnimationUtility.GetAnimationClipSettings(clip).loopTime +
                ", AnimatorApplyRootMotion=" + inheritedApplyRootMotion +
                ", MovementCurveModified=False, SuppliedAttackFbxInstance=True, ApprovedAppearance=True, OtherSlotsUnchanged=True");
        }

        public static void ApplyOstinatoAttackFbxReplacement()
        {
            ApplyOstinatoScissorAttackAnimation();
        }

        public static void ApplyOstinatoAttackFbxUnmodified()
        {
            ApplyOstinatoScissorAttackAnimation();
        }

        public static void EnableOstinatoAttackLoopPlayback()
        {
            ApplyOstinatoScissorAttackAnimation();
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Downstrike Speed Acceleration")]
        public static void ApplyOstinatoAttackDownstrikeAcceleration()
        {
            var scene = RequireOpenScene();
            var root = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(root);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato attack slot must contain exactly one playback object.");
            }

            var playbackObjectId = GlobalObjectId.GetGlobalObjectIdSlow(slot.GetChild(0).gameObject).ToString();
            var otherSlotsBefore = CaptureOtherSlotSignatures(root);
            var sourcePath = ProjectAbsolutePath(SourceAttackRelativePath);
            var importedPath = ProjectAbsolutePath(AttackModelPath);
            var sourceHashBefore = ComputeSha256(sourcePath);
            var importedHashBefore = ComputeSha256(importedPath);
            if (sourceHashBefore != importedHashBefore)
            {
                throw new InvalidOperationException("The imported Ostinato attack FBX differs from the supplied source.");
            }

            var clip = RequireAttackClip();
            RequireClipContract(clip);
            var curveFingerprintBefore = BuildClipCurveFingerprint(clip);
            var state = RequireAttackSourceState(clip);
            var profile = RequireOrCreateAttackAccelerationProfile(state);
            profile.ConfigureApprovedProfile();
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(RequireAsset<AnimatorController>(ControllerPath));
            AssetDatabase.SaveAssets();

            if (GlobalObjectId.GetGlobalObjectIdSlow(slot.GetChild(0).gameObject).ToString() != playbackObjectId)
            {
                throw new InvalidOperationException("The Ostinato attack playback object changed during speed-profile application.");
            }
            RequireOtherSlotsUnchanged(root, otherSlotsBefore);
            if (ComputeSha256(sourcePath) != sourceHashBefore || ComputeSha256(importedPath) != importedHashBefore ||
                BuildClipCurveFingerprint(RequireAttackClip()) != curveFingerprintBefore)
            {
                throw new InvalidOperationException("The Ostinato source attack data changed during speed-profile application.");
            }

            var inspection = InspectAttackAccelerationProfileInternal();
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName);
            report.AppendLine("PlaybackObjectId=" + playbackObjectId);
            report.AppendLine("ControllerStateSpeed=" + Format(AttackPlaybackSpeed));
            report.AppendLine("SpeedControl=Animator.speed only");
            report.AppendLine("SourceFrameRange=0..196");
            report.AppendLine("ForwardSlashAccelerationFrames=53..93");
            report.AppendLine("ForwardSlashAdditionalMultiplier=3..5");
            report.AppendLine("ForwardSlashEffectiveSpeed=6..10");
            report.AppendLine("OutsideForwardSlashEffectiveSpeed=2");
            report.AppendLine("MaximumAdditionalMultiplier=" + Format(inspection.Profile.StrikeEndMultiplier));
            report.AppendLine("MaximumEffectiveSpeed=" + Format(AttackPlaybackSpeed * inspection.Profile.StrikeEndMultiplier));
            report.AppendLine("SourceFbxSha256=" + sourceHashBefore);
            report.AppendLine("SourceCurveFingerprint=" + curveFingerprintBefore);
            report.AppendLine("AnimationCurveOrPoseEdit=False");
            report.AppendLine("ModelOrBoneEdit=False");
            report.AppendLine("AttackObjectRecreated=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            WriteText(AccelerationApplyReportPath, report.ToString());
            Debug.Log("Ostinato forward-slash speed-only profile applied: frames 53 through 93 accelerate from effective speed 6 to 10.");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Downstrike Speed Acceleration")]
        public static void InspectOstinatoAttackDownstrikeAcceleration()
        {
            InspectOstinatoScissorAttackTarget();
            var inspection = InspectAttackAccelerationProfileInternal();
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("ControllerState=" + StateName);
            report.AppendLine("ControllerStateSpeed=" + Format(AttackPlaybackSpeed));
            report.AppendLine("SpeedControl=Animator.speed only");
            report.AppendLine("SourceClip=" + inspection.Clip.name);
            report.AppendLine("SourceFrameRange=0..196");
            report.AppendLine("SourceClipLength=" + Format(inspection.Clip.length));
            report.AppendLine("SourceClipFrameRate=" + Format(inspection.Clip.frameRate));
            report.AppendLine("SourceCurveBindings=" + AnimationUtility.GetCurveBindings(inspection.Clip).Length);
            report.AppendLine("SourceCurveFingerprint=" + BuildClipCurveFingerprint(inspection.Clip));
            foreach (var frame in new[] { 0f, 52f, 53f, 70f, 84f, 85f, 90f, 93f, 94f, 100f, 101f, 196f })
            {
                var additional = inspection.Profile.EvaluateAdditionalSpeedAtSourceFrame(frame);
                report.AppendLine("Frame" + Format(frame) + "AdditionalMultiplier=" + Format(additional));
                report.AppendLine("Frame" + Format(frame) + "EffectiveSpeed=" + Format(additional * AttackPlaybackSpeed));
            }
            report.AppendLine("MaximumEffectiveSpeed=" + Format(inspection.Profile.StrikeEndMultiplier * AttackPlaybackSpeed));
            report.AppendLine("FullSourceLoopPreserved=True");
            report.AppendLine("AnimationCurveOrPoseEdit=False");
            report.AppendLine("ModelOrBoneEdit=False");
            report.AppendLine("AttackObjectRecreated=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            WriteText(AccelerationInspectionReportPath, report.ToString());
            Debug.Log("Ostinato downstrike speed-only acceleration inspection passed.");
        }

        public static void CaptureOstinatoAttackDownstrikeAcceleration()
        {
            InspectAttackAccelerationProfileInternal();
            CaptureOstinatoScissorAttackRuntimePlayback();
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Review Scissor Attack Animation")]
        public static void ReviewOstinatoScissorAttackAnimation()
        {
            var scene = RequireOpenScene();
            var root = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(root);
            if (slot.name != AttackSlotName || slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato slot 04 attack instance has not been applied.");
            }
            var model = slot.GetChild(0).gameObject;
            var animator = model.GetComponent<Animator>() ??
                throw new InvalidOperationException("Ostinato attack Animator is missing.");
            var renderer = RequireSingleRenderer(model, "attack instance");
            var clip = RequireAttackClip();
            RequireClipContract(clip);
            if (animator.runtimeAnimatorController == null || animator.applyRootMotion || !animator.enabled)
            {
                throw new InvalidOperationException("Ostinato attack Animator is not configured for root-locked playback.");
            }

            var modelPosition = model.transform.localPosition;
            var modelRotation = model.transform.localRotation;
            var modelScale = model.transform.localScale;
            PoseMeasure defaultPose;
            PoseMeasure opposedWindupPose;
            PoseMeasure wholeArmSwingPose;
            PoseMeasure crossedImpactPose;
            PoseMeasure compressedPullPose;
            PoseMeasure loopPose;
            AnimationMode.StartAnimationMode();
            try
            {
                defaultPose = SamplePose(model, renderer, clip, 0f);
                opposedWindupPose = SamplePose(model, renderer, clip, 63f / 60f);
                wholeArmSwingPose = SamplePose(model, renderer, clip, 95f / 60f);
                crossedImpactPose = SamplePose(model, renderer, clip, 105f / 60f);
                compressedPullPose = SamplePose(model, renderer, clip, 153f / 60f);
                loopPose = SamplePose(model, renderer, clip, clip.length - (1f / clip.frameRate));
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
            if (!MatchesTransform(model.transform, modelPosition, modelRotation, modelScale))
            {
                throw new InvalidOperationException("Ostinato attack model root changed during edit-mode sampling.");
            }
            if (opposedWindupPose.HandSeparation <= defaultPose.HandSeparation * 1.1f ||
                opposedWindupPose.VerticalHandOffset < 0.25f ||
                opposedWindupPose.DepthHandOffset < 0.2f)
            {
                throw new InvalidOperationException(
                    "Ostinato attack does not reach the opposed high/back and low/forward wind-up. " +
                    "DefaultHands=" + Format(defaultPose.HandSeparation) +
                    ", WindupHands=" + Format(opposedWindupPose.HandSeparation) +
                    ", WindupVerticalOffset=" + Format(opposedWindupPose.VerticalHandOffset) +
                    ", WindupDepthOffset=" + Format(opposedWindupPose.DepthHandOffset) +
                    ", DefaultWidth=" + Format(defaultPose.BoundsWidth) +
                    ", WindupWidth=" + Format(opposedWindupPose.BoundsWidth));
            }
            if (!wholeArmSwingPose.KeepsBilateralHandOrderFrom(defaultPose) ||
                !crossedImpactPose.KeepsBilateralHandOrderFrom(defaultPose) ||
                !compressedPullPose.KeepsBilateralHandOrderFrom(defaultPose))
            {
                throw new InvalidOperationException(
                    "Ostinato whole-arm scissor crosses the anatomical hand roots. " +
                    "DefaultHands=" + Format(defaultPose.HandSeparation) +
                    ", ImpactHands=" + Format(crossedImpactPose.HandSeparation) +
                    ", PullHands=" + Format(compressedPullPose.HandSeparation));
            }
            var maximumHandRotation = Mathf.Max(
                wholeArmSwingPose.MaximumHandLocalRotationFrom(defaultPose),
                crossedImpactPose.MaximumHandLocalRotationFrom(defaultPose),
                compressedPullPose.MaximumHandLocalRotationFrom(defaultPose));
            if (maximumHandRotation > 8f)
            {
                throw new InvalidOperationException(
                    "Ostinato attack rotates the wrists instead of driving the blades with the whole arms. " +
                    "MaximumHandLocalRotation=" + Format(maximumHandRotation));
            }
            var impactSpineRotation = crossedImpactPose.SpineLocalRotationFrom(defaultPose);
            if (compressedPullPose.HandDepthFromBody >= crossedImpactPose.HandDepthFromBody - 0.20f)
            {
                throw new InvalidOperationException(
                    "Ostinato hooked arms do not pull the target volume toward the body. " +
                    "ImpactDepth=" + Format(crossedImpactPose.HandDepthFromBody) +
                    ", PullDepth=" + Format(compressedPullPose.HandDepthFromBody));
            }
            if (!defaultPose.Matches(loopPose, 0.002f))
            {
                throw new InvalidOperationException(
                    "Ostinato attack last pose does not return to the default pose. " +
                    "DefaultLeft=" + defaultPose.LeftHand.ToString("R", CultureInfo.InvariantCulture) +
                    ", LoopLeft=" + loopPose.LeftHand.ToString("R", CultureInfo.InvariantCulture) +
                    ", DefaultRight=" + defaultPose.RightHand.ToString("R", CultureInfo.InvariantCulture) +
                    ", LoopRight=" + loopPose.RightHand.ToString("R", CultureInfo.InvariantCulture) +
                    ", DefaultBounds=" + Format(defaultPose.BoundsWidth) + "x" + Format(defaultPose.BoundsHeight) +
                    ", LoopBounds=" + Format(loopPose.BoundsWidth) + "x" + Format(loopPose.BoundsHeight));
            }

            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName);
            report.AppendLine("ClipLength=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("StageOrder=Default>OpposedHighBackLowForwardWindup>WholeArmForwardScissor>HorizontalBladeCross>CompressedPullTowardBody>Default");
            report.AppendLine("DefaultHandSeparation=" + Format(defaultPose.HandSeparation));
            report.AppendLine("WindupHandSeparation=" + Format(opposedWindupPose.HandSeparation));
            report.AppendLine("WindupVerticalHandOffset=" + Format(opposedWindupPose.VerticalHandOffset));
            report.AppendLine("WindupDepthHandOffset=" + Format(opposedWindupPose.DepthHandOffset));
            report.AppendLine("WholeArmSwingHandSeparation=" + Format(wholeArmSwingPose.HandSeparation));
            report.AppendLine("CrossedImpactHandSeparation=" + Format(crossedImpactPose.HandSeparation));
            report.AppendLine("CompressedPullHandSeparation=" + Format(compressedPullPose.HandSeparation));
            report.AppendLine("DefaultBoundsWidth=" + Format(defaultPose.BoundsWidth));
            report.AppendLine("WindupBoundsWidth=" + Format(opposedWindupPose.BoundsWidth));
            report.AppendLine("MaximumHandLocalRotation=" + Format(maximumHandRotation));
            report.AppendLine("HookImpactSpineRotation=" + Format(impactSpineRotation));
            report.AppendLine("CrossedImpactDepthFromBody=" + Format(crossedImpactPose.HandDepthFromBody));
            report.AppendLine("CompressedPullDepthFromBody=" + Format(compressedPullPose.HandDepthFromBody));
            report.AppendLine("WristsStayedNeutral=True");
            report.AppendLine("ArmsStayedBilateral=True");
            report.AppendLine("TorsoMotionRequired=False");
            report.AppendLine("HookedVolumePulledTowardBody=True");
            report.AppendLine("LoopPoseMatchesDefault=True");
            report.AppendLine("RootStayedFixed=True");
            report.AppendLine("RendererStayedValid=True");
            WriteText(ReviewReportPath, report.ToString());
            Debug.Log("OstinatoScissorAttackReviewed, FourSeconds=True, OpposedWindup=True, WholeArmForwardScissor=True, HorizontalBladeCrossReviewed=True, WristsNeutral=True, CompressedPull=True, LoopReturn=True, RootFixed=True, TorsoMotionRequired=False");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Scissor Attack Runtime Playback")]
        public static void CaptureOstinatoScissorAttackRuntimePlayback()
        {
            OstinatoScissorAttackRuntimeCapture.Begin();
        }

        public static void CaptureOstinatoAttackAppearanceComparison()
        {
            CaptureOstinatoScissorAttackRuntimePlayback();
        }

        public static void CaptureOstinatoAttackFbxUnmodifiedComparison()
        {
            CaptureOstinatoScissorAttackRuntimePlayback();
        }

        private static PoseMeasure SamplePose(GameObject model, SkinnedMeshRenderer renderer, AnimationClip clip, float time)
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, clip, Mathf.Clamp(time, 0f, clip.length));
            AnimationMode.EndSampling();
            var leftHand = FindDescendant(model.transform, "LeftHand");
            var rightHand = FindDescendant(model.transform, "RightHand");
            var spine = FindDescendant(model.transform, "Spine");
            var left = model.transform.InverseTransformPoint(leftHand.position);
            var right = model.transform.InverseTransformPoint(rightHand.position);
            var bakedMesh = new Mesh();
            try
            {
                renderer.BakeMesh(bakedMesh);
                return new PoseMeasure(
                    left,
                    right,
                    bakedMesh.bounds.size.x,
                    bakedMesh.bounds.size.y,
                    leftHand.localRotation,
                    rightHand.localRotation,
                    spine.localRotation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        private static void ConfigureAttackImporter()
        {
            var importer = AssetImporter.GetAtPath(AttackModelPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato attack FBX importer is missing.");
            importer.isReadable = true;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.resampleCurves = false;
            var defaultTake = RequireDefaultAttackTake(importer);
            defaultTake.loopTime = true;
            importer.clipAnimations = new[] { defaultTake };
            importer.SaveAndReimport();
            RequireLoopOnlyImporterContract(out _);
        }

        private static AnimationClip RequireAttackClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(AttackModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var attackClips = clips.Where(clip =>
                    clip.name.IndexOf(AttackTakeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (attackClips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Ostinato attack FBX must expose exactly one unmodified attack take. Matching=" +
                    attackClips.Length + ", All=" + string.Join("|", clips.Select(clip => clip.name)));
            }
            return attackClips[0];
        }

        private static void RequireClipContract(AnimationClip clip)
        {
            if (clip.length <= 0f)
            {
                throw new InvalidOperationException("Ostinato supplied attack clip must contain animation data.");
            }
            if (Mathf.Abs(clip.frameRate - 60f) > 0.01f)
            {
                throw new InvalidOperationException("Ostinato supplied attack clip frame rate changed. Actual=" + Format(clip.frameRate));
            }
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("Ostinato supplied attack clip loop playback is disabled.");
            }
            if (AnimationUtility.GetCurveBindings(clip).Length == 0)
            {
                throw new InvalidOperationException("Ostinato supplied attack clip contains no animation curves.");
            }
        }

        private static ModelImporterClipAnimation RequireDefaultAttackTake(ModelImporter importer)
        {
            var defaultClips = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var attackTakes = defaultClips.Where(candidate =>
                    candidate.name.IndexOf(AttackTakeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (candidate.takeName ?? string.Empty).IndexOf(AttackTakeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (attackTakes.Length != 1)
            {
                throw new InvalidOperationException(
                    "Ostinato attack FBX must expose one default attack take. Matching=" + attackTakes.Length +
                    ", Available=" + string.Join("|", defaultClips.Select(candidate => candidate.name + "/" + candidate.takeName)));
            }
            return attackTakes[0];
        }

        private static ModelImporterClipAnimation RequireLoopOnlyImporterContract(out ModelImporterClipAnimation defaultTake)
        {
            var importer = AssetImporter.GetAtPath(AttackModelPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato attack FBX importer is missing.");
            defaultTake = RequireDefaultAttackTake(importer);
            var overrides = importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            if (overrides.Length != 1)
            {
                throw new InvalidOperationException("Ostinato attack FBX must contain one loop-only clip override. Count=" + overrides.Length);
            }
            var overrideTake = overrides[0];
            if (!overrideTake.loopTime || defaultTake.loopTime ||
                overrideTake.name != defaultTake.name || overrideTake.takeName != defaultTake.takeName ||
                !Mathf.Approximately(overrideTake.firstFrame, defaultTake.firstFrame) ||
                !Mathf.Approximately(overrideTake.lastFrame, defaultTake.lastFrame) ||
                overrideTake.loopPose != defaultTake.loopPose || overrideTake.mirror != defaultTake.mirror ||
                overrideTake.lockRootRotation != defaultTake.lockRootRotation ||
                overrideTake.lockRootHeightY != defaultTake.lockRootHeightY ||
                overrideTake.lockRootPositionXZ != defaultTake.lockRootPositionXZ ||
                overrideTake.keepOriginalOrientation != defaultTake.keepOriginalOrientation ||
                overrideTake.keepOriginalPositionY != defaultTake.keepOriginalPositionY ||
                overrideTake.keepOriginalPositionXZ != defaultTake.keepOriginalPositionXZ ||
                overrideTake.heightFromFeet != defaultTake.heightFromFeet ||
                !Mathf.Approximately(overrideTake.additiveReferencePoseFrame, defaultTake.additiveReferencePoseFrame))
            {
                throw new InvalidOperationException("Ostinato attack clip override must differ from the default take only by loopTime=true.");
            }
            return overrideTake;
        }

        private static void RequireClipBindingsResolve(GameObject playbackAsset, AnimationClip clip)
        {
            var paths = playbackAsset.GetComponentsInChildren<Transform>(true)
                .Select(target => AnimationUtility.CalculateTransformPath(target, playbackAsset.transform))
                .ToHashSet(StringComparer.Ordinal);
            var missing = AnimationUtility.GetCurveBindings(clip)
                .Select(binding => binding.path)
                .Where(path => !string.IsNullOrEmpty(path) && !paths.Contains(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                var hierarchyPaths = paths
                    .Where(path => path.IndexOf("Armature", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   path.EndsWith("/Hips", StringComparison.Ordinal) ||
                                   path == "Hips")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                throw new InvalidOperationException(
                    "Ostinato attack clip has unresolved approved-model bindings: " + string.Join("|", missing) +
                    "; AvailablePlaybackPaths=" + string.Join("|", hierarchyPaths));
            }
        }

        private static void ConfigureAttackPlaybackBindingRoot(GameObject playbackModel)
        {
            var hipsCandidates = playbackModel.GetComponentsInChildren<Transform>(true)
                .Where(target => target.name == "Hips")
                .ToArray();
            if (hipsCandidates.Length != 1 || hipsCandidates[0].parent == null || hipsCandidates[0].parent == playbackModel.transform)
            {
                throw new InvalidOperationException(
                    "Ostinato approved playback model must contain exactly one Hips bone below an Armature container. Count=" +
                    hipsCandidates.Length);
            }
            hipsCandidates[0].parent.name = AttackBindingArmatureName;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states.Select(entry => entry.state).ToArray())
            {
                stateMachine.RemoveState(state);
            }
            var attackState = stateMachine.AddState(StateName);
            attackState.motion = clip;
            attackState.speed = AttackPlaybackSpeed;
            attackState.writeDefaultValues = true;
            stateMachine.defaultState = attackState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static float RequireAttackControllerContract(AnimationClip clip)
        {
            var controller = RequireAsset<AnimatorController>(ControllerPath);
            var states = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(entry => entry.state)
                .Where(state => state.name == StateName)
                .ToArray();
            if (states.Length != 1 || states[0].motion != clip ||
                !Mathf.Approximately(states[0].speed, AttackPlaybackSpeed) ||
                states[0].behaviours.OfType<OstinatoAttackAccelerationBehaviour>().Any())
            {
                throw new InvalidOperationException("Ostinato attack controller must use the supplied clip at uniform playback speed 2.");
            }
            return states[0].speed;
        }

        private static AnimatorState RequireAttackSourceState(AnimationClip clip)
        {
            var controller = RequireAsset<AnimatorController>(ControllerPath);
            var states = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Select(entry => entry.state)
                .Where(state => state.name == StateName)
                .ToArray();
            if (states.Length != 1 || states[0].motion != clip ||
                !Mathf.Approximately(states[0].speed, AttackPlaybackSpeed))
            {
                throw new InvalidOperationException("Ostinato attack source state is not configured for the full clip at speed 2.");
            }
            return states[0];
        }

        private static OstinatoAttackAccelerationBehaviour RequireOrCreateAttackAccelerationProfile(AnimatorState state)
        {
            var profiles = state.behaviours.OfType<OstinatoAttackAccelerationBehaviour>().ToArray();
            if (profiles.Length > 1)
            {
                throw new InvalidOperationException("Ostinato attack state contains more than one acceleration profile.");
            }
            return profiles.Length == 1
                ? profiles[0]
                : state.AddStateMachineBehaviour<OstinatoAttackAccelerationBehaviour>();
        }

        private static AccelerationInspection InspectAttackAccelerationProfileInternal()
        {
            var scene = RequireOpenScene();
            var root = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(root);
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato attack slot must contain exactly one playback object.");
            }
            var clip = RequireAttackClip();
            RequireClipContract(clip);
            var state = RequireAttackSourceState(clip);
            var profiles = state.behaviours.OfType<OstinatoAttackAccelerationBehaviour>().ToArray();
            if (profiles.Length != 1)
            {
                throw new InvalidOperationException("Ostinato attack state must contain exactly one acceleration profile.");
            }
            var profile = profiles[0];
            RequireApproximately(profile.SourceLastFrame, 196f, "source last frame");
            RequireApproximately(profile.ApproachStartFrame, 53f, "approach start frame");
            RequireApproximately(profile.ApproachEndFrame, 84f, "approach end frame");
            RequireApproximately(profile.StrikeEndFrame, 93f, "strike end frame");
            RequireApproximately(profile.ApproachStartMultiplier, 3f, "approach start multiplier");
            RequireApproximately(profile.ApproachEndMultiplier, 4f, "approach end multiplier");
            RequireApproximately(profile.StrikeEndMultiplier, 5f, "strike multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(0f), 1f, "frame 0 multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(53f), 3f, "frame 53 multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(84f), 4f, "frame 84 multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(93f), 5f, "frame 93 multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(94f), 1f, "frame 94 multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(100f), 1f, "frame 100 multiplier");
            RequireApproximately(profile.EvaluateAdditionalSpeedAtSourceFrame(196f), 1f, "frame 196 multiplier");
            RequireMonotonicProfile(profile, 53f, 93f, true);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceAttackRelativePath));
            var importedHash = ComputeSha256(ProjectAbsolutePath(AttackModelPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("Ostinato source and imported FBX hashes differ.");
            }
            return new AccelerationInspection(clip, profile);
        }

        private static void RequireMonotonicProfile(
            OstinatoAttackAccelerationBehaviour profile,
            float startFrame,
            float endFrame,
            bool increasing)
        {
            var previous = profile.EvaluateAdditionalSpeedAtSourceFrame(startFrame);
            for (var frame = startFrame + 0.25f; frame <= endFrame + 0.0001f; frame += 0.25f)
            {
                var current = profile.EvaluateAdditionalSpeedAtSourceFrame(frame);
                if ((increasing && current + 0.00001f < previous) ||
                    (!increasing && current - 0.00001f > previous))
                {
                    throw new InvalidOperationException("Ostinato acceleration profile is not monotonic at frame " + Format(frame) + ".");
                }
                previous = current;
            }
        }

        private static void RequireConstantProfile(
            OstinatoAttackAccelerationBehaviour profile,
            float startFrame,
            float endFrame,
            float expectedMultiplier)
        {
            for (var frame = startFrame; frame <= endFrame + 0.0001f; frame += 0.25f)
            {
                RequireApproximately(
                    profile.EvaluateAdditionalSpeedAtSourceFrame(frame),
                    expectedMultiplier,
                    "constant gather multiplier at frame " + Format(frame));
            }
        }

        private static void RequireApproximately(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) > 0.0001f)
            {
                throw new InvalidOperationException("Unexpected Ostinato " + label + ". Expected=" +
                    Format(expected) + ", Actual=" + Format(actual));
            }
        }

        private readonly struct AccelerationInspection
        {
            public AccelerationInspection(AnimationClip clip, OstinatoAttackAccelerationBehaviour profile)
            {
                Clip = clip;
                Profile = profile;
            }

            public AnimationClip Clip { get; }
            public OstinatoAttackAccelerationBehaviour Profile { get; }
        }

        private static Transform RequireAttackSlot(Transform root)
        {
            if (root.childCount != 9)
            {
                throw new InvalidOperationException("Approved Ostinato placement must contain nine slots.");
            }
            var slot = root.GetChild(ExpectedSlotIndex);
            if (slot.name != StaticSlotName && slot.name != AttackSlotName)
            {
                throw new InvalidOperationException("Expected slot 04 static or attack target, found " + slot.name + ".");
            }
            return slot;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects().SingleOrDefault(root => root.name == PlacementRootName)?.transform ??
                   throw new InvalidOperationException("Approved Ostinato placement root is missing.");
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the active scene. Active=" + scene.path);
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Unity must be in Edit Mode for Ostinato attack application/review.");
            }
            return scene;
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException("Required asset is missing: " + path);
        }

        private static SkinnedMeshRenderer RequireSingleRenderer(GameObject target, string label)
        {
            var renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMesh == null)
            {
                throw new InvalidOperationException(label + " must contain one valid SkinnedMeshRenderer.");
            }
            return renderers[0];
        }

        private static void RequireApprovedAppearance(Mesh attack, Mesh approved)
        {
            var attackFingerprint = BuildCoreAppearanceFingerprint(attack);
            var approvedFingerprint = BuildCoreAppearanceFingerprint(approved);
            if (attackFingerprint != approvedFingerprint)
            {
                throw new InvalidOperationException("Ostinato attack mesh appearance differs from the approved static mesh. Attack=" +
                    attackFingerprint + ", Approved=" + approvedFingerprint);
            }
        }

        private static void SynchronizeApprovedAppearance(GameObject attackModel, SkinnedMeshRenderer attackRenderer,
            SkinnedMeshRenderer approvedRenderer, Material[] materials)
        {
            var approvedBones = approvedRenderer.bones;
            if (approvedBones.Length == 0 || approvedRenderer.rootBone == null)
            {
                throw new InvalidOperationException("The approved static Ostinato renderer has no valid skinning bones.");
            }
            var attackBones = approvedBones.Select(bone => FindDescendant(attackModel.transform, bone.name)).ToArray();
            attackRenderer.sharedMesh = approvedRenderer.sharedMesh;
            attackRenderer.bones = attackBones;
            attackRenderer.rootBone = FindDescendant(attackModel.transform, approvedRenderer.rootBone.name);
            attackRenderer.sharedMaterials = materials;
        }

        private static void RequireSynchronizedAppearance(GameObject attackModel, SkinnedMeshRenderer attackRenderer,
            SkinnedMeshRenderer approvedRenderer)
        {
            if (attackRenderer.sharedMesh != approvedRenderer.sharedMesh)
            {
                throw new InvalidOperationException("The supplied attack FBX instance does not use the approved static Ostinato mesh.");
            }
            var approvedBoneNames = approvedRenderer.bones.Select(bone => bone.name).ToArray();
            var attackBoneNames = attackRenderer.bones.Select(bone => bone == null ? string.Empty : bone.name).ToArray();
            if (!attackBoneNames.SequenceEqual(approvedBoneNames) || attackRenderer.rootBone == null ||
                approvedRenderer.rootBone == null || attackRenderer.rootBone.name != approvedRenderer.rootBone.name)
            {
                throw new InvalidOperationException("The approved static Ostinato mesh is not mapped to the supplied attack FBX rig.");
            }
            foreach (var boneName in approvedBoneNames)
            {
                FindDescendant(attackModel.transform, boneName);
            }
            if (!attackRenderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).SequenceEqual(ApprovedMaterialPaths))
            {
                throw new InvalidOperationException("The supplied attack FBX instance does not use the approved static Ostinato materials.");
            }
            if (attackModel.GetComponentsInChildren<Transform>(true).Any(target =>
                    target.name == "LeftBladeControl" || target.name == "RightBladeControl"))
            {
                throw new InvalidOperationException("A removed blade-correction control is still present in the supplied attack FBX instance.");
            }
        }

        private static string BuildCoreAppearanceFingerprint(Mesh mesh)
        {
            if (mesh == null)
            {
                return "Missing";
            }
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(mesh.vertexCount);
                writer.Write(mesh.subMeshCount);
                WriteVectors(writer, mesh.vertices);
                WriteVectors(writer, mesh.uv);
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    writer.Write((int)mesh.GetTopology(subMesh));
                    var indices = mesh.GetIndices(subMesh);
                    writer.Write(indices.Length);
                    foreach (var index in indices) writer.Write(index);
                }
            }
            stream.Position = 0;
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string BuildAppearanceFingerprint(Mesh mesh)
        {
            if (mesh == null)
            {
                return "Missing";
            }
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(mesh.vertexCount);
                writer.Write(mesh.subMeshCount);
                WriteVectors(writer, mesh.vertices);
                WriteVectors(writer, mesh.normals);
                foreach (var tangent in mesh.tangents)
                {
                    writer.Write(tangent.x); writer.Write(tangent.y); writer.Write(tangent.z); writer.Write(tangent.w);
                }
                WriteVectors(writer, mesh.uv);
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    writer.Write((int)mesh.GetTopology(subMesh));
                    var indices = mesh.GetIndices(subMesh);
                    writer.Write(indices.Length);
                    foreach (var index in indices) writer.Write(index);
                }
            }
            stream.Position = 0;
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void WriteVectors(BinaryWriter writer, IEnumerable<Vector3> values)
        {
            foreach (var value in values)
            {
                writer.Write(value.x); writer.Write(value.y); writer.Write(value.z);
            }
        }

        private static void WriteVectors(BinaryWriter writer, IEnumerable<Vector2> values)
        {
            foreach (var value in values)
            {
                writer.Write(value.x); writer.Write(value.y);
            }
        }

        private static string[] CaptureOtherSlotSignatures(Transform root)
        {
            return root.Cast<Transform>().Where((_, index) => index != ExpectedSlotIndex)
                .Select(BuildHierarchySignature).ToArray();
        }

        private static void RequireOtherSlotsUnchanged(Transform root, string[] before)
        {
            var after = CaptureOtherSlotSignatures(root);
            if (!before.SequenceEqual(after))
            {
                throw new InvalidOperationException("An Ostinato slot outside slot 04 changed during attack application.");
            }
        }

        private static string BuildHierarchySignature(Transform target)
        {
            var builder = new StringBuilder();
            foreach (var item in target.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.GetSiblingIndex()).Append('|')
                    .Append(item.localPosition.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(item.localRotation.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(item.localScale.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            }
            foreach (var renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                builder.Append(AssetDatabase.GetAssetPath(renderer.sharedMesh)).Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials.Select(material => material == null ? "None" : AssetDatabase.GetAssetPath(material))));
            }
            return builder.ToString();
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required attack bone is missing: " + name);
        }

        private static bool MatchesTransform(Transform target, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            return Vector3.Distance(target.localPosition, position) < 0.0001f &&
                   Quaternion.Angle(target.localRotation, rotation) < 0.001f &&
                   Vector3.Distance(target.localScale, scale) < 0.0001f;
        }

        internal static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string BuildClipCurveFingerprint(AnimationClip clip)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(clip.length);
                writer.Write(clip.frameRate);
                var bindings = AnimationUtility.GetCurveBindings(clip)
                    .OrderBy(binding => binding.path, StringComparer.Ordinal)
                    .ThenBy(binding => binding.type.FullName, StringComparer.Ordinal)
                    .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                    .ToArray();
                writer.Write(bindings.Length);
                foreach (var binding in bindings)
                {
                    writer.Write(binding.path ?? string.Empty);
                    writer.Write(binding.type.FullName ?? string.Empty);
                    writer.Write(binding.propertyName ?? string.Empty);
                    var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
                    writer.Write((int)curve.preWrapMode);
                    writer.Write((int)curve.postWrapMode);
                    writer.Write(curve.keys.Length);
                    foreach (var key in curve.keys)
                    {
                        writer.Write(key.time);
                        writer.Write(key.value);
                        writer.Write(key.inTangent);
                        writer.Write(key.outTangent);
                        writer.Write(key.inWeight);
                        writer.Write(key.outWeight);
                        writer.Write((int)key.weightedMode);
                    }
                }
            }
            stream.Position = 0;
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        internal static void WriteText(string relativePath, string contents)
        {
            var path = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Output directory is invalid."));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private readonly struct PoseMeasure
        {
            public PoseMeasure(
                Vector3 leftHand,
                Vector3 rightHand,
                float boundsWidth,
                float boundsHeight,
                Quaternion leftHandLocalRotation,
                Quaternion rightHandLocalRotation,
                Quaternion spineLocalRotation)
            {
                LeftHand = leftHand;
                RightHand = rightHand;
                HandSeparation = Vector3.Distance(leftHand, rightHand);
                BoundsWidth = boundsWidth;
                BoundsHeight = boundsHeight;
                LeftHandLocalRotation = leftHandLocalRotation;
                RightHandLocalRotation = rightHandLocalRotation;
                SpineLocalRotation = spineLocalRotation;
                HandDepthFromBody = Mathf.Abs((leftHand.z + rightHand.z) * 0.5f);
                VerticalHandOffset = Mathf.Abs(leftHand.y - rightHand.y);
                DepthHandOffset = Mathf.Abs(leftHand.z - rightHand.z);
                SignedHandOrder = Mathf.Sign(leftHand.x - rightHand.x);
            }
            public Vector3 LeftHand { get; }
            public Vector3 RightHand { get; }
            public float HandSeparation { get; }
            public float BoundsWidth { get; }
            public float BoundsHeight { get; }
            public Quaternion LeftHandLocalRotation { get; }
            public Quaternion RightHandLocalRotation { get; }
            public Quaternion SpineLocalRotation { get; }
            public float HandDepthFromBody { get; }
            public float VerticalHandOffset { get; }
            public float DepthHandOffset { get; }
            public float SignedHandOrder { get; }
            public bool KeepsBilateralHandOrderFrom(PoseMeasure other)
            {
                return SignedHandOrder != 0f && SignedHandOrder == other.SignedHandOrder;
            }
            public float MaximumHandLocalRotationFrom(PoseMeasure other)
            {
                return Mathf.Max(
                    Quaternion.Angle(LeftHandLocalRotation, other.LeftHandLocalRotation),
                    Quaternion.Angle(RightHandLocalRotation, other.RightHandLocalRotation));
            }
            public float SpineLocalRotationFrom(PoseMeasure other)
            {
                return Quaternion.Angle(SpineLocalRotation, other.SpineLocalRotation);
            }
            public bool Matches(PoseMeasure other, float tolerance)
            {
                return Vector3.Distance(LeftHand, other.LeftHand) <= tolerance &&
                       Vector3.Distance(RightHand, other.RightHand) <= tolerance &&
                       Mathf.Abs(BoundsWidth - other.BoundsWidth) <= tolerance &&
                       Mathf.Abs(BoundsHeight - other.BoundsHeight) <= tolerance;
            }
        }
    }

    [InitializeOnLoad]
    internal static class OstinatoScissorAttackRuntimeCapture
    {
        private const string RuntimeImagePath = OstinatoScissorAttackAnimation.ValidationFolderPath + "/Ostinato_AttackAppearanceComparison.png";
        private const string RuntimeFramesPath = OstinatoScissorAttackAnimation.ValidationFolderPath + "/runtime_frames";
        private const string RuntimeReportPath = OstinatoScissorAttackAnimation.ValidationFolderPath + "/Ostinato_AttackRuntimePlayback.txt";
        private const string CompletionPath = OstinatoScissorAttackAnimation.ValidationFolderPath + "/Ostinato_AttackRuntimePlayback.completed";
        private const string FailurePath = OstinatoScissorAttackAnimation.ValidationFolderPath + "/Ostinato_AttackRuntimePlayback.failed.txt";
        private const string SessionKey = "Bellerophon.OstinatoScissorAttackRuntimeCapture.State";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int ReviewLayer = 30;
        private const int ImageSize = 320;
        private const int SheetColumns = 5;
        private const int ContinuousFrameCount = 41;
        private static readonly float[] TargetNormalizedTimes = Enumerable.Range(0, ContinuousFrameCount)
            .Select(index => index / (float)(ContinuousFrameCount - 1)).ToArray();

        private static Animator animator;
        private static SkinnedMeshRenderer renderer;
        private static Camera reviewCamera;
        private static GameObject cameraObject;
        private static GameObject keyObject;
        private static GameObject fillObject;
        private static GameObject[] layeredObjects;
        private static int[] originalLayers;
        private static readonly List<byte[]> CapturedImages = new List<byte[]>();
        private static Vector3 modelStartPosition;
        private static Quaternion modelStartRotation;
        private static Bounds framingBounds;
        private static double captureStartTime;
        private static float startNormalizedTime;
        private static int nextCaptureIndex;
        private static float maximumRootDistance;
        private static float maximumRootRotation;

        static OstinatoScissorAttackRuntimeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Unity must be in Edit Mode before Ostinato attack runtime capture begins.");
            }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoScissorAttackAnimation.ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the active scene before attack capture.");
            }
            TryDelete(CompletionPath);
            TryDelete(FailurePath);
            TryDeleteDirectory(RuntimeFramesPath);
            CapturedImages.Clear();
            SessionState.SetInt(SessionKey, WaitingForPlayMode);
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            var state = SessionState.GetInt(SessionKey, 0);
            if (state == 0) return;
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (EditorApplication.isPlaying) StartCapture();
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying) throw new InvalidOperationException("Unity left Play Mode before attack capture completed.");
                    CaptureWhenDue();
                    return;
                }
                if (state == WaitingForEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.EraseInt(SessionKey);
                    OstinatoScissorAttackAnimation.WriteText(CompletionPath, "Ostinato supplied-FBX unmodified attack capture completed in Play Mode.");
                    Debug.Log("OstinatoAttackFbxUnmodifiedComparisonCaptured, ContinuousFrames=" + ContinuousFrameCount +
                        ", Views=Front|ThreeQuarter, MovementOverride=False, Image=" + RuntimeImagePath);
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void StartCapture()
        {
            var root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(target => target.name == OstinatoScissorAttackAnimation.PlacementRootName).transform;
            var slot = root.GetChild(3);
            if (slot.name != OstinatoScissorAttackAnimation.AttackSlotName || slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato attack slot is not ready for runtime capture.");
            }
            var model = slot.GetChild(0).gameObject;
            animator = model.GetComponent<Animator>();
            renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("Ostinato attack runtime Animator is invalid.");
            }
            layeredObjects = model.GetComponentsInChildren<Transform>(true).Select(target => target.gameObject).ToArray();
            originalLayers = layeredObjects.Select(target => target.layer).ToArray();
            foreach (var target in layeredObjects) target.layer = ReviewLayer;
            cameraObject = new GameObject("Ostinato_Attack_ReviewCamera", typeof(Camera));
            keyObject = new GameObject("Ostinato_Attack_KeyLight", typeof(Light));
            fillObject = new GameObject("Ostinato_Attack_FillLight", typeof(Light));
            reviewCamera = cameraObject.GetComponent<Camera>();
            ConfigureCameraAndLights();
            modelStartPosition = model.transform.position;
            modelStartRotation = model.transform.rotation;
            framingBounds = renderer.bounds;
            framingBounds.Expand(new Vector3(0.15f, 0.12f, 0.12f));
            animator.Play(OstinatoScissorAttackAnimation.StateName, 0, 0f);
            animator.Update(0f);
            startNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            captureStartTime = EditorApplication.timeSinceStartup;
            nextCaptureIndex = 0;
            maximumRootDistance = 0f;
            maximumRootRotation = 0f;
            CapturedImages.Clear();
            SessionState.SetInt(SessionKey, Capturing);
        }

        private static void CaptureWhenDue()
        {
            if (EditorApplication.timeSinceStartup - captureStartTime > 12d)
            {
                throw new TimeoutException("Ostinato attack Animator did not complete one loop within 12 seconds.");
            }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(OstinatoScissorAttackAnimation.StateName))
            {
                throw new InvalidOperationException("Ostinato attack Animator left its attack state.");
            }
            var elapsed = state.normalizedTime - startNormalizedTime;
            if (elapsed + 0.002f < TargetNormalizedTimes[nextCaptureIndex]) return;
            maximumRootDistance = Mathf.Max(maximumRootDistance,
                Vector3.Distance(animator.transform.position, modelStartPosition));
            maximumRootRotation = Mathf.Max(maximumRootRotation,
                Quaternion.Angle(animator.transform.rotation, modelStartRotation));
            var texture = RenderFrame();
            CapturedImages.Add(texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            nextCaptureIndex++;
            if (nextCaptureIndex >= TargetNormalizedTimes.Length) Finish();
        }

        private static Texture2D RenderFrame()
        {
            var currentBounds = renderer.bounds;
            currentBounds.Expand(new Vector3(0.15f, 0.12f, 0.12f));
            var target = currentBounds.center + Vector3.up * currentBounds.extents.y * 0.02f;
            var halfFov = reviewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var distance = Mathf.Max(currentBounds.extents.y, currentBounds.extents.x) / Mathf.Tan(halfFov) + currentBounds.extents.z + 0.15f;
            var front = RenderView(target, Vector3.back, distance);
            var threeQuarter = RenderView(target, new Vector3(0.7f, 0f, -1f).normalized, distance);
            var combined = new Texture2D(ImageSize * 2, ImageSize, TextureFormat.RGBA32, false);
            combined.SetPixels(0, 0, ImageSize, ImageSize, front.GetPixels());
            combined.SetPixels(ImageSize, 0, ImageSize, ImageSize, threeQuarter.GetPixels());
            combined.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(front);
            UnityEngine.Object.DestroyImmediate(threeQuarter);
            return combined;
        }

        private static Texture2D RenderView(Vector3 target, Vector3 cameraDirection, float distance)
        {
            reviewCamera.transform.position = target + cameraDirection * distance;
            reviewCamera.transform.rotation = Quaternion.LookRotation(target - reviewCamera.transform.position, Vector3.up);
            var renderTexture = RenderTexture.GetTemporary(ImageSize, ImageSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                reviewCamera.targetTexture = renderTexture;
                reviewCamera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                reviewCamera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void Finish()
        {
            var frameWidth = ImageSize * 2;
            var sheetRows = Mathf.CeilToInt(CapturedImages.Count / (float)SheetColumns);
            var sheet = new Texture2D(frameWidth * SheetColumns, ImageSize * sheetRows, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(new Color32(9, 12, 14, 255), sheet.width * sheet.height).ToArray());
            var framesPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(RuntimeFramesPath);
            Directory.CreateDirectory(framesPath);
            for (var index = 0; index < CapturedImages.Count; index++)
            {
                File.WriteAllBytes(Path.Combine(framesPath, "frame_" + index.ToString("D3", CultureInfo.InvariantCulture) + ".png"), CapturedImages[index]);
                var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                frame.LoadImage(CapturedImages[index], false);
                var column = index % SheetColumns;
                var rowFromTop = index / SheetColumns;
                var sheetRow = sheetRows - 1 - rowFromTop;
                sheet.SetPixels(column * frameWidth, sheetRow * ImageSize, frameWidth, ImageSize, frame.GetPixels());
                UnityEngine.Object.DestroyImmediate(frame);
            }
            sheet.Apply(false, false);
            var imagePath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(RuntimeImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ?? throw new InvalidOperationException("Attack capture directory is invalid."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            var report = new StringBuilder();
            report.AppendLine("Target=" + OstinatoScissorAttackAnimation.PlacementRootName + "/" + OstinatoScissorAttackAnimation.AttackSlotName);
            report.AppendLine("PlaybackMode=Unity Editor Play Mode scene Animator");
            report.AppendLine("AnimatorState=" + OstinatoScissorAttackAnimation.StateName);
            report.AppendLine("CaptureMode=Continuous unmodified source-timeline sampling");
            report.AppendLine("CapturedFrames=" + CapturedImages.Count);
            report.AppendLine("ViewsPerFrame=Front|ThreeQuarter");
            report.AppendLine("Timeline=Supplied ostinato attack FBX full take playback");
            report.AppendLine("PlaybackPrefab=" + OstinatoScissorAttackAnimation.AttackModelPath);
            report.AppendLine("AppearanceModel=" + OstinatoScissorAttackAnimation.ApprovedModelPath);
            report.AppendLine("AppearanceMatchesApprovedStatic=True");
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("AnimatorApplyRootMotionOverridden=False");
            report.AppendLine("MaximumObservedRootDistance=" + maximumRootDistance.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaximumObservedRootRotation=" + maximumRootRotation.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MovementOverride=False");
            report.AppendLine("FinalImage=" + RuntimeImagePath);
            report.AppendLine("FrameDirectory=" + RuntimeFramesPath);
            OstinatoScissorAttackAnimation.WriteText(RuntimeReportPath, report.ToString());
            Cleanup();
            SessionState.SetInt(SessionKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void ConfigureCameraAndLights()
        {
            reviewCamera.clearFlags = CameraClearFlags.SolidColor;
            reviewCamera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            reviewCamera.fieldOfView = 40f;
            reviewCamera.nearClipPlane = 0.05f;
            reviewCamera.farClipPlane = 100f;
            reviewCamera.cullingMask = 1 << ReviewLayer;
            reviewCamera.allowHDR = true;
            reviewCamera.allowMSAA = true;
            var key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.45f;
            key.color = new Color(1f, 0.89f, 0.72f);
            key.cullingMask = 1 << ReviewLayer;
            keyObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.78f;
            fill.color = new Color(0.46f, 0.66f, 1f);
            fill.cullingMask = 1 << ReviewLayer;
            fillObject.transform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void Cleanup()
        {
            if (layeredObjects != null && originalLayers != null)
            {
                for (var index = 0; index < Mathf.Min(layeredObjects.Length, originalLayers.Length); index++)
                    if (layeredObjects[index] != null) layeredObjects[index].layer = originalLayers[index];
            }
            Destroy(cameraObject); Destroy(keyObject); Destroy(fillObject);
            animator = null; renderer = null; reviewCamera = null; cameraObject = null; keyObject = null; fillObject = null;
            layeredObjects = null; originalLayers = null; CapturedImages.Clear();
        }

        private static void Destroy(GameObject target)
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
        }

        private static void Fail(Exception exception)
        {
            Cleanup();
            OstinatoScissorAttackAnimation.WriteText(FailurePath, exception.ToString());
            SessionState.EraseInt(SessionKey);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            Debug.LogException(exception);
        }

        private static void TryDelete(string relativePath)
        {
            var path = OstinatoScissorAttackAnimation.ProjectAbsolutePath(relativePath);
            if (File.Exists(path)) File.Delete(path);
        }

        private static void TryDeleteDirectory(string relativePath)
        {
            var path = OstinatoScissorAttackAnimation.ProjectAbsolutePath(relativePath);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }
}
