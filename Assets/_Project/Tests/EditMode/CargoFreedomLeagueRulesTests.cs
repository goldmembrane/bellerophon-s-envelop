using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class CargoFreedomLeagueRulesTests
    {
        [Test]
        public void CreateAllSourceProfiles_UsesFourKindsAndBoardingCraftSourceStats()
        {
            var profiles = CargoFreedomLeagueRules.CreateAllSourceCargoFreedomLeagueProfiles();

            Assert.That(profiles.Length, Is.EqualTo(4));
            Assert.That(profiles[0].Kind, Is.EqualTo(CargoFreedomLeagueKind.Negatif));
            Assert.That(profiles[1].Kind, Is.EqualTo(CargoFreedomLeagueKind.Rebellion));
            Assert.That(profiles[2].Kind, Is.EqualTo(CargoFreedomLeagueKind.Resistance));
            Assert.That(profiles[3].Kind, Is.EqualTo(CargoFreedomLeagueKind.Revolution));

            var negatif = CargoFreedomLeagueRules.GetProfile(CargoFreedomLeagueKind.Negatif);
            var rebellion = CargoFreedomLeagueRules.GetProfile(CargoFreedomLeagueKind.Rebellion);
            var resistance = CargoFreedomLeagueRules.GetProfile(CargoFreedomLeagueKind.Resistance);
            var revolution = CargoFreedomLeagueRules.GetProfile(CargoFreedomLeagueKind.Revolution);

            Assert.That(negatif.IntruderDefinition.MaxHealth, Is.EqualTo(40));
            Assert.That(negatif.IntruderDefinition.MovementSpeed, Is.EqualTo(2.8f));
            Assert.That(negatif.AttackDamage, Is.EqualTo(8));
            Assert.That(negatif.CargoDamagePercentPerAttack, Is.EqualTo(0.08f));
            Assert.That(negatif.StoredCargoRecoveryPercent, Is.EqualTo(0.7f));
            Assert.That(negatif.HasConfirmedAttackDelay, Is.False);
            Assert.That(negatif.BoardingCraft.Health, Is.EqualTo(300));
            Assert.That(negatif.BoardingCraft.TotalUnitCount, Is.EqualTo(15));

            Assert.That(rebellion.IntruderDefinition.MaxHealth, Is.EqualTo(60));
            Assert.That(rebellion.InstallsCargoShield, Is.True);
            Assert.That(rebellion.AttackModeTransitionSeconds, Is.EqualTo(2f));
            Assert.That(rebellion.SweepDurationSeconds, Is.EqualTo(0.8f));
            Assert.That(rebellion.BoardingCraft.Health, Is.EqualTo(450));
            Assert.That(rebellion.BoardingCraft.TotalUnitCount, Is.EqualTo(6));

            Assert.That(resistance.IntruderDefinition.MaxHealth, Is.EqualTo(100));
            Assert.That(resistance.LootedWeaponKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(resistance.LootedWeaponDamage, Is.EqualTo(50));
            Assert.That(resistance.LootedWeaponMinimumRange, Is.EqualTo(5f));
            Assert.That(resistance.LootedWeaponMaximumRange, Is.EqualTo(7f));
            Assert.That(resistance.LootedWeaponAttackDelaySeconds, Is.EqualTo(3.5f));
            Assert.That(resistance.BoardingCraft.Health, Is.EqualTo(700));
            Assert.That(resistance.BoardingCraft.TotalUnitCount, Is.EqualTo(4));

            Assert.That(revolution.IntruderDefinition.MaxHealth, Is.EqualTo(200));
            Assert.That(revolution.TransformDurationSeconds, Is.EqualTo(1.5f));
            Assert.That(revolution.BombInstallDurationSeconds, Is.EqualTo(3f));
            Assert.That(revolution.BombDetonationDelaySeconds, Is.EqualTo(10f));
            Assert.That(revolution.MaximumSustainedAttackSeconds, Is.EqualTo(8f));
            Assert.That(revolution.HasConfirmedMovementSpeed, Is.False);
            Assert.That(revolution.BoardingCraft.Health, Is.EqualTo(1000));
            Assert.That(revolution.BoardingCraft.TotalUnitCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateIntrusionAndExternalTarget_UseCargoFreedomFactionAndCraftHealth()
        {
            var hazard = TransportHazardState.Start(TransportHazardType.CargoFreedomLeagueRegion, 2, 20);
            var target = TransportHazardRules.CreateExternalTarget(hazard);
            var selectedCraft = CargoFreedomLeagueRules.GetBoardingCraftProfile(
                CargoFreedomLeagueRules.SelectKindForSeed(hazard.Seed));
            var state = CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionFromHazard(
                hazard,
                1,
                ShipRoomId.Cockpit);

            Assert.That(target.TargetType, Is.EqualTo(ExternalTargetType.CargoFreedomLeagueBoardingCraft));
            Assert.That(target.MaxHealth, Is.EqualTo(selectedCraft.Health));
            Assert.That(state.Kind, Is.Not.EqualTo(CargoFreedomLeagueKind.None));
            Assert.That(state.Definition.Faction, Is.EqualTo(IntruderFaction.CargoFreedomLeague));
            Assert.That(state.Attempt.Phase, Is.EqualTo(IntrusionPhase.Boarded));
            Assert.That(state.Intruder.IsActive, Is.True);

            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.CargoFreedomLeague, IntruderFaction.CargoFreedomLeague).RelationKind,
                Is.EqualTo(IntruderRelationKind.Bonded));
            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.CargoFreedomLeague, IntruderFaction.AlienLifeform).RelationKind,
                Is.EqualTo(IntruderRelationKind.Competitive));
            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.CargoFreedomLeague, IntruderFaction.SeedEntity).RelationKind,
                Is.EqualTo(IntruderRelationKind.Hostile));
            Assert.That(
                IntruderRules.DetermineRelation(IntruderFaction.CargoFreedomLeague, IntruderFaction.SpacePirate).RelationKind,
                Is.EqualTo(IntruderRelationKind.Hostile));
        }

        [Test]
        public void Negatif_DamagesStoresRetargetsAndDropsRecoveredCargo()
        {
            var state = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Negatif,
                5,
                ShipRoomId.Cockpit,
                "cargo-negatif-test"));
            var cargo = new CargoState(CargoGrade.Common, 50, 500, 1f, false);

            var result = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                state,
                ShipState.CreateDefault(),
                cargo,
                1f);
            var retargeted = CargoFreedomLeagueRules.RetargetToAttacker(result.State, ShipRoomId.Cockpit);
            var neutralized = CargoFreedomLeagueRules.ApplyDamage(result.State, 100);
            var drops = CargoFreedomLeagueRules.ResolveDrops(neutralized, false);

            Assert.That(result.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.CargoDamagedAndStored));
            Assert.That(result.Cargo.DurabilityPercent, Is.EqualTo(0.92f).Within(0.0001f));
            Assert.That(result.State.StoredCargoPercent, Is.EqualTo(0.08f).Within(0.0001f));
            Assert.That(retargeted.Intruder.TargetType, Is.EqualTo(IntruderTargetType.Player));
            Assert.That(drops.DropKind, Is.EqualTo(CargoFreedomLeagueDropKind.StoredCargo));
            Assert.That(drops.RecoveredCargoPercent, Is.EqualTo(0.056f).Within(0.0001f));
        }

        [Test]
        public void Rebellion_InstallsCargoShieldThenAttacksCargoHoldArea()
        {
            var state = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Rebellion,
                7,
                ShipRoomId.Cockpit,
                "cargo-rebellion-test"));
            var cargo = new CargoState(CargoGrade.Common, 50, 500, 1f, false);

            var shield = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                state,
                ShipState.CreateDefault(),
                cargo,
                1f);
            var attack = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                shield.State,
                shield.Ship,
                shield.Cargo,
                1f);

            Assert.That(shield.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.CargoShieldInstalled));
            Assert.That(shield.CargoShieldInstalled, Is.True);
            Assert.That(shield.State.CargoShieldInstalled, Is.True);
            Assert.That(attack.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.ShieldedAreaAttack));
            Assert.That(attack.RoomDamageApplied, Is.EqualTo(5));
            Assert.That(attack.Ship.GetRoom(ShipRoomId.CargoHold).CurrentDurability, Is.EqualTo(95));
        }

        [Test]
        public void Resistance_LootsWeaponAndDropsWeaponPlusSpecialMissionChip()
        {
            var state = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Resistance,
                9,
                ShipRoomId.Cockpit,
                "cargo-resistance-test"));
            var cargo = new CargoState(CargoGrade.Common, 50, 500, 1f, false);

            var loot = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                state,
                ShipState.CreateDefault(),
                cargo,
                1f);
            var neutralized = CargoFreedomLeagueRules.ApplyDamage(loot.State, 150);
            var drops = CargoFreedomLeagueRules.ResolveDrops(neutralized, true);

            Assert.That(loot.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.WeaponLooted));
            Assert.That(loot.LootedEquipmentKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(loot.State.StolenEquipmentKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(drops.DropKind, Is.EqualTo(CargoFreedomLeagueDropKind.StolenWeapon));
            Assert.That(drops.DroppedEquipmentKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(drops.ResistanceChipDropped, Is.True);
            Assert.That(drops.RevolutionChipDropped, Is.False);
        }

        [Test]
        public void Revolution_ArmsBombDetonatesSupplyRoomAndDropsSpecialMissionChip()
        {
            var state = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Revolution,
                11,
                ShipRoomId.Cockpit,
                "cargo-revolution-test"));
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(30, 100));
            var cargo = new CargoState(CargoGrade.Common, 50, 500, 1f, false);

            var installing = CargoFreedomLeagueRules.TickCargoFreedomLeague(state, ship, cargo, 1f);
            var armed = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                installing.State,
                installing.Ship,
                installing.Cargo,
                2f);
            var waiting = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                armed.State,
                armed.Ship,
                armed.Cargo,
                9f);
            var detonated = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                waiting.State,
                waiting.Ship,
                waiting.Cargo,
                1f);
            var neutralized = CargoFreedomLeagueRules.ApplyDamage(detonated.State, 250);
            var drops = CargoFreedomLeagueRules.ResolveDrops(neutralized, true);

            Assert.That(installing.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.BombInstalling));
            Assert.That(armed.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.BombArmed));
            Assert.That(waiting.BombDetonated, Is.False);
            Assert.That(detonated.ActionKind, Is.EqualTo(CargoFreedomLeagueActionKind.BombDetonated));
            Assert.That(detonated.BombDetonated, Is.True);
            Assert.That(detonated.Retargeted, Is.True);
            Assert.That(detonated.Ship.GetRoom(ShipRoomId.SupplyRoom).CurrentDurability, Is.Zero);
            Assert.That(detonated.State.Intruder.TargetRoom, Is.Not.EqualTo(ShipRoomId.SupplyRoom));
            Assert.That(drops.RevolutionChipDropped, Is.True);
        }

        private static CargoFreedomLeagueState MoveToTarget(CargoFreedomLeagueState state)
        {
            return state.WithIntruder(state.Intruder.MoveToTargetRoom());
        }
    }
}
