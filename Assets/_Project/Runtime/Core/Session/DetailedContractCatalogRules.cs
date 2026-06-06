using System;
using System.Collections.Generic;

namespace Bellerophon.Core.Session
{
    public readonly struct DetailedContractOfferContext
    {
        public DetailedContractOfferContext(
            bool isAssociationMember,
            int cargoHoldScore,
            int fameScore,
            int associationFameScore,
            int completedAssociationTransportCount,
            int repairCostEstimate,
            bool includeSpecialContracts = false,
            bool hasUsedRevivalContract = false)
        {
            IsAssociationMember = isAssociationMember;
            CargoHoldScore = ContentModelValidation.RequireNonNegative(cargoHoldScore, nameof(cargoHoldScore));
            FameScore = fameScore;
            AssociationFameScore = associationFameScore;
            CompletedAssociationTransportCount = ContentModelValidation.RequireNonNegative(
                completedAssociationTransportCount,
                nameof(completedAssociationTransportCount));
            RepairCostEstimate = ContentModelValidation.RequireNonNegative(repairCostEstimate, nameof(repairCostEstimate));
            IncludeSpecialContracts = includeSpecialContracts;
            HasUsedRevivalContract = hasUsedRevivalContract;
        }

        public bool IsAssociationMember { get; }

        public int CargoHoldScore { get; }

        public int FameScore { get; }

        public int AssociationFameScore { get; }

        public int CompletedAssociationTransportCount { get; }

        public int RepairCostEstimate { get; }

        public bool IncludeSpecialContracts { get; }

        public bool HasUsedRevivalContract { get; }

        public int NextAssociationTransportNumber => CompletedAssociationTransportCount + 1;
    }

    public readonly struct DetailedContractRewardBreakdown
    {
        public DetailedContractRewardBreakdown(
            int cargoValuePay,
            int distancePay,
            int reputationPay,
            int repairSupportAmount,
            int safeStreakBonus,
            int associationMaintenanceFee,
            int fixedRewardCredits = 0)
        {
            CargoValuePay = ContentModelValidation.RequireNonNegative(cargoValuePay, nameof(cargoValuePay));
            DistancePay = ContentModelValidation.RequireNonNegative(distancePay, nameof(distancePay));
            ReputationPay = ContentModelValidation.RequireNonNegative(reputationPay, nameof(reputationPay));
            RepairSupportAmount = ContentModelValidation.RequireNonNegative(repairSupportAmount, nameof(repairSupportAmount));
            SafeStreakBonus = ContentModelValidation.RequireNonNegative(safeStreakBonus, nameof(safeStreakBonus));
            AssociationMaintenanceFee = ContentModelValidation.RequireNonNegative(
                associationMaintenanceFee,
                nameof(associationMaintenanceFee));
            FixedRewardCredits = ContentModelValidation.RequireNonNegative(fixedRewardCredits, nameof(fixedRewardCredits));
        }

        public int CargoValuePay { get; }

        public int DistancePay { get; }

        public int ReputationPay { get; }

        public int RepairSupportAmount { get; }

        public int SafeStreakBonus { get; }

        public int AssociationMaintenanceFee { get; }

        public int FixedRewardCredits { get; }

        public bool IsFixedReward => FixedRewardCredits > 0;

        public int ContractPayCredits => IsFixedReward
            ? FixedRewardCredits
            : CargoValuePay + DistancePay + ReputationPay;

        public int TotalPositiveCredits => ContractPayCredits + RepairSupportAmount + SafeStreakBonus;
    }

    public static class DetailedContractCatalogRules
    {
        public const int CargoValueRewardWeightPercent = 60;
        public const int DistanceRewardWeightPercent = 30;
        public const int ReputationRewardWeightPercent = 10;
        public const int AssociationRepairSupportPercent = 10;
        public const int AssociationMaintenanceFeeCredits = 100;
        public const int AssociationMaintenanceStartsAtTransport = 4;
        public const int AssociationMemberMinimumVisibleContracts = 5;
        public const int NonMemberAssociationVisibleContracts = 2;
        public const int LowFamePrivateContractThreshold = -500;
        public const string TransportTargetName = "Cargo Hold Center Cargo";

        private const string TutorialContractId = "association-tutorial-001";
        private const string AssociationLocalContractId = "association-local-001";
        private const string PrivateSampleContractId = "private-sample-001";
        private const string RevivalContractId = "association-revival-001";

        public static DetailedContentCatalog CreateDefaultStepTwoCatalog()
        {
            return new DetailedContentCatalog(
                CreateDefaultPlanets(),
                CreateDefaultRoutes(),
                CreateDefaultContracts(),
                CreateDefaultCargo(),
                CreateDefaultRooms(),
                CreateDefaultEquipment(),
                CreateDefaultHostileFactions(),
                CreateDefaultHostileUnits(),
                CreateDefaultHazards());
        }

        public static TransportContractDefinition CreateTutorialContract()
        {
            var catalog = CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(true, 100, 0, 0, 0, 0);
            return CreateTransportContract(
                FindContract(catalog, TutorialContractId),
                catalog,
                context);
        }

        public static TransportContractDefinition CreateAssociationFollowUpContract()
        {
            var catalog = CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(true, 100, 0, 0, 1, 0);
            return CreateTransportContract(
                FindContract(catalog, AssociationLocalContractId),
                catalog,
                context);
        }

        public static TransportContractDefinition CreatePrivateFollowUpContract()
        {
            var catalog = CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(true, 100, 0, 0, 1, 0);
            return CreateTransportContract(
                FindContract(catalog, PrivateSampleContractId),
                catalog,
                context);
        }

        public static TransportContractDefinition[] CreatePostTutorialContracts()
        {
            return new[]
            {
                CreateAssociationFollowUpContract(),
                CreatePrivateFollowUpContract()
            };
        }

        public static ContractContentDefinition[] GetPostTutorialContractContents(
            DetailedContentCatalog catalog,
            DetailedContractOfferContext context)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var visibleContracts = new List<ContractContentDefinition>();
            var associationVisibleCount = context.IsAssociationMember
                ? AssociationMemberMinimumVisibleContracts
                : NonMemberAssociationVisibleContracts;
            if (ShouldShowRevivalContract(context))
            {
                visibleContracts.Add(FindContract(catalog, RevivalContractId));
            }

            AddContractsByType(
                visibleContracts,
                catalog.Contracts,
                ContractType.Association,
                associationVisibleCount);
            if (ShouldShowPrivateContracts(context))
            {
                AddContractsByType(
                    visibleContracts,
                    catalog.Contracts,
                    ContractType.Private,
                    int.MaxValue);
            }

            if (context.IncludeSpecialContracts)
            {
                AddContractsByType(
                    visibleContracts,
                    catalog.Contracts,
                    ContractType.Special,
                    int.MaxValue);
            }

            return visibleContracts.ToArray();
        }

        public static bool ShouldShowPrivateContracts(DetailedContractOfferContext context)
        {
            return context.FameScore >= LowFamePrivateContractThreshold;
        }

        public static bool ShouldShowRevivalContract(DetailedContractOfferContext context)
        {
            return context.FameScore < LowFamePrivateContractThreshold &&
                   !context.HasUsedRevivalContract;
        }

        public static bool RequiresForcedAssociationMembership(
            DetailedContractOfferContext context,
            ContractContentDefinition contract)
        {
            return !context.IsAssociationMember &&
                   context.FameScore < LowFamePrivateContractThreshold &&
                   contract.ContractType == ContractType.Association;
        }

        public static TransportContractDefinition[] GetPostTutorialTransportContracts(
            DetailedContentCatalog catalog,
            DetailedContractOfferContext context)
        {
            var contents = GetPostTutorialContractContents(catalog, context);
            var contracts = new TransportContractDefinition[contents.Length];
            for (var i = 0; i < contents.Length; i++)
            {
                contracts[i] = CreateTransportContract(contents[i], catalog, context);
            }

            return contracts;
        }

        public static bool CanAcceptContract(
            DetailedContractOfferContext context,
            ContractContentDefinition contract)
        {
            return context.CargoHoldScore >= contract.RequiredCargoHoldScore;
        }

        public static DetailedContractRewardBreakdown CalculateReward(
            ContractContentDefinition contract,
            TransportRouteDefinition route,
            CargoContentDefinition cargo,
            DetailedContractOfferContext context)
        {
            if (contract.FixedRewardCredits > 0)
            {
                return new DetailedContractRewardBreakdown(0, 0, 0, 0, 0, 0, contract.FixedRewardCredits);
            }

            var reputationScore = contract.ContractType == ContractType.Association
                ? context.AssociationFameScore
                : context.FameScore;
            var cargoValuePay = CalculateWeightedAmount(cargo.BaseValueCredits, CargoValueRewardWeightPercent);
            var distancePay = CalculateWeightedAmount(route.DistanceUnits, DistanceRewardWeightPercent);
            var reputationPay = Math.Max(0, CalculateWeightedAmount(reputationScore, ReputationRewardWeightPercent));
            var isAssociation = contract.ContractType == ContractType.Association;
            var repairSupportAmount = isAssociation
                ? CalculateWeightedAmount(context.RepairCostEstimate, AssociationRepairSupportPercent)
                : 0;
            var safeStreakBonus = isAssociation
                ? SettlementCalculator.CalculateAssociationSafeStreakBonus(context.NextAssociationTransportNumber)
                : 0;
            var maintenanceFee = context.IsAssociationMember &&
                                 isAssociation &&
                                 context.NextAssociationTransportNumber >= AssociationMaintenanceStartsAtTransport
                ? AssociationMaintenanceFeeCredits
                : 0;

            return new DetailedContractRewardBreakdown(
                cargoValuePay,
                distancePay,
                reputationPay,
                repairSupportAmount,
                safeStreakBonus,
                maintenanceFee);
        }

        public static TransportContractDefinition CreateTransportContract(
            ContractContentDefinition contract,
            DetailedContentCatalog catalog,
            DetailedContractOfferContext context)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var route = FindRoute(catalog, contract.RouteId);
            var cargo = FindCargo(catalog, contract.CargoId);
            var originPlanet = FindPlanet(catalog, route.OriginPlanetId);
            var destinationPlanet = FindPlanet(catalog, route.DestinationPlanetId);
            var reward = CalculateReward(contract, route, cargo, context);
            return new TransportContractDefinition(
                contract.ContractId,
                contract.DisplayName,
                TransportTargetName,
                contract.ContractType,
                contract.Difficulty,
                route.DurationSeconds,
                reward.ContractPayCredits,
                new CargoState(
                    cargo.Grade,
                    cargo.SizeUnits,
                    cargo.BaseValueCredits,
                    1f,
                    cargo.Ownership == CargoOwnership.Personal),
                contract.IsTutorial,
                contract.RequiredCargoHoldScore,
                contract.IsRecoveryContract,
                GetPrimaryTrait(originPlanet),
                GetPrimaryTrait(destinationPlanet));
        }

        public static ContractContentDefinition FindContract(DetailedContentCatalog catalog, string contractId)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var contracts = catalog.Contracts;
            for (var i = 0; i < contracts.Length; i++)
            {
                if (string.Equals(contracts[i].ContractId, contractId, StringComparison.Ordinal))
                {
                    return contracts[i];
                }
            }

            throw new InvalidOperationException("Contract not found: " + contractId);
        }

        public static TransportRouteDefinition FindRoute(DetailedContentCatalog catalog, string routeId)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var routes = catalog.Routes;
            for (var i = 0; i < routes.Length; i++)
            {
                if (string.Equals(routes[i].RouteId, routeId, StringComparison.Ordinal))
                {
                    return routes[i];
                }
            }

            throw new InvalidOperationException("Route not found: " + routeId);
        }

        public static PlanetContentDefinition FindPlanet(DetailedContentCatalog catalog, string planetId)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var planets = catalog.Planets;
            for (var i = 0; i < planets.Length; i++)
            {
                if (string.Equals(planets[i].PlanetId, planetId, StringComparison.Ordinal))
                {
                    return planets[i];
                }
            }

            throw new InvalidOperationException("Planet not found: " + planetId);
        }

        public static CargoContentDefinition FindCargo(DetailedContentCatalog catalog, string cargoId)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            var cargo = catalog.Cargo;
            for (var i = 0; i < cargo.Length; i++)
            {
                if (string.Equals(cargo[i].CargoId, cargoId, StringComparison.Ordinal))
                {
                    return cargo[i];
                }
            }

            throw new InvalidOperationException("Cargo not found: " + cargoId);
        }

        private static int CalculateWeightedAmount(int value, int percent)
        {
            return (int)Math.Round(value * (percent / 100f), MidpointRounding.AwayFromZero);
        }

        private static PlanetTrait GetPrimaryTrait(PlanetContentDefinition planet)
        {
            var traits = planet.Traits;
            if (traits.Length == 0)
            {
                throw new InvalidOperationException("Planet has no trait: " + planet.PlanetId);
            }

            return traits[0];
        }

        private static void AddContractsByType(
            List<ContractContentDefinition> visibleContracts,
            ContractContentDefinition[] contracts,
            ContractType contractType,
            int maxCount)
        {
            var added = 0;
            for (var i = 0; i < contracts.Length && added < maxCount; i++)
            {
                var contract = contracts[i];
                if (contract.IsTutorial ||
                    contract.IsRecoveryContract ||
                    contract.ContractType != contractType)
                {
                    continue;
                }

                visibleContracts.Add(contract);
                added++;
            }
        }

        private static PlanetContentDefinition[] CreateDefaultPlanets()
        {
            return new[]
            {
                new PlanetContentDefinition(
                    "planet-association-logo",
                    "Association Start Planet",
                    new[] { PlanetTrait.CommonMineralRich, PlanetTrait.WoodRich }),
                new PlanetContentDefinition(
                    "planet-water-harbor",
                    "Step Two Water Harbor",
                    new[] { PlanetTrait.WaterRich, PlanetTrait.OrganicRich }),
                new PlanetContentDefinition(
                    "planet-ore-belt",
                    "Step Two Ore Belt",
                    new[] { PlanetTrait.CommonMineralRich, PlanetTrait.RareMineralRich }),
                new PlanetContentDefinition(
                    "planet-volcanic-foundry",
                    "Step Two Volcanic Foundry",
                    new[] { PlanetTrait.VolcanicActive, PlanetTrait.CommonMineralRich })
            };
        }

        private static TransportRouteDefinition[] CreateDefaultRoutes()
        {
            return new[]
            {
                new TransportRouteDefinition(
                    "route-association-tutorial",
                    "planet-association-logo",
                    "planet-water-harbor",
                    60,
                    1000,
                    new RouteHazardKind[0]),
                new TransportRouteDefinition(
                    "route-association-revival",
                    "planet-association-logo",
                    "planet-water-harbor",
                    45,
                    500,
                    new RouteHazardKind[0]),
                new TransportRouteDefinition(
                    "route-association-local-001",
                    "planet-water-harbor",
                    "planet-ore-belt",
                    75,
                    1000,
                    new[] { RouteHazardKind.SmallAsteroidField }),
                new TransportRouteDefinition(
                    "route-association-local-002",
                    "planet-ore-belt",
                    "planet-water-harbor",
                    80,
                    1150,
                    new[] { RouteHazardKind.SmallAsteroidField }),
                new TransportRouteDefinition(
                    "route-association-local-003",
                    "planet-water-harbor",
                    "planet-volcanic-foundry",
                    95,
                    1300,
                    new[] { RouteHazardKind.SmallAsteroidField, RouteHazardKind.LargeAsteroidField }),
                new TransportRouteDefinition(
                    "route-association-local-004",
                    "planet-ore-belt",
                    "planet-volcanic-foundry",
                    110,
                    1500,
                    new[] { RouteHazardKind.LargeAsteroidField }),
                new TransportRouteDefinition(
                    "route-association-local-005",
                    "planet-volcanic-foundry",
                    "planet-water-harbor",
                    120,
                    1700,
                    new[] { RouteHazardKind.SmallAsteroidField, RouteHazardKind.AlienLifeformRegion }),
                new TransportRouteDefinition(
                    "route-private-sample-001",
                    "planet-ore-belt",
                    "planet-water-harbor",
                    90,
                    1000,
                    new[] { RouteHazardKind.SmallAsteroidField }),
                new TransportRouteDefinition(
                    "route-private-sample-002",
                    "planet-volcanic-foundry",
                    "planet-ore-belt",
                    130,
                    1800,
                    new[] { RouteHazardKind.LargeAsteroidField, RouteHazardKind.CargoFreedomLeagueRegion }),
                new TransportRouteDefinition(
                    "route-special-signal-001",
                    "planet-water-harbor",
                    "planet-volcanic-foundry",
                    150,
                    2200,
                    new[] { RouteHazardKind.HiddenBlackHole })
            };
        }

        private static CargoContentDefinition[] CreateDefaultCargo()
        {
            return new[]
            {
                new CargoContentDefinition(
                    "cargo-tutorial-basic",
                    "Tutorial Association Cargo",
                    CargoOwnership.Contract,
                    CargoGrade.Common,
                    CargoMaterial.CommonMetal,
                    50,
                    1000),
                new CargoContentDefinition(
                    "cargo-association-revival",
                    "Revival Contract Essentials",
                    CargoOwnership.Contract,
                    CargoGrade.Common,
                    CargoMaterial.CommonMetal,
                    25,
                    500),
                new CargoContentDefinition(
                    "cargo-association-local-001",
                    "Association Metal Freight",
                    CargoOwnership.Contract,
                    CargoGrade.Common,
                    CargoMaterial.CommonMetal,
                    45,
                    1000),
                new CargoContentDefinition(
                    "cargo-association-local-002",
                    "Association Water Filters",
                    CargoOwnership.Contract,
                    CargoGrade.Common,
                    CargoMaterial.Water,
                    50,
                    1150),
                new CargoContentDefinition(
                    "cargo-association-local-003",
                    "Association Organic Samples",
                    CargoOwnership.Contract,
                    CargoGrade.Common,
                    CargoMaterial.OrganicMatter,
                    55,
                    1250),
                new CargoContentDefinition(
                    "cargo-association-local-004",
                    "Association Foundry Parts",
                    CargoOwnership.Contract,
                    CargoGrade.Common,
                    CargoMaterial.CommonMetal,
                    60,
                    1400),
                new CargoContentDefinition(
                    "cargo-association-local-005",
                    "Association Rare Ore Crate",
                    CargoOwnership.Contract,
                    CargoGrade.Rare,
                    CargoMaterial.RareMetal,
                    65,
                    1600),
                new CargoContentDefinition(
                    "cargo-private-sample-001",
                    "Private Volatile Sample",
                    CargoOwnership.Contract,
                    CargoGrade.Rare,
                    CargoMaterial.VolcanicMineral,
                    60,
                    2500),
                new CargoContentDefinition(
                    "cargo-private-sample-002",
                    "Private Rare Alloy Lot",
                    CargoOwnership.Contract,
                    CargoGrade.Rare,
                    CargoMaterial.RareMetal,
                    75,
                    3200),
                new CargoContentDefinition(
                    "cargo-special-signal-001",
                    "Special Signal Core",
                    CargoOwnership.SpecialContract,
                    CargoGrade.Premium,
                    CargoMaterial.RareMetal,
                    80,
                    4500)
            };
        }

        private static ContractContentDefinition[] CreateDefaultContracts()
        {
            return new[]
            {
                new ContractContentDefinition(
                    TutorialContractId,
                    "Tutorial Delivery",
                    ContractType.Association,
                    ContractDifficulty.Intro,
                    "route-association-tutorial",
                    "cargo-tutorial-basic",
                    0,
                    ContentImplementationState.Implemented,
                    1000,
                    true),
                new ContractContentDefinition(
                    RevivalContractId,
                    "Association Revival Freight",
                    ContractType.Association,
                    ContractDifficulty.Intro,
                    "route-association-revival",
                    "cargo-association-revival",
                    0,
                    ContentImplementationState.Implemented,
                    500,
                    false,
                    true),
                new ContractContentDefinition(
                    AssociationLocalContractId,
                    "Association Local Freight",
                    ContractType.Association,
                    ContractDifficulty.VeryEasy,
                    "route-association-local-001",
                    "cargo-association-local-001",
                    40,
                    ContentImplementationState.Implemented),
                new ContractContentDefinition(
                    "association-local-002",
                    "Association Water Filter Run",
                    ContractType.Association,
                    ContractDifficulty.Easy,
                    "route-association-local-002",
                    "cargo-association-local-002",
                    45,
                    ContentImplementationState.Planned),
                new ContractContentDefinition(
                    "association-local-003",
                    "Association Organic Courier",
                    ContractType.Association,
                    ContractDifficulty.Normal,
                    "route-association-local-003",
                    "cargo-association-local-003",
                    50,
                    ContentImplementationState.Planned),
                new ContractContentDefinition(
                    "association-local-004",
                    "Association Foundry Parts",
                    ContractType.Association,
                    ContractDifficulty.Hard,
                    "route-association-local-004",
                    "cargo-association-local-004",
                    55,
                    ContentImplementationState.Planned),
                new ContractContentDefinition(
                    "association-local-005",
                    "Association Rare Ore Escort",
                    ContractType.Association,
                    ContractDifficulty.VeryHard,
                    "route-association-local-005",
                    "cargo-association-local-005",
                    60,
                    ContentImplementationState.Planned),
                new ContractContentDefinition(
                    PrivateSampleContractId,
                    "Private Volatile Sample",
                    ContractType.Private,
                    ContractDifficulty.Normal,
                    "route-private-sample-001",
                    "cargo-private-sample-001",
                    65,
                    ContentImplementationState.Implemented),
                new ContractContentDefinition(
                    "private-sample-002",
                    "Private Rare Alloy Lot",
                    ContractType.Private,
                    ContractDifficulty.VeryHard,
                    "route-private-sample-002",
                    "cargo-private-sample-002",
                    75,
                    ContentImplementationState.Planned),
                new ContractContentDefinition(
                    "special-signal-001",
                    "Special Signal Core",
                    ContractType.Special,
                    ContractDifficulty.Master,
                    "route-special-signal-001",
                    "cargo-special-signal-001",
                    80,
                    ContentImplementationState.Planned)
            };
        }

        private static ShipRoomContentDefinition[] CreateDefaultRooms()
        {
            return new[]
            {
                new ShipRoomContentDefinition(ShipRoomId.Cockpit, "Cockpit", ShipStateRules.SettlementSummaryRepairRatePerPercent, true, true),
                new ShipRoomContentDefinition(ShipRoomId.CargoHold, "Cargo Hold", ShipStateRules.SettlementSummaryRepairRatePerPercent, true, false),
                new ShipRoomContentDefinition(ShipRoomId.Armory, "Armory", ShipStateRules.SettlementSummaryRepairRatePerPercent, true, true),
                new ShipRoomContentDefinition(ShipRoomId.EngineRoom, "Engine Room", ShipStateRules.SettlementSummaryRepairRatePerPercent, true, true),
                new ShipRoomContentDefinition(ShipRoomId.ControlRoom, "Control Room", ShipStateRules.SettlementSummaryRepairRatePerPercent, true, true),
                new ShipRoomContentDefinition(ShipRoomId.SupplyRoom, "Supply Room", ShipStateRules.SettlementSummaryRepairRatePerPercent, true, true)
            };
        }

        private static EquipmentContentDefinition[] CreateDefaultEquipment()
        {
            var buyCatalog = EquipmentRules.CreatePhase15BuyCatalog();
            var equipment = new EquipmentContentDefinition[buyCatalog.Length + 1];
            equipment[0] = CreateEquipmentContent(
                EquipmentItemKind.BasicProtectiveSuit,
                ContentImplementationState.Implemented);
            for (var i = 0; i < buyCatalog.Length; i++)
            {
                equipment[i + 1] = CreateEquipmentContent(
                    buyCatalog[i].ItemKind,
                    buyCatalog[i].FunctionalInPhase15
                        ? ContentImplementationState.Implemented
                        : ContentImplementationState.Planned);
            }

            return equipment;
        }

        private static EquipmentContentDefinition CreateEquipmentContent(
            EquipmentItemKind itemKind,
            ContentImplementationState implementationState)
        {
            var definition = EquipmentRules.GetDefinition(itemKind);
            return new EquipmentContentDefinition(
                itemKind.ToString(),
                definition.DisplayName,
                definition.Category,
                definition.Availability,
                definition.PriceCredits,
                implementationState);
        }

        private static HostileFactionContentDefinition[] CreateDefaultHostileFactions()
        {
            return new[]
            {
                new HostileFactionContentDefinition(
                    IntruderFaction.SeedEntity,
                    "Seed Entity",
                    HostileFactionRelation.Hostile,
                    ContentImplementationState.Implemented)
            };
        }

        private static HostileUnitContentDefinition[] CreateDefaultHostileUnits()
        {
            return new[]
            {
                new HostileUnitContentDefinition(
                    "seed-parvum",
                    "Parvum",
                    IntruderFaction.SeedEntity,
                    HostileUnitRole.ShipSaboteur,
                    IntruderObjectiveType.DestroyShip,
                    ContentImplementationState.Implemented)
            };
        }

        private static HazardContentDefinition[] CreateDefaultHazards()
        {
            return new[]
            {
                new HazardContentDefinition(
                    RouteHazardKind.SmallAsteroidField,
                    "Small Asteroid Field",
                    ContentImplementationState.Implemented),
                new HazardContentDefinition(
                    RouteHazardKind.LargeAsteroidField,
                    "Large Asteroid Field",
                    ContentImplementationState.Planned),
                new HazardContentDefinition(
                    RouteHazardKind.CargoFreedomLeagueRegion,
                    "Cargo Freedom League Region",
                    ContentImplementationState.Planned),
                new HazardContentDefinition(
                    RouteHazardKind.SpacePirateRegion,
                    "Space Pirate Region",
                    ContentImplementationState.Planned),
                new HazardContentDefinition(
                    RouteHazardKind.AlienLifeformRegion,
                    "Alien Lifeform Region",
                    ContentImplementationState.Planned),
                new HazardContentDefinition(
                    RouteHazardKind.HiddenBlackHole,
                    "Hidden Black Hole",
                    ContentImplementationState.Skeleton)
            };
        }
    }
}
