using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace Bellerophon.Core.Player
{
    public sealed class PlayerEquipmentController : MonoBehaviour
    {
        [SerializeField] private FirstPersonHandInventory handInventory;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private Text equipmentHudText;
        [SerializeField] private Text precisionAimReticleText;
        [SerializeField] private bool alternateModeActive;
        [SerializeField] private EquipmentItemKind alternateModeItemKind;

        public Text EquipmentHudText => equipmentHudText;

        public Text PrecisionAimReticleText => precisionAimReticleText;

        public bool AlternateModeActive => IsAlternateModeActive();

        public ShipDeviceInteractionState ShipDeviceState => shipDeviceState;

        public void Configure(
            FirstPersonHandInventory inventory,
            FirstPersonPlayerInput input,
            ShipDeviceInteractionState deviceState,
            Text hudText,
            Text reticleText)
        {
            handInventory = inventory;
            playerInput = input;
            shipDeviceState = deviceState;
            equipmentHudText = hudText;
            precisionAimReticleText = reticleText;
            SubscribeInput();
            RefreshHud();
        }

        private void Awake()
        {
            if (handInventory == null)
            {
                handInventory = GetComponent<FirstPersonHandInventory>();
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<FirstPersonPlayerInput>();
            }

            if (shipDeviceState == null)
            {
                shipDeviceState = Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            }
        }

        private void OnEnable()
        {
            SubscribeInput();
            RefreshHud();
        }

        private void Update()
        {
            RefreshHud();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            HideReticle();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        public EquipmentUseResult UseActiveEquipmentForValidation(bool alternateMode)
        {
            if (shipDeviceState == null)
            {
                return default;
            }

            var result = shipDeviceState.UseActiveEquipment(alternateMode);
            RefreshHud();
            return result;
        }

        public void RefreshHudForValidation()
        {
            RefreshHud();
        }

        public EquipmentUseResult ReloadActiveEquipmentForValidation()
        {
            if (shipDeviceState == null)
            {
                return default;
            }

            var result = shipDeviceState.ReloadActiveEquipment();
            RefreshHud();
            return result;
        }

        public EquipmentUseResult DropActiveEquipmentForValidation()
        {
            if (shipDeviceState == null)
            {
                return default;
            }

            var result = shipDeviceState.DropActiveEquipment();
            RefreshHud();
            return result;
        }

        private void SubscribeInput()
        {
            if (handInventory != null)
            {
                handInventory.UseRequested -= HandleUseRequested;
                handInventory.DropRequested -= HandleDropRequested;
                handInventory.SlotSelected -= HandleSlotSelected;
                handInventory.UseRequested += HandleUseRequested;
                handInventory.DropRequested += HandleDropRequested;
                handInventory.SlotSelected += HandleSlotSelected;
            }

            if (playerInput != null)
            {
                playerInput.ReloadPressed -= HandleReloadPressed;
                playerInput.AimPressed -= HandleAimPressed;
                playerInput.ReloadPressed += HandleReloadPressed;
                playerInput.AimPressed += HandleAimPressed;
            }
        }

        private void UnsubscribeInput()
        {
            if (handInventory != null)
            {
                handInventory.UseRequested -= HandleUseRequested;
                handInventory.DropRequested -= HandleDropRequested;
                handInventory.SlotSelected -= HandleSlotSelected;
            }

            if (playerInput != null)
            {
                playerInput.ReloadPressed -= HandleReloadPressed;
                playerInput.AimPressed -= HandleAimPressed;
            }
        }

        private void HandleUseRequested(int slotIndex)
        {
            if (shipDeviceState == null)
            {
                return;
            }

            if (slotIndex >= 0 && slotIndex < shipDeviceState.HandSlotCount)
            {
                shipDeviceState.SelectEquipmentHandSlot(slotIndex);
            }

            shipDeviceState.UseActiveEquipment(IsAlternateModeActive());
            RefreshHud();
        }

        private void HandleSlotSelected(int slotIndex)
        {
            if (shipDeviceState == null ||
                slotIndex < 0 ||
                slotIndex >= shipDeviceState.HandSlotCount)
            {
                return;
            }

            shipDeviceState.SelectEquipmentHandSlot(slotIndex);
            RefreshHud();
        }

        private void HandleDropRequested(int slotIndex)
        {
            if (shipDeviceState == null)
            {
                return;
            }

            if (slotIndex >= 0 && slotIndex < shipDeviceState.HandSlotCount)
            {
                shipDeviceState.SelectEquipmentHandSlot(slotIndex);
            }

            shipDeviceState.DropActiveEquipment();
            RefreshHud();
        }

        private void HandleReloadPressed()
        {
            if (shipDeviceState == null)
            {
                return;
            }

            shipDeviceState.ReloadActiveEquipment();
            RefreshHud();
        }

        private void HandleAimPressed()
        {
            ToggleAlternateMode();
        }

        private void RefreshHud()
        {
            if (shipDeviceState == null)
            {
                HideReticle();
                if (equipmentHudText != null)
                {
                    equipmentHudText.enabled = false;
                    equipmentHudText.text = string.Empty;
                }

                return;
            }

            var equipment = shipDeviceState.CurrentEquipmentState;
            var activeSlot = equipment.ActiveHandSlot;
            if (handInventory != null)
            {
                handInventory.SyncActiveSlotIndex(equipment.ActiveHandSlotIndex);
            }

            DisableAlternateModeIfIncompatible(activeSlot);
            if (equipmentHudText != null)
            {
                equipmentHudText.enabled = true;
                equipmentHudText.text = BuildHudText(equipment, activeSlot);
            }

            RefreshReticle(equipment, activeSlot);
        }

        private string BuildHudText(PlayerEquipmentState equipment, EquipmentSlotState activeSlot)
        {
            var itemName = activeSlot.IsEmpty
                ? "Empty"
                : EquipmentRules.FormatItemName(activeSlot.ItemKind);
            var mode = GetDisplayMode(activeSlot);
            var cooldown = equipment.UseCooldownSeconds > 0f
                ? "Cooldown " + equipment.UseCooldownSeconds.ToString("0.0") + "s"
                : "Ready";
            var reload = !activeSlot.IsEmpty && EquipmentRules.GetDefinition(activeSlot.ItemKind).HasReloadInputSkeleton
                ? "\nReload: R skeleton, magazine pending confirmation"
                : string.Empty;
            return "Equipment\n"
                   + "Hand " + (equipment.ActiveHandSlotIndex + 1) + ": " + itemName + "\n"
                   + "Mode: " + mode + "\n"
                   + cooldown
                   + reload
                   + BuildEffectText(equipment);
        }

        private string GetDisplayMode(EquipmentSlotState activeSlot)
        {
            if (activeSlot.ItemKind == EquipmentItemKind.Stick && IsAlternateModeActive())
            {
                return "Throwing skeleton";
            }

            if (activeSlot.ItemKind == EquipmentItemKind.Musket && IsAlternateModeActive())
            {
                return "Precision Aim";
            }

            if (activeSlot.ItemKind == EquipmentItemKind.Dagger && IsAlternateModeActive())
            {
                return "Throwing";
            }

            return "Primary";
        }

        private static string BuildEffectText(PlayerEquipmentState equipment)
        {
            var text = string.Empty;
            if (equipment.ActiveDamageReductionPercent > 0)
            {
                text += "\nProtection: " +
                        EquipmentRules.FormatItemName(equipment.ActiveProtectiveItemKind) +
                        " +" +
                        equipment.ActiveDamageReductionPercent +
                        "%";
            }

            if (equipment.HasActiveStrengthEnhancer)
            {
                text += "\nStrength: +" +
                        equipment.StrengthDamageBonusPercent +
                        "% " +
                        equipment.StrengthEnhancerRemainingSeconds.ToString("0") +
                        "s";
            }

            if (equipment.HasActiveFlashlight)
            {
                text += "\nFlashlight: " +
                        equipment.FlashlightRemainingSeconds.ToString("0") +
                        "s";
            }

            return text;
        }

        private void RefreshReticle(PlayerEquipmentState equipment, EquipmentSlotState activeSlot)
        {
            if (precisionAimReticleText == null)
            {
                return;
            }

            var showReticle = activeSlot.ItemKind == EquipmentItemKind.Musket && IsAlternateModeActive();
            precisionAimReticleText.enabled = showReticle;
            precisionAimReticleText.text = showReticle ? "+" : string.Empty;
        }

        private void HideReticle()
        {
            if (precisionAimReticleText == null)
            {
                return;
            }

            precisionAimReticleText.enabled = false;
            precisionAimReticleText.text = string.Empty;
        }

        public void ToggleAlternateModeForValidation()
        {
            ToggleAlternateMode();
        }

        private void ToggleAlternateMode()
        {
            if (shipDeviceState == null)
            {
                alternateModeActive = false;
                alternateModeItemKind = EquipmentItemKind.None;
                HideReticle();
                return;
            }

            var activeSlot = shipDeviceState.CurrentEquipmentState.ActiveHandSlot;
            if (!CanUseAlternateMode(activeSlot))
            {
                SetAlternateMode(false, EquipmentItemKind.None);
                RefreshHud();
                return;
            }

            var shouldEnable = !alternateModeActive || alternateModeItemKind != activeSlot.ItemKind;
            SetAlternateMode(shouldEnable, shouldEnable ? activeSlot.ItemKind : EquipmentItemKind.None);
            RefreshHud();
        }

        private bool IsAlternateModeActive()
        {
            if (!alternateModeActive || shipDeviceState == null)
            {
                return false;
            }

            var activeSlot = shipDeviceState.CurrentEquipmentState.ActiveHandSlot;
            return activeSlot.ItemKind == alternateModeItemKind && CanUseAlternateMode(activeSlot);
        }

        private void DisableAlternateModeIfIncompatible(EquipmentSlotState activeSlot)
        {
            if (!alternateModeActive)
            {
                return;
            }

            if (activeSlot.ItemKind == alternateModeItemKind && CanUseAlternateMode(activeSlot))
            {
                return;
            }

            SetAlternateMode(false, EquipmentItemKind.None);
        }

        private void SetAlternateMode(bool active, EquipmentItemKind itemKind)
        {
            alternateModeActive = active;
            alternateModeItemKind = active ? itemKind : EquipmentItemKind.None;

            if (shipDeviceState == null)
            {
                return;
            }

            var equipment = shipDeviceState.CurrentEquipmentState;
            var mode = active ? GetAlternateModeForItem(itemKind) : EquipmentUseMode.Primary;
            var summary = active
                ? EquipmentRules.FormatItemName(itemKind) + " alternate mode enabled."
                : "Equipment alternate mode disabled.";
            shipDeviceState.SetEquipmentState(equipment.WithModeAndSummary(mode, summary));
        }

        private static bool CanUseAlternateMode(EquipmentSlotState activeSlot)
        {
            if (activeSlot.IsEmpty)
            {
                return false;
            }

            var definition = EquipmentRules.GetDefinition(activeSlot.ItemKind);
            return definition.HasThrowMode || definition.HasPrecisionAimMode;
        }

        private static EquipmentUseMode GetAlternateModeForItem(EquipmentItemKind itemKind)
        {
            switch (itemKind)
            {
                case EquipmentItemKind.Stick:
                case EquipmentItemKind.Dagger:
                    return EquipmentUseMode.Throwing;
                case EquipmentItemKind.Musket:
                    return EquipmentUseMode.PrecisionAim;
                default:
                    return EquipmentUseMode.Primary;
            }
        }
    }
}
