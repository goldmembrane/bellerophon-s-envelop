using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class AlienLifeformRulesTests
    {
        [Test]
        public void CreateAllSourceAlienProfiles_UsesConfirmedSevenKindsAndCoreStats()
        {
            var profiles = AlienLifeformRules.CreateAllSourceAlienProfiles();

            Assert.That(profiles.Length, Is.EqualTo(7));
            Assert.That(profiles[0].Kind, Is.EqualTo(AlienLifeformKind.Cantabile));
            Assert.That(profiles[1].Kind, Is.EqualTo(AlienLifeformKind.ConSpirito));
            Assert.That(profiles[2].Kind, Is.EqualTo(AlienLifeformKind.Accelerando));
            Assert.That(profiles[3].Kind, Is.EqualTo(AlienLifeformKind.Grave));
            Assert.That(profiles[4].Kind, Is.EqualTo(AlienLifeformKind.Smorzando));
            Assert.That(profiles[5].Kind, Is.EqualTo(AlienLifeformKind.Ostinato));
            Assert.That(profiles[6].Kind, Is.EqualTo(AlienLifeformKind.Dolore));

            var cantabile = AlienLifeformRules.GetProfile(AlienLifeformKind.Cantabile);
            var conSpirito = AlienLifeformRules.GetProfile(AlienLifeformKind.ConSpirito);
            var accelerando = AlienLifeformRules.GetProfile(AlienLifeformKind.Accelerando);
            var grave = AlienLifeformRules.GetProfile(AlienLifeformKind.Grave);
            var smorzando = AlienLifeformRules.GetProfile(AlienLifeformKind.Smorzando);
            var ostinato = AlienLifeformRules.GetProfile(AlienLifeformKind.Ostinato);
            var dolore = AlienLifeformRules.GetProfile(AlienLifeformKind.Dolore);

            Assert.That(cantabile.IntruderDefinition.MaxHealth, Is.EqualTo(70));
            Assert.That(cantabile.IntruderDefinition.MobilityKind, Is.EqualTo(IntruderMobilityKind.Flying));
            Assert.That(cantabile.DirectDamage, Is.EqualTo(20));
            Assert.That(cantabile.SonicStopRadiusMeters, Is.EqualTo(3f));
            Assert.That(cantabile.SonicStopDurationSeconds, Is.EqualTo(1f));
            Assert.That(cantabile.CanResonateWithEngineRoom, Is.True);
            Assert.That(cantabile.CanBeExternallyRepelled, Is.True);
            Assert.That(cantabile.ExternalIntrusionObjectHealth, Is.EqualTo(350));

            Assert.That(conSpirito.IntruderDefinition.MaxHealth, Is.EqualTo(80));
            Assert.That(conSpirito.IntruderDefinition.MovementSpeed, Is.EqualTo(2f));
            Assert.That(conSpirito.ChargeDamage, Is.EqualTo(40));
            Assert.That(conSpirito.ChargeDistanceMeters, Is.EqualTo(10f));
            Assert.That(conSpirito.ChargeDurationSeconds, Is.EqualTo(4f));
            Assert.That(conSpirito.RestDurationSeconds, Is.EqualTo(2f));
            Assert.That(conSpirito.HasNoPriorityTarget, Is.True);

            Assert.That(accelerando.IntruderDefinition.MaxHealth, Is.EqualTo(100));
            Assert.That(accelerando.AccelerationStartSpeed, Is.EqualTo(1f));
            Assert.That(accelerando.AccelerationMaxSpeed, Is.EqualTo(5f));
            Assert.That(accelerando.AccelerationStartAttackDelaySeconds, Is.EqualTo(1.5f));
            Assert.That(accelerando.AccelerationMinimumAttackDelaySeconds, Is.EqualTo(0.5f));
            Assert.That(accelerando.AccelerationAttackMovementSpeed, Is.EqualTo(2.5f));
            Assert.That(accelerando.AccelerationResetSightLossSeconds, Is.EqualTo(5f));

            Assert.That(grave.IntruderDefinition.MaxHealth, Is.EqualTo(300));
            Assert.That(grave.DirectDamage, Is.EqualTo(35));
            Assert.That(grave.AttackWindupSeconds, Is.EqualTo(3f));
            Assert.That(grave.StatusDamageMultiplier, Is.EqualTo(1.5f));
            Assert.That(grave.StatusDurationMultiplier, Is.EqualTo(2f));

            Assert.That(smorzando.IntruderDefinition.MaxHealth, Is.EqualTo(160));
            Assert.That(smorzando.LiquidDamagePerSecond, Is.EqualTo(5));
            Assert.That(smorzando.LiquidDamageReductionPercent, Is.EqualTo(90));
            Assert.That(smorzando.AbsorptionStopStackThreshold, Is.EqualTo(10));
            Assert.That(smorzando.ZombieHealth, Is.EqualTo(1));
            Assert.That(smorzando.ZombieSelfDestructDamage, Is.EqualTo(140));

            Assert.That(ostinato.IntruderDefinition.MaxHealth, Is.EqualTo(110));
            Assert.That(ostinato.FrenzyHealthThresholdPercent, Is.EqualTo(0.5f));
            Assert.That(ostinato.RoarDurationSeconds, Is.EqualTo(2f));
            Assert.That(ostinato.FrenzyMovementSpeed, Is.EqualTo(3.8f));
            Assert.That(ostinato.FrenzyDamage, Is.EqualTo(25));
            Assert.That(ostinato.RecoveryHealPercent, Is.EqualTo(0.4f));

            Assert.That(dolore.IntruderDefinition.MaxHealth, Is.EqualTo(100));
            Assert.That(dolore.DirectDamage, Is.EqualTo(20));
            Assert.That(dolore.ExecutionHealthThresholdPercent, Is.EqualTo(0.4f));
        }

        [Test]
        public void CreateAlienLifeformIntrusionFromHazard_UsesAlienFactionAndBoardedState()
        {
            var hazard = TransportHazardState.Start(TransportHazardType.AlienLifeRegion, 991, 30);

            var state = AlienLifeformRules.CreateAlienLifeformIntrusionFromHazard(hazard, 1, ShipRoomId.EngineRoom);

            Assert.That(state.Kind, Is.Not.EqualTo(AlienLifeformKind.None));
            Assert.That(state.Definition.Faction, Is.EqualTo(IntruderFaction.AlienLifeform));
            Assert.That(state.Attempt.Phase, Is.EqualTo(IntrusionPhase.Boarded));
            Assert.That(state.Intruder.IsActive, Is.True);
            Assert.That(state.Intruder.Faction, Is.EqualTo(IntruderFaction.AlienLifeform));
        }

        [Test]
        public void TickCantabile_AppliesSonicStopAndEngineRoomResonance()
        {
            var state = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.Cantabile,
                17,
                ShipRoomId.EngineRoom,
                "alien-cantabile-test");
            var ship = ShipState.CreateDefault();

            var result = AlienLifeformRules.TickAlienLifeform(
                state,
                ship,
                AlienLifeformRules.CantabileAttackDelaySeconds,
                ShipRoomId.EngineRoom);

            Assert.That(result.AttackCount, Is.EqualTo(1));
            Assert.That(result.PlayerDamageApplied, Is.EqualTo(AlienLifeformRules.CantabileDamage));
            Assert.That(result.StatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Stopped));
            Assert.That(result.StatusEffectToApply.DurationSeconds, Is.EqualTo(1f));
            Assert.That(result.EngineRoomResonanceApplied, Is.True);
            Assert.That(result.RoomDamageApplied, Is.EqualTo(AlienLifeformRules.CantabileDamage));
            Assert.That(result.Ship.GetRoom(ShipRoomId.EngineRoom).CurrentDurability, Is.EqualTo(80));
        }

        [Test]
        public void ConSpirito_ChargesNonAlienFactionAndRestWhenStopped()
        {
            var state = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.ConSpirito,
                12,
                ShipRoomId.Cockpit,
                "alien-con-spirito-test");

            var charge = AlienLifeformRules.TickAlienLifeform(
                state,
                ShipState.CreateDefault(),
                0.1f,
                encounteredFaction: IntruderFaction.SpacePirate);
            var stopped = AlienLifeformRules.ApplyStatusEffect(
                charge.State,
                CombatStatusEffectRules.CreateStopped(1f));
            var interrupted = AlienLifeformRules.TickAlienLifeform(
                stopped,
                ShipState.CreateDefault(),
                0.1f);

            Assert.That(charge.State.Phase, Is.EqualTo(AlienLifeformBehaviorPhase.Charging));
            Assert.That(charge.AttackCount, Is.EqualTo(1));
            Assert.That(charge.PlayerDamageApplied, Is.EqualTo(AlienLifeformRules.ConSpiritoChargeDamage));
            Assert.That(charge.SpecialEffectKind, Is.EqualTo(AlienLifeformSpecialEffectKind.Charge));
            Assert.That(interrupted.State.Phase, Is.EqualTo(AlienLifeformBehaviorPhase.Resting));
            Assert.That(interrupted.PlayerDamageApplied, Is.Zero);
        }

        [Test]
        public void AccelerandoAndGrave_ExposeSourceSpecialMath()
        {
            var burn = CombatStatusEffectRules.CreateBurn(5f, 10);
            var amplified = AlienLifeformRules.AmplifyStatusEffectForGrave(burn);

            Assert.That(AlienLifeformRules.CalculateAccelerandoMovementSpeed(0f), Is.EqualTo(1f));
            Assert.That(AlienLifeformRules.CalculateAccelerandoMovementSpeed(10f), Is.EqualTo(5f));
            Assert.That(AlienLifeformRules.CalculateAccelerandoAttackDelay(0f), Is.EqualTo(1.5f));
            Assert.That(AlienLifeformRules.CalculateAccelerandoAttackDelay(10f), Is.EqualTo(0.5f));
            Assert.That(amplified.DurationSeconds, Is.EqualTo(10f));
            Assert.That(amplified.TickDamage, Is.EqualTo(15));
            Assert.That(AlienLifeformRules.CalculateStatusDamageToGrave(10), Is.EqualTo(15));
        }

        [Test]
        public void SmorzandoOstinatoAndDolore_ApplyRepresentativeSpecialBehaviors()
        {
            var smorzando = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.Smorzando,
                21,
                ShipRoomId.Cockpit,
                "alien-smorzando-test");
            var smorzandoTick = AlienLifeformRules.TickAlienLifeform(
                smorzando,
                ShipState.CreateDefault(),
                10f);
            var zombie = AlienLifeformRules.TransformSmorzandoToZombie(smorzandoTick.State);

            Assert.That(smorzandoTick.PlayerDamageApplied, Is.EqualTo(60));
            Assert.That(smorzandoTick.StatusEffectToApply.Kind, Is.EqualTo(CombatStatusEffectKind.Stopped));
            Assert.That(smorzandoTick.MovementSlowPercentApplied, Is.EqualTo(100));
            Assert.That(smorzandoTick.State.AbsorptionStack, Is.Zero);
            Assert.That(zombie.Phase, Is.EqualTo(AlienLifeformBehaviorPhase.Zombie));
            Assert.That(zombie.Intruder.CurrentHealth, Is.EqualTo(AlienLifeformRules.SmorzandoZombieHealth));

            var ostinato = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.Ostinato,
                31,
                ShipRoomId.Cockpit,
                "alien-ostinato-test");
            ostinato = AlienLifeformRules.ApplyDamage(ostinato, 60);
            var roaring = AlienLifeformRules.TickAlienLifeform(ostinato, ShipState.CreateDefault(), 0.1f);
            var frenzy = AlienLifeformRules.TickAlienLifeform(roaring.State, ShipState.CreateDefault(), 2f);

            Assert.That(roaring.State.Phase, Is.EqualTo(AlienLifeformBehaviorPhase.Roaring));
            Assert.That(frenzy.State.Phase, Is.EqualTo(AlienLifeformBehaviorPhase.Frenzy));
            Assert.That(frenzy.FrenzyActive, Is.True);
            Assert.That(frenzy.PlayerDamageApplied, Is.EqualTo(AlienLifeformRules.OstinatoFrenzyDamage));

            var dolore = AlienLifeformRules.CreateAlienLifeformIntrusionForSeed(
                AlienLifeformKind.Dolore,
                41,
                ShipRoomId.Cockpit,
                "alien-dolore-test");
            var execution = AlienLifeformRules.TickAlienLifeform(
                dolore,
                ShipState.CreateDefault(),
                AlienLifeformRules.DoloreAttackDelaySeconds,
                targetCurrentHealth: 40,
                targetMaxHealth: 100);

            Assert.That(AlienLifeformRules.ShouldDoloreExecuteTarget(40, 100), Is.True);
            Assert.That(execution.ExecutedTarget, Is.True);
            Assert.That(execution.SpecialEffectKind, Is.EqualTo(AlienLifeformSpecialEffectKind.ExecutionPull));
            Assert.That(execution.PlayerDamageApplied, Is.EqualTo(40));
        }
    }
}
