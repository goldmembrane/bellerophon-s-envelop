using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Runtime.Animation
{
    [DisallowMultipleComponent]
    public sealed class DeathRagdollLoop : MonoBehaviour
    {
        public enum PlaybackMode
        {
            AnimationEnd,
            FullBodyLaunch
        }

        private enum ColliderShape
        {
            Capsule,
            Box,
            Sphere
        }

        private enum RuntimePhase
        {
            Animation,
            Ragdoll,
            LandingHold
        }

        private readonly struct BoneDefinition
        {
            internal BoneDefinition(string path, string connectedPath, string endpointPath,
                ColliderShape shape, float mass, Vector3 boxSize, float sphereRadius)
            {
                Path = path;
                ConnectedPath = connectedPath;
                EndpointPath = endpointPath;
                Shape = shape;
                Mass = mass;
                BoxSize = boxSize;
                SphereRadius = sphereRadius;
            }

            internal string Path { get; }
            internal string ConnectedPath { get; }
            internal string EndpointPath { get; }
            internal ColliderShape Shape { get; }
            internal float Mass { get; }
            internal Vector3 BoxSize { get; }
            internal float SphereRadius { get; }
        }

        private sealed class BoneBinding
        {
            internal Transform Bone;
            internal Rigidbody Body;
            internal Collider Collider;
            internal Vector3 InitialLocalPosition;
            internal Quaternion InitialLocalRotation;
            internal Vector3 InitialLocalScale;
        }

        private static readonly BoneDefinition[] BoneDefinitions =
        {
            Box("Armature/Hips", null, 3f, new Vector3(0.28f, 0.18f, 0.22f)),
            Capsule("Armature/Hips/Spine02", "Armature/Hips", "Armature/Hips/Spine02/Spine01", 1.5f),
            Capsule("Armature/Hips/Spine02/Spine01", "Armature/Hips/Spine02", "Armature/Hips/Spine02/Spine01/Spine", 1.5f),
            Box("Armature/Hips/Spine02/Spine01/Spine", "Armature/Hips/Spine02/Spine01", 2.5f, new Vector3(0.34f, 0.24f, 0.20f)),
            Sphere("Armature/Hips/Spine02/Spine01/Spine/neck/Head", "Armature/Hips/Spine02/Spine01/Spine", 1.2f, 0.16f),
            Capsule("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm", "Armature/Hips/Spine02/Spine01/Spine", "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm", 0.8f),
            Capsule("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm", "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm", "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand", 0.6f),
            Box("Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand", "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm", 0.3f, new Vector3(0.12f, 0.16f, 0.09f)),
            Capsule("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm", "Armature/Hips/Spine02/Spine01/Spine", "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm", 0.8f),
            Capsule("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm", "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm", "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand", 0.6f),
            Box("Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand", "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm", 0.3f, new Vector3(0.12f, 0.16f, 0.09f)),
            Capsule("Armature/Hips/LeftUpLeg", "Armature/Hips", "Armature/Hips/LeftUpLeg/LeftLeg", 1.4f),
            Capsule("Armature/Hips/LeftUpLeg/LeftLeg", "Armature/Hips/LeftUpLeg", "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot", 1.0f),
            Box("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot", "Armature/Hips/LeftUpLeg/LeftLeg", 0.5f, new Vector3(0.13f, 0.24f, 0.11f)),
            Capsule("Armature/Hips/RightUpLeg", "Armature/Hips", "Armature/Hips/RightUpLeg/RightLeg", 1.4f),
            Capsule("Armature/Hips/RightUpLeg/RightLeg", "Armature/Hips/RightUpLeg", "Armature/Hips/RightUpLeg/RightLeg/RightFoot", 1.0f),
            Box("Armature/Hips/RightUpLeg/RightLeg/RightFoot", "Armature/Hips/RightUpLeg/RightLeg", 0.5f, new Vector3(0.13f, 0.24f, 0.11f))
        };

        private const float GroundThickness = 0.2f;
        private const float GroundSize = 10f;
        private const float GroundContactTolerance = 0.035f;
        private const float GroundFitClearance = 0.006f;
        private const float MinimumContactSettleSeconds = 0.4f;

        [SerializeField] private Animator animator;
        [SerializeField] private string stateName = "Death_Source";
        [SerializeField] private PlaybackMode playbackMode;
        [SerializeField] private float sharedGroundHeight;
        [SerializeField, Min(0.01f)] private float ragdollDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float launchDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float landingHoldDuration = 0.5f;
        [SerializeField, Min(0.1f)] private float maximumHorizontalRange = 3f;

        private readonly List<Rigidbody> bodies = new List<Rigidbody>();
        private readonly List<Collider> colliders = new List<Collider>();
        private readonly List<CharacterJoint> joints = new List<CharacterJoint>();
        private readonly List<BoneBinding> bindings = new List<BoneBinding>();
        private GameObject runtimeGround;
        private Rigidbody hipsBody;
        private Rigidbody headBody;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private Vector3 initialLocalScale;
        private Vector3 ragdollStartHipsPosition;
        private Quaternion ragdollStartHipsRotation;
        private Vector3 ragdollStartHeadPosition;
        private Vector3 ragdollStartTorsoAxis;
        private Vector3 ragdollStartRootPosition;
        private Vector3 launchTumbleAxis;
        private int stateHash;
        private float cycleStartedAt;
        private float ragdollStartedAt;
        private float landingHoldStartedAt;
        private float firstGroundContactAt = -1f;
        private bool initialized;
        private RuntimePhase phase;

        public bool IsRagdollActive => phase != RuntimePhase.Animation;
        public bool IsLandingHold => phase == RuntimePhase.LandingHold;
        public bool HasGroundContact { get; private set; }
        public Animator ConfiguredAnimator => animator;
        public string StateName => stateName;
        public PlaybackMode Mode => playbackMode;
        public float SharedGroundHeight => sharedGroundHeight;
        public float CycleElapsed => initialized ? Time.time - cycleStartedAt : 0f;
        public float RagdollElapsed => phase == RuntimePhase.Ragdoll ? Time.time - ragdollStartedAt : 0f;
        public float LandingHoldElapsed => phase == RuntimePhase.LandingHold ? Time.time - landingHoldStartedAt : 0f;
        public float RagdollDuration => ragdollDuration;
        public float LaunchDelay => launchDelay;
        public float LandingHoldDuration => landingHoldDuration;
        public float MaximumHorizontalRange => maximumHorizontalRange;
        public int ConfiguredBodyCount => bodies.Count;
        public Bounds DirectBonePhysicsBounds
        {
            get
            {
                if (bindings.Count == 0) return new Bounds(transform.position, Vector3.one);
                var bounds = new Bounds(bindings[0].Bone.position, Vector3.zero);
                foreach (BoneBinding binding in bindings) bounds.Encapsulate(binding.Bone.position);
                bounds.Expand(0.5f);
                return bounds;
            }
        }
        public bool UsesDirectBoneRigidbodies
        {
            get
            {
                if (bindings.Count == 0) return false;
                foreach (BoneBinding binding in bindings)
                    if (binding.Body == null || binding.Body.transform != binding.Bone) return false;
                return true;
            }
        }
        public int ActiveDynamicBodyCount
        {
            get
            {
                int count = 0;
                foreach (Rigidbody body in bodies)
                    if (body != null && !body.isKinematic) count++;
                return count;
            }
        }

        public int CompletedCycleCount { get; private set; }
        public float LastRagdollDuration { get; private set; }
        public float LastLaunchStartedAtSeconds { get; private set; }
        public float LastFlightDuration { get; private set; }
        public float LastLandingHoldDuration { get; private set; }
        public float LastMaximumHorizontalDisplacement { get; private set; }
        public float LastMaximumHipsHeightGain { get; private set; }
        public float LastMaximumHipsDrop { get; private set; }
        public float LastMaximumHipsRotationDegrees { get; private set; }
        public float LastMaximumAirborneHipsRotationDegrees { get; private set; }
        public float LastMaximumAirborneTorsoTiltDegrees { get; private set; }
        public float LastMaximumHeadRise { get; private set; }
        public float LastMaximumAverageAngularSpeed { get; private set; }
        public float LastMaximumJointAnchorSeparation { get; private set; }
        public float LastMaximumRootTransformDisplacement { get; private set; }
        public float LastMaximumGroundPenetration { get; private set; }
        public float LastLandingHeadHeightAboveGround { get; private set; }
        public float LastResetPositionError { get; private set; }
        public float LastResetRotationError { get; private set; }
        public Vector3 LastLaunchDirection { get; private set; }

        public void Configure(Animator configuredAnimator, string configuredStateName, float configuredDuration) =>
            ConfigureAnimationDeath(configuredAnimator, configuredStateName, configuredDuration, transform.position.y);

        public void ConfigureAnimationDeath(Animator configuredAnimator, string configuredStateName,
            float configuredDuration, float configuredGroundHeight)
        {
            animator = configuredAnimator;
            stateName = configuredStateName;
            playbackMode = PlaybackMode.AnimationEnd;
            ragdollDuration = Mathf.Max(0.01f, configuredDuration);
            sharedGroundHeight = configuredGroundHeight;
        }

        public void ConfigureFullBodyLaunch(Animator configuredAnimator, float configuredLaunchDelay,
            float configuredLandingHoldDuration, float configuredMaximumHorizontalRange,
            float configuredGroundHeight)
        {
            animator = configuredAnimator;
            stateName = string.Empty;
            playbackMode = PlaybackMode.FullBodyLaunch;
            launchDelay = Mathf.Max(0.01f, configuredLaunchDelay);
            landingHoldDuration = Mathf.Max(0.01f, configuredLandingHoldDuration);
            maximumHorizontalRange = Mathf.Max(0.1f, configuredMaximumHorizontalRange);
            sharedGroundHeight = configuredGroundHeight;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            animator ??= GetComponent<Animator>();
            if (animator == null)
                throw new InvalidOperationException(name + " DeathRagdollLoop requires an Animator.");
            if (playbackMode == PlaybackMode.AnimationEnd && string.IsNullOrWhiteSpace(stateName))
                throw new InvalidOperationException(name + " DeathRagdollLoop state name is empty.");

            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            initialLocalScale = transform.localScale;
            stateHash = string.IsNullOrEmpty(stateName) ? 0 : Animator.StringToHash(stateName);
            BuildRuntimeRagdoll();
            BuildRuntimeGround();
            IgnoreOtherDeathRagdollCollisions();
        }

        private void Start()
        {
            ResetToAnimationStart(false);
            initialized = true;
        }

        private void FixedUpdate()
        {
            if (!initialized) return;
            if (phase == RuntimePhase.Animation && playbackMode == PlaybackMode.FullBodyLaunch &&
                Time.time - cycleStartedAt >= launchDelay)
            {
                BeginRagdoll(true);
                return;
            }
            if (phase != RuntimePhase.Ragdoll) return;
            bool airborne = hipsBody.position.y - ragdollStartHipsPosition.y >= 0.04f;
            bool contactedWhileUpright = HasGroundContact &&
                headBody.worldCenterOfMass.y > sharedGroundHeight + 0.55f;
            if (playbackMode == PlaybackMode.FullBodyLaunch &&
                Time.time - ragdollStartedAt <= 1.35f &&
                (airborne || contactedWhileUpright) &&
                CurrentTorsoTiltDegrees() < 80f)
                ApplyAirborneTumbleTorque();
            UpdateRagdollMetrics();
            if (playbackMode == PlaybackMode.FullBodyLaunch && HasSettledOnGround())
                BeginLandingHold();
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (phase == RuntimePhase.Ragdoll)
            {
                UpdateRagdollMetrics();
                if (playbackMode == PlaybackMode.AnimationEnd &&
                    Time.time - ragdollStartedAt >= ragdollDuration)
                    ResetToAnimationStart(true);
                return;
            }
            if (phase == RuntimePhase.LandingHold)
            {
                if (Time.time - landingHoldStartedAt >= landingHoldDuration)
                    ResetToAnimationStart(true);
                return;
            }
            if (playbackMode != PlaybackMode.AnimationEnd) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(stateName) && state.normalizedTime >= 0.999f)
                BeginRagdoll(false);
        }

        private void BeginRagdoll(bool applyLaunchImpulse)
        {
            if (phase != RuntimePhase.Animation) return;
            animator.enabled = false;
            foreach (BoneBinding binding in bindings)
            {
                binding.Collider.enabled = true;
                binding.Body.isKinematic = true;
                binding.Body.useGravity = false;
            }
            Physics.SyncTransforms();
            FitCollidersAboveSharedGround();
            Physics.SyncTransforms();
            foreach (BoneBinding binding in bindings)
            {
                Rigidbody body = binding.Body;
                ConfigureDynamicBodyForMode(body);
                body.isKinematic = false;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = true;
                if (playbackMode == PlaybackMode.AnimationEnd && !applyLaunchImpulse)
                    body.Sleep();
                else
                    body.WakeUp();
            }

            ragdollStartedAt = Time.time;
            ragdollStartHipsPosition = hipsBody.position;
            ragdollStartHipsRotation = hipsBody.rotation;
            ragdollStartHeadPosition = headBody.position;
            ragdollStartTorsoAxis = (ragdollStartHeadPosition - ragdollStartHipsPosition).normalized;
            ragdollStartRootPosition = transform.position;
            firstGroundContactAt = -1f;
            HasGroundContact = false;
            LastMaximumHorizontalDisplacement = 0f;
            LastMaximumHipsHeightGain = 0f;
            LastMaximumHipsDrop = 0f;
            LastMaximumHipsRotationDegrees = 0f;
            LastMaximumAirborneHipsRotationDegrees = 0f;
            LastMaximumAirborneTorsoTiltDegrees = 0f;
            LastMaximumHeadRise = 0f;
            LastMaximumAverageAngularSpeed = 0f;
            LastMaximumJointAnchorSeparation = 0f;
            LastMaximumRootTransformDisplacement = 0f;
            LastMaximumGroundPenetration = 0f;
            LastLandingHeadHeightAboveGround = 0f;
            LastLaunchDirection = Vector3.zero;
            launchTumbleAxis = Vector3.zero;
            LastLaunchStartedAtSeconds = Time.time - cycleStartedAt;
            phase = RuntimePhase.Ragdoll;
            if (applyLaunchImpulse) ApplyRandomLaunchImpulse();
            Physics.SyncTransforms();
        }

        private void ApplyRandomLaunchImpulse()
        {
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float forwardVariation = UnityEngine.Random.Range(-0.65f, 0.65f);
            Vector3 horizontalDirection =
                (transform.right * side + transform.forward * forwardVariation).normalized;
            float horizontalSpeed = UnityEngine.Random.Range(1.45f, 1.8f);
            float verticalSpeed = UnityEngine.Random.Range(2.45f, 2.8f);
            Vector3 blastOrigin = hipsBody.worldCenterOfMass - horizontalDirection * 0.85f - Vector3.up * 0.55f;
            float tumbleSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Vector3 tumbleAxis = transform.forward * tumbleSign;
            float tumbleSpeed = UnityEngine.Random.Range(1.5f, 1.9f);
            launchTumbleAxis = tumbleAxis;
            Vector3 tumbleCenter = RagdollCenterOfMass();
            Vector3 tumbleAngularVelocity = tumbleAxis * tumbleSpeed;
            foreach (Rigidbody body in bodies)
            {
                float variation = UnityEngine.Random.Range(0.92f, 1.08f);
                Vector3 travelVelocity =
                    horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed;
                body.AddForce(travelVelocity * body.mass * variation, ForceMode.Impulse);
                body.AddExplosionForce(
                    body.mass * UnityEngine.Random.Range(0.55f, 0.85f),
                    blastOrigin,
                    3.5f,
                    0.35f,
                    ForceMode.Impulse);
                Vector3 radius = body.worldCenterOfMass - tumbleCenter;
                body.AddForce(
                    Vector3.Cross(tumbleAngularVelocity, radius),
                    ForceMode.VelocityChange);
                body.AddTorque(tumbleAngularVelocity, ForceMode.VelocityChange);
                Vector3 spinAxis = UnityEngine.Random.onUnitSphere;
                float spinStrength = body == hipsBody
                    ? UnityEngine.Random.Range(0.34f, 0.48f)
                    : body.mass >= 1.2f
                        ? UnityEngine.Random.Range(0.11f, 0.19f)
                        : UnityEngine.Random.Range(0.025f, 0.07f);
                body.AddTorque(
                    spinAxis * body.mass * spinStrength,
                    ForceMode.Impulse);
            }
            LastLaunchDirection = horizontalDirection;
        }

        private void ApplyAirborneTumbleTorque()
        {
            Vector3 tumbleCenter = RagdollCenterOfMass();
            Vector3 angularAcceleration = launchTumbleAxis * 2f;
            foreach (Rigidbody body in bodies)
            {
                Vector3 radius = body.worldCenterOfMass - tumbleCenter;
                body.AddForce(
                    Vector3.Cross(angularAcceleration, radius),
                    ForceMode.Acceleration);
                body.AddTorque(angularAcceleration, ForceMode.Acceleration);
            }
        }

        private Vector3 RagdollCenterOfMass()
        {
            float totalMass = 0f;
            Vector3 weightedCenter = Vector3.zero;
            foreach (Rigidbody body in bodies)
            {
                totalMass += body.mass;
                weightedCenter += body.worldCenterOfMass * body.mass;
            }
            return totalMass > 0f ? weightedCenter / totalMass : hipsBody.worldCenterOfMass;
        }

        private float CurrentTorsoTiltDegrees()
        {
            Vector3 currentTorsoAxis = headBody.position - hipsBody.position;
            if (ragdollStartTorsoAxis.sqrMagnitude <= 0.000001f ||
                currentTorsoAxis.sqrMagnitude <= 0.000001f)
                return 0f;
            return Vector3.Angle(ragdollStartTorsoAxis, currentTorsoAxis.normalized);
        }

        private void BeginLandingHold()
        {
            foreach (Rigidbody body in bodies)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
                body.Sleep();
            }
            LastFlightDuration = Time.time - ragdollStartedAt;
            LastLandingHeadHeightAboveGround = Mathf.Max(
                0f, headBody.worldCenterOfMass.y - sharedGroundHeight);
            landingHoldStartedAt = Time.time;
            phase = RuntimePhase.LandingHold;
        }

        private bool HasSettledOnGround()
        {
            if (!HasGroundContact || firstGroundContactAt < 0f ||
                Time.time - firstGroundContactAt < MinimumContactSettleSeconds) return false;
            float totalLinearSpeed = 0f;
            float totalAngularSpeed = 0f;
            foreach (Rigidbody body in bodies)
            {
                totalLinearSpeed += body.linearVelocity.magnitude;
                totalAngularSpeed += body.angularVelocity.magnitude;
            }
            float inverseCount = bodies.Count > 0 ? 1f / bodies.Count : 0f;
            return headBody.worldCenterOfMass.y <= sharedGroundHeight + 0.55f &&
                   totalLinearSpeed * inverseCount <= 1.1f &&
                   totalAngularSpeed * inverseCount <= 3.5f;
        }

        private void UpdateRagdollMetrics()
        {
            Vector3 displacement = hipsBody.position - ragdollStartHipsPosition;
            LastMaximumHorizontalDisplacement = Mathf.Max(LastMaximumHorizontalDisplacement,
                new Vector2(displacement.x, displacement.z).magnitude);
            LastMaximumHipsHeightGain = Mathf.Max(LastMaximumHipsHeightGain, displacement.y);
            LastMaximumHipsDrop = Mathf.Max(LastMaximumHipsDrop, -displacement.y);
            LastMaximumHipsRotationDegrees = Mathf.Max(
                LastMaximumHipsRotationDegrees,
                Quaternion.Angle(ragdollStartHipsRotation, hipsBody.rotation));
            if (displacement.y >= 0.12f)
            {
                LastMaximumAirborneHipsRotationDegrees = Mathf.Max(
                    LastMaximumAirborneHipsRotationDegrees,
                    Quaternion.Angle(ragdollStartHipsRotation, hipsBody.rotation));
                LastMaximumAirborneTorsoTiltDegrees = Mathf.Max(
                    LastMaximumAirborneTorsoTiltDegrees,
                    CurrentTorsoTiltDegrees());
            }
            LastMaximumHeadRise = Mathf.Max(
                LastMaximumHeadRise, headBody.position.y - ragdollStartHeadPosition.y);
            LastMaximumRootTransformDisplacement = Mathf.Max(
                LastMaximumRootTransformDisplacement,
                Vector3.Distance(ragdollStartRootPosition, transform.position));
            bool contact = false;
            float totalAngularSpeed = 0f;
            int dynamicBodyCount = 0;
            foreach (Rigidbody body in bodies)
            {
                if (body.isKinematic) continue;
                totalAngularSpeed += body.angularVelocity.magnitude;
                dynamicBodyCount++;
            }
            if (dynamicBodyCount > 0)
                LastMaximumAverageAngularSpeed = Mathf.Max(
                    LastMaximumAverageAngularSpeed, totalAngularSpeed / dynamicBodyCount);
            foreach (CharacterJoint joint in joints)
            {
                Vector3 ownAnchor = joint.transform.TransformPoint(joint.anchor);
                Vector3 connectedAnchor = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
                LastMaximumJointAnchorSeparation = Mathf.Max(
                    LastMaximumJointAnchorSeparation,
                    Vector3.Distance(ownAnchor, connectedAnchor));
            }
            foreach (Collider collider in colliders)
            {
                float penetration = sharedGroundHeight - collider.bounds.min.y;
                LastMaximumGroundPenetration = Mathf.Max(LastMaximumGroundPenetration, penetration);
                if (collider.bounds.min.y <= sharedGroundHeight + GroundContactTolerance) contact = true;
            }
            if (contact && !HasGroundContact)
            {
                HasGroundContact = true;
                firstGroundContactAt = Time.time;
            }
        }

        private void ResetToAnimationStart(bool completedRagdoll)
        {
            float elapsed = completedRagdoll ? Time.time - ragdollStartedAt : 0f;
            float held = phase == RuntimePhase.LandingHold ? Time.time - landingHoldStartedAt : 0f;
            foreach (BoneBinding binding in bindings)
            {
                Rigidbody body = binding.Body;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.useGravity = false;
                body.isKinematic = true;
                body.Sleep();
                binding.Collider.enabled = false;
            }

            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            transform.localScale = initialLocalScale;
            animator.enabled = true;
            animator.Rebind();
            if (playbackMode == PlaybackMode.AnimationEnd)
            {
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
            }
            else
            {
                foreach (BoneBinding binding in bindings)
                {
                    binding.Bone.localPosition = binding.InitialLocalPosition;
                    binding.Bone.localRotation = binding.InitialLocalRotation;
                    binding.Bone.localScale = binding.InitialLocalScale;
                }
            }
            Physics.SyncTransforms();

            LastResetPositionError = Vector3.Distance(initialLocalPosition, transform.localPosition);
            LastResetRotationError = Quaternion.Angle(initialLocalRotation, transform.localRotation);
            if (completedRagdoll)
            {
                LastRagdollDuration = elapsed;
                LastLandingHoldDuration = held;
                CompletedCycleCount++;
            }
            phase = RuntimePhase.Animation;
            HasGroundContact = false;
            cycleStartedAt = Time.time;
        }

        private void BuildRuntimeRagdoll()
        {
            var bodyByPath = new Dictionary<string, Rigidbody>(StringComparer.Ordinal);
            var bindingByPath = new Dictionary<string, BoneBinding>(StringComparer.Ordinal);
            foreach (BoneDefinition definition in BoneDefinitions)
            {
                Transform bone = RequireTransform(definition.Path);
                Rigidbody body = bone.gameObject.AddComponent<Rigidbody>();
                ConfigureBody(body, definition.Mass);
                Collider collider = ConfigureCollider(bone, definition);
                collider.enabled = false;
                var binding = new BoneBinding
                {
                    Bone = bone,
                    Body = body,
                    Collider = collider,
                    InitialLocalPosition = bone.localPosition,
                    InitialLocalRotation = bone.localRotation,
                    InitialLocalScale = bone.localScale
                };
                bindings.Add(binding);
                bodies.Add(body);
                colliders.Add(collider);
                bodyByPath.Add(definition.Path, body);
                bindingByPath.Add(definition.Path, binding);
            }

            hipsBody = bodyByPath["Armature/Hips"];
            headBody = bodyByPath["Armature/Hips/Spine02/Spine01/Spine/neck/Head"];
            foreach (BoneDefinition definition in BoneDefinitions)
            {
                if (string.IsNullOrEmpty(definition.ConnectedPath)) continue;
                BoneBinding binding = bindingByPath[definition.Path];
                CharacterJoint joint = binding.Bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = bodyByPath[definition.ConnectedPath];
                joint.autoConfigureConnectedAnchor = true;
                joint.enableCollision = false;
                joint.enablePreprocessing = false;
                Vector3 towardParent = binding.Bone.InverseTransformDirection(
                    joint.connectedBody.worldCenterOfMass - binding.Bone.position).normalized;
                joint.axis = towardParent.sqrMagnitude > 0.0001f ? towardParent : Vector3.right;
                joint.swingAxis = Perpendicular(joint.axis);
                ConfigureJointForBone(joint, definition.Path);
                joints.Add(joint);
            }
            for (int first = 0; first < colliders.Count; first++)
            for (int second = first + 1; second < colliders.Count; second++)
                Physics.IgnoreCollision(colliders[first], colliders[second], true);
        }

        private void BuildRuntimeGround()
        {
            runtimeGround = new GameObject(name + "_DeathRagdollGround");
            runtimeGround.transform.position = new Vector3(transform.position.x,
                sharedGroundHeight - GroundThickness * 0.5f, transform.position.z);
            BoxCollider groundCollider = runtimeGround.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(GroundSize, GroundThickness, GroundSize);
        }

        private void IgnoreOtherDeathRagdollCollisions()
        {
            foreach (DeathRagdollLoop other in FindObjectsByType<DeathRagdollLoop>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other == this || other.colliders.Count == 0) continue;
                foreach (Collider ownCollider in colliders)
                foreach (Collider otherCollider in other.colliders)
                    Physics.IgnoreCollision(ownCollider, otherCollider, true);
            }
        }

        private void OnDestroy()
        {
            if (runtimeGround != null) Destroy(runtimeGround);
        }

        private void FitCollidersAboveSharedGround()
        {
            foreach (Collider collider in colliders)
            {
                float lift = sharedGroundHeight + GroundFitClearance - collider.bounds.min.y;
                if (lift <= 0f) continue;
                Transform colliderTransform = collider.transform;
                Vector3 localCenter = GetColliderCenter(collider);
                Vector3 worldCenter = colliderTransform.TransformPoint(localCenter) + Vector3.up * lift;
                SetColliderCenter(collider, colliderTransform.InverseTransformPoint(worldCenter));
            }
        }

        private static Vector3 GetColliderCenter(Collider collider)
        {
            if (collider is BoxCollider box) return box.center;
            if (collider is SphereCollider sphere) return sphere.center;
            if (collider is CapsuleCollider capsule) return capsule.center;
            return Vector3.zero;
        }

        private static void SetColliderCenter(Collider collider, Vector3 center)
        {
            if (collider is BoxCollider box) box.center = center;
            else if (collider is SphereCollider sphere) sphere.center = center;
            else if (collider is CapsuleCollider capsule) capsule.center = center;
        }

        private static void ConfigureBody(Rigidbody body, float mass)
        {
            body.mass = mass;
            body.linearDamping = 0.15f;
            body.angularDamping = 0.8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.maxAngularVelocity = 18f;
            body.useGravity = false;
            body.isKinematic = true;
        }

        private void ConfigureDynamicBodyForMode(Rigidbody body)
        {
            bool groundedDeath = playbackMode == PlaybackMode.AnimationEnd;
            body.linearDamping = groundedDeath ? 7.5f : 0.15f;
            body.angularDamping = groundedDeath
                ? 11f
                : body.mass >= 1.4f
                    ? 1.8f
                    : 3.4f;
            body.maxDepenetrationVelocity = groundedDeath ? 0.05f : 3f;
            body.solverIterations = groundedDeath ? 16 : 12;
            body.solverVelocityIterations = groundedDeath ? 8 : 4;
            body.maxAngularVelocity = groundedDeath ? 6f : 10f;
        }

        private static void ConfigureJointForBone(CharacterJoint joint, string path)
        {
            float lowTwist;
            float highTwist;
            float swing1;
            float swing2;
            float spring;
            float damper;

            if (path.EndsWith("/Head", StringComparison.Ordinal))
            {
                lowTwist = -16f; highTwist = 16f; swing1 = 20f; swing2 = 16f;
                spring = 130f; damper = 18f;
            }
            else if (path.EndsWith("/Spine02", StringComparison.Ordinal) ||
                     path.EndsWith("/Spine01", StringComparison.Ordinal) ||
                     path.EndsWith("/Spine", StringComparison.Ordinal))
            {
                lowTwist = -10f; highTwist = 10f; swing1 = 13f; swing2 = 13f;
                spring = 165f; damper = 22f;
            }
            else if (path.EndsWith("Arm", StringComparison.Ordinal) &&
                     !path.EndsWith("ForeArm", StringComparison.Ordinal))
            {
                lowTwist = -28f; highTwist = 28f; swing1 = 42f; swing2 = 34f;
                spring = 90f; damper = 15f;
            }
            else if (path.EndsWith("ForeArm", StringComparison.Ordinal))
            {
                lowTwist = -8f; highTwist = 8f; swing1 = 8f; swing2 = 52f;
                spring = 115f; damper = 18f;
            }
            else if (path.EndsWith("Hand", StringComparison.Ordinal))
            {
                lowTwist = -12f; highTwist = 12f; swing1 = 16f; swing2 = 16f;
                spring = 95f; damper = 16f;
            }
            else if (path.EndsWith("UpLeg", StringComparison.Ordinal))
            {
                lowTwist = -20f; highTwist = 20f; swing1 = 34f; swing2 = 28f;
                spring = 105f; damper = 17f;
            }
            else if (path.EndsWith("Leg", StringComparison.Ordinal))
            {
                lowTwist = -7f; highTwist = 7f; swing1 = 7f; swing2 = 48f;
                spring = 135f; damper = 20f;
            }
            else
            {
                lowTwist = -10f; highTwist = 10f; swing1 = 15f; swing2 = 15f;
                spring = 110f; damper = 18f;
            }

            joint.lowTwistLimit = Limit(lowTwist, 1f);
            joint.highTwistLimit = Limit(highTwist, 1f);
            joint.swing1Limit = Limit(swing1, 1f);
            joint.swing2Limit = Limit(swing2, 1f);
            joint.twistLimitSpring = new SoftJointLimitSpring { spring = spring, damper = damper };
            joint.swingLimitSpring = new SoftJointLimitSpring { spring = spring, damper = damper };
            joint.enableProjection = true;
            joint.projectionDistance = 0.025f;
            joint.projectionAngle = 7f;
            joint.massScale = 1f;
            joint.connectedMassScale = 1f;
        }

        private Collider ConfigureCollider(Transform proxy, BoneDefinition definition)
        {
            switch (definition.Shape)
            {
                case ColliderShape.Box:
                    BoxCollider box = proxy.gameObject.AddComponent<BoxCollider>();
                    box.size = definition.BoxSize;
                    return box;
                case ColliderShape.Sphere:
                    SphereCollider sphere = proxy.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = definition.SphereRadius;
                    return sphere;
                default:
                    Transform endpoint = RequireTransform(definition.EndpointPath);
                    Vector3 localEndpoint = proxy.InverseTransformPoint(endpoint.position);
                    float length = Mathf.Max(0.12f, localEndpoint.magnitude);
                    CapsuleCollider capsule = proxy.gameObject.AddComponent<CapsuleCollider>();
                    capsule.direction = LargestAxis(localEndpoint);
                    capsule.center = localEndpoint * 0.5f;
                    capsule.radius = Mathf.Clamp(length * 0.22f, 0.035f, 0.09f);
                    capsule.height = Mathf.Max(length, capsule.radius * 2f);
                    return capsule;
            }
        }

        private Transform RequireTransform(string path) =>
            transform.Find(path) ?? throw new InvalidOperationException(
                name + " DeathRagdollLoop is missing bone path " + path + ".");

        private static int LargestAxis(Vector3 value)
        {
            Vector3 absolute = new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z) return 0;
            return absolute.y >= absolute.z ? 1 : 2;
        }

        private static Vector3 Perpendicular(Vector3 axis)
        {
            Vector3 candidate = Vector3.Cross(axis, Vector3.up);
            if (candidate.sqrMagnitude < 0.0001f) candidate = Vector3.Cross(axis, Vector3.forward);
            return candidate.normalized;
        }

        private static SoftJointLimit Limit(float value, float contactDistance = 0f) =>
            new SoftJointLimit { limit = value, contactDistance = contactDistance };
        private static BoneDefinition Capsule(string path, string connectedPath, string endpointPath, float mass) =>
            new BoneDefinition(path, connectedPath, endpointPath, ColliderShape.Capsule, mass, Vector3.zero, 0f);
        private static BoneDefinition Box(string path, string connectedPath, float mass, Vector3 size) =>
            new BoneDefinition(path, connectedPath, null, ColliderShape.Box, mass, size, 0f);
        private static BoneDefinition Sphere(string path, string connectedPath, float mass, float radius) =>
            new BoneDefinition(path, connectedPath, null, ColliderShape.Sphere, mass, Vector3.zero, radius);
    }
}
