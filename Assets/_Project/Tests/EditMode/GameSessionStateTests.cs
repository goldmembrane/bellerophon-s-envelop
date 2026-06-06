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

        [Test]
        public void Session_TowingIncidentCountIncrementsWhenSettlementRequiresTowing()
        {
            var towedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));
            var wallet = new WalletState(5000, false);
            var session = GameSessionState.StartSession(wallet).StartTransport();

            var completed = session.CompleteTransport(
                CreateSettlementInput(
                    wallet,
                    towedShip,
                    towingCost: ShipStateRules.CalculateTowingCost(session.TowingIncidentCount + 1)));

            Assert.That(completed.TowingIncidentCount, Is.EqualTo(1));
            Assert.That(completed.SettlementResult.RequiresTowing, Is.True);
            Assert.That(completed.SettlementResult.Expenses, Is.EqualTo(2000));
        }

        [Test]
        public void Session_DurabilityUpgradePurchaseAutoEquips()
        {
            var wallet = new WalletState(1000, false);
            var completed = GameSessionState.StartSession(wallet)
                .StartTransport()
                .CompleteTransport(CreateSettlementInput(wallet, ShipState.CreateDefault()));

            var purchase = completed.PurchaseShipUpgrade(ShipUpgradeCategory.Durability);
            var equip = purchase.State.EquipShipUpgrade(ShipUpgradeCategory.Durability);

            Assert.That(purchase.Purchased, Is.True);
            Assert.That(purchase.SpentCredits, Is.EqualTo(1000));
            Assert.That(purchase.State.Wallet.Credits, Is.EqualTo(100));
            Assert.That(purchase.State.ShipUpgrades.GetPurchasedTier(ShipUpgradeCategory.Durability), Is.EqualTo(1));
            Assert.That(purchase.State.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.Durability), Is.EqualTo(1));
            Assert.That(equip.Equipped, Is.False);
            Assert.That(equip.State.Wallet.Credits, Is.EqualTo(100));
            Assert.That(equip.State.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.Durability), Is.EqualTo(1));
        }

        [Test]
        public void Session_NonDurabilityUpgradePurchaseAndEquipAreSeparate()
        {
            var wallet = new WalletState(3000, false);
            var completed = GameSessionState.StartSession(wallet)
                .StartTransport()
                .CompleteTransport(CreateSettlementInput(wallet, ShipState.CreateDefault()));

            var purchase = completed.PurchaseShipUpgrade(ShipUpgradeCategory.SupplySlots);
            var equip = purchase.State.EquipShipUpgrade(ShipUpgradeCategory.SupplySlots);

            Assert.That(purchase.Purchased, Is.True);
            Assert.That(purchase.State.Wallet.Credits, Is.EqualTo(2100));
            Assert.That(purchase.State.ShipUpgrades.GetPurchasedTier(ShipUpgradeCategory.SupplySlots), Is.EqualTo(1));
            Assert.That(purchase.State.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.SupplySlots), Is.Zero);
            Assert.That(equip.Equipped, Is.True);
            Assert.That(equip.State.Wallet.Credits, Is.EqualTo(2100));
            Assert.That(equip.State.ShipUpgrades.GetEquippedTier(ShipUpgradeCategory.SupplySlots), Is.EqualTo(1));
        }

        [Test]
        public void Session_AcceptsMultipleContractsBeforeStartingTransport()
        {
            var wallet = new WalletState(50, false);
            var completed = GameSessionState.StartSession(wallet)
                .StartTransport()
                .CompleteTransport(CreateSettlementInput(wallet, ShipState.CreateDefault()));
            var association = TransportContractDefinition.CreateAssociationFollowUp();
            var privateContract = TransportContractDefinition.CreatePrivateFollowUp();

            var accepted = completed
                .AcceptTransportContract(association)
                .AcceptTransportContract(association)
                .AcceptTransportContract(privateContract);
            var started = accepted.StartAcceptedTransportContracts();

            Assert.That(accepted.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(accepted.PendingTransportContractCount, Is.EqualTo(2));
            Assert.That(accepted.IsTransportContractPending("association-local-001"), Is.True);
            Assert.That(accepted.IsTransportContractPending("private-sample-001"), Is.True);
            Assert.That(started.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(started.PendingTransportContractCount, Is.Zero);
            Assert.That(started.ActiveTransportContractCount, Is.EqualTo(2));
            Assert.That(started.ActiveTransportContract.Value.Id, Is.EqualTo("association-local-001"));
            Assert.That(started.ActiveTransportContracts[1].Id, Is.EqualTo("private-sample-001"));
            Assert.That(started.ActiveCargo.HasValue, Is.True);
        }

        private static SettlementInput CreateSettlementInput(
            WalletState wallet,
            ShipState ship,
            int repairCost = 0,
            int cargoLossPenalty = 0,
            int towingCost = 0)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Normal,
                new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ship,
                new CrewState(1, 0),
                wallet,
                repairCost: repairCost,
                cargoLossPenalty: cargoLossPenalty,
                towingCost: towingCost);
        }
    }
}
