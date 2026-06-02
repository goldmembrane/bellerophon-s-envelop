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
    }
}
