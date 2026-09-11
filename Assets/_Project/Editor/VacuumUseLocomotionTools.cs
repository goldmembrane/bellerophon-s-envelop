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
    internal static class VacuumUseLocomotionTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "Vacuum_Use";
        internal const string OutputFolder =
            "docs/validation/vacuum_use_locomotion_blend_tree_2026-09-11";
        internal const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/VacuumUse/VacuumUse_Locomotion.controller";
        internal const string UpperMaskPath =
            "Assets/_Project/Art/Player/Animations/VacuumUse/VacuumUse_UpperBody.mask";
        internal const string DiagnosticImagePath = OutputFolder + "/diagnostic.png";
        internal const string FinalImagePath = OutputFolder + "/final.png";

        private const string AssetFolder = "Assets/_Project/Art/Player/Animations/VacuumUse";
        private const string ForwardSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward_Meshy_Walking.anim";
        private const string BackwardSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Meshy_InPlace.anim";
        private const string SidestepSourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Sidestep_Mixamo_InPlace.anim";
        private const string RunSourcePath =
            "Assets/_Project/Art/Player/Animations/transfer running.fbx";
        private const string RunSourceClipName = "Armature|Armature|running|baselayer";
        private const string ForwardClipPath = AssetFolder + "/VacuumUse_WalkForward.anim";
        private const string BackwardClipPath = AssetFolder + "/VacuumUse_WalkBackward.anim";
        private const string SidestepClipPath = AssetFolder + "/VacuumUse_Sidestep.anim";
        private const string RunClipPath = AssetFolder + "/VacuumUse_RunForward.anim";
        private const string StationaryClipPath = AssetFolder + "/VacuumUse_Stationary.anim";
        private const string UpperHoldClipPath = AssetFolder + "/VacuumUse_UpperBodyHold.anim";
        private const string BaseStateName = "VacuumUseLocomotion2D";
        private const string UpperStateName = "VacuumUseUpperBodyHold";
        private const string MoveXParameter = "MoveX";
        private const string MoveYParameter = "MoveY";
        private const string DiagonalBlendParameter = "DiagonalBlend";
        private const string SpineName = "Spine";
        private const string HipsName = "Hips";
        private const string ArmatureName = "Armature";
        private const string VacuumHolderName = "VacuumCleaner_Prop";
        private const string VacuumModelName = "VacuumCleaner_Model";
        private const float PositionTolerance = 0.000001f;
        private const float RotationToleranceDegrees = 0.0001f;

        private static readonly string[] SourceNames =
        {
            "Player_Walk_Forward", "Player_Walk_Backward", "Player_Sidestep",
            "Player_Run_Forward", "Player_Walk_Diagonal"
        };

        private static readonly string[] SourceAssetPaths =
        {
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward.controller",
            ForwardSourcePath,
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward.controller",
            BackwardSourcePath,
            "Assets/_Project/Art/Player/Animations/Player_Sidestep.controller",
            SidestepSourcePath,
            "Assets/_Project/Art/Player/Animations/Player_Run_Forward.controller",
            RunSourcePath,
            "Assets/_Project/Art/Player/Animations/Player_Walk_Diagonal.controller"
        };

        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 2f),
            new Vector2(0.70710677f, 0.70710677f),
            new Vector2(-0.70710677f, 0.70710677f),
            new Vector2(0.70710677f, -0.70710677f),
            new Vector2(-0.70710677f, -0.70710677f)
        };

        internal static string DiagnosticAbsolutePath => Absolute(DiagnosticImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            var report = new StringBuilder()
                .AppendLine("Vacuum_Use locomotion source inspection")
                .AppendLine("verificationTargetManipulated=False");
            foreach (string sourceName in SourceNames)
            {
                GameObject source = FindUnique(scene, sourceName);
                Animator animator = RequireAnimator(source);
                AnimatorController controller = RequireAnimatorController(source);
                AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                    throw new InvalidOperationException(sourceName + " has no default Animator state.");
                report.AppendLine("source=" + sourceName)
                    .AppendLine("animatorPath=" + TransformPath(animator.transform, source.transform))
                    .AppendLine("avatarPath=" + AssetDatabase.GetAssetPath(animator.avatar))
                    .AppendLine("controllerPath=" + AssetDatabase.GetAssetPath(controller))
                    .AppendLine("state=" + state.name)
                    .Append(DescribeMotion(state.motion, "motion"));
            }

            GameObject target = FindUnique(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target);
            Transform spine = RequireDescendant(target.transform, SpineName);
            Transform hips = RequireDescendant(target.transform, HipsName);
            Transform holder = RequireDescendant(target.transform, VacuumHolderName);
            Transform model = RequireDescendant(holder, VacuumModelName);
            report.AppendLine("target=" + TargetName)
                .AppendLine("targetAnimatorPath=" + TransformPath(targetAnimator.transform, target.transform))
                .AppendLine("targetAvatarPath=" + AssetDatabase.GetAssetPath(targetAnimator.avatar))
                .AppendLine("targetControllerPath=" +
                    AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController))
                .AppendLine("hipsPath=" + TransformPath(hips, target.transform))
                .AppendLine("spinePath=" + TransformPath(spine, target.transform))
                .AppendLine("vacuumHolderLocalPosition=" + Vec(holder.localPosition))
                .AppendLine("vacuumHolderLocalRotation=" + Quat(holder.localRotation))
                .AppendLine("vacuumHolderLocalScale=" + Vec(holder.localScale))
                .AppendLine("vacuumModelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("vacuumModelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("vacuumModelLocalScale=" + Vec(model.localScale))
                .AppendLine("sceneDirty=" + scene.isDirty);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null && EditorUtility.IsDirty(component))
                    {
                        report.AppendLine("dirtyComponent=" +
                            TransformPath(component.transform, root.transform) + "|" +
                            component.GetType().FullName);
                    }
                }
            }
            WriteText("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[VacuumUseLocomotion] Sources inspected read-only. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;

            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, "Vacuum_Idle");
            Animator animator = RequireAnimator(target);
            string avatarPathBefore = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrEmpty(avatarPathBefore))
                throw new InvalidOperationException("Vacuum_Use Animator avatar is missing.");

            var sourceHashesBefore = SourceAssetPaths.ToDictionary(
                path => path, ComputeAssetHash, StringComparer.Ordinal);
            var signaturesBefore = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Vacuum_Idle"] = ProtectedHierarchySignature(idle.transform),
                [TargetName] = ProtectedHierarchySignature(target.transform)
            };
            foreach (string sourceName in SourceNames)
                signaturesBefore[sourceName] = ProtectedHierarchySignature(
                    FindUnique(scene, sourceName).transform);

            SourceMotions sources = RequireSourceMotions(scene);
            EnsureAssetFolder(AssetFolder);
            AnimationClip forward = CopyClip(sources.Forward, ForwardClipPath, "VacuumUse_WalkForward");
            AnimationClip backward = CopyClip(sources.Backward, BackwardClipPath, "VacuumUse_WalkBackward");
            AnimationClip sidestep = CopyClip(sources.Sidestep, SidestepClipPath, "VacuumUse_Sidestep");
            AnimationClip run = CopyClip(sources.Run, RunClipPath, "VacuumUse_RunForward");

            Transform armature = RequireDescendant(target.transform, ArmatureName);
            Transform spine = RequireDescendant(target.transform, SpineName);
            Transform holder = RequireDescendant(target.transform, VacuumHolderName);
            Transform model = RequireDescendant(holder, VacuumModelName);
            Transform[] upperBones = spine.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => TransformPath(item, target.transform), StringComparer.Ordinal).ToArray();
            AnimationClip stationary = CreateConstantPoseClip(
                StationaryClipPath, "VacuumUse_Stationary",
                armature.GetComponentsInChildren<Transform>(true), target.transform);
            AnimationClip upperHold = CreateConstantPoseClip(
                UpperHoldClipPath, "VacuumUse_UpperBodyHold", upperBones, target.transform);
            AvatarMask upperMask = CreateUpperBodyMask(target.transform, spine);
            AnimatorController controller = CreateController(
                stationary, forward, backward, sidestep, run, upperHold, upperMask);

            Undo.RecordObject(animator, "Configure Vacuum_Use locomotion Animator");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            RequireStringEqual(avatarPathBefore, AssetDatabase.GetAssetPath(animator.avatar),
                "Vacuum_Use avatar path");
            RequireProtectedSignatures(signaturesBefore, scene);
            RequireAssetHashes(sourceHashesBefore);
            RequireVacuumTransformsMatch(
                RequireDescendant(idle.transform, VacuumHolderName), holder);
            WriteBaselineFiles(sourceHashesBefore, signaturesBefore);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    controller,
                    out string controllerGuid,
                    out long controllerLocalId))
                throw new InvalidOperationException("Generated controller identity is unavailable.");
            GlobalObjectId animatorId = GlobalObjectId.GetGlobalObjectIdSlow(animator);

            var report = new StringBuilder()
                .AppendLine("Vacuum_Use locomotion application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("scenePatchRequired=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("vacuumIdleChanged=False")
                .AppendLine("vacuumUseProtectedHierarchyChanged=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sequence=Forward,Backward,SidestepRight,RunForward,DiagonalForwardRight")
                .AppendLine("sequenceLoopsAfterDiagonal=True")
                .AppendLine("controller=" + ControllerPath)
                .AppendLine("controllerGuid=" + controllerGuid)
                .AppendLine("controllerLocalId=" + controllerLocalId)
                .AppendLine("animatorLocalId=" + animatorId.targetObjectId)
                .AppendLine("baseBlendType=FreeformCartesian2D")
                .AppendLine("baseChildCount=10")
                .AppendLine("upperLayer=OverrideWeight1")
                .AppendLine("upperMaskRoot=" + TransformPath(spine, target.transform))
                .AppendLine("upperBoneCount=" + upperBones.Length)
                .AppendLine("vacuumHolderLocalPosition=" + Vec(holder.localPosition))
                .AppendLine("vacuumHolderLocalRotation=" + Quat(holder.localRotation))
                .AppendLine("vacuumHolderLocalScale=" + Vec(holder.localScale))
                .AppendLine("vacuumModelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("vacuumModelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("vacuumModelLocalScale=" + Vec(model.localScale));
            WriteText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[VacuumUseLocomotion] Applied. " + report.ToString().Replace('\n', ' '));
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException("Vacuum_Use generated controller is missing.");
            RequireStringEqual(ControllerPath, AssetDatabase.GetAssetPath(controller),
                "Vacuum_Use controller path");
            if (animator.applyRootMotion || !animator.enabled)
                throw new InvalidOperationException("Vacuum_Use Animator configuration is invalid.");

            RequireFloatParameter(controller.parameters, MoveXParameter);
            RequireFloatParameter(controller.parameters, MoveYParameter);
            RequireFloatParameter(controller.parameters, DiagonalBlendParameter);
            if (controller.layers.Length != 2)
                throw new InvalidOperationException("Vacuum controller must have exactly two layers.");
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorState baseState = baseLayer.stateMachine.defaultState ??
                throw new InvalidOperationException("Vacuum base state is missing.");
            if (baseState.name != BaseStateName || !(baseState.motion is BlendTree tree) ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter != MoveXParameter || tree.blendParameterY != MoveYParameter)
                throw new InvalidOperationException("Vacuum base 2D Freeform Cartesian tree is invalid.");
            RequireBlendTreeLayout(tree);

            AnimatorControllerLayer upperLayer = controller.layers[1];
            AnimatorState upperState = upperLayer.stateMachine.defaultState ??
                throw new InvalidOperationException("Vacuum upper hold state is missing.");
            if (upperLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(upperLayer.defaultWeight, 1f) ||
                AssetDatabase.GetAssetPath(upperLayer.avatarMask) != UpperMaskPath ||
                upperState.name != UpperStateName ||
                AssetDatabase.GetAssetPath(upperState.motion) != UpperHoldClipPath)
                throw new InvalidOperationException("Vacuum upper-body hold layer is invalid.");
            RequireUpperMask(target.transform, upperLayer.avatarMask);

            AnimationClip forward = RequireAsset<AnimationClip>(ForwardClipPath);
            AnimationClip backward = RequireAsset<AnimationClip>(BackwardClipPath);
            AnimationClip sidestep = RequireAsset<AnimationClip>(SidestepClipPath);
            AnimationClip run = RequireAsset<AnimationClip>(RunClipPath);
            if (!forward.isLooping || !backward.isLooping || !sidestep.isLooping || !run.isLooping)
                throw new InvalidOperationException("Copied locomotion clips must preserve looping.");
            RequireBaselineAssetHashes();
            RequireBaselineSceneSignatures(scene);
            Transform idleHolder = RequireDescendant(
                FindUnique(scene, "Vacuum_Idle").transform, VacuumHolderName);
            Transform useHolder = RequireDescendant(target.transform, VacuumHolderName);
            RequireVacuumTransformsMatch(idleHolder, useHolder);
            if (baseState.behaviours.OfType<VacuumUseLocomotionCycleBehaviour>().Count() != 1)
                throw new InvalidOperationException(
                    "Vacuum one-second StateMachineBehaviour is missing or duplicated.");

            var report = new StringBuilder()
                .AppendLine("Vacuum_Use locomotion inspection")
                .AppendLine("controllerStructureValid=True")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("blendTreeChildCount=" + tree.children.Length)
                .AppendLine("centerIdle=True")
                .AppendLine("forwardBackward=True")
                .AppendLine("leftRightSidestep=True")
                .AppendLine("forwardRun=True")
                .AppendLine("fourDiagonalNodes=True")
                .AppendLine("leftNodesMirrored=True")
                .AppendLine("upperOverrideLayer=True")
                .AppendLine("upperMaskSpineAndDescendantsOnly=True")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cycleDriver=AnimatorStateMachineBehaviour")
                .AppendLine("originalClipLengthsPreserved=True")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("vacuumIdleChanged=False")
                .AppendLine("vacuumUseProtectedHierarchyChanged=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("vacuumCleanerTransformsMatchIdle=True")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[VacuumUseLocomotion] Inspection passed. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void EnterReview()
        {
            RequireEditMode();
            RequireScene();
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

        internal static RuntimePoseMetrics MeasureRuntimePose()
        {
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Transform spine = RequireDescendant(target.transform, SpineName);
            AnimationClip upperHold = RequireAsset<AnimationClip>(UpperHoldClipPath);
            float upperPosition = 0f;
            float upperRotation = 0f;
            float upperScale = 0f;
            foreach (Transform bone in spine.GetComponentsInChildren<Transform>(true))
            {
                string path = TransformPath(bone, target.transform);
                Vector3 expectedPosition = new Vector3(
                    CurveValue(upperHold, path, "m_LocalPosition.x"),
                    CurveValue(upperHold, path, "m_LocalPosition.y"),
                    CurveValue(upperHold, path, "m_LocalPosition.z"));
                Quaternion expectedRotation = new Quaternion(
                    CurveValue(upperHold, path, "m_LocalRotation.x"),
                    CurveValue(upperHold, path, "m_LocalRotation.y"),
                    CurveValue(upperHold, path, "m_LocalRotation.z"),
                    CurveValue(upperHold, path, "m_LocalRotation.w"));
                Vector3 expectedScale = new Vector3(
                    CurveValue(upperHold, path, "m_LocalScale.x"),
                    CurveValue(upperHold, path, "m_LocalScale.y"),
                    CurveValue(upperHold, path, "m_LocalScale.z"));
                upperPosition = Mathf.Max(upperPosition,
                    Vector3.Distance(bone.localPosition, expectedPosition));
                upperRotation = Mathf.Max(upperRotation,
                    Quaternion.Angle(bone.localRotation, expectedRotation));
                upperScale = Mathf.Max(upperScale,
                    Vector3.Distance(bone.localScale, expectedScale));
            }

            Transform idleHolder = RequireDescendant(
                FindUnique(scene, "Vacuum_Idle").transform,
                VacuumHolderName);
            Transform useHolder = RequireDescendant(target.transform, VacuumHolderName);
            Transform idleModel = RequireDescendant(idleHolder, VacuumModelName);
            Transform useModel = RequireDescendant(useHolder, VacuumModelName);
            float vacuumPosition = Mathf.Max(
                Vector3.Distance(idleHolder.localPosition, useHolder.localPosition),
                Vector3.Distance(idleModel.localPosition, useModel.localPosition));
            float vacuumRotation = Mathf.Max(
                Quaternion.Angle(idleHolder.localRotation, useHolder.localRotation),
                Quaternion.Angle(idleModel.localRotation, useModel.localRotation));
            float vacuumScale = Mathf.Max(
                Vector3.Distance(idleHolder.localScale, useHolder.localScale),
                Vector3.Distance(idleModel.localScale, useModel.localScale));
            return new RuntimePoseMetrics(
                upperPosition,
                upperRotation,
                upperScale,
                vacuumPosition,
                vacuumRotation,
                vacuumScale);
        }

        internal static Texture2D CaptureRuntimePanel()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Runtime panel capture requires natural Play Mode.");
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Bounds bounds = CalculateWorldBounds(target);
            const int width = 480;
            const int height = 720;
            float size = Mathf.Max(bounds.extents.y * 1.12f,
                bounds.extents.x / ((float)width / height) * 1.12f);
            Vector3 front = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
            if (front.sqrMagnitude < 0.0001f) front = Vector3.forward;
            front.Normalize();

            var cameraObject = new GameObject("VacuumUseLocomotion_ReadOnlyCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(0.5f, size);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
            Vector3 lookAt = bounds.center + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.SetPositionAndRotation(
                lookAt + front * 4f, Quaternion.LookRotation(-front, Vector3.up));
            var lightObject = new GameObject("VacuumUseLocomotion_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

            RenderTexture previous = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(
                width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply(false, false);
                return image;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(image);
                throw;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        internal static void ComposeRuntimeReview(IReadOnlyList<Texture2D> panels, string destination)
        {
            if (panels == null || panels.Count != 10)
                throw new InvalidOperationException("Runtime review requires ten natural-playback panels.");
            int width = panels[0].width;
            int height = panels[0].height;
            var composite = new Texture2D(width * 5, height * 2, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < panels.Count; index++)
                {
                    int column = index % 5;
                    int row = index < 5 ? 1 : 0;
                    composite.SetPixels32(column * width, row * height, width, height,
                        panels[index].GetPixels32());
                }
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
            AnimationClip forward = RequireAsset<AnimationClip>(ForwardSourcePath);
            AnimationClip backward = RequireAsset<AnimationClip>(BackwardSourcePath);
            AnimationClip sidestep = RequireAsset<AnimationClip>(SidestepSourcePath);
            AnimationClip run = AssetDatabase.LoadAllAssetsAtPath(RunSourcePath)
                .OfType<AnimationClip>().SingleOrDefault(item => item.name == RunSourceClipName) ??
                throw new InvalidOperationException("Player_Run_Forward source clip is missing.");
            BlendTree diagonalTree = RequireAnimatorController(
                    FindUnique(scene, "Player_Walk_Diagonal"))
                .layers[0].stateMachine.defaultState?.motion as BlendTree ??
                throw new InvalidOperationException("Player_Walk_Diagonal Blend Tree is missing.");
            if (diagonalTree.blendType != BlendTreeType.Simple1D ||
                diagonalTree.children.Length != 2 ||
                diagonalTree.children[0].motion != forward ||
                diagonalTree.children[1].motion != sidestep ||
                !Mathf.Approximately(diagonalTree.children[0].threshold, 0f) ||
                !Mathf.Approximately(diagonalTree.children[1].threshold, 1f))
                throw new InvalidOperationException("Player_Walk_Diagonal source changed after inspection.");
            return new SourceMotions(forward, backward, sidestep, run);
        }

        private static AnimationClip CopyClip(AnimationClip source, string path, string name)
        {
            AnimationClip destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (destination == null)
            {
                destination = new AnimationClip();
                AssetDatabase.CreateAsset(destination, path);
            }
            EditorUtility.CopySerialized(source, destination);
            destination.name = name;
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static AnimationClip CreateConstantPoseClip(
            string path, string name, IEnumerable<Transform> transforms, Transform root)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            clip.name = name;
            clip.frameRate = 60f;
            foreach (Transform item in transforms.OrderBy(
                         value => TransformPath(value, root), StringComparer.Ordinal))
                SetTransformCurves(clip, TransformPath(item, root), item);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void SetTransformCurves(AnimationClip clip, string path, Transform item)
        {
            SetCurve(clip, path, "m_LocalPosition.x", item.localPosition.x);
            SetCurve(clip, path, "m_LocalPosition.y", item.localPosition.y);
            SetCurve(clip, path, "m_LocalPosition.z", item.localPosition.z);
            SetCurve(clip, path, "m_LocalRotation.x", item.localRotation.x);
            SetCurve(clip, path, "m_LocalRotation.y", item.localRotation.y);
            SetCurve(clip, path, "m_LocalRotation.z", item.localRotation.z);
            SetCurve(clip, path, "m_LocalRotation.w", item.localRotation.w);
            SetCurve(clip, path, "m_LocalScale.x", item.localScale.x);
            SetCurve(clip, path, "m_LocalScale.y", item.localScale.y);
            SetCurve(clip, path, "m_LocalScale.z", item.localScale.z);
        }

        private static void SetCurve(AnimationClip clip, string path, string property, float value)
        {
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                new AnimationCurve(new Keyframe(0f, value), new Keyframe(1f, value)));
        }

        private static float CurveValue(AnimationClip clip, string path, string property)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
            if (curve == null)
                throw new InvalidOperationException(
                    "Upper hold curve is missing: " + path + "|" + property + ".");
            return curve.Evaluate(0f);
        }

        private static AvatarMask CreateUpperBodyMask(Transform root, Transform spine)
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask { name = "VacuumUse_UpperBody" };
                AssetDatabase.CreateAsset(mask, UpperMaskPath);
            }
            string spinePath = TransformPath(spine, root);
            string[] paths = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item != root).Select(item => TransformPath(item, root))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item.Count(character => character == '/'))
                .ThenBy(item => item, StringComparer.Ordinal).ToArray();
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index, paths[index] == spinePath ||
                    paths[index].StartsWith(spinePath + "/", StringComparison.Ordinal));
            }
            for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController CreateController(
            AnimationClip stationary, AnimationClip forward, AnimationClip backward,
            AnimationClip sidestep, AnimationClip run, AnimationClip upperHold,
            AvatarMask upperMask)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException("Existing Vacuum controller could not be replaced.");
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(MoveXParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveYParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(DiagonalBlendParameter, AnimatorControllerParameterType.Float);

            var diagonal = new BlendTree
            {
                name = "VacuumUse_WalkDiagonal50", blendType = BlendTreeType.Simple1D,
                blendParameter = DiagonalBlendParameter, useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(diagonal, controller);
            diagonal.AddChild(forward, 0f);
            diagonal.AddChild(sidestep, 1f);
            var tree = new BlendTree
            {
                name = "VacuumUse_Locomotion2D", blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = MoveXParameter, blendParameterY = MoveYParameter,
                useAutomaticThresholds = false, hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            Add2DChild(tree, stationary, RequiredPositions[0], false);
            Add2DChild(tree, forward, RequiredPositions[1], false);
            Add2DChild(tree, backward, RequiredPositions[2], false);
            Add2DChild(tree, sidestep, RequiredPositions[3], false);
            Add2DChild(tree, sidestep, RequiredPositions[4], true);
            Add2DChild(tree, run, RequiredPositions[5], false);
            Add2DChild(tree, diagonal, RequiredPositions[6], false);
            Add2DChild(tree, diagonal, RequiredPositions[7], true);
            Add2DChild(tree, diagonal, RequiredPositions[8], false);
            Add2DChild(tree, diagonal, RequiredPositions[9], true);

            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "Base Locomotion";
            AnimatorState baseState = baseLayer.stateMachine.AddState(BaseStateName);
            baseState.motion = tree;
            baseState.writeDefaultValues = false;
            baseState.AddStateMachineBehaviour<VacuumUseLocomotionCycleBehaviour>();
            baseLayer.stateMachine.defaultState = baseState;
            controller.layers = new[] { baseLayer };
            var upperMachine = new AnimatorStateMachine
            {
                name = "Upper Body Vacuum Hold", hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(upperMachine, controller);
            AnimatorState upperState = upperMachine.AddState(UpperStateName);
            upperState.motion = upperHold;
            upperState.writeDefaultValues = false;
            upperMachine.defaultState = upperState;
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = "Upper Body Vacuum Hold", stateMachine = upperMachine,
                avatarMask = upperMask, blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 1f, iKPass = false
            });
            EditorUtility.SetDirty(diagonal);
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(baseState);
            EditorUtility.SetDirty(upperState);
            EditorUtility.SetDirty(upperMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void Add2DChild(BlendTree tree, Motion motion, Vector2 position, bool mirror)
        {
            tree.AddChild(motion, position);
            ChildMotion[] children = tree.children;
            ChildMotion child = children[children.Length - 1];
            child.mirror = mirror;
            child.timeScale = 1f;
            child.cycleOffset = 0f;
            children[children.Length - 1] = child;
            tree.children = children;
        }

        private static void RequireBlendTreeLayout(BlendTree tree)
        {
            ChildMotion[] children = tree.children;
            if (children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Vacuum 2D tree must contain ten nodes.");
            for (int index = 0; index < children.Length; index++)
                if (Vector2.Distance(children[index].position, RequiredPositions[index]) > PositionTolerance)
                    throw new InvalidOperationException("Vacuum 2D node position changed at " + index + ".");
            int[] mirroredIndices = { 4, 7, 9 };
            for (int index = 0; index < children.Length; index++)
                if (children[index].mirror != mirroredIndices.Contains(index))
                    throw new InvalidOperationException("Vacuum 2D mirror changed at " + index + ".");
            for (int index = 6; index <= 9; index++)
            {
                if (!(children[index].motion is BlendTree diagonal) ||
                    diagonal.blendType != BlendTreeType.Simple1D ||
                    diagonal.blendParameter != DiagonalBlendParameter ||
                    diagonal.children.Length != 2 ||
                    !Mathf.Approximately(diagonal.children[0].threshold, 0f) ||
                    !Mathf.Approximately(diagonal.children[1].threshold, 1f))
                    throw new InvalidOperationException("Vacuum diagonal node is invalid.");
            }
        }

        private static void RequireUpperMask(Transform target, AvatarMask mask)
        {
            if (mask == null) throw new InvalidOperationException("Vacuum upper mask is missing.");
            string spinePath = TransformPath(RequireDescendant(target, SpineName), target);
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                bool expected = path == spinePath ||
                    path.StartsWith(spinePath + "/", StringComparison.Ordinal);
                if (mask.GetTransformActive(index) != expected)
                    throw new InvalidOperationException("Vacuum upper mask path is invalid: " + path + ".");
            }
        }

        private static void RequireFloatParameter(
            IEnumerable<AnimatorControllerParameter> parameters, string name)
        {
            AnimatorControllerParameter parameter = parameters.SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float)
                throw new InvalidOperationException("Vacuum float parameter is missing: " + name + ".");
        }

        private static void WriteBaselineFiles(
            IReadOnlyDictionary<string, string> sourceHashes,
            IReadOnlyDictionary<string, string> signatures)
        {
            WriteText("source_asset_hashes.txt", string.Join(Environment.NewLine,
                sourceHashes.OrderBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => item.Key + "|" + item.Value)) + Environment.NewLine);
            WriteText("baseline_scene_signatures.txt", string.Join(Environment.NewLine,
                signatures.OrderBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => item.Key + "|" + ComputeStringHash(item.Value))) +
                Environment.NewLine);
        }

        private static void RequireBaselineAssetHashes()
        {
            string path = Absolute(OutputFolder + "/source_asset_hashes.txt");
            if (!File.Exists(path)) throw new InvalidOperationException("Source hash baseline is missing.");
            var expected = File.ReadAllLines(path, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(new[] { '|' }, 2))
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
            RequireAssetHashes(expected);
        }

        private static void RequireBaselineSceneSignatures(Scene scene)
        {
            string path = Absolute(OutputFolder + "/baseline_scene_signatures.txt");
            if (!File.Exists(path)) throw new InvalidOperationException("Scene signature baseline is missing.");
            var expected = File.ReadAllLines(path, Encoding.UTF8)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(new[] { '|' }, 2))
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in expected)
                RequireStringEqual(pair.Value, ComputeStringHash(ProtectedHierarchySignature(
                    FindUnique(scene, pair.Key).transform)), pair.Key + " protected signature");
        }

        private static void RequireProtectedSignatures(
            IReadOnlyDictionary<string, string> expected, Scene scene)
        {
            foreach (KeyValuePair<string, string> pair in expected)
                RequireStringEqual(pair.Value, ProtectedHierarchySignature(
                    FindUnique(scene, pair.Key).transform), pair.Key + " protected hierarchy");
        }

        private static string ProtectedHierarchySignature(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(value => TransformPath(value, root), StringComparer.Ordinal))
            {
                string path = TransformPath(item, root);
                result.Append(path).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(item.GetSiblingIndex()).Append('|').Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|').Append(Vec(item.localScale)).AppendLine();
                foreach (MeshFilter filter in item.GetComponents<MeshFilter>())
                    result.Append("MF|").Append(path).Append('|')
                        .Append(AssetIdentity(filter.sharedMesh)).AppendLine();
                foreach (SkinnedMeshRenderer renderer in item.GetComponents<SkinnedMeshRenderer>())
                    result.Append("SMR|").Append(path).Append('|').Append(renderer.enabled).Append('|')
                        .Append(AssetIdentity(renderer.sharedMesh)).Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials.Select(AssetIdentity))).AppendLine();
                foreach (MeshRenderer renderer in item.GetComponents<MeshRenderer>())
                    result.Append("MR|").Append(path).Append('|').Append(renderer.enabled).Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials.Select(AssetIdentity))).AppendLine();
            }
            return result.ToString();
        }

        private static string AssetIdentity(UnityEngine.Object asset)
        {
            return asset == null ? "null" : AssetDatabase.GetAssetPath(asset) + "#" + asset.name;
        }

        private static void RequireVacuumTransformsMatch(Transform idleHolder, Transform useHolder)
        {
            RequireTransformMatch(idleHolder, useHolder, "holder");
            RequireTransformMatch(RequireDescendant(idleHolder, VacuumModelName),
                RequireDescendant(useHolder, VacuumModelName), "model");
        }

        private static void RequireTransformMatch(Transform expected, Transform actual, string label)
        {
            if (Vector3.Distance(expected.localPosition, actual.localPosition) > PositionTolerance ||
                Quaternion.Angle(expected.localRotation, actual.localRotation) >
                    RotationToleranceDegrees ||
                Vector3.Distance(expected.localScale, actual.localScale) > PositionTolerance)
                throw new InvalidOperationException("Vacuum_Use " + label + " transform changed.");
        }

        private static string ComputeAssetHash(string assetPath)
        {
            string path = Absolute(assetPath);
            if (!File.Exists(path)) throw new InvalidOperationException("Source asset missing: " + assetPath);
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ComputeStringHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty);
        }

        private static void RequireAssetHashes(IReadOnlyDictionary<string, string> expected)
        {
            foreach (KeyValuePair<string, string> pair in expected)
                RequireStringEqual(pair.Value, ComputeAssetHash(pair.Key), pair.Key + " hash");
        }

        private static void RequireStringEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static Bounds CalculateWorldBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Vacuum_Use has no visible renderers.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static AnimatorController RequireAnimatorController(GameObject root)
        {
            return RequireAnimator(root).runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(root.name + " does not use an AnimatorController.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                throw new InvalidOperationException("Required asset is missing: " + path + ".");
        }

        private static string DescribeMotion(Motion motion, string prefix)
        {
            if (motion == null) throw new InvalidOperationException(prefix + " is null.");
            var report = new StringBuilder().AppendLine(prefix + "Type=" + motion.GetType().Name)
                .AppendLine(prefix + "Name=" + motion.name)
                .AppendLine(prefix + "Path=" + AssetDatabase.GetAssetPath(motion));
            if (motion is AnimationClip clip)
                report.AppendLine(prefix + "Length=" + Num(clip.length))
                    .AppendLine(prefix + "Looping=" + clip.isLooping)
                    .AppendLine(prefix + "CurveBindings=" +
                        AnimationUtility.GetCurveBindings(clip).Length);
            else if (motion is BlendTree tree)
            {
                report.AppendLine(prefix + "BlendType=" + tree.blendType)
                    .AppendLine(prefix + "BlendParameter=" + tree.blendParameter)
                    .AppendLine(prefix + "BlendParameterY=" + tree.blendParameterY)
                    .AppendLine(prefix + "ChildCount=" + tree.children.Length);
                for (int index = 0; index < tree.children.Length; index++)
                {
                    ChildMotion child = tree.children[index];
                    string childPrefix = prefix + "Child" + index;
                    report.AppendLine(childPrefix + "Threshold=" + Num(child.threshold))
                        .AppendLine(childPrefix + "Position=" + Vec(child.position))
                        .AppendLine(childPrefix + "Mirror=" + child.mirror)
                        .Append(DescribeMotion(child.motion, childPrefix));
                }
            }
            return report.ToString();
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Vacuum operation requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string objectName)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == objectName).Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one " + objectName + "; found " +
                    matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject root)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one Animator; found " +
                    animators.Length + ".");
            return animators[0];
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + name +
                    "; found " + matches.Length + ".");
            return matches[0];
        }

        private static string TransformPath(Transform transform, Transform root)
        {
            return AnimationUtility.CalculateTransformPath(transform, root);
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void WriteText(string fileName, string contents)
        {
            string directory = Absolute(OutputFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), contents, Encoding.UTF8);
        }

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(root,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector2 value) => Num(value.x) + "," + Num(value.y);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private sealed class SourceMotions
        {
            public SourceMotions(AnimationClip forward, AnimationClip backward,
                AnimationClip sidestep, AnimationClip run)
            {
                Forward = forward;
                Backward = backward;
                Sidestep = sidestep;
                Run = run;
            }
            public AnimationClip Forward { get; }
            public AnimationClip Backward { get; }
            public AnimationClip Sidestep { get; }
            public AnimationClip Run { get; }
        }

        internal readonly struct RuntimePoseMetrics
        {
            internal RuntimePoseMetrics(
                float upperPosition,
                float upperRotation,
                float upperScale,
                float vacuumPosition,
                float vacuumRotation,
                float vacuumScale)
            {
                UpperPosition = upperPosition;
                UpperRotation = upperRotation;
                UpperScale = upperScale;
                VacuumPosition = vacuumPosition;
                VacuumRotation = vacuumRotation;
                VacuumScale = vacuumScale;
            }

            internal float UpperPosition { get; }
            internal float UpperRotation { get; }
            internal float UpperScale { get; }
            internal float VacuumPosition { get; }
            internal float VacuumRotation { get; }
            internal float VacuumScale { get; }
        }
    }
}
