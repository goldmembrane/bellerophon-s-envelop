using System;
using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class DetailedContractCatalogRulesTests
    {
        [Test]
        public void DefaultStepTwoCatalog_ContainsTemporaryPlanetsRoutesCargoAndContracts()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();

            Assert.That(catalog.CoversPhaseOneDomains, Is.True);
            Assert.That(catalog.Planets.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(catalog.Routes.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(catalog.Contracts.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(catalog.Cargo.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(catalog.Planets, Has.Some.Matches<PlanetContentDefinition>(planet =>
                planet.HasTrait(PlanetTrait.WaterRich) && planet.HasTrait(PlanetTrait.OrganicRich)));
        }

        [Test]
        public void ContractVisibility_AssociationMemberGetsAtLeastFiveAssociationContracts()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: true,
                cargoHoldScore: 100,
                fameScore: 0,
                associationFameScore: 0,
                completedAssociationTransportCount: 1,
                repairCostEstimate: 0);

            var contracts = DetailedContractCatalogRules.GetPostTutorialContractContents(catalog, context);

            Assert.That(CountByType(contracts, ContractType.Association), Is.GreaterThanOrEqualTo(5));
            Assert.That(contracts, Has.None.Matches<ContractContentDefinition>(contract => contract.IsTutorial));
        }

        [Test]
        public void ContractVisibility_NonMemberGetsTwoAssociationContracts()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: false,
                cargoHoldScore: 100,
                fameScore: 0,
                associationFameScore: 0,
                completedAssociationTransportCount: 0,
                repairCostEstimate: 0);

            var contracts = DetailedContractCatalogRules.GetPostTutorialContractContents(catalog, context);

            Assert.That(CountByType(contracts, ContractType.Association), Is.EqualTo(2));
        }

        [Test]
        public void ContractVisibility_LowFameHidesPrivateAndShowsOneTimeRevivalContract()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: false,
                cargoHoldScore: 100,
                fameScore: DetailedContractCatalogRules.LowFamePrivateContractThreshold - 1,
                associationFameScore: 0,
                completedAssociationTransportCount: 0,
                repairCostEstimate: 0);

            var contracts = DetailedContractCatalogRules.GetPostTutorialContractContents(catalog, context);
            var afterUsed = DetailedContractCatalogRules.GetPostTutorialContractContents(
                catalog,
                new DetailedContractOfferContext(
                    isAssociationMember: false,
                    cargoHoldScore: 100,
                    fameScore: DetailedContractCatalogRules.LowFamePrivateContractThreshold - 1,
                    associationFameScore: 0,
                    completedAssociationTransportCount: 0,
                    repairCostEstimate: 0,
                    hasUsedRevivalContract: true));

            Assert.That(contracts, Has.Some.Matches<ContractContentDefinition>(contract =>
                contract.ContractId == "association-revival-001" && contract.IsRecoveryContract));
            Assert.That(contracts, Has.None.Matches<ContractContentDefinition>(contract =>
                contract.ContractType == ContractType.Private));
            Assert.That(afterUsed, Has.None.Matches<ContractContentDefinition>(contract =>
                contract.ContractId == "association-revival-001"));
        }

        [Test]
        public void ContractVisibility_LowFameNonMemberAssociationContractRequiresForcedMembership()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: false,
                cargoHoldScore: 100,
                fameScore: DetailedContractCatalogRules.LowFamePrivateContractThreshold - 1,
                associationFameScore: 0,
                completedAssociationTransportCount: 0,
                repairCostEstimate: 0);
            var contract = DetailedContractCatalogRules.FindContract(catalog, "association-local-001");

            Assert.That(DetailedContractCatalogRules.RequiresForcedAssociationMembership(context, contract), Is.True);
        }

        [Test]
        public void RewardFormula_TracksCargoDistanceAndReputationWeights()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: true,
                cargoHoldScore: 100,
                fameScore: 0,
                associationFameScore: 200,
                completedAssociationTransportCount: 2,
                repairCostEstimate: 1000);
            var contract = DetailedContractCatalogRules.FindContract(catalog, "association-local-001");
            var route = DetailedContractCatalogRules.FindRoute(catalog, contract.RouteId);
            var cargo = DetailedContractCatalogRules.FindCargo(catalog, contract.CargoId);

            var reward = DetailedContractCatalogRules.CalculateReward(contract, route, cargo, context);

            Assert.That(reward.CargoValuePay, Is.EqualTo(600));
            Assert.That(reward.DistancePay, Is.EqualTo(300));
            Assert.That(reward.ReputationPay, Is.EqualTo(20));
            Assert.That(reward.RepairSupportAmount, Is.EqualTo(100));
            Assert.That(reward.SafeStreakBonus, Is.EqualTo(50));
            Assert.That(reward.ContractPayCredits, Is.EqualTo(920));
            Assert.That(reward.TotalPositiveCredits, Is.EqualTo(1070));
        }

        [Test]
        public void RewardFormula_AssociationMaintenanceFeeStartsAtFourthTransport()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: true,
                cargoHoldScore: 100,
                fameScore: 0,
                associationFameScore: 0,
                completedAssociationTransportCount: 3,
                repairCostEstimate: 0);
            var contract = DetailedContractCatalogRules.FindContract(catalog, "association-local-001");
            var route = DetailedContractCatalogRules.FindRoute(catalog, contract.RouteId);
            var cargo = DetailedContractCatalogRules.FindCargo(catalog, contract.CargoId);

            var reward = DetailedContractCatalogRules.CalculateReward(contract, route, cargo, context);

            Assert.That(reward.AssociationMaintenanceFee, Is.EqualTo(100));
        }

        [Test]
        public void RewardFormula_NegativeReputationDoesNotCreateNegativePay()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: true,
                cargoHoldScore: 100,
                fameScore: -1200,
                associationFameScore: -1200,
                completedAssociationTransportCount: 1,
                repairCostEstimate: 0);
            var contract = DetailedContractCatalogRules.FindContract(catalog, "association-local-001");
            var route = DetailedContractCatalogRules.FindRoute(catalog, contract.RouteId);
            var cargo = DetailedContractCatalogRules.FindCargo(catalog, contract.CargoId);

            var reward = DetailedContractCatalogRules.CalculateReward(contract, route, cargo, context);

            Assert.That(reward.ReputationPay, Is.EqualTo(0));
            Assert.That(reward.ContractPayCredits, Is.EqualTo(900));
        }

        [Test]
        public void RevivalContract_ConvertsToFixedRewardTransportContract()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var context = new DetailedContractOfferContext(
                isAssociationMember: false,
                cargoHoldScore: 100,
                fameScore: DetailedContractCatalogRules.LowFamePrivateContractThreshold - 1,
                associationFameScore: 0,
                completedAssociationTransportCount: 0,
                repairCostEstimate: 0);
            var contract = DetailedContractCatalogRules.FindContract(catalog, "association-revival-001");

            var transportContract = DetailedContractCatalogRules.CreateTransportContract(contract, catalog, context);

            Assert.That(transportContract.RewardCredits, Is.EqualTo(500));
            Assert.That(transportContract.DurationSeconds, Is.EqualTo(45));
            Assert.That(transportContract.IsRevivalContract, Is.True);
            Assert.That(transportContract.RequiredCargoHoldScore, Is.EqualTo(0));
        }

        [Test]
        public void CanAcceptContract_RequiresCargoHoldScore()
        {
            var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
            var contract = DetailedContractCatalogRules.FindContract(catalog, "private-sample-001");
            var lowScore = new DetailedContractOfferContext(
                isAssociationMember: true,
                cargoHoldScore: 64,
                fameScore: 0,
                associationFameScore: 0,
                completedAssociationTransportCount: 1,
                repairCostEstimate: 0);
            var enoughScore = new DetailedContractOfferContext(
                isAssociationMember: true,
                cargoHoldScore: 65,
                fameScore: 0,
                associationFameScore: 0,
                completedAssociationTransportCount: 1,
                repairCostEstimate: 0);

            Assert.That(DetailedContractCatalogRules.CanAcceptContract(lowScore, contract), Is.False);
            Assert.That(DetailedContractCatalogRules.CanAcceptContract(enoughScore, contract), Is.True);
        }

        [Test]
        public void TutorialContract_RemainsIntroFixedRewardAndSixtySeconds()
        {
            var tutorial = TransportContractDefinition.CreateTutorial();

            Assert.That(tutorial.Id, Is.EqualTo("association-tutorial-001"));
            Assert.That(tutorial.ContractType, Is.EqualTo(ContractType.Association));
            Assert.That(tutorial.Difficulty, Is.EqualTo(ContractDifficulty.Intro));
            Assert.That(tutorial.DurationSeconds, Is.EqualTo(60));
            Assert.That(tutorial.RewardCredits, Is.EqualTo(1000));
            Assert.That(tutorial.IsTutorial, Is.True);
            Assert.That(tutorial.TransportTargetName, Is.EqualTo(DetailedContractCatalogRules.TransportTargetName));
        }

        [Test]
        public void PostTutorialContracts_KeepCurrentMaintenanceUiCompatiblePair()
        {
            var contracts = TransportContractDefinition.CreatePostTutorialContracts();

            Assert.That(contracts.Length, Is.EqualTo(2));
            Assert.That(contracts[0].Id, Is.EqualTo("association-local-001"));
            Assert.That(contracts[0].RewardCredits, Is.EqualTo(900));
            Assert.That(contracts[0].RequiredCargoHoldScore, Is.EqualTo(40));
            Assert.That(contracts[1].Id, Is.EqualTo("private-sample-001"));
            Assert.That(contracts[1].RewardCredits, Is.EqualTo(1800));
            Assert.That(contracts[1].RequiredCargoHoldScore, Is.EqualTo(65));
        }

        [Test]
        public void PlanetContentDefinition_RequiresAtLeastOneTrait()
        {
            Assert.Throws<ArgumentException>(() => new PlanetContentDefinition(
                "planet-empty",
                "Empty Planet",
                new PlanetTrait[0]));
        }

        private static int CountByType(ContractContentDefinition[] contracts, ContractType type)
        {
            var count = 0;
            for (var i = 0; i < contracts.Length; i++)
            {
                if (contracts[i].ContractType == type)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
