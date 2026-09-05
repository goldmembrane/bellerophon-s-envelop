using UnityEngine;

namespace Bellerophon.Vfx
{
    [DisallowMultipleComponent]
    public sealed class FlamethrowerTankExplosionVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] layers;
        [SerializeField] private Renderer[] assemblyRenderers;
        [SerializeField] private float explosionTimeSeconds = 1f;
        [SerializeField] private float cycleDurationSeconds = 3f;
        [SerializeField] private float visualRadiusMeters = 6f;
        [SerializeField] private float effectDurationSeconds = 1.8f;

        private float cycleElapsedSeconds;
        private int completedCycleCount;
        private bool loopRequested;

        public bool ExplosionStarted { get; private set; }

        public bool LoopRequested => loopRequested;

        public bool AssemblyVisible => !ExplosionStarted;

        public float CycleElapsedSeconds => cycleElapsedSeconds;

        public int CompletedCycleCount => completedCycleCount;

        public float ExplosionTimeSeconds => explosionTimeSeconds;

        public float CycleDurationSeconds => cycleDurationSeconds;

        public float VisualRadiusMeters => visualRadiusMeters;

        public float EffectDurationSeconds => effectDurationSeconds;

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
            Renderer[] configuredAssemblyRenderers,
            float configuredExplosionTimeSeconds,
            float configuredCycleDurationSeconds,
            float configuredVisualRadiusMeters,
            float configuredEffectDurationSeconds)
        {
            layers = configuredLayers;
            assemblyRenderers = configuredAssemblyRenderers;
            explosionTimeSeconds = Mathf.Max(
                0f,
                configuredExplosionTimeSeconds);
            cycleDurationSeconds = Mathf.Max(
                explosionTimeSeconds + 0.01f,
                configuredCycleDurationSeconds);
            visualRadiusMeters = Mathf.Max(
                0.01f,
                configuredVisualRadiusMeters);
            effectDurationSeconds = Mathf.Clamp(
                configuredEffectDurationSeconds,
                0.01f,
                cycleDurationSeconds - explosionTimeSeconds);
            EndLooping();
        }

        public void BeginLooping()
        {
            loopRequested = true;
            completedCycleCount = 0;
            ResetCycle();
            enabled = true;
        }

        public void EndLooping()
        {
            loopRequested = false;
            ResetCycle();
            enabled = false;
        }

        private void Awake()
        {
            loopRequested = false;
            completedCycleCount = 0;
            ResetCycle();
            enabled = false;
        }

        private void Update()
        {
            if (!loopRequested)
            {
                return;
            }

            cycleElapsedSeconds += Time.deltaTime;
            while (cycleElapsedSeconds >= cycleDurationSeconds)
            {
                cycleElapsedSeconds -= cycleDurationSeconds;
                completedCycleCount++;
                ResetCycleState();
            }

            if (!ExplosionStarted &&
                cycleElapsedSeconds >= explosionTimeSeconds)
            {
                TriggerExplosion();
            }
        }

        private void OnDisable()
        {
            if (!gameObject.activeInHierarchy)
            {
                loopRequested = false;
                ResetCycle();
            }
        }

        private void TriggerExplosion()
        {
            ExplosionStarted = true;
            SetAssemblyVisible(false);
            if (layers == null)
            {
                return;
            }

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
        }

        private void ResetCycle()
        {
            cycleElapsedSeconds = 0f;
            ResetCycleState();
        }

        private void ResetCycleState()
        {
            ExplosionStarted = false;
            SetAssemblyVisible(true);
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

        private void SetAssemblyVisible(bool visible)
        {
            if (assemblyRenderers == null)
            {
                return;
            }

            foreach (Renderer assemblyRenderer in assemblyRenderers)
            {
                if (assemblyRenderer != null)
                {
                    assemblyRenderer.enabled = visible;
                }
            }
        }
    }
}
