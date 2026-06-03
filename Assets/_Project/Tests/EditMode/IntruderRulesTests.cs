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
