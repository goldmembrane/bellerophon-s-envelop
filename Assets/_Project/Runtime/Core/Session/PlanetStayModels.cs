using System;

namespace Bellerophon.Core.Session
{
    public enum PlanetStayFacilityKind
    {
        None,
        RepairShop,
        ContractOffice,
        Shop,
        PersonalCargoDepot,
        Ship
    }

    public enum PlanetStayMapMarkerKind
    {
        Shop,
        RepairShop,
        Ship,
        CargoSupplyDepot
    }

    public readonly struct PlanetStayMapMarkerState
    {
        public PlanetStayMapMarkerState(
            PlanetStayMapMarkerKind kind,
            string displayName,
            float normalizedX,
            float normalizedY)
        {
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            NormalizedX = Clamp01(normalizedX);
            NormalizedY = Clamp01(normalizedY);
        }

        public PlanetStayMapMarkerKind Kind { get; }

        public string DisplayName { get; }

        public float NormalizedX { get; }

        public float NormalizedY { get; }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }

    public readonly struct PlanetStayContractBoardSummary
    {
        public PlanetStayContractBoardSummary(
            int associationContractCount,
            int privateContractCount,
            int specialContractCount,
            bool buyTabAvailable,
            bool sellTabAvailable)
        {
            AssociationContractCount = Math.Max(0, associationContractCount);
            PrivateContractCount = Math.Max(0, privateContractCount);
            SpecialContractCount = Math.Max(0, specialContractCount);
            BuyTabAvailable = buyTabAvailable;
            SellTabAvailable = sellTabAvailable;
        }

        public int AssociationContractCount { get; }

        public int PrivateContractCount { get; }

        public int SpecialContractCount { get; }

        public int TotalContractCount => AssociationContractCount + PrivateContractCount + SpecialContractCount;

        public bool BuyTabAvailable { get; }

        public bool SellTabAvailable { get; }
    }

    public readonly struct SpecialContractOfferSummary
    {
        public SpecialContractOfferSummary(
            SpecialContractKind kind,
            string displayName,
            bool isAvailable,
            bool isActive,
            bool rewardUnlocked,
            string summary)
        {
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            IsAvailable = isAvailable;
            IsActive = isActive;
            RewardUnlocked = rewardUnlocked;
            Summary = summary ?? string.Empty;
        }

        public SpecialContractKind Kind { get; }

        public string DisplayName { get; }

        public bool IsAvailable { get; }

        public bool IsActive { get; }

        public bool RewardUnlocked { get; }

        public bool IsLocked => !IsAvailable && !IsActive && !RewardUnlocked;

        public string Summary { get; }
    }

    public readonly struct PlanetStayHubState
    {
        public PlanetStayHubState(
            PlanetStayMapMarkerState[] mapMarkers,
            PlanetStayContractBoardSummary contractBoard,
            SpecialContractOfferSummary[] specialContractOffers,
            int repairCharge,
            bool canOpenRepairShop,
            bool canOpenContractOffice,
            bool canOpenShop,
            bool canOpenPersonalCargoDepot,
            bool canOpenShip,
            bool canCollectPersonalCargo,
            bool canDepartWithAcceptedContracts,
            string readinessSummary)
        {
            MapMarkers = mapMarkers == null
                ? new PlanetStayMapMarkerState[0]
                : (PlanetStayMapMarkerState[])mapMarkers.Clone();
            ContractBoard = contractBoard;
            SpecialContractOffers = specialContractOffers == null
                ? new SpecialContractOfferSummary[0]
                : (SpecialContractOfferSummary[])specialContractOffers.Clone();
            RepairCharge = Math.Max(0, repairCharge);
            CanOpenRepairShop = canOpenRepairShop;
            CanOpenContractOffice = canOpenContractOffice;
            CanOpenShop = canOpenShop;
            CanOpenPersonalCargoDepot = canOpenPersonalCargoDepot;
            CanOpenShip = canOpenShip;
            CanCollectPersonalCargo = canCollectPersonalCargo;
            CanDepartWithAcceptedContracts = canDepartWithAcceptedContracts;
            ReadinessSummary = readinessSummary ?? string.Empty;
        }

        public PlanetStayMapMarkerState[] MapMarkers { get; }

        public PlanetStayContractBoardSummary ContractBoard { get; }

        public SpecialContractOfferSummary[] SpecialContractOffers { get; }

        public int RepairCharge { get; }

        public bool CanOpenRepairShop { get; }

        public bool CanOpenContractOffice { get; }

        public bool CanOpenShop { get; }

        public bool CanOpenPersonalCargoDepot { get; }

        public bool CanOpenShip { get; }

        public bool CanCollectPersonalCargo { get; }

        public bool CanDepartWithAcceptedContracts { get; }

        public string ReadinessSummary { get; }
    }

    public static class PlanetStayRules
    {
        private static readonly PlanetStayMapMarkerState[] PlanetMapMarkers =
        {
            new PlanetStayMapMarkerState(PlanetStayMapMarkerKind.Shop, "Shop", 0.2f, 0.65f),
            new PlanetStayMapMarkerState(PlanetStayMapMarkerKind.RepairShop, "Repair Shop", 0.38f, 0.35f),
            new PlanetStayMapMarkerState(PlanetStayMapMarkerKind.Ship, "Ship", 0.68f, 0.46f),
            new PlanetStayMapMarkerState(PlanetStayMapMarkerKind.CargoSupplyDepot, "Cargo Supply Depot", 0.82f, 0.7f)
        };

        public static PlanetStayMapMarkerState[] CreatePlanetMapMarkers()
        {
            return (PlanetStayMapMarkerState[])PlanetMapMarkers.Clone();
        }

        public static PlanetStayHubState CreateHubState(GameSessionState session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var isPlanetStay = session.Phase == GameSessionPhase.Completed;
            var readiness = ShipStateRules.EvaluateStartReadiness(session.Ship, session.PersonalCargoHold.HasCargo);
            var repairCharge = CalculateRepairCharge(session);
            var contractBoard = CreateContractBoardSummary(session);
            return new PlanetStayHubState(
                CreatePlanetMapMarkers(),
                contractBoard,
                CreateSpecialContractOfferSummaries(session),
                repairCharge,
                isPlanetStay,
                isPlanetStay,
                isPlanetStay,
                isPlanetStay,
                isPlanetStay,
                isPlanetStay &&
                ShipStateRules.CanTransportPersonalCargo(session.Ship) &&
                PersonalCargoRules.CalculateAvailableUnits(session.Ship, session.PersonalCargoHold) > 0,
                isPlanetStay &&
                session.PendingTransportContractCount > 0 &&
                readiness.CanStartTransport,
                BuildReadinessSummary(session, readiness, repairCharge));
        }

        public static PlanetStayContractBoardSummary CreateContractBoardSummary(GameSessionState session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = CreateOfferContext(session, HasAnySpecialContractOffer(session));
            var contracts = session.Phase == GameSessionPhase.Completed
                ? DetailedContractCatalogRules.GetPostTutorialContractContents(catalog, context)
                : new ContractContentDefinition[0];
            return new PlanetStayContractBoardSummary(
                CountByType(contracts, ContractType.Association),
                CountByType(contracts, ContractType.Private),
                CountAvailableSpecialOffers(session),
                EquipmentRules.CreatePhase15BuyCatalog().Length > 0,
                EquipmentRules.CreatePhase15SellCatalog().Length > 0);
        }

        public static DetailedContractOfferContext CreateOfferContext(
            GameSessionState session,
            bool includeSpecialContracts)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var cargoHoldScore = (int)Math.Round(ShipStateRules.CalculateCargoHoldScore(session.Ship) * 100f);
            return new DetailedContractOfferContext(
                session.IsAssociationMember,
                cargoHoldScore,
                session.Reputation.FameScore,
                session.Reputation.AssociationFameScore,
                session.CompletedTransportCount,
                CalculateRepairCharge(session),
                includeSpecialContracts,
                session.Reputation.HasUsedRevivalContract);
        }

        public static SpecialContractOfferSummary[] CreateSpecialContractOfferSummaries(GameSessionState session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var definitions = SpecialContractRules.CreateAllDefinitions();
            var summaries = new SpecialContractOfferSummary[definitions.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                var isActive = session.SpecialContracts.ActiveContractKind == definition.Kind;
                var rewardUnlocked = session.SpecialContracts.EquipmentUnlocks.IsUnlocked(definition.UnlockItemKind);
                var isAvailable = SpecialContractRules.CanOfferContract(
                    session.SpecialContracts,
                    session.Reputation,
                    session.CurrentPlanetTrait,
                    definition.Kind);
                summaries[i] = new SpecialContractOfferSummary(
                    definition.Kind,
                    definition.DisplayName,
                    isAvailable,
                    isActive,
                    rewardUnlocked,
                    BuildSpecialOfferSummary(definition, isAvailable, isActive, rewardUnlocked));
            }

            return summaries;
        }

        public static bool HasAnySpecialContractOffer(GameSessionState session)
        {
            if (session == null)
            {
                return false;
            }

            var definitions = SpecialContractRules.CreateAllDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                if (SpecialContractRules.CanOfferContract(
                        session.SpecialContracts,
                        session.Reputation,
                        session.CurrentPlanetTrait,
                        definitions[i].Kind))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountAvailableSpecialOffers(GameSessionState session)
        {
            var offers = CreateSpecialContractOfferSummaries(session);
            var count = 0;
            for (var i = 0; i < offers.Length; i++)
            {
                if (offers[i].IsAvailable || offers[i].IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountByType(ContractContentDefinition[] contracts, ContractType type)
        {
            var count = 0;
            for (var i = 0; i < contracts.Length; i++)
            {
                if (contracts[i].ContractType == type)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CalculateRepairCharge(GameSessionState session)
        {
            var currentRepairCost = session.Ship.IsTotalLoss
                ? ShipStateRules.CalculateTotalLossClaimCost(session.Ship)
                : ShipStateRules.CalculateRepairCost(session.Ship);
            if (currentRepairCost <= 0)
            {
                return 0;
            }

            return session.SettlementResult.PendingRepairCost > 0
                ? session.SettlementResult.PendingRepairCost
                : currentRepairCost;
        }

        private static string BuildReadinessSummary(
            GameSessionState session,
            ShipStartAssessment readiness,
            int repairCharge)
        {
            if (session.Phase != GameSessionPhase.Completed)
            {
                return "Planet stay actions unlock after transport settlement or tutorial skip.";
            }

            if (repairCharge > 0)
            {
                return "Repair charge pending: $" + repairCharge + ".";
            }

            if (!readiness.CanStartTransport)
            {
                return "Ship readiness blocks departure.";
            }

            return session.PendingTransportContractCount > 0
                ? "Ready to depart with accepted contracts."
                : "Ready for repair, contracts, shop, cargo depot, upgrades, or ship preparation.";
        }

        private static string BuildSpecialOfferSummary(
            SpecialContractDefinition definition,
            bool isAvailable,
            bool isActive,
            bool rewardUnlocked)
        {
            if (rewardUnlocked)
            {
                return definition.DisplayName + " reward unlocked.";
            }

            if (isActive)
            {
                return definition.DisplayName + " is active.";
            }

            if (isAvailable)
            {
                return definition.DisplayName + " available.";
            }

            return definition.DisplayName + " requirements are not met.";
        }
    }
}
