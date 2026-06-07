using System;

namespace Bellerophon.Core.Session
{
    public readonly struct PlayerCombatState
    {
        public PlayerCombatState(
            int maxHealth,
            int maxShield,
            int currentHealth,
            int currentShield,
            CombatStatusEffectState[] statusEffects)
        {
            MaxHealth = Math.Max(0, maxHealth);
            MaxShield = Math.Max(0, maxShield);
            CurrentHealth = Clamp(currentHealth, 0, MaxHealth);
            CurrentShield = Clamp(currentShield, 0, MaxShield);
            StatusEffects = CombatStatusEffectRules.CloneEffects(statusEffects);
        }

        public int MaxHealth { get; }

        public int MaxShield { get; }

        public int CurrentHealth { get; }

        public int CurrentShield { get; }

        public CombatStatusEffectState[] StatusEffects { get; }

        public bool IsDead => CurrentHealth <= 0;

        public PlayerCombatState WithVitals(int currentHealth, int currentShield)
        {
            return new PlayerCombatState(MaxHealth, MaxShield, currentHealth, currentShield, StatusEffects);
        }

        public PlayerCombatState WithStatusEffects(CombatStatusEffectState[] statusEffects)
        {
            return new PlayerCombatState(MaxHealth, MaxShield, CurrentHealth, CurrentShield, statusEffects);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public readonly struct PlayerDamageProfile
    {
        public PlayerDamageProfile(
            int rawDamage,
            CombatDamageSourceKind sourceKind,
            CombatStatusEffectApplication statusEffectToApply = default,
            int statusChancePercent = 100)
        {
            RawDamage = Math.Max(0, rawDamage);
            SourceKind = sourceKind;
            StatusEffectToApply = statusEffectToApply;
            StatusChancePercent = ClampPercent(statusChancePercent);
        }

        public int RawDamage { get; }

        public CombatDamageSourceKind SourceKind { get; }

        public CombatStatusEffectApplication StatusEffectToApply { get; }

        public int StatusChancePercent { get; }

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }

    public readonly struct PlayerDamageResult
    {
        public PlayerDamageResult(
            PlayerCombatState state,
            int shieldDamage,
            int healthDamage,
            int preventedDamage,
            bool statusApplied,
            bool statusPrevented,
            CombatStatusEffectKind statusEffectKind)
        {
            State = state;
            ShieldDamage = Math.Max(0, shieldDamage);
            HealthDamage = Math.Max(0, healthDamage);
            PreventedDamage = Math.Max(0, preventedDamage);
            StatusApplied = statusApplied;
            StatusPrevented = statusPrevented;
            StatusEffectKind = statusEffectKind;
        }

        public PlayerCombatState State { get; }

        public int ShieldDamage { get; }

        public int HealthDamage { get; }

        public int PreventedDamage { get; }

        public bool StatusApplied { get; }

        public bool StatusPrevented { get; }

        public CombatStatusEffectKind StatusEffectKind { get; }

        public bool WasKilled => State.IsDead;
    }

    public static class PlayerCombatRules
    {
        public static PlayerDamageResult ApplyIncomingDamage(
            PlayerCombatState state,
            PlayerEquipmentState equipment,
            PlayerDamageProfile profile,
            int statusRollPercent = 0)
        {
            var reducedDamage = EquipmentRules.CalculateDamageAfterProtection(profile.RawDamage, equipment);
            var shieldDamage = Math.Min(state.CurrentShield, reducedDamage);
            var healthDamage = Math.Min(state.CurrentHealth, reducedDamage - shieldDamage);
            var nextState = state.WithVitals(
                state.CurrentHealth - healthDamage,
                state.CurrentShield - shieldDamage);

            var statusPrevented = ShouldPreventStatus(equipment, profile, statusRollPercent);
            var statusApplied = false;
            if (profile.StatusEffectToApply.HasEffect && !statusPrevented)
            {
                nextState = ApplyStatusEffect(nextState, profile.StatusEffectToApply);
                statusApplied = true;
            }

            return new PlayerDamageResult(
                nextState,
                shieldDamage,
                healthDamage,
                profile.RawDamage - reducedDamage,
                statusApplied,
                statusPrevented,
                profile.StatusEffectToApply.Kind);
        }

        public static PlayerDamageResult TickStatusEffects(PlayerCombatState state, float deltaSeconds)
        {
            var ticked = CombatStatusEffectRules.TickEffects(state.StatusEffects, deltaSeconds);
            var healthDamage = Math.Min(state.CurrentHealth, ticked.HealthDamage);
            var next = new PlayerCombatState(
                state.MaxHealth,
                state.MaxShield,
                state.CurrentHealth - healthDamage,
                state.CurrentShield,
                ticked.Effects);
            return new PlayerDamageResult(next, 0, healthDamage, 0, false, false, CombatStatusEffectKind.None);
        }

        public static PlayerCombatState ApplyRecovery(
            PlayerCombatState state,
            int healthAmount,
            int shieldAmount)
        {
            return state.WithVitals(
                state.CurrentHealth + Math.Max(0, healthAmount),
                state.CurrentShield + Math.Max(0, shieldAmount));
        }

        public static PlayerCombatState ApplyStatusEffect(
            PlayerCombatState state,
            CombatStatusEffectApplication application)
        {
            return state.WithStatusEffects(CombatStatusEffectRules.ApplyEffect(state.StatusEffects, application));
        }

        public static PlayerCombatState ClearStatusEffect(PlayerCombatState state, CombatStatusEffectKind kind)
        {
            return state.WithStatusEffects(CombatStatusEffectRules.ClearEffect(state.StatusEffects, kind));
        }

        private static bool ShouldPreventStatus(
            PlayerEquipmentState equipment,
            PlayerDamageProfile profile,
            int statusRollPercent)
        {
            if (!profile.StatusEffectToApply.HasEffect)
            {
                return false;
            }

            var kind = profile.StatusEffectToApply.Kind;
            if (equipment.ActiveProtectiveItemKind == EquipmentItemKind.InsulatedSuit &&
                profile.SourceKind == CombatDamageSourceKind.Electric &&
                kind == CombatStatusEffectKind.Stopped)
            {
                return true;
            }

            if (equipment.ActiveProtectiveItemKind == EquipmentItemKind.FireproofSuit &&
                profile.SourceKind == CombatDamageSourceKind.Fire &&
                kind == CombatStatusEffectKind.Burn)
            {
                return true;
            }

            var chance = profile.StatusChancePercent;
            if (equipment.ActiveProtectiveItemKind == EquipmentItemKind.PhysicalProtectiveSuit &&
                (kind == CombatStatusEffectKind.Stopped || kind == CombatStatusEffectKind.Burn))
            {
                chance /= 2;
            }

            return NormalizeRoll(statusRollPercent) >= chance;
        }

        private static int NormalizeRoll(int rollPercent)
        {
            var roll = rollPercent % 100;
            return roll < 0 ? roll + 100 : roll;
        }
    }
}
