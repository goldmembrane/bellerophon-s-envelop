using System.Text;
using Bellerophon.Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class PlanetStayController : MonoBehaviour
    {
        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private ContractBoardController contractBoardController;
        [SerializeField] private EquipmentShopController shopController;
        [SerializeField] private PersonalCargoController personalCargoController;
        [SerializeField] private ShipUpgradeController shipUpgradeController;
        [SerializeField] private GameObject planetRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button repairShopButton;
        [SerializeField] private Button contractOfficeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button cargoDepotButton;
        [SerializeField] private Button shipButton;

        private string lastStatus = string.Empty;

        public GameObject PlanetRoot => planetRoot;

        public Text TitleText => titleText;

        public Text BodyText => bodyText;

        public Text StatusText => statusText;

        public Button RepairShopButton => repairShopButton;

        public Button ContractOfficeButton => contractOfficeButton;

        public Button ShopButton => shopButton;

        public Button CargoDepotButton => cargoDepotButton;

        public Button ShipButton => shipButton;

        public bool IsPlanetVisible => planetRoot != null && planetRoot.activeSelf;

        public PlanetMaintenanceController MaintenanceController => maintenanceController;

        public ContractBoardController ContractBoardController => contractBoardController;

        public EquipmentShopController ShopController => shopController;

        public PersonalCargoController PersonalCargoController => personalCargoController;

        public ShipUpgradeController ShipUpgradeController => shipUpgradeController;

        public GameSessionState CurrentSession => startFlowController != null
            ? startFlowController.CurrentSession
            : null;

        public void Configure(
            NewGameStartFlowController startController,
            FirstPersonPlayerInput firstPersonInput,
            PlanetMaintenanceController maintenance,
            ContractBoardController board,
            EquipmentShopController shop,
            PersonalCargoController cargo,
            ShipUpgradeController upgrades,
            GameObject root,
            Text titleLabel,
            Text bodyLabel,
            Text statusLabel,
            Button repairButton,
            Button contractButton,
            Button shopEntryButton,
            Button cargoButton,
            Button shipEntryButton)
        {
            startFlowController = startController;
            playerInput = firstPersonInput;
            maintenanceController = maintenance;
            contractBoardController = board;
            shopController = shop;
            personalCargoController = cargo;
            shipUpgradeController = upgrades;
            planetRoot = root;
            titleText = titleLabel;
            bodyText = bodyLabel;
            statusText = statusLabel;
            repairShopButton = repairButton;
            contractOfficeButton = contractButton;
            shopButton = shopEntryButton;
            cargoDepotButton = cargoButton;
            shipButton = shipEntryButton;
            DisableTextRaycasts();
            BindButtons();
            HidePlanet();
        }

        public void ConfigureDestinations(
            ContractBoardController board,
            EquipmentShopController shop,
            PersonalCargoController cargo,
            ShipUpgradeController upgrades)
        {
            contractBoardController = board;
            shopController = shop;
            personalCargoController = cargo;
            shipUpgradeController = upgrades;
        }

        public void ShowPlanet()
        {
            if (startFlowController == null || planetRoot == null)
            {
                return;
            }

            startFlowController.PreparePostTransportContracts();
            planetRoot.SetActive(true);
            DisableTextRaycasts();
            SetCursorLockSuppressed(true);
            RefreshPlanet();
        }

        public void HidePlanet()
        {
            if (planetRoot != null)
            {
                planetRoot.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        public void RefreshPlanet()
        {
            DisableTextRaycasts();
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            var hub = PlanetStayRules.CreateHubState(session);
            if (titleText != null)
            {
                titleText.text = BuildTitleText(session);
            }

            if (bodyText != null)
            {
                bodyText.text = BuildBodyText(session, hub);
            }

            SetButtonState(repairShopButton, hub.CanOpenRepairShop);
            SetButtonState(contractOfficeButton, hub.CanOpenContractOffice);
            SetButtonState(shopButton, hub.CanOpenShop && shopController != null);
            SetButtonState(cargoDepotButton, hub.CanOpenPersonalCargoDepot && personalCargoController != null);
            SetButtonState(shipButton, hub.CanOpenShip && shipUpgradeController != null);

            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(lastStatus)
                    ? hub.ReadinessSummary
                    : lastStatus;
            }
        }

        public void OpenRepairShop()
        {
            if (maintenanceController == null)
            {
                lastStatus = "Repair shop is not configured.";
                RefreshPlanet();
                return;
            }

            HidePlanet();
            maintenanceController.ShowMaintenance();
        }

        public void OpenContractOffice()
        {
            if (contractBoardController == null)
            {
                lastStatus = "Contract office is not configured.";
                RefreshPlanet();
                return;
            }

            HidePlanet();
            contractBoardController.ShowBoardFromPlanet();
        }

        public void OpenShop()
        {
            if (shopController == null)
            {
                lastStatus = "Shop is not configured.";
                RefreshPlanet();
                return;
            }

            HidePlanet();
            shopController.ShowBuyTabFromPlanet();
        }

        public void OpenCargoDepot()
        {
            if (personalCargoController == null)
            {
                lastStatus = "Cargo depot is not configured.";
                RefreshPlanet();
                return;
            }

            HidePlanet();
            personalCargoController.ShowCargoCollectionFromPlanet();
        }

        public void OpenShip()
        {
            if (shipUpgradeController == null)
            {
                lastStatus = "Ship preparation is not configured.";
                RefreshPlanet();
                return;
            }

            HidePlanet();
            shipUpgradeController.ShowUpgradesFromPlanet();
        }

        private void Awake()
        {
            BindButtons();
            HidePlanet();
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

        private void BindButtons()
        {
            UnbindButtons();
            if (repairShopButton != null)
            {
                repairShopButton.onClick.AddListener(OpenRepairShop);
            }

            if (contractOfficeButton != null)
            {
                contractOfficeButton.onClick.AddListener(OpenContractOffice);
            }

            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OpenShop);
            }

            if (cargoDepotButton != null)
            {
                cargoDepotButton.onClick.AddListener(OpenCargoDepot);
            }

            if (shipButton != null)
            {
                shipButton.onClick.AddListener(OpenShip);
            }
        }

        private void UnbindButtons()
        {
            if (repairShopButton != null)
            {
                repairShopButton.onClick.RemoveListener(OpenRepairShop);
            }

            if (contractOfficeButton != null)
            {
                contractOfficeButton.onClick.RemoveListener(OpenContractOffice);
            }

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OpenShop);
            }

            if (cargoDepotButton != null)
            {
                cargoDepotButton.onClick.RemoveListener(OpenCargoDepot);
            }

            if (shipButton != null)
            {
                shipButton.onClick.RemoveListener(OpenShip);
            }
        }

        private static string BuildTitleText(GameSessionState session)
        {
            var planetName = session.CurrentPlanet.IsConfigured
                ? session.CurrentPlanet.DisplayName
                : "Docked Planet";
            return planetName + " Surface Hub";
        }

        private static string BuildBodyText(GameSessionState session, PlanetStayHubState hub)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Planet trait: " + PersonalCargoRules.FormatTraitName(session.CurrentPlanetTrait));
            builder.AppendLine("Wallet: " + FormatMoney(session.Wallet.Credits));
            builder.AppendLine("Repair charge: " + FormatMoney(hub.RepairCharge));
            builder.AppendLine("Contracts: Association " + hub.ContractBoard.AssociationContractCount +
                               " / Private " + hub.ContractBoard.PrivateContractCount +
                               " / Special " + hub.ContractBoard.SpecialContractCount);
            builder.AppendLine();
            builder.AppendLine("Surface map");

            var markers = hub.MapMarkers;
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                builder.AppendLine(
                    " - " + marker.DisplayName +
                    " (" + Mathf.RoundToInt(marker.NormalizedX * 100f) +
                    ", " + Mathf.RoundToInt(marker.NormalizedY * 100f) + ")");
            }

            builder.AppendLine();
            builder.AppendLine("Ship readiness: " + hub.ReadinessSummary);
            builder.AppendLine("Cargo depot: " + (hub.CanCollectPersonalCargo ? "common cargo available" : "no free cargo space or cargo already collected"));
            return builder.ToString();
        }

        private void ProcessPointerClickFallback()
        {
            if (!IsPlanetVisible ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickButtonAtScreenPosition(repairShopButton, pointerPosition, OpenRepairShop) ||
                TryClickButtonAtScreenPosition(contractOfficeButton, pointerPosition, OpenContractOffice) ||
                TryClickButtonAtScreenPosition(shopButton, pointerPosition, OpenShop) ||
                TryClickButtonAtScreenPosition(cargoDepotButton, pointerPosition, OpenCargoDepot) ||
                TryClickButtonAtScreenPosition(shipButton, pointerPosition, OpenShip))
            {
                return;
            }
        }

        private void SetCursorLockSuppressed(bool suppressed)
        {
            if (playerInput == null)
            {
                playerInput = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }

            if (playerInput != null)
            {
                playerInput.SetCursorLockSuppressed(suppressed);
            }
        }

        private static void SetButtonState(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.interactable = interactable;
        }

        private static bool TryClickButtonAtScreenPosition(Button button, Vector2 screenPosition, UnityEngine.Events.UnityAction action)
        {
            if (button == null ||
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

            action?.Invoke();
            return true;
        }

        private static void DisableTextRaycasts()
        {
            var labels = Object.FindObjectsByType<Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i].raycastTarget = false;
            }
        }

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }
    }
}
