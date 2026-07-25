using UnityEngine;

namespace Bellerophon.Enemies.Smorzando
{
    [DisallowMultipleComponent]
    public sealed class SmorzandoInstalledIdleMotion : MonoBehaviour
    {
        [Header("Visible wax mesh")]
        [SerializeField] private MeshFilter waxMeshFilter;
        [SerializeField, Min(0.1f)] private float cycleDurationSeconds = 3.2f;
        [SerializeField, Min(0f)] private float phaseOffsetSeconds;
        [SerializeField, Min(0f)] private float bodyBreathScale = 0.008f;
        [SerializeField, Min(0f)] private float poolBreathScale = 0.004f;
        [SerializeField, Min(0f)] private float poolWaveHeightMeters = 0.006f;
        [SerializeField, Min(0f)] private float wholeBodyBobHeightMeters = 0.008f;
        [SerializeField, Min(0.1f)] private float radialWaveCount = 1.25f;

        [Header("Flame body-follow anchor")]
        [SerializeField] private Transform flameRoot;
        [SerializeField] private Vector3 flameAnchorLocalPosition;

        private Mesh originalMesh;
        private Mesh deformedMesh;
        private Vector3[] originalVertices;
        private Vector3[] deformedVertices;
        private Bounds expandedBounds;
        private Vector3 flameBaseScale;
        private Quaternion flameBaseRotation;
        private float verticalMetersPerLocalUnit;
        private float elapsedSeconds;
        private bool previewPrepared;

        public float CycleDurationSeconds => cycleDurationSeconds;
        public float PhaseOffsetSeconds => phaseOffsetSeconds;
        public MeshFilter WaxMeshFilter => waxMeshFilter;
        public Transform FlameRoot => flameRoot;

        public void Configure(
            MeshFilter configuredMeshFilter,
            Transform configuredFlameRoot,
            Vector3 configuredFlameAnchor,
            float configuredPhaseOffsetSeconds)
        {
            waxMeshFilter = configuredMeshFilter;
            flameRoot = configuredFlameRoot;
            flameAnchorLocalPosition = configuredFlameAnchor;
            phaseOffsetSeconds = Mathf.Max(0f, configuredPhaseOffsetSeconds);
            CaptureFlameBasePose();
        }

        public void PreparePreview()
        {
            EnsureDeformedMesh();
            previewPrepared = true;
        }

        public void SampleAtTime(float timeSeconds)
        {
            EnsureDeformedMesh();
            ApplyPose(Mathf.Max(0f, timeSeconds) + phaseOffsetSeconds);
        }

        public void RestoreSourceMesh()
        {
            if (waxMeshFilter != null && deformedMesh != null && waxMeshFilter.sharedMesh == deformedMesh)
            {
                waxMeshFilter.sharedMesh = originalMesh;
            }

            if (deformedMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(deformedMesh);
                }
                else
                {
                    DestroyImmediate(deformedMesh);
                }
            }

            deformedMesh = null;
            originalVertices = null;
            deformedVertices = null;
            previewPrepared = false;
            RestoreFlameBasePose();
        }

        private void Awake()
        {
            CaptureFlameBasePose();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds = 0f;
            EnsureDeformedMesh();
            ApplyPose(phaseOffsetSeconds);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            ApplyPose(elapsedSeconds + phaseOffsetSeconds);
        }

        private void OnDisable()
        {
            if (Application.isPlaying || previewPrepared)
            {
                RestoreSourceMesh();
            }
        }

        private void OnDestroy()
        {
            if (deformedMesh != null)
            {
                RestoreSourceMesh();
            }
        }

        private void EnsureDeformedMesh()
        {
            if (deformedMesh != null)
            {
                return;
            }

            if (waxMeshFilter == null)
            {
                waxMeshFilter = GetComponent<MeshFilter>();
            }

            if (waxMeshFilter == null || waxMeshFilter.sharedMesh == null)
            {
                throw new MissingComponentException("Smorzando installed idle motion requires a readable MeshFilter.");
            }

            originalMesh = waxMeshFilter.sharedMesh;
            originalVertices = originalMesh.vertices;
            deformedVertices = new Vector3[originalVertices.Length];
            deformedMesh = Instantiate(originalMesh);
            deformedMesh.name = originalMesh.name + "_SmorzandoInstalledIdleRuntime";
            deformedMesh.hideFlags = HideFlags.HideAndDontSave;
            deformedMesh.MarkDynamic();
            expandedBounds = originalMesh.bounds;
            var localExpansion = Mathf.Max(poolWaveHeightMeters, wholeBodyBobHeightMeters) /
                Mathf.Max(GetVerticalMetersPerLocalUnit(), 0.000001f);
            expandedBounds.Expand(new Vector3(0f, 0f, localExpansion * 2.5f));
            deformedMesh.bounds = expandedBounds;
            waxMeshFilter.sharedMesh = deformedMesh;
            CaptureFlameBasePose();
        }

        private void ApplyPose(float timeSeconds)
        {
            if (deformedMesh == null || originalVertices == null)
            {
                return;
            }

            var duration = Mathf.Max(cycleDurationSeconds, 0.1f);
            var phase = Mathf.Repeat(timeSeconds, duration) / duration * Mathf.PI * 2f;
            var breath = Mathf.Sin(phase);
            var bounds = originalMesh.bounds;
            var minimumZ = bounds.min.z;
            var height = Mathf.Max(bounds.size.z, 0.000001f);
            var radialX = Mathf.Max(bounds.extents.x, 0.000001f);
            var radialY = Mathf.Max(bounds.extents.y, 0.000001f);
            var center = bounds.center;
            var verticalScale = Mathf.Max(GetVerticalMetersPerLocalUnit(), 0.000001f);
            var bobLocal = breath * wholeBodyBobHeightMeters / verticalScale;
            var waveLocalAmplitude = poolWaveHeightMeters / verticalScale;

            for (var index = 0; index < originalVertices.Length; index++)
            {
                var source = originalVertices[index];
                var height01 = Mathf.Clamp01((source.z - minimumZ) / height);
                var bodyMask = SmoothStep(0.22f, 0.58f, height01);
                var poolMask = 1f - SmoothStep(0.18f, 0.42f, height01);
                var normalizedX = (source.x - center.x) / radialX;
                var normalizedY = (source.y - center.y) / radialY;
                var radius01 = Mathf.Clamp01(Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY));
                var lateralAmplitude = Mathf.Lerp(poolBreathScale, bodyBreathScale, bodyMask);
                var lateralScale = 1f + breath * lateralAmplitude;
                var wave = Mathf.Sin(phase - radius01 * Mathf.PI * 2f * radialWaveCount) *
                    poolMask * waveLocalAmplitude;

                deformedVertices[index] = new Vector3(
                    center.x + (source.x - center.x) * lateralScale,
                    center.y + (source.y - center.y) * lateralScale,
                    source.z + bobLocal + wave);
            }

            deformedMesh.vertices = deformedVertices;
            deformedMesh.bounds = expandedBounds;
            UpdateFlamePose(phase, breath, bobLocal);
        }

        private void UpdateFlamePose(float phase, float breath, float bobLocal)
        {
            if (flameRoot != null)
            {
                var bodyScale = 1f + breath * bodyBreathScale;
                var anchorOffset = flameAnchorLocalPosition - originalMesh.bounds.center;
                flameRoot.localPosition = new Vector3(
                    originalMesh.bounds.center.x + anchorOffset.x * bodyScale,
                    originalMesh.bounds.center.y + anchorOffset.y * bodyScale,
                    flameAnchorLocalPosition.z + bobLocal);
                flameRoot.localRotation = flameBaseRotation;
                flameRoot.localScale = Vector3.Scale(
                    flameBaseScale,
                    new Vector3(bodyScale, bodyScale, 1f));
            }

        }

        private float GetVerticalMetersPerLocalUnit()
        {
            if (verticalMetersPerLocalUnit <= 0f)
            {
                verticalMetersPerLocalUnit = transform.TransformVector(Vector3.forward).magnitude;
            }

            return verticalMetersPerLocalUnit;
        }

        private void CaptureFlameBasePose()
        {
            if (flameRoot == null)
            {
                return;
            }

            flameBaseScale = flameRoot.localScale;
            flameBaseRotation = flameRoot.localRotation;
        }

        private void RestoreFlameBasePose()
        {
            if (flameRoot == null)
            {
                return;
            }

            flameRoot.localPosition = flameAnchorLocalPosition;
            flameRoot.localScale = flameBaseScale;
            flameRoot.localRotation = flameBaseRotation;
        }

        private static float SmoothStep(float start, float end, float value)
        {
            var normalized = Mathf.Clamp01((value - start) / Mathf.Max(end - start, 0.000001f));
            return normalized * normalized * (3f - 2f * normalized);
        }
    }
}
