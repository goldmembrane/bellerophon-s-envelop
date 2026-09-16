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
    internal static class ElectricMineIdleLocomotionTools
    {
        internal const string TargetName = "ElectricMine_Idle";
        internal const string OutputFolder =
            "docs/validation/ElectricMineIdleLocomotion";
        internal const string TempFolder = "Temp/ElectricMineIdleLocomotion";
        internal const string ReviewImagePath = TempFolder + "/Review.png";
        internal const string ReviewReportPath = TempFolder + "/Review.txt";
        internal const string FinalImagePath = OutputFolder + "/Final.png";
        internal const string FinalReportPath = OutputFolder + "/Final.txt";
        internal const string ControllerPath =
            AssetFolder + "/ElectricMineIdle_Locomotion.controller";
        internal const string ArmedTargetName = "ElectricMine_Armed_Idle";
        internal const string ArmedControllerPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_Locomotion.controller";
        internal const string ArmedReviewFolder =
            "Temp/ElectricMineArmedIdleLocomotion";
        internal const string ArmedReviewImagePath =
            ArmedReviewFolder + "/FinalContactSheet.png";
        internal const string ArmedReviewReportPath =
            ArmedReviewFolder + "/FinalReport.txt";

        private const string AssetFolder =
            "Assets/_Project/Art/Player/Animations/ElectricMineIdleLocomotion";
        private const string ArmedAssetFolder =
            "Assets/_Project/Animations/ElectricMineArmedIdleLocomotion";
        private const string ArmedIdleClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_Idle.anim";
        private const string ArmedForwardClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_WalkForward.anim";
        private const string ArmedBackwardClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_WalkBackward.anim";
        private const string ArmedSidestepClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_Sidestep.anim";
        private const string ArmedRunClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_RunForward.anim";
        private const string ArmedJumpClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_Jump.anim";
        private const string ArmedGripClipPath =
            ArmedAssetFolder + "/ElectricMineArmedIdle_CurrentGrip.anim";
        private const string ArmedStateName = "ElectricMineArmedIdleLocomotion2D";
        private const string ArmedRootTreeName = "ElectricMineArmedIdleSevenMotion2D";
        private const string ArmedDiagonalTreeName =
            "ElectricMineArmedIdleWalkDiagonalSourceExact";
        private const string IdleClipPath =
            AssetFolder + "/ElectricMineIdle_Idle.anim";
        private const string ForwardClipPath =
            AssetFolder + "/ElectricMineIdle_WalkForward.anim";
        private const string BackwardClipPath =
            AssetFolder + "/ElectricMineIdle_WalkBackward.anim";
        private const string SidestepClipPath =
            AssetFolder + "/ElectricMineIdle_Sidestep.anim";
        private const string RunClipPath =
            AssetFolder + "/ElectricMineIdle_RunForward.anim";
        private const string GripClipPath =
            AssetFolder + "/ElectricMineIdle_CurrentGrip.anim";
        private const string StateName = "ElectricMineIdleLocomotion2D";
        private const string RootTreeName = "ElectricMineIdleSixMotion2D";
        private const string DiagonalTreeName =
            "ElectricMineIdleWalkDiagonalSourceExact";
        private const string RightArmPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm";
        private const string SpinePath = "Armature/Hips/Spine02/Spine01/Spine";
        private const string RightShoulderPath = SpinePath + "/RightShoulder";
        private const string PropName = "ElectricMine_Prop";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.02f;

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

        private static readonly Vector2[] ArmedRequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 0f), new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f), new Vector2(0f, -2f)
        };

        internal static string ReviewAbsolutePath => Absolute(ReviewImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Idle Locomotion Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            SourceMotions sources = RequireSourceMotions(scene);
            GameObject target = FindUnique(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target);
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            var report = new StringBuilder()
                .AppendLine("ElectricMine_Idle locomotion source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceOrder=" + string.Join(",", SourceNames))
                .AppendLine("targetAvatar=" +
                    AssetDatabase.GetAssetPath(targetAnimator.avatar))
                .AppendLine("rightGripBoneCount=" + GripPose(target).Paths.Length)
                .AppendLine("propLocalState=" + TransformState(prop));
            foreach (string sourceName in SourceNames)
            {
                AnimatorController controller = RequireController(
                    RequireAnimator(FindUnique(scene, sourceName)), sourceName);
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
                .AppendLine("diagonalChildCount=" + sources.Diagonal.children.Length)
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            Write("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineIdleLocomotion] Sources inspected read-only.");
        }

        [MenuItem("Bellerophon/Player/Apply Electric Mine Idle Locomotion")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrWhiteSpace(avatarPath))
                throw new InvalidOperationException(TargetName + " Avatar is missing.");
            SourceMotions sources = RequireSourceMotions(scene);
            Dictionary<string, string> sourceHashes = SourceAssetPaths(scene)
                .ToDictionary(path => path, AssetHash, StringComparer.Ordinal);
            string outsideBefore = SceneSignatureOutsideTarget(scene);
            string targetBefore = TargetProtectedSignature(target);
            string propBefore = MineSignature(target);
            GripData grip = GripPose(target);
            Vector3 targetPosition = target.transform.position;
            Quaternion targetRotation = target.transform.rotation;
            Vector3 targetScale = target.transform.localScale;

            EnsureFolder(AssetFolder);
            AnimationClip idle = CopyClip(
                sources.Idle, IdleClipPath, "ElectricMineIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath, "ElectricMineIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath, "ElectricMineIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath, "ElectricMineIdle_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, RunClipPath, "ElectricMineIdle_RunForward");
            AnimationClip gripClip = CreateGripClip(grip);
            AnimatorController controller = CreateController(
                sources.Diagonal, idle, forward, backward, sidestep, run,
                gripClip, grip);

            Undo.RecordObject(animator, "Connect ElectricMine_Idle 2D locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(animator.avatar), "Avatar");
            RequireNear(target.transform.position, targetPosition, "target position");
            RequireNear(target.transform.rotation, targetRotation, "target rotation");
            RequireNear(target.transform.localScale, targetScale, "target scale");
            RequireEqual(outsideBefore, SceneSignatureOutsideTarget(scene),
                "scene objects outside ElectricMine_Idle");
            RequireEqual(targetBefore, TargetProtectedSignature(target),
                "ElectricMine_Idle transforms and appearance");
            RequireEqual(propBefore, MineSignature(target), "electric mine state");
            RequireHashes(sourceHashes);
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");
            RequireGripClipMatches(grip, gripClip);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            WriteLines("source_asset_hashes.txt", sourceHashes.Select(
                item => item.Key + "|" + item.Value));
            Write("target_protected.txt", targetBefore);
            Write("mine_protected.txt", propBefore);
            Write("application.txt", new StringBuilder()
                .AppendLine("ElectricMine_Idle 2D locomotion application")
                .AppendLine("sceneSaved=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=6")
                .AppendLine("diagonalSourceTreeCopiedExactly=True")
                .AppendLine("secondsPerMotion=1")
                .AppendLine(
                    "sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("rightShoulderAndSpineSourceMotionEnabled=True")
                .AppendLine("gripOverrideCapturedFromCurrentTarget=True")
                .AppendLine("rightArmGripPosePreserved=True")
                .AppendLine("electricMineTransformChanged=False")
                .AppendLine("electricMineFollowsRightHand=True")
                .AppendLine("controller=" + ControllerPath)
                .ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineIdleLocomotion] Applied and saved.");
        }

        [MenuItem("Bellerophon/Player/Apply Electric Mine Armed Idle Locomotion")]
        internal static void ApplyArmed()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, ArmedTargetName);
            Animator animator = RequireAnimator(target);
            string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrWhiteSpace(avatarPath))
                throw new InvalidOperationException(ArmedTargetName + " Avatar is missing.");

            SourceMotions sources = RequireSourceMotions(scene);
            AnimationClip jumpSource = RequireSourceClip(scene, "Player_Jump");
            var sourceSignatures = new Dictionary<AnimationClip, string>
            {
                [sources.Idle] = ClipSignature(sources.Idle),
                [sources.Forward] = ClipSignature(sources.Forward),
                [sources.Backward] = ClipSignature(sources.Backward),
                [sources.Sidestep] = ClipSignature(sources.Sidestep),
                [sources.Run] = ClipSignature(sources.Run),
                [jumpSource] = ClipSignature(jumpSource)
            };
            string targetBefore = TargetProtectedSignature(target);
            string mineBefore = MineSignature(target);
            GripData grip = GripPose(target);
            Vector3 targetPosition = target.transform.position;
            Quaternion targetRotation = target.transform.rotation;
            Vector3 targetScale = target.transform.localScale;

            EnsureFolder(ArmedAssetFolder);
            AnimationClip idle = CopyClip(
                sources.Idle, ArmedIdleClipPath, "ElectricMineArmedIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ArmedForwardClipPath,
                "ElectricMineArmedIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, ArmedBackwardClipPath,
                "ElectricMineArmedIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, ArmedSidestepClipPath,
                "ElectricMineArmedIdle_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, ArmedRunClipPath, "ElectricMineArmedIdle_RunForward");
            AnimationClip jump = CopyClip(
                jumpSource, ArmedJumpClipPath, "ElectricMineArmedIdle_Jump");
            AnimationClip gripClip = CreateArmedGripClip(grip);
            AnimatorController controller = CreateArmedController(
                sources.Diagonal,
                idle,
                forward,
                backward,
                sidestep,
                run,
                jump,
                gripClip,
                grip);

            Undo.RecordObject(animator, "Connect ElectricMine_Armed_Idle locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(animator.avatar), "Avatar");
            RequireNear(target.transform.position, targetPosition, "target position");
            RequireNear(target.transform.rotation, targetRotation, "target rotation");
            RequireNear(target.transform.localScale, targetScale, "target scale");
            RequireEqual(targetBefore, TargetProtectedSignature(target),
                "ElectricMine_Armed_Idle transforms and appearance");
            RequireEqual(mineBefore, MineSignature(target),
                "ElectricMine_Armed_Idle electric mine state");
            RequireCopiedClip(sources.Idle, idle, "Armed Idle");
            RequireCopiedClip(sources.Forward, forward, "Armed WalkForward");
            RequireCopiedClip(sources.Backward, backward, "Armed WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Armed Sidestep");
            RequireCopiedClip(sources.Run, run, "Armed RunForward");
            RequireCopiedClip(jumpSource, jump, "Armed Jump");
            foreach (KeyValuePair<AnimationClip, string> item in sourceSignatures)
                RequireEqual(item.Value, ClipSignature(item.Key),
                    item.Key.name + " source clip");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            AssetDatabase.SaveAssets();
            InspectArmedStructure();
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[ElectricMineArmedIdleLocomotion] Applied seven exact source motions " +
                "with one-second looping preview and preserved armed mine grip.");
        }

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Armed Idle Locomotion")]
        internal static void InspectArmedStructure()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, ArmedTargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireController(animator, ArmedTargetName);
            RequireEqual(ArmedControllerPath, AssetDatabase.GetAssetPath(controller),
                "armed controller path");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    "ElectricMine_Armed_Idle Animator differs.");
            if (controller.layers.Length != 2)
                throw new InvalidOperationException(
                    "Armed controller must have two layers.");
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException("Armed locomotion state is missing.");
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("Armed 2D Blend Tree is missing.");
            if (root.blendType != BlendTreeType.FreeformCartesian2D ||
                root.children.Length != 7)
                throw new InvalidOperationException(
                    "Armed 2D Blend Tree must contain seven motions.");
            for (int index = 0; index < ArmedRequiredPositions.Length; index++)
            {
                if (root.children[index].motion == null ||
                    Vector2.Distance(root.children[index].position,
                        ArmedRequiredPositions[index]) > 0.00001f)
                    throw new InvalidOperationException(
                        "Armed Blend Tree child differs at index " + index + ".");
            }
            ElectricMineArmedIdleLocomotionCycleBehaviour behaviour = state.behaviours
                .OfType<ElectricMineArmedIdleLocomotionCycleBehaviour>()
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    "Armed seven-motion cycle behaviour is missing.");
            if (behaviour.GripBoneCount != GripPose(target).Paths.Length)
                throw new InvalidOperationException("Armed grip bone count differs.");
            AnimatorState gripState = controller.layers[1].stateMachine.defaultState ??
                throw new InvalidOperationException("Armed grip state is missing.");
            AnimationClip gripClip = gripState.motion as AnimationClip ??
                throw new InvalidOperationException("Armed grip clip is missing.");
            RequireGripClipMatches(GripPose(target), gripClip);
            ElectricMineSetupTools.RequireRuntimeProp(target);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[ElectricMineArmedIdleLocomotion] Seven-motion structure passed. " +
                "secondsPerMotion=1|sequenceLoopsAfterJump=True|mineFollowsRightHand=True");
        }

        internal static void BeginArmedNaturalRuntimeReview(
            string requestId, string logPath)
        {
            RequireEditMode();
            InspectArmedStructure();
            DetectorAttachedStaticStartSetupTools.InspectHelmEnterStartView();
            ElectricMineArmedIdleLocomotionPlayModeReview.Start(requestId, logPath);
        }

        [MenuItem("Bellerophon/Player/Inspect Electric Mine Idle Locomotion")]
        internal static void InspectStructure()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireController(animator, TargetName);
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller),
                "controller path");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("ElectricMine_Idle Animator differs.");
            if (controller.layers.Length != 2)
                throw new InvalidOperationException("Controller must have two layers.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.avatarMask != null ||
                layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f))
                throw new InvalidOperationException("Controller layer settings differ.");
            RequireFloatParameter(controller,
                ElectricMineIdleLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                ElectricMineIdleLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                ElectricMineIdleLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Default state is missing.");
            if (state.name != StateName || state.writeDefaultValues || state.mirror ||
                !Mathf.Approximately(state.speed, 1f))
                throw new InvalidOperationException("Locomotion state settings differ.");
            ElectricMineIdleLocomotionCycleBehaviour behaviour = state.behaviours
                .OfType<ElectricMineIdleLocomotionCycleBehaviour>().SingleOrDefault() ??
                throw new InvalidOperationException("Cycle and grip behaviour differs.");
            if (behaviour.GripBoneCount != GripPose(target).Paths.Length)
                throw new InvalidOperationException("Stored grip bone count differs.");
            AnimatorControllerLayer gripLayer = controller.layers[1];
            if (gripLayer.name != "Current Grip Override" ||
                gripLayer.avatarMask != null ||
                gripLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(gripLayer.defaultWeight, 1f))
                throw new InvalidOperationException("Grip override layer differs.");
            AnimatorState gripState = gripLayer.stateMachine.defaultState ??
                throw new InvalidOperationException("Grip override state is missing.");
            AnimationClip gripClip = gripState.motion as AnimationClip ??
                throw new InvalidOperationException("Grip override clip is missing.");
            if (AssetDatabase.GetAssetPath(gripClip) != GripClipPath ||
                gripState.writeDefaultValues || gripState.mirror ||
                !Mathf.Approximately(gripState.speed, 1f))
                throw new InvalidOperationException("Grip override state differs.");
            RequireGripClipMatches(GripPose(target), gripClip);
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("Root 2D Blend Tree is missing.");
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
                sources.Diagonal, root.children[4].motion as BlendTree,
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [sources.Forward] = forward,
                    [sources.Sidestep] = sidestep
                }, "WalkDiagonal");
            RequireBaselineHashes("source_asset_hashes.txt");
            RequireEqual(Read("target_protected.txt"),
                TargetProtectedSignature(target), "target protected state");
            RequireEqual(Read("mine_protected.txt"),
                MineSignature(target), "electric mine protected state");
            Write("inspection.txt", new StringBuilder()
                .AppendLine("ElectricMine_Idle locomotion structural inspection")
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
                .AppendLine("rightShoulderAndSpineSourceMotionEnabled=True")
                .AppendLine("gripOverrideCapturedFromCurrentTarget=True")
                .AppendLine("rightArmGripPosePreserved=True")
                .AppendLine("electricMineTransformChanged=False")
                .AppendLine("electricMineFollowsRightHand=True")
                .ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineIdleLocomotion] Structural inspection passed.");
        }

        internal static void BeginNaturalRuntimeReview(string requestId, string logPath)
        {
            RequireEditMode();
            InspectStructure();
            ElectricMineIdleLocomotionPlayModeReview.Start(requestId, logPath);
        }

        [MenuItem("Bellerophon/Player/Capture Electric Mine Idle Locomotion Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectStructure();
            string review = Absolute(ReviewImagePath);
            string reviewReport = Absolute(ReviewReportPath);
            if (!File.Exists(review) || !File.Exists(reviewReport))
                throw new InvalidOperationException("Natural locomotion review is missing.");
            string reportText = File.ReadAllText(reviewReport, Encoding.UTF8);
            if (!reportText.Contains("passed=True") ||
                !reportText.Contains("naturalPlayback=True") ||
                !reportText.Contains("cyclesObserved=2") ||
                !reportText.Contains("targetManipulatedByValidation=False") ||
                !reportText.Contains("electricMineFollowsRightHand=True"))
                throw new InvalidOperationException("Natural locomotion review did not pass.");
            string final = Absolute(FinalImagePath);
            string finalReport = Absolute(FinalReportPath);
            if (File.Exists(final) || File.Exists(finalReport))
                throw new InvalidOperationException("Final contact sheet already exists.");
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.Copy(review, final, false);
            File.WriteAllText(finalReport, new StringBuilder()
                .AppendLine("ElectricMine_Idle locomotion final direct contact sheet")
                .AppendLine(
                    "leftToRight=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("topRow=full body")
                .AppendLine("bottomRow=right arm grip and electric mine close-up")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sourceMotionCurvesChanged=False")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("finalSha256=" + Sha256File(final))
                .ToString(), new UTF8Encoding(false));
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[ElectricMineIdleLocomotion] Final contact sheet captured once.");
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(RequireScene(), TargetName);
        }

        internal static GameObject RequireArmedRuntimeTarget()
        {
            return FindUnique(RequireScene(), ArmedTargetName);
        }

        internal static RuntimeMetrics MeasureArmedRuntime(GameObject target)
        {
            Animator animator = RequireAnimator(target);
            ElectricMineArmedIdleLocomotionCycleBehaviour behaviour =
                animator.GetBehaviour<
                    ElectricMineArmedIdleLocomotionCycleBehaviour>() ??
                throw new InvalidOperationException(
                    "Runtime armed grip behaviour is missing.");
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            Transform spine = RequirePath(target.transform, SpinePath);
            Transform shoulder = RequirePath(target.transform, RightShoulderPath);
            return new RuntimeMetrics(
                behaviour.MaximumGripDeviation(animator),
                prop.localPosition,
                prop.localRotation,
                prop.localScale,
                spine.localRotation,
                shoulder.localRotation,
                prop.parent != null && prop.parent.name == "RightHand");
        }

        internal static void ComposeArmedReview(IReadOnlyList<Texture2D> panels)
        {
            if (panels.Count != 14 || panels.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Armed review requires fourteen panels.");
            int panelWidth = panels[0].width;
            int panelHeight = panels[0].height;
            var output = new Texture2D(
                panelWidth * 7, panelHeight * 2, TextureFormat.RGB24, false);
            try
            {
                output.SetPixels32(Enumerable.Repeat(
                    new Color32(5, 7, 10, 255), output.width * output.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int column = index % 7;
                    int row = index < 7 ? 1 : 0;
                    output.SetPixels32(column * panelWidth, row * panelHeight,
                        panelWidth, panelHeight, panels[index].GetPixels32());
                }
                output.Apply(false, false);
                string absolute = Absolute(ArmedReviewImagePath);
                if (File.Exists(absolute))
                    throw new InvalidOperationException(
                        "The one final armed locomotion contact sheet already exists.");
                Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                    throw new InvalidOperationException(
                        "Armed review folder is unavailable."));
                File.WriteAllBytes(absolute, output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
        }

        internal static void WriteArmedReviewReport(string value)
        {
            string absolute = Absolute(ArmedReviewReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Armed review report folder is unavailable."));
            File.WriteAllText(absolute, value, new UTF8Encoding(false));
        }

        internal static RuntimeMetrics MeasureRuntime(GameObject target)
        {
            Animator animator = RequireAnimator(target);
            ElectricMineIdleLocomotionCycleBehaviour behaviour =
                animator.GetBehaviour<ElectricMineIdleLocomotionCycleBehaviour>() ??
                throw new InvalidOperationException("Runtime grip behaviour is missing.");
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            Transform spine = RequirePath(target.transform, SpinePath);
            Transform shoulder = RequirePath(target.transform, RightShoulderPath);
            return new RuntimeMetrics(
                behaviour.MaximumGripDeviation(animator),
                prop.localPosition,
                prop.localRotation,
                prop.localScale,
                spine.localRotation,
                shoulder.localRotation,
                prop.parent != null && prop.parent.name == "RightHand");
        }

        internal static void ComposeReview(IReadOnlyList<Texture2D> panels)
        {
            if (panels.Count != 12 || panels.Any(panel => panel == null))
                throw new InvalidOperationException("Review requires twelve panels.");
            int panelWidth = panels[0].width;
            int panelHeight = panels[0].height;
            var output = new Texture2D(
                panelWidth * 6, panelHeight * 2, TextureFormat.RGB24, false);
            try
            {
                output.SetPixels32(Enumerable.Repeat(
                    new Color32(5, 7, 10, 255), output.width * output.height).ToArray());
                for (int index = 0; index < panels.Count; index++)
                {
                    int column = index % 6;
                    int row = index < 6 ? 1 : 0;
                    output.SetPixels32(column * panelWidth, row * panelHeight,
                        panelWidth, panelHeight, panels[index].GetPixels32());
                }
                output.Apply(false, false);
                Directory.CreateDirectory(Absolute(TempFolder));
                File.WriteAllBytes(ReviewAbsolutePath, output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
        }

        internal static void WriteReviewReport(string value)
        {
            Directory.CreateDirectory(Absolute(TempFolder));
            File.WriteAllText(Absolute(ReviewReportPath), value, new UTF8Encoding(false));
        }

        internal static Scene RequireScene() => ElectricMineSetupTools.RequireScene();
        internal static GameObject FindUnique(Scene scene, string name) =>
            ElectricMineSetupTools.FindUnique(scene, name);
        internal static string Absolute(string path) => ElectricMineSetupTools.Absolute(path);

        private static SourceMotions RequireSourceMotions(Scene scene)
        {
            Motion[] motions = SourceNames.Select(name =>
            {
                AnimatorController controller = RequireController(
                    RequireAnimator(FindUnique(scene, name)), name);
                return controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new InvalidOperationException(name + " default motion is missing.");
            }).ToArray();
            if (!(motions[0] is AnimationClip idle) ||
                !(motions[1] is AnimationClip forward) ||
                !(motions[2] is AnimationClip backward) ||
                !(motions[3] is AnimationClip sidestep) ||
                !(motions[4] is BlendTree diagonal) ||
                !(motions[5] is AnimationClip run))
                throw new InvalidOperationException("Requested source motion types changed.");
            if (diagonal.blendType != BlendTreeType.Simple1D ||
                diagonal.children.Length != 2)
                throw new InvalidOperationException(
                    "Player_Walk_Diagonal source Blend Tree changed.");
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
                AddMotionAssetPaths(paths,
                    controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new InvalidOperationException(name + " source motion is missing."));
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
            AnimationClip source, string path, string name)
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

        private static AnimationClip RequireSourceClip(Scene scene, string sourceName)
        {
            AnimatorController controller = RequireController(
                RequireAnimator(FindUnique(scene, sourceName)), sourceName);
            return controller.layers[0].stateMachine.defaultState?.motion as AnimationClip ??
                throw new InvalidOperationException(
                    sourceName + " default AnimationClip is missing.");
        }

        private static AnimatorController CreateArmedController(
            BlendTree sourceDiagonal,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip backward,
            AnimationClip sidestep,
            AnimationClip run,
            AnimationClip jump,
            AnimationClip gripClip,
            GripData grip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ArmedControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    ArmedControllerPath);
            AnimatorControllerLayer layer = controller.layers[0];
            foreach (AnimatorControllerLayer extraLayer in controller.layers.Skip(1))
                if (extraLayer.stateMachine != null)
                    UnityEngine.Object.DestroyImmediate(extraLayer.stateMachine, true);
            controller.layers = new[] { layer };
            AnimatorStateMachine machine = layer.stateMachine;
            foreach (AnimatorState state in machine.states.Select(item => item.state).ToArray())
                machine.RemoveState(state);
            foreach (AnimatorStateMachine child in machine.stateMachines
                         .Select(item => item.stateMachine).ToArray())
                machine.RemoveStateMachine(child);
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(ArmedControllerPath)
                         .OfType<BlendTree>().ToArray())
                UnityEngine.Object.DestroyImmediate(tree, true);

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(
                ElectricMineArmedIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                ElectricMineArmedIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = ElectricMineArmedIdleLocomotionCycleBehaviour
                    .DiagonalBlendParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f
            });
            layer.name = "Exact Source Locomotion With Armed Grip";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            layer.iKPass = false;

            ChildMotion[] diagonalChildren = sourceDiagonal.children;
            var clipMap = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)diagonalChildren[0].motion] = forward,
                [(AnimationClip)diagonalChildren[1].motion] = sidestep
            };
            BlendTree diagonal = CloneTree(
                sourceDiagonal, ArmedDiagonalTreeName, controller, clipMap);
            var root = new BlendTree
            {
                name = ArmedRootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter =
                    ElectricMineArmedIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY =
                    ElectricMineArmedIdleLocomotionCycleBehaviour.MoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(root, controller);
            root.children = new[]
            {
                RootChild(idle, ArmedRequiredPositions[0]),
                RootChild(forward, ArmedRequiredPositions[1]),
                RootChild(backward, ArmedRequiredPositions[2]),
                RootChild(sidestep, ArmedRequiredPositions[3]),
                RootChild(diagonal, ArmedRequiredPositions[4]),
                RootChild(run, ArmedRequiredPositions[5]),
                RootChild(jump, ArmedRequiredPositions[6])
            };
            AnimatorState locomotion = machine.AddState(ArmedStateName);
            locomotion.motion = root;
            locomotion.speed = 1f;
            locomotion.cycleOffset = 0f;
            locomotion.mirror = false;
            locomotion.writeDefaultValues = false;
            ElectricMineArmedIdleLocomotionCycleBehaviour behaviour = locomotion
                .AddStateMachineBehaviour<
                    ElectricMineArmedIdleLocomotionCycleBehaviour>();
            behaviour.ConfigureGrip(grip.Paths, grip.Rotations);
            machine.defaultState = locomotion;
            controller.layers = new[] { layer };

            controller.AddLayer("Current Armed Grip Override");
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer gripLayer = layers[1];
            gripLayer.name = "Current Armed Grip Override";
            gripLayer.defaultWeight = 1f;
            gripLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            gripLayer.avatarMask = null;
            gripLayer.iKPass = false;
            AnimatorState gripState = gripLayer.stateMachine.AddState(
                "CurrentArmedGripPose");
            gripState.motion = gripClip;
            gripState.speed = 1f;
            gripState.cycleOffset = 0f;
            gripState.mirror = false;
            gripState.writeDefaultValues = false;
            gripLayer.stateMachine.defaultState = gripState;
            layers[1] = gripLayer;
            controller.layers = layers;
            EditorUtility.SetDirty(behaviour);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(gripLayer.stateMachine);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(diagonal);
            return controller;
        }

        private static AnimationClip CreateArmedGripClip(GripData grip)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(ArmedGripClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "ElectricMineArmedIdle_CurrentGrip",
                    frameRate = 60f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, ArmedGripClipPath);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            for (int index = 0; index < grip.Paths.Length; index++)
            {
                Quaternion rotation = grip.Rotations[index];
                SetConstantRotationCurve(
                    clip, grip.Paths[index], "m_LocalRotation.x", rotation.x);
                SetConstantRotationCurve(
                    clip, grip.Paths[index], "m_LocalRotation.y", rotation.y);
                SetConstantRotationCurve(
                    clip, grip.Paths[index], "m_LocalRotation.z", rotation.z);
                SetConstantRotationCurve(
                    clip, grip.Paths[index], "m_LocalRotation.w", rotation.w);
            }
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            RequireGripClipMatches(grip, clip);
            return clip;
        }

        private static AnimatorController CreateController(
            BlendTree sourceDiagonal,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip backward,
            AnimationClip sidestep,
            AnimationClip run,
            AnimationClip gripClip,
            GripData grip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            AnimatorControllerLayer layer = controller.layers[0];
            foreach (AnimatorControllerLayer extraLayer in controller.layers.Skip(1))
                if (extraLayer.stateMachine != null)
                    UnityEngine.Object.DestroyImmediate(extraLayer.stateMachine, true);
            controller.layers = new[] { layer };
            AnimatorStateMachine machine = layer.stateMachine;
            foreach (AnimatorState state in machine.states.Select(item => item.state).ToArray())
                machine.RemoveState(state);
            foreach (AnimatorStateMachine child in machine.stateMachines
                         .Select(item => item.stateMachine).ToArray())
                machine.RemoveStateMachine(child);
            foreach (BlendTree tree in AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                         .OfType<BlendTree>().ToArray())
                UnityEngine.Object.DestroyImmediate(tree, true);

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(
                ElectricMineIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                ElectricMineIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = ElectricMineIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f
            });
            layer.name = "Source Locomotion With Preserved Grip";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            layer.iKPass = false;

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
                blendParameter = ElectricMineIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY = ElectricMineIdleLocomotionCycleBehaviour.MoveYParameter,
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
            AnimatorState locomotion = machine.AddState(StateName);
            locomotion.motion = root;
            locomotion.speed = 1f;
            locomotion.cycleOffset = 0f;
            locomotion.mirror = false;
            locomotion.writeDefaultValues = false;
            ElectricMineIdleLocomotionCycleBehaviour behaviour = locomotion
                .AddStateMachineBehaviour<ElectricMineIdleLocomotionCycleBehaviour>();
            behaviour.ConfigureGrip(grip.Paths, grip.Rotations);
            machine.defaultState = locomotion;
            controller.layers = new[] { layer };

            controller.AddLayer("Current Grip Override");
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer gripLayer = layers[1];
            gripLayer.name = "Current Grip Override";
            gripLayer.defaultWeight = 1f;
            gripLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            gripLayer.avatarMask = null;
            gripLayer.iKPass = false;
            AnimatorState gripState = gripLayer.stateMachine.AddState("CurrentGripPose");
            gripState.motion = gripClip;
            gripState.speed = 1f;
            gripState.cycleOffset = 0f;
            gripState.mirror = false;
            gripState.writeDefaultValues = false;
            gripLayer.stateMachine.defaultState = gripState;
            layers[1] = gripLayer;
            controller.layers = layers;
            EditorUtility.SetDirty(behaviour);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(gripLayer.stateMachine);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(diagonal);
            return controller;
        }

        private static AnimationClip CreateGripClip(GripData grip)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GripClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "ElectricMineIdle_CurrentGrip",
                    frameRate = 60f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, GripClipPath);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            for (int index = 0; index < grip.Paths.Length; index++)
            {
                Quaternion rotation = grip.Rotations[index];
                SetConstantRotationCurve(clip, grip.Paths[index], "m_LocalRotation.x", rotation.x);
                SetConstantRotationCurve(clip, grip.Paths[index], "m_LocalRotation.y", rotation.y);
                SetConstantRotationCurve(clip, grip.Paths[index], "m_LocalRotation.z", rotation.z);
                SetConstantRotationCurve(clip, grip.Paths[index], "m_LocalRotation.w", rotation.w);
            }
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            RequireGripClipMatches(grip, clip);
            return clip;
        }

        private static void SetConstantRotationCurve(
            AnimationClip clip, string path, string property, float value)
        {
            AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
            {
                path = path,
                type = typeof(Transform),
                propertyName = property
            }, new AnimationCurve(
                new Keyframe(0f, value), new Keyframe(1f, value)));
        }

        private static void RequireGripClipMatches(GripData grip, AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != grip.Paths.Length * 4)
                throw new InvalidOperationException("Grip override curve count differs.");
            string[] properties =
            {
                "m_LocalRotation.x", "m_LocalRotation.y",
                "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (int index = 0; index < grip.Paths.Length; index++)
            {
                Quaternion rotation = grip.Rotations[index];
                float[] values = { rotation.x, rotation.y, rotation.z, rotation.w };
                for (int component = 0; component < properties.Length; component++)
                {
                    EditorCurveBinding binding = bindings.SingleOrDefault(item =>
                        item.path == grip.Paths[index] &&
                        item.type == typeof(Transform) &&
                        item.propertyName == properties[component]);
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.keys.Length != 2 ||
                        Mathf.Abs(curve.keys[0].time) > 0.00001f ||
                        Mathf.Abs(curve.keys[1].time - 1f) > 0.00001f ||
                        Mathf.Abs(curve.keys[0].value - values[component]) > 0.00001f ||
                        Mathf.Abs(curve.keys[1].value - values[component]) > 0.00001f)
                        throw new InvalidOperationException(
                            "Grip override curve differs: " + grip.Paths[index] + " " +
                            properties[component]);
                }
            }
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
            ChildMotion[] children = source.children;
            var copied = new ChildMotion[children.Length];
            for (int index = 0; index < children.Length; index++)
            {
                ChildMotion child = children[index];
                Motion motion;
                if (child.motion is AnimationClip clip)
                {
                    if (!clipMap.TryGetValue(clip, out AnimationClip mapped))
                        throw new InvalidOperationException(
                            "Diagonal source clip mapping is missing: " + clip.name);
                    motion = mapped;
                }
                else if (child.motion is BlendTree tree)
                    motion = CloneTree(tree, name + "_" + index, owner, clipMap);
                else
                    throw new InvalidOperationException("Unsupported diagonal child motion.");
                copied[index] = new ChildMotion
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
            copy.children = copied;
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

        private static GripData GripPose(GameObject target)
        {
            Transform arm = RequirePath(target.transform, RightArmPath);
            Transform[] bones = arm.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name != PropName &&
                    !HasAncestorNamed(item, arm, PropName))
                .OrderBy(item => TransformPath(item, target.transform),
                    StringComparer.Ordinal)
                .ToArray();
            return new GripData(
                bones.Select(item => TransformPath(item, target.transform)).ToArray(),
                bones.Select(item => item.localRotation).ToArray());
        }

        private static bool HasAncestorNamed(Transform item, Transform root, string name)
        {
            for (Transform current = item.parent;
                 current != null && current != root.parent;
                 current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static void RequireRootTree(BlendTree root)
        {
            if (root.name != RootTreeName ||
                root.blendType != BlendTreeType.FreeformCartesian2D ||
                root.blendParameter !=
                    ElectricMineIdleLocomotionCycleBehaviour.MoveXParameter ||
                root.blendParameterY !=
                    ElectricMineIdleLocomotionCycleBehaviour.MoveYParameter ||
                root.children.Length != 6)
                throw new InvalidOperationException("Root 2D Blend Tree differs.");
            for (int index = 0; index < RequiredPositions.Length; index++)
                if (Vector2.Distance(root.children[index].position,
                        RequiredPositions[index]) > 0.00001f)
                    throw new InvalidOperationException(
                        "Root Blend Tree position differs at " + index + ".");
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
            ChildMotion[] a = source.children;
            ChildMotion[] b = copy.children;
            for (int index = 0; index < a.Length; index++)
            {
                if (!Mathf.Approximately(a[index].threshold, b[index].threshold) ||
                    a[index].position != b[index].position ||
                    !Mathf.Approximately(a[index].timeScale, b[index].timeScale) ||
                    !Mathf.Approximately(a[index].cycleOffset, b[index].cycleOffset) ||
                    a[index].mirror != b[index].mirror ||
                    a[index].directBlendParameter != b[index].directBlendParameter)
                    throw new InvalidOperationException(label + " child differs at " + index);
                if (a[index].motion is AnimationClip sourceClip)
                {
                    if (!(b[index].motion is AnimationClip copiedClip) ||
                        !clipMap.TryGetValue(sourceClip, out AnimationClip expected) ||
                        copiedClip != expected)
                        throw new InvalidOperationException(
                            label + " clip differs at " + index);
                    RequireCopiedClip(sourceClip, copiedClip, label + " child " + index);
                }
                else if (a[index].motion is BlendTree sourceTree)
                    RequireMotionTreeEquivalent(
                        sourceTree, b[index].motion as BlendTree, clipMap,
                        label + " child " + index);
                else
                    throw new InvalidOperationException(label + " contains unsupported motion.");
            }
        }

        private static void RequireCopiedClip(
            AnimationClip source, AnimationClip copy, string label)
        {
            RequireEqual(ClipSignature(source), ClipSignature(copy),
                label + " clip content");
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var text = new StringBuilder()
                .AppendLine("length=" + F(clip.length))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                         .OrderBy(item => item.path, StringComparer.Ordinal)
                         .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                         .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                text.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    text.AppendLine(string.Join(",", new[]
                    {
                        F(key.time), F(key.value), F(key.inTangent), F(key.outTangent),
                        F(key.inWeight), F(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                text.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" + item.messageOptions);
            return Sha256Text(text.ToString());
        }

        private static string TargetProtectedSignature(GameObject target)
        {
            var text = new StringBuilder();
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => TransformPath(item, target.transform),
                             StringComparer.Ordinal))
                text.Append(TransformPath(item, target.transform)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(item.gameObject.activeSelf).AppendLine();
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true)
                         .OrderBy(item => TransformPath(item.transform, target.transform),
                             StringComparer.Ordinal))
            {
                text.Append("renderer|")
                    .Append(TransformPath(renderer.transform, target.transform));
                if (renderer is SkinnedMeshRenderer skinned)
                    text.Append("|mesh=").Append(AssetIdentity(skinned.sharedMesh));
                foreach (Material material in renderer.sharedMaterials)
                    text.Append("|material=").Append(AssetIdentity(material));
                text.AppendLine();
            }
            return Sha256Text(text.ToString());
        }

        private static string MineSignature(GameObject target)
        {
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            var text = new StringBuilder();
            foreach (Transform item in prop.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => TransformPath(item, prop), StringComparer.Ordinal))
                text.Append(TransformPath(item, prop)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).AppendLine();
            return Sha256Text(text.ToString());
        }

        private static string SceneSignatureOutsideTarget(Scene scene)
        {
            var text = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects().OrderBy(item => item.name))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => !HasNamedAncestor(item, TargetName))
                         .OrderBy(item => TransformPath(item, root.transform),
                             StringComparer.Ordinal))
                text.Append(root.name).Append('|')
                    .Append(TransformPath(item, root.transform)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(item.gameObject.activeSelf).AppendLine();
            return Sha256Text(text.ToString());
        }

        private static bool HasNamedAncestor(Transform item, string name)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static string TransformState(Transform item) =>
            Vec(item.localPosition) + "|" + Quat(item.localRotation) + "|" +
            Vec(item.localScale);

        private static string TransformPath(Transform item, Transform root) =>
            AnimationUtility.CalculateTransformPath(item, root);

        private static string AssetIdentity(UnityEngine.Object asset)
        {
            if (asset == null) return "<null>";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    asset, out string guid, out long localId))
                return guid + ":" + localId;
            return asset.name;
        }

        private static Animator RequireAnimator(GameObject target) =>
            target.GetComponent<Animator>() ??
            throw new InvalidOperationException(target.name + " Animator is missing.");

        private static AnimatorController RequireController(Animator animator, string label) =>
            animator.runtimeAnimatorController as AnimatorController ??
            throw new InvalidOperationException(label + " AnimatorController is missing.");

        private static Transform RequirePath(Transform root, string path) =>
            root.Find(path) ??
            throw new InvalidOperationException(root.name + " path is missing: " + path);

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException("Asset is missing: " + path);

        private static void RequireFloatParameter(
            AnimatorController controller, string name, float expected)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null ||
                parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expected))
                throw new InvalidOperationException("Float parameter differs: " + name);
        }

        private static StringBuilder DescribeMotion(Motion motion, string key)
        {
            if (motion == null)
                throw new InvalidOperationException(key + " is missing.");
            var text = new StringBuilder()
                .AppendLine(key + "Type=" + motion.GetType().Name)
                .AppendLine(key + "Name=" + motion.name)
                .AppendLine(key + "Path=" + AssetDatabase.GetAssetPath(motion));
            if (motion is AnimationClip clip)
                text.AppendLine(key + "Length=" + F(clip.length))
                    .AppendLine(key + "FrameRate=" + F(clip.frameRate))
                    .AppendLine(key + "FloatCurveCount=" +
                        AnimationUtility.GetCurveBindings(clip).Length);
            else if (motion is BlendTree tree)
                text.AppendLine(key + "BlendType=" + tree.blendType)
                    .AppendLine(key + "ChildCount=" + tree.children.Length);
            return text;
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void Write(string name, string value)
        {
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Absolute(OutputFolder + "/" + name), value, new UTF8Encoding(false));
        }

        private static string Read(string name) =>
            File.ReadAllText(Absolute(OutputFolder + "/" + name), Encoding.UTF8);

        private static void WriteLines(string name, IEnumerable<string> values) =>
            Write(name, string.Join(Environment.NewLine, values) + Environment.NewLine);

        private static void RequireBaselineHashes(string name)
        {
            foreach (string line in Read(name).Split(
                         new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.LastIndexOf('|');
                if (separator <= 0)
                    throw new InvalidOperationException("Invalid source hash baseline.");
                string path = line.Substring(0, separator);
                string expected = line.Substring(separator + 1);
                RequireEqual(expected, AssetHash(path), "source asset " + path);
            }
        }

        private static void RequireHashes(IReadOnlyDictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, AssetHash(item.Key), "source asset " + item.Key);
        }

        private static string AssetHash(string path) => Sha256File(Absolute(path));

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " differs.");
        }

        private static void RequireNear(Vector3 actual, Vector3 expected, string label)
        {
            if (Vector3.Distance(actual, expected) > PositionTolerance)
                throw new InvalidOperationException(label + " differs.");
        }

        private static void RequireNear(Quaternion actual, Quaternion expected, string label)
        {
            if (Quaternion.Angle(actual, expected) > RotationTolerance)
                throw new InvalidOperationException(label + " differs.");
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This command requires Edit Mode.");
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z);

        private static string Quat(Quaternion value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);

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

        private readonly struct GripData
        {
            internal GripData(string[] paths, Quaternion[] rotations)
            {
                Paths = paths;
                Rotations = rotations;
            }

            internal string[] Paths { get; }
            internal Quaternion[] Rotations { get; }
        }

        internal readonly struct RuntimeMetrics
        {
            internal RuntimeMetrics(
                float gripDeviation,
                Vector3 propLocalPosition,
                Quaternion propLocalRotation,
                Vector3 propLocalScale,
                Quaternion spineRotation,
                Quaternion shoulderRotation,
                bool propParentIsRightHand)
            {
                GripDeviation = gripDeviation;
                PropLocalPosition = propLocalPosition;
                PropLocalRotation = propLocalRotation;
                PropLocalScale = propLocalScale;
                SpineRotation = spineRotation;
                ShoulderRotation = shoulderRotation;
                PropParentIsRightHand = propParentIsRightHand;
            }

            internal float GripDeviation { get; }
            internal Vector3 PropLocalPosition { get; }
            internal Quaternion PropLocalRotation { get; }
            internal Vector3 PropLocalScale { get; }
            internal Quaternion SpineRotation { get; }
            internal Quaternion ShoulderRotation { get; }
            internal bool PropParentIsRightHand { get; }
        }
    }

    [InitializeOnLoad]
    internal static class ElectricMineArmedIdleLocomotionPlayModeReview
    {
        private const string PendingKey =
            "Bellerophon.ElectricMineArmedIdleLocomotionReview.Pending";
        private const string RequestIdKey =
            "Bellerophon.ElectricMineArmedIdleLocomotionReview.RequestId";
        private const string LogPathKey =
            "Bellerophon.ElectricMineArmedIdleLocomotionReview.LogPath";
        private const string StateKey =
            "Bellerophon.ElectricMineArmedIdleLocomotionReview.State";
        private const string FailureKey =
            "Bellerophon.ElectricMineArmedIdleLocomotionReview.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const float CapturePhaseTime = 0.48f;
        private const double TimeoutSeconds = 45d;

        private static bool initialized;
        private static double startedAt;
        private static int baseAbsolutePhase = -1;
        private static int nextCapture;
        private static GameObject target;
        private static Animator animator;
        private static Texture2D[] panels;
        private static Vector3 rootPosition;
        private static Quaternion rootRotation;
        private static Vector3 rootScale;
        private static Vector3 propPosition;
        private static Quaternion propRotation;
        private static Vector3 propScale;
        private static float maximumGripDeviation;
        private static float maximumPropPositionError;
        private static float maximumPropRotationError;
        private static float maximumPropScaleError;
        private static readonly List<double> CaptureTimes = new List<double>();
        private static readonly List<string> Observations = new List<string>();

        static ElectricMineArmedIdleLocomotionPlayModeReview()
        {
            if (SessionState.GetBool(PendingKey, false)) Subscribe();
        }

        internal static void Start(string requestId, string logPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Armed locomotion review must start in Edit Mode.");
            if (SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException(
                    "An armed locomotion review is already pending.");
            string finalImage = ElectricMineIdleLocomotionTools.Absolute(
                ElectricMineIdleLocomotionTools.ArmedReviewImagePath);
            if (File.Exists(finalImage))
                throw new InvalidOperationException(
                    "The one final armed locomotion contact sheet already exists.");
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
                            "Play Mode ended before armed review completed.");
                    Observe();
                    return;
                }
                if (state == WaitingForEditMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteInEditMode();
                    return;
                }
                throw new InvalidOperationException("Unknown armed review state.");
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void InitializeRuntime()
        {
            initialized = true;
            startedAt = EditorApplication.timeSinceStartup;
            baseAbsolutePhase = -1;
            nextCapture = 0;
            target = ElectricMineIdleLocomotionTools.RequireArmedRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException("Runtime Animator is missing.");
            panels = new Texture2D[14];
            rootPosition = target.transform.position;
            rootRotation = target.transform.rotation;
            rootScale = target.transform.localScale;
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            propPosition = prop.localPosition;
            propRotation = prop.localRotation;
            propScale = prop.localScale;
            maximumGripDeviation = 0f;
            maximumPropPositionError = 0f;
            maximumPropRotationError = 0f;
            maximumPropScaleError = 0f;
            CaptureTimes.Clear();
            Observations.Clear();
        }

        private static void Observe()
        {
            if (!initialized) InitializeRuntime();
            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
                throw new TimeoutException(
                    "Natural armed locomotion review exceeded 45 seconds.");
            if (!animator.isInitialized ||
                !ElectricMineArmedIdleLocomotionCycleBehaviour.TryGetSequenceState(
                    animator, out int absolutePhase, out int phase,
                    out float phaseElapsed))
                return;
            if (baseAbsolutePhase < 0)
            {
                if (phase != 0 || phaseElapsed > 0.65f) return;
                baseAbsolutePhase = absolutePhase;
            }

            ElectricMineIdleLocomotionTools.RuntimeMetrics metrics =
                ElectricMineIdleLocomotionTools.MeasureArmedRuntime(target);
            maximumGripDeviation = Mathf.Max(
                maximumGripDeviation, metrics.GripDeviation);
            maximumPropPositionError = Mathf.Max(maximumPropPositionError,
                Vector3.Distance(metrics.PropLocalPosition, propPosition));
            maximumPropRotationError = Mathf.Max(maximumPropRotationError,
                Quaternion.Angle(metrics.PropLocalRotation, propRotation));
            maximumPropScaleError = Mathf.Max(maximumPropScaleError,
                Vector3.Distance(metrics.PropLocalScale, propScale));
            if (!metrics.PropParentIsRightHand)
                throw new InvalidOperationException(
                    "Armed electric mine stopped following RightHand.");
            if (Vector3.Distance(target.transform.position, rootPosition) > 0.0001f ||
                Quaternion.Angle(target.transform.rotation, rootRotation) > 0.02f ||
                Vector3.Distance(target.transform.localScale, rootScale) > 0.00001f)
                throw new InvalidOperationException(
                    "Armed target root moved during playback.");

            if (nextCapture < 7)
            {
                int expectedAbsolute = baseAbsolutePhase + nextCapture;
                if (absolutePhase < expectedAbsolute || phaseElapsed < CapturePhaseTime)
                    return;
                if (absolutePhase > expectedAbsolute || phase != nextCapture)
                    throw new InvalidOperationException(
                        "An armed locomotion phase was missed.");
                Vector2 expected =
                    ElectricMineArmedIdleLocomotionCycleBehaviour.MotionPosition(phase);
                float moveX = animator.GetFloat(
                    ElectricMineArmedIdleLocomotionCycleBehaviour.MoveXParameter);
                float moveY = animator.GetFloat(
                    ElectricMineArmedIdleLocomotionCycleBehaviour.MoveYParameter);
                if (Mathf.Abs(moveX - expected.x) > 0.001f ||
                    Mathf.Abs(moveY - expected.y) > 0.001f)
                    throw new InvalidOperationException(
                        "Armed Blend Tree parameters differ.");
                panels[nextCapture] = ElectricMineSetupTools.CaptureTargetPanel(
                    target, false);
                panels[nextCapture + 7] = ElectricMineSetupTools.CaptureTargetPanel(
                    target, true);
                CaptureTimes.Add(EditorApplication.timeSinceStartup);
                Observations.Add("phase=" + phase + "|motion=" +
                    ElectricMineArmedIdleLocomotionCycleBehaviour.MotionName(phase) +
                    "|phaseElapsed=" + F(phaseElapsed) + "|moveX=" + F(moveX) +
                    "|moveY=" + F(moveY));
                nextCapture++;
                return;
            }

            if (absolutePhase < baseAbsolutePhase + 14) return;
            if (absolutePhase != baseAbsolutePhase + 14 || phase != 0)
                throw new InvalidOperationException(
                    "Jump did not return naturally to Idle after two cycles.");
            FinishPlayMode();
        }

        private static void FinishPlayMode()
        {
            if (panels == null || panels.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Armed review panels are incomplete.");
            float minimumInterval = float.MaxValue;
            float maximumInterval = 0f;
            for (int index = 1; index < CaptureTimes.Count; index++)
            {
                float interval = (float)(CaptureTimes[index] - CaptureTimes[index - 1]);
                minimumInterval = Mathf.Min(minimumInterval, interval);
                maximumInterval = Mathf.Max(maximumInterval, interval);
            }
            if (minimumInterval < 0.75f || maximumInterval > 1.25f)
                throw new InvalidOperationException(
                    "Natural armed one-second phase timing differs.");
            if (maximumGripDeviation > 0.1f)
                throw new InvalidOperationException("Armed right-hand grip drifted.");
            if (maximumPropPositionError > 0.00001f ||
                maximumPropRotationError > 0.02f ||
                maximumPropScaleError > 0.00001f)
                throw new InvalidOperationException(
                    "Armed electric mine local state changed.");

            ElectricMineIdleLocomotionTools.ComposeArmedReview(panels);
            var report = new StringBuilder()
                .AppendLine("ElectricMine_Armed_Idle natural 2D locomotion review")
                .AppendLine("passed=True")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=2")
                .AppendLine(
                    "sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward,Jump")
                .AppendLine("returnedToIdleAfterJump=True")
                .AppendLine("minimumObservedCaptureInterval=" + F(minimumInterval))
                .AppendLine("maximumObservedCaptureInterval=" + F(maximumInterval))
                .AppendLine("maximumGripDeviationDegrees=" +
                    F(maximumGripDeviation))
                .AppendLine("electricMineFollowsRightHand=True")
                .AppendLine("maximumMineLocalPositionError=" +
                    F(maximumPropPositionError))
                .AppendLine("maximumMineLocalRotationErrorDegrees=" +
                    F(maximumPropRotationError))
                .AppendLine("maximumMineLocalScaleError=" +
                    F(maximumPropScaleError));
            foreach (string observation in Observations)
                report.AppendLine(observation);
            ElectricMineIdleLocomotionTools.WriteArmedReviewReport(
                report.ToString());
            CleanupRuntime();
            SessionState.SetInt(StateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void CompleteInEditMode()
        {
            string requestId = SessionState.GetString(RequestIdKey, string.Empty);
            string logPath = SessionState.GetString(LogPathKey, string.Empty);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            if (string.IsNullOrEmpty(failure))
            {
                ElectricMineIdleLocomotionTools.InspectArmedStructure();
                DetectorAttachedStaticStartSetupTools.InspectHelmEnterStartView();
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=passed" + Environment.NewLine +
                    "ElectricMine_Armed_Idle natural two-cycle review completed.");
            }
            else
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=failed" + Environment.NewLine + failure);
            ClearSession();
        }

        private static void Fail(Exception exception)
        {
            CleanupRuntime();
            SessionState.SetString(FailureKey, exception.ToString());
            SessionState.SetInt(StateKey, WaitingForEditMode);
            Debug.LogWarning(
                "ElectricMine_Armed_Idle review failed: " + exception.Message);
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
            CaptureTimes.Clear();
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

        private static void WriteBridgeLog(string path, string value)
        {
            string absolute = ElectricMineIdleLocomotionTools.Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Armed review bridge log folder is unavailable."));
            File.WriteAllText(absolute, value, new UTF8Encoding(false));
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }

    [InitializeOnLoad]
    internal static class ElectricMineIdleLocomotionPlayModeReview
    {
        private const string PendingKey =
            "Bellerophon.ElectricMineIdleLocomotionReview.Pending";
        private const string RequestIdKey =
            "Bellerophon.ElectricMineIdleLocomotionReview.RequestId";
        private const string LogPathKey =
            "Bellerophon.ElectricMineIdleLocomotionReview.LogPath";
        private const string StateKey =
            "Bellerophon.ElectricMineIdleLocomotionReview.State";
        private const string FailureKey =
            "Bellerophon.ElectricMineIdleLocomotionReview.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const float CapturePhaseTime = 0.48f;
        private const double TimeoutSeconds = 40d;

        private static bool initialized;
        private static double startedAt;
        private static int baseAbsolutePhase = -1;
        private static int nextCapture;
        private static GameObject target;
        private static Animator animator;
        private static Texture2D[] panels;
        private static Vector3 rootPosition;
        private static Quaternion rootRotation;
        private static Vector3 rootScale;
        private static Vector3 propPosition;
        private static Quaternion propRotation;
        private static Vector3 propScale;
        private static Quaternion initialSpine;
        private static Quaternion initialShoulder;
        private static float maximumGripDeviation;
        private static float maximumPropPositionError;
        private static float maximumPropRotationError;
        private static float maximumPropScaleError;
        private static float maximumSpineSway;
        private static float maximumShoulderSway;
        private static readonly List<double> CaptureTimes = new List<double>();
        private static readonly List<string> Observations = new List<string>();

        static ElectricMineIdleLocomotionPlayModeReview()
        {
            if (SessionState.GetBool(PendingKey, false)) Subscribe();
        }

        internal static void Start(string requestId, string logPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Review must start in Edit Mode.");
            if (SessionState.GetBool(PendingKey, false))
                throw new InvalidOperationException("A review is already pending.");
            Directory.CreateDirectory(ElectricMineIdleLocomotionTools.Absolute(
                ElectricMineIdleLocomotionTools.TempFolder));
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
                            "Play Mode ended before review completed.");
                    Observe();
                    return;
                }
                if (state == WaitingForEditMode)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CompleteInEditMode();
                    return;
                }
                throw new InvalidOperationException("Unknown review state.");
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void InitializeRuntime()
        {
            initialized = true;
            startedAt = EditorApplication.timeSinceStartup;
            baseAbsolutePhase = -1;
            nextCapture = 0;
            target = ElectricMineIdleLocomotionTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException("Runtime Animator is missing.");
            panels = new Texture2D[12];
            rootPosition = target.transform.position;
            rootRotation = target.transform.rotation;
            rootScale = target.transform.localScale;
            Transform prop = ElectricMineSetupTools.RequireRuntimeProp(target);
            propPosition = prop.localPosition;
            propRotation = prop.localRotation;
            propScale = prop.localScale;
            ElectricMineIdleLocomotionTools.RuntimeMetrics metrics =
                ElectricMineIdleLocomotionTools.MeasureRuntime(target);
            initialSpine = metrics.SpineRotation;
            initialShoulder = metrics.ShoulderRotation;
            maximumGripDeviation = 0f;
            maximumPropPositionError = 0f;
            maximumPropRotationError = 0f;
            maximumPropScaleError = 0f;
            maximumSpineSway = 0f;
            maximumShoulderSway = 0f;
            CaptureTimes.Clear();
            Observations.Clear();
        }

        private static void Observe()
        {
            if (!initialized) InitializeRuntime();
            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
                throw new TimeoutException("Natural review exceeded 40 seconds.");
            if (!animator.isInitialized ||
                !ElectricMineIdleLocomotionCycleBehaviour.TryGetSequenceState(
                    animator, out int absolutePhase, out int phase,
                    out float phaseElapsed))
                return;
            if (baseAbsolutePhase < 0)
            {
                if (phase != 0 || phaseElapsed > 0.65f) return;
                baseAbsolutePhase = absolutePhase;
            }

            ElectricMineIdleLocomotionTools.RuntimeMetrics metrics =
                ElectricMineIdleLocomotionTools.MeasureRuntime(target);
            maximumGripDeviation = Mathf.Max(
                maximumGripDeviation, metrics.GripDeviation);
            maximumPropPositionError = Mathf.Max(maximumPropPositionError,
                Vector3.Distance(metrics.PropLocalPosition, propPosition));
            maximumPropRotationError = Mathf.Max(maximumPropRotationError,
                Quaternion.Angle(metrics.PropLocalRotation, propRotation));
            maximumPropScaleError = Mathf.Max(maximumPropScaleError,
                Vector3.Distance(metrics.PropLocalScale, propScale));
            maximumSpineSway = Mathf.Max(maximumSpineSway,
                Quaternion.Angle(metrics.SpineRotation, initialSpine));
            maximumShoulderSway = Mathf.Max(maximumShoulderSway,
                Quaternion.Angle(metrics.ShoulderRotation, initialShoulder));
            if (!metrics.PropParentIsRightHand)
                throw new InvalidOperationException(
                    "Electric mine stopped following the RightHand hierarchy.");
            if (Vector3.Distance(target.transform.position, rootPosition) > 0.0001f ||
                Quaternion.Angle(target.transform.rotation, rootRotation) > 0.02f ||
                Vector3.Distance(target.transform.localScale, rootScale) > 0.00001f)
                throw new InvalidOperationException("Target root moved during playback.");

            if (nextCapture < 6)
            {
                int expectedAbsolute = baseAbsolutePhase + nextCapture;
                if (absolutePhase < expectedAbsolute || phaseElapsed < CapturePhaseTime)
                    return;
                if (absolutePhase > expectedAbsolute || phase != nextCapture)
                    throw new InvalidOperationException("A locomotion phase was missed.");
                Vector2 expected =
                    ElectricMineIdleLocomotionCycleBehaviour.MotionPosition(phase);
                float moveX = animator.GetFloat(
                    ElectricMineIdleLocomotionCycleBehaviour.MoveXParameter);
                float moveY = animator.GetFloat(
                    ElectricMineIdleLocomotionCycleBehaviour.MoveYParameter);
                if (Mathf.Abs(moveX - expected.x) > 0.001f ||
                    Mathf.Abs(moveY - expected.y) > 0.001f)
                    throw new InvalidOperationException("Blend Tree parameters differ.");
                panels[nextCapture] = ElectricMineSetupTools.CaptureTargetPanel(
                    target, false);
                panels[nextCapture + 6] = ElectricMineSetupTools.CaptureTargetPanel(
                    target, true);
                CaptureTimes.Add(EditorApplication.timeSinceStartup);
                Observations.Add("phase=" + phase + "|motion=" +
                    ElectricMineIdleLocomotionCycleBehaviour.MotionName(phase) +
                    "|phaseElapsed=" + F(phaseElapsed) + "|moveX=" + F(moveX) +
                    "|moveY=" + F(moveY));
                nextCapture++;
                return;
            }

            if (absolutePhase < baseAbsolutePhase + 12) return;
            if (absolutePhase != baseAbsolutePhase + 12 || phase != 0)
                throw new InvalidOperationException(
                    "RunForward did not return naturally to Idle after two cycles.");
            FinishPlayMode();
        }

        private static void FinishPlayMode()
        {
            if (panels == null || panels.Any(panel => panel == null))
                throw new InvalidOperationException("Review panels are incomplete.");
            float minimumInterval = float.MaxValue;
            float maximumInterval = 0f;
            for (int index = 1; index < CaptureTimes.Count; index++)
            {
                float interval = (float)(CaptureTimes[index] - CaptureTimes[index - 1]);
                minimumInterval = Mathf.Min(minimumInterval, interval);
                maximumInterval = Mathf.Max(maximumInterval, interval);
            }
            if (minimumInterval < 0.75f || maximumInterval > 1.25f)
                throw new InvalidOperationException(
                    "Natural one-second phase timing differs.");
            if (maximumGripDeviation > 0.1f)
                throw new InvalidOperationException("Right-hand grip pose drifted.");
            if (maximumPropPositionError > 0.00001f ||
                maximumPropRotationError > 0.02f ||
                maximumPropScaleError > 0.00001f)
                throw new InvalidOperationException("Electric mine local state changed.");
            if (maximumSpineSway <= 0.1f && maximumShoulderSway <= 0.1f)
                throw new InvalidOperationException("Natural upper-body sway was not observed.");

            ElectricMineIdleLocomotionTools.ComposeReview(panels);
            var report = new StringBuilder()
                .AppendLine("ElectricMine_Idle natural 2D locomotion review")
                .AppendLine("passed=True")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=2")
                .AppendLine(
                    "sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("returnedToIdleAfterRun=True")
                .AppendLine("minimumObservedCaptureInterval=" + F(minimumInterval))
                .AppendLine("maximumObservedCaptureInterval=" + F(maximumInterval))
                .AppendLine("maximumGripDeviationDegrees=" + F(maximumGripDeviation))
                .AppendLine("maximumSpineSwayDegrees=" + F(maximumSpineSway))
                .AppendLine("maximumRightShoulderSwayDegrees=" + F(maximumShoulderSway))
                .AppendLine("electricMineFollowsRightHand=True")
                .AppendLine("maximumMineLocalPositionError=" +
                    F(maximumPropPositionError))
                .AppendLine("maximumMineLocalRotationErrorDegrees=" +
                    F(maximumPropRotationError))
                .AppendLine("maximumMineLocalScaleError=" + F(maximumPropScaleError));
            foreach (string observation in Observations) report.AppendLine(observation);
            ElectricMineIdleLocomotionTools.WriteReviewReport(report.ToString());
            CleanupRuntime();
            SessionState.SetInt(StateKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void CompleteInEditMode()
        {
            string requestId = SessionState.GetString(RequestIdKey, string.Empty);
            string logPath = SessionState.GetString(LogPathKey, string.Empty);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            if (string.IsNullOrEmpty(failure))
            {
                ElectricMineIdleLocomotionTools.InspectStructure();
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=passed" + Environment.NewLine +
                    "ElectricMine_Idle natural two-cycle review completed.");
            }
            else
                WriteBridgeLog(logPath,
                    "Unity editor bridge request completed: " + requestId +
                    Environment.NewLine + "status=failed" + Environment.NewLine + failure);
            ClearSession();
        }

        private static void Fail(Exception exception)
        {
            CleanupRuntime();
            SessionState.SetString(FailureKey, exception.ToString());
            SessionState.SetInt(StateKey, WaitingForEditMode);
            Debug.LogWarning(
                "ElectricMine_Idle locomotion review failed: " + exception.Message);
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
            CaptureTimes.Clear();
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

        private static void WriteBridgeLog(string path, string value)
        {
            string absolute = ElectricMineIdleLocomotionTools.Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Bridge log folder is unavailable."));
            File.WriteAllText(absolute, value, new UTF8Encoding(false));
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
    }
}
