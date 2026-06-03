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
        public void RepairCost_UsesPerRoomRepairInputs()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(90, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));

            var repairCost = ShipStateRules.CalculateRepairCost(ship);

            Assert.That(repairCost, Is.EqualTo(6050));
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
