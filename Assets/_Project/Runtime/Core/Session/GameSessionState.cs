using System;

namespace Bellerophon.Core.Session
{
    public enum GameSessionPhase
    {
        Ready,
        Transporting,
        Completed,
        Failed,
        GameOver
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
            PlayerEquipmentState equipment,
            int completedTransportCount,
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
            Equipment = equipment;
            CompletedTransportCount = completedTransportCount;
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

        public PlayerEquipmentState Equipment { get; }

        public int CompletedTransportCount { get; }

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
                PlayerEquipmentState.Empty,
                0,
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
                PlayerEquipmentState.CreateDefaultAssociationIssue(),
                CompletedTransportCount,
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
            var normalizedInput = settlementInput.WithWallet(Wallet);
            var result = SettlementCalculator.Calculate(normalizedInput);
            var nextPhase = result.IsGameOver ? GameSessionPhase.GameOver : GameSessionPhase.Completed;

            return new GameSessionState(
                nextPhase,
                normalizedInput.Ship.WithRunState(ShipRunState.Completed),
                CreateWalletFromSettlement(result),
                result,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                CompletedTransportCount + 1,
                ActiveTransportContract,
                ActiveCargo);
        }

        public GameSessionState FailTransport(SettlementInput settlementInput)
        {
            RequirePhase(GameSessionPhase.Transporting);
            var failedInput = settlementInput
                .WithWallet(Wallet)
                .WithShip(settlementInput.Ship.WithRunState(ShipRunState.Failed));
            var result = SettlementCalculator.Calculate(failedInput);
            var nextPhase = result.IsGameOver ? GameSessionPhase.GameOver : GameSessionPhase.Failed;

            return new GameSessionState(
                nextPhase,
                failedInput.Ship,
                CreateWalletFromSettlement(result),
                result,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                CompletedTransportCount,
                ActiveTransportContract,
                ActiveCargo);
        }

        public GameSessionState ApplyMaintenanceRepair(int repairCost)
        {
            RequirePhase(GameSessionPhase.Completed);
            if (repairCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repairCost), "Repair cost cannot be negative.");
            }

            var nextCredits = Wallet.Credits - repairCost;
            return new GameSessionState(
                Phase,
                ShipStateRules.RepairAllRooms(Ship),
                new WalletState(nextCredits, Wallet.AllowsDebt, nextCredits < 0),
                SettlementResult.WithPendingRepairCost(0),
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                CompletedTransportCount,
                ActiveTransportContract,
                ActiveCargo);
        }

        public GameSessionState WithEquipment(PlayerEquipmentState equipment)
        {
            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                equipment,
                CompletedTransportCount,
                ActiveTransportContract,
                ActiveCargo);
        }

        public GameSessionState PurchaseEquipment(EquipmentItemKind itemKind)
        {
            var purchase = EquipmentRules.PurchaseItem(Equipment, itemKind);
            if (!purchase.Purchased)
            {
                return WithEquipment(purchase.State);
            }

            if (Wallet.Credits < purchase.SpentCredits)
            {
                return this;
            }

            return new GameSessionState(
                Phase,
                Ship,
                new WalletState(Wallet.Credits - purchase.SpentCredits, Wallet.AllowsDebt, Wallet.HasUnpaidDebtGrace),
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                purchase.State,
                CompletedTransportCount,
                ActiveTransportContract,
                ActiveCargo);
        }

        private GameSessionState StartTransportCore(TransportContractDefinition? contract, CargoState? cargo)
        {
            RequireCanStartTransport();
            return new GameSessionState(
                GameSessionPhase.Transporting,
                Ship.WithRunState(ShipRunState.InTransit),
                Wallet,
                default,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                CompletedTransportCount,
                contract,
                cargo);
        }

        private WalletState CreateWalletFromSettlement(SettlementResult result)
        {
            return new WalletState(
                result.FinalBalance,
                Wallet.AllowsDebt,
                result.DebtStatus == SettlementDebtStatus.GraceActive);
        }

        private void RequirePhase(GameSessionPhase expected)
        {
            if (Phase != expected)
            {
                throw new InvalidOperationException($"Expected session phase {expected}, but current phase is {Phase}.");
            }
        }

        private void RequireCanStartTransport()
        {
            if (Phase == GameSessionPhase.Ready || Phase == GameSessionPhase.Completed)
            {
                return;
            }

            throw new InvalidOperationException($"Cannot start transport while session phase is {Phase}.");
        }
    }
}
