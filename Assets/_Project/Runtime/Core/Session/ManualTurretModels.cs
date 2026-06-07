using System;

namespace Bellerophon.Core.Session
{
    public enum ExternalTargetType
    {
        None,
        Asteroid
    }

    public enum ManualTurretFireOutcome
    {
        None,
        Miss,
        Hit,
        Destroyed,
        EmptyMagazine,
        Reloading,
        Inactive
    }

    public enum ManualTurretPlasmaOutcome
    {
        None,
        Activated,
        Unavailable,
        Cooldown,
        AlreadyActive,
        Inactive
    }

    public readonly struct ExternalTargetState
    {
        public ExternalTargetState(
            string targetId,
            ExternalTargetType targetType,
            int currentHealth,
            int maxHealth,
            float positionX,
            float positionY,
            float hitRadius)
        {
            if (targetType == ExternalTargetType.None)
            {
                TargetId = string.Empty;
                TargetType = ExternalTargetType.None;
                CurrentHealth = 0;
                MaxHealth = 0;
                PositionX = 0f;
                PositionY = 0f;
                HitRadius = 0f;
                return;
            }

            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw new ArgumentException("External target id is required.", nameof(targetId));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "External target max health must be positive.");
            }

            if (hitRadius <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hitRadius), "External target hit radius must be positive.");
            }

            TargetId = targetId;
            TargetType = targetType;
            CurrentHealth = Clamp(currentHealth, 0, maxHealth);
            MaxHealth = maxHealth;
            PositionX = Clamp(positionX, -1f, 1f);
            PositionY = Clamp(positionY, -1f, 1f);
            HitRadius = Clamp(hitRadius, 0.01f, 1f);
        }

        public string TargetId { get; }

        public ExternalTargetType TargetType { get; }

        public int CurrentHealth { get; }

        public int MaxHealth { get; }

        public float PositionX { get; }

        public float PositionY { get; }

        public float HitRadius { get; }

        public bool IsActive => TargetType != ExternalTargetType.None && CurrentHealth > 0;

        public bool IsDestroyed => TargetType != ExternalTargetType.None && CurrentHealth <= 0;

        public static ExternalTargetState None => new ExternalTargetState(
            string.Empty,
            ExternalTargetType.None,
            0,
            0,
            0f,
            0f,
            0f);

        public ExternalTargetState WithDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "External target damage cannot be negative.");
            }

            if (!IsActive || damage == 0)
            {
                return this;
            }

            return new ExternalTargetState(
                TargetId,
                TargetType,
                CurrentHealth - damage,
                MaxHealth,
                PositionX,
                PositionY,
                HitRadius);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public readonly struct ManualTurretState
    {
        public const int MagazineSize = 50;
        public const int ShotDamage = 50;
        public const int DefaultAsteroidTargetHealth = 400;
        public const float ReloadDurationSeconds = 2f;
        public const float HeldFireIntervalSeconds = 0.25f;
        public const float AimInputSensitivity = 0.005f;
        public const float DefaultAsteroidHitRadius = 0.2f;
        public const float PlasmaCannonDurationSeconds = 3f;
        public const float PlasmaCannonTickIntervalSeconds = 0.1f;
        public const int PlasmaCannonTickDamage = 50;
        public const float PlasmaCannonCooldownSeconds = 60f;

        private ManualTurretState(
            bool isActive,
            int magazineCapacity,
            int ammoInMagazine,
            bool isReloading,
            float reloadRemainingSeconds,
            float aimX,
            float aimY,
            bool intruderHitPossible,
            bool plasmaCannonInstalled,
            float plasmaCooldownRemainingSeconds,
            float plasmaActiveRemainingSeconds,
            float plasmaTickAccumulatorSeconds)
        {
            IsActive = isActive;
            MagazineCapacity = Math.Max(1, magazineCapacity);
            AmmoInMagazine = Clamp(ammoInMagazine, 0, MagazineCapacity);
            IsReloading = isReloading;
            ReloadRemainingSeconds = Clamp(reloadRemainingSeconds, 0f, ReloadDurationSeconds);
            AimX = Clamp(aimX, -1f, 1f);
            AimY = Clamp(aimY, -1f, 1f);
            IntruderHitPossible = intruderHitPossible;
            PlasmaCannonInstalled = plasmaCannonInstalled;
            PlasmaCooldownRemainingSeconds = Clamp(
                plasmaCooldownRemainingSeconds,
                0f,
                PlasmaCannonCooldownSeconds);
            PlasmaActiveRemainingSeconds = Clamp(
                plasmaActiveRemainingSeconds,
                0f,
                PlasmaCannonDurationSeconds);
            PlasmaTickAccumulatorSeconds = Math.Max(0f, plasmaTickAccumulatorSeconds);
        }

        public bool IsActive { get; }

        public int MagazineCapacity { get; }

        public int AmmoInMagazine { get; }

        public bool IsReloading { get; }

        public float ReloadRemainingSeconds { get; }

        public float AimX { get; }

        public float AimY { get; }

        public bool IntruderHitPossible { get; }

        public bool PlasmaCannonInstalled { get; }

        public float PlasmaCooldownRemainingSeconds { get; }

        public float PlasmaActiveRemainingSeconds { get; }

        public float PlasmaTickAccumulatorSeconds { get; }

        public bool IsPlasmaActive => PlasmaActiveRemainingSeconds > 0.0001f;

        public bool IsPlasmaCoolingDown => PlasmaCooldownRemainingSeconds > 0.0001f;

        public static ManualTurretState Inactive => new ManualTurretState(
            false,
            MagazineSize,
            0,
            false,
            0f,
            0f,
            0f,
            false,
            false,
            0f,
            0f,
            0f);

        public static ManualTurretState Start(bool intruderHitPossible)
        {
            return Start(intruderHitPossible, MagazineSize, false);
        }

        public static ManualTurretState Start(
            bool intruderHitPossible,
            int magazineCapacity,
            bool plasmaCannonInstalled)
        {
            return new ManualTurretState(
                true,
                magazineCapacity,
                magazineCapacity,
                false,
                0f,
                0f,
                0f,
                intruderHitPossible,
                plasmaCannonInstalled,
                0f,
                0f,
                0f);
        }

        public ManualTurretState Stop()
        {
            return new ManualTurretState(
                false,
                MagazineCapacity,
                AmmoInMagazine,
                false,
                0f,
                AimX,
                AimY,
                false,
                PlasmaCannonInstalled,
                PlasmaCooldownRemainingSeconds,
                PlasmaActiveRemainingSeconds,
                PlasmaTickAccumulatorSeconds);
        }

        public ManualTurretState Tick(float deltaSeconds)
        {
            return TickWithPlasma(deltaSeconds, ExternalTargetState.None).Turret;
        }

        public ManualTurretPlasmaTickResult TickWithPlasma(float deltaSeconds, ExternalTargetState target)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (deltaSeconds <= 0f)
            {
                return new ManualTurretPlasmaTickResult(this, target, 0);
            }

            var nextCooldown = Math.Max(0f, PlasmaCooldownRemainingSeconds - deltaSeconds);
            var nextActive = Math.Max(0f, PlasmaActiveRemainingSeconds - deltaSeconds);
            var nextAccumulator = PlasmaTickAccumulatorSeconds;
            var damageApplied = 0;
            var nextTarget = target;
            if (PlasmaCannonInstalled && PlasmaActiveRemainingSeconds > 0.0001f)
            {
                var activeSeconds = Math.Min(deltaSeconds, PlasmaActiveRemainingSeconds);
                nextAccumulator += activeSeconds;
                var tickCount = 0;
                while (nextAccumulator + 0.0001f >= PlasmaCannonTickIntervalSeconds)
                {
                    nextAccumulator -= PlasmaCannonTickIntervalSeconds;
                    tickCount++;
                }

                if (nextActive <= 0.0001f)
                {
                    nextAccumulator = 0f;
                }

                if (tickCount > 0 && target.IsActive && IsAimOnTarget(target))
                {
                    var rawDamage = tickCount * PlasmaCannonTickDamage;
                    nextTarget = target.WithDamage(rawDamage);
                    damageApplied = target.CurrentHealth - nextTarget.CurrentHealth;
                }
            }

            var nextAmmo = AmmoInMagazine;
            var nextReloading = IsReloading;
            var nextReloadRemaining = ReloadRemainingSeconds;
            if (IsActive && IsReloading)
            {
                nextReloadRemaining = ReloadRemainingSeconds - deltaSeconds;
                if (nextReloadRemaining <= 0f)
                {
                    nextAmmo = MagazineCapacity;
                    nextReloading = false;
                    nextReloadRemaining = 0f;
                }
            }

            var nextTurret = new ManualTurretState(
                IsActive,
                MagazineCapacity,
                nextAmmo,
                nextReloading,
                nextReloadRemaining,
                AimX,
                AimY,
                IntruderHitPossible,
                PlasmaCannonInstalled,
                nextCooldown,
                nextActive,
                nextAccumulator);
            return new ManualTurretPlasmaTickResult(nextTurret, nextTarget, damageApplied);
        }

        public ManualTurretState WithMagazineCapacity(
            int magazineCapacity,
            bool plasmaCannonInstalled)
        {
            return new ManualTurretState(
                IsActive,
                magazineCapacity,
                AmmoInMagazine,
                IsReloading,
                ReloadRemainingSeconds,
                AimX,
                AimY,
                IntruderHitPossible,
                plasmaCannonInstalled,
                PlasmaCooldownRemainingSeconds,
                PlasmaActiveRemainingSeconds,
                PlasmaTickAccumulatorSeconds);
        }

        public ManualTurretPlasmaResult FirePlasmaCannon(ExternalTargetState target)
        {
            if (!IsActive)
            {
                return new ManualTurretPlasmaResult(
                    ManualTurretPlasmaOutcome.Inactive,
                    this,
                    target,
                    0);
            }

            if (!PlasmaCannonInstalled)
            {
                return new ManualTurretPlasmaResult(
                    ManualTurretPlasmaOutcome.Unavailable,
                    this,
                    target,
                    0);
            }

            if (PlasmaActiveRemainingSeconds > 0.0001f)
            {
                return new ManualTurretPlasmaResult(
                    ManualTurretPlasmaOutcome.AlreadyActive,
                    this,
                    target,
                    0);
            }

            if (PlasmaCooldownRemainingSeconds > 0.0001f)
            {
                return new ManualTurretPlasmaResult(
                    ManualTurretPlasmaOutcome.Cooldown,
                    this,
                    target,
                    0);
            }

            var nextTurret = new ManualTurretState(
                true,
                MagazineCapacity,
                AmmoInMagazine,
                IsReloading,
                ReloadRemainingSeconds,
                AimX,
                AimY,
                IntruderHitPossible,
                PlasmaCannonInstalled,
                PlasmaCannonCooldownSeconds,
                PlasmaCannonDurationSeconds,
                0f);
            return new ManualTurretPlasmaResult(
                ManualTurretPlasmaOutcome.Activated,
                nextTurret,
                target,
                0);
        }

        public ManualTurretState BeginReload()
        {
            if (!IsActive || IsReloading || AmmoInMagazine >= MagazineCapacity)
            {
                return this;
            }

            return new ManualTurretState(
                true,
                MagazineCapacity,
                AmmoInMagazine,
                true,
                ReloadDurationSeconds,
                AimX,
                AimY,
                IntruderHitPossible,
                PlasmaCannonInstalled,
                PlasmaCooldownRemainingSeconds,
                PlasmaActiveRemainingSeconds,
                PlasmaTickAccumulatorSeconds);
        }

        public ManualTurretState ApplyAimInput(float horizontalDelta, float verticalDelta)
        {
            if (!IsActive)
            {
                return this;
            }

            return SetAim(
                AimX + horizontalDelta * AimInputSensitivity,
                AimY + verticalDelta * AimInputSensitivity);
        }

        public ManualTurretState SetAim(float aimX, float aimY)
        {
            return new ManualTurretState(
                IsActive,
                MagazineCapacity,
                AmmoInMagazine,
                IsReloading,
                ReloadRemainingSeconds,
                aimX,
                aimY,
                IntruderHitPossible,
                PlasmaCannonInstalled,
                PlasmaCooldownRemainingSeconds,
                PlasmaActiveRemainingSeconds,
                PlasmaTickAccumulatorSeconds);
        }

        public ManualTurretFireResult FireAt(ExternalTargetState target)
        {
            if (!IsActive)
            {
                return new ManualTurretFireResult(
                    ManualTurretFireOutcome.Inactive,
                    this,
                    target,
                    0);
            }

            if (IsReloading)
            {
                return new ManualTurretFireResult(
                    ManualTurretFireOutcome.Reloading,
                    this,
                    target,
                    0);
            }

            if (AmmoInMagazine <= 0)
            {
                return new ManualTurretFireResult(
                    ManualTurretFireOutcome.EmptyMagazine,
                    this,
                    target,
                    0);
            }

            var nextTurret = new ManualTurretState(
                true,
                MagazineCapacity,
                AmmoInMagazine - 1,
                false,
                0f,
                AimX,
                AimY,
                IntruderHitPossible,
                PlasmaCannonInstalled,
                PlasmaCooldownRemainingSeconds,
                PlasmaActiveRemainingSeconds,
                PlasmaTickAccumulatorSeconds);

            if (!target.IsActive || !IsAimOnTarget(target))
            {
                return new ManualTurretFireResult(
                    ManualTurretFireOutcome.Miss,
                    nextTurret,
                    target,
                    0);
            }

            var nextTarget = target.WithDamage(ShotDamage);
            return new ManualTurretFireResult(
                nextTarget.IsDestroyed ? ManualTurretFireOutcome.Destroyed : ManualTurretFireOutcome.Hit,
                nextTurret,
                nextTarget,
                ShotDamage);
        }

        public bool IsAimOnTarget(ExternalTargetState target)
        {
            if (!target.IsActive)
            {
                return false;
            }

            var dx = AimX - target.PositionX;
            var dy = AimY - target.PositionY;
            return dx * dx + dy * dy <= target.HitRadius * target.HitRadius;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public readonly struct ManualTurretFireResult
    {
        public ManualTurretFireResult(
            ManualTurretFireOutcome outcome,
            ManualTurretState turret,
            ExternalTargetState target,
            int damageApplied)
        {
            Outcome = outcome;
            Turret = turret;
            Target = target;
            DamageApplied = damageApplied;
        }

        public ManualTurretFireOutcome Outcome { get; }

        public ManualTurretState Turret { get; }

        public ExternalTargetState Target { get; }

        public int DamageApplied { get; }

        public bool HitTarget =>
            Outcome == ManualTurretFireOutcome.Hit ||
            Outcome == ManualTurretFireOutcome.Destroyed;

        public bool DestroyedTarget => Outcome == ManualTurretFireOutcome.Destroyed;
    }

    public readonly struct ManualTurretPlasmaResult
    {
        public ManualTurretPlasmaResult(
            ManualTurretPlasmaOutcome outcome,
            ManualTurretState turret,
            ExternalTargetState target,
            int damageApplied)
        {
            Outcome = outcome;
            Turret = turret;
            Target = target;
            DamageApplied = Math.Max(0, damageApplied);
        }

        public ManualTurretPlasmaOutcome Outcome { get; }

        public ManualTurretState Turret { get; }

        public ExternalTargetState Target { get; }

        public int DamageApplied { get; }
    }

    public readonly struct ManualTurretPlasmaTickResult
    {
        public ManualTurretPlasmaTickResult(
            ManualTurretState turret,
            ExternalTargetState target,
            int damageApplied)
        {
            Turret = turret;
            Target = target;
            DamageApplied = Math.Max(0, damageApplied);
        }

        public ManualTurretState Turret { get; }

        public ExternalTargetState Target { get; }

        public int DamageApplied { get; }

        public bool DestroyedTarget => DamageApplied > 0 && Target.IsDestroyed;
    }
}
