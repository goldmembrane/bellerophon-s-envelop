using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep15CargoFreedomLeagueEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 15 Cargo Freedom League editor validation passed.");
            Debug.Log("Detailed step 15 Cargo Freedom League validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var profiles = CargoFreedomLeagueRules.CreateAllSourceCargoFreedomLeagueProfiles();
            if (profiles.Length != 4 ||
                profiles[0].Kind != CargoFreedomLeagueKind.Negatif ||
                profiles[3].Kind != CargoFreedomLeagueKind.Revolution)
            {
                throw new InvalidOperationException("Detailed step 15 must expose the four Cargo Freedom League unit kinds.");
            }

            if (profiles[0].BoardingCraft.Health != CargoFreedomLeagueRules.NegatifBoardingCraftHealth ||
                profiles[1].BoardingCraft.Health != CargoFreedomLeagueRules.RebellionBoardingCraftHealth ||
                profiles[2].BoardingCraft.Health != CargoFreedomLeagueRules.ResistanceBoardingCraftHealth ||
                profiles[3].BoardingCraft.Health != CargoFreedomLeagueRules.RevolutionBoardingCraftHealth)
            {
                throw new InvalidOperationException("Detailed step 15 boarding craft health must follow the source values.");
            }

            var hazard = TransportHazardState.Start(TransportHazardType.CargoFreedomLeagueRegion, 3, 30);
            var target = TransportHazardRules.CreateExternalTarget(hazard);
            if (target.TargetType != ExternalTargetType.CargoFreedomLeagueBoardingCraft ||
                target.MaxHealth != CargoFreedomLeagueRules.RevolutionBoardingCraftHealth)
            {
                throw new InvalidOperationException("Detailed step 15 Cargo Freedom League region must create a source-health boarding craft target.");
            }

            var turret = ManualTurretState.Start(true).SetAim(target.PositionX, target.PositionY);
            ManualTurretFireResult shot = default;
            var shotCount = 0;
            while (!target.IsDestroyed && shotCount < 30)
            {
                shot = turret.FireAt(target);
                turret = shot.Turret;
                target = shot.Target;
                shotCount++;
            }

            if (shot.Outcome != ManualTurretFireOutcome.Destroyed || shotCount != 20)
            {
                throw new InvalidOperationException("Detailed step 15 Revolution boarding craft must be destroyed by twenty 50-damage turret shots.");
            }

            var cargo = new CargoState(CargoGrade.Common, 50, 500, 1f, false);
            var negatif = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Negatif,
                5,
                ShipRoomId.Cockpit,
                "detailed-step15-negatif"));
            var negatifTick = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                negatif,
                ShipState.CreateDefault(),
                cargo,
                1f);
            var negatifDrop = CargoFreedomLeagueRules.ResolveDrops(
                CargoFreedomLeagueRules.ApplyDamage(negatifTick.State, 100),
                false);
            if (negatifTick.CargoDamagePercentApplied != CargoFreedomLeagueRules.NegatifCargoDamagePercentPerAttack ||
                negatifDrop.DropKind != CargoFreedomLeagueDropKind.StoredCargo)
            {
                throw new InvalidOperationException("Detailed step 15 Negatif must damage, store, and drop recoverable cargo.");
            }

            var rebellion = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Rebellion,
                7,
                ShipRoomId.Cockpit,
                "detailed-step15-rebellion"));
            var shield = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                rebellion,
                ShipState.CreateDefault(),
                cargo,
                1f);
            if (shield.ActionKind != CargoFreedomLeagueActionKind.CargoShieldInstalled ||
                !shield.State.CargoShieldInstalled)
            {
                throw new InvalidOperationException("Detailed step 15 Rebellion must install a cargo shield.");
            }

            var resistance = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Resistance,
                9,
                ShipRoomId.Cockpit,
                "detailed-step15-resistance"));
            var loot = CargoFreedomLeagueRules.TickCargoFreedomLeague(
                resistance,
                ShipState.CreateDefault(),
                cargo,
                1f);
            var resistanceDrop = CargoFreedomLeagueRules.ResolveDrops(
                CargoFreedomLeagueRules.ApplyDamage(loot.State, 150),
                true);
            if (loot.LootedEquipmentKind != EquipmentItemKind.Musket ||
                !resistanceDrop.ResistanceChipDropped)
            {
                throw new InvalidOperationException("Detailed step 15 Resistance must loot a weapon and expose the special mission chip hook.");
            }

            var revolution = MoveToTarget(CargoFreedomLeagueRules.CreateCargoFreedomLeagueIntrusionForSeed(
                CargoFreedomLeagueKind.Revolution,
                11,
                ShipRoomId.Cockpit,
                "detailed-step15-revolution"));
            var damagedSupply = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(30, 100));
            var installing = CargoFreedomLeagueRules.TickCargoFreedomLeague(revolution, damagedSupply, cargo, 1f);
            var armed = CargoFreedomLeagueRules.TickCargoFreedomLeague(installing.State, installing.Ship, installing.Cargo, 2f);
            var waiting = CargoFreedomLeagueRules.TickCargoFreedomLeague(armed.State, armed.Ship, armed.Cargo, 9f);
            var detonated = CargoFreedomLeagueRules.TickCargoFreedomLeague(waiting.State, waiting.Ship, waiting.Cargo, 1f);
            if (!detonated.BombDetonated ||
                detonated.Ship.GetRoom(ShipRoomId.SupplyRoom).CurrentDurability != 0 ||
                detonated.State.Intruder.TargetRoom == ShipRoomId.SupplyRoom)
            {
                throw new InvalidOperationException("Detailed step 15 Revolution must arm a bomb, destroy the supply room, and retarget.");
            }

            var alienRelation = IntruderRules.DetermineRelation(
                IntruderFaction.CargoFreedomLeague,
                IntruderFaction.AlienLifeform);
            var pirateRelation = IntruderRules.DetermineRelation(
                IntruderFaction.CargoFreedomLeague,
                IntruderFaction.SpacePirate);
            if (alienRelation.RelationKind != IntruderRelationKind.Competitive ||
                pirateRelation.RelationKind != IntruderRelationKind.Hostile)
            {
                throw new InvalidOperationException("Detailed step 15 Cargo Freedom League faction relations must match the source.");
            }

            return "Kinds=4; CraftHP=" + profiles[0].BoardingCraft.Health + "/" +
                   profiles[1].BoardingCraft.Health + "/" +
                   profiles[2].BoardingCraft.Health + "/" +
                   profiles[3].BoardingCraft.Health +
                   "; TurretShots=" + shotCount +
                   "; NegatifCargo=" + negatifTick.CargoDamagePercentApplied.ToString("0.00") +
                   "; ResistanceChip=" + resistanceDrop.ResistanceChipDropped +
                   "; RevolutionBomb=" + detonated.BombDetonated;
        }

        private static CargoFreedomLeagueState MoveToTarget(CargoFreedomLeagueState state)
        {
            return state.WithIntruder(state.Intruder.MoveToTargetRoom());
        }
    }
}
