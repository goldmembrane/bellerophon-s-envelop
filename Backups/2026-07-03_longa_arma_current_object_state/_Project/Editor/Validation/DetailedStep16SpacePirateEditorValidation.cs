using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep16SpacePirateEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 16 space pirate editor validation passed.");
            Debug.Log("Detailed step 16 space pirate validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var profiles = SpacePirateRules.CreateAllSourceSpacePirateProfiles();
            if (profiles.Length != 4 ||
                profiles[0].Kind != SpacePirateKind.Pahur ||
                profiles[1].Kind != SpacePirateKind.Kurus ||
                profiles[2].Kind != SpacePirateKind.Istante ||
                profiles[3].Kind != SpacePirateKind.Ata)
            {
                throw new InvalidOperationException("Detailed step 16 must expose the four source space pirate kinds.");
            }

            if (profiles[0].BoardingCraft.Health != SpacePirateRules.PahurBoardingCraftHealth ||
                profiles[1].BoardingCraft.Health != SpacePirateRules.KurusBoardingCraftHealth ||
                profiles[2].BoardingCraft.Health != SpacePirateRules.IstanteBoardingCraftHealth ||
                profiles[3].BoardingCraft.Health != SpacePirateRules.AtaBoardingCraftHealth)
            {
                throw new InvalidOperationException("Detailed step 16 boarding craft health must follow the source values.");
            }

            var hazard = TransportHazardState.Start(TransportHazardType.SpacePirateRegion, 3, 60);
            var target = TransportHazardRules.CreateExternalTarget(hazard);
            if (target.TargetType != ExternalTargetType.SpacePirateBoardingCraft ||
                target.MaxHealth != SpacePirateRules.AtaBoardingCraftHealth)
            {
                throw new InvalidOperationException("Detailed step 16 space pirate region must create a source-health boarding craft target.");
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

            if (shot.Outcome != ManualTurretFireOutcome.Destroyed || shotCount != 24)
            {
                throw new InvalidOperationException("Detailed step 16 Ata boarding craft must be destroyed by twenty-four 50-damage turret shots.");
            }

            var pahur = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Pahur,
                5,
                ShipRoomId.Cockpit,
                "detailed-step16-pahur"));
            var pahurTick = SpacePirateRules.TickSpacePirate(
                pahur,
                ShipState.CreateDefault(),
                1f);
            if (pahurTick.ActionKind != SpacePirateActionKind.RocketAreaAttack ||
                pahurTick.RoomDamageApplied != 10)
            {
                throw new InvalidOperationException("Detailed step 16 Pahur must apply its rocket area attack.");
            }

            var kurus = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Kurus,
                7,
                ShipRoomId.Cockpit,
                "detailed-step16-kurus"));
            var guard = SpacePirateRules.TickSpacePirate(kurus, ShipState.CreateDefault(), 1f);
            var bash = SpacePirateRules.TickSpacePirate(guard.State, guard.Ship, 1f, encounteredTarget: true);
            if (guard.ActionKind != SpacePirateActionKind.ShieldGuard ||
                !guard.DefensiveStanceActive ||
                bash.ActionKind != SpacePirateActionKind.ShieldBash ||
                bash.PlayerDamageApplied != SpacePirateRules.KurusShieldBashDamage)
            {
                throw new InvalidOperationException("Detailed step 16 Kurus must guard with its shield and bash encountered targets.");
            }

            var istante = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Istante,
                9,
                ShipRoomId.Cockpit,
                "detailed-step16-istante"));
            var musket = SpacePirateRules.TickSpacePirate(istante, ShipState.CreateDefault(), 1f);
            var dagger = SpacePirateRules.TickSpacePirate(istante, ShipState.CreateDefault(), 1f, closeTarget: true);
            if (musket.ActionKind != SpacePirateActionKind.MusketShot ||
                musket.PlayerDamageApplied != SpacePirateRules.IstanteMusketDamage ||
                dagger.ActionKind != SpacePirateActionKind.DaggerSlash ||
                dagger.PlayerDamageApplied != SpacePirateRules.IstanteDaggerDamage)
            {
                throw new InvalidOperationException("Detailed step 16 Istante must use musket and dagger attacks.");
            }

            var protective = SpacePirateRules.IssueAtaCommand(SpacePirateFormationKind.Protective);
            var breakthrough = SpacePirateRules.IssueAtaCommand(SpacePirateFormationKind.Breakthrough);
            var stopped = SpacePirateRules.TickSpacePirate(
                pahur,
                ShipState.CreateDefault(),
                1f,
                commanderAlive: false);
            if (protective.CommandRadiusMeters != SpacePirateRules.AtaCommandRadiusMeters ||
                !protective.PlacesIstanteNearAta ||
                !breakthrough.PlacesKurusInFront ||
                stopped.ActionKind != SpacePirateActionKind.SubordinatesStopped)
            {
                throw new InvalidOperationException("Detailed step 16 Ata command formations and death stop behavior must be represented.");
            }

            var ata = MoveToTarget(SpacePirateRules.CreateSpacePirateIntrusionForSeed(
                SpacePirateKind.Ata,
                11,
                ShipRoomId.Cockpit,
                "detailed-step16-ata"));
            var enginePreparing = SpacePirateRules.TickAtaSabotage(
                ata,
                ShipState.CreateDefault(),
                SpacePirateSabotageKind.EngineOutputReduction,
                59f);
            var engine = SpacePirateRules.TickAtaSabotage(
                enginePreparing.State,
                enginePreparing.Ship,
                SpacePirateSabotageKind.EngineOutputReduction,
                1f);
            var control = SpacePirateRules.TickAtaSabotage(
                ata,
                ShipState.CreateDefault(),
                SpacePirateSabotageKind.ControlRoomHack,
                60f);
            var supplyBomb = SpacePirateRules.TickAtaSabotage(
                ata,
                ShipState.CreateDefault(),
                SpacePirateSabotageKind.SupplyRoomBomb,
                45f);
            if (enginePreparing.SabotageApplied ||
                !engine.SabotageApplied ||
                !engine.BoosterDisabled ||
                engine.BlackoutRoomCount != SpacePirateRules.AtaEngineBlackoutRoomCount ||
                !control.SabotageApplied ||
                control.ClosedCorridorCount != SpacePirateRules.AtaControlHackClosedCorridorCount ||
                !control.Ship.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline ||
                !supplyBomb.SabotageApplied ||
                supplyBomb.Ship.GetRoom(ShipRoomId.SupplyRoom).CurrentDurability != 0)
            {
                throw new InvalidOperationException("Detailed step 16 Ata sabotage hooks must apply after the source durations.");
            }

            var alienRelation = IntruderRules.DetermineRelation(
                IntruderFaction.SpacePirate,
                IntruderFaction.AlienLifeform);
            var cargoRelation = IntruderRules.DetermineRelation(
                IntruderFaction.SpacePirate,
                IntruderFaction.CargoFreedomLeague);
            var commandRelation = IntruderRules.DetermineRelation(
                SpacePirateRules.GetProfile(SpacePirateKind.Ata).IntruderDefinition,
                SpacePirateRules.GetProfile(SpacePirateKind.Kurus).IntruderDefinition);
            if (alienRelation.RelationKind != IntruderRelationKind.Competitive ||
                cargoRelation.RelationKind != IntruderRelationKind.Hostile ||
                commandRelation.RelationKind != IntruderRelationKind.Commanded)
            {
                throw new InvalidOperationException("Detailed step 16 space pirate faction relations must match the source.");
            }

            return "Kinds=4; CraftHP=" + profiles[0].BoardingCraft.Health + "/" +
                   profiles[1].BoardingCraft.Health + "/" +
                   profiles[2].BoardingCraft.Health + "/" +
                   profiles[3].BoardingCraft.Health +
                   "; TurretShots=" + shotCount +
                   "; PahurRocket=" + pahurTick.RoomDamageApplied +
                   "; KurusBash=" + bash.PlayerDamageApplied +
                   "; Istante=" + musket.PlayerDamageApplied + "/" + dagger.PlayerDamageApplied +
                   "; AtaSabotage=" + engine.SabotageApplied + "/" + control.SabotageApplied + "/" + supplyBomb.SabotageApplied;
        }

        private static SpacePirateState MoveToTarget(SpacePirateState state)
        {
            return state.WithIntruder(state.Intruder.MoveToTargetRoom());
        }
    }
}
