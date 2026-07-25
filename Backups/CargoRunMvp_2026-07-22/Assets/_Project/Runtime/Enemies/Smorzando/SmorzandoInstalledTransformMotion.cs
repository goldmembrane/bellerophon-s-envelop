using UnityEngine;

namespace Bellerophon.Enemies.Smorzando
{
    [DisallowMultipleComponent]
    public sealed class SmorzandoInstalledTransformMotion : MonoBehaviour
    {
        [Header("Installed wax")]
        [SerializeField] private MeshFilter installedMeshFilter;
        [SerializeField] private Renderer installedRenderer;

        [Header("Existing flame")]
        [SerializeField] private Transform flameRoot;
        [SerializeField] private Light flameLight;
        [SerializeField] private SmorzandoInstalledFlameMotion flameMotion;

        [Header("Person target")]
        [SerializeField] private SkinnedMeshRenderer personSourceRenderer;

        [Header("Connected sculpt surface")]
        [SerializeField] private MeshFilter sculptSurfaceSourceFilter;
        [SerializeField] private Renderer sculptSurfaceSourceRenderer;

        [Header("Three-second transform timing")]
        [SerializeField, Min(0.1f)] private float durationSeconds = 3f;
        [SerializeField, Min(0f)] private float flameOutSeconds = 0.5f;
        [SerializeField, Min(0f)] private float emberEndSeconds = 0.65f;
        [SerializeField, Min(0f)] private float waxGatherStartSeconds = 0.5f;
        [SerializeField, Min(0f)] private float waxGatherEndSeconds = 1.8f;
        [SerializeField, Min(0f)] private float personMorphStartSeconds = 1.8f;

        [Header("Review playback")]
        [SerializeField] private bool loopForReview;
        [SerializeField, Min(0f)] private float finalHoldSeconds = 1f;

        private Mesh installedSourceMesh;
        private Mesh installedDeformedMesh;
        private Vector3[] installedSourceVertices;
        private Vector3[] installedDeformedVertices;
        private Mesh personBakedMesh;
        private Mesh personDeformedMesh;
        private Vector3[] personFinalVertices;
        private Vector3[] personFinalNormals;
        private Vector3[] personDeformedVertices;
        private Vector3[] personDeformedNormals;
        private GameObject personProxy;
        private MeshRenderer personProxyRenderer;
        private Material[] personProxyMaterials;
        private Renderer[] flameRenderers;
        private Vector3 flameBaseScale;
        private Color flameBaseColor;
        private float flameBaseIntensity;
        private bool flameBaseEnabled;
        private bool flameMotionBaseEnabled;
        private bool installedRendererBaseEnabled;
        private bool personSourceBaseEnabled;
        private bool sculptSurfaceRendererBaseEnabled;
        private float elapsedSeconds;
        private bool stateCaptured;
        private bool previewPrepared;

        public float DurationSeconds => durationSeconds;
        public float CycleDurationSeconds => durationSeconds + finalHoldSeconds;
        public bool LoopForReview => loopForReview;
        public float FinalHoldSeconds => finalHoldSeconds;
        public MeshFilter InstalledMeshFilter => installedMeshFilter;
        public SkinnedMeshRenderer PersonSourceRenderer => personSourceRenderer;
        public MeshFilter SculptSurfaceSourceFilter => sculptSurfaceSourceFilter;

        public void Configure(
            MeshFilter configuredInstalledMeshFilter,
            Renderer configuredInstalledRenderer,
            Transform configuredFlameRoot,
            Light configuredFlameLight,
            SmorzandoInstalledFlameMotion configuredFlameMotion,
            SkinnedMeshRenderer configuredPersonSourceRenderer,
            MeshFilter configuredSculptSurfaceSourceFilter,
            Renderer configuredSculptSurfaceSourceRenderer,
            float configuredBlobReadySeconds,
            bool configuredLoopForReview,
            float configuredFinalHoldSeconds)
        {
            installedMeshFilter = configuredInstalledMeshFilter;
            installedRenderer = configuredInstalledRenderer;
            flameRoot = configuredFlameRoot;
            flameLight = configuredFlameLight;
            flameMotion = configuredFlameMotion;
            personSourceRenderer = configuredPersonSourceRenderer;
            sculptSurfaceSourceFilter = configuredSculptSurfaceSourceFilter;
            sculptSurfaceSourceRenderer = configuredSculptSurfaceSourceRenderer;
            waxGatherEndSeconds = Mathf.Clamp(configuredBlobReadySeconds, waxGatherStartSeconds, durationSeconds);
            personMorphStartSeconds = waxGatherEndSeconds;
            loopForReview = configuredLoopForReview;
            finalHoldSeconds = Mathf.Max(0f, configuredFinalHoldSeconds);
            CaptureInitialState();
        }

        public void PreparePreview()
        {
            CaptureInitialState();
            EnsureRuntimeMeshes();
            previewPrepared = true;
        }

        public void SampleAtTime(float timeSeconds)
        {
            EnsureRuntimeMeshes();
            ApplyPlaybackTime(Mathf.Max(0f, timeSeconds));
        }

        public void RestoreInitialState()
        {
            if (!stateCaptured)
            {
                return;
            }

            if (installedMeshFilter != null && installedDeformedMesh != null &&
                installedMeshFilter.sharedMesh == installedDeformedMesh)
            {
                installedMeshFilter.sharedMesh = installedSourceMesh;
            }

            if (installedRenderer != null)
            {
                installedRenderer.enabled = installedRendererBaseEnabled;
            }

            if (personSourceRenderer != null)
            {
                personSourceRenderer.enabled = personSourceBaseEnabled;
            }

            if (sculptSurfaceSourceRenderer != null)
            {
                sculptSurfaceSourceRenderer.enabled = sculptSurfaceRendererBaseEnabled;
            }

            if (flameMotion != null)
            {
                flameMotion.enabled = flameMotionBaseEnabled;
            }

            if (flameRoot != null)
            {
                flameRoot.localScale = flameBaseScale;
            }

            if (flameLight != null)
            {
                flameLight.enabled = flameBaseEnabled;
                flameLight.color = flameBaseColor;
                flameLight.intensity = flameBaseIntensity;
            }

            if (flameRenderers != null)
            {
                foreach (var flameRenderer in flameRenderers)
                {
                    if (flameRenderer != null)
                    {
                        flameRenderer.enabled = true;
                    }
                }
            }

            DestroyRuntimeObject(personProxy);
            DestroyRuntimeObject(installedDeformedMesh);
            DestroyRuntimeObject(personDeformedMesh);
            DestroyRuntimeObject(personBakedMesh);
            if (personProxyMaterials != null)
            {
                foreach (var proxyMaterial in personProxyMaterials)
                {
                    DestroyRuntimeObject(proxyMaterial);
                }
            }
            personProxy = null;
            personProxyRenderer = null;
            personProxyMaterials = null;
            installedDeformedMesh = null;
            personDeformedMesh = null;
            personBakedMesh = null;
            installedSourceVertices = null;
            installedDeformedVertices = null;
            personFinalVertices = null;
            personFinalNormals = null;
            personDeformedVertices = null;
            personDeformedNormals = null;
            previewPrepared = false;
            stateCaptured = false;
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

            CaptureInitialState();
            EnsureRuntimeMeshes();
            elapsedSeconds = 0f;
            ApplyPlaybackTime(0f);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            if (!loopForReview)
            {
                elapsedSeconds = Mathf.Min(durationSeconds, elapsedSeconds);
            }

            ApplyPlaybackTime(elapsedSeconds);
        }

        private void OnDisable()
        {
            if (previewPrepared)
            {
                RestoreInitialState();
            }
        }

        private void CaptureInitialState()
        {
            if (stateCaptured)
            {
                return;
            }

            if (installedRenderer == null && installedMeshFilter != null)
            {
                installedRenderer = installedMeshFilter.GetComponent<Renderer>();
            }

            flameRenderers = flameRoot != null
                ? flameRoot.GetComponentsInChildren<Renderer>(true)
                : new Renderer[0];
            flameBaseScale = flameRoot != null ? flameRoot.localScale : Vector3.one;
            flameBaseEnabled = flameLight != null && flameLight.enabled;
            flameBaseColor = flameLight != null ? flameLight.color : Color.white;
            flameBaseIntensity = flameLight != null ? flameLight.intensity : 0f;
            flameMotionBaseEnabled = flameMotion != null && flameMotion.enabled;
            installedRendererBaseEnabled = installedRenderer != null && installedRenderer.enabled;
            personSourceBaseEnabled = personSourceRenderer != null && personSourceRenderer.enabled;
            sculptSurfaceRendererBaseEnabled = sculptSurfaceSourceRenderer != null && sculptSurfaceSourceRenderer.enabled;
            stateCaptured = true;
        }

        private void EnsureRuntimeMeshes()
        {
            if (installedDeformedMesh != null && personDeformedMesh != null && personProxy != null)
            {
                return;
            }

            if (installedMeshFilter == null || installedMeshFilter.sharedMesh == null)
            {
                throw new MissingComponentException("Smorzando transform motion requires the installed MeshFilter.");
            }

            if (personSourceRenderer == null || personSourceRenderer.sharedMesh == null)
            {
                throw new MissingComponentException("Smorzando transform motion requires the person SkinnedMeshRenderer.");
            }

            if (sculptSurfaceSourceFilter == null || sculptSurfaceSourceFilter.sharedMesh == null)
            {
                throw new MissingComponentException("Smorzando transform motion requires the connected sculpt MeshFilter.");
            }

            if (sculptSurfaceSourceRenderer == null)
            {
                sculptSurfaceSourceRenderer = sculptSurfaceSourceFilter.GetComponent<Renderer>();
            }

            CaptureInitialState();
            installedSourceMesh = installedMeshFilter.sharedMesh;
            installedSourceVertices = installedSourceMesh.vertices;
            installedDeformedVertices = new Vector3[installedSourceVertices.Length];
            installedDeformedMesh = Instantiate(installedSourceMesh);
            installedDeformedMesh.name = installedSourceMesh.name + "_SmorzandoTransformRuntime";
            installedDeformedMesh.hideFlags = HideFlags.HideAndDontSave;
            installedDeformedMesh.MarkDynamic();
            installedMeshFilter.sharedMesh = installedDeformedMesh;

            personBakedMesh = Instantiate(sculptSurfaceSourceFilter.sharedMesh);
            personBakedMesh.name = sculptSurfaceSourceFilter.sharedMesh.name + "_SmorzandoConnectedSculptSource";
            personBakedMesh.hideFlags = HideFlags.HideAndDontSave;
            personFinalVertices = personBakedMesh.vertices;
            personFinalNormals = personBakedMesh.normals;
            personDeformedVertices = new Vector3[personFinalVertices.Length];
            personDeformedNormals = new Vector3[personFinalNormals.Length];
            personDeformedMesh = Instantiate(personBakedMesh);
            personDeformedMesh.name = personBakedMesh.name + "_Deformed";
            personDeformedMesh.hideFlags = HideFlags.HideAndDontSave;
            personDeformedMesh.MarkDynamic();

            personProxy = new GameObject("Smorzando_Transform_PersonMorphProxy")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = gameObject.layer
            };
            var sourceTransform = sculptSurfaceSourceFilter.transform;
            personProxy.transform.SetParent(sourceTransform.parent, false);
            personProxy.transform.localPosition = sourceTransform.localPosition;
            personProxy.transform.localRotation = sourceTransform.localRotation;
            personProxy.transform.localScale = sourceTransform.localScale;
            personProxy.AddComponent<MeshFilter>().sharedMesh = personDeformedMesh;
            personProxyRenderer = personProxy.AddComponent<MeshRenderer>();
            personProxyMaterials = new Material[Mathf.Max(personDeformedMesh.subMeshCount, 1)];
            for (var materialIndex = 0; materialIndex < personProxyMaterials.Length; materialIndex++)
            {
                var sourceMaterials = personSourceRenderer.sharedMaterials;
                var sourceMaterial = materialIndex < sourceMaterials.Length && sourceMaterials[materialIndex] != null
                    ? sourceMaterials[materialIndex]
                    : installedRenderer.sharedMaterial;
                var proxyMaterial = new Material(sourceMaterial)
                {
                    name = sourceMaterial.name + "_SmorzandoTransformDoubleSided",
                    hideFlags = HideFlags.HideAndDontSave,
                    doubleSidedGI = true
                };
                if (proxyMaterial.HasProperty("_Cull"))
                {
                    proxyMaterial.SetFloat("_Cull", 0f);
                }
                if (proxyMaterial.HasProperty("_CullMode"))
                {
                    proxyMaterial.SetFloat("_CullMode", 0f);
                }
                personProxyMaterials[materialIndex] = proxyMaterial;
            }
            personProxyRenderer.sharedMaterials = personProxyMaterials;
            personProxyRenderer.shadowCastingMode = sculptSurfaceSourceRenderer.shadowCastingMode;
            personProxyRenderer.receiveShadows = sculptSurfaceSourceRenderer.receiveShadows;
            personProxyRenderer.lightProbeUsage = sculptSurfaceSourceRenderer.lightProbeUsage;
            personProxyRenderer.reflectionProbeUsage = sculptSurfaceSourceRenderer.reflectionProbeUsage;
            personProxyRenderer.enabled = false;
            personSourceRenderer.enabled = false;
            sculptSurfaceSourceRenderer.enabled = false;
        }

        private void ApplyPose(float timeSeconds)
        {
            if (flameMotion != null && flameMotion.enabled)
            {
                flameMotion.enabled = false;
            }

            ApplyFlamePose(timeSeconds);
            ApplyInstalledWaxPose(timeSeconds);
            ApplyPersonPose(timeSeconds);
        }

        private void ApplyPlaybackTime(float playbackTimeSeconds)
        {
            if (!loopForReview)
            {
                ApplyPose(Mathf.Clamp(playbackTimeSeconds, 0f, durationSeconds));
                return;
            }

            var cycleDuration = Mathf.Max(durationSeconds + finalHoldSeconds, durationSeconds);
            var cycleTime = Mathf.Repeat(playbackTimeSeconds, cycleDuration);
            ApplyPose(Mathf.Min(cycleTime, durationSeconds));
        }

        private void ApplyFlamePose(float timeSeconds)
        {
            var flameFade = SmoothRange(0f, flameOutSeconds, timeSeconds);
            var emberFade = SmoothRange(flameOutSeconds, emberEndSeconds, timeSeconds);
            var visible = timeSeconds < emberEndSeconds;
            if (flameRoot != null)
            {
                var flameScale = Mathf.Lerp(1f, 0.16f, flameFade);
                flameScale = Mathf.Lerp(flameScale, 0.035f, emberFade);
                flameRoot.localScale = flameBaseScale * flameScale;
            }

            if (flameRenderers != null)
            {
                foreach (var flameRenderer in flameRenderers)
                {
                    if (flameRenderer != null)
                    {
                        flameRenderer.enabled = visible;
                    }
                }
            }

            if (flameLight != null)
            {
                flameLight.enabled = visible;
                flameLight.color = Color.Lerp(flameBaseColor, new Color(1f, 0.08f, 0.01f, 1f), flameFade);
                var emberIntensity = flameBaseIntensity * Mathf.Lerp(1f, 0.12f, flameFade);
                flameLight.intensity = Mathf.Lerp(emberIntensity, 0f, emberFade);
            }
        }

        private void ApplyInstalledWaxPose(float timeSeconds)
        {
            var gather = SmoothRange(waxGatherStartSeconds, waxGatherEndSeconds, timeSeconds);
            var bounds = installedSourceMesh.bounds;
            var center = bounds.center;
            var height = Mathf.Max(bounds.size.z, 0.000001f);
            var maxRadius = Mathf.Max(bounds.extents.x, bounds.extents.y);
            var gatheredRadius = maxRadius * 0.24f;
            var handoff = SmoothRange(
                Mathf.Max(waxGatherStartSeconds, waxGatherEndSeconds - 0.18f),
                waxGatherEndSeconds,
                timeSeconds);
            var handoffCompression = Mathf.Lerp(1f, 0.08f, handoff);

            for (var index = 0; index < installedSourceVertices.Length; index++)
            {
                var source = installedSourceVertices[index];
                var radial = new Vector2(source.x - center.x, source.y - center.y);
                var radius = radial.magnitude;
                var radius01 = Mathf.Clamp01(radius / Mathf.Max(maxRadius, 0.000001f));
                var height01 = Mathf.Clamp01((source.z - bounds.min.z) / height);
                var poolMask = 1f - SmoothStep(0.20f, 0.46f, height01);
                var angle = Mathf.Atan2(radial.y, radial.x);
                var unevenEdge = 0.88f + Mathf.Sin(angle * 3f + radius01 * 4.6f) * 0.12f;
                // Press the installed wax inward like clay. The upper candle body keeps more width
                // than the floor pool, while every non-central vertex participates in the squeeze.
                var clayCompression = Mathf.Lerp(0.56f, 0.44f, poolMask);
                var targetRadius = Mathf.Min(
                    radius * clayCompression,
                    gatheredRadius * unevenEdge * Mathf.Lerp(0.72f, 1f, 1f - radius01)) *
                    handoffCompression;
                var targetRadial = radius > 0.000001f
                    ? radial / radius * targetRadius
                    : Vector2.zero;
                var radiusCompression = radius > 0.000001f
                    ? Mathf.Clamp01(targetRadius / radius)
                    : clayCompression;
                // Keep the bottom fixed and convert the lost horizontal span into vertical stretch.
                var verticalStretch = 1f + (1f - radiusCompression) * 1.45f;
                var squeezedZ = bounds.min.z + (source.z - bounds.min.z) * verticalStretch;
                var pooledZ = Mathf.Lerp(
                    source.z,
                    bounds.min.z + height * Mathf.Lerp(0.28f, 0.96f, 1f - radius01),
                    poolMask);
                var gatheredZ = Mathf.Max(squeezedZ, pooledZ);
                var gathered = new Vector3(
                    center.x + targetRadial.x,
                    center.y + targetRadial.y,
                    gatheredZ);
                installedDeformedVertices[index] = Vector3.Lerp(source, gathered, gather);
            }

            installedDeformedMesh.vertices = installedDeformedVertices;
            installedDeformedMesh.RecalculateBounds();
            installedDeformedMesh.RecalculateNormals();
            // The original pool stays visible until the full-height wax mass has taken over its volume.
            installedRenderer.enabled = timeSeconds < waxGatherEndSeconds;
        }

        private void ApplyPersonPose(float timeSeconds)
        {
            var gather = SmoothRange(waxGatherStartSeconds, waxGatherEndSeconds, timeSeconds);
            var morph = SmoothRange(personMorphStartSeconds, durationSeconds, timeSeconds);
            var finalBounds = personBakedMesh.bounds;
            var center = finalBounds.center;
            var height = Mathf.Max(finalBounds.size.y, 0.000001f);
            var horizontalRadius = Mathf.Max(finalBounds.extents.x, finalBounds.extents.z);
            var wriggleFade = 1f - SmoothRange(durationSeconds - 0.55f, durationSeconds, timeSeconds);
            var timePhase = timeSeconds * 8.2f;
            for (var index = 0; index < personFinalVertices.Length; index++)
            {
                var final = personFinalVertices[index];
                var height01 = Mathf.Clamp01((final.y - finalBounds.min.y) / height);
                var radial = new Vector2(final.x - center.x, final.z - center.z);
                var radialLength = radial.magnitude;
                var radialDirection = radialLength > 0.000001f ? radial / radialLength : Vector2.zero;
                var angle = Mathf.Atan2(radial.y, radial.x);
                var centerShift = new Vector2(
                    Mathf.Sin(timePhase + height01 * 8.6f) +
                    Mathf.Sin(timePhase * 0.63f - height01 * 13.4f) * 0.38f,
                    Mathf.Cos(timePhase * 0.82f - height01 * 9.8f) +
                    Mathf.Sin(timePhase * 0.51f + height01 * 15.1f) * 0.34f) *
                    (horizontalRadius * 0.045f * wriggleFade * (1f - morph * 0.72f));
                var softPulse = 1f + Mathf.Sin(
                    timePhase * 1.11f + angle * 2.4f + height01 * 10.7f) *
                    (0.07f * wriggleFade * (1f - morph));
                var gatherTwist = (1f - gather) * (2.2f + height01 * 4.8f);
                var twistCos = Mathf.Cos(gatherTwist);
                var twistSin = Mathf.Sin(gatherTwist);
                var twistedRadial = new Vector2(
                    radial.x * twistCos - radial.y * twistSin,
                    radial.x * twistSin + radial.y * twistCos) * (0.48f * softPulse);
                var compactInsideBlob = new Vector3(
                    center.x + centerShift.x + twistedRadial.x,
                    final.y + Mathf.Sin(timePhase * 0.74f + angle + height01 * 12.3f) *
                    (height * 0.008f * wriggleFade * Mathf.Sin(height01 * Mathf.PI)),
                    center.z + centerShift.y + twistedRadial.y);

                // The final zombie vertices themselves rise from the floor into the wax mass. There is no
                // separate outer shell to shrink away while another model appears underneath it.
                var lowerFirstGather = Mathf.Pow(gather, Mathf.Lerp(0.65f, 2f, height01));
                var gatheredFromFloor = new Vector3(
                    Mathf.Lerp(center.x, compactInsideBlob.x, gather),
                    Mathf.Lerp(finalBounds.min.y, compactInsideBlob.y, lowerFirstGather),
                    Mathf.Lerp(center.z, compactInsideBlob.z, gather));

                // A low-frequency vertical delay keeps neighbouring triangles coherent while distinct regions form.
                var regionalDelay = (Mathf.Sin(height01 * Mathf.PI * 2f) * 0.5f + 0.5f) * 0.12f;
                var surfaceMorph = Mathf.Clamp01((morph - regionalDelay) / Mathf.Max(1f - regionalDelay, 0.000001f));
                surfaceMorph = surfaceMorph * surfaceMorph * (3f - 2f * surfaceMorph);
                var sculpted = Vector3.Lerp(gatheredFromFloor, final, surfaceMorph);
                var surfaceWriggle = Mathf.Sin(
                    timePhase * 1.27f + angle * 3.2f - height01 * 14.6f) *
                    (horizontalRadius * 0.02f * wriggleFade * (1f - surfaceMorph));
                sculpted.x += radialDirection.x * surfaceWriggle;
                sculpted.z += radialDirection.y * surfaceWriggle;
                personDeformedVertices[index] = sculpted;
                var finalNormal = personFinalNormals[index];
                personDeformedNormals[index] = new Vector3(
                    finalNormal.x * twistCos - finalNormal.z * twistSin,
                    finalNormal.y,
                    finalNormal.x * twistSin + finalNormal.z * twistCos);
            }

            personDeformedMesh.vertices = personDeformedVertices;
            personDeformedMesh.RecalculateBounds();
            // The exact-detail proxy preserves the authored baked normals. Recalculating them from
            // disconnected clothing/drip islands creates dark seams even though the surface positions match.
            personDeformedMesh.normals = personDeformedNormals;
            var reachedFinal = timeSeconds >= durationSeconds - 0.001f;
            personProxyRenderer.enabled = gather > 0.001f && !reachedFinal;
            personSourceRenderer.enabled = reachedFinal;
        }

        private static float SmoothRange(float start, float end, float value)
        {
            var normalized = Mathf.Clamp01((value - start) / Mathf.Max(end - start, 0.000001f));
            return normalized * normalized * (3f - 2f * normalized);
        }

        private static float SmoothStep(float start, float end, float value)
        {
            return SmoothRange(start, end, value);
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
