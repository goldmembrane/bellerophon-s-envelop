using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ShipCargoStateTests
    {
        [Test]
        public void ShipState_AverageDurabilityUsesAllSixRooms()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(50, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(25, 100));

            Assert.That(ship.AverageDurabilityPercent, Is.EqualTo(475f / 600f).Within(0.0001f));
        }

        [Test]
        public void RepairCost_UsesSettlementSummaryMissingDurabilityRate()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(90, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));

            var repairCost = ShipStateRules.CalculateRepairCost(ship);

            Assert.That(repairCost, Is.EqualTo(800));
        }

        [Test]
        public void RepairCost_CapsNormalRepairAt599PercentAndSeparatesTotalLoss()
        {
            var nearTotalLoss = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(1, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));
            var totalLoss = nearTotalLoss.WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100));

            Assert.That(nearTotalLoss.IsTotalLoss, Is.False);
            Assert.That(ShipStateRules.CalculateMissingDurabilityPercent(nearTotalLoss), Is.EqualTo(599));
            Assert.That(ShipStateRules.CalculateRepairCost(nearTotalLoss), Is.EqualTo(2995));
            Assert.That(totalLoss.IsTotalLoss, Is.True);
            Assert.That(ShipStateRules.CalculateRepairCost(totalLoss), Is.Zero);
            Assert.That(ShipStateRules.CalculateTotalLossClaimCost(totalLoss), Is.EqualTo(5000));
        }

        [TestCase(1, 2000)]
        [TestCase(2, 3000)]
        [TestCase(3, 5000)]
        [TestCase(4, 7500)]
        [TestCase(5, 10000)]
        public void TowingCost_FollowsIncidentNumber(int incidentNumber, int expectedCost)
        {
            Assert.That(ShipStateRules.CalculateTowingCost(incidentNumber), Is.EqualTo(expectedCost));
        }

        [Test]
        public void RepairAllRooms_RestoresDurabilityAndClearsDamageFlags()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(10, 100, true, true, true))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));

            var repaired = ShipStateRules.RepairAllRooms(ship);

            Assert.That(repaired.RunState, Is.EqualTo(ShipRunState.Docked));
            Assert.That(repaired.GetRoom(ShipRoomId.ControlRoom).CurrentDurability, Is.EqualTo(100));
            Assert.That(repaired.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline, Is.False);
            Assert.That(repaired.GetRoom(ShipRoomId.ControlRoom).IsBlackout, Is.False);
            Assert.That(repaired.GetRoom(ShipRoomId.ControlRoom).IsSealed, Is.False);
            Assert.That(repaired.GetRoom(ShipRoomId.EngineRoom).CurrentDurability, Is.EqualTo(100));
            Assert.That(repaired.RequiresTowing, Is.False);
        }

        [Test]
        public void CargoState_DamageUpdatesDurabilityAndLossPercent()
        {
            var cargo = new CargoState(CargoGrade.Rare, 100, 1000, 0.8f, false);

            var damaged = cargo.WithDamagePercent(0.35f);

            Assert.That(damaged.DurabilityPercent, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(damaged.LossPercent, Is.EqualTo(0.55f).Within(0.0001f));
        }

        [Test]
        public void CargoHoldCritical_AppliesCargoLossAndBlocksStart()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(25, 100));

            var lossPercent = ShipStateRules.CalculateCargoLossPercentFromCargoHold(ship);
            var startReadiness = ShipStateRules.EvaluateStartReadiness(ship);

            Assert.That(lossPercent, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(startReadiness.CanStartTransport, Is.False);
            Assert.That(startReadiness.IsCargoHoldBlocked, Is.True);
        }

        [Test]
        public void CargoHoldDestroyed_DamagesCargoOverTime()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(0, 100));
            var cargo = new CargoState(CargoGrade.Common, 50, 500, 0.5f, false);

            var damaged = ShipStateRules.ApplyCargoHoldDamageOverTime(cargo, ship, 30f);

            Assert.That(damaged.DurabilityPercent, Is.EqualTo(0.47f).Within(0.0001f));
        }

        [Test]
        public void StartReadiness_EngineAndCockpitDestroyedBlockStartButControlRoomOnlyWarns()
        {
            var blocked = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));
            var warningOnly = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));

            var blockedReadiness = ShipStateRules.EvaluateStartReadiness(blocked);
            var warningReadiness = ShipStateRules.EvaluateStartReadiness(warningOnly);

            Assert.That(blockedReadiness.CanStartTransport, Is.False);
            Assert.That(blockedReadiness.IsCockpitDestroyed, Is.True);
            Assert.That(blockedReadiness.IsEngineRoomDestroyed, Is.True);
            Assert.That(warningReadiness.CanStartTransport, Is.True);
            Assert.That(warningReadiness.HasControlRoomDestroyedWarning, Is.True);
        }

        [Test]
        public void SixRoomDamageEffects_FollowThresholdRules()
        {
            var personalCargoBlocked = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100));
            var cargoCritical = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(25, 100));
            var engineStable = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(75, 100));
            var engineDamaged = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(50, 100));
            var engineCritical = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(25, 100));
            var cockpitStable = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(75, 100));
            var cockpitCritical = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(25, 100));
            var armoryStable = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(75, 100));
            var armoryDamaged = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(50, 100));
            var armoryCritical = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(25, 100));
            var armoryDestroyed = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100));
            var controlDamaged = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(50, 100));
            var controlCritical = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(25, 100));
            var controlDestroyed = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));
            var supplyStable = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(75, 100));
            var supplyDamaged = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(50, 100));
            var supplyCritical = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(25, 100));
            var supplyDestroyed = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(0, 100));

            Assert.That(ShipStateRules.EvaluateStartReadiness(personalCargoBlocked).CanStartTransport, Is.True);
            Assert.That(ShipStateRules.EvaluateStartReadiness(personalCargoBlocked, true).IsPersonalCargoBlocked, Is.True);
            Assert.That(ShipStateRules.EvaluateStartReadiness(personalCargoBlocked, true).CanStartTransport, Is.False);
            Assert.That(ShipStateRules.CalculateCargoLossPercentFromCargoHold(cargoCritical), Is.EqualTo(0.2f).Within(0.0001f));

            Assert.That(ShipStateRules.CalculateEffectiveTransportDurationSeconds(60, engineStable), Is.EqualTo(90));
            Assert.That(ShipStateRules.CalculateEffectiveTransportDurationSeconds(60, engineDamaged), Is.EqualTo(150));
            Assert.That(ShipStateRules.CalculateEffectiveTransportDurationSeconds(60, engineCritical), Is.EqualTo(210));
            Assert.That(ShipStateRules.CalculateEngineBlackoutRoomCount(engineDamaged), Is.EqualTo(5));
            Assert.That(ShipStateRules.CalculateEngineOfflineRoomCount(engineCritical), Is.EqualTo(5));
            Assert.That(ShipStateRules.CanUseEngineOverclock(engineDamaged), Is.False);

            Assert.That(ShipStateRules.CalculateManualFlightInputMultiplier(cockpitStable), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(ShipStateRules.CalculateManualFlightInputMultiplier(cockpitCritical), Is.EqualTo(0.5f).Within(0.0001f));

            Assert.That(ShipStateRules.IsPlasmaCannonAvailable(armoryStable), Is.False);
            Assert.That(ShipStateRules.CalculateActiveAutoTurretCount(armoryDamaged, 3), Is.EqualTo(2));
            Assert.That(ShipStateRules.CalculateManualTurretAimMultiplier(armoryCritical), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(ShipStateRules.CanUseManualTurret(armoryDestroyed), Is.False);

            Assert.That(ShipStateRules.CalculateControlRoomClosedCorridorPercent(controlDamaged), Is.EqualTo(50));
            Assert.That(ShipStateRules.CalculateControlRoomAvailableCctvCount(controlDamaged), Is.EqualTo(3));
            Assert.That(ShipStateRules.IsIntruderDetectionOnline(controlDamaged), Is.False);
            Assert.That(ShipStateRules.CalculateControlRoomAvailableCctvCount(controlCritical), Is.Zero);
            Assert.That(ShipStateRules.IsCargoDamageWarningOnline(controlCritical), Is.False);
            Assert.That(ShipStateRules.IsIntruderSuppressionOnline(controlDestroyed), Is.False);
            Assert.That(ShipStateRules.CalculateSeedIntruderOccurrencePercent(15, controlDestroyed), Is.EqualTo(30));
            Assert.That(ShipStateRules.CalculateInternalIntruderRoomDamage(3, controlDestroyed), Is.EqualTo(9));

            Assert.That(ShipStateRules.CalculateSupplyStorageSlotCount(supplyStable, 3), Is.Zero);
            Assert.That(ShipStateRules.IsSupplyStorageSecurityOnline(supplyDamaged), Is.False);
            Assert.That(ShipStateRules.CalculateSupplyEquipmentDurabilityDamagePercent(supplyCritical), Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(ShipStateRules.CalculateSupplyEquipmentExplosionChancePercent(supplyDestroyed), Is.EqualTo(25));
        }

        [Test]
        public void TotalLossClaim_AppliesWhenAllRoomsAreDestroyed()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));

            Assert.That(ship.IsTotalLoss, Is.True);
            Assert.That(ShipStateRules.CalculateTotalLossClaimCost(ship), Is.EqualTo(5000));
        }
    }
}
