using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class KnockbackReactionAnimationSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Knockback_Reaction";
        private const string IdleReferenceName = "Player_Idle";
        private const string ExternalSourcePath = "player model/transfer knockback.fbx";
        internal const string SourceAssetPath =
            "Assets/_Project/Animations/Knockback_Reaction_Source.fbx";
        internal const string IdleCopyPath =
            "Assets/_Project/Animations/Knockback_Reaction_PlayerIdle.anim";
        internal const string UpperMaskPath =
            "Assets/_Project/Animations/Knockback_Reaction_Upper.mask";
        internal const string ControllerPath =
            "Assets/_Project/Animations/Knockback_Reaction.controller";
        internal const string FinalImagePath =
            "docs/validation/KnockbackReaction/Final.png";
        internal const string FlightPoseFinalImagePath =
            "docs/validation/KnockbackReactionFlightPose/Final.png";
        internal const string ImmediateLaunchFinalImagePath =
            "docs/validation/KnockbackReactionImmediateLaunch/Final.png";
        internal const string LandingArmRecoveryFinalImagePath =
            "docs/validation/KnockbackReactionLandingArmRecovery/Final.png";

        internal const string BaseLayerName = "KnockbackLowerIdle2D";
        internal const string UpperLayerName = "KnockbackUpperSource";
        internal const string BaseStateName = "KnockbackLowerPlayerIdle";
        internal const string UpperStateName = "KnockbackUpperImportedSource";
        internal const string TreeName = "KnockbackPlayerIdle2D";
        internal const string MoveX = "KnockbackMoveX";
        internal const string MoveY = "KnockbackMoveY";
        internal const string CycleBehaviourTypeName =
            "Bellerophon.PlayerAnimation.KnockbackReactionCycleBehaviour";
        internal const string ActiveRagdollTypeName =
            "Bellerophon.PlayerAnimation.KnockbackReactionActiveRagdoll";
        internal const string FlightPoseTypeName =
            "Bellerophon.PlayerAnimation.KnockbackReactionFlightPose";

        internal const float ExplosionDelay = 0f;
        internal const float ExplosionOriginDistance = 3f;
        internal const float ExplosionForce = 12.4f;
        internal const float ExplosionUpwardModifier = 0.9f;
        internal const float ExplosionLateralOffset = 0.65f;
        internal const float RootAngularSpring = 52f;
        internal const float RootAngularDamper = 10f;
        internal const float TargetDisplacement = 2.5f;
        internal const float RecoveryDuration = 0.1f;
        internal const float IdleHoldDuration = 1.5f;
        internal const float LegJointSpring = 125f;
        internal const float LegJointDamper = 17f;
        internal const float LegReactionVelocity = 0.45f;
        internal const float LegReactionTorque = 0.85f;

        [MenuItem("Bellerophon/Player/Apply Knockback Reaction Animation")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject idleReference = FindUnique(scene, IdleReferenceName);
            Animator animator = RequireAnimator(target, TargetName);
            Animator idleAnimator = RequireAnimator(idleReference, IdleReferenceName);
            TransformSnapshot root = new TransformSnapshot(target.transform);
            string rendererSignature = RendererSignature(target.transform);
            Avatar avatar = animator.avatar;

            ConfigureExactGenericSourceForLooping();
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip idleSource = RequireDefaultClip(idleAnimator, IdleReferenceName);
            DeleteOutputAssets();
            AnimationClip idleCopy = CreateExactCopy(idleSource, IdleCopyPath);
            AvatarMask upperMask = CreateUpperBodyMask(target.transform);
            AnimatorController controller = CreateController(source, idleCopy, upperMask);

            Undo.RecordObject(animator, "Connect Knockback_Reaction layered animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);

            Type activeRagdollType = RequireActiveRagdollType();
            Component activeRagdoll = target.GetComponent(activeRagdollType) ??
                Undo.AddComponent(target, activeRagdollType);
            Undo.RecordObject(activeRagdoll, "Configure Knockback_Reaction active ragdoll");
            SerializedObject serialized = new SerializedObject(activeRagdoll);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("upperLayerName").stringValue = UpperLayerName;
            serialized.FindProperty("explosionDelay").floatValue = ExplosionDelay;
            serialized.FindProperty("explosionOriginDistance").floatValue = ExplosionOriginDistance;
            serialized.FindProperty("explosionForce").floatValue = ExplosionForce;
            serialized.FindProperty("explosionUpwardModifier").floatValue = ExplosionUpwardModifier;
            serialized.FindProperty("explosionLateralOffset").floatValue = ExplosionLateralOffset;
            serialized.FindProperty("rootAngularSpring").floatValue = RootAngularSpring;
            serialized.FindProperty("rootAngularDamper").floatValue = RootAngularDamper;
            serialized.FindProperty("targetDisplacement").floatValue = TargetDisplacement;
            serialized.FindProperty("recoveryDuration").floatValue = RecoveryDuration;
            serialized.FindProperty("idleHoldDuration").floatValue = IdleHoldDuration;
            serialized.FindProperty("maximumPhysicsDuration").floatValue = 2.2f;
            serialized.FindProperty("sharedGroundHeight").floatValue = target.transform.position.y;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(activeRagdoll);
            EditorUtility.SetDirty(activeRagdoll);

            root.RequireUnchanged(target.transform, TargetName);
            RequireEqual(rendererSignature, RendererSignature(target.transform),
                TargetName + " renderer signature");
            if (animator.avatar != avatar)
                throw new InvalidOperationException(TargetName + " Avatar changed unexpectedly.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying Knockback_Reaction.");
            AssetDatabase.SaveAssets();
            Inspect();
            Debug.Log(
                "[KnockbackReaction] Applied exact imported upper animation, exact Player_Idle lower animation in a 2D Blend Tree, and Rigidbody/Collider/ConfigurableJoint active-ragdoll cycle. sourceCurvesModified=False,generatedMotion=False,explosionVfx=False");
        }

        [MenuItem("Bellerophon/Player/Inspect Knockback Reaction Animation")]
        internal static void Inspect()
        {
            RequireEditMode();
            RequireExactSourceCopy();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target, TargetName);
            AnimationClip source = RequireSingleSourceClip();
            AnimationClip idleReference = RequireDefaultClip(
                RequireAnimator(FindUnique(scene, IdleReferenceName), IdleReferenceName),
                IdleReferenceName);
            AnimationClip idleCopy = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleCopyPath) ??
                throw new InvalidOperationException(IdleCopyPath + " is missing.");
            RequireEqual(ClipSignature(idleReference), ClipSignature(idleCopy),
                "Player_Idle exact clip copy");
            RequireLoop(source, true, SourceAssetPath);
            if (source.humanMotion)
                throw new InvalidOperationException(
                    "Knockback source must remain a direct Generic clip.");

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException(ControllerPath + " is missing.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException(TargetName + " controller differs.");
            if (animator.applyRootMotion)
                throw new InvalidOperationException(TargetName + " root motion must be disabled.");
            if (controller.layers.Length != 2)
                throw new InvalidOperationException(
                    "Knockback controller must contain exactly two layers.");
            RequireFloatParameter(controller, MoveX);
            RequireFloatParameter(controller, MoveY);

            AnimatorControllerLayer baseLayer = controller.layers[0];
            AnimatorState baseState = RequireSingleDefaultState(baseLayer, BaseStateName);
            BlendTree tree = baseState.motion as BlendTree ??
                throw new InvalidOperationException(
                    "Knockback lower layer must use a 2D Blend Tree.");
            if (baseLayer.name != BaseLayerName ||
                baseLayer.avatarMask != null ||
                baseLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(baseLayer.defaultWeight, 1f) ||
                tree.name != TreeName ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter != MoveX ||
                tree.blendParameterY != MoveY ||
                tree.children.Length != 1 ||
                tree.children[0].motion != idleCopy ||
                tree.children[0].position != Vector2.zero ||
                baseState.behaviours.Count(item => item != null &&
                    item.GetType().FullName == CycleBehaviourTypeName) != 1)
                throw new InvalidOperationException(
                    "Knockback lower Player_Idle 2D Blend Tree differs.");

            AnimatorControllerLayer upperLayer = controller.layers[1];
            AnimatorState upperState = RequireSingleDefaultState(upperLayer, UpperStateName);
            AvatarMask upperMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperMaskPath) ??
                throw new InvalidOperationException(UpperMaskPath + " is missing.");
            if (upperLayer.name != UpperLayerName ||
                upperLayer.avatarMask != upperMask ||
                upperLayer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(upperLayer.defaultWeight, 1f) ||
                upperState.motion != source ||
                !Mathf.Approximately(upperState.speed, 1f) ||
                upperState.transitions.Length != 0)
                throw new InvalidOperationException(
                    "Knockback imported upper-body layer differs.");
            RequireUpperBodyMask(target.transform, upperMask);

            Component activeRagdoll = target.GetComponent(RequireActiveRagdollType()) ??
                throw new InvalidOperationException(
                    TargetName + " active-ragdoll component is missing.");
            SerializedObject serialized = new SerializedObject(activeRagdoll);
            RequireObject(serialized, "animator", animator);
            RequireString(serialized, "upperLayerName", UpperLayerName);
            RequireFloat(serialized, "explosionDelay", ExplosionDelay);
            RequireFloat(serialized, "explosionOriginDistance", ExplosionOriginDistance);
            RequireFloat(serialized, "explosionForce", ExplosionForce);
            RequireFloat(serialized, "explosionUpwardModifier", ExplosionUpwardModifier);
            RequireFloat(serialized, "explosionLateralOffset", ExplosionLateralOffset);
            RequireFloat(serialized, "rootAngularSpring", RootAngularSpring);
            RequireFloat(serialized, "rootAngularDamper", RootAngularDamper);
            RequireFloat(serialized, "targetDisplacement", TargetDisplacement);
            RequireFloat(serialized, "recoveryDuration", RecoveryDuration);
            RequireFloat(serialized, "idleHoldDuration", IdleHoldDuration);
            RequireFloat(serialized, "sharedGroundHeight", target.transform.position.y);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[KnockbackReaction] Structural inspection passed. sourceSha256=" +
                Sha256(Absolute(ExternalSourcePath)) +
                ",sourceBinaryCopyExact=True,sourceAnimationCurvesModified=False," +
                "lowerClipExactPlayerIdle=True,blendTree=FreeformCartesian2D," +
                "upperSourceDirect=True,delay=0,originBehind=3,lateralOffset=0.65," +
                "explosionForce=12.4,upwardModifier=0.9,targetDistance=2.5," +
                "recovery=0.1,idleHold=1.5,explosionVfx=False,unityConsoleErrors=0");
        }

        [MenuItem("Bellerophon/Player/Apply Knockback Reaction Flight Pose")]
        internal static void ApplyFlightPose()
        {
            ApplyImmediateLaunch();
        }

        [MenuItem("Bellerophon/Player/Apply Knockback Reaction Immediate Launch")]
        internal static void ApplyImmediateLaunch()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Component activeRagdoll = RequireRuntimeActiveRagdoll(target);
            Undo.RecordObject(activeRagdoll, "Configure Knockback_Reaction immediate launch");
            SerializedObject activeSerialized = new SerializedObject(activeRagdoll);
            activeSerialized.FindProperty("explosionDelay").floatValue = ExplosionDelay;
            activeSerialized.FindProperty("explosionOriginDistance").floatValue = ExplosionOriginDistance;
            activeSerialized.FindProperty("explosionForce").floatValue = ExplosionForce;
            activeSerialized.FindProperty("explosionUpwardModifier").floatValue = ExplosionUpwardModifier;
            activeSerialized.FindProperty("explosionLateralOffset").floatValue = ExplosionLateralOffset;
            activeSerialized.FindProperty("rootAngularSpring").floatValue = RootAngularSpring;
            activeSerialized.FindProperty("rootAngularDamper").floatValue = RootAngularDamper;
            activeSerialized.FindProperty("targetDisplacement").floatValue = TargetDisplacement;
            activeSerialized.FindProperty("recoveryDuration").floatValue = RecoveryDuration;
            activeSerialized.FindProperty("idleHoldDuration").floatValue = IdleHoldDuration;
            activeSerialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(activeRagdoll);
            EditorUtility.SetDirty(activeRagdoll);

            Type flightPoseType = RequireFlightPoseType();
            Component flightPose = target.GetComponent(flightPoseType) ??
                Undo.AddComponent(target, flightPoseType);
            Undo.RecordObject(flightPose, "Configure Knockback_Reaction flight pose");
            SerializedObject serialized = new SerializedObject(flightPose);
            serialized.FindProperty("activeRagdoll").objectReferenceValue = activeRagdoll;
            GameObject playerIdleReference = FindUnique(scene, IdleReferenceName);
            serialized.FindProperty("playerIdleReferenceObject").objectReferenceValue =
                playerIdleReference;
            serialized.FindProperty("armPoseBlendDuration").floatValue = 0.12f;
            serialized.FindProperty("armElevationDegrees").floatValue = 0f;
            serialized.FindProperty("armOutwardBias").floatValue = 0.035f;
            serialized.FindProperty("legJointSpring").floatValue = LegJointSpring;
            serialized.FindProperty("legJointDamper").floatValue = LegJointDamper;
            serialized.FindProperty("legReactionVelocity").floatValue = LegReactionVelocity;
            serialized.FindProperty("legReactionTorque").floatValue = LegReactionTorque;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(flightPose);
            EditorUtility.SetDirty(flightPose);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene save failed while applying Knockback_Reaction flight pose.");
            AssetDatabase.SaveAssets();
            InspectImmediateLaunch();
            Debug.Log(
                "[KnockbackReactionImmediateLaunch] Applied launch at cycle start, off-centre physical explosion trajectory, forward arm/palm pose and mildly reactive six-body jointed legs. explosionVfx=False");
        }

        [MenuItem("Bellerophon/Player/Inspect Knockback Reaction Flight Pose")]
        internal static void InspectFlightPose()
        {
            InspectImmediateLaunch();
        }

        [MenuItem("Bellerophon/Player/Inspect Knockback Reaction Immediate Launch")]
        internal static void InspectImmediateLaunch()
        {
            RequireEditMode();
            Inspect();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Component activeRagdoll = RequireRuntimeActiveRagdoll(target);
            Component flightPose = target.GetComponent(RequireFlightPoseType()) ??
                throw new InvalidOperationException(
                    TargetName + " flight-pose component is missing.");
            SerializedObject serialized = new SerializedObject(flightPose);
            RequireObject(serialized, "activeRagdoll", activeRagdoll);
            GameObject playerIdleReference = FindUnique(scene, IdleReferenceName);
            RequireObject(serialized, "playerIdleReferenceObject", playerIdleReference);
            RequireFloat(serialized, "armPoseBlendDuration", 0.12f);
            RequireFloat(serialized, "armElevationDegrees", 0f);
            RequireFloat(serialized, "armOutwardBias", 0.035f);
            RequireFloat(serialized, "legJointSpring", LegJointSpring);
            RequireFloat(serialized, "legJointDamper", LegJointDamper);
            RequireFloat(serialized, "legReactionVelocity", LegReactionVelocity);
            RequireFloat(serialized, "legReactionTorque", LegReactionTorque);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            Debug.Log(
                "[KnockbackReactionImmediateLaunch] Structural inspection passed. delay=0,trajectory=offCentreExplosionForce,arms=straightAlongTravel,palms=travelDirection,lowerBodyJointBodies=6,legs=blastReactive,recovery=PlayerIdle,explosionVfx=False,unityConsoleErrors=0");
        }

        [MenuItem("Bellerophon/Player/Apply Knockback Reaction Landing Arm Recovery")]
        internal static void ApplyLandingArmRecovery()
        {
            ApplyImmediateLaunch();
            InspectLandingArmRecovery();
            Debug.Log(
                "[KnockbackReactionLandingArmRecovery] Applied the live Player_Idle scene object's final arm transforms across the existing 0.1-second landing recovery. Other flight physics and cycle timing preserved.");
        }

        [MenuItem("Bellerophon/Player/Inspect Knockback Reaction Landing Arm Recovery")]
        internal static void InspectLandingArmRecovery()
        {
            InspectImmediateLaunch();
            Debug.Log(
                "[KnockbackReactionLandingArmRecovery] Structural inspection passed. source=livePlayerIdleSceneObject,duration=existingRecovery0.1,arms=bilateral,sampling=currentRuntimePose,flightPhysicsPreserved=True,unityConsoleErrors=0");
        }

        internal static GameObject RequireRuntimeTarget()
        {
            return FindUnique(SceneManager.GetActiveScene(), TargetName);
        }

        internal static GameObject RequireRuntimeIdleReference()
        {
            return FindUnique(SceneManager.GetActiveScene(), IdleReferenceName);
        }

        internal static Component RequireRuntimeActiveRagdoll(GameObject target)
        {
            return target.GetComponent(RequireActiveRagdollType()) ??
                throw new InvalidOperationException(
                    TargetName + " active-ragdoll component is missing in Play Mode.");
        }

        internal static Component RequireRuntimeFlightPose(GameObject target)
        {
            return target.GetComponent(RequireFlightPoseType()) ??
                throw new InvalidOperationException(
                    TargetName + " flight-pose component is missing in Play Mode.");
        }

        internal static void WriteFlightPoseFinalEvidence(
            Texture2D image,
            float maximumForwardDisplacement,
            float observedExplosionStart,
            float observedRecoveryDuration,
            float observedIdleHoldDuration,
            Vector3 explosionOrigin,
            Vector3 initialPosition,
            Vector3 approvedForward,
            int configuredLegBodyCount,
            int maximumDynamicLegBodyCount,
            bool usedLowerBodyConfigurableJoints,
            float maximumJointAnchorSeparation,
            float maximumBoneLengthError,
            float maximumStableArmForwardError,
            float maximumStableArmBendDegrees,
            float maximumStablePalmForwardError,
            float minimumMovementDirectionDot,
            int observedPhaseMask,
            int consoleErrorsBefore)
        {
            if (observedPhaseMask != 15)
                throw new InvalidOperationException(
                    "Knockback flight-pose review did not observe all four phases.");
            if (observedExplosionStart < 1.92f || observedExplosionStart > 2.12f)
                throw new InvalidOperationException(
                    "Explosion force start differs from two seconds: " +
                    F(observedExplosionStart));
            Vector3 originDirection = Vector3.ProjectOnPlane(
                initialPosition - explosionOrigin,
                Vector3.up).normalized;
            if (Vector3.Dot(originDirection, approvedForward) < 0.995f)
                throw new InvalidOperationException(
                    "Explosion origin is not three metres behind the carrier.");
            if (maximumForwardDisplacement < 2.30f || maximumForwardDisplacement > 2.75f)
                throw new InvalidOperationException(
                    "Physical forward displacement differs from approximately 2.5 m: " +
                    F(maximumForwardDisplacement));
            if (observedRecoveryDuration < 0.08f || observedRecoveryDuration > 0.18f ||
                observedIdleHoldDuration < 1.45f || observedIdleHoldDuration > 1.65f)
                throw new InvalidOperationException(
                    "Recovery or Player_Idle hold duration differs.");
            if (configuredLegBodyCount != 6 || maximumDynamicLegBodyCount != 6 ||
                !usedLowerBodyConfigurableJoints)
                throw new InvalidOperationException(
                    "Six-body jointed lower-body physics was not continuously active in flight.");
            if (maximumJointAnchorSeparation > 0.055f || maximumBoneLengthError > 0.04f)
                throw new InvalidOperationException(
                    "Lower-body skeleton did not preserve joint connectivity. anchor=" +
                    F(maximumJointAnchorSeparation) + ",boneLength=" +
                    F(maximumBoneLengthError));
            if (maximumStableArmForwardError > 8f || maximumStableArmBendDegrees > 10f)
                throw new InvalidOperationException(
                    "Both arms were not straight in the flight direction. forwardError=" +
                    F(maximumStableArmForwardError) + ",bend=" +
                    F(maximumStableArmBendDegrees));
            if (maximumStablePalmForwardError > 10f)
                throw new InvalidOperationException(
                    "Both palms did not face the flight direction: " +
                    F(maximumStablePalmForwardError));
            if (minimumMovementDirectionDot < 0.96f)
                throw new InvalidOperationException(
                    "Physical travel did not remain aligned with the approved forward direction: " +
                    F(minimumMovementDirectionDot));
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console errors changed during Knockback flight-pose review. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");
            string absoluteImage = Absolute(FlightPoseFinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteImage) ??
                throw new InvalidOperationException("Flight-pose image folder is unavailable."));
            File.WriteAllBytes(absoluteImage, image.EncodeToPNG());
            Debug.Log(
                "[KnockbackReactionFlightPose] Direct Play Mode review passed. phaseMask=" +
                observedPhaseMask + ",explosionStartSeconds=" + F(observedExplosionStart) +
                ",maximumForwardDisplacement=" + F(maximumForwardDisplacement) +
                ",jointBodies=6,jointAnchorSeparation=" + F(maximumJointAnchorSeparation) +
                ",boneLengthError=" + F(maximumBoneLengthError) +
                ",armForwardError=" + F(maximumStableArmForwardError) +
                ",armBend=" + F(maximumStableArmBendDegrees) +
                ",palmForwardError=" + F(maximumStablePalmForwardError) +
                ",movementDirectionDot=" + F(minimumMovementDirectionDot) +
                ",consoleErrors=0,finalImage=" + FlightPoseFinalImagePath);
        }

        internal static void ValidateImmediateLaunchEvidence(
            float maximumTravelDisplacement,
            float maximumLateralDisplacement,
            float maximumHeightGain,
            float maximumTiltDegrees,
            float observedExplosionStart,
            float observedRecoveryDuration,
            float observedIdleHoldDuration,
            float minimumInitialVelocityDirectionDot,
            int explosionEventCount,
            int configuredLegBodyCount,
            int maximumDynamicLegBodyCount,
            bool usedLowerBodyConfigurableJoints,
            float maximumJointAnchorSeparation,
            float maximumBoneLengthError,
            float maximumStableArmForwardError,
            float maximumStableArmBendDegrees,
            float maximumStablePalmForwardError,
            float minimumMovementDirectionDot,
            float maximumLegAngularVelocity,
            float maximumLegPoseDeviation,
            float maximumAsymmetricLegAngularSpeedDelta,
            int observedPhaseMask,
            bool observedWaitingAfterInitialization,
            int consoleErrorsBefore)
        {
            if (observedWaitingAfterInitialization || (observedPhaseMask & 1) != 0)
                throw new InvalidOperationException(
                    "Knockback immediate-launch review observed a pre-flight Waiting phase.");
            if ((observedPhaseMask & 14) != 14 || explosionEventCount < 2)
                throw new InvalidOperationException(
                    "Knockback immediate-launch review did not observe physical flight, recovery, hold and a repeated second launch.");
            if (observedExplosionStart < 0f || observedExplosionStart > 0.06f)
                throw new InvalidOperationException(
                    "Explosion force did not begin at cycle start: " +
                    F(observedExplosionStart));
            if (minimumInitialVelocityDirectionDot < 0.95f)
                throw new InvalidOperationException(
                    "Initial Rigidbody velocity did not follow the explosion impulse direction: " +
                    F(minimumInitialVelocityDirectionDot));
            if (maximumTravelDisplacement < 2.30f || maximumTravelDisplacement > 2.75f)
                throw new InvalidOperationException(
                    "Physical travel differs from approximately 2.5 m: " +
                    F(maximumTravelDisplacement));
            if (maximumLateralDisplacement < 0.18f)
                throw new InvalidOperationException(
                    "The physical path did not visibly inherit the off-centre explosion direction: " +
                    F(maximumLateralDisplacement));
            if (maximumHeightGain < 0.08f || maximumTiltDegrees < 6f)
                throw new InvalidOperationException(
                    "The Rigidbody flight did not visibly react to the blast. height=" +
                    F(maximumHeightGain) + ",tilt=" + F(maximumTiltDegrees));
            if (observedRecoveryDuration < 0.08f || observedRecoveryDuration > 0.18f ||
                observedIdleHoldDuration < 1.45f || observedIdleHoldDuration > 1.65f)
                throw new InvalidOperationException(
                    "Recovery or Player_Idle hold duration differs.");
            if (configuredLegBodyCount != 6 || maximumDynamicLegBodyCount != 6 ||
                !usedLowerBodyConfigurableJoints)
                throw new InvalidOperationException(
                    "Six-body jointed lower-body physics was not continuously active in flight.");
            if (maximumJointAnchorSeparation > 0.055f || maximumBoneLengthError > 0.04f)
                throw new InvalidOperationException(
                    "Lower-body skeleton did not preserve joint connectivity. anchor=" +
                    F(maximumJointAnchorSeparation) + ",boneLength=" +
                    F(maximumBoneLengthError));
            if (maximumLegAngularVelocity < 0.35f || maximumLegPoseDeviation < 4f ||
                maximumAsymmetricLegAngularSpeedDelta < 0.12f)
                throw new InvalidOperationException(
                    "The connected legs did not visibly and asymmetrically react to the blast. angularVelocity=" +
                    F(maximumLegAngularVelocity) + ",poseDeviation=" +
                    F(maximumLegPoseDeviation) + ",asymmetry=" +
                    F(maximumAsymmetricLegAngularSpeedDelta));
            if (maximumStableArmForwardError > 8f || maximumStableArmBendDegrees > 10f ||
                maximumStablePalmForwardError > 10f)
                throw new InvalidOperationException(
                    "The approved straight arm and forward palm pose was not preserved. armError=" +
                    F(maximumStableArmForwardError) + ",bend=" +
                    F(maximumStableArmBendDegrees) + ",palmError=" +
                    F(maximumStablePalmForwardError));
            if (minimumMovementDirectionDot < 0.96f)
                throw new InvalidOperationException(
                    "The body pose did not remain aligned to the actual explosion-driven travel direction: " +
                    F(minimumMovementDirectionDot));
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console errors changed during Knockback immediate-launch review. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");
        }

        internal static void WriteImmediateLaunchFinalEvidence(
            Texture2D image,
            float maximumTravelDisplacement,
            float maximumLateralDisplacement,
            float maximumHeightGain,
            float maximumTiltDegrees,
            float observedExplosionStart,
            float observedRecoveryDuration,
            float observedIdleHoldDuration,
            float minimumInitialVelocityDirectionDot,
            int explosionEventCount,
            int configuredLegBodyCount,
            int maximumDynamicLegBodyCount,
            bool usedLowerBodyConfigurableJoints,
            float maximumJointAnchorSeparation,
            float maximumBoneLengthError,
            float maximumStableArmForwardError,
            float maximumStableArmBendDegrees,
            float maximumStablePalmForwardError,
            float minimumMovementDirectionDot,
            float maximumLegAngularVelocity,
            float maximumLegPoseDeviation,
            float maximumAsymmetricLegAngularSpeedDelta,
            int observedPhaseMask,
            bool observedWaitingAfterInitialization,
            int consoleErrorsBefore)
        {
            ValidateImmediateLaunchEvidence(
                maximumTravelDisplacement,
                maximumLateralDisplacement,
                maximumHeightGain,
                maximumTiltDegrees,
                observedExplosionStart,
                observedRecoveryDuration,
                observedIdleHoldDuration,
                minimumInitialVelocityDirectionDot,
                explosionEventCount,
                configuredLegBodyCount,
                maximumDynamicLegBodyCount,
                usedLowerBodyConfigurableJoints,
                maximumJointAnchorSeparation,
                maximumBoneLengthError,
                maximumStableArmForwardError,
                maximumStableArmBendDegrees,
                maximumStablePalmForwardError,
                minimumMovementDirectionDot,
                maximumLegAngularVelocity,
                maximumLegPoseDeviation,
                maximumAsymmetricLegAngularSpeedDelta,
                observedPhaseMask,
                observedWaitingAfterInitialization,
                consoleErrorsBefore);
            string absoluteImage = Absolute(ImmediateLaunchFinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteImage) ??
                throw new InvalidOperationException(
                    "Immediate-launch image folder is unavailable."));
            File.WriteAllBytes(absoluteImage, image.EncodeToPNG());
            Debug.Log(
                "[KnockbackReactionImmediateLaunch] Direct Play Mode review passed. explosionEvents=" +
                explosionEventCount + ",explosionStartSeconds=" + F(observedExplosionStart) +
                ",travel=" + F(maximumTravelDisplacement) +
                ",lateral=" + F(maximumLateralDisplacement) +
                ",height=" + F(maximumHeightGain) +
                ",tilt=" + F(maximumTiltDegrees) +
                ",initialVelocityDirectionDot=" + F(minimumInitialVelocityDirectionDot) +
                ",legAngularVelocity=" + F(maximumLegAngularVelocity) +
                ",legPoseDeviation=" + F(maximumLegPoseDeviation) +
                ",legAsymmetry=" + F(maximumAsymmetricLegAngularSpeedDelta) +
                ",jointAnchorSeparation=" + F(maximumJointAnchorSeparation) +
                ",boneLengthError=" + F(maximumBoneLengthError) +
                ",armForwardError=" + F(maximumStableArmForwardError) +
                ",armBend=" + F(maximumStableArmBendDegrees) +
                ",palmForwardError=" + F(maximumStablePalmForwardError) +
                ",movementDirectionDot=" + F(minimumMovementDirectionDot) +
                ",consoleErrors=0,finalImage=" + ImmediateLaunchFinalImagePath);
        }

        internal static void ValidateLandingArmRecoveryEvidence(
            float maximumTravelDisplacement,
            float maximumLateralDisplacement,
            float observedRecoveryDuration,
            int explosionEventCount,
            int landingArmRecoverySampleCount,
            float firstLandingArmIdleRotationError,
            float finalLandingArmIdleRotationError,
            float finalLandingArmIdlePositionError,
            float maximumLandingArmFrameStepDegrees,
            float maximumLandingArmErrorIncrease,
            bool landingArmRecoveryReachedIdle,
            float maximumJointAnchorSeparation,
            float maximumBoneLengthError,
            float maximumLegPoseDeviation,
            int observedPhaseMask,
            int consoleErrorsBefore)
        {
            if ((observedPhaseMask & 14) != 14 || explosionEventCount < 2)
                throw new InvalidOperationException(
                    "Landing-arm review did not observe flight, recovery, idle hold and repeat.");
            if (observedRecoveryDuration < 0.08f || observedRecoveryDuration > 0.18f)
                throw new InvalidOperationException(
                    "Landing arm recovery no longer uses the approved 0.1-second recovery: " +
                    F(observedRecoveryDuration));
            if (landingArmRecoverySampleCount < 3)
                throw new InvalidOperationException(
                    "Too few landing arm samples were observed for a natural transition: " +
                    landingArmRecoverySampleCount);
            if (firstLandingArmIdleRotationError < 12f)
                throw new InvalidOperationException(
                    "The review did not begin from the forward flight arm pose: " +
                    F(firstLandingArmIdleRotationError));
            if (!landingArmRecoveryReachedIdle ||
                finalLandingArmIdleRotationError > 0.75f ||
                finalLandingArmIdlePositionError > 0.005f)
                throw new InvalidOperationException(
                    "Both arms did not reach the exact Player_Idle target at landing. rotation=" +
                    F(finalLandingArmIdleRotationError) + ",position=" +
                    F(finalLandingArmIdlePositionError));
            if (maximumLandingArmFrameStepDegrees > 65f ||
                maximumLandingArmErrorIncrease > 2f)
                throw new InvalidOperationException(
                    "Landing arm transition was not continuous. frameStep=" +
                    F(maximumLandingArmFrameStepDegrees) + ",errorIncrease=" +
                    F(maximumLandingArmErrorIncrease));
            if (maximumTravelDisplacement < 2.30f || maximumTravelDisplacement > 2.75f ||
                maximumLateralDisplacement < 0.18f)
                throw new InvalidOperationException(
                    "The approved explosion-driven path changed during landing correction. travel=" +
                    F(maximumTravelDisplacement) + ",lateral=" +
                    F(maximumLateralDisplacement));
            if (maximumJointAnchorSeparation > 0.055f ||
                maximumBoneLengthError > 0.04f ||
                maximumLegPoseDeviation < 4f)
                throw new InvalidOperationException(
                    "The approved connected reactive-leg motion changed during landing correction. anchor=" +
                    F(maximumJointAnchorSeparation) + ",boneLength=" +
                    F(maximumBoneLengthError) + ",legPose=" +
                    F(maximumLegPoseDeviation));
            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console errors changed during landing-arm review. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");
        }

        internal static void WriteLandingArmRecoveryFinalEvidence(
            Texture2D image,
            float maximumTravelDisplacement,
            float maximumLateralDisplacement,
            float observedRecoveryDuration,
            int explosionEventCount,
            int landingArmRecoverySampleCount,
            float firstLandingArmIdleRotationError,
            float finalLandingArmIdleRotationError,
            float finalLandingArmIdlePositionError,
            float maximumLandingArmFrameStepDegrees,
            float maximumLandingArmErrorIncrease,
            bool landingArmRecoveryReachedIdle,
            float maximumJointAnchorSeparation,
            float maximumBoneLengthError,
            float maximumLegPoseDeviation,
            int observedPhaseMask,
            int consoleErrorsBefore)
        {
            ValidateLandingArmRecoveryEvidence(
                maximumTravelDisplacement,
                maximumLateralDisplacement,
                observedRecoveryDuration,
                explosionEventCount,
                landingArmRecoverySampleCount,
                firstLandingArmIdleRotationError,
                finalLandingArmIdleRotationError,
                finalLandingArmIdlePositionError,
                maximumLandingArmFrameStepDegrees,
                maximumLandingArmErrorIncrease,
                landingArmRecoveryReachedIdle,
                maximumJointAnchorSeparation,
                maximumBoneLengthError,
                maximumLegPoseDeviation,
                observedPhaseMask,
                consoleErrorsBefore);
            string absoluteImage = Absolute(LandingArmRecoveryFinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteImage) ??
                throw new InvalidOperationException(
                    "Landing-arm image folder is unavailable."));
            File.WriteAllBytes(absoluteImage, image.EncodeToPNG());
            Debug.Log(
                "[KnockbackReactionLandingArmRecovery] Direct Play Mode review passed. samples=" +
                landingArmRecoverySampleCount + ",firstIdleRotationError=" +
                F(firstLandingArmIdleRotationError) + ",finalIdleRotationError=" +
                F(finalLandingArmIdleRotationError) + ",finalIdlePositionError=" +
                F(finalLandingArmIdlePositionError) + ",maximumFrameStep=" +
                F(maximumLandingArmFrameStepDegrees) + ",maximumErrorIncrease=" +
                F(maximumLandingArmErrorIncrease) + ",recoveryDuration=" +
                F(observedRecoveryDuration) + ",travel=" +
                F(maximumTravelDisplacement) + ",lateral=" +
                F(maximumLateralDisplacement) + ",jointAnchorSeparation=" +
                F(maximumJointAnchorSeparation) + ",boneLengthError=" +
                F(maximumBoneLengthError) + ",legPoseDeviation=" +
                F(maximumLegPoseDeviation) +
                ",consoleErrors=0,finalImage=" + LandingArmRecoveryFinalImagePath);
        }

        internal static void WriteFinalEvidence(
            Texture2D image,
            float maximumForwardDisplacement,
            float maximumHeightGain,
            float maximumTiltDegrees,
            float observedExplosionStart,
            float observedRecoveryDuration,
            float observedIdleHoldDuration,
            bool usedExplosionForce,
            bool usedConfigurableJoint,
            bool animatorStayedEnabled,
            int observedPhaseMask,
            int consoleErrorsBefore)
        {
            if (observedPhaseMask != 15)
                throw new InvalidOperationException(
                    "Knockback direct review did not observe all four phases.");
            if (observedExplosionStart < 1.92f || observedExplosionStart > 2.12f)
                throw new InvalidOperationException(
                    "Explosion force did not begin approximately two seconds after cycle start: " +
                    F(observedExplosionStart));
            if (maximumForwardDisplacement < 2.30f || maximumForwardDisplacement > 2.75f)
                throw new InvalidOperationException(
                    "Physical forward displacement differs from approximately 2.5 m: " +
                    F(maximumForwardDisplacement));
            if (observedRecoveryDuration < 0.08f || observedRecoveryDuration > 0.18f)
                throw new InvalidOperationException(
                    "Player_Idle recovery duration differs from 0.1 seconds: " +
                    F(observedRecoveryDuration));
            if (observedIdleHoldDuration < 1.45f || observedIdleHoldDuration > 1.65f)
                throw new InvalidOperationException(
                    "Player_Idle hold duration differs from 1.5 seconds: " +
                    F(observedIdleHoldDuration));
            if (!usedExplosionForce || !usedConfigurableJoint || !animatorStayedEnabled)
                throw new InvalidOperationException(
                    "Physical active-ragdoll or upper animation preservation was not observed.");

            int consoleErrorsAfter = LightsaberSetupTools.ConsoleErrorCount();
            if (consoleErrorsAfter != consoleErrorsBefore)
                throw new InvalidOperationException(
                    "Unity console errors changed during Knockback review. Before=" +
                    consoleErrorsBefore + ", After=" + consoleErrorsAfter + ".");
            string absoluteImage = Absolute(FinalImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteImage) ??
                throw new InvalidOperationException("Final image folder is unavailable."));
            File.WriteAllBytes(absoluteImage, image.EncodeToPNG());
            Debug.Log(
                "[KnockbackReaction] Direct Play Mode review passed. phaseMask=" +
                observedPhaseMask + ",explosionStartSeconds=" + F(observedExplosionStart) +
                ",maximumForwardDisplacement=" + F(maximumForwardDisplacement) +
                ",maximumHeightGain=" + F(maximumHeightGain) +
                ",maximumTiltDegrees=" + F(maximumTiltDegrees) +
                ",recoveryDuration=" + F(observedRecoveryDuration) +
                ",idleHoldDuration=" + F(observedIdleHoldDuration) +
                ",RigidbodyExplosionForce=True,ConfigurableJoint=True," +
                "upperAnimatorContinuous=True,consoleErrors=0,finalImage=" + FinalImagePath);
        }

        private static void ConfigureExactGenericSourceForLooping()
        {
            RequireExactSourceCopy();
            AssetDatabase.ImportAsset(
                SourceAssetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            ModelImporter importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter is unavailable: " + SourceAssetPath);
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.resampleCurves = false;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(SourceAssetPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "ModelImporter disappeared: " + SourceAssetPath);
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must expose exactly one embedded animation clip.");
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            RequireExactSourceCopy();
        }

        private static void DeleteOutputAssets()
        {
            foreach (string path in new[] { IdleCopyPath, UpperMaskPath, ControllerPath })
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                    !AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Could not replace " + path + ".");
            }
        }

        private static AnimationClip CreateExactCopy(AnimationClip source, string path)
        {
            var copy = new AnimationClip();
            EditorUtility.CopySerialized(source, copy);
            copy.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(copy, path);
            RequireEqual(ClipSignature(source), ClipSignature(copy), path + " exact copy");
            return copy;
        }

        private static AvatarMask CreateUpperBodyMask(Transform target)
        {
            var mask = new AvatarMask { name = "Knockback_Reaction_Upper" };
            string upperPath = UpperBranchPath(target);
            string[] paths = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target)
                .Select(item => AnimationUtility.CalculateTransformPath(item, target))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path.Count(character => character == '/'))
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index, IsInUpperBranch(paths[index], upperPath));
            }
            for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            AssetDatabase.CreateAsset(mask, UpperMaskPath);
            return mask;
        }

        private static AnimatorController CreateController(
            AnimationClip upperSource,
            AnimationClip idleCopy,
            AvatarMask upperMask)
        {
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(MoveX, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveY, AnimatorControllerParameterType.Float);

            var baseMachine = new AnimatorStateMachine { name = BaseLayerName };
            AssetDatabase.AddObjectToAsset(baseMachine, controller);
            var tree = new BlendTree
            {
                name = TreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = MoveX,
                blendParameterY = MoveY,
                useAutomaticThresholds = false,
                children = new[]
                {
                    new ChildMotion
                    {
                        motion = idleCopy,
                        position = Vector2.zero,
                        timeScale = 1f,
                        cycleOffset = 0f,
                        mirror = false,
                        threshold = 0f,
                        directBlendParameter = string.Empty
                    }
                }
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            AnimatorState baseState = baseMachine.AddState(BaseStateName);
            baseState.motion = tree;
            baseState.speed = 1f;
            baseState.writeDefaultValues = false;
            baseState.AddStateMachineBehaviour(RequireCycleBehaviourType());
            baseMachine.defaultState = baseState;
            var baseLayer = new AnimatorControllerLayer
            {
                name = BaseLayerName,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = baseMachine
            };

            var upperMachine = new AnimatorStateMachine { name = UpperLayerName };
            AssetDatabase.AddObjectToAsset(upperMachine, controller);
            AnimatorState upperState = upperMachine.AddState(UpperStateName);
            upperState.motion = upperSource;
            upperState.speed = 1f;
            upperState.writeDefaultValues = false;
            upperMachine.defaultState = upperState;
            var upperLayer = new AnimatorControllerLayer
            {
                name = UpperLayerName,
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                avatarMask = upperMask,
                stateMachine = upperMachine
            };
            controller.layers = new[] { baseLayer, upperLayer };
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(baseMachine);
            EditorUtility.SetDirty(upperMachine);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip RequireSingleSourceClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(SourceAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(
                    SourceAssetPath + " must contain exactly one embedded clip; actual=" +
                    clips.Length + ".");
            return clips[0];
        }

        private static AnimationClip RequireDefaultClip(Animator animator, string label)
        {
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(label + " must use an AnimatorController.");
            AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                throw new InvalidOperationException(label + " default state is missing.");
            if (state.motion is AnimationClip clip)
                return clip;
            if (state.motion is BlendTree tree)
            {
                AnimationClip[] clips = tree.children
                    .Select(child => child.motion)
                    .OfType<AnimationClip>()
                    .Distinct()
                    .ToArray();
                if (clips.Length == 1)
                    return clips[0];
            }
            throw new InvalidOperationException(
                label + " default motion does not resolve to one AnimationClip.");
        }

        private static void RequireExactSourceCopy()
        {
            string external = Absolute(ExternalSourcePath);
            string copied = Absolute(SourceAssetPath);
            if (!File.Exists(external) || !File.Exists(copied))
                throw new FileNotFoundException(
                    "Knockback source or project copy is missing.");
            RequireEqual(Sha256(external), Sha256(copied),
                "Knockback source binary copy");
        }

        private static void RequireLoop(AnimationClip clip, bool expected, string label)
        {
            bool actual = AnimationUtility.GetAnimationClipSettings(clip).loopTime;
            if (actual != expected)
                throw new InvalidOperationException(
                    label + " loopTime differs. Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static AnimatorState RequireSingleDefaultState(
            AnimatorControllerLayer layer,
            string expectedName)
        {
            AnimatorState[] states = layer.stateMachine.states.Select(item => item.state).ToArray();
            if (states.Length != 1 ||
                layer.stateMachine.defaultState != states[0] ||
                states[0].name != expectedName)
                throw new InvalidOperationException(
                    layer.name + " must contain one default state named " + expectedName + ".");
            return states[0];
        }

        private static void RequireFloatParameter(AnimatorController controller, string name)
        {
            AnimatorControllerParameter[] matches = controller.parameters
                .Where(parameter => parameter.name == name).ToArray();
            if (matches.Length != 1 || matches[0].type != AnimatorControllerParameterType.Float)
                throw new InvalidOperationException(
                    "Knockback controller float parameter differs: " + name + ".");
        }

        private static void RequireUpperBodyMask(Transform target, AvatarMask mask)
        {
            string upperPath = UpperBranchPath(target);
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                if (mask.GetTransformActive(index) != IsInUpperBranch(path, upperPath))
                    throw new InvalidOperationException(
                        "Knockback upper-body mask differs at " + path + ".");
            }
        }

        private static string UpperBranchPath(Transform target)
        {
            Transform[] matches = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "Spine02").ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected exactly one Spine02 under " + TargetName + ".");
            return AnimationUtility.CalculateTransformPath(matches[0], target);
        }

        private static bool IsInUpperBranch(string path, string upperPath)
        {
            return path == upperPath || path.StartsWith(upperPath + "/", StringComparison.Ordinal);
        }

        private static Type RequireCycleBehaviourType()
        {
            Type type = TypeCache.GetTypesDerivedFrom<StateMachineBehaviour>()
                .SingleOrDefault(candidate => candidate.FullName == CycleBehaviourTypeName);
            return type ?? throw new InvalidOperationException(
                "Knockback cycle behaviour type is unavailable.");
        }

        private static Type RequireActiveRagdollType()
        {
            Type type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .SingleOrDefault(candidate => candidate.FullName == ActiveRagdollTypeName);
            return type ?? throw new InvalidOperationException(
                "Knockback active-ragdoll runtime type is unavailable.");
        }

        private static Type RequireFlightPoseType()
        {
            Type type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .SingleOrDefault(candidate => candidate.FullName == FlightPoseTypeName);
            return type ?? throw new InvalidOperationException(
                "Knockback flight-pose runtime type is unavailable.");
        }

        private static void RequireObject(
            SerializedObject serialized,
            string property,
            UnityEngine.Object expected)
        {
            if (serialized.FindProperty(property).objectReferenceValue != expected)
                throw new InvalidOperationException(property + " reference differs.");
        }

        private static void RequireString(
            SerializedObject serialized,
            string property,
            string expected)
        {
            if (serialized.FindProperty(property).stringValue != expected)
                throw new InvalidOperationException(property + " differs.");
        }

        private static void RequireFloat(
            SerializedObject serialized,
            string property,
            float expected)
        {
            if (Mathf.Abs(serialized.FindProperty(property).floatValue - expected) > 0.0001f)
                throw new InvalidOperationException(property + " differs.");
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + F(clip.length))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                         .OrderBy(item => item.path, StringComparer.Ordinal)
                         .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                         .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        F(key.time), F(key.value), F(key.inTangent), F(key.outTangent),
                        F(key.inWeight), F(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            return result.ToString();
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected exactly one " + name + "; actual=" + matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target, string label)
        {
            return target.GetComponent<Animator>() ??
                throw new InvalidOperationException(label + " Animator is missing.");
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Knockback_Reaction setup requires Edit Mode.");
        }

        private static string RendererSignature(Transform root)
        {
            return string.Join(
                "\n",
                root.GetComponentsInChildren<Renderer>(true)
                    .OrderBy(renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform, root), StringComparer.Ordinal)
                    .Select(renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform, root) + "|" + renderer.GetType().FullName + "|" +
                        renderer.enabled));
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable."),
                relativePath));
        }

        private static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " differs.");
        }

        internal static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            internal TransformSnapshot(Transform transform)
            {
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (transform.localPosition != position ||
                    transform.localRotation != rotation ||
                    transform.localScale != scale)
                    throw new InvalidOperationException(
                        label + " root Transform changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class KnockbackReactionPlayModeCapture
    {
        private const string PendingKey = "Bellerophon.KnockbackReaction.Pending";
        private const string StateKey = "Bellerophon.KnockbackReaction.State";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.KnockbackReaction.ConsoleErrorsBefore";
        private const string FailureKey = "Bellerophon.KnockbackReaction.Failure";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditModeAfterSuccess = 3;
        private const int WaitingForEditModeAfterFailure = 4;
        private const int RequiredPanelCount = 5;

        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static Animator animator;
        private static Component cycle;
        private static Type cycleType;
        private static double startedAt;
        private static Vector3 viewDirection;
        private static float maximumForwardDisplacement;
        private static float maximumHeightGain;
        private static float maximumTiltDegrees;
        private static float observedExplosionStart;
        private static float observedRecoveryDuration;
        private static float observedIdleHoldDuration;
        private static bool usedExplosionForce;
        private static bool usedConfigurableJoint;
        private static bool animatorStayedEnabled = true;
        private static int observedPhaseMask;
        private static int nextPanel;

        static KnockbackReactionPlayModeCapture()
        {
            if (HasPendingCapture)
                Subscribe();
        }

        internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Knockback direct review must start in Edit Mode.");
            KnockbackReactionAnimationSetupTools.Inspect();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Knockback direct review has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before Knockback review completed.");
                    if (EditorApplication.timeSinceStartup - startedAt > 20d)
                        throw new TimeoutException(
                            "Knockback direct review exceeded 20 seconds.");
                    InspectRuntimeAndCapture();
                    if (Panels.Count < RequiredPanelCount)
                        return;
                    Texture2D image = CombinePanels();
                    try
                    {
                        KnockbackReactionAnimationSetupTools.WriteFinalEvidence(
                            image,
                            maximumForwardDisplacement,
                            maximumHeightGain,
                            maximumTiltDegrees,
                            observedExplosionStart,
                            observedRecoveryDuration,
                            observedIdleHoldDuration,
                            usedExplosionForce,
                            usedConfigurableJoint,
                            animatorStayedEnabled,
                            observedPhaseMask,
                            SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(image);
                        CleanupPanels();
                    }
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                KnockbackReactionAnimationSetupTools.Inspect();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(
                    "Knockback_Reaction was directly observed through wait, Rigidbody explosion knockback, recovery, Player_Idle hold, and immediate reset.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            target = KnockbackReactionAnimationSetupTools.RequireRuntimeTarget();
            animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Knockback_Reaction Animator is missing in Play Mode.");
            cycle = KnockbackReactionAnimationSetupTools.RequireRuntimeActiveRagdoll(target);
            cycleType = cycle.GetType();
            startedAt = EditorApplication.timeSinceStartup;
            viewDirection = target.transform.forward.normalized;
            maximumForwardDisplacement = 0f;
            maximumHeightGain = 0f;
            maximumTiltDegrees = 0f;
            observedExplosionStart = 0f;
            observedRecoveryDuration = 0f;
            observedIdleHoldDuration = 0f;
            usedExplosionForce = false;
            usedConfigurableJoint = false;
            animatorStayedEnabled = true;
            observedPhaseMask = 0;
            nextPanel = 0;
        }

        private static void InspectRuntimeAndCapture()
        {
            string phase = GetProperty("Phase").ToString();
            float cycleElapsed = Convert.ToSingle(GetProperty("CycleElapsed"), CultureInfo.InvariantCulture);
            float phaseElapsed = Convert.ToSingle(GetProperty("PhaseElapsed"), CultureInfo.InvariantCulture);
            int completed = Convert.ToInt32(GetProperty("CompletedCycleCount"), CultureInfo.InvariantCulture);
            maximumForwardDisplacement = Mathf.Max(
                maximumForwardDisplacement,
                Convert.ToSingle(GetProperty("LastMaximumForwardDisplacement"), CultureInfo.InvariantCulture));
            maximumHeightGain = Mathf.Max(
                maximumHeightGain,
                Convert.ToSingle(GetProperty("LastMaximumHeightGain"), CultureInfo.InvariantCulture));
            maximumTiltDegrees = Mathf.Max(
                maximumTiltDegrees,
                Convert.ToSingle(GetProperty("LastMaximumTiltDegrees"), CultureInfo.InvariantCulture));
            observedExplosionStart = Mathf.Max(
                observedExplosionStart,
                Convert.ToSingle(GetProperty("LastExplosionStartedAtSeconds"), CultureInfo.InvariantCulture));
            observedRecoveryDuration = Mathf.Max(
                observedRecoveryDuration,
                Convert.ToSingle(GetProperty("LastRecoveryDuration"), CultureInfo.InvariantCulture));
            observedIdleHoldDuration = Mathf.Max(
                observedIdleHoldDuration,
                Convert.ToSingle(GetProperty("LastIdleHoldDuration"), CultureInfo.InvariantCulture));
            usedExplosionForce |= Convert.ToBoolean(GetProperty("UsesRigidbodyExplosionForce"), CultureInfo.InvariantCulture);
            usedConfigurableJoint |= Convert.ToBoolean(GetProperty("UsesConfigurableJoint"), CultureInfo.InvariantCulture);
            animatorStayedEnabled &= Convert.ToBoolean(GetProperty("AnimatorStayedEnabled"), CultureInfo.InvariantCulture);

            int phaseIndex = phase == "Waiting" ? 0 :
                phase == "PhysicsKnockback" ? 1 :
                phase == "Recovering" ? 2 :
                phase == "IdleHold" ? 3 : -1;
            if (phaseIndex >= 0)
                observedPhaseMask |= 1 << phaseIndex;

            bool capture = nextPanel == 0 && phase == "Waiting" && cycleElapsed >= 1.45f ||
                nextPanel == 1 && phase == "PhysicsKnockback" && phaseElapsed >= 0.12f ||
                nextPanel == 2 && phase == "PhysicsKnockback" &&
                    maximumForwardDisplacement >= 1.75f ||
                nextPanel == 3 && phase == "IdleHold" && phaseElapsed >= 0.35f ||
                nextPanel == 4 && phase == "Waiting" && completed >= 1 && cycleElapsed >= 0.12f;
            if (!capture)
                return;
            Panels.Add(RenderTarget());
            nextPanel++;
        }

        private static object GetProperty(string name)
        {
            return cycleType.GetProperty(
                    name,
                    BindingFlags.Public | BindingFlags.Instance)?.GetValue(cycle) ??
                throw new InvalidOperationException(
                    ActiveLabel() + " property is unavailable: " + name + ".");
        }

        private static string ActiveLabel()
        {
            return cycleType != null ? cycleType.FullName : "Knockback active ragdoll";
        }

        private static Texture2D RenderTarget()
        {
            const int width = 420;
            const int height = 520;
            Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (targetRenderers.Length == 0)
                throw new InvalidOperationException(
                    "Knockback target has no enabled renderers.");
            Bounds bounds = targetRenderers[0].bounds;
            foreach (Renderer renderer in targetRenderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            GameObject cameraObject = new GameObject("KnockbackReviewCamera", typeof(Camera));
            GameObject lightObject = new GameObject("KnockbackReviewLight", typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            Renderer[] others = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(target.transform))
                .ToArray();
            bool[] states = others.Select(renderer => renderer.forceRenderingOff).ToArray();
            RenderTexture renderTexture = null;
            Texture2D result = null;
            try
            {
                foreach (Renderer renderer in others)
                    renderer.forceRenderingOff = true;
                Vector3 focus = bounds.center;
                float distance = Mathf.Max(6f, bounds.extents.magnitude * 3.2f);
                camera.transform.position = focus + viewDirection * distance + Vector3.up * 0.18f;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y * 1.25f, 1.75f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.025f, 0.04f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
                renderTexture = RenderTexture.GetTemporary(
                    width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return result;
            }
            catch
            {
                if (result != null)
                    UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
            finally
            {
                for (int index = 0; index < others.Length; index++)
                    if (others[index] != null)
                        others[index].forceRenderingOff = states[index];
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != RequiredPanelCount)
                throw new InvalidOperationException(
                    "Knockback direct review requires five phase panels.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(width * RequiredPanelCount, height,
                TextureFormat.RGBA32, false);
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(index * width, 0, width, height, Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Knockback direct Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            animator = null;
            cycle = null;
            cycleType = null;
            nextPanel = 0;
            observedPhaseMask = 0;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
        }
    }

    [InitializeOnLoad]
    internal static class KnockbackReactionFlightPosePlayModeCapture
    {
        private const string PendingKey =
            "Bellerophon.KnockbackReactionFlightPose.Pending";
        private const string StateKey =
            "Bellerophon.KnockbackReactionFlightPose.State";
        private const string ConsoleErrorsBeforeKey =
            "Bellerophon.KnockbackReactionFlightPose.ConsoleErrorsBefore";
        private const string FailureKey =
            "Bellerophon.KnockbackReactionFlightPose.Failure";
        private const string CaptureFinalKey =
            "Bellerophon.KnockbackReactionFlightPose.CaptureFinal";
        private const string LandingArmReviewKey =
            "Bellerophon.KnockbackReactionFlightPose.LandingArmReview";
        private const string LandingDiagnosticPassedKey =
            "Bellerophon.KnockbackReactionFlightPose.LandingDiagnosticPassed";
        private const string LandingDiagnosticFrameStepKey =
            "Bellerophon.KnockbackReactionFlightPose.LandingDiagnosticFrameStep";
        private const string LandingDiagnosticErrorIncreaseKey =
            "Bellerophon.KnockbackReactionFlightPose.LandingDiagnosticErrorIncrease";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditModeAfterSuccess = 3;
        private const int WaitingForEditModeAfterFailure = 4;
        private const int RequiredPanelCount = 5;

        private static readonly List<Texture2D> Panels = new List<Texture2D>();
        private static Action<string> complete;
        private static Action<Exception> fail;
        private static GameObject target;
        private static GameObject playerIdleTarget;
        private static Component cycle;
        private static Component flightPose;
        private static Type cycleType;
        private static Type flightPoseType;
        private static double startedAt;
        private static Vector3 initialPosition;
        private static Vector3 approvedForward;
        private static Vector3 explosionOrigin;
        private static Vector3 reviewViewDirection;
        private static float maximumForwardDisplacement;
        private static float maximumLateralDisplacement;
        private static float maximumHeightGain;
        private static float maximumTiltDegrees;
        private static float observedExplosionStart;
        private static float observedRecoveryDuration;
        private static float observedIdleHoldDuration;
        private static int configuredLegBodyCount;
        private static int maximumDynamicLegBodyCount;
        private static bool usedLowerBodyConfigurableJoints;
        private static float maximumJointAnchorSeparation;
        private static float maximumBoneLengthError;
        private static float maximumStableArmForwardError;
        private static float maximumStableArmBendDegrees;
        private static float maximumStablePalmForwardError;
        private static float minimumMovementDirectionDot = 1f;
        private static float minimumInitialVelocityDirectionDot = 1f;
        private static int explosionEventCount;
        private static float maximumLegAngularVelocity;
        private static float maximumLegPoseDeviation;
        private static float maximumAsymmetricLegAngularSpeedDelta;
        private static bool observedWaitingAfterInitialization;
        private static int landingArmRecoverySampleCount;
        private static float firstLandingArmIdleRotationError;
        private static float previousLandingArmIdleRotationError;
        private static float finalLandingArmIdleRotationError;
        private static float finalLandingArmIdlePositionError;
        private static float maximumLandingArmFrameStepDegrees;
        private static float maximumLandingArmErrorIncrease;
        private static bool landingArmRecoveryReachedIdle;
        private static bool observedLandingRecoverySample;
        private static bool landingRecoverySequenceActive;
        private static int observedPhaseMask;
        private static int nextPanel;

        static KnockbackReactionFlightPosePlayModeCapture()
        {
            if (HasPendingCapture)
                Subscribe();
        }

        internal static bool HasPendingCapture =>
            SessionState.GetBool(PendingKey, false);

        internal static void ResetStaleCapture()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Cleanup();
        }

        internal static void Start(Action<string> onComplete, Action<Exception> onFail)
        {
            Start(true, onComplete, onFail);
        }

        internal static void Start(
            bool captureFinal,
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            StartInternal(captureFinal, false, onComplete, onFail);
        }

        internal static void StartLandingArmReview(
            bool captureFinal,
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (captureFinal && !SessionState.GetBool(LandingDiagnosticPassedKey, false))
                throw new InvalidOperationException(
                    "Landing-arm final capture requires a passing non-capture runtime diagnostic first.");
            if (!captureFinal)
                SessionState.SetBool(LandingDiagnosticPassedKey, false);
            StartInternal(captureFinal, true, onComplete, onFail);
        }

        private static void StartInternal(
            bool captureFinal,
            bool landingArmReview,
            Action<string> onComplete,
            Action<Exception> onFail)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Knockback flight-pose review must start in Edit Mode.");
            if (landingArmReview)
                KnockbackReactionAnimationSetupTools.InspectLandingArmRecovery();
            else
                KnockbackReactionAnimationSetupTools.InspectImmediateLaunch();
            complete = onComplete;
            fail = onFail;
            CleanupPanels();
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CaptureFinalKey, captureFinal);
            SessionState.SetBool(LandingArmReviewKey, landingArmReview);
            SessionState.SetInt(StateKey, WaitingForPlayMode);
            SessionState.SetInt(
                ConsoleErrorsBeforeKey,
                LightsaberSetupTools.ConsoleErrorCount());
            SessionState.EraseString(FailureKey);
            Subscribe();
            EditorApplication.EnterPlaymode();
        }

        internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
        {
            complete = onComplete;
            fail = onFail;
            if (!HasPendingCapture)
                throw new InvalidOperationException(
                    "Knockback flight-pose review has no pending state.");
            Subscribe();
        }

        private static void Subscribe()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!HasPendingCapture)
            {
                EditorApplication.update -= Tick;
                return;
            }
            int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (!EditorApplication.isPlaying)
                        return;
                    InitializeRuntime();
                    SessionState.SetInt(StateKey, Capturing);
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                        throw new InvalidOperationException(
                            "Play Mode ended before Knockback flight-pose review completed.");
                    if (EditorApplication.timeSinceStartup - startedAt > 22d)
                        throw new TimeoutException(
                            "Knockback flight-pose direct review exceeded 22 seconds.");
                    InspectRuntimeAndCapture();
                    bool captureFinal = SessionState.GetBool(CaptureFinalKey, true);
                    bool cycleComplete = explosionEventCount >= 2 &&
                        cycle.GetType().GetProperty(
                            "PhaseElapsed",
                            BindingFlags.Public | BindingFlags.Instance) != null &&
                        GetCycleProperty("Phase").ToString() == "PhysicsKnockback" &&
                        FloatCycle("PhaseElapsed") >= 0.12f;
                    if (captureFinal && Panels.Count < RequiredPanelCount)
                        return;
                    if (!captureFinal && !cycleComplete)
                        return;
                    bool landingArmReview =
                        SessionState.GetBool(LandingArmReviewKey, false);
                    if (captureFinal)
                    {
                        Texture2D image = CombinePanels();
                        try
                        {
                            if (landingArmReview)
                                KnockbackReactionAnimationSetupTools.WriteLandingArmRecoveryFinalEvidence(
                                    image,
                                    maximumForwardDisplacement,
                                    maximumLateralDisplacement,
                                    observedRecoveryDuration,
                                    explosionEventCount,
                                    landingArmRecoverySampleCount,
                                    firstLandingArmIdleRotationError,
                                    finalLandingArmIdleRotationError,
                                    finalLandingArmIdlePositionError,
                                    SessionState.GetFloat(
                                        LandingDiagnosticFrameStepKey,
                                        maximumLandingArmFrameStepDegrees),
                                    SessionState.GetFloat(
                                        LandingDiagnosticErrorIncreaseKey,
                                        maximumLandingArmErrorIncrease),
                                    landingArmRecoveryReachedIdle,
                                    maximumJointAnchorSeparation,
                                    maximumBoneLengthError,
                                    maximumLegPoseDeviation,
                                    observedPhaseMask,
                                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                            else
                                KnockbackReactionAnimationSetupTools.WriteImmediateLaunchFinalEvidence(
                                    image,
                                    maximumForwardDisplacement,
                                    maximumLateralDisplacement,
                                    maximumHeightGain,
                                    maximumTiltDegrees,
                                    observedExplosionStart,
                                    observedRecoveryDuration,
                                    observedIdleHoldDuration,
                                    minimumInitialVelocityDirectionDot,
                                    explosionEventCount,
                                    configuredLegBodyCount,
                                    maximumDynamicLegBodyCount,
                                    usedLowerBodyConfigurableJoints,
                                    maximumJointAnchorSeparation,
                                    maximumBoneLengthError,
                                    maximumStableArmForwardError,
                                    maximumStableArmBendDegrees,
                                    maximumStablePalmForwardError,
                                    minimumMovementDirectionDot,
                                    maximumLegAngularVelocity,
                                    maximumLegPoseDeviation,
                                    maximumAsymmetricLegAngularSpeedDelta,
                                    observedPhaseMask,
                                    observedWaitingAfterInitialization,
                                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(image);
                            CleanupPanels();
                        }
                    }
                    else
                    {
                        if (landingArmReview)
                        {
                            KnockbackReactionAnimationSetupTools.ValidateLandingArmRecoveryEvidence(
                                maximumForwardDisplacement,
                                maximumLateralDisplacement,
                                observedRecoveryDuration,
                                explosionEventCount,
                                landingArmRecoverySampleCount,
                                firstLandingArmIdleRotationError,
                                finalLandingArmIdleRotationError,
                                finalLandingArmIdlePositionError,
                                maximumLandingArmFrameStepDegrees,
                                maximumLandingArmErrorIncrease,
                                landingArmRecoveryReachedIdle,
                                maximumJointAnchorSeparation,
                                maximumBoneLengthError,
                                maximumLegPoseDeviation,
                                observedPhaseMask,
                                SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                            SessionState.SetFloat(
                                LandingDiagnosticFrameStepKey,
                                maximumLandingArmFrameStepDegrees);
                            SessionState.SetFloat(
                                LandingDiagnosticErrorIncreaseKey,
                                maximumLandingArmErrorIncrease);
                            SessionState.SetBool(LandingDiagnosticPassedKey, true);
                        }
                        else
                            KnockbackReactionAnimationSetupTools.ValidateImmediateLaunchEvidence(
                                maximumForwardDisplacement,
                                maximumLateralDisplacement,
                                maximumHeightGain,
                                maximumTiltDegrees,
                                observedExplosionStart,
                                observedRecoveryDuration,
                                observedIdleHoldDuration,
                                minimumInitialVelocityDirectionDot,
                                explosionEventCount,
                                configuredLegBodyCount,
                                maximumDynamicLegBodyCount,
                                usedLowerBodyConfigurableJoints,
                                maximumJointAnchorSeparation,
                                maximumBoneLengthError,
                                maximumStableArmForwardError,
                                maximumStableArmBendDegrees,
                                maximumStablePalmForwardError,
                                minimumMovementDirectionDot,
                                maximumLegAngularVelocity,
                                maximumLegPoseDeviation,
                                maximumAsymmetricLegAngularSpeedDelta,
                                observedPhaseMask,
                                observedWaitingAfterInitialization,
                                SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                    }
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                    EditorApplication.ExitPlaymode();
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                if (state == WaitingForEditModeAfterFailure)
                {
                    FinishFailure();
                    return;
                }
                bool landingReview = SessionState.GetBool(LandingArmReviewKey, false);
                if (landingReview)
                    KnockbackReactionAnimationSetupTools.InspectLandingArmRecovery();
                else
                    KnockbackReactionAnimationSetupTools.InspectImmediateLaunch();
                Action<string> callback = complete;
                Cleanup();
                callback?.Invoke(landingReview
                    ? "Knockback_Reaction was directly observed blending both arms from the flight pose into the live Player_Idle scene object's arm pose throughout landing recovery, while preserving the approved physical flight and repeat cycle."
                    : "Knockback_Reaction was directly observed launching immediately along its off-centre physical explosion path with straight travel-facing arms and palms, mildly reactive jointed legs, Player_Idle recovery, hold, and immediate repeat.");
            }
            catch (Exception exception)
            {
                SessionState.SetString(FailureKey, exception.ToString());
                CleanupPanels();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                    if (EditorApplication.isPlaying)
                        EditorApplication.ExitPlaymode();
                    return;
                }
                FinishFailure();
            }
        }

        private static void InitializeRuntime()
        {
            target = KnockbackReactionAnimationSetupTools.RequireRuntimeTarget();
            playerIdleTarget =
                KnockbackReactionAnimationSetupTools.RequireRuntimeIdleReference();
            cycle = KnockbackReactionAnimationSetupTools.RequireRuntimeActiveRagdoll(target);
            flightPose = KnockbackReactionAnimationSetupTools.RequireRuntimeFlightPose(target);
            cycleType = cycle.GetType();
            flightPoseType = flightPose.GetType();
            startedAt = EditorApplication.timeSinceStartup;
            initialPosition = target.transform.position;
            approvedForward = ((Vector3)GetCycleProperty("ApprovedForwardDirection")).normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, approvedForward).normalized;
            reviewViewDirection = (approvedForward * 0.42f + lateral * 0.91f).normalized;
            explosionOrigin = initialPosition - approvedForward * 3f + Vector3.up * 0.55f;
            maximumForwardDisplacement = 0f;
            maximumLateralDisplacement = 0f;
            maximumHeightGain = 0f;
            maximumTiltDegrees = 0f;
            observedExplosionStart = 0f;
            observedRecoveryDuration = 0f;
            observedIdleHoldDuration = 0f;
            configuredLegBodyCount = 0;
            maximumDynamicLegBodyCount = 0;
            usedLowerBodyConfigurableJoints = false;
            maximumJointAnchorSeparation = 0f;
            maximumBoneLengthError = 0f;
            maximumStableArmForwardError = 0f;
            maximumStableArmBendDegrees = 0f;
            maximumStablePalmForwardError = 0f;
            minimumMovementDirectionDot = 1f;
            minimumInitialVelocityDirectionDot = 1f;
            explosionEventCount = 0;
            maximumLegAngularVelocity = 0f;
            maximumLegPoseDeviation = 0f;
            maximumAsymmetricLegAngularSpeedDelta = 0f;
            observedWaitingAfterInitialization = false;
            landingArmRecoverySampleCount = 0;
            firstLandingArmIdleRotationError = 0f;
            previousLandingArmIdleRotationError = 0f;
            finalLandingArmIdleRotationError = float.PositiveInfinity;
            finalLandingArmIdlePositionError = float.PositiveInfinity;
            maximumLandingArmFrameStepDegrees = 0f;
            maximumLandingArmErrorIncrease = 0f;
            landingArmRecoveryReachedIdle = false;
            observedLandingRecoverySample = false;
            landingRecoverySequenceActive = false;
            observedPhaseMask = 0;
            nextPanel = 0;
        }

        private static void InspectRuntimeAndCapture()
        {
            string phase = GetCycleProperty("Phase").ToString();
            float cycleElapsed = FloatCycle("CycleElapsed");
            float phaseElapsed = FloatCycle("PhaseElapsed");
            int completed = Convert.ToInt32(
                GetCycleProperty("CompletedCycleCount"),
                CultureInfo.InvariantCulture);
            maximumForwardDisplacement = Mathf.Max(
                maximumForwardDisplacement,
                FloatCycle("LastMaximumForwardDisplacement"),
                FloatCycle("CurrentTravelDisplacement"));
            Vector3 displacement = target.transform.position - initialPosition;
            maximumLateralDisplacement = Mathf.Max(
                maximumLateralDisplacement,
                Vector3.ProjectOnPlane(displacement, approvedForward).magnitude);
            maximumHeightGain = Mathf.Max(
                maximumHeightGain,
                FloatCycle("LastMaximumHeightGain"),
                target.transform.position.y - initialPosition.y);
            maximumTiltDegrees = Mathf.Max(
                maximumTiltDegrees,
                FloatCycle("LastMaximumTiltDegrees"),
                Quaternion.Angle(target.transform.rotation,
                    Quaternion.LookRotation(approvedForward, Vector3.up)));
            observedExplosionStart = Mathf.Max(
                observedExplosionStart,
                FloatCycle("LastExplosionStartedAtSeconds"));
            observedRecoveryDuration = Mathf.Max(
                observedRecoveryDuration,
                FloatCycle("LastRecoveryDuration"));
            observedIdleHoldDuration = Mathf.Max(
                observedIdleHoldDuration,
                FloatCycle("LastIdleHoldDuration"));
            Vector3 runtimeOrigin = (Vector3)GetCycleProperty("LastExplosionOrigin");
            if (runtimeOrigin.sqrMagnitude > 0.001f)
                explosionOrigin = runtimeOrigin;
            explosionEventCount = Mathf.Max(
                explosionEventCount,
                Convert.ToInt32(
                    GetCycleProperty("ExplosionEventCount"),
                    CultureInfo.InvariantCulture));
            float initialVelocityDirectionDot =
                FloatCycle("LastInitialVelocityDirectionDot");
            if (explosionEventCount > 0 && initialVelocityDirectionDot > 0f)
                minimumInitialVelocityDirectionDot = Mathf.Min(
                    minimumInitialVelocityDirectionDot,
                    initialVelocityDirectionDot);

            configuredLegBodyCount = Mathf.Max(
                configuredLegBodyCount,
                IntFlight("ConfiguredLegBodyCount"));
            maximumDynamicLegBodyCount = Mathf.Max(
                maximumDynamicLegBodyCount,
                IntFlight("DynamicLegBodyCount"));
            usedLowerBodyConfigurableJoints |=
                BoolFlight("UsesLowerBodyConfigurableJoints");
            maximumJointAnchorSeparation = Mathf.Max(
                maximumJointAnchorSeparation,
                FloatFlight("MaximumJointAnchorSeparation"));
            maximumBoneLengthError = Mathf.Max(
                maximumBoneLengthError,
                FloatFlight("MaximumBoneLengthError"));
            maximumStableArmForwardError = Mathf.Max(
                maximumStableArmForwardError,
                FloatFlight("MaximumStableArmForwardError"));
            maximumStableArmBendDegrees = Mathf.Max(
                maximumStableArmBendDegrees,
                FloatFlight("MaximumStableArmBendDegrees"));
            maximumStablePalmForwardError = Mathf.Max(
                maximumStablePalmForwardError,
                FloatFlight("MaximumStablePalmForwardError"));
            minimumMovementDirectionDot = Mathf.Min(
                minimumMovementDirectionDot,
                FloatFlight("MinimumStableMovementDirectionDot"));
            maximumLegAngularVelocity = Mathf.Max(
                maximumLegAngularVelocity,
                FloatFlight("MaximumLegAngularVelocity"));
            maximumLegPoseDeviation = Mathf.Max(
                maximumLegPoseDeviation,
                FloatFlight("MaximumLegPoseDeviation"));
            maximumAsymmetricLegAngularSpeedDelta = Mathf.Max(
                maximumAsymmetricLegAngularSpeedDelta,
                FloatFlight("MaximumAsymmetricLegAngularSpeedDelta"));
            maximumLandingArmFrameStepDegrees = Mathf.Max(
                maximumLandingArmFrameStepDegrees,
                FloatFlight("MaximumLandingArmFrameStepDegrees"));
            landingArmRecoverySampleCount = Mathf.Max(
                landingArmRecoverySampleCount,
                IntFlight("LandingArmRecoverySampleCount"));
            if (phase == "Recovering")
            {
                float currentLandingError =
                    FloatFlight("CurrentLandingArmIdleRotationError");
                if (!landingRecoverySequenceActive)
                {
                    if (!observedLandingRecoverySample)
                        firstLandingArmIdleRotationError = currentLandingError;
                    previousLandingArmIdleRotationError = currentLandingError;
                    observedLandingRecoverySample = true;
                    landingRecoverySequenceActive = true;
                }
                else
                {
                    maximumLandingArmErrorIncrease = Mathf.Max(
                        maximumLandingArmErrorIncrease,
                        currentLandingError - previousLandingArmIdleRotationError);
                    previousLandingArmIdleRotationError = currentLandingError;
                }
            }
            else
            {
                landingRecoverySequenceActive = false;
            }
            if (phase == "IdleHold")
            {
                finalLandingArmIdleRotationError =
                    FloatFlight("CurrentLandingArmIdleRotationError");
                finalLandingArmIdlePositionError =
                    FloatFlight("CurrentLandingArmIdlePositionError");
                landingArmRecoveryReachedIdle |=
                    BoolFlight("LandingArmRecoveryReachedIdle");
            }

            int phaseIndex = phase == "Waiting" ? 0 :
                phase == "PhysicsKnockback" ? 1 :
                phase == "Recovering" ? 2 :
                phase == "IdleHold" ? 3 : -1;
            if (phaseIndex >= 0)
                observedPhaseMask |= 1 << phaseIndex;
            observedWaitingAfterInitialization |= phase == "Waiting";

            if (!SessionState.GetBool(CaptureFinalKey, true))
                return;
            bool landingArmReview =
                SessionState.GetBool(LandingArmReviewKey, false);
            bool capture = landingArmReview
                ? nextPanel == 0 && phase == "PhysicsKnockback" &&
                        maximumForwardDisplacement >= 1.75f ||
                    nextPanel == 1 && phase == "Recovering" && phaseElapsed >= 0.015f ||
                    nextPanel == 2 && phase == "Recovering" && phaseElapsed >= 0.045f ||
                    nextPanel == 3 && phase == "Recovering" && phaseElapsed >= 0.075f ||
                    nextPanel == 4 && phase == "IdleHold" && phaseElapsed >= 0.12f
                : nextPanel == 0 && phase == "PhysicsKnockback" &&
                        phaseElapsed <= 0.12f ||
                    nextPanel == 1 && phase == "PhysicsKnockback" && phaseElapsed >= 0.08f ||
                    nextPanel == 2 && phase == "PhysicsKnockback" &&
                        maximumForwardDisplacement >= 1.15f ||
                    nextPanel == 3 && phase == "IdleHold" && phaseElapsed >= 0.35f ||
                    nextPanel == 4 && phase == "PhysicsKnockback" && completed >= 1 &&
                        phaseElapsed <= 0.18f;
            if (!capture)
                return;
            Panels.Add(RenderReviewPanel());
            nextPanel++;
        }

        private static object GetCycleProperty(string name)
        {
            return cycleType.GetProperty(
                    name,
                    BindingFlags.Public | BindingFlags.Instance)?.GetValue(cycle) ??
                throw new InvalidOperationException(
                    "Knockback active-ragdoll property is unavailable: " + name + ".");
        }

        private static object GetFlightProperty(string name)
        {
            return flightPoseType.GetProperty(
                    name,
                    BindingFlags.Public | BindingFlags.Instance)?.GetValue(flightPose) ??
                throw new InvalidOperationException(
                    "Knockback flight-pose property is unavailable: " + name + ".");
        }

        private static float FloatCycle(string name)
        {
            return Convert.ToSingle(GetCycleProperty(name), CultureInfo.InvariantCulture);
        }

        private static float FloatFlight(string name)
        {
            return Convert.ToSingle(GetFlightProperty(name), CultureInfo.InvariantCulture);
        }

        private static int IntFlight(string name)
        {
            return Convert.ToInt32(GetFlightProperty(name), CultureInfo.InvariantCulture);
        }

        private static bool BoolFlight(string name)
        {
            return Convert.ToBoolean(GetFlightProperty(name), CultureInfo.InvariantCulture);
        }

        private static Texture2D RenderReviewPanel()
        {
            const int mainWidth = 460;
            const int insetWidth = 220;
            const int height = 520;
            const int insetHeight = height / 2;
            Vector3 pathFocus = initialPosition + approvedForward * 1.25f + Vector3.up * 1.05f;
            Texture2D main = RenderView(
                mainWidth,
                height,
                pathFocus,
                reviewViewDirection,
                2.55f,
                target);
            Transform leftShoulder = RequireRuntimeTransform(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
            Transform rightShoulder = RequireRuntimeTransform(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
            Transform leftHand = RequireRuntimeTransform(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
            Transform rightHand = RequireRuntimeTransform(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
            Vector3 armFocus =
                (leftShoulder.position + rightShoulder.position +
                 leftHand.position + rightHand.position) * 0.25f;
            Texture2D arms = RenderView(
                insetWidth,
                insetHeight,
                armFocus,
                approvedForward,
                0.86f,
                target);
            Texture2D lowerInset;
            if (SessionState.GetBool(LandingArmReviewKey, false))
            {
                Transform referenceLeftShoulder = RequireReferenceTransform(
                    "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm");
                Transform referenceRightShoulder = RequireReferenceTransform(
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm");
                Transform referenceLeftHand = RequireReferenceTransform(
                    "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
                Transform referenceRightHand = RequireReferenceTransform(
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand");
                Vector3 referenceArmFocus =
                    (referenceLeftShoulder.position + referenceRightShoulder.position +
                     referenceLeftHand.position + referenceRightHand.position) * 0.25f;
                lowerInset = RenderView(
                    insetWidth,
                    insetHeight,
                    referenceArmFocus,
                    playerIdleTarget.transform.forward,
                    0.86f,
                    playerIdleTarget);
            }
            else
            {
                Transform leftUpLeg = RequireRuntimeTransform("Armature/Hips/LeftUpLeg");
                Transform rightUpLeg = RequireRuntimeTransform("Armature/Hips/RightUpLeg");
                Transform leftFoot = RequireRuntimeTransform("Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot");
                Transform rightFoot = RequireRuntimeTransform("Armature/Hips/RightUpLeg/RightLeg/RightFoot");
                Vector3 legFocus =
                    (leftUpLeg.position + rightUpLeg.position +
                     leftFoot.position + rightFoot.position) * 0.25f;
                Vector3 lateral = Vector3.Cross(Vector3.up, approvedForward).normalized;
                lowerInset = RenderView(
                    insetWidth,
                    insetHeight,
                    legFocus,
                    lateral,
                    0.92f,
                    target);
            }
            var panel = new Texture2D(
                mainWidth + insetWidth,
                height,
                TextureFormat.RGBA32,
                false);
            panel.SetPixels32(0, 0, mainWidth, height, main.GetPixels32());
            panel.SetPixels32(mainWidth, insetHeight, insetWidth, insetHeight, arms.GetPixels32());
            panel.SetPixels32(mainWidth, 0, insetWidth, insetHeight, lowerInset.GetPixels32());
            panel.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(main);
            UnityEngine.Object.DestroyImmediate(arms);
            UnityEngine.Object.DestroyImmediate(lowerInset);
            return panel;
        }

        private static Texture2D RenderView(
            int width,
            int height,
            Vector3 focus,
            Vector3 fromTargetDirection,
            float orthographicSize,
            GameObject visibleRoot)
        {
            GameObject cameraObject = new GameObject(
                "KnockbackFlightPoseReviewCamera",
                typeof(Camera));
            GameObject lightObject = new GameObject(
                "KnockbackFlightPoseReviewLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            Light light = lightObject.GetComponent<Light>();
            Renderer[] others = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(renderer => !renderer.transform.IsChildOf(visibleRoot.transform))
                .ToArray();
            bool[] states = others.Select(renderer => renderer.forceRenderingOff).ToArray();
            RenderTexture renderTexture = null;
            Texture2D result = null;
            try
            {
                foreach (Renderer renderer in others)
                    renderer.forceRenderingOff = true;
                Vector3 direction = fromTargetDirection.sqrMagnitude > 0.9f
                    ? fromTargetDirection.normalized
                    : Vector3.forward;
                float distance = 8f;
                camera.transform.position = focus + direction * distance;
                camera.transform.rotation = Quaternion.LookRotation(
                    focus - camera.transform.position,
                    Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.012f, 0.02f, 0.035f, 1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = Color.white;
                light.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
                renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                RenderTexture previous = RenderTexture.active;
                camera.Render();
                RenderTexture.active = renderTexture;
                result = new Texture2D(width, height, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                result.Apply(false, false);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                return result;
            }
            catch
            {
                if (result != null)
                    UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
            finally
            {
                for (int index = 0; index < others.Length; index++)
                    if (others[index] != null)
                        others[index].forceRenderingOff = states[index];
                if (renderTexture != null)
                    RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Transform RequireRuntimeTransform(string path)
        {
            return target.transform.Find(path) ?? throw new InvalidOperationException(
                "Knockback flight-pose review rig is missing " + path + ".");
        }

        private static Transform RequireReferenceTransform(string path)
        {
            return playerIdleTarget.transform.Find(path) ??
                throw new InvalidOperationException(
                    "Player_Idle comparison rig is missing " + path + ".");
        }

        private static Texture2D CombinePanels()
        {
            if (Panels.Count != RequiredPanelCount)
                throw new InvalidOperationException(
                    "Knockback flight-pose review requires five phase panels.");
            int width = Panels[0].width;
            int height = Panels[0].height;
            var sheet = new Texture2D(
                width * RequiredPanelCount,
                height,
                TextureFormat.RGBA32,
                false);
            for (int index = 0; index < Panels.Count; index++)
                sheet.SetPixels32(
                    index * width,
                    0,
                    width,
                    height,
                    Panels[index].GetPixels32());
            sheet.Apply(false, false);
            return sheet;
        }

        private static void CleanupPanels()
        {
            foreach (Texture2D panel in Panels)
                if (panel != null)
                    UnityEngine.Object.DestroyImmediate(panel);
            Panels.Clear();
        }

        private static void FinishFailure()
        {
            string message = SessionState.GetString(
                FailureKey,
                "Knockback flight-pose direct Play Mode review failed.");
            Action<Exception> callback = fail;
            Cleanup();
            callback?.Invoke(new InvalidOperationException(message));
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            CleanupPanels();
            complete = null;
            fail = null;
            target = null;
            playerIdleTarget = null;
            cycle = null;
            flightPose = null;
            cycleType = null;
            flightPoseType = null;
            nextPanel = 0;
            observedPhaseMask = 0;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseInt(StateKey);
            SessionState.EraseInt(ConsoleErrorsBeforeKey);
            SessionState.EraseString(FailureKey);
            SessionState.EraseBool(CaptureFinalKey);
            SessionState.EraseBool(LandingArmReviewKey);
        }
    }
}
