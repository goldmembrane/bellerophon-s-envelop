using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class NewGameStartFlowStateTests
    {
        [Test]
        public void AssociationContract_StartsAssociationPlanetWithDefaultIssue()
        {
            var flow = CreateScrolledContractPrompt();

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
            var accepted = CreateScrolledContractPrompt()
                .AcceptAssociationContract()
                .AcceptTutorialContract();

            Assert.That(accepted.Phase, Is.EqualTo(NewGameStartFlowPhase.TutorialContractAccepted));
            Assert.That(accepted.Session.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(accepted.Session.Ship.RunState, Is.EqualTo(ShipRunState.InTransit));
            Assert.That(accepted.Session.ActiveTransportContract.HasValue, Is.True);
            Assert.That(accepted.Session.ActiveTransportContract.Value.ContractType, Is.EqualTo(ContractType.Association));
            Assert.That(accepted.Session.ActiveTransportContract.Value.Difficulty, Is.EqualTo(ContractDifficulty.Intro));
            Assert.That(accepted.Session.ActiveTransportContract.Value.DurationSeconds, Is.EqualTo(60));
            Assert.That(accepted.Session.ActiveTransportContract.Value.RewardCredits, Is.EqualTo(1000));
            Assert.That(accepted.Session.ActiveTransportContract.Value.IsTutorial, Is.True);
            Assert.That(accepted.Session.ActiveTransportContract.Value.TransportTargetName, Is.EqualTo("Cargo Hold Center Cargo"));
            Assert.That(accepted.Session.HasActiveCargo, Is.True);
            Assert.That(accepted.Session.ActiveCargo.Value.DurabilityPercent, Is.EqualTo(1f));
        }

        [Test]
        public void PostTransportContracts_ExposeAssociationAndPrivateOptions()
        {
            var accepted = CreateScrolledContractPrompt()
                .AcceptAssociationContract()
                .AcceptTutorialContract();
            var contract = accepted.Session.ActiveTransportContract.Value;
            var completed = accepted.Session.CompleteTransport(new SettlementInput(
                contract.ContractType,
                contract.Difficulty,
                contract.Cargo,
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                accepted.Session.Wallet,
                contractBasePay: contract.RewardCredits,
                repairSupportAmount: 100));

            var postTransport = accepted
                .WithSession(completed)
                .PreparePostTransportContracts();

            Assert.That(postTransport.AvailableContractCount, Is.EqualTo(2));
            Assert.That(postTransport.GetAvailableContract(0).ContractType, Is.EqualTo(ContractType.Association));
            Assert.That(postTransport.GetAvailableContract(0).RequiredCargoHoldScore, Is.EqualTo(40));
            Assert.That(postTransport.GetAvailableContract(1).ContractType, Is.EqualTo(ContractType.Private));
            Assert.That(postTransport.GetAvailableContract(1).RewardCredits, Is.EqualTo(1800));
            Assert.That(postTransport.GetAvailableContract(1).RequiredCargoHoldScore, Is.EqualTo(65));
        }

        [Test]
        public void AssociationContractScroll_UsesSixtySecondAutoAndThreeSecondDownArrowRules()
        {
            var flow = NewGameStartFlowState.CreateNewGame();

            var half = flow.TickAssociationContractScroll(30f);
            var fast = flow.TickAssociationContractDownArrowFastMove(3f);

            Assert.That(flow.CanAcceptAssociationContract, Is.False);
            Assert.That(half.AssociationContractScroll.ProgressPercent, Is.EqualTo(50));
            Assert.That(half.CanAcceptAssociationContract, Is.False);
            Assert.That(fast.AssociationContractScroll.HasReachedBottom, Is.True);
            Assert.That(fast.CanAcceptAssociationContract, Is.True);
        }

        [Test]
        public void AssociationContractNo_IsBlockedAfterTentativeConsent()
        {
            var flow = CreateScrolledContractPrompt();

            var rejected = flow.RejectAssociationContract();

            Assert.That(flow.IsAssociationNoBlocked, Is.True);
            Assert.That(rejected.Blocked, Is.True);
            Assert.That(rejected.State.Phase, Is.EqualTo(NewGameStartFlowPhase.ContractPrompt));
            Assert.That(rejected.Summary, Is.EqualTo("이미 잠정적으로 동의한 상태입니다"));
        }

        [Test]
        public void HiddenPrivateBusinessRoute_RequiresStopThenCancelBeforeBottom()
        {
            var flow = NewGameStartFlowState.CreateNewGame()
                .TickAssociationContractScroll(10f);

            var blocked = flow.StartPrivateBusinessRouteFromStoppedContract();
            var started = flow
                .StopAssociationContractScroll()
                .StartPrivateBusinessRouteFromStoppedContract();

            Assert.That(blocked.Blocked, Is.True);
            Assert.That(started.Succeeded, Is.True);
            Assert.That(started.State.Phase, Is.EqualTo(NewGameStartFlowPhase.PrivateBusinessPlanet));
            Assert.That(started.State.Session.IsAssociationMember, Is.False);
            Assert.That(started.State.Session.CurrentPlanet.HasAssociationLogoSign, Is.False);
        }

        [Test]
        public void ReturningPlayerTutorialSkip_GrantsCreditsAndShowsPostTutorialContracts()
        {
            var association = NewGameStartFlowState.CreateReturningPlayerNewGame()
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract();

            var skipped = association.SkipTutorialForReturningPlayer();

            Assert.That(association.CanSkipTutorial, Is.True);
            Assert.That(skipped.TutorialSkipped, Is.True);
            Assert.That(skipped.Session.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(NewGameStartFlowState.TutorialSkipRepairSupportCredits, Is.EqualTo(100));
            Assert.That(skipped.Session.Wallet.Credits, Is.EqualTo(NewGameStartFlowState.TutorialSkipRewardCredits));
            Assert.That(skipped.Session.CompletedTransportCount, Is.EqualTo(1));
            Assert.That(skipped.AvailableContractCount, Is.EqualTo(2));
            Assert.That(skipped.GetAvailableContract(0).IsTutorial, Is.False);
        }

        private static NewGameStartFlowState CreateScrolledContractPrompt()
        {
            return NewGameStartFlowState.CreateNewGame()
                .MoveAssociationContractToBottom();
        }
    }
}
