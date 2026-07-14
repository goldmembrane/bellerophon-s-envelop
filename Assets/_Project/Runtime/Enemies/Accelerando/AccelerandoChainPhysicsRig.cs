using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Enemies.Accelerando
{
    [DisallowMultipleComponent]
    public sealed class AccelerandoChainPhysicsRig : MonoBehaviour
    {
        private const string PhysicsRootName = "Accelerando_ChainPhysicsRoot";
        private const int DefaultVisibleLinkCount = 12;

        // Attack damping stays loose while the antenna is driving the chain, then rises only
        // during low-input holds so residual mace inertia settles instead of self-oscillating.
        private const float AttackAntennaMovingSpeedThreshold = 1.00f;
        private const float AttackMovingLinkLinearDamping = 0.55f;
        private const float AttackMovingLinkAngularDamping = 1.00f;
        private const float AttackMovingMaceLinearDamping = 0.30f;
        private const float AttackMovingMaceAngularDamping = 0.62f;
        private const float AttackStationaryDampingDelay = 0.18f;
        private const float AttackSettledLinkLinearDamping = 2.30f;
        private const float AttackSettledLinkAngularDamping = 1.70f;
        private const float AttackSettledMaceLinearDamping = 2.80f;
        private const float AttackSettledMaceAngularDamping = 1.70f;
        private const float AttackSettleDampingResponsePerSecond = 4.00f;

        [SerializeField]
        private int visibleLinkCount = DefaultVisibleLinkCount;

        [SerializeField]
        private float linkMass = 0.06f;

        [SerializeField]
        private float maceMass = 0.55f;

        [SerializeField]
        private float jointLimit = 0.028f;

        [SerializeField]
        private float linkColliderRadius = 0.035f;

        [SerializeField]
        private float maceColliderRadius = 0.145f;

        [SerializeField]
        private float chainRestSpring = 130f;

        [SerializeField]
        private float chainRestDamper = 7.2f;

        [SerializeField]
        private float maceRestSpring = 82f;

        [SerializeField]
        private float maceRestDamper = 5.4f;

        [SerializeField]
        private float crawlInertiaScale = 2.25f;

        // Attack-only hinge mode keeps every link segment at its authored length while rotations remain physical.
        [SerializeField]
        private bool lockLinearChainConnections;

        private readonly List<KinematicFollower> kinematicFollowers = new();
        private readonly List<DynamicFollower> dynamicFollowers = new();
        private readonly List<VisualFollower> visualFollowers = new();
        private readonly List<LockedChainSegment> lockedChainSegments = new();

        public int VisibleLinkCount => visibleLinkCount;
        public int DynamicFollowerCount => dynamicFollowers.Count;

        public void Configure(int configuredVisibleLinkCount)
        {
            ConfigureInternal(
                configuredVisibleLinkCount,
                configuredLinkMass: 0.06f,
                configuredMaceMass: 0.55f,
                configuredJointLimit: 0.028f,
                configuredChainRestSpring: 130f,
                configuredChainRestDamper: 7.2f,
                configuredMaceRestSpring: 82f,
                configuredMaceRestDamper: 5.4f,
                configuredInertiaScale: 2.25f,
                configuredLockLinearChainConnections: false);
        }

        public void ConfigureAttackStrike(int configuredVisibleLinkCount)
        {
            ConfigureInternal(
                configuredVisibleLinkCount,
                configuredLinkMass: 0.050f,
                configuredMaceMass: 0.78f,
                configuredJointLimit: 0.058f,
                configuredChainRestSpring: 0f,
                configuredChainRestDamper: 0f,
                configuredMaceRestSpring: 0f,
                configuredMaceRestDamper: 0f,
                configuredInertiaScale: 0f,
                configuredLockLinearChainConnections: true);
        }

        private void ConfigureInternal(
            int configuredVisibleLinkCount,
            float configuredLinkMass,
            float configuredMaceMass,
            float configuredJointLimit,
            float configuredChainRestSpring,
            float configuredChainRestDamper,
            float configuredMaceRestSpring,
            float configuredMaceRestDamper,
            float configuredInertiaScale,
            bool configuredLockLinearChainConnections)
        {
            visibleLinkCount = Mathf.Max(2, configuredVisibleLinkCount);
            linkMass = configuredLinkMass;
            maceMass = configuredMaceMass;
            jointLimit = configuredJointLimit;
            chainRestSpring = configuredChainRestSpring;
            chainRestDamper = configuredChainRestDamper;
            maceRestSpring = configuredMaceRestSpring;
            maceRestDamper = configuredMaceRestDamper;
            crawlInertiaScale = configuredInertiaScale;
            lockLinearChainConnections = configuredLockLinearChainConnections;
            Rebuild();
        }

        public void Rebuild()
        {
            kinematicFollowers.Clear();
            dynamicFollowers.Clear();
            visualFollowers.Clear();
            lockedChainSegments.Clear();

            var physicsRoot = GetOrCreatePhysicsRoot();
            ClearGeneratedProxyChildren(physicsRoot);
            ConfigureSide("Left", physicsRoot);
            ConfigureSide("Right", physicsRoot);
            ApplyVisualFollowers();
        }

        public void SimulatePhysicsTick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateKinematicFollowers(deltaTime);
            ApplyAttackStationaryDamping(deltaTime);
            ApplyRestForces(deltaTime);
        }

        public void SyncVisualsToPhysics()
        {
            ProjectLockedChainSegments();
            ApplyVisualFollowers();
        }

        private void Awake()
        {
            Rebuild();
        }

        private void OnEnable()
        {
            Rebuild();
        }

        private void FixedUpdate()
        {
            SimulatePhysicsTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            ProjectLockedChainSegments();
            ApplyVisualFollowers();
        }

        private void ConfigureSide(string sideName, Transform physicsRoot)
        {
            var antennaTip = FindOptionalChild($"Accelerando_{sideName}_AntennaPhysicsAnchor") ??
                FindRequiredChild($"Accelerando_{sideName}_AntennaTip_Ring");
            var previousBody = ConfigureKinematicProxy(
                physicsRoot,
                ChainProxyName(sideName, 1),
                antennaTip,
                FindRequiredChild($"Accelerando_{sideName}_ConnectedChain_Link_01"));

            for (var linkIndex = 2; linkIndex <= visibleLinkCount; linkIndex++)
            {
                var link = FindRequiredChild($"Accelerando_{sideName}_ConnectedChain_Link_{linkIndex:00}");
                var linkBody = ConfigureDynamicProxy(
                    physicsRoot,
                    ChainProxyName(sideName, linkIndex),
                    antennaTip,
                    link,
                    linkMass,
                    linkColliderRadius,
                    chainRestSpring,
                    chainRestDamper,
                    isMace: false);
                ConfigureJoint(linkBody, previousBody);
                RegisterLockedChainSegment(previousBody, linkBody);
                previousBody = linkBody;
            }

            var maceSocket = FindOptionalChild($"Accelerando_{sideName}_MaceSocket_Ring");
            var maceAnchor = FindOptionalChild($"Accelerando_{sideName}_MacePhysicsAnchor");
            var maceHead = FindOptionalChild($"Accelerando_{sideName}_MaceHead");
            var maceVisual = maceHead != null ? maceHead : maceSocket != null ? maceSocket : maceAnchor;
            if (maceVisual == null)
            {
                throw new InvalidOperationException($"Accelerando_{sideName}_MaceHead or hidden mace physics anchor is missing under {name}.");
            }

            var maceBody = ConfigureDynamicProxy(
                physicsRoot,
                MaceProxyName(sideName),
                antennaTip,
                maceVisual,
                maceMass,
                maceColliderRadius,
                maceRestSpring,
                maceRestDamper,
                isMace: true);
            if (maceHead != null && maceSocket != null)
            {
                var socketRotationOffset = Quaternion.Inverse(maceBody.rotation) * maceSocket.rotation;
                visualFollowers.Add(new VisualFollower(maceSocket, maceBody, socketRotationOffset));
            }

            if (maceAnchor != null && maceAnchor != maceVisual)
            {
                var anchorRotationOffset = Quaternion.Inverse(maceBody.rotation) * maceAnchor.rotation;
                visualFollowers.Add(new VisualFollower(maceAnchor, maceBody, anchorRotationOffset));
            }

            ConfigureJoint(maceBody, previousBody);
            RegisterLockedChainSegment(previousBody, maceBody);
        }

        private void RegisterLockedChainSegment(Rigidbody connectedBody, Rigidbody body)
        {
            if (!lockLinearChainConnections)
            {
                return;
            }

            lockedChainSegments.Add(new LockedChainSegment(
                connectedBody,
                body,
                Vector3.Distance(connectedBody.position, body.position),
                (body.position - connectedBody.position).normalized));
        }

        // Unity's joint solver may briefly stretch a fast kinematic chain. This attack-only
        // post-solver projection removes radial separation while preserving tangential swing.
        private void ProjectLockedChainSegments()
        {
            if (!lockLinearChainConnections)
            {
                return;
            }

            for (var i = 0; i < lockedChainSegments.Count; i++)
            {
                var segment = lockedChainSegments[i];
                if (segment.ConnectedBody == null || segment.Body == null)
                {
                    continue;
                }

                var offset = segment.Body.position - segment.ConnectedBody.position;
                var direction = offset.sqrMagnitude > 0.0000001f
                    ? offset.normalized
                    : segment.FallbackDirection;
                segment.Body.position =
                    segment.ConnectedBody.position + direction * segment.RestDistance;
                var relativeVelocity = segment.Body.linearVelocity - segment.ConnectedBody.linearVelocity;
                segment.Body.linearVelocity =
                    segment.ConnectedBody.linearVelocity + Vector3.ProjectOnPlane(relativeVelocity, direction);
            }
        }

        private Rigidbody ConfigureKinematicProxy(Transform physicsRoot, string proxyName, Transform target, Transform visual)
        {
            var rotationOffset = Quaternion.Inverse(target.rotation) * visual.rotation;
            var body = CreateProxyBody(physicsRoot, proxyName, visual, linkMass, linkColliderRadius, isKinematic: true);
            body.useGravity = false;
            body.linearDamping = 0.45f;
            body.angularDamping = 0.9f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.position = target.position;
            body.rotation = target.rotation * rotationOffset;

            kinematicFollowers.Add(new KinematicFollower(target, body, rotationOffset));
            visualFollowers.Add(new VisualFollower(visual, body));
            return body;
        }

        private Rigidbody ConfigureDynamicProxy(
            Transform physicsRoot,
            string proxyName,
            Transform anchor,
            Transform visual,
            float mass,
            float colliderRadius,
            float restSpring,
            float restDamper,
            bool isMace)
        {
            var body = CreateProxyBody(physicsRoot, proxyName, visual, mass, colliderRadius, isKinematic: false);
            body.useGravity = true;
            body.linearDamping = lockLinearChainConnections
                ? isMace ? AttackMovingMaceLinearDamping : AttackMovingLinkLinearDamping
                : 0.65f;
            body.angularDamping = lockLinearChainConnections
                ? isMace ? AttackMovingMaceAngularDamping : AttackMovingLinkAngularDamping
                : 1.2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = lockLinearChainConnections
                ? isMace ? 18f : 24f
                : 18f;
            body.constraints = lockLinearChainConnections
                ? RigidbodyConstraints.None
                : RigidbodyConstraints.FreezeRotation;
            if (lockLinearChainConnections)
            {
                body.solverIterations = 24;
                body.solverVelocityIterations = 12;
            }

            var anchorLocalOffset = Quaternion.Inverse(anchor.rotation) * (visual.position - anchor.position);
            var visualRotationOffset = Quaternion.Inverse(body.rotation) * visual.rotation;
            dynamicFollowers.Add(new DynamicFollower(
                anchor,
                body,
                anchorLocalOffset,
                transform.InverseTransformPoint(anchor.position),
                restSpring,
                restDamper,
                isMace));
            visualFollowers.Add(new VisualFollower(visual, body, visualRotationOffset));
            return body;
        }

        private Rigidbody CreateProxyBody(
            Transform physicsRoot,
            string proxyName,
            Transform visual,
            float mass,
            float colliderRadius,
            bool isKinematic)
        {
            RemoveVisualPhysicsComponents(visual);

            var proxyObject = new GameObject(proxyName);
            var proxyTransform = proxyObject.transform;
            proxyTransform.SetParent(physicsRoot, false);
            proxyTransform.position = visual.position;
            proxyTransform.rotation = visual.rotation;
            proxyTransform.localScale = Vector3.one;

            var body = proxyObject.AddComponent<Rigidbody>();
            body.isKinematic = isKinematic;
            body.detectCollisions = true;
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var collider = proxyObject.AddComponent<SphereCollider>();
            collider.radius = EstimateWorldColliderRadius(visual, colliderRadius);
            collider.center = Vector3.zero;

            return body;
        }

        private void ConfigureJoint(Rigidbody body, Rigidbody connectedBody)
        {
            var joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(body.position);
            joint.xMotion = lockLinearChainConnections
                ? ConfigurableJointMotion.Locked
                : ConfigurableJointMotion.Limited;
            joint.yMotion = joint.xMotion;
            joint.zMotion = joint.xMotion;
            joint.angularXMotion = lockLinearChainConnections
                ? ConfigurableJointMotion.Free
                : ConfigurableJointMotion.Locked;
            joint.angularYMotion = joint.angularXMotion;
            joint.angularZMotion = joint.angularXMotion;
            joint.linearLimit = new SoftJointLimit
            {
                limit = jointLimit,
                contactDistance = jointLimit * 0.5f
            };
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = lockLinearChainConnections ? 0.002f : jointLimit * 1.5f;
            joint.projectionAngle = 6f;
            joint.enableCollision = false;
            joint.enablePreprocessing = true;
        }

        private void UpdateKinematicFollowers(float deltaTime)
        {
            for (var i = 0; i < kinematicFollowers.Count; i++)
            {
                var follower = kinematicFollowers[i];
                if (follower.Target == null || follower.Body == null)
                {
                    continue;
                }

                var currentPosition = follower.Body.position;
                var nextPosition = follower.Target.position;
                follower.Body.MovePosition(nextPosition);
                follower.Body.MoveRotation(follower.Target.rotation * follower.RotationOffset);
                kinematicFollowers[i] = follower.WithVelocity(
                    (nextPosition - currentPosition) / deltaTime,
                    deltaTime);
            }
        }

        private void ApplyAttackStationaryDamping(float deltaTime)
        {
            if (!lockLinearChainConnections)
            {
                return;
            }

            for (var i = 0; i < dynamicFollowers.Count; i++)
            {
                var follower = dynamicFollowers[i];
                if (follower.Body == null)
                {
                    continue;
                }

                // Every follower stores its own left/right antenna tip as Anchor, so one side's
                // small residual input cannot release the other side's stationary damping.
                var antennaIsMoving = GetAnchorStationaryDuration(follower.Anchor) <
                    AttackStationaryDampingDelay;
                var movingLinearDamping = follower.IsMace
                    ? AttackMovingMaceLinearDamping
                    : AttackMovingLinkLinearDamping;
                var movingAngularDamping = follower.IsMace
                    ? AttackMovingMaceAngularDamping
                    : AttackMovingLinkAngularDamping;
                if (antennaIsMoving)
                {
                    // Release the settling resistance immediately when the next antenna drive begins.
                    follower.Body.linearDamping = movingLinearDamping;
                    follower.Body.angularDamping = movingAngularDamping;
                    continue;
                }

                var settledLinearDamping = follower.IsMace
                    ? AttackSettledMaceLinearDamping
                    : AttackSettledLinkLinearDamping;
                var settledAngularDamping = follower.IsMace
                    ? AttackSettledMaceAngularDamping
                    : AttackSettledLinkAngularDamping;
                var dampingStep = AttackSettleDampingResponsePerSecond * deltaTime;
                follower.Body.linearDamping = Mathf.MoveTowards(
                    follower.Body.linearDamping,
                    settledLinearDamping,
                    dampingStep);
                follower.Body.angularDamping = Mathf.MoveTowards(
                    follower.Body.angularDamping,
                    settledAngularDamping,
                    dampingStep);
            }
        }

        private void ApplyRestForces(float deltaTime)
        {
            // During the attack, the animated antenna may only pull the first kinematic link.
            // The connected joints, link inertia, and mace mass must transmit the motion down
            // the chain; applying forces to every follower would make the mace self-propelled.
            if (lockLinearChainConnections)
            {
                return;
            }

            for (var i = 0; i < dynamicFollowers.Count; i++)
            {
                var follower = dynamicFollowers[i];
                if (follower.Anchor == null || follower.Body == null)
                {
                    continue;
                }

                var restPosition = follower.Anchor.position + follower.Anchor.rotation * follower.AnchorLocalOffset;
                var springAcceleration = (restPosition - follower.Body.position) * follower.RestSpring;
                var dampingAcceleration = -follower.Body.linearVelocity * follower.RestDamper;
                var anchorVelocity = GetAnchorVelocity(follower.Anchor);
                var inertiaAcceleration = -anchorVelocity * crawlInertiaScale;

                follower.Body.AddForce(
                    springAcceleration + dampingAcceleration + inertiaAcceleration,
                    ForceMode.Acceleration);
            }
        }

        private Vector3 GetAnchorVelocity(Transform anchor)
        {
            for (var i = 0; i < kinematicFollowers.Count; i++)
            {
                if (kinematicFollowers[i].Target == anchor)
                {
                    return kinematicFollowers[i].Velocity;
                }
            }

            return Vector3.zero;
        }

        private float GetAnchorStationaryDuration(Transform anchor)
        {
            for (var i = 0; i < kinematicFollowers.Count; i++)
            {
                if (kinematicFollowers[i].Target == anchor)
                {
                    return kinematicFollowers[i].StationaryDuration;
                }
            }

            return 0f;
        }

        private Transform GetOrCreatePhysicsRoot()
        {
            var root = transform.Find(PhysicsRootName);
            if (root != null)
            {
                return root;
            }

            var rootObject = new GameObject(PhysicsRootName);
            root = rootObject.transform;
            root.SetParent(transform, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            return root;
        }

        private void ClearGeneratedProxyChildren(Transform physicsRoot)
        {
            for (var i = physicsRoot.childCount - 1; i >= 0; i--)
            {
                DestroyComponentOrObject(physicsRoot.GetChild(i).gameObject);
            }
        }

        private void ApplyVisualFollowers()
        {
            for (var i = 0; i < visualFollowers.Count; i++)
            {
                var follower = visualFollowers[i];
                if (follower.Visual == null || follower.Driver == null)
                {
                    continue;
                }

                var driverPosition = follower.DriverBody != null
                    ? follower.DriverBody.position
                    : follower.Driver.position;
                var driverRotation = follower.DriverBody != null
                    ? follower.DriverBody.rotation
                    : follower.Driver.rotation;
                follower.Visual.position = driverPosition;
                follower.Visual.rotation = driverRotation * follower.RotationOffset;
            }
        }

        private void RemoveVisualPhysicsComponents(Transform visual)
        {
            var joints = visual.GetComponents<ConfigurableJoint>();
            for (var i = joints.Length - 1; i >= 0; i--)
            {
                DestroyComponentOrObject(joints[i]);
            }

            var colliders = visual.GetComponents<Collider>();
            for (var i = colliders.Length - 1; i >= 0; i--)
            {
                DestroyComponentOrObject(colliders[i]);
            }

            var body = visual.GetComponent<Rigidbody>();
            if (body != null)
            {
                DestroyComponentOrObject(body);
            }
        }

        private float EstimateWorldColliderRadius(Transform visual, float fallbackRadius)
        {
            var renderer = visual.GetComponent<Renderer>();
            if (renderer == null)
            {
                return fallbackRadius;
            }

            var worldRadius = Mathf.Max(
                renderer.bounds.extents.x,
                renderer.bounds.extents.y,
                renderer.bounds.extents.z) * 0.72f;
            return Mathf.Clamp(worldRadius, fallbackRadius * 0.65f, fallbackRadius * 2.25f);
        }

        private void DestroyComponentOrObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Transform FindRequiredChild(string childName)
        {
            var child = FindOptionalChild(childName);
            if (child != null)
            {
                return child;
            }

            throw new InvalidOperationException($"{childName} is missing under {name}.");
        }

        private Transform FindOptionalChild(string childName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static string ChainProxyName(string sideName, int linkIndex)
        {
            return $"Accelerando_{sideName}_ChainPhysics_Link_{linkIndex:00}";
        }

        private static string MaceProxyName(string sideName)
        {
            return $"Accelerando_{sideName}_ChainPhysics_MaceSocket";
        }

        private readonly struct KinematicFollower
        {
            public KinematicFollower(Transform target, Rigidbody body, Quaternion rotationOffset)
                : this(target, body, rotationOffset, Vector3.zero, 0f)
            {
            }

            private KinematicFollower(
                Transform target,
                Rigidbody body,
                Quaternion rotationOffset,
                Vector3 velocity,
                float stationaryDuration)
            {
                Target = target;
                Body = body;
                RotationOffset = rotationOffset;
                Velocity = velocity;
                StationaryDuration = stationaryDuration;
            }

            public Transform Target { get; }
            public Rigidbody Body { get; }
            public Quaternion RotationOffset { get; }
            public Vector3 Velocity { get; }
            public float StationaryDuration { get; }

            public KinematicFollower WithVelocity(Vector3 velocity, float deltaTime)
            {
                var nextStationaryDuration = velocity.magnitude <= AttackAntennaMovingSpeedThreshold
                    ? StationaryDuration + deltaTime
                    : 0f;
                return new KinematicFollower(
                    Target,
                    Body,
                    RotationOffset,
                    velocity,
                    nextStationaryDuration);
            }
        }

        private readonly struct DynamicFollower
        {
            public DynamicFollower(
                Transform anchor,
                Rigidbody body,
                Vector3 anchorLocalOffset,
                Vector3 initialAnchorLocalPosition,
                float restSpring,
                float restDamper,
                bool isMace)
            {
                Anchor = anchor;
                Body = body;
                AnchorLocalOffset = anchorLocalOffset;
                InitialAnchorLocalPosition = initialAnchorLocalPosition;
                RestSpring = restSpring;
                RestDamper = restDamper;
                IsMace = isMace;
            }

            public Transform Anchor { get; }
            public Rigidbody Body { get; }
            public Vector3 AnchorLocalOffset { get; }
            public Vector3 InitialAnchorLocalPosition { get; }
            public float RestSpring { get; }
            public float RestDamper { get; }
            public bool IsMace { get; }
        }

        private readonly struct VisualFollower
        {
            public VisualFollower(Transform visual, Rigidbody driverBody)
                : this(visual, driverBody, Quaternion.identity)
            {
            }

            public VisualFollower(Transform visual, Rigidbody driverBody, Quaternion rotationOffset)
            {
                Visual = visual;
                DriverBody = driverBody;
                Driver = driverBody != null ? driverBody.transform : null;
                RotationOffset = rotationOffset;
            }

            public Transform Visual { get; }
            public Rigidbody DriverBody { get; }
            public Transform Driver { get; }
            public Quaternion RotationOffset { get; }
        }

        private readonly struct LockedChainSegment
        {
            public LockedChainSegment(
                Rigidbody connectedBody,
                Rigidbody body,
                float restDistance,
                Vector3 fallbackDirection)
            {
                ConnectedBody = connectedBody;
                Body = body;
                RestDistance = restDistance;
                FallbackDirection = fallbackDirection;
            }

            public Rigidbody ConnectedBody { get; }
            public Rigidbody Body { get; }
            public float RestDistance { get; }
            public Vector3 FallbackDirection { get; }
        }
    }
}
