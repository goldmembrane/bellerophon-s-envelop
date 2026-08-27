using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PlayerWalkDiagonalBlendTreeTool
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetName = "Player_Walk_Diagonal";
        internal const string ForwardControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward.controller";
        internal const string SidestepControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Sidestep.controller";
        internal const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/Player_Walk_Diagonal.controller";
        internal const string StateName = "PlayerWalkDiagonalForwardBlend";
        internal const string BlendTreeName = "PlayerWalkDiagonalForward50";
        internal const string BlendParameter = "ForwardSidestepBlend";
        internal const float BlendValue = 0.5f;
        internal const string FinalCapturePath =
            "docs/validation/player_walk_diagonal_forward_blend_final.png";

        [MenuItem("Bellerophon/Player/Apply Walk Diagonal Forward Blend")]
        internal static void Apply()
        {
            RequireCleanScene();

            AnimatorController forwardController = RequireController(ForwardControllerPath);
            AnimatorController sidestepController = RequireController(SidestepControllerPath);
            AnimationClip forwardClip = RequireSingleDefaultClip(forwardController, "Player_Walk_Forward");
            AnimationClip sidestepClip = RequireSingleDefaultClip(sidestepController, "Player_Sidestep");
            string forwardClipPath = AssetDatabase.GetAssetPath(forwardClip);
            string sidestepClipPath = AssetDatabase.GetAssetPath(sidestepClip);
            Dictionary<string, string> sourceHashes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ForwardControllerPath] = HashFile(ForwardControllerPath),
                [SidestepControllerPath] = HashFile(SidestepControllerPath),
                [forwardClipPath] = HashFile(forwardClipPath),
                [sidestepClipPath] = HashFile(sidestepClipPath)
            };

            Scene scene = OpenCargoRunScene();
            GameObject target = FindUniqueTarget(scene);
            string otherObjectsBefore = CaptureOtherObjects(scene, target);
            string rendererAssetsBefore = CaptureRendererAssets(target);
            string prefabPathBefore = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            Vector3 targetPositionBefore = target.transform.position;
            Quaternion targetRotationBefore = target.transform.rotation;
            Vector3 targetScaleBefore = target.transform.localScale;

            VerifyAllTransformBindingsExist(forwardClip, target, "Forward");
            VerifyAllTransformBindingsExist(sidestepClip, target, "Sidestep");
            AnimatorController controller = CreateOrUpdateController(forwardClip, sidestepClip);
            Animator animator = ConfigureAnimator(target, controller);

            AssertAnimatorConfiguration(animator, controller, forwardClip, sidestepClip);
            AssertUnchanged(target.transform.position, targetPositionBefore, "Player_Walk_Diagonal world position");
            AssertUnchanged(target.transform.rotation, targetRotationBefore, "Player_Walk_Diagonal world rotation");
            AssertUnchanged(target.transform.localScale, targetScaleBefore, "Player_Walk_Diagonal local scale");
            AssertEqual(rendererAssetsBefore, CaptureRendererAssets(target), "model/skin/material asset connection");
            AssertEqual(prefabPathBefore, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target), "prefab asset path");
            AssertEqual(otherObjectsBefore, CaptureOtherObjects(scene, target), "objects outside Player_Walk_Diagonal");

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            foreach (KeyValuePair<string, string> pair in sourceHashes)
            {
                AssertEqual(pair.Value, HashFile(pair.Key), $"source asset hash {pair.Key}");
            }

            Debug.Log(
                $"[PlayerWalkDiagonal] Applied exact 50:50 forward/right-sidestep Blend Tree to {TargetName}. " +
                $"Forward={forwardClipPath}, Sidestep={sidestepClipPath}, Parameter={BlendParameter}, " +
                $"Default={BlendValue:R}, child speeds=1/1, cycle offsets=0/0, mirrors=False/False, " +
                "ApplyRootMotion=False. Source clips and source controllers were unchanged.");
        }

        [MenuItem("Bellerophon/Player/Capture Walk Diagonal Forward Blend Final")]
        internal static void CaptureFinal()
        {
            RequireCleanScene();
            const string metricsPath =
                "docs/validation/player_walk_diagonal_forward_blend_review_metrics.json";
            string absoluteMetricsPath = Path.GetFullPath(metricsPath);
            if (!File.Exists(absoluteMetricsPath) ||
                !File.ReadAllText(absoluteMetricsPath).Contains("\"passedNumericChecks\": true"))
            {
                throw new InvalidOperationException(
                    "The direct two-loop diagonal Blend Tree review must pass before final capture composition.");
            }

            int[] phaseFrames = { 0, 8, 15, 23 };
            Texture2D[] phaseTextures = new Texture2D[phaseFrames.Length];
            Texture2D strip = null;
            try
            {
                int sourceWidth = 0;
                int sourceHeight = 0;
                for (int i = 0; i < phaseFrames.Length; i++)
                {
                    string framePath = Path.GetFullPath(
                        $"Logs/PlayerWalkDiagonalForwardBlendReviewFrames/frame_{phaseFrames[i]:000}.png");
                    if (!File.Exists(framePath))
                    {
                        throw new FileNotFoundException("A validated diagonal review phase frame is missing.", framePath);
                    }

                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(framePath), false))
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                        throw new InvalidOperationException($"Could not load validated phase frame: {framePath}");
                    }

                    phaseTextures[i] = texture;
                    sourceWidth = sourceWidth == 0 ? texture.width : sourceWidth;
                    sourceHeight = sourceHeight == 0 ? texture.height : sourceHeight;
                    if (texture.width != sourceWidth || texture.height != sourceHeight || texture.width % 2 != 0)
                    {
                        throw new InvalidOperationException(
                            "Validated diagonal review frames do not share the expected composite dimensions.");
                    }
                }

                int panelWidth = sourceWidth / 2;
                strip = new Texture2D(panelWidth * phaseFrames.Length, sourceHeight, TextureFormat.RGB24, false);
                for (int i = 0; i < phaseTextures.Length; i++)
                {
                    Color[] pixels = phaseTextures[i].GetPixels(0, 0, panelWidth, sourceHeight);
                    strip.SetPixels(panelWidth * i, 0, panelWidth, sourceHeight, pixels);
                }

                strip.Apply(false, false);
                string absolutePath = Path.GetFullPath(FinalCapturePath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ??
                    throw new InvalidOperationException("Final capture directory is unavailable."));
                File.WriteAllBytes(absolutePath, strip.EncodeToPNG());
                Debug.Log(
                    $"[PlayerWalkDiagonal] Final four-phase strip composed from validated Play Mode frames " +
                    $"{string.Join(",", phaseFrames)}: {absolutePath}");
            }
            finally
            {
                foreach (Texture2D phaseTexture in phaseTextures)
                {
                    if (phaseTexture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(phaseTexture);
                    }
                }

                if (strip != null)
                {
                    UnityEngine.Object.DestroyImmediate(strip);
                }
            }
        }

        internal static Scene OpenCargoRunScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid() || !string.Equals(active.path, ScenePath, StringComparison.Ordinal))
            {
                active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            return active;
        }

        internal static GameObject FindUniqueTarget(Scene scene)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => string.Equals(item.name, TargetName, StringComparison.Ordinal))
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly one {TargetName}; found {matches.Length}.");
            }

            return matches[0];
        }

        internal static Transform FindHips(GameObject root)
        {
            return FindUniqueBone(root, "Hips");
        }

        internal static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && !item.forceRenderingOff)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException($"No enabled renderer found under {root.name}.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        internal static void AssertAnimatorConfiguration(
            Animator animator,
            AnimatorController controller,
            AnimationClip expectedForward,
            AnimationClip expectedSidestep)
        {
            if (!animator.enabled || animator.applyRootMotion || animator.runtimeAnimatorController != controller ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException("Player_Walk_Diagonal Animator connection is not exact.");
            }

            AnimatorControllerParameter[] parameters = controller.parameters;
            if (parameters.Length != 1 ||
                !string.Equals(parameters[0].name, BlendParameter, StringComparison.Ordinal) ||
                parameters[0].type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameters[0].defaultFloat, BlendValue))
            {
                throw new InvalidOperationException("Diagonal Blend Tree must have one float parameter defaulted to 0.5.");
            }

            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            if (states.Length != 1 ||
                !string.Equals(states[0].state.name, StateName, StringComparison.Ordinal) ||
                !Mathf.Approximately(states[0].state.speed, 1f) || states[0].state.mirror ||
                !(states[0].state.motion is BlendTree tree))
            {
                throw new InvalidOperationException(
                    "Diagonal controller must contain exactly one unmirrored speed-1 Blend Tree state.");
            }

            ChildMotion[] children = tree.children;
            if (!string.Equals(tree.name, BlendTreeName, StringComparison.Ordinal) ||
                tree.blendType != BlendTreeType.Simple1D ||
                !string.Equals(tree.blendParameter, BlendParameter, StringComparison.Ordinal) ||
                tree.useAutomaticThresholds || children.Length != 2 ||
                children[0].motion != expectedForward || children[1].motion != expectedSidestep ||
                !Mathf.Approximately(children[0].threshold, 0f) ||
                !Mathf.Approximately(children[1].threshold, 1f) ||
                !Mathf.Approximately(children[0].timeScale, 1f) ||
                !Mathf.Approximately(children[1].timeScale, 1f) ||
                !Mathf.Approximately(children[0].cycleOffset, 0f) ||
                !Mathf.Approximately(children[1].cycleOffset, 0f) ||
                children[0].mirror || children[1].mirror)
            {
                throw new InvalidOperationException(
                    "Diagonal Blend Tree children must be the exact Forward/Sidestep motions at 50:50 without timing changes.");
            }
        }

        internal static AnimationClip RequireCurrentForwardClip()
        {
            return RequireSingleDefaultClip(RequireController(ForwardControllerPath), "Player_Walk_Forward");
        }

        internal static AnimationClip RequireCurrentSidestepClip()
        {
            return RequireSingleDefaultClip(RequireController(SidestepControllerPath), "Player_Sidestep");
        }

        private static void RequireCleanScene()
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                throw new InvalidOperationException(
                    "The active scene has unsaved changes. Save or discard them before applying Player_Walk_Diagonal.");
            }
        }

        private static AnimatorController RequireController(string path)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                throw new InvalidOperationException($"Required AnimatorController is missing: {path}");
            }

            return controller;
        }

        private static AnimationClip RequireSingleDefaultClip(AnimatorController controller, string label)
        {
            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException($"{label} controller must have exactly one layer.");
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ChildAnimatorState[] states = stateMachine.states;
            if (states.Length != 1 || stateMachine.defaultState != states[0].state ||
                !(states[0].state.motion is AnimationClip clip) ||
                !Mathf.Approximately(states[0].state.speed, 1f) || states[0].state.mirror)
            {
                throw new InvalidOperationException(
                    $"{label} controller must expose one exact unmirrored speed-1 AnimationClip as its default Motion.");
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
            {
                throw new InvalidOperationException($"{label} source Motion is not configured to loop.");
            }

            return clip;
        }

        private static void VerifyAllTransformBindingsExist(AnimationClip clip, GameObject target, string label)
        {
            string[] missing = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => binding.type == typeof(Transform))
                .Select(binding => binding.path)
                .Distinct()
                .Where(path => !string.IsNullOrEmpty(path) && target.transform.Find(path) == null)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"{label} Motion has transform paths missing from {TargetName}: {string.Join(", ", missing)}");
            }
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip forwardClip,
            AnimationClip sidestepClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (BlendTree existingTree in AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>().ToArray())
            {
                UnityEngine.Object.DestroyImmediate(existingTree, true);
            }

            controller.parameters = new[]
            {
                new AnimatorControllerParameter
                {
                    name = BlendParameter,
                    type = AnimatorControllerParameterType.Float,
                    defaultFloat = BlendValue
                }
            };

            BlendTree tree = new BlendTree
            {
                name = BlendTreeName,
                blendType = BlendTreeType.Simple1D,
                blendParameter = BlendParameter,
                useAutomaticThresholds = false,
                minThreshold = 0f,
                maxThreshold = 1f
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.children = new[]
            {
                new ChildMotion
                {
                    motion = forwardClip,
                    threshold = 0f,
                    timeScale = 1f,
                    cycleOffset = 0f,
                    mirror = false,
                    directBlendParameter = string.Empty
                },
                new ChildMotion
                {
                    motion = sidestepClip,
                    threshold = 1f,
                    timeScale = 1f,
                    cycleOffset = 0f,
                    mirror = false,
                    directBlendParameter = string.Empty
                }
            };

            AnimatorState state = stateMachine.AddState(StateName);
            state.motion = tree;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Animator ConfigureAnimator(GameObject target, RuntimeAnimatorController controller)
        {
            Animator[] animators = target.GetComponentsInChildren<Animator>(true);
            Animator animator;
            if (animators.Length == 0)
            {
                animator = target.AddComponent<Animator>();
            }
            else if (animators.Length == 1 && animators[0].gameObject == target)
            {
                animator = animators[0];
            }
            else
            {
                throw new InvalidOperationException(
                    $"{TargetName} Animator placement is ambiguous; expected zero or one root Animator, found {animators.Length}.");
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            return animator;
        }

        private static string CaptureRendererAssets(GameObject target)
        {
            return string.Join("\n", target.GetComponentsInChildren<Renderer>(true)
                .OrderBy(renderer => AnimationUtility.CalculateTransformPath(renderer.transform, target.transform),
                    StringComparer.Ordinal)
                .Select(renderer =>
                {
                    string path = AnimationUtility.CalculateTransformPath(renderer.transform, target.transform);
                    string mesh = renderer is SkinnedMeshRenderer skinned
                        ? AssetDatabase.GetAssetPath(skinned.sharedMesh)
                        : renderer is MeshRenderer && renderer.GetComponent<MeshFilter>() != null
                            ? AssetDatabase.GetAssetPath(renderer.GetComponent<MeshFilter>().sharedMesh)
                            : string.Empty;
                    string materials = string.Join("|", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath));
                    return $"{path}:{renderer.GetType().FullName}:{mesh}:{materials}";
                }));
        }

        private static string CaptureOtherObjects(Scene scene, GameObject excludedRoot)
        {
            return string.Join("\n", scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item != excludedRoot.transform && !item.IsChildOf(excludedRoot.transform))
                .Select(item =>
                {
                    string path = FullScenePath(item);
                    string components = string.Join(",", item.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName));
                    return $"{path}|{item.gameObject.activeSelf}|{item.localPosition:R}|{item.localRotation:R}|" +
                        $"{item.localScale:R}|{components}";
                })
                .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static string FullScenePath(Transform transform)
        {
            Stack<string> parts = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
            {
                parts.Push(current.name);
            }

            return string.Join("/", parts);
        }

        private static Transform FindUniqueBone(GameObject root, string exactNameWithoutNamespace)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    StripNamespace(item.name), exactNameWithoutNamespace, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one {exactNameWithoutNamespace} under {root.name}; found {matches.Length}.");
            }

            return matches[0];
        }

        private static string StripNamespace(string name)
        {
            int colon = name.LastIndexOf(':');
            return colon >= 0 ? name.Substring(colon + 1) : name;
        }

        private static string HashFile(string assetPath)
        {
            string absolutePath = Path.GetFullPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException("Required source asset is missing.", absolutePath);
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(absolutePath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static void AssertUnchanged(Vector3 actual, Vector3 expected, string label)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException($"{label} changed: expected {expected:R}, actual {actual:R}.");
            }
        }

        private static void AssertUnchanged(Quaternion actual, Quaternion expected, string label)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException($"{label} changed: expected {expected:R}, actual {actual:R}.");
            }
        }

        private static void AssertEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{label} changed unexpectedly.");
            }
        }
    }
}
