using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class PlanetStayRulesTests
    {
        [Test]
        public void HubState_ExposesPlanetMapFacilitiesContractsAndShopTabs()
        {
            var session = NewGameStartFlowState.CreateReturningPlayerNewGame()
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract()
                .SkipTutorialForReturningPlayer()
                .Session;

            var hub = PlanetStayRules.CreateHubState(session);

            Assert.That(hub.MapMarkers.Length, Is.EqualTo(4));
            Assert.That(hub.MapMarkers[0].Kind, Is.EqualTo(PlanetStayMapMarkerKind.Shop));
            Assert.That(hub.MapMarkers[1].Kind, Is.EqualTo(PlanetStayMapMarkerKind.RepairShop));
            Assert.That(hub.MapMarkers[2].Kind, Is.EqualTo(PlanetStayMapMarkerKind.Ship));
            Assert.That(hub.MapMarkers[3].Kind, Is.EqualTo(PlanetStayMapMarkerKind.CargoSupplyDepot));
            Assert.That(hub.CanOpenRepairShop, Is.True);
            Assert.That(hub.CanOpenContractOffice, Is.True);
            Assert.That(hub.CanOpenShop, Is.True);
            Assert.That(hub.CanOpenPersonalCargoDepot, Is.True);
            Assert.That(hub.CanOpenShip, Is.True);
            Assert.That(hub.ContractBoard.AssociationContractCount, Is.GreaterThan(0));
            Assert.That(hub.ContractBoard.PrivateContractCount, Is.GreaterThan(0));
            Assert.That(hub.ContractBoard.BuyTabAvailable, Is.True);
            Assert.That(hub.ContractBoard.SellTabAvailable, Is.True);
        }

        [Test]
        public void SpecialContractOffer_CanBeAcceptedIntoSessionFromPlanetStay()
        {
            var session = CreateOrganicPresenceOfferSession();

            var hub = PlanetStayRules.CreateHubState(session);
            var accepted = session.AcceptSpecialContract(SpecialContractKind.PresenceDetectorUnlock);

            Assert.That(hub.ContractBoard.SpecialContractCount, Is.EqualTo(1));
            Assert.That(FindOffer(hub, SpecialContractKind.PresenceDetectorUnlock).IsAvailable, Is.True);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.State.SpecialContracts.ActiveContractKind, Is.EqualTo(SpecialContractKind.PresenceDetectorUnlock));
        }

        private static SpecialContractOfferSummary FindOffer(
            PlanetStayHubState hub,
            SpecialContractKind kind)
        {
            for (var i = 0; i < hub.SpecialContractOffers.Length; i++)
            {
                if (hub.SpecialContractOffers[i].Kind == kind)
                {
                    return hub.SpecialContractOffers[i];
                }
            }

            Assert.Fail("Missing special contract offer: " + kind);
            return default;
        }

        private static GameSessionState CreateOrganicPresenceOfferSession()
        {
            var contract = new TransportContractDefinition(
                "planet-stay-organic-arrival",
                "Planet Stay Organic Arrival",
                "Organic Rich Planet",
                ContractType.Association,
                ContractDifficulty.VeryEasy,
                60,
                0,
                new CargoState(CargoGrade.Common, 1, 0, 1f, false),
                false,
                destinationTrait: PlanetTrait.OrganicRich);
            var started = GameSessionState.StartAssociationSession().StartTransport(contract);
            return started
                .CompleteTransport(new SettlementInput(
                    contract.ContractType,
                    contract.Difficulty,
                    contract.Cargo,
                    started.Ship,
                    new CrewState(1, 0),
                    started.Wallet,
                    contractBasePay: contract.RewardCredits))
                .WithReputation(new ReputationState(SpecialContractRules.PresenceDetectorRequiredFame, 0, false));
        }
    }
}
