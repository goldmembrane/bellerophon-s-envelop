using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonPlayerInput : MonoBehaviour
    {
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction useAction;
        private InputAction aimAction;
        private InputAction dropAction;
        private InputAction reloadAction;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private bool cursorStateCaptured;
        private bool cursorLockSuppressed;
        private bool gameplayInputSuppressed;
        private bool cursorUnlockedByEscape;

        public event Action InteractPressed;

        public event Action UsePressed;

        public event Action AimPressed;

        public event Action DropPressed;

        public event Action ReloadPressed;

        public bool CursorLockSuppressed => cursorLockSuppressed;

        public bool GameplayInputSuppressed => gameplayInputSuppressed;

        public bool GameplayActionInputSuppressed => IsInputSuppressed;

        public bool CursorUnlockedByEscape => cursorUnlockedByEscape;

        public Vector2 Move => IsInputSuppressed ? Vector2.zero : moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public Vector2 Look => IsInputSuppressed ? Vector2.zero : lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public bool JumpPressedThisFrame => !IsInputSuppressed && jumpAction != null && jumpAction.WasPressedThisFrame();

        public bool JumpHeld => !IsInputSuppressed && jumpAction != null && jumpAction.IsPressed();

        public bool SprintHeld => !IsInputSuppressed && sprintAction != null && sprintAction.IsPressed();

        public bool CrouchHeld => !IsInputSuppressed && crouchAction != null && crouchAction.IsPressed();

        public bool AimHeld => !IsInputSuppressed && aimAction != null && aimAction.IsPressed();

        private bool IsInputSuppressed => IsGameplayActionSuppressed(
            cursorLockSuppressed,
            gameplayInputSuppressed,
            cursorUnlockedByEscape);

        public static bool IsGameplayActionSuppressedForValidation(
            bool isCursorLockSuppressed,
            bool isGameplayInputSuppressed,
            bool wasCursorUnlockedByEscape)
        {
            return IsGameplayActionSuppressed(
                isCursorLockSuppressed,
                isGameplayInputSuppressed,
                wasCursorUnlockedByEscape);
        }

        public void SetCursorLockSuppressed(bool suppressed)
        {
            cursorLockSuppressed = suppressed;
            if (!Application.isPlaying)
            {
                return;
            }

            if (suppressed)
            {
                cursorUnlockedByEscape = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (isActiveAndEnabled)
            {
                LockCursorForPlay();
            }
        }

        public void SetGameplayInputSuppressed(bool suppressed)
        {
            gameplayInputSuppressed = suppressed;
        }

        private void Awake()
        {
            CreateActions();
        }

        private void OnEnable()
        {
            EnableActions();
            LockCursorForPlay();
        }

        private void OnDisable()
        {
            cursorUnlockedByEscape = false;
            gameplayInputSuppressed = false;
            DisableActions();
            RestoreCursorState();
        }

        private void OnDestroy()
        {
            DisposeActions();
        }

        private void Update()
        {
            UpdateCursorLock();
        }

        private void CreateActions()
        {
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta", expectedControlType: "Vector2");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");

            crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            crouchAction.AddBinding("<Gamepad>/rightStickPress");

            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/f");
            interactAction.AddBinding("<Gamepad>/buttonWest");
            interactAction.performed += HandleInteractPerformed;

            useAction = new InputAction("Use", InputActionType.Button, "<Mouse>/leftButton");
            useAction.AddBinding("<Gamepad>/rightTrigger");
            useAction.performed += HandleUsePerformed;

            aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
            aimAction.AddBinding("<Gamepad>/leftTrigger");
            aimAction.performed += HandleAimPerformed;

            dropAction = new InputAction("Drop", InputActionType.Button, "<Keyboard>/b");
            dropAction.AddBinding("<Gamepad>/dpad/down");
            dropAction.performed += HandleDropPerformed;

            reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
            reloadAction.AddBinding("<Gamepad>/buttonEast");
            reloadAction.performed += HandleReloadPerformed;
        }

        private void HandleInteractPerformed(InputAction.CallbackContext context)
        {
            if (IsInputSuppressed)
            {
                return;
            }

            InteractPressed?.Invoke();
        }

        private void HandleUsePerformed(InputAction.CallbackContext context)
        {
            if (IsInputSuppressed)
            {
                return;
            }

            UsePressed?.Invoke();
        }

        private void HandleAimPerformed(InputAction.CallbackContext context)
        {
            if (IsInputSuppressed)
            {
                return;
            }

            AimPressed?.Invoke();
        }

        private void HandleDropPerformed(InputAction.CallbackContext context)
        {
            if (IsInputSuppressed)
            {
                return;
            }

            DropPressed?.Invoke();
        }

        private void HandleReloadPerformed(InputAction.CallbackContext context)
        {
            if (IsInputSuppressed)
            {
                return;
            }

            ReloadPressed?.Invoke();
        }

        private void EnableActions()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            sprintAction?.Enable();
            crouchAction?.Enable();
            interactAction?.Enable();
            useAction?.Enable();
            aimAction?.Enable();
            dropAction?.Enable();
            reloadAction?.Enable();
        }

        private void LockCursorForPlay()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!cursorStateCaptured)
            {
                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                cursorStateCaptured = true;
            }

            if (cursorLockSuppressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            cursorUnlockedByEscape = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RestoreCursorState()
        {
            if (!cursorStateCaptured)
            {
                return;
            }

            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            cursorStateCaptured = false;
        }

        private void UpdateCursorLock()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (cursorLockSuppressed)
            {
                cursorUnlockedByEscape = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (gameplayInputSuppressed)
            {
                cursorUnlockedByEscape = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                cursorUnlockedByEscape = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                cursorUnlockedByEscape = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private static bool IsGameplayActionSuppressed(
            bool isCursorLockSuppressed,
            bool isGameplayInputSuppressed,
            bool wasCursorUnlockedByEscape)
        {
            return isCursorLockSuppressed ||
                   isGameplayInputSuppressed ||
                   wasCursorUnlockedByEscape;
        }

        private void DisableActions()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            sprintAction?.Disable();
            crouchAction?.Disable();
            interactAction?.Disable();
            useAction?.Disable();
            aimAction?.Disable();
            dropAction?.Disable();
            reloadAction?.Disable();
        }

        private void DisposeActions()
        {
            moveAction?.Dispose();
            lookAction?.Dispose();
            jumpAction?.Dispose();
            sprintAction?.Dispose();
            crouchAction?.Dispose();
            interactAction?.Dispose();
            useAction?.Dispose();
            aimAction?.Dispose();
            dropAction?.Dispose();
            reloadAction?.Dispose();
        }
    }
}
