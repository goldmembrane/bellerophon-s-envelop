using UnityEngine;
using UnityEngine.UI;

namespace Bellerophon.Core.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(FirstPersonPlayerStatus))]
    public sealed class PlayerHitFlash : MonoBehaviour
    {
        // Approved artSample/player_hit_flash specification. One overlay, no additive stacking.
        public const float PeakAlpha = .18f;
        public const float RiseSeconds = .03f;
        public const float DurationSeconds = .25f;
        private FirstPersonPlayerStatus status;
        private GameObject overlay;
        private Image image;
        private float startedAt = float.NegativeInfinity;
        private float startAlpha;
        public float Alpha => image ? image.color.a : 0f;
        public float Elapsed => Time.unscaledTime - startedAt;

        private void Awake()
        {
            status = GetComponent<FirstPersonPlayerStatus>();
            overlay = new GameObject("Player Hit Flash Overlay", typeof(RectTransform), typeof(Canvas));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var panel = new GameObject("Approved Red Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            image = panel.GetComponent<Image>();
            image.raycastTarget = false;
            SetAlpha(0);
        }

        private void OnEnable()
        {
            status.DamageTaken += OnDamageTaken;
            overlay.SetActive(true);
        }

        private void OnDisable()
        {
            if (status) status.DamageTaken -= OnDamageTaken;
            startedAt = float.NegativeInfinity;
            if (image) SetAlpha(0);
            if (overlay) overlay.SetActive(false);
        }

        private void OnDestroy() { if (overlay) Destroy(overlay); }

        private void OnDamageTaken()
        {
            startAlpha = Evaluate(Time.unscaledTime - startedAt);
            startedAt = Time.unscaledTime;
            SetAlpha(startAlpha);
        }

        private float Evaluate(float elapsed)
        {
            if (elapsed >= DurationSeconds) return 0;
            if (elapsed < RiseSeconds) return Mathf.Lerp(startAlpha, PeakAlpha, elapsed / RiseSeconds);
            return Mathf.Lerp(PeakAlpha, 0, (elapsed - RiseSeconds) / (DurationSeconds - RiseSeconds));
        }

        private void LateUpdate() { SetAlpha(Evaluate(Elapsed)); }
        private void SetAlpha(float alpha)
        {
            image.color = new Color(224f / 255f, 32f / 255f, 32f / 255f, Mathf.Clamp(alpha, 0, PeakAlpha));
            image.enabled = alpha > 0;
        }
    }
}
