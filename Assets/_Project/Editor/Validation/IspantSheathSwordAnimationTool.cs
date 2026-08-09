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
    internal static class IspantSheathSwordAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string DrawSwordSlotName = "Ispant_04_DrawSword";
        private const string TargetSlotName = "Ispant_06_SheathSwordDrawMusket";
        private const string StaticModelName = "Ispant_Model";
        private const string DrawSwordModelName = "Ispant_DrawSword_Model";
        private const string TargetModelName = "Ispant_SheathSword_Model";
        private const string SwordRootName = "Ispant_ApprovedLongSword";
        private const string SwordRendererName = "Ispant_ApprovedLongSword_Renderer";
        private const string WaistSwordRootName = "Ispant_ApprovedLongSword_LeftWaist";
        private const string WaistSwordRendererName = "Ispant_ApprovedLongSword_LeftWaist_Renderer";
        private const string HandMusketRootName = "Ispant_ChangeToRifle_HandMusket";
        private const string HandMusketRendererName = "Ispant_ChangeToRifle_HandMusket_Renderer";
        private const string MusketName = "Ispant_Sheath_RigidMusket";
        private const string SourceFbxPath = "enemies model/išpant sheating.fbx";
        private const string StaticFbxPath = "enemies model/Ispant_Static.fbx";
        private const string ProjectSourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_SheathSword_Source.fbx";
        private const string DerivedFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_SheathSword.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_SheathSword.controller";
        private const string StaticHoldClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_SheathSword_StaticHold.anim";
        private const string ChangeToRifleSourceFbxPath =
            "enemies model/išpant changing to rifle.fbx";
        private const string ProjectChangeToRifleSourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_ChangeToRifle_Source.fbx";
        private const string ChangeToRifleFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_ChangeToRifle.fbx";
        private const string ChangeToRifleClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_ChangeToRifle.anim";
        private const string SheathToRifleBridgeFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_SheathToRifleBridge.fbx";
        private const string SheathToRifleBridgeClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_SheathToRifleBridge.anim";
        private const string InspectionPath =
            "docs/validation/ispant_sheath_sword_2026-08-09/Ispant_06_SheathSword_Inspection.txt";
        private const string CapturePath =
            "docs/validation/ispant_sheath_sword_2026-08-09/Ispant_06_SheathSword_FinalReview.png";
        private const string StaticHoldInspectionPath =
            "docs/validation/ispant_sheath_sword_waist_hold_revision_2026-08-09/Ispant_06_SheathSword_WaistHold_Inspection.txt";
        private const string StaticHoldCapturePath =
            "docs/validation/ispant_sheath_sword_waist_hold_revision_2026-08-09/Ispant_06_SheathSword_WaistHold_FinalReview.png";
        private const string RifleSequenceInspectionPath =
            "docs/validation/ispant_sheath_to_rifle_final_aim_arm_lift_revision_2026-08-09/Ispant_06_SheathToRifle_FinalAimArmLift_Inspection.txt";
        private const string RifleSequenceCapturePath =
            "docs/validation/ispant_sheath_to_rifle_final_aim_arm_lift_revision_2026-08-09/Ispant_06_SheathToRifle_FinalAimArmLift_FinalReview.png";
        private const string SourceSha256 =
            "D5570DCD8A496D33CC54E2E96D6D1CFF238A6F1CAAAAE66B070473626E84728E";
        private const string StaticSha256 =
            "28EBF3FC2EE9441478389477FE56547DF11C74CEBD152553F5F7B5FCD235A8BE";
        private const string DerivedSha256 =
            "B5976948B8FC95E837BFFB909CA2D99E441B0583E678FC114F866E0BFD0B3551";
        private const string ChangeToRifleSourceSha256 =
            "CAD8A855CF9FB6B8C13393EF5E0F9ABBFDCA74E81C1F0592E6CFCB3E733C64C4";
        private const string ChangeToRifleDerivedSha256 =
            "2F29D119DA2BACE3B39C9B0B059D171A1EA0A1935B4FADC6574596A3B5D49AE7";
        private const string SheathToRifleBridgeSha256 =
            "0368F53D130EF863BCD2EDF06033B05928DE3B2FAB95301EAEF9D8EB6BD279C3";
        private const string ImportedClipName = "Ispant_SheathSword_Mixamo";
        private const string StateName = "Ispant_SheathSword_Mixamo";
        private const string StaticHoldClipName = "Ispant_06_SheathSword_StaticHold";
        private const string StaticHoldStateName = "Ispant_SheathSword_StaticHold";
        private const string ImportedChangeToRifleClipName = "Ispant_ChangeToRifle_Mixamo";
        private const string RuntimeChangeToRifleClipName = "Ispant_06_ChangeToRifle";
        private const string ChangeToRifleStateName = "Ispant_ChangeToRifle_Mixamo";
        private const string ImportedSheathToRifleBridgeClipName = "Ispant_SheathToRifle_Bridge";
        private const string RuntimeSheathToRifleBridgeClipName = "Ispant_06_SheathToRifleBridge";
        private const string SheathToRifleBridgeStateName = "Ispant_SheathToRifle_Bridge";
        private const int ExpectedSlots = 12;
        private const int ExpectedBones = 33;
        private const int ExpectedBodyTriangles = 3364;
        private const int ExpectedMusketTriangles = 154;
        private const int ExpectedCrescentTriangles = 1253;
        private const int ExpectedEyeTriangles = 312;
        private const int ExpectedSwordTriangles = 4092;
        private const int FirstFrame = 1;
        private const int LastFrame = 100;
        private const int ChangeToRifleFirstFrame = 1;
        private const int ChangeToRifleLastFrame = 213;
        private const float ChangeToRifleFrameRate = 60f;
        private const int SheathToRifleBridgeFirstFrame = 1;
        private const int SheathToRifleBridgeLastFrame = 50;
        private const float TransformTolerance = 0.0001f;
        private const float AttachmentTolerance = 0.0001f;
        private const float SizeRatioTolerance = 0.01f;
        private const float MinimumRightArmMotion = 10f;
        private const float MinimumRightHandMotion = 0.05f;
        private const float MinimumSwordAngularMotion = 10f;
        private const float MaximumSwordVertexToHandDistance = 0.04f;
        private const float ExpectedSwordLength = 1.4374533f;
        private const float TargetWorldBladeLength = 0.6f;
        private const float SwordDimensionTolerance = 0.0001f;
        private const float StaticHoldDuration = 0.5f;
        private const float StaticHoldTolerance = 0.0001f;
        // Wrist bones sit inside each hand; this is the allowed excess over the measured
        // approved right-hand grab distance when checking the left support hand.
        private const float SupportHandSurfaceDistanceTolerance = 0.02f;
        // User-approved final aiming lift, achieved through arm-bone rotation rather
        // than a direct weapon or root-position animation curve.
        private const float FinalAimArmLift = 0.15f;
        private static readonly Vector3 ApprovedGripCenterLocal = new Vector3(0f, 0f, -0.103f);
        private static readonly float[] ReviewNormalizedTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Sheath Sword Animation")]
        public static void ApplyIspantSheathSwordAnimation()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DerivedFbxPath) ??
                throw new InvalidOperationException("The derived Ispant sheath-sword FBX is unavailable.");
            var clip = RequireImportedClip();
            var controller = CreateOrUpdateController(clip);

            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var targetSlot = RequireSlot(placement.transform, TargetSlotName, 5);
            if (targetSlot.childCount != 1)
                throw new InvalidOperationException(
                    "Ispant_06_SheathSwordDrawMusket must contain exactly one model before replacement.");

            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, targetSlot);
            var slotBefore = new TransformSnapshot(targetSlot);
            var previous = targetSlot.GetChild(0);
            var replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException("The Ispant sheath-sword FBX could not be instantiated.");
            replacement.name = TargetModelName;
            replacement.transform.SetParent(targetSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previous.localPosition, previous.localRotation);
            replacement.transform.localScale = Vector3.one;

            try
            {
                ApplyStaticAppearance(staticModel, replacement.transform);
                FitToStaticReference(replacement.transform, staticModel);
                CloneApprovedSword(staticModel, drawModel, replacement.transform);
                var animator = ConfigureAnimator(replacement.transform, controller);
                var metrics = InspectModel(
                    replacement.transform, staticModel, drawModel, animator, clip, controller);
                WriteInspection(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (targetSlot.childCount != 1 || targetSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("The slot-6 replacement did not leave exactly one model.");
            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The slot-6 anchor transform changed during replacement.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, targetSlot),
                "An Ispant slot outside slot 6 changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(targetSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot-6 replacement.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = targetSlot.gameObject;
            Debug.Log(
                "IspantSheathSwordAnimationApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + TargetSlotName +
                ", SourceMixamoFrames=1-100, Loop=True, RootMotion=False" +
                ", StaticAppearanceDirectReference=True" +
                ", SwordParent=mixamorig:RightHand, SwordRigid=True" +
                ", MusketParent=mixamorig:Spine2, MusketRigid=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Sheath Sword Animation")]
        public static void InspectIspantSheathSwordAnimation()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 5), TargetModelName);
            var clip = RequireImportedClip();
            var metrics = InspectModel(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(),
                clip, RequireController());
            WriteInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-6 inspection changed the scene dirty state.");
            Debug.Log(
                "IspantSheathSwordAnimationInspected Result=PASS" +
                ", RightHandMotion=" + Num(metrics.MaximumRightHandMotion) +
                ", RightArmAngularMotion=" + Num(metrics.MaximumRightArmAngularMotion) +
                ", SwordAngularMotion=" + Num(metrics.MaximumSwordAngularMotion) +
                ", SwordAttachmentError=" + Num(metrics.MaximumSwordAttachmentError) +
                ", MusketAttachmentError=" + Num(metrics.MaximumMusketAttachmentError) +
                ", SwordWorldBladeLength=" + Num(metrics.SwordBladeLength) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Sheath Sword Animation Review")]
        public static void CaptureIspantSheathSwordAnimationReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 5), TargetModelName);
            var clip = RequireImportedClip();
            var metrics = InspectModel(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(),
                clip, RequireController());
            WriteInspection(metrics);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-6 final review already exists.");
            CaptureReview(staticModel, model, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-6 review capture changed the scene dirty state.");
            Debug.Log(
                "IspantSheathSwordAnimationReviewCaptured Result=PASS" +
                ", Panels=Static,0,0.25,0.5,0.75,1, Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Sheath Sword Static Hold")]
        public static void ApplyIspantSheathSwordStaticHold()
        {
            RequireHashes();
            ConfigureImporter();
            RequireHashes();
            var sourceClip = RequireImportedClip();
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var targetSlot = RequireSlot(placement.transform, TargetSlotName, 5);
            var model = RequireDirectChild(targetSlot, TargetModelName);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, targetSlot);
            var slotBefore = new TransformSnapshot(targetSlot);

            CreateOrUpdateWaistSword(staticModel, model, sourceClip);
            var holdClip = CreateOrUpdateStaticHoldClip(sourceClip, staticModel, model);
            var controller = CreateOrUpdateStaticHoldController(sourceClip, holdClip);
            var animator = ConfigureAnimator(model, controller);
            var metrics = InspectStaticHold(
                model, staticModel, drawModel, animator, sourceClip, holdClip, controller);
            WriteStaticHoldInspection(metrics);

            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException("The slot-6 anchor changed during static-hold application.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, targetSlot),
                "An Ispant slot outside slot 6 changed.");
            EditorUtility.SetDirty(model.gameObject);
            EditorUtility.SetDirty(targetSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved after slot-6 static hold.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = targetSlot.gameObject;
            Debug.Log(
                "IspantSheathSwordStaticHoldApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + TargetSlotName +
                ", SourceDuration=" + Num(sourceClip.length) +
                ", StaticHoldDuration=" + Num(holdClip.length) +
                ", SequenceDuration=" + Num(sourceClip.length + holdClip.length) +
                ", SwordStaticReferenceError=" + Num(metrics.MaximumSwordStaticReferenceMatrixError) +
                ", HoldTransformDrift=" + Num(metrics.MaximumHoldTransformDrift) +
                ", LoopSequence=True, OtherSlotsChanged=False" +
                ", OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        public static void ApplyIspantSheathSwordWaistHoldRevision()
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == ScenePath && active.isDirty)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                ApplyIspantSheathSwordStaticHold();
            }
            catch
            {
                active = SceneManager.GetActiveScene();
                if (active.path == ScenePath && active.isDirty)
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Sheath Sword Static Hold")]
        public static void InspectIspantSheathSwordStaticHold()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 5), TargetModelName);
            var sourceClip = RequireImportedClip();
            var holdClip = RequireStaticHoldClip();
            var controller = RequireController();
            var metrics = InspectStaticHold(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(),
                sourceClip, holdClip, controller);
            WriteStaticHoldInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-6 static-hold inspection changed the scene.");
            Debug.Log(
                "IspantSheathSwordStaticHoldInspected Result=PASS" +
                ", SourceDuration=" + Num(metrics.SourceDuration) +
                ", HoldDuration=" + Num(metrics.HoldDuration) +
                ", SwordStaticReferenceError=" +
                Num(metrics.MaximumSwordStaticReferenceMatrixError) +
                ", HoldTransformDrift=" + Num(metrics.MaximumHoldTransformDrift) +
                ", HoldSwordDrift=" + Num(metrics.MaximumHoldSwordMatrixDrift) +
                ", SceneChanged=False.");
        }

        public static void InspectIspantSheathSwordWaistHoldRevision()
        {
            InspectIspantSheathSwordStaticHold();
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Sheath Sword Static Hold Review")]
        public static void CaptureIspantSheathSwordStaticHoldReview()
        {
            RequireHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 5), TargetModelName);
            var sourceClip = RequireImportedClip();
            var holdClip = RequireStaticHoldClip();
            var controller = RequireController();
            var metrics = InspectStaticHold(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(),
                sourceClip, holdClip, controller);
            WriteStaticHoldInspection(metrics);
            var destination = Absolute(StaticHoldCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-6 static-hold review already exists.");
            CaptureStaticHoldReview(staticModel, model, sourceClip, holdClip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("The slot-6 static-hold capture changed the scene.");
            Debug.Log(
                "IspantSheathSwordStaticHoldReviewCaptured Result=PASS" +
                ", Panels=Static,MixamoEnd,HoldStart,HoldMiddle,HoldEnd,RepeatStart" +
                ", Image=" + StaticHoldCapturePath + ", SceneChanged=False.");
        }

        public static void CaptureIspantSheathSwordWaistHoldRevisionReview()
        {
            CaptureIspantSheathSwordStaticHoldReview();
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Sheath To Rifle Sequence")]
        public static void ApplyIspantSheathToRifleSequence()
        {
            ApplyIspantSheathToRifleSequence(requireClean: true);
        }

        private static void ApplyIspantSheathToRifleSequence(bool requireClean)
        {
            RequireHashes();
            RequireChangeToRifleHashes();
            ConfigureChangeToRifleImporter();
            ConfigureSheathToRifleBridgeImporter();
            RequireChangeToRifleHashes();
            var sheathClip = RequireImportedClip();
            var bridgeSourceClip = RequireImportedSheathToRifleBridgeClip();
            var rifleSourceClip = RequireImportedChangeToRifleClip();
            var scene = RequireScene(requireClean);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var targetSlot = RequireSlot(placement.transform, TargetSlotName, 5);
            var model = RequireDirectChild(targetSlot, TargetModelName);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, targetSlot);
            var slotBefore = new TransformSnapshot(targetSlot);

            CreateOrUpdateWaistSword(staticModel, model, sheathClip);
            var holdClip = CreateOrUpdateStaticHoldClip(
                sheathClip, staticModel, model);
            var grab = FindRifleGrab(model, rifleSourceClip);
            CreateOrUpdateHandMusket(model, rifleSourceClip, grab.Time);
            SetSequenceDefaultVisibility(model);
            var bridgeClip = CreateOrUpdateRuntimeBridgeClip(
                model, holdClip, rifleSourceClip, bridgeSourceClip.length);
            var rifleClip = CreateOrUpdateRuntimeChangeToRifleClip(
                model, rifleSourceClip, grab.Time);
            var controller = CreateOrUpdateSheathToRifleController(
                sheathClip, holdClip, bridgeClip, rifleClip);
            var animator = ConfigureAnimator(model, controller);
            var metrics = InspectSheathToRifleSequence(
                model, staticModel, drawModel, animator, sheathClip, holdClip,
                bridgeSourceClip, bridgeClip,
                rifleSourceClip, rifleClip, controller);
            WriteRifleSequenceInspection(metrics);

            if (!slotBefore.Matches(TransformTolerance))
                throw new InvalidOperationException(
                    "The slot-6 anchor changed during sheath-to-rifle application.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform, targetSlot),
                "An Ispant slot outside slot 6 changed.");
            EditorUtility.SetDirty(model.gameObject);
            EditorUtility.SetDirty(targetSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-6 sheath-to-rifle sequence.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = targetSlot.gameObject;
            Debug.Log(
                "IspantSheathToRifleSequenceApplied Result=PASS" +
                ", Target=" + PlacementRootName + "/" + TargetSlotName +
                ", RifleFrames=" + ChangeToRifleFirstFrame + "-" + ChangeToRifleLastFrame +
                ", GrabFrame=" + metrics.GrabFrame +
                ", GrabTime=" + Num(metrics.GrabTime) +
                ", HoldDuration=" + Num(holdClip.length) +
                ", SequenceDuration=" + Num(metrics.SequenceDuration) +
                ", LoopSequence=True, OtherSlotsChanged=False" +
                ", OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Sheath To Rifle Sequence")]
        public static void InspectIspantSheathToRifleSequence()
        {
            RequireHashes();
            RequireChangeToRifleHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 5), TargetModelName);
            var metrics = InspectSheathToRifleSequence(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(),
                RequireImportedClip(), RequireStaticHoldClip(),
                RequireImportedSheathToRifleBridgeClip(), RequireRuntimeBridgeClip(),
                RequireImportedChangeToRifleClip(), RequireRuntimeChangeToRifleClip(),
                RequireController());
            WriteRifleSequenceInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The sheath-to-rifle inspection changed the slot-6 scene state.");
            Debug.Log(
                "IspantSheathToRifleSequenceInspected Result=PASS" +
                ", GrabFrame=" + metrics.GrabFrame +
                ", GrabDistance=" + Num(metrics.GrabDistance) +
                ", GrabContinuityError=" + Num(metrics.GrabContinuityError) +
                ", HandMusketFollowError=" + Num(metrics.HandMusketFollowError) +
                ", ForwardMotion=" + Num(metrics.MaximumPostGrabMusketMotion) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Sheath To Rifle Sequence Review")]
        public static void CaptureIspantSheathToRifleSequenceReview()
        {
            RequireHashes();
            RequireChangeToRifleHashes();
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var drawModel = RequireDirectChild(
                RequireSlot(placement.transform, DrawSwordSlotName, 3), DrawSwordModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, TargetSlotName, 5), TargetModelName);
            var sheathClip = RequireImportedClip();
            var holdClip = RequireStaticHoldClip();
            var bridgeSourceClip = RequireImportedSheathToRifleBridgeClip();
            var bridgeClip = RequireRuntimeBridgeClip();
            var rifleSourceClip = RequireImportedChangeToRifleClip();
            var rifleClip = RequireRuntimeChangeToRifleClip();
            var metrics = InspectSheathToRifleSequence(
                model, staticModel, drawModel,
                model.GetComponentsInChildren<Animator>(true).Single(),
                sheathClip, holdClip, bridgeSourceClip, bridgeClip,
                rifleSourceClip, rifleClip, RequireController());
            WriteRifleSequenceInspection(metrics);
            var destination = Absolute(RifleSequenceCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time slot-6 sheath-to-rifle review already exists.");
            CaptureSheathToRifleReview(
                staticModel, model, sheathClip, holdClip, bridgeClip,
                rifleClip, metrics, destination);
            WriteRifleSequenceInspection(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The sheath-to-rifle capture changed the slot-6 scene state.");
            Debug.Log(
                "IspantSheathToRifleSequenceReviewCaptured Result=PASS" +
                ", Panels=Static,SheathEnd,Hold,Bridge25,Bridge50,Bridge75,BridgeEnd,PreGrab,Grab,Forward,End" +
                ", Image=" + RifleSequenceCapturePath + ", SceneChanged=False.");
        }

        public static void ApplyIspantSheathToRifleMotionRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleMotionRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleMotionRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleTwoHandGripRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleTwoHandGripRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleTwoHandGripRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleArmDrivenAimRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleArmDrivenAimRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleArmDrivenAimRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleForwardMuzzleRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleForwardMuzzleRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleForwardMuzzleRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleUprightTriggerGripRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleUprightTriggerGripRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleUprightTriggerGripRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleStockAndTriggerDownRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleStockAndTriggerDownRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleStockAndTriggerDownRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleWaistSwordBodyFollowRevision()
        {
            ApplyIspantSheathToRifleSequence();
        }

        public static void InspectIspantSheathToRifleWaistSwordBodyFollowRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleWaistSwordBodyFollowRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        public static void ApplyIspantSheathToRifleFinalAimArmLiftRevision()
        {
            // A failed IK attempt can leave only the in-scope slot dirty without saving it.
            // This deterministic apply regenerates that slot and saves only after inspection.
            ApplyIspantSheathToRifleSequence(requireClean: false);
        }

        public static void InspectIspantSheathToRifleFinalAimArmLiftRevision()
        {
            InspectIspantSheathToRifleSequence();
        }

        public static void CaptureIspantSheathToRifleFinalAimArmLiftRevisionReview()
        {
            CaptureIspantSheathToRifleSequenceReview();
        }

        private static void ConfigureChangeToRifleImporter()
        {
            AssetDatabase.ImportAsset(
                ChangeToRifleFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(ChangeToRifleFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The change-to-rifle ModelImporter is missing.");
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
                throw new InvalidOperationException(
                    "The change-to-rifle FBX must expose exactly one Mixamo take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The sole change-to-rifle take is not Mixamo: " + clips[0].takeName + ".");
            clips[0].name = ImportedChangeToRifleClipName;
            clips[0].firstFrame = ChangeToRifleFirstFrame;
            clips[0].lastFrame = ChangeToRifleLastFrame;
            clips[0].loopTime = false;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = false;
            clips[0].lockRootPositionXZ = false;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireImportedChangeToRifleClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ChangeToRifleFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedChangeToRifleClipName)
                throw new InvalidOperationException("The imported change-to-rifle Mixamo clip differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            var expectedLength =
                (ChangeToRifleLastFrame - ChangeToRifleFirstFrame - 1f) /
                ChangeToRifleFrameRate;
            if (settings.loopTime || Mathf.Abs(clips[0].length - expectedLength) > 0.0001f)
                throw new InvalidOperationException(
                    "The imported change-to-rifle clip duration or loop setting differs: " +
                    "Actual=" + Num(clips[0].length) +
                    ", Expected=" + Num(expectedLength) +
                    ", Loop=" + settings.loopTime + ".");
            return clips[0];
        }

        private static void ConfigureSheathToRifleBridgeImporter()
        {
            AssetDatabase.ImportAsset(
                SheathToRifleBridgeFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(SheathToRifleBridgeFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The sheath-to-rifle bridge ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException("The bridge FBX must expose exactly one take.");
            clips[0].name = ImportedSheathToRifleBridgeClipName;
            clips[0].firstFrame = SheathToRifleBridgeFirstFrame;
            clips[0].lastFrame = SheathToRifleBridgeLastFrame;
            clips[0].loopTime = false;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = false;
            clips[0].lockRootPositionXZ = false;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireImportedSheathToRifleBridgeClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(SheathToRifleBridgeFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var expectedLength =
                (SheathToRifleBridgeLastFrame - SheathToRifleBridgeFirstFrame) /
                ChangeToRifleFrameRate;
            if (clips.Length != 1 || clips[0].name != ImportedSheathToRifleBridgeClipName ||
                AnimationUtility.GetAnimationClipSettings(clips[0]).loopTime ||
                Mathf.Abs(clips[0].length - expectedLength) > 0.0001f)
                throw new InvalidOperationException("The imported sheath-to-rifle bridge differs.");
            return clips[0];
        }

        private static AnimationClip RequireRuntimeBridgeClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SheathToRifleBridgeClipPath) ??
                throw new InvalidOperationException("The runtime sheath-to-rifle bridge clip is missing.");
            if (clip.name != RuntimeSheathToRifleBridgeClipName ||
                AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The runtime bridge clip configuration differs.");
            return clip;
        }

        private static AnimationClip CreateOrUpdateRuntimeBridgeClip(
            Transform model,
            AnimationClip hold,
            AnimationClip rifleSource,
            float duration)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SheathToRifleBridgeClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = RuntimeSheathToRifleBridgeClipName };
                AssetDatabase.CreateAsset(clip, SheathToRifleBridgeClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            var sourceBindings = AnimationUtility.GetCurveBindings(rifleSource);
            var holdBindings = AnimationUtility.GetCurveBindings(hold)
                .ToDictionary(item => item.path + "\n" + item.type.FullName + "\n" +
                    item.propertyName, item => item);
            var rotationEndSigns = new Dictionary<string, float>();
            foreach (var path in sourceBindings
                         .Where(item => item.propertyName.StartsWith(
                             "m_LocalRotation.", StringComparison.Ordinal))
                         .Select(item => item.path).Distinct())
            {
                var start = new Quaternion(
                    BridgeEndpoint(hold, holdBindings, path, "m_LocalRotation.x", hold.length),
                    BridgeEndpoint(hold, holdBindings, path, "m_LocalRotation.y", hold.length),
                    BridgeEndpoint(hold, holdBindings, path, "m_LocalRotation.z", hold.length),
                    BridgeEndpoint(hold, holdBindings, path, "m_LocalRotation.w", hold.length));
                var end = new Quaternion(
                    BridgeEndpoint(rifleSource, null, path, "m_LocalRotation.x", 0f),
                    BridgeEndpoint(rifleSource, null, path, "m_LocalRotation.y", 0f),
                    BridgeEndpoint(rifleSource, null, path, "m_LocalRotation.z", 0f),
                    BridgeEndpoint(rifleSource, null, path, "m_LocalRotation.w", 0f));
                rotationEndSigns[path] = Quaternion.Dot(start, end) < 0f ? -1f : 1f;
            }
            foreach (var binding in sourceBindings)
            {
                var key = binding.path + "\n" + binding.type.FullName + "\n" +
                    binding.propertyName;
                if (!holdBindings.TryGetValue(key, out var holdBinding))
                    throw new InvalidOperationException(
                        "The hold clip is missing a rifle-start bridge binding: " +
                        binding.path + "/" + binding.propertyName + ".");
                var holdCurve = AnimationUtility.GetEditorCurve(hold, holdBinding) ??
                    throw new InvalidOperationException("A hold bridge curve is missing.");
                var rifleCurve = AnimationUtility.GetEditorCurve(rifleSource, binding) ??
                    throw new InvalidOperationException("A rifle bridge curve is missing.");
                var startValue = holdCurve.Evaluate(hold.length);
                var endValue = rifleCurve.Evaluate(0f);
                if (binding.propertyName.StartsWith(
                        "m_LocalRotation.", StringComparison.Ordinal))
                    endValue *= rotationEndSigns[binding.path];
                var keys = new Keyframe[SheathToRifleBridgeLastFrame];
                for (var index = 0; index < keys.Length; index++)
                {
                    var raw = index / (float)(keys.Length - 1);
                    var smooth = raw * raw * (3f - 2f * raw);
                    keys[index] = new Keyframe(
                        duration * raw, Mathf.Lerp(startValue, endValue, smooth));
                }
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(
                        curve, index, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            var handSword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(model, MusketName);
            var handMusket = RequireRenderer<MeshRenderer>(model, HandMusketRendererName);
            SetConstantRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(handSword.transform, model),
                false, duration);
            SetConstantRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(waistSword.transform, model),
                true, duration);
            SetConstantRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(backMusket.transform, model),
                true, duration);
            SetConstantRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(handMusket.transform, model),
                false, duration);
            clip.name = RuntimeSheathToRifleBridgeClipName;
            clip.frameRate = ChangeToRifleFrameRate;
            clip.wrapMode = WrapMode.ClampForever;
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            if (Mathf.Abs(clip.length - duration) > 0.0001f)
                throw new InvalidOperationException("The runtime bridge duration differs.");
            return clip;
        }

        private static float BridgeEndpoint(
            AnimationClip clip,
            IReadOnlyDictionary<string, EditorCurveBinding> knownBindings,
            string path,
            string propertyName,
            float time)
        {
            var key = path + "\n" + typeof(Transform).FullName + "\n" + propertyName;
            EditorCurveBinding binding;
            if (knownBindings != null)
            {
                if (!knownBindings.TryGetValue(key, out binding))
                    throw new InvalidOperationException(
                        "A required bridge rotation binding is missing: " +
                        path + "/" + propertyName + ".");
            }
            else
            {
                binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            }
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                throw new InvalidOperationException(
                    "A required bridge endpoint curve is missing: " +
                    path + "/" + propertyName + ".");
            return curve.Evaluate(time);
        }

        private static AnimationClip RequireRuntimeChangeToRifleClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ChangeToRifleClipPath) ??
                throw new InvalidOperationException("The runtime change-to-rifle clip is missing.");
            if (clip.name != RuntimeChangeToRifleClipName ||
                AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The runtime change-to-rifle clip configuration differs.");
            return clip;
        }

        private static RifleGrabSample FindRifleGrab(Transform model, AnimationClip clip)
        {
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var backMusket = RequireRenderer<MeshRenderer>(model, MusketName);
            var vertices = SharedMesh(backMusket).vertices;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var minimumDistance = float.PositiveInfinity;
            var minimumFrame = -1;
            var startDistance = 0f;
            try
            {
                for (var frame = ChangeToRifleFirstFrame;
                     frame <= ChangeToRifleLastFrame;
                     frame++)
                {
                    var time = ChangeToRifleTimeForFrame(frame, clip);
                    SampleClip(model.gameObject, clip, time);
                    var distance = vertices.Min(vertex => Vector3.Distance(
                        rightHand.position, backMusket.transform.TransformPoint(vertex)));
                    if (frame == ChangeToRifleFirstFrame)
                        startDistance = distance;
                    if (distance < minimumDistance)
                    {
                        minimumDistance = distance;
                        minimumFrame = frame;
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }
            if (minimumFrame <= ChangeToRifleFirstFrame ||
                minimumFrame >= ChangeToRifleLastFrame ||
                minimumDistance >= startDistance)
                throw new InvalidOperationException(
                    "The supplied Mixamo motion has no valid right-hand approach to the back musket.");
            return new RifleGrabSample(
                minimumFrame,
                ChangeToRifleTimeForFrame(minimumFrame, clip),
                minimumDistance,
                startDistance);
        }

        private static float ChangeToRifleTimeForFrame(int frame, AnimationClip clip)
        {
            if (frame < ChangeToRifleFirstFrame || frame > ChangeToRifleLastFrame)
                throw new ArgumentOutOfRangeException(nameof(frame));
            var normalized = (frame - ChangeToRifleFirstFrame) /
                (float)(ChangeToRifleLastFrame - ChangeToRifleFirstFrame);
            return normalized * clip.length;
        }

        private static MeshRenderer CreateOrUpdateHandMusket(
            Transform model, AnimationClip rifleSourceClip, float grabTime)
        {
            var oldRoot = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == HandMusketRootName);
            if (oldRoot != null)
                UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            var backMusket = RequireRenderer<MeshRenderer>(model, MusketName);
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightShoulder = RequireDescendant(model, "mixamorig:RightShoulder");
            var rightArm = RequireDescendant(model, "mixamorig:RightArm");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            var leftHand = RequireDescendant(model, "mixamorig:LeftHand");
            var mesh = SharedMesh(backMusket);
            var vertices = mesh.vertices;
            var localMuzzleAxis = DetermineMusketLocalMuzzleAxis(mesh);
            var characterForward = DetermineCharacterForward(model);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            Vector3 pivotLocalPosition;
            Matrix4x4 rendererLocal;
            Matrix4x4 grabRightHandWorld;
            Matrix4x4 grabMusketWorld;
            Matrix4x4 finalRightHandWorld;
            Vector3 finalLeftHandPosition;
            float[] grabVertexDistances;
            try
            {
                SampleClip(model.gameObject, rifleSourceClip, grabTime);
                grabRightHandWorld = rightHand.localToWorldMatrix;
                grabMusketWorld = backMusket.transform.localToWorldMatrix;
                grabVertexDistances = vertices.Select(vertex => Vector3.Distance(
                    rightHand.position, grabMusketWorld.MultiplyPoint3x4(vertex))).ToArray();

                SampleClip(model.gameObject, rifleSourceClip, rifleSourceClip.length);
                finalRightHandWorld = rightHand.localToWorldMatrix;
                finalLeftHandPosition = leftHand.position;
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }
            var minimumGrabDistance = grabVertexDistances.Min();
            var candidateIndices = Enumerable.Range(0, vertices.Length)
                .Where(index => grabVertexDistances[index] <=
                    minimumGrabDistance + SupportHandSurfaceDistanceTolerance)
                .ToArray();
            var bestFinalSupportDistance = float.PositiveInfinity;
            pivotLocalPosition = Vector3.zero;
            rendererLocal = Matrix4x4.identity;
            foreach (var candidateIndex in candidateIndices)
            {
                var gripVertex = vertices[candidateIndex];
                var pivotWorldPosition = grabMusketWorld.MultiplyPoint3x4(gripVertex);
                var candidatePivotLocal = grabRightHandWorld.inverse
                    .MultiplyPoint3x4(pivotWorldPosition);
                var grabPivotWorld = grabRightHandWorld * Matrix4x4.TRS(
                    candidatePivotLocal, Quaternion.identity, Vector3.one);
                var candidateRendererLocal = grabPivotWorld.inverse * grabMusketWorld;
                var finalPivotWorld = finalRightHandWorld * Matrix4x4.TRS(
                    candidatePivotLocal, Quaternion.identity, Vector3.one);
                DecomposeMatrix(
                    finalPivotWorld, out var finalPivotPosition,
                    out var finalPivotRotation, out var finalPivotScale);
                var rootLocalMuzzleAxis = candidateRendererLocal
                    .MultiplyVector(localMuzzleAxis).normalized;
                var currentMuzzle = finalPivotRotation * rootLocalMuzzleAxis;
                var baseFinalRotation = Quaternion.FromToRotation(
                    currentMuzzle, characterForward) * finalPivotRotation;
                var supportVertexIndices = DetermineApprovedLeatherForegripVertexIndices(
                    backMusket, gripVertex, localMuzzleAxis);
                if (supportVertexIndices.Length == 0)
                    continue;
                var candidateDistance = float.PositiveInfinity;
                for (var roll = -180; roll < 180; roll++)
                {
                    var rotation = Quaternion.AngleAxis(roll, characterForward) *
                                   baseFinalRotation;
                    candidateDistance = Mathf.Min(
                        candidateDistance,
                        MinimumSupportSurfaceDistance(
                            finalLeftHandPosition,
                            finalPivotPosition,
                            rotation,
                            finalPivotScale,
                            candidateRendererLocal,
                            vertices,
                            supportVertexIndices));
                }
                if (candidateDistance >= bestFinalSupportDistance)
                    continue;
                bestFinalSupportDistance = candidateDistance;
                pivotLocalPosition = candidatePivotLocal;
                rendererLocal = candidateRendererLocal;
            }
            if (float.IsInfinity(bestFinalSupportDistance) ||
                float.IsNaN(bestFinalSupportDistance))
                throw new InvalidOperationException(
                    "No right-hand surface pivot can support the supplied final left-hand pose.");
            var root = new GameObject(HandMusketRootName);
            root.transform.SetParent(rightHand, false);
            root.transform.SetLocalPositionAndRotation(
                pivotLocalPosition, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            var rendererObject = new GameObject(HandMusketRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            DecomposeMatrix(
                rendererLocal, out var rendererPosition,
                out var rendererRotation, out var rendererScale);
            rendererObject.transform.SetLocalPositionAndRotation(
                rendererPosition, rendererRotation);
            rendererObject.transform.localScale = rendererScale;
            var filter = rendererObject.AddComponent<MeshFilter>();
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterials = backMusket.sharedMaterials;
            renderer.enabled = false;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
            return renderer;
        }

        private static AnimationClip CreateOrUpdateRuntimeChangeToRifleClip(
            Transform model, AnimationClip source, float grabTime)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ChangeToRifleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = RuntimeChangeToRifleClipName };
                AssetDatabase.CreateAsset(clip, ChangeToRifleClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                    throw new InvalidOperationException(
                        "A change-to-rifle source curve is missing during exact copy.");
                var curve = new AnimationCurve(sourceCurve.keys)
                {
                    preWrapMode = sourceCurve.preWrapMode,
                    postWrapMode = sourceCurve.postWrapMode
                };
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }

            var handSword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(model, MusketName);
            var handMusket = RequireRenderer<MeshRenderer>(model, HandMusketRendererName);
            SetConstantRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(handSword.transform, model),
                false, source.length);
            SetConstantRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(waistSword.transform, model),
                true, source.length);
            SetStepRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(backMusket.transform, model),
                grabTime, true, false, source.length);
            SetStepRendererEnabledCurve(
                clip, AnimationUtility.CalculateTransformPath(handMusket.transform, model),
                grabTime, false, true, source.length);
            SetHandMusketAimRotationCurves(model, clip, source, grabTime);
            clip.name = RuntimeChangeToRifleClipName;
            clip.frameRate = ChangeToRifleFrameRate;
            clip.wrapMode = WrapMode.ClampForever;
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            if (Mathf.Abs(clip.length - source.length) > 0.0001f)
                throw new InvalidOperationException(
                    "The exact runtime change-to-rifle clip duration differs from its source.");
            return clip;
        }

        private static void SetStepRendererEnabledCurve(
            AnimationClip clip,
            string path,
            float transitionTime,
            bool before,
            bool after,
            float duration)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, before ? 1f : 0f),
                new Keyframe(transitionTime, after ? 1f : 0f),
                new Keyframe(duration, after ? 1f : 0f));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
            }
            var binding = EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled");
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void SetHandMusketAimRotationCurves(
            Transform model,
            AnimationClip clip,
            AnimationClip source,
            float grabTime)
        {
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightShoulder = RequireDescendant(model, "mixamorig:RightShoulder");
            var rightArm = RequireDescendant(model, "mixamorig:RightArm");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            var leftHand = RequireDescendant(model, "mixamorig:LeftHand");
            var leftArm = RequireDescendant(model, "mixamorig:LeftArm");
            var leftForeArm = RequireDescendant(model, "mixamorig:LeftForeArm");
            var handMusket = RequireRenderer<MeshRenderer>(model, HandMusketRendererName);
            var handMusketRoot = handMusket.transform.parent ??
                throw new InvalidOperationException("The hand-musket root is missing.");
            var mesh = SharedMesh(handMusket);
            var vertices = mesh.vertices;
            var localMuzzleAxis = DetermineMusketLocalMuzzleAxis(mesh);
            var rendererLocal = LocalMatrix(handMusket.transform);
            var rootLocalMuzzleAxis = rendererLocal.MultiplyVector(localMuzzleAxis).normalized;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var times = new List<float>();
            var handPositions = new List<Vector3>();
            var leftHandPositions = new List<Vector3>();
            var rootPositions = new List<Vector3>();
            var rootScales = new List<Vector3>();
            var rightHandRotations = new List<Quaternion>();
            var rightHandScales = new List<Vector3>();
            var baselineRootRotations = new List<Quaternion>();
            var rightShoulderParentRotations = new List<Quaternion>();
            var rightShoulderWorldRotations = new List<Quaternion>();
            var rightShoulderPositions = new List<Vector3>();
            var rightArmParentRotations = new List<Quaternion>();
            var rightArmLocalRotations = new List<Quaternion>();
            var rightForeArmLocalRotations = new List<Quaternion>();
            var rightArmWorldRotations = new List<Quaternion>();
            var rightForeArmWorldRotations = new List<Quaternion>();
            var rightArmPositions = new List<Vector3>();
            var rightForeArmPositions = new List<Vector3>();
            var leftArmParentRotations = new List<Quaternion>();
            var leftArmLocalRotations = new List<Quaternion>();
            var leftForeArmLocalRotations = new List<Quaternion>();
            var leftArmWorldRotations = new List<Quaternion>();
            var leftForeArmWorldRotations = new List<Quaternion>();
            var leftArmPositions = new List<Vector3>();
            var leftForeArmPositions = new List<Vector3>();
            var characterForward = DetermineCharacterForward(model);
            Vector3 grabMuzzleDirection;
            int gripVertexIndex;
            try
            {
                SampleClip(model.gameObject, source, grabTime);
                grabMuzzleDirection = handMusket.transform.TransformDirection(localMuzzleAxis)
                    .normalized;
                gripVertexIndex = Enumerable.Range(0, vertices.Length).OrderBy(index =>
                    Vector3.Distance(
                        handMusketRoot.position,
                        handMusket.transform.TransformPoint(vertices[index]))).First();
                var pivotError = Vector3.Distance(
                    handMusketRoot.position,
                    handMusket.transform.TransformPoint(vertices[gripVertexIndex]));
                if (pivotError > TransformTolerance)
                    throw new InvalidOperationException(
                        "The generated right-hand pivot is not on the approved musket surface: " +
                        Num(pivotError) + ".");

                for (var frame = ChangeToRifleFirstFrame;
                     frame <= ChangeToRifleLastFrame;
                     frame++)
                {
                    var time = ChangeToRifleTimeForFrame(frame, source);
                    if (time + 0.000001f < grabTime)
                        continue;
                    SampleClip(model.gameObject, source, time);
                    times.Add(time);
                    handPositions.Add(rightHand.position);
                    leftHandPositions.Add(leftHand.position);
                    rootPositions.Add(handMusketRoot.position);
                    rootScales.Add(handMusketRoot.lossyScale);
                    rightHandRotations.Add(rightHand.rotation);
                    rightHandScales.Add(rightHand.lossyScale);
                    baselineRootRotations.Add(handMusketRoot.rotation);
                    rightShoulderParentRotations.Add(rightShoulder.parent.rotation);
                    rightShoulderWorldRotations.Add(rightShoulder.rotation);
                    rightShoulderPositions.Add(rightShoulder.position);
                    rightArmParentRotations.Add(rightArm.parent.rotation);
                    rightArmLocalRotations.Add(rightArm.localRotation);
                    rightForeArmLocalRotations.Add(rightForeArm.localRotation);
                    rightArmWorldRotations.Add(rightArm.rotation);
                    rightForeArmWorldRotations.Add(rightForeArm.rotation);
                    rightArmPositions.Add(rightArm.position);
                    rightForeArmPositions.Add(rightForeArm.position);
                    leftArmParentRotations.Add(leftArm.parent.rotation);
                    leftArmLocalRotations.Add(leftArm.localRotation);
                    leftForeArmLocalRotations.Add(leftForeArm.localRotation);
                    leftArmWorldRotations.Add(leftArm.rotation);
                    leftForeArmWorldRotations.Add(leftForeArm.rotation);
                    leftArmPositions.Add(leftArm.position);
                    leftForeArmPositions.Add(leftForeArm.position);
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }
            if (times.Count < 2)
                throw new InvalidOperationException(
                    "The change-to-rifle motion has too few post-grab samples.");

            var cumulative = new float[times.Count];
            for (var index = 1; index < times.Count; index++)
                cumulative[index] = cumulative[index - 1] +
                    Vector3.Distance(handPositions[index - 1], handPositions[index]);
            var totalMotion = cumulative[cumulative.Length - 1];
            if (totalMotion < 0.1f)
                throw new InvalidOperationException(
                    "The supplied right-hand motion is insufficient to aim the musket.");
            var maximumReachIndex = handPositions.Count - 1;
            var aimMotion = cumulative[maximumReachIndex];
            if (aimMotion < 0.1f)
                throw new InvalidOperationException(
                    "The supplied right hand has no measurable forward reach for aiming.");

            var progressValues = new float[times.Count];
            var desiredDirections = new Vector3[times.Count];
            for (var index = 0; index < times.Count; index++)
            {
                var progress = Mathf.Clamp01(cumulative[index] / aimMotion);
                progress = progress * progress * (3f - 2f * progress);
                progressValues[index] = progress;
                var desiredMuzzle = Vector3.Slerp(
                    grabMuzzleDirection, characterForward, progress).normalized;
                desiredDirections[index] = desiredMuzzle;
            }

            var rightShoulderRotations = new Quaternion[times.Count];
            var rightArmRotations = new Quaternion[times.Count];
            var rightForeArmRotations = new Quaternion[times.Count];
            var rightHandLocalRotations = new Quaternion[times.Count];
            var liftedRightHandRotations = new Quaternion[times.Count];
            var liftedRootPositions = new Vector3[times.Count];
            var liftedBaselineRootRotations = new Quaternion[times.Count];
            var rootLocalPosition = handMusketRoot.localPosition;
            for (var index = 0; index < times.Count; index++)
            {
                var desiredRootPosition = rootPositions[index] +
                    Vector3.up * (FinalAimArmLift * progressValues[index]);
                SolveRightArmLiftForMusketRoot(
                    rightShoulderPositions[index],
                    rightArmPositions[index],
                    rightForeArmPositions[index],
                    handPositions[index],
                    rightShoulderParentRotations[index],
                    rightShoulderWorldRotations[index],
                    rightArmParentRotations[index],
                    rightArmWorldRotations[index],
                    rightForeArmWorldRotations[index],
                    rightHandRotations[index],
                    rightHandScales[index],
                    rootLocalPosition,
                    desiredRootPosition,
                    out rightShoulderRotations[index],
                    out rightArmRotations[index],
                    out rightForeArmRotations[index],
                    out rightHandLocalRotations[index],
                    out liftedRightHandRotations[index],
                    out liftedRootPositions[index]);
                MakeQuaternionContinuous(rightShoulderRotations, index);
                MakeQuaternionContinuous(rightArmRotations, index);
                MakeQuaternionContinuous(rightForeArmRotations, index);
                MakeQuaternionContinuous(rightHandLocalRotations, index);
                liftedBaselineRootRotations[index] =
                    liftedRightHandRotations[index] *
                    (Quaternion.Inverse(rightHandRotations[index]) *
                     baselineRootRotations[index]);
            }

            var rightShoulderPath = AnimationUtility.CalculateTransformPath(
                rightShoulder, model);
            var rightArmPath = AnimationUtility.CalculateTransformPath(rightArm, model);
            var rightForeArmPath = AnimationUtility.CalculateTransformPath(rightForeArm, model);
            var rightHandPath = AnimationUtility.CalculateTransformPath(rightHand, model);
            SetQuaternionOverrideCurves(
                clip, source, rightShoulderPath, times, rightShoulderRotations);
            SetQuaternionOverrideCurves(
                clip, source, rightArmPath, times, rightArmRotations);
            SetQuaternionOverrideCurves(
                clip, source, rightForeArmPath, times, rightForeArmRotations);
            SetQuaternionOverrideCurves(
                clip, source, rightHandPath, times, rightHandLocalRotations);

            var baseDesiredWorldRotations = new Quaternion[times.Count];
            for (var index = 0; index < times.Count; index++)
            {
                var currentMuzzle =
                    liftedBaselineRootRotations[index] * rootLocalMuzzleAxis;
                baseDesiredWorldRotations[index] =
                    Quaternion.FromToRotation(
                        currentMuzzle, desiredDirections[index]) *
                    liftedBaselineRootRotations[index];
            }

            var gripVertex = vertices[gripVertexIndex];
            var supportVertexIndices = DetermineApprovedLeatherForegripVertexIndices(
                handMusket, gripVertex, localMuzzleAxis);
            if (supportVertexIndices.Length == 0)
                throw new InvalidOperationException(
                    "The approved musket has no support surface ahead of the right-hand grip.");
            // Keep one local grip rotation relative to the right hand and blend only
            // toward one final local aim rotation. World-space per-frame correction
            // would cancel the approved Mixamo right-arm rotation and make the gun
            // appear angle-locked while the arm moves around it.
            var lastIndex = times.Count - 1;
            var localStockAndTriggerDown = DetermineMusketLocalStockAndTriggerDownAxis(
                SharedMesh(handMusket), localMuzzleAxis);
            var rootLocalStockAndTriggerDown = rendererLocal
                .MultiplyVector(localStockAndTriggerDown).normalized;
            var baseFinalStockAndTriggerDown =
                baseDesiredWorldRotations[lastIndex] * rootLocalStockAndTriggerDown;
            var desiredWorldDown = Vector3.ProjectOnPlane(
                Vector3.down, desiredDirections[lastIndex]).normalized;
            if (desiredWorldDown.sqrMagnitude < 0.999f)
                throw new InvalidOperationException(
                    "The forward muzzle axis does not establish a stable world-down plane.");
            // The approved musket side silhouette establishes its authored -Y as the
            // shared stock-thickness/trigger underside. Align that measured mesh axis
            // with character down instead of choosing a hand-distance roll symmetry.
            var stockAndTriggerDownRoll = Vector3.SignedAngle(
                baseFinalStockAndTriggerDown,
                desiredWorldDown,
                desiredDirections[lastIndex]);
            var grabLocalRotation =
                Quaternion.Inverse(liftedRightHandRotations[0]) *
                liftedBaselineRootRotations[0];
            var finalDesiredWorldRotation = Quaternion.AngleAxis(
                stockAndTriggerDownRoll, desiredDirections[lastIndex]) *
                baseDesiredWorldRotations[lastIndex];
            var finalLocalRotation =
                Quaternion.Inverse(liftedRightHandRotations[lastIndex]) *
                finalDesiredWorldRotation;
            var rotations = new Quaternion[times.Count];
            var desiredRootWorldRotations = new Quaternion[times.Count];
            for (var index = 0; index < times.Count; index++)
            {
                rotations[index] = Quaternion.Slerp(
                    grabLocalRotation, finalLocalRotation, progressValues[index]);
                desiredRootWorldRotations[index] =
                    liftedRightHandRotations[index] * rotations[index];
                if (index > 0 && Quaternion.Dot(rotations[index - 1], rotations[index]) < 0f)
                    rotations[index] = new Quaternion(
                        -rotations[index].x, -rotations[index].y,
                        -rotations[index].z, -rotations[index].w);
            }

            var path = AnimationUtility.CalculateTransformPath(handMusketRoot, model);
            SetQuaternionCurve(clip, path, "m_LocalRotation.x", times, rotations, item => item.x);
            SetQuaternionCurve(clip, path, "m_LocalRotation.y", times, rotations, item => item.y);
            SetQuaternionCurve(clip, path, "m_LocalRotation.z", times, rotations, item => item.z);
            SetQuaternionCurve(clip, path, "m_LocalRotation.w", times, rotations, item => item.w);

            var finalRootWorld = Matrix4x4.TRS(
                liftedRootPositions[liftedRootPositions.Length - 1],
                desiredRootWorldRotations[desiredRootWorldRotations.Length - 1],
                rootScales[rootScales.Count - 1]) * rendererLocal;
            var supportVertexIndex = supportVertexIndices.OrderBy(index => Vector3.Distance(
                leftHandPositions[leftHandPositions.Count - 1],
                finalRootWorld.MultiplyPoint3x4(vertices[index]))).First();
            var wristSurfaceOffset = Mathf.Max(
                0f,
                Vector3.Distance(handPositions[0], rootPositions[0]) -
                SupportHandSurfaceDistanceTolerance);
            var leftArmRotations = new Quaternion[times.Count];
            var leftForeArmRotations = new Quaternion[times.Count];
            for (var index = 0; index < times.Count; index++)
            {
                var musketWorld = Matrix4x4.TRS(
                    liftedRootPositions[index], desiredRootWorldRotations[index],
                    rootScales[index]) * rendererLocal;
                var supportPoint = musketWorld.MultiplyPoint3x4(
                    vertices[supportVertexIndex]);
                var surfaceToWrist = leftHandPositions[index] - supportPoint;
                if (surfaceToWrist.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException(
                        "The left wrist direction at the musket support surface is undefined.");
                var wristTarget = supportPoint +
                    surfaceToWrist.normalized * wristSurfaceOffset;
                SolveLeftArmSupportIk(
                    leftArmPositions[index],
                    leftForeArmPositions[index],
                    leftHandPositions[index],
                    leftArmParentRotations[index],
                    leftArmWorldRotations[index],
                    leftForeArmWorldRotations[index],
                    out var targetArmLocal,
                    out var targetForeArmLocal,
                    wristTarget);
                leftArmRotations[index] = Quaternion.Slerp(
                    leftArmLocalRotations[index], targetArmLocal,
                    progressValues[index]);
                leftForeArmRotations[index] = Quaternion.Slerp(
                    leftForeArmLocalRotations[index], targetForeArmLocal,
                    progressValues[index]);
                MakeQuaternionContinuous(leftArmRotations, index);
                MakeQuaternionContinuous(leftForeArmRotations, index);
            }
            var leftArmPath = AnimationUtility.CalculateTransformPath(leftArm, model);
            var leftForeArmPath = AnimationUtility.CalculateTransformPath(leftForeArm, model);
            SetQuaternionOverrideCurves(
                clip, source, leftArmPath, times, leftArmRotations);
            SetQuaternionOverrideCurves(
                clip, source, leftForeArmPath, times, leftForeArmRotations);
        }

        private static void SolveRightArmLiftForMusketRoot(
            Vector3 clavicle,
            Vector3 shoulder,
            Vector3 elbow,
            Vector3 wrist,
            Quaternion shoulderParentWorldRotation,
            Quaternion shoulderWorldRotation,
            Quaternion armParentWorldRotation,
            Quaternion armWorldRotation,
            Quaternion foreArmWorldRotation,
            Quaternion wristWorldRotation,
            Vector3 wristWorldScale,
            Vector3 musketRootLocalPosition,
            Vector3 desiredMusketRootPosition,
            out Quaternion shoulderLocalRotation,
            out Quaternion armLocalRotation,
            out Quaternion foreArmLocalRotation,
            out Quaternion handLocalRotation,
            out Quaternion solvedWristWorldRotation,
            out Vector3 solvedMusketRootPosition)
        {
            var wristTarget = wrist + (desiredMusketRootPosition -
                Matrix4x4.TRS(
                    wrist, wristWorldRotation, wristWorldScale)
                    .MultiplyPoint3x4(musketRootLocalPosition));
            shoulderLocalRotation = Quaternion.identity;
            armLocalRotation = Quaternion.identity;
            foreArmLocalRotation = Quaternion.identity;
            handLocalRotation = Quaternion.identity;
            solvedWristWorldRotation = wristWorldRotation;
            solvedMusketRootPosition = Vector3.zero;
            var sourceMaximumArmReach = Vector3.Distance(shoulder, elbow) +
                                        Vector3.Distance(elbow, wrist) - 0.0001f;
            var useShoulderLift = Vector3.Distance(shoulder, wristTarget) >
                                  sourceMaximumArmReach;
            SolveMinimumShoulderReachRotation(
                    clavicle, shoulder, elbow, wrist,
                    shoulderParentWorldRotation,
                    shoulderWorldRotation,
                    armParentWorldRotation,
                    armWorldRotation,
                    foreArmWorldRotation,
                    wristWorldRotation,
                    wristTarget,
                    useShoulderLift,
                    out shoulderLocalRotation,
                    out var liftedShoulder,
                    out var liftedElbow,
                    out var liftedWrist,
                    out var liftedArmParentWorldRotation,
                    out var liftedArmWorldRotation,
                    out var liftedForeArmWorldRotation,
                    out var liftedWristWorldRotation);
            SolveTwoBoneArmIkWithWristRotation(
                    liftedShoulder, liftedElbow, liftedWrist,
                    liftedArmParentWorldRotation,
                    liftedArmWorldRotation,
                    liftedForeArmWorldRotation,
                    liftedWristWorldRotation,
                    wristTarget,
                    out armLocalRotation,
                    out foreArmLocalRotation,
                    out _,
                    out var solvedWristPosition);
            var solvedArmWorldRotation = liftedArmParentWorldRotation *
                                         armLocalRotation;
            var solvedForeArmWorldRotation = solvedArmWorldRotation *
                                             foreArmLocalRotation;
            solvedWristWorldRotation = wristWorldRotation;
            handLocalRotation = Quaternion.Inverse(solvedForeArmWorldRotation) *
                                solvedWristWorldRotation;
            solvedMusketRootPosition = Matrix4x4.TRS(
                        solvedWristPosition,
                        solvedWristWorldRotation,
                        wristWorldScale)
                    .MultiplyPoint3x4(musketRootLocalPosition);
            var error = Vector3.Distance(
                desiredMusketRootPosition, solvedMusketRootPosition);
            if (error > 0.0005f)
                throw new InvalidOperationException(
                    "The approved 0.15m arm lift is outside the right-arm IK reach: " +
                    Num(error) + "m.");
        }

        private static void SolveMinimumShoulderReachRotation(
            Vector3 clavicle,
            Vector3 shoulder,
            Vector3 elbow,
            Vector3 wrist,
            Quaternion shoulderParentWorldRotation,
            Quaternion shoulderWorldRotation,
            Quaternion armParentWorldRotation,
            Quaternion armWorldRotation,
            Quaternion foreArmWorldRotation,
            Quaternion wristWorldRotation,
            Vector3 wristTarget,
            bool useShoulderLift,
            out Quaternion shoulderLocalRotation,
            out Vector3 liftedShoulder,
            out Vector3 liftedElbow,
            out Vector3 liftedWrist,
            out Quaternion liftedArmParentWorldRotation,
            out Quaternion liftedArmWorldRotation,
            out Quaternion liftedForeArmWorldRotation,
            out Quaternion liftedWristWorldRotation)
        {
            var clavicleLength = Vector3.Distance(clavicle, shoulder);
            var upperLength = Vector3.Distance(shoulder, elbow);
            var lowerLength = Vector3.Distance(elbow, wrist);
            var maximumArmReach = upperLength + lowerLength - 0.0001f;
            var shoulderTarget = shoulder;
            if (useShoulderLift)
            {
                const float elbowBendReserve = 0.015f;
                var preferredArmReach = maximumArmReach - elbowBendReserve;
                var clavicleToTarget = wristTarget - clavicle;
                var centerDistance = clavicleToTarget.magnitude;
                if (centerDistance >= clavicleLength + preferredArmReach)
                    throw new InvalidOperationException(
                        "The approved 0.15m arm lift exceeds the shoulder-and-arm reach.");
                var targetDirection = clavicleToTarget / centerDistance;
                var along = (clavicleLength * clavicleLength -
                             preferredArmReach * preferredArmReach +
                             centerDistance * centerDistance) /
                            (2f * centerDistance);
                var radialLength = Mathf.Sqrt(Mathf.Max(
                    0f, clavicleLength * clavicleLength - along * along));
                var radialDirection = Vector3.ProjectOnPlane(
                    shoulder - clavicle, targetDirection);
                if (radialDirection.sqrMagnitude < 0.000001f)
                    radialDirection = Vector3.ProjectOnPlane(
                        elbow - shoulder, targetDirection);
                if (radialDirection.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException(
                        "The right shoulder does not establish a stable lift plane.");
                shoulderTarget = clavicle + targetDirection * along +
                                 radialDirection.normalized * radialLength;
            }

            var shoulderDelta = Quaternion.FromToRotation(
                shoulder - clavicle, shoulderTarget - clavicle);
            var liftedShoulderWorldRotation = shoulderDelta * shoulderWorldRotation;
            shoulderLocalRotation = Quaternion.Inverse(shoulderParentWorldRotation) *
                                    liftedShoulderWorldRotation;
            liftedShoulder = shoulderTarget;
            liftedElbow = clavicle + shoulderDelta * (elbow - clavicle);
            liftedWrist = clavicle + shoulderDelta * (wrist - clavicle);
            liftedArmParentWorldRotation = shoulderDelta * armParentWorldRotation;
            liftedArmWorldRotation = shoulderDelta * armWorldRotation;
            liftedForeArmWorldRotation = shoulderDelta * foreArmWorldRotation;
            liftedWristWorldRotation = shoulderDelta * wristWorldRotation;
        }

        private static void SolveTwoBoneArmIkWithWristRotation(
            Vector3 shoulder,
            Vector3 elbow,
            Vector3 wrist,
            Quaternion armParentWorldRotation,
            Quaternion armWorldRotation,
            Quaternion foreArmWorldRotation,
            Quaternion wristWorldRotation,
            Vector3 wristTarget,
            out Quaternion armLocalRotation,
            out Quaternion foreArmLocalRotation,
            out Quaternion solvedWristWorldRotation,
            out Vector3 solvedWristPosition)
        {
            var upperLength = Vector3.Distance(shoulder, elbow);
            var lowerLength = Vector3.Distance(elbow, wrist);
            var shoulderToTarget = wristTarget - shoulder;
            var targetDistance = Mathf.Clamp(
                shoulderToTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            var targetDirection = shoulderToTarget.normalized;
            var originalElbowDirection = elbow - shoulder;
            var bendDirection = Vector3.ProjectOnPlane(
                originalElbowDirection, targetDirection);
            if (bendDirection.sqrMagnitude < 0.000001f)
                bendDirection = Vector3.ProjectOnPlane(
                    wrist - elbow, targetDirection);
            if (bendDirection.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    "The supplied right arm does not establish an IK bend direction.");
            bendDirection.Normalize();
            var along = (upperLength * upperLength - lowerLength * lowerLength +
                         targetDistance * targetDistance) / (2f * targetDistance);
            var bend = Mathf.Sqrt(Mathf.Max(
                0f, upperLength * upperLength - along * along));
            var targetElbow = shoulder + targetDirection * along +
                              bendDirection * bend;
            var upperDelta = Quaternion.FromToRotation(
                elbow - shoulder, targetElbow - shoulder);
            var targetArmWorld = upperDelta * armWorldRotation;
            var foreArmAfterUpper = upperDelta * foreArmWorldRotation;
            var wristRotationAfterUpper = upperDelta * wristWorldRotation;
            var lowerDirectionAfterUpper = upperDelta * (wrist - elbow);
            var lowerDelta = Quaternion.FromToRotation(
                lowerDirectionAfterUpper, wristTarget - targetElbow);
            var targetForeArmWorld = lowerDelta * foreArmAfterUpper;
            armLocalRotation = Quaternion.Inverse(armParentWorldRotation) *
                               targetArmWorld;
            foreArmLocalRotation = Quaternion.Inverse(targetArmWorld) *
                                   targetForeArmWorld;
            solvedWristWorldRotation = lowerDelta * wristRotationAfterUpper;
            solvedWristPosition = targetElbow +
                                  lowerDelta * lowerDirectionAfterUpper;
        }

        private static void SolveLeftArmSupportIk(
            Vector3 shoulder,
            Vector3 elbow,
            Vector3 wrist,
            Quaternion armParentWorldRotation,
            Quaternion armWorldRotation,
            Quaternion foreArmWorldRotation,
            out Quaternion armLocalRotation,
            out Quaternion foreArmLocalRotation,
            Vector3 wristTarget)
        {
            var upperLength = Vector3.Distance(shoulder, elbow);
            var lowerLength = Vector3.Distance(elbow, wrist);
            var shoulderToTarget = wristTarget - shoulder;
            var targetDistance = Mathf.Clamp(
                shoulderToTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            var targetDirection = shoulderToTarget.normalized;
            var originalElbowDirection = elbow - shoulder;
            var bendDirection = Vector3.ProjectOnPlane(
                originalElbowDirection, targetDirection);
            if (bendDirection.sqrMagnitude < 0.000001f)
                bendDirection = Vector3.ProjectOnPlane(
                    wrist - elbow, targetDirection);
            if (bendDirection.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    "The supplied left arm does not establish an IK bend direction.");
            bendDirection.Normalize();
            var along = (upperLength * upperLength - lowerLength * lowerLength +
                         targetDistance * targetDistance) / (2f * targetDistance);
            var bend = Mathf.Sqrt(Mathf.Max(
                0f, upperLength * upperLength - along * along));
            var targetElbow = shoulder + targetDirection * along +
                              bendDirection * bend;
            var upperDelta = Quaternion.FromToRotation(
                elbow - shoulder, targetElbow - shoulder);
            var targetArmWorld = upperDelta * armWorldRotation;
            var foreArmAfterUpper = upperDelta * foreArmWorldRotation;
            var lowerDirectionAfterUpper = upperDelta * (wrist - elbow);
            var lowerDelta = Quaternion.FromToRotation(
                lowerDirectionAfterUpper, wristTarget - targetElbow);
            var targetForeArmWorld = lowerDelta * foreArmAfterUpper;
            armLocalRotation = Quaternion.Inverse(armParentWorldRotation) *
                               targetArmWorld;
            foreArmLocalRotation = Quaternion.Inverse(targetArmWorld) *
                                   targetForeArmWorld;
        }

        private static void MakeQuaternionContinuous(
            Quaternion[] rotations, int index)
        {
            if (index == 0 || Quaternion.Dot(rotations[index - 1], rotations[index]) >= 0f)
                return;
            rotations[index] = new Quaternion(
                -rotations[index].x, -rotations[index].y,
                -rotations[index].z, -rotations[index].w);
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations)
        {
            SetQuaternionCurve(clip, path, "m_LocalRotation.x", times, rotations, item => item.x);
            SetQuaternionCurve(clip, path, "m_LocalRotation.y", times, rotations, item => item.y);
            SetQuaternionCurve(clip, path, "m_LocalRotation.z", times, rotations, item => item.z);
            SetQuaternionCurve(clip, path, "m_LocalRotation.w", times, rotations, item => item.w);
        }

        private static void SetQuaternionOverrideCurves(
            AnimationClip clip,
            AnimationClip source,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations)
        {
            SetQuaternionOverrideCurve(
                clip, source, path, "m_LocalRotation.x", times, rotations, item => item.x);
            SetQuaternionOverrideCurve(
                clip, source, path, "m_LocalRotation.y", times, rotations, item => item.y);
            SetQuaternionOverrideCurve(
                clip, source, path, "m_LocalRotation.z", times, rotations, item => item.z);
            SetQuaternionOverrideCurve(
                clip, source, path, "m_LocalRotation.w", times, rotations, item => item.w);
        }

        private static void SetQuaternionOverrideCurve(
            AnimationClip clip,
            AnimationClip source,
            string path,
            string propertyName,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations,
            Func<Quaternion, float> component)
        {
            var binding = EditorCurveBinding.FloatCurve(
                path, typeof(Transform), propertyName);
            var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                throw new InvalidOperationException(
                    "A required left-arm source rotation curve is missing: " +
                    path + "/" + propertyName + ".");
            var keys = sourceCurve.keys
                .Where(key => key.time < times[0] - 0.000001f)
                .Concat(times.Select((time, index) =>
                    new Keyframe(time, component(rotations[index]))))
                .ToArray();
            var curve = new AnimationCurve(keys)
            {
                preWrapMode = sourceCurve.preWrapMode,
                postWrapMode = sourceCurve.postWrapMode
            };
            var prefixCount = keys.Length - times.Count;
            for (var index = prefixCount; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static float FindOptimalSupportRoll(
            int firstSupportIndex,
            IReadOnlyList<Vector3> leftHandPositions,
            IReadOnlyList<Vector3> rootPositions,
            IReadOnlyList<Vector3> rootScales,
            IReadOnlyList<Vector3> desiredDirections,
            IReadOnlyList<Quaternion> baseDesiredWorldRotations,
            Matrix4x4 rendererLocal,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> supportVertexIndices)
        {
            var bestRoll = 0f;
            var bestAverageDistance = float.PositiveInfinity;
            for (var roll = -180; roll < 180; roll++)
            {
                var sum = 0f;
                var count = 0;
                for (var index = firstSupportIndex;
                     index < leftHandPositions.Count;
                     index++)
                {
                    var rootRotation = Quaternion.AngleAxis(
                        roll, desiredDirections[index]) *
                        baseDesiredWorldRotations[index];
                    sum += MinimumSupportSurfaceDistance(
                        leftHandPositions[index],
                        rootPositions[index],
                        rootRotation,
                        rootScales[index],
                        rendererLocal,
                        vertices,
                        supportVertexIndices);
                    count++;
                }
                var average = sum / count;
                if (average >= bestAverageDistance)
                    continue;
                bestAverageDistance = average;
                bestRoll = roll;
            }
            return bestRoll;
        }

        private static float MinimumSupportSurfaceDistance(
            Vector3 handPosition,
            Vector3 rootPosition,
            Quaternion rootRotation,
            Vector3 rootScale,
            Matrix4x4 rendererLocal,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> vertexIndices)
        {
            var world = Matrix4x4.TRS(rootPosition, rootRotation, rootScale) *
                        rendererLocal;
            var minimum = float.PositiveInfinity;
            foreach (var index in vertexIndices)
                minimum = Mathf.Min(minimum, Vector3.Distance(
                    handPosition, world.MultiplyPoint3x4(vertices[index])));
            return minimum;
        }

        private static Vector3 DetermineMusketLocalMuzzleAxis(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices.Length < 4)
                throw new InvalidOperationException(
                    "The approved musket mesh has too few vertices to establish its muzzle.");
            var first = 0;
            var second = 1;
            var maximumSquaredDistance = 0f;
            for (var left = 0; left < vertices.Length; left++)
            for (var right = left + 1; right < vertices.Length; right++)
            {
                var squaredDistance = (vertices[right] - vertices[left]).sqrMagnitude;
                if (squaredDistance <= maximumSquaredDistance)
                    continue;
                maximumSquaredDistance = squaredDistance;
                first = left;
                second = right;
            }
            var axis = (vertices[second] - vertices[first]).normalized;
            var projections = vertices.Select(vertex =>
                Vector3.Dot(vertex - vertices[first], axis)).ToArray();
            var length = Mathf.Sqrt(maximumSquaredDistance);
            var endRange = length * 0.2f;
            var firstSpread = vertices.Where((vertex, index) => projections[index] <= endRange)
                .Average(vertex => Vector3.Cross(vertex - vertices[first], axis).magnitude);
            var secondSpread = vertices.Where((vertex, index) => projections[index] >= length - endRange)
                .Average(vertex => Vector3.Cross(vertex - vertices[second], axis).magnitude);
            if (Mathf.Abs(firstSpread - secondSpread) < 0.0001f)
                throw new InvalidOperationException(
                    "The approved musket geometry does not distinguish stock from muzzle.");
            return secondSpread < firstSpread ? axis : -axis;
        }

        private static Vector3 DetermineMusketLocalStockAndTriggerDownAxis(
            Mesh mesh, Vector3 localMuzzleAxis)
        {
            var localDown = Vector3.ProjectOnPlane(Vector3.down, localMuzzleAxis);
            if (localDown.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    "The approved musket authoring down axis is parallel to its muzzle axis.");
            localDown.Normalize();

            var localSide = Vector3.Cross(localMuzzleAxis, localDown).normalized;
            var vertices = mesh.vertices;
            var projections = vertices.Select(vertex =>
                Vector3.Dot(vertex, localMuzzleAxis)).ToArray();
            var minimum = projections.Min();
            var maximum = projections.Max();
            var stockLimit = Mathf.Lerp(minimum, maximum, 0.25f);
            var stockVertices = vertices.Where((vertex, index) =>
                projections[index] <= stockLimit).ToArray();
            if (stockVertices.Length < 4)
                throw new InvalidOperationException(
                    "The approved musket stock region cannot establish its thick downward side.");
            var downSpan = stockVertices.Max(vertex => Vector3.Dot(vertex, localDown)) -
                           stockVertices.Min(vertex => Vector3.Dot(vertex, localDown));
            var sideSpan = stockVertices.Max(vertex => Vector3.Dot(vertex, localSide)) -
                           stockVertices.Min(vertex => Vector3.Dot(vertex, localSide));
            if (downSpan <= sideSpan * 1.25f)
                throw new InvalidOperationException(
                    "The approved musket authored down axis does not match the broad stock side: " +
                    "DownSpan=" + Num(downSpan) + ", SideSpan=" + Num(sideSpan) + ".");
            return localDown;
        }

        private static int[] DetermineApprovedLeatherForegripVertexIndices(
            MeshRenderer renderer,
            Vector3 gripVertex,
            Vector3 localMuzzleAxis)
        {
            var mesh = SharedMesh(renderer);
            var materials = renderer.sharedMaterials;
            var leatherSubMesh = Array.FindIndex(
                materials,
                material => material != null &&
                    NormalizeMaterialName(material.name) == "Ispant_Leather_Approved");
            if (leatherSubMesh < 0 || leatherSubMesh >= mesh.subMeshCount)
                throw new InvalidOperationException(
                    "The approved musket leather foregrip submesh is missing.");
            var vertices = mesh.vertices;
            var supportVertexIndices = mesh.GetTriangles(leatherSubMesh)
                .Distinct()
                .Where(index => Vector3.Dot(
                    vertices[index] - gripVertex, localMuzzleAxis) > 0f)
                .ToArray();
            if (supportVertexIndices.Length == 0)
                throw new InvalidOperationException(
                    "The approved leather foregrip has no surface ahead of the right-hand grip.");
            return supportVertexIndices;
        }

        private static void SetQuaternionCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations,
            Func<Quaternion, float> component)
        {
            var keys = new Keyframe[times.Count + 1];
            keys[0] = new Keyframe(0f, component(rotations[0]));
            for (var index = 0; index < times.Count; index++)
                keys[index + 1] = new Keyframe(times[index], component(rotations[index]));
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static void SetSequenceDefaultVisibility(Transform model)
        {
            RequireRenderer<MeshRenderer>(model, SwordRendererName).enabled = true;
            RequireRenderer<MeshRenderer>(model, WaistSwordRendererName).enabled = false;
            RequireRenderer<MeshRenderer>(model, MusketName).enabled = true;
            RequireRenderer<MeshRenderer>(model, HandMusketRendererName).enabled = false;
        }

        private static AnimatorController CreateOrUpdateSheathToRifleController(
            AnimationClip sheathClip,
            AnimationClip holdClip,
            AnimationClip bridgeClip,
            AnimationClip rifleClip)
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
            var sheathState = stateMachine.AddState(StateName);
            sheathState.motion = sheathClip;
            sheathState.speed = 1f;
            sheathState.writeDefaultValues = true;
            var holdState = stateMachine.AddState(StaticHoldStateName);
            holdState.motion = holdClip;
            holdState.speed = 1f;
            holdState.writeDefaultValues = true;
            var bridgeState = stateMachine.AddState(SheathToRifleBridgeStateName);
            bridgeState.motion = bridgeClip;
            bridgeState.speed = 1f;
            bridgeState.writeDefaultValues = true;
            var rifleState = stateMachine.AddState(ChangeToRifleStateName);
            rifleState.motion = rifleClip;
            rifleState.speed = 1f;
            rifleState.writeDefaultValues = true;
            ConfigureExactExitTransition(sheathState.AddTransition(holdState));
            ConfigureExactExitTransition(holdState.AddTransition(bridgeState));
            ConfigureExactExitTransition(bridgeState.AddTransition(rifleState));
            ConfigureExactExitTransition(rifleState.AddTransition(sheathState));
            stateMachine.defaultState = sheathState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static RifleSequenceMetrics InspectSheathToRifleSequence(
            Transform model,
            Transform staticModel,
            Transform drawModel,
            Animator animator,
            AnimationClip sheathClip,
            AnimationClip holdClip,
            AnimationClip bridgeSourceClip,
            AnimationClip bridgeClip,
            AnimationClip rifleSourceClip,
            AnimationClip rifleClip,
            AnimatorController controller)
        {
            InspectModel(model, staticModel, drawModel, animator, sheathClip, controller);
            if (Mathf.Abs(holdClip.length - StaticHoldDuration) > StaticHoldTolerance ||
                AnimationUtility.GetAnimationClipSettings(holdClip).loopTime)
                throw new InvalidOperationException(
                    "The existing slot-6 final-pose hold is not exactly 0.5 seconds.");
            if (AnimationUtility.GetAnimationClipSettings(bridgeSourceClip).loopTime ||
                AnimationUtility.GetAnimationClipSettings(bridgeClip).loopTime ||
                Mathf.Abs(bridgeSourceClip.length - bridgeClip.length) > 0.0001f ||
                AnimationUtility.GetAnimationClipSettings(rifleSourceClip).loopTime ||
                AnimationUtility.GetAnimationClipSettings(rifleClip).loopTime ||
                Mathf.Abs(rifleSourceClip.length - rifleClip.length) > 0.0001f)
                throw new InvalidOperationException(
                    "The bridge or change-to-rifle source/runtime duration or loop setting differs.");

            var states = controller.layers[0].stateMachine.states
                .Select(item => item.state).ToArray();
            if (states.Length != 4)
                throw new InvalidOperationException(
                    "The slot-6 sheath-to-rifle controller must contain exactly four states.");
            var sheathState = states.SingleOrDefault(item => item.name == StateName) ??
                throw new InvalidOperationException("The slot-6 sheath state is missing.");
            var holdState = states.SingleOrDefault(item => item.name == StaticHoldStateName) ??
                throw new InvalidOperationException("The slot-6 hold state is missing.");
            var bridgeState = states.SingleOrDefault(
                item => item.name == SheathToRifleBridgeStateName) ??
                throw new InvalidOperationException("The slot-6 bridge state is missing.");
            var rifleState = states.SingleOrDefault(item => item.name == ChangeToRifleStateName) ??
                throw new InvalidOperationException("The slot-6 change-to-rifle state is missing.");
            if (sheathState.motion != sheathClip || holdState.motion != holdClip ||
                bridgeState.motion != bridgeClip ||
                rifleState.motion != rifleClip ||
                controller.layers[0].stateMachine.defaultState != sheathState)
                throw new InvalidOperationException(
                    "The slot-6 sheath/hold/bridge/rifle state motion sequence differs.");
            RequireExactExitTransition(sheathState, holdState);
            RequireExactExitTransition(holdState, bridgeState);
            RequireExactExitTransition(bridgeState, rifleState);
            RequireExactExitTransition(rifleState, sheathState);

            var bridgeSourceBindings = AnimationUtility.GetCurveBindings(rifleSourceClip);
            var bridgeRuntimeBindings = AnimationUtility.GetCurveBindings(bridgeClip);
            if (bridgeRuntimeBindings.Length != bridgeSourceBindings.Length + 4)
                throw new InvalidOperationException(
                    "The runtime bridge clip must add exactly four weapon-visibility curves.");

            var sourceBindings = bridgeSourceBindings;
            var runtimeBindings = AnimationUtility.GetCurveBindings(rifleClip);
            if (runtimeBindings.Length != sourceBindings.Length + 8)
                throw new InvalidOperationException(
                    "The runtime rifle clip must add four visibility and four aim-rotation curves.");
            foreach (var binding in sourceBindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(rifleSourceClip, binding) ??
                    throw new InvalidOperationException("A source rifle curve is missing.");
                var runtimeCurve = AnimationUtility.GetEditorCurve(rifleClip, binding) ??
                    throw new InvalidOperationException(
                        "The runtime rifle clip is missing an original Mixamo curve: " +
                        binding.path + "/" + binding.propertyName + ".");
                if (!IsAimArmOverrideBinding(binding))
                    RequireSameCurve(sourceCurve, runtimeCurve, binding);
            }

            var handSword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var backMusket = RequireRenderer<MeshRenderer>(model, MusketName);
            var handMusket = RequireRenderer<MeshRenderer>(model, HandMusketRendererName);
            var handMusketRoot = handMusket.transform.parent;
            var waistSwordRoot = waistSword.transform.parent;
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            var rightShoulder = RequireDescendant(model, "mixamorig:RightShoulder");
            var rightArm = RequireDescendant(model, "mixamorig:RightArm");
            var rightForeArm = RequireDescendant(model, "mixamorig:RightForeArm");
            var leftHand = RequireDescendant(model, "mixamorig:LeftHand");
            var hips = RequireDescendant(model, "mixamorig:Hips");
            var spine2 = RequireDescendant(model, "mixamorig:Spine2");
            var handMusketMesh = SharedMesh(handMusket);
            var handMusketVertices = handMusketMesh.vertices;
            var localMuzzleAxis = DetermineMusketLocalMuzzleAxis(handMusketMesh);
            var localStockAndTriggerDown = DetermineMusketLocalStockAndTriggerDownAxis(
                handMusketMesh, localMuzzleAxis);
            var characterForward = DetermineCharacterForward(model);
            if (backMusket.transform.parent != spine2)
                throw new InvalidOperationException("The pre-grab musket is not rigid on Spine2.");
            if (handMusketRoot == null || handMusketRoot.parent != rightHand ||
                handMusketRoot.name != HandMusketRootName)
                throw new InvalidOperationException(
                    "The post-grab musket is not rigid on mixamorig:RightHand.");
            if (waistSwordRoot == null || waistSwordRoot.parent != hips ||
                waistSwordRoot.name != WaistSwordRootName)
                throw new InvalidOperationException(
                    "The left-waist sword is not rigidly attached under mixamorig:Hips.");
            if (SharedMesh(handMusket) != SharedMesh(backMusket) ||
                !handMusket.sharedMaterials.SequenceEqual(backMusket.sharedMaterials))
                throw new InvalidOperationException(
                    "The hand musket is not an exact shared-mesh/material clone of the back musket.");
            var handMusketRootPath =
                AnimationUtility.CalculateTransformPath(handMusketRoot, model);
            var musketRootPositionCurveCount = runtimeBindings.Count(binding =>
                binding.path == handMusketRootPath &&
                binding.propertyName.StartsWith(
                    "m_LocalPosition.", StringComparison.Ordinal));
            if (musketRootPositionCurveCount != 0)
                throw new InvalidOperationException(
                    "The 0.15m final aim lift directly animates the musket root position.");
            if (!handSword.enabled || waistSword.enabled ||
                !backMusket.enabled || handMusket.enabled)
                throw new InvalidOperationException(
                    "The saved sequence-start weapon visibility differs.");

            var grab = FindRifleGrab(model, rifleSourceClip);
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var weaponRoots = new[]
            {
                handSword.transform.parent,
                waistSword.transform.parent,
                handMusketRoot
            };
            var bodyTransforms = transforms.Where(item =>
                weaponRoots.All(root => item != root && !item.IsChildOf(root))).ToArray();
            var transformSnapshots = transforms.Select(item => new TransformSnapshot(item)).ToArray();
            var rendererSnapshots = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererSnapshot(item)).ToArray();
            var maximumSheathEndToHoldError = 0f;
            var maximumHoldDrift = 0f;
            var maximumHoldToBridgeStartError = 0f;
            var maximumBridgeEndToRifleStartError = 0f;
            var bridgeRightHandMotion = 0f;
            var grabContinuityError = 0f;
            var handMusketFollowError = 0f;
            var maximumHandMusketRotationChange = 0f;
            var maximumRightGripPivotError = 0f;
            var maximumRightHandGripDistanceDrift = 0f;
            var forwardLeftHandSurfaceDistance = float.PositiveInfinity;
            var finalLeftHandSurfaceDistance = float.PositiveInfinity;
            var finalMuzzleForwardAngle = 180f;
            var forwardPoseMuzzleAngle = 180f;
            var finalStockAndTriggerDownAngle = 180f;
            var maximumPostGrabMusketMotion = 0f;
            var maximumPostGrabMusketMotionTime = grab.Time;
            var maximumArmDrivenLocalInterpolationError = 0f;
            var holdWaistSwordStaticReferenceMatrixError = float.PositiveInfinity;
            var maximumWaistSwordHipLocalMatrixDrift = 0f;
            var maximumWaistSwordBodyFollowPositionChange = 0f;
            var maximumWaistSwordBodyFollowRotationChange = 0f;
            var finalMusketRootVerticalLift = float.NegativeInfinity;
            var finalMusketRootHorizontalDrift = float.PositiveInfinity;
            var finalRightHandVerticalLift = float.NegativeInfinity;
            var finalRightHandHorizontalDrift = float.PositiveInfinity;
            var finalRightShoulderRotationOverride = 0f;
            var finalRightArmRotationOverride = 0f;
            var finalRightForeArmRotationOverride = 0f;
            var sourceFinalMusketRootPosition = Vector3.zero;
            var sourceFinalRightHandPosition = Vector3.zero;
            var sourceFinalRightShoulderLocalRotation = Quaternion.identity;
            var sourceFinalRightArmLocalRotation = Quaternion.identity;
            var sourceFinalRightForeArmLocalRotation = Quaternion.identity;
            var waistSwordHipLocalReference = LocalMatrix(waistSwordRoot);
            var waistSwordModelReference = Matrix4x4.identity;
            var postGrabRightHandPositions = new List<Vector3>();
            var postGrabSourceRightHandPositions = new List<Vector3>();
            var postGrabMusketLocalRotations = new List<Quaternion>();
            try
            {
                SetSequenceDefaultVisibility(model);
                SampleClip(model.gameObject, rifleSourceClip, rifleSourceClip.length);
                sourceFinalMusketRootPosition = handMusketRoot.position;
                sourceFinalRightHandPosition = rightHand.position;
                sourceFinalRightShoulderLocalRotation = rightShoulder.localRotation;
                sourceFinalRightArmLocalRotation = rightArm.localRotation;
                sourceFinalRightForeArmLocalRotation = rightForeArm.localRotation;

                SampleClip(model.gameObject, sheathClip, sheathClip.length);
                var sheathEnd = bodyTransforms.ToDictionary(item => item, LocalMatrix);
                SampleClip(model.gameObject, holdClip, 0f);
                foreach (var item in bodyTransforms)
                    maximumSheathEndToHoldError = Mathf.Max(
                        maximumSheathEndToHoldError,
                        MatrixError(sheathEnd[item], LocalMatrix(item)));
                waistSwordModelReference =
                    model.worldToLocalMatrix * waistSword.transform.localToWorldMatrix;
                holdWaistSwordStaticReferenceMatrixError = MatrixError(
                    StaticSwordTargetMatrix(staticModel, model, staticSword),
                    waistSwordModelReference);
                var holdReference = bodyTransforms.ToDictionary(item => item, LocalMatrix);
                SampleClip(model.gameObject, holdClip, holdClip.length);
                foreach (var item in bodyTransforms)
                    maximumHoldDrift = Mathf.Max(
                        maximumHoldDrift,
                        MatrixError(holdReference[item], LocalMatrix(item)));

                SetSequenceDefaultVisibility(model);
                SampleClip(model.gameObject, bridgeClip, 0f);
                AccumulateWaistSwordBodyFollow(
                    model, waistSwordRoot,
                    waistSwordModelReference, waistSwordHipLocalReference,
                    ref maximumWaistSwordBodyFollowPositionChange,
                    ref maximumWaistSwordBodyFollowRotationChange,
                    ref maximumWaistSwordHipLocalMatrixDrift);
                foreach (var item in bodyTransforms)
                    maximumHoldToBridgeStartError = Mathf.Max(
                        maximumHoldToBridgeStartError,
                        MatrixError(holdReference[item], LocalMatrix(item)));
                RequireRifleVisibility(
                    handSword, waistSword, backMusket, handMusket,
                    backExpected: true, handExpected: false, label: "bridge start");
                var bridgeStartHand = rightHand.position;

                SetSequenceDefaultVisibility(model);
                SampleClip(model.gameObject, bridgeClip, bridgeClip.length);
                AccumulateWaistSwordBodyFollow(
                    model, waistSwordRoot,
                    waistSwordModelReference, waistSwordHipLocalReference,
                    ref maximumWaistSwordBodyFollowPositionChange,
                    ref maximumWaistSwordBodyFollowRotationChange,
                    ref maximumWaistSwordHipLocalMatrixDrift);
                var bridgeEnd = bodyTransforms.ToDictionary(item => item, LocalMatrix);
                bridgeRightHandMotion = Vector3.Distance(bridgeStartHand, rightHand.position);
                RequireRifleVisibility(
                    handSword, waistSword, backMusket, handMusket,
                    backExpected: true, handExpected: false, label: "bridge end");

                SetSequenceDefaultVisibility(model);
                SampleClip(model.gameObject, rifleClip, 0f);
                AccumulateWaistSwordBodyFollow(
                    model, waistSwordRoot,
                    waistSwordModelReference, waistSwordHipLocalReference,
                    ref maximumWaistSwordBodyFollowPositionChange,
                    ref maximumWaistSwordBodyFollowRotationChange,
                    ref maximumWaistSwordHipLocalMatrixDrift);
                foreach (var item in bodyTransforms)
                    maximumBridgeEndToRifleStartError = Mathf.Max(
                        maximumBridgeEndToRifleStartError,
                        MatrixError(bridgeEnd[item], LocalMatrix(item)));
                RequireRifleVisibility(
                    handSword, waistSword, backMusket, handMusket,
                    backExpected: true, handExpected: false, label: "rifle start");

                SetSequenceDefaultVisibility(model);
                SampleClip(model.gameObject, rifleClip,
                    Mathf.Max(0f, grab.Time - 0.5f / ChangeToRifleFrameRate));
                AccumulateWaistSwordBodyFollow(
                    model, waistSwordRoot,
                    waistSwordModelReference, waistSwordHipLocalReference,
                    ref maximumWaistSwordBodyFollowPositionChange,
                    ref maximumWaistSwordBodyFollowRotationChange,
                    ref maximumWaistSwordHipLocalMatrixDrift);
                RequireRifleVisibility(
                    handSword, waistSword, backMusket, handMusket,
                    backExpected: true, handExpected: false, label: "pre-grab");

                SetSequenceDefaultVisibility(model);
                SampleClip(model.gameObject, rifleClip, grab.Time);
                AccumulateWaistSwordBodyFollow(
                    model, waistSwordRoot,
                    waistSwordModelReference, waistSwordHipLocalReference,
                    ref maximumWaistSwordBodyFollowPositionChange,
                    ref maximumWaistSwordBodyFollowRotationChange,
                    ref maximumWaistSwordHipLocalMatrixDrift);
                RequireRifleVisibility(
                    handSword, waistSword, backMusket, handMusket,
                    backExpected: false, handExpected: true, label: "grab");
                var backAtGrab = model.worldToLocalMatrix * backMusket.transform.localToWorldMatrix;
                var handAtGrab = model.worldToLocalMatrix * handMusket.transform.localToWorldMatrix;
                grabContinuityError = MatrixError(backAtGrab, handAtGrab);
                var handRootLocalPosition = handMusketRoot.localPosition;
                var handRootLocalScale = handMusketRoot.localScale;
                var handRootGrabRotation = handMusketRoot.localRotation;
                var grabCenter = MeshWorldCenter(handMusket);
                var rightHandGripDistance = Vector3.Distance(
                    rightHand.position, handMusketRoot.position);
                var gripVertexIndex = Enumerable.Range(0, handMusketVertices.Length)
                    .OrderBy(index => Vector3.Distance(
                        handMusketRoot.position,
                        handMusket.transform.TransformPoint(handMusketVertices[index])))
                    .First();
                var gripVertex = handMusketVertices[gripVertexIndex];
                var supportVertexIndices = DetermineApprovedLeatherForegripVertexIndices(
                    handMusket, gripVertex, localMuzzleAxis);

                for (var frame = grab.Frame; frame <= ChangeToRifleLastFrame; frame++)
                {
                    var time = ChangeToRifleTimeForFrame(frame, rifleClip);
                    SetSequenceDefaultVisibility(model);
                    SampleClip(model.gameObject, rifleClip, time);
                    AccumulateWaistSwordBodyFollow(
                        model, waistSwordRoot,
                        waistSwordModelReference, waistSwordHipLocalReference,
                        ref maximumWaistSwordBodyFollowPositionChange,
                        ref maximumWaistSwordBodyFollowRotationChange,
                        ref maximumWaistSwordHipLocalMatrixDrift);
                    RequireRifleVisibility(
                        handSword, waistSword, backMusket, handMusket,
                        backExpected: false, handExpected: true,
                        label: "post-grab frame " + frame);
                    handMusketFollowError = Mathf.Max(
                        handMusketFollowError,
                        Vector3.Distance(handRootLocalPosition, handMusketRoot.localPosition),
                        Vector3.Distance(handRootLocalScale, handMusketRoot.localScale));
                    maximumHandMusketRotationChange = Mathf.Max(
                        maximumHandMusketRotationChange,
                        Quaternion.Angle(handRootGrabRotation, handMusketRoot.localRotation));
                    maximumRightGripPivotError = Mathf.Max(
                        maximumRightGripPivotError,
                        Vector3.Distance(
                            handMusketRoot.position,
                            handMusket.transform.TransformPoint(
                                handMusketVertices[gripVertexIndex])));
                    maximumRightHandGripDistanceDrift = Mathf.Max(
                        maximumRightHandGripDistanceDrift,
                        Mathf.Abs(Vector3.Distance(
                            rightHand.position, handMusketRoot.position) -
                            rightHandGripDistance));
                    postGrabRightHandPositions.Add(rightHand.position);
                    postGrabMusketLocalRotations.Add(handMusketRoot.localRotation);
                    var motion = Vector3.Distance(grabCenter, MeshWorldCenter(handMusket));
                    if (motion > maximumPostGrabMusketMotion)
                    {
                        maximumPostGrabMusketMotion = motion;
                        maximumPostGrabMusketMotionTime = time;
                    }
                    var currentMuzzleAngle = Vector3.Angle(
                        handMusket.transform.TransformDirection(localMuzzleAxis),
                        characterForward);
                    forwardPoseMuzzleAngle = Mathf.Min(
                        forwardPoseMuzzleAngle, currentMuzzleAngle);
                    if (currentMuzzleAngle <= 5f)
                        forwardLeftHandSurfaceDistance = Mathf.Min(
                            forwardLeftHandSurfaceDistance,
                            MinimumCurrentMeshSurfaceDistance(
                                leftHand.position, handMusket,
                                handMusketVertices, supportVertexIndices));
                    if (frame == ChangeToRifleLastFrame)
                    {
                        var musketRootDelta =
                            handMusketRoot.position - sourceFinalMusketRootPosition;
                        finalMusketRootVerticalLift = Vector3.Dot(
                            musketRootDelta, Vector3.up);
                        finalMusketRootHorizontalDrift = Vector3.ProjectOnPlane(
                            musketRootDelta, Vector3.up).magnitude;
                        var rightHandDelta =
                            rightHand.position - sourceFinalRightHandPosition;
                        finalRightHandVerticalLift = Vector3.Dot(
                            rightHandDelta, Vector3.up);
                        finalRightHandHorizontalDrift = Vector3.ProjectOnPlane(
                            rightHandDelta, Vector3.up).magnitude;
                        finalRightShoulderRotationOverride = Quaternion.Angle(
                            sourceFinalRightShoulderLocalRotation,
                            rightShoulder.localRotation);
                        finalRightArmRotationOverride = Quaternion.Angle(
                            sourceFinalRightArmLocalRotation,
                            rightArm.localRotation);
                        finalRightForeArmRotationOverride = Quaternion.Angle(
                            sourceFinalRightForeArmLocalRotation,
                            rightForeArm.localRotation);
                        var finalMuzzleDirection = handMusket.transform
                            .TransformDirection(localMuzzleAxis).normalized;
                        finalMuzzleForwardAngle = Vector3.Angle(
                            finalMuzzleDirection, characterForward);
                        var desiredFinalDown = Vector3.ProjectOnPlane(
                            Vector3.down, finalMuzzleDirection).normalized;
                        finalStockAndTriggerDownAngle = Vector3.Angle(
                            handMusket.transform.TransformDirection(
                                localStockAndTriggerDown).normalized,
                            desiredFinalDown);
                        finalLeftHandSurfaceDistance =
                            MinimumCurrentMeshSurfaceDistance(
                                leftHand.position, handMusket,
                                handMusketVertices, supportVertexIndices);
                    }
                }

                for (var frame = grab.Frame;
                     frame <= ChangeToRifleLastFrame;
                     frame++)
                {
                    SampleClip(
                        model.gameObject,
                        rifleSourceClip,
                        ChangeToRifleTimeForFrame(frame, rifleSourceClip));
                    postGrabSourceRightHandPositions.Add(rightHand.position);
                }
            }
            finally
            {
                foreach (var snapshot in transformSnapshots)
                    snapshot.Restore();
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Restore();
                StopSampling();
            }

            if (postGrabSourceRightHandPositions.Count !=
                postGrabRightHandPositions.Count)
                throw new InvalidOperationException(
                    "The source/runtime post-grab right-hand sample counts differ.");
            var runtimeCumulativeMotion =
                new float[postGrabSourceRightHandPositions.Count];
            for (var index = 1;
                 index < postGrabSourceRightHandPositions.Count;
                 index++)
                runtimeCumulativeMotion[index] = runtimeCumulativeMotion[index - 1] +
                    Vector3.Distance(
                        postGrabSourceRightHandPositions[index - 1],
                        postGrabSourceRightHandPositions[index]);
            var runtimeTotalMotion = runtimeCumulativeMotion[runtimeCumulativeMotion.Length - 1];
            if (runtimeTotalMotion < 0.1f)
                throw new InvalidOperationException(
                    "The runtime right hand has insufficient motion for arm-driven aiming.");
            var runtimeGrabLocalRotation = postGrabMusketLocalRotations[0];
            var runtimeFinalLocalRotation =
                postGrabMusketLocalRotations[postGrabMusketLocalRotations.Count - 1];
            for (var index = 0; index < postGrabMusketLocalRotations.Count; index++)
            {
                var progress = Mathf.Clamp01(
                    runtimeCumulativeMotion[index] / runtimeTotalMotion);
                progress = progress * progress * (3f - 2f * progress);
                var expectedLocalRotation = Quaternion.Slerp(
                    runtimeGrabLocalRotation, runtimeFinalLocalRotation, progress);
                maximumArmDrivenLocalInterpolationError = Mathf.Max(
                    maximumArmDrivenLocalInterpolationError,
                    Quaternion.Angle(
                        expectedLocalRotation,
                        postGrabMusketLocalRotations[index]));
            }

            if (maximumSheathEndToHoldError > StaticHoldTolerance ||
                maximumHoldDrift > StaticHoldTolerance ||
                maximumHoldToBridgeStartError > StaticHoldTolerance ||
                maximumBridgeEndToRifleStartError > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The sheath/hold/bridge/rifle body sequence is not pose-continuous.");
            if (bridgeRightHandMotion < 0.02f)
                throw new InvalidOperationException(
                    "The bridge does not visibly move the right hand toward the rifle motion.");
            if (grabContinuityError > 0.0001f)
                throw new InvalidOperationException(
                    "The back-to-hand musket switch is not transform-continuous: " +
                    Num(grabContinuityError) + ".");
            if (handMusketFollowError > 0.0001f)
                throw new InvalidOperationException(
                    "The post-grab musket position or scale drifts from the right hand.");
            if (maximumRightGripPivotError > TransformTolerance ||
                maximumRightHandGripDistanceDrift > TransformTolerance)
                throw new InvalidOperationException(
                    "The post-grab musket does not rotate around its right-hand surface pivot: " +
                    "PivotError=" + Num(maximumRightGripPivotError) +
                    ", HandDistanceDrift=" + Num(maximumRightHandGripDistanceDrift) + ".");
            if (maximumHandMusketRotationChange < 10f)
                throw new InvalidOperationException(
                    "The post-grab musket angle does not respond to the right-arm motion.");
            if (maximumArmDrivenLocalInterpolationError > 0.1f)
                throw new InvalidOperationException(
                    "The musket local rotation cancels or diverges from the approved " +
                    "right-hand-driven grip interpolation: " +
                    Num(maximumArmDrivenLocalInterpolationError) + " degrees.");
            if (maximumPostGrabMusketMotion < 0.1f)
                throw new InvalidOperationException(
                    "The supplied Mixamo right arm does not carry the hand musket forward.");
            if (Mathf.Abs(finalMusketRootVerticalLift - FinalAimArmLift) > 0.001f ||
                finalMusketRootHorizontalDrift > 0.001f)
                throw new InvalidOperationException(
                    "The final musket pivot was not raised exactly 0.15m by the arms: " +
                    "Vertical=" + Num(finalMusketRootVerticalLift) +
                    ", Horizontal=" + Num(finalMusketRootHorizontalDrift) + ".");
            if (finalRightShoulderRotationOverride < 0.1f &&
                finalRightArmRotationOverride < 0.5f &&
                finalRightForeArmRotationOverride < 0.5f)
                throw new InvalidOperationException(
                    "The final 0.15m weapon lift was not produced by right-arm rotation.");
            if (holdWaistSwordStaticReferenceMatrixError > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The hip-attached waist sword does not preserve the approved sheath-end placement: " +
                    Num(holdWaistSwordStaticReferenceMatrixError) + ".");
            if (maximumWaistSwordHipLocalMatrixDrift > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The waist sword deforms or drifts relative to mixamorig:Hips: " +
                    Num(maximumWaistSwordHipLocalMatrixDrift) + ".");
            if (maximumWaistSwordBodyFollowPositionChange < 0.005f &&
                maximumWaistSwordBodyFollowRotationChange < 0.5f)
                throw new InvalidOperationException(
                    "The waist sword does not measurably follow the animated body: " +
                    "Position=" + Num(maximumWaistSwordBodyFollowPositionChange) +
                    ", Rotation=" + Num(maximumWaistSwordBodyFollowRotationChange) + ".");
            if (finalMuzzleForwardAngle > 1f)
                throw new InvalidOperationException(
                    "The final musket muzzle does not face character-forward: " +
                    Num(finalMuzzleForwardAngle) + " degrees.");
            if (forwardPoseMuzzleAngle > 5f)
                throw new InvalidOperationException(
                    "The musket muzzle is not forward while the weapon is extended: " +
                    Num(forwardPoseMuzzleAngle) + " degrees.");
            if (finalStockAndTriggerDownAngle > 1f)
                throw new InvalidOperationException(
                    "The final broad stock side and trigger axis do not face down: " +
                    Num(finalStockAndTriggerDownAngle) + " degrees.");
            var maximumSupportDistance = grab.Distance +
                SupportHandSurfaceDistanceTolerance * 0.5f;
            if (forwardLeftHandSurfaceDistance > maximumSupportDistance ||
                finalLeftHandSurfaceDistance > maximumSupportDistance)
                throw new InvalidOperationException(
                    "The supplied left hand does not support the aimed musket surface: " +
                    "MinimumAimed=" + Num(forwardLeftHandSurfaceDistance) +
                    ", Final=" + Num(finalLeftHandSurfaceDistance) +
                    ", Maximum=" + Num(maximumSupportDistance) + ".");

            return new RifleSequenceMetrics(
                grab.Frame,
                grab.Time,
                grab.Distance,
                grab.StartDistance,
                sheathClip.length,
                holdClip.length,
                bridgeClip.length,
                rifleClip.length,
                sheathClip.length + holdClip.length + bridgeClip.length + rifleClip.length,
                bridgeSourceBindings.Length,
                bridgeRuntimeBindings.Length,
                sourceBindings.Length,
                runtimeBindings.Length,
                maximumSheathEndToHoldError,
                maximumHoldDrift,
                maximumHoldToBridgeStartError,
                maximumBridgeEndToRifleStartError,
                bridgeRightHandMotion,
                grabContinuityError,
                handMusketFollowError,
                maximumHandMusketRotationChange,
                maximumRightGripPivotError,
                maximumRightHandGripDistanceDrift,
                forwardLeftHandSurfaceDistance,
                finalLeftHandSurfaceDistance,
                forwardPoseMuzzleAngle,
                finalMuzzleForwardAngle,
                finalStockAndTriggerDownAngle,
                maximumArmDrivenLocalInterpolationError,
                maximumPostGrabMusketMotion,
                maximumPostGrabMusketMotionTime,
                holdWaistSwordStaticReferenceMatrixError,
                maximumWaistSwordHipLocalMatrixDrift,
                maximumWaistSwordBodyFollowPositionChange,
                maximumWaistSwordBodyFollowRotationChange,
                finalMusketRootVerticalLift,
                finalMusketRootHorizontalDrift,
                finalRightHandVerticalLift,
                finalRightHandHorizontalDrift,
                finalRightShoulderRotationOverride,
                finalRightArmRotationOverride,
                finalRightForeArmRotationOverride,
                musketRootPositionCurveCount);
        }

        private static void AccumulateWaistSwordBodyFollow(
            Transform model,
            Transform waistSwordRoot,
            Matrix4x4 referenceModelRelative,
            Matrix4x4 referenceHipLocal,
            ref float maximumPositionChange,
            ref float maximumRotationChange,
            ref float maximumHipLocalDrift)
        {
            var currentModelRelative =
                model.worldToLocalMatrix * waistSwordRoot.localToWorldMatrix;
            DecomposeMatrix(
                referenceModelRelative,
                out var referencePosition,
                out var referenceRotation,
                out _);
            DecomposeMatrix(
                currentModelRelative,
                out var currentPosition,
                out var currentRotation,
                out _);
            maximumPositionChange = Mathf.Max(
                maximumPositionChange,
                Vector3.Distance(referencePosition, currentPosition));
            maximumRotationChange = Mathf.Max(
                maximumRotationChange,
                Quaternion.Angle(referenceRotation, currentRotation));
            maximumHipLocalDrift = Mathf.Max(
                maximumHipLocalDrift,
                MatrixError(referenceHipLocal, LocalMatrix(waistSwordRoot)));
        }

        private static float MinimumCurrentMeshSurfaceDistance(
            Vector3 point,
            MeshRenderer renderer,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> vertexIndices)
        {
            var minimum = float.PositiveInfinity;
            foreach (var index in vertexIndices)
                minimum = Mathf.Min(minimum, Vector3.Distance(
                    point, renderer.transform.TransformPoint(vertices[index])));
            return minimum;
        }

        private static Vector3 DetermineCharacterForward(Transform model)
        {
            // Slot 6 is rotated 180 degrees around world Y in CargoRunMvp, so the
            // approved model root's transformed +Z axis is the visible facial front.
            var forward = Vector3.ProjectOnPlane(model.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    "The approved model root does not establish a horizontal visual forward.");
            return forward.normalized;
        }

        private static void RequireSameCurve(
            AnimationCurve expected, AnimationCurve actual, EditorCurveBinding binding)
        {
            if (expected.length != actual.length)
                throw new InvalidOperationException(
                    "A copied Mixamo curve key count differs: " +
                    binding.path + "/" + binding.propertyName + ".");
            for (var index = 0; index < expected.length; index++)
            {
                var left = expected.keys[index];
                var right = actual.keys[index];
                if (Mathf.Abs(left.time - right.time) > 0.000001f ||
                    Mathf.Abs(left.value - right.value) > 0.000001f ||
                    Mathf.Abs(left.inTangent - right.inTangent) > 0.00001f ||
                    Mathf.Abs(left.outTangent - right.outTangent) > 0.00001f)
                    throw new InvalidOperationException(
                        "A copied Mixamo curve value differs: " +
                        binding.path + "/" + binding.propertyName + ".");
            }
        }

        private static bool IsAimArmOverrideBinding(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform) &&
                   binding.propertyName.StartsWith(
                       "m_LocalRotation.", StringComparison.Ordinal) &&
                   (binding.path.EndsWith(
                        "mixamorig:LeftArm", StringComparison.Ordinal) ||
                    binding.path.EndsWith(
                        "mixamorig:LeftForeArm", StringComparison.Ordinal) ||
                    binding.path.EndsWith(
                        "mixamorig:RightShoulder", StringComparison.Ordinal) ||
                    binding.path.EndsWith(
                        "mixamorig:RightArm", StringComparison.Ordinal) ||
                    binding.path.EndsWith(
                        "mixamorig:RightForeArm", StringComparison.Ordinal) ||
                    binding.path.EndsWith(
                        "mixamorig:RightHand", StringComparison.Ordinal));
        }

        private static void RequireRifleVisibility(
            MeshRenderer handSword,
            MeshRenderer waistSword,
            MeshRenderer backMusket,
            MeshRenderer handMusket,
            bool backExpected,
            bool handExpected,
            string label)
        {
            if (handSword.enabled || !waistSword.enabled ||
                backMusket.enabled != backExpected || handMusket.enabled != handExpected)
                throw new InvalidOperationException(
                    "The slot-6 weapon visibility differs at " + label + ".");
        }

        private static Vector3 MeshWorldCenter(MeshRenderer renderer)
        {
            var vertices = SharedMesh(renderer).vertices;
            return vertices.Aggregate(Vector3.zero,
                (sum, vertex) => sum + renderer.transform.TransformPoint(vertex)) / vertices.Length;
        }

        private static void ConfigureImporter()
        {
            AssetDatabase.ImportAsset(
                DerivedFbxPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(DerivedFbxPath) as ModelImporter ??
                throw new InvalidOperationException("The sheath-sword ModelImporter is missing.");
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
                throw new InvalidOperationException("The sheath-sword FBX must expose exactly one Mixamo take.");
            if (clips[0].takeName.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The sole sheath-sword take is not Mixamo: " + clips[0].takeName + ".");
            clips[0].name = ImportedClipName;
            clips[0].firstFrame = FirstFrame;
            clips[0].lastFrame = LastFrame;
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = false;
            clips[0].lockRootPositionXZ = false;
            clips[0].lockRootHeightY = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireImportedClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(DerivedFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 || clips[0].name != ImportedClipName)
                throw new InvalidOperationException("The imported sheath-sword Mixamo clip differs.");
            var settings = AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime)
                throw new InvalidOperationException("The imported sheath-sword Mixamo clip is not looping.");
            return clips[0];
        }

        private static AnimationClip CreateOrUpdateStaticHoldClip(
            AnimationClip source,
            Transform staticModel,
            Transform model)
        {
            var hold = AssetDatabase.LoadAssetAtPath<AnimationClip>(StaticHoldClipPath);
            if (hold == null)
            {
                hold = new AnimationClip { name = StaticHoldClipName };
                AssetDatabase.CreateAsset(hold, StaticHoldClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(hold))
                AnimationUtility.SetEditorCurve(hold, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(hold))
                AnimationUtility.SetObjectReferenceCurve(hold, binding, null);

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                    throw new InvalidOperationException(
                        "A source Mixamo curve is missing while creating the static hold.");
                var value = sourceCurve.Evaluate(source.length);
                AnimationUtility.SetEditorCurve(
                    hold, binding, AnimationCurve.Constant(0f, StaticHoldDuration, value));
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var sourceKeys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                if (sourceKeys == null || sourceKeys.Length == 0)
                    continue;
                var value = sourceKeys[sourceKeys.Length - 1].value;
                AnimationUtility.SetObjectReferenceCurve(hold, binding, new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = value },
                    new ObjectReferenceKeyframe { time = StaticHoldDuration, value = value }
                });
            }

            var handSword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var handSwordRoot = handSword.transform.parent;
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            if (handSwordRoot == null || handSwordRoot.parent != rightHand)
                throw new InvalidOperationException("The slot-6 sword mount differs before static hold creation.");
            var hips = RequireDescendant(model, "mixamorig:Hips");
            if (waistSword.transform.parent == null ||
                waistSword.transform.parent.parent != hips ||
                waistSword.transform.parent.name != WaistSwordRootName)
                throw new InvalidOperationException("The slot-6 left-waist sword mount differs.");
            SetConstantRendererEnabledCurve(
                hold, AnimationUtility.CalculateTransformPath(handSword.transform, model), false);
            SetConstantRendererEnabledCurve(
                hold, AnimationUtility.CalculateTransformPath(waistSword.transform, model), true);

            hold.name = StaticHoldClipName;
            hold.frameRate = source.frameRate;
            hold.wrapMode = WrapMode.ClampForever;
            AnimationUtility.SetAnimationEvents(hold, Array.Empty<AnimationEvent>());
            var settings = AnimationUtility.GetAnimationClipSettings(hold);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(hold, settings);
            EditorUtility.SetDirty(hold);
            AssetDatabase.SaveAssets();
            if (Mathf.Abs(hold.length - StaticHoldDuration) > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The generated slot-6 static hold is not exactly 0.5 seconds.");
            return hold;
        }

        private static void SetConstantRendererEnabledCurve(
            AnimationClip clip, string path, bool enabled)
        {
            SetConstantRendererEnabledCurve(
                clip, path, enabled, StaticHoldDuration);
        }

        private static void SetConstantRendererEnabledCurve(
            AnimationClip clip, string path, bool enabled, float duration)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled");
            AnimationUtility.SetEditorCurve(
                clip, binding,
                AnimationCurve.Constant(0f, duration, enabled ? 1f : 0f));
        }

        private static void SetConstantTransformCurves(
            AnimationClip clip,
            string path,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            SetConstantCurve(clip, path, "m_LocalPosition.x", position.x);
            SetConstantCurve(clip, path, "m_LocalPosition.y", position.y);
            SetConstantCurve(clip, path, "m_LocalPosition.z", position.z);
            SetConstantCurve(clip, path, "m_LocalRotation.x", rotation.x);
            SetConstantCurve(clip, path, "m_LocalRotation.y", rotation.y);
            SetConstantCurve(clip, path, "m_LocalRotation.z", rotation.z);
            SetConstantCurve(clip, path, "m_LocalRotation.w", rotation.w);
            SetConstantCurve(clip, path, "m_LocalScale.x", scale.x);
            SetConstantCurve(clip, path, "m_LocalScale.y", scale.y);
            SetConstantCurve(clip, path, "m_LocalScale.z", scale.z);
        }

        private static void SetConstantCurve(
            AnimationClip clip, string path, string propertyName, float value)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            AnimationUtility.SetEditorCurve(
                clip, binding, AnimationCurve.Constant(0f, StaticHoldDuration, value));
        }

        private static void DecomposeMatrix(
            Matrix4x4 matrix,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale)
        {
            position = matrix.GetColumn(3);
            var right = (Vector3)matrix.GetColumn(0);
            var up = (Vector3)matrix.GetColumn(1);
            var forward = (Vector3)matrix.GetColumn(2);
            scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
            if (scale.x <= 0.000001f || scale.y <= 0.000001f || scale.z <= 0.000001f)
                throw new InvalidOperationException("The static sword target matrix has a zero scale axis.");
            if (Vector3.Dot(Vector3.Cross(right, up), forward) < 0f)
                scale.x = -scale.x;
            rotation = Quaternion.LookRotation(forward / scale.z, up / scale.y);
        }

        private static AnimationClip RequireStaticHoldClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(StaticHoldClipPath) ??
                throw new InvalidOperationException("The slot-6 static-hold clip is missing.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (clip.name != StaticHoldClipName || settings.loopTime ||
                Mathf.Abs(clip.length - StaticHoldDuration) > StaticHoldTolerance)
                throw new InvalidOperationException("The slot-6 static-hold clip configuration differs.");
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

        private static AnimatorController CreateOrUpdateStaticHoldController(
            AnimationClip sourceClip, AnimationClip holdClip)
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
            var sourceState = stateMachine.AddState(StateName);
            sourceState.motion = sourceClip;
            sourceState.speed = 1f;
            sourceState.writeDefaultValues = true;
            var holdState = stateMachine.AddState(StaticHoldStateName);
            holdState.motion = holdClip;
            holdState.speed = 1f;
            holdState.writeDefaultValues = true;
            ConfigureExactExitTransition(sourceState.AddTransition(holdState));
            ConfigureExactExitTransition(holdState.AddTransition(sourceState));
            stateMachine.defaultState = sourceState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureExactExitTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = true;
            transition.canTransitionToSelf = false;
        }

        private static AnimatorController RequireController()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("The slot-6 AnimatorController is missing.");
        }

        private static Animator ConfigureAnimator(
            Transform model, RuntimeAnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException("The slot-6 model must contain exactly one Animator.");
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
            var approved = StaticMaterialMap(staticModel);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 4)
                throw new InvalidOperationException(
                    "The derived sheath-sword FBX must contain body, crescent, eyes, and rigid musket renderers.");
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
                {
                    if (material == null)
                        throw new InvalidOperationException("A sheath-sword material slot is null.");
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

        private static void CloneApprovedSword(
            Transform staticModel, Transform drawModel, Transform targetModel)
        {
            var staticRenderer = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var sourceRenderer = RequireRenderer<MeshRenderer>(drawModel, SwordRendererName);
            var sourceRoot = sourceRenderer.transform.parent;
            if (sourceRoot == null || sourceRoot.name != SwordRootName ||
                sourceRoot.parent == null || sourceRoot.parent.name != "mixamorig:RightHand")
                throw new InvalidOperationException("The approved draw-slot right-hand sword mount differs.");
            var rightHand = RequireDescendant(targetModel, "mixamorig:RightHand");
            var root = new GameObject(SwordRootName);
            root.transform.SetParent(rightHand, false);
            CopyLocalTransform(sourceRoot, root.transform);
            var rendererObject = new GameObject(SwordRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            CopyLocalTransform(sourceRenderer.transform, rendererObject.transform);
            var filter = rendererObject.AddComponent<MeshFilter>();
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = staticRenderer.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterials = staticRenderer.sharedMaterials;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
        }

        private static MeshRenderer CreateOrUpdateWaistSword(
            Transform staticModel, Transform targetModel, AnimationClip sheathClip)
        {
            var existing = targetModel.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == WaistSwordRootName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var staticRenderer = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var staticFilter = staticRenderer.GetComponent<MeshFilter>() ??
                throw new InvalidOperationException("The exact static Ispant sword mesh is missing.");
            var desiredRelative = StaticSwordTargetMatrix(
                staticModel, targetModel, staticRenderer);
            var desiredWorld = targetModel.localToWorldMatrix * desiredRelative;
            var hips = RequireDescendant(targetModel, "mixamorig:Hips");
            var snapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            Matrix4x4 hipsWorldAtSheathEnd;
            try
            {
                SampleClip(targetModel.gameObject, sheathClip, sheathClip.length);
                hipsWorldAtSheathEnd = hips.localToWorldMatrix;
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }
            var desiredHipLocal = hipsWorldAtSheathEnd.inverse * desiredWorld;
            DecomposeMatrix(
                desiredHipLocal, out var position, out var rotation, out var scale);

            var root = new GameObject(WaistSwordRootName);
            root.transform.SetParent(hips, false);
            root.transform.SetLocalPositionAndRotation(position, rotation);
            root.transform.localScale = scale;

            var rendererObject = new GameObject(WaistSwordRendererName);
            rendererObject.transform.SetParent(root.transform, false);
            rendererObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            rendererObject.transform.localScale = Vector3.one;
            var filter = rendererObject.AddComponent<MeshFilter>();
            var renderer = rendererObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = staticFilter.sharedMesh;
            renderer.sharedMaterials = staticRenderer.sharedMaterials;
            renderer.enabled = false;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(rendererObject);
            return renderer;
        }

        private static Matrix4x4 StaticSwordTargetMatrix(
            Transform staticModel,
            Transform targetModel,
            MeshRenderer staticSword)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BODY_NAME);
            var targetBody = RequireRenderer<SkinnedMeshRenderer>(targetModel, BODY_NAME);
            var staticBounds = BindWorldBounds(staticBody);
            var targetBounds = BindWorldBounds(targetBody);
            if (staticBounds.size.y <= 0.0001f || targetBounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The Ispant body bounds cannot align the waist sword.");
            var bodyScale = targetBounds.size.y / staticBounds.size.y;
            var bodyRotation = targetModel.rotation * Quaternion.Inverse(staticModel.rotation);
            var bodyAlignment =
                Matrix4x4.TRS(targetBounds.center, bodyRotation, Vector3.one * bodyScale) *
                Matrix4x4.Translate(-staticBounds.center);
            return targetModel.worldToLocalMatrix *
                bodyAlignment * staticSword.transform.localToWorldMatrix;
        }

        private static void CopyLocalTransform(Transform source, Transform target)
        {
            target.localPosition = source.localPosition;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void FitToStaticReference(Transform model, Transform staticModel)
        {
            var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BODY_NAME);
            var body = RequireRenderer<SkinnedMeshRenderer>(model, BODY_NAME);
            var staticBounds = BindWorldBounds(staticBody);
            var bounds = BindWorldBounds(body);
            if (bounds.size.y <= 0.0001f)
                throw new InvalidOperationException("The slot-6 bind bounds are invalid.");
            var scale = staticBounds.size.y / bounds.size.y;
            if (scale < 0.5f || scale > 2f)
                throw new InvalidOperationException("The slot-6 size ratio is unsafe: " + Num(scale) + ".");
            model.localScale *= scale;
            bounds = BindWorldBounds(body);
            model.position += Vector3.up * (staticBounds.min.y - bounds.min.y);
            EditorUtility.SetDirty(model);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        private const string BODY_NAME = "Ispant_Armed_Body";

        private static Metrics InspectModel(
            Transform model,
            Transform staticModel,
            Transform drawModel,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (!animator.enabled || animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("The slot-6 Animator configuration differs.");
            if (controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name != StateName ||
                controller.layers[0].stateMachine.defaultState.motion != clip)
                throw new InvalidOperationException("The slot-6 default Mixamo state differs.");
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The slot-6 Mixamo state is not looping.");

            var body = RequireRenderer<SkinnedMeshRenderer>(model, BODY_NAME);
            var crescent = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Crescent_Ornament");
            var eyes = RequireRenderer<SkinnedMeshRenderer>(model, "Ispant_Reference_Eye_Slits");
            var musket = RequireRenderer<MeshRenderer>(model, MusketName);
            var sword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 5 &&
                (renderers.Length != 6 ||
                 renderers.Count(item => item.name == WaistSwordRendererName) != 1) &&
                (renderers.Length != 7 ||
                 renderers.Count(item => item.name == WaistSwordRendererName) != 1 ||
                 renderers.Count(item => item.name == HandMusketRendererName) != 1))
                throw new InvalidOperationException("The slot-6 renderer set differs.");
            if (body.bones.Length != ExpectedBones || crescent.bones.Length != ExpectedBones ||
                eyes.bones.Length != ExpectedBones)
                throw new InvalidOperationException("The slot-6 Mixamo bone count differs.");
            if (TriangleCount(SharedMesh(body)) != ExpectedBodyTriangles ||
                TriangleCount(SharedMesh(musket)) != ExpectedMusketTriangles ||
                TriangleCount(SharedMesh(crescent)) != ExpectedCrescentTriangles ||
                TriangleCount(SharedMesh(eyes)) != ExpectedEyeTriangles ||
                TriangleCount(SharedMesh(sword)) != ExpectedSwordTriangles)
                throw new InvalidOperationException("The slot-6 synchronized mesh topology differs.");
            if (musket.GetComponent<SkinnedMeshRenderer>() != null ||
                sword.GetComponent<SkinnedMeshRenderer>() != null)
                throw new InvalidOperationException("The slot-6 weapons must be rigid MeshRenderers.");

            var spine2 = RequireDescendant(model, "mixamorig:Spine2");
            var rightArm = RequireDescendant(model, "mixamorig:RightArm");
            var rightHand = RequireDescendant(model, "mixamorig:RightHand");
            if (musket.transform.parent != spine2)
                throw new InvalidOperationException("The rigid musket is not parented to mixamorig:Spine2.");
            if (sword.transform.parent == null || sword.transform.parent.parent != rightHand ||
                sword.transform.parent.name != SwordRootName)
                throw new InvalidOperationException("The approved sword is not mounted under the right hand.");
            RequireExactStaticMaterials(staticModel, model);
            RequireExactSword(staticModel, drawModel, sword);

            var staticBounds = BindWorldBounds(
                RequireRenderer<SkinnedMeshRenderer>(staticModel, BODY_NAME));
            var modelBounds = BindWorldBounds(body);
            var heightRatio = modelBounds.size.y / staticBounds.size.y;
            var groundDifference = Mathf.Abs(modelBounds.min.y - staticBounds.min.y);
            if (Mathf.Abs(heightRatio - 1f) > SizeRatioTolerance || groundDifference > 0.005f)
                throw new InvalidOperationException(
                    "The slot-6 model does not match the static size and ground level.");

            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var initialMusketLocal = LocalMatrix(musket.transform);
            var swordRoot = sword.transform.parent;
            var initialSwordLocal = LocalMatrix(swordRoot);
            var initialHandPosition = rightHand.position;
            var initialRightArmRotation = rightArm.rotation;
            var initialSwordRotation = swordRoot.rotation;
            var initialSwordPosition = sword.transform.position;
            var initialMusketPosition = musket.transform.position;
            var maximumMusketAttachmentError = 0f;
            var maximumSwordAttachmentError = 0f;
            var maximumRightHandMotion = 0f;
            var maximumRightArmAngularMotion = 0f;
            var maximumSwordAngularMotion = 0f;
            var maximumSwordFollowMotion = 0f;
            var maximumMusketFollowMotion = 0f;
            var maximumNearestSwordVertexToHand = 0f;
            Vector3? firstHandPosition = null;
            Vector3? lastHandPosition = null;
            var hips = RequireDescendant(model, "mixamorig:Hips");
            var horizontalHips = new List<Vector2>();
            var verticalHips = new List<float>();
            try
            {
                var sampleCount = LastFrame - FirstFrame + 1;
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = clip.length * index / (sampleCount - 1f);
                    SampleClip(model.gameObject, clip, time);
                    var handPosition = rightHand.position;
                    if (index == 0)
                        firstHandPosition = handPosition;
                    if (index == sampleCount - 1)
                        lastHandPosition = handPosition;
                    maximumMusketAttachmentError = Mathf.Max(
                        maximumMusketAttachmentError,
                        MatrixError(initialMusketLocal, LocalMatrix(musket.transform)));
                    maximumSwordAttachmentError = Mathf.Max(
                        maximumSwordAttachmentError,
                        MatrixError(initialSwordLocal, LocalMatrix(swordRoot)));
                    maximumRightHandMotion = Mathf.Max(
                        maximumRightHandMotion, Vector3.Distance(initialHandPosition, handPosition));
                    maximumRightArmAngularMotion = Mathf.Max(
                        maximumRightArmAngularMotion,
                        Quaternion.Angle(initialRightArmRotation, rightArm.rotation));
                    maximumSwordAngularMotion = Mathf.Max(
                        maximumSwordAngularMotion,
                        Quaternion.Angle(initialSwordRotation, swordRoot.rotation));
                    maximumSwordFollowMotion = Mathf.Max(
                        maximumSwordFollowMotion,
                        Vector3.Distance(initialSwordPosition, sword.transform.position));
                    maximumMusketFollowMotion = Mathf.Max(
                        maximumMusketFollowMotion,
                        Vector3.Distance(initialMusketPosition, musket.transform.position));
                    var nearest = SharedMesh(sword).vertices.Min(
                        vertex => Vector3.Distance(
                            sword.transform.TransformPoint(vertex), handPosition));
                    maximumNearestSwordVertexToHand = Mathf.Max(
                        maximumNearestSwordVertexToHand, nearest);
                    var hipsLocal = model.InverseTransformPoint(hips.position);
                    horizontalHips.Add(new Vector2(hipsLocal.x, hipsLocal.z));
                    verticalHips.Add(hipsLocal.y);
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                StopSampling();
            }

            if (maximumMusketAttachmentError > AttachmentTolerance ||
                maximumSwordAttachmentError > AttachmentTolerance)
                throw new InvalidOperationException("A slot-6 rigid weapon changed relative to its follow bone.");
            if (maximumRightHandMotion < MinimumRightHandMotion ||
                maximumRightArmAngularMotion < MinimumRightArmMotion ||
                maximumSwordAngularMotion < MinimumSwordAngularMotion ||
                maximumSwordFollowMotion < MinimumRightHandMotion)
                throw new InvalidOperationException(
                    "The slot-6 right arm and sword do not contain the supplied animated motion.");
            if (maximumNearestSwordVertexToHand > MaximumSwordVertexToHandDistance)
                throw new InvalidOperationException(
                    "The approved sword handle is too far from the right hand: " +
                    Num(maximumNearestSwordVertexToHand) + ".");
            var horizontalRange =
                horizontalHips.Max(value => value.x) - horizontalHips.Min(value => value.x) +
                horizontalHips.Max(value => value.y) - horizontalHips.Min(value => value.y);
            var verticalRange = verticalHips.Max() - verticalHips.Min();
            var loopHandError = Vector3.Distance(
                firstHandPosition ?? Vector3.zero, lastHandPosition ?? Vector3.one);
            return new Metrics(
                clip.length,
                clip.frameRate,
                horizontalRange,
                verticalRange,
                maximumMusketAttachmentError,
                maximumSwordAttachmentError,
                maximumMusketFollowMotion,
                maximumSwordFollowMotion,
                maximumRightHandMotion,
                maximumRightArmAngularMotion,
                maximumSwordAngularMotion,
                loopHandError,
                maximumNearestSwordVertexToHand,
                staticBounds.size.y,
                modelBounds.size.y,
                groundDifference,
                MeasureSwordDimensions(sword).BladeLength);
        }

        private static StaticHoldMetrics InspectStaticHold(
            Transform model,
            Transform staticModel,
            Transform drawModel,
            Animator animator,
            AnimationClip sourceClip,
            AnimationClip holdClip,
            AnimatorController controller)
        {
            InspectModel(
                model, staticModel, drawModel, animator, sourceClip, controller);
            if (Mathf.Abs(holdClip.length - StaticHoldDuration) > StaticHoldTolerance ||
                AnimationUtility.GetAnimationClipSettings(holdClip).loopTime)
                throw new InvalidOperationException("The slot-6 hold is not an exact non-looping 0.5-second clip.");

            var states = controller.layers[0].stateMachine.states.Select(item => item.state).ToArray();
            if (states.Length != 2)
                throw new InvalidOperationException("The slot-6 controller must contain exactly two states.");
            var sourceState = states.SingleOrDefault(item => item.name == StateName) ??
                throw new InvalidOperationException("The slot-6 Mixamo state is missing.");
            var holdState = states.SingleOrDefault(item => item.name == StaticHoldStateName) ??
                throw new InvalidOperationException("The slot-6 static-hold state is missing.");
            if (sourceState.motion != sourceClip || holdState.motion != holdClip ||
                controller.layers[0].stateMachine.defaultState != sourceState)
                throw new InvalidOperationException("The slot-6 source/hold state motions differ.");
            RequireExactExitTransition(sourceState, holdState);
            RequireExactExitTransition(holdState, sourceState);

            foreach (var binding in AnimationUtility.GetCurveBindings(holdClip))
            {
                var curve = AnimationUtility.GetEditorCurve(holdClip, binding) ??
                    throw new InvalidOperationException("A slot-6 hold curve is missing.");
                if (Mathf.Abs(curve.Evaluate(0f) - curve.Evaluate(StaticHoldDuration)) >
                    StaticHoldTolerance)
                    throw new InvalidOperationException(
                        "A slot-6 hold curve changes during the 0.5-second pause: " +
                        binding.path + "/" + binding.propertyName + ".");
            }

            var handSword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
            var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var handSwordRoot = handSword.transform.parent;
            var waistSwordRoot = waistSword.transform.parent;
            var hips = RequireDescendant(model, "mixamorig:Hips");
            if (!handSword.enabled || waistSword.enabled)
                throw new InvalidOperationException(
                    "The slot-6 default sword visibility must be hand=true and left-waist=false.");
            if (waistSwordRoot == null || waistSwordRoot.parent != hips ||
                waistSwordRoot.name != WaistSwordRootName)
                throw new InvalidOperationException(
                    "The slot-6 left-waist sword is not rigid under mixamorig:Hips.");
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var bodyTransforms = transforms.Where(
                item => item != handSwordRoot && !item.IsChildOf(handSwordRoot) &&
                        item != waistSwordRoot && !item.IsChildOf(waistSwordRoot)).ToArray();
            var snapshots = transforms.Select(item => new TransformSnapshot(item)).ToArray();
            var rendererSnapshots = model.GetComponentsInChildren<Renderer>(true)
                .Select(item => new RendererSnapshot(item)).ToArray();
            var maximumSourceEndToHoldBodyError = 0f;
            var maximumHoldTransformDrift = 0f;
            var maximumHoldSwordMatrixDrift = 0f;
            var maximumSwordStaticReferenceMatrixError = 0f;
            var maximumSwordStaticPositionError = 0f;
            var maximumSwordStaticRotationError = 0f;
            var maximumSwordStaticScaleError = 0f;
            try
            {
                SampleClip(model.gameObject, sourceClip, sourceClip.length);
                var sourceEndBody = bodyTransforms.ToDictionary(
                    item => item, LocalMatrix);
                SampleClip(model.gameObject, holdClip, 0f);
                if (handSword.enabled || !waistSword.enabled)
                    throw new InvalidOperationException(
                        "The hold state did not switch from the right-hand sword to the left-waist sword.");
                foreach (var item in bodyTransforms)
                    maximumSourceEndToHoldBodyError = Mathf.Max(
                        maximumSourceEndToHoldBodyError,
                        MatrixError(sourceEndBody[item], LocalMatrix(item)));
                var holdTransformReference = transforms.ToDictionary(
                    item => item, LocalMatrix);
                var holdSwordReference =
                    model.worldToLocalMatrix * waistSword.transform.localToWorldMatrix;
                var expectedSwordRelative =
                    StaticSwordTargetMatrix(staticModel, model, staticSword);
                DecomposeMatrix(
                    expectedSwordRelative,
                    out var expectedSwordPosition,
                    out var expectedSwordRotation,
                    out var expectedSwordScale);
                foreach (var time in new[] { 0f, StaticHoldDuration * 0.5f, StaticHoldDuration })
                {
                    SampleClip(model.gameObject, holdClip, time);
                    foreach (var item in transforms)
                        maximumHoldTransformDrift = Mathf.Max(
                            maximumHoldTransformDrift,
                            MatrixError(holdTransformReference[item], LocalMatrix(item)));
                    var actualSwordRelative =
                        model.worldToLocalMatrix * waistSword.transform.localToWorldMatrix;
                    maximumHoldSwordMatrixDrift = Mathf.Max(
                        maximumHoldSwordMatrixDrift,
                        MatrixError(holdSwordReference, actualSwordRelative));
                    maximumSwordStaticReferenceMatrixError = Mathf.Max(
                        maximumSwordStaticReferenceMatrixError,
                        MatrixError(expectedSwordRelative, actualSwordRelative));
                    DecomposeMatrix(
                        actualSwordRelative,
                        out var actualSwordPosition,
                        out var actualSwordRotation,
                        out var actualSwordScale);
                    maximumSwordStaticPositionError = Mathf.Max(
                        maximumSwordStaticPositionError,
                        Vector3.Distance(expectedSwordPosition, actualSwordPosition));
                    maximumSwordStaticRotationError = Mathf.Max(
                        maximumSwordStaticRotationError,
                        Quaternion.Angle(expectedSwordRotation, actualSwordRotation));
                    maximumSwordStaticScaleError = Mathf.Max(
                        maximumSwordStaticScaleError,
                        Vector3.Distance(expectedSwordScale, actualSwordScale));
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
                foreach (var snapshot in rendererSnapshots)
                    snapshot.Restore();
                StopSampling();
            }

            if (maximumSourceEndToHoldBodyError > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The final Mixamo body pose changed at the start of the 0.5-second hold: " +
                    Num(maximumSourceEndToHoldBodyError) + ".");
            if (maximumHoldTransformDrift > StaticHoldTolerance ||
                maximumHoldSwordMatrixDrift > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The slot-6 pose or sword moved during the 0.5-second hold.");
            if (maximumSwordStaticReferenceMatrixError > StaticHoldTolerance ||
                maximumSwordStaticPositionError > StaticHoldTolerance ||
                maximumSwordStaticRotationError > StaticHoldTolerance ||
                maximumSwordStaticScaleError > StaticHoldTolerance)
                throw new InvalidOperationException(
                    "The held slot-6 sword does not match the exact static-model sword transform. " +
                    "Matrix=" + Num(maximumSwordStaticReferenceMatrixError) +
                    ", Position=" + Num(maximumSwordStaticPositionError) +
                    ", Rotation=" + Num(maximumSwordStaticRotationError) +
                    ", Scale=" + Num(maximumSwordStaticScaleError) + ".");
            var waistSwordBladeLength = MeasureSwordDimensions(waistSword).BladeLength;
            if (Mathf.Abs(waistSwordBladeLength - TargetWorldBladeLength) > SwordDimensionTolerance)
                throw new InvalidOperationException(
                    "The left-waist sword blade is not the approved 0.6m world length: " +
                    Num(waistSwordBladeLength) + "m.");

            return new StaticHoldMetrics(
                sourceClip.length,
                holdClip.length,
                sourceClip.length + holdClip.length,
                AnimationUtility.GetCurveBindings(sourceClip).Length,
                AnimationUtility.GetCurveBindings(holdClip).Length,
                maximumSourceEndToHoldBodyError,
                maximumHoldTransformDrift,
                maximumHoldSwordMatrixDrift,
                maximumSwordStaticReferenceMatrixError,
                maximumSwordStaticPositionError,
                maximumSwordStaticRotationError,
                maximumSwordStaticScaleError,
                waistSwordBladeLength);
        }

        private static void RequireExactExitTransition(
            AnimatorState source, AnimatorState destination)
        {
            if (source.transitions.Length != 1)
                throw new InvalidOperationException(
                    "A slot-6 sequence state must contain exactly one transition.");
            var transition = source.transitions[0];
            if (transition.destinationState != destination || !transition.hasExitTime ||
                Mathf.Abs(transition.exitTime - 1f) > StaticHoldTolerance ||
                !transition.hasFixedDuration ||
                Mathf.Abs(transition.duration) > StaticHoldTolerance ||
                transition.conditions.Length != 0)
                throw new InvalidOperationException(
                    "The slot-6 Mixamo/hold transition is not an exact unblended exit transition.");
        }

        private static void RequireExactSword(
            Transform staticModel, Transform drawModel, MeshRenderer target)
        {
            var staticSword = RequireRenderer<MeshRenderer>(staticModel, SwordRendererName);
            var drawSword = RequireRenderer<MeshRenderer>(drawModel, SwordRendererName);
            if (SharedMesh(target) != SharedMesh(staticSword))
                throw new InvalidOperationException("The slot-6 sword is not the exact static shared mesh.");
            if (!target.sharedMaterials.SequenceEqual(staticSword.sharedMaterials))
                throw new InvalidOperationException("The slot-6 sword materials differ from the static sword.");
            RequireLocalTransform(target.transform.parent, drawSword.transform.parent,
                "right-hand sword root mount");
            RequireLocalTransform(target.transform, drawSword.transform,
                "right-hand sword renderer correction");
            var staticDimensions = MeasureSwordDimensions(staticSword);
            var targetDimensions = MeasureSwordDimensions(target);
            if (Mathf.Abs(staticDimensions.BladeLength - TargetWorldBladeLength) > SwordDimensionTolerance ||
                Mathf.Abs(targetDimensions.BladeLength - staticDimensions.BladeLength) > SwordDimensionTolerance ||
                Mathf.Abs(targetDimensions.HandleSize - staticDimensions.HandleSize) > SwordDimensionTolerance)
                throw new InvalidOperationException(
                    "The slot-6 sword world dimensions differ from the exact 0.6m static sword.");
        }

        private static SwordDimensions MeasureSwordDimensions(MeshRenderer renderer)
        {
            var mesh = SharedMesh(renderer);
            var grip = ApprovedGripCenterLocal * (mesh.bounds.size.z / ExpectedSwordLength);
            var vertices = mesh.vertices;
            var maximumZ = vertices.Max(vertex => vertex.z);
            var tipVertices = vertices.Where(vertex => maximumZ - vertex.z <= 0.000005f).ToArray();
            var tip = tipVertices.Aggregate(Vector3.zero, (sum, vertex) => sum + vertex) /
                      tipVertices.Length;
            var bladeLength = Vector3.Distance(
                renderer.transform.TransformPoint(grip), renderer.transform.TransformPoint(tip));
            var handlePoints = mesh.GetTriangles(1).Distinct()
                .Select(index => renderer.transform.TransformPoint(vertices[index])).ToArray();
            var handleSize = 0f;
            for (var first = 0; first < handlePoints.Length; first++)
            for (var second = first + 1; second < handlePoints.Length; second++)
                handleSize = Mathf.Max(
                    handleSize, Vector3.Distance(handlePoints[first], handlePoints[second]));
            return new SwordDimensions(bladeLength, handleSize);
        }

        private static void RequireLocalTransform(Transform actual, Transform expected, string label)
        {
            if (Vector3.Distance(actual.localPosition, expected.localPosition) > TransformTolerance ||
                Quaternion.Angle(actual.localRotation, expected.localRotation) > TransformTolerance ||
                Vector3.Distance(actual.localScale, expected.localScale) > TransformTolerance)
                throw new InvalidOperationException("The copied " + label + " differs.");
        }

        private static void RequireExactStaticMaterials(Transform staticModel, Transform model)
        {
            var approved = StaticMaterialMap(staticModel);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null ||
                    !approved.TryGetValue(NormalizeMaterialName(material.name), out var exact) ||
                    material != exact)
                    throw new InvalidOperationException(
                        "A slot-6 material is not a direct static appearance reference.");
            }
        }

        private static Dictionary<string, Material> StaticMaterialMap(Transform staticModel)
        {
            return staticModel.GetComponentsInChildren<Renderer>(true)
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .GroupBy(material => NormalizeMaterialName(material.name), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private static string NormalizeMaterialName(string name)
        {
            var result = name.Replace(" (Instance)", string.Empty);
            var suffix = result.LastIndexOf('.');
            if (suffix >= 0 && result.Length - suffix == 4 &&
                int.TryParse(result.Substring(suffix + 1), out _))
                result = result.Substring(0, suffix);
            return result;
        }

        private static void CaptureReview(
            Transform staticModel, Transform model, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The slot-6 capture folder is invalid."));
            const int panelWidth = 512;
            const int panelHeight = 768;
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var strip = new Texture2D(panelWidth * 6, panelHeight, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("IspantSheathSwordReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            var oldActive = RenderTexture.active;
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = staticRenderers.Concat(modelRenderers)
                .Select(item => new RendererSnapshot(item)).ToArray();
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                foreach (var renderer in modelRenderers)
                    renderer.enabled = false;
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BODY_NAME);
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                FrameCamera(camera, BindWorldBounds(staticBody).center, referenceHeight, 1f);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                var body = RequireRenderer<SkinnedMeshRenderer>(model, BODY_NAME);
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
                camera.targetTexture = null;
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

        private static void CaptureSheathToRifleReview(
            Transform staticModel,
            Transform model,
            AnimationClip sheathClip,
            AnimationClip holdClip,
            AnimationClip bridgeClip,
            AnimationClip rifleClip,
            RifleSequenceMetrics metrics,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "The slot-6 sheath-to-rifle capture folder is invalid."));
            const int panelWidth = 512;
            const int panelHeight = 768;
            const int panelCount = 11;
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var strip = new Texture2D(panelWidth * panelCount, panelHeight, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("IspantSheathToRifleReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            var oldActive = RenderTexture.active;
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = staticRenderers.Concat(modelRenderers)
                .Select(item => new RendererSnapshot(item)).ToArray();
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                foreach (var renderer in modelRenderers)
                    renderer.enabled = false;
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BODY_NAME);
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                FrameCamera(camera, BindWorldBounds(staticBody).center, referenceHeight, 1f);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);

                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                var body = RequireRenderer<SkinnedMeshRenderer>(model, BODY_NAME);
                var samples = new[]
                {
                    new ClipSample(sheathClip, sheathClip.length),
                    new ClipSample(holdClip, holdClip.length * 0.5f),
                    new ClipSample(bridgeClip, bridgeClip.length * 0.25f),
                    new ClipSample(bridgeClip, bridgeClip.length * 0.5f),
                    new ClipSample(bridgeClip, bridgeClip.length * 0.75f),
                    new ClipSample(bridgeClip, bridgeClip.length),
                    new ClipSample(rifleClip,
                        Mathf.Max(0f, metrics.GrabTime - 0.5f / ChangeToRifleFrameRate)),
                    new ClipSample(rifleClip, metrics.GrabTime),
                    new ClipSample(rifleClip, metrics.MaximumPostGrabMusketMotionTime),
                    new ClipSample(rifleClip, rifleClip.length)
                };
                for (var index = 0; index < samples.Length; index++)
                {
                    SetSequenceDefaultVisibility(model);
                    SampleClip(model.gameObject, samples[index].Clip, samples[index].Time);
                    FrameCamera(camera, body.bounds.center, referenceHeight, 1f);
                    RenderPanel(
                        camera, panel, strip, target, index + 1, panelWidth, panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
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

        private static void CaptureStaticHoldReview(
            Transform staticModel,
            Transform model,
            AnimationClip sourceClip,
            AnimationClip holdClip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The slot-6 static-hold capture folder is invalid."));
            const int panelWidth = 512;
            const int panelHeight = 768;
            var target = new RenderTexture(panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var strip = new Texture2D(panelWidth * 6, panelHeight, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("IspantSheathSwordStaticHoldReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            var oldActive = RenderTexture.active;
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = staticRenderers.Concat(modelRenderers)
                .Select(item => new RendererSnapshot(item)).ToArray();
            var transformSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            try
            {
                foreach (var renderer in modelRenderers)
                    renderer.enabled = false;
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BODY_NAME);
                var referenceHeight = BindWorldBounds(staticBody).size.y;
                FrameCamera(camera, BindWorldBounds(staticBody).center, referenceHeight, 1f);
                RenderPanel(camera, panel, strip, target, 0, panelWidth, panelHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                var handSword = RequireRenderer<MeshRenderer>(model, SwordRendererName);
                var waistSword = RequireRenderer<MeshRenderer>(model, WaistSwordRendererName);
                waistSword.enabled = false;
                var body = RequireRenderer<SkinnedMeshRenderer>(model, BODY_NAME);
                var samples = new[]
                {
                    new ClipSample(sourceClip, sourceClip.length),
                    new ClipSample(holdClip, 0f),
                    new ClipSample(holdClip, StaticHoldDuration * 0.5f),
                    new ClipSample(holdClip, StaticHoldDuration),
                    new ClipSample(sourceClip, 0f)
                };
                for (var index = 0; index < samples.Length; index++)
                {
                    handSword.enabled = true;
                    waistSword.enabled = false;
                    SampleClip(model.gameObject, samples[index].Clip, samples[index].Time);
                    FrameCamera(camera, body.bounds.center, referenceHeight, 1f);
                    RenderPanel(camera, panel, strip, target, index + 1, panelWidth, panelHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
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
                throw new InvalidOperationException("The slot-6 review contains magenta shader fallback.");
            strip.SetPixels32(panelIndex * width, 0, width, height, pixels);
        }

        private static void FrameCamera(Camera camera, Vector3 center, float height, float aspect)
        {
            camera.aspect = aspect;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.25f +
                                        Vector3.up * height * 0.01f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void WriteInspection(Metrics metrics)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The slot-6 inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + TargetSlotName,
                "SourceFbx=" + SourceFbxPath,
                "ProjectSourceFbx=" + ProjectSourceFbxPath,
                "DerivedFbx=" + DerivedFbxPath,
                "SourceSha256=" + SourceSha256,
                "ProjectSourceSha256=" + SourceSha256,
                "StaticFbxSha256=" + StaticSha256,
                "DerivedSha256=" + DerivedSha256,
                "SourceAction=Armature|mixamo.com|Layer0",
                "ImportedClip=" + ImportedClipName,
                "ClipFrames=" + FirstFrame + "-" + LastFrame,
                "ClipLengthSeconds=" + Num(metrics.ClipLength),
                "ClipFrameRate=" + Num(metrics.FrameRate),
                "LoopTime=True",
                "AnimatorApplyRootMotion=False",
                "MixamoCurvesModified=False",
                "HorizontalHipsRange=" + Num(metrics.HorizontalHipsRange),
                "VerticalHipsRange=" + Num(metrics.VerticalHipsRange),
                "RightHandMaximumMotion=" + Num(metrics.MaximumRightHandMotion),
                "RightArmMaximumAngularMotionDegrees=" + Num(metrics.MaximumRightArmAngularMotion),
                "SwordMaximumAngularMotionDegrees=" + Num(metrics.MaximumSwordAngularMotion),
                "LoopRightHandError=" + Num(metrics.LoopRightHandError),
                "MixamoBones=" + ExpectedBones,
                "AnimatedBodyTriangles=" + ExpectedBodyTriangles,
                "CrescentTriangles=" + ExpectedCrescentTriangles,
                "EyeTriangles=" + ExpectedEyeTriangles,
                "RigidMusketTriangles=" + ExpectedMusketTriangles,
                "MusketStaticComponents=41,75,76",
                "MusketParent=mixamorig:Spine2",
                "MusketSkinned=False",
                "MusketMaximumAttachmentError=" + Num(metrics.MaximumMusketAttachmentError),
                "MusketMaximumBodyFollowMotion=" + Num(metrics.MaximumMusketFollowMotion),
                "SourceSwordWeightMajority=mixamorig:LeftShoulder 0.655878615",
                "SourceSwordRemoved=True",
                "SwordSource=Ispant_01_Static shared mesh and materials",
                "SwordMountSource=Ispant_04_DrawSword exact right-hand local transform",
                "SwordTriangles=" + ExpectedSwordTriangles,
                "SwordParent=mixamorig:RightHand",
                "SwordSkinned=False",
                "SwordWorldBladeLength=" + Num(metrics.SwordBladeLength),
                "SwordMaximumAttachmentError=" + Num(metrics.MaximumSwordAttachmentError),
                "SwordMaximumBodyFollowMotion=" + Num(metrics.MaximumSwordFollowMotion),
                "MaximumNearestSwordVertexToHand=" + Num(metrics.MaximumNearestSwordVertexToHand),
                "StaticBodyHeight=" + Num(metrics.StaticBodyHeight),
                "Slot6BodyHeight=" + Num(metrics.TargetBodyHeight),
                "GroundLevelDifference=" + Num(metrics.GroundLevelDifference),
                "StaticAppearanceMaterialsDirectReference=True",
                "SourceStaticGeometryMaximumWorldVertexError=0.000000119209",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + CapturePath
            }, Encoding.UTF8);
        }

        private static void WriteStaticHoldInspection(StaticHoldMetrics metrics)
        {
            var destination = Absolute(StaticHoldInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The slot-6 static-hold inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + TargetSlotName,
                "SourceFbx=" + SourceFbxPath,
                "SourceSha256=" + SourceSha256,
                "StaticFbxSha256=" + StaticSha256,
                "DerivedSha256=" + DerivedSha256,
                "SourceClip=" + ImportedClipName,
                "SourceFrames=" + FirstFrame + "-" + LastFrame,
                "SourceDurationSeconds=" + Num(metrics.SourceDuration),
                "SourceCurveCount=" + metrics.SourceCurveCount,
                "SourceMixamoCurvesModified=False",
                "HoldClip=" + StaticHoldClipName,
                "HoldDurationSeconds=" + Num(metrics.HoldDuration),
                "HoldCurveCount=" + metrics.HoldCurveCount,
                "SequenceDurationSeconds=" + Num(metrics.SequenceDuration),
                "ControllerSequence=" + StateName + "->" + StaticHoldStateName + "->" + StateName,
                "TransitionsHaveExitTime=True",
                "TransitionDurationSeconds=0",
                "LoopSequence=True",
                "MaximumSourceEndToHoldBodyError=" + Num(metrics.MaximumSourceEndToHoldBodyError),
                "MaximumHoldTransformDrift=" + Num(metrics.MaximumHoldTransformDrift),
                "MaximumHoldSwordMatrixDrift=" + Num(metrics.MaximumHoldSwordMatrixDrift),
                "SwordTarget=Ispant_01_Static body-relative left-waist transform",
                "SwordParent=mixamorig:Hips",
                "SourceSwordVisibility=RightHandTrue,LeftWaistFalse",
                "HoldSwordVisibility=RightHandFalse,LeftWaistTrue",
                "MaximumSwordStaticReferenceMatrixError=" +
                Num(metrics.MaximumSwordStaticReferenceMatrixError),
                "MaximumSwordStaticPositionError=" + Num(metrics.MaximumSwordStaticPositionError),
                "MaximumSwordStaticRotationErrorDegrees=" +
                Num(metrics.MaximumSwordStaticRotationError),
                "MaximumSwordStaticScaleError=" + Num(metrics.MaximumSwordStaticScaleError),
                "SwordWorldBladeLength=" + Num(metrics.SwordBladeLength),
                "StaticAppearanceMaterialsDirectReference=True",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + StaticHoldCapturePath
            }, Encoding.UTF8);
        }

        private static void WriteRifleSequenceInspection(RifleSequenceMetrics metrics)
        {
            var destination = Absolute(RifleSequenceInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "The slot-6 sheath-to-rifle inspection folder is invalid."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=" + PlacementRootName + "/" + TargetSlotName,
                "SheathSourceFbx=" + SourceFbxPath,
                "SheathSourceSha256=" + SourceSha256,
                "ChangeToRifleSourceFbx=" + ChangeToRifleSourceFbxPath,
                "ChangeToRifleSourceSha256=" + ChangeToRifleSourceSha256,
                "ChangeToRifleDerivedSha256=" + ChangeToRifleDerivedSha256,
                "SheathToRifleBridgeFbx=" + SheathToRifleBridgeFbxPath,
                "SheathToRifleBridgeSha256=" + SheathToRifleBridgeSha256,
                "BridgeSourceCurveCount=" + metrics.BridgeSourceCurveCount,
                "BridgeRuntimeCurveCount=" + metrics.BridgeRuntimeCurveCount,
                "ChangeToRifleSourceFrames=" + ChangeToRifleFirstFrame + "-" +
                ChangeToRifleLastFrame,
                "ChangeToRifleSourceCurveCount=" + metrics.RifleSourceCurveCount,
                "ChangeToRifleRuntimeCurveCount=" + metrics.RifleRuntimeCurveCount,
                "OriginalMixamoCurvesModified=RightShoulderRightArmRightForeArmAndRightHandRotationFor0.15mLift;LeftArmAndLeftForeArmRotationForTwoHandSupport",
                "AimArmOverrideCurveCount=24",
                "SheathDurationSeconds=" + Num(metrics.SheathDuration),
                "HoldDurationSeconds=" + Num(metrics.HoldDuration),
                "BridgeDurationSeconds=" + Num(metrics.BridgeDuration),
                "ChangeToRifleDurationSeconds=" + Num(metrics.RifleDuration),
                "SequenceDurationSeconds=" + Num(metrics.SequenceDuration),
                "ControllerSequence=" + StateName + "->" + StaticHoldStateName + "->" +
                SheathToRifleBridgeStateName + "->" + ChangeToRifleStateName + "->" + StateName,
                "TransitionDurationSeconds=0",
                "LoopSequence=True",
                "GrabSelection=Minimum right-hand to approved rigid back-musket surface distance",
                "GrabFrame=" + metrics.GrabFrame,
                "GrabTimeSeconds=" + Num(metrics.GrabTime),
                "StartHandToBackMusketDistance=" +
                Num(metrics.StartHandToBackMusketDistance),
                "GrabHandToBackMusketDistance=" + Num(metrics.GrabDistance),
                "MaximumSheathEndToHoldBodyError=" +
                Num(metrics.MaximumSheathEndToHoldError),
                "MaximumHoldBodyDrift=" + Num(metrics.MaximumHoldDrift),
                "MaximumHoldToBridgeStartBodyError=" +
                Num(metrics.MaximumHoldToBridgeStartError),
                "MaximumBridgeEndToRifleStartBodyError=" +
                Num(metrics.MaximumBridgeEndToRifleStartError),
                "BridgeRightHandWorldMotion=" + Num(metrics.BridgeRightHandMotion),
                "GrabBackToHandMusketMatrixError=" + Num(metrics.GrabContinuityError),
                "PostGrabHandMusketLocalPositionScaleDrift=" + Num(metrics.HandMusketFollowError),
                "MaximumPostGrabHandMusketLocalRotationChangeDegrees=" +
                Num(metrics.MaximumHandMusketRotationChange),
                "MaximumRightGripPivotError=" +
                Num(metrics.MaximumRightGripPivotError),
                "MaximumRightHandGripDistanceDrift=" +
                Num(metrics.MaximumRightHandGripDistanceDrift),
                "MinimumAimedLeftHandToMusketSurfaceDistance=" +
                Num(metrics.ForwardLeftHandSurfaceDistance),
                "FinalLeftHandToMusketSurfaceDistance=" +
                Num(metrics.FinalLeftHandSurfaceDistance),
                "MinimumPostGrabMuzzleToCharacterForwardAngleDegrees=" +
                Num(metrics.ForwardPoseMuzzleAngle),
                "FinalMuzzleToCharacterForwardAngleDegrees=" +
                Num(metrics.FinalMuzzleForwardAngle),
                "FinalStockAndTriggerDownToWorldDownAngleDegrees=" +
                Num(metrics.FinalStockAndTriggerDownAngle),
                "MaximumArmDrivenLocalInterpolationErrorDegrees=" +
                Num(metrics.MaximumArmDrivenLocalInterpolationError),
                "MaximumPostGrabMusketWorldMotion=" +
                Num(metrics.MaximumPostGrabMusketMotion),
                "MaximumPostGrabMusketWorldMotionTime=" +
                Num(metrics.MaximumPostGrabMusketMotionTime),
                "WaistSwordParent=mixamorig:Hips",
                "WaistSwordRigidMesh=True",
                "HoldWaistSwordStaticReferenceMatrixError=" +
                Num(metrics.HoldWaistSwordStaticReferenceMatrixError),
                "MaximumWaistSwordHipLocalMatrixDrift=" +
                Num(metrics.MaximumWaistSwordHipLocalMatrixDrift),
                "MaximumWaistSwordBodyFollowPositionChange=" +
                Num(metrics.MaximumWaistSwordBodyFollowPositionChange),
                "MaximumWaistSwordBodyFollowRotationChangeDegrees=" +
                Num(metrics.MaximumWaistSwordBodyFollowRotationChange),
                "FinalAimArmLiftTargetMeters=" + Num(FinalAimArmLift),
                "FinalMusketRootVerticalLiftMeters=" +
                Num(metrics.FinalMusketRootVerticalLift),
                "FinalMusketRootHorizontalDriftMeters=" +
                Num(metrics.FinalMusketRootHorizontalDrift),
                "FinalRightHandVerticalLiftMeters=" +
                Num(metrics.FinalRightHandVerticalLift),
                "FinalRightHandHorizontalDriftMeters=" +
                Num(metrics.FinalRightHandHorizontalDrift),
                "FinalRightShoulderRotationOverrideDegrees=" +
                Num(metrics.FinalRightShoulderRotationOverride),
                "FinalRightArmRotationOverrideDegrees=" +
                Num(metrics.FinalRightArmRotationOverride),
                "FinalRightForeArmRotationOverrideDegrees=" +
                Num(metrics.FinalRightForeArmRotationOverride),
                "MusketRootPositionCurveCount=" +
                metrics.MusketRootPositionCurveCount,
                "FinalAimLiftImplementation=RightShoulderRightArmRightForeArmAndRightHandRotationOnly;NoMusketRootPositionCurve",
                "SheathWeaponVisibility=RightHandSwordTrue,LeftWaistSwordFalse,BackMusketTrue,HandMusketFalse",
                "HoldWeaponVisibility=RightHandSwordFalse,LeftWaistSwordTrue,BackMusketTrue,HandMusketFalse",
                "RiflePreGrabVisibility=RightHandSwordFalse,LeftWaistSwordTrue,BackMusketTrue,HandMusketFalse",
                "RiflePostGrabVisibility=RightHandSwordFalse,LeftWaistSwordTrue,BackMusketFalse,HandMusketTrue",
                "HandMusketParent=mixamorig:RightHand",
                "HandMusketSkinned=False",
                "HandMusketPivot=Best two-hand support candidate within 0.02m of measured closest right-hand grab surface distance",
                "HandMusketAimRotation=Right-hand-driven fixed local grip blended to one final local aim rotation",
                "StockAndTriggerDownRollSource=Approved broad-stock authoring -Y projected orthogonal to measured muzzle axis",
                "FinalTriggerAssemblyDirection=Down",
                "FinalBroadStockThickSideDirection=Down",
                "LeftSupportSelection=Approved leather foregrip surface ahead of right-hand trigger grip",
                "MuzzleAxisSelection=Thinner endpoint of approved musket farthest-vertex axis",
                "CharacterForwardSelection=Approved slot-6 transformed model-root +Z facial axis",
                "HandMusketSharedMeshAndMaterialsFromApprovedBackMusket=True",
                "OtherSlotsChanged=False",
                "OtherSceneRootsChanged=False",
                "ReviewImage=" + RifleSequenceCapturePath,
                "ReviewImageStatus=" +
                (File.Exists(Absolute(RifleSequenceCapturePath))
                    ? "Current 0.15m final-aim arm-lift evidence"
                    : "Not captured"),
                "ReviewImageMatchesCurrentRevision=" +
                File.Exists(Absolute(RifleSequenceCapturePath))
            }, Encoding.UTF8);
        }

        private static void RequireHashes()
        {
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ProjectSourceFbxPath, SourceSha256);
            RequireHash(StaticFbxPath, StaticSha256);
            RequireHash(DerivedFbxPath, DerivedSha256);
        }

        private static void RequireChangeToRifleHashes()
        {
            RequireHash(ChangeToRifleSourceFbxPath, ChangeToRifleSourceSha256);
            RequireHash(ProjectChangeToRifleSourceFbxPath, ChangeToRifleSourceSha256);
            RequireHash(ChangeToRifleFbxPath, ChangeToRifleDerivedSha256);
            RequireHash(SheathToRifleBridgeFbxPath, SheathToRifleBridgeSha256);
        }

        private static void RequireHash(string path, string expected)
        {
            using var stream = File.OpenRead(Absolute(path));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Ispant slot-6 asset hash differs: " + path + ".");
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active for slot-6 work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
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
            return parent.Cast<Transform>().SingleOrDefault(child => child.name == name) ??
                   throw new InvalidOperationException(
                       "Required direct child is missing: " + parent.name + "/" + name + ".");
        }

        private static T RequireRenderer<T>(Transform model, string name) where T : Renderer
        {
            return model.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
                   throw new InvalidOperationException("Required slot-6 renderer is missing: " + name + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Required slot-6 bone differs: " + name + ".");
            return matches[0];
        }

        private static Mesh SharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null && filter.sharedMesh != null
                ? filter.sharedMesh
                : throw new InvalidOperationException("A slot-6 renderer has no mesh: " + renderer.name + ".");
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
            var vertices = SharedMesh(renderer).vertices;
            if (vertices.Length == 0)
                throw new InvalidOperationException("A slot-6 mesh has no vertices.");
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
            return scene.GetRootGameObjects().Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform)).ToArray();
        }

        private static string[] OtherSlotSignatures(Transform placement, Transform targetSlot)
        {
            return Enumerable.Range(0, placement.childCount).Select(placement.GetChild)
                .Where(item => item != targetSlot).Select(RecursiveSignature).ToArray();
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
                        mesh = skinned.sharedMesh;
                    else
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        mesh = filter != null ? filter.sharedMesh : null;
                    }
                    builder.Append("|R:").Append(renderer.enabled).Append(':')
                        .Append(mesh != null ? AssetDatabase.GetAssetPath(mesh) : renderer.GetType().FullName);
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private static string Num(float value)
        {
            return value.ToString("0.#########", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
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
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null)
                    renderer.enabled = enabled;
            }
        }

        private readonly struct ClipSample
        {
            public readonly AnimationClip Clip;
            public readonly float Time;

            public ClipSample(AnimationClip clip, float time)
            {
                Clip = clip;
                Time = time;
            }
        }

        private readonly struct RifleGrabSample
        {
            public readonly int Frame;
            public readonly float Time;
            public readonly float Distance;
            public readonly float StartDistance;

            public RifleGrabSample(
                int frame, float time, float distance, float startDistance)
            {
                Frame = frame;
                Time = time;
                Distance = distance;
                StartDistance = startDistance;
            }
        }

        private readonly struct RifleSequenceMetrics
        {
            public readonly int GrabFrame;
            public readonly float GrabTime;
            public readonly float GrabDistance;
            public readonly float StartHandToBackMusketDistance;
            public readonly float SheathDuration;
            public readonly float HoldDuration;
            public readonly float BridgeDuration;
            public readonly float RifleDuration;
            public readonly float SequenceDuration;
            public readonly int BridgeSourceCurveCount;
            public readonly int BridgeRuntimeCurveCount;
            public readonly int RifleSourceCurveCount;
            public readonly int RifleRuntimeCurveCount;
            public readonly float MaximumSheathEndToHoldError;
            public readonly float MaximumHoldDrift;
            public readonly float MaximumHoldToBridgeStartError;
            public readonly float MaximumBridgeEndToRifleStartError;
            public readonly float BridgeRightHandMotion;
            public readonly float GrabContinuityError;
            public readonly float HandMusketFollowError;
            public readonly float MaximumHandMusketRotationChange;
            public readonly float MaximumRightGripPivotError;
            public readonly float MaximumRightHandGripDistanceDrift;
            public readonly float ForwardLeftHandSurfaceDistance;
            public readonly float FinalLeftHandSurfaceDistance;
            public readonly float ForwardPoseMuzzleAngle;
            public readonly float FinalMuzzleForwardAngle;
            public readonly float FinalStockAndTriggerDownAngle;
            public readonly float MaximumArmDrivenLocalInterpolationError;
            public readonly float MaximumPostGrabMusketMotion;
            public readonly float MaximumPostGrabMusketMotionTime;
            public readonly float HoldWaistSwordStaticReferenceMatrixError;
            public readonly float MaximumWaistSwordHipLocalMatrixDrift;
            public readonly float MaximumWaistSwordBodyFollowPositionChange;
            public readonly float MaximumWaistSwordBodyFollowRotationChange;
            public readonly float FinalMusketRootVerticalLift;
            public readonly float FinalMusketRootHorizontalDrift;
            public readonly float FinalRightHandVerticalLift;
            public readonly float FinalRightHandHorizontalDrift;
            public readonly float FinalRightShoulderRotationOverride;
            public readonly float FinalRightArmRotationOverride;
            public readonly float FinalRightForeArmRotationOverride;
            public readonly int MusketRootPositionCurveCount;

            public RifleSequenceMetrics(
                int grabFrame,
                float grabTime,
                float grabDistance,
                float startHandToBackMusketDistance,
                float sheathDuration,
                float holdDuration,
                float bridgeDuration,
                float rifleDuration,
                float sequenceDuration,
                int bridgeSourceCurveCount,
                int bridgeRuntimeCurveCount,
                int rifleSourceCurveCount,
                int rifleRuntimeCurveCount,
                float maximumSheathEndToHoldError,
                float maximumHoldDrift,
                float maximumHoldToBridgeStartError,
                float maximumBridgeEndToRifleStartError,
                float bridgeRightHandMotion,
                float grabContinuityError,
                float handMusketFollowError,
                float maximumHandMusketRotationChange,
                float maximumRightGripPivotError,
                float maximumRightHandGripDistanceDrift,
                float forwardLeftHandSurfaceDistance,
                float finalLeftHandSurfaceDistance,
                float forwardPoseMuzzleAngle,
                float finalMuzzleForwardAngle,
                float finalStockAndTriggerDownAngle,
                float maximumArmDrivenLocalInterpolationError,
                float maximumPostGrabMusketMotion,
                float maximumPostGrabMusketMotionTime,
                float holdWaistSwordStaticReferenceMatrixError,
                float maximumWaistSwordHipLocalMatrixDrift,
                float maximumWaistSwordBodyFollowPositionChange,
                float maximumWaistSwordBodyFollowRotationChange,
                float finalMusketRootVerticalLift,
                float finalMusketRootHorizontalDrift,
                float finalRightHandVerticalLift,
                float finalRightHandHorizontalDrift,
                float finalRightShoulderRotationOverride,
                float finalRightArmRotationOverride,
                float finalRightForeArmRotationOverride,
                int musketRootPositionCurveCount)
            {
                GrabFrame = grabFrame;
                GrabTime = grabTime;
                GrabDistance = grabDistance;
                StartHandToBackMusketDistance = startHandToBackMusketDistance;
                SheathDuration = sheathDuration;
                HoldDuration = holdDuration;
                BridgeDuration = bridgeDuration;
                RifleDuration = rifleDuration;
                SequenceDuration = sequenceDuration;
                BridgeSourceCurveCount = bridgeSourceCurveCount;
                BridgeRuntimeCurveCount = bridgeRuntimeCurveCount;
                RifleSourceCurveCount = rifleSourceCurveCount;
                RifleRuntimeCurveCount = rifleRuntimeCurveCount;
                MaximumSheathEndToHoldError = maximumSheathEndToHoldError;
                MaximumHoldDrift = maximumHoldDrift;
                MaximumHoldToBridgeStartError = maximumHoldToBridgeStartError;
                MaximumBridgeEndToRifleStartError = maximumBridgeEndToRifleStartError;
                BridgeRightHandMotion = bridgeRightHandMotion;
                GrabContinuityError = grabContinuityError;
                HandMusketFollowError = handMusketFollowError;
                MaximumHandMusketRotationChange = maximumHandMusketRotationChange;
                MaximumRightGripPivotError = maximumRightGripPivotError;
                MaximumRightHandGripDistanceDrift = maximumRightHandGripDistanceDrift;
                ForwardLeftHandSurfaceDistance = forwardLeftHandSurfaceDistance;
                FinalLeftHandSurfaceDistance = finalLeftHandSurfaceDistance;
                ForwardPoseMuzzleAngle = forwardPoseMuzzleAngle;
                FinalMuzzleForwardAngle = finalMuzzleForwardAngle;
                FinalStockAndTriggerDownAngle = finalStockAndTriggerDownAngle;
                MaximumArmDrivenLocalInterpolationError =
                    maximumArmDrivenLocalInterpolationError;
                MaximumPostGrabMusketMotion = maximumPostGrabMusketMotion;
                MaximumPostGrabMusketMotionTime = maximumPostGrabMusketMotionTime;
                HoldWaistSwordStaticReferenceMatrixError =
                    holdWaistSwordStaticReferenceMatrixError;
                MaximumWaistSwordHipLocalMatrixDrift =
                    maximumWaistSwordHipLocalMatrixDrift;
                MaximumWaistSwordBodyFollowPositionChange =
                    maximumWaistSwordBodyFollowPositionChange;
                MaximumWaistSwordBodyFollowRotationChange =
                    maximumWaistSwordBodyFollowRotationChange;
                FinalMusketRootVerticalLift = finalMusketRootVerticalLift;
                FinalMusketRootHorizontalDrift = finalMusketRootHorizontalDrift;
                FinalRightHandVerticalLift = finalRightHandVerticalLift;
                FinalRightHandHorizontalDrift = finalRightHandHorizontalDrift;
                FinalRightShoulderRotationOverride =
                    finalRightShoulderRotationOverride;
                FinalRightArmRotationOverride = finalRightArmRotationOverride;
                FinalRightForeArmRotationOverride = finalRightForeArmRotationOverride;
                MusketRootPositionCurveCount = musketRootPositionCurveCount;
            }
        }

        private readonly struct StaticHoldMetrics
        {
            public readonly float SourceDuration;
            public readonly float HoldDuration;
            public readonly float SequenceDuration;
            public readonly int SourceCurveCount;
            public readonly int HoldCurveCount;
            public readonly float MaximumSourceEndToHoldBodyError;
            public readonly float MaximumHoldTransformDrift;
            public readonly float MaximumHoldSwordMatrixDrift;
            public readonly float MaximumSwordStaticReferenceMatrixError;
            public readonly float MaximumSwordStaticPositionError;
            public readonly float MaximumSwordStaticRotationError;
            public readonly float MaximumSwordStaticScaleError;
            public readonly float SwordBladeLength;

            public StaticHoldMetrics(
                float sourceDuration,
                float holdDuration,
                float sequenceDuration,
                int sourceCurveCount,
                int holdCurveCount,
                float maximumSourceEndToHoldBodyError,
                float maximumHoldTransformDrift,
                float maximumHoldSwordMatrixDrift,
                float maximumSwordStaticReferenceMatrixError,
                float maximumSwordStaticPositionError,
                float maximumSwordStaticRotationError,
                float maximumSwordStaticScaleError,
                float swordBladeLength)
            {
                SourceDuration = sourceDuration;
                HoldDuration = holdDuration;
                SequenceDuration = sequenceDuration;
                SourceCurveCount = sourceCurveCount;
                HoldCurveCount = holdCurveCount;
                MaximumSourceEndToHoldBodyError = maximumSourceEndToHoldBodyError;
                MaximumHoldTransformDrift = maximumHoldTransformDrift;
                MaximumHoldSwordMatrixDrift = maximumHoldSwordMatrixDrift;
                MaximumSwordStaticReferenceMatrixError = maximumSwordStaticReferenceMatrixError;
                MaximumSwordStaticPositionError = maximumSwordStaticPositionError;
                MaximumSwordStaticRotationError = maximumSwordStaticRotationError;
                MaximumSwordStaticScaleError = maximumSwordStaticScaleError;
                SwordBladeLength = swordBladeLength;
            }
        }

        private readonly struct SwordDimensions
        {
            public readonly float BladeLength;
            public readonly float HandleSize;

            public SwordDimensions(float bladeLength, float handleSize)
            {
                BladeLength = bladeLength;
                HandleSize = handleSize;
            }
        }

        private readonly struct Metrics
        {
            public readonly float ClipLength;
            public readonly float FrameRate;
            public readonly float HorizontalHipsRange;
            public readonly float VerticalHipsRange;
            public readonly float MaximumMusketAttachmentError;
            public readonly float MaximumSwordAttachmentError;
            public readonly float MaximumMusketFollowMotion;
            public readonly float MaximumSwordFollowMotion;
            public readonly float MaximumRightHandMotion;
            public readonly float MaximumRightArmAngularMotion;
            public readonly float MaximumSwordAngularMotion;
            public readonly float LoopRightHandError;
            public readonly float MaximumNearestSwordVertexToHand;
            public readonly float StaticBodyHeight;
            public readonly float TargetBodyHeight;
            public readonly float GroundLevelDifference;
            public readonly float SwordBladeLength;

            public Metrics(
                float clipLength,
                float frameRate,
                float horizontalHipsRange,
                float verticalHipsRange,
                float maximumMusketAttachmentError,
                float maximumSwordAttachmentError,
                float maximumMusketFollowMotion,
                float maximumSwordFollowMotion,
                float maximumRightHandMotion,
                float maximumRightArmAngularMotion,
                float maximumSwordAngularMotion,
                float loopRightHandError,
                float maximumNearestSwordVertexToHand,
                float staticBodyHeight,
                float targetBodyHeight,
                float groundLevelDifference,
                float swordBladeLength)
            {
                ClipLength = clipLength;
                FrameRate = frameRate;
                HorizontalHipsRange = horizontalHipsRange;
                VerticalHipsRange = verticalHipsRange;
                MaximumMusketAttachmentError = maximumMusketAttachmentError;
                MaximumSwordAttachmentError = maximumSwordAttachmentError;
                MaximumMusketFollowMotion = maximumMusketFollowMotion;
                MaximumSwordFollowMotion = maximumSwordFollowMotion;
                MaximumRightHandMotion = maximumRightHandMotion;
                MaximumRightArmAngularMotion = maximumRightArmAngularMotion;
                MaximumSwordAngularMotion = maximumSwordAngularMotion;
                LoopRightHandError = loopRightHandError;
                MaximumNearestSwordVertexToHand = maximumNearestSwordVertexToHand;
                StaticBodyHeight = staticBodyHeight;
                TargetBodyHeight = targetBodyHeight;
                GroundLevelDifference = groundLevelDifference;
                SwordBladeLength = swordBladeLength;
            }
        }
    }
}
