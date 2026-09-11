using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class SpeakerIdleLocomotionTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "Speaker_Idle";
        internal const string OutputFolder =
            "docs/validation/speaker_idle_locomotion_2026-09-11";
        internal const string ScaleOutputFolder =
            "docs/validation/speaker_idle_scale_2026-09-11";
        internal const string TransformReflectionOutputFolder =
            "docs/validation/speaker_idle_transform_reflection_2026-09-11_02";
        internal const string NeutralHandleHandOutputFolder =
            "docs/validation/speaker_idle_neutral_handle_hand_2026-09-11";
        internal const string PalmRightOutputFolder =
            "docs/validation/speaker_idle_palm_right_2026-09-11";
        internal const string ControllerPath =
            AssetFolder + "/SpeakerIdle_LowerBodyLocomotion.controller";
        internal const string LowerBodyMaskPath =
            AssetFolder + "/SpeakerIdle_LegsOnly.mask";
        internal const string DiagnosticImagePath = OutputFolder + "/diagnostic.png";
        internal const string FinalImagePath = OutputFolder + "/final.png";
        internal const string ScaleFinalImagePath = ScaleOutputFolder + "/final.png";
        internal const string TransformReflectionFinalImagePath =
            TransformReflectionOutputFolder + "/final.png";
        internal const string NeutralHandleHandDiagnosticImagePath =
            NeutralHandleHandOutputFolder + "/diagnostic.png";
        internal const string NeutralHandleHandFinalImagePath =
            NeutralHandleHandOutputFolder + "/final.png";
        internal const string PalmRightDiagnosticImagePath =
            PalmRightOutputFolder + "/diagnostic.png";
        internal const string PalmRightFinalImagePath =
            PalmRightOutputFolder + "/final.png";

        private const string AssetFolder =
            "Assets/_Project/Art/Player/Animations/SpeakerIdleLocomotion";
        private const string IdleClipPath = AssetFolder + "/SpeakerIdle_Idle.anim";
        private const string ForwardClipPath = AssetFolder + "/SpeakerIdle_WalkForward.anim";
        private const string BackwardClipPath = AssetFolder + "/SpeakerIdle_WalkBackward.anim";
        private const string SidestepClipPath = AssetFolder + "/SpeakerIdle_Sidestep.anim";
        private const string RunClipPath = AssetFolder + "/SpeakerIdle_RunForward.anim";
        private const string StateName = "SpeakerIdleLowerBodyLocomotion2D";
        private const string RootTreeName = "SpeakerIdleSixMotion2D";
        private const string DiagonalTreeName = "SpeakerIdleWalkDiagonalSourceExact";

        private static readonly string[] SourceNames =
        {
            "Player_Idle", "Player_Walk_Forward", "Player_Walk_Backward",
            "Player_Sidestep", "Player_Walk_Diagonal", "Player_Run_Forward"
        };

        private static readonly string[] SourceAssetPaths =
        {
            "Assets/_Project/Art/Player/Animations/Player_Idle.controller",
            "Assets/_Project/Art/Player/Animations/Player_Idle.anim",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward.controller",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward_Meshy_Walking.anim",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward.controller",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Meshy_InPlace.anim",
            "Assets/_Project/Art/Player/Animations/Player_Sidestep.controller",
            "Assets/_Project/Art/Player/Animations/Player_Sidestep_Mixamo_InPlace.anim",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Diagonal.controller",
            "Assets/_Project/Art/Player/Animations/Player_Run_Forward.controller",
            "Assets/_Project/Art/Player/Animations/transfer running.fbx"
        };

        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 0f), new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f)
        };

        internal static string DiagnosticAbsolutePath => Absolute(DiagnosticImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);
        internal static string ScaleFinalAbsolutePath => Absolute(ScaleFinalImagePath);
        internal static string TransformReflectionFinalAbsolutePath =>
            Absolute(TransformReflectionFinalImagePath);
        internal static string NeutralHandleHandDiagnosticAbsolutePath =>
            Absolute(NeutralHandleHandDiagnosticImagePath);
        internal static string NeutralHandleHandFinalAbsolutePath =>
            Absolute(NeutralHandleHandFinalImagePath);
        internal static string PalmRightDiagnosticAbsolutePath =>
            Absolute(PalmRightDiagnosticImagePath);
        internal static string PalmRightFinalAbsolutePath => Absolute(PalmRightFinalImagePath);

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle locomotion source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceOrder=" + string.Join(",", SourceNames));

            foreach (string sourceName in SourceNames)
            {
                GameObject source = FindUnique(scene, sourceName);
                Animator animator = RequireAnimator(source);
                AnimatorController controller = RequireAnimatorController(animator, sourceName);
                AnimatorControllerLayer layer = controller.layers[0];
                AnimatorState state = layer.stateMachine.defaultState ??
                    throw new InvalidOperationException(sourceName + " has no default state.");
                report.AppendLine("source=" + sourceName)
                    .AppendLine("sourceActive=" + source.activeInHierarchy)
                    .AppendLine("avatarPath=" + AssetDatabase.GetAssetPath(animator.avatar))
                    .AppendLine("controllerPath=" + AssetDatabase.GetAssetPath(controller))
                    .AppendLine("controllerSha256=" + ComputeAssetHash(
                        AssetDatabase.GetAssetPath(controller)))
                    .AppendLine("layer=" + layer.name)
                    .AppendLine("state=" + state.name)
                    .AppendLine("stateSpeed=" + F(state.speed))
                    .AppendLine("stateCycleOffset=" + F(state.cycleOffset))
                    .AppendLine("stateMirror=" + state.mirror)
                    .AppendLine("stateWriteDefaultValues=" + state.writeDefaultValues)
                    .Append(DescribeMotion(state.motion, "motion", 0));
                foreach (AnimatorControllerParameter parameter in controller.parameters)
                    report.AppendLine("parameter=" + parameter.name + "|type=" + parameter.type +
                        "|defaultFloat=" + F(parameter.defaultFloat));
            }

            GameObject target = FindUnique(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target);
            Transform hips = RequireDescendant(target.transform, "Hips");
            Transform leftLeg = RequireDescendant(hips, "LeftUpLeg");
            Transform rightLeg = RequireDescendant(hips, "RightUpLeg");
            Transform spine = RequireDescendant(hips, "Spine");
            RequireCarry(target);
            report.AppendLine("target=" + TargetName)
                .AppendLine("targetAvatarPath=" + AssetDatabase.GetAssetPath(targetAnimator.avatar))
                .AppendLine("targetControllerPath=" +
                    AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController))
                .AppendLine("targetApplyRootMotion=" + targetAnimator.applyRootMotion)
                .AppendLine("targetCullingMode=" + targetAnimator.cullingMode)
                .AppendLine("hipsPath=" + TransformPath(hips, target.transform))
                .AppendLine("leftLegRootPath=" + TransformPath(leftLeg, target.transform))
                .AppendLine("rightLegRootPath=" + TransformPath(rightLeg, target.transform))
                .AppendLine("spinePath=" + TransformPath(spine, target.transform))
                .AppendLine("shoulderFollowBehaviourPresent=True")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleLocomotion] Sources inspected read-only.");
        }

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            SpeakerLeftShoulderFollowBehaviour carry = RequireCarry(target);
            carry.RefreshPreview();
            string avatarBefore = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrEmpty(avatarBefore))
                throw new InvalidOperationException("Speaker_Idle avatar is missing.");

            var sourceHashes = SourceAssetPaths.ToDictionary(
                path => path, ComputeAssetHash, StringComparer.Ordinal);
            var sourceObjects = SourceNames.ToDictionary(
                name => name, name => HierarchyHash(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            string protectedBaseline = ProtectedBaselineText(target.transform);
            string protectedHash = Sha256(protectedBaseline);
            string speakerBaseline = SpeakerBaselineText(carry);
            string speakerHash = Sha256(speakerBaseline);

            SourceMotions sources = RequireSourceMotions(scene);
            EnsureAssetFolder();
            AnimationClip idle = CopyClip(sources.Idle, IdleClipPath, "SpeakerIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath, "SpeakerIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath, "SpeakerIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath, "SpeakerIdle_Sidestep");
            AnimationClip run = CopyClip(sources.Run, RunClipPath, "SpeakerIdle_RunForward");
            AnimatorController controller = CreateController(
                sources.Diagonal, idle, forward, backward, sidestep, run);

            Undo.RecordObject(animator, "Configure Speaker_Idle source locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarBefore, AssetDatabase.GetAssetPath(animator.avatar), "avatar");
            RequireAssetHashes(sourceHashes);
            RequireSourceObjectHashes(sourceObjects, scene);
            RequireEqual(protectedHash, Sha256(ProtectedBaselineText(target.transform)),
                "protected target transforms after Apply");
            RequireEqual(speakerHash, Sha256(SpeakerBaselineText(carry)),
                "speaker state after Apply");
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");

            WriteLines("source_asset_hashes.txt", sourceHashes.Select(
                item => item.Key + "|" + item.Value));
            WriteLines("source_object_hashes.txt", sourceObjects.Select(
                item => item.Key + "|" + item.Value));
            WriteText("target_protected_baseline.txt", protectedBaseline);
            WriteText("speaker_baseline.txt", speakerBaseline);

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    controller, out string controllerGuid, out long controllerLocalId))
                throw new InvalidOperationException("Controller identity is unavailable.");
            ulong animatorLocalId = GlobalObjectId.GetGlobalObjectIdSlow(animator).targetObjectId;
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle locomotion application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("scenePatchRequired=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("targetProtectedTransformsChanged=False")
                .AppendLine("speakerAppearanceChanged=False")
                .AppendLine("speakerModelTransformChanged=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("controller=" + ControllerPath)
                .AppendLine("controllerGuid=" + controllerGuid)
                .AppendLine("controllerLocalId=" + controllerLocalId)
                .AppendLine("animatorLocalId=" + animatorLocalId)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=6")
                .AppendLine("diagonalSourceTreeCopiedExactly=True")
                .AppendLine("diagonalParameterDefault=0.5")
                .AppendLine("animatorLayerMask=None")
                .AppendLine("sourceHipsMotionPreserved=True")
                .AppendLine("sourceTorsoHeadRightArmMotionPreserved=True")
                .AppendLine("leftArmCarryAbsolutePoseAppliedAfterAnimator=False")
                .AppendLine("leftArmAnimatedOffsetAppliedAfterAnimator=True")
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("speakerFollowsAnimatedLeftShoulder=True")
                .AppendLine("targetProtectedBaselineSha256=" + protectedHash)
                .AppendLine("speakerBaselineSha256=" + speakerHash);
            WriteText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleLocomotion] Applied without saving the dirty scene.");
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            SpeakerLeftShoulderFollowBehaviour carry = RequireCarry(target);
            AnimatorController controller = RequireAnimatorController(animator, TargetName);
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller), "controller path");
            if (animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("Speaker_Idle Animator settings are invalid.");

            RequireFloatParameter(controller,
                SpeakerIdleLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                SpeakerIdleLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                SpeakerIdleLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            if (controller.layers.Length != 1)
                throw new InvalidOperationException("Controller must have exactly one layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f) || layer.avatarMask != null)
                throw new InvalidOperationException(
                    "Source-motion layer must pass the complete original animation.");
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Default state is missing.");
            if (state.name != StateName || state.writeDefaultValues ||
                !Mathf.Approximately(state.speed, 1f))
                throw new InvalidOperationException("State settings are invalid.");
            BlendTree tree = state.motion as BlendTree ??
                throw new InvalidOperationException("Root 2D Blend Tree is missing.");
            RequireRootTree(tree);
            if (state.behaviours.OfType<SpeakerIdleLocomotionCycleBehaviour>().Count() != 1)
                throw new InvalidOperationException("One-second cycle behaviour is invalid.");

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
                sources.Diagonal, tree.children[4].motion as BlendTree,
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [sources.Forward] = forward,
                    [sources.Sidestep] = sidestep
                }, "WalkDiagonal");
            RequireBaselineAssetHashes();
            RequireBaselineSourceObjectHashes(scene);
            RequireEqual(
                Sha256(ImmutableTargetBaselineText(ReadText("target_protected_baseline.txt"))),
                Sha256(ImmutableTargetBaselineText(ProtectedBaselineText(target.transform))),
                "protected transforms");
            RequireEqual(Sha256(ImmutableSpeakerBaselineText(ReadText("speaker_baseline.txt"))),
                Sha256(ImmutableSpeakerBaselineText(SpeakerBaselineText(carry))),
                "speaker holder and appearance");
            RequireScenePatch(controller, animator);

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle locomotion inspection")
                .AppendLine("controllerStructureValid=True")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=6")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cycleDriver=AnimatorStateMachineBehaviour")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("animatorLayerMask=None")
                .AppendLine("sourceHipsMotionPreserved=True")
                .AppendLine("sourceTorsoHeadRightArmMotionPreserved=True")
                .AppendLine("leftArmCarryAbsoluteOverride=False")
                .AppendLine("leftArmAnimatedOffset=True")
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("speakerFollowsAnimatedShoulder=True")
                .AppendLine("speakerHolderAndAppearancePreserved=True")
                .AppendLine("speakerModelTransformEditableInEditMode=True")
                .AppendLine("sourceClipCurvesPreserved=True")
                .AppendLine("sourceClipLengthsPreserved=True")
                .AppendLine("sourceClipEventsPreserved=True")
                .AppendLine("sourceClipLoopSettingsPreserved=True")
                .AppendLine("diagonalSourceTreePreserved=True")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("scenePatchValid=True")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleLocomotion] Inspection passed.");
        }

        internal static void ApplyScale()
        {
            RequireEditMode();
            ClearUnityConsole();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            SpeakerLeftShoulderFollowBehaviour carry = RequireCarry(
                FindUnique(scene, TargetName));
            string baselinePath = ScaleAbsolute("protected_baseline.sha256");
            if (!File.Exists(baselinePath))
                throw new FileNotFoundException(
                    "Speaker scale baseline must be inspected before applying.", baselinePath);

            carry.RefreshPreview();
            ValidateScaleState(carry);
            RequireEqual(File.ReadAllText(baselinePath, Encoding.UTF8).Trim(),
                Sha256(ScaleProtectedText(carry)), "speaker protected transform and appearance");

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle speaker scale application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("target=Speaker_Idle/Speaker_Prop/PortableSpeaker_Model")
                .AppendLine("modelLocalPosition=" + Vec(carry.SpeakerModel.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(carry.SpeakerModel.localRotation))
                .AppendLine("modelLocalScale=" + Vec(carry.SpeakerModel.localScale))
                .AppendLine("profileModelLocalScale=" + Vec(carry.Profile.ModelLocalScale))
                .AppendLine("editModeTransformLocked=" + EditModeSpeakerTransformLocked())
                .AppendLine("positionRotationScaleXZPreserved=True")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("manualEditsPersistedToProfile=False")
                .AppendLine("playModeShoulderFollowPreserved=True");
            WriteScaleText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleScale] Applied Y scale 60 with Edit Mode transform unlocked.");
        }

        internal static void InspectScale()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            SpeakerLeftShoulderFollowBehaviour carry = RequireCarry(
                FindUnique(scene, TargetName));
            if (carry.SpeakerHolder == null || carry.SpeakerModel == null)
                throw new MissingReferenceException("Speaker preview is missing.");

            string baselinePath = ScaleAbsolute("protected_baseline.sha256");
            bool baselineCaptured = !File.Exists(baselinePath);
            if (baselineCaptured)
            {
                WriteScaleText("protected_baseline.sha256",
                    Sha256(ScaleProtectedText(carry)) + Environment.NewLine);
                var before = new StringBuilder()
                    .AppendLine("Speaker_Idle speaker scale baseline")
                    .AppendLine("verificationTargetManipulated=False")
                    .AppendLine("modelLocalPosition=" + Vec(carry.SpeakerModel.localPosition))
                    .AppendLine("modelLocalRotation=" + Quat(carry.SpeakerModel.localRotation))
                    .AppendLine("modelLocalScale=" + Vec(carry.SpeakerModel.localScale))
                    .AppendLine("profileModelLocalScale=" + Vec(carry.Profile.ModelLocalScale))
                    .AppendLine("editModeTransformLocked=" + EditModeSpeakerTransformLocked())
                    .AppendLine("sceneDirty=" + scene.isDirty);
                WriteScaleText("before.txt", before.ToString());
                UnityConsoleDiagnostics.AssertNoErrors();
                Debug.Log("[SpeakerIdleScale] Baseline inspected read-only.");
                return;
            }

            ValidateScaleState(carry);
            RequireEqual(File.ReadAllText(baselinePath, Encoding.UTF8).Trim(),
                Sha256(ScaleProtectedText(carry)), "speaker protected transform and appearance");
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle speaker scale inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("modelLocalPosition=" + Vec(carry.SpeakerModel.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(carry.SpeakerModel.localRotation))
                .AppendLine("modelLocalScale=" + Vec(carry.SpeakerModel.localScale))
                .AppendLine("profileModelLocalScale=" + Vec(carry.Profile.ModelLocalScale))
                .AppendLine("modelLocalScaleYIs60=True")
                .AppendLine("positionRotationScaleXZPreserved=True")
                .AppendLine("editModeTransformLocked=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("sceneSaved=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteScaleText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleScale] Inspection passed read-only.");
        }

        internal static void EnterReview()
        {
            RequireEditMode();
            Inspect();
            EditorApplication.EnterPlaymode();
        }

        internal static void StopReview()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Unity is changing Play Mode state.");
            RequireScene();
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(RequireScene(), TargetName);
        }

        internal static IReadOnlyDictionary<string, TransformPose> ReadProtectedBaseline()
        {
            var result = new Dictionary<string, TransformPose>(StringComparer.Ordinal);
            foreach (string line in ReadText("target_protected_baseline.txt")
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = line.Split('|');
                if (fields.Length != 11)
                    throw new InvalidOperationException("Invalid protected baseline: " + line);
                result.Add(fields[0], new TransformPose(
                    ParseVector3(fields, 1), ParseQuaternion(fields, 4),
                    ParseVector3(fields, 8)));
            }
            return result;
        }

        internal static Transform ResolveProtectedTransform(Transform target, string path)
        {
            if (path == target.name) return target;
            Transform result = target.Find(path);
            return result != null ? result : throw new MissingReferenceException(
                "Protected transform is missing: " + path);
        }

        internal static SpeakerLeftShoulderFollowBehaviour RequireCarry(GameObject target)
        {
            SpeakerLeftShoulderFollowBehaviour carry =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>();
            return carry != null ? carry : throw new MissingReferenceException(
                "Speaker_Idle shoulder-follow behaviour is missing.");
        }

        internal static string TransformPath(Transform item, Transform root)
        {
            if (item == root) return root.name;
            var parts = new List<string>();
            Transform current = item;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            if (current != root)
                throw new InvalidOperationException(item.name + " is not below " + root.name + ".");
            parts.Reverse();
            return string.Join("/", parts);
        }

        internal static void WriteText(string name, string text)
        {
            string folder = Absolute(OutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
        }

        internal static void WriteScaleText(string name, string text)
        {
            string folder = Absolute(ScaleOutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
        }

        internal static void WriteTransformReflectionText(string name, string text)
        {
            string folder = Absolute(TransformReflectionOutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
        }

        internal static void WriteNeutralHandleHandText(string name, string text)
        {
            string folder = Absolute(NeutralHandleHandOutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
        }

        internal static void WritePalmRightText(string name, string text)
        {
            string folder = Absolute(PalmRightOutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name), text, new UTF8Encoding(false));
        }

        private static SourceMotions RequireSourceMotions(Scene scene)
        {
            Motion[] motions = SourceNames.Select(name =>
            {
                AnimatorController controller = RequireAnimatorController(
                    RequireAnimator(FindUnique(scene, name)), name);
                return controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new MissingReferenceException(name + " default motion is missing.");
            }).ToArray();
            if (!(motions[0] is AnimationClip idle) ||
                !(motions[1] is AnimationClip forward) ||
                !(motions[2] is AnimationClip backward) ||
                !(motions[3] is AnimationClip sidestep) ||
                !(motions[4] is BlendTree diagonal) ||
                !(motions[5] is AnimationClip run))
                throw new InvalidOperationException("Source motion types changed.");
            if (diagonal.blendType != BlendTreeType.Simple1D || diagonal.children.Length != 2)
                throw new InvalidOperationException("Diagonal source tree changed.");
            AnimatorController diagonalController = RequireAnimatorController(
                RequireAnimator(FindUnique(scene, SourceNames[4])), SourceNames[4]);
            AnimatorControllerParameter parameter = diagonalController.parameters
                .SingleOrDefault(item => item.name == diagonal.blendParameter);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, 0.5f))
                throw new InvalidOperationException("Diagonal source default is not 0.5.");
            return new SourceMotions(idle, forward, backward, sidestep, diagonal, run);
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
            return copy;
        }

        private static AvatarMask CreateLowerBodyMask(Transform target)
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(LowerBodyMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask { name = "SpeakerIdle_LegsOnly" };
                AssetDatabase.CreateAsset(mask, LowerBodyMaskPath);
            }
            string left = TransformPath(RequireDescendant(target, "LeftUpLeg"), target);
            string right = TransformPath(RequireDescendant(target, "RightUpLeg"), target);
            string[] paths = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target)
                .Select(item => TransformPath(item, target))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item.Count(character => character == '/'))
                .ThenBy(item => item, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                mask.SetTransformPath(i, path);
                mask.SetTransformActive(i,
                    IsPathInBranch(path, left) || IsPathInBranch(path, right));
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, true);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController CreateController(
            BlendTree sourceDiagonal, AnimationClip idle, AnimationClip forward,
            AnimationClip backward, AnimationClip sidestep, AnimationClip run)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(SpeakerIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(SpeakerIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = SpeakerIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f
            });
            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = "Source Locomotion";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            AnimatorStateMachine stateMachine = layer.stateMachine;
            foreach (AnimatorState item in stateMachine.states.Select(item => item.state).ToArray())
                stateMachine.RemoveState(item);
            foreach (AnimatorStateMachine item in stateMachine.stateMachines
                .Select(item => item.stateMachine).ToArray())
                stateMachine.RemoveStateMachine(item);

            ChildMotion[] sourceChildren = sourceDiagonal.children;
            var clipMap = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)sourceChildren[0].motion] = forward,
                [(AnimationClip)sourceChildren[1].motion] = sidestep
            };
            BlendTree diagonal = CloneTree(
                sourceDiagonal, DiagonalTreeName, controller, clipMap);
            var tree = new BlendTree
            {
                name = RootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = SpeakerIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY = SpeakerIdleLocomotionCycleBehaviour.MoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.children = new[]
            {
                Child(idle, RequiredPositions[0]), Child(forward, RequiredPositions[1]),
                Child(backward, RequiredPositions[2]), Child(sidestep, RequiredPositions[3]),
                Child(diagonal, RequiredPositions[4]), Child(run, RequiredPositions[5])
            };
            AnimatorState state = stateMachine.AddState(StateName);
            state.motion = tree;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            state.AddStateMachineBehaviour<SpeakerIdleLocomotionCycleBehaviour>();
            stateMachine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(diagonal);
            return controller;
        }

        private static BlendTree CloneTree(
            BlendTree source, string name, AnimatorController owner,
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
            for (int i = 0; i < sourceChildren.Length; i++)
            {
                ChildMotion child = sourceChildren[i];
                Motion motion;
                if (child.motion is AnimationClip sourceClip)
                {
                    if (!clipMap.TryGetValue(sourceClip, out AnimationClip mapped))
                        throw new InvalidOperationException("Diagonal clip mapping is missing.");
                    motion = mapped;
                }
                else if (child.motion is BlendTree nested)
                    motion = CloneTree(nested, name + "_" + i, owner, clipMap);
                else
                    throw new InvalidOperationException("Unsupported diagonal child motion.");
                children[i] = new ChildMotion
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

        private static ChildMotion Child(Motion motion, Vector2 position)
        {
            return new ChildMotion
            {
                motion = motion, position = position, timeScale = 1f,
                cycleOffset = 0f, mirror = false, threshold = 0f,
                directBlendParameter = string.Empty
            };
        }

        private static void RequireRootTree(BlendTree tree)
        {
            if (tree.name != RootTreeName || tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter != SpeakerIdleLocomotionCycleBehaviour.MoveXParameter ||
                tree.blendParameterY != SpeakerIdleLocomotionCycleBehaviour.MoveYParameter ||
                tree.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Root 2D Blend Tree is invalid.");
            ChildMotion[] children = tree.children;
            string[] paths =
            {
                IdleClipPath, ForwardClipPath, BackwardClipPath,
                SidestepClipPath, ControllerPath, RunClipPath
            };
            for (int i = 0; i < children.Length; i++)
            {
                if ((children[i].position - RequiredPositions[i]).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(children[i].timeScale, 1f) ||
                    !Mathf.Approximately(children[i].cycleOffset, 0f) || children[i].mirror)
                    throw new InvalidOperationException("Root child differs at " + i + ".");
                RequireEqual(paths[i], AssetDatabase.GetAssetPath(children[i].motion),
                    "root child path " + i);
            }
        }

        private static void RequireLowerBodyMask(Transform target, AvatarMask mask)
        {
            if (mask == null || AssetDatabase.GetAssetPath(mask) != LowerBodyMaskPath)
                throw new InvalidOperationException("Leg-only mask is missing.");
            string left = TransformPath(RequireDescendant(target, "LeftUpLeg"), target);
            string right = TransformPath(RequireDescendant(target, "RightUpLeg"), target);
            for (int i = 0; i < mask.transformCount; i++)
            {
                string path = mask.GetTransformPath(i);
                bool expected = IsMaskPathInBranch(path, target.name, left) ||
                    IsMaskPathInBranch(path, target.name, right);
                if (mask.GetTransformActive(i) != expected)
                    throw new InvalidOperationException("Unexpected mask state: " + path);
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool expected = part == AvatarMaskBodyPart.LeftLeg ||
                    part == AvatarMaskBodyPart.RightLeg;
                if (mask.GetHumanoidBodyPartActive(part) != expected)
                    throw new InvalidOperationException("Unexpected humanoid mask state: " + part);
            }
        }

        private static void RequireCopiedClip(
            AnimationClip source, AnimationClip copy, string label)
        {
            RequireEqual(ClipSignature(source), ClipSignature(copy), label + " clip content");
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
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
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
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility
                    .GetObjectReferenceCurve(clip, binding))
                    result.AppendLine(F(key.time) + "|" + ObjectIdentity(key.value));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" + ObjectIdentity(item.objectReferenceParameter) +
                    "|" + item.messageOptions);
            return Sha256(result.ToString());
        }

        private static void RequireMotionTreeEquivalent(
            BlendTree source, BlendTree copy,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap, string label)
        {
            if (copy == null || source.blendType != copy.blendType ||
                source.blendParameter != copy.blendParameter ||
                source.blendParameterY != copy.blendParameterY ||
                source.useAutomaticThresholds != copy.useAutomaticThresholds ||
                !Mathf.Approximately(source.minThreshold, copy.minThreshold) ||
                !Mathf.Approximately(source.maxThreshold, copy.maxThreshold) ||
                source.children.Length != copy.children.Length)
                throw new InvalidOperationException(label + " tree differs.");
            ChildMotion[] a = source.children;
            ChildMotion[] b = copy.children;
            for (int i = 0; i < a.Length; i++)
            {
                if (!Mathf.Approximately(a[i].threshold, b[i].threshold) ||
                    (a[i].position - b[i].position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(a[i].timeScale, b[i].timeScale) ||
                    !Mathf.Approximately(a[i].cycleOffset, b[i].cycleOffset) ||
                    a[i].mirror != b[i].mirror ||
                    a[i].directBlendParameter != b[i].directBlendParameter)
                    throw new InvalidOperationException(label + " child differs at " + i + ".");
                if (a[i].motion is AnimationClip sourceClip)
                {
                    if (!(b[i].motion is AnimationClip copiedClip) ||
                        !clipMap.TryGetValue(sourceClip, out AnimationClip expected) ||
                        copiedClip != expected)
                        throw new InvalidOperationException(label + " clip differs at " + i + ".");
                    RequireCopiedClip(sourceClip, copiedClip, label + " child " + i);
                }
                else if (a[i].motion is BlendTree sourceTree)
                    RequireMotionTreeEquivalent(
                        sourceTree, b[i].motion as BlendTree, clipMap, label + " child " + i);
                else
                    throw new InvalidOperationException(label + " has an unsupported motion.");
            }
        }

        private static string ProtectedBaselineText(Transform target)
        {
            Transform left = RequireDescendant(target, "LeftUpLeg");
            Transform right = RequireDescendant(target, "RightUpLeg");
            SpeakerLeftShoulderFollowBehaviour carry = RequireCarry(target.gameObject);
            Transform holder = carry.SpeakerHolder != null ? carry.SpeakerHolder.transform : null;
            var result = new StringBuilder();
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != left && item != right &&
                    !item.IsChildOf(left) && !item.IsChildOf(right) &&
                    (holder == null || (item != holder && !item.IsChildOf(holder))))
                .OrderBy(item => TransformPath(item, target), StringComparer.Ordinal))
            {
                result.Append(TransformPath(item, target)).Append('|')
                    .Append(F(item.localPosition.x)).Append('|')
                    .Append(F(item.localPosition.y)).Append('|')
                    .Append(F(item.localPosition.z)).Append('|')
                    .Append(F(item.localRotation.x)).Append('|')
                    .Append(F(item.localRotation.y)).Append('|')
                    .Append(F(item.localRotation.z)).Append('|')
                    .Append(F(item.localRotation.w)).Append('|')
                    .Append(F(item.localScale.x)).Append('|')
                    .Append(F(item.localScale.y)).Append('|')
                    .AppendLine(F(item.localScale.z));
            }
            return result.ToString();
        }

        private static string SpeakerBaselineText(SpeakerLeftShoulderFollowBehaviour carry)
        {
            if (carry.SpeakerHolder == null || carry.SpeakerModel == null)
                throw new MissingReferenceException("Speaker preview is missing.");
            Transform holder = carry.SpeakerHolder.transform;
            Transform model = carry.SpeakerModel;
            return new StringBuilder()
                .AppendLine("holderLocalPosition=" + Vec(holder.localPosition))
                .AppendLine("holderLocalRotation=" + Quat(holder.localRotation))
                .AppendLine("holderLocalScale=" + Vec(holder.localScale))
                .AppendLine("modelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("modelLocalScale=" + Vec(model.localScale))
                .AppendLine("profileModelLocalPosition=" + Vec(carry.Profile.ModelLocalPosition))
                .AppendLine("profileModelLocalRotation=" + Quat(carry.Profile.ModelLocalRotation))
                .AppendLine("profileModelLocalScale=" + Vec(carry.Profile.ModelLocalScale))
                .AppendLine("appearance=" + RendererAppearanceHash(holder))
                .ToString();
        }

        private static string ImmutableSpeakerBaselineText(string baseline)
        {
            string[] prefixes =
            {
                "holderLocalPosition=", "holderLocalRotation=", "holderLocalScale=", "appearance="
            };
            return string.Join(Environment.NewLine, baseline
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => prefixes.Any(prefix => line.StartsWith(
                    prefix, StringComparison.Ordinal)))) + Environment.NewLine;
        }

        private static string ImmutableTargetBaselineText(string baseline)
        {
            const string leftArmPrefix =
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm";
            return string.Join(Environment.NewLine, baseline
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !line.StartsWith(leftArmPrefix, StringComparison.Ordinal))) +
                Environment.NewLine;
        }

        private static string ScaleProtectedText(SpeakerLeftShoulderFollowBehaviour carry)
        {
            if (carry.SpeakerHolder == null || carry.SpeakerModel == null)
                throw new MissingReferenceException("Speaker preview is missing.");
            Transform holder = carry.SpeakerHolder.transform;
            Transform model = carry.SpeakerModel;
            return new StringBuilder()
                .AppendLine("holderLocalPosition=" + Vec(holder.localPosition))
                .AppendLine("holderLocalRotation=" + Quat(holder.localRotation))
                .AppendLine("holderLocalScale=" + Vec(holder.localScale))
                .AppendLine("modelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("modelLocalScaleX=" + F(model.localScale.x))
                .AppendLine("modelLocalScaleZ=" + F(model.localScale.z))
                .AppendLine("appearance=" + RendererAppearanceHash(holder))
                .ToString();
        }

        private static void ValidateScaleState(SpeakerLeftShoulderFollowBehaviour carry)
        {
            if (carry.SpeakerHolder == null || carry.SpeakerModel == null)
                throw new MissingReferenceException("Speaker preview is missing.");
            if (Mathf.Abs(carry.SpeakerModel.localScale.y - 60f) > 0.00001f ||
                Mathf.Abs(carry.Profile.ModelLocalScale.y - 60f) > 0.00001f)
                throw new InvalidOperationException(
                    "PortableSpeaker model and profile Y scale must both be 60.");
            if (EditModeSpeakerTransformLocked())
                throw new InvalidOperationException(
                    "PortableSpeaker Edit Mode transform is still automatically locked.");
        }

        private static bool EditModeSpeakerTransformLocked()
        {
            PropertyInfo property = typeof(SpeakerLeftShoulderFollowBehaviour).GetProperty(
                "EditModeSpeakerTransformLocked",
                BindingFlags.Public | BindingFlags.Static);
            return property == null || (bool)property.GetValue(null);
        }

        private static void ClearUnityConsole()
        {
            Type logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.CoreModule");
            MethodInfo clear = logEntries?.GetMethod(
                "Clear", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            clear?.Invoke(null, null);
        }

        private static string ScaleAbsolute(string name)
        {
            return Path.Combine(Absolute(ScaleOutputFolder), name);
        }

        private static string HierarchyHash(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => TransformPath(item, root), StringComparer.Ordinal))
                text.Append(TransformPath(item, root)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .AppendLine(Vec(item.localScale));
            text.AppendLine("appearance=" + RendererAppearanceHash(root));
            return Sha256(text.ToString());
        }

        private static string RendererAppearanceHash(Transform root)
        {
            var text = new StringBuilder();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => TransformPath(item.transform, root), StringComparer.Ordinal))
            {
                text.Append(TransformPath(renderer.transform, root)).Append('|')
                    .Append(renderer.GetType().FullName).Append('|');
                if (renderer is SkinnedMeshRenderer skinned)
                    text.Append(ObjectIdentity(skinned.sharedMesh));
                else if (renderer.TryGetComponent(out MeshFilter filter))
                    text.Append(ObjectIdentity(filter.sharedMesh));
                text.Append('|').AppendLine(string.Join(",",
                    renderer.sharedMaterials.Select(ObjectIdentity)));
            }
            return Sha256(text.ToString());
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                value, out string guid, out long localId)
                ? guid + ":" + localId
                : value.GetType().FullName + ":" + value.name;
        }

        private static void RequireScenePatch(
            AnimatorController controller, Animator animator)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                controller, out string guid, out long localId);
            string sceneText = File.ReadAllText(Absolute(ScenePath), Encoding.UTF8);
            const string nameMarker = "      value: Speaker_Idle";
            int nameIndex = sceneText.IndexOf(nameMarker, StringComparison.Ordinal);
            int start = nameIndex >= 0
                ? sceneText.LastIndexOf("--- !u!1001 &", nameIndex, StringComparison.Ordinal)
                : -1;
            if (start < 0) throw new InvalidOperationException("Speaker_Idle prefab block is missing.");
            int end = sceneText.IndexOf("\n--- !u!", nameIndex + nameMarker.Length,
                StringComparison.Ordinal);
            string block = end >= 0 ? sceneText.Substring(start, end - start) :
                sceneText.Substring(start);
            string reference = "m_Controller: {fileID: " + localId + ", guid: " +
                guid + ", type: 2}";
            if (!block.Contains("propertyPath: m_Controller") ||
                !block.Contains("objectReference: {fileID: " + localId + ", guid: " +
                    guid + ", type: 2}") ||
                !block.Contains("propertyPath: m_CullingMode") ||
                !block.Contains("      value: 0"))
                throw new InvalidOperationException("Selective Animator scene patch is absent.");
        }

        private static void RequireBaselineAssetHashes()
        {
            RequireBaselineHashes("source_asset_hashes.txt", ComputeAssetHash);
        }

        private static void RequireBaselineSourceObjectHashes(Scene scene)
        {
            RequireBaselineHashes("source_object_hashes.txt",
                name => HierarchyHash(FindUnique(scene, name).transform));
        }

        private static void RequireBaselineHashes(
            string fileName, Func<string, string> actual)
        {
            foreach (string line in ReadText(fileName)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.LastIndexOf('|');
                if (separator <= 0) throw new InvalidOperationException("Invalid hash baseline.");
                string key = line.Substring(0, separator);
                RequireEqual(line.Substring(separator + 1), actual(key), key);
            }
        }

        private static void RequireAssetHashes(IReadOnlyDictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, ComputeAssetHash(item.Key), item.Key);
        }

        private static void RequireSourceObjectHashes(
            IReadOnlyDictionary<string, string> hashes, Scene scene)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value,
                    HierarchyHash(FindUnique(scene, item.Key).transform), item.Key);
        }

        private static void RequireFloatParameter(
            AnimatorController controller, string name, float expectedDefault)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expectedDefault))
                throw new InvalidOperationException("Invalid float parameter: " + name);
        }

        private static StringBuilder DescribeMotion(Motion motion, string key, int depth)
        {
            var result = new StringBuilder();
            if (motion == null) return result.AppendLine(key + "=null");
            string path = AssetDatabase.GetAssetPath(motion);
            result.AppendLine(key + "Type=" + motion.GetType().Name)
                .AppendLine(key + "Name=" + motion.name)
                .AppendLine(key + "Path=" + path)
                .AppendLine(key + "Sha256=" + ComputeAssetHash(path));
            if (motion is AnimationClip clip)
                return result.AppendLine(key + "Length=" + F(clip.length))
                    .AppendLine(key + "FrameRate=" + F(clip.frameRate))
                    .AppendLine(key + "Looping=" + clip.isLooping)
                    .AppendLine(key + "Legacy=" + clip.legacy)
                    .AppendLine(key + "Events=" + AnimationUtility.GetAnimationEvents(clip).Length)
                    .AppendLine(key + "FloatCurveCount=" +
                        AnimationUtility.GetCurveBindings(clip).Length)
                    .AppendLine(key + "ObjectCurveCount=" +
                        AnimationUtility.GetObjectReferenceCurveBindings(clip).Length);
            if (!(motion is BlendTree tree)) return result;
            result.AppendLine(key + "BlendType=" + tree.blendType)
                .AppendLine(key + "BlendParameter=" + tree.blendParameter)
                .AppendLine(key + "BlendParameterY=" + tree.blendParameterY)
                .AppendLine(key + "UseAutomaticThresholds=" + tree.useAutomaticThresholds)
                .AppendLine(key + "MinThreshold=" + F(tree.minThreshold))
                .AppendLine(key + "MaxThreshold=" + F(tree.maxThreshold))
                .AppendLine(key + "ChildCount=" + tree.children.Length);
            if (depth >= 8) throw new InvalidOperationException("Blend Tree is too deep.");
            ChildMotion[] children = tree.children;
            for (int i = 0; i < children.Length; i++)
            {
                ChildMotion child = children[i];
                string childKey = key + "Child" + i;
                result.AppendLine(childKey + "Threshold=" + F(child.threshold))
                    .AppendLine(childKey + "Position=" + Vec(child.position))
                    .AppendLine(childKey + "TimeScale=" + F(child.timeScale))
                    .AppendLine(childKey + "CycleOffset=" + F(child.cycleOffset))
                    .AppendLine(childKey + "Mirror=" + child.mirror)
                    .AppendLine(childKey + "DirectBlendParameter=" + child.directBlendParameter)
                    .Append(DescribeMotion(child.motion, childKey + "Motion", depth + 1));
            }
            return result;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("CargoRunMvp scene is not loaded.");
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("This command requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != name) continue;
                if (found != null) throw new InvalidOperationException("Duplicate object: " + name);
                found = item.gameObject;
            }
            return found != null ? found : throw new MissingReferenceException(
                "Scene object is missing: " + name);
        }

        private static Animator RequireAnimator(GameObject item)
        {
            Animator animator = item.GetComponent<Animator>();
            return animator != null ? animator : throw new MissingReferenceException(
                item.name + " Animator is missing.");
        }

        private static AnimatorController RequireAnimatorController(
            Animator animator, string owner)
        {
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            return controller != null ? controller : throw new MissingReferenceException(
                owner + " AnimatorController is missing.");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform found = null;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name != name) continue;
                if (found != null) throw new InvalidOperationException("Duplicate bone: " + name);
                found = item;
            }
            return found != null ? found : throw new MissingReferenceException(
                "Bone is missing: " + name);
        }

        private static void EnsureAssetFolder()
        {
            if (AssetDatabase.IsValidFolder(AssetFolder)) return;
            string parent = Path.GetDirectoryName(AssetFolder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                throw new DirectoryNotFoundException("Animation parent folder is missing.");
            AssetDatabase.CreateFolder(parent, Path.GetFileName(AssetFolder));
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static bool IsPathInBranch(string path, string branch)
        {
            return path == branch || path.StartsWith(branch + "/", StringComparison.Ordinal);
        }

        private static bool IsMaskPathInBranch(
            string maskPath,
            string targetName,
            string relativeBranch)
        {
            string prefix = targetName + "/";
            string relativePath = maskPath.StartsWith(prefix, StringComparison.Ordinal)
                ? maskPath.Substring(prefix.Length)
                : maskPath;
            return IsPathInBranch(relativePath, relativeBranch);
        }

        private static string ComputeAssetHash(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "none";
            string path = Absolute(assetPath);
            if (!File.Exists(path)) return "subasset:" + assetPath;
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text)))
                    .Replace("-", string.Empty);
        }

        private static void WriteLines(string name, IEnumerable<string> lines)
        {
            WriteText(name, string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }

        private static string ReadText(string name)
        {
            string path = Path.Combine(Absolute(OutputFolder), name);
            if (!File.Exists(path)) throw new FileNotFoundException("Baseline is missing.", path);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(
                label + " differs. expected=" + expected + " actual=" + actual);
        }

        private static Vector3 ParseVector3(string[] fields, int start)
        {
            return new Vector3(Parse(fields[start]), Parse(fields[start + 1]), Parse(fields[start + 2]));
        }

        private static Quaternion ParseQuaternion(string[] fields, int start)
        {
            return new Quaternion(Parse(fields[start]), Parse(fields[start + 1]),
                Parse(fields[start + 2]), Parse(fields[start + 3]));
        }

        private static float Parse(string text)
        {
            return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector2 value) =>
            "(" + F(value.x) + "," + F(value.y) + ")";
        private static string Vec(Vector3 value) =>
            "(" + F(value.x) + "," + F(value.y) + "," + F(value.z) + ")";
        private static string Quat(Quaternion value) =>
            "(" + F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w) + ")";

        internal readonly struct TransformPose
        {
            internal TransformPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }
        }

        private sealed class SourceMotions
        {
            internal SourceMotions(
                AnimationClip idle, AnimationClip forward, AnimationClip backward,
                AnimationClip sidestep, BlendTree diagonal, AnimationClip run)
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
    }

    internal static class SpeakerIdleLocomotionPlayModeCapture
    {
        private const int PanelWidth = 320;
        private const int PanelHeight = 480;
        private const int PanelCount = SpeakerIdleLocomotionCycleBehaviour.MotionCount * 2;
        private const float CapturePhaseTime = 0.48f;
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.001f;
        private const double TimeoutSeconds = 32d;

        private static readonly Color[] PhaseColors =
        {
            new Color(0.35f, 0.75f, 1f),
            new Color(0.3f, 0.9f, 0.45f),
            new Color(1f, 0.65f, 0.25f),
            new Color(0.9f, 0.35f, 0.85f),
            new Color(0.95f, 0.85f, 0.25f),
            new Color(1f, 0.3f, 0.3f)
        };

        private static bool active;
        private static bool finalCapture;
        private static bool scaleFinalCapture;
        private static bool transformReflectionFinalCapture;
        private static bool neutralHandleHandCapture;
        private static bool palmRightCapture;
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static double startTime;
        private static int baseAbsolutePhase = -1;
        private static int nextPanel;
        private static GameObject target;
        private static Animator animator;
        private static IReadOnlyDictionary<string, SpeakerIdleLocomotionTools.TransformPose>
            protectedBaseline;
        private static Texture2D[] panels;
        private static readonly List<string> Observations = new List<string>();
        private static readonly HashSet<string> LegPoseHashes = new HashSet<string>();
        private static readonly HashSet<string> UpperPoseHashes = new HashSet<string>();
        private static readonly HashSet<string> LeftArmPoseHashes = new HashSet<string>();
        private static float maximumProtectedPositionError;
        private static float maximumProtectedRotationError;
        private static float maximumProtectedScaleError;
        private static float maximumSpeakerPositionError;
        private static float maximumSpeakerRotationError;
        private static float maximumLeftArmRotationError;
        private static float minimumPalmToHandleDistance;
        private static float maximumPalmToHandleDistance;
        private static float maximumPalmRightAngle;

        internal static bool HasPendingCapture => active;

        internal static void Start(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(final, false, completeCallback, failCallback);
        }

        internal static void StartScaleFinal(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(true, true, completeCallback, failCallback);
        }

        internal static void StartTransformReflectionFinal(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(true, false, true, completeCallback, failCallback);
        }

        internal static void StartNeutralHandleHand(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(final, false, false, true, false,
                completeCallback, failCallback);
        }

        internal static void StartPalmRight(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(final, false, false, false, true,
                completeCallback, failCallback);
        }

        private static void StartInternal(
            bool final,
            bool scaleFinal,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(final, scaleFinal, false, completeCallback, failCallback);
        }

        private static void StartInternal(
            bool final,
            bool scaleFinal,
            bool transformReflectionFinal,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            StartInternal(final, scaleFinal, transformReflectionFinal, false, false,
                completeCallback, failCallback);
        }

        private static void StartInternal(
            bool final,
            bool scaleFinal,
            bool transformReflectionFinal,
            bool neutralHandleHand,
            bool palmRight,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode &&
                !EditorApplication.isPlaying)
            {
                failCallback(new InvalidOperationException(
                    "EnterSpeakerIdleLocomotionReview must reach Play Mode before capture."));
                return;
            }
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }

            try
            {
                active = true;
                finalCapture = final;
                scaleFinalCapture = scaleFinal;
                transformReflectionFinalCapture = transformReflectionFinal;
                neutralHandleHandCapture = neutralHandleHand;
                palmRightCapture = palmRight;
                complete = completeCallback;
                fail = failCallback;
                startTime = EditorApplication.timeSinceStartup;
                baseAbsolutePhase = -1;
                nextPanel = 0;
                target = SpeakerIdleLocomotionTools.RequireRuntimeTarget();
                animator = target.GetComponent<Animator>() ??
                    throw new MissingReferenceException("Speaker_Idle runtime Animator is missing.");
                protectedBaseline = SpeakerIdleLocomotionTools.ReadProtectedBaseline();
                panels = new Texture2D[PanelCount];
                Observations.Clear();
                LegPoseHashes.Clear();
                UpperPoseHashes.Clear();
                LeftArmPoseHashes.Clear();
                maximumProtectedPositionError = 0f;
                maximumProtectedRotationError = 0f;
                maximumProtectedScaleError = 0f;
                maximumSpeakerPositionError = 0f;
                maximumSpeakerRotationError = 0f;
                maximumLeftArmRotationError = 0f;
                minimumPalmToHandleDistance = float.PositiveInfinity;
                maximumPalmToHandleDistance = 0f;
                maximumPalmRightAngle = 0f;
                EditorApplication.update += Tick;
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        internal static void Resume(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            Start(final, completeCallback, failCallback);
        }

        internal static void ResumeScaleFinal(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            StartScaleFinal(completeCallback, failCallback);
        }

        internal static void ResumeTransformReflectionFinal(
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            StartTransformReflectionFinal(completeCallback, failCallback);
        }

        internal static void ResumeNeutralHandleHand(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            StartNeutralHandleHand(final, completeCallback, failCallback);
        }

        internal static void ResumePalmRight(
            bool final,
            Action<string> completeCallback,
            Action<Exception> failCallback)
        {
            if (active)
            {
                complete = completeCallback;
                fail = failCallback;
                return;
            }
            StartPalmRight(final, completeCallback, failCallback);
        }

        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play Mode ended before capture completed.");
                if (EditorApplication.timeSinceStartup - startTime > TimeoutSeconds)
                    throw new TimeoutException("Natural two-cycle capture exceeded 32 seconds.");
                if (!animator.isInitialized ||
                    !SpeakerIdleLocomotionCycleBehaviour.TryGetSequenceState(
                        animator, out int absolutePhase, out int phase, out float phaseElapsed))
                    return;

                if (baseAbsolutePhase < 0)
                {
                    if (phase != 0 || phaseElapsed > 0.65f) return;
                    baseAbsolutePhase = absolutePhase;
                }

                int expectedAbsolutePhase = baseAbsolutePhase + nextPanel;
                if (absolutePhase < expectedAbsolutePhase || phaseElapsed < CapturePhaseTime) return;
                if (absolutePhase > expectedAbsolutePhase)
                    throw new InvalidOperationException(
                        "A natural sequence phase was missed during capture.");
                int expectedPhase = nextPanel % SpeakerIdleLocomotionCycleBehaviour.MotionCount;
                if (phase != expectedPhase)
                    throw new InvalidOperationException("Observed phase order differs from the request.");

                MeasureProtectedTransforms();
                if (neutralHandleHandCapture || palmRightCapture) MeasureAnimatedLeftArm();
                else MeasureLeftArmPose();
                MeasureSpeaker();
                LegPoseHashes.Add(LegPoseSignature());
                UpperPoseHashes.Add(UpperPoseSignature());
                Texture2D panel = CapturePanel();
                DrawPhaseBorder(panel, PhaseColors[phase]);
                panels[nextPanel] = panel;
                Observations.Add("panel=" + nextPanel +
                    "|cycle=" + (nextPanel / SpeakerIdleLocomotionCycleBehaviour.MotionCount + 1) +
                    "|phase=" + phase +
                    "|motion=" + SpeakerIdleLocomotionCycleBehaviour.MotionName(phase) +
                    "|phaseElapsed=" + phaseElapsed.ToString("R", CultureInfo.InvariantCulture) +
                    "|moveX=" + animator.GetFloat(
                        SpeakerIdleLocomotionCycleBehaviour.MoveXParameter).ToString(
                            "R", CultureInfo.InvariantCulture) +
                    "|moveY=" + animator.GetFloat(
                        SpeakerIdleLocomotionCycleBehaviour.MoveYParameter).ToString(
                            "R", CultureInfo.InvariantCulture));
                nextPanel++;
                if (nextPanel == PanelCount) Finish();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void MeasureProtectedTransforms()
        {
            foreach (KeyValuePair<string, SpeakerIdleLocomotionTools.TransformPose> item in
                protectedBaseline)
            {
                if (item.Key == "Armature" ||
                    item.Key.StartsWith("Armature/", StringComparison.Ordinal))
                    continue;
                Transform current = SpeakerIdleLocomotionTools.ResolveProtectedTransform(
                    target.transform, item.Key);
                float positionError = Vector3.Distance(current.localPosition, item.Value.Position);
                float rotationError = Quaternion.Angle(current.localRotation, item.Value.Rotation);
                float scaleError = Vector3.Distance(current.localScale, item.Value.Scale);
                maximumProtectedPositionError = Mathf.Max(
                    maximumProtectedPositionError, positionError);
                maximumProtectedRotationError = Mathf.Max(
                    maximumProtectedRotationError, rotationError);
                maximumProtectedScaleError = Mathf.Max(maximumProtectedScaleError, scaleError);
                if (positionError > PositionTolerance || rotationError > RotationTolerance ||
                    scaleError > PositionTolerance)
                    throw new InvalidOperationException(
                        "Protected transform changed during natural playback: " + item.Key +
                        " positionError=" + positionError +
                        " rotationError=" + rotationError +
                        " scaleError=" + scaleError);
            }
        }

        private static void MeasureLeftArmPose()
        {
            SpeakerLeftShoulderFollowBehaviour carry =
                SpeakerIdleLocomotionTools.RequireCarry(target);
            foreach (SpeakerBoneRotation pose in carry.Profile.LeftArmPose)
            {
                Transform bone = target.transform.Find(pose.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "Left-arm carry bone is missing: " + pose.Path);
                float error = Quaternion.Angle(bone.localRotation, pose.LocalRotation);
                maximumLeftArmRotationError = Mathf.Max(maximumLeftArmRotationError, error);
                if (error > RotationTolerance)
                    throw new InvalidOperationException(
                        "Left-arm carry pose changed during source locomotion: " +
                        pose.Path + " error=" + error);
            }
        }

        private static void MeasureAnimatedLeftArm()
        {
            SpeakerLeftShoulderFollowBehaviour carry =
                SpeakerIdleLocomotionTools.RequireCarry(target);
            if (SpeakerLeftShoulderFollowBehaviour.LeftHandGripLocked)
                throw new InvalidOperationException(
                    "Speaker_Idle left hand must not be locked to the handle.");
            string[] paths = carry.Profile.LeftArmPose.Select(item => item.Path).ToArray();
            if (paths.Length != 3)
                throw new InvalidOperationException(
                    "Speaker_Idle animated handle-near pose must contain three arm bones.");
            var signature = new StringBuilder();
            foreach (string path in paths)
            {
                Transform bone = target.transform.Find(path) ??
                    throw new MissingReferenceException("Left-arm bone is missing: " + path);
                signature.Append(path).Append('|').AppendLine(
                    bone.localRotation.ToString("R", CultureInfo.InvariantCulture));
            }
            using (SHA256 sha = SHA256.Create())
                LeftArmPoseHashes.Add(BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(signature.ToString())))
                    .Replace("-", string.Empty));

            float palmToHandleDistance = Vector3.Distance(
                LeftPalmCenter(), carry.HandleWorldPosition());
            minimumPalmToHandleDistance = Mathf.Min(
                minimumPalmToHandleDistance, palmToHandleDistance);
            maximumPalmToHandleDistance = Mathf.Max(
                maximumPalmToHandleDistance, palmToHandleDistance);
            if (palmRightCapture)
                maximumPalmRightAngle = Mathf.Max(
                    maximumPalmRightAngle,
                    Vector3.Angle(LeftPalmNormal(), target.transform.right.normalized));
        }

        private static Vector3 LeftPalmCenter()
        {
            string[] names =
            {
                "LeftIndexProximal", "LeftMiddleProximal",
                "LeftRingProximal", "LeftLittleProximal"
            };
            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            return names.Select(name => transforms.Single(item => item.name == name).position)
                .Aggregate(Vector3.zero, (sum, point) => sum + point) / names.Length;
        }

        private static Vector3 LeftPalmNormal()
        {
            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            Transform hand = transforms.Single(item => item.name == "LeftHand");
            Transform index = transforms.Single(item => item.name == "LeftIndexProximal");
            Transform middle = transforms.Single(item => item.name == "LeftMiddleProximal");
            Transform little = transforms.Single(item => item.name == "LeftLittleProximal");
            Vector3 finger = (middle.position - hand.position).normalized;
            Vector3 width = (little.position - index.position).normalized;
            return Vector3.Cross(finger, width).normalized;
        }

        private static void MeasureSpeaker()
        {
            SpeakerLeftShoulderFollowBehaviour carry =
                SpeakerIdleLocomotionTools.RequireCarry(target);
            if (carry.SpeakerHolder == null || carry.SpeakerModel == null)
                throw new MissingReferenceException("Runtime speaker instance is missing.");
            Transform holder = carry.SpeakerHolder.transform;
            Transform model = carry.SpeakerModel;
            float holderPositionError = Vector3.Distance(
                holder.position, carry.ExpectedWorldPosition());
            float holderRotationError = Quaternion.Angle(
                holder.rotation, carry.ExpectedWorldRotation());
            float modelPositionError = Vector3.Distance(
                model.localPosition, carry.Profile.ModelLocalPosition);
            float modelRotationError = Quaternion.Angle(
                model.localRotation, carry.Profile.ModelLocalRotation);
            float holderScaleError = Vector3.Distance(
                holder.localScale, carry.Profile.HolderScale);
            float modelScaleError = Vector3.Distance(
                model.localScale, carry.Profile.ModelLocalScale);
            maximumSpeakerPositionError = Mathf.Max(
                maximumSpeakerPositionError,
                Mathf.Max(holderPositionError, modelPositionError));
            maximumSpeakerRotationError = Mathf.Max(
                maximumSpeakerRotationError,
                Mathf.Max(holderRotationError, modelRotationError));
            if (holderPositionError > PositionTolerance ||
                modelPositionError > PositionTolerance ||
                holderRotationError > RotationTolerance ||
                modelRotationError > RotationTolerance ||
                holderScaleError > PositionTolerance || modelScaleError > PositionTolerance)
                throw new InvalidOperationException(
                    "Speaker carry state changed during natural playback.");
        }

        private static string LegPoseSignature()
        {
            Transform left = target.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "LeftUpLeg");
            Transform right = target.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "RightUpLeg");
            var text = new StringBuilder();
            foreach (Transform item in left.GetComponentsInChildren<Transform>(true)
                .Concat(right.GetComponentsInChildren<Transform>(true)))
                text.Append(item.name).Append('|')
                    .Append(item.localPosition.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').AppendLine(
                        item.localRotation.ToString("R", CultureInfo.InvariantCulture));
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())))
                    .Replace("-", string.Empty);
        }

        private static string UpperPoseSignature()
        {
            string[] names =
            {
                "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
                "RightShoulder", "RightArm", "RightForeArm", "RightHand"
            };
            var text = new StringBuilder();
            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            foreach (string name in names)
            {
                Transform item = transforms.Single(transform => transform.name == name);
                text.Append(name).Append('|')
                    .Append(item.localPosition.ToString("R", CultureInfo.InvariantCulture))
                    .Append('|').AppendLine(
                        item.localRotation.ToString("R", CultureInfo.InvariantCulture));
            }
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())))
                    .Replace("-", string.Empty);
        }

        private static Texture2D CapturePanel()
        {
            Bounds bounds = CalculateBounds();
            var cameraObject = new GameObject("SpeakerIdleLocomotion_CaptureCamera");
            var lightObject = new GameObject("SpeakerIdleLocomotion_CaptureLight");
            var camera = cameraObject.AddComponent<Camera>();
            var light = lightObject.AddComponent<Light>();
            var renderTexture = new RenderTexture(PanelWidth, PanelHeight, 24,
                RenderTextureFormat.ARGB32);
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.075f, 0.085f, 0.105f, 1f);
                camera.fieldOfView = 28f;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                float aspect = (float)PanelWidth / PanelHeight;
                float framedHeight = Mathf.Max(bounds.size.y, bounds.size.x / aspect);
                float distance = framedHeight * 0.58f /
                    Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                Vector3 viewDirection = target.transform.forward.normalized;
                camera.transform.position = bounds.center + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position, target.transform.up);
                camera.targetTexture = renderTexture;

                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.LookRotation(
                    -viewDirection + target.transform.right * 0.35f - target.transform.up * 0.2f);
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(
                    PanelWidth, PanelHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, PanelWidth, PanelHeight), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Bounds CalculateBounds()
        {
            if (neutralHandleHandCapture || palmRightCapture)
            {
                SpeakerLeftShoulderFollowBehaviour carry =
                    SpeakerIdleLocomotionTools.RequireCarry(target);
                Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
                Transform head = transforms.Single(item => item.name == "Head");
                Transform upperArm = transforms.Single(item => item.name == "LeftArm");
                Transform foreArm = transforms.Single(item => item.name == "LeftForeArm");
                Transform hand = transforms.Single(item => item.name == "LeftHand");
                var closeBounds = new Bounds(head.position, Vector3.one * 0.08f);
                closeBounds.Encapsulate(upperArm.position);
                closeBounds.Encapsulate(foreArm.position);
                closeBounds.Encapsulate(hand.position);
                foreach (Renderer renderer in carry.SpeakerHolder
                             .GetComponentsInChildren<Renderer>(true)
                             .Where(item => item.enabled))
                    closeBounds.Encapsulate(renderer.bounds);
                closeBounds.Expand(0.16f);
                return closeBounds;
            }
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new MissingReferenceException("Speaker_Idle has no active renderer.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            bounds.Expand(bounds.size * 0.12f);
            return bounds;
        }

        private static void DrawPhaseBorder(Texture2D panel, Color color)
        {
            const int border = 6;
            for (int y = 0; y < PanelHeight; y++)
            for (int x = 0; x < PanelWidth; x++)
            {
                if (x < border || x >= PanelWidth - border ||
                    y < border || y >= PanelHeight - border)
                    panel.SetPixel(x, y, color);
            }
            panel.Apply(false, false);
        }

        private static void Finish()
        {
            if (LegPoseHashes.Count < 2)
                throw new InvalidOperationException(
                    "The lower-body pose did not vary during the requested sequence. " +
                    "distinctLegPoseCount=" + LegPoseHashes.Count);
            if (UpperPoseHashes.Count < 2)
                throw new InvalidOperationException(
                    "The source Hips and upper-body motion remained frozen. " +
                    "distinctUpperPoseCount=" + UpperPoseHashes.Count);
            if ((neutralHandleHandCapture || palmRightCapture) &&
                LeftArmPoseHashes.Count < 2)
                throw new InvalidOperationException(
                    "The Speaker_Idle left arm remained fixed during source locomotion. " +
                    "distinctLeftArmPoseCount=" + LeftArmPoseHashes.Count);
            Texture2D composite = ComposePanels();
            string outputPath = palmRightCapture
                ? finalCapture
                    ? SpeakerIdleLocomotionTools.PalmRightFinalAbsolutePath
                    : SpeakerIdleLocomotionTools.PalmRightDiagnosticAbsolutePath
                : neutralHandleHandCapture
                ? finalCapture
                    ? SpeakerIdleLocomotionTools.NeutralHandleHandFinalAbsolutePath
                    : SpeakerIdleLocomotionTools.NeutralHandleHandDiagnosticAbsolutePath
                : transformReflectionFinalCapture
                ? SpeakerIdleLocomotionTools.TransformReflectionFinalAbsolutePath
                : scaleFinalCapture
                ? SpeakerIdleLocomotionTools.ScaleFinalAbsolutePath
                : finalCapture
                    ? SpeakerIdleLocomotionTools.FinalAbsolutePath
                    : SpeakerIdleLocomotionTools.DiagnosticAbsolutePath;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, composite.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(composite);
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle natural locomotion runtime capture")
                .AppendLine("captureKind=" + (palmRightCapture
                    ? finalCapture ? "PalmRightFinal" : "PalmRightDiagnostic"
                    : neutralHandleHandCapture
                    ? finalCapture ? "NeutralHandleHandFinal" : "NeutralHandleHandDiagnostic"
                    : transformReflectionFinalCapture
                    ? "TransformReflectionFinal"
                    : scaleFinalCapture ? "ScaleFinal"
                    : finalCapture ? "Final" : "Diagnostic"))
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=2")
                .AppendLine("panelsCaptured=12")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sourceHipsAndUpperBodyMotionVisible=True")
                .AppendLine("leftArmCarryPosePreserved=" +
                    !(neutralHandleHandCapture || palmRightCapture))
                .AppendLine("leftArmNaturalMotionPreserved=" +
                    (neutralHandleHandCapture || palmRightCapture))
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("leftArmMotionWeight=" +
                    SpeakerLeftShoulderFollowBehaviour.LeftArmMotionWeight.ToString(
                        "R", CultureInfo.InvariantCulture))
                .AppendLine("leftForeArmMotionWeight=" +
                    SpeakerLeftShoulderFollowBehaviour.LeftForeArmMotionWeight.ToString(
                        "R", CultureInfo.InvariantCulture))
                .AppendLine("leftHandMotionWeight=" +
                    SpeakerLeftShoulderFollowBehaviour.LeftHandMotionWeight.ToString(
                        "R", CultureInfo.InvariantCulture))
                .AppendLine("palmRightMaximumDeviationDegrees=" +
                    SpeakerLeftShoulderFollowBehaviour.PalmRightMaximumDeviationDegrees.ToString(
                        "R", CultureInfo.InvariantCulture))
                .AppendLine("speakerFollowsAnimatedShoulder=True")
                .AppendLine("distinctLegPoseCount=" + LegPoseHashes.Count)
                .AppendLine("distinctUpperPoseCount=" + UpperPoseHashes.Count)
                .AppendLine("distinctLeftArmPoseCount=" + LeftArmPoseHashes.Count)
                .AppendLine("minimumPalmToHandleDistance=" +
                    minimumPalmToHandleDistance.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumPalmToHandleDistance=" +
                    maximumPalmToHandleDistance.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumPalmRightAngleDegrees=" +
                    maximumPalmRightAngle.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumProtectedPositionError=" +
                    maximumProtectedPositionError.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumProtectedRotationErrorDegrees=" +
                    maximumProtectedRotationError.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumProtectedScaleError=" +
                    maximumProtectedScaleError.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumSpeakerPositionError=" +
                    maximumSpeakerPositionError.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumSpeakerRotationErrorDegrees=" +
                    maximumSpeakerRotationError.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("maximumLeftArmRotationErrorDegrees=" +
                    maximumLeftArmRotationError.ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("modelLocalScale=" +
                    SpeakerIdleLocomotionTools.RequireCarry(target).SpeakerModel.localScale
                        .ToString("R", CultureInfo.InvariantCulture))
                .AppendLine("modelLocalScaleMatchesProfile=" +
                    (Vector3.Distance(
                        SpeakerIdleLocomotionTools.RequireCarry(target).SpeakerModel.localScale,
                        SpeakerIdleLocomotionTools.RequireCarry(target).Profile.ModelLocalScale) <=
                        PositionTolerance));
            foreach (string observation in Observations) report.AppendLine(observation);
            if (palmRightCapture)
                SpeakerIdleLocomotionTools.WritePalmRightText(
                    finalCapture ? "final_runtime.txt" : "diagnostic_runtime.txt",
                    report.ToString());
            else if (neutralHandleHandCapture)
                SpeakerIdleLocomotionTools.WriteNeutralHandleHandText(
                    finalCapture ? "final_runtime.txt" : "diagnostic_runtime.txt",
                    report.ToString());
            else if (transformReflectionFinalCapture)
                SpeakerIdleLocomotionTools.WriteTransformReflectionText(
                    "final_runtime.txt", report.ToString());
            else if (scaleFinalCapture)
                SpeakerIdleLocomotionTools.WriteScaleText("final_runtime.txt", report.ToString());
            else
                SpeakerIdleLocomotionTools.WriteText(
                    finalCapture ? "final_runtime.txt" : "diagnostic_runtime.txt",
                    report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            string marker = "Speaker_Idle natural two-cycle " +
                (palmRightCapture
                    ? finalCapture ? "palm-right final" : "palm-right diagnostic"
                    : neutralHandleHandCapture
                    ? finalCapture ? "neutral handle hand final" :
                        "neutral handle hand diagnostic"
                    : transformReflectionFinalCapture
                    ? "transform reflection final"
                    : scaleFinalCapture ? "scale final" : finalCapture ? "final" : "diagnostic") +
                " capture completed.";
            Action<string> callback = complete;
            Cleanup();
            callback?.Invoke(marker);
        }

        private static Texture2D ComposePanels()
        {
            int columns = SpeakerIdleLocomotionCycleBehaviour.MotionCount;
            var composite = new Texture2D(
                PanelWidth * columns, PanelHeight * 2, TextureFormat.RGB24, false);
            Color background = new Color(0.04f, 0.045f, 0.055f, 1f);
            Color[] all = Enumerable.Repeat(
                background, composite.width * composite.height).ToArray();
            composite.SetPixels(all);
            for (int i = 0; i < panels.Length; i++)
            {
                int column = i % columns;
                int cycle = i / columns;
                int y = cycle == 0 ? PanelHeight : 0;
                composite.SetPixels(column * PanelWidth, y,
                    PanelWidth, PanelHeight, panels[i].GetPixels());
            }
            composite.Apply(false, false);
            return composite;
        }

        private static void Fail(Exception exception)
        {
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(exception);
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (panels != null)
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            }
            active = false;
            finalCapture = false;
            scaleFinalCapture = false;
            transformReflectionFinalCapture = false;
            neutralHandleHandCapture = false;
            palmRightCapture = false;
            complete = null;
            fail = null;
            target = null;
            animator = null;
            protectedBaseline = null;
            panels = null;
            Observations.Clear();
            LegPoseHashes.Clear();
            UpperPoseHashes.Clear();
            LeftArmPoseHashes.Clear();
        }
    }
}
