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
        public void Settlement_GameOverWhenFinalBalanceIsNegativeAndDebtIsDisabled()
        {
            var input = CreateInput(
                ShipState.CreateDefault(),
                wallet: new WalletState(10, false),
                repairCost: 150);

            var result = SettlementCalculator.Calculate(input);

            Assert.That(result.FinalBalance, Is.EqualTo(-40));
            Assert.That(result.IsGameOver, Is.True);
        }

        [Test]
        public void TotalLossClaim_CanFeedSettlementGameOver()
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

            Assert.That(result.Expenses, Is.EqualTo(5000));
            Assert.That(result.IsGameOver, Is.True);
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

        private static SettlementInput CreateInput(
            ShipState ship,
            ContractType contractType = ContractType.Association,
            CargoState? cargo = null,
            WalletState? wallet = null,
            int repairCost = 0,
            int towingCost = 0,
            float personalCargoSaleMultiplier = 1f)
        {
            return new SettlementInput(
                contractType,
                ContractDifficulty.Normal,
                cargo ?? new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ship,
                new CrewState(1, 0),
                wallet ?? new WalletState(0, false),
                repairCost: repairCost,
                towingCost: towingCost,
                personalCargoSaleMultiplier: personalCargoSaleMultiplier);
        }
    }
}
