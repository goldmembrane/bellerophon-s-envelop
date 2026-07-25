using UnityEngine;

namespace Bellerophon.Enemies.Smorzando
{
    [DisallowMultipleComponent]
    public sealed class SmorzandoPersonIdleMotion : MonoBehaviour
    {
        [Header("Visible person mesh")]
        [SerializeField] private SkinnedMeshRenderer sourceRenderer;

        [Header("Whole-body wax breathing")]
        [SerializeField, Min(0.1f)] private float cycleDurationSeconds = 3.4f;
        [SerializeField, Min(0f)] private float horizontalBreathScale = 0.014f;
        [SerializeField, Min(0f)] private float verticalBreathScale = 0.007f;
        [SerializeField, Min(0f)] private float secondarySurfaceScale = 0.003f;
        [SerializeField, Range(0.01f, 0.5f)] private float footLockHeight01 = 0.18f;

        private Mesh bakedSourceMesh;
        private Mesh deformedMesh;
        private Vector3[] sourceVertices;
        private Vector3[] sourceNormals;
        private Vector3[] deformedVertices;
        private GameObject morphProxy;
        private MeshRenderer morphProxyRenderer;
        private bool sourceRendererWasEnabled;
        private bool stateCaptured;
        private bool previewPrepared;
        private float elapsedSeconds;

        public float CycleDurationSeconds => cycleDurationSeconds;
        public SkinnedMeshRenderer SourceRenderer => sourceRenderer;

        public void Configure(SkinnedMeshRenderer configuredSourceRenderer)
        {
            if (morphProxy != null || deformedMesh != null)
            {
                RestoreInitialState();
            }

            sourceRenderer = configuredSourceRenderer;
            CaptureInitialState();
        }

        public void PreparePreview()
        {
            EnsureRuntimeMesh();
            previewPrepared = true;
        }

        public void SampleAtTime(float timeSeconds)
        {
            EnsureRuntimeMesh();
            ApplyPose(Mathf.Max(0f, timeSeconds));
        }

        public void RestoreInitialState()
        {
            if (sourceRenderer != null && stateCaptured)
            {
                sourceRenderer.enabled = sourceRendererWasEnabled;
            }

            DestroyRuntimeObject(morphProxy);
            DestroyRuntimeObject(deformedMesh);
            DestroyRuntimeObject(bakedSourceMesh);
            morphProxy = null;
            morphProxyRenderer = null;
            deformedMesh = null;
            bakedSourceMesh = null;
            sourceVertices = null;
            sourceNormals = null;
            deformedVertices = null;
            previewPrepared = false;
        }

        private void Awake()
        {
            CaptureInitialState();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds = 0f;
            EnsureRuntimeMesh();
            ApplyPose(0f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            ApplyPose(elapsedSeconds);
        }

        private void OnDisable()
        {
            if (Application.isPlaying || previewPrepared)
            {
                RestoreInitialState();
            }
        }

        private void OnDestroy()
        {
            if (morphProxy != null || deformedMesh != null || bakedSourceMesh != null)
            {
                RestoreInitialState();
            }
        }

        private void CaptureInitialState()
        {
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            }

            if (sourceRenderer == null)
            {
                return;
            }

            sourceRendererWasEnabled = sourceRenderer.enabled;
            stateCaptured = true;
        }

        private void EnsureRuntimeMesh()
        {
            if (morphProxy != null && deformedMesh != null)
            {
                return;
            }

            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            }

            if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
            {
                throw new MissingComponentException(
                    "Smorzando person idle motion requires a SkinnedMeshRenderer.");
            }

            CaptureInitialState();
            bakedSourceMesh = new Mesh
            {
                name = sourceRenderer.sharedMesh.name + "_SmorzandoPersonIdleBaked",
                hideFlags = HideFlags.HideAndDontSave
            };
            sourceRenderer.BakeMesh(bakedSourceMesh, false);
            sourceVertices = bakedSourceMesh.vertices;
            sourceNormals = bakedSourceMesh.normals;
            deformedVertices = new Vector3[sourceVertices.Length];
            deformedMesh = Instantiate(bakedSourceMesh);
            deformedMesh.name = bakedSourceMesh.name + "_Deformed";
            deformedMesh.hideFlags = HideFlags.HideAndDontSave;
            deformedMesh.MarkDynamic();
            deformedMesh.bounds = CalculateExpandedBounds(bakedSourceMesh.bounds);

            morphProxy = new GameObject("Smorzando_PersonIdle_MorphProxy")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = sourceRenderer.gameObject.layer
            };
            var sourceTransform = sourceRenderer.transform;
            morphProxy.transform.SetParent(sourceTransform.parent, false);
            morphProxy.transform.localPosition = sourceTransform.localPosition;
            morphProxy.transform.localRotation = sourceTransform.localRotation;
            morphProxy.transform.localScale = sourceTransform.localScale;
            morphProxy.AddComponent<MeshFilter>().sharedMesh = deformedMesh;
            morphProxyRenderer = morphProxy.AddComponent<MeshRenderer>();
            morphProxyRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
            morphProxyRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            morphProxyRenderer.receiveShadows = sourceRenderer.receiveShadows;
            morphProxyRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            morphProxyRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
            morphProxyRenderer.motionVectorGenerationMode = sourceRenderer.motionVectorGenerationMode;
            sourceRenderer.enabled = false;
        }

        private void ApplyPose(float timeSeconds)
        {
            if (deformedMesh == null || sourceVertices == null)
            {
                return;
            }

            var duration = Mathf.Max(cycleDurationSeconds, 0.1f);
            var phase = Mathf.Repeat(timeSeconds, duration) / duration * Mathf.PI * 2f;
            var breath = Mathf.Sin(phase);
            var secondaryBreath = Mathf.Sin(phase * 2f) * secondarySurfaceScale;
            var bounds = bakedSourceMesh.bounds;
            var height = Mathf.Max(bounds.size.y, 0.000001f);
            var center = bounds.center;

            for (var index = 0; index < sourceVertices.Length; index++)
            {
                var source = sourceVertices[index];
                var height01 = Mathf.Clamp01((source.y - bounds.min.y) / height);
                var footWeight = SmoothStep(0.035f, footLockHeight01, height01);
                var fullBodyProfile = Mathf.Lerp(
                    0.78f,
                    1f,
                    Mathf.Sin(height01 * Mathf.PI));
                var radial = new Vector2(source.x - center.x, source.z - center.z);
                var angle = Mathf.Atan2(radial.y, radial.x);
                var flowingSurfaceProfile = 1f +
                    Mathf.Sin(angle * 2f + height01 * 5.2f) * 0.22f;
                var radialScale = 1f + footWeight * fullBodyProfile *
                    (breath * horizontalBreathScale + secondaryBreath * flowingSurfaceProfile);
                var verticalStretch = breath * verticalBreathScale * footWeight * fullBodyProfile;

                deformedVertices[index] = new Vector3(
                    center.x + radial.x * radialScale,
                    source.y + (source.y - bounds.min.y) * verticalStretch,
                    center.z + radial.y * radialScale);
            }

            deformedMesh.vertices = deformedVertices;
            deformedMesh.bounds = CalculateExpandedBounds(bounds);
            if (sourceNormals != null && sourceNormals.Length == sourceVertices.Length)
            {
                // Preserve the authored baked normals: recalculating this multi-island wax surface creates seams.
                deformedMesh.normals = sourceNormals;
            }
        }

        private Bounds CalculateExpandedBounds(Bounds sourceBounds)
        {
            var expanded = sourceBounds;
            var horizontalExpansion = sourceBounds.size.magnitude *
                (horizontalBreathScale + secondarySurfaceScale) * 2f;
            var verticalExpansion = sourceBounds.size.y * verticalBreathScale * 2f;
            expanded.Expand(new Vector3(
                horizontalExpansion,
                verticalExpansion,
                horizontalExpansion));
            return expanded;
        }

        private static float SmoothStep(float start, float end, float value)
        {
            var normalized = Mathf.Clamp01((value - start) / Mathf.Max(end - start, 0.000001f));
            return normalized * normalized * (3f - 2f * normalized);
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
