using System;

namespace Bellerophon.Core.Session
{
    public enum PlanetTrait
    {
        WaterRich,
        CommonMineralRich,
        OrganicRich,
        RareMineralRich,
        WoodRich,
        VolcanicActive
    }

    public enum RouteHazardKind
    {
        SmallAsteroidField,
        LargeAsteroidField,
        CargoFreedomLeagueRegion,
        SpacePirateRegion,
        AlienLifeformRegion,
        HiddenBlackHole
    }

    public enum CargoOwnership
    {
        Contract,
        Personal,
        SpecialContract
    }

    public enum CargoMaterial
    {
        Unspecified,
        Water,
        CommonMetal,
        OrganicMatter,
        RareMetal,
        Wood,
        VolcanicMineral
    }

    public enum EquipmentAvailability
    {
        CommonShop,
        FameRestrictedShop,
        SpecialUnlock,
        StartingLoadout
    }

    public enum HostileFactionRelation
    {
        Neutral,
        Competitive,
        Hostile,
        Allied,
        Commanded
    }

    public enum HostileUnitRole
    {
        CargoAttacker,
        RoomOccupier,
        PlayerHunter,
        ShipSaboteur,
        Commander,
        BoardingCraft
    }

    public enum ContentImplementationState
    {
        Skeleton,
        Planned,
        Implemented
    }

    public readonly struct PlanetContentDefinition
    {
        private static readonly PlanetTrait[] EmptyTraits = new PlanetTrait[0];
        private readonly PlanetTrait[] traits;

        public PlanetContentDefinition(string planetId, string displayName, PlanetTrait[] traits)
        {
            PlanetId = ContentModelValidation.RequireId(planetId, nameof(planetId));
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            if (traits == null || traits.Length == 0)
            {
                throw new ArgumentException("Planet content requires at least one trait.", nameof(traits));
            }

            this.traits = traits == null || traits.Length == 0
                ? EmptyTraits
                : (PlanetTrait[])traits.Clone();
        }

        public string PlanetId { get; }

        public string DisplayName { get; }

        public PlanetTrait[] Traits => traits == null ? EmptyTraits : (PlanetTrait[])traits.Clone();

        public bool HasTrait(PlanetTrait trait)
        {
            var currentTraits = traits ?? EmptyTraits;
            for (var i = 0; i < currentTraits.Length; i++)
            {
                if (currentTraits[i] == trait)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly struct TransportRouteDefinition
    {
        private static readonly RouteHazardKind[] EmptyHazards = new RouteHazardKind[0];
        private readonly RouteHazardKind[] hazardCandidates;

        public TransportRouteDefinition(
            string routeId,
            string originPlanetId,
            string destinationPlanetId,
            int durationSeconds,
            RouteHazardKind[] hazardCandidates)
            : this(routeId, originPlanetId, destinationPlanetId, durationSeconds, durationSeconds, hazardCandidates)
        {
        }

        public TransportRouteDefinition(
            string routeId,
            string originPlanetId,
            string destinationPlanetId,
            int durationSeconds,
            int distanceUnits,
            RouteHazardKind[] hazardCandidates)
        {
            RouteId = ContentModelValidation.RequireId(routeId, nameof(routeId));
            OriginPlanetId = ContentModelValidation.RequireId(originPlanetId, nameof(originPlanetId));
            DestinationPlanetId = ContentModelValidation.RequireId(destinationPlanetId, nameof(destinationPlanetId));
            if (string.Equals(OriginPlanetId, DestinationPlanetId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Route origin and destination must be different.", nameof(destinationPlanetId));
            }

            DurationSeconds = ContentModelValidation.RequirePositive(durationSeconds, nameof(durationSeconds));
            DistanceUnits = ContentModelValidation.RequirePositive(distanceUnits, nameof(distanceUnits));
            this.hazardCandidates = hazardCandidates == null || hazardCandidates.Length == 0
                ? EmptyHazards
                : (RouteHazardKind[])hazardCandidates.Clone();
        }

        public string RouteId { get; }

        public string OriginPlanetId { get; }

        public string DestinationPlanetId { get; }

        public int DurationSeconds { get; }

        public int DistanceUnits { get; }

        public RouteHazardKind[] HazardCandidates => hazardCandidates == null
            ? EmptyHazards
            : (RouteHazardKind[])hazardCandidates.Clone();
    }

    public readonly struct CargoContentDefinition
    {
        public CargoContentDefinition(
            string cargoId,
            string displayName,
            CargoOwnership ownership,
            CargoGrade grade,
            CargoMaterial material,
            int sizeUnits,
            int baseValueCredits)
        {
            CargoId = ContentModelValidation.RequireId(cargoId, nameof(cargoId));
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            Ownership = ownership;
            Grade = grade;
            Material = material;
            SizeUnits = ContentModelValidation.RequirePositive(sizeUnits, nameof(sizeUnits));
            BaseValueCredits = ContentModelValidation.RequireNonNegative(baseValueCredits, nameof(baseValueCredits));
        }

        public string CargoId { get; }

        public string DisplayName { get; }

        public CargoOwnership Ownership { get; }

        public CargoGrade Grade { get; }

        public CargoMaterial Material { get; }

        public int SizeUnits { get; }

        public int BaseValueCredits { get; }
    }

    public readonly struct ContractContentDefinition
    {
        public ContractContentDefinition(
            string contractId,
            string displayName,
            ContractType contractType,
            ContractDifficulty difficulty,
            string routeId,
            string cargoId,
            int requiredCargoHoldScore,
            ContentImplementationState implementationState)
            : this(
                contractId,
                displayName,
                contractType,
                difficulty,
                routeId,
                cargoId,
                requiredCargoHoldScore,
                implementationState,
                0,
                false)
        {
        }

        public ContractContentDefinition(
            string contractId,
            string displayName,
            ContractType contractType,
            ContractDifficulty difficulty,
            string routeId,
            string cargoId,
            int requiredCargoHoldScore,
            ContentImplementationState implementationState,
            int fixedRewardCredits,
            bool isTutorial,
            bool isRecoveryContract = false)
        {
            ContractId = ContentModelValidation.RequireId(contractId, nameof(contractId));
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            ContractType = contractType;
            Difficulty = difficulty;
            RouteId = ContentModelValidation.RequireId(routeId, nameof(routeId));
            CargoId = ContentModelValidation.RequireId(cargoId, nameof(cargoId));
            RequiredCargoHoldScore = ContentModelValidation.RequireNonNegative(requiredCargoHoldScore, nameof(requiredCargoHoldScore));
            ImplementationState = implementationState;
            FixedRewardCredits = ContentModelValidation.RequireNonNegative(fixedRewardCredits, nameof(fixedRewardCredits));
            IsTutorial = isTutorial;
            IsRecoveryContract = isRecoveryContract;
        }

        public string ContractId { get; }

        public string DisplayName { get; }

        public ContractType ContractType { get; }

        public ContractDifficulty Difficulty { get; }

        public string RouteId { get; }

        public string CargoId { get; }

        public int RequiredCargoHoldScore { get; }

        public ContentImplementationState ImplementationState { get; }

        public int FixedRewardCredits { get; }

        public bool IsTutorial { get; }

        public bool IsRecoveryContract { get; }
    }

    public readonly struct ShipRoomContentDefinition
    {
        public ShipRoomContentDefinition(
            ShipRoomId roomId,
            string displayName,
            int repairCostPerPercent,
            bool supportsCctv,
            bool supportsDirectPlayerOperation)
        {
            RoomId = roomId;
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            RepairCostPerPercent = ContentModelValidation.RequireNonNegative(repairCostPerPercent, nameof(repairCostPerPercent));
            SupportsCctv = supportsCctv;
            SupportsDirectPlayerOperation = supportsDirectPlayerOperation;
        }

        public ShipRoomId RoomId { get; }

        public string DisplayName { get; }

        public int RepairCostPerPercent { get; }

        public bool SupportsCctv { get; }

        public bool SupportsDirectPlayerOperation { get; }
    }

    public readonly struct EquipmentContentDefinition
    {
        public EquipmentContentDefinition(
            string itemId,
            string displayName,
            EquipmentItemCategory category,
            EquipmentAvailability availability,
            int basePriceCredits,
            ContentImplementationState implementationState)
        {
            ItemId = ContentModelValidation.RequireId(itemId, nameof(itemId));
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            if (category == EquipmentItemCategory.None)
            {
                throw new ArgumentOutOfRangeException(nameof(category), "Equipment content requires a category.");
            }

            Category = category;
            Availability = availability;
            BasePriceCredits = ContentModelValidation.RequireNonNegative(basePriceCredits, nameof(basePriceCredits));
            ImplementationState = implementationState;
        }

        public string ItemId { get; }

        public string DisplayName { get; }

        public EquipmentItemCategory Category { get; }

        public EquipmentAvailability Availability { get; }

        public int BasePriceCredits { get; }

        public ContentImplementationState ImplementationState { get; }
    }

    public readonly struct HostileFactionContentDefinition
    {
        public HostileFactionContentDefinition(
            IntruderFaction faction,
            string displayName,
            HostileFactionRelation relationToAssociation,
            ContentImplementationState implementationState)
        {
            if (faction == IntruderFaction.None)
            {
                throw new ArgumentOutOfRangeException(nameof(faction), "Hostile faction content requires a faction.");
            }

            Faction = faction;
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            RelationToAssociation = relationToAssociation;
            ImplementationState = implementationState;
        }

        public IntruderFaction Faction { get; }

        public string DisplayName { get; }

        public HostileFactionRelation RelationToAssociation { get; }

        public ContentImplementationState ImplementationState { get; }
    }

    public readonly struct HostileUnitContentDefinition
    {
        public HostileUnitContentDefinition(
            string unitId,
            string displayName,
            IntruderFaction faction,
            HostileUnitRole role,
            IntruderObjectiveType primaryObjective,
            ContentImplementationState implementationState)
        {
            UnitId = ContentModelValidation.RequireId(unitId, nameof(unitId));
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            if (faction == IntruderFaction.None)
            {
                throw new ArgumentOutOfRangeException(nameof(faction), "Hostile unit content requires a faction.");
            }

            if (primaryObjective == IntruderObjectiveType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(primaryObjective), "Hostile unit content requires an objective.");
            }

            Faction = faction;
            Role = role;
            PrimaryObjective = primaryObjective;
            ImplementationState = implementationState;
        }

        public string UnitId { get; }

        public string DisplayName { get; }

        public IntruderFaction Faction { get; }

        public HostileUnitRole Role { get; }

        public IntruderObjectiveType PrimaryObjective { get; }

        public ContentImplementationState ImplementationState { get; }
    }

    public readonly struct HazardContentDefinition
    {
        public HazardContentDefinition(
            RouteHazardKind hazardKind,
            string displayName,
            ContentImplementationState implementationState)
        {
            HazardKind = hazardKind;
            DisplayName = ContentModelValidation.RequireDisplayName(displayName, nameof(displayName));
            ImplementationState = implementationState;
        }

        public RouteHazardKind HazardKind { get; }

        public string DisplayName { get; }

        public ContentImplementationState ImplementationState { get; }
    }

    public sealed class DetailedContentCatalog
    {
        private static readonly PlanetContentDefinition[] EmptyPlanets = new PlanetContentDefinition[0];
        private static readonly TransportRouteDefinition[] EmptyRoutes = new TransportRouteDefinition[0];
        private static readonly ContractContentDefinition[] EmptyContracts = new ContractContentDefinition[0];
        private static readonly CargoContentDefinition[] EmptyCargo = new CargoContentDefinition[0];
        private static readonly ShipRoomContentDefinition[] EmptyRooms = new ShipRoomContentDefinition[0];
        private static readonly EquipmentContentDefinition[] EmptyEquipment = new EquipmentContentDefinition[0];
        private static readonly HostileFactionContentDefinition[] EmptyFactions = new HostileFactionContentDefinition[0];
        private static readonly HostileUnitContentDefinition[] EmptyUnits = new HostileUnitContentDefinition[0];
        private static readonly HazardContentDefinition[] EmptyHazards = new HazardContentDefinition[0];

        private readonly PlanetContentDefinition[] planets;
        private readonly TransportRouteDefinition[] routes;
        private readonly ContractContentDefinition[] contracts;
        private readonly CargoContentDefinition[] cargo;
        private readonly ShipRoomContentDefinition[] rooms;
        private readonly EquipmentContentDefinition[] equipment;
        private readonly HostileFactionContentDefinition[] hostileFactions;
        private readonly HostileUnitContentDefinition[] hostileUnits;
        private readonly HazardContentDefinition[] hazards;

        public DetailedContentCatalog(
            PlanetContentDefinition[] planets,
            TransportRouteDefinition[] routes,
            ContractContentDefinition[] contracts,
            CargoContentDefinition[] cargo,
            ShipRoomContentDefinition[] rooms,
            EquipmentContentDefinition[] equipment,
            HostileFactionContentDefinition[] hostileFactions,
            HostileUnitContentDefinition[] hostileUnits,
            HazardContentDefinition[] hazards)
        {
            this.planets = CloneOrEmpty(planets, EmptyPlanets);
            this.routes = CloneOrEmpty(routes, EmptyRoutes);
            this.contracts = CloneOrEmpty(contracts, EmptyContracts);
            this.cargo = CloneOrEmpty(cargo, EmptyCargo);
            this.rooms = CloneOrEmpty(rooms, EmptyRooms);
            this.equipment = CloneOrEmpty(equipment, EmptyEquipment);
            this.hostileFactions = CloneOrEmpty(hostileFactions, EmptyFactions);
            this.hostileUnits = CloneOrEmpty(hostileUnits, EmptyUnits);
            this.hazards = CloneOrEmpty(hazards, EmptyHazards);
        }

        public PlanetContentDefinition[] Planets => CloneOrEmpty(planets, EmptyPlanets);

        public TransportRouteDefinition[] Routes => CloneOrEmpty(routes, EmptyRoutes);

        public ContractContentDefinition[] Contracts => CloneOrEmpty(contracts, EmptyContracts);

        public CargoContentDefinition[] Cargo => CloneOrEmpty(cargo, EmptyCargo);

        public ShipRoomContentDefinition[] Rooms => CloneOrEmpty(rooms, EmptyRooms);

        public EquipmentContentDefinition[] Equipment => CloneOrEmpty(equipment, EmptyEquipment);

        public HostileFactionContentDefinition[] HostileFactions => CloneOrEmpty(hostileFactions, EmptyFactions);

        public HostileUnitContentDefinition[] HostileUnits => CloneOrEmpty(hostileUnits, EmptyUnits);

        public HazardContentDefinition[] Hazards => CloneOrEmpty(hazards, EmptyHazards);

        public bool CoversPhaseOneDomains =>
            Planets.Length > 0 &&
            Routes.Length > 0 &&
            Contracts.Length > 0 &&
            Cargo.Length > 0 &&
            Rooms.Length > 0 &&
            Equipment.Length > 0 &&
            HostileFactions.Length > 0 &&
            HostileUnits.Length > 0 &&
            Hazards.Length > 0;

        private static T[] CloneOrEmpty<T>(T[] source, T[] empty)
        {
            return source == null || source.Length == 0
                ? empty
                : (T[])source.Clone();
        }
    }

    internal static class ContentModelValidation
    {
        public static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Content id is required.", parameterName);
            }

            return value;
        }

        public static string RequireDisplayName(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Display name is required.", parameterName);
            }

            return value;
        }

        public static int RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be positive.");
            }

            return value;
        }

        public static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
            }

            return value;
        }
    }
}
