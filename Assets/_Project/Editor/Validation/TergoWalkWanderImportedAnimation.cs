using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.TergoCargoRunScene
{
    internal static class TergoWalkWanderImportedAnimation
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Tergo Enemy Placement";
        private const string IdleRootName = "Tergo_01_Idle";
        private const string WalkRootName = "Tergo_02_Walk_Wander";
        private const string EyeContainerName = "TergoApprovedEyes";
        private const string TergoModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx";
        private const string AnimationFolderPath = "Assets/_Project/Art/Enemies/Tergo/Animations";
        private const string IdleBreathingControllerPath = AnimationFolderPath + "/Tergo_Idle_Breathing.controller";
        private const string WalkImportedClipPath = AnimationFolderPath + "/Tergo_Walk_Wander_FromFbx.anim";
        private const string WalkImportedControllerPath = AnimationFolderPath + "/Tergo_Walk_Wander_FromFbx.controller";
        private const string WalkImportedClipName = "Tergo_Walk_Wander_FromFbx";

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Walk Wander Imported Animation")]
        public static void ApplyTergoWalkWanderImportedAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);

            var walkTransformState = TransformState.Capture(walkRoot);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            RequireIdlePreserved(idleRoot, idleController);
            RequireNoConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);

            var importedClips = LoadImportedAnimationClips();
            var importedClip = SelectWalkClip(importedClips);
            var copiedClip = EnsureCopiedLoopClip(importedClip);
            var controller = EnsureWalkController(copiedClip);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(TergoModelAssetPath).OfType<Avatar>().FirstOrDefault();

            var animator = walkRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = walkRoot.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            if (!walkTransformState.Matches(walkRoot))
            {
                throw new InvalidOperationException(WalkRootName + " transform changed while applying imported walk animation.");
            }

            var idleConfiguredAnimators = CountConfiguredAnimators(idleRoot);
            var walkConfiguredAnimators = CountConfiguredAnimators(walkRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);
            if (walkConfiguredAnimators != 1)
            {
                throw new InvalidOperationException(
                    WalkRootName + " must have exactly one configured Animator. Count=" +
                    walkConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            if (otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Only idle and walk Tergo objects may have animation controllers now. OtherConfiguredAnimators=" +
                    otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(walkRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    WalkRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo walk animation.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoWalkWanderImportedAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + WalkRootName +
                ", ImportedClip=" + importedClip.name +
                ", ImportedClipLength=" + importedClip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ImportedClips=" + FormatClipNames(importedClips) +
                ", CopiedClip=" + WalkImportedClipPath +
                ", Controller=" + WalkImportedControllerPath +
                ", AvatarAssigned=" + (avatar != null).ToString(CultureInfo.InvariantCulture) +
                ", ApplyRootMotion=False" +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", IdleConfiguredAnimators=" + idleConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", WalkConfiguredAnimators=" + walkConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", RootTransformUnchanged=True" +
                ", SourceFbxModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Walk Wander Imported Animation")]
        public static void ValidateTergoWalkWanderImportedAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var walkRoot = RequireChild(placementRoot.transform, WalkRootName);
            var idleController = RequireAsset<AnimatorController>(IdleBreathingControllerPath);
            var clip = RequireAsset<AnimationClip>(WalkImportedClipPath);
            var controller = RequireAsset<AnimatorController>(WalkImportedControllerPath);

            RequireIdlePreserved(idleRoot, idleController);

            var animator = walkRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(WalkRootName + " is not using the imported walk controller.");
            }

            if (animator.applyRootMotion)
            {
                throw new InvalidOperationException(WalkRootName + " must keep root motion disabled for review placement.");
            }

            if (!ControllerUsesClip(controller, clip))
            {
                throw new InvalidOperationException("Tergo walk controller is not bound to " + WalkImportedClipPath);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || clip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException("Tergo walk copied clip must loop.");
            }

            var curveBindingCount = AnimationUtility.GetCurveBindings(clip).Length;
            var objectBindingCount = AnimationUtility.GetObjectReferenceCurveBindings(clip).Length;
            if (clip.length <= 0.01f || (curveBindingCount + objectBindingCount) == 0)
            {
                throw new InvalidOperationException(
                    "Tergo walk copied clip has no usable animation data. Length=" +
                    clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                    ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture));
            }

            var sampleTransformChanged = SampleClipChangesTransforms(clip, walkRoot);
            if (!sampleTransformChanged)
            {
                throw new InvalidOperationException("Tergo walk copied clip did not change any target transforms when sampled.");
            }

            var idleConfiguredAnimators = CountConfiguredAnimators(idleRoot);
            var walkConfiguredAnimators = CountConfiguredAnimators(walkRoot);
            var otherConfiguredAnimators = CountConfiguredTergoAnimatorsExcept(placementRoot.transform, IdleRootName, WalkRootName);
            if (walkConfiguredAnimators != 1 || otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected Tergo configured animator counts. Walk=" +
                    walkConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                    ", Other=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(walkRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    WalkRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoWalkWanderImportedAnimationValidated" +
                ", Target=" + PlacementRootName + "/" + WalkRootName +
                ", Clip=" + WalkImportedClipPath +
                ", Controller=" + WalkImportedControllerPath +
                ", ClipLength=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) +
                ", CurveBindings=" + curveBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", ObjectBindings=" + objectBindingCount.ToString(CultureInfo.InvariantCulture) +
                ", SampleTransformChanged=True" +
                ", LoopTime=True" +
                ", ApplyRootMotion=False" +
                ", IdleConfiguredAnimators=" + idleConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", WalkConfiguredAnimators=" + walkConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", StaticAndRemainingTergoUnanimated=True");
        }

        private static AnimationClip[] LoadImportedAnimationClips()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(TergoModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    clip != null &&
                    !clip.empty &&
                    clip.length > 0.01f &&
                    !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (clips.Length == 0)
            {
                throw new InvalidOperationException("No imported animation clips were found in " + TergoModelAssetPath);
            }

            return clips;
        }

        private static AnimationClip SelectWalkClip(AnimationClip[] importedClips)
        {
            return importedClips
                .OrderByDescending(clip => GetWalkClipScore(clip.name))
                .ThenByDescending(clip => clip.length)
                .ThenBy(clip => clip.name, StringComparer.Ordinal)
                .First();
        }

        private static int GetWalkClipScore(string clipName)
        {
            var lower = (clipName ?? string.Empty).ToLowerInvariant();
            var score = 0;
            if (lower.Contains("walk"))
            {
                score += 100;
            }

            if (lower.Contains("wander"))
            {
                score += 90;
            }

            if (lower.Contains("move"))
            {
                score += 80;
            }

            if (lower.Contains("locomotion"))
            {
                score += 70;
            }

            if (lower.Contains("take"))
            {
                score += 10;
            }

            return score;
        }

        private static AnimationClip EnsureCopiedLoopClip(AnimationClip importedClip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkImportedClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = WalkImportedClipName
                };
                AssetDatabase.CreateAsset(clip, WalkImportedClipPath);
            }

            EditorUtility.CopySerialized(importedClip, clip);
            clip.name = WalkImportedClipName;
            clip.wrapMode = WrapMode.Loop;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureWalkController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(WalkImportedControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(WalkImportedControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == WalkImportedClipName);
            if (state == null)
            {
                state = stateMachine.AddState(WalkImportedClipName);
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static bool ControllerUsesClip(AnimatorController controller, AnimationClip clip)
        {
            return controller.animationClips.Any(candidate =>
                candidate == clip ||
                string.Equals(AssetDatabase.GetAssetPath(candidate), WalkImportedClipPath, StringComparison.Ordinal));
        }

        private static bool SampleClipChangesTransforms(AnimationClip clip, Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var originalStates = transforms.Select(TransformState.Capture).ToArray();
            try
            {
                clip.SampleAnimation(root.gameObject, 0f);
                var startStates = transforms.Select(TransformState.Capture).ToArray();
                clip.SampleAnimation(root.gameObject, Mathf.Clamp(clip.length * 0.5f, 0f, Mathf.Max(clip.length - 0.001f, 0f)));

                for (var index = 0; index < transforms.Length; index++)
                {
                    if (!startStates[index].Matches(transforms[index]))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    originalStates[index].ApplyTo(transforms[index]);
                }
            }
        }

        private static void RequireIdlePreserved(Transform idleRoot, AnimatorController idleController)
        {
            var animator = idleRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != idleController)
            {
                throw new InvalidOperationException(IdleRootName + " must keep the idle breathing controller before applying walk.");
            }

            var configuredCount = CountConfiguredAnimators(idleRoot);
            if (configuredCount != 1)
            {
                throw new InvalidOperationException(
                    IdleRootName + " must keep exactly one configured Animator. Count=" +
                    configuredCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void RequireNoConfiguredTergoAnimatorsExcept(Transform placementRoot, params string[] allowedRootNames)
        {
            var count = CountConfiguredTergoAnimatorsExcept(placementRoot, allowedRootNames);
            if (count != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected configured Tergo animators outside approved roots. Count=" +
                    count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static int CountConfiguredTergoAnimatorsExcept(Transform placementRoot, params string[] allowedRootNames)
        {
            var count = 0;
            for (var index = 0; index < placementRoot.childCount; index++)
            {
                var child = placementRoot.GetChild(index);
                if (!child.name.StartsWith("Tergo_", StringComparison.Ordinal) ||
                    allowedRootNames.Contains(child.name, StringComparer.Ordinal))
                {
                    continue;
                }

                count += CountConfiguredAnimators(child);
            }

            return count;
        }

        private static int CountConfiguredAnimators(Transform root)
        {
            return root.GetComponentsInChildren<Animator>(true)
                .Count(animator => animator.runtimeAnimatorController != null);
        }

        private static int CountDescendantsByName(Transform root, string objectName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Count(transform => string.Equals(transform.name, objectName, StringComparison.Ordinal));
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing asset: " + path);
            }

            return asset;
        }

        private static GameObject RequireSceneObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                return target;
            }

            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!string.Equals(candidate.name, objectName, StringComparison.Ordinal) ||
                    EditorUtility.IsPersistent(candidate) ||
                    !candidate.scene.IsValid() ||
                    !string.Equals(candidate.scene.path, CargoRunScenePath, StringComparison.Ordinal))
                {
                    continue;
                }

                return candidate;
            }

            throw new InvalidOperationException("Missing scene object: " + objectName);
        }

        private static Transform RequireChild(Transform root, string childName)
        {
            var child = root.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException("Missing child under " + root.name + ": " + childName);
            }

            return child;
        }

        private static string FormatClipNames(AnimationClip[] clips)
        {
            return string.Join(
                "|",
                clips.Select(clip =>
                    clip.name + "(" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) + "s)"));
        }

        private readonly struct TransformState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            private TransformState(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                this.localPosition = localPosition;
                this.localRotation = localRotation;
                this.localScale = localScale;
            }

            public static TransformState Capture(Transform transform)
            {
                return new TransformState(transform.localPosition, transform.localRotation, transform.localScale);
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(localPosition, transform.localPosition) <= 0.0001f &&
                       Quaternion.Angle(localRotation, transform.localRotation) <= 0.001f &&
                       Vector3.Distance(localScale, transform.localScale) <= 0.0001f;
            }

            public void ApplyTo(Transform transform)
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }
    }
}
