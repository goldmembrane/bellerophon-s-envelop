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
        [SerializeField] private Button associationContractButton;
        [SerializeField] private Button privateContractButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button personalCargoButton;
        [SerializeField] private Button upgradesButton;

        private string lastStatus = string.Empty;

        public GameObject MaintenanceRoot => maintenanceRoot;

        public Text RoomStatusText => roomStatusText;

        public Text ContractListText => contractListText;

        public Text StatusText => statusText;

        public Button RepairButton => repairButton;

        public Button AssociationContractButton => associationContractButton;

        public Button PrivateContractButton => privateContractButton;

        public Button ShopButton => shopButton;

        public Button PersonalCargoButton => personalCargoButton;

        public Button UpgradesButton => upgradesButton;

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

        public void ShowMaintenance()
        {
            if (startFlowController == null || maintenanceRoot == null)
            {
                return;
            }

            startFlowController.PreparePostTransportContracts();
            maintenanceRoot.SetActive(true);
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
                contractListText.text = BuildContractListText(session);
            }

            var repairCharge = GetRepairCharge(session);
            SetButtonState(repairButton, repairCharge > 0);
            SetButtonState(associationContractButton, CanStartContract(session, GetContract(0)));
            SetButtonState(privateContractButton, CanStartContract(session, GetContract(1)));
            SetButtonState(shopButton, true);
            SetButtonState(personalCargoButton, true);
            SetButtonState(upgradesButton, true);

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
            StartSelectedContract(GetContract(0));
        }

        public void SelectPrivateContract()
        {
            StartSelectedContract(GetContract(1));
        }

        public void OpenShopEntry()
        {
            lastStatus = "Shop buy/sell skeleton available. Phase 15 weapon purchases are active.";
            RefreshMaintenance();
        }

        public void OpenPersonalCargoEntry()
        {
            lastStatus = "Personal cargo entry point available. Collection and trading are scheduled for a later phase.";
            RefreshMaintenance();
        }

        public void OpenUpgradesEntry()
        {
            lastStatus = "Upgrades entry point available. Detailed upgrades are scheduled for a later phase.";
            RefreshMaintenance();
        }

        private void Awake()
        {
            BindButtons();
            HideMaintenance();
        }

        private void Update()
        {
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
        }

        private void UnbindButtons()
        {
            if (repairButton != null)
            {
                repairButton.onClick.RemoveListener(RepairShip);
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
        }

        private void StartSelectedContract(TransportContractDefinition contract)
        {
            var session = CurrentSession;
            if (session == null || !CanStartContract(session, contract))
            {
                lastStatus = "Repair and cargo hold readiness are required before selecting this contract.";
                RefreshMaintenance();
                return;
            }

            var nextSession = session.StartTransport(contract);
            startFlowController.ApplySessionState(nextSession);
            if (shipDeviceState != null)
            {
                shipDeviceState.SetShipState(nextSession.Ship);
                shipDeviceState.SetCargoState(contract.Cargo);
                shipDeviceState.SetEquipmentState(nextSession.Equipment);
                shipDeviceState.StartTransportRun(contract.DurationSeconds);
                shipDeviceState.TryStartAsteroidFieldForCurrentRun(nextSession);
            }

            lastStatus = "Contract selected: " + contract.DisplayName;
            HideMaintenance();
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

        private TransportContractDefinition GetContract(int index)
        {
            if (startFlowController != null && startFlowController.AvailableContractCount > index)
            {
                return startFlowController.GetAvailableContract(index);
            }

            return index == 0
                ? TransportContractDefinition.CreateAssociationFollowUp()
                : TransportContractDefinition.CreatePrivateFollowUp();
        }

        private static bool CanStartContract(GameSessionState session, TransportContractDefinition contract)
        {
            if (session == null || session.Phase != GameSessionPhase.Completed)
            {
                return false;
            }

            var readiness = ShipStateRules.EvaluateStartReadiness(session.Ship);
            var cargoHoldScore = Mathf.RoundToInt(ShipStateRules.CalculateCargoHoldScore(session.Ship) * 100f);
            return readiness.CanStartTransport && cargoHoldScore >= contract.RequiredCargoHoldScore;
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
                builder.Append(GetRiskNotice(roomId, room));
                if (i < RoomOrder.Length - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private string BuildContractListText(GameSessionState session)
        {
            var association = GetContract(0);
            var privateContract = GetContract(1);
            var builder = new StringBuilder();
            builder.AppendLine(BuildContractLine("Association", association, session));
            builder.AppendLine(BuildContractLine("Private", privateContract, session));
            builder.AppendLine();
            builder.Append("Entry points: Shop / Personal Cargo / Upgrades");
            return builder.ToString();
        }

        private static string BuildContractLine(string prefix, TransportContractDefinition contract, GameSessionState session)
        {
            return prefix + ": " +
                   contract.DisplayName +
                   " | Reward " + FormatMoney(contract.RewardCredits) +
                   " | Duration " + contract.DurationSeconds + "s" +
                   " | Required cargo score " + contract.RequiredCargoHoldScore +
                   " | Difficulty " + contract.Difficulty +
                   " | " + (CanStartContract(session, contract) ? "Ready" : "Needs repair");
        }

        private static string BuildStartReadinessText(GameSessionState session)
        {
            var readiness = ShipStateRules.EvaluateStartReadiness(session.Ship);
            var cargoHoldScore = Mathf.RoundToInt(ShipStateRules.CalculateCargoHoldScore(session.Ship) * 100f);
            if (!readiness.CanStartTransport)
            {
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

        private static string GetRiskNotice(ShipRoomId roomId, ShipRoomState room)
        {
            if (room.DurabilityTier == ShipRoomDurabilityTier.Optimal)
            {
                return "No active maintenance risk.";
            }

            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return room.CurrentDurability <= 0
                        ? "Launch control blocked; transport duration penalty applies."
                        : "Auto pilot reliability risk.";
                case ShipRoomId.CargoHold:
                    return room.DurabilityPercent <= ShipStateRules.CargoHoldBlockedThreshold
                        ? "Cargo hold readiness blocked; cargo loss risk."
                        : "Cargo safety score reduced.";
                case ShipRoomId.EngineRoom:
                    return room.CurrentDurability <= 0
                        ? "Towing and transport failure risk."
                        : "Travel time and power stability risk.";
                case ShipRoomId.ControlRoom:
                    return room.CurrentDurability <= 0
                        ? "Control warning active; monitoring unreliable."
                        : "Control screen stability risk.";
                case ShipRoomId.Armory:
                    return "Turret response risk.";
                case ShipRoomId.SupplyRoom:
                    return "Supply access risk.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
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
                IsEquipmentShopVisible() ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickButtonAtScreenPosition(repairButton, pointerPosition, RepairShip) ||
                TryClickButtonAtScreenPosition(associationContractButton, pointerPosition, SelectAssociationContract) ||
                TryClickButtonAtScreenPosition(privateContractButton, pointerPosition, SelectPrivateContract) ||
                TryClickButtonAtScreenPosition(shopButton, pointerPosition, OpenShopEntry) ||
                TryClickButtonAtScreenPosition(personalCargoButton, pointerPosition, OpenPersonalCargoEntry) ||
                TryClickButtonAtScreenPosition(upgradesButton, pointerPosition, OpenUpgradesEntry))
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
