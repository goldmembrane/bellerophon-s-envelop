using System;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class ContractBoardController : MonoBehaviour
    {
        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private GameObject boardRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text contractListText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button[] contractSlotButtons = new Button[0];
        [SerializeField] private Button associationContractButton;
        [SerializeField] private Button privateContractButton;
        [SerializeField] private Button specialContractButton;
        [SerializeField] private Button previousContractButton;
        [SerializeField] private Button nextContractButton;
        [SerializeField] private Button acceptContractButton;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button backButton;

        private readonly DetailedContentCatalog catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
        private ContractContentDefinition[] visibleContracts = new ContractContentDefinition[0];
        private UnityAction[] contractSlotClickActions = new UnityAction[0];
        private ContractType selectedContractType = ContractType.Association;
        private int selectedContractIndex = -1;
        private string lastStatus = string.Empty;

        public GameObject BoardRoot => boardRoot;

        public Text SummaryText => summaryText;

        public Text ContractListText => contractListText;

        public Text StatusText => statusText;

        public Button[] ContractSlotButtons => contractSlotButtons;

        public Button AssociationContractButton => associationContractButton;

        public Button PrivateContractButton => privateContractButton;

        public Button SpecialContractButton => specialContractButton;

        public Button PreviousContractButton => previousContractButton;

        public Button NextContractButton => nextContractButton;

        public Button AcceptContractButton => acceptContractButton;

        public Button StartRunButton => startRunButton;

        public Button BackButton => backButton;

        public bool IsBoardVisible => boardRoot != null && boardRoot.activeSelf;

        public GameSessionState CurrentSession => startFlowController != null
            ? startFlowController.CurrentSession
            : null;

        public int VisibleContractCount => visibleContracts == null ? 0 : visibleContracts.Length;

        public string SelectedContractId => TryGetSelectedContract(out var contract)
            ? contract.ContractId
            : string.Empty;

        public void Configure(
            NewGameStartFlowController startController,
            ShipDeviceInteractionState deviceState,
            FirstPersonPlayerInput firstPersonInput,
            PlanetMaintenanceController planetMaintenanceController,
            GameObject root,
            Text titleLabel,
            Text summaryLabel,
            Text contractListLabel,
            Text statusLabel,
            Button[] slotButtons,
            Button associationButton,
            Button privateButton,
            Button specialButton,
            Button previousButton,
            Button nextButton,
            Button acceptButton,
            Button startRunActionButton,
            Button backActionButton)
        {
            startFlowController = startController;
            shipDeviceState = deviceState;
            playerInput = firstPersonInput;
            maintenanceController = planetMaintenanceController;
            boardRoot = root;
            titleText = titleLabel;
            summaryText = summaryLabel;
            contractListText = contractListLabel;
            statusText = statusLabel;
            contractSlotButtons = slotButtons ?? new Button[0];
            associationContractButton = associationButton;
            privateContractButton = privateButton;
            specialContractButton = specialButton;
            previousContractButton = previousButton;
            nextContractButton = nextButton;
            acceptContractButton = acceptButton;
            startRunButton = startRunActionButton;
            backButton = backActionButton;
            DisableTextRaycasts();
            BindButtons();
            HideBoard();
        }

        public void ShowBoard()
        {
            if (startFlowController == null || boardRoot == null)
            {
                return;
            }

            startFlowController.PreparePostTransportContracts();
            lastStatus = string.Empty;
            boardRoot.SetActive(true);
            DisableTextRaycasts();
            SetCursorLockSuppressed(true);
            RefreshBoard();
        }

        public void HideBoard()
        {
            if (boardRoot != null)
            {
                boardRoot.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        public void ReturnToMaintenance()
        {
            HideBoard();
            if (maintenanceController != null)
            {
                maintenanceController.ShowMaintenance();
            }
        }

        public void RefreshBoard()
        {
            DisableTextRaycasts();
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            var context = BuildOfferContext(session);
            visibleContracts = DetailedContractCatalogRules.GetPostTutorialContractContents(catalog, context);
            EnsureSelectedContract();

            if (titleText != null)
            {
                titleText.text = "Contract Board";
            }

            if (summaryText != null)
            {
                summaryText.text = BuildSummaryText(session, context);
            }

            if (contractListText != null)
            {
                contractListText.text = BuildContractListText(session, context);
            }

            RefreshContractSlotButtons(session, context);
            SetButtonState(associationContractButton, CountByType(ContractType.Association) > 0);
            SetButtonState(privateContractButton, CountByType(ContractType.Private) > 0);
            SetButtonState(specialContractButton, CountByType(ContractType.Special) > 0);
            var selectedTypeCount = CountByType(selectedContractType);
            SetButtonState(previousContractButton, selectedTypeCount > 1);
            SetButtonState(nextContractButton, selectedTypeCount > 1);
            SetButtonState(
                acceptContractButton,
                TryGetSelectedContract(out var selectedContract) &&
                CanStartContract(session, context, selectedContract) &&
                !session.IsTransportContractPending(selectedContract.ContractId));
            SetButtonState(startRunButton, CanStartAcceptedContracts(session));
            SetButtonState(backButton, true);

            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(lastStatus)
                    ? BuildStatusText(session, context)
                    : lastStatus;
            }
        }

        public void SelectAssociationContract()
        {
            SelectCategory(ContractType.Association);
        }

        public void SelectPrivateContract()
        {
            SelectCategory(ContractType.Private);
        }

        public void SelectSpecialContract()
        {
            SelectCategory(ContractType.Special);
        }

        public void SelectPreviousContract()
        {
            MoveSelection(-1);
        }

        public void SelectNextContract()
        {
            MoveSelection(1);
        }

        public void AcceptSelectedContract()
        {
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            var context = BuildOfferContext(session);
            visibleContracts = DetailedContractCatalogRules.GetPostTutorialContractContents(catalog, context);
            EnsureSelectedContract();
            if (!TryGetSelectedContract(out var contract))
            {
                lastStatus = "Select a visible contract before accepting.";
                RefreshBoard();
                return;
            }

            if (!CanStartContract(session, context, contract))
            {
                lastStatus = "Repair and cargo hold readiness are required before accepting this contract.";
                RefreshBoard();
                return;
            }

            AcceptSelectedContractForPendingRun(session, context, contract);
        }

        public void StartAcceptedContractRun()
        {
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            if (!CanStartAcceptedContracts(session))
            {
                lastStatus = session.PendingTransportContractCount <= 0
                    ? "Accept at least one contract before starting a transport run."
                    : BuildReadinessFailureText(
                        ShipStateRules.EvaluateStartReadiness(session.Ship, session.PersonalCargoHold.HasCargo),
                        true);
                RefreshBoard();
                return;
            }

            var nextSession = session.StartAcceptedTransportContracts();
            startFlowController.ApplySessionState(nextSession);
            if (shipDeviceState != null && nextSession.ActiveCargo.HasValue)
            {
                shipDeviceState.SetShipState(nextSession.Ship);
                shipDeviceState.SetCargoState(nextSession.ActiveCargo.Value);
                shipDeviceState.SetEquipmentState(nextSession.Equipment);
                shipDeviceState.StartTransportRun(CalculateActiveRunDuration(nextSession));
                shipDeviceState.TryStartAsteroidFieldForCurrentRun(nextSession);
            }

            lastStatus = "Started transport with " + nextSession.ActiveTransportContractCount + " accepted contract(s).";
            HideBoard();
        }

        private void Awake()
        {
            BindButtons();
            HideBoard();
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
            if (associationContractButton != null)
            {
                associationContractButton.onClick.AddListener(SelectAssociationContract);
            }

            if (privateContractButton != null)
            {
                privateContractButton.onClick.AddListener(SelectPrivateContract);
            }

            if (specialContractButton != null)
            {
                specialContractButton.onClick.AddListener(SelectSpecialContract);
            }

            BindContractSlotButtons();

            if (previousContractButton != null)
            {
                previousContractButton.onClick.AddListener(SelectPreviousContract);
            }

            if (nextContractButton != null)
            {
                nextContractButton.onClick.AddListener(SelectNextContract);
            }

            if (acceptContractButton != null)
            {
                acceptContractButton.onClick.AddListener(AcceptSelectedContract);
            }

            if (startRunButton != null)
            {
                startRunButton.onClick.AddListener(StartAcceptedContractRun);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(ReturnToMaintenance);
            }
        }

        private void UnbindButtons()
        {
            if (associationContractButton != null)
            {
                associationContractButton.onClick.RemoveListener(SelectAssociationContract);
            }

            if (privateContractButton != null)
            {
                privateContractButton.onClick.RemoveListener(SelectPrivateContract);
            }

            if (specialContractButton != null)
            {
                specialContractButton.onClick.RemoveListener(SelectSpecialContract);
            }

            UnbindContractSlotButtons();

            if (previousContractButton != null)
            {
                previousContractButton.onClick.RemoveListener(SelectPreviousContract);
            }

            if (nextContractButton != null)
            {
                nextContractButton.onClick.RemoveListener(SelectNextContract);
            }

            if (acceptContractButton != null)
            {
                acceptContractButton.onClick.RemoveListener(AcceptSelectedContract);
            }

            if (startRunButton != null)
            {
                startRunButton.onClick.RemoveListener(StartAcceptedContractRun);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(ReturnToMaintenance);
            }
        }

        private void BindContractSlotButtons()
        {
            if (contractSlotButtons == null || contractSlotButtons.Length == 0)
            {
                contractSlotClickActions = new UnityAction[0];
                return;
            }

            contractSlotClickActions = new UnityAction[contractSlotButtons.Length];
            for (var i = 0; i < contractSlotButtons.Length; i++)
            {
                var button = contractSlotButtons[i];
                if (button == null)
                {
                    continue;
                }

                var slotIndex = i;
                contractSlotClickActions[i] = () => SelectContractSlot(slotIndex);
                button.onClick.AddListener(contractSlotClickActions[i]);
            }
        }

        private void UnbindContractSlotButtons()
        {
            if (contractSlotButtons == null ||
                contractSlotClickActions == null ||
                contractSlotClickActions.Length == 0)
            {
                return;
            }

            var count = Mathf.Min(contractSlotButtons.Length, contractSlotClickActions.Length);
            for (var i = 0; i < count; i++)
            {
                if (contractSlotButtons[i] != null && contractSlotClickActions[i] != null)
                {
                    contractSlotButtons[i].onClick.RemoveListener(contractSlotClickActions[i]);
                }
            }

            contractSlotClickActions = new UnityAction[0];
        }

        private void SelectCategory(ContractType contractType)
        {
            selectedContractType = contractType;
            selectedContractIndex = FindFirstContractIndex(contractType);
            lastStatus = selectedContractIndex >= 0
                ? "Selected " + contractType + " contract category. Press Accept to add the selected contract."
                : "No " + contractType + " contract is currently visible.";
            RefreshBoard();
        }

        private void SelectContractSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= visibleContracts.Length)
            {
                lastStatus = "Select a visible contract before accepting.";
                RefreshBoard();
                return;
            }

            selectedContractIndex = slotIndex;
            selectedContractType = visibleContracts[slotIndex].ContractType;
            lastStatus = "Selected " + visibleContracts[slotIndex].DisplayName + ". Press Accept to add this contract.";
            RefreshBoard();
        }

        private void MoveSelection(int direction)
        {
            if (direction == 0)
            {
                return;
            }

            var matchingIndices = GetContractIndices(selectedContractType);
            if (matchingIndices.Length == 0)
            {
                selectedContractIndex = -1;
                lastStatus = "No " + selectedContractType + " contract is currently visible.";
                RefreshBoard();
                return;
            }

            var currentPosition = 0;
            for (var i = 0; i < matchingIndices.Length; i++)
            {
                if (matchingIndices[i] == selectedContractIndex)
                {
                    currentPosition = i;
                    break;
                }
            }

            var nextPosition = (currentPosition + direction) % matchingIndices.Length;
            if (nextPosition < 0)
            {
                nextPosition += matchingIndices.Length;
            }

            selectedContractIndex = matchingIndices[nextPosition];
            lastStatus = "Selected contract changed. Press Accept to add the selected contract.";
            RefreshBoard();
        }

        private void AcceptSelectedContractForPendingRun(
            GameSessionState session,
            DetailedContractOfferContext context,
            ContractContentDefinition contract)
        {
            if (DetailedContractCatalogRules.RequiresForcedAssociationMembership(context, contract))
            {
                session = session.WithAssociationMembership(true);
            }

            var transportContract = DetailedContractCatalogRules.CreateTransportContract(contract, catalog, context);
            if (session.IsTransportContractPending(transportContract.Id))
            {
                lastStatus = transportContract.DisplayName + " is already accepted for the next run.";
                RefreshBoard();
                return;
            }

            var nextSession = session.AcceptTransportContract(transportContract);
            startFlowController.ApplySessionState(nextSession);
            lastStatus = "Accepted " + transportContract.DisplayName +
                         ". Accepted contracts: " + nextSession.PendingTransportContractCount +
                         ". Press Start Run when ready.";
            RefreshBoard();
        }

        private void EnsureSelectedContract()
        {
            if (IsSelectedContractValid())
            {
                return;
            }

            selectedContractIndex = FindFirstContractIndex(selectedContractType);
            if (selectedContractIndex >= 0)
            {
                return;
            }

            selectedContractIndex = visibleContracts.Length > 0 ? 0 : -1;
            if (selectedContractIndex >= 0)
            {
                selectedContractType = visibleContracts[selectedContractIndex].ContractType;
            }
        }

        private bool IsSelectedContractValid()
        {
            return selectedContractIndex >= 0 &&
                   selectedContractIndex < visibleContracts.Length &&
                   visibleContracts[selectedContractIndex].ContractType == selectedContractType;
        }

        private bool TryGetSelectedContract(out ContractContentDefinition contract)
        {
            if (selectedContractIndex >= 0 && selectedContractIndex < visibleContracts.Length)
            {
                contract = visibleContracts[selectedContractIndex];
                return true;
            }

            contract = default;
            return false;
        }

        private int FindFirstContractIndex(ContractType contractType)
        {
            for (var i = 0; i < visibleContracts.Length; i++)
            {
                if (visibleContracts[i].ContractType == contractType)
                {
                    return i;
                }
            }

            return -1;
        }

        private int[] GetContractIndices(ContractType contractType)
        {
            var count = CountByType(contractType);
            if (count == 0)
            {
                return new int[0];
            }

            var indices = new int[count];
            var writeIndex = 0;
            for (var i = 0; i < visibleContracts.Length; i++)
            {
                if (visibleContracts[i].ContractType == contractType)
                {
                    indices[writeIndex] = i;
                    writeIndex++;
                }
            }

            return indices;
        }

        private int CountByType(ContractType contractType)
        {
            var count = 0;
            for (var i = 0; i < visibleContracts.Length; i++)
            {
                if (visibleContracts[i].ContractType == contractType)
                {
                    count++;
                }
            }

            return count;
        }

        private void RefreshContractSlotButtons(
            GameSessionState session,
            DetailedContractOfferContext context)
        {
            if (contractSlotButtons == null)
            {
                return;
            }

            for (var i = 0; i < contractSlotButtons.Length; i++)
            {
                var button = contractSlotButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i >= visibleContracts.Length)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                button.gameObject.SetActive(true);
                button.interactable = true;
                SetContractSlotLabel(button, i, session, context);
                SetContractSlotVisual(button, i == selectedContractIndex);
            }
        }

        private void SetContractSlotLabel(
            Button button,
            int contractIndex,
            GameSessionState session,
            DetailedContractOfferContext context)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            var contract = visibleContracts[contractIndex];
            var route = DetailedContractCatalogRules.FindRoute(catalog, contract.RouteId);
            var cargo = DetailedContractCatalogRules.FindCargo(catalog, contract.CargoId);
            var reward = DetailedContractCatalogRules.CalculateReward(contract, route, cargo, context);
            label.text = (contractIndex == selectedContractIndex ? "Selected | " : string.Empty) +
                         contract.ContractType +
                         " | " +
                         contract.DisplayName +
                         " | " +
                         FormatMoney(reward.TotalPositiveCredits) +
                         " | " +
                         route.DurationSeconds +
                         "s | Cargo " +
                         contract.RequiredCargoHoldScore +
                         " | " +
                         (session.IsTransportContractPending(contract.ContractId)
                             ? "Accepted"
                             : CanStartContract(session, context, contract) ? "Ready" : "Locked");
        }

        private static void SetContractSlotVisual(Button button, bool selected)
        {
            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = selected
                    ? new Color(0.28f, 0.36f, 0.2f, 1f)
                    : new Color(0.12f, 0.18f, 0.22f, 1f);
            }

            var colors = button.colors;
            colors.normalColor = selected
                ? new Color(0.28f, 0.36f, 0.2f, 1f)
                : new Color(0.12f, 0.18f, 0.22f, 1f);
            colors.highlightedColor = selected
                ? new Color(0.34f, 0.44f, 0.25f, 1f)
                : new Color(0.18f, 0.27f, 0.32f, 1f);
            colors.pressedColor = selected
                ? new Color(0.18f, 0.25f, 0.12f, 1f)
                : new Color(0.08f, 0.13f, 0.16f, 1f);
            button.colors = colors;
        }

        private DetailedContractOfferContext BuildOfferContext(GameSessionState session)
        {
            var cargoHoldScore = Mathf.RoundToInt(ShipStateRules.CalculateCargoHoldScore(session.Ship) * 100f);
            var repairCost = session.Ship.IsTotalLoss
                ? ShipStateRules.CalculateTotalLossClaimCost(session.Ship)
                : ShipStateRules.CalculateRepairCost(session.Ship);
            return new DetailedContractOfferContext(
                session.IsAssociationMember,
                cargoHoldScore,
                session.Reputation.FameScore,
                session.Reputation.AssociationFameScore,
                session.CompletedTransportCount,
                repairCost,
                false,
                session.Reputation.HasUsedRevivalContract);
        }

        private bool CanStartContract(
            GameSessionState session,
            DetailedContractOfferContext context,
            ContractContentDefinition contract)
        {
            if (session == null || session.Phase != GameSessionPhase.Completed)
            {
                return false;
            }

            var readiness = ShipStateRules.EvaluateStartReadiness(session.Ship, session.PersonalCargoHold.HasCargo);
            return readiness.CanStartTransport &&
                   DetailedContractCatalogRules.CanAcceptContract(context, contract);
        }

        private string BuildSummaryText(GameSessionState session, DetailedContractOfferContext context)
        {
            return "Fame: " + session.Reputation.FameScore +
                   " | Association fame: " + session.Reputation.AssociationFameScore +
                   " | Association: " + (session.IsAssociationMember ? "Member" : "Non-member") +
                   "\nCargo hold score: " + context.CargoHoldScore +
                   " | Visible contracts: " + visibleContracts.Length +
                   " | Accepted: " + session.PendingTransportContractCount +
                   " | Selected: " + (TryGetSelectedContract(out var selected) ? selected.DisplayName : "None") +
                   " | Private contracts: " + (DetailedContractCatalogRules.ShouldShowPrivateContracts(context) ? "Visible" : "Hidden");
        }

        private string BuildContractListText(GameSessionState session, DetailedContractOfferContext context)
        {
            var builder = new StringBuilder();
            AppendContracts(builder, "Association", ContractType.Association, session, context);
            AppendContracts(builder, "Private", ContractType.Private, session, context);
            AppendContracts(builder, "Special", ContractType.Special, session, context);
            return builder.ToString();
        }

        private void AppendContracts(
            StringBuilder builder,
            string header,
            ContractType contractType,
            GameSessionState session,
            DetailedContractOfferContext context)
        {
            builder.AppendLine(header);
            var count = 0;
            for (var i = 0; i < visibleContracts.Length; i++)
            {
                var contract = visibleContracts[i];
                if (contract.ContractType != contractType)
                {
                    continue;
                }

                var route = DetailedContractCatalogRules.FindRoute(catalog, contract.RouteId);
                var cargo = DetailedContractCatalogRules.FindCargo(catalog, contract.CargoId);
                var reward = DetailedContractCatalogRules.CalculateReward(contract, route, cargo, context);
                builder.Append(i == selectedContractIndex ? " >> " : " - ");
                builder.Append(contract.DisplayName);
                builder.Append(" | Reward ");
                builder.Append(FormatMoney(reward.TotalPositiveCredits));
                builder.Append(" | Duration ");
                builder.Append(route.DurationSeconds);
                builder.Append("s | Required cargo score ");
                builder.Append(contract.RequiredCargoHoldScore);
                builder.Append(" | Difficulty ");
                builder.Append(contract.Difficulty);
                if (contract.IsRecoveryContract)
                {
                    builder.Append(" | Revival");
                }

                builder.Append(" | ");
                builder.Append(session.IsTransportContractPending(contract.ContractId)
                    ? "Accepted"
                    : CanStartContract(session, context, contract) ? "Ready" : "Needs repair");
                builder.AppendLine();
                count++;
            }

            if (count == 0)
            {
                builder.AppendLine(" - None visible");
            }
        }

        private static string BuildStatusText(GameSessionState session, DetailedContractOfferContext context)
        {
            var readiness = ShipStateRules.EvaluateStartReadiness(session.Ship, session.PersonalCargoHold.HasCargo);
            if (!readiness.CanStartTransport)
            {
                return BuildReadinessFailureText(readiness, false);
            }

            if (session.PendingTransportContractCount > 0)
            {
                return "Accepted contracts: " + session.PendingTransportContractCount + ". Press Start Run to begin the next transport.";
            }

            if (DetailedContractCatalogRules.ShouldShowRevivalContract(context))
            {
                return "Low fame revival contract is visible once for this game.";
            }

            if (!DetailedContractCatalogRules.ShouldShowPrivateContracts(context))
            {
                return "Private contracts are hidden because fame is below threshold.";
            }

            return "Select contracts from the list, press Accept to add them, then press Start Run.";
        }

        private void ProcessPointerClickFallback()
        {
            if (!IsBoardVisible ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickContractSlotAtScreenPosition(pointerPosition) ||
                TryClickButtonAtScreenPosition(associationContractButton, pointerPosition, SelectAssociationContract) ||
                TryClickButtonAtScreenPosition(privateContractButton, pointerPosition, SelectPrivateContract) ||
                TryClickButtonAtScreenPosition(specialContractButton, pointerPosition, SelectSpecialContract) ||
                TryClickButtonAtScreenPosition(previousContractButton, pointerPosition, SelectPreviousContract) ||
                TryClickButtonAtScreenPosition(nextContractButton, pointerPosition, SelectNextContract) ||
                TryClickButtonAtScreenPosition(acceptContractButton, pointerPosition, AcceptSelectedContract) ||
                TryClickButtonAtScreenPosition(startRunButton, pointerPosition, StartAcceptedContractRun) ||
                TryClickButtonAtScreenPosition(backButton, pointerPosition, ReturnToMaintenance))
            {
                return;
            }
        }

        private bool TryClickContractSlotAtScreenPosition(Vector2 screenPosition)
        {
            if (contractSlotButtons == null)
            {
                return false;
            }

            for (var i = 0; i < contractSlotButtons.Length; i++)
            {
                var slotIndex = i;
                if (TryClickButtonAtScreenPosition(
                        contractSlotButtons[i],
                        screenPosition,
                        () => SelectContractSlot(slotIndex)))
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

        private static void SetButtonState(Button button, bool interactable)
        {
            if (button != null)
            {
                button.gameObject.SetActive(true);
                button.interactable = interactable;
            }
        }

        private static bool CanStartAcceptedContracts(GameSessionState session)
        {
            if (session == null ||
                session.Phase != GameSessionPhase.Completed ||
                session.PendingTransportContractCount <= 0)
            {
                return false;
            }

            return ShipStateRules.EvaluateStartReadiness(session.Ship, session.PersonalCargoHold.HasCargo)
                .CanStartTransport;
        }

        private static string BuildReadinessFailureText(ShipStartAssessment readiness, bool forAcceptedRun)
        {
            var action = forAcceptedRun
                ? "starting the accepted run"
                : "accepting a board contract";

            if (readiness.IsPersonalCargoBlocked)
            {
                return "Personal cargo cannot launch with the current cargo hold damage. Sell it or repair before " +
                       action + ".";
            }

            if (readiness.IsCargoHoldBlocked)
            {
                return "Cargo hold repair is required before " + action + ".";
            }

            if (readiness.IsCockpitDestroyed)
            {
                return "Cockpit repair is required before " + action + ".";
            }

            if (readiness.IsEngineRoomDestroyed)
            {
                return "Engine room repair is required before " + action + ".";
            }

            return "Ship repair is required before " + action + ".";
        }

        private static int CalculateActiveRunDuration(GameSessionState session)
        {
            var duration = session.ActiveTransportContract.HasValue
                ? session.ActiveTransportContract.Value.DurationSeconds
                : 1;
            var contracts = session.ActiveTransportContracts;
            for (var i = 0; i < contracts.Length; i++)
            {
                duration = Mathf.Max(duration, contracts[i].DurationSeconds);
            }

            return duration;
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

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }

        private void DisableTextRaycasts()
        {
            SetTextNonBlocking(titleText);
            SetTextNonBlocking(summaryText);
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
