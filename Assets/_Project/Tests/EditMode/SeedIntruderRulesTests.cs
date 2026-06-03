using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class SeedIntruderRulesTests
    {
        [Test]
        public void CanCheckSeedIntruder_ExcludesTutorialAndAllowsFollowUpTransport()
        {
            var tutorialSession = GameSessionState.StartAssociationSession()
                .StartTransport(TransportContractDefinition.CreateTutorial());
            var followUpSession = CreateFollowUpTransportSession();

            Assert.That(SeedIntruderRules.CanCheckSeedIntruder(tutorialSession), Is.False);
            Assert.That(SeedIntruderRules.CanCheckSeedIntruder(followUpSession), Is.True);
        }

        [Test]
        public void ShouldStartSeedIntruder_UsesTwoSecondFifteenPercentChecks()
        {
            var session = CreateFollowUpTransportSession();
            var triggeringCheck = 0;
            var triggeringRoll = 100;

            for (var checkIndex = 1; checkIndex <= 200; checkIndex++)
            {
                if (!SeedIntruderRules.ShouldStartSeedIntruder(session, checkIndex))
                {
                    continue;
                }

                triggeringCheck = checkIndex;
                triggeringRoll = SeedIntruderRules.RollSeedIntruderPercent(
                    SeedIntruderRules.CreateSeedIntruderSeed(session, checkIndex));
                break;
            }

            Assert.That(SeedIntruderRules.OccurrenceCheckIntervalSeconds, Is.EqualTo(2f));
            Assert.That(SeedIntruderRules.OccurrencePercent, Is.EqualTo(15));
            Assert.That(triggeringCheck, Is.GreaterThan(0));
            Assert.That(triggeringRoll, Is.LessThan(15));
        }

        [Test]
        public void CreateParvumIntrusion_UsesConfirmedParvumStatsAndInternalBoarding()
        {
            var state = SeedIntruderRules.CreateParvumIntrusionForSeed(42, ShipRoomId.Cockpit);

            Assert.That(state.Kind, Is.EqualTo(SeedIntruderKind.Parvum));
            Assert.That(state.Definition.DefinitionId, Is.EqualTo(SeedIntruderRules.ParvumDefinitionId));
            Assert.That(state.Definition.Faction, Is.EqualTo(IntruderFaction.SeedEntity));
            Assert.That(state.Definition.MaxHealth, Is.EqualTo(55));
            Assert.That(state.Definition.MovementSpeed, Is.EqualTo(2.5f));
            Assert.That(state.Definition.AttackRange, Is.EqualTo(1f));
            Assert.That(state.Definition.AttackDelaySeconds, Is.EqualTo(0.5f));
            Assert.That(state.Attempt.Phase, Is.EqualTo(IntrusionPhase.Boarded));
            Assert.That(state.Intruder.IsActive, Is.True);
            Assert.That(state.Intruder.CurrentRoom, Is.EqualTo(IntruderRules.SelectEntryRoom(42)));
            Assert.That(state.Intruder.TargetType, Is.EqualTo(IntruderTargetType.Ship));
        }

        [Test]
        public void TickParvum_MovesToTargetAndDamagesShipEveryHalfSecond()
        {
            var state = SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit);
            var ship = ShipState.CreateDefault();
            var cargo = new CargoState(CargoGrade.Common, 45, 180, 1f, false);

            var noDamage = SeedIntruderRules.TickParvum(state, ship, cargo, 0.49f);
            var result = SeedIntruderRules.TickParvum(noDamage.State, noDamage.Ship, noDamage.Cargo, 0.51f);

            Assert.That(noDamage.RoomDamageApplied, Is.Zero);
            Assert.That(result.State.Intruder.CurrentRoom, Is.EqualTo(result.State.Intruder.TargetRoom));
            Assert.That(result.AttackCount, Is.EqualTo(2));
            Assert.That(result.RoomDamageApplied, Is.EqualTo(SeedIntruderRules.ParvumShipFacilityDamage * 2));
            Assert.That(result.Ship.GetRoom(result.State.TargetRoom).CurrentDurability, Is.EqualTo(100 - result.RoomDamageApplied));
            Assert.That(ShipStateRules.CalculateRepairCost(result.Ship), Is.GreaterThan(0));
            Assert.That(result.Cargo.DurabilityPercent, Is.EqualTo(1f));
        }

        [Test]
        public void ApplyDamage_NeutralizesParvumAndStopsFurtherDamage()
        {
            var state = SeedIntruderRules.CreateParvumIntrusionForSeed(51, ShipRoomId.Cockpit);
            var neutralized = SeedIntruderRules.ApplyDamage(state, SeedIntruderRules.ParvumHealth);
            var result = SeedIntruderRules.TickParvum(
                neutralized,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                1f);

            Assert.That(neutralized.IsResolved, Is.True);
            Assert.That(neutralized.Intruder.Resolution, Is.EqualTo(IntruderResolution.Neutralized));
            Assert.That(result.RoomDamageApplied, Is.Zero);
            Assert.That(ShipStateRules.CalculateRepairCost(result.Ship), Is.Zero);
        }

        private static GameSessionState CreateFollowUpTransportSession()
        {
            var tutorialContract = TransportContractDefinition.CreateTutorial();
            var tutorialSession = GameSessionState.StartAssociationSession().StartTransport(tutorialContract);
            var completedSession = tutorialSession.CompleteTransport(new SettlementInput(
                tutorialContract.ContractType,
                tutorialContract.Difficulty,
                tutorialContract.Cargo,
                tutorialSession.Ship,
                new CrewState(1, 0),
                tutorialSession.Wallet,
                contractBasePay: tutorialContract.RewardCredits,
                repairSupportAmount: 100));

            return completedSession.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }
    }
}
