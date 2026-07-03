using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ReputationRulesTests
    {
        [Test]
        public void DifficultyDeltas_MatchDetailedFameTable()
        {
            Assert.That(ReputationRules.GetDifficultyDelta(ContractDifficulty.Intro, true), Is.EqualTo(10));
            Assert.That(ReputationRules.GetDifficultyDelta(ContractDifficulty.VeryEasy, false), Is.EqualTo(-30));
            Assert.That(ReputationRules.GetDifficultyDelta(ContractDifficulty.Normal, true), Is.EqualTo(120));
            Assert.That(ReputationRules.GetDifficultyDelta(ContractDifficulty.VeryHard, false), Is.EqualTo(-1500));
            Assert.That(ReputationRules.GetDifficultyDelta(ContractDifficulty.Master, true), Is.EqualTo(3000));
        }

        [Test]
        public void CompletingTutorialCleanly_AddsFameAndAssociationFame()
        {
            var tutorial = TransportContractDefinition.CreateTutorial();
            var session = GameSessionState.StartAssociationSession().StartTransport(tutorial);

            var completed = session.CompleteTransport(CreateSettlementInput(session.Wallet, tutorial.Cargo));

            Assert.That(completed.Reputation.FameScore, Is.EqualTo(10));
            Assert.That(completed.Reputation.AssociationFameScore, Is.EqualTo(10));
            Assert.That(completed.Reputation.HasUsedRevivalContract, Is.False);
        }

        [Test]
        public void FailingPrivateNormalContract_AppliesFailureFameOnly()
        {
            var privateContract = TransportContractDefinition.CreatePrivateFollowUp();
            var session = GameSessionState.StartAssociationSession().StartTransport(privateContract);

            var failed = session.FailTransport(CreateSettlementInput(
                session.Wallet,
                privateContract.Cargo.WithDurabilityPercent(0.75f),
                new CrewState(0, 1)));

            Assert.That(failed.Reputation.FameScore, Is.EqualTo(-220));
            Assert.That(failed.Reputation.AssociationFameScore, Is.EqualTo(0));
            Assert.That(failed.Reputation.HasUsedRevivalContract, Is.False);
        }

        [Test]
        public void CompletingRevivalContractCleanly_ResetsFameAndMarksContractUsed()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: false,
                cargoHoldScore: 100,
                fameScore: -650,
                associationFameScore: 0,
                completedAssociationTransportCount: 0,
                repairCostEstimate: 0);
            var revival = DetailedContractCatalogRules.CreateTransportContract(
                DetailedContractCatalogRules.FindContract(catalog, "association-revival-001"),
                catalog,
                context);
            var session = GameSessionState.StartSession(new WalletState(0, false))
                .WithReputation(new ReputationState(-650, 0, false))
                .StartTransport(revival);

            var completed = session.CompleteTransport(CreateSettlementInput(session.Wallet, revival.Cargo));

            Assert.That(completed.Reputation.FameScore, Is.EqualTo(0));
            Assert.That(completed.Reputation.AssociationFameScore, Is.EqualTo(5));
            Assert.That(completed.Reputation.HasUsedRevivalContract, Is.True);
        }

        private static SettlementInput CreateSettlementInput(
            WalletState wallet,
            CargoState cargo,
            CrewState? crew = null)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Intro,
                cargo,
                ShipState.CreateDefault(),
                crew ?? new CrewState(1, 0),
                wallet,
                contractBasePay: 100);
        }
    }
}
