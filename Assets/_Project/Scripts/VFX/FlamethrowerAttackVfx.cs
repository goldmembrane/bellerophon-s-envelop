using UnityEngine;

namespace Bellerophon.Vfx
{
    [DisallowMultipleComponent]
    public sealed class FlamethrowerAttackVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] layers;
        [SerializeField] private float minimumEffectiveRangeMeters = 1f;
        [SerializeField] private float maximumEffectiveRangeMeters = 3f;
        [SerializeField] private float maximumEmissionSeconds = 10f;
        [SerializeField] private float cooldownSeconds = 2.5f;

        private float emissionElapsedSeconds;
        private float cooldownRemainingSeconds;
        private bool repeatRequested;

        public bool IsEmitting { get; private set; }

        public bool RepeatRequested => repeatRequested;

        public float MinimumEffectiveRangeMeters =>
            minimumEffectiveRangeMeters;

        public float MaximumEffectiveRangeMeters =>
            maximumEffectiveRangeMeters;

        public float MaximumEmissionSeconds => maximumEmissionSeconds;

        public float CooldownSeconds => cooldownSeconds;

        public float EmissionElapsedSeconds => emissionElapsedSeconds;

        public float CooldownRemainingSeconds => cooldownRemainingSeconds;

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

        public void Configure(
            ParticleSystem[] configuredLayers,
            float minimumRangeMeters,
            float maximumRangeMeters,
            float maximumDurationSeconds,
            float reuseCooldownSeconds)
        {
            layers = configuredLayers;
            minimumEffectiveRangeMeters = Mathf.Max(0f, minimumRangeMeters);
            maximumEffectiveRangeMeters = Mathf.Max(
                minimumEffectiveRangeMeters,
                maximumRangeMeters);
            maximumEmissionSeconds = Mathf.Max(0.01f, maximumDurationSeconds);
            cooldownSeconds = Mathf.Max(0f, reuseCooldownSeconds);
            StopEmissionImmediate();
        }

        public bool TryStartEmission()
        {
            if (IsEmitting || cooldownRemainingSeconds > 0f ||
                layers == null || layers.Length == 0)
            {
                return false;
            }

            emissionElapsedSeconds = 0f;
            IsEmitting = true;
            foreach (ParticleSystem layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                layer.Play(true);
            }

            enabled = true;
            return true;
        }

        public void BeginRepeatingEmission()
        {
            repeatRequested = true;
            enabled = true;
            TryStartEmission();
        }

        public void EndRepeatingEmission()
        {
            repeatRequested = false;
            StopEmission();
        }

        public void StopEmission()
        {
            if (!IsEmitting)
            {
                return;
            }

            IsEmitting = false;
            cooldownRemainingSeconds = cooldownSeconds;
            foreach (ParticleSystem layer in layers)
            {
                if (layer != null)
                {
                    layer.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmitting);
                }
            }

            enabled = true;
        }

        public void StopEmissionImmediate()
        {
            IsEmitting = false;
            repeatRequested = false;
            emissionElapsedSeconds = 0f;
            cooldownRemainingSeconds = 0f;
            if (layers == null)
            {
                return;
            }

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

        private void Awake()
        {
            StopEmissionImmediate();
            enabled = false;
        }

        private void Update()
        {
            if (IsEmitting)
            {
                emissionElapsedSeconds += Time.deltaTime;
                if (emissionElapsedSeconds >= maximumEmissionSeconds)
                {
                    StopEmission();
                }
            }
            else if (cooldownRemainingSeconds > 0f)
            {
                cooldownRemainingSeconds = Mathf.Max(
                    0f,
                    cooldownRemainingSeconds - Time.deltaTime);
            }

            if (!IsEmitting && repeatRequested &&
                cooldownRemainingSeconds <= 0f)
            {
                TryStartEmission();
            }

            if (!IsEmitting && !repeatRequested &&
                cooldownRemainingSeconds <= 0f &&
                !AnyLayerAlive())
            {
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (!gameObject.activeInHierarchy)
            {
                StopEmissionImmediate();
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
    }
}
