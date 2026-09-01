using UnityEngine;

namespace Bellerophon.Vfx
{
    [DisallowMultipleComponent]
    public sealed class MusketMuzzleFlashVfx : MonoBehaviour
    {
        public const int ConcurrentSmokeLimit = 6;
        public const int ConcurrentLightLimit = 4;

        private static int activeSmokeEffects;
        private static int activeLights;

        [SerializeField] private ParticleSystem flash;
        [SerializeField] private ParticleSystem hotGas;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem embers;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private float lightDurationSeconds = 0.055f;
        [SerializeField] private float lightPeakIntensity = 3.25f;

        private bool ownsSmokeSlot;
        private bool ownsLightSlot;
        private float effectAgeSeconds;
        private float lightRemainingSeconds;

        public int PlayCount { get; private set; }

        public float EffectAgeSeconds => effectAgeSeconds;

        public bool IsEffectActive => ownsSmokeSlot || ownsLightSlot ||
            IsAlive(flash) || IsAlive(hotGas) || IsAlive(smoke) ||
            IsAlive(embers);

        public int ActiveParticleCount => ParticleCount(flash) +
            ParticleCount(hotGas) + ParticleCount(smoke) +
            ParticleCount(embers);

        public int FlashParticleCount => ParticleCount(flash);

        public int HotGasParticleCount => ParticleCount(hotGas);

        public int SmokeParticleCount => ParticleCount(smoke);

        public int EmberParticleCount => ParticleCount(embers);

        public int SystemCount => (flash != null ? 1 : 0) +
            (hotGas != null ? 1 : 0) + (smoke != null ? 1 : 0) +
            (embers != null ? 1 : 0);

        public int MaximumConfiguredParticles => MaxParticles(flash) +
            MaxParticles(hotGas) + MaxParticles(smoke) +
            MaxParticles(embers);

        public Light MuzzleLight => muzzleLight;

        public void Configure(
            ParticleSystem flashSystem,
            ParticleSystem hotGasSystem,
            ParticleSystem smokeSystem,
            ParticleSystem emberSystem,
            Light lightSource,
            float lightDuration,
            float peakIntensity)
        {
            flash = flashSystem;
            hotGas = hotGasSystem;
            smoke = smokeSystem;
            embers = emberSystem;
            muzzleLight = lightSource;
            lightDurationSeconds = Mathf.Max(0.01f, lightDuration);
            lightPeakIntensity = Mathf.Max(0f, peakIntensity);
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }
        }

        public void PlayEffect()
        {
            PlayCount++;
            effectAgeSeconds = 0f;
            Restart(flash);
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
            if (muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }

            enabled = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            effectAgeSeconds += deltaTime;
            if (ownsLightSlot)
            {
                lightRemainingSeconds -= deltaTime;
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
    }
}
