using System;
using System.Linq;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DefaultExecutionOrder(900)]
    public sealed class DaggerStabPoseTransitionMotion : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform armatureRoot;
        [SerializeField] private Transform excludedPropRoot;
        [SerializeField, Min(0.01f)] private float transitionDurationSeconds = 0.25f;

        private Transform[] poseTransforms = Array.Empty<Transform>();
        private Vector3[] sourceLocalPositions = Array.Empty<Vector3>();
        private Quaternion[] sourceLocalRotations = Array.Empty<Quaternion>();
        private Vector3[] sourceLocalScales = Array.Empty<Vector3>();
        private float transitionStartedAt;
        private bool transitionActive;

        public float TransitionDurationSeconds => transitionDurationSeconds;
        public bool IsTransitioning => transitionActive;
        public float TransitionProgress { get; private set; }
        public int StartedTransitions { get; private set; }
        public int CompletedTransitions { get; private set; }
        public float LastCompletedDurationSeconds { get; private set; }

        public void Configure(
            Animator targetAnimator,
            Transform targetArmatureRoot,
            Transform targetExcludedPropRoot,
            float durationSeconds)
        {
            animator = targetAnimator;
            armatureRoot = targetArmatureRoot;
            excludedPropRoot = targetExcludedPropRoot;
            transitionDurationSeconds = Mathf.Max(0.01f, durationSeconds);
            RebuildPoseTransformCache();
        }

        public bool BeginTransition(int stateHash, int layerIndex, float targetNormalizedTime)
        {
            if (transitionActive || animator == null || armatureRoot == null)
            {
                return false;
            }

            if (poseTransforms.Length == 0)
            {
                RebuildPoseTransformCache();
            }

            for (var index = 0; index < poseTransforms.Length; index++)
            {
                var poseTransform = poseTransforms[index];
                sourceLocalPositions[index] = poseTransform.localPosition;
                sourceLocalRotations[index] = poseTransform.localRotation;
                sourceLocalScales[index] = poseTransform.localScale;
            }

            transitionStartedAt = Time.time;
            TransitionProgress = 0f;
            transitionActive = true;
            StartedTransitions++;
            animator.Play(stateHash, layerIndex, targetNormalizedTime);
            return true;
        }

        private void Awake()
        {
            RebuildPoseTransformCache();
        }

        private void LateUpdate()
        {
            if (!transitionActive)
            {
                return;
            }

            var linearProgress = Mathf.Clamp01((Time.time - transitionStartedAt) / transitionDurationSeconds);
            var easedProgress = Mathf.SmoothStep(0f, 1f, linearProgress);
            TransitionProgress = linearProgress;

            for (var index = 0; index < poseTransforms.Length; index++)
            {
                var poseTransform = poseTransforms[index];
                var targetPosition = poseTransform.localPosition;
                var targetRotation = poseTransform.localRotation;
                var targetScale = poseTransform.localScale;
                poseTransform.localPosition = Vector3.LerpUnclamped(
                    sourceLocalPositions[index],
                    targetPosition,
                    easedProgress);
                poseTransform.localRotation = Quaternion.SlerpUnclamped(
                    sourceLocalRotations[index],
                    targetRotation,
                    easedProgress);
                poseTransform.localScale = Vector3.LerpUnclamped(
                    sourceLocalScales[index],
                    targetScale,
                    easedProgress);
            }

            if (linearProgress < 1f)
            {
                return;
            }

            transitionActive = false;
            CompletedTransitions++;
            LastCompletedDurationSeconds = Time.time - transitionStartedAt;
        }

        private void OnDisable()
        {
            transitionActive = false;
            TransitionProgress = 0f;
        }

        private void RebuildPoseTransformCache()
        {
            if (armatureRoot == null)
            {
                poseTransforms = Array.Empty<Transform>();
            }
            else
            {
                poseTransforms = armatureRoot
                    .GetComponentsInChildren<Transform>(true)
                    .Where(poseTransform => excludedPropRoot == null ||
                                            (poseTransform != excludedPropRoot &&
                                             !poseTransform.IsChildOf(excludedPropRoot)))
                    .ToArray();
            }

            sourceLocalPositions = new Vector3[poseTransforms.Length];
            sourceLocalRotations = new Quaternion[poseTransforms.Length];
            sourceLocalScales = new Vector3[poseTransforms.Length];
        }
    }
}
