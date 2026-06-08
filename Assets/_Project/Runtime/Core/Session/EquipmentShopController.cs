using System;
using System.Collections.Generic;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class EquipmentShopController : MonoBehaviour
    {
        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private PlanetStayController planetStayController;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private PlayerEquipmentController equipmentController;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private GameObject shopRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Button buyStickButton;
        [SerializeField] private Button buyMusketButton;
        [SerializeField] private Button buyShotgunButton;
        [SerializeField] private Button buyFlashlightButton;
        [SerializeField] private Button buyInjuryRelieverButton;
        [SerializeField] private Button buyProtectiveSuitButton;
        [SerializeField] private Button buyStrengthEnhancerButton;
        [SerializeField] private Button[] sellItemButtons;
        [SerializeField] private Button sellSelectedItemButton;
        [SerializeField] private Button disposePurchasedItemButton;
        [SerializeField] private Button sellPersonalCargoButton;
        [SerializeField] private Button closeButton;

        private const int MaxSellItemButtons = 8;

        private enum SellItemSource
        {
            None,
            PurchasedHand,
            PurchasedSupply,
            PersonalCargo
        }

        private readonly struct SellCandidate
        {
            public SellCandidate(
                SellItemSource source,
                int index,
                string displayName,
                int saleCredits,
                string detail)
            {
                Source = source;
                Index = index;
                DisplayName = displayName ?? string.Empty;
                SaleCredits = Math.Max(0, saleCredits);
                Detail = detail ?? string.Empty;
            }

            public SellItemSource Source { get; }

            public int Index { get; }

            public string DisplayName { get; }

            public int SaleCredits { get; }

            public string Detail { get; }

            public bool IsValid => Source != SellItemSource.None && Index >= 0;
        }

        private readonly UnityAction[] sellItemButtonActions = new UnityAction[MaxSellItemButtons];
        private EquipmentShopSection activeSection = EquipmentShopSection.Buy;
        private SellItemSource selectedSellSource = SellItemSource.None;
        private int selectedSellIndex = -1;
        private string lastStatus = string.Empty;
        private bool returnToPlanet;

        public GameObject ShopRoot => shopRoot;

        public Text BodyText => bodyText;

        public Text StatusText => statusText;

        public Button BuyTabButton => buyTabButton;

        public Button SellTabButton => sellTabButton;

        public Button BuyStickButton => buyStickButton;

        public Button BuyMusketButton => buyMusketButton;

        public Button BuyShotgunButton => buyShotgunButton;

        public Button BuyFlashlightButton => buyFlashlightButton;

        public Button BuyInjuryRelieverButton => buyInjuryRelieverButton;

        public Button BuyProtectiveSuitButton => buyProtectiveSuitButton;

        public Button BuyStrengthEnhancerButton => buyStrengthEnhancerButton;

        public Button[] SellItemButtons => sellItemButtons == null ? new Button[0] : (Button[])sellItemButtons.Clone();

        public Button SellSelectedItemButton => sellSelectedItemButton ?? disposePurchasedItemButton;

        public Button DisposePurchasedItemButton => SellSelectedItemButton;

        public Button SellPersonalCargoButton => sellPersonalCargoButton;

        public Button CloseButton => closeButton;

        public PlanetMaintenanceController MaintenanceController => maintenanceController;

        public PlanetStayController PlanetStayController => planetStayController;

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
            Button close,
            Button sellPersonalCargo = null,
            Button buyFlashlight = null,
            Button buyInjuryReliever = null,
            Button disposePurchasedItem = null,
            Button sellSelectedItem = null,
            Button[] sellRows = null,
            Button buyShotgun = null,
            Button buyProtectiveSuit = null,
            Button buyStrengthEnhancer = null)
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
            buyShotgunButton = buyShotgun;
            buyFlashlightButton = buyFlashlight;
            buyInjuryRelieverButton = buyInjuryReliever;
            buyProtectiveSuitButton = buyProtectiveSuit;
            buyStrengthEnhancerButton = buyStrengthEnhancer;
            disposePurchasedItemButton = disposePurchasedItem;
            sellSelectedItemButton = sellSelectedItem ?? disposePurchasedItem;
            sellItemButtons = sellRows == null ? new Button[0] : (Button[])sellRows.Clone();
            sellPersonalCargoButton = sellPersonalCargo;
            closeButton = close;
            DisableTextRaycasts();
            BindButtons();
            HideShop();
        }

        public void ConfigurePlanetStay(PlanetStayController planetController)
        {
            planetStayController = planetController;
        }

        public void ConfigureMaintenance(PlanetMaintenanceController maintenance)
        {
            if (maintenanceController != null && maintenanceController.ShopButton != null)
            {
                maintenanceController.ShopButton.onClick.RemoveListener(ShowBuyTab);
            }

            maintenanceController = maintenance;
            if (maintenanceController != null && maintenanceController.ShopButton != null)
            {
                maintenanceController.ShopButton.onClick.AddListener(ShowBuyTab);
            }
        }

        public void ShowBuyTab()
        {
            activeSection = EquipmentShopSection.Buy;
            ClearSellSelection();
            ShowShop(returnToPlanet);
        }

        public void ShowBuyTabFromPlanet()
        {
            activeSection = EquipmentShopSection.Buy;
            ClearSellSelection();
            ShowShop(returnToPlanetAfterClose: true);
        }

        public void ShowSellTab()
        {
            activeSection = EquipmentShopSection.Sell;
            ClearSellSelection();
            ShowShop(returnToPlanet);
        }

        public void ShowShop()
        {
            ShowShop(returnToPlanetAfterClose: false);
        }

        private void ShowShop(bool returnToPlanetAfterClose)
        {
            if (shopRoot == null)
            {
                return;
            }

            returnToPlanet = returnToPlanetAfterClose;
            shopRoot.SetActive(true);
            SetCursorLockSuppressed(true);
            DisableTextRaycasts();
            RefreshShop();
        }

        public void HideShop()
        {
            if (shopRoot != null)
            {
                shopRoot.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        public void CloseShop()
        {
            if (shopRoot != null)
            {
                shopRoot.SetActive(false);
            }

            if (returnToPlanet && planetStayController != null)
            {
                SetCursorLockSuppressed(false);
                returnToPlanet = false;
                planetStayController.ShowPlanet();
                return;
            }

            SetCursorLockSuppressed(maintenanceController != null && maintenanceController.IsMaintenanceVisible);
            returnToPlanet = false;
        }

        public void BuyStick()
        {
            BuyItem(EquipmentItemKind.Stick);
        }

        public void BuyMusket()
        {
            BuyItem(EquipmentItemKind.Musket);
        }

        public void BuyShotgun()
        {
            BuyItem(EquipmentItemKind.Shotgun);
        }

        public void BuyFlashlight()
        {
            BuyItem(EquipmentItemKind.Flashlight);
        }

        public void BuyInjuryReliever()
        {
            BuyItem(EquipmentItemKind.InjuryReliever);
        }

        public void BuyProtectiveSuit()
        {
            BuyItem(EquipmentItemKind.ProtectiveSuit);
        }

        public void BuyStrengthEnhancer()
        {
            BuyItem(EquipmentItemKind.StrengthEnhancer);
        }

        public void SelectSellItem(int rowIndex)
        {
            if (rowIndex < 0)
            {
                ClearSellSelection();
                RefreshShop();
                return;
            }

            var session = startFlowController != null ? startFlowController.CurrentSession : null;
            var candidates = BuildSellCandidates(session);
            if (rowIndex >= candidates.Count)
            {
                ClearSellSelection();
                lastStatus = "No sell item is available for that row.";
                RefreshShop();
                return;
            }

            var candidate = candidates[rowIndex];
            selectedSellSource = candidate.Source;
            selectedSellIndex = candidate.Index;
            lastStatus = "Selected " + candidate.DisplayName + " for " + FormatMoney(candidate.SaleCredits) + ".";
            RefreshShop();
        }

        public void SellSelectedItem()
        {
            var session = startFlowController != null ? startFlowController.CurrentSession : null;
            if (session == null)
            {
                lastStatus = "No active session is available for selling.";
                RefreshShop();
                return;
            }

            var selected = FindSelectedSellCandidate(session);
            if (!selected.IsValid)
            {
                ClearSellSelection();
                lastStatus = "Select an item from the sell list before pressing Sell.";
                RefreshShop();
                return;
            }

            switch (selected.Source)
            {
                case SellItemSource.PurchasedHand:
                    SellEquipmentDisposal(session.DisposePurchasedHandEquipment(selected.Index));
                    break;
                case SellItemSource.PurchasedSupply:
                    SellEquipmentDisposal(session.DisposePurchasedSupplyEquipment(selected.Index));
                    break;
                case SellItemSource.PersonalCargo:
                    SellPersonalCargoSelection(session.SellPersonalCargo(selected.Index));
                    break;
                default:
                    lastStatus = "Select an item from the sell list before pressing Sell.";
                    break;
            }

            ClearSellSelection();
            RefreshShop();
        }

        public void DisposeFirstPurchasedItem()
        {
            SellSelectedItem();
        }

        public void SellFirstPersonalCargo()
        {
            SellSelectedItem();
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
            ValidateSellSelection(session);
            var sellCandidates = BuildSellCandidates(session);
            RefreshSellItemButtonStates(sellCandidates);
            SetButtonState(buyStickButton, activeSection == EquipmentShopSection.Buy && !equipment.HasAnyItem(EquipmentItemKind.Stick));
            SetButtonState(buyMusketButton, activeSection == EquipmentShopSection.Buy && !equipment.HasAnyItem(EquipmentItemKind.Musket));
            SetButtonState(buyShotgunButton, activeSection == EquipmentShopSection.Buy && !equipment.HasAnyItem(EquipmentItemKind.Shotgun));
            SetButtonState(buyFlashlightButton, activeSection == EquipmentShopSection.Buy);
            SetButtonState(buyInjuryRelieverButton, activeSection == EquipmentShopSection.Buy);
            SetButtonState(buyProtectiveSuitButton, activeSection == EquipmentShopSection.Buy && !equipment.HasAnyItem(EquipmentItemKind.ProtectiveSuit));
            SetButtonState(buyStrengthEnhancerButton, activeSection == EquipmentShopSection.Buy);
            SetButtonState(
                SellSelectedItemButton,
                activeSection == EquipmentShopSection.Sell &&
                FindSelectedSellCandidate(session).IsValid);
            SetButtonState(sellPersonalCargoButton, false);

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
                    ? GetDefaultStatusText()
                    : lastStatus;
            }
        }

        private string GetDefaultStatusText()
        {
            return activeSection == EquipmentShopSection.Sell
                ? "Select a sell list item, then press Sell Selected."
                : "Select an item to buy.";
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
            SetCursorLockSuppressed(false);
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

            if (buyShotgunButton != null)
            {
                buyShotgunButton.onClick.AddListener(BuyShotgun);
            }

            if (buyFlashlightButton != null)
            {
                buyFlashlightButton.onClick.AddListener(BuyFlashlight);
            }

            if (buyInjuryRelieverButton != null)
            {
                buyInjuryRelieverButton.onClick.AddListener(BuyInjuryReliever);
            }

            if (buyProtectiveSuitButton != null)
            {
                buyProtectiveSuitButton.onClick.AddListener(BuyProtectiveSuit);
            }

            if (buyStrengthEnhancerButton != null)
            {
                buyStrengthEnhancerButton.onClick.AddListener(BuyStrengthEnhancer);
            }

            if (disposePurchasedItemButton != null)
            {
                disposePurchasedItemButton.onClick.AddListener(SellSelectedItem);
            }

            if (sellSelectedItemButton != null && sellSelectedItemButton != disposePurchasedItemButton)
            {
                sellSelectedItemButton.onClick.AddListener(SellSelectedItem);
            }

            if (sellPersonalCargoButton != null)
            {
                sellPersonalCargoButton.onClick.AddListener(SellSelectedItem);
            }

            BindSellItemButtons();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
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

            if (buyShotgunButton != null)
            {
                buyShotgunButton.onClick.RemoveListener(BuyShotgun);
            }

            if (buyFlashlightButton != null)
            {
                buyFlashlightButton.onClick.RemoveListener(BuyFlashlight);
            }

            if (buyInjuryRelieverButton != null)
            {
                buyInjuryRelieverButton.onClick.RemoveListener(BuyInjuryReliever);
            }

            if (buyProtectiveSuitButton != null)
            {
                buyProtectiveSuitButton.onClick.RemoveListener(BuyProtectiveSuit);
            }

            if (buyStrengthEnhancerButton != null)
            {
                buyStrengthEnhancerButton.onClick.RemoveListener(BuyStrengthEnhancer);
            }

            if (disposePurchasedItemButton != null)
            {
                disposePurchasedItemButton.onClick.RemoveListener(SellSelectedItem);
            }

            if (sellSelectedItemButton != null && sellSelectedItemButton != disposePurchasedItemButton)
            {
                sellSelectedItemButton.onClick.RemoveListener(SellSelectedItem);
            }

            if (sellPersonalCargoButton != null)
            {
                sellPersonalCargoButton.onClick.RemoveListener(SellSelectedItem);
            }

            UnbindSellItemButtons();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HideShop);
                closeButton.onClick.RemoveListener(CloseShop);
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

            var purchasePreview = EquipmentRules.PurchaseItem(
                session.Equipment,
                itemKind,
                session.SpecialContracts.EquipmentUnlocks);
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
            AppendCatalogGroup(builder, "Common products", EquipmentAvailability.CommonShop);
            AppendCatalogGroup(builder, "Fame-limited products", EquipmentAvailability.FameRestrictedShop);
            AppendCatalogGroup(builder, "Special products", EquipmentAvailability.SpecialUnlock);

            builder.AppendLine();
            builder.Append("Step 8 first-pass effects are active for expanded weapons, protective gear, treatment, enhancement, and flashlight utility.");
            return builder.ToString();
        }

        private string BuildSellText()
        {
            var session = startFlowController != null ? startFlowController.CurrentSession : null;
            var builder = new StringBuilder();
            builder.AppendLine("Sell");
            foreach (var entry in EquipmentRules.CreatePhase15SellCatalog())
            {
                builder.AppendLine(FormatCatalogEntry(entry));
            }

            builder.AppendLine();
            if (session == null)
            {
                builder.Append("No active session.");
                return builder.ToString();
            }

            builder.AppendLine("Purchased item disposal");
            builder.AppendLine("Personal cargo sale");
            builder.AppendLine("Contract cargo: Not sellable");
            builder.AppendLine("Selected: " + FormatSelectedSellCandidate(session));
            builder.AppendLine();
            AppendSellCandidateList(builder, session);
            builder.AppendLine("Current planet trait: " + PersonalCargoRules.FormatTraitName(session.CurrentPlanetTrait));
            builder.Append("Use the numbered row buttons, then press Sell Selected.");
            return builder.ToString();
        }

        private static void AppendEquipmentSummary(StringBuilder builder, PlayerEquipmentState equipment)
        {
            builder.AppendLine("Suit: " + (equipment.HasBasicProtectiveSuit ? "Basic Protective Suit" : "None"));
            builder.AppendLine("Hand Slots: " + equipment.UnlockedHandSlotCount + "/" + PlayerEquipmentState.MaxHandSlotCount);
            for (var i = 0; i < equipment.UnlockedHandSlotCount; i++)
            {
                var slot = equipment.GetHandSlot(i);
                builder.AppendLine("Hand " + (i + 1) + ": " + FormatSlot(slot));
            }

            builder.AppendLine("Supply Slots: " + equipment.UnlockedSupplySlotCount + "/" + PlayerEquipmentState.MaxSupplySlotCount);
            for (var i = 0; i < equipment.UnlockedSupplySlotCount; i++)
            {
                var slot = equipment.GetSupplySlot(i);
                builder.AppendLine("Supply " + (i + 1) + ": " + FormatSlot(slot));
            }
        }

        private static string FormatCatalogEntry(EquipmentShopCatalogEntry entry)
        {
            var state = entry.FunctionalInPhase15 ? "Purchasable" : "Locked";
            return EquipmentRules.FormatAvailabilityName(entry.Availability) +
                   " | " +
                   EquipmentRules.FormatCategoryTabName(entry.Category) +
                   " | " +
                   entry.DisplayName +
                   " | " +
                   (entry.PriceCredits > 0 ? FormatMoney(entry.PriceCredits) : "-") +
                   " | " +
                   state;
        }

        private static string FormatSlot(EquipmentSlotState slot)
        {
            if (slot.IsEmpty)
            {
                return "Empty";
            }

            var label = EquipmentRules.FormatItemName(slot.ItemKind);
            if (slot.Count > 1)
            {
                label += " x" + slot.Count;
            }

            label += " | Durability " + slot.DurabilityPercent + "%";
            if (slot.PurchasePriceCredits > 0)
            {
                label += " | Paid " + FormatMoney(slot.PurchasePriceCredits);
            }

            return label;
        }

        private static void AppendCatalogGroup(
            StringBuilder builder,
            string title,
            EquipmentAvailability availability)
        {
            builder.AppendLine(title);
            var entries = EquipmentRules.FilterCatalogByAvailability(
                EquipmentRules.CreatePhase15BuyCatalog(),
                availability);
            for (var i = 0; i < entries.Length; i++)
            {
                builder.AppendLine(" - " + FormatCatalogEntry(entries[i]));
            }
        }

        private void AppendSellCandidateList(StringBuilder builder, GameSessionState session)
        {
            var candidates = BuildSellCandidates(session);
            if (candidates.Count == 0)
            {
                builder.AppendLine(" - Empty");
                return;
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var selected = IsSelected(candidate) ? "> " : "  ";
                builder.AppendLine(
                    selected +
                    "[" + (i + 1) + "] " +
                    candidate.DisplayName +
                    " | Sale " +
                    FormatMoney(candidate.SaleCredits) +
                    " | " +
                    candidate.Detail);
            }
        }

        private List<SellCandidate> BuildSellCandidates(GameSessionState session)
        {
            var candidates = new List<SellCandidate>();
            if (session == null)
            {
                return candidates;
            }

            var equipment = session.Equipment;
            for (var i = 0; i < equipment.UnlockedHandSlotCount; i++)
            {
                var slot = equipment.GetHandSlot(i);
                if (!slot.WasPurchased)
                {
                    continue;
                }

                candidates.Add(new SellCandidate(
                    SellItemSource.PurchasedHand,
                    i,
                    "Hand " + (i + 1) + " " + EquipmentRules.FormatItemName(slot.ItemKind),
                    EquipmentRules.CalculateDisposalCredits(slot),
                    FormatSlot(slot)));
            }

            for (var i = 0; i < equipment.UnlockedSupplySlotCount; i++)
            {
                var slot = equipment.GetSupplySlot(i);
                if (!slot.WasPurchased)
                {
                    continue;
                }

                candidates.Add(new SellCandidate(
                    SellItemSource.PurchasedSupply,
                    i,
                    "Supply " + (i + 1) + " " + EquipmentRules.FormatItemName(slot.ItemKind),
                    EquipmentRules.CalculateDisposalCredits(slot),
                    FormatSlot(slot)));
            }

            var items = session.PersonalCargoHold.Items;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var quote = PersonalCargoRules.CalculateSaleQuote(item, session.CurrentPlanetTrait);
                candidates.Add(new SellCandidate(
                    SellItemSource.PersonalCargo,
                    i,
                    "Personal Cargo " + (i + 1) + " " + item.DisplayName,
                    quote.SalePrice,
                    "Size " + item.SizeUnits +
                    " | Origin " + PersonalCargoRules.FormatTraitName(item.OriginTrait) +
                    " | Modifier " + FormatSignedPercent(quote.TraitModifierPercent) +
                    " | Durability " + Mathf.RoundToInt(item.DurabilityPercent * 100f) + "%"));
            }

            return candidates;
        }

        private SellCandidate FindSelectedSellCandidate(GameSessionState session)
        {
            var candidates = BuildSellCandidates(session);
            for (var i = 0; i < candidates.Count; i++)
            {
                if (IsSelected(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return default;
        }

        private string FormatSelectedSellCandidate(GameSessionState session)
        {
            var selected = FindSelectedSellCandidate(session);
            return selected.IsValid
                ? selected.DisplayName + " for " + FormatMoney(selected.SaleCredits)
                : "None";
        }

        private void ValidateSellSelection(GameSessionState session)
        {
            if (selectedSellSource == SellItemSource.None)
            {
                return;
            }

            if (!FindSelectedSellCandidate(session).IsValid)
            {
                ClearSellSelection();
            }
        }

        private bool IsSelected(SellCandidate candidate)
        {
            return candidate.Source == selectedSellSource && candidate.Index == selectedSellIndex;
        }

        private void ClearSellSelection()
        {
            selectedSellSource = SellItemSource.None;
            selectedSellIndex = -1;
        }

        private void SellEquipmentDisposal(EquipmentDisposalSessionResult disposal)
        {
            if (disposal.Disposed)
            {
                startFlowController.ApplySessionState(disposal.State);
                SyncEquipmentState(disposal.State.Equipment);
                if (maintenanceController != null)
                {
                    maintenanceController.RefreshMaintenance();
                }
            }

            lastStatus = disposal.Summary;
        }

        private void SellPersonalCargoSelection(PersonalCargoSaleResult sale)
        {
            if (sale.Sold)
            {
                startFlowController.ApplySessionState(sale.State);
                if (maintenanceController != null)
                {
                    maintenanceController.RefreshMaintenance();
                }
            }

            lastStatus = sale.Summary;
        }

        private void BindSellItemButtons()
        {
            var buttons = sellItemButtons ?? new Button[0];
            var count = Math.Min(buttons.Length, sellItemButtonActions.Length);
            for (var i = 0; i < count; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }

                var index = i;
                sellItemButtonActions[i] = sellItemButtonActions[i] ?? (() => SelectSellItem(index));
                buttons[i].onClick.AddListener(sellItemButtonActions[i]);
            }
        }

        private void UnbindSellItemButtons()
        {
            var buttons = sellItemButtons ?? new Button[0];
            var count = Math.Min(buttons.Length, sellItemButtonActions.Length);
            for (var i = 0; i < count; i++)
            {
                if (buttons[i] == null || sellItemButtonActions[i] == null)
                {
                    continue;
                }

                buttons[i].onClick.RemoveListener(sellItemButtonActions[i]);
            }
        }

        private void RefreshSellItemButtonStates(IReadOnlyList<SellCandidate> candidates)
        {
            var buttons = sellItemButtons ?? new Button[0];
            for (var i = 0; i < buttons.Length; i++)
            {
                var hasCandidate = candidates != null && i < candidates.Count;
                var active = activeSection == EquipmentShopSection.Sell && hasCandidate;
                SetButtonState(buttons[i], active);
                if (active)
                {
                    SetButtonLabel(buttons[i], (i + 1).ToString());
                }
            }
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
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

        private static string FormatSignedPercent(int value)
        {
            return value >= 0 ? "+" + value + "%" : value + "%";
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
            if (TryClickButtonAtScreenPosition(closeButton, pointerPosition, CloseShop) ||
                TryClickButtonAtScreenPosition(buyTabButton, pointerPosition, ShowBuyTab) ||
                TryClickButtonAtScreenPosition(sellTabButton, pointerPosition, ShowSellTab) ||
                TryClickButtonAtScreenPosition(buyStickButton, pointerPosition, BuyStick) ||
                TryClickButtonAtScreenPosition(buyMusketButton, pointerPosition, BuyMusket) ||
                TryClickButtonAtScreenPosition(buyShotgunButton, pointerPosition, BuyShotgun) ||
                TryClickButtonAtScreenPosition(buyFlashlightButton, pointerPosition, BuyFlashlight) ||
                TryClickButtonAtScreenPosition(buyInjuryRelieverButton, pointerPosition, BuyInjuryReliever) ||
                TryClickButtonAtScreenPosition(buyProtectiveSuitButton, pointerPosition, BuyProtectiveSuit) ||
                TryClickButtonAtScreenPosition(buyStrengthEnhancerButton, pointerPosition, BuyStrengthEnhancer) ||
                TryClickButtonAtScreenPosition(SellSelectedItemButton, pointerPosition, SellSelectedItem) ||
                TryClickSellItemButtonAtScreenPosition(pointerPosition) ||
                TryClickButtonAtScreenPosition(sellPersonalCargoButton, pointerPosition, SellSelectedItem))
            {
                return;
            }
        }

        private bool TryClickSellItemButtonAtScreenPosition(Vector2 pointerPosition)
        {
            var buttons = sellItemButtons ?? new Button[0];
            for (var i = 0; i < buttons.Length; i++)
            {
                var index = i;
                if (TryClickButtonAtScreenPosition(buttons[i], pointerPosition, () => SelectSellItem(index)))
                {
                    return true;
                }
            }

            return false;
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

        private static void SetTextNonBlocking(Text text)
        {
            if (text != null)
            {
                text.raycastTarget = false;
            }
        }
    }
}
