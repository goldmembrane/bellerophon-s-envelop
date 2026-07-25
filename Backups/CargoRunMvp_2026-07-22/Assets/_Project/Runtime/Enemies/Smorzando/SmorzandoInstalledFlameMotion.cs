using UnityEngine;

namespace Bellerophon.Enemies.Smorzando
{
    [DisallowMultipleComponent]
    public sealed class SmorzandoInstalledFlameMotion : MonoBehaviour
    {
        [Header("Air-flow response")]
        [SerializeField] private Transform flameEffectRoot;
        [SerializeField] private Light flameLight;
        [SerializeField, Min(0.1f)] private float cycleDurationSeconds = 3.2f;
        [SerializeField, Min(0f)] private float phaseOffsetSeconds;
        [SerializeField, Min(0f)] private float flameBaseIntensity = 0.45f;
        [SerializeField, Min(0f)] private float flameFlickerAmount = 0.12f;

        private Vector3 flameEffectBaseScale;
        private Quaternion flameEffectBaseRotation;
        private float elapsedSeconds;
        private bool previewPrepared;

        public float PhaseOffsetSeconds => phaseOffsetSeconds;

        public void Configure(
            Transform configuredFlameEffectRoot,
            Light configuredFlameLight,
            float configuredPhaseOffsetSeconds)
        {
            flameEffectRoot = configuredFlameEffectRoot;
            flameLight = configuredFlameLight;
            phaseOffsetSeconds = Mathf.Max(0f, configuredPhaseOffsetSeconds);
            CaptureBasePose();
        }

        public void PreparePreview()
        {
            CaptureBasePose();
            previewPrepared = true;
        }

        public void SampleAtTime(float timeSeconds)
        {
            ApplyPose(Mathf.Max(0f, timeSeconds) + phaseOffsetSeconds);
        }

        public void RestoreBasePose()
        {
            if (flameEffectRoot != null)
            {
                flameEffectRoot.localScale = flameEffectBaseScale;
                flameEffectRoot.localRotation = flameEffectBaseRotation;
            }

            if (flameLight != null)
            {
                flameLight.intensity = flameBaseIntensity;
            }

            previewPrepared = false;
        }

        private void Awake()
        {
            CaptureBasePose();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds = 0f;
            CaptureBasePose();
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
                RestoreBasePose();
            }
        }

        private void ApplyPose(float timeSeconds)
        {
            var duration = Mathf.Max(cycleDurationSeconds, 0.1f);
            var phase = Mathf.Repeat(timeSeconds, duration) / duration * Mathf.PI * 2f;
            if (flameEffectRoot != null)
            {
                var swayX = Mathf.Sin(phase * 2.17f + 0.6f) * 3.2f;
                var swayY = Mathf.Sin(phase * 1.73f - 0.35f) * 2.4f;
                flameEffectRoot.localRotation =
                    flameEffectBaseRotation * Quaternion.Euler(swayX, swayY, 0f);
                var widthFlicker = Mathf.Sin(phase * 2.41f + 1.1f) * 0.05f;
                var heightFlicker = Mathf.Sin(phase * 3.13f - 0.4f) * 0.08f;
                flameEffectRoot.localScale = Vector3.Scale(
                    flameEffectBaseScale,
                    new Vector3(1f + widthFlicker, 1f + widthFlicker, 1f + heightFlicker));
            }

            if (flameLight != null)
            {
                var flicker = 1f + Mathf.Sin(phase * 2.67f + 0.9f) * flameFlickerAmount;
                flameLight.intensity = flameBaseIntensity * flicker;
            }
        }

        private void CaptureBasePose()
        {
            if (flameEffectRoot == null)
            {
                return;
            }

            flameEffectBaseScale = flameEffectRoot.localScale;
            flameEffectBaseRotation = flameEffectRoot.localRotation;
        }
    }
}
