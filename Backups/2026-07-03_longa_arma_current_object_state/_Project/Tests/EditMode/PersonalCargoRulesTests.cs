using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class PersonalCargoRulesTests
    {
        [Test]
        public void PersonalCargo_LoadingUsesCargoHoldCapacity()
        {
            var ship = ShipState.CreateDefault();
            var premium = PersonalCargoRules.CreateCollectedCargo(PlanetTrait.WaterRich, 95);
            var rare = PersonalCargoRules.CreateCollectedCargo(PlanetTrait.WaterRich, 75);
            var common = PersonalCargoRules.CreateCollectedCargo(PlanetTrait.WaterRich, 0);

            var hold = PersonalCargoHoldState.Empty.WithCargoAdded(premium);

            Assert.That(PersonalCargoRules.CalculateCapacityUnits(ship), Is.EqualTo(300));
            Assert.That(PersonalCargoRules.CanAddCargo(ship, hold, rare), Is.True);

            hold = hold.WithCargoAdded(rare);
            Assert.That(hold.UsedSizeUnits, Is.EqualTo(300));
            Assert.That(PersonalCargoRules.CanAddCargo(ship, hold, common), Is.False);
        }

        [Test]
        public void PersonalCargo_DamagedCargoHoldReducesCapacity()
        {
            var damagedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(60, 100));
            var rare = PersonalCargoRules.CreateCollectedCargo(PlanetTrait.WaterRich, 75);
            var premium = PersonalCargoRules.CreateCollectedCargo(PlanetTrait.WaterRich, 95);

            Assert.That(PersonalCargoRules.CalculateCapacityUnits(damagedShip), Is.EqualTo(180));
            Assert.That(PersonalCargoRules.CanAddCargo(damagedShip, PersonalCargoHoldState.Empty, rare), Is.True);
            Assert.That(PersonalCargoRules.CanAddCargo(damagedShip, PersonalCargoHoldState.Empty, premium), Is.False);
        }

        [Test]
        public void PersonalCargo_CargoHoldAtFiftyPercentBlocksPersonalCargoTransport()
        {
            var blockedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100));
            var rare = PersonalCargoRules.CreateCollectedCargo(PlanetTrait.WaterRich, 75);

            Assert.That(PersonalCargoRules.CalculateCapacityUnits(blockedShip), Is.EqualTo(150));
            Assert.That(PersonalCargoRules.CanAddCargo(blockedShip, PersonalCargoHoldState.Empty, rare), Is.False);
            Assert.That(ShipStateRules.EvaluateStartReadiness(blockedShip).CanStartTransport, Is.True);
            Assert.That(ShipStateRules.EvaluateStartReadiness(blockedShip, true).IsPersonalCargoBlocked, Is.True);
        }

        [Test]
        public void PersonalCargo_SaleUsesTraitModifierAndDamage()
        {
            var cargo = new PersonalCargoItemState(
                "test-water",
                "Water Test Cargo",
                CargoGrade.Common,
                50,
                100,
                PlanetTrait.WaterRich,
                1f);

            var volcanicQuote = PersonalCargoRules.CalculateSaleQuote(cargo, PlanetTrait.VolcanicActive);
            var sameTraitDamagedQuote = PersonalCargoRules.CalculateSaleQuote(
                cargo.WithDurabilityPercent(0.5f),
                PlanetTrait.WaterRich);

            Assert.That(volcanicQuote.TraitModifierPercent, Is.EqualTo(100));
            Assert.That(volcanicQuote.SalePrice, Is.EqualTo(200));
            Assert.That(sameTraitDamagedQuote.TraitModifierPercent, Is.EqualTo(-50));
            Assert.That(sameTraitDamagedQuote.SalePrice, Is.EqualTo(25));
        }

        [Test]
        public void Session_CollectsAndSellsPersonalCargoAtCurrentPlanet()
        {
            var completed = CreateCompletedTutorialSession();

            var collection = completed.CollectPersonalCargo(0);
            var sale = collection.State.SellPersonalCargo(0);

            Assert.That(completed.CurrentPlanetTrait, Is.EqualTo(PlanetTrait.WaterRich));
            Assert.That(collection.Collected, Is.True);
            Assert.That(collection.State.PersonalCargoHold.Count, Is.EqualTo(1));
            Assert.That(sale.Sold, Is.True);
            Assert.That(sale.Quote.TraitModifierPercent, Is.EqualTo(-50));
            Assert.That(sale.Quote.SalePrice, Is.EqualTo(50));
            Assert.That(sale.State.PersonalCargoHold.Count, Is.Zero);
            Assert.That(sale.State.Wallet.Credits, Is.EqualTo(completed.Wallet.Credits + 50));
        }

        [Test]
        public void Session_TransportDamageReducesStoredPersonalCargoSalePrice()
        {
            var completed = CreateCompletedTutorialSession().CollectPersonalCargo(0).State;
            var followUp = TransportContractDefinition.CreateAssociationFollowUp();
            var started = completed.StartTransport(followUp);
            var damagedContractCargo = followUp.Cargo.WithDurabilityPercent(0.75f);

            var arrived = started.CompleteTransport(new SettlementInput(
                followUp.ContractType,
                followUp.Difficulty,
                damagedContractCargo,
                started.Ship,
                new CrewState(1, 0),
                started.Wallet,
                contractBasePay: followUp.RewardCredits));
            var storedCargo = arrived.PersonalCargoHold.GetCargo(0);
            var quote = PersonalCargoRules.CalculateSaleQuote(storedCargo, arrived.CurrentPlanetTrait);

            Assert.That(arrived.CurrentPlanetTrait, Is.EqualTo(PlanetTrait.CommonMineralRich));
            Assert.That(storedCargo.DurabilityPercent, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(quote.TraitModifierPercent, Is.EqualTo(70));
            Assert.That(quote.SalePrice, Is.EqualTo(128));
        }

        [Test]
        public void Session_PersonalCargoBlocksLaunchWhenCargoHoldCannotTransportIt()
        {
            var completed = CreateCompletedTutorialSession().CollectPersonalCargo(0).State
                .WithShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100)));
            var followUp = TransportContractDefinition.CreateAssociationFollowUp();

            Assert.That(
                () => completed.StartTransport(followUp),
                Throws.InvalidOperationException.With.Message.Contains("ship readiness"));
        }

        private static GameSessionState CreateCompletedTutorialSession()
        {
            var tutorial = TransportContractDefinition.CreateTutorial();
            var started = GameSessionState.StartAssociationSession().StartTransport(tutorial);
            return started.CompleteTransport(new SettlementInput(
                tutorial.ContractType,
                tutorial.Difficulty,
                tutorial.Cargo,
                started.Ship,
                new CrewState(1, 0),
                started.Wallet,
                contractBasePay: tutorial.RewardCredits,
                repairSupportAmount: 100));
        }
    }
}
