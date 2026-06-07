using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ManualTurretStateTests
    {
        [Test]
        public void ManualTurret_StartsWithMagazineAndClampedAim()
        {
            var turret = ManualTurretState.Start(true)
                .SetAim(2f, -2f);

            Assert.That(ManualTurretState.HeldFireIntervalSeconds, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(turret.IsActive, Is.True);
            Assert.That(turret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize));
            Assert.That(turret.AimX, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(turret.AimY, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(turret.IntruderHitPossible, Is.True);
        }

        [Test]
        public void ManualTurret_HitTargetConsumesAmmoAndAppliesDamage()
        {
            var target = CreateTarget();
            var turret = ManualTurretState.Start(true)
                .SetAim(target.PositionX, target.PositionY);

            var result = turret.FireAt(target);

            Assert.That(result.Outcome, Is.EqualTo(ManualTurretFireOutcome.Hit));
            Assert.That(result.Turret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize - 1));
            Assert.That(result.Target.CurrentHealth, Is.EqualTo(target.CurrentHealth - ManualTurretState.ShotDamage));
        }

        [Test]
        public void ManualTurret_RepeatedHitsDestroyDefaultAsteroidTarget()
        {
            var target = CreateTarget();
            var turret = ManualTurretState.Start(true)
                .SetAim(target.PositionX, target.PositionY);

            ManualTurretFireResult shot = default;
            for (var i = 0; i < 20 && !target.IsDestroyed; i++)
            {
                shot = turret.FireAt(target);
                turret = shot.Turret;
                target = shot.Target;
            }

            Assert.That(shot.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
            Assert.That(shot.Target.IsDestroyed, Is.True);
            Assert.That(shot.Turret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize - 8));
        }

        [Test]
        public void ManualTurret_ReloadRestoresMagazineAfterTwoSeconds()
        {
            var target = CreateTarget();
            var fired = ManualTurretState.Start(true)
                .SetAim(target.PositionX, target.PositionY)
                .FireAt(target)
                .Turret;

            var reloading = fired.BeginReload();
            var stillReloading = reloading.Tick(1f);
            var completed = stillReloading.Tick(1f);

            Assert.That(reloading.IsReloading, Is.True);
            Assert.That(stillReloading.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize - 1));
            Assert.That(completed.IsReloading, Is.False);
            Assert.That(completed.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize));
        }

        [Test]
        public void ManualTurret_UpgradedMagazineAndPlasmaUseSourceTiming()
        {
            var target = new ExternalTargetState(
                "asteroid-large-test",
                ExternalTargetType.Asteroid,
                1000,
                1000,
                0.2f,
                -0.1f,
                ManualTurretState.DefaultAsteroidHitRadius);
            var turret = ManualTurretState.Start(true, 75, true)
                .SetAim(target.PositionX, target.PositionY);

            var plasma = turret.FirePlasmaCannon(target);
            var ticked = plasma.Turret.TickWithPlasma(0.3f, target);
            var coolingInAutoMode = ticked.Turret.Stop().Tick(10f);

            Assert.That(turret.MagazineCapacity, Is.EqualTo(75));
            Assert.That(turret.AmmoInMagazine, Is.EqualTo(75));
            Assert.That(plasma.Outcome, Is.EqualTo(ManualTurretPlasmaOutcome.Activated));
            Assert.That(plasma.Turret.PlasmaActiveRemainingSeconds, Is.EqualTo(ManualTurretState.PlasmaCannonDurationSeconds).Within(0.0001f));
            Assert.That(plasma.Turret.PlasmaCooldownRemainingSeconds, Is.EqualTo(ManualTurretState.PlasmaCannonCooldownSeconds).Within(0.0001f));
            Assert.That(ticked.DamageApplied, Is.EqualTo(150));
            Assert.That(ticked.Target.CurrentHealth, Is.EqualTo(850));
            Assert.That(coolingInAutoMode.PlasmaCooldownRemainingSeconds, Is.LessThan(ticked.Turret.PlasmaCooldownRemainingSeconds));
        }

        [Test]
        public void ManualTurret_PlasmaRequiresInstalledUpgrade()
        {
            var target = CreateTarget();
            var turret = ManualTurretState.Start(true)
                .SetAim(target.PositionX, target.PositionY);

            var plasma = turret.FirePlasmaCannon(target);

            Assert.That(plasma.Outcome, Is.EqualTo(ManualTurretPlasmaOutcome.Unavailable));
            Assert.That(plasma.Turret.PlasmaActiveRemainingSeconds, Is.Zero);
        }

        private static ExternalTargetState CreateTarget()
        {
            return new ExternalTargetState(
                "asteroid-test",
                ExternalTargetType.Asteroid,
                ManualTurretState.DefaultAsteroidTargetHealth,
                ManualTurretState.DefaultAsteroidTargetHealth,
                0.2f,
                -0.1f,
                ManualTurretState.DefaultAsteroidHitRadius);
        }
    }
}
