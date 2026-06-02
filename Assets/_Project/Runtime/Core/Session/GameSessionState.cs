using System;

namespace Bellerophon.Core.Session
{
    public enum GameSessionPhase
    {
        Ready,
        Transporting,
        Completed,
        Failed
    }

    public sealed class GameSessionState
    {
        private GameSessionState(
            GameSessionPhase phase,
            ShipState ship,
            WalletState wallet,
            SettlementResult settlementResult,
            bool isAssociationMember,
            PlanetStartState currentPlanet,
            StartingLoadoutState startingLoadout,
            TransportContractDefinition? activeTransportContract,
            CargoState? activeCargo)
        {
            Phase = phase;
            Ship = ship;
            Wallet = wallet;
            SettlementResult = settlementResult;
            IsAssociationMember = isAssociationMember;
            CurrentPlanet = currentPlanet;
            StartingLoadout = startingLoadout;
            ActiveTransportContract = activeTransportContract;
            ActiveCargo = activeCargo;
        }

        public GameSessionPhase Phase { get; }

        public ShipState Ship { get; }

        public WalletState Wallet { get; }

        public SettlementResult SettlementResult { get; }

        public bool IsAssociationMember { get; }

        public PlanetStartState CurrentPlanet { get; }

        public StartingLoadoutState StartingLoadout { get; }

        public TransportContractDefinition? ActiveTransportContract { get; }

        public CargoState? ActiveCargo { get; }

        public bool HasActiveCargo => ActiveCargo.HasValue;

        public static GameSessionState StartSession(WalletState wallet)
        {
            return new GameSessionState(
                GameSessionPhase.Ready,
                ShipState.CreateDefault(),
                wallet,
                default,
                false,
                PlanetStartState.None,
                StartingLoadoutState.Empty,
                null,
                null);
        }

        public static GameSessionState StartAssociationSession()
        {
            return StartSession(new WalletState(0, false))
                .WithAssociationStart(
                    PlanetStartState.CreateAssociationLogoStart(),
                    StartingLoadoutState.CreateDefaultAssociationIssue());
        }

        public GameSessionState WithAssociationStart(PlanetStartState planet, StartingLoadoutState loadout)
        {
            RequirePhase(GameSessionPhase.Ready);
            if (!planet.IsConfigured)
            {
                throw new ArgumentException("Association start planet must be configured.", nameof(planet));
            }

            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                true,
                planet,
                loadout,
                ActiveTransportContract,
                ActiveCargo);
        }

        public GameSessionState StartTransport()
        {
            return StartTransportCore(null, null);
        }

        public GameSessionState StartTransport(TransportContractDefinition contract)
        {
            return StartTransportCore(contract, contract.Cargo);
        }

        public GameSessionState CompleteTransport(SettlementInput settlementInput)
        {
            RequirePhase(GameSessionPhase.Transporting);
            var result = SettlementCalculator.Calculate(settlementInput);
            var nextPhase = result.IsGameOver ? GameSessionPhase.Failed : GameSessionPhase.Completed;
            var nextRunState = nextPhase == GameSessionPhase.Completed ? ShipRunState.Completed : ShipRunState.Failed;

            return new GameSessionState(
                nextPhase,
                settlementInput.Ship.WithRunState(nextRunState),
                new WalletState(result.FinalBalance, Wallet.AllowsDebt),
                result,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                ActiveTransportContract,
                ActiveCargo);
        }

        public GameSessionState FailTransport(SettlementInput settlementInput)
        {
            RequirePhase(GameSessionPhase.Transporting);
            var failedInput = settlementInput.WithShip(settlementInput.Ship.WithRunState(ShipRunState.Failed));
            var result = SettlementCalculator.Calculate(failedInput);

            return new GameSessionState(
                GameSessionPhase.Failed,
                failedInput.Ship,
                new WalletState(result.FinalBalance, Wallet.AllowsDebt),
                result,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                ActiveTransportContract,
                ActiveCargo);
        }

        private GameSessionState StartTransportCore(TransportContractDefinition? contract, CargoState? cargo)
        {
            RequirePhase(GameSessionPhase.Ready);
            return new GameSessionState(
                GameSessionPhase.Transporting,
                Ship.WithRunState(ShipRunState.InTransit),
                Wallet,
                default,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                contract,
                cargo);
        }

        private void RequirePhase(GameSessionPhase expected)
        {
            if (Phase != expected)
            {
                throw new InvalidOperationException($"Expected session phase {expected}, but current phase is {Phase}.");
            }
        }
    }
}
