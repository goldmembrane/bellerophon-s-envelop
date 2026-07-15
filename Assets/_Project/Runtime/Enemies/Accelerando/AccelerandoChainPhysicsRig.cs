using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Enemies.Accelerando
{
    [DisallowMultipleComponent]
    public sealed class AccelerandoChainPhysicsRig : MonoBehaviour
    {
        private const string PhysicsRootName = "Accelerando_ChainPhysicsRoot";
        private const string AttackTorsoCollisionProxyName = "Accelerando_AttackTorsoCollisionProxy";
        private const int DefaultVisibleLinkCount = 12;

        // Attack bodies use one continuous PhysX damping model. Switching damping from an
        // animation-derived antenna-speed threshold can freeze a mace behind a slowly moving
        // pivot, so settling is left to joint impulses, constant drag, and Rigidbody sleeping.
        private const float AttackLinkLinearDamping = 0.18f;
        private const float AttackLinkAngularDamping = 0.35f;
        private const float AttackMaceLinearDamping = 0.06f;
        private const float AttackMaceAngularDamping = 0.18f;
        // Unity mass does not change gravitational acceleration. A reduced attack gravity scale
        // is the physical equivalent of the requested lighter flail weight while the antenna
        // joint tension remains the only circular drive.
        private const float AttackGravityScale = 0.15f;
        private const float AttackAntennaCircularDriveRadius = 0.24f;
        private const float AttackBodySleepThreshold = 0.015f;
        // Emergency solver recovery only; normal antenna-driven motion stays joint-simulated.
        private const float AttackJointProjectionDistance = 0.002f;
        // The final joint sees a much heavier mace than one link. Solver-side inverse-mass
        // scaling keeps that connection continuous without changing Rigidbody gravity/inertia.
        private const float AttackMaceJointMassScale = 4.00f;
        // Links 02-11 form a joint-constrained rotating handle. Keeping only link 12 as the
        // flexible physical tail prevents the tether from folding instead of orbiting.
        private const int AttackDrivenHandleLastLinkIndex = 11;
        private const float AttackTailAngularLimit = 25f;
        private const float HitLinkLinearDamping = 0.24f;
        private const float HitLinkAngularDamping = 0.55f;
        private const float HitMaceLinearDamping = 0.10f;
        private const float HitMaceAngularDamping = 0.28f;
        private const float DeathLinkLinearDamping = 1.20f;
        private const float DeathLinkAngularDamping = 1.60f;
        private const float DeathMaceLinearDamping = 1.40f;
        private const float DeathMaceAngularDamping = 2.00f;
        private const float DeathLinkMaximumLinearVelocity = 2.00f;
        private const float DeathMaceMaximumLinearVelocity = 1.50f;
        // A looping review clip returns Bone_000 from the collapsed pose to neutral in one frame.
        // Limiting only the death kinematic attachment speed prevents that authored reset from
        // injecting an unbounded joint impulse while every dynamic link keeps its physical state.
        private const float DeathKinematicFollowerMaximumSpeed = 3.00f;

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

        // Attack-only tether mode keeps each point-mass link within its authored maximum
        // distance while allowing the chain to go slack and rotate freely.
        [SerializeField]
        private bool lockLinearChainConnections;

        // Hit recoil keeps the gameplay slot fixed. Bone_000 supplies the animated body impulse,
        // while this profile transfers that displacement to non-skinned antenna anchors and lets
        // the Rigidbody chain and mace lag behind through inertia instead of animation curves.
        [SerializeField]
        private bool hitRecoilMode;

        // Death collapse uses the same body-to-anchor displacement bridge as hit recoil, but
        // higher damping lets the gravity-driven chain and mace settle into a final limp pose.
        [SerializeField]
        private bool deathCollapseMode;

        private readonly List<KinematicFollower> kinematicFollowers = new();
        private readonly List<DynamicFollower> dynamicFollowers = new();
        private readonly List<VisualFollower> visualFollowers = new();
        private readonly List<Collider> physicsColliders = new();
        private Transform bodyMotionRoot;
        private Vector3 bodyMotionInitialRootPosition;

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
                configuredLockLinearChainConnections: false,
                configuredHitRecoilMode: false,
                configuredDeathCollapseMode: false);
        }

        public void ConfigureAttackStrike(int configuredVisibleLinkCount)
        {
            ConfigureInternal(
                configuredVisibleLinkCount,
                configuredLinkMass: 0.040f,
                configuredMaceMass: 0.16f,
                configuredJointLimit: 0.058f,
                configuredChainRestSpring: 0f,
                configuredChainRestDamper: 0f,
                configuredMaceRestSpring: 0f,
                configuredMaceRestDamper: 0f,
                configuredInertiaScale: 0f,
                configuredLockLinearChainConnections: true,
                configuredHitRecoilMode: false,
                configuredDeathCollapseMode: false);
        }

        public void ConfigureHitRecoil(int configuredVisibleLinkCount)
        {
            ConfigureInternal(
                configuredVisibleLinkCount,
                configuredLinkMass: 0.05f,
                configuredMaceMass: 0.34f,
                configuredJointLimit: 0.032f,
                configuredChainRestSpring: 0f,
                configuredChainRestDamper: 0f,
                configuredMaceRestSpring: 0f,
                configuredMaceRestDamper: 0f,
                configuredInertiaScale: 0f,
                configuredLockLinearChainConnections: false,
                configuredHitRecoilMode: true,
                configuredDeathCollapseMode: false);
        }

        public void ConfigureDeathCollapse(int configuredVisibleLinkCount)
        {
            ConfigureInternal(
                configuredVisibleLinkCount,
                configuredLinkMass: 0.05f,
                configuredMaceMass: 0.42f,
                configuredJointLimit: 0.032f,
                configuredChainRestSpring: 0f,
                configuredChainRestDamper: 0f,
                configuredMaceRestSpring: 0f,
                configuredMaceRestDamper: 0f,
                configuredInertiaScale: 0f,
                configuredLockLinearChainConnections: false,
                configuredHitRecoilMode: false,
                configuredDeathCollapseMode: true);
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
            bool configuredLockLinearChainConnections,
            bool configuredHitRecoilMode,
            bool configuredDeathCollapseMode)
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
            hitRecoilMode = configuredHitRecoilMode;
            deathCollapseMode = configuredDeathCollapseMode;
            Rebuild();
        }

        public void Rebuild()
        {
            kinematicFollowers.Clear();
            dynamicFollowers.Clear();
            visualFollowers.Clear();
            physicsColliders.Clear();
            bodyMotionRoot = hitRecoilMode || deathCollapseMode ? FindRequiredChild("Bone_000") : null;
            bodyMotionInitialRootPosition = bodyMotionRoot != null ? bodyMotionRoot.position : Vector3.zero;

            var physicsRoot = GetOrCreatePhysicsRoot();
            ClearGeneratedProxyChildren(physicsRoot);
            if (lockLinearChainConnections || hitRecoilMode || deathCollapseMode)
            {
                ConfigureAttackTorsoCollisionProxy(physicsRoot);
            }

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
            var antennaTip = FindOptionalChild($"Accelerando_{sideName}_AntennaPhysicsAnchor") ??
                FindRequiredChild($"Accelerando_{sideName}_AntennaTip_Ring");
            var kinematicTarget = antennaTip;
            var kinematicPositionOffset = Vector3.zero;
            if (deathCollapseMode)
            {
                var terminalBoneName = string.Equals(sideName, "Left", StringComparison.Ordinal)
                    ? "Bone_009"
                    : "Bone_006";
                kinematicTarget = FindRequiredChild(terminalBoneName);
                kinematicPositionOffset = kinematicTarget.InverseTransformPoint(antennaTip.position);
            }
            var previousBody = ConfigureKinematicProxy(
                physicsRoot,
                ChainProxyName(sideName, 1),
                kinematicTarget,
                FindRequiredChild($"Accelerando_{sideName}_ConnectedChain_Link_01"),
                kinematicPositionOffset);

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
                ConfigureJoint(linkBody, previousBody, isMace: false);
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

            ConfigureJoint(maceBody, previousBody, isMace: true);
        }

        private Rigidbody ConfigureKinematicProxy(
            Transform physicsRoot,
            string proxyName,
            Transform target,
            Transform visual,
            Vector3 positionOffset)
        {
            var rotationOffset = Quaternion.Inverse(target.rotation) * visual.rotation;
            var body = CreateProxyBody(physicsRoot, proxyName, visual, linkMass, linkColliderRadius, isKinematic: true);
            body.useGravity = false;
            body.linearDamping = 0.45f;
            body.angularDamping = 0.9f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.position = IsDeathAntennaTerminalTarget(target)
                ? target.TransformPoint(positionOffset)
                : target.position + target.rotation * positionOffset;
            body.rotation = target.rotation * rotationOffset;

            kinematicFollowers.Add(new KinematicFollower(
                target,
                body,
                rotationOffset,
                positionOffset,
                followRotation: true,
                circularAttackRotation: lockLinearChainConnections));
            visualFollowers.Add(new VisualFollower(visual, body));
            return body;
        }

        private void ConfigureAttackTorsoCollisionProxy(Transform physicsRoot)
        {
            var torsoRoot = FindRequiredChild("Bone_000");
            var proxyObject = new GameObject(AttackTorsoCollisionProxyName);
            var proxyTransform = proxyObject.transform;
            proxyTransform.SetParent(physicsRoot, false);
            proxyTransform.position = torsoRoot.position;
            proxyTransform.rotation = transform.rotation;
            proxyTransform.localScale = Vector3.one;

            var body = proxyObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var chest = proxyObject.AddComponent<CapsuleCollider>();
            chest.direction = 1;
            chest.center = new Vector3(0f, 0.40f, -0.05f);
            chest.radius = 0.48f;
            chest.height = 1.25f;

            var shellObject = new GameObject("Accelerando_AttackRearTorsoCollider");
            shellObject.transform.SetParent(proxyTransform, false);
            var shell = shellObject.AddComponent<CapsuleCollider>();
            shell.direction = 2;
            shell.center = new Vector3(0f, 0.40f, -0.70f);
            shell.radius = 0.55f;
            shell.height = 1.65f;

            var positionOffset = Quaternion.Inverse(torsoRoot.rotation) *
                (proxyTransform.position - torsoRoot.position);
            var rotationOffset = Quaternion.Inverse(torsoRoot.rotation) * proxyTransform.rotation;
            kinematicFollowers.Add(new KinematicFollower(
                torsoRoot,
                body,
                rotationOffset,
                positionOffset,
                followRotation: true,
                circularAttackRotation: false));
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
                ? isMace ? AttackMaceLinearDamping : AttackLinkLinearDamping
                : deathCollapseMode
                ? isMace ? DeathMaceLinearDamping : DeathLinkLinearDamping
                : hitRecoilMode
                ? isMace ? HitMaceLinearDamping : HitLinkLinearDamping
                : 0.65f;
            body.angularDamping = lockLinearChainConnections
                ? isMace ? AttackMaceAngularDamping : AttackLinkAngularDamping
                : deathCollapseMode
                ? isMace ? DeathMaceAngularDamping : DeathLinkAngularDamping
                : hitRecoilMode
                ? isMace ? HitMaceAngularDamping : HitLinkAngularDamping
                : 1.2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = lockLinearChainConnections
                ? isMace ? 18f : 24f
                : 18f;
            if (deathCollapseMode)
            {
                body.maxLinearVelocity = isMace
                    ? DeathMaceMaximumLinearVelocity
                    : DeathLinkMaximumLinearVelocity;
            }
            body.constraints = lockLinearChainConnections
                // Each authored chain keeps its own lateral X coordinate while remaining fully
                // physical in the vertical forward YZ attack plane.
                ? RigidbodyConstraints.FreezePositionX
                : hitRecoilMode || deathCollapseMode
                ? RigidbodyConstraints.None
                : RigidbodyConstraints.FreezeRotation;
            if (lockLinearChainConnections)
            {
                body.solverIterations = 64;
                body.solverVelocityIterations = 32;
                body.sleepThreshold = AttackBodySleepThreshold;
            }
            else if (hitRecoilMode || deathCollapseMode)
            {
                body.solverIterations = 64;
                body.solverVelocityIterations = 32;
                body.sleepThreshold = AttackBodySleepThreshold;
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
            IgnoreGeneratedProxyCollisions(collider);

            return body;
        }

        // The generated spheres are solver proxies, not literal chain-link collision hulls.
        // Letting non-neighbouring links or opposite chains collide injects impulses that are
        // unrelated to antenna motion and makes the mace appear self-propelled.
        private void IgnoreGeneratedProxyCollisions(Collider collider)
        {
            for (var i = 0; i < physicsColliders.Count; i++)
            {
                var other = physicsColliders[i];
                if (other != null)
                {
                    Physics.IgnoreCollision(collider, other, true);
                }
            }

            physicsColliders.Add(collider);
        }

        private void ConfigureJoint(Rigidbody body, Rigidbody connectedBody, bool isMace)
        {
            var joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedBody;
            joint.massScale = lockLinearChainConnections && isMace
                ? AttackMaceJointMassScale
                : 1f;
            joint.connectedMassScale = 1f;
            joint.autoConfigureConnectedAnchor = false;
            var isAntennaDrivenHandle = lockLinearChainConnections &&
                !isMace &&
                IsAttackDrivenHandleBody(body.name);
            if (isAntennaDrivenHandle)
            {
                // The first dynamic link is the physical handle segment swung by the rotating
                // antenna ring. Only this attachment follows the ring's rotating offset; every
                // downstream link remains a tension-only tether.
                joint.anchor = Vector3.zero;
                joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(body.position);
            }
            else if (lockLinearChainConnections)
            {
                // Downstream links preserve their authored centre distance through a rotating
                // offset anchor. Wide angular limits keep the chain flexible without allowing
                // high-speed distance-limit overshoot to open visible gaps.
                joint.anchor = Vector3.zero;
                joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(body.position);
            }
            else
            {
                joint.anchor = Vector3.zero;
                joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(body.position);
            }

            joint.xMotion = isAntennaDrivenHandle || lockLinearChainConnections || hitRecoilMode || deathCollapseMode
                ? ConfigurableJointMotion.Locked
                : ConfigurableJointMotion.Limited;
            joint.yMotion = joint.xMotion;
            joint.zMotion = joint.xMotion;
            joint.angularXMotion = hitRecoilMode || deathCollapseMode
                ? ConfigurableJointMotion.Limited
                : lockLinearChainConnections
                ? isAntennaDrivenHandle || isMace
                    ? ConfigurableJointMotion.Locked
                    : ConfigurableJointMotion.Limited
                : ConfigurableJointMotion.Locked;
            joint.angularYMotion = joint.angularXMotion;
            joint.angularZMotion = joint.angularXMotion;
            if (lockLinearChainConnections && !isAntennaDrivenHandle && !isMace)
            {
                joint.lowAngularXLimit = new SoftJointLimit
                    { limit = -AttackTailAngularLimit, contactDistance = 3f };
                joint.highAngularXLimit = new SoftJointLimit
                    { limit = AttackTailAngularLimit, contactDistance = 3f };
                joint.angularYLimit = new SoftJointLimit
                    { limit = AttackTailAngularLimit, contactDistance = 3f };
                joint.angularZLimit = new SoftJointLimit
                    { limit = AttackTailAngularLimit, contactDistance = 3f };
            }
            else if (hitRecoilMode || deathCollapseMode)
            {
                var bodyDrivenAngularLimit = deathCollapseMode ? 58f : 42f;
                joint.lowAngularXLimit = new SoftJointLimit
                    { limit = -bodyDrivenAngularLimit, contactDistance = 3f };
                joint.highAngularXLimit = new SoftJointLimit
                    { limit = bodyDrivenAngularLimit, contactDistance = 3f };
                joint.angularYLimit = new SoftJointLimit
                    { limit = bodyDrivenAngularLimit, contactDistance = 3f };
                joint.angularZLimit = new SoftJointLimit
                    { limit = bodyDrivenAngularLimit, contactDistance = 3f };
            }
            joint.linearLimit = new SoftJointLimit
            {
                limit = lockLinearChainConnections
                    ? Vector3.Distance(body.position, connectedBody.position)
                    : jointLimit,
                contactDistance = lockLinearChainConnections
                    ? Mathf.Min(0.008f, Vector3.Distance(body.position, connectedBody.position) * 0.08f)
                    : jointLimit * 0.5f
            };
            // Projection is only the emergency recovery for a solver error beyond the visible
            // continuity tolerance. Normal motion remains driven by the locked joint impulses.
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = lockLinearChainConnections
                ? AttackJointProjectionDistance
                : hitRecoilMode || deathCollapseMode
                ? AttackJointProjectionDistance
                : jointLimit * 1.5f;
            joint.projectionAngle = 6f;
            joint.enableCollision = false;
            joint.enablePreprocessing = true;
        }

        private static bool IsAttackDrivenHandleBody(string bodyName)
        {
            for (var linkIndex = 2; linkIndex <= AttackDrivenHandleLastLinkIndex; linkIndex++)
            {
                if (bodyName.EndsWith($"_Link_{linkIndex:00}", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsDeathAntennaTerminalTarget(Transform target)
        {
            return deathCollapseMode && target != null &&
                (string.Equals(target.name, "Bone_009", StringComparison.Ordinal) ||
                 string.Equals(target.name, "Bone_006", StringComparison.Ordinal));
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
                var nextPosition = IsDeathAntennaTerminalTarget(follower.Target)
                    ? follower.Target.TransformPoint(follower.PositionOffset)
                    : follower.Target.position + follower.Target.rotation * follower.PositionOffset;
                if ((hitRecoilMode || deathCollapseMode) &&
                    bodyMotionRoot != null &&
                    !follower.Target.IsChildOf(bodyMotionRoot))
                {
                    nextPosition += bodyMotionRoot.position - bodyMotionInitialRootPosition;
                }
                if (deathCollapseMode && follower.Target == bodyMotionRoot)
                {
                    nextPosition = Vector3.MoveTowards(
                        currentPosition,
                        nextPosition,
                        DeathKinematicFollowerMaximumSpeed * deltaTime);
                }
                follower.Body.MovePosition(nextPosition);
                if (follower.CircularAttackRotation)
                {
                    var targetOffsetLocal = transform.InverseTransformVector(
                        follower.Target.position - follower.InitialTargetPosition);
                    var radialY = targetOffsetLocal.y + AttackAntennaCircularDriveRadius;
                    var radialZ = targetOffsetLocal.z;
                    if (radialY * radialY + radialZ * radialZ > 0.0001f)
                    {
                        var circularAngle = Mathf.Atan2(radialZ, radialY) * Mathf.Rad2Deg;
                        follower.Body.MoveRotation(
                            Quaternion.AngleAxis(circularAngle, transform.right) *
                            follower.InitialBodyRotation);
                    }
                }
                else if (follower.FollowRotation)
                {
                    follower.Body.MoveRotation(follower.Target.rotation * follower.RotationOffset);
                }

                // The attack ring phase comes from the animated tip's actual circular position,
                // not the IK bone quaternion (which may choose alternating shortest poses).
                // Its offset joint then pulls the dynamic handle and downstream tether.
                kinematicFollowers[i] = follower.WithVelocity(
                    (nextPosition - currentPosition) / deltaTime,
                    deltaTime);
            }
        }

        private void ApplyRestForces(float deltaTime)
        {
            // During the attack, the animated antenna may only pull the first kinematic link.
            // The connected joints, link inertia, and mace mass must transmit the motion down
            // the chain; applying forces to every follower would make the mace self-propelled.
            if (lockLinearChainConnections)
            {
                for (var i = 0; i < dynamicFollowers.Count; i++)
                {
                    var body = dynamicFollowers[i].Body;
                    if (body != null)
                    {
                        body.AddForce(
                            -Physics.gravity * (1f - AttackGravityScale),
                            ForceMode.Acceleration);
                    }
                }

                return;
            }

            // Hit recoil is transmitted only from the animated antenna/body-side kinematic
            // attachment through ConfigurableJoint tension. Driving every downstream body from
            // the same recoil velocity would add a second, non-physical impulse to the chain.
            if (hitRecoilMode || deathCollapseMode)
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
                if ((hitRecoilMode || deathCollapseMode) &&
                    bodyMotionRoot != null &&
                    !follower.Anchor.IsChildOf(bodyMotionRoot))
                {
                    restPosition += bodyMotionRoot.position - bodyMotionInitialRootPosition;
                }
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
            public KinematicFollower(
                Transform target,
                Rigidbody body,
                Quaternion rotationOffset,
                Vector3 positionOffset,
                bool followRotation,
                bool circularAttackRotation)
                : this(
                    target,
                    body,
                    rotationOffset,
                    positionOffset,
                    followRotation,
                    circularAttackRotation,
                    target.position,
                    body.rotation,
                    Vector3.zero,
                    0f)
            {
            }

            private KinematicFollower(
                Transform target,
                Rigidbody body,
                Quaternion rotationOffset,
                Vector3 positionOffset,
                bool followRotation,
                bool circularAttackRotation,
                Vector3 initialTargetPosition,
                Quaternion initialBodyRotation,
                Vector3 velocity,
                float stationaryDuration)
            {
                Target = target;
                Body = body;
                RotationOffset = rotationOffset;
                PositionOffset = positionOffset;
                FollowRotation = followRotation;
                CircularAttackRotation = circularAttackRotation;
                InitialTargetPosition = initialTargetPosition;
                InitialBodyRotation = initialBodyRotation;
                Velocity = velocity;
                StationaryDuration = stationaryDuration;
            }

            public Transform Target { get; }
            public Rigidbody Body { get; }
            public Quaternion RotationOffset { get; }
            public Vector3 PositionOffset { get; }
            public bool FollowRotation { get; }
            public bool CircularAttackRotation { get; }
            public Vector3 InitialTargetPosition { get; }
            public Quaternion InitialBodyRotation { get; }
            public Vector3 Velocity { get; }
            public float StationaryDuration { get; }

            public KinematicFollower WithVelocity(Vector3 velocity, float deltaTime)
            {
                return new KinematicFollower(
                    Target,
                    Body,
                    RotationOffset,
                    PositionOffset,
                    FollowRotation,
                    CircularAttackRotation,
                    InitialTargetPosition,
                    InitialBodyRotation,
                    velocity,
                    0f);
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

    }
}
