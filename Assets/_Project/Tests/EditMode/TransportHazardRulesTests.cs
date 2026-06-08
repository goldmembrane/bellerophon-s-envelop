using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class TransportHazardRulesTests
    {
        [Test]
        public void Hazards_DoNotStartDuringTutorialTransport()
        {
            var session = GameSessionState.StartAssociationSession()
                .StartTransport(TransportContractDefinition.CreateTutorial());

            Assert.That(TransportHazardRules.CanCheckTransportHazard(session), Is.False);
            Assert.That(TransportHazardRules.ShouldStartAsteroidField(session), Is.False);
        }

        [Test]
        public void SourceHazardConstants_MatchUpdatedDesignSource()
        {
            Assert.That(TransportHazardRules.AsteroidFieldOccurrenceCheckIntervalSeconds, Is.EqualTo(5));
            Assert.That(TransportHazardRules.AsteroidFieldOccurrencePercent, Is.EqualTo(30));
            Assert.That(TransportHazardRules.MinimumAsteroidFieldDurationSeconds, Is.EqualTo(10));
            Assert.That(TransportHazardRules.MaximumAsteroidFieldDurationSeconds, Is.EqualTo(120));
            Assert.That(TransportHazardRules.AsteroidFieldSmallDamage, Is.EqualTo(10));
            Assert.That(TransportHazardRules.AsteroidFieldLargeDamage, Is.EqualTo(20));
            Assert.That(TransportHazardRules.AsteroidFieldSmallTargetHealth, Is.EqualTo(400));
            Assert.That(TransportHazardRules.AsteroidFieldLargeTargetHealth, Is.EqualTo(1000));

            Assert.That(TransportHazardRules.CargoFreedomLeagueFameThreshold, Is.EqualTo(1800));
            Assert.That(TransportHazardRules.CargoFreedomLeagueOccurrenceCheckIntervalSeconds, Is.EqualTo(10));
            Assert.That(TransportHazardRules.CargoFreedomLeagueOccurrencePercent, Is.EqualTo(15));
            Assert.That(TransportHazardRules.MinimumCargoFreedomLeagueDurationSeconds, Is.EqualTo(5));
            Assert.That(TransportHazardRules.MaximumCargoFreedomLeagueDurationSeconds, Is.EqualTo(35));

            Assert.That(TransportHazardRules.SpacePirateFameThreshold, Is.EqualTo(3000));
            Assert.That(TransportHazardRules.SpacePirateOccurrenceCheckIntervalSeconds, Is.EqualTo(10));
            Assert.That(TransportHazardRules.SpacePirateOccurrencePercent, Is.EqualTo(5));
            Assert.That(TransportHazardRules.MinimumSpacePirateDurationSeconds, Is.EqualTo(60));
            Assert.That(TransportHazardRules.MaximumSpacePirateDurationSeconds, Is.EqualTo(300));
            Assert.That(TransportHazardRules.SpacePirateBombardmentDamage, Is.EqualTo(20));

            Assert.That(TransportHazardRules.AlienLifeFameThreshold, Is.EqualTo(900));
            Assert.That(TransportHazardRules.AlienLifeOccurrenceCheckIntervalSeconds, Is.EqualTo(5));
            Assert.That(TransportHazardRules.AlienLifeOccurrencePercent, Is.EqualTo(10));
            Assert.That(TransportHazardRules.MinimumAlienLifeDurationSeconds, Is.EqualTo(30));
            Assert.That(TransportHazardRules.MaximumAlienLifeDurationSeconds, Is.EqualTo(300));
            Assert.That(TransportHazardRules.AlienLifeExternalTargetHealth, Is.EqualTo(350));

            Assert.That(TransportHazardRules.ConcealedBlackHoleFameThreshold, Is.EqualTo(4500));
            Assert.That(TransportHazardRules.GetManualFlightBoosterReductionSeconds(TransportHazardType.ConcealedBlackHole), Is.Zero);
        }

        [Test]
        public void AsteroidField_CreatesSmallOrLargeWithSourceDurationRange()
        {
            var followUp = CreatePostTutorialTransport();
            var checkIndex = FindStartingCheck(followUp, TransportHazardType.AsteroidFieldSmall);

            var hazard = TransportHazardRules.CreateAsteroidField(followUp, checkIndex);

            Assert.That(TransportHazardRules.ShouldStartAsteroidField(followUp, checkIndex), Is.True);
            Assert.That(
                hazard.HazardType == TransportHazardType.AsteroidFieldSmall ||
                hazard.HazardType == TransportHazardType.AsteroidFieldLarge,
                Is.True);
            Assert.That(hazard.Seed, Is.EqualTo(TransportHazardRules.CreateHazardSeed(
                followUp,
                TransportHazardType.AsteroidFieldSmall,
                checkIndex)));
            Assert.That(hazard.DurationSeconds, Is.InRange(
                TransportHazardRules.MinimumAsteroidFieldDurationSeconds,
                TransportHazardRules.MaximumAsteroidFieldDurationSeconds));
        }

        [Test]
        public void AsteroidField_SmallAndLargeApplySourceDamageAndTurretHealth()
        {
            var small = TransportHazardState.StartAsteroidFieldSmall(0, 10).Tick(10f, false);
            var large = TransportHazardState.StartAsteroidFieldLarge(0, 10).Tick(10f, false);

            var smallResult = TransportHazardRules.ResolveTransportHazard(small);
            var largeResult = TransportHazardRules.ResolveTransportHazard(large);
            var smallTarget = TransportHazardRules.CreateExternalTarget(small);
            var largeTarget = TransportHazardRules.CreateExternalTarget(large);

            Assert.That(smallResult.Resolution, Is.EqualTo(TransportHazardResolution.DirectHit));
            Assert.That(smallResult.RoomDamages[0].Damage, Is.EqualTo(TransportHazardRules.AsteroidFieldSmallDamage));
            Assert.That(largeResult.Resolution, Is.EqualTo(TransportHazardResolution.DirectHit));
            Assert.That(largeResult.RoomDamages[0].Damage, Is.EqualTo(TransportHazardRules.AsteroidFieldLargeDamage));
            Assert.That(smallTarget.MaxHealth, Is.EqualTo(TransportHazardRules.AsteroidFieldSmallTargetHealth));
            Assert.That(largeTarget.MaxHealth, Is.EqualTo(TransportHazardRules.AsteroidFieldLargeTargetHealth));
        }

        [Test]
        public void AsteroidField_ManualAvoidanceAndBoosterCanPreventDamage()
        {
            var hazard = TransportHazardState
                .StartAsteroidFieldSmall(8721, 12)
                .Tick(6f, true)
                .Tick(6f, false);
            var boosted = TransportHazardRules.ApplyManualFlightBooster(
                    TransportHazardState.StartAsteroidFieldSmall(8721, 12))
                .Tick(2f, false);

            var avoided = TransportHazardRules.ResolveTransportHazard(hazard);
            var boosterAvoided = TransportHazardRules.ResolveTransportHazard(boosted);

            Assert.That(TransportHazardRules.GetManualFlightBoosterReductionSeconds(hazard.HazardType), Is.EqualTo(10));
            Assert.That(avoided.Resolution, Is.EqualTo(TransportHazardResolution.Avoided));
            Assert.That(boosterAvoided.Resolution, Is.EqualTo(TransportHazardResolution.Avoided));
            Assert.That(avoided.RoomDamages, Is.Empty);
            Assert.That(boosterAvoided.RoomDamages, Is.Empty);
        }

        [Test]
        public void BoardingAndPirateHazards_ResolveToBoardingAndBombardmentEffects()
        {
            var cargoFreedom = TransportHazardState
                .Start(TransportHazardType.CargoFreedomLeagueRegion, 0, 6)
                .Tick(6f, false);
            var alienLife = TransportHazardState
                .Start(TransportHazardType.AlienLifeRegion, 0, 30)
                .Tick(30f, false);
            var pirate = TransportHazardState
                .Start(TransportHazardType.SpacePirateRegion, 0, 60)
                .Tick(60f, false);

            var cargoResult = TransportHazardRules.ResolveTransportHazard(cargoFreedom);
            var alienResult = TransportHazardRules.ResolveTransportHazard(alienLife);
            var pirateResult = TransportHazardRules.ResolveTransportHazard(pirate);
            var cargoTarget = TransportHazardRules.CreateExternalTarget(cargoFreedom);
            var alienTarget = TransportHazardRules.CreateExternalTarget(alienLife);
            var pirateTarget = TransportHazardRules.CreateExternalTarget(pirate);
            var pirateDamagedShip = TransportHazardRules.ApplyHazardResult(ShipState.CreateDefault(), pirateResult);

            Assert.That(cargoResult.BoardingEventCount, Is.GreaterThan(0));
            Assert.That(cargoResult.RoomDamages, Is.Empty);
            Assert.That(cargoTarget.TargetType, Is.EqualTo(ExternalTargetType.CargoFreedomLeagueBoardingCraft));
            Assert.That(cargoTarget.MaxHealth, Is.EqualTo(CargoFreedomLeagueRules.GetBoardingCraftProfile(
                CargoFreedomLeagueRules.SelectKindForSeed(cargoFreedom.Seed)).Health));
            Assert.That(alienResult.BoardingEventCount, Is.GreaterThan(0));
            Assert.That(alienResult.RoomDamages, Is.Empty);
            Assert.That(alienTarget.TargetType, Is.EqualTo(ExternalTargetType.AlienLifeform));
            Assert.That(alienTarget.MaxHealth, Is.EqualTo(350));
            Assert.That(pirateTarget.TargetType, Is.EqualTo(ExternalTargetType.SpacePirateBoardingCraft));
            Assert.That(pirateTarget.MaxHealth, Is.EqualTo(SpacePirateRules.GetBoardingCraftProfile(
                SpacePirateRules.SelectKindForSeed(pirate.Seed)).Health));
            Assert.That(pirateResult.BoardingEventCount, Is.GreaterThan(0));
            Assert.That(pirateResult.BombardmentHitCount, Is.EqualTo(pirateResult.RoomDamages.Length));
            Assert.That(pirateResult.RoomDamages[0].Damage, Is.EqualTo(TransportHazardRules.SpacePirateBombardmentDamage));
            Assert.That(ShipStateRules.CalculateRepairCost(pirateDamagedShip), Is.GreaterThan(0));
        }

        [Test]
        public void FameGates_AreSessionUnlocksAndDoNotRelockOnFameDrop()
        {
            var baseRun = CreatePostTutorialTransport();

            var underAlien = baseRun.WithReputation(new ReputationState(899, 0, false));
            var alienUnlocked = underAlien.WithReputation(new ReputationState(900, 0, false));
            var alienDropped = alienUnlocked.WithReputation(new ReputationState(100, 0, false));

            var cargoUnlocked = baseRun.WithReputation(new ReputationState(1800, 0, false))
                .WithReputation(new ReputationState(1500, 0, false));
            var pirateUnlocked = baseRun.WithReputation(new ReputationState(3000, 0, false))
                .WithReputation(new ReputationState(2000, 0, false));
            var blackHoleUnlocked = baseRun.WithReputation(new ReputationState(4500, 0, false))
                .WithReputation(new ReputationState(0, 0, false));

            Assert.That(underAlien.TransportHazardUnlocks.AlienLifeUnlocked, Is.False);
            Assert.That(alienUnlocked.TransportHazardUnlocks.AlienLifeUnlocked, Is.True);
            Assert.That(alienDropped.TransportHazardUnlocks.AlienLifeUnlocked, Is.True);
            Assert.That(cargoUnlocked.TransportHazardUnlocks.CargoFreedomLeagueUnlocked, Is.True);
            Assert.That(pirateUnlocked.TransportHazardUnlocks.SpacePirateUnlocked, Is.True);
            Assert.That(blackHoleUnlocked.TransportHazardUnlocks.ConcealedBlackHoleUnlocked, Is.True);
            Assert.That(GameSessionState.StartAssociationSession().TransportHazardUnlocks.CargoFreedomLeagueUnlocked, Is.False);
        }

        [Test]
        public void LockedFameGatedHazards_DoNotStartUntilUnlocked()
        {
            var baseRun = CreatePostTutorialTransport();
            var alienCheck = FindRawStartingCheck(baseRun, TransportHazardType.AlienLifeRegion);
            var unlocked = baseRun.WithReputation(new ReputationState(900, 0, false));

            Assert.That(TransportHazardRules.ShouldStartHazard(
                baseRun,
                TransportHazardType.AlienLifeRegion,
                alienCheck), Is.False);
            Assert.That(TransportHazardRules.ShouldStartHazard(
                unlocked,
                TransportHazardType.AlienLifeRegion,
                alienCheck), Is.True);
            Assert.Throws<System.InvalidOperationException>(() =>
                TransportHazardRules.CreateHazard(
                    baseRun,
                    TransportHazardType.ConcealedBlackHole,
                    1));
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

        private static int FindRawStartingCheck(GameSessionState session, TransportHazardType hazardType)
        {
            var unlocked = session.WithReputation(new ReputationState(
                TransportHazardRules.GetFameThreshold(hazardType),
                0,
                false));
            return FindStartingCheck(unlocked, hazardType);
        }

        private static GameSessionState CreatePostTutorialTransport()
        {
            var tutorialRun = GameSessionState.StartAssociationSession()
                .StartTransport(TransportContractDefinition.CreateTutorial());
            var completed = tutorialRun.CompleteTransport(new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Intro,
                TransportContractDefinition.CreateTutorial().Cargo,
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                tutorialRun.Wallet,
                contractBasePay: 1000,
                repairSupportAmount: 100));
            return completed.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }
    }
}
