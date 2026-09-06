using UnityEngine;

namespace Bellerophon.Vfx
{
    [DisallowMultipleComponent]
    public sealed class BatonDischargeFireCycle : MonoBehaviour
    {
        [SerializeField] private GameObject dischargeEffect;
        [SerializeField] private float dischargeSeconds = 1.6f;
        [SerializeField] private float postDischargeHoldSeconds = 0.5f;

        private bool loopRequested;
        private int completedCycleCount;
        private float cycleElapsedSeconds;

        public bool LoopRequested => loopRequested;

        public bool DischargeVisible =>
            dischargeEffect != null && dischargeEffect.activeSelf;

        public int CompletedCycleCount => completedCycleCount;

        public float CycleElapsedSeconds => cycleElapsedSeconds;

        public float DischargeSeconds => dischargeSeconds;

        public float PostDischargeHoldSeconds => postDischargeHoldSeconds;

        public float CycleDurationSeconds =>
            dischargeSeconds + postDischargeHoldSeconds;

        public void Configure(
            GameObject configuredDischargeEffect,
            float configuredDischargeSeconds,
            float configuredPostDischargeHoldSeconds)
        {
            dischargeEffect = configuredDischargeEffect;
            dischargeSeconds = Mathf.Max(0.01f, configuredDischargeSeconds);
            postDischargeHoldSeconds = Mathf.Max(
                0.01f,
                configuredPostDischargeHoldSeconds);
            EndLooping();
        }

        public void BeginLooping()
        {
            loopRequested = true;
            completedCycleCount = 0;
            cycleElapsedSeconds = 0f;
            ApplyVisibility();
        }

        public void EvaluateNormalizedTime(float normalizedTime)
        {
            if (!loopRequested)
            {
                return;
            }

            float clampedNormalized = Mathf.Max(0f, normalizedTime);
            completedCycleCount = Mathf.FloorToInt(clampedNormalized);
            cycleElapsedSeconds = Mathf.Repeat(
                clampedNormalized,
                1f) * CycleDurationSeconds;
            ApplyVisibility();
        }

        public void EndLooping()
        {
            loopRequested = false;
            completedCycleCount = 0;
            cycleElapsedSeconds = 0f;
            SetDischargeVisible(false);
        }

        private void Awake()
        {
            EndLooping();
        }

        private void OnDisable()
        {
            EndLooping();
        }

        private void ApplyVisibility()
        {
            SetDischargeVisible(cycleElapsedSeconds < dischargeSeconds);
        }

        private void SetDischargeVisible(bool visible)
        {
            if (dischargeEffect != null &&
                dischargeEffect.activeSelf != visible)
            {
                dischargeEffect.SetActive(visible);
            }
        }
    }
}
