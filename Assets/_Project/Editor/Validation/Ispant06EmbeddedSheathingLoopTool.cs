using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class Ispant06EmbeddedSheathingLoopTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string SlotName = "Ispant_06_SheathSwordDrawMusket";
        private const string ModelName = "Ispant_New_Direct_Model";
        // This imported copy preserves the user-supplied FBX as the sole motion source for slot 6.
        private const string SourceFbxPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_New_SheathingSword_Source.fbx";
        private const string SourceClipName = "mixamo.com";
        private const string LoopClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_New_SheathingSword_Loop.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_06_New_SheathingSword_Loop.controller";
        private const string BodyWithoutMusketPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWithoutBackMusket.asset";
        private const string ReviewPath =
            "docs/validation/ispant_06_embedded_sheathing_loop_2026-08-21/" +
            "Ispant_06_EmbeddedSheathingLoop_Review.png";
        private const string StretchTriangleDiagnosisPath =
            "docs/validation/ispant_06_left_arm_weight_fix_2026-08-21/" +
            "Ispant_06_StretchTriangle_Diagnosis.txt";
        private const string RendererInspectionPath =
            "docs/validation/ispant_06_waist_remnant_2026-08-21/" +
            "Ispant_06_Renderers.txt";
        private const string PickedPartHighlightPath =
            "docs/validation/ispant_06_waist_remnant_2026-08-21/" +
            "Ispant_06_PickedPartHighlight.png";
        private const string PickedPartRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyPickedPartRemoved.asset";
        private const string SelectionHighlightPath =
            "docs/validation/ispant_06_waist_remnant_2026-08-21/" +
            "Ispant_06_SelectionHighlight.png";
        private const string HipAsymmetryPath =
            "docs/validation/ispant_06_waist_remnant_2026-08-21/" +
            "Ispant_06_HipAsymmetry.txt";
        private const string HipAsymmetryRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyHipAsymmetryRemoved.asset";
        private const string LeftHipCloseupPath =
            "docs/validation/ispant_06_waist_remnant_2026-08-21/" +
            "Ispant_06_LeftHip_Closeup.png";
        private const string FloatingHiltComparisonPath =
            "docs/validation/ispant_06_floating_hilt_2026-08-21/" +
            "Ispant_06_FloatingHilt_Comparison.png";
        private const string FloatingHiltHighlightPath =
            "docs/validation/ispant_06_floating_hilt_2026-08-21/" +
            "Ispant_06_FloatingHilt_Highlight.png";
        private const string FloatingHiltPreviewPath =
            "docs/validation/ispant_06_floating_hilt_2026-08-21/" +
            "Ispant_06_FloatingHilt_RemovalPreview.png";
        private const string FloatingHiltRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyFloatingHiltRemoved.asset";
        private const string MissingGeometryPath =
            "docs/validation/ispant_06_left_thigh_restore_2026-08-21/" +
            "Ispant_06_MissingGeometry.txt";
        private const string MissingClusterAtlasPath =
            "docs/validation/ispant_06_left_thigh_restore_2026-08-21/" +
            "Ispant_06_MissingClusters.png";
        private const string LeftThighRestorePreviewPath =
            "docs/validation/ispant_06_left_thigh_restore_2026-08-21/" +
            "Ispant_06_LeftThighRestore_Preview.png";
        private const string RestoredClusterAtlasPath =
            "docs/validation/ispant_06_left_leg_restore_2026-08-21/" +
            "Ispant_06_RestoredClusters.png";
        private const string RestoredClusterReportPath =
            "docs/validation/ispant_06_left_leg_restore_2026-08-21/" +
            "Ispant_06_RestoredClusters.txt";
        private const string RestoredClusterRemovalPreviewPath =
            "docs/validation/ispant_06_left_leg_restore_2026-08-21/" +
            "Ispant_06_RestoredClusterRemoval_Preview.png";
        private const string WaistDebrisRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved.asset";
        private const string MarkedHiltFragmentRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyMarkedHiltFragmentRemoved.asset";
        private const string MarkedHiltFragmentPreviewPath =
            "docs/validation/ispant_06_marked_hilt_fragment_removal_2026-08-25/" +
            "Ispant_06_MarkedHiltFragment_Preview.png";
        private const string MarkedHiltFragmentFinalPath =
            "docs/validation/ispant_06_marked_hilt_fragment_removal_2026-08-25/" +
            "Ispant_06_MarkedHiltFragment_Final.png";
        private const string MarkedHiltFragmentReportPath =
            "docs/validation/ispant_06_marked_hilt_fragment_removal_2026-08-25/" +
            "Ispant_06_MarkedHiltFragment_Report.txt";
        private const string LeftThighRestoredMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftThighRestored.asset";
        // Indexes into the removed-cluster list ordered by triangle count, the same order the
        // missing geometry report and the cluster atlas use. Cluster 4 is the left thigh plate
        // that an earlier pass cut away after mistaking it for a sheathed hilt. Cluster 0 is
        // deliberately not here: several removals merged into that one connected lump, so
        // restoring it would bring the floating flakes back with it.
        private static readonly int[] LeftThighRestoreClusters = { 0, 2, 3, 4, 5, 6, 7, 8 };
        private const string RemainingIslandsPath =
            "docs/validation/ispant_06_waist_remnant_2026-08-21/" +
            "Ispant_06_RemainingIslands.txt";
        private const string WaistRemnantRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistRemnantRemoved.asset";
        private const string ArmTorsoBridgeRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyArmTorsoBridgeRemoved.asset";
        private const string LeftArmRegionCleanMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmRegionClean.asset";
        private const string LeftArmSeamSplitMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmSeamSplit.asset";
        private const string LeftArmWeightFixedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmWeightFixed.asset";
        private const string LeftArmStretchDiagnosisPath =
            "docs/validation/ispant_06_left_arm_stretch_2026-08-21/" +
            "Ispant_06_LeftArmStretch_Diagnosis.txt";
        private const string LeftArmStretchRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmStretchRemoved.asset";
        private const string StaticReturnBoundaryPath =
            "docs/validation/ispant_06_static_return_tail_2026-08-21/" +
            "Ispant_06_StaticReturn_Boundary.txt";
        private const string StaticReturnTailPath =
            "docs/validation/ispant_06_static_return_tail_2026-08-21/" +
            "Ispant_06_StaticReturn_Tail.txt";
        // The user asked for a 0.4 second return to the static model pose after the sheathing.
        private const float StaticReturnTailSeconds = 0.4f;
        private const string WaistHiltSeparatedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltSeparated.asset";
        private const string WaistHiltRemovedMeshPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltRemoved.asset";
        private const string WaistHiltDiagnosisPath =
            "docs/validation/ispant_06_waist_hilt_2026-08-21/" +
            "Ispant_06_WaistHilt_Diagnosis.txt";
        private const string HandSwordClearancePath =
            "docs/validation/ispant_06_hand_sword_2026-08-21/" +
            "Ispant_06_HandSword_Clearance.txt";
        private const string HandSwordReviewPath =
            "docs/validation/ispant_06_hand_sword_2026-08-21/" +
            "Ispant_06_HandSword_GripAndWaist_Review.png";
        private const string RollCorrectionReviewPath =
            "docs/validation/ispant_06_right_arm_basis_2026-08-21/" +
            "Ispant_06_RightArmRollCorrection_Review.png";
        private const string BasisDiagnosisPath =
            "docs/validation/ispant_06_right_arm_basis_2026-08-21/" +
            "Ispant_06_RightArmBasis_Diagnosis.txt";
        private const string ModelBuiltInSwordName = "Ispant_Approved_LongSword_10K";
        private const string HandSwordName = "Ispant_06_LegacyHandSword";
        private const string WaistSwordName = "Ispant_06_LegacyWaistSword";
        private const string BackMusketName = "Ispant_06_BackMusket";
        private const string HandMusketRendererName = "Ispant_06_HandMusket_Renderer";
        private const string SwordGripReviewPath =
            "docs/validation/ispant_06_embedded_sheathing_loop_2026-08-21/" +
            "Ispant_06_EmbeddedLoopSwordGrip_Review.png";
        private const string SwordSheathReviewPath =
            "docs/validation/ispant_06_embedded_sheathing_loop_2026-08-21/" +
            "Ispant_06_EmbeddedLoopSwordSheath_Review.png";
        private const string ArmMeshReviewPath =
            "docs/validation/ispant_06_right_arm_axis_fix_2026-08-21/" +
            "Ispant_06_RightArmRestBasis_Review.png";
        // These existing approved-sword proportions isolate the hand grip near the pommel.
        private const float SwordGripDistanceFromPommelRatio = 0.13f;
        private const float SwordGripHalfWidthRatio = 0.05f;
        // The loop starts and ends in the static left-waist mount; the middle follows RightHand.
        private const float SwordHandFollowStartRatio = 0.08f;
        private const float SwordSheathRotationStartRatio = 0.58f;
        private const float SwordSheathRotationEndRatio = 0.82f;
        private const float SwordSheathPositionStartRatio = 0.78f;
        private const float SwordSheathPositionEndRatio = 0.94f;

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Embedded Sheathing Source")]
        public static void InspectIspant06EmbeddedSheathingSource()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var animator = model.GetComponent<Animator>() ??
                           throw new InvalidOperationException(
                               "The current slot-6 model has no Animator.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath) ??
                         throw new InvalidOperationException(
                             "The imported slot-6 sheathing source FBX is missing.");
            var clips = AssetDatabase.LoadAllAssetsAtPath(SourceFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length == 0)
                throw new InvalidOperationException(
                    "The imported slot-6 sheathing source FBX contains no animation clip.");

            var targetPaths = model.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, model))
                .ToHashSet(StringComparer.Ordinal);
            var report = new StringBuilder("Ispant06EmbeddedSheathingSourceInspection\n");
            report.AppendLine("Source=" + SourceFbxPath);
            report.AppendLine("CurrentController=" +
                              AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            foreach (var clip in clips)
            {
                var transformBindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(item => item.type == typeof(Transform))
                    .ToArray();
                var missingPaths = transformBindings.Select(item => item.path)
                    .Distinct(StringComparer.Ordinal)
                    .Where(path => !targetPaths.Contains(path))
                    .ToArray();
                report.AppendLine(
                    "Clip=" + clip.name +
                    "|Length=" + clip.length.ToString("0.######") +
                    "|FrameRate=" + clip.frameRate.ToString("0.######") +
                    "|TransformBindings=" + transformBindings.Length +
                    "|MissingTargetPaths=" + missingPaths.Length);
                foreach (var path in missingPaths.Take(20))
                    report.AppendLine("MissingPath=" + path);
            }
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                report.AppendLine(
                    "SourceRenderer=" +
                    AnimationUtility.CalculateTransformPath(renderer.transform, source.transform) +
                    "|Name=" + renderer.name + "|Type=" + renderer.GetType().Name);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                report.AppendLine(
                    "CurrentRenderer=" +
                    AnimationUtility.CalculateTransformPath(renderer.transform, model) +
                    "|Name=" + renderer.name + "|Enabled=" + renderer.enabled);
            var sourceClip = RequireSourceClip();
            var loopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                           throw new InvalidOperationException(
                               "The slot-6 embedded sheathing loop clip is missing.");
            RequireMatchingTransformCurves(sourceClip, loopClip);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException(
                                 "The slot-6 embedded sheathing controller is missing.");
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states)
                .Select(item => item.state).ToArray();
            if (animator.runtimeAnimatorController != controller ||
                controller.layers.Length != 1 ||
                states.Length != 1 ||
                states[0].motion != loopClip ||
                controller.layers[0].stateMachine.defaultState != states[0])
                throw new InvalidOperationException(
                    "The slot-6 Animator is not connected only to the raw embedded loop.");
            report.AppendLine("RawMixamoTransformCurvesMatch=True");
            report.AppendLine("SingleLoopControllerConnected=True");
            Debug.Log(report.ToString());
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Embedded Sheathing Loop")]
        public static void ApplyIspant06EmbeddedSheathingLoop()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var sourceClip = RequireSourceClip();
            RequireDirectClipCompatibility(sourceClip, model);
            var animator = model.GetComponent<Animator>() ?? model.gameObject.AddComponent<Animator>();
            var previousController = AssetDatabase.GetAssetPath(
                animator.runtimeAnimatorController);
            // Disconnect before rebuilding the one-state controller so no prior motion
            // connection remains active while the raw embedded clip is restored.
            animator.runtimeAnimatorController = null;
            EditorUtility.SetDirty(animator);
            var loopClip = CreateOrUpdateLoopClip(sourceClip);
            RequireMatchingTransformCurves(sourceClip, loopClip);
            var controller = CreateOrUpdateLoopController(loopClip);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true)
                         .Where(item =>
                             item.name == "Ispant_06_LegacyHandSword" ||
                             item.name == "Ispant_06_LegacyWaistSword" ||
                             item.name == "Ispant_06_HandMusket_Renderer"))
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after replacing the slot-6 Animator connection.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06EmbeddedSheathingLoopAppliedForVisualReview" +
                ", SourceClip=mixamo.com" +
                ", PreviousController=" + previousController +
                ", PreviousAnimatorConnectionExplicitlyCleared=True" +
                ", RawMixamoTransformCurvesMatch=True" +
                ", SingleLoopControllerConnected=True" +
                ", CurrentModelPreserved=True" +
                ", VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Embedded Sheathing Loop Review")]
        public static void CaptureIspant06EmbeddedSheathingLoopReview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var destination = Absolute(ReviewPath);
            const int panelWidth = 480;
            const int panelHeight = 640;
            const int columns = 3;
            const int rows = 2;
            const int captureLayer = 30;
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelWidth * columns, panelHeight * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06EmbeddedSheathingLoopReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 32f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < columns * rows; index++)
                {
                    Restore(transforms);
                    var time = clip.length * index / (columns * rows - 1f);
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, time);
                    FrameCamera(camera, body.bounds);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, panelWidth, panelHeight), 0, 0);
                    panel.Apply();
                    var column = index % columns;
                    var row = rows - 1 - index / columns;
                    sheet.SetPixels32(
                        column * panelWidth, row * panelHeight,
                        panelWidth, panelHeight, panel.GetPixels32());
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6 embedded sheathing review changed the scene dirty state.");
            Debug.Log(
                "Ispant06EmbeddedSheathingLoopReviewCaptured" +
                ", Image=" + ReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Embedded Loop Sword Grip")]
        public static void ApplyIspant06EmbeddedLoopSwordGrip()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var rightHand = RequireBone(model, "RightHand");
            var rightForeArm = RequireBone(model, "RightForeArm");
            var rightShoulder = RequireBone(model, "RightShoulder");
            var spine = RequireBone(model, "Spine");
            var handSword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandSwordName);
            if (handSword.transform.parent != rightHand)
                handSword.transform.SetParent(rightHand, false);
            BakeRightArmSwordCurves(
                model, body, rightHand, rightForeArm, rightShoulder, spine, handSword, clip);
            handSword.enabled = true;
            EditorUtility.SetDirty(handSword);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true)
                         .Where(item => item.name == WaistSwordName))
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-6 sword grip update.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06EmbeddedLoopSwordGripAppliedForVisualReview" +
                ", SwordParent=RightHand" +
                ", HiltTracksVisibleRightGlove=True" +
                ", BladeDirectionTracksRightForeArm=True" +
                ", MixamoBodyCurvesChanged=False" +
                ", VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Embedded Loop Sword Grip Review")]
        public static void CaptureIspant06EmbeddedLoopSwordGripReview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var rightHand = RequireBone(model, "RightHand");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var destination = Absolute(SwordGripReviewPath);
            const int panelSize = 512;
            const int panelCount = 6;
            const int captureLayer = 30;
            var target = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelSize, panelSize, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelSize * panelCount, panelSize * 2, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06EmbeddedLoopSwordGripReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < panelCount; index++)
                {
                    Restore(transforms);
                    var time = clip.length * index / (panelCount - 1f);
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, time);

                    FrameCamera(camera, body.bounds);
                    RenderIntoSheet(camera, target, panel, sheet, index, 1, panelSize);

                    FrameGripCamera(camera, rightHand.position, body.bounds.size.y);
                    RenderIntoSheet(camera, target, panel, sheet, index, 0, panelSize);
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6 sword-grip review changed the scene dirty state.");
            Debug.Log(
                "Ispant06EmbeddedLoopSwordGripReviewCaptured" +
                ", Top=FullBodySixPhases" +
                ", Bottom=RightHandCloseSixPhases" +
                ", Image=" + SwordGripReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Embedded Loop Sword Sheath Path")]
        public static void ApplyIspant06EmbeddedLoopSwordSheathPath()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var rightHand = RequireBone(model, "RightHand");
            var handSword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandSwordName);
            var handSwordMesh = handSword.GetComponent<MeshFilter>()?.sharedMesh ??
                                throw new InvalidOperationException(
                                    "The slot-6 hand sword mesh is missing.");
            var staticSword = staticModel.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.GetComponent<MeshFilter>()?.sharedMesh == handSwordMesh);
            var previousSwordPath = AnimationUtility.CalculateTransformPath(
                handSword.transform, model);
            var handMountRotation = CaptureSwordHandMountRotation(
                model, rightHand, handSword.transform, clip,
                clip.length * SwordHandFollowStartRatio);
            var staticSwordMatrix = staticModel.worldToLocalMatrix * staticSword.localToWorldMatrix;
            DecomposeTrs(
                staticSwordMatrix,
                out var staticLocalPosition,
                out var staticLocalRotation,
                out var staticLocalScale);

            handSword.transform.SetParent(model, true);
            handSword.transform.localScale = staticLocalScale;
            BakeRightHandToStaticWaistSwordCurves(
                model,
                body,
                rightHand,
                handSword,
                clip,
                previousSwordPath,
                handMountRotation,
                staticLocalPosition,
                staticLocalRotation,
                RequireSourceClip().length);
            handSword.enabled = true;
            EditorUtility.SetDirty(handSword);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true)
                         .Where(item => item.name == WaistSwordName ||
                                        (item != handSword &&
                                         item.GetComponent<MeshFilter>()?.sharedMesh == handSwordMesh)))
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-6 sword sheath-path update.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06EmbeddedLoopSwordSheathPathAppliedForVisualReview" +
                ", SwordParent=Ispant_New_Direct_Model" +
                ", MiddleMotionFollowsRightHandPositionAndRotation=True" +
                ", FinalPoseUsesIspant01StaticSword=True" +
                ", MixamoBodyCurvesChanged=False" +
                ", VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Embedded Loop Sword Sheath Review")]
        public static void CaptureIspant06EmbeddedLoopSwordSheathReview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var staticBody = staticModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var destination = Absolute(SwordSheathReviewPath);
            var phases = new[] { 0f, 0.15f, 0.35f, 0.55f, 0.75f, 0.85f, 0.95f, 0.99f };
            const int panelWidth = 400;
            const int panelHeight = 480;
            const int columns = 9;
            const int captureLayer = 30;
            const int staticLayer = 29;
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelWidth * columns, panelHeight * 2, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06EmbeddedLoopSwordSheathReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Concat(staticModel.GetComponentsInChildren<Transform>(true))
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .Concat(staticModel.GetComponentsInChildren<Transform>(true))
                .Distinct()
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in model.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = captureLayer;
            foreach (var item in staticModel.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = staticLayer;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    Restore(transforms);
                    var reviewBody = staticBody;
                    camera.cullingMask = 1 << staticLayer;
                    if (column > 0)
                    {
                        AnimationMode.SampleAnimationClip(
                            model.gameObject, clip, clip.length * phases[column - 1]);
                        reviewBody = body;
                        camera.cullingMask = 1 << captureLayer;
                    }

                    FrameCamera(camera, reviewBody.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 1, panelWidth, panelHeight);
                    FrameTorsoCamera(camera, reviewBody.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 0, panelWidth, panelHeight);
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6 sword-sheath review changed the scene dirty state.");
            Debug.Log(
                "Ispant06EmbeddedLoopSwordSheathReviewCaptured" +
                ", FirstColumn=Ispant01StaticSwordReference" +
                ", RemainingColumns=Slot06EightMotionPhases" +
                ", Image=" + SwordSheathReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Embedded Loop Arm And Musket Fix")]
        public static void ApplyIspant06EmbeddedLoopArmAndMusketFix()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var separatedBody = AssetDatabase.LoadAssetAtPath<Mesh>(BodyWithoutMusketPath) ??
                                throw new InvalidOperationException(
                                    "The existing slot-6 body-without-musket mesh is missing.");
            var rightHand = RequireBone(model, "RightHand");
            var handSword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandSwordName);
            var handSwordMesh = handSword.GetComponent<MeshFilter>()?.sharedMesh ??
                                throw new InvalidOperationException(
                                    "The slot-6 hand sword mesh is missing.");
            var staticSword = staticModel.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.GetComponent<MeshFilter>()?.sharedMesh == handSwordMesh);
            var staticSwordMatrix = staticModel.worldToLocalMatrix * staticSword.localToWorldMatrix;
            DecomposeTrs(
                staticSwordMatrix,
                out var staticLocalPosition,
                out var staticLocalRotation,
                out var staticLocalScale);

            body.sharedMesh = separatedBody;
            EditorUtility.SetDirty(body);
            BakeRightArmRestBasisRotationTransfer(model, clip);
            var handMountRotation = CaptureSwordHandMountRotation(
                model, rightHand, handSword.transform, clip,
                clip.length * SwordHandFollowStartRatio);

            var backMusket = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == BackMusketName);
            var spine = RequireBone(model, "Spine");
            var backMusketModelMatrix = model.worldToLocalMatrix * backMusket.localToWorldMatrix;
            var spineModelMatrix = model.worldToLocalMatrix * spine.localToWorldMatrix;
            backMusket.transform.SetParent(spine, false);
            SetLocalMatrix(backMusket.transform, spineModelMatrix.inverse * backMusketModelMatrix);
            backMusket.enabled = true;
            EditorUtility.SetDirty(backMusket.transform);
            EditorUtility.SetDirty(backMusket);
            foreach (var handMusket in model.GetComponentsInChildren<MeshRenderer>(true)
                         .Where(item => item.name == HandMusketRendererName))
            {
                handMusket.enabled = false;
                EditorUtility.SetDirty(handMusket);
            }

            handSword.transform.localScale = staticLocalScale;
            BakeRightHandToStaticWaistSwordCurves(
                model,
                body,
                rightHand,
                handSword,
                clip,
                AnimationUtility.CalculateTransformPath(handSword.transform, model),
                handMountRotation,
                staticLocalPosition,
                staticLocalRotation,
                RequireSourceClip().length);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-6 arm and musket fix.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06EmbeddedLoopArmAndMusketFixAppliedForVisualReview" +
                ", RightArmJointTrajectoryPreserved=True" +
                ", UpperAndLowerArmUseSourceFullRotationDeltaInTargetRestBasis=True" +
                ", RightHandUsesFixedTargetRestForeArmRelativePose=True" +
                ", QuaternionHemisphereContinuity=True" +
                ", ExistingBodyWithoutMusketReused=True" +
                ", BackMusketParent=Spine" +
                ", SwordCurvesRebakedForCorrectedHand=True" +
                ", VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Embedded Loop Arm Axis Fix")]
        public static void InspectIspant06EmbeddedLoopArmAxisFix()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var sourceClip = RequireSourceClip();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            RequireDirectClipCompatibility(sourceClip, model);
            if (Mathf.Abs(sourceClip.length - clip.length) > 0.0001f ||
                Mathf.Abs(sourceClip.frameRate - clip.frameRate) > 0.0001f)
                throw new InvalidOperationException(
                    "The corrected slot-6 loop no longer matches the source duration or frame rate.");
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("The corrected slot-6 clip is not looping.");

            var names = new[] { "RightArm", "RightForeArm", "RightHand" };
            var rotationPathByName = names.ToDictionary(
                name => name,
                name => AnimationUtility.CalculateTransformPath(
                    RequireBone(model, name), model),
                StringComparer.Ordinal);
            var rotationPaths = rotationPathByName.Values.ToHashSet(StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip)
                         .Where(item => item.type == typeof(Transform) &&
                                        !(rotationPaths.Contains(item.path) &&
                                          item.propertyName.StartsWith(
                                              "m_LocalRotation.", StringComparison.Ordinal))))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                var targetCurve = AnimationUtility.GetEditorCurve(clip, binding);
                if (!CurvesMatch(sourceCurve, targetCurve))
                    throw new InvalidOperationException(
                        "A non-target source curve changed during the right-arm correction: " +
                        binding.path + "|" + binding.propertyName);
            }

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath) ??
                               throw new InvalidOperationException(
                                   "The imported slot-6 sheathing source FBX is missing.");
            var previewScene = EditorSceneManager.NewPreviewScene();
            var sourceObject = PrefabUtility.InstantiatePrefab(
                                   sourcePrefab, previewScene) as GameObject ??
                               throw new InvalidOperationException(
                                   "The slot-6 source FBX could not be instantiated for inspection.");
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            var sourceModel = sourceObject.transform;
            var sourceBones = names.ToDictionary(
                name => name, name => RequireBone(sourceModel, name), StringComparer.Ordinal);
            var targetBones = names.ToDictionary(
                name => name, name => RequireBone(model, name), StringComparer.Ordinal);
            var sourceRest = sourceBones.ToDictionary(
                item => item.Key, item => item.Value.localRotation, StringComparer.Ordinal);
            var targetRest = targetBones.ToDictionary(
                item => item.Key, item => item.Value.localRotation, StringComparer.Ordinal);
            var sourceStates = sourceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var previous = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
            var maximumAngleError = 0f;
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(
                    1, Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(sourceStates);
                    var time = sourceClip.length * frame / frameCount;
                    AnimationMode.SampleAnimationClip(sourceObject, sourceClip, time);
                    foreach (var name in names)
                    {
                        var expected = name == "RightHand"
                            ? targetRest[name]
                            : TransferLocalRotationThroughRestBasis(
                                sourceRest[name],
                                targetRest[name],
                                sourceBones[name].localRotation);
                        var actual = EvaluateLocalRotation(
                            clip, rotationPathByName[name], time);
                        maximumAngleError = Mathf.Max(
                            maximumAngleError, Quaternion.Angle(expected, actual));
                        if (previous.TryGetValue(name, out var previousRotation) &&
                            Quaternion.Dot(previousRotation, actual) < 0f)
                            throw new InvalidOperationException(
                                "Quaternion hemisphere continuity failed for " + name + ".");
                        previous[name] = actual;
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(sourceStates);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
            if (maximumAngleError > 0.1f)
                throw new InvalidOperationException(
                    "The right-arm rest-basis transfer differs from the sampled source. " +
                    "MaximumAngleErrorDegrees=" + maximumAngleError.ToString("0.######"));
            Debug.Log(
                "Ispant06EmbeddedLoopArmAxisFixInspection" +
                ", SourceDurationAndFrameRatePreserved=True" +
                ", LoopPreserved=True" +
                ", NonTargetSourceCurvesPreserved=True" +
                ", RightArmAndForeArmFullRestBasisDeltaMatch=True" +
                ", RightHandFixedForeArmRelativeRestPose=True" +
                ", QuaternionHemisphereContinuity=True" +
                ", MaximumAngleErrorDegrees=" + maximumAngleError.ToString("0.######") +
                ", VisualVerdict=PendingUserReview.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Embedded Loop Arm And Musket Review")]
        public static void CaptureIspant06EmbeddedLoopArmAndMusketReview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var destination = Absolute(ArmMeshReviewPath);
            var phases = new[] { 0f, 0.14f, 0.28f, 0.42f, 0.56f, 0.70f, 0.84f, 0.98f };
            const int panelWidth = 420;
            const int panelHeight = 520;
            const int columns = 8;
            const int captureLayer = 30;
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelWidth * columns, panelHeight * 2, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06EmbeddedLoopArmMeshReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * phases[column]);
                    FrameCamera(camera, body.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 1, panelWidth, panelHeight);
                    FrameRightArmCamera(camera, body.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 0, panelWidth, panelHeight);
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The slot-6 arm and musket review changed the scene dirty state.");
            Debug.Log(
                "Ispant06EmbeddedLoopArmAndMusketReviewCaptured" +
                ", Top=FullBodyEightPhases" +
                ", Bottom=ArmAndMusketCloseEightPhases" +
                ", Image=" + ArmMeshReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        // Diagnosis only. Reports how far the source Mixamo rig rest orientation sits from the
        // current slot-6 rig across the whole right arm chain, whether the loop clip still carries
        // the raw source curves per bone, and how closely the limb direction trajectory matches.
        // A near-zero rest difference would mean the twist lives in the source motion itself.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Right Arm Rest Basis Diff")]
        public static void InspectIspant06RightArmRestBasisDiff()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var sourceClip = RequireSourceClip();
            var loopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                           throw new InvalidOperationException(
                               "The slot-6 embedded sheathing loop clip is missing.");
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath) ??
                               throw new InvalidOperationException(
                                   "The imported slot-6 sheathing source FBX is missing.");
            var previewScene = EditorSceneManager.NewPreviewScene();
            var sourceObject = PrefabUtility.InstantiatePrefab(
                                   sourcePrefab, previewScene) as GameObject ??
                               throw new InvalidOperationException(
                                   "The slot-6 source FBX could not be instantiated for diagnosis.");
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            var report = new StringBuilder();
            var chain = new[] { "RightShoulder", "RightArm", "RightForeArm", "RightHand" };
            var mirror = new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };
            var maxLocalRest = 0f;
            var maxDirection = 0f;
            var maxAxisDeviation = 0f;
            var sourceModel = sourceObject.transform;
            var sourceStates = sourceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var targetStates = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            try
            {
                report.AppendLine("Ispant06RightArmRestBasisDiagnosis");
                report.AppendLine("SourceFbx=" + SourceFbxPath);
                report.AppendLine("SourceClip=" + sourceClip.name +
                                  ", Length=" + sourceClip.length.ToString("F6") +
                                  ", FrameRate=" + sourceClip.frameRate.ToString("F2"));
                report.AppendLine();
                report.AppendLine("[Rest orientation: source rig vs current slot-6 rig]");
                foreach (var name in chain.Concat(mirror))
                {
                    var sourceBone = RequireBone(sourceModel, name);
                    var targetBone = RequireBone(model, name);
                    var localAngle = Quaternion.Angle(
                        sourceBone.localRotation, targetBone.localRotation);
                    var sourceSpace = Quaternion.Inverse(sourceModel.rotation) * sourceBone.rotation;
                    var targetSpace = Quaternion.Inverse(model.rotation) * targetBone.rotation;
                    var modelAngle = Quaternion.Angle(sourceSpace, targetSpace);
                    if (chain.Contains(name)) maxLocalRest = Mathf.Max(maxLocalRest, localAngle);
                    report.AppendLine(
                        name +
                        ": LocalRestAngle=" + localAngle.ToString("F4") + "deg" +
                        ", ModelSpaceRestAngle=" + modelAngle.ToString("F4") + "deg");
                }

                report.AppendLine();
                report.AppendLine("[Bind pose rest, the actual skinning reference]");
                var targetPrefab = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
                report.AppendLine(
                    "CurrentModelPrefab=" +
                    (targetPrefab == null
                        ? "none"
                        : AssetDatabase.GetAssetPath(targetPrefab)));
                var sourceSkin = sourceModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .OrderByDescending(item => item.sharedMesh == null ? 0 : item.sharedMesh.vertexCount)
                    .FirstOrDefault();
                var targetSkin = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(item => item.name == "char1");
                report.AppendLine(
                    "SourceSkin=" + (sourceSkin == null ? "none" : sourceSkin.name) +
                    ", TargetSkin=" + (targetSkin == null ? "none" : targetSkin.name));
                if (sourceSkin != null && targetSkin != null)
                {
                    foreach (var name in chain)
                    {
                        var sourceBind = BindRotation(sourceSkin, name);
                        var targetBind = BindRotation(targetSkin, name);
                        if (sourceBind == null || targetBind == null)
                        {
                            report.AppendLine(name + ": bind pose not found");
                            continue;
                        }

                        var bindDelta = Quaternion.Inverse(sourceBind.Value) * targetBind.Value;
                        var targetNodeRest = Quaternion.Inverse(targetSkin.transform.rotation) *
                                             RequireBone(model, name).rotation;
                        var sourceNodeRest = Quaternion.Inverse(sourceSkin.transform.rotation) *
                                             RequireBone(sourceModel, name).rotation;
                        report.AppendLine(
                            name +
                            ": BindRestAngle=" +
                            Quaternion.Angle(sourceBind.Value, targetBind.Value).ToString("F4") +
                            "deg, BindDeltaAngle=" +
                            Quaternion.Angle(Quaternion.identity, bindDelta).ToString("F4") +
                            "deg, TargetNodeRestVsBind=" +
                            Quaternion.Angle(targetNodeRest, targetBind.Value).ToString("F4") +
                            "deg, SourceNodeRestVsBind=" +
                            Quaternion.Angle(sourceNodeRest, sourceBind.Value).ToString("F4") + "deg");
                    }
                }

                report.AppendLine();
                report.AppendLine("[Scene instance pose vs its own prefab rest]");
                if (targetPrefab != null)
                {
                    foreach (var name in chain)
                    {
                        var prefabBone = RequireBone(targetPrefab.transform, name);
                        var sceneBone = RequireBone(model, name);
                        report.AppendLine(
                            name + ": SceneVsPrefabLocalAngle=" +
                            Quaternion.Angle(prefabBone.localRotation, sceneBone.localRotation)
                                .ToString("F4") + "deg");
                    }
                }

                report.AppendLine();
                report.AppendLine("[Rest basis delta and child axis preservation]");
                report.AppendLine(
                    "Delta = inverse(sourceModelSpaceRest) * targetModelSpaceRest. " +
                    "ChildAxisDeviation near zero means the delta is a pure roll about the bone " +
                    "axis, so applying it cannot move any joint.");
                for (var index = 0; index < chain.Length; index++)
                {
                    var name = chain[index];
                    var sourceBone = RequireBone(sourceModel, name);
                    var targetBone = RequireBone(model, name);
                    var sourceSpace = Quaternion.Inverse(sourceModel.rotation) * sourceBone.rotation;
                    var targetSpace = Quaternion.Inverse(model.rotation) * targetBone.rotation;
                    var delta = Quaternion.Inverse(sourceSpace) * targetSpace;
                    var line = name +
                               ": DeltaAngle=" + Quaternion.Angle(Quaternion.identity, delta)
                                   .ToString("F4") + "deg";
                    if (index + 1 < chain.Length)
                    {
                        var childAxis = RequireBone(targetBone, chain[index + 1]).localPosition
                            .normalized;
                        var sourceChildAxis = RequireBone(sourceBone, chain[index + 1])
                            .localPosition.normalized;
                        line += ", ChildLocalOffsetMatch=" +
                                Vector3.Angle(childAxis, sourceChildAxis).ToString("F4") + "deg" +
                                ", ChildAxisDeviation=" +
                                Vector3.Angle(childAxis, delta * childAxis).ToString("F4") + "deg";
                        maxAxisDeviation = Mathf.Max(
                            maxAxisDeviation, Vector3.Angle(childAxis, delta * childAxis));
                    }

                    report.AppendLine(line);
                }

                report.AppendLine();
                report.AppendLine("[Loop clip rotation curves vs raw source clip]");
                foreach (var name in chain)
                {
                    var path = AnimationUtility.CalculateTransformPath(
                        RequireBone(model, name), model);
                    var identical = true;
                    foreach (var property in new[]
                             {
                                 "m_LocalRotation.x", "m_LocalRotation.y",
                                 "m_LocalRotation.z", "m_LocalRotation.w"
                             })
                    {
                        var binding = EditorCurveBinding.FloatCurve(
                            path, typeof(Transform), property);
                        identical &= CurvesMatch(
                            AnimationUtility.GetEditorCurve(sourceClip, binding),
                            AnimationUtility.GetEditorCurve(loopClip, binding));
                    }
                    report.AppendLine(name + ": Path=" + path + ", MatchesRawSource=" + identical);
                }

                report.AppendLine();
                report.AppendLine("[Model-space limb direction: source motion vs current loop]");
                var frameCount = Mathf.Max(
                    1, Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                var segments = new[]
                {
                    new[] { "RightShoulder", "RightArm" },
                    new[] { "RightArm", "RightForeArm" },
                    new[] { "RightForeArm", "RightHand" }
                };
                var worst = new float[segments.Length];
                AnimationMode.StartAnimationMode();
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var time = sourceClip.length * frame / frameCount;
                    Restore(sourceStates);
                    AnimationMode.SampleAnimationClip(sourceObject, sourceClip, time);
                    var sourceDirections = segments
                        .Select(pair => Direction(sourceModel, sourceModel, pair[0], pair[1]))
                        .ToArray();
                    Restore(targetStates);
                    AnimationMode.SampleAnimationClip(model.gameObject, loopClip, time);
                    for (var index = 0; index < segments.Length; index++)
                    {
                        var targetDirection = Direction(
                            model, model, segments[index][0], segments[index][1]);
                        var angle = Vector3.Angle(sourceDirections[index], targetDirection);
                        worst[index] = Mathf.Max(worst[index], angle);
                    }
                }

                for (var index = 0; index < segments.Length; index++)
                {
                    maxDirection = Mathf.Max(maxDirection, worst[index]);
                    report.AppendLine(
                        segments[index][0] + "->" + segments[index][1] +
                        ": MaxDirectionAngle=" + worst[index].ToString("F4") + "deg");
                }

                report.AppendLine();
                report.AppendLine("[Axial roll carried by the source motion, right vs left]");
                report.AppendLine(
                    "Roll is the twist of each bone about its own child axis, measured against the " +
                    "rest orientation carried down the chain. The intact left arm is the reference.");
                var rollChains = new[]
                {
                    new[] { "Spine", "RightShoulder", "RightArm", "RightForeArm", "RightHand" },
                    new[] { "Spine", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" }
                };
                var bindSpace = rollChains.SelectMany(item => item).Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        name => name,
                        name => ToModelSpaceBind(model, targetSkin, name),
                        StringComparer.Ordinal);
                var rollPeak = new Dictionary<string, float>(StringComparer.Ordinal);
                var loopRollPeak = new Dictionary<string, float>(StringComparer.Ordinal);
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var time = sourceClip.length * frame / frameCount;
                    Restore(sourceStates);
                    AnimationMode.SampleAnimationClip(sourceObject, sourceClip, time);
                    foreach (var rollChain in rollChains)
                        AccumulateRoll(sourceModel, rollChain, rollPeak, bindSpace);
                    Restore(targetStates);
                    AnimationMode.SampleAnimationClip(model.gameObject, loopClip, time);
                    foreach (var rollChain in rollChains)
                        AccumulateRoll(model, rollChain, loopRollPeak, bindSpace);
                }

                foreach (var entry in rollPeak.OrderBy(item => item.Key, StringComparer.Ordinal))
                    report.AppendLine(
                        entry.Key +
                        ": RawSourceMaxAxialRoll=" + entry.Value.ToString("F4") + "deg" +
                        ", CurrentLoopMaxAxialRoll=" +
                        (loopRollPeak.TryGetValue(entry.Key, out var current)
                            ? current.ToString("F4")
                            : "n/a") + "deg");
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(sourceStates);
                Restore(targetStates);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            var absolute = Absolute(BasisDiagnosisPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "The right-arm basis diagnosis must not dirty CargoRunMvp.");
            Debug.Log(
                "Ispant06RightArmRestBasisDiagnosisWritten" +
                ", MaxRightChainLocalRestAngle=" + maxLocalRest.ToString("F4") +
                ", MaxLimbDirectionAngle=" + maxDirection.ToString("F4") +
                ", MaxChildAxisDeviation=" + maxAxisDeviation.ToString("F4") +
                ", SceneDirty=" + scene.isDirty +
                ", Report=" + BasisDiagnosisPath + ".");
        }

        // Finds geometry that is welded into the main body but skinned to the left arm while
        // sitting far away from it. Such vertices stretch from the waist to the arm as soon as the
        // arm moves, which is what is left of the sheathed sword on the left hip.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Left Arm Stretch")]
        public static void InspectIspant06LeftArmStretch()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = body.sharedMesh;
            var chain = new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };
            var chainIndices = chain
                .Select(name => Array.FindIndex(
                    body.bones, item => item != null && item.name == name))
                .ToArray();
            if (chainIndices.Any(index => index < 0))
                throw new InvalidOperationException(
                    "The slot-6 body is not skinned to the whole left arm chain.");
            var bindposes = mesh.bindposes;
            var chainPoints = chainIndices
                .Select(index => (Vector3)bindposes[index].inverse.GetColumn(3))
                .ToArray();
            var armSet = new HashSet<int>(chainIndices);
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var distances = new List<(int Index, float Distance)>();
            for (var index = 0; index < vertices.Length; index++)
            {
                if (!TouchesBones(weights[index], armSet)) continue;
                var distance = float.PositiveInfinity;
                for (var segment = 0; segment + 1 < chainPoints.Length; segment++)
                    distance = Mathf.Min(
                        distance,
                        DistanceToSegment(
                            vertices[index], chainPoints[segment], chainPoints[segment + 1]));
                distances.Add((index, distance));
            }

            var report = new StringBuilder();
            report.AppendLine("Ispant06LeftArmStretchInspection");
            report.AppendLine("BodyMesh=" + AssetDatabase.GetAssetPath(mesh));
            report.AppendLine("Vertices=" + mesh.vertexCount +
                              ", Triangles=" + (mesh.triangles.Length / 3));
            report.AppendLine("LeftArmWeightedVertices=" + distances.Count);
            report.AppendLine();
            report.AppendLine("[Bind pose distance from the left arm bone chain]");
            var buckets = new[] { 0.05f, 0.1f, 0.15f, 0.2f, 0.3f, 0.5f, 1f, float.PositiveInfinity };
            var previous = 0f;
            foreach (var bucket in buckets)
            {
                var count = distances.Count(item => item.Distance > previous &&
                                                    item.Distance <= bucket);
                report.AppendLine(
                    previous.ToString("F2") + " - " +
                    (float.IsInfinity(bucket) ? "inf" : bucket.ToString("F2")) +
                    "m : " + count);
                previous = bucket;
            }

            var far = distances.Where(item => item.Distance > 0.2f).ToArray();
            report.AppendLine();
            report.AppendLine("[Vertices further than 0.20m from the left arm]");
            report.AppendLine("Count=" + far.Length);
            if (far.Length > 0)
            {
                var centre = far.Aggregate(
                    Vector3.zero, (sum, item) => sum + vertices[item.Index]) / far.Length;
                var localCentre = model.InverseTransformPoint(
                    body.transform.TransformPoint(centre));
                var hipsLocal = model.InverseTransformPoint(RequireBone(model, "Hips").position);
                report.AppendLine(
                    "ModelSpaceCentre=(" + localCentre.x.ToString("F4") + ", " +
                    localCentre.y.ToString("F4") + ", " + localCentre.z.ToString("F4") + ")" +
                    ", HipsModelSpaceY=" + hipsLocal.y.ToString("F4") +
                    ", MaxDistance=" + far.Max(item => item.Distance).ToString("F4") + "m");
                var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var item in far)
                {
                    var index = weights[item.Index].boneIndex0;
                    var name = index >= 0 && index < body.bones.Length && body.bones[index] != null
                        ? body.bones[index].name
                        : "none";
                    dominant[name] = dominant.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                report.AppendLine("DominantBones=" + string.Join(
                    "|",
                    dominant.OrderByDescending(item => item.Value)
                        .Select(item => item.Key + ":" + item.Value)));
            }

            report.AppendLine();
            report.AppendLine("[Triangles that stretch during the loop]");
            var stretched = StretchRatios(model, body, out var bindEdge, out var peakEdge);
            var stretchBuckets = new[] { 1.2f, 1.5f, 2f, 3f, 5f, float.PositiveInfinity };
            var previousRatio = 1f;
            foreach (var bucket in stretchBuckets)
            {
                var count = stretched.Count(item => item > previousRatio && item <= bucket);
                report.AppendLine(
                    previousRatio.ToString("F2") + " - " +
                    (float.IsInfinity(bucket) ? "inf" : bucket.ToString("F2")) +
                    "x : " + count);
                previousRatio = bucket;
            }

            var triangles = mesh.triangles;
            var badTriangles = Enumerable.Range(0, stretched.Length)
                .Where(index => stretched[index] > 2f)
                .ToArray();
            var badVertices = new HashSet<int>();
            foreach (var index in badTriangles)
            {
                badVertices.Add(triangles[index * 3]);
                badVertices.Add(triangles[index * 3 + 1]);
                badVertices.Add(triangles[index * 3 + 2]);
            }

            report.AppendLine("TrianglesOver2x=" + badTriangles.Length +
                              ", VerticesInvolved=" + badVertices.Count);
            if (badVertices.Count > 0)
            {
                var centre = badVertices.Aggregate(
                    Vector3.zero, (sum, index) => sum + vertices[index]) / badVertices.Count;
                var localCentre = model.InverseTransformPoint(
                    body.transform.TransformPoint(centre));
                report.AppendLine(
                    "BindCentreModelSpace=(" + localCentre.x.ToString("F4") + ", " +
                    localCentre.y.ToString("F4") + ", " + localCentre.z.ToString("F4") + ")" +
                    ", MaxRatio=" + stretched.Max().ToString("F3") +
                    ", MaxBindEdge=" + bindEdge.ToString("F4") +
                    "m, MaxPeakEdge=" + peakEdge.ToString("F4") + "m");
                var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var index in badVertices)
                {
                    var bone = weights[index].boneIndex0;
                    var name = bone >= 0 && bone < body.bones.Length && body.bones[bone] != null
                        ? body.bones[bone].name
                        : "none";
                    dominant[name] = dominant.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                report.AppendLine("DominantBones=" + string.Join(
                    "|",
                    dominant.OrderByDescending(item => item.Value)
                        .Select(item => item.Key + ":" + item.Value)));
            }

            report.AppendLine();
            report.AppendLine("[Removal candidates: dominant left arm weight and far from the arm]");
            var candidates = LeftArmForeignVertices(model, body);
            report.AppendLine("Count=" + candidates.Count);
            if (candidates.Count > 0)
            {
                var centre = candidates.Aggregate(
                    Vector3.zero, (sum, index) => sum + vertices[index]) / candidates.Count;
                var localCentre = model.InverseTransformPoint(
                    body.transform.TransformPoint(centre));
                var covered = badTriangles.Count(index =>
                    candidates.Contains(triangles[index * 3]) ||
                    candidates.Contains(triangles[index * 3 + 1]) ||
                    candidates.Contains(triangles[index * 3 + 2]));
                var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var index in candidates)
                {
                    var bone = weights[index].boneIndex0;
                    var name = bone >= 0 && bone < body.bones.Length && body.bones[bone] != null
                        ? body.bones[bone].name
                        : "none";
                    dominant[name] = dominant.TryGetValue(name, out var value) ? value + 1 : 1;
                }

                report.AppendLine(
                    "BindCentreModelSpace=(" + localCentre.x.ToString("F4") + ", " +
                    localCentre.y.ToString("F4") + ", " + localCentre.z.ToString("F4") + ")" +
                    ", StretchedTrianglesCovered=" + covered + " of " + badTriangles.Length);
                report.AppendLine("DominantBones=" + string.Join(
                    "|",
                    dominant.OrderByDescending(item => item.Value)
                        .Select(item => item.Key + ":" + item.Value)));
            }

            report.AppendLine();
            report.AppendLine("[Remaining islands below the hips]");
            var islands = ConnectedIslands(mesh);
            var largest = islands.OrderByDescending(item => item.Count).First();
            var hipsY = model.InverseTransformPoint(RequireBone(model, "Hips").position).y;
            foreach (var island in islands.Where(item => item != largest)
                         .OrderByDescending(item => item.Count).Take(10))
            {
                var centre = island.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) /
                             island.Count;
                var localCentre = model.InverseTransformPoint(
                    body.transform.TransformPoint(centre));
                if (localCentre.y >= hipsY) continue;
                report.AppendLine(
                    "Island vertices=" + island.Count +
                    ", ModelSpaceCentre=(" + localCentre.x.ToString("F4") + ", " +
                    localCentre.y.ToString("F4") + ", " + localCentre.z.ToString("F4") + ")");
            }

            var absolute = Absolute(LeftArmStretchDiagnosisPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The left arm stretch inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06LeftArmStretchInspected" +
                ", LeftArmWeightedVertices=" + distances.Count +
                ", FurtherThan20cm=" + far.Length +
                ", Report=" + LeftArmStretchDiagnosisPath + ".");
        }

        // Removes the leftover sheathed hilt on the Ispant left hip: the small separate pieces that
        // sit on the negative model X side at or below hip height. The main shell and anything
        // large enough to be real armour are left alone.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Waist Remnant Removal")]
        public static void ApplyIspant06WaistRemnantRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var islands = ConnectedIslands(source);
            var largest = islands.OrderByDescending(item => item.Count).First();
            var vertices = source.vertices;
            var hips = model.InverseTransformPoint(RequireBone(model, "Hips").position);
            var removed = new HashSet<int>();
            var picked = new List<string>();
            foreach (var island in islands.Where(item => item != largest && item.Count <= 200))
            {
                var centre = island
                    .Select(index => model.InverseTransformPoint(
                        body.transform.TransformPoint(vertices[index])))
                    .Aggregate(Vector3.zero, (sum, item) => sum + item) / island.Count;
                if (centre.x >= hips.x) continue;
                if (centre.y > hips.y + 0.05f) continue;
                foreach (var index in island) removed.Add(index);
                picked.Add(
                    "vertices=" + island.Count +
                    " centre=(" + centre.x.ToString("F4") + ", " + centre.y.ToString("F4") +
                    ", " + centre.z.ToString("F4") + ")");
            }

            if (removed.Count == 0)
                throw new InvalidOperationException(
                    "No leftover hilt piece was found on the Ispant left hip.");
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            ReplaceBodyMesh(
                body,
                BuildDerivedMesh(source, removed, null, 0),
                WaistRemnantRemovedMeshPath,
                "Ispant_06_BodyWaistRemnantRemoved");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the waist remnant removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06WaistRemnantRemoved" +
                ", Pieces=" + picked.Count +
                ", Detail=" + string.Join(" ; ", picked) +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", RemovedTriangles=" + (beforeTriangles - applied.triangles.Length / 3) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", BindPosesPreserved=" +
                (applied.bindposes.Length == source.bindposes.Length) +
                ", Mesh=" + WaistRemnantRemovedMeshPath + ".");
        }

        // Lists every renderer under slot 6 with its enabled flag and mesh, so a stray weapon
        // renderer cannot hide behind a name nobody checked.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Renderers")]
        public static void InspectIspant06Renderers()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var report = new StringBuilder();
            report.AppendLine("Ispant06RendererInspection");
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = renderer is SkinnedMeshRenderer skinned
                    ? skinned.sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                report.AppendLine(
                    "name=" + renderer.name +
                    ", type=" + renderer.GetType().Name +
                    ", enabled=" + renderer.enabled +
                    ", activeSelf=" + renderer.gameObject.activeSelf +
                    ", activeInHierarchy=" + renderer.gameObject.activeInHierarchy +
                    ", path=" + AnimationUtility.CalculateTransformPath(renderer.transform, model) +
                    ", mesh=" + (mesh == null ? "none" : mesh.name) +
                    ", meshAsset=" + (mesh == null ? "none" : AssetDatabase.GetAssetPath(mesh)));
            }

            var absolute = Absolute(RendererInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The renderer inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06RenderersInspected" +
                ", Count=" + model.GetComponentsInChildren<Renderer>(true).Length +
                ", Report=" + RendererInspectionPath + ".");
        }

        // Puts the slot-6 weapon renderers back to the intended state. The capture helpers used to
        // re-enable every renderer they had hidden, which switched on the waist sword and the hand
        // musket that were deliberately off, and a later scene save made that permanent.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Restore Weapon Visibility")]
        public static void ApplyIspant06RestoreWeaponVisibility()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var wanted = new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                { HandSwordName, true },
                { BackMusketName, true },
                { WaistSwordName, false },
                { HandMusketRendererName, false },
                // The direct model ships its own sword under the Armature. It hangs on the left
                // waist by default and must stay hidden, since the slot uses the hand sword.
                { ModelBuiltInSwordName, false }
            };
            var report = new List<string>();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!wanted.TryGetValue(renderer.name, out var enabled)) continue;
                report.Add(renderer.name + ": " + renderer.enabled + " -> " + enabled);
                renderer.enabled = enabled;
                EditorUtility.SetDirty(renderer);
            }

            if (report.Count != wanted.Count)
                throw new InvalidOperationException(
                    "Expected " + wanted.Count + " slot-6 weapon renderers but found " +
                    report.Count + ".");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after restoring the weapon visibility.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06WeaponVisibilityRestored, " + string.Join(" ; ", report) + ".");
        }

        // Picks geometry the way a person does: a ray is cast through one pixel of the same render
        // the marked screenshot came from, the triangle it hits becomes the seed, and the selection
        // grows across neighbouring triangles while the surface stays flat enough to be one part.
        // The result is painted red and rendered, so it can be compared before anything is removed.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Picked Part Highlight")]
        public static void CaptureIspant06PickedPartHighlight()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var selection = PickedPartVertices(model, body, out var pickReport);
            RenderHighlight(model, body, selection, PickedPartHighlightPath);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The picked part highlight changed the scene dirty state.");
            Debug.Log(
                "Ispant06PickedPartHighlightCaptured" +
                ", " + pickReport +
                ", Image=" + PickedPartHighlightPath +
                ", SceneChanged=False.");
        }

        // Removes the part the ray pick selected, once the highlight render has confirmed it.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Picked Part Removal")]
        public static void ApplyIspant06PickedPartRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var selection = PickedPartVertices(model, body, out var pickReport);
            if (selection.Count == 0)
                throw new InvalidOperationException("The ray pick selected nothing to remove.");
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            ReplaceBodyMesh(
                body,
                BuildDerivedMesh(source, selection, null, 0),
                PickedPartRemovedMeshPath,
                "Ispant_06_BodyPickedPartRemoved");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the picked part removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06PickedPartRemoved" +
                ", " + pickReport +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", RemovedTriangles=" + (beforeTriangles - applied.triangles.Length / 3) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", Mesh=" + PickedPartRemovedMeshPath + ".");
        }

        // The pixel that lands on the marked wedge in the left front quarter view of the closeup.
        private const int PickPanel = 560;
        private const int PickPixelX = 263;
        private const int PickPixelY = 263;
        private const float PickFlatAngle = 45f;
        private const int PickSweepSteps = 3;
        private const int PickSweepStride = 7;
        private const int PickMaxPartTriangles = 28;

        private static HashSet<int> PickedPartVertices(
            Transform model,
            SkinnedMeshRenderer body,
            out string report)
        {
            var mesh = body.sharedMesh;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var hips = RequireBone(model, "Hips");
            var leftUpLeg = RequireBone(model, "LeftUpLeg");
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item.enabled).ToArray();
            var baked = new Mesh();
            var selection = new HashSet<int>();
            var hitTriangle = -1;
            var hitPoint = Vector3.zero;
            var partTriangles = 0;
            try
            {
                AnimationMode.StartAnimationMode();
                Restore(transforms);
                AnimationMode.SampleAnimationClip(model.gameObject, clip, clip.length * 0.5f);
                foreach (var item in hidden) item.enabled = false;
                body.BakeMesh(baked);
                var world = baked.vertices
                    .Select(item => body.transform.TransformPoint(item)).ToArray();
                var centre = Vector3.Lerp(hips.position, leftUpLeg.position, 0.35f) -
                             model.right * 0.22f;
                var direction = (-model.right - model.forward).normalized;
                var eye = centre + direction * 2.1f + model.up * 0.08f;
                var rotation = Quaternion.LookRotation(centre - eye, model.up);
                // Rebuild the same perspective ray the render used for that pixel.
                var half = Mathf.Tan(26f * 0.5f * Mathf.Deg2Rad);
                var nx = (PickPixelX + 0.5f) / PickPanel * 2f - 1f;
                var ny = (PickPixelY + 0.5f) / PickPanel * 2f - 1f;
                var ray = rotation * new Vector3(nx * half, ny * half, 1f).normalized;
                var triangles = mesh.triangles;
                {
                    var normals = new Vector3[triangles.Length / 3];
                    for (var index = 0; index < normals.Length; index++)
                        normals[index] = Vector3.Cross(
                                world[triangles[index * 3 + 1]] - world[triangles[index * 3]],
                                world[triangles[index * 3 + 2]] - world[triangles[index * 3]])
                            .normalized;
                    var byVertex = new Dictionary<int, List<int>>();
                    for (var index = 0; index < normals.Length; index++)
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var vertex = triangles[index * 3 + corner];
                        if (!byVertex.TryGetValue(vertex, out var list))
                        {
                            list = new List<int>();
                            byVertex.Add(vertex, list);
                        }

                        list.Add(index);
                    }

                    // Sweep a grid of rays over the marked area. Each hit grows into its own
                    // flat part; only small parts are kept, because the leftover shards are small
                    // while real armour panels are large connected patches.
                    var accepted = new HashSet<int>();
                    for (var gx = -PickSweepSteps; gx <= PickSweepSteps; gx++)
                    for (var gy = -PickSweepSteps; gy <= PickSweepSteps; gy++)
                    {
                        var px = PickPixelX + gx * PickSweepStride;
                        var py = PickPixelY + gy * PickSweepStride;
                        if (px < 0 || px >= PickPanel || py < 0 || py >= PickPanel) continue;
                        var nxs = (px + 0.5f) / PickPanel * 2f - 1f;
                        var nys = (py + 0.5f) / PickPanel * 2f - 1f;
                        var sweep = rotation * new Vector3(nxs * half, nys * half, 1f).normalized;
                        var best = float.PositiveInfinity;
                        var hit = -1;
                        for (var index = 0; index + 2 < triangles.Length; index += 3)
                        {
                            if (!RayHitsTriangle(
                                    eye, sweep,
                                    world[triangles[index]],
                                    world[triangles[index + 1]],
                                    world[triangles[index + 2]],
                                    out var distance)) continue;
                            if (distance >= best) continue;
                            best = distance;
                            hit = index / 3;
                        }

                        if (hit < 0 || accepted.Contains(hit)) continue;
                        if (hitTriangle < 0)
                        {
                            hitTriangle = hit;
                            hitPoint = eye + sweep * best;
                        }

                        var visited = new HashSet<int> { hit };
                        var queue = new Queue<int>();
                        queue.Enqueue(hit);
                        while (queue.Count > 0 && visited.Count <= PickMaxPartTriangles)
                        {
                            var current = queue.Dequeue();
                            for (var corner = 0; corner < 3; corner++)
                            {
                                var vertex = triangles[current * 3 + corner];
                                if (!byVertex.TryGetValue(vertex, out var list)) continue;
                                foreach (var neighbour in list)
                                {
                                    if (visited.Contains(neighbour)) continue;
                                    if (Vector3.Angle(normals[current], normals[neighbour]) >
                                        PickFlatAngle) continue;
                                    visited.Add(neighbour);
                                    queue.Enqueue(neighbour);
                                }
                            }
                        }

                        if (visited.Count > PickMaxPartTriangles) continue;
                        foreach (var index in visited) accepted.Add(index);
                    }

                    partTriangles = accepted.Count;
                    foreach (var index in accepted)
                    for (var corner = 0; corner < 3; corner++)
                        selection.Add(triangles[index * 3 + corner]);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                UnityEngine.Object.DestroyImmediate(baked);
            }

            var local = hitTriangle < 0
                ? Vector3.zero
                : model.InverseTransformPoint(hitPoint);
            report = "HitTriangle=" + hitTriangle +
                     ", HitModelSpace=(" + local.x.ToString("F4") + ", " +
                     local.y.ToString("F4") + ", " + local.z.ToString("F4") + ")" +
                     ", PartTriangles=" + partTriangles +
                     ", PartVertices=" + selection.Count;
            return selection;
        }

        private static bool RayHitsTriangle(
            Vector3 origin,
            Vector3 direction,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            out float distance)
        {
            distance = 0f;
            var edge1 = b - a;
            var edge2 = c - a;
            var h = Vector3.Cross(direction, edge2);
            var determinant = Vector3.Dot(edge1, h);
            if (Mathf.Abs(determinant) < 1e-8f) return false;
            var inverse = 1f / determinant;
            var s = origin - a;
            var u = inverse * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;
            var q = Vector3.Cross(s, edge1);
            var v = inverse * Vector3.Dot(direction, q);
            if (v < 0f || u + v > 1f) return false;
            distance = inverse * Vector3.Dot(edge2, q);
            return distance > 0.0001f;
        }

        private static void RenderHighlight(
            Transform model,
            SkinnedMeshRenderer body,
            ICollection<int> selection,
            string outputPath)
        {
            var mesh = body.sharedMesh;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            var hips = RequireBone(model, "Hips");
            var leftUpLeg = RequireBone(model, "LeftUpLeg");
            const int panel = 560;
            const int columns = 4;
            const int captureLayer = 30;
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06HighlightCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 26f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var highlightObject = new GameObject("Ispant06Highlight")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            highlightObject.transform.SetParent(body.transform, false);
            var highlightFilter = highlightObject.AddComponent<MeshFilter>();
            var highlightRenderer = highlightObject.AddComponent<MeshRenderer>();
            var highlightMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.05f, 0.05f, 1f),
                hideFlags = HideFlags.HideAndDontSave
            };
            highlightRenderer.sharedMaterial = highlightMaterial;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item != highlightRenderer && item.enabled).ToArray();
            var baked = new Mesh();
            var highlightMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            try
            {
                AnimationMode.StartAnimationMode();
                Restore(transforms);
                AnimationMode.SampleAnimationClip(model.gameObject, clip, clip.length * 0.5f);
                foreach (var item in hidden) item.enabled = false;
                body.BakeMesh(baked);
                BuildHighlightMesh(baked, mesh.triangles, selection, highlightMesh);
                highlightFilter.sharedMesh = highlightMesh;
                var centre = Vector3.Lerp(hips.position, leftUpLeg.position, 0.35f) -
                             model.right * 0.22f;
                var directions = new[]
                {
                    -model.right, (-model.right - model.forward).normalized,
                    -model.forward, (-model.right + model.forward).normalized
                };
                for (var column = 0; column < columns; column++)
                {
                    camera.transform.position = centre + directions[column] * 2.1f +
                                                model.up * 0.08f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        centre - camera.transform.position, model.up);
                    RenderIntoSheet(camera, target, buffer, sheet, column, 0, panel);
                }

                sheet.Apply();
                var destination = Absolute(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(highlightMesh);
                UnityEngine.Object.DestroyImmediate(highlightObject);
                UnityEngine.Object.DestroyImmediate(highlightMaterial);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        // Paints candidate selections bright red on top of the posed body and renders them from the
        // same angles as the marked screenshot. Nothing is removed here: the point is to see which
        // candidate actually covers the leftover hilt before touching the mesh.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Selection Highlight")]
        public static void CaptureIspant06SelectionHighlight()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = body.sharedMesh;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var candidates = SelectionCandidates(model, body, out var summary);
            var hips = RequireBone(model, "Hips");
            var leftUpLeg = RequireBone(model, "LeftUpLeg");
            const int panel = 560;
            const int columns = 4;
            const int captureLayer = 30;
            var rows = candidates.Count;
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06SelectionHighlightCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 26f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var highlightObject = new GameObject("Ispant06SelectionHighlight")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            highlightObject.transform.SetParent(body.transform, false);
            var highlightFilter = highlightObject.AddComponent<MeshFilter>();
            var highlightRenderer = highlightObject.AddComponent<MeshRenderer>();
            var highlightMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.05f, 0.05f, 1f),
                hideFlags = HideFlags.HideAndDontSave
            };
            highlightRenderer.sharedMaterial = highlightMaterial;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item != highlightRenderer && item.enabled)
                .ToArray();
            var baked = new Mesh();
            var highlightMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            try
            {
                AnimationMode.StartAnimationMode();
                for (var row = 0; row < rows; row++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, clip.length * 0.5f);
                    foreach (var item in hidden) item.enabled = false;
                    body.BakeMesh(baked);
                    BuildHighlightMesh(baked, mesh.triangles, candidates[row].Vertices, highlightMesh);
                    highlightFilter.sharedMesh = highlightMesh;
                    var centre = Vector3.Lerp(hips.position, leftUpLeg.position, 0.35f) -
                                 model.right * 0.22f;
                    var directions = new[]
                    {
                        -model.right, (-model.right - model.forward).normalized,
                        -model.forward, (-model.right + model.forward).normalized
                    };
                    for (var column = 0; column < columns; column++)
                    {
                        camera.transform.position = centre + directions[column] * 2.1f +
                                                    model.up * 0.08f;
                        camera.transform.rotation = Quaternion.LookRotation(
                            centre - camera.transform.position, model.up);
                        RenderIntoSheet(
                            camera, target, buffer, sheet, column, rows - 1 - row, panel);
                    }
                }

                sheet.Apply();
                var destination = Absolute(SelectionHighlightPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(highlightMesh);
                UnityEngine.Object.DestroyImmediate(highlightObject);
                UnityEngine.Object.DestroyImmediate(highlightMaterial);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The selection highlight changed the scene dirty state.");
            Debug.Log(
                "Ispant06SelectionHighlightCaptured" +
                ", Rows=" + rows +
                ", RowOrderTopToBottom=" + string.Join(
                    "|", Enumerable.Reverse(candidates).Select(item => item.Name)) +
                ", " + summary +
                ", Image=" + SelectionHighlightPath +
                ", SceneChanged=False.");
        }

        private static void BuildHighlightMesh(
            Mesh baked,
            IReadOnlyList<int> triangles,
            ICollection<int> selection,
            Mesh destination)
        {
            var vertices = baked.vertices;
            var kept = new List<int>();
            for (var index = 0; index + 2 < triangles.Count; index += 3)
            {
                if (!selection.Contains(triangles[index]) &&
                    !selection.Contains(triangles[index + 1]) &&
                    !selection.Contains(triangles[index + 2])) continue;
                kept.Add(triangles[index]);
                kept.Add(triangles[index + 1]);
                kept.Add(triangles[index + 2]);
            }

            destination.Clear();
            destination.SetVertices(vertices);
            destination.SetTriangles(kept, 0);
            destination.RecalculateNormals();
            destination.RecalculateBounds();
        }

        private static List<(string Name, HashSet<int> Vertices)> SelectionCandidates(
            Transform model,
            SkinnedMeshRenderer body,
            out string summary)
        {
            var mesh = body.sharedMesh;
            var values = new List<(string, HashSet<int>)>();
            var asymmetry = HipAsymmetryVertices(model, body, out _);
            values.Add(("BeltHeightAsymmetry", asymmetry));
            var adjacency = VertexAdjacency(mesh);
            var expanded = new HashSet<int>(asymmetry);
            var queue = new Queue<int>(asymmetry);
            var hips = model.InverseTransformPoint(RequireBone(model, "Hips").position);
            var local = mesh.vertices
                .Select(item => model.InverseTransformPoint(body.transform.TransformPoint(item)))
                .ToArray();
            while (queue.Count > 0)
            {
                var vertex = queue.Dequeue();
                if (!adjacency.TryGetValue(vertex, out var neighbours)) continue;
                foreach (var neighbour in neighbours)
                {
                    if (expanded.Contains(neighbour)) continue;
                    if (local[neighbour].x >= hips.x - 0.10f) continue;
                    if (Mathf.Abs(local[neighbour].y - hips.y) > 0.30f) continue;
                    expanded.Add(neighbour);
                    queue.Enqueue(neighbour);
                }
            }

            values.Add(("AsymmetryFloodExpanded", expanded));
            var submeshes = new HashSet<int>();
            if (mesh.subMeshCount > 1)
                foreach (var index in mesh.GetTriangles(mesh.subMeshCount - 1))
                    submeshes.Add(index);
            values.Add(("LastSubmesh", submeshes));
            summary = "SubMeshCount=" + mesh.subMeshCount +
                      ", MaterialCount=" + body.sharedMaterials.Length +
                      ", AsymmetryVertices=" + asymmetry.Count +
                      ", ExpandedVertices=" + expanded.Count +
                      ", LastSubmeshVertices=" + submeshes.Count;
            return values.Select(item => (item.Item1, item.Item2)).ToList();
        }

        // The Ispant body is built symmetric, so geometry around the hips that has no mirrored
        // counterpart on the other side is leftover kit such as a sheathed sword. This finds those
        // unmatched vertices and reports them in clusters.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Hip Asymmetry")]
        public static void InspectIspant06HipAsymmetry()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var unmatched = HipAsymmetryVertices(model, body, out var report);
            var absolute = Absolute(HipAsymmetryPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report, new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The hip asymmetry inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06HipAsymmetryInspected" +
                ", UnmatchedVertices=" + unmatched.Count +
                ", Report=" + HipAsymmetryPath + ".");
        }

        // Removes the unmatched hip geometry that the asymmetry check finds on the Ispant left side.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Hip Asymmetry Removal")]
        public static void ApplyIspant06HipAsymmetryRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var unmatched = HipAsymmetryVertices(model, body, out _);
            if (unmatched.Count == 0)
                throw new InvalidOperationException("No unmatched hip geometry was found.");
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            ReplaceBodyMesh(
                body,
                BuildDerivedMesh(source, unmatched, null, 0),
                HipAsymmetryRemovedMeshPath,
                "Ispant_06_BodyHipAsymmetryRemoved");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the hip asymmetry removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06HipAsymmetryRemoved" +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", RemovedTriangles=" + (beforeTriangles - applied.triangles.Length / 3) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", BindPosesPreserved=" +
                (applied.bindposes.Length == source.bindposes.Length) +
                ", Mesh=" + HipAsymmetryRemovedMeshPath + ".");
        }

        private static HashSet<int> HipAsymmetryVertices(
            Transform model,
            SkinnedMeshRenderer body,
            out string report)
        {
            var mesh = body.sharedMesh;
            var vertices = mesh.vertices;
            var hips = model.InverseTransformPoint(RequireBone(model, "Hips").position);
            var local = vertices
                .Select(item => model.InverseTransformPoint(body.transform.TransformPoint(item)))
                .ToArray();
            // A coarse hash of mirrored positions makes the partner lookup cheap and tolerant.
            var buckets = new Dictionary<Vector3Int, List<int>>();

            Vector3Int Key(Vector3 point) => new Vector3Int(
                Mathf.RoundToInt(point.x / 0.02f),
                Mathf.RoundToInt(point.y / 0.02f),
                Mathf.RoundToInt(point.z / 0.02f));

            for (var index = 0; index < local.Length; index++)
            {
                var key = Key(local[index]);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    buckets.Add(key, list);
                }

                list.Add(index);
            }

            bool HasPartner(Vector3 point)
            {
                var mirrored = new Vector3(2f * hips.x - point.x, point.y, point.z);
                var key = Key(mirrored);
                for (var x = -1; x <= 1; x++)
                for (var y = -1; y <= 1; y++)
                for (var z = -1; z <= 1; z++)
                {
                    if (!buckets.TryGetValue(key + new Vector3Int(x, y, z), out var list)) continue;
                    if (list.Any(index => Vector3.Distance(local[index], mirrored) <= 0.03f))
                        return true;
                }

                return false;
            }

            var unmatched = new HashSet<int>();
            for (var index = 0; index < local.Length; index++)
            {
                var point = local[index];
                if (point.x >= hips.x - 0.14f) continue;
                if (Mathf.Abs(point.y - hips.y) > 0.22f) continue;
                if (HasPartner(point)) continue;
                unmatched.Add(index);
            }

            var builder = new StringBuilder();
            builder.AppendLine("Ispant06HipAsymmetryInspection");
            builder.AppendLine("BodyMesh=" + AssetDatabase.GetAssetPath(mesh));
            builder.AppendLine("Vertices=" + mesh.vertexCount +
                               ", Triangles=" + (mesh.triangles.Length / 3));
            builder.AppendLine("HipsModelSpace=(" + hips.x.ToString("F4") + ", " +
                               hips.y.ToString("F4") + ", " + hips.z.ToString("F4") + ")");
            builder.AppendLine("Window: model X below hips - 0.14, |Y - hipsY| <= 0.22");
            builder.AppendLine("UnmatchedVertices=" + unmatched.Count);
            if (unmatched.Count > 0)
            {
                var centre = unmatched.Aggregate(Vector3.zero, (sum, index) => sum + local[index]) /
                             unmatched.Count;
                var min = unmatched.Select(index => local[index]).Aggregate(Vector3.Min);
                var max = unmatched.Select(index => local[index]).Aggregate(Vector3.Max);
                builder.AppendLine(
                    "Centre=(" + centre.x.ToString("F4") + ", " + centre.y.ToString("F4") +
                    ", " + centre.z.ToString("F4") + ")" +
                    ", Size=(" + (max.x - min.x).ToString("F4") + ", " +
                    (max.y - min.y).ToString("F4") + ", " + (max.z - min.z).ToString("F4") + ")");
                var bones = body.bones;
                var weights = mesh.boneWeights;
                var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var index in unmatched)
                {
                    var bone = weights[index].boneIndex0;
                    var name = bone >= 0 && bone < bones.Length && bones[bone] != null
                        ? bones[bone].name
                        : "none";
                    dominant[name] = dominant.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                builder.AppendLine("DominantBones=" + string.Join(
                    "|",
                    dominant.OrderByDescending(item => item.Value)
                        .Select(item => item.Key + ":" + item.Value)));
            }

            report = builder.ToString();
            return unmatched;
        }

        // Renders the Ispant left hip close up from four angles at the pose where the loop ends, so
        // leftover geometry there can be judged by eye instead of by numbers.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Left Hip Closeup")]
        public static void CaptureIspant06LeftHipCloseup()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var hips = RequireBone(model, "Hips");
            var leftUpLeg = RequireBone(model, "LeftUpLeg");
            var destination = Absolute(LeftHipCloseupPath);
            const int panel = 560;
            const int columns = 4;
            const int rows = 2;
            const int captureLayer = 30;
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06LeftHipCloseupCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 26f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            // Only the skinned body may show. With the sword and the musket hidden, anything that
            // still looks like a hilt on the left hip has to be leftover body geometry.
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item.enabled)
                .ToArray();
            foreach (var item in hidden) item.enabled = false;
            try
            {
                AnimationMode.StartAnimationMode();
                var times = new[] { clip.length, clip.length * 0.5f };
                for (var row = 0; row < rows; row++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, times[row]);
                    // The clip animates the sword renderer enabled flag, so it must be switched
                    // off after sampling or it comes back on every frame.
                    foreach (var item in hidden) item.enabled = false;
                    var centre = Vector3.Lerp(hips.position, leftUpLeg.position, 0.35f) -
                                 model.right * 0.22f;
                    var radius = 2.1f;
                    var directions = new[]
                    {
                        -model.right, (-model.right - model.forward).normalized,
                        -model.forward, (-model.right + model.forward).normalized
                    };
                    for (var column = 0; column < columns; column++)
                    {
                        camera.transform.position = centre + directions[column] * radius +
                                                    model.up * 0.08f;
                        camera.transform.rotation = Quaternion.LookRotation(
                            centre - camera.transform.position, model.up);
                        RenderIntoSheet(
                            camera, target, buffer, sheet, column, rows - 1 - row, panel);
                    }
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The left hip closeup changed the scene dirty state.");
            Debug.Log(
                "Ispant06LeftHipCloseupCaptured" +
                ", TopRow=LoopEndPose, BottomRow=MidLoopPose" +
                ", Angles=LeftSide|LeftFrontQuarter|Front|LeftBackQuarter" +
                ", Image=" + LeftHipCloseupPath +
                ", SceneChanged=False.");
        }

        // Renders the left hip at four loop phases twice from one camera: the top row with the
        // props the scene keeps visible, the bottom row with only the skinned body. The sword the
        // design parks on the left waist disappears from the bottom row, so anything that still
        // reads as a hilt down there is leftover body geometry and nothing else.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Floating Hilt Comparison")]
        public static void CaptureIspant06FloatingHiltComparison()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var hips = RequireBone(model, "Hips");
            var leftUpLeg = RequireBone(model, "LeftUpLeg");
            var destination = Absolute(FloatingHiltComparisonPath);
            const int panel = 640;
            const int columns = 4;
            const int rows = 2;
            const int captureLayer = 30;
            var phases = new[] { 0.3f, 0.7f, 0.88f, 1f };
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06FloatingHiltCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 24f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            // Only renderers that are already on take part. The ones the scene deliberately keeps
            // off stay off, so this capture cannot switch a prop back on the way an earlier bug did.
            var props = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item.enabled)
                .ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                for (var row = 0; row < rows; row++)
                {
                    var showProps = row == 0;
                    for (var column = 0; column < columns; column++)
                    {
                        Restore(transforms);
                        AnimationMode.SampleAnimationClip(
                            model.gameObject, clip, clip.length * phases[column]);
                        // The clip drives the sword renderer enabled flag, so the state this row
                        // wants has to be forced back after every sample.
                        foreach (var item in props) item.enabled = showProps;
                        var centre = Vector3.Lerp(hips.position, leftUpLeg.position, 0.2f) -
                                     model.right * 0.16f;
                        var direction = (-model.right * 0.55f - model.forward).normalized;
                        camera.transform.position = centre + direction * 2.8f + model.up * 0.16f;
                        camera.transform.rotation = Quaternion.LookRotation(
                            centre - camera.transform.position, model.up);
                        RenderIntoSheet(
                            camera, target, buffer, sheet, column, rows - 1 - row, panel);
                    }
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in props) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The floating hilt comparison changed the scene dirty state.");
            Debug.Log(
                "Ispant06FloatingHiltComparisonCaptured" +
                ", TopRow=PropsVisible, BottomRow=BodyOnly" +
                ", Phases=0.30|0.70|0.88|1.00" +
                ", Image=" + FloatingHiltComparisonPath +
                ", SceneChanged=False.");
        }

        // The rays below come from the same camera and the same loop phase as the comparison
        // capture, so a pixel read off that image maps straight onto geometry. The window covers
        // the flakes that float beside the Ispant left belt with the sword still in the right hand,
        // which is the state the user marked up.
        private const int FloatingHiltPanel = 640;
        private const float FloatingHiltFieldOfView = 24f;
        private const float FloatingHiltRadius = 2.8f;
        private const float FloatingHiltPhase = 0.3f;
        // Rays are swept at every phase the comparison capture renders. A flake that hides behind
        // the arm or a plate at one phase stands clear of the body at another, and picking at a
        // single phase is what left debris behind the first time round.
        private static readonly float[] FloatingHiltPhases = { 0.3f, 0.7f, 0.88f, 1f };
        // One aimed window per phase. A single window swept at every phase does not work: the
        // window is calibrated against where the body sits at one phase, so at another phase the
        // same pixels land on healthy armour instead of on debris.
        private struct FloatingHiltSweep
        {
            public float Phase;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
        }

        private static readonly FloatingHiltSweep[] FloatingHiltSweeps =
        {
            new FloatingHiltSweep { Phase = 0.3f, MinX = 248, MaxX = 322, MinY = 252, MaxY = 470 },
            new FloatingHiltSweep { Phase = 0.88f, MinX = 262, MaxX = 334, MinY = 178, MaxY = 356 }
        };

        private const int FloatingHiltStride = 2;
        private const float FloatingHiltFlatAngle = 45f;
        private const int FloatingHiltMaxPartTriangles = 24;
        // A triangle budget alone is not enough: the hip armour panels are low poly, so a real
        // panel also fits inside 24 triangles. The flakes are physically small, the panels are not,
        // so a part is only accepted when its bounding box is small as well.
        // Raising this to 0.22 let three sub-plates of the hip armour through and wiped the whole
        // panel out in preview. The armour is split into flat parts of 0.19 to 0.21, so the cap has
        // to stay under that; leftover debris above it is dealt with by aiming the rays, not by
        // widening the cap.
        private const float FloatingHiltMaxPartSize = 0.17f;

        private static void FloatingHiltCamera(
            Transform model,
            out Vector3 centre,
            out Vector3 eye,
            out Quaternion rotation)
        {
            var hips = RequireBone(model, "Hips");
            var leftUpLeg = RequireBone(model, "LeftUpLeg");
            centre = Vector3.Lerp(hips.position, leftUpLeg.position, 0.2f) - model.right * 0.16f;
            var direction = (-model.right * 0.55f - model.forward).normalized;
            eye = centre + direction * FloatingHiltRadius + model.up * 0.16f;
            rotation = Quaternion.LookRotation(centre - eye, model.up);
        }

        // Sweeps rays over the marked window. Every hit grows into its own flat part and only
        // small parts are kept, because the floating flakes are small while the armour panels
        // around them are large connected patches.
        private static HashSet<int> FloatingHiltVertices(
            Transform model,
            SkinnedMeshRenderer body,
            out string report)
        {
            var mesh = body.sharedMesh;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item.enabled).ToArray();
            var baked = new Mesh();
            var selection = new HashSet<int>();
            var parts = 0;
            var partTriangles = 0;
            var partSizes = new List<string>();
            var rejectedSizes = new List<string>();
            var centreModel = Vector3.zero;
            try
            {
                AnimationMode.StartAnimationMode();
                var half = Mathf.Tan(FloatingHiltFieldOfView * 0.5f * Mathf.Deg2Rad);
                var triangles = mesh.triangles;
                var byVertex = new Dictionary<int, List<int>>();
                for (var index = 0; index + 2 < triangles.Length; index += 3)
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = triangles[index + corner];
                    if (!byVertex.TryGetValue(vertex, out var list))
                    {
                        list = new List<int>();
                        byVertex.Add(vertex, list);
                    }

                    list.Add(index / 3);
                }

                var accepted = new HashSet<int>();
                var world = new Vector3[mesh.vertexCount];
                var normals = new Vector3[triangles.Length / 3];
                foreach (var sweep in FloatingHiltSweeps)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * sweep.Phase);
                    // The clip drives the sword renderer enabled flag, so the props have to be put
                    // back down after sampling or the rays would hit the sword, not the body.
                    foreach (var item in hidden) item.enabled = false;
                    body.BakeMesh(baked);
                    var posed = baked.vertices;
                    for (var index = 0; index < world.Length; index++)
                        world[index] = body.transform.TransformPoint(posed[index]);
                    for (var index = 0; index < normals.Length; index++)
                        normals[index] = Vector3.Cross(
                                world[triangles[index * 3 + 1]] - world[triangles[index * 3]],
                                world[triangles[index * 3 + 2]] - world[triangles[index * 3]])
                            .normalized;
                    FloatingHiltCamera(model, out _, out var eye, out var rotation);
                    SweepFloatingHiltRays(
                        sweep, eye, rotation, half, world, triangles, normals, byVertex,
                        accepted, partSizes, rejectedSizes, ref parts);
                }

                partTriangles = accepted.Count;
                var bind = mesh.vertices;
                var sum = Vector3.zero;
                foreach (var index in accepted)
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = triangles[index * 3 + corner];
                    selection.Add(vertex);
                    sum += bind[vertex];
                }

                if (partTriangles > 0)
                    centreModel = model.InverseTransformPoint(
                        body.transform.TransformPoint(sum / (partTriangles * 3)));
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                UnityEngine.Object.DestroyImmediate(baked);
            }

            report = "Parts=" + parts +
                     ", PartTriangles=" + partTriangles +
                     ", PartVertices=" + selection.Count +
                     ", CentreModelSpace=(" + centreModel.x.ToString("F4") + ", " +
                     centreModel.y.ToString("F4") + ", " + centreModel.z.ToString("F4") + ")" +
                     ", PartDetail=" + string.Join(" ", partSizes.Distinct()) +
                     ", RejectedBySize=" + string.Join(" ", rejectedSizes.Distinct());
            return selection;
        }

        private static void SweepFloatingHiltRays(
            FloatingHiltSweep sweep,
            Vector3 eye,
            Quaternion rotation,
            float half,
            IReadOnlyList<Vector3> world,
            int[] triangles,
            IReadOnlyList<Vector3> normals,
            IReadOnlyDictionary<int, List<int>> byVertex,
            HashSet<int> accepted,
            ICollection<string> partSizes,
            ICollection<string> rejectedSizes,
            ref int parts)
        {
            {
                for (var px = sweep.MinX; px <= sweep.MaxX; px += FloatingHiltStride)
                for (var py = sweep.MinY; py <= sweep.MaxY; py += FloatingHiltStride)
                {
                    var nx = (px + 0.5f) / FloatingHiltPanel * 2f - 1f;
                    var ny = (py + 0.5f) / FloatingHiltPanel * 2f - 1f;
                    var ray = rotation * new Vector3(nx * half, ny * half, 1f).normalized;
                    var best = float.PositiveInfinity;
                    var hit = -1;
                    for (var index = 0; index + 2 < triangles.Length; index += 3)
                    {
                        if (!RayHitsTriangle(
                                eye, ray,
                                world[triangles[index]],
                                world[triangles[index + 1]],
                                world[triangles[index + 2]],
                                out var distance)) continue;
                        if (distance >= best) continue;
                        best = distance;
                        hit = index / 3;
                    }

                    if (hit < 0 || accepted.Contains(hit)) continue;
                    var visited = new HashSet<int> { hit };
                    var queue = new Queue<int>();
                    queue.Enqueue(hit);
                    while (queue.Count > 0 && visited.Count <= FloatingHiltMaxPartTriangles)
                    {
                        var current = queue.Dequeue();
                        for (var corner = 0; corner < 3; corner++)
                        {
                            var vertex = triangles[current * 3 + corner];
                            if (!byVertex.TryGetValue(vertex, out var list)) continue;
                            foreach (var neighbour in list)
                            {
                                if (visited.Contains(neighbour)) continue;
                                if (Vector3.Angle(normals[current], normals[neighbour]) >
                                    FloatingHiltFlatAngle) continue;
                                visited.Add(neighbour);
                                queue.Enqueue(neighbour);
                            }
                        }
                    }

                    if (visited.Count > FloatingHiltMaxPartTriangles) continue;
                    var min = new Vector3(
                        float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                    var max = new Vector3(
                        float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                    foreach (var index in visited)
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var point = world[triangles[index * 3 + corner]];
                        min = Vector3.Min(min, point);
                        max = Vector3.Max(max, point);
                    }

                    var size = (max - min).magnitude;
                    if (size > FloatingHiltMaxPartSize)
                    {
                        // Recorded so the gap between the flakes and the real armour panels can be
                        // read off the log instead of guessed at when the cap is tuned.
                        rejectedSizes.Add("t" + visited.Count + "/" + size.ToString("F3") + "m");
                        continue;
                    }
                    parts++;
                    partSizes.Add(
                        "t" + visited.Count + "/" + size.ToString("F3") + "m");
                    foreach (var index in visited) accepted.Add(index);
                }

            }
        }

        // Paints the picked flakes red and renders them from the comparison camera plus three more
        // angles, so the selection can be checked against the marked image before anything is cut.
        private static void RenderFloatingHiltHighlight(
            Transform model,
            SkinnedMeshRenderer body,
            ICollection<int> selection)
        {
            var mesh = body.sharedMesh;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            const int columns = 4;
            const int captureLayer = 30;
            var target = new RenderTexture(
                FloatingHiltPanel, FloatingHiltPanel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(
                FloatingHiltPanel, FloatingHiltPanel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                FloatingHiltPanel * columns, FloatingHiltPanel, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06FloatingHiltHighlightCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = FloatingHiltFieldOfView;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var highlightObject = new GameObject("Ispant06FloatingHiltHighlight")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            highlightObject.transform.SetParent(body.transform, false);
            var highlightFilter = highlightObject.AddComponent<MeshFilter>();
            var highlightRenderer = highlightObject.AddComponent<MeshRenderer>();
            var highlightMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.05f, 0.05f, 1f),
                hideFlags = HideFlags.HideAndDontSave
            };
            highlightRenderer.sharedMaterial = highlightMaterial;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != body && item != highlightRenderer && item.enabled).ToArray();
            var baked = new Mesh();
            var highlightMesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            try
            {
                AnimationMode.StartAnimationMode();
                Restore(transforms);
                AnimationMode.SampleAnimationClip(
                    model.gameObject, clip, clip.length * FloatingHiltPhase);
                foreach (var item in hidden) item.enabled = false;
                body.BakeMesh(baked);
                BuildHighlightMesh(baked, mesh.triangles, selection, highlightMesh);
                highlightFilter.sharedMesh = highlightMesh;
                FloatingHiltCamera(model, out var centre, out _, out _);
                // The first column repeats the comparison camera exactly; the other three walk
                // around the hip so a flake cannot hide behind the body in the only view checked.
                var directions = new[]
                {
                    (-model.right * 0.55f - model.forward).normalized,
                    -model.right,
                    -model.forward,
                    (-model.right + model.forward).normalized
                };
                for (var column = 0; column < columns; column++)
                {
                    camera.transform.position = centre + directions[column] * FloatingHiltRadius +
                                                model.up * 0.16f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        centre - camera.transform.position, model.up);
                    RenderIntoSheet(camera, target, buffer, sheet, column, 0, FloatingHiltPanel);
                }

                sheet.Apply();
                var destination = Absolute(FloatingHiltHighlightPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(highlightMesh);
                UnityEngine.Object.DestroyImmediate(highlightObject);
                UnityEngine.Object.DestroyImmediate(highlightMaterial);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Floating Hilt Highlight")]
        public static void CaptureIspant06FloatingHiltHighlight()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var selection = FloatingHiltVertices(model, body, out var pickReport);
            RenderFloatingHiltHighlight(model, body, selection);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The floating hilt highlight changed the scene dirty state.");
            Debug.Log(
                "Ispant06FloatingHiltHighlightCaptured" +
                ", " + pickReport +
                ", Image=" + FloatingHiltHighlightPath +
                ", SceneChanged=False.");
        }

        // Renders the hip as it stands now and as it would look with the picked flakes gone, from
        // the same four cameras. Nothing in the scene changes: both rows draw a baked copy of the
        // body, so the only difference between the rows is the removal itself. This is what proves
        // the cut takes the floating slab and does not open a hole in the armour.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Floating Hilt Removal Preview")]
        public static void CaptureIspant06FloatingHiltRemovalPreview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var selection = FloatingHiltVertices(model, body, out var pickReport);
            RenderRemovalPreview(model, body, selection, FloatingHiltPreviewPath);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The floating hilt removal preview changed the scene dirty state.");
            Debug.Log(
                "Ispant06FloatingHiltRemovalPreviewCaptured" +
                ", " + pickReport +
                ", TopRow=AsItStands, BottomRow=AfterRemoval" +
                ", Image=" + FloatingHiltPreviewPath +
                ", SceneChanged=False.");
        }

        // Shared by every removal preview: the body as it stands on the top row, the body with the
        // given vertices dropped on the bottom row, one column per loop phase. Nothing in the scene
        // changes, because both rows draw a baked stand-in rather than the real renderer.
        private static void RenderRemovalPreview(
            Transform model,
            SkinnedMeshRenderer body,
            ICollection<int> selection,
            string outputPath)
        {
            var mesh = body.sharedMesh;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            const int columns = 4;
            const int rows = 2;
            const int captureLayer = 30;
            var target = new RenderTexture(
                FloatingHiltPanel, FloatingHiltPanel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(
                FloatingHiltPanel, FloatingHiltPanel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                FloatingHiltPanel * columns, FloatingHiltPanel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06FloatingHiltPreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = FloatingHiltFieldOfView;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var previewObject = new GameObject("Ispant06FloatingHiltPreview")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            previewObject.transform.SetParent(body.transform, false);
            var previewFilter = previewObject.AddComponent<MeshFilter>();
            var previewRenderer = previewObject.AddComponent<MeshRenderer>();
            previewRenderer.sharedMaterials = body.sharedMaterials;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            // The real body is hidden too. Both rows come from the baked stand-in, so the rows
            // differ only by the dropped triangles and nothing else.
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != previewRenderer && item.enabled).ToArray();
            var baked = new Mesh();
            try
            {
                AnimationMode.StartAnimationMode();
                // One column per loop phase from the same camera as the comparison capture, so
                // debris that only stands clear of the body at one phase is still on the sheet.
                for (var row = 0; row < rows; row++)
                for (var column = 0; column < columns; column++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * FloatingHiltPhases[column]);
                    foreach (var item in hidden) item.enabled = false;
                    body.BakeMesh(baked);
                    var posed = BuildFloatingHiltPreviewMesh(baked, mesh, selection, row == 1);
                    previewFilter.sharedMesh = posed;
                    FloatingHiltCamera(model, out _, out var eye, out var rotation);
                    camera.transform.position = eye;
                    camera.transform.rotation = rotation;
                    RenderIntoSheet(
                        camera, target, buffer, sheet, column, rows - 1 - row, FloatingHiltPanel);
                    previewFilter.sharedMesh = null;
                    UnityEngine.Object.DestroyImmediate(posed);
                }

                sheet.Apply();
                var destination = Absolute(outputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(previewObject);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        // Picks whole restored clusters instead of rays through pixels. Every vertex the restore
        // appended belongs to exactly one restored cluster, and anything a chosen cluster shares
        // with geometry outside the selection is dropped from it, so this cut cannot reach past
        // the clusters named here. That guarantee is what the ray picking never had.
        private static readonly int[] RestoredClusterRemoveIndexes = { 1, 6 };

        // The current body differs from the pre-restore body by exactly 511 triangles. The
        // approved leg restoration accounts for the one 455-triangle armour cluster; the other
        // 56 triangles are the narrow hilt fragments marked by the user. These exact lineage
        // counts are guards, not tuning values: a different source mesh aborts before selection.
        private const int ExpectedCurrentRestoredTriangles = 511;
        private const int ExpectedRestoredArmourTriangles = 455;
        private const int ExpectedMarkedHiltFragmentTriangles = 56;

        private static HashSet<int> MarkedHiltFragmentVertices(
            SkinnedMeshRenderer body,
            out string report)
        {
            var current = body.sharedMesh;
            var currentPath = AssetDatabase.GetAssetPath(current);
            if (!string.Equals(
                    currentPath, WaistDebrisRemovedMeshPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The marked hilt removal requires " + WaistDebrisRemovedMeshPath +
                    ", but slot 6 currently uses " + currentPath + ".");
            if (current.vertexCount != 8700 || current.triangles.Length / 3 != 9469)
                throw new InvalidOperationException(
                    "The marked hilt source mesh lineage changed. Expected 8700 vertices and " +
                    "9469 triangles, found " + current.vertexCount + " vertices and " +
                    (current.triangles.Length / 3) + " triangles.");

            var reference = AssetDatabase.LoadAssetAtPath<Mesh>(FloatingHiltRemovedMeshPath) ??
                            throw new InvalidOperationException(
                                "The pre-restore slot-6 body mesh is missing.");
            var clusters = MissingTriangleClusters(reference, current)
                .OrderByDescending(item => item.Count).ToList();
            var restoredTriangles = clusters.Sum(item => item.Count);
            if (restoredTriangles != ExpectedCurrentRestoredTriangles)
                throw new InvalidOperationException(
                    "The restored lineage changed. Expected " +
                    ExpectedCurrentRestoredTriangles + " triangles, found " +
                    restoredTriangles + ".");

            var selectedClusterIndexes = new List<int>();
            var chosenTriangles = new HashSet<int>();
            for (var index = 0; index < clusters.Count; index++)
            {
                // The 455-triangle connected armour cluster is preserved. Every remaining
                // restored cluster together is the 56-triangle marked hilt fragment set.
                if (clusters[index].Count == ExpectedRestoredArmourTriangles) continue;
                selectedClusterIndexes.Add(index);
                foreach (var triangle in clusters[index]) chosenTriangles.Add(triangle);
            }

            if (chosenTriangles.Count != ExpectedMarkedHiltFragmentTriangles ||
                restoredTriangles - chosenTriangles.Count != ExpectedRestoredArmourTriangles)
                throw new InvalidOperationException(
                    "The marked hilt split changed. Expected armour/hilt triangles " +
                    ExpectedRestoredArmourTriangles + "/" +
                    ExpectedMarkedHiltFragmentTriangles + ", found " +
                    (restoredTriangles - chosenTriangles.Count) + "/" +
                    chosenTriangles.Count + ".");

            var triangles = current.triangles;
            var selection = new HashSet<int>();
            foreach (var triangle in chosenTriangles)
            for (var corner = 0; corner < 3; corner++)
                selection.Add(triangles[triangle * 3 + corner]);
            var shared = new HashSet<int>();
            for (var triangle = 0; triangle * 3 + 2 < triangles.Length; triangle++)
            {
                if (chosenTriangles.Contains(triangle)) continue;
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = triangles[triangle * 3 + corner];
                    if (selection.Contains(vertex)) shared.Add(vertex);
                }
            }

            if (shared.Count != 0)
                throw new InvalidOperationException(
                    "The marked hilt shares " + shared.Count +
                    " vertices with geometry outside the selection; nothing was removed.");
            report = "CurrentMesh=" + currentPath +
                     ", ClusterTriangles=" +
                     string.Join("|", clusters.Select(item => item.Count)) +
                     ", SelectedClusters=" + string.Join("|", selectedClusterIndexes) +
                     ", ArmourTrianglesKept=" + ExpectedRestoredArmourTriangles +
                     ", HiltTrianglesSelected=" + chosenTriangles.Count +
                     ", SharedVerticesOutsideSelection=" + shared.Count +
                     ", SelectedVertices=" + selection.Count;
            return selection;
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Preview Slot 06 Marked Hilt Fragment Removal")]
        public static void PreviewIspant06MarkedHiltFragmentRemoval()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var selection = MarkedHiltFragmentVertices(body, out var selectionReport);
            RenderRemovalPreview(model, body, selection, MarkedHiltFragmentPreviewPath);
            var destination = Absolute(MarkedHiltFragmentReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(
                destination,
                "Ispant06MarkedHiltFragmentPreview\n" + selectionReport + "\n" +
                "TopRow=AsItStands\nBottomRow=AfterRemoval\nSceneChanged=False\n",
                new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The marked hilt removal preview changed the scene dirty state.");
            Debug.Log(
                "Ispant06MarkedHiltFragmentPreviewCaptured, " + selectionReport +
                ", Image=" + MarkedHiltFragmentPreviewPath +
                ", Report=" + MarkedHiltFragmentReportPath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Marked Hilt Fragment Removal")]
        public static void ApplyIspant06MarkedHiltFragmentRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var sourceBindPoses = source.bindposes;
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            var selection = MarkedHiltFragmentVertices(body, out var selectionReport);
            var derived = BuildDerivedMesh(source, selection, null, 0);
            var removedTriangles = beforeTriangles - derived.triangles.Length / 3;
            if (removedTriangles != ExpectedMarkedHiltFragmentTriangles)
            {
                UnityEngine.Object.DestroyImmediate(derived);
                throw new InvalidOperationException(
                    "The derived mesh would remove " + removedTriangles +
                    " triangles instead of " + ExpectedMarkedHiltFragmentTriangles + ".");
            }
            if (!sourceBindPoses.SequenceEqual(derived.bindposes))
            {
                UnityEngine.Object.DestroyImmediate(derived);
                throw new InvalidOperationException(
                    "The derived mesh changed the slot-6 bind poses; nothing was applied.");
            }

            ReplaceBodyMesh(
                body,
                derived,
                MarkedHiltFragmentRemovedMeshPath,
                "Ispant_06_BodyMarkedHiltFragmentRemoved");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the marked hilt removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06MarkedHiltFragmentRemoved, " + selectionReport +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", RemovedTriangles=" +
                (beforeTriangles - applied.triangles.Length / 3) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", BindPosesPreserved=" + sourceBindPoses.SequenceEqual(applied.bindposes) +
                ", Mesh=" + MarkedHiltFragmentRemovedMeshPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Marked Hilt Fragment Removal")]
        public static void CaptureIspant06MarkedHiltFragmentRemoval()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var currentPath = AssetDatabase.GetAssetPath(body.sharedMesh);
            if (!string.Equals(
                    currentPath,
                    MarkedHiltFragmentRemovedMeshPath,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "The final capture requires " + MarkedHiltFragmentRemovedMeshPath +
                    ", but slot 6 currently uses " + currentPath + ".");
            var reference = AssetDatabase.LoadAssetAtPath<Mesh>(FloatingHiltRemovedMeshPath) ??
                            throw new InvalidOperationException(
                                "The pre-restore slot-6 body mesh is missing.");
            var remainingRestoredTriangles =
                MissingTriangleClusters(reference, body.sharedMesh).Sum(item => item.Count);
            if (remainingRestoredTriangles != ExpectedRestoredArmourTriangles)
                throw new InvalidOperationException(
                    "The final body should retain exactly " + ExpectedRestoredArmourTriangles +
                    " restored armour triangles, but found " +
                    remainingRestoredTriangles + ".");
            RenderRemovalPreview(
                model, body, new HashSet<int>(), MarkedHiltFragmentFinalPath);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The marked hilt final capture changed the scene dirty state.");
            Debug.Log(
                "Ispant06MarkedHiltFragmentFinalCaptured" +
                ", RemainingRestoredArmourTriangles=" + remainingRestoredTriangles +
                ", Image=" + MarkedHiltFragmentFinalPath +
                ", SceneChanged=False.");
        }

        private static HashSet<int> RestoredClusterVertices(
            SkinnedMeshRenderer body,
            out string report)
        {
            var current = body.sharedMesh;
            var reference = AssetDatabase.LoadAssetAtPath<Mesh>(FloatingHiltRemovedMeshPath) ??
                            throw new InvalidOperationException(
                                "The pre-restore slot-6 body mesh is missing.");
            var clusters = MissingTriangleClusters(reference, current)
                .OrderByDescending(item => item.Count).ToList();
            var triangles = current.triangles;
            var chosenTriangles = new HashSet<int>();
            foreach (var index in RestoredClusterRemoveIndexes)
            {
                if (index < 0 || index >= clusters.Count)
                    throw new InvalidOperationException(
                        "Restored cluster " + index + " does not exist; the atlas lists " +
                        clusters.Count + " clusters.");
                foreach (var triangle in clusters[index]) chosenTriangles.Add(triangle);
            }

            var selection = new HashSet<int>();
            foreach (var triangle in chosenTriangles)
            for (var corner = 0; corner < 3; corner++)
                selection.Add(triangles[triangle * 3 + corner]);
            var shared = new HashSet<int>();
            for (var triangle = 0; triangle * 3 + 2 < triangles.Length; triangle++)
            {
                if (chosenTriangles.Contains(triangle)) continue;
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = triangles[triangle * 3 + corner];
                    if (selection.Contains(vertex)) shared.Add(vertex);
                }
            }

            selection.ExceptWith(shared);
            report = "Clusters=" + string.Join("|", RestoredClusterRemoveIndexes) +
                     ", ClusterTriangles=" + chosenTriangles.Count +
                     ", SharedVerticesKept=" + shared.Count +
                     ", SelectedVertices=" + selection.Count;
            return selection;
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Restored Cluster Removal Preview")]
        public static void CaptureIspant06RestoredClusterRemovalPreview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var selection = RestoredClusterVertices(body, out var clusterReport);
            RenderRemovalPreview(model, body, selection, RestoredClusterRemovalPreviewPath);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The restored cluster removal preview changed the scene dirty state.");
            Debug.Log(
                "Ispant06RestoredClusterRemovalPreviewCaptured" +
                ", " + clusterReport +
                ", TopRow=AsItStands, BottomRow=AfterRemoval" +
                ", Image=" + RestoredClusterRemovalPreviewPath +
                ", SceneChanged=False.");
        }

        // Takes the named restored clusters back out, once the preview has confirmed the leg and
        // waist armour stays put.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Restored Cluster Removal")]
        public static void ApplyIspant06RestoredClusterRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var sourceBindPoses = source.bindposes.Length;
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            var selection = RestoredClusterVertices(body, out var clusterReport);
            if (selection.Count == 0)
                throw new InvalidOperationException("The chosen clusters selected nothing.");
            ReplaceBodyMesh(
                body,
                BuildDerivedMesh(source, selection, null, 0),
                WaistDebrisRemovedMeshPath,
                "Ispant_06_BodyWaistDebrisRemoved");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the restored cluster removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06RestoredClusterRemoved" +
                ", " + clusterReport +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", RemovedTriangles=" + (beforeTriangles - applied.triangles.Length / 3) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", BindPosesPreserved=" + (applied.bindposes.Length == sourceBindPoses) +
                ", Mesh=" + WaistDebrisRemovedMeshPath + ".");
        }

        // Draws the baked body, optionally without the triangles the removal would drop. Vertices
        // are kept either way so both rows shade identically and only the cut shows up.
        private static Mesh BuildFloatingHiltPreviewMesh(
            Mesh baked,
            Mesh source,
            ICollection<int> selection,
            bool drop)
        {
            var result = new Mesh
            {
                indexFormat = IndexFormat.UInt32,
                hideFlags = HideFlags.HideAndDontSave,
                vertices = baked.vertices,
                normals = baked.normals,
                uv = baked.uv,
                subMeshCount = source.subMeshCount
            };
            for (var sub = 0; sub < source.subMeshCount; sub++)
            {
                var indices = source.GetTriangles(sub);
                var kept = new List<int>(indices.Length);
                for (var index = 0; index + 2 < indices.Length; index += 3)
                {
                    if (drop &&
                        (selection.Contains(indices[index]) ||
                         selection.Contains(indices[index + 1]) ||
                         selection.Contains(indices[index + 2]))) continue;
                    kept.Add(indices[index]);
                    kept.Add(indices[index + 1]);
                    kept.Add(indices[index + 2]);
                }

                result.SetTriangles(kept, sub, false);
            }

            result.RecalculateBounds();
            return result;
        }

        // Removes the flakes the ray pick selected, once the highlight render has confirmed them.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Floating Hilt Removal")]
        public static void ApplyIspant06FloatingHiltRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var sourceBindPoses = source.bindposes.Length;
            var selection = FloatingHiltVertices(model, body, out var pickReport);
            if (selection.Count == 0)
                throw new InvalidOperationException("The ray pick selected nothing to remove.");
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            ReplaceBodyMesh(
                body,
                BuildDerivedMesh(source, selection, null, 0),
                FloatingHiltRemovedMeshPath,
                "Ispant_06_BodyFloatingHiltRemoved");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the floating hilt removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06FloatingHiltRemoved" +
                ", " + pickReport +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", RemovedTriangles=" + (beforeTriangles - applied.triangles.Length / 3) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", BindPosesPreserved=" + (applied.bindposes.Length == sourceBindPoses) +
                ", Mesh=" + FloatingHiltRemovedMeshPath + ".");
        }

        // Every removal so far only dropped vertices and re-indexed what was left; none of them
        // moved a vertex. Bind positions therefore line up one to one across the whole derived
        // chain, which makes it possible to compare the mesh in the scene against the untouched
        // export and say exactly which triangles have been cut away since.
        private static Vector3Int WeldKey(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x * 100000f),
                Mathf.RoundToInt(position.y * 100000f),
                Mathf.RoundToInt(position.z * 100000f));
        }

        // Returns the origin-mesh triangle indices that no longer exist in the current mesh,
        // grouped into clusters that share a welded vertex position.
        private static List<List<int>> MissingTriangleClusters(Mesh current, Mesh origin)
        {
            var currentVertices = current.vertices;
            var currentTriangles = current.triangles;
            var present = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index + 2 < currentTriangles.Length; index += 3)
                present.Add(TriangleKey(
                    WeldKey(currentVertices[currentTriangles[index]]),
                    WeldKey(currentVertices[currentTriangles[index + 1]]),
                    WeldKey(currentVertices[currentTriangles[index + 2]])));
            var originVertices = origin.vertices;
            var originTriangles = origin.triangles;
            var missing = new List<int>();
            for (var index = 0; index + 2 < originTriangles.Length; index += 3)
            {
                var key = TriangleKey(
                    WeldKey(originVertices[originTriangles[index]]),
                    WeldKey(originVertices[originTriangles[index + 1]]),
                    WeldKey(originVertices[originTriangles[index + 2]]));
                if (!present.Contains(key)) missing.Add(index / 3);
            }

            // Grouped by shared edge, not by shared vertex. Vertex adjacency merges parts that
            // only touch at a single corner, which lumped a whole run of separate removals into
            // one 514 triangle blob and made it impossible to restore the armour without the
            // sword debris that happened to touch it.
            var byEdge = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (var triangle in missing)
            for (var corner = 0; corner < 3; corner++)
            {
                var a = WeldKey(originVertices[originTriangles[triangle * 3 + corner]]);
                var b = WeldKey(originVertices[originTriangles[triangle * 3 + (corner + 1) % 3]]);
                var key = EdgeKey(a, b);
                if (!byEdge.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    byEdge.Add(key, list);
                }

                list.Add(triangle);
            }

            var clusters = new List<List<int>>();
            var visited = new HashSet<int>();
            foreach (var start in missing)
            {
                if (!visited.Add(start)) continue;
                var cluster = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var triangle = queue.Dequeue();
                    cluster.Add(triangle);
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var a = WeldKey(originVertices[originTriangles[triangle * 3 + corner]]);
                        var b = WeldKey(
                            originVertices[originTriangles[triangle * 3 + (corner + 1) % 3]]);
                        if (!byEdge.TryGetValue(EdgeKey(a, b), out var list)) continue;
                        foreach (var neighbour in list)
                            if (visited.Add(neighbour))
                                queue.Enqueue(neighbour);
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        private static string EdgeKey(Vector3Int a, Vector3Int b)
        {
            // Direction must not change the key, so the two ends are sorted before joining.
            var first = a.x != b.x ? a.x < b.x : a.y != b.y ? a.y < b.y : a.z <= b.z;
            var low = first ? a : b;
            var high = first ? b : a;
            return low.x + "," + low.y + "," + low.z + "|" +
                   high.x + "," + high.y + "," + high.z;
        }

        private static string TriangleKey(Vector3Int a, Vector3Int b, Vector3Int c)
        {
            // Winding must not change the key, so the three corners are sorted before joining.
            var corners = new[] { a, b, c }
                .OrderBy(item => item.x).ThenBy(item => item.y).ThenBy(item => item.z);
            return string.Join("|", corners.Select(item => item.x + "," + item.y + "," + item.z));
        }

        // Lists every triangle cut away since the untouched export, grouped and measured, so a
        // wrongly removed part can be identified by where it sits instead of by memory.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Missing Geometry")]
        public static void InspectIspant06MissingGeometry()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var current = body.sharedMesh;
            var origin = AssetDatabase.LoadAssetAtPath<Mesh>(BodyWithoutMusketPath) ??
                         throw new InvalidOperationException(
                             "The untouched slot-6 body export is missing.");
            var hips = RequireBone(model, "Hips");
            var hipsModel = model.InverseTransformPoint(hips.position);
            var clusters = MissingTriangleClusters(current, origin);
            var originTriangles = origin.triangles;
            var originVertices = origin.vertices;
            var weights = origin.boneWeights;
            var bones = body.bones;
            var report = new StringBuilder();
            report.AppendLine("Ispant06MissingGeometryInspection");
            report.AppendLine("CurrentMesh=" + AssetDatabase.GetAssetPath(current));
            report.AppendLine("OriginMesh=" + BodyWithoutMusketPath);
            report.AppendLine(
                "CurrentVertices=" + current.vertexCount +
                ", CurrentTriangles=" + current.triangles.Length / 3 +
                ", OriginVertices=" + origin.vertexCount +
                ", OriginTriangles=" + originTriangles.Length / 3 +
                ", MissingTriangles=" + clusters.Sum(item => item.Count) +
                ", Clusters=" + clusters.Count);
            report.AppendLine(
                "HipsModelSpace=(" + hipsModel.x.ToString("F4") + ", " +
                hipsModel.y.ToString("F4") + ", " + hipsModel.z.ToString("F4") + ")");
            report.AppendLine("Negative model X is the Ispant left side.");
            report.AppendLine();
            report.AppendLine("[Removed clusters, largest first]");
            var order = 0;
            foreach (var cluster in clusters.OrderByDescending(item => item.Count))
            {
                var used = new HashSet<int>();
                foreach (var triangle in cluster)
                for (var corner = 0; corner < 3; corner++)
                    used.Add(originTriangles[triangle * 3 + corner]);
                var min = new Vector3(
                    float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector3(
                    float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                var sum = Vector3.zero;
                foreach (var vertex in used)
                {
                    var point = originVertices[vertex];
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                    sum += point;
                }

                var centre = model.InverseTransformPoint(
                    body.transform.TransformPoint(sum / used.Count));
                var size = max - min;
                var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var vertex in used)
                {
                    var bone = weights[vertex].boneIndex0;
                    var name = bone >= 0 && bone < bones.Length && bones[bone] != null
                        ? bones[bone].name
                        : "none";
                    dominant[name] = dominant.TryGetValue(name, out var value) ? value + 1 : 1;
                }

                report.AppendLine(
                    "#" + order +
                    " triangles=" + cluster.Count +
                    ", vertices=" + used.Count +
                    ", centre=(" + centre.x.ToString("F4") + ", " +
                    centre.y.ToString("F4") + ", " + centre.z.ToString("F4") + ")" +
                    ", size=(" + size.x.ToString("F4") + ", " +
                    size.y.ToString("F4") + ", " + size.z.ToString("F4") + ")" +
                    ", side=" + (centre.x < hipsModel.x ? "IspantLeft" : "IspantRight") +
                    ", bones=" + string.Join(
                        "|",
                        dominant.OrderByDescending(item => item.Value)
                            .Select(item => item.Key + ":" + item.Value)));
                order++;
            }

            var destination = Absolute(MissingGeometryPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination, report.ToString());
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The missing geometry inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06MissingGeometryInspected" +
                ", MissingTriangles=" + clusters.Sum(item => item.Count) +
                ", Clusters=" + clusters.Count +
                ", Report=" + MissingGeometryPath +
                ", SceneChanged=False.");
        }

        // Draws the untouched export in bind pose and paints one removed cluster red per column,
        // the last column showing all of them together. This is how a cluster gets matched to the
        // hole it actually left on the body, instead of being judged from its centre coordinate.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Missing Cluster Atlas")]
        public static void CaptureIspant06MissingClusterAtlas()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var origin = AssetDatabase.LoadAssetAtPath<Mesh>(BodyWithoutMusketPath) ??
                         throw new InvalidOperationException(
                             "The untouched slot-6 body export is missing.");
            var clusters = MissingTriangleClusters(body.sharedMesh, origin)
                .OrderByDescending(item => item.Count).ToList();
            var columns = clusters.Count + 2;
            const int panel = 512;
            const int rows = 2;
            const int captureLayer = 30;
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06MissingClusterCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var shellObject = new GameObject("Ispant06MissingClusterShell")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            shellObject.transform.SetParent(body.transform, false);
            shellObject.AddComponent<MeshFilter>().sharedMesh = origin;
            var shellRenderer = shellObject.AddComponent<MeshRenderer>();
            shellRenderer.sharedMaterials = body.sharedMaterials;
            var markObject = new GameObject("Ispant06MissingClusterMark")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            markObject.transform.SetParent(body.transform, false);
            var markFilter = markObject.AddComponent<MeshFilter>();
            var markRenderer = markObject.AddComponent<MeshRenderer>();
            var markMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.05f, 0.05f, 1f),
                hideFlags = HideFlags.HideAndDontSave
            };
            markRenderer.sharedMaterial = markMaterial;
            var oldActive = RenderTexture.active;
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            // The skinned body and every prop are hidden: this atlas is about the bind-pose shell
            // and nothing else, so the animated pose cannot confuse where a hole sits.
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != shellRenderer && item != markRenderer && item.enabled)
                .ToArray();
            var marks = new List<Mesh>();
            try
            {
                foreach (var item in hidden) item.enabled = false;
                // Framed on the Ispant left hip and thigh, which is where every cluster in
                // question sits. The whole-body framing was too small to read.
                var bounds = shellRenderer.bounds;
                // Wide enough to take in the waist down to the knee, which is the whole span the
                // removed clusters cover.
                var centre = bounds.center -
                             model.up * bounds.size.y * 0.11f -
                             model.right * bounds.size.x * 0.12f;
                var radius = bounds.size.magnitude * 0.72f;
                var directions = new[]
                {
                    (-model.right * 0.55f - model.forward).normalized,
                    -model.right
                };
                for (var column = 0; column < columns; column++)
                {
                    // Column 0 is the mesh as it stands in the scene, drawn in the same bind pose,
                    // so the hole itself is on the sheet next to the clusters that could fill it.
                    shellObject.GetComponent<MeshFilter>().sharedMesh =
                        column == 0 ? body.sharedMesh : origin;
                    var mark = new Mesh { indexFormat = IndexFormat.UInt32 };
                    var indices = new List<int>();
                    var chosen = column == 0
                        ? new List<int>[0]
                        : column <= clusters.Count
                            ? new[] { clusters[column - 1] }
                            : clusters.ToArray();
                    foreach (var cluster in chosen)
                    foreach (var triangle in cluster)
                    for (var corner = 0; corner < 3; corner++)
                        indices.Add(origin.triangles[triangle * 3 + corner]);
                    // Pushed a hair along the normal so the red reads on top of the shell rather
                    // than fighting it for depth.
                    var vertices = origin.vertices;
                    var normals = origin.normals;
                    var offset = new Vector3[vertices.Length];
                    for (var index = 0; index < vertices.Length; index++)
                        offset[index] = normals.Length == vertices.Length
                            ? vertices[index] + normals[index] * 0.002f
                            : vertices[index];
                    mark.vertices = offset;
                    mark.SetTriangles(indices, 0, false);
                    mark.RecalculateBounds();
                    marks.Add(mark);
                    markFilter.sharedMesh = mark;
                    for (var row = 0; row < rows; row++)
                    {
                        camera.transform.position = centre + directions[row] * radius;
                        camera.transform.rotation = Quaternion.LookRotation(
                            centre - camera.transform.position, model.up);
                        RenderIntoSheet(
                            camera, target, buffer, sheet, column, rows - 1 - row, panel);
                    }
                }

                sheet.Apply();
                var destination = Absolute(MissingClusterAtlasPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                foreach (var mark in marks) UnityEngine.Object.DestroyImmediate(mark);
                UnityEngine.Object.DestroyImmediate(markObject);
                UnityEngine.Object.DestroyImmediate(markMaterial);
                UnityEngine.Object.DestroyImmediate(shellObject);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The missing cluster atlas changed the scene dirty state.");
            Debug.Log(
                "Ispant06MissingClusterAtlasCaptured" +
                ", Clusters=" + clusters.Count +
                ", Columns=" + columns +
                ", TopRow=LeftFrontQuarter, BottomRow=LeftSide" +
                ", Image=" + MissingClusterAtlasPath +
                ", SceneChanged=False.");
        }

        // Adds the chosen removed clusters back onto the current mesh. Their vertices are appended
        // straight from the untouched export rather than welded onto existing ones, so the original
        // positions, normals, UVs and bone weights come back exactly as they were and no seam UV
        // gets reused for a corner it never belonged to.
        private static Mesh BuildRestoredMesh(
            Mesh current,
            Mesh origin,
            IEnumerable<List<int>> restore,
            out int addedVertices,
            out int addedTriangles)
        {
            var vertices = new List<Vector3>(current.vertices);
            var normals = new List<Vector3>(current.normals);
            var tangents = new List<Vector4>(current.tangents);
            var uv = new List<Vector2>(current.uv);
            var uv2 = new List<Vector2>(current.uv2);
            var colors = new List<Color32>(current.colors32);
            var weights = new List<BoneWeight>(current.boneWeights);
            var hasNormals = normals.Count == current.vertexCount;
            var hasTangents = tangents.Count == current.vertexCount;
            var hasUv = uv.Count == current.vertexCount;
            var hasUv2 = uv2.Count == current.vertexCount;
            var hasColors = colors.Count == current.vertexCount;
            var originVertices = origin.vertices;
            var originNormals = origin.normals;
            var originTangents = origin.tangents;
            var originUv = origin.uv;
            var originUv2 = origin.uv2;
            var originColors = origin.colors32;
            var originWeights = origin.boneWeights;
            var originTriangles = origin.triangles;
            var mapped = new Dictionary<int, int>();
            var added = new List<int>();
            foreach (var cluster in restore)
            foreach (var triangle in cluster)
            for (var corner = 0; corner < 3; corner++)
            {
                var source = originTriangles[triangle * 3 + corner];
                if (!mapped.TryGetValue(source, out var index))
                {
                    index = vertices.Count;
                    mapped.Add(source, index);
                    vertices.Add(originVertices[source]);
                    if (hasNormals)
                        normals.Add(originNormals.Length == originVertices.Length
                            ? originNormals[source]
                            : Vector3.up);
                    if (hasTangents)
                        tangents.Add(originTangents.Length == originVertices.Length
                            ? originTangents[source]
                            : new Vector4(1f, 0f, 0f, 1f));
                    if (hasUv)
                        uv.Add(originUv.Length == originVertices.Length
                            ? originUv[source]
                            : Vector2.zero);
                    if (hasUv2)
                        uv2.Add(originUv2.Length == originVertices.Length
                            ? originUv2[source]
                            : Vector2.zero);
                    if (hasColors)
                        colors.Add(originColors.Length == originVertices.Length
                            ? originColors[source]
                            : new Color32(255, 255, 255, 255));
                    weights.Add(originWeights[source]);
                }

                added.Add(index);
            }

            var result = new Mesh { indexFormat = IndexFormat.UInt32 };
            result.SetVertices(vertices);
            if (hasNormals) result.SetNormals(normals);
            if (hasTangents) result.SetTangents(tangents);
            if (hasUv) result.SetUVs(0, uv);
            if (hasUv2) result.SetUVs(1, uv2);
            if (hasColors) result.SetColors(colors);
            result.boneWeights = weights.ToArray();
            result.bindposes = current.bindposes;
            result.subMeshCount = current.subMeshCount;
            for (var sub = 0; sub < current.subMeshCount; sub++)
            {
                var indices = new List<int>(current.GetTriangles(sub));
                // The restored patch joins the first submesh, which is the only one the body has.
                if (sub == 0) indices.AddRange(added);
                result.SetTriangles(indices, sub, false);
            }

            result.RecalculateBounds();
            addedVertices = mapped.Count;
            addedTriangles = added.Count / 3;
            return result;
        }

        private static List<List<int>> LeftThighRestoreSet(Mesh current, Mesh origin)
        {
            var clusters = MissingTriangleClusters(current, origin)
                .OrderByDescending(item => item.Count).ToList();
            var chosen = new List<List<int>>();
            foreach (var index in LeftThighRestoreClusters)
            {
                if (index < 0 || index >= clusters.Count)
                    throw new InvalidOperationException(
                        "Restore cluster " + index + " does not exist; the missing geometry " +
                        "report has " + clusters.Count + " clusters.");
                chosen.Add(clusters[index]);
            }

            return chosen;
        }

        // Draws the mesh as it stands and as it would be with the chosen clusters put back, from
        // three angles in bind pose. Nothing in the scene changes.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Left Thigh Restore Preview")]
        public static void CaptureIspant06LeftThighRestorePreview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var current = body.sharedMesh;
            var origin = AssetDatabase.LoadAssetAtPath<Mesh>(BodyWithoutMusketPath) ??
                         throw new InvalidOperationException(
                             "The untouched slot-6 body export is missing.");
            var restore = LeftThighRestoreSet(current, origin);
            var restored = BuildRestoredMesh(
                current, origin, restore, out var addedVertices, out var addedTriangles);
            restored.hideFlags = HideFlags.HideAndDontSave;
            const int panel = 640;
            const int columns = 3;
            // Three rows: the mesh as it stands, the mesh with the clusters put back, and the
            // untouched export. The third row is the answer to "is the thigh whole again",
            // which the first two rows on their own cannot settle.
            const int rows = 3;
            const int captureLayer = 30;
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06LeftThighRestoreCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var shellObject = new GameObject("Ispant06LeftThighRestoreShell")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            shellObject.transform.SetParent(body.transform, false);
            var shellFilter = shellObject.AddComponent<MeshFilter>();
            shellFilter.sharedMesh = current;
            var shellRenderer = shellObject.AddComponent<MeshRenderer>();
            shellRenderer.sharedMaterials = body.sharedMaterials;
            var oldActive = RenderTexture.active;
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != shellRenderer && item.enabled).ToArray();
            try
            {
                foreach (var item in hidden) item.enabled = false;
                var bounds = shellRenderer.bounds;
                // Same framing as the cluster atlas: waist down to the knee.
                var centre = bounds.center -
                             model.up * bounds.size.y * 0.11f -
                             model.right * bounds.size.x * 0.12f;
                var radius = bounds.size.magnitude * 0.72f;
                var directions = new[]
                {
                    (-model.right * 0.55f - model.forward).normalized,
                    -model.right,
                    (-model.right + model.forward).normalized
                };
                for (var row = 0; row < rows; row++)
                {
                    shellFilter.sharedMesh = row == 0 ? current : row == 1 ? restored : origin;
                    for (var column = 0; column < columns; column++)
                    {
                        camera.transform.position = centre + directions[column] * radius;
                        camera.transform.rotation = Quaternion.LookRotation(
                            centre - camera.transform.position, model.up);
                        RenderIntoSheet(
                            camera, target, buffer, sheet, column, rows - 1 - row, panel);
                    }
                }

                sheet.Apply();
                var destination = Absolute(LeftThighRestorePreviewPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(shellObject);
                UnityEngine.Object.DestroyImmediate(restored);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The left thigh restore preview changed the scene dirty state.");
            Debug.Log(
                "Ispant06LeftThighRestorePreviewCaptured" +
                ", Clusters=" + string.Join("|", LeftThighRestoreClusters) +
                ", AddedVertices=" + addedVertices +
                ", AddedTriangles=" + addedTriangles +
                ", Rows=AsItStands|Restored|UntouchedExport" +
                ", Image=" + LeftThighRestorePreviewPath +
                ", SceneChanged=False.");
        }

        // Puts the chosen clusters back, once the preview has confirmed they fill the right hole.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Left Thigh Restore")]
        public static void ApplyIspant06LeftThighRestore()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var current = body.sharedMesh;
            var origin = AssetDatabase.LoadAssetAtPath<Mesh>(BodyWithoutMusketPath) ??
                         throw new InvalidOperationException(
                             "The untouched slot-6 body export is missing.");
            var sourceBindPoses = current.bindposes.Length;
            var beforeVertices = current.vertexCount;
            var beforeTriangles = current.triangles.Length / 3;
            var restore = LeftThighRestoreSet(current, origin);
            var restored = BuildRestoredMesh(
                current, origin, restore, out var addedVertices, out var addedTriangles);
            ReplaceBodyMesh(
                body, restored, LeftThighRestoredMeshPath, "Ispant_06_BodyLeftThighRestored");
            var applied = body.sharedMesh;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the left thigh restore.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06LeftThighRestored" +
                ", Clusters=" + string.Join("|", LeftThighRestoreClusters) +
                ", AddedVertices=" + addedVertices +
                ", AddedTriangles=" + addedTriangles +
                ", BeforeVertices=" + beforeVertices +
                ", BeforeTriangles=" + beforeTriangles +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", BindPosesPreserved=" +
                (applied.bindposes.Length == sourceBindPoses) +
                ", Mesh=" + LeftThighRestoredMeshPath + ".");
        }

        // The restore brings back everything that was cut away, debris included. This paints each
        // restored cluster onto the current body one column at a time so the leg armour can be
        // told apart from the sword flakes that came back with it, and only the flakes taken out
        // again. Column 0 is the body with nothing painted.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Restored Cluster Atlas")]
        public static void CaptureIspant06RestoredClusterAtlas()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var current = body.sharedMesh;
            var reference = AssetDatabase.LoadAssetAtPath<Mesh>(FloatingHiltRemovedMeshPath) ??
                            throw new InvalidOperationException(
                                "The pre-restore slot-6 body mesh is missing.");
            // Swapped arguments on purpose: this asks which triangles the current mesh has that
            // the pre-restore mesh does not, which is exactly the restored set.
            var clusters = MissingTriangleClusters(reference, current)
                .OrderByDescending(item => item.Count).ToList();
            var columns = clusters.Count + 1;
            const int panel = 512;
            const int rows = 2;
            const int captureLayer = 30;
            var target = new RenderTexture(panel, panel, 24, RenderTextureFormat.ARGB32);
            var buffer = new Texture2D(panel, panel, TextureFormat.RGB24, false);
            var sheet = new Texture2D(panel * columns, panel * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06RestoredClusterCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            camera.aspect = 1f;
            var shellObject = new GameObject("Ispant06RestoredClusterShell")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            shellObject.transform.SetParent(body.transform, false);
            shellObject.AddComponent<MeshFilter>().sharedMesh = current;
            var shellRenderer = shellObject.AddComponent<MeshRenderer>();
            shellRenderer.sharedMaterials = body.sharedMaterials;
            var markObject = new GameObject("Ispant06RestoredClusterMark")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = captureLayer
            };
            markObject.transform.SetParent(body.transform, false);
            var markFilter = markObject.AddComponent<MeshFilter>();
            var markRenderer = markObject.AddComponent<MeshRenderer>();
            var markMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.05f, 0.05f, 1f),
                hideFlags = HideFlags.HideAndDontSave
            };
            markRenderer.sharedMaterial = markMaterial;
            var oldActive = RenderTexture.active;
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            var hidden = model.GetComponentsInChildren<Renderer>(true)
                .Where(item => item != shellRenderer && item != markRenderer && item.enabled)
                .ToArray();
            var marks = new List<Mesh>();
            var report = new StringBuilder();
            report.AppendLine("Ispant06RestoredClusterAtlas");
            report.AppendLine("CurrentMesh=" + AssetDatabase.GetAssetPath(current));
            report.AppendLine("ReferenceMesh=" + FloatingHiltRemovedMeshPath);
            var hips = RequireBone(model, "Hips");
            var hipsModel = model.InverseTransformPoint(hips.position);
            report.AppendLine(
                "HipsModelSpace=(" + hipsModel.x.ToString("F4") + ", " +
                hipsModel.y.ToString("F4") + ", " + hipsModel.z.ToString("F4") + ")");
            var currentVertices = current.vertices;
            var currentTriangles = current.triangles;
            var currentWeights = current.boneWeights;
            var bones = body.bones;
            try
            {
                foreach (var item in hidden) item.enabled = false;
                var bounds = shellRenderer.bounds;
                var centre = bounds.center -
                             model.up * bounds.size.y * 0.11f -
                             model.right * bounds.size.x * 0.12f;
                var radius = bounds.size.magnitude * 0.72f;
                var directions = new[]
                {
                    (-model.right * 0.55f - model.forward).normalized,
                    -model.right
                };
                for (var column = 0; column < columns; column++)
                {
                    var mark = new Mesh { indexFormat = IndexFormat.UInt32 };
                    var indices = new List<int>();
                    if (column > 0)
                    {
                        var cluster = clusters[column - 1];
                        var used = new HashSet<int>();
                        var min = new Vector3(
                            float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                        var max = new Vector3(
                            float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                        var sum = Vector3.zero;
                        foreach (var triangle in cluster)
                        for (var corner = 0; corner < 3; corner++)
                        {
                            var vertex = currentTriangles[triangle * 3 + corner];
                            indices.Add(vertex);
                            if (!used.Add(vertex)) continue;
                            var point = currentVertices[vertex];
                            min = Vector3.Min(min, point);
                            max = Vector3.Max(max, point);
                            sum += point;
                        }

                        var clusterCentre = model.InverseTransformPoint(
                            body.transform.TransformPoint(sum / used.Count));
                        var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                        foreach (var vertex in used)
                        {
                            var bone = currentWeights[vertex].boneIndex0;
                            var name = bone >= 0 && bone < bones.Length && bones[bone] != null
                                ? bones[bone].name
                                : "none";
                            dominant[name] =
                                dominant.TryGetValue(name, out var value) ? value + 1 : 1;
                        }

                        report.AppendLine(
                            "column " + column + " -> cluster " + (column - 1) +
                            ", triangles=" + cluster.Count +
                            ", vertices=" + used.Count +
                            ", centre=(" + clusterCentre.x.ToString("F4") + ", " +
                            clusterCentre.y.ToString("F4") + ", " +
                            clusterCentre.z.ToString("F4") + ")" +
                            ", size=" + (max - min).magnitude.ToString("F4") + "m" +
                            ", bones=" + string.Join(
                                "|",
                                dominant.OrderByDescending(item => item.Value)
                                    .Select(item => item.Key + ":" + item.Value)));
                    }

                    var vertices = currentVertices;
                    var normals = current.normals;
                    var offset = new Vector3[vertices.Length];
                    for (var index = 0; index < vertices.Length; index++)
                        offset[index] = normals.Length == vertices.Length
                            ? vertices[index] + normals[index] * 0.002f
                            : vertices[index];
                    mark.vertices = offset;
                    mark.SetTriangles(indices, 0, false);
                    mark.RecalculateBounds();
                    marks.Add(mark);
                    markFilter.sharedMesh = mark;
                    for (var row = 0; row < rows; row++)
                    {
                        camera.transform.position = centre + directions[row] * radius;
                        camera.transform.rotation = Quaternion.LookRotation(
                            centre - camera.transform.position, model.up);
                        RenderIntoSheet(
                            camera, target, buffer, sheet, column, rows - 1 - row, panel);
                    }
                }

                sheet.Apply();
                var destination = Absolute(RestoredClusterAtlasPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
                File.WriteAllText(
                    Absolute(RestoredClusterReportPath), report.ToString());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                foreach (var item in hidden) item.enabled = true;
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                foreach (var mark in marks) UnityEngine.Object.DestroyImmediate(mark);
                UnityEngine.Object.DestroyImmediate(markObject);
                UnityEngine.Object.DestroyImmediate(markMaterial);
                UnityEngine.Object.DestroyImmediate(shellObject);
                UnityEngine.Object.DestroyImmediate(buffer);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The restored cluster atlas changed the scene dirty state.");
            Debug.Log(
                "Ispant06RestoredClusterAtlasCaptured" +
                ", Clusters=" + clusters.Count +
                ", Columns=" + columns +
                ", TopRow=LeftFrontQuarter, BottomRow=LeftSide" +
                ", Image=" + RestoredClusterAtlasPath +
                ", Report=" + RestoredClusterReportPath +
                ", SceneChanged=False.");
        }

        // Lists every connected piece of the slot-6 body except the main shell, with its size and
        // where it sits on the model, so leftover props such as a sheathed hilt can be picked out.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Remaining Islands")]
        public static void InspectIspant06RemainingIslands()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = body.sharedMesh;
            var islands = ConnectedIslands(mesh);
            var largest = islands.OrderByDescending(item => item.Count).First();
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bones = body.bones;
            var hips = model.InverseTransformPoint(RequireBone(model, "Hips").position);
            var report = new StringBuilder();
            report.AppendLine("Ispant06RemainingIslandsInspection");
            report.AppendLine("BodyMesh=" + AssetDatabase.GetAssetPath(mesh));
            report.AppendLine("Vertices=" + mesh.vertexCount +
                              ", Triangles=" + (mesh.triangles.Length / 3) +
                              ", Islands=" + islands.Count +
                              ", MainShellVertices=" + largest.Count);
            report.AppendLine("HipsModelSpace=(" + hips.x.ToString("F4") + ", " +
                              hips.y.ToString("F4") + ", " + hips.z.ToString("F4") + ")");
            report.AppendLine("Negative model X is the Ispant left side.");
            report.AppendLine();
            report.AppendLine("[Every island except the main shell, largest first]");
            var order = 0;
            foreach (var island in islands.Where(item => item != largest)
                         .OrderByDescending(item => item.Count))
            {
                var points = island.Select(index => model.InverseTransformPoint(
                    body.transform.TransformPoint(vertices[index]))).ToArray();
                var min = points.Aggregate(Vector3.Min);
                var max = points.Aggregate(Vector3.Max);
                var centre = points.Aggregate(Vector3.zero, (sum, item) => sum + item) /
                             points.Length;
                var dominant = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var index in island)
                {
                    var bone = weights[index].boneIndex0;
                    var name = bone >= 0 && bone < bones.Length && bones[bone] != null
                        ? bones[bone].name
                        : "none";
                    dominant[name] = dominant.TryGetValue(name, out var count) ? count + 1 : 1;
                }

                report.AppendLine(
                    "#" + order++ +
                    " vertices=" + island.Count +
                    ", centre=(" + centre.x.ToString("F4") + ", " + centre.y.ToString("F4") +
                    ", " + centre.z.ToString("F4") + ")" +
                    ", size=(" + (max.x - min.x).ToString("F4") + ", " +
                    (max.y - min.y).ToString("F4") + ", " + (max.z - min.z).ToString("F4") + ")" +
                    ", side=" + (centre.x < hips.x ? "IspantLeft" : "IspantRight") +
                    ", bones=" + string.Join(
                        "|",
                        dominant.OrderByDescending(item => item.Value).Take(3)
                            .Select(item => item.Key + ":" + item.Value)));
            }

            var absolute = Absolute(RemainingIslandsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The remaining island inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06RemainingIslandsInspected" +
                ", Islands=" + islands.Count +
                ", Report=" + RemainingIslandsPath + ".");
        }

        // Removes only the strip that welds the left arm surface to the torso: triangles near the
        // left arm whose corners mix a left arm bone with a torso bone. Triangles that sit entirely
        // on the arm are kept, so the arm surface itself is never cut away. Once this bridge is
        // gone the arm weights can be cleaned without tearing anything.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Arm Torso Bridge Removal")]
        public static void ApplyIspant06ArmTorsoBridgeRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var ratios = StretchRatios(model, body, out _, out _);
            var triangles = source.triangles;
            var weights = source.boneWeights;
            var vertices = source.vertices;
            var bones = body.bones;
            var bindposes = source.bindposes;

            Vector3 BindPoint(string name)
            {
                var index = Array.FindIndex(bones, item => item != null && item.name == name);
                if (index < 0)
                    throw new InvalidOperationException("The body is not skinned to " + name + ".");
                return bindposes[index].inverse.GetColumn(3);
            }

            var armSegments = new[]
            {
                (BindPoint("LeftShoulder"), BindPoint("LeftArm")),
                (BindPoint("LeftArm"), BindPoint("LeftForeArm")),
                (BindPoint("LeftForeArm"), BindPoint("LeftHand"))
            };
            var chain = new HashSet<int>(
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" }
                    .Select(name => Array.FindIndex(
                        bones, item => item != null && item.name == name)));
            var dropped = new HashSet<int>();
            var stretchingBridges = 0;
            for (var index = 0; index * 3 + 2 < triangles.Length; index++)
            {
                var corners = new[]
                {
                    triangles[index * 3], triangles[index * 3 + 1], triangles[index * 3 + 2]
                };
                var arm = corners.Count(vertex => chain.Contains(weights[vertex].boneIndex0));
                if (arm == 0 || arm == 3) continue;
                var centroid = (vertices[corners[0]] + vertices[corners[1]] + vertices[corners[2]]) /
                               3f;
                var distance = armSegments.Min(
                    segment => DistanceToSegment(centroid, segment.Item1, segment.Item2));
                if (distance > 0.25f) continue;
                dropped.Add(index);
                if (ratios[index] > 2f) stretchingBridges++;
            }

            if (dropped.Count == 0)
                throw new InvalidOperationException("No arm to torso bridge triangles were found.");
            var beforeVertices = source.vertexCount;
            var beforeTriangles = triangles.Length / 3;
            ReplaceBodyMesh(
                body,
                BuildMeshWithoutTriangles(source, dropped),
                ArmTorsoBridgeRemovedMeshPath,
                "Ispant_06_BodyArmTorsoBridgeRemoved");
            // ReplaceBodyMesh may copy into the existing asset and destroy the temporary mesh, so
            // every count below reads the renderer mesh rather than the local one.
            var applied = body.sharedMesh;
            var remaining = StretchRatios(model, body, out _, out _);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the arm to torso bridge removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06ArmTorsoBridgeRemoved" +
                ", BridgeTriangles=" + dropped.Count +
                ", OfWhichStretching=" + stretchingBridges +
                ", RemovedTriangles=" + (beforeTriangles - applied.triangles.Length / 3) +
                ", RemovedVertices=" + (beforeVertices - applied.vertexCount) +
                ", BodyVertices=" + applied.vertexCount +
                ", BodyTriangles=" + (applied.triangles.Length / 3) +
                ", RemainingTrianglesOver2x=" + remaining.Count(item => item > 2f) +
                ", MaxRemainingRatio=" + remaining.Max().ToString("F3") +
                ", BindPosesPreserved=" +
                (applied.bindposes.Length == source.bindposes.Length) +
                ", Mesh=" + ArmTorsoBridgeRemovedMeshPath + ".");
        }

        // Cleans the whole connected left arm surface at once instead of a handful of vertices.
        // Starting from the stretching arm vertices it walks the mesh through neighbours that the
        // left arm already drives, then drops every influence from outside the arm chain and
        // renormalises. No vertex is duplicated and none is deleted, so the surface stays welded
        // and the modelled shape is untouched; the remaining deformation collapses onto the single
        // row where the arm meets the torso, which is ordinary joint skinning.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Left Arm Region Weight Clean")]
        public static void ApplyIspant06LeftArmRegionWeightClean()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var ratios = StretchRatios(model, body, out _, out _);
            var triangles = source.triangles;
            var weights = source.boneWeights.ToArray();
            var bones = body.bones;
            var chain = new HashSet<int>(
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" }
                    .Select(name => Array.FindIndex(
                        bones, item => item != null && item.name == name)));
            // Membership is geometric, not by current weighting. The bind pose has the arms down
            // beside the hips, so the auto rig bled hip and thigh weights onto the forearm surface;
            // those vertices are still arm surface and must be treated as such.
            var bindposes = source.bindposes;

            Vector3 BindPoint(string name)
            {
                var index = Array.FindIndex(bones, item => item != null && item.name == name);
                if (index < 0)
                    throw new InvalidOperationException("The body is not skinned to " + name + ".");
                return bindposes[index].inverse.GetColumn(3);
            }

            var armSegments = new[]
            {
                (BindPoint("LeftShoulder"), BindPoint("LeftArm")),
                (BindPoint("LeftArm"), BindPoint("LeftForeArm")),
                (BindPoint("LeftForeArm"), BindPoint("LeftHand"))
            };
            var torsoSegments = new[]
            {
                (BindPoint("Hips"), BindPoint("Spine02")),
                (BindPoint("Spine02"), BindPoint("Spine01")),
                (BindPoint("Spine01"), BindPoint("Spine")),
                (BindPoint("Hips"), BindPoint("LeftUpLeg")),
                (BindPoint("LeftUpLeg"), BindPoint("LeftLeg"))
            };
            var vertices = source.vertices;
            var region = new HashSet<int>();
            for (var index = 0; index < vertices.Length; index++)
            {
                var toArm = armSegments.Min(
                    segment => DistanceToSegment(vertices[index], segment.Item1, segment.Item2));
                if (toArm > 0.15f) continue;
                var toTorso = torsoSegments.Min(
                    segment => DistanceToSegment(vertices[index], segment.Item1, segment.Item2));
                if (toArm >= toTorso) continue;
                region.Add(index);
            }

            var seeds = region.Count(index => chain.Contains(weights[index].boneIndex0));
            if (region.Count == 0)
                throw new InvalidOperationException("No left arm surface vertices were found.");

            var beforeWeight0 = region.Average(index => weights[index].weight0);
            var changed = 0;
            foreach (var index in region)
            {
                var repaired = KeepOnlyBones(weights[index], chain);
                if (repaired.Equals(weights[index])) continue;
                weights[index] = repaired;
                changed++;
            }

            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.boneWeights = weights;
            ReplaceBodyMesh(
                body, mesh, LeftArmRegionCleanMeshPath, "Ispant_06_BodyLeftArmRegionClean");
            var afterWeight0 = region.Average(
                index => body.sharedMesh.boneWeights[index].weight0);
            var remaining = StretchRatios(model, body, out _, out _);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the left arm region weight clean.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06LeftArmRegionWeightCleanApplied" +
                ", ArmDominantSeedVertices=" + seeds +
                ", RegionVertices=" + region.Count +
                ", ChangedVertices=" + changed +
                ", MeanWeight0Before=" + beforeWeight0.ToString("F3") +
                ", MeanWeight0After=" + afterWeight0.ToString("F3") +
                ", Vertices=" + body.sharedMesh.vertexCount +
                ", Triangles=" + (body.sharedMesh.triangles.Length / 3) +
                ", RemainingTrianglesOver2x=" + remaining.Count(item => item > 2f) +
                ", MaxRemainingRatio=" + remaining.Max().ToString("F3") +
                ", Mesh=" + LeftArmRegionCleanMeshPath + ".");
        }

        private static BoneWeight KeepOnlyBones(BoneWeight weight, ICollection<int> allowed)
        {
            var pairs = new[]
            {
                (weight.boneIndex0, weight.weight0),
                (weight.boneIndex1, weight.weight1),
                (weight.boneIndex2, weight.weight2),
                (weight.boneIndex3, weight.weight3)
            }.Where(pair => pair.Item2 > 0f && allowed.Contains(pair.Item1))
                .OrderByDescending(pair => pair.Item2).ToArray();
            var total = pairs.Sum(pair => pair.Item2);
            if (pairs.Length == 0 || total <= 0f)
                return new BoneWeight { boneIndex0 = weight.boneIndex0, weight0 = 1f };
            var result = new BoneWeight();
            for (var slot = 0; slot < pairs.Length && slot < 4; slot++)
            {
                var bone = pairs[slot].Item1;
                var value = pairs[slot].Item2 / total;
                switch (slot)
                {
                    case 0:
                        result.boneIndex0 = bone;
                        result.weight0 = value;
                        break;
                    case 1:
                        result.boneIndex1 = bone;
                        result.weight1 = value;
                        break;
                    case 2:
                        result.boneIndex2 = bone;
                        result.weight2 = value;
                        break;
                    default:
                        result.boneIndex3 = bone;
                        result.weight3 = value;
                        break;
                }
            }

            return result;
        }

        private static Dictionary<int, HashSet<int>> VertexAdjacency(Mesh mesh)
        {
            var values = new Dictionary<int, HashSet<int>>();
            var triangles = mesh.triangles;

            void Link(int a, int b)
            {
                if (!values.TryGetValue(a, out var set))
                {
                    set = new HashSet<int>();
                    values.Add(a, set);
                }

                set.Add(b);
            }

            for (var index = 0; index + 2 < triangles.Length; index += 3)
            {
                Link(triangles[index], triangles[index + 1]);
                Link(triangles[index + 1], triangles[index]);
                Link(triangles[index + 1], triangles[index + 2]);
                Link(triangles[index + 2], triangles[index + 1]);
                Link(triangles[index + 2], triangles[index]);
                Link(triangles[index], triangles[index + 2]);
            }

            return values;
        }

        // Measures how far apart vertices that share a bind position drift during the loop. That is
        // exactly how wide the split seam opens, and it needs no bookkeeping from the split itself.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Seam Gap")]
        public static void InspectIspant06SeamGap()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = body.sharedMesh;
            var bind = mesh.vertices;
            var groups = new Dictionary<Vector3Int, List<int>>();
            for (var index = 0; index < bind.Length; index++)
            {
                var key = new Vector3Int(
                    Mathf.RoundToInt(bind[index].x * 100000f),
                    Mathf.RoundToInt(bind[index].y * 100000f),
                    Mathf.RoundToInt(bind[index].z * 100000f));
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    groups.Add(key, list);
                }

                list.Add(index);
            }

            var shared = groups.Values.Where(item => item.Count > 1).ToArray();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var baked = new Mesh();
            var worst = 0f;
            var worstPhase = 0f;
            var worstCentre = Vector3.zero;
            var over1cm = 0;
            var over5cm = 0;
            try
            {
                AnimationMode.StartAnimationMode();
                var frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                var peak = new float[shared.Length];
                for (var frame = 0; frame <= frames; frame++)
                {
                    Restore(states);
                    var phase = frame / (float)frames;
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * frame / frames);
                    body.BakeMesh(baked);
                    var posed = baked.vertices;
                    for (var group = 0; group < shared.Length; group++)
                    {
                        var members = shared[group];
                        var separation = 0f;
                        for (var a = 0; a < members.Count; a++)
                        for (var b = a + 1; b < members.Count; b++)
                            separation = Mathf.Max(
                                separation,
                                Vector3.Distance(posed[members[a]], posed[members[b]]));
                        peak[group] = Mathf.Max(peak[group], separation);
                        if (separation <= worst) continue;
                        worst = separation;
                        worstPhase = phase;
                        worstCentre = model.InverseTransformPoint(
                            body.transform.TransformPoint(bind[members[0]]));
                    }
                }

                over1cm = peak.Count(item => item > 0.01f);
                over5cm = peak.Count(item => item > 0.05f);
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
                UnityEngine.Object.DestroyImmediate(baked);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The seam gap inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06SeamGapInspected" +
                ", SharedPositionGroups=" + shared.Length +
                ", MaxSeamGap=" + worst.ToString("F6") +
                ", AtPhase=" + worstPhase.ToString("F4") +
                ", AtModelSpace=(" + worstCentre.x.ToString("F4") + ", " +
                worstCentre.y.ToString("F4") + ", " + worstCentre.z.ToString("F4") + ")" +
                ", GroupsOver1cm=" + over1cm +
                ", GroupsOver5cm=" + over5cm + ".");
        }

        // Splits the seam instead of deleting or reweighting in place. Every stretching triangle
        // gets its own copies of its three corners, and those copies are bound rigidly to the bone
        // that already drives most of the triangle. The triangle then travels as one piece, so it
        // cannot stretch, while the original vertices keep serving every other triangle. Nothing is
        // removed and the bind pose shape is untouched; the seam simply opens instead of smearing.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Left Arm Seam Split")]
        public static void ApplyIspant06LeftArmSeamSplit()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var ratios = StretchRatios(model, body, out _, out _);
            var triangles = source.triangles;
            var weights = source.boneWeights;
            var assignments = new Dictionary<int, int>();
            for (var index = 0; index < ratios.Length; index++)
            {
                if (ratios[index] <= 2f) continue;
                var corners = new[]
                {
                    weights[triangles[index * 3]],
                    weights[triangles[index * 3 + 1]],
                    weights[triangles[index * 3 + 2]]
                };
                var bone = corners
                    .GroupBy(item => item.boneIndex0)
                    .OrderByDescending(group => group.Count())
                    .ThenByDescending(group => group.Max(item => item.weight0))
                    .First().Key;
                assignments[index] = bone;
            }

            if (assignments.Count == 0)
                throw new InvalidOperationException("No stretching triangles were found to split.");
            var mesh = BuildMeshWithSplitSeam(source, assignments);
            ReplaceBodyMesh(body, mesh, LeftArmSeamSplitMeshPath, "Ispant_06_BodyLeftArmSeamSplit");
            var remaining = StretchRatios(model, body, out _, out _);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the left arm seam split.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06LeftArmSeamSplitApplied" +
                ", SplitTriangles=" + assignments.Count +
                ", AddedVertices=" + (mesh.vertexCount - source.vertexCount) +
                ", TrianglesUnchangedCount=" + (mesh.triangles.Length / 3) +
                ", SourceTriangles=" + (triangles.Length / 3) +
                ", RemainingTrianglesOver2x=" + remaining.Count(item => item > 2f) +
                ", MaxRemainingRatio=" + remaining.Max().ToString("F3") +
                ", BindPosesPreserved=" +
                (mesh.bindposes.Length == source.bindposes.Length) +
                ", Mesh=" + LeftArmSeamSplitMeshPath + ".");
        }

        private static Mesh BuildMeshWithSplitSeam(
            Mesh source,
            IReadOnlyDictionary<int, int> assignments)
        {
            var vertices = source.vertices.ToList();
            var normals = source.normals.ToList();
            var tangents = source.tangents.ToList();
            var uv = source.uv.ToList();
            var uv2 = source.uv2.ToList();
            var colors = source.colors32.ToList();
            var weights = source.boneWeights.ToList();
            var submeshes = new List<int[]>();
            var global = 0;
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                var triangles = source.GetTriangles(submesh);
                for (var index = 0; index + 2 < triangles.Length; index += 3, global++)
                {
                    if (!assignments.TryGetValue(global, out var bone)) continue;
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var original = triangles[index + corner];
                        vertices.Add(vertices[original]);
                        if (normals.Count > original) normals.Add(normals[original]);
                        if (tangents.Count > original) tangents.Add(tangents[original]);
                        if (uv.Count > original) uv.Add(uv[original]);
                        if (uv2.Count > original) uv2.Add(uv2[original]);
                        if (colors.Count > original) colors.Add(colors[original]);
                        weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1f });
                        triangles[index + corner] = vertices.Count - 1;
                    }
                }

                submeshes.Add(triangles);
            }

            var result = new Mesh { indexFormat = IndexFormat.UInt32 };
            result.SetVertices(vertices);
            if (normals.Count == vertices.Count) result.SetNormals(normals);
            if (tangents.Count == vertices.Count) result.SetTangents(tangents);
            if (uv.Count == vertices.Count) result.SetUVs(0, uv);
            if (uv2.Count == vertices.Count) result.SetUVs(1, uv2);
            if (colors.Count == vertices.Count) result.SetColors(colors);
            result.subMeshCount = source.subMeshCount;
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
                result.SetTriangles(submeshes[submesh], submesh);
            result.boneWeights = weights.ToArray();
            result.bindposes = source.bindposes;
            result.RecalculateBounds();
            return result;
        }

        // Repairs the skinning instead of deleting geometry. A vertex that stretches is influenced
        // by a bone far away in the skeleton, so only the influences from its dominant bone and
        // that bone immediate neighbours are kept and renormalised. No vertex or triangle is
        // removed, so the modelled shape stays exactly as authored.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Left Arm Weight Fix")]
        public static void ApplyIspant06LeftArmWeightFix()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var ratios = StretchRatios(model, body, out _, out _);
            var triangles = source.triangles;
            var targets = new HashSet<int>();
            for (var index = 0; index < ratios.Length; index++)
            {
                if (ratios[index] <= 2f) continue;
                targets.Add(triangles[index * 3]);
                targets.Add(triangles[index * 3 + 1]);
                targets.Add(triangles[index * 3 + 2]);
            }

            var bones = body.bones;
            // Only vertices that the left arm itself drives are repaired. Touching torso vertices
            // as well made neighbouring corners snap to different bones and stretched even more.
            var chain = new HashSet<int>(
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" }
                    .Select(name => Array.FindIndex(
                        bones, item => item != null && item.name == name)));
            var weights = source.boneWeights.ToArray();
            targets.RemoveWhere(index => !chain.Contains(weights[index].boneIndex0));
            if (targets.Count == 0)
                throw new InvalidOperationException("No stretching left arm vertices were found.");
            var beforeWeight0 = targets.Average(index => weights[index].weight0);
            var changed = 0;
            foreach (var index in targets)
            {
                var dominant = weights[index].boneIndex0;
                if (dominant < 0 || dominant >= bones.Length) continue;
                var allowed = chain;
                var pairs = new[]
                {
                    (weights[index].boneIndex0, weights[index].weight0),
                    (weights[index].boneIndex1, weights[index].weight1),
                    (weights[index].boneIndex2, weights[index].weight2),
                    (weights[index].boneIndex3, weights[index].weight3)
                }.Where(pair => pair.Item2 > 0f && allowed.Contains(pair.Item1)).ToArray();
                var total = pairs.Sum(pair => pair.Item2);
                if (total <= 0f)
                {
                    pairs = new[] { (dominant, 1f) };
                    total = 1f;
                }

                var repaired = new BoneWeight();
                for (var slot = 0; slot < 4; slot++)
                {
                    var value = slot < pairs.Length
                        ? (pairs[slot].Item1, pairs[slot].Item2 / total)
                        : (0, 0f);
                    switch (slot)
                    {
                        case 0:
                            repaired.boneIndex0 = value.Item1;
                            repaired.weight0 = value.Item2;
                            break;
                        case 1:
                            repaired.boneIndex1 = value.Item1;
                            repaired.weight1 = value.Item2;
                            break;
                        case 2:
                            repaired.boneIndex2 = value.Item1;
                            repaired.weight2 = value.Item2;
                            break;
                        default:
                            repaired.boneIndex3 = value.Item1;
                            repaired.weight3 = value.Item2;
                            break;
                    }
                }

                if (Mathf.Approximately(repaired.weight0, weights[index].weight0) &&
                    repaired.boneIndex0 == weights[index].boneIndex0 &&
                    Mathf.Approximately(repaired.weight1, weights[index].weight1) &&
                    Mathf.Approximately(repaired.weight2, weights[index].weight2) &&
                    Mathf.Approximately(repaired.weight3, weights[index].weight3)) continue;
                weights[index] = repaired;
                changed++;
            }

            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.boneWeights = weights;
            ReplaceBodyMesh(
                body, mesh, LeftArmWeightFixedMeshPath, "Ispant_06_BodyLeftArmWeightFixed");
            var afterWeight0 = targets.Average(index => body.sharedMesh.boneWeights[index].weight0);
            var remaining = StretchRatios(model, body, out _, out _);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the left arm weight fix.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06LeftArmWeightFixApplied" +
                ", TargetVertices=" + targets.Count +
                ", ChangedVertices=" + changed +
                ", MeanWeight0Before=" + beforeWeight0.ToString("F3") +
                ", MeanWeight0After=" + afterWeight0.ToString("F3") +
                ", VerticesUnchangedCount=" + body.sharedMesh.vertexCount +
                ", TrianglesUnchangedCount=" + (body.sharedMesh.triangles.Length / 3) +
                ", RemainingTrianglesOver2x=" + remaining.Count(item => item > 2f) +
                ", MaxRemainingRatio=" + remaining.Max().ToString("F3") +
                ", Mesh=" + LeftArmWeightFixedMeshPath + ".");
        }

        // Each bone maps to itself, its parent and its direct children inside the skin bone list.
        private static Dictionary<int, HashSet<int>> BoneNeighbourSets(IReadOnlyList<Transform> bones)
        {
            var lookup = new Dictionary<Transform, int>();
            for (var index = 0; index < bones.Count; index++)
                if (bones[index] != null)
                    lookup[bones[index]] = index;
            var values = new Dictionary<int, HashSet<int>>();
            for (var index = 0; index < bones.Count; index++)
            {
                if (bones[index] == null) continue;
                var set = new HashSet<int> { index };
                if (bones[index].parent != null && lookup.TryGetValue(bones[index].parent, out var parent))
                    set.Add(parent);
                for (var child = 0; child < bones.Count; child++)
                    if (bones[child] != null && bones[child].parent == bones[index])
                        set.Add(child);
                values[index] = set;
            }

            return values;
        }

        // Puts the slot-6 body back to the mesh that still contains the left arm geometry, undoing
        // the triangle removal. Nothing else about the slot changes.
        [MenuItem("Bellerophon/Enemies/Ispant/Restore Slot 06 Body Before Stretch Removal")]
        public static void RestoreIspant06BodyBeforeStretchRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(WaistHiltRemovedMeshPath) ??
                       throw new InvalidOperationException(
                           "The body mesh from before the stretch removal is missing.");
            body.sharedMesh = mesh;
            EditorUtility.SetDirty(body);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after restoring the slot-6 body mesh.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06BodyRestoredBeforeStretchRemoval" +
                ", Mesh=" + WaistHiltRemovedMeshPath +
                ", Vertices=" + mesh.vertexCount +
                ", Triangles=" + (mesh.triangles.Length / 3) + ".");
        }

        // Breaks the stretching triangles down vertex by vertex so the real left arm geometry can
        // be told apart from whatever it is welded to, without deleting anything.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Stretch Triangles")]
        public static void InspectIspant06StretchTriangles()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = body.sharedMesh;
            var ratios = StretchRatios(model, body, out _, out _);
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindposes = mesh.bindposes;
            var foreArmIndex = Array.FindIndex(
                body.bones, item => item != null && item.name == "LeftForeArm");
            var armIndex = Array.FindIndex(
                body.bones, item => item != null && item.name == "LeftArm");
            var handIndex = Array.FindIndex(
                body.bones, item => item != null && item.name == "LeftHand");
            var foreArmPoint = (Vector3)bindposes[foreArmIndex].inverse.GetColumn(3);
            var armPoint = (Vector3)bindposes[armIndex].inverse.GetColumn(3);
            var handPoint = (Vector3)bindposes[handIndex].inverse.GetColumn(3);
            var hipsLocal = model.InverseTransformPoint(RequireBone(model, "Hips").position);
            var leftHandLocal = model.InverseTransformPoint(RequireBone(model, "LeftHand").position);
            var rightHandLocal = model.InverseTransformPoint(
                RequireBone(model, "RightHand").position);
            var report = new StringBuilder();
            report.AppendLine("Ispant06StretchTriangleInspection");
            report.AppendLine("BodyMesh=" + AssetDatabase.GetAssetPath(mesh));
            report.AppendLine("Vertices=" + mesh.vertexCount +
                              ", Triangles=" + (triangles.Length / 3));
            report.AppendLine(
                "SideConvention: HipsModelX=" + hipsLocal.x.ToString("F4") +
                ", LeftHandModelX=" + leftHandLocal.x.ToString("F4") +
                ", RightHandModelX=" + rightHandLocal.x.ToString("F4"));
            report.AppendLine();
            var bad = Enumerable.Range(0, ratios.Length).Where(index => ratios[index] > 2f).ToArray();
            report.AppendLine("[Stretching triangles, ratio over 2x] Count=" + bad.Length);
            var perVertex = new Dictionary<int, float>();
            foreach (var index in bad)
            for (var corner = 0; corner < 3; corner++)
            {
                var vertex = triangles[index * 3 + corner];
                perVertex[vertex] = perVertex.TryGetValue(vertex, out var value)
                    ? Mathf.Max(value, ratios[index])
                    : ratios[index];
            }

            report.AppendLine("VerticesInvolved=" + perVertex.Count);
            report.AppendLine();
            report.AppendLine("[Involved vertices grouped by dominant bone]");
            foreach (var group in perVertex.Keys
                         .GroupBy(vertex =>
                         {
                             var bone = weights[vertex].boneIndex0;
                             return bone >= 0 && bone < body.bones.Length && body.bones[bone] != null
                                 ? body.bones[bone].name
                                 : "none";
                         }, StringComparer.Ordinal)
                         .OrderByDescending(item => item.Count()))
            {
                var members = group.ToArray();
                var centre = members.Aggregate(Vector3.zero, (sum, v) => sum + vertices[v]) /
                             members.Length;
                var localCentre = model.InverseTransformPoint(
                    body.transform.TransformPoint(centre));
                var armDistance = members.Average(v => Mathf.Min(
                    DistanceToSegment(vertices[v], armPoint, foreArmPoint),
                    DistanceToSegment(vertices[v], foreArmPoint, handPoint)));
                report.AppendLine(
                    group.Key + ": count=" + members.Length +
                    ", BindCentreModelSpace=(" + localCentre.x.ToString("F4") + ", " +
                    localCentre.y.ToString("F4") + ", " + localCentre.z.ToString("F4") + ")" +
                    ", Side=" + (localCentre.x < hipsLocal.x ? "IspantLeft" : "IspantRight") +
                    ", MeanDistanceToLeftArmBones=" + armDistance.ToString("F4") + "m" +
                    ", MeanWeight0=" + members.Average(v => weights[v].weight0).ToString("F3"));
            }

            report.AppendLine();
            report.AppendLine("[Influences on the involved vertices]");
            var influence = new Dictionary<string, (int Count, float Sum, int Dominant)>(
                StringComparer.Ordinal);
            foreach (var vertex in perVertex.Keys)
            {
                var weight = weights[vertex];
                var pairs = new[]
                {
                    (weight.boneIndex0, weight.weight0),
                    (weight.boneIndex1, weight.weight1),
                    (weight.boneIndex2, weight.weight2),
                    (weight.boneIndex3, weight.weight3)
                };
                for (var slot = 0; slot < pairs.Length; slot++)
                {
                    var (bone, value) = pairs[slot];
                    if (value <= 0.0001f) continue;
                    var name = bone >= 0 && bone < body.bones.Length && body.bones[bone] != null
                        ? body.bones[bone].name
                        : "none";
                    var entry = influence.TryGetValue(name, out var current)
                        ? current
                        : (Count: 0, Sum: 0f, Dominant: 0);
                    influence[name] = (
                        entry.Count + 1,
                        entry.Sum + value,
                        entry.Dominant + (slot == 0 ? 1 : 0));
                }
            }

            foreach (var entry in influence.OrderByDescending(item => item.Value.Sum))
                report.AppendLine(
                    entry.Key + ": vertices=" + entry.Value.Count +
                    ", asDominant=" + entry.Value.Dominant +
                    ", weightSum=" + entry.Value.Sum.ToString("F3") +
                    ", meanWeight=" + (entry.Value.Sum / entry.Value.Count).ToString("F3"));

            report.AppendLine();
            report.AppendLine("[Worst triangles]");
            foreach (var index in bad.OrderByDescending(item => ratios[item]).Take(15))
            {
                var names = Enumerable.Range(0, 3).Select(corner =>
                {
                    var vertex = triangles[index * 3 + corner];
                    var bone = weights[vertex].boneIndex0;
                    return bone >= 0 && bone < body.bones.Length && body.bones[bone] != null
                        ? body.bones[bone].name
                        : "none";
                });
                report.AppendLine(
                    "ratio=" + ratios[index].ToString("F3") + " corners=" + string.Join("/", names));
            }

            var absolute = Absolute(StretchTriangleDiagnosisPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The stretch triangle inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06StretchTrianglesInspected" +
                ", Triangles=" + bad.Length +
                ", Vertices=" + perVertex.Count +
                ", Report=" + StretchTriangleDiagnosisPath + ".");
        }

        // Removes the deformed geometry itself: triangles that more than double their longest edge
        // during the loop and are driven by a left arm bone. That is the sheathed sword remnant on
        // the left waist together with the strip it stretches into the arm. Vertices that no
        // triangle uses afterwards are dropped as well, and nothing else is touched.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Left Arm Stretch Removal")]
        public static void ApplyIspant06LeftArmStretchRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var ratios = StretchRatios(model, body, out _, out _);
            var triangles = source.triangles;
            var weights = source.boneWeights;
            var chain = new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };
            var chainIndices = new HashSet<int>(chain
                .Select(name => Array.FindIndex(
                    body.bones, item => item != null && item.name == name)));
            var dropped = new HashSet<int>();
            for (var index = 0; index < ratios.Length; index++)
            {
                if (ratios[index] <= 2f) continue;
                var driven = chainIndices.Contains(weights[triangles[index * 3]].boneIndex0) ||
                             chainIndices.Contains(weights[triangles[index * 3 + 1]].boneIndex0) ||
                             chainIndices.Contains(weights[triangles[index * 3 + 2]].boneIndex0);
                if (driven) dropped.Add(index);
            }

            if (dropped.Count == 0)
                throw new InvalidOperationException(
                    "No stretching left-arm-driven triangles were found to remove.");
            var beforeVertices = source.vertexCount;
            var beforeTriangles = triangles.Length / 3;
            var mesh = BuildMeshWithoutTriangles(source, dropped);
            var afterVertices = mesh.vertexCount;
            var afterTriangles = mesh.triangles.Length / 3;
            ReplaceBodyMesh(
                body, mesh, LeftArmStretchRemovedMeshPath, "Ispant_06_BodyLeftArmStretchRemoved");
            var remaining = StretchRatios(model, body, out _, out _);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the left arm stretch removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06LeftArmStretchRemoved" +
                ", RemovedTriangles=" + (beforeTriangles - afterTriangles) +
                ", RemovedVertices=" + (beforeVertices - afterVertices) +
                ", BodyVertices=" + afterVertices +
                ", BodyTriangles=" + afterTriangles +
                ", RemainingTrianglesOver2x=" + remaining.Count(item => item > 2f) +
                ", MaxRemainingRatio=" + remaining.Max().ToString("F3") +
                ", BindPosesPreserved=" +
                (mesh.bindposes.Length == source.bindposes.Length) +
                ", Mesh=" + LeftArmStretchRemovedMeshPath + ".");
        }

        private static Mesh BuildMeshWithoutTriangles(Mesh source, ICollection<int> dropped)
        {
            var kept = new List<List<int>>();
            var used = new HashSet<int>();
            var global = 0;
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                var triangles = source.GetTriangles(submesh);
                var list = new List<int>();
                for (var index = 0; index + 2 < triangles.Length; index += 3, global++)
                {
                    if (dropped.Contains(global)) continue;
                    list.Add(triangles[index]);
                    list.Add(triangles[index + 1]);
                    list.Add(triangles[index + 2]);
                    used.Add(triangles[index]);
                    used.Add(triangles[index + 1]);
                    used.Add(triangles[index + 2]);
                }

                kept.Add(list);
            }

            var remap = new int[source.vertexCount];
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var colors = new List<Color32>();
            var weights = new List<BoneWeight>();
            var sourceVertices = source.vertices;
            var sourceNormals = source.normals;
            var sourceTangents = source.tangents;
            var sourceUv = source.uv;
            var sourceUv2 = source.uv2;
            var sourceColors = source.colors32;
            var sourceWeights = source.boneWeights;
            for (var index = 0; index < source.vertexCount; index++)
            {
                if (!used.Contains(index))
                {
                    remap[index] = -1;
                    continue;
                }

                remap[index] = vertices.Count;
                vertices.Add(sourceVertices[index]);
                if (sourceNormals.Length == source.vertexCount) normals.Add(sourceNormals[index]);
                if (sourceTangents.Length == source.vertexCount) tangents.Add(sourceTangents[index]);
                if (sourceUv.Length == source.vertexCount) uv.Add(sourceUv[index]);
                if (sourceUv2.Length == source.vertexCount) uv2.Add(sourceUv2[index]);
                if (sourceColors.Length == source.vertexCount) colors.Add(sourceColors[index]);
                weights.Add(sourceWeights[index]);
            }

            var result = new Mesh { indexFormat = source.indexFormat };
            result.SetVertices(vertices);
            if (normals.Count == vertices.Count) result.SetNormals(normals);
            if (tangents.Count == vertices.Count) result.SetTangents(tangents);
            if (uv.Count == vertices.Count) result.SetUVs(0, uv);
            if (uv2.Count == vertices.Count) result.SetUVs(1, uv2);
            if (colors.Count == vertices.Count) result.SetColors(colors);
            result.subMeshCount = source.subMeshCount;
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
                result.SetTriangles(kept[submesh].Select(index => remap[index]).ToList(), submesh);
            result.boneWeights = weights.ToArray();
            result.bindposes = source.bindposes;
            result.RecalculateBounds();
            return result;
        }

        // Vertices whose dominant influence is a left arm bone yet sit further than 20 cm from the
        // left arm chain in the bind pose. Real arm geometry never does that; this is the foreign
        // piece that stretches between the torso and the arm.
        private static HashSet<int> LeftArmForeignVertices(
            Transform model,
            SkinnedMeshRenderer body)
        {
            var mesh = body.sharedMesh;
            var chain = new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" };
            var chainIndices = chain
                .Select(name => Array.FindIndex(
                    body.bones, item => item != null && item.name == name))
                .ToArray();
            if (chainIndices.Any(index => index < 0))
                throw new InvalidOperationException(
                    "The slot-6 body is not skinned to the whole left arm chain.");
            var bindposes = mesh.bindposes;
            var chainPoints = chainIndices
                .Select(index => (Vector3)bindposes[index].inverse.GetColumn(3))
                .ToArray();
            var dominantSet = new HashSet<int>(chainIndices);
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var values = new HashSet<int>();
            for (var index = 0; index < vertices.Length; index++)
            {
                if (!dominantSet.Contains(weights[index].boneIndex0)) continue;
                if (weights[index].weight0 < 0.5f) continue;
                var distance = float.PositiveInfinity;
                for (var segment = 0; segment + 1 < chainPoints.Length; segment++)
                    distance = Mathf.Min(
                        distance,
                        DistanceToSegment(
                            vertices[index], chainPoints[segment], chainPoints[segment + 1]));
                if (distance > 0.2f) values.Add(index);
            }

            return values;
        }

        // For every triangle, how much its longest edge grows during the loop compared with the
        // bind pose. Geometry that is skinned to a distant bone shows up as a huge ratio.
        private static float[] StretchRatios(
            Transform model,
            SkinnedMeshRenderer body,
            out float maxBindEdge,
            out float maxPeakEdge)
        {
            var mesh = body.sharedMesh;
            var triangles = mesh.triangles;
            var bind = mesh.vertices;
            var count = triangles.Length / 3;
            var bindEdges = new float[count];
            var peakEdges = new float[count];
            for (var index = 0; index < count; index++)
            {
                bindEdges[index] = LongestEdge(
                    bind[triangles[index * 3]],
                    bind[triangles[index * 3 + 1]],
                    bind[triangles[index * 3 + 2]]);
                peakEdges[index] = bindEdges[index];
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            if (clip != null)
            {
                var states = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new TransformState(item)).ToArray();
                var baked = new Mesh();
                try
                {
                    AnimationMode.StartAnimationMode();
                    var frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                    for (var frame = 0; frame <= frames; frame++)
                    {
                        Restore(states);
                        AnimationMode.SampleAnimationClip(
                            model.gameObject, clip, clip.length * frame / frames);
                        body.BakeMesh(baked);
                        var posed = baked.vertices;
                        for (var index = 0; index < count; index++)
                            peakEdges[index] = Mathf.Max(
                                peakEdges[index],
                                LongestEdge(
                                    posed[triangles[index * 3]],
                                    posed[triangles[index * 3 + 1]],
                                    posed[triangles[index * 3 + 2]]));
                    }
                }
                finally
                {
                    if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                    Restore(states);
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            maxBindEdge = 0f;
            maxPeakEdge = 0f;
            var ratios = new float[count];
            for (var index = 0; index < count; index++)
            {
                ratios[index] = bindEdges[index] < 1e-6f
                    ? 1f
                    : peakEdges[index] / bindEdges[index];
                if (ratios[index] <= 2f) continue;
                maxBindEdge = Mathf.Max(maxBindEdge, bindEdges[index]);
                maxPeakEdge = Mathf.Max(maxPeakEdge, peakEdges[index]);
            }

            return ratios;
        }

        private static float LongestEdge(Vector3 a, Vector3 b, Vector3 c) =>
            Mathf.Max(
                Vector3.Distance(a, b),
                Mathf.Max(Vector3.Distance(b, c), Vector3.Distance(c, a)));

        private static bool TouchesBones(BoneWeight weight, ICollection<int> bones)
        {
            bool Matches(int index, float value) => value > 0.001f && bones.Contains(index);
            return Matches(weight.boneIndex0, weight.weight0) ||
                   Matches(weight.boneIndex1, weight.weight1) ||
                   Matches(weight.boneIndex2, weight.weight2) ||
                   Matches(weight.boneIndex3, weight.weight3);
        }

        // Measures how far the loop start and the loop end sit from the static model pose, which
        // decides how smooth the loop boundary will be once the static return tail is appended.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Static Return Boundary")]
        public static void InspectIspant06StaticReturnBoundary()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var report = new StringBuilder();
            report.AppendLine("Ispant06StaticReturnBoundaryInspection");
            report.AppendLine("Clip=" + LoopClipPath +
                              ", Length=" + clip.length.ToString("F6") +
                              ", FrameRate=" + clip.frameRate.ToString("F2"));
            report.AppendLine();
            var rows = new List<(string Path, float StartVsStatic, float EndVsStatic, float StartVsEnd)>();
            var unresolved = new List<string>();
            foreach (var path in RotationPaths(clip))
            {
                var staticBone = staticModel.Find(path);
                if (staticBone == null)
                {
                    unresolved.Add(path);
                    continue;
                }

                var start = EvaluateLocalRotation(clip, path, 0f);
                var end = EvaluateLocalRotation(clip, path, clip.length);
                rows.Add((
                    path,
                    Quaternion.Angle(start, staticBone.localRotation),
                    Quaternion.Angle(end, staticBone.localRotation),
                    Quaternion.Angle(start, end)));
            }

            report.AppendLine("[Per bone, degrees]");
            report.AppendLine("MaxClipStartVsStatic=" +
                              rows.Max(item => item.StartVsStatic).ToString("F4"));
            report.AppendLine("MaxClipEndVsStatic=" +
                              rows.Max(item => item.EndVsStatic).ToString("F4"));
            report.AppendLine("MaxClipStartVsClipEnd=" +
                              rows.Max(item => item.StartVsEnd).ToString("F4"));
            report.AppendLine();
            report.AppendLine("[Largest clip start vs static pose differences]");
            foreach (var row in rows.OrderByDescending(item => item.StartVsStatic).Take(12))
                report.AppendLine(
                    row.Path.Split('/').Last() +
                    ": StartVsStatic=" + row.StartVsStatic.ToString("F4") +
                    ", EndVsStatic=" + row.EndVsStatic.ToString("F4") +
                    ", StartVsEnd=" + row.StartVsEnd.ToString("F4"));
            report.AppendLine();
            report.AppendLine("[Paths with rotation curves that the static model does not have]");
            foreach (var path in unresolved) report.AppendLine(path);
            var absolute = Absolute(StaticReturnBoundaryPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The static return boundary inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06StaticReturnBoundaryInspected" +
                ", Bones=" + rows.Count +
                ", MaxClipStartVsStatic=" + rows.Max(item => item.StartVsStatic).ToString("F4") +
                ", MaxClipEndVsStatic=" + rows.Max(item => item.EndVsStatic).ToString("F4") +
                ", MaxClipStartVsClipEnd=" + rows.Max(item => item.StartVsEnd).ToString("F4") +
                ", Report=" + StaticReturnBoundaryPath + ".");
        }

        // Appends a 0.4 second tail in which every animated bone eases from the sheathing end pose
        // to the static model pose. Existing keys are left untouched, and the sword keeps whatever
        // value it ends the sheathing with, which is the left waist mount.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Static Return Tail")]
        public static void ApplyIspant06StaticReturnTail()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var sourceLength = RequireSourceClip().length;
            if (clip.length > sourceLength + 0.001f)
                throw new InvalidOperationException(
                    "The static return tail is already present; rebuild the loop clip first.");
            var end = clip.length;
            var frames = Mathf.Max(1, Mathf.RoundToInt(StaticReturnTailSeconds * clip.frameRate));
            var report = new StringBuilder();
            report.AppendLine("Ispant06StaticReturnTail");
            report.AppendLine("ClipLengthBefore=" + end.ToString("F6") +
                              ", TailSeconds=" + StaticReturnTailSeconds.ToString("F4") +
                              ", TailFrames=" + frames);
            report.AppendLine();
            var worstRotation = 0f;
            var worstPosition = 0f;
            var bones = 0;
            var held = 0;
            foreach (var path in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.type == typeof(Transform))
                         .Select(item => item.path)
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(item => item, StringComparer.Ordinal)
                         .ToArray())
            {
                var staticBone = staticModel.Find(path);
                var hasRotation = AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x")) != null;
                var hasPosition = AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x")) != null;
                if (staticBone == null) held++;
                else bones++;

                if (hasRotation)
                {
                    var from = EvaluateLocalRotation(clip, path, end);
                    var to = staticBone == null ? from : staticBone.localRotation;
                    if (Quaternion.Dot(from, to) < 0f)
                        to = new Quaternion(-to.x, -to.y, -to.z, -to.w);
                    worstRotation = Mathf.Max(worstRotation, Quaternion.Angle(from, to));
                    var x = new List<Keyframe>();
                    var y = new List<Keyframe>();
                    var z = new List<Keyframe>();
                    var w = new List<Keyframe>();
                    for (var frame = 1; frame <= frames; frame++)
                    {
                        var time = end + StaticReturnTailSeconds * frame / frames;
                        var value = Quaternion.Slerp(
                            from, to, Mathf.SmoothStep(0f, 1f, frame / (float)frames));
                        x.Add(new Keyframe(time, value.x));
                        y.Add(new Keyframe(time, value.y));
                        z.Add(new Keyframe(time, value.z));
                        w.Add(new Keyframe(time, value.w));
                    }

                    AppendTailKeys(clip, path, "m_LocalRotation.x", x);
                    AppendTailKeys(clip, path, "m_LocalRotation.y", y);
                    AppendTailKeys(clip, path, "m_LocalRotation.z", z);
                    AppendTailKeys(clip, path, "m_LocalRotation.w", w);
                }

                if (!hasPosition) continue;
                {
                    var from = EvaluateLocalPosition(clip, path, end);
                    var to = staticBone == null ? from : staticBone.localPosition;
                    worstPosition = Mathf.Max(worstPosition, Vector3.Distance(from, to));
                    var x = new List<Keyframe>();
                    var y = new List<Keyframe>();
                    var z = new List<Keyframe>();
                    for (var frame = 1; frame <= frames; frame++)
                    {
                        var time = end + StaticReturnTailSeconds * frame / frames;
                        var value = Vector3.Lerp(
                            from, to, Mathf.SmoothStep(0f, 1f, frame / (float)frames));
                        x.Add(new Keyframe(time, value.x));
                        y.Add(new Keyframe(time, value.y));
                        z.Add(new Keyframe(time, value.z));
                    }

                    AppendTailKeys(clip, path, "m_LocalPosition.x", x);
                    AppendTailKeys(clip, path, "m_LocalPosition.y", y);
                    AppendTailKeys(clip, path, "m_LocalPosition.z", z);
                }
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            report.AppendLine("ClipLengthAfter=" + clip.length.ToString("F6"));
            report.AppendLine("BonePathsReturnedToStatic=" + bones);
            report.AppendLine("PathsHeldAtSheathEnd=" + held);
            report.AppendLine("MaxRotationTravelInTail=" + worstRotation.ToString("F4") + "deg");
            report.AppendLine("MaxPositionTravelInTail=" + worstPosition.ToString("F6") + "m");
            var absolute = Absolute(StaticReturnTailPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "The static return tail must not dirty CargoRunMvp.");
            Debug.Log(
                "Ispant06StaticReturnTailApplied" +
                ", ClipLength=" + clip.length.ToString("F6") +
                ", TailFrames=" + frames +
                ", BonePathsReturnedToStatic=" + bones +
                ", PathsHeldAtSheathEnd=" + held +
                ", MaxRotationTravelInTail=" + worstRotation.ToString("F4") +
                ", MaxPositionTravelInTail=" + worstPosition.ToString("F6") +
                ", SceneDirty=" + scene.isDirty + ".");
        }

        private static void AppendTailKeys(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyCollection<Keyframe> extra)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) return;
            var existing = curve.length;
            var keys = curve.keys.ToList();
            keys.AddRange(extra);
            var result = new AnimationCurve(keys.ToArray());
            for (var index = Mathf.Max(0, existing - 1); index < result.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    result, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    result, index, AnimationUtility.TangentMode.Linear);
            }

            AnimationUtility.SetEditorCurve(clip, binding, result);
        }

        private static Vector3 EvaluateLocalPosition(AnimationClip clip, string path, float time)
        {
            float Evaluate(string property)
            {
                var curve = AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
                if (curve == null)
                    throw new InvalidOperationException(
                        "A required position curve is missing: " + path + "|" + property);
                return curve.Evaluate(time);
            }

            return new Vector3(
                Evaluate("m_LocalPosition.x"),
                Evaluate("m_LocalPosition.y"),
                Evaluate("m_LocalPosition.z"));
        }

        private static IEnumerable<string> RotationPaths(AnimationClip clip) =>
            AnimationUtility.GetCurveBindings(clip)
                .Where(item => item.type == typeof(Transform) &&
                               item.propertyName == "m_LocalRotation.x")
                .Select(item => item.path)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal);

        // Step one of the user requested order: detach the leftover waist hilt from the left arm by
        // rebinding it to Hips, so the left arm mesh stops being dragged by it. The geometry is
        // still present after this step.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Waist Hilt Separation")]
        public static void ApplyIspant06WaistHiltSeparation()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var island = RequireWaistHiltIsland(model, body);
            var hipsIndex = Array.FindIndex(
                body.bones, item => item != null && item.name == "Hips");
            if (hipsIndex < 0)
                throw new InvalidOperationException(
                    "The slot-6 body is not skinned to a Hips bone.");
            var before = MaxLeftArmDrivenDisplacement(model, body, island);
            var mesh = BuildDerivedMesh(body.sharedMesh, null, new HashSet<int>(island), hipsIndex);
            ReplaceBodyMesh(
                body, mesh, WaistHiltSeparatedMeshPath, "Ispant_06_BodyWaistHiltSeparated");
            var after = MaxLeftArmDrivenDisplacement(model, body, island);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the waist hilt separation.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06WaistHiltSeparated" +
                ", IslandVertices=" + island.Count +
                ", ReboundTo=Hips" +
                ", LeftArmDrivenDisplacementBefore=" + before.ToString("F6") +
                ", LeftArmDrivenDisplacementAfter=" + after.ToString("F6") +
                ", BodyVertices=" + body.sharedMesh.vertexCount +
                ", Mesh=" + WaistHiltSeparatedMeshPath + ".");
        }

        // Step two: remove the detached waist hilt geometry from the body mesh entirely.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Waist Hilt Removal")]
        public static void ApplyIspant06WaistHiltRemoval()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var source = body.sharedMesh;
            var islands = ConnectedIslands(source);
            var largest = islands.OrderByDescending(item => item.Count).First();
            var vertices = source.vertices;
            var hipsLocalY = model.InverseTransformPoint(RequireBone(model, "Hips").position).y;
            var hipsIndex = Array.FindIndex(
                body.bones, item => item != null && item.name == "Hips");
            var matches = islands
                .Where(island => island != largest && island.Count >= 64)
                .Where(island => island.All(index => source.boneWeights[index].boneIndex0 == hipsIndex))
                .Where(island =>
                {
                    var centre = island.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) /
                                 island.Count;
                    return model.InverseTransformPoint(
                        body.transform.TransformPoint(centre)).y < hipsLocalY;
                })
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Run the waist hilt separation first; expected one separated hilt island but " +
                    "found " + matches.Length + ".");
            var island = matches[0];
            var beforeVertices = source.vertexCount;
            var beforeTriangles = source.triangles.Length / 3;
            var mesh = BuildDerivedMesh(source, new HashSet<int>(island), null, 0);
            var afterVertices = mesh.vertexCount;
            var afterTriangles = mesh.triangles.Length / 3;
            ReplaceBodyMesh(
                body, mesh, WaistHiltRemovedMeshPath, "Ispant_06_BodyWaistHiltRemoved");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the waist hilt removal.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06WaistHiltRemoved" +
                ", RemovedVertices=" + (beforeVertices - afterVertices) +
                ", RemovedTriangles=" + (beforeTriangles - afterTriangles) +
                ", BodyVertices=" + afterVertices +
                ", BodyTriangles=" + afterTriangles +
                ", BindPosesPreserved=" +
                (mesh.bindposes.Length == source.bindposes.Length) +
                ", Mesh=" + WaistHiltRemovedMeshPath + ".");
        }

        // How far the given vertices travel when the loop drives the left arm. A hilt bound to the
        // shoulder moves a lot; a hilt bound to the hips barely moves.
        private static float MaxLeftArmDrivenDisplacement(
            Transform model,
            SkinnedMeshRenderer body,
            IReadOnlyCollection<int> island)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            if (clip == null) return -1f;
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var baked = new Mesh();
            var reference = new Vector3[island.Count];
            var worst = 0f;
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(states);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * frame / frameCount);
                    body.BakeMesh(baked);
                    var vertices = baked.vertices;
                    var position = 0;
                    foreach (var index in island)
                    {
                        var local = model.InverseTransformPoint(
                            body.transform.TransformPoint(vertices[index]));
                        if (frame == 0) reference[position] = local;
                        else worst = Mathf.Max(worst, Vector3.Distance(reference[position], local));
                        position++;
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
                UnityEngine.Object.DestroyImmediate(baked);
            }

            return worst;
        }

        // Lists the connected islands of the slot-6 body mesh with their model-space centre and the
        // bones that drive them, so the leftover waist hilt can be identified without guessing.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Waist Hilt")]
        public static void InspectIspant06WaistHilt()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var mesh = body.sharedMesh;
            var islands = ConnectedIslands(mesh);
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bones = body.bones;
            var hips = RequireBone(model, "Hips");
            var leftArmNames = new HashSet<string>(
                new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand" }, StringComparer.Ordinal);
            var report = new StringBuilder();
            report.AppendLine("Ispant06WaistHiltInspection");
            report.AppendLine("BodyMesh=" + AssetDatabase.GetAssetPath(mesh));
            report.AppendLine("Vertices=" + mesh.vertexCount +
                              ", Triangles=" + (mesh.triangles.Length / 3) +
                              ", Islands=" + islands.Count);
            report.AppendLine();
            report.AppendLine("[Islands, largest first]");
            foreach (var island in islands.OrderByDescending(item => item.Count).Take(24))
            {
                var centre = island.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) /
                             island.Count;
                var worldCentre = body.transform.TransformPoint(centre);
                var localCentre = model.InverseTransformPoint(worldCentre);
                var hipsLocal = model.InverseTransformPoint(hips.position);
                var boneUse = new Dictionary<string, int>(StringComparer.Ordinal);
                var leftArmVertices = 0;
                foreach (var index in island)
                {
                    var dominant = weights[index].boneIndex0;
                    var name = dominant >= 0 && dominant < bones.Length && bones[dominant] != null
                        ? bones[dominant].name
                        : "none";
                    boneUse[name] = boneUse.TryGetValue(name, out var count) ? count + 1 : 1;
                    if (HasLeftArmWeight(weights[index], bones, leftArmNames)) leftArmVertices++;
                }

                var top = boneUse.OrderByDescending(item => item.Value).Take(4)
                    .Select(item => item.Key + ":" + item.Value);
                report.AppendLine(
                    "Island vertices=" + island.Count +
                    ", ModelSpaceCentre=(" + localCentre.x.ToString("F4") + ", " +
                    localCentre.y.ToString("F4") + ", " + localCentre.z.ToString("F4") + ")" +
                    ", SideOfHips=" + (localCentre.x > hipsLocal.x ? "modelPlusX" : "modelMinusX") +
                    ", LeftArmWeightedVertices=" + leftArmVertices +
                    ", DominantBones=" + string.Join("|", top));
            }

            var absolute = Absolute(WaistHiltDiagnosisPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The waist hilt inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06WaistHiltInspected" +
                ", Islands=" + islands.Count +
                ", Report=" + WaistHiltDiagnosisPath + ".");
        }

        // The leftover waist hilt is the one island, outside the main body, that sits below the
        // hips and is driven entirely by LeftShoulder. Anything else stays untouched.
        private static List<int> RequireWaistHiltIsland(
            Transform model,
            SkinnedMeshRenderer body)
        {
            var mesh = body.sharedMesh;
            var islands = ConnectedIslands(mesh);
            var largest = islands.OrderByDescending(item => item.Count).First();
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bones = body.bones;
            var hipsLocalY = model.InverseTransformPoint(RequireBone(model, "Hips").position).y;
            var shoulderIndex = Array.FindIndex(
                bones, item => item != null && item.name == "LeftShoulder");
            if (shoulderIndex < 0)
                throw new InvalidOperationException(
                    "The slot-6 body is not skinned to a LeftShoulder bone.");
            var matches = islands
                .Where(island => island != largest && island.Count >= 64)
                .Where(island => island.All(index => weights[index].boneIndex0 == shoulderIndex))
                .Where(island =>
                {
                    var centre = island.Aggregate(Vector3.zero, (sum, index) => sum + vertices[index]) /
                                 island.Count;
                    return model.InverseTransformPoint(
                        body.transform.TransformPoint(centre)).y < hipsLocalY;
                })
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected exactly one leftover waist hilt island but found " + matches.Length + ".");
            return matches[0];
        }

        // Copies every vertex attribute of the source mesh, optionally dropping vertices and
        // optionally rebinding the given vertices to a single bone.
        private static Mesh BuildDerivedMesh(
            Mesh source,
            ICollection<int> dropped,
            ICollection<int> rebound,
            int reboundBoneIndex)
        {
            var keep = new int[source.vertexCount];
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var colors = new List<Color32>();
            var weights = new List<BoneWeight>();
            var sourceVertices = source.vertices;
            var sourceNormals = source.normals;
            var sourceTangents = source.tangents;
            var sourceUv = source.uv;
            var sourceUv2 = source.uv2;
            var sourceColors = source.colors32;
            var sourceWeights = source.boneWeights;
            for (var index = 0; index < source.vertexCount; index++)
            {
                if (dropped != null && dropped.Contains(index))
                {
                    keep[index] = -1;
                    continue;
                }

                keep[index] = vertices.Count;
                vertices.Add(sourceVertices[index]);
                if (sourceNormals.Length == source.vertexCount) normals.Add(sourceNormals[index]);
                if (sourceTangents.Length == source.vertexCount) tangents.Add(sourceTangents[index]);
                if (sourceUv.Length == source.vertexCount) uv.Add(sourceUv[index]);
                if (sourceUv2.Length == source.vertexCount) uv2.Add(sourceUv2[index]);
                if (sourceColors.Length == source.vertexCount) colors.Add(sourceColors[index]);
                var weight = sourceWeights[index];
                if (rebound != null && rebound.Contains(index))
                    weight = new BoneWeight
                    {
                        boneIndex0 = reboundBoneIndex,
                        weight0 = 1f,
                        boneIndex1 = 0,
                        weight1 = 0f,
                        boneIndex2 = 0,
                        weight2 = 0f,
                        boneIndex3 = 0,
                        weight3 = 0f
                    };
                weights.Add(weight);
            }

            var result = new Mesh { indexFormat = source.indexFormat };
            result.SetVertices(vertices);
            if (normals.Count == vertices.Count) result.SetNormals(normals);
            if (tangents.Count == vertices.Count) result.SetTangents(tangents);
            if (uv.Count == vertices.Count) result.SetUVs(0, uv);
            if (uv2.Count == vertices.Count) result.SetUVs(1, uv2);
            if (colors.Count == vertices.Count) result.SetColors(colors);
            result.subMeshCount = source.subMeshCount;
            for (var submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                var triangles = source.GetTriangles(submesh);
                var kept = new List<int>();
                for (var index = 0; index + 2 < triangles.Length; index += 3)
                {
                    var a = keep[triangles[index]];
                    var b = keep[triangles[index + 1]];
                    var c = keep[triangles[index + 2]];
                    if (a < 0 || b < 0 || c < 0) continue;
                    kept.Add(a);
                    kept.Add(b);
                    kept.Add(c);
                }

                result.SetTriangles(kept, submesh);
            }

            result.boneWeights = weights.ToArray();
            result.bindposes = source.bindposes;
            result.RecalculateBounds();
            return result;
        }

        private static void ReplaceBodyMesh(
            SkinnedMeshRenderer body,
            Mesh mesh,
            string assetPath,
            string meshName)
        {
            mesh.name = meshName;
            // Copying into an existing mesh asset leaves stale buffers behind when the vertex
            // count changes, which makes the SkinnedMeshRenderer refuse to draw. Always write a
            // fresh asset instead.
            if (AssetDatabase.LoadAssetAtPath<Mesh>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(mesh, assetPath);

            EditorUtility.SetDirty(mesh);
            body.sharedMesh = mesh;
            EditorUtility.SetDirty(body);
            AssetDatabase.SaveAssets();
        }

        private static bool HasLeftArmWeight(
            BoneWeight weight,
            IReadOnlyList<Transform> bones,
            ICollection<string> names)
        {
            bool Matches(int index, float value) =>
                value > 0.001f && index >= 0 && index < bones.Count && bones[index] != null &&
                names.Contains(bones[index].name);

            return Matches(weight.boneIndex0, weight.weight0) ||
                   Matches(weight.boneIndex1, weight.weight1) ||
                   Matches(weight.boneIndex2, weight.weight2) ||
                   Matches(weight.boneIndex3, weight.weight3);
        }

        // Groups mesh vertices that share triangles, welding by position so that duplicated
        // seam vertices do not split one solid piece into many.
        private static List<List<int>> ConnectedIslands(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var welded = new Dictionary<Vector3Int, int>();
            var representative = new int[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var key = new Vector3Int(
                    Mathf.RoundToInt(vertices[index].x * 100000f),
                    Mathf.RoundToInt(vertices[index].y * 100000f),
                    Mathf.RoundToInt(vertices[index].z * 100000f));
                if (!welded.TryGetValue(key, out var value))
                {
                    value = index;
                    welded.Add(key, value);
                }

                representative[index] = value;
            }

            var parent = Enumerable.Range(0, vertices.Length).ToArray();

            int Find(int value)
            {
                while (parent[value] != value)
                {
                    parent[value] = parent[parent[value]];
                    value = parent[value];
                }

                return value;
            }

            void Union(int left, int right)
            {
                var a = Find(left);
                var b = Find(right);
                if (a != b) parent[a] = b;
            }

            for (var index = 0; index < vertices.Length; index++)
                Union(index, representative[index]);
            var triangles = mesh.triangles;
            for (var index = 0; index + 2 < triangles.Length; index += 3)
            {
                Union(triangles[index], triangles[index + 1]);
                Union(triangles[index + 1], triangles[index + 2]);
            }

            var groups = new Dictionary<int, List<int>>();
            for (var index = 0; index < vertices.Length; index++)
            {
                var root = Find(index);
                if (!groups.TryGetValue(root, out var list))
                {
                    list = new List<int>();
                    groups.Add(root, list);
                }

                list.Add(index);
            }

            return groups.Values.ToList();
        }

        // One review sheet for the hand sword: full body on top, right-hand grip close-up below.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Hand Sword Grip Review")]
        public static void CaptureIspant06HandSwordGripReview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var sword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandSwordName);
            var swordMesh = sword.GetComponent<MeshFilter>()?.sharedMesh ??
                            throw new InvalidOperationException(
                                "The slot-6 hand sword mesh is missing.");
            var gripLocal = CalculateSwordGripCenter(swordMesh);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var destination = Absolute(HandSwordReviewPath);
            var phases = new[] { 0f, 0.14f, 0.28f, 0.42f, 0.56f, 0.70f, 0.84f, 0.98f };
            const int panelWidth = 420;
            const int panelHeight = 520;
            const int columns = 8;
            const int captureLayer = 30;
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelWidth * columns, panelHeight * 2, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06HandSwordGripReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * phases[column]);
                    FrameCamera(camera, body.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 1, panelWidth, panelHeight);
                    FrameGripCamera(
                        camera, sword.transform.TransformPoint(gripLocal), body.bounds.size.y);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 0, panelWidth, panelHeight);
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The hand sword review changed the scene dirty state.");
            Debug.Log(
                "Ispant06HandSwordGripReviewCaptured" +
                ", TopRow=FullBodyEightPhases" +
                ", BottomRow=RightHandGripCloseEightPhases" +
                ", Image=" + HandSwordReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        // Measures the hand sword result without changing anything: how rigid the grip is, how
        // close the hilt sits to the visible palm, how far the blade stays from the torso and the
        // right arm, and whether the loop ends on the left waist mount.
        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Slot 06 Hand Sword Clearance")]
        public static void InspectIspant06HandSwordClearance()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var sword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandSwordName);
            var swordMesh = sword.GetComponent<MeshFilter>()?.sharedMesh ??
                            throw new InvalidOperationException(
                                "The slot-6 hand sword mesh is missing.");
            var rightHand = RequireBone(model, "RightHand");
            var gripLocal = CalculateSwordGripCenter(swordMesh);
            var tipLocal = new Vector3(
                swordMesh.bounds.max.x, swordMesh.bounds.center.y, swordMesh.bounds.center.z);
            // The hilt is meant to sit inside the hand, and the guard is far wider than the blade,
            // so the interference test uses the blade region only, detected from the mesh itself.
            var bladeStartLocal = DetectBladeStart(swordMesh, gripLocal, out var bladeHalfWidth);
            var swordVertices = swordMesh.vertices;
            var bladeSamples = Enumerable.Range(0, swordVertices.Length)
                .Where(index => swordVertices[index].x >= bladeStartLocal.x)
                .Where((_, position) => position % 3 == 0)
                .ToArray();
            if (bladeSamples.Length < 16)
                throw new InvalidOperationException(
                    "The detected blade region has too few vertices to measure clearance.");
            var handIndices = RightHandWeightedVertexIndices(body, rightHand);
            var torsoBones = new[] { "Hips", "Spine", "Spine01", "Spine02", "Neck", "Head" };
            var armBones = new[] { "RightShoulder", "RightArm", "RightForeArm" };
            var torsoSet = BoneIndexSet(body, torsoBones);
            var armSet = BoneIndexSet(body, armBones);
            var weights = body.sharedMesh.boneWeights;
            var torsoSamples = new List<int>();
            var armSamples = new List<int>();
            for (var index = 0; index < weights.Length; index += 8)
            {
                var dominant = weights[index].boneIndex0;
                if (torsoSet.Contains(dominant)) torsoSamples.Add(index);
                else if (armSet.Contains(dominant)) armSamples.Add(index);
            }

            var report = new StringBuilder();
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var baked = new Mesh();
            var minTorso = float.PositiveInfinity;
            var minArm = float.PositiveInfinity;
            var minTorsoPhase = 0f;
            var minArmPhase = 0f;
            var gripWindowTorso = float.PositiveInfinity;
            var gripWindowArm = float.PositiveInfinity;
            var gripWindowTorsoPhase = 0f;
            var gripWindowArmPhase = 0f;
            var previousGrip = Vector3.zero;
            var hasPreviousGrip = false;
            var maxGripStep = 0f;
            var maxGripStepPhase = 0f;
            var maxGrip = 0f;
            var maxMountDrift = 0f;
            var waistPositionError = 0f;
            var waistAngleError = 0f;
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                var mountReference = Quaternion.identity;
                var hasMount = false;
                var motionSeconds = RequireSourceClip().length;
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(states);
                    var time = clip.length * frame / frameCount;
                    var phase = time / motionSeconds;
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, time);
                    body.BakeMesh(baked);
                    var vertices = baked.vertices;
                    var grip = sword.transform.TransformPoint(bladeStartLocal);
                    var tip = sword.transform.TransformPoint(tipLocal);
                    var hilt = sword.transform.TransformPoint(gripLocal);
                    if (hasPreviousGrip)
                    {
                        var step = Vector3.Distance(previousGrip, hilt);
                        if (step > maxGripStep)
                        {
                            maxGripStep = step;
                            maxGripStepPhase = phase;
                        }
                    }

                    previousGrip = hilt;
                    hasPreviousGrip = true;
                    // The bake keeps the sword on the hand from the first frame until the waist
                    // blend begins, so the grip window spans that whole span.
                    var inGripWindow = phase <= SwordSheathPositionStartRatio;
                    var bladeWorld = bladeSamples
                        .Select(index => sword.transform.TransformPoint(swordVertices[index]))
                        .ToArray();

                    void Measure(
                        IReadOnlyCollection<int> samples,
                        ref float windowMinimum,
                        ref float windowPhase,
                        ref float loopMinimum,
                        ref float loopPhase)
                    {
                        foreach (var index in samples)
                        {
                            var point = body.transform.TransformPoint(vertices[index]);
                            var distance = float.PositiveInfinity;
                            foreach (var bladePoint in bladeWorld)
                                distance = Mathf.Min(distance, Vector3.Distance(point, bladePoint));
                            if (inGripWindow && distance < windowMinimum)
                            {
                                windowMinimum = distance;
                                windowPhase = phase;
                            }

                            if (distance >= loopMinimum) continue;
                            loopMinimum = distance;
                            loopPhase = phase;
                        }
                    }

                    Measure(
                        torsoSamples, ref gripWindowTorso, ref gripWindowTorsoPhase,
                        ref minTorso, ref minTorsoPhase);
                    Measure(
                        armSamples, ref gripWindowArm, ref gripWindowArmPhase,
                        ref minArm, ref minArmPhase);

                    if (inGripWindow)
                    {
                        var palm = VisibleRightPalmWorld(body, rightHand, handIndices, baked);
                        maxGrip = Mathf.Max(maxGrip, Vector3.Distance(hilt, palm));
                        var mount = Quaternion.Inverse(rightHand.rotation) * sword.transform.rotation;
                        if (!hasMount)
                        {
                            mountReference = mount;
                            hasMount = true;
                        }
                        else
                        {
                            maxMountDrift = Mathf.Max(
                                maxMountDrift, Quaternion.Angle(mountReference, mount));
                        }
                    }

                    if (frame != frameCount) continue;
                    var staticSword = staticModel.GetComponentsInChildren<MeshRenderer>(true)
                        .Single(item => item.GetComponent<MeshFilter>()?.sharedMesh == swordMesh);
                    var staticMatrix =
                        staticModel.worldToLocalMatrix * staticSword.localToWorldMatrix;
                    DecomposeTrs(staticMatrix, out var waistPosition, out var waistRotation, out _);
                    waistPositionError = Vector3.Distance(
                        sword.transform.localPosition, waistPosition);
                    waistAngleError = Quaternion.Angle(
                        sword.transform.localRotation, waistRotation);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
                UnityEngine.Object.DestroyImmediate(baked);
            }

            var swordPath = AnimationUtility.CalculateTransformPath(sword.transform, model);
            var swordCurveCount = AnimationUtility.GetCurveBindings(clip)
                .Count(item => item.path == swordPath);
            report.AppendLine("Ispant06HandSwordClearanceInspection");
            report.AppendLine("SwordPath=" + swordPath);
            report.AppendLine("SwordRendererEnabled=" + sword.enabled);
            report.AppendLine("SwordCurveBindings=" + swordCurveCount);
            report.AppendLine("TorsoSampleCount=" + torsoSamples.Count +
                              ", RightArmSampleCount=" + armSamples.Count);
            report.AppendLine();
            report.AppendLine("[Grip]");
            report.AppendLine("MaxHiltToVisiblePalmDistance=" + maxGrip.ToString("F6") + "m");
            report.AppendLine("MaxHandRelativeMountDrift=" + maxMountDrift.ToString("F6") + "deg");
            report.AppendLine("MaxHiltStepBetweenFrames=" + maxGripStep.ToString("F6") +
                              "m at phase " + maxGripStepPhase.ToString("F4"));
            var bladeHalfThickness = bladeHalfWidth * Mathf.Max(
                sword.transform.lossyScale.y, sword.transform.lossyScale.z);
            report.AppendLine();
            report.AppendLine("[Blade clearance]");
            report.AppendLine(
                "Surface to surface: the minimum distance between the blade mesh vertices and " +
                "the body vertices. The hilt and the hand that holds it are excluded.");
            report.AppendLine("BladeStartLocalX=" + bladeStartLocal.x.ToString("F6") +
                              ", TipLocalX=" + tipLocal.x.ToString("F6") +
                              ", BladeSampleCount=" + bladeSamples.Length +
                              ", BladeHalfWidth=" + bladeHalfThickness.ToString("F6") + "m");
            report.AppendLine("GripWindowMinToTorso=" + gripWindowTorso.ToString("F6") +
                              "m at phase " + gripWindowTorsoPhase.ToString("F4"));
            report.AppendLine("GripWindowMinToRightArm=" + gripWindowArm.ToString("F6") +
                              "m at phase " + gripWindowArmPhase.ToString("F4"));
            report.AppendLine("WholeLoopMinToTorso=" + minTorso.ToString("F6") +
                              "m at phase " + minTorsoPhase.ToString("F4"));
            report.AppendLine("WholeLoopMinToRightArm=" + minArm.ToString("F6") +
                              "m at phase " + minArmPhase.ToString("F4"));
            report.AppendLine();
            report.AppendLine("[Left waist mount at the loop end]");
            report.AppendLine("PositionError=" + waistPositionError.ToString("F6") + "m");
            report.AppendLine("RotationError=" + waistAngleError.ToString("F6") + "deg");
            var absolute = Absolute(HandSwordClearancePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The hand sword clearance inspection changed the scene dirty state.");
            Debug.Log(
                "Ispant06HandSwordClearanceInspected" +
                ", MinBladeToTorso=" + minTorso.ToString("F6") +
                ", MinBladeToRightArm=" + minArm.ToString("F6") +
                ", MaxHiltToPalm=" + maxGrip.ToString("F6") +
                ", MaxMountDrift=" + maxMountDrift.ToString("F6") +
                ", WaistPositionError=" + waistPositionError.ToString("F6") +
                ", WaistRotationError=" + waistAngleError.ToString("F6") +
                ", Report=" + HandSwordClearancePath + ".");
        }

        // Finds where the blade begins by scanning cross sections along the local X axis. The guard
        // is the widest section right after the grip, and the blade starts where the section drops
        // to half of it. Also returns the widest half extent inside the blade region.
        private static Vector3 DetectBladeStart(Mesh mesh, Vector3 gripLocal, out float bladeHalfWidth)
        {
            const int slices = 64;
            var minimum = mesh.bounds.min.x;
            var maximum = mesh.bounds.max.x;
            var step = (maximum - minimum) / slices;
            var radius = new float[slices];
            foreach (var vertex in mesh.vertices)
            {
                var slice = Mathf.Clamp(Mathf.FloorToInt((vertex.x - minimum) / step), 0, slices - 1);
                var extent = Mathf.Max(
                    Mathf.Abs(vertex.y - mesh.bounds.center.y),
                    Mathf.Abs(vertex.z - mesh.bounds.center.z));
                radius[slice] = Mathf.Max(radius[slice], extent);
            }

            // The guard is the widest section in the lower part of the sword; the blade begins at
            // the first section past it that has narrowed to half the guard width.
            var guardLimit = Mathf.RoundToInt(slices * 0.4f);
            var guardSlice = 0;
            for (var slice = 1; slice < guardLimit; slice++)
                if (radius[slice] > radius[guardSlice])
                    guardSlice = slice;
            var guardRadius = radius[guardSlice];
            var start = guardSlice + 1;
            while (start < slices && radius[start] > guardRadius * 0.5f) start++;
            start = Mathf.Min(start, slices - 1);
            bladeHalfWidth = 0f;
            for (var slice = start; slice < slices; slice++)
                bladeHalfWidth = Mathf.Max(bladeHalfWidth, radius[slice]);
            return new Vector3(
                minimum + step * start, mesh.bounds.center.y, mesh.bounds.center.z);
        }

        private static HashSet<int> BoneIndexSet(SkinnedMeshRenderer body, IEnumerable<string> names)
        {
            var wanted = new HashSet<string>(names, StringComparer.Ordinal);
            var values = new HashSet<int>();
            var bones = body.bones;
            for (var index = 0; index < bones.Length; index++)
                if (bones[index] != null && wanted.Contains(bones[index].name))
                    values.Add(index);
            return values;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            var direction = end - start;
            var lengthSquared = direction.sqrMagnitude;
            if (lengthSquared < 1e-10f) return Vector3.Distance(point, start);
            var t = Mathf.Clamp01(Vector3.Dot(point - start, direction) / lengthSquared);
            return Vector3.Distance(point, start + direction * t);
        }

        // Attaches the existing approved long sword to the corrected right hand and lets the loop
        // end put it back on the left waist. The grip is a single rigid hand-relative mount, so the
        // sword angle and position follow the right arm exactly, and the blade is aimed along the
        // forearm at the frame where the hand sits farthest from the torso, which keeps the blade
        // out of the torso and the right arm.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Hand Sword Grip And Waist")]
        public static void ApplyIspant06HandSwordGripAndWaist()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var staticModel = RequireStaticModel(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var sword = model.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.name == HandSwordName);
            var swordMesh = sword.GetComponent<MeshFilter>()?.sharedMesh ??
                            throw new InvalidOperationException(
                                "The slot-6 hand sword mesh is missing.");
            if (swordMesh.bounds.size.x < swordMesh.bounds.size.y ||
                swordMesh.bounds.size.x < swordMesh.bounds.size.z)
                throw new InvalidOperationException(
                    "The approved long sword is expected to run along its local X axis.");
            var staticSword = staticModel.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.GetComponent<MeshFilter>()?.sharedMesh == swordMesh);
            var staticMatrix = staticModel.worldToLocalMatrix * staticSword.localToWorldMatrix;
            DecomposeTrs(
                staticMatrix,
                out var staticLocalPosition,
                out var staticLocalRotation,
                out var staticLocalScale);
            var motionSeconds = RequireSourceClip().length;
            var previousSwordPath = AnimationUtility.CalculateTransformPath(
                sword.transform, model);
            var rightHand = RequireBone(model, "RightHand");
            var rightForeArm = RequireBone(model, "RightForeArm");
            var spine = RequireBone(model, "Spine");

            if (sword.transform.parent != model)
                sword.transform.SetParent(model, false);
            sword.transform.localScale = staticLocalScale;
            sword.enabled = true;
            EditorUtility.SetDirty(sword);
            EditorUtility.SetDirty(sword.transform);

            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            Quaternion handMountRotation;
            float referencePhase;
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                var bestFrame = 0;
                var bestDistance = float.NegativeInfinity;
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var phase = clip.length * frame / frameCount / motionSeconds;
                    if (phase > SwordSheathPositionStartRatio) continue;
                    Restore(states);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * frame / frameCount);
                    var distance = Vector3.Distance(rightHand.position, spine.position);
                    if (distance <= bestDistance) continue;
                    bestDistance = distance;
                    bestFrame = frame;
                }

                referencePhase = clip.length * bestFrame / frameCount / motionSeconds;
                Restore(states);
                AnimationMode.SampleAnimationClip(
                    model.gameObject, clip, motionSeconds * referencePhase);
                var bladeDirection =
                    (rightHand.position - rightForeArm.position).normalized;
                // LookRotation aims local +Z, and the extra yaw turns that into local +X, the
                // pommel to tip axis of the approved sword mesh.
                var desired = Quaternion.LookRotation(bladeDirection, rightHand.up) *
                              Quaternion.Euler(0f, -90f, 0f);
                handMountRotation = Quaternion.Inverse(rightHand.rotation) * desired;
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
            }

            BakeRightHandToStaticWaistSwordCurves(
                model,
                body,
                rightHand,
                sword,
                clip,
                previousSwordPath,
                handMountRotation,
                staticLocalPosition,
                staticLocalRotation,
                motionSeconds);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the slot-6 hand sword grip.");
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Ispant06HandSwordGripAndWaistApplied" +
                ", SwordParent=ModelRoot" +
                ", RendererEnabled=True" +
                ", ClipLength=" + clip.length.ToString("F6") +
                ", SheathingMotionSeconds=" + motionSeconds.ToString("F6") +
                ", GripMountReferencePhase=" + referencePhase.ToString("F4") +
                ", BladeAimedAlongForearm=True" +
                ", HandFollowWindow=0.00-" + SwordSheathPositionStartRatio.ToString("F2") +
                ", WaistBlend=" + SwordSheathPositionStartRatio.ToString("F2") + "-" +
                SwordSheathPositionEndRatio.ToString("F2") +
                ", WaistHeldThroughStaticReturnTail=True" +
                ", VisualVerdict=PendingUserReview.");
        }

        // One review sheet for the right arm roll correction. The top row samples the raw source
        // clip on the same model, so before and after sit in identical camera framing.
        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 06 Right Arm Roll Correction Review")]
        public static void CaptureIspant06RightArmRollCorrectionReview()
        {
            var scene = RequireActiveScene();
            var wasDirty = scene.isDirty;
            var model = RequireModel(scene);
            var body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var sourceClip = RequireSourceClip();
            var destination = Absolute(RollCorrectionReviewPath);
            var phases = new[] { 0f, 0.14f, 0.28f, 0.42f, 0.56f, 0.70f, 0.84f, 0.98f };
            const int panelWidth = 420;
            const int panelHeight = 520;
            const int columns = 8;
            const int rows = 3;
            const int captureLayer = 30;
            var target = new RenderTexture(
                panelWidth, panelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(panelWidth, panelHeight, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                panelWidth * columns, panelHeight * rows, TextureFormat.RGB24, false);
            var cameraObject = new GameObject("Ispant06RightArmRollCorrectionReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.085f, 0.1f, 1f);
            camera.fieldOfView = 30f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.targetTexture = target;
            camera.cullingMask = 1 << captureLayer;
            var oldActive = RenderTexture.active;
            var transforms = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var layers = model.GetComponentsInChildren<Transform>(true)
                .ToDictionary(item => item, item => item.gameObject.layer);
            foreach (var item in layers.Keys)
                item.gameObject.layer = captureLayer;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var column = 0; column < columns; column++)
                {
                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, sourceClip, sourceClip.length * phases[column]);
                    FrameRightArmCamera(camera, body.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 2, panelWidth, panelHeight);

                    Restore(transforms);
                    AnimationMode.SampleAnimationClip(
                        model.gameObject, clip, clip.length * phases[column]);
                    FrameRightArmCamera(camera, body.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 1, panelWidth, panelHeight);
                    FrameCamera(camera, body.bounds);
                    RenderIntoSheet(
                        camera, target, panel, sheet, column, 0, panelWidth, panelHeight);
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(transforms);
                foreach (var item in layers)
                    item.Key.gameObject.layer = item.Value;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The right-arm roll correction review changed the scene dirty state.");
            Debug.Log(
                "Ispant06RightArmRollCorrectionReviewCaptured" +
                ", TopRow=RawSourceRightArmClose" +
                ", MiddleRow=CorrectedRightArmClose" +
                ", BottomRow=CorrectedFullBody" +
                ", Image=" + RollCorrectionReviewPath +
                ", SceneChanged=False, VisualVerdict=PendingUserReview.");
        }

        // Removes the excess axial spin the source motion puts on the right upper arm while every
        // joint of the right arm keeps the exact model-space position it already has. Only the
        // roll about each bone own axis changes, so the arm motion itself is untouched.
        // RightShoulder keeps the raw source curve because its roll already matches the intact
        // left shoulder; the hand goes back to the raw source curve so the wrist follows the
        // corrected forearm instead of being frozen at rest.
        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 06 Right Arm Roll Correction")]
        public static void ApplyIspant06RightArmRollCorrection()
        {
            var scene = RequireActiveScene();
            var model = RequireModel(scene);
            var sourceClip = RequireSourceClip();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                       throw new InvalidOperationException(
                           "The slot-6 embedded sheathing loop clip is missing.");
            var targetSkin = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(item => item.name == "char1");
            var bind = new[] { "Spine", "RightShoulder", "RightArm", "RightForeArm", "RightHand" }
                .ToDictionary(
                    name => name,
                    name => ToModelSpaceBind(model, targetSkin, name),
                    StringComparer.Ordinal);
            var armAxis = RequireBone(model, "RightForeArm").localPosition.normalized;
            var foreArmAxis = RequireBone(model, "RightHand").localPosition.normalized;
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath) ??
                               throw new InvalidOperationException(
                                   "The imported slot-6 sheathing source FBX is missing.");
            var previewScene = EditorSceneManager.NewPreviewScene();
            var sourceObject = PrefabUtility.InstantiatePrefab(
                                   sourcePrefab, previewScene) as GameObject ??
                               throw new InvalidOperationException(
                                   "The slot-6 source FBX could not be instantiated for correction.");
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            var sourceModel = sourceObject.transform;
            var sourceStates = sourceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var armCurves = new QuaternionCurveSet();
            var foreArmCurves = new QuaternionCurveSet();
            var handCurves = new QuaternionCurveSet();
            var removedRoll = 0f;
            var elbowError = 0f;
            var wristError = 0f;
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(
                    1, Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var time = sourceClip.length * frame / frameCount;
                    Restore(sourceStates);
                    AnimationMode.SampleAnimationClip(sourceObject, sourceClip, time);
                    var root = Quaternion.Inverse(sourceModel.rotation);
                    var shoulderBone = RequireBone(sourceModel, "RightShoulder");
                    var armBone = RequireBone(sourceModel, "RightArm");
                    var foreArmBone = RequireBone(sourceModel, "RightForeArm");
                    var handBone = RequireBone(sourceModel, "RightHand");
                    var shoulderWorld = root * shoulderBone.rotation;
                    var armWorld = root * armBone.rotation;
                    var foreArmWorld = root * foreArmBone.rotation;

                    // Upper arm: keep the elbow direction, drop the spin around it.
                    var armRest = shoulderWorld *
                                  (Quaternion.Inverse(bind["RightShoulder"]) * bind["RightArm"]);
                    var armDelta = Quaternion.Inverse(armRest) * armWorld;
                    var armTwist = Twist(armDelta, armAxis);
                    removedRoll = Mathf.Max(
                        removedRoll, Quaternion.Angle(Quaternion.identity, armTwist));
                    var correctedArm = armRest * (armDelta * Quaternion.Inverse(armTwist));

                    // Forearm: swing inside its own rest frame to the unchanged wrist direction,
                    // then re-apply exactly the roll the source gave it. Aiming first and rolling
                    // second keeps both the wrist position and the natural forearm roll intact.
                    var foreArmRestLocal =
                        Quaternion.Inverse(bind["RightArm"]) * bind["RightForeArm"];
                    var foreArmDelta =
                        Quaternion.Inverse(armWorld * foreArmRestLocal) * foreArmWorld;
                    var foreArmRest = correctedArm * foreArmRestLocal;
                    var wristDirection = Quaternion.Inverse(foreArmRest) *
                                         (foreArmWorld * foreArmAxis);
                    var correctedForeArm = foreArmRest *
                                           Quaternion.FromToRotation(foreArmAxis, wristDirection) *
                                           Twist(foreArmDelta, foreArmAxis);

                    elbowError = Mathf.Max(
                        elbowError,
                        Vector3.Angle(correctedArm * armAxis, armWorld * armAxis));
                    wristError = Mathf.Max(
                        wristError,
                        Vector3.Angle(
                            correctedForeArm * foreArmAxis, foreArmWorld * foreArmAxis));

                    armCurves.Add(time, Quaternion.Inverse(shoulderWorld) * correctedArm);
                    foreArmCurves.Add(time, Quaternion.Inverse(correctedArm) * correctedForeArm);
                    handCurves.Add(time, handBone.localRotation);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(sourceStates);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            WriteRotationCurves(clip, model, RequireBone(model, "RightArm"), armCurves);
            WriteRotationCurves(clip, model, RequireBone(model, "RightForeArm"), foreArmCurves);
            WriteRotationCurves(clip, model, RequireBone(model, "RightHand"), handCurves);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "The right-arm roll correction must not dirty CargoRunMvp.");
            Debug.Log(
                "Ispant06RightArmRollCorrectionApplied" +
                ", MaxRemovedUpperArmRoll=" + removedRoll.ToString("F4") +
                ", ElbowDirectionError=" + elbowError.ToString("F6") +
                ", WristDirectionError=" + wristError.ToString("F6") +
                ", RightShoulderCurveUntouched=True" +
                ", RightHandRestoredToRawSource=True" +
                ", SceneDirty=" + scene.isDirty +
                ", VisualVerdict=PendingUserReview.");
        }

        // Bind rotation of one bone expressed in the model root space.
        private static Quaternion ToModelSpaceBind(
            Transform model,
            SkinnedMeshRenderer renderer,
            string boneName)
        {
            var bindRotation = BindRotation(renderer, boneName) ??
                               throw new InvalidOperationException(
                                   "The current body has no bind pose for " + boneName + ".");
            return Quaternion.Inverse(model.rotation) * renderer.transform.rotation * bindRotation;
        }

        // Swing-twist decomposition: the twist part about the given axis.
        private static Quaternion Twist(Quaternion rotation, Vector3 axis)
        {
            var projected = Vector3.Project(
                new Vector3(rotation.x, rotation.y, rotation.z), axis);
            var twist = new Quaternion(projected.x, projected.y, projected.z, rotation.w);
            var magnitude = Mathf.Sqrt(
                twist.x * twist.x + twist.y * twist.y + twist.z * twist.z + twist.w * twist.w);
            if (magnitude < 1e-6f) return Quaternion.identity;
            return new Quaternion(
                twist.x / magnitude, twist.y / magnitude,
                twist.z / magnitude, twist.w / magnitude);
        }

        // Records, per bone, the largest axial roll the sampled pose carries relative to the rest
        // orientation. Roll is the component that spins the mesh around the limb without moving
        // any joint, so a large value on one side only marks an authoring defect.
        private static void AccumulateRoll(
            Transform model,
            IReadOnlyList<string> boneChain,
            IDictionary<string, float> peak,
            IReadOnlyDictionary<string, Quaternion> bindSpace)
        {
            var root = Quaternion.Inverse(model.rotation);
            for (var index = 1; index + 1 < boneChain.Count; index++)
            {
                var name = boneChain[index];
                var bone = RequireBone(model, name);
                var axis = RequireBone(bone, boneChain[index + 1]).localPosition.normalized;
                if (axis.sqrMagnitude < 1e-8f) continue;
                var parentWorld = root * RequireBone(model, boneChain[index - 1]).rotation;
                var restCarried = parentWorld *
                                  (Quaternion.Inverse(bindSpace[boneChain[index - 1]]) *
                                   bindSpace[name]);
                var delta = Quaternion.Inverse(restCarried) * (root * bone.rotation);
                var roll = TwistAngle(delta, axis);
                peak[name] = peak.TryGetValue(name, out var previous)
                    ? Mathf.Max(previous, roll)
                    : roll;
            }
        }

        // Swing-twist decomposition: returns only the twist magnitude about the given axis.
        private static float TwistAngle(Quaternion rotation, Vector3 axis) =>
            Quaternion.Angle(Quaternion.identity, Twist(rotation, axis));

        // Bind pose rotation of one bone in the renderer space, which is what skinning actually
        // uses. Returns null when the renderer does not drive a bone with that name.
        private static Quaternion? BindRotation(SkinnedMeshRenderer renderer, string boneName)
        {
            var bones = renderer.bones;
            var bindposes = renderer.sharedMesh == null
                ? Array.Empty<Matrix4x4>()
                : renderer.sharedMesh.bindposes;
            for (var index = 0; index < bones.Length && index < bindposes.Length; index++)
            {
                if (bones[index] == null || bones[index].name != boneName) continue;
                DecomposeTrs(bindposes[index].inverse, out _, out var rotation, out _);
                return rotation;
            }

            return null;
        }

        // Model-space unit direction between two bones, used to compare limb trajectories
        // between the source rig and the current rig without depending on bone roll.
        private static Vector3 Direction(
            Transform space,
            Transform model,
            string fromName,
            string toName)
        {
            var from = RequireBone(model, fromName).position;
            var to = RequireBone(model, toName).position;
            return Quaternion.Inverse(space.rotation) * (to - from).normalized;
        }

        private static void BakeRightArmRestBasisRotationTransfer(
            Transform model,
            AnimationClip clip)
        {
            var sourceClip = RequireSourceClip();
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath) ??
                               throw new InvalidOperationException(
                                   "The imported slot-6 sheathing source FBX is missing.");
            var previewScene = EditorSceneManager.NewPreviewScene();
            var sourceObject = PrefabUtility.InstantiatePrefab(
                                   sourcePrefab, previewScene) as GameObject ??
                               throw new InvalidOperationException(
                                   "The slot-6 source FBX could not be instantiated for rotation transfer.");
            sourceObject.hideFlags = HideFlags.HideAndDontSave;
            var sourceModel = sourceObject.transform;
            var names = new[] { "RightArm", "RightForeArm", "RightHand" };
            var sourceBones = names.ToDictionary(
                name => name, name => RequireBone(sourceModel, name), StringComparer.Ordinal);
            var targetBones = names.ToDictionary(
                name => name, name => RequireBone(model, name), StringComparer.Ordinal);
            var sourceRest = sourceBones.ToDictionary(
                item => item.Key, item => item.Value.localRotation, StringComparer.Ordinal);
            var targetRest = targetBones.ToDictionary(
                item => item.Key, item => item.Value.localRotation, StringComparer.Ordinal);
            var curves = names.ToDictionary(
                name => name, _ => new QuaternionCurveSet(), StringComparer.Ordinal);
            var sourceStates = sourceModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(
                    1, Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(sourceStates);
                    var time = sourceClip.length * frame / frameCount;
                    AnimationMode.SampleAnimationClip(sourceObject, sourceClip, time);
                    curves["RightArm"].Add(
                        time,
                        TransferLocalRotationThroughRestBasis(
                            sourceRest["RightArm"],
                            targetRest["RightArm"],
                            sourceBones["RightArm"].localRotation));
                    curves["RightForeArm"].Add(
                        time,
                        TransferLocalRotationThroughRestBasis(
                            sourceRest["RightForeArm"],
                            targetRest["RightForeArm"],
                            sourceBones["RightForeArm"].localRotation));
                    // The hand keeps the current model's own rest offset relative to the
                    // corrected forearm, so the source skeleton cannot invert the palm.
                    curves["RightHand"].Add(time, targetRest["RightHand"]);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(sourceStates);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }

            foreach (var name in names)
                WriteRotationCurves(clip, model, targetBones[name], curves[name]);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static Quaternion TransferLocalRotationThroughRestBasis(
            Quaternion sourceRest,
            Quaternion targetRest,
            Quaternion sourceSample) =>
            targetRest * Quaternion.Inverse(sourceRest) * sourceSample;

        private static Quaternion EvaluateLocalRotation(
            AnimationClip clip,
            string path,
            float time)
        {
            float Evaluate(string property)
            {
                var curve = AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
                if (curve == null)
                    throw new InvalidOperationException(
                        "A required rotation curve is missing: " + path + "|" + property);
                return curve.Evaluate(time);
            }

            var value = new Quaternion(
                Evaluate("m_LocalRotation.x"),
                Evaluate("m_LocalRotation.y"),
                Evaluate("m_LocalRotation.z"),
                Evaluate("m_LocalRotation.w"));
            value.Normalize();
            return value;
        }

        private static bool CurvesMatch(AnimationCurve expected, AnimationCurve actual)
        {
            if (expected == null || actual == null ||
                expected.preWrapMode != actual.preWrapMode ||
                expected.postWrapMode != actual.postWrapMode ||
                expected.length != actual.length)
                return false;
            for (var index = 0; index < expected.length; index++)
            {
                var left = expected[index];
                var right = actual[index];
                if (!Mathf.Approximately(left.time, right.time) ||
                    !Mathf.Approximately(left.value, right.value) ||
                    !Mathf.Approximately(left.inTangent, right.inTangent) ||
                    !Mathf.Approximately(left.outTangent, right.outTangent) ||
                    !Mathf.Approximately(left.inWeight, right.inWeight) ||
                    !Mathf.Approximately(left.outWeight, right.outWeight) ||
                    left.weightedMode != right.weightedMode)
                    return false;
            }
            return true;
        }

        private static void WriteRotationCurves(
            AnimationClip clip,
            Transform model,
            Transform target,
            QuaternionCurveSet curves)
        {
            var path = AnimationUtility.CalculateTransformPath(target, model);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path &&
                                        item.type == typeof(Transform) &&
                                        item.propertyName.StartsWith(
                                            "m_LocalRotation.", StringComparison.Ordinal))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
            curves.Write(clip, path);
        }

        private static Quaternion CaptureSwordHandMountRotation(
            Transform model,
            Transform rightHand,
            Transform sword,
            AnimationClip clip,
            float time)
        {
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(model.gameObject, clip, time);
                return Quaternion.Inverse(rightHand.rotation) * sword.rotation;
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
            }
        }

        private static void BakeRightHandToStaticWaistSwordCurves(
            Transform model,
            SkinnedMeshRenderer body,
            Transform rightHand,
            MeshRenderer sword,
            AnimationClip clip,
            string previousSwordPath,
            Quaternion handMountRotation,
            Vector3 staticLocalPosition,
            Quaternion staticLocalRotation,
            float motionSeconds)
        {
            var mesh = sword.GetComponent<MeshFilter>()?.sharedMesh ??
                       throw new InvalidOperationException("The slot-6 hand sword mesh is missing.");
            var gripLocal = CalculateSwordGripCenter(mesh);
            var handVertexIndices = RightHandWeightedVertexIndices(body, rightHand);
            var positionCurves = new VectorCurveSet();
            var rotationCurves = new QuaternionCurveSet();
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var baked = new Mesh();
            try
            {
                AnimationMode.StartAnimationMode();
                Restore(states);
                AnimationMode.SampleAnimationClip(
                    model.gameObject,
                    clip,
                    motionSeconds * 0.2f);
                // Capture the visible palm offset once; per-frame BakeMesh drift must not move the hilt.
                var visiblePalmLocal = rightHand.InverseTransformPoint(
                    VisibleRightPalmWorld(body, rightHand, handVertexIndices, baked));
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(states);
                    var time = clip.length * frame / frameCount;
                    // Phase runs against the sheathing motion, so the appended static return tail
                    // simply stays past the end of the blend and keeps the sword on the waist.
                    var phase = time / motionSeconds;
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, time);
                    // Rotation and position share one window and both are anchored on the hilt
                    // grip point. Blending them over different windows used to swing the blade
                    // out into the air before the position caught up.
                    var handRotation = rightHand.rotation * handMountRotation;
                    var handGrip = rightHand.TransformPoint(visiblePalmLocal);
                    var waistRotation = model.rotation * staticLocalRotation;
                    var scaledGrip = Vector3.Scale(gripLocal, sword.transform.lossyScale);
                    var waistGrip = model.TransformPoint(staticLocalPosition) +
                                    waistRotation * scaledGrip;
                    // The sword is in the hand from the very first frame, so nothing has to fly
                    // back from the waist at the loop restart.
                    var waistBlend = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(
                            SwordSheathPositionStartRatio,
                            SwordSheathPositionEndRatio,
                            phase));
                    var blendedRotation = Quaternion.Slerp(handRotation, waistRotation, waistBlend);
                    var blendedGrip = Vector3.Lerp(handGrip, waistGrip, waistBlend);
                    sword.transform.rotation = blendedRotation;
                    sword.transform.position = blendedGrip - blendedRotation * scaledGrip;
                    positionCurves.Add(time, sword.transform.localPosition);
                    rotationCurves.Add(time, sword.transform.localRotation);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
                UnityEngine.Object.DestroyImmediate(baked);
            }

            var path = AnimationUtility.CalculateTransformPath(sword.transform, model);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.type == typeof(Transform) &&
                                        (item.path == previousSwordPath || item.path == path))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
            positionCurves.Write(clip, path);
            rotationCurves.Write(clip, path);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled"),
                AnimationCurve.Constant(0f, clip.length, 1f));
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static void BakeRightArmSwordCurves(
            Transform model,
            SkinnedMeshRenderer body,
            Transform rightHand,
            Transform rightForeArm,
            Transform rightShoulder,
            Transform spine,
            MeshRenderer sword,
            AnimationClip clip)
        {
            var mesh = sword.GetComponent<MeshFilter>()?.sharedMesh ??
                       throw new InvalidOperationException("The slot-6 hand sword mesh is missing.");
            var gripLocal = CalculateSwordGripCenter(mesh);
            var bladeLocal = (mesh.bounds.center - gripLocal).normalized;
            if (bladeLocal.sqrMagnitude < 0.9f)
                throw new InvalidOperationException(
                    "The slot-6 sword mesh cannot establish its blade direction.");
            var localUpSeed = Mathf.Abs(Vector3.Dot(bladeLocal, Vector3.up)) < 0.9f
                ? Vector3.up
                : Vector3.forward;
            var localUp = Vector3.ProjectOnPlane(localUpSeed, bladeLocal).normalized;
            var localFrame = Quaternion.LookRotation(bladeLocal, localUp);
            var handVertexIndices = RightHandWeightedVertexIndices(body, rightHand);
            var positionCurves = new VectorCurveSet();
            var rotationCurves = new QuaternionCurveSet();
            var states = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var baked = new Mesh();
            try
            {
                AnimationMode.StartAnimationMode();
                var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    Restore(states);
                    var time = clip.length * frame / frameCount;
                    AnimationMode.SampleAnimationClip(model.gameObject, clip, time);
                    var foreArmDirection = (rightHand.position - rightForeArm.position).normalized;
                    var outward = Vector3.ProjectOnPlane(
                        rightShoulder.position - spine.position, model.up).normalized;
                    var armVariation = Vector3.ProjectOnPlane(
                        foreArmDirection, outward).normalized;
                    var bladeDirection =
                        (outward * 0.9f + armVariation * 0.25f - model.up * 0.35f).normalized;
                    var worldUp = Vector3.ProjectOnPlane(model.forward, bladeDirection).normalized;
                    if (worldUp.sqrMagnitude < 0.9f)
                        worldUp = Vector3.ProjectOnPlane(model.up, bladeDirection).normalized;
                    sword.transform.rotation =
                        Quaternion.LookRotation(bladeDirection, worldUp) *
                        Quaternion.Inverse(localFrame);
                    var visiblePalm = VisibleRightPalmWorld(
                        body, rightHand, handVertexIndices, baked);
                    sword.transform.position =
                        visiblePalm - sword.transform.TransformVector(gripLocal);
                    positionCurves.Add(time, sword.transform.localPosition);
                    rotationCurves.Add(time, sword.transform.localRotation);
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Restore(states);
                UnityEngine.Object.DestroyImmediate(baked);
            }

            var path = AnimationUtility.CalculateTransformPath(sword.transform, model);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path && item.type == typeof(Transform))
                         .ToArray())
                AnimationUtility.SetEditorCurve(clip, binding, null);
            positionCurves.Write(clip, path);
            rotationCurves.Write(clip, path);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(MeshRenderer), "m_Enabled"),
                AnimationCurve.Constant(0f, clip.length, 1f));
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        private static Vector3 CalculateSwordGripCenter(Mesh mesh)
        {
            var gripX = Mathf.Lerp(
                mesh.bounds.min.x, mesh.bounds.max.x, SwordGripDistanceFromPommelRatio);
            var halfWidth = mesh.bounds.size.x * SwordGripHalfWidthRatio;
            var values = mesh.vertices
                .Where(item => Mathf.Abs(item.x - gripX) <= halfWidth)
                .ToArray();
            if (values.Length < 16)
                throw new InvalidOperationException(
                    "The current long-sword grip region is unavailable.");
            var center = values.Aggregate(Vector3.zero, (sum, value) => sum + value) /
                         values.Length;
            center.x = gripX;
            return center;
        }

        private static int[] RightHandWeightedVertexIndices(
            SkinnedMeshRenderer body,
            Transform rightHand)
        {
            var mesh = body.sharedMesh;
            var boneIndex = Array.IndexOf(body.bones, rightHand);
            if (boneIndex < 0 || mesh.boneWeights.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "The current right-hand skinning data is unavailable.");
            var values = Enumerable.Range(0, mesh.vertexCount)
                .Where(index => WeightForBone(mesh.boneWeights[index], boneIndex) >= 0.1f)
                .ToArray();
            if (values.Length < 16)
                throw new InvalidOperationException(
                    "The current model has too few right-hand weighted vertices.");
            return values;
        }

        private static Vector3 VisibleRightPalmWorld(
            SkinnedMeshRenderer body,
            Transform rightHand,
            IReadOnlyCollection<int> indices,
            Mesh baked)
        {
            body.BakeMesh(baked);
            var handLocal = indices.Select(index =>
                    rightHand.InverseTransformPoint(
                        body.transform.TransformPoint(baked.vertices[index])))
                .ToArray();
            var minimum = handLocal.Min(item => item.y);
            var maximum = handLocal.Max(item => item.y);
            var end = Mathf.Lerp(minimum, maximum, 0.5f);
            var fist = handLocal.Where(item => item.y <= end).ToArray();
            if (fist.Length < 16)
                throw new InvalidOperationException(
                    "The visible right-hand fist region is unavailable.");
            var center = fist.Aggregate(Vector3.zero, (sum, value) => sum + value) / fist.Length;
            return rightHand.TransformPoint(center);
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var value = 0f;
            if (weight.boneIndex0 == boneIndex) value += weight.weight0;
            if (weight.boneIndex1 == boneIndex) value += weight.weight1;
            if (weight.boneIndex2 == boneIndex) value += weight.weight2;
            if (weight.boneIndex3 == boneIndex) value += weight.weight3;
            return value;
        }

        private static void RenderIntoSheet(
            Camera camera,
            RenderTexture target,
            Texture2D panel,
            Texture2D sheet,
            int column,
            int row,
            int panelSize)
        {
            RenderIntoSheet(
                camera, target, panel, sheet, column, row, panelSize, panelSize);
        }

        private static void RenderIntoSheet(
            Camera camera,
            RenderTexture target,
            Texture2D panel,
            Texture2D sheet,
            int column,
            int row,
            int panelWidth,
            int panelHeight)
        {
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, panelWidth, panelHeight), 0, 0);
            panel.Apply();
            sheet.SetPixels32(
                column * panelWidth, row * panelHeight,
                panelWidth, panelHeight, panel.GetPixels32());
        }

        private static void FrameGripCamera(Camera camera, Vector3 center, float bodyHeight)
        {
            camera.aspect = 1f;
            camera.fieldOfView = 24f;
            var height = bodyHeight * 0.34f;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.1f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void FrameTorsoCamera(Camera camera, Bounds bounds)
        {
            camera.aspect = 5f / 6f;
            camera.fieldOfView = 24f;
            var height = bounds.size.y * 0.66f;
            var center = bounds.center + Vector3.up * bounds.size.y * 0.14f;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.1f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void FrameRightArmCamera(Camera camera, Bounds bounds)
        {
            camera.aspect = 21f / 26f;
            camera.fieldOfView = 23f;
            var height = bounds.size.y * 0.72f;
            var center = bounds.center + Vector3.up * bounds.size.y * 0.16f -
                         Vector3.right * bounds.size.x * 0.08f;
            var vertical = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * vertical * 1.08f -
                                        Vector3.right * vertical * 0.28f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(SourceFbxPath)
                .OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal))
                .Where(item => item.name == SourceClipName)
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    "The imported source must contain exactly one mixamo.com clip.");
            return clips[0];
        }

        private static void RequireDirectClipCompatibility(AnimationClip clip, Transform model)
        {
            var paths = model.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, model))
                .ToHashSet(StringComparer.Ordinal);
            var missing = AnimationUtility.GetCurveBindings(clip)
                .Where(item => item.type == typeof(Transform))
                .Select(item => item.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => !paths.Contains(path))
                .ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException(
                    "The embedded mixamo.com clip is not directly compatible with the current model. " +
                    string.Join("|", missing.Take(20)));
        }

        private static AnimationClip CreateOrUpdateLoopClip(AnimationClip source)
        {
            var destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            if (destination == null)
            {
                destination = new AnimationClip();
                AssetDatabase.CreateAsset(destination, LoopClipPath);
            }
            EditorUtility.CopySerialized(source, destination);
            destination.name = "Ispant_06_New_SheathingSword_Loop";
            destination.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(destination);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(destination, settings);
            EditorUtility.SetDirty(destination);
            AssetDatabase.SaveAssets();
            return destination;
        }

        private static void RequireMatchingTransformCurves(
            AnimationClip source,
            AnimationClip destination)
        {
            var sourceBindings = AnimationUtility.GetCurveBindings(source)
                .Where(item => item.type == typeof(Transform)).ToArray();
            var destinationBindings = AnimationUtility.GetCurveBindings(destination)
                .Where(item => item.type == typeof(Transform)).ToArray();
            if (sourceBindings.Length != destinationBindings.Length)
                throw new InvalidOperationException(
                    "The raw embedded Transform binding count changed during copying.");
            foreach (var sourceBinding in sourceBindings)
            {
                var destinationBinding = destinationBindings.FirstOrDefault(item =>
                    item.path == sourceBinding.path &&
                    item.propertyName == sourceBinding.propertyName &&
                    item.type == sourceBinding.type);
                if (destinationBinding.path == null ||
                    !CurvesMatch(
                        AnimationUtility.GetEditorCurve(source, sourceBinding),
                        AnimationUtility.GetEditorCurve(destination, destinationBinding)))
                    throw new InvalidOperationException(
                        "A raw embedded Transform curve changed during copying: " +
                        sourceBinding.path + "|" + sourceBinding.propertyName);
            }
        }

        private static AnimatorController CreateOrUpdateLoopController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var machine = controller.layers.Single().stateMachine;
            foreach (var child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (var child in machine.stateMachines.ToArray())
                machine.RemoveStateMachine(child.stateMachine);
            var state = machine.AddState("Ispant_06_New_SheathingSword_Loop");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void FrameCamera(Camera camera, Bounds bounds)
        {
            camera.aspect = 3f / 4f;
            var vertical = (bounds.size.y * 0.55f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var center = bounds.center + Vector3.up * bounds.size.y * 0.02f;
            camera.transform.position = center + Vector3.back * vertical * 1.15f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static string Absolute(string relative) =>
            System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", relative));

        private static void Restore(IEnumerable<TransformState> states)
        {
            foreach (var state in states)
                state.Restore();
        }

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene for the slot-6 operation.");
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before the slot-6 operation.");
            return scene;
        }

        private static Transform RequireModel(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementName);
            var slot = placement.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == SlotName);
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(
                    "Slot 6 does not contain the expected current direct model.");
            return slot.GetChild(0);
        }

        private static Transform RequireStaticModel(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementName);
            var slot = placement.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == StaticSlotName);
            if (slot.childCount != 1)
                throw new InvalidOperationException(
                    "Ispant_01_Static does not contain exactly one model.");
            return slot.GetChild(0);
        }

        private static Transform RequireBone(Transform model, string name)
        {
            var values = model.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (values.Length != 1)
                throw new InvalidOperationException(
                    "The current model must contain exactly one " + name + " bone.");
            return values[0];
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IList<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static void DecomposeTrs(
            Matrix4x4 matrix,
            out Vector3 position,
            out Quaternion rotation,
            out Vector3 scale)
        {
            position = new Vector3(matrix.m03, matrix.m13, matrix.m23);
            var x = new Vector3(matrix.m00, matrix.m10, matrix.m20);
            var y = new Vector3(matrix.m01, matrix.m11, matrix.m21);
            var z = new Vector3(matrix.m02, matrix.m12, matrix.m22);
            scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (matrix.determinant < 0f)
                scale.x = -scale.x;
            rotation = Quaternion.LookRotation(z / scale.z, y / scale.y);
        }

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            DecomposeTrs(matrix, out var position, out var rotation, out var scale);
            target.localPosition = position;
            target.localRotation = rotation;
            target.localScale = scale;
        }

        private sealed class VectorCurveSet
        {
            private readonly List<Keyframe> x = new List<Keyframe>();
            private readonly List<Keyframe> y = new List<Keyframe>();
            private readonly List<Keyframe> z = new List<Keyframe>();

            public void Add(float time, Vector3 value)
            {
                x.Add(new Keyframe(time, value.x));
                y.Add(new Keyframe(time, value.y));
                z.Add(new Keyframe(time, value.z));
            }

            public void Write(AnimationClip clip, string path)
            {
                SetLinearCurve(clip, path, "m_LocalPosition.x", x);
                SetLinearCurve(clip, path, "m_LocalPosition.y", y);
                SetLinearCurve(clip, path, "m_LocalPosition.z", z);
            }
        }

        private sealed class QuaternionCurveSet
        {
            private readonly List<Keyframe> x = new List<Keyframe>();
            private readonly List<Keyframe> y = new List<Keyframe>();
            private readonly List<Keyframe> z = new List<Keyframe>();
            private readonly List<Keyframe> w = new List<Keyframe>();
            private Quaternion previous;
            private bool hasPrevious;

            public void Add(float time, Quaternion value)
            {
                value.Normalize();
                if (hasPrevious && Quaternion.Dot(previous, value) < 0f)
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                previous = value;
                hasPrevious = true;
                x.Add(new Keyframe(time, value.x));
                y.Add(new Keyframe(time, value.y));
                z.Add(new Keyframe(time, value.z));
                w.Add(new Keyframe(time, value.w));
            }

            public void Write(AnimationClip clip, string path)
            {
                SetLinearCurve(clip, path, "m_LocalRotation.x", x);
                SetLinearCurve(clip, path, "m_LocalRotation.y", y);
                SetLinearCurve(clip, path, "m_LocalRotation.z", z);
                SetLinearCurve(clip, path, "m_LocalRotation.w", w);
            }
        }

        private sealed class TransformState
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformState(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void Restore()
            {
                if (target == null) return;
                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }
        }
    }
}
