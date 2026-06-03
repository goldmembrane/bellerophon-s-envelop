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
            Assert.That(completed.CompletedTransportCount, Is.EqualTo(1));
        }

        [Test]
        public void Session_FailTransportRecordsFailedPhase()
        {
            var wallet = new WalletState(50, false);
            var session = GameSessionState.StartSession(wallet).StartTransport();
            var input = CreateSettlementInput(wallet, ShipState.CreateDefault(), cargoLossPenalty: 25);

            var failed = session.FailTransport(input);

            Assert.That(failed.Phase, Is.EqualTo(GameSessionPhase.Failed));
            Assert.That(failed.Ship.RunState, Is.EqualTo(ShipRunState.Failed));
            Assert.That(failed.SettlementResult.IsTransportFailed, Is.True);
            Assert.That(failed.SettlementResult.NetChange, Is.EqualTo(-25));
        }

        [Test]
        public void Session_FirstNegativeSettlementAllowsNextTransport()
        {
            var wallet = new WalletState(0, false);
            var session = GameSessionState.StartSession(wallet).StartTransport();
            var input = CreateSettlementInput(wallet, ShipState.CreateDefault(), cargoLossPenalty: 150);

            var completed = session.CompleteTransport(input);
            var nextTransport = completed.StartTransport();

            Assert.That(completed.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(completed.Wallet.Credits, Is.EqualTo(-50));
            Assert.That(completed.Wallet.HasUnpaidDebtGrace, Is.True);
            Assert.That(completed.SettlementResult.DebtStatus, Is.EqualTo(SettlementDebtStatus.GraceActive));
            Assert.That(nextTransport.Phase, Is.EqualTo(GameSessionPhase.Transporting));
        }

        [Test]
        public void Session_SecondNegativeSettlementEntersGameOver()
        {
            var firstRun = GameSessionState.StartSession(new WalletState(0, false)).StartTransport();
            var firstDebt = firstRun.CompleteTransport(
                CreateSettlementInput(firstRun.Wallet, ShipState.CreateDefault(), cargoLossPenalty: 150));
            var secondRun = firstDebt.StartTransport();

            var gameOver = secondRun.CompleteTransport(
                CreateSettlementInput(secondRun.Wallet, ShipState.CreateDefault(), cargoLossPenalty: 150));

            Assert.That(gameOver.Phase, Is.EqualTo(GameSessionPhase.GameOver));
            Assert.That(gameOver.Ship.RunState, Is.EqualTo(ShipRunState.Completed));
            Assert.That(gameOver.SettlementResult.IsGameOver, Is.True);
            Assert.That(gameOver.SettlementResult.DebtStatus, Is.EqualTo(SettlementDebtStatus.FinalGameOver));
        }

        [Test]
        public void Session_MaintenanceRepairChargesPendingCostAndRestoresShip()
        {
            var damagedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(50, 100));
            var wallet = new WalletState(0, false);
            var session = GameSessionState.StartSession(wallet).StartTransport();
            var completed = session.CompleteTransport(
                CreateSettlementInput(wallet, damagedShip, repairCost: 150));

            var repaired = completed.ApplyMaintenanceRepair(completed.SettlementResult.PendingRepairCost);

            Assert.That(repaired.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(repaired.Wallet.Credits, Is.EqualTo(-50));
            Assert.That(repaired.Wallet.HasUnpaidDebtGrace, Is.True);
            Assert.That(repaired.SettlementResult.PendingRepairCost, Is.EqualTo(0));
            Assert.That(repaired.Ship.RunState, Is.EqualTo(ShipRunState.Docked));
            Assert.That(repaired.Ship.GetRoom(ShipRoomId.Armory).CurrentDurability, Is.EqualTo(100));
        }

        private static SettlementInput CreateSettlementInput(
            WalletState wallet,
            ShipState ship,
            int repairCost = 0,
            int cargoLossPenalty = 0)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Normal,
                new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ship,
                new CrewState(1, 0),
                wallet,
                repairCost: repairCost,
                cargoLossPenalty: cargoLossPenalty);
        }
    }
}
