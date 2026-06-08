using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private Button noButton;
        [SerializeField] private Button tutorialContractButton;
        [SerializeField] private Button skipTutorialButton;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private bool enableLocalPersistence;
        [SerializeField] private bool loadFullSavedSessionOnStartup;
        [SerializeField] private string saveSlotId = SaveGameService.DefaultSlotId;

        private NewGameStartFlowState flowState;
        private SaveGameService saveGameService;
        private GameSettingsState currentSettings = GameSettingsState.Default;
        private bool buttonsBound;
        private bool ownsCursorSuppression;
        private string lastStatus = string.Empty;

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

        public Button NoButton => noButton;

        public Button TutorialContractButton => tutorialContractButton;

        public Button SkipTutorialButton => skipTutorialButton;

        public ShipDeviceInteractionState ShipDeviceState => shipDeviceState;

        public FirstPersonPlayerInput PlayerInput => playerInput;

        public int AvailableContractCount => FlowState.AvailableContractCount;

        public void Configure(
            Text titleLabel,
            Text bodyLabel,
            Text statusLabel,
            Button acceptAssociationButton,
            Button acceptTutorialButton,
            ShipDeviceInteractionState deviceState,
            FirstPersonPlayerInput firstPersonInput = null,
            Button rejectAssociationButton = null,
            Button skipTutorialActionButton = null)
        {
            titleText = titleLabel;
            bodyText = bodyLabel;
            statusText = statusLabel;
            yesButton = acceptAssociationButton;
            noButton = rejectAssociationButton;
            tutorialContractButton = acceptTutorialButton;
            skipTutorialButton = skipTutorialActionButton;
            shipDeviceState = deviceState;
            playerInput = firstPersonInput;
            EnsureState();
            EnsurePlayerInput();
            BindButtons();
            Refresh();
        }

        public void ConfigurePersistence(
            bool enabled,
            string slotId = SaveGameService.DefaultSlotId,
            bool loadFullSavedSession = false)
        {
            enableLocalPersistence = enabled;
            saveSlotId = string.IsNullOrWhiteSpace(slotId) ? SaveGameService.DefaultSlotId : slotId;
            loadFullSavedSessionOnStartup = loadFullSavedSession;
        }

        public void AcceptAssociationContract()
        {
            EnsureState();
            if (flowState.Phase != NewGameStartFlowPhase.ContractPrompt)
            {
                return;
            }

            if (!flowState.CanAcceptAssociationContract)
            {
                lastStatus = "Contract agreement is still scrolling. Down reaches the bottom faster.";
                Refresh();
                return;
            }

            flowState = flowState.AcceptAssociationContract();
            lastStatus = string.Empty;
            ApplySessionEquipmentToShipDevices();
            Refresh();
            SaveCurrentFlow();
        }

        public void RejectAssociationContract()
        {
            EnsureState();
            if (flowState.Phase != NewGameStartFlowPhase.ContractPrompt)
            {
                return;
            }

            var result = flowState.RejectAssociationContract();
            flowState = result.State;
            lastStatus = result.Summary;
            Refresh();
            SaveCurrentFlow();
        }

        public void SkipTutorialForReturningPlayer()
        {
            EnsureState();
            if (!flowState.CanSkipTutorial)
            {
                lastStatus = "Tutorial skip is only available after the first tutorial has been completed.";
                Refresh();
                return;
            }

            flowState = flowState.SkipTutorialForReturningPlayer();
            lastStatus = "Tutorial skipped. $1100 granted and post-tutorial contracts are visible.";
            ApplySessionEquipmentToShipDevices();
            Refresh();
            SaveCurrentFlow();
            OpenPostTutorialPlanetStayIfConfigured();
        }

        public void SetTutorialCompletedBefore(bool hasCompletedTutorialBefore)
        {
            EnsureState();
            flowState = flowState.WithTutorialCompletedBefore(hasCompletedTutorialBefore);
            Refresh();
            SaveCurrentFlow();
        }

        public void FastForwardAssociationContractForValidation()
        {
            EnsureState();
            if (flowState.Phase != NewGameStartFlowPhase.ContractPrompt)
            {
                return;
            }

            flowState = flowState.MoveAssociationContractToBottom();
            lastStatus = string.Empty;
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
            lastStatus = string.Empty;
            ApplyActiveCargoToShipDevices();
            Refresh();
            SaveCurrentFlow();
            CloseStartUi();
        }

        public void ApplySessionState(GameSessionState session)
        {
            EnsureState();
            flowState = flowState.WithSession(session);
            ApplySessionEquipmentToShipDevices();
            Refresh();
            SaveCurrentFlow();
        }

        public void PreparePostTransportContracts()
        {
            EnsureState();
            flowState = flowState.PreparePostTransportContracts();
            Refresh();
            SaveCurrentFlow();
        }

        public TransportContractDefinition GetAvailableContract(int index)
        {
            return FlowState.GetAvailableContract(index);
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
            ProcessAssociationContractInput();
            ApplyCursorMode();
            TickTransportHazardOccurrence();
            TickSeedIntruderOccurrence();
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

            if (noButton != null)
            {
                noButton.onClick.RemoveListener(RejectAssociationContract);
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.RemoveListener(SkipTutorialForReturningPlayer);
            }

            buttonsBound = false;
            SetCursorLockSuppressed(false);
        }

        private void EnsureState()
        {
            if (flowState == null)
            {
                flowState = CreateInitialFlowState();
            }
        }

        private NewGameStartFlowState CreateInitialFlowState()
        {
            if (!enableLocalPersistence || !Application.isPlaying)
            {
                return NewGameStartFlowState.CreateNewGame();
            }

            try
            {
                NewGameStartFlowState loadedFlow;
                GameSettingsState loadedSettings;
                var service = GetSaveGameService();
                if (loadFullSavedSessionOnStartup &&
                    service.TryLoad(saveSlotId, out loadedFlow, out loadedSettings))
                {
                    currentSettings = loadedSettings;
                    return loadedFlow;
                }

                if (service.TryCreateNewGameFromProfile(saveSlotId, out loadedFlow))
                {
                    return loadedFlow;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load Bellerophon save slot '" + saveSlotId + "': " + exception.Message);
            }

            return NewGameStartFlowState.CreateNewGame();
        }

        private SaveGameService GetSaveGameService()
        {
            return saveGameService ?? (saveGameService = new SaveGameService(FileSaveGameRepository.CreateDefault()));
        }

        private void SaveCurrentFlow()
        {
            if (!enableLocalPersistence || !Application.isPlaying || flowState == null)
            {
                return;
            }

            try
            {
                GetSaveGameService().Save(saveSlotId, flowState, currentSettings);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to save Bellerophon slot '" + saveSlotId + "': " + exception.Message);
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

            if (noButton != null)
            {
                noButton.onClick.RemoveListener(RejectAssociationContract);
                noButton.onClick.AddListener(RejectAssociationContract);
                hasButton = true;
            }

            if (tutorialContractButton != null)
            {
                tutorialContractButton.onClick.RemoveListener(AcceptTutorialContract);
                tutorialContractButton.onClick.AddListener(AcceptTutorialContract);
                hasButton = true;
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.RemoveListener(SkipTutorialForReturningPlayer);
                skipTutorialButton.onClick.AddListener(SkipTutorialForReturningPlayer);
                hasButton = true;
            }

            buttonsBound = hasButton;
        }

        private void EnsurePlayerInput()
        {
            if (playerInput == null)
            {
                playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }
        }

        private void Refresh()
        {
            EnsureState();
            ApplyCursorMode();

            if (yesButton != null)
            {
                SetButtonVisible(yesButton, flowState.CanAcceptAssociationContract);
            }

            if (noButton != null)
            {
                SetButtonVisible(
                    noButton,
                    flowState.Phase == NewGameStartFlowPhase.ContractPrompt &&
                    flowState.AssociationContractScroll.HasReachedBottom);
            }

            if (tutorialContractButton != null)
            {
                SetButtonVisible(
                    tutorialContractButton,
                    flowState.Phase == NewGameStartFlowPhase.AssociationPlanet &&
                    flowState.AvailableContractCount == 1 &&
                    flowState.GetAvailableContract(0).IsTutorial &&
                    !flowState.TutorialSkipped);
            }

            if (skipTutorialButton != null)
            {
                SetButtonVisible(skipTutorialButton, flowState.CanSkipTutorial);
            }

            switch (flowState.Phase)
            {
                case NewGameStartFlowPhase.ContractPrompt:
                    SetText(
                        "Association Contract",
                        BuildContractPromptText(),
                        GetStatusText("Contract scroll: " + flowState.AssociationContractScroll.ProgressPercent + "%."));
                    break;
                case NewGameStartFlowPhase.AssociationPlanet:
                    SetText(
                        "Association Start Planet",
                        BuildPlanetStartText(),
                        GetStatusText(flowState.TutorialSkipped
                            ? "Tutorial skipped. Post-tutorial contracts are visible."
                            : "Only the 60 second tutorial contract is available."));
                    break;
                case NewGameStartFlowPhase.PrivateBusinessPlanet:
                    SetText(
                        "Private Business Route",
                        BuildPrivateBusinessStartText(),
                        GetStatusText("Association contract cancelled before tentative consent."));
                    break;
                case NewGameStartFlowPhase.TutorialContractAccepted:
                    SetText(
                        "Tutorial Contract Accepted",
                        BuildAcceptedContractText(),
                        GetStatusText("Cargo Hold Center Cargo is registered as the active transport target."));
                    break;
            }
        }

        private void ProcessAssociationContractInput()
        {
            EnsureState();
            if (flowState.Phase != NewGameStartFlowPhase.ContractPrompt)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var nextState = flowState;
            var forceRefresh = false;
            if (keyboard != null &&
                (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
                keyboard.cKey.wasPressedThisFrame)
            {
                nextState = nextState.StopAssociationContractScroll();
                lastStatus = "Association contract scrolling stopped. Ctrl+X starts the private business route.";
                forceRefresh = true;
            }
            else if (keyboard != null &&
                     (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed) &&
                     keyboard.xKey.wasPressedThisFrame)
            {
                var result = nextState.StartPrivateBusinessRouteFromStoppedContract();
                nextState = result.State;
                lastStatus = result.Summary;
                forceRefresh = true;
            }
            else if (keyboard != null && keyboard.downArrowKey.isPressed)
            {
                nextState = nextState.TickAssociationContractDownArrowFastMove(Time.deltaTime);
            }
            else
            {
                nextState = nextState.TickAssociationContractScroll(Time.deltaTime);
            }

            if (forceRefresh || !ReferenceEquals(nextState, flowState))
            {
                flowState = nextState;
                Refresh();
                if (forceRefresh)
                {
                    SaveCurrentFlow();
                }
            }
        }

        private void ApplyCursorMode()
        {
            if (RequiresPointerInput())
            {
                SetCursorLockSuppressed(true);
                return;
            }

            if (ownsCursorSuppression)
            {
                SetCursorLockSuppressed(false);
            }
        }

        private bool RequiresPointerInput()
        {
            EnsureState();
            return flowState.Phase == NewGameStartFlowPhase.ContractPrompt ||
                   flowState.Phase == NewGameStartFlowPhase.AssociationPlanet ||
                   flowState.Phase == NewGameStartFlowPhase.PrivateBusinessPlanet;
        }

        private void SetCursorLockSuppressed(bool suppressed)
        {
            EnsurePlayerInput();
            if (playerInput != null)
            {
                if (!suppressed && !ownsCursorSuppression)
                {
                    return;
                }

                playerInput.SetCursorLockSuppressed(suppressed);
                ownsCursorSuppression = suppressed;
                return;
            }

            if (Application.isPlaying && suppressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ownsCursorSuppression = true;
                return;
            }

            ownsCursorSuppression = false;
        }

        private string BuildPlanetStartText()
        {
            var session = flowState.Session;
            var builder = "Association logo sign: present\n"
                          + "Credits: " + session.Wallet.Credits + "\n"
                          + "Ship: Default Cargo Ship\n"
                          + "Suit: " + (session.Equipment.HasBasicProtectiveSuit ? "Basic Protective Suit" : "None") + "\n"
                          + "Weapon: " + EquipmentRules.FormatItemName(session.Equipment.GetHandSlot(0).ItemKind) + " x" + session.StartingLoadout.StickCount + "\n";
            if (flowState.AvailableContractCount <= 0)
            {
                return builder + "Available: None";
            }

            var contract = flowState.GetAvailableContract(0);
            return builder + "Available: " + contract.DisplayName + " (" + contract.DurationSeconds + "s, $" + contract.RewardCredits + ")";
        }

        private string BuildContractPromptText()
        {
            return "Transport Association membership agreement.\n\n" +
                   "Auto scroll reaches the bottom in 60 seconds.\n" +
                   "Down reaches the bottom in 3 seconds.\n" +
                   "Ctrl+C stops the scroll; Ctrl+X starts the private business route only after Ctrl+C.\n" +
                   "Progress: " + flowState.AssociationContractScroll.ProgressPercent + "%";
        }

        private string BuildPrivateBusinessStartText()
        {
            var session = flowState.Session;
            return "Association: Non-member\n" +
                   "Association logo sign: absent\n" +
                   "Credits: " + session.Wallet.Credits + "\n" +
                   "Available route: Private business start";
        }

        private string GetStatusText(string defaultStatus)
        {
            return string.IsNullOrWhiteSpace(lastStatus) ? defaultStatus : lastStatus;
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
                   + "Reward: $" + contract.RewardCredits + "\n"
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

            shipDeviceState.SetShipState(flowState.Session.Ship);
            shipDeviceState.SetCargoState(flowState.Session.ActiveCargo.Value);
            shipDeviceState.SetEquipmentState(flowState.Session.Equipment);
            shipDeviceState.SetShipUpgradeState(flowState.Session.ShipUpgrades);
            if (flowState.Session.ActiveTransportContract.HasValue)
            {
                shipDeviceState.StartTransportRun(flowState.Session.ActiveTransportContract.Value.DurationSeconds);
            }
        }

        private void ApplySessionEquipmentToShipDevices()
        {
            if (shipDeviceState == null)
            {
                return;
            }

            shipDeviceState.SetEquipmentState(flowState.Session.Equipment);
            shipDeviceState.SetShipUpgradeState(flowState.Session.ShipUpgrades);
        }

        private void TickSeedIntruderOccurrence()
        {
            if (shipDeviceState == null || flowState == null)
            {
                return;
            }

            shipDeviceState.TickSeedIntruderOccurrenceForCurrentRun(Time.deltaTime, flowState.Session);
        }

        private void TickTransportHazardOccurrence()
        {
            if (shipDeviceState == null || flowState == null)
            {
                return;
            }

            shipDeviceState.TickTransportHazardOccurrenceForCurrentRun(Time.deltaTime, flowState.Session);
        }

        private void CloseStartUi()
        {
            SetCursorLockSuppressed(false);
            gameObject.SetActive(false);
        }

        private void OpenPostTutorialPlanetStayIfConfigured()
        {
            var planetStay = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            if (planetStay == null)
            {
                CloseStartUi();
                return;
            }

            CloseStartUi();
            planetStay.ShowPlanet();
        }
    }
}
