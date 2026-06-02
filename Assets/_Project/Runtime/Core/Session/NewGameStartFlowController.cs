using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class NewGameStartFlowController : MonoBehaviour
    {
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button tutorialContractButton;
        [SerializeField] private FirstPersonPlayerInput playerInput;

        private NewGameStartFlowState flowState;
        private bool buttonsBound;

        public NewGameStartFlowState FlowState
        {
            get
            {
                EnsureState();
                return flowState;
            }
        }

        public GameSessionState CurrentSession => FlowState.Session;

        public Text TitleText => titleText;

        public Text BodyText => bodyText;

        public Text StatusText => statusText;

        public Button YesButton => yesButton;

        public Button TutorialContractButton => tutorialContractButton;

        public ShipDeviceInteractionState ShipDeviceState => shipDeviceState;

        public FirstPersonPlayerInput PlayerInput => playerInput;

        public void Configure(
            Text titleLabel,
            Text bodyLabel,
            Text statusLabel,
            Button acceptAssociationButton,
            Button acceptTutorialButton,
            ShipDeviceInteractionState deviceState,
            FirstPersonPlayerInput firstPersonInput = null)
        {
            titleText = titleLabel;
            bodyText = bodyLabel;
            statusText = statusLabel;
            yesButton = acceptAssociationButton;
            tutorialContractButton = acceptTutorialButton;
            shipDeviceState = deviceState;
            playerInput = firstPersonInput;
            EnsureState();
            EnsurePlayerInput();
            BindButtons();
            Refresh();
        }

        public void AcceptAssociationContract()
        {
            EnsureState();
            if (flowState.Phase != NewGameStartFlowPhase.ContractPrompt)
            {
                return;
            }

            flowState = flowState.AcceptAssociationContract();
            Refresh();
        }

        public void AcceptTutorialContract()
        {
            EnsureState();
            if (flowState.Phase != NewGameStartFlowPhase.AssociationPlanet)
            {
                return;
            }

            flowState = flowState.AcceptTutorialContract();
            ApplyActiveCargoToShipDevices();
            Refresh();
            CloseStartUi();
        }

        private void Awake()
        {
            EnsureState();
            EnsurePlayerInput();
            BindButtons();
            Refresh();
        }

        private void OnEnable()
        {
            EnsureState();
            EnsurePlayerInput();
            BindButtons();
            Refresh();
        }

        private void Update()
        {
            ApplyCursorMode();
        }

        private void OnDisable()
        {
            SetCursorLockSuppressed(false);
        }

        private void OnDestroy()
        {
            if (yesButton != null)
            {
                yesButton.onClick.RemoveListener(AcceptAssociationContract);
            }

            if (tutorialContractButton != null)
            {
                tutorialContractButton.onClick.RemoveListener(AcceptTutorialContract);
            }

            buttonsBound = false;
            SetCursorLockSuppressed(false);
        }

        private void EnsureState()
        {
            if (flowState == null)
            {
                flowState = NewGameStartFlowState.CreateNewGame();
            }
        }

        private void BindButtons()
        {
            if (buttonsBound)
            {
                return;
            }

            var hasButton = false;
            if (yesButton != null)
            {
                yesButton.onClick.RemoveListener(AcceptAssociationContract);
                yesButton.onClick.AddListener(AcceptAssociationContract);
                hasButton = true;
            }

            if (tutorialContractButton != null)
            {
                tutorialContractButton.onClick.RemoveListener(AcceptTutorialContract);
                tutorialContractButton.onClick.AddListener(AcceptTutorialContract);
                hasButton = true;
            }

            buttonsBound = hasButton;
        }

        private void EnsurePlayerInput()
        {
            if (playerInput == null)
            {
                playerInput = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }
        }

        private void Refresh()
        {
            EnsureState();
            ApplyCursorMode();

            if (yesButton != null)
            {
                SetButtonVisible(yesButton, flowState.Phase == NewGameStartFlowPhase.ContractPrompt);
            }

            if (tutorialContractButton != null)
            {
                SetButtonVisible(tutorialContractButton, flowState.Phase == NewGameStartFlowPhase.AssociationPlanet);
            }

            switch (flowState.Phase)
            {
                case NewGameStartFlowPhase.ContractPrompt:
                    SetText(
                        "Association Contract",
                        "Transport Association membership agreement.\n\nProceed with association membership.",
                        "Start state: contract pending.");
                    break;
                case NewGameStartFlowPhase.AssociationPlanet:
                    SetText(
                        "Association Start Planet",
                        BuildPlanetStartText(),
                        "Only the 60 second tutorial contract is available.");
                    break;
                case NewGameStartFlowPhase.TutorialContractAccepted:
                    SetText(
                        "Tutorial Contract Accepted",
                        BuildAcceptedContractText(),
                        "Cargo Hold Center Cargo is registered as the active transport target.");
                    break;
            }
        }

        private void ApplyCursorMode()
        {
            SetCursorLockSuppressed(RequiresPointerInput());
        }

        private bool RequiresPointerInput()
        {
            EnsureState();
            return flowState.Phase == NewGameStartFlowPhase.ContractPrompt ||
                   flowState.Phase == NewGameStartFlowPhase.AssociationPlanet;
        }

        private void SetCursorLockSuppressed(bool suppressed)
        {
            EnsurePlayerInput();
            if (playerInput != null)
            {
                playerInput.SetCursorLockSuppressed(suppressed);
                return;
            }

            if (Application.isPlaying && suppressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private string BuildPlanetStartText()
        {
            var session = flowState.Session;
            var contract = flowState.GetAvailableContract(0);
            return "Association logo sign: present\n"
                   + "Credits: " + session.Wallet.Credits + "\n"
                   + "Ship: Default Cargo Ship\n"
                   + "Suit: Basic Protective Suit\n"
                   + "Weapon: Stick x" + session.StartingLoadout.StickCount + "\n"
                   + "Available: " + contract.DisplayName + " (" + contract.DurationSeconds + "s)";
        }

        private string BuildAcceptedContractText()
        {
            var session = flowState.Session;
            if (!session.ActiveTransportContract.HasValue || !session.ActiveCargo.HasValue)
            {
                return "No active tutorial transport.";
            }

            var contract = session.ActiveTransportContract.Value;
            var cargo = session.ActiveCargo.Value;
            return "Contract: " + contract.DisplayName + "\n"
                   + "Duration: " + contract.DurationSeconds + "s\n"
                   + "Target: " + contract.TransportTargetName + "\n"
                   + "Cargo durability: " + Mathf.RoundToInt(cargo.DurabilityPercent * 100f) + "%\n"
                   + "Session: " + session.Phase;
        }

        private void SetText(string title, string body, string status)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (statusText != null)
            {
                statusText.text = status;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            button.gameObject.SetActive(true);
            button.interactable = visible;

            var group = button.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = button.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void ApplyActiveCargoToShipDevices()
        {
            if (shipDeviceState == null || !flowState.Session.ActiveCargo.HasValue)
            {
                return;
            }

            shipDeviceState.SetCargoState(flowState.Session.ActiveCargo.Value);
        }

        private void CloseStartUi()
        {
            SetCursorLockSuppressed(false);
            gameObject.SetActive(false);
        }
    }
}
