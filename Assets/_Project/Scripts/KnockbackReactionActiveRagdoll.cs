using System;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class KnockbackReactionActiveRagdoll : MonoBehaviour
    {
        public enum RuntimePhase
        {
            Waiting,
            PhysicsKnockback,
            Recovering,
            IdleHold
        }

        [SerializeField] private Animator animator;
        [SerializeField] private string upperLayerName = "KnockbackUpperSource";
        [SerializeField, Min(0f)] private float explosionDelay;
        [SerializeField, Min(0.1f)] private float explosionOriginDistance = 3f;
        [SerializeField, Min(0.1f)] private float explosionForce = 12.4f;
        [SerializeField, Min(0f)] private float explosionUpwardModifier = 0.9f;
        [SerializeField, Min(0f)] private float explosionLateralOffset = 0.65f;
        [SerializeField, Min(1f)] private float rootAngularSpring = 52f;
        [SerializeField, Min(0.1f)] private float rootAngularDamper = 10f;
        [SerializeField, Min(0.1f)] private float targetDisplacement = 2.5f;
        [SerializeField, Min(0.01f)] private float recoveryDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float idleHoldDuration = 1.5f;
        [SerializeField, Min(0.1f)] private float maximumPhysicsDuration = 2.2f;
        [SerializeField] private float sharedGroundHeight;

        private Rigidbody body;
        private CapsuleCollider bodyCollider;
        private ConfigurableJoint activeJoint;
        private Rigidbody jointAnchor;
        private GameObject jointAnchorObject;
        private GameObject runtimeGround;
        private Vector3 initialWorldPosition;
        private Quaternion initialWorldRotation;
        private Vector3 initialLocalScale;
        private Vector3 initialForward;
        private Vector3 recoveryStartPosition;
        private Quaternion recoveryStartRotation;
        private Vector3 recoveryTargetPosition;
        private Vector3 explosionHorizontalDirection;
        private float phaseStartedAt;
        private int upperLayerIndex = -1;
        private bool initialized;
        private bool awaitingInitialVelocitySample;

        public RuntimePhase Phase { get; private set; }
        public float CycleElapsed => initialized ? Time.time - cycleStartedAt : 0f;
        public float PhaseElapsed => initialized ? Time.time - phaseStartedAt : 0f;
        public float ExplosionDelay => explosionDelay;
        public float ExplosionOriginDistance => explosionOriginDistance;
        public float ExplosionForce => explosionForce;
        public float ExplosionUpwardModifier => explosionUpwardModifier;
        public float ExplosionLateralOffset => explosionLateralOffset;
        public float TargetDisplacement => targetDisplacement;
        public float RecoveryDuration => recoveryDuration;
        public float IdleHoldDuration => idleHoldDuration;
        public float SharedGroundHeight => sharedGroundHeight;
        public int CompletedCycleCount { get; private set; }
        public float LastMaximumForwardDisplacement { get; private set; }
        public float LastMaximumHeightGain { get; private set; }
        public float LastMaximumTiltDegrees { get; private set; }
        public float LastExplosionStartedAtSeconds { get; private set; }
        public float LastRecoveryDuration { get; private set; }
        public float LastIdleHoldDuration { get; private set; }
        public Vector3 LastExplosionOrigin { get; private set; }
        public Vector3 LastExplosionImpulseDirection { get; private set; }
        public Vector3 LastInitialExplosionVelocity { get; private set; }
        public float LastInitialVelocityDirectionDot { get; private set; }
        public int ExplosionEventCount { get; private set; }
        public bool AnimatorStayedEnabled { get; private set; } = true;
        public bool UsesRigidbodyExplosionForce { get; private set; }
        public bool UsesConfigurableJoint => activeJoint != null;
        public Rigidbody PhysicsBody => body;
        public Collider PhysicsCollider => bodyCollider;
        public Animator ConfiguredAnimator => animator;
        public Vector3 ApprovedForwardDirection => initialForward;
        public Vector3 ExplosionHorizontalDirection => explosionHorizontalDirection;
        public Vector3 CurrentTravelDirection
        {
            get
            {
                if (body == null)
                    return initialForward;
                Vector3 horizontal = Vector3.ProjectOnPlane(
                    body.position - initialWorldPosition,
                    Vector3.up);
                if (horizontal.sqrMagnitude <= 0.0001f)
                    horizontal = Vector3.ProjectOnPlane(
                        body.linearVelocity,
                        Vector3.up);
                return horizontal.sqrMagnitude > 0.0001f
                    ? horizontal.normalized
                    : initialForward;
            }
        }
        public float CurrentForwardDisplacement => body != null
            ? ForwardDisplacement()
            : 0f;
        public float CurrentTravelDisplacement => body != null
            ? TravelDisplacement()
            : 0f;

        private float cycleStartedAt;

        public void Configure(
            Animator configuredAnimator,
            float configuredExplosionDelay,
            float configuredOriginDistance,
            float configuredDisplacement,
            float configuredRecoveryDuration,
            float configuredIdleHoldDuration,
            float configuredGroundHeight)
        {
            animator = configuredAnimator != null
                ? configuredAnimator
                : throw new ArgumentNullException(nameof(configuredAnimator));
            explosionDelay = Mathf.Max(0f, configuredExplosionDelay);
            explosionOriginDistance = Mathf.Max(0.1f, configuredOriginDistance);
            targetDisplacement = Mathf.Max(0.1f, configuredDisplacement);
            recoveryDuration = Mathf.Max(0.01f, configuredRecoveryDuration);
            idleHoldDuration = Mathf.Max(0.01f, configuredIdleHoldDuration);
            sharedGroundHeight = configuredGroundHeight;
        }

        private void Awake()
        {
            if (!Application.isPlaying)
                return;
            animator ??= GetComponent<Animator>();
            if (animator == null)
                throw new InvalidOperationException(
                    name + " KnockbackReactionActiveRagdoll requires an Animator.");

            upperLayerIndex = animator.GetLayerIndex(upperLayerName);
            if (upperLayerIndex < 0)
                throw new InvalidOperationException(
                    name + " is missing Animator layer " + upperLayerName + ".");

            initialWorldPosition = transform.position;
            initialWorldRotation = transform.rotation;
            initialLocalScale = transform.localScale;
            initialForward = transform.forward.normalized;
            if (initialForward.sqrMagnitude < 0.99f)
                initialForward = Vector3.forward;

            BuildPhysicsBody();
            BuildActiveJoint();
            BuildRuntimeGround();
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;
            ResetCycle(false);
            initialized = true;
            BeginPhysicsKnockback();
        }

        private void FixedUpdate()
        {
            if (!initialized)
                return;

            AnimatorStayedEnabled &= animator.enabled;
            switch (Phase)
            {
                case RuntimePhase.Waiting:
                    if (Time.time - phaseStartedAt >= explosionDelay)
                        BeginPhysicsKnockback();
                    break;
                case RuntimePhase.PhysicsKnockback:
                    CaptureInitialExplosionVelocity();
                    UpdatePhysicsMetrics();
                    float travelDistance = TravelDisplacement();
                    if (travelDistance >= targetDisplacement ||
                        Time.time - phaseStartedAt >= maximumPhysicsDuration)
                        BeginRecovery();
                    break;
                case RuntimePhase.Recovering:
                    UpdateRecovery();
                    break;
                case RuntimePhase.IdleHold:
                    if (Time.time - phaseStartedAt >= idleHoldDuration)
                    {
                        ResetCycle(true);
                        BeginPhysicsKnockback();
                    }
                    break;
            }
        }

        private void BuildPhysicsBody()
        {
            body = gameObject.AddComponent<Rigidbody>();
            body.mass = 12f;
            body.linearDamping = 1.05f;
            body.angularDamping = 2.4f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.maxAngularVelocity = 7f;
            body.useGravity = false;
            body.isKinematic = true;

            Bounds bounds = CalculateVisualBounds();
            bodyCollider = gameObject.AddComponent<CapsuleCollider>();
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
            float scaleZ = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.z));
            bodyCollider.direction = 1;
            bodyCollider.center = localCenter;
            bodyCollider.height = Mathf.Max(0.8f, bounds.size.y / scaleY * 0.92f);
            bodyCollider.radius = Mathf.Clamp(
                Mathf.Max(bounds.size.x / scaleX, bounds.size.z / scaleZ) * 0.24f,
                0.22f,
                bodyCollider.height * 0.45f);
            var material = new PhysicsMaterial(name + "_KnockbackMaterial")
            {
                dynamicFriction = 0.08f,
                staticFriction = 0.08f,
                bounciness = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            material.hideFlags = HideFlags.HideAndDontSave;
            bodyCollider.material = material;
        }

        private void BuildActiveJoint()
        {
            jointAnchorObject = new GameObject(name + "_KnockbackActiveRagdollAnchor");
            jointAnchorObject.hideFlags = HideFlags.HideAndDontSave;
            jointAnchorObject.transform.SetPositionAndRotation(
                initialWorldPosition,
                initialWorldRotation);
            jointAnchor = jointAnchorObject.AddComponent<Rigidbody>();
            jointAnchor.isKinematic = true;
            jointAnchor.useGravity = false;

            activeJoint = gameObject.AddComponent<ConfigurableJoint>();
            activeJoint.connectedBody = jointAnchor;
            activeJoint.autoConfigureConnectedAnchor = true;
            activeJoint.xMotion = ConfigurableJointMotion.Free;
            activeJoint.yMotion = ConfigurableJointMotion.Free;
            activeJoint.zMotion = ConfigurableJointMotion.Free;
            activeJoint.angularXMotion = ConfigurableJointMotion.Limited;
            activeJoint.angularYMotion = ConfigurableJointMotion.Limited;
            activeJoint.angularZMotion = ConfigurableJointMotion.Limited;
            activeJoint.lowAngularXLimit = SoftLimit(-32f);
            activeJoint.highAngularXLimit = SoftLimit(32f);
            activeJoint.angularYLimit = SoftLimit(30f);
            activeJoint.angularZLimit = SoftLimit(30f);
            JointDrive angularDrive = new JointDrive
            {
                positionSpring = rootAngularSpring,
                positionDamper = rootAngularDamper,
                maximumForce = 900f
            };
            activeJoint.angularXDrive = angularDrive;
            activeJoint.angularYZDrive = angularDrive;
            activeJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
            activeJoint.projectionMode = JointProjectionMode.PositionAndRotation;
            activeJoint.projectionDistance = 0.03f;
            activeJoint.projectionAngle = 6f;
            activeJoint.enableCollision = false;
        }

        private void BuildRuntimeGround()
        {
            runtimeGround = new GameObject(name + "_KnockbackGround");
            runtimeGround.hideFlags = HideFlags.HideAndDontSave;
            runtimeGround.transform.position = new Vector3(
                initialWorldPosition.x,
                sharedGroundHeight - 0.1f,
                initialWorldPosition.z);
            BoxCollider ground = runtimeGround.AddComponent<BoxCollider>();
            ground.size = new Vector3(14f, 0.2f, 14f);
        }

        private void BeginPhysicsKnockback()
        {
            animator.SetLayerWeight(upperLayerIndex, 1f);
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            float lateralSign = CompletedCycleCount % 2 == 0 ? 1f : -1f;
            Vector3 lateral = Vector3.Cross(Vector3.up, initialForward).normalized;
            LastExplosionOrigin = initialWorldPosition -
                initialForward * explosionOriginDistance +
                lateral * explosionLateralOffset * lateralSign +
                Vector3.up * 0.15f;
            LastExplosionImpulseDirection =
                (body.worldCenterOfMass - LastExplosionOrigin +
                 Vector3.up * explosionUpwardModifier).normalized;
            explosionHorizontalDirection = Vector3.ProjectOnPlane(
                LastExplosionImpulseDirection,
                Vector3.up).normalized;
            if (explosionHorizontalDirection.sqrMagnitude < 0.99f)
                explosionHorizontalDirection = initialForward;

            // The slightly off-centre, low explosion point lets the Rigidbody trajectory
            // and rotation express the blast direction instead of a scripted path.
            body.AddExplosionForce(
                explosionForce,
                LastExplosionOrigin,
                explosionOriginDistance * 2f,
                explosionUpwardModifier,
                ForceMode.VelocityChange);
            float torqueSign = Mathf.Sign(Vector3.Dot(
                lateral,
                LastExplosionOrigin - initialWorldPosition));
            body.AddTorque(
                lateral * -1.15f +
                initialForward * torqueSign * 0.4f +
                Vector3.up * torqueSign * 0.28f,
                ForceMode.VelocityChange);
            LastInitialExplosionVelocity = Vector3.zero;
            LastInitialVelocityDirectionDot = 0f;
            awaitingInitialVelocitySample = true;
            ExplosionEventCount++;
            UsesRigidbodyExplosionForce = true;
            LastExplosionStartedAtSeconds = Time.time - cycleStartedAt;
            Phase = RuntimePhase.PhysicsKnockback;
            phaseStartedAt = Time.time;
        }

        private void CaptureInitialExplosionVelocity()
        {
            if (!awaitingInitialVelocitySample || body.linearVelocity.sqrMagnitude <= 0.0001f)
                return;
            LastInitialExplosionVelocity = body.linearVelocity;
            LastInitialVelocityDirectionDot = Vector3.Dot(
                body.linearVelocity.normalized,
                LastExplosionImpulseDirection);
            awaitingInitialVelocitySample = false;
        }

        private void BeginRecovery()
        {
            UpdatePhysicsMetrics();
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
            recoveryStartPosition = body.position;
            recoveryStartRotation = body.rotation;
            recoveryTargetPosition = initialWorldPosition +
                explosionHorizontalDirection * targetDisplacement;
            recoveryTargetPosition.y = sharedGroundHeight;
            Phase = RuntimePhase.Recovering;
            phaseStartedAt = Time.time;
        }

        private void UpdateRecovery()
        {
            float elapsed = Time.time - phaseStartedAt;
            float t = Mathf.Clamp01(elapsed / recoveryDuration);
            float smooth = t * t * (3f - 2f * t);
            body.MovePosition(Vector3.Lerp(
                recoveryStartPosition,
                recoveryTargetPosition,
                smooth));
            body.MoveRotation(Quaternion.Slerp(
                recoveryStartRotation,
                initialWorldRotation,
                smooth));
            animator.SetLayerWeight(upperLayerIndex, 1f - smooth);
            if (t < 1f)
                return;

            body.position = recoveryTargetPosition;
            body.rotation = initialWorldRotation;
            animator.SetLayerWeight(upperLayerIndex, 0f);
            LastRecoveryDuration = elapsed;
            Phase = RuntimePhase.IdleHold;
            phaseStartedAt = Time.time;
        }

        private void ResetCycle(bool completed)
        {
            if (completed)
            {
                LastIdleHoldDuration = Time.time - phaseStartedAt;
                CompletedCycleCount++;
            }
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.useGravity = false;
            body.isKinematic = true;
            body.position = initialWorldPosition;
            body.rotation = initialWorldRotation;
            transform.localScale = initialLocalScale;
            jointAnchor.position = initialWorldPosition;
            jointAnchor.rotation = initialWorldRotation;
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.SetFloat(KnockbackReactionCycleBehaviour.MoveXParameter, 0f);
            animator.SetFloat(KnockbackReactionCycleBehaviour.MoveYParameter, 0f);
            animator.SetLayerWeight(upperLayerIndex, 1f);
            Physics.SyncTransforms();

            LastMaximumForwardDisplacement = 0f;
            LastMaximumHeightGain = 0f;
            LastMaximumTiltDegrees = 0f;
            UsesRigidbodyExplosionForce = false;
            awaitingInitialVelocitySample = false;
            AnimatorStayedEnabled = true;
            Phase = RuntimePhase.Waiting;
            cycleStartedAt = Time.time;
            phaseStartedAt = Time.time;
        }

        private void UpdatePhysicsMetrics()
        {
            LastMaximumForwardDisplacement = Mathf.Max(
                LastMaximumForwardDisplacement,
                TravelDisplacement());
            LastMaximumHeightGain = Mathf.Max(
                LastMaximumHeightGain,
                body.position.y - initialWorldPosition.y);
            LastMaximumTiltDegrees = Mathf.Max(
                LastMaximumTiltDegrees,
                Quaternion.Angle(body.rotation, initialWorldRotation));
        }

        private float ForwardDisplacement()
        {
            return Vector3.Dot(body.position - initialWorldPosition, initialForward);
        }

        private float TravelDisplacement()
        {
            Vector3 direction = explosionHorizontalDirection.sqrMagnitude > 0.99f
                ? explosionHorizontalDirection
                : initialForward;
            return Vector3.Dot(body.position - initialWorldPosition, direction);
        }

        private Bounds CalculateVisualBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    name + " has no Renderer for active-ragdoll bounds.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static SoftJointLimit SoftLimit(float value)
        {
            return new SoftJointLimit { limit = value, contactDistance = 1f };
        }

        private void OnDestroy()
        {
            if (jointAnchorObject != null)
                Destroy(jointAnchorObject);
            if (runtimeGround != null)
                Destroy(runtimeGround);
            if (bodyCollider != null && bodyCollider.material != null)
                Destroy(bodyCollider.material);
        }
    }
}
