using UnityEngine;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonPlayerStatus : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerSettings settings;
        [SerializeField] private int currentHealth;
        [SerializeField] private int currentShield;

        public int CurrentHealth => currentHealth;

        public int CurrentShield => currentShield;

        public int MaxHealth => settings == null ? currentHealth : settings.MaxHealth;

        public int MaxShield => settings == null ? currentShield : settings.MaxShield;

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

        public void ResetVitals()
        {
            if (settings == null)
            {
                return;
            }

            currentHealth = settings.MaxHealth;
            currentShield = settings.MaxShield;
        }

        public void ApplyRecovery(int healthAmount, int shieldAmount)
        {
            if (healthAmount > 0)
            {
                currentHealth = Mathf.Clamp(currentHealth + healthAmount, 0, MaxHealth);
            }

            if (shieldAmount > 0)
            {
                currentShield = Mathf.Clamp(currentShield + shieldAmount, 0, MaxShield);
            }
        }

        public void ApplyDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            var shieldDamage = Mathf.Min(currentShield, damage);
            currentShield -= shieldDamage;
            currentHealth = Mathf.Clamp(currentHealth - (damage - shieldDamage), 0, MaxHealth);
        }

        public void SetVitalsForValidation(int health, int shield)
        {
            currentHealth = Mathf.Clamp(health, 0, MaxHealth);
            currentShield = Mathf.Clamp(shield, 0, MaxShield);
        }
    }
}
