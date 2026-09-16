using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellerophon.Repair
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RepairWeldingVfxPresenter : MonoBehaviour
    {
        public const float DesignForwardDistanceMeters = 0.5f;
        public const float DesignWorldHeightMeters = 0.62f;
        public const float DesignAnimationLoopSeconds = 0.72f;

        private static readonly int LayerModeId = Shader.PropertyToID("_LayerMode");
        private static readonly int PhaseOffsetId = Shader.PropertyToID("_PhaseOffset");
        private static readonly int LoopSecondsId = Shader.PropertyToID("_LoopSeconds");

        [SerializeField] private Transform ownerRoot;
        [SerializeField] private Vector3 fixedWorldPosition;
        [SerializeField] private bool hasFixedWorldPosition;
        [SerializeField] private SpriteRenderer arcLayer;
        [SerializeField] private SpriteRenderer[] sparkLayers = Array.Empty<SpriteRenderer>();
        [SerializeField] private SpriteRenderer[] smokeLayers = Array.Empty<SpriteRenderer>();
        [SerializeField] private float forwardDistanceMeters = DesignForwardDistanceMeters;
        [SerializeField] private float worldHeightMeters = DesignWorldHeightMeters;

        public Transform OwnerRoot => ownerRoot;
        public Vector3 FixedWorldPosition => fixedWorldPosition;
        public bool HasFixedWorldPosition => hasFixedWorldPosition;
        public bool HasAnimatedVfx =>
            arcLayer != null &&
            sparkLayers != null &&
            sparkLayers.Length == 2 &&
            Array.TrueForAll(sparkLayers, layer => layer != null) &&
            smokeLayers != null &&
            smokeLayers.Length == 2 &&
            Array.TrueForAll(smokeLayers, layer => layer != null);
        public float ForwardDistanceMeters => forwardDistanceMeters;
        public float WorldHeightMeters => worldHeightMeters;
        public float AnimationPhase { get; private set; }
        public float CurrentArcIntensity { get; private set; } = 1f;
        public float CurrentSparkTravelNormalized { get; private set; }
        public float CurrentSmokeTravelNormalized { get; private set; }
        public int ApprovedSampleLayerCount => HasAnimatedVfx ? 5 : 0;

        private void OnEnable()
        {
            InitializeFixedWorldPositionIfNeeded();
            ConfigureRenderers();
            ApplyLayerProperties();
            ApplyPose(Camera.main);
            ApplyAnimationState();
        }

        private void LateUpdate()
        {
            ApplyPose(Camera.main);
            ApplyAnimationState();
        }

        private void OnValidate()
        {
            forwardDistanceMeters = Mathf.Max(0f, forwardDistanceMeters);
            worldHeightMeters = Mathf.Max(0.01f, worldHeightMeters);
            InitializeFixedWorldPositionIfNeeded();
            ConfigureRenderers();
            ApplyLayerProperties();
            ApplyPose(Camera.main);
            ApplyAnimationState();
        }

        public void Configure(
            Transform targetOwnerRoot,
            Sprite approvedSprite,
            Vector3 targetWorldPosition,
            SpriteRenderer targetArcLayer,
            SpriteRenderer[] targetSparkLayers,
            SpriteRenderer[] targetSmokeLayers,
            float distanceMeters,
            float heightMeters)
        {
            ownerRoot = targetOwnerRoot;
            fixedWorldPosition = targetWorldPosition;
            hasFixedWorldPosition = true;
            arcLayer = targetArcLayer;
            sparkLayers = targetSparkLayers ?? Array.Empty<SpriteRenderer>();
            smokeLayers = targetSmokeLayers ?? Array.Empty<SpriteRenderer>();
            forwardDistanceMeters = Mathf.Max(0f, distanceMeters);
            worldHeightMeters = Mathf.Max(0.01f, heightMeters);

            foreach (SpriteRenderer renderer in AllLayers())
            {
                if (renderer == null)
                {
                    continue;
                }
                renderer.sprite = approvedSprite;
                renderer.color = Color.white;
            }

            ConfigureRenderers();
            ApplyLayerProperties();
            ApplyPose(Camera.main);
            ApplyAnimationState();
        }

        public void RefreshForCamera(Camera targetCamera)
        {
            ConfigureRenderers();
            ApplyLayerProperties();
            ApplyPose(targetCamera);
        }

        public float CurrentForwardDistance()
        {
            if (ownerRoot == null)
            {
                return 0f;
            }
            return Vector3.Dot(
                transform.position - ownerRoot.position,
                ownerRoot.forward.normalized);
        }

        private void ConfigureRenderers()
        {
            SpriteRenderer[] renderers = AllLayers();
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.allowOcclusionWhenDynamic = false;
                renderer.sortingOrder = 31999 + index;
            }
        }

        private void ApplyLayerProperties()
        {
            SetLayerProperties(arcLayer, 1f, 0f);
            if (sparkLayers != null && sparkLayers.Length == 2)
            {
                SetLayerProperties(sparkLayers[0], 2f, 0f);
                SetLayerProperties(sparkLayers[1], 2f, 0.5f);
            }
            if (smokeLayers != null && smokeLayers.Length == 2)
            {
                SetLayerProperties(smokeLayers[0], 3f, 0f);
                SetLayerProperties(smokeLayers[1], 3f, 0.5f);
            }
        }

        private static void SetLayerProperties(
            SpriteRenderer renderer,
            float layerMode,
            float phaseOffset)
        {
            if (renderer == null)
            {
                return;
            }
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(LayerModeId, layerMode);
            properties.SetFloat(PhaseOffsetId, phaseOffset);
            properties.SetFloat(LoopSecondsId, DesignAnimationLoopSeconds);
            renderer.SetPropertyBlock(properties);
        }

        private void ApplyAnimationState()
        {
            if (!Application.isPlaying)
            {
                AnimationPhase = 0f;
                CurrentArcIntensity = 1f;
                CurrentSparkTravelNormalized = 0f;
                CurrentSmokeTravelNormalized = 0f;
                return;
            }

            AnimationPhase = Mathf.Repeat(
                Time.unscaledTime,
                DesignAnimationLoopSeconds) / DesignAnimationLoopSeconds;
            float pulseA = Mathf.Sin(AnimationPhase * Mathf.PI * 10f) * 0.5f + 0.5f;
            float pulseB = Mathf.Sin(AnimationPhase * Mathf.PI * 22f + 0.7f) * 0.5f + 0.5f;
            CurrentArcIntensity = Mathf.Lerp(
                0.58f,
                1f,
                Mathf.Clamp01(pulseA * 0.72f + pulseB * 0.28f));
            CurrentSparkTravelNormalized = AnimationPhase * 0.16f;
            CurrentSmokeTravelNormalized = AnimationPhase * 0.085f;
        }

        private void ApplyPose(Camera targetCamera)
        {
            if (ownerRoot == null || !hasFixedWorldPosition)
            {
                return;
            }
            transform.position = fixedWorldPosition;
            Sprite sprite = arcLayer != null ? arcLayer.sprite : null;
            if (sprite != null && sprite.bounds.size.y > 0.000001f)
            {
                float uniformScale = worldHeightMeters / sprite.bounds.size.y;
                Vector3 compensation = CompensateParentScale(transform.parent);
                transform.localScale = new Vector3(
                    uniformScale * compensation.x,
                    uniformScale * compensation.y,
                    uniformScale * compensation.z);
            }
            if (targetCamera == null)
            {
                return;
            }
            Vector3 direction = targetCamera.transform.position - transform.position;
            if (direction.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private SpriteRenderer[] AllLayers()
        {
            SpriteRenderer[] result = new SpriteRenderer[5];
            result[0] = arcLayer;
            if (sparkLayers != null && sparkLayers.Length == 2)
            {
                result[1] = sparkLayers[0];
                result[2] = sparkLayers[1];
            }
            if (smokeLayers != null && smokeLayers.Length == 2)
            {
                result[3] = smokeLayers[0];
                result[4] = smokeLayers[1];
            }
            return result;
        }

        private void InitializeFixedWorldPositionIfNeeded()
        {
            if (hasFixedWorldPosition)
            {
                return;
            }
            fixedWorldPosition = transform.position;
            hasFixedWorldPosition = true;
        }

        private static Vector3 CompensateParentScale(Transform parent)
        {
            if (parent == null)
            {
                return Vector3.one;
            }
            Vector3 scale = parent.lossyScale;
            return new Vector3(
                SafeReciprocal(scale.x),
                SafeReciprocal(scale.y),
                SafeReciprocal(scale.z));
        }

        private static float SafeReciprocal(float value)
        {
            return Mathf.Abs(value) <= 0.000001f ? 1f : 1f / Mathf.Abs(value);
        }
    }
}
