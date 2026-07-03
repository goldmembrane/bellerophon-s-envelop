using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep14AlienLifeformEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 14 alien lifeform editor validation passed.");
            Debug.Log("Detailed step 14 alien lifeform validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var profiles = AlienLifeformRules.CreateAllSourceAlienProfiles();
            if (profiles.Length != 7 ||
                profiles[0].Kind != AlienLifeformKind.Cantabile ||
                profiles[6].Kind != AlienLifeformKind.Dolore)
            {
                throw new InvalidOperationException("Detailed step 14 must expose the confirmed seven alien lifeform kinds.");
            }

            for (var i = 0; i < profiles.Length; i++)
            {
                if (!profiles[i].CanBeExternallyRepelled ||
                    profiles[i].ExternalIntrusionObjectHealth != AlienLifeformRules.ExternalIntrusionObjectHealth)
                {
                    throw new InvalidOperationException("Detailed step 14 alien lifeforms must all use the confirmed external intrusion object health.");
                }
            }

            var hazard = TransportHazardState.Start(TransportHazardType.AlienLifeRegion, 991, 30);
            var target = TransportHazardRules.CreateExternalTarget(hazard);
            if (target.TargetType != ExternalTargetType.AlienLifeform ||
                target.MaxHealth != AlienLifeformRules.ExternalIntrusionObjectHealth)
            {
                throw new InvalidOperationException("Detailed step 14 alien life region must create a 350 HP alien external target.");
            }

            var turret = ManualTurretState.Start(true).SetAim(target.PositionX, target.PositionY);
            ManualTurretFireResult shot = default;
            var shotCount = 0;
            while (!target.IsDestroyed && shotCount < 20)
            {
                shot = turret.FireAt(target);
                turret = shot.Turret;
                target = shot.Target;
                shotCount++;
            }

            if (shot.Outcome != ManualTurretFireOutcome.Destroyed || shotCount != 7)
            {
                throw new InvalidOperationException("Detailed step 14 alien external target must be destroyed by seven 50-damage turret shots.");
            }

            var cantabile = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.Cantabile,
                17,
                ShipRoomId.EngineRoom,
                "detailed-step14-cantabile");
            var cantabileTick = AlienLifeformRules.TickAlienLifeform(
                cantabile,
                ShipState.CreateDefault(),
                AlienLifeformRules.CantabileAttackDelaySeconds,
                ShipRoomId.EngineRoom);
            if (cantabileTick.StatusEffectToApply.Kind != CombatStatusEffectKind.Stopped ||
                !cantabileTick.EngineRoomResonanceApplied)
            {
                throw new InvalidOperationException("Detailed step 14 Cantabile must expose sonic stop and engine room resonance.");
            }

            var conSpirito = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.ConSpirito,
                19,
                ShipRoomId.Cockpit,
                "detailed-step14-con-spirito");
            var charge = AlienLifeformRules.TickAlienLifeform(
                conSpirito,
                ShipState.CreateDefault(),
                0.1f,
                encounteredFaction: IntruderFaction.CargoFreedomLeague);
            if (charge.State.Phase != AlienLifeformBehaviorPhase.Charging ||
                charge.PlayerDamageApplied != AlienLifeformRules.ConSpiritoChargeDamage)
            {
                throw new InvalidOperationException("Detailed step 14 Con Spirito must charge encountered non-alien factions.");
            }

            var amplifiedBurn = AlienLifeformRules.AmplifyStatusEffectForGrave(
                CombatStatusEffectRules.CreateBurn(5f, 10));
            if (amplifiedBurn.DurationSeconds != 10f || amplifiedBurn.TickDamage != 15)
            {
                throw new InvalidOperationException("Detailed step 14 Grave must amplify status damage and duration.");
            }

            var smorzando = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.Smorzando,
                21,
                ShipRoomId.Cockpit,
                "detailed-step14-smorzando");
            var smorzandoTick = AlienLifeformRules.TickAlienLifeform(
                smorzando,
                ShipState.CreateDefault(),
                10f);
            if (smorzandoTick.StatusEffectToApply.Kind != CombatStatusEffectKind.Stopped ||
                smorzandoTick.PlayerDamageApplied != 60)
            {
                throw new InvalidOperationException("Detailed step 14 Smorzando must stack absorption into a stop effect.");
            }

            if (!AlienLifeformRules.ShouldDoloreExecuteTarget(40, 100))
            {
                throw new InvalidOperationException("Detailed step 14 Dolore must execute targets at or below 40% health.");
            }

            var relation = IntruderRules.DetermineRelation(
                IntruderFaction.AlienLifeform,
                IntruderFaction.SeedEntity);
            if (relation.RelationKind != IntruderRelationKind.Hostile)
            {
                throw new InvalidOperationException("Detailed step 14 alien lifeforms must remain hostile to seed entities.");
            }

            return "Kinds=7; ExternalHP=" + AlienLifeformRules.ExternalIntrusionObjectHealth +
                   "; TurretShots=" + shotCount +
                   "; CantabileStop=" + cantabileTick.StatusEffectToApply.DurationSeconds.ToString("0.0") +
                   "; ConSpiritoDamage=" + charge.PlayerDamageApplied +
                   "; SmorzandoDamage=" + smorzandoTick.PlayerDamageApplied;
        }
    }
}
