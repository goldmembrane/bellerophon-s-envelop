using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class NewGameStartFlowStateTests
    {
        [Test]
        public void AssociationContract_StartsAssociationPlanetWithDefaultIssue()
        {
            var flow = NewGameStartFlowState.CreateNewGame();

            var accepted = flow.AcceptAssociationContract();

            Assert.That(accepted.Phase, Is.EqualTo(NewGameStartFlowPhase.AssociationPlanet));
            Assert.That(accepted.Session.IsAssociationMember, Is.True);
            Assert.That(accepted.Session.CurrentPlanet.HasAssociationLogoSign, Is.True);
            Assert.That(accepted.Session.Wallet.Credits, Is.EqualTo(0));
            Assert.That(accepted.Session.StartingLoadout.HasDefaultCargoShip, Is.True);
            Assert.That(accepted.Session.StartingLoadout.HasBasicProtectiveSuit, Is.True);
            Assert.That(accepted.Session.StartingLoadout.StickCount, Is.EqualTo(1));
            Assert.That(accepted.AvailableContractCount, Is.EqualTo(1));
        }

        [Test]
        public void TutorialContract_IsOnlyIntroContractAndRegistersCargoForTransport()
        {
            var accepted = NewGameStartFlowState.CreateNewGame()
                .AcceptAssociationContract()
                .AcceptTutorialContract();

            Assert.That(accepted.Phase, Is.EqualTo(NewGameStartFlowPhase.TutorialContractAccepted));
            Assert.That(accepted.Session.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(accepted.Session.Ship.RunState, Is.EqualTo(ShipRunState.InTransit));
            Assert.That(accepted.Session.ActiveTransportContract.HasValue, Is.True);
            Assert.That(accepted.Session.ActiveTransportContract.Value.ContractType, Is.EqualTo(ContractType.Association));
            Assert.That(accepted.Session.ActiveTransportContract.Value.Difficulty, Is.EqualTo(ContractDifficulty.Intro));
            Assert.That(accepted.Session.ActiveTransportContract.Value.DurationSeconds, Is.EqualTo(60));
            Assert.That(accepted.Session.ActiveTransportContract.Value.IsTutorial, Is.True);
            Assert.That(accepted.Session.ActiveTransportContract.Value.TransportTargetName, Is.EqualTo("Cargo Hold Center Cargo"));
            Assert.That(accepted.Session.HasActiveCargo, Is.True);
            Assert.That(accepted.Session.ActiveCargo.Value.DurabilityPercent, Is.EqualTo(1f));
        }
    }
}
