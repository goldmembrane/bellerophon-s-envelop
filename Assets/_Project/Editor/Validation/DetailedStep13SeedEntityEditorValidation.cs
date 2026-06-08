using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep13SeedEntityEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 13 seed entity editor validation passed.");
            Debug.Log("Detailed step 13 seed entity validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var profiles = SeedIntruderRules.CreateAllSourceSeedProfiles();
            if (profiles.Length != 8 ||
                profiles[1].Kind != SeedIntruderKind.Fuga ||
                profiles[7].Kind != SeedIntruderKind.Mimesis)
            {
                throw new InvalidOperationException("Detailed step 13 must expose the confirmed eight seed entity kinds.");
            }

            var metal = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Fuga,
                7,
                ShipRoomId.Cockpit,
                "detailed-step13-fuga",
                CargoMaterial.CommonMetal);
            var metalTick = SeedIntruderRules.TickSeedIntruder(
                metal,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                SeedIntruderRules.FugaAttackDelaySeconds,
                cargoMaterial: CargoMaterial.CommonMetal);
            if (metal.Intruder.TargetType != IntruderTargetType.Cargo ||
                metalTick.CargoDamagePercentApplied <= 0f ||
                metalTick.RoomDamageApplied != 0)
            {
                throw new InvalidOperationException("Detailed step 13 metal seed entity validation must damage metal cargo without room damage.");
            }

            var tergo = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Tergo,
                11,
                ShipRoomId.Cockpit,
                "detailed-step13-tergo");
            var tergoTick = SeedIntruderRules.TickSeedIntruder(
                tergo,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                0.1f);
            if (tergoTick.PlayerPostureApplied != PlayerPostureState.KnockedDownByTergo ||
                tergoTick.SpecialEffectKind != SeedIntruderSpecialEffectKind.TergoPostureBreak)
            {
                throw new InvalidOperationException("Detailed step 13 Tergo validation must use a separate posture state.");
            }

            var urzere = SeedIntruderRules.CreateSeedIntrusionForSeed(
                SeedIntruderKind.Urzere,
                12,
                ShipRoomId.Cockpit,
                "detailed-step13-urzere");
            var urzereTick = SeedIntruderRules.TickSeedIntruder(
                urzere,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 45, 180, 1f, false),
                0.1f);
            if (urzereTick.SpecialEffectKind != SeedIntruderSpecialEffectKind.SeedEntityEmpowerment ||
                urzereTick.SeedAttackDamageBonusPercentApplied != SeedIntruderRules.UrzereSeedAttackBonusPercent ||
                urzereTick.NonSeedVisionPenaltyApplied != SeedIntruderRules.UrzereNonSeedVisionPenalty)
            {
                throw new InvalidOperationException("Detailed step 13 Urzere validation must expose seed buffs and non-seed vision penalty.");
            }

            var mimesis = SeedIntruderRules.GetProfile(SeedIntruderKind.Mimesis);
            if (!mimesis.HasMimesisVoiceMimicryPlaceholder ||
                !mimesis.StopsWhenInjectionInterrupted)
            {
                throw new InvalidOperationException("Detailed step 13 Mimesis validation must keep voice mimicry as a placeholder.");
            }

            var relation = SeedIntruderRules.DetermineSeedRelation(
                SeedIntruderKind.Mimesis,
                IntruderFaction.SpacePirate);
            if (relation.RelationKind != IntruderRelationKind.Competitive)
            {
                throw new InvalidOperationException("Detailed step 13 Mimesis relation validation must keep non-player focus competitive.");
            }

            return "Kinds=8; MetalCargo=" + metalTick.Cargo.DurabilityPercent.ToString("0.00") +
                   "; TergoPosture=" + tergoTick.PlayerPostureApplied +
                   "; UrzereBuff=" + urzereTick.SeedAttackDamageBonusPercentApplied +
                   "; MimesisVoicePlaceholder=" + mimesis.HasMimesisVoiceMimicryPlaceholder;
        }
    }
}
