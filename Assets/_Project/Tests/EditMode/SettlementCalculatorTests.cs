using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class SettlementCalculatorTests
    {
        [TestCase(100, ShipRoomDurabilityTier.Optimal)]
        [TestCase(75, ShipRoomDurabilityTier.Stable)]
        [TestCase(50, ShipRoomDurabilityTier.Damaged)]
        [TestCase(25, ShipRoomDurabilityTier.Critical)]
        [TestCase(0, ShipRoomDurabilityTier.Destroyed)]
        public void RoomDurability_UsesExpectedThresholds(int currentDurability, ShipRoomDurabilityTier expectedTier)
        {
            var room = new ShipRoomState(currentDurability, 100);

            Assert.That(room.DurabilityTier, Is.EqualTo(expectedTier));
        }

        [Test]
        public void EngineRoomDestroyed_RequiresTowingAndFailsTransport()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));
            var input = CreateInput(ship, towingCost: 40);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.RequiresTowing, Is.True);
            Assert.That(result.IsTransportFailed, Is.True);
            Assert.That(result.GrossRevenue, Is.EqualTo(0));
            Assert.That(result.Expenses, Is.EqualTo(40));
        }

        [Test]
        public void CargoHoldScore_FollowsCargoHoldDurability()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100));

            var score = SettlementCalculator.CalculateCargoHoldScore(ship);

            Assert.That(score, Is.EqualTo(0.5f));
        }

        [Test]
        public void ContractRewards_ApplyAssociationAndPrivateMultipliers()
        {
            var ship = ShipState.CreateDefault();
            var association = SettlementCalculator.Calculate(CreateInput(ship, ContractType.Association));
            var privateContract = SettlementCalculator.Calculate(CreateInput(ship, ContractType.Private));

            Assert.That(association.GrossRevenue, Is.EqualTo(100));
            Assert.That(privateContract.GrossRevenue, Is.EqualTo(135));
        }

        [Test]
        public void Settlement_FirstNegativeBalanceActivatesDebtGraceInsteadOfImmediateGameOver()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                wallet: new WalletState(10, false),
                cargoLossPenalty: 150);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.FinalBalance, Is.EqualTo(-40));
            Assert.That(result.IsGameOver, Is.False);
            Assert.That(result.DebtStatus, Is.EqualTo(SettlementDebtStatus.GraceActive));
            Assert.That(result.RequiresDebtGrace, Is.True);
        }

        [Test]
        public void Settlement_SecondNegativeBalanceTriggersFinalGameOver()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                wallet: new WalletState(-40, false, true),
                cargoLossPenalty: 150);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.FinalBalance, Is.EqualTo(-90));
            Assert.That(result.IsGameOver, Is.True);
            Assert.That(result.DebtStatus, Is.EqualTo(SettlementDebtStatus.FinalGameOver));
        }

        [Test]
        public void RepairCost_IsPendingAndDoesNotReduceFinalBalance()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                repairCost: 150);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.Expenses, Is.EqualTo(0));
            Assert.That(result.NetChange, Is.EqualTo(100));
            Assert.That(result.FinalBalance, Is.EqualTo(100));
            Assert.That(result.PendingRepairCost, Is.EqualTo(150));
            Assert.That(result.DebtStatus, Is.EqualTo(SettlementDebtStatus.Clear));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Ship repair cost" &&
                item.Amount == -150 &&
                !item.AffectsBalance));
        }

        [Test]
        public void TotalLossClaim_IsPendingRepairCostAndDoesNotTriggerDebtGrace()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));
            var input = CreateInput(
                ship,
                wallet: new WalletState(1000, false),
                repairCost: ShipStateRules.CalculateTotalLossClaimCost(ship));

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.Expenses, Is.EqualTo(0));
            Assert.That(result.PendingRepairCost, Is.EqualTo(5000));
            Assert.That(result.IsGameOver, Is.False);
            Assert.That(result.DebtStatus, Is.EqualTo(SettlementDebtStatus.Clear));
        }

        [Test]
        public void PersonalCargo_AppliesPlanetSaleMultiplier()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                cargo: new CargoState(CargoGrade.Common, 1, 100, 1f, true),
                personalCargoSaleMultiplier: 1.5f);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.PersonalCargoSaleMultiplier, Is.EqualTo(1.5f));
            Assert.That(result.GrossRevenue, Is.EqualTo(150));
        }

        [Test]
        public void TutorialSettlement_AppliesFixedRewardAndAssociationSupportBonus()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                contractBasePay: 1000,
                repairSupportAmount: 100);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.GrossRevenue, Is.EqualTo(1100));
            Assert.That(result.FinalBalance, Is.EqualTo(1100));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Contract reward" && item.Amount == 1000));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Association support bonus" && item.Amount == 100));
        }

        [Test]
        public void ShipLossInsurancePayout_FollowsAverageDurabilityBands()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(40, 100));

            Assert.That(ShipStateRules.CalculateShipLossInsurancePayout(ship), Is.EqualTo(500));
        }

        [Test]
        public void Settlement_LineItemsIncludeCargoPenaltyCrewCostsAndAssociationFees()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                cargo: new CargoState(CargoGrade.Common, 1, 100, 0.5f, false),
                repairCost: 30,
                revivalCostPerDeadCrew: 300,
                survivorCount: 0,
                deadCount: 2,
                cleaningCostWhenNoSurvivors: 250,
                associationBrokerageFee: 40,
                associationMaintenanceFee: 100);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.Expenses, Is.EqualTo(50 + 600 + 250 + 40 + 100));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Ship repair cost" && item.Amount == -30 && !item.AffectsBalance));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Cargo loss penalty" && item.Amount == -50));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Dead crew life insurance" && item.Amount == -600));
            Assert.That(result.LineItems, Has.Some.Matches<SettlementLineItem>(item =>
                item.Label == "Association maintenance fee" && item.Amount == -100));
        }

        private static SettlementInput CreateInput(
            ShipState ship,
            ContractType contractType = ContractType.Association,
            CargoState? cargo = null,
            WalletState? wallet = null,
            int repairCost = 0,
            int towingCost = 0,
            float personalCargoSaleMultiplier = 1f,
            int revivalCostPerDeadCrew = 0,
            int survivorCount = 1,
            int deadCount = 0,
            int cleaningCostWhenNoSurvivors = 0,
            int associationBrokerageFee = 0,
            int associationMaintenanceFee = 0,
            int contractBasePay = 0,
            int repairSupportAmount = 0,
            int cargoLossPenalty = 0)
        {
            return new SettlementInput(
                contractType,
                ContractDifficulty.Normal,
                cargo ?? new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ship,
                new CrewState(survivorCount, deadCount),
                wallet ?? new WalletState(0, false),
                repairCost: repairCost,
                towingCost: towingCost,
                revivalCostPerDeadCrew: revivalCostPerDeadCrew,
                personalCargoSaleMultiplier: personalCargoSaleMultiplier,
                cleaningCostWhenNoSurvivors: cleaningCostWhenNoSurvivors,
                associationBrokerageFee: associationBrokerageFee,
                associationMaintenanceFee: associationMaintenanceFee,
                contractBasePay: contractBasePay,
                repairSupportAmount: repairSupportAmount,
                cargoLossPenalty: cargoLossPenalty);
        }
    }
}
