using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Ship
{
    public sealed class ManualTurretView : MonoBehaviour
    {
        [SerializeField] private ShipDeviceInteractionState interactionState;
        [SerializeField] private GameObject viewRoot;
        [SerializeField] private RectTransform reticleMarker;
        [SerializeField] private RectTransform targetMarker;
        [SerializeField] private Text statusText;
        [SerializeField] private FirstPersonPlayerInput playerInput;

        private float heldFireCooldownSeconds;

        public GameObject ViewRoot => viewRoot;

        public RectTransform ReticleMarker => reticleMarker;

        public RectTransform TargetMarker => targetMarker;

        public Text StatusText => statusText;

        public bool IsViewActive => viewRoot != null && viewRoot.activeSelf;

        public void Configure(
            ShipDeviceInteractionState state,
            GameObject root,
            RectTransform reticle,
            RectTransform target,
            Text statusLabel,
            FirstPersonPlayerInput firstPersonInput)
        {
            interactionState = state;
            viewRoot = root;
            reticleMarker = reticle;
            targetMarker = target;
            statusText = statusLabel;
            playerInput = firstPersonInput;
            RefreshView();
        }

        private void Update()
        {
            ProcessManualTurretInput(Time.deltaTime);
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

        public void ProcessManualTurretInput(float deltaSeconds)
        {
            if (interactionState == null ||
                !interactionState.TurretManualModeActive ||
                interactionState.ActivePanelMode != ShipDevicePanelMode.TurretManual)
            {
                SetPlayerInputSuppressed(false);
                heldFireCooldownSeconds = 0f;
                return;
            }

            SetPlayerInputSuppressed(true);

            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    interactionState.ExitManualTurretMode();
                    return;
                }

                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    interactionState.BeginManualTurretReload();
                }
            }

            if (Mouse.current == null)
            {
                return;
            }

            var delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0001f)
            {
                interactionState.ApplyManualTurretAimInput(delta.x, delta.y);
            }

            ProcessHeldFire(
                deltaSeconds,
                Mouse.current.leftButton.isPressed,
                Mouse.current.leftButton.wasPressedThisFrame);
        }

        public ManualTurretFireResult ProcessHeldFireForValidation(
            float deltaSeconds,
            bool isPressed,
            bool wasPressedThisFrame)
        {
            return ProcessHeldFire(deltaSeconds, isPressed, wasPressedThisFrame);
        }

        public void RefreshView()
        {
            var shouldShow = interactionState != null &&
                             interactionState.TurretManualModeActive &&
                             interactionState.ActivePanelMode == ShipDevicePanelMode.TurretManual;
            if (viewRoot != null && viewRoot.activeSelf != shouldShow)
            {
                viewRoot.SetActive(shouldShow);
            }

            SetPlayerInputSuppressed(shouldShow);

            if (!shouldShow)
            {
                heldFireCooldownSeconds = 0f;
                return;
            }

            EnsureOpaqueBackground();
            UpdateReticle();
            UpdateTarget();
            if (statusText != null)
            {
                statusText.text = BuildStatusText();
            }
        }

        private ManualTurretFireResult ProcessHeldFire(
            float deltaSeconds,
            bool isPressed,
            bool wasPressedThisFrame)
        {
            if (interactionState == null)
            {
                return default;
            }

            heldFireCooldownSeconds = Mathf.Max(0f, heldFireCooldownSeconds - Mathf.Max(0f, deltaSeconds));
            if (!isPressed)
            {
                heldFireCooldownSeconds = 0f;
                return new ManualTurretFireResult(
                    ManualTurretFireOutcome.None,
                    interactionState.CurrentManualTurret,
                    interactionState.CurrentExternalTarget,
                    0);
            }

            if (!wasPressedThisFrame && heldFireCooldownSeconds > 0f)
            {
                return new ManualTurretFireResult(
                    ManualTurretFireOutcome.None,
                    interactionState.CurrentManualTurret,
                    interactionState.CurrentExternalTarget,
                    0);
            }

            var result = interactionState.FireManualTurret();
            heldFireCooldownSeconds = ManualTurretState.HeldFireIntervalSeconds;
            return result;
        }

        private void EnsureOpaqueBackground()
        {
            if (viewRoot == null)
            {
                return;
            }

            var background = viewRoot.GetComponent<Image>();
            if (background == null)
            {
                return;
            }

            var color = background.color;
            if (color.a < 1f)
            {
                color.a = 1f;
                background.color = color;
            }
        }

        private void UpdateReticle()
        {
            if (reticleMarker == null || viewRoot == null)
            {
                return;
            }

            var turret = interactionState.CurrentManualTurret;
            reticleMarker.anchoredPosition = CalculateMarkerPosition(turret.AimX, turret.AimY);
        }

        private void UpdateTarget()
        {
            if (targetMarker == null || viewRoot == null)
            {
                return;
            }

            var target = interactionState.CurrentExternalTarget;
            targetMarker.gameObject.SetActive(target.IsActive);
            if (!target.IsActive)
            {
                return;
            }

            targetMarker.anchoredPosition = CalculateMarkerPosition(target.PositionX, target.PositionY);
        }

        private Vector2 CalculateMarkerPosition(float normalizedX, float normalizedY)
        {
            var rectTransform = viewRoot != null ? viewRoot.GetComponent<RectTransform>() : null;
            var size = rectTransform != null
                ? rectTransform.rect.size
                : new Vector2(640f, 360f);
            var bounds = new Vector2(Mathf.Max(120f, size.x * 0.42f), Mathf.Max(80f, size.y * 0.38f));
            return new Vector2(normalizedX * bounds.x, normalizedY * bounds.y);
        }

        private string BuildStatusText()
        {
            var turret = interactionState.CurrentManualTurret;
            var target = interactionState.CurrentExternalTarget;
            var text = "Manual Turret\n"
                       + "Ammo: " + turret.AmmoInMagazine + "/" + ManualTurretState.MagazineSize + "\n";
            if (turret.IsReloading)
            {
                text += "Reload: " + Mathf.CeilToInt(turret.ReloadRemainingSeconds) + "s\n";
            }

            text += target.IsActive
                ? "Target: " + FormatTargetType(target.TargetType) + " " + target.CurrentHealth + "/" + target.MaxHealth + "\n"
                : "Target: None\n";
            if (interactionState.HasActiveTransportHazard)
            {
                text += "Hazard: Asteroid Field "
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

        private static string FormatTargetType(ExternalTargetType targetType)
        {
            switch (targetType)
            {
                case ExternalTargetType.Asteroid:
                    return "Asteroid";
                default:
                    return "None";
            }
        }
    }
}
