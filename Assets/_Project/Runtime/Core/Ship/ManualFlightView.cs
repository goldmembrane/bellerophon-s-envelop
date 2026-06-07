using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Ship
{
    public sealed class ManualFlightView : MonoBehaviour
    {
        [SerializeField] private ShipDeviceInteractionState interactionState;
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private Text statusText;
        [SerializeField] private FirstPersonPlayerInput playerInput;

        public GameObject ViewRoot => viewRoot;

        public RectTransform PlayerMarker => playerMarker;

        public Text StatusText => statusText;

        public bool IsViewActive => viewRoot != null && viewRoot.activeSelf;

        public void Configure(
            ShipDeviceInteractionState state,
            GameObject root,
            RectTransform marker,
            Text statusLabel,
            FirstPersonPlayerInput firstPersonInput)
        {
            interactionState = state;
            viewRoot = root;
            playerMarker = marker;
            statusText = statusLabel;
            playerInput = firstPersonInput;
            RefreshView();
        }

        private void Update()
        {
            ProcessManualFlightInput(Time.deltaTime);
            RefreshView();
        }

        private void OnDisable()
        {
            SetPlayerInputSuppressed(false);
        }

        private void OnDestroy()
        {
            SetPlayerInputSuppressed(false);
        }

        public void ProcessManualFlightInput(float deltaSeconds)
        {
            if (interactionState == null ||
                !interactionState.HasActiveTransportRun ||
                !interactionState.ManualFlightModeActive ||
                Keyboard.current == null)
            {
                SetPlayerInputSuppressed(false);
                return;
            }

            SetPlayerInputSuppressed(true);

            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.escapeKey.isPressed)
            {
                interactionState.ExitManualFlightToAutoPilot();
                return;
            }

            var horizontal = 0f;
            var vertical = 0f;
            if (Keyboard.current.aKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                horizontal += 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                vertical += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                vertical -= 1f;
            }

            interactionState.ApplyManualFlightInput(horizontal, vertical, deltaSeconds);
        }

        public void RefreshView()
        {
            var shouldShow = interactionState != null &&
                             interactionState.HasActiveTransportRun &&
                             interactionState.ManualFlightModeActive &&
                             interactionState.ActivePanelMode == ShipDevicePanelMode.ManualFlight;
            if (viewRoot != null && viewRoot.activeSelf != shouldShow)
            {
                viewRoot.SetActive(shouldShow);
            }

            SetPlayerInputSuppressed(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            UpdateMarker();
            if (statusText != null)
            {
                statusText.text = BuildStatusText();
            }
        }

        private void UpdateMarker()
        {
            if (playerMarker == null || viewRoot == null)
            {
                return;
            }

            var rectTransform = viewRoot.GetComponent<RectTransform>();
            var size = rectTransform != null
                ? rectTransform.rect.size
                : new Vector2(640f, 360f);
            var bounds = new Vector2(Mathf.Max(120f, size.x * 0.42f), Mathf.Max(80f, size.y * 0.38f));
            playerMarker.anchoredPosition = new Vector2(
                interactionState.ManualFlightOffsetX * bounds.x,
                interactionState.ManualFlightOffsetY * bounds.y);
        }

        private string BuildStatusText()
        {
            var text = "Manual Flight\n"
                       + "Progress: " + Mathf.RoundToInt(interactionState.TransportProgressPercent * 100f) + "%\n"
                       + "Remaining: " + Mathf.CeilToInt(interactionState.TransportRemainingSeconds) + "s\n";
            if (interactionState.HasActiveTransportHazard)
            {
                text += "Hazard: " + TransportHazardRules.FormatHazardType(interactionState.CurrentTransportHazard.HazardType) + " "
                        + Mathf.CeilToInt(interactionState.CurrentTransportHazard.RemainingSeconds)
                        + "s\n";
            }

            return text + interactionState.LastInteractionSummary;
        }

        private void SetPlayerInputSuppressed(bool suppressed)
        {
            if (playerInput == null)
            {
                playerInput = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }

            if (playerInput != null && playerInput.GameplayInputSuppressed != suppressed)
            {
                playerInput.SetGameplayInputSuppressed(suppressed);
            }
        }
    }
}
