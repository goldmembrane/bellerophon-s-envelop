using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class IntruderRulesTests
    {
        [Test]
        public void IntruderDefinition_StoresCommonStatsAndPriorityData()
        {
            var definition = CreateDefinition(
                IntruderObjectiveType.DestroyShip,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.EngineRoom, 2),
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.ControlRoom, 1)
                });

            var priorities = definition.TargetPriorities;

            Assert.That(definition.Faction, Is.EqualTo(IntruderFaction.SeedEntity));
            Assert.That(definition.PrimaryObjective, Is.EqualTo(IntruderObjectiveType.DestroyShip));
            Assert.That(definition.MaxHealth, Is.EqualTo(80));
            Assert.That(definition.MovementSpeed, Is.EqualTo(1.5f));
            Assert.That(definition.AttackRange, Is.EqualTo(2f));
            Assert.That(definition.AttackDelaySeconds, Is.EqualTo(1.25f));
            Assert.That(definition.MobilityKind, Is.EqualTo(IntruderMobilityKind.Walking));
            Assert.That(priorities.Length, Is.EqualTo(2));
            Assert.That(priorities[0].TargetType, Is.EqualTo(IntruderTargetType.Ship));
        }

        [Test]
        public void CreateAttempt_UsesSeededEntryRoomAndCargoObjectiveTarget()
        {
            var definition = CreateDefinition(IntruderObjectiveType.AttackCargo);

            var attempt = IntruderRules.CreateAttempt(
                "attempt-cargo",
                definition,
                42,
                ShipRoomId.Cockpit);

            Assert.That(attempt.Phase, Is.EqualTo(IntrusionPhase.Attempting));
            Assert.That(attempt.EntryRoom, Is.EqualTo(IntruderRules.SelectEntryRoom(42)));
            Assert.That(attempt.TargetType, Is.EqualTo(IntruderTargetType.Cargo));
            Assert.That(attempt.TargetRoom, Is.EqualTo(ShipRoomId.CargoHold));
        }

        [Test]
        public void ResolveAttempt_BoardedIntruderStartsActiveAndCanBeNeutralized()
        {
            var definition = CreateDefinition(IntruderObjectiveType.AttackPlayer);
            var attempt = IntruderRules.CreateAttempt(
                "attempt-player",
                definition,
                13,
                ShipRoomId.Armory);

            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, definition);
            var neutralized = intruder.WithDamage(definition.MaxHealth);

            Assert.That(boarded.Phase, Is.EqualTo(IntrusionPhase.Boarded));
            Assert.That(intruder.IsActive, Is.True);
            Assert.That(intruder.TargetType, Is.EqualTo(IntruderTargetType.Player));
            Assert.That(intruder.TargetRoom, Is.EqualTo(ShipRoomId.Armory));
            Assert.That(neutralized.IsResolved, Is.True);
            Assert.That(neutralized.Resolution, Is.EqualTo(IntruderResolution.Neutralized));
        }

        [Test]
        public void ResolveAttempt_RepelledAttemptDoesNotCreateBoardedIntruder()
        {
            var definition = CreateDefinition(IntruderObjectiveType.DestroyShip);
            var attempt = IntruderRules.CreateAttempt(
                "attempt-repelled",
                definition,
                7,
                ShipRoomId.Cockpit);

            var repelled = IntruderRules.ResolveAttempt(attempt, true);

            Assert.That(repelled.IsResolved, Is.True);
            Assert.That(repelled.Resolution, Is.EqualTo(IntruderResolution.Repelled));
            Assert.That(
                () => IntruderRules.CreateBoardedIntruder(repelled, definition),
                Throws.InvalidOperationException);
        }

        [Test]
        public void ApplyObjectivePressure_AttackCargoDamagesCargoWithoutRepairCost()
        {
            var intruder = CreateActiveIntruder(CreateDefinition(IntruderObjectiveType.AttackCargo));
            var ship = ShipState.CreateDefault();
            var cargo = new CargoState(CargoGrade.Common, 50, 100, 1f, false);

            var result = IntruderRules.ApplyObjectivePressure(intruder, ship, cargo, 10, 0.2f);

            Assert.That(result.AffectedTargetType, Is.EqualTo(IntruderTargetType.Cargo));
            Assert.That(result.Cargo.DurabilityPercent, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(ShipStateRules.CalculateRepairCost(result.Ship), Is.Zero);
            Assert.That(result.Intruder.Resolution, Is.EqualTo(IntruderResolution.ObjectiveApplied));
        }

        [Test]
        public void ApplyObjectivePressure_OccupyRoomMarksTargetRoomOffline()
        {
            var definition = CreateDefinition(
                IntruderObjectiveType.OccupyRoom,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.ControlRoom, 0)
                });
            var intruder = CreateActiveIntruder(definition);

            var result = IntruderRules.ApplyObjectivePressure(
                intruder,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 50, 100, 1f, false));

            Assert.That(result.RoomOccupied, Is.True);
            Assert.That(result.AffectedRoom, Is.EqualTo(ShipRoomId.ControlRoom));
            Assert.That(result.Ship.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline, Is.True);
        }

        [Test]
        public void ApplyObjectivePressure_AttackPlayerOnlyFlagsPlayerThreat()
        {
            var intruder = CreateActiveIntruder(CreateDefinition(IntruderObjectiveType.AttackPlayer));

            var result = IntruderRules.ApplyObjectivePressure(
                intruder,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 50, 100, 1f, false));

            Assert.That(result.ThreatensPlayer, Is.True);
            Assert.That(result.AffectedTargetType, Is.EqualTo(IntruderTargetType.Player));
            Assert.That(result.RoomDamageApplied, Is.Zero);
            Assert.That(result.CargoDamagePercentApplied, Is.Zero);
        }

        [Test]
        public void ApplyObjectivePressure_DestroyShipDamagesTargetRoom()
        {
            var definition = CreateDefinition(
                IntruderObjectiveType.DestroyShip,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.EngineRoom, 0)
                });
            var intruder = CreateActiveIntruder(definition);

            var result = IntruderRules.ApplyObjectivePressure(
                intruder,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 50, 100, 1f, false),
                25,
                0.1f);

            Assert.That(result.ThreatensShip, Is.True);
            Assert.That(result.AffectedRoom, Is.EqualTo(ShipRoomId.EngineRoom));
            Assert.That(result.RoomDamageApplied, Is.EqualTo(25));
            Assert.That(ShipStateRules.CalculateRepairCost(result.Ship), Is.GreaterThan(0));
            Assert.That(result.Cargo.DurabilityPercent, Is.EqualTo(1f));
        }

        [Test]
        public void SelectTarget_WithShipStateSkipsSealedRoomPriority()
        {
            var definition = CreateDefinition(
                IntruderObjectiveType.DestroyShip,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.EngineRoom, 0),
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.ControlRoom, 1)
                });
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.EngineRoom, ShipState.CreateDefault().GetRoom(ShipRoomId.EngineRoom).WithSealed(true));

            var target = IntruderRules.SelectTarget(definition, 5, ShipRoomId.Cockpit, ship);

            Assert.That(target.TargetType, Is.EqualTo(IntruderTargetType.Ship));
            Assert.That(target.RoomId, Is.EqualTo(ShipRoomId.ControlRoom));
        }

        [Test]
        public void AssessRoute_UsesShipMapCorridorsAndClosedCorridors()
        {
            var definition = CreateDefinition(
                IntruderObjectiveType.DestroyShip,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.Armory, 0)
                });
            var attempt = IntruderRules.CreateAttempt("route-open", definition, 0, ShipRoomId.Cockpit);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, definition);
            var openRoute = IntruderRules.AssessRoute(intruder, ShipState.CreateDefault());

            Assert.That(intruder.CurrentRoom, Is.EqualTo(ShipRoomId.Cockpit));
            Assert.That(openRoute.HasPath, Is.True);
            Assert.That(openRoute.NextRoom, Is.EqualTo(ShipRoomId.CargoHold));
            Assert.That(openRoute.RemainingStepCount, Is.EqualTo(2));

            var criticalControl = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(25, 100));
            var closedRoute = IntruderRules.AssessRoute(intruder, criticalControl);

            Assert.That(closedRoute.ClosedCorridorPercent, Is.EqualTo(90));
            Assert.That(closedRoute.ClosedCorridorCount, Is.EqualTo(9));
            Assert.That(closedRoute.HasPath, Is.False);
        }

        [Test]
        public void AssessRoute_FlyingIgnoresClosedCorridorsButNotSealedRooms()
        {
            var flying = new IntruderDefinition(
                "framework-flying",
                "Framework Flying",
                IntruderFaction.AlienLifeform,
                IntruderObjectiveType.DestroyShip,
                maxHealth: 70,
                movementSpeed: 3.5f,
                attackRange: 1f,
                attackDelaySeconds: 1f,
                targetPriorities: new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.Armory, 0)
                },
                mobilityKind: IntruderMobilityKind.Flying);
            var attempt = IntruderRules.CreateAttempt("route-flying", flying, 0, ShipRoomId.Cockpit);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, flying);
            var criticalControl = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(25, 100));

            var flyingRoute = IntruderRules.AssessRoute(intruder, criticalControl);

            Assert.That(flyingRoute.HasPath, Is.True);
            Assert.That(flyingRoute.ClosedCorridorCount, Is.Zero);

            var sealedTarget = criticalControl
                .WithRoom(ShipRoomId.Armory, criticalControl.GetRoom(ShipRoomId.Armory).WithSealed(true));
            var sealedRoute = IntruderRules.AssessRoute(intruder, sealedTarget);

            Assert.That(sealedRoute.HasPath, Is.False);
            Assert.That(sealedRoute.BlockedBySealedRoom, Is.True);
        }

        [Test]
        public void AssessEnvironment_ReflectsControlRoomCctvDetectionSuppressionAndBlackout()
        {
            var definition = CreateDefinition(IntruderObjectiveType.DestroyShip);
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(50, 100));

            var assessment = IntruderRules.AssessEnvironment(definition, ship, 3);

            Assert.That(assessment.ClosedCorridorPercent, Is.Zero);
            Assert.That(assessment.BlackoutRoomCount, Is.EqualTo(5));
            Assert.That(assessment.AvailableCctvCount, Is.Zero);
            Assert.That(assessment.IntruderDetectionOnline, Is.False);
            Assert.That(assessment.IntruderSuppressionOnline, Is.False);
            Assert.That(assessment.StatMultiplier, Is.EqualTo(ShipStateRules.ControlRoomDestroyedIntruderStatMultiplier));
            Assert.That(assessment.EffectiveMovementSpeed, Is.EqualTo(4.5f));
            Assert.That(assessment.EffectiveRoomDamage, Is.EqualTo(9));
        }

        [Test]
        public void DetermineRelation_UsesSourceFactionRelationshipRules()
        {
            var cargoBond = IntruderRules.DetermineRelation(
                IntruderFaction.CargoFreedomLeague,
                IntruderFaction.CargoFreedomLeague);
            var pirateBond = IntruderRules.DetermineRelation(
                IntruderFaction.SpacePirate,
                IntruderFaction.SpacePirate);
            var seedCompetition = IntruderRules.DetermineRelation(
                IntruderFaction.SeedEntity,
                IntruderFaction.SeedEntity);
            var alienAlliance = IntruderRules.DetermineRelation(
                IntruderFaction.AlienLifeform,
                IntruderFaction.AlienLifeform);
            var seedAlienHostility = IntruderRules.DetermineRelation(
                IntruderFaction.SeedEntity,
                IntruderFaction.AlienLifeform);
            var alienPirateCompetition = IntruderRules.DetermineRelation(
                IntruderFaction.AlienLifeform,
                IntruderFaction.SpacePirate);
            var cargoPirateHostility = IntruderRules.DetermineRelation(
                IntruderFaction.CargoFreedomLeague,
                IntruderFaction.SpacePirate);

            Assert.That(cargoBond.RelationKind, Is.EqualTo(IntruderRelationKind.Bonded));
            Assert.That(cargoBond.MarkerKind, Is.EqualTo(IntruderRelationMarkerKind.GreenCircle));
            Assert.That(cargoBond.FriendlyFireDamagesHealth, Is.False);
            Assert.That(cargoBond.FriendlyFireAppliesStatusEffects, Is.False);
            Assert.That(pirateBond.RelationKind, Is.EqualTo(IntruderRelationKind.Bonded));
            Assert.That(seedCompetition.RelationKind, Is.EqualTo(IntruderRelationKind.Competitive));
            Assert.That(seedCompetition.MarkerKind, Is.EqualTo(IntruderRelationMarkerKind.GrayCircle));
            Assert.That(seedCompetition.FriendlyFireDamagesHealth, Is.True);
            Assert.That(seedCompetition.FriendlyFireAppliesStatusEffects, Is.True);
            Assert.That(alienAlliance.RelationKind, Is.EqualTo(IntruderRelationKind.Allied));
            Assert.That(alienAlliance.FriendlyFireDamagesHealth, Is.False);
            Assert.That(alienAlliance.FriendlyFireAppliesStatusEffects, Is.True);
            Assert.That(seedAlienHostility.RelationKind, Is.EqualTo(IntruderRelationKind.Hostile));
            Assert.That(seedAlienHostility.CanDirectlyAttack, Is.True);
            Assert.That(alienPirateCompetition.RelationKind, Is.EqualTo(IntruderRelationKind.Competitive));
            Assert.That(cargoPirateHostility.RelationKind, Is.EqualTo(IntruderRelationKind.Hostile));
        }

        [Test]
        public void DetermineRelation_CommandingDefinitionUsesCommandedRelationForSameFaction()
        {
            var commander = new IntruderDefinition(
                "framework-commander",
                "Framework Commander",
                IntruderFaction.SpacePirate,
                IntruderObjectiveType.DestroyShip,
                maxHealth: 120,
                movementSpeed: 1.5f,
                attackRange: 3f,
                attackDelaySeconds: 1.5f,
                targetPriorities: null,
                issuesFactionCommands: true);
            var subordinate = new IntruderDefinition(
                "framework-subordinate",
                "Framework Subordinate",
                IntruderFaction.SpacePirate,
                IntruderObjectiveType.AttackPlayer,
                maxHealth: 80,
                movementSpeed: 2f,
                attackRange: 2f,
                attackDelaySeconds: 1f,
                targetPriorities: null);

            var relation = IntruderRules.DetermineRelation(commander, subordinate);

            Assert.That(relation.RelationKind, Is.EqualTo(IntruderRelationKind.Commanded));
            Assert.That(relation.CanDirectlyAttack, Is.False);
            Assert.That(relation.FriendlyFireDamagesHealth, Is.False);
            Assert.That(relation.FriendlyFireAppliesStatusEffects, Is.False);
        }

        private static IntruderEntityState CreateActiveIntruder(IntruderDefinition definition)
        {
            var attempt = IntruderRules.CreateAttempt(
                "attempt-" + definition.PrimaryObjective,
                definition,
                100,
                ShipRoomId.Cockpit);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            return IntruderRules.CreateBoardedIntruder(boarded, definition).MoveToTargetRoom();
        }

        private static IntruderDefinition CreateDefinition(
            IntruderObjectiveType objective,
            IntruderTargetPriority[] priorities = null)
        {
            return new IntruderDefinition(
                "framework-" + objective,
                "Framework " + objective,
                IntruderFaction.SeedEntity,
                objective,
                maxHealth: 80,
                movementSpeed: 1.5f,
                attackRange: 2f,
                attackDelaySeconds: 1.25f,
                targetPriorities: priorities);
        }
    }
}
