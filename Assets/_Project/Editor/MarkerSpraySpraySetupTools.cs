using System;
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
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class MarkerSpraySpraySetupTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string SourceTargetName = "MarkerSpray_Idle";
        internal const string TargetName = "MarkerSpray_Spray";
        internal const string SourceClipPath =
            "Assets/_Project/Animation/MarkerSpray/MarkerSprayIdle_Idle.anim";
        internal const string SourceControllerPath =
            "Assets/_Project/Animation/MarkerSpray/MarkerSprayIdle_Locomotion.controller";
        internal const string OutputClipPath =
            "Assets/_Project/Animation/MarkerSpray/MarkerSpraySpray_Idle.anim";
        internal const string OutputControllerPath =
            "Assets/_Project/Animation/MarkerSpray/MarkerSpraySpray_Idle.controller";
        internal const string FinalImagePath =
            "Assets/_Project/Animation/MarkerSpray/Review/marker_spray_spray_idle_final.png";
        internal const string ActualPlaybackFinalImagePath =
            "Assets/_Project/Animation/MarkerSpray/Review/marker_spray_spray_actual_playback_final.png";
        internal const string CarryFinalImagePath =
            "Assets/_Project/Animation/MarkerSpray/Review/marker_spray_spray_carry_final.png";

        private const string ExternalModelPath = "item model/marking spray.fbx";
        private const string ImportedModelPath =
            "Assets/_Project/Art/Items/MarkerSpray/MarkingSpray.fbx";
        private const string MaterialPath =
            "Assets/_Project/Art/Items/MarkerSpray/Materials/MarkingSpray.mat";
        private const string BaseColorPath =
            "Assets/_Project/Art/Items/MarkerSpray/Textures/base_color.jpg";
        private const string NormalPath =
            "Assets/_Project/Art/Items/MarkerSpray/Textures/normal.jpg";
        private const string MetallicSmoothnessPath =
            "Assets/_Project/Art/Items/MarkerSpray/Textures/MarkingSpray_MetallicSmoothness.png";
        private const string SourceCarryProfilePath =
            "Assets/_Project/Animation/MarkerSpray/MarkerSprayIdleCarryProfile.asset";
        private const string TargetCarryProfilePath =
            "Assets/_Project/Animation/MarkerSpray/MarkerSpraySprayCarryProfile.asset";

        private const string StateName = "MarkerSpraySprayIdleLoop";
        private const float PositionTolerance = 0.00001f;

        private static PlayableGraph actualPreviewGraph;
        private static AnimatorControllerPlayable actualPreviewPlayable;
        private static GameObject actualPreviewTarget;
        private static double actualPreviewLastTime;

        internal static string FinalAbsolutePath => Absolute(FinalImagePath);
        internal static string ActualPlaybackFinalAbsolutePath =>
            Absolute(ActualPlaybackFinalImagePath);
        internal static string CarryFinalAbsolutePath => Absolute(CarryFinalImagePath);

        internal static void InspectCarrySource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject source = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            MarkerSprayRightHandFollowBehaviour sourceCarry = RequireCarry(source);
            RequireEqual(SourceCarryProfilePath,
                AssetDatabase.GetAssetPath(sourceCarry.Profile), "source carry profile path");
            RequireEqual(ImportedModelPath,
                AssetDatabase.GetAssetPath(sourceCarry.Profile.SprayPrefab),
                "source marking spray prefab path");
            RequireEqual(Sha256File(ExternalModelPath), Sha256File(ImportedModelPath),
                "source/imported marking spray FBX hash");
            RequireAppearanceAssets(sourceCarry.Profile.SprayPrefab);
            if (target.GetComponent<MarkerSprayRightHandFollowBehaviour>() != null)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray already has a carry component before application.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySprayCarry] Source inspected read-only. sourceSha256=" +
                Sha256File(ExternalModelPath) + ", profile=" +
                ProfileSignature(sourceCarry.Profile) + ", rightArmBones=" +
                sourceCarry.Profile.RightArmPose.Length + ", sourcePoseBones=" +
                sourceCarry.Profile.SourceRightArmPose.Length + ".");
        }

        internal static void ApplyCarry()
        {
            RequireEditMode();
            StopActualPreview();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject source = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            MarkerSprayRightHandFollowBehaviour sourceCarry = RequireCarry(source);
            string sourceProfileHash = ComputeAssetHash(SourceCarryProfilePath);
            string sourceModelHash = Sha256File(ExternalModelPath);
            string sourceHierarchy = PersistentHierarchySignature(source.transform);

            MarkerSprayIdleCarryProfile targetProfile =
                AssetDatabase.LoadAssetAtPath<MarkerSprayIdleCarryProfile>(
                    TargetCarryProfilePath);
            if (targetProfile == null)
            {
                targetProfile = ScriptableObject.CreateInstance<MarkerSprayIdleCarryProfile>();
                EditorUtility.CopySerialized(sourceCarry.Profile, targetProfile);
                targetProfile.name = "MarkerSpraySprayCarryProfile";
                AssetDatabase.CreateAsset(targetProfile, TargetCarryProfilePath);
            }
            else
            {
                EditorUtility.CopySerialized(sourceCarry.Profile, targetProfile);
                targetProfile.name = "MarkerSpraySprayCarryProfile";
                EditorUtility.SetDirty(targetProfile);
            }

            MarkerSprayRightHandFollowBehaviour targetCarry =
                target.GetComponent<MarkerSprayRightHandFollowBehaviour>();
            if (targetCarry == null)
                targetCarry = Undo.AddComponent<MarkerSprayRightHandFollowBehaviour>(target);
            targetCarry.Configure(targetProfile);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetCarry);
            EditorUtility.SetDirty(targetCarry);
            AssetDatabase.SaveAssets();

            RequireEqual(sourceProfileHash, ComputeAssetHash(SourceCarryProfilePath),
                "source carry profile hash");
            RequireEqual(sourceModelHash, Sha256File(ExternalModelPath),
                "source marking spray FBX hash");
            RequireEqual(sourceHierarchy, PersistentHierarchySignature(source.transform),
                "MarkerSpray_Idle persistent hierarchy");
            RequireEqual(ProfileSignature(sourceCarry.Profile),
                ProfileSignature(targetProfile), "source/target carry profile");
            RequireAppearanceAssets(targetProfile.SprayPrefab);

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene could not be saved.");
            AssetDatabase.SaveAssets();
            StartActualPreview();
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySprayCarry] Exact MarkerSpray_Idle carry profile, model, " +
                "right-arm pose and right-hand follow applied. Preexisting scene dirty=" +
                sceneWasDirty + ".");
        }

        internal static void InspectCarry()
        {
            RequireEditMode();
            InspectActualPlayback();
            Scene scene = RequireScene();
            GameObject source = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            MarkerSprayRightHandFollowBehaviour sourceCarry = RequireCarry(source);
            MarkerSprayRightHandFollowBehaviour targetCarry = RequireCarry(target);
            RequireEqual(SourceCarryProfilePath,
                AssetDatabase.GetAssetPath(sourceCarry.Profile), "source carry profile path");
            RequireEqual(TargetCarryProfilePath,
                AssetDatabase.GetAssetPath(targetCarry.Profile), "target carry profile path");
            RequireEqual(ProfileSignature(sourceCarry.Profile),
                ProfileSignature(targetCarry.Profile), "source/target carry profile");
            RequireEqual(Sha256File(ExternalModelPath), Sha256File(ImportedModelPath),
                "source/imported marking spray FBX hash");
            RequireCurrentCarryPose(target, targetCarry);
            RequireAppearanceAssets(targetCarry.Profile.SprayPrefab);
            RequireVisibleCarryModel(targetCarry);
            if (!IsActualPreviewActive)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray Edit Mode idle preview is not active.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySprayCarry] Inspection passed. exactProfile=True, " +
                "singleVisibleModel=True, handPositionError=" +
                F(Vector3.Distance(targetCarry.SprayHolder.transform.position,
                    targetCarry.ExpectedWorldPosition())) + ", handRotationError=" +
                F(Quaternion.Angle(targetCarry.SprayHolder.transform.rotation,
                    targetCarry.ExpectedWorldRotation())) +
                ", editModePreviewActive=True.");
        }

        internal static void CaptureCarryFinal()
        {
            RequireEditMode();
            InspectCarry();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject source = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            Animator sourceAnimator = RequireAnimator(source);
            Animator targetAnimator = RequireAnimator(target);
            AnimationClip sourceClip = RequireSourceClip(
                RequireController(sourceAnimator, SourceTargetName));
            AnimatorController targetController = RequireController(targetAnimator, TargetName);
            MarkerSprayRightHandFollowBehaviour sourceCarry = RequireCarry(source);
            MarkerSprayRightHandFollowBehaviour targetCarry = RequireCarry(target);
            StopActualPreview();
            TransformSnapshot[] sourceSnapshot = CaptureTransformSnapshot(source.transform);
            TransformSnapshot[] targetSnapshot = CaptureTransformSnapshot(target.transform);
            var sourcePanels = new Texture2D[6];
            var targetPanels = new Texture2D[6];
            var targetBoundarySignatures = new string[2];
            try
            {
                for (int index = 0; index < 6; index++)
                {
                    float normalized = index < 5 ? index * 0.2f : 1f;
                    SampleClip(sourceAnimator, sourceClip, normalized * sourceClip.length);
                    sourceCarry.RefreshPreview();
                    SampleController(targetAnimator, targetController, normalized);
                    targetCarry.RefreshPreview();
                    RequireCurrentCarryPose(source, sourceCarry);
                    RequireCurrentCarryPose(target, targetCarry);
                    RequireVisibleCarryModel(sourceCarry);
                    RequireVisibleCarryModel(targetCarry);
                    RequireEqual(RelevantCarryPoseSignature(
                            source.transform, sourceClip, sourceCarry.Profile),
                        RelevantCarryPoseSignature(
                            target.transform, sourceClip, targetCarry.Profile),
                        "source/target evaluated rig pose at " + F(normalized));
                    if (index == 0)
                        targetBoundarySignatures[0] =
                            RelevantCarryPoseSignature(
                                target.transform, sourceClip, targetCarry.Profile);
                    if (index == 5)
                        targetBoundarySignatures[1] =
                            RelevantCarryPoseSignature(
                                target.transform, sourceClip, targetCarry.Profile);
                    sourcePanels[index] = CapturePanel(source.transform, true);
                    targetPanels[index] = CapturePanel(target.transform, true);
                }
                RequireEqual(targetBoundarySignatures[0], targetBoundarySignatures[1],
                    "MarkerSpray_Spray carry loop boundary pose");
                ComposeReview(sourcePanels, targetPanels, CarryFinalAbsolutePath);
            }
            finally
            {
                RestoreTransformSnapshot(sourceSnapshot);
                RestoreTransformSnapshot(targetSnapshot);
                foreach (Texture2D image in sourcePanels)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
                foreach (Texture2D image in targetPanels)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
                StartActualPreview();
            }
            AssetDatabase.ImportAsset(CarryFinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (scene.isDirty != sceneWasDirty)
                throw new InvalidOperationException(
                    "MarkerSpray carry direct capture changed the scene dirty state.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySprayCarry] Final direct comparison captured once. " +
                "Top=MarkerSpray_Idle source, bottom=MarkerSpray_Spray target, " +
                "phases=0,20,40,60,80,100 percent.");
        }

        internal static void InspectActualPlayback()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireController(animator, TargetName);
            RequireEqual(OutputControllerPath, AssetDatabase.GetAssetPath(controller),
                "actual target controller path");
            AnimatorState state = RequireSingleTargetState(controller);
            AnimationClip clip = state.motion as AnimationClip ??
                throw new InvalidOperationException(
                    "MarkerSpray_Spray actual target state has no AnimationClip.");
            RequireEqual(OutputClipPath, AssetDatabase.GetAssetPath(clip),
                "actual target clip path");
            if (!target.activeSelf || !target.activeInHierarchy)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray is inactive and cannot play its Animator.");
            if (!animator.enabled || animator.speed <= 0f)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray Animator is disabled or stopped.");
            if (animator.updateMode != AnimatorUpdateMode.Normal || animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray actual playback settings differ.");
            EditorCurveBinding[] transformBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(item => item.type == typeof(Transform)).ToArray();
            if (transformBindings.Length == 0)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray copied idle has no Transform curves.");
            string[] missingPaths = transformBindings.Select(item => item.path)
                .Distinct(StringComparer.Ordinal)
                .Where(path => target.transform.Find(path) == null).ToArray();
            if (missingPaths.Length != 0)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray is missing animated paths: " +
                    string.Join(", ", missingPaths));
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            int enabledRenderers = renderers.Count(item => item.enabled && item.gameObject.activeInHierarchy);
            if (enabledRenderers == 0)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray has no active renderer for direct review.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySpray] Actual target inspected read-only. activeSelf=" +
                target.activeSelf + ", activeInHierarchy=" + target.activeInHierarchy +
                ", animatorEnabled=" + animator.enabled + ", animatorSpeed=" +
                F(animator.speed) + ", controller=" + AssetDatabase.GetAssetPath(controller) +
                ", state=" + state.name + ", clip=" + AssetDatabase.GetAssetPath(clip) +
                ", clipLength=" + F(clip.length) + ", looping=" + clip.isLooping +
                ", avatarValid=" + (animator.avatar != null && animator.avatar.isValid) +
                ", transformCurves=" + transformBindings.Length +
                ", boundTransformPaths=" + transformBindings.Select(item => item.path)
                    .Distinct(StringComparer.Ordinal).Count() +
                ", enabledRenderers=" + enabledRenderers +
                ", editorPlaying=" + EditorApplication.isPlaying +
                ", animatorInitialized=" + animator.isInitialized +
                ", editModePreviewActive=" + IsActualPreviewActive + ".");
        }

        internal static void ApplyActualPlaybackFix()
        {
            ApplyIdleLoop();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            Undo.RecordObject(animator, "Repair MarkerSpray_Spray actual idle playback");
            animator.enabled = true;
            animator.speed = 1f;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.fireEvents = true;
            animator.keepAnimatorStateOnDisable = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene could not be saved.");
            AssetDatabase.SaveAssets();
            InspectActualPlayback();
            StartActualPreview();
            Debug.Log(
                "[MarkerSpraySpray] Actual target playback configuration repaired and saved. " +
                "The connected controller is now visibly looping on the actual Edit Mode target.");
        }

        internal static void CaptureActualPlaybackFinal()
        {
            RequireEditMode();
            InspectActualPlayback();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            AnimatorController controller = RequireController(animator, TargetName);
            bool restartPreview = IsActualPreviewActive;
            if (restartPreview) StopActualPreview();
            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            Vector3[] positions = transforms.Select(item => item.localPosition).ToArray();
            Quaternion[] rotations = transforms.Select(item => item.localRotation).ToArray();
            Vector3[] scales = transforms.Select(item => item.localScale).ToArray();
            var fullPanels = new Texture2D[6];
            var upperPanels = new Texture2D[6];
            string firstBoundaryPose = null;
            var sampledPoses = new string[6];
            try
            {
                for (int index = 0; index < 6; index++)
                {
                    float normalized = index < 5 ? index * 0.2f : 1f;
                    SampleController(animator, controller, normalized);
                    sampledPoses[index] = PoseSignature(target.transform);
                    if (index == 0) firstBoundaryPose = sampledPoses[index];
                    fullPanels[index] = CapturePanel(target.transform, false);
                    upperPanels[index] = CapturePanel(target.transform, true);
                }
                RequireEqual(firstBoundaryPose, sampledPoses[5],
                    "actual controller loop boundary pose");
                if (sampledPoses.Distinct(StringComparer.Ordinal).Count() < 3)
                    throw new InvalidOperationException(
                        "MarkerSpray_Spray actual controller produced no visible motion.");
                ComposeReview(fullPanels, upperPanels, ActualPlaybackFinalAbsolutePath);
            }
            finally
            {
                for (int index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = positions[index];
                    transforms[index].localRotation = rotations[index];
                    transforms[index].localScale = scales[index];
                }
                foreach (Texture2D image in fullPanels)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
                foreach (Texture2D image in upperPanels)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (restartPreview) StartActualPreview();
            }
            AssetDatabase.ImportAsset(ActualPlaybackFinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (scene.isDirty != sceneWasDirty)
                throw new InvalidOperationException(
                    "Direct actual-target capture changed the scene dirty state.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySpray] Actual scene target controller captured directly. " +
                "Six phases include an exact loop-boundary return and visible motion.");
        }

        private static bool IsActualPreviewActive =>
            actualPreviewGraph.IsValid() && actualPreviewGraph.IsPlaying() &&
            actualPreviewTarget != null;

        private static void StartActualPreview()
        {
            StopActualPreview();
            Scene scene = RequireScene();
            actualPreviewTarget = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(actualPreviewTarget);
            AnimatorController controller = RequireController(animator, TargetName);
            actualPreviewGraph = PlayableGraph.Create(
                "MarkerSpraySpray_ActualEditModePreviewGraph");
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                actualPreviewGraph,
                "MarkerSpraySpray_ActualEditModePreviewOutput",
                animator);
            actualPreviewPlayable = AnimatorControllerPlayable.Create(
                actualPreviewGraph, controller);
            output.SetSourcePlayable(actualPreviewPlayable);
            actualPreviewGraph.Play();
            actualPreviewPlayable.Play(StateName, 0, 0f);
            actualPreviewGraph.Evaluate(0f);
            RefreshActualCarryPreview();
            actualPreviewLastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update -= UpdateActualPreview;
            EditorApplication.update += UpdateActualPreview;
            AssemblyReloadEvents.beforeAssemblyReload -= StopActualPreview;
            AssemblyReloadEvents.beforeAssemblyReload += StopActualPreview;
            SceneView.RepaintAll();
        }

        private static void UpdateActualPreview()
        {
            if (!IsActualPreviewActive || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StopActualPreview();
                return;
            }
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)Math.Min(0.1d, Math.Max(0d, now - actualPreviewLastTime));
            actualPreviewLastTime = now;
            actualPreviewGraph.Evaluate(delta);
            RefreshActualCarryPreview();
            SceneView.RepaintAll();
        }

        private static void RefreshActualCarryPreview()
        {
            if (actualPreviewTarget == null) return;
            MarkerSprayRightHandFollowBehaviour carry =
                actualPreviewTarget.GetComponent<MarkerSprayRightHandFollowBehaviour>();
            if (carry != null) carry.RefreshPreview();
        }

        private static void StopActualPreview()
        {
            EditorApplication.update -= UpdateActualPreview;
            AssemblyReloadEvents.beforeAssemblyReload -= StopActualPreview;
            if (actualPreviewGraph.IsValid()) actualPreviewGraph.Destroy();
            actualPreviewPlayable = default;
            actualPreviewTarget = null;
        }

        internal static void InspectSource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject sourceTarget = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            Animator sourceAnimator = RequireAnimator(sourceTarget);
            Animator targetAnimator = RequireAnimator(target);
            AnimatorController sourceController = RequireController(
                sourceAnimator, SourceTargetName);
            RequireEqual(SourceControllerPath, AssetDatabase.GetAssetPath(sourceController),
                "source controller path");
            AnimationClip sourceClip = RequireSourceClip(sourceController);
            RequireEqual(SourceClipPath, AssetDatabase.GetAssetPath(sourceClip),
                "source idle clip path");
            if (target.GetComponent<Bellerophon.PlayerAnimation.MarkerSprayRightHandFollowBehaviour>() != null)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray unexpectedly has a carry/follow component.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySpray] Source inspected read-only. clipLength=" +
                F(sourceClip.length) + ", frameRate=" + F(sourceClip.frameRate) +
                ", looping=" + sourceClip.isLooping + ", targetController=" +
                AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController) +
                ", sceneDirty=" + scene.isDirty + ".");
        }

        internal static void ApplyIdleLoop()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject sourceTarget = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            Animator sourceAnimator = RequireAnimator(sourceTarget);
            Animator targetAnimator = RequireAnimator(target);
            AnimatorController sourceController = RequireController(
                sourceAnimator, SourceTargetName);
            AnimationClip sourceClip = RequireSourceClip(sourceController);

            string sourceClipHash = ComputeAssetHash(SourceClipPath);
            string sourceControllerHash = ComputeAssetHash(SourceControllerPath);
            string sourceHierarchy = HierarchySignature(sourceTarget.transform);
            string targetHierarchy = HierarchySignature(target.transform);
            string avatarPath = AssetDatabase.GetAssetPath(targetAnimator.avatar);
            if (string.IsNullOrEmpty(avatarPath))
                throw new InvalidOperationException("MarkerSpray_Spray avatar is missing.");

            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputClipPath);
            if (copy == null)
            {
                copy = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, copy);
                copy.name = "MarkerSpraySpray_Idle";
                AssetDatabase.CreateAsset(copy, OutputClipPath);
            }
            else
            {
                EditorUtility.CopySerialized(sourceClip, copy);
                copy.name = "MarkerSpraySpray_Idle";
                EditorUtility.SetDirty(copy);
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    OutputControllerPath);
            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = "MarkerSpray Spray Idle";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            AnimatorStateMachine machine = layer.stateMachine;
            foreach (AnimatorState state in machine.states.Select(item => item.state).ToArray())
                machine.RemoveState(state);
            foreach (AnimatorStateMachine nested in machine.stateMachines
                .Select(item => item.stateMachine).ToArray())
                machine.RemoveStateMachine(nested);
            AnimatorState sourceState = sourceController.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException("MarkerSpray_Idle default state is missing.");
            AnimatorState targetState = machine.AddState(StateName);
            targetState.motion = copy;
            targetState.speed = sourceState.speed;
            targetState.cycleOffset = 0f;
            targetState.mirror = sourceState.mirror;
            targetState.writeDefaultValues = sourceState.writeDefaultValues;
            machine.defaultState = targetState;
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(targetState);

            Undo.RecordObject(targetAnimator, "Connect MarkerSpray_Spray copied idle loop");
            targetAnimator.runtimeAnimatorController = controller;
            targetAnimator.applyRootMotion = false;
            targetAnimator.enabled = true;
            targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
            EditorUtility.SetDirty(targetAnimator);
            AssetDatabase.SaveAssets();

            RequireEqual(sourceClipHash, ComputeAssetHash(SourceClipPath), "source clip hash");
            RequireEqual(sourceControllerHash, ComputeAssetHash(SourceControllerPath),
                "source controller hash");
            RequireEqual(sourceHierarchy, HierarchySignature(sourceTarget.transform),
                "MarkerSpray_Idle hierarchy");
            RequireEqual(targetHierarchy, HierarchySignature(target.transform),
                "MarkerSpray_Spray hierarchy");
            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(targetAnimator.avatar),
                "MarkerSpray_Spray avatar");
            RequireEqual(ClipSignature(sourceClip), ClipSignature(copy),
                "source/copied idle clip");

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene could not be saved.");
            AssetDatabase.SaveAssets();
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySpray] Source-exact idle clip copied and connected as a " +
                "single looping state. Preexisting scene dirty state preserved=" +
                sceneWasDirty + ".");
        }

        internal static void InspectIdleLoop()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject sourceTarget = FindUnique(scene, SourceTargetName);
            GameObject target = FindUnique(scene, TargetName);
            Animator sourceAnimator = RequireAnimator(sourceTarget);
            Animator targetAnimator = RequireAnimator(target);
            AnimatorController sourceController = RequireController(
                sourceAnimator, SourceTargetName);
            AnimatorController controller = RequireController(targetAnimator, TargetName);
            RequireEqual(OutputControllerPath, AssetDatabase.GetAssetPath(controller),
                "target controller path");
            if (controller.layers.Length != 1 || controller.parameters.Length != 0)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray controller must have one parameterless layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.avatarMask != null ||
                layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f))
                throw new InvalidOperationException("MarkerSpray_Spray layer settings differ.");
            AnimatorState[] states = layer.stateMachine.states
                .Select(item => item.state).ToArray();
            if (states.Length != 1 || layer.stateMachine.defaultState != states[0] ||
                states[0].name != StateName || states[0].transitions.Length != 0 ||
                !Mathf.Approximately(states[0].speed, 1f) ||
                !Mathf.Approximately(states[0].cycleOffset, 0f) || states[0].mirror ||
                states[0].writeDefaultValues)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray single looping state settings differ.");
            AnimationClip sourceClip = RequireSourceClip(sourceController);
            AnimationClip copy = states[0].motion as AnimationClip ??
                throw new InvalidOperationException("MarkerSpray_Spray idle clip is missing.");
            RequireEqual(OutputClipPath, AssetDatabase.GetAssetPath(copy),
                "copied idle clip path");
            RequireEqual(ClipSignature(sourceClip), ClipSignature(copy),
                "source/copied idle clip");
            if (!sourceClip.isLooping || !copy.isLooping)
                throw new InvalidOperationException("The copied idle clip is not looping.");
            if (targetAnimator.applyRootMotion || !targetAnimator.enabled ||
                targetAnimator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("MarkerSpray_Spray Animator settings differ.");
            if (target.GetComponent<Bellerophon.PlayerAnimation.MarkerSprayRightHandFollowBehaviour>() != null)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray carry/follow state was modified outside scope.");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySpray] Inspection passed: one exact copied idle clip, " +
                "one state, loop enabled, no transitions, no carry/model changes.");
        }

        internal static void CaptureFinal()
        {
            RequireEditMode();
            InspectIdleLoop();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireAsset<AnimationClip>(OutputClipPath);
            var fullPanels = new Texture2D[6];
            var upperPanels = new Texture2D[6];
            try
            {
                for (int index = 0; index < 6; index++)
                {
                    float normalized = index < 5 ? index * 0.2f : 0f;
                    GameObject clone = UnityEngine.Object.Instantiate(target);
                    clone.name = "MarkerSpraySprayIdle_FinalReviewClone";
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(clone, scene);
                    clone.transform.SetPositionAndRotation(
                        new Vector3(10000f, -10000f, 10000f), target.transform.rotation);
                    try
                    {
                        foreach (MonoBehaviour behaviour in
                            clone.GetComponentsInChildren<MonoBehaviour>(true))
                            behaviour.enabled = false;
                        SampleClip(RequireAnimator(clone), clip, normalized * clip.length);
                        fullPanels[index] = CapturePanel(clone.transform, false);
                        upperPanels[index] = CapturePanel(clone.transform, true);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(clone);
                    }
                }
                ComposeReview(fullPanels, upperPanels, FinalAbsolutePath);
            }
            finally
            {
                foreach (Texture2D image in fullPanels)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
                foreach (Texture2D image in upperPanels)
                    if (image != null) UnityEngine.Object.DestroyImmediate(image);
            }
            AssetDatabase.ImportAsset(FinalImagePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log(
                "[MarkerSpraySpray] Final direct idle-loop comparison captured once. " +
                "Panels 1 and 6 are the same loop-boundary pose.");
        }

        private static AnimationClip RequireSourceClip(AnimatorController controller)
        {
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException("MarkerSpray_Idle default state is missing.");
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("MarkerSpray_Idle 2D Blend Tree is missing.");
            if (root.children.Length != 6 || root.blendType != BlendTreeType.FreeformCartesian2D)
                throw new InvalidOperationException("MarkerSpray_Idle 2D Blend Tree changed.");
            ChildMotion idle = root.children.SingleOrDefault(item =>
                (item.position - Vector2.zero).sqrMagnitude <= 0.00000001f);
            AnimationClip clip = idle.motion as AnimationClip ??
                throw new InvalidOperationException("MarkerSpray_Idle idle child is missing.");
            return clip;
        }

        private static AnimatorState RequireSingleTargetState(AnimatorController controller)
        {
            if (controller.layers.Length != 1 || controller.parameters.Length != 0)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray controller must have one parameterless layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorState[] states = layer.stateMachine.states
                .Select(item => item.state).ToArray();
            if (states.Length != 1 || layer.stateMachine.defaultState != states[0] ||
                states[0].name != StateName || states[0].transitions.Length != 0)
                throw new InvalidOperationException(
                    "MarkerSpray_Spray actual controller state configuration differs.");
            return states[0];
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
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                text.AppendLine("wrap=" + curve.preWrapMode + "," + curve.postWrapMode);
                foreach (Keyframe key in curve.keys)
                    text.AppendLine(string.Join(",", new[]
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
                text.Append("objectCurve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in
                    AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    text.AppendLine(F(key.time) + "|" + ObjectIdentity(key.value));
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                text.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" + ObjectIdentity(item.objectReferenceParameter) +
                    "|" + item.messageOptions);
            return Sha256Text(text.ToString());
        }

        private static void SampleClip(Animator animator, AnimationClip clip, double time)
        {
            PlayableGraph graph = PlayableGraph.Create("MarkerSpraySprayIdle_FinalGraph");
            try
            {
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                    graph, "MarkerSpraySprayIdle_FinalOutput", animator);
                AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetTime(time);
                output.SetSourcePlayable(playable);
                graph.Play();
                graph.Evaluate(0f);
            }
            finally
            {
                graph.Destroy();
            }
        }

        private static void SampleController(
            Animator animator,
            AnimatorController controller,
            float normalizedTime)
        {
            PlayableGraph graph = PlayableGraph.Create(
                "MarkerSpraySpray_ActualControllerReviewGraph");
            try
            {
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                    graph, "MarkerSpraySpray_ActualControllerReviewOutput", animator);
                AnimatorControllerPlayable playable =
                    AnimatorControllerPlayable.Create(graph, controller);
                output.SetSourcePlayable(playable);
                graph.Play();
                playable.Play(StateName, 0, normalizedTime);
                graph.Evaluate(0f);
            }
            finally
            {
                graph.Destroy();
            }
        }

        private static string PoseSignature(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                text.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(F(item.localPosition.x)).Append(',')
                    .Append(F(item.localPosition.y)).Append(',')
                    .Append(F(item.localPosition.z)).Append('|')
                    .Append(F(item.localRotation.x)).Append(',')
                    .Append(F(item.localRotation.y)).Append(',')
                    .Append(F(item.localRotation.z)).Append(',')
                    .AppendLine(F(item.localRotation.w));
            return Sha256Text(text.ToString());
        }

        private static string RelevantCarryPoseSignature(
            Transform root,
            AnimationClip clip,
            MarkerSprayIdleCarryProfile profile)
        {
            string[] paths = AnimationUtility.GetCurveBindings(clip)
                .Where(item => item.type == typeof(Transform))
                .Select(item => item.path)
                .Concat(profile.RightArmPose.Select(item => item.Path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
            var text = new StringBuilder();
            foreach (string path in paths)
            {
                Transform item = root.Find(path) ?? throw new MissingReferenceException(
                    root.name + " relevant carry path is missing: " + path);
                text.Append(path).Append('|')
                    .Append(F(item.localPosition.x)).Append(',')
                    .Append(F(item.localPosition.y)).Append(',')
                    .Append(F(item.localPosition.z)).Append('|')
                    .Append(F(item.localRotation.x)).Append(',')
                    .Append(F(item.localRotation.y)).Append(',')
                    .Append(F(item.localRotation.z)).Append(',')
                    .Append(F(item.localRotation.w)).Append('|')
                    .Append(F(item.localScale.x)).Append(',')
                    .Append(F(item.localScale.y)).Append(',')
                    .AppendLine(F(item.localScale.z));
            }
            return Sha256Text(text.ToString());
        }

        private static Texture2D CapturePanel(Transform target, bool upperBody)
        {
            Bounds bounds = upperBody ? UpperBodyBounds(target) : FullBounds(target);
            Vector3 direction = (target.forward + target.right * 0.72f).normalized;
            var cameraObject = new GameObject("MarkerSpraySprayIdle_FinalCamera");
            var lightObject = new GameObject("MarkerSpraySprayIdle_FinalLight");
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
                Vector3 cameraUp = Vector3.ProjectOnPlane(target.up, direction).normalized;
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

        private static Bounds FullBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("MarkerSpray_Spray has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds UpperBodyBounds(Transform target)
        {
            Transform head = RequireDescendant(target, "Head");
            Transform leftHand = RequireDescendant(target, "LeftHand");
            Transform rightHand = RequireDescendant(target, "RightHand");
            Transform hips = RequireDescendant(target, "Hips");
            Bounds bounds = new Bounds(head.position, Vector3.zero);
            bounds.Encapsulate(leftHand.position);
            bounds.Encapsulate(rightHand.position);
            bounds.Encapsulate(hips.position);
            bounds.Expand(0.22f);
            return bounds;
        }

        private static void ComposeReview(
            Texture2D[] fullPanels,
            Texture2D[] upperPanels,
            string destination)
        {
            var composite = new Texture2D(3072, 1024, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < 6; index++)
                {
                    composite.SetPixels(index * 512, 512, 512, 512,
                        fullPanels[index].GetPixels());
                    composite.SetPixels(index * 512, 0, 512, 512,
                        upperPanels[index].GetPixels());
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

        private static string HierarchySignature(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                text.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(item.localPosition).Append('|').Append(item.localRotation).Append('|')
                    .AppendLine(item.localScale.ToString());
            return Sha256Text(text.ToString());
        }

        private static string PersistentHierarchySignature(Transform root)
        {
            var text = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => !IsBelowNamed(item, root, "MarkerSpray_Prop"))
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                text.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(F(item.localPosition.x)).Append(',')
                    .Append(F(item.localPosition.y)).Append(',')
                    .Append(F(item.localPosition.z)).Append('|')
                    .Append(F(item.localRotation.x)).Append(',')
                    .Append(F(item.localRotation.y)).Append(',')
                    .Append(F(item.localRotation.z)).Append(',')
                    .Append(F(item.localRotation.w)).Append('|')
                    .Append(F(item.localScale.x)).Append(',')
                    .Append(F(item.localScale.y)).Append(',')
                    .AppendLine(F(item.localScale.z));
            return Sha256Text(text.ToString());
        }

        private static bool IsBelowNamed(Transform item, Transform root, string name)
        {
            for (Transform current = item; current != null && current != root;
                current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static MarkerSprayRightHandFollowBehaviour RequireCarry(GameObject root)
        {
            MarkerSprayRightHandFollowBehaviour carry =
                root.GetComponent<MarkerSprayRightHandFollowBehaviour>();
            if (carry == null || carry.Profile == null)
                throw new MissingReferenceException(
                    root.name + " MarkerSpray carry component/profile is missing.");
            return carry;
        }

        private static void RequireCurrentCarryPose(
            GameObject root,
            MarkerSprayRightHandFollowBehaviour carry)
        {
            if (carry.SprayHolder == null || carry.SprayModel == null)
                throw new MissingReferenceException(
                    root.name + " visible marking spray instance is missing.");
            float positionError = Vector3.Distance(
                carry.SprayHolder.transform.position, carry.ExpectedWorldPosition());
            float rotationError = Quaternion.Angle(
                carry.SprayHolder.transform.rotation, carry.ExpectedWorldRotation());
            if (positionError > PositionTolerance || rotationError > 0.01f)
                throw new InvalidOperationException(
                    root.name + " marking spray does not follow the right hand. positionError=" +
                    F(positionError) + ", rotationError=" + F(rotationError));
            foreach (MarkerSprayBoneRotation pose in carry.Profile.RightArmPose)
            {
                Transform bone = root.transform.Find(pose.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        root.name + " carry bone is missing: " + pose.Path);
                float angle = Quaternion.Angle(bone.localRotation, pose.LocalRotation);
                if (angle > 0.01f)
                    throw new InvalidOperationException(
                        root.name + " carry pose differs at " + pose.Path +
                        ". angle=" + F(angle));
            }
            int holders = root.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == "MarkerSpray_Prop");
            if (holders != 1)
                throw new InvalidOperationException(
                    root.name + " must have exactly one visible marking spray; found " +
                    holders + ".");
        }

        private static void RequireAppearanceAssets(GameObject prefab)
        {
            Material expectedMaterial = RequireAsset<Material>(MaterialPath);
            RequireEqual(BaseColorPath,
                AssetDatabase.GetAssetPath(expectedMaterial.GetTexture("_BaseMap")),
                "marking spray base color texture");
            RequireEqual(NormalPath,
                AssetDatabase.GetAssetPath(expectedMaterial.GetTexture("_BumpMap")),
                "marking spray normal texture");
            RequireEqual(MetallicSmoothnessPath,
                AssetDatabase.GetAssetPath(expectedMaterial.GetTexture("_MetallicGlossMap")),
                "marking spray metallic/smoothness texture");
            if (expectedMaterial.shader == null ||
                expectedMaterial.shader.name != "Universal Render Pipeline/Lit")
                throw new InvalidOperationException(
                    "Marking spray material does not use URP Lit.");
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new MissingReferenceException(
                    "Imported marking spray prefab has no Renderer.");
            Material[] materials = renderers.SelectMany(item => item.sharedMaterials).ToArray();
            if (materials.Length == 0 || materials.Any(item => item == null) ||
                materials.Any(item => item != expectedMaterial))
                throw new InvalidOperationException(
                    "Imported marking spray Renderer material remap differs.");
        }

        private static void RequireVisibleCarryModel(
            MarkerSprayRightHandFollowBehaviour carry)
        {
            if (carry.SprayModel == null)
                throw new MissingReferenceException("Visible marking spray model is missing.");
            Material expectedMaterial = RequireAsset<Material>(MaterialPath);
            Renderer[] renderers = carry.SprayModel.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 ||
                renderers.SelectMany(item => item.sharedMaterials)
                    .Any(item => item != expectedMaterial))
                throw new InvalidOperationException(
                    "Visible marking spray material differs from the imported material.");
        }

        private static string ProfileSignature(MarkerSprayIdleCarryProfile profile)
        {
            var text = new StringBuilder()
                .AppendLine(ObjectIdentity(profile.SprayPrefab))
                .AppendLine(profile.RightHandPath)
                .AppendLine(VectorText(profile.PositionOffsetInHandSpace))
                .AppendLine(QuaternionText(profile.RotationOffsetFromHand))
                .AppendLine(VectorText(profile.HolderScale))
                .AppendLine(VectorText(profile.ModelLocalPosition))
                .AppendLine(QuaternionText(profile.ModelLocalRotation))
                .AppendLine(VectorText(profile.ModelLocalScale));
            foreach (MarkerSprayBoneRotation pose in profile.SourceRightArmPose)
                text.Append("source|").Append(pose.Path).Append('|')
                    .AppendLine(QuaternionText(pose.LocalRotation));
            foreach (MarkerSprayBoneRotation pose in profile.RightArmPose)
                text.Append("carry|").Append(pose.Path).Append('|')
                    .AppendLine(QuaternionText(pose.LocalRotation));
            return Sha256Text(text.ToString());
        }

        private static string VectorText(Vector3 value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z);
        }

        private static string QuaternionText(Quaternion value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);
        }

        private sealed class TransformSnapshot
        {
            internal Transform Transform;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal Vector3 Scale;
        }

        private static TransformSnapshot[] CaptureTransformSnapshot(Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot
                {
                    Transform = item,
                    Position = item.localPosition,
                    Rotation = item.localRotation,
                    Scale = item.localScale
                }).ToArray();
        }

        private static void RestoreTransformSnapshot(TransformSnapshot[] snapshot)
        {
            foreach (TransformSnapshot item in snapshot)
            {
                if (item.Transform == null) continue;
                item.Transform.localPosition = item.Position;
                item.Transform.localRotation = item.Rotation;
                item.Transform.localScale = item.Scale;
            }
        }

        private static Animator RequireAnimator(GameObject root)
        {
            return root.GetComponent<Animator>() ??
                throw new MissingReferenceException(root.name + " Animator is missing.");
        }

        private static AnimatorController RequireController(Animator animator, string label)
        {
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(label + " does not use an AnimatorController.");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + " below " + root.name + ".");
            return matches[0];
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
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
                throw new InvalidOperationException(
                    "MarkerSpray_Spray idle operation requires Edit Mode.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256File(string path)
        {
            using (FileStream stream = File.OpenRead(Absolute(path)))
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

        private static string F(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    label + " differs. expected=" + expected + " actual=" + actual);
        }
    }
}
