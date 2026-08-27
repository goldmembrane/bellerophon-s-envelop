using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerJumpAnimationTool
    {
        internal const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string LayoutRootName = "PlayerAnimationLayout";
        internal const string TargetName = "Player_Jump";
        internal const string StateName = "PlayerJump";
        internal const string SourcePath =
            "Assets/_Project/Art/Player/Animations/Player_Jump_Mixamo.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Jump.controller";
        internal const string ExpectedTakeName = "mixamo.com";

        [MenuItem("Bellerophon/Player/Apply Jump Embedded Animation")]
        internal static void Apply()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before Player_Jump apply.");
            }

            Transform target = RequireTarget(scene);
            Vector3 positionBefore = target.position;
            Quaternion rotationBefore = target.rotation;
            Vector3 scaleBefore = target.localScale;
            Dictionary<string, string> otherAnimatorStates =
                CaptureOtherAnimatorStates(target);

            ConfigureDirectSourceImporter();
            AnimationClip clip = LoadSingleSourceClip();
            if (!clip.isLooping)
            {
                throw new InvalidOperationException(
                    "The embedded jump clip did not retain Loop Time.");
            }

            VerifyAllTransformBindingsExist(clip, target);
            AnimatorController controller = ConfigureController(clip);
            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = target.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorSceneManager.SaveScene(scene);

            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Player_Jump Animator connection or Apply Root Motion differs.");
            }

            if (Vector3.Distance(target.position, positionBefore) > 0.000001f ||
                Quaternion.Angle(target.rotation, rotationBefore) > 0.000001f ||
                Vector3.Distance(target.localScale, scaleBefore) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Player_Jump root Transform changed during connection.");
            }

            RequireEqual(
                otherAnimatorStates,
                CaptureOtherAnimatorStates(target),
                "Another Player animation instance changed.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp remained dirty after Player_Jump save.");
            }

            Debug.Log(
                "Player_Jump embedded Mixamo animation connected." +
                " Take=" + clip.name +
                ", Duration=" + Num(clip.length) +
                ", FrameRate=" + Num(clip.frameRate) +
                ", Loop=True" +
                ", ApplyRootMotion=False" +
                ", SourceClipDirect=True" +
                ", Retargeting=False" +
                ", DerivedClip=False" +
                ", OtherPlayersUnchanged=True.");
        }

        internal static Transform RequireTarget(Scene scene)
        {
            Transform[] layoutRoots = scene.GetRootGameObjects()
                .Where(root => root.name == LayoutRootName)
                .Select(root => root.transform)
                .ToArray();
            if (layoutRoots.Length != 1)
            {
                throw new InvalidOperationException(
                    "PlayerAnimationLayout root count differs.");
            }

            Transform[] targets = Enumerable.Range(0, layoutRoots[0].childCount)
                .Select(layoutRoots[0].GetChild)
                .Where(child => child.name == TargetName)
                .ToArray();
            if (targets.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Jump instance count differs.");
            }

            return targets[0];
        }

        internal static Transform FindUniqueBone(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    StripNamespace(item.name),
                    name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Jump bone count differs: " + name + ".");
            }

            return matches[0];
        }

        private static void ConfigureDirectSourceImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter ??
                                     throw new InvalidOperationException(
                                         "Player_Jump Mixamo FBX is not imported.");
            if (!importer.importAnimation)
            {
                throw new InvalidOperationException(
                    "Player_Jump Mixamo FBX has animation import disabled.");
            }

            ModelImporterClipAnimation[] defaultClips =
                importer.defaultClipAnimations ??
                Array.Empty<ModelImporterClipAnimation>();
            if (defaultClips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one embedded jump Take; found " +
                    defaultClips.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            ModelImporterClipAnimation sourceTake = defaultClips[0];
            if (!string.Equals(
                    sourceTake.takeName,
                    ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The embedded Take is not the investigated Mixamo Take. Actual=" +
                    sourceTake.takeName + ".");
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.resampleCurves = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            sourceTake.name = sourceTake.takeName;
            sourceTake.loopTime = true;
            importer.clipAnimations = new[] { sourceTake };
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadSingleSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourcePath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1 ||
                !string.Equals(
                    clips[0].name,
                    ExpectedTakeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The imported FBX does not expose exactly the investigated jump Take.");
            }

            if (!string.Equals(
                    AssetDatabase.GetAssetPath(clips[0]),
                    SourcePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Player_Jump does not use the direct FBX source clip.");
            }

            return clips[0];
        }

        private static AnimatorController ConfigureController(AnimationClip clip)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            }

            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player_Jump controller layer count differs.");
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState[] namedStates = stateMachine.states
                .Select(child => child.state)
                .Where(state => state != null && state.name == StateName)
                .ToArray();
            if (namedStates.Length > 1)
            {
                throw new InvalidOperationException(
                    "Player_Jump controller contains duplicate states.");
            }

            AnimatorState state = namedStates.Length == 1
                ? namedStates[0]
                : stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.mirror = false;
            state.cycleOffset = 0f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void VerifyAllTransformBindingsExist(
            AnimationClip clip,
            Transform target)
        {
            string[] missing = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct()
                .Where(path => !string.IsNullOrEmpty(path) && target.Find(path) == null)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    "The embedded jump clip paths do not match Player_Jump. " +
                    "Retargeting is prohibited. Missing=" +
                    string.Join(", ", missing) + ".");
            }
        }

        private static Dictionary<string, string> CaptureOtherAnimatorStates(
            Transform target)
        {
            Transform layoutRoot = target.parent ??
                                   throw new InvalidOperationException(
                                       "Player_Jump has no layout parent.");
            return Enumerable.Range(0, layoutRoot.childCount)
                .Select(layoutRoot.GetChild)
                .Where(child => child != target)
                .ToDictionary(
                    child => child.name,
                    child =>
                    {
                        Animator animator = child.GetComponent<Animator>();
                        return animator == null
                            ? "none"
                            : string.Join(
                                "|",
                                animator.enabled,
                                animator.applyRootMotion,
                                AssetDatabase.GetAssetPath(
                                    animator.runtimeAnimatorController));
                    },
                    StringComparer.Ordinal);
        }

        private static void RequireEqual(
            IReadOnlyDictionary<string, string> expected,
            IReadOnlyDictionary<string, string> actual,
            string message)
        {
            if (expected.Count != actual.Count ||
                expected.Any(pair =>
                    !actual.TryGetValue(pair.Key, out string value) ||
                    !string.Equals(pair.Value, value, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string StripNamespace(string value)
        {
            int separator = value.LastIndexOf(':');
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active for Player_Jump apply.");
            }

            return scene;
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
