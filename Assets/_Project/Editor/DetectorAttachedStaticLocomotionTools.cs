using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class DetectorAttachedStaticLocomotionTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "Detector_Attached_Static";
        internal const string OutputFolder =
            "docs/validation/DetectorAttachedStaticLocomotion";
        internal const string TempReviewFolder =
            "Temp/DetectorAttachedStaticLocomotion";
        internal const string ReviewImagePath = TempReviewFolder + "/Review.png";
        internal const string ReviewReportPath = TempReviewFolder + "/Review.txt";
        internal const string FinalImagePath = OutputFolder + "/Final.png";
        internal const string FinalReportPath = OutputFolder + "/Final.txt";
        internal const string ControllerPath =
            AssetFolder + "/DetectorAttachedStatic_Locomotion.controller";

        private const string AssetFolder =
            "Assets/_Project/Art/Player/Animations/DetectorAttachedStaticLocomotion";
        private const string IdleClipPath =
            AssetFolder + "/DetectorAttachedStatic_Idle.anim";
        private const string ForwardClipPath =
            AssetFolder + "/DetectorAttachedStatic_WalkForward.anim";
        private const string BackwardClipPath =
            AssetFolder + "/DetectorAttachedStatic_WalkBackward.anim";
        private const string SidestepClipPath =
            AssetFolder + "/DetectorAttachedStatic_Sidestep.anim";
        private const string RunClipPath =
            AssetFolder + "/DetectorAttachedStatic_RunForward.anim";
        private const string StateName = "DetectorAttachedStaticLocomotion2D";
        private const string RootTreeName = "DetectorAttachedStaticSixMotion2D";
        private const string DiagonalTreeName =
            "DetectorAttachedStaticWalkDiagonalSourceExact";
        private const string AttachmentName = "PresenceDetector_Attached";
        private const string ReferenceName = "Armor_Head_Idle";
        private const string ReferenceAttachmentName = "ArmorHelmet_Attached";
        private const int PanelWidth = 512;
        private const int PanelHeight = 512;

        private static readonly string[] SourceNames =
        {
            "Player_Idle", "Player_Walk_Forward", "Player_Walk_Backward",
            "Player_Sidestep", "Player_Walk_Diagonal", "Player_Run_Forward"
        };

        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 0f), new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f)
        };

        internal static string ReviewAbsolutePath => Absolute(ReviewImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        [MenuItem("Bellerophon/Player/Inspect Detector Attached Static Locomotion Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            SourceMotions sources = RequireSourceMotions(scene);
            GameObject target = FindUnique(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target);
            Transform attachment = RequireAttachment(target);
            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static locomotion source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceOrder=" + string.Join(",", SourceNames))
                .AppendLine("targetAvatarPath=" +
                    AssetDatabase.GetAssetPath(targetAnimator.avatar))
                .AppendLine("presenceDetectorParent=" +
                    TransformPath(attachment.parent, target.transform))
                .AppendLine("presenceDetectorLocalPosition=" + Vec(attachment.localPosition))
                .AppendLine("presenceDetectorLocalRotation=" + Quat(attachment.localRotation))
                .AppendLine("presenceDetectorLocalScale=" + Vec(attachment.localScale));

            foreach (string sourceName in SourceNames)
            {
                GameObject source = FindUnique(scene, sourceName);
                Animator animator = RequireAnimator(source);
                AnimatorController controller = RequireController(animator, sourceName);
                AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                    throw new InvalidOperationException(
                        sourceName + " default state is missing.");
                report.AppendLine("source=" + sourceName)
                    .AppendLine("controller=" + AssetDatabase.GetAssetPath(controller))
                    .AppendLine("controllerSha256=" +
                        AssetHash(AssetDatabase.GetAssetPath(controller)))
                    .AppendLine("state=" + state.name)
                    .AppendLine("stateSpeed=" + F(state.speed))
                    .AppendLine("stateMirror=" + state.mirror)
                    .Append(DescribeMotion(state.motion, "motion"));
            }

            report.AppendLine("diagonalBlendType=" + sources.Diagonal.blendType)
                .AppendLine("diagonalChildren=" + sources.Diagonal.children.Length)
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteOutput("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[DetectorAttachedStaticLocomotion] Sources inspected read-only.");
        }

        [MenuItem("Bellerophon/Player/Apply Detector Attached Static Locomotion")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            Transform attachment = RequireAttachment(target);
            string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrWhiteSpace(avatarPath))
                throw new InvalidOperationException(TargetName + " Avatar is missing.");

            SourceMotions sources = RequireSourceMotions(scene);
            Dictionary<string, string> sourceAssetHashes =
                SourceAssetPaths(scene).ToDictionary(
                    path => path, AssetHash, StringComparer.Ordinal);
            Dictionary<string, string> sourceObjectHashes = SourceNames.ToDictionary(
                name => name,
                name => ObjectSignature(FindUnique(scene, name), true),
                StringComparer.Ordinal);
            string targetProtected = ObjectSignature(target, false);
            string attachmentProtected = AttachmentSignature(attachment, target.transform);
            Vector3 targetPosition = target.transform.position;
            Quaternion targetRotation = target.transform.rotation;
            Vector3 targetScale = target.transform.localScale;

            EnsureAssetFolder();
            AnimationClip idle = CopyClip(
                sources.Idle, IdleClipPath, "DetectorAttachedStatic_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath,
                "DetectorAttachedStatic_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath,
                "DetectorAttachedStatic_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath,
                "DetectorAttachedStatic_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, RunClipPath, "DetectorAttachedStatic_RunForward");
            AnimatorController controller = CreateController(
                sources.Diagonal, idle, forward, backward, sidestep, run);

            Undo.RecordObject(animator, "Connect Detector_Attached_Static locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(animator.avatar), "Avatar");
            RequireNear(target.transform.position, targetPosition, "target world position");
            RequireNear(target.transform.rotation, targetRotation, "target world rotation");
            RequireNear(target.transform.localScale, targetScale, "target local scale");
            RequireEqual(targetProtected, ObjectSignature(target, false),
                "target protected transforms and appearance");
            RequireEqual(attachmentProtected,
                AttachmentSignature(RequireAttachment(target), target.transform),
                "presence detector attachment");
            RequireHashes(sourceAssetHashes);
            RequireObjectHashes(sourceObjectHashes, scene);
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();

            WriteLines("source_asset_hashes.txt", sourceAssetHashes.Select(
                item => item.Key + "|" + item.Value));
            WriteLines("source_object_hashes.txt", sourceObjectHashes.Select(
                item => item.Key + "|" + item.Value));
            WriteOutput("target_protected.txt", targetProtected);
            WriteOutput("attachment_protected.txt", attachmentProtected);
            WriteOutput("application.txt", new StringBuilder()
                .AppendLine("Detector_Attached_Static locomotion application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine(
                    "sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("diagonalSourceTreeCopiedExactly=True")
                .AppendLine("avatarMask=None")
                .AppendLine("fullSourceBodyMotionEnabled=True")
                .AppendLine("presenceDetectorTransformChanged=False")
                .AppendLine("presenceDetectorAppearanceChanged=False")
                .AppendLine("presenceDetectorFollowsHead=True")
                .AppendLine("controller=" + ControllerPath)
                .ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[DetectorAttachedStaticLocomotion] Applied and saved.");
        }

        [MenuItem("Bellerophon/Player/Inspect Detector Attached Static Locomotion")]
        internal static void InspectStructure()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireController(animator, TargetName);
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller),
                "target controller path");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(TargetName + " Animator settings differ.");
            if (controller.layers.Length != 1)
                throw new InvalidOperationException(
                    "Detector locomotion controller must have one layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.avatarMask != null ||
                layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f))
                throw new InvalidOperationException(
                    "Detector locomotion must preserve the complete source body motion.");
            RequireFloatParameter(controller,
                DetectorAttachedStaticLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                DetectorAttachedStaticLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                DetectorAttachedStaticLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Detector default state is missing.");
            if (state.name != StateName || state.writeDefaultValues || state.mirror ||
                !Mathf.Approximately(state.speed, 1f))
                throw new InvalidOperationException("Detector locomotion state differs.");
            if (state.behaviours.Any(item => item == null) ||
                state.behaviours
                    .OfType<DetectorAttachedStaticLocomotionCycleBehaviour>().Count() != 1)
                throw new InvalidOperationException(
                    "Detector one-second sequence Behaviour differs.");
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("Detector root 2D Blend Tree is missing.");
            RequireRootTree(root);

            SourceMotions sources = RequireSourceMotions(scene);
            AnimationClip idle = RequireAsset<AnimationClip>(IdleClipPath);
            AnimationClip forward = RequireAsset<AnimationClip>(ForwardClipPath);
            AnimationClip backward = RequireAsset<AnimationClip>(BackwardClipPath);
            AnimationClip sidestep = RequireAsset<AnimationClip>(SidestepClipPath);
            AnimationClip run = RequireAsset<AnimationClip>(RunClipPath);
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");
            RequireMotionTreeEquivalent(
                sources.Diagonal,
                root.children[4].motion as BlendTree,
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [sources.Forward] = forward,
                    [sources.Sidestep] = sidestep
                },
                "WalkDiagonal");

            RequireBaselineHashes("source_asset_hashes.txt", AssetHash);
            RequireBaselineHashes("source_object_hashes.txt",
                name => ObjectSignature(FindUnique(scene, name), true));
            RequireEqual(ReadOutput("target_protected.txt"),
                ObjectSignature(target, false),
                "target protected transforms and appearance");
            RequireEqual(ReadOutput("attachment_protected.txt"),
                AttachmentSignature(RequireAttachment(target), target.transform),
                "presence detector protected state");

            WriteOutput("inspection.txt", new StringBuilder()
                .AppendLine("Detector_Attached_Static locomotion structural inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("controllerPath=" + ControllerPath)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=6")
                .AppendLine(
                    "motionOrder=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("sourceClipCurvesPreserved=True")
                .AppendLine("sourceClipEventsPreserved=True")
                .AppendLine("diagonalSourceTreePreserved=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAssetsChanged=False")
                .AppendLine("presenceDetectorTransformChanged=False")
                .AppendLine("presenceDetectorAppearanceChanged=False")
                .AppendLine("presenceDetectorFollowsHead=True")
                .AppendLine("sceneDirty=" + scene.isDirty)
                .ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[DetectorAttachedStaticLocomotion] Structural inspection passed.");
        }

        internal static void BeginNaturalRuntimeReview(string requestId, string logPath)
        {
            RequireEditMode();
            InspectStructure();
            DetectorAttachedStaticLocomotionPlayModeReview.Start(requestId, logPath);
        }

        [MenuItem("Bellerophon/Player/Capture Detector Attached Static Locomotion Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectStructure();
            string reviewPath = Absolute(ReviewImagePath);
            string reviewReportPath = Absolute(ReviewReportPath);
            if (!File.Exists(reviewPath) || !File.Exists(reviewReportPath))
                throw new InvalidOperationException(
                    "Passed two-cycle Detector locomotion review is missing.");
            string reviewReport = File.ReadAllText(reviewReportPath, Encoding.UTF8);
            if (!reviewReport.Contains("passed=True") ||
                !reviewReport.Contains("naturalPlayback=True") ||
                !reviewReport.Contains("cyclesObserved=2") ||
                !reviewReport.Contains("targetManipulatedByValidation=False") ||
                !reviewReport.Contains("presenceDetectorFollowsHead=True"))
                throw new InvalidOperationException(
                    "Detector locomotion natural review did not pass.");

            string finalPath = Absolute(FinalImagePath);
            string finalReportPath = Absolute(FinalReportPath);
            if (File.Exists(finalPath) || File.Exists(finalReportPath))
                throw new InvalidOperationException(
                    "The one final Detector locomotion contact sheet already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath) ??
                throw new InvalidOperationException("Final folder is unavailable."));

            var review = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Texture2D firstCycle = null;
            try
            {
                if (!review.LoadImage(File.ReadAllBytes(reviewPath), false) ||
                    review.width != PanelWidth * 6 || review.height != PanelHeight * 2)
                    throw new InvalidOperationException(
                        "Detector locomotion two-cycle review dimensions differ.");
                firstCycle = new Texture2D(
                    PanelWidth * 6, PanelHeight, TextureFormat.RGB24, false);
                firstCycle.SetPixels(review.GetPixels(
                    0, PanelHeight, PanelWidth * 6, PanelHeight));
                firstCycle.Apply(false, false);
                File.WriteAllBytes(finalPath, firstCycle.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(review);
                if (firstCycle != null) UnityEngine.Object.DestroyImmediate(firstCycle);
            }

            File.WriteAllText(finalReportPath, new StringBuilder()
                .AppendLine("Detector_Attached_Static locomotion final contact sheet")
                .AppendLine("leftToRight=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sourceMotionCurvesChanged=False")
                .AppendLine("presenceDetectorFollowsHead=True")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("finalSha256=" + Sha256File(finalPath))
                .ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[DetectorAttachedStaticLocomotion] Final contact sheet captured once.");
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(RequireScene(), TargetName);
        }

        internal static RuntimeMetrics MeasureRuntime(GameObject target)
        {
            GameObject reference = FindUnique(RequireScene(), ReferenceName);
            Transform referenceHead = FindHead(reference);
            Transform referenceAttachment = referenceHead.Cast<Transform>()
                .SingleOrDefault(item => item.name == ReferenceAttachmentName) ??
                throw new InvalidOperationException(
                    ReferenceName + " reference attachment is missing.");
            Transform attachment = RequireAttachment(target);
            Transform head = FindHead(target);
            return new RuntimeMetrics(
                Vector3.Distance(attachment.localPosition,
                    referenceAttachment.localPosition),
                Quaternion.Angle(attachment.localRotation,
                    referenceAttachment.localRotation),
                Vector3.Distance(attachment.localScale,
                    referenceAttachment.localScale),
                TransformPath(attachment.parent, target.transform) ==
                    TransformPath(head, target.transform),
                head.position,
                attachment.position);
        }

        internal static Texture2D CaptureRuntimePanel()
        {
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Bounds bounds = FullBounds(target);
            Vector3 direction =
                (target.transform.forward + target.transform.right * 0.68f).normalized;
            var cameraObject = new GameObject("DetectorLocomotion_ReadOnlyCamera");
            var lightObject = new GameObject("DetectorLocomotion_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                PanelWidth, PanelHeight, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.magnitude * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 10f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    target.transform.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 5f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    PanelWidth, PanelHeight, TextureFormat.RGB24, false);
                image.ReadPixels(
                    new Rect(0, 0, PanelWidth, PanelHeight), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        internal static void ComposeReview(
            IReadOnlyList<Texture2D> panels,
            string destination)
        {
            if (panels.Count != 12 || panels.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Detector review requires twelve runtime panels.");
            var composite = new Texture2D(
                PanelWidth * 6, PanelHeight * 2, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < panels.Count; index++)
                {
                    int column = index % 6;
                    int row = index < 6 ? 1 : 0;
                    composite.SetPixels32(
                        column * PanelWidth, row * PanelHeight,
                        PanelWidth, PanelHeight, panels[index].GetPixels32());
                }
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException("Review folder is unavailable."));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        internal static void WriteReviewReport(string contents)
        {
            string path = Absolute(ReviewReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ??
                throw new InvalidOperationException("Review report folder is unavailable."));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        internal static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..",
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static SourceMotions RequireSourceMotions(Scene scene)
        {
            Motion[] motions = SourceNames.Select(name =>
            {
                AnimatorController controller = RequireController(
                    RequireAnimator(FindUnique(scene, name)), name);
                return controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new InvalidOperationException(
                        name + " default motion is missing.");
            }).ToArray();
            if (!(motions[0] is AnimationClip idle) ||
                !(motions[1] is AnimationClip forward) ||
                !(motions[2] is AnimationClip backward) ||
                !(motions[3] is AnimationClip sidestep) ||
                !(motions[4] is BlendTree diagonal) ||
                !(motions[5] is AnimationClip run))
                throw new InvalidOperationException(
                    "Requested source motion types changed.");
            if (diagonal.blendType != BlendTreeType.Simple1D ||
                diagonal.children.Length != 2)
                throw new InvalidOperationException(
                    "Player_Walk_Diagonal source Blend Tree changed.");
            AnimatorController diagonalController = RequireController(
                RequireAnimator(FindUnique(scene, SourceNames[4])), SourceNames[4]);
            AnimatorControllerParameter parameter = diagonalController.parameters
                .SingleOrDefault(item => item.name == diagonal.blendParameter);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, 0.5f))
                throw new InvalidOperationException(
                    "Player_Walk_Diagonal source parameter differs.");
            return new SourceMotions(idle, forward, backward, sidestep, diagonal, run);
        }

        private static IEnumerable<string> SourceAssetPaths(Scene scene)
        {
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in SourceNames)
            {
                AnimatorController controller = RequireController(
                    RequireAnimator(FindUnique(scene, name)), name);
                AddAssetPath(paths, controller);
                Motion motion = controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new InvalidOperationException(name + " default motion is missing.");
                AddMotionAssetPaths(paths, motion);
            }
            return paths.OrderBy(path => path, StringComparer.Ordinal);
        }

        private static void AddMotionAssetPaths(ISet<string> paths, Motion motion)
        {
            AddAssetPath(paths, motion);
            if (!(motion is BlendTree tree)) return;
            foreach (ChildMotion child in tree.children)
                if (child.motion != null) AddMotionAssetPaths(paths, child.motion);
        }

        private static void AddAssetPath(ISet<string> paths, UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
        }

        private static AnimationClip CopyClip(
            AnimationClip source,
            string path,
            string name)
        {
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null)
            {
                copy = new AnimationClip();
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                EditorUtility.SetDirty(copy);
            }
            RequireCopiedClip(source, copy, name);
            return copy;
        }

        private static AnimatorController CreateController(
            BlendTree sourceDiagonal,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip backward,
            AnimationClip sidestep,
            AnimationClip run)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorStateMachine stateMachine = layer.stateMachine;
            foreach (AnimatorState existing in stateMachine.states
                .Select(item => item.state).ToArray())
                stateMachine.RemoveState(existing);
            foreach (AnimatorStateMachine existing in stateMachine.stateMachines
                .Select(item => item.stateMachine).ToArray())
                stateMachine.RemoveStateMachine(existing);
            foreach (BlendTree existing in AssetDatabase
                .LoadAllAssetsAtPath(ControllerPath)
                .OfType<BlendTree>().ToArray())
                UnityEngine.Object.DestroyImmediate(existing, true);

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(
                DetectorAttachedStaticLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                DetectorAttachedStaticLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = DetectorAttachedStaticLocomotionCycleBehaviour
                    .DiagonalBlendParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f
            });
            layer.name = "Source Locomotion";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;

            ChildMotion[] diagonalChildren = sourceDiagonal.children;
            var clipMap = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)diagonalChildren[0].motion] = forward,
                [(AnimationClip)diagonalChildren[1].motion] = sidestep
            };
            BlendTree diagonal = CloneTree(
                sourceDiagonal, DiagonalTreeName, controller, clipMap);
            var root = new BlendTree
            {
                name = RootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter =
                    DetectorAttachedStaticLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY =
                    DetectorAttachedStaticLocomotionCycleBehaviour.MoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(root, controller);
            root.children = new[]
            {
                RootChild(idle, RequiredPositions[0]),
                RootChild(forward, RequiredPositions[1]),
                RootChild(backward, RequiredPositions[2]),
                RootChild(sidestep, RequiredPositions[3]),
                RootChild(diagonal, RequiredPositions[4]),
                RootChild(run, RequiredPositions[5])
            };
            AnimatorState state = stateMachine.AddState(StateName);
            state.motion = root;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            state.AddStateMachineBehaviour<
                DetectorAttachedStaticLocomotionCycleBehaviour>();
            stateMachine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(diagonal);
            return controller;
        }

        private static BlendTree CloneTree(
            BlendTree source,
            string name,
            AnimatorController owner,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap)
        {
            var copy = new BlendTree
            {
                name = name,
                blendType = source.blendType,
                blendParameter = source.blendParameter,
                blendParameterY = source.blendParameterY,
                useAutomaticThresholds = source.useAutomaticThresholds,
                minThreshold = source.minThreshold,
                maxThreshold = source.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(copy, owner);
            ChildMotion[] sourceChildren = source.children;
            var children = new ChildMotion[sourceChildren.Length];
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion child = sourceChildren[index];
                Motion motion;
                if (child.motion is AnimationClip sourceClip)
                {
                    if (!clipMap.TryGetValue(sourceClip, out AnimationClip mapped))
                        throw new InvalidOperationException(
                            "Diagonal source clip mapping is missing.");
                    motion = mapped;
                }
                else if (child.motion is BlendTree nested)
                    motion = CloneTree(
                        nested, name + "_" + index, owner, clipMap);
                else
                    throw new InvalidOperationException(
                        "Diagonal source contains an unsupported Motion.");
                children[index] = new ChildMotion
                {
                    motion = motion,
                    threshold = child.threshold,
                    position = child.position,
                    timeScale = child.timeScale,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    directBlendParameter = child.directBlendParameter
                };
            }
            copy.children = children;
            return copy;
        }

        private static ChildMotion RootChild(Motion motion, Vector2 position)
        {
            return new ChildMotion
            {
                motion = motion,
                position = position,
                timeScale = 1f,
                cycleOffset = 0f,
                mirror = false,
                threshold = 0f,
                directBlendParameter = string.Empty
            };
        }

        private static void RequireRootTree(BlendTree tree)
        {
            if (tree.name != RootTreeName ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter !=
                    DetectorAttachedStaticLocomotionCycleBehaviour.MoveXParameter ||
                tree.blendParameterY !=
                    DetectorAttachedStaticLocomotionCycleBehaviour.MoveYParameter ||
                tree.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Detector root Blend Tree differs.");
            string[] paths =
            {
                IdleClipPath, ForwardClipPath, BackwardClipPath,
                SidestepClipPath, ControllerPath, RunClipPath
            };
            ChildMotion[] children = tree.children;
            for (int index = 0; index < children.Length; index++)
            {
                if ((children[index].position - RequiredPositions[index]).sqrMagnitude >
                        0.00000001f ||
                    !Mathf.Approximately(children[index].timeScale, 1f) ||
                    !Mathf.Approximately(children[index].cycleOffset, 0f) ||
                    children[index].mirror ||
                    AssetDatabase.GetAssetPath(children[index].motion) != paths[index])
                    throw new InvalidOperationException(
                        "Detector root Blend Tree child differs at " + index + ".");
            }
        }

        private static void RequireMotionTreeEquivalent(
            BlendTree source,
            BlendTree copy,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap,
            string label)
        {
            if (copy == null || source.blendType != copy.blendType ||
                source.blendParameter != copy.blendParameter ||
                source.blendParameterY != copy.blendParameterY ||
                source.useAutomaticThresholds != copy.useAutomaticThresholds ||
                !Mathf.Approximately(source.minThreshold, copy.minThreshold) ||
                !Mathf.Approximately(source.maxThreshold, copy.maxThreshold) ||
                source.children.Length != copy.children.Length)
                throw new InvalidOperationException(label + " Blend Tree differs.");
            ChildMotion[] sourceChildren = source.children;
            ChildMotion[] copyChildren = copy.children;
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion a = sourceChildren[index];
                ChildMotion b = copyChildren[index];
                if (!Mathf.Approximately(a.threshold, b.threshold) ||
                    (a.position - b.position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(a.timeScale, b.timeScale) ||
                    !Mathf.Approximately(a.cycleOffset, b.cycleOffset) ||
                    a.mirror != b.mirror ||
                    a.directBlendParameter != b.directBlendParameter)
                    throw new InvalidOperationException(
                        label + " child differs at " + index + ".");
                if (a.motion is AnimationClip sourceClip)
                {
                    if (!(b.motion is AnimationClip copyClip) ||
                        !clipMap.TryGetValue(sourceClip, out AnimationClip expected) ||
                        copyClip != expected)
                        throw new InvalidOperationException(
                            label + " clip differs at " + index + ".");
                    RequireCopiedClip(sourceClip, copyClip,
                        label + " child " + index);
                }
                else if (a.motion is BlendTree sourceTree)
                    RequireMotionTreeEquivalent(
                        sourceTree, b.motion as BlendTree, clipMap,
                        label + " child " + index);
                else
                    throw new InvalidOperationException(
                        label + " contains an unsupported Motion.");
            }
        }

        private static void RequireCopiedClip(
            AnimationClip source,
            AnimationClip copy,
            string label)
        {
            RequireEqual(ClipSignature(source), ClipSignature(copy),
                label + " clip content");
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + F(clip.length))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                result.AppendLine("wrap=" + curve.preWrapMode + "," + curve.postWrapMode);
                foreach (Keyframe key in curve.keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        F(key.time), F(key.value), F(key.inTangent), F(key.outTangent),
                        F(key.inWeight), F(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetObjectReferenceCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("objectCurve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .AppendLine(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility
                    .GetObjectReferenceCurve(clip, binding))
                    result.AppendLine(F(key.time) + "|" + ObjectIdentity(key.value));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" +
                    ObjectIdentity(item.objectReferenceParameter) + "|" +
                    item.messageOptions);
            return Sha256Text(result.ToString());
        }

        private static string ObjectSignature(GameObject root, bool includeAnimator)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => TransformPath(item, root.transform),
                    StringComparer.Ordinal))
            {
                text.Append(TransformPath(item, root.transform)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .AppendLine(item.gameObject.activeSelf.ToString());
                Renderer renderer = item.GetComponent<Renderer>();
                if (renderer != null)
                {
                    text.Append("renderer|")
                        .Append(TransformPath(item, root.transform)).Append('|')
                        .AppendLine(string.Join(",", renderer.sharedMaterials.Select(
                            material => ObjectIdentity(material))));
                }
            }
            if (includeAnimator)
            {
                Animator animator = RequireAnimator(root);
                text.AppendLine("controller=" +
                    ObjectIdentity(animator.runtimeAnimatorController));
                text.AppendLine("avatar=" + ObjectIdentity(animator.avatar));
                text.AppendLine("applyRootMotion=" + animator.applyRootMotion);
            }
            return Sha256Text(text.ToString());
        }

        private static string AttachmentSignature(Transform attachment, Transform root)
        {
            var text = new StringBuilder()
                .AppendLine("path=" + TransformPath(attachment, root))
                .AppendLine("parent=" + TransformPath(attachment.parent, root))
                .AppendLine("position=" + Vec(attachment.localPosition))
                .AppendLine("rotation=" + Quat(attachment.localRotation))
                .AppendLine("scale=" + Vec(attachment.localScale));
            foreach (Renderer renderer in attachment.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => TransformPath(item.transform, root),
                    StringComparer.Ordinal))
            {
                text.Append("renderer|")
                    .Append(TransformPath(renderer.transform, root)).Append('|')
                    .AppendLine(string.Join(",", renderer.sharedMaterials.Select(
                        material => ObjectIdentity(material))));
            }
            foreach (MeshFilter filter in attachment.GetComponentsInChildren<MeshFilter>(true)
                .OrderBy(item => TransformPath(item.transform, root),
                    StringComparer.Ordinal))
                text.Append("mesh|")
                    .Append(TransformPath(filter.transform, root)).Append('|')
                    .AppendLine(ObjectIdentity(filter.sharedMesh));
            return Sha256Text(text.ToString());
        }

        private static string DescribeMotion(Motion motion, string label)
        {
            var result = new StringBuilder()
                .AppendLine(label + "Type=" + motion.GetType().Name)
                .AppendLine(label + "Name=" + motion.name)
                .AppendLine(label + "Path=" + AssetDatabase.GetAssetPath(motion));
            if (motion is AnimationClip clip)
                result.AppendLine(label + "Length=" + F(clip.length))
                    .AppendLine(label + "FrameRate=" + F(clip.frameRate))
                    .AppendLine(label + "Events=" +
                        AnimationUtility.GetAnimationEvents(clip).Length)
                    .AppendLine(label + "CurveCount=" +
                        AnimationUtility.GetCurveBindings(clip).Length)
                    .AppendLine(label + "Signature=" + ClipSignature(clip));
            else if (motion is BlendTree tree)
                result.AppendLine(label + "BlendType=" + tree.blendType)
                    .AppendLine(label + "ChildCount=" + tree.children.Length)
                    .AppendLine(label + "BlendParameter=" + tree.blendParameter);
            return result.ToString();
        }

        private static Transform RequireAttachment(GameObject target)
        {
            Transform[] matches = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == AttachmentName).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    TargetName + " expected one " + AttachmentName +
                    "; found " + matches.Length + ".");
            Transform head = FindHead(target);
            if (matches[0].parent != head)
                throw new InvalidOperationException(
                    AttachmentName + " is not a direct Head child.");
            return matches[0];
        }

        private static Transform FindHead(GameObject root)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isHuman)
            {
                Transform humanoidHead = animator.GetBoneTransform(HumanBodyBones.Head);
                if (humanoidHead != null) return humanoidHead;
            }
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(item =>
                           item.name.Equals("Head", StringComparison.OrdinalIgnoreCase) ||
                           item.name.EndsWith(":Head", StringComparison.OrdinalIgnoreCase)) ??
                   throw new InvalidOperationException(root.name + " Head bone is missing.");
        }

        private static Bounds FullBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(TargetName + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.12f);
            return bounds;
        }

        private static void RequireFloatParameter(
            AnimatorController controller,
            string name,
            float expected)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null ||
                parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expected))
                throw new InvalidOperationException(
                    "Detector controller float parameter differs: " + name);
        }

        private static void RequireHashes(
            IReadOnlyDictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, AssetHash(item.Key), item.Key);
        }

        private static void RequireObjectHashes(
            IReadOnlyDictionary<string, string> hashes,
            Scene scene)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value,
                    ObjectSignature(FindUnique(scene, item.Key), true), item.Key);
        }

        private static void RequireBaselineHashes(
            string fileName,
            Func<string, string> actual)
        {
            foreach (string line in ReadOutput(fileName)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.LastIndexOf('|');
                if (separator <= 0)
                    throw new InvalidOperationException(
                        "Invalid Detector hash baseline: " + fileName);
                RequireEqual(
                    line.Substring(separator + 1),
                    actual(line.Substring(0, separator)),
                    line.Substring(0, separator));
            }
        }

        private static Animator RequireAnimator(GameObject root)
        {
            return root.GetComponent<Animator>() ??
                throw new InvalidOperationException(root.name + " Animator is missing.");
        }

        private static AnimatorController RequireController(
            Animator animator,
            string label)
        {
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(
                    label + " does not use an AnimatorController.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Detector locomotion setup requires Edit Mode.");
        }

        private static void EnsureAssetFolder()
        {
            string current = "Assets";
            foreach (string part in AssetFolder.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new InvalidOperationException(
                "Detector locomotion asset is missing: " + path);
        }

        private static string TransformPath(Transform item, Transform root)
        {
            return AnimationUtility.CalculateTransformPath(item, root);
        }

        private static string AssetHash(string assetPath)
        {
            return Sha256File(Absolute(assetPath));
        }

        private static string Sha256File(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                value, out string guid, out long localId)
                ? guid + ":" + localId
                : value.GetType().FullName + ":" + value.name;
        }

        private static void WriteOutput(string fileName, string contents)
        {
            string folder = Absolute(OutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(
                Path.Combine(folder, fileName), contents, new UTF8Encoding(false));
        }

        private static void WriteLines(string fileName, IEnumerable<string> lines)
        {
            WriteOutput(fileName,
                string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }

        private static string ReadOutput(string fileName)
        {
            string path = Path.Combine(Absolute(OutputFolder), fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Detector locomotion baseline is missing.", path);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return F(value.x) + "," + F(value.y) + "," +
                   F(value.z) + "," + F(value.w);
        }

        private static void RequireNear(Vector3 actual, Vector3 expected, string label)
        {
            if ((actual - expected).sqrMagnitude > 0.0000000001f)
                throw new InvalidOperationException(
                    label + " differs. actual=" + Vec(actual) +
                    " expected=" + Vec(expected));
        }

        private static void RequireNear(
            Quaternion actual,
            Quaternion expected,
            string label)
        {
            if (Quaternion.Angle(actual, expected) > 0.001f)
                throw new InvalidOperationException(
                    label + " differs. actual=" + Quat(actual) +
                    " expected=" + Quat(expected));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    label + " differs. expected=" + expected + " actual=" + actual);
        }

        private readonly struct SourceMotions
        {
            internal SourceMotions(
                AnimationClip idle,
                AnimationClip forward,
                AnimationClip backward,
                AnimationClip sidestep,
                BlendTree diagonal,
                AnimationClip run)
            {
                Idle = idle;
                Forward = forward;
                Backward = backward;
                Sidestep = sidestep;
                Diagonal = diagonal;
                Run = run;
            }

            internal AnimationClip Idle { get; }
            internal AnimationClip Forward { get; }
            internal AnimationClip Backward { get; }
            internal AnimationClip Sidestep { get; }
            internal BlendTree Diagonal { get; }
            internal AnimationClip Run { get; }
        }

        internal readonly struct RuntimeMetrics
        {
            internal RuntimeMetrics(
                float localPositionError,
                float localRotationError,
                float localScaleError,
                bool directHeadChild,
                Vector3 headWorldPosition,
                Vector3 attachmentWorldPosition)
            {
                LocalPositionError = localPositionError;
                LocalRotationError = localRotationError;
                LocalScaleError = localScaleError;
                DirectHeadChild = directHeadChild;
                HeadWorldPosition = headWorldPosition;
                AttachmentWorldPosition = attachmentWorldPosition;
            }

            internal float LocalPositionError { get; }
            internal float LocalRotationError { get; }
            internal float LocalScaleError { get; }
            internal bool DirectHeadChild { get; }
            internal Vector3 HeadWorldPosition { get; }
            internal Vector3 AttachmentWorldPosition { get; }
        }
    }

    [InitializeOnLoad]
    internal static class DetectorAttachedStaticLocomotionPlayModeReview
    {
        private const string PendingKey =
            "Bellerophon.DetectorLocomotionReview.Pending";
        private const string RequestIdKey =
            "Bellerophon.DetectorLocomotionReview.RequestId";
        private const string LogPathKey =
            "Bellerophon.DetectorLocomotionReview.LogPath";
        private const string StateKey =
            "Bellerophon.DetectorLocomotionReview.State";
        private const string FailureKey =
            "Bellerophon.DetectorLocomotionReview.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int MotionCount = 6;
        private const int PanelCount = 12;
        private const float CapturePhaseTime = 0.48f;
        private const double TimeoutSeconds = 40d;

        private static readonly Color[] PhaseColors =
        {
            new Color(0.35f, 0.75f, 1f),
            new Color(0.3f, 0.9f, 0.45f),
            new Color(1f, 0.65f, 0.25f),
            new Color(0.9f, 0.35f, 0.85f),
            new Color(0.95f, 0.85f, 0.25f),
            new Color(1f, 0.3f, 0.3f)
        };

        private static bool initialized;
        private static double runtimeStart;
        private static int baseAbsolutePhase = -1;
        private static int nextPanel;
        private static GameObject target;
        private static Animator animator;
        private static Texture2D[] panels;
        private static Vector3 rootPosition;
        private static Vector3 firstHeadPosition;
        private static float maximumHeadTravel;
        private static float maximumAttachmentLocalPositionError;
        private static float maximumAttachmentLocalRotationError;
        private static float maximumAttachmentLocalScaleError;
        private static readonly List<double> SampleTimes = new List<double>();
        private static readonly List<string> Observations = new List<string>();

        static DetectorAttachedStaticLocomotionPlayModeReview()
        {
            if (SessionState.GetBool(PendingKey, false)) Subscribe();
        }

        internal static void Start(string requestId, string logPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Detector locomotion review must start in Edit Mode.");
            if (SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException(
                    "A Detector locomotion review is already pending.");

            string tempFolder = DetectorAttachedStaticLocomotionTools.Absolute(
                DetectorAttachedStaticLocomotionTools.TempReviewFolder);
            Directory.CreateDirectory(tempFolder);
            foreach (string file in Directory.GetFiles(tempFolder)) File.Delete(file);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(RequestIdKey, requestId);
            SessionState.SetString(LogPathKey, logPath);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update -= Tick;
                return;
            }

            try
            {
                int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying) return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before the two-cycle review completed.");
                    CaptureTick();
                    return;
                }
                if (state == WaitingForEditMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteInEditMode();
                    return;
                }
                throw new InvalidOperationException(
                    "Unknown Detector locomotion review state: " + state);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void InitializeRuntime()
        {
            initialized = true;
            runtimeStart = EditorApplication.timeSinceStartup;
            baseAbsolutePhase = -1;
            nextPanel = 0;
            target = DetectorAttachedStaticLocomotionTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Detector_Attached_Static runtime Animator is missing.");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.runtimeAnimatorController == null)
                throw new InvalidOperationException(
                    "Detector_Attached_Static runtime Animator settings differ.");
            panels = new Texture2D[PanelCount];
            rootPosition = target.transform.position;
            firstHeadPosition = Vector3.zero;
            maximumHeadTravel = 0f;
            maximumAttachmentLocalPositionError = 0f;
            maximumAttachmentLocalRotationError = 0f;
            maximumAttachmentLocalScaleError = 0f;
            SampleTimes.Clear();
            Observations.Clear();
        }

        private static void CaptureTick()
        {
            if (!initialized) InitializeRuntime();
            if (EditorApplication.timeSinceStartup - runtimeStart > TimeoutSeconds)
                throw new TimeoutException(
                    "Detector locomotion natural two-cycle review exceeded 40 seconds.");
            if (!animator.isInitialized ||
                !DetectorAttachedStaticLocomotionCycleBehaviour.TryGetSequenceState(
                    animator, out int absolutePhase, out int phase,
                    out float phaseElapsed))
                return;

            if (baseAbsolutePhase < 0)
            {
                if (phase != 0 || phaseElapsed > 0.65f) return;
                baseAbsolutePhase = absolutePhase;
            }

            if (nextPanel < PanelCount)
            {
                int expectedAbsolute = baseAbsolutePhase + nextPanel;
                if (absolutePhase < expectedAbsolute || phaseElapsed < CapturePhaseTime)
                    return;
                if (absolutePhase > expectedAbsolute)
                    throw new InvalidOperationException(
                        "A Detector locomotion phase was missed during natural playback.");
                int expectedPhase = nextPanel % MotionCount;
                if (phase != expectedPhase)
                    throw new InvalidOperationException(
                        "Detector locomotion phase order differs.");
                Vector2 expectedMove =
                    DetectorAttachedStaticLocomotionCycleBehaviour.MotionPosition(
                        expectedPhase);
                float moveX = animator.GetFloat(
                    DetectorAttachedStaticLocomotionCycleBehaviour.MoveXParameter);
                float moveY = animator.GetFloat(
                    DetectorAttachedStaticLocomotionCycleBehaviour.MoveYParameter);
                if (Mathf.Abs(moveX - expectedMove.x) > 0.001f ||
                    Mathf.Abs(moveY - expectedMove.y) > 0.001f)
                    throw new InvalidOperationException(
                        "Detector locomotion Blend Tree parameters differ.");
                if (Vector3.Distance(target.transform.position, rootPosition) > 0.0001f)
                    throw new InvalidOperationException(
                        "Detector_Attached_Static root moved during in-place playback.");

                DetectorAttachedStaticLocomotionTools.RuntimeMetrics metrics =
                    DetectorAttachedStaticLocomotionTools.MeasureRuntime(target);
                if (!metrics.DirectHeadChild)
                    throw new InvalidOperationException(
                        "Presence detector stopped following the Head bone.");
                maximumAttachmentLocalPositionError = Mathf.Max(
                    maximumAttachmentLocalPositionError, metrics.LocalPositionError);
                maximumAttachmentLocalRotationError = Mathf.Max(
                    maximumAttachmentLocalRotationError, metrics.LocalRotationError);
                maximumAttachmentLocalScaleError = Mathf.Max(
                    maximumAttachmentLocalScaleError, metrics.LocalScaleError);
                if (nextPanel == 0) firstHeadPosition = metrics.HeadWorldPosition;
                maximumHeadTravel = Mathf.Max(maximumHeadTravel,
                    Vector3.Distance(firstHeadPosition, metrics.HeadWorldPosition));

                Texture2D panel =
                    DetectorAttachedStaticLocomotionTools.CaptureRuntimePanel();
                DrawBorder(panel, PhaseColors[phase]);
                panels[nextPanel] = panel;
                SampleTimes.Add(EditorApplication.timeSinceStartup);
                Observations.Add(
                    "panel=" + nextPanel +
                    "|cycle=" + (nextPanel / MotionCount + 1) +
                    "|phase=" + phase +
                    "|motion=" +
                        DetectorAttachedStaticLocomotionCycleBehaviour.MotionName(phase) +
                    "|phaseElapsed=" + F(phaseElapsed) +
                    "|moveX=" + F(moveX) +
                    "|moveY=" + F(moveY) +
                    "|head=" + Vec(metrics.HeadWorldPosition) +
                    "|detector=" + Vec(metrics.AttachmentWorldPosition));
                nextPanel++;
                return;
            }

            if (absolutePhase < baseAbsolutePhase + PanelCount) return;
            if (absolutePhase != baseAbsolutePhase + PanelCount || phase != 0)
                throw new InvalidOperationException(
                    "RunForward did not return naturally to Idle after two cycles.");
            FinishPlayMode();
        }

        private static void FinishPlayMode()
        {
            if (panels == null || panels.Length != PanelCount ||
                panels.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Detector locomotion runtime panels are incomplete.");
            float minimumInterval = float.MaxValue;
            float maximumInterval = 0f;
            for (int index = 1; index < SampleTimes.Count; index++)
            {
                float interval = (float)(SampleTimes[index] - SampleTimes[index - 1]);
                minimumInterval = Mathf.Min(minimumInterval, interval);
                maximumInterval = Mathf.Max(maximumInterval, interval);
            }
            if (minimumInterval < 0.75f || maximumInterval > 1.25f)
                throw new InvalidOperationException(
                    "Natural one-second phase timing differs. min=" +
                    F(minimumInterval) + " max=" + F(maximumInterval));
            if (maximumAttachmentLocalPositionError > 0.00001f ||
                maximumAttachmentLocalRotationError > 0.01f ||
                maximumAttachmentLocalScaleError > 0.00001f)
                throw new InvalidOperationException(
                    "Presence detector local attachment state changed during playback.");
            if (maximumHeadTravel <= 0.001f)
                throw new InvalidOperationException(
                    "Head motion was not observed across the copied locomotion sequence.");

            DetectorAttachedStaticLocomotionTools.ComposeReview(
                panels, DetectorAttachedStaticLocomotionTools.ReviewAbsolutePath);
            var report = new StringBuilder()
                .AppendLine("Detector_Attached_Static natural locomotion review")
                .AppendLine("passed=True")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=2")
                .AppendLine("panelsCaptured=12")
                .AppendLine(
                    "sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("returnedToIdleAfterRun=True")
                .AppendLine("minimumObservedPanelInterval=" + F(minimumInterval))
                .AppendLine("maximumObservedPanelInterval=" + F(maximumInterval))
                .AppendLine("rootMotionApplied=False")
                .AppendLine("maximumHeadTravelMeters=" + F(maximumHeadTravel))
                .AppendLine("presenceDetectorFollowsHead=True")
                .AppendLine("maximumAttachmentLocalPositionError=" +
                    F(maximumAttachmentLocalPositionError))
                .AppendLine("maximumAttachmentLocalRotationErrorDegrees=" +
                    F(maximumAttachmentLocalRotationError))
                .AppendLine("maximumAttachmentLocalScaleError=" +
                    F(maximumAttachmentLocalScaleError));
            foreach (string observation in Observations) report.AppendLine(observation);
            DetectorAttachedStaticLocomotionTools.WriteReviewReport(report.ToString());
            CleanupRuntime();
            SessionState.SetInt(StateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void CompleteInEditMode()
        {
            string requestId = SessionState.GetString(RequestIdKey, string.Empty);
            string logPath = SessionState.GetString(LogPathKey, string.Empty);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            if (string.IsNullOrWhiteSpace(failure))
            {
                DetectorAttachedStaticLocomotionTools.InspectStructure();
                if (!File.Exists(
                        DetectorAttachedStaticLocomotionTools.ReviewAbsolutePath))
                    throw new InvalidOperationException(
                        "Detector locomotion review image is missing after Play Mode.");
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=passed" + Environment.NewLine +
                    "Detector_Attached_Static natural two-cycle review completed.");
            }
            else
            {
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=failed" + Environment.NewLine +
                    failure);
            }
            ClearSession();
        }

        private static void Fail(Exception exception)
        {
            CleanupRuntime();
            SessionState.SetString(FailureKey, exception.ToString());
            SessionState.SetInt(StateKey, WaitingForEditMode);
            Debug.LogWarning(
                "Detector locomotion natural review failed: " + exception.Message);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.ExitPlaymode();
            else
                CompleteInEditMode();
        }

        private static void CleanupRuntime()
        {
            if (panels != null)
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            initialized = false;
            target = null;
            animator = null;
            panels = null;
            SampleTimes.Clear();
            Observations.Clear();
        }

        private static void ClearSession()
        {
            EditorApplication.update -= Tick;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(RequestIdKey);
            SessionState.EraseString(LogPathKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseString(FailureKey);
        }

        private static void WriteBridgeLog(string path, string contents)
        {
            string absolute = DetectorAttachedStaticLocomotionTools.Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Bridge log folder is unavailable."));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
        }

        private static void DrawBorder(Texture2D image, Color color)
        {
            const int width = 6;
            for (int y = 0; y < image.height; y++)
            for (int x = 0; x < image.width; x++)
                if (x < width || y < width ||
                    x >= image.width - width || y >= image.height - width)
                    image.SetPixel(x, y, color);
            image.Apply(false, false);
        }

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z);
        }
    }
}
