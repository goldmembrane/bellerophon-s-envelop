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

namespace Bellerophon.Editor.Validation
{
    internal static class FatigueHeadShakeAnimationSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Fatigue_HeadShake";
        private const string StateName = "FatigueLocomotion2D";
        private const string ExternalSourcePath = "player model/transfer tired.fbx";
        private const string SourceAssetPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Sources/Fatigue_HeadShake_Source.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Fatigue_HeadShake.controller";
        internal const string FinalImagePath =
            "docs/validation/FatigueHeadShakeLocomotion/Final.png";

        [MenuItem("Bellerophon/Player/Apply Fatigue Head Shake Animation")]
        internal static void ApplyFatigueHeadShakeAnimation()
        {
            FatigueHeadShakeLocomotionSetup.Apply();
        }

        [MenuItem("Bellerophon/Player/Inspect Fatigue Head Shake Animation")]
        internal static void InspectFatigueHeadShakeAnimation()
        {
            FatigueHeadShakeLocomotionSetup.Inspect();
        }

        internal static GameObject RequireRuntimeTarget()
        {
            Scene scene = RequireScene();
            return FindUnique(scene, TargetName);
        }

        internal static void WriteFinalEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float maximumNormalizedTime,
            float minimumYaw,
            float maximumYaw,
            float maximumPitch)
        {
            FatigueHeadShakeLocomotionSetup.WriteFinalEvidence(
                sheet,
                consoleErrorsBefore,
                maximumNormalizedTime,
                minimumYaw,
                maximumYaw,
                maximumPitch);
        }

        private static void ConfigureExactGenericSourceForLooping()
        {
            RequireExactSourceCopy();
            AssetDatabase.ImportAsset(
                SourceAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter is unavailable: " + SourceAssetPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = false;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter disappeared: " + SourceAssetPath);
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must expose exactly one embedded animation clip.");
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            RequireExactSourceCopy();
        }

        private static AnimatorController CreateController(AnimationClip source)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException(
                    "Could not replace target-specific controller: " + ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorState state = controller.layers[0].stateMachine.AddState(StateName);
            state.motion = source;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip RequireSingleSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourceAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must contain exactly one embedded clip; actual=" +
                    clips.Length);
            return clips[0];
        }

        private static void RequireExactSourceCopy()
        {
            string external = Absolute(ExternalSourcePath);
            string copied = Absolute(SourceAssetPath);
            if (!File.Exists(external) || !File.Exists(copied))
                throw new FileNotFoundException(
                    "Fatigue head-shake source or project copy is missing.");
            RequireEqual(
                Sha256(external),
                Sha256(copied),
                "Fatigue head-shake source binary copy");
        }

        private static void RequireLoop(AnimationClip clip, bool expected, string label)
        {
            bool actual = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            if (actual != expected)
                throw new InvalidOperationException(
                    label + " loopTime differs. Expected=" + expected +
                    ", Actual=" + actual);
        }

        private static string DescribeClip(AnimationClip clip) =>
            clip.name + ",length=" +
            clip.length.ToString("0.######", CultureInfo.InvariantCulture) +
            ",frameRate=" +
            clip.frameRate.ToString("0.######", CultureInfo.InvariantCulture) +
            ",humanMotion=" + clip.humanMotion;

        private static Animator RequireAnimator(GameObject target) =>
            target.GetComponent<Animator>() ??
            throw new InvalidOperationException(TargetName + " Animator is missing.");

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            return scene;
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
                    "Expected exactly one " + name + "; actual=" + matches.Length);
            return matches[0];
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake setup requires Edit Mode.");
        }

        private static string RendererSignature(Transform root) =>
            string.Join(
                "\n",
                root.GetComponentsInChildren<Renderer>(true)
                    .OrderBy(renderer =>
                        AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            root),
                        StringComparer.Ordinal)
                    .Select(renderer =>
                        AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            root) + "|" +
                        renderer.GetType().FullName + "|" +
                        renderer.enabled));

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Project root is unavailable."),
                    relativePath));

        private static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    label + " differs. Expected=" + expected + ", Actual=" + actual);
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            internal TransformSnapshot(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    internal static class FatigueHeadShakeLocomotionSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Fatigue_HeadShake";
        private const string SourceAssetPath =
            "Assets/_Project/Animations/PlayerStatusEffects/Sources/Fatigue_HeadShake_Source.fbx";
        private const string ExternalSourcePath = "player model/transfer tired.fbx";
        private const string OutputFolder =
            "Assets/_Project/Animations/PlayerStatusEffects/FatigueHeadShakeLocomotion";
        private const string UpperMaskPath =
            OutputFolder + "/Fatigue_HeadShake_UpperOnly.mask";
        private const string BaseStateName = "FatigueLocomotion2D";
        private const string UpperStateName = "FatigueUpperSource";
        private const string RootTreeName = "FatigueLocomotion2DTree";
        private const string UpperLayerName = "Fatigue Upper Body";
        private const string MoveX =
            Bellerophon.PlayerAnimation.FatigueHeadShakeLocomotionCycleBehaviour
                .MoveXParameter;
        private const string MoveY =
            Bellerophon.PlayerAnimation.FatigueHeadShakeLocomotionCycleBehaviour
                .MoveYParameter;

        private static readonly SourceSpec[] Sources =
        {
            new SourceSpec("Player_Idle", "Idle", new Vector2(0f, 0f)),
            new SourceSpec("Player_Walk_Forward", "WalkForward", new Vector2(0f, 1f)),
            new SourceSpec("Player_Walk_Backward", "WalkBackward", new Vector2(0f, -1f)),
            new SourceSpec("Player_Sidestep", "Sidestep", new Vector2(1f, 0f)),
            new SourceSpec(
                "Player_Walk_Diagonal",
                "WalkDiagonal",
                new Vector2(0.70710677f, 0.70710677f))
        };

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator targetAnimator = RequireAnimator(target, TargetName);
            TransformSnapshot rootSnapshot = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Avatar targetAvatar = targetAnimator.avatar;

            ConfigureFatigueSource();
            AnimationClip fatigueSource = RequireSingleFatigueSourceClip();
            SourceRecord[] records = RequireSourceRecords(scene);
            ResetOutputFolder();
            AvatarMask upperMask = CreateUpperBodyMask(target.transform);
            AnimatorController controller = CreateController(
                target.transform,
                fatigueSource,
                records,
                upperMask);

            Undo.RecordObject(
                targetAnimator,
                "Connect Fatigue_HeadShake locomotion Blend Tree");
            targetAnimator.runtimeAnimatorController = controller;
            targetAnimator.applyRootMotion = false;
            targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            targetAnimator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
            EditorUtility.SetDirty(targetAnimator);

            Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver[] drivers =
                target.GetComponents<
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver>();
            Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver driver =
                drivers.Length == 0
                    ? Undo.AddComponent<
                        Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver>(target)
                    : drivers[0];
            for (int index = 1; index < drivers.Length; index++)
                Undo.DestroyObjectImmediate(drivers[index]);
            Undo.RecordObject(driver, "Configure fatigue wake head shake");
            Transform upperRig = RequireUniqueDescendant(
                target.transform,
                "Spine02");
            driver.Configure(
                targetAnimator,
                RequireUniqueDescendant(upperRig, "neck"),
                RequireUniqueDescendant(upperRig, "Head"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(driver);
            EditorUtility.SetDirty(driver);

            rootSnapshot.RequireUnchanged(target.transform, TargetName);
            RequireEqual(
                rendererSignature,
                RendererSignature(target.transform),
                TargetName + " renderer signature");
            if (targetAnimator.avatar != targetAvatar)
                throw new InvalidOperationException(
                    TargetName + " Avatar changed unexpectedly.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying Fatigue_HeadShake locomotion.");
            AssetDatabase.SaveAssets();
            Inspect();

            Debug.Log(
                "[FatigueHeadShakeLocomotion] Applied exact Player motion copies to one " +
                "FreeformCartesian2D base while preserving the unchanged fatigue source " +
                "on a continuous Spine02 upper-body layer and adding the approved " +
                "one-second four-cycle wake head shake. secondsPerMotion=1," +
                "baseMotionCount=5,sequencePhaseCount=6,cycleSeconds=6," +
                "headPitch=10,headYaw=20,shakeDurations=0.4|0.3|0.15|0.15," +
                "sourceCurvesGenerated=False," +
                "poseInference=False,targetTransformChanged=False," +
                "targetRenderersChanged=False,targetAvatarChanged=False");
        }

        internal static void Inspect()
        {
            RequireEditMode();
            RequireExactFatigueSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target, TargetName);
            AnimationClip fatigueSource = RequireSingleFatigueSourceClip();
            RequireLoop(fatigueSource, true, SourceAssetPath);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    FatigueHeadShakeAnimationSetupTools.ControllerPath) ??
                throw new InvalidOperationException(
                    FatigueHeadShakeAnimationSetupTools.ControllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(
                    TargetName + " controller connection differs.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException(
                    TargetName + " applyRootMotion must remain disabled.");
            if (controller.layers.Length != 2)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake controller must have exactly two layers.");

            RequireFloatParameter(controller, MoveX, 0f);
            RequireFloatParameter(controller, MoveY, 0f);
            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorState baseState = RequireSingleDefaultState(
                baseLayer,
                BaseStateName);
            BlendTree tree = baseState.motion as BlendTree ??
                throw new InvalidOperationException(
                    "Fatigue_HeadShake base state must use a Blend Tree.");
            if (baseLayer.avatarMask != null ||
                baseLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(baseLayer.defaultWeight, 1f) ||
                tree.name != RootTreeName ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter != MoveX ||
                tree.blendParameterY != MoveY ||
                tree.children.Length != Sources.Length ||
                !Mathf.Approximately(baseState.speed, 1f) ||
                baseState.transitions.Length != 0)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake base locomotion layer differs.");
            if (baseState.behaviours.OfType<
                    Bellerophon.PlayerAnimation
                        .FatigueHeadShakeLocomotionCycleBehaviour>().Count() != 1)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake must have one one-second cycle behaviour.");

            SourceRecord[] records = RequireSourceRecords(scene);
            ChildMotion[] children = tree.children;
            for (int index = 0; index < Sources.Length; index++)
            {
                SourceSpec spec = Sources[index];
                ChildMotion child = children[index];
                if ((child.position - spec.Position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(child.timeScale, 1f) ||
                    !Mathf.Approximately(child.cycleOffset, 0f) ||
                    child.mirror)
                    throw new InvalidOperationException(
                        spec.ObjectName + " root Blend Tree child differs.");
                RequireMotionEquivalent(
                    records[index].Motion,
                    child.motion,
                    spec.ObjectName + " copied motion");
                RequireReferencedParameters(
                    records[index].Controller,
                    records[index].Motion,
                    controller,
                    spec.ObjectName);
            }

            AnimatorControllerLayer upperLayer = controller.layers[1];
            AnimatorState upperState = RequireSingleDefaultState(
                upperLayer,
                UpperStateName);
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath) ??
                throw new InvalidOperationException(UpperMaskPath + " is missing.");
            if (upperLayer.name != UpperLayerName ||
                upperLayer.avatarMask != mask ||
                upperLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(upperLayer.defaultWeight, 1f) ||
                upperState.motion != fatigueSource ||
                !Mathf.Approximately(upperState.speed, 1f) ||
                upperState.transitions.Length != 0)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake upper-body source layer differs.");
            RequireUpperBodyMask(target.transform, mask);

            Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver[] drivers =
                target.GetComponents<
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver>();
            if (drivers.Length != 1)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake must have exactly one wake head-shake driver.");
            Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver driver = drivers[0];
            Transform upperRig = RequireUniqueDescendant(
                target.transform,
                "Spine02");
            if (!driver.IsConfigured ||
                driver.ConfiguredAnimator != animator ||
                driver.ConfiguredNeck != RequireUniqueDescendant(upperRig, "neck") ||
                driver.ConfiguredHead != RequireUniqueDescendant(upperRig, "Head"))
                throw new InvalidOperationException(
                    "Fatigue wake head-shake rig references differ.");
            RequireWakeHeadShakeDefinition();

            string upperPath = UpperBranchPath(target.transform);
            int upperCurveCount = AnimationUtility.GetCurveBindings(fatigueSource)
                .Count(binding => IsPathInBranch(binding.path, upperPath));
            if (upperCurveCount == 0)
                throw new InvalidOperationException(
                    "Fatigue source has no curves under the Spine02 upper-body branch.");
            if (fatigueSource.humanMotion)
                throw new InvalidOperationException(
                    "Fatigue upper source must remain the original Generic clip.");

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[FatigueHeadShakeLocomotion] Structural inspection passed. " +
                "blendTreeType=FreeformCartesian2D,baseMotionCount=5," +
                "sequencePhaseCount=6,secondsPerMotion=1,cycleSeconds=6," +
                "wakeLowerBody=Idle,headPitch=10,headYaw=20," +
                "shakeDurations=0.4|0.3|0.15|0.15," +
                "playerMotionCopiesExact=True," +
                "fatigueUpperSourceDirect=True,fatigueUpperCurvesPreserved=" +
                upperCurveCount + ",upperMaskBranch=Spine02," +
                "continuousUpperLayer=True,applyRootMotion=False," +
                "unityConsoleErrors=0");
        }

        internal static void WriteFinalEvidence(
            Texture2D sheet,
            int consoleErrorsBefore,
            float elapsedSeconds,
            float minimumYaw,
            float maximumYaw,
            float maximumPitch)
        {
            if (sheet == null)
                throw new ArgumentNullException(nameof(sheet));
            if (elapsedSeconds < 6f)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake did not complete its six-second sequence.");
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter > consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake locomotion review introduced Unity console errors. " +
                    "Before=" + consoleErrorsBefore + ", After=" + consoleErrorsAfter);
            string absolute = Absolute(
                FatigueHeadShakeAnimationSetupTools.FinalImagePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Fatigue_HeadShake locomotion validation folder is unavailable."));
            File.WriteAllBytes(absolute, sheet.EncodeToPNG());
            Debug.Log(
                "[FatigueHeadShakeLocomotion] Five locomotion phases, the exact " +
                "one-second four-cycle wake head shake, and the repeat boundary " +
                "captured once. elapsedSeconds=" +
                elapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture) +
                ",observedMinimumYaw=" +
                minimumYaw.ToString("0.######", CultureInfo.InvariantCulture) +
                ",observedMaximumYaw=" +
                maximumYaw.ToString("0.######", CultureInfo.InvariantCulture) +
                ",observedMaximumPitch=" +
                maximumPitch.ToString("0.######", CultureInfo.InvariantCulture) +
                ",upperMotionPreserved=True,consoleErrorsBefore=" +
                consoleErrorsBefore + ",consoleErrorsAfter=" + consoleErrorsAfter +
                ",finalImage=" + FatigueHeadShakeAnimationSetupTools.FinalImagePath);
        }

        private static void RequireWakeHeadShakeDefinition()
        {
            if (Bellerophon.PlayerAnimation
                    .FatigueHeadShakeLocomotionCycleBehaviour.MotionCount != 6 ||
                Bellerophon.PlayerAnimation
                    .FatigueHeadShakeLocomotionCycleBehaviour.HeadShakePhase != 5)
                throw new InvalidOperationException(
                    "Fatigue wake head-shake sequence phases differ.");
            float total =
                Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                    .FirstShakeSeconds +
                Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                    .SecondShakeSeconds +
                Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                    .ThirdShakeSeconds +
                Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                    .FourthShakeSeconds;
            if (!Mathf.Approximately(total, 1f) ||
                !Mathf.Approximately(
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                        .FirstShakeSeconds,
                    0.4f) ||
                !Mathf.Approximately(
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                        .SecondShakeSeconds,
                    0.3f) ||
                !Mathf.Approximately(
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                        .ThirdShakeSeconds,
                    0.15f) ||
                !Mathf.Approximately(
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                        .FourthShakeSeconds,
                    0.15f) ||
                !Mathf.Approximately(
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                        .DownPitchDegrees,
                    10f) ||
                !Mathf.Approximately(
                    Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
                        .SideYawDegrees,
                    20f))
                throw new InvalidOperationException(
                    "Fatigue wake head-shake timing or angles differ.");
            float[] times =
            {
                0.1f, 0.3f,
                0.475f, 0.625f,
                0.7375f, 0.8125f,
                0.8875f, 0.9625f
            };
            for (int index = 0; index < times.Length; index++)
            {
                float expected = index % 2 == 0 ? -20f : 20f;
                float actual = Bellerophon.PlayerAnimation
                    .FatigueWakeHeadShakeDriver.EvaluateYawDegrees(times[index]);
                if (Mathf.Abs(actual - expected) > 0.001f)
                    throw new InvalidOperationException(
                        "Fatigue wake head-shake extremum differs at " + times[index] + ".");
            }
        }

        private static AnimatorController CreateController(
            Transform target,
            AnimationClip fatigueSource,
            SourceRecord[] records,
            AvatarMask upperMask)
        {
            string controllerPath = FatigueHeadShakeAnimationSetupTools.ControllerPath;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(controllerPath) != null &&
                !AssetDatabase.DeleteAsset(controllerPath))
                throw new InvalidOperationException(
                    "Could not replace Fatigue_HeadShake controller.");
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = MoveX,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0f
            });
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = MoveY,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0f
            });

            Motion[] copies = new Motion[records.Length];
            int clipIndex = 0;
            for (int index = 0; index < records.Length; index++)
            {
                SourceRecord record = records[index];
                CopyReferencedParameters(
                    record.Controller,
                    record.Motion,
                    controller);
                copies[index] = CopyMotion(
                    record.Motion,
                    controller,
                    OutputFolder,
                    "Fatigue_" + record.Spec.Label + "_PlayerLower",
                    ref clipIndex);
            }

            AnimatorControllerLayer baseLayer = controller.layers[0];
            baseLayer.name = "Player Lower Locomotion 2D";
            baseLayer.defaultWeight = 1f;
            baseLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            baseLayer.avatarMask = null;
            baseLayer.iKPass = false;
            var tree = new BlendTree
            {
                name = RootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = MoveX,
                blendParameterY = MoveY,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.children = Sources.Select((spec, index) =>
                Child(copies[index], spec.Position)).ToArray();
            AnimatorState baseState = baseLayer.stateMachine.AddState(BaseStateName);
            baseState.motion = tree;
            baseState.speed = 1f;
            baseState.cycleOffset = 0f;
            baseState.mirror = false;
            baseState.writeDefaultValues = false;
            baseState.AddStateMachineBehaviour<
                Bellerophon.PlayerAnimation
                    .FatigueHeadShakeLocomotionCycleBehaviour>();
            baseLayer.stateMachine.defaultState = baseState;
            controller.layers = new[] { baseLayer };

            controller.AddLayer(UpperLayerName);
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer upperLayer = layers[1];
            upperLayer.name = UpperLayerName;
            upperLayer.defaultWeight = 1f;
            upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
            upperLayer.avatarMask = upperMask;
            upperLayer.iKPass = false;
            AnimatorState upperState = upperLayer.stateMachine.AddState(UpperStateName);
            upperState.motion = fatigueSource;
            upperState.speed = 1f;
            upperState.cycleOffset = 0f;
            upperState.mirror = false;
            upperState.writeDefaultValues = false;
            upperLayer.stateMachine.defaultState = upperState;
            layers[1] = upperLayer;
            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(baseLayer.stateMachine);
            EditorUtility.SetDirty(upperLayer.stateMachine);
            EditorUtility.SetDirty(tree);
            return controller;
        }

        private static SourceRecord[] RequireSourceRecords(Scene scene)
        {
            return Sources.Select(spec =>
            {
                AnimatorController controller = RequireSourceController(
                    scene,
                    spec.ObjectName);
                Motion motion = RequireDefaultMotion(controller, spec.ObjectName);
                return new SourceRecord(spec, controller, motion);
            }).ToArray();
        }

        private static AnimatorController RequireSourceController(
            Scene scene,
            string objectName)
        {
            Animator animator = RequireAnimator(
                FindUnique(scene, objectName),
                objectName);
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(
                    objectName + " must use an AnimatorController.");
        }

        private static Motion RequireDefaultMotion(
            AnimatorController controller,
            string objectName)
        {
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException(
                    objectName + " default state is missing.");
            return state.motion ??
                throw new InvalidOperationException(
                    objectName + " default motion is missing.");
        }

        private static Motion CopyMotion(
            Motion source,
            AnimatorController owner,
            string folder,
            string name,
            ref int clipIndex)
        {
            if (source is AnimationClip sourceClip)
            {
                string path = folder + "/" + name + "_" + clipIndex++ + ".anim";
                var copy = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, copy);
                copy.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(copy, path);
                RequireEqual(
                    ClipSignature(sourceClip),
                    ClipSignature(copy),
                    name + " exact clip copy");
                return copy;
            }
            if (!(source is BlendTree sourceTree))
                throw new InvalidOperationException(
                    "Unsupported Player motion type: " + source.GetType().FullName);
            var copyTree = new BlendTree
            {
                name = name,
                blendType = sourceTree.blendType,
                blendParameter = sourceTree.blendParameter,
                blendParameterY = sourceTree.blendParameterY,
                useAutomaticThresholds = sourceTree.useAutomaticThresholds,
                minThreshold = sourceTree.minThreshold,
                maxThreshold = sourceTree.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(copyTree, owner);
            ChildMotion[] sourceChildren = sourceTree.children;
            var copiedChildren = new ChildMotion[sourceChildren.Length];
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion child = sourceChildren[index];
                copiedChildren[index] = new ChildMotion
                {
                    motion = CopyMotion(
                        child.motion,
                        owner,
                        folder,
                        name + "_Child" + index,
                        ref clipIndex),
                    threshold = child.threshold,
                    position = child.position,
                    timeScale = child.timeScale,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    directBlendParameter = child.directBlendParameter
                };
            }
            copyTree.children = copiedChildren;
            EditorUtility.SetDirty(copyTree);
            return copyTree;
        }

        private static void CopyReferencedParameters(
            AnimatorController sourceController,
            Motion sourceMotion,
            AnimatorController destination)
        {
            foreach (string parameterName in ReferencedBlendParameters(sourceMotion)
                         .Distinct(StringComparer.Ordinal))
            {
                if (parameterName == MoveX || parameterName == MoveY)
                    continue;
                AnimatorControllerParameter source = sourceController.parameters
                    .SingleOrDefault(item => item.name == parameterName) ??
                    throw new InvalidOperationException(
                        sourceController.name + " is missing parameter " + parameterName + ".");
                AnimatorControllerParameter existing = destination.parameters
                    .SingleOrDefault(item => item.name == parameterName);
                if (existing != null)
                {
                    RequireParameterEqual(source, existing, parameterName);
                    continue;
                }
                destination.AddParameter(new AnimatorControllerParameter
                {
                    name = source.name,
                    type = source.type,
                    defaultBool = source.defaultBool,
                    defaultFloat = source.defaultFloat,
                    defaultInt = source.defaultInt
                });
            }
        }

        private static void RequireReferencedParameters(
            AnimatorController sourceController,
            Motion sourceMotion,
            AnimatorController destination,
            string label)
        {
            foreach (string parameterName in ReferencedBlendParameters(sourceMotion)
                         .Distinct(StringComparer.Ordinal))
            {
                if (parameterName == MoveX || parameterName == MoveY)
                    continue;
                AnimatorControllerParameter source = sourceController.parameters
                    .SingleOrDefault(item => item.name == parameterName) ??
                    throw new InvalidOperationException(
                        label + " source parameter is missing: " + parameterName);
                AnimatorControllerParameter copy = destination.parameters
                    .SingleOrDefault(item => item.name == parameterName) ??
                    throw new InvalidOperationException(
                        label + " copied parameter is missing: " + parameterName);
                RequireParameterEqual(source, copy, label + " " + parameterName);
            }
        }

        private static void RequireParameterEqual(
            AnimatorControllerParameter expected,
            AnimatorControllerParameter actual,
            string label)
        {
            if (expected.type != actual.type ||
                expected.defaultBool != actual.defaultBool ||
                !Mathf.Approximately(expected.defaultFloat, actual.defaultFloat) ||
                expected.defaultInt != actual.defaultInt)
                throw new InvalidOperationException(
                    label + " Animator parameter differs.");
        }

        private static IEnumerable<string> ReferencedBlendParameters(Motion motion)
        {
            if (!(motion is BlendTree tree))
                yield break;
            if (!string.IsNullOrEmpty(tree.blendParameter))
                yield return tree.blendParameter;
            if ((tree.blendType == BlendTreeType.FreeformCartesian2D ||
                 tree.blendType == BlendTreeType.FreeformDirectional2D ||
                 tree.blendType == BlendTreeType.SimpleDirectional2D) &&
                !string.IsNullOrEmpty(tree.blendParameterY))
                yield return tree.blendParameterY;
            foreach (ChildMotion child in tree.children)
            {
                if (tree.blendType == BlendTreeType.Direct &&
                    !string.IsNullOrEmpty(child.directBlendParameter))
                    yield return child.directBlendParameter;
                foreach (string nested in ReferencedBlendParameters(child.motion))
                    yield return nested;
            }
        }

        private static void RequireMotionEquivalent(
            Motion source,
            Motion copy,
            string label)
        {
            if (source is AnimationClip sourceClip)
            {
                if (!(copy is AnimationClip copiedClip))
                    throw new InvalidOperationException(label + " type differs.");
                RequireEqual(
                    ClipSignature(sourceClip),
                    ClipSignature(copiedClip),
                    label);
                return;
            }
            if (!(source is BlendTree sourceTree) ||
                !(copy is BlendTree copiedTree) ||
                sourceTree.blendType != copiedTree.blendType ||
                sourceTree.blendParameter != copiedTree.blendParameter ||
                sourceTree.blendParameterY != copiedTree.blendParameterY ||
                sourceTree.useAutomaticThresholds != copiedTree.useAutomaticThresholds ||
                !Mathf.Approximately(sourceTree.minThreshold, copiedTree.minThreshold) ||
                !Mathf.Approximately(sourceTree.maxThreshold, copiedTree.maxThreshold) ||
                sourceTree.children.Length != copiedTree.children.Length)
                throw new InvalidOperationException(label + " Blend Tree differs.");
            ChildMotion[] a = sourceTree.children;
            ChildMotion[] b = copiedTree.children;
            for (int index = 0; index < a.Length; index++)
            {
                if (!Mathf.Approximately(a[index].threshold, b[index].threshold) ||
                    (a[index].position - b[index].position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(a[index].timeScale, b[index].timeScale) ||
                    !Mathf.Approximately(a[index].cycleOffset, b[index].cycleOffset) ||
                    a[index].mirror != b[index].mirror ||
                    a[index].directBlendParameter != b[index].directBlendParameter)
                    throw new InvalidOperationException(
                        label + " child differs at " + index + ".");
                RequireMotionEquivalent(
                    a[index].motion,
                    b[index].motion,
                    label + " child " + index);
            }
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
                foreach (ObjectReferenceKeyframe key in
                         AnimationUtility.GetObjectReferenceCurve(clip, binding))
                {
                    string guid = string.Empty;
                    long localId = 0;
                    if (key.value != null)
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                            key.value,
                            out guid,
                            out localId);
                    result.AppendLine(F(key.time) + "," + guid + "," + localId);
                }
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine(
                    "event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + F(item.floatParameter) + "|" +
                    item.intParameter);
            return result.ToString();
        }

        private static AvatarMask CreateUpperBodyMask(Transform target)
        {
            var mask = new AvatarMask { name = "Fatigue_HeadShake_UpperOnly" };
            string upperPath = UpperBranchPath(target);
            string[] paths = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target)
                .Select(item => AnimationUtility.CalculateTransformPath(item, target))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item.Count(character => character == '/'))
                .ThenBy(item => item, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(
                    index,
                    IsPathInBranch(paths[index], upperPath));
            }
            for (int index = 0;
                 index < (int)AvatarMaskBodyPart.LastBodyPart;
                 index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            AssetDatabase.CreateAsset(mask, UpperMaskPath);
            return mask;
        }

        private static void RequireUpperBodyMask(Transform target, AvatarMask mask)
        {
            string upperPath = UpperBranchPath(target);
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                if (mask.GetTransformActive(index) !=
                    IsPathInBranch(path, upperPath))
                    throw new InvalidOperationException(
                        "Fatigue upper mask differs at " + path + ".");
            }
        }

        private static string UpperBranchPath(Transform target) =>
            AnimationUtility.CalculateTransformPath(
                RequireUniqueDescendant(target, "Spine02"),
                target);

        private static Transform RequireUniqueDescendant(
            Transform root,
            string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    root.name + " must contain exactly one " + name + ".");
            return matches[0];
        }

        private static bool IsPathInBranch(string path, string branch) =>
            path == branch || path.StartsWith(branch + "/", StringComparison.Ordinal);

        private static ChildMotion Child(Motion motion, Vector2 position) =>
            new ChildMotion
            {
                motion = motion,
                position = position,
                timeScale = 1f,
                cycleOffset = 0f,
                mirror = false,
                threshold = 0f,
                directBlendParameter = string.Empty
            };

        private static AnimatorState RequireSingleDefaultState(
            AnimatorControllerLayer layer,
            string expectedName)
        {
            AnimatorState[] states = layer.stateMachine.states
                .Select(item => item.state)
                .ToArray();
            if (states.Length != 1 ||
                layer.stateMachine.defaultState != states[0] ||
                states[0].name != expectedName)
                throw new InvalidOperationException(
                    layer.name + " must contain one default state named " +
                    expectedName + ".");
            return states[0];
        }

        private static void RequireFloatParameter(
            AnimatorController controller,
            string name,
            float defaultValue)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name) ??
                throw new InvalidOperationException(name + " parameter is missing.");
            if (parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, defaultValue))
                throw new InvalidOperationException(name + " parameter differs.");
        }

        private static void ConfigureFatigueSource()
        {
            RequireExactFatigueSourceCopy();
            AssetDatabase.ImportAsset(
                SourceAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter is unavailable: " + SourceAssetPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = false;
            importer.SaveAndReimport();
            importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter disappeared: " + SourceAssetPath);
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must expose exactly one clip.");
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            RequireExactFatigueSourceCopy();
        }

        private static AnimationClip RequireSingleFatigueSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourceAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must contain exactly one embedded clip.");
            return clips[0];
        }

        private static void RequireLoop(
            AnimationClip clip,
            bool expected,
            string label)
        {
            bool actual = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            if (actual != expected)
                throw new InvalidOperationException(
                    label + " loopTime differs.");
        }

        private static void RequireExactFatigueSourceCopy()
        {
            string source = Absolute(ExternalSourcePath);
            string copy = Absolute(SourceAssetPath);
            if (!File.Exists(source) || !File.Exists(copy))
                throw new FileNotFoundException(
                    "Fatigue source or project copy is missing.");
            RequireEqual(
                Sha256(source),
                Sha256(copy),
                "Fatigue source binary copy");
        }

        private static void ResetOutputFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder) &&
                !AssetDatabase.DeleteAsset(OutputFolder))
                throw new InvalidOperationException(
                    "Could not replace " + OutputFolder + ".");
            string parent = Path.GetDirectoryName(OutputFolder)
                ?.Replace('\\', '/') ??
                throw new InvalidOperationException("Output parent is unavailable.");
            string name = Path.GetFileName(OutputFolder);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, name)))
                throw new InvalidOperationException(
                    "Could not create " + OutputFolder + ".");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            return scene;
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
                    "Expected exactly one " + name + "; actual=" + matches.Length);
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target, string label) =>
            target.GetComponent<Animator>() ??
            throw new InvalidOperationException(label + " Animator is missing.");

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake locomotion setup requires Edit Mode.");
        }

        private static string RendererSignature(Transform root) =>
            string.Join(
                "\n",
                root.GetComponentsInChildren<Renderer>(true)
                    .OrderBy(renderer =>
                        AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            root),
                        StringComparer.Ordinal)
                    .Select(renderer =>
                        AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            root) + "|" + renderer.GetType().FullName + "|" +
                        renderer.enabled));

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException("Project root is unavailable."),
                    relativePath));

        private static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private static void RequireEqual(
            string expected,
            string actual,
            string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    label + " differs.");
        }

        private readonly struct SourceSpec
        {
            internal SourceSpec(string objectName, string label, Vector2 position)
            {
                ObjectName = objectName;
                Label = label;
                Position = position;
            }

            internal string ObjectName { get; }
            internal string Label { get; }
            internal Vector2 Position { get; }
        }

        private readonly struct SourceRecord
        {
            internal SourceRecord(
                SourceSpec spec,
                AnimatorController controller,
                Motion motion)
            {
                Spec = spec;
                Controller = controller;
                Motion = motion;
            }

            internal SourceSpec Spec { get; }
            internal AnimatorController Controller { get; }
            internal Motion Motion { get; }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            internal TransformSnapshot(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class FatigueHeadShakePlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.FatigueHeadShake.Pending";
        private const string StateKey =
            "Bellerophon.FatigueHeadShake.State";
        private const string FailureKey =
            "Bellerophon.FatigueHeadShake.Failure";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.FatigueHeadShake.ConsoleErrorsBefore";
        private const int WaitingForPlayMode = 0;
        private const int Capturing = 1;
        private const int WaitingForEditModeAfterSuccess = 2;
        private const int WaitingForEditModeAfterFailure = 3;
        private const string StateName = "FatigueLocomotion2D";
        private const int PanelCount = 14;
        private static readonly int[] PhaseOffsets =
        {
            0, 1, 2, 3, 4,
            5, 5, 5, 5, 5, 5, 5, 5,
            6
        };
        private static readonly float[] PhaseSampleTimes =
        {
            0.5f, 0.5f, 0.5f, 0.5f, 0.5f,
            0.0875f, 0.2875f,
            0.4625f, 0.6125f,
            0.725f, 0.8f,
            0.875f, 0.95f,
            0.08f
        };
        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static readonly List<Quaternion[]> UpperPoseSamples =
            new List<Quaternion[]>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver
            headShakeDriver;
        private static Transform head;
        private static Vector3 initialLocalPosition;
        private static Quaternion initialLocalRotation;
        private static Vector3 initialLocalScale;
        private static double startedAt;
        private static int captureCycleStartAbsolutePhase;
        private static float maximumElapsedSeconds;
        private static float observedMinimumYaw;
        private static float observedMaximumYaw;
        private static float observedMaximumPitch;
        private static Transform[] upperProbes;

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake review must start in Edit Mode.");
            FatigueHeadShakeAnimationSetupTools.InspectFatigueHeadShakeAnimation();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake review has no pending state.");
            if (EditorApplication.isPlaying &&
                (target == null || animator == null || Panels.Count == 0))
            {
                InitializeRuntime();
                SessionState.SetInt(StateKey, Capturing);
            }
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before Fatigue_HeadShake review completed.");
                    CaptureNaturalLoop();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                FatigueHeadShakeAnimationSetupTools.InspectFatigueHeadShakeAnimation();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Fatigue_HeadShake completed five one-second locomotion phases, " +
                    "the approved one-second four-cycle wake head shake, and repeat " +
                    "with its continuous original upper-body motion preserved.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            target = FatigueHeadShakeAnimationSetupTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Fatigue_HeadShake Animator is missing in Play Mode.");
            headShakeDriver = target.GetComponent<
                Bellerophon.PlayerAnimation.FatigueWakeHeadShakeDriver>() ??
                throw new InvalidOperationException(
                    "Fatigue_HeadShake wake head-shake driver is missing in Play Mode.");
            head = headShakeDriver.ConfiguredHead;
            if (head == null)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake configured Head transform is missing in Play Mode.");
            initialLocalPosition = target.transform.localPosition;
            initialLocalRotation = target.transform.localRotation;
            initialLocalScale = target.transform.localScale;
            startedAt = EditorApplication.timeSinceStartup;
            captureCycleStartAbsolutePhase = -1;
            maximumElapsedSeconds = 0f;
            string[] upperNames = { "Spine02", "Spine01", "Spine", "neck", "Head" };
            upperProbes = target.GetComponentsInChildren<Transform>(true)
                .Where(item => upperNames.Contains(item.name))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (upperProbes.Length < 3)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake upper-body review probes are missing.");
        }

        private static void CaptureNaturalLoop()
        {
            if (EditorApplication.timeSinceStartup - startedAt > 30d)
                throw new TimeoutException(
                    "Fatigue_HeadShake six-second sequence review exceeded 30 seconds.");
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(StateName))
                return;
            if (!Bellerophon.PlayerAnimation
                    .FatigueHeadShakeLocomotionCycleBehaviour.TryGetSequenceState(
                        animator,
                        out int absolutePhase,
                        out int phase,
                        out float phaseElapsed))
                return;
            int motionCount = Bellerophon.PlayerAnimation
                .FatigueHeadShakeLocomotionCycleBehaviour.MotionCount;
            if (captureCycleStartAbsolutePhase < 0)
            {
                captureCycleStartAbsolutePhase =
                    phase == 0 && phaseElapsed <= 0.15f
                        ? absolutePhase
                        : absolutePhase + (motionCount - phase);
                return;
            }
            if (Panels.Count >= PanelCount)
                return;
            int expectedAbsolutePhase =
                captureCycleStartAbsolutePhase + PhaseOffsets[Panels.Count];
            if (absolutePhase < expectedAbsolutePhase)
                return;
            if (absolutePhase > expectedAbsolutePhase)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake review missed sample " + Panels.Count + ".");
            int expectedPhase = PhaseOffsets[Panels.Count] % motionCount;
            if (phase != expectedPhase)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake locomotion phase order differs.");
            float sampleTime = PhaseSampleTimes[Panels.Count];
            if (phaseElapsed < sampleTime)
                return;
            Vector2 expectedMove = Bellerophon.PlayerAnimation
                .FatigueHeadShakeLocomotionCycleBehaviour.MotionPosition(expectedPhase);
            if (Mathf.Abs(animator.GetFloat(
                    Bellerophon.PlayerAnimation
                        .FatigueHeadShakeLocomotionCycleBehaviour.MoveXParameter) -
                    expectedMove.x) > 0.001f ||
                Mathf.Abs(animator.GetFloat(
                    Bellerophon.PlayerAnimation
                        .FatigueHeadShakeLocomotionCycleBehaviour.MoveYParameter) -
                    expectedMove.y) > 0.001f)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake Blend Tree parameters differ at phase " +
                    expectedPhase + ".");
            AnimatorStateInfo upperState = animator.GetCurrentAnimatorStateInfo(1);
            if (!upperState.IsName("FatigueUpperSource"))
                throw new InvalidOperationException(
                    "Fatigue_HeadShake upper source stopped during locomotion.");
            bool headSample = Panels.Count >= 5 && Panels.Count <= 12;
            if (headSample)
            {
                if (!headShakeDriver.IsActive)
                    throw new InvalidOperationException(
                        "Fatigue wake head-shake driver is inactive during its phase.");
                float expectedSign = Panels.Count % 2 == 1 ? -1f : 1f;
                if (Mathf.Sign(headShakeDriver.CurrentYawDegrees) != expectedSign ||
                    Mathf.Abs(headShakeDriver.CurrentYawDegrees) < 7f ||
                    headShakeDriver.CurrentPitchDegrees < 7f)
                    throw new InvalidOperationException(
                        "Fatigue wake head-shake direction or downward pitch differs at sample " +
                        Panels.Count + ". yaw=" + headShakeDriver.CurrentYawDegrees +
                        ",pitch=" + headShakeDriver.CurrentPitchDegrees + ".");
                observedMinimumYaw = Mathf.Min(
                    observedMinimumYaw,
                    headShakeDriver.CurrentYawDegrees);
                observedMaximumYaw = Mathf.Max(
                    observedMaximumYaw,
                    headShakeDriver.CurrentYawDegrees);
                observedMaximumPitch = Mathf.Max(
                    observedMaximumPitch,
                    headShakeDriver.CurrentPitchDegrees);
            }
            else if (headShakeDriver.IsActive)
            {
                throw new InvalidOperationException(
                    "Fatigue wake head-shake driver remained active outside its phase.");
            }
            Panels.Add(RenderTarget(headSample));
            UpperPoseSamples.Add(upperProbes
                .Select(item => item.localRotation)
                .ToArray());
            maximumElapsedSeconds = Mathf.Max(
                maximumElapsedSeconds,
                absolutePhase - captureCycleStartAbsolutePhase + phaseElapsed);
            RequireRootUnchanged();
            if (Panels.Count == PanelCount)
                FinishCapture();
        }

        private static Texture2D RenderTarget(bool headCloseUp)
        {
            const int width = 240;
            const int height = 310;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject cameraObject = new GameObject(
                "FatigueHeadShake_ReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "FatigueHeadShake_ReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            Renderer[] otherRenderers = UnityEngine.Object
                .FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] otherStates = otherRenderers
                .Select(renderer => renderer.forceRenderingOff)
                .ToArray();
            try
            {
                foreach (Renderer renderer in otherRenderers)
                    renderer.forceRenderingOff = true;
                Vector3 viewDirection = (
                    target.transform.forward * 0.98f +
                    target.transform.right * 0.10f +
                    Vector3.up * 0.03f).normalized;
                float distance = Mathf.Max(4f, bounds.extents.magnitude * 3f);
                Vector3 focus = headCloseUp ? head.position : bounds.center;
                camera.transform.position = focus + viewDirection * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                float aspect = width / (float)height;
                camera.orthographicSize = headCloseUp
                    ? 0.72f
                    : Mathf.Max(
                        bounds.extents.y * 1.10f,
                        bounds.extents.x / aspect * 1.10f,
                        0.85f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 150f, 0f);

                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGBA32,
                    false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return texture;
            }
            catch
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                for (int index = 0; index < otherRenderers.Length; index++)
                {
                    if (otherRenderers[index] != null)
                        otherRenderers[index].forceRenderingOff = otherStates[index];
                }
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FinishCapture()
        {
            RequireRootUnchanged();
            float maximumUpperAngle = 0f;
            Quaternion[] first = UpperPoseSamples[0];
            foreach (Quaternion[] sample in UpperPoseSamples.Skip(1))
                for (int index = 0; index < first.Length; index++)
                    maximumUpperAngle = Mathf.Max(
                        maximumUpperAngle,
                        Quaternion.Angle(first[index], sample[index]));
            if (maximumUpperAngle < 0.1f)
                throw new InvalidOperationException(
                    "Fatigue upper-body motion was not preserved across the cycle.");
            if (observedMinimumYaw > -7f ||
                observedMaximumYaw < 7f ||
                observedMaximumPitch < 7f)
                throw new InvalidOperationException(
                    "Fatigue wake head-shake range was not observed in natural playback.");
            Texture2D sheet = CombinePanels();
            try
            {
                FatigueHeadShakeAnimationSetupTools.WriteFinalEvidence(
                    sheet,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0),
                    maximumElapsedSeconds,
                    observedMinimumYaw,
                    observedMaximumYaw,
                    observedMaximumPitch);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                CleanupPanels();
            }
            SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != PanelCount)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake review requires fourteen frames.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * 5,
                height * 3,
                TextureFormat.RGBA32,
                false);
            sheet.SetPixels32(Enumerable.Repeat(
                new Color32(0, 0, 0, 255),
                sheet.width * sheet.height).ToArray());
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(
                    index % 5 * width,
                    (2 - index / 5) * height,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void RequireRootUnchanged()
        {
            if (Vector3.Distance(
                    initialLocalPosition,
                    target.transform.localPosition) > 0.0001f ||
                Quaternion.Angle(
                    initialLocalRotation,
                    target.transform.localRotation) > 0.01f ||
                Vector3.Distance(
                    initialLocalScale,
                    target.transform.localScale) > 0.0001f)
                throw new InvalidOperationException(
                    "Fatigue_HeadShake root changed during natural playback.");
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Fatigue_HeadShake natural Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
            {
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            }
            Panels.Clear();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            headShakeDriver = null;
            head = null;
            upperProbes = null;
            UpperPoseSamples.Clear();
            captureCycleStartAbsolutePhase = -1;
            maximumElapsedSeconds = 0f;
            observedMinimumYaw = 0f;
            observedMaximumYaw = 0f;
            observedMaximumPitch = 0f;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }
}
