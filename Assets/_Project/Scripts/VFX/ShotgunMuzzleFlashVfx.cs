using UnityEngine;

namespace Bellerophon.Vfx
{
    [DisallowMultipleComponent]
    public sealed class ShotgunMuzzleFlashVfx : MonoBehaviour
    {
        public const int ConcurrentSmokeLimit = 6;
        public const int ConcurrentLightLimit = 4;

        // V3 reproduces the approved dense shotgun fan through the existing
        // Musket-style layers; these are per-shot pooled particle budgets.
        private const short V3FlashBurstCount = 8;
        private const short V3HotGasBurstCount = 38;
        private const short V3SmokeBurstCount = 8;
        private const short V3EmberBurstCount = 5;

        private static int activeSmokeEffects;
        private static int activeLights;

        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private ParticleSystem approvedBurst;
        [SerializeField] private ParticleSystem flashBloom;
        [SerializeField] private ParticleSystem hotGas;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem embers;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private float hotFlashDurationSeconds = 0.06f;
        [SerializeField] private float totalDurationSeconds = 0.35f;
        [SerializeField] private float expansionDurationSeconds = 0.025f;
        [SerializeField] private float lightDurationSeconds = 0.065f;
        [SerializeField] private float lightPeakIntensity = 2.3f;
        [SerializeField] private Vector3 initialScaleRatio =
            new Vector3(0.65f, 0.55f, 0.55f);
        [SerializeField] private Vector3 fullLocalScale = Vector3.one;
        [SerializeField] private Vector3 anchorLocalPosition;
        [SerializeField] private Quaternion anchorLocalRotation =
            Quaternion.identity;

        private static readonly int BaseColorId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock propertyBlock;
        private Transform visualTransform;
        private bool ownsSmokeSlot;
        private bool ownsLightSlot;
        private float effectAgeSeconds;
        private float lightRemainingSeconds;
        private bool coreActive;

        public int PlayCount { get; private set; }
        public float EffectAgeSeconds => effectAgeSeconds;
        public bool IsEffectActive => coreActive || ownsSmokeSlot ||
            ownsLightSlot || IsAlive(flashBloom) || IsAlive(hotGas) ||
            IsAlive(smoke) || IsAlive(embers);
        public int ActiveParticleCount => ParticleCount(approvedBurst) +
            ParticleCount(flashBloom) +
            ParticleCount(hotGas) + ParticleCount(smoke) +
            ParticleCount(embers);
        public int SystemCount => (approvedBurst != null ? 1 : 0) +
            (flashBloom != null ? 1 : 0) +
            (hotGas != null ? 1 : 0) + (smoke != null ? 1 : 0) +
            (embers != null ? 1 : 0);
        public int MaximumConfiguredParticles => MaxParticles(approvedBurst) +
            MaxParticles(flashBloom) +
            MaxParticles(hotGas) + MaxParticles(smoke) + MaxParticles(embers);
        public Renderer VisualRenderer => visualRenderer;
        public float HotFlashDurationSeconds => hotFlashDurationSeconds;
        public float TotalDurationSeconds => totalDurationSeconds;
        public float ExpansionDurationSeconds => expansionDurationSeconds;
        public Vector3 InitialScaleRatio => initialScaleRatio;
        public Vector3 FullLocalScale => fullLocalScale;
        public bool UsesCameraBillboarding => true;
        public Light MuzzleLight => muzzleLight;

        public void Configure(
            Renderer renderer,
            float hotFlashDuration,
            float totalDuration)
        {
            ConfigureVolume(
                renderer,
                hotFlashDuration,
                totalDuration,
                expansionDurationSeconds,
                new Vector3(0.65f, 0.55f, 0.55f));
        }

        public void ConfigureVolume(
            Renderer renderer,
            float hotFlashDuration,
            float totalDuration,
            float expansionDuration,
            Vector3 contractedScaleRatio)
        {
            visualRenderer = renderer;
            approvedBurst = visualRenderer != null
                ? visualRenderer.GetComponent<ParticleSystem>()
                : null;
            visualTransform = visualRenderer != null
                ? visualRenderer.transform
                : null;
            hotFlashDurationSeconds = Mathf.Max(0.01f, hotFlashDuration);
            totalDurationSeconds = Mathf.Max(hotFlashDurationSeconds, totalDuration);
            expansionDurationSeconds = Mathf.Clamp(
                expansionDuration,
                0.01f,
                totalDurationSeconds);
            initialScaleRatio = new Vector3(
                Mathf.Clamp(contractedScaleRatio.x, 0.01f, 1f),
                Mathf.Clamp(contractedScaleRatio.y, 0.01f, 1f),
                Mathf.Clamp(contractedScaleRatio.z, 0.01f, 1f));
            // The mesh is authored at its requested final dimensions. Using the
            // already-contracted prefab transform here compounds the startup
            // contraction every time the prefab instance is configured.
            fullLocalScale = Vector3.one;
            anchorLocalPosition = visualTransform != null
                ? visualTransform.localPosition
                : Vector3.zero;
            anchorLocalRotation = visualTransform != null
                ? visualTransform.localRotation
                : Quaternion.identity;
            PrepareCore(false);
        }

        public void ConfigureLayers(
            ParticleSystem flashSystem,
            ParticleSystem hotGasSystem,
            ParticleSystem smokeSystem,
            ParticleSystem emberSystem,
            Light lightSource,
            float lightDuration,
            float peakIntensity)
        {
            flashBloom = flashSystem;
            hotGas = hotGasSystem;
            smoke = smokeSystem;
            embers = emberSystem;
            muzzleLight = lightSource;
            lightDurationSeconds = Mathf.Max(0.01f, lightDuration);
            lightPeakIntensity = Mathf.Max(0f, peakIntensity);
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
                muzzleLight.intensity = 0f;
            }
        }

        public void PlayEffect()
        {
            if (visualRenderer == null)
            {
                return;
            }

            PlayCount++;
            effectAgeSeconds = 0f;
            PrepareCore(true);
            Restart(approvedBurst);
            Restart(flashBloom);
            Restart(hotGas);
            Restart(embers);
            if (ownsSmokeSlot || activeSmokeEffects < ConcurrentSmokeLimit)
            {
                if (!ownsSmokeSlot)
                {
                    activeSmokeEffects++;
                    ownsSmokeSlot = true;
                }

                Restart(smoke);
            }
            else if (smoke != null)
            {
                smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (muzzleLight != null &&
                (ownsLightSlot || activeLights < ConcurrentLightLimit))
            {
                if (!ownsLightSlot)
                {
                    activeLights++;
                    ownsLightSlot = true;
                }

                lightRemainingSeconds = lightDurationSeconds;
                muzzleLight.intensity = lightPeakIntensity;
                muzzleLight.enabled = true;
            }

            enabled = true;
        }

        private void Awake()
        {
            ApplyApprovedV3Profile();
            visualTransform = visualRenderer != null
                ? visualRenderer.transform
                : null;
            if (fullLocalScale.sqrMagnitude <= 0.000001f)
            {
                fullLocalScale = visualTransform != null
                    ? visualTransform.localScale
                    : Vector3.one;
            }

            PrepareCore(false);
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
                muzzleLight.intensity = 0f;
            }

            enabled = false;
        }

        private void ApplyApprovedV3Profile()
        {
            hotFlashDurationSeconds = 0.085f;
            // Keep the controller alive past the 0.55-second direct-review age.
            // Individual particle lifetimes still define the visible duration.
            totalDurationSeconds = 0.72f;
            expansionDurationSeconds = 0.018f;
            lightDurationSeconds = 0.06f;
            lightPeakIntensity = 5.2f;
            initialScaleRatio = new Vector3(0.48f, 0.34f, 0.34f);

            ConfigureV3Layer(
                approvedBurst,
                0.16f,
                V3FlashBurstCount,
                0.08f,
                0.13f,
                0.05f,
                0.20f,
                0.22f,
                0.38f,
                new Color(1f, 1f, 0.98f, 1f),
                new Color(1f, 0.98f, 0.92f, 1f),
                30f,
                0.018f,
                0.035f,
                new AnimationCurve(
                    new Keyframe(0f, 0.60f),
                    new Keyframe(0.24f, 1.35f),
                    new Keyframe(1f, 0.10f)));
            ConfigureV3Layer(
                hotGas,
                0.26f,
                V3HotGasBurstCount,
                0.12f,
                0.24f,
                1.60f,
                3.60f,
                0.07f,
                0.12f,
                new Color(1f, 1f, 0.94f, 1f),
                new Color(1f, 0.76f, 0.22f, 0.96f),
                55f,
                0.025f,
                0.060f,
                new AnimationCurve(
                    new Keyframe(0f, 0.58f),
                    new Keyframe(0.28f, 1.42f),
                    new Keyframe(1f, 0.12f)));
            ConfigureV3Layer(
                smoke,
                0.82f,
                V3SmokeBurstCount,
                0.55f,
                0.88f,
                0.50f,
                1.30f,
                0.16f,
                0.34f,
                new Color(0.92f, 0.90f, 0.86f, 0.86f),
                new Color(0.50f, 0.53f, 0.58f, 0.68f),
                60f,
                0.045f,
                0.12f,
                new AnimationCurve(
                    new Keyframe(0f, 0.48f),
                    new Keyframe(0.34f, 1.45f),
                    new Keyframe(1f, 2.20f)));
            ConfigureV3Layer(
                embers,
                0.50f,
                V3EmberBurstCount,
                0.22f,
                0.46f,
                2.60f,
                5.80f,
                0.018f,
                0.048f,
                new Color(1f, 0.92f, 0.30f, 1f),
                new Color(1f, 0.08f, 0.005f, 0.96f),
                62f,
                0.018f,
                0.045f,
                null);

            ConfigureV3BillboardRenderer(approvedBurst, 5);
            ConfigureV3StretchedRenderer(
                hotGas,
                1.40f,
                0.08f,
                4);
            ConfigureV3BillboardRenderer(smoke, 2);
            ConfigureV3StretchedRenderer(
                embers,
                1.80f,
                0.15f,
                6);
            ConfigureV3HotGasColor(hotGas);
            ConfigureV3SmokeColor(smoke);

            if (embers != null)
            {
                ParticleSystem.MainModule emberMain = embers.main;
                emberMain.gravityModifier =
                    new ParticleSystem.MinMaxCurve(0.16f);
            }

            if (muzzleLight != null)
            {
                muzzleLight.color = new Color(1f, 0.48f, 0.12f, 1f);
                muzzleLight.range = 1.8f;
                muzzleLight.shadows = LightShadows.None;
                muzzleLight.intensity = 0f;
                muzzleLight.enabled = false;
            }
        }

        private static void ConfigureV3Layer(
            ParticleSystem system,
            float duration,
            short burstCount,
            float minimumLifetime,
            float maximumLifetime,
            float minimumSpeed,
            float maximumSpeed,
            float minimumSize,
            float maximumSize,
            Color minimumColor,
            Color maximumColor,
            float coneAngle,
            float coneRadius,
            float coneLength,
            AnimationCurve sizeCurve)
        {
            if (system == null)
            {
                return;
            }

            ParticleSystem.MainModule main = system.main;
            main.duration = duration;
            main.maxParticles = burstCount;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                minimumLifetime,
                maximumLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                minimumSpeed,
                maximumSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(
                minimumSize,
                maximumSize);
            main.startColor = new ParticleSystem.MinMaxGradient(
                minimumColor,
                maximumColor);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = coneRadius;
            shape.length = coneLength;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, burstCount)
            });

            if (sizeCurve != null)
            {
                ParticleSystem.SizeOverLifetimeModule size =
                    system.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            }

            ParticleSystemRenderer renderer =
                system.GetComponent<ParticleSystemRenderer>();
            if (renderer != null &&
                renderer.renderMode == ParticleSystemRenderMode.Billboard)
            {
                renderer.maxParticleSize = 0.78f;
            }
        }

        private static void ConfigureV3StretchedRenderer(
            ParticleSystem system,
            float lengthScale,
            float velocityScale,
            int sortingOrder)
        {
            if (system == null)
            {
                return;
            }

            ParticleSystemRenderer renderer =
                system.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.lengthScale = lengthScale;
            renderer.velocityScale = velocityScale;
            renderer.cameraVelocityScale = 0f;
            renderer.maxParticleSize = 0.48f;
            renderer.sortingOrder = sortingOrder;
        }

        private static void ConfigureV3BillboardRenderer(
            ParticleSystem system,
            int sortingOrder)
        {
            if (system == null)
            {
                return;
            }

            ParticleSystemRenderer renderer =
                system.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.lengthScale = 1f;
            renderer.velocityScale = 0f;
            renderer.maxParticleSize = 0.46f;
            renderer.sortingOrder = sortingOrder;
        }

        private static void ConfigureV3HotGasColor(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.82f, 0.30f), 0.42f),
                    new GradientColorKey(new Color(1f, 0.25f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.94f, 0.46f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule color =
                system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static void ConfigureV3SmokeColor(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.82f, 0.76f, 0.68f), 0f),
                    new GradientColorKey(new Color(0.52f, 0.50f, 0.48f), 0.32f),
                    new GradientColorKey(new Color(0.26f, 0.28f, 0.32f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.82f, 0.08f),
                    new GradientAlphaKey(0.64f, 0.58f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule color =
                system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void Update()
        {
            effectAgeSeconds += Time.deltaTime;
            if (coreActive)
            {
                UpdateCore();
            }

            if (ownsLightSlot)
            {
                lightRemainingSeconds -= Time.deltaTime;
                float weight = Mathf.Clamp01(
                    lightRemainingSeconds / lightDurationSeconds);
                muzzleLight.intensity = lightPeakIntensity * weight * weight;
                if (lightRemainingSeconds <= 0f)
                {
                    ReleaseLightSlot();
                }
            }

            if (ownsSmokeSlot && !IsAlive(smoke))
            {
                activeSmokeEffects = Mathf.Max(0, activeSmokeEffects - 1);
                ownsSmokeSlot = false;
            }

            if (!IsEffectActive)
            {
                enabled = false;
            }
        }

        private void UpdateCore()
        {
            if (effectAgeSeconds >= totalDurationSeconds)
            {
                coreActive = false;
                SetOpacity(0f);
                if (approvedBurst != null)
                {
                    approvedBurst.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                visualRenderer.enabled = false;
                SetVisualScale(initialScaleRatio);
                return;
            }

            float expansion = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(effectAgeSeconds / expansionDurationSeconds));
            SetVisualScale(Vector3.Lerp(
                initialScaleRatio,
                Vector3.one,
                expansion));
            float opacity = effectAgeSeconds <= hotFlashDurationSeconds
                ? 1f
                : Mathf.Pow(
                    1f - Mathf.InverseLerp(
                        hotFlashDurationSeconds,
                        totalDurationSeconds,
                        effectAgeSeconds),
                    1.35f);
            SetOpacity(opacity);
        }

        private void PrepareCore(bool visible)
        {
            EnsurePropertyBlock();
            SetOpacity(visible ? 1f : 0f);
            if (visualRenderer != null)
            {
                visualRenderer.enabled = visible;
            }

            if (visualTransform != null)
            {
                visualTransform.localPosition = anchorLocalPosition;
                visualTransform.localRotation = anchorLocalRotation;
                SetVisualScale(initialScaleRatio);
            }

            coreActive = visible;
        }

        private void SetVisualScale(Vector3 ratio)
        {
            if (visualTransform != null)
            {
                visualTransform.localScale = Vector3.Scale(
                    fullLocalScale,
                    ratio);
            }
        }

        private void OnDisable()
        {
            ReleaseLightSlot();
            if (ownsSmokeSlot)
            {
                activeSmokeEffects = Mathf.Max(0, activeSmokeEffects - 1);
                ownsSmokeSlot = false;
            }
        }

        private void ReleaseLightSlot()
        {
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
                muzzleLight.intensity = 0f;
            }

            if (!ownsLightSlot)
            {
                return;
            }

            activeLights = Mathf.Max(0, activeLights - 1);
            ownsLightSlot = false;
        }

        private static void Restart(ParticleSystem system)
        {
            if (system == null)
            {
                return;
            }

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);
        }

        private static bool IsAlive(ParticleSystem system)
        {
            return system != null && system.IsAlive(true);
        }

        private static int ParticleCount(ParticleSystem system)
        {
            return system != null ? system.particleCount : 0;
        }

        private static int MaxParticles(ParticleSystem system)
        {
            return system != null ? system.main.maxParticles : 0;
        }

        private void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void SetOpacity(float opacity)
        {
            if (visualRenderer == null)
            {
                return;
            }

            EnsurePropertyBlock();
            visualRenderer.GetPropertyBlock(propertyBlock);
            Color color = new Color(
                3.4f,
                3.15f,
                2.65f,
                Mathf.Clamp01(opacity));
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            visualRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
