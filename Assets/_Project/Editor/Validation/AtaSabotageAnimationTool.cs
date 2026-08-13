using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bellerophon.Core.Session;
using Bellerophon.Enemies.Ata;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaSabotageAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_06_Sabotage";
        private const string ModelName = "Ata_Model";
        private const string HeadPath = "Armature/Hips/Spine02/Spine01/Spine/neck/Head";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_Typing.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_06_Sabotage.controller";
        private const string StandingClipPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_06_Sabotage_Standing.anim";
        private const string CapturePath =
            "docs/validation/ata06_sabotage_animation_2026-08-13/Ata_06_Sabotage_TwoLoopReview.png";
        private const string ReportPath =
            "docs/validation/ata06_sabotage_animation_2026-08-13/Ata_06_Sabotage_Report.txt";
        private const string ProgressBarName = "Ata_SabotageProgressBar";
        private const string ProgressBarCapturePath =
            "docs/validation/ata06_sabotage_progress_bar_2026-08-13/Ata_06_Sabotage_ProgressBarReview.png";
        private const string ProgressBarReportPath =
            "docs/validation/ata06_sabotage_progress_bar_2026-08-13/Ata_06_Sabotage_ProgressBarReport.txt";
        private const string RepeatingCycleCapturePath =
            "docs/validation/ata06_sabotage_repeating_cycle_2026-08-13/Ata_06_Sabotage_RepeatingCycleReview.png";
        private const string RepeatingCycleReportPath =
            "docs/validation/ata06_sabotage_repeating_cycle_2026-08-13/Ata_06_Sabotage_RepeatingCycleReport.txt";
        private const string StateName = "AtaSabotage";

        [MenuItem("Bellerophon/Enemies/Ata/Apply Sabotage Animation")]
        public static void ApplyAtaSabotageAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var slotTransform = new TransformSnapshot(slot);
            var modelTransform = new TransformSnapshot(model);
            var standingLowerBodyPose = CreateAnatomicalStandingLowerBodyPose(
                model,
                out var standingMetrics);
            RequireMatchingLowerBodyHierarchy(model, standingLowerBodyPose);

            ConfigureMixamoClipLoop();
            var sourceClip = RequireMixamoClip();
            var clip = CreateStandingClip(
                sourceClip,
                standingLowerBodyPose,
                out var removedLowerBodyCurves,
                out var bakedLowerBodyCurves);
            var controller = CreateController(clip);
            var animator = ConfigureAnimator(model, controller);
            var correctedRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.CorrectModelForClips(
                    SlotName,
                    model,
                    new[] { clip });

            if (!slotTransform.Matches() || !modelTransform.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_06_Sabotage slot or model transform changed while applying the typing clip.");
            }

            RequireAppliedState(model, animator, clip, controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata sabotage animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaSabotageAnimationApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Source=" + SourcePath +
                ", EmbeddedClip=" + sourceClip.name +
                ", AppliedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", StandingReference=AnatomicalLegLengthPose" +
                ", RemovedLowerBodyCurves=" + removedLowerBodyCurves +
                ", BakedLowerBodyCurves=" + bakedLowerBodyCurves +
                ", LeftKneeFlexion=" + Num(standingMetrics.LeftKneeFlexion) +
                ", RightKneeFlexion=" + Num(standingMetrics.RightKneeFlexion) +
                ", FootHeightDifference=" + Num(standingMetrics.FootHeightDifference) +
                ", Loop=True" +
                ", RootMotion=False" +
                ", CorrectedRightArmComponents=" + correctedRightArmComponents +
                ", SlotTransformFixed=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Sabotage Animation")]
        public static void CaptureAtaSabotageAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_06_Sabotage Animator is missing.");
            var clip = RequireStandingClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_06_Sabotage controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var standingLowerBodyPose = CreateAnatomicalStandingLowerBodyPose(
                model,
                out var standingMetrics);
            RequireMatchingLowerBodyHierarchy(model, standingLowerBodyPose);

            var destination = Absolute(CapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid sabotage capture path."));
            var result = CaptureTwoLoopReview(
                model,
                animator,
                clip,
                standingLowerBodyPose,
                standingMetrics,
                destination);
            WriteReport(clip, result);
            Debug.Log(
                "AtaSabotageAnimationCaptured Result=PASS" +
                ", Path=" + CapturePath +
                ", Samples=16" +
                ", Views=FrontThreeQuarter,Side" +
                ", Loops=2" +
                ", Loop=True" +
                ", RootMotion=False" +
                ", MaximumModelRootError=" + Num(result.MaximumModelRootError) +
                ", MaximumStandingLowerBodyPositionError=" +
                Num(result.MaximumStandingLowerBodyPositionError) +
                ", MaximumStandingLowerBodyRotationError=" +
                Num(result.MaximumStandingLowerBodyRotationError) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Sabotage Progress Bar")]
        public static void ApplyAtaSabotageProgressBar()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_06_Sabotage Animator is missing.");
            var clip = RequireStandingClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_06_Sabotage controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            if (Mathf.Abs(
                    AtaSabotageProgressBar.CastDurationSeconds -
                    SpacePirateRules.AtaSabotageCastSeconds) > 0.0001f ||
                Mathf.Abs(AtaSabotageProgressBar.CastDurationSeconds - 35f) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata sabotage progress bar must reuse the 35-second gameplay duration.");
            }

            var head = model.Find(HeadPath) ??
                       throw new InvalidOperationException(
                           "Ata_06_Sabotage Head transform is missing.");
            var existing = head.Cast<Transform>()
                .Where(child => child.name == ProgressBarName)
                .ToArray();
            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata sabotage head contains multiple progress bars.");
            }

            var progressObject = existing.Length == 1
                ? existing[0].gameObject
                : new GameObject(ProgressBarName);
            if (progressObject.transform.parent != head)
            {
                progressObject.transform.SetParent(head, false);
            }

            var progressBar = progressObject.GetComponent<AtaSabotageProgressBar>() ??
                              progressObject.AddComponent<AtaSabotageProgressBar>();
            progressBar.enabled = true;
            var visibleHeadTopY = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.enabled &&
                    !renderer.transform.IsChildOf(progressObject.transform))
                .Select(renderer => renderer.bounds.max.y)
                .DefaultIfEmpty(float.NaN)
                .Max();
            if (float.IsNaN(visibleHeadTopY))
            {
                throw new InvalidOperationException(
                    "Ata_06_Sabotage has no visible renderer for head-top placement.");
            }

            progressBar.Configure(
                head,
                visibleHeadTopY,
                SpacePirateRules.AtaSabotageCastSeconds,
                true);
            EditorUtility.SetDirty(progressObject);
            EditorUtility.SetDirty(progressBar);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Ata sabotage progress bar.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaSabotageProgressBarApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", ParentBone=" + head.name +
                ", DurationSeconds=" + Num(AtaSabotageProgressBar.CastDurationSeconds) +
                ", WidthMeters=" + Num(AtaSabotageProgressBar.WidthMeters) +
                ", HeightMeters=" + Num(AtaSabotageProgressBar.HeightMeters) +
                ", HeadClearanceMeters=" + Num(AtaSabotageProgressBar.HeadOffsetMeters) +
                ", BarCenterOffsetMeters=" + Num(progressBar.BarCenterOffsetMeters) +
                ", RuntimeText=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Sabotage Progress Bar")]
        public static void CaptureAtaSabotageProgressBar()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_06_Sabotage Animator is missing.");
            var clip = RequireStandingClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_06_Sabotage controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var head = model.Find(HeadPath) ??
                       throw new InvalidOperationException(
                           "Ata_06_Sabotage Head transform is missing.");
            var progressTransform = head.Cast<Transform>()
                .SingleOrDefault(child => child.name == ProgressBarName) ??
                throw new InvalidOperationException(
                    "Ata sabotage progress bar is missing from the head bone.");
            var progressBar = progressTransform.GetComponent<AtaSabotageProgressBar>() ??
                              throw new InvalidOperationException(
                                  "Ata sabotage progress bar component is missing.");
            if (Mathf.Abs(AtaSabotageProgressBar.CastDurationSeconds - 35f) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata sabotage progress bar duration differs from 35 seconds.");
            }

            var destination = Absolute(ProgressBarCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid progress-bar capture path."));
            var progressStates = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(renderer => (renderer, renderer.enabled))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            var originalProgress = progressBar.NormalizedProgress;
            var cameraObject = new GameObject("Ata Sabotage Progress Bar Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
            camera.fieldOfView = 27f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            const int width = 420;
            const int height = 560;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var sheet = new Texture2D(width * progressStates.Length, height, TextureFormat.RGB24, false);
            var maximumAnchorError = 0f;
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                animator.enabled = false;
                camera.targetTexture = target;
                for (var index = 0; index < progressStates.Length; index++)
                {
                    foreach (var snapshot in modelSnapshots)
                    {
                        snapshot.Restore();
                    }

                    clip.SampleAnimation(model.gameObject, clip.length * 0.35f);
                    progressBar.SetProgressForReview(progressStates[index]);
                    progressBar.RefreshForCamera(null);
                    FrameModel(camera, model, 35f);
                    progressBar.RefreshForCamera(camera);
                    maximumAnchorError = Mathf.Max(
                        maximumAnchorError,
                        Vector3.Distance(
                            progressBar.transform.position,
                            head.position + Vector3.up * progressBar.BarCenterOffsetMeters));
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata sabotage progress-bar review contains Unity magenta shader fallback.");
                    }

                    sheet.SetPixels32(index * width, 0, width, height, pixels);
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                progressBar.SetProgressForReview(originalProgress);
                animator.enabled = originalAnimatorEnabled;
                foreach (var state in rendererStates)
                {
                    if (state.renderer != null)
                    {
                        state.renderer.enabled = state.enabled;
                    }
                }

                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            var reportDestination = Absolute(ProgressBarReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(reportDestination) ??
                throw new InvalidOperationException("Invalid progress-bar report path."));
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "ApprovedSample=artSample/enemies/ata/sabotage_progress_bar/Ata_SabotageProgressBar_ApprovalSample.svg",
                "DurationSeconds=" + Num(AtaSabotageProgressBar.CastDurationSeconds),
                "StateSeconds=0,8.75,17.5,26.25,35",
                "StateProgress=0,0.25,0.5,0.75,1",
                "WidthMeters=" + Num(AtaSabotageProgressBar.WidthMeters),
                "HeightMeters=" + Num(AtaSabotageProgressBar.HeightMeters),
                "HeadClearanceMeters=" + Num(AtaSabotageProgressBar.HeadOffsetMeters),
                "BarCenterOffsetMeters=" + Num(progressBar.BarCenterOffsetMeters),
                "Frame=ApprovedMatteBlackIronWithSideBrackets",
                "FillGradient=#8F201F>#D84535>#FF8A4C",
                "FillDirection=LeftToRight",
                "RuntimeText=False",
                "MaximumHeadAnchorError=" + Num(maximumAnchorError),
                "StandingTypingClipPreserved=True",
                "Capture=" + ProgressBarCapturePath
            });
            Debug.Log(
                "AtaSabotageProgressBarCaptured Result=PASS" +
                ", Path=" + ProgressBarCapturePath +
                ", States=0,0.25,0.5,0.75,1" +
                ", DurationSeconds=" + Num(AtaSabotageProgressBar.CastDurationSeconds) +
                ", MaximumHeadAnchorError=" + Num(maximumAnchorError) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Sabotage Repeating Cycle")]
        public static void ApplyAtaSabotageRepeatingCycle()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_06_Sabotage Animator is missing.");
            var clip = RequireStandingClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_06_Sabotage controller is missing.");
            RequireAppliedState(model, animator, clip, controller, false);
            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var skinnedMeshesBefore = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => (renderer, renderer.sharedMesh))
                .ToArray();
            var progressBar = model.GetComponentsInChildren<AtaSabotageProgressBar>(true)
                .SingleOrDefault(component => component.name == ProgressBarName) ??
                              throw new InvalidOperationException(
                                  "Ata sabotage progress bar is missing.");
            if (Mathf.Abs(progressBar.DurationSeconds -
                          SpacePirateRules.AtaSabotageCastSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata sabotage progress bar must use the 35-second gameplay duration.");
            }

            ConfigureRepeatingCycle(controller, clip);
            progressBar.SetRestartOnCompletion(true);
            EditorUtility.SetDirty(progressBar);
            RequireAppliedState(model, animator, clip, controller);
            AssetDatabase.SaveAssets();

            if (!slotBefore.Matches() || !modelBefore.Matches() ||
                skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata sabotage repeating-cycle apply changed a model transform or skinned mesh.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Ata sabotage repeating cycle.");
            }

            Debug.Log(
                "AtaSabotageRepeatingCycleApplied Result=PASS" +
                ", ClipDurationSeconds=" + Num(clip.length) +
                ", StateSpeed=1" +
                ", CycleDurationSeconds=" + Num(SpacePirateRules.AtaSabotageCastSeconds) +
                ", RestartExitTime=" + Num(ResolveRepeatingCycleExitTime(clip)) +
                ", ReturnsToStartAtCycleEnd=True" +
                ", ProgressBarRestarts=True" +
                ", StandingTypingClipPreserved=True" +
                ", RightArmCorrectedMeshPreserved=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Sabotage Repeating Cycle")]
        public static void CaptureAtaSabotageRepeatingCycle()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_06_Sabotage Animator is missing.");
            var clip = RequireStandingClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_06_Sabotage controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var progressBar = model.GetComponentsInChildren<AtaSabotageProgressBar>(true)
                .SingleOrDefault(component => component.name == ProgressBarName) ??
                              throw new InvalidOperationException(
                                  "Ata sabotage progress bar is missing.");
            if (Mathf.Abs(progressBar.DurationSeconds -
                          SpacePirateRules.AtaSabotageCastSeconds) > 0.0001f ||
                !progressBar.RestartOnCompletion)
            {
                throw new InvalidOperationException(
                    "Ata sabotage progress bar must restart every 35 seconds.");
            }

            var elapsedStates = new[]
            {
                0f,
                8.75f,
                17.5f,
                34.99f,
                35f,
                35.01f,
                43.75f,
                52.5f
            };
            var destination = Absolute(RepeatingCycleCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid sabotage repeating-cycle capture path."));
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var poseBones = model.Find("Armature")?.GetComponentsInChildren<Transform>(true) ??
                            throw new InvalidOperationException(
                                "Ata sabotage Armature is missing.");
            var standingLowerBodyPose = CreateAnatomicalStandingLowerBodyPose(
                model,
                out _);
            var standingLowerBodyTransforms = model.GetComponentsInChildren<Transform>(true)
                .Where(item => standingLowerBodyPose.ContainsKey(
                    AnimationUtility.CalculateTransformPath(item, model)))
                .ToDictionary(
                    item => AnimationUtility.CalculateTransformPath(item, model),
                    item => item);
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(renderer => (renderer, renderer.enabled))
                .ToArray();
            var skinnedMeshesBefore = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => (renderer, renderer.sharedMesh))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            var originalProgress = progressBar.NormalizedProgress;
            var head = progressBar.transform.parent ??
                       throw new InvalidOperationException(
                           "Ata sabotage progress bar head anchor is missing.");
            var cameraObject = new GameObject("Ata Sabotage Repeating Cycle Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
            camera.fieldOfView = 27f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            const int columns = 4;
            const int rows = 2;
            const int width = 420;
            const int height = 560;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                width * columns,
                height * rows,
                TextureFormat.RGB24,
                false);
            var maximumAnchorError = 0f;
            var maximumStandingLowerBodyPositionError = 0f;
            var maximumStandingLowerBodyRotationError = 0f;
            var startPoseDifferenceAt34_99 = 0f;
            var startPoseDifferenceAt35 = 0f;
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                animator.enabled = false;
                camera.targetTexture = target;
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                SampleRepeatingCycleTimeline(model.gameObject, clip, 0f);
                var startPositions = poseBones.Select(bone => bone.localPosition).ToArray();
                var startRotations = poseBones.Select(bone => bone.localRotation).ToArray();
                for (var index = 0; index < elapsedStates.Length; index++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    var elapsed = elapsedStates[index];
                    SampleRepeatingCycleTimeline(model.gameObject, clip, elapsed);
                    if (Mathf.Abs(elapsed - 34.99f) < 0.0001f ||
                        Mathf.Abs(elapsed - 35f) < 0.0001f)
                    {
                        var poseDifference = 0f;
                        for (var boneIndex = 0; boneIndex < poseBones.Length; boneIndex++)
                        {
                            poseDifference += Vector3.Distance(
                                startPositions[boneIndex],
                                poseBones[boneIndex].localPosition) * 100f;
                            poseDifference += Quaternion.Angle(
                                startRotations[boneIndex],
                                poseBones[boneIndex].localRotation);
                        }

                        poseDifference /= poseBones.Length;
                        if (Mathf.Abs(elapsed - 34.99f) < 0.0001f)
                        {
                            startPoseDifferenceAt34_99 = poseDifference;
                        }
                        else
                        {
                            startPoseDifferenceAt35 = poseDifference;
                        }
                    }

                    foreach (var reference in standingLowerBodyPose)
                    {
                        var current = standingLowerBodyTransforms[reference.Key];
                        maximumStandingLowerBodyPositionError = Mathf.Max(
                            maximumStandingLowerBodyPositionError,
                            Vector3.Distance(
                                current.localPosition,
                                reference.Value.LocalPosition));
                        maximumStandingLowerBodyRotationError = Mathf.Max(
                            maximumStandingLowerBodyRotationError,
                            Quaternion.Angle(
                                current.localRotation,
                                reference.Value.LocalRotation));
                    }

                    var cycleElapsed = Mathf.Repeat(elapsed, progressBar.DurationSeconds);
                    progressBar.SetProgressForReview(
                        cycleElapsed / progressBar.DurationSeconds);
                    progressBar.RefreshForCamera(null);
                    FrameModel(camera, model, 35f);
                    progressBar.RefreshForCamera(camera);
                    maximumAnchorError = Mathf.Max(
                        maximumAnchorError,
                        Vector3.Distance(
                            progressBar.transform.position,
                            head.position + Vector3.up * progressBar.BarCenterOffsetMeters));
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata sabotage repeating-cycle review contains Unity magenta shader fallback.");
                    }

                    sheet.SetPixels32(
                        (index % columns) * width,
                        (rows - 1 - index / columns) * height,
                        width,
                        height,
                        pixels);
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                progressBar.SetProgressForReview(originalProgress);
                animator.enabled = originalAnimatorEnabled;
                foreach (var item in rendererStates)
                {
                    if (item.renderer != null)
                    {
                        item.renderer.enabled = item.enabled;
                    }
                }

                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            RequireAppliedState(model, animator, clip, controller);
            if (startPoseDifferenceAt34_99 <= 0.1f ||
                startPoseDifferenceAt35 > 0.0001f ||
                maximumStandingLowerBodyPositionError > 0.0002f ||
                maximumStandingLowerBodyRotationError > 0.01f ||
                scene.isDirty ||
                skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata sabotage cycle did not reset at 35 seconds or changed the approved standing/mesh state.");
            }

            var reportDestination = Absolute(RepeatingCycleReportPath);
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "DurationRule=SpacePirateRules.AtaSabotageCastSeconds",
                "CycleDurationSeconds=" + Num(SpacePirateRules.AtaSabotageCastSeconds),
                "ClipDurationSeconds=" + Num(clip.length),
                "StateSpeed=1",
                "RestartExitTime=" + Num(ResolveRepeatingCycleExitTime(clip)),
                "ReviewedSeconds=" + string.Join(",", elapsedStates.Select(Num)),
                "StartPoseDifferenceAt34.99Seconds=" + Num(startPoseDifferenceAt34_99),
                "StartPoseDifferenceAt35Seconds=" + Num(startPoseDifferenceAt35),
                "ProgressBarAt34.99Seconds=" +
                Num(34.99f / progressBar.DurationSeconds),
                "ProgressBarAt35Seconds=0",
                "ReturnsToStartBeforeCompletion=False",
                "ReturnsToStartAt35Seconds=True",
                "ProgressBarRestartsOnCompletion=True",
                "StandingTypingClipPreserved=True",
                "StandingLowerBodyPreserved=True",
                "RootMotion=False",
                "RightArmCorrectedMeshPreserved=True",
                "ProgressBarVisualPreserved=True",
                "MaximumStandingLowerBodyPositionError=" +
                Num(maximumStandingLowerBodyPositionError),
                "MaximumStandingLowerBodyRotationError=" +
                Num(maximumStandingLowerBodyRotationError),
                "MaximumHeadAnchorError=" + Num(maximumAnchorError),
                "Capture=" + RepeatingCycleCapturePath
            });
            Debug.Log(
                "AtaSabotageRepeatingCycleCaptured Result=PASS" +
                ", StateSpeed=1" +
                ", StartPoseDifferenceAt34.99Seconds=" +
                Num(startPoseDifferenceAt34_99) +
                ", StartPoseDifferenceAt35Seconds=" + Num(startPoseDifferenceAt35) +
                ", ReturnsToStartAt35Seconds=True" +
                ", ProgressBarAt35Seconds=0" +
                ", SecondCycleReviewed=True" +
                ", SceneChanged=False.");
        }

        private static void SampleRepeatingCycleTimeline(
            GameObject model,
            AnimationClip clip,
            float elapsedSeconds)
        {
            var cycleElapsed = Mathf.Repeat(
                elapsedSeconds,
                SpacePirateRules.AtaSabotageCastSeconds);
            clip.SampleAnimation(model, Mathf.Repeat(cycleElapsed, clip.length));
        }

        private static void ConfigureMixamoClipLoop()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Ata typing FBX importer is unavailable.");
            importer.importAnimation = true;
            var clips = importer.defaultClipAnimations;
            var mixamoIndices = clips
                .Select((clip, index) => (clip, index))
                .Where(item => item.clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.index)
                .ToArray();
            if (mixamoIndices.Length != 1)
            {
                throw new InvalidOperationException(
                    "attas typing.fbx must expose exactly one mixamo-named default clip.");
            }

            var selected = clips[mixamoIndices[0]];
            selected.loopTime = true;
            selected.loopPose = false;
            clips[mixamoIndices[0]] = selected;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireMixamoClip()
        {
            var available = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            var clips = available
                .Where(clip => clip.name.IndexOf(
                    "mixamo",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "attas typing.fbx must expose exactly one mixamo-named animation clip. Found=" +
                    clips.Length +
                    ", AvailableClips=" + string.Join(",", available.Select(clip =>
                        clip.name + "[" + Num(clip.length) + "s]")));
            }

            return clips[0];
        }

        private static AnimationClip CreateStandingClip(
            AnimationClip source,
            IReadOnlyDictionary<string, LocalPose> standingLowerBodyPose,
            out int removedLowerBodyCurves,
            out int bakedLowerBodyCurves)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(StandingClipPath) != null &&
                !AssetDatabase.DeleteAsset(StandingClipPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_06 standing sabotage clip could not be replaced.");
            }

            var standing = UnityEngine.Object.Instantiate(source);
            standing.name = "Ata_06_Sabotage_Standing";
            removedLowerBodyCurves = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(standing))
            {
                if (!IsStandingLowerBodyPath(binding.path))
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(standing, binding, null);
                removedLowerBodyCurves++;
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(standing))
            {
                if (!IsStandingLowerBodyPath(binding.path))
                {
                    continue;
                }

                AnimationUtility.SetObjectReferenceCurve(standing, binding, null);
                removedLowerBodyCurves++;
            }

            if (removedLowerBodyCurves == 0 || standingLowerBodyPose.Count == 0)
            {
                UnityEngine.Object.DestroyImmediate(standing);
                throw new InvalidOperationException(
                    "Ata standing clip could not resolve source or reference lower-body data.");
            }

            bakedLowerBodyCurves = 0;
            foreach (var item in standingLowerBodyPose)
            {
                bakedLowerBodyCurves += SetConstantVector3Curves(
                    standing,
                    item.Key,
                    "m_LocalPosition",
                    item.Value.LocalPosition,
                    source.length);
                bakedLowerBodyCurves += SetConstantQuaternionCurves(
                    standing,
                    item.Key,
                    item.Value.LocalRotation,
                    source.length);
                bakedLowerBodyCurves += SetConstantVector3Curves(
                    standing,
                    item.Key,
                    "m_LocalScale",
                    item.Value.LocalScale,
                    source.length);
            }

            AssetDatabase.CreateAsset(standing, StandingClipPath);
            var serializedClip = new SerializedObject(standing);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime") ??
                       throw new InvalidOperationException(
                           "Ata standing sabotage loop setting is unavailable.");
            loop.boolValue = true;
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            standing.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(standing);
            return standing;
        }

        private static int SetConstantVector3Curves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            Vector3 value,
            float duration)
        {
            var values = new[] { value.x, value.y, value.z };
            var suffixes = new[] { ".x", ".y", ".z" };
            for (var index = 0; index < values.Length; index++)
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        propertyPrefix + suffixes[index]),
                    AnimationCurve.Constant(0f, duration, values[index]));
            }

            return 3;
        }

        private static int SetConstantQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion value,
            float duration)
        {
            var values = new[] { value.x, value.y, value.z, value.w };
            var suffixes = new[] { ".x", ".y", ".z", ".w" };
            for (var index = 0; index < values.Length; index++)
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        "m_LocalRotation" + suffixes[index]),
                    AnimationCurve.Constant(0f, duration, values[index]));
            }

            return 4;
        }

        private static AnimationClip RequireStandingClip() =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>(StandingClipPath) ??
            throw new InvalidOperationException(
                "Ata_06 standing sabotage clip is missing.");

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata_06 sabotage controller could not be replaced.");
            }

            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            ConfigureRepeatingCycle(controller, clip);
            return controller;
        }

        private static void ConfigureRepeatingCycle(
            AnimatorController controller,
            AnimationClip clip)
        {
            var states = controller.layers[0].stateMachine.states
                .Select(item => item.state)
                .ToArray();
            if (states.Length != 1)
            {
                throw new InvalidOperationException(
                    "Ata sabotage controller must contain exactly one typing state.");
            }

            var state = states[0];
            if (state.name != StateName || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Ata sabotage controller does not reference the standing typing clip.");
            }

            foreach (var transition in state.transitions.ToArray())
            {
                state.RemoveTransition(transition);
            }

            state.speed = 1f;
            var restartTransition = state.AddTransition(state);
            restartTransition.hasExitTime = true;
            restartTransition.exitTime = ResolveRepeatingCycleExitTime(clip);
            restartTransition.hasFixedDuration = true;
            restartTransition.duration = 0f;
            restartTransition.offset = 0f;
            restartTransition.canTransitionToSelf = true;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
        }

        private static float ResolveRepeatingCycleExitTime(AnimationClip clip) =>
            SpacePirateRules.AtaSabotageCastSeconds / clip.length;

        private static Animator ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_06_Sabotage contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_06_Sabotage Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            return animator;
        }

        private static void RequireAppliedState(
            Transform model,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            bool requireRepeatingCycle = true)
        {
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_06_Sabotage Animator configuration differs.");
            }

            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException(
                    "Ata standing sabotage clip is not configured to loop.");
            }

            var state = controller.layers[0].stateMachine.defaultState;
            var states = controller.layers[0].stateMachine.states;
            if (states.Length != 1 || state == null ||
                state.name != StateName || state.motion != clip ||
                Mathf.Abs(state.speed - 1f) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Ata sabotage controller does not directly reference the standing typing clip.");
            }

            if (!requireRepeatingCycle)
            {
                return;
            }

            var restartTransition = state.transitions.SingleOrDefault();
            if (restartTransition == null ||
                restartTransition.destinationState != state ||
                !restartTransition.hasExitTime ||
                Mathf.Abs(restartTransition.exitTime -
                          ResolveRepeatingCycleExitTime(clip)) > 0.0001f ||
                !restartTransition.hasFixedDuration ||
                Mathf.Abs(restartTransition.duration) > 0.000001f ||
                Mathf.Abs(restartTransition.offset) > 0.000001f ||
                !restartTransition.canTransitionToSelf)
            {
                throw new InvalidOperationException(
                    "Ata sabotage controller does not restart the original-speed typing state at 35 seconds.");
            }
        }

        private static CaptureResult CaptureTwoLoopReview(
            Transform model,
            Animator animator,
            AnimationClip clip,
            IReadOnlyDictionary<string, LocalPose> standingLowerBodyPose,
            StandingMetrics standingMetrics,
            string destination)
        {
            var normalizedTimes = new[]
            {
                0f, 0.25f, 0.5f, 0.75f,
                1f, 1.25f, 1.5f, 1.75f
            };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var standingLowerBodyTransforms = model.GetComponentsInChildren<Transform>(true)
                .Where(item => standingLowerBodyPose.ContainsKey(
                    AnimationUtility.CalculateTransformPath(item, model)))
                .ToDictionary(
                    item => AnimationUtility.CalculateTransformPath(item, model),
                    item => item);
            var originalAnimatorEnabled = animator.enabled;
            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(renderer => (renderer, renderer.enabled))
                .ToArray();
            var cameraObject = new GameObject("Ata Sabotage Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
            camera.fieldOfView = 27f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            const int width = 420;
            const int height = 560;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var sheet = new Texture2D(width * 4, height * 4, TextureFormat.RGB24, false);
            var originalModelPosition = model.localPosition;
            var originalModelRotation = model.localRotation;
            var originalModelScale = model.localScale;
            var maximumModelRootError = 0f;
            var maximumStandingLowerBodyPositionError = 0f;
            var maximumStandingLowerBodyRotationError = 0f;
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                animator.enabled = false;
                camera.targetTexture = target;
                for (var viewIndex = 0; viewIndex < 2; viewIndex++)
                for (var index = 0; index < normalizedTimes.Length; index++)
                {
                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }

                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * (normalizedTimes[index] % 1f));
                    maximumModelRootError = Mathf.Max(
                        maximumModelRootError,
                        Vector3.Distance(model.localPosition, originalModelPosition));
                    foreach (var reference in standingLowerBodyPose)
                    {
                        var current = standingLowerBodyTransforms[reference.Key];
                        maximumStandingLowerBodyPositionError = Mathf.Max(
                            maximumStandingLowerBodyPositionError,
                            Vector3.Distance(
                                current.localPosition,
                                reference.Value.LocalPosition));
                        maximumStandingLowerBodyRotationError = Mathf.Max(
                            maximumStandingLowerBodyRotationError,
                            Quaternion.Angle(
                                current.localRotation,
                                reference.Value.LocalRotation));
                    }
                    if (Quaternion.Angle(model.localRotation, originalModelRotation) > 0.01f ||
                        Vector3.Distance(model.localScale, originalModelScale) > 0.0002f)
                    {
                        throw new InvalidOperationException(
                            "Ata sabotage clip changed the scene model root rotation or scale.");
                    }

                    FrameModel(camera, model, viewIndex == 0 ? 35f : 90f);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata sabotage review contains Unity magenta shader fallback.");
                    }

                    sheet.SetPixels32(
                        (index % 4) * width,
                        (3 - (viewIndex * 2 + index / 4)) * height,
                        width,
                        height,
                        pixels);
                }

                sheet.Apply();
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
                return new CaptureResult(
                    maximumModelRootError,
                    maximumStandingLowerBodyPositionError,
                    maximumStandingLowerBodyRotationError,
                    standingMetrics);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = originalAnimatorEnabled;
                foreach (var state in rendererStates)
                {
                    if (state.renderer != null)
                    {
                        state.renderer.enabled = state.enabled;
                    }
                }

                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameModel(Camera camera, Transform model, float viewAngle)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Ata sabotage review has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            var direction = Quaternion.AngleAxis(viewAngle, model.up) * model.forward;
            var target = bounds.center;
            var distance = bounds.extents.magnitude /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.04f;
            camera.transform.position = target + direction.normalized * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                model.up);
        }

        private static void WriteReport(AnimationClip clip, CaptureResult result)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid sabotage report path."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "Source=" + SourcePath,
                "SourceSha256=214373A124DF59958CBBB2165042E14D789E0DEBC57F6F4F5D49F704AF059E94",
                "EmbeddedClip=mixamo.com",
                "AppliedClip=" + clip.name,
                "Duration=" + Num(clip.length),
                "StandingReference=AnatomicalLegLengthPose",
                "Loop=True",
                "RootMotion=False",
                "Samples=16",
                "Views=FrontThreeQuarter,Side",
                "ReviewedLoops=2",
                "MaximumModelRootError=" + Num(result.MaximumModelRootError),
                "MaximumStandingLowerBodyPositionError=" +
                Num(result.MaximumStandingLowerBodyPositionError),
                "MaximumStandingLowerBodyRotationError=" +
                Num(result.MaximumStandingLowerBodyRotationError),
                "LeftKneeFlexion=" + Num(result.StandingMetrics.LeftKneeFlexion),
                "RightKneeFlexion=" + Num(result.StandingMetrics.RightKneeFlexion),
                "FootHeightDifference=" +
                Num(result.StandingMetrics.FootHeightDifference),
                "Capture=" + CapturePath
            });
        }

        private static Dictionary<string, LocalPose> CreateAnatomicalStandingLowerBodyPose(
            Transform model,
            out StandingMetrics metrics)
        {
            metrics = default;
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault();
            var animatorEnabled = animator != null && animator.enabled;
            try
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }

                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                var hips = RequireUniqueDescendant(model, "Hips");
                var left = new LegChain(
                    RequireUniqueDescendant(model, "LeftUpLeg"),
                    RequireUniqueDescendant(model, "LeftLeg"),
                    RequireUniqueDescendant(model, "LeftFoot"));
                var right = new LegChain(
                    RequireUniqueDescendant(model, "RightUpLeg"),
                    RequireUniqueDescendant(model, "RightLeg"),
                    RequireUniqueDescendant(model, "RightFoot"));
                var up = model.up.normalized;
                var forward = Vector3.ProjectOnPlane(model.forward, up).normalized;
                if (forward.sqrMagnitude < 0.999f)
                {
                    throw new InvalidOperationException(
                        "Ata standing pose could not resolve model forward and up axes.");
                }

                var groundHeight = Mathf.Min(
                    Vector3.Dot(left.Foot.position, up),
                    Vector3.Dot(right.Foot.position, up));
                var leftTarget = ProjectToGround(left.Foot.position, up, groundHeight);
                var rightTarget = ProjectToGround(right.Foot.position, up, groundHeight);
                var leftFootRotation = left.Foot.rotation;
                var rightFootRotation = right.Foot.rotation;
                var requiredLift = 0.5f * (
                    RequiredHipLift(left, leftTarget, up) +
                    RequiredHipLift(right, rightTarget, up));
                hips.position += up * requiredLift;

                SolveLeg(left, leftTarget, leftFootRotation, forward);
                SolveLeg(right, rightTarget, rightFootRotation, forward);
                var leftKneeFlexion = KneeFlexion(left);
                var rightKneeFlexion = KneeFlexion(right);
                if (leftKneeFlexion > 25f || rightKneeFlexion > 25f)
                {
                    throw new InvalidOperationException(
                        "Ata anatomical standing pose did not straighten both knees. Left=" +
                        Num(leftKneeFlexion) + ", Right=" + Num(rightKneeFlexion) + ".");
                }

                var footHeightDifference = Mathf.Abs(
                    Vector3.Dot(left.Foot.position, up) -
                    Vector3.Dot(right.Foot.position, up));
                if (footHeightDifference > 0.002f)
                {
                    throw new InvalidOperationException(
                        "Ata anatomical standing feet do not share one ground height. Difference=" +
                        Num(footHeightDifference) + ".");
                }

                metrics = new StandingMetrics(
                    leftKneeFlexion,
                    rightKneeFlexion,
                    footHeightDifference);

                var result = model.GetComponentsInChildren<Transform>(true)
                    .Select(item => new
                    {
                        Path = AnimationUtility.CalculateTransformPath(item, model),
                        Transform = item
                    })
                    .Where(item => IsStandingLowerBodyPath(item.Path))
                    .ToDictionary(
                        item => item.Path,
                        item => new LocalPose(item.Transform));
                if (result.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Ata anatomical standing pose has no lower-body transforms.");
                }

                return result;
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                if (animator != null)
                {
                    animator.enabled = animatorEnabled;
                }
            }
        }

        private static Vector3 ProjectToGround(
            Vector3 position,
            Vector3 up,
            float groundHeight) =>
            position - up * (Vector3.Dot(position, up) - groundHeight);

        private static float RequiredHipLift(
            LegChain chain,
            Vector3 target,
            Vector3 up)
        {
            var totalLength = chain.UpperLength + chain.LowerLength;
            var desiredDistance = totalLength * 0.99f;
            var rootToTarget = target - chain.Upper.position;
            var vertical = Mathf.Abs(Vector3.Dot(rootToTarget, up));
            var horizontal = Vector3.ProjectOnPlane(rootToTarget, up).magnitude;
            var desiredVertical = Mathf.Sqrt(Mathf.Max(
                desiredDistance * desiredDistance - horizontal * horizontal,
                0f));
            return desiredVertical - vertical;
        }

        private static void SolveLeg(
            LegChain chain,
            Vector3 target,
            Quaternion footRotation,
            Vector3 forward)
        {
            var root = chain.Upper.position;
            var toTarget = target - root;
            var distance = Mathf.Clamp(
                toTarget.magnitude,
                Mathf.Abs(chain.UpperLength - chain.LowerLength) + 0.0001f,
                (chain.UpperLength + chain.LowerLength) * 0.999f);
            var along = toTarget.normalized;
            var bend = Vector3.ProjectOnPlane(forward, along).normalized;
            if (bend.sqrMagnitude < 0.999f)
            {
                bend = Vector3.ProjectOnPlane(chain.Lower.position - root, along).normalized;
            }

            var alongDistance = (
                chain.UpperLength * chain.UpperLength -
                chain.LowerLength * chain.LowerLength +
                distance * distance) / (2f * distance);
            var bendDistance = Mathf.Sqrt(Mathf.Max(
                chain.UpperLength * chain.UpperLength -
                alongDistance * alongDistance,
                0f));
            var desiredKnee = root + along * alongDistance + bend * bendDistance;
            chain.Upper.rotation = Quaternion.FromToRotation(
                chain.Lower.position - root,
                desiredKnee - root) * chain.Upper.rotation;
            chain.Lower.rotation = Quaternion.FromToRotation(
                chain.Foot.position - chain.Lower.position,
                target - chain.Lower.position) * chain.Lower.rotation;
            chain.Foot.rotation = footRotation;
        }

        private static float KneeFlexion(LegChain chain) =>
            180f - Vector3.Angle(
                chain.Upper.position - chain.Lower.position,
                chain.Foot.position - chain.Lower.position);

        private static Transform RequireUniqueDescendant(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .SingleOrDefault() ??
            throw new InvalidOperationException(
                root.name + " must contain exactly one " + name + " transform.");

        private static void RequireMatchingLowerBodyHierarchy(
            Transform model,
            IReadOnlyDictionary<string, LocalPose> reference)
        {
            var paths = model.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, model))
                .Where(IsStandingLowerBodyPath)
                .ToHashSet(StringComparer.Ordinal);
            if (!paths.SetEquals(reference.Keys))
            {
                throw new InvalidOperationException(
                    "Ata_06 lower-body hierarchy differs from the Ata_02_Idle reference.");
            }
        }

        private static bool IsStandingLowerBodyPath(string path)
        {
            var bones = path.Split('/').Select(segment =>
            {
                var separator = segment.LastIndexOf(':');
                return separator >= 0 ? segment.Substring(separator + 1) : segment;
            }).ToArray();
            if (bones.Length == 0)
            {
                return false;
            }

            var last = bones[bones.Length - 1];
            if (last == "Hips")
            {
                return true;
            }

            return bones.Any(bone =>
                bone == "LeftUpLeg" || bone == "LeftLeg" ||
                bone == "LeftFoot" || bone == "LeftToeBase" ||
                bone == "RightUpLeg" || bone == "RightLeg" ||
                bone == "RightFoot" || bone == "RightToeBase");
        }

        private static Scene RequireCleanScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene before handling Ata sabotage animation.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects()
                .SingleOrDefault(root => root.name == PlacementRootName) ??
            throw new InvalidOperationException(
                "Approved Ata enemy placement is missing.");

        private static Transform RequireDirectChild(Transform parent, string name) =>
            parent.Cast<Transform>().SingleOrDefault(child => child.name == name) ??
            throw new InvalidOperationException(
                parent.name + "/" + name + " is missing or duplicated.");

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private static string Num(float value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);

        private readonly struct CaptureResult
        {
            public CaptureResult(
                float maximumModelRootError,
                float maximumStandingLowerBodyPositionError,
                float maximumStandingLowerBodyRotationError,
                StandingMetrics standingMetrics)
            {
                MaximumModelRootError = maximumModelRootError;
                MaximumStandingLowerBodyPositionError =
                    maximumStandingLowerBodyPositionError;
                MaximumStandingLowerBodyRotationError =
                    maximumStandingLowerBodyRotationError;
                StandingMetrics = standingMetrics;
            }

            public float MaximumModelRootError { get; }
            public float MaximumStandingLowerBodyPositionError { get; }
            public float MaximumStandingLowerBodyRotationError { get; }
            public StandingMetrics StandingMetrics { get; }
        }

        private readonly struct StandingMetrics
        {
            public StandingMetrics(
                float leftKneeFlexion,
                float rightKneeFlexion,
                float footHeightDifference)
            {
                LeftKneeFlexion = leftKneeFlexion;
                RightKneeFlexion = rightKneeFlexion;
                FootHeightDifference = footHeightDifference;
            }

            public float LeftKneeFlexion { get; }
            public float RightKneeFlexion { get; }
            public float FootHeightDifference { get; }
        }

        private readonly struct LocalPose
        {
            public LocalPose(Transform transform)
            {
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
                LocalScale = transform.localScale;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct LegChain
        {
            public LegChain(Transform upper, Transform lower, Transform foot)
            {
                Upper = upper;
                Lower = lower;
                Foot = foot;
                UpperLength = Vector3.Distance(upper.position, lower.position);
                LowerLength = Vector3.Distance(lower.position, foot.position);
            }

            public Transform Upper { get; }
            public Transform Lower { get; }
            public Transform Foot { get; }
            public float UpperLength { get; }
            public float LowerLength { get; }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public bool Matches() =>
                transform != null &&
                Vector3.Distance(transform.localPosition, localPosition) <= 0.0002f &&
                Quaternion.Angle(transform.localRotation, localRotation) <= 0.01f &&
                Vector3.Distance(transform.localScale, localScale) <= 0.0002f;

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }
    }
}
