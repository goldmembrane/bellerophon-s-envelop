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
    internal static class TergoIdleBreathingAnimation
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Tergo Enemy Placement";
        private const string IdleRootName = "Tergo_01_Idle";
        private const string StaticRootName = "Tergo_00_Static_Review";
        private const string EyeContainerName = "TergoApprovedEyes";
        private const string TergoModelAssetPath = "Assets/_Project/Art/Enemies/Tergo/Models/tergo.fbx";
        private const string AnimationFolderPath = "Assets/_Project/Art/Enemies/Tergo/Animations";
        private const string GeneratedFolderPath = "Assets/_Project/Art/Enemies/Tergo/Generated";
        private const string IdleBreathingClipPath = AnimationFolderPath + "/Tergo_Idle_Breathing.anim";
        private const string IdleBreathingControllerPath = AnimationFolderPath + "/Tergo_Idle_Breathing.controller";
        private const string IdleBreathingBodyMeshPath = GeneratedFolderPath + "/Tergo_Idle_Breathing_BodyMesh.asset";
        private const string IdleBreathingBlendShapeName = "Idle_Breathing_BodyMorph";

        [MenuItem("Bellerophon/Enemies/Tergo/Apply Idle Breathing Animation")]
        public static void ApplyTergoIdleBreathingAnimation()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);

            var transformState = TransformState.Capture(idleRoot);
            var staticAnimatorCountBefore = CountConfiguredAnimators(RequireChild(placementRoot.transform, StaticRootName));
            var otherConfiguredAnimatorsBefore = CountOtherConfiguredTergoAnimators(placementRoot.transform);
            if (staticAnimatorCountBefore != 0 || otherConfiguredAnimatorsBefore != 0)
            {
                throw new InvalidOperationException(
                    "Only " + IdleRootName + " may receive an animation controller at this step. " +
                    "StaticConfiguredAnimators=" + staticAnimatorCountBefore.ToString(CultureInfo.InvariantCulture) +
                    ", OtherConfiguredAnimators=" + otherConfiguredAnimatorsBefore.ToString(CultureInfo.InvariantCulture));
            }

            var bodyRenderer = RequireBodySkinnedRenderer(idleRoot);
            var bodyRendererPath = AnimationUtility.CalculateTransformPath(bodyRenderer.transform, idleRoot);
            var bodyMesh = EnsureIdleBreathingBodyMesh(bodyRenderer.sharedMesh);
            bodyRenderer.sharedMesh = bodyMesh;
            bodyRenderer.localBounds = ExpandBounds(bodyMesh.bounds, 1.18f);
            EditorUtility.SetDirty(bodyRenderer);

            var clip = EnsureIdleBreathingClip(idleRoot, bodyRenderer);
            var controller = EnsureIdleBreathingController(clip);
            var animator = idleRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = idleRoot.gameObject.AddComponent<Animator>();
            }

            var avatar = AssetDatabase.LoadAllAssetsAtPath(TergoModelAssetPath).OfType<Avatar>().FirstOrDefault();
            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            if (!transformState.Matches(idleRoot))
            {
                throw new InvalidOperationException(IdleRootName + " transform changed while applying idle breathing.");
            }

            var otherConfiguredAnimatorsAfter = CountOtherConfiguredTergoAnimators(placementRoot.transform);
            if (otherConfiguredAnimatorsAfter != 0)
            {
                throw new InvalidOperationException(
                    "Unexpected non-idle Tergo animation controller count after applying idle breathing: " +
                    otherConfiguredAnimatorsAfter.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(idleRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    IdleRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp scene after applying Tergo idle breathing.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "TergoIdleBreathingAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + IdleRootName +
                ", Clip=" + IdleBreathingClipPath +
                ", Controller=" + IdleBreathingControllerPath +
                ", GeneratedBodyMesh=" + IdleBreathingBodyMeshPath +
                ", RendererPath=" + bodyRendererPath +
                ", BlendShape=" + IdleBreathingBlendShapeName +
                ", BlendShapeCurveBound=" + HasBlendShapeCurve(clip, bodyRendererPath).ToString(CultureInfo.InvariantCulture) +
                ", LoopTime=True" +
                ", LoopBlend=True" +
                ", TargetConfiguredAnimators=" + CountConfiguredAnimators(idleRoot).ToString(CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimatorsAfter.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", RootTransformUnchanged=True" +
                ", SourceFbxModified=False");
        }

        [MenuItem("Bellerophon/Enemies/Tergo/Validate Idle Breathing Animation")]
        public static void ValidateTergoIdleBreathingAnimation()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = RequireSceneObject(PlacementRootName);
            var idleRoot = RequireChild(placementRoot.transform, IdleRootName);
            var bodyRenderer = RequireBodySkinnedRenderer(idleRoot);
            var bodyRendererPath = AnimationUtility.CalculateTransformPath(bodyRenderer.transform, idleRoot);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleBreathingControllerPath);

            if (clip == null)
            {
                throw new InvalidOperationException("Missing Tergo idle breathing clip: " + IdleBreathingClipPath);
            }

            if (controller == null)
            {
                throw new InvalidOperationException("Missing Tergo idle breathing controller: " + IdleBreathingControllerPath);
            }

            var animator = idleRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException(IdleRootName + " is not using the Tergo idle breathing controller.");
            }

            var blendShapeIndex = bodyRenderer.sharedMesh != null
                ? bodyRenderer.sharedMesh.GetBlendShapeIndex(IdleBreathingBlendShapeName)
                : -1;
            if (blendShapeIndex < 0)
            {
                throw new InvalidOperationException("Tergo idle body renderer is missing blend shape: " + IdleBreathingBlendShapeName);
            }

            if (!HasBlendShapeCurve(clip, bodyRendererPath))
            {
                throw new InvalidOperationException("Tergo idle breathing clip has no bound blendShape curve.");
            }

            clip.SampleAnimation(idleRoot.gameObject, 0f);
            var startWeight = bodyRenderer.GetBlendShapeWeight(blendShapeIndex);
            clip.SampleAnimation(idleRoot.gameObject, 1.2f);
            var peakWeight = bodyRenderer.GetBlendShapeWeight(blendShapeIndex);
            clip.SampleAnimation(idleRoot.gameObject, 0f);

            if (startWeight > 0.1f || peakWeight < 99f)
            {
                throw new InvalidOperationException(
                    "Tergo idle breathing blend shape sample weights are invalid. Start=" +
                    startWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", Peak=" + peakWeight.ToString("0.###", CultureInfo.InvariantCulture));
            }

            var otherConfiguredAnimators = CountOtherConfiguredTergoAnimators(placementRoot.transform);
            if (otherConfiguredAnimators != 0)
            {
                throw new InvalidOperationException(
                    "Non-idle Tergo animation controllers must remain absent. Count=" +
                    otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture));
            }

            var eyeContainerCount = CountDescendantsByName(idleRoot, EyeContainerName);
            if (eyeContainerCount != 1)
            {
                throw new InvalidOperationException(
                    IdleRootName + " must keep exactly one generated eye container. Count=" +
                    eyeContainerCount.ToString(CultureInfo.InvariantCulture));
            }

            Debug.Log(
                "TergoIdleBreathingAnimationValidated" +
                ", Target=" + PlacementRootName + "/" + IdleRootName +
                ", Clip=" + IdleBreathingClipPath +
                ", Controller=" + IdleBreathingControllerPath +
                ", RendererPath=" + bodyRendererPath +
                ", BlendShape=" + IdleBreathingBlendShapeName +
                ", StartWeight=" + startWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                ", PeakWeight=" + peakWeight.ToString("0.###", CultureInfo.InvariantCulture) +
                ", OtherTergoConfiguredAnimators=" + otherConfiguredAnimators.ToString(CultureInfo.InvariantCulture) +
                ", EyeContainersPreserved=" + eyeContainerCount.ToString(CultureInfo.InvariantCulture) +
                ", StaticReviewUnanimated=True");
        }

        private static Mesh EnsureIdleBreathingBodyMesh(Mesh sourceMesh)
        {
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("Tergo idle body renderer has no source mesh.");
            }

            Directory.CreateDirectory(GeneratedFolderPath);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(IdleBreathingBodyMeshPath);
            if (existing != null && existing.GetBlendShapeIndex(IdleBreathingBlendShapeName) >= 0)
            {
                return existing;
            }

            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "Tergo_Idle_Breathing_BodyMesh";
            generated.ClearBlendShapes();
            AddBreathingBlendShape(generated);
            generated.RecalculateBounds();

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(IdleBreathingBodyMeshPath);
            }

            AssetDatabase.CreateAsset(generated, IdleBreathingBodyMeshPath);
            AssetDatabase.SaveAssets();
            return generated;
        }

        private static void AddBreathingBlendShape(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Tergo idle body mesh has no vertices for breathing blend shape.");
            }

            var bounds = mesh.bounds;
            var height = Mathf.Max(bounds.size.y, 0.0001f);
            var center = bounds.center;
            var deltaVertices = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];

            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var normalizedY = Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertex.y);
                var lowerMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.38f, normalizedY));
                var upperMask = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.86f, 0.98f, normalizedY));
                var chestBias = Mathf.Lerp(0.70f, 1.15f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.72f, normalizedY)));
                var mask = lowerMask * upperMask * chestBias;

                deltaVertices[index] = new Vector3(
                    (vertex.x - center.x) * 0.050f * mask,
                    height * 0.010f * mask,
                    (vertex.z - center.z) * 0.040f * mask);
            }

            mesh.AddBlendShapeFrame(IdleBreathingBlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
        }

        private static AnimationClip EnsureIdleBreathingClip(Transform idleRoot, SkinnedMeshRenderer bodyRenderer)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleBreathingClipPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "Tergo_Idle_Breathing",
                    frameRate = 30f
                };
                AssetDatabase.CreateAsset(clip, IdleBreathingClipPath);
            }

            clip.ClearCurves();
            clip.name = "Tergo_Idle_Breathing";
            clip.frameRate = 30f;
            clip.wrapMode = WrapMode.Loop;

            var path = AnimationUtility.CalculateTransformPath(bodyRenderer.transform, idleRoot);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + IdleBreathingBlendShapeName),
                CreateSmoothCurve(
                    new[] { 0f, 0.72f, 1.20f, 1.76f, 2.40f },
                    new[] { 0f, 62f, 100f, 44f, 0f }));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static AnimatorController EnsureIdleBreathingController(AnimationClip clip)
        {
            Directory.CreateDirectory(AnimationFolderPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleBreathingControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(IdleBreathingControllerPath);
            }

            if (controller.layers == null || controller.layers.Length == 0)
            {
                controller.AddLayer("Base Layer");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "Tergo_Idle_Breathing");
            if (state == null)
            {
                state = stateMachine.AddState("Tergo_Idle_Breathing");
            }

            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static SkinnedMeshRenderer RequireBodySkinnedRenderer(Transform root)
        {
            var renderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null && !IsGeneratedEyeObject(candidate.transform))
                .OrderByDescending(candidate => candidate.sharedMesh.vertexCount)
                .FirstOrDefault();
            if (renderer == null)
            {
                throw new InvalidOperationException(root.name + " has no body SkinnedMeshRenderer.");
            }

            return renderer;
        }

        private static bool HasBlendShapeCurve(AnimationClip clip, string rendererPath)
        {
            return AnimationUtility.GetCurveBindings(clip).Any(
                binding =>
                    string.Equals(binding.path, rendererPath, StringComparison.Ordinal) &&
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    string.Equals(
                        binding.propertyName,
                        "blendShape." + IdleBreathingBlendShapeName,
                        StringComparison.Ordinal));
        }

        private static AnimationCurve CreateSmoothCurve(float[] times, float[] values)
        {
            if (times.Length != values.Length)
            {
                throw new ArgumentException("Curve times and values must have the same length.");
            }

            var keyframes = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                keyframes[index] = new Keyframe(times[index], values[index]);
            }

            var curve = new AnimationCurve(keyframes);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Auto);
            }

            return curve;
        }

        private static Bounds ExpandBounds(Bounds bounds, float factor)
        {
            bounds.Expand(bounds.size * Mathf.Max(0f, factor - 1f));
            return bounds;
        }

        private static int CountOtherConfiguredTergoAnimators(Transform placementRoot)
        {
            var count = 0;
            for (var index = 0; index < placementRoot.childCount; index++)
            {
                var child = placementRoot.GetChild(index);
                if (!child.name.StartsWith("Tergo_", StringComparison.Ordinal) ||
                    string.Equals(child.name, IdleRootName, StringComparison.Ordinal))
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

        private static bool IsGeneratedEyeObject(Transform transform)
        {
            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, EyeContainerName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
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
        }
    }
}
