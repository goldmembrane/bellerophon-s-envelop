using System;
using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class DetailedStep21BalancePlaytestHardeningTests
    {
        [Test]
        public void SourceValuedEconomyPins_RemainUnchangedForPlaytestTuning()
        {
            var tutorial = TransportContractDefinition.CreateTutorial();
            var postTutorial = TransportContractDefinition.CreatePostTutorialContracts();
            var stick = EquipmentRules.GetDefinition(EquipmentItemKind.Stick);
            var musket = EquipmentRules.GetDefinition(EquipmentItemKind.Musket);
            var shotgun = EquipmentRules.GetDefinition(EquipmentItemKind.Shotgun);

            Assert.That(ShipStateRules.SettlementSummaryRepairRatePerPercent, Is.EqualTo(5));
            Assert.That(ShipStateRules.MaxNormalRepairMissingPercent, Is.EqualTo(599));
            Assert.That(ShipStateRules.TotalLossClaimCost, Is.EqualTo(5000));
            Assert.That(ShipStateRules.CalculateTowingCost(1), Is.EqualTo(2000));
            Assert.That(ShipStateRules.CalculateTowingCost(2), Is.EqualTo(3000));
            Assert.That(ShipStateRules.CalculateTowingCost(3), Is.EqualTo(5000));
            Assert.That(ShipStateRules.CalculateTowingCost(4), Is.EqualTo(7500));

            Assert.That(tutorial.RewardCredits, Is.EqualTo(1000));
            Assert.That(tutorial.DurationSeconds, Is.EqualTo(60));
            Assert.That(postTutorial[0].Id, Is.EqualTo("association-local-001"));
            Assert.That(postTutorial[0].RewardCredits, Is.EqualTo(900));
            Assert.That(postTutorial[1].Id, Is.EqualTo("private-sample-001"));
            Assert.That(postTutorial[1].RewardCredits, Is.EqualTo(1800));

            Assert.That(stick.PriceCredits, Is.EqualTo(200));
            Assert.That(stick.Damage, Is.EqualTo(30));
            Assert.That(musket.PriceCredits, Is.EqualTo(450));
            Assert.That(musket.Damage, Is.EqualTo(50));
            Assert.That(shotgun.PriceCredits, Is.EqualTo(600));
            Assert.That(shotgun.Damage, Is.EqualTo(70));
            Assert.That(EquipmentRules.GetDefinition(EquipmentItemKind.Flashlight).PriceCredits, Is.EqualTo(25));
            Assert.That(EquipmentRules.GetDefinition(EquipmentItemKind.InjuryReliever).PriceCredits, Is.EqualTo(125));
            Assert.That(EquipmentRules.GetDefinition(EquipmentItemKind.LightBlade).PriceCredits, Is.EqualTo(1000));
            Assert.That(EquipmentRules.GetDefinition(EquipmentItemKind.ElectricMine).PriceCredits, Is.EqualTo(1000));
            Assert.That(EquipmentRules.GetDefinition(EquipmentItemKind.CorridorPurifier).PriceCredits, Is.EqualTo(600));
        }

        [Test]
        public void HazardRiskCadenceAndHighRiskRoutes_RemainSourcePinned()
        {
            var corridorState = SpecialContractProgressState.Empty
                .WithActiveContract(SpecialContractKind.CorridorPurifierUnlock);
            var routeModifier = SpecialContractRules.CreateRouteModifier(corridorState);
            var corridorContract = SpecialContractRules.CreateCorridorPurifierTransportContract();

            Assert.That(TransportHazardRules.AsteroidFieldOccurrencePercent, Is.EqualTo(30));
            Assert.That(TransportHazardRules.AsteroidFieldOccurrenceCheckIntervalSeconds, Is.EqualTo(5));
            Assert.That(TransportHazardRules.CargoFreedomLeagueFameThreshold, Is.EqualTo(1800));
            Assert.That(TransportHazardRules.CargoFreedomLeagueOccurrencePercent, Is.EqualTo(15));
            Assert.That(TransportHazardRules.SpacePirateFameThreshold, Is.EqualTo(3000));
            Assert.That(TransportHazardRules.SpacePirateOccurrencePercent, Is.EqualTo(5));
            Assert.That(TransportHazardRules.AlienLifeFameThreshold, Is.EqualTo(900));
            Assert.That(TransportHazardRules.AlienLifeOccurrencePercent, Is.EqualTo(10));
            Assert.That(TransportHazardRules.GetManualFlightBoosterReductionSeconds(TransportHazardType.SpacePirateRegion), Is.EqualTo(45));
            Assert.That(TransportHazardRules.GetManualFlightBoosterReductionSeconds(TransportHazardType.ConcealedBlackHole), Is.Zero);

            Assert.That(routeModifier.ForcesAllIntrusionHazards, Is.True);
            Assert.That(routeModifier.IntrusionOccurrenceMultiplier, Is.EqualTo(3));
            Assert.That(routeModifier.FixedDurationSeconds, Is.EqualTo(284));
            Assert.That(corridorContract.Difficulty, Is.EqualTo(ContractDifficulty.Master));
            Assert.That(corridorContract.DurationSeconds, Is.EqualTo(284));
            Assert.Throws<InvalidOperationException>(() =>
                TransportHazardState.Start(TransportHazardType.ConcealedBlackHole, 1, 10));
        }

        [Test]
        public void FailureRecoveryScenarios_CoverDebtGraceFinalGameOverAndTotalLossClaim()
        {
            var firstNegative = SettlementCalculator.Calculate(CreateInput(
                ShipState.CreateDefault(),
                wallet: new WalletState(10, false),
                cargoLossPenalty: 150));
            var secondNegative = SettlementCalculator.Calculate(CreateInput(
                ShipState.CreateDefault(),
                wallet: new WalletState(-40, false, true),
                cargoLossPenalty: 150));
            var totalLossShip = CreateTotalLossShip();
            var totalLoss = SettlementCalculator.Calculate(CreateInput(
                totalLossShip,
                wallet: new WalletState(1000, false),
                repairCost: ShipStateRules.CalculateTotalLossClaimCost(totalLossShip)));

            Assert.That(firstNegative.FinalBalance, Is.EqualTo(-40));
            Assert.That(firstNegative.DebtStatus, Is.EqualTo(SettlementDebtStatus.GraceActive));
            Assert.That(firstNegative.IsGameOver, Is.False);

            Assert.That(secondNegative.FinalBalance, Is.EqualTo(-90));
            Assert.That(secondNegative.DebtStatus, Is.EqualTo(SettlementDebtStatus.FinalGameOver));
            Assert.That(secondNegative.IsGameOver, Is.True);

            Assert.That(ShipStateRules.CalculateTotalLossClaimCost(totalLossShip), Is.EqualTo(5000));
            Assert.That(totalLoss.PendingRepairCost, Is.EqualTo(5000));
            Assert.That(totalLoss.FinalBalance, Is.EqualTo(1000));
            Assert.That(totalLoss.IsGameOver, Is.False);
        }

        [Test]
        public void RepeatablePlaytestScenarioDefinitions_CoverEarlyMidAndHighRiskLoops()
        {
            var early = TransportContractDefinition.CreateTutorial();
            var mid = CreatePostTutorialTransport()
                .WithReputation(new ReputationState(TransportHazardRules.AlienLifeFameThreshold, 0, false));
            var high = SpecialContractRules.CreateCorridorPurifierTransportContract();
            var alienCheck = FindStartingCheck(mid, TransportHazardType.AlienLifeRegion);

            Assert.That(early.IsTutorial, Is.True);
            Assert.That(early.Difficulty, Is.EqualTo(ContractDifficulty.Intro));
            Assert.That(TransportHazardRules.ShouldStartHazard(
                mid,
                TransportHazardType.AlienLifeRegion,
                alienCheck), Is.True);
            Assert.That(high.ContractType, Is.EqualTo(ContractType.Special));
            Assert.That(high.RequiredCargoHoldScore, Is.EqualTo(80));
            Assert.That(high.Difficulty, Is.EqualTo(ContractDifficulty.Master));
        }

        private static SettlementInput CreateInput(
            ShipState ship,
            WalletState wallet,
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
                contractBasePay: 100,
                cargoLossPenalty: cargoLossPenalty);
        }

        private static ShipState CreateTotalLossShip()
        {
            return ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100))
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(0, 100));
        }

        private static GameSessionState CreatePostTutorialTransport()
        {
            var tutorial = TransportContractDefinition.CreateTutorial();
            var tutorialRun = GameSessionState.StartAssociationSession()
                .StartTransport(tutorial);
            var completed = tutorialRun.CompleteTransport(new SettlementInput(
                tutorial.ContractType,
                tutorial.Difficulty,
                tutorial.Cargo,
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                tutorialRun.Wallet,
                contractBasePay: tutorial.RewardCredits,
                repairSupportAmount: 100));

            return completed.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }

        private static int FindStartingCheck(GameSessionState session, TransportHazardType hazardType)
        {
            for (var i = 1; i <= 1000; i++)
            {
                if (TransportHazardRules.ShouldStartHazard(session, hazardType, i))
                {
                    return i;
                }
            }

            throw new AssertionException("No deterministic hazard check found for " + hazardType + ".");
        }
    }
}
