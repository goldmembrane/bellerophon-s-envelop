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
        public void ManualTurret_ThreeHitsDestroyDefaultAsteroidTarget()
        {
            var target = CreateTarget();
            var turret = ManualTurretState.Start(true)
                .SetAim(target.PositionX, target.PositionY);

            var first = turret.FireAt(target);
            var second = first.Turret.FireAt(first.Target);
            var third = second.Turret.FireAt(second.Target);

            Assert.That(third.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
            Assert.That(third.Target.IsDestroyed, Is.True);
            Assert.That(third.Turret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize - 3));
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
