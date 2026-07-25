using UnityEngine;

namespace Bellerophon.Core.Player
{
    [CreateAssetMenu(menuName = "Bellerophon/Player/First Person Player Settings")]
    public sealed class FirstPersonPlayerSettings : ScriptableObject
    {
        // Phase 2 player tuning values live in this asset so movement and interaction variables are not hidden in scene objects.
        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float walkSpeed = 4f;
        [SerializeField, Min(0.01f)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(0.01f)] private float crouchSpeed = 2.2f;
        [SerializeField, Min(0.01f)] private float jumpSpeed = 4.8f;
        [SerializeField, Min(0.01f)] private float gravity = 18f;

        [Header("Editor Playtest")]
        // These values affect Unity Editor Play mode only. Player builds keep the grounded CharacterController movement path.
        [SerializeField] private bool editorPlaytestFreeMovementEnabled = true;
        [SerializeField, Min(0.01f)] private float editorPlaytestFreeMoveSpeed = 7.5f;
        [SerializeField, Min(1f)] private float editorPlaytestFreeMoveFastMultiplier = 3f;
        [SerializeField, Min(0.01f)] private float editorPlaytestFreeMoveSlowMultiplier = 0.25f;

        [Header("View")]
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -82f;
        [SerializeField] private float maxPitch = 82f;

        [Header("Body")]
        [SerializeField, Min(0.1f)] private float standingHeight = 1.8f;
        [SerializeField, Min(0.1f)] private float crouchingHeight = 1.1f;
        [SerializeField, Min(0.1f)] private float characterRadius = 0.35f;
        [SerializeField, Min(0.1f)] private float cameraStandingHeight = 1.62f;
        [SerializeField, Min(0.1f)] private float cameraCrouchingHeight = 0.96f;
        [SerializeField, Min(0.01f)] private float crouchTransitionDuration = 0.22f;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField, Min(1)] private int handSlotCount = Bellerophon.Core.Session.PlayerEquipmentState.DefaultHandSlotCount;

        [Header("Vitals")]
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0)] private int maxShield = 50;

        public float WalkSpeed => Mathf.Max(0.01f, walkSpeed);

        public float SprintSpeed => Mathf.Max(WalkSpeed, sprintSpeed);

        public float CrouchSpeed => Mathf.Max(0.01f, crouchSpeed);

        public float JumpSpeed => Mathf.Max(0.01f, jumpSpeed);

        public float Gravity => Mathf.Max(0.01f, gravity);

        public bool EditorPlaytestFreeMovementEnabled => editorPlaytestFreeMovementEnabled;

        public float EditorPlaytestFreeMoveSpeed => Mathf.Max(0.01f, editorPlaytestFreeMoveSpeed);

        public float EditorPlaytestFreeMoveFastMultiplier => Mathf.Max(1f, editorPlaytestFreeMoveFastMultiplier);

        public float EditorPlaytestFreeMoveSlowMultiplier => Mathf.Clamp(editorPlaytestFreeMoveSlowMultiplier, 0.01f, 1f);

        public float MouseSensitivity => Mathf.Max(0.001f, mouseSensitivity);

        public float MinPitch => minPitch;

        public float MaxPitch => maxPitch;

        public float StandingHeight => Mathf.Max(0.1f, standingHeight);

        public float CrouchingHeight => Mathf.Clamp(crouchingHeight, 0.1f, StandingHeight);

        public float CharacterRadius => Mathf.Max(0.1f, characterRadius);

        public float CameraStandingHeight => Mathf.Max(0.1f, cameraStandingHeight);

        public float CameraCrouchingHeight => Mathf.Clamp(cameraCrouchingHeight, 0.1f, CameraStandingHeight);

        public float CrouchTransitionDuration => Mathf.Max(0.01f, crouchTransitionDuration);

        public float InteractionDistance => Mathf.Max(0.1f, interactionDistance);

        public int HandSlotCount => Mathf.Max(1, handSlotCount);

        public int MaxHealth => Mathf.Max(1, maxHealth);

        public int MaxShield => Mathf.Max(0, maxShield);
    }
}
