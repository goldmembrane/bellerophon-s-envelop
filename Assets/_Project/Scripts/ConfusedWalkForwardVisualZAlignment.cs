using System;
using UnityEngine;

namespace Bellerophon.Animation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(32000)]
    public sealed class ConfusedWalkForwardVisualZAlignment : MonoBehaviour
    {
        private const string DefaultAnchorPath = "Armature/Hips";

        [SerializeField] private Transform referenceRoot;
        [SerializeField] private string targetAnchorPath = DefaultAnchorPath;
        [SerializeField] private string referenceAnchorPath = DefaultAnchorPath;

        private Transform targetAnchor;
        private Transform referenceAnchor;

        public Transform ReferenceRoot => referenceRoot;
        public string TargetAnchorPath => targetAnchorPath;
        public string ReferenceAnchorPath => referenceAnchorPath;
        public float LastCorrectionMeters { get; private set; }
        public float LastWorldZError { get; private set; }
        public int CorrectionCount { get; private set; }

        public void Configure(Transform reference)
        {
            referenceRoot = reference != null
                ? reference
                : throw new ArgumentNullException(nameof(reference));
            targetAnchorPath = DefaultAnchorPath;
            referenceAnchorPath = DefaultAnchorPath;
            ResolveAnchors();
        }

        private void OnEnable()
        {
            LastCorrectionMeters = 0f;
            LastWorldZError = 0f;
            CorrectionCount = 0;
            if (referenceRoot != null)
                ResolveAnchors();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;
            if (targetAnchor == null || referenceAnchor == null)
                ResolveAnchors();

            float correction = referenceAnchor.position.z - targetAnchor.position.z;
            Vector3 alignedPosition = transform.position;
            alignedPosition.z += correction;
            transform.position = alignedPosition;

            LastCorrectionMeters = correction;
            LastWorldZError = Mathf.Abs(
                referenceAnchor.position.z - targetAnchor.position.z);
            CorrectionCount++;
        }

        private void ResolveAnchors()
        {
            if (referenceRoot == null)
                throw new InvalidOperationException(
                    "Confused walk visual Z reference is not configured.");
            targetAnchor = transform.Find(targetAnchorPath);
            referenceAnchor = referenceRoot.Find(referenceAnchorPath);
            if (targetAnchor == null || referenceAnchor == null)
                throw new InvalidOperationException(
                    "Confused walk visual Z alignment requires Armature/Hips on both roots.");
        }
    }
}
