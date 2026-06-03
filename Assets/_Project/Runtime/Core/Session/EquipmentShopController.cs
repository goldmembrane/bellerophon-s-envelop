using System;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class EquipmentShopController : MonoBehaviour
    {
        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private PlayerEquipmentController equipmentController;
        [SerializeField] private GameObject shopRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Button buyStickButton;
        [SerializeField] private Button buyMusketButton;
        [SerializeField] private Button closeButton;

        private EquipmentShopSection activeSection = EquipmentShopSection.Buy;
        private string lastStatus = string.Empty;

        public GameObject ShopRoot => shopRoot;

        public Text BodyText => bodyText;

        public Text StatusText => statusText;

        public Button BuyTabButton => buyTabButton;

        public Button SellTabButton => sellTabButton;

        public Button BuyStickButton => buyStickButton;

        public Button BuyMusketButton => buyMusketButton;

        public Button CloseButton => closeButton;

        public bool IsShopVisible => shopRoot != null && shopRoot.activeSelf;

        public void Configure(
            NewGameStartFlowController startController,
            PlanetMaintenanceController maintenance,
            ShipDeviceInteractionState deviceState,
            PlayerEquipmentController playerEquipmentController,
            GameObject root,
            Text titleLabel,
            Text bodyLabel,
            Text statusLabel,
            Button buyTab,
            Button sellTab,
            Button buyStick,
            Button buyMusket,
            Button close)
        {
            startFlowController = startController;
            maintenanceController = maintenance;
            shipDeviceState = deviceState;
            equipmentController = playerEquipmentController;
            shopRoot = root;
            titleText = titleLabel;
            bodyText = bodyLabel;
            statusText = statusLabel;
            buyTabButton = buyTab;
            sellTabButton = sellTab;
            buyStickButton = buyStick;
            buyMusketButton = buyMusket;
            closeButton = close;
            DisableTextRaycasts();
            BindButtons();
            HideShop();
        }

        public void ShowBuyTab()
        {
            activeSection = EquipmentShopSection.Buy;
            ShowShop();
        }

        public void ShowSellTab()
        {
            activeSection = EquipmentShopSection.Sell;
            ShowShop();
        }

        public void ShowShop()
        {
            if (shopRoot == null)
            {
                return;
            }

            shopRoot.SetActive(true);
            DisableTextRaycasts();
            RefreshShop();
        }

        public void HideShop()
        {
            if (shopRoot != null)
            {
                shopRoot.SetActive(false);
            }
        }

        public void BuyStick()
        {
            BuyItem(EquipmentItemKind.Stick);
        }

        public void BuyMusket()
        {
            BuyItem(EquipmentItemKind.Musket);
        }

        public void RefreshShop()
        {
            DisableTextRaycasts();
            if (titleText != null)
            {
                titleText.text = "Equipment Shop";
            }

            if (bodyText != null)
            {
                bodyText.text = activeSection == EquipmentShopSection.Buy
                    ? BuildBuyText()
                    : BuildSellText();
            }

            var session = startFlowController != null ? startFlowController.CurrentSession : null;
            var equipment = session != null ? session.Equipment : PlayerEquipmentState.Empty;
            SetButtonState(buyStickButton, activeSection == EquipmentShopSection.Buy && !equipment.HasAnyItem(EquipmentItemKind.Stick));
            SetButtonState(buyMusketButton, activeSection == EquipmentShopSection.Buy && !equipment.HasAnyItem(EquipmentItemKind.Musket));

            if (buyTabButton != null)
            {
                buyTabButton.interactable = activeSection != EquipmentShopSection.Buy;
            }

            if (sellTabButton != null)
            {
                sellTabButton.interactable = activeSection != EquipmentShopSection.Sell;
            }

            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(lastStatus)
                    ? "Buy tab has Phase 15 weapon skeletons. Sell tab is data-only."
                    : lastStatus;
            }
        }

        private void Awake()
        {
            BindButtons();
            HideShop();
        }

        private void Update()
        {
            ProcessPointerClickFallback();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void BindButtons()
        {
            UnbindButtons();
            if (maintenanceController != null && maintenanceController.ShopButton != null)
            {
                maintenanceController.ShopButton.onClick.AddListener(ShowBuyTab);
            }

            if (buyTabButton != null)
            {
                buyTabButton.onClick.AddListener(ShowBuyTab);
            }

            if (sellTabButton != null)
            {
                sellTabButton.onClick.AddListener(ShowSellTab);
            }

            if (buyStickButton != null)
            {
                buyStickButton.onClick.AddListener(BuyStick);
            }

            if (buyMusketButton != null)
            {
                buyMusketButton.onClick.AddListener(BuyMusket);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideShop);
            }
        }

        private void UnbindButtons()
        {
            if (maintenanceController != null && maintenanceController.ShopButton != null)
            {
                maintenanceController.ShopButton.onClick.RemoveListener(ShowBuyTab);
            }

            if (buyTabButton != null)
            {
                buyTabButton.onClick.RemoveListener(ShowBuyTab);
            }

            if (sellTabButton != null)
            {
                sellTabButton.onClick.RemoveListener(ShowSellTab);
            }

            if (buyStickButton != null)
            {
                buyStickButton.onClick.RemoveListener(BuyStick);
            }

            if (buyMusketButton != null)
            {
                buyMusketButton.onClick.RemoveListener(BuyMusket);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HideShop);
            }
        }

        private void BuyItem(EquipmentItemKind itemKind)
        {
            var session = startFlowController != null ? startFlowController.CurrentSession : null;
            if (session == null)
            {
                lastStatus = "No active session is available for shop purchase.";
                RefreshShop();
                return;
            }

            var purchasePreview = EquipmentRules.PurchaseItem(session.Equipment, itemKind);
            if (!purchasePreview.Purchased)
            {
                lastStatus = purchasePreview.Summary;
                startFlowController.ApplySessionState(session.WithEquipment(purchasePreview.State));
                SyncEquipmentState(purchasePreview.State);
                RefreshShop();
                return;
            }

            if (session.Wallet.Credits < purchasePreview.SpentCredits)
            {
                lastStatus = "Insufficient credits for " + EquipmentRules.FormatItemName(itemKind) + ".";
                RefreshShop();
                return;
            }

            var purchasedSession = session.PurchaseEquipment(itemKind);
            startFlowController.ApplySessionState(purchasedSession);
            SyncEquipmentState(purchasedSession.Equipment);
            lastStatus = purchasePreview.Summary + " Spent $" + purchasePreview.SpentCredits + ".";
            if (maintenanceController != null)
            {
                maintenanceController.RefreshMaintenance();
            }

            RefreshShop();
        }

        private void SyncEquipmentState(PlayerEquipmentState equipment)
        {
            if (shipDeviceState != null)
            {
                shipDeviceState.SetEquipmentState(equipment);
            }
        }

        private string BuildBuyText()
        {
            var session = startFlowController != null ? startFlowController.CurrentSession : null;
            var wallet = session != null ? session.Wallet.Credits : 0;
            var equipment = session != null ? session.Equipment : PlayerEquipmentState.Empty;
            var builder = new StringBuilder();
            builder.AppendLine("Buy");
            builder.AppendLine("Wallet: " + FormatMoney(wallet));
            AppendEquipmentSummary(builder, equipment);
            builder.AppendLine();
            foreach (var entry in EquipmentRules.CreatePhase15BuyCatalog())
            {
                builder.AppendLine(FormatCatalogEntry(entry));
            }

            builder.AppendLine();
            builder.Append("Musket magazine size and reload time are pending confirmation; R only records the reload skeleton.");
            return builder.ToString();
        }

        private static string BuildSellText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Sell");
            foreach (var entry in EquipmentRules.CreatePhase15SellCatalog())
            {
                builder.AppendLine(FormatCatalogEntry(entry));
            }

            builder.AppendLine();
            builder.Append("Personal cargo sale logic is data-only and intentionally not active in Phase 15.");
            return builder.ToString();
        }

        private static void AppendEquipmentSummary(StringBuilder builder, PlayerEquipmentState equipment)
        {
            builder.AppendLine("Suit: " + (equipment.HasBasicProtectiveSuit ? "Basic Protective Suit" : "None"));
            for (var i = 0; i < PlayerEquipmentState.DefaultHandSlotCount; i++)
            {
                var slot = equipment.GetHandSlot(i);
                builder.AppendLine("Hand " + (i + 1) + ": " + FormatSlot(slot));
            }
        }

        private static string FormatCatalogEntry(EquipmentShopCatalogEntry entry)
        {
            var state = entry.FunctionalInPhase15 ? "Phase 15 active" : "Data only";
            return entry.Category + " | " +
                   entry.DisplayName +
                   " | " +
                   (entry.PriceCredits > 0 ? FormatMoney(entry.PriceCredits) : "-") +
                   " | " +
                   state;
        }

        private static string FormatSlot(EquipmentSlotState slot)
        {
            return slot.IsEmpty ? "Empty" : EquipmentRules.FormatItemName(slot.ItemKind);
        }

        private static void SetButtonState(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(active);
            button.interactable = active;
        }

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }

        private void ProcessPointerClickFallback()
        {
            if (!IsShopVisible ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickButtonAtScreenPosition(closeButton, pointerPosition, HideShop) ||
                TryClickButtonAtScreenPosition(buyTabButton, pointerPosition, ShowBuyTab) ||
                TryClickButtonAtScreenPosition(sellTabButton, pointerPosition, ShowSellTab) ||
                TryClickButtonAtScreenPosition(buyStickButton, pointerPosition, BuyStick) ||
                TryClickButtonAtScreenPosition(buyMusketButton, pointerPosition, BuyMusket))
            {
                return;
            }
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

        private void DisableTextRaycasts()
        {
            SetTextNonBlocking(titleText);
            SetTextNonBlocking(bodyText);
            SetTextNonBlocking(statusText);
        }

        private static void SetTextNonBlocking(Text text)
        {
            if (text != null)
            {
                text.raycastTarget = false;
            }
        }
    }
}
