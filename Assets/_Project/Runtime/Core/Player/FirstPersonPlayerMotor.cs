using UnityEngine;

namespace Bellerophon.Core.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayerMotor : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerSettings settings;
        [SerializeField] private FirstPersonPlayerInput input;
        [SerializeField] private FirstPersonPlayerStatus playerStatus;
        [SerializeField] private Transform playerCamera;

        private CharacterController characterController;
        private float pitch;
        private float verticalVelocity;
        private float currentBodyHeight;
        private float currentCameraHeight;
        private float bodyHeightVelocity;
        private float cameraHeightVelocity;
        private bool bodySettingsInitialized;

        public Transform PlayerCamera => playerCamera;

        public FirstPersonPlayerSettings Settings => settings;

        public void Configure(
            FirstPersonPlayerSettings playerSettings,
            FirstPersonPlayerInput playerInput,
            Transform cameraTransform)
        {
            settings = playerSettings;
            input = playerInput;
            playerStatus = GetComponent<FirstPersonPlayerStatus>();
            playerCamera = cameraTransform;
            ApplyBodySettings(false, true);
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (input == null)
            {
                input = GetComponent<FirstPersonPlayerInput>();
            }

            if (playerStatus == null)
            {
                playerStatus = GetComponent<FirstPersonPlayerStatus>();
            }

            ApplyBodySettings(false, true);
        }

        private void Update()
        {
            if (settings == null || input == null || playerCamera == null)
            {
                return;
            }

            UpdateView();
            UpdateMovement();
        }

        private void UpdateView()
        {
            var look = input.Look * settings.MouseSensitivity;
            transform.Rotate(Vector3.up, look.x, Space.World);

            pitch = Mathf.Clamp(pitch - look.y, settings.MinPitch, settings.MaxPitch);
            playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            var crouching = input.CrouchHeld;
            ApplyBodySettings(crouching, false);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            if (characterController.isGrounded &&
                input.JumpPressedThisFrame &&
                !crouching &&
                (playerStatus == null || !playerStatus.IsMovementBlocked))
            {
                verticalVelocity = settings.JumpSpeed;
            }

            verticalVelocity -= settings.Gravity * Time.deltaTime;

            var moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            var planarMove = transform.right * moveInput.x + transform.forward * moveInput.y;
            var speed = GetMoveSpeed(crouching);
            var velocity = planarMove * speed;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }

        private float GetMoveSpeed(bool crouching)
        {
            float baseSpeed;
            if (crouching)
            {
                baseSpeed = settings.CrouchSpeed;
            }
            else
            {
                var canSprint = playerStatus == null || !playerStatus.IsSprintBlocked;
                baseSpeed = input.SprintHeld && canSprint ? settings.SprintSpeed : settings.WalkSpeed;
            }

            return baseSpeed * GetStatusMovementMultiplier();
        }

        private float GetStatusMovementMultiplier()
        {
            return playerStatus == null ? 1f : playerStatus.MovementMultiplier;
        }

        private void ApplyBodySettings(bool crouching, bool immediate)
        {
            if (settings == null)
            {
                return;
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            var targetBodyHeight = crouching ? settings.CrouchingHeight : settings.StandingHeight;
            var targetCameraHeight = crouching ? settings.CameraCrouchingHeight : settings.CameraStandingHeight;
            if (!bodySettingsInitialized || immediate || !Application.isPlaying)
            {
                currentBodyHeight = targetBodyHeight;
                currentCameraHeight = targetCameraHeight;
                bodyHeightVelocity = 0f;
                cameraHeightVelocity = 0f;
                bodySettingsInitialized = true;
            }
            else
            {
                var transitionDuration = settings.CrouchTransitionDuration;
                currentBodyHeight = Mathf.SmoothDamp(
                    currentBodyHeight,
                    targetBodyHeight,
                    ref bodyHeightVelocity,
                    transitionDuration,
                    Mathf.Infinity,
                    Time.deltaTime);
                currentCameraHeight = Mathf.SmoothDamp(
                    currentCameraHeight,
                    targetCameraHeight,
                    ref cameraHeightVelocity,
                    transitionDuration,
                    Mathf.Infinity,
                    Time.deltaTime);

                currentBodyHeight = SnapWhenClose(currentBodyHeight, targetBodyHeight, 0.001f);
                currentCameraHeight = SnapWhenClose(currentCameraHeight, targetCameraHeight, 0.001f);
            }

            characterController.height = currentBodyHeight;
            characterController.radius = settings.CharacterRadius;
            characterController.center = new Vector3(0f, currentBodyHeight * 0.5f, 0f);

            if (playerCamera != null)
            {
                playerCamera.localPosition = new Vector3(0f, currentCameraHeight, 0f);
            }
        }

        private static float SnapWhenClose(float value, float target, float tolerance)
        {
            return Mathf.Abs(value - target) <= tolerance ? target : value;
        }
    }
}
