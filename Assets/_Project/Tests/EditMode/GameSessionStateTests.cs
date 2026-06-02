using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class GameSessionStateTests
    {
        [Test]
        public void Session_CanStartTransportAndComplete()
        {
            var wallet = new WalletState(50, false);
            var session = GameSessionState.StartSession(wallet).StartTransport();
            var input = CreateSettlementInput(wallet, ShipState.CreateDefault());

            var completed = session.CompleteTransport(input);

            Assert.That(session.Ship.RunState, Is.EqualTo(ShipRunState.InTransit));
            Assert.That(completed.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(completed.Ship.RunState, Is.EqualTo(ShipRunState.Completed));
            Assert.That(completed.SettlementResult.GrossRevenue, Is.EqualTo(100));
            Assert.That(completed.Wallet.Credits, Is.EqualTo(150));
        }

        [Test]
        public void Session_FailTransportRecordsFailedPhase()
        {
            var wallet = new WalletState(50, false);
            var session = GameSessionState.StartSession(wallet).StartTransport();
            var input = CreateSettlementInput(wallet, ShipState.CreateDefault(), repairCost: 25);

            var failed = session.FailTransport(input);

            Assert.That(failed.Phase, Is.EqualTo(GameSessionPhase.Failed));
            Assert.That(failed.Ship.RunState, Is.EqualTo(ShipRunState.Failed));
            Assert.That(failed.SettlementResult.IsTransportFailed, Is.True);
            Assert.That(failed.SettlementResult.NetChange, Is.EqualTo(-25));
        }

        private static SettlementInput CreateSettlementInput(
            WalletState wallet,
            ShipState ship,
            int repairCost = 0)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Normal,
                new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ship,
                new CrewState(1, 0),
                wallet,
                repairCost: repairCost);
        }
    }
}
