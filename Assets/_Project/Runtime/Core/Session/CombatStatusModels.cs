using System;

namespace Bellerophon.Core.Session
{
    public enum CombatStatusEffectKind
    {
        None,
        Stopped,
        Burn,
        Bleeding,
        Exhaustion,
        Fatigue,
        Dizziness,
        Confusion
    }

    public enum CombatDamageSourceKind
    {
        Generic,
        Physical,
        Fire,
        Electric
    }

    public readonly struct CombatStatusEffectApplication
    {
        public CombatStatusEffectApplication(
            CombatStatusEffectKind kind,
            float durationSeconds,
            float tickIntervalSeconds = 0f,
            int tickDamage = 0)
        {
            Kind = kind;
            DurationSeconds = Math.Max(0f, durationSeconds);
            TickIntervalSeconds = Math.Max(0f, tickIntervalSeconds);
            TickDamage = Math.Max(0, tickDamage);
        }

        public CombatStatusEffectKind Kind { get; }

        public float DurationSeconds { get; }

        public float TickIntervalSeconds { get; }

        public int TickDamage { get; }

        public bool HasEffect => Kind != CombatStatusEffectKind.None && DurationSeconds > 0.0001f;
    }

    public readonly struct CombatStatusEffectState
    {
        public CombatStatusEffectState(
            CombatStatusEffectKind kind,
            float remainingSeconds,
            float tickIntervalSeconds = 0f,
            int tickDamage = 0,
            float tickAccumulatorSeconds = 0f)
        {
            Kind = kind;
            RemainingSeconds = Math.Max(0f, remainingSeconds);
            TickIntervalSeconds = Math.Max(0f, tickIntervalSeconds);
            TickDamage = Math.Max(0, tickDamage);
            TickAccumulatorSeconds = Math.Max(0f, tickAccumulatorSeconds);
        }

        public CombatStatusEffectKind Kind { get; }

        public float RemainingSeconds { get; }

        public float TickIntervalSeconds { get; }

        public int TickDamage { get; }

        public float TickAccumulatorSeconds { get; }

        public bool IsActive => Kind != CombatStatusEffectKind.None && RemainingSeconds > 0.0001f;

        public static CombatStatusEffectState FromApplication(CombatStatusEffectApplication application)
        {
            return new CombatStatusEffectState(
                application.Kind,
                application.DurationSeconds,
                application.TickIntervalSeconds,
                application.TickDamage);
        }
    }

    public readonly struct CombatStatusEffectTickResult
    {
        public CombatStatusEffectTickResult(CombatStatusEffectState[] effects, int healthDamage)
        {
            Effects = CombatStatusEffectRules.CloneEffects(effects);
            HealthDamage = Math.Max(0, healthDamage);
        }

        public CombatStatusEffectState[] Effects { get; }

        public int HealthDamage { get; }
    }

    public static class CombatStatusEffectRules
    {
        public const float ElectricBatonChargedStoppedDurationSeconds = 1f;
        public const float ElectricBatonDischargeStoppedDurationSeconds = 2.5f;
        public const float ElectricBatonChargeCooldownSeconds = 60f;
        public const int ElectricBatonChargedDamageBonus = 20;
        public const float MiniFlamethrowerBurnTriggerSeconds = 1f;
        public const float BurnDefaultDurationSeconds = 5f;
        public const float BurnDefaultTickIntervalSeconds = 1f;
        public const int BurnDefaultTickDamage = 10;
        public const float BleedingDefaultDurationSeconds = 10f;
        public const float BleedingDefaultTickIntervalSeconds = 1f;
        public const int BleedingDefaultTickDamage = 10;
        public const float ExhaustionDefaultDurationSeconds = 3f;
        public const float FatigueDefaultDurationSeconds = 60f;
        public const float DizzinessDefaultDurationSeconds = 60f;
        public const float FlashbangConfusionDurationSeconds = 5f;
        public const float PlayerFlashbangWhiteoutSeconds = 3f;

        public static CombatStatusEffectApplication CreateStopped(float durationSeconds)
        {
            return new CombatStatusEffectApplication(CombatStatusEffectKind.Stopped, durationSeconds);
        }

        public static CombatStatusEffectApplication CreateBurn(float durationSeconds, int tickDamage)
        {
            return new CombatStatusEffectApplication(
                CombatStatusEffectKind.Burn,
                durationSeconds,
                BurnDefaultTickIntervalSeconds,
                tickDamage);
        }

        public static CombatStatusEffectApplication CreateBleeding(float durationSeconds, int tickDamage)
        {
            return new CombatStatusEffectApplication(
                CombatStatusEffectKind.Bleeding,
                durationSeconds,
                BleedingDefaultTickIntervalSeconds,
                tickDamage);
        }

        public static CombatStatusEffectApplication CreateExhaustion(float durationSeconds)
        {
            return new CombatStatusEffectApplication(CombatStatusEffectKind.Exhaustion, durationSeconds);
        }

        public static CombatStatusEffectApplication CreateFatigue(float durationSeconds)
        {
            return new CombatStatusEffectApplication(CombatStatusEffectKind.Fatigue, durationSeconds);
        }

        public static CombatStatusEffectApplication CreateDizziness(float durationSeconds)
        {
            return new CombatStatusEffectApplication(CombatStatusEffectKind.Dizziness, durationSeconds);
        }

        public static CombatStatusEffectApplication CreateConfusion(float durationSeconds)
        {
            return new CombatStatusEffectApplication(CombatStatusEffectKind.Confusion, durationSeconds);
        }

        public static CombatStatusEffectState[] ApplyEffect(
            CombatStatusEffectState[] current,
            CombatStatusEffectApplication application)
        {
            if (!application.HasEffect)
            {
                return CloneEffects(current);
            }

            var existing = current ?? Array.Empty<CombatStatusEffectState>();
            var next = new CombatStatusEffectState[existing.Length + 1];
            var nextCount = 0;
            for (var i = 0; i < existing.Length; i++)
            {
                if (!existing[i].IsActive || existing[i].Kind == application.Kind)
                {
                    continue;
                }

                next[nextCount] = existing[i];
                nextCount++;
            }

            next[nextCount] = CombatStatusEffectState.FromApplication(application);
            nextCount++;
            Array.Resize(ref next, nextCount);
            return next;
        }

        public static CombatStatusEffectState[] ClearEffect(
            CombatStatusEffectState[] current,
            CombatStatusEffectKind kind)
        {
            if (kind == CombatStatusEffectKind.None || current == null || current.Length == 0)
            {
                return CloneEffects(current);
            }

            var next = new CombatStatusEffectState[current.Length];
            var nextCount = 0;
            for (var i = 0; i < current.Length; i++)
            {
                if (!current[i].IsActive || current[i].Kind == kind)
                {
                    continue;
                }

                next[nextCount] = current[i];
                nextCount++;
            }

            Array.Resize(ref next, nextCount);
            return next;
        }

        public static CombatStatusEffectTickResult TickEffects(
            CombatStatusEffectState[] current,
            float deltaSeconds)
        {
            if (current == null || current.Length == 0 || deltaSeconds <= 0f)
            {
                return new CombatStatusEffectTickResult(current, 0);
            }

            var next = new CombatStatusEffectState[current.Length];
            var nextCount = 0;
            var healthDamage = 0;
            for (var i = 0; i < current.Length; i++)
            {
                var effect = current[i];
                if (!effect.IsActive)
                {
                    continue;
                }

                var activeSeconds = Math.Min(effect.RemainingSeconds, deltaSeconds);
                var remaining = Math.Max(0f, effect.RemainingSeconds - deltaSeconds);
                var accumulator = effect.TickAccumulatorSeconds;
                if (effect.TickDamage > 0 && effect.TickIntervalSeconds > 0.0001f)
                {
                    accumulator += activeSeconds;
                    while (accumulator + 0.0001f >= effect.TickIntervalSeconds)
                    {
                        accumulator -= effect.TickIntervalSeconds;
                        healthDamage += effect.TickDamage;
                    }
                }

                if (remaining <= 0.0001f)
                {
                    continue;
                }

                next[nextCount] = new CombatStatusEffectState(
                    effect.Kind,
                    remaining,
                    effect.TickIntervalSeconds,
                    effect.TickDamage,
                    accumulator);
                nextCount++;
            }

            Array.Resize(ref next, nextCount);
            return new CombatStatusEffectTickResult(next, healthDamage);
        }

        public static bool HasEffect(CombatStatusEffectState[] current, CombatStatusEffectKind kind)
        {
            if (kind == CombatStatusEffectKind.None || current == null)
            {
                return false;
            }

            for (var i = 0; i < current.Length; i++)
            {
                if (current[i].IsActive && current[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool BlocksActions(CombatStatusEffectState[] current)
        {
            return HasEffect(current, CombatStatusEffectKind.Stopped) ||
                   HasEffect(current, CombatStatusEffectKind.Confusion);
        }

        public static bool BlocksMovement(CombatStatusEffectState[] current)
        {
            return HasEffect(current, CombatStatusEffectKind.Stopped);
        }

        public static bool BlocksSprint(CombatStatusEffectState[] current)
        {
            return HasEffect(current, CombatStatusEffectKind.Stopped) ||
                   HasEffect(current, CombatStatusEffectKind.Exhaustion);
        }

        public static float CalculateMovementMultiplier(CombatStatusEffectState[] current)
        {
            if (BlocksMovement(current))
            {
                return 0f;
            }

            var multiplier = 1f;
            if (HasEffect(current, CombatStatusEffectKind.Exhaustion))
            {
                multiplier = Math.Min(multiplier, 0.2f);
            }

            if (HasEffect(current, CombatStatusEffectKind.Fatigue))
            {
                multiplier = Math.Min(multiplier, 0.7f);
            }

            if (HasEffect(current, CombatStatusEffectKind.Dizziness))
            {
                multiplier = Math.Min(multiplier, 0.5f);
            }

            if (HasEffect(current, CombatStatusEffectKind.Confusion))
            {
                multiplier = Math.Min(multiplier, 0.3f);
            }

            return multiplier;
        }

        public static float CalculateWeaponDelay(
            CombatStatusEffectState[] current,
            bool rangedWeapon,
            float baseDelaySeconds)
        {
            var delay = Math.Max(0f, baseDelaySeconds);
            if (!HasEffect(current, CombatStatusEffectKind.Fatigue))
            {
                return delay;
            }

            return rangedWeapon ? 10f : Math.Max(5f, delay * 2f);
        }

        public static string BuildHudSummary(CombatStatusEffectState[] current)
        {
            if (current == null || current.Length == 0)
            {
                return string.Empty;
            }

            var summary = string.Empty;
            for (var i = 0; i < current.Length; i++)
            {
                if (!current[i].IsActive)
                {
                    continue;
                }

                if (summary.Length > 0)
                {
                    summary += ", ";
                }

                summary += FormatEffectName(current[i].Kind);
            }

            return summary;
        }

        public static string FormatEffectName(CombatStatusEffectKind kind)
        {
            switch (kind)
            {
                case CombatStatusEffectKind.None:
                    return string.Empty;
                case CombatStatusEffectKind.Stopped:
                    return "정지";
                case CombatStatusEffectKind.Burn:
                    return "화상";
                case CombatStatusEffectKind.Bleeding:
                    return "출혈";
                case CombatStatusEffectKind.Exhaustion:
                    return "탈진";
                case CombatStatusEffectKind.Fatigue:
                    return "피로";
                case CombatStatusEffectKind.Dizziness:
                    return "현기증";
                case CombatStatusEffectKind.Confusion:
                    return "혼란";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static CombatStatusEffectState[] CloneEffects(CombatStatusEffectState[] effects)
        {
            if (effects == null || effects.Length == 0)
            {
                return Array.Empty<CombatStatusEffectState>();
            }

            var clone = new CombatStatusEffectState[effects.Length];
            Array.Copy(effects, clone, effects.Length);
            return clone;
        }
    }
}
