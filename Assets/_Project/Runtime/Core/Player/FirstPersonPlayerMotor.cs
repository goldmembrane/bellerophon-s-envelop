using UnityEngine;
using UnityEngine.InputSystem;

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
        private float authoredStepOffset;
        private readonly RaycastHit[] overheadHits=new RaycastHit[16];
        private readonly RaycastHit[] planarHits=new RaycastHit[32];
        private readonly Collider[] nearbyWalls=new Collider[32];
#if UNITY_EDITOR
        public string MovementDiagnostic { get; private set; }
        private string movementContacts;
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(movementContacts.Length<600)movementContacts+=$" {hit.collider.name}:{hit.normal:F2}";
        }
#endif

        public Transform PlayerCamera => playerCamera;

        public FirstPersonPlayerSettings Settings => settings;

        public static Vector3 CalculatePlaytestFreeMoveDirectionForValidation(
            Vector3 forward,
            Vector3 right,
            Vector2 planarInput,
            bool ascend,
            bool descend)
        {
            return CalculatePlaytestFreeMoveDirection(forward, right, planarInput, ascend, descend);
        }

        public void Configure(
            FirstPersonPlayerSettings playerSettings,
            FirstPersonPlayerInput playerInput,
            Transform cameraTransform)
        {
            settings = playerSettings;
            input = playerInput;
            playerStatus = GetComponent<FirstPersonPlayerStatus>();
            playerCamera = cameraTransform;
            InitializePitchFromAuthoredCameraRotation();
            ApplyBodySettings(false, true);
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            authoredStepOffset=characterController.stepOffset;
            if (input == null)
            {
                input = GetComponent<FirstPersonPlayerInput>();
            }

            if (playerStatus == null)
            {
                playerStatus = GetComponent<FirstPersonPlayerStatus>();
            }

            InitializePitchFromAuthoredCameraRotation();
            ApplyBodySettings(false, true);
        }

        private void InitializePitchFromAuthoredCameraRotation()
        {
            if (playerCamera == null)
            {
                pitch = 0f;
                return;
            }

            var authoredPitch = playerCamera.localEulerAngles.x;
            if (authoredPitch > 180f)
            {
                authoredPitch -= 360f;
            }

            pitch = settings == null
                ? authoredPitch
                : Mathf.Clamp(authoredPitch, settings.MinPitch, settings.MaxPitch);
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
            if (ShouldUseEditorPlaytestFreeMovement())
            {
                UpdateEditorPlaytestFreeMovement();
                return;
            }

            var crouching = input.CrouchHeld;
            ApplyBodySettings(crouching, false);
            // Do not auto-step onto a wall lip if that lift would wedge the head into its header.
            // This preserves the standing capsule and all physical wall collisions.
            float step=authoredStepOffset;
            var upper=transform.TransformPoint(characterController.center)+Vector3.up*(characterController.height*.5f-characterController.radius);
            var plannedDirection=(transform.right*input.Move.x+transform.forward*input.Move.y).normalized;
            for(int probe=0;probe<2;probe++)
            {
                var origin=upper+plannedDirection*(probe*(characterController.radius+authoredStepOffset));
                int overheadCount=Physics.SphereCastNonAlloc(origin,characterController.radius-.01f,Vector3.up,overheadHits,
                    authoredStepOffset+characterController.skinWidth+.02f,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<overheadCount;i++)
                    if(!overheadHits[i].transform.IsChildOf(transform) &&
                        !(overheadHits[i].rigidbody && !overheadHits[i].rigidbody.isKinematic) && overheadHits[i].normal.y<-.3f)
                        // skinWidth is collision tolerance, not extra geometric head height.
                        // Subtracting it here incorrectly forbids the doorway's small floor lip.
                        step=Mathf.Min(step,Mathf.Max(0,overheadHits[i].distance-.04f));
            }
            if(!Mathf.Approximately(characterController.stepOffset,step))characterController.stepOffset=step;

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

#if UNITY_EDITOR
            movementContacts="";var before=transform.position;
#endif
            var planarStep=SafePlanarStep(new Vector3(velocity.x,0,velocity.z)*Time.deltaTime);
            characterController.Move(planarStep+Vector3.up*velocity.y*Time.deltaTime);
#if UNITY_EDITOR
            MovementDiagnostic=$"input={moveInput} desired={velocity:F3} moved={transform.position-before:F4} step={characterController.stepOffset:F3} height={characterController.height:F3} flags={characterController.collisionFlags} contacts={movementContacts}";
#endif
        }

        private Vector3 SafePlanarStep(Vector3 displacement)
        {
            // Extra seam protection is only needed under a low header; preserve ordinary
            // controller sliding and step clearance throughout the rest of the ship.
            if(characterController.stepOffset>=authoredStepOffset-.001f)return displacement;
            // Reserve the controller's skin before entering a concave wall seam. Once embedded,
            // PhysX can reject even an outward move; preventing entry still permits tangent sliding.
            var center=transform.TransformPoint(characterController.center);
            float radius=characterController.radius;
            float queryRadius=radius+characterController.skinWidth+.01f;
            // Concave liners can present reversed triangle winding. Query both sides of local
            // wall faces before the capsule sweep, keeping skin outside rather than recovering it.
            float reach=queryRadius+displacement.magnitude+.08f;
            int wallCount=Physics.OverlapSphereNonAlloc(center,reach,nearbyWalls,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<wallCount;i++)
            {
                var wall=nearbyWalls[i];if(wall.transform.IsChildOf(transform) || wall.attachedRigidbody && !wall.attachedRigidbody.isKinematic)continue;
                for(int ray=0;ray<8;ray++)
                {
                    var direction=Quaternion.AngleAxis(ray*45,Vector3.up)*Vector3.forward;
                    if(!wall.Raycast(new Ray(center,direction),out var face,reach) &&
                        !wall.Raycast(new Ray(center+direction*reach,-direction),out face,reach))continue;
                    if(Mathf.Abs(face.normal.y)>.6f)continue;
                    var normal=Vector3.ProjectOnPlane(face.normal,Vector3.up).normalized;
                    if(Vector3.Dot(normal,center-face.point)<0)normal=-normal;
                    float clearance=Mathf.Max(0,Vector3.Dot(center-face.point,normal)-queryRadius-.01f);
                    float into=Vector3.Dot(displacement,normal);
                    if(into < -clearance)
                    {
#if UNITY_EDITOR
                        if(movementContacts.Length<600)movementContacts+=$" Guard:{wall.name}:{normal:F2}/{clearance:F3}";
#endif
                        displacement+=normal*(-clearance-into);
                    }
                }
            }
            // This is a lateral query only: leave the existing controller responsible for feet/head.
            // Its extra radial skin must not extend the query through the supporting floor.
            var half=Vector3.up*Mathf.Max(0,characterController.height*.5f-queryRadius-.12f);
            var result=Vector3.zero;var remaining=displacement;
            for(int pass=0;pass<3 && remaining.sqrMagnitude>.0000001f;pass++)
            {
                float length=remaining.magnitude;var direction=remaining/length;
                int count=Physics.CapsuleCastNonAlloc(center-half+result,center+half+result,
                    queryRadius,direction,planarHits,length+.01f,~0,QueryTriggerInteraction.Ignore);
                float distance=length;var normal=Vector3.zero;
                for(int i=0;i<count;i++)
                {
                    var hit=planarHits[i];
                    // Moving bodies retain normal CharacterController collision; they are not wall seams.
                    if(hit.transform.IsChildOf(transform) || hit.rigidbody && !hit.rigidbody.isKinematic)continue;
                    // An inflated query overlaps its supporting deck. Unity reports -direction
                    // for that zero-distance overlap; it is not a real horizontal wall normal.
                    if(hit.distance<=.0001f && hit.collider.Raycast(new Ray(center,Vector3.down),out var deck,characterController.height) &&
                        deck.normal.y>.6f && deck.point.y<transform.position.y+.25f)continue;
                    if(hit.distance<=.0001f)
                    {
                        // Initial-overlap sweep normals are synthetic (-travel direction).
                        // Require a real surface ahead; never block an outward escape on that normal.
                        if(!hit.collider.Raycast(new Ray(center,direction),out var ahead,length+queryRadius+.02f))continue;
                        hit.normal=ahead.normal;hit.distance=Mathf.Max(0,ahead.distance-queryRadius);
                    }
                    if(Mathf.Abs(hit.normal.y)>.6f || Vector3.Dot(direction,hit.normal)>=-.001f || hit.distance>distance)continue;
#if UNITY_EDITOR
                    if(movementContacts.Length<600)movementContacts+=$" Sweep:{hit.collider.name}:{hit.distance:F3}/{hit.normal:F2}";
#endif
                    distance=hit.distance;normal=Vector3.ProjectOnPlane(hit.normal,Vector3.up).normalized;
                }
                if(normal==Vector3.zero){result+=remaining;break;}
                var advance=direction*Mathf.Max(0,distance-.01f);result+=advance;
                remaining=Vector3.ProjectOnPlane(remaining-advance,normal);
            }
            return result;
        }

        private bool ShouldUseEditorPlaytestFreeMovement()
        {
#if UNITY_EDITOR
            return Application.isPlaying && settings.EditorPlaytestFreeMovementEnabled;
#else
            return false;
#endif
        }

        private void UpdateEditorPlaytestFreeMovement()
        {
            verticalVelocity = 0f;

            var movementBasis = playerCamera != null ? playerCamera : transform;
            var direction = CalculatePlaytestFreeMoveDirection(
                movementBasis.forward,
                movementBasis.right,
                Vector2.ClampMagnitude(input.Move, 1f),
                input.JumpHeld || IsUnsuppressedKeyHeld(Key.E),
                input.CrouchHeld || IsUnsuppressedKeyHeld(Key.Q));

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var speed = settings.EditorPlaytestFreeMoveSpeed;
            if (input.SprintHeld)
            {
                speed *= settings.EditorPlaytestFreeMoveFastMultiplier;
            }
            else if (IsUnsuppressedKeyHeld(Key.LeftAlt) || IsUnsuppressedKeyHeld(Key.RightAlt))
            {
                speed *= settings.EditorPlaytestFreeMoveSlowMultiplier;
            }

            transform.position += direction * speed * Time.unscaledDeltaTime;
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

        private static Vector3 CalculatePlaytestFreeMoveDirection(
            Vector3 forward,
            Vector3 right,
            Vector2 planarInput,
            bool ascend,
            bool descend)
        {
            var safeForward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            var safeRight = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
            var direction = safeRight * planarInput.x + safeForward * planarInput.y;
            if (ascend)
            {
                direction += Vector3.up;
            }

            if (descend)
            {
                direction -= Vector3.up;
            }

            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private bool IsUnsuppressedKeyHeld(Key key)
        {
            return !input.GameplayActionInputSuppressed &&
                   Keyboard.current != null &&
                   Keyboard.current[key].isPressed;
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

            if(!Mathf.Approximately(characterController.height,currentBodyHeight))characterController.height = currentBodyHeight;
            if(!Mathf.Approximately(characterController.radius,settings.CharacterRadius))characterController.radius = settings.CharacterRadius;
            var center=new Vector3(0f, currentBodyHeight * 0.5f, 0f);
            if(characterController.center!=center)characterController.center = center;

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
