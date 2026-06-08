using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class TransportSettlementController : MonoBehaviour
    {
        private const int AssociationDefaultBasePay = 500;
        private const int AssociationDistancePayPerSecond = 5;
        private const int AssociationSupportBonus = 100;
        private const int DeadCrewLifeInsuranceCost = 300;
        private const float DefaultGameOverCutsceneSeconds = 2.5f;

        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private GameObject settlementRoot;
        [SerializeField] private Text settlementTitleText;
        [SerializeField] private Text settlementBodyText;
        [SerializeField] private Text settlementStatusText;
        [SerializeField] private Button continueToMaintenanceButton;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private PlanetStayController planetStayController;
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private RectTransform cargoShipVisual;
        [SerializeField] private RectTransform podVisual;
        [SerializeField] private Text gameOverTitleText;
        [SerializeField] private Text gameOverBodyText;
        [SerializeField] private float gameOverCutsceneSeconds = DefaultGameOverCutsceneSeconds;

        private bool settlementShownForCurrentTransport;
        private bool gameOverCutsceneActive;
        private float gameOverCutsceneElapsed;
        private bool hasObservedPhase;
        private GameSessionPhase lastObservedPhase;
        private int settlementShownCompletedTransportCount = -1;
        private SettlementResult lastSettlementResult;

        public GameObject SettlementRoot => settlementRoot;

        public Text SettlementBodyText => settlementBodyText;

        public Button ContinueToMaintenanceButton => continueToMaintenanceButton;

        public PlanetStayController PlanetStayController => planetStayController;

        public GameObject GameOverRoot => gameOverRoot;

        public RectTransform CargoShipVisual => cargoShipVisual;

        public RectTransform PodVisual => podVisual;

        public Text GameOverTitleText => gameOverTitleText;

        public bool IsSettlementVisible => settlementRoot != null && settlementRoot.activeSelf;

        public bool IsGameOverVisible => gameOverRoot != null && gameOverRoot.activeSelf;

        public bool IsGameOverCutsceneComplete =>
            IsGameOverVisible && gameOverCutsceneElapsed >= GetCutsceneDuration();

        public SettlementResult LastSettlementResult => lastSettlementResult;

        public bool ArrivalGateClosedForValidation => settlementShownForCurrentTransport;

        public int SettlementShownCompletedTransportCountForValidation => settlementShownCompletedTransportCount;

        public bool HasObservedPhaseForValidation => hasObservedPhase;

        public GameSessionPhase LastObservedPhaseForValidation => lastObservedPhase;

        public GameSessionState CurrentSession => startFlowController != null
            ? startFlowController.CurrentSession
            : null;

        public void Configure(
            NewGameStartFlowController startController,
            ShipDeviceInteractionState deviceState,
            FirstPersonPlayerInput firstPersonInput,
            GameObject settlementPanelRoot,
            Text settlementTitleLabel,
            Text settlementBodyLabel,
            Text settlementStatusLabel,
            GameObject gameOverPanelRoot,
            RectTransform cargoShipRect,
            RectTransform podRect,
            Text gameOverTitleLabel,
            Text gameOverBodyLabel)
        {
            startFlowController = startController;
            shipDeviceState = deviceState;
            playerInput = firstPersonInput;
            settlementRoot = settlementPanelRoot;
            settlementTitleText = settlementTitleLabel;
            settlementBodyText = settlementBodyLabel;
            settlementStatusText = settlementStatusLabel;
            gameOverRoot = gameOverPanelRoot;
            cargoShipVisual = cargoShipRect;
            podVisual = podRect;
            gameOverTitleText = gameOverTitleLabel;
            gameOverBodyText = gameOverBodyLabel;
            HideSettlement();
            HideGameOver();
        }

        public void ConfigureMaintenanceContinuation(
            PlanetMaintenanceController planetMaintenanceController,
            Button continueButton)
        {
            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.onClick.RemoveListener(ContinueToMaintenance);
                continueToMaintenanceButton.onClick.RemoveListener(ContinueToPlanet);
            }

            maintenanceController = planetMaintenanceController;
            planetStayController = null;
            continueToMaintenanceButton = continueButton;
            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.onClick.AddListener(ContinueToMaintenance);
                continueToMaintenanceButton.gameObject.SetActive(false);
            }
        }

        public void ConfigurePlanetContinuation(
            PlanetStayController planetController,
            Button continueButton)
        {
            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.onClick.RemoveListener(ContinueToMaintenance);
                continueToMaintenanceButton.onClick.RemoveListener(ContinueToPlanet);
            }

            planetStayController = planetController;
            if (planetController != null && planetController.MaintenanceController != null)
            {
                maintenanceController = planetController.MaintenanceController;
            }

            continueToMaintenanceButton = continueButton;
            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.onClick.AddListener(ContinueToPlanet);
                continueToMaintenanceButton.gameObject.SetActive(false);
            }
        }

        public void ProcessTransportArrival()
        {
            ObserveSessionPhase();
            if (startFlowController == null ||
                shipDeviceState == null ||
                !shipDeviceState.HasActiveTransportRun ||
                !shipDeviceState.CurrentTransportRun.IsComplete)
            {
                return;
            }

            var session = startFlowController.CurrentSession;
            if (session.Phase != GameSessionPhase.Transporting)
            {
                return;
            }

            var expectedCompletedTransportCount = session.CompletedTransportCount + 1;
            if (settlementShownForCurrentTransport &&
                settlementShownCompletedTransportCount == expectedCompletedTransportCount)
            {
                return;
            }

            CompleteCurrentTransport(BuildSettlementInput(session));
        }

        public void CompleteCurrentTransportForValidation(SettlementInput input)
        {
            CompleteCurrentTransport(input);
        }

        public void ResetArrivalGateForValidation()
        {
            settlementShownForCurrentTransport = false;
            settlementShownCompletedTransportCount = -1;
            HideSettlement();
        }

        public void AdvanceGameOverCutsceneForValidation(float deltaSeconds)
        {
            UpdateGameOverCutscene(deltaSeconds);
        }

        public bool ProcessContinueButtonClickForValidation(Vector2 screenPosition)
        {
            return TryContinueToMaintenanceAtScreenPosition(screenPosition);
        }

        public void ContinueToMaintenance()
        {
            if (maintenanceController == null ||
                CurrentSession == null ||
                CurrentSession.Phase != GameSessionPhase.Completed)
            {
                return;
            }

            HideSettlement();
            maintenanceController.ShowMaintenance();
        }

        public void ContinueToPlanet()
        {
            if (planetStayController == null ||
                CurrentSession == null ||
                CurrentSession.Phase != GameSessionPhase.Completed)
            {
                return;
            }

            HideSettlement();
            planetStayController.ShowPlanet();
        }

        private void Awake()
        {
            BindMaintenanceContinuation();
            HideSettlement();
            HideGameOver();
        }

        private void OnEnable()
        {
            BindMaintenanceContinuation();
        }

        private void Update()
        {
            ProcessTransportArrival();
            ProcessContinueButtonPointerClick();
            UpdateGameOverCutscene(Time.deltaTime);
        }

        private void OnDisable()
        {
            SetCursorLockSuppressed(false);
            SetPlayerInputSuppressed(false);
        }

        private void OnDestroy()
        {
            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.onClick.RemoveListener(ContinueToMaintenance);
                continueToMaintenanceButton.onClick.RemoveListener(ContinueToPlanet);
            }

            SetCursorLockSuppressed(false);
            SetPlayerInputSuppressed(false);
        }

        private void BindMaintenanceContinuation()
        {
            if (continueToMaintenanceButton == null)
            {
                return;
            }

            continueToMaintenanceButton.onClick.RemoveListener(ContinueToMaintenance);
            continueToMaintenanceButton.onClick.RemoveListener(ContinueToPlanet);
            if (planetStayController != null)
            {
                continueToMaintenanceButton.onClick.AddListener(ContinueToPlanet);
                return;
            }

            continueToMaintenanceButton.onClick.AddListener(ContinueToMaintenance);
        }

        private void ObserveSessionPhase()
        {
            if (startFlowController == null)
            {
                return;
            }

            var phase = startFlowController.CurrentSession.Phase;
            if (phase == GameSessionPhase.Transporting &&
                (!hasObservedPhase || lastObservedPhase != GameSessionPhase.Transporting))
            {
                settlementShownForCurrentTransport = false;
                HideSettlement();
            }

            hasObservedPhase = true;
            lastObservedPhase = phase;
        }

        private void CompleteCurrentTransport(SettlementInput input)
        {
            if (startFlowController == null)
            {
                return;
            }

            var session = startFlowController.CurrentSession;
            if (session.Phase != GameSessionPhase.Transporting)
            {
                return;
            }

            var completedSession = session.CompleteTransport(input);
            startFlowController.ApplySessionState(completedSession);
            lastSettlementResult = completedSession.SettlementResult;
            settlementShownForCurrentTransport = true;
            settlementShownCompletedTransportCount = completedSession.CompletedTransportCount;
            hasObservedPhase = true;
            lastObservedPhase = completedSession.Phase;
            ShowSettlement(completedSession);

            if (completedSession.Phase == GameSessionPhase.GameOver)
            {
                BeginGameOverCutscene(completedSession);
            }
        }

        private SettlementInput BuildSettlementInput(GameSessionState session)
        {
            var ship = shipDeviceState != null ? shipDeviceState.CurrentShipState : session.Ship;
            var cargo = shipDeviceState != null
                ? shipDeviceState.CurrentCargoState
                : session.ActiveCargo.GetValueOrDefault(new CargoState(CargoGrade.Common, 1, 0, 1f, false));
            var hasContract = session.ActiveTransportContract.HasValue;
            var contract = hasContract ? session.ActiveTransportContract.Value : default;
            var contractType = hasContract ? contract.ContractType : ContractType.Association;
            var difficulty = hasContract ? contract.Difficulty : ContractDifficulty.Normal;
            var completedTransportNumber = session.CompletedTransportCount + 1;
            var isAssociationContract = session.IsAssociationMember &&
                                        (contractType == ContractType.Association ||
                                         HasActiveContractType(session, ContractType.Association));
            var fixedRewardTotal = CalculateActiveContractRewardCredits(session);
            var hasFixedReward = fixedRewardTotal > 0;
            var repairCost = ship.IsTotalLoss
                ? ShipStateRules.CalculateTotalLossClaimCost(ship)
                : ShipStateRules.CalculateRepairCost(ship);

            return new SettlementInput(
                contractType,
                difficulty,
                cargo,
                ship,
                new CrewState(1, 0),
                session.Wallet,
                repairCost: repairCost,
                towingCost: ShipStateRules.CalculateTowingCost(session.TowingIncidentCount + 1),
                revivalCostPerDeadCrew: DeadCrewLifeInsuranceCost,
                contractBasePay: hasFixedReward
                    ? fixedRewardTotal
                    : isAssociationContract ? AssociationDefaultBasePay : 0,
                distancePay: isAssociationContract && hasContract && !hasFixedReward
                    ? contract.DurationSeconds * AssociationDistancePayPerSecond
                    : 0,
                repairSupportAmount: isAssociationContract ? AssociationSupportBonus : 0,
                safeStreakBonus: isAssociationContract
                    ? SettlementCalculator.CalculateAssociationSafeStreakBonus(completedTransportNumber)
                    : 0,
                shipLossInsurancePayout: ShipStateRules.CalculateShipLossInsurancePayout(ship),
                associationMaintenanceFee: session.IsAssociationMember &&
                                           completedTransportNumber >= DetailedContractCatalogRules.AssociationMaintenanceStartsAtTransport
                    ? DetailedContractCatalogRules.AssociationMaintenanceFeeCredits
                    : 0);
        }

        private void ShowSettlement(GameSessionState session)
        {
            if (settlementRoot != null)
            {
                settlementRoot.SetActive(true);
            }

            if (settlementTitleText != null)
            {
                settlementTitleText.text = "Arrival Settlement";
            }

            if (settlementBodyText != null)
            {
                settlementBodyText.text = BuildSettlementBody(session);
            }

            if (settlementStatusText != null)
            {
                settlementStatusText.text = BuildSettlementStatus(session.SettlementResult);
            }

            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.gameObject.SetActive(session.Phase == GameSessionPhase.Completed);
                continueToMaintenanceButton.interactable = session.Phase == GameSessionPhase.Completed;
            }

            if (session.Phase == GameSessionPhase.Completed)
            {
                SetCursorLockSuppressed(true);
            }
        }

        private void HideSettlement()
        {
            if (settlementRoot != null)
            {
                settlementRoot.SetActive(false);
            }

            if (continueToMaintenanceButton != null)
            {
                continueToMaintenanceButton.gameObject.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        private void BeginGameOverCutscene(GameSessionState session)
        {
            HideSettlement();
            if (gameOverRoot != null)
            {
                gameOverRoot.SetActive(true);
            }

            gameOverCutsceneActive = true;
            gameOverCutsceneElapsed = 0f;
            SetPlayerInputSuppressed(true);
            UpdateGameOverText(session, false);
            UpdateGameOverVisuals();
        }

        private void HideGameOver()
        {
            if (gameOverRoot != null)
            {
                gameOverRoot.SetActive(false);
            }

            gameOverCutsceneActive = false;
            gameOverCutsceneElapsed = 0f;
        }

        private void UpdateGameOverCutscene(float deltaSeconds)
        {
            if (!gameOverCutsceneActive || deltaSeconds <= 0f)
            {
                return;
            }

            gameOverCutsceneElapsed = Mathf.Min(
                GetCutsceneDuration(),
                gameOverCutsceneElapsed + deltaSeconds);
            UpdateGameOverVisuals();
            if (gameOverCutsceneElapsed >= GetCutsceneDuration())
            {
                gameOverCutsceneActive = false;
                UpdateGameOverText(CurrentSession, true);
            }
        }

        private void UpdateGameOverVisuals()
        {
            if (cargoShipVisual != null)
            {
                cargoShipVisual.anchoredPosition = new Vector2(-170f, 25f);
            }

            if (podVisual == null)
            {
                return;
            }

            var t = Mathf.Clamp01(gameOverCutsceneElapsed / GetCutsceneDuration());
            podVisual.anchoredPosition = Vector2.Lerp(new Vector2(-55f, 6f), new Vector2(360f, -230f), t);
            podVisual.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.62f, 0.62f, 1f), t);
        }

        private void UpdateGameOverText(GameSessionState session, bool complete)
        {
            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = complete ? "GAME OVER" : "Debt Claim Finalized";
            }

            if (gameOverBodyText == null || session == null)
            {
                return;
            }

            gameOverBodyText.text = complete
                ? "The pod has been discarded from the cargo ship.\nFinal balance: " + FormatMoney(session.Wallet.Credits)
                : "Unpaid debt remained after the next settlement.\nThe cargo ship is ejecting the pod.";
        }

        private string BuildSettlementBody(GameSessionState session)
        {
            var result = session.SettlementResult;
            var builder = new StringBuilder();
            builder.AppendLine("Transport count: " + session.CompletedTransportCount);
            if (session.ActiveTransportContractCount > 1)
            {
                builder.AppendLine("Completed contracts: " + session.ActiveTransportContractCount);
            }

            builder.AppendLine("Gross revenue: " + FormatMoney(result.GrossRevenue));
            builder.AppendLine("Expenses: " + FormatMoney(result.Expenses));
            builder.AppendLine("Net change: " + FormatSignedMoney(result.NetChange));
            builder.AppendLine("Final balance: " + FormatSignedMoney(result.FinalBalance));
            if (result.PendingRepairCost > 0)
            {
                builder.AppendLine("Repair charge due at maintenance: " + FormatMoney(result.PendingRepairCost));
            }

            builder.AppendLine();
            for (var i = 0; i < result.LineItems.Length; i++)
            {
                var item = result.LineItems[i];
                builder.AppendLine(item.AffectsBalance
                    ? item.Label + ": " + FormatSignedMoney(item.Amount)
                    : item.Label + ": " + FormatSignedMoney(item.Amount) + " (charged at maintenance)");
            }

            return builder.ToString();
        }

        private static string BuildSettlementStatus(SettlementResult result)
        {
            switch (result.DebtStatus)
            {
                case SettlementDebtStatus.GraceActive:
                    return "Debt grace active: next transport may proceed.";
                case SettlementDebtStatus.FinalGameOver:
                    return "Debt remained after the next settlement.";
                default:
                    return "Settlement clear.";
            }
        }

        private static int CalculateActiveContractRewardCredits(GameSessionState session)
        {
            var contracts = session.ActiveTransportContracts;
            if (contracts.Length == 0)
            {
                return session.ActiveTransportContract.HasValue
                    ? session.ActiveTransportContract.Value.RewardCredits
                    : 0;
            }

            var total = 0;
            for (var i = 0; i < contracts.Length; i++)
            {
                total += contracts[i].RewardCredits;
            }

            return total;
        }

        private static bool HasActiveContractType(GameSessionState session, ContractType contractType)
        {
            var contracts = session.ActiveTransportContracts;
            for (var i = 0; i < contracts.Length; i++)
            {
                if (contracts[i].ContractType == contractType)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetCutsceneDuration()
        {
            return gameOverCutsceneSeconds <= 0f
                ? DefaultGameOverCutsceneSeconds
                : gameOverCutsceneSeconds;
        }

        private void SetPlayerInputSuppressed(bool suppressed)
        {
            if (playerInput == null)
            {
                playerInput = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }

            if (playerInput != null)
            {
                playerInput.SetGameplayInputSuppressed(suppressed);
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

        private void ProcessContinueButtonPointerClick()
        {
            if (continueToMaintenanceButton == null ||
                (maintenanceController == null && planetStayController == null) ||
                !continueToMaintenanceButton.gameObject.activeInHierarchy ||
                !continueToMaintenanceButton.interactable ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var rectTransform = continueToMaintenanceButton.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            TryContinueToMaintenanceAtScreenPosition(Mouse.current.position.ReadValue());
        }

        private bool TryContinueToMaintenanceAtScreenPosition(Vector2 screenPosition)
        {
            if (continueToMaintenanceButton == null ||
                (maintenanceController == null && planetStayController == null) ||
                !continueToMaintenanceButton.gameObject.activeInHierarchy ||
                !continueToMaintenanceButton.interactable)
            {
                return false;
            }

            var rectTransform = continueToMaintenanceButton.GetComponent<RectTransform>();
            if (rectTransform == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null))
            {
                return false;
            }

            if (planetStayController != null)
            {
                ContinueToPlanet();
            }
            else
            {
                ContinueToMaintenance();
            }

            return true;
        }

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }

        private static string FormatSignedMoney(int value)
        {
            return value >= 0 ? "+$" + value : "-$" + -value;
        }
    }
}
