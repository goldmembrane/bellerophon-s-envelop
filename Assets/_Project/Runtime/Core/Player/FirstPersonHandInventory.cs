using System;
using UnityEngine;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonHandInventory : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerSettings settings;
        [SerializeField] private FirstPersonPlayerInput input;
        [SerializeField] private int activeSlotIndex;

        public event Action<int> UseRequested;

        public event Action<int> DropRequested;

        public int SlotCount => settings == null ? 2 : settings.HandSlotCount;

        public int ActiveSlotIndex => activeSlotIndex;

        public bool IsAiming => input != null && input.AimHeld;

        public void Configure(FirstPersonPlayerSettings playerSettings, FirstPersonPlayerInput playerInput)
        {
            settings = playerSettings;
            input = playerInput;
            activeSlotIndex = 0;
            SubscribeInput();
        }

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<FirstPersonPlayerInput>();
            }
        }

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void SubscribeInput()
        {
            if (input == null)
            {
                return;
            }

            input.UsePressed -= HandleUsePressed;
            input.DropPressed -= HandleDropPressed;
            input.UsePressed += HandleUsePressed;
            input.DropPressed += HandleDropPressed;
        }

        private void UnsubscribeInput()
        {
            if (input == null)
            {
                return;
            }

            input.UsePressed -= HandleUsePressed;
            input.DropPressed -= HandleDropPressed;
        }

        private void HandleUsePressed()
        {
            UseRequested?.Invoke(activeSlotIndex);
        }

        private void HandleDropPressed()
        {
            DropRequested?.Invoke(activeSlotIndex);
        }
    }
}
