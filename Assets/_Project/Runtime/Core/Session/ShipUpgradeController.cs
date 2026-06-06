using System;
using System.Text;
using Bellerophon.Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class ShipUpgradeController : MonoBehaviour
    {
        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private GameObject upgradeRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] purchaseButtons;
        [SerializeField] private Button[] equipButtons;
        [SerializeField] private Button closeButton;

        private static readonly ShipUpgradeCategory[] CategoryOrder =
        {
            ShipUpgradeCategory.Durability,
            ShipUpgradeCategory.WeaponSystems,
            ShipUpgradeCategory.AutoPilot,
            ShipUpgradeCategory.SupplySlots,
            ShipUpgradeCategory.InternalControl
        };

        private string lastStatus = string.Empty;

        public GameObject UpgradeRoot => upgradeRoot;

        public Text BodyText => bodyText;

        public Text StatusText => statusText;

        public Button[] PurchaseButtons => purchaseButtons == null ? new Button[0] : (Button[])purchaseButtons.Clone();

        public Button[] EquipButtons => equipButtons == null ? new Button[0] : (Button[])equipButtons.Clone();

        public Button CloseButton => closeButton;

        public bool IsUpgradeVisible => upgradeRoot != null && upgradeRoot.activeSelf;

        public void Configure(
            NewGameStartFlowController startController,
            PlanetMaintenanceController maintenance,
            FirstPersonPlayerInput firstPersonInput,
            GameObject root,
            Text titleLabel,
            Text bodyLabel,
            Text statusLabel,
            Button[] purchaseActionButtons,
            Button[] equipActionButtons,
            Button closeActionButton)
        {
            startFlowController = startController;
            maintenanceController = maintenance;
            playerInput = firstPersonInput;
            upgradeRoot = root;
            titleText = titleLabel;
            bodyText = bodyLabel;
            statusText = statusLabel;
            purchaseButtons = CloneButtons(purchaseActionButtons);
            equipButtons = CloneButtons(equipActionButtons);
            closeButton = closeActionButton;
            DisableTextRaycasts();
            BindButtons();
            HideUpgrades();
        }

        public void ShowUpgrades()
        {
            if (upgradeRoot == null)
            {
                return;
            }

            if (maintenanceController != null)
            {
                maintenanceController.HideMaintenance();
            }

            upgradeRoot.SetActive(true);
            SetCursorLockSuppressed(true);
            DisableTextRaycasts();
            RefreshUpgrades();
        }

        public void HideUpgrades()
        {
            if (upgradeRoot != null)
            {
                upgradeRoot.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        public void ReturnToMaintenance()
        {
            HideUpgrades();
            if (maintenanceController != null)
            {
                maintenanceController.ShowMaintenance();
            }
        }

        public void RefreshUpgrades()
        {
            DisableTextRaycasts();
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "Ship Upgrades";
            }

            if (bodyText != null)
            {
                bodyText.text = BuildBodyText(session);
            }

            RefreshButtonStates(session);

            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(lastStatus)
                    ? "Durability purchases apply immediately. Other purchased tiers must be equipped to change the active loadout."
                    : lastStatus;
            }
        }

        public void PurchaseDurability()
        {
            PurchaseUpgrade(ShipUpgradeCategory.Durability);
        }

        public void PurchaseWeaponSystems()
        {
            PurchaseUpgrade(ShipUpgradeCategory.WeaponSystems);
        }

        public void PurchaseAutoPilot()
        {
            PurchaseUpgrade(ShipUpgradeCategory.AutoPilot);
        }

        public void PurchaseSupplySlots()
        {
            PurchaseUpgrade(ShipUpgradeCategory.SupplySlots);
        }

        public void PurchaseInternalControl()
        {
            PurchaseUpgrade(ShipUpgradeCategory.InternalControl);
        }

        public void EquipDurability()
        {
            EquipUpgrade(ShipUpgradeCategory.Durability);
        }

        public void EquipWeaponSystems()
        {
            EquipUpgrade(ShipUpgradeCategory.WeaponSystems);
        }

        public void EquipAutoPilot()
        {
            EquipUpgrade(ShipUpgradeCategory.AutoPilot);
        }

        public void EquipSupplySlots()
        {
            EquipUpgrade(ShipUpgradeCategory.SupplySlots);
        }

        public void EquipInternalControl()
        {
            EquipUpgrade(ShipUpgradeCategory.InternalControl);
        }

        private void Awake()
        {
            BindButtons();
            HideUpgrades();
        }

        private void Update()
        {
            ProcessPointerClickFallback();
        }

        private void OnDisable()
        {
            SetCursorLockSuppressed(false);
        }

        private void OnDestroy()
        {
            UnbindButtons();
            SetCursorLockSuppressed(false);
        }

        private GameSessionState CurrentSession => startFlowController != null
            ? startFlowController.CurrentSession
            : null;

        private void PurchaseUpgrade(ShipUpgradeCategory category)
        {
            var session = CurrentSession;
            if (session == null)
            {
                lastStatus = "No active session is available for ship upgrades.";
                RefreshUpgrades();
                return;
            }

            var result = session.PurchaseShipUpgrade(category);
            if (result.Purchased)
            {
                startFlowController.ApplySessionState(result.State);
                if (maintenanceController != null)
                {
                    maintenanceController.RefreshMaintenance();
                }
            }

            lastStatus = result.Summary;
            RefreshUpgrades();
        }

        private void EquipUpgrade(ShipUpgradeCategory category)
        {
            var session = CurrentSession;
            if (session == null)
            {
                lastStatus = "No active session is available for ship upgrades.";
                RefreshUpgrades();
                return;
            }

            var result = session.EquipShipUpgrade(category);
            if (result.Equipped)
            {
                startFlowController.ApplySessionState(result.State);
                if (maintenanceController != null)
                {
                    maintenanceController.RefreshMaintenance();
                }
            }

            lastStatus = result.Summary;
            RefreshUpgrades();
        }

        private void RefreshButtonStates(GameSessionState session)
        {
            for (var i = 0; i < CategoryOrder.Length; i++)
            {
                var category = CategoryOrder[i];
                var purchaseButton = GetButton(purchaseButtons, i);
                if (purchaseButton != null)
                {
                    purchaseButton.gameObject.SetActive(true);
                    purchaseButton.interactable = ShipUpgradeRules.CanPurchaseNextTier(
                        session.ShipUpgrades,
                        category,
                        session.Wallet.Credits);
                }

                var equipButton = GetButton(equipButtons, i);
                if (equipButton != null)
                {
                    var isAutoAppliedCategory = category == ShipUpgradeCategory.Durability;
                    equipButton.gameObject.SetActive(!isAutoAppliedCategory);
                    equipButton.interactable = !isAutoAppliedCategory &&
                                               ShipUpgradeRules.CanEquipPurchasedTier(session.ShipUpgrades, category);
                }
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.interactable = true;
            }
        }

        private static string BuildBodyText(GameSessionState session)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Wallet: " + FormatMoney(session.Wallet.Credits));
            builder.AppendLine("Pending repair: " + FormatMoney(session.SettlementResult.PendingRepairCost));
            builder.AppendLine("Towing incidents: " + session.TowingIncidentCount);
            builder.AppendLine();
            builder.AppendLine("Category | Purchased | Equipped | Next cost | Equipped effect");

            var upgrades = session.ShipUpgrades;
            for (var i = 0; i < CategoryOrder.Length; i++)
            {
                var category = CategoryOrder[i];
                var purchasedTier = upgrades.GetPurchasedTier(category);
                var equippedTier = upgrades.GetEquippedTier(category);
                var nextCost = ShipUpgradeRules.GetNextPurchaseCost(upgrades, category);
                builder.AppendLine(
                    ShipUpgradeRules.FormatCategoryName(category) +
                    " | T" + purchasedTier +
                    " | T" + equippedTier +
                    " | " + (nextCost > 0 ? FormatMoney(nextCost) : "Max") +
                    " | " + ShipUpgradeRules.FormatEffectSummary(category, equippedTier));
            }

            builder.AppendLine();
            builder.AppendLine("Appearance slots");
            builder.AppendLine("Hull paint: " + upgrades.Appearance.HullPaintSlotId);
            builder.AppendLine("Emblem: " + upgrades.Appearance.EmblemSlotId);
            builder.Append("Nameplate: " + upgrades.Appearance.NameplateSlotId);
            return builder.ToString();
        }

        private void BindButtons()
        {
            UnbindButtons();
            BindCategoryButton(purchaseButtons, 0, PurchaseDurability);
            BindCategoryButton(purchaseButtons, 1, PurchaseWeaponSystems);
            BindCategoryButton(purchaseButtons, 2, PurchaseAutoPilot);
            BindCategoryButton(purchaseButtons, 3, PurchaseSupplySlots);
            BindCategoryButton(purchaseButtons, 4, PurchaseInternalControl);
            BindCategoryButton(equipButtons, 0, EquipDurability);
            BindCategoryButton(equipButtons, 1, EquipWeaponSystems);
            BindCategoryButton(equipButtons, 2, EquipAutoPilot);
            BindCategoryButton(equipButtons, 3, EquipSupplySlots);
            BindCategoryButton(equipButtons, 4, EquipInternalControl);

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ReturnToMaintenance);
            }
        }

        private void UnbindButtons()
        {
            UnbindCategoryButton(purchaseButtons, 0, PurchaseDurability);
            UnbindCategoryButton(purchaseButtons, 1, PurchaseWeaponSystems);
            UnbindCategoryButton(purchaseButtons, 2, PurchaseAutoPilot);
            UnbindCategoryButton(purchaseButtons, 3, PurchaseSupplySlots);
            UnbindCategoryButton(purchaseButtons, 4, PurchaseInternalControl);
            UnbindCategoryButton(equipButtons, 0, EquipDurability);
            UnbindCategoryButton(equipButtons, 1, EquipWeaponSystems);
            UnbindCategoryButton(equipButtons, 2, EquipAutoPilot);
            UnbindCategoryButton(equipButtons, 3, EquipSupplySlots);
            UnbindCategoryButton(equipButtons, 4, EquipInternalControl);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ReturnToMaintenance);
            }
        }

        private static void BindCategoryButton(Button[] buttons, int index, UnityEngine.Events.UnityAction action)
        {
            var button = GetButton(buttons, index);
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void UnbindCategoryButton(Button[] buttons, int index, UnityEngine.Events.UnityAction action)
        {
            var button = GetButton(buttons, index);
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private void ProcessPointerClickFallback()
        {
            if (!IsUpgradeVisible ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickButtonAtScreenPosition(closeButton, pointerPosition, ReturnToMaintenance) ||
                TryClickButtonAtScreenPosition(GetButton(purchaseButtons, 0), pointerPosition, PurchaseDurability) ||
                TryClickButtonAtScreenPosition(GetButton(purchaseButtons, 1), pointerPosition, PurchaseWeaponSystems) ||
                TryClickButtonAtScreenPosition(GetButton(purchaseButtons, 2), pointerPosition, PurchaseAutoPilot) ||
                TryClickButtonAtScreenPosition(GetButton(purchaseButtons, 3), pointerPosition, PurchaseSupplySlots) ||
                TryClickButtonAtScreenPosition(GetButton(purchaseButtons, 4), pointerPosition, PurchaseInternalControl) ||
                TryClickButtonAtScreenPosition(GetButton(equipButtons, 0), pointerPosition, EquipDurability) ||
                TryClickButtonAtScreenPosition(GetButton(equipButtons, 1), pointerPosition, EquipWeaponSystems) ||
                TryClickButtonAtScreenPosition(GetButton(equipButtons, 2), pointerPosition, EquipAutoPilot) ||
                TryClickButtonAtScreenPosition(GetButton(equipButtons, 3), pointerPosition, EquipSupplySlots) ||
                TryClickButtonAtScreenPosition(GetButton(equipButtons, 4), pointerPosition, EquipInternalControl))
            {
                return;
            }
        }

        private void SetCursorLockSuppressed(bool suppressed)
        {
            if (playerInput == null)
            {
                playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }

            if (playerInput != null)
            {
                playerInput.SetCursorLockSuppressed(suppressed);
            }
        }

        private void DisableTextRaycasts()
        {
            SetTextNonBlocking(titleText);
            SetTextNonBlocking(bodyText);
            SetTextNonBlocking(statusText);
        }

        private static bool TryClickButtonAtScreenPosition(Button button, Vector2 screenPosition, Action action)
        {
            if (button == null ||
                action == null ||
                !button.gameObject.activeInHierarchy ||
                !button.interactable)
            {
                return false;
            }

            var rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null))
            {
                return false;
            }

            action();
            return true;
        }

        private static Button GetButton(Button[] buttons, int index)
        {
            return buttons == null || index < 0 || index >= buttons.Length ? null : buttons[index];
        }

        private static Button[] CloneButtons(Button[] buttons)
        {
            return buttons == null ? new Button[0] : (Button[])buttons.Clone();
        }

        private static void SetTextNonBlocking(Text text)
        {
            if (text != null)
            {
                text.raycastTarget = false;
            }
        }

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }
    }
}
