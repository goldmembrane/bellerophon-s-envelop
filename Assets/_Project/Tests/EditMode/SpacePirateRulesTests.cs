using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class SpacePirateRulesTests
    {
        [Test]
        public void CreateAllSourceProfiles_UsesFourKindsAndBoardingCraftSourceStats()
        {
            var profiles = SpacePirateRules.CreateAllSourceSpacePirateProfiles();

            Assert.That(profiles.Length, Is.EqualTo(4));
            Assert.That(profiles[0].Kind, Is.EqualTo(SpacePirateKind.Pahur));
            Assert.That(profiles[1].Kind, Is.EqualTo(SpacePirateKind.Kurus));
            Assert.That(profiles[2].Kind, Is.EqualTo(SpacePirateKind.Istante));
            Assert.That(profiles[3].Kind, Is.EqualTo(SpacePirateKind.Ata));

            var pahur = SpacePirateRules.GetProfile(SpacePirateKind.Pahur);
            var kurus = SpacePirateRules.GetProfile(SpacePirateKind.Kurus);
            var istante = SpacePirateRules.GetProfile(SpacePirateKind.Istante);
            var ata = SpacePirateRules.GetProfile(SpacePirateKind.Ata);

            Assert.That(pahur.IntruderDefinition.MaxHealth, Is.EqualTo(150));
            Assert.That(pahur.IntruderDefinition.MovementSpeed, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(pahur.PrimaryDamage, Is.EqualTo(5));
            Assert.That(pahur.PrimaryDelaySeconds, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(pahur.PrimaryMinimumRange, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(pahur.PrimaryMaximumRange, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(pahur.MaximumFireDurationSeconds, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(pahur.ReloadWaitSeconds, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(pahur.BoardingCraft.Health, Is.EqualTo(350));
            Assert.That(pahur.BoardingCraft.WidthMeters, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(pahur.BoardingCraft.LengthMeters, Is.EqualTo(8f).Within(0.0001f));
            Assert.That(pahur.BoardingCraft.HeightMeters, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(pahur.BoardingCraft.PayloadUnitCount, Is.EqualTo(4));

            Assert.That(kurus.IntruderDefinition.MaxHealth, Is.EqualTo(130));
            Assert.That(kurus.IntruderDefinition.MovementSpeed, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(kurus.ShieldDurability, Is.EqualTo(100));
            Assert.That(kurus.PrimaryDamage, Is.EqualTo(10));
            Assert.That(kurus.PrimaryDelaySeconds, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(kurus.ShieldBashRadiusMeters, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(kurus.ShieldBashWindupSeconds, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(kurus.BoardingCraft.Health, Is.EqualTo(600));
            Assert.That(kurus.BoardingCraft.PayloadUnitCount, Is.EqualTo(4));

            Assert.That(istante.IntruderDefinition.MaxHealth, Is.EqualTo(200));
            Assert.That(istante.PrimaryDamage, Is.EqualTo(60));
            Assert.That(istante.PrimaryMinimumRange, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(istante.PrimaryMaximumRange, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(istante.PrimaryDelaySeconds, Is.EqualTo(3.5f).Within(0.0001f));
            Assert.That(istante.SecondaryDamage, Is.EqualTo(40));
            Assert.That(istante.SecondaryMinimumRange, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(istante.SecondaryMaximumRange, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(istante.SecondaryDelaySeconds, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(istante.BoardingCraft.Health, Is.EqualTo(900));
            Assert.That(istante.BoardingCraft.PayloadUnitCount, Is.EqualTo(1));

            Assert.That(ata.IntruderDefinition.MaxHealth, Is.EqualTo(120));
            Assert.That(ata.IntruderDefinition.IssuesFactionCommands, Is.True);
            Assert.That(ata.PrimaryDamage, Is.EqualTo(8));
            Assert.That(ata.PrimaryMinimumRange, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(ata.PrimaryMaximumRange, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(ata.PrimaryDelaySeconds, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(ata.CommandRadiusMeters, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(ata.SabotageCastSeconds, Is.EqualTo(60f).Within(0.0001f));
            Assert.That(ata.SabotageRecoverySeconds, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(ata.BombInstallSeconds, Is.EqualTo(45f).Within(0.0001f));
            Assert.That(ata.BoardingCraft.Health, Is.EqualTo(1200));
            Assert.That(ata.BoardingCraft.PayloadUnitCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateIntrusionAndExternalTarget_UseSpacePirateFactionAndCraftHealth()
        {
            var hazard = TransportHazardState.Start(TransportHazardType.SpacePirateRegion, 3, 60);
            var target = TransportHazardRules.CreateExternalTarget(hazard);
            var state = SpacePirateRules.CreateSpacePirateIntrusionFromHazard(
                hazard,
                1,
                ShipRoomId.Cockpit);

            Assert.That(SpacePirateRules.SelectKindForSeed(hazard.Seed), Is.EqualTo(SpacePirateKind.Ata));
            Assert.That(target.TargetType, Is.EqualTo(ExternalTargetType.SpacePirateBoardingCraft));
            Assert.That(target.MaxHealth, Is.EqualTo(SpacePirateRules.AtaBoardingCraftHealth));
            Assert.That(state.Kind, Is.Not.EqualTo(SpacePirateKind.None));
            Assert.That(state.Definition.Faction, Is.EqualTo(IntruderFaction.SpacePirate));
            Assert.That(state.Attempt.Phase, Is.EqualTo(IntrusionPhase.Boarded));
            Assert.That(state.Intruder.IsActive, Is.True);

            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.SpacePirate, IntruderFaction.SpacePirate).RelationKind,
                Is.EqualTo(IntruderRelationKind.Bonded));
            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.SpacePirate, IntruderFaction.AlienLifeform).RelationKind,
                Is.EqualTo(IntruderRelationKind.Competitive));
            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.SpacePirate, IntruderFaction.CargoFreedomLeague).RelationKind,
                Is.EqualTo(IntruderRelationKind.Hostile));
            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.SpacePirate, IntruderFaction.SeedEntity).RelationKind,
                Is.EqualTo(IntruderRelationKind.Hostile));
            Assert.That(
                IntruderRules.DetermineRelation(
                    SpacePirateRules.GetProfile(SpacePirateKind.Ata).IntruderDefinition,
                    SpacePirateRules.GetProfile(SpacePirateKind.Pahur).IntruderDefinition).RelationKind,
                Is.EqualTo(IntruderRelationKind.Commanded));
        }

        [Test]
        public void PahurKurusAndIstante_ApplyRepresentativeCombatActions()
        {
            var pahur = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Pahur,
                5,
                ShipRoomId.Cockpit,
                "space-pirate-pahur-test"));
            var pahurResult = SpacePirateRules.TickSpacePirate(
                pahur,
                ShipState.CreateDefault(),
                1f);

            Assert.That(pahurResult.ActionKind, Is.EqualTo(SpacePirateActionKind.RocketAreaAttack));
            Assert.That(pahurResult.RoomDamageApplied, Is.EqualTo(10));
            Assert.That(pahurResult.Ship.GetRoom(pahur.Intruder.TargetRoom).CurrentDurability, Is.EqualTo(90));

            var kurus = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Kurus,
                7,
                ShipRoomId.Cockpit,
                "space-pirate-kurus-test"));
            var guard = SpacePirateRules.TickSpacePirate(
                kurus,
                ShipState.CreateDefault(),
                1f);
            var bash = SpacePirateRules.TickSpacePirate(
                guard.State,
                guard.Ship,
                1f,
                encounteredTarget: true);

            Assert.That(guard.ActionKind, Is.EqualTo(SpacePirateActionKind.ShieldGuard));
            Assert.That(guard.DefensiveStanceActive, Is.True);
            Assert.That(bash.ActionKind, Is.EqualTo(SpacePirateActionKind.ShieldBash));
            Assert.That(bash.PlayerDamageApplied, Is.EqualTo(10));
            Assert.That(bash.ShieldDamageApplied, Is.EqualTo(10));

            var istante = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Istante,
                9,
                ShipRoomId.Cockpit,
                "space-pirate-istante-test"));
            var musket = SpacePirateRules.TickSpacePirate(
                istante,
                ShipState.CreateDefault(),
                1f);
            var dagger = SpacePirateRules.TickSpacePirate(
                istante,
                ShipState.CreateDefault(),
                1f,
                closeTarget: true);

            Assert.That(musket.ActionKind, Is.EqualTo(SpacePirateActionKind.MusketShot));
            Assert.That(musket.PlayerDamageApplied, Is.EqualTo(60));
            Assert.That(dagger.ActionKind, Is.EqualTo(SpacePirateActionKind.DaggerSlash));
            Assert.That(dagger.PlayerDamageApplied, Is.EqualTo(40));
        }

        [Test]
        public void Ata_IssuesFormationsAndStopsSubordinatesWhenCommanderDies()
        {
            var protective = SpacePirateRules.IssueAtaCommand(SpacePirateFormationKind.Protective);
            var breakthrough = SpacePirateRules.IssueAtaCommand(SpacePirateFormationKind.Breakthrough);
            var ata = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Ata,
                11,
                ShipRoomId.Cockpit,
                "space-pirate-ata-command-test"));
            var ataTick = SpacePirateRules.TickSpacePirate(
                ata,
                ShipState.CreateDefault(),
                1f);
            var pahur = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Pahur,
                13,
                ShipRoomId.Cockpit,
                "space-pirate-pahur-stopped-test"));
            var stopped = SpacePirateRules.TickSpacePirate(
                pahur,
                ShipState.CreateDefault(),
                1f,
                commanderAlive: false);

            Assert.That(protective.CommandRadiusMeters, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(protective.SubordinateMovementSpeed, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(protective.PlacesIstanteNearAta, Is.True);
            Assert.That(protective.PlacesKurusInFront, Is.True);
            Assert.That(protective.PlacesPahurBehindShield, Is.True);
            Assert.That(breakthrough.PlacesIstanteNearAta, Is.False);
            Assert.That(breakthrough.PlacesKurusInFront, Is.True);
            Assert.That(ataTick.ActionKind, Is.EqualTo(SpacePirateActionKind.CommandIssued));
            Assert.That(ataTick.CommandIssued, Is.True);
            Assert.That(stopped.ActionKind, Is.EqualTo(SpacePirateActionKind.SubordinatesStopped));
            Assert.That(stopped.SubordinatesStopped, Is.True);
            Assert.That(stopped.State.CommanderAlive, Is.False);
            Assert.That(stopped.State.SubordinateStopped, Is.True);
        }

        [Test]
        public void AtaSabotage_AppliesRoomAndSystemHooksAfterSourceDurations()
        {
            var ata = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Ata,
                17,
                ShipRoomId.Cockpit,
                "space-pirate-ata-sabotage-test"));
            var ship = ShipState.CreateDefault();

            var preparingEngine = SpacePirateRules.TickAtaSabotage(
                ata,
                ship,
                SpacePirateSabotageKind.EngineOutputReduction,
                59f);
            var engine = SpacePirateRules.TickAtaSabotage(
                preparingEngine.State,
                preparingEngine.Ship,
                SpacePirateSabotageKind.EngineOutputReduction,
                1f);
            var control = SpacePirateRules.TickAtaSabotage(
                ata,
                ship,
                SpacePirateSabotageKind.ControlRoomHack,
                60f);
            var cockpit = SpacePirateRules.TickAtaSabotage(
                ata,
                ship,
                SpacePirateSabotageKind.AutoPilotDisable,
                60f);
            var armory = SpacePirateRules.TickAtaSabotage(
                ata,
                ship,
                SpacePirateSabotageKind.ArmoryTurretDisable,
                60f);
            var supplyBomb = SpacePirateRules.TickAtaSabotage(
                ata,
                ship,
                SpacePirateSabotageKind.SupplyRoomBomb,
                45f);

            Assert.That(preparingEngine.SabotageApplied, Is.False);
            Assert.That(preparingEngine.State.SabotageProgressSeconds, Is.EqualTo(59f).Within(0.0001f));
            Assert.That(engine.SabotageApplied, Is.True);
            Assert.That(engine.BoosterDisabled, Is.True);
            Assert.That(engine.BlackoutRoomCount, Is.EqualTo(5));
            Assert.That(engine.RecoveryInteractionSeconds, Is.EqualTo(10f).Within(0.0001f));
            Assert.That(control.SabotageApplied, Is.True);
            Assert.That(control.ClosedCorridorCount, Is.EqualTo(7));
            Assert.That(control.Ship.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline, Is.True);
            Assert.That(cockpit.AutoPilotDisabled, Is.True);
            Assert.That(cockpit.Ship.GetRoom(ShipRoomId.Cockpit).IsFunctionOffline, Is.True);
            Assert.That(armory.TurretDisabled, Is.True);
            Assert.That(armory.Ship.GetRoom(ShipRoomId.Armory).IsFunctionOffline, Is.True);
            Assert.That(supplyBomb.SabotageApplied, Is.True);
            Assert.That(supplyBomb.RoomDamageApplied, Is.EqualTo(100));
            Assert.That(supplyBomb.Ship.GetRoom(ShipRoomId.SupplyRoom).CurrentDurability, Is.Zero);
        }

        private static SpacePirateState MoveToTarget(SpacePirateState state)
        {
            return state.WithIntruder(state.Intruder.MoveToTargetRoom());
        }
    }
}
