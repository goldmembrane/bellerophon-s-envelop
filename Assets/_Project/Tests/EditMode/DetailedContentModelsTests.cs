using System;
using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class DetailedContentModelsTests
    {
        [Test]
        public void DetailedContentCatalog_CoversPhaseOneDomainSkeletons()
        {
            var catalog = CreateSampleCatalog();

            Assert.That(catalog.CoversPhaseOneDomains, Is.True);
            Assert.That(catalog.Planets.Length, Is.EqualTo(2));
            Assert.That(catalog.Routes.Length, Is.EqualTo(1));
            Assert.That(catalog.Contracts.Length, Is.EqualTo(1));
            Assert.That(catalog.Cargo.Length, Is.EqualTo(1));
            Assert.That(catalog.Rooms.Length, Is.EqualTo(6));
            Assert.That(catalog.Equipment.Length, Is.EqualTo(1));
            Assert.That(catalog.HostileFactions.Length, Is.EqualTo(1));
            Assert.That(catalog.HostileUnits.Length, Is.EqualTo(1));
            Assert.That(catalog.Hazards.Length, Is.EqualTo(1));
        }

        [Test]
        public void PlanetContentDefinition_ClonesTraitArrays()
        {
            var traits = new[] { PlanetTrait.WaterRich, PlanetTrait.CommonMineralRich };
            var planet = new PlanetContentDefinition("planet-alpha", "Planet Alpha", traits);

            traits[0] = PlanetTrait.VolcanicActive;
            var exposedTraits = planet.Traits;
            exposedTraits[1] = PlanetTrait.RareMineralRich;

            Assert.That(planet.HasTrait(PlanetTrait.WaterRich), Is.True);
            Assert.That(planet.HasTrait(PlanetTrait.CommonMineralRich), Is.True);
            Assert.That(planet.HasTrait(PlanetTrait.VolcanicActive), Is.False);
            Assert.That(planet.HasTrait(PlanetTrait.RareMineralRich), Is.False);
        }

        [Test]
        public void TransportRouteDefinition_RequiresPositiveRouteWithDifferentEndpoints()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TransportRouteDefinition(
                "route-alpha-beta",
                "planet-alpha",
                "planet-beta",
                0,
                new[] { RouteHazardKind.SmallAsteroidField }));

            Assert.Throws<ArgumentException>(() => new TransportRouteDefinition(
                "route-alpha-alpha",
                "planet-alpha",
                "planet-alpha",
                60,
                new[] { RouteHazardKind.SmallAsteroidField }));
        }

        [Test]
        public void ContentDefinitions_RejectInvalidIdsAndRequiredCategories()
        {
            Assert.Throws<ArgumentException>(() => new CargoContentDefinition(
                string.Empty,
                "Cargo",
                CargoOwnership.Contract,
                CargoGrade.Common,
                CargoMaterial.CommonMetal,
                50,
                100));

            Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentContentDefinition(
                "empty-item",
                "Empty Item",
                EquipmentItemCategory.None,
                EquipmentAvailability.CommonShop,
                0,
                ContentImplementationState.Skeleton));

            Assert.Throws<ArgumentOutOfRangeException>(() => new HostileFactionContentDefinition(
                IntruderFaction.None,
                "No Faction",
                HostileFactionRelation.Neutral,
                ContentImplementationState.Skeleton));
        }

        [Test]
        public void DetailedContentCatalog_ClonesStoredArrays()
        {
            var rooms = CreateAllRoomDefinitions();
            var catalog = new DetailedContentCatalog(
                new[] { new PlanetContentDefinition("planet-alpha", "Planet Alpha", new[] { PlanetTrait.WaterRich }) },
                new[]
                {
                    new TransportRouteDefinition(
                        "route-alpha-beta",
                        "planet-alpha",
                        "planet-beta",
                        60,
                        new[] { RouteHazardKind.SmallAsteroidField })
                },
                new[]
                {
                    new ContractContentDefinition(
                        "contract-alpha",
                        "Association Freight",
                        ContractType.Association,
                        ContractDifficulty.VeryEasy,
                        "route-alpha-beta",
                        "cargo-alpha",
                        40,
                        ContentImplementationState.Skeleton)
                },
                new[]
                {
                    new CargoContentDefinition(
                        "cargo-alpha",
                        "Basic Metal Cargo",
                        CargoOwnership.Contract,
                        CargoGrade.Common,
                        CargoMaterial.CommonMetal,
                        50,
                        100)
                },
                rooms,
                new[]
                {
                    new EquipmentContentDefinition(
                        "stick",
                        "Stick",
                        EquipmentItemCategory.Weapon,
                        EquipmentAvailability.StartingLoadout,
                        0,
                        ContentImplementationState.Implemented)
                },
                new[]
                {
                    new HostileFactionContentDefinition(
                        IntruderFaction.SeedEntity,
                        "Seed Entity",
                        HostileFactionRelation.Hostile,
                        ContentImplementationState.Planned)
                },
                new[]
                {
                    new HostileUnitContentDefinition(
                        "seed-parvum",
                        "Parvum",
                        IntruderFaction.SeedEntity,
                        HostileUnitRole.ShipSaboteur,
                        IntruderObjectiveType.DestroyShip,
                        ContentImplementationState.Implemented)
                },
                new[]
                {
                    new HazardContentDefinition(
                        RouteHazardKind.SmallAsteroidField,
                        "Small Asteroid Field",
                        ContentImplementationState.Planned)
                });

            rooms[0] = new ShipRoomContentDefinition(
                ShipRoomId.Cockpit,
                "Mutated Cockpit",
                999,
                false,
                false);
            var exposedRooms = catalog.Rooms;
            exposedRooms[1] = new ShipRoomContentDefinition(
                ShipRoomId.CargoHold,
                "Mutated Cargo Hold",
                999,
                false,
                false);

            Assert.That(catalog.Rooms[0].RepairCostPerPercent, Is.EqualTo(30));
            Assert.That(catalog.Rooms[1].RepairCostPerPercent, Is.EqualTo(15));
        }

        private static DetailedContentCatalog CreateSampleCatalog()
        {
            return new DetailedContentCatalog(
                new[]
                {
                    new PlanetContentDefinition("planet-alpha", "Planet Alpha", new[] { PlanetTrait.WaterRich }),
                    new PlanetContentDefinition("planet-beta", "Planet Beta", new[] { PlanetTrait.CommonMineralRich })
                },
                new[]
                {
                    new TransportRouteDefinition(
                        "route-alpha-beta",
                        "planet-alpha",
                        "planet-beta",
                        60,
                        new[] { RouteHazardKind.SmallAsteroidField })
                },
                new[]
                {
                    new ContractContentDefinition(
                        "contract-alpha",
                        "Association Freight",
                        ContractType.Association,
                        ContractDifficulty.VeryEasy,
                        "route-alpha-beta",
                        "cargo-alpha",
                        40,
                        ContentImplementationState.Skeleton)
                },
                new[]
                {
                    new CargoContentDefinition(
                        "cargo-alpha",
                        "Basic Metal Cargo",
                        CargoOwnership.Contract,
                        CargoGrade.Common,
                        CargoMaterial.CommonMetal,
                        50,
                        100)
                },
                CreateAllRoomDefinitions(),
                new[]
                {
                    new EquipmentContentDefinition(
                        "stick",
                        "Stick",
                        EquipmentItemCategory.Weapon,
                        EquipmentAvailability.StartingLoadout,
                        0,
                        ContentImplementationState.Implemented)
                },
                new[]
                {
                    new HostileFactionContentDefinition(
                        IntruderFaction.SeedEntity,
                        "Seed Entity",
                        HostileFactionRelation.Hostile,
                        ContentImplementationState.Planned)
                },
                new[]
                {
                    new HostileUnitContentDefinition(
                        "seed-parvum",
                        "Parvum",
                        IntruderFaction.SeedEntity,
                        HostileUnitRole.ShipSaboteur,
                        IntruderObjectiveType.DestroyShip,
                        ContentImplementationState.Implemented)
                },
                new[]
                {
                    new HazardContentDefinition(
                        RouteHazardKind.SmallAsteroidField,
                        "Small Asteroid Field",
                        ContentImplementationState.Planned)
                });
        }

        private static ShipRoomContentDefinition[] CreateAllRoomDefinitions()
        {
            return new[]
            {
                new ShipRoomContentDefinition(ShipRoomId.Cockpit, "Cockpit", 30, true, true),
                new ShipRoomContentDefinition(ShipRoomId.CargoHold, "Cargo Hold", 15, true, false),
                new ShipRoomContentDefinition(ShipRoomId.Armory, "Armory", 40, true, true),
                new ShipRoomContentDefinition(ShipRoomId.EngineRoom, "Engine Room", 50, true, true),
                new ShipRoomContentDefinition(ShipRoomId.ControlRoom, "Control Room", 20, true, true),
                new ShipRoomContentDefinition(ShipRoomId.SupplyRoom, "Supply Room", 5, true, true)
            };
        }
    }
}
