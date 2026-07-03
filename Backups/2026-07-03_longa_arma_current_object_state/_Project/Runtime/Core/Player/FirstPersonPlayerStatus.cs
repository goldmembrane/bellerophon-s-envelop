using UnityEngine;
using Bellerophon.Core.Session;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonPlayerStatus : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerSettings settings;
        [SerializeField] private int currentHealth;
        [SerializeField] private int currentShield;

        // Runtime-only source status effects; pure rules live in PlayerCombatRules.
        private CombatStatusEffectState[] activeStatusEffects = new CombatStatusEffectState[0];
        private ScheduledStatusEffect[] scheduledStatusEffects = new ScheduledStatusEffect[0];
        private PlayerPostureState postureState = PlayerPostureState.Standing;

        public int CurrentHealth => currentHealth;

        public int CurrentShield => currentShield;

        public int MaxHealth => settings == null ? currentHealth : settings.MaxHealth;

        public int MaxShield => settings == null ? currentShield : settings.MaxShield;

        public CombatStatusEffectState[] ActiveStatusEffects =>
            CombatStatusEffectRules.CloneEffects(activeStatusEffects);

        public string StatusEffectSummary => CombatStatusEffectRules.BuildHudSummary(activeStatusEffects);

        public PlayerPostureState PostureState => postureState;

        public bool IsDead => currentHealth <= 0;

        public bool IsActionBlocked => postureState != PlayerPostureState.Standing ||
                                       CombatStatusEffectRules.BlocksActions(activeStatusEffects);

        public bool IsMovementBlocked => postureState != PlayerPostureState.Standing ||
                                         CombatStatusEffectRules.BlocksMovement(activeStatusEffects);

        public bool IsSprintBlocked => postureState != PlayerPostureState.Standing ||
                                       CombatStatusEffectRules.BlocksSprint(activeStatusEffects);

        public float MovementMultiplier => postureState == PlayerPostureState.Standing
            ? CombatStatusEffectRules.CalculateMovementMultiplier(activeStatusEffects)
            : 0f;

        public void Configure(FirstPersonPlayerSettings playerSettings)
        {
            settings = playerSettings;
            ResetVitals();
        }

        private void Awake()
        {
            if (settings != null && currentHealth <= 0)
            {
                ResetVitals();
            }
        }

        private void Update()
        {
            TickStatusEffects(Time.deltaTime);
        }

        public void ResetVitals()
        {
            if (settings == null)
            {
                return;
            }

            currentHealth = settings.MaxHealth;
            currentShield = settings.MaxShield;
            activeStatusEffects = new CombatStatusEffectState[0];
            scheduledStatusEffects = new ScheduledStatusEffect[0];
            postureState = PlayerPostureState.Standing;
        }

        public void ApplyRecovery(int healthAmount, int shieldAmount)
        {
            ApplyCombatState(PlayerCombatRules.ApplyRecovery(CurrentCombatState, healthAmount, shieldAmount));
        }

        public PlayerDamageResult ApplyIncomingDamage(
            PlayerDamageProfile profile,
            PlayerEquipmentState equipment,
            int statusRollPercent = 0)
        {
            var result = PlayerCombatRules.ApplyIncomingDamage(
                CurrentCombatState,
                equipment,
                profile,
                statusRollPercent);
            ApplyCombatState(result.State);
            return result;
        }

        public void ApplyDamage(int damage)
        {
            ApplyIncomingDamage(
                new PlayerDamageProfile(damage, CombatDamageSourceKind.Generic),
                PlayerEquipmentState.Empty);
        }

        public void ApplyStatusEffect(CombatStatusEffectApplication application)
        {
            ApplyCombatState(PlayerCombatRules.ApplyStatusEffect(CurrentCombatState, application));
        }

        public void ClearStatusEffect(CombatStatusEffectKind kind)
        {
            ApplyCombatState(PlayerCombatRules.ClearStatusEffect(CurrentCombatState, kind));
        }

        public void ApplyPostureState(PlayerPostureState state)
        {
            ApplyCombatState(PlayerCombatRules.ApplyPostureState(CurrentCombatState, state));
        }

        public void ClearPostureState()
        {
            ApplyCombatState(PlayerCombatRules.ClearPostureState(CurrentCombatState));
        }

        public bool HasStatusEffect(CombatStatusEffectKind kind)
        {
            return CombatStatusEffectRules.HasEffect(activeStatusEffects, kind);
        }

        public void ScheduleStatusEffect(CombatStatusEffectApplication application, float delaySeconds)
        {
            if (!application.HasEffect)
            {
                return;
            }

            var next = new ScheduledStatusEffect[scheduledStatusEffects.Length + 1];
            for (var i = 0; i < scheduledStatusEffects.Length; i++)
            {
                next[i] = scheduledStatusEffects[i];
            }

            next[next.Length - 1] = new ScheduledStatusEffect(application, Mathf.Max(0f, delaySeconds));
            scheduledStatusEffects = next;
        }

        public PlayerDamageResult TickStatusEffects(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return default;
            }

            var result = PlayerCombatRules.TickStatusEffects(CurrentCombatState, deltaSeconds);
            ApplyCombatState(result.State);
            TickScheduledStatusEffects(deltaSeconds);
            return result;
        }

        public void SetVitalsForValidation(int health, int shield)
        {
            currentHealth = Mathf.Clamp(health, 0, MaxHealth);
            currentShield = Mathf.Clamp(shield, 0, MaxShield);
        }

        public void ClearStatusEffectsForValidation()
        {
            activeStatusEffects = new CombatStatusEffectState[0];
            scheduledStatusEffects = new ScheduledStatusEffect[0];
            postureState = PlayerPostureState.Standing;
        }

        private PlayerCombatState CurrentCombatState => new PlayerCombatState(
            MaxHealth,
            MaxShield,
            currentHealth,
            currentShield,
            activeStatusEffects,
            postureState);

        private void ApplyCombatState(PlayerCombatState state)
        {
            currentHealth = Mathf.Clamp(state.CurrentHealth, 0, MaxHealth);
            currentShield = Mathf.Clamp(state.CurrentShield, 0, MaxShield);
            activeStatusEffects = state.StatusEffects;
            postureState = state.PostureState;
        }

        private void TickScheduledStatusEffects(float deltaSeconds)
        {
            if (scheduledStatusEffects.Length == 0)
            {
                return;
            }

            var next = new ScheduledStatusEffect[scheduledStatusEffects.Length];
            var nextCount = 0;
            for (var i = 0; i < scheduledStatusEffects.Length; i++)
            {
                var scheduled = scheduledStatusEffects[i].Tick(deltaSeconds);
                if (scheduled.RemainingDelaySeconds <= 0.0001f)
                {
                    ApplyStatusEffect(scheduled.Application);
                    continue;
                }

                next[nextCount] = scheduled;
                nextCount++;
            }

            if (nextCount != next.Length)
            {
                System.Array.Resize(ref next, nextCount);
            }

            scheduledStatusEffects = next;
        }

        private readonly struct ScheduledStatusEffect
        {
            public ScheduledStatusEffect(CombatStatusEffectApplication application, float remainingDelaySeconds)
            {
                Application = application;
                RemainingDelaySeconds = Mathf.Max(0f, remainingDelaySeconds);
            }

            public CombatStatusEffectApplication Application { get; }

            public float RemainingDelaySeconds { get; }

            public ScheduledStatusEffect Tick(float deltaSeconds)
            {
                return new ScheduledStatusEffect(Application, RemainingDelaySeconds - Mathf.Max(0f, deltaSeconds));
            }
        }
    }
}
