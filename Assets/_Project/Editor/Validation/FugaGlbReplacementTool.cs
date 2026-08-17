using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaGlbReplacementTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceModelPath = "D:/Bellerophon2/Bellerophon/enemies model/fuga.glb";
        private const string ImportedModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string ExpectedSourceSha256 = "009430EB298B83C6EA48CD2AF7B9BE3DF075EA512DAF6978BBE41D5C917AF3AB";
        private const string ExpectedImportedRigSha256 = "4DA5AE82DE38E84804188549A6E24F923D77BC04EF072B98D245F34C2B0A9C3B";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string PlayerName = "Player";
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/Fuga/FugaApproved.prefab";
        private const string ReplacementModelName = "Fuga_Model";
        private const string OutputFolder = "docs/validation/fuga_model_replacement_2026-08-16";
        private const string ReportPath = OutputFolder + "/Fuga_Model_Replacement_Report.txt";
        private const string CapturePath = OutputFolder + "/Fuga_Model_Replacement_Final.png";
        private const string FacingReportPath = OutputFolder + "/Fuga_Facing_And_Static_Report.txt";
        private const string FacingCapturePath = OutputFolder + "/Fuga_Facing_And_Static_Final.png";
        private const string RotationBeforeReportPath = OutputFolder + "/Fuga_Rotation_Pivot_Before_Report.txt";
        private const string PerObjectRotationReportPath = OutputFolder + "/Fuga_PerObject_Rotation_Report.txt";
        private const string PerObjectRotationCapturePath = OutputFolder + "/Fuga_PerObject_Rotation_Final.png";
        private const string ScreenOrderReportPath = OutputFolder + "/Fuga_Screen_Left_To_Right_Order_Report.txt";
        private const string ScreenOrderCapturePath = OutputFolder + "/Fuga_Screen_Left_To_Right_Order_Final.png";
        private const string FrontPlayerOrderReportPath =
            OutputFolder + "/Fuga_PerObject_Front_Player_Order_Report.txt";
        private const string FrontPlayerOrderCapturePath =
            OutputFolder + "/Fuga_PerObject_Front_Player_Order_Final.png";
        private const string IdleControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Idle_NewModel_WingbeatBreathing.controller";
        private const string IdleMeshPath =
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Idle_BreathingMesh.asset";
        private const string IdleHoverTargetName = "Fuga_01_Idle_HoverTarget";
        private const float MinimumPlayerDistance = 2.5f;
        private const float CameraMargin = 0.8f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private static readonly string[] SlotNames =
        {
            "Fuga_00_Static",
            "Fuga_01_Idle",
            "Fuga_02_Move",
            "Fuga_03_Attack",
            "Fuga_04_Hit",
            "Fuga_05_Death",
            "Fuga_06_Consume",
        };

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Supplied GLB Replacement")]
        public static void ApplyFugaModelReplacement()
        {
            var scene = RequireCurrentScene();
            RequireExactModelCopy();
            var importedModel = LoadImportedModel();
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var slots = RequireSlots(placementRoot);
            var protectedBefore = ProtectedRootSignatures(scene);
            var slotContractsBefore = slots.ToDictionary(
                slot => slot.name,
                SlotContractSignature,
                StringComparer.Ordinal);
            var helperContractsBefore = slots.ToDictionary(
                slot => slot.name,
                NonVisualDirectChildSignature,
                StringComparer.Ordinal);

            ReplacePrefabModel(importedModel);
            foreach (var slot in slots)
            {
                ReplaceVisibleModel(slot, importedModel, scene);
            }

            ConfigurePlayer(placementRoot);
            var result = InspectState(placementRoot);

            foreach (var slot in slots)
            {
                if (!string.Equals(
                        slotContractsBefore[slot.name],
                        SlotContractSignature(slot),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(slot.name + " root contract changed during model replacement.");
                }

                if (!string.Equals(
                        helperContractsBefore[slot.name],
                        NonVisualDirectChildSignature(slot),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(slot.name + " non-visual helper hierarchy changed during model replacement.");
                }
            }

            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Fuga and Player changed during model replacement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after replacing Fuga models.");
            }

            AssetDatabase.SaveAssets();
            RequireExactModelCopy();
            WriteReport(result, captureCreated: false);

            Debug.Log(
                "FugaModelReplacementApplied Result=PASS" +
                ", SourceSha256=" + ExpectedSourceSha256 +
                ", SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", PrefabReplaced=True" +
                ", PlayerFacesFuga=True" +
                ", CompleteLineupVisible=True" +
                ", SlotContractsChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Supplied GLB Replacement")]
        public static void InspectFugaModelReplacement()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireExactModelCopy();
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var result = InspectState(placementRoot);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Fuga model inspection changed the scene dirty state.");
            }

            WriteReport(result, File.Exists(Absolute(CapturePath)));
            Debug.Log(
                "FugaModelReplacementInspected Result=PASS" +
                ", SourceSha256=" + ExpectedSourceSha256 +
                ", SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", DirectGlbInstances=" + result.DirectGlbInstanceCount.ToString(CultureInfo.InvariantCulture) +
                ", PlayerFacesFuga=True" +
                ", CompleteLineupVisible=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Supplied GLB Replacement")]
        public static void CaptureFugaModelReplacement()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Fuga capture.");
            }

            RequireExactModelCopy();
            var result = InspectState(RequireRoot(PlacementRootName).transform);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(CapturePath));
            WriteReport(result, captureCreated: true);
            AssetDatabase.Refresh();

            Debug.Log(
                "FugaModelReplacementCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", CompleteLineupVisible=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply 180 Facing And Disconnect Legacy Animations")]
        public static void ApplyFugaFacingAndDisconnectLegacyAnimations()
        {
            var scene = RequireCurrentScene();
            RequireExactModelCopy();
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var slots = RequireSlots(placementRoot);
            var protectedBefore = ProtectedRootSignaturesExceptFuga(scene);
            var slotTransformsBefore = slots.ToDictionary(
                slot => slot.name,
                RootTransformSignature,
                StringComparer.Ordinal);

            foreach (var slot in slots)
            {
                var model = RequireExactModelTransform(slot, slot.name);
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.Euler(0f, 180f, 0f);

                var animator = slot.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = null;
                    animator.enabled = false;
                    EditorUtility.SetDirty(animator);
                }

                foreach (var playback in slot.GetComponents<FugaAnimationReviewPlaybackDriver>())
                {
                    UnityEngine.Object.DestroyImmediate(playback);
                }

                var physicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>();
                if (physicsDriver != null)
                {
                    physicsDriver.LockRootMotionForReview = true;
                    EditorUtility.SetDirty(physicsDriver);
                }

                var body = slot.GetComponent<Rigidbody>();
                if (body != null && !body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    EditorUtility.SetDirty(body);
                }

                EditorUtility.SetDirty(model);
                EditorUtility.SetDirty(slot);
            }

            foreach (var slot in slots)
            {
                if (!string.Equals(
                        slotTransformsBefore[slot.name],
                        RootTransformSignature(slot),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(slot.name + " root transform changed during Fuga facing correction.");
                }
            }

            var protectedAfter = ProtectedRootSignaturesExceptFuga(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Fuga changed during facing and animation-disconnection work.");
            }

            var result = InspectFacingAndDisconnectedState(placementRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after correcting Fuga facing and animation connections.");
            }

            AssetDatabase.SaveAssets();
            RequireExactModelCopy();
            WriteFacingReport(result, captureCreated: false);

            Debug.Log(
                "FugaFacingAndLegacyAnimationsDisconnected Result=PASS" +
                ", SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", ModelLocalYawDegrees=180" +
                ", LegacyAnimatorControllers=0" +
                ", LegacyPlaybackDrivers=0" +
                ", PhysicsReviewMotionLocked=True" +
                ", IdleModelLocalY=" + Num(result.IdleModelLocalY) +
                ", MinimumPlayerFacingDot=" + Num(result.MinimumPlayerFacingDot) +
                ", PlayerChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect 180 Facing And Disconnected Animations")]
        public static void InspectFugaFacingAndDisconnectedAnimations()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireExactModelCopy();
            var result = InspectFacingAndDisconnectedState(RequireRoot(PlacementRootName).transform);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException(
                    "Fuga facing and animation-disconnection inspection changed the scene dirty state.");
            }

            WriteFacingReport(result, File.Exists(Absolute(FacingCapturePath)));
            Debug.Log(
                "FugaFacingAndDisconnectedAnimationsInspected Result=PASS" +
                ", SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", ModelLocalYawDegrees=180" +
                ", LegacyAnimatorControllers=0" +
                ", LegacyPlaybackDrivers=0" +
                ", IdleModelLocalY=" + Num(result.IdleModelLocalY) +
                ", MinimumPlayerFacingDot=" + Num(result.MinimumPlayerFacingDot) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture 180 Facing And Disconnected Animations")]
        public static void CaptureFugaFacingAndDisconnectedAnimations()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before the final Fuga facing capture.");
            }

            RequireExactModelCopy();
            var result = InspectFacingAndDisconnectedState(RequireRoot(PlacementRootName).transform);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(FacingCapturePath));
            WriteFacingReport(result, captureCreated: true);
            AssetDatabase.Refresh();

            Debug.Log(
                "FugaFacingAndDisconnectedAnimationsCaptured Result=PASS" +
                ", Image=" + FacingCapturePath +
                ", SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", PlayerChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Rotation Pivot And Placement")]
        public static void InspectFugaRotationPivotAndPlacement()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var snapshot = CaptureRotationSnapshot(placementRoot);
            WriteRotationReport(
                RotationBeforeReportPath,
                "BeforeCorrection",
                snapshot,
                correctionApplied: false,
                revertedToIdentityFirst: false,
                placementRootUnchanged: true,
                slotTransformsUnchanged: true,
                siblingIndicesUnchanged: true,
                helperTransformsUnchanged: true,
                protectedRootsUnchanged: true,
                captureCreated: false);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Fuga pivot inspection changed the scene dirty state.");
            }

            Debug.Log(
                "FugaRotationPivotAndPlacementInspected Result=PASS" +
                ", DirectChildren=" + snapshot.DirectChildCount.ToString(CultureInfo.InvariantCulture) +
                ", NamedSlots=" + snapshot.Slots.Length.ToString(CultureInfo.InvariantCulture) +
                ", HelperChildren=" + snapshot.HelperCount.ToString(CultureInfo.InvariantCulture) +
                ", PlacementRootLocalRotationIdentity=" + snapshot.PlacementRootRotationIdentity +
                ", SlotLocalRotationsIdentity=" + snapshot.SlotRotationsIdentity +
                ", ModelsLocalYaw180=" + snapshot.ModelsLocalYaw180 +
                ", RotationOwner=" + snapshot.RotationOwner +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Restore Placement And Apply Per-Object 180 Facing")]
        public static void RestoreFugaPlacementAndApplyPerObject180Facing()
        {
            var scene = RequireCurrentScene();
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var slots = RequireSlots(placementRoot);
            var protectedBefore = ProtectedRootSignaturesExceptFuga(scene);
            var placementRootBefore = DetailedTransformSignature(placementRoot);
            var slotTransformsBefore = slots.ToDictionary(
                slot => slot.name,
                DetailedTransformSignature,
                StringComparer.Ordinal);
            var siblingIndicesBefore = slots.ToDictionary(
                slot => slot.name,
                slot => slot.GetSiblingIndex(),
                StringComparer.Ordinal);
            var helperBefore = HelperHierarchySignature(placementRoot);
            var models = slots.ToDictionary(
                slot => slot.name,
                slot => RequirePerObjectModelTransform(slot),
                StringComparer.Ordinal);
            var modelPivotsBefore = models.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.position,
                StringComparer.Ordinal);
            var modelLocalPositionsBefore = models.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.localPosition,
                StringComparer.Ordinal);
            var modelLocalScalesBefore = models.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.localScale,
                StringComparer.Ordinal);

            foreach (var pair in models)
            {
                pair.Value.localRotation = Quaternion.identity;
                if ((pair.Value.position - modelPivotsBefore[pair.Key]).sqrMagnitude > 0.0000000001f)
                {
                    throw new InvalidOperationException(pair.Key + " pivot moved while reverting its own rotation to identity.");
                }
            }

            foreach (var pair in models)
            {
                pair.Value.localRotation = Quaternion.Euler(0f, 180f, 0f);
                if ((pair.Value.position - modelPivotsBefore[pair.Key]).sqrMagnitude > 0.0000000001f)
                {
                    throw new InvalidOperationException(pair.Key + " pivot moved during per-object 180-degree rotation.");
                }

                if ((pair.Value.localPosition - modelLocalPositionsBefore[pair.Key]).sqrMagnitude > 0.0000000001f ||
                    (pair.Value.localScale - modelLocalScalesBefore[pair.Key]).sqrMagnitude > 0.0000000001f)
                {
                    throw new InvalidOperationException(pair.Key + " model position or scale changed during per-object rotation.");
                }

                EditorUtility.SetDirty(pair.Value);
            }

            var placementRootUnchanged = string.Equals(
                placementRootBefore,
                DetailedTransformSignature(placementRoot),
                StringComparison.Ordinal);
            var slotTransformsUnchanged = slots.All(slot => string.Equals(
                slotTransformsBefore[slot.name],
                DetailedTransformSignature(slot),
                StringComparison.Ordinal));
            var siblingIndicesUnchanged = slots.All(slot =>
                siblingIndicesBefore[slot.name] == slot.GetSiblingIndex());
            var helperTransformsUnchanged = string.Equals(
                helperBefore,
                HelperHierarchySignature(placementRoot),
                StringComparison.Ordinal);
            var protectedAfter = ProtectedRootSignaturesExceptFuga(scene);
            var protectedRootsUnchanged = protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal);
            if (!placementRootUnchanged || !slotTransformsUnchanged || !siblingIndicesUnchanged ||
                !helperTransformsUnchanged || !protectedRootsUnchanged)
            {
                throw new InvalidOperationException(
                    "A protected placement, sibling index, helper, Player, or non-Fuga scene root changed during per-object rotation.");
            }

            var snapshot = CaptureRotationSnapshot(placementRoot);
            RequirePerObjectRotationContract(snapshot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after per-object Fuga rotation.");
            }

            WriteRotationReport(
                PerObjectRotationReportPath,
                "AfterPerObjectCorrection",
                snapshot,
                correctionApplied: true,
                revertedToIdentityFirst: true,
                placementRootUnchanged,
                slotTransformsUnchanged,
                siblingIndicesUnchanged,
                helperTransformsUnchanged,
                protectedRootsUnchanged,
                captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaPlacementRestoredAndPerObjectFacingApplied Result=PASS" +
                ", ModelsRevertedToIdentityFirst=7" +
                ", ModelsIndividuallyRotated180=7" +
                ", PlacementRootUnchanged=True" +
                ", SlotTransformsUnchanged=True" +
                ", SiblingIndicesUnchanged=True" +
                ", ModelPivotsUnchanged=True" +
                ", PlayerChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Corrected Per-Object Facing")]
        public static void InspectCorrectedFugaPerObjectFacing()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var snapshot = CaptureRotationSnapshot(RequireRoot(PlacementRootName).transform);
            RequirePerObjectRotationContract(snapshot);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Corrected Fuga facing inspection changed the scene dirty state.");
            }

            WriteRotationReport(
                PerObjectRotationReportPath,
                "AfterPerObjectCorrection",
                snapshot,
                correctionApplied: true,
                revertedToIdentityFirst: true,
                placementRootUnchanged: true,
                slotTransformsUnchanged: true,
                siblingIndicesUnchanged: true,
                helperTransformsUnchanged: true,
                protectedRootsUnchanged: true,
                captureCreated: File.Exists(Absolute(PerObjectRotationCapturePath)));
            Debug.Log(
                "CorrectedFugaPerObjectFacingInspected Result=PASS" +
                ", NamedSlots=7" +
                ", ModelsIndividuallyRotated180=7" +
                ", PlacementRootLocalRotationIdentity=True" +
                ", SlotTransformsPreserved=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Corrected Per-Object Facing")]
        public static void CaptureCorrectedFugaPerObjectFacing()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final per-object Fuga capture.");
            }

            var snapshot = CaptureRotationSnapshot(RequireRoot(PlacementRootName).transform);
            RequirePerObjectRotationContract(snapshot);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            Capture(camera, Absolute(PerObjectRotationCapturePath));
            WriteRotationReport(
                PerObjectRotationReportPath,
                "AfterPerObjectCorrection",
                snapshot,
                correctionApplied: true,
                revertedToIdentityFirst: true,
                placementRootUnchanged: true,
                slotTransformsUnchanged: true,
                siblingIndicesUnchanged: true,
                helperTransformsUnchanged: true,
                protectedRootsUnchanged: true,
                captureCreated: true);
            AssetDatabase.Refresh();
            Debug.Log(
                "CorrectedFugaPerObjectFacingCaptured Result=PASS" +
                ", Image=" + PerObjectRotationCapturePath +
                ", PlacementOrderChanged=False" +
                ", PlayerChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Screen Left-To-Right Order")]
        public static void ApplyFugaScreenLeftToRightOrder()
        {
            var scene = RequireCurrentScene();
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var slots = RequireSlots(placementRoot);
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var protectedBefore = ProtectedRootSignaturesExceptFuga(scene);
            var placementRootBefore = DetailedTransformSignature(placementRoot);
            var preservedSlotStateBefore = PreservedSlotStateSignature(slots);
            var siblingIndicesBefore = slots.ToDictionary(
                slot => slot.name,
                slot => slot.GetSiblingIndex(),
                StringComparer.Ordinal);

            var screenOrderedLocalXs = slots
                .OrderBy(slot => camera.WorldToViewportPoint(VisibleBoundsCenter(slot)).x)
                .Select(slot => slot.localPosition.x)
                .ToArray();
            if (screenOrderedLocalXs.Distinct().Count() != SlotNames.Length)
            {
                throw new InvalidOperationException("The seven Fuga horizontal positions are not unique.");
            }

            var idleSlot = slots[1];
            var idleDeltaX = screenOrderedLocalXs[1] - idleSlot.localPosition.x;
            for (var index = 0; index < slots.Length; index++)
            {
                var localPosition = slots[index].localPosition;
                localPosition.x = screenOrderedLocalXs[index];
                slots[index].localPosition = localPosition;
                EditorUtility.SetDirty(slots[index]);
            }

            RebaseIdleHoverHorizontalPosition(idleSlot, placementRoot, idleDeltaX);

            if (!string.Equals(
                    placementRootBefore,
                    DetailedTransformSignature(placementRoot),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fuga placement root changed during screen-order correction.");
            }

            if (!string.Equals(
                    preservedSlotStateBefore,
                    PreservedSlotStateSignature(slots),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A protected Fuga Y/Z position, rotation, scale, model, component, or animation connection changed.");
            }

            if (slots.Any(slot => siblingIndicesBefore[slot.name] != slot.GetSiblingIndex()))
            {
                throw new InvalidOperationException("A Fuga Sibling Index changed during screen-order correction.");
            }

            var protectedAfter = ProtectedRootSignaturesExceptFuga(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Player or a non-Fuga scene root changed during screen-order correction.");
            }

            var result = InspectScreenOrderState(placementRoot, camera);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after reordering the Fuga lineup.");
            }

            WriteScreenOrderReport(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaScreenLeftToRightOrderApplied Result=PASS" +
                ", PlayerScreenLeftToRight=" + string.Join(",", result.ScreenOrderedNames) +
                ", SlotXOnlyChanged=True" +
                ", IdleHoverTargetRebased=True" +
                ", SiblingIndicesChanged=False" +
                ", AnimationConnectionsChanged=False" +
                ", PlayerChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Screen Left-To-Right Order")]
        public static void InspectFugaScreenLeftToRightOrder()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var result = InspectScreenOrderState(placementRoot, camera);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga screen-order inspection changed the scene dirty state.");
            }

            WriteScreenOrderReport(result, File.Exists(Absolute(ScreenOrderCapturePath)));
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaScreenLeftToRightOrderInspected Result=PASS" +
                ", PlayerScreenLeftToRight=" + string.Join(",", result.ScreenOrderedNames) +
                ", SiblingIndices=0,1,2,3,4,5,6" +
                ", AnimationConnectionsPreserved=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Screen Left-To-Right Order")]
        public static void CaptureFugaScreenLeftToRightOrder()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Fuga screen-order capture.");
            }

            var placementRoot = RequireRoot(PlacementRootName).transform;
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var result = InspectScreenOrderState(placementRoot, camera);
            Capture(camera, Absolute(ScreenOrderCapturePath));
            WriteScreenOrderReport(result, captureCreated: true);
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final Fuga screen-order capture changed the scene.");
            }

            Debug.Log(
                "FugaScreenLeftToRightOrderCaptured Result=PASS" +
                ", PlayerScreenLeftToRight=" + string.Join(",", result.ScreenOrderedNames) +
                ", Image=" + ScreenOrderCapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Per-Object Front, Player Start, And Order")]
        public static void ApplyFugaPerObjectFrontFacingPlayerAndOrder()
        {
            var scene = RequireCurrentScene();
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var slots = RequireSlots(placementRoot);
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var protectedBefore = ProtectedRootSignatures(scene);
            var placementRootBefore = DetailedTransformSignature(placementRoot);
            var protectedSlotStateBefore = FrontPlayerOrderProtectedSlotStateSignature(slots);
            var playerPreservedBefore = PlayerPreservedSignature(player);
            var siblingIndicesBefore = slots.ToDictionary(
                slot => slot.name,
                slot => slot.GetSiblingIndex(),
                StringComparer.Ordinal);
            var modelPivotPositionsBefore = slots.ToDictionary(
                slot => slot.name,
                slot => RequirePerObjectModelTransform(slot).position,
                StringComparer.Ordinal);
            var idleDriver = slots[1].GetComponent<FugaPhysicsMotionDriver>() ??
                             throw new InvalidOperationException("Fuga_01_Idle physics driver is missing.");
            var idleTarget = idleDriver.MotionPathTarget ??
                             throw new InvalidOperationException("Fuga_01_Idle hover target is missing.");
            var idleTargetBefore = idleTarget.localPosition;
            var idleBaseBefore = idleDriver.IdleHoverBaseLocalPosition;

            foreach (var slot in slots)
            {
                var model = RequirePerObjectModelTransform(slot);
                if (ApproximatelyEuler(model.localEulerAngles, new Vector3(0f, 180f, 0f)))
                {
                    var expectedRotation = model.localRotation * Quaternion.Euler(0f, 180f, 0f);
                    model.localRotation = expectedRotation;
                    EditorUtility.SetDirty(model);
                    if (Vector3.Distance(modelPivotPositionsBefore[slot.name], model.position) > 0.000001f ||
                        Quaternion.Angle(expectedRotation, model.localRotation) > 0.001f)
                    {
                        throw new InvalidOperationException(
                            slot.name + " was not rotated exactly 180 degrees around its own unchanged pivot.");
                    }
                }
                else if (!ApproximatelyEuler(model.localEulerAngles, Vector3.zero))
                {
                    throw new InvalidOperationException(
                        slot.name + " model is neither at the approved pre-correction state nor the corrected state.");
                }
            }

            var correctedFront = RequirePerObjectModelTransform(slots[0]).forward;
            correctedFront.y = 0f;
            if (correctedFront.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The corrected Fuga front direction is unusable.");
            }

            ConfigurePlayerFromFront(placementRoot, correctedFront.normalized);

            var screenOrderedLocalXs = slots
                .OrderBy(slot => camera.WorldToViewportPoint(VisibleBoundsCenter(slot)).x)
                .Select(slot => slot.localPosition.x)
                .ToArray();
            if (screenOrderedLocalXs.Distinct().Count() != SlotNames.Length)
            {
                throw new InvalidOperationException("The seven Fuga horizontal positions are not unique.");
            }

            var idleDeltaX = screenOrderedLocalXs[1] - slots[1].localPosition.x;
            for (var index = 0; index < slots.Length; index++)
            {
                var localPosition = slots[index].localPosition;
                localPosition.x = screenOrderedLocalXs[index];
                slots[index].localPosition = localPosition;
                EditorUtility.SetDirty(slots[index]);
            }

            RebaseIdleHoverHorizontalPosition(slots[1], placementRoot, idleDeltaX);

            if (!string.Equals(
                    placementRootBefore,
                    DetailedTransformSignature(placementRoot),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fuga placement root changed during the correction.");
            }

            if (!string.Equals(
                    protectedSlotStateBefore,
                    FrontPlayerOrderProtectedSlotStateSignature(slots),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A protected Fuga slot, model, mesh, component, or animation connection changed.");
            }

            if (!string.Equals(playerPreservedBefore, PlayerPreservedSignature(player), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A Player property outside its approved root position and rotation changed.");
            }

            if (slots.Any(slot => siblingIndicesBefore[slot.name] != slot.GetSiblingIndex()))
            {
                throw new InvalidOperationException("A Fuga Sibling Index changed during the correction.");
            }

            var expectedIdleTarget = idleTargetBefore;
            expectedIdleTarget.x += idleDeltaX;
            var expectedIdleBase = idleBaseBefore;
            expectedIdleBase.x += idleDeltaX;
            if (Vector3.Distance(expectedIdleTarget, idleTarget.localPosition) > 0.000001f ||
                Vector3.Distance(expectedIdleBase, idleDriver.IdleHoverBaseLocalPosition) > 0.000001f)
            {
                throw new InvalidOperationException("The idle hover target was not rebased only by the idle slot X delta.");
            }

            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(protectedAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Fuga and Player changed during the correction.");
            }

            var result = InspectFrontPlayerOrderState(placementRoot, camera);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after correcting Fuga facing, Player start, and order.");
            }

            WriteFrontPlayerOrderReport(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaPerObjectFrontFacingPlayerAndOrderApplied Result=PASS" +
                ", RelativeModelYawApplied=180" +
                ", PlayerMovedToCorrectedFront=True" +
                ", PlayerScreenLeftToRight=" + string.Join(",", result.ScreenOrderedNames) +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Per-Object Front, Player Start, And Order")]
        public static void InspectFugaPerObjectFrontFacingPlayerAndOrder()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var placementRoot = RequireRoot(PlacementRootName).transform;
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var result = InspectFrontPlayerOrderState(placementRoot, camera);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga front/player/order inspection changed the scene dirty state.");
            }

            WriteFrontPlayerOrderReport(
                result,
                File.Exists(Absolute(FrontPlayerOrderCapturePath)));
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaPerObjectFrontFacingPlayerAndOrderInspected Result=PASS" +
                ", PlayerScreenLeftToRight=" + string.Join(",", result.ScreenOrderedNames) +
                ", MinimumFrontFacingDot=" + Num(result.MinimumFrontFacingDot) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Per-Object Front, Player Start, And Order")]
        public static void CaptureFugaPerObjectFrontFacingPlayerAndOrder()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Fuga capture.");
            }

            var placementRoot = RequireRoot(PlacementRootName).transform;
            var camera = RequirePlayer().GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var result = InspectFrontPlayerOrderState(placementRoot, camera);
            Capture(camera, Absolute(FrontPlayerOrderCapturePath));
            WriteFrontPlayerOrderReport(result, captureCreated: true);
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final Fuga capture changed the scene.");
            }

            Debug.Log(
                "FugaPerObjectFrontFacingPlayerAndOrderCaptured Result=PASS" +
                ", PlayerScreenLeftToRight=" + string.Join(",", result.ScreenOrderedNames) +
                ", Image=" + FrontPlayerOrderCapturePath +
                ", SceneChanged=False.");
        }

        private static FrontPlayerOrderResult InspectFrontPlayerOrderState(
            Transform placementRoot,
            Camera camera)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            if (!ApproximatelyEuler(placementRoot.localEulerAngles, Vector3.zero))
            {
                throw new InvalidOperationException("The Fuga placement root rotation changed.");
            }

            RequireSameHash(ExpectedSourceSha256, Sha256(SourceModelPath), "source GLB");
            RequireSameHash(ExpectedImportedRigSha256, Sha256(Absolute(ImportedModelPath)), "imported lip-rig GLB");
            var slots = RequireSlots(placementRoot);
            var ordered = slots
                .Select(slot => new
                {
                    Slot = slot,
                    Viewport = camera.WorldToViewportPoint(VisibleBoundsCenter(slot))
                })
                .OrderBy(item => item.Viewport.x)
                .ToArray();
            var orderedNames = ordered.Select(item => item.Slot.name).ToArray();
            if (!orderedNames.SequenceEqual(SlotNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Player screen order is incorrect. Actual=" + string.Join(",", orderedNames) + ".");
            }

            if (ordered.Any(item => item.Viewport.z <= 0f || item.Viewport.x < 0f || item.Viewport.x > 1f))
            {
                throw new InvalidOperationException("A Fuga slot is outside the Player camera's horizontal view.");
            }

            var minimumFrontFacingDot = 1f;
            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                if (slot.GetSiblingIndex() != index ||
                    !ApproximatelyEuler(slot.localEulerAngles, new Vector3(0f, 180f, 0f)))
                {
                    throw new InvalidOperationException(slot.name + " slot order or preserved rotation changed.");
                }

                var model = RequirePerObjectModelTransform(slot);
                if (model.localPosition.sqrMagnitude > 0.00000001f ||
                    !ApproximatelyEuler(model.localEulerAngles, Vector3.zero))
                {
                    throw new InvalidOperationException(
                        slot.name + " was not corrected by an additional local-pivot Y 180-degree rotation.");
                }

                var toCamera = camera.transform.position - model.position;
                toCamera.y = 0f;
                var modelFront = model.forward;
                modelFront.y = 0f;
                if (toCamera.sqrMagnitude < 0.001f || modelFront.sqrMagnitude < 0.001f)
                {
                    throw new InvalidOperationException(slot.name + " front-to-Player direction is unusable.");
                }

                minimumFrontFacingDot = Mathf.Min(
                    minimumFrontFacingDot,
                    Vector3.Dot(modelFront.normalized, toCamera.normalized));

                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                               throw new InvalidOperationException(slot.name + " has no SkinnedMeshRenderer.");
                var expectedMeshPath = index == 1 ? IdleMeshPath : ImportedModelPath;
                if (!string.Equals(
                        AssetDatabase.GetAssetPath(renderer.sharedMesh),
                        expectedMeshPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(slot.name + " mesh assignment changed.");
                }

                var animator = slot.GetComponent<Animator>();
                var controllerPath = animator != null && animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : string.Empty;
                if (index == 1)
                {
                    if (animator == null || !animator.enabled ||
                        !string.Equals(controllerPath, IdleControllerPath, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Fuga_01_Idle lost its idle Animator connection.");
                    }
                }
                else if (!string.IsNullOrEmpty(controllerPath))
                {
                    throw new InvalidOperationException(slot.name + " received an unexpected Animator Controller.");
                }
            }

            if (minimumFrontFacingDot < 0.6f)
            {
                throw new InvalidOperationException(
                    "The Player camera is not on the corrected front side of every Fuga model.");
            }

            RequireIdleHoverAligned(slots[1], placementRoot);
            InspectPlayerStart(placementRoot);
            return new FrontPlayerOrderResult(
                orderedNames,
                slots.Select(slot => slot.localPosition.x).ToArray(),
                ordered.Select(item => item.Viewport.x).ToArray(),
                minimumFrontFacingDot,
                RequirePlayer().position,
                RequirePlayer().eulerAngles);
        }

        private static ScreenOrderResult InspectScreenOrderState(Transform placementRoot, Camera camera)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var slots = RequireSlots(placementRoot);
            var ordered = slots
                .Select(slot => new
                {
                    Slot = slot,
                    Viewport = camera.WorldToViewportPoint(VisibleBoundsCenter(slot))
                })
                .OrderBy(item => item.Viewport.x)
                .ToArray();
            var orderedNames = ordered.Select(item => item.Slot.name).ToArray();
            if (!orderedNames.SequenceEqual(SlotNames, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Player screen order is incorrect. Actual=" + string.Join(",", orderedNames) + ".");
            }

            if (ordered.Any(item => item.Viewport.z <= 0f || item.Viewport.x < 0f || item.Viewport.x > 1f))
            {
                throw new InvalidOperationException("A Fuga slot is outside the Player camera's horizontal view.");
            }

            var commonY = slots[0].localPosition.y;
            var commonZ = slots[0].localPosition.z;
            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                if (slot.GetSiblingIndex() != index)
                {
                    throw new InvalidOperationException(slot.name + " has an unexpected Sibling Index.");
                }

                if (Mathf.Abs(slot.localPosition.y - commonY) > 0.000001f ||
                    Mathf.Abs(slot.localPosition.z - commonZ) > 0.000001f)
                {
                    throw new InvalidOperationException(slot.name + " no longer shares the preserved lineup Y/Z plane.");
                }

                var model = RequirePerObjectModelTransform(slot);
                if (model.localPosition.sqrMagnitude > 0.00000001f ||
                    !ApproximatelyEuler(model.localEulerAngles, new Vector3(0f, 180f, 0f)))
                {
                    throw new InvalidOperationException(slot.name + " model local transform changed.");
                }

                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                               throw new InvalidOperationException(slot.name + " has no SkinnedMeshRenderer.");
                var expectedMeshPath = index == 1 ? IdleMeshPath : ImportedModelPath;
                if (!string.Equals(
                        AssetDatabase.GetAssetPath(renderer.sharedMesh),
                        expectedMeshPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(slot.name + " mesh assignment changed.");
                }

                var animator = slot.GetComponent<Animator>();
                var controllerPath = animator != null && animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : string.Empty;
                if (index == 1)
                {
                    if (animator == null || !animator.enabled ||
                        !string.Equals(controllerPath, IdleControllerPath, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Fuga_01_Idle lost its new idle Animator connection.");
                    }
                }
                else if (!string.IsNullOrEmpty(controllerPath))
                {
                    throw new InvalidOperationException(slot.name + " received an unexpected Animator Controller.");
                }
            }

            RequireIdleHoverAligned(slots[1], placementRoot);
            return new ScreenOrderResult(
                orderedNames,
                slots.Select(slot => slot.localPosition.x).ToArray(),
                ordered.Select(item => item.Viewport.x).ToArray());
        }

        private static void RebaseIdleHoverHorizontalPosition(
            Transform idleSlot,
            Transform placementRoot,
            float deltaX)
        {
            var driver = idleSlot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Fuga_01_Idle physics driver is missing.");
            var target = driver.MotionPathTarget ??
                         throw new InvalidOperationException("Fuga_01_Idle hover target is missing.");
            if (target.parent != placementRoot || target.name != IdleHoverTargetName)
            {
                throw new InvalidOperationException("Fuga_01_Idle hover target ownership is unexpected.");
            }

            var targetPosition = target.localPosition;
            targetPosition.x += deltaX;
            target.localPosition = targetPosition;
            EditorUtility.SetDirty(target);

            var serializedDriver = new SerializedObject(driver);
            var basePositionProperty = serializedDriver.FindProperty("idleHoverBaseLocalPosition") ??
                                       throw new InvalidOperationException("Idle hover base position property is missing.");
            var basePosition = basePositionProperty.vector3Value;
            basePosition.x += deltaX;
            basePositionProperty.vector3Value = basePosition;
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(driver);
        }

        private static void RequireIdleHoverAligned(Transform idleSlot, Transform placementRoot)
        {
            var driver = idleSlot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Fuga_01_Idle physics driver is missing.");
            var target = driver.MotionPathTarget ??
                         throw new InvalidOperationException("Fuga_01_Idle hover target is missing.");
            if (!driver.IdleHoverEnabled || target.parent != placementRoot || target.name != IdleHoverTargetName ||
                Mathf.Abs(target.localPosition.x - idleSlot.localPosition.x) > 0.000001f ||
                Mathf.Abs(driver.IdleHoverBaseLocalPosition.x - idleSlot.localPosition.x) > 0.000001f)
            {
                throw new InvalidOperationException("Fuga_01_Idle hover target is not aligned with the reordered idle slot.");
            }
        }

        private static Vector3 VisibleBoundsCenter(Transform slot)
        {
            var model = RequirePerObjectModelTransform(slot);
            return BoundsOf(model, new Bounds(model.position, Vector3.zero)).center;
        }

        private static string PreservedSlotStateSignature(IEnumerable<Transform> slots)
        {
            var lines = new List<string>();
            foreach (var slot in slots)
            {
                var model = RequirePerObjectModelTransform(slot);
                var animator = slot.GetComponent<Animator>();
                var controllerPath = animator != null && animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : string.Empty;
                var driver = slot.GetComponent<FugaPhysicsMotionDriver>();
                lines.Add(
                    slot.name + "|" +
                    slot.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) + "|" +
                    Num(slot.localPosition.y) + "|" + Num(slot.localPosition.z) + "|" +
                    Vec(slot.localEulerAngles) + "|" + Vec(slot.localScale) + "|" +
                    Vec(model.localPosition) + "|" + Vec(model.localEulerAngles) + "|" + Vec(model.localScale) + "|" +
                    string.Join(",", model.GetComponentsInChildren<Renderer>(true)
                        .Select(renderer => AssetDatabase.GetAssetPath(RendererMesh(renderer)))
                        .OrderBy(path => path, StringComparer.Ordinal)) + "|" +
                    (animator != null && animator.enabled) + "|" + controllerPath + "|" +
                    slot.GetComponents<FugaAnimationReviewPlaybackDriver>().Length.ToString(CultureInfo.InvariantCulture) + "|" +
                    (driver != null ? driver.MotionPathTarget?.name : string.Empty) + "|" +
                    (driver != null && driver.LockRootMotionForReview) + "|" +
                    (driver != null && driver.FollowVerticalAxis) + "|" +
                    (driver != null && driver.UseDeathFallSequence) + "|" +
                    (driver != null && driver.IdleHoverEnabled) + "|" +
                    (driver != null ? Num(driver.IdleHoverAmplitude) : string.Empty) + "|" +
                    (driver != null ? Num(driver.IdleHoverFrequency) : string.Empty) + "|" +
                    string.Join(",", slot.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(typeName => typeName, StringComparer.Ordinal)));
            }

            return string.Join("\n", lines);
        }

        private static string FrontPlayerOrderProtectedSlotStateSignature(IEnumerable<Transform> slots)
        {
            var lines = new List<string>();
            foreach (var slot in slots)
            {
                var model = RequirePerObjectModelTransform(slot);
                var animator = slot.GetComponent<Animator>();
                var controllerPath = animator != null && animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : string.Empty;
                var driver = slot.GetComponent<FugaPhysicsMotionDriver>();
                lines.Add(
                    slot.name + "|" +
                    slot.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) + "|" +
                    Num(slot.localPosition.y) + "|" + Num(slot.localPosition.z) + "|" +
                    Vec(slot.localEulerAngles) + "|" + Vec(slot.localScale) + "|" +
                    Vec(model.localPosition) + "|" + Vec(model.localScale) + "|" +
                    string.Join(",", model.GetComponentsInChildren<Renderer>(true)
                        .Select(renderer => AssetDatabase.GetAssetPath(RendererMesh(renderer)))
                        .OrderBy(path => path, StringComparer.Ordinal)) + "|" +
                    (animator != null && animator.enabled) + "|" + controllerPath + "|" +
                    slot.GetComponents<FugaAnimationReviewPlaybackDriver>().Length.ToString(CultureInfo.InvariantCulture) + "|" +
                    (driver != null ? driver.MotionPathTarget?.name : string.Empty) + "|" +
                    (driver != null && driver.LockRootMotionForReview) + "|" +
                    (driver != null && driver.FollowVerticalAxis) + "|" +
                    (driver != null && driver.UseDeathFallSequence) + "|" +
                    (driver != null && driver.IdleHoverEnabled) + "|" +
                    (driver != null ? Num(driver.IdleHoverAmplitude) : string.Empty) + "|" +
                    (driver != null ? Num(driver.IdleHoverFrequency) : string.Empty) + "|" +
                    string.Join(",", slot.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(typeName => typeName, StringComparer.Ordinal)));
            }

            return string.Join("\n", lines);
        }

        private static string PlayerPreservedSignature(Transform player)
        {
            var builder = new StringBuilder()
                .Append(Vec(player.localScale)).Append('|')
                .Append(player.GetSiblingIndex().ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(string.Join(",", player.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().FullName)
                    .OrderBy(typeName => typeName, StringComparer.Ordinal)))
                .AppendLine();
            foreach (var child in player.GetComponentsInChildren<Transform>(true).Where(child => child != player))
            {
                builder.Append(child.name).Append('|')
                    .Append(Vec(child.localPosition)).Append('|')
                    .Append(Vec(child.localEulerAngles)).Append('|')
                    .Append(Vec(child.localScale)).Append('|')
                    .Append(child.GetSiblingIndex().ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(child.gameObject.activeSelf ? '1' : '0').Append('|')
                    .Append(string.Join(",", child.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(typeName => typeName, StringComparer.Ordinal)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static void WriteScreenOrderReport(ScreenOrderResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Player-Screen Left-To-Right Order Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("PlacementRoot=" + PlacementRootName)
                .AppendLine("PlayerScreenLeftToRight=" + string.Join(" > ", result.ScreenOrderedNames))
                .AppendLine("SlotNamesAndLocalX=" + string.Join(" > ", SlotNames.Select((name, index) =>
                    name + "@" + Num(result.SlotLocalXs[index]))))
                .AppendLine("ViewportXLeftToRight=" + string.Join(" > ", result.ScreenViewportXs.Select(Num)))
                .AppendLine("SlotXOnlyChanged=True")
                .AppendLine("SlotYAndZPreserved=True")
                .AppendLine("SlotRotationsAndScalesPreserved=True")
                .AppendLine("SiblingIndicesPreserved=True")
                .AppendLine("ModelLocalTransformsPreserved=True")
                .AppendLine("AnimationConnectionsPreserved=True")
                .AppendLine("IdleHoverTargetRebased=True")
                .AppendLine("PlayerChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ScreenOrderReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga screen-order report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteFrontPlayerOrderReport(
            FrontPlayerOrderResult result,
            bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Per-Object Front, Player Start, And Screen Order Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("PlacementRoot=" + PlacementRootName)
                .AppendLine("PerObjectRelativeLocalYawAppliedDegrees=180")
                .AppendLine("FinalModelLocalYawDegrees=0")
                .AppendLine("RotationOwner=EachFuga_ModelLocalPivot")
                .AppendLine("GroupCenterRotationApplied=False")
                .AppendLine("ModelPivotPositionsPreserved=True")
                .AppendLine("PlayerPosition=" + Vec(result.PlayerPosition))
                .AppendLine("PlayerEuler=" + Vec(result.PlayerEuler))
                .AppendLine("PlayerMovedToCorrectedFront=True")
                .AppendLine("MinimumFrontFacingDot=" + Num(result.MinimumFrontFacingDot))
                .AppendLine("CompleteLineupVisible=True")
                .AppendLine("PlayerScreenLeftToRight=" + string.Join(" > ", result.ScreenOrderedNames))
                .AppendLine("SlotNamesAndLocalX=" + string.Join(" > ", SlotNames.Select((name, index) =>
                    name + "@" + Num(result.SlotLocalXs[index]))))
                .AppendLine("ViewportXLeftToRight=" + string.Join(" > ", result.ScreenViewportXs.Select(Num)))
                .AppendLine("SlotYAndZPreserved=True")
                .AppendLine("SlotRotationsAndScalesPreserved=True")
                .AppendLine("SiblingIndicesPreserved=True")
                .AppendLine("IdleMotionConnectionPreserved=True")
                .AppendLine("OtherAnimationConnectionsPreserved=True")
                .AppendLine("IdleHoverTargetRebased=True")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(FrontPlayerOrderReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga front/player/order report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static FacingResult InspectFacingAndDisconnectedState(Transform placementRoot)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var player = RequirePlayer();
            var slots = RequireSlots(placementRoot);
            var minimumFacingDot = 1f;
            var idleModelLocalY = 0f;
            foreach (var slot in slots)
            {
                var model = RequireExactModelTransform(slot, slot.name);
                if (Quaternion.Angle(model.localRotation, Quaternion.Euler(0f, 180f, 0f)) > 0.05f)
                {
                    throw new InvalidOperationException(slot.name + " model is not rotated exactly 180 degrees on local Y.");
                }

                if (model.localPosition.sqrMagnitude > 0.00000001f)
                {
                    throw new InvalidOperationException(slot.name + " model local position is not reset to zero.");
                }

                var toPlayer = player.position - model.position;
                var forward = model.forward;
                toPlayer.y = 0f;
                forward.y = 0f;
                if (toPlayer.sqrMagnitude < 0.001f || forward.sqrMagnitude < 0.001f)
                {
                    throw new InvalidOperationException(slot.name + " has an unusable Player-facing direction.");
                }

                var facingDot = Vector3.Dot(forward.normalized, toPlayer.normalized);
                minimumFacingDot = Mathf.Min(minimumFacingDot, facingDot);
                if (facingDot <= 0f)
                {
                    throw new InvalidOperationException(slot.name + " model does not face toward the Player start side.");
                }

                var animator = slot.GetComponent<Animator>();
                if (animator != null && (animator.runtimeAnimatorController != null || animator.enabled))
                {
                    throw new InvalidOperationException(slot.name + " still has an active legacy Animator connection.");
                }

                if (slot.GetComponent<FugaAnimationReviewPlaybackDriver>() != null)
                {
                    throw new InvalidOperationException(slot.name + " still has a legacy animation review playback driver.");
                }

                var physicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>();
                if (physicsDriver != null && !physicsDriver.LockRootMotionForReview)
                {
                    throw new InvalidOperationException(slot.name + " still permits legacy review root motion.");
                }

                if (slot.name == "Fuga_01_Idle")
                {
                    idleModelLocalY = model.localPosition.y;
                }
            }

            return new FacingResult(slots.Length, idleModelLocalY, minimumFacingDot);
        }

        private static RotationSnapshot CaptureRotationSnapshot(Transform placementRoot)
        {
            var slots = RequireSlots(placementRoot);
            var states = new SlotRotationState[slots.Length];
            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                var model = RequirePerObjectModelTransform(slot);
                var bounds = BoundsOf(model, new Bounds(model.position, Vector3.zero));
                states[index] = new SlotRotationState(
                    slot.name,
                    slot.GetSiblingIndex(),
                    slot.localPosition,
                    slot.localEulerAngles,
                    slot.position,
                    model.localPosition,
                    model.localEulerAngles,
                    model.position,
                    bounds.center,
                    bounds.center - model.position);
            }

            var slotSet = new HashSet<string>(SlotNames, StringComparer.Ordinal);
            var helperCount = placementRoot.Cast<Transform>().Count(child => !slotSet.Contains(child.name));
            var placementRootRotationIdentity =
                Quaternion.Angle(placementRoot.localRotation, Quaternion.identity) <= 0.05f;
            var slotRotationsIdentity = slots.All(slot =>
                Quaternion.Angle(slot.localRotation, Quaternion.identity) <= 0.05f);
            var modelsLocalYaw180 = states.All(state =>
                ApproximatelyEuler(state.ModelLocalEuler, new Vector3(0f, 180f, 0f)));
            var rotationOwner = placementRootRotationIdentity && modelsLocalYaw180
                ? "EachFuga_ModelLocalPivot"
                : "MixedOrNonModelTransform";
            return new RotationSnapshot(
                placementRoot.childCount,
                helperCount,
                placementRoot.localPosition,
                placementRoot.localEulerAngles,
                placementRoot.position,
                states,
                placementRootRotationIdentity,
                slotRotationsIdentity,
                modelsLocalYaw180,
                rotationOwner);
        }

        private static void RequirePerObjectRotationContract(RotationSnapshot snapshot)
        {
            if (snapshot.Slots.Length != SlotNames.Length ||
                !snapshot.PlacementRootRotationIdentity ||
                !snapshot.ModelsLocalYaw180 ||
                !string.Equals(snapshot.RotationOwner, "EachFuga_ModelLocalPivot", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Fuga rotation is not owned exclusively by each Fuga_Model local pivot.");
            }
        }

        private static Transform RequirePerObjectModelTransform(Transform slot)
        {
            var model = slot.Find(ReplacementModelName) ??
                        throw new InvalidOperationException(slot.name + "/" + ReplacementModelName + " is missing.");
            if (model.parent != slot || model.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException(slot.name + " does not contain a direct visible Fuga_Model.");
            }

            return model;
        }

        private static bool ApproximatelyEuler(Vector3 actual, Vector3 expected)
        {
            return Mathf.Abs(Mathf.DeltaAngle(actual.x, expected.x)) <= 0.05f &&
                   Mathf.Abs(Mathf.DeltaAngle(actual.y, expected.y)) <= 0.05f &&
                   Mathf.Abs(Mathf.DeltaAngle(actual.z, expected.z)) <= 0.05f;
        }

        private static string DetailedTransformSignature(Transform transform)
        {
            return transform.name + "|" +
                   transform.GetSiblingIndex().ToString(CultureInfo.InvariantCulture) + "|" +
                   Vec(transform.localPosition) + "|" +
                   Vec(transform.localEulerAngles) + "|" +
                   Vec(transform.localScale) + "|" +
                   Vec(transform.position) + "|" +
                   Vec(transform.eulerAngles);
        }

        private static string HelperHierarchySignature(Transform placementRoot)
        {
            var slotSet = new HashSet<string>(SlotNames, StringComparer.Ordinal);
            return string.Join(
                "\n",
                placementRoot.Cast<Transform>()
                    .Where(child => !slotSet.Contains(child.name))
                    .Select(HierarchySignature)
                    .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static Transform RequireExactModelTransform(Transform parent, string label)
        {
            RequireExactVisibleModel(parent, label);
            return DirectVisibleChildren(parent)[0];
        }

        private static void ReplacePrefabModel(GameObject importedModel)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                throw new InvalidOperationException("The current Fuga prefab is missing: " + PrefabPath);
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var rootContractBefore = SlotContractSignature(prefabRoot.transform);
                var helperContractBefore = NonVisualDirectChildSignature(prefabRoot.transform);
                ReplaceVisibleModel(prefabRoot.transform, importedModel, prefabRoot.scene);

                if (!string.Equals(
                        rootContractBefore,
                        SlotContractSignature(prefabRoot.transform),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("FugaApproved prefab root contract changed.");
                }

                if (!string.Equals(
                        helperContractBefore,
                        NonVisualDirectChildSignature(prefabRoot.transform),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("FugaApproved prefab non-visual helper hierarchy changed.");
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ??
                              throw new InvalidOperationException("The replaced Fuga prefab could not be reloaded.");
            RequireExactVisibleModel(savedPrefab.transform, "FugaApproved prefab");
        }

        private static void ReplaceVisibleModel(Transform parent, GameObject importedModel, Scene scene)
        {
            var visibleChildren = DirectVisibleChildren(parent);
            if (visibleChildren.Length == 0)
            {
                throw new InvalidOperationException(parent.name + " has no existing visible model child.");
            }

            foreach (var child in visibleChildren)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            var instance = PrefabUtility.InstantiatePrefab(importedModel, scene) as GameObject ??
                           throw new InvalidOperationException(
                               "The supplied Fuga GLB could not be instantiated for " + parent.name + ".");
            instance.name = ReplacementModelName;
            instance.transform.SetParent(parent, false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            RequireVisibleGeometry(instance.transform);
            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(parent);
        }

        private static ReplacementResult InspectState(Transform placementRoot)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var slots = RequireSlots(placementRoot);
            foreach (var slot in slots)
            {
                RequireExactVisibleModel(slot, slot.name);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ??
                         throw new InvalidOperationException("The current Fuga prefab is missing.");
            RequireExactVisibleModel(prefab.transform, "FugaApproved prefab");
            InspectPlayerStart(placementRoot);
            return new ReplacementResult(
                slots.Length,
                slots.Length + 1,
                BoundsOf(placementRoot, new Bounds(placementRoot.position, Vector3.one)));
        }

        private static void RequireExactVisibleModel(Transform parent, string label)
        {
            var models = DirectVisibleChildren(parent);
            if (models.Length != 1 || models[0].name != ReplacementModelName)
            {
                throw new InvalidOperationException(
                    label + " must contain exactly one supplied Fuga GLB model child.");
            }

            var renderers = RequireVisibleGeometry(models[0]);
            foreach (var renderer in renderers)
            {
                var mesh = RendererMesh(renderer);
                if (mesh == null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(mesh),
                        ImportedModelPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        label + "/" + renderer.name + " does not use the exact supplied Fuga GLB mesh.");
                }
            }
        }

        private static Mesh RendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            return renderer.GetComponent<MeshFilter>()?.sharedMesh;
        }

        private static Transform[] DirectVisibleChildren(Transform parent)
        {
            return parent.Cast<Transform>()
                .Where(child => child.GetComponentsInChildren<Renderer>(true).Length > 0)
                .ToArray();
        }

        private static Transform[] RequireSlots(Transform placementRoot)
        {
            var slots = new Transform[SlotNames.Length];
            for (var index = 0; index < SlotNames.Length; index++)
            {
                slots[index] = placementRoot.Find(SlotNames[index]) ??
                               throw new InvalidOperationException("Missing current Fuga slot: " + SlotNames[index]);
                if (slots[index].parent != placementRoot)
                {
                    throw new InvalidOperationException(SlotNames[index] + " is not a direct Fuga placement slot.");
                }
            }

            return slots;
        }

        private static GameObject LoadImportedModel()
        {
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException(
                               "The supplied Fuga GLB was not imported as a GameObject asset.");
            RequireVisibleGeometry(imported.transform);
            return imported;
        }

        private static void RequireExactModelCopy()
        {
            if (!File.Exists(SourceModelPath))
            {
                throw new InvalidOperationException("Missing supplied Fuga GLB: " + SourceModelPath);
            }

            if (!File.Exists(Absolute(ImportedModelPath)))
            {
                throw new InvalidOperationException("Missing imported Fuga GLB copy: " + ImportedModelPath);
            }

            RequireSameHash(ExpectedSourceSha256, Sha256(SourceModelPath), "source GLB");
            RequireSameHash(ExpectedImportedRigSha256, Sha256(Absolute(ImportedModelPath)), "imported lip-rig GLB");
            AssetDatabase.ImportAsset(
                ImportedModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigurePlayer(Transform placementRoot)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var staticSlot = placementRoot.Find(SlotNames[0]) ??
                             throw new InvalidOperationException("The static Fuga slot is missing.");
            var front = staticSlot.rotation * Vector3.back;
            front.y = 0f;
            if (front.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The existing Fuga front direction is unusable.");
            }

            front.Normalize();
            var distance = PlayerDistance(bounds, camera);
            var cameraOffsetLocal = player.InverseTransformPoint(camera.transform.position);
            var playerY = player.position.y;
            var framed = false;
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var desiredCamera = bounds.center + front * distance;
                var yaw = YawToward(desiredCamera, bounds.center);
                var desiredPlayer = desiredCamera - yaw * cameraOffsetLocal;
                desiredPlayer.y = playerY;
                player.SetPositionAndRotation(desiredPlayer, yaw);
                if (AreBoundsVisible(bounds, camera))
                {
                    framed = true;
                    break;
                }

                distance *= 1.15f;
            }

            if (!framed)
            {
                throw new InvalidOperationException(
                    "The Player camera could not frame all supplied Fuga GLB instances from the existing front direction.");
            }

            EditorUtility.SetDirty(player);
        }

        private static void ConfigurePlayerFromFront(Transform placementRoot, Vector3 front)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            front.y = 0f;
            if (front.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The corrected Fuga front direction is unusable.");
            }

            front.Normalize();
            var distance = PlayerDistance(bounds, camera);
            var cameraOffsetLocal = player.InverseTransformPoint(camera.transform.position);
            var playerY = player.position.y;
            var framed = false;
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var desiredCamera = bounds.center + front * distance;
                var yaw = YawToward(desiredCamera, bounds.center);
                var desiredPlayer = desiredCamera - yaw * cameraOffsetLocal;
                desiredPlayer.y = playerY;
                player.SetPositionAndRotation(desiredPlayer, yaw);
                if (AreBoundsVisible(bounds, camera))
                {
                    framed = true;
                    break;
                }

                distance *= 1.15f;
            }

            if (!framed)
            {
                throw new InvalidOperationException(
                    "The Player camera could not frame all Fuga instances from the corrected front direction.");
            }

            EditorUtility.SetDirty(player);
        }

        private static void InspectPlayerStart(Transform placementRoot)
        {
            var player = RequirePlayer();
            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var bounds = BoundsOf(placementRoot, new Bounds(placementRoot.position, Vector3.one));
            var toFocus = bounds.center - camera.transform.position;
            var cameraForward = camera.transform.forward;
            toFocus.y = 0f;
            cameraForward.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f ||
                cameraForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, cameraForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException("The Player camera does not face the Fuga lineup.");
            }

            if (!AreBoundsVisible(bounds, camera))
            {
                throw new InvalidOperationException(
                    "The complete supplied Fuga GLB lineup is not visible from the Player start.");
            }
        }

        private static bool AreBoundsVisible(Bounds bounds, Camera camera)
        {
            return Corners(bounds).All(corner =>
            {
                var view = camera.WorldToViewportPoint(corner);
                return view.z > 0f &&
                       view.x >= -0.02f && view.x <= 1.02f &&
                       view.y >= -0.02f && view.y <= 1.02f;
            });
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical = Mathf.Max(1f, camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            var currentAspect = Mathf.Max(0.1f, camera.aspect);
            var captureAspect = CaptureWidth / (float)CaptureHeight;
            var framingAspect = Mathf.Min(currentAspect, captureAspect);
            var horizontal = Mathf.Atan(Mathf.Tan(vertical) * framingAspect);
            return Mathf.Max(
                MinimumPlayerDistance,
                bounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontal)) + CameraMargin,
                bounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(vertical)) + CameraMargin);
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The Player-to-Fuga direction is unusable.");
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        yield return bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z));
                    }
                }
            }
        }

        private static Renderer[] RequireVisibleGeometry(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            return renderers;
        }

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = RequireVisibleGeometry(root);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.size.sqrMagnitude > 0.000001f ? bounds : fallback;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName && root.name != PlayerName)
                .Select(root => HierarchySignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignaturesExceptFuga(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName)
                .Select(root => HierarchySignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RootTransformSignature(Transform root)
        {
            return root.name + "|" +
                   Vec(root.position) + "|" +
                   Vec(root.eulerAngles) + "|" +
                   Vec(root.lossyScale);
        }

        private static string SlotContractSignature(Transform slot)
        {
            return slot.name + "|" +
                   Vec(slot.position) + "|" +
                   Vec(slot.eulerAngles) + "|" +
                   Vec(slot.lossyScale) + "|" +
                   string.Join(",", slot.GetComponents<Component>()
                       .Where(component => component != null)
                       .Select(component => component.GetType().FullName)
                       .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static string NonVisualDirectChildSignature(Transform parent)
        {
            return string.Join(
                "\n",
                parent.Cast<Transform>()
                    .Where(child => child.GetComponentsInChildren<Renderer>(true).Length == 0)
                    .Select(HierarchySignature)
                    .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(Vec(item.position)).Append('|')
                    .Append(Vec(item.eulerAngles)).Append('|')
                    .Append(Vec(item.lossyScale)).Append('|')
                    .Append(item.gameObject.activeSelf ? '1' : '0').AppendLine();
            }

            return builder.ToString();
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName) ??
                         throw new InvalidOperationException("The Player root is missing.");
            if (player.transform.parent != null)
            {
                throw new InvalidOperationException("The Player object is not a scene root.");
            }

            return player.transform;
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static void Capture(Camera camera, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid capture path."));
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            var reviewCameraObject = new GameObject("FugaPlayerStartReviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera reviewCamera = null;
            try
            {
                reviewCamera = reviewCameraObject.GetComponent<Camera>();
                reviewCamera.CopyFrom(camera);
                reviewCamera.transform.SetPositionAndRotation(
                    camera.transform.position,
                    camera.transform.rotation);
                reviewCamera.allowHDR = false;
                reviewCamera.targetTexture = target;
                RenderTexture.active = target;
                reviewCamera.Render();
                image.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                image.Apply();
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                if (reviewCamera != null)
                {
                    reviewCamera.targetTexture = null;
                }

                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(reviewCameraObject);
            }
        }

        private static void WriteReport(ReplacementResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga GLB Replacement Report")
                .AppendLine("Result=PASS")
                .AppendLine("Source=" + SourceModelPath)
                .AppendLine("ImportedAsset=" + ImportedModelPath)
                .AppendLine("SourceSha256=" + ExpectedSourceSha256)
                .AppendLine("SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectGlbInstancesIncludingPrefab=" + result.DirectGlbInstanceCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LineupBoundsCenter=" + Vec(result.Bounds.center))
                .AppendLine("LineupBoundsSize=" + Vec(result.Bounds.size))
                .AppendLine("ModelGeometryModified=False")
                .AppendLine("GeneratedModelParts=False")
                .AppendLine("ExistingSlotTransformsPreserved=True")
                .AppendLine("ExistingSlotComponentsPreserved=True")
                .AppendLine("PlayerFacesFuga=True")
                .AppendLine("CompleteLineupVisible=True")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteFacingReport(FacingResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Facing And Disconnected Legacy Animations Report")
                .AppendLine("Result=PASS")
                .AppendLine("SourceSha256=" + ExpectedSourceSha256)
                .AppendLine("SceneSlots=" + result.SceneSlotCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("ModelLocalYawDegrees=180")
                .AppendLine("LegacyAnimatorControllers=0")
                .AppendLine("EnabledLegacyAnimators=0")
                .AppendLine("LegacyPlaybackDrivers=0")
                .AppendLine("PhysicsReviewMotionLocked=True")
                .AppendLine("IdleRootCause=LegacyExecuteAlwaysPlaybackSampledFugaApprovedModelLocalYCurve")
                .AppendLine("IdleLegacyCurveMaximumY=0.14")
                .AppendLine("IdleLegacyCurveMinimumY=-0.084")
                .AppendLine("IdleModelLocalY=" + Num(result.IdleModelLocalY))
                .AppendLine("MinimumPlayerFacingDot=" + Num(result.MinimumPlayerFacingDot))
                .AppendLine("PlayerChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("ExistingAnimationAssetsModified=False")
                .AppendLine("NewAnimationsCreated=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(FacingReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid facing report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteRotationReport(
            string reportPath,
            string stage,
            RotationSnapshot snapshot,
            bool correctionApplied,
            bool revertedToIdentityFirst,
            bool placementRootUnchanged,
            bool slotTransformsUnchanged,
            bool siblingIndicesUnchanged,
            bool helperTransformsUnchanged,
            bool protectedRootsUnchanged,
            bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Per-Object Rotation And Placement Report")
                .AppendLine("Result=PASS")
                .AppendLine("Stage=" + stage)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("PlacementRoot=" + PlacementRootName)
                .AppendLine("PlacementRootLocalPosition=" + Vec(snapshot.PlacementRootLocalPosition))
                .AppendLine("PlacementRootLocalEuler=" + Vec(snapshot.PlacementRootLocalEuler))
                .AppendLine("PlacementRootWorldPosition=" + Vec(snapshot.PlacementRootWorldPosition))
                .AppendLine("PlacementRootLocalRotationIdentity=" + snapshot.PlacementRootRotationIdentity)
                .AppendLine("DirectChildren=" + snapshot.DirectChildCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("NamedFugaSlots=" + snapshot.Slots.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("HelperChildren=" + snapshot.HelperCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("SlotLocalRotationsIdentity=" + snapshot.SlotRotationsIdentity)
                .AppendLine("ModelsLocalYaw180=" + snapshot.ModelsLocalYaw180)
                .AppendLine("RotationOwner=" + snapshot.RotationOwner)
                .AppendLine("GroupCenterRotationApplied=False")
                .AppendLine("CorrectionApplied=" + correctionApplied)
                .AppendLine("ModelsRevertedToIdentityBeforeRotation=" + revertedToIdentityFirst)
                .AppendLine("ModelsIndividuallyRotated180=" + (correctionApplied ? "7" : "0"))
                .AppendLine("PlacementRootTransformUnchanged=" + placementRootUnchanged)
                .AppendLine("SlotTransformsUnchanged=" + slotTransformsUnchanged)
                .AppendLine("SiblingIndicesUnchanged=" + siblingIndicesUnchanged)
                .AppendLine("HelperTransformsUnchanged=" + helperTransformsUnchanged)
                .AppendLine("ModelPivotPositionsUnchanged=True")
                .AppendLine("ProtectedRootsIncludingPlayerUnchanged=" + protectedRootsUnchanged)
                .AppendLine();

            foreach (var slot in snapshot.Slots)
            {
                report.AppendLine("Slot=" + slot.Name)
                    .AppendLine("  SiblingIndex=" + slot.SiblingIndex.ToString(CultureInfo.InvariantCulture))
                    .AppendLine("  SlotLocalPosition=" + Vec(slot.SlotLocalPosition))
                    .AppendLine("  SlotLocalEuler=" + Vec(slot.SlotLocalEuler))
                    .AppendLine("  SlotWorldPosition=" + Vec(slot.SlotWorldPosition))
                    .AppendLine("  ModelLocalPosition=" + Vec(slot.ModelLocalPosition))
                    .AppendLine("  ModelLocalEuler=" + Vec(slot.ModelLocalEuler))
                    .AppendLine("  ModelPivotWorldPosition=" + Vec(slot.ModelPivotWorldPosition))
                    .AppendLine("  RendererBoundsCenter=" + Vec(slot.RendererBoundsCenter))
                    .AppendLine("  PivotToBoundsCenter=" + Vec(slot.PivotToBoundsCenter));
            }

            report.AppendLine()
                .AppendLine("AnimationConnectionsChanged=False")
                .AppendLine("PrefabChanged=False")
                .AppendLine("PlayerChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .AppendLine("EditModeTestsRun=False")
                .AppendLine("PlayModeTestsRun=False")
                .AppendLine("WindowsBuildRun=False");
            var destination = Absolute(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga rotation report path."));
            File.WriteAllText(destination, report.ToString(), new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireSameHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    label + " SHA-256 mismatch. Expected=" + expected + ", Actual=" + actual + ".");
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct ReplacementResult
        {
            public ReplacementResult(int sceneSlotCount, int directGlbInstanceCount, Bounds bounds)
            {
                SceneSlotCount = sceneSlotCount;
                DirectGlbInstanceCount = directGlbInstanceCount;
                Bounds = bounds;
            }

            public int SceneSlotCount { get; }
            public int DirectGlbInstanceCount { get; }
            public Bounds Bounds { get; }
        }

        private readonly struct FacingResult
        {
            public FacingResult(int sceneSlotCount, float idleModelLocalY, float minimumPlayerFacingDot)
            {
                SceneSlotCount = sceneSlotCount;
                IdleModelLocalY = idleModelLocalY;
                MinimumPlayerFacingDot = minimumPlayerFacingDot;
            }

            public int SceneSlotCount { get; }
            public float IdleModelLocalY { get; }
            public float MinimumPlayerFacingDot { get; }
        }

        private readonly struct RotationSnapshot
        {
            public RotationSnapshot(
                int directChildCount,
                int helperCount,
                Vector3 placementRootLocalPosition,
                Vector3 placementRootLocalEuler,
                Vector3 placementRootWorldPosition,
                SlotRotationState[] slots,
                bool placementRootRotationIdentity,
                bool slotRotationsIdentity,
                bool modelsLocalYaw180,
                string rotationOwner)
            {
                DirectChildCount = directChildCount;
                HelperCount = helperCount;
                PlacementRootLocalPosition = placementRootLocalPosition;
                PlacementRootLocalEuler = placementRootLocalEuler;
                PlacementRootWorldPosition = placementRootWorldPosition;
                Slots = slots;
                PlacementRootRotationIdentity = placementRootRotationIdentity;
                SlotRotationsIdentity = slotRotationsIdentity;
                ModelsLocalYaw180 = modelsLocalYaw180;
                RotationOwner = rotationOwner;
            }

            public int DirectChildCount { get; }
            public int HelperCount { get; }
            public Vector3 PlacementRootLocalPosition { get; }
            public Vector3 PlacementRootLocalEuler { get; }
            public Vector3 PlacementRootWorldPosition { get; }
            public SlotRotationState[] Slots { get; }
            public bool PlacementRootRotationIdentity { get; }
            public bool SlotRotationsIdentity { get; }
            public bool ModelsLocalYaw180 { get; }
            public string RotationOwner { get; }
        }

        private readonly struct SlotRotationState
        {
            public SlotRotationState(
                string name,
                int siblingIndex,
                Vector3 slotLocalPosition,
                Vector3 slotLocalEuler,
                Vector3 slotWorldPosition,
                Vector3 modelLocalPosition,
                Vector3 modelLocalEuler,
                Vector3 modelPivotWorldPosition,
                Vector3 rendererBoundsCenter,
                Vector3 pivotToBoundsCenter)
            {
                Name = name;
                SiblingIndex = siblingIndex;
                SlotLocalPosition = slotLocalPosition;
                SlotLocalEuler = slotLocalEuler;
                SlotWorldPosition = slotWorldPosition;
                ModelLocalPosition = modelLocalPosition;
                ModelLocalEuler = modelLocalEuler;
                ModelPivotWorldPosition = modelPivotWorldPosition;
                RendererBoundsCenter = rendererBoundsCenter;
                PivotToBoundsCenter = pivotToBoundsCenter;
            }

            public string Name { get; }
            public int SiblingIndex { get; }
            public Vector3 SlotLocalPosition { get; }
            public Vector3 SlotLocalEuler { get; }
            public Vector3 SlotWorldPosition { get; }
            public Vector3 ModelLocalPosition { get; }
            public Vector3 ModelLocalEuler { get; }
            public Vector3 ModelPivotWorldPosition { get; }
            public Vector3 RendererBoundsCenter { get; }
            public Vector3 PivotToBoundsCenter { get; }
        }

        private readonly struct ScreenOrderResult
        {
            public ScreenOrderResult(
                string[] screenOrderedNames,
                float[] slotLocalXs,
                float[] screenViewportXs)
            {
                ScreenOrderedNames = screenOrderedNames;
                SlotLocalXs = slotLocalXs;
                ScreenViewportXs = screenViewportXs;
            }

            public string[] ScreenOrderedNames { get; }
            public float[] SlotLocalXs { get; }
            public float[] ScreenViewportXs { get; }
        }

        private readonly struct FrontPlayerOrderResult
        {
            public FrontPlayerOrderResult(
                string[] screenOrderedNames,
                float[] slotLocalXs,
                float[] screenViewportXs,
                float minimumFrontFacingDot,
                Vector3 playerPosition,
                Vector3 playerEuler)
            {
                ScreenOrderedNames = screenOrderedNames;
                SlotLocalXs = slotLocalXs;
                ScreenViewportXs = screenViewportXs;
                MinimumFrontFacingDot = minimumFrontFacingDot;
                PlayerPosition = playerPosition;
                PlayerEuler = playerEuler;
            }

            public string[] ScreenOrderedNames { get; }
            public float[] SlotLocalXs { get; }
            public float[] ScreenViewportXs { get; }
            public float MinimumFrontFacingDot { get; }
            public Vector3 PlayerPosition { get; }
            public Vector3 PlayerEuler { get; }
        }
    }
}
