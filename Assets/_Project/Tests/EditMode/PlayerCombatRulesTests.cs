using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class PlayerCombatRulesTests
    {
        [Test]
        public void StatusEffects_UseOnlySourceNamedSevenKinds()
        {
            var effects = new[]
            {
                CombatStatusEffectRules.CreateStopped(1f),
                CombatStatusEffectRules.CreateBurn(5f, 10),
                CombatStatusEffectRules.CreateBleeding(10f, 10),
                CombatStatusEffectRules.CreateExhaustion(3f),
                CombatStatusEffectRules.CreateFatigue(60f),
                CombatStatusEffectRules.CreateDizziness(60f),
                CombatStatusEffectRules.CreateConfusion(5f)
            };

            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[0].Kind), Is.EqualTo("정지"));
            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[1].Kind), Is.EqualTo("화상"));
            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[2].Kind), Is.EqualTo("출혈"));
            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[3].Kind), Is.EqualTo("탈진"));
            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[4].Kind), Is.EqualTo("피로"));
            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[5].Kind), Is.EqualTo("현기증"));
            Assert.That(CombatStatusEffectRules.FormatEffectName(effects[6].Kind), Is.EqualTo("혼란"));
        }

        [Test]
        public void StatusEffects_TickBurnBleedingAndExposeMovementActionRules()
        {
            var effects = CombatStatusEffectRules.ApplyEffect(
                null,
                CombatStatusEffectRules.CreateBurn(
                    CombatStatusEffectRules.BurnDefaultDurationSeconds,
                    CombatStatusEffectRules.BurnDefaultTickDamage));
            effects = CombatStatusEffectRules.ApplyEffect(
                effects,
                CombatStatusEffectRules.CreateBleeding(
                    CombatStatusEffectRules.BleedingDefaultDurationSeconds,
                    CombatStatusEffectRules.BleedingDefaultTickDamage));
            effects = CombatStatusEffectRules.ApplyEffect(effects, CombatStatusEffectRules.CreateExhaustion(3f));

            var ticked = CombatStatusEffectRules.TickEffects(effects, 2f);

            Assert.That(ticked.HealthDamage, Is.EqualTo(40));
            Assert.That(CombatStatusEffectRules.CalculateMovementMultiplier(ticked.Effects), Is.EqualTo(0.2f));
            Assert.That(CombatStatusEffectRules.BlocksSprint(ticked.Effects), Is.True);
            Assert.That(CombatStatusEffectRules.BlocksActions(
                CombatStatusEffectRules.ApplyEffect(ticked.Effects, CombatStatusEffectRules.CreateConfusion(5f))), Is.True);
        }

        [Test]
        public void PlayerDamage_AppliesShieldFirstAndProtectiveStatusRules()
        {
            var state = new PlayerCombatState(100, 100, 100, 50, null);
            var fireproof = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.FireproofSuit, 700));
            fireproof = EquipmentRules.UseSupplyItem(fireproof, 0).State;
            var fire = new PlayerDamageProfile(
                50,
                CombatDamageSourceKind.Fire,
                CombatStatusEffectRules.CreateBurn(5f, 10));

            var result = PlayerCombatRules.ApplyIncomingDamage(state, fireproof, fire);

            Assert.That(result.ShieldDamage, Is.EqualTo(35));
            Assert.That(result.HealthDamage, Is.Zero);
            Assert.That(result.StatusPrevented, Is.True);
            Assert.That(CombatStatusEffectRules.HasEffect(result.State.StatusEffects, CombatStatusEffectKind.Burn), Is.False);

            var insulated = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.InsulatedSuit, 700));
            insulated = EquipmentRules.UseSupplyItem(insulated, 0).State;
            var electric = new PlayerDamageProfile(
                10,
                CombatDamageSourceKind.Electric,
                CombatStatusEffectRules.CreateStopped(1f));
            var electricResult = PlayerCombatRules.ApplyIncomingDamage(state, insulated, electric);

            Assert.That(electricResult.StatusPrevented, Is.True);
            Assert.That(CombatStatusEffectRules.HasEffect(electricResult.State.StatusEffects, CombatStatusEffectKind.Stopped), Is.False);
        }

        [Test]
        public void PlayerPostureState_IsSeparateFromSourceNamedStatusEffects()
        {
            var state = new PlayerCombatState(100, 100, 100, 100, null);
            var knockedDown = PlayerCombatRules.ApplyPostureState(
                state,
                PlayerPostureState.KnockedDownByTergo);
            var cleared = PlayerCombatRules.ClearPostureState(knockedDown);

            Assert.That(knockedDown.PostureState, Is.EqualTo(PlayerPostureState.KnockedDownByTergo));
            Assert.That(knockedDown.IsPostureRestrained, Is.True);
            Assert.That(knockedDown.StatusEffects.Length, Is.Zero);
            Assert.That(cleared.PostureState, Is.EqualTo(PlayerPostureState.Standing));
        }

        [Test]
        public void PhysicalProtectiveSuit_HalvesStoppedAndBurnApplicationChance()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.PhysicalProtectiveSuit, 1250));
            equipment = EquipmentRules.UseSupplyItem(equipment, 0).State;
            var profile = new PlayerDamageProfile(
                20,
                CombatDamageSourceKind.Electric,
                CombatStatusEffectRules.CreateStopped(1f));

            var applied = PlayerCombatRules.ApplyIncomingDamage(
                new PlayerCombatState(100, 100, 100, 100, null),
                equipment,
                profile,
                49);
            var prevented = PlayerCombatRules.ApplyIncomingDamage(
                new PlayerCombatState(100, 100, 100, 100, null),
                equipment,
                profile,
                50);

            Assert.That(applied.StatusApplied, Is.True);
            Assert.That(prevented.StatusPrevented, Is.True);
        }

        [Test]
        public void EquipmentStatusEffects_FollowElectricBatonFlamethrowerFlashbangAndBandageRules()
        {
            var electric = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithHandSlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.ElectricBaton, EquipmentRules.ElectricBatonPriceCredits))
                .WithActiveHandSlot(1);
            var electricHit = EquipmentRules.UseActiveEquipment(electric, false, true);
            var electricCoolingHit = EquipmentRules.Tick(electricHit.State, EquipmentRules.ElectricBatonUseDelaySeconds);
            electricCoolingHit = EquipmentRules.UseActiveEquipment(electricCoolingHit, false, true).State;

            Assert.That(electricHit.Damage, Is.EqualTo(EquipmentRules.ElectricBatonDamage + CombatStatusEffectRules.ElectricBatonChargedDamageBonus));
            Assert.That(electricHit.StatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Stopped));
            Assert.That(electricHit.State.ElectricBatonChargeCooldownSeconds, Is.EqualTo(CombatStatusEffectRules.ElectricBatonChargeCooldownSeconds));
            Assert.That(electricCoolingHit.ElectricBatonChargeCooldownSeconds, Is.GreaterThan(0f));

            var flame = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithHandSlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.MiniFlamethrower, EquipmentRules.MiniFlamethrowerPriceCredits))
                .WithActiveHandSlot(1);
            var firstFlame = EquipmentRules.UseActiveEquipment(flame, false, true);
            var secondFlameReady = EquipmentRules.Tick(firstFlame.State, EquipmentRules.MiniFlamethrowerUseDelaySeconds);
            var secondFlame = EquipmentRules.UseActiveEquipment(secondFlameReady, false, true);

            Assert.That(firstFlame.StatusEffectToApply.HasEffect, Is.False);
            Assert.That(secondFlame.StatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Burn));

            var flashbang = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithHandSlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.Flashbang, 100))
                .WithActiveHandSlot(1);
            var flash = EquipmentRules.UseActiveEquipment(flashbang, false, true);

            Assert.That(flash.StatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Confusion));
            Assert.That(flash.State.GetHandSlot(1).IsEmpty, Is.True);

            var bandage = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.BandageSet, 75));
            var bandageUse = EquipmentRules.UseSupplyItem(bandage, 0);

            Assert.That(bandageUse.HealthDelta, Is.EqualTo(EquipmentRules.BandageSetHealAmount));
            Assert.That(bandageUse.StatusEffectToClear, Is.EqualTo(CombatStatusEffectKind.Bleeding));
        }

        [Test]
        public void Enhancers_ScheduleOriginalSideEffectStatuses()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.MoveSpeedEnhancer, 65))
                .WithSupplySlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.StrengthEnhancer, 100))
                .WithSupplySlot(2, EquipmentSlotState.Purchased(EquipmentItemKind.FocusEnhancer, 180));

            var move = EquipmentRules.UseSupplyItem(equipment, 0);
            var strength = EquipmentRules.UseSupplyItem(move.State, 1);
            var focus = EquipmentRules.UseSupplyItem(strength.State, 2);

            Assert.That(move.DelayedStatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Exhaustion));
            Assert.That(move.DelayedStatusEffectDelaySeconds, Is.EqualTo(EquipmentRules.MoveSpeedEnhancerDurationSeconds));
            Assert.That(strength.DelayedStatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Fatigue));
            Assert.That(strength.DelayedStatusEffectDelaySeconds, Is.EqualTo(EquipmentRules.StrengthEnhancerDurationSeconds));
            Assert.That(focus.DelayedStatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Dizziness));
            Assert.That(focus.DelayedStatusEffectDelaySeconds, Is.EqualTo(EquipmentRules.FocusEnhancerDurationSeconds));
        }

        [Test]
        public void ShipDeviceState_AppliesEquipmentStatusesToParvumAndPlayer()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var statusObject = new GameObject("Player Status Test");
            var settings = ScriptableObject.CreateInstance<FirstPersonPlayerSettings>();
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var status = statusObject.AddComponent<FirstPersonPlayerStatus>();
                status.Configure(settings);
                status.ApplyPostureState(PlayerPostureState.KnockedDownByTergo);

                Assert.That(status.PostureState, Is.EqualTo(PlayerPostureState.KnockedDownByTergo));
                Assert.That(status.IsMovementBlocked, Is.True);
                Assert.That(status.IsActionBlocked, Is.True);
                Assert.That(status.MovementMultiplier, Is.Zero);

                status.ClearPostureState();
                Assert.That(status.PostureState, Is.EqualTo(PlayerPostureState.Standing));

                state.SetPlayerStatusForValidation(status);
                state.SetEquipmentStateForValidation(PlayerEquipmentState.CreateDefaultAssociationIssue()
                    .WithHandSlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.Flashbang, 100))
                    .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.MoveSpeedEnhancer, 65))
                    .WithActiveHandSlot(1));
                state.StartTransportRun(60);
                state.StartSeedIntruderForValidation(
                    SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit));

                var flash = state.UseActiveEquipment(false);
                var beforeRoomDamage = state.CurrentSeedIntruder.TotalRoomDamageApplied;
                state.TickTransportRun(SeedIntruderRules.ParvumAttackDelaySeconds);
                var move = state.UseSupplyItem(0);
                status.TickStatusEffects(move.DelayedStatusEffectDelaySeconds);

                Assert.That(flash.StatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Confusion));
                Assert.That(state.CurrentSeedIntruder.Intruder.HasStatusEffect(CombatStatusEffectKind.Confusion), Is.True);
                Assert.That(state.CurrentSeedIntruder.TotalRoomDamageApplied, Is.EqualTo(beforeRoomDamage));
                Assert.That(status.HasStatusEffect(CombatStatusEffectKind.Exhaustion), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(statusObject);
                Object.DestroyImmediate(stateObject);
            }
        }
    }
}
