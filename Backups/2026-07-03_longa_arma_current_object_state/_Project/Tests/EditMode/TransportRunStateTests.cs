using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class TransportRunStateTests
    {
        [Test]
        public void TransportRun_DefaultsToAutoPilotAndTracksRemainingTime()
        {
            var run = TransportRunState.Start(60, ShipState.CreateDefault());

            var ticked = run.Tick(15f);

            Assert.That(run.FlightMode, Is.EqualTo(ShipFlightMode.AutoPilot));
            Assert.That(ticked.ProgressPercent, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(ticked.RemainingSeconds, Is.EqualTo(45f).Within(0.0001f));
        }

        [Test]
        public void TransportRun_CockpitAtFiftyPercentDisablesAutoPilot()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(50, 100));

            var run = TransportRunState.Start(60, ship);

            Assert.That(run.IsAutoPilotAvailable, Is.False);
            Assert.That(run.FlightMode, Is.EqualTo(ShipFlightMode.ManualFlight));
        }

        [Test]
        public void TransportRun_ManualInputMovesWithinAvoidanceField()
        {
            var run = TransportRunState.Start(60, ShipState.CreateDefault())
                .EnterManualFlight();

            var moved = run.ApplyManualFlightInput(1f, -1f, 0.5f);
            var clamped = moved.ApplyManualFlightInput(1f, -1f, 10f);

            Assert.That(moved.ManualOffsetX, Is.GreaterThan(0f));
            Assert.That(moved.ManualOffsetY, Is.LessThan(0f));
            Assert.That(clamped.ManualOffsetX, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(clamped.ManualOffsetY, Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void TransportRun_CockpitDamageReducesManualInputResponse()
        {
            var damagedCockpit = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(75, 100));
            var criticalCockpit = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(25, 100));
            var destroyedCockpit = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100));

            var damagedRun = TransportRunState.Start(60, damagedCockpit)
                .EnterManualFlight()
                .ApplyManualFlightInput(1f, 0f, 0.5f);
            var criticalRun = TransportRunState.Start(60, criticalCockpit)
                .EnterManualFlight()
                .ApplyManualFlightInput(1f, 0f, 0.5f);
            var destroyedRun = TransportRunState.Start(60, destroyedCockpit)
                .ApplyManualFlightInput(1f, 0f, 0.5f);

            Assert.That(damagedRun.ManualOffsetX, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(criticalRun.ManualOffsetX, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(destroyedRun.ManualOffsetX, Is.Zero);
        }

        [Test]
        public void ShipRules_CockpitDestroyedBlocksStartAndDoublesDurationRule()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100));

            var readiness = ShipStateRules.EvaluateStartReadiness(ship);
            var duration = ShipStateRules.CalculateEffectiveTransportDurationSeconds(60, ship);

            Assert.That(readiness.CanStartTransport, Is.False);
            Assert.That(readiness.IsCockpitDestroyed, Is.True);
            Assert.That(duration, Is.EqualTo(120));
        }
    }
}
