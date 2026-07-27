using System;
using UnityEngine;

namespace Bellerophon.Enemies.Resistance
{
    public enum ResistanceDeathExplosionPhase
    {
        DeathMotion,
        Explosion
    }

    public readonly struct ResistanceDeathExplosionSample
    {
        public ResistanceDeathExplosionSample(
            ResistanceDeathExplosionPhase phase,
            float cycleTimeSeconds,
            float deathNormalizedTime,
            float explosionTimeSeconds)
        {
            Phase = phase;
            CycleTimeSeconds = cycleTimeSeconds;
            DeathNormalizedTime = deathNormalizedTime;
            ExplosionTimeSeconds = explosionTimeSeconds;
        }

        public ResistanceDeathExplosionPhase Phase { get; }
        public float CycleTimeSeconds { get; }
        public float DeathNormalizedTime { get; }
        public float ExplosionTimeSeconds { get; }
        public bool ModelVisible =>
            Phase == ResistanceDeathExplosionPhase.DeathMotion;
    }

    public static class ResistanceDeathExplosionTimeline
    {
        public static ResistanceDeathExplosionSample Evaluate(
            float elapsedSeconds,
            float deathDurationSeconds,
            float explosionDurationSeconds)
        {
            var deathDuration = Mathf.Max(0.0001f, deathDurationSeconds);
            var explosionDuration =
                Mathf.Max(0.0001f, explosionDurationSeconds);
            var cycleDuration = deathDuration + explosionDuration;
            var cycleTime = Mathf.Repeat(
                Mathf.Max(0f, elapsedSeconds),
                cycleDuration);
            if (cycleTime < deathDuration)
            {
                return new ResistanceDeathExplosionSample(
                    ResistanceDeathExplosionPhase.DeathMotion,
                    cycleTime,
                    Mathf.Clamp01(cycleTime / deathDuration),
                    0f);
            }

            return new ResistanceDeathExplosionSample(
                ResistanceDeathExplosionPhase.Explosion,
                cycleTime,
                1f,
                Mathf.Clamp(
                    cycleTime - deathDuration,
                    0f,
                    explosionDuration));
        }
    }

    [DisallowMultipleComponent]
    public sealed class ResistanceDeathExplosionLoop : MonoBehaviour
    {
        [Header("Death sequence")]
        [SerializeField] private Animator animator;
        [SerializeField] private string deathStateName =
            "Resistance_Death_Mixamo";
        [SerializeField, Min(0.0001f)] private float deathDurationSeconds =
            3.7f;
        [SerializeField, Min(0.0001f)] private float explosionDurationSeconds =
            0.8f;

        [Header("Visibility and explosion")]
        [SerializeField] private Renderer[] modelRenderers =
            Array.Empty<Renderer>();
        [SerializeField] private ParticleSystem[] explosionParticles =
            Array.Empty<ParticleSystem>();
        [SerializeField] private Light explosionLight;
        [SerializeField, Min(0f)] private float maximumLightIntensity = 5f;
        [SerializeField, Min(0f)] private float explosionDiameterMeters = 2f;

        private float elapsedSeconds;

        public Animator Animator => animator;
        public string DeathStateName => deathStateName;
        public float DeathDurationSeconds => deathDurationSeconds;
        public float ExplosionDurationSeconds => explosionDurationSeconds;
        public float CycleDurationSeconds =>
            deathDurationSeconds + explosionDurationSeconds;
        public float ExplosionDiameterMeters => explosionDiameterMeters;
        public Renderer[] ModelRenderers => modelRenderers;
        public ParticleSystem[] ExplosionParticles => explosionParticles;
        public Light ExplosionLight => explosionLight;

        public void Configure(
            Animator configuredAnimator,
            string configuredDeathStateName,
            float configuredDeathDurationSeconds,
            float configuredExplosionDurationSeconds,
            Renderer[] configuredModelRenderers,
            ParticleSystem[] configuredExplosionParticles,
            Light configuredExplosionLight,
            float configuredMaximumLightIntensity,
            float configuredExplosionDiameterMeters)
        {
            animator = configuredAnimator;
            deathStateName = configuredDeathStateName;
            deathDurationSeconds =
                Mathf.Max(0.0001f, configuredDeathDurationSeconds);
            explosionDurationSeconds =
                Mathf.Max(0.0001f, configuredExplosionDurationSeconds);
            modelRenderers =
                configuredModelRenderers ?? Array.Empty<Renderer>();
            explosionParticles =
                configuredExplosionParticles ??
                Array.Empty<ParticleSystem>();
            explosionLight = configuredExplosionLight;
            maximumLightIntensity =
                Mathf.Max(0f, configuredMaximumLightIntensity);
            explosionDiameterMeters =
                Mathf.Max(0f, configuredExplosionDiameterMeters);
            ResetSequence();
        }

        public void ResetSequence()
        {
            elapsedSeconds = 0f;
            SampleAtSequenceTime(0f);
        }

        public ResistanceDeathExplosionSample SampleAtSequenceTime(
            float sequenceTimeSeconds)
        {
            var sample = ResistanceDeathExplosionTimeline.Evaluate(
                sequenceTimeSeconds,
                deathDurationSeconds,
                explosionDurationSeconds);
            SampleAnimator(sample.DeathNormalizedTime);
            SetModelVisible(sample.ModelVisible);
            SampleExplosion(sample);
            return sample;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                ResetSequence();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            var cycleDuration = CycleDurationSeconds;
            if (elapsedSeconds >= cycleDuration)
            {
                elapsedSeconds = Mathf.Repeat(
                    elapsedSeconds,
                    cycleDuration);
            }

            SampleAtSequenceTime(elapsedSeconds);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                ResetSequence();
            }
        }

        private void SampleAnimator(float normalizedTime)
        {
            if (animator == null ||
                animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.speed = 0f;
            var stateHash = Animator.StringToHash(
                "Base Layer." + deathStateName);
            animator.Play(
                stateHash,
                0,
                Mathf.Clamp01(normalizedTime));
            animator.Update(0f);
        }

        private void SetModelVisible(bool visible)
        {
            foreach (var modelRenderer in modelRenderers)
            {
                if (modelRenderer != null)
                {
                    modelRenderer.enabled = visible;
                }
            }
        }

        private void SampleExplosion(
            ResistanceDeathExplosionSample sample)
        {
            if (sample.Phase ==
                ResistanceDeathExplosionPhase.DeathMotion)
            {
                foreach (var particleSystem in explosionParticles)
                {
                    if (particleSystem == null)
                    {
                        continue;
                    }

                    particleSystem.Stop(
                        true,
                        ParticleSystemStopBehavior
                            .StopEmittingAndClear);
                }

                if (explosionLight != null)
                {
                    explosionLight.enabled = false;
                    explosionLight.intensity = 0f;
                }

                return;
            }

            foreach (var particleSystem in explosionParticles)
            {
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Simulate(
                    sample.ExplosionTimeSeconds,
                    false,
                    true,
                    false);
            }

            if (explosionLight != null)
            {
                var normalized = Mathf.Clamp01(
                    sample.ExplosionTimeSeconds /
                    explosionDurationSeconds);
                explosionLight.enabled = normalized < 1f;
                explosionLight.intensity =
                    maximumLightIntensity *
                    Mathf.Sin(normalized * Mathf.PI) *
                    (1f - normalized * 0.35f);
            }
        }
    }
}
