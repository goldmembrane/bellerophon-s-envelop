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
        private static readonly TransportContractDefinition[] EmptyTransportContracts = new TransportContractDefinition[0];
        // Pending contracts are accepted on the board; active contracts are carried by the current or last transport run.
        private readonly TransportContractDefinition[] pendingTransportContracts;
        private readonly TransportContractDefinition[] activeTransportContracts;

        private GameSessionState(
            GameSessionPhase phase,
            ShipState ship,
            WalletState wallet,
            SettlementResult settlementResult,
            bool isAssociationMember,
            PlanetStartState currentPlanet,
            StartingLoadoutState startingLoadout,
            PlayerEquipmentState equipment,
            ReputationState reputation,
            PersonalCargoHoldState personalCargoHold,
            ShipUpgradeState shipUpgrades,
            PlanetTrait currentPlanetTrait,
            int completedTransportCount,
            int towingIncidentCount,
            TransportContractDefinition? activeTransportContract,
            CargoState? activeCargo,
            TransportContractDefinition[] pendingTransportContracts,
            TransportContractDefinition[] activeTransportContracts,
            TransportHazardUnlockState transportHazardUnlocks = default,
            SpecialContractProgressState specialContracts = default)
        {
            Phase = phase;
            Ship = ship;
            Wallet = wallet;
            SettlementResult = settlementResult;
            IsAssociationMember = isAssociationMember;
            CurrentPlanet = currentPlanet;
            StartingLoadout = startingLoadout;
            Equipment = equipment;
            Reputation = reputation;
            PersonalCargoHold = personalCargoHold ?? PersonalCargoHoldState.Empty;
            ShipUpgrades = shipUpgrades.Appearance.HullPaintSlotId == null
                ? ShipUpgradeState.Empty
                : shipUpgrades;
            CurrentPlanetTrait = currentPlanetTrait;
            CompletedTransportCount = completedTransportCount;
            TowingIncidentCount = RequireNonNegative(towingIncidentCount, nameof(towingIncidentCount));
            ActiveTransportContract = activeTransportContract;
            ActiveCargo = activeCargo;
            TransportHazardUnlocks = transportHazardUnlocks.WithFameScore(reputation.FameScore);
            SpecialContracts = specialContracts;
            this.pendingTransportContracts = CloneContracts(pendingTransportContracts);
            this.activeTransportContracts = CloneContracts(activeTransportContracts);
        }

        public GameSessionPhase Phase { get; }

        public ShipState Ship { get; }

        public WalletState Wallet { get; }

        public SettlementResult SettlementResult { get; }

        public bool IsAssociationMember { get; }

        public PlanetStartState CurrentPlanet { get; }

        public StartingLoadoutState StartingLoadout { get; }

        public PlayerEquipmentState Equipment { get; }

        public ReputationState Reputation { get; }

        public PersonalCargoHoldState PersonalCargoHold { get; }

        public ShipUpgradeState ShipUpgrades { get; }

        public PlanetTrait CurrentPlanetTrait { get; }

        public int CompletedTransportCount { get; }

        public int TowingIncidentCount { get; }

        public TransportContractDefinition? ActiveTransportContract { get; }

        public CargoState? ActiveCargo { get; }

        public bool HasActiveCargo => ActiveCargo.HasValue;

        public TransportHazardUnlockState TransportHazardUnlocks { get; }

        public SpecialContractProgressState SpecialContracts { get; }

        public TransportContractDefinition[] PendingTransportContracts => CloneContracts(pendingTransportContracts);

        public TransportContractDefinition[] ActiveTransportContracts => CloneContracts(activeTransportContracts);

        public int PendingTransportContractCount => pendingTransportContracts == null ? 0 : pendingTransportContracts.Length;

        public int ActiveTransportContractCount => activeTransportContracts == null ? 0 : activeTransportContracts.Length;

        public bool HasPendingTransportContracts => PendingTransportContractCount > 0;

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
                ReputationState.Default,
                PersonalCargoHoldState.Empty,
                ShipUpgradeState.Empty,
                PlanetTrait.CommonMineralRich,
                0,
                0,
                null,
                null,
                EmptyTransportContracts,
                EmptyTransportContracts);
        }

        public static GameSessionState StartAssociationSession()
        {
            return StartSession(new WalletState(0, false))
                .WithAssociationStart(
                    PlanetStartState.CreateAssociationLogoStart(),
                    StartingLoadoutState.CreateDefaultAssociationIssue());
        }

        public static GameSessionState Restore(
            GameSessionPhase phase,
            ShipState ship,
            WalletState wallet,
            SettlementResult settlementResult,
            bool isAssociationMember,
            PlanetStartState currentPlanet,
            StartingLoadoutState startingLoadout,
            PlayerEquipmentState equipment,
            ReputationState reputation,
            PersonalCargoHoldState personalCargoHold,
            ShipUpgradeState shipUpgrades,
            PlanetTrait currentPlanetTrait,
            int completedTransportCount,
            int towingIncidentCount,
            TransportContractDefinition? activeTransportContract,
            CargoState? activeCargo,
            TransportContractDefinition[] pendingTransportContracts,
            TransportContractDefinition[] activeTransportContracts,
            TransportHazardUnlockState transportHazardUnlocks,
            SpecialContractProgressState specialContracts)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return new GameSessionState(
                phase,
                ship,
                wallet,
                settlementResult,
                isAssociationMember,
                currentPlanet,
                startingLoadout,
                equipment,
                reputation,
                personalCargoHold ?? PersonalCargoHoldState.Empty,
                shipUpgrades,
                currentPlanetTrait,
                completedTransportCount,
                towingIncidentCount,
                activeTransportContract,
                activeCargo,
                pendingTransportContracts,
                activeTransportContracts,
                transportHazardUnlocks,
                specialContracts);
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
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                PlanetTrait.CommonMineralRich,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public GameSessionState GrantTutorialSkipReward(int rewardCredits)
        {
            RequirePhase(GameSessionPhase.Ready);
            if (rewardCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardCredits), "Tutorial skip reward cannot be negative.");
            }

            var nextCredits = Wallet.Credits + rewardCredits;
            return new GameSessionState(
                GameSessionPhase.Completed,
                Ship.WithRunState(ShipRunState.Docked),
                new WalletState(nextCredits, Wallet.AllowsDebt, Wallet.HasUnpaidDebtGrace),
                default,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                Math.Max(CompletedTransportCount, 1),
                TowingIncidentCount,
                null,
                null,
                EmptyTransportContracts,
                EmptyTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public GameSessionState StartTransport()
        {
            return StartTransportCore(null, null, EmptyTransportContracts);
        }

        public GameSessionState StartTransport(TransportContractDefinition contract)
        {
            return StartTransportCore(contract, contract.Cargo, new[] { contract });
        }

        public GameSessionState AcceptTransportContract(TransportContractDefinition contract)
        {
            RequirePhase(GameSessionPhase.Completed);
            if (ContainsContract(pendingTransportContracts, contract.Id))
            {
                return this;
            }

            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                AppendContract(pendingTransportContracts, contract),
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public bool IsTransportContractPending(string contractId)
        {
            return ContainsContract(pendingTransportContracts, contractId);
        }

        public GameSessionState StartAcceptedTransportContracts()
        {
            RequirePhase(GameSessionPhase.Completed);
            if (PendingTransportContractCount <= 0)
            {
                throw new InvalidOperationException("Cannot start transport without accepted contracts.");
            }

            var activeContracts = CloneContracts(pendingTransportContracts);
            var primaryContract = activeContracts[0];
            return StartTransportCore(primaryContract, primaryContract.Cargo, activeContracts);
        }

        public GameSessionState CompleteTransport(SettlementInput settlementInput)
        {
            RequirePhase(GameSessionPhase.Transporting);
            var normalizedInput = settlementInput.WithWallet(Wallet);
            var result = SettlementCalculator.Calculate(normalizedInput);
            var nextReputation = ApplyContractReputation(normalizedInput, true);
            var nextPersonalCargoHold = ApplyTransportDamageToPersonalCargo(normalizedInput);
            var nextPlanetTrait = ActiveTransportContract.HasValue
                ? ActiveTransportContract.Value.DestinationTrait
                : CurrentPlanetTrait;
            var nextTowingIncidentCount = result.RequiresTowing
                ? TowingIncidentCount + 1
                : TowingIncidentCount;
            var nextSpecialContracts = SpecialContractRules.RecordTransportArrivalProgress(
                SpecialContracts,
                nextPlanetTrait,
                ActiveTransportContract,
                true);
            var specialSettlement = SpecialContractRules.ResolveTransportArrival(
                nextSpecialContracts,
                normalizedInput.Cargo,
                true);
            var nextWallet = CreateWalletFromSettlement(result);
            if (specialSettlement.BonusCredits > 0)
            {
                var nextCredits = nextWallet.Credits + specialSettlement.BonusCredits;
                nextWallet = new WalletState(nextCredits, nextWallet.AllowsDebt, nextCredits < 0);
            }

            var nextPhase = nextWallet.Credits < 0 && Wallet.HasUnpaidDebtGrace
                ? GameSessionPhase.GameOver
                : GameSessionPhase.Completed;
            var nextEquipment = Equipment;
            if (specialSettlement.Completed &&
                specialSettlement.GrantedItemKind != EquipmentItemKind.None)
            {
                nextEquipment = EquipmentRules.GrantItem(nextEquipment, specialSettlement.GrantedItemKind).State;
            }

            return new GameSessionState(
                nextPhase,
                normalizedInput.Ship.WithRunState(ShipRunState.Completed),
                nextWallet,
                result,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                nextEquipment,
                nextReputation,
                nextPersonalCargoHold,
                ShipUpgrades,
                nextPlanetTrait,
                CompletedTransportCount + 1,
                nextTowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                EmptyTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                specialSettlement.State);
        }

        public GameSessionState FailTransport(SettlementInput settlementInput)
        {
            RequirePhase(GameSessionPhase.Transporting);
            var failedInput = settlementInput
                .WithWallet(Wallet)
                .WithShip(settlementInput.Ship.WithRunState(ShipRunState.Failed));
            var result = SettlementCalculator.Calculate(failedInput);
            var nextPhase = result.IsGameOver ? GameSessionPhase.GameOver : GameSessionPhase.Failed;
            var nextReputation = ApplyContractReputation(failedInput, false);
            var nextPersonalCargoHold = ApplyTransportDamageToPersonalCargo(failedInput);
            var nextTowingIncidentCount = result.RequiresTowing
                ? TowingIncidentCount + 1
                : TowingIncidentCount;
            var specialSettlement = SpecialContractRules.ResolveTransportArrival(
                SpecialContracts,
                failedInput.Cargo,
                false);

            return new GameSessionState(
                nextPhase,
                failedInput.Ship,
                CreateWalletFromSettlement(result),
                result,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                nextReputation,
                nextPersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                nextTowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                EmptyTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                specialSettlement.State);
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
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
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
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public GameSessionState WithShipState(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return new GameSessionState(
                Phase,
                ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public GameSessionState WithReputation(ReputationState reputation)
        {
            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public GameSessionState WithSpecialContracts(SpecialContractProgressState specialContracts)
        {
            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                specialContracts);
        }

        public GameSessionState WithAssociationMembership(bool isAssociationMember)
        {
            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                isAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public SpecialContractSessionAcceptanceResult AcceptSpecialContract(SpecialContractKind kind)
        {
            RequirePhase(GameSessionPhase.Completed);
            var acceptance = SpecialContractRules.AcceptContract(
                SpecialContracts,
                Reputation,
                CurrentPlanetTrait,
                kind);
            if (!acceptance.Accepted)
            {
                return new SpecialContractSessionAcceptanceResult(
                    false,
                    this,
                    kind,
                    acceptance.Summary);
            }

            return new SpecialContractSessionAcceptanceResult(
                true,
                WithSpecialContracts(acceptance.State),
                kind,
                acceptance.Summary);
        }

        public GameSessionState WithShipUpgrades(ShipUpgradeState upgrades)
        {
            var equipment = ApplyEquipmentCapacityForUpgrades(Equipment, upgrades);
            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                equipment,
                Reputation,
                PersonalCargoHold,
                upgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public ShipUpgradePurchaseResult PurchaseShipUpgrade(ShipUpgradeCategory category)
        {
            RequirePhase(GameSessionPhase.Completed);
            var nextCost = ShipUpgradeRules.GetNextPurchaseCost(ShipUpgrades, category);
            if (nextCost <= 0)
            {
                return new ShipUpgradePurchaseResult(
                    false,
                    this,
                    category,
                    ShipUpgrades.GetPurchasedTier(category),
                    0,
                    ShipUpgradeRules.FormatCategoryName(category) + " is already at max purchase tier.");
            }

            if (Wallet.Credits < nextCost)
            {
                return new ShipUpgradePurchaseResult(
                    false,
                    this,
                    category,
                    ShipUpgrades.GetPurchasedTier(category),
                    0,
                    "Insufficient credits for " + ShipUpgradeRules.FormatCategoryName(category) +
                    " tier " + (ShipUpgrades.GetPurchasedTier(category) + 1) + ".");
            }

            var nextUpgrades = ShipUpgradeRules.PurchaseNextTier(ShipUpgrades, category);
            var nextCredits = Wallet.Credits - nextCost;
            var nextEquipment = ApplyEquipmentCapacityForUpgrades(Equipment, nextUpgrades);
            var nextState = new GameSessionState(
                Phase,
                Ship,
                new WalletState(nextCredits, Wallet.AllowsDebt, Wallet.HasUnpaidDebtGrace),
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                nextEquipment,
                Reputation,
                PersonalCargoHold,
                nextUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);

            return new ShipUpgradePurchaseResult(
                true,
                nextState,
                category,
                nextUpgrades.GetPurchasedTier(category),
                nextCost,
                "Purchased " + ShipUpgradeRules.FormatCategoryName(category) +
                " tier " + nextUpgrades.GetPurchasedTier(category) +
                " for " + FormatMoney(nextCost) +
                (category == ShipUpgradeCategory.Durability ? " Applied automatically." : "."));
        }

        public ShipUpgradeEquipResult EquipShipUpgrade(ShipUpgradeCategory category)
        {
            RequirePhase(GameSessionPhase.Completed);
            if (!ShipUpgradeRules.CanEquipPurchasedTier(ShipUpgrades, category))
            {
                return new ShipUpgradeEquipResult(
                    false,
                    this,
                    category,
                    ShipUpgrades.GetEquippedTier(category),
                    "No un-equipped purchased tier is available for " +
                    ShipUpgradeRules.FormatCategoryName(category) + ".");
            }

            var nextUpgrades = ShipUpgradeRules.EquipHighestPurchasedTier(ShipUpgrades, category);
            var nextState = WithShipUpgrades(nextUpgrades);
            return new ShipUpgradeEquipResult(
                true,
                nextState,
                category,
                nextUpgrades.GetEquippedTier(category),
                "Equipped " + ShipUpgradeRules.FormatCategoryName(category) +
                " tier " + nextUpgrades.GetEquippedTier(category) + ".");
        }

        public PersonalCargoCollectionResult CollectPersonalCargo(int collectionSeed)
        {
            RequirePhase(GameSessionPhase.Completed);
            var cargo = PersonalCargoRules.CreateCollectedCargo(CurrentPlanetTrait, collectionSeed);
            if (!PersonalCargoRules.CanAddCargo(Ship, PersonalCargoHold, cargo))
            {
                var summary = ShipStateRules.CanTransportPersonalCargo(Ship)
                    ? "Cargo hold capacity is full. Required " + cargo.SizeUnits +
                      " units, available " + PersonalCargoRules.CalculateAvailableUnits(Ship, PersonalCargoHold) + "."
                    : "Cargo hold damage blocks personal cargo transport.";
                return new PersonalCargoCollectionResult(
                    false,
                    this,
                    cargo,
                    summary);
            }

            var nextState = WithPersonalCargoHold(PersonalCargoHold.WithCargoAdded(cargo));
            return new PersonalCargoCollectionResult(
                true,
                nextState,
                cargo,
                "Collected " + cargo.DisplayName + " for free.");
        }

        public PersonalCargoSaleResult SellPersonalCargo(int cargoIndex)
        {
            RequirePhase(GameSessionPhase.Completed);
            if (cargoIndex < 0 || cargoIndex >= PersonalCargoHold.Count)
            {
                return new PersonalCargoSaleResult(
                    false,
                    this,
                    default,
                    default,
                    "No personal cargo is selected for sale.");
            }

            var cargo = PersonalCargoHold.GetCargo(cargoIndex);
            var quote = PersonalCargoRules.CalculateSaleQuote(cargo, CurrentPlanetTrait);
            var nextHold = PersonalCargoHold.WithoutCargoAt(cargoIndex);
            var nextState = new GameSessionState(
                Phase,
                Ship,
                new WalletState(Wallet.Credits + quote.SalePrice, Wallet.AllowsDebt, Wallet.HasUnpaidDebtGrace),
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                nextHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);

            return new PersonalCargoSaleResult(
                true,
                nextState,
                cargo,
                quote,
                "Sold " + cargo.DisplayName + " for " + FormatMoney(quote.SalePrice) + ".");
        }

        public GameSessionState WithPersonalCargoHold(PersonalCargoHoldState hold)
        {
            return new GameSessionState(
                Phase,
                Ship,
                Wallet,
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                Equipment,
                Reputation,
                hold ?? PersonalCargoHoldState.Empty,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public GameSessionState PurchaseEquipment(EquipmentItemKind itemKind)
        {
            var purchase = EquipmentRules.PurchaseItem(Equipment, itemKind, SpecialContracts.EquipmentUnlocks);
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
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        public EquipmentDisposalSessionResult DisposeFirstPurchasedEquipment()
        {
            RequirePhase(GameSessionPhase.Completed);
            return ApplyEquipmentDisposal(EquipmentRules.DisposeFirstPurchasedItem(Equipment));
        }

        public EquipmentDisposalSessionResult DisposePurchasedHandEquipment(int handSlotIndex)
        {
            RequirePhase(GameSessionPhase.Completed);
            return ApplyEquipmentDisposal(EquipmentRules.DisposePurchasedHandItem(Equipment, handSlotIndex));
        }

        public EquipmentDisposalSessionResult DisposePurchasedSupplyEquipment(int supplySlotIndex)
        {
            RequirePhase(GameSessionPhase.Completed);
            return ApplyEquipmentDisposal(EquipmentRules.DisposePurchasedSupplyItem(Equipment, supplySlotIndex));
        }

        private EquipmentDisposalSessionResult ApplyEquipmentDisposal(EquipmentDisposalResult disposal)
        {
            if (!disposal.Disposed)
            {
                return new EquipmentDisposalSessionResult(
                    false,
                    WithEquipment(disposal.State),
                    disposal.ItemKind,
                    0,
                    disposal.Summary);
            }

            var nextState = new GameSessionState(
                Phase,
                Ship,
                new WalletState(Wallet.Credits + disposal.ReceivedCredits, Wallet.AllowsDebt, Wallet.HasUnpaidDebtGrace),
                SettlementResult,
                IsAssociationMember,
                CurrentPlanet,
                StartingLoadout,
                disposal.State,
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                ActiveTransportContract,
                ActiveCargo,
                pendingTransportContracts,
                activeTransportContracts,
                TransportHazardUnlocks,
                SpecialContracts);

            return new EquipmentDisposalSessionResult(
                true,
                nextState,
                disposal.ItemKind,
                disposal.ReceivedCredits,
                disposal.Summary);
        }

        private GameSessionState StartTransportCore(
            TransportContractDefinition? contract,
            CargoState? cargo,
            TransportContractDefinition[] activeContracts)
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
                Reputation,
                PersonalCargoHold,
                ShipUpgrades,
                CurrentPlanetTrait,
                CompletedTransportCount,
                TowingIncidentCount,
                contract,
                cargo,
                EmptyTransportContracts,
                activeContracts,
                TransportHazardUnlocks,
                SpecialContracts);
        }

        private static PlayerEquipmentState ApplyEquipmentCapacityForUpgrades(
            PlayerEquipmentState equipment,
            ShipUpgradeState upgrades)
        {
            var supplySlotCount = ShipUpgradeRules.GetEffectValue(
                ShipUpgradeCategory.SupplySlots,
                upgrades.GetEquippedTier(ShipUpgradeCategory.SupplySlots));
            return equipment.WithUnlockedSupplySlotCount(supplySlotCount);
        }

        private WalletState CreateWalletFromSettlement(SettlementResult result)
        {
            return new WalletState(
                result.FinalBalance,
                Wallet.AllowsDebt,
                result.DebtStatus == SettlementDebtStatus.GraceActive);
        }

        private PersonalCargoHoldState ApplyTransportDamageToPersonalCargo(SettlementInput input)
        {
            if (!PersonalCargoHold.HasCargo)
            {
                return PersonalCargoHold;
            }

            var damagePercent = Math.Max(
                input.Cargo.LossPercent,
                ShipStateRules.CalculateCargoLossPercentFromCargoHold(input.Ship));
            return PersonalCargoHold.WithDamagePercent(damagePercent);
        }

        private ReputationState ApplyContractReputation(SettlementInput input, bool completedTransport)
        {
            var contracts = activeTransportContracts;
            if ((contracts == null || contracts.Length == 0) && ActiveTransportContract.HasValue)
            {
                contracts = new[] { ActiveTransportContract.Value };
            }

            if (contracts == null || contracts.Length == 0)
            {
                return Reputation;
            }

            var reputation = Reputation;
            for (var i = 0; i < contracts.Length; i++)
            {
                var change = ReputationRules.CalculateContractResult(
                    contracts[i],
                    IsAssociationMember,
                    completedTransport,
                    input.Crew.DeadCount,
                    input.Cargo.LossPercent);
                reputation = ReputationRules.ApplyChange(reputation, change);
            }

            return reputation;
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
            if (Phase != GameSessionPhase.Ready && Phase != GameSessionPhase.Completed)
            {
                throw new InvalidOperationException($"Cannot start transport while session phase is {Phase}.");
            }

            var readiness = ShipStateRules.EvaluateStartReadiness(Ship, PersonalCargoHold.HasCargo);
            if (!readiness.CanStartTransport)
            {
                throw new InvalidOperationException("Cannot start transport until ship readiness is restored.");
            }
        }

        private static TransportContractDefinition[] CloneContracts(TransportContractDefinition[] contracts)
        {
            return contracts == null || contracts.Length == 0
                ? EmptyTransportContracts
                : (TransportContractDefinition[])contracts.Clone();
        }

        private static TransportContractDefinition[] AppendContract(
            TransportContractDefinition[] contracts,
            TransportContractDefinition contract)
        {
            var current = contracts ?? EmptyTransportContracts;
            var next = new TransportContractDefinition[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = contract;
            return next;
        }

        private static bool ContainsContract(TransportContractDefinition[] contracts, string contractId)
        {
            if (contracts == null || string.IsNullOrWhiteSpace(contractId))
            {
                return false;
            }

            for (var i = 0; i < contracts.Length; i++)
            {
                if (string.Equals(contracts[i].Id, contractId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Session counts cannot be negative.");
            }

            return value;
        }

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }
    }

    public readonly struct SpecialContractSessionAcceptanceResult
    {
        public SpecialContractSessionAcceptanceResult(
            bool accepted,
            GameSessionState state,
            SpecialContractKind contractKind,
            string summary)
        {
            Accepted = accepted;
            State = state ?? throw new ArgumentNullException(nameof(state));
            ContractKind = contractKind;
            Summary = summary ?? string.Empty;
        }

        public bool Accepted { get; }

        public GameSessionState State { get; }

        public SpecialContractKind ContractKind { get; }

        public string Summary { get; }
    }
}
