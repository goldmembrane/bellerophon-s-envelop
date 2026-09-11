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
    internal static class HoloSprayIdleLocomotionTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "HoloSpray_Idle";
        internal const string OutputFolder =
            "docs/validation/holo_spray_idle_locomotion_2026-09-11";
        internal const string ControllerPath =
            AssetFolder + "/HoloSprayIdle_Locomotion.controller";
        internal const string FinalImagePath = OutputFolder + "/final.png";

        private const string AssetFolder =
            "Assets/_Project/Art/Player/Animations/HoloSprayIdleLocomotion";
        private const string IdleClipPath = AssetFolder + "/HoloSprayIdle_Idle.anim";
        private const string ForwardClipPath = AssetFolder + "/HoloSprayIdle_WalkForward.anim";
        private const string BackwardClipPath = AssetFolder + "/HoloSprayIdle_WalkBackward.anim";
        private const string SidestepClipPath = AssetFolder + "/HoloSprayIdle_Sidestep.anim";
        private const string RunClipPath = AssetFolder + "/HoloSprayIdle_RunForward.anim";
        private const string StateName = "HoloSprayIdleLocomotion2D";
        private const string RootTreeName = "HoloSprayIdleSixMotion2D";
        private const string DiagonalTreeName = "HoloSprayIdleWalkDiagonalSourceExact";
        private const string CarryProfilePath =
            "Assets/_Project/Art/Player/Animations/HoloSprayIdle/HoloSprayIdleCarryProfile.asset";
        private const string ModelPath =
            "Assets/_Project/Art/Items/HoloSpray/HolographicSpray.fbx";
        private const string MaterialPath =
            "Assets/_Project/Art/Items/HoloSpray/Materials/HolographicSpray.mat";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.05f;

        private static readonly string[] TexturePaths =
        {
            "Assets/_Project/Art/Items/HoloSpray/Textures/base_color.jpg",
            "Assets/_Project/Art/Items/HoloSpray/Textures/normal.jpg",
            "Assets/_Project/Art/Items/HoloSpray/Textures/texture_0_metallic.png",
            "Assets/_Project/Art/Items/HoloSpray/Textures/texture_0_roughness.png",
            "Assets/_Project/Art/Items/HoloSpray/Textures/HolographicSpray_MetallicSmoothness.png"
        };

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

        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            SourceMotions sources = RequireSourceMotions(scene);
            GameObject target = FindUnique(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target);
            HoloSprayRightHandFollowBehaviour carry = RequireCarry(target);
            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle locomotion source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("sourceOrder=" + string.Join(",", SourceNames));

            foreach (string sourceName in SourceNames)
            {
                GameObject source = FindUnique(scene, sourceName);
                Animator animator = RequireAnimator(source);
                AnimatorController controller = RequireAnimatorController(animator, sourceName);
                AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                    throw new InvalidOperationException(sourceName + " has no default state.");
                report.AppendLine("source=" + sourceName)
                    .AppendLine("controllerPath=" + AssetDatabase.GetAssetPath(controller))
                    .AppendLine("controllerSha256=" +
                        ComputeAssetHash(AssetDatabase.GetAssetPath(controller)))
                    .AppendLine("state=" + state.name)
                    .AppendLine("stateSpeed=" + F(state.speed))
                    .AppendLine("stateMirror=" + state.mirror)
                    .Append(DescribeMotion(state.motion, "motion"));
            }

            report.AppendLine("target=" + TargetName)
                .AppendLine("targetAvatarPath=" + AssetDatabase.GetAssetPath(targetAnimator.avatar))
                .AppendLine("targetControllerPath=" +
                    AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController))
                .AppendLine("carryProfilePath=" + AssetDatabase.GetAssetPath(carry.Profile))
                .AppendLine("rightHandFollowPresent=True")
                .AppendLine("diagonalSourceChildCount=" + sources.Diagonal.children.Length)
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdleLocomotion] Sources inspected read-only.");
        }

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            if (sceneWasDirty)
                throw new InvalidOperationException(
                    "HoloSpray_Idle locomotion Apply requires a clean scene so unrelated " +
                    "user changes are never saved with the Animator override.");
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            HoloSprayRightHandFollowBehaviour carry = RequireCarry(target);
            carry.RefreshPreview();
            string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrEmpty(avatarPath))
                throw new InvalidOperationException("HoloSpray_Idle avatar is missing.");

            Dictionary<string, string> sourceHashes = SourceAssetPaths.ToDictionary(
                path => path, ComputeAssetHash, StringComparer.Ordinal);
            Dictionary<string, string> sourceObjects = SourceNames.ToDictionary(
                name => name,
                name => HierarchyHash(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            Dictionary<string, string> carryHashes = CarryAssetPaths().ToDictionary(
                path => path, ComputeAssetHash, StringComparer.Ordinal);
            string targetBaseline = TargetBaselineText(target.transform);
            string carryBaseline = CarryBaselineText(carry);

            SourceMotions sources = RequireSourceMotions(scene);
            EnsureAssetFolder();
            AnimationClip idle = CopyClip(sources.Idle, IdleClipPath, "HoloSprayIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath, "HoloSprayIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath, "HoloSprayIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath, "HoloSprayIdle_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, RunClipPath, "HoloSprayIdle_RunForward");
            AnimatorController controller = CreateController(
                sources.Diagonal, idle, forward, backward, sidestep, run);

            Undo.RecordObject(animator, "Configure HoloSpray_Idle source locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(animator.avatar), "avatar");
            RequireHashes(sourceHashes);
            RequireSourceObjects(sourceObjects, scene);
            RequireHashes(carryHashes);
            RequireEqual(targetBaseline, TargetBaselineText(target.transform),
                "target hierarchy after Apply");
            RequireEqual(carryBaseline, CarryBaselineText(carry), "carry state after Apply");
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the HoloSpray_Idle Animator override.");
            RequireScenePatch(controller, animator);

            WriteLines("source_asset_hashes.txt", sourceHashes.Select(
                item => item.Key + "|" + item.Value));
            WriteLines("source_object_hashes.txt", sourceObjects.Select(
                item => item.Key + "|" + item.Value));
            WriteLines("carry_asset_hashes.txt", carryHashes.Select(
                item => item.Key + "|" + item.Value));
            WriteText("target_baseline.txt", targetBaseline);
            WriteText("carry_baseline.txt", carryBaseline);

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    controller, out string controllerGuid, out long controllerLocalId))
                throw new InvalidOperationException("Controller identity is unavailable.");
            Component sourceAnimator = PrefabUtility.GetCorrespondingObjectFromSource(animator);
            if (sourceAnimator == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    sourceAnimator, out string prefabGuid, out long sourceAnimatorLocalId))
                throw new InvalidOperationException("Source Animator identity is unavailable.");
            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle locomotion application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=True")
                .AppendLine("scenePatchRequired=False")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("currentCarryProfileChanged=False")
                .AppendLine("holoSprayAppearanceChanged=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("controller=" + ControllerPath)
                .AppendLine("controllerGuid=" + controllerGuid)
                .AppendLine("controllerLocalId=" + controllerLocalId)
                .AppendLine("prefabGuid=" + prefabGuid)
                .AppendLine("sourceAnimatorLocalId=" + sourceAnimatorLocalId)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=6")
                .AppendLine("diagonalSourceTreeCopiedExactly=True")
                .AppendLine("animatorLayerMask=None")
                .AppendLine("fullSourceBodyMotionEnabled=True")
                .AppendLine("rightArmMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.RightArmMotionWeight))
                .AppendLine("rightForeArmMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.RightForeArmMotionWeight))
                .AppendLine("rightHandMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.RightHandMotionWeight))
                .AppendLine("fingerMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.FingerMotionWeight))
                .AppendLine("sprayRotationFollowWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.SprayRotationFollowWeight))
                .AppendLine("sprayFollowsAnimatedRightHand=True");
            WriteText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[HoloSprayIdleLocomotion] Applied and saved from a clean scene.");
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            HoloSprayRightHandFollowBehaviour carry = RequireCarry(target);
            AnimatorController controller = RequireAnimatorController(animator, TargetName);
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller), "controller path");
            if (animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("HoloSpray_Idle Animator settings are invalid.");

            RequireFloatParameter(controller,
                HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                HoloSprayIdleLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            if (controller.layers.Length != 1)
                throw new InvalidOperationException("Controller must have exactly one layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f) || layer.avatarMask != null)
                throw new InvalidOperationException(
                    "The complete original source animation must pass through the base layer.");
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Default state is missing.");
            if (state.name != StateName || state.writeDefaultValues ||
                !Mathf.Approximately(state.speed, 1f))
                throw new InvalidOperationException("Locomotion state settings are invalid.");
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("Root 2D Blend Tree is missing.");
            RequireRootTree(root);
            if (state.behaviours.OfType<HoloSprayIdleLocomotionCycleBehaviour>().Count() != 1)
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
                sources.Diagonal,
                root.children[4].motion as BlendTree,
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [sources.Forward] = forward,
                    [sources.Sidestep] = sidestep
                },
                "WalkDiagonal");
            RequireBaselineHashes("source_asset_hashes.txt", ComputeAssetHash);
            RequireBaselineHashes("carry_asset_hashes.txt", ComputeAssetHash);
            RequireBaselineHashes("source_object_hashes.txt",
                name => HierarchyHash(FindUnique(scene, name).transform));
            RequireEqual(ReadText("target_baseline.txt"), TargetBaselineText(target.transform),
                "target baseline");
            RequireEqual(ReadText("carry_baseline.txt"), CarryBaselineText(carry),
                "carry baseline");
            RequireScenePatch(controller, animator);

            var report = new StringBuilder()
                .AppendLine("HoloSpray_Idle locomotion read-only inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("controllerPath=" + ControllerPath)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=" + root.children.Length)
                .AppendLine("motionOrder=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("sourceClipCurvesPreserved=True")
                .AppendLine("sourceClipEventsPreserved=True")
                .AppendLine("sourceClipLoopSettingsPreserved=True")
                .AppendLine("diagonalSourceTreePreserved=True")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("currentCarryProfileChanged=False")
                .AppendLine("holoSprayAppearanceChanged=False")
                .AppendLine("fullSourceBodyMotionEnabled=True")
                .AppendLine("animatedRightArmOffsetEnabled=True")
                .AppendLine("fingerGripLocked=True")
                .AppendLine("sprayFollowsAnimatedRightHand=True")
                .AppendLine("scenePatchValid=True")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[HoloSprayIdleLocomotion] Inspection passed.");
        }

        internal static void EnterReview()
        {
            RequireEditMode();
            Inspect();
            EditorApplication.EnterPlaymode();
        }

        internal static void StopReview()
        {
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(RequireScene(), TargetName);
        }

        internal static RuntimePoseMetrics MeasureRuntimePose()
        {
            GameObject target = RequireRuntimeTarget();
            HoloSprayRightHandFollowBehaviour carry = RequireCarry(target);
            Transform holder = carry.SprayHolder != null
                ? carry.SprayHolder.transform
                : throw new InvalidOperationException("Runtime HoloSpray holder is missing.");
            Transform upperArm = RequireDescendant(target.transform, "RightArm");
            Transform foreArm = RequireDescendant(target.transform, "RightForeArm");
            Transform hand = RequireDescendant(target.transform, "RightHand");
            return new RuntimePoseMetrics(
                Vector3.Distance(holder.position, carry.ExpectedWorldPosition()),
                Quaternion.Angle(holder.rotation, carry.ExpectedWorldRotation()),
                Vector3.Angle(holder.up, target.transform.up),
                Vector3.Angle(holder.forward, target.transform.forward),
                RightPalmToPlayerLeftAngle(target.transform),
                RightWristStraightness(target.transform),
                Vector3.Angle((hand.position - upperArm.position).normalized,
                    target.transform.forward.normalized),
                Vector3.Angle((foreArm.position - upperArm.position).normalized,
                    (hand.position - foreArm.position).normalized),
                PoseDeviation(carry.Profile, target.transform, upperArm),
                PoseDeviation(carry.Profile, target.transform, foreArm),
                PoseDeviation(carry.Profile, target.transform, hand),
                MaximumFingerDeviation(carry.Profile, target.transform),
                RequireDescendant(target.transform, "Spine").localRotation,
                RequireDescendant(target.transform, "Head").localRotation,
                RequireDescendant(target.transform, "LeftArm").localRotation,
                RequireDescendant(target.transform, "RightShoulder").localRotation);
        }

        internal static Texture2D CaptureRuntimePanel(bool gripCloseup)
        {
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            HoloSprayRightHandFollowBehaviour carry = RequireCarry(target);
            Bounds bounds = gripCloseup
                ? GripBounds(target.transform, carry)
                : FullBounds(target.transform, carry);
            Vector3 direction = (
                target.transform.forward + target.transform.right * 0.72f).normalized;
            var cameraObject = new GameObject("HoloSprayLocomotion_ReadOnlyCamera");
            var lightObject = new GameObject("HoloSprayLocomotion_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.magnitude * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    target.transform.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
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

        internal static void ComposeRuntimeReview(
            IReadOnlyList<Texture2D> fullPanels,
            IReadOnlyList<Texture2D> gripPanels,
            string destination)
        {
            if (fullPanels.Count != 12 || gripPanels.Count != 6)
                throw new InvalidOperationException("Expected 12 full panels and 6 grip panels.");
            var composite = new Texture2D(3072, 1536, TextureFormat.RGB24, false);
            try
            {
                for (int i = 0; i < fullPanels.Count; i++)
                {
                    int column = i % 6;
                    int row = i < 6 ? 2 : 1;
                    composite.SetPixels(column * 512, row * 512, 512, 512,
                        fullPanels[i].GetPixels());
                }
                for (int i = 0; i < gripPanels.Count; i++)
                    composite.SetPixels(i * 512, 0, 512, 512, gripPanels[i].GetPixels());
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        internal static void WriteRuntimeReport(string fileName, string contents)
        {
            WriteText(fileName, contents);
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
            if (diagonal.blendType != BlendTreeType.Simple1D ||
                diagonal.children.Length != 2)
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
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = HoloSprayIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
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
                blendParameter = HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY = HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter,
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
            state.AddStateMachineBehaviour<HoloSprayIdleLocomotionCycleBehaviour>();
            stateMachine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(tree);
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
                tree.blendParameter != HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter ||
                tree.blendParameterY != HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter ||
                tree.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Root 2D Blend Tree is invalid.");
            string[] paths =
            {
                IdleClipPath, ForwardClipPath, BackwardClipPath,
                SidestepClipPath, ControllerPath, RunClipPath
            };
            ChildMotion[] children = tree.children;
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

        private static void RequireScenePatch(
            AnimatorController controller, Animator animator)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                controller, out string controllerGuid, out long controllerLocalId);
            Component sourceAnimator = PrefabUtility.GetCorrespondingObjectFromSource(animator);
            if (sourceAnimator == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    sourceAnimator, out string prefabGuid, out long sourceAnimatorLocalId))
                throw new InvalidOperationException("Source Animator identity is unavailable.");
            string sceneText = File.ReadAllText(Absolute(ScenePath), Encoding.UTF8);
            const string marker = "      value: HoloSpray_Idle";
            int markerIndex = sceneText.IndexOf(marker, StringComparison.Ordinal);
            int start = markerIndex >= 0
                ? sceneText.LastIndexOf("--- !u!1001 &", markerIndex, StringComparison.Ordinal)
                : -1;
            int end = markerIndex >= 0
                ? sceneText.IndexOf("\n--- !u!", markerIndex + marker.Length,
                    StringComparison.Ordinal)
                : -1;
            if (start < 0)
                throw new InvalidOperationException("HoloSpray_Idle prefab block is missing.");
            string block = end >= 0
                ? sceneText.Substring(start, end - start)
                : sceneText.Substring(start);
            string target = "target: {fileID: " + sourceAnimatorLocalId + ", guid: " +
                prefabGuid + ", type: 3}";
            string controllerReference = "objectReference: {fileID: " + controllerLocalId +
                ", guid: " + controllerGuid + ", type: 2}";
            if (!block.Contains(target) || !block.Contains("propertyPath: m_Controller") ||
                !block.Contains(controllerReference) ||
                !block.Contains("propertyPath: m_CullingMode"))
                throw new InvalidOperationException("Selective Animator scene patch is absent.");
        }

        private static string TargetBaselineText(Transform target)
        {
            var result = new StringBuilder();
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name != "HoloSpray_Prop" &&
                    !HasAncestorNamed(item, target, "HoloSpray_Prop"))
                .OrderBy(item => TransformPath(item, target), StringComparer.Ordinal))
                AppendTransform(result, item, target);
            return result.ToString();
        }

        private static string CarryBaselineText(HoloSprayRightHandFollowBehaviour carry)
        {
            var result = new StringBuilder()
                .AppendLine("profile=" + AssetDatabase.GetAssetPath(carry.Profile))
                .AppendLine("profileHash=" + ComputeAssetHash(CarryProfilePath));
            if (carry.SprayHolder != null)
                AppendTransform(result, carry.SprayHolder.transform, carry.transform);
            if (carry.SprayModel != null)
                AppendTransform(result, carry.SprayModel, carry.transform);
            return result.ToString();
        }

        private static void AppendTransform(
            StringBuilder result, Transform item, Transform root)
        {
            result.Append(TransformPath(item, root)).Append('|')
                .Append(Vec(item.localPosition)).Append('|')
                .Append(Quat(item.localRotation)).Append('|')
                .AppendLine(Vec(item.localScale));
        }

        private static string HierarchyHash(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => TransformPath(item, root), StringComparer.Ordinal))
                AppendTransform(text, item, root);
            return Sha256(text.ToString());
        }

        private static bool HasAncestorNamed(Transform item, Transform root, string name)
        {
            Transform current = item.parent;
            while (current != null && current != root)
            {
                if (current.name == name) return true;
                current = current.parent;
            }
            return false;
        }

        private static string[] CarryAssetPaths()
        {
            return new[] { CarryProfilePath, ModelPath, MaterialPath }
                .Concat(TexturePaths).ToArray();
        }

        private static float PoseDeviation(
            HoloSprayIdleCarryProfile profile, Transform targetRoot, Transform bone)
        {
            string path = TransformPath(bone, targetRoot);
            HoloSprayBoneRotation pose = profile.RightArmPose
                .Single(item => item.Path == path);
            return Quaternion.Angle(pose.LocalRotation, bone.localRotation);
        }

        private static float MaximumFingerDeviation(
            HoloSprayIdleCarryProfile profile, Transform root)
        {
            float maximum = 0f;
            foreach (HoloSprayBoneRotation pose in profile.RightArmPose)
            {
                if (pose.Path.IndexOf("/RightHand/", StringComparison.Ordinal) < 0) continue;
                Transform bone = root.Find(pose.Path) ??
                    throw new MissingReferenceException("Finger pose path is missing: " + pose.Path);
                maximum = Mathf.Max(maximum,
                    Quaternion.Angle(pose.LocalRotation, bone.localRotation));
            }
            return maximum;
        }

        private static float RightPalmToPlayerLeftAngle(Transform root)
        {
            Transform hand = RequireDescendant(root, "RightHand");
            Transform index = RequireDescendant(hand, "RightIndexProximal");
            Transform middle = RequireDescendant(hand, "RightMiddleProximal");
            Transform little = RequireDescendant(hand, "RightLittleProximal");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 finger = (middle.position - hand.position).normalized;
            return Vector3.Angle(Vector3.Cross(width, finger).normalized, -root.right);
        }

        private static float RightWristStraightness(Transform root)
        {
            Transform foreArm = RequireDescendant(root, "RightForeArm");
            Transform hand = RequireDescendant(root, "RightHand");
            Transform middle = RequireDescendant(hand, "RightMiddleProximal");
            return Vector3.Angle(
                (hand.position - foreArm.position).normalized,
                (middle.position - hand.position).normalized);
        }

        private static Bounds FullBounds(
            Transform target, HoloSprayRightHandFollowBehaviour carry)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("HoloSpray_Idle has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            foreach (Renderer renderer in carry.SprayHolder.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds GripBounds(
            Transform target, HoloSprayRightHandFollowBehaviour carry)
        {
            Transform upper = RequireDescendant(target, "RightArm");
            Transform fore = RequireDescendant(target, "RightForeArm");
            Transform hand = RequireDescendant(target, "RightHand");
            Bounds bounds = new Bounds(upper.position, Vector3.zero);
            bounds.Encapsulate(fore.position);
            bounds.Encapsulate(hand.position);
            foreach (Renderer renderer in carry.SprayHolder.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.13f);
            return bounds;
        }

        private static void RequireFloatParameter(
            AnimatorController controller, string name, float expected)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expected))
                throw new InvalidOperationException("Invalid float parameter: " + name);
        }

        private static void RequireBaselineHashes(
            string fileName, Func<string, string> actual)
        {
            foreach (string line in ReadText(fileName)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.LastIndexOf('|');
                if (separator <= 0)
                    throw new InvalidOperationException("Invalid hash baseline: " + fileName);
                string key = line.Substring(0, separator);
                RequireEqual(line.Substring(separator + 1), actual(key), key);
            }
        }

        private static void RequireHashes(IReadOnlyDictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, ComputeAssetHash(item.Key), item.Key);
        }

        private static void RequireSourceObjects(
            IReadOnlyDictionary<string, string> hashes, Scene scene)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value,
                    HierarchyHash(FindUnique(scene, item.Key).transform), item.Key);
        }

        private static StringBuilder DescribeMotion(Motion motion, string key)
        {
            var result = new StringBuilder()
                .AppendLine(key + "Type=" + motion.GetType().Name)
                .AppendLine(key + "Name=" + motion.name)
                .AppendLine(key + "Path=" + AssetDatabase.GetAssetPath(motion));
            if (motion is AnimationClip clip)
                result.AppendLine(key + "Length=" + F(clip.length))
                    .AppendLine(key + "FrameRate=" + F(clip.frameRate))
                    .AppendLine(key + "Looping=" + clip.isLooping)
                    .AppendLine(key + "Events=" +
                        AnimationUtility.GetAnimationEvents(clip).Length)
                    .AppendLine(key + "FloatCurveCount=" +
                        AnimationUtility.GetCurveBindings(clip).Length);
            else if (motion is BlendTree tree)
                result.AppendLine(key + "BlendType=" + tree.blendType)
                    .AppendLine(key + "ChildCount=" + tree.children.Length)
                    .AppendLine(key + "BlendParameter=" + tree.blendParameter);
            return result;
        }

        private static Animator RequireAnimator(GameObject root)
        {
            return root.GetComponent<Animator>() ??
                throw new MissingReferenceException(root.name + " Animator is missing.");
        }

        private static AnimatorController RequireAnimatorController(
            Animator animator, string label)
        {
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(label + " does not use an AnimatorController.");
        }

        private static HoloSprayRightHandFollowBehaviour RequireCarry(GameObject target)
        {
            HoloSprayRightHandFollowBehaviour carry =
                target.GetComponent<HoloSprayRightHandFollowBehaviour>();
            if (carry == null || carry.Profile == null)
                throw new MissingReferenceException("HoloSpray right-hand follow is missing.");
            carry.RefreshPreview();
            return carry;
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + " below " + root.name + "; found " +
                    matches.Length + ".");
            return matches[0];
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("HoloSpray locomotion operation requires Edit Mode.");
        }

        private static void EnsureAssetFolder()
        {
            string[] segments = AssetFolder.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static string TransformPath(Transform item, Transform root)
        {
            return AnimationUtility.CalculateTransformPath(item, root);
        }

        private static string ComputeAssetHash(string assetPath)
        {
            string path = Absolute(assetPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Asset file is missing.", path);
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty);
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                value, out string guid, out long localId)
                ? guid + ":" + localId
                : value.GetType().FullName + ":" + value.name;
        }

        private static void WriteText(string fileName, string contents)
        {
            string folder = Absolute(OutputFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllText(
                Path.Combine(folder, fileName), contents, new UTF8Encoding(false));
        }

        private static void WriteLines(string fileName, IEnumerable<string> lines)
        {
            WriteText(fileName,
                string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }

        private static string ReadText(string fileName)
        {
            string path = Path.Combine(Absolute(OutputFolder), fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException("Baseline is missing.", path);
            return File.ReadAllText(path, Encoding.UTF8);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    label + " differs. expected=" + expected + " actual=" + actual);
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
            return F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);
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

        internal readonly struct RuntimePoseMetrics
        {
            internal RuntimePoseMetrics(
                float followPosition,
                float followRotation,
                float canUp,
                float canFront,
                float palmLeft,
                float wrist,
                float armForward,
                float elbowBend,
                float armDeviation,
                float foreArmDeviation,
                float handDeviation,
                float fingerDeviation,
                Quaternion spine,
                Quaternion head,
                Quaternion leftArm,
                Quaternion rightShoulder)
            {
                FollowPosition = followPosition;
                FollowRotation = followRotation;
                CanUp = canUp;
                CanFront = canFront;
                PalmLeft = palmLeft;
                Wrist = wrist;
                ArmForward = armForward;
                ElbowBend = elbowBend;
                ArmDeviation = armDeviation;
                ForeArmDeviation = foreArmDeviation;
                HandDeviation = handDeviation;
                FingerDeviation = fingerDeviation;
                Spine = spine;
                Head = head;
                LeftArm = leftArm;
                RightShoulder = rightShoulder;
            }

            internal float FollowPosition { get; }
            internal float FollowRotation { get; }
            internal float CanUp { get; }
            internal float CanFront { get; }
            internal float PalmLeft { get; }
            internal float Wrist { get; }
            internal float ArmForward { get; }
            internal float ElbowBend { get; }
            internal float ArmDeviation { get; }
            internal float ForeArmDeviation { get; }
            internal float HandDeviation { get; }
            internal float FingerDeviation { get; }
            internal Quaternion Spine { get; }
            internal Quaternion Head { get; }
            internal Quaternion LeftArm { get; }
            internal Quaternion RightShoulder { get; }
        }
    }
}
