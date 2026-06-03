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
        public const int DefaultAsteroidTargetHealth = 150;
        public const float ReloadDurationSeconds = 2f;
        public const float HeldFireIntervalSeconds = 0.25f;
        public const float AimInputSensitivity = 0.005f;
        public const float DefaultAsteroidHitRadius = 0.2f;

        private ManualTurretState(
            bool isActive,
            int ammoInMagazine,
            bool isReloading,
            float reloadRemainingSeconds,
            float aimX,
            float aimY,
            bool intruderHitPossible)
        {
            IsActive = isActive;
            AmmoInMagazine = Clamp(ammoInMagazine, 0, MagazineSize);
            IsReloading = isReloading;
            ReloadRemainingSeconds = Clamp(reloadRemainingSeconds, 0f, ReloadDurationSeconds);
            AimX = Clamp(aimX, -1f, 1f);
            AimY = Clamp(aimY, -1f, 1f);
            IntruderHitPossible = intruderHitPossible;
        }

        public bool IsActive { get; }

        public int AmmoInMagazine { get; }

        public bool IsReloading { get; }

        public float ReloadRemainingSeconds { get; }

        public float AimX { get; }

        public float AimY { get; }

        public bool IntruderHitPossible { get; }

        public static ManualTurretState Inactive => new ManualTurretState(
            false,
            0,
            false,
            0f,
            0f,
            0f,
            false);

        public static ManualTurretState Start(bool intruderHitPossible)
        {
            return new ManualTurretState(
                true,
                MagazineSize,
                false,
                0f,
                0f,
                0f,
                intruderHitPossible);
        }

        public ManualTurretState Stop()
        {
            return new ManualTurretState(
                false,
                AmmoInMagazine,
                false,
                0f,
                AimX,
                AimY,
                false);
        }

        public ManualTurretState Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!IsActive || !IsReloading || deltaSeconds <= 0f)
            {
                return this;
            }

            var nextRemaining = ReloadRemainingSeconds - deltaSeconds;
            if (nextRemaining > 0f)
            {
                return new ManualTurretState(
                    true,
                    AmmoInMagazine,
                    true,
                    nextRemaining,
                    AimX,
                    AimY,
                    IntruderHitPossible);
            }

            return new ManualTurretState(
                true,
                MagazineSize,
                false,
                0f,
                AimX,
                AimY,
                IntruderHitPossible);
        }

        public ManualTurretState BeginReload()
        {
            if (!IsActive || IsReloading || AmmoInMagazine >= MagazineSize)
            {
                return this;
            }

            return new ManualTurretState(
                true,
                AmmoInMagazine,
                true,
                ReloadDurationSeconds,
                AimX,
                AimY,
                IntruderHitPossible);
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
                AmmoInMagazine,
                IsReloading,
                ReloadRemainingSeconds,
                aimX,
                aimY,
                IntruderHitPossible);
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
                AmmoInMagazine - 1,
                false,
                0f,
                AimX,
                AimY,
                IntruderHitPossible);

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
}
