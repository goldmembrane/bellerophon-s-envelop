using System;

namespace Bellerophon.Core.Session
{
    public enum NewGameStartFlowPhase
    {
        ContractPrompt,
        AssociationPlanet,
        PrivateBusinessPlanet,
        TutorialContractAccepted
    }

    public readonly struct StartingLoadoutState
    {
        public StartingLoadoutState(
            bool hasDefaultCargoShip,
            bool hasBasicProtectiveSuit,
            int stickCount)
        {
            if (stickCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stickCount), "Stick count cannot be negative.");
            }

            HasDefaultCargoShip = hasDefaultCargoShip;
            HasBasicProtectiveSuit = hasBasicProtectiveSuit;
            StickCount = stickCount;
        }

        public bool HasDefaultCargoShip { get; }

        public bool HasBasicProtectiveSuit { get; }

        public int StickCount { get; }

        public static StartingLoadoutState Empty => new StartingLoadoutState(false, false, 0);

        public static StartingLoadoutState CreateDefaultAssociationIssue()
        {
            return new StartingLoadoutState(true, true, 1);
        }
    }

    public readonly struct PlanetStartState
    {
        public PlanetStartState(string displayName, bool hasAssociationLogoSign)
        {
            DisplayName = displayName ?? string.Empty;
            HasAssociationLogoSign = hasAssociationLogoSign;
        }

        public string DisplayName { get; }

        public bool HasAssociationLogoSign { get; }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(DisplayName);

        public static PlanetStartState None => new PlanetStartState(string.Empty, false);

        public static PlanetStartState CreateAssociationLogoStart()
        {
            return new PlanetStartState("Association Start Planet", true);
        }

        public static PlanetStartState CreatePrivateBusinessStart()
        {
            return new PlanetStartState("Private Business Start Planet", false);
        }
    }

    public readonly struct AssociationContractScrollState
    {
        public const float AutoScrollDurationSeconds = 60f;
        public const float DownArrowFastMoveSeconds = 3f;

        private readonly float progress01;

        public AssociationContractScrollState(float progress01, bool isStopped)
        {
            this.progress01 = Clamp01(progress01);
            IsStopped = isStopped;
        }

        public float Progress01 => progress01;

        public int ProgressPercent => (int)Math.Round(progress01 * 100f);

        public bool IsStopped { get; }

        public bool HasReachedBottom => progress01 >= 0.999f;

        public bool TentativeConsentLocked => HasReachedBottom;

        public bool CanStartPrivateBusinessRoute => IsStopped && !HasReachedBottom;

        public static AssociationContractScrollState CreateInitial()
        {
            return new AssociationContractScrollState(0f, false);
        }

        public AssociationContractScrollState TickAuto(float deltaSeconds)
        {
            return Advance(deltaSeconds, AutoScrollDurationSeconds);
        }

        public AssociationContractScrollState TickDownArrowFastMove(float deltaSeconds)
        {
            return Advance(deltaSeconds, DownArrowFastMoveSeconds);
        }

        public AssociationContractScrollState StopScroll()
        {
            return new AssociationContractScrollState(progress01, true);
        }

        public AssociationContractScrollState MoveToBottom()
        {
            return new AssociationContractScrollState(1f, IsStopped);
        }

        private AssociationContractScrollState Advance(float deltaSeconds, float fullDurationSeconds)
        {
            if (IsStopped || HasReachedBottom || deltaSeconds <= 0f)
            {
                return this;
            }

            if (fullDurationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fullDurationSeconds), "Scroll duration must be positive.");
            }

            return new AssociationContractScrollState(
                progress01 + (deltaSeconds / fullDurationSeconds),
                false);
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }

    public readonly struct NewGameStartFlowActionResult
    {
        public NewGameStartFlowActionResult(
            NewGameStartFlowState state,
            bool succeeded,
            bool blocked,
            string summary)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Succeeded = succeeded;
            Blocked = blocked;
            Summary = summary ?? string.Empty;
        }

        public NewGameStartFlowState State { get; }

        public bool Succeeded { get; }

        public bool Blocked { get; }

        public string Summary { get; }
    }

    public readonly struct TransportContractDefinition
    {
        public TransportContractDefinition(
            string id,
            string displayName,
            string transportTargetName,
            ContractType contractType,
            ContractDifficulty difficulty,
            int durationSeconds,
            int rewardCredits,
            CargoState cargo,
            bool isTutorial,
            int requiredCargoHoldScore = 0,
            bool isRevivalContract = false,
            PlanetTrait originTrait = PlanetTrait.CommonMineralRich,
            PlanetTrait destinationTrait = PlanetTrait.WaterRich)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Contract id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Contract display name is required.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(transportTargetName))
            {
                throw new ArgumentException("Transport target name is required.", nameof(transportTargetName));
            }

            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Contract duration must be positive.");
            }

            if (rewardCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardCredits), "Contract reward cannot be negative.");
            }

            if (requiredCargoHoldScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCargoHoldScore), "Required cargo hold score cannot be negative.");
            }

            Id = id;
            DisplayName = displayName;
            TransportTargetName = transportTargetName;
            ContractType = contractType;
            Difficulty = difficulty;
            DurationSeconds = durationSeconds;
            RewardCredits = rewardCredits;
            Cargo = cargo;
            IsTutorial = isTutorial;
            RequiredCargoHoldScore = requiredCargoHoldScore;
            IsRevivalContract = isRevivalContract;
            OriginTrait = originTrait;
            DestinationTrait = destinationTrait;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string TransportTargetName { get; }

        public ContractType ContractType { get; }

        public ContractDifficulty Difficulty { get; }

        public int DurationSeconds { get; }

        public int RewardCredits { get; }

        public CargoState Cargo { get; }

        public bool IsTutorial { get; }

        public int RequiredCargoHoldScore { get; }

        public bool IsRevivalContract { get; }

        public PlanetTrait OriginTrait { get; }

        public PlanetTrait DestinationTrait { get; }

        public static TransportContractDefinition CreateTutorial()
        {
            return DetailedContractCatalogRules.CreateTutorialContract();
        }

        public static TransportContractDefinition CreateAssociationFollowUp()
        {
            return DetailedContractCatalogRules.CreateAssociationFollowUpContract();
        }

        public static TransportContractDefinition CreatePrivateFollowUp()
        {
            return DetailedContractCatalogRules.CreatePrivateFollowUpContract();
        }

        public static TransportContractDefinition[] CreatePostTutorialContracts()
        {
            return DetailedContractCatalogRules.CreatePostTutorialContracts();
        }
    }

    public sealed class NewGameStartFlowState
    {
        private readonly TransportContractDefinition[] availableContracts;

        public const int TutorialSkipRepairSupportCredits = 100;

        private NewGameStartFlowState(
            NewGameStartFlowPhase phase,
            GameSessionState session,
            TransportContractDefinition[] availableContracts,
            AssociationContractScrollState associationContractScroll,
            bool hasCompletedTutorialBefore,
            bool tutorialSkipped)
        {
            Phase = phase;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.availableContracts = availableContracts ?? new TransportContractDefinition[0];
            AssociationContractScroll = associationContractScroll;
            HasCompletedTutorialBefore = hasCompletedTutorialBefore;
            TutorialSkipped = tutorialSkipped;
        }

        public NewGameStartFlowPhase Phase { get; }

        public GameSessionState Session { get; }

        public int AvailableContractCount => availableContracts.Length;

        public AssociationContractScrollState AssociationContractScroll { get; }

        public bool HasCompletedTutorialBefore { get; }

        public bool TutorialSkipped { get; }

        public bool CanAcceptAssociationContract =>
            Phase == NewGameStartFlowPhase.ContractPrompt &&
            AssociationContractScroll.HasReachedBottom;

        public bool IsAssociationNoBlocked =>
            Phase == NewGameStartFlowPhase.ContractPrompt &&
            AssociationContractScroll.TentativeConsentLocked;

        public bool CanStartPrivateBusinessRoute =>
            Phase == NewGameStartFlowPhase.ContractPrompt &&
            AssociationContractScroll.CanStartPrivateBusinessRoute;

        public bool CanSkipTutorial =>
            Phase == NewGameStartFlowPhase.AssociationPlanet &&
            HasCompletedTutorialBefore &&
            AvailableContractCount == 1 &&
            GetAvailableContract(0).IsTutorial;

        public static int TutorialSkipRewardCredits =>
            TransportContractDefinition.CreateTutorial().RewardCredits + TutorialSkipRepairSupportCredits;

        public static NewGameStartFlowState CreateNewGame()
        {
            return CreateNewGame(false);
        }

        public static NewGameStartFlowState CreateNewGame(bool hasCompletedTutorialBefore)
        {
            return new NewGameStartFlowState(
                NewGameStartFlowPhase.ContractPrompt,
                GameSessionState.StartSession(new WalletState(0, false)),
                new TransportContractDefinition[0],
                AssociationContractScrollState.CreateInitial(),
                hasCompletedTutorialBefore,
                false);
        }

        public static NewGameStartFlowState CreateReturningPlayerNewGame()
        {
            return CreateNewGame(true);
        }

        public static NewGameStartFlowState Restore(
            NewGameStartFlowPhase phase,
            GameSessionState session,
            TransportContractDefinition[] availableContracts,
            AssociationContractScrollState associationContractScroll,
            bool hasCompletedTutorialBefore,
            bool tutorialSkipped)
        {
            return new NewGameStartFlowState(
                phase,
                session,
                availableContracts,
                associationContractScroll,
                hasCompletedTutorialBefore,
                tutorialSkipped);
        }

        public NewGameStartFlowState TickAssociationContractScroll(float deltaSeconds)
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            return WithAssociationContractScroll(AssociationContractScroll.TickAuto(deltaSeconds));
        }

        public NewGameStartFlowState TickAssociationContractDownArrowFastMove(float deltaSeconds)
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            return WithAssociationContractScroll(AssociationContractScroll.TickDownArrowFastMove(deltaSeconds));
        }

        public NewGameStartFlowState StopAssociationContractScroll()
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            return WithAssociationContractScroll(AssociationContractScroll.StopScroll());
        }

        public NewGameStartFlowState MoveAssociationContractToBottom()
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            return WithAssociationContractScroll(AssociationContractScroll.MoveToBottom());
        }

        public NewGameStartFlowActionResult RejectAssociationContract()
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            if (AssociationContractScroll.TentativeConsentLocked)
            {
                return new NewGameStartFlowActionResult(
                    this,
                    false,
                    true,
                    "이미 잠정적으로 동의한 상태입니다");
            }

            return new NewGameStartFlowActionResult(
                this,
                false,
                true,
                "계약서가 아직 끝까지 내려가지 않았습니다. Ctrl+C 후 Ctrl+X로만 취소할 수 있습니다.");
        }

        public NewGameStartFlowActionResult StartPrivateBusinessRouteFromStoppedContract()
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            if (!AssociationContractScroll.CanStartPrivateBusinessRoute)
            {
                return new NewGameStartFlowActionResult(
                    this,
                    false,
                    true,
                    "Ctrl+C로 계약서 스크롤을 먼저 멈춘 뒤 Ctrl+X를 눌러야 합니다.");
            }

            var next = new NewGameStartFlowState(
                NewGameStartFlowPhase.PrivateBusinessPlanet,
                GameSessionState.StartSession(new WalletState(0, false)),
                new TransportContractDefinition[0],
                AssociationContractScroll,
                HasCompletedTutorialBefore,
                false);
            return new NewGameStartFlowActionResult(
                next,
                true,
                false,
                "Association contract cancelled. Private business route started.");
        }

        public NewGameStartFlowState AcceptAssociationContract()
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            if (!AssociationContractScroll.HasReachedBottom)
            {
                throw new InvalidOperationException("Association contract can only be accepted after the scroll reaches the bottom.");
            }

            return new NewGameStartFlowState(
                NewGameStartFlowPhase.AssociationPlanet,
                GameSessionState.StartAssociationSession(),
                new[] { TransportContractDefinition.CreateTutorial() },
                AssociationContractScroll.MoveToBottom(),
                HasCompletedTutorialBefore,
                false);
        }

        public NewGameStartFlowState AcceptTutorialContract()
        {
            RequirePhase(NewGameStartFlowPhase.AssociationPlanet);
            var tutorial = GetAvailableContract(0);
            if (!tutorial.IsTutorial)
            {
                throw new InvalidOperationException("The first new game contract must be the tutorial contract.");
            }

            return new NewGameStartFlowState(
                NewGameStartFlowPhase.TutorialContractAccepted,
                Session.StartTransport(tutorial),
                availableContracts,
                AssociationContractScroll,
                HasCompletedTutorialBefore,
                false);
        }

        public NewGameStartFlowState SkipTutorialForReturningPlayer()
        {
            if (!CanSkipTutorial)
            {
                throw new InvalidOperationException("Tutorial skip is only available after a previous tutorial completion.");
            }

            return new NewGameStartFlowState(
                NewGameStartFlowPhase.AssociationPlanet,
                Session.GrantTutorialSkipReward(TutorialSkipRewardCredits),
                TransportContractDefinition.CreatePostTutorialContracts(),
                AssociationContractScroll,
                HasCompletedTutorialBefore,
                true);
        }

        public NewGameStartFlowState WithSession(GameSessionState session)
        {
            return new NewGameStartFlowState(
                Phase,
                session,
                availableContracts,
                AssociationContractScroll,
                HasCompletedTutorialBefore,
                TutorialSkipped);
        }

        public NewGameStartFlowState WithTutorialCompletedBefore(bool hasCompletedTutorialBefore)
        {
            return new NewGameStartFlowState(
                Phase,
                Session,
                availableContracts,
                AssociationContractScroll,
                hasCompletedTutorialBefore,
                TutorialSkipped);
        }

        public NewGameStartFlowState PreparePostTransportContracts()
        {
            if (Session.CompletedTransportCount <= 0)
            {
                return this;
            }

            return new NewGameStartFlowState(
                Phase,
                Session,
                TransportContractDefinition.CreatePostTutorialContracts(),
                AssociationContractScroll,
                HasCompletedTutorialBefore,
                TutorialSkipped);
        }

        public TransportContractDefinition GetAvailableContract(int index)
        {
            if (index < 0 || index >= availableContracts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return availableContracts[index];
        }

        private NewGameStartFlowState WithAssociationContractScroll(AssociationContractScrollState scroll)
        {
            return new NewGameStartFlowState(
                Phase,
                Session,
                availableContracts,
                scroll,
                HasCompletedTutorialBefore,
                TutorialSkipped);
        }

        private void RequirePhase(NewGameStartFlowPhase expected)
        {
            if (Phase != expected)
            {
                throw new InvalidOperationException($"Expected start flow phase {expected}, but current phase is {Phase}.");
            }
        }
    }
}
