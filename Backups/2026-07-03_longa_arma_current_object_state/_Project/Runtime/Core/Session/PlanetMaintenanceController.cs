using System;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class PlanetMaintenanceController : MonoBehaviour
    {
        private static readonly ShipRoomId[] RoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom
        };

        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private GameObject maintenanceRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text walletText;
        [SerializeField] private Text roomStatusText;
        [SerializeField] private Text contractListText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button repairButton;
        [SerializeField] private Button contractBoardButton;
        [SerializeField] private Button associationContractButton;
        [SerializeField] private Button privateContractButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button personalCargoButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button planetBackButton;
        [SerializeField] private ContractBoardController contractBoardController;
        [SerializeField] private ShipUpgradeController shipUpgradeController;
        [SerializeField] private PlanetStayController planetStayController;

        private string lastStatus = string.Empty;
        private int maintenanceShownFrame = -1;

        public GameObject MaintenanceRoot => maintenanceRoot;

        public Text RoomStatusText => roomStatusText;

        public Text ContractListText => contractListText;

        public Text StatusText => statusText;

        public Button RepairButton => repairButton;

        public Button ContractBoardButton => contractBoardButton;

        public Button AssociationContractButton => associationContractButton;

        public Button PrivateContractButton => privateContractButton;

        public Button ShopButton => shopButton;

        public Button PersonalCargoButton => personalCargoButton;

        public Button UpgradesButton => upgradesButton;

        public Button PlanetBackButton => planetBackButton;

        public ShipUpgradeController UpgradeController => shipUpgradeController;

        public PlanetStayController PlanetStayController => planetStayController;

        public bool IsMaintenanceVisible => maintenanceRoot != null && maintenanceRoot.activeSelf;

        public GameSessionState CurrentSession => startFlowController != null
            ? startFlowController.CurrentSession
            : null;

        public void Configure(
            NewGameStartFlowController startController,
            ShipDeviceInteractionState deviceState,
            FirstPersonPlayerInput firstPersonInput,
            GameObject root,
            Text titleLabel,
            Text walletLabel,
            Text roomStatusLabel,
            Text contractListLabel,
            Text statusLabel,
            Button repairActionButton,
            Button associationActionButton,
            Button privateActionButton,
            Button shopEntryButton,
            Button personalCargoEntryButton,
            Button upgradesEntryButton)
        {
            startFlowController = startController;
            shipDeviceState = deviceState;
            playerInput = firstPersonInput;
            maintenanceRoot = root;
            titleText = titleLabel;
            walletText = walletLabel;
            roomStatusText = roomStatusLabel;
            contractListText = contractListLabel;
            statusText = statusLabel;
            repairButton = repairActionButton;
            associationContractButton = associationActionButton;
            privateContractButton = privateActionButton;
            shopButton = shopEntryButton;
            personalCargoButton = personalCargoEntryButton;
            upgradesButton = upgradesEntryButton;
            DisableTextRaycasts();
            BindButtons();
            HideMaintenance();
        }

        public void ConfigureContractBoard(
            ContractBoardController boardController,
            Button boardButton)
        {
            if (contractBoardButton != null)
            {
                contractBoardButton.onClick.RemoveListener(OpenContractBoardEntry);
            }

            contractBoardController = boardController;
            contractBoardButton = boardButton;
            if (contractBoardButton != null)
            {
                contractBoardButton.onClick.AddListener(OpenContractBoardEntry);
            }
        }

        public void ConfigureShipUpgrades(ShipUpgradeController upgradeController)
        {
            shipUpgradeController = upgradeController;
        }

        public void ConfigurePlanetStay(
            PlanetStayController planetController,
            Button backToPlanetButton)
        {
            if (planetBackButton != null)
            {
                planetBackButton.onClick.RemoveListener(ReturnToPlanet);
            }

            planetStayController = planetController;
            planetBackButton = backToPlanetButton;
            if (planetBackButton != null)
            {
                planetBackButton.onClick.AddListener(ReturnToPlanet);
            }
        }

        public void ShowMaintenance()
        {
            if (startFlowController == null || maintenanceRoot == null)
            {
                return;
            }

            startFlowController.PreparePostTransportContracts();
            maintenanceRoot.SetActive(true);
            maintenanceShownFrame = Time.frameCount;
            DisableTextRaycasts();
            SetCursorLockSuppressed(true);
            RefreshMaintenance();
        }

        public void HideMaintenance()
        {
            if (maintenanceRoot != null)
            {
                maintenanceRoot.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        public void RefreshMaintenance()
        {
            DisableTextRaycasts();
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "Planet Maintenance";
            }

            if (walletText != null)
            {
                walletText.text = "Wallet: " + FormatMoney(session.Wallet.Credits) +
                                  "\nRepair charge: " + FormatMoney(GetRepairCharge(session));
            }

            if (roomStatusText != null)
            {
                roomStatusText.text = BuildRoomStatusText(session.Ship);
            }

            if (contractListText != null)
            {
                contractListText.text = BuildContractBoardEntryText(session);
            }

            var repairCharge = GetRepairCharge(session);
            var hub = PlanetStayRules.CreateHubState(session);
            SetButtonState(repairButton, hub.CanOpenRepairShop && repairCharge > 0);
            SetButtonState(contractBoardButton, hub.CanOpenContractOffice);
            SetButtonState(associationContractButton, false);
            SetButtonState(privateContractButton, false);
            SetButtonState(shopButton, hub.CanOpenShop);
            SetButtonState(personalCargoButton, hub.CanOpenPersonalCargoDepot);
            SetButtonState(upgradesButton, hub.CanOpenShip);
            SetButtonState(planetBackButton, planetStayController != null && session.Phase == GameSessionPhase.Completed);

            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(lastStatus)
                    ? BuildStartReadinessText(session)
                    : lastStatus;
            }
        }

        public void RepairShip()
        {
            var session = CurrentSession;
            if (session == null || session.Phase != GameSessionPhase.Completed)
            {
                return;
            }

            var repairCharge = GetRepairCharge(session);
            if (repairCharge <= 0)
            {
                lastStatus = "No repair charge is pending.";
                RefreshMaintenance();
                return;
            }

            var repaired = session.ApplyMaintenanceRepair(repairCharge);
            startFlowController.ApplySessionState(repaired);
            if (shipDeviceState != null)
            {
                shipDeviceState.SetShipState(repaired.Ship);
            }

            lastStatus = "Repair charged: " + FormatMoney(repairCharge) + ". Ship condition restored.";
            RefreshMaintenance();
        }

        public void SelectAssociationContract()
        {
            OpenContractBoardEntry();
        }

        public void SelectPrivateContract()
        {
            OpenContractBoardEntry();
        }

        public void OpenContractBoardEntry()
        {
            if (contractBoardController == null)
            {
                lastStatus = "Contract board is not configured.";
                RefreshMaintenance();
                return;
            }

            HideMaintenance();
            contractBoardController.ShowBoard();
        }

        public void OpenShopEntry()
        {
            lastStatus = "Shop buy/sell skeleton available. Phase 15 weapon purchases are active.";
            RefreshMaintenance();
        }

        public void OpenPersonalCargoEntry()
        {
            lastStatus = "Personal cargo collection is available. Collected cargo is sold from the shop Sell tab.";
            RefreshMaintenance();
        }

        public void OpenUpgradesEntry()
        {
            if (shipUpgradeController == null)
            {
                lastStatus = "Ship upgrades are not configured.";
                RefreshMaintenance();
                return;
            }

            HideMaintenance();
            shipUpgradeController.ShowUpgrades();
        }

        public void ReturnToPlanet()
        {
            if (planetStayController == null)
            {
                lastStatus = "Planet hub is not configured.";
                RefreshMaintenance();
                return;
            }

            HideMaintenance();
            planetStayController.ShowPlanet();
        }

        private void Awake()
        {
            BindButtons();
            HideMaintenance();
        }

        private void Update()
        {
            TickTransportHazardOccurrence();
            TickSeedIntruderOccurrence();
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

        private void BindButtons()
        {
            UnbindButtons();
            if (repairButton != null)
            {
                repairButton.onClick.AddListener(RepairShip);
            }

            if (contractBoardButton != null)
            {
                contractBoardButton.onClick.AddListener(OpenContractBoardEntry);
            }

            if (associationContractButton != null)
            {
                associationContractButton.onClick.AddListener(SelectAssociationContract);
            }

            if (privateContractButton != null)
            {
                privateContractButton.onClick.AddListener(SelectPrivateContract);
            }

            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OpenShopEntry);
            }

            if (personalCargoButton != null)
            {
                personalCargoButton.onClick.AddListener(OpenPersonalCargoEntry);
            }

            if (upgradesButton != null)
            {
                upgradesButton.onClick.AddListener(OpenUpgradesEntry);
            }

            if (planetBackButton != null)
            {
                planetBackButton.onClick.AddListener(ReturnToPlanet);
            }
        }

        private void UnbindButtons()
        {
            if (repairButton != null)
            {
                repairButton.onClick.RemoveListener(RepairShip);
            }

            if (contractBoardButton != null)
            {
                contractBoardButton.onClick.RemoveListener(OpenContractBoardEntry);
            }

            if (associationContractButton != null)
            {
                associationContractButton.onClick.RemoveListener(SelectAssociationContract);
            }

            if (privateContractButton != null)
            {
                privateContractButton.onClick.RemoveListener(SelectPrivateContract);
            }

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OpenShopEntry);
            }

            if (personalCargoButton != null)
            {
                personalCargoButton.onClick.RemoveListener(OpenPersonalCargoEntry);
            }

            if (upgradesButton != null)
            {
                upgradesButton.onClick.RemoveListener(OpenUpgradesEntry);
            }

            if (planetBackButton != null)
            {
                planetBackButton.onClick.RemoveListener(ReturnToPlanet);
            }
        }

        private void TickSeedIntruderOccurrence()
        {
            var session = CurrentSession;
            if (shipDeviceState == null || session == null)
            {
                return;
            }

            shipDeviceState.TickSeedIntruderOccurrenceForCurrentRun(Time.deltaTime, session);
        }

        private void TickTransportHazardOccurrence()
        {
            var session = CurrentSession;
            if (shipDeviceState == null || session == null)
            {
                return;
            }

            shipDeviceState.TickTransportHazardOccurrenceForCurrentRun(Time.deltaTime, session);
        }

        private static int GetRepairCharge(GameSessionState session)
        {
            var currentRepairCost = session.Ship.IsTotalLoss
                ? ShipStateRules.CalculateTotalLossClaimCost(session.Ship)
                : ShipStateRules.CalculateRepairCost(session.Ship);
            if (currentRepairCost <= 0)
            {
                return 0;
            }

            return session.SettlementResult.PendingRepairCost > 0
                ? session.SettlementResult.PendingRepairCost
                : currentRepairCost;
        }

        private static string BuildRoomStatusText(ShipState ship)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < RoomOrder.Length; i++)
            {
                var roomId = RoomOrder[i];
                var room = ship.GetRoom(roomId);
                builder.Append(GetRoomDisplayName(roomId));
                builder.Append(": ");
                builder.Append(Mathf.RoundToInt(room.DurabilityPercent * 100f));
                builder.Append("% ");
                builder.Append(room.DurabilityTier);
                builder.Append(" - ");
                builder.Append(ShipStateRules.BuildRoomDamageEffectSummary(ship, roomId));
                if (i < RoomOrder.Length - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string BuildContractBoardEntryText(GameSessionState session)
        {
            var cargoHoldScore = Mathf.RoundToInt(ShipStateRules.CalculateCargoHoldScore(session.Ship) * 100f);
            var hub = PlanetStayRules.CreateHubState(session);
            return "Planet Map: Shop / Repair Shop / Ship / Cargo Supply Depot\n" +
                   "Contract Board: Association " + hub.ContractBoard.AssociationContractCount +
                   " | Private " + hub.ContractBoard.PrivateContractCount +
                   " | Special " + hub.ContractBoard.SpecialContractCount + "\n" +
                   "Fame: " + session.Reputation.FameScore +
                   " | Association fame: " + session.Reputation.AssociationFameScore + "\n" +
                   "Cargo hold score: " + cargoHoldScore + "\n" +
                   "Upgrades equipped: " + BuildUpgradeSummary(session.ShipUpgrades) + "\n" +
                   "Entry points: Repair / Contract Office / Shop / Cargo Depot / Ship";
        }

        private static string BuildStartReadinessText(GameSessionState session)
        {
            var readiness = ShipStateRules.EvaluateStartReadiness(session.Ship, session.PersonalCargoHold.HasCargo);
            var cargoHoldScore = Mathf.RoundToInt(ShipStateRules.CalculateCargoHoldScore(session.Ship) * 100f);
            if (!readiness.CanStartTransport)
            {
                if (readiness.IsPersonalCargoBlocked)
                {
                    return "Personal cargo cannot launch with current cargo hold damage. Sell it or repair. Cargo hold score: " +
                           cargoHoldScore;
                }

                if (readiness.IsCargoHoldBlocked)
                {
                    return "Cargo hold repair required before next transport. Cargo hold score: " + cargoHoldScore;
                }

                if (readiness.IsCockpitDestroyed)
                {
                    return "Cockpit repair required before next transport. Cargo hold score: " + cargoHoldScore;
                }

                if (readiness.IsEngineRoomDestroyed)
                {
                    return "Engine room repair required before next transport. Cargo hold score: " + cargoHoldScore;
                }

                return "Repair required before next transport. Cargo hold score: " + cargoHoldScore;
            }

            return "Ship ready for next contract. Cargo hold score: " + cargoHoldScore;
        }

        private static string GetRoomDisplayName(ShipRoomId roomId)
        {
            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return "Cockpit";
                case ShipRoomId.CargoHold:
                    return "Cargo Hold";
                case ShipRoomId.EngineRoom:
                    return "Engine Room";
                case ShipRoomId.ControlRoom:
                    return "Control Room";
                case ShipRoomId.Armory:
                    return "Armory";
                case ShipRoomId.SupplyRoom:
                    return "Supply Room";
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
            }
        }

        private static string BuildUpgradeSummary(ShipUpgradeState upgrades)
        {
            var categories = ShipUpgradeRules.GetCategoryOrder();
            var equippedTotal = 0;
            var purchasedTotal = 0;
            for (var i = 0; i < categories.Length; i++)
            {
                equippedTotal += upgrades.GetEquippedTier(categories[i]);
                purchasedTotal += upgrades.GetPurchasedTier(categories[i]);
            }

            return equippedTotal + "/" + purchasedTotal + " tiers";
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

        private static void SetButtonState(Button button, bool interactable)
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
                button.interactable = interactable;
            }
        }

        private void ProcessPointerClickFallback()
        {
            if (!IsMaintenanceVisible ||
                Time.frameCount == maintenanceShownFrame ||
                IsEquipmentShopVisible() ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickButtonAtScreenPosition(repairButton, pointerPosition, RepairShip) ||
                TryClickButtonAtScreenPosition(contractBoardButton, pointerPosition, OpenContractBoardEntry) ||
                TryClickButtonAtScreenPosition(associationContractButton, pointerPosition, SelectAssociationContract) ||
                TryClickButtonAtScreenPosition(privateContractButton, pointerPosition, SelectPrivateContract) ||
                TryClickButtonAtScreenPosition(shopButton, pointerPosition, OpenShopEntry) ||
                TryClickButtonAtScreenPosition(personalCargoButton, pointerPosition, OpenPersonalCargoEntry) ||
                TryClickButtonAtScreenPosition(upgradesButton, pointerPosition, OpenUpgradesEntry) ||
                TryClickButtonAtScreenPosition(planetBackButton, pointerPosition, ReturnToPlanet))
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

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }

        private static bool IsEquipmentShopVisible()
        {
            var shopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            return shopController != null && shopController.IsShopVisible;
        }

        private void DisableTextRaycasts()
        {
            SetTextNonBlocking(titleText);
            SetTextNonBlocking(walletText);
            SetTextNonBlocking(roomStatusText);
            SetTextNonBlocking(contractListText);
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
