using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellerophon.Core.Player
{
    public sealed class ModelingInspectionFreeCamera : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float baseMoveSpeed = 6f;
        [SerializeField] private float fastMoveMultiplier = 3f;
        [SerializeField] private float slowMoveMultiplier = 0.25f;
        [SerializeField] private float lookSensitivity = 0.08f;
        [SerializeField] private float minMoveSpeed = 0.5f;
        [SerializeField] private float maxMoveSpeed = 40f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private float yaw;
        private float pitch;
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;
        private bool cursorStateCaptured;

        public Camera TargetCamera => targetCamera;

        public float BaseMoveSpeed => baseMoveSpeed;

        public bool LockCursorOnPlay => lockCursorOnPlay;

        public void Configure(
            Camera camera,
            float moveSpeed,
            float fastMultiplier,
            float slowMultiplier,
            float sensitivity,
            float minSpeed,
            float maxSpeed,
            bool shouldLockCursor)
        {
            targetCamera = camera;
            baseMoveSpeed = Mathf.Clamp(moveSpeed, minSpeed, maxSpeed);
            fastMoveMultiplier = Mathf.Max(1f, fastMultiplier);
            slowMoveMultiplier = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
            lookSensitivity = Mathf.Max(0.001f, sensitivity);
            minMoveSpeed = Mathf.Max(0.01f, minSpeed);
            maxMoveSpeed = Mathf.Max(minMoveSpeed, maxSpeed);
            lockCursorOnPlay = shouldLockCursor;
            SyncAnglesFromTransform();
        }

        public void ResetView(Vector3 position, Vector3 lookAt)
        {
            transform.position = position;
            var direction = lookAt - position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            SyncAnglesFromTransform();
        }

        public void MoveForValidation(Vector3 worldDirection, float deltaSeconds)
        {
            ApplyMovement(worldDirection, Mathf.Max(0f, deltaSeconds), baseMoveSpeed);
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            SyncAnglesFromTransform();
        }

        private void OnEnable()
        {
            SyncAnglesFromTransform();
            if (Application.isPlaying && lockCursorOnPlay)
            {
                LockCursor();
            }
        }

        private void OnDisable()
        {
            RestoreCursor();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateCursorLock();
            UpdateMoveSpeed();
            UpdateLook();
            UpdateMovement();
        }

        private void UpdateCursorLock()
        {
            if (!lockCursorOnPlay)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked &&
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }
        }

        private void UpdateMoveSpeed()
        {
            if (Mouse.current == null)
            {
                return;
            }

            var scrollY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) < 0.01f)
            {
                return;
            }

            var direction = Mathf.Sign(scrollY);
            baseMoveSpeed = Mathf.Clamp(baseMoveSpeed + direction * 0.75f, minMoveSpeed, maxMoveSpeed);
        }

        private void UpdateLook()
        {
            if (Mouse.current == null ||
                (lockCursorOnPlay && Cursor.lockState != CursorLockMode.Locked))
            {
                return;
            }

            var delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateMovement()
        {
            var direction = ReadMovementDirection();
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var speed = baseMoveSpeed;
            if (IsKeyHeld(Key.LeftShift) || IsKeyHeld(Key.RightShift))
            {
                speed *= fastMoveMultiplier;
            }
            else if (IsKeyHeld(Key.LeftAlt) || IsKeyHeld(Key.RightAlt))
            {
                speed *= slowMoveMultiplier;
            }

            ApplyMovement(direction, Time.unscaledDeltaTime, speed);
        }

        private Vector3 ReadMovementDirection()
        {
            var direction = Vector3.zero;
            if (IsKeyHeld(Key.W) || IsKeyHeld(Key.UpArrow))
            {
                direction += transform.forward;
            }

            if (IsKeyHeld(Key.S) || IsKeyHeld(Key.DownArrow))
            {
                direction -= transform.forward;
            }

            if (IsKeyHeld(Key.D) || IsKeyHeld(Key.RightArrow))
            {
                direction += transform.right;
            }

            if (IsKeyHeld(Key.A) || IsKeyHeld(Key.LeftArrow))
            {
                direction -= transform.right;
            }

            if (IsKeyHeld(Key.E) || IsKeyHeld(Key.Space))
            {
                direction += Vector3.up;
            }

            if (IsKeyHeld(Key.Q) || IsKeyHeld(Key.LeftCtrl) || IsKeyHeld(Key.RightCtrl))
            {
                direction -= Vector3.up;
            }

            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void ApplyMovement(Vector3 direction, float deltaSeconds, float speed)
        {
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            transform.position += direction * speed * deltaSeconds;
        }

        private void SyncAnglesFromTransform()
        {
            var eulerAngles = transform.rotation.eulerAngles;
            yaw = eulerAngles.y;
            pitch = NormalizePitch(eulerAngles.x);
        }

        private static float NormalizePitch(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static bool IsKeyHeld(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].isPressed;
        }

        private void LockCursor()
        {
            CaptureCursorState();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            CaptureCursorState();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CaptureCursorState()
        {
            if (cursorStateCaptured)
            {
                return;
            }

            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            cursorStateCaptured = true;
        }

        private void RestoreCursor()
        {
            if (!Application.isPlaying || !cursorStateCaptured)
            {
                return;
            }

            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
            cursorStateCaptured = false;
        }
    }
}
