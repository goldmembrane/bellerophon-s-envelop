using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1100)]
    public sealed class KnockbackReactionFlightPose : MonoBehaviour
    {
        private enum ProxyShape
        {
            Capsule,
            Box
        }

        private sealed class LegProxy
        {
            internal Transform Bone;
            internal Transform EndpointBone;
            internal Transform Proxy;
            internal Rigidbody Body;
            internal Collider Collider;
            internal ConfigurableJoint Joint;
            internal float InitialLength;
            internal Quaternion LaunchRotation;
            internal float ReactionFactor;
        }

        private sealed class PoseBone
        {
            internal Transform Bone;
            internal Vector3 RecoveryLocalPosition;
            internal Quaternion RecoveryLocalRotation;
            internal Vector3 PlayerIdleLocalPosition;
            internal Quaternion PlayerIdleLocalRotation;
            internal Quaternion PreviousOutputLocalRotation;
            internal bool IsArm;
            internal Transform PlayerIdleReferenceBone;
        }

        private const string HipsPath = "Armature/Hips";
        private const string LeftUpperArmPath =
            HipsPath + "/Spine02/Spine01/Spine/LeftShoulder/LeftArm";
        private const string LeftForearmPath = LeftUpperArmPath + "/LeftForeArm";
        private const string LeftHandPath = LeftForearmPath + "/LeftHand";
        private const string RightUpperArmPath =
            HipsPath + "/Spine02/Spine01/Spine/RightShoulder/RightArm";
        private const string RightForearmPath = RightUpperArmPath + "/RightForeArm";
        private const string RightHandPath = RightForearmPath + "/RightHand";
        private const string LeftUpperLegPath = HipsPath + "/LeftUpLeg";
        private const string LeftLegPath = LeftUpperLegPath + "/LeftLeg";
        private const string LeftFootPath = LeftLegPath + "/LeftFoot";
        private const string RightUpperLegPath = HipsPath + "/RightUpLeg";
        private const string RightLegPath = RightUpperLegPath + "/RightLeg";
        private const string RightFootPath = RightLegPath + "/RightFoot";

        [SerializeField] private KnockbackReactionActiveRagdoll activeRagdoll;
        [SerializeField] private GameObject playerIdleReferenceObject;
        [SerializeField, Min(0.01f)] private float armPoseBlendDuration = 0.12f;
        [SerializeField, Range(0f, 20f)] private float armElevationDegrees = 0f;
        [SerializeField, Range(0f, 0.2f)] private float armOutwardBias = 0.035f;
        [SerializeField, Min(1f)] private float legJointSpring = 125f;
        [SerializeField, Min(0.1f)] private float legJointDamper = 17f;
        [SerializeField, Min(0f)] private float legReactionVelocity = 0.45f;
        [SerializeField, Min(0f)] private float legReactionTorque = 0.85f;

        private readonly List<LegProxy> legProxies = new List<LegProxy>();
        private readonly List<PoseBone> recoveryBones = new List<PoseBone>();
        private readonly List<GameObject> helperObjects = new List<GameObject>();
        private Transform leftUpperArm;
        private Transform leftForearm;
        private Transform leftHand;
        private Transform rightUpperArm;
        private Transform rightForearm;
        private Transform rightHand;
        private Transform leftIndex;
        private Transform leftMiddle;
        private Transform leftLittle;
        private Transform rightIndex;
        private Transform rightMiddle;
        private Transform rightLittle;
        private Vector3 leftLocalFingerDirection;
        private Vector3 leftLocalPalmNormal;
        private Vector3 rightLocalFingerDirection;
        private Vector3 rightLocalPalmNormal;
        private KnockbackReactionActiveRagdoll.RuntimePhase previousPhase;
        private bool initialized;
        private bool legPhysicsActive;
        private bool landingArmCompletionPending;

        public int DynamicLegBodyCount { get; private set; }
        public int ConfiguredLegBodyCount => legProxies.Count;
        public float MaximumJointAnchorSeparation { get; private set; }
        public float MaximumBoneLengthError { get; private set; }
        public float MaximumStableArmForwardError { get; private set; }
        public float MaximumStableArmBendDegrees { get; private set; }
        public float MaximumStablePalmForwardError { get; private set; }
        public float MinimumStableMovementDirectionDot { get; private set; } = 1f;
        public float MaximumLegAngularVelocity { get; private set; }
        public float MaximumLegPoseDeviation { get; private set; }
        public float MaximumAsymmetricLegAngularSpeedDelta { get; private set; }
        public float CurrentLandingArmIdleRotationError { get; private set; }
        public float CurrentLandingArmIdlePositionError { get; private set; }
        public float MaximumLandingArmFrameStepDegrees { get; private set; }
        public int LandingArmRecoverySampleCount { get; private set; }
        public bool LandingArmRecoveryReachedIdle { get; private set; }
        public bool UsesLowerBodyConfigurableJoints =>
            legProxies.Count == 6 && legProxies.TrueForAll(proxy => proxy.Joint != null);
        public bool IsLegPhysicsActive => legPhysicsActive;

        public void Configure(
            KnockbackReactionActiveRagdoll configuredActiveRagdoll,
            float configuredArmBlendDuration)
        {
            activeRagdoll = configuredActiveRagdoll != null
                ? configuredActiveRagdoll
                : throw new ArgumentNullException(nameof(configuredActiveRagdoll));
            armPoseBlendDuration = Mathf.Max(0.01f, configuredArmBlendDuration);
        }

        private void Awake()
        {
            if (!Application.isPlaying)
                return;
            activeRagdoll ??= GetComponent<KnockbackReactionActiveRagdoll>();
            if (activeRagdoll == null)
                throw new InvalidOperationException(
                    name + " KnockbackReactionFlightPose requires the active-ragdoll cycle.");
            if (playerIdleReferenceObject == null)
                throw new InvalidOperationException(
                    name + " KnockbackReactionFlightPose requires the live Player_Idle reference object.");

            ResolveArmRig();
            CachePalmFrames();
            BuildLowerBodyPhysics();
            CacheRecoveryBones();
            previousPhase = activeRagdoll.Phase;
            initialized = true;
        }

        private void FixedUpdate()
        {
            if (!initialized)
                return;
            KnockbackReactionActiveRagdoll.RuntimePhase phase = activeRagdoll.Phase;
            if (phase != previousPhase)
            {
                HandlePhaseTransition(previousPhase, phase);
                previousPhase = phase;
            }

            if (phase != KnockbackReactionActiveRagdoll.RuntimePhase.PhysicsKnockback)
                return;
            if (activeRagdoll.PhaseElapsed < 0.12f)
                return;
            UpdateLegPhysicsMetrics();
            Vector3 travel = activeRagdoll.CurrentTravelDirection;
            MinimumStableMovementDirectionDot = Mathf.Min(
                MinimumStableMovementDirectionDot,
                Vector3.Dot(travel, activeRagdoll.ExplosionHorizontalDirection));
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;
            switch (activeRagdoll.Phase)
            {
                case KnockbackReactionActiveRagdoll.RuntimePhase.Waiting:
                    SyncInactiveLegProxies();
                    CompleteLandingArmRecoveryIfNeeded();
                    break;
                case KnockbackReactionActiveRagdoll.RuntimePhase.IdleHold:
                    SyncInactiveLegProxies();
                    if (landingArmCompletionPending)
                        CompleteLandingArmRecoveryIfNeeded();
                    else
                        ApplyPlayerIdleArmPose();
                    break;
                case KnockbackReactionActiveRagdoll.RuntimePhase.PhysicsKnockback:
                    DriveVisibleLegsFromPhysics();
                    ApplyFlightArmPose();
                    break;
                case KnockbackReactionActiveRagdoll.RuntimePhase.Recovering:
                    BlendRecoveryPose();
                    break;
            }
        }

        private void HandlePhaseTransition(
            KnockbackReactionActiveRagdoll.RuntimePhase from,
            KnockbackReactionActiveRagdoll.RuntimePhase to)
        {
            if (to == KnockbackReactionActiveRagdoll.RuntimePhase.PhysicsKnockback)
            {
                ResetCycleMetrics();
                ActivateLegPhysics();
                return;
            }
            if (from == KnockbackReactionActiveRagdoll.RuntimePhase.PhysicsKnockback &&
                to == KnockbackReactionActiveRagdoll.RuntimePhase.Recovering)
            {
                DriveVisibleLegsFromPhysics();
                ApplyFlightArmPose(1f);
                CacheRecoveryPose();
                landingArmCompletionPending = true;
                DeactivateLegPhysics();
                return;
            }
            if (to == KnockbackReactionActiveRagdoll.RuntimePhase.Waiting)
                DeactivateLegPhysics();
        }

        private void ResolveArmRig()
        {
            leftUpperArm = RequireTransform(LeftUpperArmPath);
            leftForearm = RequireTransform(LeftForearmPath);
            leftHand = RequireTransform(LeftHandPath);
            rightUpperArm = RequireTransform(RightUpperArmPath);
            rightForearm = RequireTransform(RightForearmPath);
            rightHand = RequireTransform(RightHandPath);
            leftIndex = RequireTransform(LeftHandPath + "/LeftIndexProximal");
            leftMiddle = RequireTransform(LeftHandPath + "/LeftMiddleProximal");
            leftLittle = RequireTransform(LeftHandPath + "/LeftLittleProximal");
            rightIndex = RequireTransform(RightHandPath + "/RightIndexProximal");
            rightMiddle = RequireTransform(RightHandPath + "/RightMiddleProximal");
            rightLittle = RequireTransform(RightHandPath + "/RightLittleProximal");
        }

        private void CachePalmFrames()
        {
            CachePalmFrame(
                leftHand,
                leftIndex,
                leftMiddle,
                leftLittle,
                -1f,
                out leftLocalFingerDirection,
                out leftLocalPalmNormal);
            CachePalmFrame(
                rightHand,
                rightIndex,
                rightMiddle,
                rightLittle,
                1f,
                out rightLocalFingerDirection,
                out rightLocalPalmNormal);
        }

        private static void CachePalmFrame(
            Transform hand,
            Transform index,
            Transform middle,
            Transform little,
            float sideSign,
            out Vector3 localFingerDirection,
            out Vector3 localPalmNormal)
        {
            Vector3 fingerDirection = (
                (index.position + middle.position + little.position) / 3f -
                hand.position).normalized;
            Vector3 across = (index.position - little.position).normalized;
            Vector3 palmNormal = Vector3.Cross(fingerDirection, across).normalized * sideSign;
            if (fingerDirection.sqrMagnitude < 0.99f || palmNormal.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    hand.name + " palm basis could not be resolved.");
            localFingerDirection = hand.InverseTransformDirection(fingerDirection).normalized;
            localPalmNormal = hand.InverseTransformDirection(palmNormal).normalized;
        }

        private void BuildLowerBodyPhysics()
        {
            Rigidbody rootBody = activeRagdoll.PhysicsBody ??
                throw new InvalidOperationException(
                    name + " root Rigidbody is unavailable for lower-body joints.");
            Collider rootCollider = activeRagdoll.PhysicsCollider;
            LegProxy leftThigh = CreateProxy(
                LeftUpperLegPath,
                LeftLegPath,
                rootBody,
                ProxyShape.Capsule,
                1.4f,
                "LeftThigh");
            LegProxy leftShin = CreateProxy(
                LeftLegPath,
                LeftFootPath,
                leftThigh.Body,
                ProxyShape.Capsule,
                1f,
                "LeftShin");
            CreateProxy(
                LeftFootPath,
                null,
                leftShin.Body,
                ProxyShape.Box,
                0.5f,
                "LeftFoot");
            LegProxy rightThigh = CreateProxy(
                RightUpperLegPath,
                RightLegPath,
                rootBody,
                ProxyShape.Capsule,
                1.4f,
                "RightThigh");
            LegProxy rightShin = CreateProxy(
                RightLegPath,
                RightFootPath,
                rightThigh.Body,
                ProxyShape.Capsule,
                1f,
                "RightShin");
            CreateProxy(
                RightFootPath,
                null,
                rightShin.Body,
                ProxyShape.Box,
                0.5f,
                "RightFoot");

            for (int first = 0; first < legProxies.Count; first++)
            {
                if (rootCollider != null)
                    Physics.IgnoreCollision(
                        rootCollider,
                        legProxies[first].Collider,
                        true);
                for (int second = first + 1; second < legProxies.Count; second++)
                    Physics.IgnoreCollision(
                        legProxies[first].Collider,
                        legProxies[second].Collider,
                        true);
            }
        }

        private LegProxy CreateProxy(
            string bonePath,
            string endpointPath,
            Rigidbody connectedBody,
            ProxyShape shape,
            float mass,
            string suffix)
        {
            Transform bone = RequireTransform(bonePath);
            Transform endpoint = string.IsNullOrEmpty(endpointPath)
                ? null
                : RequireTransform(endpointPath);
            GameObject proxyObject = new GameObject(name + "_KnockbackProxy_" + suffix);
            proxyObject.hideFlags = HideFlags.HideInHierarchy;
            proxyObject.transform.SetPositionAndRotation(bone.position, bone.rotation);
            helperObjects.Add(proxyObject);
            Rigidbody proxyBody = proxyObject.AddComponent<Rigidbody>();
            proxyBody.mass = mass;
            proxyBody.linearDamping = 0.35f;
            proxyBody.angularDamping = 2.8f;
            proxyBody.interpolation = RigidbodyInterpolation.Interpolate;
            proxyBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            proxyBody.maxAngularVelocity = 10f;
            proxyBody.useGravity = false;
            proxyBody.isKinematic = true;

            Collider proxyCollider;
            float initialLength = 0f;
            if (shape == ProxyShape.Capsule && endpoint != null)
            {
                Vector3 localEndpoint = proxyObject.transform.InverseTransformPoint(
                    endpoint.position);
                initialLength = Vector3.Distance(bone.position, endpoint.position);
                CapsuleCollider capsule = proxyObject.AddComponent<CapsuleCollider>();
                capsule.direction = LargestAxis(localEndpoint);
                capsule.center = localEndpoint * 0.5f;
                capsule.radius = Mathf.Clamp(initialLength * 0.22f, 0.035f, 0.09f);
                capsule.height = Mathf.Max(initialLength, capsule.radius * 2f);
                proxyCollider = capsule;
            }
            else
            {
                BoxCollider box = proxyObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.13f, 0.24f, 0.11f);
                proxyCollider = box;
            }
            proxyCollider.enabled = false;

            ConfigurableJoint joint = proxyObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(bone.position);
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
            ConfigureAngularLimits(joint, suffix);
            JointDrive drive = new JointDrive
            {
                positionSpring = legJointSpring,
                positionDamper = legJointDamper,
                maximumForce = 700f
            };
            joint.angularXDrive = drive;
            joint.angularYZDrive = drive;
            joint.rotationDriveMode = RotationDriveMode.XYAndZ;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.018f;
            joint.projectionAngle = 5f;
            joint.enableCollision = false;

            var proxy = new LegProxy
            {
                Bone = bone,
                EndpointBone = endpoint,
                Proxy = proxyObject.transform,
                Body = proxyBody,
                Collider = proxyCollider,
                Joint = joint,
                InitialLength = initialLength,
                ReactionFactor = suffix.EndsWith("Thigh", StringComparison.Ordinal)
                    ? 0.45f
                    : suffix.EndsWith("Shin", StringComparison.Ordinal)
                        ? 0.72f
                        : 1f
            };
            legProxies.Add(proxy);
            return proxy;
        }

        private static void ConfigureAngularLimits(ConfigurableJoint joint, string suffix)
        {
            bool thigh = suffix.EndsWith("Thigh", StringComparison.Ordinal);
            bool shin = suffix.EndsWith("Shin", StringComparison.Ordinal);
            float x = thigh ? 38f : shin ? 52f : 18f;
            float yz = thigh ? 28f : shin ? 12f : 16f;
            joint.lowAngularXLimit = SoftLimit(-x);
            joint.highAngularXLimit = SoftLimit(x);
            joint.angularYLimit = SoftLimit(yz);
            joint.angularZLimit = SoftLimit(yz);
        }

        private void CacheRecoveryBones()
        {
            var unique = new HashSet<Transform>();
            foreach (LegProxy proxy in legProxies)
                unique.Add(proxy.Bone);
            unique.Add(leftUpperArm);
            unique.Add(leftForearm);
            unique.Add(leftHand);
            unique.Add(rightUpperArm);
            unique.Add(rightForearm);
            unique.Add(rightHand);
            foreach (Transform bone in unique)
                recoveryBones.Add(new PoseBone
                {
                    Bone = bone,
                    IsArm = IsArmBone(bone),
                    PlayerIdleReferenceBone = IsArmBone(bone)
                        ? RequirePlayerIdleReferenceBone(RelativeRigPath(bone))
                        : null
                });
        }

        private bool IsArmBone(Transform bone)
        {
            return bone == leftUpperArm || bone == leftForearm || bone == leftHand ||
                bone == rightUpperArm || bone == rightForearm || bone == rightHand;
        }

        private string RelativeRigPath(Transform bone)
        {
            string path = bone.name;
            Transform current = bone.parent;
            while (current != null && current != transform)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private Transform RequirePlayerIdleReferenceBone(string path)
        {
            return playerIdleReferenceObject.transform.Find(path) ??
                throw new InvalidOperationException(
                    "Live Player_Idle reference rig is missing " + path + ".");
        }

        private void ActivateLegPhysics()
        {
            Rigidbody rootBody = activeRagdoll.PhysicsBody;
            Vector3 impulseDirection = activeRagdoll.LastExplosionImpulseDirection;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                impulseDirection,
                Vector3.up).normalized;
            if (horizontal.sqrMagnitude < 0.99f)
                horizontal = activeRagdoll.ApprovedForwardDirection;
            Vector3 lateral = Vector3.Cross(Vector3.up, horizontal).normalized;
            foreach (LegProxy proxy in legProxies)
            {
                proxy.Proxy.SetPositionAndRotation(
                    proxy.Bone.position,
                    proxy.Bone.rotation);
                proxy.Body.position = proxy.Bone.position;
                proxy.Body.rotation = proxy.Bone.rotation;
                proxy.LaunchRotation = proxy.Bone.rotation;
                proxy.Joint.connectedAnchor =
                    proxy.Joint.connectedBody.transform.InverseTransformPoint(
                        proxy.Bone.position);
                proxy.Body.isKinematic = false;
                proxy.Body.useGravity = true;
                proxy.Body.linearVelocity = rootBody.linearVelocity;
                proxy.Body.angularVelocity = rootBody.angularVelocity;
                proxy.Collider.enabled = true;
                float sideSign = proxy.Bone.name.StartsWith("Left", StringComparison.Ordinal)
                    ? -1f
                    : 1f;
                Vector3 secondaryVelocity =
                    lateral * sideSign * 0.34f +
                    Vector3.up * (0.35f + proxy.ReactionFactor * 0.2f) -
                    horizontal * 0.12f;
                proxy.Body.AddForce(
                    secondaryVelocity * legReactionVelocity * proxy.ReactionFactor,
                    ForceMode.VelocityChange);
                Vector3 reactionTorque =
                    lateral * (0.58f + proxy.ReactionFactor * 0.32f) +
                    horizontal * sideSign * (0.28f + proxy.ReactionFactor * 0.22f) +
                    Vector3.up * sideSign * 0.2f;
                proxy.Body.AddTorque(
                    reactionTorque * legReactionTorque,
                    ForceMode.VelocityChange);
            }
            Physics.SyncTransforms();
            legPhysicsActive = true;
            DynamicLegBodyCount = legProxies.Count;
        }

        private void SyncInactiveLegProxies()
        {
            if (legPhysicsActive)
                return;
            foreach (LegProxy proxy in legProxies)
            {
                proxy.Proxy.SetPositionAndRotation(
                    proxy.Bone.position,
                    proxy.Bone.rotation);
                proxy.Body.position = proxy.Bone.position;
                proxy.Body.rotation = proxy.Bone.rotation;
                proxy.Joint.connectedAnchor =
                    proxy.Joint.connectedBody.transform.InverseTransformPoint(
                        proxy.Bone.position);
            }
        }

        private void DeactivateLegPhysics()
        {
            foreach (LegProxy proxy in legProxies)
            {
                if (!proxy.Body.isKinematic)
                {
                    proxy.Body.linearVelocity = Vector3.zero;
                    proxy.Body.angularVelocity = Vector3.zero;
                }
                proxy.Body.useGravity = false;
                proxy.Body.isKinematic = true;
                proxy.Collider.enabled = false;
            }
            legPhysicsActive = false;
            DynamicLegBodyCount = 0;
        }

        private void DriveVisibleLegsFromPhysics()
        {
            foreach (LegProxy proxy in legProxies)
                proxy.Bone.SetPositionAndRotation(proxy.Proxy.position, proxy.Proxy.rotation);
        }

        private void ApplyFlightArmPose()
        {
            float blend = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(activeRagdoll.PhaseElapsed / armPoseBlendDuration));
            ApplyFlightArmPose(blend);
        }

        private void ApplyFlightArmPose(float blend)
        {
            Vector3 direction = Vector3.ProjectOnPlane(
                activeRagdoll.CurrentTravelDirection,
                Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.99f)
                direction = activeRagdoll.ApprovedForwardDirection;
            direction = Quaternion.AngleAxis(
                -armElevationDegrees,
                Vector3.Cross(direction, Vector3.up).normalized) * direction;
            Vector3 lateral = Vector3.Cross(Vector3.up, direction).normalized;
            ApplyArm(
                leftUpperArm,
                leftForearm,
                leftHand,
                leftLocalFingerDirection,
                leftLocalPalmNormal,
                (direction - lateral * armOutwardBias).normalized,
                direction,
                blend);
            ApplyArm(
                rightUpperArm,
                rightForearm,
                rightHand,
                rightLocalFingerDirection,
                rightLocalPalmNormal,
                (direction + lateral * armOutwardBias).normalized,
                direction,
                blend);

            if (blend < 0.999f || activeRagdoll.PhaseElapsed < armPoseBlendDuration + 0.03f)
                return;
            MeasureArmPose(direction);
        }

        private static void ApplyArm(
            Transform upperArm,
            Transform forearm,
            Transform hand,
            Vector3 localFingerDirection,
            Vector3 localPalmNormal,
            Vector3 segmentDirection,
            Vector3 palmDirection,
            float blend)
        {
            AimSegment(upperArm, forearm.position - upperArm.position, segmentDirection, blend);
            AimSegment(forearm, hand.position - forearm.position, segmentDirection, blend);

            Quaternion animatedLocal = hand.localRotation;
            Quaternion localFrame = Quaternion.LookRotation(
                localPalmNormal,
                localFingerDirection);
            Quaternion desiredFrame = Quaternion.LookRotation(
                palmDirection,
                Vector3.up);
            Quaternion desiredWorld = desiredFrame * Quaternion.Inverse(localFrame);
            Quaternion desiredLocal = Quaternion.Inverse(hand.parent.rotation) * desiredWorld;
            hand.localRotation = Quaternion.Slerp(animatedLocal, desiredLocal, blend);
        }

        private static void AimSegment(
            Transform bone,
            Vector3 currentDirection,
            Vector3 desiredDirection,
            float blend)
        {
            if (currentDirection.sqrMagnitude < 0.000001f ||
                desiredDirection.sqrMagnitude < 0.000001f)
                return;
            Quaternion animatedLocal = bone.localRotation;
            Quaternion desiredWorld = Quaternion.FromToRotation(
                currentDirection.normalized,
                desiredDirection.normalized) * bone.rotation;
            Quaternion desiredLocal = Quaternion.Inverse(bone.parent.rotation) * desiredWorld;
            bone.localRotation = Quaternion.Slerp(animatedLocal, desiredLocal, blend);
        }

        private void MeasureArmPose(Vector3 direction)
        {
            MeasureArm(
                leftUpperArm,
                leftForearm,
                leftHand,
                leftLocalPalmNormal,
                direction);
            MeasureArm(
                rightUpperArm,
                rightForearm,
                rightHand,
                rightLocalPalmNormal,
                direction);
        }

        private void MeasureArm(
            Transform upperArm,
            Transform forearm,
            Transform hand,
            Vector3 localPalmNormal,
            Vector3 direction)
        {
            Vector3 upperDirection = (forearm.position - upperArm.position).normalized;
            Vector3 foreDirection = (hand.position - forearm.position).normalized;
            MaximumStableArmForwardError = Mathf.Max(
                MaximumStableArmForwardError,
                Vector3.Angle(upperDirection, direction),
                Vector3.Angle(foreDirection, direction));
            MaximumStableArmBendDegrees = Mathf.Max(
                MaximumStableArmBendDegrees,
                Vector3.Angle(upperDirection, foreDirection));
            Vector3 palmNormal = hand.TransformDirection(localPalmNormal).normalized;
            MaximumStablePalmForwardError = Mathf.Max(
                MaximumStablePalmForwardError,
                Vector3.Angle(palmNormal, direction));
        }

        private void CacheRecoveryPose()
        {
            foreach (PoseBone poseBone in recoveryBones)
            {
                poseBone.RecoveryLocalPosition = poseBone.Bone.localPosition;
                poseBone.RecoveryLocalRotation = poseBone.Bone.localRotation;
                poseBone.PreviousOutputLocalRotation = poseBone.Bone.localRotation;
            }
            CurrentLandingArmIdleRotationError = 180f;
            CurrentLandingArmIdlePositionError = float.PositiveInfinity;
            MaximumLandingArmFrameStepDegrees = 0f;
            LandingArmRecoverySampleCount = 0;
            LandingArmRecoveryReachedIdle = false;
        }

        private void BlendRecoveryPose()
        {
            float linearT = Mathf.Clamp01(
                activeRagdoll.PhaseElapsed /
                Mathf.Max(0.01f, activeRagdoll.RecoveryDuration));
            float t = Mathf.SmoothStep(0f, 1f, linearT);
            ReadPlayerIdleArmTargets();
            float maximumRotationError = 0f;
            float maximumPositionError = 0f;
            foreach (PoseBone poseBone in recoveryBones)
            {
                Vector3 animatedPosition = poseBone.IsArm
                    ? poseBone.PlayerIdleLocalPosition
                    : poseBone.Bone.localPosition;
                Quaternion animatedRotation = poseBone.IsArm
                    ? poseBone.PlayerIdleLocalRotation
                    : poseBone.Bone.localRotation;
                poseBone.Bone.localPosition = Vector3.Lerp(
                    poseBone.RecoveryLocalPosition,
                    animatedPosition,
                    poseBone.IsArm ? linearT : t);
                poseBone.Bone.localRotation = Quaternion.Slerp(
                    poseBone.RecoveryLocalRotation,
                    animatedRotation,
                    poseBone.IsArm ? linearT : t);
                if (!poseBone.IsArm)
                    continue;
                MaximumLandingArmFrameStepDegrees = Mathf.Max(
                    MaximumLandingArmFrameStepDegrees,
                    Quaternion.Angle(
                        poseBone.PreviousOutputLocalRotation,
                        poseBone.Bone.localRotation));
                maximumRotationError = Mathf.Max(
                    maximumRotationError,
                    Quaternion.Angle(
                        poseBone.Bone.localRotation,
                        poseBone.PlayerIdleLocalRotation));
                maximumPositionError = Mathf.Max(
                    maximumPositionError,
                    Vector3.Distance(
                        poseBone.Bone.localPosition,
                        poseBone.PlayerIdleLocalPosition));
                poseBone.PreviousOutputLocalRotation = poseBone.Bone.localRotation;
            }
            CurrentLandingArmIdleRotationError = maximumRotationError;
            CurrentLandingArmIdlePositionError = maximumPositionError;
            LandingArmRecoverySampleCount++;
        }

        private void ReadPlayerIdleArmTargets()
        {
            foreach (PoseBone poseBone in recoveryBones)
            {
                if (!poseBone.IsArm)
                    continue;
                poseBone.PlayerIdleLocalPosition =
                    poseBone.PlayerIdleReferenceBone.localPosition;
                poseBone.PlayerIdleLocalRotation =
                    poseBone.PlayerIdleReferenceBone.localRotation;
            }
        }

        private void CompleteLandingArmRecoveryIfNeeded()
        {
            if (!landingArmCompletionPending)
                return;
            ReadPlayerIdleArmTargets();
            float maximumRotationError = 0f;
            float maximumPositionError = 0f;
            foreach (PoseBone poseBone in recoveryBones)
            {
                if (!poseBone.IsArm)
                    continue;
                MaximumLandingArmFrameStepDegrees = Mathf.Max(
                    MaximumLandingArmFrameStepDegrees,
                    Quaternion.Angle(
                        poseBone.PreviousOutputLocalRotation,
                        poseBone.PlayerIdleLocalRotation));
                poseBone.Bone.localPosition = poseBone.PlayerIdleLocalPosition;
                poseBone.Bone.localRotation = poseBone.PlayerIdleLocalRotation;
                maximumRotationError = Mathf.Max(
                    maximumRotationError,
                    Quaternion.Angle(
                        poseBone.Bone.localRotation,
                        poseBone.PlayerIdleLocalRotation));
                maximumPositionError = Mathf.Max(
                    maximumPositionError,
                    Vector3.Distance(
                        poseBone.Bone.localPosition,
                        poseBone.PlayerIdleLocalPosition));
                poseBone.PreviousOutputLocalRotation = poseBone.Bone.localRotation;
            }
            CurrentLandingArmIdleRotationError = maximumRotationError;
            CurrentLandingArmIdlePositionError = maximumPositionError;
            LandingArmRecoveryReachedIdle =
                maximumRotationError <= 0.75f && maximumPositionError <= 0.005f;
            landingArmCompletionPending = false;
        }

        private void ApplyPlayerIdleArmPose()
        {
            ReadPlayerIdleArmTargets();
            foreach (PoseBone poseBone in recoveryBones)
            {
                if (!poseBone.IsArm)
                    continue;
                poseBone.Bone.localPosition = poseBone.PlayerIdleLocalPosition;
                poseBone.Bone.localRotation = poseBone.PlayerIdleLocalRotation;
            }
            CurrentLandingArmIdleRotationError = 0f;
            CurrentLandingArmIdlePositionError = 0f;
            LandingArmRecoveryReachedIdle = true;
        }

        private void UpdateLegPhysicsMetrics()
        {
            float minimumAngularSpeed = float.PositiveInfinity;
            float maximumAngularSpeed = 0f;
            foreach (LegProxy proxy in legProxies)
            {
                float angularSpeed = proxy.Body.angularVelocity.magnitude;
                minimumAngularSpeed = Mathf.Min(minimumAngularSpeed, angularSpeed);
                maximumAngularSpeed = Mathf.Max(maximumAngularSpeed, angularSpeed);
                MaximumLegAngularVelocity = Mathf.Max(
                    MaximumLegAngularVelocity,
                    angularSpeed);
                MaximumLegPoseDeviation = Mathf.Max(
                    MaximumLegPoseDeviation,
                    Quaternion.Angle(proxy.LaunchRotation, proxy.Body.rotation));
                Vector3 ownAnchor = proxy.Joint.transform.TransformPoint(proxy.Joint.anchor);
                Vector3 connectedAnchor = proxy.Joint.connectedBody.transform.TransformPoint(
                    proxy.Joint.connectedAnchor);
                MaximumJointAnchorSeparation = Mathf.Max(
                    MaximumJointAnchorSeparation,
                    Vector3.Distance(ownAnchor, connectedAnchor));
                if (proxy.EndpointBone == null || proxy.InitialLength <= 0f)
                    continue;
                LegProxy endpointProxy = legProxies.Find(
                    candidate => candidate.Bone == proxy.EndpointBone);
                if (endpointProxy == null)
                    continue;
                MaximumBoneLengthError = Mathf.Max(
                    MaximumBoneLengthError,
                    Mathf.Abs(
                        Vector3.Distance(proxy.Proxy.position, endpointProxy.Proxy.position) -
                        proxy.InitialLength));
            }
            if (!float.IsPositiveInfinity(minimumAngularSpeed))
                MaximumAsymmetricLegAngularSpeedDelta = Mathf.Max(
                    MaximumAsymmetricLegAngularSpeedDelta,
                    maximumAngularSpeed - minimumAngularSpeed);
        }

        private void ResetCycleMetrics()
        {
            MaximumJointAnchorSeparation = 0f;
            MaximumBoneLengthError = 0f;
            MaximumStableArmForwardError = 0f;
            MaximumStableArmBendDegrees = 0f;
            MaximumStablePalmForwardError = 0f;
            MinimumStableMovementDirectionDot = 1f;
            MaximumLegAngularVelocity = 0f;
            MaximumLegPoseDeviation = 0f;
            MaximumAsymmetricLegAngularSpeedDelta = 0f;
            CurrentLandingArmIdleRotationError = 0f;
            CurrentLandingArmIdlePositionError = 0f;
            MaximumLandingArmFrameStepDegrees = 0f;
            LandingArmRecoverySampleCount = 0;
            LandingArmRecoveryReachedIdle = false;
            landingArmCompletionPending = false;
        }

        private Transform RequireTransform(string path)
        {
            return transform.Find(path) ?? throw new InvalidOperationException(
                name + " flight-pose rig is missing " + path + ".");
        }

        private static int LargestAxis(Vector3 value)
        {
            Vector3 absolute = new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z)
                return 0;
            return absolute.y >= absolute.z ? 1 : 2;
        }

        private static SoftJointLimit SoftLimit(float value)
        {
            return new SoftJointLimit { limit = value, contactDistance = 1f };
        }

        private void OnDestroy()
        {
            foreach (GameObject helper in helperObjects)
                if (helper != null)
                    Destroy(helper);
            helperObjects.Clear();
        }
    }
}
