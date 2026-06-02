using System;

namespace Bellerophon.Core.Session
{
    public enum NewGameStartFlowPhase
    {
        ContractPrompt,
        AssociationPlanet,
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
            CargoState cargo,
            bool isTutorial)
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

            Id = id;
            DisplayName = displayName;
            TransportTargetName = transportTargetName;
            ContractType = contractType;
            Difficulty = difficulty;
            DurationSeconds = durationSeconds;
            Cargo = cargo;
            IsTutorial = isTutorial;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public string TransportTargetName { get; }

        public ContractType ContractType { get; }

        public ContractDifficulty Difficulty { get; }

        public int DurationSeconds { get; }

        public CargoState Cargo { get; }

        public bool IsTutorial { get; }

        public static TransportContractDefinition CreateTutorial()
        {
            return new TransportContractDefinition(
                "association-tutorial-001",
                "Tutorial Delivery",
                "Cargo Hold Center Cargo",
                ContractType.Association,
                ContractDifficulty.Intro,
                60,
                new CargoState(CargoGrade.Common, 50, 100, 1f, false),
                true);
        }
    }

    public sealed class NewGameStartFlowState
    {
        private readonly TransportContractDefinition[] availableContracts;

        private NewGameStartFlowState(
            NewGameStartFlowPhase phase,
            GameSessionState session,
            TransportContractDefinition[] availableContracts)
        {
            Phase = phase;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.availableContracts = availableContracts ?? new TransportContractDefinition[0];
        }

        public NewGameStartFlowPhase Phase { get; }

        public GameSessionState Session { get; }

        public int AvailableContractCount => availableContracts.Length;

        public static NewGameStartFlowState CreateNewGame()
        {
            return new NewGameStartFlowState(
                NewGameStartFlowPhase.ContractPrompt,
                GameSessionState.StartSession(new WalletState(0, false)),
                new TransportContractDefinition[0]);
        }

        public NewGameStartFlowState AcceptAssociationContract()
        {
            RequirePhase(NewGameStartFlowPhase.ContractPrompt);
            return new NewGameStartFlowState(
                NewGameStartFlowPhase.AssociationPlanet,
                GameSessionState.StartAssociationSession(),
                new[] { TransportContractDefinition.CreateTutorial() });
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
                availableContracts);
        }

        public TransportContractDefinition GetAvailableContract(int index)
        {
            if (index < 0 || index >= availableContracts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return availableContracts[index];
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
