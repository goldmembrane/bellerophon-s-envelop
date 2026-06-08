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
        public void ShouldStartSeedIntruder_ControlRoomDestroyedDoublesOccurrenceChance()
        {
            var session = CreateFollowUpTransportSession();
            var controlDestroyed = session.Ship.WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));
            var triggeringCheck = 0;
            var triggeringRoll = 100;

            for (var checkIndex = 1; checkIndex <= 200; checkIndex++)
            {
                var roll = SeedIntruderRules.RollSeedIntruderPercent(
                    SeedIntruderRules.CreateSeedIntruderSeed(session, checkIndex));
                if (roll < SeedIntruderRules.OccurrencePercent ||
                    roll >= SeedIntruderRules.OccurrencePercent * 2)
                {
                    continue;
                }

                triggeringCheck = checkIndex;
                triggeringRoll = roll;
                break;
            }

            Assert.That(triggeringCheck, Is.GreaterThan(0));
            Assert.That(SeedIntruderRules.ShouldStartSeedIntruder(session, triggeringCheck), Is.False);
            Assert.That(SeedIntruderRules.ShouldStartSeedIntruder(session, triggeringCheck, controlDestroyed), Is.True);
            Assert.That(triggeringRoll, Is.InRange(SeedIntruderRules.OccurrencePercent, SeedIntruderRules.OccurrencePercent * 2 - 1));
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
        public void CreateAllSourceSeedProfiles_UsesConfirmedEightKindsAndCoreStats()
        {
            var profiles = SeedIntruderRules.CreateAllSourceSeedProfiles();

            Assert.That(profiles.Length, Is.EqualTo(8));
            Assert.That(profiles[0].Kind, Is.EqualTo(SeedIntruderKind.Parvum));
            Assert.That(profiles[1].Kind, Is.EqualTo(SeedIntruderKind.Fuga));
            Assert.That(profiles[2].Kind, Is.EqualTo(SeedIntruderKind.LongaArma));
            Assert.That(profiles[3].Kind, Is.EqualTo(SeedIntruderKind.Tergo));
            Assert.That(profiles[4].Kind, Is.EqualTo(SeedIntruderKind.Urzere));
            Assert.That(profiles[5].Kind, Is.EqualTo(SeedIntruderKind.Societas));
            Assert.That(profiles[6].Kind, Is.EqualTo(SeedIntruderKind.Monstrum));
            Assert.That(profiles[7].Kind, Is.EqualTo(SeedIntruderKind.Mimesis));

            var fuga = SeedIntruderRules.GetProfile(SeedIntruderKind.Fuga);
            var tergo = SeedIntruderRules.GetProfile(SeedIntruderKind.Tergo);
            var urzere = SeedIntruderRules.GetProfile(SeedIntruderKind.Urzere);
            var mimesis = SeedIntruderRules.GetProfile(SeedIntruderKind.Mimesis);

            Assert.That(fuga.IntruderDefinition.DisplayName, Is.EqualTo("Fuga"));
            Assert.That(fuga.IntruderDefinition.MobilityKind, Is.EqualTo(IntruderMobilityKind.Flying));
            Assert.That(fuga.IntruderDefinition.MaxHealth, Is.EqualTo(65));
            Assert.That(fuga.CanBeExternallyRepelled, Is.False);
            Assert.That(tergo.PlayerPostureOnHit, Is.EqualTo(PlayerPostureState.KnockedDownByTergo));
            Assert.That(tergo.TergoPierceDamage, Is.EqualTo(100));
            Assert.That(tergo.TergoPinnedDrillDamage, Is.EqualTo(20));
            Assert.That(urzere.SeedAttackDamageBonusPercent, Is.EqualTo(25));
            Assert.That(urzere.NonSeedVisionPenalty, Is.EqualTo(1));
            Assert.That(mimesis.HasMimesisVoiceMimicryPlaceholder, Is.True);
            Assert.That(mimesis.StopsWhenInjectionInterrupted, Is.True);
        }

        [Test]
        public void CreateSeedIntrusionForSeed_UsesMetalCargoTargetOnlyForMetalCargo()
        {
            var metal = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Fuga,
                7,
                ShipRoomId.Cockpit,
                "seed-fuga-metal",
                CargoMaterial.CommonMetal);
            var water = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Fuga,
                7,
                ShipRoomId.Cockpit,
                "seed-fuga-water",
                CargoMaterial.Water);

            Assert.That(metal.Definition.PrimaryObjective, Is.EqualTo(IntruderObjectiveType.AttackCargo));
            Assert.That(metal.Intruder.TargetType, Is.EqualTo(IntruderTargetType.Cargo));
            Assert.That(metal.Intruder.TargetRoom, Is.EqualTo(ShipRoomId.CargoHold));
            Assert.That(water.Definition.PrimaryObjective, Is.EqualTo(IntruderObjectiveType.DestroyShip));
            Assert.That(water.Intruder.TargetType, Is.EqualTo(IntruderTargetType.Ship));
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
        public void TickSeedIntruder_DamagesMetalCargoWhenMetalCargoIsTheTarget()
        {
            var state = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Societas,
                47,
                ShipRoomId.Cockpit,
                "seed-societas-metal",
                CargoMaterial.RareMetal);
            var ship = ShipState.CreateDefault();
            var cargo = new CargoState(CargoGrade.Rare, 45, 180, 1f, false);

            var result = SeedIntruderRules.TickSeedIntruder(
                state,
                ship,
                cargo,
                SeedIntruderRules.SocietasAttackDelaySeconds,
                cargoMaterial: CargoMaterial.RareMetal);

            Assert.That(result.AttackCount, Is.EqualTo(1));
            Assert.That(result.RoomDamageApplied, Is.Zero);
            Assert.That(result.CargoDamagePercentApplied, Is.EqualTo(IntruderRules.DefaultCargoDamagePercent));
            Assert.That(result.Cargo.DurabilityPercent, Is.EqualTo(1f - IntruderRules.DefaultCargoDamagePercent));
            Assert.That(ShipStateRules.CalculateRepairCost(result.Ship), Is.Zero);
        }

        [Test]
        public void TickSeedIntruder_AppliesRepresentativeSpecialEffects()
        {
            var tergo = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Tergo,
                11,
                ShipRoomId.Cockpit,
                "seed-tergo");
            var tergoResult = SeedIntruderRules.TickSeedIntruder(
                tergo,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                0.1f);

            Assert.That(tergoResult.AttackCount, Is.EqualTo(1));
            Assert.That(tergoResult.PlayerDamageApplied, Is.EqualTo(SeedIntruderRules.TergoPierceDamage));
            Assert.That(tergoResult.SpecialEffectKind, Is.EqualTo(SeedIntruderSpecialEffectKind.TergoPostureBreak));
            Assert.That(tergoResult.PlayerPostureApplied, Is.EqualTo(PlayerPostureState.KnockedDownByTergo));

            var urzere = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Urzere,
                12,
                ShipRoomId.Cockpit,
                "seed-urzere");
            var urzereResult = SeedIntruderRules.TickSeedIntruder(
                urzere,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                0.1f);

            Assert.That(urzereResult.AttackCount, Is.Zero);
            Assert.That(urzereResult.SpecialEffectKind, Is.EqualTo(SeedIntruderSpecialEffectKind.SeedEntityEmpowerment));
            Assert.That(urzereResult.SeedAttackDamageBonusPercentApplied, Is.EqualTo(25));
            Assert.That(urzereResult.SeedMovementSpeedBonusApplied, Is.EqualTo(1f));
            Assert.That(urzereResult.SeedHealthRegenPerSecondApplied, Is.EqualTo(5));
            Assert.That(urzereResult.NonSeedVisionPenaltyApplied, Is.EqualTo(1));

            var monstrum = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Monstrum,
                13,
                ShipRoomId.Cockpit,
                "seed-monstrum");
            var monstrumResult = SeedIntruderRules.TickSeedIntruder(
                monstrum,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                SeedIntruderRules.MonstrumAttackDelaySeconds);

            Assert.That(monstrumResult.RoomDamageApplied, Is.EqualTo(SeedIntruderRules.MonstrumDamage));
            Assert.That(monstrumResult.SpecialEffectKind, Is.EqualTo(SeedIntruderSpecialEffectKind.RoomWideMovementSlow));
            Assert.That(monstrumResult.RoomWideMovementSlowPercentApplied, Is.EqualTo(80));
            Assert.That(monstrumResult.RoomWideMovementSlowDurationSecondsApplied, Is.EqualTo(1f));
        }

        [Test]
        public void SeedRelations_KeepSeedCompetitionAndMimesisPlayerFocusException()
        {
            var seedToSeed = IntruderRules.DetermineRelation(
                IntruderFaction.SeedEntity,
                IntruderFaction.SeedEntity);
            var seedToAlien = SeedIntruderRules.DetermineSeedRelation(
                SeedIntruderKind.Parvum,
                IntruderFaction.AlienLifeform);
            var mimesisToPirate = SeedIntruderRules.DetermineSeedRelation(
                SeedIntruderKind.Mimesis,
                IntruderFaction.SpacePirate);

            Assert.That(seedToSeed.RelationKind, Is.EqualTo(IntruderRelationKind.Competitive));
            Assert.That(seedToAlien.RelationKind, Is.EqualTo(IntruderRelationKind.Hostile));
            Assert.That(mimesisToPirate.RelationKind, Is.EqualTo(IntruderRelationKind.Competitive));
        }

        [Test]
        public void RetargetMetalFeederToPlayer_SwitchesInterruptedMetalFeedingToAttacker()
        {
            var state = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Parvum,
                47,
                ShipRoomId.Cockpit,
                "seed-parvum-interrupted",
                CargoMaterial.CommonMetal);

            var retargeted = SeedIntruderRules.RetargetMetalFeederToPlayer(state, ShipRoomId.Armory);

            Assert.That(state.Intruder.TargetType, Is.EqualTo(IntruderTargetType.Cargo));
            Assert.That(retargeted.Intruder.TargetType, Is.EqualTo(IntruderTargetType.Player));
            Assert.That(retargeted.Intruder.TargetRoom, Is.EqualTo(ShipRoomId.Armory));
            Assert.That(retargeted.Intruder.Objective, Is.EqualTo(IntruderObjectiveType.AttackPlayer));
        }

        [Test]
        public void TickParvum_UsesProvidedRoomDamagePerAttack()
        {
            var state = SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit);
            var ship = ShipState.CreateDefault();
            var cargo = new CargoState(CargoGrade.Common, 45, 180, 1f, false);

            var result = SeedIntruderRules.TickParvum(state, ship, cargo, SeedIntruderRules.ParvumAttackDelaySeconds, 9);

            Assert.That(result.AttackCount, Is.EqualTo(1));
            Assert.That(result.RoomDamageApplied, Is.EqualTo(9));
            Assert.That(result.Ship.GetRoom(result.State.TargetRoom).CurrentDurability, Is.EqualTo(91));
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
