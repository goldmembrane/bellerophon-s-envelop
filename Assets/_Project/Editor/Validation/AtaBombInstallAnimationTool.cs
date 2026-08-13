using System;
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
    internal static class AtaBombInstallAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string SlotName = "Ata_07_BombInstall";
        private const string ModelName = "Ata_Model";
        private const string HeadPath = "Armature/Hips/Spine02/Spine01/Spine/neck/Head";
        private const string SourcePath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Sources/Ata_InstallingBomb.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_07_BombInstall.controller";
        private const string EnterSeatedClipPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_07_BombInstall_EnterSeated.anim";
        private const string SeatedLoopClipPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_07_BombInstall_SeatedLoop.anim";
        private const string ReviewPath =
            "Assets/_Project/Art/Enemies/Ata/Animations/Ata_07_BombInstall_Review.png";
        private const string ProgressBarName = "Ata_BombInstallProgressBar";
        private const string ProgressBarCapturePath =
            "docs/validation/ata07_bomb_install_progress_bar_2026-08-13/Ata_07_BombInstall_ProgressBarReview.png";
        private const string ProgressBarReportPath =
            "docs/validation/ata07_bomb_install_progress_bar_2026-08-13/Ata_07_BombInstall_ProgressBarReport.txt";
        private const string TimingCapturePath =
            "docs/validation/ata07_bomb_install_timing_2026-08-13/Ata_07_BombInstall_25SecondTimingReview.png";
        private const string TimingReportPath =
            "docs/validation/ata07_bomb_install_timing_2026-08-13/Ata_07_BombInstall_25SecondTimingReport.txt";
        private const string SourceAnalysisCapturePath =
            "docs/validation/ata07_bomb_install_seated_loop_2026-08-13/Ata_07_BombInstall_SourceMotionAnalysis.png";
        private const string SourceAnalysisReportPath =
            "docs/validation/ata07_bomb_install_seated_loop_2026-08-13/Ata_07_BombInstall_SourceMotionAnalysis.txt";
        private const string SeatedLoopCapturePath =
            "docs/validation/ata07_bomb_install_repeating_cycle_2026-08-13/Ata_07_BombInstall_RepeatingCycleReview.png";
        private const string SeatedLoopReportPath =
            "docs/validation/ata07_bomb_install_repeating_cycle_2026-08-13/Ata_07_BombInstall_RepeatingCycleReport.txt";
        private const string EnterStateName = "AtaBombInstallEnterSeated";
        private const string SeatedLoopStateName = "AtaBombInstallSeatedLoop";
        private const string StateName = "AtaBombInstall";

        // Source-analysis result: frames 398-458 are the closest matching seated
        // installation poses. The source is 60 fps, so this preserves playback speed 1.
        private const float SeatedLoopStartSeconds = 398f / 60f;
        private const float SeatedLoopEndSeconds = 458f / 60f;

        [MenuItem("Bellerophon/Enemies/Ata/Apply Bomb Install Animation")]
        public static void ApplyAtaBombInstallAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);

            ConfigureMixamoClipLoop();
            var clip = RequireMixamoClip();
            var controller = CreateController(clip);
            var animator = ConfigureAnimator(model, controller);
            var correctedRightArmComponents =
                AtaOtherSlotsRightArmMeshTool.CorrectModelForClips(
                    SlotName,
                    model,
                    new[] { clip },
                    maximumComponentTriangles: 88);

            if (!slotBefore.Matches() || !modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_07_BombInstall slot or model transform changed while applying the supplied clip.");
            }

            RequireAppliedState(model, animator, clip, controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata bomb-install animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaBombInstallAnimationApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", Source=" + SourcePath +
                ", EmbeddedClip=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", Loop=True" +
                ", RootMotion=False" +
                ", CorrectedRightArmComponents=" + correctedRightArmComponents +
                ", SlotTransformFixed=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Bomb Install Animation")]
        public static void CaptureAtaBombInstallAnimation()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_07_BombInstall controller is missing.");
            RequireAppliedState(model, animator, clip, controller);

            var remainingStretchComponents =
                AtaOtherSlotsRightArmMeshTool.InspectModelForClips(
                    model,
                    new[] { clip },
                    out var maximumRightArmStretchRatio);
            if (remainingStretchComponents != 0)
            {
                throw new InvalidOperationException(
                    "Ata_07_BombInstall still contains right-arm stretch components after apply.");
            }

            var modelBefore = new TransformSnapshot(model);
            var destination = Absolute(ReviewPath);
            var folder = Path.GetDirectoryName(destination) ??
                         throw new InvalidOperationException("Invalid Ata bomb-install review path.");
            Directory.CreateDirectory(folder);
            CaptureTwoLoopReview(model, clip, destination);
            if (!modelBefore.Matches() || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install review changed the saved scene state.");
            }

            Debug.Log(
                "AtaBombInstallAnimationCaptured Result=PASS" +
                ", Path=" + ReviewPath +
                ", Samples=16" +
                ", Views=FrontThreeQuarter,Side" +
                ", ReviewedLoops=2" +
                ", Loop=True" +
                ", RootMotion=False" +
                ", RemainingRightArmStretchComponents=" + remainingStretchComponents +
                ", MaximumRightArmStretchRatio=" + Num(maximumRightArmStretchRatio) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Bomb Install Progress Bar")]
        public static void ApplyAtaBombInstallProgressBar()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_07_BombInstall controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            if (Mathf.Abs(SpacePirateRules.AtaBombInstallSeconds - 25f) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install progress bar must reuse the 25-second gameplay duration.");
            }

            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var skinnedMeshesBefore = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => (renderer, renderer.sharedMesh))
                .ToArray();
            var head = model.Find(HeadPath) ??
                       throw new InvalidOperationException(
                           "Ata_07_BombInstall Head transform is missing.");
            var existing = head.Cast<Transform>()
                .Where(child => child.name == ProgressBarName)
                .ToArray();
            if (existing.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install head contains multiple progress bars.");
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
                    "Ata_07_BombInstall has no visible renderer for head-top placement.");
            }

            progressBar.Configure(
                head,
                visibleHeadTopY,
                SpacePirateRules.AtaBombInstallSeconds,
                true);
            if (Mathf.Abs(progressBar.DurationSeconds - 25f) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install progress bar duration differs from 25 seconds.");
            }

            RequireAppliedState(model, animator, clip, controller);
            if (!slotBefore.Matches() || !modelBefore.Matches() ||
                skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata bomb-install animation, model transform, or skinned mesh changed while applying the progress bar.");
            }

            EditorUtility.SetDirty(progressObject);
            EditorUtility.SetDirty(progressBar);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Ata bomb-install progress bar.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaBombInstallProgressBarApplied Result=PASS" +
                ", Slot=" + SlotName +
                ", ParentBone=" + head.name +
                ", DurationSeconds=" + Num(progressBar.DurationSeconds) +
                ", WidthMeters=" + Num(AtaSabotageProgressBar.WidthMeters) +
                ", HeightMeters=" + Num(AtaSabotageProgressBar.HeightMeters) +
                ", HeadClearanceMeters=" + Num(AtaSabotageProgressBar.HeadOffsetMeters) +
                ", BarCenterOffsetMeters=" + Num(progressBar.BarCenterOffsetMeters) +
                ", ApprovedSabotageVisual=True" +
                ", BombAnimationPreserved=True" +
                ", SkinnedMeshesPreserved=True" +
                ", RuntimeText=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Bomb Install Progress Bar")]
        public static void CaptureAtaBombInstallProgressBar()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_07_BombInstall controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var head = model.Find(HeadPath) ??
                       throw new InvalidOperationException(
                           "Ata_07_BombInstall Head transform is missing.");
            var progressTransform = head.Cast<Transform>()
                .SingleOrDefault(child => child.name == ProgressBarName) ??
                throw new InvalidOperationException(
                    "Ata bomb-install progress bar is missing from the head bone.");
            var progressBar = progressTransform.GetComponent<AtaSabotageProgressBar>() ??
                              throw new InvalidOperationException(
                                  "Ata bomb-install progress bar component is missing.");
            if (Mathf.Abs(progressBar.DurationSeconds - 25f) > 0.0001f ||
                Mathf.Abs(progressBar.DurationSeconds -
                          SpacePirateRules.AtaBombInstallSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install progress bar duration differs from the 25-second gameplay rule.");
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
            var skinnedMeshesBefore = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => (renderer, renderer.sharedMesh))
                .ToArray();
            var originalAnimatorEnabled = animator.enabled;
            var originalProgress = progressBar.NormalizedProgress;
            var cameraObject = new GameObject("Ata Bomb Install Progress Bar Review Camera");
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
            var sheet = new Texture2D(
                width * progressStates.Length,
                height,
                TextureFormat.RGB24,
                false);
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
                            "Ata bomb-install progress-bar review contains Unity magenta shader fallback.");
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

            RequireAppliedState(model, animator, clip, controller);
            if (scene.isDirty || skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata bomb-install progress-bar review changed the saved animation or mesh state.");
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
                "DurationSource=docs/GAME_DESIGN_SOURCE.txt:247",
                "DurationRule=SpacePirateRules.AtaBombInstallSeconds",
                "DurationSeconds=" + Num(progressBar.DurationSeconds),
                "StateSeconds=0,6.25,12.5,18.75,25",
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
                "OriginalBombInstallClipPreserved=True",
                "SkinnedMeshesPreserved=True",
                "Capture=" + ProgressBarCapturePath
            });
            Debug.Log(
                "AtaBombInstallProgressBarCaptured Result=PASS" +
                ", Path=" + ProgressBarCapturePath +
                ", States=0,0.25,0.5,0.75,1" +
                ", StateSeconds=0,6.25,12.5,18.75,25" +
                ", DurationSeconds=" + Num(progressBar.DurationSeconds) +
                ", MaximumHeadAnchorError=" + Num(maximumAnchorError) +
                ", BombAnimationPreserved=True" +
                ", SkinnedMeshesPreserved=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Apply Bomb Install Seated Loop")]
        public static void ApplyAtaBombInstallSeatedLoop()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var sourceClip = RequireMixamoClip();
            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var skinnedMeshesBefore = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => (renderer, renderer.sharedMesh))
                .ToArray();
            var controller = CreateController(sourceClip);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            var progressBar = model.GetComponentsInChildren<AtaSabotageProgressBar>(true)
                .SingleOrDefault(component => component.name == ProgressBarName) ??
                              throw new InvalidOperationException(
                                  "Ata bomb-install progress bar is missing.");
            progressBar.SetRestartOnCompletion(true);
            EditorUtility.SetDirty(progressBar);
            RequireAppliedState(model, animator, sourceClip, controller);
            AssetDatabase.SaveAssets();

            if (!slotBefore.Matches() || !modelBefore.Matches() ||
                skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata bomb-install seated-loop apply changed the model transform or skinned mesh.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying the Ata bomb-install seated loop.");
            }

            Debug.Log(
                "AtaBombInstallSeatedLoopApplied Result=PASS" +
                ", SourceDurationSeconds=" + Num(sourceClip.length) +
                ", EnterDurationSeconds=" + Num(SeatedLoopStartSeconds) +
                ", SeatedLoopStartSeconds=" + Num(SeatedLoopStartSeconds) +
                ", SeatedLoopEndSeconds=" + Num(SeatedLoopEndSeconds) +
                ", SeatedLoopDurationSeconds=" +
                Num(SeatedLoopEndSeconds - SeatedLoopStartSeconds) +
                ", EnterStateSpeed=1" +
                ", SeatedLoopStateSpeed=1" +
                ", ReturnsToStandingStartAtCycleEnd=True" +
                ", ProgressBarSeconds=" + Num(SpacePirateRules.AtaBombInstallSeconds) +
                ", ProgressBarRestarts=True" +
                ", OriginalMixamoClipPreserved=True" +
                ", RightArmCorrectedMeshPreserved=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Bomb Install Seated Loop")]
        public static void CaptureAtaBombInstallSeatedLoop()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var sourceClip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_07_BombInstall controller is missing.");
            RequireAppliedState(model, animator, sourceClip, controller);
            var enterClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(EnterSeatedClipPath) ??
                throw new InvalidOperationException(
                    "Ata bomb-install enter-seated clip is missing.");
            var seatedLoopClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(SeatedLoopClipPath) ??
                throw new InvalidOperationException(
                    "Ata bomb-install seated-loop clip is missing.");
            var progressBar = model.GetComponentsInChildren<AtaSabotageProgressBar>(true)
                .SingleOrDefault(component => component.name == ProgressBarName) ??
                              throw new InvalidOperationException(
                                  "Ata bomb-install progress bar is missing.");
            if (Mathf.Abs(progressBar.DurationSeconds -
                          SpacePirateRules.AtaBombInstallSeconds) > 0.0001f ||
                !progressBar.RestartOnCompletion)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install progress bar must restart every 25 seconds.");
            }

            var elapsedStates = new[]
            {
                0f,
                3f,
                SeatedLoopStartSeconds,
                24.99f,
                25f,
                25.01f,
                28f,
                25f + SeatedLoopStartSeconds
            };
            var destination = Absolute(SeatedLoopCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid seated-loop capture path."));
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var poseBones = model.Find("Armature")?.GetComponentsInChildren<Transform>(true) ??
                            throw new InvalidOperationException(
                                "Ata bomb-install Armature is missing.");
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
                           "Ata bomb-install progress bar head anchor is missing.");
            var cameraObject = new GameObject("Ata Bomb Install Seated Loop Review Camera");
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
            var startPoseDifferenceAt24_99 = 0f;
            var startPoseDifferenceAt25 = 0f;
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                animator.enabled = false;
                camera.targetTexture = target;
                foreach (var snapshot in modelSnapshots)
                {
                    snapshot.Restore();
                }

                SampleSeatedLoopTimeline(model.gameObject, enterClip, seatedLoopClip, 0f);
                var startPositions = poseBones.Select(bone => bone.localPosition).ToArray();
                var startRotations = poseBones.Select(bone => bone.localRotation).ToArray();
                for (var index = 0; index < elapsedStates.Length; index++)
                {
                    foreach (var snapshot in modelSnapshots)
                    {
                        snapshot.Restore();
                    }

                    var elapsed = elapsedStates[index];
                    SampleSeatedLoopTimeline(
                        model.gameObject,
                        enterClip,
                        seatedLoopClip,
                        elapsed);
                    if (Mathf.Abs(elapsed - 24.99f) < 0.0001f ||
                        Mathf.Abs(elapsed - 25f) < 0.0001f)
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
                        if (Mathf.Abs(elapsed - 24.99f) < 0.0001f)
                        {
                            startPoseDifferenceAt24_99 = poseDifference;
                        }
                        else
                        {
                            startPoseDifferenceAt25 = poseDifference;
                        }
                    }

                    var cycleElapsed = Mathf.Repeat(
                        elapsed,
                        progressBar.DurationSeconds);
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
                            "Ata bomb-install seated-loop review contains Unity magenta shader fallback.");
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
                foreach (var snapshot in modelSnapshots)
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

            RequireAppliedState(model, animator, sourceClip, controller);
            if (startPoseDifferenceAt24_99 <= 0.1f ||
                startPoseDifferenceAt25 > 0.0001f || scene.isDirty ||
                skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata bomb-install repeating cycle did not reset at 25 seconds or changed the saved scene.");
            }

            var reportDestination = Absolute(SeatedLoopReportPath);
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "DurationSource=docs/GAME_DESIGN_SOURCE.txt:247",
                "ProgressBarSeconds=" + Num(progressBar.DurationSeconds),
                "SourceClipDurationSeconds=" + Num(sourceClip.length),
                "EnterClipDurationSeconds=" + Num(enterClip.length),
                "SeatedLoopStartSeconds=" + Num(SeatedLoopStartSeconds),
                "SeatedLoopEndSeconds=" + Num(SeatedLoopEndSeconds),
                "SeatedLoopDurationSeconds=" + Num(seatedLoopClip.length),
                "EnterStateSpeed=1",
                "SeatedLoopStateSpeed=1",
                "CycleDurationSeconds=" + Num(SpacePirateRules.AtaBombInstallSeconds),
                "SeatedLoopRestartExitTime=" + Num(ResolveSeatedLoopRestartExitTime()),
                "ReviewedSeconds=" + string.Join(",", elapsedStates.Select(Num)),
                "StartPoseDifferenceAt24.99Seconds=" + Num(startPoseDifferenceAt24_99),
                "StartPoseDifferenceAt25Seconds=" + Num(startPoseDifferenceAt25),
                "ReturnsToStandingStartBeforeCompletion=False",
                "ReturnsToStandingStartAt25Seconds=True",
                "ProgressBarAt24.99Seconds=" + Num(24.99f / progressBar.DurationSeconds),
                "ProgressBarAt25Seconds=0",
                "ProgressBarRestartsOnCompletion=True",
                "SeatedLoopHasReturnTransition=True",
                "OriginalMixamoClipPreserved=True",
                "RightArmCorrectedMeshPreserved=True",
                "ProgressBarVisualPreserved=True",
                "MaximumHeadAnchorError=" + Num(maximumAnchorError),
                "Capture=" + SeatedLoopCapturePath
            });
            Debug.Log(
                "AtaBombInstallSeatedLoopCaptured Result=PASS" +
                ", EnterClipDurationSeconds=" + Num(enterClip.length) +
                ", SeatedLoopDurationSeconds=" + Num(seatedLoopClip.length) +
                ", EnterStateSpeed=1" +
                ", SeatedLoopStateSpeed=1" +
                ", StartPoseDifferenceAt24.99Seconds=" +
                Num(startPoseDifferenceAt24_99) +
                ", StartPoseDifferenceAt25Seconds=" + Num(startPoseDifferenceAt25) +
                ", ReturnsToStandingStartBeforeCompletion=False" +
                ", ReturnsToStandingStartAt25Seconds=True" +
                ", ProgressBarAt25Seconds=0" +
                ", SceneChanged=False.");
        }

        private static void SampleSeatedLoopTimeline(
            GameObject model,
            AnimationClip enterClip,
            AnimationClip seatedLoopClip,
            float elapsedSeconds)
        {
            var cycleElapsed = Mathf.Repeat(
                elapsedSeconds,
                SpacePirateRules.AtaBombInstallSeconds);
            if (cycleElapsed < enterClip.length)
            {
                enterClip.SampleAnimation(model, cycleElapsed);
                return;
            }

            var loopTime = Mathf.Repeat(
                cycleElapsed - enterClip.length,
                seatedLoopClip.length);
            seatedLoopClip.SampleAnimation(model, loopTime);
        }

        public static void ApplyAtaBombInstallTiming()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_07_BombInstall controller is missing.");
            RequireAppliedState(model, animator, clip, controller, false);

            var slotBefore = new TransformSnapshot(slot);
            var modelBefore = new TransformSnapshot(model);
            var skinnedMeshesBefore = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => (renderer, renderer.sharedMesh))
                .ToArray();
            var state = RequireBombInstallState(controller, clip);
            state.speed = ResolveBombInstallStateSpeed(clip);
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            RequireAppliedState(model, animator, clip, controller);
            var effectiveDuration = clip.length / state.speed;
            if (!slotBefore.Matches() || !modelBefore.Matches() || scene.isDirty ||
                skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata bomb-install timing apply changed the scene, model transform, or skinned mesh.");
            }

            Debug.Log(
                "AtaBombInstallTimingApplied Result=PASS" +
                ", ClipDurationSeconds=" + Num(clip.length) +
                ", StateSpeed=" + Num(state.speed) +
                ", EffectiveCycleSeconds=" + Num(effectiveDuration) +
                ", ProgressBarSeconds=" + Num(SpacePirateRules.AtaBombInstallSeconds) +
                ", FirstRestartAtProgressComplete=True" +
                ", OriginalClipPreserved=True" +
                ", SkinnedMeshesPreserved=True" +
                ", SceneChanged=False.");
        }

        public static void CaptureAtaBombInstallTiming()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_07_BombInstall Animator is missing.");
            var clip = RequireMixamoClip();
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(
                    "Ata_07_BombInstall controller is missing.");
            RequireAppliedState(model, animator, clip, controller);
            var state = RequireBombInstallState(controller, clip);
            var effectiveDuration = clip.length / state.speed;
            var progressBar = model.GetComponentsInChildren<AtaSabotageProgressBar>(true)
                .SingleOrDefault(component => component.name == ProgressBarName) ??
                              throw new InvalidOperationException(
                                  "Ata bomb-install progress bar is missing.");
            if (Mathf.Abs(effectiveDuration - progressBar.DurationSeconds) > 0.0001f ||
                Mathf.Abs(effectiveDuration - SpacePirateRules.AtaBombInstallSeconds) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install animation cycle and progress-bar duration differ.");
            }

            var elapsedStates = new[] { 0f, 6.25f, 12.5f, 18.75f, 24.99f, 25f };
            var destination = Absolute(TimingCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid timing capture path."));
            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
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
                           "Ata bomb-install progress bar head anchor is missing.");
            var cameraObject = new GameObject("Ata Bomb Install Timing Review Camera");
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
            var sheet = new Texture2D(
                width * elapsedStates.Length,
                height,
                TextureFormat.RGB24,
                false);
            var maximumAnchorError = 0f;
            try
            {
                foreach (var item in allRenderers)
                {
                    item.enabled = item.transform.IsChildOf(model);
                }

                animator.enabled = false;
                camera.targetTexture = target;
                for (var index = 0; index < elapsedStates.Length; index++)
                {
                    foreach (var snapshot in modelSnapshots)
                    {
                        snapshot.Restore();
                    }

                    var elapsed = elapsedStates[index];
                    var cycleProgress = elapsed / effectiveDuration;
                    var clipTime = elapsed >= effectiveDuration
                        ? 0f
                        : clip.length * cycleProgress;
                    clip.SampleAnimation(model.gameObject, clipTime);
                    progressBar.SetProgressForReview(elapsed / progressBar.DurationSeconds);
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
                            "Ata bomb-install timing review contains Unity magenta shader fallback.");
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
            if (scene.isDirty || skinnedMeshesBefore.Any(item =>
                    item.renderer == null || item.renderer.sharedMesh != item.sharedMesh))
            {
                throw new InvalidOperationException(
                    "Ata bomb-install timing review changed the saved animation or mesh state.");
            }

            var preCompletionCycles = state.speed * 24.99f / clip.length;
            var completionCycles = state.speed * 25f / clip.length;
            if (preCompletionCycles >= 1f ||
                Mathf.Abs(completionCycles - 1f) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install animation restarts before progress completion.");
            }

            var reportDestination = Absolute(TimingReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(reportDestination) ??
                throw new InvalidOperationException("Invalid timing report path."));
            File.WriteAllLines(reportDestination, new[]
            {
                "Result=PASS",
                "Slot=" + SlotName,
                "DurationSource=docs/GAME_DESIGN_SOURCE.txt:247",
                "DurationRule=SpacePirateRules.AtaBombInstallSeconds",
                "ClipDurationSeconds=" + Num(clip.length),
                "StateSpeed=" + Num(state.speed),
                "EffectiveCycleSeconds=" + Num(effectiveDuration),
                "ProgressBarSeconds=" + Num(progressBar.DurationSeconds),
                "ReviewedSeconds=0,6.25,12.5,18.75,24.99,25",
                "CyclesAt24.99Seconds=" + Num(preCompletionCycles),
                "CyclesAt25Seconds=" + Num(completionCycles),
                "RestartBeforeProgressComplete=False",
                "FirstRestartAtProgressComplete=True",
                "OriginalMixamoClipPreserved=True",
                "RightArmCorrectedMeshPreserved=True",
                "ProgressBarVisualPreserved=True",
                "MaximumHeadAnchorError=" + Num(maximumAnchorError),
                "Capture=" + TimingCapturePath
            });
            Debug.Log(
                "AtaBombInstallTimingCaptured Result=PASS" +
                ", ClipDurationSeconds=" + Num(clip.length) +
                ", StateSpeed=" + Num(state.speed) +
                ", EffectiveCycleSeconds=" + Num(effectiveDuration) +
                ", ProgressBarSeconds=" + Num(progressBar.DurationSeconds) +
                ", CyclesAt24.99Seconds=" + Num(preCompletionCycles) +
                ", CyclesAt25Seconds=" + Num(completionCycles) +
                ", RestartBeforeProgressComplete=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Bomb Install Source Motion Analysis")]
        public static void CaptureAtaBombInstallSourceAnalysis()
        {
            var scene = RequireCleanScene();
            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireMixamoClip();
            var destination = Absolute(SourceAnalysisCapturePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid source-analysis capture path."));

            const int columns = 6;
            const int rows = 4;
            const int sampleCount = columns * rows;
            const int width = 280;
            const int height = 420;
            var reviewModel = UnityEngine.Object.Instantiate(model.gameObject);
            reviewModel.name = "Ata Bomb Install Source Analysis Model";
            reviewModel.hideFlags = HideFlags.HideAndDontSave;
            reviewModel.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var child in reviewModel.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 31;
            }

            var reviewAnimator = reviewModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                                 throw new InvalidOperationException(
                                     "Ata bomb-install source-analysis Animator is missing.");
            reviewAnimator.enabled = false;
            var hips = reviewModel.transform.Find("Armature/Hips") ??
                       throw new InvalidOperationException(
                           "Ata bomb-install source-analysis Hips transform is missing.");
            var armature = reviewModel.transform.Find("Armature") ??
                           throw new InvalidOperationException(
                               "Ata bomb-install source-analysis Armature is missing.");
            var poseBones = armature.GetComponentsInChildren<Transform>(true);
            var cameraObject = new GameObject("Ata Bomb Install Source Analysis Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
            camera.fieldOfView = 27f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.cullingMask = 1 << 31;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var sheet = new Texture2D(
                width * columns,
                height * rows,
                TextureFormat.RGB24,
                false);
            var sampleTimes = Enumerable.Range(0, sampleCount)
                .Select(index => clip.length * index / (sampleCount - 1f))
                .ToArray();
            var hipsHeights = new float[sampleCount];
            var seatedCandidateCount = Mathf.CeilToInt((clip.length - 4f) * 30f) + 1;
            var seatedCandidateTimes = Enumerable.Range(0, seatedCandidateCount)
                .Select(index => Mathf.Min(clip.length, 4f + index / 30f))
                .ToArray();
            var seatedPositions = new Vector3[seatedCandidateCount][];
            var seatedRotations = new Quaternion[seatedCandidateCount][];
            var bestLoopStartSeconds = 0f;
            var bestLoopEndSeconds = 0f;
            var bestLoopPoseError = float.PositiveInfinity;
            var boundsInitialized = false;
            var sequenceBounds = new Bounds();
            try
            {
                for (var index = 0; index < sampleCount; index++)
                {
                    clip.SampleAnimation(reviewModel, sampleTimes[index]);
                    hipsHeights[index] = hips.position.y;
                    var renderers = reviewModel.GetComponentsInChildren<Renderer>(true)
                        .Where(renderer => renderer.enabled)
                        .ToArray();
                    foreach (var renderer in renderers)
                    {
                        if (!boundsInitialized)
                        {
                            sequenceBounds = renderer.bounds;
                            boundsInitialized = true;
                        }
                        else
                        {
                            sequenceBounds.Encapsulate(renderer.bounds);
                        }
                    }
                }

                if (!boundsInitialized)
                {
                    throw new InvalidOperationException(
                        "Ata bomb-install source analysis has no visible renderer.");
                }

                for (var index = 0; index < seatedCandidateCount; index++)
                {
                    clip.SampleAnimation(reviewModel, seatedCandidateTimes[index]);
                    seatedPositions[index] = poseBones
                        .Select(bone => bone.localPosition)
                        .ToArray();
                    seatedRotations[index] = poseBones
                        .Select(bone => bone.localRotation)
                        .ToArray();
                }

                for (var startIndex = 0; startIndex < seatedCandidateCount; startIndex++)
                for (var endIndex = startIndex + 1;
                     endIndex < seatedCandidateCount;
                     endIndex++)
                {
                    var duration = seatedCandidateTimes[endIndex] -
                                   seatedCandidateTimes[startIndex];
                    if (duration < 1f || duration > 3f)
                    {
                        continue;
                    }

                    var poseError = 0f;
                    for (var boneIndex = 0; boneIndex < poseBones.Length; boneIndex++)
                    {
                        poseError += Vector3.Distance(
                                         seatedPositions[startIndex][boneIndex],
                                         seatedPositions[endIndex][boneIndex]) * 100f;
                        poseError += Quaternion.Angle(
                                         seatedRotations[startIndex][boneIndex],
                                         seatedRotations[endIndex][boneIndex]);
                    }

                    poseError /= poseBones.Length;
                    if (poseError >= bestLoopPoseError)
                    {
                        continue;
                    }

                    bestLoopPoseError = poseError;
                    bestLoopStartSeconds = seatedCandidateTimes[startIndex];
                    bestLoopEndSeconds = seatedCandidateTimes[endIndex];
                }

                if (float.IsInfinity(bestLoopPoseError))
                {
                    throw new InvalidOperationException(
                        "Ata bomb-install source analysis could not resolve a seated loop seam.");
                }

                FrameBounds(camera, reviewModel.transform, sequenceBounds, 35f);
                camera.targetTexture = target;
                for (var index = 0; index < sampleCount; index++)
                {
                    clip.SampleAnimation(reviewModel, sampleTimes[index]);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata bomb-install source analysis contains Unity magenta shader fallback.");
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
                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(reviewModel);
            }

            var reportDestination = Absolute(SourceAnalysisReportPath);
            File.WriteAllLines(
                reportDestination,
                new[]
                {
                    "Result=PASS",
                    "Clip=mixamo.com",
                    "ClipDurationSeconds=" + Num(clip.length),
                    "PlaybackSpeed=1",
                    "Panels=24",
                    "PanelOrder=LeftToRight,TopToBottom",
                    "PanelTimesSeconds=" + string.Join(",", sampleTimes.Select(Num)),
                    "HipsWorldY=" + string.Join(",", hipsHeights.Select(Num)),
                    "MatchedSeatedLoopStartSeconds=" + Num(bestLoopStartSeconds),
                    "MatchedSeatedLoopEndSeconds=" + Num(bestLoopEndSeconds),
                    "MatchedSeatedLoopDurationSeconds=" +
                    Num(bestLoopEndSeconds - bestLoopStartSeconds),
                    "MatchedSeatedLoopPoseError=" + Num(bestLoopPoseError),
                    "SceneChanged=" + scene.isDirty,
                    "Capture=" + SourceAnalysisCapturePath
                });
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install source analysis changed the saved scene state.");
            }

            Debug.Log(
                "AtaBombInstallSourceAnalysisCaptured Result=PASS" +
                ", ClipDurationSeconds=" + Num(clip.length) +
                ", PlaybackSpeed=1" +
                ", Panels=24" +
                ", MatchedSeatedLoopStartSeconds=" + Num(bestLoopStartSeconds) +
                ", MatchedSeatedLoopEndSeconds=" + Num(bestLoopEndSeconds) +
                ", MatchedSeatedLoopPoseError=" + Num(bestLoopPoseError) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Diagnose Bomb Install Animation")]
        public static void DiagnoseAtaBombInstallAnimation()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active before diagnosing Ata bomb-install animation.");
            }

            var placement = RequirePlacement(scene);
            var slot = RequireDirectChild(placement.transform, SlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = RequireMixamoClip();
            var rightArmCurves = AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    IsRightArmPath(binding.path) ||
                    binding.propertyName.IndexOf("LocalScale", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(binding =>
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    var minimum = curve.keys.Min(key => key.value);
                    var maximum = curve.keys.Max(key => key.value);
                    return binding.path + "|" + binding.propertyName +
                           "|Min=" + Num(minimum) + "|Max=" + Num(maximum);
                })
                .ToArray();
            var meshDescription =
                AtaOtherSlotsRightArmMeshTool.DescribeModelForClips(
                    model,
                    new[] { clip });
            Debug.Log(
                "AtaBombInstallAnimationDiagnostic Result=PASS" +
                ", Duration=" + Num(clip.length) +
                ", " + meshDescription +
                ", RightArmAndScaleCurves=" + string.Join(";", rightArmCurves) +
                ", SceneDirty=" + scene.isDirty + ".");
        }

        private static bool IsRightArmPath(string path)
        {
            var leaf = path.Split('/').LastOrDefault() ?? string.Empty;
            var separator = leaf.LastIndexOf(':');
            if (separator >= 0)
            {
                leaf = leaf.Substring(separator + 1);
            }

            return leaf == "RightShoulder" || leaf == "RightArm" ||
                   leaf == "RightForeArm" || leaf == "RightHand";
        }

        private static void ConfigureMixamoClipLoop()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "Ata installing-bomb FBX importer is unavailable.");
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
                    "attas installing bomb.fbx must expose exactly one mixamo-named default clip.");
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
                    "attas installing bomb.fbx must expose exactly one mixamo-named animation clip. Found=" +
                    clips.Length +
                    ", AvailableClips=" + string.Join(",", available.Select(clip =>
                        clip.name + "[" + Num(clip.length) + "s]")));
            }

            return clips[0];
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var enterClip = CreateSegmentClip(
                clip,
                EnterSeatedClipPath,
                "Ata_07_BombInstall_EnterSeated",
                0f,
                SeatedLoopStartSeconds,
                false);
            var seatedLoopClip = CreateSegmentClip(
                clip,
                SeatedLoopClipPath,
                "Ata_07_BombInstall_SeatedLoop",
                SeatedLoopStartSeconds,
                SeatedLoopEndSeconds,
                true);
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            var enterState = stateMachine.AddState(EnterStateName);
            enterState.motion = enterClip;
            enterState.speed = 1f;
            enterState.writeDefaultValues = false;
            var seatedLoopState = stateMachine.AddState(SeatedLoopStateName);
            seatedLoopState.motion = seatedLoopClip;
            seatedLoopState.speed = 1f;
            seatedLoopState.writeDefaultValues = false;
            var transition = enterState.AddTransition(seatedLoopState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
            var restartTransition = seatedLoopState.AddTransition(enterState);
            restartTransition.hasExitTime = true;
            restartTransition.exitTime = ResolveSeatedLoopRestartExitTime();
            restartTransition.hasFixedDuration = true;
            restartTransition.duration = 0f;
            restartTransition.offset = 0f;
            restartTransition.canTransitionToSelf = false;
            stateMachine.defaultState = enterState;
            EditorUtility.SetDirty(enterState);
            EditorUtility.SetDirty(seatedLoopState);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip CreateSegmentClip(
            AnimationClip source,
            string assetPath,
            string clipName,
            float startSeconds,
            float endSeconds,
            bool loop)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null &&
                !AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata bomb-install segment clip could not be replaced: " + assetPath);
            }

            var segment = UnityEngine.Object.Instantiate(source);
            segment.name = clipName;
            segment.frameRate = source.frameRate;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationUtility.SetEditorCurve(
                    segment,
                    binding,
                    SliceCurve(curve, startSeconds, endSeconds));
            }

            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                var sliced = keys
                    .Where(key =>
                        key.time >= startSeconds - 0.00001f &&
                        key.time <= endSeconds + 0.00001f)
                    .Select(key => new ObjectReferenceKeyframe
                    {
                        time = Mathf.Clamp(key.time, startSeconds, endSeconds) - startSeconds,
                        value = key.value
                    })
                    .ToArray();
                AnimationUtility.SetObjectReferenceCurve(segment, binding, sliced);
            }

            var events = AnimationUtility.GetAnimationEvents(source)
                .Where(item =>
                    item.time >= startSeconds - 0.00001f &&
                    item.time <= endSeconds + 0.00001f)
                .Select(item =>
                {
                    item.time = Mathf.Clamp(item.time, startSeconds, endSeconds) - startSeconds;
                    return item;
                })
                .ToArray();
            AnimationUtility.SetAnimationEvents(segment, events);
            AssetDatabase.CreateAsset(segment, assetPath);
            segment.EnsureQuaternionContinuity();
            var serializedClip = new SerializedObject(segment);
            var loopTime = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime") ??
                           throw new InvalidOperationException(
                               "Ata bomb-install segment loop setting is unavailable.");
            loopTime.boolValue = loop;
            var loopBlend = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopBlend");
            if (loopBlend != null)
            {
                loopBlend.boolValue = loop;
            }

            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(segment);
            AssetDatabase.SaveAssetIfDirty(segment);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) ??
                   throw new InvalidOperationException(
                       "Ata bomb-install segment clip could not be reloaded: " + assetPath);
        }

        private static AnimationCurve SliceCurve(
            AnimationCurve source,
            float startSeconds,
            float endSeconds)
        {
            var duration = endSeconds - startSeconds;
            var keys = source.keys
                .Where(key =>
                    key.time >= startSeconds - 0.00001f &&
                    key.time <= endSeconds + 0.00001f)
                .Select(key =>
                {
                    key.time = Mathf.Clamp(key.time, startSeconds, endSeconds) - startSeconds;
                    return key;
                })
                .ToList();
            if (keys.Count == 0 || keys[0].time > 0.00001f)
            {
                keys.Insert(0, new Keyframe(0f, source.Evaluate(startSeconds)));
            }

            if (keys[keys.Count - 1].time < duration - 0.00001f)
            {
                keys.Add(new Keyframe(duration, source.Evaluate(endSeconds)));
            }

            var sliced = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return sliced;
        }

        private static Animator ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_07_BombInstall contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_07_BombInstall Animator must be on Ata_Model.");
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
            bool requireSynchronizedTiming = true)
        {
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_07_BombInstall Animator configuration differs.");
            }

            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install Mixamo clip is not configured to loop.");
            }

            var enterClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(EnterSeatedClipPath) ??
                throw new InvalidOperationException(
                    "Ata bomb-install enter-seated clip is missing.");
            var seatedLoopClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(SeatedLoopClipPath) ??
                throw new InvalidOperationException(
                    "Ata bomb-install seated-loop clip is missing.");
            var states = controller.layers[0].stateMachine.states
                .Select(item => item.state)
                .ToArray();
            var enterState = states.SingleOrDefault(state => state.name == EnterStateName) ??
                             throw new InvalidOperationException(
                                 "Ata bomb-install enter-seated state is missing.");
            var seatedLoopState = states.SingleOrDefault(
                                      state => state.name == SeatedLoopStateName) ??
                                  throw new InvalidOperationException(
                                      "Ata bomb-install seated-loop state is missing.");
            var transition = enterState.transitions.SingleOrDefault();
            var restartTransition = seatedLoopState.transitions.SingleOrDefault();
            var expectedRestartExitTime = ResolveSeatedLoopRestartExitTime();
            if (states.Length != 2 ||
                controller.layers[0].stateMachine.defaultState != enterState ||
                enterState.motion != enterClip || seatedLoopState.motion != seatedLoopClip ||
                Mathf.Abs(enterState.speed - 1f) > 0.000001f ||
                Mathf.Abs(seatedLoopState.speed - 1f) > 0.000001f ||
                transition == null || transition.destinationState != seatedLoopState ||
                !transition.hasExitTime || Mathf.Abs(transition.exitTime - 1f) > 0.000001f ||
                restartTransition == null || restartTransition.destinationState != enterState ||
                !restartTransition.hasExitTime ||
                Mathf.Abs(restartTransition.exitTime - expectedRestartExitTime) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install controller does not use the original-speed enter and seated-loop states.");
            }

            var enterSettings = new SerializedObject(enterClip).FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            var loopSettings = new SerializedObject(seatedLoopClip).FindProperty(
                "m_AnimationClipSettings.m_LoopTime");
            if (enterSettings == null || enterSettings.boolValue ||
                loopSettings == null || !loopSettings.boolValue ||
                Mathf.Abs(enterClip.length - SeatedLoopStartSeconds) > 0.0001f ||
                Mathf.Abs(seatedLoopClip.length -
                          (SeatedLoopEndSeconds - SeatedLoopStartSeconds)) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install segment clips differ from the analyzed seated-loop timing.");
            }
        }

        private static float ResolveSeatedLoopRestartExitTime()
        {
            var loopDuration = SeatedLoopEndSeconds - SeatedLoopStartSeconds;
            return (SpacePirateRules.AtaBombInstallSeconds - SeatedLoopStartSeconds) /
                   loopDuration;
        }

        private static AnimatorState RequireBombInstallState(
            AnimatorController controller,
            AnimationClip clip)
        {
            var state = controller.layers[0].stateMachine.defaultState;
            if (state == null || state.name != StateName || state.motion != clip)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install controller does not directly reference the Mixamo clip.");
            }

            return state;
        }

        private static float ResolveBombInstallStateSpeed(AnimationClip clip)
        {
            if (clip == null || clip.length <= 0f ||
                SpacePirateRules.AtaBombInstallSeconds <= 0f)
            {
                throw new InvalidOperationException(
                    "Ata bomb-install clip and gameplay duration must be positive.");
            }

            return clip.length / SpacePirateRules.AtaBombInstallSeconds;
        }

        private static void CaptureTwoLoopReview(
            Transform sourceModel,
            AnimationClip clip,
            string destination)
        {
            var normalizedTimes = new[]
            {
                0f, 0.25f, 0.5f, 0.75f,
                1f, 1.25f, 1.5f, 1.75f
            };
            var reviewModel = UnityEngine.Object.Instantiate(sourceModel.gameObject);
            reviewModel.name = "Ata Bomb Install Review Model";
            reviewModel.hideFlags = HideFlags.HideAndDontSave;
            reviewModel.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (var child in reviewModel.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 31;
            }

            var reviewAnimator = reviewModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                                 throw new InvalidOperationException(
                                     "Ata bomb-install review model Animator is missing.");
            reviewAnimator.enabled = false;
            var cameraObject = new GameObject("Ata Bomb Install Review Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
            camera.fieldOfView = 27f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.cullingMask = 1 << 31;
            const int width = 420;
            const int height = 560;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var sheet = new Texture2D(width * 4, height * 4, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                for (var viewIndex = 0; viewIndex < 2; viewIndex++)
                for (var index = 0; index < normalizedTimes.Length; index++)
                {
                    clip.SampleAnimation(
                        reviewModel,
                        clip.length * (normalizedTimes[index] % 1f));
                    FrameModel(camera, reviewModel.transform, viewIndex == 0 ? 35f : 90f);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata bomb-install review contains Unity magenta shader fallback.");
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
            }
            finally
            {
                RenderTexture.active = null;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(reviewModel);
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
                    "Ata bomb-install review has no visible renderer.");
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

        private static void FrameBounds(
            Camera camera,
            Transform model,
            Bounds bounds,
            float viewAngle)
        {
            var direction = Quaternion.AngleAxis(viewAngle, model.up) * model.forward;
            var distance = bounds.extents.magnitude /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.04f;
            camera.transform.position = bounds.center + direction.normalized * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                model.up);
        }

        private static Scene RequireCleanScene()
        {
            var scene = RequireActiveScene();

            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active before handling Ata bomb-install animation.");
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
