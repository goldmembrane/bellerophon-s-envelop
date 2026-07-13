using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Enemies.Accelerando
{
    [DisallowMultipleComponent]
    public sealed class AccelerandoChainPhysicsRig : MonoBehaviour
    {
        private const string PhysicsRootName = "Accelerando_ChainPhysicsRoot";
        private const int DefaultVisibleLinkCount = 8;

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

        private readonly List<KinematicFollower> kinematicFollowers = new();
        private readonly List<DynamicFollower> dynamicFollowers = new();
        private readonly List<VisualFollower> visualFollowers = new();

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
                configuredInertiaScale: 2.25f);
        }

        public void ConfigureAttackStrike(int configuredVisibleLinkCount)
        {
            ConfigureInternal(
                configuredVisibleLinkCount,
                configuredLinkMass: 0.055f,
                configuredMaceMass: 0.68f,
                configuredJointLimit: 0.058f,
                configuredChainRestSpring: 72f,
                configuredChainRestDamper: 4.1f,
                configuredMaceRestSpring: 30f,
                configuredMaceRestDamper: 2.6f,
                configuredInertiaScale: 7.2f);
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
            float configuredInertiaScale)
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
            Rebuild();
        }

        public void Rebuild()
        {
            kinematicFollowers.Clear();
            dynamicFollowers.Clear();
            visualFollowers.Clear();

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
            ApplyRestForces(deltaTime);
        }

        public void SyncVisualsToPhysics()
        {
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
            ApplyVisualFollowers();
        }

        private void ConfigureSide(string sideName, Transform physicsRoot)
        {
            var antennaTip = FindRequiredChild($"Accelerando_{sideName}_AntennaTip_Ring");
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
                    chainRestDamper);
                ConfigureJoint(linkBody, previousBody);
                previousBody = linkBody;
            }

            var mace = FindRequiredChild($"Accelerando_{sideName}_MaceSocket_Ring");
            var maceBody = ConfigureDynamicProxy(
                physicsRoot,
                MaceProxyName(sideName),
                antennaTip,
                mace,
                maceMass,
                maceColliderRadius,
                maceRestSpring,
                maceRestDamper);
            ConfigureJoint(maceBody, previousBody);
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
            visualFollowers.Add(new VisualFollower(visual, body.transform));
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
            float restDamper)
        {
            var body = CreateProxyBody(physicsRoot, proxyName, visual, mass, colliderRadius, isKinematic: false);
            body.useGravity = true;
            body.linearDamping = 0.65f;
            body.angularDamping = 1.2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 18f;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            var anchorLocalOffset = Quaternion.Inverse(anchor.rotation) * (visual.position - anchor.position);
            var visualRotationOffset = Quaternion.Inverse(body.rotation) * visual.rotation;
            dynamicFollowers.Add(new DynamicFollower(anchor, body, anchorLocalOffset, restSpring, restDamper));
            visualFollowers.Add(new VisualFollower(visual, body.transform, visualRotationOffset));
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
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.linearLimit = new SoftJointLimit
            {
                limit = jointLimit,
                contactDistance = jointLimit * 0.5f
            };
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = jointLimit * 1.5f;
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
                kinematicFollowers[i] = follower.WithVelocity((nextPosition - currentPosition) / deltaTime);
            }
        }

        private void ApplyRestForces(float deltaTime)
        {
            var anchorVelocity = GetAverageAnchorVelocity();
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
                var inertiaAcceleration = -anchorVelocity * crawlInertiaScale;
                follower.Body.AddForce(springAcceleration + dampingAcceleration + inertiaAcceleration, ForceMode.Acceleration);
            }
        }

        private Vector3 GetAverageAnchorVelocity()
        {
            if (kinematicFollowers.Count == 0)
            {
                return Vector3.zero;
            }

            var velocity = Vector3.zero;
            for (var i = 0; i < kinematicFollowers.Count; i++)
            {
                velocity += kinematicFollowers[i].Velocity;
            }

            return velocity / kinematicFollowers.Count;
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

                follower.Visual.position = follower.Driver.position;
                follower.Visual.rotation = follower.Driver.rotation * follower.RotationOffset;
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
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            throw new InvalidOperationException($"{childName} is missing under {name}.");
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
                : this(target, body, rotationOffset, Vector3.zero)
            {
            }

            private KinematicFollower(Transform target, Rigidbody body, Quaternion rotationOffset, Vector3 velocity)
            {
                Target = target;
                Body = body;
                RotationOffset = rotationOffset;
                Velocity = velocity;
            }

            public Transform Target { get; }
            public Rigidbody Body { get; }
            public Quaternion RotationOffset { get; }
            public Vector3 Velocity { get; }

            public KinematicFollower WithVelocity(Vector3 velocity)
            {
                return new KinematicFollower(Target, Body, RotationOffset, velocity);
            }
        }

        private readonly struct DynamicFollower
        {
            public DynamicFollower(
                Transform anchor,
                Rigidbody body,
                Vector3 anchorLocalOffset,
                float restSpring,
                float restDamper)
            {
                Anchor = anchor;
                Body = body;
                AnchorLocalOffset = anchorLocalOffset;
                RestSpring = restSpring;
                RestDamper = restDamper;
            }

            public Transform Anchor { get; }
            public Rigidbody Body { get; }
            public Vector3 AnchorLocalOffset { get; }
            public float RestSpring { get; }
            public float RestDamper { get; }
        }

        private readonly struct VisualFollower
        {
            public VisualFollower(Transform visual, Transform driver)
                : this(visual, driver, Quaternion.identity)
            {
            }

            public VisualFollower(Transform visual, Transform driver, Quaternion rotationOffset)
            {
                Visual = visual;
                Driver = driver;
                RotationOffset = rotationOffset;
            }

            public Transform Visual { get; }
            public Transform Driver { get; }
            public Quaternion RotationOffset { get; }
        }
    }
}
