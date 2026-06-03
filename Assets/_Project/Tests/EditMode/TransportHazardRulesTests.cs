using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class TransportHazardRulesTests
    {
        [Test]
        public void AsteroidField_DoesNotStartDuringTutorialTransport()
        {
            var session = GameSessionState.StartAssociationSession()
                .StartTransport(TransportContractDefinition.CreateTutorial());

            Assert.That(TransportHazardRules.ShouldStartAsteroidField(session), Is.False);
        }

        [Test]
        public void AsteroidField_StartsForPostTutorialTransportWithSeededDuration()
        {
            var followUp = CreatePostTutorialTransport();

            var hazard = TransportHazardRules.CreateAsteroidField(followUp);

            Assert.That(TransportHazardRules.ShouldStartAsteroidField(followUp), Is.True);
            Assert.That(hazard.HazardType, Is.EqualTo(TransportHazardType.AsteroidField));
            Assert.That(hazard.Seed, Is.EqualTo(TransportHazardRules.CreateAsteroidSeed(followUp)));
            Assert.That(hazard.DurationSeconds, Is.InRange(
                TransportHazardRules.MinimumAsteroidFieldDurationSeconds,
                TransportHazardRules.MinimumAsteroidFieldDurationSeconds +
                TransportHazardRules.AsteroidFieldDurationVarianceSeconds - 1));
        }

        [Test]
        public void AsteroidField_AutoPilotIgnoredAppliesSeededRoomDamage()
        {
            var hazard = TransportHazardState
                .StartAsteroidField(8721, 12)
                .Tick(12f, false);

            var result = TransportHazardRules.ResolveAsteroidField(hazard);
            var damaged = TransportHazardRules.ApplyHazardResult(ShipState.CreateDefault(), result);

            Assert.That(result.Resolution, Is.EqualTo(TransportHazardResolution.DirectHit));
            Assert.That(result.RoomDamages.Length, Is.EqualTo(TransportHazardRules.AsteroidFieldDirectHitRoomCount));
            Assert.That(ShipStateRules.CalculateRepairCost(damaged), Is.GreaterThan(0));
        }

        [Test]
        public void AsteroidField_ManualAvoidancePreventsDamage()
        {
            var hazard = TransportHazardState
                .StartAsteroidField(8721, 12)
                .Tick(6f, true)
                .Tick(6f, false);

            var result = TransportHazardRules.ResolveAsteroidField(hazard);
            var ship = TransportHazardRules.ApplyHazardResult(ShipState.CreateDefault(), result);

            Assert.That(result.Resolution, Is.EqualTo(TransportHazardResolution.Avoided));
            Assert.That(result.RoomDamages, Is.Empty);
            Assert.That(ShipStateRules.CalculateRepairCost(ship), Is.Zero);
        }

        [Test]
        public void AsteroidField_TurretNeutralizedPreventsDamage()
        {
            var hazard = TransportHazardState
                .StartAsteroidField(8721, 12)
                .Tick(2f, false);

            var result = TransportHazardRules.ResolveAsteroidField(hazard, true);
            var ship = TransportHazardRules.ApplyHazardResult(ShipState.CreateDefault(), result);

            Assert.That(result.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
            Assert.That(result.RoomDamages, Is.Empty);
            Assert.That(ShipStateRules.CalculateRepairCost(ship), Is.Zero);
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
