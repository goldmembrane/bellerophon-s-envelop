using System;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonHandInventory : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerSettings settings;
        [SerializeField] private FirstPersonPlayerInput input;
        [SerializeField] private int activeSlotIndex;

        public event Action<int> UseRequested;

        public event Action<int> DropRequested;

        public event Action<int> SlotSelected;

        public int SlotCount => settings == null ? PlayerEquipmentState.DefaultHandSlotCount : settings.HandSlotCount;

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

        private void Update()
        {
            ProcessSlotSelectionInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        public void SelectSlotForValidation(int slotIndex)
        {
            SelectSlot(slotIndex, true);
        }

        public void SyncActiveSlotIndex(int slotIndex)
        {
            SelectSlot(slotIndex, false);
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

        private void ProcessSlotSelectionInput()
        {
            if (IsGameplayInputSuppressed())
            {
                return;
            }

            var requestedSlot = ReadRequestedSlotIndex();
            if (requestedSlot >= 0)
            {
                SelectSlot(requestedSlot, true);
                return;
            }

            var scrollDirection = ReadScrollDirection();
            if (scrollDirection != 0)
            {
                SelectSlot(WrapSlotIndex(activeSlotIndex + scrollDirection), true);
            }
        }

        private bool IsGameplayInputSuppressed()
        {
            return input != null && (input.CursorLockSuppressed || input.GameplayInputSuppressed);
        }

        private int ReadRequestedSlotIndex()
        {
            if (Keyboard.current == null)
            {
                return -1;
            }

            for (var i = 0; i < SlotCount && i < PlayerEquipmentState.MaxHandSlotCount; i++)
            {
                if (IsSlotKeyPressed(i + 1))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsSlotKeyPressed(int displayIndex)
        {
            switch (displayIndex)
            {
                case 1:
                    return Keyboard.current[Key.Digit1].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad1].wasPressedThisFrame;
                case 2:
                    return Keyboard.current[Key.Digit2].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad2].wasPressedThisFrame;
                case 3:
                    return Keyboard.current[Key.Digit3].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad3].wasPressedThisFrame;
                case 4:
                    return Keyboard.current[Key.Digit4].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad4].wasPressedThisFrame;
                default:
                    return false;
            }
        }

        private static int ReadScrollDirection()
        {
            if (Mouse.current == null)
            {
                return 0;
            }

            var scrollY = Mouse.current.scroll.ReadValue().y;
            if (scrollY > 0.01f)
            {
                return -1;
            }

            return scrollY < -0.01f ? 1 : 0;
        }

        private int WrapSlotIndex(int slotIndex)
        {
            var count = Mathf.Max(1, SlotCount);
            if (slotIndex < 0)
            {
                return count - 1;
            }

            return slotIndex >= count ? 0 : slotIndex;
        }

        private void SelectSlot(int slotIndex, bool notify)
        {
            var clamped = Mathf.Clamp(slotIndex, 0, Mathf.Max(1, SlotCount) - 1);
            if (activeSlotIndex == clamped)
            {
                return;
            }

            activeSlotIndex = clamped;
            if (notify)
            {
                SlotSelected?.Invoke(activeSlotIndex);
            }
        }
    }
}
