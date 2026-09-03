using UnityEngine;

namespace Bellerophon.Vfx
{
    [DisallowMultipleComponent]
    public sealed class ShotgunMuzzleFlashV4Vfx : MonoBehaviour
    {
        public const int ConcurrentSmokeLimit = 4;
        public const int ConcurrentLightLimit = 4;

        private static int activeSmokeEffects;
        private static int activeLights;

        [SerializeField] private ParticleSystem[] layers;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private Light muzzleLight;
        [SerializeField] private float lightDurationSeconds = 0.06f;
        [SerializeField] private float lightPeakIntensity = 6.5f;

        private bool ownsSmokeSlot;
        private bool ownsLightSlot;
        private float lightRemainingSeconds;

        public int PlayCount { get; private set; }

        public int SystemCount => layers != null ? layers.Length : 0;

        public int MaximumConfiguredParticles
        {
            get
            {
                int total = 0;
                if (layers == null)
                {
                    return total;
                }

                foreach (ParticleSystem layer in layers)
                {
                    if (layer != null)
                    {
                        total += layer.main.maxParticles;
                    }
                }

                return total;
            }
        }

        public int ActiveParticleCount
        {
            get
            {
                int total = 0;
                if (layers == null)
                {
                    return total;
                }

                foreach (ParticleSystem layer in layers)
                {
                    if (layer != null)
                    {
                        total += layer.particleCount;
                    }
                }

                return total;
            }
        }

        public bool CreatesObjectsPerShot => false;

        public Light MuzzleLight => muzzleLight;

        public void Configure(
            ParticleSystem[] configuredLayers,
            ParticleSystem smokeSystem,
            Light lightSource,
            float lightDuration,
            float peakIntensity)
        {
            layers = configuredLayers;
            smoke = smokeSystem;
            muzzleLight = lightSource;
            lightDurationSeconds = Mathf.Max(0.01f, lightDuration);
            lightPeakIntensity = Mathf.Max(0f, peakIntensity);
            ResetLight();
        }

        public void PlayEffect()
        {
            if (layers == null || layers.Length == 0)
            {
                return;
            }

            PlayCount++;
            foreach (ParticleSystem layer in layers)
            {
                if (layer == null || layer == smoke)
                {
                    continue;
                }

                Restart(layer);
            }

            if (smoke != null)
            {
                if (ownsSmokeSlot || activeSmokeEffects < ConcurrentSmokeLimit)
                {
                    if (!ownsSmokeSlot)
                    {
                        activeSmokeEffects++;
                        ownsSmokeSlot = true;
                    }

                    Restart(smoke);
                }
                else
                {
                    smoke.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }
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
            if (layers != null)
            {
                foreach (ParticleSystem layer in layers)
                {
                    if (layer != null)
                    {
                        layer.Stop(
                            true,
                            ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }

            ResetLight();
            enabled = false;
        }

        private void Update()
        {
            if (ownsLightSlot)
            {
                lightRemainingSeconds -= Time.deltaTime;
                float weight = Mathf.Clamp01(
                    lightRemainingSeconds / lightDurationSeconds);
                muzzleLight.intensity =
                    lightPeakIntensity * weight * weight;
                if (lightRemainingSeconds <= 0f)
                {
                    ReleaseLightSlot();
                }
            }

            if (ownsSmokeSlot && (smoke == null || !smoke.IsAlive(true)))
            {
                activeSmokeEffects = Mathf.Max(0, activeSmokeEffects - 1);
                ownsSmokeSlot = false;
            }

            if (!ownsLightSlot && !ownsSmokeSlot && !AnyLayerAlive())
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

        private bool AnyLayerAlive()
        {
            if (layers == null)
            {
                return false;
            }

            foreach (ParticleSystem layer in layers)
            {
                if (layer != null && layer.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReleaseLightSlot()
        {
            ResetLight();
            if (!ownsLightSlot)
            {
                return;
            }

            activeLights = Mathf.Max(0, activeLights - 1);
            ownsLightSlot = false;
        }

        private void ResetLight()
        {
            lightRemainingSeconds = 0f;
            if (muzzleLight != null)
            {
                muzzleLight.intensity = 0f;
                muzzleLight.enabled = false;
            }
        }

        private static void Restart(ParticleSystem system)
        {
            system.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);
        }
    }
}
